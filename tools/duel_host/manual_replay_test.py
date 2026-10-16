"""Replay the auto-mode response sequence through the manual prompt protocol."""
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
TIMEOUT = 90


def run_host(auto, responses=None):
    process = subprocess.Popen(
        [DOTNET, HOST, '--root', str(STAGING)],
        cwd=str(STAGING), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
        stderr=subprocess.PIPE, text=True, encoding='utf-8', errors='replace')
    lines = queue.Queue()

    def read_stdout():
        for line in process.stdout:
            lines.put(line.rstrip())

    threading.Thread(target=read_stdout, daemon=True).start()
    start = {
        'cmd': 'start', 'auto': auto, 'seed': SEED,
        'deck0': 'starter.ydk', 'deck1': 'starter.ydk',
    }
    process.stdin.write(json.dumps(start) + '\n')
    process.stdin.flush()

    events = []
    response_index = 0
    ended = False
    deadline = time.time() + TIMEOUT
    while time.time() < deadline:
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
        if event.get('type') == 'prompt' and responses is not None:
            if response_index >= len(responses):
                raise RuntimeError(
                    f'prompt {response_index} has no recorded response')
            response = responses[response_index]
            response_index += 1
            if 'value' in response:
                command = {'cmd': 'response', 'value': response['value']}
            else:
                command = {'cmd': 'responseBytes', 'hex': response['hex']}
            process.stdin.write(json.dumps(command) + '\n')
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
    return {
        'events': events,
        'ended': ended,
        'responses_used': response_index,
        'returncode': process.returncode,
    }


def summarize(result):
    events = result['events']
    return {
        'events': len(events),
        'messages': sum(1 for e in events if e.get('type') == 'message'),
        'prompts': sum(1 for e in events if e.get('type') == 'prompt'),
        'responses': sum(1 for e in events if e.get('type') == 'response'),
        'response_bytes': sum(1 for e in events if e.get('type') == 'responseBytes'),
        'errors': [e for e in events if e.get('type') == 'error'],
        'ended': result['ended'],
        'responses_used': result['responses_used'],
        'wins': [
            e for e in events
            if e.get('type') == 'message' and e.get('function') == 5
        ],
    }


auto = run_host(True)
recorded = []
for event in auto['events']:
    if event.get('type') == 'response':
        recorded.append({'value': event.get('value')})
    elif event.get('type') == 'responseBytes':
        recorded.append({'hex': event.get('hex')})

manual = run_host(False, recorded)
summary = {
    'seed': SEED,
    'auto': summarize(auto),
    'recorded_responses': len(recorded),
    'manual': summarize(manual),
}
out = Path(r'D:\MDProMod\docs\manual-replay-test-2026-10-12.json')
out.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(summary, ensure_ascii=False, indent=2))
