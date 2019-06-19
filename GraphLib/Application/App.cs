using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Data;
using System.Drawing;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ASOL.TestGUI")]

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// App: service class
    /// </summary>
    public class App
    {
        #region Singleton of App
        /// <summary>
        /// Singleton instance
        /// </summary>
        protected static App Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (__Locker)
                    {
                        if (__Instance == null)
                        {
                            if (__InitialisationStatus == InitialisationStatus.InProgressRaw)
                                throw new GraphLibCodeException("Chyba při inicializaci instance App: předčasné použití App singletonu ve fázi InProgressRaw.");

                            __InitialisationStatus = InitialisationStatus.InProgressRaw;
                            App instance = new App();
                            __Instance = instance;
                            __InitialisationStatus = InitialisationStatus.InProgressApp;
                            __Instance._InitApp();
                            __InitialisationStatus = InitialisationStatus.Working;
                        }
                    }
                }
                return __Instance;
            }
        }
        /// <summary>
        /// Singleton úložiště App
        /// </summary>
        protected static App __Instance = null;
        /// <summary>
        /// Stav inicializace
        /// </summary>
        protected static InitialisationStatus __InitialisationStatus = InitialisationStatus.None;
        /// <summary>
        /// Zámek singletonu
        /// </summary>
        protected static object __Locker = new object();
        /// <summary>
        /// Stavy inicializace
        /// </summary>
        protected enum InitialisationStatus
        {
            /// <summary>
            /// Not initialised
            /// </summary>
            None,
            /// <summary>
            /// Create single instance, Instance property is invalid, attempt to use Instance property throw an InvalidOperationException
            /// </summary>
            InProgressRaw,
            /// <summary>
            /// Create single instance, Instance property is now valid, but not fully initialised (can be used, but not all parts of Instance are usable)
            /// </summary>
            InProgressApp,
            /// <summary>
            /// Instance is fully initialised and valid
            /// </summary>
            Working
        }
        /// <summary>
        /// true pokud byla aplikace spuštěna v Debug režimu.
        /// Tato property nereaguje na změny (Attach / Detach) ve Visual Studiu, provedené až po spuštění aplikace.
        /// </summary>
        public static bool IsDebugMode { get { return Instance._IsDebugMode; } }
        private void _RunTest()
        {
            TestEngine.RunTests(TestType.AllStandard);
        }
        /// <summary>
        /// Ukončí aplikaci
        /// </summary>
        public static void End()
        {
            if (__Instance != null)
                Instance._End();
        }
        private void _End()
        {
            Trace.End();
            this._WorkerEnd();
            this._BackgroundStop();
            Asol.Tools.WorkScheduler.Components.FontInfo.ResetFonts();
        }
        #endregion
        #region Constructor, Initialisation
        /// <summary>
        /// Konstruktor instance <see cref="App"/>, vyvolá první fázi inicializace <see cref="_InitRaw()"/>.
        /// Teprve po proběhnutí konstruktoru a uložení instance do singletonu lze provést druhou fázi inicializace <see cref="_InitApp()"/>.
        /// </summary>
        private App()
        {
            this._InitRaw();
        }
        /// <summary>
        /// První fáze inicializace, kdy ještě není k dispozici property <see cref="Instance"/>.
        /// Objekty, které se zde inicializují, nesmí používat metody App.
        /// Použití property <see cref="Instance"/> property vyvolá chybu InvalidOperationException.
        /// </summary>
        private void _InitRaw()
        {   // Tyto inicializátory NESMÍ používat instanci App. Pořadí inicializátorů je libovolné (každý si udělá jen to svoje).
            this._AppInit();
            this._TraceInit();
            this._NextIdInit();
            this._RegisterInit();
            this._ZoomInit();
        }
        /// <summary>
        /// Druhá fáze inicializace, kdy už je k dispozici property <see cref="Instance"/>.
        /// Objekty, které se zde inicializují, mohou používat metody App.
        /// </summary>
        private void _InitApp()
        {   // Tyto inicializátory MOHOU používat instanci App. Pořadí inicializátorů je DŮLEŽITÉ, protože již mohou využívat navzájem svých služeb.
            this._PluginInit();
            this._WorkerInit();
            this._ResourcesInit();
            this._RunTest();
        }
        /// <summary>
        /// Prvotní inicializace samotného objektu
        /// </summary>
        private void _AppInit()
        {
            this._IsDebugMode = System.Diagnostics.Debugger.IsAttached;
            this._IsDeveloperMode = null;
        }
        private bool _IsDebugMode;
        #endregion
        #region Run main form
        /// <summary>
        /// Spustí main formulář aplikace
        /// </summary>
        /// <param name="formType"></param>
        public static void RunMainForm(Type formType)
        {
            Instance._RunMainForm(formType);
        }
        /// <summary>
        /// Hlavní formulář aplikace
        /// </summary>
        public static System.Windows.Forms.Form MainForm { get { return Instance._AppMainForm; } }
        /// <summary>
        /// Spustí main formulář aplikace
        /// </summary>
        /// <param name="formType"></param>
        private void _RunMainForm(Type formType)
        {
            System.Threading.Thread.CurrentThread.Name = "GUI";
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                using (System.Windows.Forms.ApplicationContext context = new System.Windows.Forms.ApplicationContext())
                {
                    this._AppContext = context;
                    this._AppMainForm = System.Activator.CreateInstance(formType) as System.Windows.Forms.Form;
                    context.MainForm = this._AppMainForm;
                    System.Windows.Forms.Application.Run(context);
                }
            }
            catch (Exception exc)
            {
                Trace.Exception(exc);

                Exception e = exc;
                while (e.InnerException != null)
                    e = e.InnerException;

                string message = e.Message + Environment.NewLine + e.StackTrace;
                ShowError(message);

                if (System.Diagnostics.Debugger.IsAttached)
                    throw;
            }
            finally
            {
                this._AppMainForm = null;
                this._AppContext = null;
            }

            Application.App.End();
        }
        private System.Windows.Forms.ApplicationContext _AppContext;
        private System.Windows.Forms.Form _AppMainForm;
        #endregion
        #region Trace (write info to trace file)
        /// <summary>
        /// Přístup k Trace systému
        /// </summary>
        public static Trace Trace { get { return Instance._Trace; } }
        /// <summary>
        /// Returns true when specified priority is equal or higher than TracePriority, and request for write to trace has been performed.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static bool TraceForPriority(TracePriority priority)
        {
            if (priority == Application.TracePriority.Priority0_None) return false;
            if (priority == Application.TracePriority.Priority9_Allways) return true;
            return ((int)priority >= Instance.__TracePriorityInt);
        }
        /// <summary>
        /// Priority for debug informations.
        /// To trace will be writted items with this priority and higher.
        /// For example, after setting TracePriority = AboveNormal_7 will be information with priority Normal_5 discard (item priority is lower than TracePriority).
        /// Setting TracePriority = DebugOnly_1 will write all item with DebugOnly_1 or higher, but not write item with priority None_0.
        /// </summary>
        public static TracePriority TracePriority
        {
            get { return Instance._TracePriority; }
            set { Instance._TracePriority = value; }
        }
        private void _TraceInit()
        {
            this._Trace = new Trace();
            this._TracePriority = (this._IsDebugMode ? Application.TracePriority.Priority3_BellowNormal : Application.TracePriority.Priority5_Normal);
        }
        /// <summary>
        /// Instance property, can be set a value, its int value will be stored to this.__TracePriorityInt.
        /// </summary>
        private TracePriority _TracePriority { get { return this.__TracePriority; } set { this.__TracePriority = value; this.__TracePriorityInt = (int)value; } }
        /// <summary>
        /// Instance variable, do not set directly!
        /// </summary>
        private TracePriority __TracePriority;
        /// <summary>
        /// Instance variable, do not set directly!
        /// </summary>
        private int __TracePriorityInt;
        /// <summary>
        /// Instance of Trace object, for writing into file
        /// </summary>
        private Trace _Trace;
        #endregion
        #region Plugins (search for implementations of IPlugin interfaces)
        /// <summary>
        /// Returns all objects, which implements specified interface.
        /// When type is null or is not interface, return empty array.
        /// When type is interface, which is not descendant of IPlugin, return also empty array.
        /// When return non-empty array, then none of items is null.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<object> GetPlugins(Type type)
        {
            return Instance._GetPlugins(type);
        }
        private IEnumerable<object> _GetPlugins(Type type)
        {
            bool isDebugMode = this._IsDebugMode;
            return this._Plugin.GetPlugins(type, isDebugMode);
        }
        private void _PluginInit()
        {
            this._Plugin = new Plugin();
            this._Plugin.LoadPlugins();
        }
        private Plugin _Plugin;
        #endregion
        #region Localizable texts
        /// <summary>
        /// Přístupový bod pro lokalizaci
        /// </summary>
        /// <param name="code"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        public static string LocalizeCode(string code, string defaultText)
        {
            return defaultText;
        }
        #endregion
        #region TryRun action
        /// <summary>
        /// Metoda vyvolá danou akci v try-catch bloku, případnou chybu zapíše do trace a pokud je Debug režim, tak ji i ohlásí.
        /// </summary>
        /// <param name="action"></param>
        public static void TryRun(Action action)
        {
            _TryRun(action, true, true);
        }
        /// <summary>
        /// Metoda vyvolá danou akci v try-catch bloku, případnou chybu zapíše do trace a pokud je Debug režim, tak ji i ohlásí.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="showErrorInDebugMode">Pokud jsme v Debug modu, tak zobrazit chybu</param>
        public static void TryRun(Action action, bool showErrorInDebugMode)
        {
            _TryRun(action, showErrorInDebugMode, showErrorInDebugMode);
        }
        /// <summary>
        /// Metoda vyvolá danou akci v try-catch bloku, v threadu na pozadí, případnou chybu zapíše do trace ale nehlásí ji.
        /// </summary>
        /// <param name="action"></param>
        public static void TryRunBgr(Action action)
        {
            System.Threading.Thread bgThread = new System.Threading.Thread(() => _TryRun(action, true, false));
            bgThread.IsBackground = true;
            bgThread.Name = "TryRunBgr_" + DateTime.Now.ToString("HH:mm:ss");
            bgThread.Start();
        }
        /// <summary>
        /// Spustí danou akci v try - catch bloku, volitelně řeší trace chyby a její Show message
        /// </summary>
        /// <param name="action"></param>
        /// <param name="traceError"></param>
        /// <param name="showErrorInDebugMode">Pokud jsme v Debug modu, tak zobrazit chybu</param>
        private static void _TryRun(Action action, bool traceError, bool showErrorInDebugMode)
        {
            try
            {
                action();
            }
            catch (Exception exc)
            {
                if (traceError)
                    Trace.Exception(exc);
                if (showErrorInDebugMode && IsDebugMode)
                    ShowError(exc);
            }
        }
        #endregion
        #region RunAfter
        /// <summary>
        /// Zajistí, že za daný čas bude vyvolána daná akce. Samozřejmě dojde k jejímu vyvolání v threadu na pozadí, nikoli v aktuálním threadu.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="action"></param>
        public static void RunAfter(TimeSpan time, Action action)
        {
            Instance._BackgroundAddAction(time, action);
        }
        /// <summary>
        /// Připraví thread na pozadí.
        /// Pokud už je připraven, nic nemění.
        /// </summary>
        private void _BackgroundPrepare()
        {
            if (this._BackgroundSemaphore != null) return;

            lock (this._BackgroundLock)
            {
                if (this._BackgroundSemaphore == null)
                {
                    this._BackgroundThread = new System.Threading.Thread(this._BackgroundLoop)
                    {
                        Name = "AppBackground",
                        Priority = System.Threading.ThreadPriority.BelowNormal,
                        IsBackground = true
                    };

                    this._BackgroundWorkList = new List<Tuple<DateTime, Action>>();
                    this._BackgroundSemaphore = new System.Threading.AutoResetEvent(false);
                }
            }
            this._BackgroundThread.Start();
        }
        /// <summary>
        /// Metoda, v níž si žije thread <see cref="_BackgroundThread"/>, do té doby než bude nastaveno <see cref="_BackgroundRunning"/> = false.
        /// </summary>
        private void _BackgroundLoop()
        {
            this._BackgroundRunning = true;
            while (this._BackgroundRunning)
            {
                TimeSpan? waitToAction;
                Action action = this._BackgroundGetAction(out waitToAction);
                if (!this._BackgroundRunning) break;
                if (action != null)
                {   // Máme něco na práci?
                    try { action(); }
                    catch (Exception exc) { Trace.Exception(exc); }
                }
                if (!this._BackgroundRunning) break;
                if (waitToAction.HasValue)
                {   // Máme si počkat, než se podíváme co je na práci?
                    this._BackgroundSemaphore.WaitOne(waitToAction.Value);
                }
                if (!this._BackgroundRunning) break;
            }
            this._BackgroundWorkList.Clear();
            this._BackgroundWorkList = null;
            this._BackgroundSemaphore.Dispose();
            this._BackgroundSemaphore = null;
            this._BackgroundThread = null;
        }
        /// <summary>
        /// Přidá danou akci do souhrnu akcí k provedení v threadu <see cref="_BackgroundThread"/>, za daný čas od teď
        /// </summary>
        /// <param name="time"></param>
        /// <param name="action"></param>
        private void _BackgroundAddAction(TimeSpan time, Action action)
        {
            if (action == null) return;
            this._BackgroundPrepare();

            DateTime start = DateTime.Now.Add(time);
            Tuple<DateTime, Action> tuple = new Tuple<DateTime, Action>(start, action);
            lock (this._BackgroundLock)
            {   // Přidáme akci do soupisu, a setřídíme podle času spuštění vzestupně:
                this._BackgroundWorkList.Add(tuple);
                if (this._BackgroundWorkList.Count > 1)
                    this._BackgroundWorkList.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            }
            // Probudíme thread na pozadí, ten si načte akci, kterou má dělat; nebo čas který má ještě počkat:
            this._BackgroundSemaphore.Set();
        }
        /// <summary>
        /// Metoda vrací akci, kterou je nutno spustit v threadu <see cref="_BackgroundThread"/>, anebo čas, která je nutno počkat
        /// </summary>
        /// <param name="waitToAction"></param>
        /// <returns></returns>
        private Action _BackgroundGetAction(out TimeSpan? waitToAction)
        {
            lock (this._BackgroundLock)
            {
                if (this._BackgroundWorkList.Count == 0)
                {   // Není vůbec nic na práci => počkejme 60 sekund (default) a uvidí se...
                    waitToAction = TimeSpan.FromSeconds(60d);
                    return null;                           // Nyní nebudu dělat žádnou akci.
                }
                // Nějaká práce tu je, ale otázkou je, zda už je to aktuální:
                DateTime now = DateTime.Now;
                Tuple<DateTime, Action> tuple = this._BackgroundWorkList[0];
                if (tuple.Item1 <= now)
                {   // Máme práci, a už se může provést:
                    this._BackgroundWorkList.RemoveAt(0);  // Tuhle akci z fronty práce odeberu.
                    waitToAction = null;                   // Po jejím provedení nebudu čekat, ale znovu se podívám do fronty.
                    return tuple.Item2;                    // Zajistím provedení dané akce
                }
                // Máme práci, ale ještě nepřišel její čas:
                waitToAction = (tuple.Item1 - now);        // Na to, abych se pustil do dané akce, máme ještě nějaký kladný čas k čekání...
                return null;                               //  a zatím se nebudeme pouštět do žádných větších akcí :-)
            }
        }
        /// <summary>
        /// Zastaví běh threadu <see cref="_BackgroundThread"/>
        /// </summary>
        private void _BackgroundStop()
        {
            if (this._BackgroundRunning && this._BackgroundSemaphore != null)
            {
                this._BackgroundRunning = false;
                this._BackgroundSemaphore.Set();
            }
        }
        private System.Threading.Thread _BackgroundThread;
        private List<Tuple<DateTime, Action>> _BackgroundWorkList;
        private object _BackgroundLock = new object();
        private System.Threading.AutoResetEvent _BackgroundSemaphore;
        private bool _BackgroundRunning;
        #endregion
        #region Dialog window
        /// <summary>
        /// Zobrazí danou informaci
        /// </summary>
        /// <param name="message"></param>
        public static void ShowInfo(string message) { _ShowMsg(message, System.Windows.Forms.MessageBoxIcon.Information); }
        /// <summary>
        /// Zobrazí dané varování
        /// </summary>
        /// <param name="message"></param>
        public static void ShowWarning(string message) { _ShowMsg(message, System.Windows.Forms.MessageBoxIcon.Warning); }
        /// <summary>
        /// Zobrazí danou chybu
        /// </summary>
        /// <param name="message"></param>
        public static void ShowError(string message) { _ShowMsg(message, System.Windows.Forms.MessageBoxIcon.Error); }
        /// <summary>
        /// Zobrazí danou chybu
        /// </summary>
        /// <param name="exc"></param>
        public static void ShowError(Exception exc) { string message = exc.Message + Environment.NewLine + exc.StackTrace; _ShowMsg(message, System.Windows.Forms.MessageBoxIcon.Error); }
        /// <summary>
        /// Zobraz danou zprávu a ikonku
        /// </summary>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        private static void _ShowMsg(string message, System.Windows.Forms.MessageBoxIcon icon)
        {
            System.Windows.Forms.Form mainForm = MainForm;
            if (mainForm == null)
                _ShowMsgGui(null, message, icon);
            else if (mainForm.IsDisposed)
                _ShowMsgGui(null, message, icon);
            else if (mainForm.InvokeRequired)
                mainForm.BeginInvoke(new Action<System.Windows.Forms.Form, string, System.Windows.Forms.MessageBoxIcon>(_ShowMsgGui), mainForm, message, icon);
            else
                _ShowMsgGui(mainForm, message, icon);
        }
        /// <summary>
        /// Zobrazí danou hlášku
        /// </summary>
        /// <param name="mainForm"></param>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        private static void _ShowMsgGui(System.Windows.Forms.Form mainForm, string message, System.Windows.Forms.MessageBoxIcon icon)
        {
            if (mainForm == null)
                System.Windows.Forms.MessageBox.Show(message, App.AppProductTitle, System.Windows.Forms.MessageBoxButtons.OK, icon);
            else
                System.Windows.Forms.MessageBox.Show(mainForm, message, App.AppProductTitle, System.Windows.Forms.MessageBoxButtons.OK, icon);
        }
        #endregion
        #region ProcessRequestOnbackground() : Worker (process requests in background thread in queue)
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        public static void ProcessRequestOnbackground<TRequest>(Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod)
        {
            Instance._Worker.AddRequest<TRequest>(workMethod, workData, callbackMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public static void ProcessRequestOnbackground<TRequest>(Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            Instance._Worker.AddRequest<TRequest>(workMethod, workData, callbackMethod, exceptionMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="priorityId">ID for priority. See also CurrentPriorityId property.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        public static void ProcessRequestOnbackground<TRequest>(int priorityId, Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod)
        {
            Instance._Worker.AddRequest<TRequest>(priorityId, workMethod, workData, callbackMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="priorityId">ID for priority. See also CurrentPriorityId property.</param>
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public static void ProcessRequestOnbackground<TRequest>(int priorityId, string name, Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            Instance._Worker.AddRequest<TRequest>(priorityId, name, workMethod, workData, callbackMethod, exceptionMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        public static void ProcessRequestOnbackground<TRequest, TResponse>(Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod)
        {
            Instance._Worker.AddRequest<TRequest, TResponse>(workMethod, workData, callbackMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public static void ProcessRequestOnbackground<TRequest, TResponse>(Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            Instance._Worker.AddRequest<TRequest, TResponse>(workMethod, workData, callbackMethod, exceptionMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="priorityId">ID for priority. See also CurrentPriorityId property.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        public static void ProcessRequestOnbackground<TRequest, TResponse>(int priorityId, Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod)
        {
            Instance._Worker.AddRequest<TRequest, TResponse>(priorityId, workMethod, workData, callbackMethod);
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="priorityId">ID for priority. See also CurrentPriorityId property.</param>
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public static void ProcessRequestOnbackground<TRequest, TResponse>(int priorityId, string name, Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            Instance._Worker.AddRequest<TRequest, TResponse>(priorityId, name, workMethod, workData, callbackMethod, exceptionMethod);
        }
        /// <summary>
        /// Priority ID of current active process. Background requests, which are waiting in queue to processing (added via method App.ProcessRequestOnbackground()), 
        /// are dequeued from queue in order by its PriorityId and App.CurrentPriorityId.
        /// When CurrentPriorityId == null (default state), then no priority sorting of Background requests is processed.
        /// </summary>
        public static int? CurrentPriorityId
        {
            get { return Instance._CurrentPriority; }
            set { Instance._CurrentPriority = value; }
        }
        /// <summary>
        /// Initialize Worker instance
        /// </summary>
        private void _WorkerInit()
        {
            this._Worker = new Worker();
            this._CurrentPriority = null;
        }
        /// <summary>
        /// Stop all work in this._Worker thread
        /// </summary>
        private void _WorkerEnd()
        {
            if (this._Worker != null)
                this._Worker.Stop();
        }
        private Worker _Worker;
        private int? _CurrentPriority;
        #endregion
        #region NextID (create identity number for new instances of specified type)
        /// <summary>
        /// Returns a new ID for new item of specified type.
        /// ID is unique for target Type, per whole application.
        /// First ID has value 1.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int GetNextId(Type target)
        {
            if (target == null) return 0;
            return Instance._GetNextId(target.Namespace + "." + target.Name);
        }
        private int _GetNextId(string key)
        {
            int id = 0;
            if (String.IsNullOrEmpty(key)) return id;
            if (this._NextId.TryGetValue(key, out id))
                this._NextId[key] = ++id;
            else
                this._NextId.Add(key, ++id);
            return id;
        }
        private void _NextIdInit()
        {
            this._NextId = new Dictionary<string, int>();
        }
        private Dictionary<string, int> _NextId;
        #endregion
        #region Zoom
        /// <summary>
        /// Zde je poskytována veškerá podpora pro zoomování aplikace
        /// </summary>
        public static Zoom Zoom { get { return Instance._Zoom; } }
        /// <summary>
        /// Inicializuje instanci objektu Zoom
        /// </summary>
        private void _ZoomInit()
        {
            this._Zoom = new Application.Zoom();
        }
        private Zoom _Zoom;
        #endregion
        #region Ikonky aplikace a další resources
        /// <summary>
        /// Inicializuje instanci objektu Icons
        /// </summary>
        private void _ResourcesInit()
        {
            string appPath = AppCodePath;
            this._Icons = new Application.Icons(appPath);
            string resourceFile = System.IO.Path.Combine(appPath, "ASOL.GraphLib.res");
            this._Resources = new Resources(resourceFile, true);
        }
        /// <summary>
        /// Zde je poskytována veškerá podpora pro přístup k ikonkám
        /// </summary>
        public static Icons Icons { get { return Instance._Icons; } } private Icons _Icons;
        /// <summary>
        /// Zde je poskytována veškerá podpora pro přístup k resources
        /// </summary>
        public static Resources Resources { get { return Instance._Resources; } } private Resources _Resources;
        #endregion
        #region Register / Config
        /// <summary>
        /// Vrací true, když rregistr obsahuje daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool RegisterContainsKey(string key)
        {
            _RegisterCheckKey(key);
            return Instance._Register.ContainsKey(key);
        }
        /// <summary>
        /// Načte hodnotu z klíče registru
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string RegisterRead(string key)
        {
            _RegisterCheckKey(key);
            return Instance._Register[key];
        }
        /// <summary>
        /// Načte hodnotu z klíče registru, string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string RegisterRead(string key, string defaultValue)
        {
            _RegisterCheckKey(key);
            string value;
            if (Instance._Register.TryGetValue(key, out value)) return value;
            return defaultValue;
        }
        /// <summary>
        /// Zkusí načíst hodnotu z klíče registru, vrací true = úspěch
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool RegisterTryGetValue(string key, out string value)
        {
            _RegisterCheckKey(key);
            return Instance._Register.TryGetValue(key, out value);
        }
        /// <summary>
        /// Uloží hodnotu do registru
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void RegisterStore(string key, string value)
        {
            _RegisterCheckKey(key);
            if (Instance._Register.ContainsKey(key))
                Instance._Register[key] = value;
            else
                Instance._Register.Add(key, value);
        }
        /// <summary>
        /// Odebere klíč z registru
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void RegisterRemove(string key, string value)
        {
            _RegisterCheckKey(key);
            if (Instance._Register.ContainsKey(key))
                Instance._Register.Remove(key);
        }
        /// <summary>
        /// Zkontroluje správnost zadání klíče
        /// </summary>
        /// <param name="key"></param>
        private static void _RegisterCheckKey(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new GraphLibDataException("App.Register: zadaný klíč nesmí být prázdný.");
        }
        /// <summary>
        /// Vytvoří new instanci pro registry aplikace (konfigurace)
        /// </summary>
        private void _RegisterInit()
        {
            this._Register = new Dictionary<string, string>();
        }
        private Dictionary<string, string> _Register;
        #endregion
        #region App constants, paths
        /// <summary>
        /// Titulek aplikace (v dialogovém okně).
        /// Default = "Graphics library". 
        /// Lze setovat jinou hodnotu.
        /// </summary>
        public static string AppProductTitle
        {
            get { string value = Instance._AppProductTitle; return (!String.IsNullOrEmpty(value) ? value : "Graphics library"); }
            set { Instance._AppProductTitle = value; }
        }
        private string _AppProductTitle;
        /// <summary>
        /// Jméno autora. 
        /// Default = "Asseco Solutions". 
        /// Lze setovat jinou hodnotu. Projeví se v <see cref="AppLocalDataPath"/> a v WindowsRegister keys.
        /// </summary>
        public static string AppCompanyName
        {
            get { string value = Instance._AppCompanyName; return (!String.IsNullOrEmpty(value) ? value : "Asseco Solutions"); }
            set { Instance._AppCompanyName = value; }
        } private string _AppCompanyName;
        /// <summary>
        /// Název aplikace.
        /// Default = "GraphUtility". 
        /// Lze setovat jinou hodnotu. Projeví se v <see cref="AppLocalDataPath"/> a v WindowsRegister keys.
        /// </summary>
        public static string AppProductName
        {
            get { string value = Instance._AppProductName; return (!String.IsNullOrEmpty(value) ? value : "GraphUtility"); }
            set { Instance._AppProductName = value; }
        }
        private string _AppProductName;
        /// <summary>
        /// Path to LocalApplicationData directory.
        /// Usually this is: C:\Users\{user}\AppData\Local\
        /// </summary>
        public static string AppLocalDataPath
        { get { return System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppCompanyName, AppProductName); } }
        /// <summary>
        /// Return path to LocalAppData directory (with subdirectories), and when result path does not exists, then create it.
        /// Applicatin has granted user-permission for write into this path.
        /// </summary>
        /// <param name="subDirs"></param>
        /// <returns></returns>
        public static string GetAppLocalDataPath(params string[] subDirs)
        {
            string path = AppLocalDataPath;
            if (subDirs != null && subDirs.Length > 0)
            {
                List<string> paths = new List<string>();
                paths.Add(path);
                paths.AddRange(subDirs);
                path = System.IO.Path.Combine(paths.ToArray());
            }
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
            return path;
        }
        /// <summary>
        /// Adresář, v němž je uložen kód aplikace
        /// </summary>
        public static string AppCodePath
        {
            get
            {
                App instance = Instance;
                if (instance._AppCodePath == null)
                {
                    string file = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    instance._AppCodePath = System.IO.Path.GetDirectoryName(file);
                }
                return instance._AppCodePath;
            }
        }
        /// <summary>
        /// true pokud je aplikace spuštěna ve vývojovém prostředí.
        /// Tato property reaguje na přítomnost zdrojových kódů v přiměřené blízkosti u DLL souborů aplikace.
        /// </summary>
        public static bool IsDeveloperMode
        {
            get
            {
                bool? isDeveloperMode = Instance._IsDeveloperMode;
                if (!isDeveloperMode.HasValue)
                {
                    isDeveloperMode = false;
                    try
                    {
                        string path = AppCodePath;
                        // D:\Working\Csharp\GraphLibrary\TestGUI\bin\Debug\  =>  D:\Working\Csharp\GraphLibrary\TestGUI\Program.cs
                        if (System.IO.File.Exists(_GetPath(path, 2, "Program.cs")))
                            isDeveloperMode = true;
                        // D:\Working\Csharp\GraphLibrary\TestGUI\bin\Debug\  =>  D:\Working\Csharp\GraphLibrary\TestGUI\SchedulerDataSource.cs
                        else if (System.IO.File.Exists(_GetPath(path, 2, "SchedulerDataSource.cs")))
                            isDeveloperMode = true;
                        // D:\Working\Csharp\GraphLibrary\bin\  =>  D:\Working\Csharp\GraphLibrary\GraphLib\Application\App.cs 
                        else if (System.IO.File.Exists(_GetPath(path, 1, "GraphLib", "Application", "App.cs")))
                            isDeveloperMode = true;
                    }
                    catch (Exception) { isDeveloperMode = false; }
                    Instance._IsDeveloperMode = isDeveloperMode;
                }
                return isDeveloperMode.Value;
            }
        }
        private string _AppCodePath;
        private bool? _IsDeveloperMode;
        /// <summary>
        /// Metoda vrátí název souboru/cesty vytvořený z dané cesty (path), jejím zkrácení o (upDirs) složek nahoru, a přidáním (addItems) složek.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="upDirs"></param>
        /// <param name="addItems"></param>
        /// <returns></returns>
        private static string _GetPath(string path, int upDirs, params string[] addItems)
        {
            if (path == null) return "";
            while (!String.IsNullOrEmpty(path) && upDirs-- > 0)
                path = System.IO.Path.GetDirectoryName(path);
            foreach (string addItem in addItems)
                path = System.IO.Path.Combine(path, addItem);
            return path;
        }
        #endregion
    }
    #region Exceptions : třídy výjimek používaných v GraphLibrary
    /// <summary>
    /// Základní třída pro výjimky, které vyhazuje projekt GraphLibrary
    /// </summary>
    public class GraphLibException : ApplicationException
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        public GraphLibException(string message) : base(message) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public GraphLibException(string message, Exception innerException) : base(message, innerException) { }
    }
    /// <summary>
    /// Výjimka, která oznamuje chybně dodaná data na vstupu do metody.
    /// Například vstup je null a nemá být, data jsou jiného typu než mají být a nejsou převoditelná na cílový typ, nebo jsou vyžadována data, která neexistují (chybějící klíč v Dictionary) atd.
    /// Jde tedy o problém spíš závislý na datech, než na kódu.
    /// </summary>
    public class GraphLibDataException : ApplicationException
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        public GraphLibDataException(string message) : base(message) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public GraphLibDataException(string message, Exception innerException) : base(message, innerException) { }
    }
    /// <summary>
    /// Výjimka, která oznamuje chybně volanou metodu za nevhodné situace.
    /// Například je volána metoda pro zpracování něčeho, co už je zpracované a uzavřené, nebo naopak se snažíme použít něco, co ještě není připravené.
    /// Nebo chceme použít určitou metodu, která v aktuální situaci je nepoužitelná.
    /// Jde tedy o problém spíš způsobený kódem, než nesprávnými daty.
    /// </summary>
    public class GraphLibCodeException : ApplicationException
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        public GraphLibCodeException(string message) : base(message) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public GraphLibCodeException(string message, Exception innerException) : base(message, innerException) { }
    }
    /// <summary>
    /// Výjimka, která oznamuje chybu uživatelského vstupu.
    /// Například do datového pole je zadána hodnota, kterou nelze převést na DateTime, nebo uživatel nezadal něco co zadat měl.
    /// Nejde tedy o problém aplikace.
    /// </summary>
    public class GraphLibUserException : ApplicationException
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        public GraphLibUserException(string message) : base(message) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public GraphLibUserException(string message, Exception innerException) : base(message, innerException) { }
    }
    #endregion
}
