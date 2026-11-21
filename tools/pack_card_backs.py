"""Pack the installed MDPro3 Protector materials at the runtime card-face resolution."""
import argparse
import re
from pathlib import Path

from PIL import Image
import UnityPy


def item_names(installation, locale):
    path = installation / "Data/locales" / locale / "IDS/IDS_ITEM.txt"
    return dict(re.findall(r"\[IDS_ITEM\.ID(107\d+)\]\s*\n([^\n]+)", path.read_text("utf-8")))


def pack(installation, assets):
    source = installation / "MDPro3_Data/StandaloneWindows64/MasterDuel/Protector"
    folder = assets / "assets/mdpromod/textures/card_back"
    folder.mkdir(parents=True, exist_ok=True)
    english = item_names(installation, "en-US")
    chinese = item_names(installation, "zh-CN")
    rows = []
    for directory in sorted(source.iterdir()):
        code = directory.name
        if not directory.is_dir() or not code.isdecimal():
            continue
        env = UnityPy.load(*(str(p) for p in directory.iterdir() if p.is_file()))
        material = next(o.read() for o in env.objects if o.type.name == "Material" and o.peek_name() == "PMat")
        surface = dict(material.m_SavedProperties.m_TexEnvs)["_MainTex"]
        # These are UV-mapped textures. The aspect of the displayed card is set by
        # its mesh even for square or power-of-two source textures. Sample the full UV rectangle.
        art = surface.m_Texture.read().image.convert("RGB").resize((176, 256), Image.Resampling.LANCZOS)
        art.save(folder / (code + ".jpg"), quality=80, optimize=True, progressive=True, subsampling=2)
        rows.append("\t".join([code, chinese.get(code, ""), english.get(code, "")]))
    if not rows:
        raise ValueError("No installed Protector materials")
    (folder / "catalog.tsv").write_text("\n".join(rows) + "\n", encoding="utf-8")
    print(f"{len(rows)} card backs")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("installation", type=Path)
    parser.add_argument("assets", type=Path)
    args = parser.parse_args()
    pack(args.installation, args.assets)
