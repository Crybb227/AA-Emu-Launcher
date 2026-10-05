using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    internal sealed class RealmStatusPayload
    {
        [JsonProperty("online")] public bool? Online { get; set; }
        [JsonProperty("phase")] public string Phase { get; set; }
        [JsonProperty("levelCap")] public int? LevelCap { get; set; }
        [JsonProperty("humanPlayers")] public List<RealmPlayerPayload> HumanPlayers { get; set; }
        [JsonProperty("playerbotCount")] public int? PlayerbotCount { get; set; }
    }

    internal sealed class RealmPlayerPayload
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("race")] public string Race { get; set; }
        [JsonProperty("class")] public string Class { get; set; }
        [JsonProperty("level")] public int? Level { get; set; }
        [JsonProperty("location")] public string Location { get; set; }
    }

    internal sealed class DashboardRow : Panel
    {
        internal readonly Label Heading = new Label();
        internal readonly Label Value = new Label();
        internal readonly Label Arrow = new Label();
        internal DashboardRow(string heading)
        {
            DoubleBuffered = true; BackColor = WowTheme.Panel; Cursor = Cursors.Hand; TabStop = true;
            Heading.Text = heading.ToUpperInvariant(); Heading.ForeColor = WowTheme.MutedText;
            Heading.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold); Heading.SetBounds(14, 7, 190, 17);
            Value.ForeColor = WowTheme.Text; Value.Font = new Font("Segoe UI", 9.5F); Value.SetBounds(14, 25, 190, 24); Value.AutoEllipsis = true;
            Arrow.Text = "›"; Arrow.ForeColor = WowTheme.AccentHot; Arrow.Font = new Font("Segoe UI", 18F); Arrow.SetBounds(212, 11, 22, 34);
            Controls.AddRange(new Control[] { Heading, Value, Arrow });
            foreach (Control child in Controls) { child.BackColor = Color.Transparent; child.Cursor = Cursors.Hand; child.Click += (s, e) => OnClick(e); }
        }
    }

    public partial class LauncherForm
    {
        private Label lGameKicker, lRealmStatus, lRealmProgression, lPopulation, lClientStatus, lLauncherVersionCompact, lLauncherUpdateCompact;
        private Label lNewsPrevious, lNewsNext, lNewsPosition;
        private DashboardRow rowAddons, rowGameLocation;
        private ContextMenuStrip gameLocationMenu;
        private ToolTip dashboardToolTip;
        private RealmStatusPayload realmDetails;
        private CancellationTokenSource realmStatusCancellation;
        private bool realmStatusRequestInFlight;
        private DateTime lastRealmDetailsCheckUtc = DateTime.MinValue;

        private void InitializeDashboard()
        {
            dashboardToolTip = new ToolTip(components) { AutoPopDelay = 7000, InitialDelay = 450, ReshowDelay = 100 };
            lGameKicker = DashboardLabel("PRIVATE SERVER", 8.5F, FontStyle.Bold, WowTheme.MutedText);
            lGameKicker.TextAlign = ContentAlignment.MiddleCenter;
            lRealmStatus = DashboardLabel("●  REALM STATUS UNAVAILABLE", 9.5F, FontStyle.Bold, WowTheme.MutedText);
            lRealmProgression = DashboardLabel("Progression data unavailable", 9F, FontStyle.Regular, WowTheme.MutedText);
            lPopulation = DashboardLabel("Player data unavailable", 9.5F, FontStyle.Regular, WowTheme.Text);
            lPopulation.Cursor = Cursors.Hand; lPopulation.Click += (s, e) => ShowPlayerPopover();
            lClientStatus = DashboardLabel("Checking client…", 9F, FontStyle.Regular, WowTheme.MutedText);
            lClientStatus.Cursor = Cursors.Hand; lClientStatus.Click += (s, e) => { if (IsHawkSelected) _ = Hawk.PlayFromDashboard(); else LClientUpdateAction_Click(s, e); };
            rowAddons = new DashboardRow("Addons"); rowAddons.Click += (s, e) => { if (IsHawkSelected) _ = Hawk.AssetsFromDashboard(); else LAddons_Click(s, e); };
            rowGameLocation = new DashboardRow("Game location"); rowGameLocation.Click += (s, e) => ShowGameLocationMenu();
            lLauncherVersionCompact = DashboardLabel("Launcher", 8.5F, FontStyle.Regular, WowTheme.MutedText);
            lLauncherUpdateCompact = DashboardLabel(string.Empty, 8.5F, FontStyle.Bold, WowTheme.AccentHot);
            lLauncherUpdateCompact.Cursor = Cursors.Hand; lLauncherUpdateCompact.Click += LDownloadLauncherUpdate_Click;
            lNewsPrevious = DashboardLabel("‹", 14F, FontStyle.Bold, WowTheme.MutedText);
            lNewsNext = DashboardLabel("›", 14F, FontStyle.Bold, WowTheme.MutedText);
            lNewsPosition = DashboardLabel(string.Empty, 8F, FontStyle.Regular, WowTheme.MutedText);
            lNewsPrevious.TextAlign = ContentAlignment.MiddleCenter; lNewsNext.TextAlign = ContentAlignment.MiddleCenter; lNewsPosition.TextAlign = ContentAlignment.MiddleCenter;
            lNewsPrevious.Cursor = lNewsNext.Cursor = Cursors.Hand;
            lNewsPrevious.Click += (s, e) => StepHeroNews(-1); lNewsNext.Click += (s, e) => StepHeroNews(1);
            lHeroNewsTitle.Cursor = lHeroNewsBody.Cursor = Cursors.Hand;
            lHeroNewsTitle.Click += OpenHeroNews; lHeroNewsBody.Click += OpenHeroNews;
            dashboardToolTip.SetToolTip(lPopulation, "View human players reported by the JasonWoW status service");
            dashboardToolTip.SetToolTip(lClientStatus, "Click to manually check for client updates");
            dashboardToolTip.SetToolTip(rowGameLocation, "Open or change this game's installation location");
            Controls.AddRange(new Control[] { lGameKicker, lRealmStatus, lRealmProgression, lPopulation, lClientStatus, rowAddons, rowGameLocation, lLauncherVersionCompact, lLauncherUpdateCompact, lNewsPrevious, lNewsNext, lNewsPosition });
            BuildGameLocationMenu(); LayoutDashboard(); UpdateDashboardForSelectedGame();
        }

        private Label DashboardLabel(string text, float size, FontStyle style, Color color) => new Label
        { AutoSize = false, BackColor = Color.Transparent, Text = text, Font = new Font("Segoe UI", size, style), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft };

        private void LayoutDashboard()
        {
            lGameKicker.SetBounds(28, 207, 242, 22); lRealmStatus.SetBounds(28, 244, 242, 24); lRealmProgression.SetBounds(28, 267, 242, 21); lPopulation.SetBounds(28, 291, 242, 26);
            btnPlay.SetBounds(28, 333, 242, 52); lClientStatus.SetBounds(28, 394, 242, 28);
            rowAddons.SetBounds(28, 434, 242, 55); rowGameLocation.SetBounds(28, 497, 242, 55);
            lLauncherUpdateCompact.SetBounds(28, 568, 242, 22); lLauncherVersionCompact.SetBounds(28, 590, 242, 22);
            lHeroTitle.SetBounds(28, 156, 242, 55); lHeroTitle.Font = new Font("Palatino Linotype", 22F, FontStyle.Bold); lHeroTitle.TextAlign = ContentAlignment.MiddleCenter;
            lHeroNewsTitle.SetBounds(904, 570, 292, 28); lHeroNewsBody.SetBounds(904, 603, 292, 76);
            lHeroNewsBody.Font = new Font("Segoe UI", 10.5F);
            lNewsPrevious.SetBounds(1118, 674, 24, 24); lNewsPosition.SetBounds(1142, 674, 48, 24); lNewsNext.SetBounds(1190, 674, 24, 24);
        }

        private void BuildGameLocationMenu()
        {
            gameLocationMenu = new ContextMenuStrip(components); StyleContextMenu(gameLocationMenu);
            gameLocationMenu.Items.Add("Open folder", null, (s, e) => OpenSelectedGameFolder());
            gameLocationMenu.Items.Add("Change location…", null, lGamePath_Click);
            gameLocationMenu.Items.Add("Locate existing installation…", null, lGamePath_Click);
        }

        private void ShowGameLocationMenu() { if (IsHawkSelected) { _ = Hawk.SettingsFromDashboard(); return; } gameLocationMenu.Items[0].Enabled = IsGameInstalled(); gameLocationMenu.Show(rowGameLocation, new Point(0, rowGameLocation.Height)); }
        private void OpenSelectedGameFolder() { var p = GetSelectedGamePath(); if (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) Process.Start("explorer.exe", "/select,\"" + p + "\""); }

        private void UpdateDashboardForSelectedGame()
        {
            if (lRealmStatus == null) return;
            if (IsHawkSelected) { UpdateHawkDashboard(); return; }
            SetHawkVisibility(false); rowAddons.Heading.Text = "ADDONS"; rowAddons.Enabled = rowGameLocation.Enabled = true;
            lPopulation.Cursor = Cursors.Hand; lHeroTitle.Font = new Font("Palatino Linotype", 22F, FontStyle.Bold);
            dashboardToolTip.SetToolTip(lPopulation, "View human players reported by the JasonWoW status service");
            bool wow = selectedGameId == "jw", home = currentPanel == ShowPanelType.Login;
            if (wow)
            {
                btnPlay.SetBounds(Px(28), Px(333), Px(242), Px(52));
                lClientStatus.SetBounds(Px(28), Px(394), Px(242), Px(28));
                rowAddons.SetBounds(Px(28), Px(434), Px(242), Px(55));
                rowGameLocation.SetBounds(Px(28), Px(497), Px(242), Px(55));
            }
            else
            {
                btnPlay.SetBounds(Px(28), Px(414), Px(242), Px(52));
                lClientStatus.SetBounds(Px(28), Px(474), Px(242), Px(28));
                rowGameLocation.SetBounds(Px(28), Px(514), Px(242), Px(55));
            }
            lRealmStatus.Visible = lRealmProgression.Visible = lPopulation.Visible = rowAddons.Visible = wow && home;
            rowGameLocation.Visible = lClientStatus.Visible = lLauncherVersionCompact.Visible = home;
            lLauncherUpdateCompact.Visible = home && !string.IsNullOrWhiteSpace(LauncherUpdateVersion);
            lHeroTitle.Text = wow ? "JASONWOW" : "ARCHEAGE";
            lGameKicker.Text = wow ? "PRIVATE SERVER" : selectedGameId == "aa30" ? "VERSION 3.0.3" : "VERSION 1.2";
            lGameKicker.Visible = home;
            rowGameLocation.Value.Text = IsGameInstalled() ? Path.GetDirectoryName(GetSelectedGamePath()) : "Not installed — locate or install";
            rowGameLocation.Value.ForeColor = IsGameInstalled() ? WowTheme.Text : WowTheme.AccentHot;
            UpdateAddonSummary(); UpdateClientStatusSummary(); UpdateRunningState();
            if (wow) _ = RefreshRealmDetailsAsync(false);
            RefreshHeroNewsNavigation();

            foreach (var control in new Control[] { lGameKicker, lRealmStatus, lRealmProgression, lPopulation, lClientStatus,
                rowAddons, rowGameLocation, lLauncherUpdateCompact, lLauncherVersionCompact, lNewsPrevious, lNewsPosition, lNewsNext })
                control.BringToFront();
        }

        private void RefreshHeroNewsNavigation()
        {
            int count = newsFeed?.Data?.Count ?? 0;
            bool visible = !IsHawkSelected && currentPanel == ShowPanelType.Login && count > 1;
            lNewsPrevious.Visible = lNewsNext.Visible = lNewsPosition.Visible = visible;
            if (visible) lNewsPosition.Text = (Math.Max(0, bigNewsIndex) + 1) + " / " + count;
        }

        private void StepHeroNews(int direction)
        {
            int count = newsFeed?.Data?.Count ?? 0; if (count < 2) return;
            bigNewsIndex = (Math.Max(0, bigNewsIndex) + direction + count) % count;
            UpdateHeroNewsCard(newsFeed.Data[bigNewsIndex]);
            RefreshHeroNewsNavigation();
            bigNewsTimer = 60000;
        }

        private void UpdateHeroNewsCard(AAEmuNewsFeedDataItem item)
        {
            lHeroNewsTitle.Text = item.ItemAttributes.ItemTitle ?? "Latest News";
            var body = item.ItemAttributes.ItemBody ?? string.Empty;
            body = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", string.Empty).Replace("\\r", "").Replace("\\n", Environment.NewLine);
            lHeroNewsBody.Text = body.Length > 180 ? body.Substring(0, 177) + "…" : body;
            lHeroNewsTitle.Tag = lHeroNewsBody.Tag = item.ItemAttributes.ItemLinks.Self;
        }

        private void OpenHeroNews(object sender, EventArgs e)
        {
            var link = (sender as Control)?.Tag as string;
            if (!string.IsNullOrWhiteSpace(link)) Process.Start(link);
        }

        private void UpdateClientStatusSummary()
        {
            if (IsHawkSelected) return;
            if (lClientStatus == null) return;
            lClientStatus.Text = isClientDeltaUpdating ? "↻ Updating client…" : !IsGameInstalled() ? "Game installation not found" :
                serverCheckStatus == serverCheck.Update ? "Update available  ·  Check now" : "✓ Client ready  ·  Check for updates";
        }

        private void UpdateAddonSummary()
        {
            if (rowAddons == null) return; int count = 0; var path = GetDefaultWoWAddOnsPath();
            try { count = AddonManager.GetInstalledAddons(path).Addons.Count; } catch { }
            var updates = pendingExclusiveAddonUpdates > 0 ? " • " + pendingExclusiveAddonUpdates + " update" + (pendingExclusiveAddonUpdates == 1 ? "" : "s") : string.Empty;
            rowAddons.Value.Text = count + " installed" + updates;
        }

        private bool IsSelectedGameRunning()
        {
            try { if (selectedGameId == "jw" && jasonWoWProcess != null) return !jasonWoWProcess.HasExited; return IsArcheAgeSelected && aaLauncher?.RunningProcess != null && !aaLauncher.RunningProcess.HasExited; }
            catch { return false; }
        }

        private void UpdateRunningState()
        {
            if (IsHawkSelected) { UpdateHawkPlayButton(); return; }
            if (btnPlay == null) return;
            if (IsSelectedGameRunning()) { btnPlay.Text = "PLAYING"; btnPlay.Enabled = false; btnPlay.Cursor = Cursors.No; }
            else if (!btnPlay.Enabled) { btnPlay.Enabled = true; UpdatePlayButton(serverCheckStatus, false); }
        }

        private async Task RefreshRealmDetailsAsync(bool force)
        {
            if (selectedGameId != "jw" || realmStatusRequestInFlight) return;
            if (!force && (DateTime.UtcNow - lastRealmDetailsCheckUtc).TotalSeconds < 120) return;
            lastRealmDetailsCheckUtc = DateTime.UtcNow; realmStatusRequestInFlight = true;
            realmStatusCancellation?.Cancel(); realmStatusCancellation?.Dispose(); realmStatusCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                if (string.IsNullOrWhiteSpace(Setting.WoWStatusUrl)) { ApplyRealmDetails(null); return; }
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                using (var response = await client.GetAsync(Setting.WoWStatusUrl, realmStatusCancellation.Token))
                { response.EnsureSuccessStatusCode(); ApplyRealmDetails(JsonConvert.DeserializeObject<RealmStatusPayload>(await response.Content.ReadAsStringAsync())); }
            }
            catch (Exception ex) { Trace.WriteLine("JasonWoW status service unavailable: " + ex.Message); ApplyRealmDetails(null); }
            finally { realmStatusRequestInFlight = false; }
        }

        private void ApplyRealmDetails(RealmStatusPayload payload)
        {
            if (IsDisposed || lRealmStatus == null) return; realmDetails = payload;
            if (IsHawkSelected) return;
            bool? online = payload?.Online ?? (serverCheckStatus == serverCheck.Online ? (bool?)true : serverCheckStatus == serverCheck.Offline ? false : (bool?)null);
            lRealmStatus.Text = online == true ? "●  REALM ONLINE" : online == false ? "●  REALM OFFLINE" : "●  REALM STATUS UNAVAILABLE";
            lRealmStatus.ForeColor = online == true ? Color.FromArgb(111, 210, 142) : online == false ? WowTheme.Danger : WowTheme.MutedText;
            var progression = new List<string>(); if (!string.IsNullOrWhiteSpace(payload?.Phase)) progression.Add(payload.Phase); if (payload?.LevelCap != null) progression.Add("Level Cap " + payload.LevelCap);
            lRealmProgression.Text = progression.Count == 0 ? "Progression data unavailable" : string.Join("  •  ", progression);
            if (payload?.HumanPlayers != null) { var n = payload.HumanPlayers.Count; lPopulation.Text = n + " " + (n == 1 ? "Player" : "Players") + " Online" + (payload.PlayerbotCount == null ? "" : "  •  " + payload.PlayerbotCount + " Adventurers"); }
            else lPopulation.Text = "Player data unavailable";
        }

        private void ShowPlayerPopover()
        {
            if (realmDetails?.HumanPlayers == null) { dashboardToolTip.Show("The configured status service did not provide human-player details.", lPopulation, 0, lPopulation.Height, 4000); return; }
            var lines = realmDetails.HumanPlayers.Take(12).Select(p => { var d = new[] { p.Race, p.Class, p.Level == null ? null : "Level " + p.Level, p.Location }.Where(v => !string.IsNullOrWhiteSpace(v)); return p.Name + (d.Any() ? Environment.NewLine + "  " + string.Join(" • ", d) : ""); });
            dashboardToolTip.Show(lines.Any() ? string.Join(Environment.NewLine + Environment.NewLine, lines) : "No human players are online.", lPopulation, 0, lPopulation.Height, 10000);
        }
    }
}
