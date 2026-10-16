using System.Runtime.InteropServices;

static string? Option(string[] values, string key)
{
    var i = Array.IndexOf(values, key);
    return i >= 0 && i + 1 < values.Length ? values[i + 1] : null;
}

var root = Path.GetFullPath(Option(args, "--root") ?? @"D:\MDProMod\tools\ygo_tcp_probe\staging");
var serverArgs = Option(args, "--server-args") ?? "17911 -1 5 0 F F F 8000 5 1 180 0";
var dll = Path.Combine(root, "ygoserver.dll");
if (!File.Exists(dll)) throw new FileNotFoundException("ygoserver.dll not found", dll);
Directory.SetCurrentDirectory(root);
NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (name, assembly, path) =>
{
    if (name is "ygoserver" or "ocgcore" or "sqlite3")
    {
        var p = Path.Combine(root, name + ".dll");
        if (File.Exists(p)) return NativeLibrary.Load(p);
    }
    return IntPtr.Zero;
});

Console.WriteLine($"MDPro3 ygoserver root: {root}");
Console.WriteLine($"start_server args: {serverArgs}");
Console.WriteLine("Press Ctrl+C or Enter to stop.");
using var stopped = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopped.Set(); };
var server = new Thread(() =>
{
    try
    {
        var rc = Native.Start(serverArgs);
        Console.WriteLine($"start_server returned {rc}");
    }
    catch (Exception ex) { Console.Error.WriteLine($"start_server failed: {ex}"); stopped.Set(); }
}) { IsBackground = true };
server.Start();
if (Console.IsInputRedirected) stopped.Wait(); else { _ = Task.Run(() => { try { Console.ReadLine(); } finally { stopped.Set(); } }); stopped.Wait(); }
try { Native.Stop(); } catch { }
server.Join(2000);
return 0;

static class Native
{
    [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int start_server([MarshalAs(UnmanagedType.LPStr)] string args);
    [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void stop_server();
    internal static int Start(string args) => start_server(args);
    internal static void Stop() => stop_server();
}
