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

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }
    }

    public class CuratedAddonCatalog
    {
        [JsonProperty("updated")]
        public string Updated { get; set; }

        [JsonProperty("addons")]
        public List<CuratedAddon> Addons { get; set; } = new List<CuratedAddon>();
    }

    /// <summary>A suggested companion addon shown when installing something that recommends it (e.g. NemesisTracker's OptionalDeps).</summary>
    public class RecommendedDependency
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; } // direct .zip URL, not a GitHub repo

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>An entry in the JWoW Exclusives catalog: custom addons built for this server, all hosted in one repo.</summary>
    public class ExclusiveAddon
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("repo")]
        public string Repo { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; } // subfolder within the repo holding this addon's .toc

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("recommends")]
        public List<RecommendedDependency> Recommends { get; set; } = new List<RecommendedDependency>();
    }

    public class ExclusiveAddonCatalog
    {
        [JsonProperty("updated")]
        public string Updated { get; set; }

        [JsonProperty("addons")]
        public List<ExclusiveAddon> Addons { get; set; } = new List<ExclusiveAddon>();
    }

    /// <summary>A WoW addon installed via the addon manager, tracked so it can be updated or removed later.</summary>
    public class InstalledAddon
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("repo")]
        public string Repo { get; set; } // "owner/name"

        [JsonProperty("version")]
        public string Version { get; set; } // release tag, commit sha, or .toc version for exclusives

        [JsonProperty("folders")]
        public List<string> Folders { get; set; } = new List<string>(); // top-level AddOns subfolders this addon owns

        [JsonProperty("installedAt")]
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("isExclusive", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsExclusive { get; set; }

        [JsonProperty("exclusiveFolder", NullValueHandling = NullValueHandling.Ignore)]
        public string ExclusiveFolder { get; set; } // subfolder within the exclusives repo, for re-checking .toc version

        [JsonProperty("foundOnDisk", NullValueHandling = NullValueHandling.Ignore)]
        public bool FoundOnDisk { get; set; } // true for addons discovered by scanning the AddOns folder rather than installed here
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
        private const string ExclusiveCatalogUrl = "https://raw.githubusercontent.com/Crybb227/JWoW-Exclusive-Addons/main/exclusive-addons.json";
        private static readonly Regex TocVersionPattern = new Regex(@"^##\s*Version\s*:\s*(?<version>.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
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

        /// <summary>Fetches the JWoW Exclusives catalog: custom addons for this server, hosted together in one repo.</summary>
        public static async Task<ExclusiveAddonCatalog> FetchExclusiveCatalogAsync(HttpClient client)
        {
            var json = await client.GetStringAsync(ExclusiveCatalogUrl);
            return JsonConvert.DeserializeObject<ExclusiveAddonCatalog>(json) ?? new ExclusiveAddonCatalog();
        }

        /// <summary>Reads the "## Version:" line from an exclusive addon's .toc file at the head of its repo, without downloading the whole zip.</summary>
        public static async Task<string> GetExclusiveAddonVersionAsync(HttpClient client, ExclusiveAddon addon)
        {
            var tocUrl = $"https://raw.githubusercontent.com/{addon.Repo}/main/{addon.Folder}/{addon.Folder}.toc";
            var tocContent = await client.GetStringAsync(tocUrl);
            var match = TocVersionPattern.Match(tocContent);
            return match.Success ? match.Groups["version"].Value : "unknown";
        }

        /// <summary>Downloads and installs a single addon folder out of the (multi-addon) exclusives repo.</summary>
        public static async Task<(List<string> folders, string version)> InstallExclusiveAddonAsync(HttpClient client, ExclusiveAddon addon, string addOnsPath, CancellationToken cancellationToken)
        {
            var version = await GetExclusiveAddonVersionAsync(client, addon);
            var downloadUrl = $"https://github.com/{addon.Repo}/archive/refs/heads/main.zip";
            var zipPath = await DownloadZipAsync(client, downloadUrl, Path.Combine(Path.GetTempPath(), "aaemu_addon_dl"), null, cancellationToken);

            var tempExtractPath = Path.Combine(Path.GetTempPath(), "aaemu_addon_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempExtractPath);
            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempExtractPath);
                var repoRoot = Directory.GetDirectories(tempExtractPath).FirstOrDefault() ?? tempExtractPath;
                var sourceFolder = Path.Combine(repoRoot, addon.Folder);
                if (!Directory.Exists(sourceFolder))
                    throw new InvalidOperationException($"The exclusives repo did not contain a '{addon.Folder}' folder.");

                var destination = Path.Combine(addOnsPath, addon.Folder);
                if (Directory.Exists(destination))
                    Directory.Delete(destination, true);
                CopyDirectory(sourceFolder, destination);

                return (new List<string> { addon.Folder }, version);
            }
            finally
            {
                try { Directory.Delete(tempExtractPath, true); } catch { /* best effort */ }
                try { File.Delete(zipPath); } catch { /* best effort */ }
            }
        }

        /// <summary>Downloads and installs an addon from a direct .zip URL (used for recommended dependencies with no GitHub repo).</summary>
        public static async Task<List<string>> InstallFromDirectZipAsync(HttpClient client, string downloadUrl, string addOnsPath, CancellationToken cancellationToken)
        {
            var zipPath = await DownloadZipAsync(client, downloadUrl, Path.Combine(Path.GetTempPath(), "aaemu_addon_dl"), null, cancellationToken);
            return ExtractAddon(zipPath, addOnsPath);
        }

        public static async Task<AddonUpdateCheckResult> CheckExclusiveForUpdateAsync(HttpClient client, InstalledAddon installed, ExclusiveAddon catalogEntry)
        {
            try
            {
                var latestVersion = await GetExclusiveAddonVersionAsync(client, catalogEntry);
                return new AddonUpdateCheckResult
                {
                    Addon = installed,
                    LatestVersion = latestVersion,
                    UpdateAvailable = !string.Equals(latestVersion, installed.Version, StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                return new AddonUpdateCheckResult { Addon = installed, Error = ex.Message };
            }
        }

        /// <summary>
        /// Addon folder/subfolder names that ship with the base game client and should never be listed
        /// as a manageable addon, regardless of how the AddOns folder was populated.
        /// </summary>
        public static bool IsBlizzardAddonFolder(string folderName)
        {
            return !string.IsNullOrEmpty(folderName) && folderName.StartsWith("Blizzard", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Scans the AddOns folder directly and reconciles it with the manifest: folders present on disk but
        /// missing from the manifest are added as "found on disk" entries (source unknown), and manifest
        /// entries whose folders no longer exist are dropped. Blizzard_* folders are always ignored.
        /// </summary>
        public static AddonManifest ScanAndReconcile(string addOnsPath, AddonManifest manifest)
        {
            if (!Directory.Exists(addOnsPath))
                return manifest;

            var onDiskFolders = new HashSet<string>(
                Directory.GetDirectories(addOnsPath).Select(Path.GetFileName).Where(name => !IsBlizzardAddonFolder(name)),
                StringComparer.OrdinalIgnoreCase);

            // Drop manifest entries whose folders were deleted outside the launcher.
            manifest.Addons.RemoveAll(a => a.Folders.Count > 0 && !a.Folders.Any(f => onDiskFolders.Contains(f)));

            var trackedFolders = new HashSet<string>(manifest.Addons.SelectMany(a => a.Folders), StringComparer.OrdinalIgnoreCase);
            foreach (var folder in onDiskFolders)
            {
                if (trackedFolders.Contains(folder))
                    continue;

                manifest.Addons.Add(new InstalledAddon
                {
                    Name = folder,
                    Repo = string.Empty,
                    Version = "unknown",
                    Folders = new List<string> { folder },
                    FoundOnDisk = true
                });
            }

            return manifest;
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

        /// <summary>
        /// Replaces any manifest entry that already owns one of the given folders (regardless of where it came
        /// from - a GitHub repo, a JWoW Exclusive, or a prior "found on disk" scan) and adds the new entry.
        /// A fresh install must always win the folder it just wrote to, or the same folder ends up tracked by
        /// two entries at once, and removing one deletes files the other still thinks it owns.
        /// </summary>
        public static void ReplaceAddonForFolders(AddonManifest manifest, InstalledAddon newEntry)
        {
            var newFolders = new HashSet<string>(newEntry.Folders, StringComparer.OrdinalIgnoreCase);
            manifest.Addons.RemoveAll(a => a.Folders.Any(f => newFolders.Contains(f)));
            manifest.Addons.Add(newEntry);
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
