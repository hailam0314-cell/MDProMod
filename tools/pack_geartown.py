"""Rebuild a duel field, idle effects and editable geometry from installed MDPro3 bundles.

python tools/pack_geartown.py <MDPro3> <runtime/field_assets> <model.bbmodel> [--theme geartown|volcano|ruins|capital]
Requires UnityPy, numpy and Pillow. Output assets are included by processResources.
"""
import argparse
import base64
from collections import defaultdict
import io
import json
from pathlib import Path
import struct
import uuid
import zlib

import numpy as np
from PIL import Image
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler


def xyz(vector):
    return np.array([vector.x, vector.y, vector.z])


def local_matrix(position, quaternion, scale):
    x, y, z, w = quaternion
    rotation = np.array([
        [1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
        [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
        [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)],
    ])
    matrix = np.eye(4)
    matrix[:3, :3] = rotation @ np.diag(scale)
    matrix[:3, 3] = position
    return matrix


def world_matrix(transform):
    q = transform.m_LocalRotation
    matrix = local_matrix(xyz(transform.m_LocalPosition), (q.x, q.y, q.z, q.w), xyz(transform.m_LocalScale))
    if transform.m_Father.path_id:
        return world_matrix(transform.m_Father.read()) @ matrix
    return matrix


def gear_curves(env):
    clip = next(o.read() for o in env.objects if o.type.name == "AnimationClip" and "gia_loop" in o.peek_name())
    streamed = clip.m_MuscleClip.m_Clip.data.m_StreamedClip
    words = np.asarray(streamed.data, dtype="<u4").tobytes()
    frames = {}
    offset = 0
    while offset < len(words):
        time, count = struct.unpack_from("<fI", words, offset)
        offset += 8
        keys = {}
        for _ in range(count):
            index, _, _, _, value = struct.unpack_from("<I4f", words, offset)
            offset += 20
            keys[index] = value
        if keys and time >= 0:
            frames[time] = keys
    duration = clip.m_MuscleClip.m_StopTime
    if set(frames) != {0, duration}:
        raise ValueError("Expected two Euler keys for each continuously rotating gear")
    rotations = [b for b in clip.m_ClipBindingConstant.genericBindings if b.attribute == 4 and b.typeID == 4]
    result = {}
    for i, binding in enumerate(rotations):
        start, end = (np.array([frames[t][3*i+j] for j in range(3)]) for t in (0, duration))
        delta = end - start
        axes = np.flatnonzero(abs(delta) > 1)
        if len(axes) != 1 or axes[0] == 2:
            raise ValueError("Unexpected gear rotation")
        axis = axes[0]
        result[binding.path] = (start, axis, round(delta[axis] / 360) * 360 / duration)
    return result


def gear_pose(transform, parent, curve):
    angles, channel, speed = curve
    x, y, z = np.radians(angles)
    cx, cy, cz = np.cos([x, y, z])
    sx, sy, sz = np.sin([x, y, z])
    # Unity applies Z, X, Y in that order. An animated X turns about the Y-rotated X axis.
    ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]])
    rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])
    base = parent @ world_matrix(transform.m_Father.read())
    local = np.eye(4)
    local[:3, :3] = ry @ rx @ rz @ np.diag(xyz(transform.m_LocalScale))
    local[:3, 3] = xyz(transform.m_LocalPosition)
    matrix = base @ local
    axis = base[:3, :3] @ (ry[:, 0] if channel == 0 else [0, 1, 0])
    # Rotate before the nonuniform Unity-to-block scale (and its Z reflection).
    scale = np.diag(parent)[:3]
    axis /= scale
    axis /= np.linalg.norm(axis)
    return matrix, (*matrix[:3, 3], *axis, speed)


