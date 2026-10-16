"""Build the single-card textures and inventory used by the card model."""
from pathlib import Path
import hashlib
import json
from datetime import datetime
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
MD_CARD_DIR = Path(r'D:\MyCardLibrary\mdpro3\Picture\CardGenerated')
SAMPLE_CODE = '89631139'
DECK_ASSETS = Path(r'D:\MDProMod\assets\deck_models')

textures = ROOT / 'textures'
textures.mkdir(parents=True, exist_ok=True)

sample = MD_CARD_DIR / f'{SAMPLE_CODE}.jpg'
if not sample.exists():
    candidates = sorted(p for p in MD_CARD_DIR.iterdir()
                        if p.suffix.lower() in ('.jpg', '.png'))
    if not candidates:
        raise SystemExit('No card images found')
    sample = candidates[0]
    SAMPLE_CODE = sample.stem

(textures / 'card_face_placeholder.jpg').write_bytes(sample.read_bytes())

back_source = DECK_ASSETS / 'textures' / 'card_back_default_simplified.png'
if not back_source.exists():
    raise SystemExit(f'Missing simplified card back: {back_source}')
(textures / 'card_back.png').write_bytes(back_source.read_bytes())

# A small stand-alone edge strip: bright top edge, dark paper body.
edge = Image.new('RGB', (32, 64), '#171310')
draw = ImageDraw.Draw(edge)
draw.rectangle((0, 0, 31, 2), fill='#d2aa61')
draw.rectangle((2, 3, 29, 5), fill='#5b3b1b')
draw.rectangle((0, 6, 31, 55), fill='#201915')
draw.rectangle((0, 56, 31, 58), fill='#6e4e26')
draw.rectangle((0, 59, 31, 63), fill='#0d0b0a')
edge.save(textures / 'card_edge.png')

files = sorted(p for p in MD_CARD_DIR.iterdir()
               if p.suffix.lower() in ('.jpg', '.png'))
sizes = {}
codes = []
total_bytes = 0
for path in files:
    total_bytes += path.stat().st_size
    codes.append(path.stem)
    try:
        size = Image.open(path).size
    except Exception:
        size = (0, 0)
    key = f'{size[0]}x{size[1]}'
    sizes[key] = sizes.get(key, 0) + 1

missing_log = MD_CARD_DIR / 'MissingAndFailedCards.txt'
missing_lines = []
if missing_log.exists():
    missing_lines = [line.strip() for line in missing_log.read_text(
        encoding='utf-8', errors='replace').splitlines() if line.strip()]

inventory = {
    'scanned_at': datetime.now().astimezone().isoformat(timespec='seconds'),
    'directory': str(MD_CARD_DIR),
    'count': len(files),
    'total_bytes': total_bytes,
    'total_gib': round(total_bytes / (1024 ** 3), 4),
    'formats': {
        '.jpg': sum(1 for p in files if p.suffix.lower() == '.jpg'),
        '.png': sum(1 for p in files if p.suffix.lower() == '.png'),
    },
    'dimension_histogram': sizes,
    'missing_log': missing_lines,
    'sample_code': SAMPLE_CODE,
    'sample_file': sample.name,
    'codes': codes,
}
(ROOT / 'card_image_inventory.json').write_text(
    json.dumps(inventory, ensure_ascii=False, indent=2), encoding='utf-8')

def directory_stats(path, patterns=('*.jpg',)):
    directory = Path(path)
    found = [item for pattern in patterns for item in directory.glob(pattern)]
    return len(found), sum(item.stat().st_size for item in found)

profile_176 = directory_stats(r'D:\MDProMod\runtime\card_textures_176x256_q80')
profile_352 = directory_stats(r'D:\MDProMod\runtime\card_textures_352x512_q85')
master_stats = directory_stats(MD_CARD_DIR, ('*.jpg', '*.png'))

