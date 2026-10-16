"""Extract Texture2D and Sprite images from a Unity AssetBundle."""
from pathlib import Path
import argparse
import json
import sys

UNITYPY_DIR = Path(r'D:\MDProMod\tools\unitypy')
sys.path.insert(0, str(UNITYPY_DIR))
import UnityPy  # noqa: E402


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('bundle', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--all', action='store_true',
                        help='Save every Texture2D and Sprite, not just named objects.')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)

    env = UnityPy.load(str(args.bundle))
    saved = []
    objects = []
    for obj in env.objects:
        type_name = obj.type.name
        try:
            data = obj.read()
        except Exception as error:
            objects.append({'type': type_name, 'path_id': obj.path_id,
                            'error': str(error)})
            continue
        name = getattr(data, 'm_Name', '') or getattr(data, 'name', '')
        entry = {'type': type_name, 'path_id': obj.path_id, 'name': name}
        if type_name in ('Texture2D', 'Sprite'):
            try:
                image = data.image
                if image is not None:
                    safe_name = ''.join(
                        character if character.isalnum() or character in '-_.' else '_'
                        for character in (name or f'object_{obj.path_id}'))
                    path = args.output / f'{safe_name}.png'
                    if path.exists():
                        path = args.output / f'{safe_name}_{obj.path_id}.png'
                    image.save(path)
                    entry['file'] = str(path)
                    entry['size'] = list(image.size)
                    saved.append(entry)
            except Exception as error:
                entry['image_error'] = str(error)
        objects.append(entry)

    summary = {
        'bundle': str(args.bundle),
        'objects': objects,
        'saved': saved,
    }
    (args.output / (args.bundle.stem + '.json')).write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'bundle': str(args.bundle), 'saved': saved}, ensure_ascii=False))


if __name__ == '__main__':
    main()
