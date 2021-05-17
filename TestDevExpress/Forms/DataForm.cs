using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    public class DataForm : DxRibbonForm
    {
        #region Konstruktor a proměnné

        public DataForm()
        {
            this.InitializeForm();
        }
        protected void InitializeForm()
        {
            this.Size = new System.Drawing.Size(800, 600);
            this.Text = "TESTER DataForm";

            _DxMainPanel = DxComponent.CreateDxPanel(this, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
            _DxMainPanel.SizeChanged += _DxMainPanel_SizeChanged;
            DxComponent.CreateDxLabel(10, 10, 500, _DxMainPanel, "Zde bude DataForm", styleType: LabelStyleType.SubTitle);

            this._DxRibbonControl = new DxRibbonControl();
            this.Ribbon = _DxRibbonControl;
            this.Controls.Add(this._DxRibbonControl);

            _DxRibbonFill();
            this._DxRibbonControl.RibbonItemClick += _DxRibbonControl_RibbonItemClick;


            this._DxRibbonStatusBar = new DxRibbonStatusBar();
            this._DxRibbonStatusBar.Ribbon = this._DxRibbonControl;
            this.StatusBar = _DxRibbonStatusBar;
            this.Controls.Add(this._DxRibbonStatusBar);

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
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemDeltaCurrent);
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemTime);
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemCurrent);
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemBefore);
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemDeltaForm);
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemDeltaConstructor);
            this._DxRibbonStatusBar.ItemLinks.Add(this._StatusItemDeltaShow);

            WinProcessInfoAfterInit = DxComponent.WinProcessInfo.GetCurent();
        }

        private void _DxMainPanel_SizeChanged(object sender, EventArgs e)
        {
            _DoLayoutAnyDataForm();
        }

        private DxRibbonControl _DxRibbonControl;
        private DxRibbonStatusBar _DxRibbonStatusBar;
        private DxPanelControl _DxMainPanel;
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
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        private DevExpress.XtraBars.BarStaticItem _StatusItemBefore;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaConstructor;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaShow;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaCurrent;
        private DevExpress.XtraBars.BarStaticItem _StatusItemDeltaForm;
        private DevExpress.XtraBars.BarStaticItem _StatusItemTime;
        private DevExpress.XtraBars.BarStaticItem _StatusItemCurrent;
        #endregion
        #region Ribbon - obsah a rozcestník
        private void _DxRibbonFill()
        {
            string imageRefresh = "svgimages/xaf/action_refresh_kpi.svg";
            string imageClear = "devav/actions/removeitem.svg";
            string imageAdd = "svgimages/icon%20builder/actions_addcircled.svg";
            string imageTest = "svgimages/xaf/actiongroup_easytestrecorder.svg";


            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "basic", GroupText = "ZÁKLADNÍ", ItemId = "Dx.Basic.Refresh", ItemText = "Refresh", ToolTip = "Znovu načíst údaje do statusbaru o spotřebě systémových zdrojů", ItemImage = imageRefresh });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "basic", GroupText = "ZÁKLADNÍ", ItemId = "Dx.Basic.Clear", ItemText = "Smazat", ToolTip = "Zahodit DataForm a uvolnit jeho zdroje", ItemImage = imageClear });

            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "design", GroupText = "DESIGN", ItemId = "Dx.Design.Skin", ItemType = RibbonItemType.SkinSetDropDown});
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "design", GroupText = "DESIGN", ItemId = "Dx.Design.Palette", ItemType = RibbonItemType.SkinPaletteDropDown });

            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "sample", GroupText = "VZORKY", ItemId = "Dx.Sample.Sample1", ItemText = "Ukázka 1", ItemImage = imageTest, Tag = "Sample1" });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "sample", GroupText = "VZORKY", ItemId = "Dx.Sample.Sample2", ItemText = "Ukázka 2", ItemImage = imageTest, Tag = "Sample2" });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "sample", GroupText = "VZORKY", ItemId = "Dx.Sample.Sample3", ItemText = "Ukázka 3", ItemImage = imageTest, Tag = "Sample3" });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "sample", GroupText = "VZORKY", ItemId = "Dx.Sample.Sample4", ItemText = "Ukázka 4", ItemImage = imageTest, Tag = "Sample4" });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "sample", GroupText = "VZORKY", ItemId = "Dx.Sample.Sample5", ItemText = "Ukázka 5", ItemImage = imageTest, Tag = "Sample5" });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "sample", GroupText = "VZORKY", ItemId = "Dx.Sample.Sample6", ItemText = "Ukázka 6", ItemImage = imageTest, Tag = "Sample6" });

            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "params", GroupText = "PARAMETRY", ItemId = "Dx.Params.AddControls", ItemText = "Vkládat Controly", ToolTip = "Vytvořit instance controlů ale NEPŘIDÁVAT JE do Panelu (test rychlosti)", ItemType = RibbonItemType.CheckBoxToggle, ItemIsChecked = true, RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "params", GroupText = "PARAMETRY", ItemId = "Dx.Params.Add50", ItemText = "Spořit Controly", ToolTip = "Vytvořit instance controlů, vložit do Panelu, a pak 50% odebrat z panelu (test rychlosti)", ItemType = RibbonItemType.CheckBoxToggle, ItemIsChecked = false, RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "params", GroupText = "PARAMETRY", ItemId = "Dx.Params.UseWinForm", ItemText = "Použít WinForms", ToolTip = "Nezaškrtnuté = DevExpress;\r\nZaškrtnuté = WinForm", ItemType = RibbonItemType.CheckBoxToggle, ItemIsChecked = null, RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText });

            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t0", GroupText = "LABEL", ItemId = "Dx.L1T0.Add10", ItemText = "Přidat 10", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 0, 0, 10, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t0", GroupText = "LABEL", ItemId = "Dx.L1T0.Add30", ItemText = "Přidat 30", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 0, 0, 30, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t0", GroupText = "LABEL", ItemId = "Dx.L1T0.Add100", ItemText = "Přidat 100", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 0, 0, 100, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t0", GroupText = "LABEL", ItemId = "Dx.LT10.Add300", ItemText = "Přidat 300", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 0, 0, 300, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t1", GroupText = "TEXTBOX", ItemId = "Dx.L0T1.Add10", ItemText = "Přidat 10", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 1, 0, 10, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t1", GroupText = "TEXTBOX", ItemId = "Dx.L0T1.Add30", ItemText = "Přidat 30", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 1, 0, 30, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t1", GroupText = "TEXTBOX", ItemId = "Dx.L0T1.Add100", ItemText = "Přidat 100", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 1, 0, 100, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t1", GroupText = "TEXTBOX", ItemId = "Dx.L0T1.Add300", ItemText = "Přidat 300", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 1, 0, 300, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t0c1", GroupText = "CHECKBOX", ItemId = "Dx.L0T0C1.Add10", ItemText = "Přidat 10", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 0, 1, 10, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t0c1", GroupText = "CHECKBOX", ItemId = "Dx.L0T0C1.Add30", ItemText = "Přidat 30", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 0, 1, 30, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t0c1", GroupText = "CHECKBOX", ItemId = "Dx.L0T0C1.Add100", ItemText = "Přidat 100", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 0, 1, 100, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l0t0c1", GroupText = "CHECKBOX", ItemId = "Dx.L0T0C1.Add300", ItemText = "Přidat 300", ItemImage = imageAdd, Tag = new DxDataFormSample(0, 0, 1, 300, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1", GroupText = "LABEL + TEXTBOX", ItemId = "Dx.L1T1.Add10", ItemText = "Přidat 10", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 0, 10, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1", GroupText = "LABEL + TEXTBOX", ItemId = "Dx.L1T1.Add30", ItemText = "Přidat 30", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 0, 30, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1", GroupText = "LABEL + TEXTBOX", ItemId = "Dx.L1T1.Add100", ItemText = "Přidat 100", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 0, 100, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1", GroupText = "LABEL + TEXTBOX", ItemId = "Dx.L1T1.Add300", ItemText = "Přidat 300", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 0, 300, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t2", GroupText = "LABEL + 2x TEXTBOX", ItemId = "Dx.L1T2.Add10", ItemText = "Přidat 10", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 2, 0, 10, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t2", GroupText = "LABEL + 2x TEXTBOX", ItemId = "Dx.L1T2.Add30", ItemText = "Přidat 30", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 2, 0, 30, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t2", GroupText = "LABEL + 2x TEXTBOX", ItemId = "Dx.L1T2.Add100", ItemText = "Přidat 100", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 2, 0, 100, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t2", GroupText = "LABEL + 2x TEXTBOX", ItemId = "Dx.L1T2.Add300", ItemText = "Přidat 300", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 2, 0, 300, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1c1", GroupText = "LABEL + TEXTBOX + CHECKBOX", ItemId = "Dx.L1T1C1.Add10", ItemText = "Přidat 10", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 1, 10, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1c1", GroupText = "LABEL + TEXTBOX + CHECKBOX", ItemId = "Dx.L1T1C1.Add30", ItemText = "Přidat 30", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 1, 30, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1c1", GroupText = "LABEL + TEXTBOX + CHECKBOX", ItemId = "Dx.L1T1C1.Add100", ItemText = "Přidat 100", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 1, 100, 1) });
            this._DxRibbonControl.AddItem(new RibbonItem() { PageText = "DevExpress", GroupId = "l1t1c1", GroupText = "LABEL + TEXTBOX + CHECKBOX", ItemId = "Dx.L1T1C1.Add300", ItemText = "Přidat 300", ItemImage = imageAdd, Tag = new DxDataFormSample(1, 1, 1, 300, 1) });

        }



        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IMenuItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Basic.Refresh":
                    GCCollect();
                    RefreshStatusCurrent();
                    break;
                case "Dx.Basic.Clear":
                    _RemoveDataForms();
                    break;
                case "Dx.Params.AddControls":
                    _DxDataFormNoAdd = !(e.Item.ItemIsChecked ?? false);
                    break;
                case "Dx.Params.Add50":
                    _DxDataFormAdd50 = (e.Item.ItemIsChecked ?? false);
                    break;
                case "Dx.Params.UseWinForm":
                    _RemoveDataForms();         // Změna zvolené komponenty musí vždy shodit aktuální komponentu, kvůli vizuální shodě Ribbon :: DataForm
                    _DxDataUseWinForm = (e.Item.ItemIsChecked ?? false);
                    break;
                default:
                    if (e.Item.Tag is DxDataFormSample sampleData)
                        this._AddDataFormSampleData(sampleData);
                    else if (e.Item.Tag is string sampleName)
                        this._AddDataFormSampleName(sampleName);
                    break;
            }
        }
        #endregion
        #region DataForm
        private void _AddDataFormSampleData(DxDataFormSample sampleData)
        {
            _RemoveDataForms();

            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus
            sampleData.NoAddControlsToPanel = _DxDataFormNoAdd;
            sampleData.Add50ControlsToPanel = _DxDataFormAdd50;
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

            var sampleItems = DxDataForm.CreateSample(sampleId);
            if (sampleItems == null) return;

            _RemoveDataForms();

            _DxShowTimeStart = DateTime.Now;               // Určení času End a času Elapsed proběhne v DxDataForm_GotFocus
            DxDataForm dxDataForm = new DxDataForm();
            dxDataForm.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            dxDataForm.GotFocus += DxDataForm_GotFocus;

            _DxDataForm = dxDataForm;
            _AnyDataForm = dxDataForm;
            _DoLayoutAnyDataForm();

            dxDataForm.AddItems(sampleItems);

            _DxMainPanel.Controls.Add(dxDataForm);

            _DoLayoutAnyDataForm();
            _AnyDataForm.Focus();

            RefreshStatusCurrent();
        }
        private void _AddDataFormDx(DxDataFormSample sample)
        {
            DxDataForm dxDataForm = new DxDataForm();
            dxDataForm.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            dxDataForm.GotFocus += DxDataForm_GotFocus;

            _DxDataForm = dxDataForm;
            _AnyDataForm = dxDataForm;
            _DoLayoutAnyDataForm();

            var items = DxDataForm.CreateSample(sample);
            dxDataForm.AddItems(items);

            _DxMainPanel.Controls.Add(dxDataForm);
        }
        private void _AddDataFormWf(DxDataFormSample sample)
        {
            WfDataForm wfDataForm = new WfDataForm();
            wfDataForm.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            wfDataForm.GotFocus += DxDataForm_GotFocus;

            _WfDataForm = wfDataForm;
            _AnyDataForm = wfDataForm;
            _DoLayoutAnyDataForm();

            wfDataForm.CreateSample(sample);

            _DxMainPanel.Controls.Add(wfDataForm);
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
            var controls = _DxMainPanel.Controls.OfType<System.Windows.Forms.Control>().ToArray();
            foreach (var control in controls)
            {
                if (!(control is DxLabelControl))          // DxLabelControl si necháme, to je titulek...
                {
                    control.GotFocus -= DxDataForm_GotFocus;
                    _DxMainPanel.Controls.Remove(control);
                    control.Dispose();
                }
            }
            GCCollect();
            WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();
        }
        private void GCCollect()
        {
            GC.Collect(0, GCCollectionMode.Forced);
        }
        private void _DoLayoutAnyDataForm()
        {
            var anyDataForm = _AnyDataForm;
            if (anyDataForm != null)
            {
                var clientSize = _DxMainPanel.ClientSize;
                anyDataForm.Bounds = new System.Drawing.Rectangle(6, 32, clientSize.Width - 12, clientSize.Height - 38);
            }
        }
        private DxDataForm _DxDataForm;
        private WfDataForm _WfDataForm;
        private System.Windows.Forms.Control _AnyDataForm;
        private bool _DxDataFormNoAdd;
        private bool _DxDataFormAdd50;
        private bool _DxDataUseWinForm;
        private DateTime? _DxShowTimeStart;
        private TimeSpan? _DxShowTimeSpan;
        #endregion
        #region Zobrazení spotřeby paměti
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
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
        #endregion
    }
}
