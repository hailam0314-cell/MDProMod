"""Direct native ocgcore smoke test for the MDPro3 Minecraft bridge.

This intentionally bypasses Unity and ygoserver. It creates one duel from two
YDK files, feeds the bundled card database/scripts through native callbacks,
and records the GameMessage stream until the core waits for a response.
"""
from __future__ import annotations

import argparse
import ctypes as C
import json
import sqlite3
import struct
import time
import zipfile
from pathlib import Path


CARD_DATA = type(
    "CardData",
    (C.Structure,),
    {
        "_fields_": [
            ("code", C.c_uint32),
            ("alias", C.c_uint32),
            ("setcode", C.c_uint16 * 16),
            ("type", C.c_uint32),
            ("level", C.c_uint32),
            ("attribute", C.c_uint32),
            ("race", C.c_uint32),
            ("attack", C.c_int32),
            ("defense", C.c_int32),
            ("lscale", C.c_uint32),
            ("rscale", C.c_uint32),
            ("link_marker", C.c_uint32),
        ]
    },
)


READ_SCRIPT = C.CFUNCTYPE(C.c_void_p, C.c_char_p, C.POINTER(C.c_int32))
READ_CARD = C.CFUNCTYPE(C.c_uint32, C.c_uint32, C.POINTER(CARD_DATA))
LOG = C.CFUNCTYPE(C.c_uint32, C.c_void_p, C.c_uint32)


GAME_MESSAGES = {
    4: "Start",
    5: "Win",
    6: "UpdateData",
    7: "UpdateCard",
    10: "SelectBattleCmd",
    11: "SelectIdleCmd",
    12: "SelectEffectYn",
    13: "SelectYesNo",
    14: "SelectOption",
    15: "SelectCard",
    16: "SelectChain",
    18: "SelectPlace",
    19: "SelectPosition",
    20: "SelectTribute",
    21: "SortChain",
    22: "SelectCounter",
    23: "SelectSum",
    24: "SelectDisfield",
    25: "SortCard",
    26: "SelectUnselect",
    30: "ConfirmDecktop",
    31: "ConfirmCards",
    32: "ShuffleDeck",
    33: "ShuffleHand",
    34: "RefreshDeck",
    35: "SwapGraveDeck",
    36: "ShuffleSetCard",
    37: "ReverseDeck",
    38: "DeckTop",
    39: "ShuffleExtra",
    40: "NewTurn",
    41: "NewPhase",
    50: "Move",
    53: "PosChange",
    54: "Set",
    55: "Swap",
    60: "Summoning",
    61: "Summoned",
    62: "SpSummoning",
    63: "SpSummoned",
    64: "FlipSummoning",
    65: "FlipSummoned",
    70: "Chaining",
    71: "Chained",
    72: "ChainSolving",
    73: "ChainSolved",
    74: "ChainEnd",
    90: "Draw",
    91: "Damage",
    94: "LpUpdate",
    110: "Attack",
    111: "Battle",
    130: "TossCoin",
    131: "TossDice",
    132: "RockPaperScissors",
    140: "AnnounceRace",
    141: "AnnounceAttrib",
    142: "AnnounceCard",
    143: "AnnounceNumber",
    162: "ReloadField",
    200: "DuelWinner",
}


