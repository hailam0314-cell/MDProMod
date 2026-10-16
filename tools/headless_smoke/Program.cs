using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace MdproHeadlessSmoke;

internal static class Paths
{
    public const string Install = @"D:\MyCardLibrary\mdpro3";
    public const string Core = Install + @"\MDPro3_Data\Plugins\x86_64\ocgcore.dll";
    public const string Sqlite = Install + @"\MDPro3_Data\Plugins\x86_64\sqlite3.dll";
}

internal static class Constants
{
    public const uint ProcessorEnd = 0x20000000;
    public const uint BufferMask = 0x0fffffff;
    public const int LocationDeck = 1;
    public const int LocationExtra = 64;
    public const int FaceDownDefense = 8;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeCardData
{
    public uint Code, Alias;
    public fixed ushort Setcode[16];
    public uint Type, Level, Attribute, Race;
    public int Attack, Defense;
    public uint LScale, RScale, LinkMarker;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate IntPtr ScriptReader(byte* name, int* length);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate uint CardReader(uint code, NativeCardData* data);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate uint MessageHandler(IntPtr duel, uint messageType);

internal static class Native
{
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void set_script_reader(ScriptReader callback);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void set_card_reader(CardReader callback);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void set_message_handler(MessageHandler callback);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr create_duel(uint seed);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void start_duel(IntPtr duel, int options);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void end_duel(IntPtr duel);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void set_player_info(IntPtr duel, int player, int lp, int startCount, int drawCount);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void new_card(IntPtr duel, uint code, byte owner, byte player, byte location, byte sequence, byte position);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern uint process(IntPtr duel);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern int get_message(IntPtr duel, IntPtr buffer);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void set_responsei(IntPtr duel, int value);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void set_responseb(IntPtr duel, IntPtr buffer);
    [DllImport(Paths.Core, CallingConvention = CallingConvention.Cdecl)] public static extern void get_log_message(IntPtr duel, IntPtr buffer);
}

internal static class Sqlite
{
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string file, out IntPtr db, int flags, IntPtr vfs);
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern int sqlite3_prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int n, out IntPtr statement, IntPtr tail);
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern int sqlite3_step(IntPtr statement);
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern int sqlite3_column_int(IntPtr statement, int column);
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern long sqlite3_column_int64(IntPtr statement, int column);
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern int sqlite3_finalize(IntPtr statement);
    [DllImport(Paths.Sqlite, CallingConvention = CallingConvention.Cdecl)] public static extern int sqlite3_close(IntPtr db);
}

internal readonly record struct CardInfo(uint Id, uint Alias, ulong Setcode, uint Type, uint Level, uint Attribute, uint Race, int Attack, int Defense);

internal unsafe sealed class Smoke : IDisposable
{
    readonly Dictionary<uint, CardInfo> cards = new();
    readonly Dictionary<string, (IntPtr Ptr, int Len)> scripts = new(StringComparer.OrdinalIgnoreCase);
    readonly byte[] messageBuffer = new byte[0x20000];
    readonly IntPtr nativeBuffer = Marshal.AllocHGlobal(0x20000);
    readonly ScriptReader scriptReader;
    readonly CardReader cardReader;
    readonly MessageHandler messageHandler;
    readonly List<byte> messageKinds = new();
    IntPtr duel;
    bool waiting;
    bool ended;
    int responses;
    int retries;
    readonly Dictionary<byte, int> messageCounts = new();

    public Smoke()
    {
        LoadCards();
        LoadScripts();
        scriptReader = ReadScript;
        cardReader = ReadCard;
        messageHandler = (_, _) => 0;
        Native.set_script_reader(scriptReader);
        Native.set_card_reader(cardReader);
        Native.set_message_handler(messageHandler);
    }

