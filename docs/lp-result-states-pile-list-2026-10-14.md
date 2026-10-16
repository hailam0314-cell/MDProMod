# LP 实时显示 / 决胜按钮两态 / 卡堆右键看全部（2026-10-14 晚）

## 1. LP 一直显示 8000

**原因（两个叠在一起）**：

1. 模组只在 `LpUpdate` 这条消息里更新 LP，而 host 给功能号 94 起的名字是 **`LpUpdate`**，
   模组里写的却是 `"LPUpdate"`（大小写不一致）→ 永远命中不了。
2. 更关键的是**核心扣血根本不发 94**：`Damage`(91) / `Recover`(92) / `PayLpCost`(100)
   载荷是 `玩家(1) + 数值(4, 小端)`，是**增量**；只有 `Duel.SetLP` 才发 94（绝对值）。
   实战日志里 5 条 `Damage`（`5B0064000000` = 玩家0 受 100 等）一条都没被处理。

**改动**（`DuelHostProcess.applyStats`）：

- `Damage` / `PayLpCost` = 减、`Recover` = 加、`LpUpdate` = 设为该值；全部 clamp 到 `[0, MAX]`。
- 加 `playerByte()` / `leU32()` / `clampLp()` 三个小工具，并顺手把每次 LP 变化写进日志
  （`LP Damage: 7900 / 5600`），以后排查一眼就能看到。

**验证**：拿日志里真实的 5 条 `Damage` 重放 → p1 = 7900（受 100）、p2 依次 5600 / 2600 / 800 / 0；
另用构造的 `Recover`(+256) 与 `PayLpCost`(-500, 收到 0 截断) 各验一条，行为正确。

## 2. 右下角按钮加「胜利 / 失败」两态 + 重开流程

**新状态**：`DuelFieldEntity.DATA_RESULT`（0 无 / 1 本侧胜 / 2 本侧负）。宿主一结束
（`host.isEnded()` 或进程已退出）且本局确实开过（`host.duelActive()`），模组就结算一次：
胜利 → 金色 `victory`，失败 → 灰色 `defeat`，并顺手清掉阶段提示、聊天栏给一句提示。

**贴图**：`assets/duel_controls/build_control_assets.py` 里加了两套配色
（`victory` 金色 / `defeat` 灰暗），生成 `control_confirm_victory.png`、
`control_confirm_defeat.png` 并同步进模组资源目录；渲染器按状态取图，文字分别是「胜利」「失败」。
同一张键在对面那一侧按镜像显示（双方各看各的胜负）。

**重开流程**（`DuelSessionManager.resetForNewDuel`）：决胜状态下**右键**这张键 →

- 停掉宿主进程、清掉场上所有卡/手牌/连锁（`stop()` → `clearVisuals()`）；
- `gameStarted=false`、结果清零、双方 LP 回 8000、阶段回 DP、牌堆计数复位；
- **本侧卡组留着**（按钮回到绿色「准备」），**对面卸掉**，这样点绿色按钮时
  `ensureAiDeck` 会重新补上 AI 卡组（"人机会自动准备"）→ `startFromField` 开新一局；
- 想换卡组：决胜状态或准备状态都可以直接用卡盒右键决斗盘（`bindDeck` 会覆盖）。

阶段键在决胜状态下强制 `disabled`（`controlState` 里 `result != 0` 直接返回）。

## 3. 右键墓地 / 额外 / 除外只弹一张卡

**原因**：墓地、除外、表侧额外里的卡在世界里是 **`DuelCardEntity` 实体**，且都堆在同一个格子上
（只差 0.008 的高度）。右键时射线先打到最上面那张卡 → 走了"单卡详情"分支，
真正列全表的 `openPile`（`PileScreen`）根本没机会执行。

**改动**：`DuelFieldEntity.interact` 里先看射线打中的那张卡属于哪个格子——只要它在
`*_graveyard` / `*_banished` / `*_extra` 里，就直接开整堆清单（右键格子本身的行为不变）。
同时 `openPile` 现在带**控制者**参数：

- 动作按钮（特殊召唤 / 发动）只对自己这一侧出现（核心的候选列表也只写自己）；
- "全部卡"按控制者取：额外卡组用绑定的 `DeckData.extraDeck`，墓地 / 除外用演出层的
  `codesIn(side, location)`（以前 side 写死 0，看对面的堆会串到自己的）；
- `PileScreen` 里加了「全部卡」标题和空堆提示，悬停出卡名、滚轮 / 翻页按钮。
  另外**左键点「全部卡」里的某一格会弹大图详情**（卡面 + 效果文本），
  这样右键看整堆、左键看单张细节，两种需求都在。

## 验证与产物

| 项 | 结果 |
| --- | --- |
| 模组构建 | `cd D:\MDProMod\mod; .\gradlew.bat build --offline` → BUILD SUCCESSFUL（含单元测试） |
| 贴图生成 | `python build_control_assets.py` → 12 张（新增 victory/defeat），已同步到 `mod/src/main/resources/assets/mdpromod/textures/entity/` |
| LP 重放 | 见上（真实 + 构造消息） |
| 产物 | `mdpromod-0.1.0.jar` 2,338,814 字节，SHA-256 `02E0DF23…AF9152E`，已复制到客户端 mods；旧包备份 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0712` |

## 需要你在游戏里确认

1. 挨一下伤害后左下角 LP 是否跟着变（日志里会出现 `LP Damage: …`）；
2. 打完一局后右下角按钮是否变金色「胜利」/ 灰色「失败」，右键是否回到绿色「准备」，
   点绿键（AI 自动补卡组）能否开新一局；
3. 右键墓地 / 额外 / 除外是否开出整堆清单（含"全部卡"网格）。

## 备注

- 用 `/mdpromod stop` 手动停局不会写胜负（只有正常分出胜负或宿主进程退出才结算）；
  如果想要"手动停 = 失败"，可以再加。
- 一直没有界面的 `SortChain`(21) / `SelectCounter`(22) / `SelectSum`(23) / 宣言(14x) 仍未做。
