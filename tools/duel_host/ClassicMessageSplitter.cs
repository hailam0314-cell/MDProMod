using System.Buffers.Binary;

/// <summary>
/// Native process() returns a concatenation of classic core messages, without lengths.
/// Decode only documented formats; an unknown/truncated tail stays intact and is flagged.
/// This runtime is protocol 4962: ConfirmCards includes a visibility byte, and
/// SelectChain stores the forced flag on each 14-byte candidate, rather than the header.
/// </summary>
internal static class ClassicMessageSplitter
{
    internal sealed record Message(byte[] Bytes, bool Framed);

    public static List<Message> Split(ReadOnlySpan<byte> buffer)
    {
        var messages = new List<Message>();
        var offset = 0;
        while (offset < buffer.Length)
        {
            var remaining = buffer[offset..];
            var length = MessageLength(remaining);
            if (length <= 0 || length > remaining.Length)
            {
                messages.Add(new Message(remaining.ToArray(), false));
                break;
            }
            messages.Add(new Message(remaining[..length].ToArray(), true));
            offset += length;
        }
        return messages;
    }

    public static int MessageLength(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return -1;
        try
        {
            var cursor = new Cursor(data);
            var function = cursor.Byte();
            switch (function)
            {
                case 1: case 3: case 8: case 37: case 61: case 63: case 65:
                case 74: case 112: case 113: case 114: case 121: case 122: case 123:
                    break;
                case 2: cursor.Skip(6); break;
                case 4: cursor.Skip(18); break;
                case 5: cursor.Skip(2); break;
                case 10:
                    cursor.Skip(1);
                    cursor.Entries(11);
                    cursor.Entries(8);
                    cursor.Skip(2);
                    break;
                case 11:
                    // 功能号 + 玩家号 + 六个列表 + to_bp / to_ep / can_shuffle 三个标志字节。
                    // （用实机报文核过：len = 6 + Σ(1+7n) + (1+11n)；以前这里多跳了 1 个字节，
                    //  导致每条 IdleCmd 都会吃掉下一条消息的首字节。）
                    cursor.Byte();          // 玩家号
                    for (var i = 0; i < 5; i++) cursor.Entries(7);
                    cursor.Entries(11);
                    cursor.Skip(3);
                    break;
                case 12: cursor.Skip(13); break;
                case 13: cursor.Skip(5); break;
                case 14: cursor.Skip(1); cursor.Entries(4); break;
                case 15: case 20: cursor.Skip(4); cursor.Entries(8); break;
                case 16:
                    // 头 12 字节（含功能号：功能号+玩家+候选数+spe_count+两个 4 字节 hint）
                    // + 每个候选 14 字节（标志1+强制1+卡号4+位置4+描述4）。
                    // 用 2024 个真实连锁帧核对过：12 + 14*候选数 与报文长度 100% 一致；
                    // 长度不够就整包收下，由上层"读到哪算哪"地解析，绝不丢掉整条提示。
                    if (data.Length < 12) return data.Length;
                    var chains = data[2];
                    var need = 12 + chains * 14;
                    return need <= data.Length ? need : data.Length;
                case 18: case 19: case 24: cursor.Skip(6); break;
                case 21: case 25: cursor.Skip(1); cursor.Entries(7); break;
                case 22: cursor.Skip(5); cursor.Entries(9); break;
                case 23: cursor.Skip(8); cursor.Entries(11); cursor.Entries(11); break;
                case 26: cursor.Skip(5); cursor.Entries(8); cursor.Entries(8); break;
                case 30: case 42: cursor.Skip(1); cursor.Entries(7); break;
                case 31: cursor.Skip(2); cursor.Entries(7); break;
                case 32: case 34: case 35: case 40: case 71: case 72: case 73:
                case 75: case 76: case 132: case 133:
                    cursor.Skip(1); break;
                case 33: case 39: case 90: cursor.Skip(1); cursor.Entries(4); break;
                case 36: cursor.Skip(1); cursor.Entries(8); break;
                case 38: cursor.Skip(6); break;
                case 41: cursor.Skip(2); break;
                case 50: case 55: case 70: cursor.Skip(16); break;
                case 53: case 160: cursor.Skip(9); break;
                case 54: case 60: case 62: case 64: case 93: case 96: case 97:
                case 110: case 120:
                    cursor.Skip(8); break;
                case 56: case 95: case 170: cursor.Skip(4); break;
                case 80: case 81: cursor.Skip(1); cursor.Entries(4); break;
                case 83: cursor.Entries(4); break;
                case 91: case 92: case 94: case 100: cursor.Skip(5); break;
                case 101: case 102: cursor.Skip(7); break;
                case 111: cursor.Skip(26); break;
                case 130: case 131: cursor.Skip(1); cursor.Entries(1); break;
                case 140: case 141: cursor.Skip(6); break;
                case 142: case 143: case 144: cursor.Skip(1); cursor.Entries(4); break;
                case 161:
                    cursor.Skip(1);
                    cursor.Byte(); // main count is metadata; only the deck top is sent
                    var extra = cursor.Byte();
                    cursor.Byte(); // face-up pendulum count
                    var hand = cursor.Byte();
                    cursor.Skip(4 + 4 * (extra + hand));
                    break;
                case 162:
                    cursor.Skip(1);
                    for (var player = 0; player < 2; player++)
                    {
                        cursor.Skip(4);
                        for (var zone = 0; zone < 7; zone++) if (cursor.Byte() != 0) cursor.Skip(2);
                        for (var zone = 0; zone < 8; zone++) if (cursor.Byte() != 0) cursor.Skip(1);
                        cursor.Skip(6);
                    }
                    cursor.Entries(15);
                    break;
                case 163: case 164: var textLength = cursor.UInt16(); cursor.Skip(textLength + 1); break;
                case 165: cursor.Skip(6); break;
                case 200: cursor.Skip(1); break;
                default: return -1;
            }
            return cursor.Position;
        }
        catch (ArgumentOutOfRangeException) { return -1; }
    }

