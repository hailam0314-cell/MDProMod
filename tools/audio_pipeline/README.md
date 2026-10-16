# MDPro 音效资源管线

`build_audio_assets.py` 从 `StandaloneWindows64/MDPro3/sound_assets_se_*.bundle`
读取 Unity AudioClip，用 FMOD 解码到内存，再用 FFmpeg 的 libvorbis 编码为
Ogg Vorbis quality 3。保留采样率和完整时长；双声道取均值转单声道，供 Minecraft
在决斗场中作位置音效播放。源包只读，模组中不生成或打包 WAV。

默认目标为 `mod/src/main/resources/assets/mdpromod/sounds/mdpro/se/`。
全部 2333 个 SE 都有独立资源，包含决斗、卡片专属、界面及尚未引用的原版 SE。
具体哪些 key 在决斗时触发，由运行时接入映射和决斗审计文档定义。

使用 Python 3.12。`tools/unitypy` 为项目已有 UnityPy/FMOD 依赖。
额外依赖安装到本目录的 `deps`，不进入模组包：

```powershell
python -m pip install --target D:\MDProMod\tools\audio_pipeline\deps soundfile imageio-ffmpeg
python D:\MDProMod\tools\audio_pipeline\build_audio_assets.py --workers 4
python D:\MDProMod\tools\audio_pipeline\audit_audio_assets.py
```

脚本生成：

- `audio-assets.json`：每个 key 的来源包、哈希、采样率、声道、时长和压缩大小。
- `sounds.generated.json`：Minecraft 声音注册表。event 形式为 `mdpro.se_card_draw_01`，
  音频资源形式为 `mdpromod:mdpro/se/se_card_draw_01`。
- `audio-assets-verification.json`：全量 OGG 解码校验结果。

2026-10-15 首次全量结果：2333/2333 成功，WAV 等价体积 1,491,942,524 bytes，
OGG 体积 69,859,425 bytes，约减少 95.32%。总时长 7,998.9678 秒。
全量解码校验核对 Vorbis、单声道、采样率、精确帧数、文件哈希及注册表覆盖。

注意：Unity 源 AssetBundle 已经压缩，不能把 OGG/WAV 的比例用于估算 AssetBundle
比例。libsndfile 的 Windows Vorbis 写入在长音频上会直接退出进程，因此编码使用
FFmpeg；libsndfile 只用于读回校验。

注册、音轨导出和最终包检查：

```powershell
python D:\MDProMod\tools\audio_pipeline\publish_registry.py
python D:\MDProMod\tools\audio_pipeline\export_timeline_cues.py
python D:\MDProMod\tools\audio_pipeline\audit_audio_integration.py --jar D:\MDProMod\mod\build\libs\mdpromod-0.1.0.jar
```

`publish_registry.py` 将生成的注册表合并进模组的 `sounds.json`，保留其他声音事件。
`export_timeline_cues.py` 以 `runtime/audio_timeline_census_full.json` 中逐对象遍历的结果
导出 `data/mdpromod/duel_audio_timelines.json`：244 个条目、448 个时间点、74 种音效。
Unity 的短触发框不限制音频长度，除非源对象明确要求停止或循环。

`audit_audio_integration.py` 核对源码与最终 JAR 的注册表、OGG、单声道 Vorbis 头、
音轨引用和 WAV 排除；这是资源一致性检查。完整解码和精确帧数检查由
`audit_audio_assets.py` 完成。具体触发行为及未实现的原版系统分别见
`docs/duel-audio-audit-2026-10-15.md` 和 `docs/duel-audio-integration-2026-10-15.md`。
