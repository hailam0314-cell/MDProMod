# SelectChain 帧长错位：连锁界面/手牌按钮/场地魔法"卡死"的同一个根因（2026-10-14）

## 症状（用户实测）

1. 左键手牌不再弹出动作按钮；
2. 连锁界面里除了第一张，其余候选显示卡背；
3. 放/发动场地魔法之后，对局像卡死一样不再推进。

## 根因：host 把 `SelectChain`(16) 的帧长算多了 1 字节，解析又少读了/多读了 1 字节

`ocgcore` 源码（`MDPro3-main/Tools/YGO Classes/ocgcore/playerop.cpp` 的 `field::select_chain`）
写出来的格式是「头 13 字节（含功能号）+ 每个候选 13 字节」，但本机 `ocgcore.dll`（v1.4.7 / 协议 4962）
实际发出来的**不一样**，必须按实测：

| 部分 | 字节 | 说明 |
| --- | --- | --- |
| 功能号 | 1 | 16 |
| 玩家 | 1 | 要回答的玩家 |
| 候选数 | 1 | n |
| spe_count | 1 | |
| hint_timing[player] | 4 | |
| hint_timing[1-player] | 4 | |
| 每个候选 | **14** | 标志(1) + 强制标志(1) + 卡号(4, 项内 +2) + 位置(4, 项内 +6) + 描述(4) |

也就是说：**帧长 = 12 + 14 × 候选数**，头里没有独立的 `forced` 字节（强制标记在候选里）。

佐证（可复验）：

- 头 12 / 候补 14 / 卡号偏移 +2：D 盘 `tools/duel_host/*.jsonl` 里 2024 个多候选窗口、
  3307 个候选（卡号能在 `cards.json` 查到 + 位置合法）**全部命中**；偏移 +1 则 0 命中。
- 客户端实战日志里 272 个窗口、416 个候选，同样是 +2 全对、+1 全错。
- 帧长：n=0→12、n=1→26、n=2→40、n=3→54，与 `12 + 14n` 完全一致。

### 两个错位点

1. `ClassicMessageSplitter.MessageLength` 的 `case 16` 写的是 `13 + chains * 14`。
   当这是这批消息的最后一条时，`need > 剩余长度` 会退回"整包收下"，看上去没坏；
   一旦后面还有消息，就会**吃掉下一条消息的首字节** → 后续消息全部错位的连锁反应
   （卡号错 → 显示卡背、移动/连锁内容错、偶发"Unknown or truncated core message"）。
2. `DuelHostProgram.DuelCore.BuildPrompt` 的 `case 16` 多读了一个"强制字节"，
   于是读候选时位置偏了 1：26 字节的窗口刚好卡在守卫条件 `Position + 14 <= Length`（13+14 > 26）
   上 → `cards: []` → 模组认为"没有候选"而不弹连锁窗 → **对局停在那里不动**。
   这直接解释了"放场地魔法就卡死"：
   `Card action: (0,2,3) kinds=activate` → `SelectChain(count=1)` → 解析失败 → 停住
   （日志 `2026-10-14-14`、`-3`、`latest.log` 三次都停在这里，last 是 06:03 那次，直到玩家退出）。

### 顺带修掉的同类问题：`SelectIdleCmd`(11) 帧长也多 1 字节

`case 11` 曾经为了"补玩家字节"多跳了 1 字节，实际格式是
`功能号 + 玩家 + 6×列表 + to_bp + to_ep + can_shuffle`，即 **6 + Σ(1+7n) + (1+11n)**。
实机报文（36 / 74 / 78 / 89 字节四种长度）核对全部吻合。多算 1 字节的后果和上面一样：
每条 IdleCmd（最频繁的消息）都会吞掉下一条消息的首字节。

## 改了什么

host（`tools/duel_host`）：

- `ClassicMessageSplitter.cs`：`case 16` 帧长改 `12 + chains * 14`；`case 11` 去掉多余的
  `Skip(1)`（保留玩家字节）；自检样例换成真实报文（26 / 40 / 36 / 89 字节四条）。
- `Program.cs` `case 16`：头只读 `player / count / spe_count / hint(4+4)`，
  候选按 14 字节读（卡号 +2、位置 +6），`forced` 取候选里的强制位；
  `summary` 增加 `cards=` 条数，`options` 按候选数列出 `Chain #k`；
  帧被截断时"有多少读多少"，不再整条提示作废。

模组（`mod/src/main/java/net/mdpromod`）：

- `DuelHostProcess`：新增 `chainChoiceCount()` / `chainChoiceHasUnknown()` / `promptFunction()`；
  `chainChoicePrompt()` 改成"**候选数 > 0 就算窗口**"，`chainChoiceCodes()` 按候选数补 0
  —— 就算哪条候选没解析出来，玩家仍能看到窗口并作答，**不会再静默停住**。
- `DuelSessionManager`：
  - 连锁窗口里有认不出的卡时，聊天栏说明"仍可按下标选择或放弃"；
  - 核心问落点时，聊天栏直接列出合法落点（"自己场地魔法区"这类），左键点绿色格子即可；
  - 点卡却没有可用动作时，把原因写进聊天栏（不是一闪而过的动作条）：
    "现在不是你的决策点 / 有连锁窗口要先答完 / 核心当前没给出候选"；
  - 新增兜底提醒：核心在等一个模组还没有界面的窗口（21 排序连锁 / 22 指示物 / 23 合计 / 14x 宣言）时，
    聊天栏说清函数号与摘要，不再静默。

## 验证

| 验证 | 命令 / 产物 | 结果 |
| --- | --- | --- |
| 切分器自检 | `duel_host.dll --test-splitter` | 9 条真实报文全部通过 |
| 整局帧切分 | `duel_host.dll --smoke-ai Data/Windbot/Decks/AI_BlueEyes.ydk --smoke-seed 4962` | 3970 行、**0 条截断警告、0 个错误**、走到 `MSG_WIN`（修复前同一命令 4 个错误并中断） |
| 连锁候选正确性 | `tools/duel_host/select_chain_probe.py` → `docs/select-chain-probe-2026-10-14.json` | 7533 条消息、**303 个连锁窗口 / 950 个候选全部解析正确**（卡号都能查到、位置合法、条数与 count 一致）、`bad_chains=[]`、正常结束 |
| 模组构建 | `cd D:\MDProMod\mod; .\gradlew.bat build --offline` | BUILD SUCCESSFUL，`mdpromod-0.1.0.jar` 2,332,807 字节 |
| 部署 | 复制到客户端 `mods` | SHA-256 `233E8D28…C442A5` 一致；旧包备份在 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0628` |

## 还没做 / 下一步

- 上面 21 / 22 / 23 / 14x 这四类窗口仍然**没有界面**：现在只是"说出来"，玩家还是没法回答。
  建议下一步给 host 加"无界面窗口 N 秒后按最小代价代答"的兜底（`AutoAnswer` 已经能处理它们）。
- 游戏内实机复验（左键手牌弹按钮、连锁界面卡图、场地魔法落点）需要你启动客户端跑一局确认视觉。
