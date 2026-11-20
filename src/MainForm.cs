using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealFat32
{
    public sealed class MainForm : Form
    {
        private readonly ComboBox units = new DarkComboBox(), clusters = new DarkComboBox();
        private readonly TextBox label = new TextBox();
        private readonly Label detail = new Label(), protection = new Label(), status = new Label();
        private readonly Button refresh = new Button(), format = new Button(), admin = new Button();
        private readonly Button settings = new Button();
        private readonly Button updates = new Button(), about = new Button();
        private readonly Label updateStatus = new Label();
        private readonly UpdateService updateService = new UpdateService();
        private readonly System.Threading.CancellationTokenSource lifetime = new System.Threading.CancellationTokenSource();
        private readonly Timer updateTimer = new Timer { Interval = 500 };
        private UpdateRelease pendingUpdate;
        private bool checkingUpdates, installingUpdate;
        private readonly PictureBox header;
        private readonly ProgressBar progress = new ProgressBar();
        private readonly bool demo;
        private bool busy;
        private int lastProgress = -1;
        public MainForm(bool preview)
        {
            demo = preview;
            Text = "RealFat32 · " + ReleaseInfo.Display; ClientSize = new Size(760, 770); MinimumSize = new Size(776, 809);
            StartPosition = FormStartPosition.CenterScreen; BackColor = Brand.Background; ForeColor = Color.White;
            Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
            using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("RealFat32.ico")) if (s != null) Icon = new Icon(s);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(28, 0, 28, 20) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 133));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 123));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);
            header = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, AccessibleName = "Real Fat 32" };
            var headerPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 162));
            headerPanel.Controls.Add(header, 0, 0);
            SetupButton(settings, "Configuración", false); settings.Name = "settings"; settings.Size = new Size(152, 32); settings.Margin = new Padding(6, 12, 0, 0);
            headerPanel.Controls.Add(settings, 1, 0); root.Controls.Add(headerPanel, 0, 0);
            root.Controls.Add(new Label { Text = "01   ELIGE UNA UNIDAD", ForeColor = Brand.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            var driveRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            driveRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); driveRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 133));
            SetupCombo(units); units.AccessibleName = "Unidad de destino"; units.Dock = DockStyle.Fill; units.Margin = new Padding(0, 4, 12, 0);
            SetupButton(refresh, "↻  Actualizar", false); refresh.Dock = DockStyle.Fill; refresh.Margin = new Padding(0, 0, 0, 8);
            driveRow.Controls.Add(units, 0, 0); driveRow.Controls.Add(refresh, 1, 0); root.Controls.Add(driveRow, 0, 2);
            var card = new TableLayoutPanel { Name = "card", BackColor = Brand.Surface, Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 10), RowCount = 2, Margin = new Padding(0, 3, 0, 9) };
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 65)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            detail.Dock = DockStyle.Fill; detail.ForeColor = Color.White; protection.Dock = DockStyle.Fill; protection.ForeColor = Brand.Accent;
            card.Controls.Add(detail, 0, 0); card.Controls.Add(protection, 0, 1); root.Controls.Add(card, 0, 3);
            var options = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty };
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            options.RowStyles.Add(new RowStyle(SizeType.Absolute, 31)); options.RowStyles.Add(new RowStyle(SizeType.Absolute, 37)); options.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            options.Controls.Add(new Label { Text = "02   ETIQUETA DEL VOLUMEN", ForeColor = Brand.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            options.Controls.Add(new Label { Text = "TAMAÑO DE CLÚSTER", ForeColor = Brand.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
            label.Text = "REALFAT32"; label.MaxLength = 11; label.CharacterCasing = CharacterCasing.Upper; label.BackColor = Brand.Surface; label.ForeColor = Color.White; label.BorderStyle = BorderStyle.FixedSingle;
            label.Dock = DockStyle.Fill; label.Margin = new Padding(0, 0, 18, 0); label.AccessibleName = "Etiqueta del volumen, máximo 11 caracteres";
            SetupCombo(clusters); clusters.Items.AddRange(new object[] { "Automático (recomendado)", "4 KiB", "8 KiB", "16 KiB", "32 KiB" }); clusters.SelectedIndex = 0; clusters.Dock = DockStyle.Fill; clusters.Margin = Padding.Empty;
            clusters.AccessibleName = "Tamaño de clúster"; options.Controls.Add(label, 0, 1); options.Controls.Add(clusters, 1, 1);
            var hint = new Label { Text = "Formato rápido · FAT32 admite archivos de hasta 4 GiB menos 1 byte.", ForeColor = Brand.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            options.Controls.Add(hint, 0, 2); options.SetColumnSpan(hint, 2); root.Controls.Add(options, 0, 4);
            var actionRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            actionRow.Controls.Add(new Label { Name = "warning", Text = "Se eliminarán todos los archivos del volumen.\nEl formato rápido no es un borrado seguro.", ForeColor = Color.FromArgb(255, 191, 132), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            SetupButton(format, "Formatear en FAT32", true); format.Dock = DockStyle.Fill; format.Margin = new Padding(14, 10, 0, 18); actionRow.Controls.Add(format, 1, 0); root.Controls.Add(actionRow, 0, 5);
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Margin = Padding.Empty };
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 9)); footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
            progress.Dock = DockStyle.Fill; progress.Margin = Padding.Empty; footer.Controls.Add(progress, 0, 0);
            status.Text = "Listo para actualizar las unidades."; status.ForeColor = Brand.Muted; status.Dock = DockStyle.Fill; status.TextAlign = ContentAlignment.MiddleLeft; footer.Controls.Add(status, 0, 1);
            SetupButton(admin, "Abrir como administrador", false); admin.AutoSize = true; admin.Anchor = AnchorStyles.Left; admin.Visible = !Volumes.IsAdmin && !demo; footer.Controls.Add(admin, 0, 2);
            var appActions = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
            SetupButton(updates, "Buscar actualizaciones", false); updates.Name = "updates"; updates.Size = new Size(230, 32); updates.Margin = new Padding(0, 4, 12, 0);
            SetupButton(about, "Acerca de", false); about.Name = "about"; about.Size = new Size(130, 32); about.Margin = new Padding(0, 4, 12, 0);
            appActions.Controls.AddRange(new Control[] { updates, about, new Label { Text = ReleaseInfo.Display, AutoSize = true, Margin = new Padding(0, 12, 0, 0), Font = new Font("Segoe UI", 8) } });
            footer.Controls.Add(appActions, 0, 3);
            updateStatus.Name = "updateStatus"; updateStatus.Dock = DockStyle.Fill; updateStatus.Font = new Font("Segoe UI", 8); updateStatus.AutoEllipsis = true; updateStatus.TextAlign = ContentAlignment.MiddleLeft;
            footer.Controls.Add(updateStatus, 0, 4);
            root.Controls.Add(footer, 0, 6);
            refresh.Click += async delegate { await RefreshUnits(); };
            units.SelectedIndexChanged += delegate { UpdateSelection(); };
            format.Click += async delegate { await StartFormat(); };
            admin.Click += delegate { if (demo) { using (var celebration = new SuccessForm("E:", true)) celebration.ShowDialog(this); } else Elevate(); };
            settings.Click += delegate { using (var dialog = new SettingsForm()) if (dialog.ShowDialog(this) == DialogResult.OK) ApplyPreferences(); };
            about.Click += delegate { using (var dialog = new AboutForm()) dialog.ShowDialog(this); };
            updates.Click += async delegate { await CheckUpdates(); };
            updateTimer.Tick += async delegate { if (pendingUpdate != null && !busy && !installingUpdate && !checkingUpdates && !OwnedForms.Any(f => f.Visible)) await OfferUpdate(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) e.Cancel = true; };
            Shown += async delegate { updateTimer.Start(); await RefreshUnits(); if (!demo && !IsDisposed) await CheckUpdates(); };
            Ui.Capture(this); ApplyPreferences();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { lifetime.Cancel(); updateTimer.Dispose(); }
            if (disposing && header != null && header.Image != null) { header.Image.Dispose(); header.Image = null; }
            base.Dispose(disposing);
        }
        private void ApplyPreferences()
        {
            Ui.Apply(this);
            units.AccessibleName = Ui.T("Unidad de destino"); clusters.AccessibleName = Ui.T("Tamaño de clúster"); label.AccessibleName = Ui.T("Etiqueta del volumen, máximo 11 caracteres");
            clusters.Items[0] = Ui.T("Automático (recomendado)");
            var old = header.Image; header.Image = Brand.Banner(520, 132, Ui.Window, Ui.Text, Ui.Muted, Ui.T("Tus unidades. FAT32. Sin vueltas.")); if (old != null) old.Dispose();
            admin.Visible = demo || !Volumes.IsAdmin;
            if (demo) { admin.Text = Ui.T("Ver celebración"); status.Text = Ui.T("Vista de demostración · no realiza operaciones de disco."); }
            UpdateSelection(); Invalidate(true);
            updateStatus.ForeColor = Ui.Muted;
        }
        private async Task CheckUpdates()
        {
            if (checkingUpdates || installingUpdate || busy || IsDisposed) return;
            checkingUpdates = true; updates.Enabled = false; pendingUpdate = null;
            updateStatus.Text = Ui.T("Buscando actualizaciones…");
            try
            {
                using (var timeout = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(30));
                    var release = await updateService.CheckAsync(Preferences.UpdateChannel, timeout.Token);
                    if (IsDisposed) return;
                    pendingUpdate = release;
                    updateStatus.Text = release == null ? Ui.T("No hay actualizaciones disponibles.") : Ui.T("Actualización disponible: ") + release.Version.Value;
                }
            }
            catch (Exception ex) { if (!IsDisposed) updateStatus.Text = Ui.T("No se pudo comprobar: ") + ex.Message; }
            finally { checkingUpdates = false; if (!IsDisposed) updates.Enabled = !busy; }
        }
        private async Task OfferUpdate()
        {
            var release = pendingUpdate; pendingUpdate = null;
            using (var prompt = new UpdatePrompt(release)) if (prompt.ShowDialog(this) != DialogResult.Yes) return;
            installingUpdate = true; SetBusy(true); updateStatus.Text = Ui.T("Descargando y verificando la actualización…");
            try
            {
                using (var timeout = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
                {
                    timeout.CancelAfter(TimeSpan.FromMinutes(3));
                    string source = await updateService.DownloadAsync(release, timeout.Token);
                    updateService.LaunchInstaller(source, Application.ExecutablePath, release.Digest);
                }
                busy = false; Close();
            }
            catch (Exception ex) { updateStatus.Text = Ui.T("No se pudo actualizar: ") + ex.Message; installingUpdate = false; SetBusy(false); }
        }
        private static void SetupCombo(ComboBox c)
        {
            c.DropDownStyle = ComboBoxStyle.DropDownList; c.FlatStyle = FlatStyle.Flat; c.BackColor = Brand.Surface; c.ForeColor = Color.White; c.DropDownWidth = 650;
            c.DrawMode = DrawMode.OwnerDrawFixed; c.ItemHeight = 26;
            c.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                using (var brush = new SolidBrush(Ui.Surface)) e.Graphics.FillRectangle(brush, e.Bounds);
                string text = e.Index >= 0 ? Ui.Item(c.Items[e.Index]) : Ui.T("Selecciona una unidad…");
                TextRenderer.DrawText(e.Graphics, text, c.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height), Ui.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.DrawFocusRectangle();
            };
        }
        private static void SetupButton(Button b, string text, bool primary)
        {
            b.Text = text; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = primary ? 0 : 1;
            if (primary) b.Name = "primary";
            b.FlatAppearance.BorderColor = Color.FromArgb(59, 78, 99); b.BackColor = primary ? Brand.Accent : Brand.Surface;
            b.ForeColor = primary ? Brand.Background : Color.White; b.Cursor = Cursors.Hand; b.Font = new Font("Segoe UI", 10, primary ? FontStyle.Bold : FontStyle.Regular);
        }
        private void Elevate()
        {
            try { Process.Start(new ProcessStartInfo(Application.ExecutablePath) { UseShellExecute = true, Verb = "runas", WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows) }); Close(); }
            catch (System.ComponentModel.Win32Exception) { status.Text = Ui.T("No se concedieron permisos de administrador."); }
        }
        private void SetBusy(bool value)
        {
            busy = value; refresh.Enabled = units.Enabled = label.Enabled = clusters.Enabled = admin.Enabled = settings.Enabled = !value;
            updates.Enabled = !value && !checkingUpdates; about.Enabled = !value;
            UpdateSelection();
        }
        private void UpdateSelection()
        {
            var v = units.SelectedItem as Volume;
            format.Enabled = !busy && !demo && v != null && v.Allowed;
            format.Text = Ui.T(demo || Volumes.IsAdmin ? "Formatear en FAT32" : "Continuar como administrador");
            if (v == null) { detail.Text = Ui.T("Conecta una unidad y pulsa Actualizar."); protection.Text = Ui.T("Selecciona el volumen que quieres preparar."); return; }
            detail.Text = v.Model + "  ·  " + Ui.T("Disco") + " " + v.Disk + "  ·  " + (v.PartitionStyle == 2 ? "GPT" : "MBR") + "\n" +
                Ui.T("Capacidad:") + " " + Volume.Human(v.Size) + "     " + Ui.T("Libre:") + " " + (v.Free < 0 ? "—" : Volume.Human(v.Free)) + "\n" +
                Ui.T("Formato actual:") + " " + Ui.Error(v.FileSystem) + "     " + Ui.T("Sector:") + " " + v.SectorSize + " bytes";
            protection.Text = "●  " + (v.Allowed ? Ui.T("Compatible con FAT32") : Ui.Error(v.Blocked));
            protection.ForeColor = v.Allowed ? Ui.Green : Ui.Warning;
            detail.BackColor = protection.BackColor = Ui.Surface;
        }
        public void SetDemo()
        {
            units.Items.Clear(); units.Items.Add(new Volume { Letter = "E:", Label = "MI PENDRIVE", Size = 64L * 1024 * 1024 * 1024, Free = 51L * 1024 * 1024 * 1024, Disk = 2, Model = "USB Flash Drive", FileSystem = "exFAT", SectorSize = 512, PartitionStyle = 1 });
            units.SelectedIndex = 0; status.Text = Ui.T("Vista de demostración · no realiza operaciones de disco.");
        }
        private async Task RefreshUnits()
        {
            if (demo) { SetDemo(); return; }
            string previous = (units.SelectedItem as Volume) == null ? null : ((Volume)units.SelectedItem).Fingerprint;
            SetBusy(true); status.Text = Ui.T("Consultando unidades y protecciones de Windows…");
            try
            {
                var found = await Task.Run(() => Volumes.Scan());
                units.Items.Clear(); foreach (var v in found) units.Items.Add(v);
                // Never automatically select a different device after refresh or hot unplug.
                units.SelectedItem = found.FirstOrDefault(v => v.Fingerprint == previous);
                status.Text = found.Count + Ui.T(" volumen(es) detectado(s). Selecciona una unidad.");
            }
            catch (Exception ex) { units.Items.Clear(); status.Text = Ui.T("No se pudieron consultar las unidades: ") + Ui.Error(ex.Message); }
            finally { SetBusy(false); }
        }
        private async Task StartFormat()
        {
            var v = units.SelectedItem as Volume;
            if (v == null || !v.Allowed || busy || demo) return;
            if (!Volumes.IsAdmin) { Elevate(); return; }
            string name; int cluster = new int[] { 0, 4096, 8192, 16384, 32768 }[clusters.SelectedIndex];
            try { name = Fat32.NormalizeLabel(label.Text); if (cluster == 0) FatLayout.Automatic(v.Size, (int)v.SectorSize); else new FatLayout(v.Size, (int)v.SectorSize, cluster); }
            catch (ArgumentException ex) { MessageBox.Show(this, Ui.Error(ex.Message), Ui.T("Revisa las opciones"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var confirm = new ConfirmForm(v)) if (confirm.ShowDialog(this) != DialogResult.OK) return;
            SetBusy(true); progress.Value = 0; lastProgress = -1;
            bool success = false;
            var report = new Progress<Tuple<int, string>>(p => { progress.Value = p.Item1; status.Text = Ui.Error(p.Item2); });
            try
            {
                await Task.Run(() => Volumes.Format(v, cluster, name, (percent, text) =>
                {
                    if (percent != lastProgress) { lastProgress = percent; ((IProgress<Tuple<int, string>>)report).Report(Tuple.Create(percent, text)); }
                }));
                success = true;
                using (var celebration = new SuccessForm(v.Letter, false)) celebration.ShowDialog(this);
            }
            catch (Exception ex) { MessageBox.Show(this, Ui.Error(ex.Message), Ui.T("No se completó el formato"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { SetBusy(false); }
            await RefreshUnits();
            if (success) status.Text = Ui.T("Formato finalizado en ") + v.Letter + Ui.T(" · FAT32 listo.");
        }
    }

    // Native ComboBox does not render its selected text in WM_PRINT screenshots.
    // Keep keyboard/accessibility behavior and draw the selection for that path.
    internal sealed class DarkComboBox : ComboBox
    {
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if ((m.Msg == 0x0317 || m.Msg == 0x0318) && m.WParam != IntPtr.Zero)
            {
                using (var g = Graphics.FromHdc(m.WParam))
                using (var brush = new SolidBrush(Ui.Surface))
                {
                    var bounds = new Rectangle(2, 2, Width - 22, Height - 4);
                    g.FillRectangle(brush, bounds);
                    string text = SelectedItem == null ? Ui.T("Selecciona una unidad…") : Ui.Item(SelectedItem);
                    bounds.X += 5; bounds.Width -= 8;
                    TextRenderer.DrawText(g, text, Font, bounds, Ui.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            }
        }
    }

    internal sealed class ConfirmForm : Form
    {
        private readonly Timer timer = new Timer { Interval = 50 };
        private readonly Stopwatch elapsed = new Stopwatch();
        private readonly Func<double> elapsedSeconds;
        private readonly Button yes;
        public ConfirmForm(Volume v, Func<double> testClock = null)
        {
            elapsedSeconds = testClock ?? (() => elapsed.Elapsed.TotalSeconds);
            Text = Ui.T("Confirmar formato de ") + v.Letter; ClientSize = new Size(560, 300); FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
            var warning = new Label { Name = "warning", Text = Ui.T("Se eliminarán TODOS los archivos de ") + v.Letter + "\n\n" + v.Model + " · " + Volume.Human(v.Size) + " · " + Ui.T("Disco") + " " + v.Disk + "\n" + Ui.T("Etiqueta actual: ") + (String.IsNullOrEmpty(v.Label) ? Ui.T("Sin etiqueta") : v.Label) + "\n\n" + Ui.T("Comprueba la unidad. El botón se habilitará después de 3 segundos."), Location = new Point(24, 22), Size = new Size(512, 177) };
            yes = new Button { Name = "confirmFormat", Text = Ui.T("Formatear") + " (3)", Location = new Point(304, 226), Size = new Size(232, 44), Enabled = false };
            var no = new Button { Text = Ui.T("Cancelar"), Location = new Point(24, 226), Size = new Size(140, 44), DialogResult = DialogResult.Cancel };
            yes.Click += delegate { if (yes.Enabled && elapsedSeconds() >= 3) { DialogResult = DialogResult.OK; Close(); } };
            timer.Tick += delegate
            {
                int remaining = Math.Max(0, (int)Math.Ceiling(3 - elapsedSeconds()));
                yes.Text = Ui.T("Formatear") + (remaining > 0 ? " (" + remaining + ")" : "");
                yes.Enabled = remaining == 0;
                if (yes.Enabled) timer.Stop();
            };
            Shown += delegate { elapsed.Start(); timer.Start(); no.Focus(); };
            CancelButton = no; Controls.AddRange(new Control[] { warning, yes, no }); Ui.Apply(this);
            yes.BackColor = Color.FromArgb(179, 48, 61); yes.ForeColor = Color.White; yes.UseVisualStyleBackColor = false;
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
