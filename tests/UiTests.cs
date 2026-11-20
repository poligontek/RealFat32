using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;
using RealFat32;

internal static class UiTests
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    [STAThread]
    private static int Main(string[] args)
    {
        try { Run(args); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Run(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string path = Path.Combine(Path.GetTempPath(), "RealFat32-test-" + Guid.NewGuid() + ".txt");
        try
        {
            Preferences.Load(path, new CultureInfo("es-AR")); Check(Ui.Language == "es" && !Ui.Night && Preferences.LanguageChoice == "auto", "Default light / automatic Spanish");
            Preferences.Save(path, "pt", true); Ui.Language = "es"; Ui.Night = false;
            Preferences.Load(path); Check(Ui.Language == "pt" && Ui.Night, "Preferences survive reload");
            File.WriteAllLines(path, new[] { "invalid", "invalid" }); Preferences.Load(path, new CultureInfo("es-AR"));
            Check(Ui.Language == "es" && !Ui.Night, "Invalid preferences use safe defaults");
            File.WriteAllLines(path, new[] { "auto", "light", "beta" }); Preferences.Load(path, new CultureInfo("pt-BR"));
            Check(Ui.Language == "pt" && Preferences.UpdateChannel == "beta", "Automatic Portuguese and channel persistence");
            Preferences.Load(path, new CultureInfo("en-US")); Check(Ui.Language == "en", "Automatic English");
            Preferences.Load(path, new CultureInfo("de-DE")); Check(Ui.Language == "en", "Unsupported OS languages fall back to English");
            File.WriteAllLines(path, new[] { "es", "night" }); Preferences.Load(path, new CultureInfo("pt-PT"));
            Check(Ui.Language == "es" && Ui.Night, "Previous manual preference survives migration");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        foreach (string language in new[] { "es", "en", "pt" })
        {
            Ui.Language = language;
            foreach (bool night in new[] { false, true })
            {
                Ui.Night = night;
                using (var form = new MainForm(true))
                {
                    form.Show(); form.SetDemo(); Application.DoEvents();
                    Check(form.BackColor == Ui.Window, "Theme applied");
                    Check(form.Controls.Find("primary", true).Length == 1, "One format action");
                    Check(!form.Controls.Find("primary", true)[0].Enabled, "Demo never enables formatting");
                    var settings = form.Controls.Find("settings", true).Single();
                    Check(settings.Visible && settings.Right <= settings.Parent.ClientSize.Width, "Settings action visible inside header");
                    Check(form.Controls.Find("updates", true).Single().Visible && form.Controls.Find("about", true).Single().Visible, "Update and About actions visible");
                }
                using (var form = new SuccessForm("E:", true)) { form.Show(); form.PreviewAt(1); Application.DoEvents(); Check(form.Text.Contains(Ui.T("Disco totalmente formateado")), "Localized celebration"); }
                using (var form = new AboutForm()) { Check(form.Controls.OfType<TextBox>().Single().Text.Contains(ReleaseInfo.Version), "Embedded changelog available"); }
            }
            // All application-owned backend errors and progress messages must translate.
            foreach (string file in new[] { "Fat32.cs", "Volumes.cs" })
            {
                string source = File.ReadAllText(Path.Combine(args[0], "src", file));
                foreach (Match match in Regex.Matches(source, "\"([^\"\\r\\n]+)\""))
                {
                    string text = match.Groups[1].Value;
                    if (language != "es" && (text.EndsWith(".") || text.EndsWith("…") || text.EndsWith(". ")) && text.Length > 12 && !text.Contains("\\"))
                        Check(Ui.Error(text) != text, "Missing backend translation: " + language + " " + text);
                }
            }
        }
        Ui.Language = "en"; Ui.Night = false;
        double elapsed = 0;
        using (var form = new ConfirmForm(new Volume { Letter = "E:", Model = "Test", Size = 64L * 1024 * 1024 * 1024 }, () => elapsed))
        {
            form.Show(); Application.DoEvents(); var clock = Stopwatch.StartNew();
            var button = (Button)form.Controls.Find("confirmFormat", true).Single();
            Check(!button.Enabled && button.Text.Contains("(3)"), "Initially locked for three seconds");
            button.PerformClick(); Check(form.DialogResult != DialogResult.OK, "Early click cannot confirm");
            elapsed = 2.999; clock.Restart();
            while (clock.Elapsed.TotalSeconds < 0.12) { Application.DoEvents(); Thread.Sleep(10); }
            Check(!button.Enabled, "Still locked before three seconds");
            elapsed = 3; clock.Restart();
            while (!button.Enabled && clock.Elapsed.TotalSeconds < 2) { Application.DoEvents(); Thread.Sleep(10); }
            Check(button.Enabled && button.Text == "Format", "Enabled after three seconds");
            button.PerformClick(); Check(form.DialogResult == DialogResult.OK, "Explicit click confirms after countdown");
        }
        using (var form = new ConfirmForm(new Volume { Letter = "E:", Model = "Test" }))
        { form.Show(); Application.DoEvents(); form.Close(); Check(form.DialogResult != DialogResult.OK, "Closing cancels confirmation"); }
        Console.WriteLine("PASS: " + checks + " UI checks (themes, OS languages, preferences, countdown, About). No disk access.");
    }
}
