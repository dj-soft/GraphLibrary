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
            DxComponent.SplashShow("Testovací aplikace Helios Nephrite", "DJ soft & ASOL",
                "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                this, Properties.Resources.Moon10,
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


            //_DxDataFormV2 = new DxDataFormV2() { Dock = System.Windows.Forms.DockStyle.Fill };
            //this.DxMainPanel.Controls.Add(_DxDataFormV2);
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
        #endregion
        #region Ribbon - obsah a rozcestník
        protected override void DxRibbonPrepare()
        {
            _DxShowLog = true;

            // string imageAdd = "svgimages/icon%20builder/actions_addcircled.svg";

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ" };
            pages.Add(page);
            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: true) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            string imageStatusRefresh = "svgimages/xaf/action_refresh.svg";
            string imageDataFormRemove = "svgimages/spreadsheet/removetablerows.svg";
            string imageLogClear = "svgimages/spreadsheet/removetablerows.svg";
            group = new DataRibbonGroup() { GroupText = "ZÁKLADNÍ" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "StatusRefresh", Text = "Refresh Status", ToolTipText = "Znovu načíst údaje o spotřebě systémových zdrojů do statusbaru", Image = imageStatusRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "DataFormRemove", Text = "Remove DataForm", ToolTipText = "Zahodit DataForm a uvolnit jeho zdroje", Image = imageDataFormRemove });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogClear", Text = "Clear Log", ToolTipText = "Smaže obsah logu vpravo", Image = imageLogClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogVisible", Text = "Show Log", ToolTipText = "Zobrazit log v pravé části hlavního okna.\r\nPOZOR: pokud je log stále zobrazený, pak veškeré logované změny jsou zatíženy časem refreshe textu Logu. \r\n Je vhodnější log zavřít, provést testy, a pak log otevřít a přečíst.", RibbonItemType = RibbonItemType.CheckBoxToggle, Checked = _DxShowLog, RibbonStyle = RibbonItemStyles.Large });


            string imageTest = "svgimages/xaf/actiongroup_easytestrecorder.svg";
            group = new DataRibbonGroup() { GroupText = "VZORKY" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "CreateSample1", Text = "Ukázka 1", Image = imageTest, Tag = "Sample1", Enabled = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "CreateSample2", Text = "Ukázka 2", Image = imageTest, Tag = "Sample2", Enabled = true });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample3", Text = "Ukázka 3", Image = imageTest, Tag = "Sample3", Enabled = true });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample4", Text = "Ukázka 4", Image = imageTest, Tag = "Sample4", Enabled = true });


            string imageTestRefresh = "svgimages/spreadsheet/refreshpivottable.svg";
            string imageTestRepaint = "svgimages/dashboards/striped.svg";
            group = new DataRibbonGroup() { GroupText = "TESTY VÝKONU" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Refresh1", Text = "Refresh 1", ToolTipText = "Provede kompletní refresh standardní", Image = imageTestRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "Refresh10", Text = "Refresh 10", ToolTipText = "Provede kompletní refresh pro 10x obsah", Image = imageTestRefresh });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Refresh2000", Text = "Refresh  2000", ToolTipText = "Provede kompletní refresh pro 2000x obsah", Image = imageTestRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "Repaint1", Text = "Repaint 1", ToolTipText = "Provede pouze repaint standardní", Image = imageTestRepaint });
            group.Items.Add(new DataRibbonItem() { ItemId = "Repaint10", Text = "Repaint 10", ToolTipText = "Provede pouze repaint pro 10x obsah", Image = imageTestRepaint });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Repaint2000", Text = "Repaint 2000", ToolTipText = "Provede pouze repaint pro 2000x obsah", Image = imageTestRepaint });

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
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
                    _DxShowLog = (e.Item.Checked ?? false);
                    _DxMainSplit.CollapsePanel = DevExpress.XtraEditors.SplitCollapsePanel.Panel2;
                    _DxMainSplit.Collapsed = !_DxShowLog;
                    _RefreshLog();
                    break;
                case "Refresh1":
                    _TestPerformance(1, true);
                    break;
                case "Refresh10":
                    _TestPerformance(10, true);
                    break;
                case "Refresh100":
                    _TestPerformance(100, true);
                    break;
                case "Refresh2000":
                    _TestPerformance(2000, true);
                    break;
                case "Repaint1":
                    _TestPerformance(1, false);
                    break;
                case "Repaint10":
                    _TestPerformance(10, false);
                    break;
                case "Repaint100":
                    _TestPerformance(100, false);
                    break;
                case "Repaint2000":
                    _TestPerformance(2000, false);
                    break;
                case "CreateSample1":
                    _AddDataFormSample(1);
                    break;
                case "CreateSample2":
                    _AddDataFormSample(2);
                    break;
                default:
                    DxComponent.LogClear();
                    if (e.Item.Tag is DxDataFormTestDefinition sampleData)
                        this._AddDataFormSampleData(sampleData);
                    else if (e.Item.Tag is string sampleName)
                        this._AddDataFormSampleName(sampleName);
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
        private void _AddDataFormSampleData(DxDataFormTestDefinition sampleData)
        {
            _RemoveDataForms();

            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus

            RefreshStatusCurrent();
        }
        private void _TestPerformance(int count, bool forceRefresh)
        {
            if (_DxDataFormV2 == null)
                _AddDataFormSampleName("Sample1");

            int itemsCount = this._DxDataFormV2.VisibleItemsCount;
            int totalCount = count * itemsCount;
            var sampleStartTime = DxComponent.LogTimeCurrent;
            _DxDataFormV2.TestPerformance(count, forceRefresh);
            DxComponent.LogAddLineTime($"TestPerformance: Count: {totalCount}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);
        }
        private void _AddDataFormSample(int sampleId)
        {
            if (!(sampleId == 1 || sampleId == 2)) return;

            string[] texts = Random.GetSentencesArray(1, 3, 120, 240, false);
            string[] tooltips = Random.GetSentencesArray(7, 16, 120, 240, true);

            _RemoveDataForms();

            var sampleStartTime = DxComponent.LogTimeCurrent;
            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus
            _DxShowTimeSpan = null;
            DxDataForm dataForm = new DxDataForm();
            dataForm.TabIndex = 1;
            dataForm.LogActive = true;
            dataForm.GotFocus += DxDataForm_GotFocus;

            _DxDataFormV2 = dataForm;
            _DoLayoutAnyDataForm();
            DxComponent.LogAddLineTime($"Create DxDataFormV2: Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            var addStartTime = DxComponent.LogTimeCurrent;
            switch (sampleId)
            {
                case 1:
                    dataForm.Pages = DxDataFormSamples.CreateSampleData(texts, tooltips, 1, 60);
                    break;
                case 2:
                    dataForm.Pages = DxDataFormSamples.CreateSampleData(texts, tooltips, 2, 2000);
                    break;
            }
            _DxTestPanel.Controls.Add(dataForm);

            _DoLayoutAnyDataForm();
            dataForm.Focus();

            RefreshStatusCurrent();

            int count = dataForm.ItemsCount;
            DxComponent.LogAddLineTime($"AddItems: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            _FocusInButton.Focus();
        }
        private void _AddDataFormSampleName(string sampleName)
        {
            int sampleId = 0;
            if (sampleName != null && sampleName.Length > 6 && sampleName.StartsWith("Sample"))
                Int32.TryParse(sampleName.Substring(6), out sampleId);
            if (sampleId <= 0) return;

            var sampleStartTime = DxComponent.LogTimeCurrent;
            var sampleItems = DxDataFormTest.CreateSample(sampleId);
            if (sampleItems == null) return;
            int count = sampleItems.Count();
            DxComponent.LogAddLineTime($"CreateSample: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            _RemoveDataForms();

            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus
            _DxShowTimeSpan = null;
            DxDataForm dataForm = new DxDataForm();
            string[] texts = Random.GetSentencesArray(1, 3, 120, 240, false);
            string[] tooltips = Random.GetSentencesArray(7, 16, 120, 240, true);
            dataForm.Pages = DxDataFormSamples.CreateSampleData(texts, tooltips, 2, 2000);
            dataForm.GotFocus += DxDataForm_GotFocus;

            _DxDataFormV2 = dataForm;
            _DoLayoutAnyDataForm();

            var addStartTime = DxComponent.LogTimeCurrent;
            // dataForm.AddItems(sampleItems);
            DxComponent.LogAddLineTime($"AddItems: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            _DxTestPanel.Controls.Add(dataForm);

            _DoLayoutAnyDataForm();
            dataForm.Focus();

            RefreshStatusCurrent();
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
                int y = _DxTitleLabel.Bounds.Bottom + 6;
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
        private void _RefreshLog()
        {
            if (_DxShowLog)
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
        private bool _DxShowLog;
        bool _LogContainChanges;
        #endregion
    }
}
