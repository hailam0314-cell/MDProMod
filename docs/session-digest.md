# 会话状态摘要（每 5 轮刷新）

最近刷新：2026-10-14（首次建立）
用途：上下文被压缩、换会话或隔天回来时，读这一页就能接上；细节再看对应文档。

## 一行状态

MDProMod（Minecraft 1.20.1 / Forge 47.4.20）已经把决斗盘控制条（按钮 + 贴图 + LP 屏）、
**真实阶段切换**、开局发牌演出、卡片动作菜单、卡堆交互和各类决策窗口（选卡/落点/表示/连锁/
排序/素材/选项）全部接线完成，并通过 headless 探针验证。
2026-10-14 修掉了 `SelectChain` / `SelectIdleCmd` 的帧长错位（症状：连锁卡图错、场地魔法
像卡死、手牌点不出动作），详见 `docs/select-chain-frame-fix-2026-10-14.md`。
同一天还修掉了"选卡应答格式回退 + 提示去重只看形状"导致的 60 次 MSG_RETRY 停局
（详见 `docs/select-answer-retry-loop-2026-10-14.md`）。
下一个大块是：给还没有界面的 21/22/23/14x 窗口加兜底，然后是联机隐藏信息过滤。
随后补齐：**LP 实时显示**（91/92/100 增量 + 94 绝对值）、右下角按钮新增**金色胜利 / 灰色失败**
两态（右键回「准备」、AI 自动补卡组、可重新装填卡组）、**右键卡堆看全部卡**
（详见 `docs/lp-result-states-pile-list-2026-10-14.md`）。
同一天又修掉：**卡堆发光/粒子一直不显示**（`zoneIdFor` 没映射卡堆地址 16/32/64）、
**列表页会看到对方盖卡的真卡号**（直连核心没有隐藏信息过滤，要自己按 `position` 判表侧/里侧），
并把"左键点卡堆"统一成开整堆清单（详见 `docs/pile-glow-and-hidden-info-2026-10-14.md`）。
接着做了**怪兽立绘**（MDPro 的 `Picture/Closeup`）：先把 11,091 张压到高度 128
（284.8 MiB → 77.4 MiB，输出 `runtime/closeup_128/`，见 `docs/closeup-compress-2026-10-14.md`），
再接进模组——表侧怪兽上方悬浮朝玩家的立绘（0.3s 弹出、5s 浮动呼吸、按属性上色），
见 `docs/closeup-render-2026-10-14.md`（开关 `-Dmdpromod.closeup=false`，目录 `-Dmdpromod.closeupRoot`）。
再往后是**六类召唤演出**（素材吸入 → 场地中央大法阵 + MD 真网格 + 类型细节 → 怪兽从中央落地），
第四版把**节奏换成 MD 实测值**（用户提示"查音效长度"）：材料段 1.33 s + 召唤段
（融合 3.31 / 同调 3.99 / 超量 3.87 / 连接 6.77 / 仪式 3.69 / 灵摆 2.42 s，就是那六条召唤音效的长度）
+ 落地 0.6 s，六类合计 4.35~8.70 s；时长表在新类 `SummonTiming`，改那六个数字就能调速。
同时重做了光柱（根因：`pendulum_spot` 那张贴图**完全没有 alpha**，MD 的加法贴图在 MC 的普通
alpha 混合里会让黑底变成"半透明黑"，所以是一根硬矩形）。见
`docs\summon-animation-2026-10-14.md` 的第五~七节，节奏图 `docs\summon-timing-and-column-2026-10-14.png`、
光柱对比图 `docs\summon-column-preview-2026-10-14.png`。
（黑场后来按用户要求整个删掉了，见该文档 7.4：`ClientSummonEffects` 里 `fade()/renderFade()`
和 `ClientForgeEvents` 的调用都已删除，别再往回加。）

## 环境速查

| 项 | 位置 |
| --- | --- |
| 工程根 | `D:\MDProMod`（`mod/` 模组、`docs/` 文档、`tools/` 工具、`runtime/` 卡图与卡组） |
| 本会话工作区 | `C:\Users\郑杰田\Documents\ChatGPT\mdpromod`（含 `bb.py` 等临时脚本） |
| Python | `C:\Users\郑杰田\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe` |
| dotnet | `D:\MDProMod\tools\dotnet\dotnet.exe`，需 `DOTNET_CLI_HOME=D:\MDProMod\cache\dotnet-headless` |
| Duel Host | `tools\duel_host\bin\Release\net8.0\duel_host.dll`，运行目录 `tools\ygo_tcp_probe\staging` |
| 客户端 mods | `D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\mods` |
| 构建 | `cd D:\MDProMod\mod; .\gradlew.bat build` |

## 已完成（都能复验）

