"""Read-only, reproducible inventory of the downloaded MDPro3 rules snapshot."""
from pathlib import Path
import hashlib
import json
import re
import sqlite3
import zipfile

ROOT = Path(r"D:\MDProMod\MDPro3-main\MDPro3-main")
OUT = Path(r"D:\MDProMod\docs")
CORE = ROOT / "Tools/YGO Classes/ocgcore"

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

with zipfile.ZipFile(ROOT / "Data/script.zip") as archive:
    scripts = {n: archive.read(n).decode("utf-8-sig") for n in archive.namelist() if n.endswith(".lua")}
    card_ids = {int(m.group(1)) for n in scripts if (m := re.fullmatch(r"script/c(\d+)\.lua", n))}
    with sqlite3.connect((ROOT / "Data/locales/zh-CN/cards.cdb").as_uri() + "?mode=ro", uri=True) as con:
        rows = con.execute("SELECT d.id,d.alias,d.type,t.name FROM datas d LEFT JOIN texts t ON d.id=t.id ORDER BY d.id").fetchall()
        schema = [dict(zip(("cid", "name", "type", "notnull", "default", "pk"), row)) for row in con.execute("PRAGMA table_info(datas)")]
    # Match interpreter::register_card and card_data::is_alternative.
    needs = [row for row in rows if not (row[2] & 0x10) or row[2] & 0x1000000]
    missing = []
    for code, alias, card_type, name in needs:
        script_id = alias if alias and code != 5405695 and abs(code - alias) < 20 else code
        if script_id not in card_ids:
            missing.append({"id": code, "alias": alias, "type": card_type, "name": name, "expected_script_id": script_id})
    tables = {}
    static_api_candidates = {}
    for ns, filename in (("Duel", "libduel.cpp"), ("Card", "libcard.cpp"), ("Effect", "libeffect.cpp"), ("Group", "libgroup.cpp"), ("Debug", "libdebug.cpp")):
        source = (CORE / filename).read_text(encoding="utf-8-sig")
        names = sorted(set(re.findall(r'\{\s*"(\w+)"\s*,\s*scriptlib::', source)))
        tables[ns] = names
        used = {}
        for path, script in scripts.items():
            # Lexical hints only: comments, Lua additions, and dynamic aliases are not resolved.
            for method in set(re.findall(r"\b" + ns + r"\.(\w+)\s*\(", script)):
                if method not in names:
                    used.setdefault(method, []).append(path)
        static_api_candidates[ns] = {name: {"occurrences_in_files": len(paths), "examples": paths[:4]} for name, paths in sorted(used.items())}

functions = {}
for filename in ("processor.cpp", "operations.cpp", "field.cpp", "card.cpp", "effect.cpp", "interpreter.cpp", "libeffect.cpp"):
    found = []
    for number, line in enumerate((CORE / filename).read_text(encoding="utf-8-sig").splitlines(), 1):
        if match := re.match(r"(?:int32|void|uint32|bool)\s+(\w+::\w+)\s*\(", line):
            found.append({"symbol": match.group(1), "line": number})
    functions[filename] = found

inputs = [ROOT / "Version.txt", ROOT / "LICENSE", ROOT / "Data/script.zip", ROOT / "Data/locales/zh-CN/cards.cdb", ROOT / "Data/lflist.conf", ROOT / "Assets/Plugins/YGO Classes/ocgcore.dll"] + sorted(CORE.glob("*.cpp")) + sorted(CORE.glob("*.h"))
report = {
    "root": str(ROOT), "version_label": (ROOT / "Version.txt").read_text().strip(),
    "default_rule": 5, "rule_name": "Master Rule 2020", "bundled_lua": "5.4.6",
    "database_rows": len(rows), "lua_files": len(scripts), "card_script_files": len(card_ids),
    "effect_or_pendulum_rows_needing_initial_effect": len(needs),
    "rows_without_expected_script": missing,
    "coverage_scope": "Base zh-CN database and base script.zip only; aliases use this snapshot's interpreter rule; script presence does not prove correct effects.",
    "database_schema": schema,
    "core_license_file_present": (CORE / "LICENSE").exists(),
    "native_core_binaries": [str(p.relative_to(ROOT)) for p in ROOT.rglob("*") if p.is_file() and p.name.lower() in ("ocgcore.dll", "libocgcore.so", "libocgcore.dylib")],
    "registered_lua_apis": tables, "static_api_candidates": static_api_candidates,
    "static_api_scope": "Lexical scan, not Lua execution or complete validation. Methods absent from C++ may be defined in Lua.",
    "function_index": functions,
    "sha256": {str(p.relative_to(ROOT)): digest(p) for p in inputs},
}
OUT.mkdir(parents=True, exist_ok=True)
(OUT / "mdpro3-source-audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: report[k] for k in ("version_label", "database_rows", "lua_files", "card_script_files", "effect_or_pendulum_rows_needing_initial_effect", "core_license_file_present", "native_core_binaries")}, ensure_ascii=False))
print("Rows missing expected script:", len(missing))
print(json.dumps(missing[:20], ensure_ascii=False))
print("Static API candidates:", json.dumps(static_api_candidates, ensure_ascii=False))
