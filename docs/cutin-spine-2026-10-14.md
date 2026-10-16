# 王牌怪兽登场演出（MonsterCutin）：调研 + 实时 Spine 播放（2026-10-14）

本轮做的是 MD 里俗称 cut-in 的那段"王牌怪兽登场"演出。和之前的六类召唤法阵不同，
它**不是**场地里的特效，而是一整段 2D 叠加演出。结论：**可以实时播 MD 自己的 Spine 骨骼动画**，
已经落进模组，并且有一条不依赖游戏就能自检的验证路径。

## 一、MD 是怎么做这套演出的（498 套全量实测）

### 1.1 入口是"预制体上的时间轴"，不是"目录里的资源"

一套 cut-in 就是 `MonsterCutin\<卡号>\` 下的一堆 AssetBundle。真正的入口链是：

```
GameObject 上的 PlayableDirector
  -> TimelineAsset
     -> SpineAnimationStateTrack 的 clip
        -> AnimationReferenceAsset
           -> animationName + SkeletonDataAsset
              -> skeletonJSON(TextAsset) + SpineAtlasAsset -> atlas(TextAsset) -> 分页 PNG
```

**必须按这条链走**，不能"把目录里所有资源都导一遍"：一个目录里经常堆着不相干的 rig。
最典型的例子是 `89631139`（青眼白龙）——目录里除了龙的 `P3801`，还塞了另一套 `P13571`
（一个紫发角色），**44 个目录共享这套 `_noait_` 资源**。照目录 dump 就会渲染出完全错误的怪兽。

### 1.2 全量体检结果（`tmp/cutin_check.json`）

| 项 | 结果 |
| --- | --- |
| 解析成功 | **498 / 498** |
| 图集分页与贴图全部对上的 | **497**，对不上的 **0** |
| 演出时长 | 1.6 s 的 **486** 套（其余 1.62~1.73 s） |
| 非 Spine 的 | **1** 套：`27204311` 是纯 Timeline 驱动网格/材质的岩石演出 |
| 骨架规模 | 骨骼中位 109、最大 **698**；插槽中位 33 |
| 约束 | IK **353** 套、路径 **432** 套、变换 214 套 |
| 图集 | **99% 多页**（中位 7 页，最多 155 页） |
| 动画名 | `animation` 422、`animation_kde` 53、其余零星 |

### 1.3 三个坑

1. **`pma:true`（预乘 alpha）**：327 套有。分页颜色已经乘过 alpha，
   必须用 `ONE, ONE_MINUS_SRC_ALPHA` 混合；拿普通 alpha 混合画会在半透明边缘发白。
2. **同一份图集有低清/高清两版**（例如 Obelisk 本地只有 2048 贴图，却有 4096 和 2048 两份 atlas）。
   要按"分页尺寸能不能对上本地贴图"挑版本——选对之后 497 套全部命中。
3. **区域附件的尺寸来自 Spine JSON 的 `width/height`**，不来自图集，
   所以 atlas 里的 `scale:` 行（libGDX 本身不解析它）对成品尺寸没有影响；但它决定了
   分页像素和 JSON 尺寸是不是同一套，挑版本时仍然要看。

## 二、实现路线：vendored 官方运行时 + 自己写渲染层

### 2.1 为什么不用自己写播放器

87% 的骨架用了路径约束、71% 用了 IK——从零实现等于把整套 Spine 约束系统重写一遍，
风险和工作量都大一个量级。所以走"**官方运行时算数学 + 我们负责渲染**"。

### 2.2 关键技巧：用同名的 `Texture` 桩顶掉 libGDX

spine-libgdx 的数据层（`SkeletonJson` / `Skeleton` / 动画 / 约束）只依赖
`com.badlogic.gdx.utils`、`math`、`files` 和 `graphics.Color`，以及一个 `Texture`——
而 libGDX 真的 `Texture` 一构造就要 GL 上下文。

做法：把 `com.badlogic.gdx.graphics.Texture` / `Pixmap` **自己写一份同名桩**，
放在 classpath 前面，libGDX 的 `TextureAtlas` / `TextureRegion` 就会链接到它。
于是 UV、旋转区域、`scale`、多页图集这些算法**仍然是官方的**，我们只接管"贴图在哪、多大"。

### 2.3 打包

`mod/build.gradle` 里：

```groovy
compileOnly files('libs/gdx-1.12.1.jar', 'libs/spine-libgdx-4.2.12.jar')
...
tasks.named('jar', Jar).configure {
    from(zipTree('libs/spine-libgdx-4.2.12.jar')) { include 'com/esotericsoftware/spine/**' }
    from(zipTree('libs/gdx-1.12.1.jar')) {
        include 'com/badlogic/gdx/*.class'
        include 'com/badlogic/gdx/utils/**'
        include 'com/badlogic/gdx/math/**'
        include 'com/badlogic/gdx/files/**'
        include 'com/badlogic/gdx/graphics/**'
        exclude 'com/badlogic/gdx/graphics/Texture*.class'   // 用我们的桩
        exclude 'com/badlogic/gdx/graphics/Pixmap*.class'
    }
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}
```

jar 从 2.37 MB 涨到 4.20 MB。打完确认过：`com/badlogic/gdx/graphics/Texture.class` 只出现一次（是我们的）。

### 2.4 代码落点

| 位置 | 作用 |
| --- | --- |
| `src/main/java/com/badlogic/gdx/graphics/Texture.java` | 桩：只读 PNG 头拿宽高，记住 `pma`，不碰 GL |
| `net/mdpromod/spine/CutinAssets` | 读 `cutin.txt`、装图集/骨架、缓存（**不依赖 MC**） |
| `net/mdpromod/spine/CutinRoster` | 随包的清单 `data/mdpromod/cutins.txt`（卡号 + 时长毫秒） |
| `net/mdpromod/spine/CutinSelfTest` | 脱离游戏的 `main`，可直接对模组 jar 跑 |
| `net/mdpromod/ClientCutinTextures` | 分页 PNG → `DynamicTexture`（像素原样上传，`setFilter(true,false)`） |
| `net/mdpromod/ClientCutinEffects` | 逐帧推进 + 换算屏幕坐标 + 按 (贴图, 混合) 分组发三角形 |

渲染走 GUI 覆盖层，用 `RenderSystem` + `BufferBuilder` 直接发
`POSITION_COLOR_TEX` 三角形：`RenderStateShard` 的常量在 1.20.1 是 `protected`，
而自己发顶点反而能把混合模式完全控住（普通 alpha / 预乘 / 加法三档）。

素材目录：`-Dmdpromod.cutinRoot`（默认 `D:\MDProMod\runtime\cutin`）；
总开关 `-Dmdpromod.cutin=false`。

## 三、触发与节奏

### 3.1 服务端（`DuelAnimationScene`）

新用了 `SummonEffect` 的 **phase 2** 表示"放王牌登场演出"（协议没改，只是复用）。

- **正规出场**（融合/同调/超量/连接/仪式/灵摆，材料匹配类型）：法阵演出照旧，
  **cut-in 接在法阵之后、和落地同时开始**。
  起播时刻交给客户端排（它知道法阵从哪一刻开始，`法阵开始 + 法阵时长 - 落地时长`），
  服务端只负责把整段按住：`holdMs = max(法阵时长, 法阵时长 + 演出时长 - 落地时长)`。
- **非材料型**（通常/上级召唤，或材料类型不匹配的特召）：**先放演出，落地压后演出那么长**
  （对齐 MDPro3 的 `cutin ? 1.6f : 0`），`pendingDelayMs = 演出时长`。

> 注：MDPro3 的代码里正规出场那条路径**不播** cut-in（只预热缓存），
> 这里的做法是按实际观感定的：正规出场也要播，接在法阵之后、与落地并行。

### 3.2 客户端

收到 phase 2 时，如果同一块决斗盘上法阵（phase 1）正在放，就排到落地那一刻；
否则立刻开始。**同一时刻只播一套**（和 MD 的静态锁一致）。

## 四、验证

### 4.1 脱离游戏的自检（可对模组 jar 直接跑）

```
java "-Dmdpromod.cutinRoot=D:\MDProMod\runtime\cutin" ^
     -cp mod\build\libs\mdpromod-0.1.0.jar net.mdpromod.spine.CutinSelfTest 89631139