1. **控制条资源**：`assets\duel_controls\`（8 张贴图、`mdpromod_duel_controls.bbmodel`、
   `build_control_assets.py`、`control_spec.json`）。
2. **渲染**：`DuelFieldRenderer` —— 底座 + 键帽 + LP 托盘，帽顶 1.8 单位，
   三态贴图（灰/绿/蓝），文字用原版字体画在顶面。
3. **状态**：`DuelFieldEntity.controlState / canAdvance`；开始键灰「开始决斗」→绿「准备」→
   蓝「游戏中」→（开局后）绿「我的回合」/红「对方回合」；阶段键的绿色只由核心当前的允许位决定。
4. **阶段接线**：host 提示补 `toBattle / toEnd / toMain2`；点击 → `response 6/7/2/3`；
   空连锁窗口由 `autoPassChain` 自动放弃。
5. **开局发牌演出**：`DuelAnimationScene` 的开局窗口（阶段键锁定 + HUD 延后 +
   蓝「游戏中」保持到牌落地），见 `docs\duel-entry-opening-2026-10-14.md`。
6. **验证记录**：`docs\phase-switch-probe-2026-10-14.json`、
   `docs\field-flow-probe-2026-10-14.json`、`manual_policy_probe` 回归到 `MSG_WIN`。
7. **卡片交互（动作菜单流）**：右键=详情；左键卡=弹出动作按钮（召唤/盖放/发动，场上卡还有
   攻击/表示，按核心候选决定是否出现）；左键按钮执行；需要落点时左键场地格子（绿色高亮）。
   详见 `docs\card-interaction-2026-10-14.md`。
   - 动作按钮**固定在世界里**（卡片前方半格，准星对着它左键；AABB 命中判定），不是屏幕 UI。
   - 手牌 HUD（快捷栏上方那排）默认**关闭**，`-Dmdpromod.handHud=true` 才显示。
8. **特殊召唤 / 卡堆交互（已完成）**：额外卡组/墓地/除外三个卡堆有可用卡时发光 + 冒粒子
   （`PileHint`，只有本人可见）。左键卡堆 = 打开 `PileScreen`（上半部是"现在可以：
   特殊召唤 / 发动 XXX"按钮，下半部是全部卡 + 滚轮翻页）；右键卡堆 = 同样打开查看。
   动作走 `PileAct` 包 → `performPileAct` → `submitCardAction(..., "spsummon"/"activate")`。
   额外卡组"全部卡"用绑定的 `DeckData.extraDeck`（核心只公开表侧额外卡），墓地/除外用
   `DuelAnimationScene.codesIn`。
   验证：`tools\duel_host\card_action_probe.py` 走通"召唤 → 落点 → MSG_SUMMONED"，0 Retry。

## 未完成

1. **还没有界面的窗口**：只剩 `SortChain`(21)、`SelectCounter`(22)、`SelectSum`(23)、
   宣言类(14x)。现在至少会在聊天栏说清函数号与摘要（不再静默），但还是没法作答。
   建议下一步给 host 加"无界面窗口 N 秒后按最小代价代答"。（原先这栏写的
   SelectCard / SelectPlace / SelectPosition / SelectEffectYN **都早已做完**，2026-10-14 更正。）
2. 联机隐藏信息过滤、StrongSummon / FinalAttack 专属演出。

## 决策窗口 / 连锁（2026-10-14 晚）

- **演出闸门**：`tickField` 里 `reveal = !opening && !scene.busy()` —— 动画队列没放完之前，
  所有弹窗（决策/选卡/素材/连锁询问/表示）、连锁显示、卡堆发光、落点高亮、阶段键
  都先压住；放完再一次性出现（各块用"签名去重"实现延迟送达，不会丢消息）。
- `DecisionScreen`：`SelectEffectYN`(12) 与 `SelectYesNo`(13) 弹"是/否"窗口，答完自动复位。
- `ChainChoiceScreen`：`SelectChain`(16) 有候选卡时弹"接连锁／放弃"，应答
  `(index<<16)` / `-1`；host 只自动放弃"空窗口"（count==0），`autoPassChain` 默认已关
  （`-Dmdpromod.autoPassChain=true` 可恢复兜底）。
- `ClientChainState`：场地中央立链，链号醒目，新节弹出、解决的逐个消失。
- 决策类界面统一成 **MC 风格弹窗 + 卡图网格**（`McPanel`：灰面板+亮暗描边+凹槽、
  54×78 卡图缩略图、悬停出卡名、金色边框表示已选）：
  - `SelectCardScreen`：选卡清单，6×3 卡图网格 + 翻页 + 确认；
  - `ChainChoiceScreen`：连锁候选卡排成一排，标"第 N 环"，每张下面一个接连锁按钮；
    按钮改为**自己绘制 + 自判点击**（和卡片同一套坐标），并在卡与按钮之间画竖线，
    链号直接标在卡上——避免控件布局与卡图错位（2026-10-14 反馈修复）。
  - `PileScreen`：卡堆（额外/墓地/除外）——上半是有动作的卡+按钮，下半是全部卡网格；
  - `DecisionScreen`（发动确认）同风格。
  - `UnselectCardScreen`：融合/仪式素材那种"一张一张选 + 完成"——上排可选、下排已选
    （点一下取消），应答 `[1,index]`（index 跨合并列表）/ 完成 `-1`；
    场上/手牌的素材同时会泛光标记（可选绿、已选金），世界点击一次选一张（核心只收单张）。
  - `PositionScreen`：召唤后选"攻击表示 / 守备表示"。
  - `SortCardScreen`：`SortCard`(25)——按想要的顺序依次点卡、卡上标序号、再点取消，
    排完自动提交（应答 = `[张数, 新顺序下标…]`）。
  - `OptionScreen`：`SelectOption`(14)——核心给若干处理方式，按钮选一个（标签只能用
    选项序号/描述 ID，脚本里的效果描述文字客户端拿不到）。
- **决斗盘是大实体**：射线经常打在它身上而不是卡/格子上。左键和右键都要先做一次
  "准星对着哪张卡"的转发；卡堆（卡组/额外/墓地/除外）不在"可放卡格子"集合里，
  取格子要用 `findAnyZone`（`findClickedZone` 只认可放置的格子）。
  - 动作按钮：**召唤 / 特殊召唤**已拆成两个按钮（分别是核心的 summon 与 spsummon 候选）；
    一张卡什么都不能做时，会在聊天栏直接说明"核心没给出候选"并附 prompt 摘要，不再静默。
  卡组/额外卡组的卡没有世界实体，全靠清单类界面；手牌/场上的卡另有"仅自己可见"的
  发光+粒子标记，两种方式互补。
- 提示：链候选卡的解析（每项 14 字节）与 host 已验证的自动放弃路径一致，但还没在真机
  抓到 count>0 的窗口（40 秒探针内该座位没有可接的卡），需要游戏内确认一次。

## 已踩过的坑（避免重犯）

- 贴图缺失会让按钮只剩悬浮文字：`control_*` 必须与 `assets\duel_controls` 同源生成。
- 阶段应答必须看核心允许位，硬发 6/7 会吃 `MSG_RETRY`。
- 中文卡组路径：宿主 stdout/stdin 必须 UTF-8（`Console.ReadLine` 默认 GBK），
  否则 `珠泪.ydk` 会被读成 `鐝犳唱.ydk` 而导致开局静默失败。
- `MSG_SELECT_PLACE` 的 flag 是**禁用**掩码（要 `~flag` 才是可用格子）；
  WindBot 不能喂"对手的卡组/额外卡组"合成查询，否则读越界还会静默结束整局。
- **核心不洗牌**：`AddDeck` 装进去的顺序就是初始顺序，必须在 host 侧按对局种子
  自己洗主卡组（extra 不洗），否则每局起手完全一样。
- **应答格式不统一**（逐条核对 ocgcore 才敢发）：
  - **选卡族（15 选卡 / 20 祭品 / 26 素材）只认字节格式 `[张数, 下标…]`**（ocgcore
    `select_card` 从 `returns.bvalue[0]` 校验），**发整数一定被判非法**。2026-10-14 之前模组里
    有个"上次被拒就改发整数"的回退，靠"候选数:min:max"判断，同形状的新提示会被误判 → 锁死在
    整数格式 → 连吃 60 次 RETRY 后 host 停局。回退已删，别再引入。
  - **界面去重要带 host 的 `seq`**（每条 prompt 事件自增）：只用"形状"当指纹的话，
    核心重问同一条提示（应答被拒时会重发）就不会重新弹界面。
  - `SelectChain`(16)：**纯下标**（引擎校验 `0 <= 值 < 候选数`）；发 `(下标<<16)` 只有下标 0
    恰好合法，其余会被判非法 → "只有一张能选"。pass = `-1`。
    **2026-10-14 修正（这一节的旧结论是错的，已实测复核）**：头是 **12 字节**
    ＝ 功能号+玩家+候选数+spe+hint(4+4)，**头里没有 `forced` 字节**；**每个候选 14 字节**
    ＝ `标志(1)+强制标志(1)+卡号(4)+位置(4)+描述(4)`，**卡号在项内 +2、位置在项内 +6**。
    帧长 = **12 + 14*候选数**（n=0/1/2/3 → 12/26/40/54）。仓库里的 C++ 源码（13+13）和
    本机 DLL 不一致，**以实测为准**；证据与全部改动见
    `docs/select-chain-frame-fix-2026-10-14.md`。
    切分器 `case 16` 与 `case 11`（IdleCmd，实测 6 + Σ(1+7n) + (1+11n)）都曾有 1 字节偏差，
    会让每条消息吃掉下一条的首字节 —— 这就是之前"连锁卡图错、场地魔法像卡死"的来源。
  - **卡号最高位**：核心给"场地效果"打 `code | 0x80000000` 标记；读的时候要按 long 读并
    `& 0x7FFFFFFF`，否则会回绕成负数（表现为卡背 + `#-18592…` 这种卡名）。
  - **LP 只有四类消息**：`Damage`(91) 减 / `Recover`(92) 加 / `PayLpCost`(100) 减（都是
    `玩家(1)+数值(4,小端)` 的**增量**），`LpUpdate`(94) 才是设为某值。只看 94 的话血永远不动
    （核心扣血不会再补一条 94）；另外消息名大小写要对（host 发的是 `LpUpdate`，不是 `LPUpdate`）。
  - **世界里堆着的卡（墓地/除外/表侧额外）是实体**，射线先打到卡上：右键这类卡要**先按格子分流**
    到整堆清单，否则永远只弹中箭的那一张。
  - **卡堆地址也要能映射到格子 id**：`DuelFieldLayout.zoneIdFor` 只认 4/8 的时候，
    卡堆的高亮/粒子（`ClientPileHints`）每帧都提前 return，看起来"设过特效但没显示"。
  - **隐藏信息要自己过滤**：这个模组直连 ocgcore，核心不会按观看者过滤。
    提示里的卡号必须过 `visibleCode()`（自己的卡 / 双方墓地 / 表侧场上的卡才给真卡号，
    其余给 0 → 客户端画卡背 + "里侧卡片"）。判断表侧看 `position & 0x05`。
  - **`DynamicTexture` 不能开 mipmap**：`setFilter(true, true)` 时，凡是"被缩小采样"的贴图
    会整片变黑（GL 按最大缩小倍数挑 mip 级别，而 DynamicTexture 没有 mip）。症状很迷惑：
    同一批贴图里，横向被放大的能显示、双方向都缩小的全黑（召唤法阵 vs 十字光柱就是这样）。
    统一用 `setFilter(true, false)`（卡面是 `false,false`）。
  - **MD 的特效贴图有一半是"加法贴图"（暗底 + 亮图案、alpha 恒为 254/255）**：直接拿来做普通
    alpha 混合会把暗底糊上去——`pendulum_spot`（8 像素高的灰阶条，最亮才 135）当光柱就是一根
    变暗的硬矩形，`ritual_circle_*` / `pendulum_mask` 当法阵就是黑方块。
    `black_circle` / `fusion_*` / `synchro_*` / `link_*` 带真 alpha 可以直接用；
    `ritual_circle_01/03` / `ritual_fire_01` / `pendulum_mask` / `pendulum_spot` 五张要在载入时
    **按亮度烘成 alpha**（`SummonFxTextures.LUMINANCE_TO_ALPHA`）。挑贴图前先看 alpha 的取值区间。
