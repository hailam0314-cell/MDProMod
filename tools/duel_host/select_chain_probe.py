"""用真实中文卡组跑一局手动对局，核对 SelectChain(16) 解析出来的候选卡号。

判定标准（每条候选都必须同时满足）：
1. code 能在 cards.json 里查到；
2. location 是合法区域（1 卡组 / 2 手牌 / 4 怪兽区 / 8 魔陷区 / 16 墓地 / 32 除外 / 64 额外）；
3. count 与解析出的候选条数一致（帧没有被截断）。
只读，不修改任何对局数据。
"""
import json
import queue
import subprocess
import threading
import time
from pathlib import Path

STAGING = Path(r'D:\MDProMod\tools\ygo_tcp_probe\staging')
DOTNET = r'D:\MDProMod\tools\dotnet\dotnet.exe'
HOST = r'D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll'
DECK0 = r'D:\MDProMod\runtime\decks\珠泪.ydk'
AI_DECK_FILE = 'Data/Windbot/Decks/AI_BlueEyes.ydk'
CARDS_JSON = Path(r'D:\MDProMod\runtime\cards.json')
OUT = Path(r'D:\MDProMod\docs\select-chain-probe-2026-10-14.json')
SEED = 4962
TIMEOUT = 180
VALID_LOCATIONS = {1, 2, 4, 8, 16, 32, 64}


def load_known_codes():
    data = json.loads(CARDS_JSON.read_text(encoding='utf-8'))
    return {int(k) for k in data}


def bytes_for_cards(prompt):
    count = int(prompt.get('min', 0))
    if count <= 0:
        return '00'
    return bytes([count] + list(range(count))).hex().upper()


def bytes_for_places(prompt):
    places = prompt.get('places') or []
    count = max(1, int(prompt.get('count', 1)))
    out = bytearray()
    for index in range(count):
        place = places[index % len(places)] if places else {
            'player': 0, 'location': 4, 'sequence': 0}
        out.extend([int(place.get('player', 0)),
                    int(place.get('location', 4)),
                    int(place.get('sequence', 0))])
    return out.hex().upper()


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
        return {'cmd': 'response', 'value': int(options[0].get('index', 1)) if options else 1}
    if function == 20:
        if prompt.get('cancelable') and int(prompt.get('min', 0)) == 0:
            return {'cmd': 'response', 'value': -1}
        return {'cmd': 'responseBytes', 'hex': bytes_for_cards(prompt)}
    if function == 25:
        count = int(prompt.get('count', 0))
        return {'cmd': 'responseBytes', 'hex': bytes([count] + list(range(count))).hex().upper()}
    if function == 26:
        if prompt.get('cancelable') and int(prompt.get('min', 0)) == 0:
            return {'cmd': 'response', 'value': -1}
        return {'cmd': 'responseBytes', 'hex': bytes_for_cards(prompt)}
    return {'cmd': 'response', 'value': 0}


def main():
    known = load_known_codes()
    process = subprocess.Popen(
        [DOTNET, HOST, '--root', str(STAGING)],
        cwd=str(STAGING), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
        stderr=subprocess.PIPE, text=True, encoding='utf-8', errors='replace')
    lines = queue.Queue()

    def pump():
        for line in process.stdout:
            lines.put(line.rstrip())

    threading.Thread(target=pump, daemon=True).start()
    process.stdin.write(json.dumps({
        'cmd': 'start', 'auto': False, 'seed': SEED,
        'deck0': DECK0, 'deck1': 'starter.ydk',
        'ai': True, 'aiSeat': 1, 'aiName': 'Blue-Eyes',
        'aiDeck': 'AI_BlueEyes', 'aiDeckFile': AI_DECK_FILE,
    }) + '\n')
    process.stdin.flush()

    chains = []
    errors = []
    messages = 0
    ended = False
    prompt_sample = None
    chain_sample = None
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
        kind = event.get('type')
        if kind == 'error':
            errors.append(event.get('message'))
        if kind == 'message':
            messages += 1
        if kind == 'prompt':
            if prompt_sample is None:
                prompt_sample = {
                    'function': event.get('function'),
                    'name': event.get('name'),
                    'seq': event.get('seq'),
                    'summary': event.get('summary'),
                    'keys': sorted(event.keys()),
                }
            if int(event.get('function', 0)) == 16:
                cards = event.get('cards') or []
                if chain_sample is None:
                    chain_sample = {
                        'seq': event.get('seq'),
                        'count': event.get('count'),
                        'cards': cards[:3],
                    }
                verdict = []
                for card in cards:
                    code = int(card.get('code', 0))
                    location = int(card.get('location', 0))
                    verdict.append({
                        'code': code,
                        'known': code in known,
                        'location': location,
                        'location_ok': location in VALID_LOCATIONS,
                        'sequence': int(card.get('sequence', 0)),
                    })
                chains.append({
                    'raw': event.get('raw'),
                    'count': int(event.get('count', 0)),
                    'parsed': len(cards),
                    'forced': bool(event.get('forced')),
                    'cards': verdict,
                    'all_ok': bool(cards)
                    and len(cards) == int(event.get('count', 0))
                    and all(v['known'] and v['location_ok'] for v in verdict),
                })
            process.stdin.write(json.dumps(choose_response(event)) + '\n')
            process.stdin.flush()
        if kind == 'ended':
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
        'deck0': DECK0,
        'ai_deck': AI_DECK_FILE,
        'messages': messages,
        'chain_prompts': len(chains),
        'chain_candidates': sum(len(c['cards']) for c in chains),
        'chains_all_parsed_ok': all(c['all_ok'] for c in chains) if chains else None,
        'bad_chains': [c for c in chains if not c['all_ok']][:5],
        'errors': errors[:10],
        'ended': ended,
        'first_prompt_sample': prompt_sample,
        'first_chain_sample': chain_sample,
        'sample_chains': chains[:5],
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({k: v for k, v in summary.items() if k != 'bad_chains'},
                     ensure_ascii=False, indent=2))
    print('bad_chains=' + json.dumps(summary['bad_chains'], ensure_ascii=False))


if __name__ == '__main__':
    main()
