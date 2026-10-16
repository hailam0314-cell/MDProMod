# 合成配方（2026-10-16）

给生存模式玩家一条从零开始的路线。四个物品的配方都是普通的原版合成，数据放在
`data/mdpromod/recipes/`，配方书解锁放在 `data/mdpromod/advancements/recipes/misc/`。

## 配方

| 物品 | 类型 | 材料 | 说明 |
| --- | --- | --- | --- |
| **卡盒** `card_case` | 有序 3×3 `PPP/LLL/PPP` | 纸 ×6 + 皮革 ×3 | 整套的基础件：右键编辑卡组 |
| **决斗盘** `duel_field` | 有序 3×3 `IRI/RCR/IRI` | 铁锭 ×4 + 红石 ×4 + 卡盒 ×1 | 需要先有卡盒 |
| **人机卡盒** `ai_deck` | 无序 | 卡盒 ×1 + 观察者 ×1 | 放到对侧半场跟 WindBot 打 |
| **音乐设置** `music_settings` | 无序 | 卡盒 ×1 + 音符盒 ×1 | 选决斗 / 王牌登场 / 低 LP 的曲子 |

设计口径：**卡盒是基础件，另外三个都是它的升级**（配方里都要一个卡盒），这样生存玩家
摸到纸+皮革就能起步，往后的成本各自对应 Minecraft 的进度（铁/红石 → 下界石英 → 木板）。

生存路线：纸+皮革 → 卡盒 → 铁+红石 → 决斗盘 → 观察者/音符盒 → 人机卡盒、音乐设置；
开局把卡盒放到自己这半场、人机卡盒放到对侧，点绿色按钮开打。

**没有配方**的两个：`duel_card`（带卡号的调试物品，配方对它没意义）、
`duel_anchor`（"决斗场锚点"方块，目前没有任何玩法逻辑，只在创造模式标签里）。

## 验证

- 新增 `RecipeDataTest`（87 条断言，离线）：四个配方都在、result/材料 id 认识、有序配方的
  pattern 每行等宽且 key 一一对应、无序配方材料数合法、每个配方都有对应的配方书解锁
  advancement（parent = `minecraft:recipes/root`、rewards 指向自己）。跑法见类注释
  （需要额外把 gson 加进 classpath，和 DuelBgmRulesTest 要 slf4j 一个道理）。
- 结构对着 1.20.1 原版配方文件逐字段核对过（`type/category/group/key/pattern/result`、
  `type/category/group/ingredients/result`），格式完全一致。
- 其余 13 套离线测试全过。
- **专用服务器上已实证**：模组自带的开局自检（`RecipeSelfCheck`，`ServerStartedEvent` 时查
  RecipeManager）在服务器日志里打出
  `合成配方自检：4 个都在，本模组共 4 条配方（全库 1178 条）`。
  ⚠️ 别被 `RecipeManager: Loaded 7 recipes` 误导——那是数据包合并**之前**的早期计数，
  库里真实的配方总数是 1178，我们那 4 条在里面（自检里 `本模组共 4 条` 就是证据）。
- 客户端里把材料摆上工作台这一步还没做（**实机未验证**）。

## 顺带修掉：模组之前跑不了专用服务器

想用 `gradlew runServer` 验证配方时，服务器在**模组构造阶段**就崩了，和配方无关：

```text
java.lang.BootstrapMethodError: java.lang.RuntimeException:
  Attempted to load class net/minecraft/client/gui/screens/Screen for invalid dist DEDICATED_SERVER
    at net.mdpromod.ModNetwork.register(ModNetwork.java:89)      <- 注册 PileView 那一行
    at net.mdpromod.MDProMod.<init>(MDProMod.java:68)
```

原因：`ModNetwork` 里那些**数据包 record 的 `handle` 方法体直接 new 了客户端界面**
（`Minecraft.getInstance().setScreen(new PileScreen(...))` 这类，共 9 处：OptionScreen /
SortCardScreen / UnselectCardScreen / PositionScreen / SelectCardScreen / ChainChoiceScreen /
DecisionScreen / PileScreen …）。`DistExecutor.unsafeRunWhenOn` 只挡住了"执行"，挡不住
**类加载与校验**：服务器上解析 `PileView::handle` 这个方法句柄时会加载 `PileView`，
它的字节码里引用了 `PileScreen` → `Screen`，被 Forge 的 RuntimeDistCleaner 判为
"在 DEDICATED_SERVER 上加载客户端类"。

改法（模组里已有现成的正确样板）：像 `ClientCardCaseBridge` 那样，把这 9 处
`setScreen(...)` 挪进一个纯客户端类（例如 `ClientPacketScreens.openPile(...)`），
record 的 `handle` 只调用这个方法——这样 record 的常量池里不再出现 `Minecraft`/`*Screen`，
服务器加载它时就不会去碰客户端类。10-13 的服务器日志里模组是能正常加载的，
说明这是这几天加界面时引入的**回退**，不是一直如此。

**已修复（2026-10-16）**：新增 [`ClientPacketScreens`](../mod/src/main/java/net/mdpromod/ClientPacketScreens.java)
（`@OnlyIn(Dist.CLIENT)`，8 个静态方法：选项/排序/素材/表示/选卡/连锁/发动确认/卡堆），
`ModNetwork` 里那 8 处 `setScreen(...)` 全部改成调它。

验证：`gradlew runServer` 起来成功（`Done (10.391s)`），开局自检打出上面那行
`合成配方自检：4 个都在…`；14 套离线测试全过。踩过的坑记一笔：**`Loaded N recipes`
那行不能用来判断配方有没有加载**（合并前就打出来了），要看 `ServerStartedEvent` 里的
`manager.getRecipes()`。另外 `runServer` 里往 stdin 敲 `stop` 不会传到服务器
（gradle 包了一层），要停就直接结束进程。
