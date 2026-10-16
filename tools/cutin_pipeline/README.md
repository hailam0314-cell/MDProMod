# MonsterCutin 资源管线

在 Python 3 中依次运行 `build_full_cutins.py`、`audit_full_cutins.py`、`make_cutin_roster.py`，然后到 `mod` 目录执行 `gradlew.bat build --offline`。UnityPy 和 Pillow 位于项目的 `tools/unitypy`；脚本自动加入该依赖目录。构建使用最多 4 个进程，仅读取选中材质实际绑定的图集页后再解码图片。

默认 MDPro 源目录为 `D:\MyCardLibrary\mdpro3\MDPro3_Data\StandaloneWindows64\MonsterCutin`，可用环境变量 `MDPRO_CUTIN_SOURCE` 覆盖。项目根默认取脚本所在项目，可用 `MDPROMOD_PROJECT` 覆盖。脚本只读源 AssetBundle，输出到项目内：

- `runtime/cutin`：选中预制体实际使用的原始 PNG、骨架、图集和清单。
- `runtime/cutin_low`：纹理宽高均除以 8 的最终资源包；4096→512、2048→256。PMA 图片直接重采样预乘通道。
- `runtime/cutin_validated_roster.json`：通过验证的 Spine 卡号和持续时间，可供模组清单生成器读取。
- `runtime/cutin_corpus_report.json`、`cutin_final_summary.json`：覆盖率和实际体积。
- `runtime/cutin_import_scaled_pages.json`：原生 Unity 导入时已经缩放过的图集页及对应逻辑、物理尺寸。

导出沿 Director→绑定 Timeline 轨道→AnimationReferenceAsset→SkeletonDataAsset→SpineAtlasAsset→Material→Texture2D 的对象身份匹配。未绑定的 SD/HD 备选轨道被剔除，重复 prefab 的相同片段去重，同一动画内连续且 clipIn 一致的片段合并。

图集逻辑坐标始终保留。某些原始材质的物理贴图经 Unity NPOT 转为最接近的 2 次幂，或者按 mip 等比缩小；这些情况只在材质纹理绑定唯一、缩放符合已识别导入规则、所有区域处于逻辑页面边界内时接受，并明确记录为 `unity_import_scaled`。运行时必须使用 atlas 的逻辑宽高生成 UV，不能改用压缩 PNG 的物理宽高。

`27204311` 是 Unity SpriteRenderer/AnimationClip/Timeline 演出，没有 Spine 骨架。其 4 张绑定纹理同样压缩至 `cutin_low/27204311`，通过 `nonspine.json` 记录；不生成假的 Spine 清单，不加入 Spine roster。

2026-10-14 全量结果：497 套 Spine、1 套非 Spine，643 张 PNG；PNG 从 999,764,731 字节降至 39,248,768 字节；包含骨架和图集的最终资源共 108,470,661 字节。审计验证全部 PNG 的完整性及 1/8 尺寸、原/低清非图片内容一致、atlas 区域边界，以及输出目录没有未引用文件。
