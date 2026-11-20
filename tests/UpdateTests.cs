using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using RealFat32;

internal static class UpdateTests
{
    private static int checks;
    private static readonly string AssetRoot = "https://github.com/poligontek/RealFat32/releases/download/v9.0.0-alpha.1/";
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    private static string Json(params object[] releases) { return new JavaScriptSerializer().Serialize(releases); }
    private static Dictionary<string, object> Release(string tag, bool draft = false, bool prerelease = false, string origin = null, bool checksum = true)
    {
        return new Dictionary<string, object> { { "tag_name", tag }, { "draft", draft }, { "prerelease", prerelease }, { "body", "Fixture release notes" }, { "assets", new object[] {
            new Dictionary<string, object> { { "name", "RealFat32.exe" }, { "browser_download_url", (origin ?? AssetRoot) + "RealFat32.exe" }, { "digest", checksum ? "sha256:" + new string('a', 64) : null } }
        } } };
    }
    private static int Main(string[] args)
    {
        try { Run(args).GetAwaiter().GetResult(); Console.WriteLine("PASS: " + checks + " update checks. Mock HTTP and disposable executable replacement only."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static async Task Run(string[] args)
    {
        Ui.Language = "en";
        string[] ordered = { "1.0.0-alpha.1", "1.0.0-alpha.2", "1.0.0-alpha.10", "1.0.0-beta.1", "1.0.0-rc.1", "1.0.0", "1.0.1", "2.0.0" };
        for (int i = 1; i < ordered.Length; i++) Check(ReleaseVersion.Parse(ordered[i]).CompareTo(ReleaseVersion.Parse(ordered[i - 1])) > 0, "Semantic release ordering");
        foreach (var invalid in new[] { "", "latest", "01.2.3", "1.2", "1.2.3-alpha.01", "1.2.3-preview.1", "1.2.3;cmd", "99999999999999.0.0" }) Check(ReleaseVersion.Parse(invalid) == null, "Malformed version rejected");
        string json = Json(Release("v1.0.0-beta.1", prerelease: true), Release("v0.9.0"), Release("v1.1.0-alpha.1", prerelease: true), Release("v0.9.1-rc.1", prerelease: true));
        Check(UpdateService.Select(json, "0.3.0-alpha.1", "stable").Version.Value == "0.9.0", "Stable excludes prereleases");
        Check(UpdateService.Select(json, "0.3.0-alpha.1", "beta").Version.Value == "1.0.0-beta.1", "Beta excludes alpha");
        Check(UpdateService.Select(json, "0.3.0-alpha.1", "alpha").Version.Value == "1.1.0-alpha.1", "Alpha includes all channels");
        Check(UpdateService.Select("[]", "0.3.0-alpha.1", "alpha") == null, "No releases means no update");
        Check(UpdateService.Select(Json(Release("v0.3.0-alpha.1", prerelease: true)), "0.3.0-alpha.1", "alpha") == null, "Same version is not an update");
        Check(UpdateService.Select(Json(Release("v0.2.0")), "0.3.0-alpha.1", "alpha") == null, "Never downgrade");
        Check(UpdateService.Select(Json(Release("v9.0.0", draft: true)), "0.3.0-alpha.1", "alpha") == null, "Draft ignored");
        Check(UpdateService.Select(Json(Release("v9.0.0", prerelease: true)), "0.3.0-alpha.1", "stable") == null, "Inconsistent stable prerelease ignored");
        Check(UpdateService.Select(Json(Release("v9.0.0", checksum: false)), "0.3.0-alpha.1", "alpha") == null, "Unverifiable release ignored");
        foreach (string url in new[] { "http://github.com/poligontek/RealFat32/releases/download/v1/app.exe", "https://evil.example/file.exe", "https://github.com/other/RealFat32/releases/download/v1/file.exe", "https://github.com/poligontek/RealFat32/releases/download/v1/file.exe?x=1", "https://github.com:444/poligontek/RealFat32/releases/download/v1/file.exe" })
        { Check(!UpdateService.IsAssetUrl(url), "Reject foreign origin"); }
        Check(UpdateService.Select(Json(Release("v9.0.0", origin: "https://evil.example/")), "0.3.0-alpha.1", "alpha") == null, "Foreign assets do not produce an update");
        Check(UpdateService.Quote("C:\\Update folder\\RealFat32.exe") == "\"C:\\Update folder\\RealFat32.exe\"", "Paths with spaces quoted");
        var empty = new UpdateService(() => new HttpClient(new FakeHandler(r => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") })));
        Check(await empty.CheckAsync("stable", CancellationToken.None) == null, "Empty API response is quiet");
        foreach (HttpStatusCode status in new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.ServiceUnavailable })
        {
            var failed = new UpdateService(() => new HttpClient(new FakeHandler(r => new HttpResponseMessage(status))));
            bool rejected = false; try { await failed.CheckAsync("stable", CancellationToken.None); } catch (Exception) { rejected = true; }
            Check(rejected, "Network/API errors are distinct from no updates");
        }
        byte[] fixture = File.ReadAllBytes(args[1]); string goodHash = Hash(fixture);
        var badDownload = new UpdateService(() => new HttpClient(new FakeHandler(r => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fixture) })));
        bool mismatch = false;
        try { await badDownload.DownloadAsync(new UpdateRelease { Version = ReleaseVersion.Parse("9.0.0-alpha.1"), DownloadUrl = AssetRoot + "RealFat32.exe", Digest = new string('0', 64) }, CancellationToken.None); } catch (InvalidDataException) { mismatch = true; }
        Check(mismatch, "Tampered download rejected before staging");
        string root = Path.Combine(Path.GetDirectoryName(args[1]), "Update smoke " + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var goodDownload = new UpdateService(() => new HttpClient(new FakeHandler(r => new HttpResponseMessage(HttpStatusCode.OK) { Content = r.RequestUri.AbsolutePath.EndsWith(".sha256") ? (HttpContent)new StringContent(goodHash + "  RealFat32.exe\n") : new ByteArrayContent(fixture) })), root);
            var release = new UpdateRelease { Version = ReleaseVersion.Parse("9.0.0-alpha.1"), DownloadUrl = AssetRoot + "RealFat32.exe", ChecksumUrl = AssetRoot + "RealFat32.exe.sha256" };
            string source = await goodDownload.DownloadAsync(release, CancellationToken.None);
            Check(Hash(File.ReadAllBytes(source)) == goodHash && release.Digest == goodHash, "Verified checksum fallback and executable metadata");
            string targetFolder = Path.Combine(root, "installed"); Directory.CreateDirectory(targetFolder);
            string target = Path.Combine(targetFolder, "RealFat32.exe"); File.WriteAllText(target, "old executable fixture");
            using (var parent = Process.Start(new ProcessStartInfo(args[1], "--wait") { UseShellExecute = false, CreateNoWindow = true }))
            {
                string[] workerArgs = { parent.Id.ToString(), parent.StartTime.ToUniversalTime().Ticks.ToString(), target, source, goodHash, "en" };
                using (var worker = Process.Start(new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(args[1]), "RealFat32.Updater.exe"), String.Join(" ", workerArgs.Select(UpdateService.Quote))) { UseShellExecute = false, CreateNoWindow = true }))
                {
                    Thread.Sleep(100); Check(File.ReadAllText(target) == "old executable fixture", "Replacement waits for parent exit");
                    if (!worker.WaitForExit(10000)) { worker.Kill(); throw new Exception("Update worker timed out"); }
                    Check(worker.ExitCode == 0, "Update worker completed");
                }
            }
            Check(Hash(File.ReadAllBytes(target)) == goodHash, "Executable replaced atomically");
            Check(File.ReadAllText(target + ".previous") == "old executable fixture", "Previous executable backed up");
            var clock = Stopwatch.StartNew(); string marker = Path.Combine(targetFolder, "started.marker");
            while (!File.Exists(marker) && clock.ElapsedMilliseconds < 3000) Thread.Sleep(20);
            Check(File.Exists(marker), "Updated executable restarted successfully");
            // The marker is written just before the fixture exits; Windows still locks its image briefly.
            var cleanupClock = Stopwatch.StartNew();
            while (true)
            {
                try { File.Delete(target); break; }
                catch (UnauthorizedAccessException) { if (cleanupClock.ElapsedMilliseconds > 3000) throw; Thread.Sleep(30); }
                catch (IOException) { if (cleanupClock.ElapsedMilliseconds > 3000) throw; Thread.Sleep(30); }
            }
            File.Delete(marker); File.Delete(target + ".previous"); Directory.Delete(targetFolder);
            File.Delete(source); Directory.Delete(Path.GetDirectoryName(source));
        }
        finally { if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root); }
    }
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> send;
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) { send = handler; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { return Task.FromResult(send(request)); }
    }
}
