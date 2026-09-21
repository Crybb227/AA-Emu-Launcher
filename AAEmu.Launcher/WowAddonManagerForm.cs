using System;
using System.Collections.Generic;
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
    /// browsing a curated catalog of popular WotLK 3.3.5 addons, and installing JWoW Exclusive addons.
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
        private Button btnRefreshInstalled;
        private Button btnCheckUpdates;
        private Button btnUpdateSelected;
        private Button btnRemoveSelected;

        // Browse tab
        private readonly TabPage tabBrowse;
        private ListView lvCatalog;
        private PictureBox pbCatalogPreview;
        private Button btnRefreshCatalog;
        private Button btnInstallFromCatalog;
        private bool catalogLoaded;

        // JWoW Exclusives tab
        private readonly TabPage tabExclusives;
        private ListView lvExclusives;
        private PictureBox pbExclusivesPreview;
        private Button btnRefreshExclusives;
        private Button btnInstallExclusive;
        private Button btnCheckExclusiveUpdates;
        private bool exclusivesLoaded;
        private ExclusiveAddonCatalog exclusiveCatalog;

        private readonly Label lStatus;
        private readonly ProgressBar pbProgress;

        private string addOnsPath;
        private AddonManifest manifest;

        public string AddOnsPath => addOnsPath;

        public WowAddonManagerForm(string initialAddOnsPath)
        {
            addOnsPath = initialAddOnsPath ?? string.Empty;

            Text = "WoW Addon Manager";
            ClientSize = new Size(780, 620);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ModernBack;
            ForeColor = ModernText;
            Font = new Font("Segoe UI", 9.5F);

            var lFolderLabel = new Label { Left = 16, Top = 14, Width = 100, Height = 24, Text = "AddOns folder:", ForeColor = ModernMutedText };
            eAddonsFolder = new TextBox { Left = 16, Top = 38, Width = 620, Height = 26, ReadOnly = true, BackColor = ModernPanel, ForeColor = ModernText, BorderStyle = BorderStyle.FixedSingle, Text = addOnsPath };
            btnBrowseFolder = new Button { Left = 644, Top = 37, Width = 120, Height = 26, Text = "Browse...", FlatStyle = FlatStyle.Flat };
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            tabs = new TabControl { Left = 16, Top = 72, Width = 748, Height = 444 };

            tabInstalled = new TabPage("Installed");
            tabBrowse = new TabPage("Browse Popular Addons");
            tabExclusives = new TabPage("JWoW Exclusives");
            tabs.TabPages.Add(tabInstalled);
            tabs.TabPages.Add(tabBrowse);
            tabs.TabPages.Add(tabExclusives);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedTab == tabBrowse && !catalogLoaded)
                    _ = LoadCuratedCatalogAsync();
                else if (tabs.SelectedTab == tabExclusives && !exclusivesLoaded)
                    _ = LoadExclusiveCatalogAsync();
            };

            BuildInstalledTab();
            BuildBrowseTab();
            BuildExclusivesTab();

            lStatus = new Label { Left = 16, Top = 524, Width = 748, Height = 20, Text = string.Empty, ForeColor = ModernMutedText };
            pbProgress = new ProgressBar { Left = 16, Top = 546, Width = 748, Height = 18, Style = ProgressBarStyle.Marquee, Visible = false };

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
            var lRepoLabel = new Label { Left = 12, Top = 10, Width = 380, Height = 24, Text = "Add addon (GitHub repo URL or owner/repo):", ForeColor = ModernMutedText };
            eRepoUrl = new TextBox { Left = 12, Top = 34, Width = 400, Height = 26, BackColor = ModernPanel, ForeColor = ModernText, BorderStyle = BorderStyle.FixedSingle };
            btnAddAddon = new Button { Left = 420, Top = 33, Width = 90, Height = 26, Text = "Install", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black };
            btnAddAddon.Click += BtnAddAddon_Click;
            btnRefreshInstalled = new Button { Left = 516, Top = 33, Width = 90, Height = 26, Text = "Refresh", FlatStyle = FlatStyle.Flat };
            btnRefreshInstalled.Click += (s, e) => RefreshAddonList();
            btnCheckUpdates = new Button { Left = 612, Top = 33, Width = 106, Height = 26, Text = "Check Updates", FlatStyle = FlatStyle.Flat };
            btnCheckUpdates.Click += BtnCheckUpdates_Click;

            lvAddons = new ListView
            {
                Left = 12,
                Top = 70,
                Width = 706,
                Height = 280,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = true,
                BackColor = ModernPanel,
                ForeColor = ModernText,
                BorderStyle = BorderStyle.FixedSingle,
            };
            lvAddons.Columns.Add("Addon", 240);
            lvAddons.Columns.Add("Repo / Source", 230);
            lvAddons.Columns.Add("Version", 100);
            lvAddons.Columns.Add("Status", 120);
            lvAddons.SelectedIndexChanged += (s, e) => UpdateActionButtonsEnabled();

            btnUpdateSelected = new Button { Left = 12, Top = 360, Width = 140, Height = 30, Text = "Update Selected", FlatStyle = FlatStyle.Flat };
            btnUpdateSelected.Click += BtnUpdateSelected_Click;
            btnRemoveSelected = new Button { Left = 160, Top = 360, Width = 140, Height = 30, Text = "Remove Selected", FlatStyle = FlatStyle.Flat, ForeColor = ModernDanger };
            btnRemoveSelected.Click += BtnRemoveSelected_Click;

            var lHint = new Label
            {
                Left = 12,
                Top = 398,
                Width = 706,
                Height = 20,
                Text = "Refresh rescans the AddOns folder directly, so anything you dropped in manually shows up here too (Blizzard_* folders are ignored).",
                ForeColor = ModernMutedText,
                Font = new Font("Segoe UI", 8F)
            };

            tabInstalled.Controls.AddRange(new Control[] { lRepoLabel, eRepoUrl, btnAddAddon, btnRefreshInstalled, btnCheckUpdates, lvAddons, btnUpdateSelected, btnRemoveSelected, lHint });
            tabInstalled.BackColor = ModernBack;
        }

        private void BuildBrowseTab()
        {
            var lHint = new Label { Left = 12, Top = 10, Width = 460, Height = 24, Text = "Popular WotLK 3.3.5 addons, fetched live from GitHub.", ForeColor = ModernMutedText };
            btnRefreshCatalog = new Button { Left = 618, Top = 8, Width = 100, Height = 26, Text = "Refresh", FlatStyle = FlatStyle.Flat };
            btnRefreshCatalog.Click += async (s, e) => await LoadCuratedCatalogAsync(forceRefresh: true);

            lvCatalog = new ListView
            {
                Left = 12,
                Top = 42,
                Width = 480,
                Height = 308,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                BackColor = ModernPanel,
                ForeColor = ModernText,
                BorderStyle = BorderStyle.FixedSingle,
            };
            lvCatalog.Columns.Add("Addon", 160);
            lvCatalog.Columns.Add("Category", 130);
            lvCatalog.Columns.Add("Description", 190);
            lvCatalog.DoubleClick += async (s, e) => await InstallSelectedFromCatalogAsync();
            lvCatalog.SelectedIndexChanged += (s, e) =>
            {
                btnInstallFromCatalog.Enabled = lvCatalog.SelectedItems.Count > 0;
                UpdatePreviewImage(pbCatalogPreview, lvCatalog.SelectedItems.Count > 0 ? ((CuratedAddon)lvCatalog.SelectedItems[0].Tag).ImageUrl : null);
            };

            pbCatalogPreview = new PictureBox { Left = 500, Top = 42, Width = 218, Height = 164, BorderStyle = BorderStyle.FixedSingle, BackColor = ModernPanel, SizeMode = PictureBoxSizeMode.Zoom };

            btnInstallFromCatalog = new Button { Left = 12, Top = 360, Width = 160, Height = 30, Text = "Install Selected", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black, Enabled = false };
            btnInstallFromCatalog.Click += async (s, e) => await InstallSelectedFromCatalogAsync();

            tabBrowse.Controls.AddRange(new Control[] { lHint, btnRefreshCatalog, lvCatalog, pbCatalogPreview, btnInstallFromCatalog });
            tabBrowse.BackColor = ModernBack;
        }

        private void BuildExclusivesTab()
        {
            var lHint = new Label { Left = 12, Top = 10, Width = 460, Height = 24, Text = "Custom addons built for JasonWoW.", ForeColor = ModernMutedText };
            btnRefreshExclusives = new Button { Left = 496, Top = 8, Width = 100, Height = 26, Text = "Refresh", FlatStyle = FlatStyle.Flat };
            btnRefreshExclusives.Click += async (s, e) => await LoadExclusiveCatalogAsync(forceRefresh: true);
            btnCheckExclusiveUpdates = new Button { Left = 602, Top = 8, Width = 116, Height = 26, Text = "Check Updates", FlatStyle = FlatStyle.Flat };
            btnCheckExclusiveUpdates.Click += async (s, e) => await CheckExclusiveUpdatesAsync();

            lvExclusives = new ListView
            {
                Left = 12,
                Top = 42,
                Width = 480,
                Height = 308,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                BackColor = ModernPanel,
                ForeColor = ModernText,
                BorderStyle = BorderStyle.FixedSingle,
            };
            lvExclusives.Columns.Add("Addon", 160);
            lvExclusives.Columns.Add("Description", 220);
            lvExclusives.Columns.Add("Status", 96);
            lvExclusives.DoubleClick += async (s, e) => await InstallSelectedExclusiveAsync();
            lvExclusives.SelectedIndexChanged += (s, e) =>
            {
                btnInstallExclusive.Enabled = lvExclusives.SelectedItems.Count > 0;
                UpdatePreviewImage(pbExclusivesPreview, lvExclusives.SelectedItems.Count > 0 ? ((ExclusiveAddon)lvExclusives.SelectedItems[0].Tag).ImageUrl : null);
            };

            pbExclusivesPreview = new PictureBox { Left = 500, Top = 42, Width = 218, Height = 164, BorderStyle = BorderStyle.FixedSingle, BackColor = ModernPanel, SizeMode = PictureBoxSizeMode.Zoom };

            btnInstallExclusive = new Button { Left = 12, Top = 360, Width = 160, Height = 30, Text = "Install Selected", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black, Enabled = false };
            btnInstallExclusive.Click += async (s, e) => await InstallSelectedExclusiveAsync();

            tabExclusives.Controls.AddRange(new Control[] { lHint, btnRefreshExclusives, btnCheckExclusiveUpdates, lvExclusives, pbExclusivesPreview, btnInstallExclusive });
            tabExclusives.BackColor = ModernBack;
        }

        private void UpdatePreviewImage(PictureBox target, string imageUrl)
        {
            target.Image = null;
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            var url = imageUrl;
            Task.Run(async () =>
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var bytes = await client.GetByteArrayAsync(url);
                        using (var ms = new MemoryStream(bytes))
                        {
                            var image = Image.FromStream(ms);
                            if (!IsDisposed)
                                BeginInvoke(new Action(() => { if (!target.IsDisposed) target.Image = image; }));
                        }
                    }
                }
                catch
                {
                    // Best effort preview; leave the box blank on failure.
                }
            });
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

        private async Task LoadExclusiveCatalogAsync(bool forceRefresh = false)
        {
            if (exclusivesLoaded && !forceRefresh)
                return;

            SetBusy(true, "Fetching JWoW Exclusives list...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    exclusiveCatalog = await AddonManager.FetchExclusiveCatalogAsync(client);
                    RenderExclusivesList();
                    exclusivesLoaded = true;
                    lStatus.Text = $"Loaded {exclusiveCatalog.Addons.Count} JWoW Exclusive addons.";
                }
            }
            catch (Exception ex)
            {
                lStatus.Text = "Failed to load JWoW Exclusives.";
                MessageBox.Show(this, $"Could not fetch the JWoW Exclusives list:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void RenderExclusivesList()
        {
            lvExclusives.Items.Clear();
            if (exclusiveCatalog == null)
                return;

            var installedFolders = new HashSet<string>(
                (manifest?.Addons ?? new List<InstalledAddon>()).Where(a => a.IsExclusive).Select(a => a.ExclusiveFolder),
                StringComparer.OrdinalIgnoreCase);

            foreach (var addon in exclusiveCatalog.Addons.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(addon.Name) { Tag = addon };
                item.SubItems.Add(addon.Description);
                item.SubItems.Add(installedFolders.Contains(addon.Folder) ? "Installed" : string.Empty);
                lvExclusives.Items.Add(item);
            }
        }

        private async Task InstallSelectedFromCatalogAsync()
        {
            if (lvCatalog.SelectedItems.Count == 0)
                return;

            var addon = (CuratedAddon)lvCatalog.SelectedItems[0].Tag;
            await InstallAddonAsync(addon.Repo, addon.Name);
        }

        private async Task InstallSelectedExclusiveAsync()
        {
            if (lvExclusives.SelectedItems.Count == 0)
                return;

            var addon = (ExclusiveAddon)lvExclusives.SelectedItems[0].Tag;
            await InstallExclusiveAddonAsync(addon);
        }

        private async Task InstallExclusiveAddonAsync(ExclusiveAddon addon)
        {
            if (!EnsureAddOnsFolderReady())
                return;

            SetBusy(true, $"Installing {addon.Name}...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    var (folders, version) = await AddonManager.InstallExclusiveAddonAsync(client, addon, addOnsPath, CancellationToken.None);

                    manifest = AddonManager.LoadManifest(addOnsPath);
                    manifest.Addons.RemoveAll(a => string.Equals(a.ExclusiveFolder, addon.Folder, StringComparison.OrdinalIgnoreCase));
                    manifest.Addons.Add(new InstalledAddon
                    {
                        Name = addon.Name,
                        Repo = addon.Repo,
                        Version = version,
                        Folders = folders,
                        IsExclusive = true,
                        ExclusiveFolder = addon.Folder
                    });
                    AddonManager.SaveManifest(addOnsPath, manifest);
                }

                RefreshAddonList();
                RenderExclusivesList();
                lStatus.Text = $"Installed {addon.Name}.";

                if (addon.Recommends != null && addon.Recommends.Count > 0)
                    ShowDependencyPrompt(addon);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to install {addon.Name}:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lStatus.Text = "Install failed.";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ShowDependencyPrompt(ExclusiveAddon addon)
        {
            using (var prompt = new AddonDependencyPromptForm(addon.Name, addon.Recommends))
            {
                prompt.InstallRequested += async dep =>
                {
                    try
                    {
                        using (var client = CreateGitHubClient())
                        {
                            var folders = await AddonManager.InstallFromDirectZipAsync(client, dep.DownloadUrl, addOnsPath, CancellationToken.None);

                            manifest = AddonManager.LoadManifest(addOnsPath);
                            manifest.Addons.RemoveAll(a => string.Equals(a.Name, dep.Name, StringComparison.OrdinalIgnoreCase));
                            manifest.Addons.Add(new InstalledAddon
                            {
                                Name = dep.Name,
                                Repo = string.Empty,
                                Version = "latest",
                                Folders = folders
                            });
                            AddonManager.SaveManifest(addOnsPath, manifest);
                            RefreshAddonList();
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Failed to install {dep.Name}:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                };

                prompt.ShowDialog(this);
            }
        }

        private async Task CheckExclusiveUpdatesAsync()
        {
            if (exclusiveCatalog == null || manifest == null)
                return;

            var installedExclusives = manifest.Addons.Where(a => a.IsExclusive).ToList();
            if (installedExclusives.Count == 0)
            {
                lStatus.Text = "No JWoW Exclusive addons installed yet.";
                return;
            }

            SetBusy(true, "Checking JWoW Exclusives for updates...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    foreach (var installed in installedExclusives)
                    {
                        var catalogEntry = exclusiveCatalog.Addons.FirstOrDefault(a => string.Equals(a.Folder, installed.ExclusiveFolder, StringComparison.OrdinalIgnoreCase));
                        if (catalogEntry == null)
                            continue;

                        var result = await AddonManager.CheckExclusiveForUpdateAsync(client, installed, catalogEntry);
                        var row = lvExclusives.Items.Cast<ListViewItem>().FirstOrDefault(i => ((ExclusiveAddon)i.Tag).Folder == catalogEntry.Folder);
                        if (row != null)
                            row.SubItems[2].Text = result.Error != null ? "Error" : result.UpdateAvailable ? $"Update: {result.LatestVersion}" : "Up to date";
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

        private void SetBusy(bool busy, string statusText = null)
        {
            pbProgress.Visible = busy;
            btnAddAddon.Enabled = !busy;
            btnRefreshInstalled.Enabled = !busy;
            btnCheckUpdates.Enabled = !busy;
            btnBrowseFolder.Enabled = !busy;
            btnRefreshCatalog.Enabled = !busy;
            btnInstallFromCatalog.Enabled = !busy && lvCatalog.SelectedItems.Count > 0;
            btnRefreshExclusives.Enabled = !busy;
            btnCheckExclusiveUpdates.Enabled = !busy;
            btnInstallExclusive.Enabled = !busy && lvExclusives.SelectedItems.Count > 0;
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
            using (var dialog = new FolderBrowserDialog { Description = "Select the WoW AddOns folder (interface\\addons)" })
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

        /// <summary>Loads the manifest and reconciles it against what's actually on disk, so manually-dropped-in addons show up too.</summary>
        private void RefreshAddonList()
        {
            lvAddons.Items.Clear();
            if (string.IsNullOrWhiteSpace(addOnsPath) || !Directory.Exists(addOnsPath))
            {
                lStatus.Text = "Select a valid AddOns folder to manage addons.";
                return;
            }

            manifest = AddonManager.LoadManifest(addOnsPath);
            manifest = AddonManager.ScanAndReconcile(addOnsPath, manifest);
            AddonManager.SaveManifest(addOnsPath, manifest);

            foreach (var addon in manifest.Addons.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(addon.Name) { Tag = addon };
                item.SubItems.Add(addon.IsExclusive ? "JWoW Exclusive" : string.IsNullOrEmpty(addon.Repo) ? "(found on disk)" : addon.Repo);
                item.SubItems.Add(addon.Version);
                item.SubItems.Add(addon.FoundOnDisk ? "Found on disk" : "Installed");
                lvAddons.Items.Add(item);
            }
            lStatus.Text = manifest.Addons.Count == 0 ? "No addons found." : $"{manifest.Addons.Count} addon(s) found.";

            if (exclusivesLoaded)
                RenderExclusivesList();
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
                        if (addon.IsExclusive || string.IsNullOrEmpty(addon.Repo))
                            continue; // exclusives are checked from their own tab; disk-only finds have no known source

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
                        if (string.IsNullOrEmpty(addon.Repo))
                            continue;

                        lStatus.Text = $"Updating {addon.Name}...";
                        if (addon.IsExclusive)
                        {
                            var catalogEntry = exclusiveCatalog?.Addons.FirstOrDefault(a => string.Equals(a.Folder, addon.ExclusiveFolder, StringComparison.OrdinalIgnoreCase));
                            if (catalogEntry == null)
                                continue;

                            var (folders, version) = await AddonManager.InstallExclusiveAddonAsync(client, catalogEntry, addOnsPath, CancellationToken.None);
                            addon.Version = version;
                            addon.Folders = folders;
                        }
                        else
                        {
                            var (downloadUrl, version) = await AddonManager.ResolveDownloadAsync(client, addon.Repo);
                            var zipPath = await AddonManager.DownloadZipAsync(client, downloadUrl, Path.Combine(Path.GetTempPath(), "aaemu_addon_dl"), null, CancellationToken.None);
                            var folders = await Task.Run(() => AddonManager.ExtractAddon(zipPath, addOnsPath));

                            addon.Version = version;
                            addon.Folders = folders;
                        }
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
