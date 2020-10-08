using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GUI = Noris.LCS.Base.WorkScheduler;
using WS = Asol.Tools.WorkScheduler.Scheduler;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    [IsMainForm("Testy FreeForm komponent", MainFormMode.AutoRun, 50)]
    public partial class DataForm : Form
    {
        public DataForm()
        {
            InitializeApplication();
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;

            using (App.Trace.Scope("DataForm", "InitializeComponent", ""))
                InitializeComponent();

            using (App.Trace.Scope("DataForm", "InitializeDataForm", ""))
                InitializeDataForm();
        }
        protected void InitializeApplication()
        {
            App.AppCompanyName = "Asseco Solutions";
            App.AppProductName = "WorkScheduler";
            App.AppProductTitle = "Dílenské plánování";
            App.Trace.Info("DataForm", "InitializeApplication", "", "First row");
        }
        protected void InitializeDataForm()
        {
            Stopwatch = new System.Diagnostics.Stopwatch();
            StopwatchFrequency = (decimal)System.Diagnostics.Stopwatch.Frequency;
            CurrentLibrary = DataFormtestLibraryType.Asol;
        }
        #region Nastavení = typ knihovny + počet objektů
        /// <summary>
        /// Zvolená knihovna
        /// </summary>
        public DataFormtestLibraryType CurrentLibrary
        {
            get { return _CurrentLibrary; }
            set
            {
                bool isChange = false;
                if (!_CurrentLibraryChanging)
                {
                    _CurrentLibraryChanging = true;
                    this.radioButtonWinForm.Checked = (value == DataFormtestLibraryType.WinForms);
                    this.radioButtonDevExpr.Checked = (value == DataFormtestLibraryType.DevExpress);
                    this.radioButtonInfrag.Checked = (value == DataFormtestLibraryType.Infragistic);
                    this.radioButtonAsol.Checked = (value == DataFormtestLibraryType.Asol);
                    isChange = (_CurrentLibrary != value);
                    _CurrentLibrary = value;
                    _CurrentLibraryChanging = false;
                }
                if (isChange)
                {
                    _TestObjectRemove();
                    if (TestControlsNumber <= 0 || TestControlsNumber > CurrentMaximumNumber)
                        TestControlsNumber = CurrentOptimumNumber;
                }
                ShowColor();
            }
        }
        private int CurrentMaximumNumber
        {
            get
            {
                switch (_CurrentLibrary)
                {
                    case DataFormtestLibraryType.WinForms: return 3000;
                    case DataFormtestLibraryType.DevExpress: return 3000;
                    case DataFormtestLibraryType.Infragistic: return 500;
                    case DataFormtestLibraryType.Asol: return 20000;
                }
                return 1000;
            }
        }
        private int CurrentOptimumNumber
        {
            get
            {
                switch (_CurrentLibrary)
                {
                    case DataFormtestLibraryType.WinForms: return 1000;
                    case DataFormtestLibraryType.DevExpress: return 1000;
                    case DataFormtestLibraryType.Infragistic: return 140;
                    case DataFormtestLibraryType.Asol: return 8000;
                }
                return 1000;
            }
        }
        private DataFormtestLibraryType _CurrentLibrary = DataFormtestLibraryType.None;
        private bool _CurrentLibraryChanging = false;
        private void _RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (Object.ReferenceEquals(sender, this.radioButtonWinForm)) CurrentLibrary = DataFormtestLibraryType.WinForms;
            else if (Object.ReferenceEquals(sender, this.radioButtonDevExpr)) CurrentLibrary = DataFormtestLibraryType.DevExpress;
            else if (Object.ReferenceEquals(sender, this.radioButtonInfrag)) CurrentLibrary = DataFormtestLibraryType.Infragistic;
            else if (Object.ReferenceEquals(sender, this.radioButtonAsol)) CurrentLibrary = DataFormtestLibraryType.Asol;
            _XlsContainTitle = false;
        }
        /// <summary>
        /// Počet vygenerovaných prvků, platná hodnota = 1 až <see cref="TestControlsNumberMax"/> (=20000)
        /// </summary>
        public int TestControlsNumber
        {
            get { return _TestControlsNumber; }
            set
            {
                bool isChange = false;
                if (!_TestControlsNumberChanging)
                {
                    _TestControlsNumberChanging = true;
                    int number = (value < 1 ? 1 : (value > TestControlsNumberMax ? TestControlsNumberMax : value));
                    isChange = (_TestControlsNumber != number);
                    this.trackBarNumber.Value = _GetTrackBarNumber_ValueFromNumber(number);
                    _TestControlsNumber = number;
                    this.labelValue.Text = number.ToString();
                    _TestControlsNumberChanging = false;
                }
                if (isChange) _TestObjectRemove();
                ShowColor();
            }
        }
        private int _TestControlsNumber;
        private bool _TestControlsNumberChanging = false;
        /// <summary>
        /// Maximální počet prvků
        /// </summary>
        public const int TestControlsNumberMax = 20000;
        private void trackBarNumber_Scroll(object sender, EventArgs e)
        {
            TestControlsNumber = _GetTrackBarNumber_NumberFromValue(this.trackBarNumber.Value);
        }
        /// <summary>
        /// Vrátí hodnotu do Trackbaru pro daný počet prvků
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private int _GetTrackBarNumber_ValueFromNumber(int number)
        {
            decimal value = 0m;
            decimal numb = (decimal)(number < 1 ? 1 : (number > TestControlsNumberMax ? TestControlsNumberMax : number));
            if (numb < 10m) value = numb;
            else if (numb < 100m) value = 10m + (numb / 10m);
            else if (numb < 1000m) value = 20m + (numb / 100m);
            else if (numb < 10000m) value = 30m + (numb / 1000m);
            else if (numb < 100000m) value = 40m + (numb / 10000m);
            else value = 42m;
            decimal ratio = (value - 1m) / 41m;
            int min = this.trackBarNumber.Minimum;
            int max = this.trackBarNumber.Maximum;
            int trackValue = (int)Math.Round((min + ratio * (max - min)), 0);
            return trackValue;
        }
        /// <summary>
        /// Vrátí cílový počet objektů (<see cref="TestControlsNumber"/>) z hodnoty na trackbaru
        /// </summary>
        /// <param name="trackValue"></param>
        /// <returns></returns>
        private int _GetTrackBarNumber_NumberFromValue(int trackValue)
        {
            // Převedeme hodnotu value na lineární rozsah int 1-42 (42 je odpověď na zásadní otázku života, vesmíru a vůbec):
            // 1-10  10-100  100-1000  1000-10000  10000-20000
            int min = this.trackBarNumber.Minimum;
            int max = this.trackBarNumber.Maximum;
            decimal ratio = (decimal)(trackValue - min) / (decimal)(max - min);     // Do rozsahu 0 až 1, včetně
            ratio = (ratio < 0m ? 0m : (ratio > 1m ? 1m : ratio));
            decimal value = 1m + Math.Round((41m * ratio), 1);
            decimal number = 0;
            if (value < 10m) number = value;                                    // value = 4    =>   number                =     4
            else if (value < 20m) number = 10m * (value - 9m);                   // value = 16   =>   number = (10 * 6)     =    60
            else if (value < 30m) number = 100m * (value - 19m);                  // value = 23   =>   number = (100 * 3)    =   300
            else if (value < 40m) number = 1000m * (value - 29m);                 // value = 38   =>   number = (1000 * 8)   =  8000
            else if (value < 50m) number = 10000m * (value - 39m);                // value = 42   =>   number = (10000 * 2)  = 20000
            else number = TestControlsNumberMax;
            return (int)Math.Round(number, 0);
        }
        /// <summary>
        /// Zobrazí barvu toolbaru podle počtu a knihovny
        /// </summary>
        private void ShowColor()
        {
            Color backColor = System.Drawing.SystemColors.ControlDark;
            int number = _TestControlsNumber;
            if (number > CurrentMaximumNumber) backColor = Color.FromArgb(192, 32, 32);
            else if (number > CurrentOptimumNumber) backColor = Color.FromArgb(160, 160, 32);
            ToolStripPanel.BackColor = backColor;
        }
        #endregion
        #region Generátor objektů
        private void RunButton_Click(object sender, EventArgs e)
        {
            CreateTestObject(CurrentLibrary, TestControlsNumber);
        }
        private void CreateTestObject(DataFormtestLibraryType library, int number)
        {
            List<Tuple<string, string>> times = new List<Tuple<string, string>>();
            StopwatchStart();
            _TestObjectRemove();
            AddTime(times, "Odstranění stávajícího");
            times.Add(new Tuple<string, string>("Knihovna", library.ToString()));
            times.Add(new Tuple<string, string>("Počet prvků", number.ToString()));

            long memoryBefore = GC.GetTotalMemory(true);
            StopwatchGetLastMiliseconds();                           // Čas spotřebovaný na GC.GetTotalMemory(true) mě nezajímá: nulujeme mezičas a zahodíme jej

            switch (library)
            {
                case DataFormtestLibraryType.WinForms:
                    CreateTestObjectWinForm(number, times);
                    break;
                case DataFormtestLibraryType.DevExpress:
                    CreateTestObjectDevExpress(number, times);
                    break;
                case DataFormtestLibraryType.Infragistic:
                    CreateTestObjectInfragistic(number, times);
                    break;
                case DataFormtestLibraryType.Asol:
                    CreateTestObjectAsol(number, times);
                    break;
            }

            long memoryAfter = GC.GetTotalMemory(false);
            decimal memoryUsed = memoryAfter - memoryBefore;
            string megabytes = (memoryUsed / 1048576m).ToString("### ##0.00").Trim().Replace(".", ",");
            times.Add(new Tuple<string, string>("Nárůst paměti [MB]", megabytes));

            ShowTime(times);
        }
        private void CreateTestObjectWinForm(int number, List<Tuple<string, string>> times)
        {
            var dataFormCtrl = new Asol.Tools.WorkScheduler.TestGUI.TestComponents.WinFormDataFormControl();
            dataFormCtrl.Dock = DockStyle.Fill;
            AddTime(times, "Vytvoření WINFORM controlu");

            this.TestContentPanel.Controls.Add(dataFormCtrl);
            _TestObject = dataFormCtrl;
            AddTime(times, "Přidání do Panelu");

            dataFormCtrl.AddDataFormItems(10, number / 10);
            AddTime(times, "Vygenerování prvků");

            dataFormCtrl.Refresh();
            AddTime(times, "Vykreslení");
        }
        private void CreateTestObjectDevExpress(int number, List<Tuple<string, string>> times)
        {
            Control dataFormCtrl = Asol.Tools.WorkScheduler.DevExpressTest.DxManager.CreateWinFormDataFormControl();
            dataFormCtrl.Dock = DockStyle.Fill;
            AddTime(times, "Vytvoření DEVEXPRESS controlu");

            this.TestContentPanel.Controls.Add(dataFormCtrl);
            _TestObject = dataFormCtrl;
            AddTime(times, "Přidání do Panelu");

            Asol.Tools.WorkScheduler.DevExpressTest.DxManager.AddDataFormItems(dataFormCtrl, 10, number / 10);
            AddTime(times, "Vygenerování prvků");

            dataFormCtrl.Refresh();
            AddTime(times, "Vykreslení");
        }
        private void CreateTestObjectInfragistic(int number, List<Tuple<string, string>> times)
        {
            Control dataFormCtrl = Asol.Tools.WorkScheduler.DevExpressTest.IfManager.CreateWinFormDataFormControl();
            dataFormCtrl.Dock = DockStyle.Fill;
            AddTime(times, "Vytvoření INFRAGISTIC controlu");
          
            this.TestContentPanel.Controls.Add(dataFormCtrl);
            _TestObject = dataFormCtrl;
            AddTime(times, "Přidání do Panelu");

            Asol.Tools.WorkScheduler.DevExpressTest.IfManager.AddDataFormItems(dataFormCtrl, 10, number / 10);
            AddTime(times, "Vygenerování prvků");

            dataFormCtrl.Refresh();
            AddTime(times, "Vykreslení");
        }
        private void CreateTestObjectAsol(int number, List<Tuple<string, string>> times)
        {
            var dataFormCtrl = new Asol.Tools.WorkScheduler.DataForm.GDataFormControl();
            dataFormCtrl.Dock = DockStyle.Fill;
            AddTime(times, "Vytvoření ASOL controlu");
           
            this.TestContentPanel.Controls.Add(dataFormCtrl);
            _TestObject = dataFormCtrl;
            AddTime(times, "Přidání do Panelu");

            dataFormCtrl.AddDataFormItems(10, number / 10);
            AddTime(times, "Vygenerování prvků");

            dataFormCtrl.Refresh();
            AddTime(times, "Vykreslení");
        }
        private void AddTime(List<Tuple<string, string>> times, string text)
        {
            times.Add(new Tuple<string, string>(text + " [ms]", StopwatchGetLastMiliseconds()));
        }

        private void ShowTime(List<Tuple<string, string>> times)
        {
            string statusText = "";
            string space = "     ";
            string xlsTitle = "";
            string xlsData = "";
            string tab = "\t";
            string eol = Environment.NewLine;
            foreach (var time in times)
            {
                statusText += time.Item1 + ": " + time.Item2 + space;
                xlsTitle += time.Item1 + tab;
                xlsData += time.Item2 + tab;
            }

            StatusStripLabel1.Text = statusText;

            string xlsText = (_XlsContainTitle ? "" : xlsTitle + eol) + xlsData;
            Clipboard.SetText(xlsText);
            _XlsContainTitle = true;
        }
        private bool _XlsContainTitle = false;

        private const string _MSSPACE = " ms;" + _SPACE;
        private const string _SPACE = "       ";
        /// <summary>
        /// Zruší a zahodí testovací objekt 
        /// </summary>
        private void _TestObjectRemove()
        {
            StatusStripLabel1.Text = "";
            this.TestContentPanel.Controls.Clear();
            if (_TestObject != null)
            {
                _TestObject.Dispose();
                _TestObject = null;
            }
            GC.Collect();
            System.Threading.Thread.Sleep(50);
            GC.Collect();
        }
        private Control _TestObject;
        #endregion
        #region Časomíra
        /// <summary>
        /// Nastartuje časomíru = čas 0
        /// </summary>
        private void StopwatchStart()
        {
            Stopwatch.Restart();
            StopwatchRunTime = Stopwatch.ElapsedTicks;
            StopwatchLastTime = StopwatchRunTime;
        }
        /// <summary>
        /// Vrátí mezičas = počet milisekund (jako string bez jednotek) od posledního volání této metody.
        /// Uloží si čas aktuálního volání pro příští volání.
        /// </summary>
        /// <returns></returns>
        private string StopwatchGetLastMiliseconds()
        {
            long currTicks = Stopwatch.ElapsedTicks;
            string result = GetMiliseconds(StopwatchLastTime, currTicks);
            StopwatchLastTime = currTicks;
            return result;
        }
        /// <summary>
        /// Vrátí celkový čas = počet milisekund (jako string bez jednotek) od startu časomíry do volání této metody.
        /// </summary>
        /// <returns></returns>
        private string StopwatchGetTotalMiliseconds()
        {
            long currTicks = Stopwatch.ElapsedTicks;
            string result = GetMiliseconds(StopwatchRunTime, currTicks);
            return result;
        }
        /// <summary>
        /// Vrátí počet milisekund do start do stop tick, jako string, bez jednotek
        /// </summary>
        /// <param name="startTick"></param>
        /// <param name="stopTick"></param>
        /// <returns></returns>
        private string GetMiliseconds(long startTick, long stopTick)
        {
            decimal ticks = (decimal)(stopTick - startTick);
            decimal milisecs = Math.Round((1000m * ticks / StopwatchFrequency), 3);
            string text = milisecs.ToString("### ### ##0.000").Trim().Replace(".", ",");
            return text;
        }
        private System.Diagnostics.Stopwatch Stopwatch;
        private long StopwatchRunTime;
        private long StopwatchLastTime;
        private decimal StopwatchFrequency;
        #endregion
    }
    public enum DataFormtestLibraryType
    {
        None = 0,
        WinForms,
        DevExpress,
        Infragistic,
        Asol
    }
}
