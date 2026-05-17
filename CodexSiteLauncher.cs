using System;
using System.Collections;
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
using System.Web.Script.Serialization;
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
                LauncherLog.Error("Fatal application error.", ex);
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

    internal sealed class ConfigRestoreResult
    {
        public bool Success { get; set; }
        public bool PreservedExternalChanges { get; set; }
        public string Error { get; set; }
    }

    [DataContract]
    internal sealed class HistorySyncStore
    {
        [DataMember]
        public List<HistorySyncEntry> Entries { get; set; }
    }

    [DataContract]
    internal sealed class HistorySyncEntry
    {
        [DataMember]
        public string OriginalId { get; set; }

        [DataMember]
        public string DuplicateId { get; set; }

        [DataMember]
        public string SourceProvider { get; set; }

        [DataMember]
        public string TargetProvider { get; set; }

        [DataMember]
        public string DuplicatePath { get; set; }

        [DataMember]
        public string CreatedAt { get; set; }
    }

    internal sealed class HistoryThreadRow
    {
        public string Id { get; set; }
        public string RolloutPath { get; set; }
        public string Title { get; set; }
        public long UpdatedAt { get; set; }
        public int Archived { get; set; }
    }

    internal sealed class SessionIndexRecord
    {
        public string Id { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public string RawLine { get; set; }
    }

    internal class HistorySyncEstimate
    {
        public string SourceProvider { get; set; }
        public string TargetProvider { get; set; }
        public string StateDbPath { get; set; }
        public int SourceCount { get; set; }
        public int ExistingCopies { get; set; }
        public int CopyCount { get; set; }
        public int MissingFiles { get; set; }
        public long CopyBytes { get; set; }
    }

    internal sealed class HistorySyncResult : HistorySyncEstimate
    {
        public int CopiedCount { get; set; }
        public string StateBackupPath { get; set; }
        public string StateWalBackupPath { get; set; }
        public string SessionIndexBackupPath { get; set; }
        public string GlobalStateBackupPath { get; set; }
        public int SessionIndexSyncedCount { get; set; }
    }

    internal static class SQLiteNative
    {
        public const int Ok = 0;
        public const int Row = 100;
        public const int Done = 101;
        public const int Integer = 1;
        public const int Text = 3;
        public const int Null = 5;

        public static readonly IntPtr Transient = new IntPtr(-1);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Open16(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            out IntPtr db);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Close(IntPtr db);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ErrorMessage16(IntPtr db);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_prepare16_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Prepare16(
            IntPtr db,
            [MarshalAs(UnmanagedType.LPWStr)] string sql,
            int byteCount,
            out IntPtr stmt,
            IntPtr tail);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Step(IntPtr stmt);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Finalize(IntPtr stmt);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindParameterIndex(
            IntPtr stmt,
            [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindText16(
            IntPtr stmt,
            int index,
            [MarshalAs(UnmanagedType.LPWStr)] string value,
            int byteCount,
            IntPtr destructor);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindInt64(IntPtr stmt, int index, long value);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BindNull(IntPtr stmt, int index);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ColumnText16(IntPtr stmt, int column);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern long ColumnInt64(IntPtr stmt, int column);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ColumnType(IntPtr stmt, int column);
    }

    internal sealed class SQLiteDatabase : IDisposable
    {
        private IntPtr db;

        public SQLiteDatabase(string path)
        {
            int rc = SQLiteNative.Open16(path, out db);
            if (rc != SQLiteNative.Ok)
            {
                throw new InvalidOperationException("无法打开 SQLite 数据库：" + path);
            }

            Execute("PRAGMA busy_timeout = 5000");
            Execute("PRAGMA foreign_keys = ON");
        }

        public void Dispose()
        {
            if (db != IntPtr.Zero)
            {
                SQLiteNative.Close(db);
                db = IntPtr.Zero;
            }
        }

        public SQLiteStatement Prepare(string sql)
        {
            IntPtr stmt;
            int rc = SQLiteNative.Prepare16(db, sql, -1, out stmt, IntPtr.Zero);
            if (rc != SQLiteNative.Ok)
            {
                throw new InvalidOperationException(ErrorMessage());
            }
            return new SQLiteStatement(this, stmt);
        }

        public void Execute(string sql)
        {
            using (SQLiteStatement stmt = Prepare(sql))
            {
                stmt.Execute();
            }
        }

        public string ErrorMessage()
        {
            IntPtr ptr = SQLiteNative.ErrorMessage16(db);
            string message = Marshal.PtrToStringUni(ptr);
            return String.IsNullOrWhiteSpace(message) ? "SQLite 操作失败。" : message;
        }
    }

    internal sealed class SQLiteStatement : IDisposable
    {
        private readonly SQLiteDatabase database;
        private IntPtr stmt;

        public SQLiteStatement(SQLiteDatabase database, IntPtr stmt)
        {
            this.database = database;
            this.stmt = stmt;
        }

        public void Dispose()
        {
            if (stmt != IntPtr.Zero)
            {
                SQLiteNative.Finalize(stmt);
                stmt = IntPtr.Zero;
            }
        }

        public void BindText(string name, string value)
        {
            int index = ParameterIndex(name);
            int rc = value == null
                ? SQLiteNative.BindNull(stmt, index)
                : SQLiteNative.BindText16(stmt, index, value, -1, SQLiteNative.Transient);
            Check(rc);
        }

        public void BindInt64(string name, long value)
        {
            Check(SQLiteNative.BindInt64(stmt, ParameterIndex(name), value));
        }

        public int Step()
        {
            return SQLiteNative.Step(stmt);
        }

        public void Execute()
        {
            while (true)
            {
                int rc = Step();
                if (rc == SQLiteNative.Done)
                {
                    return;
                }
                if (rc == SQLiteNative.Row)
                {
                    continue;
                }
                throw new InvalidOperationException(database.ErrorMessage());
            }
        }

        public string ColumnText(int index)
        {
            if (SQLiteNative.ColumnType(stmt, index) == SQLiteNative.Null)
            {
                return null;
            }

            IntPtr ptr = SQLiteNative.ColumnText16(stmt, index);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUni(ptr);
        }

        public long ColumnInt64(int index)
        {
            return SQLiteNative.ColumnInt64(stmt, index);
        }

        private int ParameterIndex(string name)
        {
            int index = SQLiteNative.BindParameterIndex(stmt, name);
            if (index == 0)
            {
                throw new InvalidOperationException("SQLite 参数不存在：" + name);
            }
            return index;
        }

        private void Check(int rc)
        {
            if (rc != SQLiteNative.Ok)
            {
                throw new InvalidOperationException(database.ErrorMessage());
            }
        }
    }

    internal static class LauncherLog
    {
        private const long MaxLogBytes = 1024 * 1024;
        private static readonly object SyncRoot = new object();

        public static string DirectoryPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CodexSiteLauncher");
            }
        }

        public static string LogPath
        {
            get { return Path.Combine(DirectoryPath, "launcher.log"); }
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    RotateIfNeeded();

                    var builder = new StringBuilder();
                    builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    builder.Append(" [");
                    builder.Append(level);
                    builder.Append("] ");
                    builder.AppendLine(message ?? "");
                    if (ex != null)
                    {
                        builder.AppendLine(ex.ToString());
                    }

                    File.AppendAllText(LogPath, builder.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static void RotateIfNeeded()
        {
            if (!File.Exists(LogPath))
            {
                return;
            }

            var info = new FileInfo(LogPath);
            if (info.Length < MaxLogBytes)
            {
                return;
            }

            string oldPath = Path.Combine(DirectoryPath, "launcher.old.log");
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
            File.Move(LogPath, oldPath);
        }
    }

    internal static class LauncherIcon
    {
        public static void Apply(Form form)
        {
            if (form == null)
            {
                return;
            }

            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    form.Icon = icon;
                }
            }
            catch
            {
            }
        }
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
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

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
            LauncherIcon.Apply(this);
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
            LauncherLog.Info("Launcher started. Config store: " + configPath);

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
                catch (Exception ex)
                {
                    LauncherLog.Error("Failed to load launcher config, defaults will be used. Path: " + configPath, ex);
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
            LauncherLog.Info("Launcher config saved. Sites: " + (config.Sites == null ? 0 : config.Sites.Count));
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
            setupLayout.RowCount = 4;
            setupLayout.Padding = new Padding(12);
            setupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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

            var openLogButton = new Button { Text = "打开日志", Width = 110, Height = 36 };
            openLogButton.Click += delegate { OpenLogFile(); };
            cleanLaunchPanel.Controls.Add(openLogButton);

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

            var historyGroup = new ThemedGroupBox();
            historyGroup.Text = "历史同步";
            historyGroup.Dock = DockStyle.Top;
            historyGroup.Height = 118;
            historyGroup.Margin = new Padding(0, 12, 0, 0);
            setupLayout.Controls.Add(historyGroup, 0, 3);

            var historyPanel = new FlowLayoutPanel();
            historyPanel.Dock = DockStyle.Fill;
            historyPanel.AutoScroll = true;
            historyPanel.WrapContents = true;
            historyPanel.Padding = new Padding(8);
            historyGroup.Controls.Add(historyPanel);

            var syncNewApiToOpenAiButton = new Button { Text = "复制 newapi 到 openai", Width = 190, Height = 36 };
            syncNewApiToOpenAiButton.Click += delegate { SyncHistoryProvider("newapi", "openai"); };
            historyPanel.Controls.Add(syncNewApiToOpenAiButton);

            var syncOpenAiToNewApiButton = new Button { Text = "复制 openai 到 newapi", Width = 190, Height = 36 };
            syncOpenAiToNewApiButton.Click += delegate { SyncHistoryProvider("openai", "newapi"); };
            historyPanel.Controls.Add(syncOpenAiToNewApiButton);

            var estimateHistoryButton = new Button { Text = "预估占用", Width = 110, Height = 36 };
            estimateHistoryButton.Click += delegate { ShowHistorySyncEstimate(); };
            historyPanel.Controls.Add(estimateHistoryButton);

            var historyHint = new Label();
            historyHint.Text = "复制会话副本并改 provider；原记录不动。同步前请完全退出 Codex。";
            historyHint.AutoSize = true;
            historyHint.Anchor = AnchorStyles.Left;
            historyHint.Margin = new Padding(8, 8, 0, 0);
            historyHint.ForeColor = AppTheme.MutedText;
            historyPanel.Controls.Add(historyHint);

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
            historyHint.ForeColor = AppTheme.MutedText;
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
            LauncherLog.Info("API key saved to user environment variable. Site: " + site.Name + ", env_key: " + site.EnvKey);
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
            LauncherLog.Info("API key reloaded from environment variable: " + envKeyBox.Text.Trim());
        }

        private void LaunchSelectedSite()
        {
            LaunchSite(GetSelectedSite());
        }

        private void LaunchCleanCodex()
        {
            config.CodexPath = codexPathBox.Text.Trim();
            SaveConfig();
            LauncherLog.Info("Clean launch requested. Path: " + config.CodexPath);

            try
            {
                LaunchCodexDesktop("");
                statusLabel.Text = "已执行纯净启动。";
                LauncherLog.Info("Clean launch completed.");
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Clean launch failed.", ex);
                MessageBox.Show(this, ex.Message, "纯净启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenLogFile()
        {
            try
            {
                Directory.CreateDirectory(LauncherLog.DirectoryPath);
                if (!File.Exists(LauncherLog.LogPath))
                {
                    File.WriteAllText(LauncherLog.LogPath, "", Encoding.UTF8);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = LauncherLog.LogPath,
                    UseShellExecute = true
                });
                statusLabel.Text = "已打开日志：" + LauncherLog.LogPath;
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Failed to open log file.", ex);
                MessageBox.Show(this, ex.Message, "打开日志失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncHistoryProvider(string sourceProvider, string targetProvider)
        {
            sourceProvider = NormalizeProviderName(sourceProvider);
            targetProvider = NormalizeProviderName(targetProvider);

            if (String.Equals(sourceProvider, targetProvider, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                if (!ConfirmHistorySyncWhenCodexRunning())
                {
                    statusLabel.Text = "已取消历史同步。";
                    return;
                }

                HistorySyncEstimate estimate = EstimateHistorySync(sourceProvider, targetProvider);
                if (estimate.CopyCount <= 0 && estimate.ExistingCopies <= 0)
                {
                    statusLabel.Text = "没有需要复制的 " + sourceProvider + " 历史记录。";
                    MessageBox.Show(
                        this,
                        "没有需要复制的记录。" + Environment.NewLine +
                        "来源：" + sourceProvider + Environment.NewLine +
                        "目标：" + targetProvider + Environment.NewLine +
                        "已存在副本：" + estimate.ExistingCopies,
                        "历史同步",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string confirmText = estimate.CopyCount > 0
                    ? "将复制 " + estimate.CopyCount + " 条历史记录：" + Environment.NewLine +
                        sourceProvider + " -> " + targetProvider + Environment.NewLine + Environment.NewLine +
                        "预计新增空间：" + FormatBytes(estimate.CopyBytes) + Environment.NewLine +
                        "已存在副本：" + estimate.ExistingCopies + Environment.NewLine +
                        "缺失会话文件：" + estimate.MissingFiles + Environment.NewLine + Environment.NewLine +
                        "会先备份 state_*.sqlite 和 session_index.jsonl，并同步完整索引元数据。同步前建议完全退出 Codex。"
                    : "没有新的历史记录需要复制。" + Environment.NewLine +
                        "可以刷新 " + estimate.ExistingCopies + " 条已有副本的完整索引元数据，让排序、标题和归档状态与来源一致。" +
                        Environment.NewLine + Environment.NewLine +
                        "会先备份 state_*.sqlite 和 session_index.jsonl。同步前建议完全退出 Codex。";

                DialogResult result = MessageBox.Show(
                    this,
                    confirmText,
                    "确认历史同步",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    statusLabel.Text = "已取消历史同步。";
                    return;
                }

                HistorySyncResult sync = CopyHistoryProvider(sourceProvider, targetProvider);
                statusLabel.Text = "历史同步完成：" + sourceProvider + " -> " + targetProvider +
                    "，复制 " + sync.CopiedCount + " 条，刷新索引 " + sync.SessionIndexSyncedCount +
                    " 条，新增约 " + FormatBytes(sync.CopyBytes) + "。";
                MessageBox.Show(
                    this,
                    "历史同步完成。" + Environment.NewLine +
                    "复制记录：" + sync.CopiedCount + Environment.NewLine +
                    "刷新索引：" + sync.SessionIndexSyncedCount + Environment.NewLine +
                    "新增空间：" + FormatBytes(sync.CopyBytes) + Environment.NewLine +
                    "SQLite 备份：" + sync.StateBackupPath,
                    "历史同步",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LauncherLog.Error("History sync failed. " + sourceProvider + " -> " + targetProvider, ex);
                MessageBox.Show(this, ex.Message, "历史同步失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowHistorySyncEstimate()
        {
            try
            {
                HistorySyncEstimate openAiToNewApi = EstimateHistorySync("openai", "newapi");
                HistorySyncEstimate newApiToOpenAi = EstimateHistorySync("newapi", "openai");
                string message =
                    BuildEstimateLine(openAiToNewApi) + Environment.NewLine +
                    BuildEstimateLine(newApiToOpenAi) + Environment.NewLine + Environment.NewLine +
                    "同步只会复制副本，原记录不动。执行同步前请完全退出 Codex。";
                MessageBox.Show(this, message, "历史同步预估", MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = "历史同步预估完成。";
            }
            catch (Exception ex)
            {
                LauncherLog.Error("History sync estimate failed.", ex);
                MessageBox.Show(this, ex.Message, "历史同步预估失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string BuildEstimateLine(HistorySyncEstimate estimate)
        {
            return estimate.SourceProvider + " -> " + estimate.TargetProvider +
                "：来源 " + estimate.SourceCount +
                " 条，待复制 " + estimate.CopyCount +
                " 条，已存在副本 " + estimate.ExistingCopies +
                " 条，预计新增 " + FormatBytes(estimate.CopyBytes) +
                "，缺失文件 " + estimate.MissingFiles + " 个";
        }

        private bool ConfirmHistorySyncWhenCodexRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("Codex");
                if (processes.Length == 0)
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }

            DialogResult result = MessageBox.Show(
                this,
                "检测到 Codex 可能正在运行。历史同步会写入 state_*.sqlite 和会话索引，建议先完全退出 Codex。" +
                Environment.NewLine + Environment.NewLine +
                "仍要继续吗？",
                "Codex 正在运行",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }

        private HistorySyncEstimate EstimateHistorySync(string sourceProvider, string targetProvider)
        {
            string statePath = FindCurrentStateDatabase();
            if (String.IsNullOrWhiteSpace(statePath))
            {
                throw new FileNotFoundException("找不到 Codex state_*.sqlite。");
            }

            HistorySyncStore store = LoadHistorySyncStore();
            var duplicateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HistorySyncEntry entry in store.Entries)
            {
                if (!String.IsNullOrWhiteSpace(entry.DuplicateId))
                {
                    duplicateIds.Add(entry.DuplicateId);
                }
            }

            var estimate = new HistorySyncEstimate
            {
                SourceProvider = sourceProvider,
                TargetProvider = targetProvider,
                StateDbPath = statePath
            };

            using (var db = new SQLiteDatabase(statePath))
            {
                List<HistoryThreadRow> rows = QueryProviderThreads(db, sourceProvider);
                estimate.SourceCount = rows.Count;

                foreach (HistoryThreadRow row in rows)
                {
                    if (duplicateIds.Contains(row.Id))
                    {
                        continue;
                    }

                    HistorySyncEntry existing = FindHistorySyncEntry(store, row.Id, targetProvider);
                    if (existing != null && ThreadExists(db, existing.DuplicateId))
                    {
                        estimate.ExistingCopies++;
                        continue;
                    }

                    string sourcePath = NormalizeFileSystemPath(row.RolloutPath);
                    if (!File.Exists(sourcePath))
                    {
                        estimate.MissingFiles++;
                        continue;
                    }

                    estimate.CopyCount++;
                    estimate.CopyBytes += new FileInfo(sourcePath).Length;
                }
            }

            return estimate;
        }

        private HistorySyncResult CopyHistoryProvider(string sourceProvider, string targetProvider)
        {
            HistorySyncEstimate estimate = EstimateHistorySync(sourceProvider, targetProvider);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string sessionIndexPath = Path.Combine(GetCodexHomeDirectory(), "session_index.jsonl");
            string globalStatePath = Path.Combine(GetCodexHomeDirectory(), ".codex-global-state.json");

            var result = new HistorySyncResult
            {
                SourceProvider = sourceProvider,
                TargetProvider = targetProvider,
                StateDbPath = estimate.StateDbPath,
                SourceCount = estimate.SourceCount,
                ExistingCopies = estimate.ExistingCopies,
                CopyCount = estimate.CopyCount,
                MissingFiles = estimate.MissingFiles,
                CopyBytes = estimate.CopyBytes,
                StateBackupPath = BackupExistingFile(estimate.StateDbPath, "before-history-sync-" + stamp),
                StateWalBackupPath = BackupExistingFile(estimate.StateDbPath + "-wal", "before-history-sync-" + stamp),
                SessionIndexBackupPath = BackupExistingFile(sessionIndexPath, "before-history-sync-" + stamp),
                GlobalStateBackupPath = BackupExistingFile(globalStatePath, "before-history-sync-" + stamp)
            };

            HistorySyncStore store = LoadHistorySyncStore();
            var duplicateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HistorySyncEntry entry in store.Entries)
            {
                if (!String.IsNullOrWhiteSpace(entry.DuplicateId))
                {
                    duplicateIds.Add(entry.DuplicateId);
                }
            }

            var copiedFiles = new List<string>();
            var createdEntries = new List<HistorySyncEntry>();

            using (var db = new SQLiteDatabase(estimate.StateDbPath))
            {
                List<HistoryThreadRow> rows = QueryProviderThreads(db, sourceProvider);
                List<string> threadColumns = GetTableColumns(db, "threads");
                List<string> childTables = GetThreadChildTables(db);

                db.Execute("BEGIN IMMEDIATE TRANSACTION");
                try
                {
                    foreach (HistoryThreadRow row in rows)
                    {
                        if (duplicateIds.Contains(row.Id))
                        {
                            continue;
                        }

                        HistorySyncEntry existing = FindHistorySyncEntry(store, row.Id, targetProvider);
                        if (existing != null && ThreadExists(db, existing.DuplicateId))
                        {
                            continue;
                        }

                        string sourcePath = NormalizeFileSystemPath(row.RolloutPath);
                        if (!File.Exists(sourcePath))
                        {
                            continue;
                        }

                        string newId = GenerateThreadId();
                        string destinationPath = BuildDuplicateRolloutPath(sourcePath, row.Id, newId);
                        string dbPath = BuildDatabasePathLike(row.RolloutPath, destinationPath);

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        CopyRolloutFile(sourcePath, destinationPath, row.Id, newId, sourceProvider, targetProvider);
                        copiedFiles.Add(destinationPath);

                        InsertThreadCopy(db, threadColumns, row.Id, newId, dbPath, targetProvider);
                        foreach (string childTable in childTables)
                        {
                            InsertThreadChildCopies(db, childTable, row.Id, newId);
                        }

                        var entry = new HistorySyncEntry
                        {
                            OriginalId = row.Id,
                            DuplicateId = newId,
                            SourceProvider = sourceProvider,
                            TargetProvider = targetProvider,
                            DuplicatePath = dbPath,
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        store.Entries.Add(entry);
                        createdEntries.Add(entry);

                        result.CopiedCount++;
                    }

                    db.Execute("COMMIT");
                }
                catch
                {
                    try { db.Execute("ROLLBACK"); } catch { }
                    foreach (string copiedFile in copiedFiles)
                    {
                        try
                        {
                            if (File.Exists(copiedFile))
                            {
                                File.Delete(copiedFile);
                            }
                        }
                        catch
                        {
                        }
                    }
                    throw;
                }
            }

            SaveHistorySyncStore(store);
            result.SessionIndexSyncedCount = RewriteSessionIndexForHistorySync(sessionIndexPath, store);
            UpdateGlobalStateForHistoryCopies(createdEntries);
            LauncherLog.Info("History sync completed. " + sourceProvider + " -> " + targetProvider + ", copied: " + result.CopiedCount);
            return result;
        }

        private HistorySyncStore LoadHistorySyncStore()
        {
            string path = Path.Combine(configDir, "history-sync.json");
            if (File.Exists(path))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(path))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(HistorySyncStore));
                        var store = (HistorySyncStore)serializer.ReadObject(stream);
                        if (store != null && store.Entries != null)
                        {
                            return store;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LauncherLog.Error("Failed to load history sync map.", ex);
                }
            }

            return new HistorySyncStore { Entries = new List<HistorySyncEntry>() };
        }

        private void SaveHistorySyncStore(HistorySyncStore store)
        {
            Directory.CreateDirectory(configDir);
            string path = Path.Combine(configDir, "history-sync.json");
            using (FileStream stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(HistorySyncStore));
                serializer.WriteObject(stream, store);
            }
        }

        private static HistorySyncEntry FindHistorySyncEntry(HistorySyncStore store, string originalId, string targetProvider)
        {
            foreach (HistorySyncEntry entry in store.Entries)
            {
                if (String.Equals(entry.OriginalId, originalId, StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(entry.TargetProvider, targetProvider, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
            return null;
        }

        private static List<HistoryThreadRow> QueryProviderThreads(SQLiteDatabase db, string sourceProvider)
        {
            var rows = new List<HistoryThreadRow>();
            using (SQLiteStatement stmt = db.Prepare(
                "SELECT id, rollout_path, title, updated_at, archived FROM threads WHERE model_provider = @provider ORDER BY updated_at DESC"))
            {
                stmt.BindText("@provider", sourceProvider);
                while (true)
                {
                    int rc = stmt.Step();
                    if (rc == SQLiteNative.Done)
                    {
                        break;
                    }
                    if (rc != SQLiteNative.Row)
                    {
                        throw new InvalidOperationException(db.ErrorMessage());
                    }

                    rows.Add(new HistoryThreadRow
                    {
                        Id = stmt.ColumnText(0),
                        RolloutPath = stmt.ColumnText(1),
                        Title = stmt.ColumnText(2),
                        UpdatedAt = stmt.ColumnInt64(3),
                        Archived = (int)stmt.ColumnInt64(4)
                    });
                }
            }
            return rows;
        }

        private static bool ThreadExists(SQLiteDatabase db, string threadId)
        {
            if (String.IsNullOrWhiteSpace(threadId))
            {
                return false;
            }

            using (SQLiteStatement stmt = db.Prepare("SELECT 1 FROM threads WHERE id = @id LIMIT 1"))
            {
                stmt.BindText("@id", threadId);
                return stmt.Step() == SQLiteNative.Row;
            }
        }

        private static List<string> GetThreadChildTables(SQLiteDatabase db)
        {
            var tables = new List<string>();
            string[] candidates = { "thread_dynamic_tools", "stage1_outputs", "thread_goals" };
            foreach (string candidate in candidates)
            {
                if (TableExists(db, candidate))
                {
                    tables.Add(candidate);
                }
            }
            return tables;
        }

        private static bool TableExists(SQLiteDatabase db, string table)
        {
            using (SQLiteStatement stmt = db.Prepare("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1"))
            {
                stmt.BindText("@name", table);
                return stmt.Step() == SQLiteNative.Row;
            }
        }

        private static List<string> GetTableColumns(SQLiteDatabase db, string table)
        {
            var columns = new List<string>();
            using (SQLiteStatement stmt = db.Prepare("PRAGMA table_info(" + QuoteIdentifier(table) + ")"))
            {
                while (true)
                {
                    int rc = stmt.Step();
                    if (rc == SQLiteNative.Done)
                    {
                        break;
                    }
                    if (rc != SQLiteNative.Row)
                    {
                        throw new InvalidOperationException(db.ErrorMessage());
                    }
                    columns.Add(stmt.ColumnText(1));
                }
            }
            return columns;
        }

        private static void InsertThreadCopy(SQLiteDatabase db, List<string> columns, string sourceId, string newId, string rolloutPath, string targetProvider)
        {
            var selectColumns = new List<string>();
            foreach (string column in columns)
            {
                if (String.Equals(column, "id", StringComparison.OrdinalIgnoreCase))
                {
                    selectColumns.Add("@newId");
                }
                else if (String.Equals(column, "rollout_path", StringComparison.OrdinalIgnoreCase))
                {
                    selectColumns.Add("@rolloutPath");
                }
                else if (String.Equals(column, "model_provider", StringComparison.OrdinalIgnoreCase))
                {
                    selectColumns.Add("@targetProvider");
                }
                else
                {
                    selectColumns.Add(QuoteIdentifier(column));
                }
            }

            string sql = "INSERT INTO " + QuoteIdentifier("threads") +
                " (" + String.Join(", ", columns.Select(QuoteIdentifier).ToArray()) + ") " +
                "SELECT " + String.Join(", ", selectColumns.ToArray()) +
                " FROM " + QuoteIdentifier("threads") + " WHERE id = @sourceId";

            using (SQLiteStatement stmt = db.Prepare(sql))
            {
                stmt.BindText("@newId", newId);
                stmt.BindText("@rolloutPath", rolloutPath);
                stmt.BindText("@targetProvider", targetProvider);
                stmt.BindText("@sourceId", sourceId);
                stmt.Execute();
            }
        }

        private static void InsertThreadChildCopies(SQLiteDatabase db, string table, string sourceId, string newId)
        {
            List<string> columns = GetTableColumns(db, table);
            if (!columns.Any(c => String.Equals(c, "thread_id", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var selectColumns = new List<string>();
            foreach (string column in columns)
            {
                selectColumns.Add(String.Equals(column, "thread_id", StringComparison.OrdinalIgnoreCase)
                    ? "@newId"
                    : QuoteIdentifier(column));
            }

            string sql = "INSERT OR IGNORE INTO " + QuoteIdentifier(table) +
                " (" + String.Join(", ", columns.Select(QuoteIdentifier).ToArray()) + ") " +
                "SELECT " + String.Join(", ", selectColumns.ToArray()) +
                " FROM " + QuoteIdentifier(table) + " WHERE thread_id = @sourceId";

            using (SQLiteStatement stmt = db.Prepare(sql))
            {
                stmt.BindText("@newId", newId);
                stmt.BindText("@sourceId", sourceId);
                stmt.Execute();
            }
        }

        private static string FindCurrentStateDatabase()
        {
            string codexHome = GetCodexHomeDirectory();
            if (!Directory.Exists(codexHome))
            {
                return "";
            }

            FileInfo newest = null;
            foreach (string file in Directory.GetFiles(codexHome, "state_*.sqlite"))
            {
                var info = new FileInfo(file);
                if (newest == null || info.LastWriteTimeUtc > newest.LastWriteTimeUtc)
                {
                    newest = info;
                }
            }
            return newest == null ? "" : newest.FullName;
        }

        private static string GetCodexHomeDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        }

        private static string BackupExistingFile(string path, string suffix)
        {
            if (String.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return "";
            }

            string backup = path + "." + suffix + ".bak";
            File.Copy(path, backup, true);
            return backup;
        }

        private static string BuildDuplicateRolloutPath(string sourcePath, string sourceId, string newId)
        {
            string directory = Path.GetDirectoryName(sourcePath);
            string fileName = Path.GetFileName(sourcePath);
            string newFileName = fileName.IndexOf(sourceId, StringComparison.OrdinalIgnoreCase) >= 0
                ? Regex.Replace(fileName, Regex.Escape(sourceId), newId, RegexOptions.IgnoreCase)
                : Path.GetFileNameWithoutExtension(fileName) + "-copy-" + newId + Path.GetExtension(fileName);
            return Path.Combine(directory, newFileName);
        }

        private static string BuildDatabasePathLike(string originalDbPath, string destinationPath)
        {
            if (!String.IsNullOrWhiteSpace(originalDbPath) &&
                originalDbPath.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                return @"\\?\" + Path.GetFullPath(destinationPath);
            }

            return Path.GetFullPath(destinationPath);
        }

        private static string GenerateThreadId()
        {
            return Guid.NewGuid().ToString();
        }

        private static string NormalizeFileSystemPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return @"\\" + path.Substring(8);
            }

            if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(4);
            }

            return path;
        }

        private static void CopyRolloutFile(string sourcePath, string destinationPath, string sourceId, string newId, string sourceProvider, string targetProvider)
        {
            string text = File.ReadAllText(sourcePath, Encoding.UTF8);
            text = text.Replace(sourceId, newId);

            string pattern = "(\"model_provider\"\\s*:\\s*\")" + Regex.Escape(sourceProvider) + "(\")";
            string replacement = "$1" + targetProvider.Replace("$", "$$") + "$2";
            text = Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);

            File.WriteAllText(destinationPath, text, Utf8NoBom);
        }

        private static void AppendSessionIndexLines(string sessionIndexPath, List<string> indexLines)
        {
            if (indexLines == null || indexLines.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(sessionIndexPath));
            File.AppendAllText(sessionIndexPath, String.Join(Environment.NewLine, indexLines.ToArray()) + Environment.NewLine, Utf8NoBom);
        }

        private static int RewriteSessionIndexForHistorySync(string sessionIndexPath, HistorySyncStore store)
        {
            if (String.IsNullOrWhiteSpace(sessionIndexPath) || !File.Exists(sessionIndexPath) ||
                store == null || store.Entries == null || store.Entries.Count == 0)
            {
                return 0;
            }

            List<SessionIndexRecord> records = LoadSessionIndexRecords(sessionIndexPath);
            if (records.Count == 0)
            {
                return 0;
            }

            var byId = new Dictionary<string, SessionIndexRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (SessionIndexRecord record in records)
            {
                if (!String.IsNullOrWhiteSpace(record.Id) && record.Data != null && !byId.ContainsKey(record.Id))
                {
                    byId.Add(record.Id, record);
                }
            }

            var duplicateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entriesByOriginal = new Dictionary<string, List<HistorySyncEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (HistorySyncEntry entry in store.Entries)
            {
                if (String.IsNullOrWhiteSpace(entry.OriginalId) ||
                    String.IsNullOrWhiteSpace(entry.DuplicateId) ||
                    String.IsNullOrWhiteSpace(entry.TargetProvider))
                {
                    continue;
                }

                duplicateIds.Add(entry.DuplicateId);
                List<HistorySyncEntry> list;
                if (!entriesByOriginal.TryGetValue(entry.OriginalId, out list))
                {
                    list = new List<HistorySyncEntry>();
                    entriesByOriginal.Add(entry.OriginalId, list);
                }
                list.Add(entry);
            }

            JavaScriptSerializer serializer = CreateJsonSerializer();
            var output = new List<string>();
            var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int synced = 0;

            foreach (SessionIndexRecord record in records)
            {
                if (!String.IsNullOrWhiteSpace(record.Id) && duplicateIds.Contains(record.Id))
                {
                    continue;
                }

                output.Add(SerializeSessionIndexRecord(serializer, record));
                if (String.IsNullOrWhiteSpace(record.Id))
                {
                    continue;
                }

                List<HistorySyncEntry> entries;
                if (!entriesByOriginal.TryGetValue(record.Id, out entries))
                {
                    continue;
                }

                foreach (HistorySyncEntry entry in entries)
                {
                    if (written.Contains(entry.DuplicateId) || !byId.ContainsKey(entry.OriginalId))
                    {
                        continue;
                    }

                    Dictionary<string, object> copy = BuildSessionIndexCopy(
                        byId[entry.OriginalId].Data,
                        entry.DuplicateId,
                        entry.TargetProvider,
                        entry.DuplicatePath);
                    output.Add(serializer.Serialize(copy));
                    written.Add(entry.DuplicateId);
                    synced++;
                }
            }

            foreach (HistorySyncEntry entry in store.Entries)
            {
                if (String.IsNullOrWhiteSpace(entry.OriginalId) ||
                    String.IsNullOrWhiteSpace(entry.DuplicateId) ||
                    written.Contains(entry.DuplicateId) ||
                    !byId.ContainsKey(entry.OriginalId))
                {
                    continue;
                }

                Dictionary<string, object> copy = BuildSessionIndexCopy(
                    byId[entry.OriginalId].Data,
                    entry.DuplicateId,
                    entry.TargetProvider,
                    entry.DuplicatePath);
                output.Add(serializer.Serialize(copy));
                written.Add(entry.DuplicateId);
                synced++;
            }

            File.WriteAllText(sessionIndexPath, String.Join(Environment.NewLine, output.ToArray()) + Environment.NewLine, Utf8NoBom);
            return synced;
        }

        private static List<SessionIndexRecord> LoadSessionIndexRecords(string sessionIndexPath)
        {
            var records = new List<SessionIndexRecord>();
            JavaScriptSerializer serializer = CreateJsonSerializer();
            foreach (string line in File.ReadAllLines(sessionIndexPath, Utf8NoBom))
            {
                if (String.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var data = serializer.DeserializeObject(line) as Dictionary<string, object>;
                    string id = null;
                    object idValue;
                    if (data != null && data.TryGetValue("id", out idValue))
                    {
                        id = Convert.ToString(idValue);
                    }

                    records.Add(new SessionIndexRecord
                    {
                        Id = id,
                        Data = data,
                        RawLine = line
                    });
                }
                catch
                {
                    records.Add(new SessionIndexRecord
                    {
                        RawLine = line
                    });
                }
            }
            return records;
        }

        private static string SerializeSessionIndexRecord(JavaScriptSerializer serializer, SessionIndexRecord record)
        {
            if (record != null && record.Data != null)
            {
                return serializer.Serialize(record.Data);
            }
            return record == null ? "" : (record.RawLine ?? "");
        }

        private static Dictionary<string, object> BuildSessionIndexCopy(
            Dictionary<string, object> source,
            string duplicateId,
            string targetProvider,
            string duplicatePath)
        {
            Dictionary<string, object> copy = DeepCloneJsonValue(source) as Dictionary<string, object>;
            if (copy == null)
            {
                copy = new Dictionary<string, object>();
            }

            copy["id"] = duplicateId;
            copy["model_provider"] = targetProvider;
            if (!String.IsNullOrWhiteSpace(duplicatePath))
            {
                copy["rollout_path"] = duplicatePath;
            }
            return copy;
        }

        private static string BuildSessionIndexLine(string id, string title, long updatedAt)
        {
            DateTime utc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(updatedAt);
            return "{\"id\":\"" + JsonEscape(id) + "\",\"thread_name\":\"" + JsonEscape(title ?? "") +
                "\",\"updated_at\":\"" + utc.ToString("yyyy-MM-ddTHH:mm:ss.fff000Z") + "\"}";
        }

        private void UpdateGlobalStateForHistoryCopies(List<HistorySyncEntry> createdEntries)
        {
            if (createdEntries == null || createdEntries.Count == 0)
            {
                return;
            }

            string path = Path.Combine(GetCodexHomeDirectory(), ".codex-global-state.json");
            if (!File.Exists(path))
            {
                LauncherLog.Info("Global state not found; skipped sidebar/history cache update.");
                return;
            }

            string text = File.ReadAllText(path, Utf8NoBom);
            JavaScriptSerializer serializer = CreateJsonSerializer();
            object root = serializer.DeserializeObject(text);
            var dict = root as Dictionary<string, object>;
            if (dict == null)
            {
                return;
            }

            Dictionary<string, object> atom = GetJsonObject(dict, "electron-persisted-atom-state");
            IList projectless = GetMutableJsonArray(dict, "projectless-thread-ids");
            Dictionary<string, object> workspaceHints = GetJsonObject(dict, "thread-workspace-root-hints");
            Dictionary<string, object> promptHistory = atom == null ? null : GetJsonObject(atom, "prompt-history");
            Dictionary<string, object> heartbeatPermissions = atom == null ? null : GetJsonObject(atom, "heartbeat-thread-permissions-by-id");

            foreach (HistorySyncEntry entry in createdEntries)
            {
                if (projectless != null && !JsonArrayContainsString(projectless, entry.DuplicateId))
                {
                    projectless.Add(entry.DuplicateId);
                }

                if (workspaceHints != null && workspaceHints.ContainsKey(entry.OriginalId))
                {
                    workspaceHints[entry.DuplicateId] = DeepCloneJsonValue(workspaceHints[entry.OriginalId]);
                }

                if (promptHistory != null && promptHistory.ContainsKey(entry.OriginalId))
                {
                    promptHistory[entry.DuplicateId] = DeepCloneJsonValue(promptHistory[entry.OriginalId]);
                }

                if (heartbeatPermissions != null && heartbeatPermissions.ContainsKey(entry.OriginalId))
                {
                    heartbeatPermissions[entry.DuplicateId] = DeepCloneJsonValue(heartbeatPermissions[entry.OriginalId]);
                }
            }

            File.WriteAllText(path, serializer.Serialize(dict), Utf8NoBom);
            LauncherLog.Info("Global state updated for copied history threads: " + createdEntries.Count);
        }

        private static object DeepCloneJsonValue(object value)
        {
            JavaScriptSerializer serializer = CreateJsonSerializer();
            string json = serializer.Serialize(value);
            return serializer.DeserializeObject(json);
        }

        private static JavaScriptSerializer CreateJsonSerializer()
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength = Int32.MaxValue,
                RecursionLimit = 100
            };
        }

        private static Dictionary<string, object> GetJsonObject(Dictionary<string, object> dict, string key)
        {
            if (dict == null)
            {
                return null;
            }

            object value;
            if (!dict.TryGetValue(key, out value))
            {
                return null;
            }
            return value as Dictionary<string, object>;
        }

        private static IList GetMutableJsonArray(Dictionary<string, object> dict, string key)
        {
            if (dict == null)
            {
                return null;
            }

            object value;
            if (!dict.TryGetValue(key, out value))
            {
                return null;
            }

            var list = value as IList;
            if (list == null)
            {
                return null;
            }

            if (list.IsFixedSize || list.IsReadOnly)
            {
                var copy = new ArrayList();
                foreach (object item in list)
                {
                    copy.Add(item);
                }
                dict[key] = copy;
                return copy;
            }

            return list;
        }

        private static bool JsonArrayContainsString(IList list, string value)
        {
            foreach (object item in list)
            {
                if (String.Equals(Convert.ToString(item), value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string JsonEscape(string value)
        {
            if (value == null)
            {
                return "";
            }

            var builder = new StringBuilder();
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        builder.Append(@"\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append(@"\r");
                        break;
                    case '\n':
                        builder.Append(@"\n");
                        break;
                    case '\t':
                        builder.Append(@"\t");
                        break;
                    default:
                        if (c < 32)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            return builder.ToString();
        }

        private static string QuoteIdentifier(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string NormalizeProviderName(string provider)
        {
            return (provider ?? "").Trim().ToLowerInvariant();
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value.ToString(unit == 0 ? "0" : "0.##") + " " + units[unit];
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

            if (!ConfirmIfCodexRunning(usePackagedActivation ? "Codex.exe" : config.CodexPath))
            {
                statusLabel.Text = "已取消启动。";
                LauncherLog.Info("Launch cancelled by user. Site: " + site.Name);
                return;
            }

            ConfigSwitchResult configSwitch = null;
            try
            {
                LauncherLog.Info("Site launch requested. Site: " + site.Name + ", base_url: " + site.BaseUrl + ", env_key: " + site.EnvKey + ", packaged: " + usePackagedActivation);
                ApplySiteEnvironment(site, key);
                configSwitch = UpdateCodexConfigForSite(site);
                SaveConfig();

                LaunchCodexDesktop(usePackagedActivation ? "" : BuildCodexArguments(site));
                ScheduleConfigRestore(configSwitch, site.Name);
                statusLabel.Text = "已临时切换配置并启动 " + site.Name + "，15 秒后恢复 config.toml。";
                LauncherLog.Info("Codex launch completed, config restore scheduled. Site: " + site.Name);
            }
            catch (Exception ex)
            {
                if (configSwitch != null)
                {
                    RestoreConfig(configSwitch);
                }
                LauncherLog.Error("Site launch failed. Site: " + site.Name, ex);
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
            LauncherLog.Info("Environment variables applied. Site: " + site.Name + ", env_key: " + site.EnvKey + ", base_url: " + site.BaseUrl);
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
            LauncherLog.Info("config.toml temporarily switched. Path: " + configPath + ", site: " + site.Name + ", backup: " + latestBackup);
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

                ConfigRestoreResult restored = RestoreConfig(configSwitch);
                if (restored.Success && restored.PreservedExternalChanges)
                {
                    statusLabel.Text = "已恢复启动器配置，并保留 Codex 写入的其它变更：" + siteName;
                }
                else if (restored.Success)
                {
                    statusLabel.Text = "已恢复启动前配置：" + siteName;
                }
                else
                {
                    statusLabel.Text = "未恢复配置：" + restored.Error + "。请查看日志和备份。";
                }
            };
            restoreTimers.Add(timer);
            timer.Start();
        }

        private static ConfigRestoreResult RestoreConfig(ConfigSwitchResult configSwitch)
        {
            if (configSwitch == null ||
                String.IsNullOrWhiteSpace(configSwitch.ConfigPath) ||
                !File.Exists(configSwitch.ConfigPath))
            {
                string error = "config.toml 不存在或恢复信息缺失";
                LauncherLog.Info("Config restore skipped: " + error);
                return new ConfigRestoreResult { Success = false, Error = error };
            }

            try
            {
                string current = File.ReadAllText(configSwitch.ConfigPath, Encoding.UTF8);
                if (String.Equals(current, configSwitch.AppliedText, StringComparison.Ordinal))
                {
                    File.WriteAllText(configSwitch.ConfigPath, configSwitch.PreviousText ?? "", Encoding.UTF8);
                    LauncherLog.Info("config.toml restored exactly to pre-launch content. Path: " + configSwitch.ConfigPath);
                    return new ConfigRestoreResult { Success = true };
                }

                string merged = RestoreLauncherChanges(current, configSwitch.PreviousText ?? "");
                File.WriteAllText(configSwitch.ConfigPath, merged, Encoding.UTF8);
                LauncherLog.Info("config.toml changed after launch; restored launcher-owned settings and preserved other changes. Path: " + configSwitch.ConfigPath);
                return new ConfigRestoreResult { Success = true, PreservedExternalChanges = true };
            }
            catch (Exception ex)
            {
                LauncherLog.Error("Config restore failed. Path: " + configSwitch.ConfigPath, ex);
                return new ConfigRestoreResult { Success = false, Error = ex.Message };
            }
        }

        private static string RestoreLauncherChanges(string current, string previous)
        {
            string restored = current;
            restored = RestoreTopLevelSetting(restored, previous, "model_provider");
            restored = RestoreTomlBlock(restored, previous, "model_providers.newapi");
            restored = RestoreTomlBlock(restored, previous, "windows");
            return NormalizeBlankLines(restored);
        }

        private static string RestoreTopLevelSetting(string current, string previous, string key)
        {
            string previousLine;
            bool hadPrevious = TryGetTopLevelSetting(previous, key, out previousLine);
            var pattern = new Regex(@"(?m)^" + Regex.Escape(key) + @"\s*=\s*""[^""]*""\s*\r?$");

            if (hadPrevious)
            {
                if (pattern.IsMatch(current))
                {
                    return pattern.Replace(current, previousLine, 1);
                }
                return previousLine + Environment.NewLine + current.TrimStart();
            }

            return pattern.Replace(current, "", 1);
        }

        private static bool TryGetTopLevelSetting(string text, string key, out string line)
        {
            line = "";
            var pattern = new Regex(@"(?m)^" + Regex.Escape(key) + @"\s*=\s*""[^""]*""\s*\r?$");
            Match match = pattern.Match(text ?? "");
            if (!match.Success)
            {
                return false;
            }

            line = match.Value.TrimEnd('\r');
            return true;
        }

        private static string RestoreTomlBlock(string current, string previous, string header)
        {
            string previousBlock;
            bool hadPrevious = TryGetTomlBlock(previous, header, out previousBlock);
            Regex pattern = TomlBlockPattern(header);

            if (hadPrevious)
            {
                string replacement = previousBlock.TrimEnd() + Environment.NewLine + Environment.NewLine;
                if (pattern.IsMatch(current))
                {
                    return pattern.Replace(current, replacement, 1);
                }

                int insertAt = FindProviderInsertPosition(current);
                if (insertAt >= 0)
                {
                    return current.Insert(insertAt, replacement);
                }

                return current.TrimEnd() + Environment.NewLine + Environment.NewLine + replacement;
            }

            return pattern.Replace(current, "", 1);
        }

        private static bool TryGetTomlBlock(string text, string header, out string block)
        {
            block = "";
            Match match = TomlBlockPattern(header).Match(text ?? "");
            if (!match.Success)
            {
                return false;
            }

            block = match.Value;
            return true;
        }

        private static Regex TomlBlockPattern(string header)
        {
            return new Regex(@"(?ms)^\[" + Regex.Escape(header) + @"\]\s*\r?\n.*?(?=^\[|\z)");
        }

        private static string NormalizeBlankLines(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            string normalized = Regex.Replace(text, @"(?m)^\s+\r?$", "");
            normalized = Regex.Replace(normalized, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
            return normalized.TrimEnd() + Environment.NewLine;
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
                LauncherLog.Info("Launching packaged Codex. Target: " + target + ", arguments: " + (arguments ?? ""));
                LaunchPackagedCodex(arguments);
                return;
            }

            var psi = new ProcessStartInfo();
            psi.FileName = target;
            psi.WorkingDirectory = Path.GetDirectoryName(target);
            psi.UseShellExecute = false;
            psi.Arguments = arguments ?? "";

            LauncherLog.Info("Launching Codex executable. Target: " + target + ", arguments: " + psi.Arguments);
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
            LauncherLog.Info("Packaged Codex activated. AppID: " + appId + ", processId: " + processId);
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
            LauncherIcon.Apply(this);
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
