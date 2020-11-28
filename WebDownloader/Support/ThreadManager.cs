using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;

namespace Djs.Tools.WebDownloader.Support
{
    public class ThreadManager
    {
        #region Fronta požadavků na zpracování akce na pozadí
        public static IActionInfo AddAction(Action actionRun, Action actionDone = null)
        {
            ActionInfo actionInfo = new ActionInfo(actionRun, null, null, actionDone, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }

        public static IActionInfo AddAction(Action actionRun, Action<object[]> actionDoneArgs, params object[] doneArguments)
        {
            ActionInfo actionInfo = new ActionInfo(actionRun, null, null, null, actionDoneArgs, doneArguments);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }

        public static IActionInfo AddAction(Action<object[]> actionRunArgs, params object[] runArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionRunArgs, runArguments, null, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }

        public static IActionInfo AddAction(Action<object[]> actionRunArgs, Action actionDone, params object[] runArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionRunArgs, runArguments, actionDone, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(Action<object[]> actionRunArgs, object[] runArguments, Action<object[]> actionDoneArgs, object[] doneArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionRunArgs, runArguments, null, actionDoneArgs, doneArguments);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }


        /// <summary>
        /// Do fronty akcí přidá další akci a zařídí její spuštění, pokud to lze
        /// </summary>
        /// <param name="actionInfo"></param>
        private void _AddAction(ActionInfo actionInfo)
        {
            if (!actionInfo.IsValid)
                throw new InvalidOperationException("Invalid action to ThreadManager.AddAction: no method Run nor RunArgs!");

            lock (__ActionQueue)
            {
                actionInfo.Id = ++__ActionId;
                if (__LogActive) App.AddLog($"Add Action: {actionInfo}");
                __ActionQueue.Enqueue(actionInfo);
            }
            __ActionSignal.Set();                                    // To probudí thread __ActionThread, který čeká na přidání další akce do fronty v metodě _ActionWaitToAnyAction()...
            __ActionCaller.WaitOne(5);
        }
        /// <summary>
        /// Inicializace bloku pro spouštění akcí v různých threadech
        /// </summary>
        private void _ActionInit()
        {
            __ActionId = 0;
            __ActionQueue = new Queue<ActionInfo>();
            __ActionCaller = new AutoResetEvent(false);
            __ActionSignal = new AutoResetEvent(false);
            __ActionThread = new Thread(__ActionDispatcher) { Name = "ActionDispatcher", IsBackground = true, Priority = ThreadPriority.BelowNormal };
            __ActionThread.Start();
        }
        /// <summary>
        /// Smyčka vlákna, které je dispečerem spouštění akcí
        /// </summary>
        private void __ActionDispatcher()
        {
            while (!__IsStopped)
            {
                _ActionWaitToAnyAction();                            // Tady v té metodě čekám, než přijde nějaká práce...
                if (__IsStopped) break;                              // Nepřišla mi práce, ale konec aplikace!
                // Ve frontě máme alespoň jednu akci (=práce) => pak tedy potřebujeme disponibilní thread, aby bylo práci kde provádět:
                ThreadWrap threadWrap = __GetThread();               // Tady v té metodě čekám, než dostanu disponibilní thread k provedení nějaké práce
                if (__IsStopped || threadWrap == null) break;        // končíme?
                ActionInfo actionInfo = _ActionGetAction();          // Tady si vyzvednu tu práci, které jsem se dočkal (ona není zajíc, neutekla mi - neměla kudy)
                _ActionRunInThread(threadWrap, actionInfo);          // A tady konečně v přidělením threadu provedu nalezenou práci.
            }
        }
        /// <summary>
        /// Tato metoda vrátí řízení ihned, jakmile ve frontě <see cref="__ActionQueue"/> bude alespoň jedna požadovaná akce.
        /// Pokud ve frontě není žádná akce, tato metoda čeká a čeká a nic nedělá, čeká na signál <see cref="__ActionSignal"/>.
        /// </summary>
        /// <remarks>Tato metoda běží výhradně v threadu <see cref="__ActionThread"/>, je tedy synchronní s odebíráním akcí z fronty, a asynchronní s přidáváním.</remarks>
        private void _ActionWaitToAnyAction()
        {
            while (!__IsStopped)
            {
                if (__ActionQueue.Count > 0) break;
                if (__LogActive) App.AddLog($"Wait to any Action");
                __ActionSignal.WaitOne(3000);
            }
            __ActionCaller.Set();
        }
        /// <summary>
        /// Z fronty akcí vyjme akci k jejímu provedení a vrátí ji.
        /// </summary>
        /// <returns></returns>
        private ActionInfo _ActionGetAction()
        {
            ActionInfo actionInfo = null;
            lock (__ActionQueue)
            {
                if (__ActionQueue.Count > 0)
                    actionInfo = __ActionQueue.Dequeue();
            }
            if (__LogActive) App.AddLog($"Found Action {(actionInfo?.Id ?? 0)} to Run...");
            return actionInfo;
        }
        /// <summary>
        /// V dodaném threadu nastartuje dodanou akci.
        /// Tato metoda je vyvolána v threadu <see cref="__ActionThread"/>, ale akci spouští v threadu dodaném jako parametr.
        /// </summary>
        /// <param name="threadWrap"></param>
        /// <param name="actionInfo"></param>
        private void _ActionRunInThread(ThreadWrap threadWrap, ActionInfo actionInfo)
        {
            if (actionInfo != null)
                threadWrap.RunAction(actionInfo);
            else
                threadWrap.Release();
        }
        /// <summary>
        /// ID posledně přidané akce, příští bude mít +1
        /// </summary>
        private volatile int __ActionId;
        /// <summary>
        /// Fronta akcí ke zpracování
        /// </summary>
        private Queue<ActionInfo> __ActionQueue;
        private AutoResetEvent __ActionCaller;
        /// <summary>
        /// Signál k akci ve frontě akcí
        /// </summary>
        private AutoResetEvent __ActionSignal;
        /// <summary>
        /// Vlákno řídící frontu akcí
        /// </summary>
        private Thread __ActionThread;
        /// <summary>
        /// Informace k jedné uložené akci
        /// </summary>
        protected class ActionInfo : IActionInfo
        {
            public ActionInfo(Action actionRun, Action<object[]> actionRunArgs, object[] runArguments, Action actionDone, Action<object[]> actionDoneArgs, object[] doneArguments)
            {
                _ActionRun = actionRun;
                _ActionRunArgs = actionRunArgs;
                _RunArguments = runArguments;
                _ActionDone = actionDone;
                _ActionDoneArgs = actionDoneArgs;
                _DoneArguments = doneArguments;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Action: " + _Id;
            }
            public int _Id;
            private Action _ActionRun;
            private Action<object[]> _ActionRunArgs;
            private object[] _RunArguments;
            private Action _ActionDone;
            private Action<object[]> _ActionDoneArgs;
            private object[] _DoneArguments;

            public int Id { get { return _Id; } set { _Id = value; } }
            public Action ActionRun { get { return _ActionRun; } }
            public Action<object[]> ActionRunArgs { get { return _ActionRunArgs; }    }
            public object[] RunArguments { get { return _RunArguments; } }
            public Action ActionDone { get { return _ActionDone; } }
            public Action<object[]> ActionDoneArgs { get { return _ActionDoneArgs; } }
            public object[] DoneArguments { get { return _DoneArguments; } }
            public bool IsValid { get { return (_ActionRun != null || _ActionRunArgs != null); } }
            /// <summary>
            /// Provede požadovanou sérii akcí
            /// </summary>
            public void Run()
            {
                if (this.ActionRun != null) this.ActionRun();
                else if (this.ActionRunArgs != null) this.ActionRunArgs(this.RunArguments);

                if (this.ActionDone != null) this.ActionDone();
                else if (this.ActionDoneArgs != null) this.ActionDoneArgs(this.DoneArguments);

                Clear();
            }
            /// <summary>
            /// Zahodí reference na všechny akce i parametry
            /// </summary>
            public void Clear()
            {
                _ActionRun = null;
                _ActionRunArgs = null;
                _RunArguments = null;
                _ActionDone = null;
                _ActionDoneArgs = null;
                _DoneArguments = null;
            }
        }
        #endregion
        #region Public static prvky
        /// <summary>
        /// Spustí danou akci v threadu "na pozadí" = daná akce poběží v jiném vláknu, a toto vlákno vrátí řízení ihned a bude moci provádět cokoli potřebuje.
        /// Pokud právě běží všechny povolené thread (<see cref="MaxThreadCount"/>), pak tato metoda zablokuje aktuální thread a počká na uvolnění prvního pracovního threadu, 
        /// a ten pak obsadí pro danou akci.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="done"></param>
        [Obsolete("Přejdi na AddAction()", true)]
        public static void RunActionAsync(Action action, Action done = null)
        {
            ActionInfo actionInfo = new ActionInfo(action, null, null, null, null, null);
            Instance.__RunActionAsync(actionInfo);
        }
        /// <summary>
        /// Spustí danou akci v threadu "na pozadí" = daná akce poběží v jiném vláknu, a toto vlákno vrátí řízení ihned a bude moci provádět cokoli potřebuje.
        /// Pokud právě běží všechny povolené thread (<see cref="MaxThreadCount"/>), pak tato metoda zablokuje aktuální thread a počká na uvolnění prvního pracovního threadu, 
        /// a ten pak obsadí pro danou akci.
        /// </summary>
        /// <param name="actionArgs"></param>
        /// <param name="arguments"></param>
        [Obsolete("Přejdi na AddAction()", true)]
        public static void RunActionAsync(Action<object[]> actionArgs, params object[] arguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionArgs, arguments, null, null, null);
            Instance.__RunActionAsync(actionInfo);
        }
        /// <summary>
        /// Spustí danou akci v threadu "na pozadí" = daná akce poběží v jiném vláknu, a toto vlákno vrátí řízení ihned a bude moci provádět cokoli potřebuje.
        /// Pokud právě běží všechny povolené thread (<see cref="MaxThreadCount"/>), pak tato metoda zablokuje aktuální thread a počká na uvolnění prvního pracovního threadu, 
        /// a ten pak obsadí pro danou akci.
        /// </summary>
        /// <param name="actionArgs"></param>
        /// <param name="done"></param>
        /// <param name="arguments"></param>
        [Obsolete("Přejdi na AddAction()", true)]
        public static void RunActionAsync(Action<object[]> actionArgs, Action done, params object[] arguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionArgs, arguments, done, null, null);
            Instance.__RunActionAsync(actionInfo);
        }
        /// <summary>
        /// Zastaví všechny pracující thready a ukončí práci
        /// </summary>
        /// <param name="abortNow"></param>
        public static void StopAll(bool abortNow = false)
        {
            Instance.__StopAll(abortNow);
        }
        /// <summary>
        /// true pokud se loguje práce s thready
        /// </summary>
        public static bool LogActive { get { return Instance.__LogActive; } set { Instance.__LogActive = value; } }
        /// <summary>
        /// Max počet threadů. Při překročení dojde k chybě. Lze zadat počet 2 až 500 (včetně). Výchozí nastavení je dáno podle počtu procesorů.
        /// </summary>
        public static int MaxThreadCount { get { return Instance.__MaxThreadCount; } set { Instance.__MaxThreadCount = (value < 2 ? 2 : (value > 500 ? 500 : value)); } }

