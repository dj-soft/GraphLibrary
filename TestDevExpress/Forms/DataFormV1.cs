using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormV1"/>
    /// </summary>
    public class DataFormV1 : DxRibbonForm
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormV1()
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
            this.Size = new System.Drawing.Size(800, 600);

            this.Text = $"Test DataForm V1 :: {DxComponent.FrameworkName}";

            _DxMainSplit = DxComponent.CreateDxSplitContainer(this.DxMainPanel, dock: System.Windows.Forms.DockStyle.Fill, splitLineOrientation: System.Windows.Forms.Orientation.Vertical,
                fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, splitPosition: 300, showSplitGlyph: true);

            _DxTestPanel = DxComponent.CreateDxPanel(_DxMainSplit.Panel1, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
            _DxTestPanel.SizeChanged += _DxMainPanel_SizeChanged;
            _DxTitleLabel = DxComponent.CreateDxLabel(10, 10, 500, _DxTestPanel, "Zde bude DataForm", styleType: LabelStyleType.SubTitle);

            _DxLogMemoEdit = DxComponent.CreateDxMemoEdit(_DxMainSplit.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);

            WinProcessInfoAfterInit = DxComponent.WinProcessInfo.GetCurent();

            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DxComponent.LogTextChanged += DxComponent_LogTextChanged;
            _LogContainChanges = true;
        }
        
        private void _DxMainPanel_SizeChanged(object sender, EventArgs e)
        {
            _DoLayoutAnyDataForm();
        }

        private DxSplitContainerControl _DxMainSplit;
        private DxLabelControl _DxTitleLabel;
        private DxPanelControl _DxTestPanel;
        private DxMemoEdit _DxLogMemoEdit;
        #endregion
        #region Ribbon - obsah a rozcestník
        /// <summary>
        /// Tvorba Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            string imageStatusRefresh = "svgimages/xaf/action_refresh.svg";
            string imageDataFormRemove = "svgimages/spreadsheet/removetablerows.svg";
            string imageLogClear = "svgimages/spreadsheet/removetablerows.svg";
            string imageAdd = "svgimages/icon%20builder/actions_addcircled.svg";
            string imageTest = "svgimages/xaf/actiongroup_easytestrecorder.svg";

            _DxDataFormMemoryOptimized = true;
            _DxShowLog = true;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ" };
            pages.Add(page);
            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: true) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            group = new DataRibbonGroup() { GroupText = "ZÁKLADNÍ" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "StatusRefresh", Text = "Refresh Status", ToolTipText = "Znovu načíst údaje o spotřebě systémových zdrojů do statusbaru", Image = imageStatusRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "DataFormRemove", Text = "Remove DataForm", ToolTipText = "Zahodit DataForm a uvolnit jeho zdroje", Image = imageDataFormRemove });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogClear", Text = "Clear Log", ToolTipText = "Smaže obsah logu vpravo", Image = imageLogClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogVisible", Text = "Show Log", ToolTipText = "Zobrazit log v pravé části hlavního okna.\r\nPOZOR: pokud je log stále zobrazený, pak veškeré logované změny jsou zatíženy časem refreshe textu Logu. \r\n Je vhodnější log zavřít, provést testy, a pak log otevřít a přečíst.", RibbonItemType = RibbonItemType.CheckBoxToggle, Checked = _DxShowLog, RibbonStyle = RibbonItemStyles.Large });

            group = new DataRibbonGroup() { GroupText = "VZORKY" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample1", Text = "Ukázka 1", Image = imageTest, Tag = "Sample1", Enabled = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample2", Text = "Ukázka 2", Image = imageTest, Tag = "Sample2", Enabled = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample3", Text = "Ukázka 3", Image = imageTest, Tag = "Sample3", Enabled = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample4", Text = "Ukázka 4", Image = imageTest, Tag = "Sample4", Enabled = true });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample5", Text = "Ukázka 5", Image = imageTest, Tag = "Sample5", Enabled = false });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Sample.Sample6", Text = "Ukázka 6", Image = imageTest, Tag = "Sample6", Enabled = false });

            //group = new DataRibbonGroup() { GroupId = "params", GroupText = "PARAMETRY" };
            //page.Groups.Add(group);
            //group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Params.MemoryOptimized", Text = "MemoryOptimized", ToolTipText = "Zaškrtnuto: používat optimalizaci paměti / Ne: bez optimalizací (může dojít k systémové chybě)", RibbonItemType = RibbonItemType.CheckBoxToggle, Checked = true, RibbonStyle = RibbonItemStyles.SmallWithText });
            //group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Params.UseWinForm", Text = "Použít WinForms", ToolTipText = "Nezaškrtnuté = DevExpress;\r\nZaškrtnuté = WinForm", RibbonItemType = RibbonItemType.CheckBoxToggle, Checked = false, RibbonStyle = RibbonItemStyles.SmallWithText });
            //group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Params.ShowLog", Text = "Zobrazit LOG", ToolTipText = "Zobrazit log v pravé části hlavního okna.\r\nPOZOR: pokud je log stále zobrazený, pak veškeré logované změny jsou zatíženy časem refreshe textu Logu. \r\n Je vhodnější log zavřít, provést testy, a pak log otevřít a přečíst.", RibbonItemType = RibbonItemType.CheckBoxToggle, Checked = true, RibbonStyle = RibbonItemStyles.SmallWithText });

            group = new DataRibbonGroup() { GroupId = "g100", GroupText = "LABEL" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T0.Add10", Text = "Přidat 10", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 0, 0, 10, 1) });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T0.Add30", Text = "Přidat 30", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 0, 0, 30, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T0.Add100", Text = "Přidat 100", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 0, 0, 100, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.LT10.Add300", Text = "Přidat 300", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 0, 0, 300, 1) });

            group = new DataRibbonGroup() { GroupId = "g010", GroupText = "TEXTBOX" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T1.Add10", Text = "Přidat 10", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 1, 0, 10, 1) });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T1.Add30", Text = "Přidat 30", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 1, 0, 30, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T1.Add100", Text = "Přidat 100", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 1, 0, 100, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T1.Add300", Text = "Přidat 300", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 1, 0, 300, 1) });

            group = new DataRibbonGroup() { GroupId = "g001", GroupText = "CHECKBOX" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T0C1.Add10", Text = "Přidat 10", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 0, 1, 10, 1) });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T0C1.Add30", Text = "Přidat 30", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 0, 1, 30, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T0C1.Add100", Text = "Přidat 100", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 0, 1, 100, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L0T0C1.Add300", Text = "Přidat 300", Image = imageAdd, Tag = new DxDataFormTestDefinition(0, 0, 1, 300, 1) });

            group = new DataRibbonGroup() { GroupId = "g110", GroupText = "LABEL + TEXTBOX" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1.Add10", Text = "Přidat 10", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 0, 10, 1) });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1.Add30", Text = "Přidat 30", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 0, 30, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1.Add100", Text = "Přidat 100", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 0, 100, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1.Add300", Text = "Přidat 300", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 0, 300, 1) });

            group = new DataRibbonGroup() { GroupId = "g120", GroupText = "LABEL + 2x TEXTBOX" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T2.Add10", Text = "Přidat 10", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 2, 0, 10, 1) });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T2.Add30", Text = "Přidat 30", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 2, 0, 30, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T2.Add100", Text = "Přidat 100", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 2, 0, 100, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T2.Add300", Text = "Přidat 300", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 2, 0, 300, 1) });

            group = new DataRibbonGroup() { GroupId = "g111", GroupText = "LABEL + TEXTBOX + CHECKBOX" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1C1.Add10", Text = "Přidat 10", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 1, 10, 1) });
            // group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1C1.Add30", Text = "Přidat 30", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 1, 30, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1C1.Add100", Text = "Přidat 100", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 1, 100, 1) });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.L1T1C1.Add300", Text = "Přidat 300", Image = imageAdd, Tag = new DxDataFormTestDefinition(1, 1, 1, 300, 1) });

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

                case "Dx.Params.MemoryOptimized":
                    _DxDataFormMemoryOptimized = (e.Item.Checked ?? false);
                    break;
                case "Dx.Params.Add50":
                    _DxDataFormAdd50 = (e.Item.Checked ?? false);
                    break;
                case "Dx.Params.UseWinForm":
                    _RemoveDataForms();         // Změna zvolené komponenty musí vždy shodit aktuální komponentu, kvůli vizuální shodě Ribbon :: DataForm
                    _DxDataUseWinForm = (e.Item.Checked ?? false);
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
        #region DataForm
        private void _AddDataFormSampleData(DxDataFormTestDefinition sampleData)
        {
            _RemoveDataForms();

            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus

            if (!_DxDataUseWinForm)
                _AddDataFormDx(sampleData);
            else
                _AddDataFormWf(sampleData);
            
            _DoLayoutAnyDataForm();
            _AnyDataForm.Focus();

            RefreshStatusCurrent();
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
            DxDataFormV1 dxDataForm = CreateValidDxDataForm();

            _DxDataForm = dxDataForm;
            _AnyDataForm = dxDataForm;
            _DoLayoutAnyDataForm();

            var addStartTime = DxComponent.LogTimeCurrent;
            dxDataForm.AddItems(sampleItems);
            DxComponent.LogAddLineTime($"AddItems: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            _DxTestPanel.Controls.Add(dxDataForm);

            _DoLayoutAnyDataForm();
            _AnyDataForm.Focus();

            RefreshStatusCurrent();
        }
        private void _AddDataFormDx(DxDataFormTestDefinition sample)
        {
            DxDataFormV1 dxDataForm = CreateValidDxDataForm();

            _DxDataForm = dxDataForm;
            _AnyDataForm = dxDataForm;
            _DoLayoutAnyDataForm();

            var sampleStartTime = DxComponent.LogTimeCurrent;
            var sampleItems = DxDataFormTest.CreateSample(sample);
            int count = sampleItems.Count();
            DxComponent.LogAddLineTime($"CreateSample: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            var addStartTime = DxComponent.LogTimeCurrent;
            dxDataForm.AddItems(sampleItems);
            DxComponent.LogAddLineTime($"AddItems: Items.Count: {count}; Time: {DxComponent.LogTokenTimeMilisec}", sampleStartTime);

            _DxTestPanel.Controls.Add(dxDataForm);
        }
        private void _AddDataFormWf(DxDataFormTestDefinition sample)
        {
            WfDataForm wfDataForm = new WfDataForm();
            wfDataForm.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            wfDataForm.GotFocus += DxDataForm_GotFocus;

            _WfDataForm = wfDataForm;
            _AnyDataForm = wfDataForm;
            _DoLayoutAnyDataForm();

            wfDataForm.CreateSample(sample);

            _DxTestPanel.Controls.Add(wfDataForm);
        }
        private DxDataFormV1 CreateValidDxDataForm()
        {
            DxDataFormV1 dxDataForm = new DxDataFormV1();
            dxDataForm.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            dxDataForm.MemoryMode = (_DxDataFormMemoryOptimized ? DxDataFormMemoryMode.Default2 : DxDataFormMemoryMode.HostAllways);
            dxDataForm.GotFocus += DxDataForm_GotFocus;
            dxDataForm.TabChangeDone += DxDataForm_TabChangeDone;
            return dxDataForm;
        }
        private void DxDataForm_TabChangeDone(object sender, EventArgs e)
        {
            RefreshStatusCurrent();
        }
        private void DxDataForm_GotFocus(object sender, EventArgs e)
        {
            if (!_DxShowTimeSpan.HasValue && _DxShowTimeStart.HasValue)
            {
                _DxShowTimeSpan = DateTime.Now - _DxShowTimeStart.Value;
                RefreshStatusCurrent();
            }
        }
        private void _RemoveDataForms()
        {
            _RemoveDxDataForms();
            RefreshStatusCurrent();
        }
        private void _RemoveDxDataForms()
        {
            _DxDataForm = null;
            _WfDataForm = null;
            _AnyDataForm = null;
            _DxShowTimeStart = null;
            _DxShowTimeSpan = null;
            var controls = _DxTestPanel.Controls.OfType<System.Windows.Forms.Control>().ToArray();
            foreach (var control in controls)
            {
                if (!(control is DxLabelControl))          // DxLabelControl si necháme, to je titulek...
                {
                    control.GotFocus -= DxDataForm_GotFocus;
                    _DxTestPanel.Controls.Remove(control);
                    control.Dispose();
                }
            }
            GCCollect();
            WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();
        }
        private void _DoLayoutAnyDataForm()
        {
            var dataForm = _AnyDataForm;
            if (dataForm != null)
            {
                var clientSize = _DxTestPanel.ClientSize;
                int y = _DxTitleLabel.Bounds.Bottom + 6;
                dataForm.Bounds = new System.Drawing.Rectangle(6, y, clientSize.Width - 12, clientSize.Height - y - 6);
            }
        }
        private DxDataFormV1 _DxDataForm;
        private WfDataForm _WfDataForm;
        private System.Windows.Forms.Control _AnyDataForm;
        private bool _DxDataFormMemoryOptimized;
        private bool _DxDataFormAdd50;
        private bool _DxDataUseWinForm;
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
