# 选卡显示位置 + 连锁标记（2026-10-15）

两件事都**已完成并部署**：①选卡页面悬停显示「卡名 + 卡在哪」；②连锁里发动的卡 / 被点名的卡在卡面打标记。

## 一、选卡页面：悬停显示「卡名 + 卡在哪」（已完成）

以前选卡清单悬停只显示卡名，同名卡（两张「灰流丽」、墓地里的和场上的）分不清。
现在加一行位置。

### 数据从哪来

选卡提示（`MSG_SELECT_CARD`）里每个候选本来就带
`controller / location / sequence / position`（宿主用 `ReadCardEntry(reader, 8, i)` 解析），
只是一直没往客户端送。

| 环节 | 改动 |
| --- | --- |
| 宿主 | 宿主没动（数据是宿主自己发的 JSON，模组侧读得到） |
| `DuelHostProcess` | 新增 `selectAddrs()`：把候选的 controller/location/sequence 打包成 `int` |
| `ModNetwork.SelectList` | 多带一个 `int[] addrs`（和 codes 一一对应） |
| `SelectCardScreen` | 悬停时用两行 tooltip：卡名 + 「位置：自己墓地」 |
| `DuelLocation`（新） | 打包地址 → 人话，纯函数；12 条的 `DuelLocationTest` 覆盖怪兽区/魔陷区/场地/卡组/手牌/墓地/除外/额外卡组 |

位置文案和落点提示（`placeLabel`）保持同一套说法：自己/对方 + 怪兽区N / 额外怪兽区 /
魔陷区N / 场地魔法区 / 卡组 / 手牌 / 墓地 / 除外区 / 额外卡组。

产物：JAR 243,153,776 字节，SHA-256
`3C3794FC5EF819261E2662E2071B9BECCB865B9BABF40718A3AFA516D12F258A`，已部署（哈希一致）。

## 二、连锁标记（已完成）

需求：**连锁里发动的卡**和**被其他卡选为对象的卡**，在卡面上打**不同**标记。

好消息是数据都是现成的，不用碰 ocgcore 协议：

| 要标记的 | 数据源 | 已经在哪 |
| --- | --- | --- |
| 连锁中发动的卡 | `MSG_CHAINING`(70) | `DuelVisualState.chainCards`（Map<链号, Card>，**带地址**） |
| 被指定为对象的卡 | `MSG_BECOME_TARGET`(83) | `DuelVisualState.targetList()` 已解析（目前只用于动画事件） |

### 实际做法（和原计划有两处不同）

- 没走 `DuelAnimationScene.entityIdAt()`：服务端早就有 `DuelSessionManager.resolveCardEntities()`（选卡发光用的那套
  地址→世界实体 id），直接复用，客户端不需要世界状态反查。
- 标记形状改成**贴着卡面平铺的一圈发光边框**，不是竖着的色块：场地上的卡是平躺的
  （`DuelCardRenderer` 里 `Axis.XP.rotationDegrees(±90)`），竖色块会从卡片中间穿过去；
  平铺边框才是「打在卡面上」，从上往下看最清楚。

| 环节 | 改动 |
| --- | --- |
| `DuelVisualState` | 新增 `targetMarkIds`（被点名的**卡令牌**，只在连锁进行中记账）；新增 `chainMarkAddresses()` / `targetMarkAddresses()`：按令牌**当前**位置过滤，只留还躺在怪兽/魔陷区的卡，打包成 `controller<<16\|location<<8\|sequence` |
| `DuelHostProcess` | 转发上面两个方法 |
| `ModNetwork.CardMarks` | 新包（两个 `int[]` 实体 id），`sendNear` 96 格，对双方可见 |
| `DuelSessionManager` | 每 tick 把地址换成实体 id，**只在集合变化时**广播（`lastChainMarks`/`lastTargetMarks` 去重）。演出进行中（`reveal == false`）不急着点亮新标记，但"收掉标记"立刻发，免得连锁结束了标记还留在场上 |
| `ClientCardMarkers`（新） | 贴卡面画：淡光晕(alpha 36) + 亮边框(alpha 205)，尺寸跟 `DuelCardRenderer`（13.75/16 × 20/16），跟着卡的位置、攻守朝向、飞行偏移走；粒子 `FLAME`（橙红）/ `SOUL_FIRE_FLAME`（青蓝） |
| `ClientForgeEvents` | client tick 撒粒子 + `AFTER_TRANSLUCENT_BLOCKS` 画边框 |

