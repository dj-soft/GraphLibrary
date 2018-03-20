using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Djs.Common.Data;
using System.Drawing;

namespace Djs.Common.Application
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
                                throw new InvalidOperationException("Error during App._InitRaw(): early use of App singleton during InProgressRaw phase.");

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
        protected static App __Instance = null;
        protected static InitialisationStatus __InitialisationStatus = InitialisationStatus.None;
        protected static object __Locker = new object();
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
        #region Constructor, Initialisation
        private App()
        {
            this._InitRaw();
        }
        /// <summary>
        /// Initialisation without existing Instance property.
        /// Using of Instance property causing exception (InvalidOperationException).
        /// </summary>
        private void _InitRaw()
        {   // These initialisations are independent on App instance, their order has no relevance:
            this._TraceInit();
            this._NextIdInit();
            this._RegisterInit();
            this._ZoomInit();
        }
        /// <summary>
        /// Initialisation with existing Instance property.
        /// You can use a Instance property, but not all parts if Instance are now initialised.
        /// </summary>
        private void _InitApp()
        {   // These initialisations are dependent on App instance, must be run after _InitRaw(), an some initialisers must run after other (ther order is significant):
            this._PluginInit();
            this._WorkerInit();
            this._RunTest();
        }
        #endregion
        private void _RunTest()
        {
            TestEngine.RunTests(TestType.AllStandard);
        }
        public static void End()
        {
            if (__Instance != null)
                Instance._End();
        }
        private void _End()
        {
            TraceEnd();
            this._WorkerEnd();
            Djs.Common.Components.FontInfo.ResetFonts();
        }
        #endregion
        #region Run main form
        public static void RunMainForm(Type formType)
        {
            Instance._RunMainForm(formType);
        }
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
                TraceException(exc);

                Exception e = exc;
                while (e.InnerException != null)
                    e = e.InnerException;

                System.Windows.Forms.MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, "Exception", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation, System.Windows.Forms.MessageBoxDefaultButton.Button1);

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
        /// Write one line "Info" into trace, when current priority level is Normal_5 or higher.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceInfo(string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(TracePriority.Priority5_Normal))
                Instance._Trace.Info(type, method, result, items);
        }
        /// <summary>
        /// Write one line "Info" into trace, when current priority level is (parameter "priority") or higher.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceInfo(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(priority))
                Instance._Trace.Info(type, method, result, items);
        }
        /// <summary>
        /// Write one line "Info" into trace, when current priority level is Normal_5 or higher.
        /// Immediatelly after write call Flush into file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceInfoNow(string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(TracePriority.Priority5_Normal))
                Instance._Trace.InfoNow(type, method, result, items);
        }
        /// <summary>
        /// Write one line "Info" into trace, when current priority level is (parameter "priority") or higher.
        /// Immediatelly after write call Flush into file.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceInfoNow(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(priority))
                Instance._Trace.InfoNow(type, method, result, items);
        }

        /// <summary>
        /// Write one line "Warning" into trace, when current priority level is Normal_5 or higher.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceWarning(string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(TracePriority.Priority5_Normal))
                Instance._Trace.Warning(type, method, result, items);
        }
        /// <summary>
        /// Write one line "Warning" into trace, when current priority level is (parameter "priority") or higher.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceWarning(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(priority))
                Instance._Trace.Warning(type, method, result, items);
        }
        /// <summary>
        /// Write one line "Warning" into trace, when current priority level is Normal_5 or higher.
        /// Immediatelly after write call Flush into file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceWarningNow(string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(TracePriority.Priority5_Normal))
                Instance._Trace.WarningNow(type, method, result, items);
        }
        /// <summary>
        /// Write one line "Warning" into trace, when current priority level is (parameter "priority") or higher.
        /// Immediatelly after write call Flush into file.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceWarningNow(TracePriority priority, string type, string method, string result, params string[] items)
        {
            if (TraceForPriority(priority))
                Instance._Trace.WarningNow(type, method, result, items);
        }

        /// <summary>
        /// Write one line "Error" into trace, regardless of current priority level.
        /// Immediatelly after write call Flush into file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        public static void TraceError(string type, string method, string result, params string[] items)
        {
            Instance._Trace.Error(type, method, result, items);
        }

        /// <summary>
        /// Write one block for "Exception" into trace, regardless of current priority level.
        /// Write all InnerExceptions.
        /// Immediatelly after write call Flush into file.
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="items"></param>
        public static void TraceException(Exception exc, params string[] items)
        {
            Instance._Trace.Exception(exc, items);
        }

        /// <summary>
        /// Returns an ITraceScope object: this object write Begin line into trace when is created, and write End line on Dispose this scope instance.
        /// Additional informations (array items) is writed into Begin line.
        /// User can add any information during scope live via methods AddItem() / AddItems(), and can set Result value. This is write into End line.
        /// Real write into trace file is performed only when current priority level is Normal_5 or higher.
        /// Instance is (in code) usable regardless on priority (=even when does not really write to trace file).
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static ITraceScope TraceScope(string type, string method, string result, params string[] items)
        {
            return Instance._Trace.Scope(TraceForPriority(TracePriority.Priority5_Normal), type, method, result, items);
        }
        /// <summary>
        /// Returns an ITraceScope object: this object write Begin line into trace when is created, and write End line on Dispose this scope instance.
        /// Additional informations (array items) is writed into Begin line.
        /// User can add any information during scope live via methods AddItem() / AddItems(), and can set Result value. This is write into End line.
        /// Real write into trace file is performed only when current priority level is (parameter "priority") or higher.
        /// Instance is (in code) usable regardless on priority (=even when does not really write to trace file).
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static ITraceScope TraceScope(TracePriority priority, string type, string method, string result, params string[] items)
        {
            return Instance._Trace.Scope(TraceForPriority(priority), type, method, result, items);
        }
        /// <summary>
        /// Returns an ITraceScope object: this object write Begin line into trace when is created, and write End line on Dispose this scope instance.
        /// Additional informations (array items) is writed into Begin line.
        /// User can add any information during scope live via methods AddItem() / AddItems(), and can set Result value. This is write into End line.
        /// Real write into trace file is performed only when current priority level is Normal_5 or higher.
        /// Instance is (in code) usable regardless on priority (=even when does not really write to trace file).
        /// This method create scope with immediatelly writing into file (after write line is performed Flush() on file stream).
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static ITraceScope TraceScopeNow(string type, string method, string result, params string[] items)
        {
            return Instance._Trace.ScopeNow(TraceForPriority(TracePriority.Priority5_Normal), type, method, result, items);
        }
        /// <summary>
        /// Returns an ITraceScope object: this object write Begin line into trace when is created, and write End line on Dispose this scope instance.
        /// Additional informations (array items) is writed into Begin line.
        /// User can add any information during scope live via methods AddItem() / AddItems(), and can set Result value. This is write into End line.
        /// Real write into trace file is performed only when current priority level is (parameter "priority") or higher.
        /// Instance is (in code) usable regardless on priority (=even when does not really write to trace file).
        /// This method create scope with immediatelly writing into file (after write line is performed Flush() on file stream).
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="result"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static ITraceScope TraceScopeNow(TracePriority priority, string type, string method, string result, params string[] items)
        {
            return Instance._Trace.ScopeNow(TraceForPriority(priority), type, method, result, items);
        }
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
        /// <summary>
        /// Immediatelly call Flush() on file stream.
        /// </summary>
        public static void TraceFlush()
        {
            Instance._Trace.Flush();
        }
        /// <summary>
        /// Write into trace new event for End of application, perform Flush on stream, and really close trace file.
        /// </summary>
        public static void TraceEnd()
        {
            Instance._Trace.End();
        }
        private void _TraceInit()
        {
            this._Trace = new Trace();
            this._IsDebugMode = System.Diagnostics.Debugger.IsAttached;
            this._TracePriority = Application.TracePriority.Priority1_ElementaryTimeDebug; // .Lowest_2;
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
        private bool _IsDebugMode;
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
        public static string LocalizeCode(string code, string defaultText)
        {
            return defaultText;
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
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
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
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
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
        #region GuiZoom
        /// <summary>
        /// Zoom for GUI. Can change size of all elements.
        /// Default zoom = 1, then GUI is showed in original pixel size.
        /// </summary>
        public static float Zoom
        {
            get { return Instance._Zoom; }
            set { Instance._ZoomSet(value); }
        }
        /// <summary>
        /// Return bounds zoomed by current App.Zoom
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static Rectangle ZoomBounds(Rectangle bounds)
        {
            float zoom = Instance._Zoom;
            int l = _ZoomValue(zoom, bounds.Left);
            int t = _ZoomValue(zoom, bounds.Top);
            int r = _ZoomValue(zoom, bounds.Right);
            int b = _ZoomValue(zoom, bounds.Bottom);
            return Rectangle.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Return point zoomed by current App.Zoom
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point ZoomPoint(Point point)
        {
            float zoom = Instance._Zoom;
            return new Point(_ZoomValue(zoom, point.X), _ZoomValue(zoom, point.Y));
        }
        /// <summary>
        /// Return size zoomed by current App.Zoom
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Size ZoomSize(Size size)
        {
            float zoom = Instance._Zoom;
            return new Size(_ZoomValue(zoom, size.Width), _ZoomValue(zoom, size.Height));
        }
        /// <summary>
        /// Return distance zoomed by current App.Zoom
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static int ZoomDistance(int distance)
        {
            float zoom = Instance._Zoom;
            return _ZoomValue(zoom, distance);
        }
        private static int _ZoomValue(float zoom, int distance)
        {
            return (int)(Math.Round((double)(zoom * (float)distance), 0));
        }
        private void _ZoomInit()
        {
            this._Zoom = 1.0f;
        }
        private void _ZoomSet(float value)
        {
            float oldZoom = this._Zoom;
            float newZoom = (value < 0.2f ? 0.2f : (value > 5.0f ? 5.0f : value));
            if (newZoom != oldZoom)
            {
                this._Zoom = newZoom;
                Djs.Common.Components.FontInfo.ResetFonts();
            }
        }
        private float _Zoom;
        #endregion
        #region Register / Config
        public static bool RegisterContainsKey(string key)
        {
            _RegisterCheckKey(key);
            return Instance._Register.ContainsKey(key);
        }
        public static string RegisterRead(string key)
        {
            _RegisterCheckKey(key);
            return Instance._Register[key];
        }
        public static string RegisterRead(string key, string defaultValue)
        {
            _RegisterCheckKey(key);
            string value;
            if (Instance._Register.TryGetValue(key, out value)) return value;
            return defaultValue;
        }
        public static bool RegisterTryGetValue(string key, out string value)
        {
            _RegisterCheckKey(key);
            return Instance._Register.TryGetValue(key, out value);
        }
        public static void RegisterStore(string key, string value)
        {
            _RegisterCheckKey(key);
            if (Instance._Register.ContainsKey(key))
                Instance._Register[key] = value;
            else
                Instance._Register.Add(key, value);
        }
        public static void RegisterRemove(string key, string value)
        {
            _RegisterCheckKey(key);
            if (Instance._Register.ContainsKey(key))
                Instance._Register.Remove(key);
        }
        private static void _RegisterCheckKey(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key", "App.Register: given key cannot be empty string.");
        }
        private void _RegisterInit()
        {
            this._Register = new Dictionary<string, string>();
        }
        private Dictionary<string, string> _Register;
        #endregion
        #region App constants, paths
        /// <summary>
        /// Name of company.
        /// Is used as part of path in AppLocalDataPath, and for Windows register keys.
        /// </summary>
        public static string AppCompanyName
        { get { return "Asseco Solutions"; } }
        /// <summary>
        /// Name of application.
        /// Is used as part of path in AppLocalDataPath, and for Windows register keys.
        /// </summary>
        public static string AppProductName
        { get { return "GraphUtility"; } }
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
        #endregion
    }
}
