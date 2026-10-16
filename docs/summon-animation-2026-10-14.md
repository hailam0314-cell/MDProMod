# 六类召唤演出：调研结论 + Minecraft 第一版（2026-10-14）

## 一、MD/MDPro 是怎么做的（调研结论，可复验）

实现位置：`MDPro3-main\Assets\Scripts\MDPro3\Managers\TimeLineManager.cs`（真代码）
+ `MDPro3\Duel\GameCard.cs`（触发与落地）+ `MDPro3\Duel\CoreWrapper.cs` / `YGOSharpOCGWrapperEnums.cs`（reason/type 位）。
资源是 Master Duel 的 Unity Timeline（`MasterDuel/Timeline/summon/...`）。

反编译 dump 里的 `YgomGame.Duel\*Effect.cs` 是**空壳**（`Create()` 返回 null、`spSummonType` 全写成 Fusion），
只有字符串常量可用；真正的逻辑在 MDPro3 自己的 Unity 源码里。

**触发（"正规出场"的判定）**：`材料数 > 0` 且 `TypeMatchReason(卡的类型, 材料卡的 reason)`：

| 类型 | 卡的 type 位 | 材料的 reason 位 |
| --- | --- | --- |
| 融合 | Fusion | `0x40000` |
| 同调 | Synchro | `0x80000` |
| 仪式 | Ritual | `0x100000` |
| 超量 | Xyz | `0x200000` |
| 连接 | Link | `0x10000000` |
| 灵摆 | （`0x400000` 有定义，TypeMatchReason 不判） | 实际走融合那套流程 |

**两段式**：

1. 材料段：加载 `SummonFusionShowUnitCard0{材料数}` 展示材料卡，
   粒子颜色按 reason 分（同调＝调谐器绿/非调谐器青、超量＝黄、连接＝红、仪式＝(0,0.5,1) 蓝、融合＝默认），
   **1.33 秒**后切召唤段；期间回调 `StrongSummonLand(位置)` + `CameraManager.BlackOut(0, 0.3f)`（黑场 0.3 s）。
2. 召唤段：Ritual/Synchro/Xyz/Link 各有协程，其余（含融合、灵摆）走 `SummonFusion()`：
   - 融合 `summonfusion0{1..5}_01`，>5 用 `fusionnum`；
   - 仪式 `summonritual01/02`（按材料数隐藏多余的 `RitualTrailIn01~03`）；
   - 同调 `summonsynchro01/02` + 调谐器/非调谐器**等级数字** + **星环按等级换**（<5 / <9 / ≥9）；
   - 超量 `summonxyz00_01/01_01/02_01/03_01`（按素材数）+ `XYZInMesh*`（叠放素材材质）；
   - 连接 `summonlink01_01/02_01/03_01`（按 Link 数）+ `LinkTrailIn01~03` 按箭头裁。

**附加系统**：`MonsterCutin`（个别怪兽专属 cut-in，有则落地延迟 1.3 s，融合卡 1.0 s）、
`StrongSummon`（连接数 >2 / 超量等级 >3 / 同调等级 >5 用更重的落地：-30° 抬升翻转 0.4 s，普通只用 -10°）、
材料卡离场时的 `fusiontrailfieldcard01` 黄色拖尾 + `SE_SUMMON_EYZ_MATERIAL`。

## 二、素材能不能用（已实测）

- **能用**：贴图（PNG，带透明通道）、音效（wav/ogg）、网格顶点、动画曲线/粒子参数/材质颜色（当规格表）、
  时间轴结构（能读出"第几秒触发哪一段"）。
- **不能直接用**：Timeline/PlayableDirector 不能"播放"（Unity 专有 + 引擎内对象引用，MC 里没有播放器）；
  ShaderGraph 材质编不进 MC 渲染管线；ParticleSystem 只有参数；Spine cut-in 需要 Java Spine runtime。
- 抽取工具：工程里现成的 `tools\unitypy`（UnityPy 1.25.4）+ `tools\extract_unity_bundle.py`。

