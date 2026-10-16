"""手动驱动 duel_host，验证阶段切换的 prompt 与响应映射。

要验证的映射：
  SelectIdleCmd(主要阶段)  response 6 = 战斗阶段, 7 = 结束阶段
  SelectBattleCmd(战斗阶段) response 2 = 主要阶段 2, 3 = 结束阶段
输出：docs/phase-switch-probe-2026-10-14.json
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
OUT = pathlib.Path(r"D:\MDProMod\docs\phase-switch-probe-2026-10-14.json")


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
                            "options": event.get("options"),
                            "toBattle": event.get("toBattle"), "toEnd": event.get("toEnd"),
                            "toMain2": event.get("toMain2"),
                            "responseKind": event.get("responseKind")})
            else:
                log.append(event)

    thread = threading.Thread(target=reader, daemon=True)
    thread.start()

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

    send({"cmd": "start", "deck0": "starter.ydk", "deck1": "starter.ydk"})
    mark = 0
    battle_entered = 0
    main2_entered = 0
    handled = 0
    while handled < 26:
        prompt = wait_prompt(mark, 25 if handled == 0 else 12)
        if prompt is None:
            print("== 没有新的提示，结束 ==", flush=True)
            break
        mark = len(log)
        function = prompt.get("function")
        summary = prompt.get("summary") or ""
        note = None
        if function == 11:
            if "battle=yes" in summary and battle_entered < 2:
                value, note = 6, "主要阶段 → 战斗阶段"
                battle_entered += 1
            else:
                value, note = 7, "主要阶段 → 结束阶段"
        elif function == 10:
            if main2_entered < 1:
                value, note = 2, "战斗阶段 → 主要阶段 2"
                main2_entered += 1
            else:
                value, note = 3, "战斗阶段 → 结束阶段（跳过主要阶段 2）"
        elif function == 16:
            value, note = -1, "连锁窗口：放弃优先权"
        elif function in (12, 13):
            value, note = 0, "不发动"
        else:
            print("== 未处理的提示：" + json.dumps(prompt, ensure_ascii=False) + " ==", flush=True)
            break
        print("== [%d] %s | %s | player=%s ==" % (handled + 1, note, summary, prompt.get("player")),
              flush=True)
        send({"cmd": "response", "value": value})
        handled += 1
        time.sleep(0.4)

    send({"cmd": "stop"})
    time.sleep(0.8)
    process.terminate()
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()

    phases = [(e.get("hex"), e.get("function")) for e in log
              if e.get("message") == "NewPhase"]
    turns = [e.get("hex") for e in log if e.get("message") == "NewTurn"]
    summary = {
        "new_phases": phases,
        "new_turns": turns,
        "responses": [e["sent"]["value"] for e in log
                      if "sent" in e and "value" in e["sent"]],
        "prompt_count": len([e for e in log if "prompt" in e]),
    }
    OUT.write_text(json.dumps({"summary": summary, "log": log}, ensure_ascii=False, indent=1),
                   encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=1), flush=True)
    print("saved:", OUT, flush=True)


if __name__ == "__main__":
    sys.exit(main())
