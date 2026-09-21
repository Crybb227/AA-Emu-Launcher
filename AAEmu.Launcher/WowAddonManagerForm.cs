using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    /// <summary>
    /// Self-contained dialog for installing, updating, and removing WoW addons from GitHub repositories,
    /// plus browsing a curated catalog of popular WotLK 3.3.5 addons fetched live from GitHub.
    /// Built entirely in code (no .resx) to match ClientDownloadForm/URIGenForm.
    /// </summary>
    public class WowAddonManagerForm : Form
    {
        private readonly Color ModernBack = Color.FromArgb(12, 15, 21);
        private readonly Color ModernPanel = Color.FromArgb(29, 34, 45);
        private readonly Color ModernAccent = Color.FromArgb(72, 198, 169);
        private readonly Color ModernText = Color.FromArgb(235, 240, 246);
        private readonly Color ModernMutedText = Color.FromArgb(145, 157, 172);
        private readonly Color ModernDanger = Color.FromArgb(210, 74, 86);

        private readonly TextBox eAddonsFolder;
        private readonly Button btnBrowseFolder;
        private readonly TabControl tabs;

        // Installed tab
        private readonly TabPage tabInstalled;
        private ListView lvAddons;
        private TextBox eRepoUrl;
        private Button btnAddAddon;
        private Button btnCheckUpdates;
        private Button btnUpdateSelected;
        private Button btnRemoveSelected;

        // Browse tab
        private readonly TabPage tabBrowse;
        private ListView lvCatalog;
        private Button btnRefreshCatalog;
        private Button btnInstallFromCatalog;
        private bool catalogLoaded;

        private readonly Label lStatus;
        private readonly ProgressBar pbProgress;

        private string addOnsPath;
        private AddonManifest manifest;

        public string AddOnsPath => addOnsPath;

        public WowAddonManagerForm(string initialAddOnsPath)
        {
            addOnsPath = initialAddOnsPath ?? string.Empty;

            Text = "WoW Addon Manager";
            ClientSize = new Size(760, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ModernBack;
            ForeColor = ModernText;
            Font = new Font("Segoe UI", 9.5F);

            var lFolderLabel = new Label { Left = 16, Top = 14, Width = 100, Height = 24, Text = "AddOns folder:", ForeColor = ModernMutedText };
            eAddonsFolder = new TextBox { Left = 16, Top = 38, Width = 600, Height = 26, ReadOnly = true, BackColor = ModernPanel, ForeColor = ModernText, BorderStyle = BorderStyle.FixedSingle, Text = addOnsPath };
            btnBrowseFolder = new Button { Left = 624, Top = 37, Width = 120, Height = 26, Text = "Browse...", FlatStyle = FlatStyle.Flat };
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            tabs = new TabControl { Left = 16, Top = 72, Width = 728, Height = 424 };

            tabInstalled = new TabPage("Installed");
            tabBrowse = new TabPage("Browse Popular Addons");
            tabs.TabPages.Add(tabInstalled);
            tabs.TabPages.Add(tabBrowse);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedTab == tabBrowse && !catalogLoaded)
                    _ = LoadCuratedCatalogAsync();
            };

            BuildInstalledTab();
            BuildBrowseTab();

            lStatus = new Label { Left = 16, Top = 504, Width = 728, Height = 20, Text = string.Empty, ForeColor = ModernMutedText };
            pbProgress = new ProgressBar { Left = 16, Top = 526, Width = 728, Height = 18, Style = ProgressBarStyle.Marquee, Visible = false };

            Controls.AddRange(new Control[]
            {
                lFolderLabel, eAddonsFolder, btnBrowseFolder,
                tabs,
                lStatus, pbProgress
            });

            Load += (s, e) => RefreshAddonList();
        }

        private void BuildInstalledTab()
        {
            var lRepoLabel = new Label { Left = 12, Top = 10, Width = 400, Height = 24, Text = "Add addon (GitHub repo URL or owner/repo):", ForeColor = ModernMutedText };
            eRepoUrl = new TextBox { Left = 12, Top = 34, Width = 460, Height = 26, BackColor = ModernPanel, ForeColor = ModernText, BorderStyle = BorderStyle.FixedSingle };
            btnAddAddon = new Button { Left = 480, Top = 33, Width = 110, Height = 26, Text = "Install", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black };
            btnAddAddon.Click += BtnAddAddon_Click;
            btnCheckUpdates = new Button { Left = 598, Top = 33, Width = 100, Height = 26, Text = "Check Updates", FlatStyle = FlatStyle.Flat };
            btnCheckUpdates.Click += BtnCheckUpdates_Click;

            lvAddons = new ListView
            {
                Left = 12,
                Top = 70,
                Width = 686,
                Height = 260,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = true,
                BackColor = ModernPanel,
                ForeColor = ModernText,
                BorderStyle = BorderStyle.FixedSingle,
            };
            lvAddons.Columns.Add("Addon", 240);
            lvAddons.Columns.Add("Repo", 220);
            lvAddons.Columns.Add("Version", 100);
            lvAddons.Columns.Add("Status", 110);
            lvAddons.SelectedIndexChanged += (s, e) => UpdateActionButtonsEnabled();

            btnUpdateSelected = new Button { Left = 12, Top = 340, Width = 140, Height = 30, Text = "Update Selected", FlatStyle = FlatStyle.Flat };
            btnUpdateSelected.Click += BtnUpdateSelected_Click;
            btnRemoveSelected = new Button { Left = 160, Top = 340, Width = 140, Height = 30, Text = "Remove Selected", FlatStyle = FlatStyle.Flat, ForeColor = ModernDanger };
            btnRemoveSelected.Click += BtnRemoveSelected_Click;

            tabInstalled.Controls.AddRange(new Control[] { lRepoLabel, eRepoUrl, btnAddAddon, btnCheckUpdates, lvAddons, btnUpdateSelected, btnRemoveSelected });
            tabInstalled.BackColor = ModernBack;
        }

        private void BuildBrowseTab()
        {
            var lHint = new Label { Left = 12, Top = 10, Width = 500, Height = 24, Text = "Popular WotLK 3.3.5 addons, fetched live from GitHub. Double-click or select and install.", ForeColor = ModernMutedText };
            btnRefreshCatalog = new Button { Left = 598, Top = 8, Width = 100, Height = 26, Text = "Refresh", FlatStyle = FlatStyle.Flat };
            btnRefreshCatalog.Click += async (s, e) => await LoadCuratedCatalogAsync(forceRefresh: true);

            lvCatalog = new ListView
            {
                Left = 12,
                Top = 42,
                Width = 686,
                Height = 288,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                BackColor = ModernPanel,
                ForeColor = ModernText,
                BorderStyle = BorderStyle.FixedSingle,
            };
            lvCatalog.Columns.Add("Addon", 200);
            lvCatalog.Columns.Add("Category", 140);
            lvCatalog.Columns.Add("Description", 260);
            lvCatalog.Columns.Add("Repo", 80);
            lvCatalog.DoubleClick += async (s, e) => await InstallSelectedFromCatalogAsync();
            lvCatalog.SelectedIndexChanged += (s, e) => btnInstallFromCatalog.Enabled = lvCatalog.SelectedItems.Count > 0;

            btnInstallFromCatalog = new Button { Left = 12, Top = 340, Width = 160, Height = 30, Text = "Install Selected", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black, Enabled = false };
            btnInstallFromCatalog.Click += async (s, e) => await InstallSelectedFromCatalogAsync();

            tabBrowse.Controls.AddRange(new Control[] { lHint, btnRefreshCatalog, lvCatalog, btnInstallFromCatalog });
            tabBrowse.BackColor = ModernBack;
        }

        private async Task LoadCuratedCatalogAsync(bool forceRefresh = false)
        {
            if (catalogLoaded && !forceRefresh)
                return;

            SetBusy(true, "Fetching curated addon list...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    var catalog = await AddonManager.FetchCuratedCatalogAsync(client);
                    lvCatalog.Items.Clear();
                    foreach (var addon in catalog.Addons.OrderBy(a => a.Category, StringComparer.OrdinalIgnoreCase).ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var item = new ListViewItem(addon.Name) { Tag = addon };
                        item.SubItems.Add(addon.Category);
                        item.SubItems.Add(addon.Description);
                        item.SubItems.Add(addon.Repo);
                        lvCatalog.Items.Add(item);
                    }
                    catalogLoaded = true;
                    lStatus.Text = $"Loaded {catalog.Addons.Count} popular addons.";
                }
            }
            catch (Exception ex)
            {
                lStatus.Text = "Failed to load the addon catalog.";
                MessageBox.Show(this, $"Could not fetch the curated addon list:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task InstallSelectedFromCatalogAsync()
        {
            if (lvCatalog.SelectedItems.Count == 0)
                return;

            var addon = (CuratedAddon)lvCatalog.SelectedItems[0].Tag;
            await InstallAddonAsync(addon.Repo, addon.Name);
        }

        private void SetBusy(bool busy, string statusText = null)
        {
            pbProgress.Visible = busy;
            btnAddAddon.Enabled = !busy;
            btnCheckUpdates.Enabled = !busy;
            btnBrowseFolder.Enabled = !busy;
            btnRefreshCatalog.Enabled = !busy;
            btnInstallFromCatalog.Enabled = !busy && lvCatalog.SelectedItems.Count > 0;
            UpdateActionButtonsEnabled(busy);
            if (statusText != null)
                lStatus.Text = statusText;
        }

        private void UpdateActionButtonsEnabled(bool forceDisabled = false)
        {
            var hasSelection = !forceDisabled && lvAddons.SelectedItems.Count > 0;
            btnUpdateSelected.Enabled = hasSelection;
            btnRemoveSelected.Enabled = hasSelection;
        }

        private void BtnBrowseFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "Select the WoW AddOns folder (Interface\\AddOns)" })
            {
                if (!string.IsNullOrWhiteSpace(addOnsPath) && Directory.Exists(addOnsPath))
                    dialog.SelectedPath = addOnsPath;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    addOnsPath = dialog.SelectedPath;
                    eAddonsFolder.Text = addOnsPath;
                    RefreshAddonList();
                }
            }
        }

        private void RefreshAddonList()
        {
            lvAddons.Items.Clear();
            if (string.IsNullOrWhiteSpace(addOnsPath) || !Directory.Exists(addOnsPath))
            {
                lStatus.Text = "Select a valid AddOns folder to manage addons.";
                return;
            }

            manifest = AddonManager.LoadManifest(addOnsPath);
            foreach (var addon in manifest.Addons.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(addon.Name) { Tag = addon };
                item.SubItems.Add(addon.Repo);
                item.SubItems.Add(addon.Version);
                item.SubItems.Add("Installed");
                lvAddons.Items.Add(item);
            }
            lStatus.Text = manifest.Addons.Count == 0 ? "No addons installed yet." : $"{manifest.Addons.Count} addon(s) installed.";
        }

        private bool EnsureAddOnsFolderReady()
        {
            if (string.IsNullOrWhiteSpace(addOnsPath))
            {
                MessageBox.Show(this, "Select the WoW AddOns folder first.", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                Directory.CreateDirectory(addOnsPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not access the AddOns folder:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async void BtnAddAddon_Click(object sender, EventArgs e)
        {
            var input = eRepoUrl.Text.Trim();
            if (!AddonManager.TryParseRepo(input, out var ownerRepo))
            {
                MessageBox.Show(this, "Enter a GitHub repo URL (https://github.com/owner/repo) or owner/repo.", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (await InstallAddonAsync(ownerRepo, null))
                eRepoUrl.Text = string.Empty;
        }

        /// <summary>Downloads and installs the given repo, whether it came from the manual URL box or the curated catalog.</summary>
        private async Task<bool> InstallAddonAsync(string ownerRepo, string displayName)
        {
            if (!EnsureAddOnsFolderReady())
                return false;

            SetBusy(true, $"Resolving {ownerRepo}...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    var (downloadUrl, version) = await AddonManager.ResolveDownloadAsync(client, ownerRepo);

                    lStatus.Text = $"Downloading {ownerRepo} ({version})...";
                    var zipPath = await AddonManager.DownloadZipAsync(client, downloadUrl, Path.Combine(Path.GetTempPath(), "aaemu_addon_dl"), null, CancellationToken.None);

                    lStatus.Text = "Extracting...";
                    var folders = await Task.Run(() => AddonManager.ExtractAddon(zipPath, addOnsPath));

                    manifest = AddonManager.LoadManifest(addOnsPath);
                    manifest.Addons.RemoveAll(a => string.Equals(a.Repo, ownerRepo, StringComparison.OrdinalIgnoreCase));
                    manifest.Addons.Add(new InstalledAddon
                    {
                        Name = displayName ?? folders.FirstOrDefault() ?? ownerRepo,
                        Repo = ownerRepo,
                        Version = version,
                        Folders = folders
                    });
                    AddonManager.SaveManifest(addOnsPath, manifest);

                    RefreshAddonList();
                    lStatus.Text = $"Installed {displayName ?? ownerRepo} ({version}).";
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to install addon:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lStatus.Text = "Install failed.";
                return false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void BtnCheckUpdates_Click(object sender, EventArgs e)
        {
            if (manifest == null || manifest.Addons.Count == 0)
                return;

            SetBusy(true, "Checking for updates...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    foreach (ListViewItem item in lvAddons.Items)
                    {
                        var addon = (InstalledAddon)item.Tag;
                        var result = await AddonManager.CheckForUpdateAsync(client, addon);
                        item.SubItems[3].Text = result.Error != null
                            ? "Error"
                            : result.UpdateAvailable ? $"Update: {result.LatestVersion}" : "Up to date";
                    }
                }
                lStatus.Text = "Update check complete.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to check for updates:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void BtnUpdateSelected_Click(object sender, EventArgs e)
        {
            var selectedAddons = lvAddons.SelectedItems.Cast<ListViewItem>().Select(i => (InstalledAddon)i.Tag).ToList();
            if (selectedAddons.Count == 0)
                return;

            SetBusy(true);
            try
            {
                using (var client = CreateGitHubClient())
                {
                    foreach (var addon in selectedAddons)
                    {
                        lStatus.Text = $"Updating {addon.Name}...";
                        var (downloadUrl, version) = await AddonManager.ResolveDownloadAsync(client, addon.Repo);
                        var zipPath = await AddonManager.DownloadZipAsync(client, downloadUrl, Path.Combine(Path.GetTempPath(), "aaemu_addon_dl"), null, CancellationToken.None);
                        var folders = await Task.Run(() => AddonManager.ExtractAddon(zipPath, addOnsPath));

                        addon.Version = version;
                        addon.Folders = folders;
                    }

                    AddonManager.SaveManifest(addOnsPath, manifest);
                }

                RefreshAddonList();
                lStatus.Text = "Update complete.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to update addon:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lStatus.Text = "Update failed.";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            var selectedAddons = lvAddons.SelectedItems.Cast<ListViewItem>().Select(i => (InstalledAddon)i.Tag).ToList();
            if (selectedAddons.Count == 0)
                return;

            var names = string.Join(", ", selectedAddons.Select(a => a.Name));
            if (MessageBox.Show(this, $"Remove {names}? This deletes the addon's files from the AddOns folder.", "Remove Addon", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            foreach (var addon in selectedAddons)
            {
                try
                {
                    AddonManager.RemoveAddon(addOnsPath, addon);
                    manifest.Addons.Remove(addon);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to remove {addon.Name}:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            AddonManager.SaveManifest(addOnsPath, manifest);
            RefreshAddonList();
        }

        private static HttpClient CreateGitHubClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AAEmu.Launcher");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }
    }
}