**本次抽出来的**（脚本 `tmp\extract_summon_fx.py`，源包只读）：13 张 → `runtime\summon_fx\`，
最长边 512、调色板 256 色、**合计 194 KB**：`ritual_circle_01/03`、`ritual_fire_01`、`fusion_010/011`、
`synchro_noise`、`synchro_line`、`link_gate`、`link_line`、`pendulum_mask`、`pendulum_spot`、
`flash`、`black_circle`。（速览图 `tmp\summon_fx_sheet.png`）

## 三、模组第一版实现了什么

**判定（服务端 `DuelAnimationScene`）**：素材 Move 事件的 reason 带 `MATERIAL(0x8)` 时记下召唤类型位
（掩码 `0x107E0000`），随后 `summon` / `special_summon` 事件到来时按 `summonFxKind()` 匹配
（类型位与 reason 位成对、素材在 4 秒内），命中就：

1. 向附近玩家发 `ModNetwork.SummonEffect`（kind 0 融合 / 1 同调 / 2 超量 / 3 连接 / 4 仪式 / 5 灵摆 + 场地中心坐标）；
2. 给这次落地**加 200 ms 顿帧**。

**演出（客户端 `ClientSummonEffects`）**：场地上放该类型的法阵——两层贴图（里层大、外层小，
反向旋转）、由小变大（easeOut）+ 淡出，约 1.5 秒；配一道十字竖直光柱（`pendulum_spot`）；
同时**黑场**：0.11 s 压黑 → 0.09 s 保持 → 0.26 s 亮回。配色照 MD 的材料粒子色。

贴图按名字从 `-Dmdpromod.summonFxRoot`（默认 `D:\MDProMod\runtime\summon_fx`）加载；
`-Dmdpromod.summonFx=false` 可整体关闭；缺图就跳过该层（不报错、不空白）。

## 四、验证与产物

| 项 | 结果 |
| --- | --- |
| 触发的真实数据 | 实战日志里 138 条 Move，其中 **26 条** `reason=0x40048`（MATERIAL + 融合）→ 触发链会命中 |
| 构建 | `gradlew build --offline` → BUILD SUCCESSFUL |
| 包内确认 | `ClientSummonEffects` / `SummonFxTextures` / `ModNetwork$SummonEffect` 都在 |
| 素材 | `runtime\summon_fx\` 13 张 / 194 KB（原图 14.7 MiB → 压到 512 档） |
| 产物 | `mdpromod-0.1.0.jar` 2,360,925 字节，SHA-256 `F5AFA83D…FBCBED`，已复制到客户端 mods；旧包备份 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0812` |

## 五、还没做（下一步候选）

## 五之二、第一次实机反馈：超量召唤"看不到动画"（同日修复）

排查结果：**触发是命中的**——那次超量素材的 Move `reason=0x200008`（XYZ+MATERIAL），
召唤的怪兽 `73542331`（俱舍怒威族的香格里拉茧）`type=0x800021`（带 XYZ 位），判定条件满足。
问题在**画面**：

1. 顶点色是**乘**在贴图上的，而超量那组第一版拿 `black_circle`（黑图）当主层 → 黑 × 金色还是黑，
   基本看不见；第二层 `flash` 又只有 0.72 倍大、透明度 165。
2. 尺寸（1.6→4.8 格）和时长（1.5 s）都太含蓄。

修复：

| 项 | 改前 | 改后 |
| --- | --- | --- |
| 层数 / 贴图 | 2 层，超量含黑图 | **3 层，只用亮线稿**（超量 = `fusion_011` + `flash` + `fusion_010`） |
| 尺寸 | 1.6 → 4.8 格 | **4 → 12 格**（场地 16×14，盖住大半个场地） |
| 时长 | 1.5 s | **2.2 s** |
| 光柱 | 1.5 宽 / 最高 2.8 | **3 格宽 / 最高约 5.7** |
| 半径 | 96 | **128**（双方都能收到） |
| 诊断 | 无 | 命中时打日志 `Summon FX: kind=N card=X at field center` |

**排查手法（下次照用）**：看不到时先看客户端日志有没有那行 `Summon FX:`——
有 → 是渲染问题；没有 → 是触发没到。这条二分法能一步定位。

## 五之三、第二版：材料段演出 + 类型细节 + "从中央落下"（同日）

按需求把召唤流程改成三段式（**素材演出取代原来"卡从卡组/额外飞到场"的动画**）：

