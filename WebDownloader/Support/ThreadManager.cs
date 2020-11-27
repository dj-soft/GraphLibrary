using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;

namespace Djs.Tools.WebDownloader.Support
{
    public class ThreadManager
    {
        #region Public static prvky
        /// <summary>
        /// Spustí danou akci v threadu "na pozadí" = daná akce poběží v jiném vláknu, a toto vlákno vrátí řízení ihned a bude moci provádět cokoli potřebuje.
        /// Pokud právě běží všechny povolené thread (<see cref="MaxThreadCount"/>), pak tato metoda zablokuje aktuální thread a počká na uvolnění prvního pracovního threadu, 
        /// a ten pak obsadí pro danou akci.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="done"></param>
        public static void RunActionAsync(Action action, Action done = null)
        {
            Instance.__RunAction(action, null, null, null);
        }
        /// <summary>
        /// Spustí danou akci v threadu "na pozadí" = daná akce poběží v jiném vláknu, a toto vlákno vrátí řízení ihned a bude moci provádět cokoli potřebuje.
        /// Pokud právě běží všechny povolené thread (<see cref="MaxThreadCount"/>), pak tato metoda zablokuje aktuální thread a počká na uvolnění prvního pracovního threadu, 
        /// a ten pak obsadí pro danou akci.
        /// </summary>
        /// <param name="actionArgs"></param>
        /// <param name="arguments"></param>
        public static void RunActionAsync(Action<object[]> actionArgs, params object[] arguments)
        {
            Instance.__RunAction(null, actionArgs, arguments, null);
        }
        /// <summary>
        /// Spustí danou akci v threadu "na pozadí" = daná akce poběží v jiném vláknu, a toto vlákno vrátí řízení ihned a bude moci provádět cokoli potřebuje.
        /// Pokud právě běží všechny povolené thread (<see cref="MaxThreadCount"/>), pak tato metoda zablokuje aktuální thread a počká na uvolnění prvního pracovního threadu, 
        /// a ten pak obsadí pro danou akci.
        /// </summary>
        /// <param name="actionArgs"></param>
        /// <param name="done"></param>
        /// <param name="arguments"></param>
        public static void RunActionAsync(Action<object[]> actionArgs, Action done, params object[] arguments)
        {
            Instance.__RunAction(null, actionArgs, arguments, done);
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
        /// Max počet threadů. Při překročení dojde k chybě. Lze zadat počet 2 až 500 (včetně). Výchozí nastavení je dáno podle počtu procesorů.
        /// </summary>
        public static int MaxThreadCount { get { return Instance.__MaxThreadCount; } set { Instance.__MaxThreadCount = (value < 2 ? 2 : (value > 500 ? 500 : value)); } }

        private void __RunAction(Action action, Action<object[]> actionArgs, object[] arguments, Action done)
        {
            ThreadWrap threadWrap = __GetThread();
            threadWrap.RunAction(action, actionArgs, arguments, done);
        }
        /// <summary>
        /// Najde volný thread / vytvoří nový thread / počká na uvolnění threadu a vrátí jej.
        /// </summary>
        /// <returns></returns>
        private ThreadWrap __GetThread()
        {
            ThreadWrap threadWrap = null;
            while (true)
            {
                if (__TryGetThread(out threadWrap)) break;                                         // Najde volný thread / vytvoří nový thread a vrátí jej.
                __Semaphore.WaitOne(1000);                                                         //  ... počká na uvolnění threadu ...
                // Až některý thread skončí svoji práci, vyvolá svůj event ThreadDone, přijde k nám do handleru AnyThreadDone(), 
                // tam zazvoní budíček (__Semaphore) a my se vrátíme do this smyčky a zkusíme si vyzvednout uvolněný thread v příštím kole smyčky...
            }
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
                threadWrap = threadList.FirstOrDefault(t => !t.TryAllocate());                     // Najdeme první thread, který je možno alokovat a rovnou jej Alokujeme
                if (threadWrap == null)
                {
                    int count = __Threads.Count;
                    if (count < __MaxThreadCount)
                    {   // Můžeme ještě přidat další thread:
                        threadWrap = new ThreadWrap(this, $"ThreadInPool{(count + 1)}", ThreadWrapState.Allocated);    // Vytvoříme nový thread, a rovnou jako Alokovaný
                        threadWrap.ThreadDone += AnyThreadDone;
                        __Threads.Add(threadWrap);
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
        }
        private List<ThreadWrap> __Threads;
        private int __MaxThreadCount;
        private AutoResetEvent __Semaphore;
        private bool __IsStopped;
        #endregion
        #region class ThreadWrap
        protected class ThreadWrap : IDisposable
        {
            #region Konstruktor a proměnné

            public ThreadWrap(ThreadManager owner, string name, ThreadWrapState initialState = ThreadWrapState.Disponible)
            {
                __Owner = owner;
                __Lock = new SpinLock();
                __Semaphore = new AutoResetEvent(false);
                __State = initialState;
                __DisponibleFrom = DateTime.UtcNow;
                __End = false;
                __Thread = new Thread(__Loop) { IsBackground = true, Name = name, Priority = ThreadPriority.BelowNormal };
                __Thread.Start();
            }
            private ThreadManager __Owner;
            private SpinLock __Lock;
            private AutoResetEvent __Semaphore;
            private volatile ThreadWrapState __State;
            private bool __End;
            private DateTime __DisponibleFrom;
            private Thread __Thread;
            private volatile Action __Action;
            private volatile Action<object[]> __ActionArgs;
            private volatile object[] __Arguments;
            private volatile Action __Done;
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
            public void RunAction(Action action, Action<object[]> actionArgs, object[] arguments, Action done)
            {
                if (action == null && actionArgs == null)
                    throw new InvalidOperationException($"ThreadWrap.RunAction error: no action method is given.");

                bool lockTaken = false;
                try
                {
                    __Lock.Enter(ref lockTaken);

                    var state = __State;
                    if (state != ThreadWrapState.Allocated)
                        throw new InvalidOperationException($"ThreadWrap.RunAction error: invalid state of Thread instance ({nameof(ThreadWrapState)}.{state}) for AddAction.");

                    __Action = action;
                    __ActionArgs = actionArgs;
                    __Arguments = arguments;
                    __Done = done;
                    __State = ThreadWrapState.WaitToRun;
                }
                finally
                {
                    if (lockTaken)
                        __Lock.Exit();
                }

                __Semaphore.Set();                   // Požádáme thread na pozadí o vykonání akce.
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
                Action action = null;
                Action<object[]> actionArgs = null;
                object[] arguments = null;
                Action done = null;
                bool needRun = false;
                bool lockTaken = false;
                try
                {   // Pod zámkem načtu požadované akce, vyhodnotím a nastavím stav Working:
                    __Lock.Enter(ref lockTaken);
                    action = __Action;
                    actionArgs = __ActionArgs;
                    arguments = __Arguments;
                    done = __Done;

                    needRun = (action != null || actionArgs != null);
                    if (needRun)
                        __State = ThreadWrapState.Working;

                    __Action = null;
                    __ActionArgs = null;
                    __Arguments = null;
                    __Done = null;
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
                        if (action != null)
                            action();
                        else if (actionArgs != null)
                            actionArgs(arguments);
                        if (done != null)
                            done();
                    }
                    catch (Exception) { }
                    finally
                    {
                        __DisponibleFrom = DateTime.UtcNow;
                        __State = ThreadWrapState.Disponible;
                        OnThreadDone();
                    }
                }
            }
            protected bool IsEnding { get { var s = __State; return (__End || s == ThreadWrapState.Abort); } }
            protected virtual void OnThreadDone()
            {
                ThreadDone?.Invoke(this, EventArgs.Empty);
            }
            public event EventHandler ThreadDone;
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
                __Action = null;
                __ActionArgs = null;
                __Arguments = null;
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
}
