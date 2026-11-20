using System;
using System.Text.RegularExpressions;

namespace RealFat32
{
    internal static class ReleaseInfo
    {
        public const string Version = "0.3.0-alpha.1";
        public const string Repository = "poligontek/RealFat32";
        public const string Company = "PoligonTek";
        public const string Author = "Fabian Gonzalez Dev";
        public static string Channel { get { return "Alpha"; } }
        public static string Display { get { return Version + " · " + Channel; } }
    }

    internal sealed class ReleaseVersion : IComparable<ReleaseVersion>
    {
        public int Major, Minor, Patch, Stage, Sequence;
        public string Value;
        public static ReleaseVersion Parse(string value)
        {
            var m = Regex.Match(value ?? "", @"^v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(alpha|beta|rc)\.(0|[1-9][0-9]*))?$");
            int major, minor, patch, sequence = 0;
            if (!m.Success || !Int32.TryParse(m.Groups[1].Value, out major) || !Int32.TryParse(m.Groups[2].Value, out minor) || !Int32.TryParse(m.Groups[3].Value, out patch) ||
                (m.Groups[4].Success && !Int32.TryParse(m.Groups[5].Value, out sequence))) return null;
            return new ReleaseVersion { Major = major, Minor = minor, Patch = patch, Stage = !m.Groups[4].Success ? 3 : m.Groups[4].Value == "rc" ? 2 : m.Groups[4].Value == "beta" ? 1 : 0, Sequence = sequence, Value = value.TrimStart('v') };
        }
        public int CompareTo(ReleaseVersion other)
        {
            if (other == null) return 1;
            int[] left = { Major, Minor, Patch, Stage, Sequence }, right = { other.Major, other.Minor, other.Patch, other.Stage, other.Sequence };
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return left[i].CompareTo(right[i]);
            return 0;
        }
        public bool InChannel(string channel) { return channel == "alpha" || (channel == "beta" && Stage >= 1) || Stage == 3; }
    }
}