1. **素材吸入**（`phase 0`）：素材被消耗时，材料卡的飞行目标由服务端改到**场地中央**
   （kind 变成 `absorbN`，N 是召唤类型），时长压到 430 ms、相邻素材每 120 ms 叠着飞；
   客户端沿路径撒**该类型配色**的粒子（融合紫 / 同调绿 / 超量金 / 连接红 / 仪式蓝 / 灵摆绿），
   同时中央先起一个小法阵（0.9 → 4 格）。
2. **正式召唤**（`phase 1`）：中央大法阵（**最大约 12 格**、三层、反向旋转）+ 十字光柱 + 黑场，
   并加**类型细节**：
   - 融合：法阵层数 = 素材数量（1~3 层）；
   - 同调：**按等级分档**（<5 → 1 层、<9 → 2 层、≥9 → 3 层）+ **等级数字**浮在法阵上方；
   - 超量：飘出**叠放素材**（1~3 张卡背小方块）；
   - 连接：按 **Link 数**向外放射箭头（最多 8 支）。
3. **落地**：怪兽不再从额外卡组/卡组飞过来，而是**从场地中央落到自己的格子**（430 ms）。

诊断日志（服务端 + 客户端各一行，看不到时先看这两行）：

```
Summon FX: kind=2 card=73542331 materials=2 at field center          ← 服务端判定命中
Summon FX recv: kind=2 code=73542331 materials=2 at=… textures=[fusion_011=ok,flash=ok,fusion_010=ok]
```

第二行里的 `MISSING` 会直接告诉我们贴图没加载到（例如目录不对），`ok` 就说明包和贴图都没问题、
问题在画法上。

## 五之四、"只看到十字光柱，法阵全黑"的真正原因（同日）

实机反馈：只看到十字光柱，六类法阵贴图一张都看不见。客户端日志显示包确实到了：

```
Summon FX recv: kind=2 code=73542331 materials=2 at=(34.5, 31.5) …      ← 包到了、位置对
```

根因是**纹理过滤参数**：`SummonFxTextures` 里写的是 `texture.setFilter(true, true)`
（线性 + **mipmap**），而 `DynamicTexture` **不会生成 mipmap**。GL 的 LOD 是按两个方向的
*最大缩小倍数*挑的，于是：

- 法阵（512² 铺在 ~12 格地面上，两个方向都在**缩小**）→ 需要 mip 级别 → 没有 → **采样为黑**；
- 光柱（256×8 的细条，纵向被**放大** 几十倍）→ LOD 停在 0 级 → 正常显示。

这就精确解释了"只有光柱可见"。修复：改成 `setFilter(true, false)`（只线性、不开 mipmap）——
和能正常显示的卡面（`false,false`）、立绘（`true,false`）保持一致。

**教训（写进"坑"清单）**：给 `DynamicTexture` 开 mipmap 会让"被缩小"的贴图变黑，
而且症状很迷惑（同一批贴图里有的能看有的不能，取决于该方向是放大还是缩小）。

## 五之五、"和想象差太远"——MD 用的是**真 3D 网格**，不是平铺贴图（同日）

查清了：MD 的召唤演出由 **网格 + 粒子 + shader** 组成，我前面那版只是"贴图平铺在地面上"，
所以形状完全不对。

