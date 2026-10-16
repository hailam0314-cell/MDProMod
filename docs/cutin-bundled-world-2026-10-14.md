# 全量低清 Spine 与场地中央播放（2026-10-14）

此文记录当前实现，替代 `cutin-spine-2026-10-14.md` 中早期的外置目录、GUI 覆盖层和素材统计。历史调研中的“原素材本来就是散的”已被后续引用链排查推翻。

## 实际结果

| 项目 | 数量或字节数 |
| --- | ---: |
| MDPro MonsterCutin 目录 | 498 |
| 可作为 Spine 播放的演出 | 497 |
| Spine PNG 页 | 639 |
| 非 Spine 演出的 PNG | 4 |
| 全部导出 PNG 原大小 | 999,764,731 |
| 全部低清 PNG 大小 | 39,248,768 |
| PNG 减少比例 | 96.07% |
| 全部低清素材（含 JSON、atlas、清单） | 108,470,661 |
| 模组内 Spine 素材解压后大小 | 108,316,699 |
| 最终模组 JAR | 67,848,866（约 64.71 MiB / 67.85 MB） |

最终包：`D:\MDProMod\mod\build\libs\mdpromod-0.1.0.jar`。
SHA-256：`786891880EF2718C9CE4344AE454A7CA91B9C8CE6658B6C43B988D00B91E29DD`。

`27204311` 使用 Unity SpriteRenderer/AnimationClip/Timeline，没有 Spine 骨架。4 张绑定纹理已压缩并保存于 `runtime/cutin_low/27204311`，未加入 Spine 名录或假造可播放动画。

## 提取与压缩

稳定脚本放在 `tools/cutin_pipeline`。只读原始 AssetBundle，按已绑定的 Director → Timeline → 动画引用 → SkeletonDataAsset → atlas → 材质 → Texture2D 的对象身份导出，禁止用同名对象覆盖实际引用。未绑定备选轨道被剔除、重复 Director 去重、91073013 的连续片段合并成 1.6 秒；最终每套只有一个有效 rig。

PNG 的宽、高各除以 8：4096→512、2048→256；更小的分页也按同一比例处理。639 张 Spine PNG 均不超过 512。PMA 页直接重采样已预乘的各通道，普通 RGBA 页按透明度重采样；保留骨架和 atlas 的逻辑坐标，运行时用逻辑尺寸生成 UV，用真实低清 PNG 上传。

14 套中的 17 页原本就经过 Unity 导入缩放，逻辑 atlas 尺寸和物理贴图尺寸不同。这些页只在真实材质绑定唯一、符合可识别的 NPOT/mip 缩放规则、区域不越界时接受，详见 `runtime/cutin_import_scaled_pages.json`，没有跨变体猜图。

10158145 与 14509651 的 `_noait_` atlas 是它们实际使用的合法图集，不能单凭文件名剔除；与青眼目录中未被引用的同名资源已区分。

## 模组内资源

Gradle 的 `bundleCutins` 只复制 manifest 引用的低清文件到 `assets/mdpromod/cutin/<卡号>`，检查缺文件并同步清理旧构建输出。`data/mdpromod/cutins.txt` 从成功导出的名录生成，包含 497 个卡号和持续时间。

`CutinAssets` 默认直接从 JAR 读取，不需要外置 D 盘目录。开发时仍可用 `-Dmdpromod.cutinRoot=目录` 覆盖。骨架缓存最多 16 套，动态纹理缓存最多 64 页，PNG 在实际播放时才上传。

骨架 JSON 和 atlas 均明确按 UTF-8 解析。13455674 的附件名含全角 `ｒ`，使用 Windows 默认编码会使 JSON 与 atlas 名称无法匹配；该问题已在正式加载器和离线预览器中修复。

## 世界中的演出

`ClientCutinEffects` 接到原有 `SummonEffect` 的 phase 2 后，以 `fieldEntityId` 找到决斗场实体，锚定场地中央。宽度从布局的 16×14 方块尺寸按观察角度计算，图像保持比例，底边高于场面；不再根据窗口大小绘制 GUI 动画。

每个客户端将演出面朝向自己的相机，包含俯仰角，因此两侧玩家与上方观众都能看到正面，不会背面消失或镜像。服务端原有事件向同维度场地周围 128 格广播，双方及附近观众均接收。每块场地独立排队，不会被另一场地的法阵或演出阻塞。

保留召唤节奏：材料型召唤接在法阵后、和落地并行；普通召唤先演出再落地。渲染接在世界透明阶段，保留深度遮挡、不写深度、关闭背面剔除，结束时恢复渲染状态。Spine 的插槽顺序、裁剪、PMA 淡入和普通/加法/乘法/屏幕混合均得到处理。

## 验证

- 全量资源审计：498 个目录，0 失败；PNG 完整性、宽高除以 8、逻辑区域边界、非图片字节一致、目录无多余文件。
- `gradlew.bat build --offline` 成功，包括现有测试及 Forge reobf。
- 从另一个工作目录、无外置根目录参数运行最终 JAR 的 `CutinSelfTest --all`：497/497 通过，检查加载、动画采样、有限顶点/UV、UV 范围和可见帧。
- 216 个几何视角检查通过：双方、侧面、斜角、上方及旋转场地；覆盖场宽、正面朝向、中心和底边高度。
- Spine 裁剪检查通过：裁剪面积、范围、UV 插值和 endSlot。
- 已实际查看低清离线动画帧：原四套“碎图”、NPOT 导入缩放样本和两套合法 `_noait_`。

尚未做双 Minecraft 客户端实机画面验证。512/256 纹理铺满场地后细节会更软，这是本次目标分辨率的结果。

复现：

```powershell
python tools\cutin_pipeline\build_full_cutins.py
python tools\cutin_pipeline\audit_full_cutins.py
python tools\cutin_pipeline\make_cutin_roster.py
cd mod
.\gradlew.bat build --offline
java -cp build\libs\mdpromod-0.1.0.jar net.mdpromod.spine.CutinSelfTest --all
```

源目录可用 `MDPRO_CUTIN_SOURCE` 覆盖，项目根可用 `MDPROMOD_PROJECT` 覆盖。原始导出、低清导出、报告分别位于 `runtime/cutin`、`runtime/cutin_low`、`runtime/cutin_*json`，自检日志在 `tmp/cutin-packaged-selftest.log`。
