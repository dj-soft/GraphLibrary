using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.SchedulerMap.Analyser
{
    public partial class MainForm : Form
    {
        #region Konstruktor, proměnné, eventy
        public MainForm()
        {
            InitializeComponent();
            InitializeState();

            Data.Testy.Run();
        }
        private void InitializeState()
        {
            this.WindowState = FormWindowState.Maximized;
            _Analyser = null;
            _ProcesInfo = System.Diagnostics.Process.GetCurrentProcess();
            _SelectedFileInit();
            ShowButtonsByState(ActionType.Dialog);
            RefreshStatusBarGui();
            _Timer.Enabled = true;
        }
        private System.Diagnostics.Process _ProcesInfo;
        private Analyser _Analyser;
        private ActionType _CurrentState;
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void _AnalyseStartClick(object sender, EventArgs e)
        {
            AnalyseStart();
        }

        private void _VisualiserButton_Click(object sender, EventArgs e)
        {
            VisualiserStart();
        }
        private void _StopClick(object sender, EventArgs e)
        {
            var analyser = _Analyser;
            if (analyser != null)
            {
                analyser.Cancel = true;
            }
            if (_CurrentState == ActionType.ActiveVisualiser)
                ShowButtonsByState(ActionType.Dialog);

            // _Analyser = null;
        }
        private void _FileButton_Click(object sender, EventArgs e)
        {
            _SelectFile();
        }
        private void _Timer_Tick(object sender, EventArgs e)
        {
            RefreshStatusBar();
        }
        #endregion
        #region Výběr souboru, aktuální soubor, předvolby načítání

        private void _SelectedFileInit()
        {
            string defaultFolder = @"Software\DjSoft\SchedulerMapAnalyser";
            DjSoft.Support.WinReg.SetRootDefaultFolder(defaultFolder);
            SelectedFile = LastSelectedFile;
        }
        private void _SelectFile()
        {
            string selectedFile = SelectedFile;
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Filter = "CSV soubory (*.csv)|*.csv|Všechny soubory (*.*)|*.*";   // Řetězec filtru musí obsahovat popis filtru, svislou čáru (|) a vzorek filtru. Řetězce pro různé volby filtrování rovněž musí být odděleny svislou čarou. Příklad: Textové soubory (*.txt)|*.txt|Všechny soubory (*.*)|*.*'
                dialog.Title = "Vyber soubor k analýze";
                dialog.Multiselect = false;
                dialog.RestoreDirectory = true;
                if (!String.IsNullOrEmpty(selectedFile))
                {
                    dialog.FileName = System.IO.Path.GetFileName(SelectedFile);
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(SelectedFile);
                }

                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    selectedFile = dialog.FileName;
                    SelectedFile = selectedFile;
                    LastSelectedFile = selectedFile;
                }
            }
        }
        /// <summary>
        /// Aktuální vybraný soubor
        /// </summary>
        private string SelectedFile
        {
            get { return _FileText.Text; }
            set { _FileText.Text = value ?? ""; }
        }
        /// <summary>
        /// Aktuálně je zaškrtnuto "Pouze zpracované položky"
        /// </summary>
        private bool OnlyProcessed
        {
            get { return _OnlyProcessedCheck.Checked; }
            set { _OnlyProcessedCheck.Checked = value; }
        }
        /// <summary>
        /// Aktuálně je zaškrtnuto "Vždy zznovu načíst data"
        /// </summary>
        private bool ReloadFile
        {
            get { return _ReloadFileCheck.Checked; }
            set { _ReloadFileCheck.Checked = value; }
        }
        /// <summary>
        /// Soubor v registrech
        /// </summary>
        private string LastSelectedFile
        {
            get { return DjSoft.Support.WinReg.ReadString("", "SelectedFile", DefaultSelectedFile); }
            set { DjSoft.Support.WinReg.WriteString("", "SelectedFile", value); }
        }
        /// <summary>
        /// Výchozí soubor podle jména počítače
        /// </summary>
        private static string DefaultSelectedFile
        {
            get
            {
                var machineName = System.Environment.MachineName;
                if (machineName == "PC-D") return @"D:\Asol\SchedulerMaps\MapAx12702.csv";
                return "";
            }
        }
        #endregion
        #region Načtená data souboru
        private MapSegment ValidMapSegment
        {
            get
            {
                string fileName = SelectedFile;
                bool onlyProcessed = OnlyProcessed;
                if (ReloadFile || _MapSegment is null || !_MapSegment.IsValidFor(fileName, onlyProcessed))
                    _MapSegment = new MapSegment(fileName, onlyProcessed);
                return _MapSegment;
            }
        }
        private MapSegment _MapSegment;
        #endregion
        #region Provádění analýzy
        private void AnalyseStart()
        {
            if (_Analyser != null) return;
            ShowButtonsByState(ActionType.RunningAnalyse);
            Task.Factory.StartNew(Analyse);
        }
        private void Analyse()
        {
            var analyser = _Analyser;
            if (analyser != null) return;

            string file = SelectedFile;

            analyser = new Analyser
            {
                MapSegment = ValidMapSegment,
                ScanByProduct = AnalyseScanByProduct,
                CycleSimulation = AnalyseCycleSimulation,
                ShowInfo = ShowProgress
            };
            _Analyser = analyser;
            analyser.Run();
            // Po doběhnutí anebo po Cancel:
            RefreshStatusBar();
            ShowButtonsByState(ActionType.Dialog);
            analyser.ShowInfo = null;
            _Analyser = null;
        }
        /// <summary>
        /// Analyzovat i vztahy vedlejších produktů?
        /// </summary>
        private bool AnalyseScanByProduct
        {
            get { return _ByProductCheck.Checked; }
            set { _ByProductCheck.Checked = value; }
        }
        /// <summary>
        /// Simulovat daný počet zacyklení
        /// </summary>
        private int AnalyseCycleSimulation
        {
            get { return (int)_SimulCycleText.Value; }
            set { _SimulCycleText.Value = value; }
        }
        #endregion
        #region Vizualizer
        private void VisualiserStart()
        {
            ShowButtonsByState(ActionType.ActiveVisualiser);
            _VisualiserPanel.MapSegment = this.ValidMapSegment;      // Získám validní segment pro aktuální soubor a předvolby, nemusí v něm ale být dosud načtena jeho data
            _VisualiserPanel.ActivateMapItem();
        }
        #endregion
        #region Řízení GUI
        private void ShowButtonsByState(ActionType state)
        {
            this._CurrentState = state;
            if (this.InvokeRequired)
                Invoke(new Action(ShowButtonsEnabledByRunningGui));
            else
                ShowButtonsEnabledByRunningGui();
        }
        private void ShowButtonsEnabledByRunningGui()
        {
            var state = _CurrentState;
            bool isInDialog = (state == ActionType.Dialog);
            bool isInAnalyse = (state == ActionType.RunningAnalyse);
            bool isInVisualiser = (state == ActionType.ActiveVisualiser);
            bool isInAction = (isInAnalyse || isInVisualiser);

            _FileButton.Enabled = isInDialog;
            _FileText.Enabled = isInDialog;
            _OnlyProcessedCheck.Enabled = isInDialog;
            _ReloadFileCheck.Enabled = isInDialog;

            _AnalyseTitleLabel.Visible = isInDialog || isInAnalyse;
            _ByProductCheck.Visible = isInDialog || isInAnalyse;
            _ByProductCheck.Enabled = isInDialog;
            _SimulCycleLabel.Visible = isInDialog || isInAnalyse;
            _SimulCycleText.Visible = isInDialog || isInAnalyse;
            _SimulCycleText.Enabled = isInDialog;
            _AnalyseStartButton.Visible = isInDialog || isInAnalyse;
            _AnalyseStartButton.Enabled = isInDialog;

            _MapTitleLabel.Visible = isInDialog || isInVisualiser;
            _VisualiserButton.Visible = isInDialog || isInVisualiser;
            _VisualiserButton.Enabled = isInDialog;


            bool visibleAnalyser = isInAnalyse;
            bool visibleVisualiser = isInVisualiser;
            if (!visibleAnalyser && !visibleVisualiser)
            {
                visibleAnalyser = _ProgressText.Visible || !_VisualiserPanel.Visible;
            }
            if (visibleAnalyser && _ProgressText.Dock != DockStyle.Fill) _ProgressText.Dock = DockStyle.Fill;
            _ProgressText.Visible = visibleAnalyser;

            if (visibleVisualiser && _VisualiserPanel.Dock != DockStyle.Fill) _VisualiserPanel.Dock = DockStyle.Fill;
            _VisualiserPanel.Visible = visibleVisualiser;

            _StopButton.Visible = isInAction;
            _StopButton.Enabled = isInAction;
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
        private enum ActionType
        {
            Dialog,
            RunningAnalyse,
            ActiveVisualiser
        }
        #endregion
    }
}
