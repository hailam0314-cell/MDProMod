"""Audit MDPro duel sound references without decoding textures or audio samples."""
from __future__ import annotations

import argparse
import csv
import io
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, r"D:\MDProMod\tools\unitypy")
import UnityPy
from UnityPy.classes.PPtr import PPtr

SOURCE = Path(r"D:\MyCardLibrary\mdpro3\MDPro3_Data\StandaloneWindows64")
DECOMPILED = Path(r"D:\MDProMod\analysis\latest_decompiled")
UNITY_SOURCE = Path(r"D:\MDProMod\MDPro3-main\MDPro3-main\Assets")
OUTPUT = Path(r"D:\MDProMod\analysis\duel_audio_audit.json")


def reference(obj, pointer):
    if not pointer or not pointer.get("m_PathID"):
        return None
    try:
        return PPtr(assetsfile=obj.assets_file, **pointer).deref()
    except Exception:
        return None


def key(obj):
    return (obj.assets_file.name, obj.path_id)


def scan_bundle(path, extended=False):
    env = UnityPy.load(str(path))
    cache = {}

    def tree(obj):
        if obj is None:
            return {}
        identity = key(obj)
        if identity not in cache:
            cache[identity] = obj.read_typetree()
        return cache[identity]

    def top_timeline(track):
        seen = set()
        current = track
        while current is not None and key(current) not in seen:
            seen.add(key(current))
            value = tree(current)
            parent = reference(current, value.get("m_Parent"))
            if parent is None:
                return value.get("m_Name", "")
            current = parent
        return ""

    events = []
    loose = []
    failures = []
    references = []
    animation_events = []
    audio_sources = []
    text_events = []

    def collect_labels(value, prefix="", context=None):
        if isinstance(value, dict):
            for name, item in value.items():
                yield from collect_labels(item, f"{prefix}.{name}" if prefix else name, value)
        elif isinstance(value, list):
            for index, item in enumerate(value):
                yield from collect_labels(item, f"{prefix}[{index}]")
        elif isinstance(value, str) and value.upper().startswith(("SE_", "BGM_")):
            yield {"field": prefix, "sound": value,
                   "context": {name: item for name, item in (context or {}).items()
                               if isinstance(item, (str, int, float, bool))}}

    for obj in env.objects:
        if extended and obj.type.name == "TextAsset":
            try:
                value = tree(obj)
                if value.get("m_Name") == "EffectSeLabelData":
                    raw = value.get("m_Script", "")
                    if isinstance(raw, bytes):
                        raw = raw.decode("utf-8-sig")
                    for row in csv.reader(io.StringIO(raw)):
                        if len(row) != 7:
                            continue
                        for index in (1, 3, 5):
                            if row[index].lower().startswith("se_"):
                                text_events.append({"asset": value["m_Name"], "effect": row[0],
                                                    "sound": row[index].upper(),
                                                    "start": float(row[index + 1] or 0)})
            except Exception as error:
                failures.append({"object": obj.path_id, "error": str(error)})
            continue
        if extended and obj.type.name == "AudioSource":
            try:
                value = tree(obj)
                clip = reference(obj, value.get("m_audioClip", value.get("m_AudioClip")))
                clip_name = tree(clip).get("m_Name", "")
                if clip_name:
                    audio_sources.append({"object": obj.path_id, "sound": clip_name,
                                          "play_on_awake": bool(value.get("m_PlayOnAwake", False)),
                                          "loop": bool(value.get("Loop", value.get("m_Loop", False))),
                                          "volume": value.get("m_Volume", 1)})
            except Exception as error:
                failures.append({"object": obj.path_id, "error": str(error)})
            continue
        if extended and obj.type.name == "AnimationClip":
            try:
                value = tree(obj)
                for event in value.get("m_Events", []):
                    label = event.get("data", event.get("stringParameter", ""))
                    if isinstance(label, str) and label.startswith("SE_"):
                        animation_events.append({"clip": value.get("m_Name", ""),
                                                 "time": event.get("time", 0),
                                                 "function": event.get("functionName", ""),
                                                 "sound": label})
            except Exception as error:
                failures.append({"object": obj.path_id, "error": str(error)})
            continue
        if obj.type.name != "MonoBehaviour" or not obj.serialized_type.node:
            continue
        fields = {node.m_Name for node in obj.serialized_type.node.m_Children}
        if not extended and not fields.intersection({"startLabel", "m_Clips", "seLabel", "label1", "label2", "label3"}):
            continue
        try:
            value = tree(obj)
            if extended:
                game_object = tree(reference(obj, value.get("m_GameObject"))).get("m_Name", "")
                script = tree(reference(obj, value.get("m_Script"))).get("m_ClassName", "")
                references.extend(dict(object=obj.path_id, name=value.get("m_Name", ""),
                                       game_object=game_object, component=script, **item)
                                  for item in collect_labels(value))
            if value.get("startLabel"):
                loose.append(value["startLabel"])
            for clip in value.get("m_Clips", []):
                asset = reference(obj, clip.get("m_Asset"))
                asset_tree = tree(asset)
                label = asset_tree.get("startLabel", "")
                if not label:
                    continue
                events.append({
                    "timeline": top_timeline(obj),
                    "track": value.get("m_Name", ""),
                    "sound": label,
                    "start": round(clip.get("m_Start", 0), 6),
                    "duration": round(clip.get("m_Duration", 0), 6),
                    "muted": bool(value.get("m_Muted", 0)),
                    "asset_muted": bool(asset_tree.get("mute", 0)),
                })
        except Exception as error:
            failures.append({"object": obj.path_id, "error": str(error)})
    return {"bundle": str(path.relative_to(SOURCE)), "events": events,
            "labels": sorted(set(loose)), "references": references,
            "animation_events": animation_events, "audio_sources": audio_sources,
            "text_events": text_events, "errors": failures}