        private void __RunActionAsync(ActionInfo actionInfo)
        {
            ThreadWrap threadWrap = __GetThread();
            if (threadWrap != null)
                threadWrap.RunAction(actionInfo);
        }
        /// <summary>
        /// Najde volný thread / vytvoří nový thread / počká na uvolnění threadu a vrátí jej.
        /// Tato metoda tedy v případě potřeby čeká na uvolnění nějakého threadu a ten pak vrátí.
        /// Pouze v případě Stop může vrátit null.
        /// </summary>
        /// <returns></returns>
        private ThreadWrap __GetThread()
        {
            ThreadWrap threadWrap = null;
            bool logThread = false;
            while (true)
            {
                // Vynuluji semafor: pokud byl semafor nyní aktivní (=některý předchozí thread skončil a rozsvítil semafor), 
                //   pak v metodě __TryGetThread() najdu ten volný Thread a nepotřebuji na to semafor.
                // Pokud by thread volný nebyl, pak přejdu do WaitOne(), a tam by mě rozsvícený semafor rovnou pustil ven a testoval bych to znovu.
                __Semaphore.Reset();

                if (__TryGetThread(out threadWrap)) break;                               // Najde volný thread / vytvoří nový thread a vrátí jej.
                if (__IsStopped) break;                                                  // Končíme jako celek? Vrátíme null...
                App.AddLog($"ThreadManager Waiting for disponible thread, current ThreadCount: {__Threads.Count} ...");
                __Semaphore.WaitOne(3000);                                               //  ... počká na uvolnění některého threadu ...
                // Až některý thread skončí svoji práci, vyvolá svůj event ThreadDone, přijde k nám do handleru AnyThreadDone(), 
                // tam zazvoní budíček (__Semaphore) a my se vrátíme do this smyčky a zkusíme si vyzvednout uvolněný thread v příštím kole smyčky...
                // A protože jsme do logu dali info o čekání, dáme tam i nalezený thread:
                logThread = true;
            }
            if (__LogActive || logThread) App.AddLog($"Allocated thread: {threadWrap}");
            return threadWrap;
        }

