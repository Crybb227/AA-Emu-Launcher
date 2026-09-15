using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AAEmu.Launcher
{
    public class GitHubReleaseAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = new List<GitHubReleaseAsset>();
    }

    public class UpdateManifestEntry
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class UpdateManifest
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("files")]
        public List<UpdateManifestEntry> Files { get; set; } = new List<UpdateManifestEntry>();
    }

    public class LauncherUpdateInfo
    {
        public string Version;
        public string ReleaseUrl;
        public UpdateManifest Manifest;
        public UpdateManifest PreviousManifest;
        public Dictionary<string, string> AssetUrlsByName;
    }

    /// <summary>
    /// Checks a GitHub repository's releases for a newer launcher version and applies
    /// delta updates by only downloading files whose hash changed since the current install.
    /// </summary>
    public static class GitHubReleaseUpdater
    {
        public const string ManifestAssetName = "manifest.json";
        private static readonly string[] UserOwnedRelativeFiles =
        {
            "settings.aelcf",
            "clientslist.json"
        };

        public static bool IsUserOwnedFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var normalizedPath = relativePath.Replace('\\', '/');
            return UserOwnedRelativeFiles.Any(p => string.Equals(p, normalizedPath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Converts a manifest-relative file path into the flattened name used for release assets.</summary>
        public static string FlattenAssetName(string relativePath)
        {
            return relativePath.Replace('\\', '/').Replace('/', '_');
        }

        public static async Task<LauncherUpdateInfo> CheckForUpdateAsync(string ownerRepo, string currentVersion)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AAEmu.Launcher");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                var releaseJson = await client.GetStringAsync($"https://api.github.com/repos/{ownerRepo}/releases/latest");
                var release = JsonConvert.DeserializeObject<GitHubRelease>(releaseJson);
                if (release == null || string.IsNullOrEmpty(release.TagName))
                    return null;

                var latestVersionString = release.TagName.TrimStart('v', 'V');
                if (!Version.TryParse(latestVersionString, out var latestVersion))
                    return null;
                if (!Version.TryParse(currentVersion, out var installedVersion))
                    installedVersion = new Version(0, 0, 0, 0);

                if (latestVersion <= installedVersion)
                    return null; // Already up to date

                var manifestAsset = release.Assets?.FirstOrDefault(a => a.Name == ManifestAssetName);
                if (manifestAsset == null)
                    return null; // Release wasn't published with a delta manifest

                var manifestJson = await client.GetStringAsync(manifestAsset.BrowserDownloadUrl);
                var manifest = JsonConvert.DeserializeObject<UpdateManifest>(manifestJson);

                // Best-effort: used to detect files that were removed since the currently installed version
                var previousManifest = await TryGetManifestForVersionAsync(client, ownerRepo, currentVersion);

                return new LauncherUpdateInfo
                {
                    Version = latestVersionString,
                    ReleaseUrl = release.HtmlUrl,
                    Manifest = manifest,
                    PreviousManifest = previousManifest,
                    AssetUrlsByName = release.Assets.ToDictionary(a => a.Name, a => a.BrowserDownloadUrl)
                };
            }
        }

        private static async Task<UpdateManifest> TryGetManifestForVersionAsync(HttpClient client, string ownerRepo, string version)
        {
            try
            {
                var releaseJson = await client.GetStringAsync($"https://api.github.com/repos/{ownerRepo}/releases/tags/v{version}");
                var release = JsonConvert.DeserializeObject<GitHubRelease>(releaseJson);
                var manifestAsset = release?.Assets?.FirstOrDefault(a => a.Name == ManifestAssetName);
                if (manifestAsset == null)
                    return null;

                var manifestJson = await client.GetStringAsync(manifestAsset.BrowserDownloadUrl);
                return JsonConvert.DeserializeObject<UpdateManifest>(manifestJson);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Returns files present in the previous release's manifest but no longer part of the new one.</summary>
        public static List<string> GetRemovedFiles(UpdateManifest previousManifest, UpdateManifest newManifest)
        {
            if (previousManifest?.Files == null)
                return new List<string>();

            var newPaths = new HashSet<string>(newManifest.Files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);
            return previousManifest.Files
                .Select(f => f.Path)
                .Where(p => !IsUserOwnedFile(p) && !newPaths.Contains(p))
                .ToList();
        }

        /// <summary>Returns the subset of manifest files that are missing locally or whose hash differs.</summary>
        public static List<UpdateManifestEntry> GetChangedFiles(string appDirectory, UpdateManifest manifest)
        {
            var changed = new List<UpdateManifestEntry>();
            foreach (var entry in manifest.Files)
            {
                if (IsUserOwnedFile(entry.Path))
                    continue;

                var localPath = Path.Combine(appDirectory, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(localPath))
                {
                    changed.Add(entry);
                    continue;
                }

                if (!string.Equals(ComputeSha256(localPath), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    changed.Add(entry);
            }
            return changed;
        }

        public static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static async Task DownloadChangedFilesAsync(LauncherUpdateInfo updateInfo, List<UpdateManifestEntry> changedFiles, string stagingDirectory, IProgress<(int current, int total, string fileName)> progress, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(stagingDirectory);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AAEmu.Launcher");

                for (var i = 0; i < changedFiles.Count; i++)
                {
                    var entry = changedFiles[i];
                    progress?.Report((i + 1, changedFiles.Count, entry.Path));

                    var assetName = FlattenAssetName(entry.Path);
                    if (!updateInfo.AssetUrlsByName.TryGetValue(assetName, out var url))
                        throw new InvalidOperationException($"Release is missing an asset for file: {entry.Path}");

                    var destPath = Path.Combine(stagingDirectory, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                        Directory.CreateDirectory(destDir);

                    using (var responseStream = await client.GetStreamAsync(url))
                    using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await responseStream.CopyToAsync(fileStream, 81920, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Writes and launches a helper PowerShell script that waits for the launcher process to exit,
        /// copies the staged files over the current install, deletes files removed in the new version,
        /// then restarts the launcher. Call this right before exiting the application.
        /// </summary>
        public static void ApplyUpdateAndRestart(string appDirectory, string stagingDirectory, string mainExeName, List<string> removedRelativeFiles = null)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "AAEmuLauncherUpdate_" + Guid.NewGuid().ToString("N") + ".ps1");
            var currentPid = Process.GetCurrentProcess().Id;
            var mainExePath = Path.Combine(appDirectory, mainExeName);

            var sb = new StringBuilder();
            sb.Append("$ErrorActionPreference = 'SilentlyContinue'\r\n");
            sb.Append($"try {{ Wait-Process -Id {currentPid} -Timeout 30 }} catch {{}}\r\n");
            sb.Append("Start-Sleep -Milliseconds 500\r\n");
            sb.Append($"Copy-Item -Path '{stagingDirectory}\\*' -Destination '{appDirectory}' -Recurse -Force\r\n");
            sb.Append($"Remove-Item -Path '{stagingDirectory}' -Recurse -Force\r\n");

            if (removedRelativeFiles != null)
            {
                foreach (var relativePath in removedRelativeFiles)
                {
                    if (IsUserOwnedFile(relativePath))
                        continue;

                    var fullPath = Path.Combine(appDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    sb.Append($"Remove-Item -Path '{fullPath}' -Force\r\n");
                }
            }

            sb.Append($"Start-Process -FilePath '{mainExePath}'\r\n");
            sb.Append($"Remove-Item -Path '{scriptPath}' -Force\r\n");

            File.WriteAllText(scriptPath, sb.ToString());

            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi);
        }
    }
}
