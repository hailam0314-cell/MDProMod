using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

// Minimal YGOPro/MDPro3 TCP client. It intentionally does not implement the
// duel UI or AI; it is a wire-level probe for room and protocol validation.
// Run from a staging directory that contains ygoserver.dll and Data/ files.
internal static class Program
{
    private const ushort DefaultPort = 17911;
    private const ushort ClientVersion = 4962;
    private const byte CtosResponse = 1;
    private const byte CtosUpdateDeck = 2;
    private const byte CtosHandResult = 3;
    private const byte CtosTpResult = 4;
    private const byte CtosPlayerInfo = 16;
    private const byte CtosJoinGame = 18;
    private const byte CtosLeaveGame = 19;
    private const byte CtosSurrender = 20;
    private const byte CtosTimeConfirm = 21;
    private const byte CtosExternalAddress = 23;
    private const byte CtosHsReady = 34;
    private const byte CtosHsNotReady = 35;
    private const byte CtosHsStart = 37;

    private const byte StocGameMsg = 1;
    private const byte StocError = 2;
    private const byte StocSelectHand = 3;
    private const byte StocSelectTp = 4;
    private const byte StocJoinGame = 18;
    private const byte StocTypeChange = 19;
    private const byte StocDuelStart = 21;
    private const byte StocDuelEnd = 22;
    private const byte StocTimeLimit = 24;
    private const byte StocChat = 25;
    private const byte StocHsPlayerEnter = 32;
    private const byte StocHsPlayerChange = 33;
    private const byte StocHsWatchChange = 34;

    private sealed record Options(
        string Host, ushort Port, string Name, string Password, string Deck,
        string? ServerArgs, bool Ready, bool Start, bool AutoResponses,
        int TimeoutMs, string? LogFile);

