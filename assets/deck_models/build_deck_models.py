"""Create the main-deck and extra-deck Blockbench models through MCP."""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, r'D:\MDProMod\tools\blockbench_mcp')
from client import call, initialize, text_result  # noqa: E402

ROOT = Path(__file__).resolve().parent
ATLAS_PATH = (ROOT / 'textures' / 'deck_atlas.png').as_posix()
SPEC = json.loads((ROOT / 'deck_models_spec.json').read_text(encoding='utf-8'))
UV = SPEC['atlas']

def uv_faces(uv, texture='deck_atlas'):
    return {face: {'uv': uv, 'texture': texture}
            for face in ('north', 'south', 'east', 'west', 'up', 'down')}

def place(name, start, end, texture, group, faces=True):
    element = {'name': name, 'from': start, 'to': end,
               'origin': [0, 0, 0], 'shade': False}
    result = call('place_cube', {
        'elements': [element], 'texture': texture,
        'group': group, 'faces': faces})
    text_result(result)
    return name

def set_uv(name, face_map):
    call('set_cube_uv', {'id': name, 'box_uv': False, 'faces': face_map})

def make_deck_project(project_name, deck_id, layout, file_name):
    result = call('create_project', {'name': project_name, 'format': 'free'})
    info = text_result(result)
    project_uri = next(c['uri'] for c in result.get('content', [])
                       if c.get('type') == 'resource_link' and c['uri'].startswith('blockbench://project/'))
    project_uuid = re.search(r'([0-9a-f-]{36})', project_uri).group(1)
    call('risky_eval', {
        'code': f"(() => {{const p=ModelProject.all.find(p=>p.uuid==='{project_uuid}');"
                f"if(!p)throw new Error('project not found');p.select();"
                f"return {{name:p.name,active:p===Project}};}})()"})
    call('create_texture', {
        'name': 'deck_atlas', 'width': 512, 'height': 512,
        'uv_width': 512, 'uv_height': 512,
        'data': ATLAS_PATH, 'render_mode': 'default',
        'render_sides': 'double'})

    call('add_group', {'name': deck_id, 'origin': [0, 0, 0], 'parent': 'root'})
    shadow_group = deck_id + '_shadow'
    stack_group = deck_id + '_stack'
    top_group = deck_id + '_top'
    call('add_group', {'name': shadow_group, 'origin': [0, 0, 0], 'parent': deck_id})
    call('add_group', {'name': stack_group, 'origin': [0, 0, 0], 'parent': deck_id})
    call('add_group', {'name': top_group, 'origin': [0, 0, 0], 'parent': deck_id})

    half_w = SPEC['card']['width'] / 2
    half_d = SPEC['card']['depth'] / 2
    shadow_start = [-half_w - 0.1125, 0, -half_d - 0.15]
    shadow_end = [half_w + 0.1125, layout['shadow_height'], half_d + 0.15]
    core_start = [-half_w, layout['shadow_height'], -half_d]
    core_end = [half_w, layout['shadow_height'] + layout['core_height'], half_d]
    top_start = [-half_w + 0.06, core_end[1], -half_d - 0.04]
    top_end = [half_w + 0.06, core_end[1] + layout['top_card_height'], half_d - 0.04]

    shadow = deck_id + '_shadow_cube'
    core = deck_id + '_stack_cube'
    top = deck_id + '_top_card'
    place(shadow, shadow_start, shadow_end, 'deck_atlas', shadow_group, False)
    set_uv(shadow, uv_faces(UV['shadow_uv']))
    place(core, core_start, core_end, 'deck_atlas', stack_group, False)
    set_uv(core, {
        'north': {'uv': UV['main_deck_side_uv'] if deck_id == 'main_deck' else UV['extra_deck_side_uv'], 'texture': 'deck_atlas'},
        'south': {'uv': UV['main_deck_side_uv'] if deck_id == 'main_deck' else UV['extra_deck_side_uv'], 'texture': 'deck_atlas'},
        'east':  {'uv': UV['main_deck_side_uv'] if deck_id == 'main_deck' else UV['extra_deck_side_uv'], 'texture': 'deck_atlas'},
        'west':  {'uv': UV['main_deck_side_uv'] if deck_id == 'main_deck' else UV['extra_deck_side_uv'], 'texture': 'deck_atlas'},
        'up':    {'uv': UV['dark_base_uv'], 'texture': 'deck_atlas'},
        'down':  {'uv': UV['dark_base_uv'], 'texture': 'deck_atlas'}})
    place(top, top_start, top_end, 'deck_atlas', top_group, False)
    set_uv(top, {
        'north': {'uv': UV['card_edge_uv'], 'texture': 'deck_atlas'},
        'south': {'uv': UV['card_edge_uv'], 'texture': 'deck_atlas'},
        'east':  {'uv': UV['card_edge_uv'], 'texture': 'deck_atlas'},
        'west':  {'uv': UV['card_edge_uv'], 'texture': 'deck_atlas'},
        'up':    {'uv': UV['card_back_uv'], 'texture': 'deck_atlas'},
        'down':  {'uv': UV['dark_base_uv'], 'texture': 'deck_atlas'}})
    place(deck_id + '_top_gap', [core_start[0]-0.02, core_end[1]-0.015, core_start[2]-0.02],
          [core_end[0]+0.02, core_end[1]+0.015, core_end[2]+0.02], 'deck_atlas', stack_group, False)
    set_uv(deck_id + '_top_gap', uv_faces(UV['shadow_uv']))

    for locator, position in (
            ('origin', [0, 0, 0]),
            ('stack_surface', [0, layout['shadow_height'], 0]),
            ('top_surface', [0.06, layout['stack_height'], -0.04]),
            ('card_anchor', [0, layout['stack_height'], 0]),
            ('vertical_scale_origin', [0, layout['shadow_height'], 0])):
        call('add_locator', {
            'name': f'{deck_id}_{locator}',
            'parent': deck_id,
            'position': position,
            'rotation': [0, 0, 0]})

    save_path = (ROOT / file_name).as_posix()
    call('export_model', {
        'codec_id': 'project', 'path': save_path,
        'overwrite': True, 'max_content_length': 0})
    call('risky_eval', {
        'code': f"(() => {{Codecs.project.afterSave('{save_path}');"
                f"return {{name:Project.name,path:Project.save_path,saved:Project.saved}};}})()"})

    view_id = deck_id + '_preview_' + project_uuid[:8]
    try:
        call('delete_offscreen_view', {'view': view_id})
    except RuntimeError:
        pass
    call('create_offscreen_view', {
        'id': view_id, 'width': 1200, 'height': 850, 'copy_view': 'none'})
    call('set_camera_angle', {
        'view': view_id,
        'position': [18, 14, 18] if deck_id == 'main_deck' else [15, 12, 15],
        'target': [0, layout['stack_height'] / 2, 0],
        'projection': 'perspective', 'fov': 38, 'max_size': 1200})
    capture = call('capture_screenshot', {
        'project': project_uuid, 'view': view_id,
        'max_size': 1200, 'format': 'png'})
    image = next(c for c in capture['content'] if c['type'] == 'image')
    import base64
    (ROOT / f'{deck_id}_preview.png').write_bytes(base64.b64decode(image['data']))
    return {'project': info, 'file': save_path}

initialize()
try:
    call('risky_eval', {
        'code': "(() => {const removed=[];for(const p of ModelProject.all.slice()){"
                "if(p.name==='main_deck_v1'||p.name==='extra_deck_v1'){p.saved=true;p.close();removed.push(p.uuid);}}"
                "return removed;})()"})
except Exception:
    pass
print(json.dumps({
    'main': make_deck_project('main_deck_v1', 'main_deck',
                              SPEC['main_deck'], 'main_deck_v1.bbmodel'),
    'extra': make_deck_project('extra_deck_v1', 'extra_deck',
                               SPEC['extra_deck'], 'extra_deck_v1.bbmodel'),
}, ensure_ascii=False))
