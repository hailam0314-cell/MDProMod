using System.Reflection;
using System.Runtime.Loader;

if (args.Length < 1) { Console.Error.WriteLine("usage: launcher <mdpro3-root> [WindBot key=value args]"); return 2; }
var root = Path.GetFullPath(args[0]);
var managed = Path.Combine(root, "MDPro3_Data", "Managed");
var plugins = Path.Combine(root, "MDPro3_Data", "Plugins", "x86_64");
Directory.SetCurrentDirectory(root);
var resolver = new AssemblyLoadContext("mdpro3", isCollectible:false);
resolver.Resolving += (_, name) => {
    var n = name.Name ?? "";
    if (n is "mscorlib" or "System" or "System.Core" or "netstandard" || n.StartsWith("System.") || n.StartsWith("Microsoft.")) return null;
    foreach (var dir in new[]{managed, plugins}) {
        var p = Path.Combine(dir, n + ".dll");
        if (File.Exists(p)) { try { return resolver.LoadFromAssemblyPath(p); } catch { return null; } }
    }
    return null;
};
var asmPath = Path.Combine(managed, "Assembly-CSharp.dll");
var asm = resolver.LoadFromAssemblyPath(asmPath);
var type = asm.GetType("WindBot.Program", throwOnError:true)!;
var main = type.GetMethod("Main", BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!;
try { main.Invoke(null, new object[]{args[1..]}); }
catch (TargetInvocationException ex) { Console.Error.WriteLine(ex.InnerException ?? ex); return 1; }
return 0;
