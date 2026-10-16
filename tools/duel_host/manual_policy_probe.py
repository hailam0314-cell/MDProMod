"""Drive the manual prompt protocol with a small deterministic player policy."""
import json
import queue
import subprocess
import threading
import time
from pathlib import Path

STAGING = Path(r'D:\MDProMod\tools\ygo_tcp_probe\staging')
DOTNET = r'D:\MDProMod\tools\dotnet\dotnet.exe'
HOST = r'D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll'
SEED = 0x12345678
MAX_PROMPTS = 600
TIMEOUT = 150


def bytes_for_cards(prompt):
    if int(prompt.get('min', 0)) <= 0:
        return '00'
    count = int(prompt.get('min', 0))
    return bytes([count] + list(range(count))).hex().upper()


def bytes_for_places(prompt):
    places = prompt.get('places') or []
    count = max(1, int(prompt.get('count', 1)))
    output = bytearray()
    for index in range(count):
        place = places[index % len(places)] if places else {
            'player': 0, 'location': 4, 'sequence': 0}
        output.extend([int(place.get('player', 0)),
                       int(place.get('location', 4)),
                       int(place.get('sequence', 0))])
    return output.hex().upper()


def choose_response(prompt):
    function = int(prompt.get('function', 0))
    if function == 11:
        return {'cmd': 'response', 'value': 7}
    if function == 10:
        return {'cmd': 'response', 'value': 3}
    if function == 16:
        return {'cmd': 'response', 'value': -1}
    if function in (12, 13, 14):
        return {'cmd': 'response', 'value': 0}
    if function == 15:
        return {'cmd': 'responseBytes', 'hex': bytes_for_cards(prompt)}
    if function in (18, 24):
        return {'cmd': 'responseBytes', 'hex': bytes_for_places(prompt)}
    if function == 19:
        options = prompt.get('options') or []
        value = options[0].get('index', 1) if options else 1
        return {'cmd': 'response', 'value': int(value)}
    if function == 20:
        if prompt.get('cancelable') and int(prompt.get('min', 0)) == 0:
            return {'cmd': 'response', 'value': -1}
        return {'cmd': 'responseBytes', 'hex': bytes_for_cards(prompt)}
    if function == 22:
        count = int(prompt.get('count', 0))
        quantity = int(prompt.get('quantity', 0))
        output = bytearray(count * 2)
        if count and quantity > 0:
            output[0] = quantity & 0xFF
            output[1] = (quantity >> 8) & 0xFF
        return {'cmd': 'responseBytes', 'hex': output.hex().upper()}
    if function == 23:
        total = max(int(prompt.get('must', 0)), int(prompt.get('min', 0)))
        total = min(255, total)
        output = bytes([total] + list(range(total)))
        return {'cmd': 'responseBytes', 'hex': output.hex().upper()}
    if function == 25:
        count = int(prompt.get('count', 0))
        output = bytes([count] + list(range(count)))
        return {'cmd': 'responseBytes', 'hex': output.hex().upper()}
    if function == 26:
        if prompt.get('cancelable') and int(prompt.get('min', 0)) == 0:
            return {'cmd': 'response', 'value': -1}
        return {'cmd': 'responseBytes', 'hex': bytes_for_cards(prompt)}
    return {'cmd': 'response', 'value': 0}


process = subprocess.Popen(
    [DOTNET, HOST, '--root', str(STAGING)],
    cwd=str(STAGING), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
    stderr=subprocess.PIPE, text=True, encoding='utf-8', errors='replace')
lines = queue.Queue()


def read_stdout():
    for line in process.stdout:
        lines.put(line.rstrip())


threading.Thread(target=read_stdout, daemon=True).start()
process.stdin.write(json.dumps({
    'cmd': 'start', 'auto': False, 'seed': SEED,
    'deck0': 'starter.ydk', 'deck1': 'starter.ydk'}) + '\n')
process.stdin.flush()

events = []
prompt_log = []
prompt_count = 0
ended = False
deadline = time.time() + TIMEOUT
while time.time() < deadline and prompt_count < MAX_PROMPTS:
    try:
        line = lines.get(timeout=0.5)
    except queue.Empty:
        if process.poll() is not None and lines.empty():
            break
        continue
    if not line.strip():
        continue
    try:
        event = json.loads(line)
    except json.JSONDecodeError:
        continue
    events.append(event)
    if event.get('type') == 'prompt':
        prompt_count += 1
        response = choose_response(event)
        prompt_log.append({
            'function': event.get('function'),
            'summary': event.get('summary'),
            'responseKind': event.get('responseKind'),
            'response': response,
        })
        process.stdin.write(json.dumps(response) + '\n')
        process.stdin.flush()
    if event.get('type') == 'ended':
        ended = True
        break

try:
    process.stdin.write(json.dumps({'cmd': 'stop'}) + '\n')
    process.stdin.flush()
except Exception:
    pass
try:
    process.wait(timeout=5)
except subprocess.TimeoutExpired:
    process.kill()
    process.wait()

summary = {
    'seed': SEED,
    'prompt_count': prompt_count,
    'ended': ended,
    'event_count': len(events),
    'error_events': [e for e in events if e.get('type') == 'error'],
    'wins': [
        e for e in events
        if e.get('type') == 'message' and e.get('function') == 5
    ],
    'prompts': prompt_log[:60],
    'last_events': events[-20:],
}
out = Path(r'D:\MDProMod\docs\manual-policy-probe-2026-10-12.json')
out.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(summary, ensure_ascii=False, indent=2))
