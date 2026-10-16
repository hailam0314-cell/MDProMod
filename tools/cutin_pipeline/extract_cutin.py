r"""把一套 MonsterCutin 解成"引擎无关"的渲染素材，按 prefab 的引用链走。

一个 cutin 目录里经常堆着好几套 rig（89631139 就掺进了一个和青眼无关的角色），
所以不能把目录里所有资源都 dump：真正的入口是

   预制体上的 PlayableDirector -> TimelineAsset
       -> SpineAnimationStateTrack 的 clip -> AnimationReferenceAsset
          -> animationName + SkeletonDataAsset

产出（默认 D:\MDProMod\runtime\cutin\<code>\）：
  cutin.json     清单：动画名、时长、skeleton/atlas 文件名、图集分页、pma、scale、相机
  <骨架>.json    Spine 骨架文本
  <图集>.atlas   Spine 图集文本
  <分页>.png     图集分页贴图（文件名照 atlas 里的引用，尺寸按 atlas 的 size: 匹配）

用法：python tmp\extract_cutin.py 89631139 [更多卡号...]
      python tmp\extract_cutin.py --raw 89631139     （附带把目录里其它 rig 也 dump 出来）
"""
import json
import os
import re
import sys
from pathlib import Path

_local_project = Path(__file__).resolve().parents[2]
PROJECT_ROOT = Path(os.environ.get("MDPROMOD_PROJECT", str(_local_project)
                                  if (_local_project / "mod" / "build.gradle").is_file()
                                  else r"D:\MDProMod"))
sys.path.insert(0, str(PROJECT_ROOT / "tools" / "unitypy"))
import UnityPy  # noqa: E402

ROOT = os.environ.get("MDPRO_CUTIN_SOURCE", r"D:\MyCardLibrary\mdpro3\MDPro3_Data\StandaloneWindows64\MonsterCutin")
OUT_ROOT = str(PROJECT_ROOT / "runtime" / "cutin")

SIZE_RE = re.compile(r"^\s*size\s*:\s*(\d+)\s*,\s*(\d+)\s*$")


def text_of(pptr):
    """PPtr(TextAsset) -> bytes"""
    if pptr is None:
        return None
    data = pptr.read()
    raw = data.m_Script
    if isinstance(raw, str):
        raw = raw.encode("utf-8", "surrogateescape")
    return raw


def name_of(pptr):
    if pptr is None:
        return None
    try:
        return getattr(pptr.read(), "m_Name", None)
    except Exception:
        return None


def reader_key(reader):
    """Return a stable identity for one Unity object within the loaded environment."""
    assets_file = getattr(reader, "assets_file", None)
    file_name = getattr(assets_file, "name", None)
    if not file_name:
        file_name = id(assets_file)
    return (file_name, getattr(reader, "path_id", None))


def pptr_key(pptr):
    """Resolve a PPtr to the identity of its target object."""
    if pptr is None:
        return None
    try:
        target = pptr.deref()
        return reader_key(target) if target is not None else None
    except Exception:
        return None


def atlas_file_name(atlas_ptr, text):
    """Get the atlas filename, tolerating stripped Unity TextAsset names."""
    name = name_of(atlas_ptr)
    if name:
        return name
    if text:
        first = text.decode("utf-8", "replace").splitlines()[0].strip()
        if first.lower().endswith((".atlas", ".txt")):
            return first
        if first:
            return first.rsplit("/", 1)[-1].rsplit("\\", 1)[-1].rsplit(".", 1)[0] + ".atlas"
    return None


def script_name(mono):
    try:
        return mono.m_Script.read().m_Name
    except Exception:
        return ""


def vector3_of(value):
    """UnityPy 的 Vector3f -> [x, y, z]"""
    if value is None:
        return None
    try:
        return [float(value.x), float(value.y), float(value.z)]
    except AttributeError:
        try:
            return [float(value[0]), float(value[1]), float(value[2])]
        except Exception:
            return None


def atlas_pages(text):
    """atlas 文本 -> [(分页文件名, 宽, 高)]，按出现顺序。"""
    pages = []
    current = None
    for line in text.splitlines():
        stripped = line.strip()
        size = SIZE_RE.match(line)
        if size:
            if current:
                pages.append((current, int(size.group(1)), int(size.group(2))))
            current = None
            continue
        if stripped and not stripped.startswith(("pma:", "filter:", "scale:", "repeat:",
                                                 "format:", "rotate:")) and ":" not in stripped:
            if stripped.lower().endswith((".png", ".jpg", ".jpeg")):
                current = stripped
    return pages