def ruins_start_pose(env):
    """Local transforms from the intact Phase1 clip, including the inlaid floor pieces."""
    clip = next(o.read() for o in env.objects if o.type.name == "AnimationClip" and o.peek_name() == "Phase1")
    data = clip.m_MuscleClip.m_Clip.data
    stream = data.m_StreamedClip
    if data.m_DenseClip.m_CurveCount:
        raise ValueError("Unexpected sampled channels in the initial field pose")
    words = np.asarray(stream.data, dtype="<u4").tobytes()
    offset = 0
    values = [None] * stream.curveCount
    while offset < len(words):
        time, count = struct.unpack_from("<fI", words, offset)
        offset += 8
        for _ in range(count):
            index, _, _, _, value = struct.unpack_from("<I4f", words, offset)
            offset += 20
            if time == 0:
                values[index] = value
    if None in values:
        raise ValueError("Incomplete frame zero in Phase1")
    values.extend(data.m_ConstantClip.data)
    bindings = defaultdict(dict)
    offset = 0
    for binding in clip.m_ClipBindingConstant.genericBindings:
        if binding.typeID != 4 or binding.attribute not in (1, 2, 3):
            raise ValueError("Unknown initial-pose transform channel")
        count = 4 if binding.attribute == 2 else 3
        bindings[binding.path][binding.attribute] = values[offset:offset+count]
        offset += count
    if offset != len(values):
        raise ValueError("Unexpected Phase1 channel count")
    matrices, paths = {}, {}
    def resolve(t):
        key = t.object_reader.path_id
        if key not in matrices:
            if t.m_Father.path_id:
                father = t.m_Father.read()
                base = resolve(father)
                name = t.m_GameObject.read().m_Name
                path = (paths[father.object_reader.path_id] + "/" + name).lstrip("/")
            else:
                path, base = "", np.eye(4)
            channels = bindings.get(zlib.crc32(path.encode()), {})
            q = t.m_LocalRotation
            rotation = channels.get(2, (q.x, q.y, q.z, q.w))
            matrices[key] = base @ local_matrix(channels.get(1, xyz(t.m_LocalPosition)), rotation,
                                                channels.get(3, xyz(t.m_LocalScale)))
            paths[key] = path
        return matrices[key]
    return resolve

def clip_samples(clip, times, include_properties=False):
    """Evaluate Unity's streamed cubic keys and constant transform channels."""
    data = clip.m_MuscleClip.m_Clip.data
    dense = data.m_DenseClip
    curves = [[] for _ in range(data.m_StreamedClip.curveCount)]
    raw = np.asarray(data.m_StreamedClip.data, dtype="<u4").tobytes()
    offset = 0
    while offset < len(raw):
        time, count = struct.unpack_from("<fI", raw, offset)
        offset += 8
        for _ in range(count):
            index, a, b, c, d = struct.unpack_from("<I4f", raw, offset)
            offset += 20
            if 0 <= time < float("inf"):
                curves[index].append((time, a, b, c, d))
    columns = []
    for keys in curves:
        if not keys:
            columns.append(np.zeros(len(times)))
            continue
        keys = np.array(keys)
        t, a, b, c, d = keys[np.maximum(0, np.searchsorted(keys[:, 0], times, side="right") - 1)].T
        dt = times - t
        columns.append(((a * dt + b) * dt + c) * dt + d)
    if dense.m_CurveCount:
        samples = np.array(dense.m_SampleArray).reshape(-1, dense.m_CurveCount)
        frames = np.clip((times - dense.m_BeginTime) * dense.m_SampleRate, 0, len(samples) - 1)
        left = frames.astype(int)
        blend = (frames - left)[:, None]
        columns.extend((samples[left] * (1 - blend) + samples[np.minimum(left + 1, len(samples)-1)] * blend).T)
    columns.extend(np.full(len(times), v) for v in data.m_ConstantClip.data)
    values = np.array(columns).T
    offset = 0
    result = defaultdict(dict)
    for binding in clip.m_ClipBindingConstant.genericBindings:
        transform = binding.typeID == 4 and binding.attribute in (1, 2, 3, 4)
        if not transform and not include_properties:
            raise ValueError("Unsupported animation binding in " + clip.m_Name)
        count = (4 if binding.attribute == 2 else 3) if transform else 1
        if transform or include_properties:
            result[binding.path][binding.attribute] = values[:, offset:offset+count]
        offset += count
    if offset != values.shape[1]:
        raise ValueError("Unexpected channel count in " + clip.m_Name)
    return result


