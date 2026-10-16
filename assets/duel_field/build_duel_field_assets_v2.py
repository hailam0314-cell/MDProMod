"""决斗场地 v2：16 x 14 方块、全部坐标对齐方块格（单位 16 = 1 方块）。

输出：
  textures/duel_field_atlas_v2.png   2048 x 1792（正好是场地表面，没有色板条）
  duel_field_layout_v2.json          32 个卡片区 + 16 个操作区
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
PX_PER_UNIT = 8
UNIT = 16                      # 每方块 16 模型单位
HALF_W, HALF_D = 128, 112      # 16 x 14 方块
IMG_W, IMG_H = HALF_W * 2 * PX_PER_UNIT, HALF_D * 2 * PX_PER_UNIT
FONT = 'C:/Windows/Fonts/msyh.ttc'
FONT_BOLD = 'C:/Windows/Fonts/msyhbd.ttc'

ATLAS = Image.new('RGB', (IMG_W, IMG_H), '#151922')
D = ImageDraw.Draw(ATLAS)


def point(x, z):
    return (round((x + HALF_W) * PX_PER_UNIT), round((z + HALF_D) * PX_PER_UNIT))


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

# ---------------------------------------------------------------- 近半场（P1，z > 0）
rect(-HALF_W, 0, HALF_W, HALF_D, fill='#23272b')
rect(-126, 2, 126, 110, outline='#817a65', width=2)
rect(-124, 4, 124, 108, outline='#363c40', width=1)
for z in range(6, 110, 4):
    for x in range(-120, 121, 6):
        D.point(point(x, z), fill='#2a2f33')
for x in (-88, 88):
    D.line((*point(x, 16), *point(x, 88)), fill='#414548', width=2)


def draw_zone(identifier, owner, kind, label, x, z, sequence=None, pendulum=None):
    inside, edge = palette[kind]
    rect(x - 12, z - 14, x + 12, z + 14, fill=inside, outline=edge, width=3)
    rect(x - 11, z - 13, x + 11, z + 13, outline='#4e5150', width=1)
    # 卡图落点：13.75 x 20 模型单位（竖放），横放也放得下
    rect(x - 6.875, z - 10, x + 6.875, z + 10, outline='#474b4b', width=1)
    text(x, z - 11.5, label, size=20, color=edge)
    zones.append({
        'id': identifier, 'owner': owner, 'kind': kind,
        'label': label, 'sequence': sequence,
        'center': [x, 1.64, z], 'card_anchor': [x, 2.0, z],
        'clear_rectangle': [24, 28], 'rotation_y': 0,
        'pendulum': pendulum,
        'atlas_uv': [x + 116, z + 98, x + 140, z + 126],
    })


for i, x in enumerate((-64, -32, 0, 32, 64)):
    draw_zone(f'p1_mzone_{i}', 'p1', 'monster', f'怪兽 {i + 1}', x, 32, i)
    draw_zone(f'p1_szone_{i}', 'p1', 'spell_trap', f'魔陷 {i + 1}', x, 64, i,
              ('left' if i == 0 else 'right' if i == 4 else None))
draw_zone('p1_deck', 'p1', 'deck', '主卡组', 96, 64)
draw_zone('p1_extra', 'p1', 'extra', '额外卡组', -96, 64)
draw_zone('p1_graveyard', 'p1', 'graveyard', '墓地', 96, 32)
draw_zone('p1_banished', 'p1', 'banished', '除外', 96, 0)
draw_zone('p1_field', 'p1', 'field', '场地魔法', -96, 32, 5)

# 阶段条（故意与所有卡片区分离）
rect(-118, 96, 118, 108, fill='#1a2024', outline='#777764', width=2)
rect(-116, 96.5, -74, 107.5, fill='#152420', outline='#718d75', width=1)
text(-95, 99, '生命值 LP', size=13, color='#8fb897')
text(-95, 104, '8000', size=26, color='#b0c9a6', bold=True)
controls.append({'id': 'p1_lp_display', 'owner': 'p1', 'kind': 'lp_display',
                 'label': '生命值', 'center': [-95, 1.8, 102], 'size': [42, 12], 'rotation_y': 0})
for i, (phase, label) in enumerate((('DP', '抽卡'), ('SP', '准备'), ('M1', '主要1'),
                                    ('BP', '战斗'), ('M2', '主要2'), ('EP', '结束'))):
    x = -55 + i * 22
    rect(x - 10, 97, x + 10, 107, fill='#313a3e', outline='#87948e', width=1)
    text(x, 100, phase, size=17, bold=True)
    text(x, 104, label, size=12, color='#a1a9a8')
    controls.append({'id': f'p1_phase_{phase.lower()}', 'owner': 'p1', 'kind': 'phase',
                     'phase': phase, 'label': label, 'center': [x, 1.8, 102],
                     'size': [20, 10], 'rotation_y': 0})
rect(74, 96.5, 116, 107.5, fill='#35403a', outline='#a1ab8d', width=1)
text(95, 100, '确认', size=18)
text(95, 104.5, '准备 / 响应', size=11, color='#aab49e')
controls.append({'id': 'p1_confirm', 'owner': 'p1', 'kind': 'confirm',
                 'label': '准备 / 响应确认', 'center': [95, 1.8, 102],
                 'size': [42, 12], 'rotation_y': 0})

# ---------------------------------------------------------------- 镜像出远半场（P2）
half = ATLAS.crop((0, IMG_H // 2, IMG_W, IMG_H))
ATLAS.paste(half.transpose(Image.Transpose.ROTATE_180), (0, 0))
D = ImageDraw.Draw(ATLAS)

for p1 in zones[:]:
    p2 = json.loads(json.dumps(p1))
    p2['id'] = p1['id'].replace('p1_', 'p2_', 1)
    p2['owner'] = 'p2'
    for key in ('center', 'card_anchor'):
        p2[key][0] *= -1
        p2[key][2] *= -1
    p2['rotation_y'] = 180
    x, _, z = p2['center']
    p2['atlas_uv'] = [x + 116, z + 98, x + 140, z + 126]
    zones.append(p2)
for p1 in controls[:]:
    p2 = json.loads(json.dumps(p1))
    p2['id'] = p1['id'].replace('p1_', 'p2_', 1)
    p2['owner'] = 'p2'
    p2['center'][0] *= -1
    p2['center'][2] *= -1
    p2['rotation_y'] = 180
    controls.append(p2)

# 中线（共享区）与两个额外怪兽区
rect(-HALF_W, -1, HALF_W, 1, fill='#88816b')
text(-40, -3, '共享', size=16, color='#a5a6a3')
text(0, -3, 'EXTRA', size=16, color='#a5a6a3')
for side, x in (('left', -32), ('right', 32)):
    inside, edge = palette['extra_monster']
    rect(x - 12, -14, x + 12, 14, fill=inside, outline=edge, width=4)
    rect(x - 11, -13, x + 11, 13, outline='#4e6875', width=1)
    rect(x - 6.875, -10, x + 6.875, 10, outline='#435765', width=1)
    text(x, 11.5, '额外怪兽区', size=18, color=edge)
    zones.append({'id': f'shared_emz_{side}', 'owner': 'shared',
                  'kind': 'extra_monster', 'label': '额外怪兽区',
                  'sequence': None, 'center': [x, 1.64, 0],
                  'card_anchor': [x, 2.0, 0], 'clear_rectangle': [24, 28],
                  'rotation_y': 0, 'pendulum': None,
                  'atlas_uv': [x + 116, 98, x + 140, 126],
                  'engine_mapping': {'p1_mzone': 5 if side == 'left' else 6,
                                     'p2_mzone': 6 if side == 'left' else 5}})

texdir = ROOT / 'textures'
texdir.mkdir(parents=True, exist_ok=True)
ATLAS.save(texdir / 'duel_field_atlas_v2.png')

layout = {
    'name': 'mdpromod_duel_field_v2', 'version': 2,
    'units_per_block': UNIT, 'model_format': 'free',
    'bounds': {'from': [-HALF_W, 0, -HALF_D], 'to': [HALF_W, 3.2, HALF_D]},
    'size_blocks': [HALF_W * 2 / UNIT, 0.1, HALF_D * 2 / UNIT],
    'orientation': {'p1': 'stands at +Z; own right is +X',
                    'p2': 'stands at -Z; own right is -X'},
    'card_size_model_units': [13.75, 20],
    'texture': {'file': 'textures/duel_field_atlas_v2.png',
                'bitmap_size': [IMG_W, IMG_H],
                'logical_uv_size': [HALF_W * 2, HALF_D * 2],
                'surface_uv': [0, 0, HALF_W * 2, HALF_D * 2]},
    'zones': zones, 'controls': controls,
    'rules': {'shared_extra_monster_zones': 2, 'personal_zones_per_player': 15,
              'pendulum_szone_sequences': [0, 4], 'field_szone_sequence': 5,
              'phase_controls_require_engine_authorization': True,
              'hands': 'temporary Minecraft inventory',
              'cards_and_deck_stacks': 'separate future models/entities'},
}
assert len(zones) == 32, len(zones)
assert len(controls) == 16, len(controls)
for zone in zones:
    x, _, z = zone['center']
    assert x % UNIT == 0 and z % UNIT == 0, zone['id']
    assert -HALF_W <= x - 12 < x + 12 <= HALF_W and -HALF_D <= z - 14 < z + 14 <= HALF_D
(ROOT / 'duel_field_layout_v2.json').write_text(
    json.dumps(layout, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'zones': len(zones), 'controls': len(controls),
                  'atlas': f'{IMG_W}x{IMG_H}', 'size_blocks': '16 x 14'}, ensure_ascii=False))
