using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AAEmu.Launcher
{
    /// <summary>An entry in the curated catalog of popular WotLK 3.3.5 addons, fetched live from GitHub.</summary>
    public class CuratedAddon
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("repo")]
        public string Repo { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class CuratedAddonCatalog
    {
        [JsonProperty("updated")]
        public string Updated { get; set; }

        [JsonProperty("addons")]
        public List<CuratedAddon> Addons { get; set; } = new List<CuratedAddon>();
    }

    /// <summary>A WoW addon installed via the addon manager, tracked so it can be updated or removed later.</summary>
    public class InstalledAddon
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("repo")]
        public string Repo { get; set; } // "owner/name"

        [JsonProperty("version")]
        public string Version { get; set; } // release tag, or commit sha for branch installs

        [JsonProperty("folders")]
        public List<string> Folders { get; set; } = new List<string>(); // top-level AddOns subfolders this addon owns

        [JsonProperty("installedAt")]
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    }

    public class AddonManifest
    {
        [JsonProperty("addons")]
        public List<InstalledAddon> Addons { get; set; } = new List<InstalledAddon>();
    }

    public class AddonUpdateCheckResult
    {
        public InstalledAddon Addon;
        public bool UpdateAvailable;
        public string LatestVersion;
        public string Error;
    }

    /// <summary>
    /// Installs and updates WoW addons from GitHub repositories: resolves a repo URL to its
    /// latest release (or default branch as a fallback), downloads the zip, and extracts the
    /// AddOns folder(s) it contains, tracking what was installed in a local manifest.
    /// </summary>
    public static class AddonManager
    {
        private const string ManifestFileName = "wow-addons.json";
        private const string CuratedCatalogUrl = "https://raw.githubusercontent.com/Crybb227/AA-Emu-Launcher/master/addons/curated-addons.json";
        private static readonly Regex RepoUrlPattern = new Regex(
            @"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/#?]+?)(\.git)?/?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryParseRepo(string input, out string ownerRepo)
        {
            ownerRepo = null;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var trimmed = input.Trim();
            var match = RepoUrlPattern.Match(trimmed);
            if (match.Success)
            {
                ownerRepo = $"{match.Groups["owner"].Value}/{match.Groups["repo"].Value}";
                return true;
            }

            // Also accept a bare "owner/repo" shorthand
            if (Regex.IsMatch(trimmed, @"^[\w.-]+/[\w.-]+$"))
            {
                ownerRepo = trimmed;
                return true;
            }

            return false;
        }

        /// <summary>Fetches the curated list of popular WotLK 3.3.5 addons, hosted alongside the launcher's source so it can be updated without a new release.</summary>
        public static async Task<CuratedAddonCatalog> FetchCuratedCatalogAsync(HttpClient client)
        {
            var json = await client.GetStringAsync(CuratedCatalogUrl);
            return JsonConvert.DeserializeObject<CuratedAddonCatalog>(json) ?? new CuratedAddonCatalog();
        }

        public static string GetManifestPath(string addOnsPath)
        {
            return Path.Combine(addOnsPath, ManifestFileName);
        }

        public static AddonManifest LoadManifest(string addOnsPath)
        {
            var path = GetManifestPath(addOnsPath);
            if (!File.Exists(path))
                return new AddonManifest();

            try
            {
                return JsonConvert.DeserializeObject<AddonManifest>(File.ReadAllText(path)) ?? new AddonManifest();
            }
            catch
            {
                return new AddonManifest();
            }
        }

        public static void SaveManifest(string addOnsPath, AddonManifest manifest)
        {
            Directory.CreateDirectory(addOnsPath);
            File.WriteAllText(GetManifestPath(addOnsPath), JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        /// <summary>Resolves a GitHub repo to a downloadable zip: the latest release asset/zipball if one exists, otherwise the default branch.</summary>
        public static async Task<(string downloadUrl, string version)> ResolveDownloadAsync(HttpClient client, string ownerRepo)
        {
            try
            {
                var releaseJson = await client.GetStringAsync($"https://api.github.com/repos/{ownerRepo}/releases/latest");
                var release = JsonConvert.DeserializeObject<GitHubRelease>(releaseJson);
                if (release != null)
                {
                    var zipAsset = release.Assets?.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                    var downloadUrl = zipAsset?.BrowserDownloadUrl ?? $"https://github.com/{ownerRepo}/archive/refs/tags/{release.TagName}.zip";
                    var version = !string.IsNullOrWhiteSpace(release.TagName) ? release.TagName : "latest";
                    return (downloadUrl, version);
                }
            }
            catch (HttpRequestException)
            {
                // No releases published for this repo - fall back to the default branch below.
            }

            var repoJson = await client.GetStringAsync($"https://api.github.com/repos/{ownerRepo}");
            var repoInfo = JsonConvert.DeserializeObject<GitHubRepoInfo>(repoJson);
            var branch = string.IsNullOrWhiteSpace(repoInfo?.DefaultBranch) ? "main" : repoInfo.DefaultBranch;
            return ($"https://github.com/{ownerRepo}/archive/refs/heads/{branch}.zip", branch);
        }

        public static async Task<string> DownloadZipAsync(HttpClient client, string downloadUrl, string destinationFolder, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(destinationFolder);
            var zipPath = Path.Combine(destinationFolder, "addon_" + Guid.NewGuid().ToString("N") + ".zip");

            progress?.Report("Downloading...");
            using (var responseStream = await client.GetStreamAsync(downloadUrl))
            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await responseStream.CopyToAsync(fileStream, 81920, cancellationToken);
            }

            return zipPath;
        }

        /// <summary>
        /// Extracts the addon zip into the AddOns folder. GitHub source zips wrap everything in a single
        /// "<repo>-<branch>" root folder; if that's all the archive contains, its contents are hoisted up
        /// so the actual Toc-named folder(s) end up directly under AddOns, matching what WoW expects.
        /// </summary>
        public static List<string> ExtractAddon(string zipPath, string addOnsPath)
        {
            var tempExtractPath = Path.Combine(Path.GetTempPath(), "aaemu_addon_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempExtractPath);
            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempExtractPath);

                var extractedRoots = Directory.GetDirectories(tempExtractPath);
                var sourceRoot = tempExtractPath;
                if (extractedRoots.Length == 1 && Directory.GetFiles(tempExtractPath).Length == 0)
                    sourceRoot = extractedRoots[0];

                var installedFolders = new List<string>();
                foreach (var folder in Directory.GetDirectories(sourceRoot))
                {
                    var folderName = Path.GetFileName(folder);
                    var destination = Path.Combine(addOnsPath, folderName);
                    if (Directory.Exists(destination))
                        Directory.Delete(destination, true);

                    CopyDirectory(folder, destination);
                    installedFolders.Add(folderName);
                }

                if (installedFolders.Count == 0)
                    throw new InvalidOperationException("The downloaded archive did not contain any addon folders.");

                return installedFolders;
            }
            finally
            {
                try { Directory.Delete(tempExtractPath, true); } catch { /* best effort */ }
                try { File.Delete(zipPath); } catch { /* best effort */ }
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            foreach (var filePath in Directory.GetFiles(sourceDir))
                File.Copy(filePath, Path.Combine(destinationDir, Path.GetFileName(filePath)), true);

            foreach (var subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destinationDir, Path.GetFileName(subDir)));
        }

        public static void RemoveAddon(string addOnsPath, InstalledAddon addon)
        {
            foreach (var folder in addon.Folders)
            {
                var fullPath = Path.Combine(addOnsPath, folder);
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
            }
        }

        public static async Task<AddonUpdateCheckResult> CheckForUpdateAsync(HttpClient client, InstalledAddon addon)
        {
            try
            {
                var (_, latestVersion) = await ResolveDownloadAsync(client, addon.Repo);
                return new AddonUpdateCheckResult
                {
                    Addon = addon,
                    LatestVersion = latestVersion,
                    UpdateAvailable = !string.Equals(latestVersion, addon.Version, StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                return new AddonUpdateCheckResult { Addon = addon, Error = ex.Message };
            }
        }

        private class GitHubRepoInfo
        {
            [JsonProperty("default_branch")]
            public string DefaultBranch { get; set; }
        }
    }
}
