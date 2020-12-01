using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;

namespace Djs.Tools.WebDownloader.Support
{
    /// <summary>
    /// ThreadManager : slouží ke spouštění akcí ve vláknech na pozadí = asynchronně.
    /// Lze vyvolat akci bez parametr/s parametry i na závěr pak akci po doběhnutí.
    /// Lze řídit počet používaných threadů.
    /// Režijní časy jsou v řádu mikrosekund.
    /// </summary>
    public class ThreadManager
    {
        #region Public static prvky
        /// <summary>
        /// Maximální počet threadů, v nichž se paralelně zpracovávají akce.
        /// Jakmile se zadá více akcí než toto maximum, pak nově zadané akce čekají ve frontě, až některé z běžících akcí skončí - a nejstarší čekající akce pak využije její uvolněný thread.
        /// Lze zadat počet 2 až 500 (včetně). 
        /// Výchozí nastavení je dáno podle počtu procesorů.
        /// </summary>
        public static int MaxThreadCount { get { return Instance.__MaxThreadCount; } set { Instance.__MaxThreadCount = (value < 2 ? 2 : (value > 500 ? 500 : value)); } }
        /// <summary>
        /// true pokud se loguje práce s thready
        /// </summary>
        public static bool LogActive { get { return Instance.__LogActive; } set { Instance.__LogActive = value; } }
        /// <summary>
        /// Zastaví všechny pracující thready a ukončí práci.
        /// Po zastavení nebude manager provádět žádné další požadované akce.
        /// </summary>
        /// <param name="abortNow"></param>
        public static void StopAll(bool abortNow = false) { Instance.__StopAll(abortNow); }
        /// <summary>
        /// Příznak, že tento manager už zastavil svoji práci, po metodě <see cref="StopAll(bool)"/>.
        /// </summary>
        public static bool IsStopped { get { return Instance.__IsStopped; } }
        #endregion
        #region Fronta požadavků na zpracování akce na pozadí
        public static IActionInfo AddAction(Action actionRun, Action actionDone = null)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionRun, null, null, actionDone, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(Action actionRun, Action<object[]> actionDoneArgs, params object[] doneArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, actionRun, null, null, null, actionDoneArgs, doneArguments);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(Action<object[]> actionRunArgs, params object[] runArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, null, actionRunArgs, runArguments, null, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(Action<object[]> actionRunArgs, Action actionDone, params object[] runArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, null, actionRunArgs, runArguments, actionDone, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(Action<object[]> actionRunArgs, object[] runArguments, Action<object[]> actionDoneArgs, object[] doneArguments)
        {
            ActionInfo actionInfo = new ActionInfo(null, null, actionRunArgs, runArguments, null, actionDoneArgs, doneArguments);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }

        public static IActionInfo AddAction(string name, Action actionRun, Action actionDone = null)
        {
            ActionInfo actionInfo = new ActionInfo(name, actionRun, null, null, actionDone, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(string name, Action actionRun, Action<object[]> actionDoneArgs, params object[] doneArguments)
        {
            ActionInfo actionInfo = new ActionInfo(name, actionRun, null, null, null, actionDoneArgs, doneArguments);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(string name, Action<object[]> actionRunArgs, params object[] runArguments)
        {
            ActionInfo actionInfo = new ActionInfo(name, null, actionRunArgs, runArguments, null, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(string name, Action<object[]> actionRunArgs, Action actionDone, params object[] runArguments)
        {
            ActionInfo actionInfo = new ActionInfo(name, null, actionRunArgs, runArguments, actionDone, null, null);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }
        public static IActionInfo AddAction(string name, Action<object[]> actionRunArgs, object[] runArguments, Action<object[]> actionDoneArgs, object[] doneArguments)
        {
            ActionInfo actionInfo = new ActionInfo(name, null, actionRunArgs, runArguments, null, actionDoneArgs, doneArguments);
            Instance._AddAction(actionInfo);
            return actionInfo;
        }


        /// <summary>
        /// Obsahuje true, pokud aktuálně běžící kód běží ve vláknu, které je spravováno jako vlákno pro spouštěné akce.
        /// Tedy, pokud tuto hodnotu čteme z vlákna provádějícího akci, obsahuje true, z jiných vláken obsahuje false.
        /// </summary>
        public static bool IsInAnyWorkingThread { get { return Instance.__IsInAnyWorkingThread; } }
        /// <summary>
        /// Metoda pozastaví provádění aktuálního threadu až do doby, kdy doběhne poslední z akcí ve frontě. Teprve poté vrátí řízení.
        /// Thread je blokován nenásilně.
        /// <para/>
        /// POZOR, DŮLEŽITÉ UPOZORNĚNÍ:
        /// Tuto metodu nesmíme volat z vlákna, které právě provádí některou akci, protože v této metodě se čeká a čeká na doběhnutí akce = tedy sebe sama, a proto se nikdy nedočkáme.
        /// <para/>
        /// </summary>
        public static void WaitToActionsDone(TimeSpan? timeout = null) { Instance.__WaitToActionsDone(timeout); }

        public static void WaitToActionsDone(IActionInfo action, TimeSpan? timeout = null)
        { }
        /// <summary>
        /// Inicializace bloku pro spouštění akcí v různých threadech
        /// </summary>
        private void _ActionInit()
        {
            __ActionId = 0;
            __ActionQueue = new Queue<ActionInfo>();
            __ActionAcceptedSignal = SignalFactory.Create(false, true);
            __NewActionIncomeSignal = SignalFactory.Create(false, true);
            __WaitToActionsDoneSignal = SignalFactory.Create(false, true);
            __WaitingToActionsDone = false;
            __ActionDispatcherThread = new Thread(__ActionDispatcherLoop) { Name = __ActionDispatcherName, IsBackground = true, Priority = ThreadPriority.AboveNormal };
            __ActionDispatcherThread.Start();
        }
        /// <summary>
        /// Do fronty akcí přidá další akci a zařídí její spuštění, pokud to lze.
        /// Tato metoda je prováděna v threadu aplikačním nebo výkonném, podle potřeby.
        /// </summary>
        /// <param name="actionInfo"></param>
        private void _AddAction(ActionInfo actionInfo)
        {
            if (__IsStopped)
                throw new InvalidOperationException("ThreadManager can not run any more Actions, is Stopped.");

            if (!actionInfo.IsValid)
                throw new InvalidOperationException("Invalid action to ThreadManager.AddAction: no method Run nor RunArgs!");

            lock (__ActionQueue)
            {
                if (__ActionId == Int32.MaxValue) __ActionId = 0;    // K tomuhle dojde maximálně 1x za 10 let, že - pane Chocholoušku :-)  -  ale jen, když nás příliš zásobujete, pane Karlík... :-D  ... Pokud stihneme dávat 6,8051103632714786902336971852488 akcí za 1 sekundu, tak Int32.Max opravdu přetečeme za 10 let.
                actionInfo.Id = ++__ActionId;
                actionInfo.Owner = this;
                if (__LogActive) App.AddLog("ThreadManager", $"Add {actionInfo}; Queue.Count: {(__ActionQueue.Count + 1)}");
                __ActionQueue.Enqueue(actionInfo);
                actionInfo.ActionState = ThreadActionState.WaitingInQueue;
            }
            if (__LogActive) App.AddLog("ThreadManager", $"Add {actionInfo}; Lock on Queue released, set signal NewActionIncome and AnyThreadDisponible ...");
            __ActionAcceptedSignal.Reset();                          // Smažeme staré signály, počkáme si na aktuální...
            __NewActionIncomeSignal.Set();                           // To probudí thread __ActionDispatcherThread, který možná čeká na přidání další akce do fronty v metodě _ActionWaitToAnyAction()...
            __AnyThreadDisponibleSignal.Set();                       // To probudí thread __ActionDispatcherThread, který možná čeká na nějaký disponibilní thread v metodě __GetDisponibleThread()...

            if (__LogActive) App.AddLog("ThreadManager", $"Add {actionInfo}; Waiting for ActionAccepted signal...");

            //  bez žádného kódu ...                       Nečeká vůbec, nepředá řízení do threadu Dispatcher
            // __ActionAcceptedSignal.WaitOne(1);          Občas zablokuje current thread na 15ms = nad rámec timeoutu, ale ihned předá řízení do Dispatcher a odstartuje provádění práce
            // Thread.Yield();                             Nepředá řízení do threadu Dispatcher
            // Thread.Sleep(1);                            Namísto 1ms čekání to trvá i 15ms
            // Thread.Sleep(0);                            Nečeká vůbec, nepředá řízení do threadu Dispatcher
            // Thread.SpinWait(10);                        Nepředá řízení do threadu Dispatcher

            __ActionAcceptedSignal.WaitOne(1);                       // Tenhle signál posílá metoda pro čtení akcí i pro získání threadu. Oběma metodám jsme poslali signál a nyní se provádějí.

            if (__LogActive) App.AddLog("ThreadManager", $"Add {actionInfo}; Done.");
        }
        /// <summary>
        /// Smyčka vlákna, které je dispečerem spouštění akcí.
        /// Tato metoda je prováděna v threadu <see cref="__ActionDispatcherThread"/>.
        /// </summary>
        private void __ActionDispatcherLoop()
        {
            while (!__IsStopped)
            {
                _ActionWaitToAnyAction();                            // Tady v té metodě čekám, než k nám přijde nějaká práce...  Buď tam nějaká práce už je (pak se vrátím hned) nebo čekám na signál __NewActionIncomeSignal.
                if (__IsStopped) break;                              // Nepřišla mi práce, ale konec aplikace!
                // Ve frontě máme alespoň jednu akci (=práce) => pak tedy potřebujeme disponibilní thread, aby bylo práci kde provádět:
                ThreadWrap threadWrap = __GetDisponibleThread();     // Tady v té metodě čekám, než dostanu disponibilní thread k provedení nějaké práce
                if (__IsStopped || threadWrap == null) break;        //  končíme?
                ActionInfo actionInfo = _ActionGetAction();          // Tady si vyzvednu tu práci, které jsem se dočkal (ona není zajíc, neutekla mi - neměla kudy)
                _ActionRunInThread(threadWrap, actionInfo);          // A tady konečně v přidělením threadu provedu nalezenou práci.
            }
        }
        /// <summary>
        /// Tato metoda vrátí řízení ihned, jakmile ve frontě <see cref="__ActionQueue"/> bude alespoň jedna požadovaná akce.
        /// Pokud ve frontě není žádná akce, tato metoda čeká a čeká a nic nedělá, čeká na signál <see cref="__NewActionIncomeSignal"/>.
        /// </summary>
        /// <remarks>Tato metoda běží výhradně v threadu <see cref="__ActionDispatcherThread"/>, je tedy synchronní s odebíráním akcí z fronty, a asynchronní s přidáváním.</remarks>
        private void _ActionWaitToAnyAction()
        {
            while (!__IsStopped)
            {
                if (__LogActive) App.AddLog("ThreadManager", $"Test any Action...");
                if (__ActionQueueLockCount > 0) break;
                if (__LogActive) App.AddLog("ThreadManager", $"Wait to any Action...");
                __NewActionIncomeSignal.WaitOne(3000);
            }
            if (__LogActive) App.AddLog("ThreadManager", $"Any action exists, set signal ActionAccepted.");
            __ActionAcceptedSignal.Set();
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
                {
                    actionInfo = __ActionQueue.Dequeue();
                    actionInfo.ActionState = ThreadActionState.WaitingToThread;
                }
            }
            if (__LogActive) App.AddLog("ThreadManager", $"Found {actionInfo} to Run; Queue.Count: {(__ActionQueue.Count + 1)}");
            return actionInfo;
        }
        /// <summary>
        /// V dodaném threadu nastartuje dodanou akci.
        /// Tato metoda je vyvolána v threadu <see cref="__ActionDispatcherThread"/>, ale akci spouští v threadu dodaném jako parametr.
        /// </summary>
        /// <param name="threadWrap"></param>
        /// <param name="actionInfo"></param>
        private void _ActionRunInThread(ThreadWrap threadWrap, ActionInfo actionInfo)
        {
            if (actionInfo != null)
            {
                if (__LogActive) App.AddLog("ThreadManager", $"Run {actionInfo} in {threadWrap}");
                threadWrap.RunAction(actionInfo);
            }
            else
                threadWrap.Release();
        }
        /// <summary>
        /// Metoda je vyvolána z konkrétní akce v situaci, kdy akce sama doběhla do konce, včetně Done.
        /// Metoda je volána i po chybě v běhu akce.
        /// </summary>
        /// <param name="actionInfo"></param>
        protected void ActionCompleted(ActionInfo actionInfo)
        {
            if (__WaitingToActionsDone)
                __WaitToActionsDoneSignal.Set();
        }
        /// <summary>
        /// Metoda pozastaví provádění aktuálního threadu až do doby, kdy doběhne poslední z akcí ve frontě. Teprve poté vrátí řízení.
        /// </summary>
        /// <param name="timeout"></param>
        private void __WaitToActionsDone(TimeSpan? timeout = null)
        {
            if (__IsInAnyWorkingThread)
                throw new InvalidOperationException("ThreadManager can not do WaitToActionsDone() in same thread, that itself is running in Action thread. [Nemohu čekat ve vlákně 1 (volající) na to, až dokončí vlákno 1 (v ThreadPool) svoji práci, protože vlákno 1 jsem já a čekám - a nikdy neskončím]");

            DateTime? end = ((timeout.HasValue && timeout.Value.TotalSeconds > 0d) ? (DateTime?) DateTime.UtcNow.Add(timeout.Value) : (DateTime?)null);
            try
            {
                __WaitingToActionsDone = true;
                while (true)
                {
                    if (__RunningThreadCount == 0 && __ActionQueueCount == 0) break;
                    __WaitToActionsDoneSignal.WaitOne(1000);
                    if (end.HasValue && DateTime.UtcNow >= end.Value) break;
                }
            }
            finally
            {
                __WaitingToActionsDone = false;
            }
        }
        /// <summary>
        /// ID posledně přidané akce, příští bude mít +1
        /// </summary>
        private volatile int __ActionId;
        /// <summary>
        /// Fronta akcí ke zpracování
        /// </summary>
        private Queue<ActionInfo> __ActionQueue;
        /// <summary>
        /// Počet akcí, které čekají na zahájení zpracování.
        /// Pokud některá akce je již v procesu zpracovávání, pak už není započtena v <see cref="__ActionQueueCount"/>.
        /// </summary>
        private int __ActionQueueCount { get { return __ActionQueue.Count; } }
        /// <summary>
        /// Počet akcí, které čekají na zahájení zpracování. Získáno ze zamčeného objektu.
        /// Pokud některá akce je již v procesu zpracovávání, pak už není započtena v <see cref="__ActionQueueCount"/>.
        /// </summary>
        private int __ActionQueueLockCount
        {
            get
            {
                int count = 0;
                lock (__ActionQueue)
                {
                    count = __ActionQueue.Count;
                }
                return count;
            }
        }
        /// <summary>
        /// Signál od dispečera (thread <see cref="__ActionDispatcherThread"/>) do uživatelského vlákna, které podávalo žádost o zpracování akce, 
        /// že tato žádost byla akceptována dispečerem, a volající se může věnovat své práci.
        /// </summary>
        private ISignal __ActionAcceptedSignal;
        /// <summary>
        /// Signál pro dispečera zpracování fronty akcí, že byla vložena nějaká další akce jako požadavek na zpracování.
        /// Pokud vlákno 
        /// </summary>
        private ISignal __NewActionIncomeSignal;
        /// <summary>
        /// Příznak, že někdo čeká na ukončení akcí. Pak je třeba posílat signál <see cref="__WaitToActionsDoneSignal"/> po ukončení práce v threadu i po ukončení práce na akci, 
        /// aby čekající metoda mohla prověřit aktuální stav.
        /// Pokud hodnota <see cref="__WaitingToActionsDone"/> je false, pak signál není třeba aktivovat.
        /// </summary>
        private volatile bool __WaitingToActionsDone;
        /// <summary>
        /// Signál aktivovaný v situaci, kdy <see cref="__WaitingToActionsDone"/> je true = někdo (metoda <see cref="__WaitToActionsDone(TimeSpan?)"/>) čeká na ukončení threadu a akcí.
        /// </summary>
        private ISignal __WaitToActionsDoneSignal;
        /// <summary>
        /// Instance threadu, který je dispečerem akcí
        /// </summary>
        private Thread __ActionDispatcherThread;
        /// <summary>
        /// Jméno threadu, který je dispečerem akcí
        /// </summary>
        private const string __ActionDispatcherName = "ActionDispatcherThread";
        #region class ActionInfo : obálka jedné akce ve frontě
        /// <summary>
        /// Informace k jedné uložené akci
        /// </summary>
        protected class ActionInfo : IActionInfo
        {
            public ActionInfo(string name, Action actionRun, Action<object[]> actionRunArgs, object[] runArguments, Action actionDone, Action<object[]> actionDoneArgs, object[] doneArguments)
            {
                _ActionName = name;
                _ActionState = ThreadActionState.Initialized;
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
                return "Action " + _Id + (!String.IsNullOrEmpty(_ActionName) ? $" \"{_ActionName}\"" : "");
            }
            private ThreadManager _Owner;
            private int _Id;
            private string _ActionName;
            private ThreadActionState _ActionState;
            private Action _ActionRun;
            private Action<object[]> _ActionRunArgs;
            private object[] _RunArguments;
            private Action _ActionDone;
            private Action<object[]> _ActionDoneArgs;
            private object[] _DoneArguments;
            private WeakReference<Thread> _WorkingThread;

            public ThreadManager Owner { get { return _Owner; } set { _Owner = value; } }
            public int Id { get { return _Id; } set { _Id = value; } }
            public ThreadActionState ActionState { get { return _ActionState; } set { _ActionState = value; } }
            public Action ActionRun { get { return _ActionRun; } }
            public Action<object[]> ActionRunArgs { get { return _ActionRunArgs; }    }
            public object[] RunArguments { get { return _RunArguments; } }
            public Action ActionDone { get { return _ActionDone; } }
            public Action<object[]> ActionDoneArgs { get { return _ActionDoneArgs; } }
            public object[] DoneArguments { get { return _DoneArguments; } }
            protected Thread WorkingThread
            {
                get
                {
                    var wt = _WorkingThread;
                    return (wt != null && wt.TryGetTarget(out Thread thread) ? thread : null);
                }
                set
                {
                    if (value == null) _WorkingThread = null;
                    else _WorkingThread = new WeakReference<Thread>(value);
                }
            }
            public bool IsValid { get { return (_ActionRun != null || _ActionRunArgs != null); } }
            /// <summary>
            /// Provede požadovanou sérii akcí
            /// </summary>
            public void Run()
            {
                try
                {
                    this.ActionState = ThreadActionState.Running;
                    this.WorkingThread = Thread.CurrentThread;

                    if (this.ActionRun != null) this.ActionRun();
                    else if (this.ActionRunArgs != null) this.ActionRunArgs(this.RunArguments);

                    if (this.ActionDone != null) this.ActionDone();
                    else if (this.ActionDoneArgs != null) this.ActionDoneArgs(this.DoneArguments);
                }
                catch (Exception exc)
                {
                    App.AddLog(exc);
                }
                finally
                {
                    this.ActionState = ThreadActionState.Completed;
                    this.WorkingThread = null;
                    this.Owner.ActionCompleted(this);
                    Clear();
                }
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
        #endregion
        #region Řízení vláken: získání, čekání, tvorba nových, zastavení všech
        /// <summary>
        /// Inicializace systému pracovních threadů
        /// </summary>
        private void _ThreadInit()
        {
            __Threads = new List<ThreadWrap>();
            __MaxThreadCount = 8 + 32 * Environment.ProcessorCount;
            __AnyThreadDisponibleSignal = SignalFactory.Create(false, true);
            __IsStopped = false;
        }
        /// <summary>
        /// Najde volný (disponibilní) thread / vytvoří nový thread / počká na uvolnění threadu a zarezervuje si jej a vrátí jej.
        /// Tato metoda tedy v případě potřeby čeká na uvolnění nějakého threadu a ten pak vrátí.
        /// Pouze v případě Stop může vrátit null.
        /// Tato metoda probíhá v threadu 
        /// </summary>
        /// <returns></returns>
        private ThreadWrap __GetDisponibleThread()
        {
            if (__IsStopped) return null;

            if (__LogActive) App.AddLog("ThreadManager", $"Search for disponible thread, current ThreadCount: {__Threads.Count} ...");
            ThreadWrap threadWrap = null;
            bool logThread = false;
            while (true)
            {
                // Vynuluji semafor: pokud byl semafor nyní aktivní (=některý předchozí thread skončil a rozsvítil semafor), 
                //   pak v metodě __TryGetThread() najdu ten volný Thread a nepotřebuji na to semafor.
                // Pokud by thread volný nebyl, pak přejdu do WaitOne(), a tam by mě rozsvícený semafor rovnou pustil ven a testoval bych to znovu.
                __AnyThreadDisponibleSignal.Reset();

                if (__TryGetDisponibleThread(out threadWrap)) break;                               // Najde volný thread / vytvoří nový thread a vrátí jej. Podmínečně píše do logu způsob získání threadu
                if (__IsStopped) break;                                                            // Končíme jako celek? Vrátíme null...
                App.AddLog("ThreadManager", $"ThreadManager Waiting for disponible thread, current ThreadCount: {__Threads.Count} ...");        // Tohle loguju povinně...
                __AnyThreadDisponibleSignal.WaitOne(3000);                                         //  ... počká na uvolnění některého threadu ... (anebo na signál o nové akci)

                // Zvenku zadaná akce (v metodě _AddAction(ActionInfo actionInfo)) přidala novou akci, poslala signály a AnyThreadDisponible) a nyní čeká na signál ActionAccepted.
                // Pošleme jí signál ať nečeká:
                __ActionAcceptedSignal.Set();


                // Až některý thread skončí svoji práci, vyvolá svůj event ThreadDisponible, ten přijde k nám do handleru __AnyThreadDisponible(), 
                // tam se - ve výkonném threadu - zazvoní na budíček (__AnyThreadDisponibleSemaphore) a ten probudí náš thread a my se dostáváme zase do this smyčky 
                //  a zkusíme si vyzvednout uvolněný thread v příštím kole smyčky...
                // A protože jsme do logu dali info o čekání, dáme tam i nalezený thread:
                logThread = true;
            }
            if (!__LogActive && logThread) App.AddLog("ThreadManager", $"Allocated: {threadWrap}");// Tohle loguju jen když není log aktivní a čekali jsme na thread, to se loguje povinně, ale neloguje se druh získání threadu...
            return threadWrap;
        }
        /// <summary>
        /// Zkusí získat disponibilní thread pro nějakou práci (do out parametru <paramref name="threadWrap"/>), a vrátí true = máme jej
        /// Anebo vrátí false = nejsou volné thready, a nelze přidat další, dosáhli jsme limit, a musíme počkat na uvolnění některého existujícího.
        /// </summary>
        /// <param name="threadWrap"></param>
        /// <returns></returns>
        private bool __TryGetDisponibleThread(out ThreadWrap threadWrap)
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
                    if (__LogActive) App.AddLog("ThreadManager", $"Allocated existing: {threadWrap}");
                }
                else
                {
                    int count = __Threads.Count;
                    if (count < __MaxThreadCount)
                    {   // Můžeme ještě přidat další thread:
                        string name = $"ThreadInPool{(count + 1)}";
                        if (__LogActive) App.AddLog("ThreadManager", $"Preparing new thread: {name}");
                        threadWrap = new ThreadWrap(this, name, ThreadWrapState.Allocated);       // Vytvoříme nový thread, a rovnou jako Alokovaný
                        __Threads.Add(threadWrap);
                        if (__LogActive) App.AddLog("ThreadManager", $"Created new: {threadWrap}");
                    }
                    // Pokud již nemůžeme přidat další thread a všechny existující jsou právě nyní obsazené, 
                    //  pak vrátíme false a nadřízená metoda počká ve smyčce (s pomocí semaforu __AnyThreadDisponibleSemaphore) na uvolnění některého threadu.
                }
            }
            return (threadWrap != null);
        }
        /// <summary>
        /// Událost volaná z Threadu poté, kdy thread dokončil akci a stává se disponibilním...
        /// Metodu volá <see cref="ThreadWrap"/> přímo, nejde o eventhandler.
        /// </summary>
        /// <param name="threadWrap"></param>
        protected void ThreadDisponible(ThreadWrap threadWrap)
        {
            // Pokud počet aktuálních threadů je větší než aktuálně nastavené Maximum, pak právě nyní je vhodná chvíle na zmenšení pole threadů o tento jeden, který doběhl:
            bool isThreadRemoved = _ThreadsReduceCount(threadWrap);

            if (!isThreadRemoved)
                __AnyThreadDisponibleSignal.Set();
            if (__WaitingToActionsDone)
                __WaitToActionsDoneSignal.Set();
        }
        /// <summary>
        /// Metoda zajistí, že počet aktuálních threadů se bude snižovat podle požadovaného maxima <see cref="MaxThreadCount"/>.
        /// Vrátí true, pokud právě dodaný thread byl odstraněn z pole, a nelze jej již dále používat.
        /// </summary>
        /// <param name="threadWrap"></param>
        /// <returns></returns>
        private bool _ThreadsReduceCount(ThreadWrap threadWrap)
        {
            int count = __Threads.Count;
            int max = __MaxThreadCount;
            int toRemove = count - max;                    // Tolik threadů potřebuji odebrat, nejprve určíme rychle = bez zámku! (Většinou není třeba měnit, to až když někdo za běhu sníží Max!)
            if (toRemove <= 0) return false;               // Není potřeba nic řešit.

            List<ThreadWrap> removedThreads = new List<ThreadWrap>();
            bool needTrackCurrent = (threadWrap != null);
            bool isRemovedCurrent = false;
            lock (__Threads)
            {   // V této době (lock) nedojde k přidání nového ani k využití žádného stávajícího threadu z pole __Threads:
                // Znovu určíme počty, nyní už spolehlivé = pod zámkem:
                count = __Threads.Count;
                toRemove = count - max;
                for (int i = (count - 1); (i >= 0 && toRemove > 0); i--)
                {
                    var thread = __Threads[i];
                    if (thread.State == ThreadWrapState.Disponible)
                    {
                        if (needTrackCurrent && thread.ManagedThreadId == threadWrap.ManagedThreadId)
                            isRemovedCurrent = true;
                        __Threads.RemoveAt(i);
                        removedThreads.Add(thread);
                        toRemove--;
                    }
                }
            }

            // Pokud jsme nějaké thready odebrali, tak je nyní ukončíme = vrátíme je operačnímu systému (skončí jejich výkonná metoda), ale nebudu je abortovat (doběhne jejich akce):
            foreach (var removedThread in removedThreads)
            {
                removedThread.Stop(false);
            }

            return isRemovedCurrent;
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně běžící kód běží ve vláknu, které je spravováno jako vlákno pro spouštěné akce.
        /// Tedy, pokud tuto hodnotu čteme z vlákna provádějícího akci, obsahuje true, z jiných vláken obsahuje false.
        /// </summary>
        private bool __IsInAnyWorkingThread { get { return __TryGetCurrentThread(out ThreadWrap threadWrap); } }
        /// <summary>
        /// Metoda zkusí získat obálku threadu, který se právě provádí. 
        /// Úspěšná je tato metoda pouze tehdy, když aktuální thread je jedním z těch threadů, které jsou obsluhovány tímto ThreadManagerem.
        /// </summary>
        /// <param name="threadWrap"></param>
        /// <returns></returns>
        private bool __TryGetCurrentThread(out ThreadWrap threadWrap)
        {
            List<ThreadWrap> threadList = null;
            lock (__Threads)
            {
                threadList = __Threads.ToList();
            }
            threadWrap = threadList.FirstOrDefault(t => t.IsCurrentThread);
            return (threadWrap != null);
        }
        /// <summary>
        /// Obsahuje počet vláken, které aktuálně pracují nebo se k tomu chystají = vlákna, jejichž <see cref="ThreadWrap.IsRunning"/> je true.
        /// </summary>
        private int __RunningThreadCount
        {
            get
            {
                List<ThreadWrap> threadList = null;
                lock (__Threads)
                {
                    threadList = __Threads.ToList();
                }
                return threadList.Count(t => t.IsRunning);
            }
        }
        /// <summary>
        /// Zastaví všechny thready
        /// </summary>
        /// <param name="abortNow"></param>
        private void __StopAll(bool abortNow = false)
        {
            if (__LogActive) App.AddLog("ThreadManager", $"Stop all threads...");
            lock (__Threads)
            {
                foreach (var threadWrap in __Threads)
                {
                    threadWrap.Stop(abortNow);
                    ((IDisposable)threadWrap).Dispose();
                }
                __Threads.Clear();
                __IsStopped = true;
                __AnyThreadDisponibleSignal.Set();
            }
            if (__LogActive) App.AddLog("ThreadManager", $"All threads is stopped.");
        }
        /// <summary>
        /// Soupis dosud vytvořených threadů.
        /// </summary>
        private List<ThreadWrap> __Threads;
        /// <summary>
        /// Maximální počet threadů. 
        /// Lze kdykoliv změnit, systém při snížení hodnoty nechá běžící thready doběhnout a teprve poté je odebere z disponibilního soupisu <see cref="__Threads"/>.
        /// </summary>
        private volatile int __MaxThreadCount;
        /// <summary>
        /// Signál aktivovaný po dokončení běhu v některém threadu (v <see cref="__Threads"/>), kdy některý thread je k dispozici.
        /// Pokud thread manager čeká na uvolnění threadu (v metodě <see cref="__GetDisponibleThread()"/>), pak aktivací tohoto signálu je jeho čekání ukončeno 
        /// a manager může nalézt a použít toto disponibilní vlákno pro další akci.
        /// </summary>
        private ISignal __AnyThreadDisponibleSignal;
        /// <summary>
        /// Obsahuje true poté, kdy <see cref="ThreadManager"/> je zastaven a nebude již přijímat další požadavky na práci.
        /// </summary>
        private bool __IsStopped;
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
                __Semaphore = SignalFactory.Create(false, true);
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
                return $"Thread {Name} [{State}]";
            }
            private ThreadManager __Owner;
            private bool __LogActive { get { return __Owner.__LogActive; } }
            private SpinLock __Lock;
            private ISignal __Semaphore;
            private volatile ThreadWrapState __State;
            /// <summary>
            /// Nastavením na true dojde k ukončení vlákna poté, kdy doběhne případná běžící akce.
            /// K nastavení se používá metoda <see cref="Stop(bool)"/>.
            /// </summary>
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
                        if (__LogActive) App.AddLog("ThreadWrap", $"Allocate current: {this}");
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
                if (actionInfo == null || !actionInfo.IsValid)
                    throw new InvalidOperationException("Invalid action to ThreadManager.AddAction: no Action, or not method Run nor RunArgs!");

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
                        if (__LogActive) App.AddLog("ThreadWrap", $"{this} : Released");
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
                    if (__LogActive) App.AddLog("ThreadWrap", $"{this} : Disponible");
                    AfterThreadDisponible();
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

                if (needRun && !__End)
                {   // Běh aplikační akce probíhá už bez zámku:
                    try
                    {
                        if (__LogActive) App.AddLog("ThreadWrap", $"{this} : Run {actionInfo}");
                        actionInfo.Run();
                        if (__LogActive) App.AddLog("ThreadWrap", $"{this} : Done {actionInfo}");
                    }
                    catch (Exception exc) { App.AddLog(exc); }
                    finally
                    {
                        __DisponibleFrom = DateTime.UtcNow;
                        __State = ThreadWrapState.Disponible;
                        if (__LogActive) App.AddLog("ThreadWrap", $"{this} : Disponible");
                        AfterThreadDisponible();
                    }
                }
            }
            protected bool IsEnding { get { var s = __State; return (__End || s == ThreadWrapState.Abort); } }
            protected void AfterThreadDisponible()
            {
                __Owner.ThreadDisponible(this);
            }
            #endregion
            #region Stop a Abort a Dispose
            /// <summary>
            /// Stav threadu
            /// </summary>
            public ThreadWrapState State { get { return __State; } }
            /// <summary>
            /// Zastaví a ukončí thread.
            /// Pokud je <paramref name="abortNow"/> false, pak nechá doběhnout akci v threadu.
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
            /// Systémové ID threadu = <see cref="Thread.ManagedThreadId"/>
            /// </summary>
            public int ManagedThreadId { get { return __Thread.ManagedThreadId; } }
            /// <summary>
            /// Obsahuje true tehdy, když je vyhodnocováno v threadu, v němž se právě provádí akce tohoto threadu (="zevnitř"),
            /// nebo obsahuje false, pokud se na hodnotu ptá někdo z jiného threadu ("zvenku").
            /// </summary>
            public bool IsCurrentThread { get { return (Thread.CurrentThread.ManagedThreadId == __Thread.ManagedThreadId); } }
            /// <summary>
            /// Obsahuje true, pokud this thread je ve stavu reprezentujícím běh, nebo očekávaný běh: 
            /// <see cref="ThreadWrapState.Allocated"/> nebo <see cref="ThreadWrapState.WaitToRun"/> nebo <see cref="ThreadWrapState.Working"/>.
            /// </summary>
            public bool IsRunning { get { var s = __State; return (s == ThreadWrapState.Allocated || s == ThreadWrapState.WaitToRun || s == ThreadWrapState.Working); } }
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
            _ThreadInit();
            _ActionInit();
        }
        private bool __LogActive;
        #endregion
    }
    #region class Signal
    public static class SignalFactory
    {
        public static ISignal Create(bool initialState, bool autoReset)
        {
            if (UseMonitor) return new SignalMonitor(initialState, autoReset);
            return new SignalAutoReset(initialState, autoReset);
        }
        private const bool UseMonitor = true;
    }
    public interface ISignal
    {
        void Reset();
        void Set();
        void WaitOne();
        bool WaitOne(int timeout);
    }
    public class SignalAutoReset : ISignal
    {
        private readonly AutoResetEvent _AutoReset;
        private readonly bool _autoResetSignal;
        public SignalAutoReset()
          : this(false, false)
        {
        }

        public SignalAutoReset(bool initialState, bool autoReset)
        {
            _AutoReset = new AutoResetEvent(initialState);
            _autoResetSignal = autoReset;
        }

        public void Reset() { _AutoReset.Reset(); }
        public void Set() { _AutoReset.Set(); }
        public void WaitOne() { _AutoReset.WaitOne(); }
        public bool WaitOne(int milliseconds) { return _AutoReset.WaitOne(milliseconds); }

    }
    public class SignalMonitor : ISignal
    {
        private readonly object _lock = new object();
        private readonly bool _autoResetSignal;
        private bool _notified;

        public SignalMonitor()
          : this(false, false)
        {
        }

        public SignalMonitor(bool initialState, bool autoReset)
        {
            _notified = initialState;
            _autoResetSignal = autoReset;
        }

        public void Reset()
        {
            _notified = false;
        }

        public void Set()
        {
            lock (_lock)
            {
                // first time?
                if (!_notified)
                {
                    // set the flag
                    _notified = true;

                    // unblock a thread which is waiting on this signal 
                    Monitor.Pulse(_lock);
                }
            }
        }

        public void WaitOne()
        {
            WaitOne(Timeout.Infinite);
        }

        public bool WaitOne(int milliseconds)
        {
            lock (_lock)
            {
                bool result = true;
                // this check needs to be inside the lock otherwise you can get nailed
                // with a race condition where the notify thread sets the flag AFTER 
                // the waiting thread has checked it and acquires the lock and does the 
                // pulse before the Monitor.Wait below - when this happens the caller
                // will wait forever as he "just missed" the only pulse which is ever 
                // going to happen 
                if (!_notified)
                {
                    result = Monitor.Wait(_lock, milliseconds);
                }

                if (_autoResetSignal)
                {
                    _notified = false;
                }
                return (result);
            }
        }
    }
    #endregion
    #region enum ThreadActionState a ThreadWrapState
    /// <summary>
    /// Stav akce
    /// </summary>
    public enum ThreadActionState
    {
        /// <summary>
        /// Inicializováno
        /// </summary>
        Initialized,
        /// <summary>
        /// Čeká ve frontě
        /// </summary>
        WaitingInQueue,
        /// <summary>
        /// Je na řadě ke zpracování, čeká na uvolnění pracovního vlákna
        /// </summary>
        WaitingToThread,
        /// <summary>
        /// Právě probíhá akce
        /// </summary>
        Running,
        /// <summary>
        /// Akce doběhla
        /// </summary>
        Completed
    }
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
            App.AddLog("ThreadManagerTests", $"Start aplikace");

            for (int r = 0; r < 60; r++)
            {
                App.AddLog("ThreadManagerTests", $"Řádek {r}", "Test");
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

        /*   VÝSLEDKY TESTOVÁNÍ, časy v mikrosekundách!
          SROVNÁNÍ VÝKONU s použitím synchronizačního signálního objektu:                                                                    AutoResetEvent         Monitor
          1. Vložení požadavku na akci = režie v aplikačním kódu = zdržení řídícího vlákna průměrně (vyjma první akce), optimálně       :    39 mikrosekund      46 mikrosekund
            a) analýza z 1000 požadavků: průměr 5% nejlepších časů                                                                      :    22 mikrosekund      20 mikrosekund
            b) analýza z 1000 požadavků: průměr středu = po odečtení 5% nejhorších a 5% nejlepších časů                                 :    39 mikrosekund      46 mikrosekund
            c) analýza z 1000 požadavků: průměr 5% nejhorších                                                                           :   544 mikrosekund     842 mikrosekund

          2. Čas mezi vložením požadavku na akci a jejím fyzickým zahájením v jejím threadu, v případě, kdy je k dispozici volný thread
            a) úplně první thread systému = rozbíhání celého systému                                                                    : 5 968 mikrosekund   5 657 mikrosekund
            b) druhý thread = vytvoření new instance Thread                                                                             : 1 094 mikrosekund   4 341 mikrosekund
            c) průběžné požadavky v době, kdy systém běží (jsou založeny instance pro výkonné thready), a má volné thready              :    57 - 500            90 - 360

          3. Čas prodlevy ve využití threadu mezi ukončením jedné akce a přidělením threadu pro další akci, z hlediska výkonného kódu   :    62 mikrosekund      76 mikrosekund
            a) Rychlost přechodu z ukončení výkonného kódu do threadu Dispatchera = pro obsloužení dalších akcí                         :    19 mikrosekund      23 mikrosekund
            b) Vyhledání další akce a její přidělení do výkonného threadu (v threadu Dispatchera)                                       :    31 mikrosekund      44 mikrosekund
            c) Nastartování akce ve výkonném threadu (z threadu Dispatchera do výkonného threadu)                                       :    12 mikrosekund      37 mikrosekund

          9. Celkový poměr času:
           a) doba běhu testu od startu do konce čekání na doběhnutí všech akcí               :   346 227 mikrosekund
           b) součet času na vložení 1000 požadavků na provedení akce                         :    63 346 mikrosekund
           c) součet doby jen výkonné části testu (=pracovní kód v akci)                      : 1 110 198 mikrosekund
           d) poměr času výkonné části / celkovém fyzickému času, použity 4 výkonné thready   :       321 %
           e) poměr pro 8 threadů (1358438 / 385126)                                          :       353 %    (CPU: 1 procesor, 2 core, 4 logical processors) 

          Podle údajů na webu je použití objektu Monitor pro synchronizaci dvou threadů 100x výkonnější než AutoResetEvent:
             http://blog.teamleadnet.com/2012/02/why-autoresetevent-is-slow-and-how-to.html
             Pozor, jde o článek z roku 2012 a dnes je 2020.
          V daném scénáři to na to nevypadá.
        */
        [TestMethod]
        public void TestThreadManager()
        {
            System.Windows.Forms.Clipboard.Clear();
            ThreadManager.MaxThreadCount = 4;
            ThreadManager.LogActive = true;
            Rand = new Random();

            Thread.CurrentThread.Name = "MainThread";
            App.AddLog("ThreadManagerTests", $"Start aplikace");

            int actionCount = 1024;

            try
            {
                actionCount = 500;
                for (int r = 1; r <= actionCount; r++)
                {
                    string code = "M";
                    string name = $"{r}{code}";
                    var runStart = App.CurrentTime;
                    App.AddLog("ThreadManagerTests", $"Spouštíme akci {name}");
                    ThreadManager.AddAction(name, _TestDataOneAction, r, code, runStart);
                    var runTime = App.GetElapsedTime(runStart, ElapsedTimeType.Microseconds, 0);
                    App.AddLog("ThreadManagerTests", $"Spuštěna akce {name}", runTime);
                    if ((r % 40) == 0)   // Rand.Next(50) < 3)
                    {
                        int wait = Rand.Next(5, 10);
                        App.AddLog("ThreadManagerTests", $"Počkáme čas {wait} ms...");
                        Thread.Sleep(wait);
                        App.AddLog("ThreadManagerTests", $"Pokračujeme");
                    }
                    if ((r % 150) == 0)
                    {
                        App.AddLog("ThreadManagerTests", $"Počkáme na doběhnutí všech akcí...");
                        ThreadManager.WaitToActionsDone();
                        App.AddLog("ThreadManagerTests", $"Pokračujeme");
                    }
                }
            }

            catch (Exception exc)
            {
                App.AddLog(exc);
            }
            finally
            {
                App.AddLog("ThreadManagerTests", $"Čekáme na doběhnutí všech akcí...");
                ThreadManager.WaitToActionsDone();
                App.AddLog("ThreadManagerTests", $"Konec aplikace");
            }

            var text = App.LogText;

            try { System.Windows.Forms.Clipboard.SetText(text); }
            catch (Exception) { }
        }
        private void _TestDataOneAction(object[] arguments)
        {
            int number = (int)arguments[0];
            string code = (string)arguments[1];
            long requestStart = (long)arguments[2];
            var requestDelay = App.GetElapsedTime(requestStart, ElapsedTimeType.Microseconds, 0);

            string name = $"{number}/{code}";

            int estimatedTime = Rand.Next(400, 2000);               // Cílový čas v mikrosekundách
            int count = 100 * estimatedTime / 8;                    // 100 cyklů = 8 mikrosecs

            string source = $"ThreadManagerTests_{name}";
            App.AddLog(source, $"Zahájení výpočtů", requestDelay);
            var startTime = App.CurrentTime;

            for (int t = 0; t < count; t++)
            {
                double a = Math.Cos(0.45d * Math.PI);
                double b = Math.Sin(0.45d * Math.PI);
            }
            var realTime = App.GetElapsedTime(startTime, ElapsedTimeType.Microseconds, 0);
            App.AddLog(source, $"Dokončení výpočtů", realTime);

            if (code == "M")
            {
                string subCode = "S";
                string subName = $"{number}/{subCode}";
                var runStart = App.CurrentTime;
                App.AddLog(source, $"Spouštíme akci {subName}");
                ThreadManager.AddAction(subName, _TestDataOneAction, number, subCode, runStart);
                var runTime = App.GetElapsedTime(runStart, ElapsedTimeType.Microseconds, 0);
                App.AddLog(source, $"Spuštěna akce {subName}", runTime);
            }

            App.AddLog(source, $"Dokončení akce");
        }
        private Random Rand;
    }
}
#endregion
