using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static void ExtractClient(string downloadFolder, string destinationFolder, IProgress<ClientDownloadProgress> progress = null)
        {
            var sevenZip = Find7ZipExecutable();
            if (sevenZip == null)
                throw new InvalidOperationException("7-Zip (7z.exe/7za.exe) was not found. The bundled copy may have been removed; reinstall the launcher or install 7-Zip (https://www.7-zip.org/).");

            var mainArchive = Path.Combine(downloadFolder, MainArchiveFileName);
            if (!File.Exists(mainArchive))
                throw new FileNotFoundException("Main client archive part not found", mainArchive);

            Directory.CreateDirectory(destinationFolder);

            progress?.Report(new ClientDownloadProgress { Stage = "Extracting", CurrentFile = MainArchiveFileName });

            var psi = new ProcessStartInfo(sevenZip, $"x \"{mainArchive}\" -o\"{destinationFolder}\" -y -aoa")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"7-Zip extraction failed with exit code {process.ExitCode}.");
            }
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
