"""用中文卡组路径启动 duel_host，验证 stdin 按 UTF-8 解码。

回归的就是这次开局失败的根因：UTF-8 路径被当成 GBK → 找不到 珠泪.ydk。
"""
import json
import subprocess
import sys
import threading
import time

DOTNET = r"D:\MDProMod\tools\dotnet\dotnet.exe"
HOST = r"D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll"
STAGING = r"D:\MDProMod\tools\ygo_tcp_probe\staging"
DECK0 = r"D:\MDProMod\runtime\decks\珠泪.ydk"


def main():
    process = subprocess.Popen(
        [DOTNET, HOST, "--root", STAGING], cwd=STAGING,
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        text=True, encoding="utf-8", bufsize=1,
        env={"DOTNET_CLI_HOME": r"D:\MDProMod\cache\dotnet-headless",
             "PATH": r"C:\Windows\system32;C:\Windows"})
    lines = []

    def reader():
        for line in process.stdout:
            lines.append(line.strip())

    threading.Thread(target=reader, daemon=True).start()
    process.stdin.write(json.dumps({
        "cmd": "start", "deck0": DECK0, "deck1": "starter.ydk",
        "ai": True, "aiSeat": 1, "aiName": "MDProBot",
        "aiDeck": "AI_BlueEyes", "aiDeckFile": "Data/Windbot/Decks/AI_BlueEyes.ydk",
        "autoPassChain": True}) + "\n")
    process.stdin.flush()
    deadline = time.time() + 25
    verdict = "timeout"
    while time.time() < deadline:
        for line in list(lines):
            if '"started"' in line:
                verdict = "started"
                break
            if '"error"' in line:
                verdict = "error: " + line[:220]
                break
        if not verdict.startswith(("timeout",)) and verdict != "timeout":
            break
        time.sleep(0.2)
    if verdict == "timeout":
        recent = [l for l in lines if '"error"' in l or '"started"' in l or '"ready"' in l]
        verdict = "timeout; notable=" + json.dumps(recent[-3:], ensure_ascii=False)
    print("deck0 =", DECK0)
    print("verdict:", verdict)
    process.stdin.write(json.dumps({"cmd": "stop"}) + "\n")
    process.stdin.flush()
    time.sleep(0.6)
    process.terminate()
    return 0 if verdict == "started" else 1


if __name__ == "__main__":
    sys.exit(main())
