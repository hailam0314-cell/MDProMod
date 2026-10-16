"""Measure card-image compression profiles on a deterministic sample."""
from pathlib import Path
import io
import json
import random
import time
from PIL import Image, ImageDraw, ImageFont

CARD_DIR = Path(r'D:\MyCardLibrary\mdpro3\Picture\CardGenerated')
OUT_DIR = Path(r'D:\MDProMod\docs')
OUT_DIR.mkdir(parents=True, exist_ok=True)
SAMPLE_SIZE = 200

files = sorted(p for p in CARD_DIR.iterdir()
               if p.suffix.lower() in ('.jpg', '.png'))
sample = random.Random(42).sample(files, min(SAMPLE_SIZE, len(files)))

profiles = {
    'original_704x1024': ((704, 1024), 'JPEG', {}),
    'field_528x768_q85': ((528, 768), 'JPEG', {
        'quality': 85, 'optimize': True, 'progressive': True, 'subsampling': 2}),
    'field_352x512_q90': ((352, 512), 'JPEG', {
        'quality': 90, 'optimize': True, 'progressive': True, 'subsampling': 2}),
    'field_352x512_q85': ((352, 512), 'JPEG', {
        'quality': 85, 'optimize': True, 'progressive': True, 'subsampling': 2}),
    'low_264x384_q82': ((264, 384), 'JPEG', {
        'quality': 82, 'optimize': True, 'progressive': True, 'subsampling': 2}),
    'icon_176x256_q80': ((176, 256), 'JPEG', {
        'quality': 80, 'optimize': True, 'progressive': True, 'subsampling': 2}),
    'field_352x512_png': ((352, 512), 'PNG', {'optimize': True}),
    'field_352x512_webp_q82': ((352, 512), 'WEBP', {'quality': 82, 'method': 6}),
}

report = {
    'sample_count': len(sample),
    'source_average_bytes': round(sum(p.stat().st_size for p in sample) / len(sample)),
    'profiles': {},
}

for name, (size, fmt, options) in profiles.items():
    started = time.perf_counter()
    total_bytes = 0
    failures = 0
    for path in sample:
        try:
            image = Image.open(path).convert('RGB')
            if image.size != size:
                image = image.resize(size, Image.Resampling.LANCZOS)
            buffer = io.BytesIO()
            image.save(buffer, format=fmt, **options)
            total_bytes += buffer.tell()
        except Exception:
            failures += 1
    elapsed = time.perf_counter() - started
    average = total_bytes / max(1, len(sample) - failures)
    report['profiles'][name] = {
        'size': list(size),
        'format': fmt,
        'average_bytes': round(average),
        'average_kib': round(average / 1024, 1),
        'estimated_6745_mib': round(average * 6745 / (1024 ** 2), 1),
        'estimated_15000_mib': round(average * 15000 / (1024 ** 2), 1),
        'estimated_6745_gib': round(average * 6745 / (1024 ** 3), 3),
        'estimated_15000_gib': round(average * 15000 / (1024 ** 3), 3),
        'vram_mib_rgba': round(size[0] * size[1] * 4 / (1024 ** 2), 3),
        'sample_encode_seconds': round(elapsed, 2),
        'failures': failures,
    }

(OUT_DIR / 'card-compression-benchmark.json').write_text(
    json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')

# Compare the text-heavy lower part of the same card at each size.
sample_path = CARD_DIR / '89631139.jpg'
original = Image.open(sample_path).convert('RGB')
crop = original.crop((0, 620, 704, 1010))
variants = [
    ('Original 704x1024', original, (0, 620, 704, 1010), 1.0),
    ('352x512 Q85', original.resize((352, 512), Image.Resampling.LANCZOS),
     (0, 310, 352, 505), 2.0),
    ('264x384 Q82', original.resize((264, 384), Image.Resampling.LANCZOS),
     (0, 232, 264, 379), 2.7),
    ('176x256 Q80', original.resize((176, 256), Image.Resampling.LANCZOS),
     (0, 155, 176, 252), 4.0),
]
panels = []
for label, image, box, scale in variants:
    panel = image.crop(box).resize(
        (round((box[2] - box[0]) * scale), round((box[3] - box[1]) * scale)),
        Image.Resampling.NEAREST)
    panels.append((label, panel))
width = max(p.width for _, p in panels)
height = sum(p.height + 46 for _, p in panels)
comparison = Image.new('RGB', (width, height), '#f4f4f2')
draw = ImageDraw.Draw(comparison)
font = ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc', 26)
y = 0
for label, panel in panels:
    draw.text((10, y + 8), label, font=font, fill='#202020')
    comparison.paste(panel, (0, y + 46))
    y += panel.height + 46
comparison.save(OUT_DIR / 'card-compression-text-comparison.png')

print(json.dumps(report, ensure_ascii=False))
