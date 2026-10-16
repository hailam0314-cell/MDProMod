"""Inventory actual Unity Timeline audio references; never infer from asset names.

Uses object identities/PPtrs, traverses nested GroupTracks, and records muted
tracks separately. The timestamps are local Timeline seconds, not PCM lengths.
"""
from __future__ import annotations

import argparse
from concurrent.futures import ProcessPoolExecutor
import json
from pathlib import Path
import sys

PROJECT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(PROJECT / "tools" / "unitypy"))
import UnityPy


def identity(obj):
    return (obj.assets_file.name, obj.path_id)


def scan(path_string):
    path = Path(path_string)
    try:
        env = UnityPy.load(str(path))
        cache = {}
        scripts = {}

        def read(obj):
            key = identity(obj)
            if key not in cache:
                cache[key] = obj.read()
            return cache[key]

        def script(obj):
            try:
                data = read(obj)
                ptr = data.m_Script.deref()
                key = identity(ptr)
                if key not in scripts:
                    scripts[key] = read(ptr).m_Name
                return scripts[key]
            except Exception:
                return ""

        def target(ptr):
            try:
                return ptr.deref()
            except Exception:
                return None

        timelines, directors, assets, ui = [], [], [], []
        for obj in env.objects:
            if obj.type.name == "PlayableDirector":
                directors.append(obj)
            elif obj.type.name == "MonoBehaviour":
                kind = script(obj)
                if kind == "TimelineAsset":
                    timelines.append(obj)
                elif kind in ("SoundPlayableAsset", "SoundStopPlayableAsset"):
                    data = read(obj)
                    assets.append({"id": identity(obj), "kind": kind,
                                   "key": getattr(data, "startLabel", None),
                                   "mute": bool(getattr(data, "mute", False))})
                elif kind == "UIEventWithAudio":
                    ui.append({"id": identity(obj), "data": obj.read_typetree()})

        director_map = {}
        for obj in directors:
            data = read(obj)
            asset = target(data.m_PlayableAsset)
            if asset is None:
                continue
            go = target(data.m_GameObject)
            director_map.setdefault(identity(asset), []).append({
                "id": identity(obj), "game_object": read(go).m_Name if go else None,
                "initial_time": getattr(data, "m_InitialTime", 0),
            })

        rows = []
        for obj in timelines:
            data = read(obj)
            timeline_id = identity(obj)
            clips, labels, controls = [], [], []
            visited = set()

            def visit(ptr, parent_muted=False):
                track_obj = target(ptr)
                if track_obj is None or identity(track_obj) in visited:
                    return
                visited.add(identity(track_obj))
                track = read(track_obj)
                muted = parent_muted or bool(getattr(track, "m_Muted", False))
                kind = script(track_obj)
                for clip in getattr(track, "m_Clips", []) or []:
                    asset = target(getattr(clip, "m_Asset", None))
                    if asset is None:
                        continue
                    clip_data = read(asset)
                    clip_kind = script(asset)
                    base = {"time": round(float(clip.m_Start), 7),
                            "duration": round(float(clip.m_Duration), 7),
                            "track": getattr(track, "m_Name", ""),
                            "muted_track": muted, "asset_id": identity(asset)}
                    if clip_kind in ("SoundPlayableAsset", "SoundStopPlayableAsset"):
                        clips.append(dict(base, kind=clip_kind,
                                          key=getattr(clip_data, "startLabel", None),
                                          mute=bool(getattr(clip_data, "mute", False)),
                                          skip_duplicate=bool(getattr(clip_data, "skipDuplicate", False))))
                    elif "Label" in clip_kind or "Event" in clip_kind:
                        labels.append(dict(base, kind=clip_kind,
                                           display=getattr(clip, "m_DisplayName", ""),
                                           data=asset.read_typetree()))
                    elif clip_kind == "ControlPlayableAsset":
                        controls.append(dict(base, kind=clip_kind,
                                             data=asset.read_typetree()))
                for child in getattr(track, "m_Children", []) or []:
                    visit(child, muted)

            for track in getattr(data, "m_Tracks", []) or []:
                visit(track)
            rows.append({"name": data.m_Name, "id": timeline_id,
                         "directors": director_map.get(timeline_id, []),
                         "sounds": sorted(clips, key=lambda row: row["time"]),
                         "labels": labels, "controls": controls})
        keys = sorted({row["key"] for row in assets if row["key"]})
        return {"source": str(path), "timelines": rows, "sound_assets": assets,
                "sound_keys": keys, "ui_audio": ui}
    except Exception as error:
        return {"source": str(path), "error": f"{type(error).__name__}: {error}"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", default=r"D:\MyCardLibrary\mdpro3\MDPro3_Data\StandaloneWindows64")
    parser.add_argument("--output", default=str(PROJECT / "runtime" / "audio_timeline_census.json"))
    parser.add_argument("--scope", choices=["builtin", "all"], default="builtin")
    parser.add_argument("--workers", type=int, default=4)
    args = parser.parse_args()
    root = Path(args.source)
    paths = [root / "MasterDuel" / "Built-in" / "duel",
             root / "MasterDuel" / "Built-in" / "outduel"]
    if args.scope == "all":
        for parent in (root / "MonsterCutin", root / "MonsterCutin2"):
            paths.extend(sorted(parent.iterdir()))
        # Each leaf folder is a self-contained prefab plus dependency bundles.
        for parent in (root / "MasterDuel" / "Timeline", root / "MasterDuel" / "Effects"):
            paths.extend(sorted(p for p in parent.rglob("*")
                                if p.is_dir() and any(q.is_file() for q in p.iterdir())))
    results = []
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        for i, result in enumerate(pool.map(scan, [str(p) for p in paths])):
            results.append(result)
            if i % 20 == 0 or result.get("error"):
                print(f"{i + 1}/{len(paths)} {result['source']} keys={len(result.get('sound_keys', []))} {result.get('error', '')}", flush=True)
    report = {"source_root": str(root), "scope": args.scope, "sources": len(results),
              "errors": [r for r in results if "error" in r],
              "sound_keys": sorted({key for r in results for key in r.get("sound_keys", [])}),
              "results": results}
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(output), "sources": len(results), "errors": len(report["errors"]),
                      "sound_keys": len(report["sound_keys"])}, ensure_ascii=False))


if __name__ == "__main__":
    main()
