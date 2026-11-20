using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RealFat32;

internal static class Fat32Tests
{
    private static int checks;
    private static void Check(bool ok, string name) { checks++; if (!ok) throw new Exception(name); }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (ArgumentException) { Check(true, name); return; }
        throw new Exception("No se rechazó: " + name);
    }
    private static void Main()
    {
        foreach (int sector in new[] { 512, 4096 })
        foreach (long mib in new long[] { 512, 513, 1024, 8192, 32768, 65536, 131072, 1048576, 2097152 })
        {
            long bytes = mib * 1024 * 1024;
            FatLayout f;
            try { f = FatLayout.Automatic(bytes, sector); }
            catch (ArgumentException) { Check(bytes / sector > uint.MaxValue, "Solo rechazar desborde de sectores"); continue; }
            var boot = Fat32.Boot(f, "TEST", 2048, 123);
            int bps = BitConverter.ToUInt16(boot, 11), spc = boot[13];
            uint reserved = BitConverter.ToUInt16(boot, 14), fat = BitConverter.ToUInt32(boot, 36), total = BitConverter.ToUInt32(boot, 32);
            long dataClusters = ((long)total - reserved - boot[16] * (long)fat) / spc;
            Check(dataClusters >= 65525 && dataClusters < 0x0FFFFFF5, "FAT32 por cantidad de clústeres");
            Check((long)fat * bps >= (dataClusters + 2) * 4, "La FAT contiene todas las entradas");
            Check((long)total * bps == bytes, "Capacidad exacta");
            Check(bps * spc <= 32768, "Compatibilidad clúster <=32 KiB");
            Check(boot[510] == 0x55 && boot[511] == 0xAA, "Firma de arranque");
            Check(BitConverter.ToUInt32(boot, 28) == 2048, "Sectores ocultos");
            Check(BitConverter.ToUInt32(boot, 67) == 123, "Serial");
        }
        Reject(() => new FatLayout(256L * 1024 * 1024, 512, 4096), "volumen pequeño");
        Reject(() => new FatLayout(3L * 1024 * 1024 * 1024 * 1024, 4096, 32768), "volumen grande");
        Reject(() => new FatLayout(1024L * 1024 * 1024, 1024, 4096), "sector incompatible");
        Reject(() => new FatLayout(1024L * 1024 * 1024 + 1, 512, 4096), "desalineación");
        Reject(() => new FatLayout(512L * 1024 * 1024, 512, 32768), "FAT16 accidental");
        Reject(() => new FatLayout(1024L * 1024 * 1024, 512, 12288), "potencia de dos");
        foreach (string bad in new[] { "ABCDEFGHIJKL", "ÁRBOL", "BAD/NAME", "A.B", "A?", "A\u0001B" }) Reject(() => Fat32.NormalizeLabel(bad), "etiqueta inválida");
        Check(Fat32.NormalizeLabel(" usb-32 ") == "USB-32", "normalización etiqueta");
        foreach (int sector in new[] { 512, 4096 })
        foreach (string label in new[] { "", "REALFAT32" })
        {
            var f = FatLayout.Automatic(1024L * 1024 * 1024, sector);
            using (var image = new SparseImage(sector))
            {
                Fat32.Format(image, f, label, 2048, (p, t) => Check(p >= 0 && p <= 100, "progreso"));
                byte[] boot = image.At(0, sector), backup = image.At(6L * sector, sector);
                Check(boot.SequenceEqual(backup), "BPB principal y copia coinciden");
                Check(Encoding.ASCII.GetString(boot, 82, 8) == "FAT32   ", "identificador");
                byte[] info = image.At(sector, sector);
                Check(info.SequenceEqual(image.At(7L * sector, sector)), "copia FSInfo");
                Check(BitConverter.ToUInt32(info, 0) == 0x41615252 && BitConverter.ToUInt32(info, 484) == 0x61417272 && BitConverter.ToUInt32(info, 508) == 0xAA550000, "firmas FSInfo");
                Check(BitConverter.ToUInt32(info, 488) == f.ClusterCount - 1 && BitConverter.ToUInt32(info, 492) == 3, "espacio libre excluye directorio raíz");
                var fat1 = image.At(32L * sector, (int)f.FatSectors * sector);
                var fat2 = image.At((32L + f.FatSectors) * sector, fat1.Length);
                Check(fat1.SequenceEqual(fat2), "ambas FAT completas coinciden");
                Check(BitConverter.ToUInt32(fat1, 0) == 0x0FFFFFF8 && BitConverter.ToUInt32(fat1, 4) == 0xFFFFFFFF && BitConverter.ToUInt32(fat1, 8) == 0x0FFFFFFF, "entradas reservadas y raíz EOC");
                Check(fat1.Skip(12).All(b => b == 0), "resto de FAT libre");
                var root = image.At(f.DataOffset, f.ClusterBytes);
                Check(label.Length == 0 ? root.All(b => b == 0) : Encoding.ASCII.GetString(root, 0, 11).TrimEnd() == label && root[11] == 8 && root.Skip(32).All(b => b == 0), "directorio raíz y etiqueta");
                Check(image.HighestWrite == f.DataOffset + f.ClusterBytes, "no se escriben datos fuera de metadatos");
                Check(image.At(image.HighestWrite, sector).All(b => b == 0xCC), "datos antiguos no se tocan en formato rápido");
            }
        }
        Console.WriteLine("PASS: " + checks + " comprobaciones. Solo imágenes en memoria; ningún disco físico abierto.");
    }

    // Sector-addressed image prefilled with nonzero bytes, independent of layout implementation.
    private sealed class SparseImage : Stream
    {
        private readonly Dictionary<long, byte[]> blocks = new Dictionary<long, byte[]>();
        private readonly int sector;
        private long position;
        public long HighestWrite;
        public SparseImage(int s) { sector = s; }
        public byte[] At(long offset, int count) { var b = new byte[count]; position = offset; Read(b, 0, count); return b; }
        public override void Write(byte[] buffer, int offset, int count)
        {
            Check(position % sector == 0 && count % sector == 0, "escrituras alineadas al sector");
            for (int n = 0; n < count; n += sector)
            { var b = new byte[sector]; Buffer.BlockCopy(buffer, offset + n, b, 0, sector); blocks[position / sector] = b; position += sector; }
            HighestWrite = Math.Max(HighestWrite, position);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            for (int n = 0; n < count; n++) { byte[] b; buffer[offset + n] = blocks.TryGetValue(position / sector, out b) ? b[position % sector] : (byte)0xCC; position++; }
            return count;
        }
        public override bool CanRead { get { return true; } } public override bool CanWrite { get { return true; } } public override bool CanSeek { get { return true; } }
        public override long Length { get { return 1024L * 1024 * 1024; } }
        public override long Position { get { return position; } set { position = value; } }
        public override void Flush() { }
        public override long Seek(long o, SeekOrigin origin) { position = origin == SeekOrigin.Begin ? o : origin == SeekOrigin.Current ? position + o : Length + o; return position; }
        public override void SetLength(long value) { throw new NotSupportedException(); }
    }
}
