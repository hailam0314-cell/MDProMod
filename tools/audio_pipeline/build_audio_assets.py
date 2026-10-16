"""Read MDPro Unity AudioClip bundles and package mono Ogg Vorbis effects.

The source bundles are read only. PCM exists only in memory, never in the mod.
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

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parent.parent
sys.path.insert(0, str(PROJECT / 'tools' / 'unitypy'))
sys.path.insert(0, str(HERE / 'deps'))
import numpy as np
import soundfile as sf
import imageio_ffmpeg
import UnityPy

DEFAULT_SOURCE = Path(r'D:/MyCardLibrary/mdpro3/MDPro3_Data/StandaloneWindows64/MDPro3')
DEFAULT_OUTPUT = PROJECT / 'mod/src/main/resources/assets/mdpromod/sounds/mdpro/se'


def safe_name(value):
    value = re.sub('[^a-z0-9_./-]', '_', value.lower())
    if not value or '..' in value or '/' in value:
        raise ValueError(f'Invalid AudioClip name: {value!r}')
    return value


def build_one(bundle: Path, output: Path, compression: float, reuse: bool):
    env = UnityPy.load(str(bundle))
    clips = [obj.read() for obj in env.objects if obj.type.name == 'AudioClip']
    if len(clips) != 1:
        raise ValueError(f'Expected one AudioClip, got {len(clips)} in {bundle.name}')
    clip = clips[0]
    key = safe_name(clip.m_Name)
    target = output / f'{key}.ogg'
    source_hash = hashlib.sha256(bundle.read_bytes()).hexdigest()
    row = {'key': clip.m_Name, 'event': f'mdpro.{key}', 'sound': f'mdpromod:mdpro/se/{key}',
           'file': target.name, 'bundle': bundle.name, 'source_sha256': source_hash,
           'source_bundle_bytes': bundle.stat().st_size, 'source_channels': clip.m_Channels,
           'source_rate': clip.m_Frequency, 'source_duration': clip.m_Length,
           'source_compression': int(clip.m_CompressionFormat), 'output_channels': 1,
           'codec': 'vorbis', 'compression_level': compression}
    samples = clip.samples
    if len(samples) != 1:
        raise ValueError(f'Expected one sample in {bundle.name}, got {list(samples)}')
    _, wav = next(iter(samples.items()))
    pcm, rate = sf.read(io.BytesIO(wav), dtype='float32', always_2d=True)
    if pcm.shape[1] != clip.m_Channels:
        raise ValueError(f'Channel mismatch in {bundle.name}')
    # Minecraft/OpenAL can position only mono sounds in the world. Averages are
    # bounded by the input peaks, so this mix cannot introduce clipping.
    mono = pcm.mean(axis=1, dtype=np.float32)
    if not np.isfinite(mono).all():
        raise ValueError(f'Nonfinite decoded samples: {bundle.name}')
    if not reuse or not target.exists():
        # Use FFmpeg's libvorbis encoder. The bundled libsndfile Vorbis writer
        # exits the interpreter on longer Windows buffers.
        quality = round((1 - compression) * 10, 2)
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error',
                        '-y', '-f', 'f32le', '-ar', str(rate), '-ac', '1', '-i', 'pipe:0',
                        '-c:a', 'libvorbis', '-q:a', str(quality), '-map_metadata', '-1', str(target)],
                       input=mono.tobytes(), stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
    info = sf.info(target)
    if info.format != 'OGG' or info.subtype != 'VORBIS' or info.channels != 1:
        raise ValueError(f'Invalid output codec: {target}')
    if info.samplerate != rate or abs(info.frames - len(mono)) > 1:
        raise ValueError(f'Output timing mismatch: {target}')
    row.update({'wav_bytes': len(wav), 'decoded_frames': len(mono),
                'duration': info.frames / rate, 'output_rate': rate,
                'ogg_bytes': target.stat().st_size, 'ogg_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
                'peak_before': float(np.abs(mono).max()) if len(mono) else 0})
    return row


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=DEFAULT_SOURCE)
    parser.add_argument('--output', type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument('--compression', type=float, default=0.7,
                        help='libsndfile compression level 0..1 (0.7 ~ Vorbis quality 3)')
    parser.add_argument('--workers', type=int, default=4)
    parser.add_argument('--limit', type=int)
    parser.add_argument('--reuse', action='store_true')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    bundles = sorted(args.source.glob('sound_assets_se_*.bundle'))
    if args.limit:
        bundles = bundles[:args.limit]
    rows, failures = [], []
    started = time.monotonic()
    # FMOD decoder has process-global state; isolate workers, never call it
    # concurrently from Python threads.
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        futures = {pool.submit(build_one, bundle, args.output, args.compression, args.reuse): bundle
                   for bundle in bundles}
        for index, future in enumerate(as_completed(futures), 1):
            bundle = futures[future]
            try:
                rows.append(future.result())
            except Exception as error:
                failures.append({'bundle': bundle.name, 'error': f'{type(error).__name__}: {error}'})
                print(json.dumps(failures[-1], ensure_ascii=False), flush=True)
            if index % 50 == 0 or index == len(bundles):
                print(f'{index}/{len(bundles)} clips, {len(failures)} failures, '
                      f'{time.monotonic() - started:.1f}s', flush=True)
    rows.sort(key=lambda r: r['key'])
    keys = [r['key'] for r in rows]
    if len(set(keys)) != len(keys):
        raise ValueError('Duplicate AudioClip names would overwrite outputs')
    report = {'source': str(args.source), 'output': str(args.output),
              'clips': len(rows), 'failed': len(failures), 'failures': failures,
              'wav_bytes': sum(r['wav_bytes'] for r in rows),
              'source_bundle_bytes': sum(r['source_bundle_bytes'] for r in rows),
              'ogg_bytes': sum(r['ogg_bytes'] for r in rows),
              'duration_seconds': sum(r['duration'] for r in rows),
              'seconds': round(time.monotonic() - started, 2), 'assets': rows}
    report_path = HERE / ('audio-assets-sample.json' if args.limit else 'audio-assets.json')
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=1), encoding='utf-8')
    events = {r['event']: {'sounds': [{'name': r['sound'], 'stream': r['duration'] > 20}]}
              for r in rows}
    (HERE / 'sounds.generated.json').write_text(json.dumps(events, ensure_ascii=False, indent=1), encoding='utf-8')
    print(json.dumps({k: v for k, v in report.items() if k not in ('assets', 'failures')}, ensure_ascii=False), flush=True)
    return bool(failures)


if __name__ == '__main__':
    raise SystemExit(main())
