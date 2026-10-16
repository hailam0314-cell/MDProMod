r"""把成功导出并压缩后的名录变成模组随包带的清单 data/mdpromod/cutins.txt。

格式：每行 `卡号 时长毫秒`，服务端据此决定要不要发 cut-in 包、演出按住多久。
"""
import json
import os
from pathlib import Path

PROJECT = Path(os.environ.get("MDPROMOD_PROJECT", Path(__file__).resolve().parents[2]))
SOURCE = PROJECT / "runtime/cutin_validated_roster.json"
TARGET = PROJECT / "mod/src/main/resources/data/mdpromod/cutins.txt"


def main():
    with open(SOURCE, encoding="utf-8") as handle:
        results = json.load(handle)
    rows = []
    skipped = []
    for result in results:
        rigs = result.get("rigs") or []
        if not rigs:
            skipped.append((result["code"], "没有 rig（非 Spine 的演出）"))
            continue
        duration = result.get("duration") or 0.0
        if len(rigs) != 1 or not rigs[0].get("animation") or duration <= 0:
            skipped.append((result["code"], "缺动画名或时长"))
            continue
        rows.append((int(result["code"]), round(duration * 1000)))
    rows.sort()
    if not rows or skipped:
        raise RuntimeError(f"名录无效，未更新模组清单: {skipped}")
    with open(TARGET, "w", encoding="utf-8") as handle:
        handle.write("# 由 tools/cutin_pipeline/make_cutin_roster.py 从已验证导出生成，勿手改\n")
        handle.write("# 卡号 演出时长(毫秒)\n")
        for code, duration in rows:
            handle.write(f"{code} {duration}\n")
    print(f"写入 {len(rows)} 条 -> {TARGET}")


if __name__ == "__main__":
    main()