    void LoadCards()
    {
        var dbPath = Path.Combine(Paths.Install, "Data", "locales", "zh-CN", "cards.cdb");
        IntPtr db;
        var rc = Sqlite.sqlite3_open_v2(dbPath, out db, 1, IntPtr.Zero);
        if (rc != 0) throw new InvalidOperationException("cards.cdb open failed: " + rc);
        IntPtr stmt = IntPtr.Zero;
        try
        {
            rc = Sqlite.sqlite3_prepare_v2(db, "SELECT id,alias,setcode,type,level,attribute,race,atk,def FROM datas", -1, out stmt, IntPtr.Zero);
            if (rc != 0) throw new InvalidOperationException("cards.cdb query failed: " + rc);
            while (Sqlite.sqlite3_step(stmt) == 100)
            {
                var id = unchecked((uint)Sqlite.sqlite3_column_int(stmt, 0));
                cards[id] = new CardInfo(id, unchecked((uint)Sqlite.sqlite3_column_int(stmt, 1)), unchecked((ulong)Sqlite.sqlite3_column_int64(stmt, 2)), unchecked((uint)Sqlite.sqlite3_column_int(stmt, 3)), unchecked((uint)Sqlite.sqlite3_column_int(stmt, 4)), unchecked((uint)Sqlite.sqlite3_column_int(stmt, 5)), unchecked((uint)Sqlite.sqlite3_column_int(stmt, 6)), Sqlite.sqlite3_column_int(stmt, 7), Sqlite.sqlite3_column_int(stmt, 8));
            }
        }
        finally
        {
            if (stmt != IntPtr.Zero) Sqlite.sqlite3_finalize(stmt);
            Sqlite.sqlite3_close(db);
        }
    }

