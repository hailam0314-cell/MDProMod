"""Audit finished generated files and publish exact size/roster reports."""
import json
from pathlib import Path

from build_full_cutins import REPORT, save
from compress_cutins import DEST, SOURCE, Image
from compress_nonspine_cutin import extract_and_compress
from extract_cutin import atlas_pages, validate_regions


def main():
    previous = json.loads(REPORT.read_text(encoding="utf-8"))
    results = [result for result in previous["results"] if result["code"] != "27204311"]
    results.append(extract_and_compress())
    assert len(results) == 498 and all(r["ok"] for r in results)
    for result in results:
        folder = DEST / result["code"]
        source = SOURCE / result["code"]
        if result.get("kind", "spine") == "spine":
            manifest = json.loads((folder / "cutin.json").read_text(encoding="utf-8"))
            assert len(manifest["rigs"]) == 1
            names = {"cutin.json", "cutin.txt"}
            for rig in manifest["rigs"]:
                assert rig["start"] == rig["clip_in"] == 0 and rig["time_scale"] == 1
                names.add(rig["skeleton"])
                for atlas in rig["atlases"]:
                    names.add(atlas["atlas"])
                    atlas_text = (folder / atlas["atlas"]).read_text(encoding="utf-8")
                    assert validate_regions(atlas_text) == atlas["verified_regions"]
                    assert atlas_pages(atlas_text) == [(p["file"], p["width"], p["height"])
                                                       for p in atlas["pages"]]
            for name in names:
                assert (folder / name).read_bytes() == (source / name).read_bytes(), name
            result["non_png_bytes"] = sum((folder / name).stat().st_size for name in names)
        else:
            names = {"nonspine.json"}
        for page in result["pages"]:
            name = page["page"]
            with Image.open(source / name) as png:
                assert png.size == tuple(page["source_size"])
                png.verify()
            with Image.open(folder / name) as png:
                assert png.size == tuple(max(1, v // 8) for v in page["source_size"])
                assert png.size == tuple(page["size"])
                png.verify()
            assert page["before"] == (source / name).stat().st_size
            assert page["after"] == (folder / name).stat().st_size
            names.add(name)
        assert set(p.name for p in folder.iterdir() if p.is_file()) == names, folder
    final = save(results, 498)
    pages = [p for result in results for p in result["pages"]]
    (DEST / "compression_report.json").write_text(json.dumps(pages, indent=1), encoding="utf-8")
    scaled = [page for page in pages if page["match"] == "unity_import_scaled"]
    (DEST.parent / "cutin_import_scaled_pages.json").write_text(
        json.dumps(scaled, indent=1), encoding="utf-8")
    summary = {key: value for key, value in final.items() if key != "results"}
    summary.update({"asset_bytes": final["compressed_png_bytes"] + final["non_png_bytes"],
                    "scaled_pages": len(scaled), "scaled_codes": len({p["code"] for p in scaled})})
    (DEST.parent / "cutin_final_summary.json").write_text(json.dumps(summary, indent=1), encoding="utf-8")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()


