using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RealFat32
{
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = Ui.T("Acerca de") + " RealFat32"; ClientSize = new Size(620, 520); Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            var name = new Label { Text = "RealFat32 " + ReleaseInfo.Display, Font = new Font("Segoe UI", 19, FontStyle.Bold), Location = new Point(24, 18), Size = new Size(574, 42) };
            var credits = new Label { Text = Ui.T("Creada por Fabian Gonzalez Dev") + "\n" + Ui.T("Empresa: PoligonTek") + "\nhttps://github.com/" + ReleaseInfo.Repository, Location = new Point(26, 72), Size = new Size(570, 76) };
            var history = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(24, 187), Size = new Size(572, 258), Text = Changelog(), AccessibleName = Ui.T("Historial de cambios") };
            var heading = new Label { Text = Ui.T("Historial de cambios"), Location = new Point(24, 155), AutoSize = true };
            var close = new Button { Text = Ui.T("Cerrar"), Location = new Point(456, 466), Size = new Size(140, 34), DialogResult = DialogResult.OK };
            Controls.AddRange(new Control[] { name, credits, heading, history, close }); CancelButton = AcceptButton = close; Ui.Apply(this);
        }
        private static string Changelog()
        {
            using (var input = typeof(AboutForm).Assembly.GetManifestResourceStream("CHANGELOG." + Ui.Language + ".txt"))
            { if (input == null) return ReleaseInfo.Display; using (var reader = new StreamReader(input)) return reader.ReadToEnd().Replace("\r\n", "\n").Replace("\n", Environment.NewLine); }
        }
    }

    internal sealed class UpdatePrompt : Form
    {
        public UpdatePrompt(UpdateRelease release)
        {
            Text = Ui.T("Actualización de RealFat32"); ClientSize = new Size(560, 355); Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            var heading = new Label { Text = Ui.T("Hay una nueva actualización. ¿Actualizar?") + "\n" + ReleaseInfo.Version + " → " + release.Version.Value, Location = new Point(24, 18), Size = new Size(512, 58) };
            var notes = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = release.Notes, Location = new Point(24, 85), Size = new Size(512, 156) };
            var hint = new Label { Text = Ui.T("Se cerrará y reiniciará la aplicación."), Location = new Point(24, 251), Size = new Size(512, 25) };
            var yes = new Button { Text = Ui.T("Sí"), Location = new Point(376, 297), Size = new Size(160, 36), DialogResult = DialogResult.Yes };
            var no = new Button { Text = Ui.T("No"), Location = new Point(24, 297), Size = new Size(140, 36), DialogResult = DialogResult.No };
            Controls.AddRange(new Control[] { heading, notes, hint, yes, no }); CancelButton = no; Ui.Apply(this);
            Shown += delegate { no.Focus(); };
        }
    }
}
