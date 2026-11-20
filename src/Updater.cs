using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RealFat32
{
    internal sealed class UpdateRelease
    {
        public ReleaseVersion Version;
        public string Notes, DownloadUrl, Digest, ChecksumUrl;
    }
    internal sealed class UpdateService
    {
        private const int MaxExeBytes = 40 * 1024 * 1024;
        private readonly Func<HttpClient> clientFactory;
        private readonly string stagingRoot;
        internal UpdateService(Func<HttpClient> factory = null, string testStagingRoot = null) { clientFactory = factory; stagingRoot = testStagingRoot; }
        internal static bool IsAssetUrl(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) && uri.Scheme == "https" && uri.Host == "github.com" && uri.IsDefaultPort && String.IsNullOrEmpty(uri.UserInfo) &&
                uri.AbsolutePath.StartsWith("/" + ReleaseInfo.Repository + "/releases/download/", StringComparison.Ordinal) && String.IsNullOrEmpty(uri.Query) && String.IsNullOrEmpty(uri.Fragment);
        }
        private HttpClient Client()
        {
            if (clientFactory != null) return clientFactory();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RealFat32/" + ReleaseInfo.Version);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }
        private static string S(Dictionary<string, object> data, string key) { object v; return data.TryGetValue(key, out v) ? Convert.ToString(v) : ""; }
        private static bool B(Dictionary<string, object> data, string key) { object v; return data.TryGetValue(key, out v) && v is bool && (bool)v; }
        internal static UpdateRelease Select(string json, string current, string channel)
        {
            var parser = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
            var releases = parser.DeserializeObject(json) as object[];
            if (releases == null) throw new InvalidDataException("Invalid release list");
            var installed = ReleaseVersion.Parse(current);
            if (installed == null) throw new InvalidDataException("Invalid installed version");
            UpdateRelease best = null;
            foreach (var item in releases)
            {
                var data = item as Dictionary<string, object>;
                if (data == null || B(data, "draft")) continue;
                var version = ReleaseVersion.Parse(S(data, "tag_name"));
                if (version == null || !version.InChannel(channel) || version.CompareTo(installed) <= 0 || (version.Stage == 3 && B(data, "prerelease"))) continue;
                object rawAssets; if (!data.TryGetValue("assets", out rawAssets)) continue;
                var assets = rawAssets as object[]; if (assets == null) continue;
                string download = null, digest = null, checksum = null;
                foreach (var asset in assets.OfType<Dictionary<string, object>>())
                {
                    string name = S(asset, "name"), url = S(asset, "browser_download_url");
                    if (!IsAssetUrl(url)) continue;
                    if (name == "RealFat32.exe") { download = url; digest = S(asset, "digest"); }
                    if (name == "RealFat32.exe.sha256") checksum = url;
                }
                if (download == null) continue;
                if (digest != null && Regex.IsMatch(digest, "^sha256:[a-fA-F0-9]{64}$")) digest = digest.Substring(7);
                else digest = null;
                if (digest == null && checksum == null) continue;
                if (best == null || version.CompareTo(best.Version) > 0) best = new UpdateRelease { Version = version, DownloadUrl = download, Digest = digest, ChecksumUrl = checksum, Notes = S(data, "body") };
            }
            return best;
        }
        public async Task<UpdateRelease> CheckAsync(string channel, CancellationToken cancel)
        {
            using (var client = Client())
            using (var response = await client.GetAsync("https://api.github.com/repos/" + ReleaseInfo.Repository + "/releases?per_page=100", HttpCompletionOption.ResponseHeadersRead, cancel))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidOperationException(Ui.T("El repositorio aún no está disponible públicamente."));
                response.EnsureSuccessStatusCode();
                var json = Encoding.UTF8.GetString(await ReadLimited(response, 4 * 1024 * 1024, cancel));
                return Select(json, ReleaseInfo.Version, channel);
            }
        }
        private static async Task<byte[]> ReadLimited(HttpResponseMessage response, int limit, CancellationToken cancel)
        {
            if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("Download exceeds size limit");
            using (var input = await response.Content.ReadAsStreamAsync())
            using (var output = new MemoryStream())
            {
                var buffer = new byte[32768]; int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancel)) > 0)
                { if (output.Length + read > limit) throw new InvalidDataException("Download exceeds size limit"); await output.WriteAsync(buffer, 0, read, cancel); }
                return output.ToArray();
            }
        }
        public async Task<string> DownloadAsync(UpdateRelease release, CancellationToken cancel)
        {
            if (!IsAssetUrl(release.DownloadUrl)) throw new InvalidDataException("Invalid release origin");
            using (var client = Client())
            {
                string hash = release.Digest;
                if (hash == null)
                {
                    if (!IsAssetUrl(release.ChecksumUrl)) throw new InvalidDataException("Missing checksum");
                    using (var response = await client.GetAsync(release.ChecksumUrl, HttpCompletionOption.ResponseHeadersRead, cancel))
                    {
                        response.EnsureSuccessStatusCode(); string text = Encoding.UTF8.GetString(await ReadLimited(response, 1024, cancel)).Trim();
                        var match = Regex.Match(text, @"^([a-fA-F0-9]{64})(?:\s+\*?RealFat32\.exe)?$");
                        if (!match.Success) throw new InvalidDataException("Invalid SHA256 manifest"); hash = match.Groups[1].Value;
                    }
                }
                byte[] exe;
                using (var response = await client.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancel))
                { response.EnsureSuccessStatusCode(); exe = await ReadLimited(response, MaxExeBytes, cancel); }
                using (var sha = SHA256.Create()) if (!String.Equals(Hex(sha.ComputeHash(exe)), hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(Ui.T("La verificación SHA256 de la actualización falló."));
                if (exe.Length < 512 || exe[0] != 'M' || exe[1] != 'Z') throw new InvalidDataException("Invalid Windows executable");
                string root = stagingRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RealFat32", "Updates");
                string folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder); string file = Path.Combine(folder, "RealFat32.exe"); File.WriteAllBytes(file, exe);
                var info = FileVersionInfo.GetVersionInfo(file);
                if (info.ProductName != "RealFat32" || info.CompanyName != ReleaseInfo.Company || info.ProductVersion != release.Version.Value) throw new InvalidDataException(Ui.T("La versión descargada no coincide con la publicación."));
                release.Digest = hash;
                return file;
            }
        }
        private static string Hex(byte[] value) { return BitConverter.ToString(value).Replace("-", "").ToLowerInvariant(); }
        public void LaunchInstaller(string source, string target, string hash)
        {
            if (Path.GetFileName(target) != "RealFat32.exe") throw new IOException(Ui.T("El ejecutable debe llamarse RealFat32.exe para actualizarse."));
            string helper = Path.Combine(Path.GetDirectoryName(source), "RealFat32.Updater.exe");
            using (var resource = typeof(UpdateService).Assembly.GetManifestResourceStream("RealFat32.Updater.exe"))
            using (var output = File.Create(helper)) { if (resource == null) throw new IOException("Updater resource missing"); resource.CopyTo(output); }
            var parent = Process.GetCurrentProcess();
            string[] args = { parent.Id.ToString(), parent.StartTime.ToUniversalTime().Ticks.ToString(), target, source, hash, Ui.Language };
            var start = new ProcessStartInfo(helper, String.Join(" ", args.Select(Quote))) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(helper) };
            using (var worker = Process.Start(start)) { if (worker == null) throw new IOException("Updater could not start"); }
        }
        internal static string Quote(string value)
        {
            if (value.Contains("\"") || value.EndsWith("\\")) throw new ArgumentException("Invalid updater argument");
            return "\"" + value + "\"";
        }
    }
}
