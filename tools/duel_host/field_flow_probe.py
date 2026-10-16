"""模拟游戏内单机链路：0 号座位（玩家）走 JSON，1 号座位交给 WindBot。

和模组 DuelHostProcess.sendStart 发出的 start 完全同形，用来验证：
  1) AI 座位会自动应答，不会卡住；
  2) 玩家座位的阶段提示只给核心允许的转移；
  3) 用 6/7 与 2/3 应答可以真的把阶段推进下去。
输出：docs/field-flow-probe-2026-10-14.json
"""
import json
import pathlib
import subprocess
import sys
import threading
import time

DOTNET = r"D:\MDProMod\tools\dotnet\dotnet.exe"
HOST = r"D:\MDProMod\tools\duel_host\bin\Release\net8.0\duel_host.dll"
STAGING = r"D:\MDProMod\tools\ygo_tcp_probe\staging"
OUT = pathlib.Path(r"D:\MDProMod\docs\field-flow-probe-2026-10-14.json")


def main():
    process = subprocess.Popen(
        [DOTNET, HOST, "--root", STAGING],
        cwd=STAGING,
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        bufsize=1,
        env={"DOTNET_CLI_HOME": r"D:\MDProMod\cache\dotnet-headless",
             "PATH": r"C:\Windows\system32;C:\Windows"},
    )
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
                log.append({"raw": line})
                continue
            kind = event.get("type")
            if kind == "message":
                log.append({"message": event.get("name"), "function": event.get("function"),
                            "hex": event.get("hex")})
            elif kind == "prompt":
                log.append({"prompt": event.get("name"), "function": event.get("function"),
                            "player": event.get("player"), "summary": event.get("summary"),
                            "toBattle": event.get("toBattle"), "toEnd": event.get("toEnd"),
                            "toMain2": event.get("toMain2")})
            else:
                log.append(event)

    threading.Thread(target=reader, daemon=True).start()

    def last_prompt(start=0):
        prompts = [e for e in log[start:] if "prompt" in e]
        return prompts[-1] if prompts else None

    def wait_prompt(start, timeout=25.0):
        deadline = time.time() + timeout
        while time.time() < deadline:
            prompt = last_prompt(start)
            if prompt is not None:
                return prompt
            time.sleep(0.1)
        return None

    send({"cmd": "start", "deck0": "starter.ydk",
          "deck1": "Data/Windbot/Decks/AI_BlueEyes.ydk",
          "ai": True, "aiSeat": 1, "aiName": "MDProBot",
          "aiDeck": "AI_BlueEyes", "aiDeckFile": "Data/Windbot/Decks/AI_BlueEyes.ydk",
          "autoPassChain": True})
    mark = 0
    battle_used = 0
    main2_used = 0
    handled = 0
    while handled < 18:
        prompt = wait_prompt(mark, 25 if handled == 0 else 15)
        if prompt is None:
            print("== 没有新提示 ==", flush=True)
            break
        mark = len(log)
        function = prompt.get("function")
        if function == 11:
            if prompt.get("toBattle") and battle_used < 1:
                value, note = 6, "进入战斗阶段"
                battle_used += 1
            else:
                value, note = 7, "进入结束阶段"
        elif function == 10:
            if prompt.get("toMain2") and main2_used < 1:
                value, note = 2, "进入主要阶段 2"
                main2_used += 1
            else:
                value, note = 3, "结束战斗阶段"
        elif function == 16:
            value, note = -1, "连锁窗口放弃"
        elif function in (12, 13):
            value, note = 0, "不发动"
        elif function == 15:
            value, note = None, "选卡：取第一张"
        else:
            print("== 未处理提示：" + json.dumps(prompt, ensure_ascii=False) + " ==", flush=True)
            break
        print("== [%d] %s | %s | player=%s ==" % (handled + 1, note,
                                                 prompt.get("summary"), prompt.get("player")), flush=True)
        if value is None:
            send({"cmd": "responseBytes", "hex": "0100"})
        else:
            send({"cmd": "response", "value": value})
        handled += 1
        time.sleep(0.4)

    time.sleep(3.0)
    send({"cmd": "stop"})
    time.sleep(0.8)
    process.terminate()
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()

    phases = []
    for entry in log:
        if entry.get("message") == "NewPhase":
            raw = bytes.fromhex(entry["hex"])
            phases.append(hex(raw[1] | (raw[2] << 8)))
    summary = {
        "phases": phases,
        "turns": [e["hex"] for e in log if e.get("message") == "NewTurn"],
        "ai_events": len([e for e in log if e.get("type") == "ai"]),
        "auto_pass_chain": len([e for e in log if e.get("type") == "autoPass"]),
        "human_prompts": [(e.get("function"), e.get("player"), e.get("toBattle"),
                           e.get("toEnd"), e.get("toMain2"))
                          for e in log if "prompt" in e and e.get("type") is None],
        "errors": [e.get("message") for e in log if e.get("type") == "error"],
    }
    OUT.write_text(json.dumps({"summary": summary, "log": log}, ensure_ascii=False, indent=1),
                   encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=1), flush=True)
    print("saved:", OUT, flush=True)


if __name__ == "__main__":
    sys.exit(main())
