# MDPro 默认卡盒模型

这个模型参考 MDPro 的默认卡盒 `1080001` 制作。默认卡盒是红色的 **DUELIST CARD CASE**，盒盖正面有白色字标和箭头，盒身和侧面有白色双线边框，顶部有阶梯式倒角，打开时盒盖向后翻起。

## 来源与提取

默认卡盒编号：

```text
1080001
```

对应的官方 AssetBundle：

```text
icon_assets_deckcase0001_l_hd.bundle
icon_assets_deckcase0001_open_l_hd.bundle
icon_assets_deckcase0001_l_reverse_hd.bundle
```

官方闭合、打开和背面参考图已经提取到 `reference/`：

- `DeckCase0001_L.png`
- `DeckCase0001_Open_L.png`
- `DeckCase0001_L_reverse.png`

`card_case_reference.png` 是这三张官方参考图的合并对照。

## 文件

- `card_case_v1.bbmodel`：可编辑卡盒模型，包含盒身、盒盖和内部卡组。
- `textures/card_case_atlas.png`：红色盒身、盒盖、内衬和卡组侧边图集。
- `textures/card_back.png`：卡组正面和背面使用的默认卡背。
- `card_case_closed_preview.png`：闭合预览。
- `card_case_open_preview.png`：打开预览。
- `card_case_preview.png`：闭合与打开并排预览。
- `card_case_reference.png`：官方参考图合并对照。
- `card_case_spec.json`：尺寸、UV、铰链和开关角度。
- `build_card_case_assets.py`：生成图集与规格。
- `build_card_case_model.py`：通过 Blockbench MCP 重建模型。

## 尺寸

模型原点在底面中心，`Y = 0` 是盒底。放到决斗场地上时，应整体上移 `0.75`。

| 部件 | 宽 | 高 | 深 |
| --- | ---: | ---: | ---: |
| 盒身 | 8.4 | 8.0 | 5.6 |
| 盒盖 | 8.6 | 3.5 | 5.8 |
| 顶部倒角 | 约 7.2 | 0.5 | 约 4.6 |
| 内部卡组 | 6.875 | 10.0 | 2.4 |

总高度约 `12.0`。内部卡组按 60 张卡的厚度 `2.4` 建模；如果以后要显示实际卡组数量，可以把它替换成动态分层卡组模型，或者按数量缩放卡组厚度。

## 盒盖与开关状态

盒盖组的铰链位置：

```text
[0, 11.5, -2.9]
```

- 闭合状态：盒盖旋转 `[0, 0, 0]`。
- 打开状态：盒盖绕 X 轴旋转 `[110, 0, 0]`。
- 模型默认保存为闭合状态。

模型分组：

- `card_case_body`：盒身。
- `card_case_lid`：盒盖，原点在铰链处，可绕 X 轴旋转。
- `card_case_deck`：内部卡组，打开时可见。

定位点：

- `card_case_origin`
- `card_case_hinge`
- `card_case_deck_anchor`
- `card_case_front_center`
- `card_case_closed_rotation`
- `card_case_open_rotation`

## 图集 UV

图集为 `512 × 512`。

| 用途 | UV 矩形 |
| --- | --- |
| 盒身正面 | `[0, 0, 128, 192]` |
| 盒身背面 | `[256, 0, 384, 192]` |
| 盒盖正面 | `[0, 384, 192, 462]` |
| 盒盖背面 | `[192, 384, 384, 462]` |
| 盒身侧面 | `[0, 192, 64, 384]` |
| 盒盖侧面 | `[64, 192, 128, 320]` |
| 盒身内衬 | `[128, 192, 256, 384]` |
| 盒盖内衬 | `[256, 192, 384, 320]` |
| 卡组侧边 | `[384, 128, 448, 384]` |
| 盒盖顶部 | `[384, 384, 512, 448]` |
| 红色纯色 | `[384, 448, 448, 512]` |
| 暗色底面 | `[448, 448, 512, 512]` |

盒盖正面的字标是重新绘制的平面贴图，使用 Arial Bold 近似还原官方字体。官方参考图是带透视的三维渲染，不能直接平铺到模型表面，所以这里保留官方图作为样式依据，模型本体使用重绘贴图。

## Minecraft 接入提示

这是一个 Blockbench Generic Model，不是普通 Java 方块 JSON。后续 Forge 层建议用方块实体或自定义渲染器绘制：

- 闭合状态：盒盖旋转 `[0, 0, 0]`。
- 打开状态：盒盖绕 X 轴旋转 `[110, 0, 0]`，内部卡组露出。
- 抽卡、洗牌、检索时可以播放盒盖开合动画。
- 卡组数量变化时，替换 `card_case_deck` 或复用动态分层卡组的层数和缩放规则。

当前还没有实现卡盒开合动画、卡组进出盒动画或与规则引擎的数量同步，这些属于后续逻辑层。
