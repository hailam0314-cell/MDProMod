"""Build the shared deck texture atlas from MDPro's default card back."""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / 'source' / 'DefaultProtector.png'
CARD_W, CARD_H = 320, 467

if not SOURCE.exists():
    raise SystemExit(f'Missing source texture: {SOURCE}')

card = Image.open(SOURCE).convert('RGB').resize((CARD_W, CARD_H), Image.Resampling.LANCZOS)
card = card.filter(ImageFilter.UnsharpMask(radius=1.1, percent=55, threshold=3))
atlas = Image.new('RGB', (512, 512), '#0b0b0d')
atlas.paste(card, (0, 0))
draw = ImageDraw.Draw(atlas)

def layer_strip(x0, x1, count, warm=True):
    """Compress many card edges into a repeating side texture."""
    draw.rectangle((x0, 0, x1 - 1, 511), fill='#120f0c' if warm else '#0f1114')
    for index in range(count):
        y = round(index * 512 / count)
        next_y = max(y + 2, round((index + 1) * 512 / count))
        wave = (math.sin(index * 1.73) + 1) / 2
        if warm:
            card_edge = (38 + round(42 * wave), 25 + round(24 * wave), 14 + round(10 * wave))
            glint = (156 + round(70 * wave), 122 + round(54 * wave), 62 + round(32 * wave))
        else:
            card_edge = (22, 25 + round(12 * wave), 30 + round(16 * wave))
            glint = (119 + round(40 * wave), 122 + round(36 * wave), 112 + round(28 * wave))
        draw.rectangle((x0 + 1, y, x1 - 2, min(next_y - 1, 511)), fill=card_edge)
        if index % 2 == 0:
            draw.line((x0 + 2, min(y + 1, 510), x1 - 3, min(y + 1, 510)), fill=glint)

layer_strip(320, 352, 52, warm=True)
layer_strip(352, 384, 18, warm=False)

# Individual card edge for the top card of each stack.
for y in range(512):
    blend = y / 511
    color = (
        round(185 - 90 * blend),
        round(148 - 72 * blend),
        round(82 - 38 * blend),
    )
    draw.line((384, y, 415, y), fill=color)
draw.line((384, 0, 384, 511), fill='#f0d9a0')
draw.line((415, 0, 415, 511), fill='#24170d')

# Footer colors keep every UV zone inside one atlas.
draw.rectangle((416, 0, 447, 511), fill='#08080a')
draw.rectangle((448, 0, 479, 511), fill='#15171a')
draw.rectangle((480, 0, 511, 511), fill='#24272b')

textures = ROOT / 'textures'
textures.mkdir(parents=True, exist_ok=True)
atlas.save(textures / 'deck_atlas.png')
card.save(textures / 'card_back_default_simplified.png')

spec = {
    'source': {
        'file': 'source/DefaultProtector.png',
        'sha256': __import__('hashlib').sha256(SOURCE.read_bytes()).hexdigest(),
        'original_size': list(Image.open(SOURCE).size),
        'mdpro_protector_id': '1070001',
    },
    'atlas': {
        'file': 'textures/deck_atlas.png',
        'size': [512, 512],
        'card_back_uv': [0, 0, 320, 467],
        'main_deck_side_uv': [320, 0, 352, 512],
        'extra_deck_side_uv': [352, 0, 384, 512],
        'card_edge_uv': [384, 0, 416, 512],
        'shadow_uv': [416, 0, 448, 512],
        'dark_base_uv': [448, 0, 480, 512],
    },
    'card': {
        'width': 6.875,
        'depth': 10.0,
        'source_pixels': [480, 700],
    },
    'main_deck': {
        'stack_height': 2.42,
        'core_height': 2.22,
        'top_card_height': 0.12,
        'shadow_height': 0.08,
        'target_cards': '40-60 sleeved cards',
    },
    'extra_deck': {
        'stack_height': 1.02,
        'core_height': 0.82,
        'top_card_height': 0.12,
        'shadow_height': 0.08,
        'target_cards': '10-15 sleeved cards',
    },
    'models': {
        'main_deck': {
            'file': 'main_deck_v1.bbmodel',
            'group': 'main_deck',
            'total_height': 2.42,
            'card_footprint': [6.875, 10.0],
            'shadow_footprint': [7.1, 10.3],
            'locators': [
                'main_deck_origin',
                'main_deck_stack_surface',
                'main_deck_top_surface',
                'main_deck_card_anchor',
                'main_deck_vertical_scale_origin',
            ],
        },
        'extra_deck': {
            'file': 'extra_deck_v1.bbmodel',
            'group': 'extra_deck',
            'total_height': 1.02,
            'card_footprint': [6.875, 10.0],
            'shadow_footprint': [7.1, 10.3],
            'locators': [
                'extra_deck_origin',
                'extra_deck_stack_surface',
                'extra_deck_top_surface',
                'extra_deck_card_anchor',
                'extra_deck_vertical_scale_origin',
            ],
        },
    },
    'placement': {
        'origin': 'bottom center',
        'duel_mat_top_y': 0.75,
        'note': 'Add 0.75 to Y when placing either model on the duel mat.',
        'dynamic_count': 'Scale the core cube vertically and move the top-card group with it.',
    },
}
(ROOT / 'deck_models_spec.json').write_text(
    json.dumps(spec, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({
    'atlas': str(textures / 'deck_atlas.png'),
    'card_back': str(textures / 'card_back_default_simplified.png'),
    'main_height': spec['main_deck']['stack_height'],
    'extra_height': spec['extra_deck']['stack_height'],
}, ensure_ascii=False))
