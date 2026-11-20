using System;
using System.IO;
using System.Text;

namespace RealFat32
{
    public sealed class FatLayout
    {
        public readonly int SectorSize, SectorsPerCluster;
        public readonly uint TotalSectors, FatSectors, ClusterCount;
        public const uint Reserved = 32;
        public long DataOffset { get { return ((long)Reserved + 2L * FatSectors) * SectorSize; } }
        public int ClusterBytes { get { return SectorSize * SectorsPerCluster; } }

        public FatLayout(long bytes, int sectorSize, int clusterBytes)
        {
            if (sectorSize != 512 && sectorSize != 4096) throw new ArgumentException("Solo se admiten sectores lógicos de 512 o 4096 bytes.");
            if (bytes < 512L * 1024 * 1024 || bytes > 2L * 1024 * 1024 * 1024 * 1024 || bytes % sectorSize != 0)
                throw new ArgumentException("El volumen debe medir entre 512 MiB y 2 TiB y estar alineado al sector.");
            if (clusterBytes < sectorSize || clusterBytes > 32768 || clusterBytes % sectorSize != 0 || (clusterBytes & (clusterBytes - 1)) != 0)
                throw new ArgumentException("Tamaño de clúster inválido (máximo 32 KiB).");
            SectorSize = sectorSize;
            SectorsPerCluster = clusterBytes / sectorSize;
            long total = bytes / sectorSize;
            if (total > uint.MaxValue) throw new ArgumentException("La cantidad de sectores excede FAT32.");
            TotalSectors = (uint)total;
            // Solve monotonically for the smallest FAT able to address every data cluster.
            long low = 1, high = (total * 4 + sectorSize - 1) / sectorSize;
            while (low < high)
            {
                long mid = (low + high) / 2;
                long clusters = (total - Reserved - 2 * mid) / SectorsPerCluster;
                if (mid * sectorSize / 4 >= clusters + 2) high = mid;
                else low = mid + 1;
            }
            FatSectors = (uint)low;
            ClusterCount = (uint)((total - Reserved - 2 * low) / SectorsPerCluster);
            if (ClusterCount < 65525 || ClusterCount > 0x0FFFFFEE)
                throw new ArgumentException("Ese tamaño de clúster no produce un volumen FAT32 válido. Usa Automático.");
        }

        public static FatLayout Automatic(long bytes, int sector)
        {
            if (sector != 512 && sector != 4096) throw new ArgumentException("Solo se admiten sectores lógicos de 512 o 4096 bytes.");
            if (bytes < 512L * 1024 * 1024 || bytes > 2L * 1024 * 1024 * 1024 * 1024 || bytes % sector != 0)
                throw new ArgumentException("El volumen debe medir entre 512 MiB y 2 TiB y estar alineado al sector.");
            if (bytes / sector > uint.MaxValue) throw new ArgumentException("La cantidad de sectores excede FAT32.");
            int preferred = bytes <= 8L * 1024 * 1024 * 1024 ? 4096 : bytes <= 32L * 1024 * 1024 * 1024 ? 16384 : 32768;
            preferred = Math.Max(preferred, sector);
            for (int size = preferred; size >= sector; size /= 2)
            {
                try { return new FatLayout(bytes, sector, size); } catch (ArgumentException) { }
            }
            throw new ArgumentException("No se pudo obtener una geometría FAT32 compatible.");
        }
    }

    public static class Fat32
    {
        public static string NormalizeLabel(string label)
        {
            label = (label ?? "").Trim().ToUpperInvariant();
            if (label.Length > 11) throw new ArgumentException("La etiqueta admite hasta 11 caracteres.");
            foreach (char c in label)
                if (c < 32 || c > 126 || "\"*+,./:;<=>?[\\]|".IndexOf(c) >= 0)
                    throw new ArgumentException("Usa letras sin acentos, números, espacios o guiones para la etiqueta.");
            return label;
        }

