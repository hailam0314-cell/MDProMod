# 阶段怎么切换：规则、协议与实测（2026-10-14）

目的：搞清 ocgcore 里“换阶段”到底由谁决定、玩家提交什么、场上应该怎么显示。
证据来源：

- `Tools/YGO Classes/ocgcore/processor.cpp`（`process_turn`、`process_idle_command`、
  `process_battle_command`）、`playerop.cpp`（消息体与应答校验）、`common.h`（阶段常量）。
- MDPro3 客户端：`Servants/OcgCore.cs`、`UI/PopupDuelPhase.cs`、`UI/PhaseButtonHandler.cs`。
- 实测：`tools/duel_host/phase_probe.py`，原始记录
  `docs/phase-switch-probe-2026-10-14.json`（手动驱动 Duel Host，逐阶段点过去）。

## 一、规则层：阶段顺序与谁能改

一个回合的常规顺序：

```
抽卡(DP) → 准备(SP) → 主要1(M1) → 战斗(BP) → 主要2(M2) → 结束(EP)
```

战斗阶段内部还会拆成：战斗开始(0x08) → 战斗步骤(0x10) → 伤害(0x20) → 伤害计算(0x40)
→ 战斗(0x80)。这些也会各自发一条阶段消息。

关键点是：**阶段不是玩家“点”出来的，是核心推进出来的**。玩家能做的只是从核心
当前给出的选项里挑一个。核心在 M1/M2 给“进战斗阶段 / 进结束阶段”，在战斗阶段
给“进主要2 / 结束战斗”。其余情况（抽卡、准备、伤害步骤等）根本没有选项，玩家
只能看着。

什么时候“没有这个选项”（`processor.cpp:2068`、`2473`）：

| 限制 | 结果 |
| --- | --- |
| 决斗第一回合、且没有开 `DUEL_ATTACK_FIRST_TURN` | 不能进 BP |
| 当前已经是 M2 | 不能进 BP（`battle` 恒为 0） |
| 受 `EFFECT_CANNOT_BP` 影响 | 不能进 BP |
| 场上有必须攻击的怪兽、或受 `EFFECT_CANNOT_EP` 影响 | M1 里不能直接结束 |
| 受 `EFFECT_CANNOT_M2` / 必须攻击 | 战斗阶段不能进 M2；必须攻击时连 EP 都不能选 |
| 受 `EFFECT_SKIP_M1` / `EFFECT_SKIP_M2` | 该主要阶段不询问玩家，直接二选一 |
| 受 `EFFECT_SKIP_DP` / `SKIP_EP` / `SKIP_BP` | 对应阶段直接跳过（仍会播阶段消息） |

另外两个由核心处理的规则细节：

- 结束阶段手牌超过 6 张时，核心在 EP 里发 `SelectCard` 让玩家弃牌（实测复现）。
- 每个阶段切换前后会有**优先权窗口**，表现为 `SelectChain`（`count=0` 时选 `-1` 放弃）。

## 二、协议层：消息与应答

阶段常量（`common.h:397`，与客户端 `DuelPhase` 枚举一致）：

| 值 | 阶段 | 值 | 阶段 |
| --- | --- | --- | --- |
| 0x01 | 抽卡 DP | 0x20 | 伤害 |
| 0x02 | 准备 SP | 0x40 | 伤害计算 |
| 0x04 | 主要1 M1 | 0x80 | 战斗 |
| 0x08 | 战斗开始 | 0x100 | 主要2 M2 |
| 0x10 | 战斗步骤 | 0x200 | 结束 EP |

**`MSG_NEW_PHASE`(41)**：载荷是 `write_buffer16(phase)`，小端 2 字节，没有玩家字段。
`tools/duel_host` 打出的 hex 形如 `29 01 00` → 阶段 = 0x0001 = 抽卡。
`DuelHostProcess.applyStats` 里 `(byte2<<8)|byte1` 的解析是对的。

**`MSG_SELECT_IDLECMD`(11)**（M1 / M2 的决策点，`playerop.cpp:85`）：

```
player(1)
召唤列表  count + count*(code32,ctrl8,loc8,seq8)
特召列表、表示变更、怪兽盖放、魔陷盖放（各同上）
发动列表  count + count*(code32,ctrl8,loc8,seq8,desc32)
to_bp(1)   ← 1 才能进战斗阶段（仅 M1 且核心允许）
to_ep(1)   ← 1 才能进结束阶段
can_shuffle(1)
```

应答：`0..5` 分别对应召唤/特召/表示变更/盖怪/盖魔陷/发动效果，
**`6` = 进战斗阶段，`7` = 进结束阶段**（`processor.cpp:3843` 起）。

**`MSG_SELECT_BATTLECMD`(10)**（战斗阶段步骤里的决策点，`playerop.cpp:31`）：

```
player(1)
发动列表  count + count*(code32,ctrl8,loc8,seq8,desc32)
攻击列表  count + count*(code32,ctrl8,loc8,seq8,direct8)
to_m2(1)   ← 1 才能进主要阶段 2
to_ep(1)   ← 1 才能结束战斗
```

应答：`(序号<<16)|0` = 发动某张、`(序号<<16)|1` = 用某只怪兽攻击，
**`2` = 进主要阶段 2，`3` = 结束战斗阶段**（`processor.cpp:3906` 起）。
选 `3` 时核心会把 M2 标记为跳过：仍然会发一条 `MSG_NEW_PHASE`(0x100)，
但**不会**再给玩家 M2 的操作窗口。