尺寸/颜色（都在卡面上方 0.05 格；两层都在时青蓝在外、橙红在内，不会互相盖住）：

- 连锁中：边框骑在卡沿上，宽 0.06 格，RGB(255, 110, 55)
- 被指定：边框再往外 0.09 格，宽 0.05 格，RGB(70, 180, 255)

预览图（`tmp/mockup_chain_marks.py` 按同一套尺寸/颜色画，输出 `tmp/chain_marks_preview.png`）：
橙红=只标记连锁中、青蓝=只标记被指定、第三个=两者都有。

### 要注意的

- `MSG_BECOME_TARGET` 可能一条里点名多张卡；也可能同一个链节解决后再点名，
  所以集合**跟着连锁状态清空**（`MSG_CHAIN_END`(74) 与 `reset()`），否则标记会留在场上。
- 点名记录只在**连锁进行中**（`latestChain > 0`）记账：不在连锁里的 `BECOME_TARGET`
  没有对应的清空时机，记下来就会永远留在场上。连锁里被点名后即使目标关系被取消
  （97 号消息）也等到连锁结束一起收掉，不做单独撤销。
- 标记跟着卡走：发动后又被送墓/回手的卡当场掉标记（按令牌的当前位置算）。
- 手牌发动的效果、墓地/除外被点名的卡**不标记**：那些区域在世界里是一堆卡共用一个卡面，
  标上去会指错卡。
- 对手的里侧卡被指定时，标记不能泄露卡的身份——只画色块，不画卡面/卡名。

### 验证

- 新增 `DuelCardMarksTest`（14 断言）：场上发动被标记 / 手牌发动的没有 / 发动后送墓掉标记 /
  被点名进集合且连锁结束清空 / 一条消息点名多张去重且顺序稳定 / 不在连锁里的点名不记账 /
  同一张卡两种标记都有 / 重开一局清空。
- 全部测试通过（main 风格，对 JAR 跑）：DuelCardMarks 14、DuelLocation 12、DuelTiming 15、
  DuelVisualState 71、DuelVisualStateAudio 526、DuelAudioRules 117、DuelBgmRules、
  DuelAudioPerspective、AiDeckCatalog 177。

产物：JAR 243,160,256 字节，SHA-256
`5C9830606423A4F9B7577A2FB1420C6F60740B3BB616897940005F13FCBC11CC`，已部署（哈希一致）。
**实机未验证**。

## 三、选卡界面里也看得见标记（2026-10-15 追加）

**问题**：标记只画在世界的卡上，而选卡时注意力在弹窗里（弹窗又挡着场地），
等于选的时候看不到。

**改法**：同一批标记**连地址一起发**，弹窗按地址给候选卡描边。

| 环节 | 改动 |
| --- | --- |
| `ModNetwork.CardMarks` | 多带 `chainAddrs` / `targetAddrs`（打包地址，和实体 id 一一对应） |
| `DuelSessionManager` | 去重时也比对地址（只换格子不换实体的情况也要重发） |
| `DuelMarks`（新，纯函数） | `at(chainAddrs, targetAddrs, packed)` → 标记位；`label(mark)` → 「连锁中发动」「被指定为对象」「连锁中发动 · 被指定为对象」；颜色常量 `CHAIN_RGB=0xFF6E37` / `TARGET_RGB=0x46B4FF` |
| `ClientCardMarkers` | 世界里的边框改用 `DuelMarks` 的颜色；新增 `drawGuiMark()`（橙红贴卡沿、青蓝往内收 3px，和世界里的两圈一个意思） |
| `SelectCardScreen` / `UnselectCardScreen` / `ChainChoiceScreen` | 每张候选卡描边；悬停提示多一行「状态：…」（`UnselectCardScreen` 的候选/已选两排都描，顺手补上"在哪"） |
| `ModNetwork.UnselectList` | 多带候选/已选两组地址（原来只发卡号） |
| `DuelHostProcess` | 新增 `chainChoiceAddrs()`（连锁窗口候选的地址，和 `selectAddrs()` 同款） |

预览图：`tmp/mockup_select_marks.py` → `tmp/select_marks_preview.png`
（第 3 张两种标记都有、第 5 张只被指定、第 4 张是原有的金框"已选"）。

验证：`DuelCardMarksTest` 补了地址查询/文案的 9 条断言（共 23 条），其余 8 套测试全过。
产物：JAR 243,164,880 字节，SHA-256
`F7D64A0FD0591AEC508A353DD1E6EE9D18EDF87C519CD9E3039611002E062064`，已部署（哈希一致）。
**实机未验证**。
