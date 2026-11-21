"""Export the additional installed MDPro3 duel fields to Forge and Blockbench.

python tools/pack_field_catalog.py <MDPro3 installation> <runtime/field_assets> <project folder> [bundle IDs ...]
"""
import argparse
import re
import zlib
from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

from pack_geartown import Field, CapitalPose, clip_samples, local_matrix, xyz

HANDCRAFTED = {"004", "005", "006", "007"}
ENABLED = zlib.crc32(b"m_Enabled")
ACTIVE = zlib.crc32(b"m_IsActive")
EVENT = re.compile(r"damage|dmg|/tap|_tap|/start|_start|_toend|_tophase[2-4]|_brk|endwin|explosion|summon|exclude", re.I)


def catalog(installation):
    data = installation / "MDPro3_Data/StandaloneWindows64"
    env = UnityPy.load(str(data / "MDPro3/prefab_assets_scriptableobjects.bundle"))
    items = next(o.read_typetree() for o in env.objects if o.type.name == "MonoBehaviour" and o.peek_name() == "Items")
    translations = []
    for language in ("en-US", "zh-CN"):
        text = (installation / "Data/locales" / language / "IDS/IDS_ITEM.txt").read_text("utf-8")
        translations.append(dict(re.findall(r"\[IDS_ITEM.ID(\d+)\]\s*\n([^\n]+)", text)))
    entries = {}
    for item in items["mats"]:
        if not item["path"].startswith("Mat/Mat_"):
            continue
        number = item["path"].split("_")[-1]
        if number in entries or number in HANDCRAFTED:
            continue
        item_id = item["id"]
        accessory_id = {1098001: 1090009, 1098002: 1090003}.get(item_id, item_id)
        parts = [next(p["path"] for p in items[group] if p["id"] == accessory_id + offset)
                 for group, offset in (("graves", 10000), ("stands", 20000))]
        names = [table.get(str(item_id)) or item["m_name"] or "" for table in translations]
        entries[number] = (names, parts)
    return entries