    public static int SelfTest(string? jsonlPath = null)
    {
        var cases = new[]
        {
            ("2901000201011B000000", new[] { 3, 7 }),
            ("0201002900000002010129000000", new[] { 7, 7 }),
            ("2101079FFB3B049F544E00A3A9570586926800869268009F544E00869268002001", new[] { 31, 2 }),
            ("1F000001A3A95705010206", new[] { 11 }),
            ("32002E0D050002000A0008020A0004000236002E0D050008020A", new[] { 17, 9 }),
            // SelectChain：真实报文（头 12 + 候选数 * 14）。
            // 1 个候选：从实战日志里抄下来的窗口（Maxx "C" 在手上）。
            ("10000100010000000000210000002A9565010002030AA0525916", new[] { 26 }),
            // 2 个候选：同一批里的多候选窗口（长度 40 = 12 + 14*2）。
            ("1000020200000000000000000000AF23DE000002000A000000000000AF23DE000002020A00000000", new[] { 40 }),
            // SelectIdleCmd：实战报文，长度 = 6 + Σ(1+7n) + (1+11n)。
            ("0B00000000000289327704000200FBDC140100020101F9F78605000400917F6F58000101", new[] { 36 }),
            ("0B0003F3EAF405000200387D6804000201B2BD0800000203000003F3EAF405000200387D6804000201B2BD08000002030289327704000202CC11480400020402B2BD080000020320DB8B00CC11480400020400000000000101", new[] { 89 }),
        };
        foreach (var (hex, lengths) in cases)
        {
            var messages = Split(Convert.FromHexString(hex));
            var raw = Convert.FromHexString(hex);
            Console.WriteLine("splitter case " + hex[..Math.Min(12, hex.Length)] + " -> "
                + string.Join(",", messages.Select(m => (m.Framed ? "F" : "U") + m.Bytes.Length))
                + " expected " + string.Join(",", lengths)
                + " | bytes=" + raw.Length
                + " len=" + MessageLength(raw.AsSpan())
                + " head=" + Convert.ToHexString(raw.AsSpan(0, Math.Min(8, raw.Length))));
            if (messages.Any(m => !m.Framed) || !messages.Select(m => m.Bytes.Length).SequenceEqual(lengths))
                throw new InvalidOperationException("Splitter regression failed: " + hex);
        }
        if (Split(Convert.FromHexString("FF010203")) is not [{ Framed: false }])
            throw new InvalidOperationException("Unknown message must remain an opaque tail");
        var count = cases.Length;
        if (jsonlPath != null)
        {
            // 历史 jsonl 只是"参考数据"：它里面的 hex 是被当时的切分器切出来的，
            // 早于某些帧长修正（例如 SelectIdleCmd 曾经少算 1 字节），所以这里只做报告，
            // 不用它当硬性断言；硬性断言用上面的 inline 真实报文。
            var unframed = 0;
            var framed = 0;
            var samples = new List<string>();
            foreach (var line in File.ReadLines(jsonlPath))
            {
                if (!line.StartsWith('{')) continue;
                using var document = System.Text.Json.JsonDocument.Parse(line);
                var root = document.RootElement;
                if (!root.TryGetProperty("type", out var type) || type.GetString() != "message") continue;
                var bytes = Convert.FromHexString(root.GetProperty("hex").GetString()!);
                var messages = Split(bytes);
                if (messages.Any(m => !m.Framed))
                {
                    unframed++;
                    if (samples.Count < 5) samples.Add(bytes[0].ToString());
                    continue;
                }
                framed += messages.Count;
            }
            Console.WriteLine($"jsonl advisory: framed={framed} unframed={unframed}"
                + (samples.Count > 0 ? " (functions: " + string.Join(",", samples) + ")" : ""));
            count += framed;
        }
        return count;
    }

    private ref struct Cursor
    {
        private readonly ReadOnlySpan<byte> _data;
        public int Position { get; private set; }
        public Cursor(ReadOnlySpan<byte> data) { _data = data; Position = 0; }
        public byte Byte() { Skip(1); return _data[Position - 1]; }
        public ushort UInt16() { var start = Position; Skip(2); return BinaryPrimitives.ReadUInt16LittleEndian(_data[start..]); }
        public void Entries(int size) { var count = Byte(); Skip(count * size); }
        public void Skip(int count)
        {
            if (count < 0 || count > _data.Length - Position) throw new ArgumentOutOfRangeException(nameof(count));
            Position += count;
        }
    }
}
