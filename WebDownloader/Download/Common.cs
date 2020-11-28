using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;
using Djs.Tools.WebDownloader.Support;

namespace Djs.Tools.WebDownloader.Download
{
    #region class WebBase + enum WorkingState
    /// <summary>
    /// Bázová třída pro funkční potomky (nikoli GUI, ale výkonné)
    /// </summary>
    public class WebBase
    {
        #region Konstruktor, State
        /// <summary>
        /// Konstruktor
        /// </summary>
        public WebBase()
        {
            this._State = WorkingState.Initiated;
        }
        /// <summary>
        /// true pokud objekt pracuje (stav je: Working, Paused, Cancelling).
        /// </summary>
        public bool IsWorking
        {
            get
            {
                WorkingState s = this._State;
                return (
                    s == WorkingState.Working ||
                    s == WorkingState.Paused ||
                    s == WorkingState.Cancelling);
            }
        }
        /// <summary>
        /// Aktuální stav.
        /// Změna stavu vyvolá událost <see cref="StateChanged"/>.
        /// </summary>
        public virtual WorkingState State
        {
            get { return _State; }
            protected set
            {
                WorkingState stateBefore = _State;
                WorkingState stateCurrent = value;
                if (stateCurrent != stateBefore)
                {
                    _State = stateCurrent;
                    this.OnStateChanged(stateBefore, stateCurrent);
                }
            }
        }
        private WorkingState _State;
        /// <summary>
        /// Provede se po změně stavu
        /// </summary>
        /// <param name="stateBefore"></param>
        /// <param name="stateCurrent"></param>
        protected virtual void OnStateChanged(WorkingState stateBefore, WorkingState stateCurrent)
        {
            if (this.StateChanged != null)
                this.StateChanged(this, new WorkingStateChangedArgs(stateBefore, stateCurrent));
        }
        /// <summary>
        /// Událost volaná po změně stavu celkového procesu downloadu
        /// </summary>
        public event WorkingStateChangedHandler StateChanged;


        #endregion
        #region Thread
        /// <summary>
        /// Nastartuje výkon threadu na pozadí, thread dostane dané jméno. Dojde ke spuštění metody <see cref="RunBackThread()"/> na pozadí. 
        /// Řízení z této metody se vrací ihned. Nastaví se stav <see cref="State"/> = <see cref="WorkingState.Working"/>.
        /// </summary>
        /// <param name="threadName"></param>
        protected void StartBackThread(string threadName = null)
        {
            this.State = WorkingState.Working;
            ThreadManager.AddAction(_RunBackThread);




            /*
            var workThread = _WorkThread;
            if (workThread != null && workThread.ThreadState == ThreadState.Running)
            {
                try { workThread.Abort(); }
                catch (Exception) { }
                this._WorkThread = null;
            }
            string name = (!String.IsNullOrEmpty(threadName) ? threadName : "WorkingThread");
            this._WorkThread = new Thread(this._RunBackgroundLoop) { Name = name, IsBackground = true, Priority = ThreadPriority.BelowNormal };
            this.State = WorkingState.Working;
            this._WorkThread.Start();
            */
        }
        private void _RunBackThread()
        {
            try
            {
                RunBackThread();
                this.State = WorkingState.Done;
            }
            catch (Exception)
            {
                // ignore
                this.State = WorkingState.Aborted;
            }
            finally { }
        }
        /// <summary>
        /// Main rutina spuštěná na pozadí, tuto metodu přepisuje potomek a v ní implementuje svůj výkonný kód
        /// </summary>
        protected virtual void RunBackThread() { }
        #endregion
    }
    #endregion
    #region enum WorkingState, class WorkingStateChangedArgs, delegate WorkingStateChangedHandler
    /// <summary>
    /// Stavy procesu celého downloadu (nikoli jednotlivé soubory)
    /// </summary>
    public enum WorkingState
    {
        /// <summary>
        /// Připraveno
        /// </summary>
        Initiated,
        /// <summary>
        /// Pracuje
        /// </summary>
        Working,
        /// <summary>
        /// Pauza
        /// </summary>
        Paused,
        /// <summary>
        /// Požadavek na Cancel, čekání na dokončení běžících přenosů
        /// </summary>
        Cancelling,
        /// <summary>
        /// Dokončeno po Cancel, neběží žádné přenosy.
        /// </summary>
        Cancelled,
        /// <summary>
        /// Abortováno bez čekání na dokončení přenosů (Cancel i při běžících downloadech).
        /// </summary>
        Aborted,
        /// <summary>
        /// Ukončeno vlivem počtu chyb
        /// </summary>
        StoppedDueErrors,
        /// <summary>
        /// Dokončeno samovolně po dojetí do konce, bez jiného důvodu
        /// </summary>
        Done
    }
    /// <summary>
    /// Argument pro eventhandlery události Změna stavu
    /// </summary>
    public class WorkingStateChangedArgs : EventArgs
    {
        public WorkingStateChangedArgs(WorkingState stateBefore, WorkingState stateCurrent)
        {
            this.StateBefore = stateBefore;
            this.StateCurrent = stateCurrent;
        }
        public WorkingState StateBefore { get; private set; }
        public WorkingState StateCurrent { get; private set; }
    }
    public delegate void WorkingStateChangedHandler(object sender, WorkingStateChangedArgs args);
    #endregion
}
