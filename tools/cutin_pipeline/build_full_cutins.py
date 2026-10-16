"""Validate Unity references, extract and compress the complete Spine corpus."""
import contextlib
import io
import json
import os
import time
import traceback
from concurrent.futures import ProcessPoolExecutor, as_completed
from pathlib import Path

import extract_cutin
from compress_cutins import compress_one, DEST

REPORT = DEST.parent / "cutin_corpus_report.json"
ROSTER = DEST.parent / "cutin_validated_roster.json"


def run_one(code):
    started = time.monotonic()
    log = io.StringIO()
    try:
        if code == "27204311":
            from compress_nonspine_cutin import extract_and_compress
            return extract_and_compress(code)
        with contextlib.redirect_stdout(log):
            manifest = extract_cutin.extract(code)
            folder = Path(extract_cutin.OUT_ROOT) / code
            pages = compress_one(folder)
        names = {"cutin.txt", "cutin.json"}
        for rig in manifest["rigs"]:
            names.add(rig["skeleton"])
            names.update(atlas["atlas"] for atlas in rig["atlases"])
        return {"code": code, "ok": True, "kind": "spine", "seconds": round(time.monotonic() - started, 2),
                "rigs": [{"skeleton": r["skeleton"], "animation": r["animation"],
                          "duration": r["duration"], "atlases": len(r["atlases"])}
                         for r in manifest["rigs"]], "pages": pages,
                "non_png_bytes": sum((DEST / code / name).stat().st_size for name in names)}
    except Exception as error:
        return {"code": code, "ok": False, "error": f"{type(error).__name__}: {error}",
                "traceback": traceback.format_exc(), "log": log.getvalue()}


def save(results, total):
    ordered = sorted(results, key=lambda r: r["code"])
    valid = [r for r in ordered if r["ok"]]
    spine = [r for r in valid if r.get("kind", "spine") == "spine"]
    pages = [p for r in valid for p in r["pages"]]
    report = {"total_source_directories": total, "completed": len(ordered),
              "spine_successes": len(spine), "non_spine_texture_sets": len(valid) - len(spine),
              "failures": sum(not r["ok"] for r in ordered),
              "png_pages": len(pages), "source_png_bytes": sum(p["before"] for p in pages),
              "compressed_png_bytes": sum(p["after"] for p in pages),
              "non_png_bytes": sum(r["non_png_bytes"] for r in valid), "results": ordered}
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=1), encoding="utf-8")
    ROSTER.write_text(json.dumps([{ "code": r["code"], "duration": max(
        float(rig["duration"] or 0) for rig in r["rigs"]), "rigs": r["rigs"]}
        for r in spine], ensure_ascii=False, indent=1), encoding="utf-8")
    return report


def main():
    codes = sorted(p.name for p in Path(extract_cutin.ROOT).iterdir() if p.is_dir())
    results = []
    workers = min(4, max(1, (os.cpu_count() or 4) - 1))
    with ProcessPoolExecutor(max_workers=workers) as pool:
        futures = {pool.submit(run_one, code): code for code in codes}
        for future in as_completed(futures):
            result = future.result()
            results.append(result)
            status = "OK" if result["ok"] else result["error"]
            print(f"[{len(results)}/{len(codes)}] {result['code']} {status}", flush=True)
            if len(results) % 10 == 0 or not result["ok"]:
                save(results, len(codes))
    report = save(results, len(codes))
    print(json.dumps({k: v for k, v in report.items() if k != "results"}, indent=2), flush=True)


if __name__ == "__main__":
    main()


