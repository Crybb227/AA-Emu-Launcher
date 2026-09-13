using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AAEmu.Launcher
{
    public class ClientUpdateManifestEntry
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }
    }

    public class ClientUpdateManifest
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("files")]
        public List<ClientUpdateManifestEntry> Files { get; set; } = new List<ClientUpdateManifestEntry>();
    }

    public class ClientDeltaUpdatePlan
    {
        public ClientUpdateManifest Manifest { get; set; }
        public List<ClientUpdateManifestEntry> ChangedFiles { get; set; } = new List<ClientUpdateManifestEntry>();
        public List<string> RemovedFiles { get; set; } = new List<string>();
        public long DownloadSize => ChangedFiles.Sum(f => f.Size);
    }

    public class ClientDeltaProgress
    {
        public int CurrentFile { get; set; }
        public int TotalFiles { get; set; }
        public string FileName { get; set; }
        public long BytesDownloaded { get; set; }
        public long BytesTotal { get; set; }
    }

    /// <summary>
    /// Static-file client updater. Host a manifest at {updateUrl}/client/manifest.json and
    /// changed files at {updateUrl}/client/files/{relative_path_with_slashes_replaced_by_underscores}.
    /// </summary>
    public static class ClientDeltaUpdater
    {
        public const string ClientManifestRelativePath = "client/manifest.json";
        public const string LocalManifestFileName = ".aaemu-client-manifest.json";

        public static string FlattenAssetName(string relativePath)
        {
            return relativePath.Replace('\\', '/').Replace('/', '_');
        }

        public static async Task<ClientDeltaUpdatePlan> CheckForUpdatesAsync(string updateBaseUrl, string gameExePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(updateBaseUrl))
                throw new InvalidOperationException("No client update URL is configured.");
            if (string.IsNullOrWhiteSpace(gameExePath) || !File.Exists(gameExePath))
                throw new InvalidOperationException("Install the game client before checking for client updates.");

            return await CheckForUpdatesInFolderAsync(updateBaseUrl, GetGameRoot(gameExePath), cancellationToken);
        }

        public static async Task<ClientDeltaUpdatePlan> CheckForUpdatesInFolderAsync(string updateBaseUrl, string gameRoot, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(updateBaseUrl))
                throw new InvalidOperationException("No client update URL is configured.");
            if (string.IsNullOrWhiteSpace(gameRoot))
                throw new InvalidOperationException("Choose an install folder before checking for client updates.");

            Directory.CreateDirectory(gameRoot);
            var remoteManifest = await DownloadManifestAsync(updateBaseUrl, cancellationToken);
            var localManifest = TryReadLocalManifest(gameRoot);

            var changedFiles = GetChangedFiles(gameRoot, remoteManifest);
            var removedFiles = GetRemovedFiles(localManifest, remoteManifest);

            return new ClientDeltaUpdatePlan
            {
                Manifest = remoteManifest,
                ChangedFiles = changedFiles,
                RemovedFiles = removedFiles,
            };
        }

        public static async Task ApplyUpdateAsync(string updateBaseUrl, string gameExePath, ClientDeltaUpdatePlan plan, IProgress<ClientDeltaProgress> progress = null, CancellationToken cancellationToken = default)
        {
            await ApplyUpdateToFolderAsync(updateBaseUrl, GetGameRoot(gameExePath), plan, progress, cancellationToken);
        }

        public static async Task ApplyUpdateToFolderAsync(string updateBaseUrl, string gameRoot, ClientDeltaUpdatePlan plan, IProgress<ClientDeltaProgress> progress = null, CancellationToken cancellationToken = default)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            Directory.CreateDirectory(gameRoot);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AAEmu.Launcher");

                for (var i = 0; i < plan.ChangedFiles.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entry = plan.ChangedFiles[i];
                    var destination = Path.Combine(gameRoot, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                    var destinationFolder = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destinationFolder))
                        Directory.CreateDirectory(destinationFolder);

                    var downloadUrl = GetEntryDownloadUrl(updateBaseUrl, entry);
                    using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();
                        var total = response.Content.Headers.ContentLength ?? entry.Size;
                        using (var input = await response.Content.ReadAsStreamAsync())
                        using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[81920];
                            long downloaded = 0;
                            int read;
                            while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                            {
                                await output.WriteAsync(buffer, 0, read, cancellationToken);
                                downloaded += read;
                                progress?.Report(new ClientDeltaProgress
                                {
                                    CurrentFile = i + 1,
                                    TotalFiles = plan.ChangedFiles.Count,
                                    FileName = entry.Path,
                                    BytesDownloaded = downloaded,
                                    BytesTotal = total
                                });
                            }
                        }
                    }

                    var actualHash = ComputeSha256(destination);
                    if (!string.Equals(actualHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Downloaded file hash mismatch: {entry.Path}");
                }
            }

            foreach (var relativePath in plan.RemovedFiles)
            {
                var fullPath = Path.Combine(gameRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                try
                {
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
                catch
                {
                    // Best effort; locked files will be overwritten or removed on a later run.
                }
            }

            WriteLocalManifest(gameRoot, plan.Manifest);
        }

        public static string GetGameRoot(string gameExePath)
        {
            var binFolder = Path.GetDirectoryName(gameExePath);
            var root = Path.GetDirectoryName(binFolder);
            return string.IsNullOrEmpty(root) ? binFolder : root;
        }

        private static async Task<ClientUpdateManifest> DownloadManifestAsync(string updateBaseUrl, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AAEmu.Launcher");
                var manifestUrl = CombineUrl(updateBaseUrl, ClientManifestRelativePath);
                using (var response = await client.GetAsync(manifestUrl, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var manifest = JsonConvert.DeserializeObject<ClientUpdateManifest>(json);
                    if (manifest?.Files == null)
                        throw new InvalidOperationException("Client update manifest is invalid.");
                    return manifest;
                }
            }
        }

        private static List<ClientUpdateManifestEntry> GetChangedFiles(string gameRoot, ClientUpdateManifest manifest)
        {
            var changed = new List<ClientUpdateManifestEntry>();
            foreach (var entry in manifest.Files)
            {
                var localPath = Path.Combine(gameRoot, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(localPath) || !string.Equals(ComputeSha256(localPath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    changed.Add(entry);
            }
            return changed;
        }

        private static List<string> GetRemovedFiles(ClientUpdateManifest localManifest, ClientUpdateManifest remoteManifest)
        {
            if (localManifest?.Files == null)
                return new List<string>();

            var remotePaths = new HashSet<string>(remoteManifest.Files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);
            return localManifest.Files
                .Select(f => f.Path)
                .Where(path => !remotePaths.Contains(path))
                .ToList();
        }

        private static ClientUpdateManifest TryReadLocalManifest(string gameRoot)
        {
            try
            {
                var path = Path.Combine(gameRoot, LocalManifestFileName);
                if (!File.Exists(path))
                    return null;
                return JsonConvert.DeserializeObject<ClientUpdateManifest>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        private static void WriteLocalManifest(string gameRoot, ClientUpdateManifest manifest)
        {
            var path = Path.Combine(gameRoot, LocalManifestFileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        private static string GetEntryDownloadUrl(string updateBaseUrl, ClientUpdateManifestEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.Url))
                return entry.Url;

            return CombineUrl(updateBaseUrl, "client/files/" + Uri.EscapeDataString(FlattenAssetName(entry.Path)));
        }

        private static string CombineUrl(string baseUrl, string relativePath)
        {
            return baseUrl.TrimEnd('/') + "/" + relativePath.TrimStart('/');
        }

        private static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
