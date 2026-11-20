using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;

internal static class UpdateWorker
{
    [STAThread]
    private static int Main(string[] args)
    {
        string target = null, replacement = null, backup = null; bool replaced = false;
        try
        {
            int pid; long start;
            if (args.Length != 6 || !Int32.TryParse(args[0], out pid) || !Int64.TryParse(args[1], out start) || !Regex.IsMatch(args[4], "^[a-fA-F0-9]{64}$")) throw new ArgumentException("Invalid updater arguments");
            target = Path.GetFullPath(args[2]); string source = Path.GetFullPath(args[3]);
            if (Path.GetFileName(target) != "RealFat32.exe" || Path.GetFileName(source) != "RealFat32.exe" || target.Equals(source, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Invalid executable path");
            Verify(source, args[4]);
            try
            {
                using (var parent = Process.GetProcessById(pid))
                { if (parent.StartTime.ToUniversalTime().Ticks != start || !parent.WaitForExit(60000)) throw new IOException("The application did not exit"); }
            }
            catch (ArgumentException) { } // Parent already exited.
            replacement = target + ".update-" + Guid.NewGuid().ToString("N") + ".tmp";
            backup = target + ".previous";
            File.Copy(source, replacement, false); Verify(replacement, args[4]);
            File.Replace(replacement, target, backup, true); replaced = true;
            Verify(target, args[4]);
            using (var app = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(target) }))
            { if (app == null) throw new IOException("The updated application could not start"); }
            return 0;
        }
        catch (Exception ex)
        {
            try
            {
                if (replaced && File.Exists(backup)) File.Copy(backup, target, true);
                if (replacement != null && File.Exists(replacement)) File.Delete(replacement);
                string heading = args.Length > 5 && args[5] == "es" ? "No se pudo actualizar RealFat32. Se conserva la versión anterior." : args.Length > 5 && args[5] == "pt" ? "Não foi possível atualizar o RealFat32. A versão anterior foi mantida." : "RealFat32 could not be updated. The previous version was kept.";
                MessageBox.Show(heading + "\n\n" + ex.Message, "RealFat32 · PoligonTek", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (target != null && File.Exists(target)) Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception) { }
            return 1;
        }
    }
    private static void Verify(string file, string hash)
    {
        using (var input = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var sha = SHA256.Create())
            if (!String.Equals(BitConverter.ToString(sha.ComputeHash(input)).Replace("-", ""), hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA256 verification failed");
    }
}
