# MDPro 的人机（WindBot）调研与打通（2026-10-13）

## 结论
MDPro 的人机就是 **WindBot**（C# 写的 YGOPro 客户端 AI），并且是**在游戏进程内直接跑**的：
`MDPro3.Servant.SoloSelector.StartWindBot(command, ip, port, password, lockHand, delay)`
把命令串拼成 `Name=… Deck=… Dialog=… Host=… Port=… HostInfo=` 之类的参数，
然后 `new Thread(() => WindBot.Program.Main(args)).Start()` —— 它作为「网络客户端」连到本机
ygopro 服务器（`ygoserver.dll`）上打牌，所以它天然支持单人、房间对战、观战席位。

## 现成资料
- 人机名单：`Data/locales/<语言>/bot.conf`（中文这份 55 组，如「随机-非常简单 / 简单 / 一般 / 普通」，
  以及「琪露诺」「谜之剑士LV4」等具名 bot）；随机池用 `Random=AI_LV1 / AI_LV2 / AI_LV3 / AI_LV4 / AI_WILD / AI_ANTI_META`。
- AI 卡组：`Data/Windbot/Decks/AI_*.ydk`（57 副）。
- 台词/性格：`Data/Windbot/Dialogs/*.json`（157 份，含 `cirno.zh-CN`、`default` 等）。
- 客户端协议版本：`MDPro3.Config.ClientVersion = 4962`（0x1362）。
- 我们自己的工具链：
  - `tools/windbot_standalone`：从游戏里抽出来、可独立运行的 WindBot（.NET 8）。
  - `tools/mdpro3_server_launcher`：调用 `ygoserver.dll` 起房间（注意 server-args 必须是一整串）。
  - `tools/ygo_tcp_probe`：线协议探针（可起服务器、可当客户端）。

## 这一轮修掉的两个坑
1. **sqlite 绑定崩**：`NamedCardsManager` 原本用游戏自带的 `Mono.Data.Sqlite.dll`，在 .NET 8 下
   `TypeLoadException: get_Database ... does not have an implementation`。
   → 新增 `NamedCardsJson.Load()`，直接读我们生成的 `cards.json`（顺带给生成器补了 `setcode` 字段，
   AI 的「系列/字段」判定需要它）。启动日志现在会打印
   `Loaded 15019 card definitions from cards.json`。
2. **协议版本不一致**：客户端版本 4962 的 `MSG_SELECT_CHAIN` 取消了包头那个独立的 forced 字节，
   改成每张卡一个标志位；bot 的旧解析会读越界（`EndOfStreamException`）。
   → 按反编译源码 `MDPro3.Duel.DuelMessage.GameMessage_SelectChain` 修正了 `OnSelectChain`。
3. 顺手给 bot 加了 `Debug=true` 时的逐条对局日志（`STOC xxx` / `MSG yyy`），方便以后排查。

## 验证：两个 AI 真打了一整局
命令（staging 目录下）：

```powershell
# 1. 起房间（server-args 必须是完整一串）
dotnet mdpro3_server_launcher.dll --root <staging> --server-args "17952 -1 5 0 F F F 8000 5 1 180 0"
# 2. 两个 bot 各自连上，房主 bot 会自动开局
dotnet windbot_standalone.dll Name=BotA Deck=AI_BlueEyes Dialog=cirno.zh-CN Version=4962 Debug=true Host=127.0.0.1 Port=17952
dotnet windbot_standalone.dll Name=BotB Deck=AI_Albaz   Dialog=cirno.zh-CN Version=4962 Debug=true Host=127.0.0.1 Port=17952
```

结果：对局从 03:02:19 打到 03:02:38，两边日志各约 5700 行、**零异常**，结尾是
`MSG Battle → MSG Damage → (1 got damage, LifePoint left = 0) → MSG Win → STOC DuelEnd`
（另一边是 `0 got damage ... LifePoint left = 0`），说明「WindBot 能在这台机器上完整对局」。
证据：`staging\bot3A.out` / `staging\bot3B.out`。

