"""把当前 Blockbench 项目编译成 .bbmodel 写到磁盘。

用法: python export_project.py <out.bbmodel>
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import client  # noqa: E402


def main():
    out = Path(sys.argv[1])
    code = "(function(){return JSON.stringify(Codecs.project.compile());})()"
    result = client.text_result(client.call("risky_eval", {"code": code}))
    text = result if isinstance(result, str) else json.dumps(result, ensure_ascii=False)
    model = json.loads(text)
    for _ in range(2):
        if isinstance(model, str):
            model = json.loads(model)
    out.write_text(json.dumps(model, ensure_ascii=False, indent=1), encoding="utf-8")
    textures = [t.get("name") for t in model.get("textures", [])]
    groups = [g.get("name") for g in model.get("groups", [])]
    print(json.dumps({
        "file": str(out),
        "bytes": out.stat().st_size,
        "elements": len(model.get("elements", [])),
        "groups": groups,
        "textures": textures,
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
