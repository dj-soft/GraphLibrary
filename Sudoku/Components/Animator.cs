using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    /// <summary>
    /// Třída, poskytující služby pro animaci = změnu vlastností v čase.
    /// </summary>
    public class Animator
    {
        #region Konstruktor, Owner, FPS, Timer
        /// <summary>
        /// Konstruktor, pro daný control. Smí být null.
        /// Pozor, animátor je vytvořen ve stavu <see cref="Running"/> = false, proto aby mohl být bezpečně naplněn a připravena okolní infrastruktura. 
        /// Až poté je vhodné animátor spustit nastavením <see cref="Running"/> = true.
        /// </summary>
        /// <param name="owner">Control, který bude invalidován po provedení změn a bude používán pro invokaci v každém Ticku</param>
        /// <param name="fps">Frames per second = počet ticků za sekundu. Default = <see cref="DefaultFps"/> = 50, přípustná hodnota 1 až 100.</param>
        /// <param name="running">Možnost vytvoření animátoru s tímto stavem <see cref="Running"/>, default = false = neběží, je třeba spustit ručně</param>
        public Animator(Control owner = null, double? fps = null, bool running = false)
        {
            if (owner != null) this.__Owner = new WeakReference<System.Windows.Forms.Control>(owner);
            this.__Running = running;
            this.__Fps = (fps.HasValue ? (fps.Value < 1 ? 1 : (fps.Value > 100 ? 100 : fps.Value)) : DefaultFps);
            this.__Motion = new List<Motion>();
            this._TimerStart();
        }
        /// <summary>
        /// Defaultní FPS
        /// </summary>
        public static int DefaultFps { get { return 50; } }
        /// <summary>
        /// WeakReference na Owner Control
        /// </summary>
        private WeakReference<System.Windows.Forms.Control> __Owner;
        /// <summary>
        /// Obsahuje true, pokud máme Ownera
        /// </summary>
        private bool _HasOwner
        {
            get
            {
                var wr = __Owner;
                return (wr != null && wr.TryGetTarget(out var _));
            }
        }
        /// <summary>
        /// Obsahuje Owner control
        /// </summary>
        private Control _Owner
        {
            get 
            {
                var wr = __Owner;
                if (wr is null || !wr.TryGetTarget(out var owner)) return null;
                return owner;
            }
        }
        /// <summary>
        /// Frames per second = počet ticků za sekundu.
        /// Lze setovat hodnotu v rozsahu 1 - 100. Setování se uplatní okamžitě. Nová hodnota ovlivní "rychlost animace" již zadaných Motions.
        /// </summary>
        public double Fps { get { return __Fps; } set { this.__Fps = (value < 1d ? 1d : (value > 100d ? 100d : value)); } } private double __Fps;
        /// <summary>
        /// Animace běží? Nastavením false bude pozastavena (freeze), nastavením true se rozběhne.
        /// </summary>
        public bool Running { get { return __Running; } set { this.__Running = value; } } private bool __Running;
        #endregion
        #region Vlákno na pozadí = časovač a řízení
        /// <summary>
        /// Spustí časovou smyčku animace.
        /// Volá se v Main (GUI) threadu, nastartuje separátní thread na pozadí a tato metoda ihned poté skončí.
        /// </summary>
        private void _TimerStart()
        {
            this.__StopWatch = new Data.StopwatchExt();
            this.__WinMMTimer = new WinMMTimer();

            this.__TimerThread = new Thread(_TimerLoop);
            this.__TimerThread.IsBackground = true;
            this.__TimerThread.Priority = ThreadPriority.Normal;
            this.__TimerThread.Name = "AnimatorTimer";
            this.__TimerThread.Start();
        }
        /// <summary>
        /// Metoda, která reprezentuje celý životní cyklus threadu na pozadí = časovač animačních akcí
        /// </summary>
        private void _TimerLoop()
        {
            __StopWatch.Start();
            __AnimatorTimerStop = false;
            try
            {
                long tick = 0L;
                bool isDiagnosticActive = Data.AppService.IsDiagnosticActive;
                StringBuilder sbLog = null;
                if (isDiagnosticActive)
                {
                    sbLog = new StringBuilder();
                    sbLog.AppendLine($"Tick\tTime [ms]\tCycle [ms]\tAction [ms]\tWait [ms]\tDoSleep\tSleep\tWake\tDream");
                }

                while (true)
                {
                    if (__AnimatorTimerStop) break;

                    double currMs = __StopWatch.ElapsedMilisecs;         // Aktuální čas
                    double cycleMs = (1000d / __Fps);                    // Délka celého cyklu, odpovídající FPS
                    double endMs = currMs + cycleMs;                     // Čas na konci cyklu
                    double actionMs = 0d;
                    double waitMs = cycleMs;
                    if (isDiagnosticActive) tick++;
                    if (__Running && __Motion.Count > 0)
                    {   // Pokud neběžíme, neřešíme nic.
                        // A naprostou většinu času bude počet akcí = 0, takže by bylo zbytečné provádět invokaci GUI threadu...
                        _RunInGui(_OneTick);
                        if (__AnimatorTimerStop) break;

                        double beginMs = __StopWatch.ElapsedMilisecs;    // Nynější čas
                        actionMs = beginMs - currMs;                     // Čas vlastní akce
                        waitMs = endMs - beginMs;                        // Zbývající čas k čekání
                    }

                    bool doWait = waitMs > 2d;

                    double sleepMs = __StopWatch.ElapsedMilisecs;        // Aktuální čas před ulehnutím
                    if (doWait)
                        __WinMMTimer.Wait((uint)waitMs);                 // Počkáme si
                    double wakeMs = __StopWatch.ElapsedMilisecs;         // Aktuální čas po vyspinkání
                    double dreamMs = wakeMs - sleepMs;                   // Takhle dlouho jsme spinkali

                    if (isDiagnosticActive)
                    {
                        sbLog.AppendLine($"{tick}\t{currMs:F3}\t{cycleMs:F3}\t{actionMs:F3}\t{waitMs:F3}\t{(doWait ? "1" : "0")}\t{sleepMs:F3}\t{wakeMs:F3}\t{dreamMs:F3}");

                        if ((tick % 100) == 0)
                        {
                            string log = sbLog.ToString();
                            // Vložit log do Clipboardu, ale tady jsme v threadu Background!
                            //_RunInGui(() =>
                            //{
                            //    System.Windows.Forms.MessageBox.Show("Máme data");
                            //    System.Windows.Forms.Clipboard.Clear();
                            //    System.Windows.Forms.Clipboard.SetText(log);
                            //});
                            sbLog.Clear();
                        }
                    }
                }
            }
            finally
            {
                this.__WinMMTimer.Dispose();
            }
        }
        /// <summary>
        /// Provede jeden animační krok = všechny platné akce <see cref="Motion"/> plus finální Repaint ownera.
        /// Tato metoda je vyvolána v threadu GUI (pokud máme k dispozici Owner control).
        /// </summary>
        private void _OneTick()
        {
            Motion[] motions = _GetValidMotions();
            if (motions is null) return;

            // Provedeme DoTick pro platné Motions:
            bool hasChanges = false;
            if (motions.Length > 0)
            {
                foreach (var motion in motions)
                    ((IMotionWorking)motion).DoTick(ref hasChanges);
            }

            // Pokud v průběhu DoTick došlo k nějakým změnám, vyvoláme překreslení Ownera (jsme v GUI threadu):
            if (hasChanges)
                _RepaintOwner();
        }
        /// <summary>
        /// Metoda vrátí pole obsahující platné <see cref="Motion"/> = existující a ne-ukončené. 
        /// Ty, které jsou Ukončené (<see cref="Motion.IsDone"/> = true), z trvalého seznamu <see cref="__Motion"/> vyhodí (a do výstupu se nedostanou)!
        /// Všechno provádí pod lockem.
        /// Metoda může vrátit null, když po vyčištění nejsou žádné platné <see cref="Motion"/>.
        /// </summary>
        /// <returns></returns>
        private Motion[] _GetValidMotions()
        {
            if (__Motion is null || __Motion.Count == 0) return null;

            Motion[] motions = null;
            lock (__Motion)
            {
                __Motion.RemoveAll(m => m.IsDone);
                if (__Motion.Count > 0)
                    motions = __Motion.ToArray();
            }
            return motions;
        }
        /// <summary>
        /// Zajistí vyvolání metody pro překreslení Ownera. Volá se po dokončení jednoho Ticku, při kterém došlo ke změnám v animaci.
        /// Tato metoda neprovádí Invokaci GUI threadu! Volá se typicky na konci každého Ticku, a ten má být celý prováděn v GUI threadu, kvůli volání akcí v controlu.
        /// Pro převolání GUI threadu je určena metoda 
        /// </summary>
        private void _RepaintOwner()
        {
            var wr = __Owner;
            if (!__AnimatorTimerStop && wr != null && wr.TryGetTarget(out var owner))
            {
                owner.Invalidate();
            }
        }
        /// <summary>
        /// Metoda zajistí, že dodaná akce <paramref name="action"/> bude volána v GUI threadu ownera <see cref="_Owner"/>.
        /// Pokud Owner neexistuje, anebo nepotřebuje provést invokaci, pak bude akce spuštěna přímo v tomto threadu.
        /// Převolání GUI threadu je synchronní = čeká se na jeho dokončení (je použita invokace Invoke(), nikoli BeginInvoke() ).
        /// </summary>
        /// <param name="action"></param>
        private void _RunInGui(System.Action action)
        {
            var owner = _Owner;
            if (owner != null && owner.InvokeRequired)
                owner.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Thread běžící na pozadí
        /// </summary>
        private Thread __TimerThread;
        /// <summary>
        /// Přesný časovač pro měření režijního času ticku, pro určení přesného času Sleep mezi dvěma Ticky tak, aby byl dodržen <see cref="Fps"/>
        /// </summary>
        private Data.StopwatchExt __StopWatch;
        /// <summary>
        /// Přesný časovač čekání
        /// </summary>
        private WinMMTimer __WinMMTimer;
        /// <summary>
        /// Příznak, že časová smyčka animátoru má být zastavena.
        /// Výchozí hodnota je false.
        /// Lze nastavit true, tím animátor skončí svoji práci. Ale nelze pak již nikdy nastavit false. Používá se typicky v metodě Dispose() controlu vlastníka, když už animace nebude prováděna.
        /// </summary>
        public bool AnimatorTimerStop { get { return __AnimatorTimerStop; } set { __AnimatorTimerStop |= value; } } private bool __AnimatorTimerStop;
        #endregion
        #region Správa animačních akcí
        /// <summary>
        /// Vloží další definici animace.
        /// Tato animace nemá omezený čas, skončí až aplikace nastaví <see cref="Motion.IsDone"/> = true.
        /// Nemá vlastní hodnotu (Value), pouze zajišťuje v pravidelném cyklu vyvolání cílové metody Action.
        /// </summary>
        /// <param name="action">Akce volaná v každém kroku</param>
        /// <param name="userData">Libovolná data aplikace</param>
        public Motion AddMotion(Action<Motion> action, object userData)
        {
            Motion motion = new Motion(action, userData);
            lock (__Motion)
                __Motion.Add(motion);
            return motion;
        }
        /// <summary>
        /// Vloží další definici animace
        /// </summary>
        /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
        /// <param name="timeMode"></param>
        /// <param name="timeZoom"></param>
        /// <param name="action">Akce volaná v každém kroku</param>
        /// <param name="startValue">Počáteční hodnota</param>
        /// <param name="endValue">Koncová hodnota</param>
        /// <param name="userData">Libovolná data aplikace</param>
        public Motion AddMotion(int? stepCount, TimeMode timeMode, double timeZoom, Action<Motion> action, object startValue, object endValue, object userData)
        {
            Motion motion = new Motion(stepCount, timeMode, timeZoom, action, startValue, endValue, userData);
            lock (__Motion)
                __Motion.Add(motion);
            return motion;
        }
        /// <summary>
        /// Vloží další definici animace
        /// </summary>
        /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
        /// <param name="stepCurrent">Výchozí pozice</param>
        /// <param name="timeMode"></param>
        /// <param name="timeZoom"></param>
        /// <param name="action">Akce volaná v každém kroku</param>
        /// <param name="startValue">Počáteční hodnota</param>
        /// <param name="endValue">Koncová hodnota</param>
        /// <param name="userData">Libovolná data aplikace</param>
        public Motion AddMotion(int? stepCount, int? stepCurrent, TimeMode timeMode, double timeZoom, Action<Motion> action, object startValue, object endValue, object userData)
        {
            Motion motion = new Motion(stepCount, stepCurrent, timeMode, timeZoom, action, startValue, endValue, userData);
            lock (__Motion)
                __Motion.Add(motion);
            return motion;
        }
        /// <summary>
        /// Vloží další definici animace
        /// </summary>
        /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
        /// <param name="timeMode"></param>
        /// <param name="timeZoom"></param>
        /// <param name="action">Akce volaná v každém kroku</param>
        /// <param name="valueSet"></param>
        /// <param name="userData">Libovolná data aplikace</param>
        public Motion AddMotion(int? stepCount, TimeMode timeMode, double timeZoom, Action<Motion> action, AnimatedValueSet valueSet, object userData)
        {
            Motion motion = new Motion(stepCount, timeMode, timeZoom, action, valueSet, userData);
            lock (__Motion)
                __Motion.Add(motion);
            return motion;
        }
        /// <summary>
        /// Vloží další definici animace
        /// </summary>
        /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
        /// <param name="stepCurrent">Výchozí pozice</param>
        /// <param name="timeMode"></param>
        /// <param name="timeZoom"></param>
        /// <param name="action">Akce volaná v každém kroku</param>
        /// <param name="valueSet"></param>
        /// <param name="userData">Libovolná data aplikace</param>
        public Motion AddMotion(int? stepCount, int? stepCurrent, TimeMode timeMode, double timeZoom, Action<Motion> action, AnimatedValueSet valueSet, object userData)
        {
            Motion motion = new Motion(stepCount, stepCurrent, timeMode, timeZoom, action, valueSet, userData);
            lock (__Motion)
                __Motion.Add(motion);
            return motion;
        }
        private List<Motion> __Motion;
        #endregion
        #region class Action
        /// <summary>
        /// Jedna animační akce, která má daný styl práce s časem (=dynamiku pohybu), počáteční a koncovou hodnotu, 
        /// a cílovou metodu, která zpracuje aktuální animovanou hodnotu do konkrétního místa, čímž provede pohyb / změnu animace (např. barva, velikost, pozice konkrétního prvku ...)
        /// </summary>
        public class Motion : IMotionWorking
        {
            #region Konstruktor a public property
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="action"></param>
            /// <param name="userData"></param>
            public Motion(Action<Motion> action, object userData)
            {
                __StepIndex = 0;
                __StepCount = -1;
                __TimeMode = TimeMode.Linear;
                __TimeModeIsCycling = true;
                __TimeCoeff = 1d;
                __Action = action;
                __StartValue = null;
                __EndValue = null;
                __CurrentValue = null;
                __UserData = userData;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="stepCount"></param>
            /// <param name="timeMode"></param>
            /// <param name="timeZoom">Zoom času, v rozmezí -10 až +10</param>
            /// <param name="action"></param>
            /// <param name="startValue"></param>
            /// <param name="endValue"></param>
            /// <param name="userData"></param>
            public Motion(int? stepCount, TimeMode timeMode, double timeZoom, Action<Motion> action, object startValue, object endValue, object userData)
            {
                CheckValues(startValue, endValue, out AnimatedValueType valueType);

                __StepCount = (stepCount.HasValue && stepCount.Value > 0 ? stepCount.Value : -1);
                __StepIndex = 0;
                __TimeMode = timeMode;
                __TimeModeIsCycling = _IsTimeModeCycling(timeMode, stepCount);
                __TimeCoeff = GetZoomCoefficient(timeZoom);
                __Action = action;
                __ValueType = valueType;
                __StartValue = startValue;
                __EndValue = endValue;
                __CurrentValue = startValue;
                __UserData = userData;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="stepCount"></param>
            /// <param name="timeMode"></param>
            /// <param name="timeZoom">Zoom času, v rozmezí -10 až +10</param>
            /// <param name="action"></param>
            /// <param name="startValue"></param>
            /// <param name="endValue"></param>
            /// <param name="userData"></param>
            public Motion(int? stepCount, int? stepCurrent, TimeMode timeMode, double timeZoom, Action<Motion> action, object startValue, object endValue, object userData)
            {
                CheckValues(startValue, endValue, out AnimatedValueType valueType);

                __StepCount = (stepCount.HasValue && stepCount.Value > 0 ? stepCount.Value : -1);
                __StepIndex = (stepCurrent.HasValue ? _GetAlignedValue(stepCurrent.Value, 0, __StepCount) : 0);
                __TimeMode = timeMode;
                __TimeModeIsCycling = _IsTimeModeCycling(timeMode, stepCount);
                __TimeCoeff = GetZoomCoefficient(timeZoom);
                __Action = action;
                __ValueType = valueType;
                __StartValue = startValue;
                __EndValue = endValue;
                __CurrentValue = startValue;
                __UserData = userData;
            }
            /// <summary>
            /// Vloží další definici animace
            /// </summary>
            /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
            /// <param name="timeMode"></param>
            /// <param name="timeZoom"></param>
            /// <param name="action">Akce volaná v každém kroku</param>
            /// <param name="valueSet"></param>
            /// <param name="userData">Libovolná data aplikace</param>
            public Motion(int? stepCount, TimeMode timeMode, double timeZoom, Action<Motion> action, AnimatedValueSet valueSet, object userData)
            {
                __StepCount = (stepCount.HasValue && stepCount.Value > 0 ? stepCount.Value : -1);
                __StepIndex = 0;
                __TimeMode = timeMode;
                __TimeModeIsCycling = _IsTimeModeCycling(timeMode, stepCount);
                __TimeCoeff = GetZoomCoefficient(timeZoom);
                __Action = action;
                __ValueType = valueSet.ValueType;
                __ValueSet = valueSet;
                __CurrentValue = valueSet.GetValueAtPosition(0d);
                __UserData = userData;
            }
            /// <summary>
            /// Vloží další definici animace
            /// </summary>
            /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
            /// <param name="timeMode"></param>
            /// <param name="timeZoom"></param>
            /// <param name="action">Akce volaná v každém kroku</param>
            /// <param name="valueSet"></param>
            /// <param name="userData">Libovolná data aplikace</param>
            public Motion(int? stepCount, int? stepCurrent, TimeMode timeMode, double timeZoom, Action<Motion> action, AnimatedValueSet valueSet, object userData)
            {
                __StepCount = (stepCount.HasValue && stepCount.Value > 0 ? stepCount.Value : -1);
                __StepIndex = (stepCurrent.HasValue ? _GetAlignedValue(stepCurrent.Value, 0, __StepCount) : 0);
                __TimeMode = timeMode;
                __TimeModeIsCycling = _IsTimeModeCycling(timeMode, stepCount);
                __TimeCoeff = GetZoomCoefficient(timeZoom);
                __Action = action;
                __ValueType = valueSet.ValueType;
                __ValueSet = valueSet;
                __CurrentValue = valueSet.GetValueAtPosition(0d);
                __UserData = userData;
            }
            /// <summary>
            /// Vrátí true, pokud režim času je nekonečný
            /// </summary>
            /// <param name="timeMode"></param>
            /// <returns></returns>
            private static bool _IsTimeModeCycling(TimeMode timeMode, int? stepCount)
            {
                if (!stepCount.HasValue || stepCount.Value <= 0) return true;
                return (timeMode == TimeMode.Cycling);
            }
            /// <summary>
            /// Vrátí hodnotu zarovnanou do daných mezí
            /// </summary>
            /// <param name="value"></param>
            /// <param name="min"></param>
            /// <param name="max"></param>
            /// <returns></returns>
            private static int _GetAlignedValue(int value, int min, int max)
            {
                if (max <= min) return min;
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }
            /// <summary>
            /// Volaná akce
            /// </summary>
            private Action<Motion> __Action;
            /// <summary>
            /// Typ hodnoty
            /// </summary>
            private AnimatedValueType __ValueType;
            /// <summary>
            /// Dynamika pohybu
            /// </summary>
            private TimeMode __TimeMode;
            /// <summary>
            /// Dynamika pohybu je nekonečná = po dokončení jednoho cyklu ihned začíná další
            /// </summary>
            private bool __TimeModeIsCycling;
            /// <summary>
            /// Koeficient Zoomu dynamiky
            /// </summary>
            private double __TimeCoeff;
            /// <summary>
            /// Pořadové číslo kroku. Při prvním volání aplikační akce je zde hodnota 0. Při posledním volání je zde hodnota == (<see cref="StepCount"/> - 1).
            /// </summary>
            public int StepIndex { get { return __StepIndex; } } private int __StepIndex;
            /// <summary>
            /// Celkový počet kroků. Pokud je zde -1, pak tato animační akce je nekončící (bude ukončena nastavením <see cref="IsDone"/> z aplikačního kódu).
            /// </summary>
            public int StepCount { get { return __StepCount; } } private int __StepCount;
            /// <summary>
            /// Počáteční hodnota
            /// </summary>
            public object StartValue { get { return __StartValue; } } private object __StartValue;
            /// <summary>
            /// Cílová hodnota
            /// </summary>
            public object EndValue { get { return __EndValue; } } private object __EndValue;
            /// <summary>
            /// Sada hodnot
            /// </summary>
            public AnimatedValueSet ValueSet { get { return __ValueSet; } } private AnimatedValueSet __ValueSet;
            /// <summary>
            /// Aktuální hodnota
            /// </summary>
            public object CurrentValue { get { return __CurrentValue; } } private object __CurrentValue;
            /// <summary>
            /// Aktuální pozice na časové ose pohybu, 0=počátek, 1=konec
            /// </summary>
            public double CurrentRatio { get { return __CurrentRatio; } } private double __CurrentRatio;
            /// <summary>
            /// Uživatelská data. Lze i setovat.
            /// </summary>
            public object UserData { get { return __UserData; } set { __UserData = value; } } private object __UserData;
            /// <summary>
            /// Animátor nastaví true v situaci, kdy nová hodnota <see cref="CurrentValue"/> (předávaná nyní do akce) se liší od hodnoty <see cref="CurrentValue"/> použité v předešlém kroku.
            /// Aplikační kód na to může reagovat.
            /// Pokud hodnota nebyla změněna, je zde false. Pokud v takovém případě nebude aplikačním kódem nastaveno true, pak po takovém cyklu nebude proveden Repaint controlu = nemá to význam.
            /// Aplikační kód tedy může pomocí zdejší hodnoty řídit požadavek na Repaint.
            /// </summary>
            public bool IsCurrentValueChanged { get { return __IsCurrentValueChanged; } set { __IsCurrentValueChanged = value; } } private bool __IsCurrentValueChanged;
            /// <summary>
            /// Pokud po provedení akce (nebo kdykoli jindy) je zde true, pak tato animační akce končí a je vyřazena ze seznamu akcí.
            /// Aplikační kód může nastavit v kterémkoli kroku.
            /// <para/>
            /// Animační jádro <see cref="Animator"/> nastaví tuto proměnnou na true před vyvoláním posledního kroku, podle toho může uživatelský kód v rámci animační akce detekovat, že jde o poslední = finální krok.
            /// Aplikační kód může hodnotu vrátit na false, a pak tato akce bude volána i v příštím cyklu.
            /// <para/>
            /// Aplikační kód si může uschovat instanci <see cref="Motion"/> (ta je výstupem metod <see cref="Animator.AddMotion(Action{Motion}, object)"/>), 
            /// a kdykoliv (asynchronně, z libovolného threadu) může do této uschované instance nastavit <see cref="IsDone"/> = true.
            /// Animátor pak takové animace vyřadí ze seznamu před zahájením příštího animačního kroku.
            /// Pokud ale již byl zahájen animační krok a <see cref="IsDone"/> bylo false, pak se jeden krok akce této <see cref="Motion"/> ještě může provést.
            /// </summary>
            public bool IsDone { get { return __IsDone; } set { __IsDone = value; } } private bool __IsDone;
            #endregion
            #region Provedení jednoho kroku animace
            /// <summary>
            /// Provede jeden časový krok
            /// </summary>
            void IMotionWorking.DoTick(ref bool hasChanges)
            {
                if (__StepCount <= 0)
                    _DoOneTick();
                else
                    _DoOneStep();
                hasChanges |= __IsCurrentValueChanged;
                __StepIndex++;
            }
            /// <summary>
            /// Provede jeden krok animace v situaci, kdy NENÍ dán cílový počet kroků.
            /// Tato metoda nastaví <see cref="IsCurrentValueChanged"/> = true a <see cref="IsDone"/> = false;
            /// ale aplikační kód to v akci <see cref="__Action"/> může změnit.
            /// </summary>
            private void _DoOneTick()
            {
                __IsCurrentValueChanged = true;
                if (!__IsDone)
                {   // Aplikační kód mohl nastavit IsDone na true i v mezidobí po zahájení kroku (tedy tato akce Motion se dostala do seznamu akcí k provedení),
                    // ale před vlastním provedením akce tohoto konkrétního Motion - vyhodnotíme tedy IsDone těsně před akcí:
                    _DoAction();
                }
            }
            /// <summary>
            /// Provede jeden krok animace v situaci, kdy je dán cílový počet kroků.
            /// Tato metoda nastaví <see cref="IsCurrentValueChanged"/> a <see cref="IsDone"/> podle aktuálního stavu dat;
            /// ale aplikační kód to v akci <see cref="__Action"/> může změnit.
            /// </summary>
            private void _DoOneStep()
            {
                int step = __StepIndex + 1;
                double currentRatio = GetCurrentRatio(step, __StepCount, __TimeMode, __TimeCoeff);
                object currentValue = GetCurrentValue(__ValueSet, __StartValue, __EndValue, currentRatio, __ValueType);
                bool isChanged = !IsEqualValues(__CurrentValue, currentValue, __ValueType);
                __CurrentValue = currentValue;
                __CurrentRatio = currentRatio;
                __IsCurrentValueChanged = isChanged;
                if (!__IsDone)
                {   // Aplikační kód mohl nastavit IsDone na true i v mezidobí po zahájení kroku (tedy tato akce Motion se dostala do seznamu akcí k provedení),
                    // ale před vlastním provedením akce tohoto konkrétního Motion - vyhodnotíme tedy IsDone těsně před akcí:
                    __IsDone = (__TimeMode != TimeMode.Cycling && step >= __StepCount);
                    _DoAction();
                }

                // Cyklický pohyb, který nebyl aplikací explicitně ukončen, a aktuální krok (step) by vedl ke konci cyklu: nastavíme Index = -1, a navazující metoda jej inkrementuje na 0:
                if (__TimeModeIsCycling && !__IsDone && step >= __StepCount)
                    __StepIndex = -1;
            }
            /// <summary>
            /// Zavolá uživatelskou akci
            /// </summary>
            private void _DoAction()
            {
                if (__Action != null)
                    __Action(this);
            }
            #endregion
        }
        public interface IMotionWorking
        {
            /// <summary>
            /// Provede jeden časový krok
            /// </summary>
            void DoTick(ref bool hasChanges);
        }
        /// <summary>
        /// Dynamika pohybu v časové ose
        /// </summary>
        public enum TimeMode
        {
            /// <summary>
            /// Lineární pohyb.<br/>
            /// V každém jednotlivém kroku se aktuální hodnota změní o stejně velký krok.<br/>
            /// Přirovnání: rovnoměrné kutálení ocelové kuličky po zcela rovné vodorovné ploše.
            /// <para/>
            /// Tento režim ignoruje parametr TimeZoom.
            /// </summary>
            Linear = 0,
            /// <summary>
            /// Zrychlující pohyb.<br/>
            /// V prvních krocích je změna hodnoty malá, v posledních krocích velká.<br/>
            /// Přirovnání: volný pád ocelové kuličky do hloubky.
            /// </summary>
            SlowStartFastEnd,
            /// <summary>
            /// Zpomalující pohyb.<br/>
            /// V prvních krocích je změna hodnoty velká, v posledních krocích malá.<br/>
            /// Přirovnání: vyhození ocelové kuličky do okna ve třetím patře.
            /// </summary>
            FastStartSlowEnd,
            /// <summary>
            /// Zrychlující a poté zpomalující pohyb.<br/>
            /// V prvních krocích je změna hodnoty malá, uprostřed cesty je velká, a v posledních krocích opět malá.<br/>
            /// Přirovnání: cesta Trabantem mezi dvěma domy = pomalý rozjezd, kousek jízdy, a pomalé brždění u cíle.
            /// </summary>
            SlowStartSlowEnd,
            /// <summary>
            /// Cyklický pohyb (sinusový) = tento režim je bez konce!
            /// Délka jednoho celého cyklu je dána počtem kroků.
            /// Začíná se na hodnotě StartValue, v půlce cyklu dojde hodnota k EndValue a na konci cyklu se vrací ke StartValue.
            /// <para/>
            /// Tento režim ignoruje parametr TimeZoom.
            /// </summary>
            Cycling
        }
        #endregion
        #region Animační support
        #region Práce s hodnotami: CheckValues, GetCurrentValue, IsEqualValues
        /// <summary>
        /// Metoda prověří, zda dodané hodnoty jsou přípustné hodnoty do animátoru.
        /// Pokud ne, dojde k chybě.
        /// Pokud ano, bude do out <paramref name="valueType"/> vložen typ hodnoty.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        protected static void CheckValues(object startValue, object endValue, out AnimatedValueType valueType)
        {
            ValueSupport.CheckValues(startValue, endValue, out valueType);
        }
        /// <summary>
        /// Metoda vrátí "Current" hodnotu pro danou hodnotu Start a End a dané Ratio.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <param name="currentRatio"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        protected static object GetCurrentValue(AnimatedValueSet valueSet, object startValue, object endValue, double currentRatio, AnimatedValueType valueType)
        {
            if (valueSet != null) return valueSet.GetValueAtPosition(currentRatio);
            return ValueSupport.MorphValue(valueType, startValue, currentRatio, endValue);
        }
        /// <summary>
        /// Vrátí true, pokud dvě dodané hodnoty jsou shodné.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(object oldValue, object newValue, AnimatedValueType valueType)
        {
            switch (valueType)
            {
                case AnimatedValueType.Int16: return ((Int16)oldValue == (Int16)newValue);
                case AnimatedValueType.Int32: return ((Int32)oldValue == (Int32)newValue);
                case AnimatedValueType.Int64: return ((Int64)oldValue == (Int64)newValue);
                case AnimatedValueType.Single: return ((Single)oldValue == (Single)newValue);
                case AnimatedValueType.Double: return ((Double)oldValue == (Double)newValue);
                case AnimatedValueType.Decimal: return ((Decimal)oldValue == (Decimal)newValue);
                case AnimatedValueType.Point: return ((Point)oldValue == (Point)newValue);
                case AnimatedValueType.Size: return ((Size)oldValue == (Size)newValue);
                case AnimatedValueType.Color: return ValueSupport.IsEqualColors((Color)oldValue, (Color)newValue);
            }
            throw new ArgumentException($"Nelze provést výpočet vyhodnocení IsEqualValues pro typ hodnoty 'valueType' = '{valueType}'.");
        }
        #endregion
        #region Vyhodnocení pozice na časové ose
        /// <summary>
        /// Metoda vrátí pozici Ratio (hodnota v rozsahu 0 - 1 včetně) na časové ose pro určitý krok <paramref name="step"/> v rámci celé délky <paramref name="count"/>, pro daný režim a zoom.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="timeMode"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        protected static double GetCurrentRatio(int step, int count, TimeMode timeMode, double timeCoefficient)
        {
            if (count <= 0) throw new ArgumentException($"Nelze určit pozici na časové ose pro délku časové osy = '{count}'");

            switch (timeMode)
            {
                case TimeMode.Linear: return _GetCurrentRatioLinear(step, count, timeCoefficient);
                case TimeMode.SlowStartFastEnd: return _GetCurrentRatioSlowStartFastEnd(step, count, timeCoefficient);
                case TimeMode.FastStartSlowEnd: return _GetCurrentRatioFastStartSlowEnd(step, count, timeCoefficient);
                case TimeMode.SlowStartSlowEnd: return _GetCurrentRatioSlowStartSlowEnd(step, count, timeCoefficient);
                case TimeMode.Cycling: return _GetCurrentRatioCycling(step, count, timeCoefficient);
            }
            throw new ArgumentException($"Nelze určit pozici na časové ose pro režim 'TimeMode' = '{timeMode}'");
        }
        /// <summary>
        /// Vrací ratio v režimu <see cref="TimeMode.Linear"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        private static double _GetCurrentRatioLinear(int step, int count, double timeCoefficient)
        {
            double position = (double)step / (double)count;
            double ratio = _GetAlignedRatio(position);
            return ratio;
        }
        /// <summary>
        /// Vrací ratio v režimu <see cref="TimeMode.SlowStartFastEnd"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        private static double _GetCurrentRatioSlowStartFastEnd(int step, int count, double timeCoefficient)
        {
            double position = (double)step / (double)count;
            double ratio = _GetSinusRatio(position, true, 1.5d, 0.5d, 1d, 1d);
            ratio = _GetZoomRatio(ratio, timeCoefficient);
            return ratio;
        }
        /// <summary>
        /// Vrací ratio v režimu <see cref="TimeMode.FastStartSlowEnd"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        private static double _GetCurrentRatioFastStartSlowEnd(int step, int count, double timeCoefficient)
        {
            double position = (double)step / (double)count;
            double ratio = _GetSinusRatio(position, true, 0.0d, 0.5d, 0d, 1d);
            ratio = _GetZoomRatio(ratio, timeCoefficient);
            return ratio;
        }
        /// <summary>
        /// Vrací ratio v režimu <see cref="TimeMode.StartSlowEnd"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        private static double _GetCurrentRatioSlowStartSlowEnd(int step, int count, double timeCoefficient)
        {
            double position = (double)step / (double)count;
            double ratio = _GetSinusRatio(position, true, -0.5d, 1.0d, 0.5d, 0.5d);
            ratio = _GetZoomRatio(ratio, timeCoefficient);
            return ratio;
        }
        /// <summary>
        /// Vrací ratio v režimu <see cref="TimeMode.Cycling"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        private static double _GetCurrentRatioCycling(int step, int count, double timeCoefficient)
        {
            step = step % count;                                 // Vyřeším přetečení přes kruh
            double position = (double)step / (double)count;      // Hodnota 0 až 0.999   (ne 1)
            double ratio = _GetSinusRatio(position, false, 1.5d, 2.0d, 0.5d, 0.5d);
            return ratio;
        }
        /// <summary>
        /// Vrátí dodanou hodnotu <paramref name="ratio"/> upravenou dodaným Zoomem.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="timeCoefficient">Koeficient Zoomu času, v rozmezí +9.000 až +0.111</param>
        /// <returns></returns>
        private static double _GetZoomRatio(double ratio, double timeCoefficient)
        {
            if (ratio <= 0d) return 0d;
            if (ratio >= 1d) return 1d;
            if (timeCoefficient == 1d) return ratio;
            double ratioZoom = Math.Exp(timeCoefficient * Math.Log(ratio));
            return ratioZoom;
        }
        /// <summary>
        /// Vrací koeficient TimeZoomu
        /// </summary>
        /// <param name="timeZoom"></param>
        /// <returns></returns>
        protected static double GetZoomCoefficient(double timeZoom)
        {
            if (timeZoom == 0d) return 1d;                                                    // Explicitní zkratka
            double zoom = (timeZoom < -10d ? -10d : (timeZoom > 10d ? 10d : timeZoom));       // Platný rozsah je -10 až +10
            double coeff = (100d / (10d + ((4d * zoom) + 40d))) - 1d;                         // Odpovídající koeficient = +9.000 až +0.111
            return coeff;
        }
        /// <summary>
        /// Vrací Ratio (v rozmezí 0-1) pro danou pozici (v rozmezí 0-1) pro sinusovou křivku.
        /// Provádí fázové a frekvenční posuny úhlu a modifikaci výsledku do cílového rozsahu.
        /// Vrací tedy hodnotu:
        /// <code>
        /// <paramref name="resultOffset"/> + (<paramref name="resultCoefficient"/> * Math.Sin (<paramref name="angleOffset"/> * PI + (<paramref name="position"/> * <paramref name="angleCoefficient"/> * PI)));
        /// </code>
        /// <para/>
        /// Pokud je zadáno <paramref name="trim"/> = true, a vstupní hodnota <paramref name="position"/> je mimo rozsah 0-1, pak vrací hodnotu 0 nebo 1.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="trim"></param>
        /// <param name="angleOffset"></param>
        /// <param name="angleCoefficient"></param>
        /// <param name="resultOffset"></param>
        /// <param name="resultCoefficient"></param>
        /// <returns></returns>
        private static double _GetSinusRatio(double position, bool trim,  double angleOffset, double angleCoefficient, double resultOffset, double resultCoefficient)
        {
            if (trim)
            {   // Pokud jsem pod 0 nebo nad 1, vracím krajní meze:
                if (position <= 0d) return 0d;
                if (position >= 1d) return 1d;
            }
            double angle = angleOffset * Math.PI + (position * angleCoefficient * Math.PI);
            double ratio = resultOffset + (resultCoefficient * Math.Sin(angle));
            return ratio;
        }
        private static double _GetAlignedRatio(double position)
        {
            return (position < 0d ? 0d : (position > 1d ? 1d : position));
        }
        #endregion
        #endregion
    }
    #region class AnimatedValueSet : Sada hodnot na lineární ose
    /// <summary>
    /// Sada hodnot na lineární ose
    /// </summary>
    public class AnimatedValueSet
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AnimatedValueSet()
        {
            __ValueType = null;
            __Dictionary = new Dictionary<double, ValuePair>();
            _ResetValues();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ValueType: {ValueType}; ValueCount: {Count}";
        }
        private AnimatedValueType? __ValueType;
        private ValuePair[] __Values;
        private Dictionary<double, ValuePair> __Dictionary;
        /// <summary>
        /// Typ hodnot zde evidovaných. Pokud není žádná, je zde <see cref="AnimatedValueType.None"/>.
        /// </summary>
        public AnimatedValueType ValueType { get { return this.__ValueType ?? AnimatedValueType.None; } }
        /// <summary>
        /// Počet hodnot
        /// </summary>
        public int Count { get { return __Dictionary.Count; } }
        /// <summary>
        /// Pozice prvního prvku
        /// </summary>
        public double StartPosition { get { _CheckValues(); return __StartPosition.Value; } }
        private double? __StartPosition;
        /// <summary>
        /// Pozice posledního prvku
        /// </summary>
        public double EndPosition { get { _CheckValues(); return __EndPosition.Value; } }
        private double? __EndPosition;
        /// <summary>
        /// Najde a vrátí hodnotu na dané relativní pozici (tj. vstupní hodnota má typicky rozsah 0 ÷ 1, který se interpoluje do rozsahu zdejších hodnot).
        /// Provádí interpolaci mezi hodnotami na explicitních pozicích. Řeší pozice před <see cref="StartPosition"/> a za <see cref="EndPosition"/>.
        /// </summary>
        /// <param name="positionRelative"></param>
        /// <returns></returns>
        public object GetValueAtPosition(double positionRelative)
        {
            var values = this.Values;
            if (values.Length == 0) return null;                     // Nejsou zadána data

            ValuePair pairBefore = null;
            ValuePair pairAfter = null;
            foreach (var pair in values)
            {
                if (pair.PositionRelative == positionRelative) return pair.Value;    // Exaktní shoda hledané pozice s některým prvkem: nebudeme hledat další, a nebudeme ani interpolovat...
                if (pair.PositionRelative > positionRelative)
                {   // Nalezený pár je na vyšší pozici = je to pár "za hledanou pozicí":
                    pairAfter = pair;
                    break;
                }
                // Nalezený pár je na nižší pozici = je to poslední pár "před hledanou pozicí":
                pairBefore = pair;
            }
            if (pairBefore is null) return pairAfter.Value;          // pairAfter je první ze zadaných hodnot, a hledaná pozice je před ním = nebude interpolace...
            if (pairAfter is null) return pairBefore.Value;          // Žádný pár není na vyšší pozici než je hledaná hodnota = všechny páry jsou nižší (poslední z nižších je pairBefore) = nebude interpolace...

            // Máme tedy páry před a po (na jejich pozicích), určíme tedy relativní pozici (morphRatio) a poté interpolovanou hodnotu na této pozici:
            double morphRatio = ((positionRelative - pairBefore.PositionRelative) / (pairAfter.PositionRelative - pairBefore.PositionRelative));
            return ValueSupport.MorphValue(this.ValueType, pairBefore.Value, morphRatio, pairAfter.Value);
        }
        /// <summary>
        /// Vyprázdní set
        /// </summary>
        public void Clear()
        {
            __ValueType = null;
            __Values = null;
            __Dictionary.Clear();
        }
        /// <summary>
        /// Přidá další pozici a hodnotu na pozici.
        /// Hodnota <paramref name="value"/> musí být některého z podporovaných typů <see cref="AnimatedValueType"/>, všechny hodnoty v jednom setu musí být stejného typu, viz <see cref="ValueType"/>.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public void Add(double position, object value)
        {
            if (value is null) 
                throw new ArgumentException($"Třída 'AnimatedValueSet' nedokáže zpracovat hodnotu NULL.");

            var valueType = ValueSupport.GetValueType(value);
            if (valueType == AnimatedValueType.Other) 
                throw new ArgumentException($"Třída 'AnimatedValueSet' nedokáže zpracovat hodnotu typu '{value.GetType().Name}'.");
            if (__ValueType.HasValue && __ValueType.Value != valueType)
                throw new ArgumentException($"Třída 'AnimatedValueSet' nedokáže zpracovat hodnoty různých typů: dosavadní typ='{this.__ValueType.Value}', přidávaná hodnota je typu {valueType}.");

            if (__Dictionary.ContainsKey(position))
                throw new ArgumentException($"Třída 'AnimatedValueSet' nemůže evidovat pro jednu pozici ({position}) více než jednu hodnotu, nyní je přidávána duplicitní hodnota pro již existující pozici.");

            if (!__ValueType.HasValue)
                __ValueType = valueType;
            __Dictionary.Add(position, new ValuePair(position, value));

            _ResetValues();
        }
        /// <summary>
        /// Setříděné pole hodnot
        /// </summary>
        protected ValuePair[] Values { get { _CheckValues(); return __Values; } }
        /// <summary>
        /// Resetuje analyzované hodnoty
        /// </summary>
        private void _ResetValues()
        {
            __Values = null;
            __StartPosition = null;
            __EndPosition = null;

        }
        /// <summary>
        /// Zajistí platnost analyzovaných hodnot
        /// </summary>
        private void _CheckValues()
        {
            bool isValid = (__Values != null && __Values.Length == __Dictionary.Count && __StartPosition.HasValue && __EndPosition.HasValue);
            if (isValid) return;

            List<ValuePair> values = __Dictionary.Values.ToList();
            values.Sort((a, b) => a.Position.CompareTo(b.Position));

            double startPosition = 0d;
            double endPosition = 0d;
            if (values.Count > 0)
            {
                startPosition = values[0].Position;
                endPosition = values[values.Count - 1].Position;
                double length = endPosition - startPosition;
                foreach (var value in values)
                    value.PositionRelative = (value.Position - startPosition) / length;
            }

            __Values = values.ToArray();
            __StartPosition = startPosition;
            __EndPosition = endPosition;
        }
        /// <summary>
        /// Třída pro uložení pozice a hodnoty
        /// </summary>
        protected class ValuePair
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="position"></param>
            /// <param name="value"></param>
            public ValuePair(double position, object value)
            {
                this.Position = position;
                this.Value = value;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Position: {Position}; Value: {Value}";
            }
            /// <summary>
            /// Pozice této hodnoty, daná aplikací
            /// </summary>
            public double Position { get; private set; }
            /// <summary>
            /// Pozice této hodnoty, v rozsahu 0-1
            /// </summary>
            public double PositionRelative { get; set; }
            /// <summary>
            /// Hodnota
            /// </summary>
            public object Value { get; private set; }
        }
    }
    #endregion
    #region class ValueSupport : Podpůrné metody pro hodnoty
    /// <summary>
    /// Podpůrné metody pro hodnoty
    /// </summary>
    public static class ValueSupport
    {
        #region Get
        /// <summary>
        /// Metoda prověří, zda dodané hodnoty jsou přípustné hodnoty do animátoru.
        /// Pokud ne, dojde k chybě.
        /// Pokud ano, bude do out <paramref name="valueType"/> vložen typ hodnoty.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <param name="valueType"></param>
        public static void CheckValues(object startValue, object endValue, out AnimatedValueType valueType)
        {
            valueType = AnimatedValueType.None;

            // Typ hodnot?
            AnimatedValueType svt = ValueSupport.GetValueType(startValue);
            AnimatedValueType evt = ValueSupport.GetValueType(endValue);

            // Nesmí být Null:
            if (svt == AnimatedValueType.Null || evt == AnimatedValueType.Null)
                throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' nesmí být null.");

            // Musí být shodné:
            if (svt != evt)
                throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' musí být obě stejného typu. Aktuálně 'startValue': {startValue.GetType().Name}, a 'endValue': {endValue.GetType().Name}.");

            // Nesmí být Other:
            if (svt == AnimatedValueType.Other)
                throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' musí být jen určitých typů. Aktuální typ '{startValue.GetType().Name}' není podporován, není připravena metoda pro interpolaci hodnoty.");

            valueType = svt;
        }
        /// <summary>
        /// Vrátí enumerační typ hodnoty, pod kterým je podporována v třídě <see cref="ValueSupport"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static AnimatedValueType GetValueType(object value)
        {
            if (value is null) return AnimatedValueType.Null;

            // Podporované typy:
            string typeName = value.GetType().FullName;
            switch (typeName)
            {
                case "System.Int16": return AnimatedValueType.Int16;
                case "System.Int32": return AnimatedValueType.Int32;
                case "System.Int64": return AnimatedValueType.Int64;
                case "System.Single": return AnimatedValueType.Single;
                case "System.Double": return AnimatedValueType.Double;
                case "System.Decimal": return AnimatedValueType.Decimal;
                case "System.DateTime": return AnimatedValueType.DateTime;
                case "System.Drawing.Point": return AnimatedValueType.Point;
                case "System.Drawing.Size": return AnimatedValueType.Size;
                case "System.Drawing.Color": return AnimatedValueType.Color;
            }
            return AnimatedValueType.Other;
        }
        #endregion
        #region IsEqual
        public static bool IsEqualColors(Color oldValue, Color newValue)
        {
            return (oldValue.A == newValue.A &&
                    oldValue.R == newValue.R &&
                    oldValue.G == newValue.G &&
                    oldValue.B == newValue.B);
        }
        #endregion
        #region Morph
        /// <summary>
        /// Metoda vrátí hodnotu daného typu na dané pozici mezi hodnotami Start a End.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="morphRatio"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        public static object MorphValue(object startValue, double morphRatio, object endValue)
        {
            ValueSupport.CheckValues(startValue, endValue, out AnimatedValueType valueType);
            return MorphValue(valueType, startValue, morphRatio, endValue);
        }
        /// <summary>
        /// Metoda vrátí hodnotu daného typu na dané pozici mezi hodnotami Start a End.
        /// </summary>
        /// <param name="valueType"></param>
        /// <param name="startValue"></param>
        /// <param name="morphRatio"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        public static object MorphValue(AnimatedValueType valueType, object startValue, double morphRatio, object endValue)
        {
            switch (valueType)
            {
                case AnimatedValueType.Int16: return ValueSupport.MorphValueInt16((Int16)startValue, morphRatio, (Int16)endValue);
                case AnimatedValueType.Int32: return ValueSupport.MorphValueInt32((Int32)startValue, morphRatio, (Int32)endValue);
                case AnimatedValueType.Int64: return ValueSupport.MorphValueInt64((Int64)startValue, morphRatio, (Int64)endValue);
                case AnimatedValueType.Single: return ValueSupport.MorphValueSingle((Single)startValue, morphRatio, (Single)endValue);
                case AnimatedValueType.Double: return ValueSupport.MorphValueDouble((Double)startValue, morphRatio, (Double)endValue);
                case AnimatedValueType.Decimal: return ValueSupport.MorphValueDecimal((Decimal)startValue, morphRatio, (Decimal)endValue);
                case AnimatedValueType.DateTime: return ValueSupport.MorphValueDateTime((DateTime)startValue, morphRatio, (DateTime)endValue);
                case AnimatedValueType.Point: return ValueSupport.MorphValuePoint((Point)startValue, morphRatio, (Point)endValue);
                case AnimatedValueType.Size: return ValueSupport.MorphValueSize((Size)startValue, morphRatio, (Size)endValue);
                case AnimatedValueType.Color: return ValueSupport.MorphValueColor((Color)startValue, morphRatio, (Color)endValue);
            }
            throw new ArgumentException($"Nelze provést výpočet MorphValue pro typ hodnoty 'valueType' = '{valueType}'.");
        }
        public static Byte MorphValueByte(Byte startValue, double morphRatio, Byte endValue)
        {
            var diffValue = (int)(Math.Round(morphRatio * (int)(endValue - startValue), 0));
            var resultValue = startValue + diffValue;
            if (resultValue < 0) return (Byte)0;
            if (resultValue > 255) return (Byte)255;
            return (Byte)resultValue;
        }
        public static Int16 MorphValueInt16(Int16 startValue, double morphRatio, Int16 endValue)
        {
            var diffValue = (Int16)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            var resultValue = startValue + diffValue;
            if (resultValue < Int16.MinValue) return Int16.MinValue;
            if (resultValue > Int16.MaxValue) return Int16.MaxValue;
            return (Int16)resultValue;
        }
        public static Int32 MorphValueInt32(Int32 startValue, double morphRatio, Int32 endValue)
        {
            var diffValue = (Int32)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static Int64 MorphValueInt64(Int64 startValue, double morphRatio, Int64 endValue)
        {
            var diffValue = (Int64)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static Single MorphValueSingle(Single startValue, double morphRatio, Single endValue)
        {
            var diffValue = (Single)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static Double MorphValueDouble(Double startValue, double morphRatio, Double endValue)
        {
            var diffValue = (Double)(Math.Round(morphRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static Decimal MorphValueDecimal(Decimal startValue, double morphRatio, Decimal endValue)
        {
            var diffValue = (Decimal)(Math.Round((Decimal)morphRatio * (Decimal)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        public static DateTime MorphValueDateTime(DateTime startValue, double morphRatio, DateTime endValue)
        {
            TimeSpan diffValue = TimeSpan.FromSeconds(morphRatio * ((TimeSpan)(endValue - startValue)).TotalSeconds);
            return startValue + diffValue;
        }
        public static Point MorphValuePoint(Point startValue, double morphRatio, Point endValue)
        {
            int x = MorphValueInt32(startValue.X, morphRatio, endValue.X);
            int y = MorphValueInt32(startValue.Y, morphRatio, endValue.Y);
            return new Point(x, y);
        }
        public static Size MorphValueSize(Size startValue, double morphRatio, Size endValue)
        {
            int width = MorphValueInt32(startValue.Width, morphRatio, endValue.Width);
            int height = MorphValueInt32(startValue.Height, morphRatio, endValue.Height);
            return new Size(width, height);
        }
        public static Color MorphValueColor(Color startValue, double morphRatio, Color endValue)
        {
            byte a = MorphValueByte(startValue.A, morphRatio, endValue.A);
            byte r = MorphValueByte(startValue.R, morphRatio, endValue.R);
            byte g = MorphValueByte(startValue.G, morphRatio, endValue.G);
            byte b = MorphValueByte(startValue.B, morphRatio, endValue.B);
            return Color.FromArgb(a, r, g, b);
        }
        #endregion
    }
    /// <summary>
    /// Barva v režimu HSV
    /// </summary>
    public struct ColorHSV
    {
        /// <summary>
        /// Alfa kanál v rozsahu 0.00 ÷ 1.00 : 0.00 = neviditelná průhledná jako sklo / 1.00 = plná naprosto neprůhledná
        /// </summary>
        public double Alpha { get; set; }
        /// <summary>
        /// Odstín v rozsahu 0.0 ÷ 360.0°
        /// </summary>
        public double Hue { get; set; }
        /// <summary>
        /// Saturace v rozsahu 0.00 ÷ 1.00
        /// </summary>
        public double Saturation { get; set; }
        /// <summary>
        /// Světlost v rozsahu 0.00 ÷ 1.00
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// Systémová barva
        /// </summary>
        public Color Color
        {
            get
            {
                int a = Convert.ToInt32(255d * Alpha);
                var hue = Hue;
                var saturation = Saturation;
                var value = Value;

                int hi = Convert.ToInt32(Math.Floor(hue / 60d)) % 6;
                double f = hue / 60d - Math.Floor(hue / 60d);

                value = value * 255d;
                int v = Convert.ToInt32(value);
                int p = Convert.ToInt32(value * (1 - saturation));
                int q = Convert.ToInt32(value * (1 - f * saturation));
                int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

                switch (hi)
                {
                    case 0: return Color.FromArgb(a, v, t, p);
                    case 1: return Color.FromArgb(a, q, v, p);
                    case 2: return Color.FromArgb(a, p, v, t);
                    case 3: return Color.FromArgb(a, p, q, v);
                    case 4: return Color.FromArgb(a, t, p, v);
                    default: return Color.FromArgb(a, v, p, q);
                }
            }
            set
            {
                var color = value;

                double max = Math.Max(color.R, Math.Max(color.G, color.B));
                double min = Math.Min(color.R, Math.Min(color.G, color.B));

                this.Alpha = (double)color.A / 255d;
                this.Hue = color.GetHue();
                this.Saturation = (max == 0) ? 0 : 1d - (1d * min / max);
                this.Value = max / 255d;
            }
        }
        public static ColorHSV FromHSV(double hue, double saturation, double value)
        {
            return FromAHSV(1d, hue, saturation, value);
        }
        public static ColorHSV FromAHSV(double alpha, double hue, double saturation, double value)
        {
            ColorHSV colorHSV = new ColorHSV();
            colorHSV.Alpha = _Align(alpha, 0d, 1d);
            colorHSV.Hue = _Align(hue, 0d, 360d);
            colorHSV.Saturation = _Align(saturation, 0d, 1d);
            colorHSV.Value = _Align(value, 0d, 1d);
            return colorHSV;
        }
        public static ColorHSV FromColor(Color color)
        {
            ColorHSV colorHSV = new ColorHSV();
            colorHSV.Color = color;
            return colorHSV;
        }

      
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        private static double _Align(double value, double min, double max) { return (value > max ? max : (value < min ? min : value)); }
    }
    #endregion
    #region class WinMMTimer : Timer založený na 'Winmm.dll' TimeEvents
    /// <summary>
    /// Timer založený na 'Winmm.dll' TimeEvents.
    /// Je podstatně přesnější než <see cref="System.Threading.Thread.Sleep(int)"/> (tam je časové okno 15 milisekund a nejistý výsledek).
    /// A nesežere celý výkon jádra CPU pro aktuální Thread.
    /// Tedy je to nenáročný a přesný Timer.
    /// </summary>
    public class WinMMTimer : IDisposable
    {
        #region Private a DllImport, Dispose, proměnné
        //Lib API declarations
        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerCallback lpTimeProc, UIntPtr dwUser, uint fuEvent);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeKillEvent(uint uTimerID);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeGetTime();

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeEndPeriod(uint uPeriod);

        [Flags]
        enum fuEvent : uint
        {
            TIME_ONESHOT = 0,      //Event occurs once, after uDelay milliseconds.
            TIME_PERIODIC = 1,
            TIME_CALLBACK_FUNCTION = 0x0000,  /* callback is function */
            TIME_CALLBACK_EVENT_SET = 0x0010, /* callback is event - use SetEvent */
            TIME_CALLBACK_EVENT_PULSE = 0x0020  /* callback is event - use PulseEvent */
        }

        delegate void TimerCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);
        /// <summary>
        /// Metoda 
        /// </summary>
        /// <param name="uTimerID"></param>
        /// <param name="uMsg"></param>
        /// <param name="dwUser"></param>
        /// <param name="dw1"></param>
        /// <param name="dw2"></param>
        void _CallbackMethod(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
        {
            //Callback from the MMTimer API that fires the Timer event. Note we are in a different thread here
            OnTimer();
            Timer?.Invoke(this, EventArgs.Empty);
            if (__AutoResetSetSignal)
            {   // Spolupráce s metodou Wait:
                __AutoResetSetSignal = false;
                __AutoResetEvent.Set();
            }
        }

        private bool __Disposed = false;

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void _Dispose(bool disposing)
        {
            if (!this.__Disposed)
            {
                if (disposing)
                {
                    Stop();
                }
            }
            __Disposed = true;
        }
        /// <summary>
        /// Semafor pro metodu <see cref="Wait(uint)"/>
        /// </summary>
        private System.Threading.AutoResetEvent __AutoResetEvent;
        /// <summary>
        /// Příznak vyžadující aktivaci signálu <see cref="__AutoResetEvent"/> po dosažení timeru
        /// </summary>
        private bool __AutoResetSetSignal;

        ~WinMMTimer()
        {
            _Dispose(false);
        }
        /// <summary>
        /// The current timer instance ID
        /// </summary>
        uint __TimerId = 0;
        /// <summary>
        /// The callback used by the the API
        /// </summary>
        TimerCallback __CallbackDelegate;
        #endregion
        #region Public
        /// <summary>
        /// Konstruktor.
        /// <para/>
        /// Timer lze použít těmito způsoby:<br/>
        /// 1. Vytvořit instanci; Zaregistrovat svůj eventhandler do eventu <see cref="Timer"/>; Nastartovat běh Timeru voláním <see cref="Start(uint, bool)"/>: 
        /// v patřičném čase bude vyvolána daná událost;<br/>
        /// 2. Vytvořit instanci; Zavolat instanční metodu <see cref="Wait(uint)"/> (tam se počká daný čas); Pokračovat v práci; = pro opakované použití<br/>
        /// 3. Nevytvářet instanci; Zavolat statickou metodu <see cref="Sleep(uint)"/> (tam se počká daný čas); Pokračovat v práci; = pro jednorázové použití<br/>
        /// </summary>
        public WinMMTimer()
        {
            //Initialize the API callback
            __CallbackDelegate = _CallbackMethod;
            __AutoResetEvent = new AutoResetEvent(false);
        }
        /// <summary>
        /// Událost volaná po dosažení timeru
        /// </summary>
        public event EventHandler Timer;
        /// <summary>
        /// Proběhnutí časovače
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTimer() { }
        /// <summary>
        /// Start a timer instance. Pokud nyní nějaký běží, bude zastaven!
        /// </summary>
        /// <param name="ms">Timer interval in milliseconds</param>
        /// <param name="repeat">If true sets a repetitive event, otherwise sets a one-shot</param>
        public void Start(uint ms, bool repeat)
        {
            //Kill any existing timer
            Stop();

            //Set the timer type flags
            fuEvent f = fuEvent.TIME_CALLBACK_FUNCTION | (repeat ? fuEvent.TIME_PERIODIC : fuEvent.TIME_ONESHOT);

            lock (this)
            {
                __TimerId = timeSetEvent(ms, 0, __CallbackDelegate, UIntPtr.Zero, (uint)f);
                if (__TimerId == 0)
                    throw new Exception("timeSetEvent error");
            }
        }
        /// <summary>
        /// Stop the current timer instance (if any)
        /// </summary>
        public void Stop()
        {
            lock (this)
            {
                if (__TimerId != 0)
                {
                    timeKillEvent(__TimerId);
                    __TimerId = 0;
                }
            }
        }
        /// <summary>
        /// Metoda zde počká přesně daný čas a poté vrátí řízení.
        /// Čas je reprezentován dostatečně přesně (jednotky milisekund) a metoda nezatěžuje CPU.
        /// </summary>
        /// <param name="ms"></param>
        public void Wait(uint ms)
        {
            Stop();
            __AutoResetEvent.Reset();
            __AutoResetSetSignal = true;
            Start(ms, false);
            __AutoResetEvent.WaitOne((int)(2 * ms));
        }
        /// <summary>
        /// Metoda zde počká přesně daný čas a poté vrátí řízení.
        /// Čas je reprezentován dostatečně přesně (jednotky milisekund) a metoda nezatěžuje CPU.
        /// </summary>
        /// <param name="ms"></param>
        public static void Sleep(uint ms)
        {
            using (var timer = new WinMMTimer())
            {
                timer.Wait(ms);
            }
        }
        #endregion
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Typ hodnoty
    /// </summary>
    public enum AnimatedValueType
    {
        None,
        Null,
        Other,
        Int16,
        Int32,
        Int64,
        Single,
        Double,
        Decimal,
        DateTime,
        Point,
        Size,
        Color
    }
    #endregion
}
