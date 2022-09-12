using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SDCardTester
{
    public partial class MainForm : Form
    {
        #region Konstrukce a nativní eventhandlery
        public MainForm()
        {
            InitializeComponent();
            InitContent();
            ShowProperties();
            InitEvents();
            ShowControls(ActionState.Dialog, true);
        }
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
            this.VisualMapPanel.ActiveItemChanged += VisualPanel_ActiveItemChanged;
        }

        private void VisualPanel_ActiveItemChanged(object sender, EventArgs e)
        {
            switch (CurrentDataPanelState)
            {
                case ActionState.AnalyseContent:
                    AnalyseActiveItemChanged(this.VisualMapPanel.ActiveItem);
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
        private void StopButton_Click(object sender, EventArgs e)
        {
            switch (CurrentState)
            {
                case ActionState.AnalyseContent:
                    StopDriveAnalyse();
                    break;
                case ActionState.TestSave:
                case ActionState.TestRead:
                    StopDriveTest();
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
                DriveCombo.Enabled = (state == ActionState.Dialog);
                OnlyRemovableCheck.Visible = (state == ActionState.Dialog);
                RefreshButton.Visible = (state == ActionState.Dialog);

                if (withDataPanel)
                {
                    PropertiesPanel.Visible = (state == ActionState.Dialog);
                    ResultsInfoPanel.Visible = (state == ActionState.AnalyseContent);
                    CurrentDataPanelState = state;
                }

                CommandsPanel.Visible = (state == ActionState.Dialog);
                StopPanel.Visible = (state == ActionState.AnalyseContent || state == ActionState.TestSave || state == ActionState.TestRead);
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
        /// Smaže prvky <see cref="DriveResultControl"/> z panelu informací <see cref="ResultsInfoPanel"/>
        /// </summary>
        private void ResultsInfoPanelClear()
        {
            for (int i = ResultsInfoPanel.Controls.Count - 1; i >= 0; i--)
            {
                var control = ResultsInfoPanel.Controls[i];
                if (control is DriveResultControl)
                {
                    ResultsInfoPanel.Controls.RemoveAt(i);
                    control.Dispose();
                }
            }
        }
        #endregion
        #region Načtení a zobrazení seznamu Drives a detailních vlastností
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

            this.DriveNameText.Text = selectedDrive?.Name ?? "";
            this.DriveTypeText.Text = selectedDrive?.DriveType.ToString() ?? "";
            this.DriveVolumeText.Text = selectedDrive?.VolumeLabel ?? "";
            this.DriveCapacityText.Text = getCapacityText(selectedDrive?.TotalSize);
            this.DriveFreeText.Text = getCapacityText(selectedDrive?.TotalFreeSpace);
            this.DriveAvailableText.Text = getCapacityText(selectedDrive?.AvailableFreeSpace);

            this.VisualMapPanel.TotalLength = selectedDrive?.TotalSize ?? 0L;
            this.VisualMapPanel.Items.Clear();
            if (selectedDrive != null)
            {
                long totalSize = selectedDrive.TotalSize;
                if (totalSize < 0L) totalSize = 0L;
                long usedSize = totalSize - selectedDrive.TotalFreeSpace;
                if (usedSize < 0L) usedSize = 0L;
                long otherSize = selectedDrive.TotalFreeSpace - selectedDrive.AvailableFreeSpace;
                if (otherSize < 0L) otherSize = 0L;
                long freeSize = selectedDrive.TotalSize - usedSize - otherSize;
                if (freeSize < 0L) freeSize = 0L;

                this.VisualMapPanel.LineHeight = GetLineHeight(totalSize);
                this.VisualMapPanel.TotalLength = totalSize;
                this.VisualMapPanel.Items.Add(new TestControl.Item(otherSize, Skin.OtherSpaceColor));
                this.VisualMapPanel.Items.Add(new TestControl.Item(usedSize, Skin.UsedSpaceColor));
                this.VisualMapPanel.Items.Add(new TestControl.Item(freeSize, Skin.FreeSpaceColor));
            }
            this.VisualMapPanel.Refresh();

            string getCapacityText(long? capacity)
            {
                if (!capacity.HasValue) return "";
                return capacity.Value.ToString("### ### ### ### ##0");
            }
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
            driveAnalyser.AnalyseStep += DriveAnalyser_AnalyseStep;
            driveAnalyser.AnalyseDone += DriveAnalyser_AnalyseDone;
            _DriveAnalyser = driveAnalyser;
            driveAnalyser.BeginAnalyse(this.SelectedDrive);
        }
        /// <summary>
        /// Požadavek na zastavení analýzy
        /// </summary>
        private void StopDriveAnalyse()
        {
            if (_DriveAnalyser != null)
                _DriveAnalyser?.StopAnalyse();
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
                    VisualPanelFillData(driveAnalyser);
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
                y += panel.Height;
                analyserGroups.Add(new Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>(group, panel));
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
            long totalSize = analyserGroups.Sum(g => g.Item1.TotalLength);
            foreach (var analyserGroup in analyserGroups)
            {
                long currentSize = analyserGroup.Item1.TotalLength;
                analyserGroup.Item2.GroupRatio = (totalSize > 0L ? (double)currentSize / (double)totalSize : 0d);
                analyserGroup.Item2.Refresh();
            }
        }
        /// <summary>
        /// Do mapy <see cref="VisualMapPanel"/> vloží prvky pocházející ze skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void VisualPanelFillData(DriveAnalyser driveAnalyser)
        { 
            var items = new List<TestControl.Item>();
            var groups = driveAnalyser.FileGroups;
            if (groups != null)
                items.AddRange(groups.Select(g => new TestControl.Item(g.TotalLength, g.Color, null, g)));

            this.VisualMapPanel.Items = items;
            this.VisualMapPanel.Refresh();
        }
        /// <summary>
        /// Volá se po aktivaci daného (nebo žádného) prvku - pohybem myší nad mapou.
        /// Vstupní prvek je <see cref="TestControl.Item"/> = prvek, který byl vložen do vizuální mapy. 
        /// Tento prvek byl vytvořen v metodě <see cref="VisualPanelFillData(DriveAnalyser)"/>, 
        /// a ve své property <see cref="TestControl.Item.Data"/> by měl obsahovat grupu z analyzeru <see cref="DriveAnalyser.FileGroup"/>.
        /// Tutéž grupu najdeme v panelu <see cref="ResultsInfoPanel"/> = tam se zobrazují jednotlivé skupiny souborů a jejich sumární hodnota, a tam bych rád zvýraznil grupu, která je aktivní v mapě.
        /// Grupu najdu v prvku v poli <see cref="_DriveAnalyserGroups"/>, kde je umístěna v Item1. 
        /// Tam pak jako párovou Item2 najdu instanci vizuálního controlu <see cref="DriveAnalyseGroupControl"/>.
        /// </summary>
        /// <param name="activeItem"></param>
        private void AnalyseActiveItemChanged(TestControl.Item activeItem)
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
            driveTester.TestStep += DriveTester_TestStep;
            driveTester.TestDone += DriveTester_TestDone;
            _DriveTester = driveTester;
            driveTester.BeginTest(this.SelectedDrive, doSave, doRead);
        }
        /// <summary>
        /// Požadavek na zastavení testu
        /// </summary>
        private void StopDriveTest()
        {
            if (_DriveTester != null)
                _DriveTester?.StopTest();
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
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> vygeneruje prvky <see cref="DriveTestPhaseControl"/> pro zobrazování dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void TestInfoPanelPrepare(DriveTester driveTester)
        {
        

        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> aktualizuje hodnoty v controlech <see cref="DriveAnalyseGroupControl"/> z dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void ResultsInfoPanelFillData(DriveTester driveTester)
        {
            //var analyserGroups = _DriveAnalyserGroups;
            //if (analyserGroups is null || analyserGroups.Count == 0) return;
            //long totalSize = analyserGroups.Sum(g => g.Item1.TotalLength);
            //foreach (var analyserGroup in analyserGroups)
            //{
            //    long currentSize = analyserGroup.Item1.TotalLength;
            //    analyserGroup.Item2.GroupRatio = (totalSize > 0L ? (double)currentSize / (double)totalSize : 0d);
            //    analyserGroup.Item2.Refresh();
            //}
        }
        /// <summary>
        /// Do mapy <see cref="VisualMapPanel"/> vloží prvky pocházející ze skupin z testeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void VisualPanelFillData(DriveTester driveTester)
        {
            //var items = new List<TestControl.Item>();
            //var groups = driveAnalyser.FileGroups;
            //if (groups != null)
            //    items.AddRange(groups.Select(g => new TestControl.Item(g.TotalLength, g.Color, null, g)));

            //this.VisualMapPanel.Items = items;
            //this.VisualMapPanel.Refresh();
        }
        /// <summary>
        /// Instance testeru
        /// </summary>
        private DriveTester _DriveTester;
        #endregion
    }
    public class DriveResultControl : Control
    { }
    public class TextDataInfo
    {
        public override string ToString()
        {
            return this.Text;
        }
        public string Text { get; set; }
        public object Data { get; set; }
    }
}
