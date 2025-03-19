using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    public partial class DxBrowseForm : DxRibbonForm
    {
        public DxBrowseForm()
        {
            this.CreateSplitContainer();
            this.CreateFilterStringTextBox();
            this.CreateDataColumns();
            this.CreateBrowse();
        }

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

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.Basic);
            pages.Add(page);

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "BROWSE TEST" };
            page.Groups.Add(group);
            string resourceClear = "svgimages/spreadsheet/removetablerows.svg";
            string resourceChange = "office2013/data/editdatasource_32x32.png";
            string resourceAddIcon = "svgimages/richedit/addparagraphtotableofcontents.svg";
            string resourceBestFit = "svgimages/richedit/tableautofitwindow.svg";
            string resourceHorizontalSplit = "svgimages/diagramicons/flipimage_horizontal.svg";
            string resourceVerticalSplit = "svgimages/diagramicons/flipimage_vertical.svg";
            string resourceHideSplit = "svgimages/icon%20builder/actions_fullscreen.svg";
            string resourceApplyRowFilter = "office2013/filter/masterfilter_32x32.png";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Clear", Text = "Smaž řádky", ToolTipText = "Do Gridu vloží tabulku bez řádků", ItemType = RibbonItemType.Button, ImageName = resourceClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.ChangeDataSource", Text = "Změň zdroj dat", ToolTipText = "Přepíná mezi zdroji dat", ItemType = RibbonItemType.Button, ImageName = resourceChange, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add1k", Text = "Vlož 1000", ToolTipText = "Do Gridu vloží 1 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add10k", Text = "Vlož 10000", ToolTipText = "Do Gridu vloží 10 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add100k", Text = "Vlož 100000", ToolTipText = "Do Gridu vloží 100 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add500k", Text = "Vlož 500000", ToolTipText = "Do Gridu vloží 500 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.BestFit", Text = "Uprav šířky", ToolTipText = "V Gridu upraví šířky sloupců podle jejich obsahu", ItemType = RibbonItemType.Button, ImageName = resourceBestFit, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.HorizontalSplit", Text = "Horintální rozdělení", ToolTipText = "Grid rozdělí na gridy nad sebou", ItemType = RibbonItemType.Button, ImageName = resourceHorizontalSplit, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.VerticalSplit", Text = "Vertikální rozdělení", ToolTipText = "Grid rozdělí na gridy vedle sebe", ItemType = RibbonItemType.Button, ImageName = resourceVerticalSplit, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.HideSplit", Text = "Odebrat rozdělení", ToolTipText = "Zruší rozdělení gridu", ItemType = RibbonItemType.Button, ImageName = resourceHideSplit, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.ApplyRowFilter", Text = "Aplikovat řádkový filtr", ToolTipText = "Aplikuje testovací řádkový filtr", ItemType = RibbonItemType.Button, ImageName = resourceApplyRowFilter, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.EnableRowFilterCore", Text = "Použít interní filtrování", ToolTipText = "Použit interní filtrovací mechanismu gridu", ItemType = RibbonItemType.CheckBoxToggle, Checked = DxComponent.UhdPaintEnabled, ClickAction = SetRowFilterCoreEnabled });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.EnableColumnSortCore", Text = "Použít interní řazení", ToolTipText = "Použit interní řadícího mechanismu gridu", ItemType = RibbonItemType.CheckBoxToggle, Checked = true, ClickAction = SetColumnSortCoreEnabled });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.CheckBoxRowSelect", Text = "CheckBox row select", ToolTipText = "Použít checkBox pro select řádků", ItemType = RibbonItemType.CheckBoxToggle, Checked = DxComponent.UhdPaintEnabled, ClickAction = SetCheckBoxRowSelect });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.AlignGroupSummaryInGroupRow", Text = "Group summary in group row", ToolTipText = "Typ zobrazení sumy v groupě", ItemType = RibbonItemType.CheckBoxStandard, ClickAction = SetAlignGroupSummaryInGroupRow });


            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.Clear":
                    FillBrowse(0);
                    break;
                case "Dx.Test.Add1k":
                    FillBrowse(1000, Randomizer.WordBookType.TriMuziVeClunu);
                    break;
                case "Dx.Test.Add10k":
                    FillBrowse(10000, Randomizer.WordBookType.TriMuziNaToulkach, true);
                    break;
                case "Dx.Test.Add100k":
                    FillBrowse(100000, Randomizer.WordBookType.TaborSvatych);
                    break;
                case "Dx.Test.Add500k":
                    FillBrowse(500000, Randomizer.WordBookType.CampOfSaints);
                    break;
                case "Dx.Test.BestFit":
                    BestFitBrowse();
                    break;
                case "Dx.Test.HorizontalSplit":
                    this._GridContainer.ShowHorizontalSplit();
                    break;
                case "Dx.Test.VerticalSplit":
                    this._GridContainer.ShowVerticalSplit();
                    break;
                case "Dx.Test.HideSplit":
                    this._GridContainer.HideSplitView();
                    break;
                case "Dx.Test.ChangeDataSource":
                    this.ChangeDataSource();
                    break;
                case "Dx.Test.ApplyRowFilter":
                    this.ApplyRowFilter();
                    break;
            }
        }

        private void SetRowFilterCoreEnabled(IMenuItem menuItem)
        {
            _View.RowFilterCoreEnabled = (menuItem?.Checked ?? false);
            ApplyRowFilter();
        }

        private void SetColumnSortCoreEnabled(IMenuItem menuItem)
        {
            _View.ColumnSortCoreEnabled = (menuItem?.Checked ?? false);
        }
        private void SetCheckBoxRowSelect(IMenuItem menuItem)
        {
            _View.CheckBoxRowSelect = (menuItem?.Checked ?? false);
        }

        private void SetAlignGroupSummaryInGroupRow(IMenuItem menuItem)
        {
            _View.AlignGroupSummaryInGroupRow = (menuItem?.Checked ?? false);
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

        #region SplitContainer
        DxSplitContainerControl _SplitContainer;

        private void CreateSplitContainer()
        {
            _SplitContainer = DxComponent.CreateDxSplitContainer(this.DxMainPanel, null, DockStyle.Fill, Orientation.Horizontal, DevExpress.XtraEditors.SplitFixedPanel.Panel1, 100, showSplitGlyph: true);
        }

        #endregion

        #region FilterString

        private DxMemoEdit _FilterString;

        private void CreateFilterStringTextBox()
        {
            _FilterString = new DxMemoEdit();
            _FilterString.Dock = DockStyle.Fill;
            _SplitContainer.Panel1.Controls.Add(_FilterString);
        }

        #endregion

        #region Browse

        private List<DataColumn> _BrowseDataColumns = new List<DataColumn>();
        private int _DataSourceIndex = 1;

        private DxGridSplitContainer _GridContainer;
        private DxGridControl _Grid;
        private DxGridView _View;

        /// <summary>
        /// Vytvoří objekt Browse a dá mu základní vzhled.
        /// Vloží tabulku s daty s daným počtem řádků, default = 0 (tzn. budou tam jen sloupce!).
        /// </summary>
        protected void CreateBrowse(int rowCount = 0)
        {
            var timeStart = DxComponent.LogTimeCurrent;

            _GridContainer = new DxGridSplitContainer();
            _GridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _GridContainer.Initialize();
            _GridContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;

            //((DevExpress.XtraGrid.Views.Grid.GridView)_GridContainer.SplitChildGrid.MainView).OptionsFind.AlwaysVisible = false; //do handleru po vytvoření splitu -> najit handler


            var timeInit = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            //this.DxMainPanel.Controls.Add(_GridContainer);
            this._SplitContainer.Panel2.Controls.Add(_GridContainer);
            //DxGridControl grid = new DxGridControl();
            var grid = _GridContainer.Grid as DxGridControl;    // new DevExpress.XtraGrid.GridControl();
            _Grid = grid;
            // _GridContainer.Grid = _Grid;
            //_GridContainer.Grid as DxGridControl;    // new DevExpress.XtraGrid.GridControl();

            // _Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            var timeAdd = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);



            timeStart = DxComponent.LogTimeCurrent;

            //DxGridView view = grid.AvailableViews["DxGridView"].CreateView(grid) as DxGridView;
            DxGridView view = grid.MainView as DxGridView;  //máme DxGridView zaregistrován jako mainView při vytváření, proto již nemusím vytvářet přes createView...

            _View = view;
            view.OptionsDetail.DetailMode = DevExpress.XtraGrid.Views.Grid.DetailMode.Embedded;
            view.OptionsView.ShowAutoFilterRow = true;
            view.OptionsView.ColumnAutoWidth = false;   //zobrazí horizontální scrollbar když je třeba
            view.OptionsBehavior.AutoPopulateColumns = false;
            view.OptionsView.ShowFooter = true;

            view.OptionsFind.AlwaysVisible = false;
            view.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;

            view.OptionsScrollAnnotations.ShowSelectedRows = DevExpress.Utils.DefaultBoolean.True;  // Anotace . https://docs.devexpress.com/WindowsForms/120556/controls-and-libraries/data-grid/scrolling/scrollbar-annotations

            var gvColumnData = CreateGridViewColumnData();
            // view.InitColumns(gvColumnData);
            InitColumnsSummary(gvColumnData);


            RegisterViewEvents();

            // Specify a data source:
            // string dataLog = FillData(rowCount, Randomizer.WordBookType.TriMuziNaToulkach);

            // grid.MainView = view;
            var timeCreateView = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Resize columns according to their values:
            timeStart = DxComponent.LogTimeCurrent;
            view.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            //StatusText = $"Tvorba GridSplitContainer: {timeInit} sec;     Přidání na Form: {timeAdd} sec;     {dataLog}Generování View: {timeCreateView} sec;     BestFitColumns: {timeFitColumns} sec";

            //zkouška image do Header - Změněno na internal -> přesun do initHEader
            //Dictionary<string, string> headerImage = new Dictionary<string, string>();
            //headerImage.Add("id", "svgimages/icon%20builder/actions_fullscreen.svg");
            //headerImage.Add("nazev", "svgimages/icon%20builder/actions_fullscreen.svg");
            //view.SetColumnHeaderImage(headerImage);
            view.ViewCaption = "Testovací data";
            view.ViewCaptionHeight = 30;

            _View.ColumnSortCoreEnabled = true;
        }

        private void RegisterViewEvents()
        {
            // _View.FilterRowChanged += _View_FilterRowChanged;
            _View.DxDoubleClick += _View_DxDoubleClick;
            _View.DxKeyDown += _View_DxKeyDown;
        }

        private void _View_DxKeyDown(object sender, KeyEventArgs e)
        {
            // ENTER in Data region
            if (!e.Handled && !e.Alt && !e.Control && !e.Shift
                && e.KeyCode == System.Windows.Forms.Keys.Enter && !this._View.IsFilterRowActive)
            {
                //OnEnter - call server
                MessageBox.Show("KeyDown Enter");
            }
        }

        private void _View_DxDoubleClick(object sender, DxGridDoubleClickEventArgs args)
        {
            bool modifierKeyCtrl = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control;
            bool modifierKeyAlt = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt;
            bool modifierKeyShift = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift;
            StringBuilder strBld = new StringBuilder();
            strBld.AppendLine($"DoubleClicked");
            strBld.AppendLine($"RowId: {args.RowId} ColumnName: {args.ColumnName}");
            strBld.AppendLine($"Control key: {modifierKeyCtrl}");
            strBld.AppendLine($"Alt key: {modifierKeyAlt}");
            strBld.AppendLine($"Shift key: {modifierKeyShift}");
            MessageBox.Show(strBld.ToString());
        }

        //private void _View_FilterRowChanged(object sender, DxGridFilterRowChangeEventArgs e)
        //{
        //    _FilterString.Text = e.FilterString;
        //}

        private void CreateDataColumns()
        {
            _BrowseDataColumns.Clear();
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "id", Caption = "ID", DataType = typeof(int) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "refer", Caption = "Reference", DataType = typeof(string) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "nazev", Caption = "Název", DataType = typeof(string) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "category", Caption = "Kategorie", DataType = typeof(string) });

            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "period", Caption = "Období", DataType = typeof(string) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "date_inp", Caption = "Datum vstupu", DataType = typeof(DateTime) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "date_out", Caption = "Datum výstupu", DataType = typeof(DateTime) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "quantity", Caption = "Počet kusů", DataType = typeof(decimal) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "price_unit", Caption = "Cena jednotková", DataType = typeof(decimal) });
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "price_total", Caption = "Cena celková", DataType = typeof(decimal) });
            if (_DataSourceIndex == 1)
            {
                _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "date_sell", Caption = "Datum prodeje", DataType = typeof(DateTime) });
                _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "note", Caption = "Poznámka", DataType = typeof(string) });
            }
            else if (_DataSourceIndex == 2)
            {
                _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "extranote", Caption = "Extra poznámka", DataType = typeof(string) });
                _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "date_sell", Caption = "Datum prodeje", DataType = typeof(DateTime) });
            }
            _BrowseDataColumns.Add(new System.Data.DataColumn() { ColumnName = "category_img", Caption = "KategorieImg", DataType = typeof(string) });


        }

        /// <summary>
        /// Jen pro počáteční testování summary. Než bude udělaná podpora v DXGrid
        /// </summary>
        /// <param name="gvColumnData"></param>
        private void InitColumnsSummary(List<GridViewColumnData> gvColumnData)
        {
            foreach (var item in gvColumnData)
            {
                if (TypeHelper.IsNumeric(item.ColumnType))
                {
                    DevExpress.XtraGrid.GridGroupSummaryItem sumItem = new DevExpress.XtraGrid.GridGroupSummaryItem()
                    {
                        FieldName = item.FieldName,
                        SummaryType = DevExpress.Data.SummaryItemType.Sum,
                        ShowInGroupColumnFooter = _View.Columns[item.FieldName]
                    };
                    _View.GroupSummary.Add(sumItem);
                }
            }
        }

        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        private void FillBrowse(int rowCount, Randomizer.WordBookType wordBookType = Randomizer.WordBookType.TriMuziNaToulkach, bool addToDatasource = false)
        {
            string dataLog = FillData(rowCount, wordBookType, addToDatasource);
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
        private string FillData(int rowCount, Randomizer.WordBookType wordBookType = Randomizer.WordBookType.TriMuziNaToulkach, bool addToDatasource = false)
        {
            var timeStart = DxComponent.LogTimeCurrent;
            var data = _CreateGridDataTable(rowCount, wordBookType);
            var timeCreateData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            if (addToDatasource)
            {
                //TODO Experimentální s přídavkem...
                var dt = _Grid.DataSource as DataTable;
                if (dt != null)
                {
                    int i = 0;
                    foreach (var column in dt.Columns)
                    {
                        data.Columns[i].DataType = column.GetType();
                        i++;
                    }

                    //dt.NewRow
                    foreach (var item in data.Rows)
                    {
                        dt.Rows.Add(item);
                    }

                }
            }
            else
            {
                _Grid.DataSource = data;
            }
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

            CreateDataColumns();
            table.Columns.AddRange(_BrowseDataColumns.ToArray());   //Vytvoří sloupce dle společné definice

            string[] categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
            int year = DateTime.Now.Year;
            DateTime dateBase = new DateTime(year, 1, 1);
            for (int i = 0; i < rowCount; i++)
            {
                int id = i + 1;
                string refer = "DL:" + Randomizer.Rand.Next(100000, 1000000).ToString();
                string nazev = Randomizer.GetSentence(1, 3, false);
                string category = Randomizer.GetItem(categories);
                DateTime dateInp = dateBase.AddDays(Randomizer.Rand.Next(0, 730));
                DateTime dateOut = dateInp.AddDays(Randomizer.Rand.Next(7, 90));
                DateTime dateSell = dateInp.AddDays(Randomizer.Rand.Next(7, 90));
                string period = dateInp.Year.ToString() + "-" + dateInp.Month.ToString("00");
                decimal qty = (decimal)(Randomizer.Rand.Next(10, 1000)) / 10m;
                decimal price1 = (decimal)(Randomizer.Rand.Next(10, 10000)) / 10m;
                decimal priceT = qty * price1;
                string note = Randomizer.GetSentence(5, 9, true);

                if (_DataSourceIndex == 1)
                {
                    table.Rows.Add(id, refer, nazev, category, period, dateInp, dateOut, qty, price1, priceT, dateSell, note, category);
                }
                else if (_DataSourceIndex == 2)
                {
                    table.Rows.Add(id, refer, nazev, category, period, dateInp, dateOut, qty, price1, priceT, note, dateSell, category);
                }
            }

            Randomizer.ActiveWordBook = currWords;

            return table;
        }

        private List<GridViewColumnData> CreateGridViewColumnData()
        {
            //Vytvořit data
            var result = new List<GridViewColumnData>();
            int i = 0;
            foreach (var column in _BrowseDataColumns)
            {
                var cd = new GridViewColumnData(column.ColumnName, column.Caption, column.DataType, i);
                result.Add(cd);
                ++i;
            }

            result[2].AllowSort = false;
            result[2].ToolTipTitle = "Vypnuto řazení";
            result[2].ToolTipText = "Dlouhý text...";

            //result[3].AllowFilter = false;
            //result[3].ToolTip = "Vypnuto Filtrování";

            result[4].IconName = "svgimages/icon%20builder/actions_fullscreen.svg";
            //result[4].IconName = "pic_0/bar_s/aktivni-plocha-16x16.png";
            result[4].IconAligment = StringAlignment.Far;
            result[4].ToolTipTitle = "Ikonka";

            result[5].Width = 200;
            result[5].ToolTipTitle = "200px široký sloupec";
            result[5].HeaderFontStyle = (System.Drawing.FontStyle)DxComponent.ConvertFontStyle(true, true, true, true);
            result[5].HeaderAlignment = DevExpress.Utils.HorzAlignment.Far;



            var categoryColumn = result[3];

            categoryColumn.CodeTable.Add(("Nákup", "NÁKUP", ""));
            categoryColumn.CodeTable.Add(("Prodej", "PRODEJ", ""));
            categoryColumn.CodeTable.Add(("Sklad", "SKLAD", ""));
            categoryColumn.CodeTable.Add(("Tuzemsko", "TUZEMSKO", ""));
            categoryColumn.CodeTable.Add(("Export", "EXPORT", ""));
            categoryColumn.CodeTable.Add(("Import", "IMPORT", ""));


            //a.CodeTable.Add(("Nakup", "NAKUP", "svgimages/icon%20builder/actions_fullscreen.svg"));
            //a.CodeTable.Add(("Prodej", "PRODEJ", "svgimages/icon%20builder/actions_fullscreen.svg"));
            //a.CodeTable.Add(("Sklad", "SKLAD", "svgimages/icon%20builder/actions_fullscreen.svg"));
            //a.CodeTable.Add(("Tuzemsko", "TUZEMSKO", "svgimages/icon%20builder/actions_fullscreen.svg"));
            //a.CodeTable.Add(("Export", "EXPORT", "svgimages/icon%20builder/actions_fullscreen.svg"));
            //a.CodeTable.Add(("Import", "IMPORT", "svgimages/icon%20builder/actions_fullscreen.svg"));

            //var categoryImgColumn = result.FirstOrDefault(x => x.FieldName == "category_img");
            //categoryImgColumn.EditStyleViewMode = EditStyleViewMode.Icon;

            //categoryImgColumn.CodeTable.Add(("Nákup", "NÁKUP", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            //categoryImgColumn.CodeTable.Add(("Prodej", "PRODEJ", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            //categoryImgColumn.CodeTable.Add(("Sklad", "SKLAD", "pic_0/bar_s/databasedisconnected-16x16.png"));
            //categoryImgColumn.CodeTable.Add(("Tuzemsko", "TUZEMSKO", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            //categoryImgColumn.CodeTable.Add(("Export", "EXPORT", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            //categoryImgColumn.CodeTable.Add(("Import", "IMPORT", "pic_0/bar_s/aktivni-plocha-16x16.png"));

            var categoryImgColumn = result.FirstOrDefault(x => x.FieldName == "category_img");
            categoryImgColumn.EditStyleViewMode = EditStyleViewMode.Icon;

            categoryImgColumn.CodeTable.Add(("NÁKUP", "NÁKUP", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            categoryImgColumn.CodeTable.Add(("PRODEJ", "PRODEJ", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            categoryImgColumn.CodeTable.Add(("SKLAD", "SKLAD", "pic_0/bar_s/databasedisconnected-16x16.png"));
            categoryImgColumn.CodeTable.Add(("TUZEMSKO", "TUZEMSKO", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            categoryImgColumn.CodeTable.Add(("EXPORT", "EXPORT", "pic_0/bar_s/aktivni-plocha-16x16.png"));
            categoryImgColumn.CodeTable.Add(("IMPORT", "IMPORT", "pic_0/bar_s/aktivni-plocha-16x16.png"));

            //categoryImgColumn.CodeTable.Add(("NÁKUP", "NÁKUP", ""));
            //categoryImgColumn.CodeTable.Add(("PRODEJ", "PRODEJ", ""));





            //result[6].Visible = false;    // jen v ctoru (private set)


            return result;
            //vytpnout automatické vytváření columns
            //Zpracovat jejich vytvoření columns
            //        bind dataTable
        }

        private void ChangeDataSource()
        {
            MoveDataSourceIndex();
            CreateDataColumns();
            //na zakladě indexu generuj jinou dataTable.. test na to až přijdou nová data ze serveru (změna šablony)
            _Grid.DataSource = null;

            // _View.InitColumns(CreateGridViewColumnData());
        }

        private void MoveDataSourceIndex()
        {
            _DataSourceIndex = _DataSourceIndex < 2 ? ++_DataSourceIndex : 1;
        }

        private void ApplyRowFilter()
        {
            //_View.ActiveFilterCriteria// = DevExpress.Data.Filtering.CriteriaOperator()
            //criteriea
            //_View.ActiveFilterString = "Contains([refer], 'dl') And IsOutlookIntervalTomorrow([date_inp])";
            //_View.ActiveFilterString = "Contains([refer], 'dl') And EndsWith([refer], '6') And Not Contains([note], 'xy') And [date_sell] Between(#2021-09-01#, #2022-07-31#) And [price_total] Is Not Null";
            //DAtaset
            //_View.ActiveFilterString = "(([refer] liKE '%dl%') And (([date_inp] >= #04/14/2022#) And ([date_inp] < #04/15/2022#)))";
            //sql
            //_View.ActiveFilterString = "((isnuLL(CharIndEX(N'dl', \"refer\"), 0) > 0) And ((\"date_inp\" >= convert(datetime, '2022-04-14 00:00:00.000', 121)) And (\"date_inp\" < convert(datetime, '2022-04-15 00:00:00.000', 121))))";

            _View.ActiveFilterString = _FilterString.Text;
        }

        #endregion
    }
}
