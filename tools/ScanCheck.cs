using System;
using RealFat32;

// Read-only integration smoke check; does not open raw-volume handles.
internal static class ScanCheck
{
    private static int Main()
    {
        try
        {
            var volumes = Volumes.Scan();
            Console.WriteLine("Consulta de solo lectura: " + volumes.Count + " volúmenes.");
            foreach (var v in volumes) Console.WriteLine(v.Letter + " " + (v.Allowed ? "Compatible" : "Protegido: " + v.Blocked));
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