    private static int Main(string[] args)
    {
        try
        {
            var o = ParseOptions(args);
            using var log = new LogWriter(o.LogFile);
            log.Write($"ygo_tcp_probe start host={o.Host}:{o.Port} name={o.Name} deck={o.Deck}");

            ServerHandle? server = null;
            if (o.ServerArgs is not null)
            {
                server = ServerHandle.Start(o.ServerArgs, log);
                if (!WaitForPort(o.Host, o.Port, o.TimeoutMs, log))
                    throw new InvalidOperationException($"ygoserver did not open {o.Host}:{o.Port}");
            }

            var deck = Deck.Load(o.Deck);
            log.Write($"deck parsed main={deck.Main.Count} extra={deck.Extra.Count} side={deck.Side.Count}");
            using var client = new YgoClient(o.Host, o.Port, o.Name, o.Password, deck, o.TimeoutMs, log);
            client.Connect();
            client.Handshake();
            var end = Stopwatch.StartNew();
            while (end.ElapsedMilliseconds < o.TimeoutMs)
            {
                var packet = client.ReceivePacket(o.TimeoutMs - (int)end.ElapsedMilliseconds);
                if (packet is null) break;
                client.Handle(packet.Value, o);
                if (client.DuelEnded) break;
            }
            log.Write($"probe finished joined={client.Joined} seat={client.Seat} host={client.IsHost} duelStart={client.DuelStarted} duelEnd={client.DuelEnded}");
            server?.Stop();
            return client.Joined ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static Options ParseOptions(string[] a)
    {
        string host = "127.0.0.1", name = "MCProbe", password = "", deck = "";
        ushort port = DefaultPort;
        string? serverArgs = null, log = null;
        bool ready = false, start = false, auto = false;
        int timeout = 15000;
        for (var i = 0; i < a.Length; i++)
        {
            string Next() => ++i < a.Length ? a[i] : throw new ArgumentException($"missing value for {a[i - 1]}");
            switch (a[i].ToLowerInvariant())
            {
                case "--host": host = Next(); break;
                case "--port": port = ushort.Parse(Next()); break;
                case "--name": name = Next(); break;
                case "--password": password = Next(); break;
                case "--deck": deck = Next(); break;
                case "--server-args": serverArgs = Next(); break;
                case "--ready": ready = true; break;
                case "--start": start = true; break;
                case "--auto": auto = true; break;
                case "--timeout": timeout = int.Parse(Next()); break;
                case "--log": log = Next(); break;
                case "--help": PrintHelp(); Environment.Exit(0); break;
                default: throw new ArgumentException($"unknown option: {a[i]}");
            }
        }
        if (string.IsNullOrWhiteSpace(deck)) throw new ArgumentException("--deck is required");
        return new Options(host, port, name, password, deck, serverArgs, ready, start, auto, timeout, log);
    }

    private static void PrintHelp() => Console.WriteLine(@"
Minimal MDPro3/YGOPro TCP probe
  --deck PATH             .ydk deck to submit (required)
  --host HOST --port N    server endpoint (default 127.0.0.1:17911)
  --name NAME             player name
  --password PASS         room password
  --server-args ARGS      call ygoserver.dll start_server with ARGS
  --ready                 send HsReady after TypeChange
  --start                 send HsStart when this client is host and both seats ready
  --auto                  answer SelectHand/SelectTp and surrender on first GameMsg
  --timeout MS             receive timeout (default 15000)
  --log PATH               append wire log to PATH

Typical local server args (Master Rule 5, 8000 LP, 5-card hand):
  17911 -1 5 0 F F F 8000 5 1 180 0
");

    private static bool WaitForPort(string host, ushort port, int timeout, LogWriter log)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeout)
        {
            try
            {
                using var c = new TcpClient();
                var t = c.ConnectAsync(host, port);
                t.Wait(Math.Min(200, timeout - (int)sw.ElapsedMilliseconds));
                if (c.Connected) { log.Write("server port is open"); return true; }
            }
            catch { }
            Thread.Sleep(50);
        }
        return false;
    }

    private sealed class YgoClient : IDisposable
    {
        private readonly TcpClient _tcp = new();
        private readonly NetworkStream _stream;
        private readonly string _name, _password;
        private readonly Deck _deck;
        private readonly int _timeout;
        private readonly LogWriter _log;
        public bool Joined { get; private set; }
        public bool DuelStarted { get; private set; }
        public bool DuelEnded { get; private set; }
        public int Seat { get; private set; } = -1;
        public bool IsHost { get; private set; }

        public YgoClient(string host, ushort port, string name, string password, Deck deck, int timeout, LogWriter log)
        {
            _name = name; _password = password; _deck = deck; _timeout = timeout; _log = log;
            _tcp.ReceiveTimeout = timeout; _tcp.SendTimeout = timeout;
            _tcp.Connect(host, port);
            _stream = _tcp.GetStream();
        }

        public void Connect() => _log.Write("TCP connected");

        public void Handshake()
        {
            Send(CtosExternalAddress, w => { w.Write(0u); WriteUtf16Auto(w, _tcp.Client.RemoteEndPoint?.ToString() ?? "127.0.0.1", 255); });
            Send(CtosPlayerInfo, w => WriteUtf16Fixed(w, _name, 20));
            Send(CtosJoinGame, w =>
            {
                w.Write(ClientVersion);
                w.Write((byte)0xCC); w.Write((byte)0xCC);
                w.Write(0u);
                WriteUtf16Fixed(w, _password, 20);
            });
            _log.Write("sent ExternalAddress, PlayerInfo, JoinGame");
        }

        public Packet? ReceivePacket(int timeout)
        {
            if (timeout <= 0) return null;
            _tcp.ReceiveTimeout = timeout;
            var h = ReadExact(2);
            if (h is null) return null;
            var len = BinaryPrimitives.ReadUInt16LittleEndian(h);
            if (len == 0 || len > 65535) throw new InvalidDataException($"invalid packet length {len}");
            var body = ReadExact(len);
            if (body is null) return null;
            return new Packet(body[0], body.AsSpan(1).ToArray());
        }

        public void Handle(Packet p, Options o)
        {
            _log.Packet("STOC", p.Function, p.Payload);
            switch (p.Function)
            {
                case StocJoinGame:
                    if (p.Payload.Length >= 20) { Joined = true; _log.Write("STOC_JOIN_GAME received"); }
                    break;
                case StocTypeChange:
                    if (p.Payload.Length > 0) { var v = p.Payload[0]; Seat = v & 0xF; IsHost = (v & 0xF0) != 0; _log.Write($"STOC_TYPE_CHANGE seat={Seat} host={IsHost}"); }
                    if (o.Ready) Send(CtosHsReady);
                    break;
                case StocHsPlayerChange:
                    // Status low nibble 9 means ready. HsStart is sent by a
                    // host once the other seat is ready; duplicate starts are
                    // harmless but avoid sending one for our own notification.
                    if (o.Start && IsHost && p.Payload.Length > 0 && (p.Payload[0] & 0xF) == 9 && ((p.Payload[0] >> 4) & 0xF) != Seat) Send(CtosHsStart);
                    break;
                case StocDuelStart:
                    DuelStarted = true; _log.Write("STOC_DUEL_START received"); break;
                case StocDuelEnd:
                    DuelEnded = true; _log.Write("STOC_DUEL_END received"); break;
                case StocSelectHand:
                    if (o.AutoResponses) Send(CtosHandResult, w => w.Write((byte)1)); break;
                case StocSelectTp:
                    if (o.AutoResponses) Send(CtosTpResult, w => w.Write((byte)1)); break;
                case StocTimeLimit:
                    if (o.AutoResponses) Send(CtosTimeConfirm); break;
                case StocGameMsg:
                    if (o.AutoResponses) { _log.Write("auto mode: surrender after first GameMsg"); Send(CtosSurrender); } break;
                case StocError:
                    _log.Write("STOC_ERROR received"); break;
                case StocHsPlayerEnter:
                    if (p.Payload.Length >= 41) _log.Write($"player enter seat={p.Payload[40] & 3}"); break;
                case StocHsWatchChange:
                    if (p.Payload.Length >= 2) _log.Write($"observer count={BinaryPrimitives.ReadUInt16LittleEndian(p.Payload)}"); break;
                case StocChat:
                    _log.Write("chat packet received"); break;
            }
            // The room sends the deck only after JoinGame. This is deliberately
            // done once so the probe mirrors WindBot and MDPro3 clients.
            if (p.Function == StocJoinGame) SendDeck();
        }

        private void SendDeck()
        {
            Send(CtosUpdateDeck, w =>
            {
                w.Write(_deck.Main.Count + _deck.Extra.Count);
                w.Write(_deck.Side.Count);
                foreach (var c in _deck.Main) w.Write(c);
                foreach (var c in _deck.Extra) w.Write(c);
                foreach (var c in _deck.Side) w.Write(c);
            });
            _log.Write("sent UpdateDeck");
        }

        private void Send(byte function, Action<BinaryWriter>? payload = null)
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.Unicode, leaveOpen: true)) { w.Write(function); payload?.Invoke(w); }
            var body = ms.ToArray();
            Span<byte> header = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(header, checked((ushort)body.Length));
            _stream.Write(header); _stream.Write(body); _stream.Flush();
            _log.Packet("CTOS", function, body.AsSpan(1).ToArray());
        }

        private byte[]? ReadExact(int n)
        {
            var b = new byte[n]; var off = 0;
            while (off < n)
            {
                int got;
                try { got = _stream.Read(b, off, n - off); }
                catch (IOException) { return null; }
                if (got <= 0) return null; off += got;
            }
            return b;
        }

        public void Dispose() { try { Send(CtosLeaveGame); } catch { } _tcp.Dispose(); }
    }

    private readonly record struct Packet(byte Function, byte[] Payload);

    private sealed class Deck
    {
        public List<int> Main { get; } = new();
        public List<int> Extra { get; } = new();
        public List<int> Side { get; } = new();
        public static Deck Load(string path)
        {
            var d = new Deck(); var section = 0;
            foreach (var raw in File.ReadLines(path))
            {
                var s = raw.Trim(); if (s.Length == 0) continue;
                if (s.Equals("#main", StringComparison.OrdinalIgnoreCase)) { section = 1; continue; }
                if (s.Equals("#extra", StringComparison.OrdinalIgnoreCase)) { section = 2; continue; }
                if (s.Equals("!side", StringComparison.OrdinalIgnoreCase)) { section = 3; continue; }
                if (s.StartsWith('#')) continue;
                if (!int.TryParse(s, out var code) || code <= 0) continue;
                (section == 1 ? d.Main : section == 2 ? d.Extra : d.Side).Add(code);
            }
            if (d.Main.Count == 0) throw new InvalidDataException("deck has no main cards");
            return d;
        }
    }

    private sealed class LogWriter : IDisposable
    {
        private readonly StreamWriter? _file;
        public LogWriter(string? path) { if (!string.IsNullOrWhiteSpace(path)) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!); _file = new StreamWriter(path, append: true, Encoding.UTF8) { AutoFlush = true }; } }
        public void Write(string s) { var line = $"[{DateTime.Now:O}] {s}"; Console.WriteLine(line); _file?.WriteLine(line); }
        public void Packet(string dir, byte f, byte[] p) => Write($"{dir} fn={f} len={p.Length} hex={Convert.ToHexString(p.AsSpan(0, Math.Min(64, p.Length)))}");
        public void Dispose() => _file?.Dispose();
    }

    private sealed class ServerHandle
    {
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int start_server([MarshalAs(UnmanagedType.LPUTF8Str)] string args);
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        private static extern void stop_server();
        public static ServerHandle Start(string args, LogWriter log)
        {
            log.Write($"starting ygoserver args={args}");
            var t = new Thread(() => { try { var rc = start_server(args); log.Write($"ygoserver returned {rc}"); } catch (Exception e) { log.Write($"ygoserver exception {e.Message}"); } }) { IsBackground = true };
            t.Start(); return new ServerHandle();
        }
        public void Stop() { try { stop_server(); } catch { } }
    }

    private static void WriteUtf16Fixed(BinaryWriter w, string value, int chars)
    {
        var data = Encoding.Unicode.GetBytes(value ?? string.Empty);
        var outb = new byte[chars * 2];
        Array.Fill(outb, (byte)0xCC);
        var n = Math.Min(data.Length, outb.Length - 2);
        Array.Copy(data, outb, n); outb[n] = 0; outb[n + 1] = 0; w.Write(outb);
    }
    private static void WriteUtf16Auto(BinaryWriter w, string value, int maxChars)
    {
        var data = Encoding.Unicode.GetBytes((value ?? string.Empty) + "\0");
        var n = Math.Min(data.Length, maxChars * 2); if (n % 2 != 0) n--;
        w.Write(data, 0, n);
    }
}