        private bool __TryGetThread(out ThreadWrap threadWrap)
        {
            threadWrap = null;
            if (__IsStopped) return false;

            lock (__Threads)
            {
                var threadList = __Threads.ToList();
                if (threadList.Count > 1) threadList.Sort(ThreadWrap.CompareForAllocate);          // Setřídíme tak, že na začátku budou nejstarší volné thready
                threadWrap = threadList.FirstOrDefault(t => t.TryAllocate());                      // Najdeme první thread, který je možno alokovat a rovnou jej Alokujeme
                if (threadWrap != null)
                {
                    if (__LogActive) App.AddLog($"Allocated existing thread: {threadWrap}");
                }
                else
                {
                    int count = __Threads.Count;
                    if (count < __MaxThreadCount)
                    {   // Můžeme ještě přidat další thread:
                        string name = $"ThreadInPool{(count + 1)}";
                        threadWrap = new ThreadWrap(this, name, ThreadWrapState.Allocated);       // Vytvoříme nový thread, a rovnou jako Alokovaný
                        threadWrap.ThreadDisponible += AnyThreadDone;
                        __Threads.Add(threadWrap);
                        if (__LogActive) App.AddLog($"Created new thread: {threadWrap}");
                    }
                    // Pokud již nemůžeme přidat další thread a všechny existující jsou právě nyní obsazené, 
                    //  pak vrátíme false a nadřízená metoda počká ve smyčce (s pomocí semaforu) na uvolnění některého threadu.
                }
            }
            return (threadWrap != null);
        }
        /// <summary>
        /// Handler události z threadu, kdy thread dokončil akci a stává se disponibilním...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnyThreadDone(object sender, EventArgs e)
        {
            __Semaphore.Set();
        }
        /// <summary>
        /// Zastaví všechny thready
        /// </summary>
        /// <param name="abortNow"></param>
        private void __StopAll(bool abortNow = false)
        {
            lock (__Threads)
            {
                foreach (var threadWrap in __Threads)
                {
                    threadWrap.Stop(abortNow);
                    ((IDisposable)threadWrap).Dispose();
                }
                __Threads.Clear();
                __IsStopped = true;
                __Semaphore.Set();
            }
        }
        #endregion
        #region Singleton, konstruktor
        /// <summary>
        /// Singleton
        /// </summary>
        protected static ThreadManager Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (__Locker)
                    {
                        if (__Instance == null)
                            __Instance = new ThreadManager();
                    }
                }
                return __Instance;
            }
        }
        /// <summary>
        /// Jediná instance
        /// </summary>
        private static ThreadManager __Instance;
        /// <summary>
        /// Zámek pro tvorbu singletonu
        /// </summary>
        private static object __Locker = new object();
        /// <summary>
        /// Konstruktor
        /// </summary>
        private ThreadManager()
        {
            __Threads = new List<ThreadWrap>();
            __MaxThreadCount = 8 + 32 * Environment.ProcessorCount;
            __Semaphore = new AutoResetEvent(false);
            __IsStopped = false;
            _ActionInit();
        }
        private List<ThreadWrap> __Threads;
        private int __MaxThreadCount;
        private AutoResetEvent __Semaphore;
        private bool __IsStopped;
        private bool __LogActive;
        #endregion
        #region class ThreadWrap
        protected class ThreadWrap : IDisposable
        {
            #region Konstruktor a proměnné
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="name"></param>
            /// <param name="initialState"></param>
            public ThreadWrap(ThreadManager owner, string name, ThreadWrapState initialState = ThreadWrapState.Disponible)
            {
                __Owner = owner;
                __Lock = new SpinLock();
                __Semaphore = new AutoResetEvent(false);
                __State = initialState;
                __DisponibleFrom = DateTime.UtcNow;
                __End = false;
                __Name = name;
                __Thread = new Thread(__Loop) { IsBackground = true, Name = name, Priority = ThreadPriority.BelowNormal };
                __Thread.Start();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"{Name} [{State}]";
            }
            private ThreadManager __Owner;
            private bool __LogActive { get { return __Owner.__LogActive; } }
            private SpinLock __Lock;
            private AutoResetEvent __Semaphore;
            private volatile ThreadWrapState __State;
            private bool __End;
            private DateTime __DisponibleFrom;
            private Thread __Thread;
            private string __Name;
            private volatile ActionInfo __ActionInfo;
            public string Name { get { return __Name; } }
            #endregion
            #region Kód volaný v aplikačním threadu
            /// <summary>
            /// Metoda zkusí nastavit stav <see cref="ThreadWrapState.Allocated"/>, pokud je objekt ve stavu <see cref="ThreadWrapState.Disponible"/>, pak vrací true.
            /// Jinak vrací false = objekt nelze alokovat. Interně probíhá pod zámkem.
            /// </summary>
            /// <returns></returns>
            public bool TryAllocate()
            {
                if (IsEnding) return false;

                bool isAllocated = false;
                bool lockTaken = false;
                try
                {
                    __Lock.Enter(ref lockTaken);

                    var state = __State;
                    if (!__End && __State == ThreadWrapState.Disponible)
                    {
                        if (__LogActive) App.AddLog($"Allocate current thread {this}");
                        __State = ThreadWrapState.Allocated;
                        isAllocated = true;
                    }
                }
                finally
                {
                    if (lockTaken)
                        __Lock.Exit();
                }
                return isAllocated;
            }
            /// <summary>
            /// V libovolném threadu (v jiném než na pozadí) požádá o provedené dané akce na pozadí (v this threadu).
            /// </summary>
            /// <param name="action"></param>
            /// <param name="actionArgs"></param>
            /// <param name="arguments"></param>
            /// <param name="done"></param>
            public void RunAction(ActionInfo actionInfo)
            {
                if (!actionInfo.IsValid)
                    throw new InvalidOperationException("Invalid action to ThreadManager.AddAction: no method Run nor RunArgs!");

                bool lockTaken = false;
                try
                {
                    __Lock.Enter(ref lockTaken);

                    var state = __State;
                    if (state != ThreadWrapState.Allocated)
                        throw new InvalidOperationException($"ThreadWrap.RunAction error: invalid state for AddAction in thread {this}.");

                    __ActionInfo = actionInfo;
                    __State = ThreadWrapState.WaitToRun;
                }
                finally
                {
                    if (lockTaken)
                        __Lock.Exit();
                }

                __Semaphore.Set();                   // Požádáme thread na pozadí o vykonání akce.
            }
            /// <summary>
            /// Zajistí uvolnění threadu ze stavu <see cref="ThreadWrapState.Allocated"/> do stavu <see cref="ThreadWrapState.Disponible"/>.
            /// Volá se tehdy, když je thread získán (alokován), ale nebude použit (nebude v něm spuštěna žádná akce). 
            /// Pak je třeba jej uvolnit obdobně, jako je uvolněn po doběhnutí akce.
            /// </summary>
            public void Release()
            {
                bool isReleased = false;
                bool lockTaken = false;
                try
                {
                    __Lock.Enter(ref lockTaken);

                    var state = __State;
                    if (state == ThreadWrapState.Allocated)
                    {
                        if (__LogActive) App.AddLog($"Thread {this} : Released");
                        __DisponibleFrom = DateTime.UtcNow;
                        __State = ThreadWrapState.Disponible;
                        isReleased = true;
                    }
                }
                finally
                {
                    if (lockTaken)
                        __Lock.Exit();
                }

                if (isReleased)
                {
                    if (__LogActive) App.AddLog($"Thread {this} : Disponible");
                    OnThreadDisponible();
                }
            }
            #endregion
            #region Kód běžící na pozadí
            /// <summary>
            /// Permanentní smyčka běžící na pozadí
            /// </summary>
            private void __Loop()
            {
                while (!__End)
                {
                    if (IsEnding) break;
                    _TryRunAction();
                    if (IsEnding) break;
                    __Semaphore.WaitOne(10000);
                }
            }
            /// <summary>
            /// Ve zdejším threadu (na pozadí) provede určitou akci.
            /// Metoda je volána po vydání signálu <see cref="__Semaphore"/> i po jeho timeoutu.
            /// </summary>
            private void _TryRunAction()
            {
                ActionInfo actionInfo = null;
                bool needRun = false;
                bool lockTaken = false;
                try
                {   // Pod zámkem načtu požadované akce, vyhodnotím a nastavím stav Working:
                    __Lock.Enter(ref lockTaken);

                    actionInfo = __ActionInfo;
                    needRun = actionInfo?.IsValid ?? false;
                    if (needRun)
                        __State = ThreadWrapState.Working;

                    __ActionInfo = null;
                }
                finally
                {
                    if (lockTaken)
                        __Lock.Exit();
                }

                if (needRun)
                {   // Běh aplikační akce probíhá už bez zámku:
                    try
                    {
                        if (__LogActive) App.AddLog($"Thread {this} : RunAction");
                        actionInfo.Run();
                        if (__LogActive) App.AddLog($"Thread {this} : DoneAction");
                    }
                    catch (Exception exc) { App.AddLog(exc); }
                    finally
                    {
                        __DisponibleFrom = DateTime.UtcNow;
                        __State = ThreadWrapState.Disponible;
                        if (__LogActive) App.AddLog($"Thread {this} : Disponible");
                        OnThreadDisponible();
                    }
                }
            }
            protected bool IsEnding { get { var s = __State; return (__End || s == ThreadWrapState.Abort); } }
            protected virtual void OnThreadDisponible()
            {
                ThreadDisponible?.Invoke(this, EventArgs.Empty);
            }
            public event EventHandler ThreadDisponible;
            #endregion
            #region Stop a Abort a Dispose
            /// <summary>
            /// Stav threadu
            /// </summary>
            public ThreadWrapState State { get { return __State; } }
            /// <summary>
            /// Zastaví thread
            /// </summary>
            /// <param name="abortNow"></param>
            public void Stop(bool abortNow)
            {
                __End = true;
                __Semaphore.Set();
                if (abortNow)
                    __Abort();
            }
            private void __Abort()
            {
            }
            /// <summary>
            /// Dispose
            /// </summary>
            void IDisposable.Dispose()
            {
                __ActionInfo = null;
            }
            #endregion
            #region Support
            /// <summary>
            /// Komparátor pro třídění threadů podle délky času, po který je thread disponibilní.
            /// Seznam tříděný tímto komparátorem bude mít na pozici 0 takový thread, který je k dispozici po nejdelší dobu, který se už kouše nudou a strašně rád by zase pracoval.
            /// Na posledních pozicích seznamu budou thready s časem = 0, tedy ty které dosud pracují.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static int CompareForAllocate(ThreadWrap a, ThreadWrap b)
            {
                long at = a.DisponibleTimeTicks;
                long bt = b.DisponibleTimeTicks;
                return bt.CompareTo(at);
            }
            /// <summary>
            /// Obsahuje počet Ticků času, po který je this thread volně k použití. Pokud stave není <see cref="ThreadWrapState.Disponible"/>, obsahuje 0.
            /// </summary>
            protected long DisponibleTimeTicks { get { return (__State == ThreadWrapState.Disponible ? (DateTime.UtcNow.Ticks - __DisponibleFrom.Ticks) : 0L); } }
            #endregion
        }
        #endregion
    }
    #region enum ThreadWrapState
    /// <summary>
    /// Stavy threadu
    /// </summary>
    public enum ThreadWrapState
    {
        None,
        /// <summary>
        /// Po inicializaci, kdy je thread k dispozici v poolu.
        /// </summary>
        Disponible,
        /// <summary>
        /// Poté, kdy thread byl vybrán jako vhodný pro nový požadavek aplikačního kódu.
        /// V tomto stavu ještě nemá vloženou akci ani nepracuje, ale toto se již očekává.
        /// V tomto stavu smí být vložena akce a spuštěn její běh, ale thread již nesmí být vybrán pro další požadavek až do doby, kdy dokončí běh akce.
        /// </summary>
        Allocated,
        /// <summary>
        /// Po platném vložení akce, v očekávání spuštění - tedy v době, kdy probíhá mezivláknové přepnutí z AddAction (v aplikačním threadu) do RunAction (ve výkonném threadu).
        /// </summary>
        WaitToRun,
        /// <summary>
        /// Probíhá aplikační akce
        /// </summary>
        Working,
        /// <summary>
        /// Probíhá ukončení života threadu, již se nemá přidělovat další aktivita.
        /// Pokud je thread v tomto stavu, do jiného se už nedostane = lze testovat bez zámku.
        /// </summary>
        Abort
    }
    #endregion
    public interface IActionInfo
    { }
}