- **提示字段也可能缺**：`SelectEffectYN` 的 host 解析只把卡号写进 summary、没有 `code`
  字段，导致"是否发动 XXX"弹窗没有卡名。现在 host 补了 code/controller/location/
  sequence/description，模组也加了"从 summary 里捞 card=" 的兜底。
  - `SelectCard`(15) / `SelectUnselectCard`(26)：**带长度前缀** `[张数, 下标…]`；
  - `SortCard`(25)：**没有前缀**，直接就是排列 `[下标…]`（加前缀会被当下标越界 → RETRY）；
  - `SelectPlace`(18)：`[player, location, sequence]` 三字节一组，无前缀。
  - host 现在收到 RETRY 时会把原提示**重新问一遍**（人类玩家可以重选），只有自动模式才兜底作答。
- **`tickField` 里 host 可能为 null**（没开局时）——每 tick 跑的分支里，
  任何 `else if (!host.xxx())` 都要先判空，否则一放决斗盘就 NPE 崩服务端线程
  （2026-10-14 04:36 崩过一次，已修）。

## 关键约定

- 16 单位 = 1 方块；控制件帽顶 y = 1.8；布局 JSON 与 `assets` 同源。
- 文档都是带日期的快照，新结论写新文件，不改旧文件的历史结论。
- 阶段规则表（谁能把阶段推到哪、应答值）见 `docs\phase-switching-2026-10-14.md`。

## 王牌怪兽登场演出（cut-in，2026-10-14 下午）

**实时播 MD 自己的 Spine 骨骼动画**已经落进模组：vendored `spine-libgdx 4.2.12` +
`gdx 1.12.1`（只挑 utils/math/files/graphics 打进包），用**同名的 `Texture` 桩**顶掉
libGDX 真身（那个要 GL 上下文），于是 UV/旋转/多页图集/约束全是官方算法，我们只写渲染层。

