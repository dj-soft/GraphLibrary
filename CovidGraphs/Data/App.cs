using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using WF = System.Windows.Forms;

namespace Djs.Tools.CovidGraphs.Data
{
    /// <summary>
    /// Aplikační support
    /// </summary>
    public class App
    {
        #region Singleton
        /// <summary>
        /// Soukromý přístup k singletonu
        /// </summary>
        protected static App Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_InstanceLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new App();
                        }
                    }
                }
                return _Instance;
            }
        }
        private App()
        {
            this._InitPath();
            this._InitConfig();
            this._InitLog();
        }
        private static App _Instance;
        private static object _InstanceLock = new object();
        #endregion
        #region Konfigurace
        /// <summary>
        /// Inicializace konfigurace
        /// </summary>
        private void _InitConfig()
        {
            this._Config = new Config(Path.Combine(this._ConfigPath, "config." + Config.ConfigFileExtension));
        }
        /// <summary>
        /// Uživatelova konfigurace
        /// </summary>
        public static Config Config { get { return Instance._Config; } }
        private Config _Config;
        #endregion
        #region Adresáře
        /// <summary>
        /// Inicializace adresářů
        /// </summary>
        private void _InitPath()
        {
            this._AppPath = Path.GetDirectoryName(this.GetType().Assembly.Location);
            this._ApplicationCode = DefaultApplicationCode;
        }
        /// <summary>
        /// Kód aplikace, používá se jako adresář
        /// </summary>
        public static string ApplicationCode
        {
            get { return Instance._ApplicationCode; }
            set { Instance._ApplicationCode = value; }
        }
        private string _ApplicationCode
        {
            get { return this.__ApplicationCode; }
            set
            {
                string path = value;
                if (!String.IsNullOrEmpty(path))
                {
                    path = path.Trim();
                    string fullPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Djs", path);
                    CreatePath(fullPath);        // Pokud někdo zadal chybnou cestu, pak tady dojde k chybě. Pak se nezmění následující hodnoty:
                    this.__ApplicationCode = value;
                    this._ConfigPath = fullPath;

                    // Změníme i úložiště Config souboru, pokud existuje:
                    if (this._Config != null)
                        this._Config.ConfigFile = Path.Combine(this._ConfigPath, "config." + Config.ConfigFileExtension);
                }
            }
        }
        private string __ApplicationCode;
        /// <summary>
        /// Adresář, kde je spuštěna aplikace
        /// </summary>
        public static string AppPath { get { return Instance._AppPath; } }
        private string _AppPath;
        /// <summary>
        /// Adresář, kde je ukládána konfigurace (=Aplikační data: konfigurace, provozní data, atd).
        /// Jde o adresář 
        /// Tento adresář uživatel nemůže změnit.
        /// </summary>
        public static string ConfigPath { get { return Instance._ConfigPath; } }
        private string _ConfigPath;
        /// <summary>
        /// Zajistí, že daný adresář bude existovat.
        /// Pokud to nelze, hodí chybu AppException.
        /// </summary>
        /// <param name="path"></param>
        public static void CreatePath(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new AppException("App.CreatePath: Není zadán adresář, který má existovat.");
            if (Directory.Exists(path)) return;
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception exc)
            {
                throw new AppException("App.CreatePath: Zadaný adresář " + path + " neexistuje a není možno jej vytvořit, chyba:" + exc.Message, exc);
            }
            if (Directory.Exists(path)) return;
            throw new AppException("App.CreatePath: Zadaný adresář " + path + " neexistuje a není možno jej vytvořit.");
        }
        public static string DefaultApplicationCode { get { return _DefaultApplicationCode; } set { if (!String.IsNullOrEmpty(value)) _DefaultApplicationCode = value; } }
        private static string _DefaultApplicationCode = "BasicApplication";
        #endregion
        #region Služby
        /// <summary>
        /// Provede danou akci v bloku try - catch, případnou chybu odchytí a zobrazí uživateli. Korektně vrátí řízení.
        /// </summary>
        /// <param name="action"></param>
        public static void TryRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exc)
            {
                ShowError(exc.Message);
            }
        }
        public static void ShowError(string text)
        {
            ShowMessage(text: text, buttons: WF.MessageBoxButtons.OK, icon: WF.MessageBoxIcon.Error);
        }
        public static void ShowWarning(WF.IWin32Window owner = null, string text = null, string caption = null)
        {
            ShowMessage(owner, text, caption, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Warning);
        }
        public static WF.DialogResult ShowMessage(WF.IWin32Window owner = null, string text = null, string caption = null, WF.MessageBoxButtons buttons = WF.MessageBoxButtons.OK, WF.MessageBoxIcon icon = WF.MessageBoxIcon.Information, WF.MessageBoxDefaultButton defaultButton = WF.MessageBoxDefaultButton.Button1)
        {
            if (owner == null) owner = GetMainForm();
            if (string.IsNullOrEmpty(text)) text = "Informace nebyla zadána";
            if (string.IsNullOrEmpty(caption)) caption = AppTitle;
            if (owner != null && owner is WF.Form form && form.InvokeRequired)
                return (WF.DialogResult)form.Invoke(new Func<WF.IWin32Window, string, string, WF.MessageBoxButtons, WF.MessageBoxIcon, WF.MessageBoxDefaultButton, WF.DialogResult>(_ShowMessage), owner, text, caption, buttons, icon, defaultButton);
            else
                return _ShowMessage(owner, text, caption, buttons, icon, defaultButton);
        }
        private static WF.DialogResult _ShowMessage(WF.IWin32Window owner, string text, string caption, WF.MessageBoxButtons buttons, WF.MessageBoxIcon icon, WF.MessageBoxDefaultButton defaultButton)
        {
            return WF.MessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
        }
        public static string AppTitle { get { return "Database"; } }
        protected static WF.Form GetMainForm()
        {
            WF.Form mainForm = null;
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var formPtr = process.MainWindowHandle;
                if (formPtr != IntPtr.Zero)
                {
                    var form = WF.Control.FromHandle(formPtr);
                    if (form != null)
                        mainForm = form as WF.Form;
                }
            }
            catch { }
            return mainForm;
        }
        #endregion
        #region Logování a čas
        /// <summary>
        /// Do logu vloží sadu itemů
        /// </summary>
        /// <param name="items"></param>
        public static void AddLog(string source, params object[] items)
        {
            if (items == null || items.Length == 0) return;
            Instance._AddLog(source, items);
        }
        /// <summary>
        /// Do logu vloží danou chybu
        /// </summary>
        /// <param name="exc"></param>
        public static void AddLog(Exception exc)
        {
            while (exc != null)
            {
                object[] items = new object[]
                {
                    exc.GetType().FullName,
                    exc.Message,
                    exc.StackTrace.Replace('\r', ';').Replace('\n', ' ').Replace('\t', ' ')
                };
                Instance._AddLog("Exception", items);
                exc = exc.InnerException;
            }
        }
        /// <summary>
        /// Aktuální text logu
        /// </summary>
        public static string LogText { get { return Instance._GetLogText(); } }
        /// <summary>
        /// Obsahuje aktuální čas od spuštění aplikace v hodnotách Tick.
        /// Zásadní význam má pro přesné měření uplynulého času, více v metodě <see cref="GetElapsedTime(long, ElapsedTimeType, int)"/>.
        /// </summary>
        public static long CurrentTime { get { return Instance._Stopwatch.ElapsedTicks; } }
        /// <summary>
        /// Vrátí čas (jako číslo), uplynulý od daného času startu (<paramref name="startTime"/>) do teď, jak počet sekund / milisekund / mikrosekund.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="timeType"></param>
        /// <param name="precission"></param>
        /// <returns></returns>
        public static decimal GetElapsedTime(long startTime, ElapsedTimeType timeType = ElapsedTimeType.Seconds, int precission = 3)
        {
            return Instance._GetElapsedTime(startTime, null, timeType, precission);
        }
        private decimal _GetElapsedTime(long startTime, long? stopTime, ElapsedTimeType timeType = ElapsedTimeType.Seconds, int precission = 3)
        {
            long time = _Stopwatch.ElapsedTicks - startTime;
            precission = (precission < 0 ? 0 : (precission > 6 ? 6 : precission));
            switch (timeType)
            {
                case ElapsedTimeType.Seconds: return Math.Round((time / _StopwatchSeconds), precission);
                case ElapsedTimeType.Miliseconds: return Math.Round((time / _StopwatchMilisecs), precission);
                case ElapsedTimeType.Microseconds: return Math.Round((time / _StopwatchMicrosecs), precission);
            }
            return 0m;
        }
        private string _GetLogText()
        {
            string text = null;
            lock (_Log)
            {
                text = _Log.ToString();
            }
            return text;
        }
        /// <summary>
        /// Inicializace logu
        /// </summary>
        private void _InitLog()
        {
            this._Stopwatch = new System.Diagnostics.Stopwatch();
            this._StopwatchSeconds = (decimal)System.Diagnostics.Stopwatch.Frequency;
            this._StopwatchMilisecs = this._StopwatchSeconds / 1000m;
            this._StopwatchMicrosecs = this._StopwatchSeconds / 1000000m;
            this._LogRow = 0L;
            this._Log = new StringBuilder();
            this._AddLogTitle();
            this._Stopwatch.Start();
            this._LogLastTick = _Stopwatch.ElapsedTicks;
        }
        /// <summary>
        /// Do logu přidá titulek
        /// </summary>
        private void _AddLogTitle()
        {
            string tab = "\t";
            StringBuilder sb = new StringBuilder();
            lock (_Log)
            {
                sb.Append("Row");
                sb.Append(tab);
                sb.Append("Time");
                sb.Append(tab);
                sb.Append("Total [us]");
                sb.Append(tab);
                sb.Append("Delta [us]");
                sb.Append(tab);
                sb.Append("Source");
                sb.Append(tab);
                sb.Append("Thread");
                for (int i = 0; i < 12; i++)
                    sb.Append(tab + "Item:" + i.ToString());
                _Log.AppendLine(sb.ToString());
            }
        }
        /// <summary>
        /// přidá dané prvky do logu
        /// </summary>
        /// <param name="items"></param>
        private void _AddLog(string source, object[] items)
        {
            string tab = "\t";
            StringBuilder sb = new StringBuilder();
            lock (_Log)
            {
                long row = ++_LogRow;
                sb.Append(row.ToString());
                sb.Append(tab);

                sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                sb.Append(tab);

                long startTime = _LogLastTick;
                long stopTime = _Stopwatch.ElapsedTicks;
                long microsecs;

                microsecs = (long)_GetElapsedTime(0L, stopTime, ElapsedTimeType.Microseconds, 0);
                sb.Append(microsecs.ToString());
                sb.Append(tab);

                microsecs = (long)_GetElapsedTime(startTime, stopTime, ElapsedTimeType.Microseconds, 0);
                sb.Append(microsecs.ToString());
                sb.Append(tab);

                sb.Append(source);
                sb.Append(tab);

                sb.Append(Thread.CurrentThread.Name);

                foreach (object item in items)
                    sb.Append(tab + (item?.ToString() ?? ""));

                _Log.AppendLine(sb.ToString());

                _LogLastTick = stopTime;
            }
        }
        private long _LogRow;
        private long _LogLastTick;
        private System.Diagnostics.Stopwatch _Stopwatch;
        private decimal _StopwatchSeconds;
        private decimal _StopwatchMilisecs;
        private decimal _StopwatchMicrosecs;
        private StringBuilder _Log;
        /// <summary>
        /// Jednotka vráceného času v metodě <see cref="App.GetElapsedTime(long, ElapsedTimeType)"/>
        /// </summary>
        public enum ElapsedTimeType
        {
            Seconds,
            Miliseconds,
            Microseconds
        }

        #endregion
    }
    #region AppException
    public class AppException : Exception
    {
        public AppException(string message) : base(message) { }
        public AppException(string message, Exception innerException) : base(message, innerException) { }
    }
    #endregion
}
