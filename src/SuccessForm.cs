using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RealFat32
{
    internal sealed class SuccessForm : Form
    {
        private readonly Timer timer = new Timer { Interval = 30 };
        private readonly Stopwatch clock = new Stopwatch();
        private readonly Particle[] confetti = new Particle[90];
        private readonly string drive;
        private readonly bool demo;
        private double seconds;
        private struct Particle { public float X, Y, SpeedX, SpeedY, Angle; public Color Color; }
        public SuccessForm(string letter, bool preview)
        {
            drive = letter; demo = preview;
            Text = "RealFat32 · " + Ui.T("Disco totalmente formateado"); ClientSize = new Size(620, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.Window; ForeColor = Ui.Text; Font = new Font("Segoe UI", 10); DoubleBuffered = true;
            var close = new Button { Text = Ui.T("Cerrar"), Location = new Point(240, 357), Size = new Size(140, 40), DialogResult = DialogResult.OK };
            Controls.Add(close); Ui.Apply(this); CancelButton = AcceptButton = close;
            var random = new Random(32);
            Color[] palette = { Color.FromArgb(34, 180, 120), Color.FromArgb(255, 185, 40), Color.FromArgb(224, 66, 115), Color.FromArgb(56, 145, 245), Color.FromArgb(158, 99, 230) };
            for (int i = 0; i < confetti.Length; i++) confetti[i] = new Particle { X = 106, Y = 238, SpeedX = (float)(random.NextDouble() * 340 - 40), SpeedY = (float)(-180 - random.NextDouble() * 250), Angle = random.Next(360), Color = palette[i % palette.Length] };
            timer.Tick += delegate { seconds = clock.Elapsed.TotalSeconds; Invalidate(); if (seconds >= 5) timer.Stop(); };
            Shown += delegate { clock.Start(); timer.Start(); };
        }
        internal void PreviewAt(double time) { timer.Stop(); seconds = time; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            float appear = (float)Math.Min(1, seconds / 0.45);
            float radius = 39 + (float)(Math.Sin(appear * Math.PI) * 7);
            using (var soft = new SolidBrush(Ui.Night ? Color.FromArgb(20, 68, 53) : Color.FromArgb(222, 246, 230))) g.FillEllipse(soft, 310 - radius - 10, 109 - radius - 10, (radius + 10) * 2, (radius + 10) * 2);
            using (var green = new Pen(Ui.Green, 4)) g.DrawArc(green, 310 - radius, 109 - radius, radius * 2, radius * 2, -90, Math.Max(1, appear * 360));
            float check = (float)Math.Max(0, Math.Min(1, (seconds - 0.2) / 0.45));
            PointF a = new PointF(290, 109), b = new PointF(304, 123), c = new PointF(332, 93);
            using (var p = new Pen(Ui.Green, 6) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                if (check > 0) g.DrawLine(p, a, new PointF(a.X + (b.X - a.X) * Math.Min(1, check * 2), a.Y + (b.Y - a.Y) * Math.Min(1, check * 2)));
                if (check > 0.5f) g.DrawLine(p, b, new PointF(b.X + (c.X - b.X) * (check * 2 - 1), b.Y + (c.Y - b.Y) * (check * 2 - 1)));
            }
            Center(g, "OK", 164, 15, FontStyle.Bold, Ui.Green);
            Center(g, Ui.T("Disco totalmente formateado"), 202, 21, FontStyle.Bold, Ui.Text);
            Center(g, drive + " · FAT32", 240, 12, FontStyle.Bold, Ui.Green);
            Center(g, Ui.T(demo ? "Celebración de demostración · no se formateó ninguna unidad." : "Formato rápido FAT32 completado. La unidad está lista."), 307, 10, FontStyle.Regular, Ui.Muted);
            DrawPopper(g);
            if (seconds < 5)
                foreach (var p in confetti)
                {
                    float t = (float)seconds;
                    float x = p.X + p.SpeedX * t, y = p.Y + p.SpeedY * t + 115 * t * t;
                    var saved = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(p.Angle + t * 130);
                    using (var brush = new SolidBrush(p.Color)) g.FillRectangle(brush, -3, -5, 6, 10);
                    g.Restore(saved);
                }
        }
        private static void Center(Graphics g, string text, int y, int size, FontStyle style, Color color)
        {
            using (var font = new Font("Segoe UI", size, style))
            using (var brush = new SolidBrush(color))
            using (var align = new StringFormat { Alignment = StringAlignment.Center }) g.DrawString(text, font, brush, new RectangleF(12, y, 596, 48), align);
        }
        private void DrawPopper(Graphics g)
        {
            var state = g.Save();
            float wobble = (float)(Math.Sin(seconds * 12) * Math.Max(0, 1 - seconds) * 9);
            g.TranslateTransform(100, 250); g.RotateTransform(wobble);
            using (var gold = new SolidBrush(Color.FromArgb(255, 180, 38))) g.FillPolygon(gold, new[] { new Point(-42, 44), new Point(-15, -22), new Point(33, 16) });
            using (var stripe = new Pen(Color.FromArgb(229, 69, 126), 7)) { g.DrawLine(stripe, -28, 12, 0, 31); g.DrawLine(stripe, -19, -8, 17, 20); }
            using (var rim = new Pen(Color.FromArgb(169, 87, 205), 5)) g.DrawLine(rim, -15, -22, 33, 16);
            using (var ribbon = new Pen(Color.FromArgb(61, 143, 238), 3)) g.DrawBezier(ribbon, 10, -20, -10, -55, 52, -35, 26, -66);
            using (var ribbon = new Pen(Color.FromArgb(230, 79, 123), 3)) g.DrawBezier(ribbon, -20, -28, -58, -60, -3, -57, -31, -82);
            g.Restore(state);
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
