using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("RealFat32")]
[assembly: AssemblyDescription("Formateador FAT32 para Windows")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]
[assembly: AssemblyInformationalVersion(RealFat32.ReleaseInfo.Version)]
[assembly: AssemblyCompany("PoligonTek")]
[assembly: AssemblyProduct("RealFat32")]
[assembly: AssemblyCopyright("Fabian Gonzalez Dev · PoligonTek")]

namespace RealFat32
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 2 && args[0] == "--export-assets") { Brand.Export(args[1]); return; }
            Preferences.Load(Preferences.DefaultPath);
            bool capture = args.Length >= 2 && (args[0] == "--screenshot" || args[0] == "--success-screenshot" || args[0] == "--confirm-screenshot");
            if (capture)
            {
                // Deterministic previews never save or change the user's preferences.
                Ui.Night = Array.IndexOf(args, "--night") >= 0; Ui.Language = "es";
                foreach (var arg in args) if (arg == "--lang=en") Ui.Language = "en"; else if (arg == "--lang=pt") Ui.Language = "pt";
                if (args[0] != "--screenshot")
                {
                    using (Form dialog = args[0] == "--success-screenshot" ? (Form)new SuccessForm("E:", true) : new ConfirmForm(new Volume { Letter = "E:", Model = "USB Flash Drive", Size = 64L * 1024 * 1024 * 1024, Label = "USB", Disk = 2 }))
                    {
                        dialog.Show(); var success = dialog as SuccessForm; if (success != null) success.PreviewAt(1.1); Application.DoEvents(); Capture(dialog, args[1]);
                    }
                    return;
                }
            }
            bool preview = args.Length > 0 && (args[0] == "--demo" || args[0] == "--screenshot");
            using (var form = new MainForm(preview))
            {
                if (capture)
                {
                    form.Show(); form.SetDemo(); Application.DoEvents();
                    Capture(form, args[1]);
                    return;
                }
                Application.Run(form);
            }
        }
        private static void Capture(Form form, string path)
        {
            using (var b = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(b, new Rectangle(0, 0, form.Width, form.Height)); b.Save(Path.GetFullPath(path)); }
        }
    }
}
