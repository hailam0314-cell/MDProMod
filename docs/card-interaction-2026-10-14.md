# 卡片交互：把点击接进规则核心（2026-10-14）

## 交互方案

**动作菜单流（当前实现）**

1. **右键**卡片 = 详情大图（不变）。
2. **左键**卡片 → 卡片前面弹出动作按钮：**召唤 / 盖放 / 发动**（场上卡还会出现
   **攻击 / 表示**）；**只有核心当前给出候选的动作才会出现按钮**。
3. 左键点按钮 → 发送应答；如果需要落点，**左键点场地格子**选定（可选的格子会
   绿色高亮）。

所有"能不能做"都必须来自核心当前提示里给的候选列表——本地不推断规则。

### 实现要点

- 服务端 `offerCardActions` 把可用动作算成位掩码，通过 `ModNetwork.CardActions`
  发给客户端；`ClientCardMenu` 把卡片位置投影成屏幕坐标，在卡片上方画按钮，
  左键命中就回发 `ModNetwork.CardAct`，服务端再用 `performCardAction` 提交
  （菜单过期时以服务端的候选为准）。
- 落点高亮：`DuelSessionManager` 每 tick 把 `places`（打包成
  `controller<<16 | location<<8 | sequence`）通过 `ModNetwork.PlaceHint` 推给客户端，
  `ClientPlaceHints` 用 `DuelFieldLayout.zoneIdFor` 反查格子并画半透明绿块；
  左键格子 = `DuelFieldEntity.onLeftClick` → `submitPlaceFor`。
- 潜行左键的旧"盖放"快捷方式已去掉：盖放现在是菜单里的一个按钮。

## 数据链路

1. **host 侧**（`Program.cs` case 11）：把 `SelectIdleCmd` 的六个列表全部结构化输出：
   `summonCards / spsummonCards / positionCards / monsterSetCards / spellSetCards /
   activateCards`，每条含 `{index, code, controller, location, sequence}`。
   `index` 就是应答里要用的序号。
2. **应答编码**（与 ocgcore `select_idle_command` 的校验一致）：
   `(序号 << 16) | 类型`，类型 `0 召唤 / 1 特殊召唤 / 2 表示变更 / 3 盖怪 / 4 盖魔陷 / 5 发动`；
   战斗阶段的提示里则是 `攻击 = (序号<<16)|1`、`发动 = (序号<<16)|0`。
3. **模组**：`DuelHostProcess.promptAction(controller, location, sequence, kinds…)`
   按优先级在候选表里找这张卡，命中就返回应答值；`DuelSessionManager.submitCardAction`
   负责发送与日志。手牌用 `(side, 2, index)`、场上卡用 `DuelFieldLayout.engineAddress(zoneId)`。
4. **落点**：`hasPlacePrompt()` 为真时，右键场地格子 → `engineAddress` → 在
   `places` 里找下标 → `respondPlace(index)`（发送三字节 `[player, location, sequence]`）。

## 踩到的两个坑（都是这次修的）

- **`flag` 是禁用掩码**：`MSG_SELECT_PLACE` 里的 `flag` 表示"不能放"的格子，
  ocgcore 自己的 AI 分支第一步就是 `flag = ~flag`。host 的 `BuildPlaces` 之前没取反，
  于是列出的 23 个"可用落点"其实全是禁止的，任何落点应答都会被 `MSG_RETRY` 顶回来。
  现在取反之后再列，正常召唤给出的是 5 个主怪兽区。
- **AI 查询喂越界**：`RefreshAiState` 会把合成查询喂给 WindBot，其中"对手的卡组 /
  额外卡组"是隐藏堆，AI 模型里没有对应记录，一读就 `EndOfStreamException`，
  而异常没人接、整局静默结束。现在隐藏堆不再喂，且喂查询失败只记一条 `warn`，
  不会再带走对局；消息处理也加了兜底，任何解析异常都会以 `error` 事件报出来。

## 验证

- `tools/duel_host/card_action_probe.py`：用真实中文卡组（`珠泪.ydk`）与同形 start 命令，
  从决策点发 `response 0`（召唤候选 0）→ 收到 `SelectPlace`（5 个主怪兽区）→
  发 `responseBytes 000400` → **`saw_summoning=true`、`saw_summoned=true`、`retries=0`**。
- 回归：`field_flow_probe.py` 仍能跑通六个回合，阶段提示序列不变
  （顺带发现旧解析把列表读串了，现在 summary 里 `sset=5/6` 等数字才是真实值）。
- 产物：`mdpromod-0.1.0.jar`（2,233,181 字节）已复制到客户端 mods 目录，哈希校验一致。

## 还没做

- 攻击宣言之后的"选择攻击对象"（`SelectCard` / `SelectUnselectCard`）还没有 UI，
  会停在提示上；`SelectEffectYN`（要不要发动）同样。
- 手牌/场上的"发动"目前只挑第一个候选；同一张卡有多个可选效果时还不能选。
- 需要落点时没有高亮提示（格子可点，但玩家看不出哪几个能选）。
