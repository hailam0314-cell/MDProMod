"""Publish generated sound registrations.

两组来源合并：

* ``sounds.generated.json`` —— 2,333 个 SE，广播给场地周围，所以给 64 格的衰减距离。
* ``sounds.bgm.generated.json`` —— 110 首 BGM，走 ``SoundSource.MUSIC`` 且
  ``Attenuation.NONE``（非定位），因此不写衰减距离。
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent


def main():
    entries = json.loads((HERE / "sounds.generated.json").read_text(encoding="utf-8"))
    for event in entries.values():
        for sound in event["sounds"]:
            sound["attenuation_distance"] = 64
    bgm_path = HERE / "sounds.bgm.generated.json"
    bgm_count = 0
    if bgm_path.exists():
        bgm = json.loads(bgm_path.read_text(encoding="utf-8"))
        duplicates = sorted(set(bgm) & set(entries))
        if duplicates:
            raise ValueError(f"BGM 与 SE 的事件重名: {duplicates[:5]}")
        entries.update(bgm)
        bgm_count = len(bgm)
    output = ROOT / "mod/src/main/resources/assets/mdpromod/sounds.json"
    output.write_text(json.dumps(entries, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Published {len(entries)} sound events "
          f"({len(entries) - bgm_count} SE + {bgm_count} BGM): {output}")


if __name__ == "__main__":
    main()