class CapitalPose:
    """Sample both PhaseAll animators in their own hierarchy, including the skinned koi."""
    frames = 24

    def __init__(self, env):
        self.channels = defaultdict(dict)
        self.cache = {}
        transforms = [o.read() for o in env.objects if o.type.name == "Transform"]
        paths = {}
        for t in transforms:
            names = [t.m_GameObject.read().m_Name]
            parent = t
            while parent.m_Father.path_id:
                parent = parent.m_Father.read()
                names.insert(0, parent.m_GameObject.read().m_Name)
            paths[t.object_reader.path_id] = names
        for o in env.objects:
            if o.type.name != "AnimationClip" or o.peek_name() not in ("Phase1", "PhaseAll"):
                continue
            clip = o.read()
            if clip.m_Name == "Phase1":
                times = np.array([0.0])
                root = "duelfield"
            else:
                self.duration = clip.m_MuscleClip.m_StopTime
                times = np.arange(self.frames) * self.duration / self.frames
                root = None
            # Identify the animator root by matching every clip path; near/far files
            # contain distinct PhaseAll clips with identical names.
            bindings = clip_samples(clip, times)
            for tid, names in paths.items():
                if root and names[-1] != root:
                    continue
                candidates = {zlib.crc32("/".join(p[len(names):]).encode()): k
                              for k, p in paths.items() if p[:len(names)] == names}
                if bindings.keys() <= candidates.keys():
                    for key, channels in bindings.items():
                        self.channels[candidates[key]].update(channels)
                    break
            else:
                raise ValueError("Missing animator for " + clip.m_Name)

    def matrices(self, t):
        tid = t.object_reader.path_id
        if tid not in self.cache:
            base = self.matrices(t.m_Father.read()) if t.m_Father.path_id else np.eye(4)[None]
            ch = self.channels[tid]
            q = t.m_LocalRotation
            arrays = [ch.get(1, xyz(t.m_LocalPosition)[None]),
                      ch.get(2, np.array([[q.x, q.y, q.z, q.w]])),
                      ch.get(3, xyz(t.m_LocalScale)[None])]
            count = max(map(len, arrays))
            positions, rotations, scales = [np.broadcast_to(a, (count, a.shape[1])) for a in arrays]
            local = [local_matrix(p, r / np.linalg.norm(r), s) for p, r, s in zip(positions, rotations, scales)]
            self.cache[tid] = base @ local
        return self.cache[tid]

    def skin(self, renderer, mesh, basis):
        bind = renderer.m_Mesh.read().m_BindPose
        weights = np.array(mesh.m_BoneWeights)
        ids = np.array(mesh.m_BoneIndices)
        verts = np.column_stack((np.asarray(mesh.m_Vertices)[:, :3], np.ones(len(weights))))
        points = np.zeros((self.frames, len(verts), 3))
        normals = np.zeros((len(verts), 3))
        for i, bone in enumerate(renderer.m_Bones):
            pose = bind[i]
            inverse = np.array([[getattr(pose, f"e{r}{c}") for c in range(4)] for r in range(4)])
            mats = basis @ self.matrices(bone.read()) @ inverse
            w = np.sum(weights * (ids == i), axis=1)[:, None]
            points += (verts @ mats[:, :3, :].transpose(0, 2, 1)) * w
            normals += (np.asarray(mesh.m_Normals)[:, :3] @ np.linalg.inv(mats[0, :3, :3])) * w
        return points, normals

def align_ruins_tray(points, normals, uv, triangles):
    # Each half has a raised card tray welded into the same mesh as the playing surface.
    # Start on its unique highest vertex and walk its connected component across UV seams.
    coords, ids = np.unique(points.round(4), axis=0, return_inverse=True)
    edges = defaultdict(set)
    for a, b, c in ids[np.asarray(triangles)]:
        edges[a].update((b, c))
        edges[b].update((a, c))
        edges[c].update((a, b))
    pending = [int(coords[:, 1].argmax())]
    tray = set()
    while pending:
        i = pending.pop()
        if i not in tray:
            tray.add(i)
            pending.extend(edges[i] - tray)
    center = coords[list(tray)].mean(axis=0)
    if abs(center[0]) < 5 or abs(center[2]) < 4:
        raise ValueError("Could not locate the raised card tray")
    tray_indices = np.flatnonzero(np.isin(ids, list(tray)))
    # Keep a recessed cap in the old socket. Moving just the tray leaves a hole
    # through the platform when viewed from above.
    cap = points[tray_indices].copy()
    cap[:, 1] -= 0.12
    faces = np.asarray(triangles)
    cap_faces = faces[np.isin(faces[:, 0], tray_indices)]
    cap_faces = np.searchsorted(tray_indices, cap_faces) + len(points)
    points[tray_indices, 2] -= np.copysign(1.33, center[2])
    return (np.vstack((points, cap)), np.vstack((normals, normals[tray_indices])),
            np.vstack((uv, uv[tray_indices])), np.vstack((faces, cap_faces)))