    void LoadScripts()
    {
        using var zip = ZipFile.OpenRead(Path.Combine(Paths.Install, "Data", "script.zip"));
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = entry.Open();
            using var data = new MemoryStream();
            stream.CopyTo(data);
            var bytes = data.ToArray();
            var ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            scripts[Normalize(entry.FullName)] = (ptr, bytes.Length);
        }
    }

    static string Normalize(string value) => value.Replace('\\', '/').TrimStart('.', '/');

    IntPtr ReadScript(byte* rawName, int* length)
    {
        var name = Marshal.PtrToStringUTF8((IntPtr)rawName) ?? string.Empty;
        if (!scripts.TryGetValue(Normalize(name), out var item)) { *length = 0; return IntPtr.Zero; }
        *length = item.Len;
        return item.Ptr;
    }

    uint ReadCard(uint code, NativeCardData* output)
    {
        *output = default;
        if (!cards.TryGetValue(code, out var card)) return 0;
        output->Code = card.Id;
        output->Alias = card.Alias;
        output->Type = card.Type;
        output->Level = card.Level;
        output->Attribute = card.Attribute;
        output->Race = card.Race;
        output->Attack = card.Attack;
        output->Defense = card.Defense;
        for (var i = 0; i < 4; i++) output->Setcode[i] = (ushort)((card.Setcode >> (i * 16)) & 0xffff);
        if ((card.Type & 0x4000000) != 0) { output->LinkMarker = unchecked((uint)card.Defense); output->Defense = 0; }
        return code;
    }

    void SendInt(int value) { Native.set_responsei(duel, value); responses++; waiting = false; }
    void SendBytes(byte[] bytes) { var ptr = Marshal.AllocHGlobal(Math.Max(64, bytes.Length)); try { Marshal.Copy(bytes, 0, ptr, bytes.Length); Native.set_responseb(duel, ptr); } finally { Marshal.FreeHGlobal(ptr); } responses++; waiting = false; }

    static byte FirstPosition(byte mask)
    {
        foreach (var p in new byte[] { 1, 4, 8, 2 }) if ((mask & p) != 0) return p;
        return 1;
    }

    static byte[] SelectFirst(int count, int min, int max)
    {
        if (count <= 0 || max <= 0) return new byte[] { 0 };
        min = Math.Clamp(min, 0, Math.Min(max, count));
        var result = new byte[min + 1];
        result[0] = (byte)min;
        for (var i = 0; i < min; i++) result[i + 1] = (byte)i;
        return result;
    }

    static byte[] SelectTribute(BinaryReader reader, int count, int min, int max)
    {
        var releases = new byte[count];
        for (var i = 0; i < count; i++)
        {
            reader.ReadUInt32(); reader.ReadByte(); reader.ReadByte(); reader.ReadByte();
            releases[i] = reader.ReadByte();
        }
        max = Math.Min(Math.Max(min, max), count);
        var chosen = new List<byte>();
        var total = 0;
        for (var i = 0; i < count && chosen.Count < max && total < min; i++)
        {
            chosen.Add((byte)i); total += releases[i];
        }
        var result = new byte[chosen.Count + 1]; result[0] = (byte)chosen.Count;
        for (var i = 0; i < chosen.Count; i++) result[i + 1] = chosen[i];
        return result;
    }

    static byte[] SelectPlace(byte player, byte count, uint flag)
    {
        var result = new byte[Math.Max(1, (int)count) * 3];
        var used = new HashSet<int>();
        for (var n = 0; n < Math.Max(1, (int)count); n++)
        {
            byte chosenPlayer = 0, chosenLoc = 4, chosenSeq = 0;
            var found = false;
            for (var p = 0; p < 2 && !found; p++)
            for (var l = 0; l < 2 && !found; l++)
            for (var s = 0; s < 7 && !found; s++)
            {
                var bit = s + (p == player ? 0 : 16) + (l == 0 ? 0 : 8);
                if ((flag & (1u << bit)) == 0 || used.Contains(bit)) continue;
                chosenPlayer = (byte)p; chosenLoc = (byte)(l == 0 ? 4 : 8); chosenSeq = (byte)s;
                used.Add(bit); found = true;
            }
            var off = n * 3;
            result[off] = chosenPlayer; result[off + 1] = chosenLoc; result[off + 2] = chosenSeq;
        }
        return result;
    }

    void HandleMessage(int length)
    {
        using var stream = new MemoryStream(messageBuffer, 0, length, false);
        using var reader = new BinaryReader(stream);
        while (stream.Position < stream.Length)
        {
            var type = reader.ReadByte();
            messageKinds.Add(type);
            messageCounts[type] = messageCounts.TryGetValue(type, out var old) ? old + 1 : 1;
            switch (type)
            {
                case 1: retries++; break;
                case 2: if (stream.Length - stream.Position >= 6) { reader.ReadByte(); reader.ReadByte(); reader.ReadUInt32(); } break;
                case 4: stream.Position = stream.Length; break; // MSG_START
                case 5: ended = true; stream.Position = stream.Length; break;
                case 10: // battle command: choose first legal action or main2/end
                    if (stream.Position + 2 <= stream.Length) { reader.ReadByte(); var activate = reader.ReadByte(); reader.ReadBytes(11 * activate); var attack = reader.ReadByte(); reader.ReadBytes(8 * attack); var m2 = reader.ReadByte(); reader.ReadByte(); SendInt(attack > 0 ? 1 : m2 != 0 ? 2 : 3); } break;
                case 11: // idle command
                    if (stream.Position < stream.Length) { reader.ReadByte(); for (var i=0;i<5;i++) { var n=reader.ReadByte(); reader.ReadBytes(7*n); } var a=reader.ReadByte(); reader.ReadBytes(11*a); var bp=reader.ReadByte(); reader.ReadByte(); if (stream.Position < stream.Length) reader.ReadByte(); SendInt(bp != 0 ? 6 : 7); } break;
                case 12: if (stream.Position + 13 <= stream.Length) { reader.ReadByte(); reader.ReadUInt32(); reader.ReadUInt32(); reader.ReadUInt32(); SendInt(0); } break;
                case 13: if (stream.Position + 5 <= stream.Length) { reader.ReadByte(); reader.ReadUInt32(); SendInt(0); } break;
                case 14: if (stream.Position+2<=stream.Length) { reader.ReadByte(); var n=reader.ReadByte(); reader.ReadBytes(4*n); SendInt(0); } break;
                case 15: if (stream.Position+5<=stream.Length) { reader.ReadByte(); reader.ReadByte(); var min=reader.ReadByte(); var max=reader.ReadByte(); var n=reader.ReadByte(); reader.ReadBytes(8*n); SendBytes(SelectFirst(n,min,max)); } break;
                case 16: // chain selection
                    if (stream.Position + 12 <= stream.Length) { reader.ReadByte(); var n=reader.ReadByte(); var spe=reader.ReadByte(); var forced=reader.ReadByte(); reader.ReadUInt32(); reader.ReadUInt32(); reader.ReadBytes(13*n); SendInt(n == 0 || forced == 0 ? -1 : 0); } else { stream.Position = stream.Length; } break;
                case 18: case 24: if (stream.Position + 6 <= stream.Length) { var player=reader.ReadByte(); var count=reader.ReadByte(); var flag=reader.ReadUInt32(); SendBytes(SelectPlace(player,count,flag)); } break;
                case 19: if (stream.Position + 6 <= stream.Length) { reader.ReadByte(); reader.ReadUInt32(); var mask=reader.ReadByte(); SendInt(FirstPosition(mask)); } break;
                case 20: if (stream.Position+5<=stream.Length) { reader.ReadByte(); var cancel=reader.ReadByte(); var min=reader.ReadByte(); var max=reader.ReadByte(); var n=reader.ReadByte(); var result=SelectTribute(reader,n,min,max); if(result[0] < min && cancel != 0) SendInt(-1); else SendBytes(result); } break;
                case 22: if (stream.Position + 6 <= stream.Length) { reader.ReadByte(); reader.ReadUInt16(); var quantity=reader.ReadUInt16(); var n=reader.ReadByte(); var counters=new ushort[n]; for(var i=0;i<n;i++){ reader.ReadUInt32(); reader.ReadByte(); reader.ReadByte(); reader.ReadByte(); counters[i]=reader.ReadUInt16(); } var outb=new byte[n*2]; var remaining=quantity; for(var i=0;i<n && remaining>0;i++){ var v=(ushort)Math.Min(counters[i],remaining); outb[2*i]=(byte)v; outb[2*i+1]=(byte)(v>>8); remaining-=v; } SendBytes(outb); } break;
                case 23: if (stream.Position + 8 <= stream.Length) { reader.ReadByte(); reader.ReadByte(); reader.ReadUInt32(); var min=reader.ReadByte(); var max=reader.ReadByte(); if(max==0) max=99; var must=reader.ReadByte(); reader.ReadBytes(11*must); var n=reader.ReadByte(); reader.ReadBytes(11*n); var total=Math.Min(255, Math.Max((int)must, (int)min)); var resp=new byte[total+1]; resp[0]=(byte)total; for(var i=0;i<total;i++) resp[i+1]=(byte)(i<must ? 0 : i-must); SendBytes(resp); } break;
                case 25: if (stream.Position + 2 <= stream.Length) { reader.ReadByte(); var n=reader.ReadByte(); reader.ReadBytes(7*n); var resp=new byte[n+1]; resp[0]=(byte)n; for(var i=0;i<n;i++) resp[i+1]=(byte)i; SendBytes(resp); } break;
                case 26: if (stream.Position + 5 <= stream.Length) { reader.ReadByte(); var finish=reader.ReadByte(); var cancel=reader.ReadByte(); var min=reader.ReadByte(); var max=reader.ReadByte(); var n=reader.ReadByte(); reader.ReadBytes(8*n); var un=reader.ReadByte(); reader.ReadBytes(8*un); if ((cancel!=0 || finish!=0) && min==0) SendInt(-1); else SendBytes(SelectFirst(n,min,max)); } break;
                case 30: case 31: case 42: if (stream.Position < stream.Length) { var n=reader.ReadByte(); reader.ReadBytes(7*n); } break;
                case 32: case 33: case 39: if (stream.Position < stream.Length) { reader.ReadByte(); if(type != 32) reader.ReadByte(); } break;
                case 40: if (stream.Position < stream.Length) reader.ReadByte(); break;
                case 41: if (stream.Position+2 <= stream.Length) reader.ReadUInt16(); break;
                default: stream.Position = stream.Length; break;
            }
        }
    }

    public object Run()
    {
        var ids = cards.Values.Where(c => (c.Type & 1) != 0 && (c.Type & 0x80) == 0).OrderBy(c => c.Id).Take(4).Select(c => c.Id).ToArray();
        if (ids.Length == 0) throw new InvalidOperationException("No usable monster cards in database");
        var deck = Enumerable.Repeat(ids, 10).SelectMany(x => x).ToArray();
        duel = Native.create_duel(0x12345678);
        Native.set_player_info(duel, 0, 8000, 5, 1);
        Native.set_player_info(duel, 1, 8000, 5, 1);
        for (var player = 0; player < 2; player++) foreach (var id in deck.Reverse()) Native.new_card(duel, id, (byte)player, (byte)player, Constants.LocationDeck, 0, Constants.FaceDownDefense);
        Native.start_duel(duel, (5 << 16) | 0x40);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        var loops = 0;
        while (!ended && DateTime.UtcNow < deadline && loops++ < 2000)
        {
            var result = Native.process(duel);
            var length = (int)(result & Constants.BufferMask);
            var flag = result & 0xf0000000;
            if (length > 0)
            {
                Native.get_message(duel, nativeBuffer);
                Marshal.Copy(nativeBuffer, messageBuffer, 0, length);
                HandleMessage(length);
            }
            if (flag == Constants.ProcessorEnd) ended = true;
            if (length == 0 && flag == 0) Thread.Sleep(1);
        }
        return new { cardCount = cards.Count, scriptCount = scripts.Count, deckCount = deck.Length, sampleIds = ids, loops, ended, responses, retries, messageKinds, messageCounts };
    }

    public void Dispose()
    {
        if (duel != IntPtr.Zero) Native.end_duel(duel);
        foreach (var item in scripts.Values) Marshal.FreeHGlobal(item.Ptr);
        Marshal.FreeHGlobal(nativeBuffer);
    }
}

internal static unsafe class Program
{
    public static int Main()
    {
        try { using var smoke = new Smoke(); Console.WriteLine(JsonSerializer.Serialize(smoke.Run(), new JsonSerializerOptions { WriteIndented = true })); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

