using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        /// </summary>
        /// <param name="owner">Control, který bude invalidován po provedení změn</param>
        /// <param name="fps">Frames per second = počet ticků za sekundu. Default = 25, přípustná hodnota 1 až 100.</param>
        public Animator(Control owner = null, int? fps = null)
        {
            if (owner != null) this.__Owner = new WeakReference<System.Windows.Forms.Control>(owner);
            this.__Fps = (fps.HasValue ? (fps.Value < 1 ? 1 : (fps.Value > 100 ? 100 : fps.Value)) : 40);
            this.__Motion = new List<Motion>();
            this._TimerStart();
        }
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
        /// Frames per second = počet ticků za sekundu.
        /// </summary>
        public int Fps { get { return __Fps; } } private readonly int __Fps;
        /// <summary>
        /// Spustí časovou smyčku animace.
        /// Volá se v Main (GUI) threadu, nastartuje separátní thread na pozadí a tato metoda ihned poté skončí.
        /// </summary>
        private void _TimerStart()
        {
            this.__StopWatch = new System.Diagnostics.Stopwatch();

            this.__TimerThread = new Thread(_TimerLoop);
            this.__TimerThread.IsBackground = true;
            this.__TimerThread.Priority = ThreadPriority.BelowNormal;
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
            while (true)
            {
                if (__AnimatorTimerStop) break;
                int sleepTimeMiliseconds = (1000 / __Fps);
                if (__Motion.Count > 0)
                {   // Naprostou většinu času bude počet akcí = 0, takže by bylo zbytečné provádět invokaci GUI threadu...
                    __StopWatch.Restart();
                    _RunInGui(_OneTick);
                    if (__AnimatorTimerStop) break;
                    sleepTimeMiliseconds -= (int)__StopWatch.ElapsedMilliseconds;
                }
                if (sleepTimeMiliseconds > 0)
                    Thread.Sleep(sleepTimeMiliseconds);
            }
        }
        /// <summary>
        /// Thread běžící na pozadí
        /// </summary>
        private Thread __TimerThread;
        /// <summary>
        /// Přesný časovač pro měření režijního času ticku, pro určení přesného času Sleep mezi dvěma Ticky tak, aby byl dodržen <see cref="Fps"/>
        /// </summary>
        private System.Diagnostics.Stopwatch __StopWatch;
        /// <summary>
        /// Příznak, že časová smyčka animátoru má být zastavena.
        /// Výchozí hodnota je false.
        /// Lze nastavit true, tím animátor skončí svoji práci. Ale nelze pak již nikdy nastavit false. Používá se typicky v metodě Dispose() controlu vlastníka, když už animace nebude prováděna.
        /// </summary>
        public bool AnimatorTimerStop { get { return __AnimatorTimerStop; } set { __AnimatorTimerStop |= value; } } private bool __AnimatorTimerStop;
        #endregion
        #region Správa animačních akcí
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="stepCount">Počet kroků na celý cyklus. Animátor provede <see cref="Fps"/> kroků za jednu sekundu, default 25. Čas jednoho cyklu animace v sekundách je tedy <paramref name="stepCount"/> / <see cref="Fps"/>.</param>
        /// <param name="timeMode"></param>
        /// <param name="timeZoom"></param>
        /// <param name="action">Akce volaná v každém kroku</param>
        /// <param name="startValue">Počáteční hodnota</param>
        /// <param name="endValue">Koncová hodnota</param>
        /// <param name="userData">Libovolná data aplikace</param>
        public void AddMotion(int? stepCount, TimeMode timeMode, double timeZoom, Action<Motion> action, object startValue, object endValue, object userData)
        {
            Motion motion = new Motion(stepCount, timeMode, timeZoom, action, startValue, endValue, userData);
            lock (__Motion)
                __Motion.Add(motion);
        }
        /// <summary>
        /// Provede jeden animační krok = všechny platné akce plus Repaint ownera
        /// </summary>
        private void _OneTick()
        {
            Motion[] motions = null;
            lock (__Motion)
                motions = __Motion.ToArray();

            bool hasChanges = false;
            if (motions.Length > 0)
            {
                foreach (var motion in motions)
                    ((IMotionWorking)motion).DoTick(ref hasChanges);

                if (motions.Any(m => m.IsDone))
                {
                    lock (__Motion)
                        __Motion.RemoveAll(m => m.IsDone);
                }
            }

            // Pokud máme nějaké změny, promítneme je vizuálně do Ownera:
            if (hasChanges)
                _RepaintOwner();
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
                CheckValues(startValue, endValue, out ValueType valueType);

                __StepIndex = 0;
                __StepCount = (stepCount.HasValue && stepCount.Value > 0 ? stepCount.Value : -1);
                __TimeMode = timeMode;
                __TimeCoeff = GetZoomCoefficient(timeZoom);
                __Action = action;
                __ValueType = valueType;
                __StartValue = startValue;
                __EndValue = endValue;
                __CurrentValue = startValue;
                __UserData = userData;
            }
            /// <summary>
            /// Volaná akce
            /// </summary>
            private Action<Motion> __Action;
            /// <summary>
            /// Typ hodnoty
            /// </summary>
            private ValueType __ValueType;
            /// <summary>
            /// Dynamika pohybu
            /// </summary>
            private TimeMode __TimeMode;
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
            /// Pokud po provedení akce je zde true, pak tato animační akce končí a je vyřazena ze seznamu akcí.
            /// Aplikační kód může nastavit v kterémkoli kroku.
            /// Animační jádro <see cref="Animator"/> nastaví tuto proměnnou na true před vyvoláním posledního kroku, podle toho může uživatelský kód detekovat, že jde o poslední = finální krok.
            /// Aplikační kód může hodnotu vrátit na false, a pak tato akce bude volána i v příštím cyklu.
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
                __IsDone = false;
                _DoAction();
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
                object currentValue = GetCurrentValue(__StartValue, __EndValue, currentRatio, __ValueType);
                bool isChanged = !IsEqualValues(__CurrentValue, currentValue, __ValueType);
                __CurrentValue = currentValue;
                __CurrentRatio = currentRatio;
                __IsCurrentValueChanged = isChanged;
                __IsDone = (__TimeMode != TimeMode.Cycling && step >= __StepCount);
                _DoAction();

                // Cyklický pohyb, který nebyl aplikací ukončen, a aktuální krok (step) by vedl ke konci cyklu: nastavíme Index = -1, a navazující metoda jej inkrementuje na 0:
                if (__TimeMode == TimeMode.Cycling && !__IsDone && step >= __StepCount)
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
        /// <summary>
        /// Metoda prověří, zda dodané hodnoty jsou přípustné hodnoty do animátoru.
        /// Pokud ne, dojde k chybě.
        /// Pokud ano, bude do out <paramref name="valueType"/> vložen typ hodnoty.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        protected static void CheckValues(object startValue, object endValue, out ValueType valueType)
        {
            valueType = ValueType.None;

            // Null?
            bool sn = (startValue is null);
            bool en = (endValue is null);
            if (sn && en) return;
            if (sn || en) throw new ArgumentNullException($"Hodnoty 'startValue' a 'endValue' předávané do 'Animator' musí být obě zadané, nebo obě nezadané. Aktuálně 'startValue': {(sn ? "NULL" : "zadáno")}, a 'endValue': {(en ? "NULL" : "zadáno")}.");

            // Type?
            var st = startValue.GetType();
            var et = endValue.GetType();
            if (st != et) throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' předávané do 'Animator' musí být obě stejného typu. Aktuálně 'startValue': {st.Name}, a 'endValue': {et.Name}.");

            // Podporované typy:
            string typeName = st.FullName;
            switch (typeName)
            {
                case "System.Int16":
                    valueType = ValueType.Int16;
                    break;
                case "System.Int32":
                    valueType = ValueType.Int32;
                    break;
                case "System.Int64":
                    valueType = ValueType.Int64;
                    break;
                case "System.Single":
                    valueType = ValueType.Single;
                    break;
                case "System.Double":
                    valueType = ValueType.Double;
                    break;
                case "System.Decimal":
                    valueType = ValueType.Decimal;
                    break;
                case "System.Drawing.Point":
                    valueType = ValueType.Point;
                    break;
                case "System.Drawing.Size":
                    valueType = ValueType.Size;
                    break;
                case "System.Drawing.Color":
                    valueType = ValueType.Color;
                    break;
            }
            if (valueType == ValueType.None) throw new ArgumentException($"Hodnoty 'startValue' a 'endValue' musí být jen určitých typů. Aktuální typ '{typeName}' není podporován, není připravena metoda pro interpolaci hodnoty.");
        }
        #region Práce s hodnotami: GetCurrentValue, IsEqualValues
        /// <summary>
        /// Metoda vrátí "Current" hodnotu pro danou hodnotu Start a End a dané Ratio.
        /// </summary>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <param name="currentRatio"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        protected static object GetCurrentValue(object startValue, object endValue, double currentRatio, ValueType valueType)
        {
            switch (valueType)
            {
                case ValueType.Int16: return _GetCurrentValueInt16((Int16)startValue, (Int16)endValue, currentRatio);
                case ValueType.Int32: return _GetCurrentValueInt32((Int32)startValue, (Int32)endValue, currentRatio);
                case ValueType.Int64: return _GetCurrentValueInt64((Int64)startValue, (Int64)endValue, currentRatio);
                case ValueType.Single: return _GetCurrentValueSingle((Single)startValue, (Single)endValue, currentRatio);
                case ValueType.Double: return _GetCurrentValueDouble((Double)startValue, (Double)endValue, currentRatio);
                case ValueType.Decimal: return _GetCurrentValueDecimal((Decimal)startValue, (Decimal)endValue, currentRatio);
                case ValueType.Point: return _GetCurrentValuePoint((Point)startValue, (Point)endValue, currentRatio);
                case ValueType.Size: return _GetCurrentValueSize((Size)startValue, (Size)endValue, currentRatio);
                case ValueType.Color: return _GetCurrentValueColor((Color)startValue, (Color)endValue, currentRatio);
            }
            throw new ArgumentException($"Nelze provést výpočet CurrentValue pro typ hodnoty 'valueType' = '{valueType}'.");
        }
        private static Byte _GetCurrentValueByte(Byte startValue, Byte endValue, double currentRatio)
        {
            var diffValue = (int)(Math.Round(currentRatio * (int)(endValue - startValue), 0));
            var resultValue = startValue + diffValue;
            if (resultValue < 0) return (Byte)0;
            if (resultValue > 255) return (Byte)255;
            return (Byte)resultValue;
        }
        private static Int16 _GetCurrentValueInt16(Int16 startValue, Int16 endValue, double currentRatio)
        {
            var diffValue = (Int16)(Math.Round(currentRatio * (double)(endValue - startValue), 0));
            var resultValue = startValue + diffValue;
            if (resultValue < Int16.MinValue) return Int16.MinValue;
            if (resultValue > Int16.MaxValue) return Int16.MaxValue;
            return (Int16)resultValue;
        }
        private static Int32 _GetCurrentValueInt32(Int32 startValue, Int32 endValue, double currentRatio)
        {
            var diffValue = (Int32)(Math.Round(currentRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        private static Int64 _GetCurrentValueInt64(Int64 startValue, Int64 endValue, double currentRatio)
        {
            var diffValue = (Int64)(Math.Round(currentRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        private static Single _GetCurrentValueSingle(Single startValue, Single endValue, double currentRatio)
        {
            var diffValue = (Single)(Math.Round(currentRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        private static Double _GetCurrentValueDouble(Double startValue, Double endValue, double currentRatio)
        {
            var diffValue = (Double)(Math.Round(currentRatio * (double)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        private static Decimal _GetCurrentValueDecimal(Decimal startValue, Decimal endValue, double currentRatio)
        {
            var diffValue = (Decimal)(Math.Round((Decimal)currentRatio * (Decimal)(endValue - startValue), 0));
            return startValue + diffValue;
        }
        private static Point _GetCurrentValuePoint(Point startValue, Point endValue, double currentRatio)
        {
            int x = _GetCurrentValueInt32(startValue.X, endValue.X, currentRatio);
            int y = _GetCurrentValueInt32(startValue.Y, endValue.Y, currentRatio);
            return new Point(x, y);
        }
        private static Size _GetCurrentValueSize(Size startValue, Size endValue, double currentRatio)
        {
            int width = _GetCurrentValueInt32(startValue.Width, endValue.Width, currentRatio);
            int height = _GetCurrentValueInt32(startValue.Height, endValue.Height, currentRatio);
            return new Size(width, height);
        }
        private static Color _GetCurrentValueColor(Color startValue, Color endValue, double currentRatio)
        {
            byte a = _GetCurrentValueByte(startValue.A, endValue.A, currentRatio);
            byte r = _GetCurrentValueByte(startValue.R, endValue.R, currentRatio);
            byte g = _GetCurrentValueByte(startValue.G, endValue.G, currentRatio);
            byte b = _GetCurrentValueByte(startValue.B, endValue.B, currentRatio);
            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// Vrátí true, pokud dvě dodané hodnoty jsou shodné.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(object oldValue, object newValue, ValueType valueType) // object oldValue, object newValue
        {
            switch (valueType)
            {
                case ValueType.Int16: return ((Int16)oldValue == (Int16)newValue);
                case ValueType.Int32: return ((Int32)oldValue == (Int32)newValue);
                case ValueType.Int64: return ((Int64)oldValue == (Int64)newValue);
                case ValueType.Single: return ((Single)oldValue == (Single)newValue);
                case ValueType.Double: return ((Double)oldValue == (Double)newValue);
                case ValueType.Decimal: return ((Decimal)oldValue == (Decimal)newValue);
                case ValueType.Point: return ((Point)oldValue == (Point)newValue);
                case ValueType.Size: return ((Size)oldValue == (Size)newValue);
                case ValueType.Color: return _IsEqualValues((Color)oldValue, (Color)newValue);
            }
            throw new ArgumentException($"Nelze provést výpočet vyhodnocení IsEqualValues pro typ hodnoty 'valueType' = '{valueType}'.");
        }
        private static bool _IsEqualValues(Color oldValue, Color newValue)
        {
            return (oldValue.A == newValue.A &&
                    oldValue.R == newValue.R &&
                    oldValue.G == newValue.G &&
                    oldValue.B == newValue.B);
        }
        /// <summary>
        /// Typ hodnoty
        /// </summary>
        protected enum ValueType
        {
            None,
            Int16,
            Int32,
            Int64,
            Single,
            Double,
            Decimal,
            Point,
            Size,
            Color
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
}
