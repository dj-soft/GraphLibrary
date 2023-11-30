using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Noris.Clients.Win.Components.AsolDX;
using DxDForm = Noris.Clients.Win.Components.AsolDX.DataForm;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DevExpress.Export;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormX"/>
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "DataForm 3", buttonOrder: 12, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře okno DataForm verze 3")]
    public class DataFormV3 : DxRibbonForm
    {
        // string resource1 = "svgimages/spreadsheet/showcompactformpivottable.svg";
        // string resource2 = "svgimages/spreadsheet/showoutlineformpivottable.svg";
        #region Inicializace
        public DataFormV3()
        {
            __CurrentId = ++__InstanceCounter;
            __DataFormId = 0;
            _RefreshTitle();
        }
        private void _RefreshTitle()
        {
            bool hasDataForm = (_DxDataFormV3 != null);
            string formId = (hasDataForm ? ":" + __DataFormId.ToString() : "");
            this.Text = $"DataForm V3 [{__CurrentId}{formId}]";

        }
        private int __CurrentId;
        private int __DataFormId;
        private static int __InstanceCounter;
        #endregion
        #region Ribbon - obsah a rozcestník
        /// <summary>
        /// Připraví Ribbon
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);
            pages.Add(page);

            string imageStatusRefresh = "svgimages/xaf/action_refresh.svg";
            string imageDataFormRemove = "svgimages/spreadsheet/removetablerows.svg";
            string imageLogClear = "svgimages/spreadsheet/removetablerows.svg";
            group = new DataRibbonGroup() { GroupText = "ZÁKLADNÍ" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "StatusRefresh", Text = "Refresh Status", ToolTipText = "Znovu načíst údaje o spotřebě systémových zdrojů do statusbaru", ImageName = imageStatusRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "LogClear", Text = "Clear Log", ToolTipText = "Smaže obsah logu vpravo", ImageName = imageLogClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "DataFormRemove", Text = "Remove DataForm", ToolTipText = "Zahodit DataForm a uvolnit jeho zdroje", ImageName = imageDataFormRemove });

            var groupSamples = new DataRibbonGroup() { GroupText = "VZORKY" };

            string radioGroupName = "SamplesGroup";
            page.Groups.Add(groupSamples);
            string imageTest1 = "svgimages/xaf/actiongroup_easytestrecorder.svg";
            string imageTest2 = "svgimages/spreadsheet/showoutlineformpivottable.svg";
            string imageTest3 = "svgimages/spreadsheet/showtabularformpivottable.svg";

            // Na čísla Sample reagují metody _CreateSampleLayout() a _CreateSampleRows() !
            addSampleButton(1001, "Form A x 1 řádek", imageTest1);
            addSampleButton(1002, "Form A x 2 řádky", imageTest1);
            addSampleButton(2001, "Form B x 1 řádek", imageTest2, true);
            addSampleButton(2100, "Form B x 100 řádků", imageTest2);
            addSampleButton(3001, "Table x 1 řádek", imageTest3, true);
            addSampleButton(3012, "Table x 12 řádek", imageTest3);
            addSampleButton(3144, "Table x 144 řádek", imageTest3);
            addSampleButton(3600, "Table x 600 řádek", imageTest3);

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
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
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
        private void GCCollect()
        {
            GC.Collect(0, GCCollectionMode.Forced);
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
        /// <summary>
        /// Přidá DataForm s daným obsahem
        /// </summary>
        /// <param name="sampleId"></param>
        private void _AddDataFormSample(int sampleId)
        {
            _RemoveDataForms();

            __DataFormId++;
            var dataForm = new DxDForm.DxDataFormPanel() { Dock = DockStyle.Fill };         // , BackColor = Color.FromArgb(190, 180, 240)
            DxMainPanel.Controls.Add(dataForm);
            dataForm.GotFocus += DxDataForm_GotFocus;

            dataForm.DataFormLayout.Store(_CreateSampleLayout(sampleId));
            dataForm.DataFormRows.Store(_CreateSampleRows(sampleId));

            _DxDataFormV3 = dataForm;
        
            _RefreshTitle();
        }

        /// <summary>
        /// Odebere DataForm
        /// </summary>
        private void _RemoveDataForms()
        {
            var dataForm = _DxDataFormV3;
            if (dataForm != null)
            {
                if (DxMainPanel.Controls.Contains(dataForm))
                    DxMainPanel.Controls.Remove(dataForm);
                dataForm.GotFocus -= DxDataForm_GotFocus;
                dataForm.Dispose();

            }
            _DxDataFormV3 = null;
            _DxShowTimeStart = null;
            _DxShowTimeSpan = null;

            GCCollect();
            WinProcessInfoAfterShown = DxComponent.WinProcessInfo.GetCurent();

            _RefreshTitle();
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
        private DxDForm.DxDataFormPanel _DxDataFormV3;
        private DateTime? _DxShowTimeStart;
        private TimeSpan? _DxShowTimeSpan;
        #endregion
        #region Layouty
        private List<DxDData.DataFormLayoutItem> _CreateSampleLayout(int sampleId)
        {
            var result = new List<DxDData.DataFormLayoutItem>();

            int layoutId = (sampleId / 1000);
            int rowsCount = (sampleId % 1000);

            switch (layoutId)
            {
                case 1:
                    addItemPairT("datum", "Datum:", DxDForm.DxRepositoryEditorType.TextBox, 6, 10, 60, 20);
                    addItemPairT("reference", "Reference:", DxDForm.DxRepositoryEditorType.TextBox, 6, 80, 120, 20);
                    addItemPairT("nazev", "Název:", DxDForm.DxRepositoryEditorType.TextBox, 6, 210, 250, 20);
                    addItemPairT("pocet", "Počet:", DxDForm.DxRepositoryEditorType.TextBox, 50, 10, 90, 20);
                    addItemPairT("cena1", "Cena 1ks:", DxDForm.DxRepositoryEditorType.TextBox, 50, 110, 80, 20);
                    addItemPairT("sazbadph", "Sazba DPH:", DxDForm.DxRepositoryEditorType.TextBox, 94, 10, 140, 20);
                    addItemPairT("cenacelk", "Cena celkem:", DxDForm.DxRepositoryEditorType.TextBox, 94, 160, 70, 20);
                    addItemPairT("poznamka", "Poznámka:", DxDForm.DxRepositoryEditorType.TextBox, 50, 240, 350, 90);
                    break;
                case 2:
                    addItemPairL("datum", "Datum:", DxDForm.DxRepositoryEditorType.TextBox, 6, 10, 60, 20);
                    addItemPairL("reference", "Reference:", DxDForm.DxRepositoryEditorType.TextBox, 6, 150, 120, 20);
                    addItemPairL("nazev", "Název:", DxDForm.DxRepositoryEditorType.TextBox, 6, 345, 250, 20);
                    addItemPairL("pocet", "Počet:", DxDForm.DxRepositoryEditorType.TextBox, 28, 10, 90, 20);
                    addItemPairL("cena1", "Cena 1ks:", DxDForm.DxRepositoryEditorType.TextBox, 28, 180, 80, 20);
                    addItemPairL("sazbadph", "Sazba DPH:", DxDForm.DxRepositoryEditorType.TextBox, 50, 10, 140, 20);
                    addItemPairL("cenacelk", "Cena celkem:", DxDForm.DxRepositoryEditorType.TextBox, 50, 230, 70, 20);
                    addItemPairL("poznamka", "Poznámka:", DxDForm.DxRepositoryEditorType.TextBox, 6, 680, 350, 90);
                    break;
                case 3:
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 0, 75, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 80, 40, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 125, 40, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 170, 150, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 325, 150, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 480, 100, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 585, 60, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 650, 60, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 715, 200, null, 1, 20, null);
                    addItemType("id", DxDForm.DxRepositoryEditorType.TextBox, 920, 400, null, 1, 20, null);
                    break;
            }

            return result;

            void addItemPairT(string columnId, string labelText, DxDForm.DxRepositoryEditorType columnType, int top, int left, int width, int height)
            {
                addItemLabel(columnId + ".label", labelText, left + 3, width - 8, null, top, 18, null);
                addItemType(columnId, columnType, left, width, null, top + 18, height, null);
            }
            void addItemPairL(string columnId, string labelText, DxDForm.DxRepositoryEditorType columnType, int top, int left, int width, int height)
            {
                addItemLabel(columnId + ".label", labelText, left, 75, null, top + 2, 18, null);
                addItemType(columnId, columnType, left + 78, width, null, top, height, null);
            }

            void addItemLabel(string columnId, string labelText, int? left, int? width, int? right, int? top, int? height, int? bottom)
            {
                DxDData.DataFormLayoutItem item = new DxDData.DataFormLayoutItem()
                {
                    ColumnId = columnId,
                    ColumnType = DxDForm.DxRepositoryEditorType.Label,
                    LabelText = labelText,
                    DesignBoundsExt = new DxDForm.RectangleExt(left, width, right, top, height, bottom)
                };
                result.Add(item);
            }
            void addItemType(string columnId, DxDForm.DxRepositoryEditorType columnType, int? left, int? width, int? right, int? top, int? height, int? bottom)
            {
                DxDData.DataFormLayoutItem item = new DxDData.DataFormLayoutItem()
                {
                    ColumnId = columnId,
                    ColumnType = columnType,
                    DesignBoundsExt = new DxDForm.RectangleExt(left, width, right, top, height, bottom)
                };
                result.Add(item);
            }
        }

        private List<DxDData.DataFormRow> _CreateSampleRows(int sampleId)
        {
            var result = new List<DxDData.DataFormRow>();

            int layoutId = (sampleId / 1000);
            int rowsCount = (sampleId % 1000);

            for (int r = 0; r < rowsCount; r++)
            {

                switch (layoutId)
                {
                    case 1:
                    case 2:
                    case 3:
                        addRow();
                        break;


                }
            }

            return result;

            void addRow()
            {
                result.Add(new DxDData.DataFormRow());
            }
        }
        #endregion
    }
}

