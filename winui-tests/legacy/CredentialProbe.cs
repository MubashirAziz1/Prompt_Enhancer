using System;
using System.IO;
using PromptEnhancer;

// Compiled with the retained application's .NET Framework compiler and exact
// credential sources, independently of the WinUI runtime. No production path.
internal static class CredentialProbe
{
    public static int Main(string[] args)
    {
        if (args.Length != 3) return 2;
        string path = Path.GetFullPath(args[1]);
        string build = Path.GetFullPath(Path.GetDirectoryName(typeof(CredentialProbe).Assembly.Location)) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(build, StringComparison.OrdinalIgnoreCase)) return 2;
        try
        {
            var store = new DpapiCredentialStore(path);
            if (args[0] == "write") { store.Save(args[2]); return 0; }
            if (args[0] == "read") return store.Load() == args[2] ? 0 : 1;
            return 2;
        }
        catch { return 1; }
    }
}
