using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    public partial class LauncherForm
    {
        private HawkSkaterForm hawkController;
        private Label hawkActivity;
        private ProgressBar hawkProgress;
        private string hawkStatus = "Install / Play downloads and installs the client and required assets automatically.";
        private int hawkPercent;
        private bool IsHawkSelected => selectedGameId == "hawk";
        private HawkSkaterForm Hawk
        {
            get
            {
                if (hawkController != null) return hawkController;
                // Reuse the native workflow, but never show its standalone prototype UI.
                hawkController = new HawkSkaterForm { DialogOwner = this };
                hawkController.StateChanged += () => { if (IsHawkSelected) UpdateHawkDashboard(); };
                hawkController.StatusChanged += (text, percent) =>
                {
                    if (IsDisposed) return;
                    Action update = () => { if (hawkStatus == text && hawkPercent == percent) return; hawkStatus = text; hawkPercent = percent; if (IsHawkSelected) UpdateHawkDashboard(); };
                    if (InvokeRequired) BeginInvoke(update); else update();
                };
                hawkActivity = new Label { ForeColor = WowTheme.Text, BackColor = WowTheme.Panel, Padding = new Padding(14), AutoEllipsis = true };
                hawkProgress = new ProgressBar();
                Controls.Add(hawkActivity); Controls.Add(hawkProgress);
                FormClosing += (s, e) => { if (hawkController.IsBusy) { e.Cancel = true; MessageBox.Show(this, "Wait for installation to finish, or exit the game before closing the launcher.", "JasonHawkSkater"); } };
                FormClosed += (s, e) => hawkController.Dispose();
                return hawkController;
            }
        }
        private void SetHawkVisibility(bool visible)
        {
            if (hawkActivity != null) hawkActivity.Visible = visible;
            if (hawkProgress != null) hawkProgress.Visible = visible;
        }
        private void UpdateHawkPlayButton()
        {
            var game = Hawk;
            btnPlay.Text = game.IsRunning ? "PLAYING" : game.IsBusy ? "INSTALLING / CHECKING…" : game.IsInstalled ? "PLAY" : "INSTALL / PLAY";
            btnPlay.Enabled = !game.IsBusy;
            btnPlay.Cursor = game.IsBusy ? Cursors.WaitCursor : Cursors.Hand;
            ApplyModernPlayButton(serverCheckStatus, false);
        }
        private void UpdateHawkDashboard()
        {
            var game = Hawk; bool home = currentPanel == ShowPanelType.Login;
            SetHawkVisibility(home);
            lHeroTitle.Text = "JASON HAWK\nSKATER"; if (lHeroTitle.Font.Size != 17F) lHeroTitle.Font = new Font("Palatino Linotype", 17F, FontStyle.Bold);
            lGameKicker.Text = "NATIVE WINDOWS · x64"; lGameKicker.Visible = home;
            lRealmStatus.Text = "SERVER START: EXTERNAL DISCORD"; lRealmStatus.ForeColor = WowTheme.MutedText;
            lRealmProgression.Text = "J: board on/off  ·  K: camera";
            lPopulation.Text = "Keyboard + Xbox / XInput";
            lPopulation.Cursor = Cursors.Default;
            dashboardToolTip.SetToolTip(lPopulation, "W/S: push/brake · A/D: steer · hold/release Space: ollie\nArrows: trick stick · R: recover · J: board · K: camera\nKeyboard skating pauses while typing or unfocused.\nRemap in benilla-config/skate-keyboard.toml while the game is closed.");
            lRealmStatus.Visible = lRealmProgression.Visible = lPopulation.Visible = home;
            btnPlay.SetBounds(Px(28), Px(333), Px(242), Px(52));
            lClientStatus.SetBounds(Px(28), Px(394), Px(242), Px(28));
            lClientStatus.Text = game.IsBusy ? "Working… see installation progress" : !game.IsInstalled ? "Client not installed" : !game.AssetsReady ? "Asset setup required" : "✓ Client ready · Check for updates";
            lClientStatus.Visible = home;
            rowAddons.Heading.Text = "SKATE 3 ASSETS"; rowAddons.Value.Text = game.AssetsReady ? "Manage content" : "Automatic setup · optional import";
            rowAddons.SetBounds(Px(28), Px(434), Px(242), Px(55)); rowAddons.Visible = home; rowAddons.Enabled = !game.IsBusy;
            rowGameLocation.SetBounds(Px(28), Px(497), Px(242), Px(55)); rowGameLocation.Visible = home; rowGameLocation.Enabled = !game.IsBusy;
            rowGameLocation.Value.Text = game.InstallRoot;
            hawkActivity.SetBounds(Px(330), Px(438), Px(536), Px(78)); hawkActivity.Text = hawkStatus;
            hawkProgress.SetBounds(Px(330), Px(522), Px(536), Px(16)); hawkProgress.Value = Math.Max(0, Math.Min(100, hawkPercent));
            lHeroNewsTitle.Text = "JasonHawkSkater";
            lHeroNewsBody.Text = "W/S: push/brake · A/D: steer\nSpace: ollie · arrows: tricks\nR: recover · J: board · K: camera\nXbox / XInput controllers supported.";
            lHeroNewsTitle.Tag = lHeroNewsBody.Tag = null;
            lNewsPrevious.Visible = lNewsNext.Visible = lNewsPosition.Visible = false;
            lLauncherVersionCompact.Visible = home;
            UpdateHawkPlayButton();
            foreach (var c in new Control[] { lRealmStatus, lRealmProgression, lPopulation, rowAddons, rowGameLocation, lClientStatus, hawkActivity, hawkProgress }) c.BringToFront();
        }
    }
}
