# 决斗盘右下角菜单：投降 + 时点（2026-10-15）

## 需求

右下角那个「确认键」（准备 / 游戏中 / 我的回合 / 胜利……）**左键**弹出一排按钮，
和卡片动作按钮一个用法（固定在世界里、准星对着点）：

1. **投降**——这一局判给对方；
2. **时点**——点一下在 **全时点 → 默认时点 → 关闭时点** 之间循环（MD / MDPro3 的「発動確認」）。

原来的右键（开始决斗 / 切阶段 / 决胜后回到准备）**一点没动**。

## 投降怎么实现的

MDPro3 的参考实现是它自己那份 C++ 服务端的 `SingleDuel::Surrender`；但**核心没有这个接口**——
`ocgcore.dll` 的导出名字里没有 surrender 一类的东西（只有 `create_duel` / `start_duel` /
`end_duel` / `set_player_info` / `process` / `set_response*` 这些）。所以宿主的做法是：

| 步骤 | 动作 |
| --- | --- |
| 1 | 模组发 `{"cmd":"surrender","player":0}` |
| 2 | 宿主标记认输座位，并唤醒可能在等应答的提示（`_response.Set()`） |
| 3 | 决斗线程回到循环顶部看到标记 → 发 `{"type":"surrendered","by":0,"winner":1}` |
| 4 | 紧接着发一条**标准 Win 消息**：`{"type":"message","function":5,"name":"Win","winner":1,"hex":"050100"}` |
| 5 | `Run` 返回 → `finally` 发 `{"type":"ended"}` |

第 4 步是关键：Java 侧判胜负**只认 Win 消息**（`DuelHostProcess` 从 JSON 的 `winner` 字段取值），
所以投降不需要单开一条收尾路径，右下角那种「胜利/失败」的显示逻辑原样复用（本侧认输 → 灰色「失败」）。

## 时点三档是什么

宿主按"核心在问什么"决定要不要停下等人。核心的提示号：`11` = 主要阶段命令、`12` = 可选效果要不要发动、
`13` = 是和否确认、`16` = 选连锁。

| 模式 | `16` 空窗口（没有可接的卡） | `16` 有卡可接 | `12` 可选发动 | 必须作答的窗口（11/13/选卡…） |
| --- | --- | --- | --- | --- |
| **全时点** `all` | **停下来问** | 问 | 问 | 照旧 |
| **默认** `default` | 自动放弃 | 问 | 问 | 照旧 |
| **关闭** `off` | 自动放弃 | **自动放弃** | **直接答"不发动"** | 照旧 |

- 「关闭」不碰 11/13：阶段推进和"是/否确认"是必须由玩家作答的，自动答会把回合弄坏。
- 「关闭」也不碰**强制发动**的连锁窗口：候选里带 forced 标志时必须由玩家选，替玩家放弃会被核心判非法
  （然后一直 RETRY）→ 这种窗口照常弹给玩家。
- `-Dmdpromod.autoPassChain=true`（没有 UI 时的兜底）优先级最高，永远压过时点设置。
- AI 座位（远半场）自己的提示先由 WindBot 回答，轮不到时点逻辑。

决策是一个纯函数 `DuelCore.ShouldAutoPass(function, emptyChain, timing, autoPassChain, out reason)`，
宿主用 `--timing-selftest` 直接断言（29 条）：三档的每个分支 + 强制窗口 + 模式名解析 + autoPassChain 优先级。
空连锁窗口在「全时点」下会停下来问，界面文案是「没有可以发动或连锁的卡，是否放弃优先权？」。

## 状态存在哪

| 东西 | 存放 | 说明 |
| --- | --- | --- |
| 当前时点 | 决斗盘实体的同步数据（NBT 键 `TimingMode`，存序号） | 客户端直接读它画按钮上的字（"时点：关闭时点"），存盘后下次进世界还在 |
| 开局时带上 | `DuelHostProcess.timingMode` → `start` 命令的 `timing` 字段 | 开局就用玩家选的档，不用等手动切 |
| 中途改 | `{"cmd":"timing","mode":"off"}` | 立刻生效，不改动已经答过的提示 |

## 代码

| 文件 | 改动 |
| --- | --- |
| `tools/duel_host/Program.cs` | `timing` / `surrender` 两条命令；`ShouldAutoPass` 决策 + 三档自动过；投降收尾（Win + ended）；`--timing-selftest` |
| `DuelTimingMode`（新，纯数据） | 三档的模式名 / 中文名 / 说明 / 循环 / 序号反查（`DuelTimingModeTest` 24 断言） |
| `DuelFieldEntity` | `TimingMode` 同步数据 + NBT；`onLeftClick` 命中自己那侧确认键 → 开菜单 |
| `ModNetwork.FieldMenu` / `FieldAction` | 服务端→客户端：开菜单；客户端→服务端：投降 / 换时点 |
| `DuelSessionManager` | `openFieldMenu` / `handleFieldAction`（投降前先核对"真的在打"）；开局把盘上的时点带给宿主 |
| `DuelHostProcess` | `setTiming`（在跑就马上发，没跑就记着）、`surrender`、`start` 带上 `timing` |
| `ClientFieldMenu`（新，客户端） | 键前方浮出一排按钮：投降（偏红，只在决斗进行中给）+ 时点（点一下循环）；左键打空收起菜单并把这一下吃掉（免得又落回确认键重开） |
| `ClientForgeEvents` | 接上 tick / 3D 渲染 / 左键 |

