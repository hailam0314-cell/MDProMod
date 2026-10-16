"""Build the default red DUELIST CARD CASE texture atlas."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
TEXTURES = ROOT / 'textures'
REFERENCE = ROOT / 'reference'
TEXTURES.mkdir(parents=True, exist_ok=True)
REFERENCE.mkdir(parents=True, exist_ok=True)

SOURCE_CLOSED = Path(r'D:\MyCardLibrary\mdpro3\MDPro3_Data\StandaloneWindows64\MDPro3\icon_assets_deckcase0001_l_hd.bundle')
EXTRACTED = Path(r'D:\MDProMod\assets\card_case_models\source')

ATLAS = Image.new('RGB', (512, 512), '#a80d25')
D = ImageDraw.Draw(ATLAS)
RED = '#c8102e'
RED_DARK = '#8e0b20'
WHITE = '#f2f2f2'
SILVER = '#d8d8d8'
INTERIOR = '#3a0a12'
GOLD = '#c89a54'

REGIONS = {
    'body_front': (0, 0, 128, 192),
    'body_back': (256, 0, 384, 192),
    'lid_front': (0, 384, 192, 462),
    'lid_back': (192, 384, 384, 462),
    'body_side': (0, 192, 64, 384),
    'lid_side': (64, 192, 128, 320),
    'interior': (128, 192, 256, 384),
    'lid_interior': (256, 192, 384, 320),
    'stack_side': (384, 128, 448, 384),
    'lid_top': (384, 384, 512, 448),
    'red': (384, 448, 448, 512),
    'dark': (448, 448, 512, 512),
}

def region(name):
    return REGIONS[name]

def draw_border(box, first=8, second=16):
    x0, y0, x1, y1 = box
    D.rectangle((x0, y0, x1 - 1, y1 - 1), fill=RED)
    D.rectangle((x0 + first, y0 + first, x1 - 1 - first, y1 - 1 - first),
                outline=WHITE, width=4)
    D.rectangle((x0 + second, y0 + second, x1 - 1 - second, y1 - 1 - second),
                outline=SILVER, width=2)

def draw_side(box):
    x0, y0, x1, y1 = box
    D.rectangle((x0, y0, x1 - 1, y1 - 1), fill=RED)
    D.line((x0 + 6, y0 + 4, x0 + 6, y1 - 5), fill=WHITE, width=3)
    D.line((x0 + 11, y0 + 4, x0 + 11, y1 - 5), fill=SILVER, width=2)
    D.line((x0 + 2, y0 + 4, x0 + 2, y1 - 5), fill=RED_DARK, width=2)
    D.line((x1 - 7, y0 + 4, x1 - 7, y1 - 5), fill=WHITE, width=3)
    D.line((x1 - 12, y0 + 4, x1 - 12, y1 - 5), fill=SILVER, width=2)
    D.line((x1 - 3, y0 + 4, x1 - 3, y1 - 5), fill=RED_DARK, width=2)

def draw_logo(box):
    x0, y0, x1, y1 = box
    draw_border(box, first=6, second=12)
    center = (x0 + x1) // 2

    def fit_font(text, max_width, start_size):
        for size in range(start_size, 7, -1):
            candidate = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', size)
            if D.textlength(text, font=candidate) <= max_width:
                return candidate
        return ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 8)

    title_font = fit_font('DUELIST', (x1 - x0) - 28, 20)
    line_font = fit_font('CARD CASE', (x1 - x0) - 28, 18)
    D.text((center, y0 + 18), 'DUELIST', font=title_font,
           fill=WHITE, anchor='mm', stroke_width=2, stroke_fill=RED_DARK)
    D.text((center, y0 + 40), 'CARD CASE', font=line_font,
           fill=WHITE, anchor='mm', stroke_width=2, stroke_fill=RED_DARK)
    cx, cy = center, y0 + 58
    D.polygon([(cx - 20, cy - 4), (cx, cy + 9), (cx + 20, cy - 4),
               (cx + 12, cy - 4), (cx, cy + 2), (cx - 12, cy - 4)],
              fill=SILVER)

def draw_interior(box, lid=False):
    x0, y0, x1, y1 = box
    for y in range(y0, y1):
        blend = (y - y0) / max(1, y1 - y0 - 1)
        color = (int(58 - 28 * blend), int(12 - 6 * blend), int(18 - 8 * blend))
        D.line((x0, y, x1 - 1, y), fill=color)
    if lid:
        D.text(((x0 + x1) // 2, y0 + 42), 'DUELIST', font=ImageFont.truetype(
            'C:/Windows/Fonts/arialbd.ttf', 20), fill=(96, 20, 34), anchor='mm')
        D.text(((x0 + x1) // 2, y0 + 68), 'CARD CASE', font=ImageFont.truetype(
            'C:/Windows/Fonts/arialbd.ttf', 18), fill=(96, 20, 34), anchor='mm')

def draw_stack_side(box):
    x0, y0, x1, y1 = box
    D.rectangle((x0, y0, x1 - 1, y1 - 1), fill=INTERIOR)
    for index in range(60):
        x = x0 + 2 + round(index * (x1 - x0 - 4) / 60)
        color = (35 + (index % 5) * 8, 18 + (index % 3) * 5, 10 + (index % 4) * 4)
        D.line((x, y0 + 2, x, y1 - 3), fill=color, width=1)
        if index % 5 == 0:
            D.line((x + 1, y0 + 2, x + 1, y1 - 3), fill=GOLD, width=1)

draw_border(region('body_front'))
draw_logo(region('lid_front'))
draw_border(region('body_back'))
draw_border(region('lid_back'))
draw_side(region('body_side'))
draw_side(region('lid_side'))
draw_interior(region('interior'))
draw_interior(region('lid_interior'), lid=True)
draw_stack_side(region('stack_side'))
draw_border(region('lid_top'), first=6, second=12)
x0, y0, x1, y1 = region('red')
D.rectangle((x0, y0, x1 - 1, y1 - 1), fill=RED)
x0, y0, x1, y1 = region('dark')
D.rectangle((x0, y0, x1 - 1, y1 - 1), fill=RED_DARK)

ATLAS.save(TEXTURES / 'card_case_atlas.png')

back_source = Path(r'D:\MDProMod\assets\deck_models\textures\card_back_default_simplified.png')
if back_source.exists():
    (TEXTURES / 'card_back.png').write_bytes(back_source.read_bytes())

REFERENCE.mkdir(parents=True, exist_ok=True)
for name in ('DeckCase0001_L.png', 'DeckCase0001_L_reverse.png',
             'DeckCase0001_Open_L.png'):
    matches = list(EXTRACTED.rglob(name))
    if matches:
        (REFERENCE / name).write_bytes(matches[0].read_bytes())

spec = {
    'name': 'card_case_v1',
    'source': {
        'default_item_id': '1080001',
        'bundle': str(SOURCE_CLOSED),
        'extracted_reference': 'reference/',
    },
    'dimensions': {
        'body_width': 8.4,
        'body_height': 8.0,
        'body_depth': 5.6,
        'lid_width': 8.6,
        'lid_height': 3.5,
        'lid_depth': 5.8,
        'top_cap_height': 0.3,
        'wall_thickness': 0.35,
        'stack_width': 6.875,
        'stack_height': 10.0,
        'stack_depth': 2.4,
        'origin': 'bottom center',
        'duel_mat_top_y': 0.75,
    },
    'hinge': {
        'position': [0, 11.5, -2.9],
        'closed_rotation': [0, 0, 0],
        'open_rotation': [110, 0, 0],
    },
    'uv': REGIONS,
    'textures': {
        'atlas': 'textures/card_case_atlas.png',
        'card_back': 'textures/card_back.png',
    },
    'colors': {
        'red': RED,
        'red_dark': RED_DARK,
        'white': WHITE,
        'silver': SILVER,
        'interior': INTERIOR,
        'gold': GOLD,
    },
}
(ROOT / 'card_case_spec.json').write_text(
    json.dumps(spec, ensure_ascii=False, indent=2), encoding='utf-8')

print(json.dumps({
    'atlas': str(TEXTURES / 'card_case_atlas.png'),
    'body': [spec['dimensions']['body_width'], spec['dimensions']['body_height'],
             spec['dimensions']['body_depth']],
    'lid': [spec['dimensions']['lid_width'], spec['dimensions']['lid_height'],
            spec['dimensions']['lid_depth']],
    'hinge': spec['hinge'],
}, ensure_ascii=False))
