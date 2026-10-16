"""Check bundled duel-audio references and the final JAR without starting Minecraft.

This is a resource/reference consistency audit, not a claim of complete behavioral
coverage. Runtime trigger coverage still needs the C# event/timeline audit and a
Minecraft play-through.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import struct
import zipfile


ROOT = Path(__file__).resolve().parents[2]
RESOURCE_ROOT = ROOT / "mod/src/main/resources"
EVENT_PREFIX = "assets/mdpromod/"
KEY = re.compile(r'"(SE_[A-Za-z0-9_./-]+)"')


def inspect(read, names):
    names = set(names)
    errors = []
    sounds = json.loads(read(EVENT_PREFIX + "sounds.json"))
    timelines = json.loads(read("data/mdpromod/duel_audio_timelines.json"))
    references = []
    oggs = set()
    for event, definition in sounds.items():
        if not event.startswith("mdpro."):
            continue
        for sound in definition["sounds"]:
            value = sound if isinstance(sound, str) else sound["name"]
            namespace, _, path = value.partition(":")
            if not path:
                namespace, path = "minecraft", namespace
            if namespace != "mdpromod":
                errors.append(f"External sound resource: {value}")
                continue
            filename = EVENT_PREFIX + "sounds/" + path + ".ogg"
            oggs.add(filename)
            if filename not in names:
                errors.append(f"Missing OGG: {filename}")
                continue
            header = read(filename)[:64]
            # The Vorbis identification packet starts after the first Ogg page
            # header and its segment table. Reject Opus, stereo, and fake OGGs.
            marker = header.find(b"\x01vorbis")
            if not header.startswith(b"OggS") or marker < 0 or len(header) < marker + 16:
                errors.append(f"Not Ogg Vorbis: {filename}")
                continue
            channels = header[marker + 11]
            rate = struct.unpack_from("<I", header, marker + 12)[0]
            if channels != 1 or rate <= 0:
                errors.append(f"Invalid positional Vorbis: {filename} ({channels}ch, {rate}Hz)")
    timeline_entries = 0
    timeline_cues = 0
    for group, entries in timelines.items():
        for name, cues in entries.items():
            timeline_entries += 1
            previous_time = -1
            for cue in cues:
                timeline_cues += 1
                key = cue["key"]
                references.append((f"timeline {group}/{name}", key))
                when = cue.get("timeMs", 0)
                if when < previous_time or when < 0:
                    errors.append(f"Unsorted/negative cue: {group}/{name}/{key}")
                previous_time = when
                # Unity trigger boxes must not truncate the actual sample.
                if 0 < cue.get("durationMs", 0) <= 250:
                    errors.append(f"Suspicious trigger-box cutoff: {group}/{name}/{key}")
    literal_keys = set()
    dynamic_prefixes = set()
    for file in (ROOT / "mod/src/main/java").rglob("*.java"):
        for match in KEY.finditer(file.read_text(encoding="utf-8")):
            key = match.group(1)
            if key.endswith("_") or key.endswith("_0") or key == "SE_PHASE":
                dynamic_prefixes.add(key)
            else:
                literal_keys.add(key)
                references.append((str(file.relative_to(ROOT)), key))
    used = set()
    missing = []
    for source, key in references:
        event = "mdpro." + key.lower()
        used.add(event)
        if event not in sounds:
            missing.append({"source": source, "key": key})
    if missing:
        errors.append(f"Unregistered references: {len(missing)}")
    for prefix in dynamic_prefixes:
        if not any(event.startswith("mdpro." + prefix.lower()) for event in sounds):
            errors.append(f"Dynamic family has no resource: {prefix}")
    wavs = sorted(name for name in names if name.lower().endswith(".wav"))
    if wavs:
        errors.append(f"WAV files present: {len(wavs)}")
    return {
        "sound_events": sum(event.startswith("mdpro.") for event in sounds),
        "ogg_resources": len(oggs),
        "ogg_bytes": sum(len(read(name)) for name in oggs if name in names),
        "timeline_groups": {name: len(entries) for name, entries in timelines.items()},
        "timeline_entries": timeline_entries,
        "timeline_cues": timeline_cues,
        "java_literal_keys": len(literal_keys),
        "java_dynamic_prefixes": sorted(dynamic_prefixes),
        "referenced_sound_events": len(used),
        "unregistered_references": missing,
        "wav_files": wavs,
        "errors": errors,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--jar", type=Path, help="Also audit a built mod JAR")
    parser.add_argument("--output", type=Path, default=ROOT / "analysis/duel_audio_integration_verification.json")
    args = parser.parse_args()
    source_names = [file.relative_to(RESOURCE_ROOT).as_posix()
                    for file in RESOURCE_ROOT.rglob("*") if file.is_file()]
    result = {"source": inspect(lambda name: (RESOURCE_ROOT / name).read_bytes(), source_names)}
    if args.jar:
        with zipfile.ZipFile(args.jar) as jar:
            result["jar"] = inspect(jar.read, jar.namelist())
            result["jar_path"] = str(args.jar.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 1 if any(value.get("errors") for value in result.values() if isinstance(value, dict)) else 0


if __name__ == "__main__":
    raise SystemExit(main())
