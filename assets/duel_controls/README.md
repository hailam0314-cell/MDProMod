# 决斗场地控制条：按钮与 LP 面板

场地近半场那条控制带（生命值 + 6 个阶段键 + 开始决斗键）的模型、贴图和状态规格。
坐标沿用 `assets/duel_field` 的约定：16 单位 = 1 方块，Y 相对决斗场地实体原点，
场地表面在 y = 0.16。

## 文件

- `build_control_assets.py`：生成全部贴图与 `control_spec.json`，并把贴图复制到
  `mod/src/main/resources/assets/mdpromod/textures/entity/`。
- `control_spec.json`：几何、UV 分块、状态表的唯一来源，Blockbench 母版与 Java
  渲染器都按它对齐。
- `textures/`：8 张贴图（下详）。
- `mdpromod_duel_controls.bbmodel`：Blockbench 母版（含 8 组控制件 + `ref_mat`
  参照垫，参照垫只用于在编辑器里看位置，`export` 已置 false）。
- `shot_bar.png` / `shot_close.png` / `shot_over_field.png`：预览截图。
- `preview_sheet.png`：贴图速览。

## 几何（模型单位）

| 部件 | 占地 | 高度 | 说明 |
| --- | --- | --- | --- |
| 阶段键底座 | 20 × 10 | 0.2 → 1.0 | 深色金属，四角螺钉 |
| 阶段键帽 | 17.2 × 7.8 | 1.0 → 1.8 | 顶面按状态换色，文字画在 1.82 |
| 开始键底座 | 42 × 12 | 0.2 → 1.0 | 同上 |
| 开始键帽 | 38.8 × 9.4 | 1.0 → 1.8 | 灰色 / 绿色 / 蓝色三态 |
| LP 面板 | 42 × 12 | 底盘 0.2 → 1.1 | 顶面是屏幕贴图 |
| LP 围栏 | 宽 1.6 的四根条 | 1.1 → 1.8 | 左右内缩、上下通长 |

LP 数字画在屏幕中央（y = 1.104），文字 5.5 单位高，最宽 30 单位，超出自动缩小。
所有控制的“帽顶”都在 1.8，也就是 `duel_field_layout.json` 里 `center[1]` 的取值；
点击判定用的就是这一层平面。

## 贴图与状态

每张贴图都是三段分块：顶面 / 侧面 / 底面，UV 在 `control_spec.json` 的
`tiles` 字段里（像素坐标，等于 UV 空间）。

| 贴图 | 尺寸 | 用在哪 | 状态 |
| --- | --- | --- | --- |
| `control_frame` | 64 × 48 | 所有底座与 LP 围栏 | 固定 |
| `control_phase_disabled` | 160 × 96 | 阶段键帽 | 灰色：不能按 |
| `control_phase_available` | 160 × 96 | 阶段键帽 | 绿色：可以按 |
| `control_phase_current` | 160 × 96 | 阶段键帽 | 蓝色：当前阶段 |
| `control_confirm_disabled` | 320 × 100 | 开始键帽 | 灰色：本侧没卡组 |
| `control_confirm_ready` | 320 × 100 | 开始键帽 | 绿色：本侧已放卡组（“准备”） |
| `control_confirm_started` | 320 × 100 | 开始键帽 | 蓝色：两侧都有卡组（“游戏中”） |
| `control_confirm_turn_mine` | 320 × 100 | 开始键帽 | 绿色：开局后本侧回合（“我的回合”） |
| `control_confirm_turn_opponent` | 320 × 100 | 开始键帽 | 红色：开局后对方回合（“对方回合”） |
| `control_lp_screen` | 336 × 100 | LP 底盘顶面 | 固定，左侧烧有 LP 标记 |

按钮上的文字（抽卡/准备/主要1/战斗/主要2/结束、开始决斗/准备/游戏中）由 Java 用
原版字体画在键帽顶面，不烘进贴图，改文案不用重新出图。

开局之后开始键变成**回合指示灯**：本侧回合绿色「我的回合」，对方回合红色「对方回合」，
状态由 `DuelFieldEntity.controlState` 按 `DATA_TURN_SIDE` 决定。五态速览图：
`preview_confirm_states.png`。

## 重建流程

```powershell
$py = 'C:\Users\<你>\.codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $py D:\MDProMod\assets\duel_controls\build_control_assets.py     # 贴图 + spec + 复制进 mod
& $py D:\MDProMod\tools\blockbench_mcp\push_controls.py tex        # 贴图推进 Blockbench
& $py D:\MDProMod\tools\blockbench_mcp\push_controls.py geom       # 按 spec 重建几何
& $py D:\MDProMod\tools\blockbench_mcp\export_project.py `
      D:\MDProMod\assets\duel_controls\mdpromod_duel_controls.bbmodel
```

改尺寸时只改 `control_spec.json`（或脚本里的常量）再跑上面两步，Blockbench 与
Java 渲染器的常量（`DuelFieldRenderer` 顶部的 `*_TILES` / `*_CAP_*`）同步修改。