def atlas_scalars(text):
    values = {}
    for line in text.splitlines():
        stripped = line.strip()
        for key in ("pma:", "filter:", "scale:"):
            if stripped.startswith(key):
                values[key.rstrip(":")] = stripped.split(":", 1)[1].strip()
    return values


def validate_regions(text):
    """Reject corrupt atlas coordinates before preserving their logical size."""
    sizes = {name: (width, height) for name, width, height in atlas_pages(text)}
    current_page = None
    region = None
    count = 0

    def check():
        nonlocal count
        if region is None or "xy" not in region or "size" not in region:
            return
        x, y = region["xy"]
        width, height = region["size"]
        if region.get("rotate", "false") in ("true", "90", "270"):
            width, height = height, width
        page_width, page_height = sizes[current_page]
        if min(x, y, width, height) < 0 or x + width > page_width or y + height > page_height:
            raise ValueError(f"atlas region outside logical page: {current_page}/{region['name']} "
                             f"{(x,y,width,height)} vs {(page_width,page_height)}")
        count += 1

    for line in text.splitlines():
        value = line.strip()
        if not value:
            continue
        if ":" not in value:
            check()
            if value in sizes:
                current_page, region = value, None
            elif current_page:
                region = {"name": value}
        elif region is not None:
            key, raw = (part.strip() for part in value.split(":", 1))
            if key in ("xy", "size"):
                region[key] = tuple(int(v.strip()) for v in raw.split(","))
            elif key == "bounds":
                x, y, width, height = [int(v.strip()) for v in raw.split(",")]
                region.update(xy=(x, y), size=(width, height))
            elif key == "rotate":
                region[key] = raw
    check()
    if not count:
        raise ValueError("atlas contains no verifiable regions")
    return count


def imported_texture_scaling(logical, physical):
    """Only accept recognizable Unity importer resizing on a unique binding."""
    width, height = logical
    actual_width, actual_height = physical
    if actual_width * height == actual_height * width:
        ratio = width / actual_width
        # Unity's max-size import can lower any page by whole mip levels.
        if ratio >= 1 and ratio.is_integer() and int(ratio) & (int(ratio) - 1) == 0:
            return "uniform_mip_downscale"
    def nearest_power_two(value):
        lower = 1 << (value.bit_length() - 1)
        upper = lower * 2
        return lower if value - lower <= upper - value else upper
    if (actual_width, actual_height) == (nearest_power_two(width), nearest_power_two(height)):
        return "unity_npot_to_nearest"
    return None


def collect_textures(env):
    """Texture2D -> {Unity object identity: image record}.

    Texture names are not unique in MonsterCutin.  Keeping the object identity
    is essential because SpineAtlasAsset materials point at a particular
    Texture2D variant.
    """
    found = {}
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        data = obj.read()
        found[reader_key(obj)] = {
            "name": data.m_Name,
            "width": data.m_Width,
            "height": data.m_Height,
            # Decode only material-bound pages selected for the real rig.
            "texture": data,
        }
    return found


