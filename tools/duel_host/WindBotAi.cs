using System.Text;
using WindBot;
using WindBot.Game;
using YGOSharp.Network.Enums;
using YGOSharp.OCGWrapper.Enums;

internal sealed record WindBotOptions(int Seat, string Name, string Deck, string DeckFile, string Dialog);

/**
 * 把 WindBot 当库跑：不走 TCP，用本地内存队列喂 STOC 消息、收 CTOS 响应。
 * 走的就是 MDPro 单人模式同一套 AI 代码。
 */
internal sealed class WindBotAi
{
    private const byte StocGameMsg = 0x01;
    private const byte StocJoinGame = 0x12;
    private const byte StocTypeChange = 0x13;
    private const byte StocDuelStart = 0x15;
    private const byte StocHsPlayerEnter = 0x20;
    private const byte StocHsPlayerChange = 0x21;

    private readonly GameClient _client;

    public int Seat { get; }

    public WindBotAi(WindBotOptions options)
    {
        Seat = options.Seat;
        EnsureWindBotRuntime();
        var info = new WindBotInfo
        {
            Name = string.IsNullOrWhiteSpace(options.Name) ? "MDProBot" : options.Name,
            Deck = ResolveExecutorName(options.Deck),
            DeckFile = string.IsNullOrWhiteSpace(options.DeckFile) ? null : options.DeckFile,
            Dialog = string.IsNullOrWhiteSpace(options.Dialog) ? "default" : options.Dialog,
            Host = "127.0.0.1",
            Port = 0,
            HostInfo = "",
            Version = 4962,
            Hand = 0,
            Debug = false,
            Chat = true,
        };
        _client = new GameClient(info);
        _client.StartLocal();
        _client.Behavior.Duel.IsFirst = Seat == 0;
        Handshake();
        FeedGameStart();
        DrainResponses();
    }

    /**
     * 直接驱动引擎时不会收到 MSG_START（决斗是 Native.StartDuel 起的），
     * 这里按同样的布局补一条，让 WindBot 自己完成 LP / 卡组 / 状态初始化。
     */
    private void FeedGameStart()
    {
        var deck = _client.Behavior.Deck;
        int mainCount = deck?.Cards.Count ?? 40;
        int extraCount = deck?.ExtraCards.Count ?? 0;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(StocGameMsg);
        writer.Write((byte)4);                 // MSG_START
        writer.Write((byte)Seat);              // 自己的座位
        writer.Write((byte)5);                 // 新大师规则 2020
        writer.Write(8000);
        writer.Write(8000);
        writer.Write((short)mainCount);
        writer.Write((short)extraCount);
        writer.Write((short)mainCount);
        writer.Write((short)extraCount);
        _client.Feed(stream.ToArray());
        SeededCards = mainCount + extraCount;
    }

    public int SeededCards { get; private set; }

    /** 调试：把 AI 自己看到的卡组/手牌打出来。 */
    public string DumpState()
    {
        var duel = _client.Behavior.Duel;
        var me = duel.Fields[0];
        var hand = new System.Text.StringBuilder();
        foreach (var card in me.Hand)
        {
            hand.Append(card == null ? "null" : card.Id + ":type=" + card.Type + ":lv=" + card.Level + ":atk=" + card.Attack).Append(',');
        }
        return "deck=" + me.Deck.Count + " hand=" + me.Hand.Count
                + " lp=" + me.LifePoints + " ids=[" + hand.ToString().TrimEnd(',') + "]";
    }

    private static readonly object RuntimeGate = new();
    private static bool _runtimeReady;

    private static string ResolveExecutorName(string requested)
    {
        // bot.conf identifies a deck file (AI_BlueEyes), while DecksManager keys
        // executors by DeckAttribute.Name (Blue-Eyes). Passing a file identifier
        // without resolving it silently chooses a random, unrelated executor.
        foreach (var type in typeof(WindBot.Game.AI.DeckAttribute).Assembly.GetTypes())
        {
            var attribute = Attribute.GetCustomAttribute(type, typeof(WindBot.Game.AI.DeckAttribute))
                            as WindBot.Game.AI.DeckAttribute;
            if (attribute == null) continue;
            if (string.Equals(attribute.Name, requested, StringComparison.OrdinalIgnoreCase)
                || string.Equals(attribute.File, requested, StringComparison.OrdinalIgnoreCase))
                return attribute.Name;
        }
        throw new ArgumentException("Unknown WindBot executor / deck: " + requested);
    }

    internal static void SetRandomSeed(uint seed)
    {
        EnsureWindBotRuntime();
        WindBot.Program.Rand = new Random(unchecked((int)seed));
    }

    /** WindBot 的静态运行时（随机数、AI 卡组表、卡名表）只需要初始化一次。 */
    private static void EnsureWindBotRuntime()
    {
        lock (RuntimeGate)
        {
            if (_runtimeReady) return;
            WindBot.Config.Load(Array.Empty<string>());
            WindBot.Program.InitDatas("cards.cdb");
            _runtimeReady = true;
        }
    }

