"""Create a separate 1/8-resolution cutin pack; retain logical atlas coordinates."""
import json
import shutil
from pathlib import Path
import sys

from extract_cutin import PROJECT_ROOT, OUT_ROOT
sys.path.insert(0, str(PROJECT_ROOT / "tools" / "unitypy"))
from PIL import Image

SOURCE = Path(OUT_ROOT)
DEST = PROJECT_ROOT / "runtime" / "cutin_low"


def compress_one(folder):
    report = []
    manifest_path = folder / "cutin.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    target = DEST / folder.name
    target.mkdir(parents=True, exist_ok=True)
    names = {"cutin.txt", "cutin.json"}
    pages = {}
    for rig in manifest["rigs"]:
        names.add(rig["skeleton"])
        for atlas in rig["atlases"]:
            names.add(atlas["atlas"])
            text = (folder / atlas["atlas"]).read_text(encoding="utf-8")
            pma = "pma:true" in text or "pma: true" in text
            for page in atlas["pages"]:
                if page.get("match") not in ("exact", "unity_import_scaled"):
                    raise ValueError(f"Unverified source page: {folder.name}/{page['file']}")
                pages[page["file"]] = (pma, (page["width"], page["height"]),
                                       tuple(page.get("actual", (page["width"], page["height"]))),
                                       page.get("match"), page.get("scaling"))
    for name in names:
        shutil.copy2(folder / name, target / name)
    for name, (pma, logical_size, physical_size, match, scaling) in pages.items():
        src, dst = folder / name, target / name
        with Image.open(src) as original:
            if original.size != physical_size:
                raise ValueError(f"Source atlas/PNG mismatch: {folder.name}/{name}")
            rgba = original.convert("RGBA")
            size = (max(1, rgba.width // 8), max(1, rgba.height // 8))
            if pma:
                # Filter stored premultiplied channels directly; Pillow RGBA
                # resize would premultiply a second time.
                small = Image.merge("RGBA", tuple(
                    channel.resize(size, Image.Resampling.LANCZOS)
                    for channel in rgba.split()))
            else:
                small = rgba.resize(size, Image.Resampling.LANCZOS)
            small.save(dst, optimize=True, compress_level=9)
            with Image.open(dst) as check:
                assert check.size == size
        report.append({"code": folder.name, "page": name,
                       "logical_size": logical_size, "size": size,
                       "source_size": physical_size, "match": match, "scaling": scaling,
                       "before": src.stat().st_size, "after": dst.stat().st_size})
    return report


def main():
    report = []
    for folder in sorted(SOURCE.iterdir()):
        if (folder / "cutin.json").is_file():
            report.extend(compress_one(folder))
    DEST.mkdir(parents=True, exist_ok=True)
    (DEST / "compression_report.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"pages": len(report), "before": sum(r["before"] for r in report),
                      "after": sum(r["after"] for r in report)}, indent=2))


if __name__ == "__main__":
    main()


