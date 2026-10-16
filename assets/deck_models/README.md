# 主卡组与额外卡组模型：固定版与 10 张分层版

这个目录现在有两套模型。固定版用于最初的比例验证；10 张分层版是后续接入规则引擎的推荐版本。两套模型都不包含正面卡图，只表示背面朝上的卡组和额外卡组。

## 默认卡背来源

MDPro 的 `TextureContainer.cardBackDefault` 指向：

`D:\MDProMod\MDPro3-main\MDPro3-main\Assets\Texture\Texture_Other\DefaultProtector.png`

对应默认卡套 `1070001`。源码中默认卡套来自 `Program.items.protectors[0].id`，因此这里使用该卡背，而不是只用于加载默认着色器的 `1070002`。

原始贴图为 `480 × 700` RGBA。为了控制 Minecraft 纹理体积，卡背被等比缩小到 `320 × 467`，并放入一张 `512 × 512` 的共享图集。图集保留金边、黑色漩涡、金色纹路和中心暗部，足以在物品栏和场地上辨认；没有逐像素复刻高光细节。

## 文件

固定版：

- `main_deck_v1.bbmodel`：主卡组模型，较厚。
- `extra_deck_v1.bbmodel`：额外卡组模型，较薄。
- `textures/deck_atlas.png`：两个模型共用的图集。
- `textures/card_back_default_simplified.png`：单独导出的简化卡背。
- `source/DefaultProtector.png`：从 MDPro3 源码复制的原始卡背。
- `deck_models_spec.json`：尺寸、UV、定位点和放置数据。
- `deck_stacks_preview.png`：两张模型并排预览。
- `build_deck_assets.py`：从原始卡背生成图集。
- `build_deck_models.py`：通过 Blockbench MCP 重建两张模型。

10 张分层动态版：

- `main_deck_dynamic_v1.bbmodel`：主卡组动态分层模型，最多 6 个完整 10 张层。
- `extra_deck_dynamic_v1.bbmodel`：额外卡组动态分层模型，1 个完整 10 张层加余数薄层。
- `deck_dynamic_preview_v1.bbmodel`：40 / 50 / 60 张主卡组和 10 / 15 张额外卡组的静态对比项目。
- `textures/deck_segment_atlas.png`：分层模型共用的卡背、10 张卡边、缝线和顶卡边缘图集。
- `segmented_deck_spec.json`：分层尺寸、高度、锚点和数量规则。
- `deck_dynamic_preview_labeled.png`：带标注的 3/4 视角对比预览。
- `build_segmented_deck_assets.py`：生成分层图集和尺寸规格。
- `build_segmented_deck_models.py`：通过 Blockbench MCP 重建动态分层模型。

## 10 张分层动态版本

每 10 张卡是一个完整厚度层：

| 参数 | 数值 |
| --- | ---: |
| 单层间距 block pitch | 0.40 |
| 单层卡体高度 | 0.388 |
| 层间暗缝 | 0.012 |
| 顶卡厚度 | 0.06 |
| 底部阴影 | 0.08 |

按这个规则，主卡组的动态高度是：

| 张数 | 完整层 | 余数 | 总高度 |
| ---: | ---: | ---: | ---: |
| 40 | 4 | 0 | 1.74 |
| 45 | 4 | 5 | 1.94 |
| 50 | 5 | 0 | 2.14 |
| 60 | 6 | 0 | 2.54 |

额外卡组按同样规则处理：

| 张数 | 完整层 | 余数 | 总高度 |
| ---: | ---: | ---: | ---: |
| 10 | 1 | 0 | 0.54 |
| 15 | 1 | 5 | 0.74 |

渲染器按卡组当前数量计算：

```text
whole = count / 10
remainder = count % 10
show whole full segments
if remainder > 0:
    show the hidden partial segment
    scale its Y to remainder / 10
    use only the first remainder stripes of the segment side UV
move the top card to the new top surface
```

主卡组最多显示 6 个完整层；额外卡组最多 1 个完整层加余数层。每层之间保留暗缝，因此 40、50、60 张的厚度差会直接表现为 4、5、6 段可见厚度，而不是一整块连续立方体。

`deck_dynamic_preview_labeled.png` 展示了 40 / 50 / 60 张主卡组与 10 / 15 张额外卡组的对比。`deck_dynamic_preview_v1.bbmodel` 只是静态可视化项目，实际运行时使用 `main_deck_dynamic_v1.bbmodel` 和 `extra_deck_dynamic_v1.bbmodel`。

## 尺寸

下面这组尺寸描述的是固定版；10 张分层版的尺寸以上一节为准。

单张卡片占位保持 `6.875 × 10` 模型单位，也就是 `704:1024` 的卡图比例。包含底部阴影时，两张模型的平面轮廓均为 `7.1 × 10.3`。

| 模型 | 阴影 | 卡体核心 | 顶卡 | 总高度 | 设计容纳量 |
| --- | ---: | ---: | ---: | ---: | --- |
| 主卡组 | 0.08 | 2.22 | 0.12 | 2.42 | 约 40–60 张带卡套的卡 |
| 额外卡组 | 0.08 | 0.82 | 0.12 | 1.02 | 约 10–15 张带卡套的卡 |

模型原点在底面中心，`Y = 0` 是阴影底面。放到决斗场地上时，应整体上移 `0.75`，与场地卡槽的 `0.75` 上表面齐平。主卡组的卡体高度是 `2.34`，额外卡组的卡体高度是 `0.94`；`2.42` 和 `1.02` 包含底部阴影。

## 图集 UV

逻辑 UV 尺寸为 `512 × 512`，与位图一致。

| 用途 | UV 矩形 |
| --- | --- |
| 卡背顶面 | `[0, 0, 320, 467]` |
| 主卡组侧层 | `[320, 0, 352, 512]` |
| 额外卡组侧层 | `[352, 0, 384, 512]` |
| 顶卡边缘 | `[384, 0, 416, 512]` |
| 阴影 | `[416, 0, 448, 512]` |
| 暗色底面 | `[448, 0, 480, 512]` |

侧面不是 40 张独立卡片，而是把多层卡边压缩成重复条纹。主卡组密度更高，额外卡组密度更低；这样既保留“厚”和“薄”的差别，也避免为每张卡建立独立几何体。

## 定位点

每个模型包含 5 个定位点：

- `<deck>_origin`：模型底面中心。
- `<deck>_stack_surface`：卡组核心底面，固定在 `Y = 0.08`。
- `<deck>_top_surface`：顶卡上表面，主卡组为 `Y = 2.42`，额外卡组为 `Y = 1.02`。
- `<deck>_card_anchor`：卡面水平中心，供后续放置卡牌实体使用。
- `<deck>_vertical_scale_origin`：动态增减卡组数量时的缩放基准。

如果以后要按实际张数改变厚度，只需纵向缩放 `<deck>_stack_cube`，同时把 `<deck>_top` 组、顶卡和 `<deck>_top_surface` 定位点移动到新的高度。`stack_surface` 和 `vertical_scale_origin` 保持不动。

## Minecraft 接入提示

这两张模型是 Blockbench Generic Model，不是普通 Java 方块 JSON。场地上的卡组以后更适合由方块实体或自定义实体渲染器绘制，模型可以直接使用同一张图集材质。主卡组和额外卡组共用同一卡背；若以后支持自定义卡套或对手卡背，只需替换 `deck_atlas.png` 的卡背区域，不必重建几何体。

当前没有实现抽卡、洗牌、卡组数量变化、顶卡翻开或与规则引擎的状态同步，这些都属于后续逻辑层。