**网格可以整块导出**（`UnityPy` 的 `mesh.export()` → OBJ 文本，再转成顶点/UV/索引 JSON）。
本次导出的（脚本 `tmp\preview_summon_meshes.py`，几何落在 `runtime\summon_mesh\`）：

| 网格 | 顶点/三角 | 长什么样 |
| --- | --- | --- |
| `SynchroCircle01` | 128 / 64 | 一圈**带箭形标记的平面环**（半径约 12.9 单位） |
| `SynchroCircle03` | 246 / 307 | 实心细环（同调星环的另一档） |
| `XYZInMesh01` | 244 / 240 | **螺旋锥**——素材被卷进叠放的样子 |
| `SummonRitualCylinder02` | 858 / 1600 | **漏斗状光柱**（上大下小，仪式的"开幕"） |
| `SummonLinkTrail02` | 186 / 244 | **螺旋飘带**（连着连接门） |
| `SummonPendulumChoker02` | 1430 / 1400 | **八边形传送门** + 下方尖刺 |
| `fxl_dithit_Ring_001` | 195 / 256 | 通用冲击环 |

预览图（俯视 + 侧视）：`docs\summon-mesh-preview-2026-10-14.png`。
另外 `built-in/duel` 里还有 42 个召唤相关网格与 3,305 个粒子系统；融合那套在 MD 里**以粒子为主**
（漩涡是粒子，不是网格）。

**下一步计划**：写一个"网格特效渲染器"——把 JSON 网格用自绘三角面画出来、贴抽出来的贴图，
再按 MD 的动画曲线驱动变换（旋转/上升/收束），六类各自用自己的网格与节奏；
融合用粒子 + 纹理重建。这样形状才和 MD 接近（粒子与后处理做不到 100%）。

## 五之六、第三版：换成 MD 的真几何（同日，用户确认后实施）

新增 `SummonMeshes`（读 `runtime\summon_mesh\*.json`：顶点/UV/索引，按自身半径自动缩放）
和 `ClientSummonEffects.mesh()`（自绘三角面、贴抽出来的贴图、**正反各画一遍**以绕开背面剔除）。

| 类型 | 用的 MD 网格 | 动作 |
| --- | --- | --- |
| 融合 | `fxl_dithit_Ring_001`（冲击环） | 快速张开 + 淡出（MD 的融合漩涡是粒子，这里先用冲击环 + 紫色贴图层近似） |
| 同调 | `SynchroCircle01/02/03`（**按等级换档**） | 高速自转 + 微升 + 等级数字浮空 |
| 超量 | `XYZInMesh01`（螺旋锥） | 从下往上升、边升边转（金） |
| 连接 | `SummonLinkTrail02`（螺旋飘带） | 自转张开 + 按 Link 数放射箭头（红） |
| 仪式 | `SummonRitualCylinder02`（漏斗光柱） | 从地面升起 + 纵向拉伸（蓝） |
| 灵摆 | `SummonPendulumChoker02`（八边形门） | 缓慢上升 + 自转（绿） |

网格目标宽度 6～9.5 格（按各自半径自动换算），仍然叠在"素材吸入 → 中央大演出 → 怪兽从中央落地"
这套流程里。贴图暂时沿用前面抽的那批；**每块网格原配的材质/贴图还没逐块对应**
（UnityPy 可以从 MeshRenderer 反查，作为下一步）。

- **音效**（用户要求先跳过）：`sound_assets_se_eff_summon_*` 已确认能解出 wav（召唤相关 29 个包 1.5 MiB → wav 14.4 MiB → ogg 约 1.6 MiB）。
- 材料段演出（材料卡的吸入/颜色粒子）、同调等级数字与星环分档、连接箭头拖尾、超量叠放材质、
  融合按材料数换法阵层数；`StrongSummon`（重落地）与 `MonsterCutin`（专属 cut-in）。
- 目前演出是叠加在原有的卡片飞行上（在场地中央播法阵），还没做"先演材料段再落地"的重排。

## 六、想自己看效果

开局后正常做一次融合/同调/超量/连接召唤（比如融合召唤一只融合怪兽），
落地瞬间应看到场地中央出现该类型的法阵 + 一圈黑场。
嫌太亮/太长就改 `ClientSummonEffects` 顶部的 `EFFECT_MS` / 配色表；`-Dmdpromod.summonFx=false` 关掉。

## 七、第四版：节奏照 MD 实测值 + 光柱重做（同日，用户反馈两点）

用户反馈：**①整个流程还是太快，要和 MDPro 一致；②十字光柱太突兀。**
用户还给了关键提示：**"可以查音效长度来推演出时长"**——MD 的音效和演出是同一段节奏，
所以音效长度就是演出长度的实测值。

### 7.1 时长从哪来（`tmp\measure_summon_se.py`，读 bundle 里的 wav 量）

| 音效包 | 文件 | 时长 | 用途 |
| --- | --- | --- | --- |
| `se_summon_fus/sync/xyz/ritual/link_material` | `SE_SUMMON_*_MATERIAL` | **全部 2.00 s** | 素材被吸进中央（召唤段开头） |
| `se_summon_advance` | `SE_SUMMON_ADVANCE` | **1.34 s** | 材料段（对上 MD 代码里的 `WaitForSeconds(1.33f)`） |
| `se_eff_summon_fusion` | `SE_EFF_SUMMON_FUSION` | **3.31 s** | 融合召唤段 |
| `se_eff_summon_synchro` | `SE_EFF_SUMMON_SYNCHRO` | **3.99 s** | 同调召唤段 |
| `se_eff_summon_xyz` | `SE_EFF_SUMMON_XYZ` | **3.87 s** | 超量召唤段 |
| `se_eff_summon_link` | `SE_EFF_SUMMON_LINK` | **6.77 s** | 连接召唤段 |
| `se_eff_summon_ritual` | `SE_EFF_SUMMON_RITUAL` | **3.69 s** | 仪式召唤段 |
| `se_eff_summon_pendulum` | `SE_EFF_SUMMON_PENDULUM` | **2.42 s** | 灵摆召唤段 |

（另有 `xyz2` 2.56 s / `link2` 1.61 s 两个叠层变体、`se_eff_summon` 2.81 s 通用、`max` 6.15 s 是 Rush 的、
`pendulum_set` 6.16 s 是灵摆放置，本次没用到。）

**材料段 1.33 s 也顺手复核了**（`tmp\timeline_duration_probe.py`）：本机装的那批
`MasterDuel\Timeline\Summon\SummonFusion\*\ShowUnitCard0N` 就是材料段那条时间轴，
**所有轨道都在 1.333 s 结束**（MagicCircle02/03/04、Thunder01~03、Dust01/02、CardBack01、ParticleBG…），
和 MD 代码里的 `WaitForSeconds(1.33f)` 分毫不差。另外那条时间轴的 `SoundPlayableAsset.startLabel`
是 `SE_EV_045_SPECIAL`（不是 `SE_SUMMON_*_MATERIAL`）——所以那 2.00 s 的材料音效是**召唤段里的**
"吸素材"音，不是第三个段落。`TimeLineManager` 的结构也对得上：
`SyncSummonTimeline()`（同调/超量/连接/仪式）在 1.33 s 等待**之前**就启动了，和材料段**并行**；
融合走 `ShiftToSummonTimeline()`，是 1.33 s **之后**才启动。所以我们统一成"材料段 → 召唤段"
（融合和 MD 完全一致：1.33 + 3.31 = 4.64 s；其余四类比 MD 多的那 1.33 s 是材料段，MD 那边是叠着放的）。

`SUMMON_MS` 表也因此从"写死的 3200 ms"换成**每类各自的音效长度**（新增 `SummonTiming`）：

| 类型 | 材料段 | 召唤段 | 落地 | **合计** | 改前 |
| --- | --- | --- | --- | --- | --- |
| 融合 | 1.33 s | 3.31 s | 0.60 s | **5.24 s** | 3.20 s |
| 同调 | 1.33 s | 3.99 s | 0.60 s | **5.92 s** | 3.20 s |
| 超量 | 1.33 s | 3.87 s | 0.60 s | **5.80 s** | 3.20 s |
| 连接 | 1.33 s | 6.77 s | 0.60 s | **8.70 s** | 3.20 s |
| 仪式 | 1.33 s | 3.69 s | 0.60 s | **5.62 s** | 3.20 s |
| 灵摆 | 1.33 s | 2.42 s | 0.60 s | **4.35 s** | 3.20 s |

节奏图（含下面 7.2 的贴图 alpha 剖面实测）：`docs\summon-timing-and-column-2026-10-14.png`；
光柱外观对比（用渲染器同一套混合公式重画）：`docs\summon-column-preview-2026-10-14.png`。

配套改动：

- **整段演出放完才继续播后面的事件**（服务端 `holdMs`）：以前做完召唤 0.6 s 就接着播下一条事件，
  于是动画还在放、别的卡已经在飞；现在和 MD 一样按整段时长按住（`nextAt` 用 `max(duration, holdMs)`）。
- 黑场先**挪到落地那一下**（MD 是 `StrongSummonLand` 之后才 `BlackOut(0, 0.3f)`）——
  随后用户反馈"黑场不要"，整个删掉，见 7.4。
- 法阵自转改成**按毫秒算角度**（召唤段 62 °/s、材料段 110 °/s）：以前按进度比例转，
  动画一拉长就会"越转越猛"。
- 出场曲线重排：0→35% 立起（淡入 + 放大，融合 3.31 s 的话正好 1.16 s，与 MD 材料段那些
  `MagicCircle` 动画轨道 1.167 s 对齐）→ 78% 保持匀速转 → 78%→100% 外扩淡出，
  正好和"最后一刻从中央落下"接上。

### 7.2 "十字光柱突兀"的真正原因（同样是"不透明贴图"）

光柱原来贴的是 `pendulum_spot`。量了一下这几张贴图的 alpha 通道：

```
black_circle    alpha 0..236     ← 真正的软光（径向渐隐）
fusion_010/011  alpha 0..241     ← 带透明
synchro_line    alpha 0..253
link_gate       alpha 0..255
ritual_circle_01/03、ritual_fire_01、pendulum_mask、pendulum_spot  alpha = 254/255  ← **完全不透明！**
```

MD 里这几张是**加法**（Additive）用法：暗底 + 亮图案，靠加法让暗底等于"什么都没画"
（`pendulum_spot` 尤其坑：它只是 8 像素高的灰阶条，最亮才 135/255，当光柱贴图就是"变暗条"）。
MC 这边是普通 alpha 混合，暗底会被原样糊上去——所以光柱成了**硬邦邦的深色矩形**，
仪式/灵摆那两档法阵也会糊上黑方块（那三张的 alpha 实测恒为 254，不是"看不到"而是"糊了一片黑"）。
这才是"突兀"的根源，不是透明度调得不够低。

两处修：

1. 光柱改用 `black_circle`（真 alpha 的径向软光），**UV 只取贴图中间那条窄带**（v 0.42~0.58）
   → 左右两边也是渐隐的、切不出直边；配合**底宽顶窄**（底 1.85 / 顶 0.80 格）+ 顶点色纵向渐隐
   （底部 alpha 上限 88、顶部 0）+ 纵向"长出来/收回去"（6%→36% 长、80%→100% 收）。
   `pendulum_spot` 本身是「8 像素高的灰阶条」，最亮也才 135/255，乘上顶点色就是一根**变暗**的矩形
   ——`docs\summon-column-preview-2026-10-14.png` 里两张图是同一套混合公式画出来的对照。
2. `SummonFxTextures` 里给那 5 张**不透明加法贴图**在载入时**用亮度烘成 alpha**
   （`LUMINANCE_TO_ALPHA`：黑底→全透明、白色图案按亮度发光、RGB 留白交给顶点色上色）。
   顺带把仪式/灵摆那两档法阵从"黑方块"变成正常发光图案。

### 7.3 验证与产物

| 项 | 结果 |
| --- | --- |
| 构建 | `gradlew build --offline` → 0 error |
| 测试 | `DuelVisualState passed 71 assertions` |
| 日志 | 服务端那行现在带时长：`Summon FX: kind=2 card=73542331 materials=2 show=3870ms at field center` |
| 产物 | `mdpromod-0.1.0.jar` 2,372,185 字节，SHA-256 `4F467ED4BF5040655FA4ACF2CB0A4C42F30616FCAA8C82B8ABFFACADC4645E2C`，已部署到客户端 mods |
| 备份 | `D:\MDProMod\tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0855`（部署前那一版） |

要再调快/调慢：只改 `SummonTiming.SUMMON_MS`（六个数，毫秒）即可，落地会跟着走。

### 7.4 黑场整个去掉（用户要求：黑场不要）

上面 7.1 那版把黑场挪到了"怪兽落地前 0.48 s"，用户看完的结论是**干脆不要黑场**。
所以整块黑场代码删了，不是调成 0：

- `ClientSummonEffects`：删掉 `FADE_OUT_MS / FADE_HOLD_MS / FADE_IN_MS / FADE_LEAD_MS`、
  `fadeStart` / `fadeAtMs`、`fade()`、`renderFade()`，以及 `play()` 里的排期那一行。
- `ClientForgeEvents.onRenderGui`：去掉 `renderFade` 调用（这个事件只剩手牌 HUD 用）。

现在召唤演出全程不压黑，只靠法阵 + 光柱 + 网格 + 类型细节 + 落地翻转出效果。
以后想加回来：MD 的 `CameraManager.BlackOut(0, 0.3f)` 是在 `StrongSummonLand` 回调里调的
（即落地那一刻），不要在开场压黑。

产物更新：`mdpromod-0.1.0.jar` 2,371,478 字节，SHA-256
`771C85711A52D30757B63ABF0D318CDAD396C6C2FDF7CF428FA6B725976D7CAD`，
备份 `tmp\mdpromod-0.1.0.jar.prev-2026-10-14-0859`。
