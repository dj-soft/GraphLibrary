﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using DxDForm = Noris.Clients.Win.Components.AsolDX.DataForm;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using System.IO;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormX"/>
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "DataForm 3", buttonOrder: 12, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře okno DataForm verze 3")]
    public class DataFormV3 : DxRibbonForm
    {
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
            this.TestPainting = false;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);
            pages.Add(page);

            string imageStatusRefresh = "svgimages/xaf/action_refresh.svg";
            string imageTestDrawing = "svgimages/dashboards/textbox.svg";
            string imageDataFormRemove = "svgimages/spreadsheet/removetablerows.svg";
            string imageLogClear = "svgimages/spreadsheet/removetablerows.svg";

            group = new DataRibbonGroup() { GroupText = "ZÁKLADNÍ" };
            page.Groups.Add(group);
            group.Items.Add(new DataRibbonItem() { ItemId = "StatusRefresh", Text = "Refresh Status", ToolTipText = "Znovu načíst údaje o spotřebě systémových zdrojů do statusbaru", ImageName = imageStatusRefresh });
            group.Items.Add(new DataRibbonItem() { ItemId = "TestDrawing", Text = "TestDrawing", ToolTipText = "Vykreslování bez fyzických Controlů - pro test rychlosti", ImageName = imageTestDrawing, RibbonStyle = RibbonItemStyles.Large, ItemType = RibbonItemType.CheckButton, Checked = TestPainting });
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
            addSampleButton(1060, "Form A x 60 řádků", imageTest1);
            addSampleButton(2001, "Form B x 1 řádek", imageTest2, true);
            addSampleButton(2024, "Form B x 24 řádků", imageTest2);
            addSampleButton(2120, "Form B x 120 řádků", imageTest2);
            addSampleButton(3001, "Table x 1 řádek", imageTest3, true);
            addSampleButton(3036, "Table x 36 řádek", imageTest3);
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
                    RefreshStatusCurrent(true);
                    break;
                case "TestDrawing":
                    this.TestPainting = e.Item?.Checked ?? false; 
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
                default:
                    var n = itemId;
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
            RefreshStatus(false);
        }
        public DxComponent.WinProcessInfo WinProcessInfoBeforeForm { get; set; }
        public DxComponent.WinProcessInfo WinProcessInfoAfterInit { get; set; }
        public DxComponent.WinProcessInfo WinProcessInfoAfterShown { get; set; }
        private void RefreshStatus(bool withGcCollect)
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

            RefreshStatusCurrent(withGcCollect);
        }
        private void RefreshStatusCurrent(bool withGcCollect)
        {
            if (withGcCollect) GCCollect();

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
            dataForm.TestPainting = this.TestPainting;

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
            RefreshStatusCurrent(false);
        }
        private void DxDataForm_GotFocus(object sender, EventArgs e)
        {
            if (!_DxShowTimeSpan.HasValue && _DxShowTimeStart.HasValue)
            {
                _DxShowTimeSpan = DateTime.Now - _DxShowTimeStart.Value;
                RefreshStatusCurrent(false);
            }
        }
        /// <summary>
        /// Používat testovací vykreslování
        /// </summary>
        public bool TestPainting
        { 
            get { return __TestPainting; } 
            set 
            {
                __TestPainting = value;
                if (this._DxDataFormV3 != null)
                    this._DxDataFormV3.TestPainting = value;
            }
        }
        private bool __TestPainting;

        private DxDForm.DxDataFormPanel _DxDataFormV3;
        private DateTime? _DxShowTimeStart;
        private TimeSpan? _DxShowTimeSpan;
        #endregion
        #region Layouty
        /// <summary>
        /// Vytvoří a vrátí layout daného ID
        /// </summary>
        /// <param name="sampleId"></param>
        /// <returns></returns>
        private List<DxDData.DataFormLayoutItem> _CreateSampleLayout(int sampleId)
        {
            var result = new List<DxDData.DataFormLayoutItem>();

            int layoutId = (sampleId / 1000);
            int rowsCount = (sampleId % 1000);
            int leftB = 14;
            int left, leftM, topM;
            int top = 0;
            switch (layoutId)
            {
                case 1:
                    top = 12;
                    left = leftB;
                    addItemPairT("datum", "Datum:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 70, 20);
                    addItemPairT("reference", "Reference:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 120, 20);
                    addItemPairT("nazev", "Název:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 250, 20);
                    leftM = left;
                    topM = top;
                    left = leftB; top += 44; 
                    addItemPairT("pocet", "Počet:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 110, 20);
                    addItemPairT("cena1", "Cena 1ks:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 80, 20);
                    addItemType("button_open", DxDForm.DxRepositoryEditorType.Button, "Otevři", left + 90, 140, null, top + 6, 44, null, item =>
                    {
                        item.Content[DxDData.DxDataFormDef.IconName] = "svgimages/reports/preview.svg";
                    });

                    left = leftB; top += 44;
                    addItemPairT("sazbadph", "Sazba DPH:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 120, 20);
                    addItemPairT("cenacelk", "Cena cel.:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 70, 20);
                    addItemPairT("filename", "Dokument:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 250, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormDef.TextBoxButtons] = "Down;Plus";
                    });
                    left = leftB; top += 44;
                    addItemPairT("relation", "Vztah:", DxDForm.DxRepositoryEditorType.TextBoxButton, top, ref left, 456, 20, item =>
                    {
                        item.Content[DxDData.DxDataFormDef.TextBoxButtons] = "SpinRight;Clear;Ellipsis";
                    });

                    left = leftM; top = topM;
                    addItemPairT("poznamka", "Poznámka:", DxDForm.DxRepositoryEditorType.EditBox, top, ref left, 350, 154);
                    break;
                case 2:
                    top = 16;
                    left = leftB;
                    addItemPairL("datum", "Datum:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 65, 20);
                    addItemPairL("reference", "Reference:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 145, 20);
                    addItemPairL("nazev", "Název:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 250, 20);

                    top = 44;
                    left = leftB;
                    addItemPairL("pocet", "Počet:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 90, 20);
                    addItemPairL("cena1", "Cena 1ks:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 120, 20);

                    top = 72;
                    left = leftB;
                    addItemPairL("sazbadph", "Sazba DPH:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 140, 20);
                    addItemPairL("cenacelk", "Cena celkem:", DxDForm.DxRepositoryEditorType.TextBox, top, ref left, 70, 20);

                    top = 16;
                    left = 730;
                    addItemPairL("poznamka", "Poznámka:", DxDForm.DxRepositoryEditorType.EditBox, top, ref left, 350, 90);
                    break;
                case 3:
                    addItemType("id1", DxDForm.DxRepositoryEditorType.TextBox, null, 0, 75, null, 2, 20, null);
                    addItemType("id2", DxDForm.DxRepositoryEditorType.TextBox, null, 80, 40, null, 2, 20, null);
                    addItemType("id3", DxDForm.DxRepositoryEditorType.TextBox, null, 125, 40, null, 2, 20, null);
                    addItemType("id4", DxDForm.DxRepositoryEditorType.TextBox, null, 170, 150, null, 2, 20, null);
                    addItemType("id5", DxDForm.DxRepositoryEditorType.TextBox, null, 325, 150, null, 2, 20, null);
                    addItemType("id6", DxDForm.DxRepositoryEditorType.TextBox, null, 480, 100, null, 2, 20, null);
                    addItemType("id7", DxDForm.DxRepositoryEditorType.TextBox, null, 585, 60, null, 2, 20, null);
                    addItemType("id8", DxDForm.DxRepositoryEditorType.TextBox, null, 650, 60, null, 2, 20, null);
                    addItemType("id9", DxDForm.DxRepositoryEditorType.TextBox, null, 715, 200, null, 2, 20, null);
                    addItemType("id0", DxDForm.DxRepositoryEditorType.TextBox, null, 920, 400, null, 2, 20, null);
                    break;
            }

            return result;

            void addItemPairT(string columnId, string labelText, DxDForm.DxRepositoryEditorType columnType, int top, ref int left, int width, int height, Action<DxDData.DataFormLayoutItem> modifier = null)
            {
                addItemLabel(columnId + ".label", labelText, left + 3, width - 8, null, top, 18, null);
                addItemType(columnId, columnType, null, left, width, null, top + 18, height, null, modifier);
                left = left + width + 8;
            }
            void addItemPairL(string columnId, string labelText, DxDForm.DxRepositoryEditorType columnType, int top, ref int left, int width, int height, Action<DxDData.DataFormLayoutItem> modifier = null)
            {
                addItemLabel(columnId + ".label", labelText, left, 75, null, top + 2, 18, null);
                left += 80;
                addItemType(columnId, columnType, null, left, width, null, top, height, null, modifier);
                left += (width + 8);
            }
            void addItemLabel(string columnId, string labelText, int? left, int? width, int? right, int? top, int? height, int? bottom)
            {
                DxDData.DataFormLayoutItem item = new DxDData.DataFormLayoutItem()
                {
                    ColumnName = columnId,
                    ColumnType = DxDForm.DxRepositoryEditorType.Label,
                    Label = labelText,
                    DesignBoundsExt = new DxDForm.RectangleExt(left, width, right, top, height, bottom)
                };
                result.Add(item);
            }
            void addItemType(string columnId, DxDForm.DxRepositoryEditorType columnType, string text, int? left, int? width, int? right, int? top, int? height, int? bottom, Action<DxDData.DataFormLayoutItem> modifier = null)
            {
                DxDData.DataFormLayoutItem item = new DxDData.DataFormLayoutItem()
                {
                    ColumnName = columnId,
                    ColumnType = columnType,
                    Label = text,
                    DesignBoundsExt = new DxDForm.RectangleExt(left, width, right, top, height, bottom)
                };
                modifier?.Invoke(item);
                result.Add(item);
            }
        }
        /// <summary>
        /// Vytvoří a vrátí jednotlivé řádky pro layout daného ID
        /// </summary>
        /// <param name="sampleId"></param>
        /// <returns></returns>
        private List<DxDData.DataFormRow> _CreateSampleRows(int sampleId)
        {
            var result = new List<DxDData.DataFormRow>();

            Random rand = new Random();

            int layoutId = (sampleId / 1000);
            int rowsCount = (sampleId % 1000);

            for (int r = 0; r < rowsCount; r++)
            {
                switch (layoutId)
                {
                    case 1:
                        addRow(r.ToString("000"), "datum;reference;nazev;pocet;cenacelk", "{dr};Ref {r};Název {r};{rnd};{rnd}");
                        break;
                    case 2:
                        addRow(r.ToString("000"), "datum;reference;nazev;pocet;cena1", "{dr};R{r};Záznam {r};{rnd};{rnd}");
                        break;
                    case 3:
                        addRow(r.ToString("000"), "id1;id2;id3;id4;id5;id7;id0", "{r};VYR;SKL;Výroba {r};Dne {dr};{t};{dr} ==> {rnd}");
                        break;
                }
            }

            return result;

            void addRow(string rowText, string columns = null, string values = null)
            {
                var dataRow = new DxDData.DataFormRow();
                string dat = DateTime.Now.ToString("d.M.yyyy");
                string tim = DateTime.Now.ToString("HH:mm");
                if (!String.IsNullOrEmpty(columns) && !String.IsNullOrEmpty(values))
                {
                    var cols = columns.Split(';');
                    var vals = values.Split(';');
                    if (cols.Length > 0 && cols.Length == vals.Length)
                    {
                        for (int c = 0; c < cols.Length; c++)
                        {
                            string col = cols[c];
                            string val = vals[c];
                            if (val.Contains("{r}")) val = val.Replace("{r}", rowText);
                            if (val.Contains("{d}")) val = val.Replace("{d}", dat);
                            if (val.Contains("{dr}")) val = val.Replace("{dr}", randomDate());
                            if (val.Contains("{t}")) val = val.Replace("{t}", tim);
                            if (val.Contains("{rnd}")) val = val.Replace("{rnd}", ((decimal)(rand.Next(10000, 1000000)) / 100m).ToString("### ##0.00").Trim());
                            dataRow.Content[col + DxDData.DxDataFormDef.ColumnDelimiter + DxDData.DxDataFormDef.Value] = val;
                        }
                    }
                }

                result.Add(dataRow);
            }

            // Náhodné datum -90 +30 dní ku dnešku
            string randomDate()
            {
                var dateRnd = DateTime.Now.AddDays(120d * rand.NextDouble() - 90d);
                return dateRnd.ToString("d.M.yyyy");
            }
        }
        #endregion
    }
}