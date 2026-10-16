"""Fully decode packaged effects and verify timing, codec, and registry coverage."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import sys
import time
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE / 'deps'))
import numpy as np
import soundfile as sf


def verify(row):
    path = OUTPUT / row['file']
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != row['ogg_sha256']:
        raise ValueError(f'Hash mismatch: {path}')
    if not raw.startswith(b'OggS'):
        raise ValueError(f'Not OGG: {path}')
    pcm, rate = sf.read(path, dtype='float32', always_2d=True)
    if pcm.shape != (row['decoded_frames'], 1) or rate != row['output_rate']:
        raise ValueError(f'PCM shape changed: {path}')
    if not np.isfinite(pcm).all():
        raise ValueError(f'Nonfinite PCM: {path}')
    return {'key':row['key'], 'decoded_peak':float(np.abs(pcm).max()) if len(pcm) else 0}


if __name__ == '__main__':
    report=json.loads((HERE / 'audio-assets.json').read_text(encoding='utf-8'))
    OUTPUT=Path(report['output'])
    started=time.monotonic()
    with ThreadPoolExecutor(max_workers=4) as pool:
        rows=list(pool.map(verify, report['assets']))
    expected={r['file'] for r in report['assets']}
    actual={p.name for p in OUTPUT.glob('*.ogg')}
    if expected != actual:
        raise ValueError(f'Extra/missing OGG: {actual ^ expected}')
    generated=json.loads((HERE/'sounds.generated.json').read_text(encoding='utf-8'))
    if {r['event'] for r in report['assets']} != set(generated):
        raise ValueError('sounds.generated.json coverage mismatch')
    summary={'clips_verified':len(rows),'codec':'Ogg Vorbis','channels':1,
             'ogg_bytes':report['ogg_bytes'],'wav_bytes':report['wav_bytes'],
             'seconds':round(time.monotonic()-started,2),
             'decoded_overshoot_count':sum(r['decoded_peak']>1 for r in rows),
             'max_decoded_peak':max(r['decoded_peak'] for r in rows)}
    (HERE/'audio-assets-verification.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(json.dumps(summary))