## 接进模组的两条路线
**路线 A（推荐，先做这个）**：把 WindBot 作为库嵌进我们的 `tools/duel_host`。
- 人类玩家（p0）继续走现有 JSON 协议（HUD、手牌实体、场地统计全部照旧）。
- AI（p1）在 host 进程内：把引擎消息喂给 `GameBehavior.OnPacket(BinaryReader)`，
  再从它发出的 CTOS 响应（`CtosMessage.Response` → `SetResponseInteger/Bytes`）回填给引擎。
- 需要给 YGOSharp 的 `YGOClient` 加一个「内存传输」变体（源码在我们自己手里，改起来干净）。
- 好处：MC 侧零改动，现有测试链路不破坏；单人 vs AI 立刻能用。

**路线 B（以后做联机时再上）**：整体换成「ygoserver + 我们当客户端 + windbot 当客户端」，
用 `ygo_tcp_probe` 的客户端实现替换 host 的引擎层。
- 好处：完全复刻 MDPro 的形态，天然支持多人与观战、断线重连；
- 代价：host 要大改，现有统计/手牌协议要重新对接。

## 路线 A 落地（已完成）
**WindBot 已经作为库编进 `tools/duel_host`**：同一个进程里，人类玩家（p0）继续走 JSON stdio 协议，
AI（p1）直接吃引擎消息、直接回填应答。

实现要点：
- `duel_host.csproj` 直接编译 `windbot_standalone/src/Windbot` + `src/YGOSharp` 的源码；
  宿主入口类改名 `DuelHostProgram`，避免和 WindBot 自己的 `Program` 撞名。
- `YGOSharp.Network.BinaryClient` 增加**本地内存传输**：`LocalTransport` 打开后 `Send` 只入队；
  `GameClient.StartLocal()` 不走 TCP，`Feed(byte[])` 直接把 STOC 包喂给 `GameBehavior.OnPacket`。
- `WindBotAi`（新文件）负责：初始化 WindBot 运行时（`Config.Load` + `InitDatas`，卡表来自 cards.json）、
  合成开局握手（JOIN_GAME / TYPE_CHANGE / HS_PLAYER_ENTER / HS_PLAYER_CHANGE / DUEL_START）、
  显式 `ClientField.Init(deck, extra)`（直接驱动引擎时收不到那步）、
  把每条引擎消息包成 `STOC_GAME_MSG` 喂给 AI，并把它的 `CTOS_RESPONSE` 转成
  `SetResponseInteger` / `SetResponseBytes` 回填。
- 提示路由：`NeedsResponse` 且消息里的玩家 == AI 座位 → 由 AI 回答（输出 `{"type":"ai"}` 事件）；
  否则照旧发给人机客户端。MSG_RETRY 连续 5 次会直接报错停局，避免死循环。
- MC 侧 `DuelHostProcess.sendStart` 现在默认带上 `ai/aiSeat/aiName/aiDeck/aiDeckFile/aiDialog`，
  对手卡组走 `Data/Windbot/Decks/<name>.ydk`（默认 `AI_BlueEyes`，可用
  `-Dmdpromod.aiDeck=...` 覆盖）。

## 路线 A 验证
`tools\duel_host\ai_smoke.ps1` / `--smoke-ai` 无头跑了一整局（人类侧用 auto 应答、p1 用真 WindBot）：

- 日志 `staging\ai_smoke6.out`：`lines=1249`、**errors=0**、**重试保护 0 次**；
- AI 决策 160 次：`SelectChain 114 / SelectIdleCmd 23 / SelectCard 22 / SelectPlace 1`；
- 结局 `{"type":"message","function":5,"name":"Win","winner":0}` —— 完整打到分出胜负。

## 下一步
- 难度/卡组选择暴露给玩家（bot.conf 的四档随机池 + 具名 bot）。
- AI 台词（Dialogs）通过聊天栏显示。
- AI 的召唤/盖放消息映射成场上卡片实体（现在场上还是手动放卡）。
