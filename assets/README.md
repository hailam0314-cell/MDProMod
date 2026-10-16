# MDProMod 资源工作区

这个目录保存在 Minecraft 接入前已经完成可视化设计的 Blockbench 资源和规格文件。所有坐标使用 16 单位等于 1 个 Minecraft 方块的约定，卡片占位尺寸统一为 `6.875 × 10`。

## 已完成的资源

- `duel_field/`：双人决斗场地，包含双方各 15 个个人区域、中央 2 个共享额外怪兽区、阶段按钮和生命值面板。
- `deck_models/`：主卡组与额外卡组模型，共用 MDPro 默认卡背纹理。
- `deck_models/` 中的 `*_dynamic_v1.bbmodel`：按每 10 张一层动态改变厚度的主卡组与额外卡组版本。
- `card_models/`：单张卡片模型，正面按卡号动态加载完整卡图，背面使用默认卡背。
- `card_case_models/`：MDPro 默认红色 DUELIST CARD CASE 卡盒模型，包含闭合和打开状态。
- `runtime/card_textures_176x256_q80/`：当前默认的最小卡面缓存，15,019 张约 203.68 MiB，完整可用。
- `runtime/card_textures_352x512_q85/`：旧的部分详情缓存，6,745 张约 332.35 MiB，不是完整档。
- Forge 第一版已经把 `duel_field`、`card_case`、`duel_card` 注册为自定义实体；卡片名称和数值来自嵌入的 `cards.json`，卡面从 `runtime/card_textures_176x256_q80` 按卡号动态加载。

## 当前通用约定

- 卡槽和卡组原点都在底面中心，放置到决斗垫上时需要把 `Y` 增加 `0.75`。
- 卡背、卡面、卡框和效果文本分开管理；决斗场地上的卡组只显示背面。
- Blockbench 文件是可编辑母版，不直接等同于 Minecraft 的方块 JSON。
- 后续 Forge 层需要在方块实体或自定义实体渲染器中读取这些尺寸、UV 和定位点。