def source_calls():
    records = []
    for folder in DECOMPILED.iterdir():
        if not folder.is_dir():
            continue
        for path in folder.glob("*.cs"):
            lines = path.read_text(encoding="utf-8-sig").splitlines()
            method = ""
            for index, line in enumerate(lines, 1):
                if re.search(r"\b(public|private|protected)\s+.*\(", line) and not "=>" in line:
                    method = line.strip()
                if re.search(r"AudioManager\.(PlaySE|PlayVoice|PlayBgm|PlayBGM)|PlayShuffleSE", line):
                    records.append({"file": str(path.relative_to(DECOMPILED)), "line": index,
                                    "method": method, "call": line.strip()})
    return records


def source_literals():
    records = []
    for path in DECOMPILED.rglob("*.cs"):
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        for index, line in enumerate(lines, 1):
            labels = re.findall(r'"((?:SE_|BGM_)[^"\r\n]*)"', line)
            if labels:
                records.append({"file": str(path.relative_to(DECOMPILED)), "line": index,
                                "labels": labels, "code": line.strip()})
    return records


def scan_paths(all_cutins=True, extended=False):
    paths = [SOURCE / "MasterDuel" / "Built-in" / "duel"]
    paths.extend((SOURCE / "MasterDuel" / "Timeline").rglob("*"))
    if all_cutins:
        paths.extend((SOURCE / "MonsterCutin").rglob("*"))
        paths.extend((SOURCE / "MonsterCutin2").rglob("*"))
    if extended:
        for name in ("BG", "Mat", "Grave", "AvatarStand", "Mate", "Card", "Effects", "Built-in"):
            paths.extend((SOURCE / "MasterDuel" / name).rglob("*"))
        paths.extend(p for p in (SOURCE / "MDPro3").rglob("*")
                     if p.is_file() and not p.name.startswith(("sound_", "bgm_", "texture_", "shader_", "sprite_", "icon_", "font_")))
    return sorted({p for p in paths if p.is_file()})


