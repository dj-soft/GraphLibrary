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

                Working = true;
                Stopping = false;
                Drive = drive;
                Task.Factory.StartNew(Run);
            }
            else
            {
                CallWorkingDone();
            }
        }
        /// <summary>
        /// Zde je spuštěna výkonná část akce, ve Working threadu
        /// </summary>
        protected abstract void Run();
        /// <summary>
        /// Požádá o zastavení běhu testu
        /// </summary>
        public void Stop()
        {
            if (Working)
                Stopping = true;
        }
        /// <summary>
        /// Zpracovávaný disk
        /// </summary>
        public System.IO.DriveInfo Drive { get; protected set; }
        /// <summary>
        /// Pracovní fáze právě běží?
        /// </summary>
        public bool Working { get; protected set; }
        /// <summary>
        /// Je vydán požadavek na zastavení práce
        /// </summary>
        public bool Stopping { get; protected set; }
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
