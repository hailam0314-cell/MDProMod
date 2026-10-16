"""Build the 10-card-per-segment deck atlas and dimensions."""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
TEXTURES = ROOT / 'textures'
TEXTURES.mkdir(parents=True, exist_ok=True)

CARD_BACK = TEXTURES / 'card_back_default_simplified.png'
if not CARD_BACK.exists():
    raise SystemExit(f'Missing card back: {CARD_BACK}')

atlas = Image.new('RGB', (512, 512), '#0b0b0d')
card_back = Image.open(CARD_BACK).convert('RGB')
atlas.paste(card_back, (0, 0))
draw = ImageDraw.Draw(atlas)

def segment_side(x0, x1, layers, warm=True):
    draw.rectangle((x0, 0, x1 - 1, 511), fill='#120f0c' if warm else '#0f1114')
    for index in range(layers):
        y0 = round(index * 512 / layers)
        y1 = max(y0 + 3, round((index + 1) * 512 / layers))
        wave = (math.sin(index * 1.31) + 1) / 2
        if warm:
            body = (34 + round(35 * wave), 23 + round(20 * wave), 13 + round(8 * wave))
            edge = (166 + round(58 * wave), 128 + round(42 * wave), 66 + round(26 * wave))
        else:
            body = (22, 25 + round(10 * wave), 30 + round(14 * wave))
            edge = (125 + round(28 * wave), 126 + round(24 * wave), 116 + round(20 * wave))
        draw.rectangle((x0 + 1, y0, x1 - 2, y1 - 1), fill=body)
        draw.line((x0 + 2, min(y0 + 2, 510), x1 - 3, min(y0 + 2, 510)),
                  fill=edge)

# One 10-card block uses the full side texture height.
segment_side(320, 352, 10, warm=True)

# A dark seam that separates two 10-card blocks.
draw.rectangle((352, 0, 383, 511), fill='#090809')
draw.rectangle((352, 250, 383, 261), fill='#8b6b32')
draw.rectangle((352, 262, 383, 273), fill='#18110b')

# Top-card edge, shadow, dark base and mat helper colors.
for y in range(512):
    blend = y / 511
    draw.line((384, y, 415, y),
              fill=(round(185 - 88 * blend),
                    round(148 - 70 * blend),
                    round(82 - 36 * blend)))
draw.line((384, 0, 384, 511), fill='#f0d9a0')
draw.line((415, 0, 415, 511), fill='#24170d')
draw.rectangle((416, 0, 447, 511), fill='#08080a')
draw.rectangle((448, 0, 479, 511), fill='#15171a')
draw.rectangle((480, 0, 511, 511), fill='#24272b')

atlas.save(TEXTURES / 'deck_segment_atlas.png')

spec = {
    'name': 'segmented_deck_models',
    'version': 1,
    'units_per_block': 16,
    'card_footprint': [6.875, 10.0],
    'block_pitch': 0.40,
    'segment_height': 0.388,
    'seam_height': 0.012,
    'top_card_height': 0.06,
    'shadow_height': 0.08,
    'main_deck': {
        'max_segments': 6,
        'counts': [40, 50, 60],
        'partial_cards': '1-9, rendered by scaling the hidden partial segment',
    },
    'extra_deck': {
        'max_segments': 1,
        'max_partial_cards': 5,
        'counts': [10, 15],
        'partial_cards': '1-9, rendered by scaling the hidden partial segment',
    },
    'texture': {
        'file': 'textures/deck_segment_atlas.png',
        'size': [512, 512],
        'card_back_uv': [0, 0, 320, 467],
        'segment_side_uv': [320, 0, 352, 512],
        'seam_uv': [352, 0, 384, 512],
        'card_edge_uv': [384, 0, 416, 512],
        'shadow_uv': [416, 0, 448, 512],
        'dark_base_uv': [448, 0, 480, 512],
        'mat_uv': [480, 0, 512, 512],
    },
    'models': {
        'main_deck_dynamic': {
            'file': 'main_deck_dynamic_v1.bbmodel',
            'root_group': 'main_deck_dynamic',
            'max_height': round(0.08 + 6 * 0.40 + 0.06, 3),
            'block_anchors_y': [round(0.08 + i * 0.40, 3) for i in range(7)],
        },
        'extra_deck_dynamic': {
            'file': 'extra_deck_dynamic_v1.bbmodel',
            'root_group': 'extra_deck_dynamic',
            'max_height': round(0.08 + 1 * 0.40 + 0.20 + 0.06, 3),
            'block_anchors_y': [round(0.08 + i * 0.40, 3) for i in range(2)],
        },
    },
}
(ROOT / 'segmented_deck_spec.json').write_text(
    json.dumps(spec, ensure_ascii=False, indent=2), encoding='utf-8')

print(json.dumps({
    'atlas': str(TEXTURES / 'deck_segment_atlas.png'),
    'block_pitch': spec['block_pitch'],
    'segment_height': spec['segment_height'],
    'main_max_height': spec['models']['main_deck_dynamic']['max_height'],
    'extra_max_height': spec['models']['extra_deck_dynamic']['max_height'],
}, ensure_ascii=False))
