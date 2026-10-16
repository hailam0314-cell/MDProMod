"""Create 10-card-segment main/extra deck models and a comparison preview."""
import base64
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, r'D:\MDProMod\tools\blockbench_mcp')
from client import call, initialize, text_result  # noqa: E402

ROOT = Path(__file__).resolve().parent
SPEC = json.loads((ROOT / 'segmented_deck_spec.json').read_text(encoding='utf-8'))
ATLAS = (ROOT / SPEC['texture']['file']).as_posix()
PITCH = SPEC['block_pitch']
SEG_H = SPEC['segment_height']
SEAM_H = SPEC['seam_height']
TOP_H = SPEC['top_card_height']
SHADOW_H = SPEC['shadow_height']
HALF_W = SPEC['card_footprint'][0] / 2
HALF_D = SPEC['card_footprint'][1] / 2
UV = SPEC['texture']

def select_new_project(result):
    uri = next(c['uri'] for c in result.get('content', [])
               if c.get('type') == 'resource_link'
               and c['uri'].startswith('blockbench://project/'))
    uuid = re.search(r'([0-9a-f-]{36})', uri).group(1)
    text_result(call('risky_eval', {
        'code': f"(() => {{const p=ModelProject.all.find(p=>p.uuid==='{uuid}');"
                f"if(!p)throw new Error('project not found');p.select();return p.name;}})()"}))
    return uuid

def add_group(name, parent, origin=(0, 0, 0), visibility=True):
    text_result(call('add_group', {
        'name': name, 'origin': list(origin), 'parent': parent,
        'visibility': visibility}))

def add_locator(name, parent, position):
    text_result(call('add_locator', {
        'name': name, 'parent': parent, 'position': position,
        'rotation': [0, 0, 0]}))

def add_cube(name, start, end, parent, faces, origin=(0, 0, 0)):
    text_result(call('place_cube', {
        'elements': [{'name': name, 'from': start, 'to': end,
                      'origin': list(origin), 'shade': False}],
        'texture': 'deck_segment_atlas', 'group': parent, 'faces': False}))
    text_result(call('set_cube_uv', {
        'id': name, 'box_uv': False, 'faces': faces}))

def face(uv, texture='deck_segment_atlas'):
    return {'uv': uv, 'texture': texture}

def side_faces(uv):
    return {key: face(uv) for key in ('north', 'south', 'east', 'west')}

def all_faces(uv):
    return {key: face(uv) for key in ('north', 'south', 'east', 'west', 'up', 'down')}

def set_visibility(group_name, visible):
    text_result(call('risky_eval', {
        'code': f"(() => {{const g=Group.all.find(g=>g.name==='{group_name}');"
                f"if(!g)throw new Error('missing group');g.visibility={str(visible).lower()};"
                f"Canvas.updateAll();return g.visibility;}})()"}))

def build_atlas_texture():
    text_result(call('create_texture', {
        'name': 'deck_segment_atlas', 'width': 512, 'height': 512,
        'uv_width': 512, 'uv_height': 512,
        'data': ATLAS, 'render_mode': 'default',
        'render_sides': 'double'}))

def add_stack(prefix, x, z, segments, partial_cards, root_group, include_top=True):
    """Add one static stack for the preview or one dynamic stack skeleton."""
    base_y = SHADOW_H
    add_cube(f'{prefix}_shadow',
             [x - HALF_W - 0.1125, 0, z - HALF_D - 0.15],
             [x + HALF_W + 0.1125, SHADOW_H, z + HALF_D + 0.15],
             root_group, all_faces(UV['shadow_uv']))
    for index in range(segments):
        y0 = base_y + index * PITCH
        y1 = y0 + SEG_H
        add_cube(f'{prefix}_segment_{index:02d}',
                 [x - HALF_W, y0, z - HALF_D],
                 [x + HALF_W, y1, z + HALF_D],
                 root_group,
                 {**side_faces(UV['segment_side_uv']),
                  'up': face(UV['dark_base_uv']),
                  'down': face(UV['dark_base_uv'])})
        add_cube(f'{prefix}_seam_{index:02d}',
                 [x - HALF_W + 0.0125, y1, z - HALF_D + 0.025],
                 [x + HALF_W - 0.0125, y1 + SEAM_H, z + HALF_D - 0.025],
                 root_group, all_faces(UV['seam_uv']))
    extra_height = 0.0
    if partial_cards:
        extra_height = (partial_cards / 10.0) * PITCH
        y0 = base_y + segments * PITCH
        partial_uv = [320, 0, 352, round(512 * partial_cards / 10)]
        add_cube(f'{prefix}_partial',
                 [x - HALF_W, y0, z - HALF_D],
                 [x + HALF_W, y0 + extra_height, z + HALF_D],
                 root_group,
                 {**side_faces(partial_uv),
                  'up': face(UV['dark_base_uv']),
                  'down': face(UV['dark_base_uv'])})
    top_y = base_y + segments * PITCH + extra_height
    if include_top:
        add_cube(f'{prefix}_top_card',
                 [x - HALF_W + 0.06, top_y, z - HALF_D - 0.04],
                 [x + HALF_W + 0.06, top_y + TOP_H, z + HALF_D - 0.04],
                 root_group,
                 {**side_faces(UV['card_edge_uv']),
                  'up': face(UV['card_back_uv']),
                  'down': face(UV['dark_base_uv'])})

