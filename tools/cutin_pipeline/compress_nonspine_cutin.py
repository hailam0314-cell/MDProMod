"""Preserve and resize the one SpriteRenderer/Timeline cutin without inventing Spine data."""
import json
from pathlib import Path
import extract_cutin as x
from compress_cutins import DEST, Image


def extract_and_compress(code="27204311"):
    env = x.UnityPy.load(str(Path(x.ROOT) / code))
    textures = {}
    sprite_bindings = []
    for obj in env.objects:
        if obj.type.name != "SpriteRenderer":
            continue
        renderer = obj.read()
        sprite = renderer.m_Sprite.read()
        texture_ptr = sprite.m_RD.texture
        texture_key = x.pptr_key(texture_ptr)
        texture = texture_ptr.read()
        textures[texture_key] = texture
        sprite_bindings.append({"renderer_key": x.reader_key(obj),
                                "game_object": x.name_of(renderer.m_GameObject),
                                "sprite": sprite.m_Name, "texture_key": texture_key,
                                "texture": texture.m_Name})
    if not textures:
        raise RuntimeError("Non-Spine prefab contains no bound sprite textures")
    source = Path(x.OUT_ROOT) / code
    target = DEST / code
    source.mkdir(parents=True, exist_ok=True)
    target.mkdir(parents=True, exist_ok=True)
    pages = []
    used_names = set()
    for key, texture in textures.items():
        name = texture.m_Name + ".png"
        if name in used_names:
            raise RuntimeError(f"Ambiguous non-Spine texture filename: {name}")
        used_names.add(name)
        image = texture.image.convert("RGBA")
        image.save(source / name)
        dimensions = (image.width, image.height)
        size = (max(1, image.width // 8), max(1, image.height // 8))
        # Unity Sprites/Default consumes straight-alpha texture samples.
        image.resize(size, Image.Resampling.LANCZOS).save(target / name, optimize=True, compress_level=9)
        with Image.open(target / name) as check:
            assert check.size == size
        pages.append({"code": code, "page": name, "source_size": dimensions,
                      "logical_size": dimensions, "size": size,
                      "before": (source / name).stat().st_size,
                      "after": (target / name).stat().st_size,
                      "match": "sprite_texture_binding", "texture_key": key})
    manifest = {"code": code, "kind": "unity_sprite_timeline", "playable_as_spine": False,
                "reason": "Uses SpriteRenderer and Unity AnimationClip/Timeline, no Spine skeleton/track",
                "bindings": sprite_bindings, "pages": pages}
    manifest_text = json.dumps(manifest, ensure_ascii=False, indent=1)
    for folder in (source, target):
        (folder / "nonspine.json").write_text(manifest_text, encoding="utf-8")
    return {"code": code, "ok": True, "kind": "unity_sprite_timeline", "rigs": [],
            "pages": pages, "non_png_bytes": (target / "nonspine.json").stat().st_size}


if __name__ == "__main__":
    print(json.dumps(extract_and_compress(), indent=2))