spec = {
    'name': 'single_card_v1',
    'version': 1,
    'model': {
        'file': 'single_card_v1.bbmodel',
        'root_group': 'single_card',
        'body_group': 'card_body',
        'cube': 'Card_Body',
        'thickness': 0.06,
        'previews': [
            'single_card_face_preview.png',
            'single_card_back_preview.png',
            'single_card_perspective_preview.png',
        ],
    },
    'card': {
        'width': 6.875,
        'depth': 10.0,
        'thickness': 0.06,
        'aspect_ratio': '704:1024',
        'origin': 'bottom center',
        'duel_mat_top_y': 0.75,
    },
    'textures': {
        'face_placeholder': 'textures/card_face_placeholder.jpg',
        'back': 'textures/card_back.png',
        'edge': 'textures/card_edge.png',
    },
    'uv': {
        'face': [0, 0, 704, 1024],
        'back': [0, 0, 320, 467],
        'edge': [0, 0, 32, 64],
        'underside': [0, 32, 32, 64],
    },
    'groups': {
        'card_body': 'Single shared card geometry. The top face is the dynamic card face, the bottom face is the shared card back, and the sides use the card-edge texture.',
    },
    'locators': [
        'card_origin',
        'card_face_surface',
        'card_back_surface',
        'card_attack_anchor',
        'card_defense_anchor',
        'card_texture_swap',
    ],
    'state_rendering': {
        'face_up': 'Bind the dynamic card image to the top face and render the top face.',
        'face_down': 'Use the shared card back on the top face, or rotate the model 180 degrees around X to expose the bottom face.',
        'attack': 'Card long axis points along the player forward direction.',
        'defense': 'Rotate the card 90 degrees around Y while keeping the same texture mapping.',
        'graveyard_or_banished': 'Use the same card model and bind a face texture only for visible face-up cards.',
    },
    'dynamic_texture': {
        'source_directory': str(MD_CARD_DIR),
        'file_pattern': '<card_code>.jpg',
        'full_size': [704, 1024],
        'default_profile': 'field_176x256_q80',
        'detail_profile': 'master_on_demand',
        'legacy_detail_profile': 'field_352x512_q85',
        'recommended_field_size': [176, 256],
        'detail_size': [704, 1024],
        'gpu_mib_full_rgba': round(704 * 1024 * 4 / (1024 ** 2), 3),
        'gpu_mib_field_rgba': round(176 * 256 * 4 / (1024 ** 2), 3),
        'recommended_cache_entries': 128,
        'eviction': 'LRU, unload when a card leaves the visible field',
        'fallback': 'card back texture for missing or failed images',
        'profiles': {
            'field_176x256_q80': {
                'directory': 'D:/MDProMod/runtime/card_textures_176x256_q80',
                'size': [176, 256], 'quality': 80, 'format': 'jpeg',
                'files': profile_176[0],
                'mib': round(profile_176[1] / (1024 ** 2), 2),
                'complete': profile_176[0] == len(files),
            },
            'field_352x512_q85': {
                'directory': 'D:/MDProMod/runtime/card_textures_352x512_q85',
                'size': [352, 512], 'quality': 85, 'format': 'jpeg',
                'files': profile_352[0],
                'mib': round(profile_352[1] / (1024 ** 2), 2),
                'complete': profile_352[0] == len(files),
                'note': 'Partial legacy detail cache; regenerate for the full set if needed.',
            },
        },
        'master': {
            'directory': str(MD_CARD_DIR),
            'size': [704, 1024], 'format': 'jpg',
            'files': master_stats[0],
            'mib': round(master_stats[1] / (1024 ** 2), 2),
            'complete': master_stats[0] == len(files),
        },
        'sample_code': SAMPLE_CODE,
        'sample_sha256': hashlib.sha256(sample.read_bytes()).hexdigest(),
    },
    'inventory_snapshot': {
        'count': len(files),
        'total_gib': round(total_bytes / (1024 ** 3), 4),
        'dimension_histogram': sizes,
        'missing_log': missing_lines,
    },
}
(ROOT / 'single_card_spec.json').write_text(
    json.dumps(spec, ensure_ascii=False, indent=2), encoding='utf-8')

print(json.dumps({
    'sample': sample.name,
    'inventory_count': len(files),
    'total_gib': round(total_bytes / (1024 ** 3), 4),
    'sizes': sizes,
    'missing': missing_lines,
    'full_texture_mib': spec['dynamic_texture']['gpu_mib_full_rgba'],
    'field_texture_mib': spec['dynamic_texture']['gpu_mib_field_rgba'],
}, ensure_ascii=False))
