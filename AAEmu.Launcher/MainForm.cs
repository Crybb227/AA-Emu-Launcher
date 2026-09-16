using AAEmu.Launcher.Basic;
using AAEmu.Launcher.MailRu10;
using AAEmu.Launcher.Trion12;
using AAEmu.Launcher.Trion60;
using AAPacker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AAEmu.Launcher
{

    public partial class LauncherForm : Form
    {

        string AppVersion = "?.?.?.?";

        public enum ShowPanelType
        {
            Login = 0,
            Settings = 1,
            UpdatePatch = 2,
        }

        public enum LauncherOpenMode
        {
            Error,
            DefaultConfigFile,
            SpecifiedConfigFile,
            OpenServerURI,
        }

        public partial class LauncherFileSettings
        {
            public const string DefaultLauncherUpdateRepo = "Crybb227/AA-Emu-Launcher";
            public const string DefaultAAClientDownloadLocation = "https://github.com/Crybb227/AA-Emu-Client-AA";
            public const string DefaultAA30ClientDownloadLocation = "https://drive.google.com/file/d/1KQE-OIgGaOSqr69MufLe8R6odaIK8nit/view";
            public const string DefaultWoWClientDownloadLocation = "https://btground.dedyn.io/chmi/additional_patches_for_335a.zip";
            public const string LegacyAAClientGoogleDriveLocation = "https://drive.google.com/drive/folders/1_pIBVHIm1YFal-nteGaVuXjTv3Yrsv4Q";

            [JsonProperty("configVersion", NullValueHandling = NullValueHandling.Ignore)]
            public int ConfigVersion { get; set; } = 0;

            [JsonProperty("configName", NullValueHandling = NullValueHandling.Ignore)]
            public string ConfigName { get; set; } = string.Empty;

            [JsonProperty("lang", NullValueHandling = NullValueHandling.Ignore)]
            public string Lang { get; set; } = settingsLangEN_US;

            [JsonProperty("launcherLang", NullValueHandling = NullValueHandling.Ignore)]
            public string LauncherLang { get; set; } = settingsLangEN_US;

            [JsonProperty("pathToGame", NullValueHandling = NullValueHandling.Ignore)]
            public string PathToGame { get; set; } = string.Empty;

            [JsonProperty("aaPath", NullValueHandling = NullValueHandling.Ignore)]
            public string AAPath { get; set; } = string.Empty;

            [JsonProperty("aaDownloadLocation", NullValueHandling = NullValueHandling.Ignore)]
            public string AADownloadLocation { get; set; } = DefaultAAClientDownloadLocation;

            [JsonProperty("aa30Path", NullValueHandling = NullValueHandling.Ignore)]
            public string AA30Path { get; set; } = string.Empty;

            [JsonProperty("aa30DownloadLocation", NullValueHandling = NullValueHandling.Ignore)]
            public string AA30DownloadLocation { get; set; } = DefaultAA30ClientDownloadLocation;

            [JsonProperty("wowPath", NullValueHandling = NullValueHandling.Ignore)]
            public string WoWPath { get; set; } = string.Empty;

            [JsonProperty("wowDownloadLocation", NullValueHandling = NullValueHandling.Ignore)]
            public string WoWDownloadLocation { get; set; } = DefaultWoWClientDownloadLocation;

            [JsonProperty("serverIPAddress", NullValueHandling = NullValueHandling.Ignore)]
            public string ServerIpAddress { get; set; } = "127.0.0.1";

            [JsonProperty("saveLoginAndPassword", NullValueHandling = NullValueHandling.Ignore)]
            public bool SaveLoginAndPassword { get; set; } = false;

            [JsonProperty("skipIntro", NullValueHandling = NullValueHandling.Ignore)]
            public bool SkipIntro { get; set; } = false;

            [JsonProperty("hideSplashLogo", NullValueHandling = NullValueHandling.Ignore)]
            public bool HideSplashLogo { get; set; } = false;

            [JsonProperty("lastLoginUser", NullValueHandling = NullValueHandling.Ignore)]
            public string LastLoginUser { get; set; } = string.Empty;

            [JsonProperty("lastLoginPass", NullValueHandling = NullValueHandling.Ignore)]
            public string LastLoginPass { get; set; } = string.Empty;

            [JsonProperty("loginType", NullValueHandling = NullValueHandling.Ignore)]
            public string ClientLoginType { get; set; } = stringTrino_1_2;

            [JsonProperty("updateLocale", NullValueHandling = NullValueHandling.Ignore)]
            public bool UpdateLocale { get; set; } = true;

            [JsonProperty("allowGameUpdates", NullValueHandling = NullValueHandling.Ignore)]
            public bool AllowGameUpdates { get; set; } = false;

            [JsonProperty("serverGameUpdateURL", NullValueHandling = NullValueHandling.Ignore)]
            public string ServerGameUpdateURL { get; set; } = string.Empty;

            [JsonProperty("serverWebsiteURL", NullValueHandling = NullValueHandling.Ignore)]
            public string ServerWebSiteURL { get; set; } = string.Empty;

            [JsonProperty("serverNewsFeedURL", NullValueHandling = NullValueHandling.Ignore)]
            public string ServerNewsFeedURL { get; set; } = string.Empty;

            [JsonProperty("serverDiscordURL", NullValueHandling = NullValueHandling.Ignore)]
            public string ServerDiscordURL { get; set; } = string.Empty;

            [JsonProperty("serverDiscordName", NullValueHandling = NullValueHandling.Ignore)]
            public string ServerDiscordName { get; set; } = string.Empty;

            [JsonProperty("userHistory", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> UserHistory { get; set; } = new List<string>();

            [JsonProperty("autoLaunch", NullValueHandling = NullValueHandling.Ignore)]
            public bool AutoLaunch { get; set; } = false;


            public LauncherFileSettings()
            {
                SetDefaultSettings(this);
            }

            public static void SetDefaultSettings(LauncherFileSettings setting)
            {
                if (setting == null)
                    setting = new LauncherFileSettings();
                setting.ConfigVersion = 0;
                setting.ConfigName = "";
                setting.Lang = settingsLangEN_US;
                setting.LauncherLang = settingsLangEN_US;
                setting.PathToGame = "";
                setting.AAPath = "";
                setting.AADownloadLocation = DefaultAAClientDownloadLocation;
                setting.AA30Path = "";
                setting.AA30DownloadLocation = DefaultAA30ClientDownloadLocation;
                setting.WoWPath = "";
                setting.WoWDownloadLocation = DefaultWoWClientDownloadLocation;
                setting.ServerIpAddress = "127.0.0.1";
                setting.SaveLoginAndPassword = false;
                setting.SkipIntro = false;
                setting.HideSplashLogo = false;
                setting.LastLoginUser = "";
                setting.LastLoginPass = "";
                setting.ClientLoginType = stringTrino_1_2;
                setting.UpdateLocale = true;
                setting.AllowGameUpdates = false;
                setting.ServerGameUpdateURL = "";
                setting.ServerWebSiteURL = ""; // "https://aaemu.info/api/articles";
                setting.ServerNewsFeedURL = "";
                setting.UserHistory = new List<string>();
            }

        }

        public partial class ClientLookupHelper
        {
            [JsonProperty("serverName")]
            public List<string> ServerNames { get; set; } = new List<string>();

            [JsonProperty("clientLocation")]
            public List<string> ClientLocations { get; set; } = new List<string>();
        }

        // Some strings for our language settings
        private const string settingsLangEN_US = "en_us"; // English US
        private const string settingsLangEN_SG = "en_sg"; // English US
        private const string settingsLangRU = "ru"; // Russian
        private const string settingsLangDE = "de"; // German
        // unused for launcher
        private const string settingsLangFR = "fr"; // French
        // unused in launcher
        private const string settingsLangKR = "ko"; // Korean
        private const string settingsLangJP = "ja"; // Japanese
        private const string settingsLangZH_CN = "zh_cn"; // Chinese simplified
        private const string settingsLangZH_TW = "zh_tw"; // Chinese traditional // Not sure how I'd do this with flags if ever
        private const string settingsLangSE_SV = "se_sv"; // Swedish

        public partial class LanguageSettings
        {
            [JsonProperty("lang")]
            public string Lang { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }

            [JsonProperty("serveraddress")]
            public string ServerAddress { get; set; }

            [JsonProperty("pathtogame")]
            public string PathToGame { get; set; }

            [JsonProperty("savecredentials")]
            public string SaveCredentials { get; set; }

            [JsonProperty("skipintro")]
            public string SkipIntro { get; set; }

            [JsonProperty("hidesplashscreen")]
            public string HideSplashScreen { get; set; }

            [JsonProperty("saveSettings")]
            public string SaveSettings { get; set; }

            [JsonProperty("cancel")]
            public string Cancel { get; set; }

            [JsonProperty("settings")]
            public string Settings { get; set; }

            [JsonProperty("website")]
            public string Website { get; set; }

            [JsonProperty("play")]
            public string Play { get; set; }

            [JsonProperty("online")]
            public string Online { get; set; }

            [JsonProperty("offline")]
            public string Offline { get; set; }

            [JsonProperty("update")]
            public string Update { get; set; }

            [JsonProperty("updating")]
            public string Updating { get; set; }

            [JsonProperty("updatelocale")]
            public string UpdateLocale { get; set; }

            [JsonProperty("allowupdates")]
            public string AllowUpdates { get; set; }

            [JsonProperty("toolsafailed")]
            public string ToolsAFailed { get; set; }

            [JsonProperty("unknownlauncherprotocol")]
            public string UnknownLauncherProtocol { get; set; }

            [JsonProperty("nouserorpassword")]
            public string NoUserOrPassword { get; set; }

            [JsonProperty("missinggame")]
            public string MissingGame { get; set; }

            [JsonProperty("errorupdatingfile")]
            public string ErrorUpdatingFile { get; set; }

            [JsonProperty("minimize")]
            public string Minimize { get; set; }

            [JsonProperty("closeprogram")]
            public string CloseProgram { get; set; }

            [JsonProperty("noupdateurl")]
            public string NoUpdateURL { get; set; }

            [JsonProperty("checkversion")]
            public string CheckVersion { get; set; }

            [JsonProperty("downloadpatch")]
            public string DownloadPatch { get; set; }

            [JsonProperty("downloaderror")]
            public string DownloadError { get; set; }

            [JsonProperty("applypatch")]
            public string ApplyPatch { get; set; }

            [JsonProperty("applypatcherror")]
            public string ApplyPatchError { get; set; }

            [JsonProperty("patchcomplete")]
            public string PatchComplete { get; set; }

            [JsonProperty("failedcreatedirectory")]
            public string FailedCreateDirectory { get; set; }

            [JsonProperty("unsupportedpatchtype")]
            public string UnsupportedPatchType { get; set; }

            [JsonProperty("errorreadinglocalversioninformation")]
            public string ErrorReadingLocalVersionInformation { get; set; }

            [JsonProperty("nothingtoupdate")]
            public string NothingToUpdate { get; set; }

            [JsonProperty("failedtoopen")]
            public string FailedToOpen { get; set; }

            [JsonProperty("filenotfound")]
            public string FileNotFound { get; set; }

            [JsonProperty("patchhashmismatch")]
            public string PatchHashMismatch { get; set; }

            [JsonProperty("nofilestoupdate")]
            public string NoFilesToUpdate { get; set; }

            [JsonProperty("errorcreatingpatchcache")]
            public string ErrorCreatingPatchCache { get; set; }

            [JsonProperty("failedtoopenpatchcache")]
            public string FailedToOpenPatchCache { get; set; }

            [JsonProperty("fatalerrorfailedtoopenfileforwrite")]
            public string FatalErrorFailedToOpenFileForWrite { get; set; }

            [JsonProperty("downloadhashmismatch")]
            public string DownloadHashMismatch { get; set; }

            [JsonProperty("failedtosavecache")]
            public string FailedToSaveCache { get; set; }

            [JsonProperty("downloadfileerror")]
            public string DownloadFileError { get; set; }

            [JsonProperty("errorpatchapplyfile")]
            public string ErrorPatchApplyFile { get; set; }

            [JsonProperty("errorpatchexportfile")]
            public string ErrorPatchExportFile { get; set; }

            [JsonProperty("errorpatchexportdb")]
            public string ErrorPatchExportDB { get; set; }

            [JsonProperty("initialization")]
            public string Initialization { get; set; }

            [JsonProperty("downloadverfile")]
            public string DownloadVerFile { get; set; }

            [JsonProperty("comparingversion")]
            public string ComparingVersion { get; set; }

            [JsonProperty("checklocalfiles")]
            public string CheckLocalFiles { get; set; }

            [JsonProperty("rehashlocalfiles")]
            public string ReHashLocalFiles { get; set; }

            [JsonProperty("downloadpatchfilesinfo")]
            public string DownloadPatchFilesInfo { get; set; }

            [JsonProperty("calculatedownloads")]
            public string CalculateDownloads { get; set; }

            [JsonProperty("downloadfiles")]
            public string DownloadFiles { get; set; }

            [JsonProperty("addfiles")]
            public string AddFiles { get; set; }

            [JsonProperty("patchdone")]
            public string PatchDone { get; set; }

            [JsonProperty("gamepakneedsupdate")]
            public string Game_PakNeedsUpdate { get; set; }

            [JsonProperty("downloadsizeandfiles")]
            public string DownloadSizeAndFiles { get; set; }

            [JsonProperty("errorsavingversioninfo")]
            public string ErrorSavingVersionInfo { get; set; }

            [JsonProperty("servernewsloading")]
            public string ServerNewsLoading { get; set; }

            [JsonProperty("servernews")]
            public string ServerNews { get; set; }

            [JsonProperty("servernewsfailed")]
            public string ServerNewsFailed { get; set; }

            [JsonProperty("deletegamesettings")]
            public string DeleteGameSettings { get; set; }

            [JsonProperty("deletegamesettingstitle")]
            public string DeleteGameSettingsTitle { get; set; }

            [JsonProperty("deleteshadercache")]
            public string DeleteShaderCache { get; set; }

            [JsonProperty("deleteshadercachetitle")]
            public string DeleteShaderCacheTitle { get; set; }

            [JsonProperty("downloadsizemismatch")]
            public string DownloadSizeMismatch { get; set; }

            [JsonProperty("troubleshoot")]
            public string TroubleShoot { get; set; }

            [JsonProperty("tsdebugmode")]
            public string TSDebugMode { get; set; }

            [JsonProperty("tsdeleteshadercache")]
            public string TSDeleteShaderCache { get; set; }

            [JsonProperty("tsdeletegamesetting")]
            public string TSDeleteGameSetting { get; set; }

            [JsonProperty("tsdeleteall")]
            public string TSDeleteAll { get; set; }

            [JsonProperty("tsforcepatch")]
            public string TSForcePatch { get; set; }

            [JsonProperty("tsforcenopatch")]
            public string TSForceNoPatch { get; set; }

            [JsonProperty("tsfixbin32")]
            public string TSFixBin32 { get; set; }

            [JsonProperty("fixbin32files")]
            public string FixBin32Files { get; set; }

            [JsonProperty("downloadlauncherupdate")]
            public string DownloadLauncherUpdate { get; set; }

            [JsonProperty("troubleshootgame")]
            public string TroubleshootGame { get;set; }

            [JsonProperty("troubleshootlauncher")]
            public string TroubleshootLauncher { get; set; }

            [JsonProperty("defaultlocationwarning")]
            public string DefaultLocationWarning { get; set; }

            [JsonProperty("urigen")]
            public string URIGen { get; set; }
        }

        public LauncherFileSettings Setting = new LauncherFileSettings();
        public LauncherFileSettings RemoteSetting = new LauncherFileSettings();
        public static LanguageSettings L = new LanguageSettings();
        public ClientLookupHelper ClientLookup = new ClientLookupHelper();

        public static string archeAgeEXE = "archeage.exe";
        public static string archeWorldEXE = "archeworld.exe";
        public static string archeAgeSystemConfigFileName = "system.cfg";
        public static ushort defaultAuthPort = 1237;
        public static string launcherDefaultConfigFile = "settings.aelcf"; // .aelcf = ArcheAge Emu Launcher Configuration File
        public static string clientLookupDefaultFile = "clientslist.json";
        private static readonly string launcherLocalCacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Jason Games Launcher");
        public static string remotePatchFolderURI = ".patch/";
        public static string patchListFileName = "patchfiles.csv";
        public static string patchVersionFileName = "patchfiles.ver";
        public static string localPatchPakFileName = "download.patch";
        public static string launcherProtocolSchema = "aelcf";
        public static string urlAAEmuGitHub = "https://github.com/AAEmu/AAEmu";
        public static string urlLauncherGitHub = "https://github.com/ZeromusXYZ/AAEmu-Launcher";
        public static string urlAAEmuDiscordInvite = "https://discord.gg/vn8E8E6";
        public static string urlLauncherDiscordInvite = "https://discord.gg/GhVfDtK";
        public static string urlWebsite = "https://github.com/AAEmu/AAEmu"; // "https://aaemu.info/";
        public static string dx9downloadURL = "https://www.microsoft.com/en-us/download/confirmation.aspx?id=35";
        string localPatchFolderName = ".patch" + Path.DirectorySeparatorChar;
        string URIConfigFileData = "";
        string URIConfigFileDataHost = "";
        string launcherOpenedConfigFile = "";
        string urlLauncherUpdateDownload = "";
        string LauncherUpdateVersion = "";
        bool checkedForLauncherUpdates = false;
        public const string launcherUpdateRepo = LauncherFileSettings.DefaultLauncherUpdateRepo;
        LauncherUpdateInfo pendingLauncherUpdate = null;
        public string DefaultGameWorkingDirectory = "";


        // launcher protocol indentifiers
        private const string stringMailRu_1_0 = "mailru_1_0";
        private const string stringTrino_1_2 = "trino_1_2";
        private const string stringTrino_3_0 = "trino_3_0";
        private const string stringTrino_6_0 = "trino_6_0";

        // Stuff for dragable form
        private bool formMouseDown;
        private Point lastLocation;

        private ShowPanelType currentPanel = ShowPanelType.Login;
        private int nextServerCheck = -1;
        enum serverCheck { Offline = 0, Online, Unknown, Update, Updating };
        private serverCheck serverCheckStatus = serverCheck.Unknown;
        private bool checkNews = false;
        AAEmuNewsFeed newsFeed = null;
        private int bigNewsIndex = -1;
        private int bigNewsTimer = -1;

        AAEmuLauncherBase aaLauncher = null;
        private bool checkGameIsRunning = false;

        private AAPatchProgress aaPatcher = new AAPatchProgress();
        private AAPak pak = null;
        private AAPak PatchDownloadPak = null;
        // (PakFileInfo, Reason to Add)
        private List<(AAPakFileInfo, string)> dlPakFileList = new List<(AAPakFileInfo, string)>();
        private LauncherOpenMode AppOpenMode = LauncherOpenMode.DefaultConfigFile;

        // Auto Close
        public int AutoCloseTimer { get; set; } = 0;
        public bool DoAutoLaunch { get; set; } = false;
        private bool CancelPatching { get; set; } = false;

        private readonly Color ModernBack = Color.FromArgb(12, 15, 21);
        private readonly Color ModernPanel = Color.FromArgb(29, 34, 45);
        private readonly Color ModernPanelAlt = Color.FromArgb(20, 24, 33);
        private readonly Color ModernAccent = Color.FromArgb(72, 198, 169);
        private readonly Color ModernAccentHot = Color.FromArgb(92, 224, 190);
        private readonly Color ModernDanger = Color.FromArgb(210, 74, 86);
        private readonly Color ModernText = Color.FromArgb(235, 240, 246);
        private readonly Color ModernMutedText = Color.FromArgb(145, 157, 172);
        private Label lBrandTitle;
        private Label lBrandSubtitle;
        private Panel pGameHeader;
        private Label lGameArcheAge;
        private Label lGameArcheAge30;
        private Label lGameJasonWoW;
        private Label lGamePlaceholder;
        private Label lInstallStatus;
        private Label lClientUpdateAction;
        private Label lDownloadLocationLabel;
        private TextBox eDownloadLocation;
        private Label lHeroTitle;
        private Label lHeroNewsTitle;
        private Label lHeroNewsBody;
        private bool isCloseButtonHot = false;
        private bool isMinimizeButtonHot = false;
        private string selectedGameId = "aa";
        private bool isClientDeltaUpdating = false;


        public LauncherForm()
        {
            InitializeComponent();
            ApplyModernTheme();
        }

        private void ApplyModernTheme()
        {
            Text = "Jason Games Launcher";
            ClientSize = new Size(1280, 720);
            CenterToScreen();
            BackColor = ModernBack;
            ForeColor = ModernText;
            BackgroundImage = null;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);

            panelLoginAndPatch.BackgroundImage = null;
            panelSettings.BackgroundImage = null;
            panelLoginAndPatch.BackColor = Color.Transparent;
            panelSettings.BackColor = Color.Transparent;
            panelLoginAndPatch.Paint += ModernPanel_Paint;
            panelSettings.Paint += ModernPanel_Paint;
            Paint += LauncherForm_Paint;

            lBrandTitle = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point, 0),
                ForeColor = ModernText,
                Location = new Point(36, 22),
                Size = new Size(520, 48),
                Text = "Jason Games Launcher"
            };

            lBrandSubtitle = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0),
                ForeColor = ModernMutedText,
                Location = new Point(39, 68),
                Size = new Size(520, 24),
                Text = "AAEmu client launcher, patcher, and server profile"
            };

            CreateBattleNetShellControls();

            Controls.Add(lBrandTitle);
            Controls.Add(lBrandSubtitle);
            lBrandTitle.BringToFront();
            lBrandSubtitle.BringToFront();
            lBrandTitle.MouseDown += LauncherForm_MouseDown;
            lBrandTitle.MouseMove += LauncherForm_MouseMove;
            lBrandTitle.MouseUp += LauncherForm_MouseUp;
            lBrandSubtitle.MouseDown += LauncherForm_MouseDown;
            lBrandSubtitle.MouseMove += LauncherForm_MouseMove;
            lBrandSubtitle.MouseUp += LauncherForm_MouseUp;

            StyleTextBox(eLogin);
            StyleTextBox(ePassword);
            StyleTextBox(eServerIP);
            cbLoginList.BackColor = Color.FromArgb(22, 27, 36);
            cbLoginList.ForeColor = ModernText;
            cbLoginList.Font = new Font("Segoe UI", 9F);

            StyleModernLabel(lLogin, ModernMutedText, 9F);
            StyleModernLabel(lPassword, ModernMutedText, 9F);
            StyleModernLabel(lIPAddress, ModernMutedText, 9F);
            StyleModernLabel(lPathToGameLabel, ModernMutedText, 9F);
            StyleModernLabel(lDownloadLocationLabel, ModernMutedText, 9F);
            StylePathValueLabel(lGamePath);
            StyleModernLabel(lDownloadClient, ModernAccent, 9F);
            StyleModernLabel(lGameClientType, ModernAccent, 9F);
            StyleModernLabel(lPatchProgressBarText, ModernText, 11F);
            StyleModernLabel(lBigNewsImage, ModernAccent, 10F);

            foreach (var label in new[] { lSaveUser, lUpdateLocale, lHideSplash, lSkipIntro, lAllowUpdates })
                StyleModernLabel(label, ModernText, 9F);
            foreach (var label in new[] { cbSaveUser, cbUpdateLocale, cbHideSplash, cbSkipIntro, cbAllowUpdates })
                StyleCheckBoxLabel(label, ModernAccent, 13F);

            lNewsFeed.Image = null;
            lNewsFeed.BackColor = Color.FromArgb(31, 37, 49);
            lNewsFeed.ForeColor = ModernText;
            lNewsFeed.Font = new Font("Segoe UI", 10F);
            lNewsFeed.Padding = new Padding(12);
            lNewsFeed.TextAlign = ContentAlignment.TopLeft;

            btnPlay.Image = null;
            btnSettings.Image = null;
            btnWebsite.Image = null;
            ConfigureWindowButton(btnMinimize);
            ConfigureWindowButton(btnClose);
            StyleCommandLabel(btnSettings, Color.FromArgb(45, 52, 66), ModernText, 9F);
            StyleCommandLabel(btnWebsite, Color.FromArgb(45, 52, 66), ModernText, 9F);
            StyleCommandLabel(lSettingsBack, Color.FromArgb(45, 52, 66), ModernText, 11F);
            StyleCommandLabel(lDownloadLauncherUpdate, Color.Transparent, Color.FromArgb(250, 210, 90), 10F);

            pPatchSteps.BackColor = Color.Transparent;
            foreach (var rb in pPatchSteps.Controls.OfType<RadioButton>())
            {
                rb.BackColor = Color.Transparent;
                rb.ForeColor = ModernText;
                rb.Font = new Font("Segoe UI", 9F);
                rb.UseVisualStyleBackColor = false;
            }

            pgbBackTotal.Image = null;
            pgbBackTotal.BackColor = Color.FromArgb(42, 48, 61);
            pgbBackTotal.SizeMode = PictureBoxSizeMode.Normal;
            pgbFrontTotal.Image = null;
            pgbFrontTotal.BackColor = ModernAccent;

            lAppVersion.ForeColor = ModernMutedText;
            lLoadedConfig.ForeColor = ModernAccent;
            lLoadedConfig.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lDownloadLauncherUpdate.ForeColor = Color.FromArgb(250, 210, 90);
            lAppVersion.Location = new Point(28, 674);
            lLoadedConfig.Location = new Point(28, 648);
            btnLocaleLang.Location = new Point(1134, 22);
            btnLauncherLangChange.Location = new Point(1174, 22);
            btnDiscord.Location = new Point(1214, 22);
            btnGithub.Location = new Point(1244, 22);

            StyleContextMenu(cmsAAEmuButton);
            StyleContextMenu(cmsGitHub);
            StyleContextMenu(cmsLauncherLanguage);
            StyleContextMenu(cmsLocaleLanguage);
            StyleContextMenu(cmsDiscord);
            StyleContextMenu(cmsAuthType);

            LayoutBattleNetShell();
            ApplyModernPlayButton(serverCheckStatus, false);
        }

        private void CreateBattleNetShellControls()
        {
            pGameHeader = new Panel
            {
                BackColor = Color.FromArgb(14, 17, 24),
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 122),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pGameHeader.MouseDown += LauncherForm_MouseDown;
            pGameHeader.MouseMove += LauncherForm_MouseMove;
            pGameHeader.MouseUp += LauncherForm_MouseUp;

            var lLogo = CreateHeaderTab("Jason Games Launcher", new Point(28, 18), false);
            lLogo.Cursor = Cursors.Default;
            lLogo.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lLogo.ForeColor = Color.FromArgb(72, 155, 255);
            lLogo.Size = new Size(270, 40);
            lLogo.TextAlign = ContentAlignment.MiddleLeft;
            lGameArcheAge = CreateGameIcon("AA v1.2", new Point(144, 64), true);
            lGameArcheAge.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lGameArcheAge.Size = new Size(72, 44);
            lGameArcheAge.Click += (s, e) => SelectLauncherGame("aa");
            lGameArcheAge30 = CreateGameIcon("AA v3.0.3", new Point(226, 64), false);
            lGameArcheAge30.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lGameArcheAge30.Size = new Size(86, 44);
            lGameArcheAge30.Click += (s, e) => SelectLauncherGame("aa30");
            lGameJasonWoW = CreateGameIcon("JW", new Point(322, 64), false);
            lGameJasonWoW.Click += (s, e) => SelectLauncherGame("jw");
            lGamePlaceholder = CreateGameIcon("+", new Point(388, 64), false);

            pGameHeader.Controls.Add(lLogo);
            pGameHeader.Controls.Add(lGameArcheAge);
            pGameHeader.Controls.Add(lGameArcheAge30);
            pGameHeader.Controls.Add(lGameJasonWoW);
            pGameHeader.Controls.Add(lGamePlaceholder);
            Controls.Add(pGameHeader);
            pGameHeader.SendToBack();

            lInstallStatus = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = ModernMutedText,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Location = new Point(28, 616),
                Size = new Size(242, 44),
                TextAlign = ContentAlignment.TopLeft
            };

            lClientUpdateAction = new Label
            {
                AutoSize = false,
                BackColor = Color.FromArgb(45, 52, 66),
                Cursor = Cursors.Hand,
                ForeColor = ModernText,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0),
                Location = new Point(28, 566),
                Size = new Size(242, 34),
                Text = "Check Client Updates",
                TextAlign = ContentAlignment.MiddleCenter
            };
            lClientUpdateAction.Click += LClientUpdateAction_Click;

            lDownloadLocationLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = ModernMutedText,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Text = "Download Location"
            };

            eDownloadLocation = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(22, 27, 36),
                ForeColor = ModernText,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0)
            };

            lHeroTitle = CreateSurfaceLabel("ARCHEAGE", new Point(28, 188), new Size(242, 96), 24F, ModernText, ContentAlignment.MiddleCenter);
            lHeroTitle.Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lHeroNewsTitle = CreateSurfaceLabel("News & Updates", new Point(900, 206), new Size(300, 32), 12F, ModernMutedText, ContentAlignment.MiddleLeft);
            lHeroNewsBody = CreateSurfaceLabel("Install, patch, and launch private server clients from one place.", new Point(900, 238), new Size(300, 128), 12F, ModernText, ContentAlignment.TopLeft);
            Controls.Add(lInstallStatus);
            Controls.Add(lClientUpdateAction);
            panelSettings.Controls.Add(lDownloadLocationLabel);
            panelSettings.Controls.Add(eDownloadLocation);
            Controls.Add(lHeroTitle);
            Controls.Add(lHeroNewsTitle);
            Controls.Add(lHeroNewsBody);

            lInstallStatus.BringToFront();
            lClientUpdateAction.BringToFront();
            SelectLauncherGame(selectedGameId);
        }

        private void SelectLauncherGame(string gameId)
        {
            SaveSelectedGameFields();
            selectedGameId = gameId;
            var isAa = selectedGameId == "aa";
            var isAa30 = selectedGameId == "aa30";

            SetGameIconSelected(lGameArcheAge, isAa);
            SetGameIconSelected(lGameArcheAge30, isAa30);
            SetGameIconSelected(lGameJasonWoW, selectedGameId == "jw");

            lHeroTitle.Text = selectedGameId == "jw" ? "JASONWOW" : "ARCHEAGE";
            lHeroNewsTitle.Text = isAa30 ? "AA v3.0.3" : selectedGameId == "jw" ? "JasonWoW" : "AA v1.2";
            lHeroNewsBody.Text = isAa30
                ? "AA v3.0.3 - Trion - r318414 - 2016-12-08 is selected."
                : isAa
                    ? "Install, patch, and launch private server clients from one place."
                    : "JasonWoW is selected.";
            ApplySelectedGameToLegacySettings();
            LoadSelectedGameFields();
            UpdateInstallStatus();
            UpdatePlayButton(serverCheckStatus, false);
            Invalidate(true);
        }

        private bool IsArcheAgeSelected => selectedGameId == "aa" || selectedGameId == "aa30";

        private string SelectedGameDisplayName
        {
            get
            {
                if (selectedGameId == "jw")
                    return "WoW";
                if (selectedGameId == "aa30")
                    return "AA v3.0.3";
                return "AA v1.2";
            }
        }

        private string GetSelectedGamePath()
        {
            if (selectedGameId == "jw")
                return Setting.WoWPath;
            if (selectedGameId == "aa30")
                return Setting.AA30Path;
            return GetAAPath();
        }

        private string GetSelectedDownloadLocation()
        {
            if (selectedGameId == "jw")
                return GetWoWDownloadLocation();
            if (selectedGameId == "aa30")
                return GetAA30DownloadLocation();
            return GetAADownloadLocation();
        }

        private string GetAAPath()
        {
            return string.IsNullOrWhiteSpace(Setting.AAPath) ? Setting.PathToGame : Setting.AAPath;
        }

        private string GetAADownloadLocation()
        {
            if (!string.IsNullOrWhiteSpace(Setting.AADownloadLocation))
                return Setting.AADownloadLocation;
            if (!string.IsNullOrWhiteSpace(Setting.ServerGameUpdateURL))
                return Setting.ServerGameUpdateURL;
            return LauncherFileSettings.DefaultAAClientDownloadLocation;
        }

        private string GetAA30DownloadLocation()
        {
            if (!string.IsNullOrWhiteSpace(Setting.AA30DownloadLocation))
                return Setting.AA30DownloadLocation;
            return LauncherFileSettings.DefaultAA30ClientDownloadLocation;
        }

        private string GetWoWDownloadLocation()
        {
            if (!string.IsNullOrWhiteSpace(Setting.WoWDownloadLocation))
                return Setting.WoWDownloadLocation;
            return LauncherFileSettings.DefaultWoWClientDownloadLocation;
        }

        private bool IsLegacyAAClientGoogleDriveLocation(string downloadLocation)
        {
            return selectedGameId == "aa" &&
                !string.IsNullOrWhiteSpace(downloadLocation) &&
                string.Equals(downloadLocation.TrimEnd('/'), LauncherFileSettings.LegacyAAClientGoogleDriveLocation, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMegaDownloadLocation(string downloadLocation)
        {
            return !string.IsNullOrWhiteSpace(downloadLocation) &&
                Uri.TryCreate(downloadLocation, UriKind.Absolute, out var uri) &&
                uri.Host.EndsWith("mega.nz", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsGoogleDriveDownloadLocation(string downloadLocation)
        {
            return !string.IsNullOrWhiteSpace(downloadLocation) &&
                Uri.TryCreate(downloadLocation, UriKind.Absolute, out var uri) &&
                uri.Host.EndsWith("google.com", StringComparison.OrdinalIgnoreCase) &&
                uri.Host.IndexOf("drive", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsManifestBasedDownloadLocation(string downloadLocation)
        {
            return !string.IsNullOrWhiteSpace(downloadLocation) &&
                !IsLegacyAAClientGoogleDriveLocation(downloadLocation) &&
                !IsDirectArchiveDownloadLocation(downloadLocation) &&
                !IsMegaDownloadLocation(downloadLocation) &&
                !IsGoogleDriveDownloadLocation(downloadLocation);
        }

        private bool IsDirectArchiveDownloadLocation(string downloadLocation)
        {
            if (string.IsNullOrWhiteSpace(downloadLocation) ||
                !Uri.TryCreate(downloadLocation, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var extension = Path.GetExtension(uri.AbsolutePath);
            return string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".7z", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".rar", StringComparison.OrdinalIgnoreCase);
        }

        private string GetDownloadArchiveFileName(string downloadLocation)
        {
            if (Uri.TryCreate(downloadLocation, UriKind.Absolute, out var uri))
            {
                var fileName = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                    return fileName;
            }

            return "client.zip";
        }

        private void SetSelectedGamePath(string path)
        {
            if (selectedGameId == "jw")
            {
                Setting.WoWPath = path;
            }
            else if (selectedGameId == "aa30")
            {
                Setting.AA30Path = path;
            }
            else
            {
                Setting.AAPath = path;
                Setting.PathToGame = path;
            }
        }

        private void SetSelectedDownloadLocation(string url)
        {
            if (selectedGameId == "jw")
            {
                Setting.WoWDownloadLocation = url;
            }
            else if (selectedGameId == "aa30")
            {
                Setting.AA30DownloadLocation = url;
            }
            else
            {
                Setting.AADownloadLocation = url;
                Setting.ServerGameUpdateURL = url;
            }
        }

        private void ApplySelectedGameToLegacySettings()
        {
            if (selectedGameId == "aa30")
            {
                Setting.PathToGame = Setting.AA30Path;
                Setting.ServerGameUpdateURL = GetAA30DownloadLocation();
                Setting.ClientLoginType = stringTrino_3_0;
            }
            else if (selectedGameId == "aa")
            {
                Setting.PathToGame = GetAAPath();
                Setting.ServerGameUpdateURL = GetAADownloadLocation();
                if (string.IsNullOrWhiteSpace(Setting.ClientLoginType) || Setting.ClientLoginType == stringTrino_3_0)
                    Setting.ClientLoginType = stringTrino_1_2;
            }
        }

        private void SaveSelectedGameFields()
        {
            if (lGamePath == null || eDownloadLocation == null)
                return;

            SetSelectedGamePath(lGamePath.Text.Trim());
            SetSelectedDownloadLocation(eDownloadLocation.Text.Trim());
        }

        private void LoadSelectedGameFields()
        {
            if (lGamePath == null || eDownloadLocation == null || lPathToGameLabel == null || lDownloadLocationLabel == null)
                return;

            lPathToGameLabel.Text = SelectedGameDisplayName + " Path";
            lDownloadLocationLabel.Text = SelectedGameDisplayName + " Download Location";
            lGamePath.Text = GetSelectedGamePath();
            eDownloadLocation.Text = GetSelectedDownloadLocation();
            if (selectedGameId == "jw")
                lGameClientType.Text = "JasonWoW";
            else if (selectedGameId == "aa30")
            {
                Setting.ClientLoginType = stringTrino_3_0;
                UpdateGameClientTypeLabel();
            }
            else
                UpdateGameClientTypeLabel();
        }

        private void SetGameIconSelected(Label label, bool selected)
        {
            label.BackColor = selected ? Color.FromArgb(42, 47, 62) : Color.Transparent;
            label.BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None;
            label.ForeColor = selected ? Color.FromArgb(248, 205, 99) : Color.FromArgb(139, 174, 166);
        }

        private Label CreateGameIcon(string text, Point location, bool selected)
        {
            return new Label
            {
                AutoSize = false,
                BackColor = selected ? Color.FromArgb(42, 47, 62) : Color.Transparent,
                BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None,
                Cursor = Cursors.Hand,
                ForeColor = selected ? Color.FromArgb(248, 205, 99) : Color.FromArgb(139, 174, 166),
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point, 0),
                Location = location,
                Size = new Size(56, 44),
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private Label CreateHeaderTab(string text, Point location, bool selected)
        {
            return new Label
            {
                AutoSize = false,
                BackColor = selected ? Color.FromArgb(32, 39, 52) : Color.Transparent,
                Cursor = Cursors.Hand,
                ForeColor = selected ? ModernText : ModernMutedText,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0),
                Location = location,
                Size = new Size(112, 36),
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private Label CreateSurfaceLabel(string text, Point location, Size size, float fontSize, Color foreColor, ContentAlignment alignment)
        {
            return new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = foreColor,
                Font = new Font("Segoe UI Semibold", fontSize, FontStyle.Bold, GraphicsUnit.Point, 0),
                Location = location,
                Size = size,
                Text = text,
                TextAlign = alignment
            };
        }

        private void LayoutBattleNetShell()
        {
            panelLoginAndPatch.Location = new Point(0, 0);
            panelLoginAndPatch.Size = Size;
            panelSettings.Location = new Point(0, 0);
            panelSettings.Size = Size;

            btnClose.Location = new Point(ClientSize.Width - 46, 0);
            btnClose.Size = new Size(46, 32);
            btnMinimize.Location = new Point(ClientSize.Width - 92, 0);
            btnMinimize.Size = new Size(46, 32);
            lBrandTitle.Visible = false;
            lBrandSubtitle.Visible = false;

            eLogin.Location = new Point(28, 320);
            eLogin.Size = new Size(242, 30);
            ePassword.Location = new Point(28, 370);
            ePassword.Size = new Size(242, 30);
            cbLoginList.Location = new Point(274, 320);
            cbLoginList.Size = new Size(28, 30);
            lLogin.Location = new Point(28, 298);
            lLogin.Size = new Size(160, 20);
            lPassword.Location = new Point(28, 348);
            lPassword.Size = new Size(160, 20);

            btnPlay.Location = new Point(28, 414);
            btnPlay.Size = new Size(242, 46);
            lClientUpdateAction.Location = new Point(28, 474);
            lClientUpdateAction.Size = new Size(242, 32);
            lInstallStatus.Location = new Point(28, 518);
            lInstallStatus.Size = new Size(242, 62);
            lHeroTitle.Location = new Point(28, 178);
            lHeroTitle.Size = new Size(242, 72);
            lHeroTitle.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSettings.Location = new Point(900, 154);
            btnSettings.Size = new Size(96, 32);
            btnWebsite.Location = new Point(1004, 154);
            btnWebsite.Size = new Size(96, 32);
            lHeroNewsTitle.Location = new Point(900, 204);
            lHeroNewsTitle.Size = new Size(300, 32);
            lHeroNewsBody.Location = new Point(900, 236);
            lHeroNewsBody.Size = new Size(300, 152);

            lNewsFeed.Location = new Point(318, 190);
            lNewsFeed.Size = new Size(540, 286);
            imgBigNews.Location = new Point(318, 190);
            imgBigNews.Size = new Size(540, 286);
            lBigNewsImage.Location = new Point(318, 482);
            lBigNewsImage.Size = new Size(540, 24);

            pgbBackTotal.Location = new Point(318, 280);
            pgbBackTotal.Size = new Size(678, 18);
            pgbFrontTotal.Location = pgbBackTotal.Location;
            pgbFrontTotal.Size = new Size(0, 18);
            lPatchProgressBarText.Location = new Point(318, 244);
            lPatchProgressBarText.Size = new Size(678, 28);
            pPatchSteps.Location = new Point(318, 316);
            pPatchSteps.Size = new Size(678, 170);

            eServerIP.Location = new Point(318, 206);
            eServerIP.Size = new Size(260, 30);
            lIPAddress.Location = new Point(318, 184);
            lIPAddress.Size = new Size(160, 20);
            lPathToGameLabel.Location = new Point(318, 258);
            lPathToGameLabel.Size = new Size(160, 20);
            lGamePath.Location = new Point(318, 282);
            lGamePath.Size = new Size(678, 38);
            lDownloadLocationLabel.Location = new Point(318, 330);
            lDownloadLocationLabel.Size = new Size(220, 20);
            eDownloadLocation.Location = new Point(318, 354);
            eDownloadLocation.Size = new Size(678, 30);
            lDownloadClient.Location = new Point(318, 394);
            lDownloadClient.Size = new Size(210, 28);
            lGameClientType.Location = new Point(548, 394);
            lGameClientType.Size = new Size(260, 28);
            lSettingsBack.Location = new Point(786, 612);
            lSettingsBack.Size = new Size(210, 38);

            cbSaveUser.Location = new Point(318, 452);
            lSaveUser.Location = new Point(348, 452);
            cbUpdateLocale.Location = new Point(318, 486);
            lUpdateLocale.Location = new Point(348, 486);
            cbAllowUpdates.Location = new Point(318, 520);
            lAllowUpdates.Location = new Point(348, 520);
            cbSkipIntro.Location = new Point(570, 452);
            lSkipIntro.Location = new Point(600, 452);
            cbHideSplash.Location = new Point(570, 486);
            lHideSplash.Location = new Point(600, 486);

            foreach (var box in new[] { cbSaveUser, cbUpdateLocale, cbAllowUpdates, cbSkipIntro, cbHideSplash })
                box.Size = new Size(22, 22);
            foreach (var label in new[] { lSaveUser, lUpdateLocale, lAllowUpdates, lSkipIntro, lHideSplash })
                label.Size = new Size(200, 24);
        }

        private void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = Color.FromArgb(22, 27, 36);
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.ForeColor = ModernText;
            textBox.Font = new Font("Segoe UI", 12.5F);
        }

        private void StyleModernLabel(Label label, Color foreColor, float size, FontStyle style = FontStyle.Regular)
        {
            label.BackColor = Color.Transparent;
            label.ForeColor = foreColor;
            label.Font = new Font("Segoe UI", size, style, GraphicsUnit.Point, 0);
        }

        private void StylePathValueLabel(Label label)
        {
            label.BackColor = Color.FromArgb(22, 27, 36);
            label.BorderStyle = BorderStyle.FixedSingle;
            label.ForeColor = ModernText;
            label.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label.Padding = new Padding(10, 0, 10, 0);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        // Gives the settings checkboxes a visible box outline, since a bare checkmark glyph is easy to miss
        private void StyleCheckBoxLabel(Label label, Color foreColor, float size)
        {
            label.BackColor = Color.FromArgb(22, 27, 36);
            label.ForeColor = foreColor;
            label.Font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Point, 0);
            label.BorderStyle = BorderStyle.FixedSingle;
            label.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void StyleCommandLabel(Label label, Color backColor, Color foreColor, float size)
        {
            label.Image = null;
            label.BackColor = backColor;
            label.ForeColor = foreColor;
            label.Font = new Font("Segoe UI Semibold", size, FontStyle.Bold, GraphicsUnit.Point, 0);
            label.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void ConfigureWindowButton(PictureBox button)
        {
            button.Image = null;
            button.BackColor = Color.Transparent;
            button.SizeMode = PictureBoxSizeMode.Normal;
            button.Paint -= WindowButton_Paint;
            button.Paint += WindowButton_Paint;
            button.Invalidate();
        }

        private void WindowButton_Paint(object sender, PaintEventArgs e)
        {
            var button = sender as PictureBox;
            if (button == null)
                return;

            var isClose = button == btnClose;
            var isHot = isClose ? isCloseButtonHot : isMinimizeButtonHot;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (isHot)
            {
                var hoverColor = isClose ? Color.FromArgb(196, 43, 43) : Color.FromArgb(40, 46, 58);
                using (var brush = new SolidBrush(hoverColor))
                    e.Graphics.FillRectangle(brush, button.ClientRectangle);
            }

            using (var pen = new Pen(isHot || !isClose ? ModernText : Color.FromArgb(185, 194, 205), 1.6F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                if (isClose)
                {
                    e.Graphics.DrawLine(pen, 18, 11, 28, 21);
                    e.Graphics.DrawLine(pen, 28, 11, 18, 21);
                }
                else
                {
                    e.Graphics.DrawLine(pen, 17, 17, 29, 17);
                }
            }
        }

        private void StyleContextMenu(ContextMenuStrip menu)
        {
            menu.BackColor = Color.FromArgb(28, 33, 43);
            menu.ForeColor = ModernText;
            menu.RenderMode = ToolStripRenderMode.System;
        }

        private void LauncherForm_Paint(object sender, PaintEventArgs e)
        {
            using (var backBrush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(10, 13, 19), Color.FromArgb(23, 29, 39), 45F))
                e.Graphics.FillRectangle(backBrush, ClientRectangle);

            using (var leftBrush = new SolidBrush(Color.FromArgb(42, 20, 24, 33)))
                e.Graphics.FillRectangle(leftBrush, new Rectangle(0, 122, 296, ClientSize.Height - 122));
            using (var heroBrush = new LinearGradientBrush(new Rectangle(296, 122, 984, 410), Color.FromArgb(42, 49, 66), Color.FromArgb(19, 23, 32), 0F))
                e.Graphics.FillRectangle(heroBrush, new Rectangle(296, 122, 984, 410));
            using (var cardBrush = new SolidBrush(Color.FromArgb(220, 31, 35, 45)))
                e.Graphics.FillRectangle(cardBrush, new Rectangle(880, 190, 340, 286));
            using (var headerLine = new SolidBrush(Color.FromArgb(54, 139, 235)))
            {
                var indicator = selectedGameId == "aa30"
                    ? new Rectangle(226, 119, 86, 3)
                    : selectedGameId == "jw"
                        ? new Rectangle(322, 119, 56, 3)
                        : new Rectangle(144, 119, 72, 3);
                e.Graphics.FillRectangle(headerLine, indicator);
            }
        }

        private void ModernPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (currentPanel == ShowPanelType.Settings)
            {
                using (var mainBrush = new SolidBrush(ModernPanel))
                    e.Graphics.FillRectangle(mainBrush, new Rectangle(296, 146, 732, 522));
                using (var borderPen = new Pen(Color.FromArgb(62, 72, 88)))
                    e.Graphics.DrawRectangle(borderPen, new Rectangle(296, 146, 732, 522));
                return;
            }

            if (currentPanel == ShowPanelType.UpdatePatch)
            {
                using (var mainBrush = new SolidBrush(ModernPanel))
                    e.Graphics.FillRectangle(mainBrush, new Rectangle(296, 184, 732, 322));
                using (var borderPen = new Pen(Color.FromArgb(62, 72, 88)))
                    e.Graphics.DrawRectangle(borderPen, new Rectangle(296, 184, 732, 322));
            }
        }

        private void ApplyModernPlayButton(serverCheck serverState, bool isMouseOver)
        {
            var buttonColor = ModernAccent;
            if (serverState == serverCheck.Offline || serverState == serverCheck.Updating)
                buttonColor = ModernDanger;
            else if (serverState == serverCheck.Unknown)
                buttonColor = Color.FromArgb(86, 117, 226);
            else if (isMouseOver)
                buttonColor = ModernAccentHot;
            if (!IsGameInstalled())
                buttonColor = isMouseOver ? Color.FromArgb(73, 161, 255) : Color.FromArgb(54, 139, 235);

            StyleCommandLabel(btnPlay, buttonColor, Color.FromArgb(8, 12, 16), 24F);
            btnPlay.Image = null;
            btnPlay.FlatStyle = FlatStyle.Flat;
        }

        private void InitDefaultLanguage()
        {
            L.Lang = settingsLangEN_US;
            btnLauncherLangChange.Image = Properties.Resources.flag_english;
            L.Username = "Username";
            L.Password = "Password";
            L.ServerAddress = "Server Address";
            L.PathToGame = "Path to Game";
            L.SaveCredentials = "Save Credentials";
            L.SkipIntro = "Skip Intro";
            L.HideSplashScreen = "Hide Splash Screen";
            L.SaveSettings = "Save";
            L.Cancel = "Cancel";
            L.Settings = "Settings";
            L.Website = "Website";
            L.Play = "Play";
            L.Online = "Online";
            L.Offline = "Offline";
            L.Update = "Update!";
            L.Updating = "Updating";
            L.UpdateLocale = "Update locale";
            L.AllowUpdates = "Allow Updates";
            L.ToolsAFailed = "Failed to load ToolsA.DLL, you might have a debugger open, if so, please close it and restart the launcher";
            L.UnknownLauncherProtocol = "Unknown launcher protocol: {0}";
            L.NoUserOrPassword = "Please enter a username and password";
            L.MissingGame = "No valid game path set!";
            L.ErrorUpdatingFile = "ERROR updating {0}";
            L.Minimize = "Minimize";
            L.CloseProgram = "Close";
            L.NoUpdateURL = "No update information provided, please start the launcher with a configuration file from the server you are trying to play on.";
            L.CheckVersion = "Checking version information.";
            L.DownloadPatch = "Downloading patch files";
            L.DownloadError = "Error downloading patch files!";
            L.ApplyPatch = "Applying patch";
            L.ApplyPatchError = "Error while patching game files!";
            L.PatchComplete = "Done patching files.";

            L.ServerNewsLoading = "Server News\n\n\n\nLoading ...";
            L.ServerNews = "Server News";
            L.ServerNewsFailed = "Server News\n\n\n\nLoad Failed !";

            L.FailedCreateDirectory = "Failed to create directory: {0}";
            L.UnsupportedPatchType = "Unsupported patch system: Version {0}";
            L.ErrorReadingLocalVersionInformation = "Error loading local Version Information";
            L.NothingToUpdate = "Nothing to update, same version";
            L.NoFilesToUpdate = "No files need to be updated";
            L.FailedToOpen = "Failed to open: {0}";
            L.FileNotFound = "File not found: {0}";
            L.PatchHashMismatch = "Downloaded patch file information does not seem to match the server version information, aborting update !\r\nGot: {0} \r\nExpected: {1}\r\\nPossible corrupted or incomplete download\"";
            L.ErrorCreatingPatchCache = "Error creating patch cache: {0}";
            L.FailedToOpen = "Failed to open patch cache, might be corrupted !\r\nTrying again with a clean cache.";
            L.FatalErrorFailedToOpenFileForWrite = "Fatal Error: unable to open patch cache for writing, please check permissions !";
            L.DownloadSizeMismatch = "Downloaded size does not match\r\n{0}\r\nExpected: {1}\r\nGot: {2}";
            L.DownloadHashMismatch = "Hash of downloaded file does not match\r\n{0}\r\nExpected: {1}\r\nGot: {2}";
            L.FailedToSaveCache = "Failed to save download cache:\r\n{0}";
            L.DownloadFileError = "Error downloading: {0}";
            L.ErrorPatchApplyFile = "Error applying patch file to game client:\r\n{0}";
            L.ErrorPatchExportFile = "Error exporting patch file to game client:\r\n{0}";
            L.ErrorPatchExportDB = "Error exporting database to game client:\r\n{0}";
            L.Initialization = "Initialization";
            L.DownloadVerFile = "Download version information";
            L.ComparingVersion = "Comparing version";
            L.CheckLocalFiles = "Checking local game client files";
            L.ReHashLocalFiles = "Initialize game client for update";
            L.DownloadPatchFilesInfo = "Downloading patch file information";
            L.CalculateDownloads = "Calculating download information";
            L.DownloadFiles = "Download patch files";
            L.AddFiles = "Update game client files";
            L.PatchDone = "Completed";
            L.Game_PakNeedsUpdate = "game_pak needs to be updated, this may take a long time";
            L.DownloadSizeAndFiles = "Download {0} MB in {1} file(s)";
            L.ErrorSavingVersionInfo = "Error saving version information: {0}";
            L.DeleteGameSettings = "Are you sure you want to delete the game settings file ?";
            L.DeleteGameSettingsTitle = "Delete system.cfg ?";
            L.DeleteShaderCache = "Are you sure you want to delete the shader cache ?";
            L.DeleteShaderCacheTitle = "Delete Shader Cache";

            L.TroubleShoot = "Troubleshoot";
            L.TSDebugMode = "Debug Mode";
            L.TSDeleteShaderCache = "Delete Shader Cache";
            L.TSDeleteGameSetting = "Delete Game Settings";
            L.TSDeleteAll = "Delete all ArcheAge settings";
            L.TSForcePatch = "Force Patch Download";
            L.TSForceNoPatch = "Force Skip Patch";
            L.TSFixBin32 = "Fix bin32 and DB files";
            L.FixBin32Files = "Overwrite all files in {0} with the original files from {1} ?";

            L.DownloadLauncherUpdate = "Launcher Version {0} available, click to download";

            L.TroubleshootGame = "Troubleshoot Game";
            L.TroubleshootLauncher = "Troubleshoot Launcher";
            L.DefaultLocationWarning = "It is not recommended to use the virtual default location, please locate the real location of the game executable instead. Do you want to use the current file anyway ?";
            L.URIGen = "Generate Server URI Link";
        }

        private void LoadLanguageFromFile(string languageID)
        {
            bool res = false;

            string lngFileName = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),"lng",languageID + ".lng");

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(lngFileName);
                var ConfigFile = reader.ReadToEnd();

                L = JsonConvert.DeserializeObject<LanguageSettings>(ConfigFile);
                res = true;
            }
            catch
            {
                res = false;
            }
            finally
            {
                // Make sure we close our stream so the file won't be in use when we need to save it
                if (reader != null)
                {
                    reader.Close();
                }
            }
            if (res == false)
            {
                InitDefaultLanguage();
            }

        }


        private void SetCustomCheckBox(Label targetLabel, bool checkState)
        {
            if (checkState)
            {
                targetLabel.Text = "✓";
            }
            else
            {
                targetLabel.Text = " ";
            }
        }

        private void SetCustomCheckBox(Label targetLabel, string checkState)
        {
            SetCustomCheckBox(targetLabel, (checkState == "True"));
        }

        private string ToggleSettingCheckBox(Label targetLabel, string settingString)
        {
            string s = "x";
            if (settingString == "True")
            {
                s = "False";
            }
            else
            {
                s = "True";
            }

            SetCustomCheckBox(targetLabel, s);
            return s;
        }

        private bool ToggleSettingCheckBox(Label targetLabel, bool isChecked)
        {
            bool s = false;
            if (isChecked)
            {
                s = false;
            }
            else
            {
                s = true;
            }
            SetCustomCheckBox(targetLabel, s);
            return s;
        }

        private void ShowPanelControls(ShowPanelType panelID)
        {
            // The reason for doing things this way is because drawing reliable transparent stuff
            // onto a panel is hard as hell. It's easier to just put EVERYTHING on the form, and
            // Just show/hide what you need (swapping out background where needed)

            // 0: Main login "panel"  and  2: Update/Patch "panel"
            panelLoginAndPatch.Visible = ((panelID == ShowPanelType.Login) || (panelID == ShowPanelType.UpdatePatch));
            panelLoginAndPatch.Location = new Point(0, 0);
            panelLoginAndPatch.Size = this.Size;
            eLogin.Visible = (panelID == ShowPanelType.Login);
            ePassword.Visible = (panelID == ShowPanelType.Login);
            lLogin.Visible = (panelID == ShowPanelType.Login);
            lPassword.Visible = (panelID == ShowPanelType.Login);
            lNewsFeed.Visible = ((panelID == ShowPanelType.Login) || (panelID == ShowPanelType.UpdatePatch));
            imgBigNews.Visible = (panelID == ShowPanelType.Login);
            cbLoginList.Visible = (cbLoginList.Items.Count > 0);
            lBigNewsImage.Visible = ((panelID == ShowPanelType.Login) && (lBigNewsImage.Tag != null) && (lBigNewsImage.Tag.ToString() != ""));
            wbNews.Visible = (((panelID == ShowPanelType.Login) || (panelID == ShowPanelType.UpdatePatch)) && (wbNews.Tag != null) && (wbNews.Tag.ToString() == "1"));
            lPatchProgressBarText.Visible = (panelID == ShowPanelType.UpdatePatch);
            pgbBackTotal.Visible = (panelID == ShowPanelType.UpdatePatch);
            pgbFrontTotal.Visible = (panelID == ShowPanelType.UpdatePatch);
            pPatchSteps.Visible = (panelID == ShowPanelType.UpdatePatch);

            // 1: Settings "panel"
            panelSettings.Visible = (panelID == ShowPanelType.Settings);
            panelSettings.Location = new Point(0, 0);
            panelSettings.Size = this.Size;
            lDownloadLocationLabel.Visible = (panelID == ShowPanelType.Settings);
            eDownloadLocation.Visible = (panelID == ShowPanelType.Settings);
            eServerIP.Enabled = (serverCheckStatus != serverCheck.Updating);

            if (serverCheckStatus == serverCheck.Updating)
            {
                lGamePath.Cursor = Cursors.No;
            }
            else
            {
                lGamePath.Cursor = Cursors.Hand;
            }


            // Gray out this setting if no update url is set
            if (!string.IsNullOrWhiteSpace(GetSelectedDownloadLocation()))
            {
                lAllowUpdates.ForeColor = ModernText;
                cbAllowUpdates.ForeColor = ModernAccent;
                cbAllowUpdates.Cursor = Cursors.Hand;
            }
            else
            {
                lAllowUpdates.ForeColor = ModernMutedText;
                cbAllowUpdates.ForeColor = ModernMutedText;
                cbAllowUpdates.Cursor = Cursors.No;
            }

            switch (panelID)
            {
                case ShowPanelType.Login:
                    BackgroundImage = null;
                    panelLoginAndPatch.BackgroundImage = null;
                    break;
                case ShowPanelType.Settings:
                    BackgroundImage = null;
                    panelSettings.BackgroundImage = null;
                    break;
                case ShowPanelType.UpdatePatch:
                    BackgroundImage = null;
                    panelLoginAndPatch.BackgroundImage = null;
                    break;
                default:
                    BackgroundImage = null;
                    break;
            }

            var showHome = (panelID == ShowPanelType.Login);
            foreach (var label in new[] { lHeroTitle, lHeroNewsTitle, lHeroNewsBody })
            {
                label.Visible = showHome;
                label.BringToFront();
            }

            pGameHeader.BringToFront();
            btnClose.BringToFront();
            btnMinimize.BringToFront();
            lInstallStatus.BringToFront();
            lClientUpdateAction.BringToFront();
            lClientUpdateAction.Visible = showHome;
            lInstallStatus.Visible = showHome;
            UpdateInstallStatus();

            currentPanel = panelID;
            Invalidate(true);
        }

        private void ApplyLanguageToLauncher()
        {

            switch (Setting.LauncherLang)
            {
                // Add new launcher languages here
                case settingsLangRU:
                    btnLauncherLangChange.Image = Properties.Resources.flag_ru;
                    break;
                case settingsLangDE:
                    btnLauncherLangChange.Image = Properties.Resources.flag_de;
                    break;
                case settingsLangFR:
                    btnLauncherLangChange.Image = Properties.Resources.flag_fr;
                    break;
                case settingsLangZH_TW:
                    btnLauncherLangChange.Image = Properties.Resources.flag_traditional_chinese;
                    break;
                case settingsLangZH_CN:
                    btnLauncherLangChange.Image = Properties.Resources.flag_cn;
                    break;
                case settingsLangSE_SV:
                    btnLauncherLangChange.Image = Properties.Resources.flag_se_sv;
                    break;
                case settingsLangEN_US:
                default:
                    Setting.LauncherLang = settingsLangEN_US;
                    btnLauncherLangChange.Image = Properties.Resources.flag_english;
                    break;
            }
            LoadLanguageFromFile(Setting.LauncherLang);

            lLogin.Text = L.Username;
            lPassword.Text = L.Password;
            lIPAddress.Text = L.ServerAddress;
            lPathToGameLabel.Text = SelectedGameDisplayName + " Path";
            lSaveUser.Text = L.SaveCredentials;
            lSkipIntro.Text = L.SkipIntro;
            lHideSplash.Text = L.HideSplashScreen;
            lSettingsBack.Text = L.SaveSettings;
            btnSettings.Text = L.Settings;
            btnWebsite.Text = L.Website;
            lUpdateLocale.Text = L.UpdateLocale;
            lAllowUpdates.Text = L.AllowUpdates;
            //minimizeToolStripMenuItem.Text = L.Minimize;
            //closeToolStripMenuItem.Text = L.CloseProgram;

            UpdatePlayButton(serverCheckStatus, false);

            rbInit.Text = L.Initialization;
            rbDownloadVerFile.Text = L.DownloadVerFile;
            rbComparingVersion.Text = L.ComparingVersion;
            rbCheckLocalFiles.Text = L.CheckLocalFiles;
            rbReHashLocalFiles.Text = L.ReHashLocalFiles;
            rbDownloadPatchFilesInfo.Text = L.DownloadPatchFilesInfo;
            rbCalculateDownloads.Text = L.CalculateDownloads;
            rbDownloadFiles.Text = L.DownloadFiles;
            rbAddFiles.Text = L.AddFiles;
            rbDone.Text = L.PatchDone;

            debugModeToolStripMenuItem.Text = L.TSDebugMode;
            deleteShaderCacheToolStripMenuItem.Text = L.TSDeleteShaderCache;
            deleteGameConfigurationToolStripMenuItem.Text = L.TSDeleteGameSetting;
            deleteAllArcheAgeSettingsToolStripMenuItem.Text = L.TSDeleteAll;
            forcePatchDownloadToolStripMenuItem.Text = L.TSForcePatch;
            skipPatchToolStripMenuItem.Text = L.TSForceNoPatch;
            fixBin32StripMenuItem.Text = L.TSFixBin32;

            lDownloadLauncherUpdate.Text = string.Format(L.DownloadLauncherUpdate, LauncherUpdateVersion);
            lDownloadClient.Text = "Install Game";
            LoadSelectedGameFields();
            UpdateInstallStatus();

            troubleshootGameToolStripMenuItem.Text = L.TroubleshootGame;
            troubleshootLauncherToolStripMenuItem.Text = L.TroubleshootLauncher;
            generateServerURILinkToolStripMenuItem.Text = L.URIGen;

            btnLauncherLangChange.Refresh();
        }

        private void UpdateLocaleLanguage()
        {

            switch (Setting.Lang)
            {
                // Add new game languages here
                case settingsLangKR:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_kr;
                    break;
                case settingsLangRU:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_ru;
                    break;
                case settingsLangDE:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_de;
                    break;
                case settingsLangFR:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_fr;
                    break;
                case settingsLangJP:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_jp;
                    break;
                case settingsLangZH_TW:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_traditional_chinese;
                    break;
                case settingsLangZH_CN:
                    btnLocaleLang.Image = Properties.Resources.mini_locale_cn;
                    break;
                case settingsLangEN_US:
                    Setting.Lang = settingsLangEN_US;
                    btnLocaleLang.Image = Properties.Resources.mini_locale_en_us;
                    break;
                case settingsLangEN_SG:
                    Setting.Lang = settingsLangEN_SG;
                    btnLocaleLang.Image = Properties.Resources.mini_locale_en_us;
                    break;
                default:
                    //Setting.Lang = settingsLangEN_US;
                    btnLocaleLang.Image = Properties.Resources.mini_locale_unknown;
                    break;
            }
            ttLocale.SetToolTip(btnLocaleLang, Setting.Lang);

            btnLocaleLang.Refresh();
        }

        private void PicButGithub_MouseEnter(object sender, EventArgs e)
        {
            btnGithub.Image = Properties.Resources.GitHub_Logo_Only_Active;
        }

        private void PicButGithub_MouseLeave(object sender, EventArgs e)
        {
            btnGithub.Image = Properties.Resources.GitHub_Logo_Only;
        }

        private void PicButDiscord_MouseEnter(object sender, EventArgs e)
        {
            btnDiscord.Image = Properties.Resources.Discord_Logo_Only_Active;
        }

        private void PicButDiscord_MouseLeave(object sender, EventArgs e)
        {
            btnDiscord.Image = Properties.Resources.Discord_Logo_Only;
        }

        private void PicButGithub_Click(object sender, EventArgs e)
        {
            cmsGitHub.Show(btnGithub, new Point(0, btnGithub.Height));
            // Process.Start(urlAAEmuGitHub);
        }

        private void PicButDiscord_Click(object sender, EventArgs e)
        {
            bool hasCustom = ((Setting.ServerDiscordURL != null) && (Setting.ServerDiscordURL != ""));
            if ((Setting.ServerDiscordName != null) && (Setting.ServerDiscordName != ""))
                customServerDiscordMenuItem.Text = Setting.ServerDiscordName;
            customServerDiscordMenuItem.Visible = hasCustom;
            cmsDiscordS1.Visible = hasCustom;
            cmsDiscord.Show(btnDiscord, new Point(0, btnDiscord.Height));
            // Process.Start(urlDiscordInvite);
        }

        private void RegisterFileExt()
        {
            try
            {
                // Might also need to check
                // HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\
                FileAssociations.EnsureAssociationsSet();
                FileAssociations.EnsureURIAssociationsSet();
            }
            catch
            {
                // Set File or URI Association failed ?
            }
        }

        public bool IsInDefaultLocation(string exeFile)
        {
            return exeFile.ToLower().StartsWith(DefaultGameWorkingDirectory.ToLower());
        }

        private void LauncherForm_Load(object sender, EventArgs e)
        {
            // Grab my own version number from the assembly
            try
            {
                var AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string v = "";
                v += AppVer.Major.ToString();
                v += "." + AppVer.Minor.ToString();
                if ((AppVer.Build > 0) || (AppVer.MinorRevision > 0))
                    v += "." + AppVer.Build.ToString();
                if (AppVer.MinorRevision > 0)
                    v += "." + AppVer.MinorRevision.ToString();
                AppVersion = v;
            }
            catch
            {
                AppVersion = "0.0.0.0";
            }
            lAppVersion.Text = "V " + AppVersion;

            // Default install working folder is C:\ArcheAge\Working
            DefaultGameWorkingDirectory = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "ArcheAge", "Working");
            Application.UseWaitCursor = true;

            // Register File Extensions to the Launcher
            RegisterFileExt();

            // Register All Launchers
            AAEmuLauncherBase.RegisterLaunchers();
            // Add them to the popup menu for launcher types
            foreach (var l in AAEmuLauncherBase.AllLaunchers)
                if (l.ConfigName != string.Empty)
                    cmsAuthType.Items.Add(l.DisplayName, null, stAuthAuto_Click);

            // Load application settings
            LauncherFileSettings.SetDefaultSettings(Setting);
            // Load default language
            InitDefaultLanguage();

            imgBigNews.SizeMode = PictureBoxSizeMode.Zoom;
            imgBigNews.Invalidate();

            AppOpenMode = LauncherOpenMode.DefaultConfigFile;

            string openCommandLineSettingsFile = "";
            string openCommandLineURISettings = "";
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                // No additional possible settings yet, only check if a argument is a valid file
                if (File.Exists(arg) && (arg != Application.ExecutablePath))
                {
                    openCommandLineSettingsFile = arg;
                    AppOpenMode = LauncherOpenMode.SpecifiedConfigFile;
                }
                else
                {
                    if (arg.StartsWith(launcherProtocolSchema + "://"))
                    {
                        openCommandLineURISettings = arg;
                        AppOpenMode = LauncherOpenMode.OpenServerURI;
                    }
                }
            }

            // Load the saved clients reference file
            LoadClientLookup();

            // Try URI Mode if selected
            if (AppOpenMode == LauncherOpenMode.OpenServerURI)
            {
                try
                {
                    Uri u = new Uri(openCommandLineURISettings);
                    var encodedPath = u.AbsolutePath.Substring(1); // remove the first slash /
                    // Check if protocol is aelcf:// and query get parameters are ?v=c (protocol version is configfile)
                    if ((u.Scheme == launcherProtocolSchema) && u.Query.StartsWith("?v=c"))
                        URIConfigFileData = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(encodedPath));
                    URIConfigFileDataHost = u.Host;

                    LauncherFileSettings.SetDefaultSettings(Setting);
                    Setting.ServerIpAddress = u.Host;
                    Setting.ConfigName = u.Host;
                    if (u.UserInfo != string.Empty)
                    {
                        var userinfo = u.UserEscaped ? u.UserInfo : Uri.UnescapeDataString(u.UserInfo);
                        Setting.LastLoginUser = userinfo;
                        Setting.LastLoginPass = string.Empty;
                    }
                    bool isEncoded = false;
                    foreach(var seg in u.Segments)
                    {
                        var s = Uri.UnescapeDataString(seg);
                        if (s.Length <= 1)
                            continue;
                        var option = s.Substring(0,1);
                        var val = s.Substring(1);
                        val = val.TrimEnd('/');
                        if (isEncoded)
                            val = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(val));
                        switch(option)
                        {
                            case "A":
                                isEncoded = !isEncoded ;
                                break;
                            case "u": // User
                                Setting.LastLoginUser = val;
                                break;
                            case "p": // Pass
                                Setting.LastLoginPass = val;
                                break;
                            case "n": // Config Name to display
                                Setting.ConfigName = val;
                                break;
                            case "a": // Authentication Type
                                Setting.ClientLoginType = val; 
                                break;
                            case "w": // Website
                                Setting.ServerWebSiteURL = val;
                                break;
                            case "f": // Newsfeed
                                Setting.ServerNewsFeedURL = val;
                                break;
                            case "x": // Patch
                                Setting.ServerGameUpdateURL = val;
                                Setting.AllowGameUpdates = true;
                                break;
                        }
                    }
                    lLoadedConfig.Text = Setting.ConfigName;

                    UpdatePanelLabels();
                }
                catch
                {
                    URIConfigFileData = "";
                    openCommandLineURISettings = "";
                    AppOpenMode = LauncherOpenMode.Error; // Revert to default
                }
            }

            if (AppOpenMode == LauncherOpenMode.SpecifiedConfigFile)
            {
                if (!LoadSettingsFromFile(openCommandLineSettingsFile))
                {
                    openCommandLineSettingsFile = "";
                    AppOpenMode = LauncherOpenMode.Error;
                }

            }

            // Load local settings file if no external config specified, or if it failed to load
            if (AppOpenMode == LauncherOpenMode.DefaultConfigFile)
            {
                if (!LoadSettingsFromFile(GetDefaultSettingsFilePath()))
                {
                    AppOpenMode = LauncherOpenMode.Error;
                }
            }

            // In case of error, Default All The Things !
            if (AppOpenMode == LauncherOpenMode.Error)
            {
                LauncherFileSettings.SetDefaultSettings(Setting);
            }

            if ((Setting.PathToGame == null) || (Setting.PathToGame == "") || !File.Exists(Setting.PathToGame))
            {
                Setting.PathToGame = "";
                TryAutoFindGameExe();
            }
            if (string.IsNullOrWhiteSpace(Setting.AAPath))
                Setting.AAPath = Setting.PathToGame;
            if (string.IsNullOrWhiteSpace(Setting.AADownloadLocation))
                Setting.AADownloadLocation = string.IsNullOrWhiteSpace(Setting.ServerGameUpdateURL)
                    ? LauncherFileSettings.DefaultAAClientDownloadLocation
                    : Setting.ServerGameUpdateURL;
            if (string.IsNullOrWhiteSpace(Setting.AA30DownloadLocation))
                Setting.AA30DownloadLocation = LauncherFileSettings.DefaultAA30ClientDownloadLocation;
            if (string.IsNullOrWhiteSpace(Setting.WoWDownloadLocation))
                Setting.WoWDownloadLocation = LauncherFileSettings.DefaultWoWClientDownloadLocation;

            if ((GetAAPath() == "") || (!File.Exists(GetAAPath())) || IsInDefaultLocation(GetAAPath()))
            {
                // Show the main launcher with a primary install action when no valid client is configured.
                ShowPanelControls(ShowPanelType.Login);
            }
            else
            {
                ShowPanelControls(ShowPanelType.Login);
                if (eLogin.Text != "")
                {
                    ePassword.Focus();
                    ePassword.SelectionStart = 0;
                    ePassword.SelectionLength = 0;
                }
                else
                {
                    eLogin.Focus();
                    eLogin.SelectionStart = 0;
                    eLogin.SelectionLength = 0;
                }

            }

            if ((Setting.ServerNewsFeedURL != null) && (Setting.ServerNewsFeedURL != ""))
            {
                checkNews = true;
            }

            if ((Setting.ServerIpAddress != null) && (Setting.ServerIpAddress != ""))
            {
                nextServerCheck = 1000 * 1;
                // the next server check will put the wait cursor off
            }
            else
            {
                nextServerCheck = -1;
                Application.UseWaitCursor = false;
            }
            DoAutoLaunch = Setting.AutoLaunch;

            UpdatePanelLabels();
        }

        private bool IsGameInstalled()
        {
            var path = GetSelectedGamePath();
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        private void UpdateInstallStatus()
        {
            if (lInstallStatus == null || lClientUpdateAction == null)
                return;

            if (isClientDeltaUpdating)
            {
                lInstallStatus.Text = "Client update is running...";
                lClientUpdateAction.Enabled = false;
                lClientUpdateAction.ForeColor = ModernMutedText;
                lClientUpdateAction.Cursor = Cursors.WaitCursor;
                return;
            }

            if (IsGameInstalled())
            {
                lInstallStatus.Text = "Installed\n" + ClientDeltaUpdater.GetGameRoot(GetSelectedGamePath());
                var downloadLocation = GetSelectedDownloadLocation();
                var canCheckUpdates = IsManifestBasedDownloadLocation(downloadLocation);
                lClientUpdateAction.Enabled = true;
                lClientUpdateAction.Cursor = Cursors.Hand;
                lClientUpdateAction.ForeColor = canCheckUpdates ? ModernText : ModernMutedText;
            }
            else
            {
                lInstallStatus.Text = "Not installed\nChoose a folder and the launcher will handle the rest.";
                lClientUpdateAction.Enabled = false;
                lClientUpdateAction.ForeColor = ModernMutedText;
                lClientUpdateAction.Cursor = Cursors.No;
            }
        }

        private bool LoadClientLookup()
        {
            bool res = false;
            string configFileName = GetCachedUserFilePath(clientLookupDefaultFile);
            MigrateBundledUserFileToCache(clientLookupDefaultFile);

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(configFileName);
                var ConfigFile = reader.ReadToEnd();

                ClientLookup = JsonConvert.DeserializeObject<ClientLookupHelper>(ConfigFile);
                res = true;
            }
            catch
            {
                ClientLookup.ServerNames = new List<string>();
                ClientLookup.ClientLocations = new List<string>();
            }
            finally
            {
                // Make sure we close our stream so the file won't be in use when we need to save it
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return res;
        }

        private bool LoadSettingsFromFile(string configFileName)
        {
            bool res = false;

            if (!File.Exists(configFileName))
                return res;

            var fs = new FileStream(configFileName, FileMode.Open, FileAccess.Read);
            res = LoadSettingsFromStream(fs, configFileName);
            fs.Dispose();

            if (res)
                launcherOpenedConfigFile = configFileName;

            return res;
        }

        private string GetCachedUserFilePath(string fileName)
        {
            return Path.Combine(launcherLocalCacheFolder, fileName);
        }

        private string GetBundledUserFilePath(string fileName)
        {
            return Path.Combine(Application.StartupPath, fileName);
        }

        private string GetDefaultSettingsFilePath()
        {
            MigrateBundledUserFileToCache(launcherDefaultConfigFile);
            var cachedPath = GetCachedUserFilePath(launcherDefaultConfigFile);
            if (File.Exists(cachedPath))
                return cachedPath;

            var bundledPath = GetBundledUserFilePath(launcherDefaultConfigFile);
            return File.Exists(bundledPath) ? bundledPath : cachedPath;
        }

        private void MigrateBundledUserFileToCache(string fileName)
        {
            var cachedPath = GetCachedUserFilePath(fileName);
            if (File.Exists(cachedPath))
                return;

            var bundledPath = GetBundledUserFilePath(fileName);
            if (!File.Exists(bundledPath))
                return;

            try
            {
                Directory.CreateDirectory(launcherLocalCacheFolder);
                File.Copy(bundledPath, cachedPath, false);
            }
            catch
            {
                // If the cache cannot be written, keep using the bundled file as a fallback.
            }
        }

        private bool LoadSettingsFromString(string configDataString, string hostName)
        {
            bool res ;
            try
            {
                var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(configDataString));
                ms.Position = 0;
                res = LoadSettingsFromStream(ms, hostName + ".aelcf",hostName);
                ms.Dispose();
            }
            catch
            {
                res = false;
            }
            return res;
        }

        private void UpdatePanelLabels()
        {
            if (string.IsNullOrWhiteSpace(Setting.AA30DownloadLocation))
                Setting.AA30DownloadLocation = LauncherFileSettings.DefaultAA30ClientDownloadLocation;
            lLoadedConfig.Text = Setting.ConfigName;
            eServerIP.Text = Setting.ServerIpAddress;

            cbLoginList.Items.Clear();
            if (Setting.UserHistory != null)
                foreach (string s in Setting.UserHistory)
                    if (s != "")
                        cbLoginList.Items.Add(s);

            UpdateGameClientTypeLabel();

            ApplyLanguageToLauncher();
            UpdateLocaleLanguage();

            SetCustomCheckBox(cbSaveUser, Setting.SaveLoginAndPassword);
            SetCustomCheckBox(cbSkipIntro, Setting.SkipIntro);
            SetCustomCheckBox(cbHideSplash, Setting.HideSplashLogo);
            SetCustomCheckBox(cbUpdateLocale, Setting.UpdateLocale);
            SetCustomCheckBox(cbAllowUpdates, Setting.AllowGameUpdates);
            LoadSelectedGameFields();
            UpdateInstallStatus();
        }

        private bool LoadSettingsFromStream(Stream aStream, string configFileName, string defaultHost = "127.0.0.1")
        {
            bool res = false;
            Setting.ServerIpAddress = defaultHost;

            try
            {
                string ConfigFileData = "";
                using (var reader = new StreamReader(aStream))
                    ConfigFileData = reader.ReadToEnd();

                Setting = JsonConvert.DeserializeObject<LauncherFileSettings>(ConfigFileData);

                if (Setting == null)
                    Setting = new LauncherFileSettings();

                res = true;

                if (configFileName == GetDefaultSettingsFilePath() ||
                    configFileName == Path.Combine(Application.StartupPath, launcherDefaultConfigFile))
                {
                    lLoadedConfig.Text = "";
                }
                else if ((Setting.ConfigName == null) || (Setting.ConfigName == ""))
                {
                    lLoadedConfig.Text = Path.GetFileNameWithoutExtension(configFileName);
                }
                else
                {
                    lLoadedConfig.Text = Setting.ConfigName;
                }
            }
            catch
            {
                lLoadedConfig.Text = "";
                // If loading fails, just put in some defaults instead
                LauncherFileSettings.SetDefaultSettings(Setting);
                Setting.ServerIpAddress = defaultHost;
            }

            eLogin.Text = Setting.LastLoginUser;
            ePassword.Text = Setting.LastLoginPass;

            if (string.IsNullOrWhiteSpace(Setting.AAPath))
                Setting.AAPath = Setting.PathToGame;
            if (string.IsNullOrWhiteSpace(Setting.AADownloadLocation))
                Setting.AADownloadLocation = string.IsNullOrWhiteSpace(Setting.ServerGameUpdateURL)
                    ? LauncherFileSettings.DefaultAAClientDownloadLocation
                    : Setting.ServerGameUpdateURL;
            if (string.IsNullOrWhiteSpace(Setting.AA30DownloadLocation))
                Setting.AA30DownloadLocation = LauncherFileSettings.DefaultAA30ClientDownloadLocation;
            if (string.IsNullOrWhiteSpace(Setting.WoWDownloadLocation))
                Setting.WoWDownloadLocation = LauncherFileSettings.DefaultWoWClientDownloadLocation;

            if (((GetAADownloadLocation() == null)) || (GetAADownloadLocation() == ""))
            {
                Setting.AllowGameUpdates = false;
            }

            UpdatePanelLabels();

            return res;
        }

        public AAEmuLauncherBase CreateLauncherByName(string configName)
        {
            AAEmuLauncherBase res = null;
            foreach(var l in AAEmuLauncherBase.AllLaunchers)
            {
                if (l.ConfigName == configName)
                {
                    res = Activator.CreateInstance(l.LauncherClass) as AAEmuLauncherBase;
                    break;
                }
            }
            return res;
        }

        private string GuessDocumentsFolder(string archeAgeExe)
        {
            var res = "ArcheAge";
            // Note: AAFree and ArcheRage can actually not be detected in this way because the exe is encrypted (note all lowercase here to compare)
            var allowedFolders = new List<string>() { "archeage", "archeworld", "aaemu" };
            var documentConfigFile =
                Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "documents.folders");

            if (File.Exists(documentConfigFile))
                allowedFolders = File.ReadAllLines(documentConfigFile).ToList();

            var notAllowed = new List<string>() { ".", "-", "_" }; // should catch most things that reference a file instead of a directory
            var maxFolderSize = res.Length;
            foreach (var folder in allowedFolders)
            {
                if (folder.Length > maxFolderSize)
                    maxFolderSize = folder.Length;
            }
            maxFolderSize += 4; // to check against appended .exe text

            try
            {
                if (File.Exists(archeAgeExe))
                {
                    using (var fs = new FileStream(archeAgeExe, FileMode.Open, FileAccess.Read))
                    {
                        using (var ms = new MemoryStream())
                        {
                            fs.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            var buffer = new byte[maxFolderSize];
                            foreach (var folder in allowedFolders)
                            {
                                for (var pos = 0; pos < ms.Length - maxFolderSize; pos++)
                                {
                                    ms.Seek(pos, SeekOrigin.Begin);
                                    var streamBytes = ms.Read(buffer, 0, maxFolderSize);

                                    var streamString = Encoding.ASCII.GetString(buffer, 0, streamBytes);
                                    var checkString = streamString.ToLower();
                                    if (checkString.StartsWith(folder))
                                    {
                                        var isOk = true;
                                        foreach (var notString in notAllowed)
                                        {
                                            if (checkString.StartsWith(folder + notString))
                                            {
                                                isOk = false;
                                                break;
                                            }
                                        }

                                        if (isOk)
                                        {
                                            res = streamString.Substring(0, folder.Length);
                                            return res;
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch
            {
                // Do Nothing
            }

            // If no data could be found inside the .exe, then, use then assume the exe's filename as a folder
            res = Path.GetFileNameWithoutExtension(archeAgeExe);

            // MessageBox.Show($"Could not guess documents folder for {archeAgeExe}, configuration file might not be updated correctly!","Detect Folder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return res;
        }

        private void StartGame()
        {
            ApplySelectedGameToLegacySettings();

            if (selectedGameId == "jw")
            {
                StartSelectedExecutable();
                return;
            }

            if (Setting.PathToGame != "")
            {
                if (eLogin.Text != "" && ePassword.Text != "")
                {

                    string serverIP = Setting.ServerIpAddress;
                    ushort serverPort = defaultAuthPort;

                    var splitPos = eServerIP.Text.IndexOf(":");
                    if (splitPos >= 0)
                    {
                        serverIP = eServerIP.Text.Substring(0, splitPos);
                        if (!ushort.TryParse(eServerIP.Text.Substring(splitPos + 1), out serverPort))
                        {
                            serverPort = defaultAuthPort;
                        }
                    }

                    if (Setting.SaveLoginAndPassword)
                        SaveSettings();

                    // Clean up previous instance
                    if (aaLauncher != null)
                    {
                        aaLauncher.Dispose();
                        aaLauncher = null;
                    }


                    aaLauncher = CreateLauncherByName(Setting.ClientLoginType);
                    if (aaLauncher == null)
                    {
                        MessageBox.Show(string.Format(L.UnknownLauncherProtocol, Setting.ClientLoginType), "Launcher");
                        return;
                    }

                    UpdateGameSystemConfigFile(GuessDocumentsFolder(Setting.PathToGame), Setting.UpdateLocale, Setting.Lang, Setting.SkipIntro);

                    aaLauncher.UserName = eLogin.Text;
                    aaLauncher.SetPassword(ePassword.Text);
                    aaLauncher.LoginServerAdress = serverIP;
                    aaLauncher.LoginServerPort = serverPort;
                    aaLauncher.GameExeFilePath = Setting.PathToGame;
                    // if (Setting.UpdateLocale)
                        aaLauncher.Locale = Setting.Lang;
                    aaLauncher.HShieldArgs = "+acpxmk";

                    if (Setting.HideSplashLogo)
                        aaLauncher.ExtraArguments += "-nosplash";

                    aaLauncher.InitializeForLaunch();

                    if (debugModeToolStripMenuItem.Checked)
                    {
                        DebugHelperForm dlg = new DebugHelperForm();
                        dlg.eArgs.Text = aaLauncher.LaunchArguments;
                        dlg.eHackShieldArg.Text = aaLauncher.HShieldArgs;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            aaLauncher.LaunchArguments = dlg.eArgs.Text;
                            aaLauncher.HShieldArgs = dlg.eHackShieldArg.Text;
                        }
                        dlg.Dispose();
                    }

                    var startOK = aaLauncher.Launch();
                    if (startOK)
                    {
                        WindowState = FormWindowState.Minimized;
                        if (!aaLauncher.FinalizeLaunch())
                        {
                            WindowState = FormWindowState.Normal;
                        }
                        else
                        {
                            checkGameIsRunning = true;
                        }
                    }


                }
                else
                {
                    MessageBox.Show(L.NoUserOrPassword);
                    // MessageBox.Show("Логин и пароль должны быть заполнены!");
                }
            }
            else
            {
                MessageBox.Show(L.MissingGame);
                // MessageBox.Show("Не указан путь размещения клиента игры!");
            }

        }

        private void ButSettingSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            ShowPanelControls(0); // Show Login
        }

        private void ButSettingCancel_Click(object sender, EventArgs e)
        {
            ShowPanelControls(0); // Show Login
        }

        private void LauncherForm_MouseDown(object sender, MouseEventArgs e)
        {
            // Code for form drag
            formMouseDown = true;
            lastLocation = e.Location;
        }

        private void LauncherForm_MouseMove(object sender, MouseEventArgs e)
        {
            // Code for form drag
            if (formMouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void LauncherForm_MouseUp(object sender, MouseEventArgs e)
        {
            // Code for form drag
            formMouseDown = false;
        }

        private void txtLogin_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //lFakePassword.Hide();
                ePassword.Show();
                ePassword.Focus();
                ePassword.SelectAll();
                //eLogin.Hide();
                //cbLoginList.Hide();
            }
        }

        private void txtPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnPlay_Click(null, null);
            }

        }

        private void LauncherForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CancelPatching = true;
            SaveSettings();
            try
            {
                PatchDownloadPak?.ClosePak();
            }
            catch
            {
                //
            }

            try
            {
                pak?.ClosePak();
            }
            catch
            {
                //
            }
        }

        private void SaveClientLookups()
        {
            var configFileName = GetCachedUserFilePath(clientLookupDefaultFile);

            if (!string.IsNullOrEmpty(Setting.ConfigName))
            {
                string findExe = TryAutoFindFromLookup();
                if ((findExe == "") && File.Exists(Setting.PathToGame))
                {
                    ClientLookup.ServerNames.Add(Setting.ConfigName);
                    ClientLookup.ClientLocations.Add(Setting.PathToGame);
                }
                else
                {
                    return;
                }
                var clientLookupJson = JsonConvert.SerializeObject(ClientLookup, Formatting.Indented);
                try
                {
                    Directory.CreateDirectory(launcherLocalCacheFolder);
                    File.WriteAllText(configFileName, clientLookupJson);
                }
                catch
                {
                    // Unable to save client lookup data
                }
            }
        }

        private void SaveSettings()
        {
            SaveSelectedGameFields();
            Setting.ConfigVersion = 1;
            ApplySelectedGameToLegacySettings();
            // Try saving lookups after we set the path
            SaveClientLookups();

            Setting.ServerIpAddress = eServerIP.Text;
            // Setting.SaveLoginAndPassword = cbSaveLogin.Checked.ToString();
            // Setting.SkipIntro = cbSkipIntro.Checked.ToString();
            // Setting.HideSplashLogo = cbHideSplashLogo.Checked.ToString();

            if (Setting.SaveLoginAndPassword)
            {
                Setting.LastLoginUser = eLogin.Text;
                // TODO: Save the password in a somewhat more safe way
                Setting.LastLoginPass = ePassword.Text;
                // Setting.LastLoginPass = ""; // don't save password, unsafe
                if ((eLogin.Text != "") && (!cbLoginList.Items.Contains(eLogin.Text)))
                {
                    cbLoginList.Items.Add(eLogin.Text);
                }
            }
            else
            {
                Setting.LastLoginUser = "";
                Setting.LastLoginPass = "";
                cbLoginList.Items.Clear(); // Also delete history if we put off the save option
            }
            Setting.UserHistory = new List<string>();
            Setting.UserHistory.Clear();
            foreach (Object o in cbLoginList.Items)
                Setting.UserHistory.Add(cbLoginList.GetItemText(o));

            if (AppOpenMode != LauncherOpenMode.OpenServerURI)
            {
                // Save settings to disk unless it was opened from a URI
                var SettingsJsonData = JsonConvert.SerializeObject(Setting, Formatting.Indented);

                if ((launcherOpenedConfigFile == null) || (launcherOpenedConfigFile == "") || (File.Exists(launcherOpenedConfigFile) == false))
                {
                    launcherOpenedConfigFile = GetDefaultSettingsFilePath();
                }
                try
                {
                    var settingsDirectory = Path.GetDirectoryName(launcherOpenedConfigFile);
                    if (!string.IsNullOrWhiteSpace(settingsDirectory))
                        Directory.CreateDirectory(settingsDirectory);
                    File.WriteAllText(launcherOpenedConfigFile, SettingsJsonData);
                }
                catch
                {
                    // Unable to save settings
                }
            }
        }

        private void txtLoginList_SelectedValueChanged(object sender, EventArgs e)
        {
            eLogin.Text = cbLoginList.Text;
            //lFakeUser.Text = cbLoginList.Text;
            cbLoginList.Hide();
        }

        private void txtLogin_Leave(object sender, EventArgs e)
        {
            eLogin.Hide();
            //lFakeUser.Text = eLogin.Text;
            //lFakeUser.Show();
        }

        private void txtPassword_Leave(object sender, EventArgs e)
        {
            ePassword.Hide();
            //lFakePassword.Text = new String('●', ePassword.TextLength);
            //lFakePassword.Show();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (!IsGameInstalled())
            {
                lDownloadClient_Click(sender, e);
                return;
            }

            if (selectedGameId == "jw")
            {
                StartSelectedExecutable();
                return;
            }

            switch (serverCheckStatus)
            {
                case serverCheck.Update:
                    if ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != ""))
                    {
                        StartUpdate();
                    }
                    else
                    {
                        MessageBox.Show(L.NoUpdateURL, "No update URL");
                        serverCheckStatus = serverCheck.Unknown;
                        nextServerCheck = 1000;
                    }
                    break;
                case serverCheck.Updating:
                    // Do nothing with this button while we are updating
                    break;
                default:
                    // Start game if status is unknown, online or offline
                    Application.UseWaitCursor = true;
                    StartGame();
                    Application.UseWaitCursor = false;
                    break;
            }
        }

        private void btnPlay_MouseEnter(object sender, EventArgs e)
        {
            UpdatePlayButton(serverCheckStatus, true);
        }

        private void btnPlay_MouseLeave(object sender, EventArgs e)
        {
            UpdatePlayButton(serverCheckStatus, false);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowPanelControls(ShowPanelType.Settings); // Show settings
        }

        private void btnWebsite_Click(object sender, EventArgs e)
        {
            if ((Setting.ServerWebSiteURL != null) && (Setting.ServerWebSiteURL != ""))
            {
                Process.Start(Setting.ServerWebSiteURL);
            }
            else
            {
                Process.Start(urlWebsite);
            }
        }

        private void LauncherForm_BackgroundImageChanged(object sender, EventArgs e)
        {
            Invalidate(true);
        }

        private void btnSystem_Click(object sender, EventArgs e)
        {
            forcePatchDownloadToolStripMenuItem.Enabled = ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != ""));
            skipPatchToolStripMenuItem.Enabled = ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != ""));
            cmsAAEmuButton.Show(btnSystem, new Point(0, btnSystem.Height));
        }

        private void lSettingsBack_Click(object sender, EventArgs e)
        {
            SaveSettings();
            if ((serverCheckStatus == serverCheck.Online) || (serverCheckStatus == serverCheck.Offline) || (serverCheckStatus == serverCheck.Unknown))
            {
                serverCheckStatus = serverCheck.Unknown;
                nextServerCheck = 1000;
                ShowPanelControls(0);
            }
            if (serverCheckStatus == serverCheck.Update)
            {
                // Allow re-checking if settings changed (or not)
                serverCheckStatus = serverCheck.Unknown;
                nextServerCheck = 1000;
                ShowPanelControls(0);
            }
            if (serverCheckStatus == serverCheck.Updating)
            {
                ShowPanelControls(ShowPanelType.UpdatePatch);
            }
            UpdatePlayButton(serverCheckStatus, false);
        }

        private void GuessAndUpdateClientType()
        {
            if (selectedGameId != "aa")
            {
                lGameClientType.Text = "JasonWoW";
                lGameClientType.ForeColor = ModernAccent;
                return;
            }

            Application.UseWaitCursor = true;
            using (var puf = new InfoPopupForm())
            {
                puf.lInfo.Text = "Detecting version, please wait ...";
                puf.Show();
                puf.Location = new Point(Location.X + ((Size.Width - puf.Size.Width) / 2), Location.Y + ((Size.Height - puf.Size.Height) / 2));
                puf.Refresh();
                var oldType = Setting.ClientLoginType;
                Setting.ClientLoginType = AAAutoDetectClient.GuessLauncher(GetAAPath());
                UpdateGameClientTypeLabel();
                if (oldType != Setting.ClientLoginType)
                    lGameClientType.ForeColor = Color.FromArgb(250, 210, 90);
            }
            Application.UseWaitCursor = false;
            ApplySelectedGameToLegacySettings();
        }

        private string ResolveGamePathSelection(string selectedPath)
        {
            if (string.IsNullOrWhiteSpace(selectedPath) ||
                !string.Equals(Path.GetExtension(selectedPath), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return selectedPath;
            }

            var shortcutTarget = ResolveShortcutTarget(selectedPath);
            if (string.IsNullOrWhiteSpace(shortcutTarget))
                throw new InvalidOperationException("The selected shortcut does not point to a game executable.");
            if (!File.Exists(shortcutTarget))
                throw new FileNotFoundException("The selected shortcut target was not found.", shortcutTarget);

            return shortcutTarget;
        }

        private string ResolveShortcutTarget(string shortcutPath)
        {
            object shell = null;
            object shortcut = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return string.Empty;

                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { shortcutPath });

                return shortcut.GetType().InvokeMember("TargetPath",
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    shortcut,
                    null) as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                try
                {
                    if (shortcut != null && Marshal.IsComObject(shortcut))
                        Marshal.FinalReleaseComObject(shortcut);
                    if (shell != null && Marshal.IsComObject(shell))
                        Marshal.FinalReleaseComObject(shell);
                }
                catch
                {
                    // Best effort COM cleanup.
                }
            }
        }

        private void lGamePath_Click(object sender, EventArgs e)
        {
            if (serverCheckStatus == serverCheck.Updating)
            {
                return;
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var currentPath = GetSelectedGamePath();
            if (currentPath != "")
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(currentPath);
            }
            if (openFileDialog.InitialDirectory == "")
            {
                openFileDialog.InitialDirectory = Path.Combine(DefaultGameWorkingDirectory,"bin32");
            }
            openFileDialog.Filter = selectedGameId == "jw"
                ? "World of Warcraft|Wow*.exe|Shortcuts|*.lnk|Executable|*.exe|All files (*.*)|*.*"
                : "ArcheAge|arche*.exe|Shortcuts|*.lnk|ArcheAge Game|" + archeAgeEXE + "|ArcheWorld Game|" + archeWorldEXE + "| Executeable |*.exe|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            while (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath;
                try
                {
                    selectedPath = ResolveGamePathSelection(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, L.PathToGame, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                if (IsInDefaultLocation(selectedPath))
                {
                    var res = MessageBox.Show(L.DefaultLocationWarning, L.PathToGame, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (res == DialogResult.No)
                        continue;
                    if (res == DialogResult.Cancel)
                        break;
                }
                //Get the path of specified file
                SetSelectedGamePath(selectedPath);
                lGamePath.Text = GetSelectedGamePath();
                GuessAndUpdateClientType();
                break;
            }
        }

        private void lDownloadClient_Click(object sender, EventArgs e)
        {
            if (serverCheckStatus == serverCheck.Updating)
                return;

            SaveSelectedGameFields();
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Choose a folder to install " + SelectedGameDisplayName + " into";
                if (!string.IsNullOrEmpty(DefaultGameWorkingDirectory))
                    folderDialog.SelectedPath = DefaultGameWorkingDirectory;

                if (folderDialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var downloadLocation = GetSelectedDownloadLocation();
                if (IsMegaDownloadLocation(downloadLocation))
                {
                    if (ClientDownloadManager.FindMegaGetExecutable() == null)
                    {
                        var installMegaCmd = MessageBox.Show(this,
                            "MEGA downloads use MEGAcmd so the launcher can download and extract the client automatically.\r\n\r\nInstall MEGAcmd from MEGA now?",
                            "MEGAcmd Required", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (installMegaCmd == DialogResult.Yes)
                            Process.Start(new ProcessStartInfo(ClientDownloadManager.MegaCmdDownloadPage) { UseShellExecute = true });
                        return;
                    }

                    using (var dlg = new ClientDownloadForm(folderDialog.SelectedPath, downloadLocation))
                    {
                        var result = dlg.ShowDialog(this);
                        if (result == DialogResult.OK && !string.IsNullOrEmpty(dlg.DetectedExePath))
                        {
                            SetSelectedGamePath(dlg.DetectedExePath);
                            lGamePath.Text = GetSelectedGamePath();
                            Setting.ClientLoginType = stringTrino_3_0;
                            ApplySelectedGameToLegacySettings();
                            UpdateGameClientTypeLabel();
                            SaveSettings();
                            UpdateInstallStatus();
                            UpdatePlayButton(serverCheckStatus, false);
                            MessageBox.Show(this, SelectedGameDisplayName + " downloaded and installed successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    return;
                }

                if (IsGoogleDriveDownloadLocation(downloadLocation))
                {
                    using (var dlg = new ClientDownloadForm(
                        folderDialog.SelectedPath,
                        expectedArchiveFileName: ClientDownloadManager.AA30ArchiveFileName,
                        googleDriveDownloadLocation: downloadLocation))
                    {
                        var result = dlg.ShowDialog(this);
                        if (result == DialogResult.OK && !string.IsNullOrEmpty(dlg.DetectedExePath))
                        {
                            SetSelectedGamePath(dlg.DetectedExePath);
                            lGamePath.Text = GetSelectedGamePath();
                            Setting.ClientLoginType = stringTrino_3_0;
                            ApplySelectedGameToLegacySettings();
                            UpdateGameClientTypeLabel();
                            SaveSettings();
                            UpdateInstallStatus();
                            UpdatePlayButton(serverCheckStatus, false);
                            MessageBox.Show(this, SelectedGameDisplayName + " downloaded and installed successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    return;
                }

                if (IsDirectArchiveDownloadLocation(downloadLocation))
                {
                    var archiveFileName = GetDownloadArchiveFileName(downloadLocation);
                    using (var dlg = new ClientDownloadForm(
                        folderDialog.SelectedPath,
                        expectedArchiveFileName: archiveFileName,
                        directArchiveDownloadLocation: downloadLocation))
                    {
                        var result = dlg.ShowDialog(this);
                        if (result == DialogResult.OK && !string.IsNullOrEmpty(dlg.DetectedExePath))
                        {
                            SetSelectedGamePath(dlg.DetectedExePath);
                            lGamePath.Text = GetSelectedGamePath();
                            ApplySelectedGameToLegacySettings();
                            LoadSelectedGameFields();
                            SaveSettings();
                            UpdateInstallStatus();
                            UpdatePlayButton(serverCheckStatus, false);
                            MessageBox.Show(this, SelectedGameDisplayName + " downloaded and installed successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    return;
                }

                if (IsManifestBasedDownloadLocation(downloadLocation))
                {
                    InstallFromManifestLocation(folderDialog.SelectedPath, downloadLocation);
                    return;
                }

                if (!IsArcheAgeSelected)
                {
                    MessageBox.Show(this, "Set a WoW Download Location URL before installing JasonWoW.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var dlg = new ClientDownloadForm(folderDialog.SelectedPath))
                {
                    var result = dlg.ShowDialog(this);
                    if (result == DialogResult.OK && !string.IsNullOrEmpty(dlg.DetectedExePath))
                    {
                        SetSelectedGamePath(dlg.DetectedExePath);
                        lGamePath.Text = GetSelectedGamePath();
                        // The client from this Google Drive package is always the same known AAEmu test client
                        // (r208022, ArcheAge 1.2 protocol) - the world.xml date heuristic in GuessLauncher() is
                        // unreliable for repacked test clients, so set it directly instead of guessing.
                        Setting.ClientLoginType = stringTrino_1_2;
                        ApplySelectedGameToLegacySettings();
                        UpdateGameClientTypeLabel();
                        SaveSettings();
                        UpdateInstallStatus();
                        UpdatePlayButton(serverCheckStatus, false);
                        MessageBox.Show(this, "Game client downloaded and installed successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (result == DialogResult.Abort)
                    {
                        // Error already shown by ClientDownloadForm
                    }
                }
            }
        }

        private async void InstallFromManifestLocation(string destinationFolder, string downloadLocation)
        {
            try
            {
                isClientDeltaUpdating = true;
                lClientUpdateAction.Text = "Installing...";
                lInstallStatus.Text = "Installing " + SelectedGameDisplayName + "...";

                var plan = await ClientDeltaUpdater.CheckForUpdatesInFolderAsync(downloadLocation, destinationFolder);
                var progress = new Progress<ClientDeltaProgress>(p =>
                {
                    lInstallStatus.Text = $"Installing {p.CurrentFile}/{p.TotalFiles}\n{p.FileName}";
                });
                await ClientDeltaUpdater.ApplyUpdateToFolderAsync(downloadLocation, destinationFolder, plan, progress);

                var exe = FindSelectedGameExecutable(destinationFolder);
                if (string.IsNullOrWhiteSpace(exe))
                    throw new FileNotFoundException("Install finished, but no game executable was found in the selected folder.");

                SetSelectedGamePath(exe);
                lGamePath.Text = GetSelectedGamePath();
                SaveSettings();
                UpdateInstallStatus();
                UpdatePlayButton(serverCheckStatus, false);
                MessageBox.Show(this, SelectedGameDisplayName + " installed successfully.", "Install Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Install Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isClientDeltaUpdating = false;
                lClientUpdateAction.Text = "Check Client Updates";
                UpdateInstallStatus();
            }
        }

        private string FindSelectedGameExecutable(string destinationFolder)
        {
            var names = selectedGameId == "jw"
                ? new[] { "Wow.exe", "WowClassic.exe", "Wow-64.exe" }
                : new[] { archeAgeEXE, archeWorldEXE };

            foreach (var name in names)
            {
                try
                {
                    var match = Directory.EnumerateFiles(destinationFolder, name, SearchOption.AllDirectories).FirstOrDefault();
                    if (match != null)
                        return match;
                }
                catch
                {
                    // Ignore inaccessible folders.
                }
            }

            try
            {
                return Directory.EnumerateFiles(destinationFolder, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private void StartSelectedExecutable()
        {
            var exePath = GetSelectedGamePath();
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show(SelectedGameDisplayName + " path is not set.");
                return;
            }

            try
            {
                if (selectedGameId == "jw")
                    UpdateWoWRealmlist(exePath);

                Process.Start(new ProcessStartInfo(exePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                });
                WindowState = FormWindowState.Minimized;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateWoWRealmlist(string exePath)
        {
            var installRoot = Path.GetDirectoryName(exePath);
            if (string.IsNullOrWhiteSpace(installRoot))
                throw new InvalidOperationException("Could not determine the WoW install location.");

            var dataFolder = Path.Combine(installRoot, "data");
            var localeFolder = Path.Combine(dataFolder, "enus");
            if (Directory.Exists(dataFolder))
            {
                var existingLocaleFolder = Directory
                    .EnumerateDirectories(dataFolder)
                    .FirstOrDefault(d => string.Equals(Path.GetFileName(d), "enus", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(existingLocaleFolder))
                    localeFolder = existingLocaleFolder;
            }

            Directory.CreateDirectory(localeFolder);
            File.WriteAllText(Path.Combine(localeFolder, "realmlist.wtf"), "set realmlist " + GetServerAddressHost() + Environment.NewLine);
        }

        private string GetServerAddressHost()
        {
            var serverAddress = eServerIP.Text.Trim();
            if (string.IsNullOrWhiteSpace(serverAddress))
                serverAddress = Setting.ServerIpAddress;

            var splitPos = serverAddress.IndexOf(":");
            if (splitPos > 0)
                serverAddress = serverAddress.Substring(0, splitPos);

            if (string.IsNullOrWhiteSpace(serverAddress))
                throw new InvalidOperationException("Set a server address before launching WoW.");

            return serverAddress;
        }

        private async void LClientUpdateAction_Click(object sender, EventArgs e)
        {
            if (isClientDeltaUpdating)
                return;

            if (!IsGameInstalled())
            {
                lDownloadClient_Click(sender, e);
                return;
            }

            var downloadLocation = GetSelectedDownloadLocation();
            var gamePath = GetSelectedGamePath();
            if (string.IsNullOrWhiteSpace(downloadLocation))
            {
                MessageBox.Show(this, "No " + SelectedGameDisplayName + " Download Location URL is configured.", "Client Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!IsManifestBasedDownloadLocation(downloadLocation))
            {
                MessageBox.Show(this,
                    "Client update checks need a manifest-based Download Location URL, such as a web folder or GitHub tree containing client/manifest.json. This download location opens in your browser and does not provide update manifests.",
                    "Client Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                isClientDeltaUpdating = true;
                lClientUpdateAction.Text = "Checking...";
                UpdateInstallStatus();

                var plan = await ClientDeltaUpdater.CheckForUpdatesAsync(downloadLocation, gamePath);
                if (plan.ChangedFiles.Count == 0 && plan.RemovedFiles.Count == 0)
                {
                    MessageBox.Show(this, "The installed client already matches the latest client manifest.", "Client Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var sizeMb = Math.Max(1, plan.DownloadSize / 1024 / 1024);
                var confirm = MessageBox.Show(this,
                    $"Client update {plan.Manifest.Version} is available.\n\nDownload {plan.ChangedFiles.Count} changed file(s), about {sizeMb} MB?",
                    "Client Updates", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                lClientUpdateAction.Text = "Updating...";
                var progress = new Progress<ClientDeltaProgress>(p =>
                {
                    lInstallStatus.Text = $"Updating {p.CurrentFile}/{p.TotalFiles}\n{p.FileName}";
                });

                await ClientDeltaUpdater.ApplyUpdateAsync(downloadLocation, gamePath, plan, progress);
                MessageBox.Show(this, "Client files updated successfully.", "Client Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Client Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isClientDeltaUpdating = false;
                lClientUpdateAction.Text = "Check Client Updates";
                UpdateInstallStatus();
                UpdatePlayButton(serverCheckStatus, false);
            }
        }

        private void cbHideSplash_Click(object sender, EventArgs e)
        {
            Setting.HideSplashLogo = ToggleSettingCheckBox(cbHideSplash, Setting.HideSplashLogo);
        }

        private void cbSkipIntro_Click(object sender, EventArgs e)
        {
            Setting.SkipIntro = ToggleSettingCheckBox(cbSkipIntro, Setting.SkipIntro);
        }

        private void cbSaveUser_Click(object sender, EventArgs e)
        {
            Setting.SaveLoginAndPassword = ToggleSettingCheckBox(cbSaveUser, Setting.SaveLoginAndPassword);
        }

        private void cbLoginList_SelectedIndexChanged(object sender, EventArgs e)
        {
            eLogin.Text = cbLoginList.Text;
            ePassword.Text = "";
        }

        private void cbUpdateLocale_Click(object sender, EventArgs e)
        {
            Setting.UpdateLocale = ToggleSettingCheckBox(cbUpdateLocale, Setting.UpdateLocale);
        }

        private void UpdateGameClientTypeLabel()
        {
            foreach (var l in AAEmuLauncherBase.AllLaunchers)
                if (l.ConfigName == Setting.ClientLoginType)
                {
                    lGameClientType.Text = l.DisplayName;
                    return;
            }
            lGameClientType.Text = "???: " + Setting.ClientLoginType;
            lGameClientType.ForeColor = ModernAccent;
        }

        private void lGameClientType_Click(object sender, EventArgs e)
        {
            if (sender is Label l)
                cmsAuthType.Show(l, new Point(0,l.Height)); // Popup below
            /*
            // toggle client types
            switch (Setting.ClientLoginType)
            {
                case stringMailRu_1_0:
                    Setting.ClientLoginType = stringTrino_1_2;
                    break;
                case stringTrino_1_2:
                    Setting.ClientLoginType = stringTrino_3_0;
                    break;
                case stringTrino_3_0:
                    Setting.ClientLoginType = stringTrino_6_0;
                    break;
                case stringTrino_6_0:
                default:
                    Setting.ClientLoginType = stringMailRu_1_0;
                    break;
            }

            updateGameClientTypeLabel();
            */
        }

        private void ClearArcheAgeCache(bool clearShaders)
        {
            // C:\ArcheAge\Documents => UserHomeFolder\ArcheAge

            var folder = File.Exists(Setting.PathToGame) ? GuessDocumentsFolder(Setting.PathToGame) : "ArcheAge";

            string aaDocumentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folder);

            if (MessageBox.Show(L.DeleteShaderCache, L.DeleteShaderCacheTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            string[] rootDirs = Directory.GetDirectories(aaDocumentsFolder);
            foreach (string dir in rootDirs)
            {
                if (Path.GetFileName(dir).StartsWith("USER", true, null))
                {
                    var di = new DirectoryInfo(dir + Path.DirectorySeparatorChar + "shaders");
                    EmptyFolder(di, false);
                }
            }
        }

        private void UpdateGameSystemConfigFile(string documentsFolderName, bool enableUpdateLocale, string locale, bool enableSkipIntro)
        {
            // C:\ArcheAge\Documents => UserHomeFolder\ArcheAge
            string configFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), documentsFolderName, archeAgeSystemConfigFileName);
            const string localeField = "locale = ";
            const string movieField = "login_first_movie = ";
            const string optionSoundField = "option_sound = ";
            const string DXField = "r_driver = ";

            List<string> lines = new List<string>();
            List<string> newLines = new List<string>();

            bool updatedLocale = false;
            bool updatedMovie = false;
            bool hasSoundOptionSetting = false;
            bool hasDXSetting = false;

            try
            {
                if (File.Exists(configFileName) == true)
                {
                    lines = File.ReadAllLines(configFileName).ToList();

                    foreach (string line in lines)
                    {
                        if ((enableUpdateLocale == true) && (line.IndexOf(localeField) >= 0))
                        {
                            // replace here
                            if (updatedLocale == false)
                            {
                                newLines.Add(localeField + locale);
                            }
                            updatedLocale = true;
                        }
                        else
                        if ((enableSkipIntro == true) && (line.IndexOf(movieField) >= 0))
                        {
                            // replace here
                            if (updatedMovie == false)
                            {
                                newLines.Add(movieField + "1");
                            }
                            updatedMovie = true;
                        }
                        else
                        {
                            newLines.Add(line);
                        }
                        if (line.IndexOf(optionSoundField) >= 0)
                        {
                            hasSoundOptionSetting = true;
                        }
                        if (line.IndexOf(DXField) >= 0)
                        {
                            hasDXSetting = true;
                        }

                    }
                }

                // Hack to make sure people can get past the server select on their first run
                // Apperantly missing the option_sound in the system.cfg will make it so the charater
                // select screen crashes
                if (hasSoundOptionSetting == false)
                {
                    newLines.Add(optionSoundField + "4");
                }

                // Default to DX11 (DX10 in settings)
                // DX9 can sometimes give problems that makes you fail to load the character select screen
                // We'll also change to windowed 1280x768, most things should be able to handle this
                if (hasDXSetting == false)
                {
                    newLines.Add(DXField + "\"DX10\"");
                    var x = (Screen.PrimaryScreen.WorkingArea.Width - 1280) / 2;
                    var y = (Screen.PrimaryScreen.WorkingArea.Height - 768) / 2;
                    newLines.Add("r_windowx = " + x.ToString());
                    newLines.Add("r_windowy = " + y.ToString());
                    newLines.Add("r_width = 1280");
                    newLines.Add("r_height = 768");
                    newLines.Add("r_fullscreen = 0");
                    newLines.Add("r_vsync = 0");
                }

                // Add our settings if needed
                if ((enableSkipIntro == true) && (updatedMovie == false))
                {
                    newLines.Add(movieField + "1");
                }
                if ((enableUpdateLocale == true) && (updatedLocale == false))
                {
                    newLines.Add(localeField + locale);
                }

                // Create the folder if needed
                if (!Directory.Exists(Path.GetDirectoryName(configFileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(configFileName));
                File.WriteAllLines(configFileName, newLines);
            }
            catch (Exception x)
            {
                MessageBox.Show(string.Format(L.ErrorUpdatingFile, configFileName)+"\n"+x.Message, "UpdateGameSystemConfigFile()");
            }

        }

        private void aAEmuServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(urlAAEmuGitHub);
        }

        private void aAEmuLauncherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(urlLauncherGitHub);
        }

        private void cbAllowUpdates_Click(object sender, EventArgs e)
        {
            if ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != ""))
            {
                Setting.AllowGameUpdates = ToggleSettingCheckBox(cbAllowUpdates, Setting.AllowGameUpdates);
            }
            else
            {
                Setting.AllowGameUpdates = false;
                SetCustomCheckBox(cbAllowUpdates, Setting.AllowGameUpdates);
            }
        }

        private string TryAutoFindInDir(string dirName, string[] fileNames, int depth)
        {
            pb1.PerformStep();
            pb1.Refresh();

            // Don't search too deep, 5 deep should be enough to be in range of what we want
            if (depth >= 5)
            {
                return "";
            }

            DirectoryInfo di = new DirectoryInfo(dirName);
            foreach (var fileName in fileNames)
            {
                FileInfo[] files = di.GetFiles(fileName);
                foreach (FileInfo fi in files)
                {
                    if (fi.Name.ToLower() == fileName.ToLower() && (File.Exists(dirName + fi.Name)))
                    {
                        return dirName + fi.Name;
                    }
                }

                DirectoryInfo[] dirs = di.GetDirectories();
                pb1.Maximum = pb1.Maximum + dirs.Length;
                foreach (DirectoryInfo downDir in dirs)
                {
                    string dirRes = TryAutoFindInDir(dirName + downDir.Name + Path.DirectorySeparatorChar, fileNames,
                        depth + 1);
                    if (dirRes != "")
                    {
                        return dirRes;
                    }
                }
            }

            return "";
        }

        private string TryAutoFindFromLookup()
        {
            if (ClientLookup.ServerNames.Count != ClientLookup.ClientLocations.Count)
            {
                ClientLookup.ServerNames.Clear();
                ClientLookup.ClientLocations.Clear();
            }
            for (int i = 0; i < ClientLookup.ServerNames.Count(); i++)
            {
                if (ClientLookup.ServerNames[i] == Setting.ConfigName)
                {
                    return ClientLookup.ClientLocations[i];
                }
            }
            return "";
        }

        private void TryAutoFindGameExe()
        {
            Application.UseWaitCursor = true;

            string exeFile = "";

            if ((Setting.ConfigName != null) && (Setting.ConfigName != ""))
            {
                // If we loaded a named config, try to pick from a saved list first
                exeFile = TryAutoFindFromLookup();
                if (exeFile != "")
                {
                    Setting.PathToGame = exeFile;
                    Setting.AAPath = exeFile;
                    lGamePath.Text = GetSelectedGamePath();
                    Application.UseWaitCursor = false;
                    return;
                }
            }

            string configPath = "";
            if (launcherOpenedConfigFile != "")
            {
                configPath = Path.GetDirectoryName(launcherOpenedConfigFile);
            }

            // Yes I know this trim looks silly, but it's to prevent stuff like "C:\\\\directory\\pathtogame.exe"
            if (configPath == "")
            {
                configPath = Path.GetDirectoryName(Application.ExecutablePath);
            }
            configPath = configPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            pb1.Visible = true;
            pb1.Minimum = 0;
            pb1.Maximum = 2;
            pb1.Value = 0;
            pb1.Step = 1;

            // Let's put a big old try/catch around this to prevent any file system shananigans
            try
            {
                var fileNames = new string[] { archeAgeEXE, archeWorldEXE };
                exeFile = TryAutoFindInDir(configPath, fileNames, 0);
                if ((exeFile != "") && (File.Exists(exeFile)))
                {
                    Setting.PathToGame = exeFile;
                    Setting.AAPath = exeFile;
                    lGamePath.Text = GetSelectedGamePath();
                }
            }
            catch
            {
                // actually do nothing with this, user doesn't need to know
            }
            pb1.Visible = false;
            Application.UseWaitCursor = false;
        }

        private void UpdatePlayButton(serverCheck serverState, bool isMouseOver)
        {
            if (!IsGameInstalled())
            {
                btnPlay.Text = "Install";
                btnPlay.Cursor = Cursors.Hand;
                ApplyModernPlayButton(serverState, isMouseOver);
                return;
            }

            if (isMouseOver == true)
            {
                switch (serverState)
                {
                    case serverCheck.Offline: // offline
                        btnPlay.Text = L.Offline;
                        break;
                    case serverCheck.Online: // Play
                        btnPlay.Text = L.Play;
                        break;
                    case serverCheck.Update: // Update
                        btnPlay.Text = L.Update;
                        break;
                    case serverCheck.Updating: // Updating
                        btnPlay.Text = L.Updating;
                        break;
                    case serverCheck.Unknown: // Play
                    default:
                        btnPlay.Text = L.Play;
                        break;
                }
            }
            else
            {
                switch (serverState)
                {
                    case serverCheck.Offline: // offline
                        btnPlay.Text = L.Offline;
                        break;
                    case serverCheck.Online:
                        btnPlay.Text = L.Play;
                        break;
                    case serverCheck.Update: // Update
                        btnPlay.Text = L.Update;
                        break;
                    case serverCheck.Updating: // Updating
                        btnPlay.Text = L.Updating;
                        break;
                    case serverCheck.Unknown:
                    default:
                        btnPlay.Text = L.Play;
                        break;
                }

                if (serverCheckStatus == serverCheck.Updating)
                {
                    btnPlay.Cursor = Cursors.No;
                }
                else
                {
                    btnPlay.Cursor = Cursors.Hand;
                }

            }

            ApplyModernPlayButton(serverState, isMouseOver);
        }

        private async void CheckForLauncherUpdates()
        {
            checkedForLauncherUpdates = true;
            urlLauncherUpdateDownload = "";
            LauncherUpdateVersion = "";
            pendingLauncherUpdate = null;
            try
            {
                var updateInfo = await GitHubReleaseUpdater.CheckForUpdateAsync(launcherUpdateRepo, AppVersion);
                if (updateInfo == null)
                    return;

                pendingLauncherUpdate = updateInfo;
                LauncherUpdateVersion = updateInfo.Version;
                urlLauncherUpdateDownload = updateInfo.ReleaseUrl;

                lDownloadLauncherUpdate.Text = string.Format(L.DownloadLauncherUpdate, LauncherUpdateVersion);
                lDownloadLauncherUpdate.Visible = true;
            }
            catch
            {
                urlLauncherUpdateDownload = "";
                LauncherUpdateVersion = "";
                pendingLauncherUpdate = null;
            }
        }

        private void timerGeneral_Tick(object sender, EventArgs e)
        {
            if ((aaLauncher != null) && (aaLauncher.RunningProcess != null) && (checkGameIsRunning == true))
            {
                if (aaLauncher.RunningProcess.HasExited)
                {
                    checkGameIsRunning = false;
                    var eCode = aaLauncher.RunningProcess.ExitCode;
                    WindowState = FormWindowState.Normal;
                    if ((eCode != -1) && (debugModeToolStripMenuItem.Checked))
                        MessageBox.Show("Client Exit Code: " + eCode.ToString());
                }
            }
            else
            {
                checkGameIsRunning = false;
            }

            if (checkedForLauncherUpdates == false)
            {
                CheckForLauncherUpdates();
            }

            if (nextServerCheck > 0)
            {
                nextServerCheck -= timerGeneral.Interval;
                if (nextServerCheck <= 0)
                {
                    nextServerCheck += 1000 * 60 * 1; // 1 minute
                    bgwServerStatusCheck.RunWorkerAsync(); // former checkServerStatus();
                    UpdatePlayButton(serverCheckStatus, false);
                }
            }

            bool updateNews = false;
            if ((bigNewsTimer > 0) && (currentPanel == ShowPanelType.Login)) // Only count timer on the main "panel"
            {
                bigNewsTimer -= timerGeneral.Interval;
                if (bigNewsTimer <= 0)
                {
                    bigNewsTimer += 1000 * 60 * 1; // 1 minute
                    bigNewsIndex++;
                    if (bigNewsIndex >= newsFeed.Data.Count)
                    {
                        bigNewsIndex = 0;
                    }

                    updateNews = true;
                }
            }

            if ((checkNews == true) || (updateNews == true))
            {
                if (!bgwNewsFeed.IsBusy)
                {
                    bgwNewsFeed.RunWorkerAsync();
                }
            }

            if (AutoCloseTimer > 0)
            {
                AutoCloseTimer -= timerGeneral.Interval;
                if (AutoCloseTimer <= 0)
                {
                    AutoCloseTimer = 0;
                    Close();
                }
            }


        }

        private bool NeedToUpdateFrom(string remoteVersion)
        {
            string s = "";
            string localPatchVerFile = Path.GetDirectoryName(Path.GetDirectoryName(Setting.PathToGame)) + Path.DirectorySeparatorChar + localPatchFolderName + patchVersionFileName;
            try
            {
                if (File.Exists(localPatchVerFile))
                {
                    s = File.ReadAllText(localPatchVerFile);
                }
            }
            catch
            {
                s = "";
            }

            if ((s != "") && (aaPatcher.SetLocalVersionByString(s)))
            {
                if (aaPatcher.remoteVersion != aaPatcher.localVersion)
                {
                    // Different version, enable patching
                    return true;
                }
            }
            else if (s == "")
            {
                // No data, enable patching
                return true;
            }
            return false;
        }

        private void checkServerStatus()
        {
            if ((Setting.ServerIpAddress == null) || (Setting.ServerIpAddress == ""))
            {
                serverCheckStatus = serverCheck.Unknown;
                return;
            }

            TcpClient testCon = null;
            Application.UseWaitCursor = true;
            try
            {

                string serverIP = Setting.ServerIpAddress;
                ushort serverPort = defaultAuthPort;

                var splitPos = eServerIP.Text.IndexOf(":");
                if (splitPos >= 0)
                {
                    serverIP = eServerIP.Text.Substring(0, splitPos);
                    if (!ushort.TryParse(eServerIP.Text.Substring(splitPos + 1), out serverPort))
                    {
                        serverPort = defaultAuthPort;
                    }
                }

                testCon = new TcpClientWithTimeout(serverIP, serverPort, 5000).Connect();
                if (testCon.Connected)
                {
                    serverCheckStatus = serverCheck.Online;
                    nextServerCheck = (1000 * 60 * 2); // check every 2 minutes when connected
                    if (DoAutoLaunch)
                    {
                        DoAutoLaunch = false;
                        btnPlay_Click(null, null);
                        AutoCloseTimer = (1000 * 10); // close after 10 seconds
                    }

                }
            }
            catch
            {
                serverCheckStatus = 0;
                nextServerCheck = (1000 * 60 * 5); // check every 5 minutes when offline
            }
            finally
            {
                if (testCon != null)
                {
                    if (testCon.Connected == true)
                    {
                        testCon.Close();
                    }
                    testCon.Dispose();
                }
            }

            if (Setting.AllowGameUpdates)
            {
                if ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != "") && (aaPatcher.remoteVersion == ""))
                {
                    // Download patch version file only once until it's invalidated
                    aaPatcher.remoteVersionString = WebHelper.SimpleGetURIAsString(Setting.ServerGameUpdateURL + remotePatchFolderURI + patchVersionFileName);
                    aaPatcher.SetRemoteVersionByString(aaPatcher.remoteVersionString);
                }
                if ((aaPatcher.remoteVersion != "") && (NeedToUpdateFrom(aaPatcher.remoteVersion)))
                {
                    aaPatcher.Init(Setting.PathToGame); // reset patch info
                    serverCheckStatus = serverCheck.Update;
                    nextServerCheck = -1;
                }
            }
            Application.UseWaitCursor = false;
        }

        private void btnLauncherLangChange_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem mi in cmsLauncherLanguage.Items)
            {
                mi.Enabled = (mi.Tag.ToString() != Setting.LauncherLang);
            }
            cmsLauncherLanguage.Show(btnLauncherLangChange, new Point(0, btnLauncherLangChange.Height));
        }

        private void swapLanguageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Setting.LauncherLang = ((ToolStripMenuItem)sender).Tag.ToString();
            ApplyLanguageToLauncher();
            btnLauncherLangChange.Refresh();
        }

        private void btnSystem_DoubleClick(object sender, EventArgs e)
        {
            if (debugModeToolStripMenuItem.Visible == false)
            {
                debugModeToolStripMenuItem.Checked = true;
                debugModeToolStripMenuItem.Visible = true;
            }
            troubleshootGameToolStripMenuItem.Visible = true;
        }

        private void debugModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugModeToolStripMenuItem.Checked = !debugModeToolStripMenuItem.Checked;
        }

        private void btnLocaleLang_Click(object sender, EventArgs e)
        {
            foreach (ToolStripItem tsi in cmsLocaleLanguage.Items)
            {
                if (tsi is ToolStripMenuItem)
                {
                    ToolStripMenuItem mi = (ToolStripMenuItem)tsi;
                    {
                        mi.Enabled = (mi.Tag?.ToString() != Setting.Lang);
                    }
                }
            }
            cmsLocaleLanguage.Show(btnLocaleLang, new Point(0, btnLocaleLang.Height));
        }

        private void miLocaleLanguageChange_Click(object sender, EventArgs e)
        {
            Setting.Lang = ((ToolStripMenuItem)sender).Tag.ToString();
            UpdateLocaleLanguage();
            btnLocaleLang.Refresh();
        }

        private void CreateNewsFeedFromJSON(string JSONString)
        {

            try
            {
                newsFeed = JsonConvert.DeserializeObject<AAEmuNewsFeed>(JSONString);
            }
            catch
            {
                lNewsFeed.Text = L.ServerNewsFailed;
                wbNews.Hide();
                wbNews.Tag = "0";
                return;
            }
            finally
            {

            }
            if (newsFeed == null)
            {
                wbNews.Hide();
                wbNews.Tag = "0";
                return;
            }

            string wns = "";
            wns += "<html>";
            wns += "<style>";
            wns += "body { ";
            wns += "  background-attachment: fixed;";
            wns += "  background-repeat: no-repeat;";
            wns += "}";
            wns += "</style>";
            wns += "<body bgcolor=\"#000000\" text=\"#FFFFFF\"  link=\"#EEEEFF\"  vlink=\"#FFEEFF\" ";
            wns += "background =\"data:image/png;base64," + Properties.Resources.bgnews_b64 + "\">";

            foreach (AAEmuNewsFeedDataItem i in newsFeed.Data)
            {
                wns += "<p align=\"center\">";
                if (i.ItemAttributes.ItemIsNew == "1")
                {
                    wns += "*new* ";
                }
                wns += "<a href=\"" + i.ItemAttributes.ItemLinks.Self + "\" target=\"_new\">";
                wns += i.ItemAttributes.ItemTitle;
                wns += "</a><br>";
                wns += "<div align=\"left\"><font size=\"1\">";
                var bodyStr = i.ItemAttributes.ItemBody;
                if (bodyStr == null)
                    bodyStr = "";
                wns += bodyStr.Replace("\\r", "").Replace("\\n", "<br>");
                wns += "</font></div></p>";
            }
            // wns += "<p align=\"center\"><a href=\"testlink\" target=\"_new\">Test</a></p>";
            wns += "</body></html>";
            wbNews.DocumentText = wns;
            wbNews.Refresh();
            // File.WriteAllText("newscache.htm",wns);

            if ((newsFeed.Data != null) && (newsFeed.Data[0].ItemAttributes.ItemPicture != null) && (newsFeed.Data[0].ItemAttributes.ItemPicture != ""))
            {
                // LoadBigNews(newsFeed.data[0]);
                // Move this to always load at the end of our newsfeed backgroundworker thread
                bigNewsIndex = 0;
                bigNewsTimer = 1000 * 10 * 1; // 10 seconds
            }
        }

        private Image LoadImageForNews(AAEmuNewsFeedDataItem newsItem)
        {
            string cacheFolder = Application.LocalUserAppDataPath + Path.DirectorySeparatorChar + "data";
            Directory.CreateDirectory(cacheFolder);
            string cacheFileName = cacheFolder + Path.DirectorySeparatorChar + "img -" + Setting.ConfigName.Replace("@", "_").Replace("/", "_").Replace("\\", "_").Replace("|", "_") + "-" + newsItem.ItemID + ".bin";

            MemoryStream imageData;
            Image img = null;
            if (File.Exists(cacheFileName) == true)
            {
                FileStream fs = new FileStream(cacheFileName, FileMode.Open);
                imageData = new MemoryStream();
                fs.CopyTo(imageData);
                fs.Dispose();
                img = Image.FromStream(imageData);
            }
            else
            {
                try
                {
                    imageData = WebHelper.SimpleGetURIAsMemoryStream(newsItem.ItemAttributes.ItemPicture, out _);
                    FileStream fs = new FileStream(cacheFileName, FileMode.Create);
                    imageData.WriteTo(fs);
                    fs.Flush();
                    fs.Dispose();
                    img = Image.FromStream(imageData);
                }
                catch
                {
                    // Error loading " + newsItem.itemAttributes.itemPicture + " into " + cacheFileName
                }

            }
            return img;
        }

        private void LoadBigNews(AAEmuNewsFeedDataItem newsItem)
        {
            Application.UseWaitCursor = true;
            try
            {
                var img = LoadImageForNews(newsItem);
                if (img != null)
                {
                    imgBigNews.Image = img;
                    imgBigNews.SizeMode = PictureBoxSizeMode.Zoom;

                    lBigNewsImage.Text = newsItem.ItemAttributes.ItemTitle;
                    lBigNewsImage.Tag = newsItem.ItemAttributes.ItemLinks.Self;
                    lBigNewsImage.Visible = (currentPanel == ShowPanelType.Login);
                }
                else
                {
                    lBigNewsImage.Tag = "";
                    lBigNewsImage.Visible = false;
                    imgBigNews.Image = Properties.Resources.bignews_default;
                    imgBigNews.Visible = (currentPanel == ShowPanelType.Login);
                }
            }
            catch
            {
                lBigNewsImage.Tag = "";
                lBigNewsImage.Visible = false;
                imgBigNews.Image = Properties.Resources.bignews_default;
                imgBigNews.Visible = (currentPanel == ShowPanelType.Login);
            }
            Application.UseWaitCursor = false;
        }

        private void wbNews_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            wbNews.Visible = (currentPanel == ShowPanelType.Login);
            wbNews.Tag = "1"; // We use this it indicate if we need to show/hide the browser when swapping panels
        }

        private void lBigNewsImage_Click(object sender, EventArgs e)
        {
            if ((lBigNewsImage.Tag != null) && (lBigNewsImage.Tag.ToString() != ""))
            {
                Process.Start(lBigNewsImage.Tag.ToString());
            }
        }

        private void forcePatchDownloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aaPatcher.Init(Setting.PathToGame); // reset patch info
            try
            {
                File.Delete(aaPatcher.localPatchDirectory + patchVersionFileName);
            }
            catch { }
            // force the button to update
            serverCheckStatus = serverCheck.Update; // Patch
            nextServerCheck = -1;
            UpdatePlayButton(serverCheckStatus, false);
            ShowPanelControls(0); // Update Panel (same as login but replaced news)
        }

        private void UpdateProgressBarTotal(int pos, int maxPos)
        {
            float posPC = (float)pos / (float)(maxPos);
            int progressSize = (int)(posPC * (float)pgbBackTotal.Width);
            pgbFrontTotal.Location = pgbBackTotal.Location;
            pgbFrontTotal.Size = new Size(progressSize, pgbBackTotal.Height);
        }

        private void StartUpdate()
        {
            serverCheckStatus = serverCheck.Updating;
            ShowPanelControls(ShowPanelType.UpdatePatch); // Swap to download layout
            UpdatePlayButton(serverCheckStatus, false);
            bgwPatcher.RunWorkerAsync(); // start patch process
        }

        private void bgwNewsFeed_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(0);
            if (checkNews == true)
            {
                checkNews = false;
                worker.ReportProgress(1);
                System.Threading.Thread.Sleep(250);
                //lNewsFeed.Text = "Server News\n\n\n\nLoading ...";
                //wbNews.Visible = false;
                //wbNews.Tag = "0";
                try
                {
                    string newsString = WebHelper.SimpleGetURIAsString(Setting.ServerNewsFeedURL);
                    worker.ReportProgress(10);
                    CreateNewsFeedFromJSON(newsString);
                    worker.ReportProgress(50);
                }
                catch
                {
                    //lNewsFeed.Text = "Server News\n\n\n\nLoad Failed !";
                    worker.ReportProgress(99);
                }
            }

            if ((bigNewsIndex >= 0) && (bigNewsIndex < newsFeed.Data.Count))
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    LoadBigNews(newsFeed.Data[bigNewsIndex]);
                }));
            }
            worker.ReportProgress(100);
        }

        private void bgwNewsFeed_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Didn't feel like making a custom object, so just using the progress settings to change the label here
            switch (e.ProgressPercentage)
            {
                case 1:
                    lNewsFeed.Text = L.ServerNewsLoading;
                    wbNews.Visible = false;
                    wbNews.Tag = "0";
                    break;
                case 10:
                    lNewsFeed.Text = L.ServerNews;
                    break;
                case 99:
                    lNewsFeed.Text = L.ServerNewsFailed;
                    break;
            }
        }

        private void bgwNewsFeed_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //
        }

        private void bgwServerStatusCheck_DoWork(object sender, DoWorkEventArgs e)
        {
            checkServerStatus();
        }

        private void bgwServerStatusCheck_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdatePlayButton(serverCheckStatus, false);
        }

        private List<AAPakFileInfo> CreateXlFileListFromStream(Stream aStream)
        {
            List<AAPakFileInfo> res = new List<AAPakFileInfo>();

            aStream.Position = 0; // Rewind!
            List<string> rows = new List<string>();
            // Are you *sure* you want ASCII?
            using (var reader = new StreamReader(aStream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    AAPakFileInfo xlfi = new AAPakFileInfo();
                    string[] items = line.Split(';');
                    // Path
                    xlfi.Name = items[0];
                    // Size
                    long l = 0;
                    if (long.TryParse(items[1], out l))
                        xlfi.Size = l;
                    else
                        xlfi.Size = 0;
                    // If directory info

                    // Skip directories
                    if (xlfi.Size < 0)
                        continue;

                    if (items.Length > 2)
                    {
                        xlfi.Md5 = StringToByteArray(items[2]);
                    }
                    else
                    {
                        xlfi.Md5 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    }
                    if (items.Length > 4)
                    {
                        xlfi.CreateTime = AAPatchProgress.PatchDateTimeStrToFILETIME(items[3]);
                        xlfi.ModifyTime = AAPatchProgress.PatchDateTimeStrToFILETIME(items[4]);
                    }
                    else
                    {
                        xlfi.CreateTime = 0;
                        xlfi.ModifyTime = 0;
                    }
                    res.Add(xlfi);
                }
            }
            return res;
        }

        private AAPakFileInfo FindPatchFileInList(string filename, ref List<AAPakFileInfo> list, ref int startIndex)
        {
            filename = filename.ToLower();
            int i = startIndex;
            for (int c = 0; c < list.Count; i++, c++)
            {
                if ((i >= list.Count) || (i <= 0))
                {
                    i = 0;
                }
                AAPakFileInfo pfi = list[i];
                if (pfi.Name.ToLower() == filename)
                {
                    startIndex = i;
                    return pfi;
                }
            }
            return null;
        }

        private AAPakFileInfo FindPatchFileInSortedList(string filename, List<AAPakFileInfo> list)
        {
            filename = filename.ToLower();
            int searchMin = 0;
            int searchMax = list.Count - 1;
            int searchPos = 0;

            while (searchMin <= searchMax)
            {
                // Calculate range left
                // int searchSize = searchMax - searchMin;
                // Find the middle
                // searchPos = searchMin + (searchSize / 2);
                searchPos = ((searchMin + searchMax) / 2);

                var res = list[searchPos].Name.ToLower().CompareTo(filename);
                if (res < 0)
                {
                    // Looking for a smaller value
                    searchMin = searchPos + 1;
                }
                else
                if (res > 0)
                {
                    // Looking for a higher value
                    searchMax = searchPos - 1;
                }
                else
                {
                    return list[searchPos];
                }
            }

            return null;
        }

        private bool IsValidSQLiteFile(Stream aSteam)
        {
            bool res = false;
            if (aSteam.Length > 16)
            {
                // var requiredHeader = "SQLite format 3";
                byte[] buf = new byte[16];
                aSteam.Position = 0;
                aSteam.Read(buf, 0, 16);
                if ((buf[0] == 'S') && (buf[1] == 'Q') && (buf[2] == 'L') && (buf[3] == 'i') && (buf[4] == 't') && (buf[5] == 'e'))
                {
                    res = true;
                }
            }
            return res;
        }


        private string MakefileDownloadUrl(string filePath)
        {
            var dlFilePath = Setting.ServerGameUpdateURL;
            // Realistically we only really need to handle a few special escapes for file paths
            foreach (var c in filePath.ToCharArray())
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == '.') || (c == '-') || (c == '_'))
                {
                    dlFilePath += c;
                }
                /*
                else if (c == ' ')
                {
                    dlFilePath += '+';
                }
                */
                else if ((c == '/') || (c == '\\'))
                {
                    dlFilePath += '/';
                }
                else
                {
                    dlFilePath += "%" + Convert.ToByte(c).ToString("X2");
                }
            }
            // filePath = filePath.Replace(" ", "+").Replace("#", "%23");
            return dlFilePath;
        }
        
        private void bgwPatcher_DoWork(object sender, DoWorkEventArgs e)
        {
            //------
            // Init
            //------
            aaPatcher.Fase = PatchFase.Init;
            bgwPatcher.ReportProgress(0, aaPatcher);
            System.Threading.Thread.Sleep(150);
            try
            {
                _ = Directory.CreateDirectory(aaPatcher.localPatchDirectory);
            }
            catch
            {
                // Failed to create temp patch directory, quit trying to update
                aaPatcher.Fase = PatchFase.Error;
                aaPatcher.ErrorMsg = string.Format(L.FailedCreateDirectory, aaPatcher.localPatchDirectory);
                return;
            }
            System.Threading.Thread.Sleep(150);

            aaPatcher.Fase = PatchFase.DownloadVerFile;
            bgwPatcher.ReportProgress(0, aaPatcher);
            if (CancelPatching)
                return;

            // Download patch version file
            aaPatcher.remoteVersionString = WebHelper.SimpleGetURIAsString(Setting.ServerGameUpdateURL + remotePatchFolderURI + patchVersionFileName);
            if (!aaPatcher.SetRemoteVersionByString(aaPatcher.remoteVersionString))
            {
                aaPatcher.Fase = PatchFase.Error;
                aaPatcher.ErrorMsg = L.DownloadError + "\r\n" + Setting.ServerGameUpdateURL + remotePatchFolderURI + patchVersionFileName ;
                return;
            }

            if (aaPatcher.remotePatchSystemVersion != "aaemu.patch.1")
            {
                aaPatcher.Fase = PatchFase.Error;
                aaPatcher.ErrorMsg = string.Format(L.UnsupportedPatchType, aaPatcher.remotePatchSystemVersion);
                return;
            }

            aaPatcher.Fase = PatchFase.CompareVersion;
            System.Threading.Thread.Sleep(250);
            if (CancelPatching)
                return;

            //----------------------------------
            // Read Local version file (if any)
            //----------------------------------
            try
            {
                if (File.Exists(aaPatcher.localPatchDirectory + patchVersionFileName))
                {
                    aaPatcher.localVersion = File.ReadAllText(aaPatcher.localPatchDirectory + patchVersionFileName);
                }
                else
                {
                    aaPatcher.localVersion = L.ErrorReadingLocalVersionInformation;
                }
            }
            catch
            {
                aaPatcher.localVersion = L.ErrorReadingLocalVersionInformation;
            }

            System.Threading.Thread.Sleep(250);
            if (CancelPatching)
                return;

            if (aaPatcher.localVersion == aaPatcher.remoteVersionString)
            {
                // Nothing to update, skip out
                aaPatcher.Fase = PatchFase.Done;
                aaPatcher.DoneMsg = L.NothingToUpdate;
                return;
            }

            aaPatcher.Fase = PatchFase.CheckLocalFiles;
            bgwPatcher.ReportProgress(1, aaPatcher);
            System.Threading.Thread.Sleep(250);
            if (CancelPatching)
                return;

            dlPakFileList.Clear();

            //--------------------------------------
            // Create/Load Local Hash from game_pak
            //--------------------------------------
            if (File.Exists(aaPatcher.localGame_Pak))
            {
                aaPatcher.Fase = PatchFase.CheckLocalFiles;
                bgwPatcher.ReportProgress(2, aaPatcher);

                if (pak != null && pak.IsOpen)
                    pak.ClosePak();

                //pak = new AAPak(aaPatcher.localGame_Pak, false, false);
                pak = new AAPak("");
                TryLoadCustomKey(pak, aaPatcher.localGame_Pak);
                pak.OpenPak(aaPatcher.localGame_Pak, false);
                if (!pak.IsOpen)
                {
                    // Failed to open pak
                    aaPatcher.Fase = PatchFase.Error;
                    aaPatcher.ErrorMsg = string.Format(L.FailedToOpen, aaPatcher.localGame_Pak);
                    return;
                }

                // Calculate local file size total
                // aaPatcher.FileDownloadSizeTotal = new System.IO.FileInfo(aaPatcher.localGame_Pak).Length;
                aaPatcher.FileDownloadSizeTotal = 0;
                var totalFilesCount = 0;
                foreach (var pfi in pak.Files)
                {
                    aaPatcher.FileDownloadSizeTotal += pfi.Size;
                    totalFilesCount++;
                }

                var filesCount = 0;
                foreach (var pfi in pak.Files)
                {
                    //if (BitConverter.ToString(pfi.md5).Replace("-", "") == AAPakFileHeader.nullHashString)
                    
                    if (AAPakFileHeader.NullHash.SequenceEqual(pfi.Md5))
                    {
                        aaPatcher.Fase = PatchFase.ReHashLocalFiles;
                        pak.UpdateMd5(pfi);
                        if (CancelPatching)
                            return;
                    }
                    filesCount++;
                    if ((filesCount % 50) == 0)
                    {
                        bgwPatcher.ReportProgress((filesCount * 100 / totalFilesCount), aaPatcher);
                    }
                }
                if (CancelPatching)
                    return;

                if (aaPatcher.Fase == PatchFase.ReHashLocalFiles)
                    pak.SaveHeader();
                System.Threading.Thread.Sleep(250);
            }
            else
            {
                // Failed to open pak
                aaPatcher.Fase = PatchFase.Error;
                aaPatcher.ErrorMsg = string.Format(L.FileNotFound, aaPatcher.localGame_Pak);
                return;
            }

            //------------------------
            // Download PatchFileInfo
            //------------------------
            aaPatcher.Fase = PatchFase.DownloadPatchFilesInfo;
            bgwPatcher.ReportProgress(2, aaPatcher);

            var ms = WebHelper.SimpleGetURIAsMemoryStream(Setting.ServerGameUpdateURL + remotePatchFolderURI + patchListFileName, out _);

            // check if downloaded patch files list has the correct hash
            var remotePatchFilesHash = WebHelper.GetMD5FromStream(ms);
            if (remotePatchFilesHash != aaPatcher.remotePatchFileHash)
            {
                aaPatcher.Fase = PatchFase.Error;
                aaPatcher.ErrorMsg = string.Format(L.PatchHashMismatch, remotePatchFilesHash, aaPatcher.remotePatchFileHash);
                return;
            }

            // Generate a files list in our pak files' format from the downloaded CSV
            var remotePakFileList = CreateXlFileListFromStream(ms);

            System.Threading.Thread.Sleep(250);
            if (CancelPatching)
                return;

            //--------------------------------------------------------------------------------------
            // Compare local game_pak with downloaded information to check what needs to be updated
            //--------------------------------------------------------------------------------------
            aaPatcher.Fase = PatchFase.CalculateDownloads;
            bgwPatcher.ReportProgress(0, aaPatcher);

            // First sort both to speed things up
            remotePakFileList.Sort();
            pak.Files.Sort();

            // Create lower-case copy of the files list for easy compare
            var lowerCasePakFileNames = new Dictionary<string, AAPakFileInfo>();
            foreach (var aaPakFileInfo in pak.Files)
            {
                lowerCasePakFileNames.Add(aaPakFileInfo.Name.ToLower(), aaPakFileInfo);
            }

            long totSize = 0;
            for (var i = 0; i < remotePakFileList.Count; i++)
            {
                var remoteFileInfo = remotePakFileList[i];

                // Don't download empty files or entries marked as directories (-1)
                if (remoteFileInfo.Size <= 0) continue;

                // var l = FindPatchFileInSortedList(r.Name, pak.Files);
                if (!lowerCasePakFileNames.TryGetValue(remoteFileInfo.Name.ToLower(), out var l))
                {
                    // We don't have a local copy of this file
                    // Add it to the list
                    dlPakFileList.Add((remoteFileInfo, "Missing"));
                    totSize += remoteFileInfo.Size;
                }
                else if (l.Size != remoteFileInfo.Size)
                {
                    // Local file-size or Hash is different from remote
                    // Re-download it
                    dlPakFileList.Add((remoteFileInfo, "Size"));
                    totSize += remoteFileInfo.Size;
                }
                else if (l.Md5.SequenceEqual(remoteFileInfo.Md5) == false)
                {
                    // Local file-size or Hash is different from remote
                    // Re-download it
                    dlPakFileList.Add((remoteFileInfo, "MD5"));
                    totSize += remoteFileInfo.Size;
                }

                if ((i % 100) == 0)
                {
                    var p = i * 100 / remotePakFileList.Count;
                    bgwPatcher.ReportProgress(p, aaPatcher);
                    // System.Threading.Thread.Sleep(1);
                }
            }
            aaPatcher.FileDownloadSizeTotal = totSize;

            if ((aaPatcher.FileDownloadSizeTotal <= 0) || (dlPakFileList.Count <= 0))
            {
                aaPatcher.Fase = PatchFase.Done;
                aaPatcher.DoneMsg = L.NoFilesToUpdate;
                return;
            }
            if (CancelPatching)
                return;

            //-------------------------------------------------
            // Initialize download.patch file (patch pak file)
            //-------------------------------------------------
            if (PatchDownloadPak == null)
                PatchDownloadPak = new AAPak("");

            if (!File.Exists(aaPatcher.localPatchDirectory + localPatchPakFileName))
            {
                try
                {
                    if (!PatchDownloadPak.NewPak(aaPatcher.localPatchDirectory + localPatchPakFileName))
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.ErrorCreatingPatchCache, aaPatcher.localPatchDirectory + localPatchPakFileName);
                        return;
                    }
                    PatchDownloadPak.SaveHeader();
                    PatchDownloadPak.ClosePak();
                }
                catch (Exception ex)
                {
                    aaPatcher.Fase = PatchFase.Error;
                    aaPatcher.ErrorMsg = string.Format(L.ErrorCreatingPatchCache, ex.Message);
                    return;
                }
            }

            if (CancelPatching)
                return;

            PatchDownloadPak.OpenPak(aaPatcher.localPatchDirectory + localPatchPakFileName, false);
            if (!PatchDownloadPak.IsOpen)
            {
                // TODO: Add better support in case of fails
                MessageBox.Show(L.FailedToOpenPatchCache, @"CACHE ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                try
                {
                    PatchDownloadPak.ClosePak();
                    PatchDownloadPak.NewPak(aaPatcher.localPatchDirectory + localPatchPakFileName);
                }
                catch
                {
                    PatchDownloadPak = null;
                }

                if ((PatchDownloadPak == null) || (!PatchDownloadPak.IsOpen))
                {
                    aaPatcher.Fase = PatchFase.Error;
                    aaPatcher.ErrorMsg = L.FatalErrorFailedToOpenFileForWrite;
                    return;
                }
            }

            // Generate a list of files to download
            // Skip any file that is already in our download.patch
            var sl = new List<string>();

            totSize = 0; // calculate this again on the final list
            for (var i = dlPakFileList.Count - 1; i >= 0; i--)
            {
                if (PatchDownloadPak.FileExists(dlPakFileList[i].Item1.Name))
                {
                    dlPakFileList.Remove(dlPakFileList[i]);
                }
                else
                {
                    totSize += dlPakFileList[i].Item1.Size;
                    sl.Add(dlPakFileList[i].Item2 + " (" + (dlPakFileList[i].Item1.Size / 1024) + "KB) => " + dlPakFileList[i].Item1.Name);
                }
            }
            aaPatcher.FileDownloadSizeTotal = totSize;

            // debug file to check what we'll download
            File.WriteAllLines(aaPatcher.localPatchDirectory + "download.txt", sl);
            if (CancelPatching)
                return;

            // MessageBox.Show("Need to download " + dlPakFileList.Count.ToString() + " files, "+ (aaPatcher.FileDownloadSizeTotal / 1024 / 1024).ToString() + " MB total");

            System.Threading.Thread.Sleep(500);

            //-------------------
            // Do download stuff
            //-------------------
            aaPatcher.Fase = PatchFase.DownloadFiles;
            bgwPatcher.ReportProgress(0, aaPatcher);

            aaPatcher.FileDownloadSizeDownloaded = 0;
            for (var i = dlPakFileList.Count - 1; i >= 0; i--)
            {
                var pfi = dlPakFileList[i].Item1;
                var fileDlUrl = MakefileDownloadUrl(pfi.Name);

                try
                {
                    var fileDlStream = WebHelper.SimpleGetURIAsMemoryStream(fileDlUrl, out var returnException);

                    if (returnException != null)
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.DownloadFileError, fileDlUrl + "\n\r" + returnException.Message);
                        fileDlStream.Dispose();
                        return;
                    }

                    if (fileDlStream.Length != pfi.Size)
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.DownloadSizeMismatch, fileDlUrl, pfi.Size.ToString(), fileDlStream.Length.ToString());
                        fileDlStream.Dispose();
                        return;
                    }

                    fileDlStream.Position = 0;
                    var fileHash = WebHelper.GetMD5FromStream(fileDlStream);
                    var expectHash = BitConverter.ToString(pfi.Md5).Replace("-", "").ToLower();
                    if (fileHash != expectHash)
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.DownloadHashMismatch, fileDlUrl, expectHash, fileHash);
                        fileDlStream.Dispose();
                        return;
                    }

                    fileDlStream.Position = 0;
                    var addRes = PatchDownloadPak.AddFileFromStream(pfi.Name, fileDlStream, DateTime.FromFileTime(pfi.CreateTime), DateTime.FromFileTime(pfi.ModifyTime), false, out var addPfi);
                    if (!addRes)
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.FailedToSaveCache, pfi.Name);
                        fileDlStream.Dispose();
                        return;
                    }
                    fileDlStream.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Error downloading {fileDlUrl} with error {ex.Message}");
                    aaPatcher.Fase = PatchFase.Error;
                    aaPatcher.ErrorMsg = string.Format(L.DownloadFileError, fileDlUrl + "\n\r" + ex.Message);
                    return;
                }

                aaPatcher.FileDownloadSizeDownloaded += pfi.Size;

                var dlProgress = (aaPatcher.FileDownloadSizeDownloaded * 100) / aaPatcher.FileDownloadSizeTotal;
                bgwPatcher.ReportProgress((int)dlProgress, aaPatcher);

                PatchDownloadPak.SaveHeader();
                if (CancelPatching)
                    return;
            }

            //------------------------------------
            // Check if we need to extract the DB
            //------------------------------------
            var exportDbAsWell = false;
            const string dbNameInPak = "game/db/compact.sqlite3";
            if (PatchDownloadPak.FileExists(dbNameInPak))
            {
                Stream testStream = PatchDownloadPak.ExportFileAsStream(dbNameInPak);
                if (IsValidSQLiteFile(testStream))
                    exportDbAsWell = true;
                testStream.Dispose();
            }
            const string bin32Dir = "bin32/";

            //----------------------------------------------------------------------------------
            // Recalculate the total size to apply (including data to copy out of the game_pak)
            //----------------------------------------------------------------------------------
            aaPatcher.FileDownloadSizeTotal = 0;
            foreach (var pfi in PatchDownloadPak.Files)
            {
                aaPatcher.FileDownloadSizeTotal += pfi.Size;
                // Count files inside bin32 twice
                if ((pfi.Name.Length > bin32Dir.Length) && (pfi.Name.Substring(0, bin32Dir.Length) == bin32Dir))
                {
                    aaPatcher.FileDownloadSizeTotal += pfi.Size;
                }
                // count compact.sqlite3 twice if it's not encrypted
                if ((pfi.Name == dbNameInPak) && (exportDbAsWell))
                {
                    aaPatcher.FileDownloadSizeTotal += pfi.Size;
                }
            }


            aaPatcher.FileDownloadSizeDownloaded = 0; // using downloaded size as progress bar
            aaPatcher.Fase = PatchFase.AddFiles;
            //---------------------------------------------
            // Apply downloaded files, export where needed
            //---------------------------------------------
            foreach (var pfi in PatchDownloadPak.Files)
            {
                if (CancelPatching)
                    return;

                var exportStream = PatchDownloadPak.ExportFileAsStream(pfi);
                exportStream.Position = 0;

                var addRes = pak.AddFileFromStream(pfi.Name, exportStream, DateTime.FromFileTime(pfi.CreateTime), DateTime.FromFileTime(pfi.ModifyTime), false, out var resPfi);
                if (!addRes)
                {
                    aaPatcher.Fase = PatchFase.Error;
                    aaPatcher.ErrorMsg = string.Format(L.ErrorPatchApplyFile, pfi.Name);
                    exportStream.Dispose();
                    return;
                }
                aaPatcher.FileDownloadSizeDownloaded += pfi.Size;

                // always export files inside bin32
                var exportErrorCount = 0;
                if ((pfi.Name.Length > bin32Dir.Length) && (pfi.Name.Substring(0, bin32Dir.Length) == bin32Dir))
                {
                    var fileOk = false;
                    while (!fileOk)
                    {
                        try
                        {
                            var destName = aaPatcher.localGameFolder + pfi.Name.Replace('/', Path.DirectorySeparatorChar);
                            _ = Directory.CreateDirectory(Path.GetDirectoryName(destName));
                            var fs = new FileStream(destName, FileMode.Create);
                            exportStream.Position = 0;

                            exportStream.CopyTo(fs);

                            fs.Dispose();

                            // Update file details
                            File.SetCreationTime(destName, DateTime.FromFileTime(pfi.CreateTime));
                            File.SetLastWriteTime(destName, DateTime.FromFileTime(pfi.ModifyTime));
                            aaPatcher.FileDownloadSizeDownloaded += pfi.Size;
                            fileOk = true;
                        }
                        catch
                        {
                            exportStream.Dispose();
                        }

                        if (!fileOk)
                        {
                            // Something went wrong while exporting
                            var retryRes = MessageBox.Show(string.Format(L.ErrorPatchExportFile, pfi.Name), L.AddFiles, MessageBoxButtons.AbortRetryIgnore);
                            switch(retryRes)
                            {
                                case DialogResult.Retry:
                                    fileOk = false;
                                    break;
                                case DialogResult.Abort:
                                    exportErrorCount++;
                                    aaPatcher.Fase = PatchFase.Error;
                                    aaPatcher.ErrorMsg = string.Format(L.ErrorPatchExportFile, pfi.Name);
                                    break;
                                case DialogResult.Ignore:
                                    fileOk = true;
                                    break;
                                case DialogResult.None:
                                case DialogResult.OK:
                                case DialogResult.Cancel:
                                case DialogResult.Yes:
                                case DialogResult.No:
                                default:
                                    break;
                            }
                        }
                    }

                    if (exportErrorCount > 0)
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.ErrorPatchExportFile, pfi.Name);
                    }
                }

                // export compact.sqlite3 if it's not encrypted
                if ((pfi.Name == dbNameInPak) && (exportDbAsWell))
                {
                    var fileOk = false;
                    while (!fileOk)
                    {
                        try
                        {
                            var destName = aaPatcher.localGameFolder + pfi.Name.Replace('/', Path.DirectorySeparatorChar);
                            Directory.CreateDirectory(Path.GetDirectoryName(destName));
                            var fs = new FileStream(destName, FileMode.Create);
                            exportStream.Position = 0;

                            exportStream.CopyTo(fs);

                            fs.Dispose();

                            // Update file details
                            File.SetCreationTime(destName, DateTime.FromFileTime(pfi.CreateTime));
                            File.SetLastWriteTime(destName, DateTime.FromFileTime(pfi.ModifyTime));
                            aaPatcher.FileDownloadSizeDownloaded += pfi.Size;
                            fileOk = true;
                        }
                        catch
                        {
                            //aaPatcher.Fase = PatchFase.Error;
                            //aaPatcher.ErrorMsg = string.Format(L.ErrorPatchExportDB, pfi.name);
                            exportStream.Dispose();
                            // return;
                        }

                        if (!fileOk)
                        {
                            // Something went wrong while exporting
                            var retryRes = MessageBox.Show(string.Format(L.ErrorPatchExportFile, pfi.Name), L.AddFiles, MessageBoxButtons.AbortRetryIgnore);
                            switch (retryRes)
                            {
                                case DialogResult.Retry:
                                    fileOk = false;
                                    break;
                                case DialogResult.Abort:
                                    aaPatcher.Fase = PatchFase.Error;
                                    aaPatcher.ErrorMsg = string.Format(L.ErrorPatchExportDB, pfi.Name);
                                    break;
                                case DialogResult.Ignore:
                                    fileOk = true;
                                    break;
                                case DialogResult.None:
                                case DialogResult.OK:
                                case DialogResult.Cancel:
                                case DialogResult.Yes:
                                case DialogResult.No:
                                default:
                                    break;
                            }
                        }

                    }
                }

                exportStream?.Dispose();

                var patchProgress = (aaPatcher.FileDownloadSizeDownloaded * 100) / aaPatcher.FileDownloadSizeTotal;
                bgwPatcher.ReportProgress((int)patchProgress, aaPatcher);

            }
            if (CancelPatching)
                return;

            //-------------------------------------------
            // Delete all files mentioned in deleted.txt
            //-------------------------------------------
            try
            {
                var delFile = "deleted.txt";
                PatchDownloadPak.FileExists(delFile);
                Stream exportStream = PatchDownloadPak.ExportFileAsStream(delFile);
                exportStream.Position = 0;
                List<string> slDelFiles = new List<string>();
                using (StreamReader reader = new StreamReader(exportStream))
                {
                    while (!reader.EndOfStream)
                    {
                        var s = reader.ReadLine();
                        slDelFiles.Add(s);
                    }
                }

                exportStream.Dispose();

                foreach (string s in slDelFiles)
                {
                    var delName = s.Replace('/', Path.DirectorySeparatorChar);
                    if (File.Exists(aaPatcher.localGameFolder + delName))
                        File.Delete(aaPatcher.localGameFolder + delName);
                }
            }
            catch
            {
                //
            }

            System.Threading.Thread.Sleep(250);

            //----------------------------------
            // All done, back to the login page
            //----------------------------------
            aaPatcher.localVersion = aaPatcher.remoteVersion;
            aaPatcher.Fase = PatchFase.Done;
            aaPatcher.DoneMsg = L.PatchComplete;
            bgwPatcher.ReportProgress(100, aaPatcher);
            System.Threading.Thread.Sleep(1500);
        }

        private void bgwPatcher_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Patch progress
            AAPatchProgress p = e.UserState as AAPatchProgress;
            switch (p.Fase)
            {
                case PatchFase.Init:
                    lPatchProgressBarText.Text = L.CheckVersion;
                    rbInit.Checked = true;
                    break;
                case PatchFase.DownloadVerFile:
                    rbDownloadVerFile.Checked = true;
                    break;
                case PatchFase.CompareVersion:
                    rbComparingVersion.Checked = true;
                    break;
                case PatchFase.CheckLocalFiles:
                    lPatchProgressBarText.Text = L.CheckLocalFiles;
                    rbCheckLocalFiles.Checked = true;
                    break;
                case PatchFase.ReHashLocalFiles:
                    lPatchProgressBarText.Text = L.Game_PakNeedsUpdate;
                    rbReHashLocalFiles.Checked = true;
                    break;
                case PatchFase.DownloadPatchFilesInfo:
                    rbDownloadPatchFilesInfo.Checked = true;
                    break;
                case PatchFase.CalculateDownloads:
                    lPatchProgressBarText.Text = L.CheckVersion + " -> " + aaPatcher.remoteVersion;
                    rbCalculateDownloads.Checked = true;
                    break;
                case PatchFase.DownloadFiles:
                    lPatchProgressBarText.Text = L.DownloadPatch;
                    rbDownloadFiles.Text = string.Format(L.DownloadSizeAndFiles, (aaPatcher.FileDownloadSizeTotal / 1024 / 1024).ToString(), dlPakFileList.Count.ToString());
                    // rbDownloadFiles.Text = "Download " + dlPakFileList.Count.ToString() + " patch files - " + (aaPatcher.FileDownloadSizeTotal / 1024 / 1024).ToString() + " MB";
                    rbDownloadFiles.Checked = true;
                    break;
                case PatchFase.AddFiles:
                    lPatchProgressBarText.Text = L.ApplyPatch;
                    rbAddFiles.Checked = true;
                    break;
                case PatchFase.Done:
                    lPatchProgressBarText.Text = L.PatchComplete;
                    rbDone.Checked = true;
                    break;
            }
            UpdateProgressBarTotal(e.ProgressPercentage, 100);
        }

        private void bgwPatcher_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Patch Finished
            // Revert server status to unknown, and do some cleaning

            // Close the patch cache file (if open)
            try
            {
                // Try saving out patch data if possible
                if (PatchDownloadPak != null)
                {
                    PatchDownloadPak.ClosePak();
                    PatchDownloadPak = null;
                }
            }
            catch { }

            // Close the client game_pak file (if open)
            try
            {
                // Try saving out patch data if possible
                if (pak != null)
                {
                    pak.ClosePak();
                    pak = null;
                }
            }
            catch { }

            if (aaPatcher.Fase == PatchFase.Error)
            {
                MessageBox.Show(aaPatcher.ErrorMsg, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (aaPatcher.Fase == PatchFase.Done)
            {
                try
                {
                    // Save our .ver file again
                    File.WriteAllText(aaPatcher.localPatchDirectory + patchVersionFileName, aaPatcher.remoteVersionString);
                    // Delete Patch Pak if completed successfully
                    // Keep patch cache file if debugging
                    if (debugModeToolStripMenuItem.Checked == false)
                        File.Delete(aaPatcher.localPatchDirectory + localPatchPakFileName);
                }
                catch (Exception x)
                {
                    MessageBox.Show(string.Format(L.ErrorSavingVersionInfo, x.Message));
                }
                if (aaPatcher.DoneMsg != "")
                {
                    MessageBox.Show(aaPatcher.DoneMsg, L.PatchComplete, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }

            serverCheckStatus = serverCheck.Unknown;
            nextServerCheck = 1000;
            UpdatePlayButton(serverCheckStatus, false);
            ShowPanelControls(0);
        }

        public byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private void skipPatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serverCheckStatus = serverCheck.Unknown; // Force to unknown mode
            nextServerCheck = -1;
            UpdatePlayButton(serverCheckStatus, false);
            ShowPanelControls(0); // Update Panel

        }

        private void deleteShaderCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearArcheAgeCache(true);
        }

        private void EmptyFolder(DirectoryInfo directoryInfo, bool onlyFiles)
        {
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo subfolder in directoryInfo.GetDirectories())
            {
                EmptyFolder(subfolder, onlyFiles);
                if (!onlyFiles)
                    subfolder.Delete();
            }
        }

        private void deleteGameConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(L.DeleteGameSettings, "Delete system.cfg", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // C:\ArcheAge\Documents => UserHomeFolder\ArcheAge
            var folder = File.Exists(Setting.PathToGame) ? GuessDocumentsFolder(Setting.PathToGame) : "ArcheAge";

            string systemConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folder, archeAgeSystemConfigFileName);
            File.Delete(systemConfigFile);
        }

        private void fixBin32StripMenuItem_Click(object sender, EventArgs e)
        {
            var bin32Dir = "bin32/";
            var dbNameInPak = "game/db/compact.sqlite3";

            Application.UseWaitCursor = true;
            Cursor.Current = Cursors.WaitCursor;


            if (aaPatcher == null)
            {
                aaPatcher = new AAPatchProgress();
            }
            aaPatcher.Init(Setting.PathToGame);
            aaPatcher.Fase = PatchFase.AddFiles;

            if (MessageBox.Show(string.Format(L.FixBin32Files, aaPatcher.localGameFolder + bin32Dir, aaPatcher.localGame_Pak), L.TSFixBin32, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                Cursor.Current = Cursors.Default;
                Application.UseWaitCursor = false;
                aaPatcher = null;
                return;
            }

            if (pak == null)
            {
                //pak = new AAPak(aaPatcher.localGame_Pak, false, false);
                pak = new AAPak("");
                TryLoadCustomKey(pak, aaPatcher.localGame_Pak);
                pak.OpenPak(aaPatcher.localGame_Pak, false);
            }

            if (!pak.IsOpen)
            {
                Cursor.Current = Cursors.Default;
                Application.UseWaitCursor = false;
                MessageBox.Show(string.Format(L.FailedToOpen, aaPatcher.localGame_Pak), L.ApplyPatchError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                aaPatcher = null;
                pak.ClosePak();
                pak = null;
                return;
            }

            //---------------------------------------------
            // Apply downloaded files, export where needed
            //---------------------------------------------
            foreach (AAPakFileInfo pfi in pak.Files)
            {

                // always export files inside bin32
                if ((pfi.Name.Length > bin32Dir.Length) && (pfi.Name.Substring(0, bin32Dir.Length) == bin32Dir))
                {
                    try
                    {
                        Stream exportStream = pak.ExportFileAsStream(pfi);
                        var destName = aaPatcher.localGameFolder + pfi.Name.Replace('/', Path.DirectorySeparatorChar);
                        Directory.CreateDirectory(Path.GetDirectoryName(destName));
                        FileStream fs = new FileStream(destName, FileMode.Create);
                        exportStream.Position = 0;

                        exportStream.CopyTo(fs);

                        fs.Dispose();

                        // Update file details
                        File.SetCreationTime(destName, DateTime.FromFileTime(pfi.CreateTime));
                        File.SetLastWriteTime(destName, DateTime.FromFileTime(pfi.ModifyTime));
                        aaPatcher.FileDownloadSizeDownloaded += pfi.Size;
                    }
                    catch
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.ErrorPatchExportFile, pfi.Name);
                        return;
                    }

                }

                // export compact.sqlite3 if it's not encrypted
                if (pfi.Name == dbNameInPak)
                {
                    try
                    {
                        Stream exportStream = pak.ExportFileAsStream(pfi);
                        if (IsValidSQLiteFile(exportStream))
                        {
                            var destName = aaPatcher.localGameFolder + pfi.Name.Replace('/', Path.DirectorySeparatorChar);
                            Directory.CreateDirectory(Path.GetDirectoryName(destName));
                            FileStream fs = new FileStream(destName, FileMode.Create);
                            exportStream.Position = 0;

                            exportStream.CopyTo(fs);

                            fs.Dispose();

                            // Update file details
                            File.SetCreationTime(destName, DateTime.FromFileTime(pfi.CreateTime));
                            File.SetLastWriteTime(destName, DateTime.FromFileTime(pfi.ModifyTime));
                            aaPatcher.FileDownloadSizeDownloaded += pfi.Size;
                        }
                    }
                    catch
                    {
                        aaPatcher.Fase = PatchFase.Error;
                        aaPatcher.ErrorMsg = string.Format(L.ErrorPatchExportDB, pfi.Name);
                        return;
                    }
                }

            } // end foreach pak.files

            Cursor.Current = Cursors.Default;
            Application.UseWaitCursor = false ;

            if (aaPatcher.Fase == PatchFase.Error)
            {
                MessageBox.Show(aaPatcher.ErrorMsg, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(L.PatchComplete, L.PatchDone, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private async void LDownloadLauncherUpdate_Click(object sender, EventArgs e)
        {
            if (pendingLauncherUpdate?.Manifest == null)
            {
                // No delta manifest available for this release, fall back to opening the release page
                Process.Start(urlLauncherUpdateDownload);
                return;
            }

            var confirm = MessageBox.Show(this,
                $"Launcher update {pendingLauncherUpdate.Version} is available. Download and install it now? The launcher will restart.",
                "Launcher Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            var appDirectory = Application.StartupPath;
            var stagingDirectory = Path.Combine(Path.GetTempPath(), "AAEmuLauncherUpdateStaging_" + Guid.NewGuid().ToString("N"));

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var changedFiles = GitHubReleaseUpdater.GetChangedFiles(appDirectory, pendingLauncherUpdate.Manifest);
                if (changedFiles.Count == 0)
                {
                    MessageBox.Show(this, "Nothing to update, files already match the latest release.", "Launcher Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                await GitHubReleaseUpdater.DownloadChangedFilesAsync(pendingLauncherUpdate, changedFiles, stagingDirectory, null);

                var removedFiles = GitHubReleaseUpdater.GetRemovedFiles(pendingLauncherUpdate.PreviousManifest, pendingLauncherUpdate.Manifest);
                GitHubReleaseUpdater.ApplyUpdateAndRestart(appDirectory, stagingDirectory, Path.GetFileName(Application.ExecutablePath), removedFiles);
                SaveSettings();
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to download update:\n{ex.Message}", "Launcher Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void AaEmuDiscordMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(urlAAEmuDiscordInvite);
        }

        private void LauncherDiscordMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(urlLauncherDiscordInvite);
        }

        private void CustomServerDiscordMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Setting.ServerDiscordURL);
        }

        private void DirectXtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(dx9downloadURL);
        }

        private void TryLoadCustomKey(AAPak aPak, string pakFileName)
        {
            byte[] customKey = new byte[16];
            string fn;

            // PAK-Header Key
            fn = Path.ChangeExtension(pakFileName, ".key");
            if (File.Exists(fn))
            {
                FileStream fs = new FileStream(fn, FileMode.Open, FileAccess.Read);
                if (fs.Length != 16)
                {
                    fs.Dispose();
                    return;
                }
                fs.Read(customKey, 0, 16);
                fs.Dispose();
                aPak.SetCustomKey(customKey);
            }
        }

        private void stAuthAuto_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripItem tsi)
            {
                if (tsi.Text.ToLower() == "auto")
                {
                    Setting.ClientLoginType = "auto";
                    GuessAndUpdateClientType();
                }
                else
                {
                    foreach (var l in AAEmuLauncherBase.AllLaunchers)
                    {
                        if (l.DisplayName == tsi.Text)
                        {
                            Setting.ClientLoginType = l.ConfigName;
                        }
                    }
                    UpdateGameClientTypeLabel();
                }
            }
        }

        private void generateServerURILinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(var genform = new URIGenForm())
            {
                genform.tServerIP.Text = Setting.ServerIpAddress;
                genform.tConfigName.Text = Setting.ConfigName;
                genform.tUserName.Text = Setting.LastLoginUser;
                genform.tPassword.Text = "";
                genform.tAuthDriver.Text = Setting.ClientLoginType;
                genform.tWebsite.Text = Setting.ServerWebSiteURL;
                genform.tNews.Text = Setting.ServerNewsFeedURL;
                genform.tPatchLocation.Text = Setting.ServerGameUpdateURL;
                genform.ShowDialog();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            isCloseButtonHot = true;
            btnClose.Invalidate();
        }

        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            isCloseButtonHot = false;
            btnClose.Invalidate();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void btnMinimize_MouseEnter(object sender, EventArgs e)
        {
            isMinimizeButtonHot = true;
            btnMinimize.Invalidate();
        }

        private void btnMinimize_MouseLeave(object sender, EventArgs e)
        {
            isMinimizeButtonHot = false;
            btnMinimize.Invalidate();
        }

        private void btnSystem_MouseEnter(object sender, EventArgs e)
        {
            btnSystem.Image = Properties.Resources.btn_aaemu_active;
        }

        private void btnSystem_MouseLeave(object sender, EventArgs e)
        {
            btnSystem.Image = Properties.Resources.btn_aaemu;
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void unknownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string v = Setting.Lang;
            if (InputBox(L.UpdateLocale,"Custom Locale",ref v) == DialogResult.OK)
            {
                Setting.Lang = v;
                UpdateLocaleLanguage();
            }
        }

        private void clearPatchCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
