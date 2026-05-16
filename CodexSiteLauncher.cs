using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CodexSiteLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                ShowFatalError(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ShowFatalError(e.ExceptionObject as Exception);
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
        }

        private static void ShowFatalError(Exception ex)
        {
            string message = ex == null ? "Unknown error" : ex.ToString();
            string logPath = "";

            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CodexSiteLauncher");
                Directory.CreateDirectory(dir);
                logPath = Path.Combine(dir, "launcher-error.log");
                File.AppendAllText(
                    logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + message + Environment.NewLine + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch
            {
            }

            try
            {
                string text = "程序启动失败。" + Environment.NewLine + Environment.NewLine + message;
                if (!String.IsNullOrWhiteSpace(logPath))
                {
                    text += Environment.NewLine + Environment.NewLine + "日志：" + logPath;
                }
                MessageBox.Show(text, "Codex API 站点启动器", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }

    [DataContract]
    internal sealed class LauncherConfig
    {
        [DataMember]
        public string CodexPath { get; set; }

        [DataMember]
        public List<ApiSite> Sites { get; set; }
    }

    [DataContract]
    internal sealed class ApiSite
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string BaseUrl { get; set; }

        [DataMember]
        public string EnvKey { get; set; }

        [DataMember]
        public bool BuiltIn { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class ConfigSwitchResult
    {
        public string ConfigPath { get; set; }
        public string PreviousText { get; set; }
        public string AppliedText { get; set; }
        public string OriginalBackupPath { get; set; }
        public string LatestBackupPath { get; set; }
    }

    internal static class AppTheme
    {
        public static readonly Color AppBack = Color.FromArgb(17, 21, 27);
        public static readonly Color Surface = Color.FromArgb(24, 29, 37);
        public static readonly Color SurfaceRaised = Color.FromArgb(31, 38, 48);
        public static readonly Color InputBack = Color.FromArgb(12, 16, 22);
        public static readonly Color Border = Color.FromArgb(38, 46, 58);
        public static readonly Color SubtleBorder = Color.FromArgb(29, 36, 46);
        public static readonly Color PrimaryText = Color.FromArgb(235, 240, 247);
        public static readonly Color MutedText = Color.FromArgb(155, 166, 181);
        public static readonly Color Accent = Color.FromArgb(92, 160, 255);
        public static readonly Color ButtonBack = Color.FromArgb(37, 46, 58);
        public static readonly Color ButtonHover = Color.FromArgb(48, 59, 74);
        public static readonly Color ButtonDown = Color.FromArgb(58, 72, 91);
        public static readonly Color StatusBack = Color.FromArgb(14, 18, 24);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableDarkTitleBar(IntPtr handle)
        {
            try
            {
                int enabled = 1;
                DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
                DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
            }
            catch
            {
            }
        }

        public static void Apply(Control control)
        {
            if (control == null)
            {
                return;
            }

            if (control is Form)
            {
                control.BackColor = AppBack;
                control.ForeColor = PrimaryText;
            }
            else if (control is TextBox)
            {
                control.BackColor = InputBack;
                control.ForeColor = PrimaryText;
                ((TextBox)control).BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ListBox)
            {
                control.BackColor = InputBack;
                control.ForeColor = PrimaryText;
            }
            else if (control is Button)
            {
                var button = (Button)control;
                button.BackColor = ButtonBack;
                button.ForeColor = PrimaryText;
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = ButtonHover;
                button.FlatAppearance.MouseDownBackColor = ButtonDown;
            }
            else if (control is GroupBox)
            {
                control.BackColor = Surface;
                control.ForeColor = PrimaryText;
            }
            else if (control is StatusStrip)
            {
                var statusStrip = (StatusStrip)control;
                statusStrip.BackColor = StatusBack;
                statusStrip.ForeColor = PrimaryText;
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.ForeColor = PrimaryText;
                    item.BackColor = StatusBack;
                }
            }
            else if (control is SplitterPanel)
            {
                control.BackColor = Surface;
                control.ForeColor = PrimaryText;
            }
            else if (control is TableLayoutPanel || control is FlowLayoutPanel || control is SplitContainer)
            {
                control.BackColor = Surface;
                control.ForeColor = PrimaryText;
            }
            else if (control is Label || control is CheckBox)
            {
                control.BackColor = Color.Transparent;
                control.ForeColor = PrimaryText;
            }
            else
            {
                control.BackColor = Surface;
                control.ForeColor = PrimaryText;
            }

            foreach (Control child in control.Controls)
            {
                Apply(child);
            }
        }
    }

    internal sealed class ThemedGroupBox : GroupBox
    {
        public ThemedGroupBox()
        {
            BackColor = AppTheme.Surface;
            ForeColor = AppTheme.PrimaryText;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                new Point(10, 0),
                ForeColor);

            Size textSize = TextRenderer.MeasureText(Text, Font);
            int textY = Font.Height / 2;
            var borderRect = new Rectangle(0, textY, Width - 1, Height - textY - 1);

            using (var pen = new Pen(AppTheme.SubtleBorder))
            {
                int leftTextGap = 8;
                int rightTextGap = leftTextGap + textSize.Width + 8;

                e.Graphics.DrawLine(pen, borderRect.Left, borderRect.Top, leftTextGap, borderRect.Top);
                e.Graphics.DrawLine(pen, rightTextGap, borderRect.Top, borderRect.Right, borderRect.Top);
                e.Graphics.DrawLine(pen, borderRect.Left, borderRect.Bottom, borderRect.Right, borderRect.Bottom);
            }
        }
    }

    internal sealed class DarkSplitContainer : SplitContainer
    {
        public DarkSplitContainer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            BackColor = AppTheme.AppBack;
            SplitterWidth = 8;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(AppTheme.AppBack);
            using (var brush = new SolidBrush(AppTheme.AppBack))
            {
                e.Graphics.FillRectangle(brush, SplitterRectangle);
            }
        }
    }

    [Flags]
    internal enum ActivateOptions
    {
        None = 0,
        DesignMode = 1,
        NoErrorUI = 2,
        NoSplashScreen = 4
    }

    [ComImport]
    [Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IApplicationActivationManager
    {
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            ActivateOptions options,
            out uint processId);

        int ActivateForFile(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string verb,
            out uint processId);

        int ActivateForProtocol(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            out uint processId);
    }

    [ComImport]
    [Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    internal sealed class ApplicationActivationManager
    {
    }

    internal sealed class MainForm : Form
    {
        private const string DefaultCodexAppUserModelId = "OpenAI.Codex_2p2nqsd0c76g0!App";
        private const int HwndBroadcast = 0xffff;
        private const int WmSettingChange = 0x001a;
        private const int SmtoAbortIfHung = 0x0002;
        private const int ConfigRestoreDelayMs = 15000;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            string lParam,
            int flags,
            int timeout,
            out IntPtr result);

        private readonly string configDir;
        private readonly string configPath;
        private readonly List<ConfigSwitchResult> pendingConfigRestores = new List<ConfigSwitchResult>();
        private readonly List<Timer> restoreTimers = new List<Timer>();
        private LauncherConfig config;

        private TextBox codexPathBox;
        private FlowLayoutPanel launchPanel;
        private SplitContainer mainSplit;
        private TextBox filterBox;
        private ListBox siteList;
        private Label siteSummaryLabel;
        private Label selectedMetaLabel;
        private TextBox nameBox;
        private TextBox baseUrlBox;
        private TextBox envKeyBox;
        private TextBox apiKeyBox;
        private CheckBox showKeyBox;
        private ToolStripStatusLabel statusLabel;
        private Button cleanLaunchButton;

        public MainForm()
        {
            Text = "Codex API 站点启动器";
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = AppTheme.AppBack;
            ForeColor = AppTheme.PrimaryText;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = true;
            MinimumSize = new Size(700, 520);
            Width = 980;
            Height = 740;

            configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CodexSiteLauncher");
            configPath = Path.Combine(configDir, "sites.json");

            LoadConfig();
            BuildUi();
            RefreshSiteList();
            SelectFirstSite();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AppTheme.EnableDarkTitleBar(Handle);
        }

        private static List<ApiSite> DefaultSites()
        {
            return new List<ApiSite>
            {
                new ApiSite
                {
                    Id = "facai-api",
                    Name = "Facai API",
                    BaseUrl = "https://api.system-update-center.club/v1",
                    EnvKey = "NEWAPI_API_KEY",
                    BuiltIn = true
                },
                new ApiSite
                {
                    Id = "code-relay",
                    Name = "Code Relay",
                    BaseUrl = "https://api.code-relay.com/",
                    EnvKey = "CODE_RELAY_API_KEY",
                    BuiltIn = true
                },
                new ApiSite
                {
                    Id = "quickrouter",
                    Name = "QuickRouter",
                    BaseUrl = "https://api.quickrouter.ai/v1",
                    EnvKey = "QUICKROUTER_API_KEY",
                    BuiltIn = true
                }
            };
        }

        private void LoadConfig()
        {
            Directory.CreateDirectory(configDir);

            if (File.Exists(configPath))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(configPath))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(LauncherConfig));
                        config = (LauncherConfig)serializer.ReadObject(stream);
                    }
                }
                catch
                {
                    config = null;
                }
            }

            if (config == null)
            {
                config = new LauncherConfig();
            }

            if (config.Sites == null)
            {
                config.Sites = new List<ApiSite>();
            }

            MergeDefaultSites();

            if (String.IsNullOrWhiteSpace(config.CodexPath))
            {
                config.CodexPath = FindCodexExecutable();
            }

            SaveConfig();
        }

        private void MergeDefaultSites()
        {
            foreach (ApiSite site in DefaultSites())
            {
                ApiSite existing = config.Sites.FirstOrDefault(s => SameId(s.Id, site.Id));
                if (existing == null)
                {
                    config.Sites.Add(site);
                }
                else
                {
                    existing.BuiltIn = true;
                    if (String.IsNullOrWhiteSpace(existing.Name)) existing.Name = site.Name;
                    if (String.IsNullOrWhiteSpace(existing.BaseUrl)) existing.BaseUrl = site.BaseUrl;
                    if (String.IsNullOrWhiteSpace(existing.EnvKey)) existing.EnvKey = site.EnvKey;
                }
            }
        }

        private static bool SameId(string left, string right)
        {
            return String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private void SaveConfig()
        {
            Directory.CreateDirectory(configDir);
            using (FileStream stream = File.Create(configPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(LauncherConfig));
                serializer.WriteObject(stream, config);
            }
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.Padding = new Padding(12);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.BackColor = AppTheme.AppBack;
            Controls.Add(root);

            var header = new TableLayoutPanel();
            header.Dock = DockStyle.Top;
            header.AutoSize = true;
            header.ColumnCount = 1;
            header.RowCount = 2;
            header.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(header, 0, 0);

            var title = new Label();
            title.Text = "Codex API 站点启动器";
            title.AutoSize = true;
            title.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = AppTheme.PrimaryText;
            header.Controls.Add(title, 0, 0);

            var subtitle = new Label();
            subtitle.Text = "按站点保存 Key，启动 Codex Desktop 前切换配置和环境变量。";
            subtitle.AutoSize = true;
            subtitle.ForeColor = AppTheme.MutedText;
            subtitle.Margin = new Padding(1, 4, 0, 0);
            header.Controls.Add(subtitle, 0, 1);

            var setupGroup = new ThemedGroupBox();
            setupGroup.Text = "启动设置";
            setupGroup.Dock = DockStyle.Top;
            setupGroup.AutoSize = true;
            setupGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            setupGroup.Margin = new Padding(0, 0, 0, 12);
            root.Controls.Add(setupGroup, 0, 1);

            var setupLayout = new TableLayoutPanel();
            setupLayout.Dock = DockStyle.Top;
            setupLayout.AutoSize = true;
            setupLayout.ColumnCount = 1;
            setupLayout.RowCount = 3;
            setupLayout.Padding = new Padding(12);
            setupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            setupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            setupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            setupGroup.Controls.Add(setupLayout);

            var pathLayout = new TableLayoutPanel();
            pathLayout.Dock = DockStyle.Top;
            pathLayout.AutoSize = true;
            pathLayout.ColumnCount = 3;
            pathLayout.RowCount = 1;
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            setupLayout.Controls.Add(pathLayout, 0, 0);

            pathLayout.Controls.Add(new Label { Text = "Codex.exe：", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            codexPathBox = new TextBox { Dock = DockStyle.Fill, Text = config.CodexPath ?? "", Margin = new Padding(4, 0, 8, 0) };
            pathLayout.Controls.Add(codexPathBox, 1, 0);

            var browseButton = new Button { Text = "浏览", Width = 92, Height = 34 };
            browseButton.Click += BrowseButton_Click;
            pathLayout.Controls.Add(browseButton, 2, 0);

            var cleanLaunchPanel = new FlowLayoutPanel();
            cleanLaunchPanel.Dock = DockStyle.Top;
            cleanLaunchPanel.AutoSize = true;
            cleanLaunchPanel.Margin = new Padding(0, 10, 0, 0);
            setupLayout.Controls.Add(cleanLaunchPanel, 0, 1);

            cleanLaunchButton = new Button { Text = "纯净启动测试", Width = 150, Height = 36 };
            cleanLaunchButton.Click += delegate { LaunchCleanCodex(); };
            cleanLaunchPanel.Controls.Add(cleanLaunchButton);

            var cleanHint = new Label();
            cleanHint.Text = "不改环境变量，仅测试能否打开 Codex Desktop";
            cleanHint.AutoSize = true;
            cleanHint.Anchor = AnchorStyles.Left;
            cleanHint.Margin = new Padding(8, 8, 0, 0);
            cleanHint.ForeColor = AppTheme.MutedText;
            cleanLaunchPanel.Controls.Add(cleanHint);

            var quickGroup = new ThemedGroupBox();
            quickGroup.Text = "快速启动";
            quickGroup.Dock = DockStyle.Top;
            quickGroup.Height = 110;
            quickGroup.Margin = new Padding(0, 12, 0, 0);
            setupLayout.Controls.Add(quickGroup, 0, 2);

            launchPanel = new FlowLayoutPanel();
            launchPanel.Dock = DockStyle.Fill;
            launchPanel.AutoScroll = true;
            launchPanel.WrapContents = true;
            launchPanel.Padding = new Padding(8);
            quickGroup.Controls.Add(launchPanel);

            mainSplit = new DarkSplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Panel1MinSize = 80;
            mainSplit.Panel2MinSize = 80;
            mainSplit.Margin = new Padding(0);
            mainSplit.SizeChanged += delegate { SetSafeSplitterDistance(false); };
            root.Controls.Add(mainSplit, 0, 2);

            var siteGroup = new ThemedGroupBox();
            siteGroup.Text = "站点";
            siteGroup.Dock = DockStyle.Fill;
            mainSplit.Panel1.Controls.Add(siteGroup);

            var leftPanel = new TableLayoutPanel();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.RowCount = 4;
            leftPanel.ColumnCount = 1;
            leftPanel.Padding = new Padding(10);
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            siteGroup.Controls.Add(leftPanel);

            var filterLayout = new TableLayoutPanel();
            filterLayout.Dock = DockStyle.Top;
            filterLayout.AutoSize = true;
            filterLayout.ColumnCount = 2;
            filterLayout.RowCount = 1;
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filterLayout.Margin = new Padding(0, 0, 0, 8);
            leftPanel.Controls.Add(filterLayout, 0, 0);

            filterLayout.Controls.Add(new Label { Text = "筛选：", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            filterBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 0, 0) };
            filterBox.TextChanged += delegate { RefreshSiteList(); };
            filterLayout.Controls.Add(filterBox, 1, 0);

            siteSummaryLabel = new Label();
            siteSummaryLabel.AutoSize = true;
            siteSummaryLabel.ForeColor = AppTheme.MutedText;
            siteSummaryLabel.Margin = new Padding(0, 0, 0, 8);
            leftPanel.Controls.Add(siteSummaryLabel, 0, 1);

            siteList = new ListBox();
            siteList.Dock = DockStyle.Fill;
            siteList.IntegralHeight = false;
            siteList.HorizontalScrollbar = true;
            siteList.Font = new Font(Font.FontFamily, 11F, FontStyle.Regular, GraphicsUnit.Point);
            siteList.SelectedIndexChanged += SiteList_SelectedIndexChanged;
            leftPanel.Controls.Add(siteList, 0, 2);

            var listButtons = new FlowLayoutPanel();
            listButtons.Dock = DockStyle.Fill;
            listButtons.AutoSize = true;
            listButtons.WrapContents = true;
            listButtons.Margin = new Padding(0, 10, 0, 0);
            leftPanel.Controls.Add(listButtons, 0, 3);

            var addButton = new Button { Text = "新增", Width = 86, Height = 34 };
            addButton.Click += AddButton_Click;
            listButtons.Controls.Add(addButton);

            var editButton = new Button { Text = "保存修改", Width = 108, Height = 34 };
            editButton.Click += EditButton_Click;
            listButtons.Controls.Add(editButton);

            var deleteButton = new Button { Text = "删除", Width = 86, Height = 34 };
            deleteButton.Click += DeleteButton_Click;
            listButtons.Controls.Add(deleteButton);

            var detailGroup = new ThemedGroupBox();
            detailGroup.Text = "站点设置";
            detailGroup.Dock = DockStyle.Fill;
            mainSplit.Panel2.Controls.Add(detailGroup);

            var detail = new TableLayoutPanel();
            detail.Dock = DockStyle.Fill;
            detail.AutoScroll = true;
            detail.Padding = new Padding(14);
            detail.ColumnCount = 2;
            detail.RowCount = 8;
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            detailGroup.Controls.Add(detail);

            selectedMetaLabel = new Label();
            selectedMetaLabel.AutoSize = true;
            selectedMetaLabel.ForeColor = AppTheme.MutedText;
            selectedMetaLabel.Margin = new Padding(0, 0, 0, 12);
            detail.Controls.Add(selectedMetaLabel, 0, 0);
            detail.SetColumnSpan(selectedMetaLabel, 2);

            detail.Controls.Add(new Label { Text = "名称：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) }, 0, 1);
            nameBox = new TextBox { Dock = DockStyle.Fill };
            detail.Controls.Add(nameBox, 1, 1);

            detail.Controls.Add(new Label { Text = "Base URL：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) }, 0, 2);
            baseUrlBox = new TextBox { Dock = DockStyle.Fill };
            detail.Controls.Add(baseUrlBox, 1, 2);

            detail.Controls.Add(new Label { Text = "环境变量：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) }, 0, 3);
            envKeyBox = new TextBox { Dock = DockStyle.Fill };
            detail.Controls.Add(envKeyBox, 1, 3);

            detail.Controls.Add(new Label { Text = "API Key：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) }, 0, 4);
            apiKeyBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            detail.Controls.Add(apiKeyBox, 1, 4);

            var keyButtons = new FlowLayoutPanel();
            keyButtons.Dock = DockStyle.Fill;
            keyButtons.AutoSize = true;
            keyButtons.WrapContents = true;
            keyButtons.Margin = new Padding(0, 10, 0, 8);
            detail.Controls.Add(new Label(), 0, 5);
            detail.Controls.Add(keyButtons, 1, 5);

            var saveKeyButton = new Button { Text = "保存 Key 到用户环境变量", Width = 220, Height = 34 };
            saveKeyButton.Click += SaveKeyButton_Click;
            keyButtons.Controls.Add(saveKeyButton);

            var reloadKeyButton = new Button { Text = "重载 Key", Width = 100, Height = 34 };
            reloadKeyButton.Click += ReloadKeyButton_Click;
            keyButtons.Controls.Add(reloadKeyButton);

            showKeyBox = new CheckBox { Text = "显示", AutoSize = true };
            showKeyBox.CheckedChanged += delegate { apiKeyBox.UseSystemPasswordChar = !showKeyBox.Checked; };
            keyButtons.Controls.Add(showKeyBox);

            var actionButtons = new FlowLayoutPanel();
            actionButtons.Dock = DockStyle.Fill;
            actionButtons.AutoSize = true;
            actionButtons.WrapContents = true;
            detail.Controls.Add(new Label(), 0, 6);
            detail.Controls.Add(actionButtons, 1, 6);

            var saveSiteButton = new Button { Text = "保存站点设置", Height = 40, Width = 150 };
            saveSiteButton.Click += EditButton_Click;
            actionButtons.Controls.Add(saveSiteButton);

            var launchSelectedButton = new Button { Text = "启动当前站点", Height = 40, Width = 160 };
            launchSelectedButton.Click += delegate { LaunchSelectedSite(); };
            actionButtons.Controls.Add(launchSelectedButton);

            var hint = new Label();
            hint.Dock = DockStyle.Top;
            hint.Text = "说明：快速启动会切换 config.toml 的 API 站点，并保留回滚备份；切换前请完全退出 Codex。";
            hint.ForeColor = AppTheme.MutedText;
            hint.Margin = new Padding(0, 16, 0, 0);
            detail.Controls.Add(new Label(), 0, 7);
            detail.Controls.Add(hint, 1, 7);

            var statusStrip = new StatusStrip();
            statusStrip.SizingGrip = true;
            statusStrip.BackColor = AppTheme.StatusBack;
            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "就绪";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(statusLabel);
            root.Controls.Add(statusStrip, 0, 3);

            AppTheme.Apply(this);
            root.BackColor = AppTheme.AppBack;
            title.ForeColor = AppTheme.PrimaryText;
            subtitle.ForeColor = AppTheme.MutedText;
            siteSummaryLabel.ForeColor = AppTheme.MutedText;
            selectedMetaLabel.ForeColor = AppTheme.MutedText;
            hint.ForeColor = AppTheme.MutedText;
            statusStrip.BackColor = AppTheme.StatusBack;
            statusLabel.ForeColor = AppTheme.PrimaryText;

            Shown += delegate { SetSafeSplitterDistance(true); };
        }

        private void SetSafeSplitterDistance(bool preferDefault)
        {
            if (mainSplit == null || mainSplit.Width <= 0)
            {
                return;
            }

            int min = mainSplit.Panel1MinSize;
            int max = mainSplit.Width - mainSplit.Panel2MinSize - mainSplit.SplitterWidth;
            if (max < min)
            {
                return;
            }

            int target = preferDefault ? 300 : mainSplit.SplitterDistance;
            if (target < min) target = min;
            if (target > max) target = max;

            if (mainSplit.SplitterDistance != target)
            {
                mainSplit.SplitterDistance = target;
            }
        }

        private void RefreshSiteList()
        {
            ApiSite selected = GetSelectedSite();
            string selectedId = selected == null ? null : selected.Id;
            string filter = filterBox == null ? "" : filterBox.Text.Trim();
            List<ApiSite> visibleSites = config.Sites
                .Where(site => MatchesSiteFilter(site, filter))
                .ToList();

            siteList.BeginUpdate();
            siteList.Items.Clear();
            foreach (ApiSite site in visibleSites)
            {
                siteList.Items.Add(site);
            }
            siteList.EndUpdate();

            if (siteSummaryLabel != null)
            {
                siteSummaryLabel.Text = String.IsNullOrWhiteSpace(filter)
                    ? config.Sites.Count + " 个站点"
                    : visibleSites.Count + " / " + config.Sites.Count + " 个站点";
            }

            launchPanel.Controls.Clear();
            foreach (ApiSite site in config.Sites)
            {
                var button = new Button();
                button.Text = "启动 " + site.Name;
                button.Tag = site;
                button.Height = 42;
                button.Width = 172;
                button.Margin = new Padding(4);
                button.Click += delegate(object sender, EventArgs args)
                {
                    var clicked = (ApiSite)((Button)sender).Tag;
                    if (!SelectSite(clicked.Id) && filterBox != null)
                    {
                        filterBox.Text = "";
                        SelectSite(clicked.Id);
                    }
                    LaunchSite(clicked);
                };
                launchPanel.Controls.Add(button);
            }

            bool restored = selectedId != null && SelectSite(selectedId);
            if (!restored)
            {
                SelectFirstSite();
            }
            if (siteList.Items.Count == 0)
            {
                SiteList_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private static bool MatchesSiteFilter(ApiSite site, string filter)
        {
            if (String.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return ContainsIgnoreCase(site.Name, filter) ||
                ContainsIgnoreCase(site.BaseUrl, filter) ||
                ContainsIgnoreCase(site.EnvKey, filter);
        }

        private static bool ContainsIgnoreCase(string value, string filter)
        {
            return value != null &&
                value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectFirstSite()
        {
            if (siteList.Items.Count > 0 && siteList.SelectedIndex < 0)
            {
                siteList.SelectedIndex = 0;
            }
        }

        private bool SelectSite(string id)
        {
            for (int i = 0; i < siteList.Items.Count; i++)
            {
                ApiSite site = (ApiSite)siteList.Items[i];
                if (SameId(site.Id, id))
                {
                    siteList.SelectedIndex = i;
                    return true;
                }
            }
            return false;
        }

        private ApiSite GetSelectedSite()
        {
            return siteList.SelectedItem as ApiSite;
        }

        private void SiteList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApiSite site = GetSelectedSite();
            if (site == null)
            {
                nameBox.Text = "";
                baseUrlBox.Text = "";
                envKeyBox.Text = "";
                apiKeyBox.Text = "";
                if (selectedMetaLabel != null) selectedMetaLabel.Text = "未选择站点";
                if (statusLabel != null) statusLabel.Text = "未选择站点";
                return;
            }

            nameBox.Text = site.Name;
            baseUrlBox.Text = site.BaseUrl;
            envKeyBox.Text = site.EnvKey;
            apiKeyBox.Text = GetEnvironmentVariable(site.EnvKey);
            bool hasKey = !String.IsNullOrWhiteSpace(apiKeyBox.Text);
            selectedMetaLabel.Text = (site.BuiltIn ? "内置站点" : "自定义站点") +
                " · " + site.EnvKey +
                " · Key " + (hasKey ? "已设置" : "未设置");
            statusLabel.Text = site.Name + " · " +
                (site.BuiltIn ? "内置站点可修改，不能删除" : "自定义站点");
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "选择 Codex Desktop 的 Codex.exe";
                dialog.Filter = "Codex or shortcut|Codex.exe;*.lnk|Executable files (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk|All files (*.*)|*.*";
                dialog.CheckFileExists = true;
                if (File.Exists(codexPathBox.Text))
                {
                    dialog.FileName = codexPathBox.Text;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    codexPathBox.Text = dialog.FileName;
                    config.CodexPath = codexPathBox.Text.Trim();
                    SaveConfig();
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var site = new ApiSite
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Custom API",
                BaseUrl = "https://",
                EnvKey = "CUSTOM_CODEX_API_KEY",
                BuiltIn = false
            };

            using (var dialog = new SiteEditForm(site, true))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    config.Sites.Add(dialog.EditedSite);
                    SaveConfig();
                    RefreshSiteList();
                    SelectSite(dialog.EditedSite.Id);
                }
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            ApiSite site = GetSelectedSite();
            if (site == null)
            {
                return;
            }

            site.Name = nameBox.Text.Trim();
            site.BaseUrl = baseUrlBox.Text.Trim();
            site.EnvKey = envKeyBox.Text.Trim();

            string validation = ValidateSite(site);
            if (validation != null)
            {
                MessageBox.Show(this, validation, "配置不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveConfig();
            RefreshSiteList();
            SelectSite(site.Id);
            statusLabel.Text = "站点修改已保存。";
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            ApiSite site = GetSelectedSite();
            if (site == null)
            {
                return;
            }

            if (site.BuiltIn)
            {
                MessageBox.Show(this, "内置站点不能删除；可以修改名称、地址或环境变量。", "不能删除", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(this, "删除自定义站点：" + site.Name + "？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            config.Sites.Remove(site);
            SaveConfig();
            RefreshSiteList();
            SelectFirstSite();
        }

        private void SaveKeyButton_Click(object sender, EventArgs e)
        {
            ApiSite site = GetSelectedSite();
            if (site == null)
            {
                return;
            }

            site.EnvKey = envKeyBox.Text.Trim();
            string key = apiKeyBox.Text.Trim();
            if (String.IsNullOrWhiteSpace(site.EnvKey))
            {
                MessageBox.Show(this, "环境变量名不能为空。", "配置不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Environment.SetEnvironmentVariable(site.EnvKey, key, EnvironmentVariableTarget.User);
            SaveConfig();
            statusLabel.Text = "已保存 " + site.EnvKey + " 到 Windows 用户环境变量。";
        }

        private void ReloadKeyButton_Click(object sender, EventArgs e)
        {
            ApiSite site = GetSelectedSite();
            if (site == null)
            {
                return;
            }

            apiKeyBox.Text = GetEnvironmentVariable(envKeyBox.Text.Trim());
            statusLabel.Text = "已从环境变量重载。";
        }

        private void LaunchSelectedSite()
        {
            LaunchSite(GetSelectedSite());
        }

        private void LaunchCleanCodex()
        {
            config.CodexPath = codexPathBox.Text.Trim();
            SaveConfig();

            try
            {
                LaunchCodexDesktop("");
                statusLabel.Text = "已执行纯净启动。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "纯净启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchSite(ApiSite site)
        {
            if (site == null)
            {
                return;
            }

            site.Name = nameBox.Text.Trim();
            site.BaseUrl = baseUrlBox.Text.Trim();
            site.EnvKey = envKeyBox.Text.Trim();
            config.CodexPath = codexPathBox.Text.Trim();

            string validation = ValidateSite(site);
            if (validation != null)
            {
                MessageBox.Show(this, validation, "配置不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string key = apiKeyBox.Text.Trim();
            if (String.IsNullOrWhiteSpace(key))
            {
                key = GetEnvironmentVariable(site.EnvKey);
            }

            if (String.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show(this, "未找到 " + site.EnvKey + "。请先填写 API Key 并保存。", "缺少 API Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string target = ResolveLaunchTarget(config.CodexPath);
            bool usePackagedActivation = ShouldUsePackagedActivation(target);
            if (!usePackagedActivation && !File.Exists(target))
            {
                string appId = FindCodexAppUserModelId();
                if (!String.IsNullOrWhiteSpace(appId))
                {
                    usePackagedActivation = true;
                }
                else
                {
                    MessageBox.Show(this, "找不到 Codex.exe，也找不到 Codex 的 AppID。请确认 Codex Desktop 已安装。", "找不到 Codex", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            ApplySiteEnvironment(site, key);
            ConfigSwitchResult configSwitch = UpdateCodexConfigForSite(site);
            SaveConfig();

            if (!ConfirmIfCodexRunning(usePackagedActivation ? "Codex.exe" : config.CodexPath))
            {
                statusLabel.Text = "已取消启动。";
                return;
            }

            try
            {
                LaunchCodexDesktop(usePackagedActivation ? "" : BuildCodexArguments(site));
                ScheduleConfigRestore(configSwitch, site.Name);
                statusLabel.Text = "已临时切换配置并启动 " + site.Name + "，15 秒后恢复 config.toml。";
            }
            catch (Exception ex)
            {
                RestoreConfig(configSwitch);
                MessageBox.Show(this, ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ValidateSite(ApiSite site)
        {
            if (site == null) return "未选择站点。";
            if (String.IsNullOrWhiteSpace(site.Name)) return "站点名称不能为空。";
            if (String.IsNullOrWhiteSpace(site.BaseUrl)) return "Base URL 不能为空。";
            if (String.IsNullOrWhiteSpace(site.EnvKey)) return "环境变量名不能为空。";
            Uri uri;
            if (!Uri.TryCreate(site.BaseUrl, UriKind.Absolute, out uri)) return "Base URL 格式不正确。";
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return "Base URL 必须是 http 或 https。";
            return null;
        }

        private static string GetEnvironmentVariable(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return "";

            string value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            if (String.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            }
            if (String.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            }
            return value ?? "";
        }

        private static void ApplySiteEnvironment(ApiSite site, string key)
        {
            Environment.SetEnvironmentVariable(site.EnvKey, key, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", key, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("OPENAI_BASE_URL", site.BaseUrl, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("OPENAI_API_BASE_URL", site.BaseUrl, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable(site.EnvKey, key, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", key, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("OPENAI_BASE_URL", site.BaseUrl, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("OPENAI_API_BASE_URL", site.BaseUrl, EnvironmentVariableTarget.Process);

            BroadcastEnvironmentChange();
        }

        private static ConfigSwitchResult UpdateCodexConfigForSite(ApiSite site)
        {
            string codexDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex");
            Directory.CreateDirectory(codexDir);

            string configPath = Path.Combine(codexDir, "config.toml");
            string text = File.Exists(configPath)
                ? File.ReadAllText(configPath, Encoding.UTF8)
                : "";

            string originalBackup = Path.Combine(codexDir, "config.toml.before-site-launcher.txt");
            if (!File.Exists(originalBackup))
            {
                File.WriteAllText(originalBackup, text, Encoding.UTF8);
            }

            string latestBackup = Path.Combine(codexDir, "config.toml.last-before-site-launcher.txt");
            File.WriteAllText(latestBackup, text, Encoding.UTF8);

            string updated = text;
            updated = SetTopLevelSetting(updated, "model_provider", "newapi");
            updated = SetProviderBlock(updated, site);
            updated = SetWindowsSandbox(updated);

            File.WriteAllText(configPath, updated, Encoding.UTF8);
            return new ConfigSwitchResult
            {
                ConfigPath = configPath,
                PreviousText = text,
                AppliedText = updated,
                OriginalBackupPath = originalBackup,
                LatestBackupPath = latestBackup
            };
        }

        private void ScheduleConfigRestore(ConfigSwitchResult configSwitch, string siteName)
        {
            if (configSwitch == null)
            {
                return;
            }

            pendingConfigRestores.Add(configSwitch);
            var timer = new Timer();
            timer.Interval = ConfigRestoreDelayMs;
            timer.Tick += delegate
            {
                timer.Stop();
                restoreTimers.Remove(timer);
                timer.Dispose();
                pendingConfigRestores.Remove(configSwitch);

                bool restored = RestoreConfig(configSwitch);
                statusLabel.Text = restored
                    ? "已恢复启动前配置：" + siteName
                    : "未恢复配置：config.toml 已被其它进程修改，请查看备份。";
            };
            restoreTimers.Add(timer);
            timer.Start();
        }

        private static bool RestoreConfig(ConfigSwitchResult configSwitch)
        {
            if (configSwitch == null ||
                String.IsNullOrWhiteSpace(configSwitch.ConfigPath) ||
                !File.Exists(configSwitch.ConfigPath))
            {
                return false;
            }

            string current = File.ReadAllText(configSwitch.ConfigPath, Encoding.UTF8);
            if (!String.Equals(current, configSwitch.AppliedText, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(configSwitch.ConfigPath, configSwitch.PreviousText ?? "", Encoding.UTF8);
            return true;
        }

        private static string SetTopLevelSetting(string text, string key, string value)
        {
            string line = key + " = \"" + EscapeTomlString(value) + "\"";
            var pattern = new Regex(@"(?m)^" + Regex.Escape(key) + @"\s*=\s*""[^""]*""\s*$");

            if (pattern.IsMatch(text))
            {
                return pattern.Replace(text, line, 1);
            }

            return line + Environment.NewLine + text.TrimStart();
        }

        private static string SetProviderBlock(string text, ApiSite site)
        {
            string block =
                "[model_providers.newapi]" + Environment.NewLine +
                "name = \"" + EscapeTomlString(site.Name) + "\"" + Environment.NewLine +
                "base_url = \"" + EscapeTomlString(site.BaseUrl) + "\"" + Environment.NewLine +
                "env_key = \"" + EscapeTomlString(site.EnvKey) + "\"" + Environment.NewLine +
                "wire_api = \"responses\"" + Environment.NewLine +
                Environment.NewLine;

            var providerPattern = new Regex(@"(?ms)^\[model_providers\.newapi\]\s*\r?\n.*?(?=^\[|\z)");
            if (providerPattern.IsMatch(text))
            {
                return providerPattern.Replace(text, block, 1);
            }

            int insertAt = FindProviderInsertPosition(text);
            if (insertAt >= 0)
            {
                return text.Insert(insertAt, block);
            }

            return text.TrimEnd() + Environment.NewLine + Environment.NewLine + block;
        }

        private static string SetWindowsSandbox(string text)
        {
            string block =
                "[windows]" + Environment.NewLine +
                "sandbox = \"unelevated\"" + Environment.NewLine +
                Environment.NewLine;

            var windowsPattern = new Regex(@"(?ms)^\[windows\]\s*\r?\n.*?(?=^\[|\z)");
            if (windowsPattern.IsMatch(text))
            {
                return windowsPattern.Replace(text, block, 1);
            }

            int insertAt = FindProviderInsertPosition(text);
            if (insertAt >= 0)
            {
                return text.Insert(insertAt, block);
            }

            return text.TrimEnd() + Environment.NewLine + Environment.NewLine + block;
        }

        private static int FindProviderInsertPosition(string text)
        {
            string[] markers =
            {
                "# --- 系统安全配置 ---",
                "[windows]",
                "[marketplaces.",
                "[plugins.",
                "# --- 信任的工作区 ---",
                "[projects."
            };

            foreach (string marker in markers)
            {
                int index = text.IndexOf(marker, StringComparison.Ordinal);
                if (index >= 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string EscapeTomlString(string value)
        {
            return (value ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static void BroadcastEnvironmentChange()
        {
            try
            {
                IntPtr result;
                SendMessageTimeout(
                    new IntPtr(HwndBroadcast),
                    WmSettingChange,
                    IntPtr.Zero,
                    "Environment",
                    SmtoAbortIfHung,
                    3000,
                    out result);
            }
            catch
            {
            }
        }

        private static bool ShouldUsePackagedActivation(string codexPath)
        {
            if (String.IsNullOrWhiteSpace(codexPath))
            {
                return true;
            }

            return codexPath.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                codexPath.IndexOf("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                codexPath.IndexOf("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                codexPath.IndexOf("!App", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void LaunchCodexDesktop(string arguments)
        {
            string target = ResolveLaunchTarget(config.CodexPath);
            if (String.IsNullOrWhiteSpace(target))
            {
                target = config.CodexPath;
            }

            if (String.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("没有找到可启动的 Codex 路径。");
            }

            if (ShouldUsePackagedActivation(target))
            {
                LaunchPackagedCodex(arguments);
                return;
            }

            var psi = new ProcessStartInfo();
            psi.FileName = target;
            psi.WorkingDirectory = Path.GetDirectoryName(target);
            psi.UseShellExecute = false;
            psi.Arguments = arguments ?? "";

            Process.Start(psi);
        }

        private static void LaunchPackagedCodex(string arguments)
        {
            string appId = FindCodexAppUserModelId();
            if (String.IsNullOrWhiteSpace(appId))
            {
                appId = DefaultCodexAppUserModelId;
            }

            object managerObject = Activator.CreateInstance(
                Type.GetTypeFromCLSID(new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")));
            var manager = (IApplicationActivationManager)managerObject;
            uint processId;
            int hr = manager.ActivateApplication(
                appId,
                arguments ?? "",
                ActivateOptions.None,
                out processId);

            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        private static string BuildCodexArguments(ApiSite site)
        {
            var args = new List<string>();
            args.Add("-c");
            args.Add("model_provider=\"newapi\"");
            args.Add("-c");
            args.Add("model_providers.newapi.base_url=\"" + site.BaseUrl + "\"");
            args.Add("-c");
            args.Add("model_providers.newapi.env_key=\"" + site.EnvKey + "\"");
            args.Add("-c");
            args.Add("model_providers.newapi.wire_api=\"responses\"");
            return String.Join(" ", args.Select(QuoteArgument).ToArray());
        }

        private static string ResolveLaunchTarget(string pathOrShortcut)
        {
            if (String.IsNullOrWhiteSpace(pathOrShortcut))
            {
                string appId = FindCodexAppUserModelId();
                return String.IsNullOrWhiteSpace(appId) ? "" : "shell:AppsFolder\\" + appId;
            }

            string trimmed = pathOrShortcut.Trim();
            if (trimmed.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (trimmed.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                string resolved = ResolveShortcutTarget(trimmed);
                if (!String.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }

            return trimmed;
        }

        private static string ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return "";
                }

                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { shortcutPath });

                string target = (string)shortcut.GetType().InvokeMember(
                    "TargetPath",
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    shortcut,
                    null);

                string args = (string)shortcut.GetType().InvokeMember(
                    "Arguments",
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    shortcut,
                    null);

                if (!String.IsNullOrWhiteSpace(target))
                {
                    if (!String.IsNullOrWhiteSpace(args))
                    {
                        return target + " " + args;
                    }
                    return target;
                }
            }
            catch
            {
            }

            return "";
        }

        private static string QuoteArgument(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var result = new StringBuilder();
            result.Append('"');
            int slashCount = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    slashCount++;
                }
                else if (c == '"')
                {
                    result.Append('\\', slashCount * 2 + 1);
                    result.Append('"');
                    slashCount = 0;
                }
                else
                {
                    result.Append('\\', slashCount);
                    slashCount = 0;
                    result.Append(c);
                }
            }
            result.Append('\\', slashCount * 2);
            result.Append('"');
            return result.ToString();
        }

        private bool ConfirmIfCodexRunning(string codexPath)
        {
            string processName = Path.GetFileNameWithoutExtension(codexPath);
            if (String.IsNullOrWhiteSpace(processName))
            {
                return true;
            }

            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch
            {
                return true;
            }

            if (processes.Length == 0)
            {
                return true;
            }

            DialogResult result = MessageBox.Show(
                this,
                "检测到 Codex 可能已经在运行。建议先完全退出 Codex Desktop 再切换站点，否则旧进程可能继续使用旧环境。\n\n仍要继续启动吗？",
                "Codex 正在运行",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }

        private static string FindCodexExecutable()
        {
            var candidates = new List<string>();
            AddEnvPath(candidates, "CODEX_DESKTOP_PATH", EnvironmentVariableTarget.User);
            AddEnvPath(candidates, "CODEX_DESKTOP_PATH", EnvironmentVariableTarget.Process);
            AddEnvPath(candidates, "CODEX_DESKTOP_PATH", EnvironmentVariableTarget.Machine);
            AddIfNotEmpty(candidates, FindCodexFromAppxPackage());

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            candidates.Add(Path.Combine(local, "Programs", "Codex", "Codex.exe"));
            candidates.Add(Path.Combine(local, "Programs", "codex", "Codex.exe"));
            candidates.Add(Path.Combine(local, "Codex", "Codex.exe"));
            candidates.Add(Path.Combine(programFiles, "Codex", "Codex.exe"));
            candidates.Add(Path.Combine(programFilesX86, "Codex", "Codex.exe"));

            string fromPath = FindOnPath("Codex.exe");
            if (!String.IsNullOrWhiteSpace(fromPath))
            {
                candidates.Add(fromPath);
            }

            foreach (string candidate in candidates)
            {
                if (!String.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return "";
        }

        private static string FindCodexAppUserModelId()
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"(Get-StartApps | Where-Object { $_.Name -eq 'Codex' -or $_.Name -match 'OpenAI' } | Select-Object -First 1 -ExpandProperty AppID)\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        return "";
                    }

                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(3000);
                    return output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static string FindCodexFromAppxPackage()
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"$p=(Get-AppxPackage -Name OpenAI.Codex -ErrorAction SilentlyContinue).InstallLocation; if ($p) { Join-Path $p 'app\\Codex.exe' }\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        return "";
                    }

                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(3000);

                    string firstLine = output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault();
                    return firstLine ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static void AddEnvPath(List<string> candidates, string name, EnvironmentVariableTarget target)
        {
            string value = Environment.GetEnvironmentVariable(name, target);
            if (!String.IsNullOrWhiteSpace(value))
            {
                candidates.Add(value);
            }
        }

        private static void AddIfNotEmpty(List<string> candidates, string value)
        {
            if (!String.IsNullOrWhiteSpace(value))
            {
                candidates.Add(value);
            }
        }

        private static string FindOnPath(string fileName)
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in path.Split(Path.PathSeparator))
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(dir)) continue;
                    string candidate = Path.Combine(dir.Trim(), fileName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                }
            }
            return "";
        }
    }

    internal sealed class SiteEditForm : Form
    {
        private readonly TextBox nameBox;
        private readonly TextBox baseUrlBox;
        private readonly TextBox envKeyBox;

        public ApiSite EditedSite { get; private set; }

        public SiteEditForm(ApiSite site, bool isNew)
        {
            EditedSite = new ApiSite
            {
                Id = site.Id,
                Name = site.Name,
                BaseUrl = site.BaseUrl,
                EnvKey = site.EnvKey,
                BuiltIn = site.BuiltIn
            };

            Text = isNew ? "新增站点" : "修改站点";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = AppTheme.AppBack;
            ForeColor = AppTheme.PrimaryText;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = true;
            Width = 600;
            Height = 280;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            layout.ColumnCount = 2;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            layout.Controls.Add(new Label { Text = "名称：", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            nameBox = new TextBox { Dock = DockStyle.Fill, Text = EditedSite.Name };
            layout.Controls.Add(nameBox, 1, 0);

            layout.Controls.Add(new Label { Text = "Base URL：", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            baseUrlBox = new TextBox { Dock = DockStyle.Fill, Text = EditedSite.BaseUrl };
            layout.Controls.Add(baseUrlBox, 1, 1);

            layout.Controls.Add(new Label { Text = "环境变量：", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            envKeyBox = new TextBox { Dock = DockStyle.Fill, Text = EditedSite.EnvKey };
            layout.Controls.Add(envKeyBox, 1, 2);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            layout.Controls.Add(new Label(), 0, 3);
            layout.Controls.Add(buttons, 1, 3);

            var okButton = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 96, Height = 34 };
            okButton.Click += OkButton_Click;
            buttons.Controls.Add(okButton);

            var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 96, Height = 34 };
            buttons.Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            AppTheme.Apply(this);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AppTheme.EnableDarkTitleBar(Handle);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            EditedSite.Name = nameBox.Text.Trim();
            EditedSite.BaseUrl = baseUrlBox.Text.Trim();
            EditedSite.EnvKey = envKeyBox.Text.Trim();

            Uri uri;
            if (String.IsNullOrWhiteSpace(EditedSite.Name) ||
                String.IsNullOrWhiteSpace(EditedSite.BaseUrl) ||
                String.IsNullOrWhiteSpace(EditedSite.EnvKey) ||
                !Uri.TryCreate(EditedSite.BaseUrl, UriKind.Absolute, out uri))
            {
                MessageBox.Show(this, "请填写完整且有效的站点信息。", "配置不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        }
    }
}
