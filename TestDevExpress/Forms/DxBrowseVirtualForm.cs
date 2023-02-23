using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors.Filtering.Templates;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář obsahující Ribbon a Browse Virtual
    /// </summary>
    public class DxBrowseVirtualForm : DxRibbonForm
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBrowseVirtualForm()
        {
            this.CreateBrowse();
        }
        #endregion
        #region Ribbon a StatusBar - obsah a rozcestník
        /// <summary>
        /// Připraví obsah Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.DxRibbon.DebugName = "BrowseRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.DxRibbon.ApplicationButtonText = " SYSTEM ";
            this.DxRibbon.LogActive = true;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ", MergeOrder = 1, PageOrder = 1 };
            pages.Add(page);

            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: false) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "BROWSE TEST" };
            page.Groups.Add(group);
            string resourceClear = "svgimages/spreadsheet/removetablerows.svg";
            string resourceAddIcon = "svgimages/richedit/addparagraphtotableofcontents.svg";
            string resourceBestFit = "svgimages/richedit/tableautofitwindow.svg";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Clear", Text = "Smaž řádky", ToolTipText = "Do Gridu vloží tabulku bez řádků", ItemType = RibbonItemType.Button, ImageName = resourceClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add1k", Text = "Vlož 1000", ToolTipText = "Do Gridu vloží 1 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.BestFit", Text = "Uprav šířky", ToolTipText = "V Gridu upraví šířky sloupců podle jejich obsahu", ItemType = RibbonItemType.Button, ImageName = resourceBestFit, RibbonStyle = RibbonItemStyles.Large });

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.Clear":
                    FillBrowse(0);
                    break;
                case "Dx.Test.Add1k":
                    FillBrowse(1000, Randomizer.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.BestFit":
                    BestFitBrowse();
                    break;
            }
        }
        /// <summary>
        /// Připraví obsah StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this._StatusItemTitle = CreateStatusBarItem();

            // V tomto pořadí budou StatusItemy viditelné (tady je zatím jen jeden):
            this.DxStatusBar.ItemLinks.Add(this._StatusItemTitle);
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
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        /// <summary>
        /// Text ve status baru
        /// </summary>
        public string StatusText { get { return _StatusItemTitle?.Caption; } set { if (_StatusItemTitle != null) _StatusItemTitle.Caption = value; } }
        #endregion
        #region Browse
        private DevExpress.XtraGrid.GridSplitContainer _GridContainer;
        private DevExpress.XtraGrid.GridControl _Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView _View;
        private DevExpress.Data.VirtualServerModeSource _VirtualDataSource;
        /// <summary>
        /// Vytvoří objekt Browse a dá mu základní vzhled.
        /// Vloží tabulku s daty s daným počtem řádků, default = 0 (tzn. budou tam jen sloupce!).
        /// </summary>
        protected void CreateBrowse(int rowCount = 0)
        {
            var timeStart = DxComponent.LogTimeCurrent;

            // https://docs.devexpress.com/CoreLibraries/DevExpress.Data.VirtualServerModeSource
            // https://docs.devexpress.com/CoreLibraries/DevExpress.Data.VirtualServerModeSource.AcquireInnerList
            // https://docs.devexpress.com/CoreLibraries/DevExpress.Data.VirtualServerModeSource.CanPerformColumnServerAction
            // 
            _VirtualDataSource = new DevExpress.Data.VirtualServerModeSource();
            _VirtualDataSource.RowType = null;   // typeof(Product);
            _VirtualDataSource.AcquireInnerList += _VirtualDataSource_AcquireInnerList;
            _VirtualDataSource.ConfigurationChanged += _VirtualDataSource_ConfigurationChanged;
            _VirtualDataSource.MoreRows += _VirtualDataSource_MoreRows;

            _GridContainer = new DevExpress.XtraGrid.GridSplitContainer();
            _GridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _GridContainer.Initialize();
            _GridContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            var timeInit = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            this.DxMainPanel.Controls.Add(_GridContainer);
            var grid = _GridContainer.Grid;
            _Grid = grid;
            _Grid.DataSource = _VirtualDataSource;

            // _Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            var timeAdd = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Specify a data source:
            string dataLog = FillData(rowCount, Randomizer.WordBookType.TriMuziNaToulkach);

            timeStart = DxComponent.LogTimeCurrent;
            var view = grid.AvailableViews["GridView"].CreateView(grid) as DevExpress.XtraGrid.Views.Grid.GridView;
            _View = view;
            view.OptionsFind.AlwaysVisible = true;
            view.OptionsDetail.DetailMode = DevExpress.XtraGrid.Views.Grid.DetailMode.Embedded;
            grid.MainView = view;
            var timeCreateView = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Resize columns according to their values:
            timeStart = DxComponent.LogTimeCurrent;
            view.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            StatusText = $"Tvorba GridSplitContainer: {timeInit} sec;     Přidání na Form: {timeAdd} sec;     {dataLog}Generování View: {timeCreateView} sec;     BestFitColumns: {timeFitColumns} sec";
        }

        private void _VirtualDataSource_AcquireInnerList(object sender, DevExpress.Data.VirtualServerModeAcquireInnerListEventArgs e)
        {
            e.ClearAndAddRowsFunc = _VirtualDataSource_ClearAndAddRowsFunc;
            e.AddMoreRowsFunc = _VirtualDataSource_AddMoreRowsFunc;
            e.InnerList = new System.Collections.ArrayList();
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.ReleaseAction = _VirtualDataSource_ReleaseAction;
        }
        private System.Collections.IList _VirtualDataSource_ClearAndAddRowsFunc(System.Collections.IList list1, System.Collections.IEnumerable list2)
        {
            return null;
        }
        private System.Collections.IList _VirtualDataSource_AddMoreRowsFunc(System.Collections.IList list1, System.Collections.IEnumerable list2)
        {
            return null;
        }
        private void _VirtualDataSource_ReleaseAction(System.Collections.IList list1)
        {
        }

        private void _VirtualDataSource_ConfigurationChanged(object sender, DevExpress.Data.VirtualServerModeRowsEventArgs e)
        {

        }

        private void _VirtualDataSource_MoreRows(object sender, DevExpress.Data.VirtualServerModeRowsEventArgs e)
        {

        }

        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        private void FillBrowse(int rowCount, Randomizer.WordBookType wordBookType = Randomizer.WordBookType.TriMuziNaToulkach)
        {
            string dataLog = FillData(rowCount, wordBookType);
            StatusText = dataLog;
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        private void BestFitBrowse()
        {
            var timeStart = DxComponent.LogTimeCurrent;
            _View.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);
            StatusText = $"BestFitColumns: {timeFitColumns} sec";
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private string FillData(int rowCount, Randomizer.WordBookType wordBookType = Randomizer.WordBookType.TriMuziNaToulkach)
        {
            return "";





            var timeStart = DxComponent.LogTimeCurrent;
            var data = _CreateGridDataTable(rowCount, wordBookType);
            var timeCreateData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            _Grid.DataSource = data;
            var timeSetData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            string logText = $"Generování DataTable[{rowCount}]: {timeCreateData} sec;     Vložení DataTable do Gridu: {timeSetData} sec;     ";
            return logText;
        }
        /// <summary>
        /// Vytvoří a vrátí data pro hlavní tabulku
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private System.Data.DataTable _CreateGridDataTable(int rowCount, Randomizer.WordBookType wordBookType = Randomizer.WordBookType.TriMuziNaToulkach)
        {
            var currWords = Randomizer.ActiveWordBook;
            Randomizer.ActiveWordBook = wordBookType;

            System.Data.DataTable table = new System.Data.DataTable();

            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "id", Caption = "ID", DataType = typeof(int) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "refer", Caption = "Reference", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "nazev", Caption = "Název", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "category", Caption = "Kategorie", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "period", Caption = "Období", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_inp", Caption = "Datum vstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_out", Caption = "Datum výstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "quantity", Caption = "Počet kusů", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_unit", Caption = "Cena jednotková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_total", Caption = "Cena celková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "note", Caption = "Poznámka", DataType = typeof(string) });

            string[] categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
            int year = DateTime.Now.Year;
            DateTime dateBase = new DateTime(year, 1, 1);
            for (int i = 0; i < rowCount; i++)
            {
                var cells = _CreateGridTableCells(i + 1, categories, dateBase);
                table.Rows.Add(cells);
            }

            Randomizer.ActiveWordBook = currWords;

            return table;
        }
        private object[] _CreateGridTableCells()
        {
            string[] categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
            int year = DateTime.Now.Year;
            DateTime dateBase = new DateTime(year, 1, 1);
            return _CreateGridTableCells(0, categories, dateBase);
        }
        private object[] _CreateGridTableCells(int id, string[] categories, DateTime dateBase)
        {
            string refer = "DL:" + Randomizer.Rand.Next(100000, 1000000).ToString();
            string nazev = Randomizer.GetSentence(1, 3, false);
            string category = Randomizer.GetItem(categories);
            DateTime dateInp = dateBase.AddDays(Randomizer.Rand.Next(0, 730));
            DateTime dateOut = dateInp.AddDays(Randomizer.Rand.Next(7, 90));
            string period = dateInp.Year.ToString() + "-" + dateInp.Month.ToString("00");
            decimal qty = (decimal)(Randomizer.Rand.Next(10, 1000)) / 10m;
            decimal price1 = (decimal)(Randomizer.Rand.Next(10, 10000)) / 10m;
            decimal priceT = qty * price1;
            string note = Randomizer.GetSentence(5, 9, true);
            return new object[] { id, refer, nazev, category, period, dateInp, dateOut, qty, price1, priceT, note };
        }
        #endregion
    }

}