def build_project(project_name, root_name):
    created = call('create_project', {'name': project_name, 'format': 'free'})
    uuid = select_new_project(created)
    build_atlas_texture()
    add_group(root_name, 'root')
    return uuid

def save_project(path):
    call('export_model', {'codec_id': 'project', 'path': path,
                          'overwrite': True, 'max_content_length': 0})
    text_result(call('risky_eval', {
        'code': f"(() => {{Codecs.project.afterSave('{path}');"
                f"return {{name:Project.name,path:Project.save_path,saved:Project.saved}};}})()"}))

initialize()
for view in text_result(call('list_views')).get('views', []):
    if view.get('kind') == 'offscreen':
        try:
            call('delete_offscreen_view', {'view': view['id']})
        except Exception:
            pass
try:
    call('risky_eval', {
        'code': "(() => {const removed=[];for(const p of ModelProject.all.slice()){"
                "if(['main_deck_dynamic_v1','extra_deck_dynamic_v1','deck_dynamic_preview_v1'].includes(p.name)){"
                "p.saved=true;p.close();removed.push(p.uuid);}}return removed;})()"})
except Exception:
    pass

# Main deck dynamic model: six full 10-card blocks, hidden partial block.
main_uuid = build_project('main_deck_dynamic_v1', 'main_deck_dynamic')
add_group('main_deck_segments', 'main_deck_dynamic')
add_group('main_deck_partial', 'main_deck_dynamic', origin=(0, SHADOW_H + 6 * PITCH, 0), visibility=False)
add_group('main_deck_top', 'main_deck_dynamic')
add_stack('MainDeck', 0, 0, 6, 0, 'main_deck_segments', include_top=False)
add_cube('MainDeck_Partial',
         [-HALF_W, SHADOW_H + 6 * PITCH, -HALF_D],
         [HALF_W, SHADOW_H + 6 * PITCH + SEG_H, HALF_D],
         'main_deck_partial',
         {**side_faces(UV['segment_side_uv']),
          'up': face(UV['dark_base_uv']),
          'down': face(UV['dark_base_uv'])})
add_cube('MainDeck_TopCard',
         [-HALF_W + 0.06, SHADOW_H + 6 * PITCH, -HALF_D - 0.04],
         [HALF_W + 0.06, SHADOW_H + 6 * PITCH + TOP_H, HALF_D - 0.04],
         'main_deck_top',
         {**side_faces(UV['card_edge_uv']),
          'up': face(UV['card_back_uv']),
          'down': face(UV['dark_base_uv'])})
for index in range(7):
    add_locator(f'main_deck_block_{index*10:02d}', 'main_deck_dynamic',
                [0, SHADOW_H + index * PITCH, 0])
for name, position in (
        ('main_deck_origin', [0, 0, 0]),
        ('main_deck_stack_base', [0, SHADOW_H, 0]),
        ('main_deck_partial_origin', [0, SHADOW_H + 6 * PITCH, 0]),
        ('main_deck_top_surface', [0, SHADOW_H + 6 * PITCH + TOP_H, 0]),
        ('main_deck_card_anchor', [0, SHADOW_H + 6 * PITCH + TOP_H, 0]),
        ('main_deck_vertical_scale_origin', [0, SHADOW_H, 0])):
    add_locator(name, 'main_deck_dynamic', position)