class Field:
    def __init__(self, assets, theme):
        self.theme = theme
        self.catalog = theme.isdecimal()
        self.assets = assets / "assets/mdpromod"
        self.textures = {}
        self.sections = defaultdict(list)
        self.elements = []
        self.groups = []
        self.vents = []
        self.vertex_loops = {}
        self.billboard_origins = {}

    def stable_id(self, key):
        return str(uuid.uuid5(uuid.NAMESPACE_URL, "mdpromod/" + self.theme + "/" + key))

    def texture(self, pointer, style="base"):
        source = pointer.read()
        key = source.m_Name.lower() + ("" if style == "base" else "_" + style)
        if key in self.textures:
            return key
        # These base-color maps were authored in linear space for the Unity lit shader.
        im = source.image.convert("RGBA")
        im.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
        rgb = np.asarray(im).copy()
        linear = rgb[:, :, :3] / 255.0
        rgb[:, :, :3] = np.rint(255 * np.where(linear <= 0.0031308, linear * 12.92,
                                               1.055 * linear**(1/2.4) - 0.055))
        if self.catalog or self.theme in ("ruins", "capital"):
            # The base-color textures of these fields already contain the authored stone colors.
            rgb = np.asarray(im).copy()
        if style in ("melt", "crack", "rune"):
            # The volcano shader packs noise and detail masks into separate channels.
            channels = np.asarray(im, dtype=float) / 255
            heat = np.clip((channels[:, :, 0] - 0.25) * 1.8, 0, 1)
            for i, ramp in enumerate(((105, 238, 255), (16, 65, 219), (6, 9, 80))):
                rgb[:, :, i] = np.interp(heat, (0, 0.47, 1), ramp)
            if style == "melt":
                rgb[:, :, 3] = 255
            elif style == "rune":
                rgb[:, :, :3] = [255, 190, 255]
                rgb[:, :, 3] = np.rint(255 * channels[:, :, 0] * channels[:, :, 3])
        if style == "glow":
            # Forge's additive eyes shader uses ONE/ONE blending. Bake the original opacity mask into RGB.
            rgb[:, :, :3] = np.rint(rgb[:, :, :3] * (rgb[:, :, 3:4] / 255))
        return self.store_texture(key, Image.fromarray(rgb))

    def store_texture(self, key, image):
        im = image.quantize(colors=256, method=Image.Quantize.FASTOCTREE)
        buf = io.BytesIO()
        im.save(buf, format="PNG", optimize=True)
        png = buf.getvalue()
        file = self.assets / "textures/field" / self.theme / (key + ".png")
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_bytes(png)
        self.textures[key] = dict(id=str(len(self.textures)), name=file.name, uuid=self.stable_id(key),
                                  width=im.width, height=im.height, uv_width=16, uv_height=16,
                                  internal=True, source="data:image/png;base64," + base64.b64encode(png).decode())
        return key

    def sigil_texture(self, material, color):
        srgb = tuple(round(255 * (1.055 * c**(1/2.4) - 0.055)) if c > 0.0031308 else round(255 * 12.92 * c)
                     for c in (color.r, color.g, color.b))
        key = "sigil_" + bytes(srgb).hex()
        if key not in self.textures:
            mask = dict(material.m_SavedProperties.m_TexEnvs)["_Texture2D"].m_Texture.read().image.convert("RGBA")
            img = Image.new("RGBA", mask.size, srgb + (255,))
            img.putalpha(mask.getchannel("A"))
            self.store_texture(key, img)
        return key

    def ocean_textures(self, material):
        if "ocean_water" in self.textures:
            return
        slots = dict(material.m_SavedProperties.m_TexEnvs)
        colors = dict(material.m_SavedProperties.m_Colors)
        def channel_map(slot):
            img = slots[slot].m_Texture.read().image.convert("RGB").resize((256, 256), Image.Resampling.BILINEAR)
            return np.asarray(img, dtype=float) / 255
        def color(slot):
            c = colors[slot]
            return np.array([c.r, c.g, c.b])
        normal = channel_map("_MainNormal")
        detail = channel_map("_SubNormal")
        foam = channel_map("_FoamTexture")[:, :, 0]
        # Light the two original tangent-space normal maps at different angles. Keep the
        # second layer translucent so the independently moving ripples cross each other.
        slope = np.clip(0.46 + 2.5 * (normal[:, :, 1] - normal[:, :, 0]), 0, 1)
        deep = 0.67 * color("_FresnelColor") + np.array([0.0, 0.013, 0.07])
        shallow = 0.42 * color("_WaterBottom") + 0.58 * color("_WaterMain")
        rgb = deep + (shallow - deep) * slope[:, :, None]
        img = np.empty((256, 256, 4), dtype=np.uint8)
        img[:, :, :3] = np.rint(255 * rgb)
        img[:, :, 3] = 105 if self.theme == "capital" else 215
        self.store_texture("ocean_water", Image.fromarray(img))
        crest = np.clip((detail[:, :, 0] - detail[:, :, 1]) * 6.0, 0, 1)
        highlight = color("_HighLightColor") if "_HighLightColor" in colors else color("_WaterMain")
        img[:, :, :3] = np.rint(255 * highlight)
        img[:, :, 3] = np.rint(120 * np.clip(crest * (0.35 + 0.9 * foam), 0, 1))
        self.store_texture("ocean_foam", Image.fromarray(img))

    def collect(self, folder, bundle, parent):
        env = UnityPy.load(str(folder / bundle))
        gears = gear_curves(env) if self.theme == "geartown" and bundle.startswith("Mat_") else {}
        rest_pose = ruins_start_pose(env) if self.theme == "ruins" and bundle.startswith("Mat_") else world_matrix
        capital = CapitalPose(env) if self.theme == "capital" and bundle.startswith("Mat_") else None
        anchors = {}
        children = []
        group_id = self.stable_id(bundle)
        for obj in env.objects:
            if obj.type.name == "Transform":
                t = obj.read()
                name = t.m_GameObject.read().m_Name
                if name.startswith("POS_"):
                    anchors[name] = parent @ world_matrix(t)
                if self.theme == "volcano" and name.startswith(("fxp_BG_bbl_001_", "fxp_BG_smk_001_")):
                    if name.rsplit("_", 1)[-1].isdigit() and t.m_Father.path_id:
                        # Only the continuous ambient groups, outside the damage-animation hierarchy.
                        group = t.m_Father.read().m_GameObject.read().m_Name
                        if group == name.rsplit("_", 1)[0] + "_" + bundle.split("_")[-1]:
                            matrix = parent @ world_matrix(t)
                            kind = 1 if "_bbl_" in name else 2
                            self.vents.append((kind, *matrix[:3, 3], 0, 1, 0, 5, 0))
                if self.theme == "ruins" and name.startswith("fxp_BG_splash_") and t.m_Father.path_id:
                    group = t.m_Father.read().m_GameObject.read().m_Name
                    if group in ("fxp_BG_wave_near", "fxp_BG_wave_far_01"):
                        matrix = parent @ world_matrix(t)
                        self.vents.append((3, *matrix[:3, 3], 0, 1, 0, 5, 0))
            if capital and obj.type.name == "ParticleSystem":
                ps = obj.read()
                go = ps.m_GameObject.read()
                if go.m_Name == "petal" and ps.looping and ps.EmissionModule.enabled:
                    t = next(c.component.read() for c in go.m_Component if c.component.type.name == "Transform")
                    mat = parent @ world_matrix(t)
                    self.vents.append((4, *mat[:3, 3], *mat[:3, 2], 2, 0))
            if self.theme == "geartown" and obj.type.name == "ParticleSystem" and bundle.startswith("Mat_"):
                ps = obj.read()
                go = ps.m_GameObject.read()
                if go.m_Name.startswith("fxp_BG_smoke_") and ps.EmissionModule.enabled:
                    transform = next(c.component.read() for c in go.m_Component if c.component.type.name == "Transform")
                    ancestor = transform
                    while ancestor.m_Father.path_id:
                        ancestor = ancestor.m_Father.read()
                        if ancestor.m_GameObject.read().m_Name in ("fxp_smoke", "fxp_smoke_" + bundle.split("_")[-1]):
                            matrix = parent @ world_matrix(transform)
                            self.vents.append((*matrix[:3, 3], *matrix[:3, 2], ps.lengthInSec, ps.startDelay.scalar))
                            break
            # The permanent gold/violet markings use stationary mesh particles (one
            # instance per emitter) rather than regular MeshRenderers in the prefab.
            sigil = self.theme == "ruins" and obj.type.name == "ParticleSystemRenderer"
            koi = capital and obj.type.name == "SkinnedMeshRenderer"
            if obj.type.name != "MeshRenderer" and not (sigil or koi):
                continue
            renderer = obj.read()
            go = renderer.m_GameObject.read()
            name = go.m_Name
            if not renderer.m_Enabled or not go.m_IsActive or ("destroy" in name.lower() and self.theme != "ruins") or name == "shadow":
                continue
            comps = {c.component.type.name: c.component for c in go.m_Component}
            materials = [ref.read() for ref in renderer.m_Materials if ref.path_id]
            if sigil:
                ps = comps["ParticleSystem"].read()
                if not (ps.looping and ps.EmissionModule.enabled and renderer.m_Mesh.path_id
                        and materials and materials[0].m_Name == "fxm_BG_ holly_03"):
                    continue
            window = name.startswith("Window_light")
            magma = self.theme == "volcano" and all(mat.m_Name in ("fxm_BG_mgm_001", "fxm_BG_mgm_002") for mat in materials)
            ocean = self.theme == "ruins" and name.startswith("ocean_")
            pond = capital and name == "water"
            wind_materials = {"fxm_BG_branch_001": "Texture2D_23E1EE5", "fxm_BG_plants_001": "Texture2D_AC0C813C",
                              "fxm_BG_flag_001_near": "Texture2D_6A3FD55F", "fxm_BG_flag_001_far": "Texture2D_6A3FD55F"}
            foliage = capital and materials and materials[0].m_Name in wind_materials
            backing = self.theme == "ruins" and name.startswith("black_")
            if not materials or not (sigil or ocean or pond or foliage or backing or window or magma or all(mat.m_Name.startswith("MT_") for mat in materials)):
                continue
            mesh = MeshHandler((renderer.m_Mesh if sigil or koi else comps["MeshFilter"].read().m_Mesh).read())
            mesh.process()
            transform = comps["Transform"].read()
            matrices = parent @ capital.matrices(transform) if capital else None
            matrix = matrices[0].copy() if capital else parent @ rest_pose(transform)
            if abs(np.linalg.det(matrix[:3, :3])) < 1e-10:
                continue
            if self.theme in ("volcano", "capital") and name == "deck":
                # The Unity trays are centered at Z +/-5.33; gameplay uses +/-4.
                matrix[2, 3] -= 1.33 if bundle.endswith("_near") else -1.33
            curve = gears.get(zlib.crc32(name.encode()))
            motion = ()
            if curve is not None:
                matrix, motion = gear_pose(transform, parent, curve)
            points = np.asarray(mesh.m_Vertices)[:, :3] @ matrix[:3, :3].T + matrix[:3, 3]
            if mesh.m_Normals is None:
                normals = np.zeros_like(points)
                triangles = np.concatenate(mesh.get_triangles())
                a, b, c = points[triangles].transpose(1, 0, 2)
                face_normals = np.cross(b - a, c - a) * np.sign(np.linalg.det(matrix[:3, :3]))
                for corner in triangles.T:
                    np.add.at(normals, corner, face_normals)
            else:
                normals = np.asarray(mesh.m_Normals)[:, :3] @ np.linalg.inv(matrix[:3, :3])
            loop = None
            if koi:
                sequence, normals = capital.skin(renderer, mesh, parent)
                points = sequence[0]
                loop = sequence - points
                matrix = matrix.copy()
                matrix[:3, 3] = points.mean(axis=0)
            elif capital and len(matrices) > 1:
                sequence = np.asarray(mesh.m_Vertices)[:, :3] @ matrices[:, :3, :3].transpose(0, 2, 1) + matrices[:, None, :3, 3]
                loop = sequence - points
            elif capital and name.startswith("Flag_"):
                # The flag shader uses its vertex colors to pin the pole edge.
                weight = np.asarray(mesh.m_Colors)[:, 0] / 255.0
                phase = np.arange(capital.frames)[:, None] * (2*np.pi/capital.frames)
                wave = np.sin(phase + points[:, 1] * 3) - np.sin(points[:, 1] * 3)
                loop = (0.085 * wave * weight)[:, :, None] * np.array([0.45, 0, 0.89])
            if loop is not None and np.max(np.abs(loop)) > 0.0005:
                motion = (bundle, obj.path_id)
                self.vertex_loops[motion] = (capital.duration, loop)
            normals /= np.maximum(np.linalg.norm(normals, axis=1)[:, None], 1e-10)
            uv = np.zeros((len(points), 2)) if mesh.m_UV0 is None else np.asarray(mesh.m_UV0)[:, :2].copy()
            uv[:, 1] = 1 - uv[:, 1]
            submeshes = mesh.get_triangles()
            if self.theme == "ruins" and name in ("field_near_L", "field_near_R", "field_far_L", "field_far_R"):
                points, normals, uv, faces = align_ruins_tray(points, normals, uv, submeshes[0])
                submeshes = [faces.tolist()]
            if capital and name == "deck":
                # Keep the original slab tops as recessed covers in the old openings.
                # The ornamental hardware belongs only on the moved trays.
                tris = np.asarray(submeshes[0])
                cap_faces = tris[np.all(np.isclose(points[tris, 1], 0.3203, atol=0.001), axis=1)]
                if not len(cap_faces):
                    raise ValueError("Cannot find the original deck covers")
                base_faces = tris
                for direction in (-1, 1):
                    faces = cap_faces[direction * points[cap_faces[:, 0], 0] > 0]
                    # The slotted top planks are not a continuous lid. Use the
                    # broadest plank's UVs on a solid inset, below the courtyard.
                    corners = points[faces]
                    areas = np.linalg.norm(np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0]), axis=1)
                    face = faces[areas.argmax()]
                    x = direction * 6.2
                    z = 5.4 if bundle.endswith("_near") else -5.4
                    cap = np.array([[x-1.75, 0.13, z-1.65], [x+1.75, 0.13, z-1.65],
                                    [x+1.75, 0.13, z+1.65], [x-1.75, 0.13, z+1.65]])
                    low, high = uv[face].min(axis=0), uv[face].max(axis=0)
                    cap_uv = low + (high-low) * np.array([[0.3, 0.3], [0.7, 0.3], [0.7, 0.7], [0.3, 0.7]])
                    cap_indices = np.array([[0, 1, 2], [0, 2, 3]]) + len(points)
                    points = np.vstack((points, cap))
                    normals = np.vstack((normals, np.tile([0, 1, 0], (4, 1))))
                    uv = np.vstack((uv, cap_uv))
                    base_faces = np.vstack((base_faces, cap_indices))
                submeshes = [base_faces.tolist()]
            if ocean and name.endswith("_side"):
                # Unity supplies a deep-water background behind the shore cutouts. Close
                # the bottom of the exported water volume so those gaps show depth in Forge.
                left, bottom, front = points.min(axis=0)
                right, _, back = points.max(axis=0)
                offset = len(points)
                points = np.vstack((points, [[left, bottom, front], [right, bottom, front],
                                             [right, bottom, back], [left, bottom, back]]))
                normals = np.vstack((normals, np.tile([0, 1, 0], (4, 1))))
                uv = np.vstack((uv, np.zeros((4, 2))))
                bottom_faces = np.array([[0, 1, 2], [0, 2, 3]]) + offset
                submeshes = [np.vstack((submeshes[0], bottom_faces)).tolist()]
            if sigil:
                # A small depth offset keeps the original coplanar light meshes visible in Forge.
                points += 0.007 * normals
            # Blockbench stores per-corner UVs; weld positions so adjacent faces stay editable.
            verts, unique, keys, faces = {}, {}, [], {}
            for p in points:
                point = tuple(float(v) for v in np.round((p - matrix[:3, 3])*16, 4))
                key = unique.setdefault(point, f"v{len(unique):x}")
                keys.append(key)
                verts[key] = point
            for tris, material in zip(submeshes, materials, strict=True):
                if not tris:
                    continue
                kind = (2 if material.m_Name.endswith("_01") else 3) if window else (1 if motion else 0)
                style = "glow" if window else "base"
                slot = "_Texture2D"
                if capital and motion:
                    kind = 9 if name.startswith("chochin") else 8
                    if material.m_Name == "fxm_BG_branch_001":
                        kind = 10
                        self.billboard_origins[motion] = matrix[:3, 3]
                if foliage:
                    slot = wind_materials[material.m_Name]
                if magma:
                    if material.m_Name.endswith("_001"):
                        kind, style, slot = 4, "melt", "Texture2D_DAD68811"
                    else:
                        kind, style, slot = 5, "crack", "Texture2D_7536E495"
                elif self.theme == "volcano" and name.startswith(("MagicCircle_", "Bloom_")):
                    kind = 5
                    style = "rune" if name.startswith("MagicCircle_") else "crack"
                    slot = "Texture2D_7536E495"
                tri = np.asarray(tris, dtype=int)
                if np.linalg.det(matrix[:3, :3]) < 0:
                    tri = tri[:, ::-1]
                # Apply both Unity tiling and offset before mapping the V axis to PNG rows.
                if sigil:
                    kind, tex = 5, self.sigil_texture(material, ps.InitialModule.startColor.maxColor)
                    uv2 = uv
                elif pond or ocean and not name.endswith("_side"):
                    self.ocean_textures(material)
                    kind, tex = 6, "ocean_water"
                    uv2 = uv * ([1.5, 1.5] if pond else [5, 4])
                    self.sections["ocean_foam", 7, ()].append((points, normals, uv * ([2, 2] if pond else [7, 5]), tri))
                elif ocean or backing:
                    tex = "ocean_side" if ocean else "underlay"
                    c = material.m_SavedProperties.m_Colors[0][1]
                    if tex not in self.textures:
                        # Solid shader parameters have no corresponding Unity texture.
                        self.store_texture(tex, Image.new("RGBA", (2, 2), tuple(round(255 * v) for v in (c.r, c.g, c.b, 1))))
                    uv2 = uv
                else:
                    texenv = dict(material.m_SavedProperties.m_TexEnvs)[slot]
                    tex = self.texture(texenv.m_Texture, style)
                    uv2 = uv * [texenv.m_Scale.x, texenv.m_Scale.y]
                    uv2 += [texenv.m_Offset.x, 1 - texenv.m_Scale.y - texenv.m_Offset.y]
                self.sections[tex, kind, motion].append((points, normals, uv2, tri))
                for triangle in tri:
                    ids = [keys[i] for i in triangle]
                    if len(set(ids)) != 3:
                        continue
                    coords = uv2[triangle]
                    coords = coords - np.floor(np.min(coords, axis=0) + 1e-6)
                    faces[f"f{len(faces):x}"] = dict(vertices=ids, uv=dict(zip(ids, (coords*16).round(5).tolist())),
                                                      texture=int(self.textures[tex]["id"]))
            eid = self.stable_id(bundle + "/" + str(obj.path_id))
            self.elements.append(dict(uuid=eid, type="mesh", name=name,
                                      origin=(matrix[:3, 3]*16).round(4).tolist(), vertices=verts, faces=faces))
            children.append(eid)
        self.groups.append((dict(uuid=group_id, name=bundle, origin=[0, 0, 0]), children))
        return anchors

    def save(self, project):
        folder = self.assets / "field" / self.theme
        folder.mkdir(parents=True, exist_ok=True)
        typed_vents = self.catalog or self.theme in ("volcano", "ruins", "capital")
        # Static materials stay batched, while each moving gear retains its pivot.
        with (folder / "scene.bin").open("wb") as output:
            format_id = b"GTF5" if self.catalog else b"GTF4" if self.theme == "capital" else b"GTF3" if typed_vents else b"GTF2"
            output.write(format_id + struct.pack(">H", len(self.sections)))
            output.write(struct.pack(">3f", *np.diag(self.basis)[:3]))
            sections = self.sections.items()
            if self.catalog or self.theme in ("ruins", "capital"):
                # Draw opaque ruins first, then the water and its surface highlights.
                sections = sorted(sections, key=lambda item: (item[0][1] in (6, 7), item[0][1]))
            for (key, kind, motion), pieces in sections:
                vertices = {}
                indices = []
                offsets = []
                for points, normals, uv, triangles in pieces:
                    vertex_ids = {}
                    for vi in np.unique(triangles):
                        pos = np.rint(points[vi] * (128 if self.catalog else 1024)).astype(int)
                        normal = np.rint(normals[vi]*127).astype(int)
                        data = struct.pack(">3h2f3b", *pos, *uv[vi], *normal)
                        if kind in (8, 9, 10):
                            # Vertices sharing a UV seam may have different skinning weights.
                            delta = np.rint(self.vertex_loops[motion][1][:, vi, :] * (256 if self.catalog else 4096))
                            if np.max(np.abs(delta)) > 32767:
                                raise ValueError("Vertex motion outside the quantization range")
                            delta = delta.astype(">i2").tobytes()
                            data = (data, delta)
                            if data not in vertices:
                                offsets.append(delta)
                        vertex_ids[vi] = vertices.setdefault(data, len(vertices))
                    indices.extend(vertex_ids[i] for i in triangles.flat)
                keybytes = key.encode("ascii")
                output.write(struct.pack(">H", len(keybytes)) + keybytes)
                output.write(bytes([kind]))
                if kind == 1:
                    output.write(struct.pack(">7f", *motion))
                output.write(struct.pack(">II", len(vertices), len(indices)))
                output.write(b"".join(key[0] for key in vertices) if offsets else b"".join(vertices))
                assert max(indices) < 65536
                output.write(np.array(indices, dtype=">u2").tobytes())
                if kind == 10:
                    output.write(struct.pack(">3f", *self.billboard_origins[motion]))
                if offsets:
                    duration, loop = self.vertex_loops[motion]
                    output.write(struct.pack(">fB", duration, len(loop)))
                    # Frame-major storage makes interpolation a single linear pass.
                    data = np.frombuffer(b"".join(offsets), dtype=">i2").reshape(len(offsets), len(loop), 3)
                    output.write(data.transpose(1, 0, 2).tobytes())
            output.write(struct.pack(">H", len(self.vents)))
            for vent in sorted(self.vents):
                output.write(struct.pack(">B8f" if typed_vents else ">8f", *vent))
        model = dict(meta=dict(format_version="5.0", model_format="free", box_uv=False),
                     name=getattr(self, "title", None) or {"ruins": "World Legacy Ruins", "capital": "Foreign Capital"}.get(self.theme, self.theme.title()), resolution=dict(width=16, height=16), elements=self.elements,
                     groups=[g for g, _ in self.groups],
                     outliner=[dict(uuid=g["uuid"], children=children) for g, children in self.groups],
                     textures=list(self.textures.values()))
        project.parent.mkdir(parents=True, exist_ok=True)
        project.write_text(json.dumps(model, separators=(",", ":")), encoding="utf-8")
        print(len(self.elements), "editable objects,", sum(len(e["faces"]) for e in self.elements),
              "faces,", len(self.textures), "textures")


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    for arg in ("installation", "assets", "project"):
        ap.add_argument(arg, type=Path)
    ap.add_argument("--theme", choices=("geartown", "volcano", "ruins", "capital"), default="geartown")
    args = ap.parse_args()
    root = args.installation / "MDPro3_Data/StandaloneWindows64/MasterDuel"
    field = Field(args.assets, args.theme)
    number = {"geartown": "004", "volcano": "005", "ruins": "006", "capital": "007"}[args.theme]
    # 8.6 Unity units between monster columns; the mod spaces columns two blocks apart.
    basis = np.diag([2/8.6, 0.22, -2/9.0, 1.0])
    field.basis = basis
    for side in ("near", "far"):
        anchors = field.collect(root/"Mat", f"Mat_{number}_{side}", basis)
        for part in ("Grave", "AvatarStand"):
            transform = anchors[f"POS_{part}_{side}"].copy()
            if part == "Grave":
                # Match the mod's fixed graveyard column and recess the raised original socket.
                direction = 1 if side == "near" else -1
                transform[:3, 3] = [6*direction, -0.56, 2*direction]
                transform[2, :3] *= 2 / (6.24 * (2/9.0))
            field.collect(root/part, f"{part}_{number}_{side}", transform)
    field.save(args.project)


if __name__ == "__main__":
    main()
