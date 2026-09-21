using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    /// <summary>
    /// Small popup offering to quick-install an addon's recommended companion addons
    /// (e.g. NemesisTracker suggesting TomTom/HandyNotes), with a per-item install button and a Dismiss button.
    /// </summary>
    public class AddonDependencyPromptForm : Form
    {
        private readonly Color ModernBack = Color.FromArgb(12, 15, 21);
        private readonly Color ModernPanel = Color.FromArgb(29, 34, 45);
        private readonly Color ModernAccent = Color.FromArgb(72, 198, 169);
        private readonly Color ModernText = Color.FromArgb(235, 240, 246);
        private readonly Color ModernMutedText = Color.FromArgb(145, 157, 172);

        public IReadOnlyList<RecommendedDependency> InstalledDependencies => installedDependencies;
        private readonly List<RecommendedDependency> installedDependencies = new List<RecommendedDependency>();

        /// <summary>Raised when the user clicks Install on a dependency row; the caller performs the actual download/extract.</summary>
        public event Func<RecommendedDependency, System.Threading.Tasks.Task<bool>> InstallRequested;

        public AddonDependencyPromptForm(string forAddonName, IEnumerable<RecommendedDependency> recommendations)
        {
            var items = recommendations.ToList();

            Text = "Recommended Addons";
            ClientSize = new Size(460, 90 + items.Count * 56 + 56);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ModernBack;
            ForeColor = ModernText;
            Font = new Font("Segoe UI", 9.5F);

            var lHeader = new Label
            {
                Left = 16,
                Top = 14,
                Width = 428,
                Height = 48,
                Text = $"{forAddonName} works best with these addons. Install them now?",
                ForeColor = ModernText
            };
            Controls.Add(lHeader);

            var top = 68;
            foreach (var dep in items)
            {
                var row = BuildRow(dep, top);
                Controls.AddRange(row);
                top += 56;
            }

            var btnDismiss = new Button
            {
                Left = 16,
                Top = top + 8,
                Width = 120,
                Height = 30,
                Text = "Dismiss",
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnDismiss.Click += (s, e) => Close();
            Controls.Add(btnDismiss);
        }

        private Control[] BuildRow(RecommendedDependency dep, int top)
        {
            var lName = new Label { Left = 16, Top = top, Width = 200, Height = 22, Text = dep.Name, ForeColor = ModernText, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) };
            var lDesc = new Label { Left = 16, Top = top + 22, Width = 300, Height = 20, Text = dep.Description ?? string.Empty, ForeColor = ModernMutedText, Font = new Font("Segoe UI", 8.5F) };
            var btnInstall = new Button { Left = 330, Top = top, Width = 114, Height = 30, Text = "Install", FlatStyle = FlatStyle.Flat, BackColor = ModernAccent, ForeColor = Color.Black };

            btnInstall.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(dep.DownloadUrl))
                {
                    MessageBox.Show(this, $"{dep.Name} doesn't have a download link configured yet.", "Recommended Addons", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                btnInstall.Enabled = false;
                btnInstall.Text = "Installing...";
                var success = InstallRequested != null && await InstallRequested(dep);
                if (success)
                {
                    installedDependencies.Add(dep);
                    btnInstall.Text = "Installed";
                }
                else
                {
                    btnInstall.Enabled = true;
                    btnInstall.Text = "Install";
                }
            };

            return new Control[] { lName, lDesc, btnInstall };
        }
    }
}
