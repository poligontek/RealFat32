using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RealFat32
{
    public sealed class Volume
    {
        public string Letter, Label, FileSystem, Model, Identity, VolumePath, Blocked;
        public uint Disk, SectorSize, PartitionStyle, MbrType;
        public long Size, Offset, Free;
        public bool Allowed { get { return String.IsNullOrEmpty(Blocked); } }
        public string Fingerprint { get { return Identity + "|" + VolumePath + "|" + Disk + "|" + Offset + "|" + Size + "|" + SectorSize; } }
        public override string ToString() { return Letter + "   " + (String.IsNullOrEmpty(Label) ? "Sin etiqueta" : Label) + "   ·   " + Human(Size); }
        public static string Human(long n) { return n >= 1099511627776L ? (n / 1099511627776.0).ToString("0.##") + " TiB" : (n / 1073741824.0).ToString("0.##") + " GiB"; }
    }

    public static class Volumes
    {
        public static bool IsAdmin { get { using (var id = WindowsIdentity.GetCurrent()) return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator); } }
        private static List<ManagementObject> Query(string scope, string sql)
        {
            using (var search = new ManagementObjectSearcher(scope, sql))
            using (var results = search.Get()) return results.Cast<ManagementObject>().ToList();
        }
        private static string S(ManagementObject m, string p) { return Convert.ToString(m[p]); }
        private static bool B(ManagementObject m, string p) { return Convert.ToBoolean(m[p]); }

        public static List<Volume> Scan()
        {
            const string storage = @"root\Microsoft\Windows\Storage";
            var disks = Query(storage, "SELECT * FROM MSFT_Disk");
            var parts = Query(storage, "SELECT * FROM MSFT_Partition");
            var pages = Query(@"root\cimv2", "SELECT Name FROM Win32_PageFileUsage");
            var protectedLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            protectedLetters.Add(Path.GetPathRoot(Environment.SystemDirectory).Substring(0, 2));
            protectedLetters.Add(Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory).Substring(0, 2));
            foreach (var p in pages) protectedLetters.Add(S(p, "Name").Substring(0, 2));
            var protectedDisks = new HashSet<uint>();
            foreach (var p in parts)
            {
                string letter = Convert.ToChar(p["DriveLetter"]) + ":";
                if (B(p, "IsBoot") || B(p, "IsSystem") || protectedLetters.Contains(letter))
                    protectedDisks.Add(Convert.ToUInt32(p["DiskNumber"]));
            }
            var encrypted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool encryptionKnown = true;
            try
            {
                foreach (var e in Query(@"root\cimv2\Security\MicrosoftVolumeEncryption", "SELECT * FROM Win32_EncryptableVolume"))
                {
                    using (var status = e.InvokeMethod("GetConversionStatus", null, null))
                    {
                        if (status == null || Convert.ToUInt32(status["ReturnValue"]) != 0 || Convert.ToUInt32(status["ConversionStatus"]) != 0)
                            encrypted.Add(S(e, "DriveLetter"));
                    }
                }
            }
            catch (ManagementException ex)
            {
                // A missing optional BitLocker provider means this edition has no provider.
                // Other failures (including access denied) must not bypass the check.
                if (ex.ErrorCode != ManagementStatus.InvalidNamespace && ex.ErrorCode != ManagementStatus.InvalidClass) encryptionKnown = false;
            }
            catch (UnauthorizedAccessException) { encryptionKnown = false; }
            var list = new List<Volume>();
            foreach (var p in parts)
            {
                char drive = Convert.ToChar(p["DriveLetter"]);
                if (drive < 'A' || drive > 'Z') continue;
                uint number = Convert.ToUInt32(p["DiskNumber"]);
                var d = disks.FirstOrDefault(x => Convert.ToUInt32(x["Number"]) == number);
                if (d == null) continue;
                var v = new Volume { Letter = drive + ":", Disk = number, Offset = Convert.ToInt64(p["Offset"]), Size = Convert.ToInt64(p["Size"]),
                    SectorSize = Convert.ToUInt32(d["LogicalSectorSize"]), Model = S(d, "FriendlyName"),
                    PartitionStyle = Convert.ToUInt32(d["PartitionStyle"]), MbrType = Convert.ToUInt32(p["MbrType"]),
                    Identity = S(d, "UniqueId") + "|" + S(d, "SerialNumber"), Free = -1 };
                var path = new StringBuilder(1024);
                if (!Native.GetVolumeNameForVolumeMountPoint(v.Letter + "\\", path, path.Capacity)) v.Blocked = "No se pudo identificar el volumen.";
                v.VolumePath = path.ToString();
                try { var di = new DriveInfo(v.Letter); v.Label = di.VolumeLabel; v.FileSystem = di.DriveFormat; v.Free = di.AvailableFreeSpace; }
                catch (IOException) { v.FileSystem = "RAW / no disponible"; }
                catch (UnauthorizedAccessException) { v.FileSystem = "Sin acceso"; }
                if (!encryptionKnown) v.Blocked = "Abre como administrador para comprobar el cifrado.";
                if (encrypted.Contains(v.Letter)) v.Blocked = "Volumen cifrado con BitLocker. Descífralo primero.";
                if (B(d, "IsReadOnly") || B(p, "IsReadOnly")) v.Blocked = "Unidad de solo lectura.";
                if (B(d, "IsOffline") || B(p, "IsOffline")) v.Blocked = "Unidad fuera de línea.";
                if (v.PartitionStyle == 1 && v.MbrType != 0x07 && v.MbrType != 0x0B && v.MbrType != 0x0C && v.MbrType != 0x06 && v.MbrType != 0x0E)
                    v.Blocked = "Tipo de partición MBR reservado o no compatible.";
                if (v.PartitionStyle == 2 && !String.Equals(S(p, "GptType").Trim('{', '}'), "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7", StringComparison.OrdinalIgnoreCase))
                    v.Blocked = "Solo se admiten particiones GPT de datos básicos.";
                if (v.PartitionStyle != 1 && v.PartitionStyle != 2) v.Blocked = "Tabla de particiones no compatible.";
                try { FatLayout.Automatic(v.Size, (int)v.SectorSize); } catch (ArgumentException ex) { v.Blocked = ex.Message; }
                if (String.IsNullOrWhiteSpace(S(d, "UniqueId"))) v.Blocked = "El disco no proporciona una identidad estable.";
                if (protectedDisks.Contains(number)) v.Blocked = "Disco protegido: Windows, arranque, paginación o RealFat32.";
                list.Add(v);
            }
            foreach (var o in disks.Concat(parts).Concat(pages)) o.Dispose();
            return list.OrderBy(v => v.Letter).ToList();
        }

        public static void Format(Volume selected, int clusterBytes, string label, Action<int, string> progress)
        {
            if (!IsAdmin) throw new InvalidOperationException("Se requieren permisos de administrador.");
            label = Fat32.NormalizeLabel(label);
            var current = Scan().FirstOrDefault(v => v.Letter == selected.Letter);
            if (current == null || current.Fingerprint != selected.Fingerprint) throw new IOException("La unidad cambió. Actualiza y vuelve a seleccionarla.");
            if (!current.Allowed) throw new IOException(current.Blocked);
            FatLayout f = clusterBytes == 0 ? FatLayout.Automatic(current.Size, (int)current.SectorSize) : new FatLayout(current.Size, (int)current.SectorSize, clusterBytes);
            if (current.Offset % current.SectorSize != 0 || (ulong)(current.Offset / current.SectorSize) > uint.MaxValue)
                throw new IOException("El inicio de la partición no se puede representar en FAT32.");
            // Open by volume GUID rather than a drive letter that can be reassigned.
            using (var handle = Native.CreateFile(current.VolumePath.TrimEnd('\\'), 0xC0000000, 3, IntPtr.Zero, 3, 0x80000000, IntPtr.Zero))
            {
                if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                Native.Control(handle, 0x00090018); // FSCTL_LOCK_VOLUME: abort if anything is using it.
                bool written = false;
                try
                {
                    var extent = Native.Control(handle, 0x00560000, 1024);
                    if (BitConverter.ToUInt32(extent, 0) != 1 || BitConverter.ToUInt32(extent, 8) != current.Disk ||
                        BitConverter.ToInt64(extent, 16) != current.Offset || BitConverter.ToInt64(extent, 24) != current.Size)
                        throw new IOException("La identidad o los límites del volumen no coinciden. No se escribió nada.");
                    var length = Native.Control(handle, 0x0007405C, 8); // IOCTL_DISK_GET_LENGTH_INFO
                    if (BitConverter.ToInt64(length, 0) != current.Size) throw new IOException("La capacidad del volumen cambió.");
                    var geo = Native.Control(handle, 0x00070000, 24); // IOCTL_DISK_GET_DRIVE_GEOMETRY
                    if (BitConverter.ToUInt32(geo, 20) != current.SectorSize) throw new IOException("El tamaño de sector cambió.");
                    progress(0, "Volumen bloqueado. Iniciando formato rápido…");
                    using (var stream = new FileStream(handle, FileAccess.ReadWrite, 4096, false))
                    {
                        written = true;
                        Fat32.Format(stream, f, label, (uint)(current.Offset / current.SectorSize), progress);
                        stream.Flush(true);
                        if (current.PartitionStyle == 1) Native.SetFat32PartitionType(handle);
                        Native.Control(handle, 0x00090020); // dismount while still holding exclusive lock
                        Native.Control(handle, 0x0009001C);
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException((written ? "El formato se interrumpió. El volumen puede haber quedado sin formato. " : "No se inició el formato. ") + ex.Message, ex);
                }
                // Closing the handle also releases the volume lock on every failure path.
            }
            progress(100, "Formato finalizado. FAT32 está listo.");
        }
    }

    internal static class Native
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetVolumeNameForVolumeMountPoint(string mount, StringBuilder name, int size);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControl(SafeFileHandle h, uint code, IntPtr input, int inputSize, byte[] output, int outputSize, out int returned, IntPtr overlapped);
        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControlInput(SafeFileHandle h, uint code, byte[] input, int inputSize, IntPtr output, int outputSize, out int returned, IntPtr overlapped);
        internal static void SetFat32PartitionType(SafeFileHandle h)
        {
            int returned;
            if (!DeviceIoControlInput(h, 0x0007C008, new byte[] { 0x0C }, 1, IntPtr.Zero, 0, out returned, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudo actualizar el tipo MBR a FAT32 LBA.");
        }
        internal static byte[] Control(SafeFileHandle h, uint code, int size = 0)
        {
            var b = new byte[size]; int returned;
            if (!DeviceIoControl(h, code, IntPtr.Zero, 0, b, size, out returned, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), code == 0x90018 ? "La unidad está en uso. Cierra sus archivos, ventanas y aplicaciones y vuelve a intentar." : "Windows rechazó la operación del volumen.");
            if (returned < size && size > 0 && returned < (code == 0x560000 ? 32 : size)) throw new IOException("Windows devolvió información incompleta del disco.");
            return b;
        }
    }
}
