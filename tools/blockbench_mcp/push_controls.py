"""把 assets/duel_controls 的贴图和几何推进正在运行的 Blockbench。

用法：
  python push_controls.py tex        仅同步贴图
  python push_controls.py geom       重建 ctl_* 分组与方块
  python push_controls.py inspect    列出项目大纲与贴图
"""
import base64
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import client  # noqa: E402

ASSETS = Path(r"D:\MDProMod\assets\duel_controls")
LAYOUT = Path(r"D:\MDProMod\assets\duel_field\duel_field_layout_v2.json")
SPEC = json.loads((ASSETS / "control_spec.json").read_text(encoding="utf-8"))


def call(name, arguments=None):
    return client.text_result(client.call(name, arguments or {}))


def eval_js(code):
    return client.text_result(client.call("risky_eval", {"code": code}))


def push_textures():
    listing = call("list_textures") or {}
    if isinstance(listing, dict):
        have = {t.get("name") for t in listing.get("textures", [])}
    else:
        have = {t.get("name") for t in listing}
    made = []
    updated = []
    for name, info in SPEC["textures"].items():
        payload = "data:image/png;base64," + base64.b64encode(
            (ASSETS / info["file"]).read_bytes()).decode("ascii")
        if name in have:
            # risky_eval 拒绝代码里出现连续斜杠，base64 可能包含，先用占位符替换再在 JS 里拼回
            safe_b64 = payload.split(",", 1)[1].replace("//", "~~")
            eval_js(
                "(function(){const t=Texture.all.find(x=>x.name===%s);"
                "if(t){t.uv_width=%d;t.uv_height=%d;"
                "t.fromDataURL('data:image/png;base64,'+%s.split('~~').join('/'+'/'));}"
                "return {updated:%s};})()"
                % (json.dumps(name), info["size"][0], info["size"][1],
                   json.dumps(safe_b64), json.dumps(name)))
            updated.append(name)
            continue
        call("create_texture", {
            "name": name,
            "width": info["size"][0],
            "height": info["size"][1],
            "uv_width": info["size"][0],
            "uv_height": info["size"][1],
            "data": payload,
        })
        made.append(name)
    return {"created": made, "updated": updated, "existing": sorted(x for x in have if x)}


def p1_controls():
    layout = json.loads(LAYOUT.read_text(encoding="utf-8"))
    return [c for c in layout["controls"] if c["owner"] == "p1"]


def box(center, half_x, half_z, y0, y1, name, texture, tile="cap"):
    x, _, z = center
    return {
        "name": name,
        "from": [round(x - half_x, 3), y0, round(z - half_z, 3)],
        "to": [round(x + half_x, 3), y1, round(z + half_z, 3)],
        "texture": texture,
        "tile": tile,
    }


def build_plan():
    spec_phase = SPEC["phase_button"]
    spec_confirm = SPEC["confirm_button"]
    spec_lp = SPEC["lp_panel"]
    frame_tex = SPEC["frame"]["texture"]
    plan = []
    state_cycle = {"dp": "disabled", "sp": "available", "m1": "current",
                   "bp": "available", "m2": "available", "ep": "available"}
    for control in p1_controls():
        center = control["center"]
        kind = control["kind"]
        if kind == "phase":
            state = state_cycle.get(control["phase"].lower(), "disabled")
            plan.append({
                "group": "ctl_" + control["id"],
                "cubes": [
                    box(center, spec_phase["footprint"][0] / 2, spec_phase["footprint"][1] / 2,
                        SPEC["frame"]["bottom"], spec_phase["cap_bottom"],
                        "frame", frame_tex, "top"),
                    box(center, spec_phase["cap"][0] / 2, spec_phase["cap"][1] / 2,
                        spec_phase["cap_bottom"], spec_phase["cap_top"],
                        "cap_" + state, "control_phase_" + state, "top"),
                ],
            })
        elif kind == "confirm":
            plan.append({
                "group": "ctl_" + control["id"],
                "cubes": [
                    box(center, spec_confirm["footprint"][0] / 2, spec_confirm["footprint"][1] / 2,
                        SPEC["frame"]["bottom"], spec_confirm["cap_bottom"],
                        "frame", frame_tex, "top"),
                    box(center, spec_confirm["cap"][0] / 2, spec_confirm["cap"][1] / 2,
                        spec_confirm["cap_bottom"], spec_confirm["cap_top"],
                        "cap_started", "control_confirm_started", "top"),
                ],
            })
        elif kind == "lp_display":
            half_x = spec_lp["footprint"][0] / 2
            half_z = spec_lp["footprint"][1] / 2
            rim = spec_lp["rim_width"]
            y0, y1 = spec_lp["slab_bottom"], spec_lp["slab_top"]
            rim0, rim1 = spec_lp["slab_top"], spec_lp["rim_top"]
            x, _, z = center
            edge_x = half_x - rim / 2
            edge_z = half_z - rim / 2
            inner_z = half_z - rim
            cubes = [
                box(center, half_x, half_z, y0, y1, "screen", spec_lp["texture"], "top"),
                box((x - edge_x, 0, z), rim / 2, inner_z, rim0, rim1, "rim_left", frame_tex, "top"),
                box((x + edge_x, 0, z), rim / 2, inner_z, rim0, rim1, "rim_right", frame_tex, "top"),
                box((x, 0, z + edge_z), half_x, rim / 2, rim0, rim1, "rim_front", frame_tex, "top"),
                box((x, 0, z - edge_z), half_x, rim / 2, rim0, rim1, "rim_back", frame_tex, "top"),
            ]
            plan.append({"group": "ctl_" + control["id"], "cubes": cubes})
    return plan