class ScenePose:
    """Resolve each clip relative to its own Animator, then apply inherited activation and TRS."""
    frames = 24
    skin = CapitalPose.skin

    def __init__(self, objects):
        self.transforms = {}
        self.paths = {}
        self.channels = defaultdict(dict)
        self.periods = {}
        self.cache = {}
        self.visible = {}
        self.parents = {}
        self.go = {}
        for o in objects:
            if o.type.name == "Transform":
                t = o.read()
                self.transforms[o.path_id] = t
                self.parents[o.path_id] = t.m_Father.path_id
                self.go[o.path_id] = t.m_GameObject.read()
        for tid in self.transforms:
            self.path(tid)
        schedules = []
        for o in objects:
            if o.type.name != "Animator":
                continue
            a = o.read()
            if not a.m_Controller.path_id:
                continue
            g = a.m_GameObject.read()
            root = next(c.component.path_id for c in g.m_Component if c.component.type.name == "Transform")
            prefix = self.paths[root]
            if EVENT.search(prefix):
                continue
            relative = {zlib.crc32(path[len(prefix):].strip("/").encode()): tid
                        for tid, path in self.paths.items() if path == prefix or path.startswith(prefix + "/")}
            for ref in a.m_Controller.read().m_AnimationClips:
                if not ref.path_id:
                    continue
                c = ref.read()
                name = c.m_Name.lower()
                rest = bool(re.search(r"(?:^|_)phase1(?:$|_)", name))
                idle = "phaseall" in name or "idle" in name or "loop" in name
                if (not idle and not rest) or re.search(r"damage|dmg|tap|start|toend|tophase|break", name):
                    continue
                schedules.append((not rest, c, relative))
        for moving, c, relative in sorted(schedules, key=lambda v: v[0]):
            if not c.m_MuscleClip or not c.m_ClipBindingConstant.genericBindings:
                continue
            period = c.m_MuscleClip.m_StopTime
            times = np.arange(self.frames) * period / self.frames if moving and period > 0.05 else np.array([0.])
            for h, channels in clip_samples(c, times, include_properties=True).items():
                tid = relative.get(h)
                if tid is None:
                    continue
                for key, curve in channels.items():
                    if key not in (1, 2, 3, 4, ACTIVE, ENABLED):
                        continue
                    animated = len(times) > 1 and np.max(np.ptp(curve, axis=0)) > 1e-5
                    self.channels[tid][key] = curve if animated and key in (1, 2, 3, 4) else curve[:1]
                    if animated and key in (1, 2, 3, 4):
                        self.periods[tid] = period

    def path(self, tid):
        if tid not in self.paths:
            self.paths[tid] = (self.path(self.parents[tid]) if self.parents[tid] else "") + "/" + self.go[tid].m_Name
        return self.paths[tid]

    def active(self, tid):
        if tid not in self.visible:
            value = self.channels[tid].get(ACTIVE)
            on = self.go[tid].m_IsActive if value is None else value[0, 0] >= .5
            self.visible[tid] = on and (not self.parents[tid] or self.active(self.parents[tid]))
        return self.visible[tid]

    def matrices(self, t):
        tid = t.object_reader.path_id
        if tid not in self.cache:
            parent = self.parents[tid]
            base = self.matrices(self.transforms[parent]) if parent else np.eye(4)[None]
            if parent in self.periods:
                self.periods.setdefault(tid, self.periods[parent])
            ch = self.channels[tid]
            q = t.m_LocalRotation
            p = ch.get(1, xyz(t.m_LocalPosition)[None])
            r = ch.get(2, np.array([[q.x, q.y, q.z, q.w]]))
            s = ch.get(3, xyz(t.m_LocalScale)[None])
            if 4 in ch:
                # Unity applies Euler rotations Z, X, Y in that order.
                x, y, z = np.radians(ch[4].T) / 2
                cx, cy, cz = np.cos([x, y, z])
                sx, sy, sz = np.sin([x, y, z])
                r = np.stack((cy*sx*cz + sy*cx*sz, sy*cx*cz - cy*sx*sz,
                              cy*cx*sz - sy*sx*cz, cy*cx*cz + sy*sx*sz), axis=1)
            count = max(map(len, (p, r, s)))
            arrays = [np.broadcast_to(a, (count, a.shape[1])) for a in (p, r, s)]
            self.cache[tid] = base @ [local_matrix(pos, rot / max(np.linalg.norm(rot), 1e-10), scale)
                                      for pos, rot, scale in zip(*arrays)]
        return self.cache[tid]


def ambient_kind(path):
    if EVENT.search(path):
        return None
    if any(group in path.lower() for group in ("mat018_bg_light", "mat040_hex_", "mat049_citynoise")):
        return 9
    leaf = path.lower().split("/")[-1]
    for terms, kind in ((("firefly", "star", "spark", "lightning", "laser", "triangle", "thunder", "flash", "_ple_"), 9), (("fire", "flame"), 5),
                        (("snow",), 6), (("blossom", "petal"), 4), (("rain",), 7),
                        (("smoke", "fog"), 2), (("bubble", "shabon"), 8),
                        (("splash",), 3), (("leaf", "spore", "dust", "dst", "sand", "confetti"), 10)):
        if any(s in leaf for s in terms):
            return kind
    return None


