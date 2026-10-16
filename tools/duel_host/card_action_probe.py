"""验证"点卡出牌"的协议链路：召唤 → 选落点 → 召唤成功。

用的应答编码与模组 DuelHostProcess.promptAction 完全一致：
  召唤 = (候选序号 << 16) | 0，落点 = responseBytes [player, location, sequence]
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
    log = []

    def send(payload):
        process.stdin.write(json.dumps(payload) + "\n")
        process.stdin.flush()
        log.append({"sent": payload})
        print(">>", json.dumps(payload, ensure_ascii=False), flush=True)

    def reader():
        for line in process.stdout:
            line = line.strip()
            if not line:
                continue
            try:
                event = json.loads(line)
            except json.JSONDecodeError:
                continue
            log.append(event)
            kind = event.get("type")
            if kind == "prompt":
                print("<P", event.get("name"), "|", (event.get("summary") or "")[:90], flush=True)
            elif kind == "message" and event.get("name") in (
                    "Summoning", "Summoned", "SpSummoning", "SpSummoned", "Retry", "Win", "Set"):
                print("<5", event.get("name"), event.get("hex"), flush=True)
            elif kind == "error":
                print("<E", json.dumps(event, ensure_ascii=False)[:200], flush=True)

    threading.Thread(target=reader, daemon=True).start()

    def wait_prompt(function, after, timeout=30.0):
        deadline = time.time() + timeout
        while time.time() < deadline:
            prompts = [e for e in log[after:] if e.get("type") == "prompt"]
            if prompts and prompts[-1].get("function") == function:
                return prompts[-1], len(log)
            time.sleep(0.1)
        return None, len(log)

    send({"cmd": "start", "deck0": DECK0, "deck1": "Data/Windbot/Decks/AI_BlueEyes.ydk",
          "ai": True, "aiSeat": 1, "aiName": "MDProBot",
          "aiDeck": "AI_BlueEyes", "aiDeckFile": "Data/Windbot/Decks/AI_BlueEyes.ydk",
          "autoPassChain": True})

    prompt, mark = wait_prompt(11, 0)
    if prompt is None:
        print("没有等到 SelectIdleCmd")
        return 1
    summons = prompt.get("summonCards") or []
    print("可召唤数量:", len(summons))
    if not summons:
        print("这套卡组第一回合没有可召唤的怪兽，换一套再试")
        return 1
    first = summons[0]
    print("准备召唤:", first)
    send({"cmd": "response", "value": 0})  # (0 << 16) | 0 = 召唤第一个候选

    place_prompt, mark2 = wait_prompt(18, mark, timeout=15)
    if place_prompt is not None:
        places = place_prompt.get("places") or []
        print("落点候选:", json.dumps(places, ensure_ascii=False))
        if places:
            chosen = places[0]
            hex_value = "%02X%02X%02X" % (chosen["player"], chosen["location"], chosen["sequence"])
            send({"cmd": "responseBytes", "hex": hex_value})
    time.sleep(2.5)

    names = [e.get("name") for e in log if e.get("type") == "message"]
    retry = names.count("Retry")
    summary = {
        "prompt_sequence": [(e.get("function"), e.get("name")) for e in log if e.get("type") == "prompt"],
        "saw_summoning": "Summoning" in names,
        "saw_summoned": "Summoned" in names or "SpSummoned" in names,
        "retries": retry,
        "sent": [e["sent"] for e in log if "sent" in e],
    }
    print(json.dumps(summary, ensure_ascii=False, indent=1))
    send({"cmd": "stop"})
    time.sleep(0.6)
    process.terminate()
    return 0 if summary["saw_summoning"] and retry == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
