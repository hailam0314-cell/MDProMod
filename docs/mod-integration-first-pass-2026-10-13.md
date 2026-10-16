# MDProMod 资源接入第一版

日期：2026-10-13

## 已完成

决斗场地、卡盒和卡片已经作为自定义实体接入 Forge 1.20.1：

| 内容 | 注册名 | 说明 |
| --- | --- | --- |
| 决斗场地 | `mdpromod:duel_field` | 8 x 7 格平面场地，包含 32 个区域定位 |
| 卡盒 | `mdpromod:card_case` | 红色 DEFAULT CARD CASE，NBT 保存卡组信息 |
| 卡片 | `mdpromod:duel_card` | 单物品 + `CardCode` NBT，支持全部 15,019 个卡号 |

卡片注册表已经嵌入 mod：

```text
data/mdpromod/cards.json
```

包含 15,019 张卡的名称、类型、攻击力、防御力、等级、种族和属性。卡片图像不嵌入 jar，运行时从外部缓存按卡号加载：

```text
D:\MDProMod\runtime\card_textures_176x256_q80\<card_code>.jpg
```

可以通过系统属性覆盖：

```text
-Dmdpromod.cardTextureRoot=<folder>
```

## 游戏内命令

```text
/mdpromod field place
/mdpromod field info
/mdpromod case place
/mdpromod case load <deck_name>
/mdpromod case info
/mdpromod card give <code> [count]
/mdpromod card spawn <code>
/mdpromod duel places
/mdpromod duel place <index>
```

卡组文件查找顺序：

```text
D:\MDProMod\runtime\decks\<name>.ydk
D:\MyCardLibrary\mdpro3\Deck\<name>.ydk
```

## 当前渲染

- 场地：8 x 7 格平面贴图，主卡组和额外卡组区域显示卡背。
- 卡盒：盒身 + 盒盖两段模型，使用默认卡盒图集。
- 卡片：薄卡片实体，正面绑定外部 `176x256` 卡面，背面绑定默认卡背。
- 卡片交互：右键翻面，潜行右键切换攻击/守备。
- 场地交互：手持卡片右键场地时，必须指向具体区域；模组会按卡种检查该区域是否合法。自动寻找最近空位的旧行为已经删除。
- 规则引擎驱动的精确放置：`/mdpromod duel places` 列出 ocgcore 当前允许的区域，`/mdpromod duel place <index>` 发送选中的区域。

## 服务器加载验证

Forge 专用服务器加载测试通过：

```text
Loaded 15019 card definitions
Loaded 32 duel field zones
Done (20.980s)!
```

日志：

```text
docs/mod-server-load-test-2026-10-13.log
```

## 尚未完成

1. 场地仍是平面贴图，Blockbench 的 3D 边框和阶段按钮还没有渲染。
2. 卡盒目前是闭合模型，开盖动画和内部卡组实体还没有接入。
3. 主卡组和额外卡组区域目前显示固定卡背，还没有按真实张数显示 10 张分层厚度。
4. 场地放置卡片目前是手动右键近似规则；规则引擎驱动的精确放置使用 `/mdpromod duel places` 和 `/mdpromod duel place <index>`。
5. 卡面纹理目前同步加载并缓存 128 张，后续应改成后台加载和按视野预取。
6. 联机隐藏信息、客户端 GUI、网络数据包和多人同步还没有实现。

## 下一步

1. 用游戏内命令放置场地和卡盒，验证实体渲染和命令链路。
2. 把 Duel Host 的 `prompt` 数据接到卡盒/场地，创建一场真实决斗。
3. 把 `Draw`、`Move`、`Summoning`、`Set` 消息映射到 `DuelCardEntity` 的自动生成和移动。
4. 把动态 10 张分层卡组接到主卡组和额外卡组区域。
5. 最后再做客户端 GUI 和网络同步。
