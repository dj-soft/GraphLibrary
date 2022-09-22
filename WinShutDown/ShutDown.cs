using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Support.WinShutDown
{
    public class ShutDown
    {
        #region Konstruktor, public rozhraní a eventy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ShutDown()
        {
            Running = false;
            Cancelled = false;
            ShutDownStatus = ShutDownStatusType.None;
            ShowStepTime = TimeSpan.FromMilliseconds(200d);
            StatusColor = Color.Wheat;
            StatusInfo = "";
            TimeInfo = "";
            _LastStepTime = null;
            _ResetTimer = new System.Threading.AutoResetEvent(false);
            _InactiveSpeedTreshold = 2d;
        }
        /// <summary>
        /// Start se zadanými parametry
        /// </summary>
        /// <param name="timeMode"></param>
        /// <param name="timeValue"></param>
        /// <param name="action"></param>
        public void Start(TimeModeType timeMode, string timeValue, ActionType action)
        {
            if (Running)
            {
                CallShowDone();
                return;
            }

            Running = true;
            Cancelled = false;
            ShutDownStatus = ShutDownStatusType.BeforeWaiting;
            StatusColor = Color.Wheat;
            StatusInfo = "";
            TimeInfo = "";
            TimeMode = timeMode;
            TimeValue = timeValue;
            Action = action;

            Task.Factory.StartNew(RunWorking);
        }
        /// <summary>
        /// Požádá o storno procesu vypnutí, lze za jakéhokoli stavu <see cref="ShutDownStatus"/> kromě <see cref="ShutDownStatusType.ShutDownInProgress"/>.
        /// </summary>
        public void Cancel()
        {
            if (ShutDownStatus != ShutDownStatusType.ShutDownInProgress)
            {
                Cancelled = true;
                _ResetTimer.Set();
            }
        }
        /// <summary>
        /// Vyčistí instanci
        /// </summary>
        public void Clear()
        {
            Running = false;
            Cancelled = true;
            ShowStep = null;
            ShutDownStatusChanged = null;
            ShowDone = null;
        }
        /// <summary>
        /// Akce běží
        /// </summary>
        public bool Running { get; private set; }
        /// <summary>
        /// Akce je stornována
        /// </summary>
        public bool Cancelled { get; private set; }
        /// <summary>
        /// Stav procesu vypnutí, odpovídá barvě
        /// </summary>
        public ShutDownStatusType ShutDownStatus { get; private set; }
        /// <summary>
        /// Režim časomíry
        /// </summary>
        public TimeModeType TimeMode { get; private set; }
        /// <summary>
        /// Požadovaný čas akce, ve spojitosti s režimem <see cref="TimeMode"/>
        /// </summary>
        public string TimeValue { get; private set; }
        /// <summary>
        /// Požadovaná akce
        /// </summary>
        public ActionType Action { get; private set; }
        /// <summary>
        /// Text do stavové informace
        /// </summary>
        public string StatusInfo { get; private set; }
        /// <summary>
        /// Pouze text zbývajícího času
        /// </summary>
        public string TimeInfo { get; private set; }
        /// <summary>
        /// Barva stavové informace
        /// </summary>
        public Color StatusColor { get; private set; }
        /// <summary>
        /// Čas, po které se opakovaně volá event <see cref="ShowStep"/>. Nula a záporný = nevolá se.
        /// Výchozí = 0.2 sec
        /// </summary>
        public TimeSpan ShowStepTime { get; set; }
        /// <summary>
        /// Vyvolá event <see cref="ShowStep"/>, pokud je to povinné anebo pokud od posledního volání uplynul potřebný čas <see cref="ShowStepTime"/>.
        /// </summary>
        /// <param name="force"></param>
        private void CallShowStep(bool force)
        {
            bool doCall = (force || !_LastStepTime.HasValue);
            if (!doCall && _LastStepTime.HasValue && ShowStepTime.Ticks > 0L && (DateTime.Now - _LastStepTime.Value) >= ShowStepTime)
                doCall = true;

            if (doCall)
            {
                CallShowStep();
                _LastStepTime = DateTime.Now;
            }
        }
        /// <summary>
        /// Vyvolá event <see cref="ShowStep"/>
        /// </summary>
        private void CallShowStep() { ShowStep?.Invoke(this, EventArgs.Empty); }
        /// <summary>
        /// Vyvolá event <see cref="ShutDownStatusChanged"/>
        /// </summary>
        private void CallShutDownStatusChanged() { ShutDownStatusChanged?.Invoke(this, EventArgs.Empty); }
        /// <summary>
        /// Vyvolá event <see cref="ShowDone"/>
        /// </summary>
        private void CallShowDone() { ShowDone?.Invoke(this, EventArgs.Empty); }
        /// <summary>
        /// Událost volaná po každé změně stavové informace
        /// </summary>
        public event EventHandler ShowStep;
        /// <summary>
        /// Událost volaná po změně stavu v <see cref="ShutDownStatus"/>
        /// </summary>
        public event EventHandler ShutDownStatusChanged;
        /// <summary>
        /// Událost volaná po konci aktivity (po Cancel i po doběhnutí a vypnutí).
        /// </summary>
        public event EventHandler ShowDone;
        /// <summary>
        /// Čas posledního volání eventu <see cref="ShowStep"/>
        /// </summary>
        private DateTime? _LastStepTime;
        /// <summary>
        /// Časovač pro řízení hlavní smyčky výkonného vlákna
        /// </summary>
        private System.Threading.AutoResetEvent _ResetTimer;
        #endregion
        #region Řídící vrstva ve Working threadu
        /// <summary>
        /// Výkonná metoda na pozadí, kompletní smyčka
        /// </summary>
        private void RunWorking()
        {
            PrepareForWork();
            _ResetTimer.Reset();
            bool force = true;
            while (true)
            {
                _ResetTimer.WaitOne(100);
                if (Cancelled) break;
                if (IsValidStateForAction())
                {
                    DoAction();
                    break;
                }
                CallShowStep(force);
                force = false;
            }
            DoStop();
            CallShowStep(true);
            CallShowDone();
        }
        /// <summary>
        /// Připraví data před zahájením smyčky WorkThread
        /// </summary>
        private void PrepareForWork()
        {
            switch (this.TimeMode)
            {
                case TimeModeType.AfterTime:
                case TimeModeType.AtTime:
                case TimeModeType.Now:
                    PrepareTimeForWork();
                    break;
                case TimeModeType.Inactivity:
                    PrepareInactivityForWork();
                    break;
            }
        }
        /// <summary>
        /// Vrátí true, pokud je vhodný čas k provedení Shutdown akce
        /// </summary>
        /// <returns></returns>
        private bool IsValidStateForAction()
        {
            switch (this.TimeMode)
            {
                case TimeModeType.AfterTime:
                case TimeModeType.AtTime:
                case TimeModeType.Now:
                    return IsRemainingTimeForAction();
                case TimeModeType.Inactivity:
                    return IsInactivityStateForAction();
            }
            return (!Cancelled && this.ShutDownStatus == ShutDownStatusType.ShutDownInProgress);
        }
        /// <summary>
        /// Ukončuje práci
        /// </summary>
        private void DoStop()
        {
            switch (this.TimeMode)
            {
                case TimeModeType.Inactivity:
                    StopInactivityWork();
                    break;
            }
        }
        #endregion
        #region ShutDown podle daného času
        /// <summary>
        /// Připraví a uloží si čas, kdy by měla být provedena akce na základě <see cref="TimeMode"/> a <see cref="TimeValue"/>.
        /// </summary>
        /// <returns></returns>
        private void PrepareTimeForWork()
        {
            var now = DateTime.Now;
            TimeSpan timeSpan;
            switch (TimeMode)
            {
                case TimeModeType.AfterTime:
                    timeSpan = GetTimeSpan(TimeValue, TimeSpan.FromMinutes(60));
                    _ActionTime = now.Add(timeSpan);
                    break;
                case TimeModeType.AtTime:
                    timeSpan = GetTimeSpan(TimeValue, now.AddMinutes(60).TimeOfDay);
                    _ActionTime = ((timeSpan > now.TimeOfDay) ? now.Date.Add(timeSpan) : now.Date.AddDays(1d).Add(timeSpan));
                    break;
                case TimeModeType.Now:
                    timeSpan = TimeSpan.FromSeconds(30d);
                    _ActionTime = now.Add(timeSpan);
                    break;
            }
        }
        /// <summary>
        /// Ze stringu typicky "02:30" vrací TimeSpan obsahující 2 hodiny + 30 minut. Pokud string neobsahuje nic, vrací <paramref name="defaultTime"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultTime"></param>
        /// <returns></returns>
        private TimeSpan GetTimeSpan(string text, TimeSpan defaultTime)
        {
            if (String.IsNullOrEmpty(text)) return defaultTime;
            text = text.Replace(" ", "");
            var parts = text.Split(':');
            if (parts.Length == 0) return defaultTime;
            if (parts.Length == 1)
            {
                bool has0 = Int32.TryParse(parts[0], out int m);
                if (!has0  || m <= 0) return defaultTime;
                return TimeSpan.FromMinutes(m);
            }
            else
            {
                bool has0 = Int32.TryParse(parts[0], out int h);
                bool has1 = Int32.TryParse(parts[1], out int m);
                if (h <= 0 && m <= 0) return defaultTime;
                if (!has0 || h < 0) return TimeSpan.FromMinutes(m);
                if (!has1 || m < 0) return TimeSpan.FromHours(h);
                m = (60 * h) + m;
                return TimeSpan.FromMinutes(m);
            }
        }
        /// <summary>
        /// Vrátí true, pokud je čas vhodný pro finální akci
        /// </summary>
        /// <returns></returns>
        private bool IsRemainingTimeForAction()
        {
            if (!_ActionTime.HasValue) return false;

            TimeSpan timeRemaining = _ActionTime.Value - DateTime.Now;
            ShutDownStatusType shutDownStatus = GetStatusForRemainingTime(timeRemaining);
            bool isChangeStatus = (shutDownStatus != ShutDownStatus);
            this.ShutDownStatus = shutDownStatus;
            this.SetInfoForStatus(shutDownStatus, timeRemaining, true);
            if (isChangeStatus) CallShutDownStatusChanged();
            return (timeRemaining.Ticks <= 0L && !Cancelled);
        }
        /// <summary>
        /// Čas provedení akce
        /// </summary>
        private DateTime? _ActionTime;
        #endregion
        #region Shutdown podle neaktivity
        /// <summary>
        /// Připraví stav pro sledování neaktivity
        /// </summary>
        private void PrepareInactivityForWork()
        {
            _NetMonitor = new NetworkMonitor(true, 250d);
            _NetStartTime = DateTime.Now;
        }
        /// <summary>
        /// Vrátí true, pokud je stav aktivity vhodný pro finální akci
        /// </summary>
        /// <returns></returns>
        private bool IsInactivityStateForAction()
        {
            ShutDownStatusType shutDownStatus = GetStatusForCurrentActivity(out var timeRemaining);
            bool isChangeStatus = (shutDownStatus != ShutDownStatus);
            this.ShutDownStatus = shutDownStatus;
            if (isChangeStatus) CallShutDownStatusChanged();
            this.SetInfoForStatus(shutDownStatus, timeRemaining, false);
            return (shutDownStatus == ShutDownStatusType.ShutDownInProgress && !Cancelled);
        }
        private ShutDownStatusType GetStatusForCurrentActivity(out TimeSpan? timeRemaining)
        {
            timeRemaining = null;

            ShutDownStatusType currDownStatus = this.ShutDownStatus;
            ShutDownStatusType shutDownStatus = currDownStatus;
            var now = DateTime.Now;
            if (!_NetStartTime.HasValue) _NetStartTime = now;
            double timeSampleLong = InactiveTimeLong;
            double timeSampleLast = InactiveTimeLast;
            double speed = 0d;

            if (((TimeSpan)(now - _NetStartTime.Value)).TotalSeconds <= timeSampleLong)
            {   // Prvních 15 sekund po zahájení musíme být ve stavu BeforeWaiting - a to bez ohledu na aktuální rychlost;
                shutDownStatus = ShutDownStatusType.BeforeWaiting;
                speed = _NetMonitor.GetMaxSpeedKBps(timeSampleLast);           // Krátkodobá rychlost sítě, jen pro uživatele
            }
            else
            {   // Poté už můžeme vyhodnotit NetSpeed:
                var speedLong = _NetMonitor.GetMaxSpeedKBps(timeSampleLong);   // Rychlost průměrná za posledních 15 sekund
                var speedLast = _NetMonitor.GetMaxSpeedKBps(timeSampleLast);   // Rychlost průměrná za poslední 3 sekundy
                speed = (speedLong > speedLast ? speedLong : speedLast);       // Beru tu vyšší
                bool isSilentio = (speed < InactiveSpeedTreshold);             // "Ticho" je, když vyšší rychlost je menší než 2KBps
                if (isSilentio)
                {   // Posledních 15 sekund i poslední 3 sekundy je rychlost sítě pod 2KBps:
                    if (!_NetSilentioTargetTime.HasValue)
                    {   // Nyní začíná čas Silentio (dosud nebyl určen čas Target):
                        _NetSilentioTargetTime = now.AddSeconds(InactiveTimeDuration);   // Od teď za 
                        shutDownStatus = ShutDownStatusType.Waiting;
                    }
                    else
                    {   // Čas Silentio už běží = zde vyhodnocujeme jeho dobu a dosavadní status (currDownStatus):
                        TimeSpan silentioTime = _NetSilentioTargetTime.Value - now;
                        shutDownStatus = GetStatusForRemainingTime(silentioTime);
                        timeRemaining = silentioTime;
                    }
                }
                else
                {   // Síťový provoz je aktivní:
                    _NetSilentioTargetTime = null;
                    shutDownStatus = ShutDownStatusType.BeforeWaiting;
                }
            }

            string speedText = speed.ToString("### ##0.00").Trim();
            this.StatusInfo = $"Rychlost sítě: {speedText} KBps";

            return shutDownStatus;
        }
        /// <summary>
        /// Ukončuje práci v režimu Inactivity
        /// </summary>
        private void StopInactivityWork()
        {
            _NetMonitor?.Stop();
            _NetMonitor = null;
        }
        /// <summary>
        /// 15 sekund = dlouhodobý průměr (měření aktivity sítě)
        /// </summary>
        private static double InactiveTimeLong { get { return 15d; } }
        /// <summary>
        /// 3 sekundy = krátkodobý průměr (měření aktivity sítě)
        /// </summary>
        private static double InactiveTimeLast { get { return 3d; } }
        /// <summary>
        /// 120 sekund = Počet sekund, po které musí být síťový provoz pod rychlost <see cref="InactiveSpeedTreshold"/>, aby došlo k akci.
        /// Krátký čas: případný výpadek zdroje při dlouhodobém stahování se bude tvářit jako "konec stahování" a my vypneme stroj.
        /// Dlouhý čas: bezpečnější.
        /// </summary>
        private static double InactiveTimeDuration { get { return 120d; } }
        /// <summary>
        /// Měření síťového provozu
        /// </summary>
        private NetworkMonitor _NetMonitor;
        /// <summary>
        /// Počáteční čas měření sítě
        /// </summary>
        private DateTime? _NetStartTime;
        /// <summary>
        /// Koncový čas stavu Silentio: určí se při jeho počátku jako Now + <see cref="InactiveTimeDuration"/> = 300 sekund = 5 minut
        /// </summary>
        private DateTime? _NetSilentioTargetTime;
        /// <summary>
        /// Rychlost sítě v KBps, kterou tolerujeme jako "šum na pozadí", a která pro nás znaměná "nic se neděje".
        /// Výchozí = 2 KB/sec, lze setovat hodnotu v rozmezí 1 až 300 KB/sec.
        /// </summary>
        public double InactiveSpeedTreshold { get { return _InactiveSpeedTreshold; } set { _InactiveSpeedTreshold = (value < 1d ? 1d : (value > 300d ? 300d : value)); } } private double _InactiveSpeedTreshold;
        #endregion
        #region Konverze zbývajícího času na status, barvu atd
        /// <summary>
        /// Vrátí status pro daný zbývající čas
        /// </summary>
        /// <param name="timeRemaining"></param>
        /// <returns></returns>
        private ShutDownStatusType GetStatusForRemainingTime(TimeSpan timeRemaining)
        {
            var seconds = timeRemaining.TotalSeconds;
            if (seconds >= 300d) return ShutDownStatusType.BeforeWaiting;
            if (seconds >= 60d) return ShutDownStatusType.Waiting;
            if (seconds >= 10d) return ShutDownStatusType.Preparing;
            if (seconds > 0d) return ShutDownStatusType.ShutDown;
            return ShutDownStatusType.ShutDownInProgress;
        }
        /// <summary>
        /// Nastaví texty a barvy pro daný stav a zbývající čas
        /// </summary>
        /// <param name="setStatusInfo"></param>
        /// <param name="timeRemaining"></param>
        /// <param name="shutDownStatus"></param>
        private void SetInfoForStatus(ShutDownStatusType shutDownStatus, TimeSpan? timeRemaining, bool setStatusInfo)
        {
            this.StatusColor = GetColorForStatus(this.ShutDownStatus);
            if (timeRemaining.HasValue && setStatusInfo) this.StatusInfo = GetTextForRemainingTime(this.ShutDownStatus, timeRemaining.Value);
            this.TimeInfo = (timeRemaining.HasValue ? GetTimeForRemainingTime(this.ShutDownStatus, timeRemaining.Value) : "");
        }
        /// <summary>
        /// Vrátí barvu podle daného stavu
        /// </summary>
        /// <param name="shutDownStatus"></param>
        /// <returns></returns>
        private Color GetColorForStatus(ShutDownStatusType shutDownStatus)
        {
            switch (shutDownStatus)
            {
                case ShutDownStatusType.None: return Color.WhiteSmoke;
                case ShutDownStatusType.BeforeWaiting: return Color.LightGreen;
                case ShutDownStatusType.Waiting: return Color.LightBlue;
                case ShutDownStatusType.Preparing: return Color.LightBlue;
                case ShutDownStatusType.ShutDown: return Color.LightCoral;
                case ShutDownStatusType.ShutDownInProgress: return Color.IndianRed;
            }
            return Color.PaleVioletRed;
        }
        private string GetTextForRemainingTime(ShutDownStatusType shutDownStatus, TimeSpan timeRemaining)
        {
            if (shutDownStatus == ShutDownStatusType.ShutDownInProgress)
            {
                return $"Akce byla právě zahájena";
            }
            else
            {
                var seconds = timeRemaining.TotalSeconds;
                if (seconds >= 300d) return $"Akce proběhne za {timeRemaining:hh\\:mm}";
                if (seconds >= 60d) return $"Akce proběhne za {timeRemaining:mm\\:ss}";
                if (seconds > 0d) return $"Akce proběhne za {timeRemaining:mm\\:ss\\.f}";
            }
            return "Akce se neočekává";
        }
        private string GetTimeForRemainingTime(ShutDownStatusType shutDownStatus, TimeSpan timeRemaining)
        {
            if (shutDownStatus == ShutDownStatusType.ShutDownInProgress)
            {
                return TimeInfoRemainingZero;
            }
            else
            {
                var seconds = timeRemaining.TotalSeconds;
                if (seconds >= 300d) return $"{timeRemaining:hh\\:mm}";
                if (seconds >= 60d) return $"{timeRemaining:mm\\:ss}";
                if (seconds > 0d) return $"{timeRemaining:mm\\:ss\\.f}";
            }
            return TimeInfoRemainingZero;
        }
        private static string TimeInfoRemainingZero { get { return "00:00.0"; } }
        #endregion
        #region Provedení cílové akce
        /// <summary>
        /// Provedení cílové akce
        /// </summary>
        private void DoAction()
        {
            this.TimeInfo = TimeInfoRemainingZero;
            CallShowStep(true);
            switch (this.Action)
            {
                case ActionType.Sleep:
                    DoActionSleep();
                    break;
                case ActionType.PowerOff:
                    DoActionPowerOff();
                    break;
                case ActionType.Restart:
                    DoActionRestart();
                    break;
                default:
                    this.StatusInfo = "Neznámá požadovaná akce " + this.Action.ToString();
                    CallShowStep(true);
                    break;
            }
        }
        private void DoActionSleep()
        {
            this.StatusInfo = "Přechod do stavu hibernace v " + DateTime.Now.ToString("dd.MM. HH:mm:ss");
            CallShowStep(true);
            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Hibernate, true, false);
        }
        private void DoActionPowerOff()
        {
            this.StatusInfo = "Přechod do stavu PowerOff v " + DateTime.Now.ToString("dd.MM. HH:mm:ss");
            CallShowStep(true);
            ExitWindowsEx(ExitWindowsEx_FlagShutDown + ExitWindowsEx_FlagForce, 0);
        }
        private void DoActionRestart()
        {
            this.StatusInfo = "Přechod do stavu Restart v " + DateTime.Now.ToString("dd.MM. HH:mm:ss");
            CallShowStep(true);
            ExitWindowsEx(ExitWindowsEx_FlagReboot + ExitWindowsEx_FlagForce, 0);
        }
        [DllImport("user32.dll")]
        public static extern int ExitWindowsEx(int uFlags, int dwReason);
        private const int ExitWindowsEx_FlagLogOff = 0;
        private const int ExitWindowsEx_FlagShutDown = 1;
        private const int ExitWindowsEx_FlagReboot = 2;
        private const int ExitWindowsEx_FlagForce = 4;


        //  Informace o postupech vypnutí:
        // https://www.c-sharpcorner.com/article/lock-logoff-reboot-shutdown-hibernate-standby-in-net/
        //  další:
        // https://docs.microsoft.com/en-us/windows/win32/shutdown/system-shutdown-functions
        // https://stackoverflow.com/questions/102567/how-to-shut-down-the-computer-from-c-sharp
        // https://www.codeproject.com/Tips/480049/Shut-Down-Restart-Log-off-Lock-Hibernate-or-Sleep
        #endregion
    }
    /// <summary>
    /// Režim určení času
    /// </summary>
    public enum TimeModeType { None, AfterTime, AtTime, Inactivity, Now }
    /// <summary>
    /// Cílová akce
    /// </summary>
    public enum ActionType { None, Sleep, PowerOff, Restart }
    /// <summary>
    /// Stav procesu ShutDown
    /// </summary>
    public enum ShutDownStatusType 
    {
        /// <summary>
        /// Není aktivní
        /// </summary>
        None,
        /// <summary>
        /// Očekáváme vypnutí delší:
        /// a) časový interval = do pěti minut před cílovým časem;
        /// b) čekání na neaktivitu = registrujeme aktivitu;
        /// </summary>
        BeforeWaiting,
        /// <summary>
        /// Očekáváme vypnutí bližší:
        /// a) časový interval = do jedné minuty před cílovým časem;
        /// b) čekání na neaktivitu = registrujeme aktivitu;
        /// </summary>
        Waiting,
        /// <summary>
        /// Vypnutí bude do jedné minuty; stále je možno cancelovat
        /// </summary>
        Preparing,
        /// <summary>
        /// Vypnutí proběhne do deseti sekund; stále je možno cancelovat
        /// </summary>
        ShutDown,
        /// <summary>
        /// Vypnutí bylo zahájeno; již není možno cancelovat
        /// </summary>
        ShutDownInProgress,
    }
}
