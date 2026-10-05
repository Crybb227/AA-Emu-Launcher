using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AAEmu.Launcher
{
    public sealed class HawkSkaterForm : Form
    {
        readonly Button primary = new Button();
        readonly Label summary = new Label();
        readonly TextBox status = new TextBox();
        readonly FlowLayoutPanel actions = new FlowLayoutPanel();
        readonly ProgressBar progress = new ProgressBar();
        readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JasonGamesLauncher", "hawk-windows.json");
        readonly HawkDeployment deployment;
        HawkNativeService game;
        string root, token = "";
        bool busy;
        public IWin32Window DialogOwner { get; set; }
        IWin32Window UiOwner { get { return DialogOwner ?? this; } }
        public event Action StateChanged;
        public event Action<string, int> StatusChanged;
        public bool IsBusy { get { return busy; } }
        public bool IsRunning { get { return game.IsRunning; } }
        public bool IsInstalled { get { return game.IsInstalled; } }
        public bool AssetsReady { get { return game.HasWow && game.HasSkate; } }
        public string InstallRoot { get { return root; } }
        public string ClientExecutable { get { return game.IsInstalled ? Path.Combine(game.ReleaseDirectory, "bin", "benilla.exe") : ""; } }
        public Task PlayFromDashboard() { return Perform(InstallPlay); }
        public Task SettingsFromDashboard() { return Perform(Settings); }
        public Task AssetsFromDashboard() { return Perform(async () => { await AssetWizard(); }); }
        public Task AccountFromDashboard() { return Perform(Account); }
        CancellationTokenSource operation;
        public HawkSkaterForm() : this(false) { }
        public HawkSkaterForm(bool diagnostic)
        {
            Text = "JasonHawkSkater"; ClientSize = new Size(720, 460); MinimumSize = new Size(640, 460);
            BackColor = WowTheme.Back; ForeColor = WowTheme.Text; Font = new Font("Segoe UI", 10F); StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, Padding = new Padding(24) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 7; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "JasonHawkSkater", Font = new Font("Segoe UI", 25F, FontStyle.Bold), ForeColor = WowTheme.AccentHot, AutoSize = true }, 0, 0);
            layout.Controls.Add(new Label { Text = "Skate through Azeroth. Native Windows client.", AutoSize = true, Margin = new Padding(3, 4, 3, 15) }, 0, 1);
            summary.AutoSize = true; summary.Margin = new Padding(3, 3, 3, 12); layout.Controls.Add(summary, 0, 2);
            primary.Text = "Install / Play"; primary.Width = 240; primary.Height = 52; primary.Font = new Font("Segoe UI", 14F, FontStyle.Bold); Style(primary);
            primary.Click += async (s, e) => await Perform(InstallPlay); layout.Controls.Add(primary, 0, 3);
            progress.Dock = DockStyle.Fill; progress.Height = 16; progress.Margin = new Padding(3, 12, 3, 8); layout.Controls.Add(progress, 0, 4);
            layout.Controls.Add(new Label { Text = "J: board    K: camera    WASD: skating    Space: ollie\nArrows: tricks    R: recover    Xbox / XInput supported\nServer startup is through the Discord helper only.", AutoSize = true, Margin = new Padding(3, 5, 3, 10) }, 0, 5);
            actions.AutoSize = true; actions.Dock = DockStyle.Fill; layout.Controls.Add(actions, 0, 6);
            ActionButton("Settings", Settings); ActionButton("Game account", Account);
            ActionButton("Import assets", async () => { await AssetWizard(); }); ActionButton("Discord helper", OpenDiscord);
            status.Multiline = true; status.ReadOnly = true; status.ScrollBars = ScrollBars.Vertical; status.Dock = DockStyle.Fill; status.BackColor = WowTheme.PanelAlt; status.ForeColor = WowTheme.Text;
            layout.Controls.Add(status, 0, 7);
            var content = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HawkSkater");
            deployment = HawkDeployment.Load(Path.Combine(content, "deployment.json"));
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JasonGamesLauncher", "Games", "JasonHawkSkater");
            if (!diagnostic && File.Exists(settings))
                try { var saved = JsonConvert.DeserializeObject<LocalSettings>(File.ReadAllText(settings)); if (saved != null && !string.IsNullOrEmpty(saved.root)) root = saved.root; }
                catch (Exception) { Log("Saved install location could not be read; using the Windows default.", 0); }
            if (!diagnostic) token = HawkCredential.Read("JasonGamesLauncher/JasonHawkSkater/windows-access");
            game = new HawkNativeService(root, Path.Combine(content, "Release"), Path.Combine(content, "wow-5875.json"), deployment, Log);
            RefreshState();
            FormClosing += (s, e) => { if (busy) { e.Cancel = true; MessageBox.Show(this, game.IsRunning ? "Exit the game before closing the launcher." : "Wait for the current operation to finish before closing.", "JasonHawkSkater"); } };
        }
        static void Style(Button button) { button.BackColor = WowTheme.Panel; button.ForeColor = WowTheme.AccentHot; button.FlatStyle = FlatStyle.Flat; button.Margin = new Padding(3, 3, 8, 3); }
        void ActionButton(string title, Func<Task> action)
        {
            var b = new Button { Text = title, AutoSize = true, Padding = new Padding(6) }; Style(b); actions.Controls.Add(b);
            b.Click += async (s, e) => await Perform(action);
        }
        void RefreshState()
        {
            primary.Text = game.IsInstalled ? "Play" : "Install / Play";
            summary.Text = !game.IsInstalled ? "Ready to install. Required assets are installed automatically." : !game.HasWow || !game.HasSkate ? "Click Install / Play to finish installing required assets." : "Ready to play. Start the server through Discord first.";
        }
        void Log(string text, int value)
        {
            if (DialogOwner != null) { StatusChanged?.Invoke(text, value); return; }
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => Log(text, value))); return; }
            status.AppendText(text + Environment.NewLine); progress.Value = Math.Max(0, Math.Min(100, value));
        }
        async Task Perform(Func<Task> action)
        {
            if (busy) return; busy = true; StateChanged?.Invoke(); primary.Enabled = false; actions.Enabled = false; operation = new CancellationTokenSource();
            try { await action(); }
            catch (OperationCanceledException) { Log("Cancelled. Verified downloads can resume next time.", 0); }
            catch (HawkAccessDeniedException ex) { token = ""; HawkCredential.Write("JasonGamesLauncher/JasonHawkSkater/windows-access", ""); Log(ex.Message, 0); }
            catch (Exception ex) { Log(ex.Message, 0); }
            finally { operation.Dispose(); operation = null; busy = false; primary.Enabled = true; actions.Enabled = true; RefreshState(); StateChanged?.Invoke(); }
        }
        async Task EnsureSignIn()
        {
            if (game.RequiresSignIn && string.IsNullOrEmpty(token))
            {
                token = await game.SignIn(operation.Token); HawkCredential.Write("JasonGamesLauncher/JasonHawkSkater/windows-access", token);
                Log("Signed in. Access is stored in Windows Credential Manager.", 0);
            }
        }
        async Task InstallPlay()
        {
            await EnsureSignIn(); Log("Checking for client updates…", 0);
            await Task.Run(() => game.Install(token, operation.Token));
            await Task.Run(() => game.InstallSkateContent(token, operation.Token));
            if (!game.HasWow) await Task.Run(() => game.DownloadWowAssets(operation.Token));
            if ((!game.HasWow || !game.HasSkate) && !await AssetWizard()) return;
            var account = ReadLogin(); await Task.Run(() => game.Launch(token, account.user, account.password, operation.Token));
        }
        HawkLogin ReadLogin()
        {
            var saved = HawkCredential.Read("JasonGamesLauncher/JasonHawkSkater/game");
            try { return JsonConvert.DeserializeObject<HawkLogin>(saved) ?? new HawkLogin(); } catch (JsonException) { return new HawkLogin { password = saved }; }
        }
        Task Account()
        {
            var login = ReadLogin();
            using (var dialog = Dialog("Game account", new Size(440, 230)))
            {
                var account = new TextBox { Text = login.user, Dock = DockStyle.Top };
                var password = new TextBox { Text = login.password, UseSystemPasswordChar = true, Dock = DockStyle.Top };
                var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(16) };
                panel.Controls.Add(new Label { Text = "Optional: save your game login on this PC.", AutoSize = true }); panel.Controls.Add(account); panel.Controls.Add(new Label { Text = "Password", AutoSize = true }); panel.Controls.Add(password);
                var save = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true }; Style(save); panel.Controls.Add(save); dialog.Controls.Add(panel); dialog.AcceptButton = save;
                if (dialog.ShowDialog(UiOwner) == DialogResult.OK) HawkCredential.Write("JasonGamesLauncher/JasonHawkSkater/game", JsonConvert.SerializeObject(new HawkLogin { user = account.Text.Trim(), password = password.Text }));
            }
            return Task.CompletedTask;
        }
        static Form Dialog(string title, Size size) { return new Form { Text = title, ClientSize = size, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog, BackColor = WowTheme.Back, ForeColor = WowTheme.Text, Font = new Font("Segoe UI", 10F) }; }
        async Task<bool> AssetWizard(bool replaceWow = false, bool replaceSkate = false)
        {
            if (!game.IsInstalled) { Log("Click Install / Play to install the client first.", 0); return false; }
            if (!game.HasWow || replaceWow)
            {
                var choice = MessageBox.Show(UiOwner, "Import your own English WoW 1.12.1 assets.\n\nYes: choose your existing ZIP.\nNo: choose an installed Data folder.\nCancel: finish later.\n\nInstall / Play downloads and validates the Stonetavern Classic 1.8 ZIP automatically.", "Your WoW assets", MessageBoxButtons.YesNoCancel);
                string input = null;
                if (choice == DialogResult.Yes) using (var picker = new OpenFileDialog { Filter = "WoW client ZIP|*.zip" }) { if (picker.ShowDialog(UiOwner) == DialogResult.OK) input = picker.FileName; }
                else if (choice == DialogResult.No) using (var picker = new FolderBrowserDialog { Description = "Choose your WoW Data folder" }) { if (picker.ShowDialog(UiOwner) == DialogResult.OK) input = picker.SelectedPath; }
                if (input == null) return false; await Task.Run(() => game.ImportWow(input, operation.Token));
            }
            if (!game.HasSkate || replaceSkate)
            {
                MessageBox.Show(UiOwner, "Choose your own extracted Skate 3 folder, or a folder containing your previously converted skate-data and skate-audio.\n\nNo Skate 3 content is included in the WoW download or launcher.", "Your Skate 3 assets");
                using (var picker = new FolderBrowserDialog { Description = "Choose your own Skate 3 content" })
                { if (picker.ShowDialog(UiOwner) != DialogResult.OK) return false; await Task.Run(() => game.ImportSkate(picker.SelectedPath, operation.Token)); }
            }
            Log("Asset setup complete. Future launches are one click.", 100); return true;
        }
        Task OpenDiscord()
        {
            if (string.IsNullOrEmpty(deployment.discordUrl)) Log("In your Discord server, use /wake skatecraft or the Start JasonHawkSkater helper button. An invite link has not been configured yet.", 0);
            else Process.Start(new ProcessStartInfo(deployment.discordUrl) { UseShellExecute = true }); return Task.CompletedTask;
        }
        async Task Settings()
        {
            bool repair = false;
            using (var dialog = Dialog("JasonHawkSkater settings", new Size(570, 320)))
            {
                var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(16), WrapContents = false };
                panel.Controls.Add(new Label { Text = "Installation: " + root, AutoSize = true, MaximumSize = new Size(520, 0) });
                Func<string, Action, Button> add = (title, action) => { var b = new Button { Text = title, AutoSize = true }; Style(b); panel.Controls.Add(b); b.Click += (s, e) => { action(); dialog.Close(); }; return b; };
                add("Change installation location…", () => {
                    using (var picker = new FolderBrowserDialog { Description = "Choose a Windows game installation folder" })
                    {
                        if (picker.ShowDialog(UiOwner) != DialogResult.OK) return;
                        var folder = Path.Combine(picker.SelectedPath, "JasonHawkSkater"); var content = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HawkSkater");
                        game = new HawkNativeService(folder, Path.Combine(content, "Release"), Path.Combine(content, "wow-5875.json"), deployment, Log); root = folder;
                        Directory.CreateDirectory(Path.GetDirectoryName(settings)); File.WriteAllText(settings, JsonConvert.SerializeObject(new LocalSettings { root = root }));
                        Log("Install location changed. Your previous installation was not moved or deleted.", 0);
                    }
                });
                add("Repair client and check assets", () => { repair = true; }).Enabled = game.IsInstalled;
                add("Game account…", () => { dialog.Tag = "account"; });
                add("Replace imported assets…", () => { dialog.Tag = "assets"; }).Enabled = game.IsInstalled;
                add("Get WoW 1.12.1 assets", () => Process.Start(new ProcessStartInfo("https://www.stonetavern.app/clients") { UseShellExecute = true }));
                add("Sign out of launcher access", () => { token = ""; HawkCredential.Write("JasonGamesLauncher/JasonHawkSkater/windows-access", ""); Log("Signed out. Game account credentials were preserved.", 0); });
                panel.Controls.Add(new Label { Text = "Downloaded files are inspectable locally. Revocation prevents future downloads and sessions.", AutoSize = true, MaximumSize = new Size(520, 0) });
                dialog.AutoScroll = true; dialog.Controls.Add(panel); dialog.ShowDialog(UiOwner);
                if ((string)dialog.Tag == "account") { await Account(); return; }
                if ((string)dialog.Tag == "assets") { await AssetWizard(true, true); return; }
            }
            if (repair) { await EnsureSignIn(); await Task.Run(() => game.Install(token, operation.Token, true)); await Task.Run(() => game.InstallSkateContent(token, operation.Token, true)); if (game.HasWow && game.HasSkate) await Task.Run(() => game.CheckAssets()); else Log("Client repaired. Import missing assets to play.", 100); }
        }
        sealed class LocalSettings { public string root; }
        sealed class HawkLogin { public string user = "", password = ""; }
    }
    internal static class HawkCredential
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct Credential { public int flags, type; public string target, comment; public long written; public int size; public IntPtr blob; public int persist, count; public IntPtr attributes; public string alias, user; }
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool WriteNative(ref Credential c, int flags);
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode)] static extern bool ReadNative(string target, int type, int flags, out IntPtr ptr);
        [DllImport("advapi32.dll")] static extern void CredFree(IntPtr ptr);
        public static void Write(string target, string secret) { var bytes = Encoding.Unicode.GetBytes(secret); var ptr = Marshal.AllocHGlobal(Math.Max(1, bytes.Length)); try { Marshal.Copy(bytes, 0, ptr, bytes.Length); var c = new Credential { type = 1, target = target, size = bytes.Length, blob = ptr, persist = 2, user = "JasonHawkSkater" }; if (!WriteNative(ref c, 0)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); } finally { Marshal.FreeHGlobal(ptr); } }
        public static string Read(string target) { IntPtr ptr; if (!ReadNative(target, 1, 0, out ptr)) return ""; try { var c = (Credential)Marshal.PtrToStructure(ptr, typeof(Credential)); return Marshal.PtrToStringUni(c.blob, c.size / 2); } finally { CredFree(ptr); } }
    }
}
