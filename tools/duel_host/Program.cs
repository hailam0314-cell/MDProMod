using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class DuelHostProgram
{
    private const string DefaultRoot = @"D:\MDProMod\tools\ygo_tcp_probe\staging";
    private static readonly object Sync = new();
    private static string _rootPath = DefaultRoot;
    private static DuelCore? _duel;
    private static CancellationTokenSource? _cts;
    private static Task? _runTask;

    public static int Main(string[] args)
    {
        if (args.Contains("--test-splitter"))
        {
            var verified = ClassicMessageSplitter.SelfTest(GetOption(args, "--test-jsonl"));
            Emit(new { type = "splitterTest", passed = true, verified });
            return 0;
        }
        // 时点自检：不该开局的纯逻辑，直接断言"哪个窗口该自动过"。
        if (args.Contains("--timing-selftest")) return RunTimingSelfTest();
        // 选项描述解析自检：--describe-option <desc...>
        // 用来核对"这个描述号翻译成什么文字"，不必真开一局。
        var describeAt = Array.IndexOf(args, "--describe-option");
        if (describeAt >= 0)
        {
            var describeRoot = GetOption(args, "--root") ?? DefaultRoot;
            DuelCore.LoadOptionTexts(describeRoot);
            Console.WriteLine($"文字表：卡片 {DuelCore.CardTextCount} 张，system {DuelCore.SystemStringCount} 条");
            for (var i = describeAt + 1; i < args.Length && !args[i].StartsWith("--"); i++)
            {
                if (!uint.TryParse(args[i], out var description)) continue;
                Console.WriteLine($"{description} -> {DuelCore.DescribeOption(description, 0)}");
            }
            return 0;
        }
        var root = GetOption(args, "--root") ?? DefaultRoot;
        _rootPath = root;
        var smokeAi = GetOption(args, "--smoke-ai");
        if (smokeAi != null)
        {
            return RunAiSmoke(root, smokeAi,
                GetOption(args, "--smoke-ai-name") ?? "AI_BlueEyes",
                GetOption(args, "--smoke-ai-dialog") ?? "cirno.zh-CN",
                uint.TryParse(GetOption(args, "--smoke-seed"), out var smokeSeed) ? smokeSeed : 305419896u,
                GetOption(args, "--smoke-ai-deck0") ?? "Data/Windbot/Decks/AI_Albaz.ydk",
                GetOption(args, "--smoke-ai-name0") ?? "AI_Albaz");
        }
        Emit(new { type = "ready", root });
        // 卡组路径可能带中文：stdin 必须按 UTF-8 解码，不能用系统默认代码页
        using var stdin = new StreamReader(Console.OpenStandardInput(),
            new System.Text.UTF8Encoding(false));
        string? input;
        while ((input = stdin.ReadLine()) is not null)
        {
            var line = input;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                Handle(doc.RootElement, root);
            }
            catch (Exception ex)
            {
                Emit(new { type = "error", message = ex.Message });
            }
        }
        Stop();
        return 0;
    }

    /** 无头验证：双方用真 WindBot；固定核心和 AI 随机种子，打完一局就退出。 */
    private static int RunAiSmoke(string root, string aiDeckFile, string aiDeckName, string aiDialog,
                                  uint seed, string deck0File, string deck0Name)
    {
        Emit(new { type = "ready", root, smokeAi = aiDeckFile, deck0 = deck0File, seed });
        var seat1 = new WindBotOptions(1, aiDeckName, aiDeckName, aiDeckFile, aiDialog);
        var seat0 = new WindBotOptions(0, "MDProBotA", deck0Name, deck0File, aiDialog);
        using var duel = new DuelCore(root, deck0File, aiDeckFile, true,
            seed, new[] { seat1, seat0 });
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        duel.Run(cts.Token);
        return 0;
    }

    private static void Handle(JsonElement command, string root)
    {
        var name = command.TryGetProperty("cmd", out var c) ? c.GetString() : null;
        switch (name)
        {
            case "start":
                Stop();
                var d0 = command.TryGetProperty("deck0", out var a) ? a.GetString() : null;
                var d1 = command.TryGetProperty("deck1", out var b) ? b.GetString() : null;
                var auto = command.TryGetProperty("auto", out var autoProp)
                           && autoProp.ValueKind == JsonValueKind.True;
                var autoPassChain = command.TryGetProperty("autoPassChain", out var passProp)
                                   && passProp.ValueKind == JsonValueKind.True;
                var seed = command.TryGetProperty("seed", out var seedProp)
                           && seedProp.ValueKind == JsonValueKind.Number
                    ? seedProp.GetUInt32()
                    : (uint)Environment.TickCount;
                WindBotOptions? ai = null;
                if (command.TryGetProperty("ai", out var aiProp) && aiProp.ValueKind == JsonValueKind.True)
                {
                    var seat = command.TryGetProperty("aiSeat", out var seatProp)
                               && seatProp.ValueKind == JsonValueKind.Number ? seatProp.GetInt32() : 1;
                    var botName = command.TryGetProperty("aiName", out var nameProp) ? nameProp.GetString() : "MDProBot";
                    var deck = command.TryGetProperty("aiDeck", out var deckProp) ? deckProp.GetString() : null;
                    var deckFile = command.TryGetProperty("aiDeckFile", out var fileProp) ? fileProp.GetString() : null;
                    var dialog = command.TryGetProperty("aiDialog", out var dialogProp) ? dialogProp.GetString() : "default";
                    ai = new WindBotOptions(seat, botName ?? "MDProBot", deck ?? "", deckFile ?? "", dialog ?? "default");
                }
                var aiList = ai == null ? Array.Empty<WindBotOptions>() : new[] { ai };
                _duel = new DuelCore(root, d0, d1, auto, seed, aiList, autoPassChain);
                // 开局就带上玩家选的时点（之后还能用 timing 命令改）
                var startTiming = command.TryGetProperty("timing", out var startTimingProp)
                    ? startTimingProp.GetString() : null;
                _duel.SetTiming(startTiming);
                Emit(new { type = "started", deck0 = d0, deck1 = d1 });
                _cts = new CancellationTokenSource();
                var duel = _duel;
                var cts = _cts;
                _runTask = Task.Run(() => duel.Run(cts.Token), cts.Token);
                break;
            case "response":
                _duel?.SetInteger(unchecked((uint)command.GetProperty("value").GetInt32()));
                break;
            case "responseBytes":
                _duel?.SetBytes(Convert.FromHexString(command.GetProperty("hex").GetString() ?? ""));
                break;
            case "stop":
                Stop();
                break;
            case "timing":
                // 时点：all = 每个优先权窗口都停下问；default = 现在的行为；off = 可选的都不问
                var mode = command.TryGetProperty("mode", out var modeProp) ? modeProp.GetString() : "default";
                _duel?.SetTiming(mode);
                break;
            case "surrender":
                // 玩家认输：座位默认 0（模组里近半场就是人类那边）
                var quitSeat = command.TryGetProperty("player", out var quitProp)
                               && quitProp.ValueKind == JsonValueKind.Number ? quitProp.GetInt32() : 0;
                _duel?.Surrender(quitSeat);
                break;
            default:
                Emit(new { type = "error", message = $"unknown cmd: {name}" });
                break;
        }
    }

    private static void Stop()
    {
        var cts = _cts;
        var duel = _duel;
        var task = _runTask;
        _cts = null;
        _duel = null;
        _runTask = null;
        try { cts?.Cancel(); } catch { }
        duel?.CancelWait();
        try { task?.Wait(2000); } catch { }
        duel?.Dispose();
        cts?.Dispose();
    }

    private static void Emit(object value)
    {
        lock (Sync)
        {
            Console.WriteLine(JsonSerializer.Serialize(value));
            Console.Out.Flush();
        }
    }

    private static string? GetOption(string[] args, string key)
    {
        var i = Array.IndexOf(args, key);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /** 时点决策的自检（main 风格，和模组那边的离线测试一个套路）。 */
    private static int RunTimingSelfTest()
    {
        var assertions = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception("自检失败：" + message);
        }
        bool Pass(int function, bool emptyChain, bool forcedChain, int timing, bool autoPassChain,
                  string expected)
        {
            var result = DuelCore.ShouldAutoPass(function, emptyChain, forcedChain, timing, autoPassChain,
                out var reason);
            Check(result == (expected.Length > 0), $"{function}/{emptyChain}/{timing} 结果不对（{result}）");
            Check(reason == expected, $"{function}/{emptyChain}/{timing} 原因应为「{expected}」实际「{reason}」");
            return result;
        }

        // default：空连锁窗口自动过，有卡可接的窗口要问玩家，可选发动要问玩家
        Pass(16, true, false, DuelCore.TimingDefaultValue, false, "empty-chain");
        Pass(16, false, false, DuelCore.TimingDefaultValue, false, "");
        Pass(12, false, false, DuelCore.TimingDefaultValue, false, "");
        // all：连空窗口都停下来问
        Pass(16, true, false, DuelCore.TimingAllValue, false, "");
        Pass(16, false, false, DuelCore.TimingAllValue, false, "");
        Pass(12, false, false, DuelCore.TimingAllValue, false, "");
        // off：连锁一律放弃、可选发动直接答否
        Pass(16, true, false, DuelCore.TimingOffValue, false, "timing-off");
        Pass(16, false, false, DuelCore.TimingOffValue, false, "timing-off");
        Pass(12, false, false, DuelCore.TimingOffValue, false, "timing-off-effect");
        // off 也不能替玩家放弃"必须接"的连锁窗口（会被核心判非法）
        Pass(16, false, true, DuelCore.TimingOffValue, false, "");
        Pass(12, false, true, DuelCore.TimingOffValue, false, "timing-off-effect");
        // off 不碰"必须回答"的窗口（13 是和否确认、11 是阶段推进）
        Pass(13, false, false, DuelCore.TimingOffValue, false, "");
        Pass(11, false, false, DuelCore.TimingOffValue, false, "");
        // autoPassChain 是给"没有 UI"用的兜底，优先于时点
        Pass(16, false, false, DuelCore.TimingAllValue, true, "auto-pass-chain");
        Check(DuelCore.TimingOf("all") == DuelCore.TimingAllValue
              && DuelCore.TimingOf("off") == DuelCore.TimingOffValue
              && DuelCore.TimingOf("default") == DuelCore.TimingDefaultValue
              && DuelCore.TimingOf("乱七八糟") == DuelCore.TimingDefaultValue, "模式名解析");
        Emit(new { type = "timingSelftest", passed = true, assertions });
        return 0;
    }

    private sealed unsafe class DuelCore : IDisposable
    {
        private readonly string _root;
        private readonly Dictionary<uint, CardInfo> _cards = new();
        /** 选项描述用的文字表（cards.cdb 的 texts + strings.conf 的 !system），整进程加载一次。 */
        private static readonly Dictionary<uint, string[]> _cardTexts = new();
        private static readonly Dictionary<int, string> _systemStrings = new();
        private static bool _optionTextsLoaded;
        internal static int CardTextCount => _cardTexts.Count;
        internal static int SystemStringCount => _systemStrings.Count;
        private readonly Dictionary<string, (IntPtr Ptr, int Length)> _scripts = new(StringComparer.OrdinalIgnoreCase);
        private readonly IntPtr _buffer = Marshal.AllocHGlobal(0x20000);
        private readonly ScriptReader _scriptReader;
        private readonly CardReader _cardReader;
        private readonly MessageHandler _messageHandler;
        private readonly object _gate = new();
        private IntPtr _duel;
        private bool _disposed;
        private bool _waiting;
        private byte[]? _lastPromptBytes;
        private int _lastPromptLength;
        private readonly bool _auto;
        private readonly bool _autoPassChain;
        /** 时点模式：见 Timing*Value，命令线程写、决斗线程读，所以是 volatile int。 */
        private volatile int _timing = TimingDefaultValue;
        /** 认输的座位；-1 = 没人投降。 */
        private volatile int _surrenderedBy = -1;
        internal const int TimingAllValue = 0;
        internal const int TimingDefaultValue = 1;
        internal const int TimingOffValue = 2;
        private readonly uint _seed;
        private readonly List<WindBotAi> _ais = new();
        private int _aiRetries;
        /** 提示序号（自增）：客户端用它区分"新提示"与"RETRY 后重发的同一提示"。 */
        private long _promptSeq;
        private byte _autoAnswerFunction;
        private readonly ManualResetEventSlim _response = new(false);
        private int _endedEmitted;

        public DuelCore(string root, string? deck0, string? deck1, bool auto, uint seed,
                        IReadOnlyList<WindBotOptions>? aiOptions = null, bool autoPassChain = false)
        {
            _root = root;
            _auto = auto;
            _autoPassChain = autoPassChain;
            _seed = seed;
            LoadCards();
            LoadScripts();
            LoadOptionTexts(_root);
            _scriptReader = ReadScript;
            _cardReader = ReadCard;
            _messageHandler = (_, _) => 0;
            Native.SetCallbacks(_scriptReader, _cardReader, _messageHandler);
            _duel = Native.CreateDuel(seed);
            if (_duel == IntPtr.Zero) throw new InvalidOperationException("create_duel returned null");
            Native.SetPlayerInfo(_duel, 0, 8000, 5, 1);
            Native.SetPlayerInfo(_duel, 1, 8000, 5, 1);
            AddDeck(deck0 ?? "starter.ydk", 0);
            AddDeck(deck1 ?? "starter.ydk", 1);
            if (aiOptions != null)
            {
                WindBotAi.SetRandomSeed(seed);
                foreach (var options in aiOptions)
                {
                    var bridge = new WindBotAi(options);
                    _ais.Add(bridge);
                    Emit(new
                    {
                        type = "ai",
                        ready = true,
                        seat = bridge.Seat,
                        deck = options.Deck,
                        seeded = bridge.SeededCards,
                    });
                }
            }
        }

        public void Run(CancellationToken token)
        {
            try
            {
                Native.StartDuel(_duel, 0);
                var buffer = new byte[0x20000];
                while (!token.IsCancellationRequested && !_disposed)
                {
                    if (_surrenderedBy >= 0)
                    {
                        EmitSurrender();
                        return;
                    }
                    var result = Native.Process(_duel);
                    var length = (int)(result & 0x0fffffff);
                    var state = result & 0xf0000000;
                    if (length > 0)
                    {
                        Native.GetMessage(_duel, _buffer);
                        Marshal.Copy(_buffer, buffer, 0, length);
                        var messages = ClassicMessageSplitter.Split(buffer.AsSpan(0, length));
                        foreach (var message in messages)
                        {
                            var messageBuffer = message.Bytes;
                            var messageLength = messageBuffer.Length;
                            try
                            {
                            EmitMessage(messageBuffer, messageLength, result);
                            if (!message.Framed)
                            {
                                // 帧坏掉只丢这一批：不要因为一条对不上长度的消息就结束整局
                                Emit(new { type = "warn", message = "Unknown or truncated core message; skipping the rest of this batch", function = messageBuffer[0], length = messageLength });
                                break;
                            }
                            if (NeedsResponse(messageBuffer[0]))
                            {
                                // The multiplayer server normally sends query_field_card
                                // refreshes before selection prompts. A direct-core client
                                // needs these as well: SetId alone does not update AI Type,
                                // ATK/DEF, status, overlay materials or public hand state.
                                RefreshAiState();
                            }
                            foreach (var bridge in _ais)
                            {
                                // A selection is private to the player being asked. Feeding
                                // it to both bots accumulates stale responses on the other seat.
                                if (NeedsResponse(messageBuffer[0])
                                    && bridge.Seat != ReadAskedPlayer(messageBuffer, messageLength)) continue;
                                try { bridge.FeedMessage(messageBuffer, messageLength); }
                                catch (Exception ex)
                                {
                                    Emit(new { type = "error", message = "AI feed failed: " + ex.Message, stack = ex.ToString() });
                                }
                            }
                            if (messageBuffer[0] == 1)
                            {
                                _aiRetries++;
                                // 关键：核心在 RETRY 之后**自己会把原提示重发一遍**，
                                // 我们这边的分支只需要记账 + 放行，绝不能再主动重发——
                                // 否则同一条提示会出现两次，界面和引擎从此对不上号。
                                if (!_auto && !_autoPassChain)
                                {
                                    if (_aiRetries > 60)
                                    {
                                        Emit(new { type = "error", message = "连续应答非法，停止对局" });
                                        return;
                                    }
                                    Emit(new { type = "retry", message = "上次应答被核心判为非法，请重新选择" });
                                    continue;
                                }
                                if (_aiRetries > 5)
                                {
                                    Emit(new { type = "error", message = "AI 连续应答非法，停止对局" });
                                    return;
                                }
                                AutoAnswer(messageBuffer, messageLength);
                                continue;
                            }
                            if (NeedsResponse(messageBuffer[0]))
                            {
                                var asked = _ais.Find(a => a.Seat == ReadAskedPlayer(messageBuffer, messageLength));
                                if (asked != null)
                                {
                                    var responses = asked.DrainResponses();
                                    if (responses.Count > 0)
                                    {
                                        ApplyAiResponse(responses[responses.Count - 1]);
                                        _aiRetries = 0;
                                    }
                                    else
                                    {
                                        AutoAnswer(messageBuffer, messageLength);
                                    }
                                    Emit(new
                                    {
                                        type = "ai",
                                        function = messageBuffer[0],
                                        name = GameMessageName(messageBuffer[0]),
                                        seat = asked.Seat,
                                        responses = responses.Count,
                                        prompt = Convert.ToHexString(messageBuffer.AsSpan(0, Math.Min(messageLength, 24))),
                                        reply = responses.Count > 0
                                            ? Convert.ToHexString(responses[responses.Count - 1])
                                            : "",
                                        state = messageBuffer[0] == 11 ? asked.DumpState() : null,
                                    });
                                    continue;
                                }
                                if (_auto)
                                {
                                    AutoAnswer(messageBuffer, messageLength);
                                    continue;
                                }
                                var function = messageBuffer[0];
                                var emptyChain = function == 16
                                                 && IsEmptyChainWindow(messageBuffer, messageLength);
                                var forcedChain = function == 16 && !emptyChain
                                                  && IsForcedChainWindow(messageBuffer, messageLength);
                                if (ShouldAutoPass(function, emptyChain, forcedChain, _timing, _autoPassChain,
                                        out var passReason))
                                {
                                    // 空连锁窗口默认自动放弃（时点=all 时也停下来问）；
                                    // autoPassChain 打开时"有卡可接"的窗口也放弃（没有 UI 时的兜底）；
                                    // 时点=off 时可选效果直接答否（和 MD 的「発動確認オフ」一个意思）。
                                    AutoAnswer(messageBuffer, messageLength);
                                    Emit(new
                                    {
                                        type = "autoPass",
                                        function,
                                        name = GameMessageName(function),
                                        seat = ReadAskedPlayer(messageBuffer, messageLength),
                                        reason = passReason,
                                    });
                                    continue;
                                }
                                Dictionary<string, object?> prompt;
                                try
                                {
                                    prompt = BuildPrompt(messageBuffer, messageLength);
                                }
                                catch (Exception ex)
                                {
                                    // 解析失败要说出来，不能让整局静默结束
                                    Emit(new
                                    {
                                        type = "error",
                                        message = $"prompt build failed for {GameMessageName(messageBuffer[0])} "
                                                  + $"({messageLength} bytes): {ex.Message}",
                                        stack = ex.ToString(),
                                    });
                                    prompt = new Dictionary<string, object?>
                                    {
                                        ["type"] = "prompt",
                                        ["function"] = (int)messageBuffer[0],
                                        ["name"] = GameMessageName(messageBuffer[0]),
                                        ["waiting"] = true,
                                        ["summary"] = "unparsed",
                                        ["responseKind"] = "integer",
                                        ["raw"] = Convert.ToHexString(messageBuffer.AsSpan(0, messageLength)),
                                    };
                                }
                                lock (_gate) _waiting = true;
                                _lastPromptBytes = messageBuffer;
                                _lastPromptLength = messageLength;
                                // 每条提示给一个自增序号：客户端据此区分"新提示"和"RETRY 后重发的同一提示"，
                                // 不然形状一样的两次提问会被当成重复而不再弹界面。
                                prompt["seq"] = ++_promptSeq;
                                Emit(new { type = "state", waiting = true, function = messageBuffer[0] });
                                Emit(prompt);
                                _response.Wait(token);
                                _response.Reset();
                                if (_surrenderedBy >= 0)
                                {
                                    EmitSurrender();
                                    return;
                                }
                            }
                            if (messageBuffer[0] is 5 or 200) return;
                            }
                            catch (Exception ex)
                            {
                                // 任何解析/刷新失败都要报出来，否则只会看到对局突然结束
                                Emit(new
                                {
                                    type = "error",
                                    message = $"message handling failed for {GameMessageName(messageBuffer[0])}: {ex.Message}",
                                    stack = ex.ToString(),
                                });
                                return;
                            }
                        }
                    }
                    else if (state == 0x20000000) break;
                    else Thread.Yield();
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                if (Interlocked.Exchange(ref _endedEmitted, 1) == 0)
                    Emit(new { type = "ended" });
            }
        }

        public void CancelWait() => _response.Set();

        /** 时点模式名 → 内部值：all / default / off（认不出来的一律当 default）。 */
        internal static int TimingOf(string? mode) => mode?.Trim().ToLowerInvariant() switch
        {
            "all" => TimingAllValue,
            "off" => TimingOffValue,
            _ => TimingDefaultValue,
        };

        /** 时点模式：all / default / off。 */
        public void SetTiming(string? mode) => _timing = TimingOf(mode);

        /**
         * 玩家认输：标记一下并唤醒还在等应答的提示，Run 里紧接着收尾（判对面赢）。
         * 核心没有 surrender 接口，所以这里由宿主动结束这一局——和决斗正常结束走同一条路。
         */
        public void Surrender(int seat)
        {
            if (seat < 0 || seat > 1) return;
            _surrenderedBy = seat;
            _response.Set();
        }

        /**
         * 该不该帮玩家自动过掉这个窗口（纯逻辑，--timing-selftest 直接断言）：
         * <list type="bullet">
         *   <item>16 = 选连锁：空窗口默认过（all 时也要问）；off 时连有卡可接的也过，
         *         但**强制发动**的候选不能替玩家放弃（会被核心判非法）；</item>
         *   <item>12 = 可选效果要不要发动：off 时直接答"不发动"。</item>
         * </list>
         */
        internal static bool ShouldAutoPass(int function, bool emptyChain, bool forcedChain, int timing,
                                            bool autoPassChain, out string reason)
        {
            if (function == 16 && (autoPassChain || (timing == TimingOffValue && !forcedChain)))
            {
                reason = autoPassChain ? "auto-pass-chain" : "timing-off";
                return true;
            }
            if (function == 16 && emptyChain && timing != TimingAllValue)
            {
                reason = "empty-chain";
                return true;
            }
            if (function == 12 && timing == TimingOffValue)
            {
                reason = "timing-off-effect";
                return true;
            }
            reason = "";
            return false;
        }

        /** 认输的收尾：发一条标准 Win 消息（Java 侧只认这条判胜负），然后让 Run 退出。 */
        private void EmitSurrender()
        {
            var winner = 1 - _surrenderedBy;
            if (winner < 0 || winner > 1) winner = 1;
            Emit(new { type = "surrendered", by = _surrenderedBy, winner });
            Emit(new
            {
                type = "message",
                function = 5,
                name = "Win",
                length = 3,
                result = 0,
                winner = (byte)winner,
                hex = $"05{winner:X2}00",
            });
        }

        private void RefreshAiState()
        {
            foreach (var player in new[] { 0, 1 })
            foreach (var location in new[] { 2, 4, 8, 16, 32, 64 })
            {
                var queryLength = Native.QueryField(_duel, player, location, 0xefffff, _buffer, 0);
                if (queryLength <= 0 || queryLength > 0x20000) continue;
                var query = new byte[queryLength + 3];
                query[0] = 6;
                query[1] = (byte)player;
                query[2] = (byte)location;
                Marshal.Copy(_buffer, query, 3, queryLength);
                // Server-private authoritative query. Minecraft filters card identity
                // per viewer before sending any public entity / animation packet.
                // This updates public-hand status without manufacturing a motion.
                EmitMessage(query, query.Length, 0);
                foreach (var bridge in _ais)
                {
                    // 对手的卡组与额外卡组是隐藏堆：AI 模型里没有这些记录，
                    // 强行喂合成查询会让它读越界并带走整局，这里直接跳过。
                    if (bridge.Seat != player && (location == 1 || location == 64)) continue;
                    var visible = (byte[])query.Clone();
                    if (bridge.Seat != player && location != 16)
                    {
                        for (var offset = 3; offset + 4 <= visible.Length;)
                        {
                            var recordLength = BitConverter.ToInt32(visible, offset);
                            if (recordLength < 4 || recordLength > visible.Length - offset) break;
                            // Full query flags always include Code + Position before
                            // all optional fields. Opponents see only face-up records.
                            if (recordLength >= 16 && (visible[offset + 15] & 5) == 0)
                                Array.Clear(visible, offset + 4, recordLength - 4);
                            offset += recordLength;
                        }
                    }
                    try
                    {
                        bridge.FeedMessage(visible, visible.Length);
                    }
                    catch (Exception ex)
                    {
                        // 一条合成查询喂不动 AI 不能带走整局：记一条、跳过
                        Emit(new
                        {
                            type = "warn",
                            message = $"AI query feed failed (player={player} location={location}): {ex.Message}",
                            records = (queryLength) / 1,
                        });
                    }
                }
            }
        }

        public void SetInteger(uint value)
        {
            lock (_gate)
            {
                if (!_waiting) return;
                Native.SetResponseInteger(_duel, value);
                _waiting = false;
            }
            _response.Set();
            Emit(new { type = "state", waiting = false });
        }

        public void SetBytes(byte[] value)
        {
            lock (_gate)
            {
                if (!_waiting || value.Length > 64) return;
                var ptr = Marshal.AllocHGlobal(64);
                try { Marshal.Copy(value, 0, ptr, value.Length); Native.SetResponseBytes(_duel, ptr); }
                finally { Marshal.FreeHGlobal(ptr); }
                _waiting = false;
            }
            _response.Set();
            Emit(new { type = "state", waiting = false });
        }

        private void SendInt(int value)
        {
            Native.SetResponseInteger(_duel, unchecked((uint)value));
            _waiting = false;
            Emit(new { type = "response", function = _autoAnswerFunction, value });
        }

        private void SendBytes(byte[] value)
        {
            var ptr = Marshal.AllocHGlobal(Math.Max(64, value.Length));
            try
            {
                Marshal.Copy(value, 0, ptr, value.Length);
                Native.SetResponseBytes(_duel, ptr);
            }
            finally { Marshal.FreeHGlobal(ptr); }
            _waiting = false;
            Emit(new { type = "responseBytes", function = _autoAnswerFunction, hex = Convert.ToHexString(value) });
        }

        private static byte FirstPosition(byte mask)
        {
            foreach (var p in new byte[] { 1, 4, 8, 2 })
                if ((mask & p) != 0) return p;
            return 1;
        }

        private static byte[] SelectFirst(int count, int min, int max)
        {
            if (count <= 0 || max <= 0) return new byte[] { 0 };
            min = Math.Clamp(min, 0, Math.Min(max, count));
            var result = new byte[min + 1];
            result[0] = (byte)min;
            for (var i = 0; i < min; i++) result[i + 1] = (byte)i;
            return result;
        }

        private static byte[] SelectTribute(BinaryReader reader, int count, int min, int max)
        {
            var releases = new byte[count];
            for (var i = 0; i < count; i++)
            {
                reader.ReadUInt32();
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();
                releases[i] = reader.ReadByte();
            }
            max = Math.Min(Math.Max(min, max), count);
            var chosen = new List<byte>();
            var total = 0;
            for (var i = 0; i < count && chosen.Count < max && total < min; i++)
            {
                chosen.Add((byte)i);
                total += releases[i];
            }
            var result = new byte[chosen.Count + 1];
            result[0] = (byte)chosen.Count;
            for (var i = 0; i < chosen.Count; i++) result[i + 1] = chosen[i];
            return result;
        }

        private static byte[] SelectPlace(byte player, byte count, uint flag)
        {
            var result = new byte[Math.Max(1, (int)count) * 3];
            var used = new HashSet<int>();
            for (var n = 0; n < Math.Max(1, (int)count); n++)
            {
                byte chosenPlayer = 0, chosenLocation = 4, chosenSequence = 0;
                var found = false;
                for (var p = 0; p < 2 && !found; p++)
                for (var l = 0; l < 2 && !found; l++)
                for (var s = 0; s < 7 && !found; s++)
                {
                    var bit = s + (p == player ? 0 : 16) + (l == 0 ? 0 : 8);
                    if ((flag & (1u << bit)) == 0 || used.Contains(bit)) continue;
                    chosenPlayer = (byte)p;
                    chosenLocation = (byte)(l == 0 ? 4 : 8);
                    chosenSequence = (byte)s;
                    used.Add(bit);
                    found = true;
                }
                var offset = n * 3;
                result[offset] = chosenPlayer;
                result[offset + 1] = chosenLocation;
                result[offset + 2] = chosenSequence;
            }
            return result;
        }

        private void AutoAnswer(byte[] buffer, int length)
        {
            using var stream = new MemoryStream(buffer, 0, length, false);
            using var reader = new BinaryReader(stream);
            var type = reader.ReadByte();
            _autoAnswerFunction = type;
            switch (type)
            {
                case 10:
                    if (stream.Position + 2 <= stream.Length)
                    {
                        reader.ReadByte();
                        var activate = reader.ReadByte();
                        reader.ReadBytes(11 * activate);
                        var attack = reader.ReadByte();
                        reader.ReadBytes(8 * attack);
                        var main2 = reader.ReadByte();
                        reader.ReadByte();
                        SendInt(attack > 0 ? 1 : main2 != 0 ? 2 : 3);
                    }
                    break;
                case 11:
                    if (stream.Position < stream.Length)
                    {
                        reader.ReadByte();
                        for (var i = 0; i < 5; i++)
                        {
                            var n = reader.ReadByte();
                            reader.ReadBytes(7 * n);
                        }
                        var actions = reader.ReadByte();
                        reader.ReadBytes(11 * actions);
                        var battlePhase = reader.ReadByte();
                        reader.ReadByte();
                        if (stream.Position < stream.Length) reader.ReadByte();
                        SendInt(battlePhase != 0 ? 6 : 7);
                    }
                    break;
                case 12:
                    if (stream.Position + 13 <= stream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadUInt32();
                        reader.ReadUInt32();
                        reader.ReadUInt32();
                        SendInt(0);
                    }
                    break;
                case 13:
                    if (stream.Position + 5 <= stream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadUInt32();
                        SendInt(0);
                    }
                    break;
                case 14:
                    if (stream.Position + 2 <= stream.Length)
                    {
                        reader.ReadByte();
                        var n = reader.ReadByte();
                        reader.ReadBytes(4 * n);
                        SendInt(0);
                    }
                    break;
                case 15:
                    if (stream.Position + 5 <= stream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadByte();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        var n = reader.ReadByte();
                        reader.ReadBytes(8 * n);
                        SendBytes(SelectFirst(n, min, max));
                    }
                    break;
                case 16:
                    if (stream.Position + 11 <= stream.Length)
                    {
                        reader.ReadByte();
                        var n = reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadUInt32();
                        reader.ReadUInt32();
                        var forced = false;
                        for (var i = 0; i < n; i++)
                        {
                            reader.ReadByte();
                            forced |= reader.ReadByte() != 0;
                            reader.ReadBytes(12);
                        }
                        SendInt(n == 0 || !forced ? -1 : 0);
                    }
                    break;
                case 18:
                case 24:
                    if (stream.Position + 6 <= stream.Length)
                    {
                        var player = reader.ReadByte();
                        var count = reader.ReadByte();
                        var flag = reader.ReadUInt32();
                        SendBytes(SelectPlace(player, count, flag));
                    }
                    break;
                case 19:
                    if (stream.Position + 6 <= stream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadUInt32();
                        var mask = reader.ReadByte();
                        SendInt(FirstPosition(mask));
                    }
                    break;
                case 20:
                    if (stream.Position + 5 <= stream.Length)
                    {
                        reader.ReadByte();
                        var cancel = reader.ReadByte();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        var n = reader.ReadByte();
                        var result = SelectTribute(reader, n, min, max);
                        if (result[0] < min && cancel != 0) SendInt(-1);
                        else SendBytes(result);
                    }
                    break;
                case 22:
                    if (stream.Position + 6 <= stream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadUInt16();
                        var quantity = reader.ReadUInt16();
                        var n = reader.ReadByte();
                        var counters = new ushort[n];
                        for (var i = 0; i < n; i++)
                        {
                            reader.ReadUInt32();
                            reader.ReadByte();
                            reader.ReadByte();
                            reader.ReadByte();
                            counters[i] = reader.ReadUInt16();
                        }
                        var output = new byte[n * 2];
                        var remaining = quantity;
                        for (var i = 0; i < n && remaining > 0; i++)
                        {
                            var value = (ushort)Math.Min(counters[i], remaining);
                            output[2 * i] = (byte)value;
                            output[2 * i + 1] = (byte)(value >> 8);
                            remaining -= value;
                        }
                        SendBytes(output);
                    }
                    break;
                case 23:
                    if (stream.Position + 8 <= stream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadUInt32();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        if (max == 0) max = 99;
                        var must = reader.ReadByte();
                        reader.ReadBytes(11 * must);
                        var n = reader.ReadByte();
                        reader.ReadBytes(11 * n);
                        var total = Math.Min(255, Math.Max((int)must, (int)min));
                        var response = new byte[total + 1];
                        response[0] = (byte)total;
                        for (var i = 0; i < total; i++) response[i + 1] = (byte)(i < must ? 0 : i - must);
                        SendBytes(response);
                    }
                    break;
                case 25:
                    if (stream.Position + 2 <= stream.Length)
                    {
                        reader.ReadByte();
                        var n = reader.ReadByte();
                        reader.ReadBytes(7 * n);
                        var response = new byte[n + 1];
                        response[0] = (byte)n;
                        for (var i = 0; i < n; i++) response[i + 1] = (byte)i;
                        SendBytes(response);
                    }
                    break;
                case 26:
                    if (stream.Position + 5 <= stream.Length)
                    {
                        reader.ReadByte();
                        var finish = reader.ReadByte();
                        var cancel = reader.ReadByte();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        var n = reader.ReadByte();
                        reader.ReadBytes(8 * n);
                        var unknown = reader.ReadByte();
                        reader.ReadBytes(8 * unknown);
                        if ((cancel != 0 || finish != 0) && min == 0) SendInt(-1);
                        else SendBytes(SelectFirst(n, min, max));
                    }
                    break;
                default:
                    SendInt(0);
                    break;
            }
            Emit(new { type = "state", waiting = false, auto = true, function = type });
        }

        private static Dictionary<string, object?> ReadCardEntry(BinaryReader reader, int size, int index = -1)
        {
            var entry = new Dictionary<string, object?>();
            if (size >= 4) entry["code"] = reader.ReadUInt32();
            if (size >= 5) entry["controller"] = reader.ReadByte();
            if (size >= 6) entry["location"] = reader.ReadByte();
            if (size >= 7) entry["sequence"] = reader.ReadByte();
            if (size >= 8) entry["position"] = reader.ReadByte();
            for (var i = 8; i < size; i++) reader.ReadByte();
            if (index >= 0) entry["index"] = index;
            return entry;
        }

        private static List<object> BuildPlaces(byte player, byte count, uint flag)
        {
            var places = new List<object>();
            // 核心给的 flag 是"禁用"掩码：允许的格子是取反之后的那一位
            // （可参考 ocgcore 的 DUEL_SIMPLE_AI 分支，第一步就是 flag = ~flag）。
            flag = ~flag;
            for (var p = 0; p < 2; p++)
            for (var l = 0; l < 2; l++)
            for (var s = 0; s < 7; s++)
            {
                var bit = s + (p == player ? 0 : 16) + (l == 0 ? 0 : 8);
                if ((flag & (1u << bit)) == 0) continue;
                places.Add(new Dictionary<string, object?>
                {
                    ["player"] = p,
                    ["location"] = l == 0 ? 4 : 8,
                    ["sequence"] = s,
                });
            }
            return places;
        }

        private static List<object> BuildPositions(byte mask)
        {
            var positions = new List<object>();
            if ((mask & 1) != 0) positions.Add(new Dictionary<string, object?> { ["index"] = 1, ["label"] = "Face-up Attack" });
            if ((mask & 2) != 0) positions.Add(new Dictionary<string, object?> { ["index"] = 2, ["label"] = "Face-down Attack" });
            if ((mask & 4) != 0) positions.Add(new Dictionary<string, object?> { ["index"] = 4, ["label"] = "Face-up Defense" });
            if ((mask & 8) != 0) positions.Add(new Dictionary<string, object?> { ["index"] = 8, ["label"] = "Face-down Defense" });
            return positions;
        }

        private static Dictionary<string, object?> BuildPrompt(byte[] buffer, int length)
        {
            using var stream = new MemoryStream(buffer, 0, length, false);
            using var reader = new BinaryReader(stream);
            var type = reader.ReadByte();
            var prompt = new Dictionary<string, object?>
            {
                ["type"] = "prompt",
                ["function"] = type,
                ["name"] = GameMessageName(type),
                ["waiting"] = true,
                ["raw"] = Convert.ToHexString(buffer.AsSpan(0, length)),
                ["responseKind"] = "integer",
                ["summary"] = $"{GameMessageName(type)} (function {type})",
            };
            try
            {
                switch (type)
                {
                    case 10:
                    {
                        var player = reader.ReadByte();
                        var activate = reader.ReadByte();
                        var activateCards = new List<object>();
                        for (var i = 0; i < activate; i++) activateCards.Add(ReadCardEntry(reader, 11, i));
                        var attack = reader.ReadByte();
                        var attackCards = new List<object>();
                        for (var i = 0; i < attack; i++) attackCards.Add(ReadCardEntry(reader, 8, i));
                        var main2 = reader.ReadByte();
                        var end = reader.ReadByte();
                        prompt["player"] = player;
                        prompt["toMain2"] = main2 != 0;
                        prompt["toEnd"] = end != 0;
                        prompt["summary"] = $"SelectBattleCmd: activate={activate}, attack={attack}, main2={(main2 != 0 ? "yes" : "no")}, end={(end != 0 ? "yes" : "no")}";
                        // 只列出核心真正允许的阶段转移：2 = 主要阶段 2，3 = 结束阶段
                        var battleOptions = new List<object>();
                        if (main2 != 0)
                            battleOptions.Add(new Dictionary<string, object?> { ["index"] = 2, ["label"] = "Main Phase 2" });
                        if (end != 0)
                            battleOptions.Add(new Dictionary<string, object?> { ["index"] = 3, ["label"] = "End Phase" });
                        prompt["options"] = battleOptions;
                        prompt["activateCards"] = activateCards;
                        prompt["attackCards"] = attackCards;
                        break;
                    }
                    case 11:
                    {
                        var player = reader.ReadByte();
                        // 六个候选列表，顺序与核心的 ctype 一一对应：
                        // 0 召唤 / 1 特殊召唤 / 2 表示变更 / 3 盖怪 / 4 盖魔陷 / 5 发动效果
                        var summon = reader.ReadByte();
                        var summonCards = new List<object>();
                        for (var i = 0; i < summon; i++) summonCards.Add(ReadCardEntry(reader, 7, i));
                        var spsummon = reader.ReadByte();
                        var spsummonCards = new List<object>();
                        for (var i = 0; i < spsummon; i++) spsummonCards.Add(ReadCardEntry(reader, 7, i));
                        var position = reader.ReadByte();
                        var positionCards = new List<object>();
                        for (var i = 0; i < position; i++) positionCards.Add(ReadCardEntry(reader, 7, i));
                        var monsterSet = reader.ReadByte();
                        var monsterSetCards = new List<object>();
                        for (var i = 0; i < monsterSet; i++) monsterSetCards.Add(ReadCardEntry(reader, 7, i));
                        var spellSet = reader.ReadByte();
                        var spellSetCards = new List<object>();
                        for (var i = 0; i < spellSet; i++) spellSetCards.Add(ReadCardEntry(reader, 7, i));
                        var actions = reader.ReadByte();
                        var idleActivateCards = new List<object>();
                        for (var i = 0; i < actions; i++) idleActivateCards.Add(ReadCardEntry(reader, 11, i));
                        var battle = reader.ReadByte();
                        var toEnd = reader.ReadByte();
                        if (stream.Position < stream.Length) reader.ReadByte();
                        prompt["player"] = player;
                        prompt["toBattle"] = battle != 0;
                        prompt["toEnd"] = toEnd != 0;
                        prompt["summonCards"] = summonCards;
                        prompt["spsummonCards"] = spsummonCards;
                        prompt["positionCards"] = positionCards;
                        prompt["monsterSetCards"] = monsterSetCards;
                        prompt["spellSetCards"] = spellSetCards;
                        prompt["activateCards"] = idleActivateCards;
                        prompt["summary"] = $"SelectIdleCmd: summon={summon}, spsummon={spsummon}, position={position}, mset={monsterSet}, sset={spellSet}, activate={actions}, battle={(battle != 0 ? "yes" : "no")}, end={(toEnd != 0 ? "yes" : "no")}";
                        // 只列出核心真正允许的阶段转移：6 = 战斗阶段，7 = 结束阶段
                        var idleOptions = new List<object>();
                        if (battle != 0)
                            idleOptions.Add(new Dictionary<string, object?> { ["index"] = 6, ["label"] = "Battle Phase" });
                        if (toEnd != 0)
                            idleOptions.Add(new Dictionary<string, object?> { ["index"] = 7, ["label"] = "End Phase" });
                        prompt["options"] = idleOptions;
                        break;
                    }
                    case 12:
                    {
                        var player = reader.ReadByte();
                        var code = reader.ReadUInt32();
                        var info = reader.ReadUInt32();
                        var description = reader.ReadUInt32();
                        var controller = (int)(info & 0xff);
                        var location = (int)((info >> 8) & 0xff);
                        var sequence = (int)((info >> 16) & 0xff);
                        prompt["player"] = player;
                        prompt["code"] = code;
                        prompt["controller"] = controller;
                        prompt["location"] = location;
                        prompt["sequence"] = sequence;
                        prompt["description"] = description;
                        prompt["summary"] = $"SelectEffectYN: card={code}, controller={controller}, location={location}, sequence={sequence}";
                        prompt["options"] = new List<object>
                        {
                            new Dictionary<string, object?> { ["index"] = 0, ["label"] = "No" },
                            new Dictionary<string, object?> { ["index"] = 1, ["label"] = "Yes" },
                        };
                        break;
                    }
                    case 13:
                    {
                        var player = reader.ReadByte();
                        var description = reader.ReadUInt32();
                        prompt["player"] = player;
                        prompt["description"] = description;
                        prompt["summary"] = $"SelectYesNo: description={description}";
                        prompt["options"] = new List<object>
                        {
                            new Dictionary<string, object?> { ["index"] = 0, ["label"] = "No" },
                            new Dictionary<string, object?> { ["index"] = 1, ["label"] = "Yes" },
                        };
                        break;
                    }
                    case 14:
                    {
                        var player = reader.ReadByte();
                        var count = reader.ReadByte();
                        var options = new List<object>();
                        for (var i = 0; i < count; i++)
                        {
                            var description = reader.ReadUInt32();
                            options.Add(new Dictionary<string, object?>
                            {
                                ["index"] = i,
                                ["description"] = description,
                                ["label"] = DescribeOption(description, i),
                            });
                        }
                        prompt["player"] = player;
                        prompt["summary"] = $"SelectOption: {count} options";
                        prompt["options"] = options;
                        break;
                    }
                    case 15:
                    {
                        var player = reader.ReadByte();
                        var cancelable = reader.ReadByte();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        var count = reader.ReadByte();
                        var cards = new List<object>();
                        for (var i = 0; i < count; i++) cards.Add(ReadCardEntry(reader, 8, i));
                        prompt["player"] = player;
                        prompt["cancelable"] = cancelable != 0;
                        prompt["min"] = min;
                        prompt["max"] = max;
                        prompt["cards"] = cards;
                        prompt["summary"] = $"SelectCard: choose {min}..{max} from {count} cards";
                        prompt["responseKind"] = "bytes";
                        break;
                    }
                    case 16:
                    {
                        var player = reader.ReadByte();
                        var count = reader.ReadByte();
                        var special = reader.ReadByte();
                        // 头就是 12 字节（含功能号）：功能号 + 玩家 + 候选数 + spe_count
                        // + hint_timing[player] + hint_timing[1-player]。这里没有单独的强制字节。
                        var hintA = reader.ReadUInt32();        // hint_timing[player]
                        var hintB = reader.ReadUInt32();        // hint_timing[1-player]
                        var forced = false;
                        var chainCards = new List<object>();
                        // 每个候选 14 字节：标志(1) + 强制标志(1) + 卡号(4) + 位置(4) + 描述(4)。
                        // 实测（2024 个真实窗口 / 3307 个候选全部命中卡库）：卡号在项内 +2，
                        // 位置在项内 +6（control/location/sequence/position 四字节）。
                        // 帧被截断时有多少读多少，别让整条提示作废。
                        for (var i = 0; i < count && stream.Length - stream.Position >= 14; i++)
                        {
                            var flag = reader.ReadByte();
                            var forcedFlag = reader.ReadByte();
                            var code = reader.ReadUInt32();
                            var info = reader.ReadUInt32();
                            var description = reader.ReadUInt32();
                            var entry = new Dictionary<string, object?>
                            {
                                ["index"] = i,
                                ["flag"] = flag,
                                ["forced"] = forcedFlag != 0,
                                ["code"] = code,
                                ["controller"] = (int)(info & 0xff),
                                ["location"] = (int)((info >> 8) & 0xff),
                                ["sequence"] = (int)((info >> 16) & 0xff),
                                // 表示形式：客户端据此判断"表侧/里侧"，里侧的卡不能给玩家看卡面
                                ["position"] = (int)((info >> 24) & 0xff),
                                ["description"] = description,
                            };
                            forced |= forcedFlag != 0;
                            chainCards.Add(entry);
                        }
                        prompt["player"] = player;
                        prompt["count"] = count;
                        prompt["special"] = special;
                        prompt["forced"] = forced;
                        prompt["cards"] = chainCards;
                        prompt["hints"] = new List<object> { hintA, hintB };
                        prompt["summary"] = $"SelectChain: count={count}, special={special}, "
                                            + $"forced={(forced ? "yes" : "no")}, cards={chainCards.Count}";
                        // options 只是给"人看的"提示；真正的候选列表在 cards 里，下标就是应答值。
                        var chainOptions = new List<object>
                        {
                            new Dictionary<string, object?> { ["index"] = -1, ["label"] = "Pass" },
                        };
                        for (var i = 0; i < chainCards.Count; i++)
                            chainOptions.Add(new Dictionary<string, object?>
                            {
                                ["index"] = i,
                                ["label"] = "Chain #" + (i + 1),
                            });
                        prompt["options"] = chainOptions;
                        break;
                    }
                    case 18:
                    case 24:
                    {
                        var player = reader.ReadByte();
                        var count = reader.ReadByte();
                        var flag = reader.ReadUInt32();
                        var places = BuildPlaces(player, count, flag);
                        prompt["player"] = player;
                        prompt["count"] = count;
                        prompt["places"] = places;
                        prompt["summary"] = $"SelectPlace: choose {count} zones ({places.Count} available)";
                        prompt["responseKind"] = "bytes";
                        break;
                    }
                    case 19:
                    {
                        var player = reader.ReadByte();
                        var code = reader.ReadUInt32();
                        var mask = reader.ReadByte();
                        prompt["player"] = player;
                        prompt["summary"] = $"SelectPosition: card={code}, mask=0x{mask:X2}";
                        prompt["options"] = BuildPositions(mask);
                        break;
                    }
                    case 20:
                    {
                        var player = reader.ReadByte();
                        var cancelable = reader.ReadByte();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        var count = reader.ReadByte();
                        var cards = new List<object>();
                        for (var i = 0; i < count; i++)
                        {
                            var entry = new Dictionary<string, object?>
                            {
                                ["index"] = i,
                                ["code"] = reader.ReadUInt32(),
                                ["controller"] = reader.ReadByte(),
                                ["location"] = reader.ReadByte(),
                                ["sequence"] = reader.ReadByte(),
                                ["releases"] = reader.ReadByte(),
                            };
                            cards.Add(entry);
                        }
                        prompt["player"] = player;
                        prompt["cancelable"] = cancelable != 0;
                        prompt["min"] = min;
                        prompt["max"] = max;
                        prompt["cards"] = cards;
                        prompt["summary"] = $"SelectTribute: choose {min}..{max} from {count} cards";
                        prompt["responseKind"] = "bytes_or_integer_cancel";
                        break;
                    }
                    case 22:
                    {
                        var player = reader.ReadByte();
                        reader.ReadUInt16();
                        var quantity = reader.ReadUInt16();
                        var count = reader.ReadByte();
                        reader.ReadBytes(9 * count);
                        prompt["player"] = player;
                        prompt["quantity"] = quantity;
                        prompt["count"] = count;
                        prompt["summary"] = $"SelectCounter: distribute {quantity} among {count} counters";
                        prompt["responseKind"] = "bytes";
                        break;
                    }
                    case 23:
                    {
                        reader.ReadByte(); // mode, before player
                        var player = reader.ReadByte();
                        reader.ReadUInt32();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        if (max == 0) max = 99;
                        var must = reader.ReadByte();
                        reader.ReadBytes(11 * must);
                        var count = reader.ReadByte();
                        reader.ReadBytes(11 * count);
                        prompt["player"] = player;
                        prompt["min"] = min;
                        prompt["max"] = max;
                        prompt["must"] = must;
                        prompt["count"] = count;
                        prompt["summary"] = $"SelectSum: must={must}, min={min}, max={max}, cards={count}";
                        prompt["responseKind"] = "bytes";
                        break;
                    }
                    case 25:
                    {
                        var player = reader.ReadByte();
                        var count = reader.ReadByte();
                        var cards = new List<object>();
                        for (var i = 0; i < count; i++) cards.Add(ReadCardEntry(reader, 7, i));
                        prompt["player"] = player;
                        prompt["count"] = count;
                        prompt["cards"] = cards;
                        prompt["summary"] = $"SortCard: {count} cards";
                        prompt["responseKind"] = "bytes";
                        break;
                    }
                    case 26:
                    {
                        var player = reader.ReadByte();
                        var finish = reader.ReadByte();
                        var cancel = reader.ReadByte();
                        var min = reader.ReadByte();
                        var max = reader.ReadByte();
                        var count = reader.ReadByte();
                        var selectCards = new List<object>();
                        for (var i = 0; i < count; i++) selectCards.Add(ReadCardEntry(reader, 8, i));
                        var unselect = reader.ReadByte();
                        var unselectCards = new List<object>();
                        for (var i = 0; i < unselect; i++) unselectCards.Add(ReadCardEntry(reader, 8, i));
                        prompt["player"] = player;
                        prompt["finish"] = finish != 0;
                        prompt["cancelable"] = cancel != 0;
                        prompt["min"] = min;
                        prompt["max"] = max;
                        prompt["count"] = count;
                        prompt["cards"] = selectCards;
                        prompt["unselectCards"] = unselectCards;
                        prompt["responseKind"] = "bytes_or_integer_cancel";
                        prompt["summary"] = $"SelectUnselectCard: finish={finish != 0}, cancel={cancel != 0}, min={min}, max={max}, cards={count}";
                        prompt["responseKind"] = "bytes_or_integer_cancel";
                        break;
                    }
                    default:
                    {
                        prompt["summary"] = $"Unhandled prompt function {type} ({GameMessageName(type)}), raw={prompt["raw"]}";
                        prompt["responseKind"] = "integer_or_bytes";
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                prompt["summary"] = $"Prompt parse failed for function {type}: {ex.Message}";
                prompt["responseKind"] = "integer_or_bytes";
            }
            return prompt;
        }

        private static bool NeedsResponse(byte value) => value is 10 or 11 or 12 or 13 or 14 or 15 or 16 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 140 or 141 or 142 or 143;

        /** 询问消息的第一个字段通常是要回答的玩家。 */
        private static int ReadAskedPlayer(byte[] buffer, int length)
        {
            // SelectSum has its mode before the player byte.
            return buffer[0] == 23 && length >= 3 ? buffer[2] : length >= 2 ? buffer[1] : 0;
        }

        /**
         * SelectChain 是不是"空窗口"：没有可接的卡（count == 0）且没有强制项。
         * 这种窗口问玩家没有意义，直接按核心允许的方式放弃。
         */
        private static bool IsEmptyChainWindow(byte[] buffer, int length)
        {
            if (length < 3 || buffer[0] != 16) return false;
            var count = buffer[2];
            return count == 0;
        }

        /**
         * 连锁窗口里有没有"必须接"的候选：每个候选 14 字节，
         * 第 2 个字节就是 forced 标志（和 BuildPrompt 里读的是同一份布局）。
         * 有强制候选时不能替玩家放弃——核心会判非法，然后一直 RETRY。
         */
        private static bool IsForcedChainWindow(byte[] buffer, int length)
        {
            if (length < 12 || buffer[0] != 16) return false;
            var count = buffer[2];
            var offset = 12;              // 功能号 + 玩家 + 候选数 + special + hint[2]
            for (var i = 0; i < count && offset + 14 <= length; i++, offset += 14)
            {
                if (buffer[offset + 1] != 0) return true;
            }
            return false;
        }

        /** 把 WindBot 给出的 CTOS_RESPONSE 参数回填给引擎。 */
        private void ApplyAiResponse(byte[] args)
        {
            if (_duel == IntPtr.Zero || args.Length == 0) return;
            if (args.Length == 4)
            {
                Native.SetResponseInteger(_duel, BitConverter.ToUInt32(args, 0));
                return;
            }
            IntPtr pointer = Marshal.AllocHGlobal(64);
            try
            {
                Marshal.Copy(args, 0, pointer, args.Length);
                Native.SetResponseBytes(_duel, pointer);
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        private void EmitMessage(byte[] buffer, int length, uint result)
        {
            var fn = buffer[0];
            var winner = fn == 5 && length > 1 ? buffer[1] : (byte?)null;
            Emit(new { type = "message", function = fn, name = GameMessageName(fn), length, result, winner, hex = Convert.ToHexString(buffer.AsSpan(0, length)) });
        }

        private void AddDeck(string path, int player)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(_root, path);
            var section = 0;
            var main = new List<int>();
            var extra = new List<int>();
            foreach (var line in File.ReadLines(full))
            {
                var s = line.Trim();
                if (s == "#main") { section = 1; continue; }
                if (s == "#extra") { section = 2; continue; }
                if (s == "!side") { section = 3; continue; }
                if (!int.TryParse(s, out var id) || section is 0 or 3) continue;
                if (section == 2) extra.Add(id); else main.Add(id);
            }
            // 核心不会替我们洗牌：装进去的顺序就是初始顺序，所以这里必须自己洗。
            // 用对局种子派生每名玩家各自的随机流，保证不同开局不同手牌、同一局可复现。
            var random = new Random(unchecked((int)(_seed ^ (uint)(player + 1) * 0x9E3779B9u)));
            for (var i = main.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (main[i], main[j]) = (main[j], main[i]);
            }
            foreach (var id in main)
                Native.NewCard(_duel, (uint)id, (byte)player, (byte)player, 0x01, 0, 0x08);
            foreach (var id in extra)
                Native.NewCard(_duel, (uint)id, (byte)player, (byte)player, 0x40, 0, 0x08);
        }

        private void LoadCards()
        {
            var db = Path.Combine(_root, "Data", "locales", "zh-CN", "cards.cdb");
            if (!File.Exists(db)) db = Path.Combine(_root, "cards.cdb");
            var rc = Sqlite.Open(db, out var connection);
            if (rc != 0) throw new InvalidOperationException($"cards.cdb open failed: {rc}");
            try
            {
                rc = Sqlite.Prepare(connection, "SELECT id,alias,setcode,type,level,attribute,race,atk,def FROM datas", out var statement);
                if (rc != 0) throw new InvalidOperationException($"cards.cdb query failed: {rc}");
                try
                {
                    while (Sqlite.Step(statement) == 100)
                    {
                        _cards[(uint)Sqlite.Int(statement, 0)] = new CardInfo((uint)Sqlite.Int(statement, 0), (uint)Sqlite.Int(statement, 1), unchecked((ulong)Sqlite.Int64(statement, 2)), (uint)Sqlite.Int(statement, 3), (uint)Sqlite.Int(statement, 4), (uint)Sqlite.Int(statement, 5), (uint)Sqlite.Int(statement, 6), Sqlite.Int(statement, 7), Sqlite.Int(statement, 8));
                    }
                }
                finally { Sqlite.Finalize(statement); }
            }
            finally { Sqlite.Close(connection); }
        }

        /**
         * 选项描述要用的两张表，都是只读参考数据，整进程加载一次：
         *
         *  * cards.cdb 的 texts 表（str1..str16）：卡片脚本里 {@code aux.Stringid(code, i)} 的原文。
         *  * strings.conf 的 !system 段：desc < 10000 的内置词（"通常召唤"这类）。
         *
         * 口径照 MDPro3 的 StringHelper.Get。
         */
        internal static void LoadOptionTexts(string root)
        {
            if (_optionTextsLoaded) return;
            _optionTextsLoaded = true;

            var db = Path.Combine(root, "Data", "locales", "zh-CN", "cards.cdb");
            if (!File.Exists(db)) db = Path.Combine(root, "cards.cdb");
            if (File.Exists(db) && Sqlite.Open(db, out var connection) == 0)
            {
                try
                {
                    var rc = Sqlite.Prepare(connection,
                        "SELECT id,str1,str2,str3,str4,str5,str6,str7,str8,str9,str10,str11,str12,str13,str14,str15,str16 FROM texts",
                        out var statement);
                    if (rc == 0)
                    {
                        try
                        {
                            while (Sqlite.Step(statement) == 100)
                            {
                                var strings = new string[16];
                                for (var i = 0; i < strings.Length; i++)
                                    strings[i] = Sqlite.Text(statement, i + 1) ?? "";
                                _cardTexts[(uint)Sqlite.Int(statement, 0)] = strings;
                            }
                        }
                        finally { Sqlite.Finalize(statement); }
                    }
                }
                finally { Sqlite.Close(connection); }
            }

            var conf = Path.Combine(root, "Data", "locales", "zh-CN", "strings.conf");
            if (!File.Exists(conf)) conf = Path.Combine(root, "strings.conf");
            if (!File.Exists(conf)) return;
            foreach (var line in File.ReadLines(conf, System.Text.Encoding.UTF8))
            {
                if (!line.StartsWith("!system ", StringComparison.Ordinal)) continue;
                var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;
                var idText = parts[1];
                var id = idText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt32(idText, 16) : int.TryParse(idText, out var parsed) ? parsed : -1;
                if (id >= 0) _systemStrings[id] = parts[2];
            }
        }

        /**
         * 把选项描述翻译成文字。口径照 MDPro3 的 StringHelper.Get：
         * desc &lt; 10000 → strings.conf 的 !system；否则 code = desc &gt;&gt; 4、index = desc &amp; 0xf
         * 取该卡的第 index 条字符串。查不到就退回带描述号的占位文字（至少能核对）。
         */
        internal static string DescribeOption(uint description, int index)
        {
            if (description > 0 && description < 10000)
            {
                if (_systemStrings.TryGetValue((int)description, out var system) && system.Length > 0)
                    return system;
            }
            else if (description >= 10000)
            {
                var code = description >> 4;
                var slot = (int)(description & 0xf);
                if (_cardTexts.TryGetValue(code, out var strings)
                    && slot >= 0 && slot < strings.Length && strings[slot].Length > 0)
                    return strings[slot];
            }
            return $"选项 {index + 1}（描述 {description}）";
        }


        private void LoadScripts()
        {
            var zip = Path.Combine(_root, "Data", "script.zip");
            if (!File.Exists(zip)) zip = Path.Combine(_root, "script.zip");
            using var archive = ZipFile.OpenRead(zip);
            foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)))
            {
                using var source = entry.Open(); using var memory = new MemoryStream(); source.CopyTo(memory);
                var bytes = memory.ToArray(); var ptr = Marshal.AllocHGlobal(bytes.Length); Marshal.Copy(bytes, 0, ptr, bytes.Length); _scripts[Normalize(entry.FullName)] = (ptr, bytes.Length);
            }
        }

        private static string Normalize(string name) => name.Replace('\\', '/').TrimStart('.', '/');
        private IntPtr ReadScript(byte* name, int* length) { var key = Normalize(Marshal.PtrToStringUTF8((IntPtr)name) ?? ""); if (!_scripts.TryGetValue(key, out var script)) { *length = 0; return IntPtr.Zero; } *length = script.Length; return script.Ptr; }
        private uint ReadCard(uint code, NativeCardData* output) { *output = default; if (!_cards.TryGetValue(code, out var c)) return 0; output->Code = c.Id; output->Alias = c.Alias; output->Type = c.Type; output->Level = c.Level; output->Attribute = c.Attribute; output->Race = c.Race; var link = (c.Type & 0x4000000) != 0; output->Attack = c.Attack; output->Defense = link ? 0 : c.Defense; output->LinkMarker = link ? unchecked((uint)c.Defense) : 0; for (var i = 0; i < 4; i++) output->Setcode[i] = (ushort)((c.Setcode >> (16 * i)) & 0xffff); return code; }

        public void Dispose()
        {
            if (_disposed) return; _disposed = true; _response.Set(); if (_duel != IntPtr.Zero) Native.EndDuel(_duel); foreach (var script in _scripts.Values) Marshal.FreeHGlobal(script.Ptr); Marshal.FreeHGlobal(_buffer);
        }
    }

    private readonly record struct CardInfo(uint Id, uint Alias, ulong Setcode, uint Type, uint Level, uint Attribute, uint Race, int Attack, int Defense);

    [StructLayout(LayoutKind.Sequential)] private unsafe struct NativeCardData { public uint Code, Alias; public fixed ushort Setcode[16]; public uint Type, Level, Attribute, Race; public int Attack, Defense; public uint LScale, RScale, LinkMarker; }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private unsafe delegate IntPtr ScriptReader(byte* name, int* length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private unsafe delegate uint CardReader(uint code, NativeCardData* data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate uint MessageHandler(IntPtr duel, uint messageType);
    private static string GameMessageName(byte value) => value switch
    {
        1 => "Retry",
        2 => "Hint",
        3 => "Waiting",
        4 => "Start",
        5 => "Win",
        6 => "UpdateData",
        7 => "UpdateCard",
        8 => "RequestDeck",
        10 => "SelectBattleCmd",
        11 => "SelectIdleCmd",
        12 => "SelectEffectYN",
        13 => "SelectYesNo",
        14 => "SelectOption",
        15 => "SelectCard",
        16 => "SelectChain",
        18 => "SelectPlace",
        19 => "SelectPosition",
        20 => "SelectTribute",
        21 => "SortChain",
        22 => "SelectCounter",
        23 => "SelectSum",
        24 => "SelectDisfield",
        25 => "SortCard",
        26 => "SelectUnselectCard",
        30 => "ConfirmDecktop",
        31 => "ConfirmCards",
        32 => "ShuffleDeck",
        33 => "ShuffleHand",
        34 => "RefreshDeck",
        35 => "SwapGraveDeck",
        36 => "ShuffleSetCard",
        37 => "ReverseDeck",
        38 => "DeckTop",
        39 => "ShuffleExtra",
        40 => "NewTurn",
        41 => "NewPhase",
        42 => "ConfirmExtratop",
        50 => "Move",
        53 => "PosChange",
        54 => "Set",
        55 => "Swap",
        56 => "FieldDisabled",
        60 => "Summoning",
        61 => "Summoned",
        62 => "SpSummoning",
        63 => "SpSummoned",
        64 => "FlipSummoning",
        65 => "FlipSummoned",
        70 => "Chaining",
        71 => "Chained",
        72 => "ChainSolving",
        73 => "ChainSolved",
        74 => "ChainEnd",
        75 => "ChainNegated",
        76 => "ChainDisabled",
        80 => "CardSelected",
        81 => "RandomSelected",
        83 => "BecomeTarget",
        90 => "Draw",
        91 => "Damage",
        92 => "Recover",
        93 => "Equip",
        94 => "LpUpdate",
        95 => "Unequip",
        96 => "CardTarget",
        97 => "CancelTarget",
        100 => "PayLpCost",
        101 => "AddCounter",
        102 => "RemoveCounter",
        110 => "Attack",
        111 => "Battle",
        112 => "AttackDisabled",
        113 => "DamageStepStart",
        114 => "DamageStepEnd",
        120 => "MissedEffect",
        121 => "BeChainTarget",
        122 => "CreateRelation",
        123 => "ReleaseRelation",
        130 => "TossCoin",
        131 => "TossDice",
        132 => "RockPaperScissors",
        133 => "HandResult",
        140 => "AnnounceRace",
        141 => "AnnounceAttrib",
        142 => "AnnounceCard",
        143 => "AnnounceNumber",
        144 => "AnnounceCardFilter",
        160 => "CardHint",
        161 => "TagSwap",
        162 => "ReloadField",
        163 => "AiName",
        164 => "ShowHint",
        165 => "PlayerHint",
        170 => "MatchKill",
        180 => "CustomMsg",
        200 => "DuelWinner",
        _ => $"Unknown_{value}",
    };

    private static class Sqlite
    {
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string file, out IntPtr db, int flags, IntPtr vfs);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int sqlite3_prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int n, out IntPtr statement, IntPtr tail);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int sqlite3_step(IntPtr statement);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int sqlite3_column_int(IntPtr statement, int column);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern long sqlite3_column_int64(IntPtr statement, int column);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern IntPtr sqlite3_column_text(IntPtr statement, int column);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int sqlite3_finalize(IntPtr statement);
        [DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)] private static extern int sqlite3_close(IntPtr db);
        [DllImport("kernel32", SetLastError = true)] private static extern bool SetDllDirectory(string lpPathName);
        static Sqlite() { SetDllDirectory(_rootPath); }
        public static int Open(string path, out IntPtr db) => sqlite3_open_v2(path, out db, 1, IntPtr.Zero); public static int Prepare(IntPtr db, string sql, out IntPtr statement) => sqlite3_prepare_v2(db, sql, -1, out statement, IntPtr.Zero); public static int Step(IntPtr statement) => sqlite3_step(statement); public static int Int(IntPtr statement, int col) => sqlite3_column_int(statement, col); public static long Int64(IntPtr statement, int col) => sqlite3_column_int64(statement, col); public static int Finalize(IntPtr statement) => sqlite3_finalize(statement); public static int Close(IntPtr db) => sqlite3_close(db);
        public static string? Text(IntPtr statement, int col)
        {
            var pointer = sqlite3_column_text(statement, col);
            return pointer == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(pointer);
        }
    }

    private static class Native
    {
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void set_script_reader(ScriptReader callback);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void set_card_reader(CardReader callback);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void set_message_handler(MessageHandler callback);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr create_duel(uint seed);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void start_duel(IntPtr duel, int options);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void end_duel(IntPtr duel);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void set_player_info(IntPtr duel, int player, int lp, int start, int draw);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void new_card(IntPtr duel, uint code, byte owner, byte player, byte location, byte sequence, byte position);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern uint process(IntPtr duel);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern int get_message(IntPtr duel, IntPtr buffer);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern int query_field_card(IntPtr duel, int player, int location, int queryFlag, IntPtr buffer, int useCache);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void set_responsei(IntPtr duel, uint value);
        [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)] private static extern void set_responseb(IntPtr duel, IntPtr buffer);
        public static void SetCallbacks(ScriptReader s, CardReader c, MessageHandler m) { set_script_reader(s); set_card_reader(c); set_message_handler(m); }
        public static int QueryField(IntPtr duel, int player, int location, int flag, IntPtr buffer, int cache) => query_field_card(duel, player, location, flag, buffer, cache);
        public static IntPtr CreateDuel(uint s) => create_duel(s); public static void StartDuel(IntPtr d, int o) => start_duel(d, o); public static void EndDuel(IntPtr d) => end_duel(d); public static void SetPlayerInfo(IntPtr d, int p, int lp, int st, int dr) => set_player_info(d, p, lp, st, dr); public static void NewCard(IntPtr d, uint c, byte o, byte p, byte l, byte s, byte pos) => new_card(d, c, o, p, l, s, pos); public static uint Process(IntPtr d) => process(d); public static void GetMessage(IntPtr d, IntPtr b) => get_message(d, b); public static void SetResponseInteger(IntPtr d, uint v) => set_responsei(d, v); public static void SetResponseBytes(IntPtr d, IntPtr b) => set_responseb(d, b);
    }
}