class CatalogField(Field):
    def material(self, mat):
        name = mat.m_Name.lower()
        props = mat.m_SavedProperties
        slots = {k:v for k,v in props.m_TexEnvs if v.m_Texture.path_id and not v.m_Texture.file_id}
        authored = name.startswith("mt_") or name.startswith("m_bird")
        if name in ("lit", "default_material") or any(k in name for k in ("depthmask", "outline", "decal", "disappearance")):
            return None
        if not authored and (EVENT.search(name) or any(k in name for k in ("shadow", "mask", "fakelight", "volumetric", "wind", "fog"))):
            return None
        candidates = [(k, v, v.m_Texture.deref().peek_name() or "") for k, v in slots.items()]
        def priority(k, tex):
            if "basecolor" in tex.lower():
                return 0
            if k in ("_Texture2D", "_BaseMap", "_BaseTexture", "_MainTex", "_Maintex"):
                return 1
            if re.search(r"noise|normal|mask|lightmap|distort|flow|gradation", k+" "+tex, re.I):
                return 4
            return 2
        candidates.sort(key=lambda slot: priority(slot[0], slot[2]))
        if tex_mask := next((v for _,v,t in candidates if t.lower().startswith("maskgide_mat013")), None):
            key = "led_" + name
            if key not in self.textures:
                im = tex_mask.m_Texture.read().image.convert("RGB")
                im.thumbnail((512,512), Image.Resampling.LANCZOS)
                rgb = np.asarray(im,dtype=float)/255
                c = dict(props.m_Colors).get("_Color01")
                tint = np.clip([c.r,c.g,c.b],0,1) if c else np.array([.1,.55,.8])
                mask = np.maximum(rgb[:,:,0]*.5,rgb[:,:,1])
                mask = mask * (.38+.62*(np.indices(mask.shape)[0]%3==0))
                lit = np.uint8(np.clip(12+150*mask[:,:,None]*tint,0,255))
                self.store_texture(key,Image.fromarray(lit))
            return key,11,tex_mask
        if not candidates:
            line_light = bool(re.search(r"mat(?:502|504)_line", name))
            if not authored and not line_light:
                return None
            colors = dict(props.m_Colors)
            c = next((colors[k] for k in (("_Color01","_BaseColor") if line_light else ("_BaseColor","_Color","_MainColor")) if k in colors), None)
            if c is None:
                return None
            rgb = tuple(int(np.clip(v, 0, 1)*255) for v in (c.r,c.g,c.b))
            key = "solid_" + bytes(rgb).hex()
            if key not in self.textures:
                self.store_texture(key, Image.new("RGBA", (2, 2), rgb+(255,)))
            return key, 5 if line_light else 0, None
        k, slot, tex = candidates[0]
        if priority(k, tex) == 4 and not authored and not re.search("water|light|godray|firefly", name):
            return None
        kind = 0
        if re.search(r"waterfall|watersurface|puddle|liquid|rippl", name): kind = 6
        elif re.search(r"monitor|signage|auroravision|screen|_sign[0-9]|_light_[orby]|emiss|lightwall|magiccircle", name): kind = 11
        elif not authored and re.search(r"glow|light|godray|hexagon|flame|fire|aurora|aura|beam|magic|lightning", name): kind = 5
        if not authored and kind == 0 and not re.search(r"flag|pole|tsuchinoko|fish|water|airship|candle|tentacle|spider|knight|ball|capa|plant|banner|branch|dragon|stone|cloud|bird|machine",name):
            return None
        if self.theme in ("009", "801") and re.match(r"Mat_009_0[23]_BaseColor", tex, re.I):
            # The skyscraper shaders use the atlas alpha as a reflection/light mask.
            window = dict(props.m_Colors).get("_BaseColor01")
            key = name if window else tex.lower() + "_opaque"
            if key not in self.textures:
                im = slot.m_Texture.read().image.convert("RGBA")
                im.thumbnail((1024,1024),Image.Resampling.LANCZOS)
                rgba = np.asarray(im,dtype=float)/255
                rgb = rgba[:,:,:3] ** (1/2.2)
                if window:
                    tint = np.array([window.r,window.g,window.b])
                    tint = tint/max(1.,tint.max())
                    rgb += 1.5*rgba[:,:,3,None]*tint
                self.store_texture(key, Image.fromarray(np.uint8(np.clip(rgb,0,1)*255)).convert("RGBA"))
            return key,kind,slot
        if not authored and kind in (5, 6):
            # Shader masks store intensity in RGB even with a fully opaque alpha channel.
            key = re.sub("[^a-z0-9_.-]", "_", name)
            if key not in self.textures:
                im = slot.m_Texture.read().image.convert("RGBA")
                im.thumbnail((512,512),Image.Resampling.LANCZOS)
                rgba = np.array(im)
                colors = dict(props.m_Colors)
                tint = next((c for k,c in colors.items() if ("color" in k.lower()) and max(c.r,c.g,c.b) > .05),None)
                rgb = np.array([tint.r,tint.g,tint.b]) if tint else np.array([.65,.8,1.])
                mask = rgba[:,:,:3].max(axis=2)/255 * (rgba[:,:,3]/255)
                rgba[:,:,:3] = np.rint(255*np.clip(rgb,0,1))
                rgba[:,:,3] = np.rint((95 if kind==6 else 125)*mask)
                self.store_texture(key,Image.fromarray(rgba))
            return key,kind,slot
        return self.texture(slot.m_Texture), kind, slot

    def collect(self, root, part, parent):
        env = UnityPy.load(str(root / part))
        objects = list(env.objects)
        for resource in ("unity default resources", "unity_builtin_extra"):
            resource_path = root.parents[1] / "Resources" / resource
            if resource_path.is_file():
                env.load_file(str(resource_path))
        pose = ScenePose(objects)
        anchors = {}
        for tid, t in pose.transforms.items():
            name = pose.go[tid].m_Name
            if name.startswith("POS_"):
                anchors[name] = parent @ pose.matrices(t)[0]
        children = []
        is_field = part.startswith("Mat/")
        for obj in objects:
            skinned = obj.type.name == "SkinnedMeshRenderer"
            if not skinned and obj.type.name not in ("MeshRenderer", "ParticleSystem"):
                continue
            renderer = obj.read()
            go = renderer.m_GameObject.read()
            comps = {c.component.type.name: c.component for c in go.m_Component}
            tid = comps["Transform"].path_id
            path = pose.paths[tid]
            # Wraitsoth names its intact, separately breakable board panels for
            # the event that removes them. Phase1 places them on the board.
            board_panel = self.theme == "025" and go.m_Name.startswith(("zone_toend", "zone_tophase"))
            if not pose.active(tid) or EVENT.search(path) and not board_panel:
                continue
            if obj.type.name == "ParticleSystem":
                kind = ambient_kind(path) if is_field and renderer.looping and renderer.EmissionModule.enabled else None
                if kind is not None:
                    mat = parent @ pose.matrices(pose.transforms[tid])[0]
                    self.vents.append((kind, *mat[:3,3], 0, 1, 0, 3, 0))
                continue
            enabled = pose.channels[tid].get(ENABLED)
            if not (renderer.m_Enabled if enabled is None else enabled[0,0] >= .5):
                continue
            if re.search(r"/shadow$|/collision|/collider|/dummy|/crack.*_end|/fxp_hl_", path, re.I):
                continue
            if 'destroy' in path.lower() and enabled is None and tid not in pose.channels:
                continue
            if not skinned and "MeshFilter" not in comps:
                continue
            mats = [ref.read() if ref.path_id and not ref.file_id else None for ref in renderer.m_Materials]
            slots = [self.material(m) if m else None for m in mats]
            if not any(slots):
                continue
            mat = parent @ pose.matrices(pose.transforms[tid])
            if abs(np.linalg.det(mat[0,:3,:3])) < 1e-9:
                continue
            meshref = renderer.m_Mesh if skinned else comps["MeshFilter"].read().m_Mesh
            if not meshref.path_id:
                continue
            mesh = MeshHandler(meshref.read())
            mesh.process()
            if not mesh.m_Vertices:
                continue
            verts = np.asarray(mesh.m_Vertices)[:,:3]
            seq = verts @ mat[:,:3,:3].transpose(0,2,1) + mat[:,None,:3,3]
            raw_norm = mesh.m_Normals
            normal = None if raw_norm is None else np.array(raw_norm)[:,:3] @ np.linalg.inv(mat[0,:3,:3])
            period = pose.periods.get(tid, 1.)
            if skinned and renderer.m_Bones and mesh.m_BoneWeights and raw_norm is not None:
                seq, normal = pose.skin(renderer, mesh, parent)
                period = next((pose.periods[b.path_id] for b in renderer.m_Bones if b.path_id in pose.periods),period)
            points = seq[0]
            if np.max(np.abs(points)) >= 255:
                continue
            sub = mesh.get_triangles()
            if normal is None:
                normal = np.zeros_like(points)
                for tris in sub:
                    if not tris:
                        continue
                    a,b,c = points[np.asarray(tris)].transpose(1,0,2)
                    face = np.cross(b-a,c-a)*np.sign(np.linalg.det(mat[0,:3,:3]))
                    for corner in np.asarray(tris).T:
                        np.add.at(normal,corner,face)
            normal /= np.maximum(1e-9, np.linalg.norm(normal,axis=1)[:,None])
            uv = np.zeros((len(points),2)) if mesh.m_UV0 is None else np.asarray(mesh.m_UV0)[:,:2].copy()
            uv[:,1] = 1-uv[:,1]
            center = mat[0,:3,3]
            motion = (part,obj.path_id)
            loop = seq - points
            if len(seq)==1 and any(m and ('flag' in m.m_Name.lower()) for m in mats) and 'pole' not in go.m_Name.lower():
                # Cloth displacement increases with distance from the pole.
                axis = np.argmax(np.ptp(points[:,[0,2]],axis=0))*2
                weight = np.abs(points[:,axis]-center[axis])
                weight /= max(.001,weight.max())
                phase = np.arange(24)[:,None]*np.pi/12
                loop = (.075*(np.sin(phase+weight*3)-np.sin(weight*3))*weight)[:,:,None]*np.array([.55,0,.83])
                period=2.4
            moving = len(loop)>1 and np.max(np.abs(loop)) > .003 and np.max(np.abs(loop)) < 127
            if moving:
                self.vertex_loops[motion]=(period,loop)
            points_bb = np.round((points-center)*16,4)
            vertex_map = {}
            keys=[]
            for p in points_bb:
                k = tuple(p)
                keys.append(vertex_map.setdefault(k,f"v{len(vertex_map):x}"))
            faces={}
            for index,tris in enumerate(sub):
                slot = slots[min(index,len(slots)-1)]
                if not slot or not tris:
                    continue
                tex,kind,st = slot
                tri = np.asarray(tris,dtype=int)
                if np.linalg.det(mat[0,:3,:3])<0:
                    tri=tri[:,::-1]
                uv2=uv if st is None else uv*[st.m_Scale.x,st.m_Scale.y]+[st.m_Offset.x,1-st.m_Scale.y-st.m_Offset.y]
                self.sections[tex,8 if moving else kind,motion if moving else ()].append((points,normal,uv2,tri))
                for triangle in tri:
                    ids=[keys[i] for i in triangle]
                    if len(set(ids))<3:
                        continue
                    coords=uv2[triangle]
                    coords=coords-np.floor(coords.min(axis=0)+1e-6)
                    faces[f"f{len(faces):x}"]=dict(vertices=ids, uv=dict(zip(ids,(coords*16).round(5).tolist())),texture=int(self.textures[tex]["id"]))
            if faces:
                eid=self.stable_id(part+"/"+str(obj.path_id))
                children.append(eid)
                self.elements.append(dict(uuid=eid,type="mesh",name=go.m_Name,origin=(center*16).round(4).tolist(),
                                          vertices={v:k for k,v in vertex_map.items()},faces=faces))
        if not children:
            raise ValueError("No drawable geometry in "+part)
        self.groups.append((dict(uuid=self.stable_id(part),name=part.split("/")[-1],origin=[0,0,0]),children))
        return anchors

    def surfaces(self):
        """Measure tray tops and find clear space for both players' button rows."""
        triangles = np.concatenate([
            points[indices] for (_, kind, _), parts in self.sections.items()
            if kind in (0, 8, 11) for points, _, _, indices in parts
        ])
        def raycaster(geometry, tolerance, edge):
            a, b, c = geometry.transpose(1, 0, 2)
            u, v = b - a, c - a
            det = u[:, 0] * v[:, 2] - u[:, 2] * v[:, 0]
            valid = np.abs(det) > tolerance
            denom = np.where(valid, det, 1)

            def top(x, z, ceiling=np.inf):
                s = ((x-a[:, 0])*v[:, 2] - (z-a[:, 2])*v[:, 0]) / denom
                t = (u[:, 0]*(z-a[:, 2]) - u[:, 2]*(x-a[:, 0])) / denom
                y = a[:, 1] + s*u[:, 1] + t*v[:, 1]
                hits = y[valid & (s >= -edge) & (t >= -edge) & (s+t <= 1+edge) & (y < ceiling)]
                return float(max(hits, default=0))

            return top

        # Test button clearance against the exact coordinates drawn by Forge.
        top = raycaster(triangles, 1e-8, 1e-6)
        packed_top = raycaster(np.rint(triangles * 128) / 128, 1e-6, 0)

        def clear_row(xs, width, distances, minimum):
            rows = []
            for z in distances:
                height = max(packed_top(side*(x+dx), side*(z+dz))
                             for side in (-1, 1) for x in xs
                             for dx in (-width, 0, width) for dz in (-.24, 0, .24))
                y = max(minimum, round(height, 2))
                rows.append((y + .18 * abs(z-distances[0]), z, round(y+.14, 3)))
            _, z, y = min(rows)
            return z, y

        phase = clear_row((-3.44, -2.06, -.69, .69, 2.06, 3.44), .54,
                          (5.22, 5.43, 5.64, 5.85, 6.06, 6.27), .25)
        panels = clear_row((-5.94, 5.94), 1.17, (3.50, 3.65, 3.80, 3.90), .12)
        floor = round(float(max(.05, -triangles[:, :, 1].min())), 3)
        anchors = ((6, 5.33), (-6, 5.33), (-6, -5.33), (6, -5.33), (6, 2), (-6, -2))
        piles = [round(max(0, top(x, z, 1.7)) + .003, 3) for x, z in anchors]
        return dict(placement=floor, controls=phase + panels, piles=piles)


def export(installation, assets, projects, number, entry):
    names, parts = entry
    field = CatalogField(assets, number)
    field.title = names[0] or "MDPro3 "+number
    root = installation/"MDPro3_Data/StandaloneWindows64/MasterDuel"
    field.basis = np.diag([2/8.6,.22,-2/9,1.])
    for side in ("near","far"):
        anchors=field.collect(root, f"Mat/Mat_{number}_{side}", field.basis)
        for part in parts:
            group = part.split("/")[0]
            m = anchors[f"POS_{group}_{side}"].copy()
            if group=="Grave":
                direction=1 if side=="near" else -1
                m[:3,3]=[6*direction,-.56,2*direction]
                m[2,:3]*=2/(6.24*(2/9))
            field.collect(root,part+"_"+side,m)
    field.save(projects/(number+".bbmodel"))
    return field.surfaces()


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for name in ("installation","assets","projects"):
        ap.add_argument(name,type=Path)
    ap.add_argument("numbers",nargs="*")
    args=ap.parse_args()
    entries=catalog(args.installation)
    for number in args.numbers or sorted(entries):
        metrics=export(args.installation,args.assets,args.projects,number,entries[number])
        print(number, metrics, flush=True)


if __name__=="__main__":
    main()
