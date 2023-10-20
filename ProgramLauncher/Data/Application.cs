using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;
using DjSoft.Tools.ProgramLauncher.Data;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Runtime.InteropServices;

namespace DjSoft.Tools.ProgramLauncher
{
    /// <summary>
    /// Main singleton aplikace
    /// </summary>
    public class App
    {
        #region Singleton
        /// <summary>
        /// Singleton celé aplikace s dostupnými daty a službami
        /// </summary>
        public static App Current
        {
            get
            {
                if (__Current is null)
                {
                    lock (__Lock)
                    {
                        if (__Current is null)
                            _CreateCurrent();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Vytvoří a vrátí new instanci
        /// </summary>
        private static void _CreateCurrent()
        {
            __Current = new App();
            __Current._Initialize();
        }
        private static App __Current;
        private static object __Lock = new object();
        /// <summary>
        /// Konstruktor: první fáze inicializace, nesmí používat <see cref="Current"/>
        /// </summary>
        private App()
        { }
        /// <summary>
        /// Inicializace: druhá fáze inicializace, v omezené míře smí používat <see cref="Current"/>
        /// </summary>
        private void _Initialize()
        {
            _InitGraphics();
            _InitFonts();
            _InitImages();
            // Co přidáš sem, přidej i do _Exit() !!!
        }
        #endregion
        #region Řízený start a konec aplikace, Argumenty
        /// <summary>
        /// Zahájení běhu aplikace, předání parametrů.
        /// <para/>
        /// Pokud this aplikace bude sloužit jako Singleton (=jediný proces v rámci Windows), pak je třeba předat aktivní Mutex <paramref name="appMutex"/>.
        /// Aplikace si jej uloží a drží, na konci běhu v metodě <see cref="Exit"/> jej uvolní.
        /// V tom případě si tato aplikace vytvoří ServerPipe, skrze kterou pak je ochotna komunikovat s případnými SlavePipe, viz metody ....
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="appMutex"></param>
        public static void Start(string[] arguments, Mutex appMutex = null)
        {
            Current._Start(arguments, appMutex);
        }
        /// <summary>
        /// Ukončení běhu aplikace
        /// </summary>
        public static void Exit()
        {
            if (__Current != null)
            {
                __Current._Exit();
                __Current = null;
            }
        }
        /// <summary>
        /// Zahájení běhu aplikace, předání parametrů, uložení Mutexu a tvorba ServerPipe
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="appMutex"></param>
        private void _Start(string[] arguments, Mutex appMutex)
        {
            _ApplicationState = ApplicationState.Starting;
            __Arguments = arguments ?? new string[0];
            __ApplicationFile = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            __ApplicationPath = System.IO.Path.GetDirectoryName(__ApplicationFile);
            __AppMutex = appMutex;
            if (appMutex != null)
                _CreateServerPipe();
        }
        /// <summary>
        /// Ukončení běhu aplikace
        /// </summary>
        private void _Exit()
        {
            _DisposeSettings();
            _DisposeGraphics();
            _DisposeFonts();
            _DisposeImages();
            _DisposeTrayNotifyIcon();
            _DisposeAppMutex();
            _MainForm = null;
        }
        /// <summary>
        /// Argumenty předané při startu.
        /// Operační systém je rozdělil v místě mezery.
        /// Pokud byl přítomen parametr s uvozovkami, pak jsou odstraněny a zde je obsah bez uvozovek.
        /// <para/>
        /// Pokud byla aplikace spuštěna s příkazovým řádkem: 'Aplikace.exe reset maximized config="c:\data.cfg" ', 
        /// pak zde jsou tři argumenty jako tři stringy: { reset , maximized , config=c:\data.cfg }         (kde string  config=c:\data.cfg  je třetí argument)
        /// <para/>
        /// Pokud byla aplikace spuštěna s příkazovým řádkem: 'Aplikace.exe reset maximized config = "c:\data aplikací.cfg" '    (mezery okolo rovnítka),
        /// pak zde je pět argumentů: { reset , maximized , config , = , c:\data aplikací.cfg }           (kde čárka odděluje jednotlivé stringy, a celý string  c:\data aplikací.cfg  je pátý argument)
        /// </summary>
        public static string[] Arguments { get { return Current.__Arguments.ToArray(); } }
        private string[] __Arguments;
        /// <summary>
        /// Plné jméno this aplikace
        /// </summary>
        public static string ApplicationFile { get { return Current.__ApplicationFile; } }
        private string __ApplicationFile;
        /// <summary>
        /// Adresář this aplikace
        /// </summary>
        public static string ApplicationPath { get { return Current.__ApplicationPath; } }
        private string __ApplicationPath;
        /// <summary>
        /// Zkusí najít argument s daným textem. Vrací true = nalezeno.
        /// </summary>
        /// <param name="text">Hledaný text</param>
        /// <param name="caseSensitive">Má se hledat CaseSensitive? Tedy pokud je true, pak "Abc" != "abc"</param>
        /// <param name="exactText">Má se hledat celý text? Tedy pokud je true, pak při hledání "ABC" najdu jen argume "ABC", ale nenajdu argument "ABC=12345"</param>
        /// <returns></returns>
        public static bool HasArgument(string text, bool caseSensitive = false, bool exactText = false)
        {
            return Current._TryGetArgument(text, out var _, caseSensitive, exactText);
        }
        /// <summary>
        /// Zkusí najít argument s daným textem. Vrací true = nalezeno
        /// </summary>
        /// <param name="text">Hledaný text</param>
        /// <param name="foundArgument">Out nalezený text argumentu</param>
        /// <param name="caseSensitive">Má se hledat CaseSensitive? Tedy pokud je true, pak "Abc" != "abc"</param>
        /// <param name="exactText">Má se hledat celý text? Tedy pokud je true, pak při hledání "ABC" najdu jen argume "ABC", ale nenajdu argument "ABC=12345"</param>
        /// <returns></returns>
        public static bool TryGetArgument(string text, out string foundArgument, bool caseSensitive = false, bool exactText = false)
        {
            return Current._TryGetArgument(text, out foundArgument, caseSensitive, exactText);
        }
        /// <summary>
        /// Zkusí najít argument s daným textem. Vrací true = nalezeno
        /// </summary>
        /// <param name="text">Hledaný text</param>
        /// <param name="foundArgument">Out nalezený text argumentu</param>
        /// <param name="caseSensitive">Má se hledat CaseSensitive? Tedy pokud je true, pak "Abc" != "abc"</param>
        /// <param name="exactText">Má se hledat celý text? Tedy pokud je true, pak při hledání "ABC" najdu jen argume "ABC", ale nenajdu argument "ABC=12345"</param>
        /// <returns></returns>
        private bool _TryGetArgument(string text, out string foundArgument, bool caseSensitive, bool exactText)
        {
            foundArgument = null;
            if (String.IsNullOrEmpty(text)) return false;
            var arguments = __Arguments;
            if (arguments.Length == 0) return false;
            StringComparison comparison = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return arguments.TryFindFirst(a => (exactText ? String.Equals(a, text, comparison) : a.StartsWith(text, comparison)), out foundArgument);
        }
        #endregion
        #region SigletonProcess: Mutex a ServerPipe, a static služby ClientPipe (mimo aplikační)
        /// <summary>
        /// Vytvoří a uloží si instanci <see cref="__ServerPipe"/>
        /// </summary>
        private void _CreateServerPipe()
        {
            __ServerPipe = new ServerPipe(SingleProcess.IpcPipeName);
            __ServerPipe.Connected += __ServerPipe_Connected;
            __ServerPipe.DataReceived += __ServerPipe_DataReceived;
        }
        /// <summary>
        /// K <see cref="__ServerPipe"/> se někdo připojil
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __ServerPipe_Connected(object sender, EventArgs e)
        {
        }
        /// <summary>
        /// Do <see cref="__ServerPipe"/> někdo poslal data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __ServerPipe_DataReceived(object sender, PipeEventArgs e)
        {
            string message = e.String;
            if (String.IsNullOrEmpty(message)) return;
            switch (message)
            {
                case SingleProcess.IpcPipeShowMainFormRequest:
                    _TrayNotifyIconActivateMainForm();
                    __ServerPipe.WriteString(SingleProcess.IpcPipeResponseOK);
                    break;
            }
        }
        /// <summary>
        /// Korektně uvolní Mutex a ServerPipe
        /// </summary>
        private void _DisposeAppMutex()
        {
            var mutex = __AppMutex;
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.TryDispose();
                __AppMutex = null;
            }

            var serverPipe = __ServerPipe;
            if (serverPipe != null)
            {
                serverPipe.Close();
                __ServerPipe = null;
            }
        }
        /// <summary>
        /// Uložený Mutex - jen v případě, kdy this process je první
        /// </summary>
        private Mutex __AppMutex;
        /// <summary>
        /// IPC komunikační Pipe typu Server, existuje jen v rámci Server Singletonu, s podporou Mutexu <see cref="__AppMutex"/>
        /// </summary>
        private ServerPipe __ServerPipe;
        /// <summary>
        /// Metoda vrátí <see cref="ClientPipe"/> s korektním jménem, ale bez volání <see cref="ClientPipe.Connect()"/>.
        /// </summary>
        /// <returns></returns>
        public static ClientPipe CreateClientIpcPipe()
        {
            var clientPipe = new ClientPipe(SingleProcess.IpcPipeName);
            return clientPipe;
        }
        #endregion
        #region Vyhledání Windows procesu, Tray ikona
        /// <summary>
        /// Metoda vyhledá a vrátí systémový process pro daný spustitelný soubor
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="arguments">Argumenty procesu. NULL = ignorují se, prázdný string = musí být shodný.</param>
        /// <param name="processId">Vyhledat primárně proces daného ID. Pokud je nalezen, netestuje se shoda <paramref name="applicationName"/> ani <paramref name="arguments"/>. Parametr <paramref name="includeNoWindowProcess"/> je i zde akceptován.</param>
        /// <param name="includeNoWindowProcess">Akceptovat i procesy, jejichž <see cref="Process.MainWindowHandle"/> je Zero</param>
        /// <param name="filter">Přidaný filtr na procesy, null = veškeré vhodné</param>
        /// <returns></returns>
        public static Process SearchForProcess(string applicationName, string arguments = null, int? processId = null, bool includeNoWindowProcess = false, Func<Process, bool> filter = null)
        {
            Process process = null;
            if (String.IsNullOrEmpty(applicationName)) return process;

            var processes = Process.GetProcesses();
            string name = System.IO.Path.GetFileName(applicationName);
            var prefiltered = processes.Where(p => isPrefilter(p)).ToArray();
            if (prefiltered.Length == 0) return null;

            var startTime = (prefiltered.Length == 0 ? null : (DateTime?)prefiltered[0].StartTime);

            if (processId.HasValue)
            {
                process = prefiltered.FirstOrDefault(p => p.Id == processId.Value);
                if (process != null) return process;
            }

            foreach (var proc in prefiltered)
            {
                try
                {
                    var mainModuleFile = proc.MainModule?.FileName;
                    if (String.Equals(mainModuleFile, applicationName, StringComparison.InvariantCultureIgnoreCase))
                    {   // Je to shodná aplikace:
                        bool accept = true;
                        if (arguments != null)
                        {   // Pokud zadané argumenty nejsou NULL, musí se shodovat s těmi nalezenými:
                            var args = getArgumentsOfProcess(proc.Id);
                            accept = String.Equals(arguments ?? "", args ?? "", StringComparison.InvariantCultureIgnoreCase);      // Konverze null => "" pro obě hodnoty
                        }
                        if (accept)
                        {
                            process = proc;
                            break;
                        }
                    }
                }
                catch (Exception) { /* Tento proces to nebude. */ }
            }
            return process;


            // Vrátí true, pokud daný proces 
            bool isPrefilter(Process filtProc)
            {
                try
                {
                    if (filtProc is null || filtProc.HasExited) return false;
                    if (!String.Equals(System.IO.Path.GetFileName(filtProc.MainModule.FileName), name, StringComparison.InvariantCultureIgnoreCase)) return false;
                    if (!includeNoWindowProcess && filtProc.MainWindowHandle == IntPtr.Zero) return false;
                    if (filter != null && !filter(filtProc)) return false;
                    return true;
                }
                catch { }
                return false;
            }

            // Pro daný proces najde a vrátí argumenty z jeho CommandLine
            string getArgumentsOfProcess(int procId)
            {
                string wmiQuery = $"select CommandLine from Win32_Process where ProcessId={procId}";
                string cmdArguments = "";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery))
                using (ManagementObjectCollection retObjectCollection = searcher.Get())
                {
                    foreach (ManagementObject retObject in retObjectCollection)
                    {   // Jednotlivé řádky = jednotlivé procesy:
                        var commandLine = retObject["CommandLine"];       // commandLine například:   "C:\WINDOWS\system32\NOTEPAD.EXE" D:\Windows\Složka\a další\Text.txt
                        cmdArguments = getArgumentsOfCommandLine(commandLine as String);
                        break;
                        // Když bych dal dotaz:                           $"select * from Win32_Process where ProcessId={procId}"
                        // Pak můžu číst všechny informace o procesu:     foreach (var item in retObject.Properties) { }
                    }
                }
                return cmdArguments;
            }

            // Z celého textu CommandLine vrátí část odpovídající argumentům
            string getArgumentsOfCommandLine(string cmdLine)
            {
                string args = null;
                if (String.IsNullOrEmpty(cmdLine)) return args;
                cmdLine = cmdLine.Trim();
                if (cmdLine.Length <= 1) return args;

                int index = ((cmdLine[0] == '"') ?
                    cmdLine.IndexOf("\"", 1) :              // Pokud začínáme uvozovkou, pak najdeme tu druhou ... "C:\WINDOWS\system32\NOTEPAD.EXE" D:\Windows\Složka\a další\Text.txt
                    cmdLine.IndexOf(" ", 1));               // Pokud NEzačínáme uvozovkou, pak najdeme mezeru  ...  C:\WINDOWS\system32\NOTEPAD.EXE D:\Windows\Složka\a další\Text.txt
                if (index < 0 || index >= (cmdLine.Length - 1)) return args;

                return cmdLine.Substring(index + 1).TrimStart();
            }
        }
        /// <summary>
        /// Metoda zajistí aktivaci hlavního okna daného procesu
        /// </summary>
        /// <param name="process"></param>
        public static bool ActivateWindowsProcess(Process process)
        {
            if (process != null && process.MainWindowHandle != IntPtr.Zero)
            {
                WinApi.SetWindowToForeground(process.MainWindowHandle, true);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Bylo požádáno o zavření aplikace, hlavní okno aplikace se tomu nebude bránit
        /// </summary>
        public static bool ApplicationIsClosing { get { return Current.__ApplicationIsClosing; } set { Current.__ApplicationIsClosing = value; } }
        /// <summary>
        /// Bylo požádáno o zavření aplikace, hlavní okno aplikace se tomu nebude bránit
        /// </summary>
        private bool __ApplicationIsClosing;
        /// <summary>
        /// Obsahuje true v VisualStudio Debugger režimu, false při běžném Run mode
        /// </summary>
        public static bool IsDebugMode { get { return System.Diagnostics.Debugger.IsAttached; } }
        /// <summary>
        /// Aktivuje ikonu aplikace v TrayNotification liště. Podle situace i zobrazí BaloonTip s informací o chování aplikace.
        /// Používá se ve chvíli, kdy Main okno aplikace je "zavíráno" a bude skryté, ale nikoli ukončené.
        /// </summary>
        public static void ActivateTrayNotifyIcon() { Current._ActivateTrayNotifyIcon(); }
        /// <summary>
        /// Skryje ikonu aplikace v TrayNotification liště, pokud tam je viditelná.
        /// </summary>
        public static void HideTrayNotifyIcon() { Current._HideTrayNotifyIcon(); }
        /// <summary>
        /// Aktivuje ikonu aplikace v TrayNotification liště. Podle situace i zobrazí BaloonTip s informací o chování aplikace.
        /// Používá se ve chvíli, kdy Main okno aplikace je "zavíráno" a bude skryté, ale nikoli ukončené.
        /// </summary>
        private void _ActivateTrayNotifyIcon()
        {
            if (__TrayNotifyMenu is null)
            {
                bool trayInfoIsAccepted = App.Settings.TrayInfoIsAccepted;
                __TrayNotifyMenuItems = new DataMenuItem[]
                {
                    new DataMenuItem() { Image = Properties.Resources.klickety_2_22, UserData = TrayIconMenuAction.ShowApplication },
                    new DataMenuItem() { Image = (trayInfoIsAccepted ? Properties.Resources.dialog_clean_22 : null), UserData = TrayIconMenuAction.AcceptTrayInfo },
                    new DataMenuItem() { ItemType = MenuItemType.Separator },
                    new DataMenuItem() { Image = Properties.Resources.application_exit_5_22, ToolTip = App.Messages.TrayIconApplicationExitToolTip, UserData = TrayIconMenuAction.ExitApplication }
                };
                _LocalizeTrayNotifyMenu();
                __TrayNotifyMenu = CreateContextMenuStrip(__TrayNotifyMenuItems, _TrayNotifyMenuClick);
                __CurrentLanguageChanged -= _CurrentLanguageChanged_TrayIcon;
                __CurrentLanguageChanged += _CurrentLanguageChanged_TrayIcon;
            }

            if (__TrayNotifyIcon is null)
            {
                __TrayNotifyIcon = new NotifyIcon()
                {
                    Icon = Properties.Resources.klickety_2_64,
                    BalloonTipIcon = ToolTipIcon.Info,
                    ContextMenuStrip = __TrayNotifyMenu
                };
                _LocalizeTrayNotifyIcon();
                __TrayNotifyIcon.MouseClick += _TrayNotifyIcon_MouseClick;
            }
            __TrayNotifyIcon.Visible = true;

            TimeSpan repeatTime = (App.IsDebugMode ? TimeSpan.FromSeconds(15d) : TimeSpan.FromMinutes(30d));
            _ShowBaloonTrayNotifyIcon(repeatTime, TimeSpan.FromSeconds(6));
        }
        /// <summary>
        /// Skryje ikonu aplikace v TrayNotification liště, pokud tam je viditelná.
        /// </summary>
        private void _HideTrayNotifyIcon()
        {
            if (__TrayNotifyIcon != null)
                __TrayNotifyIcon.Visible = false;
        }
        /// <summary>
        /// Po změně jazyka provedeme překlad prvků menu TrayNotifyIcon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _CurrentLanguageChanged_TrayIcon(object sender, EventArgs e)
        {
            _LocalizeTrayNotifyMenu();
            _LocalizeTrayNotifyIcon();
        }
        /// <summary>
        /// Lokalizuje položky menu <see cref="__TrayNotifyMenuItems"/>
        /// </summary>
        private void _LocalizeTrayNotifyMenu()
        {
            var menuItems = __TrayNotifyMenuItems;
            if (menuItems != null)
            {
                foreach (var menuItem in menuItems)
                {
                    if (menuItem.UserData is TrayIconMenuAction action)
                    {
                        switch (action)
                        {
                            case TrayIconMenuAction.ShowApplication:
                                menuItem.Text = App.Messages.TrayIconShowApplicationText;
                                menuItem.ToolTip = App.Messages.TrayIconShowApplicationToolTip;
                                break;
                            case TrayIconMenuAction.AcceptTrayInfo:
                                menuItem.Text = App.Messages.TrayIconAcceptTrayInfoText;
                                menuItem.ToolTip = App.Messages.TrayIconAcceptTrayInfoToolTip;
                                break;
                            case TrayIconMenuAction.ExitApplication:
                                menuItem.Text = App.Messages.TrayIconApplicationExitText;
                                menuItem.ToolTip = App.Messages.TrayIconApplicationExitToolTip;
                                break;
                        }
                        App.RefreshMenuItem(menuItem);
                    }
                }
            }


        }
        /// <summary>
        /// Lokalizuje položky menu <see cref="__TrayNotifyMenuItems"/>
        /// </summary>
        private void _LocalizeTrayNotifyIcon()
        {
            var trayIcon = __TrayNotifyIcon;
            if (trayIcon != null)
            {
                trayIcon.Text = App.Messages.TrayIconText;
                trayIcon.BalloonTipText = App.Messages.TrayIconBalloonText; 
                trayIcon.BalloonTipTitle = App.Messages.TrayIconBalloonToolTip;
            }
        }
        /// <summary>
        /// Metoda zobrazí BaloonTip pro Tray ikonu.
        /// Nezobrazí ji, pokud byla zobrazena nedávno (v době <paramref name="repeatTime"/>).
        /// Zobrazí ji na daný časový interval <paramref name="baloonTime"/>.
        /// Zapamatuje si čas, kdy je zobrazena, aby příště bylo možno určit čas <paramref name="repeatTime"/>.
        /// </summary>
        /// <param name="repeatTime"></param>
        /// <param name="baloonTime"></param>
        private void _ShowBaloonTrayNotifyIcon(TimeSpan? repeatTime, TimeSpan baloonTime)
        {
            if (App.Settings.TrayInfoIsAccepted) return;             // Uživatel v kontextovém menu v TrayIcon zaškrtnul, že chápe skrývání aplikace.

            // Pokud je dán čas 'repeatTime' (=čas opakování informace), a informace už byla zobrazena (naposledy v čase _TrayNotifyIconLastBaloonTime),
            // a aktuální čas 'now' je menší než čas poslední informace plus 'repeatTime', pak baloon nezobrazím:
            var now = DateTime.Now;
            if (repeatTime.HasValue && __TrayNotifyIconLastBaloonTime.HasValue && now < __TrayNotifyIconLastBaloonTime.Value.Add(repeatTime.Value)) return;

            __TrayNotifyIconLastBaloonTime = now;
            __TrayNotifyIcon.ShowBalloonTip((int)baloonTime.TotalMilliseconds);
        }
        /// <summary>
        /// Kliknutí na TrayNotification ikonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TrayNotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _TrayNotifyIconActivateMainForm();
            }
            else
            {

            }
        }
        /// <summary>
        /// Výběr z kontextového menu na TrayNotification ikoně
        /// </summary>
        /// <param name="menuItem"></param>
        private void _TrayNotifyMenuClick(IMenuItem menuItem)
        {
            if (menuItem.UserData is TrayIconMenuAction action)
            {
                switch (action)
                {
                    case TrayIconMenuAction.ShowApplication:
                        _TrayNotifyIconActivateMainForm();
                        break;
                    case TrayIconMenuAction.AcceptTrayInfo:
                        _TrayNotifyIconAcceptTrayInfo(menuItem);
                        break;
                    case TrayIconMenuAction.ExitApplication:
                        _TrayNotifyIconCloseApplication();
                        break;
                }
            }
        }
        /// <summary>
        /// Výběr z kontextového menu na TrayNotification ikoně: aktivovat formulář
        /// </summary>
        private void _TrayNotifyIconActivateMainForm()
        {
            this.__MainForm.Activate(true);
            __TrayNotifyIcon.Visible = false;
        }
        /// <summary>
        /// Výběr z kontextového menu na TrayNotification ikoně: akceptovat info o tray ikoně a aplikaci
        /// </summary>
        /// <param name="menuItem"></param>
        private void _TrayNotifyIconAcceptTrayInfo(IMenuItem menuItem)
        {
            bool trayInfoIsAccepted = !App.Settings.TrayInfoIsAccepted;                  // Akceptujeme TrayInfo = nový stav bude opačný než dosavadní
            (menuItem as DataMenuItem).Image = (trayInfoIsAccepted ? Properties.Resources.dialog_clean_22 : null);
            App.RefreshMenuItem(menuItem);

            App.Settings.TrayInfoIsAccepted = trayInfoIsAccepted;
        }
        /// <summary>
        /// Výběr z kontextového menu na TrayNotification ikoně: Zavřít aplikaci
        /// </summary>
        private void _TrayNotifyIconCloseApplication()
        {
            this.__ApplicationIsClosing = true;
            this.__MainForm.Close();
        }
        /// <summary>
        /// Provede úklid TrayNotifyIcon
        /// </summary>
        private void _DisposeTrayNotifyIcon()
        {
            if (__TrayNotifyIcon != null)
            {
                __TrayNotifyIcon.Visible = false;
                __TrayNotifyIcon.Dispose();
                __TrayNotifyIcon = null;
            }
            if (__TrayNotifyMenu != null)
            {
                __TrayNotifyMenu.Dispose();
                __TrayNotifyMenu = null;
            }
        }
        /// <summary>
        /// Jednotlivé akce na TrayNotification ikoně
        /// </summary>
        private enum TrayIconMenuAction { ShowApplication, AcceptTrayInfo, ExitApplication }
        private DataMenuItem[] __TrayNotifyMenuItems;
        /// <summary>
        /// Kontextové menu na TrayNotification ikoně
        /// </summary>
        private ContextMenuStrip __TrayNotifyMenu;
        /// <summary>
        /// TrayNotification ikona
        /// </summary>
        private NotifyIcon __TrayNotifyIcon;
        /// <summary>
        /// Čas posledního zobrazení BaloonTipu na TrayNotification ikoně
        /// </summary>
        private DateTime? __TrayNotifyIconLastBaloonTime;
        #endregion
        #region Settings
        #region Přístup na Settings
        /// <summary>
        /// Konfigurace aplikace
        /// </summary>
        public static Settings Settings { get { return Current._Settings; } }
        /// <summary>
        /// Instance Konfigurace aplikace, OnDemand
        /// </summary>
        private Settings _Settings
        {
            get
            {
                if (__Settings is null)
                    __Settings = Settings.Create();
                return __Settings;
            }
        }
        /// <summary>
        /// Proměnná pro Konfigurace aplikace
        /// </summary>
        private Settings __Settings;
        /// <summary>
        /// Ukončení Konfigurace aplikace = její Save
        /// </summary>
        private void _DisposeSettings()
        {
            __Settings?.SaveNow();
            __Settings = null;
        }
        internal const string Company = "DjSoft";
        internal const string ProductName = "ProgramLauncher";
        internal static string ProductTitle { get { return "Nabídka aplikací"; } }
        #endregion
        #region Využívání Settings pro práci s jeho daty pomocí App
        #endregion
        #endregion
        #region Messages a Language
        /// <summary>
        /// Veškeré textové výstupy systému (lokalizace)
        /// </summary>
        public static LanguageSet Messages { get { return Current._Messages; } }
        private LanguageSet _Messages
        {
            get
            {
                if (__Messages is null)
                    __Messages = LanguageSet.CreateDefault();
                return __Messages;
            }
        }
        private LanguageSet __Messages;

        /// <summary>
        /// Pole všech přítomných jazyků
        /// </summary>
        public static Language[] Languages { get { return LanguageSet.Collection; } }
        /// <summary>
        /// Aktuální sada definující layout; lze změnit, po změně dojde eventu <see cref="CurrentLayoutSetChanged"/>.
        /// Pozor, tato property není propojena s <see cref="Settings.LayoutSetName"/>, toto propojení musí zajistit aplikace.
        /// Důvodem je vhodné načasování zaháčkování a provedení eventu po změně / inicializaci.
        /// </summary>
        public static Language CurrentLanguage { get { return Current._CurrentLanguage; } set { Current._CurrentLanguage = value; } }
        /// <summary>
        /// Aktuální jazyk aplikace; řeší autoinicializaci i hlídání změny a vyvolání eventu
        /// </summary>
        private Language _CurrentLanguage
        {
            get
            {
                if (__CurrentLanguage is null)
                    __CurrentLanguage = _Messages.Default;
                return __CurrentLanguage;
            }
            set
            {
                if (value is null) return;
                bool isChange = (__CurrentLanguage is null || !Object.ReferenceEquals(value, __CurrentLanguage));
                __CurrentLanguage = value;
                if (isChange) __CurrentLanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aktuální jazyk aplikace, proměnná
        /// </summary>
        private Language __CurrentLanguage;
        /// <summary>
        /// Událost volaná po změně aktuálního jazyka
        /// </summary>
        public static event EventHandler CurrentLanguageChanged { add { Current.__CurrentLanguageChanged += value; } remove { Current.__CurrentLanguageChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně aktuálního jazyka
        /// </summary>
        private event EventHandler __CurrentLanguageChanged;
        #endregion
        #region Monitory, zarovnání souřadnic do monitoru, ukládání souřadnic oken
        #endregion
        #region Popup Menu
        /// <summary>
        /// Z dodaných prvků vytvoří kontextové Popup menu, zobrazí jej na dané souřadnici (nebo na aktuální souřadnici myši) a nabídne uživateli.
        /// Až si uživatel vybere (asynchronní), pak předá řízení do dané metody <paramref name="onSelectItem"/> a předá do ní vybranou položku menu.
        /// Volající si pak sám provede odpovídající akci. 
        /// Může k tomu využít prostor <see cref="IMenuItem.UserData"/> v prvku menu, kde si uchová kontext pro tuto akci.
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="onSelectItem"></param>
        /// <param name="pointOnScreen"></param>
        public static void SelectFromMenu(IEnumerable<IMenuItem> menuItems, Action<IMenuItem> onSelectItem, Point? pointOnScreen)
        {
            if (!pointOnScreen.HasValue) pointOnScreen = Control.MousePosition;

            ToolStripDropDownMenu menu = CreateToolStripDropDownMenu(menuItems, onSelectItem);
            menu.Show(pointOnScreen.Value);
        }
        /// <summary>
        /// Vytvoří a vrátí <see cref="ToolStripDropDownMenu"/> pro dané prvky a cílovou akci
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="onSelectItem"></param>
        /// <returns></returns>
        public static ToolStripDropDownMenu CreateToolStripDropDownMenu(IEnumerable<IMenuItem> menuItems, Action<IMenuItem> onSelectItem)
        {
            var menu = new ToolStripDropDownMenu();

            menu.DropShadowEnabled = true;
            menu.RenderMode = ToolStripRenderMode.Professional;
            menu.ShowCheckMargin = false;
            menu.ShowImageMargin = true;
            menu.ItemClicked += _OnMenuItemClicked;
            menu.ImageScalingSize = new Size(20, 20);

            foreach (var menuItem in menuItems)
                _AddToolStripItemTo(menu.Items, menuItem, onSelectItem);

            return menu;
        }
        /// <summary>
        /// Vytvoří a vrátí <see cref="ContextMenuStrip"/> pro dané prvky a cílovou akci
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="onSelectItem"></param>
        /// <returns></returns>
        public static ContextMenuStrip CreateContextMenuStrip(IEnumerable<IMenuItem> menuItems, Action<IMenuItem> onSelectItem)
        {
            var menu = new ContextMenuStrip();

            menu.DropShadowEnabled = true;
            menu.RenderMode = ToolStripRenderMode.Professional;
            menu.ShowCheckMargin = false;
            menu.ShowImageMargin = true;
            menu.ItemClicked += _OnMenuItemClicked;

            foreach (var menuItem in menuItems)
                _AddToolStripItemTo(menu.Items, menuItem, onSelectItem);

            return menu;
        }
        /// <summary>
        /// Provede refresh prvku menu
        /// </summary>
        /// <param name="menuItem"></param>
        public static void RefreshMenuItem(IMenuItem menuItem)
        {
            if (menuItem is null || menuItem.ToolItem is null) return;
            ToolStripItem toolItem = null;
            switch (menuItem.ItemType)
            {
                case MenuItemType.Separator:
                    break;
                case MenuItemType.Header:
                    if (menuItem.ToolItem is ToolStripLabel headerItem)
                        toolItem = headerItem;
                    break;
                case MenuItemType.Button:
                default:
                    if (menuItem.ToolItem is ToolStripMenuItem buttonItem)
                    {
                        buttonItem.Enabled = menuItem.Enabled;
                        buttonItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                        buttonItem.ToolTipText = menuItem.ToolTip;
                        toolItem = buttonItem;
                    }
                    break;
            }

            if (toolItem != null)
            {
                toolItem.Text = menuItem.Text;
                toolItem.Image = menuItem.Image;
                var fontStyle = menuItem.FontStyle;
                if (fontStyle.HasValue)
                    toolItem.Font = App.GetFont(toolItem.Font, null, fontStyle.Value);
            }
        }
        /// <summary>
        /// Vygeneruje a vrátí <see cref="ToolStripMenuItem"/> z dané datové položky
        /// </summary>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private static void _AddToolStripItemTo(ToolStripItemCollection toolItems, IMenuItem menuItem, Action<IMenuItem> onSelectItem)
        {
            toolItems.Add(_CreateToolStripItem(menuItem, onSelectItem));
            if (menuItem.ItemType == MenuItemType.Header)
                toolItems.Add(new ToolStripSeparator());
        }
        /// <summary>
        /// Vygeneruje a vrátí <see cref="ToolStripMenuItem"/> z dané datové položky
        /// </summary>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private static ToolStripItem _CreateToolStripItem(IMenuItem menuItem, Action<IMenuItem> onSelectItem)
        {
            ToolStripItem toolItem;

            var fontStyle = menuItem.FontStyle;
            switch (menuItem.ItemType)
            {
                case MenuItemType.Separator:
                    var separatorItem = new ToolStripSeparator();
                    toolItem = separatorItem;
                    break;
                case MenuItemType.Header:
                    var headerItem = new ToolStripLabel(menuItem.Text, menuItem.Image);
                    if (!fontStyle.HasValue) fontStyle = FontStyle.Bold;       // Implicitní styl fontu pro Header
                    toolItem = headerItem;
                    break;
                case MenuItemType.Button:
                case MenuItemType.Default:
                default:
                    var buttonItem = new ToolStripMenuItem(menuItem.Text, menuItem.Image) { Tag = new Tuple<IMenuItem, Action<IMenuItem>>(menuItem, onSelectItem) };
                    buttonItem.Enabled = menuItem.Enabled;
                    buttonItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                    buttonItem.ToolTipText = menuItem.ToolTip;
                    toolItem = buttonItem;
                    break;
            }
            toolItem.Tag = new Tuple<IMenuItem, Action<IMenuItem>>(menuItem, onSelectItem);
            toolItem.ToolTipText = menuItem.ToolTip;

            if (fontStyle.HasValue)
                toolItem.Font = App.GetFont(toolItem.Font, null, fontStyle.Value);

            menuItem.ToolItem = toolItem;
            return toolItem;
        }
        /// <summary>
        /// Provede se po kliknutí na prvek menu. Najde data o prvku i cílovou metodu, a vyvolá ji.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnMenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Tag is Tuple<IMenuItem, Action<IMenuItem>> selectedInfo && selectedInfo.Item2 != null)
                selectedInfo.Item2(selectedInfo.Item1);
        }
        #endregion
        #region Grafické prvky
        /// <summary>
        /// Vrátí připravené fungující pero správně namočené do inkoustu dané barvy, šířka pera 1.
        /// Nesmí se Disposovat, jde o obecně používané půjčovací pero.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="color"/> je null
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen GetPen(Color? color, float? alpha = null)
        {
            if (!color.HasValue) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value.GetAlpha(alpha);
            pen.Width = 1f;
            return pen;
        }
        /// <summary>
        /// Vrátí připravené fungující pero dané šířky a správně namočené do inkoustu dané barvy.
        /// Nesmí se Disposovat, jde o obecně používané půjčovací pero.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="color"/> je null
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen GetPen(Color? color, float width, float? alpha = null)
        {
            if (!color.HasValue) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value.GetAlpha(alpha);
            pen.Width = width;
            return pen;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="colorSet"/> je null nebo vrátí null pro daný stav = pak se nemá nic kreslit.
        /// </summary>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Pen GetPen(ColorSet colorSet, Components.InteractiveState interactiveState = Components.InteractiveState.Default, float? width = null, float? alpha = null)
        {
            var color = colorSet?.GetColor(interactiveState);
            if (color == null) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value.GetAlpha(alpha);
            pen.Width = width ?? 1f;
            return pen;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="color"/> je null
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Brush GetBrush(Color? color, float? alpha = null)
        {
            if (!color.HasValue) return null;
            var brush = Current.__Brush;
            brush.Color = color.Value.GetAlpha(alpha);
            return brush;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="colorSet"/> je null nebo vrátí null pro daný stav = pak se nemá nic kreslit.
        /// </summary>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Brush GetBrush(ColorSet colorSet, Components.InteractiveState interactiveState = Components.InteractiveState.Default, float? alpha = null)
        {
            var color = colorSet?.GetColor(interactiveState);
            if (color == null) return null;
            var brush = Current.__Brush;
            brush.Color = color.Value.GetAlpha(alpha);
            return brush;
        }
        /// <summary>
        /// Inicializace grafických prvků
        /// </summary>
        private void _InitGraphics()
        {
            __Pen = new Pen(Color.White, 1f);
            __Brush = new SolidBrush(Color.White);
        }
        /// <summary>
        /// Dispose grafických prvků
        /// </summary>
        private void _DisposeGraphics()
        {
            __Pen.TryDispose();
            __Pen = null;

            __Brush.TryDispose();
            __Brush = null;
        }
        private Pen __Pen;
        private SolidBrush __Brush;
        /// <summary>
        /// Vrátí kurzor daného typu
        /// </summary>
        /// <param name="cursorTypes"></param>
        /// <returns></returns>
        public static Cursor GetCursor(CursorTypes cursorTypes)
        {
            switch (cursorTypes)
            {
                case CursorTypes.Default: return Cursors.Default;
                case CursorTypes.Hand: return Cursors.Hand;
                case CursorTypes.Arrow: return Cursors.Arrow;
                case CursorTypes.Cross: return Cursors.Cross;
                case CursorTypes.IBeam: return Cursors.IBeam;
                case CursorTypes.Help: return Cursors.Help;
                case CursorTypes.AppStarting: return Cursors.AppStarting;
                case CursorTypes.UpArrow: return Cursors.UpArrow;
                case CursorTypes.WaitCursor: return Cursors.WaitCursor;
                case CursorTypes.HSplit: return Cursors.HSplit;
                case CursorTypes.VSplit: return Cursors.VSplit;
                case CursorTypes.NoMove2D: return Cursors.NoMove2D;
                case CursorTypes.NoMoveHoriz: return Cursors.NoMoveHoriz;
                case CursorTypes.NoMoveVert: return Cursors.NoMoveVert;
                case CursorTypes.SizeAll: return Cursors.SizeAll;
                case CursorTypes.SizeNESW: return Cursors.SizeNESW;
                case CursorTypes.SizeNS: return Cursors.SizeNS;
                case CursorTypes.SizeNWSE: return Cursors.SizeNWSE;
                case CursorTypes.SizeWE: return Cursors.SizeWE;
                case CursorTypes.PanEast: return Cursors.PanEast;
                case CursorTypes.PanNE: return Cursors.PanNE;
                case CursorTypes.PanNorth: return Cursors.PanNorth;
                case CursorTypes.PanNW: return Cursors.PanNW;
                case CursorTypes.PanSE: return Cursors.PanSE;
                case CursorTypes.PanSouth: return Cursors.PanSouth;
                case CursorTypes.PanSW: return Cursors.PanSW;
                case CursorTypes.PanWest: return Cursors.PanWest;
                case CursorTypes.No: return Cursors.No;
            }
            return Cursors.Default;
        }
        #endregion
        #region FontLibrary
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public static Font GetFont(FontType? fontType = null, float? emSize = null, FontStyle? fontStyle = null)
        {
            return Current._GetFont(fontType, emSize, fontStyle);
        }
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// Pokud je dodán parametr <paramref name="interactiveState"/>, vyhledá i odpovídající variantu stylu.
        /// </summary>
        /// <param name="textAppearance"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Font GetFont(TextAppearance textAppearance, Components.InteractiveState interactiveState = Components.InteractiveState.Default)
        {
            FontType fontType = textAppearance.FontType ?? FontType.DefaultFont;
            float emSize = textAppearance.TextStyles[interactiveState].EmSize ?? textAppearance.TextStyles[Components.InteractiveState.Default].EmSize ?? GetSystemFont(fontType).Size;
            var sizeRatio = textAppearance.TextStyles[interactiveState].SizeRatio ?? textAppearance.TextStyles[Components.InteractiveState.Default].SizeRatio;
            var fontStyle = textAppearance.TextStyles[interactiveState].FontStyle ?? textAppearance.TextStyles[Components.InteractiveState.Default].FontStyle;

            if (sizeRatio.HasValue) emSize = emSize * sizeRatio.Value;

            return Current._GetFont(fontType, emSize, fontStyle);
        }
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="original">Originální font</param>
        /// <param name="sizeRatio">Poměrná změna velikosti</param>
        /// <param name="fontStyle">Explicitní styl fontu, null = bezez změny</param>
        /// <returns></returns>
        public static Font GetFont(Font original, float? sizeRatio, FontStyle? fontStyle = null)
        {
            string familyName = original.FontFamily.Name;
            float emSize = original.Size;
            if (sizeRatio.HasValue) emSize = emSize * sizeRatio.Value;
            if (!fontStyle.HasValue) fontStyle = original.Style;

            return Current._GetFont(familyName, emSize, fontStyle.Value);
        }
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public Font _GetFont(string familyName, float emSize, FontStyle fontStyle)
        {
            string key = _GetFontKey(familyName, emSize, fontStyle);
            if (!__Fonts.TryGetValue(key, out var font))
            {
                font = new Font(familyName, emSize, fontStyle);
                __Fonts.Add(key, font);
            }
            return font;
        }
        /// <summary>
        /// Najde nebo vytvoří a vrátí požadovaný font
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private Font _GetFont(FontType? fontType, float? emSize, FontStyle? fontStyle)
        {
            if (!fontType.HasValue) fontType = FontType.DefaultFont;
            if (!emSize.HasValue) emSize = GetSystemFont(fontType.Value).Size;
            if (!fontStyle.HasValue) fontStyle = GetSystemFont(fontType.Value).Style;

            string key = _GetFontKey(fontType.Value, emSize.Value, fontStyle.Value);
            if (!__Fonts.TryGetValue(key, out var font))
            {
                font = new Font(GetSystemFont(fontType.Value).FontFamily, emSize.Value, fontStyle.Value);
                __Fonts.Add(key, font);
            }
            return font;
        }
        /// <summary>
        /// Vrátí string klíč pro danou definici fontu. Pod klíčem bude font uložen do <see cref="__Fonts"/>.
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private static string _GetFontKey(FontType fontType, float emSize, FontStyle fontStyle)
        {
            int size = (int)Math.Round(5f * emSize, 0);
            string style = "S" +
                (fontStyle.HasFlag(FontStyle.Bold) ? "B" : "") +
                (fontStyle.HasFlag(FontStyle.Italic) ? "I" : "") +
                (fontStyle.HasFlag(FontStyle.Underline) ? "U" : "") +
                (fontStyle.HasFlag(FontStyle.Strikeout) ? "S" : "");
            return $"T.{fontType}.{size}.{style}";
        }
        /// <summary>
        /// Vrátí string klíč pro danou definici fontu. Pod klíčem bude font uložen do <see cref="__Fonts"/>.
        /// </summary>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private static string _GetFontKey(string familyName, float emSize, FontStyle fontStyle)
        {
            int size = (int)Math.Round(5f * emSize, 0);
            string style = "S" +
                (fontStyle.HasFlag(FontStyle.Bold) ? "B" : "") +
                (fontStyle.HasFlag(FontStyle.Italic) ? "I" : "") +
                (fontStyle.HasFlag(FontStyle.Underline) ? "U" : "") +
                (fontStyle.HasFlag(FontStyle.Strikeout) ? "S" : "");
            return $"N.{familyName}.{size}.{style}";
        }
        /// <summary>
        /// Vrátí systémový font, např. <see cref="SystemFonts.DefaultFont"/> pro daný typ <paramref name="fontType"/>
        /// </summary>
        /// <param name="fontType"></param>
        /// <returns></returns>
        public static Font GetSystemFont(FontType fontType)
        {
            switch (fontType)
            {
                case FontType.DefaultFont: return SystemFonts.DefaultFont;
                case FontType.DialogFont: return SystemFonts.DialogFont;
                case FontType.MessageBoxFont: return SystemFonts.MessageBoxFont;
                case FontType.CaptionFont: return SystemFonts.CaptionFont;
                case FontType.SmallCaptionFont: return SystemFonts.SmallCaptionFont;
                case FontType.MenuFont: return SystemFonts.MenuFont;
                case FontType.StatusFont: return SystemFonts.StatusFont;
                case FontType.IconTitleFont: return SystemFonts.IconTitleFont;
            }
            return SystemFonts.DefaultFont;
        }
        /// <summary>
        /// Systémové typy fontů
        /// </summary>
        public enum FontType
        {
            DefaultFont,
            DialogFont,
            MessageBoxFont,
            CaptionFont,
            SmallCaptionFont,
            MenuFont,
            StatusFont,
            IconTitleFont
        }
        /// <summary>
        /// Inicializace fontů
        /// </summary>
        private void _InitFonts()
        {
            __Fonts = new Dictionary<string, Font>();
        }
        /// <summary>
        /// Dispose fontů
        /// </summary>
        private void _DisposeFonts()
        {
            __Fonts.Values.ForEachExec(f => f.TryDispose());
            __Fonts = null;
        }
        private Dictionary<string, Font> __Fonts;
        #endregion
        #region ImageLibrary
        /// <summary>
        /// Najde a vrátí Image načtený z dodaného souboru.
        /// Image se nesmí měnit ani Disposovat, používá se opakovaně.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Image GetImage(string fileName)
        {
            return Current._GetImage(fileName, null);
        }
        /// <summary>
        /// Najde a vrátí Image načtený z dodaného obsahu.
        /// Image se nesmí měnit ani Disposovat, používá se opakovaně.
        /// <para/>
        /// Dodaný <paramref name="imageName"/> nesmí být prázdný - používá se jako jednoznačný klíč pro Image, pod ním je uložen v interní paměti aplikace!
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Image GetImage(string imageName, byte[] content)
        {
            return Current._GetImage(imageName, content);
        }
        /// <summary>
        /// Najde / vytvoří a vrátí Image z dané definice.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private Image _GetImage(string fileName, byte[] content)
        {
            if (String.IsNullOrEmpty(fileName)) return null;
            string type = (content is null ? "File" : "Data");
            string key = _GetImageKey(type, fileName);
            if (!__Images.TryGetValue(key, out Image image))
            {
                try
                {
                    if (content != null)
                    {   // Z obsahu:
                        using (var stream = new System.IO.MemoryStream(content))
                            image = Image.FromStream(stream);
                    }
                    else if (System.IO.File.Exists(fileName))
                    {   // Ze souboru:
                        image = Image.FromFile(fileName);
                    }
                }
                catch (Exception) { image = null; }
                __Images.Add(key, image);
            }
            return image;
        }
        /// <summary>
        /// Vrátí klíč pro Image
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string _GetImageKey(string type, string name)
        {
            name = name.Trim().ToLower().Replace("\\", "/");
            return $"{type}>{name}";
        }
        /// <summary>
        /// Inicializace Images
        /// </summary>
        private void _InitImages()
        {
            __Images = new Dictionary<string, Image>();
        }
        /// <summary>
        /// Dispose Images
        /// </summary>
        private void _DisposeImages()
        {
            __Images.Values.ForEachExec(f => f.TryDispose());
            __Images = null;
        }
        private Dictionary<string, Image> __Images;
        #endregion
        #region Vzhled a Layout
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta); lze změnit, po změně dojde eventu <see cref="CurrentAppearanceChanged"/>.
        /// Pozor, tato property není propojena s <see cref="Settings.AppearanceName"/>, toto propojení musí zajistit aplikace.
        /// Důvodem je vhodné načasování zaháčkování a provedení eventu po změně / inicializaci.
        /// </summary>
        public static AppearanceInfo CurrentAppearance { get { return Current._CurrentAppearance; } set { Current._CurrentAppearance = value; } }
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta); řeší autoinicializaci i hlídání změny a vyvolání eventu
        /// </summary>
        private AppearanceInfo _CurrentAppearance
        {
            get
            {
                if (__CurrentAppearance is null)
                    __CurrentAppearance = AppearanceInfo.Default;
                return __CurrentAppearance;
            }
            set
            {
                if (value is null) return;
                bool isChange = (__CurrentAppearance is null || !Object.ReferenceEquals(value, __CurrentAppearance));
                __CurrentAppearance = value;
                if (isChange) __CurrentAppearanceChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta), proměnná
        /// </summary>
        private AppearanceInfo __CurrentAppearance;
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        public static event EventHandler CurrentAppearanceChanged { add { Current.__CurrentAppearanceChanged += value; } remove { Current.__CurrentAppearanceChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        private event EventHandler __CurrentAppearanceChanged;

        /// <summary>
        /// Aktuální sada definující layout; lze změnit, po změně dojde eventu <see cref="CurrentLayoutSetChanged"/>.
        /// Pozor, tato property není propojena s <see cref="Settings.LayoutSetName"/>, toto propojení musí zajistit aplikace.
        /// Důvodem je vhodné načasování zaháčkování a provedení eventu po změně / inicializaci.
        /// </summary>
        public static ItemLayoutSet CurrentLayoutSet { get { return Current._CurrentLayoutSet; } set { Current._CurrentLayoutSet = value; } }
        /// <summary>
        /// Aktuální sada definující layout; řeší autoinicializaci i hlídání změny a vyvolání eventu
        /// </summary>
        private ItemLayoutSet _CurrentLayoutSet
        {
            get
            {
                if (__CurrentLayoutSet is null)
                    __CurrentLayoutSet = ItemLayoutSet.Default;
                return __CurrentLayoutSet;
            }
            set
            {
                if (value is null) return;
                bool isChange = (__CurrentLayoutSet is null || !Object.ReferenceEquals(value, __CurrentLayoutSet));
                __CurrentLayoutSet = value;
                if (isChange) __CurrentLayoutSetChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aktuální sada definující layout, proměnná
        /// </summary>
        private ItemLayoutSet __CurrentLayoutSet;
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        public static event EventHandler CurrentLayoutSetChanged { add { Current.__CurrentLayoutSetChanged += value; } remove { Current.__CurrentLayoutSetChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        private event EventHandler __CurrentLayoutSetChanged;
        #endregion
        #region MainForm a StatusBar a StatusImage
        /// <summary>
        /// Main okno aplikace. Slouží jako Owner pro Dialog okna a další Child okna.
        /// </summary>
        public static MainForm MainForm { get { return Current._MainForm; } set { Current._MainForm = value; } }
        /// <summary>
        /// Main okno aplikace, aktivní instanční property.
        /// Změna okna mění stav aplikace <see cref="_ApplicationState"/>.
        /// </summary>
        private MainForm _MainForm
        { 
            get { return __MainForm; }
            set 
            {
                var oldForm = __MainForm;
                if (oldForm != null)
                {
                    oldForm.FirstShown -= _MainFormFirstShown;
                    oldForm.FormClosed -= _MainFormClosed;
                }
                __MainForm = null;

                var newForm = value;
                if (newForm != null)
                {
                    newForm.FirstShown += _MainFormFirstShown;
                    newForm.FormClosed += _MainFormClosed;
                }
                __MainForm = newForm;

                // Stav aplikace:
                if (newForm != null)
                {   // Podle stavu okna:
                    bool formIsShown = newForm.IsShown;
                    _ApplicationState = (!formIsShown ? ApplicationState.Starting : ApplicationState.Running);
                }
                else
                {   // Když někdo nasetuje MainForm = null, pak je konec:
                    _ApplicationState = ApplicationState.Exited;
                }
            }
        }
        private MainForm __MainForm;
        /// <summary>
        /// Stav aplikace
        /// </summary>
        public static ApplicationState ApplicationState { get { return Current._ApplicationState; } }
        /// <summary>
        /// Stav aplikace, aktivní instanční property
        /// </summary>
        private ApplicationState _ApplicationState 
        {
            get { return __ApplicationState; }
            set
            {
                if (value != __ApplicationState)
                {
                    __ApplicationState = value;
                    __ApplicationStateChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Stav aplikace, proměnná
        /// </summary>
        private ApplicationState __ApplicationState;
        /// <summary>
        /// Událost po změně stavu aplikace. Sender předávaný do handleru je null.
        /// </summary>
        public static event EventHandler ApplicationStateChanged { add { Current.__ApplicationStateChanged += value; } remove { Current.__ApplicationStateChanged -= value; } }
        private event EventHandler __ApplicationStateChanged;
        /// <summary>
        /// Poté, kdy MainForm provede FirstShown, nastavíme Stav aplikace = Running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainFormFirstShown(object sender, EventArgs e)
        {
            _ApplicationState = ApplicationState.Running;
        }
        /// <summary>
        /// Poté, kdy MainForm provede Closed, nastavíme Stav aplikace = Exited
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainFormClosed(object sender, FormClosedEventArgs e)
        {
            _ApplicationState = ApplicationState.Exited;
        }
        /// <summary>
        /// Spustí danou metodu v GUI vlákně, které garantuje <see cref="MainForm"/>.
        /// Pokud <see cref="MainForm"/> není dán anebo není invokování třeba, spustí metodu rovnou.
        /// </summary>
        /// <param name="action"></param>
        public static void RunInGuiThread(Action action) { Current._RunInGuiThread(action); }
        /// <summary>
        /// Spustí danou metodu v GUI vlákně, které garantuje <see cref="MainForm"/>.
        /// Pokud <see cref="MainForm"/> není dán anebo není invokování třeba, spustí metodu rovnou.
        /// </summary>
        /// <param name="action"></param>
        private void _RunInGuiThread(Action action)
        {
            if (action is null) return;
            var mainForm = _MainForm;
            if (mainForm != null && mainForm.IsHandleCreated && mainForm.InvokeRequired)
                mainForm.Invoke(action);
            else
                action();
        }

        /// <summary>
        /// Vrátí image daného typu do PopupMenu nebo do StatusBar
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Image GetStatusImage(ImageKindType value)
        {
            switch (value)
            {
                case ImageKindType.None: return null;
                case ImageKindType.Other: return null;
                case ImageKindType.Delete: return Properties.Resources.delete_22;
                case ImageKindType.DocumentNew: return Properties.Resources.document_new_3_22;
                case ImageKindType.DocumentPreview: return Properties.Resources.document_preview_22;
                case ImageKindType.DocumentProperties: return Properties.Resources.document_properties_22;
                case ImageKindType.DocumentEdit: return Properties.Resources.edit_3_22;
                case ImageKindType.EditCopy: return Properties.Resources.edit_copy_3_22;
                case ImageKindType.EditCut: return Properties.Resources.edit_cut_3_22;
                case ImageKindType.EditPaste: return Properties.Resources.edit_paste_3_22;
                case ImageKindType.EditRemove: return Properties.Resources.edit_remove_3_22;
                case ImageKindType.EditSelect: return Properties.Resources.edit_select_22;
                case ImageKindType.EditRows: return Properties.Resources.edit_select_all_3_22;
                case ImageKindType.FormatJustify: return Properties.Resources.format_justify_left_4_22;
                case ImageKindType.Home: return Properties.Resources.go_home_4_22;
                case ImageKindType.Help: return Properties.Resources.help_3_22;
                case ImageKindType.Hint: return Properties.Resources.help_hint_22;
                case ImageKindType.MediaPlay: return Properties.Resources.media_playback_start_3_22;
                case ImageKindType.MediaForward: return Properties.Resources.media_seek_forward_3_22;
            }
            return null;
        }
        #endregion
        #region Messages a texty
        /// <summary>
        /// Zobrazí standardní Message. Zadáním ikony lze definovat titulek okna.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="icon"></param>
        /// <param name="title"></param>
        public static void ShowMessage(string text, MessageBoxIcon icon = MessageBoxIcon.Information, string title = null)
        {
            if (title is null) title = _GetMessageBoxTitle(icon);
            System.Windows.Forms.MessageBox.Show(MainForm, text, title, MessageBoxButtons.OK, icon);
        }
        /// <summary>
        /// Vrátí defaultní titulek MessageBox okna podle dané ikony.
        /// Pro ikonu typu <see cref="MessageBoxIcon.Question"/> přihlédne k parametru <paramref name="isQuestion"/>.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="isQuestion"></param>
        /// <returns></returns>
        private static string _GetMessageBoxTitle(MessageBoxIcon icon, bool isQuestion = false)
        {
            switch (icon)
            {
                case MessageBoxIcon.None: return "Poznámka";
                case MessageBoxIcon.Stop: return "Chyba";
                case MessageBoxIcon.Question: return (isQuestion ? "Dotaz" : "Podivné");
                case MessageBoxIcon.Exclamation: return "Varování";
                case MessageBoxIcon.Asterisk: return "Informace";
            }
            return "Zpráva";
        }
        /// <summary>
        /// Vrátí text odpovídající počtu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="textAll">Texty pro všechny počty, oddělené znakem ×</param>
        /// <returns></returns>
        public static string GetCountText(int count, string textAll)
        {
            if (String.IsNullOrEmpty(textAll)) return "";
            var texts = textAll.Split('×');
            if (texts.Length < 4) return "";
            return GetCountText(count, texts[0], texts[1], texts[2], texts[3]);
        }
        /// <summary>
        /// Vrátí text odpovídající počtu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="textZero"></param>
        /// <param name="textOne"></param>
        /// <param name="textSmall"></param>
        /// <param name="textMany"></param>
        /// <returns></returns>
        public static string GetCountText(int count, string textZero, string textOne, string textSmall, string textMany)
        {
            if (count <= 0) return textZero;
            if (count == 1) return count.ToString() + " " + textOne;
            if (count <= 4) return count.ToString() + " " + textSmall;
            return count.ToString() + " " + textMany;
        }
        #endregion
    }

    #region Podpůrné třídy (StatusInfo) a enumy
    /// <summary>
    /// Třída obsahující logická data reprezentovaná v jednom prvku Status labelu
    /// </summary>
    public class StatusInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="statusLabel"></param>
        public StatusInfo(ToolStripStatusLabel statusLabel)
        {
            __StatusLabel = statusLabel;
        }
        /// <summary>
        /// Instance controlu <see cref="ToolStripStatusLabel"/>
        /// </summary>
        private ToolStripStatusLabel __StatusLabel;
        /// <summary>
        /// Typ obrázku
        /// </summary>
        private ImageKindType __ImageKind;
        /// <summary>
        /// Text ve Statusbaru
        /// </summary>
        public string Text { get { return __StatusLabel.Text; } set { __StatusLabel.Text = value; } }
        /// <summary>
        /// Typ obrázku ve Statusbaru
        /// </summary>
        public ImageKindType ImageKind { get { return __ImageKind; } set { __ImageKind = value; __StatusLabel.Image = App.GetStatusImage(value); } }
        /// <summary>
        /// Fyzický obrázek ve Statusbaru
        /// </summary>
        public Image Image { get { return __StatusLabel.Image; } set { __ImageKind = (value is null ? ImageKindType.None : ImageKindType.Other); __StatusLabel.Image = value; } }
    }

    /// <summary>
    /// Třída zajišťující služby pro SingleProcess
    /// </summary>
    public static class SingleProcess
    {
        /// <summary>
        /// hwnd reprezentující cílové okno = Broadcast (pro všechna okna)
        /// </summary>
        public static IntPtr HWND_BROADCAST { get { return new IntPtr(0xffff); } }
        /// <summary>
        /// Výsledek v message <see cref="WM_SHOWME"/>, když jej detekuje a zpracuje konkrétní okno aplikace
        /// </summary>
        public const int RESULT_VALID = 0x1ae0;
        /// <summary>
        /// Jméno mutexu
        /// </summary>
        public const string MutexName = "Applications.DjSoft.ProgramLauncher.Mutex";
        /// <summary>
        /// Jméno Message ShowMessage
        /// </summary>
        public const string ShowMeWmMessageName = "Applications.DjSoft.ProgramLauncher.ShowMessage";
        /// <summary>
        /// Jméno IPC Pipe
        /// </summary>
        public const string IpcPipeName = "Applications.DjSoft.ProgramLauncher.IpcPipe";
        /// <summary>
        /// Text zprávy IPC Pipe pro aktivaci okna Singleton aplikace
        /// </summary>
        public const string IpcPipeShowMainFormRequest = "Applications.DjSoft.ProgramLauncher.ShowMainForm";
        /// <summary>
        /// Text odpovědi IPC Pipe potvrzující aktivaci okna Singleton aplikace
        /// </summary>
        public const string IpcPipeResponseOK = "Applications.DjSoft.ProgramLauncher.OK";
        /// <summary>
        /// Int obsahující systémové (Windows) číslo zprávy 
        /// </summary>
        public static int WM_SHOWME
        {
            get
            {
                if (!__WM_ShowMe.HasValue)
                    __WM_ShowMe = RegisterWindowMessage(ShowMeWmMessageName);
                return __WM_ShowMe.Value;
            }
        }
        private static int? __WM_ShowMe = null;
        /// <summary>
        /// Zajistí odeslání zdejší specifické zprávy <see cref="ShowMeWmMessageName"/> do všech oken všech procesů.
        /// Cílové okno musí řešit svoji metodu <see cref="Control.WndProc(ref Message)"/>, 
        /// a v jejím těle má testovat příchozí zprávu pomocí zdejší metody <see cref="IsShowMeWmMessage(Message)"/>.
        /// </summary>
        /// <param name="hWnd"></param>
        public static void SendShowMeWmMessage(IntPtr? hWnd = null)
        {
            IntPtr targetWnd = hWnd ?? HWND_BROADCAST;
            PostMessage(targetWnd, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
            // var result = SendMessage(targetWnd, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
        }
        /// <summary>
        /// Metoda vrátí true, pokud přijatá Message je zpráva typu ShowMe a aktuální okno se má na jejím základě reaktivovat.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool IsShowMeWmMessage(ref System.Windows.Forms.Message m, Form form = null)
        {
            if (m.Msg != WM_SHOWME) return false;

            if (form != null)
            {
                form.Show();
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                form.Activate();
            }

            return true;
        }
        [DllImport("user32")]
        private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32")]
        private static extern string SendMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32")]
        private static extern int RegisterWindowMessage(string message);

    }
    /// <summary>
    /// Typ ikony
    /// </summary>
    public enum ImageKindType
    {
        None,
        Other,
        Delete,
        DocumentNew,
        DocumentPreview,
        DocumentProperties,
        DocumentEdit,
        EditCopy,
        EditCut,
        EditPaste,
        EditRemove,
        EditSelect,
        EditRows,
        FormatJustify,
        Home,
        Help,
        Hint,
        MediaPlay,
        MediaForward
    }
    /// <summary>
    /// Typ kurzoru. 
    /// Fyzický kurzor pro konkrétní typ vrátí <see cref="App.GetCursor(CursorTypes)"/>.
    /// </summary>
    public enum CursorTypes
    {
        Default,
        Hand,
        Arrow,
        Cross,
        IBeam,
        Help,
        AppStarting,
        UpArrow,
        WaitCursor,
        HSplit,
        VSplit,
        NoMove2D,
        NoMoveHoriz,
        NoMoveVert,
        SizeAll,
        SizeNESW,
        SizeNS,
        SizeNWSE,
        SizeWE,
        PanEast,
        PanNE,
        PanNorth,
        PanNW,
        PanSE,
        PanSouth,
        PanSW,
        PanWest,
        No
    }
    /// <summary>
    /// Stav aplikace
    /// </summary>
    public enum ApplicationState
    {
        None,
        Starting,
        Running,
        Closing,
        Exited
    }
    #endregion
}
