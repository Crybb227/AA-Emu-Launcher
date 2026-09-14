using AAEmu.Launcher.Basic;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    /// <summary>
    /// Small standalone dialog that downloads the split game client archive from Google Drive
    /// and extracts it into the target folder. Built entirely in code (no .resx) to keep this self-contained.
    /// </summary>
    public class ClientDownloadForm : Form
    {
        private readonly Label lStatus;
        private readonly ProgressBar pbProgress;
        private readonly Button btnCancel;
        private readonly BackgroundWorker worker;
        private CancellationTokenSource cts;
        private readonly string megaDownloadLocation;
        private readonly string googleDriveDownloadLocation;
        private readonly string expectedArchiveFileName;

        public string DestinationGameFolder { get; }
        public string DetectedExePath { get; private set; }

        public ClientDownloadForm(string destinationGameFolder, string megaDownloadLocation = null, string expectedArchiveFileName = null, string googleDriveDownloadLocation = null)
        {
            DestinationGameFolder = destinationGameFolder;
            this.megaDownloadLocation = megaDownloadLocation;
            this.googleDriveDownloadLocation = googleDriveDownloadLocation;
            this.expectedArchiveFileName = expectedArchiveFileName;

            Text = !string.IsNullOrWhiteSpace(megaDownloadLocation)
                ? "Downloading MEGA Client"
                : !string.IsNullOrWhiteSpace(googleDriveDownloadLocation)
                    ? "Downloading Google Drive Client"
                    : "Downloading Game Client";
            ClientSize = new Size(560, 160);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            lStatus = new Label { Left = 12, Top = 12, Width = 536, Height = 78, Text = "Preparing download..." };
            pbProgress = new ProgressBar { Left = 12, Top = 96, Width = 536, Height = 24, Minimum = 0, Maximum = 100 };
            btnCancel = new Button { Left = 458, Top = 126, Width = 90, Height = 26, Text = "Cancel" };
            btnCancel.Click += BtnCancel_Click;

            Controls.Add(lStatus);
            Controls.Add(pbProgress);
            Controls.Add(btnCancel);

            worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            Load += (s, e) =>
            {
                cts = new CancellationTokenSource();
                worker.RunWorkerAsync();
            };
            FormClosing += (s, e) =>
            {
                if (worker.IsBusy && DialogResult != DialogResult.OK)
                {
                    cts?.Cancel();
                }
            };
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            btnCancel.Enabled = false;
            lStatus.Text = "Cancelling...";
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var token = cts.Token;
            var downloadFolder = Path.Combine(DestinationGameFolder, ".clientdownload");

            var progress = new Progress<ClientDownloadProgress>(p => worker.ReportProgress(0, p));

            if (!string.IsNullOrWhiteSpace(googleDriveDownloadLocation))
            {
                ClientDownloadManager.DownloadGoogleDriveArchiveAsync(googleDriveDownloadLocation, downloadFolder, expectedArchiveFileName, progress, token).GetAwaiter().GetResult();
                token.ThrowIfCancellationRequested();

                var archivePath = ClientDownloadManager.FindDownloadedArchive(downloadFolder, expectedArchiveFileName);
                if (string.IsNullOrWhiteSpace(archivePath))
                    throw new FileNotFoundException("Google Drive download completed, but the expected client archive was not found: " + expectedArchiveFileName);

                ClientDownloadManager.ExtractArchive(archivePath, DestinationGameFolder, progress);
                token.ThrowIfCancellationRequested();
            }
            else if (string.IsNullOrWhiteSpace(megaDownloadLocation))
            {
                ClientDownloadManager.DownloadAllPartsAsync(downloadFolder, progress, token).GetAwaiter().GetResult();
                token.ThrowIfCancellationRequested();

                ClientDownloadManager.ExtractClient(downloadFolder, DestinationGameFolder, progress);
                token.ThrowIfCancellationRequested();

                ClientDownloadManager.CleanupDownloadedParts(downloadFolder);
            }
            else
            {
                ClientDownloadManager.DownloadMegaLinkAsync(megaDownloadLocation, downloadFolder, expectedArchiveFileName, progress, token).GetAwaiter().GetResult();
                token.ThrowIfCancellationRequested();

                var archivePath = ClientDownloadManager.FindDownloadedArchive(downloadFolder, expectedArchiveFileName);
                if (string.IsNullOrWhiteSpace(archivePath))
                    throw new FileNotFoundException("MEGA download completed, but the expected client archive was not found: " + expectedArchiveFileName);

                ClientDownloadManager.ExtractArchive(archivePath, DestinationGameFolder, progress);
                token.ThrowIfCancellationRequested();
            }

            try { Directory.Delete(downloadFolder, true); } catch { /* best effort */ }

            DetectedExePath = ClientDownloadManager.FindGameExecutable(DestinationGameFolder);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!(e.UserState is ClientDownloadProgress p))
                return;

            if (p.Stage == "Extracting")
            {
                pbProgress.Style = ProgressBarStyle.Marquee;
                lStatus.Text = "Extracting " + p.CurrentFile + ", this can take a while...";
                return;
            }

            if (p.Stage == "MegaDownloading")
            {
                pbProgress.Style = ProgressBarStyle.Marquee;
                lStatus.Text = string.IsNullOrWhiteSpace(p.Message) ? "Downloading from MEGA..." : p.Message;
                return;
            }

            pbProgress.Style = ProgressBarStyle.Continuous;
            var percent = p.BytesTotal > 0 ? (int)(p.BytesDownloaded * 100L / p.BytesTotal) : 0;
            pbProgress.Value = Math.Max(0, Math.Min(100, percent));
            lStatus.Text = $"Downloading part {p.CurrentFileIndex}/{p.TotalFiles}: {p.CurrentFile}\n{p.BytesDownloaded / 1024 / 1024} MB / {p.BytesTotal / 1024 / 1024} MB";
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(this, e.Error.Message, "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Abort;
                Close();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
