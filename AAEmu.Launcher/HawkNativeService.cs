using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AAEmu.Launcher
{
    // This is the player runtime. There are no WSL, Docker, SSH or publisher-secret paths.
    public sealed class HawkDeployment
    {
        public int schema = 1;
        public string platform = "windows-x86_64", manifestUrl = "", skateManifestUrl = "", lifecycleUrl = "", authUrl = "", discordUrl = "";
        public string authAddress = "24.16.12.90:13724", worldAddress = "24.16.12.90:18085";
        public bool publicDownloads, publicSessions;
        public static HawkDeployment Load(string file)
        {
            var c = JsonConvert.DeserializeObject<HawkDeployment>(File.ReadAllText(file));
            if (c == null || c.schema != 1 || c.platform != "windows-x86_64") throw new Exception("Unsupported publisher configuration.");
            foreach (var url in new[] { c.manifestUrl, c.skateManifestUrl, c.lifecycleUrl, c.authUrl, c.discordUrl })
                if (!string.IsNullOrWhiteSpace(url)) HawkNativeService.Https(url);
            return c;
        }
    }
    public sealed class HawkFile { public string path, sha256, url; public long size; public List<HawkFile> alternatives; }
    public sealed class HawkManifest { public int schema; public string version, platform, kind; public List<HawkFile> files; }
    public sealed class HawkNativeService
    {
        public readonly string Root;
        readonly string packageRoot, profilePath;
        readonly HawkDeployment deployment;
        readonly Action<string, int> report;
        readonly Func<HttpClient> httpFactory;
        public bool IsInstalled { get { return File.Exists(Path.Combine(Root, "current.json")); } }
        public bool HasWow { get { return File.Exists(Path.Combine(Root, "wow.json")); } }
        public bool HasSkate { get { return File.Exists(Path.Combine(Root, "skate.json")); } }
        public bool IsRunning { get; private set; }
        public bool RequiresSignIn { get { return (!deployment.publicDownloads && (!string.IsNullOrEmpty(deployment.manifestUrl) || !string.IsNullOrEmpty(deployment.skateManifestUrl))) || (!deployment.publicSessions && !string.IsNullOrEmpty(deployment.lifecycleUrl)); } }
        public HawkNativeService(string root, string package, string profile, HawkDeployment c, Action<string, int> progress, Func<HttpClient> transport = null)
        {
            Root = Path.GetFullPath(root); packageRoot = Path.GetFullPath(package); profilePath = profile; deployment = c; report = progress; httpFactory = transport;
            if (Root == Path.GetPathRoot(Root) || Root.StartsWith(@"\\", StringComparison.Ordinal) || Root.IndexOf(':', 2) >= 0)
                throw new Exception("Choose a local Windows game folder.");
        }
        public static Uri Https(string value)
        {
            Uri url;
            if (!Uri.TryCreate(value, UriKind.Absolute, out url) || url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment))
                throw new Exception("Publisher endpoints must use HTTPS without embedded credentials.");
            return url;
        }
        static bool SameOrigin(Uri a, Uri b) { return a.Scheme == b.Scheme && a.Host == b.Host && a.Port == b.Port; }
        public static string SafePath(string relative)
        {
            if (string.IsNullOrEmpty(relative) || relative.Contains('\\') || relative.Contains(':') || relative.StartsWith("/") || relative.Split('/').Any(p => p == "" || p == "." || p == ".." || p.EndsWith(".") || p.EndsWith(" ") || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Regex.IsMatch(p, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase)))
                throw new Exception("Unsafe package path.");
            return relative.Replace('/', Path.DirectorySeparatorChar);
        }
        static string Under(string folder, string relative)
        {
            var path = Path.GetFullPath(Path.Combine(folder, SafePath(relative)));
            if (!path.StartsWith(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new Exception("Path escaped the installation.");
            // Extended-length paths avoid depending on the machine-wide Windows
            // LongPathsEnabled policy for deeply nested runtime/notice files.
            if (path.Length >= 240 && !path.StartsWith(@"\\?\", StringComparison.Ordinal))
                path = path.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + path.Substring(2) : @"\\?\" + path;
            // Junctions/symlinks must not redirect writes outside the managed installation.
            for (var p = new DirectoryInfo(Path.GetDirectoryName(path)); p != null; p = p.Parent)
                if (p.Exists && UnsafeLink(p.FullName)) throw new Exception("Linked installation folders are not supported.");
            if (File.Exists(path) && UnsafeLink(path)) throw new Exception("Linked files are not supported.");
            return path;
        }
        [StructLayout(LayoutKind.Sequential)] struct AttributeTag { public uint attributes, tag; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetFileInformationByHandleEx(SafeFileHandle file, int infoClass, out AttributeTag info, uint size);
        static bool UnsafeLink(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) return false;
            using (var handle = CreateFile(path, 0, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero))
            {
                AttributeTag info;
                if (handle.IsInvalid || !GetFileInformationByHandleEx(handle, 9, out info, 8)) return true;
                // Cloud Files placeholders are not name-surrogate links. Reject all other
                // reparse types, including junctions/symlinks; hydrated OneDrive is safe.
                return (info.tag & 0xffff0fff) != 0x9000001a;
            }
        }
        public static HawkManifest ValidateManifest(string json)
        {
            var m = JsonConvert.DeserializeObject<HawkManifest>(json);
            if (m == null || m.schema != 1 || m.platform != "windows-x86_64" || !Regex.IsMatch(m.version ?? "", @"^[A-Za-z0-9][A-Za-z0-9._-]{0,80}$") || m.files == null || m.files.Count == 0 || m.files.Count > 20000)
                throw new Exception("This release is not a supported native Windows client.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long size = 0;
            foreach (var f in m.files)
            {
                SafePath(f.path);
                if (!new[] { "bin", "runtime", "tools", "licenses" }.Contains(f.path.Split('/')[0]) || !seen.Add(f.path) || f.size < 0 || f.size > 4L * 1024 * 1024 * 1024 || !Regex.IsMatch(f.sha256 ?? "", "^[a-f0-9]{64}$")) throw new Exception("Invalid release file metadata.");
                size = checked(size + f.size);
                if (size > 12L * 1024 * 1024 * 1024) throw new Exception("Release exceeds the installation limit.");
            }
            if (!seen.Contains("bin/benilla.exe")) throw new Exception("Native Windows client is missing from the release.");
            return m;
        }
        public static string Hash(string file)
        {
            using (var sha = SHA256.Create()) using (var input = File.OpenRead(file)) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }
        static bool Valid(string path, HawkFile f) { return File.Exists(path) && new FileInfo(path).Length == f.size && Hash(path) == f.sha256; }
        static void AtomicJson(string file, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (File.Exists(file)) File.Replace(temporary, file, null); else File.Move(temporary, file);
        }
        FileStream Lock()
        {
            Directory.CreateDirectory(Root);
            // Check the entire ancestry before acquiring a lock or creating package directories.
            Under(Root, "current.json");
            try { return new FileStream(Path.Combine(Root, ".operation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { throw new Exception("Another installation or game session is active."); }
        }
        string Pointer(string file, string parent, string field)
        {
            var value = (string)JObject.Parse(File.ReadAllText(Path.Combine(Root, file)))[field];
            if (string.IsNullOrEmpty(value) || value.Contains('/') || value.Contains('\\')) throw new Exception("Invalid installation pointer; use Repair or reimport assets.");
            return Under(Path.Combine(Root, parent), value);
        }
        public string ReleaseDirectory { get { return Pointer("current.json", "releases", "generation"); } }
        public string AssetDirectory(string kind) { return Pointer(kind + ".json", "assets", "folder"); }
        HttpClient Http()
        {
            if (httpFactory != null) return httpFactory();
            return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(3) };
        }
        static async Task<string> Body(HttpContent content, int limit, CancellationToken cancel)
        {
            using (var input = await content.ReadAsStreamAsync().ConfigureAwait(false)) using (var output = new MemoryStream())
            {
                var buffer = new byte[8192]; int count;
                while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancel).ConfigureAwait(false)) > 0)
                { if (output.Length + count > limit) throw new Exception("Publisher response exceeds the allowed size."); output.Write(buffer, 0, count); }
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }
        async Task<HttpResponseMessage> Get(HttpClient client, Uri uri, string token, long start, CancellationToken cancel)
        {
            var origin = uri;
            for (int count = 0; count < 5; count++)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    if (start > 0) request.Headers.Range = new RangeHeaderValue(start, null);
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        var next = response.Headers.Location == null ? null : new Uri(uri, response.Headers.Location);
                        response.Dispose();
                        if (next == null || !SameOrigin(origin, next)) throw new Exception("Download redirect refused: publisher credentials cannot leave the configured host.");
                        uri = next; continue;
                    }
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) { response.Dispose(); throw new HawkAccessDeniedException(); }
                    if (!response.IsSuccessStatusCode) { response.Dispose(); throw new Exception("Download failed. Retry Install / Play to resume."); }
                    return response;
                }
            }
            throw new Exception("Too many download redirects.");
        }
        async Task Download(HttpClient client, HawkFile f, string path, string token, CancellationToken cancel, bool publicAsset = false)
        {
            if (Valid(path, f)) return;
            if (File.Exists(path) && new FileInfo(path).Length >= f.size) File.Delete(path);
            long offset = File.Exists(path) ? new FileInfo(path).Length : 0;
            var url = Https(f.url);
            if (!publicAsset && !new[] { deployment.manifestUrl, deployment.skateManifestUrl }.Where(s => !string.IsNullOrEmpty(s)).Any(s => SameOrigin(Https(s), url))) throw new Exception("Release file is outside the publisher's download host.");
            if (publicAsset) token = ""; // Never send private launcher credentials to an asset mirror.
            using (var response = await Get(client, url, token, offset, cancel).ConfigureAwait(false))
            {
                bool resumed = offset > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                if (resumed && (response.Content.Headers.ContentRange == null || response.Content.Headers.ContentRange.From != offset || response.Content.Headers.ContentRange.Length != f.size)) throw new Exception("Invalid resumed download range.");
                if (!resumed) offset = 0;
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(path, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, true))
                {
                    var buffer = new byte[1024 * 1024]; int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancel).ConfigureAwait(false)) > 0)
                    {
                        offset += count; if (offset > f.size) throw new Exception("Download exceeds its manifest size.");
                        await output.WriteAsync(buffer, 0, count, cancel).ConfigureAwait(false);
                        report("Downloading " + f.path, f.size == 0 ? 100 : (int)(offset * 100 / f.size));
                    }
                }
            }
            if (!Valid(path, f)) { File.Delete(path); throw new Exception("Download checksum failed for " + f.path + ". Retry to restore it."); }
        }
        public const string WowDownloadUrl = "https://downloads.stonetavern.app/clients/classic-1.12.1/all/1.8/Stonetavern-Classic-1.12.1-v1.8.zip";
        public static HawkFile WowDownloadFile() { return new HawkFile { path = "Stonetavern-Classic-1.12.1-v1.8.zip", url = WowDownloadUrl, size = 5321185674L, sha256 = "0ebb9b5f0386d547e0715fdae57955743dd06e991b507ca5a7729d14af0479fc" }; }
        public async Task DownloadWowAssets(CancellationToken cancel)
        {
            var file = WowDownloadFile(); string archive;
            using (Lock()) using (var client = Http())
            {
                var downloads = Path.Combine(Root, "downloads"); Directory.CreateDirectory(downloads);
                archive = Under(downloads, file.path);
                report("Downloading WoW 1.12.1 assets from Stonetavern (4.96 GiB)…", 0);
                try { await Download(client, file, archive, "", cancel, true).ConfigureAwait(false); }
                catch (HawkAccessDeniedException) { throw new Exception("Stonetavern refused the WoW asset download. Retry later or import your existing ZIP/Data folder from Settings."); }
            }
            report("Archive checksum verified. Importing only required MPQs…", 100);
            ImportWow(archive, cancel);
        }
        public async Task Install(string token, CancellationToken cancel, bool repair = false)
        {
            if (deployment.publicDownloads) token = "";
            using (Lock()) using (var client = Http())
            {
                string json;
                if (!string.IsNullOrEmpty(deployment.manifestUrl))
                    using (var response = await Get(client, Https(deployment.manifestUrl), token, 0, cancel).ConfigureAwait(false))
                    { json = await Body(response.Content, 8 * 1024 * 1024, cancel).ConfigureAwait(false); }
                else
                {
                    var manifest = Path.Combine(packageRoot, "manifest.json");
                    if (!File.Exists(manifest)) throw new Exception("This preview has no native client package yet. The publisher must provide a Windows release; no developer installation is used.");
                    json = File.ReadAllText(manifest);
                }
                var m = ValidateManifest(json);
                if (!repair && IsInstalled)
                {
                    try
                    {
                        var existing = ValidateManifest(File.ReadAllText(Path.Combine(ReleaseDirectory, "manifest.json")));
                        if (existing.version == m.version && existing.files.Count == m.files.Count && m.files.All(f => existing.files.Any(e => e.path == f.path && e.size == f.size && e.sha256 == f.sha256)) && m.files.All(f => Valid(Under(ReleaseDirectory, f.path), f)))
                        { report("Client is up to date.", 100); return; }
                    }
                    catch (Exception) { report("Repairing the installed client…", 0); }
                }
                var releases = Path.Combine(Root, "releases"); Directory.CreateDirectory(releases);
                var stage = Path.Combine(releases, ".staging-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stage);
                var cache = Path.Combine(Root, "downloads"); Directory.CreateDirectory(cache);
                try
                {
                    int index = 0;
                    foreach (var f in m.files)
                    {
                        cancel.ThrowIfCancellationRequested(); var target = Under(stage, f.path); Directory.CreateDirectory(Path.GetDirectoryName(target));
                        if (string.IsNullOrEmpty(deployment.manifestUrl))
                        {
                            var original = Under(packageRoot, f.path);
                            if (!Valid(original, f)) throw new Exception("Bundled client file failed verification: " + f.path);
                            File.Copy(original, target);
                        }
                        else
                        {
                            var downloaded = Under(cache, f.sha256 + ".part");
                            await Download(client, f, downloaded, token, cancel).ConfigureAwait(false); File.Copy(downloaded, target);
                        }
                        report("Verified " + (++index) + "/" + m.files.Count + " files", index * 100 / m.files.Count);
                    }
                    AtomicJson(Path.Combine(stage, "manifest.json"), m);
                    var generation = m.version + "-" + Guid.NewGuid().ToString("N");
                    Directory.Move(stage, Path.Combine(releases, generation));
                    AtomicJson(Path.Combine(Root, "current.json"), new { generation, version = m.version });
                    report("Installed " + m.version + ". Your settings and assets are preserved.", 100);
                }
                finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
            }
        }
        public static HawkManifest ValidateSkateManifest(string json)
        {
            var m = JsonConvert.DeserializeObject<HawkManifest>(json);
            if (m == null || m.schema != 1 || m.kind != "skate-content" || !Regex.IsMatch(m.version ?? "", @"^[A-Za-z0-9][A-Za-z0-9._-]{0,80}$") || m.files == null || m.files.Count < 1 || m.files.Count > 20000) throw new Exception("Invalid Skate content manifest.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); long size = 0;
            foreach (var f in m.files)
            {
                SafePath(f.path);
                if (!(f.path.StartsWith("skate-data/assets/private/", StringComparison.Ordinal) || Regex.IsMatch(f.path, @"^skate-audio/[A-Za-z0-9_-]+\.wav$")) || !seen.Add(f.path) || f.size < 0 || f.size > 4L * 1024 * 1024 * 1024 || !Regex.IsMatch(f.sha256 ?? "", "^[a-f0-9]{64}$")) throw new Exception("Invalid Skate content file.");
                size = checked(size + f.size); if (size > 24L * 1024 * 1024 * 1024) throw new Exception("Skate content exceeds the installation limit.");
            }
            return m;
        }
        public async Task InstallSkateContent(string token, CancellationToken cancel, bool repair = false)
        {
            if (deployment.publicDownloads) token = "";
            using (Lock()) using (var client = Http())
            {
                // User-imported content remains theirs; publisher updates never replace it.
                if (HasSkate && (string)JObject.Parse(File.ReadAllText(Path.Combine(Root, "skate.json")))["source"] != "publisher") return;
                var bundled = Path.Combine(Path.GetDirectoryName(packageRoot), "SkateContent"); string json;
                bool remote = !string.IsNullOrEmpty(deployment.skateManifestUrl);
                if (remote) using (var response = await Get(client, Https(deployment.skateManifestUrl), token, 0, cancel).ConfigureAwait(false)) json = await Body(response.Content, 8 * 1024 * 1024, cancel).ConfigureAwait(false);
                else { var manifest = Path.Combine(bundled, "manifest.json"); if (!File.Exists(manifest)) return; json = File.ReadAllText(manifest); }
                var m = ValidateSkateManifest(json);
                if (!repair && HasSkate && (string)JObject.Parse(File.ReadAllText(Path.Combine(Root, "skate.json")))["version"] == m.version && m.files.All(f => Valid(Under(AssetDirectory("skate"), f.path), f))) return;
                var assets = Path.Combine(Root, "assets"); Directory.CreateDirectory(assets);
                var generation = "skate-" + Guid.NewGuid().ToString("N"); var stage = Path.Combine(assets, ".staging-" + generation); Directory.CreateDirectory(stage);
                var cache = Path.Combine(Root, "downloads"); Directory.CreateDirectory(cache);
                try
                {
                    int index = 0;
                    foreach (var f in m.files)
                    {
                        cancel.ThrowIfCancellationRequested(); var target = Under(stage, f.path); Directory.CreateDirectory(Path.GetDirectoryName(target));
                        if (remote)
                        {
                            if (!SameOrigin(Https(deployment.skateManifestUrl), Https(f.url))) throw new Exception("Skate content download is outside its configured host.");
                            var partial = Under(cache, f.sha256 + ".part"); await Download(client, f, partial, token, cancel).ConfigureAwait(false); File.Copy(partial, target);
                        }
                        else { var original = Under(bundled, f.path); if (!Valid(original, f)) throw new Exception("Bundled Skate content failed verification: " + f.path); File.Copy(original, target); }
                        report("Installing Skate content " + (++index) + "/" + m.files.Count, index * 100 / m.files.Count);
                    }
                    ValidateSkate(stage); AtomicJson(Path.Combine(stage, ".asset-integrity.json"), m.files); AtomicJson(Path.Combine(stage, "content-manifest.json"), m);
                    Directory.Move(stage, Path.Combine(assets, generation)); AtomicJson(Path.Combine(Root, "skate.json"), new { folder = generation, source = "publisher", version = m.version });
                    report("Skate content installed and verified.", 100);
                }
                finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
            }
        }
        public void ValidateClient()
        {
            var release = ReleaseDirectory;
            var m = ValidateManifest(File.ReadAllText(Path.Combine(release, "manifest.json")));
            foreach (var file in m.files) if (!Valid(Under(release, file.path), file)) throw new Exception("Client files are damaged. Choose Repair in Settings.");
            using (var input = new BinaryReader(File.OpenRead(Path.Combine(release, "bin", "benilla.exe"))))
            {
                if (input.ReadUInt16() != 0x5a4d) throw new Exception("Unsupported client: expected a Windows executable.");
                input.BaseStream.Position = 0x3c; int offset = input.ReadInt32(); input.BaseStream.Position = offset;
                if (input.ReadUInt32() != 0x4550 || input.ReadUInt16() != 0x8664) throw new Exception("Unsupported client: Windows x64 is required.");
            }
        }
        Dictionary<string, HawkFile> WowProfile()
        {
            return JsonConvert.DeserializeObject<Dictionary<string, HawkFile>>(File.ReadAllText(profilePath));
        }
        static IEnumerable<HawkFile> WowVersions(HawkFile file)
        {
            yield return file;
            foreach (var alternative in file.alternatives ?? new List<HawkFile>()) yield return alternative;
        }
        public void ValidateWow(string folder)
        {
            foreach (var entry in WowProfile())
            {
                var path = Under(folder, entry.Key);
                if (!WowVersions(entry.Value).Any(version => Valid(path, version))) throw new Exception("WoW assets are missing or not the verified English 1.12.1 build 5875: " + entry.Key + ". Import your client ZIP or Data folder.");
            }
        }
        public void ImportWow(string source, CancellationToken cancel)
        {
            using (Lock())
            {
                var assets = Path.Combine(Root, "assets"); Directory.CreateDirectory(assets);
                var generation = "wow-" + Guid.NewGuid().ToString("N"); var stage = Path.Combine(assets, ".staging-" + generation); Directory.CreateDirectory(stage);
                try
                {
                    var expected = WowProfile();
                    if (File.Exists(source))
                    {
                        using (var zip = ZipFile.OpenRead(source))
                        {
                            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var file in zip.Entries)
                            {
                                var normalized = file.FullName.Replace('\\', '/'); var parts = normalized.Split('/');
                                var name = parts.Last(); var key = expected.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
                                if (key == null || !parts.Take(parts.Length - 1).Any(p => p.Equals("Data", StringComparison.OrdinalIgnoreCase))) continue;
                                SafePath(normalized);
                                if (!seen.Add(key) || ((file.ExternalAttributes >> 16) & 0xf000) == 0xa000 || !WowVersions(expected[key]).Any(version => file.Length == version.size)) throw new Exception("Invalid, duplicate or linked MPQ in the WoW ZIP.");
                                cancel.ThrowIfCancellationRequested(); report("Importing " + key, seen.Count * 100 / expected.Count);
                                using (var input = file.Open()) using (var output = File.Create(Under(stage, key))) CopyLimited(input, output, file.Length, cancel);
                            }
                        }
                    }
                    else
                    {
                        if (Directory.Exists(Path.Combine(source, "Data"))) source = Path.Combine(source, "Data");
                        ValidateWow(source); int index = 0;
                        foreach (var entry in expected) { cancel.ThrowIfCancellationRequested(); report("Importing " + entry.Key, ++index * 100 / expected.Count); File.Copy(Under(source, entry.Key), Under(stage, entry.Key)); }
                    }
                    ValidateWow(stage); Directory.Move(stage, Path.Combine(assets, generation)); AtomicJson(Path.Combine(Root, "wow.json"), new { folder = generation });
                    report("WoW 1.12.1 assets imported. No executable, addons or DLLs were copied.", 100);
                }
                finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
            }
        }
        static void CopyLimited(Stream input, Stream output, long limit, CancellationToken cancel)
        {
            var buffer = new byte[1024 * 1024]; long size = 0; int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0) { cancel.ThrowIfCancellationRequested(); size += count; if (size > limit) throw new Exception("Asset exceeds the allowed size."); output.Write(buffer, 0, count); }
            if (size != limit) throw new Exception("Incomplete asset import.");
        }
        public void ValidateSkate(string folder)
        {
            // The client derives rig.json and board.json from the imported skater GLB on first activation.
            foreach (var path in new[] { "skate-data/assets/private/skater.glb", "skate-data/assets/private/game.json", "skate-data/assets/private/stock/physics-skeletons.json", "skate-data/assets/private/stock/skater-collections.json", "skate-audio/pop_1.wav" })
                if (!File.Exists(Under(folder, path))) throw new Exception("Missing Skate 3 content: " + path + ". Import your own game files.");
            var integrity = Path.Combine(folder, ".asset-integrity.json");
            if (File.Exists(integrity)) foreach (var f in JsonConvert.DeserializeObject<List<HawkFile>>(File.ReadAllText(integrity)))
                if (!Valid(Under(folder, f.path), f)) throw new Exception("Damaged Skate 3 content: " + f.path + ". Reimport your game files.");
        }
        public void ImportSkate(string source, CancellationToken cancel)
        {
            using (Lock())
            {
                var assets = Path.Combine(Root, "assets"); Directory.CreateDirectory(assets);
                var generation = "skate-" + Guid.NewGuid().ToString("N"); var stage = Path.Combine(assets, ".staging-" + generation); Directory.CreateDirectory(stage);
                try
                {
                    if (File.Exists(Path.Combine(source, "default.xex")) && Directory.Exists(Path.Combine(source, "data")))
                    {
                        var converter = Path.Combine(ReleaseDirectory, "tools", "skate-convert.exe");
                        if (!File.Exists(converter)) throw new Exception("The publisher's release is missing the standalone Skate 3 converter. Import your own previously converted skate-data and skate-audio folder, or ask for a complete client package.");
                        using (var process = Process.Start(new ProcessStartInfo(converter) { WorkingDirectory = Path.GetDirectoryName(converter), UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true }))
                        {
                            process.StandardInput.WriteLine(JsonConvert.SerializeObject(new { source, output = stage })); process.StandardInput.Close();
                            var stdout = Task.Run(async () => { string line; while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null) report(line, 0); });
                            var stderr = process.StandardError.ReadToEndAsync();
                            while (!process.WaitForExit(200)) if (cancel.IsCancellationRequested) { process.Kill(); cancel.ThrowIfCancellationRequested(); }
                            Task.WaitAll(stdout, stderr); if (process.ExitCode != 0) throw new Exception("Skate 3 conversion failed. Check your extracted game files.");
                        }
                    }
                    else
                    {
                        ValidateSkate(source); var files = new List<string>();
                        foreach (var directory in new[] { "skate-data", "skate-audio" }) Gather(Under(source, directory), files);
                        int index = 0; long total = 0;
                        foreach (var file in files)
                        {
                            cancel.ThrowIfCancellationRequested(); total += new FileInfo(file).Length;
                            if (total > 24L * 1024 * 1024 * 1024) throw new Exception("Skate content exceeds the import limit.");
                            var relative = RelativeAsset(source, file);
                            var dest = Under(stage, relative); Directory.CreateDirectory(Path.GetDirectoryName(dest)); File.Copy(file, dest); report("Importing your converted Skate 3 content", ++index * 100 / files.Count);
                        }
                    }
                    ValidateSkate(stage);
                    var imported = new List<string>(); Gather(stage, imported);
                    AtomicJson(Path.Combine(stage, ".asset-integrity.json"), imported.Select(f => new HawkFile { path = RelativeAsset(stage, f), size = new FileInfo(f).Length, sha256 = Hash(f) }).ToList());
                    Directory.Move(stage, Path.Combine(assets, generation)); AtomicJson(Path.Combine(Root, "skate.json"), new { folder = generation }); report("Your Skate 3 content is ready.", 100);
                }
                finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
            }
        }
        static void Gather(string directory, List<string> files)
        {
            directory = ExtendedPath(directory);
            if (UnsafeLink(directory)) throw new Exception("Linked asset folders are not supported.");
            foreach (var file in Directory.GetFiles(directory)) { if (UnsafeLink(file)) throw new Exception("Linked assets are not supported."); files.Add(file); }
            foreach (var sub in Directory.GetDirectories(directory)) Gather(sub, files);
        }
        static string ExtendedPath(string path)
        {
            path = Path.GetFullPath(path);
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path;
            return path.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + path.Substring(2) : @"\\?\" + path;
        }
        static string RelativeAsset(string root, string file)
        {
            var prefix = ExtendedPath(root).TrimEnd('\\') + @"\";
            file = ExtendedPath(file);
            if (!file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new Exception("Asset path escaped its source.");
            return file.Substring(prefix.Length).Replace('\\', '/');
        }
        public void CheckAssets() { ValidateWow(AssetDirectory("wow")); ValidateSkate(AssetDirectory("skate")); }
        async Task<JObject> Post(string url, object body, string token, CancellationToken cancel)
        {
            using (var client = Http()) using (var request = new HttpRequestMessage(HttpMethod.Post, Https(url)))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var response = await client.SendAsync(request, cancel).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.Conflict && url.EndsWith("/v1/heartbeat", StringComparison.Ordinal)) throw new HawkSessionExpiredException();
                    if (response.StatusCode == HttpStatusCode.Conflict) throw new Exception("Server offline. Start JasonHawkSkater through the Discord helper (/wake skatecraft), then click Play.");
                    if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized) throw new HawkAccessDeniedException();
                    if (!response.IsSuccessStatusCode) throw new Exception("Service unavailable. Retry shortly; the launcher cannot start the server.");
                    return JObject.Parse(await Body(response.Content, 1024 * 1024, cancel).ConfigureAwait(false));
                }
            }
        }
        public async Task<string> SignIn(CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(deployment.authUrl)) throw new Exception("The publisher has not configured external-user sign-in yet.");
            var baseUrl = deployment.authUrl.TrimEnd('/'); var response = await Post(baseUrl + "/v1/auth/device", new { }, null, cancel).ConfigureAwait(false);
            var uri = new Uri((string)response["verification_uri_complete"]);
            if (!SameOrigin(Https(baseUrl), uri) || uri.Scheme != "https") throw new Exception("Sign-in URL is outside the configured publisher host.");
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            report("Complete Discord sign-in in your browser.", 0);
            var deadline = DateTime.UtcNow.AddMinutes(5);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(2000, cancel).ConfigureAwait(false);
                var result = await Post(baseUrl + "/v1/auth/token", new { device_code = (string)response["device_code"] }, null, cancel).ConfigureAwait(false);
                if ((string)result["error"] == "authorization_pending") continue;
                var token = (string)result["access_token"];
                if (string.IsNullOrEmpty(token)) throw new Exception("Sign-in was not approved. Contact the Discord helper administrator.");
                return token;
            }
            throw new Exception("Sign-in timed out. Click Install / Play to retry.");
        }
        async Task<JObject> Attach(string session, string token, CancellationToken cancel)
        {
            return await Post(deployment.lifecycleUrl.TrimEnd('/') + "/v1/attach", new { session_id = session }, token, cancel).ConfigureAwait(false);
        }
        static async Task<bool> Reachable(string address)
        {
            int colon = address.LastIndexOf(':'); int port;
            if (colon < 1 || !int.TryParse(address.Substring(colon + 1), out port) || port < 1 || port > 65535) throw new Exception("Invalid game connection settings from the publisher.");
            using (var socket = new System.Net.Sockets.TcpClient())
            { var connect = socket.ConnectAsync(address.Substring(0, colon), port); if (await Task.WhenAny(connect, Task.Delay(3000)).ConfigureAwait(false) != connect) return false; try { await connect.ConfigureAwait(false); return true; } catch (System.Net.Sockets.SocketException) { return false; } }
        }
        public async Task Launch(string token, string user, string password, CancellationToken cancel)
        {
            // Anonymous ownership secret is per launch, never a repository credential.
            if (deployment.publicSessions) token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            if (!Environment.Is64BitOperatingSystem) throw new Exception("Unsupported platform: Windows x64 is required.");
            using (Lock())
            {
                ValidateClient(); CheckAssets(); string session = Guid.NewGuid().ToString(); Process process = null; bool attached = false, started = false;
                try
                {
                    string auth = deployment.authAddress, world = deployment.worldAddress;
                    if (!string.IsNullOrEmpty(deployment.lifecycleUrl))
                    {
                        var state = await Attach(session, token, cancel).ConfigureAwait(false); attached = true;
                        var deadline = DateTime.UtcNow.AddMinutes(10);
                        while (!(bool)(state["ready"] ?? false))
                        {
                            if (DateTime.UtcNow >= deadline) throw new Exception("The Discord-started server did not become ready. Ask the helper administrator.");
                            report("Waiting for the Discord-started server: " + (string)state["phase"], 0);
                            await Task.Delay(3000, cancel).ConfigureAwait(false);
                            try { state = await Post(deployment.lifecycleUrl.TrimEnd('/') + "/v1/heartbeat", new { session_id = session }, token, cancel).ConfigureAwait(false); }
                            catch (HawkSessionExpiredException) { state = await Attach(session, token, cancel).ConfigureAwait(false); }
                        }
                        auth = (string)state["auth_address"]; world = (string)state["world_address"];
                    }
                    if (!await Reachable(auth).ConfigureAwait(false) || !await Reachable(world).ConfigureAwait(false)) throw new Exception("Server offline. Start JasonHawkSkater through the Discord helper (/wake skatecraft), then click Play.");
                    var release = ReleaseDirectory; var start = new ProcessStartInfo(Path.Combine(release, "bin", "benilla.exe")) { WorkingDirectory = release, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                    foreach (var key in new[] { "VK_DRIVER_FILES", "WGPU_ALLOW_UNDERLYING_NONCOMPLIANT_ADAPTER", "LD_LIBRARY_PATH", "ALSA_CONFIG_PATH", "WINIT_UNIX_BACKEND" }) start.EnvironmentVariables.Remove(key);
                    start.EnvironmentVariables["WOW_DATA"] = AssetDirectory("wow"); start.EnvironmentVariables["WOW_HOST"] = auth;
                    start.EnvironmentVariables["WOW_SKATE_ASSETS"] = Path.Combine(AssetDirectory("skate"), "skate-data", "assets"); start.EnvironmentVariables["WOW_SKATE_AUDIO"] = Path.Combine(AssetDirectory("skate"), "skate-audio"); start.EnvironmentVariables["BENILLA_HOME"] = Path.Combine(Root, "benilla-config");
                    start.EnvironmentVariables.Remove("WOW_USER"); start.EnvironmentVariables.Remove("WOW_PASS");
                    if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password)) { start.EnvironmentVariables["WOW_USER"] = user; start.EnvironmentVariables["WOW_PASS"] = password; }
                    report("Starting JasonHawkSkater. J: board, K: camera; keyboard and XInput supported.", 100);
                    using (var log = new StreamWriter(Path.Combine(Root, "client.log"), false))
                    {
                        process = new Process { StartInfo = start }; var logLock = new object();
                        process.OutputDataReceived += (s, e) => { if (e.Data != null) lock (logLock) { log.WriteLine(e.Data); log.Flush(); } };
                        process.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (logLock) { log.WriteLine(e.Data); log.Flush(); } };
                        process.Start(); started = true; IsRunning = true; process.BeginOutputReadLine(); process.BeginErrorReadLine(); var last = DateTime.UtcNow;
                        AtomicJson(Path.Combine(Root, ".session.json"), new { session_id = session, heartbeat = last });
                        try
                        {
                        while (!process.HasExited)
                        {
                            await Task.Delay(500, cancel).ConfigureAwait(false);
                            if ((DateTime.UtcNow - last).TotalSeconds >= 20)
                            {
                                if (attached)
                                {
                                    try
                                    {
                                        try { await Post(deployment.lifecycleUrl.TrimEnd('/') + "/v1/heartbeat", new { session_id = session }, token, cancel).ConfigureAwait(false); }
                                        catch (HawkSessionExpiredException) { await Attach(session, token, cancel).ConfigureAwait(false); }
                                    }
                                    catch (HawkAccessDeniedException) { throw; }
                                    catch (Exception) { report("Session heartbeat unavailable; retrying while connected.", 100); }
                                }
                                AtomicJson(Path.Combine(Root, ".session.json"), new { session_id = session, heartbeat = DateTime.UtcNow }); last = DateTime.UtcNow;
                            }
                        }
                        }
                        finally
                        {
                            // Drain output while its log is still open, even after cancellation.
                            if (!process.HasExited) { process.CloseMainWindow(); if (!process.WaitForExit(5000)) process.Kill(); }
                            process.WaitForExit();
                        }
                        process.WaitForExit();
                        if (process.ExitCode != 0) throw new Exception("The client exited with an error. Check client.log; update your graphics driver and verify assets using Repair.");
                    }
                    report("Game closed. Session released.", 100);
                }
                finally
                {
                    if (process != null) { if (started && !process.HasExited) { process.CloseMainWindow(); if (!process.WaitForExit(5000)) process.Kill(); } process.Dispose(); } IsRunning = false;
                    if (attached) try { using (var releaseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10))) await Post(deployment.lifecycleUrl.TrimEnd('/') + "/v1/release", new { session_id = session }, token, releaseTimeout.Token).ConfigureAwait(false); } catch (Exception) { report("Session release unavailable; the server will expire its lease.", 100); }
                    var lease = Path.Combine(Root, ".session.json"); if (File.Exists(lease)) File.Delete(lease);
                }
            }
        }
    }
    public sealed class HawkAccessDeniedException : Exception { public HawkAccessDeniedException() : base("Access denied or revoked. Sign in again through Discord.") { } }
    public sealed class HawkSessionExpiredException : Exception { public HawkSessionExpiredException() : base("Session expired; reattach to the Discord-started server.") { } }
}
