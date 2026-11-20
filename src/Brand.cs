using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace RealFat32
{
    public static class Brand
    {
        public static readonly Color Background = Color.FromArgb(12, 20, 32);
        public static readonly Color Surface = Color.FromArgb(23, 35, 51);
        public static readonly Color Accent = Color.FromArgb(65, 225, 182);
        public static readonly Color Muted = Color.FromArgb(163, 181, 201);
        public static Bitmap Usb(int size)
        {
            var b = new Bitmap(size, size);
            using (var g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(size / 128f, size / 128f);
                using (var metal = new SolidBrush(Color.FromArgb(209, 224, 235))) g.FillRectangle(metal, 44, 7, 40, 34);
                using (var dark = new SolidBrush(Background)) { g.FillRectangle(dark, 51, 14, 8, 11); g.FillRectangle(dark, 69, 14, 8, 11); }
                using (var body = new SolidBrush(Accent))
                using (var p = Rounded(new RectangleF(27, 34, 74, 88), 19)) g.FillPath(body, p);
                using (var f = new Font("Segoe UI", 34, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(Background))
                using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("32", f, brush, new RectangleF(27, 38, 74, 68), fmt);
                using (var p = new Pen(Background, 4)) g.DrawLine(p, 54, 110, 74, 110);
            }
            return b;
        }
        private static GraphicsPath Rounded(RectangleF r, float radius)
        {
            float d = radius * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }
        public static Bitmap Banner(int width, int height)
        {
            return Banner(width, height, Background, Color.White, Muted, "Tus unidades. FAT32. Sin vueltas.");
        }
        public static Bitmap Banner(int width, int height, Color background, Color foreground, Color secondary, string slogan)
        {
            var b = new Bitmap(width, height);
            using (var g = Graphics.FromImage(b))
            using (var usb = Usb(128))
            using (var title = new Font("Segoe UI", 34, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var small = new Font("Segoe UI", 15, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var white = new SolidBrush(foreground))
            using (var muted = new SolidBrush(secondary))
            {
                g.Clear(background); g.DrawImage(usb, 22, (height - 90) / 2, 90, 90);
                g.DrawString("Real Fat 32", title, white, 131, height / 2 - 40);
                g.DrawString(slogan, small, muted, 135, height / 2 + 13);
            }
            return b;
        }
        public static void Export(string folder)
        {
            Directory.CreateDirectory(folder);
            using (var b = Usb(256)) b.Save(Path.Combine(folder, "icon.png"), ImageFormat.Png);
            using (var b = Banner(800, 160)) b.Save(Path.Combine(folder, "banner.png"), ImageFormat.Png);
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
            var images = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
                using (var b = Usb(sizes[i])) using (var ms = new MemoryStream()) { b.Save(ms, ImageFormat.Png); images[i] = ms.ToArray(); }
            using (var w = new BinaryWriter(File.Create(Path.Combine(folder, "RealFat32.ico"))))
            {
                w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);
                int offset = 6 + sizes.Length * 16;
                for (int i = 0; i < sizes.Length; i++)
                {
                    w.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); w.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    w.Write((byte)0); w.Write((byte)0); w.Write((ushort)1); w.Write((ushort)32);
                    w.Write(images[i].Length); w.Write(offset); offset += images[i].Length;
                }
                foreach (var data in images) w.Write(data);
            }
        }
    }
}