save_project((ROOT / 'main_deck_dynamic_v1.bbmodel').as_posix())

# Extra deck dynamic model: one full 10-card block plus hidden partial block.
extra_uuid = build_project('extra_deck_dynamic_v1', 'extra_deck_dynamic')
add_group('extra_deck_segments', 'extra_deck_dynamic')
add_group('extra_deck_partial', 'extra_deck_dynamic', origin=(0, SHADOW_H + PITCH, 0), visibility=True)
add_group('extra_deck_top', 'extra_deck_dynamic')
add_stack('ExtraDeck', 0, 0, 1, 0, 'extra_deck_segments', include_top=False)
add_cube('ExtraDeck_Partial',
         [-HALF_W, SHADOW_H + PITCH, -HALF_D],
         [HALF_W, SHADOW_H + PITCH + 0.20, HALF_D],
         'extra_deck_partial',
         {**side_faces([320, 0, 352, 256]),
          'up': face(UV['dark_base_uv']),
          'down': face(UV['dark_base_uv'])})
add_cube('ExtraDeck_TopCard',
         [-HALF_W + 0.06, SHADOW_H + PITCH + 0.20, -HALF_D - 0.04],
         [HALF_W + 0.06, SHADOW_H + PITCH + 0.20 + TOP_H, HALF_D - 0.04],
         'extra_deck_top',
         {**side_faces(UV['card_edge_uv']),
          'up': face(UV['card_back_uv']),
          'down': face(UV['dark_base_uv'])})
for name, position in (
        ('extra_deck_origin', [0, 0, 0]),
        ('extra_deck_stack_base', [0, SHADOW_H, 0]),
        ('extra_deck_block_10', [0, SHADOW_H + PITCH, 0]),
        ('extra_deck_partial_origin', [0, SHADOW_H + PITCH, 0]),
        ('extra_deck_top_surface', [0, SHADOW_H + PITCH + 0.20 + TOP_H, 0]),
        ('extra_deck_card_anchor', [0, SHADOW_H + PITCH + 0.20 + TOP_H, 0]),
        ('extra_deck_vertical_scale_origin', [0, SHADOW_H, 0])):
    add_locator(name, 'extra_deck_dynamic', position)
save_project((ROOT / 'extra_deck_dynamic_v1.bbmodel').as_posix())

# Static preview project: main deck 40/50/60 and extra deck 10/15.
preview_uuid = build_project('deck_dynamic_preview_v1', 'deck_dynamic_preview')
add_group('main_40', 'deck_dynamic_preview')
add_group('main_50', 'deck_dynamic_preview')
add_group('main_60', 'deck_dynamic_preview')
add_group('extra_10', 'deck_dynamic_preview')
add_group('extra_15', 'deck_dynamic_preview')
add_stack('Main40', -45, -18, 4, 0, 'main_40')
add_stack('Main50', 0, -18, 5, 0, 'main_50')
add_stack('Main60', 45, -18, 6, 0, 'main_60')
add_stack('Extra10', -20, 18, 1, 0, 'extra_10')
add_stack('Extra15', 20, 18, 1, 5, 'extra_15')
save_project((ROOT / 'deck_dynamic_preview_v1.bbmodel').as_posix())

# Render the static preview from one 3/4 view.
view_id = 'deck_dynamic_preview_' + preview_uuid[:8]
call('create_offscreen_view', {
    'id': view_id, 'width': 1800, 'height': 1050, 'copy_view': 'none'})
call('set_camera_angle', {
    'view': view_id, 'position': [125, 105, 155], 'target': [0, 0.8, 0],
    'projection': 'perspective', 'fov': 40, 'max_size': 1800})
capture = call('capture_screenshot', {
    'project': preview_uuid, 'view': view_id,
    'max_size': 1800, 'format': 'png'})
image = next(c for c in capture['content'] if c['type'] == 'image')
(ROOT / 'deck_dynamic_preview.png').write_bytes(base64.b64decode(image['data']))

print(json.dumps({
    'main_dynamic': 'main_deck_dynamic_v1.bbmodel',
    'extra_dynamic': 'extra_deck_dynamic_v1.bbmodel',
    'preview': 'deck_dynamic_preview_v1.bbmodel',
    'preview_image': 'deck_dynamic_preview.png',
}, ensure_ascii=False))
