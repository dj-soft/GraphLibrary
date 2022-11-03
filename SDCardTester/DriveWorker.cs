using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Tools.SDCardTester
{
    /// <summary>
    /// Bázová třída pro konkrétní pracovní třídy
    /// </summary>
    public abstract class DriveWorker
    {
        #region Konstrukce a public rozhraní
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DriveWorker()
        {
            InitStopwatch();
            InitState();
            InitData();
        }
        /// <summary>
        /// Inicializace dat v rámci konstruktoru
        /// </summary>
        protected virtual void InitData() { }
        /// <summary>
        /// Požádá o zahájení akce na pozadí
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="action">Akce volaná ještě před startem </param>
        protected void StartAction(System.IO.DriveInfo drive, Action action = null)
        {
            if (drive != null && drive.IsReady && !Working)
            {
                action?.Invoke();

                __State = RunState.Run;
                Drive = drive;
                Task.Factory.StartNew(Run);
            }
            else
            {
                CallWorkingDone();
            }
        }
        /// <summary>
        /// Inicializace stavu
        /// </summary>
        protected void InitState()
        {
            __State = RunState.None;
            __Signal = new System.Threading.AutoResetEvent(false);
        }
        /// <summary>
        /// Zde je spuštěna výkonná část akce, ve Working threadu
        /// </summary>
        protected abstract void Run();
        /// <summary>
        /// Požádá o zastavení běhu testu
        /// </summary>
        public void ChangeState(RunState state)
        {
            this.State = state;
        }
        /// <summary>
        /// Zpracovávaný disk
        /// </summary>
        public System.IO.DriveInfo Drive { get; protected set; }
        /// <summary>
        /// Stav běhu
        /// </summary>
        public RunState State 
        {
            get { return __State; }
            set 
            {
                var oldValue = __State;
                var newValue = value;
                if ((oldValue == RunState.None && (newValue == RunState.Run || newValue == RunState.Pause || newValue == RunState.Stop)) ||
                    (oldValue == RunState.Run && (newValue == RunState.Pause || newValue == RunState.Stop)) ||
                    (oldValue == RunState.Pause && (newValue == RunState.Run || newValue == RunState.Stop)) ||
                    (oldValue == RunState.Stop && (newValue == RunState.None)))
                {
                    __State = value;
                    __Signal.Set();
                }
            }
        }
        /// <summary>
        /// Úložiště stavu
        /// </summary>
        private RunState __State;
        /// <summary>
        /// Signál pro čekající thread, viz property <see cref="Stopping"/>
        /// </summary>
        private System.Threading.AutoResetEvent __Signal;
        /// <summary>
        /// Pracovní fáze právě běží?
        /// Tedy jsme ve stavu Run / Pause / Stop, ale ne None
        /// </summary>
        public bool Working 
        {
            get 
            {
                var state = __State;
                return (state == RunState.Run || state == RunState.Pause || state == RunState.Stop);
            }
        }
        /// <summary>
        /// Tuto hodnotu testuje pracující vlákno.<br/>
        /// Pokud jsme ve stavu <see cref="State"/> == <see cref="RunState.Run"/>, pak property vrací false = nejsme zastaveni, a pracovní algoritmus pokračuje.<br/>
        /// Pokud jsme ve stavu <see cref="State"/> == <see cref="RunState.Stop"/>, pak tato property vrací true = jsme zastaveni, a pracovní algoritmus skončí.<br/>
        /// Pokud jsme ve stavu <see cref="State"/> == <see cref="RunState.Pause"/>, pak tato property nic nevrací a čeká, až bude stav Run nebo Stop.
        /// </summary>
        public bool Stopping 
        {
            get
            {
                this.Stopwatch.Stop();           // Pozastaví měření času. Pokud budeme v pauze, nepoběží časomíra = zkreslení rychlosti přenisu.
                var state = __State;
                while (true)
                {
                    state = __State;
                    if (state == RunState.Run || state == RunState.Stop || state == RunState.None) break;
                    __Signal.WaitOne(100);
                }
                this.Stopwatch.Start();          // Rozběhneme měření času, protože vracíme řízení.
                return (state == RunState.Stop || state == RunState.None);
            }
        }
        /// <summary>
        /// Časový interval, po jehož uplynutí se může opakovaně volat událost <see cref="WorkingStep"/>.
        /// </summary>
        public TimeSpan WorkingStepTime { get; set; }
        /// <summary>
        /// Vrátí true, pokud je vhodné volat <see cref="CallWorkingStep"/>
        /// </summary>
        /// <param name="force"></param>
        protected bool CanCallWorkingStep(bool force)
        {
            if (force) return true;
            var nowTime = DateTime.Now;
            var lastTime = LastStepTime;
            var stepTime = WorkingStepTime;
            return (!lastTime.HasValue || stepTime.TotalMilliseconds <= 0d || (lastTime.HasValue && ((TimeSpan)(nowTime - lastTime.Value) >= stepTime)));
        }
        /// <summary>
        /// Čas posledního hlášení změny
        /// </summary>
        protected DateTime? LastStepTime { get; set; }
        /// <summary>
        /// Vyvolá událost <see cref="WorkingStep"/>
        /// </summary>
        protected void CallWorkingStep()
        {
            WorkingStep?.Invoke(this, EventArgs.Empty);
            LastStepTime = DateTime.Now;
        }
        /// <summary>
        /// Událost vyvolaná po nějakém pokroku v akci, mezi dvěma událostmi bude čas nejméně <see cref="WorkingStepTime"/> i kdyby změny nastaly častěji.
        /// </summary>
        public event EventHandler WorkingStep;
        /// <summary>
        /// Vyvolá událost <see cref="WorkingDone"/>
        /// </summary>
        protected void CallWorkingDone()
        {
            WorkingDone?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost vyvolaná po jakémkoli doběhnutí testu, i po chybách.
        /// </summary>
        public event EventHandler WorkingDone;
        #endregion
        #region Přesná časomíra
        /// <summary>
        /// Inicializuje časomíru
        /// </summary>
        protected void InitStopwatch()
        {
            Stopwatch = new System.Diagnostics.Stopwatch();
            Frequency = (decimal)System.Diagnostics.Stopwatch.Frequency;
            WorkingStepTime = TimeSpan.FromMilliseconds(333);
        }
        /// <summary>
        /// Nuluje a nastartuje časomíru
        /// </summary>
        /// <returns></returns>
        protected long RestartStopwatch()
        {
            Stopwatch.Restart();
            return Stopwatch.ElapsedTicks;
        }
        /// <summary>
        /// Aktuální čas (ticky), použije se jako parametr do metody <see cref="GetSeconds(long)"/> na konci měřeného cyklu
        /// </summary>
        protected long CurrentTime { get { return Stopwatch.ElapsedTicks; } }
        /// <summary>
        /// Vrátí počet sekund od daného počátečního času. Bez parametru = od restartu časovače = <see cref="RestartStopwatch"/>.
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        protected decimal GetSeconds(long startTime = 0L) { return GetSeconds(startTime, CurrentTime); }
        /// <summary>
        /// Vrátí počet sekund od daného počátečního do daného koncového času.
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        protected decimal GetSeconds(long startTime, long stopTime)
        {
            decimal elapsedTime = (decimal)(stopTime - startTime);
            return elapsedTime / Frequency;
        }
        /// <summary>
        /// Časovač
        /// </summary>
        protected System.Diagnostics.Stopwatch Stopwatch;
        /// <summary>
        /// Frekvence časovače = počet ticků / sekunda
        /// </summary>
        protected decimal Frequency;
        #endregion
    }
    #region class TextDataInfo
    /// <summary>
    /// Třída obsahující textový popisek a libovolná data
    /// </summary>
    public class TextDataInfo
    {
        public override string ToString()
        {
            return this.Text;
        }
        public string Text { get; set; }
        public object Data { get; set; }
    }
    #endregion
    #region class DriveResultControl
    /// <summary>
    /// Společný předek pro třídy obsahující výsledky testu a analýzy
    /// </summary>
    public class WorkingResultControl : Control
    {
        public WorkingResultControl()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.ContainerControl | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, false);
            InitControls();
        }
        protected virtual void InitControls()
        {
            this.Size = new Size(293, CurrentOptimalHeight);
        }
        /// <summary>
        /// Zdejší optimální výška
        /// </summary>
        protected virtual int CurrentOptimalHeight { get { return 28; } }
        /// <summary>
        /// Refresh - umí sám přejít do GUI threadu
        /// </summary>
        public override void Refresh()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(base.Refresh));
            else
                base.Refresh();
        }
    }
    #endregion
}