def parse_deck(path: Path) -> tuple[list[int], list[int], list[int]]:
    section = None
    main: list[int] = []
    extra: list[int] = []
    side: list[int] = []
    for raw in path.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw.strip()
        if line == "#main":
            section = main
        elif line == "#extra":
            section = extra
        elif line == "!side":
            section = side
        elif section is not None and line.isdigit():
            section.append(int(line))
    return main, extra, side


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=r"D:\MDProMod\tools\ygo_tcp_probe\staging")
    ap.add_argument("--deck-a", default="starter.ydk")
    ap.add_argument("--deck-b", default="starter.ydk")
    ap.add_argument("--seed", type=int, default=20261012)
    ap.add_argument("--max-messages", type=int, default=80)
    ap.add_argument("--auto-response", action="store_true")
    ap.add_argument("--output", default=r"D:\MDProMod\docs\headless-duel-probe-2026-10-12.json")
    args = ap.parse_args()

    root = Path(args.root)
    deck_a = Path(args.deck_a)
    if not deck_a.is_absolute():
        deck_a = root / deck_a
    deck_b = Path(args.deck_b)
    if not deck_b.is_absolute():
        deck_b = root / deck_b

    cards_path = root / "Data" / "locales" / "zh-CN" / "cards.cdb"
    if not cards_path.exists():
        cards_path = root / "cards.cdb"
    scripts_zip = root / "Data" / "script.zip"
    if not scripts_zip.exists():
        scripts_zip = root / "script.zip"
    core_path = root / "ocgcore.dll"
    if not core_path.exists():
        core_path = root / "MDPro3_Data" / "Plugins" / "x86_64" / "ocgcore.dll"

    with sqlite3.connect(cards_path.as_uri() + "?mode=ro", uri=True) as db:
        rows = {r[0]: r for r in db.execute("SELECT id,alias,setcode,type,level,attribute,race,atk,def FROM datas")}
    with zipfile.ZipFile(scripts_zip) as archive:
        scripts = {name: C.create_string_buffer(archive.read(name)) for name in archive.namelist() if name.endswith(".lua")}

    log_lines: list[dict] = []
    read_paths: list[str] = []

    @READ_SCRIPT
    def read_script(name, length):
        key = name.decode("utf-8", errors="replace").replace("\\", "/").removeprefix("./")
        read_paths.append(key)
        buf = scripts.get(key)
        if buf is None:
            length[0] = 0
            return None
        length[0] = C.sizeof(buf) - 1
        return C.addressof(buf)

    @READ_CARD
    def read_card(code, data):
        C.memset(data, 0, C.sizeof(CARD_DATA))
        row = rows.get(code)
        if row is None:
            return 0
        value = data.contents
        value.code, value.alias, packed, value.type, level, value.attribute, value.race, value.attack, defense = row
        for i in range(4):
            value.setcode[i] = (packed >> (i * 16)) & 0xFFFF
        value.level = level & 0xFF
        value.lscale = (level >> 24) & 0xFF
        value.rscale = (level >> 16) & 0xFF
        if value.type & 0x4000000:
            value.link_marker, value.defense = defense, 0
        else:
            value.defense, value.link_marker = defense, 0
        return code

    core = C.CDLL(str(core_path))
    core.set_script_reader.argtypes = [READ_SCRIPT]
    core.set_card_reader.argtypes = [READ_CARD]
    core.set_script_reader(read_script)
    core.set_card_reader(read_card)

    @LOG
    def log(duel, kind):
        buffer = C.create_string_buffer(256)
        core.get_log_message(duel, buffer)
        log_lines.append({"kind": int(kind), "message": buffer.value.decode("utf-8", errors="replace")})
        return 0

    core.set_message_handler.argtypes = [LOG]
    core.set_message_handler(log)
    core.create_duel.argtypes = [C.c_uint32]
    core.create_duel.restype = C.c_void_p
    core.end_duel.argtypes = [C.c_void_p]
    core.set_player_info.argtypes = [C.c_void_p, C.c_int32, C.c_int32, C.c_int32, C.c_int32]
    core.new_card.argtypes = [C.c_void_p, C.c_uint32, C.c_uint8, C.c_uint8, C.c_uint8, C.c_uint8, C.c_uint8]
    core.start_duel.argtypes = [C.c_void_p, C.c_int32]
    core.process.argtypes = [C.c_void_p]
    core.process.restype = C.c_uint32
    core.get_message.argtypes = [C.c_void_p, C.c_void_p]
    core.set_responsei.argtypes = [C.c_void_p, C.c_uint32]

    duel = core.create_duel(args.seed)
    if not duel:
        raise RuntimeError("create_duel returned null")
    messages: list[dict] = []
    waiting = None
    try:
        core.set_player_info(duel, 0, 8000, 5, 1)
        core.set_player_info(duel, 1, 8000, 5, 1)
        main_a, extra_a, _ = parse_deck(deck_a)
        main_b, extra_b, _ = parse_deck(deck_b)
        for owner, main, extra in ((0, main_a, extra_a), (1, main_b, extra_b)):
            for code in reversed(main):
                core.new_card(duel, code, owner, owner, 0x01, 0, 0x08)
            for code in extra:
                core.new_card(duel, code, owner, owner, 0x40, 0, 0x08)
        core.start_duel(duel, 0)
        buffer = C.create_string_buffer(65536)
        for _ in range(args.max_messages):
            result = int(core.process(duel))
            length = result & 0x0FFFFFFF
            state = result & 0xF0000000
            if length:
                core.get_message(duel, buffer)
                payload = buffer.raw[:length]
                fn = payload[0]
                entry = {
                    "function": fn,
                    "name": GAME_MESSAGES.get(fn, f"Unknown_{fn}"),
                    "length": length,
                    "hex_prefix": payload[:64].hex(),
                    "process_result": result,
                }
                messages.append(entry)
                if fn in (10, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25, 26, 132, 140, 141, 142, 143):
                    waiting = entry
                    if args.auto_response:
                        core.set_responsei(duel, 0)
                        waiting = None
                        continue
                    break
                if fn in (5, 200):
                    break
                continue
            if state == 0x10000000:
                waiting = {"reason": "core_waiting", "process_result": result}
                break
            if state == 0x20000000:
                break
            time.sleep(0.001)
    finally:
        core.end_duel(duel)

    report = {
        "root": str(root),
        "core": str(core_path),
        "seed": args.seed,
        "deck_a": str(deck_a),
        "deck_b": str(deck_b),
        "card_count": len(rows),
        "messages": messages,
        "waiting": waiting,
        "native_logs": log_lines,
        "script_read_paths": read_paths,
    }
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
