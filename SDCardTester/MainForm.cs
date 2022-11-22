using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.SDCardTester
{
    public partial class MainForm : Form
    {
        #region Konstrukce a nativní eventhandlery
        public MainForm()
        {
            InitializeComponent();
            InitializeProgress();
            InitContent();
            VisualMapPanelInit();
            ShowProperties();
            InitEvents();
            ShowControls(ActionState.Dialog, true);
        }
        private void InitializeProgress()
        {
            __TaskProgress = new TaskProgress(this);
            __TaskProgress.ProgressMaximum = 500;
            /*  Použití je jednoduché:
            var rand = new Random();
            var next = rand.Next(30);
            __TaskProgress.ProgressState = (next < 10 ? ThumbnailProgressState.Normal : next < 20 ? ThumbnailProgressState.Error : ThumbnailProgressState.Paused);
            __TaskProgress.ProgressValue = rand.Next(0, 100);
            */
            this.__AppTitleTextStandard = this.Text;
            this.__AppTitleTextCurrent = null;
        }
        protected override void WndProc(ref Message m)
        {
            __TaskProgress.FormWndProc(ref m);
            base.WndProc(ref m);
        }
        private TaskProgress __TaskProgress;
        private void InitContent()
        {
            this.OnlyRemovableCheck.Checked = true;
            int driveCount = FillDrives();
            if (driveCount == 0)
            {
                this.OnlyRemovableCheck.Checked = false;
                driveCount = FillDrives();
            }
        }
        private void InitEvents()
        {
            this.DriveCombo.SelectedIndexChanged += DriveCombo_SelectedIndexChanged;
            this.OnlyRemovableCheck.CheckedChanged += OnlyRemovableCheck_CheckedChanged;
            this.LinearMapControl.ActiveItemChanged += VisualPanel_ActiveItemChanged;
            this.ClientSizeChanged += _ClientSizeChanged;
            this.DoLayout();
        }

        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this.DoLayout();
        }
        private void DoLayout()
        {
            if (this.UserPanel is null || this.DrivesPanel is null || this.DriveInfoPanel is null || this.ResultsInfoPanel is null || this.CommandsPanel is null || this.RunPauseStopPanel is null) return;

            // Layout se týká pouze levého panelu: this.UserPanel
            // Obsahuje prvky: DrivesPanel, DriveInfoPanel, CommandsPanel, RunPauseStopPanel, ResultsInfoPanel
            var clientSize = this.UserPanel.ClientSize;
            int width = clientSize.Width - 6;
            int height = clientSize.Height - 6;
            int mx = 3;
            int my = 3;
            int x = mx;
            int y = my;
            int bottom = height - my;
            int commandHeight = this.CommandsPanel.Height;
            int commandY = bottom - commandHeight;

            this.DrivesPanel.Bounds = new Rectangle(x, y, width, 86);
            y = this.DrivesPanel.Bounds.Bottom + 3;

            int centerHeight = commandY - y - my;
            this.DriveInfoPanel.Bounds = new Rectangle(x, y, width, centerHeight);
            this.ResultsInfoPanel.Bounds = new Rectangle(x, y, width, centerHeight);

            this.CommandsPanel.Bounds = new Rectangle(x, commandY, width, commandHeight);

            int runPauseStopHeight = this.RunPauseStopPanel.Bounds.Height;
            int runPauseStopY = bottom - runPauseStopHeight;
            this.RunPauseStopPanel.Bounds = new Rectangle(x, runPauseStopY, width, runPauseStopHeight);
        }
        private void VisualPanel_ActiveItemChanged(object sender, EventArgs e)
        {
            switch (CurrentDataPanelState)
            {
                case ActionState.AnalyseContent:
                    AnalyseActiveItemChanged(this.LinearMapControl.ActiveItem);
                    break;
                default:
                    break;
            }
        }
        private void DriveCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowControls(ActionState.Dialog, true);
            ShowProperties();
        }
        private void OnlyRemovableCheck_CheckedChanged(object sender, EventArgs e)
        {
            ShowControls(ActionState.Dialog, true);
            FillDrives();
        }
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            FillDrives();
            //ShowProperties();
            DriveCombo.Focus();
        }
        private void AnalyseContentButton_Click(object sender, EventArgs e)
        {
            RunDriveAnalyse();
        }
        private void TestSaveButton_Click(object sender, EventArgs e)
        {
            RunDriveTestSave();
        }
        private void TestReadButton_Click(object sender, EventArgs e)
        {
            RunDriveTestRead();
            ShowControls(ActionState.TestRead, true);
        }
        private void RunPauseStopPanel_StateChanged(object sender, EventArgs e)
        {
            var state = this.RunPauseStopPanel.State;
            switch (CurrentState)
            {
                case ActionState.AnalyseContent:
                    RunPauseStopDriveAnalyse(state);
                    break;
                case ActionState.TestSave:
                case ActionState.TestRead:
                    RunPauseStopDriveTest(state);
                    break;
                default:
                    ShowControls(ActionState.Dialog, false);
                    break;
            }
        }
        #endregion
        #region Zobrazení controlů v okně podle aktuálního stavu
        /// <summary>
        /// Zobrazí controly vhodné pro daný stav okna.
        /// Lze volat z Working threadů.
        /// </summary>
        /// <param name="state"></param>
        protected void ShowControls(ActionState state, bool withDataPanel)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action<ActionState, bool>(ShowControls), state, withDataPanel);
            else
            {
                bool currentWorking = (CurrentState == ActionState.AnalyseContent || CurrentState == ActionState.TestSave || CurrentState == ActionState.TestRead);
                bool nextWorking = (state == ActionState.AnalyseContent || state == ActionState.TestSave || state == ActionState.TestRead);

                DriveCombo.Enabled = (state == ActionState.Dialog);
                OnlyRemovableCheck.Visible = (state == ActionState.Dialog);
                RefreshButton.Visible = (state == ActionState.Dialog);

                if (withDataPanel)
                {
                    DriveInfoPanel.Visible = (state == ActionState.Dialog);
                    ResultsInfoPanel.Visible = (state == ActionState.AnalyseContent || state == ActionState.TestSave || state == ActionState.TestRead);
                    CurrentDataPanelState = state;
                }

                CommandsPanel.Visible = (state == ActionState.Dialog);
                RunPauseStopPanel.Visible = nextWorking;
                if (!currentWorking && nextWorking)
                    RunPauseStopPanel.State = RunState.Run;

                bool currentIsTest = (CurrentState == ActionState.AnalyseContent || CurrentState == ActionState.TestSave || CurrentState == ActionState.TestRead);
                bool nextIsTest = (state == ActionState.TestSave || state == ActionState.TestRead);
                __TaskProgress.ProgressState = (nextIsTest ? ThumbnailProgressState.Normal : ThumbnailProgressState.NoProgress);   // Tam se hlídá změna interně!
                if (currentIsTest && !nextIsTest)
                    this.AppTitleTextCurrent = this.__AppTitleTextStandard;

                CurrentState = state;
            }
        }
        /// <summary>
        /// Aktuální stav okna
        /// </summary>
        protected ActionState CurrentState;
        /// <summary>
        /// Aktuální stav panelu = který datový panel zůstal být vidět po doběhnutí akce
        /// </summary>
        protected ActionState CurrentDataPanelState;
        /// <summary>
        /// Stav okna podle aktuální akce
        /// </summary>
        protected enum ActionState
        {
            Dialog,
            AnalyseContent,
            TestSave,
            TestRead
        }
        /// <summary>
        /// Smaže prvky <see cref="WorkingResultControl"/> z panelu informací <see cref="ResultsInfoPanel"/>
        /// </summary>
        private void ResultsInfoPanelClear()
        {
            for (int i = ResultsInfoPanel.Controls.Count - 1; i >= 0; i--)
            {
                var control = ResultsInfoPanel.Controls[i];
                if (control is WorkingResultControl)
                {
                    ResultsInfoPanel.Controls.RemoveAt(i);
                    control.Dispose();
                }
            }
        }
        #endregion
        #region Načtení a zobrazení seznamu Drives a detailních vlastností o zvoleném disku, včetně mapy
        /// <summary>
        /// Aktuálně vybraný disk
        /// </summary>
        public System.IO.DriveInfo SelectedDrive
        {
            get
            {
                TextDataInfo selectedItem = this.DriveCombo.SelectedItem as TextDataInfo;
                return selectedItem?.Data as System.IO.DriveInfo;
            }
            set
            {
                string name = value?.Name;
                object selectedItem = null;
                foreach (var item in this.DriveCombo.Items)
                {
                    if (item is TextDataInfo info && info.Data is System.IO.DriveInfo driveInfo && String.Equals(driveInfo.Name, name))
                    {
                        selectedItem = item;
                        break;
                    }
                }
                this.DriveCombo.SelectedItem = selectedItem;
            }
        }
        /// <summary>
        /// Naplní seznam Drives
        /// </summary>
        /// <returns></returns>
        private int FillDrives()
        {
            int driveCount = 0;
            bool onlyRemovable = this.OnlyRemovableCheck.Checked;
            decimal oneKB = 1024m;
            decimal oneMB = oneKB * oneKB;
            decimal oneGB = oneKB * oneKB * oneKB;
            string selectedName = SelectedDrive?.Name;
            TextDataInfo selectedItem = null;

            this.DriveCombo.Items.Clear();
            var drives = System.IO.DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (!drive.IsReady) continue;
                if (onlyRemovable  && drive.DriveType!= System.IO.DriveType.Removable) continue;

                string name = drive.Name;
                string label = drive.VolumeLabel;
                string total = Math.Round((decimal)drive.TotalSize / oneGB, 1).ToString("### ##0.0") + " GB";
                string text = $"{name}    '{label}'    [{total}]";
                TextDataInfo info = new TextDataInfo() { Text = text, Data = drive };
                this.DriveCombo.Items.Add(info);
                if (selectedItem is null || String.Equals(drive.Name, selectedName))
                    selectedItem = info;
                driveCount++;
            }
            this.DriveCombo.SelectedItem = selectedItem;
            return driveCount;
        }
        /// <summary>
        /// Načte a zobrazí vlastnosti aktuálně vybraného <see cref="SelectedDrive"/>, provede aktuální načtení dat
        /// </summary>
        private void ShowProperties()
        {
            var selectedDrive = SelectedDrive;
            if (selectedDrive != null) selectedDrive = new System.IO.DriveInfo(selectedDrive.Name);     // = refresh

            this.DriveInfoPanel.ShowProperties(selectedDrive);
            VisualMapPanelFillBasicData(selectedDrive);
        }
        /// <summary>
        /// Inicializace vizuálního controlu
        /// </summary>
        private void VisualMapPanelInit()
        {
            Skin.Palette = Skin.PaletteType.Light;
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> načte a vepíše základní informace o daném disku <paramref name="drive"/> (velikost, obsazenost, testovací data).
        /// </summary>
        /// <param name="drive"></param>
        private void VisualMapPanelFillBasicData(System.IO.DriveInfo drive)
        {
            var fileGroups = DriveAnalyser.GetFileGroupsForDrive(drive, false, out long totalSize);
            VisualMapPanelFillData(fileGroups, totalSize);
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky popisující stav obsazení disku podle dodaných <see cref="DriveAnalyser.FileGroup"/>.
        /// </summary>
        /// <param name="fileGroups"></param>
        private void VisualMapPanelFillData(IEnumerable<DriveAnalyser.FileGroup> fileGroups, long? totalSize = null)
        {
            var items = new List<LinearMapControl.Item>();
            if (fileGroups != null)
                items.AddRange(fileGroups.Select(g => new LinearMapControl.Item(g.SizeTotal, g.Color, null, g)));

            if (totalSize.HasValue)
                VisualMapPanelSetupHeight(totalSize.Value, false);

            this.LinearMapControl.Items = items;
            this.LinearMapControl.Refresh();
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky popisující stav obsazení disku podle dodaných <see cref="DriveAnalyser.FileGroup"/>.
        /// </summary>
        /// <param name="fileGroups"></param>
        private void VisualMapPanelSetupHeight(long totalSize, bool refresh)
        {
            this.LinearMapControl.LineHeight = GetLineHeight(totalSize);
            this.LinearMapControl.TotalLength = totalSize;

            if (refresh)
                this.LinearMapControl.Refresh();
        }
        /// <summary>
        /// Vrátí výšku jedné vizuální linky pro danou velikost disku: menší disk = vyšší linky, velký disk = malé linky (víc se tam toho vejde)
        /// </summary>
        /// <param name="totalSize"></param>
        /// <returns></returns>
        protected int GetLineHeight(long totalSize)
        {
            long sizeMB = totalSize / 1048576L;
            long sizeGB = sizeMB / 1024L;
            long sizeTB = sizeGB / 1024L;
            if (sizeMB <= 256L) return 24;                 // Obstarožní média
            if (sizeMB <= 512L) return 22;
            if (sizeMB <= 1024L) return 21;
            if (sizeGB <= 2L) return 20;                   // SD karty
            if (sizeGB <= 4L) return 19;
            if (sizeGB <= 8L) return 18;
            if (sizeGB <= 16L) return 17;
            if (sizeGB <= 32L) return 16;
            if (sizeGB <= 64L) return 15;
            if (sizeGB <= 128L) return 14;                 // SSD disky
            if (sizeGB <= 256L) return 13;
            if (sizeGB <= 512L) return 12;
            if (sizeGB <= 1024L) return 11;
            if (sizeTB <= 2L) return 10;                   // Velkoplotnové disky
            if (sizeTB <= 4L) return 9;
            if (sizeTB <= 8L) return 8;
            if (sizeTB <= 16L) return 7;
            if (sizeTB <= 32L) return 6;
            return 5;
        }
        #endregion
        #region Analýza stavu disku
        /// <summary>
        /// Požadavek na start analýzy
        /// </summary>
        private void RunDriveAnalyse()
        {
            ResultsInfoPanelClear();
            ShowControls(ActionState.AnalyseContent, true);

            DriveAnalyser driveAnalyser = new DriveAnalyser();
            AnalyseInfoPanelPrepare(driveAnalyser);
            driveAnalyser.WorkingStep += DriveAnalyser_AnalyseStep;
            driveAnalyser.WorkingDone += DriveAnalyser_AnalyseDone;
            _DriveAnalyser = driveAnalyser;
            driveAnalyser.Start(this.SelectedDrive);
        }
        /// <summary>
        /// Požadavek na zastavení analýzy
        /// </summary>
        private void RunPauseStopDriveAnalyse(RunState state)
        {
            if (_DriveAnalyser != null)
                _DriveAnalyser?.ChangeState(state);
            else
                ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Analyzer provedl krok analýzy a volá refresh dat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DriveAnalyser_AnalyseStep(object sender, EventArgs e)
        {
            DriveAnalyserRefresh();
        }
        /// <summary>
        /// Analyzer skončil svoji činnost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DriveAnalyser_AnalyseDone(object sender, EventArgs e)
        {
            DriveAnalyserRefresh();
            _DriveAnalyser = null;
            // grupy si ponechám:   _DriveAnalyserGroups = null;
            ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Zajistí refresh dat po jednom každém postupném kroku analyzeru
        /// </summary>
        private void DriveAnalyserRefresh()
        {
            if (_DriveAnalyser is null) return;

            if (this.InvokeRequired)
                this.BeginInvoke(new Action(DriveAnalyserRefresh));
            else
            {
                DriveAnalyser driveAnalyser = _DriveAnalyser;
                if (driveAnalyser != null)
                {
                    ResultsInfoPanelFillData(driveAnalyser);
                    VisualMapPanelFillData(driveAnalyser);
                }
            }
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> vygeneruje prvky <see cref="DriveAnalyseGroupControl"/> pro zobrazování dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void AnalyseInfoPanelPrepare(DriveAnalyser driveAnalyser)
        {
            var analyserGroups = new List<Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>>();
            var groups = driveAnalyser.FileGroups;
            int x = 3;
            int y = 3;
            int w = ResultsInfoPanel.ClientRectangle.Width - 6;
            int h = DriveAnalyseGroupControl.OptimalHeight;
            foreach (var group in groups)
            {
                var panel = new DriveAnalyseGroupControl(group.Name, group.Color);
                panel.Bounds = new Rectangle(x, y, w, h);
                ResultsInfoPanel.Controls.Add(panel);
                analyserGroups.Add(new Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>(group, panel));
                y += panel.Height;
            }
            _DriveAnalyserGroups = analyserGroups;
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> aktualizuje hodnoty v controlech <see cref="DriveAnalyseGroupControl"/> z dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void ResultsInfoPanelFillData(DriveAnalyser driveAnalyser)
        {
            var analyserGroups = _DriveAnalyserGroups;
            if (analyserGroups is null || analyserGroups.Count == 0) return;
            long totalSize = analyserGroups.Sum(g => g.Item1.SizeTotal);
            foreach (var analyserGroup in analyserGroups)
            {
                long currentSize = analyserGroup.Item1.SizeTotal;
                analyserGroup.Item2.GroupRatio = (totalSize > 0L ? (double)currentSize / (double)totalSize : 0d);
                analyserGroup.Item2.Refresh();
            }
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky pocházející ze skupin z analyzeru z <paramref name="driveAnalyser"/> : <see cref="DriveAnalyser.FileGroups"/>
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void VisualMapPanelFillData(DriveAnalyser driveAnalyser)
        {
            VisualMapPanelFillData(driveAnalyser.FileGroups);
        }
        /// <summary>
        /// Volá se po aktivaci daného (nebo žádného) prvku - pohybem myší nad mapou.
        /// Vstupní prvek je <see cref="LinearMapControl.Item"/> = prvek, který byl vložen do vizuální mapy. 
        /// Tento prvek byl vytvořen v metodě <see cref="VisualMapPanelFillData(DriveAnalyser)"/>, 
        /// a ve své property <see cref="LinearMapControl.Item.Data"/> by měl obsahovat grupu z analyzeru <see cref="DriveAnalyser.FileGroup"/>.
        /// Tutéž grupu najdeme v panelu <see cref="ResultsInfoPanel"/> = tam se zobrazují jednotlivé skupiny souborů a jejich sumární hodnota, a tam bych rád zvýraznil grupu, která je aktivní v mapě.
        /// Grupu najdu v prvku v poli <see cref="_DriveAnalyserGroups"/>, kde je umístěna v Item1. 
        /// Tam pak jako párovou Item2 najdu instanci vizuálního controlu <see cref="DriveAnalyseGroupControl"/>.
        /// </summary>
        /// <param name="activeItem"></param>
        private void AnalyseActiveItemChanged(LinearMapControl.Item activeItem)
        {
            var activeGroup = activeItem?.Data as DriveAnalyser.FileGroup;
            var lastActivePanel = _DriveAnalyserGroups?.FirstOrDefault(t => t.Item2.IsActive)?.Item2;
            var currActivePanel = _DriveAnalyserGroups?.FirstOrDefault(t => Object.ReferenceEquals(t.Item1, activeGroup))?.Item2;
            
            if (lastActivePanel != null && (currActivePanel is null || (!Object.ReferenceEquals(lastActivePanel, currActivePanel))))
            {
                lastActivePanel.IsActive = false;
                lastActivePanel.Refresh();
                lastActivePanel = null;
            }

            if (currActivePanel != null && (currActivePanel is null || (!Object.ReferenceEquals(currActivePanel, lastActivePanel))))
            {
                currActivePanel.IsActive = true;
                currActivePanel.Refresh();
            }
        }
        /// <summary>
        /// Instance analyzeru, po doběhnutí se nuluje
        /// </summary>
        private DriveAnalyser _DriveAnalyser;
        /// <summary>
        /// Instance analyzovaných skupin, nenuluje se po doběhnutí
        /// </summary>
        private List<Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>> _DriveAnalyserGroups;
        #endregion
        #region Vepsání testovacích dat do volného prostoru daného disku a jejich čtení a ověření
        /// <summary>
        /// Požadavek na start zápisu dat na disk
        /// </summary>
        private void RunDriveTestSave() { RunDriveTest(ActionState.TestSave); }
        /// <summary>
        /// Požadavek na start čtení dat z disku
        /// </summary>
        private void RunDriveTestRead() { RunDriveTest(ActionState.TestRead); }
        /// <summary>
        /// Požadavek na start zápisu / čtení dat z disku
        /// </summary>
        private void RunDriveTest(ActionState action)
        {
            bool doSave = (action == ActionState.TestSave);
            bool doRead = (action == ActionState.TestRead);
            if (!(doSave || doRead)) return;

            ResultsInfoPanelClear();
            ShowControls(action, true);

            DriveTester driveTester = new DriveTester();
            TestInfoPanelPrepare(driveTester);
            driveTester.WorkingStep += DriveTester_TestStep;
            driveTester.WorkingDone += DriveTester_TestDone;
            _DriveTester = driveTester;
            driveTester.Start(this.SelectedDrive, doSave, doRead);
        }
        /// <summary>
        /// Požadavek na zastavení testu
        /// </summary>
        private void RunPauseStopDriveTest(RunState state)
        {
            if (_DriveTester != null)
                _DriveTester?.ChangeState(state);
            else
                ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Tester provedl krok testu a volá refresh dat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DriveTester_TestStep(object sender, EventArgs e)
        {
            DriveTesterRefresh();
        }
        /// <summary>
        /// Tester skončil svoji činnost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DriveTester_TestDone(object sender, EventArgs e)
        {
            DriveTesterRefresh();
            _DriveTester = null;
            ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Zajistí refresh dat po jednom každém postupném kroku testeru
        /// </summary>
        private void DriveTesterRefresh()
        {
            if (_DriveTester is null) return;

            if (this.InvokeRequired)
                this.BeginInvoke(new Action(DriveTesterRefresh));
            else
            {
                var driveTester = _DriveTester;
                if (driveTester != null)
                {
                    ResultsInfoPanelFillData(driveTester);
                    VisualPanelFillData(driveTester);
                }
            }
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> vygeneruje prvky <see cref="DriveTestTimePhaseControl"/> pro zobrazování dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void TestInfoPanelPrepare(DriveTester driveTester)
        {
            var testPhases = new Dictionary<DriveTester.TestPhase, DriveTestTimePhaseControl>();

            int x = 3;
            int y = 3;
            int w = ResultsInfoPanel.ClientRectangle.Width - 6;
            int h = DriveTestTimePhaseControl.OptimalHeight;

            addPhase(DriveTester.TestPhase.SaveShortFile);
            addPhase(DriveTester.TestPhase.ReadShortFile);
            addPhase(DriveTester.TestPhase.SaveLongFile);
            addPhase(DriveTester.TestPhase.ReadLongFile);

            _DriveTesterPhases = testPhases;

            // Zařadí control pro danou fázi
            void addPhase(DriveTester.TestPhase phase)
            {
                DriveTestTimePhaseControl panel = new DriveTestTimePhaseControl();
                panel.Bounds = new Rectangle(x, y, w, h);
                ResultsInfoPanel.Controls.Add(panel);
                testPhases.Add(phase, panel);
                y += panel.Height;
            }
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> aktualizuje hodnoty v controlech <see cref="DriveAnalyseGroupControl"/> z dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void ResultsInfoPanelFillData(DriveTester driveTester)
        {
            var testPhases = _DriveTesterPhases;
            if (testPhases != null && driveTester != null)
            {
                var testPhase = driveTester.CurrentTestPhase;
                testPhases[DriveTester.TestPhase.SaveShortFile].StoreInfo(driveTester.TimeInfoSaveShort, testPhase);
                testPhases[DriveTester.TestPhase.SaveLongFile].StoreInfo(driveTester.TimeInfoSaveLong, testPhase);
                testPhases[DriveTester.TestPhase.ReadShortFile].StoreInfo(driveTester.TimeInfoReadShort, testPhase);
                testPhases[DriveTester.TestPhase.ReadLongFile].StoreInfo(driveTester.TimeInfoReadLong, testPhase);

                decimal progressRatio = driveTester.ProgressRatio;

                int value = (int)Math.Round(progressRatio * (decimal)__TaskProgress.ProgressMaximum, 0);
                if (value == 0 && driveTester.ProgressRatio > 0m) value = 1;
                __TaskProgress.ProgressValue = value;

                bool hasError = (driveTester.TimeInfoSaveShort.ErrorBytes > 0 || driveTester.TimeInfoSaveLong.ErrorBytes > 0 || driveTester.TimeInfoReadShort.ErrorBytes > 0 || driveTester.TimeInfoReadLong.ErrorBytes > 0);
                __TaskProgress.ProgressState = (!hasError ? ThumbnailProgressState.Normal : ThumbnailProgressState.Error);

                // Titulek aplikace = "SD Card tester H: 58%"
                string drive = driveTester.Drive.Name.Substring(0, 1).ToUpper();
                string appName = (testPhase == DriveTester.TestPhase.SaveShortFile || testPhase == DriveTester.TestPhase.SaveLongFile ? "SD Card Write" :
                                (testPhase == DriveTester.TestPhase.ReadShortFile || testPhase == DriveTester.TestPhase.ReadLongFile ? "SD Card Read" : ""));
                string percent = ((int)Math.Round(100m * progressRatio, 0)).ToString();
                string title = $"{appName} {drive}: {percent}%";

                this.AppTitleTextCurrent = title;
            }
        }
        /// <summary>
        /// Titulek okna bez suffixu = titulek aplikace
        /// </summary>
        private string __AppTitleTextStandard;
        /// <summary>
        /// Titulek okna aktuální, napojený na aktuální Text = fyzický titulek okna.
        /// Pokud bude opakovaně setována stejná hodnota, detekuje se to podle fieldu <see cref="__AppTitleTextCurrent"/> a nebude se do property Text nic vkládat.
        /// </summary>
        private string AppTitleTextCurrent
        {
            get { return __AppTitleTextCurrent; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {   // Pouze neprázdný string:
                    if (!String.Equals(value, __AppTitleTextCurrent))
                    {   // Pouze změněný string:
                        __AppTitleTextCurrent = value;
                        this.Text = value;
                    }
                }
            }
        }
        private string __AppTitleTextCurrent;
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky pocházející ze skupin z testeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void VisualPanelFillData(DriveTester driveTester)
        {
            var fileGroups = driveTester.FileGroups;
            var totalSize = driveTester.TotalSize;
            VisualMapPanelFillData(fileGroups, totalSize);
        }
        /// <summary>
        /// Instance testeru
        /// </summary>
        private DriveTester _DriveTester;
        private Dictionary<DriveTester.TestPhase, DriveTestTimePhaseControl> _DriveTesterPhases;
        #endregion
    }
}
