"""Small local HTTP client for the already installed Blockbench MCP plugin."""
import json
import sys
from pathlib import Path
from urllib.request import Request, urlopen

ENDPOINT = 'http://127.0.0.1:3000/bb-mcp'
STATE = Path(__file__).with_name('session.json')
request_id = 0

def rpc(method, params=None, session=None, notification=False):
    global request_id
    request_id += 1
    payload = {'jsonrpc': '2.0', 'method': method}
    if not notification:
        payload['id'] = request_id
    if params is not None:
        payload['params'] = params
    headers = {'Content-Type': 'application/json',
               'Accept': 'application/json, text/event-stream'}
    if session:
        headers['mcp-session-id'] = session
        headers['mcp-protocol-version'] = '2025-03-26'
    req = Request(ENDPOINT, data=json.dumps(payload).encode(), headers=headers)
    with urlopen(req, timeout=30) as response:
        data = response.read()
        body = json.loads(data) if data else {}
        new_session = response.headers.get('mcp-session-id')
    if 'error' in body:
        raise RuntimeError(json.dumps(body['error'], ensure_ascii=False))
    return body.get('result', {}), new_session

def initialize():
    result, session = rpc('initialize', {
        'protocolVersion': '2025-03-26', 'capabilities': {},
        'clientInfo': {'name': 'Codex-MDProMod', 'version': '0.1.0'}})
    if not session:
        raise RuntimeError('Blockbench did not return a session ID')
    STATE.write_text(json.dumps({'session': session}), encoding='utf-8')
    rpc('notifications/initialized', session=session, notification=True)
    return result

def request(method, params=None):
    session = json.loads(STATE.read_text(encoding='utf-8'))['session']
    return rpc(method, params, session)[0]

def call(name, arguments=None):
    result = request('tools/call', {'name': name, 'arguments': arguments or {}})
    if result.get('isError'):
        raise RuntimeError(json.dumps(result['content'], ensure_ascii=False))
    return result

def text_result(result):
    if 'structuredContent' in result:
        return result['structuredContent']
    parts = [c['text'] for c in result.get('content', []) if c.get('type') == 'text']
    if len(parts) == 1:
        try:
            return json.loads(parts[0])
        except json.JSONDecodeError:
            return parts[0]
    return parts

if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    if sys.argv[1] == 'connect':
        output = {'server': initialize(), 'project': text_result(call('get_project_info')),
                  'views': text_result(call('list_views')),
                  'resources': request('resources/list'),
                  'tools': [{'name': t['name'], 'inputSchema': t['inputSchema']}
                            for t in request('tools/list')['tools']
                            if t['name'] in {'export_model', 'create_texture', 'risky_eval',
                                            'get_texture', 'capture_screenshot', 'set_camera_angle',
                                            'create_offscreen_view', 'get_cube_uv'}]}
        print(json.dumps(output, ensure_ascii=False))
    elif sys.argv[1] == 'call':
        arguments = json.loads(Path(sys.argv[3]).read_text(encoding='utf-8')) if len(sys.argv)>3 else {}
        print(json.dumps(text_result(call(sys.argv[2], arguments)), ensure_ascii=False))