### 踩坑修正（同日）：菜单别把别人的点击吃掉

第一版有两个让人感觉"决斗盘的按钮变难点了"的毛病，都改了：

1. **菜单开着时，任何左键都被它吃掉一次**（本来是为了避免"关菜单的那一下又落回确认键重开"）。
   结果是：点阶段键、点卡片时第一下没反应。现在改成——**没打中按钮就收起菜单，并且把这下点击放行**，
   点阶段键/卡片立刻生效；只有真的打中按钮才吃掉这次点击。
   菜单还加了 8 秒空闲自动收起，不会一直挂在那儿挡事（点按钮时会刷新计时）。
2. **开关菜单不再走服务端往返**：原来是"左键 → 服务端判定 → 发包子端开菜单"，点了要等一下才出现。
   现在客户端自己命中判定（`DuelFieldEntity.controlUnderCrosshair`，和右键命中共用同一套算法）
   就地开关，点了就有。服务端只负责执行菜单里的动作（投降 / 换时点）。
3. **改客户端判定时漏了一条**：菜单没开的时候 `fieldEntityId` 还是 `-1`，而"按记住的 id 找实体"
   在那种状态下必然返回 null，于是打开菜单的分支被直接跳过——**左键点确认键完全弹不出来**。
   现在菜单没开时改用"准星现找"：`fieldUnderCrosshair()` 扫附近的决斗盘，用同一套命中判定
   确认准星正对着**自己那一侧的确认键**，再开菜单。

左键的完整行为（`ClientFieldMenu.handleClick`）：

| 状态 | 左键落点 | 行为 |
| --- | --- | --- |
| 菜单关着 | 自己那侧的确认键 | 就地打开菜单（吃掉这次点击，免得顺手抡一拳） |
| 菜单关着 | 别处 | 不介入，交回原版（阶段键、卡片照常） |
| 菜单开着 | 菜单按钮 | 执行（投降后收起 / 时点留在原地方便连点） |
| 菜单开着 | 别处 | 收起菜单，**并把这次点击放行** |
| 菜单开着 | 8 秒没动作 | 自动收起 |

### 再修正（同日）：按键判定不再看"你站在桌子哪一边"

用户反馈"右键点不了决斗盘的按钮，而且**第一回合没问题、第二回合才出**"。查下来是归属判定写错了：

```java
if (control != null && playerSide(player).equals(control.owner()))   // ← 站位判定
```

`playerSide()` 是**按几何位置**算的（在这半场算 p1、过了中线算 p2）。于是绕着桌子走一圈、
或者站到中线另一边去看对面的墓地，**自己那半场的阶段键 / 确认键会一起失灵**——
"第一回合好用、第二回合点不动"多半就是中途走动了。而且失败是静默的（控制键那段被跳过，
逻辑掉进后面"空手右键 = 绑定决斗盘"之类的分支），表现就是"点了没反应"。

改法：

1. **归属看归属**：新增 `DuelFieldEntity.ownsControl()`——这块盘是玩家在打，p1 就是他那半场，
   跟站位无关（以后真有第二个真人坐 p2，再改成按座位判断）。按到对面那半场的键会明说
   「这是对面那半场的按键」，不再静默。
2. **点击判定搬到客户端**：阶段键 / 确认键的命中用客户端**此刻**的视线算（服务端用的是上一 tick 的
   视角，本来就偏），左右键都算"按一下"，通过新包 `ModNetwork.FieldClick(controlId)` 发给服务端；
   服务端只做"控件存在 + 是自己那侧的 + 现在能不能按"的校验，然后执行
   （`DuelFieldEntity.activateControl()`，和右键交互共用同一段逻辑）。点了立刻有声音和反馈，
   也不用再靠原版实体交互那条又慢又偏的射线。
3. 菜单按钮从"正对按键的视线上"挪高了一点（`FORWARD 0.9 / HEIGHT 0.95`），不再挡着看按键。

行为表更新：

