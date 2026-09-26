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
        private readonly Color ModernBack = WowTheme.Back;
        private readonly Color ModernPanel = WowTheme.Panel;
        private readonly Color ModernAccent = WowTheme.Accent;
        private readonly Color ModernText = WowTheme.Text;
        private readonly Color ModernMutedText = WowTheme.MutedText;
        private readonly Color ModernDanger = WowTheme.Danger;

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
        private Label lInstalledHint;

        // Browse tab
        private readonly TabPage tabBrowse;
        private ListView lvCatalog;
        private PictureBox pbCatalogPreview;
        private Button btnRefreshCatalog;
        private Button btnInstallFromCatalog;
        private bool catalogLoaded;
        private Label lBrowseHint;

        // JWoW Exclusives tab
        private readonly TabPage tabExclusives;
        private ListView lvExclusives;
        private PictureBox pbExclusivesPreview;
        private Button btnRefreshExclusives;
        private Button btnInstallExclusive;
        private Button btnCheckExclusiveUpdates;
        private bool exclusivesLoaded;
        private ExclusiveAddonCatalog exclusiveCatalog;
        private Label lExclusivesHint;

        private readonly Label lStatus;
        private readonly ProgressBar pbProgress;
        private readonly Label lInstalledSummary;
        private readonly Label lManagedSummary;
        private readonly Label lUpdatesSummary;

        private string addOnsPath;
        private AddonManifest manifest;

        public string AddOnsPath => addOnsPath;

        public WowAddonManagerForm(string initialAddOnsPath)
        {
            addOnsPath = initialAddOnsPath ?? string.Empty;

            Text = "JasonWoW Addon Manager";
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(960, 720);
            MinimumSize = new Size(780, 620);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = ModernBack;
            ForeColor = ModernText;
            Font = new Font("Segoe UI", 9.5F);

            var lTitle = new Label { Left = 16, Top = 12, Width = 720, Height = 44, Text = "JasonWoW Addons", ForeColor = ModernText, Font = new Font("Palatino Linotype", 20F, FontStyle.Bold), AutoEllipsis = true };
            var lSubtitle = new Label { Left = 18, Top = 53, Width = 760, Height = 24, Text = "Manage, discover, and update your interface without leaving the launcher.", ForeColor = ModernMutedText, AutoEllipsis = true };
            lInstalledSummary = CreateSummaryLabel(16, "0 INSTALLED");
            lManagedSummary = CreateSummaryLabel(266, "0 MANAGED");
            lUpdatesSummary = CreateSummaryLabel(516, "UPDATES NOT CHECKED");

            var lFolderLabel = new Label { Left = 16, Top = 112, Width = 150, Height = 22, Text = "GAME ADDONS LOCATION", ForeColor = ModernMutedText, Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold) };
            eAddonsFolder = new TextBox { Left = 16, Top = 134, Width = 620, Height = 28, ReadOnly = true, BackColor = ModernPanel, ForeColor = ModernText, BorderStyle = BorderStyle.FixedSingle, Text = addOnsPath };
            btnBrowseFolder = new Button { Left = 644, Top = 133, Width = 120, Height = 29, Text = "Change...", FlatStyle = FlatStyle.Flat };
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            tabs = new TabControl
            {
                Left = 16, Top = 174, Width = 748, Height = 438,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(190, 34),
                Padding = new Point(14, 5)
            };
            tabs.DrawItem += Tabs_DrawItem;

            tabInstalled = new TabPage("Installed");
            tabBrowse = new TabPage("Browse Popular Addons");
            tabExclusives = new TabPage("JasonWoW Exclusives");
            tabInstalled.UseVisualStyleBackColor = false;
            tabBrowse.UseVisualStyleBackColor = false;
            tabExclusives.UseVisualStyleBackColor = false;
            tabs.TabPages.Add(tabInstalled);
            tabs.TabPages.Add(tabBrowse);
            tabs.TabPages.Add(tabExclusives);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                LayoutResponsive();
                if (tabs.SelectedTab == tabBrowse && !catalogLoaded)
                    _ = LoadCuratedCatalogAsync();
                else if (tabs.SelectedTab == tabExclusives && !exclusivesLoaded)
                    _ = LoadExclusiveCatalogAsync();
            };

            BuildInstalledTab();
            BuildBrowseTab();
            BuildExclusivesTab();

            lStatus = new Label { Left = 16, Top = 620, Width = 748, Height = 22, Text = string.Empty, ForeColor = ModernMutedText };
            pbProgress = new ProgressBar { Left = 16, Top = 646, Width = 748, Height = 8, Style = ProgressBarStyle.Marquee, Visible = false };

            Controls.AddRange(new Control[]
            {
                lTitle, lSubtitle, lInstalledSummary, lManagedSummary, lUpdatesSummary,
                lFolderLabel, eAddonsFolder, btnBrowseFolder,
                tabs,
                lStatus, pbProgress
            });

            StyleAllButtons(this);

            Load += (s, e) => { LayoutResponsive(); RefreshAddonList(); };
            Resize += (s, e) => LayoutResponsive();
        }

        private Label CreateSummaryLabel(int left, string text)
        {
            return new Label
            {
                Left = left, Top = 78, Width = 232, Height = 27, Text = text,
                BackColor = ModernPanel, ForeColor = ModernAccent,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            bool selected = e.Index == tabs.SelectedIndex;
            var bounds = e.Bounds;
            using (var background = new SolidBrush(selected ? ModernPanel : ModernBack))
                e.Graphics.FillRectangle(background, bounds);
            if (selected)
            {
                using (var accent = new SolidBrush(ModernAccent))
                    e.Graphics.FillRectangle(accent, bounds.Left, bounds.Bottom - 3, bounds.Width, 3);
            }
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, Font, bounds,
                selected ? ModernText : ModernMutedText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void StyleAllButtons(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button button)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = WowTheme.Border;
                    button.FlatAppearance.MouseOverBackColor = WowTheme.PanelAlt;
                    button.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 57, 34);
                    button.Cursor = Cursors.Hand;
                }
                if (control.HasChildren) StyleAllButtons(control);
            }
        }

        private void LayoutResponsive()
        {
            if (tabs == null || lStatus == null || ClientSize.Width < 1 || ClientSize.Height < 1)
                return;

            const int margin = 16;
            const int gap = 8;
            int contentWidth = ClientSize.Width - margin * 2;
            int cardWidth = Math.Max(150, (contentWidth - gap * 2) / 3);
            lInstalledSummary.SetBounds(margin, 82, cardWidth, 30);
            lManagedSummary.SetBounds(margin + cardWidth + gap, 82, cardWidth, 30);
            lUpdatesSummary.SetBounds(margin + (cardWidth + gap) * 2, 82,
                contentWidth - (cardWidth + gap) * 2, 30);

            btnBrowseFolder.SetBounds(ClientSize.Width - margin - 122, 140, 122, 30);
            eAddonsFolder.SetBounds(margin, 140, btnBrowseFolder.Left - margin - gap, 30);
            tabs.SetBounds(margin, 182, contentWidth, Math.Max(360, ClientSize.Height - 252));
            lStatus.SetBounds(margin, ClientSize.Height - 58, contentWidth, 24);
            pbProgress.SetBounds(margin, ClientSize.Height - 28, contentWidth, 8);

            LayoutInstalledTab();
            LayoutBrowseTab();
            LayoutExclusivesTab();
        }

        private void LayoutInstalledTab()
        {
            if (tabInstalled == null || tabInstalled.ClientSize.Width < 100) return;
            int width = tabInstalled.ClientSize.Width, height = tabInstalled.ClientSize.Height;
            const int margin = 12, gap = 8;
            int checkWidth = 118, refreshWidth = 90, installWidth = 90;
            btnCheckUpdates.SetBounds(width - margin - checkWidth, 34, checkWidth, 29);
            btnRefreshInstalled.SetBounds(btnCheckUpdates.Left - gap - refreshWidth, 34, refreshWidth, 29);
            btnAddAddon.SetBounds(btnRefreshInstalled.Left - gap - installWidth, 34, installWidth, 29);
            eRepoUrl.SetBounds(margin, 34, Math.Max(160, btnAddAddon.Left - margin - gap), 29);

            int actionTop = height - 58;
            btnUpdateSelected.SetBounds(margin, actionTop, 150, 32);
            btnRemoveSelected.SetBounds(margin + 158, actionTop, 150, 32);
            lInstalledHint.SetBounds(margin + 322, actionTop + 2, Math.Max(120, width - margin * 2 - 322), 34);
            lvAddons.SetBounds(margin, 74, width - margin * 2, Math.Max(170, actionTop - 84));
            int listWidth = Math.Max(400, lvAddons.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
            lvAddons.Columns[0].Width = (int)(listWidth * .31);
            lvAddons.Columns[1].Width = (int)(listWidth * .34);
            lvAddons.Columns[2].Width = (int)(listWidth * .15);
            lvAddons.Columns[3].Width = listWidth - lvAddons.Columns[0].Width - lvAddons.Columns[1].Width - lvAddons.Columns[2].Width;
        }

        private void LayoutBrowseTab()
        {
            if (tabBrowse == null || tabBrowse.ClientSize.Width < 100) return;
            int width = tabBrowse.ClientSize.Width, height = tabBrowse.ClientSize.Height;
            const int margin = 12, gap = 10;
            btnRefreshCatalog.SetBounds(width - margin - 104, 8, 104, 29);
            lBrowseHint.SetBounds(margin, 10, Math.Max(150, btnRefreshCatalog.Left - margin - gap), 24);
            int previewWidth = Math.Max(210, width / 3);
            int listWidth = width - margin * 2 - gap - previewWidth;
            int listHeight = Math.Max(190, height - 112);
            lvCatalog.SetBounds(margin, 46, listWidth, listHeight);
            pbCatalogPreview.SetBounds(margin + listWidth + gap, 46, previewWidth, Math.Min(210, listHeight));
            btnInstallFromCatalog.SetBounds(margin, height - 54, 170, 32);
        }

        private void LayoutExclusivesTab()
        {
            if (tabExclusives == null || tabExclusives.ClientSize.Width < 100) return;
            int width = tabExclusives.ClientSize.Width, height = tabExclusives.ClientSize.Height;
            const int margin = 12, gap = 10;
            btnCheckExclusiveUpdates.SetBounds(width - margin - 124, 8, 124, 29);
            btnRefreshExclusives.SetBounds(btnCheckExclusiveUpdates.Left - gap - 104, 8, 104, 29);
            lExclusivesHint.SetBounds(margin, 10, Math.Max(130, btnRefreshExclusives.Left - margin - gap), 24);
            int previewWidth = Math.Max(210, width / 3);
            int listWidth = width - margin * 2 - gap - previewWidth;
            int listHeight = Math.Max(190, height - 112);
            lvExclusives.SetBounds(margin, 46, listWidth, listHeight);
            pbExclusivesPreview.SetBounds(margin + listWidth + gap, 46, previewWidth, Math.Min(210, listHeight));
            btnInstallExclusive.SetBounds(margin, height - 54, 170, 32);
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

            lInstalledHint = new Label
            {
                Left = 12,
                Top = 398,
                Width = 706,
                Height = 20,
                Text = "Refresh rescans the AddOns folder directly, so anything you dropped in manually shows up here too (Blizzard_* folders are ignored).",
                ForeColor = ModernMutedText,
                Font = new Font("Segoe UI", 8F)
            };

            tabInstalled.Controls.AddRange(new Control[] { lRepoLabel, eRepoUrl, btnAddAddon, btnRefreshInstalled, btnCheckUpdates, lvAddons, btnUpdateSelected, btnRemoveSelected, lInstalledHint });
            tabInstalled.BackColor = ModernBack;
        }

        private void BuildBrowseTab()
        {
            lBrowseHint = new Label { Left = 12, Top = 10, Width = 460, Height = 24, Text = "Popular WotLK 3.3.5 addons, fetched live from GitHub.", ForeColor = ModernMutedText };
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
                var hasSelection = lvCatalog.SelectedItems.Count > 0;
                btnInstallFromCatalog.Enabled = hasSelection;
                if (hasSelection)
                {
                    var selected = (CuratedAddon)lvCatalog.SelectedItems[0].Tag;
                    UpdatePreviewImage(pbCatalogPreview, selected.ImageUrl);
                    var alreadyInstalled = (manifest?.Addons ?? new List<InstalledAddon>()).Any(a => string.Equals(a.Repo, selected.Repo, StringComparison.OrdinalIgnoreCase));
                    btnInstallFromCatalog.Text = alreadyInstalled ? "Reinstall Selected" : "Install Selected";
                }
                else
                {
                    UpdatePreviewImage(pbCatalogPreview, null);
                    btnInstallFromCatalog.Text = "Install Selected";
                }
            };

            pbCatalogPreview = new PictureBox { Left = 500, Top = 42, Width = 218, Height = 164, BorderStyle = BorderStyle.FixedSingle, BackColor = ModernPanel, SizeMode = PictureBoxSizeMode.Zoom };

            btnInstallFromCatalog = new Button { Left = 12, Top = 360, Width = 160, Height = 30, Text = "Install Selected", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black, Enabled = false };
            btnInstallFromCatalog.Click += async (s, e) => await InstallSelectedFromCatalogAsync();

            tabBrowse.Controls.AddRange(new Control[] { lBrowseHint, btnRefreshCatalog, lvCatalog, pbCatalogPreview, btnInstallFromCatalog });
            tabBrowse.BackColor = ModernBack;
        }

        private void BuildExclusivesTab()
        {
            lExclusivesHint = new Label { Left = 12, Top = 10, Width = 460, Height = 24, Text = "Custom addons built for JasonWoW.", ForeColor = ModernMutedText };
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
                var hasSelection = lvExclusives.SelectedItems.Count > 0;
                btnInstallExclusive.Enabled = hasSelection;
                if (hasSelection)
                {
                    var selected = (ExclusiveAddon)lvExclusives.SelectedItems[0].Tag;
                    UpdatePreviewImage(pbExclusivesPreview, selected.ImageUrl);
                    var alreadyInstalled = (manifest?.Addons ?? new List<InstalledAddon>()).Any(a => a.Folders.Contains(selected.Folder, StringComparer.OrdinalIgnoreCase));
                    btnInstallExclusive.Text = alreadyInstalled ? "Reinstall Selected" : "Install Selected";
                }
                else
                {
                    UpdatePreviewImage(pbExclusivesPreview, null);
                    btnInstallExclusive.Text = "Install Selected";
                }
            };

            pbExclusivesPreview = new PictureBox { Left = 500, Top = 42, Width = 218, Height = 164, BorderStyle = BorderStyle.FixedSingle, BackColor = ModernPanel, SizeMode = PictureBoxSizeMode.Zoom };

            btnInstallExclusive = new Button { Left = 12, Top = 360, Width = 160, Height = 30, Text = "Install Selected", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black, Enabled = false };
            btnInstallExclusive.Click += async (s, e) => await InstallSelectedExclusiveAsync();

            tabExclusives.Controls.AddRange(new Control[] { lExclusivesHint, btnRefreshExclusives, btnCheckExclusiveUpdates, lvExclusives, pbExclusivesPreview, btnInstallExclusive });
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

            SetBusy(true, "Fetching JasonWoW Exclusives list...");
            try
            {
                using (var client = CreateGitHubClient())
                {
                    exclusiveCatalog = await AddonManager.FetchExclusiveCatalogAsync(client);
                    RenderExclusivesList();
                    exclusivesLoaded = true;
                    lStatus.Text = $"Loaded {exclusiveCatalog.Addons.Count} JasonWoW Exclusive addons.";
                }
            }
            catch (Exception ex)
            {
                lStatus.Text = "Failed to load JasonWoW Exclusives.";
                MessageBox.Show(this, $"Could not fetch the JasonWoW Exclusives list:\n{ex.Message}", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            var previouslySelectedFolder = lvExclusives.SelectedItems.Count > 0 ? ((ExclusiveAddon)lvExclusives.SelectedItems[0].Tag).Folder : null;

            var installedFolders = new HashSet<string>(
                (manifest?.Addons ?? new List<InstalledAddon>()).SelectMany(a => a.Folders),
                StringComparer.OrdinalIgnoreCase);

            foreach (var addon in exclusiveCatalog.Addons.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(addon.Name) { Tag = addon };
                item.SubItems.Add(addon.Description);
                item.SubItems.Add(installedFolders.Contains(addon.Folder) ? "Installed" : string.Empty);
                lvExclusives.Items.Add(item);

                if (string.Equals(addon.Folder, previouslySelectedFolder, StringComparison.OrdinalIgnoreCase))
                    item.Selected = true;
            }
        }

        private async Task InstallSelectedFromCatalogAsync()
        {
            if (lvCatalog.SelectedItems.Count == 0)
                return;

            var addon = (CuratedAddon)lvCatalog.SelectedItems[0].Tag;
            await InstallAddonAsync(addon.Repo, addon.Name);
        }

        /// <summary>Recomputes the Browse tab's Install/Reinstall button text for whatever's currently selected, against the freshly-reloaded manifest.</summary>
        private void RefreshCatalogSelectionButtonText()
        {
            if (lvCatalog.SelectedItems.Count == 0)
                return;

            var selected = (CuratedAddon)lvCatalog.SelectedItems[0].Tag;
            var alreadyInstalled = (manifest?.Addons ?? new List<InstalledAddon>()).Any(a => string.Equals(a.Repo, selected.Repo, StringComparison.OrdinalIgnoreCase));
            btnInstallFromCatalog.Text = alreadyInstalled ? "Reinstall Selected" : "Install Selected";
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
                    AddonManager.ReplaceAddonForFolders(manifest, new InstalledAddon
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
                            AddonManager.ReplaceAddonForFolders(manifest, new InstalledAddon
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

            // Chatter has no meaningful "version" of its own (it's just a UI for LLM chatter tone/traits),
            // so it's excluded from update checks.
            var installedExclusives = manifest.Addons
                .Where(a => a.IsExclusive && !string.Equals(a.ExclusiveFolder, "Chatter", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (installedExclusives.Count == 0)
            {
                lStatus.Text = "No JasonWoW Exclusive addons installed yet.";
                return;
            }

            SetBusy(true, "Checking JasonWoW Exclusives for updates...");
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
                UpdateSummaryCards();
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
            using (var dialog = new FolderBrowserDialog { Description = "Select the JasonWoW AddOns folder (interface\\addons)" })
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

            manifest = AddonManager.GetInstalledAddons(addOnsPath);
            AddonManager.SaveManifest(addOnsPath, manifest);

            foreach (var addon in manifest.Addons.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(addon.Name) { Tag = addon };
                item.SubItems.Add(addon.IsExclusive ? "JasonWoW Exclusive" : string.IsNullOrEmpty(addon.Repo) ? "(found on disk)" : addon.Repo);
                item.SubItems.Add(addon.Version);
                item.SubItems.Add(addon.FoundOnDisk ? "Found on disk" : "Installed");
                lvAddons.Items.Add(item);
            }
            lStatus.Text = manifest.Addons.Count == 0 ? "No addons found." : $"{manifest.Addons.Count} addon(s) installed.";
            tabInstalled.Text = $"Installed ({manifest.Addons.Count})";
            UpdateSummaryCards();

            if (exclusivesLoaded)
                RenderExclusivesList();
        }

        private bool EnsureAddOnsFolderReady()
        {
            if (string.IsNullOrWhiteSpace(addOnsPath))
            {
                MessageBox.Show(this, "Select the JasonWoW AddOns folder first.", "Addon Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void UpdateSummaryCards()
        {
            var addons = manifest?.Addons ?? new List<InstalledAddon>();
            int managed = addons.Count(a => !a.FoundOnDisk && (!string.IsNullOrWhiteSpace(a.Repo) || a.IsExclusive));
            int updates = (lvAddons?.Items.Cast<ListViewItem>().Count(i => i.SubItems.Count > 3 && i.SubItems[3].Text.StartsWith("Update:", StringComparison.OrdinalIgnoreCase)) ?? 0)
                + (lvExclusives?.Items.Cast<ListViewItem>().Count(i => i.SubItems.Count > 2 && i.SubItems[2].Text.StartsWith("Update:", StringComparison.OrdinalIgnoreCase)) ?? 0);
            lInstalledSummary.Text = addons.Count + " INSTALLED";
            lManagedSummary.Text = managed + " MANAGED";
            lUpdatesSummary.Text = updates == 0 ? "NO KNOWN UPDATES" : updates + " UPDATE" + (updates == 1 ? string.Empty : "S");
            lUpdatesSummary.ForeColor = updates > 0 ? WowTheme.AccentHot : ModernMutedText;
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
                    AddonManager.ReplaceAddonForFolders(manifest, new InstalledAddon
                    {
                        Name = displayName ?? folders.FirstOrDefault() ?? ownerRepo,
                        Repo = ownerRepo,
                        Version = version,
                        Folders = folders
                    });
                    AddonManager.SaveManifest(addOnsPath, manifest);

                    RefreshAddonList();
                    RefreshCatalogSelectionButtonText();
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
                UpdateSummaryCards();
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
