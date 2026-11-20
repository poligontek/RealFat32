using System;
using System.IO;
using System.Threading;
using System.Reflection;
[assembly: AssemblyProduct("RealFat32")]
[assembly: AssemblyCompany("PoligonTek")]
[assembly: AssemblyInformationalVersion("9.0.0-alpha.1")]
internal static class UpdateFixture
{
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--wait") { Thread.Sleep(1500); return; }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "started.marker"), "updated fixture started");
    }
}