def spine_tracks(env):
    """找 prefab 的 PlayableDirector -> TimelineAsset -> 每条 Spine 轨道。

    返回 [(skeleton_text_asset_name, animation_name, duration)]，以及相机信息。
    """
    timelines = {}
    directors = []
    animation_refs = {}

    for obj in env.objects:
        kind = obj.type.name
        if kind == "PlayableDirector":
            data = obj.read()
            timelines[id(data)] = data
            directors.append((reader_key(obj), data))
        elif kind == "MonoBehaviour":
            data = obj.read()
            script = script_name(data)
            if script == "TimelineAsset":
                timelines.setdefault("asset:" + (data.m_Name or ""), data)
            elif script == "AnimationReferenceAsset":
                animation_refs[id(data)] = data

    # PlayableDirector.m_PlayableAsset -> TimelineAsset
    picked = []
    for director_key, director in directors:
        asset = director.m_PlayableAsset
        try:
            timeline = asset.read()
        except Exception:
            continue
        tracks = getattr(timeline, "m_Tracks", None) or []
        for track_ref in tracks:
            try:
                track = track_ref.read()
            except Exception:
                continue
            if script_name(track) != "SpineAnimationStateTrack":
                continue
            if getattr(track, "m_Muted", False):
                continue
            track_key = pptr_key(track_ref)
            bindings = {pptr_key(binding.key): pptr_key(binding.value)
                        for binding in (getattr(director, "m_SceneBindings", None) or [])}
            # SD/HD tracks coexist in a Timeline; only the bound one is played.
            if track_key in bindings and bindings[track_key] is None:
                continue
            for clip in (getattr(track, "m_Clips", None) or []):
                duration = getattr(clip, "m_Duration", None)
                asset_ref = getattr(clip, "m_Asset", None)
                if asset_ref is None:
                    continue
                try:
                    clip_asset = asset_ref.read()
                except Exception:
                    continue
                template = getattr(clip_asset, "template", None)
                ref_ptr = getattr(template, "animationReference", None) if template else None
                if ref_ptr is None:
                    continue
                try:
                    ref = ref_ptr.read()
                except Exception:
                    continue
                picked.append({
                    "timeline": getattr(timeline, "m_Name", None),
                    "animation": getattr(ref, "animationName", None),
                    "duration": round(float(duration), 4) if duration else None,
                    "start": round(float(getattr(clip, "m_Start", 0)), 7),
                    "clip_in": round(float(getattr(clip, "m_ClipIn", 0)), 7),
                    "time_scale": float(getattr(clip, "m_TimeScale", 1)),
                    "director_key": director_key,
                    "track_key": track_key,
                    "binding_key": bindings.get(track_key),
                    "skeleton_data_asset": name_of(getattr(ref, "skeletonDataAsset", None)),
                    "skeleton_data_key": pptr_key(getattr(ref, "skeletonDataAsset", None)),
                })

    camera = None
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        data = obj.read()
        if script_name(data) == "ScreenEffect":
            camera = {
                "position": vector3_of(getattr(data, "cameraPosition", None)),
                "angle": vector3_of(getattr(data, "cameraAngle", None)),
            }
            break
    # Duplicate prefabs can reference the same rig and identical Timeline clips.
    unique = {}
    for track in picked:
        key = (track["skeleton_data_key"], track["animation"], track["start"],
               track["clip_in"], track["time_scale"], track["duration"])
        unique.setdefault(key, track)
    picked = sorted(unique.values(), key=lambda track: track["start"])
    merged = []
    for track in picked:
        clip = {key: track[key] for key in ("start", "clip_in", "time_scale", "duration")}
        if merged:
            previous = merged[-1]
            contiguous = abs(previous["start"] + previous["duration"] - track["start"]) < 0.0002
            same_animation = all(previous[key] == track[key] for key in
                                 ("skeleton_data_key", "animation", "time_scale"))
            continuous_time = abs(previous["clip_in"] + previous["duration"] * previous["time_scale"]
                                  - track["clip_in"]) < 0.0002
            if contiguous and same_animation and continuous_time:
                previous["duration"] = round(previous["duration"] + track["duration"], 4)
                previous["clips"].append(clip)
                continue
        track["clips"] = [clip]
        merged.append(track)
    return merged, camera


def skeleton_data_assets(env):
    """名字 -> {json: TextAsset 名, atlases: [atlas TextAsset 名], atlas_assets: [SpineAtlasAsset]}"""
    table = {}
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        data = obj.read()
        if script_name(data) != "SkeletonDataAsset":
            continue
        atlases = []
        for atlas_ref in (getattr(data, "atlasAssets", None) or []):
            try:
                atlas_asset = atlas_ref.read()
            except Exception:
                continue
            texture_keys = []
            for material_ref in (getattr(atlas_asset, "materials", None) or []):
                try:
                    material = material_ref.read()
                    props = getattr(material, "m_SavedProperties", None)
                    for prop, tex_env in (getattr(props, "m_TexEnvs", None) or []):
                        if prop != "_MainTex":
                            continue
                        key = pptr_key(getattr(tex_env, "m_Texture", None))
                        if key is not None:
                            texture_keys.append(key)
                except Exception:
                    continue
            atlas_text = text_of(getattr(atlas_asset, "atlasFile", None))
            atlases.append({
                "name": getattr(atlas_asset, "m_Name", None),
                "file": atlas_file_name(getattr(atlas_asset, "atlasFile", None), atlas_text),
                "text": atlas_text,
                "atlas_key": reader_key(atlas_ref.deref()) if atlas_ref is not None else None,
                "texture_keys": texture_keys,
            })
        table[reader_key(obj)] = {
            "name": getattr(data, "m_Name", None),
            "json": name_of(getattr(data, "skeletonJSON", None)),
            "json_text": text_of(getattr(data, "skeletonJSON", None)),
            "atlases": atlases,
        }
    return table


