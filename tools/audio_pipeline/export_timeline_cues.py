"""Generate the Java-readable cue table from the audited Unity PPtr census.

Do not use TimelineClip duration to stop a one-shot: MDPro's
SoundPlayableBehaviour plays the entire AudioClip on entry.
"""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "runtime/audio_timeline_census_full.json"
OUTPUT = ROOT / "mod/src/main/resources/data/mdpromod/duel_audio_timelines.json"


def main():
    census = json.loads(SOURCE.read_text(encoding="utf-8"))
    groups = {"prefab": {}, "cutin": {}, "specialwin": {}}
    evidence = []
    for result in census["results"]:
        for timeline in result.get("timelines", []):
            cues = [{"key": sound["key"], "timeMs": round(sound["time"] * 1000)}
                    for sound in timeline["sounds"]
                    if sound.get("key") and not sound["muted_track"]
                    and sound["kind"] == "SoundPlayableAsset"]
            # mute is not copied by the installed SoundPlayableAsset implementation,
            # so only TrackAsset.m_Muted actually suppresses a source cue.
            if not cues:
                continue
            aliases = {timeline["name"]}
            aliases.update(d["game_object"] for d in timeline["directors"] if d["game_object"])
            for alias in sorted(aliases):
                if alias in groups["prefab"] and groups["prefab"][alias] != cues:
                    raise ValueError(f"Conflicting same-name timelines: {alias}")
                groups["prefab"][alias] = cues
            if "\\SpecialWin\\" in result["source"] or "/SpecialWin/" in result["source"]:
                groups["specialwin"][Path(result["source"]).name] = cues
            for cue in cues:
                if cue["key"].startswith("SE_MONSTER_CUTIN_"):
                    groups["cutin"][cue["key"].removeprefix("SE_MONSTER_CUTIN_")] = [cue]
            evidence.append({"source": result["source"], "timeline": timeline["name"],
                             "aliases": sorted(aliases), "cues": cues})
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(groups, ensure_ascii=False, indent=2), encoding="utf-8")
    evidence_path = ROOT / "runtime/audio_timeline_cues_evidence.json"
    evidence_path.write_text(json.dumps(evidence, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(OUTPUT), "groups": {k: len(v) for k, v in groups.items()},
                      "keys": len({c["key"] for g in groups.values() for cues in g.values() for c in cues})}))


if __name__ == "__main__":
    main()