    private void Handshake()
    {
        var join = new byte[20];
        join[6] = 5; // 新大师规则 2020
        FeedStoc(StocJoinGame, join);
        FeedStoc(StocTypeChange, new[] { (byte)(Seat == 0 ? 0x11 : 0x01) });
        FeedStoc(StocHsPlayerEnter, PlayerEnter("MDProBot", Seat));
        FeedStoc(StocHsPlayerEnter, PlayerEnter("Duelist", 1 - Seat));
        FeedStoc(StocHsPlayerChange, new[] { (byte)Seat });
        FeedStoc(StocHsPlayerChange, new[] { (byte)(1 - Seat) });
        FeedStoc(StocDuelStart, Array.Empty<byte>());
    }

    private static byte[] PlayerEnter(string name, int position)
    {
        var payload = new byte[41];
        var bytes = Encoding.Unicode.GetBytes(name ?? string.Empty);
        Array.Copy(bytes, payload, Math.Min(bytes.Length, 40));
        payload[40] = (byte)position;
        return payload;
    }

    private void FeedStoc(byte function, byte[] payload)
    {
        var packet = new byte[payload.Length + 1];
        packet[0] = function;
        Array.Copy(payload, 0, packet, 1, payload.Length);
        _client.Feed(packet);
    }

    /** 喂一条引擎消息（buffer[0] 是 MSG 功能号）。 */
    public void FeedMessage(byte[] buffer, int length)
    {
        // Apply the same hidden-information boundary as the regular duel server.
        // Native core messages contain both players' identities; AI must not use
        // a hidden hand/deck/facedown card just because this transport is local.
        if (length > 0 && buffer[0] is 33 or 39 or 50 or 54 or 90)
        {
            buffer = buffer.AsSpan(0, length).ToArray();
            if (buffer[0] == 90 && length >= 3 && buffer[1] != Seat)
            {
                for (var i = 0; i < buffer[2] && 3 + 4 * (i + 1) <= length; i++)
                {
                    var offset = 3 + 4 * i;
                    if ((buffer[offset + 3] & 0x80) == 0) Array.Clear(buffer, offset, 4);
                }
            }
            else if (buffer[0] == 50 && length == 17 && buffer[9] != Seat)
            {
                var location = buffer[10];
                var position = buffer[12];
                if ((location & (16 | 128)) == 0
                    && ((location & (1 | 2)) != 0 || (position & 10) != 0))
                    Array.Clear(buffer, 1, 4);
            }
            else if (buffer[0] == 54 && length == 9) Array.Clear(buffer, 1, 4);
            else if (buffer[0] is 33 or 39 && length >= 3 && buffer[1] != Seat)
            {
                Array.Clear(buffer, 3, length - 3);
            }
        }
        // The 4962 core includes a visibility byte in MSG_CONFIRM_CARDS. The
        // embedded classic WindBot parser expects the older player/count shape.
        if (length >= 4 && buffer[0] == 31 && length == 4 + buffer[3] * 7)
        {
            var legacy = new byte[length - 1];
            Array.Copy(buffer, 0, legacy, 0, 2);
            Array.Copy(buffer, 3, legacy, 2, length - 3);
            buffer = legacy;
            length = legacy.Length;
        }
        bool isDraw = length >= 3 && buffer[0] == 90; // MSG_DRAW
        int drawPlayer = isDraw ? buffer[1] & 0x0F : 0;
        var drawCodes = isDraw ? ReadDrawCodes(buffer, length) : null;
        var packet = new byte[length + 1];
        packet[0] = StocGameMsg;
        Array.Copy(buffer, 0, packet, 1, length);
        _client.Feed(packet);
        if (isDraw) ApplyDrawCodes(drawPlayer, drawCodes);
    }

    /** MSG_DRAW 的 payload 里带着抽到的卡号（4 字节小端/张），直连引擎时用它补手牌卡号。 */
    private static List<int> ReadDrawCodes(byte[] buffer, int length)
    {
        int count = buffer[2] & 0xFF;
        var codes = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int offset = 3 + i * 4;
            if (offset + 4 > length) break;
            int code = buffer[offset] | (buffer[offset + 1] << 8)
                    | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
            if (code > 0 && code < 100000000)
            {
                codes.Add(code);
            }
        }
        return codes;
    }

    private void ApplyDrawCodes(int enginePlayer, List<int>? codes)
    {
        if (codes == null || codes.Count == 0) return;
        var duel = _client.Behavior.Duel;
        int local = duel.IsFirst ? enginePlayer : 1 - enginePlayer;
        var hand = duel.Fields[local].Hand;
        int index = 0;
        for (int i = Math.Max(0, hand.Count - codes.Count); i < hand.Count && index < codes.Count; i++)
        {
            var card = hand[i];
            if (card == null) continue;
            if (card.Id == 0)
            {
                card.SetId(codes[index]);
            }
            index++;
        }
    }

    /** 取走 AI 产生的响应参数（CTOS_RESPONSE 去掉功能号后的字节）。 */
    public List<byte[]> DrainResponses()
    {
        var responses = new List<byte[]>();
        while (true)
        {
            var packet = _client.Connection.TakeLocalPacket();
            if (packet == null) break;
            if (packet.Length >= 1 && packet[0] == (byte)CtosMessage.Response)
            {
                var args = new byte[packet.Length - 1];
                Array.Copy(packet, 1, args, 0, args.Length);
                responses.Add(args);
            }
            // 本地模式下其它 CTOS（UpdateDeck / HsReady / Chat / HandResult …）直接忽略
        }
        return responses;
    }
}