def choose_atlas(candidates, textures):
    """同一个 atlas 文件常有低清/高清两版（分页尺寸不同、本地只装了一版）。

    挑"所有分页都能对上贴图"的那份，再按总像素取最大的。
    返回 (选中的 atlas, 全部候选的评分明细)。
    """
    scored = []
    for atlas in candidates:
        text = atlas["text"].decode("utf-8", "replace")
        pages = []
        for page, width, height in atlas_pages(text):
            stem = page.rsplit(".", 1)[0]
            bound = []
            for key in set(atlas.get("texture_keys", [])):
                record = textures.get(key)
                if record is not None and record["name"] == stem:
                    bound.append(record)
            # No global name fallback: same-name Texture2D objects are known
            # to collide across AssetBundles.  A missing material binding is
            # an extraction error, not a reason to guess.
            exact = [r for r in bound
                     if r["width"] == width and r["height"] == height]
            if len(bound) == 1 and len(exact) == 1:
                pages.append((page, width, height, exact[0], "exact"))
            elif len(bound) > 1:
                pages.append((page, width, height, None, "ambiguous"))
            elif bound:
                scaling = imported_texture_scaling((width, height),
                                                   (bound[0]["width"], bound[0]["height"]))
                pages.append((page, width, height, bound[0],
                              "unity_import_scaled" if scaling else "sizemismatch"))
            else:
                pages.append((page, width, height, None, "missing"))
        complete = bool(pages) and all(p[4] in ("exact", "unity_import_scaled") for p in pages)
        pixels = sum(p[1] * p[2] for p in pages)
        scored.append({"atlas": atlas, "pages": pages,
                       "complete": complete, "pixels": pixels})
    scored.sort(key=lambda item: (item["complete"], item["pixels"]), reverse=True)
    return (scored[0] if scored else None), scored