def validate_catalog(data):
    """Validate literal asset references; this is not a claim of runtime hook coverage."""
    available = {p.stem[len("sound_assets_"):].upper()
                 for p in (SOURCE / "MDPro3").glob("sound_assets_*.bundle")}
    refs = set()
    for bundle in data.get("bundles", []):
        for kind in ("events", "animation_events", "references", "text_events"):
            refs.update(item["sound"].upper() for item in bundle.get(kind, [])
                        if item.get("sound"))
    source = {label.upper() for item in source_literals() for label in item["labels"]
              if re.fullmatch(r"SE_[A-Za-z0-9_]+", label) and not label.endswith("_")}
    dynamic = set()
    for attribute in ("DARK", "EARTH", "FIRE", "LIGHT", "WATER", "WIND", "DIVINE"):
        dynamic.add(f"SE_MONSTER_CUTIN_{attribute}")
        for strength in ("DEFAULT", "SPECIAL"):
            for stage in (1, 2):
                dynamic.add(f"SE_ATTACK_A_{attribute}_{strength}_{stage:02d}")
    for kind in ("NORMAL", "FUSION", "SYNCHRO", "XYZ", "LINK", "RITUAL", "PENDULUM"):
        for strength in ("MIDDLE", "HIGH"):
            dynamic.add(f"SE_LAND_{kind}_{strength}")
    for opponent in ("", "_OPP"):
        for phase in ("DRAW", "STANDBY", "MAIN1", "BATTLE", "MAIN2", "END"):
            dynamic.add(f"SE_PHASE{opponent}_{phase}")
    for side in ("PLAYER", "RIVAL"):
        for kind in ("COUNT", "RECOVERY"):
            dynamic.add(f"SE_LP_{kind}_{side}")
    for number in range(1, 5):
        dynamic.add(f"SE_CARD_MOVE_{number:02d}")

    def missing(labels):
        exact = sorted(labels - available)
        fallback = [label for label in exact if label.endswith("_ONESHOT")
                    and label[:-len("_ONESHOT")] in available]
        return {"labels": len(labels), "missing_exact": exact,
                "resolved_by_oneshot_fallback": fallback,
                "missing_after_fallback": [label for label in exact if label not in fallback]}

    return {"method": "Case-insensitive label vs installed sound_assets_<label>.bundle filename. "
                      "Unused prefab fields and source string fragments are not proof of an audible event.",
            "installed_sound_bundles": len(available),
            "bundle_references": missing(refs), "source_literals": missing(source),
            "expanded_core_families": missing(dynamic)}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--skip-cutins", action="store_true")
    parser.add_argument("--extended", action="store_true",
                        help="Also audit duel fields, mates, card effects and UI prefab labels/events.")
    parser.add_argument("--catalog-only", action="store_true",
                        help="Refresh source and resource validation in an existing output without rescanning bundles.")
    parser.add_argument("--output", type=Path, default=OUTPUT)
    args = parser.parse_args()
    if args.catalog_only:
        data = json.loads(args.output.read_text(encoding="utf-8"))
        data["source_calls"] = source_calls()
        data["source_literals"] = source_literals()
        data["catalog_validation"] = validate_catalog(data)
        args.output.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps(data["catalog_validation"], ensure_ascii=False), flush=True)
        return
    paths = scan_paths(not args.skip_cutins, args.extended)
    bundles = []
    errors = []
    for index, path in enumerate(paths, 1):
        try:
            result = scan_bundle(path, args.extended)
            if any(result[k] for k in ("labels", "events", "references", "animation_events", "audio_sources", "text_events", "errors")):
                bundles.append(result)
        except Exception as error:
            errors.append({"bundle": str(path.relative_to(SOURCE)), "error": str(error)})
        if index % 100 == 0:
            print(f"scanned {index}/{len(paths)}; sound bundles={len(bundles)}", flush=True)
    calls = source_calls()
    source_ogg = [dict(name=p.stem, category=p.parent.name, bytes=p.stat().st_size)
                  for p in (UNITY_SOURCE / "Addressables" / "Sound").rglob("*.ogg")]
    data = {"source": str(SOURCE), "scanned_files": len(paths),
            "source_calls": calls, "source_literals": source_literals(), "bundles": bundles, "errors": errors,
            "source_ogg": source_ogg}
    data["catalog_validation"] = validate_catalog(data)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(args.output), "scanned": len(paths),
                      "bundles": len(bundles), "events": sum(len(x["events"]) for x in bundles),
                      "labels": len({label for x in bundles for label in x["labels"]}),
                      "errors": len(errors)}, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
