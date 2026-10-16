"""把 MDPro 的 BGM bundle 转成 Minecraft 用的 Ogg Vorbis，并生成模组清单。

和 `build_audio_assets.py`（SE）同一套路，但有两点不同：

* **保留立体声**。BGM 是 `SoundSource.MUSIC` + `Attenuation.NONE` 的非定位播放，
  不需要为了世界衰减而降成单声道；实测立体声 q0 和单声道 q3 体积相当，
  但保住了声场。
* 输出多一份 `data/mdpromod/bgm_tracks.txt`（键 + 中文标签），给"音乐设置"界面用。

只读源 AssetBundle；PCM 只在内存里，不落 WAV。
"""
from __future__ import annotations

from concurrent.futures import ProcessPoolExecutor, as_completed
from pathlib import Path
import argparse
import hashlib
import io
import json
import re
import subprocess
import sys
import time

sys.path.insert(0, r"D:\MDProMod\tools\unitypy")
sys.path.insert(0, r"D:\MDProMod\tools\audio_pipeline\deps")
import numpy as np  # noqa: E402
import soundfile as sf  # noqa: E402
import imageio_ffmpeg  # noqa: E402
import UnityPy  # noqa: E402

DEFAULT_SOURCE = Path(r"D:/MyCardLibrary/mdpro3/MDPro3_Data/StandaloneWindows64/MDPro3")

# 键里的固定词 → 中文标签片段
TOKENS = {
    "DUEL": "决斗", "NORMAL": "常规", "CLIMAX": "高潮", "KEYCARD": "王牌登场",
    "MENU": "菜单", "RETRO": "复古", "ALL": "完整", "PHASE": "", "A": "A 段",
    "B": "B 段", "C": "C 段", "DICERALLY": "骰子拉力",
}


def safe_name(value: str) -> str:
    value = re.sub(r"[^a-z0-9_./-]", "_", value.lower())
    if not value or ".." in value or "/" in value:
        raise ValueError(f"Invalid AudioClip name: {value!r}")
    return value


def label_for(key: str) -> str:
    """BGM_DUEL_EX_05_KEYCARD → 「EX05 · 王牌登场」这样的可读标签。"""
    name = key[4:] if key.upper().startswith("BGM_") else key
    parts = [p for p in name.split("_") if p]
    pieces: list[str] = []
    numbers: list[str] = []
    for part in parts:
        upper = part.upper()
        if upper.isdigit():
            numbers.append(upper)
            continue
        if numbers:
            pieces.append("".join(numbers))
            numbers = []
        if upper in TOKENS:
            text = TOKENS[upper]
            if text:
                pieces.append(text)
            continue
        # F01 / EX05 / DC02 / RATE01 这类分组名，保留原样
        if re.fullmatch(r"[A-Z]+\d+", upper):
            pieces.append(upper)
            continue
        # 键本身就是 SCREAMING_SNAKE，保持原样；否则 EX 会被 title() 成 Ex，
        # 后面「EX + 05 → EX05」的合并就匹配不上
        pieces.append(upper)
    if numbers:
        pieces.append("".join(numbers))
    # 分组前缀和数字被下划线拆开时并起来：EX + 05 → EX05、DC + 02 → DC02
    grouped: list[str] = []
    for piece in pieces:
        if (grouped and piece.isdigit() and re.fullmatch(r"[A-Z]{1,4}", grouped[-1])
                and grouped[-1] not in ("", )):
            grouped[-1] = f"{grouped[-1]}{piece}"
        else:
            grouped.append(piece)
    pieces = grouped
    # 数字跟在同名分类后面时并成一项，例如「常规 01」
    merged: list[str] = []
    for piece in pieces:
        if piece.isdigit() and merged and not re.fullmatch(r"[A-Z]+\d+", merged[-1]):
            merged[-1] = f"{merged[-1]} {piece}"
        else:
            merged.append(piece)
    return " · ".join(merged) if merged else key


