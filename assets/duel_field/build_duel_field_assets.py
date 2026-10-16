"""Create deterministic textures and placement metadata for the Blockbench mat."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
SCALE = 8
FONT = 'C:/Windows/Fonts/msyh.ttc'
FONT_BOLD = 'C:/Windows/Fonts/msyhbd.ttc'
ATLAS = Image.new('RGB', (1024, 1024), '#151922')
D = ImageDraw.Draw(ATLAS)

def point(x, z):
    return (round((x + 64) * SCALE), round((z + 56) * SCALE))

def rect(x1, z1, x2, z2, fill=None, outline=None, width=1):
    D.rectangle((*point(x1, z1), *point(x2, z2)), fill=fill, outline=outline, width=width)

def text(x, z, label, size=15, color='#c6c8c9', bold=False):
    font = ImageFont.truetype(FONT_BOLD if bold else FONT, size)
    D.text(point(x, z), label, font=font, fill=color, anchor='mm')

palette = {
    'monster': ('#27282c', '#b2a276'),
    'spell_trap': ('#212d2d', '#77a799'),
    'deck': ('#302b25', '#b9a786'),
    'extra': ('#292735', '#a392b5'),
    'graveyard': ('#282a2b', '#9aa5a8'),
    'banished': ('#30282b', '#b09192'),
    'field': ('#263025', '#97ad81'),
    'extra_monster': ('#23303a', '#8aacbc'),
}
zones = []
controls = []

# Draw the near half once, then rotate its complete printed artwork for P2.
rect(-64, 0, 64, 56, fill='#23272b')
rect(-62.5, .5, 62.5, 54.5, outline='#817a65', width=2)
rect(-61.5, 1, 61.5, 53.5, outline='#363c40', width=1)
for z in range(2, 54):
    for x in range(-60, 61, 3):
        px, py = point(x, z)
        D.point((px, py), fill='#2a2f33')
for x in (-44, 44):
    D.line((*point(x, 10), *point(x, 42)), fill='#414548', width=2)

def draw_zone(identifier, kind, label, x, z, sequence=None, pendulum=None):
    inside, edge = palette[kind]
    rect(x-6, z-7, x+6, z+7, fill=inside, outline=edge, width=2)
    rect(x-5.5, z-6.5, x+5.5, z+6.5, outline='#4e5150', width=1)
    # A modest line suggests the portrait card footprint; horizontal cards fit too.
    rect(x-3.4375, z-4.7, x+3.4375, z+5.3, outline='#474b4b', width=1)
    text(x, z-5.65, label, size=13, color=edge)
    if pendulum:
        text(x, z+5.85, '灵摆区', size=12, color='#b69c78')
        rect(x-5.1, z+4.4, x-4.65, z+5.7, fill='#769dad')
        rect(x+4.65, z+4.4, x+5.1, z+5.7, fill='#b17b77')
    zones.append({
        'id': identifier, 'owner': 'p1', 'kind': kind,
        'label': label, 'sequence': sequence,
        'center': [x, .82, z], 'card_anchor': [x, 1.05, z],
        'clear_rectangle': [12, 14], 'rotation_y': 0,
        'pendulum': pendulum,
        'atlas_uv': [x+58, z+49, x+70, z+63],
    })

for i, x in enumerate((-32, -16, 0, 16, 32)):
    draw_zone(f'p1_mzone_{i}', 'monster', f'怪兽 {i+1}', x, 18, i)
    draw_zone(f'p1_szone_{i}', 'spell_trap', f'魔陷 {i+1}', x, 34, i,
              ('left' if i == 0 else 'right' if i == 4 else None))
draw_zone('p1_deck', 'deck', '主卡组', 52, 34)
draw_zone('p1_extra', 'extra', '额外卡组', -52, 34)
draw_zone('p1_graveyard', 'graveyard', '墓地', 52, 18)
draw_zone('p1_banished', 'banished', '除外', 52, 2)
draw_zone('p1_field', 'field', '场地魔法', -52, 18, 5)

# The front strip is deliberately separate from all card placement rectangles.
rect(-59, 43.5, 59, 53.2, fill='#1a2024', outline='#777764', width=2)
text(0, 44.8, '阶段 / 回合', size=13, color='#878f92')
for i, (phase, label) in enumerate((('DP','抽卡'), ('SP','准备'),
                                  ('M1','主要1'), ('BP','战斗'),
                                  ('M2','主要2'), ('EP','结束'))):
    x = -29 + i*10
    rect(x-4, 46, x+4, 52, fill='#313a3e', outline='#87948e', width=1)
    text(x, 47.9, phase, size=16, bold=True)
    text(x, 50.5, label, size=12, color='#a1a9a8')
    controls.append({'id': f'p1_phase_{phase.lower()}', 'owner':'p1',
                     'kind':'phase', 'phase':phase, 'label':label,
                     'center':[x, .95, 49], 'size':[8, 6], 'rotation_y':0})
rect(29, 46, 40, 52, fill='#35403a', outline='#a1ab8d', width=1)
text(34.5, 48, '确认', size=17)
text(34.5, 50.7, '准备 / 响应', size=10, color='#aab49e')
controls.append({'id':'p1_confirm', 'owner':'p1', 'kind':'confirm',
                 'label':'准备 / 响应确认', 'center':[34.5,.95,49],
                 'size':[11,6], 'rotation_y':0})
rect(-58, 46, -38, 52, fill='#152420', outline='#718d75', width=1)
text(-48, 47.2, '生命值 LP', size=11, color='#8fb897')
text(-48, 50, '8000', size=24, color='#b0c9a6', bold=True)
controls.append({'id':'p1_lp_display','owner':'p1','kind':'lp_display',
                 'label':'生命值','center':[-48,.9,49], 'size':[20,6], 'rotation_y':0})
text(49.5, 49, '决斗', size=19, color='#a6a99a', bold=True)

near = ATLAS.crop((0, 448, 1024, 896))
ATLAS.paste(near.transpose(Image.Transpose.ROTATE_180), (0, 0))
D = ImageDraw.Draw(ATLAS)
rect(-41, -.12, 41, .12, fill='#88816b')
text(0, -1.5, '共享', size=13, color='#a5a6a3')
text(0, 1.5, 'EXTRA', size=13, color='#a5a6a3')

for p1 in zones[:]:
    p2 = json.loads(json.dumps(p1))
    p2['id'] = p1['id'].replace('p1_', 'p2_', 1)
    p2['owner'] = 'p2'
    for key in ('center', 'card_anchor'):
        p2[key][0] *= -1
        p2[key][2] *= -1
    p2['rotation_y'] = 180
    x, _, z = p2['center']
    p2['atlas_uv'] = [x+58, z+49, x+70, z+63]
    zones.append(p2)
for p1 in controls[:]:
    p2 = json.loads(json.dumps(p1))
    p2['id'] = p1['id'].replace('p1_', 'p2_', 1)
    p2['owner'] = 'p2'
    p2['center'][0] *= -1
    p2['center'][2] *= -1
    p2['rotation_y'] = 180
    controls.append(p2)

# The banished rectangles straddle the half-mat join, so redraw them after
# mirroring the halves. Keep each owner's label upright from their viewpoint.
x, z = 52, 2
inside, edge = palette['banished']
rect(x-6, z-7, x+6, z+7, fill=inside, outline=edge, width=2)
rect(x-5.5, z-6.5, x+5.5, z+6.5, outline='#4e5150', width=1)
rect(x-3.4375, z-4.7, x+3.4375, z+5.3, outline='#474b4b', width=1)
text(x, z-5.65, '除外', size=13, color=edge)
banished_tile = ATLAS.crop((*point(46, -5), *point(58, 9)))
ATLAS.paste(banished_tile.transpose(Image.Transpose.ROTATE_180), point(-58, -9))
D = ImageDraw.Draw(ATLAS)

for side, x in (('left', -16), ('right', 16)):
    inside, edge = palette['extra_monster']
    rect(x-6, -7, x+6, 7, fill=inside, outline=edge, width=3)
    rect(x-5.5, -6.5, x+5.5, 6.5, outline='#4e6875', width=1)
    rect(x-3.4375, -5, x+3.4375, 5, outline='#435765', width=1)
    text(x, 5.7, '额外怪兽区', size=12, color=edge)
    zones.append({'id':f'shared_emz_{side}', 'owner':'shared',
                  'kind':'extra_monster', 'label':'额外怪兽区',
                  'sequence': None, 'center':[x,.82,0],
                  'card_anchor':[x,1.05,0], 'clear_rectangle':[12,14],
                  'rotation_y':0, 'pendulum':None,
                  'atlas_uv':[x+58,49,x+70,63],
                  'engine_mapping':{'p1_mzone':5 if side=='left' else 6,
                                    'p2_mzone':6 if side=='left' else 5}})

# Reserve bottom rows as material swatches, with an inset sample to avoid bleed.
swatches = {'base':'#171c20','binding':'#8b816b',
            'edge':'#333a3e','zone_side':'#232a2e',
            'control_side':'#384447','lp_side':'#24352e'}
uv_swatches = {}
for i, (key, color) in enumerate(swatches.items()):
    D.rectangle((i*128, 912, (i+1)*128-1, 1023), fill=color)
    uv_swatches[key] = [i*16+3, 116, i*16+13, 124]
for i, (kind, (_, color)) in enumerate(palette.items()):
    x0, y0 = 768+(i%4)*64, 912+(i//4)*48
    D.rectangle((x0,y0,x0+63,y0+47), fill=color)
    uv_swatches[kind+'_edge']=[x0/8+2,y0/8+2,x0/8+6,y0/8+4]

texdir = ROOT/'textures'
texdir.mkdir(parents=True, exist_ok=True)
ATLAS.save(texdir/'duel_field_atlas.png')
ATLAS.crop((0,0,1024,896)).save(ROOT/'duel_field_layout_plan.png')
layout = {
    'name':'mdpromod_duel_field_v1', 'version':1,
    'units_per_block':16, 'model_format':'free',
    'bounds':{'from':[-64,0,-56], 'to':[64,1.6,56]},
    'size_blocks':[8, .1, 7],
    'orientation':{'p1':'stands at +Z; own right is +X',
                   'p2':'stands at -Z; own right is -X'},
    'card_size_model_units':[6.875,10],
    'texture':{'file':'textures/duel_field_atlas.png',
               'bitmap_size':[1024,1024], 'logical_uv_size':[128,128],
               'surface_uv':[0,0,128,112], 'swatches':uv_swatches},
    'zones':zones, 'controls':controls,
    'rules':{'shared_extra_monster_zones':2, 'personal_zones_per_player':15,
             'pendulum_szone_sequences':[0,4], 'field_szone_sequence':5,
             'phase_controls_require_engine_authorization':True,
             'hands':'temporary Minecraft inventory',
             'cards_and_deck_stacks':'separate future models/entities'},
}
assert len(zones)==32
assert len(controls)==16
for zone in zones:
    x, _, z=zone['center']
    assert -64<=x-6<x+6<=64 and -56<=z-7<z+7<=56
    assert zone['clear_rectangle'][0]>=10  # Horizontal defense card fits.
(ROOT/'duel_field_layout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'zones':len(zones),'controls':len(controls),'atlas':'1024x1024'},ensure_ascii=False))
