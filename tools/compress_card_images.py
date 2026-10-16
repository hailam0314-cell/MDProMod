"""Build a smaller runtime card-texture cache without changing the originals."""
from pathlib import Path
import argparse
import json
import os
import time
from concurrent.futures import ProcessPoolExecutor, as_completed
from PIL import Image

DEFAULT_SOURCE = Path(r'D:\MyCardLibrary\mdpro3\Picture\CardGenerated')
DEFAULT_ROOT = Path(r'D:\MDProMod\runtime')


def parse_size(value):
    width, height = value.lower().split('x', 1)
    return int(width), int(height)


def compress_one(task):
    source, output, width, height, quality, fmt = task
    target = output / f'{source.stem}.jpg'
    try:
        if target.exists() and target.stat().st_size > 0:
            return 'skip', source.name, target.stat().st_size, ''
        with Image.open(source) as image:
            resized = image.convert('RGB').resize(
                (width, height), Image.Resampling.LANCZOS)
            if fmt == 'jpeg':
                resized.save(target, format='JPEG', quality=quality,
                             optimize=True, progressive=True, subsampling=2)
            elif fmt == 'webp':
                resized.save(target, format='WEBP', quality=quality, method=6)
            elif fmt == 'png':
                resized.save(target, format='PNG', optimize=True)
            else:
                raise ValueError(f'Unsupported format: {fmt}')
        return 'ok', source.name, target.stat().st_size, ''
    except Exception as error:
        return 'error', source.name, 0, str(error)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=DEFAULT_SOURCE)
    parser.add_argument('--output', type=Path, default=None)
    parser.add_argument('--size', default='352x512')
    parser.add_argument('--quality', type=int, default=85)
    parser.add_argument('--format', choices=('jpeg', 'webp', 'png'),
                        default='jpeg')
    parser.add_argument('--workers', type=int,
                        default=min(8, os.cpu_count() or 4))
    parser.add_argument('--limit', type=int, default=0)
    args = parser.parse_args()

    width, height = parse_size(args.size)
    output = args.output or (DEFAULT_ROOT /
                             f'card_textures_{width}x{height}_q{args.quality}')
    output.mkdir(parents=True, exist_ok=True)
    failures_path = output / 'failures.txt'
    if failures_path.exists():
        failures_path.unlink()

    files = sorted(path for path in args.source.iterdir()
                   if path.suffix.lower() in ('.jpg', '.png', '.jpeg'))
    if args.limit:
        files = files[:args.limit]

    started = time.perf_counter()
    results = []
    tasks = [(path, output, width, height, args.quality, args.format)
             for path in files]
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        futures = [pool.submit(compress_one, task) for task in tasks]
        for index, future in enumerate(as_completed(futures), 1):
            results.append(future.result())
            if index % 500 == 0 or index == len(futures):
                print(json.dumps({'processed': index, 'total': len(futures)}),
                      flush=True)

    ok = [result for result in results if result[0] == 'ok']
    skipped = [result for result in results if result[0] == 'skip']
    errors = [result for result in results if result[0] == 'error']
    output_bytes = sum(path.stat().st_size for path in output.glob('*.jpg'))
    source_bytes = sum(path.stat().st_size for path in files)
    elapsed = time.perf_counter() - started
    manifest = {
        'source': str(args.source),
        'output': str(output),
        'size': [width, height],
        'quality': args.quality,
        'format': args.format,
        'source_files': len(files),
        'output_files': len(list(output.glob('*.jpg'))),
        'compressed_now': len(ok),
        'skipped_existing': len(skipped),
        'errors': len(errors),
        'source_bytes_for_scanned_files': source_bytes,
        'output_bytes': output_bytes,
        'output_mib': round(output_bytes / (1024 ** 2), 2),
        'elapsed_seconds': round(elapsed, 2),
    }
    (output / 'manifest.json').write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    if errors:
        (output / 'failures.txt').write_text(
            '\n'.join(f'{name}\t{error}' for _, name, _, error in errors),
            encoding='utf-8')
    print(json.dumps(manifest, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
