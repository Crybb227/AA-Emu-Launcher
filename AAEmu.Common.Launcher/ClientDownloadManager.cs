using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AAEmu.Launcher.Basic
{
    public class ClientDownloadPart
    {
        public string FileId;
        public string FileName;

        public ClientDownloadPart(string fileId, string fileName)
        {
            FileId = fileId;
            FileName = fileName;
        }
    }

    public class ClientDownloadProgress
    {
        public string Stage;
        public string CurrentFile;
        public int CurrentFileIndex;
        public int TotalFiles;
        public long BytesDownloaded;
        public long BytesTotal;
        public string Message;
    }

    /// <summary>
    /// Downloads and extracts the full game client, which is distributed as a
    /// split zip archive (aaemu client.z01 .. .z05 + aaemu client.zip) hosted on Google Drive.
    /// </summary>
    public static class ClientDownloadManager
    {
        // Source folder: https://drive.google.com/drive/folders/1_pIBVHIm1YFal-nteGaVuXjTv3Yrsv4Q
        public static readonly List<ClientDownloadPart> ClientParts = new List<ClientDownloadPart>
        {
            new ClientDownloadPart("1YChiTh5WrS02vP1z8LmI93ghHBYBV6aE", "aaemu client.z01"),
            new ClientDownloadPart("1VZVWlif8teegJbFKPH5aB4QBWOSBuFbS", "aaemu client.z02"),
            new ClientDownloadPart("1fl5mb-zoYDCAahAB4u2umY4UxiY6Uayv", "aaemu client.z03"),
            new ClientDownloadPart("1GgbtIvqtl4ZuEHD2sRJslY56q2nI30L-", "aaemu client.z04"),
            new ClientDownloadPart("1XkT_KtvWpQ8_snP3cJlcz9wyQaRik0pj", "aaemu client.z05"),
            new ClientDownloadPart("1khKIZVMUuKA4ID75XGMCH-8NySQTH-r8", "aaemu client.zip"),
        };

        private const string MainArchiveFileName = "aaemu client.zip";
        private const string MegaCmdDownloadUrl = "https://mega.io/cmd";
        public const string AA30ArchiveFileName = "AA 3.0.3 - Trion - r318414 - 2016-12-08.7z";

        public static async Task DownloadAllPartsAsync(string downloadFolder, IProgress<ClientDownloadProgress> progress, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(downloadFolder);

            for (var i = 0; i < ClientParts.Count; i++)
            {
                var part = ClientParts[i];
                var dest = Path.Combine(downloadFolder, part.FileName);

                // Skip parts that were already fully downloaded in a previous attempt
                if (File.Exists(dest))
                    continue;

                var index = i;
                var fileProgress = new Progress<(long downloaded, long total)>(p =>
                {
                    progress?.Report(new ClientDownloadProgress
                    {
                        Stage = "Downloading",
                        CurrentFile = part.FileName,
                        CurrentFileIndex = index + 1,
                        TotalFiles = ClientParts.Count,
                        BytesDownloaded = p.downloaded,
                        BytesTotal = p.total
                    });
                });

                await GoogleDriveDownloader.DownloadFileAsync(part.FileId, dest, fileProgress, cancellationToken);
            }
        }

        public static string Find7ZipExecutable()
        {
            var candidates = new List<string>
            {
                // Bundled alongside the launcher (see AAEmu.Launcher.csproj), preferred so no separate install is needed
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "7za.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            try
            {
                foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
                {
                    var exeName7Z = Path.Combine(dir, "7z.exe");
                    if (File.Exists(exeName7Z))
                        return exeName7Z;

                    var exeName7Za = Path.Combine(dir, "7za.exe");
                    if (File.Exists(exeName7Za))
                        return exeName7Za;
                }
            }
            catch
            {
                // Ignore malformed PATH entries
            }

            return null;
        }

        public static string FindMegaGetExecutable()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new List<string>
            {
                Path.Combine(localAppData, "MEGAcmd", "mega-get.bat"),
                Path.Combine(localAppData, "MEGAcmd", "mega-get.exe"),
                Path.Combine(localAppData, "MEGAcmd", "mega-get"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MEGAcmd", "mega-get.bat"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MEGAcmd", "mega-get.bat"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            try
            {
                foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(dir))
                        continue;

                    foreach (var name in new[] { "mega-get.bat", "mega-get.exe", "mega-get" })
                    {
                        var candidate = Path.Combine(dir.Trim(), name);
                        if (File.Exists(candidate))
                            return candidate;
                    }
                }
            }
            catch
            {
                // Ignore malformed PATH entries
            }

            return null;
        }

        public static string MegaCmdDownloadPage => MegaCmdDownloadUrl;

        public static async Task DownloadGoogleDriveArchiveAsync(string driveLinkOrFileId, string downloadFolder, string archiveFileName, IProgress<ClientDownloadProgress> progress, CancellationToken cancellationToken = default)
        {
            var fileId = ExtractGoogleDriveFileId(driveLinkOrFileId);
            if (string.IsNullOrWhiteSpace(fileId))
                throw new InvalidOperationException("The Google Drive download link is not valid.");

            Directory.CreateDirectory(downloadFolder);
            var destinationPath = Path.Combine(downloadFolder, archiveFileName);
            var fileProgress = new Progress<(long downloaded, long total)>(p =>
            {
                progress?.Report(new ClientDownloadProgress
                {
                    Stage = "Downloading",
                    CurrentFile = archiveFileName,
                    CurrentFileIndex = 1,
                    TotalFiles = 1,
                    BytesDownloaded = p.downloaded,
                    BytesTotal = p.total
                });
            });

            await GoogleDriveDownloader.DownloadFileAsync(fileId, destinationPath, fileProgress, cancellationToken);
        }

        private static string ExtractGoogleDriveFileId(string driveLinkOrFileId)
        {
            if (string.IsNullOrWhiteSpace(driveLinkOrFileId))
                return string.Empty;

            var value = driveLinkOrFileId.Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return value;

            var marker = "/file/d/";
            var markerIndex = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var start = markerIndex + marker.Length;
                var end = uri.AbsolutePath.IndexOf('/', start);
                return end >= 0
                    ? uri.AbsolutePath.Substring(start, end - start)
                    : uri.AbsolutePath.Substring(start);
            }

            var query = uri.Query.TrimStart('?').Split('&');
            foreach (var part in query)
            {
                var pair = part.Split(new[] { '=' }, 2);
                if (pair.Length == 2 && string.Equals(pair[0], "id", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(pair[1]);
            }

            return string.Empty;
        }

        public static async Task DownloadArchiveUrlAsync(string archiveUrl, string downloadFolder, string archiveFileName, IProgress<ClientDownloadProgress> progress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(archiveUrl) || !Uri.TryCreate(archiveUrl, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("The archive download link is not valid.");

            if (string.IsNullOrWhiteSpace(archiveFileName))
                archiveFileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(archiveFileName))
                archiveFileName = "client.zip";

            Directory.CreateDirectory(downloadFolder);
            var destinationPath = Path.Combine(downloadFolder, archiveFileName);

            using (var webClient = new WebClient())
            using (cancellationToken.Register(webClient.CancelAsync))
            {
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    progress?.Report(new ClientDownloadProgress
                    {
                        Stage = "Downloading",
                        CurrentFile = archiveFileName,
                        CurrentFileIndex = 1,
                        TotalFiles = 1,
                        BytesDownloaded = e.BytesReceived,
                        BytesTotal = e.TotalBytesToReceive
                    });
                };

                await webClient.DownloadFileTaskAsync(uri, destinationPath);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public static async Task DownloadMegaLinkAsync(string megaLink, string downloadFolder, string expectedFileName, IProgress<ClientDownloadProgress> progress, CancellationToken cancellationToken = default)
        {
            var megaGet = FindMegaGetExecutable();
            if (megaGet == null)
                throw new FileNotFoundException("MEGAcmd was not found. Install MEGAcmd from " + MegaCmdDownloadUrl + " and try again.");

            Directory.CreateDirectory(downloadFolder);
            DeleteUnexpectedMegaDownloads(downloadFolder, expectedFileName);
            var startedAt = DateTime.UtcNow;
            progress?.Report(new ClientDownloadProgress
            {
                Stage = "MegaDownloading",
                CurrentFile = "MEGA download",
                Message = "Downloading " + expectedFileName + " from MEGA with MEGAcmd..."
            });

            using (var heartbeat = new Timer(_ =>
            {
                var elapsed = DateTime.UtcNow - startedAt;
                var downloadedBytes = GetExpectedDownloadSize(downloadFolder, expectedFileName);
                var sizeMessage = downloadedBytes > 0
                    ? "Downloaded " + FormatBytes(downloadedBytes) + " so far."
                    : "Waiting for MEGAcmd download data...";

                progress?.Report(new ClientDownloadProgress
                {
                    Stage = "MegaDownloading",
                    CurrentFile = expectedFileName,
                    BytesDownloaded = downloadedBytes,
                    Message = "Downloading " + expectedFileName + "\r\n" + sizeMessage + "\r\nElapsed " + FormatDuration(elapsed) + "."
                });
            }, null, 5000, 5000))
            {
                var megaSource = GetMegaDownloadSource(megaLink, expectedFileName);
                var output = await RunProcessAsync(megaGet, $"\"{megaSource}\" \"{downloadFolder}\"", line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                        return;

                    progress?.Report(new ClientDownloadProgress
                    {
                        Stage = "MegaDownloading",
                        CurrentFile = expectedFileName,
                        BytesDownloaded = GetExpectedDownloadSize(downloadFolder, expectedFileName),
                        Message = line.Trim()
                    });
                }, cancellationToken);

                if (output.ExitCode != 0)
                    throw new InvalidOperationException("MEGAcmd download failed with exit code " + output.ExitCode + ".\r\n" + output.Output.Trim());
            }

            DeleteUnexpectedMegaDownloads(downloadFolder, expectedFileName);
        }

        private static string GetMegaDownloadSource(string megaLink, string expectedFileName)
        {
            if (string.IsNullOrWhiteSpace(expectedFileName) ||
                megaLink.IndexOf("/file/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return megaLink;
            }

            // MEGAcmd can download a direct exported file link by itself. Public folder links
            // generally do not expose a stable child-file path to mega-get, so keep the folder
            // source but enforce the expected archive before extraction.
            return megaLink;
        }

        public static string FindDownloadedArchive(string downloadFolder, string expectedFileName = null)
        {
            if (!Directory.Exists(downloadFolder))
                return null;

            var archiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".7z", ".zip", ".rar" };
            var archives = Directory
                .EnumerateFiles(downloadFolder, "*.*", SearchOption.AllDirectories)
                .Where(path => archiveExtensions.Contains(Path.GetExtension(path)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(expectedFileName))
            {
                return archives.FirstOrDefault(path =>
                    string.Equals(Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase));
            }

            return archives
                .OrderByDescending(path => new FileInfo(path).Length)
                .FirstOrDefault();
        }

        private static long GetExpectedDownloadSize(string downloadFolder, string expectedFileName)
        {
            if (!string.IsNullOrWhiteSpace(expectedFileName))
            {
                try
                {
                    var expectedPath = Directory.EnumerateFiles(downloadFolder, expectedFileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (expectedPath != null)
                        return new FileInfo(expectedPath).Length;
                }
                catch
                {
                    // Fall back to directory size below
                }
            }

            return GetDirectorySize(downloadFolder);
        }

        private static void DeleteUnexpectedMegaDownloads(string downloadFolder, string expectedFileName)
        {
            if (string.IsNullOrWhiteSpace(expectedFileName) || !Directory.Exists(downloadFolder))
                return;

            foreach (var path in Directory.EnumerateFiles(downloadFolder, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                try { File.Delete(path); } catch { /* best effort */ }
            }
        }

        // Archives seen inside the extracted client that also need unpacking ("double packed")
        private static readonly string[] NestedArchiveExtensions = { ".zip", ".7z", ".rar" };

        public static void ExtractClient(string downloadFolder, string destinationFolder, IProgress<ClientDownloadProgress> progress = null)
        {
            var sevenZip = Find7ZipExecutable();
            if (sevenZip == null)
                throw new InvalidOperationException("7-Zip (7z.exe/7za.exe) was not found. The bundled copy may have been removed; reinstall the launcher or install 7-Zip (https://www.7-zip.org/).");

            var mainArchive = Path.Combine(downloadFolder, MainArchiveFileName);
            if (!File.Exists(mainArchive))
                throw new FileNotFoundException("Main client archive part not found", mainArchive);

            ExtractArchive(mainArchive, destinationFolder, progress);
        }

        public static void ExtractArchive(string archivePath, string destinationFolder, IProgress<ClientDownloadProgress> progress = null)
        {
            var sevenZip = Find7ZipExecutable();
            if (sevenZip == null)
                throw new InvalidOperationException("7-Zip (7z.exe/7za.exe) was not found. The bundled copy may have been removed; reinstall the launcher or install 7-Zip (https://www.7-zip.org/).");

            if (!File.Exists(archivePath))
                throw new FileNotFoundException("Client archive not found", archivePath);

            Directory.CreateDirectory(destinationFolder);

            progress?.Report(new ClientDownloadProgress { Stage = "Extracting", CurrentFile = Path.GetFileName(archivePath) });
            RunSevenZipExtract(sevenZip, archivePath, destinationFolder);

            // The client is "double packed": extracting the main archive can leave one or more
            // nested archives behind that need extracting in turn before the game files show up.
            const int maxPasses = 5;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                var nestedArchives = FindNestedArchives(destinationFolder);
                if (nestedArchives.Count == 0)
                    break;

                foreach (var nestedArchivePath in nestedArchives)
                {
                    progress?.Report(new ClientDownloadProgress { Stage = "Extracting", CurrentFile = Path.GetFileName(nestedArchivePath) });
                    var extractInto = Path.GetDirectoryName(nestedArchivePath);
                    RunSevenZipExtract(sevenZip, nestedArchivePath, extractInto);
                    try { File.Delete(nestedArchivePath); } catch { /* best effort */ }
                }
            }
        }

        private static long GetDirectorySize(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                    return 0;

                return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                    .Sum(path =>
                    {
                        try { return new FileInfo(path).Length; }
                        catch { return 0L; }
                    });
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
                return (bytes / 1024D / 1024D / 1024D).ToString("0.0") + " GB";
            if (bytes >= 1024L * 1024L)
                return (bytes / 1024D / 1024D).ToString("0") + " MB";
            if (bytes >= 1024L)
                return (bytes / 1024D).ToString("0") + " KB";
            return bytes + " bytes";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return duration.ToString(@"h\:mm\:ss");
            return duration.ToString(@"m\:ss");
        }

        private static Task<ProcessRunResult> RunProcessAsync(string fileName, string arguments, Action<string> outputLineReceived, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var output = new System.Text.StringBuilder();
                var psiFileName = fileName;
                var psiArguments = arguments;
                if (string.Equals(Path.GetExtension(fileName), ".bat", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetExtension(fileName), ".cmd", StringComparison.OrdinalIgnoreCase))
                {
                    psiFileName = "cmd.exe";
                    psiArguments = "/c \"\"" + fileName + "\" " + arguments + "\"";
                }

                var psi = new ProcessStartInfo(psiFileName, psiArguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                            return;
                        output.AppendLine(e.Data);
                        outputLineReceived?.Invoke(e.Data);
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                            return;
                        output.AppendLine(e.Data);
                        outputLineReceived?.Invoke(e.Data);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    using (cancellationToken.Register(() =>
                    {
                        try
                        {
                            if (!process.HasExited)
                                process.Kill();
                        }
                        catch
                        {
                            // Best effort cancellation
                        }
                    }))
                    {
                        process.WaitForExit();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return new ProcessRunResult { ExitCode = process.ExitCode, Output = output.ToString() };
                }
            }, cancellationToken);
        }

        private class ProcessRunResult
        {
            public int ExitCode;
            public string Output;
        }

        private static void RunSevenZipExtract(string sevenZipExe, string archivePath, string destinationFolder)
        {
            var psi = new ProcessStartInfo(sevenZipExe, $"x \"{archivePath}\" -o\"{destinationFolder}\" -y -aoa")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"7-Zip extraction of '{Path.GetFileName(archivePath)}' failed with exit code {process.ExitCode}.");
            }
        }

        private static List<string> FindNestedArchives(string destinationFolder)
        {
            var found = new List<string>();
            foreach (var extension in NestedArchiveExtensions)
            {
                try
                {
                    found.AddRange(Directory.EnumerateFiles(destinationFolder, "*" + extension, SearchOption.AllDirectories));
                }
                catch
                {
                    // Ignore inaccessible paths
                }
            }
            return found;
        }

        /// <summary>Recursively searches the extracted client folder for the game executable.</summary>
        public static string FindGameExecutable(string destinationFolder)
        {
            foreach (var exeName in new[] { "archeage.exe", "archeworld.exe" })
            {
                try
                {
                    var match = Directory.EnumerateFiles(destinationFolder, exeName, SearchOption.AllDirectories).FirstOrDefault();
                    if (match != null)
                        return match;
                }
                catch
                {
                    // Ignore inaccessible paths
                }
            }
            return null;
        }

        public static void CleanupDownloadedParts(string downloadFolder)
        {
            foreach (var part in ClientParts)
            {
                var path = Path.Combine(downloadFolder, part.FileName);
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }
}