```

它会逐帧走动画并打印包围盒漂移（0 就说明停在 setup pose）。已确认
89631139 / 46986417 / 10000020 等都能装载、且动画确实在跑（位移 300~1400 单位）。

### 4.2 离线预览器（进游戏之前先看效果）

`tools/spine_preview`（Java + Java2D）能把任意一套 cut-in 渲成 PNG 序列；
它和模组共用同一套"官方运行时 + 桩"的装载路径，所以预览对了，游戏里就应该对。

```
javac -encoding UTF-8 -cp "out\shim;libs\gdx-1.12.1.jar;libs\spine-libgdx-4.2.12.jar" ^
      -d out\preview src\preview\CutinPreview.java
java -cp "out\shim;out\preview;libs\gdx-1.12.1.jar;libs\spine-libgdx-4.2.12.jar" ^
     preview.CutinPreview "D:\MDProMod\runtime\cutin\89631139" "D:\MDProMod\tmp\cutin_preview\89631139" 16
```

第 4 个参数写 `setup` 可以只渲染 setup pose（不播动画）——排查"部件位置不对"时很有用。

### 4.3 已渲染确认

完整成形的：青眼白龙 `89631139`、黑魔术师 `46986417`、欧西里斯天空龙 `10000020`、
守护者·奇美拉 `11321089`（都是 setup pose 就是完整的）。

部件散开的：欧贝利斯克 `10000000`、卡雷多哈特 `28226490`、阿莱斯哈特 `48626373`、
米德拉什 `94977269`。**这四套的 setup pose 本身就是散的**，和动画无关：
`setToSetupPose()` 渲染出来同样是散的。图集与附件名**一一对应**（无缺件、无多余），
所以不是素材不全。见下面"待确认"。

## 五、待确认 / 已知问题

1. **"散架"的 4 套：已经把能排查的都排除了，需要一张参考图才能继续。**

   现象：调试用的 8 套里，青眼白龙 / 黑魔术师 / 天空龙 / 奇美拉渲染成完整的怪兽；
   欧贝利斯克 / 卡雷多哈特 / 阿莱斯哈特 / 米德拉什的部件是**散开**的。

   已经用实验排除的（每条都留了脚本）：

   | 怀疑 | 怎么验的 | 结论 |
   | --- | --- | --- |
   | 我们少渲染了一套 rig | dump prefab 的 GameObject/组件/SkeletonAnimation/时间轴绑定 | 每套 prefab **只有一套** SkeletonAnimation，用的就是我们抽的那套骨架 |
   | 运行时丢了轨道 | `TimelineCountProbe`：数解析出的 timeline（欧贝利斯克 33 条：9 旋转/1 缩放/2 位移/4 translateX/4 translateY/13 颜色），和 JSON 完全对得上 | **一条不少** |
   | 约束没解 | 把 JSON 里 2 条 `path` 约束清空后重渲，包围盒从 -1064..1082 变成 -1215..1110；把 `inherit` 去掉后头饰位置也变了 | **约束和 inherit 都在生效** |
   | 图集挑错版本 / 裁剪错位 | `check_region_placement.py` 按官方 `updateRegion()` 公式逐附件算四角；`check_atlas_match.py` 比对图集区域名与附件名 | 全部正常、**一一对应**（无缺件、无多余） |
   | setup pose 算错 | 我们算出的 setup 包围盒 = 骨架自己记录的画板，**精确到小数**（欧贝利斯克 x[-969.1,976.0] y[-1179.0,962.6] vs 声明 x=-969,y=-1179,w=1945,h=2141.54） | setup pose 就是 Spine 编辑器算出的那个 |
   | 取景（相机）不同 | `check_screen_effect.py`：所有卡的 `camera2DSize=5.0`、`viewType=3D`、位置角度**完全一样** | 不是取景问题 |
   | 插槽被隐藏 | 读 `slots` 的 rgba 关键帧：所有插槽都是 0→1→1→0，一起淡入淡出 | 那几块不是"本该看不见" |
   | 素材来自别处 | 检查 `MonsterCutin2`（自制包）里有没有这几张 | 没有 |

   **所以：按本机这份 `MonsterCutin` 数据，我们渲染出的就是数据本身的样子**——
   而且这几套 rig 的 setup pose 本身就是"炸开"的（图集里也确实是抽象碎片，不是整只怪兽）。

   要继续定位，需要一张**游戏内的参考图**（哪张卡都行，比如欧贝利斯克或卡雷多哈特），
   有了它就能直接比对部件位置，判断是
   (a) 本机这份 dump 和现在 MD 用的素材不是同一版，还是
   (b) 还有某个我们没覆盖到的东西。

   自己出对照图：

   ```
   java -cp "tools\spine_preview\out\shim;tools\spine_preview\out\preview;tools\spine_preview\libs\gdx-1.12.1.jar;tools\spine_preview\libs\spine-libgdx-4.2.12.jar" ^
        preview.CutinPreview "D:\MDProMod\runtime\cutin\10000000" "D:\MDProMod\tmp\cutin_preview\10000000" 20
   ```

   （末尾加 `setup` 则只渲 setup pose，不播动画。）
2. **许可证**：官方 Spine Runtimes 的授权要求持有 Spine 编辑器许可（MDPro3 自身也捆了 spine-unity）。
   若要规避，替代方案是自研播放器，但要把 IK/路径/变换约束都实现——不推荐。
3. **素材体积**：`MonsterCutin` 原始 **2.6 GB**（498 套）。目前按需抽到 `runtime\cutin\`，
   进包或全量抽取前先想好压缩策略（可沿用 `closeup_128` 那套流程）。
4. **`27204311`** 是唯一非 Spine 的 cut-in，清单里已跳过，需要的话单独做兜底。

## 六、怎么扩素材

```powershell
# 单张
python tmp\extract_cutin.py 28226490
# 全部 498 套（约 2.6 GB，建议先想好放哪）
python tmp\extract_cutin.py --check      # 先体检，看有没有对不上的
```

清单要跟着重生成（模组靠它判断"这张卡有没有演出、演出多长"）：

```powershell
python tmp\make_cutin_roster.py           # -> mod\src\main\resources\data\mdpromod\cutins.txt
cd D:\MDProMod\mod; .\gradlew.bat build --offline
```

## 七、本轮产物

| 项 | 位置 |
| --- | --- |
| 抽取管线 | `C:\Users\郑杰田\Documents\ChatGPT\mdpromod\tmp\extract_cutin.py` |
| 体检 / 名录 / 清单 | 同目录 `cutin_check.json`、`cutin_roster.txt`、`make_cutin_roster.py` |
| 离线预览器 | `D:\MDProMod\tools\spine_preview\` |
| 导出的素材 | `D:\MDProMod\runtime\cutin\<卡号>\`（本轮 8 套，含卡组里那 4 张） |
| 预览图 | `D:\MDProMod\tmp\cutin_preview\` |
| 模组包 | `mdpromod-0.1.0.jar` 4,200,446 B，已部署到客户端 mods（旧包备份在 `D:\MDProMod\tmp\`） |

## 八、已定位的“碎图”根因（2026-10-14 继续排查）

此前把四套碎图误判成了“Spine setup pose 本身就是散的”。重新沿原始 AssetBundle 的真实引用链核对后，确认主要问题发生在抽取器：

- 同一卡号内存在多个同名 `SkeletonDataAsset` / `SpineAtlasAsset` / `Texture2D` 变体；名称不是唯一键。
- `spine_tracks()` 只保存 `SkeletonDataAsset` 名称，`skeleton_data_assets()` 再按名称覆盖，导致 Timeline 指向的骨架 JSON 被替换成另一套。
- `collect_textures()` 以 `(name,width,height)` 覆盖同名纹理；`choose_atlas()` 在尺寸不匹配时还会优先选择更大的 atlas。
- 结果是 JSON、atlas 和 PNG 来自不同变体。欧贝利斯克还出现了明确的 `4096×4096 atlas + 2048×2048 PNG` 错配，libGDX 按实际 PNG 尺寸算 UV 后会直接采样错位。

已修正临时抽取器 `C:\Users\郑杰田\Documents\ChatGPT\mdpromod\tmp\extract_cutin.py`：

1. 用 UnityPy 解析后的目标对象身份 `(assets_file.name,path_id)` 作为 `SkeletonDataAsset` 键；Timeline 的 `PPtr` 解析到同一身份后再取骨架。
2. 每个 `SpineAtlasAsset` 从 `materials -> _MainTex` 保存自己的分页纹理，禁止跨变体按名字兜底。
3. atlas 声明尺寸与 PNG 实际尺寸必须 exact；不匹配不再算“完整”。
4. 不再只按 atlas 文本去重，避免相同文本但不同材质贴图的变体被合并。

四套资源已重新抽取并离线预览，setup pose 均恢复为完整画面：

- `10000000`：欧贝利斯克
- `28226490`：卡雷多哈特
- `48626373`：阿莱斯哈特
- `94977269`：米德拉什

因此当前无需修改 Spine 约束、mesh 顶点或 Minecraft GUI 渲染层；先用修正后的抽取结果重新打包资源即可。
