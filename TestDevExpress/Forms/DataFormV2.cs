using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormX"/>
    /// </summary>
    public class DataFormV2 : DxRibbonForm
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormV2()
        {
            var moon10 = DxComponent.CreateBitmapImage("Images/Moon10.png");
            DxComponent.SplashShow("Testovací aplikace Helios Nephrite", "DJ soft & ASOL",
                "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                this, moon10,
                useFadeOut: false);

            this.InitializeForm();

            DxComponent.SplashUpdate(rightFooter: "Už to jede...");
        }
        protected override void OnShown(EventArgs e)
        {
            DxComponent.SplashHide();
            base.OnShown(e);
            WinProcessReadAfterShown();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxComponent.LogTextChanged -= DxComponent_LogTextChanged;
        }

        protected void InitializeForm()
        {
            this.Size = new Size(800, 600);

            this.Text = $"Test DataForm V2 :: {DxComponent.FrameworkName}";

            _DxMainSplit = DxComponent.CreateDxSplitContainer(this.DxMainPanel, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, splitPosition: 300, showSplitGlyph: true);

            _DxTestPanel = DxComponent.CreateDxPanel(_DxMainSplit.Panel1, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
            _DxTestPanel.SizeChanged += _DxMainPanel_SizeChanged;
            _FocusInButton = DxComponent.CreateDxSimpleButton(5, 8, 140, 25, _DxTestPanel, " Button před...", tabStop: true);
            _FocusInButton.TabIndex = 0;
            _DxTitleLabel = DxComponent.CreateDxLabel(250, 10, 500, _DxTestPanel, "Zde bude DataForm", styleType: LabelStyleType.SubTitle);
            _FocusOutButton = DxComponent.CreateDxSimpleButton(500, 8, 140, 25, _DxTestPanel, "... Button za.", tabStop: true);
            _FocusOutButton.TabIndex = 2;

            _DxLogMemoEdit = DxComponent.CreateDxMemoEdit(_DxMainSplit.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);

            WinProcessInfoAfterInit = DxComponent.WinProcessInfo.GetCurent();

            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DxComponent.LogTextChanged += DxComponent_LogTextChanged;
            _LogContainChanges = true;

            _ShowLog();
        }
        private void _DxMainPanel_SizeChanged(object sender, EventArgs e)
        {
            _DoLayoutAnyDataForm();
        }
        private DxSplitContainerControl _DxMainSplit;
        private DxLabelControl _DxTitleLabel;
        private DxSimpleButton _FocusInButton;
        private DxSimpleButton _FocusOutButton;

        private DxPanelControl _DxTestPanel;
        private DxMemoEdit _DxLogMemoEdit;
        /// <summary>
        /// Deklarace tlačítka v <see cref="MainAppForm"/> pro spuštění tohoto formuláře
        /// </summary>
        public static RunFormInfo RunFormInfo { get { return new RunFormInfo() { ButtonText = "DataForm", ButtonImage = "svgimages/spreadsheet/showtabularformpivottable.svg", GroupText = "Testovací okna", ButtonOrder = 10 } ; } }
        #endregion
        #region Ribbon - obsah a rozcestník
        protected override void DxRibbonPrepare()
        {
            _IsLogVisible = false;

            // string imageAdd = "svgimages/icon%20builder/actions_addcircled.svg";

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.Default);
            pages.Add(page);

            string imageStatusRefresh = "svgimages/xaf/action_refresh.svg";
            string imageDataFormRemove = "svgimages/spreadsheet/removetablerows.svg";
            string imageLogClear = "svgimages/spreadsheet/removetablerows.svg";
            group = new DataRibbonGroup() { GroupText = "ZÁKLADNÍ" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "StatusRefresh", Text = "Refresh Status", ToolTipText = "Znovu načíst údaje o spotřebě systémových zdrojů do statusbaru", ImageName = imageStatusRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "DataFormRemove", Text = "Remove DataForm", ToolTipText = "Zahodit DataForm a uvolnit jeho zdroje", ImageName = imageDataFormRemove });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogClear", Text = "Clear Log", ToolTipText = "Smaže obsah logu vpravo", ImageName = imageLogClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogVisible", Text = "Show Log", ToolTipText = "Zobrazit log v pravé části hlavního okna.\r\nPOZOR: pokud je log stále zobrazený, pak veškeré logované změny jsou zatíženy časem refreshe textu Logu. \r\n Je vhodnější log zavřít, provést testy, a pak log otevřít a přečíst.", ItemType = RibbonItemType.CheckButton, Checked = _IsLogVisible, RibbonStyle = RibbonItemStyles.Large, ImageName = "devav/layout/datapanel.svg" });


            var groupSamples = new DataRibbonGroup() { GroupText = "VZORKY" };
            string radioGroupName = "SamplesGroup";
            page.Groups.Add(groupSamples);
            string imageTest1 = "svgimages/xaf/actiongroup_easytestrecorder.svg";
            string imageTest2 = "svgimages/spreadsheet/showoutlineformpivottable.svg";
            string imageTest3 = "svgimages/spreadsheet/showtabularformpivottable.svg";
            addSampleButton(10, "Mřížka 1", imageTest1);
            addSampleButton(20, "Mřížka 2", imageTest1);
            addSampleButton(30, "Mřížka 3", imageTest1);
            addSampleButton(40, "Mřížka 4", imageTest1);

            addSampleButton(101, "Design 1", imageTest2, true);
            addSampleButton(102, "Design 2", imageTest2);
            addSampleButton(103, "Design 3", imageTest2);
            addSampleButton(104, "Design 4", imageTest2);

            addSampleButton(201, "Controly 1", imageTest3, true);
            addSampleButton(202, "Controly 2", imageTest3);
            addSampleButton(203, "Controly 3", imageTest3);
            addSampleButton(204, "Controly 4", imageTest3);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;

            void addSampleButton(int sampleId, string text, string imageName, bool isFirstInGroup = false)
            {
                groupSamples.Items.Add(new DataRibbonItem() 
                {
                    ItemId = "CreateSample" + sampleId.ToString(),
                    Text = text,
                    ImageName = imageName,
                    ItemType = RibbonItemType.CheckButton,
                    RadioButtonGroupName = radioGroupName,
                    ItemIsFirstInGroup = isFirstInGroup
                });
            }
        }
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            var itemId = e.Item.ItemId;
            int sampleId = 0;
            if (itemId.StartsWith("CreateSample") && Int32.TryParse(itemId.Substring(12), out sampleId) && sampleId > 0)
                itemId = "CreateSample";

            switch (itemId)
            {
                case "StatusRefresh":
                    GCCollect();
                    RefreshStatusCurrent();
                    break;
                case "DataFormRemove":
                    _RemoveDataForms();
                    break;
                case "LogClear":
                    DxComponent.LogClear();
                    break;
                case "LogVisible":
                    _IsLogVisible = (e.Item.Checked ?? false);
                    _ShowLog();
                    break;
                case "CreateSample":
                    _AddDataFormSample(sampleId);
                    break;
            }
        }
        #endregion
        #region Status - proměnné, Zobrazení spotřeby paměti
        /// <summary>
        /// Tvorba StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this._StatusItemTitle = CreateStatusBarItem();
            this._StatusItemBefore = CreateStatusBarItem();
            this._StatusItemDeltaConstructor = CreateStatusBarItem();
            this._StatusItemDeltaShow = CreateStatusBarItem();
            this._StatusItemDeltaCurrent = CreateStatusBarItem(2);
            this._StatusItemDeltaForm = CreateStatusBarItem();
            this._StatusItemCurrent = CreateStatusBarItem(1);
            this._StatusItemTime = CreateStatusBarItem(2);

            // V tomto pořadí budou viditelné:
            //   netřeba : this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemTitle);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaCurrent);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemTime);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemCurrent);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemBefore);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaForm);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaConstructor);
            this.DxStatusBar.ItemLinks.Add(this._StatusItemDeltaShow);

            this.DxStatusBar.Visible = true;
        }
        private DevExpress.XtraBars.BarStaticItem CreateStatusBarItem(int? fontSizeDelta = null)
        {
            DevExpress.XtraBars.BarStaticItem item = new DevExpress.XtraBars.BarStaticItem();
            item.MinWidth = 240;
            item.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            if (fontSizeDelta.HasValue)
                item.Appearance.FontSizeDelta = fontSizeDelta.Value;
            item.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            return item;
        }
        private void WinProcessReadAfterShown()
        {
            if (WinProcessInfoAfterShown == null)
                WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();
            RefreshStatus();
        }
        public DxComponent.WinProcessInfo WinProcessInfoBeforeForm { get; set; }
        public DxComponent.WinProcessInfo WinProcessInfoAfterInit { get; set; }
        public DxComponent.WinProcessInfo WinProcessInfoAfterShown { get; set; }
        private void RefreshStatus()
        {
            string eol = Environment.NewLine;
            this._StatusItemTitle.Caption = ""; // "<b>DataForm tester</b>";

            var before = WinProcessInfoBeforeForm;
            this._StatusItemBefore.Caption = "   Stav před: <b>" + (before?.Text2 ?? "") + "<b>";
            this._StatusItemBefore.Hint = "Obsazená paměť před zahájením otevírání okna:" + eol + (before?.Text4Full ?? "");

            var deltaInit = WinProcessInfoAfterInit - WinProcessInfoBeforeForm;
            this._StatusItemDeltaConstructor.Caption = "   Delta Init: <b>" + (deltaInit?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaConstructor.Hint = "Spotřeba paměti v rámci konstruktoru a inicializaci:" + eol + (deltaInit?.Text4Full ?? "");

            var deltaShow = WinProcessInfoAfterShown - WinProcessInfoAfterInit;
            this._StatusItemDeltaShow.Caption = "   Delta Show: <b>" + (deltaShow?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaShow.Hint = "Spotřeba paměti od dokončení inicializace do konce provádění Show:" + eol + (deltaShow?.Text4Full ?? "");
            RefreshStatusCurrent();
        }
        private void RefreshStatusCurrent()
        {
            string eol = Environment.NewLine;
            var current = DxComponent.WinProcessInfo.GetCurent();

            var deltaCurr = current - WinProcessInfoAfterShown;
            this._StatusItemDeltaCurrent.Caption = "   Delta Current: <b>" + (deltaCurr?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaCurrent.Hint = "Spotřeba paměti DxDataForm (od prázdného zobrazení do aktuálního stavu):" + eol + (deltaCurr?.Text4Full ?? "");

            var deltaForm = current - WinProcessInfoBeforeForm;
            this._StatusItemDeltaForm.Caption = "   Delta Form: <b>" + (deltaForm?.Text2 ?? "") + "<b>";
            this._StatusItemDeltaForm.Hint = "Spotřeba celého aktuálního formuláře (od vytvoření do aktuálního stavu):" + eol + (deltaForm?.Text4Full ?? "");

            this._StatusItemCurrent.Caption = "   Total Current: <b>" + (current?.Text2 ?? "") + "<b>";
            this._StatusItemCurrent.Hint = "Spotřeba paměti aktuálně:" + eol + (current?.Text4Full ?? "");

            var time = _DxShowTimeSpan;
            bool hasTime = time.HasValue;
            this._StatusItemTime.Caption = hasTime ? "   Time: <b>" + time.Value.TotalMilliseconds.ToString("### ##0").Trim() + " ms<b>" : "";
            this._StatusItemTime.Hint = "Čas zobrazení DataFormu od začátku konstruktoru po GotFocus";
        }

        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        private DevExpress.XtraBars.BarStaticItem _StatusItemBefore;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaConstructor;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaShow;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaCurrent;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaForm;
        private DevExpress.XtraBars.BarStaticItem _StatusItemTime;
        private DevExpress.XtraBars.BarStaticItem _StatusItemCurrent;
        #endregion
        #region DxDataFormV2
       
        private void _AddDataFormSample(int sampleId)
        {
            if (!TestDevExpress.Components.DxDataFormSamples.AllowedSampled(sampleId)) return;

            string[] texts = Randomizer.GetSentencesArray(1, 3, 160, 320, false);
            string[] tooltips = Randomizer.GetSentencesArray(7, 16, 300, 500, true);

            _RemoveDataForms();

            long startTime = DxComponent.LogTimeCurrent;
            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus
            DxDataForm dataForm = _DxDataFormV2;
            if (dataForm == null)
            {
                startTime = DxComponent.LogTimeCurrent;
                _DxShowTimeSpan = null;
                dataForm = new DxDataForm();
                dataForm.TabIndex = 1;
                dataForm.LogActive = true;
                dataForm.GotFocus += DxDataForm_GotFocus;

                _DxDataFormV2 = dataForm;
                _DoLayoutAnyDataForm();
                DxComponent.LogAddLineTime($"Create DxDataFormV2: Time: {DxComponent.LogTokenTimeMilisec}", startTime);
            }

            startTime = DxComponent.LogTimeCurrent;
            dataForm.IForm = TestDevExpress.Components.DxDataFormSamples.CreateSampleDefinition(sampleId, texts, tooltips);
            int rowCount = (sampleId == 40 ? 200 : 1);
            dataForm.Data.Source = this.CreateDataSource(rowCount, dataForm.IForm);
            _DxTestPanel.Controls.Add(dataForm);

            _DoLayoutAnyDataForm();
            dataForm.Focus();

            RefreshStatusCurrent();

            int count = 0; // dataForm.ItemsCount;
            DxComponent.LogAddLineTime($"AddItems: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);

            _FocusInButton.Focus();
        }
        private void _RemoveDataForms()
        {
            var dataForm = _DxDataFormV2;
            if (dataForm != null)
            {
                if (_DxTestPanel.Controls.Contains(dataForm))
                    _DxTestPanel.Controls.Remove(dataForm);
                dataForm.GotFocus -= DxDataForm_GotFocus;
                dataForm.Dispose();

            }
            _DxDataFormV2 = null;
            _DxShowTimeStart = null;
            _DxShowTimeSpan = null;

            GCCollect();
            WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();

            RefreshStatusCurrent();
        }
        private void _DoLayoutAnyDataForm()
        {
            var dataForm = _DxDataFormV2;
            if (dataForm != null)
            {
                var clientSize = _DxTestPanel.ClientSize;
                int y = _FocusInButton.Bounds.Bottom + 6;
                dataForm.Bounds = new System.Drawing.Rectangle(6, y, clientSize.Width - 12, clientSize.Height - y - 6);
            }
        }
        private void DxDataForm_GotFocus(object sender, EventArgs e)
        {
            if (!_DxShowTimeSpan.HasValue && _DxShowTimeStart.HasValue)
            {
                _DxShowTimeSpan = DateTime.Now - _DxShowTimeStart.Value;
                RefreshStatusCurrent();
            }
        }

        private DxDataForm _DxDataFormV2;
        private DateTime? _DxShowTimeStart;
        private TimeSpan? _DxShowTimeSpan;
        #endregion
        #region DataSource
        /// <summary>
        /// Metoda vytvoří datovou tabulku s daty pro daný layout
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="form"></param>
        /// <returns></returns>
        private object CreateDataSource(int rowCount, Noris.Clients.Win.Components.AsolDX.DataForm.IDataForm form)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            var items = form.Pages
                        .SelectMany(p => p.Groups)
                        .SelectMany(g => g.Items)
                        .OfType<Noris.Clients.Win.Components.AsolDX.DataForm.DataFormColumn>()
                        .ToArray();
            int c = 0;
            foreach (var item in items)
            {
                c++;
                string columnName = "column_" + c.ToString();
                dataTable.Columns.Add(columnName, typeof(string));
                item.ColumnId = columnName;
            }

            int columnCount = items.Length;
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                object[] cells = new object[columnCount];
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                    cells[columnIndex] = Randomizer.GetSentence(1, 4);
                dataTable.Rows.Add(cells);
            }

            return dataTable;
        }
        #endregion
        #region Log
        private void DxComponent_LogTextChanged(object sender, EventArgs e)
        {
            // _RefreshLog();
            _LogContainChanges = true;
        }
        protected override void OnApplicationIdle()
        {
            if (_LogContainChanges)
                _RefreshLog();
        }
        private void GCCollect()
        {
            GC.Collect(0, GCCollectionMode.Forced);
        }
        private void _ShowLog()
        {
            _ShowLog(_IsLogVisible);
        }
        private void _ShowLog(bool isLogVisible)
        {
            _IsLogVisible = isLogVisible;
            if (_DxMainSplit != null)
            {
                _DxMainSplit.CollapsePanel = DevExpress.XtraEditors.SplitCollapsePanel.Panel2;
                _DxMainSplit.Collapsed = !isLogVisible;
                if (isLogVisible)
                    _RefreshLog();
            }
        }
        private void _RefreshLog()
        {
            if (_IsLogVisible)
            {
                var logText = DxComponent.LogText;
                if (logText != null)
                {
                    _DxLogMemoEdit.Text = logText;
                    _DxLogMemoEdit.SelectionStart = logText.Length;
                    _DxLogMemoEdit.SelectionLength = 0;
                    _DxLogMemoEdit.ScrollToCaret();
                }
            }
            RefreshStatusCurrent();
            _LogContainChanges = false;
        }
        private bool _IsLogVisible;
        bool _LogContainChanges;
        #endregion
    }
}
