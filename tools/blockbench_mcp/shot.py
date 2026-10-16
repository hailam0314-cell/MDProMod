"""截图 Blockbench 视图并保存 PNG。

用法: python shot.py <view> <out.png> [max_size]
"""
import base64
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import client  # noqa: E402


def main():
    view = sys.argv[1]
    out = Path(sys.argv[2])
    arguments = {"view": view}
    if len(sys.argv) > 3:
        arguments["max_size"] = int(sys.argv[3])
    result = client.call("capture_screenshot", arguments)
    for block in result.get("content", []):
        if block.get("type") == "image":
            raw = base64.b64decode(block["data"])
            out.write_bytes(raw)
            print(json.dumps({"file": str(out), "bytes": len(raw)}))
            return
    print("no image block:", json.dumps(result, ensure_ascii=False)[:1500])


if __name__ == "__main__":
    main()