def build_one(bundle: Path, output: Path, quality: str, reuse: bool):
    env = UnityPy.load(str(bundle))
    clips = [obj.read() for obj in env.objects if obj.type.name == "AudioClip"]
    if len(clips) != 1:
        raise ValueError(f"Expected one AudioClip, got {len(clips)} in {bundle.name}")
    clip = clips[0]
    key = clip.m_Name
    name = safe_name(key)
    target = output / f"{name}.ogg"
    source_hash = hashlib.sha256(bundle.read_bytes()).hexdigest()
    row = {
        "key": key, "name": name, "event": f"mdpro.{name}",
        "sound": f"mdpromod:mdpro/bgm/{name}", "file": target.name,
        "label": label_for(key), "bundle": bundle.name,
        "source_sha256": source_hash, "source_bundle_bytes": bundle.stat().st_size,
        "source_channels": clip.m_Channels, "source_rate": clip.m_Frequency,
        "source_duration": clip.m_Length, "output_channels": 2,
        "codec": "vorbis", "quality": quality,
    }
    samples = clip.samples
    if len(samples) != 1:
        raise ValueError(f"Expected one sample in {bundle.name}, got {list(samples)}")
    _, wav = next(iter(samples.items()))
    pcm, rate = sf.read(io.BytesIO(wav), dtype="float32", always_2d=True)
    if pcm.shape[1] != clip.m_Channels:
        raise ValueError(f"Channel mismatch in {bundle.name}")
    if not np.isfinite(pcm).all():
        raise ValueError(f"Nonfinite decoded samples: {bundle.name}")
    if not reuse or not target.exists():
        # 源就是立体声，直接编码；不做降混音
        interleaved = np.ascontiguousarray(pcm, dtype=np.float32)
        subprocess.run([
            imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-y",
            "-f", "f32le", "-ar", str(rate), "-ac", str(pcm.shape[1]), "-i", "pipe:0",
            "-c:a", "libvorbis", "-q:a", quality, "-map_metadata", "-1", str(target),
        ], input=interleaved.tobytes(), stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
    info = sf.info(target)
    if info.format != "OGG" or info.subtype != "VORBIS" or info.channels != pcm.shape[1]:
        raise ValueError(f"Invalid output codec: {target}")
    if info.samplerate != rate or abs(info.frames - len(pcm)) > 1:
        raise ValueError(f"Output timing mismatch: {target}")
    row.update({
        "wav_bytes": len(wav), "decoded_frames": len(pcm),
        "duration": info.frames / rate, "output_rate": rate,
        "stream": (info.frames / rate) > 20.0,
        "ogg_bytes": target.stat().st_size,
        "ogg_sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
        "peak": float(np.abs(pcm).max()) if len(pcm) else 0.0,
    })
    return row


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(r"D:/MDProMod"))
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--quality", default="0",
                        help="libvorbis 质量：0 约 106 MiB，1 约 126 MiB（全量 13509 秒）")
    parser.add_argument("--workers", type=int, default=6)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--reuse", action="store_true")
    args = parser.parse_args()

    resources = args.project / "mod/src/main/resources"
    output = resources / "assets/mdpromod/sounds/mdpro/bgm"
    output.mkdir(parents=True, exist_ok=True)
    pipeline = args.project / "tools/audio_pipeline"
    pipeline.mkdir(parents=True, exist_ok=True)
    (resources / "data/mdpromod").mkdir(parents=True, exist_ok=True)

    bundles = sorted(args.source.glob("sound_assets_bgm_*.bundle"))
    if args.limit:
        bundles = bundles[: args.limit]
    if not bundles:
        raise SystemExit(f"没有找到 BGM bundle：{args.source}")

    rows, failures = [], []
    started = time.monotonic()
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        futures = {pool.submit(build_one, bundle, output, args.quality, args.reuse): bundle
                   for bundle in bundles}
        for index, future in enumerate(as_completed(futures), 1):
            bundle = futures[future]
            try:
                rows.append(future.result())
            except Exception as error:  # noqa: BLE001
                failures.append({"bundle": bundle.name, "error": f"{type(error).__name__}: {error}"})
                print(json.dumps(failures[-1], ensure_ascii=False), flush=True)
            if index % 20 == 0 or index == len(bundles):
                print(f"{index}/{len(bundles)} 首，失败 {len(failures)}，"
                      f"{time.monotonic() - started:.0f}s", flush=True)

    rows.sort(key=lambda r: r["key"])
    keys = [r["key"] for r in rows]
    if len(set(keys)) != len(keys):
        raise ValueError("重复的 AudioClip 名会互相覆盖输出")

    report = {
        "source": str(args.source), "output": str(output), "quality": args.quality,
        "clips": len(rows), "failed": len(failures), "failures": failures,
        "duration_seconds": sum(r["duration"] for r in rows),
        "source_bundle_bytes": sum(r["source_bundle_bytes"] for r in rows),
        "wav_bytes": sum(r["wav_bytes"] for r in rows),
        "ogg_bytes": sum(r["ogg_bytes"] for r in rows),
        "seconds": round(time.monotonic() - started, 1), "assets": rows,
    }
    (pipeline / "bgm-assets.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=1), encoding="utf-8")
    events = {r["event"]: {"sounds": [{"name": r["sound"], "stream": r["stream"]}]}
              for r in rows}
    (pipeline / "sounds.bgm.generated.json").write_text(
        json.dumps(events, ensure_ascii=False, indent=1), encoding="utf-8")
    (resources / "data/mdpromod/bgm_tracks.txt").write_text(
        "# 由 tools/audio_pipeline/build_bgm_assets.py 生成，勿手改\n"
        "# 键<TAB>中文标签\n"
        + "".join(f"{r['key']}\t{r['label']}\n" for r in rows),
        encoding="utf-8")
    summary = {k: v for k, v in report.items() if k not in ("assets", "failures")}
    summary["ogg_mib"] = round(report["ogg_bytes"] / 1048576, 1)
    print(json.dumps(summary, ensure_ascii=False), flush=True)
    return bool(failures)


if __name__ == "__main__":
    raise SystemExit(main())
