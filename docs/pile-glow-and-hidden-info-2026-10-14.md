# 卡堆发光/粒子不出 + 列表页能看见对方盖卡（2026-10-14 晚）

## 1. 墓地 / 除外 / 额外"有卡可用"时没有粒子、没有泛光

**原因**：`DuelFieldLayout.zoneIdFor(controller, location, sequence)` 只认场上格子
（`location == 4` 怪兽区、`location == 8` 魔陷区/场地区），其它一律返回 `null`。
而 `ClientPileHints`（粒子 + 高亮）和卡堆高亮用的正是这个函数：

```java
String zoneId = DuelFieldLayout.zoneIdFor(0, location, 0);   // location = 16/32/64
if (zoneId == null) continue;                                 // ← 永远走这里，所以什么都不画
```

也就是说：host 侧"哪些堆有可用的卡"算得没错（`glowingPileZones` 用的是 prompt 里
`controller == 0` 且 `location ∈ {16, 32, 64}` 的候选），包也发过去了，
**只是客户端拿不到对应的格子**，`tick()` 撒粒子和 `render()` 画高亮每帧都提前返回。

**改动**：`zoneIdFor` 补上卡堆映射——`16 → p*_graveyard`、`32 → p*_banished`、`64 → p*_extra`
（`engineAddress` 早就有反向映射，两边现在对称了）。顺带 `resolveCardEntities` 里
"可选卡发光标记"也能覆盖卡堆了。

**左键的用法**（这次一起理顺）：射线通常先打到堆里最上面那张卡，以前左键会走"单卡动作菜单"
（而且地址只认堆首，序数不对 → 提示"没有可做的动作"）。现在**左键 / 右键点卡堆里的卡或格子，
都直接开整堆清单**（`PileScreen`）：

- 清单上排是"现在能做的动作"——每张可用的卡一个按钮（`发动：卡名` / `特殊召唤：卡名`），
  点下去才走 `PileAct` 把应答提交给核心；
- 下排是"全部卡"网格（翻页 / 滚轮，悬停出卡名，**左键点某一格弹大图详情**）。

## 2. 选择页面能看到对方盖的卡（信息泄露）

**原因**：这个模组是**直连 ocgcore** 的，核心不替我们做隐藏信息过滤（正常 YGOPro
是服务器按观看者过滤 `query` 结果）。`MSG_SELECT_CARD` 之类的提示里，
对方里侧盖卡的 `code` 就是**真卡号**，模组原样画了出来。

**改动**：新增 `DuelHostProcess.visibleCode(card)`，凡是把提示里的卡号发给客户端的地方都过一遍：

| 情况 | 处理 |
| --- | --- |
| `controller == 0`（自己的卡） | 照实显示 |
| 对方的卡，`location == 16`（墓地） | 公开信息，照实显示 |
| 对方其它区域的卡，表侧（`position & 0x05 != 0`：表侧攻击 0x1 / 表侧守备 0x4） | 公开信息，照实显示 |
| 对方的手牌 / 卡组 / 里侧盖卡 / 里侧除外 / 里侧额外 | 返回 `0` → 客户端画卡背、名字显示 **"里侧卡片"** |

覆盖面：`SelectCard`/`SelectTribute` 清单、`SelectUnselectCard` 的两组卡、
`SelectChain` 候选、`SortCard`、以及**对方卡堆清单**（新增
`DuelAnimationScene.publicCodesIn()`，只保留 `publicKnown` 的卡；墓地在规则上公开）。

为了让连锁候选也能判断朝向，host 的 `SelectChain` 候选里补了 `position` 字段
（取自候选项内 `info_location` 的最高字节）。

客户端侧新增 `McPanel.displayName(code)`：`code <= 0` 统一显示"里侧卡片"，
不再出现 `#0` 这种字样（连锁界面、选卡清单、卡堆清单、选素材、排序界面都改了）。

## 验证

| 项 | 结果 |
| --- | --- |
| host 重编译 | 0 错误；`--test-splitter` 9 条真实报文全过 |
| AI 整局 | 3970 行、0 截断警告、0 错误、`MSG_WIN` |
| 探针 | 303 个连锁窗口 / 950 个候选全部解析正确、0 错误；候选里已带 `position`（样本见 `docs/select-chain-probe-2026-10-14.json`） |
| 模组构建 | `cd D:\MDProMod\mod; .\gradlew.bat build --offline` → BUILD SUCCESSFUL |
| 产物 | `mdpromod-0.1.0.jar` 2,338,660 字节，SHA-256 `E2CC3E0A…9C85B0`，已复制到客户端 mods；旧包备份 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0725` |

## 需要你在游戏里确认

1. 自己回合、墓地 / 除外里有效果可发动时，堆上是否泛光 + 冒粒子（只有本人可见）；
2. 左键点卡堆 → 清单是否打开，点上面的「发动：卡名」是否真的把这张卡发动了；
3. 需要选卡时，对方的里侧盖卡是否显示成卡背 + "里侧卡片"。

## 备注

- 之前设想的"先在世界里弹一个『发动』按钮、再展开选卡界面"这次没有做：现在的做法是
  **一次点开清单，清单上排就是每张卡的『发动』按钮**，少一次点击、也不用再猜是哪张。
  如果你更想要"世界里先出按钮"的两段式，我可以按 `ClientCardMenu` 那套再补一个。
- 联机时"发给其他玩家"的隐藏信息过滤仍是缺口（现在只过滤了界面用的卡号）。
