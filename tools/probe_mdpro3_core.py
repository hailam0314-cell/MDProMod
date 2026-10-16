"""Small isolated DLL probe; never opens Minecraft or changes upstream files."""
import ctypes as C
import json
from pathlib import Path
import sqlite3
import zipfile

ROOT = Path(r"D:\MDProMod\MDPro3-main\MDPro3-main")
core = C.CDLL(str(ROOT / "Assets/Plugins/YGO Classes/ocgcore.dll"))

class CardData(C.Structure):
    _fields_ = [("code", C.c_uint32), ("alias", C.c_uint32), ("setcode", C.c_uint16 * 16)] + [(n, C.c_uint32) for n in ("type", "level", "attribute", "race")] + [("attack", C.c_int32), ("defense", C.c_int32)] + [(n, C.c_uint32) for n in ("lscale", "rscale", "link_marker")]

with sqlite3.connect((ROOT / "Data/locales/zh-CN/cards.cdb").as_uri() + "?mode=ro", uri=True) as con:
    rows = {r[0]: r for r in con.execute("SELECT id,alias,setcode,type,level,attribute,race,atk,def FROM datas")}
with zipfile.ZipFile(ROOT / "Data/script.zip") as archive:
    scripts = {n: C.create_string_buffer(archive.read(n)) for n in archive.namelist() if n.endswith(".lua")}

logs = []
read_paths = []
READ_SCRIPT = C.CFUNCTYPE(C.c_void_p, C.c_char_p, C.POINTER(C.c_int32))
READ_CARD = C.CFUNCTYPE(C.c_uint32, C.c_uint32, C.POINTER(CardData))
LOG = C.CFUNCTYPE(C.c_uint32, C.c_void_p, C.c_uint32)

@READ_SCRIPT
def read_script(name, length):
    key = name.decode("utf-8").replace("\\", "/").removeprefix("./")
    read_paths.append(key)
    buf = scripts.get(key)
    if buf is None:
        length[0] = 0
        return None
    length[0] = C.sizeof(buf) - 1
    return C.addressof(buf)

@READ_CARD
def read_card(code, data):
    C.memset(data, 0, C.sizeof(CardData))
    r = rows.get(code)
    if r is None:
        return 0
    v = data.contents
    v.code, v.alias, packed, v.type, level, v.attribute, v.race, v.attack, defense = r
    packed &= (1 << 64) - 1
    chunks = [(packed >> (16 * i)) & 0xffff for i in range(4)]
    for i, chunk in enumerate(x for x in chunks if x):
        v.setcode[i] = chunk
    v.level, v.lscale, v.rscale = level & 255, (level >> 24) & 255, (level >> 16) & 255
    v.defense, v.link_marker = (0, defense) if v.type & 0x4000000 else (defense, 0)
    return code

core.get_log_message.argtypes = [C.c_void_p, C.c_void_p]
@LOG
def log(duel, kind):
    buf = C.create_string_buffer(256)
    core.get_log_message(duel, buf)
    logs.append({"type": kind, "message": buf.value.decode("utf-8", errors="replace")})
    return 0

for name, callback, typ in (("set_script_reader", read_script, READ_SCRIPT), ("set_card_reader", read_card, READ_CARD), ("set_message_handler", log, LOG)):
    fn = getattr(core, name)
    fn.argtypes, fn.restype = [typ], None
    fn(callback)
core.create_duel.argtypes, core.create_duel.restype = [C.c_uint32], C.c_void_p
core.end_duel.argtypes, core.end_duel.restype = [C.c_void_p], None
core.preload_script.argtypes, core.preload_script.restype = [C.c_void_p, C.c_void_p, C.c_int32], C.c_int32
core.new_card.argtypes, core.new_card.restype = [C.c_void_p, C.c_uint32] + [C.c_uint8] * 5, None

duel = core.create_duel(20261011)
try:
    helper_load_logs = list(logs)
    logs.clear()
    # Error is intentional: reports whether this bundled DLL exposes the suspect API.
    check = b'assert(type(Duel.RevealSelectDeckSequence)=="function", "probe: RevealSelectDeckSequence missing")'
    # This snapshot treats preload_script's second argument as a reader path;
    # its length argument is unused. Feed content through the registered reader.
    scripts["script/mdpromod_probe.lua"] = C.create_string_buffer(check)
    result = core.preload_script(duel, b"./script/mdpromod_probe.lua", len(check))
    api_probe_logs = list(logs)
    logs.clear()
    helper_check = b'assert(type(aux)=="table" and type(aux.AddSynchroProcedure)=="function" and type(CATEGORY_DRAW)=="number")'
    scripts["script/mdpromod_helpers_probe.lua"] = C.create_string_buffer(helper_check)
    helper_check_result = core.preload_script(duel, b"./script/mdpromod_helpers_probe.lua", len(helper_check))
    helper_check_logs = list(logs)
    logs.clear()
    sample_cards = [55144522, 83764718, 14558127, 70368879, 44095762, 12580477, 44508094, 84013237, 23995346, 33396948]
    for card_id in sample_cards:
        core.new_card(duel, card_id, 0, 0, 0x01, 0, 0x08)
    report = {"scope": "Load helpers and initialize ten representative card scripts; not a full duel or ruling test.", "card_data_size": C.sizeof(CardData), "helper_load_logs": helper_load_logs, "helper_check_result": helper_check_result, "helper_check_logs": helper_check_logs, "api_probe_result": result, "api_probe_logs": api_probe_logs, "sample_card_ids": sample_cards, "sample_initialization_logs": list(logs), "script_read_paths": read_paths}
    Path(r"D:\MDProMod\docs\mdpro3-native-probe.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
finally:
    core.end_duel(duel)
