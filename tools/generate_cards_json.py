"""Generate a compact card registry JSON from the installed cards.cdb."""
import json
import sqlite3
from pathlib import Path

DB = Path(r'D:\MyCardLibrary\mdpro3\Data\locales\zh-CN\cards.cdb')
MOD_OUT = Path(r'D:\MDProMod\mod\src\main\resources\data\mdpromod\cards.json')
RUNTIME_OUT = Path(r'D:\MDProMod\runtime\cards.json')

connection = sqlite3.connect(str(DB))
texts = {}
for row in connection.execute('select id, name, desc from texts'):
    code = int(row[0])
    texts[code] = {
        'name': row[1] or f'#{code}',
        'desc': (row[2] or '').replace('\r\n', '\n').replace('\r', '\n').strip(),
    }
cards = {}
for row in connection.execute(
        'select id, ot, alias, setcode, type, atk, def, level, race, attribute from datas'):
    code = int(row[0])
    text = texts.get(code, {})
    cards[str(code)] = {
        'name': text.get('name', f'#{code}'),
        'desc': text.get('desc', ''),
        'ot': int(row[1]),
        'alias': int(row[2]),
        'setcode': int(row[3]),
        'type': int(row[4]),
        'atk': int(row[5]),
        'def': int(row[6]),
        'level': int(row[7]),
        'race': int(row[8]),
        'attribute': int(row[9]),
    }
connection.close()

MOD_OUT.parent.mkdir(parents=True, exist_ok=True)
RUNTIME_OUT.parent.mkdir(parents=True, exist_ok=True)
payload = json.dumps(cards, ensure_ascii=False, separators=(',', ':'))
MOD_OUT.write_text(payload, encoding='utf-8')
RUNTIME_OUT.write_text(payload, encoding='utf-8')
print(json.dumps({
    'cards': len(cards),
    'mod_json': str(MOD_OUT),
    'mod_bytes': MOD_OUT.stat().st_size,
    'runtime_json': str(RUNTIME_OUT),
    'sample_89631139': cards.get('89631139'),
}, ensure_ascii=False))
