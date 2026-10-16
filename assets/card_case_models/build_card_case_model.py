"""Create the default red DUELIST CARD CASE model through Blockbench MCP."""
import base64
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, r'D:\MDProMod\tools\blockbench_mcp')
from client import call, initialize, text_result  # noqa: E402

ROOT = Path(__file__).resolve().parent
SPEC = json.loads((ROOT / 'card_case_spec.json').read_text(encoding='utf-8'))
ATLAS = (ROOT / SPEC['textures']['atlas']).as_posix()
CARD_BACK = (ROOT / SPEC['textures']['card_back']).as_posix()
UV = SPEC['uv']

def add_group(name, parent, origin=(0, 0, 0), visibility=True):
    text_result(call('add_group', {
        'name': name, 'origin': list(origin), 'parent': parent,
        'visibility': visibility}))

def add_cube(name, start, end, parent, faces, origin=(0, 0, 0)):
    text_result(call('place_cube', {
        'elements': [{'name': name, 'from': start, 'to': end,
                      'origin': list(origin), 'shade': False}],
        'texture': 'card_case_atlas', 'group': parent, 'faces': False}))
    text_result(call('set_cube_uv', {
        'id': name, 'box_uv': False, 'faces': faces}))

def face(uv, texture='card_case_atlas'):
    return {'uv': uv, 'texture': texture}

def add_locator(name, parent, position, rotation=(0, 0, 0)):
    text_result(call('add_locator', {
        'name': name, 'parent': parent, 'position': position,
        'rotation': list(rotation)}))

def select_new_project(result):
    uri = next(c['uri'] for c in result.get('content', [])
               if c.get('type') == 'resource_link'
               and c['uri'].startswith('blockbench://project/'))
    uuid = re.search(r'([0-9a-f-]{36})', uri).group(1)
    text_result(call('risky_eval', {
        'code': f"(() => {{const p=ModelProject.all.find(p=>p.uuid==='{uuid}');"
                f"if(!p)throw new Error('project not found');p.select();return p.name;}})()"}))
    return uuid

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
                "if(p.name==='card_case_v1'){p.saved=true;p.close();removed.push(p.uuid);}}"
                "return removed;})()"})
except Exception:
    pass

created = call('create_project', {'name': 'card_case_v1', 'format': 'free'})
project_uuid = select_new_project(created)

for name, path, width, height in (
        ('card_case_atlas', ATLAS, 512, 512),
        ('card_back', CARD_BACK, 320, 467)):
    text_result(call('create_texture', {
        'name': name, 'width': width, 'height': height,
        'uv_width': width, 'uv_height': height,
        'data': path, 'render_mode': 'default',
        'render_sides': 'double'}))

add_group('card_case', 'root')
add_group('card_case_body', 'card_case')
add_group('card_case_lid', 'card_case',
          origin=SPEC['hinge']['position'], visibility=True)
add_group('card_case_deck', 'card_case_body')

red = face(UV['red'])
dark = face(UV['dark'])
interior = face(UV['interior'])
lid_interior = face(UV['lid_interior'])

# Lower body: four walls and a floor.
add_cube('Body_Front', [-4.2, 0, 2.45], [4.2, 8.0, 2.8], 'card_case_body', {
    'south': face(UV['body_front']), 'north': interior,
    'east': red, 'west': red, 'up': red, 'down': dark})
add_cube('Body_Back', [-4.2, 0, -2.8], [4.2, 8.0, -2.45], 'card_case_body', {
    'north': face(UV['body_back']), 'south': interior,
    'east': red, 'west': red, 'up': red, 'down': dark})
add_cube('Body_Left', [-4.2, 0, -2.45], [-3.85, 8.0, 2.45], 'card_case_body', {
    'west': face(UV['body_side']), 'east': interior,
    'north': red, 'south': red, 'up': red, 'down': dark})
add_cube('Body_Right', [3.85, 0, -2.45], [4.2, 8.0, 2.45], 'card_case_body', {
    'east': face(UV['body_side']), 'west': interior,
    'north': red, 'south': red, 'up': red, 'down': dark})
add_cube('Body_Bottom', [-4.2, 0, -2.8], [4.2, 0.35, 2.8], 'card_case_body', {
    'up': interior, 'down': dark,
    'north': red, 'south': red, 'east': red, 'west': red})

# Lid: four walls, a top plate and a stepped cap.
add_cube('Lid_Front', [-4.3, 8.0, 2.45], [4.3, 11.5, 2.9], 'card_case_lid', {
    'south': face(UV['lid_front']), 'north': lid_interior,
    'east': red, 'west': red, 'up': red, 'down': lid_interior})
add_cube('Lid_Back', [-4.3, 8.0, -2.9], [4.3, 11.5, -2.45], 'card_case_lid', {
    'north': face(UV['lid_back']), 'south': lid_interior,
    'east': red, 'west': red, 'up': red, 'down': lid_interior})
