# 决斗场地按钮：模型、贴图与状态（2026-10-14）

## 这一轮修了什么

上一版控制条只有文字，颜色完全不出现。原因很直接：`DuelFieldRenderer` 引用的
`control_disabled / control_available / control_current / control_display / control_lp`
五张贴图**在资源目录里根本不存在**，方块只能画成缺省贴图，所以场上只剩悬浮的文字。

这一轮把控制条做成真正的三件套（底座 + 键帽 + LP 屏），贴图、状态和交互一起补齐。

## 按钮与状态

每个玩家一侧（P1 在 z = +102，P2 镜像到 z = −102）都有：

| 控制 | 位置 | 尺寸（单位） | 灰色 | 绿色 | 蓝色 |
| --- | --- | --- | --- | --- | --- |
| 生命值 | x = ±95 | 42 × 12 | — | — | — |
| 抽卡 / 准备 / 主要1 / 战斗 / 主要2 / 结束 | x = ∓55 … ∓55 | 20 × 10 | 不能按 | 可以按 | 当前阶段 |
| 开始决斗 | x = ±95 | 42 × 12 | 本侧没卡组 | 本侧已放卡组「准备」 | 两侧都有卡组「游戏中」 |

- 阶段键全部统一成 20 × 10、帽顶 1.8 单位，按状态换贴图：灰 = 不可按，
  绿 = 可按，蓝 = 当前阶段。点击绿色键会切换到这个阶段；点蓝色键只提示“已经是
  该阶段”；灰色键提示当前不能切换。
- 开始键同样按状态换贴图：点灰色键提示先放卡组；点绿色键会把预设的 WindBot 卡组
  补到远半场（单人流程），两侧都有卡组时立即开局；**两侧玩家各自放下卡组后不需要
  再点按钮，会自动开局**并变成蓝色的「游戏中」。
- 生命值面板是长方体托盘：底盘顶面是屏幕贴图，四根围栏压边，数字画在屏幕正中，
  随 `DuelFieldEntity` 的同步字段实时变化（数值由 DuelHost 每 tick 推过来）。

## 资源与代码位置

- 资源母版与生成脚本：`assets/duel_controls/`（见该目录 README）。
- 运行时贴图：`mod/src/main/resources/assets/mdpromod/textures/entity/control_*.png`。
- 渲染：`DuelFieldRenderer.drawControl / drawPushButton / drawLifePanel / drawFlatText`。
- 状态与交互：`DuelFieldEntity.controlState / handleConfirmClick / interact`。
- 布局：`data/mdpromod/duel_field_layout.json` 的 `controls` 段（与
  `assets/duel_field/duel_field_layout_v2.json` 同源，阶段键 y 已统一为 1.8）。

## 游戏内验证步骤

```powershell
cd D:\MDProMod\mod
.\gradlew.bat runClient
```

1. `/mdpromod field place` 放下决斗盘 —— 控制条先是灰色「开始决斗」。
2. 用卡盒右键自己这一侧放下卡组 —— 本侧按钮变绿「准备」，生命值显示 8000。
3. 点绿色按钮（或让第二名玩家也放下卡组）—— 按钮变蓝「游戏中」，决斗开始。
4. 开局后轮到自己时，阶段键：灰色不可按、绿色可按、蓝色是当前阶段。

## 还没做

1. 阶段切换目前只改本地状态，还没有通过 `SelectIdleCmd` 的 prompt 回给 ocgcore；
   真正的阶段推进要等“把 Duel Host 的 prompt 接到按钮”这一步。
2. 按钮没有悬停/按下的瞬时反馈（可以后续加按压下沉 0.1 单位 + 音效）。
3. 控制条只做了 P1 母版，P2 由渲染器按 180° 旋转复用，没有单独的 Blockbench 分组。
4. 联机时的隐藏信息过滤仍缺（手牌、卡组内容）。
