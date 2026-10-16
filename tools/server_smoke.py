import ctypes
import json
import socket
import struct
import threading
import time
from pathlib import Path

ROOT = Path(r"D:\MDProMod\tools\ygo_tcp_probe\staging")
DLL_DIR = ROOT
PORT = 17911
ARGS = f"{PORT} -1 5 0 F F F 8000 5 1 180 0"

server = ctypes.CDLL(str(DLL_DIR / "ygoserver.dll"))
server.start_server.argtypes = [ctypes.c_char_p]
server.start_server.restype = ctypes.c_int
server.stop_server.argtypes = []
server.stop_server.restype = None

result = {"args": ARGS, "port": PORT, "connected": False, "packets": [], "errors": []}

def run_server():
    try:
        rc = server.start_server(ARGS.encode("utf-8"))
        result["start_return"] = int(rc)
    except Exception as e:
        result["errors"].append(f"start_server: {e!r}")

thread = threading.Thread(target=run_server, daemon=True)
thread.start()

sock = None
try:
    deadline = time.time() + 8
    while time.time() < deadline:
        try:
            sock = socket.create_connection(("127.0.0.1", PORT), timeout=1)
            break
        except OSError:
            time.sleep(0.1)
    if sock is None:
        result["errors"].append("server did not accept a TCP connection within 8 seconds")
    else:
        result["connected"] = True
        sock.settimeout(1.0)
        def packet(function, payload=b""):
            body = bytes([function]) + payload
            return struct.pack("<H", len(body)) + body
        def utf16_fixed(text, chars):
            raw = text.encode("utf-16-le")
            return raw[:chars*2].ljust(chars*2, b"\x00")
        # Follow MDPro3 TcpHelper.Join order.
        sock.sendall(packet(23, struct.pack("<I", 0) + utf16_fixed("127.0.0.1", 10)))
        sock.sendall(packet(16, utf16_fixed("mdpromod-probe", 20)))
        sock.sendall(packet(18, struct.pack("<HBBi", 4962, 204, 204, 0) + utf16_fixed("", 20)))
        end = time.time() + 5
        buf = b""
        while time.time() < end:
            try:
                data = sock.recv(65536)
                if not data:
                    break
                buf += data
            except socket.timeout:
                continue
        while len(buf) >= 2:
            n = struct.unpack_from("<H", buf, 0)[0]
            if len(buf) < 2 + n:
                break
            body = buf[2:2+n]
            result["packets"].append({"function": body[0], "payload_length": len(body)-1, "payload_hex_prefix": body[1:33].hex()})
            buf = buf[2+n:]
        result["remaining_bytes"] = len(buf)
except Exception as e:
    result["errors"].append(f"client: {e!r}")
finally:
    if sock is not None:
        try: sock.close()
        except Exception: pass
    try: server.stop_server()
    except Exception as e: result["errors"].append(f"stop_server: {e!r}")
    time.sleep(0.2)

out = Path(r"D:\\MDProMod\\docs\\headless-server-smoke-2026-10-11.json")
out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, ensure_ascii=False, indent=2))



