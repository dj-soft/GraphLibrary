using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SchedulerMapAnalyser
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            InitializeState();
        }
        private void InitializeState()
        {
            this.WindowState = FormWindowState.Maximized;
            _Analyser = null;
            _ProcesInfo = System.Diagnostics.Process.GetCurrentProcess();
            ShowButtonsEnabledByRunning(false);
            RefreshStatusBarGui();
            _Timer.Enabled = true;
        }
        private System.Diagnostics.Process _ProcesInfo;
        private Analyser _Analyser;
        private bool _Running;
        private void _StartClick(object sender, EventArgs e)
        {
            if (_Analyser != null) return;

            ShowButtonsEnabledByRunning(true);
            Task.Factory.StartNew(Analyse);
        }
        private void _StopClick(object sender, EventArgs e)
        {
            var analyser = _Analyser;
            if (analyser != null)
            {
                analyser.Cancel = true;
            }
            // _Analyser = null;
        }
        private void _Timer_Tick(object sender, EventArgs e)
        {
            RefreshStatusBar();
        }
        private void Analyse()
        {
            var analyser = _Analyser;
            if (analyser != null) return;

            string file = @"D:\Asol\SchedulerMaps\MapAx12702.csv";
            analyser = new Analyser
            {
                File = file,
                OnlyProcessedItems = _OnlyProcessedCheck.Checked,
                CycleSimulation = (int)_SimulCycleText.Value,
                ShowInfo = ShowProgress
            };
            _Analyser = analyser;
            analyser.Run();
            // Po doběhnutí anebo po Cancel:
            RefreshStatusBar();
            ShowButtonsEnabledByRunning(false);
            analyser.ShowInfo = null;
            _Analyser = null;
        }
        private void ShowButtonsEnabledByRunning(bool running)
        {
            this._Running = running;
            if (this.InvokeRequired)
                Invoke(new Action(ShowButtonsEnabledByRunningGui));
            else
                ShowButtonsEnabledByRunningGui();
        }
        private void ShowButtonsEnabledByRunningGui()
        {
            bool running = _Running;
            _RunButton.Enabled = !running;
            _StopButton.Enabled = running;
        }
        private void ShowProgress(string info)
        {
            if (this.InvokeRequired)
                Invoke(new Action<string>(ShowProgressGui), info);
            else
                ShowProgressGui(info);
        }
        private void ShowProgressGui(string info)
        {
            this._ProgressText.Text = info;

            // this._ProgressText.Refresh();

            int length = info.Length;
            int end = length - 3;
            int max = end - 250;
            if (max < 0) max = 0;
            int selStart = end;
            for (int i = end; i >= max; i--)
            {
                selStart = i;
                char c = info[i];
                if (c == '\r' || c == '\n')
                    break;
            }

            int selLength = length - selStart;
            this._ProgressText.Select(selStart, selLength);
            this._ProgressText.ScrollToCaret();
        }
        private void RefreshStatusBar()
        {
            if (this.InvokeRequired)
                Invoke(new Action(RefreshStatusBarGui));
            else
                RefreshStatusBarGui();
        }
        private void RefreshStatusBarGui()
        {
            _ProcesInfo.Refresh();
            string processText = "Private Memory: " + (_ProcesInfo.PrivateMemorySize64 / 1024L).ToString("### ### ##0").Trim() + " KB";
            this._StatusProcessText.Text = processText;

            string analyserText = _Analyser?.StatusInfo ?? "";
            this._StatusAnalyserText.Text = analyserText;
        }
    }
}
