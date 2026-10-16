# 多人联机打牌：现状、模拟结果与缺口清单（2026-10-16）

## 一、这轮"双座位模拟"跑了什么

用宿主自带的烟测模式跑**一整局两个座位各自独立决策的对局**（两个座位都由 WindBot 驱动，
但走的是和真人完全一样的提示/应答通道：每个提示带 `seat`，每张应答都送到引擎）：

| 局 | 对局 | 结果 | 座 0 决策 | 座 1 决策 | 对局消息 | 连锁 | 错误 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 珠泪(座0) vs 青眼(座1) | 座 0 获胜 | 199 | 96 | 3,979 | 35 次 Chaining/Chained | 0 |
| 2 | 烙印/阿尔巴兹(座0) vs 相剑(座1) | 座 0 获胜 | 187 | 114 | 4,075 | 30 次 Chaining/Chained | 0 |

两局都跑到 `ended`（分出胜负收尾），**零错误**。小坑：`--smoke-ai-deck0` 的卡组名必须真实存在
（我第一次写 `AI_Branded` 直接开不了局，正确的是 `AI_Albaz`）；清单就是 `Data/Windbot/Decks/AI_*.ydk`。

两局的原始 JSONL：`tmp/pvp_sim_1.jsonl`、`tmp/pvp_sim_2.jsonl`（每行一条事件，
`type` 里有 `ai`（某座的决定）、`message`（引擎消息）、`ended` 等；`seat` 字段标明座位）。
复现命令（在 `D:\MDProMod\tools\ygo_tcp_probe\staging` 下）：

```powershell
tools\duel_host\bin\Release\net8.0\duel_host.exe `
  --smoke-ai Data/Windbot/Decks/AI_Swordsoul.ydk `
  --smoke-ai-deck0 Data/Windbot/Decks/AI_Albaz.ydk `
  --smoke-seed 777 --root .
```

**这证明了什么**：引擎与宿主完全支持"两个座位各自决策"——座位号在提示里、应答不分座位地送进引擎、
双方各有一套卡组/LP/墓地，胜负按座位判。**没证明什么**：模组侧把提示分发给两个真人玩家、
两人的界面互不串台、隐藏信息不泄露——这些现在都还没有代码。

## 二、为什么现在还不能真人打：模组侧的缺口

`DuelSessionManager` 现在是"**一个 owner + 一个人机**"的结构：

| 现状 | 位置 | 要让两个真人打，需要 |
| --- | --- | --- |
| 只有 `ownerId` 一个玩家；所有提示都发给 `server.getPlayerList().getPlayer(ownerId)` | `tickField` 里每一处 `sendTo(chooser/decider/hintPlayer/sorter/pilePlayer…)` | 按**提示的座位**选玩家：`host.promptPlayer()` → seat 0 / seat 1 各自的玩家 |
| 开局要求远半场必须放**人机卡组**（否则不让开始） | `startFromField` / `DuelFieldEntity` 的按钮分支 | 允许远半场放**第二个玩家的卡盒**，并把 seat1 绑到那个玩家 |
| 阶段键/确认键"谁在近半场就能按"（`ownsControl()` 按几何位置判） | `DuelFieldEntity` | 追加"只有当前该操作的玩家能按"（核心给出 `promptPlayer()`） |
| 认输/胜负绑在 owner 上（`ownerId` → `resultPlayer`） | `tickField` 收尾分支 | 按座位结算，把结果发给对应玩家 |
| `fieldDecks` 已经是 `Map<side, DeckData>` ✓ | `DuelSessionManager` | 底层数据结构已经支持两侧不同卡组，不用改 |
| 提示/演出大量 `sendNear(...)` 广播 | `ModNetwork.sendNear` 调用点 | 逐条审：给 A 的提示不能捎带 B 的手牌/盖卡信息（隐藏信息过滤 `visibleCode()` 已有基础） |
| 宿主侧 | `tools/duel_host/Program.cs` | **已经就绪**：提示带 `seat`、`aiSeat` 可配、`Surrender(seat)`、无 AI 时 `auto=false` 会等 stdin 应答 |

## 三、建议的落地顺序

1. **座位绑定**：远半场的卡盒改成可以放"另一个玩家的卡盒"，开局时记录两个玩家（`seat0Id` / `seat1Id`）。
2. **提示路由**：把 `tickField` 里每个 `sendTo(X)` 换成 `sendTo(playerForSeat(host.promptPlayer()))`；
   收到的应答校验来源（不是被问的人就拒绝，避免替对手作答）。
3. **准备流程**：双方各自放卡盒 → 各自按绿色按钮「准备」→ 两边都准备才开始（现在是一边按就开）。
4. **隐藏信息复核**：把所有 `sendNear` 广播逐条过一遍，凡是包含卡号/卡名的，按观看者过滤或改成 `sendTo`。
5. **实机验证**：本地起一台服务器（`gradlew runServer`，我可以帮你起），两个客户端连上去，
   各摆一块盘、各放一个卡盒，打一局完整对局——这一步才真正验证"联机打牌"。

## 四、现在就能做的两种"模拟"

| 方案 | 需要什么 | 覆盖什么 |
| --- | --- | --- |
| **A. 引擎层双座位**（本轮已跑 ✓） | 只需本机 | 两座位决策、连锁、胜负判定 |
| **B. 双客户端本地服务器** | 你开两个 Minecraft 客户端（同机两个游戏目录即可），我起服务器 | 两个玩家各自摆盘、看彼此演出、资源加载、服务器稳定性（**但这一版两人还不能互打**，因为 PvP 未实现） |
| **C. 真·联机打牌** | 先按上面 1–4 实现 PvP，再做 B | 全部 |