非法应答的后果：核心回一条 `MSG_RETRY`(1)，要求重新选择。实测里在第一回合
发送 `6`（此时 `to_bp=0`）就吃到了 Retry。

## 三、实测时间线（`phase_probe.py`）

```
=== 回合 1 · 玩家0 ===
PHASE 0x0001 抽卡 → 0x0002 准备 → 0x0004 主要1
  SelectIdleCmd(玩家0) battle=no          ← 第一回合不给 BP
  应答 7 → SelectChain(玩家1) 放弃
PHASE 0x0200 结束 → 回合结束

=== 回合 2 · 玩家1 ===
PHASE 0x0001 抽卡（连锁窗口放弃）→ 0x0002 准备 → 0x0004 主要1
  SelectIdleCmd(玩家1) battle=yes
  应答 6 → SelectChain 放弃
PHASE 0x0008 战斗开始
  SelectBattleCmd(玩家1) main2=yes end=yes
  应答 2 → SelectChain 放弃
PHASE 0x0100 主要2
  SelectIdleCmd(玩家1) battle=no          ← M2 不再提供 BP
  应答 7 → SelectChain 放弃
PHASE 0x0200 结束

=== 回合 3 · 玩家0 ===
  主要1 battle=yes → 应答 6 → PHASE 0x0008 战斗开始
  SelectBattleCmd → 应答 3 → PHASE 0x0100 主要2（无操作窗口）→ PHASE 0x0200 结束

=== 回合 4 · 玩家1 ===
  主要1 battle=yes → 应答 7 → PHASE 0x0200 结束
  SelectCard 弃牌 1..1（结束阶段手牌上限 6）
```

## 四、对我们的按钮意味着什么

1. **“当前阶段”用 `MSG_NEW_PHASE` 驱动**，蓝色键的点亮完全跟这条消息走
   （`DuelHostProcess.phaseCode` → `DuelFieldEntity.setCurrentPhase`，已经通了）。
2. **“可以按的阶段”不是固定的顺序表**，必须来自当前 prompt 的两个允许位：
   - M1/M2 的 `SelectIdleCmd`：`to_bp` → 战斗键可点亮，`to_ep` → 结束键可点亮；
   - BP 的 `SelectBattleCmd`：`to_m2` → 主要2 可点亮，`to_ep` → 结束键可点亮。
   其余阶段键（抽卡、准备）永远不可按——玩家没有这个权限。
3. **点击要转成对应应答**：M1/M2 → `response 6` / `response 7`；BP → `response 2` / `response 3`。
   发之前必须确认对应允许位为真，否则核心回 `MSG_RETRY`。
4. 现在 Duel Host 的 prompt 只有一句 `summary`（`battle=yes/no`），**没有把允许位
   做成字段**，而且 `options` 无论允许与否都把 6/7 列出来 —— 客户端照着点就会踩
   Retry。接线前应先给 prompt 补上 `toBattle / toEnd / toMain2` 字段。
5. 连锁窗口（`SelectChain`）会插在阶段切换前后，按钮只在轮到自己的
   `SelectIdleCmd / SelectBattleCmd` 时可用；对手回合整排灰色。

## 五、接线结果（2026-10-14 已完成）

| 位置 | 改动 |
| --- | --- |
| `tools/duel_host/Program.cs` | `SelectIdleCmd` 补 `toBattle`/`toEnd`，`SelectBattleCmd` 补 `toMain2`/`toEnd`；`options` 只列核心真正允许的转移；`SelectChain` 补 `count`/`special`/`forced`；新增 `autoPassChain` 启动项 |
| `DuelHostProcess` | 解析上述字段，`phaseResponse(phase)` 把 BP/M2/EP 翻成 6/7/2/3，且只在允许位为真时返回 |
| `DuelSessionManager` | 每 tick 把 `promptPlayer / toBattle / toEnd / toMain2` 推给决斗盘；新增 `submitPhase(...)` |
| `DuelFieldEntity` | 新增同步字段与 `canAdvance(side, phase)`；阶段键状态改为 disabled / available / current 三态由核心允许位决定；点击改为提交应答，不再本地改阶段 |

验证：`tools/duel_host/field_flow_probe.py` 用与模组同形的 `start`（`ai=true, aiSeat=1,
autoPassChain=true`）跑完整链路——1 号位 WindBot 自动出牌，0 号位只用 6/7/2/3 应答，
六个回合零错误，阶段序列 DP→SP→M1→BP→M2→EP 全部出现，47 个连锁窗口被 host 自动放弃。
记录：`docs/field-flow-probe-2026-10-14.json`。

### 关于 autoPassChain

没有连锁/效果 UI 时，人类座位每个优先权窗口都要应答，决斗会卡死。现在
`DuelHostProcess.sendStart` 默认带 `autoPassChain: true`，由 host 按核心允许的方式
放弃（`count == 0` 或非强制 → `-1`；强制连锁 → 选第一个）。用
`-Dmdpromod.autoPassChain=false` 可以关掉，等连锁 UI 做好后应当关闭。

### 仍然缺的

- 人类座位的非阶段决策还没有 UI：`SelectCard`（弃牌）、`SelectPlace`、`SelectPosition`、
  `SelectEffectYN` 等仍然会停在提示上等应答，需要 prompt → GUI 那一步。
- 强制连锁目前由 host 自动选第一个选项，属于临时策略。
