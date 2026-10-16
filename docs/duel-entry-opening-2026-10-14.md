# 进入决斗：开局发牌演出（2026-10-14）

## 目标序列

1. 两侧都放好卡组 → 右下角变蓝「游戏中」；
2. 双方初始手牌（各 5 张）**从各自的卡组区飞出来**，落到世界里的手牌位；
3. 牌全部落位之后，才进入回合：右下角切成「我的回合 / 对方回合」，阶段键解除锁定。

## 链路与实现

| 环节 | 位置 | 说明 |
| --- | --- | --- |
| 引擎消息 | `Draw(90)` | 开局每个座位一条 `count=5` 的抽卡消息，带 5 个卡号 |
| 事件层 | `DuelVisualState.draw()` | 每张牌产出一个 `draw` 事件：`from` = 卡组顶，`to` = 手牌第 N 格 |
| 演出层 | `DuelAnimationScene.play()` | `draw` 飞行 520ms；相邻抽卡间隔 120ms 叠成瀑布 |
| 网络 | `ModNetwork.DuelAnimation` | 视角内玩家都会收到；对面 5 张以卡背朝上飞 |
| 客户端 | `ClientAnimBridge` + `ClientDuelAnimationRenderer` | 生成飞行卡片精灵，并在此期间隐藏目的地实体（`hideEntityId`） |
| 落点 | `DuelAnimationScene.handPoint()` | z = ±7.75 方块、y = 1.375，间距随张数收紧，与 `hand-entities` 文档一致 |

## 新增：开局演出窗口

`DuelAnimationScene` 现在带一个开局窗口：

- `initialize()` 时 `opening = true`；
- 播放到第一条 `draw` 后记录 `openingDrew`；
- **初始手牌全部落位**（队列清空且当前飞行结束）或兜底 8 秒超时后，`opening = false`。

这期间 `DuelSessionManager` 会：

- 让决斗盘保持 `setOpening(true)` —— 右下角显示蓝色「游戏中」，阶段键整排锁住
  （`DuelFieldEntity.controlState` 里 `isOpening()` 直接返回 disabled）；
- 暂时不推手牌 HUD（`HandSync` 以空手牌 + inactive 发送），牌落地后才亮手牌。

演出结束后一切照旧：`promptPlayer / toBattle / toEnd / toMain2` 正常下发，
回合指示灯（绿「我的回合」/ 红「对方回合」）开始工作。

## 验证

- 事件层：`tmp\draw-smoke`（`DrawSmoke.java`）用**真实开局消息**
  （`5A000500...` / `5A010500...`）驱动 `DuelVisualState`，输出 10 个 `draw` 事件，
  起点 `controller=s, location=1`（卡组），终点 `location=2`（手牌）逐格递增，
  `hand0=5 hand1=5` ✓。
- 编译与产物：`gradlew build` 通过，`mdpromod-0.1.0.jar`（2,228,945 字节）已复制到
  `D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods\`（哈希校验一致）。
- 未做：游戏内实机截图（需要你启动客户端确认视觉节奏）。

## 还没做

- 开局抽卡的音效与屏幕提示（可以复用原版音效）。
- 对手侧的抽卡目前只是卡背飞过去，没有额外的「对手摸牌」提示。
- 手牌 HUD 与世界手牌实体同时存在，属于早先两种方案并存，是否二选一待定。

## 附：开局"什么都没发生"的真正原因（2026-10-14 修）

实机日志里不是没有动画，而是**决斗根本没开起来**：

```
Duel Host error: Could not find file 'D:\MDProMod\runtime\decks\鐝犳唱.ydk'
```

`鐝犳唱` 是 `珠泪` 的 UTF-8 字节被按 GBK 解码的结果：模组用 UTF-8 把中文卡组路径
写进宿主进程的 stdin，而 `Console.ReadLine()` 用的是系统默认代码页（中文 Windows
上是 GBK）→ 路径读错 → `AddDeck` 抛错 → 没有发牌、没有飞行。

修复：

1. `duel_host` 改为 `new StreamReader(Console.OpenStandardInput(), UTF8Encoding(false))`
   显式按 UTF-8 读取 stdin；
2. 宿主开局成功时新增 `{"type":"started"}` 事件，模组据此区分"进程活着"和"真的开了一局"；
3. `DuelSessionManager.start` 允许在"进程活着但没建起对局"时重开，不再卡在
   `A duel is already active`；开局失败会退回「准备」状态并把原因显示给玩家，可以直接重试。

验证：`tools/duel_host/deck_path_probe.py` 用真实中文路径
`D:\MDProMod\runtime\decks\珠泪.ydk` 启动宿主，返回 `started`（修复前是
`Could not find file`）。
