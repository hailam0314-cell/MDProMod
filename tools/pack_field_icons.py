"""Pack MDPro3 field selection artwork by the mat path used by the game."""
import argparse
from pathlib import Path

import UnityPy
from PIL import Image

NAMED_FIELDS = {"004": "geartown", "005": "volcano", "006": "ruins", "007": "capital"}


def pack(installation, assets):
    bundles = installation / "MDPro3_Data/StandaloneWindows64/MDPro3"
    env = UnityPy.load(str(bundles / "prefab_assets_scriptableobjects.bundle"))
    items = next(o.read_typetree() for o in env.objects if o.type.name == "MonoBehaviour" and o.peek_name() == "Items")
    output = assets / "assets/mdpromod/textures/field_icon"
    output.mkdir(parents=True, exist_ok=True)
    for mat in items["mats"]:
        if not mat["path"].startswith("Mat/Mat_"):
            continue
        number = mat["path"].split("_")[-1]
        theme = NAMED_FIELDS.get(number, number)
        if not (assets / "assets/mdpromod/field" / theme / "scene.bin").is_file():
            continue
        resource = bundles / f"icon_assets_fieldicon{mat['id']}_hd.bundle"
        icon = next(o.read().image for o in UnityPy.load(str(resource)).objects if o.type.name == "Texture2D")
        art = icon.convert("RGBA")
        art = art.crop(art.getchannel("A").getbbox())
        art.thumbnail((184, 120), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (192, 128))
        canvas.paste(art, ((192 - art.width) // 2, (128 - art.height) // 2))
        canvas.save(output / (theme + ".png"), optimize=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("installation", type=Path)
    parser.add_argument("assets", type=Path)
    args = parser.parse_args()
    pack(args.installation, args.assets)