- 素材：`runtime\cutin\<卡号>\`（`tmp\extract_cutin.py` 按 **prefab→Timeline→Spine 轨道**
  的引用链抽，别照目录 dump——目录里常混着别的卡片的 rig，44 个目录共享一套 `_noait_`）。
- 清单：`data\mdpromod\cutins.txt`（卡号+时长毫秒，`tmp\make_cutin_roster.py` 生成）——服务端据此决定发不发。
- 触发：复用 `SummonEffect` 的 **phase 2**。正规出场**接在法阵之后、和落地并行**；
  非材料型（通常/上级召唤）先放演出、落地压后演出那么长。
- 自检（不用进游戏）：`java "-Dmdpromod.cutinRoot=..." -cp mod\build\libs\mdpromod-0.1.0.jar net.mdpromod.spine.CutinSelfTest <卡号>`。
- 预览（进游戏前先看效果）：`tools\spine_preview`，第 4 个参数 `setup` 可只渲 setup pose。
- 全集 498 套，时长几乎都是 1.6 s；细节与"散架疑云"见 `docs\cutin-spine-2026-10-14.md`。

## 全量低清与场地播放（2026-10-14 后续更新）

当前实现见 `docs\cutin-bundled-world-2026-10-14.md`：已全量导出并按宽高 1/8 压缩，497 套 Spine（639 PNG）内置模组，另 1 套非 Spine 的 4 张图也已压缩。全部 PNG 约 1 GB → 39.25 MB，最终模组约 67.85 MB。默认直接读包内资源，不再依赖外置目录。

演出已由 GUI 移到场地中央，按场地尺寸撑满宽度、保留比例、朝向各客户端相机，双方及附近观众可见。每个场地独立排队，保留召唤时序。全量正式 JAR 自检 497/497、构建、216 种视角几何及裁剪检查通过；双客户端游戏画面尚未实测。稳定提取/压缩/审计/名录生成管线在 `tools\cutin_pipeline`。

## 背景音乐（BGM）与「音乐设置」（2026-10-15）

110 首 BGM 全量转 OGG（**立体声 q0**，101.7 MiB；立体声 q0 与单声道 q3 体积相当但保住声场，
因为 BGM 是非定位播放，不需要单声道），开局演出结束后延迟 250 ms 播放。
新增「音乐设置」物品（`mdpromod:music_settings`，图标用卡背），右键打开**分页界面**
（110 首 + 「不播放」，滚轮翻页），选择存进**物品 NBT**，上线时服务端同步给客户端；
对局中换曲子立刻生效。默认曲目从借用的 `SE_M08092_F_LOOP` 换成真 BGM `BGM_DUEL_NORMAL_01`。
JAR 243 MB（+106 MB），协议仍是 3，服务端/客户端需同版。细节见
`docs\bgm-tracks-2026-10-15.md`；**实机听感尚未验证**，也还没做"LP 低于 25% 自动切高潮曲"。

同日修订：改成**按「时机」分栏**——决斗 / 王牌登场（cut-in 期间接管）/ 低 LP 高潮
（本地 LP ≤ 2000 自动切），每栏各选一首，优先级 王牌登场 > 高潮 > 决斗；
某栏选「不播放」= 该时机不打断常驻 BGM。界面加了**每行 ♪ 试听**和底部「试听/停止」，
关闭界面、按任意键、被别的界面顶掉都会停试听。选择存物品 NBT（`Music.<时机>`），
旧字段 `MusicTrack` 兼容读作「决斗」。⚠️ 界面控件重建必须**延迟到 tick**，
别在按钮回调里直接 `rebuildWidgets()`（MC 正在遍历 children）。
另：首次反馈"音乐没触发"实为**玩家设置里音乐音量为 0**，不是 bug。

再修订（同日）：时机会话被"每 tick 重算"导致 cut-in 一结束就切回普通曲——已改成
**闩锁**（王牌登场 / 低 LP 高潮 一旦触发就保持到本场结束，cut-in 上升沿还会把这首从头再放）；
另外我们的 BGM 与原版背景乐同为 `SoundSource.MUSIC`，现在只要有我们自己的音乐在放就
每 tick `MusicManager.stopPlaying()` 压住原版。每次换曲打一行 `BGM 切换：[时机] 曲目`，
排查直接看这行。产物 SHA-256 `B03C145E…F199B`。

第三次修正（同日）：上述"闩锁 + 上升沿重放"仍会让**多只王牌把音乐反复重播**。
回去读原版 `AudioManager.cs`（299~320 行）后照抄其 **只前进不后退阶段机**：
0 常规 → 1 王牌 → 2 高潮；`PlayBgmKeyCard()` 只在 `bgmState == 0` 生效、
`PlayBgmClimax()` 只在非 2 生效，开局 `PlayBgmNormal()`（OcgCore 2818/2975，
牌组演出结束回调里）复位成 0。结论：**王牌只切一次、多只不重播；高潮压住王牌且不回头**
（优先级改为 高潮 > 王牌）。状态机抽成纯函数 `DuelBgmRules.advanceBgmStage`，
`DuelBgmRulesTest.stageMachine()` 有回归断言（第二只王牌不得重播等）。
已知差异：原版每首 BGM 带循环起止点（前奏只放一次，如 NORMAL_01 是 9.6s~115.2s），
MC 只能整首循环，前奏会重放——属独立改动，未做。
产物 SHA-256 `A736092B…C8F62`。

## 「选择选项」文字（2026-10-15）

弹窗只显示 `Option 0 (description 78857041)` 这类占位——**宿主从没解析过选项描述**。
口径照原版 `StringHelper.Get`：`desc < 10000` 取 `strings.conf` 的 `!system`；
否则 `code = desc >> 4`、`index = desc & 0xf`，取该卡在 **`cards.cdb` 的 `texts` 表**
（`str{index+1}`，即卡脚本 `aux.Stringid` 的原文）。宿主现在整进程加载这两张表
（15,019 张卡 / 535 条 system）并在 `case 14` 用 `DescribeOption()` 出文字；
新增自检 `--describe-option <id…>` 可直接核对（78857041 →「从自己卡组上面把3张卡送去墓地」）。
`OptionScreen` 另做了宽度自适应与空标签兜底。**宿主改完要 `dotnet build -c Release`**
（模组按固定路径启动它，不需要重打 jar）。细节见 `docs\option-labels-2026-10-15.md`。

## 「人机卡组」物品（2026-10-15）

远半场不再自动补青眼白龙（`ensureAiDeck` 已删）。新增 `mdpromod:ai_deck`（蓝色卡盒图标，
`tmp/make_ai_deck_icon.py` 从卡盒图标按亮度染蓝）：右键开只读界面（`AiDeckScreen`）——
上方 `◀ [人机名 ▾] ▶` 可展开下拉（8 条/页、滚轮翻页），下方是主/额外/副三区卡图预览，
悬停显示卡名，**不能增删卡**；换人机立刻写回物品 NBT。
人机清单由 `AiDeckCatalog` 扫 `Data/Windbot/Decks/AI_*.ydk`（57 个）并读
`Data/locales/zh-CN/bot.conf` 取中文名/人格/描述（`'Level VIII'`→`AI_Level8.ydk`，
大小写不敏感），匹配不上用文件名兜底。

规则变化：自己的卡盒只能放**自己这侧**，放对侧提示「等待对手选择卡组」；
人机卡组放对侧 → 读那副 ydk、绑定 p2、记住人机身份；点绿色按钮时对面没放也提示同一句
（不再自动补）。开局用玩家选的 ydk + 人格启动宿主（`sendStart` 优先用传入的 `deck1`）。
离线验证 `AiDeckCatalogTest`（177 断言，字段说明见 `docs\ai-deck-item-2026-10-15.md`）。
**实机未验证**。

### 严重修正：弹窗按钮点不动（"卡死在増殖的G界面"）（同日）

用户报卡死在连锁窗口。翻 `logs/latest.log` 定位到：窗口本身正常（増殖的G 被连问四次是因为对方
连续特召，前三次"不发动"都成功送达，日志有 `Chain answer: tawil2 -> -1`），**第四次窗口的点击
根本没离开客户端**（之后再无 `Chain answer` 行）。原因：把决斗盘鼠标处理接到
`InputEvent.MouseButton.Pre` 时没注意**这个事件在界面打开时也会触发**，处理里"准星对着自己那侧的
按键"就 `setCanceled(true)` 把点击吃掉了 → 连锁窗口 / 选卡界面里的按钮全部失灵。
修法：`ClientForgeEvents.onMouseButton` 与 `ClientFieldMenu.handleMouse` 都加
「`minecraft.screen != null` 就让路」守卫（同一守卫也治好 `ClientCardMenu` / 手牌 HUD 的同源问题）。
另外补了中断善后：重启/崩溃后决斗盘残留的"进行中"假状态，按确认键会自动退回「准备」
（`DuelSessionManager.hasLiveDuel()`），不再只会提示"现在是我的回合"。
产物 SHA-256 `649200A6…53C5CD69` 已部署，12 套离线测试全过。**实机未验证**。

### 卡堆界面视觉改版（同日追加）

第一版排版偏裸文字，按模组统一的 MC 风格重做：标题条（凹灰条 + 白字标题 + 张数 + 深底彩边
状态标签「可以特殊召唤 N 张」）、提示行右侧放「已选：〈卡名〉」、可召唤卡金框**外面再包一圈深金**
（立体感）、悬停**淡白纱**、选中**白框 + ✔**、MC 风格滚动条（深槽 + 带亮暗边的滑块）、
底部分隔线 + 「看到第 x–y 张 / 共 N 张」+ 右侧两个按钮。预览图 `tmp/pile_screen_preview.png`
（脚本 `tmp/mockup_pile_screen.py`，用运行时目录里的真实卡图 1:1 画）；预览阶段顺手修掉两个排版问题：
提示行与首行卡片太挤（HINT_H 12→14）、底部"已选"被按钮压住（挪到提示行右侧并按剩余宽度截断）。
产物 SHA-256 `B3E4459C…5C01C986` 已部署，12 套离线测试全过。**实机未验证**。

### 额外卡组清单跟着核心序号走（同日追加，重要）

用户反馈"召唤完一只怪兽，额外卡组没减少、它还能再被召唤"。实机日志坐实：**核心的额外卡组是有序表，
卡片离开后序号整体前移**（卡组清单 `8491961,8491961,8491961,48608796,…` 召掉一张后，
核心给的候选变成 `8491961@0,8491961@1,48608796@2,…`）。界面却一直用**卡组原始清单** →
张数不变、而且"下标=序号"的贴法从第二张起就错位（点召唤过的卡，其实唤的是别人）。
改法：新增纯逻辑 `ExtraDeckTracker`（离开=去掉最前面那个同卡号；回到额外卡组=追加到末尾；
只算自己那侧；`ExtraDeckTrackerTest` 17 断言），`DuelSessionManager` 开局初始化（读开局导出的 ydk）、
每 tick 用 `host.drainEvents()` 更新、`resetForNewDuel()` 恢复整副；`allCodes(0,64)` 返回这份实时清单，
并和核心 `count(0,0x40)` 对账（不一致写日志警告）。召唤动作本身走
`host.promptAction(controller, location, sequence)`，序号对上之后就点哪张召哪张。
产物 SHA-256 `73017947…F21C6DF7` 已部署，13 套离线测试全过。**实机未验证**。

界面改版（同日）：`AiDeckScreen` 重做成 **MC 风格**（`McPanel` 面板/凹槽/卡图/金框，
与编辑卡组同观感）——顶部 `◀ [名字 ▾] ▶`、描述行、**主/额外/副三个分区标签**、
固定 3 行卡图网格 + 滚轮翻页 + 页脚，卡图大小按窗口高度自适应（GUI scale 3 的
427×240 也不溢出）。选人机改成**第二层** `AiDeckPickerScreen`（两列、9 行/页、
滚轮翻页、✓ 标记、悬停显描述、点选后回第一层）。
排版先用 `tmp/mockup_ai_deck_screen.py` 出预览图核对过（当时发现并修掉"页脚压住
第三行卡""描述行与标签重叠"两个问题）。产物 SHA-256 `5C97C12A…C8E06`。

## 提示里的「时机」说明（2026-10-15）

MD 弹「要不要发动/连锁」时会先说时机，模组现在补上（`DuelTiming` 纯函数，`DuelTimingTest` 15 断言）：
① 连锁窗口 `ChainChoiceScreen` 标题下多一行「「〈卡〉」的效果发动了，是否连锁？」，
来源卡取 `host.chainCodes()` **最后一项**（掩掉场地效果的 `0x80000000`）；
② 发动确认（功能号 12）问句改成「〈对方/自己〉进入〈阶段〉，是否发动「〈卡〉」的效果？」；
③ 进入新阶段时给自己一条 action bar 播报「对方进入准备阶段」（判变化，同阶段不重复）。
`SelectYesNo`(13) 没改（它是"是/否"确认，硬套时机反而误导）；`SelectIdleCmd` 没有独立弹窗，
靠阶段播报说明时机。细节见 `docs\prompt-timing-2026-10-15.md`。**实机未验证**。

## 选卡显示位置 + 连锁标记（2026-10-15）

**已完成**：选卡清单悬停改成两行 tooltip「卡名 + 位置：自己墓地」。
候选的 controller/location/sequence 本来就在宿主 JSON 里，模组只是补了
`selectAddrs()` → `SelectList.addrs` → `SelectCardScreen` 显示；文案抽成纯函数
`DuelLocation`（`DuelLocationTest` 12 断言）。

**已完成**：连锁中"发动的卡"与"被指定为对象的卡"在卡面打不同标记。
`DuelVisualState` 新增 `targetMarkIds`（被点名的卡令牌，只在连锁进行中记账，74/reset 清空）
和 `chainMarkAddresses()` / `targetMarkAddresses()`（按令牌**当前**位置过滤：送墓/回手当场掉标记，
只留怪兽/魔陷区）；`DuelHostProcess` 转发 → `DuelSessionManager` 每 tick 用现成的
`resolveCardEntities()` 换实体 id，**只在集合变化时** `sendNear` 新包 `ModNetwork.CardMarks`
（演出进行中不急着点亮新标记，但"收掉标记"立刻发，免得连锁结束了还留在场上）。客户端 `ClientCardMarkers` 贴卡面画
一圈发光边框（淡光晕 alpha 36 + 亮边 alpha 205；连锁中=橙红骑卡沿、被指定=青蓝再往外 0.09 格，
两者都在时同心不互盖），粒子 `FLAME`/`SOUL_FIRE_FLAME`。
注意点：不在连锁里的 83 号消息不记账（没有清空时机，会永远留在场上）；手牌/墓地/除外的卡不标记
（那些区域一堆卡共用一个卡面，会指错）。
离线验证 `DuelCardMarksTest`（14 断言）+ 其余 9 套全过；预览图 `tmp/chain_marks_preview.png`
（脚本 `tmp/mockup_chain_marks.py`）。产物 SHA-256 `5C983060…BC11CC`，已部署。**实机未验证**。
详见 `docs\select-location-and-chain-marks-2026-10-15.md`。

### 追加修正（同日）

**① 转阶段不再说"有卡发动了效果"**：链上还什么都没有时（第一次发动之前的窗口，
转阶段的诱发效果就是这类），核心问的是"要不要发动"。判据 `host.chainCodes().length == 0` →
用 `DuelTiming.openQuestion(候选卡名)`：「「X」的效果可以发动，是否发动？」
（多张候选 →「有 N 张卡可以发动效果，是否发动？」）。窗口跟着换措辞
（`ChainChoice.opening`）：标题「发动效果」、按钮「发动/不发动」、角标「必须发动」、
不显示"第几环"。`DuelTimingTest` 20 断言。
**② 选卡界面也能看到标记**：`CardMarks` 连打包地址一起发，新纯函数 `DuelMarks`
（`at()`/`label()`/颜色常量，世界边框和界面都用它）→ `ClientCardMarkers.drawGuiMark()`
给候选卡描边（橙红贴卡沿=连锁中、青蓝往内收=被指定），`SelectCardScreen` /
`UnselectCardScreen` / `ChainChoiceScreen` 都接上，悬停多一行「状态：…」；
`UnselectList` 补了两组地址。预览图 `tmp/select_marks_preview.png`。
`DuelCardMarksTest` 23 断言，其余 8 套全过。产物 SHA-256 `F7D64A0F…E062064`，已部署。

## 禁卡表配置文件（2026-10-15）

之前卡盒的禁限判定是 `DeckRules` 里写死的 6 条 `LIMITS`，而运行时根目录里那份完整
`lflist.conf`（104 张表、676 KB）根本没读过——这就是"禁卡表很混乱"的来源。
现在拆成 **数据 + 开关** 两个文件（都在运行时根目录）：
`lflist.conf` 只读；新增 `banlist.conf` 每行 `on/off 卡表名`（首次运行自动生成，
默认打开最新的 `2026.10`，其余 103 张全 off）。可以同时开多张，**同一张卡取最严的那条**；
一张都没开 = 只校验张数；`$whitelist` 模式的表照 MD 的规矩办（没列出来的卡 0 张）；
alias（同一张卡的不同版本，580 张）共用额度。每 2 秒查一次文件的修改时间+大小，
变了才重新解析（真 lflist 解析 83 ms），所以**改完存盘最多 2 秒生效、不用重启**。
判定收敛在 `DeckRules`，于是卡盒加卡拦截/角标（禁=深红「禁」、1=红、2=橙）/悬停
「限制 1（2026.10 限制）」/标题「禁卡表：2026.10」/检索"只看限卡"/存卡组与开局校验
（`problems`）一起生效；**人机卡组不校验**（挡 Windbot 会让对局开不起来）。
新文件：`Banlists`（纯解析/合并）、`BanlistFile`（读取+签名缓存+生成默认配置）、
`ModPaths`（运行时根 + config 目录，`DuelSessionManager.runtimeRoot()` 转调）、`ModLog`（懒拿 slf4j，
离线测试没有 slf4j 也不炸）；`CardRegistry` 日志改走 `ModLog`、载入失败只试一次。
离线验证 `BanlistTest` 67 断言（含真 lflist 与 alias），其余 9 套全过。
产物 SHA-256 `C960943D…7AD8D59`，已部署。

**同日追加：配置文件改放游戏 config 目录**——开关文件现在在
`<实例>\config\mdpromod\banlist.conf`（线上用 `FMLPaths.CONFIGDIR`，`MDProMod` 启动时登记给
`ModPaths.useGameConfigDir`；离线测试用 `-Dmdpromod.configDir` 覆盖，都没有才退回运行时根目录的
`config/`）。卡表数据仍默认用运行时根目录的 `lflist.conf`，但 config 目录里放一份就用那一份；
旧位置（运行时根目录）里已有的 `banlist.conf` 会自动搬进 config 目录，内容不丢。
`BanlistTest` 补 7 条断言（搬到 config、自定义 lflist 覆盖）。本机那份已搬到
`D:\光明MOD服1.12客户端\.minecraft\versions\1.20.1-Forge_47.4.20\config\mdpromod\banlist.conf`。
详见 `docs\banlist-config-2026-10-15.md`。**实机未验证**。

## 决斗盘右下角菜单：投降 + 时点（2026-10-15）

右下角确认键**左键**弹一排世界按钮（和卡片动作按钮同款，准星对着点）：**投降** +
**时点**（点一下在 全时点 → 默认 → 关闭 之间循环）。右键老功能（开局/切阶段/决胜后回准备）没动。
投降：ocgcore **没有** surrender 接口（导出名里只有 create/start/end_duel、process、set_response*），
所以宿主动收尾——标记认输座位 → 发 `surrendered` + 一条**标准 Win 消息**（`{"type":"message",
"function":5,"winner":1,"hex":"050100"}`）+ `ended`，Java 侧沿用现成的胜负判定（判对面赢）。
时点：宿主 `ShouldAutoPass(function, emptyChain, timing, autoPassChain, reason)` 纯函数决策——
16 空窗口（all 才问 / default 自动过 / off 也过）、16 有卡可接（off 自动放弃）、
12 可选发动（off 直接答否）；**不碰** 11/13 这些必须作答的窗口，**也不碰强制发动**的连锁窗口
（候选带 forced 标志时替玩家放弃会被核心判非法、一直 RETRY），AI 座位的提示仍归 WindBot。
状态存在决斗盘同步数据 + NBT（`TimingMode`），客户端直接读它画按钮上的字，开局经 `start` 的
`timing` 字段带给宿主。新文件 `DuelTimingMode`（纯数据，`DuelTimingModeTest` 24 断言）、
`ClientFieldMenu`（客户端世界按钮）；宿主加了 `timing`/`surrender` 命令、`--timing-selftest`
（29 断言）；联调脚本 `tmp/probe/surrender_probe.py`（真实一局：started → surrendered(winner=1)
→ Win → ended；两个坑：宿主工作目录必须是运行时根，否则 WindBot 找不到 cards.cdb；
AI 对 AI 一局几百毫秒就打完，surrender 要紧跟 start 发，不能等 started 再发）。
离线 11 套测试全过；模组 JAR SHA-256 `A53C344F…3335BF5` 已部署，`duel_host.dll` 已重新
Release 构建。详见 `docs\field-menu-surrender-timing-2026-10-15.md`。**实机未验证**。

## 空手蹲下右键收回决斗盘 / 卡盒（2026-10-15）

**空手（两只手都空）＋蹲下＋右键**：对着决斗盘就收回决斗盘（物品回背包，背包满就掉脚下），
对着卡盒实体就收回卡盒。手势放在 `interact` 最前面（shift = "拿起"的惯例，准星压在卡上也是收起来）。
决斗盘收回顺序有讲究：①不是本人绑的盘、②`isGameStarted() && !hasResult()`（开局还没出结果）
都只提醒不动（后者指路右下角菜单的「投降」）；然后 `stop()`（关宿主 + `clearVisuals()`，
这步要用 `fieldId`，必须赶在清空它之前）→ 清掉盘上的卡组记录 `fieldDecks` 与 `fieldId/ownerId`
→ `field.discard()` → 还物品。卡盒物品从来没被消耗，所以收回时啥都不丢，只是这场对局的记录没了。
顺手补了个丢卡组的 bug：`CardCaseEntity` 原来（蹲下右键放下会**消耗**物品）只能念摘要、
拿不回来——现在同一个手势用 `CardCaseItem.setDeck` 把卡组写回物品再还给你。
全 11 套离线测试通过（改动全在实体交互层，无新增纯函数）。产物 SHA-256 `A600D7B1…4DE0E083`
已部署。详见 `docs\field-reclaim-2026-10-15.md`。**实机未验证**。

### 修正：决斗盘按钮变难点（同日追加）

用户反馈"决斗中点决斗盘的几个按钮变得很困难"，两条原因都在这一批改动里：
①右下角菜单开着时**任何左键都被吃掉一次**（原来"点空也吃掉"是怕关菜单的那下又落回确认键重开），
点阶段键/卡片第一下没反应 → 改成"没打中按钮就收起菜单并把点击**放行**"，菜单加 8 秒空闲自动收起；
②收回决斗盘的判断原来压在 `interact` **最前面**，蹲着（两只手都空）右键点阶段键会走进收回分支 →
键等于失灵 → 挪到卡/卡堆/按键判定**之后**，想收就对着空白处蹲下右键。
顺带把开关菜单改成**客户端就地判定**（新增 `DuelFieldEntity.controlUnderCrosshair`，和右键命中共用算法），
不再走服务端往返，点了就出现；`ModNetwork.FieldMenu` 包随之删掉，服务端只留 `FieldAction` 执行动作。
产物 SHA-256 `5AD211BF…0FC48A97` 已部署。

### 再修正：左键弹不出菜单（同上）

上一条把开关菜单搬到客户端时漏了一步：菜单**没开**的时候 `fieldEntityId` 还是 `-1`，
而"按记住的 id 找实体"在那种状态下必然 null → 打开分支被跳过，左键确认键彻底弹不出菜单。
改成菜单没开时**准星现找**：`ClientFieldMenu.fieldUnderCrosshair()` 扫附近决斗盘 + 同一套命中判定，
确认正对着自己那侧的确认键再打开。左键五种情况的行为表已写进
`docs\field-menu-surrender-timing-2026-10-15.md`。全 11 套离线测试通过；产物 SHA-256
`BB46D987…A081C7D2` 已部署。**实机未验证**。

## 卡堆界面改版：全部卡 + 滚轮 + 两个按钮（2026-10-15）

用户反馈"从额外卡组特殊召唤时一页最多 3 个、第 4 个之后无法选择"。根因：`PileScreen` 的动作按钮
只画一排、`x > panelW - 110` 就 `break`（写死最多三个），而且只在 `init()` 建一次、**不跟翻页走**——
第 4 张之后连按钮都没有；顺带按钮上的长卡名会画出界压到隔壁格子上。
按用户要求改成：**所有卡一页显示**（滚轮滑动 + 右侧滚动条）、**能召唤/发动的排最前面并描金框**、
下面固定两个按钮「特殊召唤」（墓地/除外区是「发动」）和「关闭」；左键选中（白框，选中可召唤的卡
按钮才可按），右键弹大图。排序抽成纯函数 `PileOrder.sorted(actionSeqs, actionCodes, allCodes)`
（动作优先 → 去重 → 序号越界时用动作里的卡号兜底，宁可多留一行也不漏卡），
`PileOrderTest` 12 断言；其余 11 套全过。产物 SHA-256 `1CAFAC54…001F5E` 已部署。
详见 `docs\pile-screen-2026-10-15.md`。**实机未验证**。

### 再修正：按键失灵（第二回合）＝ 归属判定写成了站位判定（同日）

用户反馈"右键点不了决斗盘的按钮，第一回合没问题、第二回合才出"。根因：控制键那段用的是
`playerSide(player).equals(control.owner())`，而 `playerSide()` 是**按几何位置**算的
（这半场算 p1、过中线算 p2）→ 绕着桌子走一圈或站到中线另一边看对面墓地，自己那半场的
阶段键/确认键一起失灵，而且失败是静默的（掉进后面"空手右键 = 绑定决斗盘"之类的分支）。
改法：①新增 `DuelFieldEntity.ownsControl()`——**归属看归属**（这块盘是玩家在打，p1 就是他那半场），
按到对面的键会明说「这是对面那半场的按键」；②阶段键/确认键的**命中判定搬到客户端**
（用此刻的视线，服务端只有上一 tick 的视角），左右键都算按一下，走新包
`ModNetwork.FieldClick(controlId)`，服务端校验（控件存在 + 自己那侧 + 状态允许）后执行
`DuelFieldEntity.activateControl()`（和右键交互共用）；③菜单按钮挪高（`FORWARD 0.9/HEIGHT 0.95`），
不再挡在"看向按键"的视线上。12 套离线测试全过；产物 SHA-256 `6736749B…96594FA2` 已部署。
**实机未验证**。

## 包内资源 + 首次解包（2026-10-16）

包外依赖不再需要 MDPro3：卡图 15,019 + 立绘 11,091 + 召唤特效/网格 + 宿主运行时
（`cards.cdb` / `cards.json` / `lflist.conf` / `script.zip` / ocgcore·ygoserver·sqlite3 三个 DLL /
Windbot 卡组与对话）+ 宿主自己的 .NET 运行时全塞进 jar（**566.8 MB，26,548 个条目**），
首次启动在后台解包到 `<游戏目录>/mdpromod/`（**396 MB，实测 25 秒**；第二次 2 秒全跳过，
删掉的文件会补回来）。路径优先级 `-D 覆盖 > 解包目录 > 旧开发目录`；四个资源加载器都改成
"解包没跑完就不记缺图"；玩家卡组迁到 `config/mdpromod/decks`（旧目录里的 ydk 会拷一份过去）；
解包没完时 `DuelHostProcess.start` 拒绝开局并在聊天栏说明。
宿主打包口径：**框架依赖 exe + 同目录共享运行时 + 由模组给子进程设 `DOTNET_ROOT`**，
千万别改成 `PublishSingleFile` / `--self-contained`（`Mono.Data.Sqlite` 会 TypeLoadException，
实测三种发布方式只有这套能跑）；`runtime/cards.json` 是 WindBot 的卡名表，**不能省**。
端到端验证：从 jar 解出来的那份运行时直接跑完整人机对局到 `MSG_WIN`（3,407 条消息）+ 13 套
离线回归全过；**实机已验**：客户端 35 秒解完 26,548 个文件（版本隔离下的
`versions\1.20.1-Forge_47.4.20\mdpromod\`），宿主 `ready.root` 指向解包目录、`cards.json`
加载 15,019 张、卡图正常、一局对局正常推进。卡组导出目录也一起从写死的
`D:\MDProMod\runtime\decks` 改成 `config/mdpromod/decks`（`DuelSessionManager`/`DuelCommands`/
`ModNetwork` 三处）。细节见 `docs\bundled-runtime-2026-10-16.md`。

## 合成配方（2026-10-16）

生存模式路线：**卡盒**（纸 ×6 + 皮革 ×3）是基础件，**决斗盘**（铁 ×4 + 红石 ×4 + 卡盒）、
**人机卡盒**（卡盒 + 观察者）、**音乐设置**（卡盒 + 音符盒）都是它的升级。
数据在 `data/mdpromod/recipes/`，配方书解锁在 `data/mdpromod/advancements/recipes/misc/`。
新增离线自检 `RecipeDataTest`（87 断言：结构/id/pattern/解锁一对一），13 套旧回归全过，
结构对着 1.20.1 原版配方逐字段核对过；产物 `902D34E2…` 已部署，**实机未验证**。
细节见 `docs\crafting-recipes-2026-10-16.md`。

**同一天还修掉一个回退：模组之前跑不了专用服务器**（`ModNetwork` 里 8 处数据包 record 的
`handle` 直接 `new` 客户端界面 → `BootstrapMethodError: Attempted to load class Screen for
invalid dist DEDICATED_SERVER`）。现在那 8 处都改调新类 `ClientPacketScreens`
（`@OnlyIn(CLIENT)`，照 `ClientCardCaseBridge` 的样板）。`gradlew runServer` 已能正常起来
（`Done (10.391s)`），并在开局自检里打出 `合成配方自检：4 个都在…`。
⚠️ 排查时注意：`RecipeManager: Loaded 7 recipes` 是**合并前**的早期计数，别拿它判断配方有没有加载；
要看 `ServerStartedEvent` 里的 `manager.getRecipes()`（真实是 1178 条，含我们 4 条）。
`runServer` 的 stdin 敲 `stop` 传不进服务器，停服直接结束进程。

## 脱离 MDPro3 单独运行（2026-10-16）

游玩只需要 `mods\mdpromod-0.1.0.jar`（566.8 MB）+ 首次解出的 `<版本目录>\mdpromod\`（396 MB，
删了会自动重解）。清理与保留清单见 `docs\standalone-checklist-2026-10-16.md`：
可删 `D:\MyCardLibrary`(19.0 GB) / `MDProMod\MDPro3-main`(1.5 GB) / `Windows`(4.5 GB) /
`Android`(3.6 GB) ≈ **28.6 GB**；重新打包要留 `runtime\{card_textures_176x256_q80,closeup_128,
summon_fx,summon_mesh}` + `tools\ygo_tcp_probe\staging` + `tools\duel_host\bin\Release\net8.0` +
`tools\dotnet\{host\fxr,shared\Microsoft.NETCore.App}` ≈ **400 MB**。
配套改动：①卡组搜索去掉 `D:\MyCardLibrary\mdpro3\Deck` 候选（只找 config 目录和 CWD）；
②MDPro3 的 5 个 DLL 拷进 `D:\MDProMod\refs\`，4 个 csproj 改成相对引用（`dotnet build -c Release`
已验证通过）；③`stageBundle` 原料缺失时打 WARN，避免悄悄打出不带资源的包。

## GitHub 发布准备（2026-10-16）

仓库已在 `D:\MDProMod` 初始化（`main` 分支）并**暂存完成：735 个文件 / 38.6 MB**，尚未提交。
新增根目录文件：`README.md`（英文主 + 中文简介）、`LICENSE`（GPL-3.0 全文）、
`THIRD-PARTY-NOTICES.md`（含 Spine 许可全文）、`.gitignore`（排除 Konami 音频 168 MB、
`runtime/`、`MDPro3-main*`、`tools/dotnet` 等 10 GB+ 大件，每条都有注释说明）。
清单、Release 附件方案（全量 jar / 无素材 jar）、命令与 Release Notes 草稿见
`docs\github-publish-2026-10-16.md`；CurseForge 用的中英简介见 `docs\curseforge-release-2026-10-16.md`。
提交前记得确认全局 git 身份是你自己的。