add_cube('Lid_Left', [-4.3, 8.0, -2.45], [-3.95, 11.5, 2.45], 'card_case_lid', {
    'west': face(UV['lid_side']), 'east': lid_interior,
    'north': red, 'south': red, 'up': red, 'down': lid_interior})
add_cube('Lid_Right', [3.95, 8.0, -2.45], [4.3, 11.5, 2.45], 'card_case_lid', {
    'east': face(UV['lid_side']), 'west': lid_interior,
    'north': red, 'south': red, 'up': red, 'down': lid_interior})
add_cube('Lid_Top', [-4.3, 11.5, -2.9], [4.3, 11.8, 2.9], 'card_case_lid', {
    'up': face(UV['lid_top']), 'down': lid_interior,
    'north': red, 'south': red, 'east': red, 'west': red})
add_cube('Lid_TopCap', [-3.6, 11.8, -2.3], [3.6, 12.0, 2.3], 'card_case_lid', {
    'up': face(UV['lid_top']), 'down': red,
    'north': red, 'south': red, 'east': red, 'west': red})

# Cards standing inside the case, face parallel to the box front.
add_cube('Deck_Stack', [-3.4375, 0.4, -1.2], [3.4375, 10.4, 1.2], 'card_case_deck', {
    'south': face([0, 0, 320, 467], 'card_back'),
    'north': face([0, 0, 320, 467], 'card_back'),
    'east': face(UV['stack_side']), 'west': face(UV['stack_side']),
    'up': face(UV['stack_side']), 'down': dark})

for name, position, rotation in (
        ('card_case_origin', [0, 0, 0], [0, 0, 0]),
        ('card_case_hinge', SPEC['hinge']['position'], SPEC['hinge']['closed_rotation']),
        ('card_case_deck_anchor', [0, 10.4, 0], [0, 0, 0]),
        ('card_case_front_center', [0, 5.75, 2.8], [0, 0, 0]),
        ('card_case_closed_rotation', SPEC['hinge']['position'], SPEC['hinge']['closed_rotation']),
        ('card_case_open_rotation', SPEC['hinge']['position'], SPEC['hinge']['open_rotation'])):
    add_locator(name, 'card_case', position, rotation)

save_path = (ROOT / 'card_case_v1.bbmodel').as_posix()
call('export_model', {'codec_id': 'project', 'path': save_path,
                      'overwrite': True, 'max_content_length': 0})
text_result(call('risky_eval', {
    'code': f"(() => {{Codecs.project.afterSave('{save_path}');"
            f"return {{name:Project.name,path:Project.save_path,saved:Project.saved}};}})()"}))

def set_lid_rotation(angle):
    text_result(call('risky_eval', {
        'code': f"(() => {{const g=Group.all.find(g=>g.name==='card_case_lid');"
                f"if(!g)throw new Error('missing lid');g.rotation=[{angle},0,0];"
                f"Canvas.updateAll();return g.rotation;}})()"}))

suffix = project_uuid[:8]
closed_view = 'card_case_closed_' + suffix
open_view = 'card_case_open_' + suffix
for view in (closed_view, open_view):
    try:
        call('delete_offscreen_view', {'view': view})
    except Exception:
        pass

set_lid_rotation(0)
call('create_offscreen_view', {
    'id': closed_view, 'width': 1000, 'height': 1200, 'copy_view': 'none'})
call('set_camera_angle', {
    'view': closed_view, 'position': [24, 20, 30], 'target': [0, 6.0, 0],
    'projection': 'perspective', 'fov': 34, 'max_size': 1200})
closed_capture = call('capture_screenshot', {
    'project': project_uuid, 'view': closed_view,
    'max_size': 1200, 'format': 'png'})
closed_image = next(c for c in closed_capture['content'] if c['type'] == 'image')
(ROOT / 'card_case_closed_preview.png').write_bytes(
    base64.b64decode(closed_image['data']))

set_lid_rotation(SPEC['hinge']['open_rotation'][0])
call('create_offscreen_view', {
    'id': open_view, 'width': 1000, 'height': 1200, 'copy_view': 'none'})
call('set_camera_angle', {
    'view': open_view, 'position': [26, 22, 32], 'target': [0, 7.0, 0],
    'projection': 'perspective', 'fov': 36, 'max_size': 1200})
open_capture = call('capture_screenshot', {
    'project': project_uuid, 'view': open_view,
    'max_size': 1200, 'format': 'png'})
open_image = next(c for c in open_capture['content'] if c['type'] == 'image')
(ROOT / 'card_case_open_preview.png').write_bytes(
    base64.b64decode(open_image['data']))
set_lid_rotation(0)

print(json.dumps({
    'project': 'card_case_v1',
    'file': save_path,
    'closed_view': closed_view,
    'open_view': open_view,
}, ensure_ascii=False))