| 状态 | 落点 | 行为 |
| --- | --- | --- |
| 菜单关着 | 自己那侧的**确认键**（左键） | 开菜单 |
| 菜单关着 | 自己那侧的**确认键**（右键） | 确认键本来的功能（开局 / 提示回合 / 回准备） |
| 菜单关着 | 自己那侧的**阶段键**（左右键都行） | 切阶段（服务端按当前决策点校验） |
| 菜单关着 | **对面那半场**的键 | 提示「这是对面那半场的按键」 |
| 菜单开着 | 菜单按钮（左右键都行） | 执行（投降后收起 / 时点留原地） |
| 菜单开着 | 别处 | 收起菜单并把点击放行 |

### 严重修正（同日）：鼠标处理抢了 GUI 的点击 → 弹窗按钮点不动

用户报"卡死在「増殖的G 是否发动」界面"。查 `logs/latest.log` 的关键证据：

```
22:48:04.728  SelectChain count=1 forced=no cards=1     ← 窗口弹出
22:48:08.005  Chain answer: tawil2 -> -1                ← 玩家点了"不发动"，服务端收到了
...（又两次同样的窗口 / 应答）
22:48:35.031  SelectChain count=1 ... seq=7             ← 第 4 个窗口弹出
（再也没有 Chain answer 行）                              ← 玩家怎么点都传不到服务端
22:48:58.171  error: message handling failed for SelectChain: The operation was canceled（关游戏）
```

也就是说：**窗口本身没问题**（増殖的G 前三轮都是"对方特召一次就问一次"，玩家每次答"不发动"都正确送达），
坏在第 4 个窗口——点击根本没离开客户端。原因是前面那次把"决斗盘鼠标处理"接到
`InputEvent.MouseButton.Pre` 上时没考虑：**这个事件在有界面打开时也会触发**，
而处理里"准星对着自己那侧的按键"就会 `setCanceled(true)` 把点击吃掉——
于是连锁窗口 / 选卡界面里的按钮全部点不动，看起来就是"卡死"。

修法：`ClientForgeEvents.onMouseButton` 和 `ClientFieldMenu.handleMouse` 都加上
**「`minecraft.screen != null` 就直接让路」**的守卫（两处都加，双保险）。这一个守卫同时治好了
`ClientCardMenu`、手牌 HUD 那几个同源的处理——它们都是同一个事件、同样会抢 GUI 的点击。

顺带补了这类中断的善后：重启 / 崩溃之后决斗盘可能还留着"进行中"的标记，而服务端已经没有宿主了——
这时按确认键会**自动退回「准备」**（`DuelSessionManager.hasLiveDuel()` 判断），
而不是傻站着提示"现在是我的回合"。

产物：模组 JAR 243,199,459 字节，SHA-256
`649200A61258D84ADC6CD42A65C398C8156DC75AED35EB9E7506D98953C5CD69`，已部署（哈希一致）。
12 套离线测试全过。**实机未验证**。

## 验证

- 宿主自检：`duel_host.dll --timing-selftest` → `{"type":"timingSelftest","passed":true,"assertions":29}`。
- 宿主联调（`tmp/probe/surrender_probe.py`，按模组的 JSON 协议跑真实一局）：
  `started … surrendered(winner=1) … message(Win, winner=1) … ended`，
  且 `timing` 命令不报 `unknown cmd`、带 `timing` 的开局成功。
  踩过的坑：①脚本必须把宿主的工作目录设成运行时根（WindBot 找 `cards.cdb` 是按当前目录来的），
  否则宿主会以"Can't find cards database file"退出；②AI 对 AI 一局可能几百毫秒就打完，
  所以 `surrender` 要**紧跟 `start`** 发出去，不能等"看到 started 再发"（会输给这场竞速，
  第一版就是这么偶发失败的，现在两遍跑都是 `started → surrendered → ended`）。
- 离线测试：新增 `DuelTimingModeTest` 24 断言；其余 10 套全过
  （Banlist 67、DuelCardMarks 23、DuelTiming 21、DuelLocation 12、DuelVisualState 71、
  DuelVisualStateAudio 526、DuelAudioRules 117、DuelBgmRules、DuelAudioPerspective、AiDeckCatalog 177）。

产物：模组 JAR 243,192,077 字节，SHA-256
`A53C344FBA9EF0415B186639CF5BD8ED9ABF224EA9AE587B022FF4CFE3335BF5`，已部署（哈希一致）；
宿主 `duel_host.dll` 已重新 Release 构建（模组按固定路径启动它，不用重打 JAR）。
**实机未验证**。

## 待实机确认

1. 左键确认键能不能稳定弹出菜单（按钮要浮在键的前上方，好瞄）；右键的老功能是否照旧。
2. 投降之后右下角是否变灰「失败」、右键能回到「准备」开下一局。
3. 三档手感：`默认` 下空连锁窗口不打断；`关闭` 下不再问"是否连锁/是否发动"，但阶段推进照旧；
   `全时点` 会连空窗口都停下来问（点"取消"即放弃）。
