"""Create the shared single-card Blockbench model through MCP."""
import base64
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, r'D:\MDProMod\tools\blockbench_mcp')
from client import call, initialize, text_result  # noqa: E402

ROOT = Path(__file__).resolve().parent
SPEC = json.loads((ROOT / 'single_card_spec.json').read_text(encoding='utf-8'))
FACE_PATH = (ROOT / SPEC['textures']['face_placeholder']).as_posix()
BACK_PATH = (ROOT / SPEC['textures']['back']).as_posix()
EDGE_PATH = (ROOT / SPEC['textures']['edge']).as_posix()

def group(name, parent, visibility=True):
    return text_result(call('add_group', {
        'name': name, 'origin': [0, 0, 0], 'parent': parent,
        'visibility': visibility}))

def cube(name, start, end, parent, faces):
    text_result(call('place_cube', {
        'elements': [{'name': name, 'from': start, 'to': end,
                      'origin': [0, 0, 0], 'shade': False}],
        'texture': 'card_edge', 'group': parent, 'faces': False}))
    text_result(call('set_cube_uv', {
        'id': name, 'box_uv': False, 'faces': faces}))

def locator(name, parent, position):
    text_result(call('add_locator', {
        'name': name, 'parent': parent, 'position': position,
        'rotation': [0, 0, 0]}))

initialize()
try:
    for view in text_result(call('list_views')).get('views', []):
        if view.get('kind') == 'offscreen':
            try:
                call('delete_offscreen_view', {'view': view['id']})
            except Exception:
                pass
except Exception:
    pass
try:
    call('risky_eval', {
        'code': "(() => {const removed=[];for(const p of ModelProject.all.slice()){"
                "if(p.name==='single_card_v1'){p.saved=true;p.close();removed.push(p.uuid);}}"
                "return removed;})()"})
except Exception:
    pass

created = call('create_project', {'name': 'single_card_v1', 'format': 'free'})
project_uri = next(c['uri'] for c in created.get('content', [])
                   if c.get('type') == 'resource_link'
                   and c['uri'].startswith('blockbench://project/'))
project_uuid = re.search(r'([0-9a-f-]{36})', project_uri).group(1)
text_result(call('risky_eval', {
    'code': f"(() => {{const p=ModelProject.all.find(p=>p.uuid==='{project_uuid}');"
            f"if(!p)throw new Error('project not found');p.select();return p.name;}})()"}))

for name, path, w, h in (
        ('card_face_placeholder', FACE_PATH, 704, 1024),
        ('card_back', BACK_PATH, 320, 467),
        ('card_edge', EDGE_PATH, 32, 64)):
    text_result(call('create_texture', {
        'name': name, 'width': w, 'height': h,
        'uv_width': w, 'uv_height': h,
        'data': path, 'render_mode': 'default',
        'render_sides': 'double'}))

group('single_card', 'root')
group('card_body', 'single_card', True)

edge_faces = {
    'north': {'uv': SPEC['uv']['edge'], 'texture': 'card_edge'},
    'south': {'uv': SPEC['uv']['edge'], 'texture': 'card_edge'},
    'east': {'uv': SPEC['uv']['edge'], 'texture': 'card_edge'},
    'west': {'uv': SPEC['uv']['edge'], 'texture': 'card_edge'},
}
underside = {'uv': SPEC['uv']['underside'], 'texture': 'card_edge'}

cube('Card_Body', [-3.4375, 0, -5], [3.4375, 0.06, 5],
     'card_body', {
         **edge_faces,
         'up': {'uv': SPEC['uv']['face'], 'texture': 'card_face_placeholder'},
         'down': {'uv': SPEC['uv']['back'], 'texture': 'card_back'},
     })

for name, position in (
        ('card_origin', [0, 0, 0]),
        ('card_face_surface', [0, 0.061, 0]),
        ('card_back_surface', [0, -0.001, 0]),
        ('card_attack_anchor', [0, 0.03, 0]),
        ('card_defense_anchor', [0, 0.03, 0]),
        ('card_texture_swap', [0, 0.062, 0])):
    locator(name, 'single_card', position)

save_path = (ROOT / 'single_card_v1.bbmodel').as_posix()
call('export_model', {'codec_id': 'project', 'path': save_path,
                      'overwrite': True, 'max_content_length': 0})
text_result(call('risky_eval', {
    'code': f"(() => {{Codecs.project.afterSave('{save_path}');"
            f"return {{name:Project.name,path:Project.save_path,saved:Project.saved}};}})()"}))

view_suffix = project_uuid[:8]
face_view = 'card_face_preview_' + view_suffix
back_view = 'card_back_preview_' + view_suffix
persp_view = 'card_perspective_preview_' + view_suffix
for view in (face_view, back_view, persp_view):
    try:
        call('delete_offscreen_view', {'view': view})
    except Exception:
        pass

call('create_offscreen_view', {'id': face_view, 'width': 820,
                               'height': 1180, 'copy_view': 'none'})
call('create_offscreen_view', {'id': back_view, 'width': 820,
                               'height': 1180, 'copy_view': 'none'})
call('create_offscreen_view', {'id': persp_view, 'width': 1100,
                               'height': 820, 'copy_view': 'none'})

def capture(view, file_name):
    result = call('capture_screenshot', {
        'project': project_uuid, 'view': view,
        'max_size': 1180, 'format': 'png'})
    image = next(c for c in result['content'] if c['type'] == 'image')
    (ROOT / file_name).write_bytes(base64.b64decode(image['data']))

call('set_camera_angle', {
    'view': face_view, 'position': [0, 30, 0], 'target': [0, 0, 0],
    'projection': 'orthographic', 'locked_angle': 'top',
    'zoom': 2.2, 'max_size': 1180})
capture(face_view, 'single_card_face_preview.png')

call('set_camera_angle', {
    'view': back_view, 'position': [0, -30, 0], 'target': [0, 0, 0],
    'projection': 'orthographic', 'locked_angle': 'bottom',
    'zoom': 2.2, 'max_size': 1180})
capture(back_view, 'single_card_back_preview.png')

call('set_camera_angle', {
    'view': persp_view, 'position': [8, 7, 10], 'target': [0, 0, 0],
    'projection': 'perspective', 'fov': 34, 'max_size': 1100})
capture(persp_view, 'single_card_perspective_preview.png')

print(json.dumps({
    'project': 'single_card_v1',
    'file': save_path,
    'views': [face_view, back_view, persp_view],
}, ensure_ascii=False))