def extract(code, raw=False):
    source = os.path.join(ROOT, code)
    if not os.path.isdir(source):
        raise SystemExit(f"没有这套 cutin: {source}")
    folder = os.path.join(OUT_ROOT, code)
    os.makedirs(folder, exist_ok=True)

    env = UnityPy.load(source)
    textures = collect_textures(env)
    tracks, camera = spine_tracks(env)
    skeletons = skeleton_data_assets(env)

    # 目录里可能还有游离的 rig（不属于 prefab），--raw 时一并导出
    extra = []
    if raw:
        used = {t.get("skeleton_data_key") for t in tracks}
        for name, sda in skeletons.items():
            if name not in used:
                extra.append(sda["name"])

    if not tracks and not raw:
        raise RuntimeError(f"{code}: no prefab Spine track (non-Spine cutin)")
    if not tracks:
        print(f"!! {code}: 没找到 prefab 的 Spine 轨道，按 --raw 导出 skeleton")
        for key, sda in skeletons.items():
            tracks.append({"timeline": None, "animation": None, "duration": None,
                           "skeleton_data_asset": sda["name"],
                           "skeleton_data_key": key})

    manifest = {"code": code, "camera": camera, "rigs": []}
    for track in tracks:
        sda = skeletons.get(track.get("skeleton_data_key"))
        if sda is None:
            print(f"   !! {code}: 找不到 SkeletonDataAsset {track['skeleton_data_asset']}")
            continue
        rig = {"skeleton_data_asset": sda["name"], "skeleton": None, "animation": track["animation"],
               "duration": track["duration"], "timeline": track["timeline"], "atlases": []}
        for key in ("start", "clip_in", "time_scale", "director_key", "track_key", "binding_key", "clips"):
            if key in track:
                rig[key] = track[key]
        skeleton_name = None
        if sda["json_text"]:
            skeleton_name = (sda["json"] or sda["name"]) + ".json"
            with open(os.path.join(folder, skeleton_name), "wb") as handle:
                handle.write(sda["json_text"])
            rig["skeleton"] = skeleton_name
        # 同一份 atlas 文件可能来自多个包（低清/高清两版），先按文件名分组再挑
        groups = {}
        for atlas in sda["atlases"]:
            if not atlas["text"]:
                continue
            groups.setdefault(atlas["file"], []).append(atlas)
        for atlas_name, candidates in groups.items():
            # Do not deduplicate by atlas text alone: two SpineAtlasAsset
            # variants may share the same atlas text but point at different
            # material textures.
            deduped = list(candidates)
            chosen, scored = choose_atlas(deduped, textures)
            if chosen is None or not chosen["complete"]:
                raise RuntimeError(f"{code}: atlas {atlas_name} has no complete exact material/PNG match")
            with open(os.path.join(folder, atlas_name), "wb") as handle:
                handle.write(chosen["atlas"]["text"])
            text = chosen["atlas"]["text"].decode("utf-8", "replace")
            info = {"atlas": atlas_name, "scalars": atlas_scalars(text),
                    "variants": len(deduped), "verified_regions": validate_regions(text), "pages": []}
            for page, width, height, image, match in chosen["pages"]:
                if image is None:
                    info["pages"].append({"file": page, "width": width, "height": height,
                                          "missing": True})
                    continue
                pixels = image["texture"].image
                if pixels.size != (image["width"], image["height"]):
                    raise RuntimeError(f"{code}: decoded image size mismatch: {page} {pixels.size}")
                pixels.save(os.path.join(folder, page))
                pixels.close()
                entry = {"file": page, "width": width, "height": height, "match": match}
                if match != "exact":
                    entry["actual"] = [image["width"], image["height"]]
                    entry["scaling"] = imported_texture_scaling((width, height), tuple(entry["actual"]))
                    entry["texture_key"] = next(key for key, value in textures.items() if value is image)
                info["pages"].append(entry)
            if any(page.get("match") not in ("exact", "unity_import_scaled") for page in info["pages"]):
                raise RuntimeError(
                    f"{code}: atlas {atlas_name} has no exact material/PNG page match: "
                    f"{info['pages']}"
                )
            rig["atlases"].append(info)
        if not rig["skeleton"] or not rig["atlases"]:
            raise RuntimeError(f"{code}: incomplete rig {sda['name']}")
        skeleton_json = json.loads(sda["json_text"])
        if track["animation"] and track["animation"] not in skeleton_json.get("animations", {}):
            raise RuntimeError(f"{code}: missing animation {track['animation']}")
        manifest["rigs"].append(rig)

    if extra:
        manifest["unused_rigs"] = extra

    with open(os.path.join(folder, "cutin.json"), "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=1)

    # 再写一份扁平清单，方便 Java 侧（不带 JSON 库）直接读
    with open(os.path.join(folder, "cutin.txt"), "w", encoding="utf-8") as handle:
        if camera and camera.get("position"):
            handle.write("camera=" + ",".join(str(v) for v in camera["position"]) + "\n")
            handle.write("\n")
        for index, rig in enumerate(manifest["rigs"]):
            handle.write(f"[rig {index}]\n")
            handle.write(f"skeleton={rig['skeleton']}\n")
            handle.write(f"animation={rig['animation']}\n")
            handle.write(f"duration={rig['duration']}\n")
            for key in ("start", "clip_in", "time_scale"):
                handle.write(f"{key}={rig.get(key, 1 if key == 'time_scale' else 0)}\n")
            for atlas in rig["atlases"]:
                handle.write(f"atlas={atlas['atlas']}\n")
                for page in atlas["pages"]:
                    handle.write(f"page={page['file']} {page['width']}x{page['height']}\n")
            handle.write("\n")

    print(f"=== {code} -> {folder}")
    for rig in manifest["rigs"]:
        print(f"  rig {rig['skeleton_data_asset']}: 动画={rig['animation']} 时长={rig['duration']}s "
              f"时间轴={rig['timeline']}")
        for atlas in rig["atlases"]:
            print(f"      图集 {atlas['atlas']} {atlas['scalars']} 分页={[p['file'] for p in atlas['pages']]}")
    if extra:
        print(f"  （未使用、仅记录）: {extra}")
    if camera:
        print(f"  相机 {camera}")
    return manifest


def main():
    if "--check" in sys.argv:
        check_all()
        return
    args = [a for a in sys.argv[1:] if a != "--raw"]
    raw = "--raw" in sys.argv
    for code in args or ["89631139"]:
        extract(code, raw=raw)


def check_one(code):
    """只解析引用链、不写任何文件：用于对全部 cutin 做覆盖率体检。"""
    source = os.path.join(ROOT, code)
    if not os.path.isdir(source):
        return {"code": code, "ok": False, "error": "目录不存在"}
    try:
        env = UnityPy.load(source)
        textures = collect_textures(env)
        tracks, _camera = spine_tracks(env)
        skeletons = skeleton_data_assets(env)
    except Exception as error:
        return {"code": code, "ok": False, "error": f"{type(error).__name__}: {error}"}
    rigs = []
    for track in tracks:
        sda = skeletons.get(track.get("skeleton_data_key"))
        if sda is None:
            rigs.append({"skeleton_data_asset": track["skeleton_data_asset"],
                         "error": "找不到 SkeletonDataAsset"})
            continue
        entry = {"skeleton_data_asset": sda["name"], "animation": track["animation"],
                 "duration": track["duration"], "has_skeleton_json": bool(sda["json_text"]),
                 "atlases": []}
        groups = {}
        for atlas in sda["atlases"]:
            if atlas["text"]:
                groups.setdefault(atlas["file"], []).append(atlas)
        for atlas_name, candidates in groups.items():
            # Do not deduplicate by atlas text alone: two SpineAtlasAsset
            # variants may share the same atlas text but point at different
            # material textures.
            deduped = list(candidates)
            chosen, scored = choose_atlas(deduped, textures)
            if chosen is None:
                entry["atlases"].append({"atlas": atlas_name, "error": "没有候选"})
                continue
            entry["atlases"].append({
                "atlas": atlas_name,
                "variants": len(deduped),
                "complete": chosen["complete"],
                "matches": sorted({page[4] for page in chosen["pages"]}),
                "pages": len(chosen["pages"]),
            })
        rigs.append(entry)
    return {"code": code, "ok": True, "rigs": rigs,
            "textures": len(textures), "animations": [t["animation"] for t in tracks]}


def check_all(out_path=r"D:\MDProMod\tmp\cutin_check.json"):
    from concurrent.futures import ProcessPoolExecutor

    codes = sorted(d for d in os.listdir(ROOT) if os.path.isdir(os.path.join(ROOT, d)))
    workers = min(4, max(1, (os.cpu_count() or 4) - 1))
    with ProcessPoolExecutor(max_workers=workers) as pool:
        results = list(pool.map(check_one, codes, chunksize=4))

    with open(out_path, "w", encoding="utf-8") as handle:
        json.dump(results, handle, ensure_ascii=False, indent=1)

    parsed = [r for r in results if r.get("ok")]
    failed = [r for r in results if not r.get("ok")]
    no_rig = [r for r in parsed if not r.get("rigs")]
    atlas_ok, atlas_partial = [], []
    multi = []
    for result in parsed:
        for rig in result["rigs"]:
            if len(rig.get("atlases", [])) > 1:
                multi.append(result["code"])
            for atlas in rig.get("atlases", []):
                if atlas.get("complete"):
                    atlas_ok.append(result["code"])
                else:
                    atlas_partial.append((result["code"], atlas))
    animations = {}
    for result in parsed:
        for rig in result["rigs"]:
            animations[rig.get("animation")] = animations.get(rig.get("animation"), 0) + 1

    print(f"共 {len(codes)} 套：解析成功 {len(parsed)}，失败 {len(failed)}，没有 rig 的 {len(no_rig)}")
    print(f"图集分页全部对上贴图的 cutin: {len(set(atlas_ok))}")
    print(f"有分页对不上的 cutin: {len({c for c, _ in atlas_partial})}")
    for code, atlas in atlas_partial[:8]:
        print(f"   {code} {atlas}")
    print(f"带多个图集的 rig: {len(set(multi))}")
    print("动画名分布(前 12):")
    for name, count in sorted(animations.items(), key=lambda item: -item[1])[:12]:
        print(f"   {name!r}: {count}")
    for result in failed[:8]:
        print(f"   失败 {result['code']}: {result.get('error')}")
    return results


if __name__ == "__main__":
    main()


