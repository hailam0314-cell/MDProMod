"""Reproduce the dated, read-only card-coverage comparison from cached metadata."""
from pathlib import Path
import hashlib
import json
import sqlite3
import zipfile

ROOT = Path(r"D:\MDProMod")
UPSTREAM = ROOT / "MDPro3-main/MDPro3-main"
DATE = "2026-10-11"
with zipfile.ZipFile(ROOT / "downloads/ygocdb-current-cards.zip") as archive:
    cards = json.loads(archive.read("cards.json"))
releases = json.loads((ROOT / "downloads/ygocdb-releaseDates.json").read_text(encoding="utf-8"))
dates = {row["cid"]: row["release"].get("jp") for row in releases}
database = UPSTREAM / "Data/locales/zh-CN/cards.cdb"
with sqlite3.connect(database.as_uri() + "?mode=ro", uri=True) as connection:
    local_ids = {row[0] for row in connection.execute("SELECT id FROM datas")}
with zipfile.ZipFile(UPSTREAM / "Data/script.zip") as archive:
    scripts = set(archive.namelist())
recent = [card for card in cards.values() if dates.get(card["cid"])
          and "2025-01-01" <= dates[card["cid"]]["date"] <= DATE]
selected_cids = {7714, 11207, 21953, 23161, 23162, 23541, 23543, 23548, 23549}
selected = []
for card in cards.values():
    if card["cid"] not in selected_cids:
        continue
    selected.append({
        "cid": card["cid"], "id": card["id"], "cn_name": card["cn_name"],
        "sc_name": card.get("sc_name"), "jp_name": card.get("jp_name"),
        "description": card["text"]["desc"], "jp_release": dates.get(card["cid"]),
        "local_database_present": card["id"] in local_ids,
        "local_script_present": f"script/c{card['id']}.lua" in scripts,
    })
inputs = [ROOT / "downloads/ygocdb-current-cards.zip",
          ROOT / "downloads/ygocdb-releaseDates.json", database, UPSTREAM / "Data/script.zip"]
report = {
    "date": DATE, "community_current_rows": len(cards),
    "scope": "Community first Japanese release 2025-01-01 through 2026-10-11; exact passwords in local base zh-CN DB; not unique-card or worldwide completeness.",
    "jp_2025_2026_rows": len(recent),
    "missing_in_local_db": sum(card["id"] not in local_ids for card in recent),
    "selected": selected,
    "sha256": {str(path): hashlib.sha256(path.read_bytes()).hexdigest() for path in inputs},
}
output = ROOT / "docs/latest-card-compatibility.json"
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({key: report[key] for key in ("date", "community_current_rows", "jp_2025_2026_rows", "missing_in_local_db")}, ensure_ascii=False))