#region testy
namespace Djs.Tools.WebDownloader.Tests
{
    using Djs.Tools.WebDownloader.Support;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ThreadManagerTests
    {
        [TestMethod]
        public void TestDataRun()
        {
            System.Windows.Forms.Clipboard.Clear();

            Thread.CurrentThread.Name = "MainThread";
            App.AddLog($"Start aplikace");

            for (int r = 0; r < 60; r++)
            {
                App.AddLog($"Řádek {r}", "Test");
                for (int t = 0; t < 100; t++)              // 100 cyklů = 8 mikrosecs
                {
                    double a = Math.Cos(0.45d * Math.PI);
                    double b = Math.Sin(0.45d * Math.PI);
                }

                // System.Threading.Thread.Sleep(10);
            }

            var text = App.LogText;
            System.Windows.Forms.Clipboard.SetText(text);
        }
        [TestMethod]
        public void TestThreadManager()
        {
            System.Windows.Forms.Clipboard.Clear();
            ThreadManager.MaxThreadCount = 4;
            ThreadManager.LogActive = true;
            Rand = new Random();

            Thread.CurrentThread.Name = "MainThread";
            App.AddLog($"Start aplikace");

            int actionCount = 1024;

            try
            {
                actionCount = 500;
                for (int r = 1; r <= actionCount; r++)
                {
                    string code = "Main";
                    string name = $"{r}/{code}";
                    App.AddLog($"Spouštíme akci {name}");
                    ThreadManager.AddAction(_TestDataOneAction, r, code);
                    App.AddLog($"Spuštěna akce {name}");
                    if ((r % 40) == 0)   // Rand.Next(50) < 3)
                    {
                        int wait = Rand.Next(5, 10);
                        App.AddLog($"Počkáme čas {wait} ms");
                        Thread.Sleep(wait);
                    }
                }
            }

            catch (Exception exc)
            {
                App.AddLog(exc);
            }
            finally
            {
                App.AddLog($"Konec aplikace");
            }

            var text = App.LogText;
            System.Windows.Forms.Clipboard.SetText(text);
        }
        private void _TestDataOneAction(object[] arguments)
        {
            int number = (int)arguments[0];
            string code = (string)arguments[1];
            string name = $"{number}/{code}";

            int time = Rand.Next(400, 2000);               // Cílový čas v mikrosekundách
            int count = 100 * time / 8;                    // 100 cyklů = 8 mikrosecs
            App.AddLog($"Zahájení výpočtů, akce {name}, počet výpočtů {count}, očekávaný čas {time}");

            for (int t = 0; t < count; t++)
            {
                double a = Math.Cos(0.45d * Math.PI);
                double b = Math.Sin(0.45d * Math.PI);
            }

            if (code == "Main")
            {
                string subCode = "Inner";
                string subName = $"{number}/{subCode}";
                App.AddLog($"Spouštíme akci {subName}");
                ThreadManager.AddAction(_TestDataOneAction, number, subCode);
                App.AddLog($"Spuštěna akce {subName}");
            }
            App.AddLog($"Dokončení výpočtů, akce {name}");
        }
        private Random Rand;
    }
}
#endregion
