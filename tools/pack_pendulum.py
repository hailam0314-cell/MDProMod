"""Compress MDPro3's pendulum summon resources into the mod's PNG and indexed mesh formats.

Usage: python tools/pack_pendulum.py <MDPro3 installation> <bundle root>
Requires UnityPy and Pillow. stageBundle reads runtime/summon_fx and runtime/summon_mesh.
"""
import argparse
import json
from pathlib import Path

import UnityPy
from PIL import Image, ImageChops, ImageDraw
from UnityPy.helpers.MeshHelper import MeshHandler


def textures(bundle, requests, output):
    env = UnityPy.load(str(bundle))
    paths = {Path(path).stem: ref for path, ref in env.container.items() if path.endswith(".png")}
    for source, destination, size, mask in requests:
        image = paths[source.lower()].read().image.convert("RGBA")
        image.thumbnail((size, size), Image.Resampling.LANCZOS)
        if mask in ("white", "color"):
            # The Unity materials use additive blending. Bake black to transparency for Forge.
            rgb = image.convert("RGB")
            red, green, blue = rgb.split()
            intensity = ImageChops.lighter(ImageChops.lighter(red, green), blue)
            if mask == "white":
                image = Image.new("RGBA", image.size, "white")
            image.putalpha(intensity)
        image = image.quantize(colors=256, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)
        image.save(output / (destination + ".png"), optimize=True)


def pack_mesh(bundle, output):
    env = UnityPy.load(str(bundle))
    ref = next(ref for path, ref in env.container.items() if path.endswith("/summonpendulumchoker02.asset"))
    mesh = MeshHandler(ref.read())
    mesh.process()
    for name, triangles in zip(("pendulum_crystal", "pendulum_chain"), mesh.get_triangles(), strict=True):
        # Keep UV seams and the independent crystal/chain materials.
        vertices = {}
        positions, uvs, indices = [], [], []
        for triangle in triangles:
            for index in triangle:
                p = tuple(round(x, 4) for x in mesh.m_Vertices[index])
                uv = tuple(round(x, 5) for x in mesh.m_UV0[index])
                key = (p, uv)
                if key not in vertices:
                    vertices[key] = len(positions)
                    positions.append(p)
                    uvs.append(uv)
                indices.append(vertices[key])
        data = dict(positions=positions, uvs=uvs, indices=indices)
        (output / (name + ".json")).write_text(json.dumps(data, separators=(",", ":")), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("installation", type=Path)
    parser.add_argument("root", type=Path)
    args = parser.parse_args()
    source = args.installation / "MDPro3_Data/StandaloneWindows64/MasterDuel/Built-in"
    fx = args.root / "runtime/summon_fx"
    mesh = args.root / "runtime/summon_mesh"
    for folder in (fx, mesh):
        folder.mkdir(parents=True, exist_ok=True)
    textures(source / "duel", [
        ("tlt_SPendulumChoker01", "pendulum_crystal", 256, None),
        ("tlt_SPendulumChain01", "pendulum_chain", 128, None),
        ("SummonPendulumFoucault", "pendulum_orbit", 384, None),
        ("fxt_mak_001", "pendulum_land_rosette", 256, "white"),
        ("fxt_imp_006", "pendulum_land_impact", 192, "white"),
        ("fxt_imp_007", "pendulum_land_flare", 128, None),
        ("fxt_Trail_001", "pendulum_scale_line", 192, "white"),
        ("fxt_fre_008", "pendulum_land_rainbow", 64, "color"),
    ], fx)
    textures(source / "textures", [
        (f"{side}PendulumNum0{digit}", f"pendulum_{side.lower()}{digit}", 96, None)
        for side in ("L", "R") for digit in range(10)
    ], fx)
    # The S2 impact ring uses a procedural Unity material with no source texture.
    ring = Image.new("L", (256, 256))
    draw = ImageDraw.Draw(ring)
    for inset, alpha in ((9, 38), (11, 96), (13, 235), (15, 125), (17, 45)):
        draw.ellipse((inset, inset, 255-inset, 255-inset), outline=alpha, width=2)
    image = Image.new("RGBA", ring.size, "white")
    image.putalpha(ring)
    image.resize((128, 128), Image.Resampling.LANCZOS).save(fx / "pendulum_land_ring.png", optimize=True)
    pack_mesh(source / "duel", mesh)


if __name__ == "__main__":
    main()