def push_geometry():
    tiles = {name: info["tiles"] for name, info in SPEC["textures"].items()}
    payload = json.dumps({"plan": build_plan(), "tiles": tiles}, ensure_ascii=False)
    code = """
(function () {
  const data = %s;
  const removed = [];
  const undo = Undo.initEdit({outliner: true, elements: [], textures: false});
  const dropGroup = (g) => {
    if (typeof g.remove === 'function') { g.remove(); removed.push(g.name); }
    else if (g.parent && g.parent.children) {
      g.parent.children.remove(g); removed.push(g.name);
    }
  };
  Group.all.slice().forEach(g => { if (g.name.indexOf('ctl_') === 0) dropGroup(g); });
  Cube.all.slice().forEach(c => { if (c.name && c.name.indexOf('ctl_') === 0) c.remove(); });
  Project.box_uv = false;
  const findTexture = (name) => Texture.all.find(t => t.name === name) || Texture.all[0];
  const facesFor = (texName, tileKey) => {
    const tiles = data.tiles[texName];
    const texture = findTexture(texName);
    const rect = tiles[tileKey];
    const mk = (r) => ({uv: r.slice(), texture: texture});
    return {
      north: mk(tiles.side), south: mk(tiles.side),
      east: mk(tiles.side), west: mk(tiles.side),
      up: mk(rect), down: mk(tiles.pad),
    };
  };
  const built = [];
  data.plan.forEach(entry => {
    const group = new Group({name: entry.group, origin: [0, 0, 0]});
    group.init();
    group.addTo('root');
    entry.cubes.forEach(c => {
      const cube = new Cube({
        name: c.name,
        from: c.from,
        to: c.to,
        origin: [0, 0, 0],
        faces: facesFor(c.texture, c.tile),
      });
      cube.box_uv = false;
      cube.init();
      cube.addTo(group);
      built.push(entry.group + '/' + c.name);
    });
  });
  Canvas.updateAll();
  Undo.finishEdit('Build duel control panels');
  return {removed_groups: removed, built: built.length, names: built,
          textures: Texture.all.map(t => t.name)};
})()
""" % payload
    return eval_js(code)


def inspect():
    return eval_js("""
({
  project: {name: Project.name, uuid: Project.uuid, format: Format.id, box_uv: Project.box_uv},
  textures: Texture.all.map(t => ({name: t.name, w: t.width, h: t.height, uw: t.uv_width, uh: t.uv_height})),
  groups: Group.all.filter(g => g.name.indexOf('ctl_') === 0).map(g => ({name: g.name, cubes: g.children.length})),
  cubes: Cube.all.filter(c => c.name && c.name.indexOf('ctl_') === 0).length,
})
""")


def main():
    command = sys.argv[1] if len(sys.argv) > 1 else "inspect"
    if command == "tex":
        out = push_textures()
    elif command == "geom":
        out = push_geometry()
    else:
        out = inspect()
    print(json.dumps(out, ensure_ascii=False, indent=1)[:6000])


if __name__ == "__main__":
    main()
