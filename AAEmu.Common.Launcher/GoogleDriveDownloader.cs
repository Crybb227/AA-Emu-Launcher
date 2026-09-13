using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AAEmu.Launcher.Basic
{
    /// <summary>
    /// Downloads publicly shared files from Google Drive, bypassing the
    /// "can't scan for viruses" interstitial page that Drive shows for large files.
    /// </summary>
    public static class GoogleDriveDownloader
    {
        private const string BaseDownloadUrl = "https://drive.google.com/uc?export=download";

        public static async Task DownloadFileAsync(string fileId, string destinationPath, IProgress<(long downloaded, long total)> progress, CancellationToken cancellationToken = default)
        {
            var cookies = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = true })
            using (var client = new HttpClient(handler))
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 AAEmu.Launcher");

                var url = $"{BaseDownloadUrl}&id={fileId}&confirm=t";
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // For very large files Drive can still return a small HTML confirmation page instead of the file
                if (IsHtmlResponse(response))
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var confirmUrl = ExtractConfirmUrl(html, fileId);
                    if (confirmUrl == null)
                        throw new InvalidOperationException($"Unable to resolve Google Drive download link for file '{fileId}'. It may no longer be shared publicly.");

                    response.Dispose();
                    response = await client.GetAsync(confirmUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                }

                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength ?? -1L;
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var tmpPath = destinationPath + ".part";
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
                {
                    var buffer = new byte[1024 * 1024];
                    long downloaded = 0;
                    int read;
                    while ((read = await httpStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await fileStream.WriteAsync(buffer, 0, read, cancellationToken);
                        downloaded += read;
                        progress?.Report((downloaded, total));
                    }
                }

                response.Dispose();

                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Move(tmpPath, destinationPath);
            }
        }

        private static bool IsHtmlResponse(HttpResponseMessage response)
        {
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            return mediaType != null && mediaType.Contains("text/html");
        }

        private static string ExtractConfirmUrl(string html, string fileId)
        {
            // Current Drive pages embed a download form with a base action + hidden fields
            var actionMatch = Regex.Match(html, "action=\"(https://drive\\.usercontent\\.google\\.com/download[^\"]*)\"");
            if (actionMatch.Success)
            {
                var action = WebUtility.HtmlDecode(actionMatch.Groups[1].Value);
                var hasQuery = action.Contains("?");
                var sb = new StringBuilder(action);

                foreach (Match m in Regex.Matches(html, "<input type=\"hidden\" name=\"(?<name>[^\"]+)\" value=\"(?<value>[^\"]*)\""))
                {
                    sb.Append(hasQuery ? "&" : "?");
                    hasQuery = true;
                    sb.Append(Uri.EscapeDataString(m.Groups["name"].Value));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(m.Groups["value"].Value));
                }

                return sb.ToString();
            }

            // Older style: a plain confirm token used as a query parameter
            var confirmMatch = Regex.Match(html, "confirm=([0-9A-Za-z_\\-]+)");
            if (confirmMatch.Success)
                return $"{BaseDownloadUrl}&id={fileId}&confirm={confirmMatch.Groups[1].Value}";

            return null;
        }
    }
}