        public static void Put16(byte[] b, int at, ushort v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b, at, 2); }
        public static void Put32(byte[] b, int at, uint v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b, at, 4); }

        public static byte[] Boot(FatLayout f, string label, uint hidden, uint serial)
        {
            var b = new byte[f.SectorSize];
            b[0] = 0xEB; b[1] = 0x58; b[2] = 0x90;
            Encoding.ASCII.GetBytes("REALFAT ").CopyTo(b, 3);
            Put16(b, 11, (ushort)f.SectorSize); b[13] = (byte)f.SectorsPerCluster;
            Put16(b, 14, (ushort)FatLayout.Reserved); b[16] = 2; b[21] = 0xF8;
            Put16(b, 24, 63); Put16(b, 26, 255); Put32(b, 28, hidden);
            Put32(b, 32, f.TotalSectors); Put32(b, 36, f.FatSectors);
            Put32(b, 44, 2); Put16(b, 48, 1); Put16(b, 50, 6);
            b[64] = 0x80; b[66] = 0x29; Put32(b, 67, serial);
            Encoding.ASCII.GetBytes((label.Length == 0 ? "NO NAME" : label).PadRight(11)).CopyTo(b, 71);
            Encoding.ASCII.GetBytes("FAT32   ").CopyTo(b, 82);
            // Non-bootable volume: halt rather than execute zero-filled memory.
            b[90] = 0xFA; b[91] = 0xF4; b[92] = 0xEB; b[93] = 0xFD;
            b[510] = 0x55; b[511] = 0xAA;
            return b;
        }

        public static byte[] Info(FatLayout f)
        {
            var b = new byte[f.SectorSize];
            Put32(b, 0, 0x41615252); Put32(b, 484, 0x61417272);
            Put32(b, 488, f.ClusterCount - 1); Put32(b, 492, 3); Put32(b, 508, 0xAA550000);
            return b;
        }

        private static void WriteAt(Stream s, long offset, byte[] data)
        {
            s.Position = offset; s.Write(data, 0, data.Length);
        }

        // Writes only metadata and the root cluster. Does not erase old file contents.
        // All writes and buffers are whole logical sectors, including raw-device writes.
        public static void Format(Stream stream, FatLayout f, string label, uint hidden, Action<int, string> progress)
        {
            label = NormalizeLabel(label);
            long metadataEnd = f.DataOffset + f.ClusterBytes;
            var zero = new byte[1024 * 1024];
            stream.Position = 0;
            for (long done = 0; done < metadataEnd; )
            {
                int count = (int)Math.Min(zero.Length, metadataEnd - done);
                stream.Write(zero, 0, count); done += count;
                progress((int)(done * 88 / metadataEnd), "Inicializando tablas FAT…");
            }
            var first = new byte[f.SectorSize];
            Put32(first, 0, 0x0FFFFFF8); Put32(first, 4, 0xFFFFFFFF); Put32(first, 8, 0x0FFFFFFF);
            WriteAt(stream, (long)FatLayout.Reserved * f.SectorSize, first);
            WriteAt(stream, ((long)FatLayout.Reserved + f.FatSectors) * f.SectorSize, first);
            if (label.Length > 0)
            {
                var root = new byte[f.SectorSize];
                Encoding.ASCII.GetBytes(label.PadRight(11)).CopyTo(root, 0); root[11] = 0x08;
                WriteAt(stream, f.DataOffset, root);
            }
            var info = Info(f);
            WriteAt(stream, f.SectorSize, info); WriteAt(stream, 7L * f.SectorSize, info);
            var end = new byte[f.SectorSize]; end[510] = 0x55; end[511] = 0xAA;
            WriteAt(stream, 2L * f.SectorSize, end); WriteAt(stream, 8L * f.SectorSize, end);
            uint serial = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0);
            var boot = Boot(f, label, hidden, serial);
            WriteAt(stream, 6L * f.SectorSize, boot);
            stream.Flush();
            progress(94, "Guardando sector de arranque…");
            WriteAt(stream, 0, boot); stream.Flush();
        }
    }
}
