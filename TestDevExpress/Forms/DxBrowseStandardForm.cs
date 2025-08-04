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
using TestDevExpress.Components;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář obsahující Ribbon a Browse
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "Browse", buttonOrder: 30, buttonImage: "svgimages/spreadsheet/chartgridlines.svg", buttonToolTip: "Otevře okno s ukázkou BrowseList standard DX testovací")]
    public class DxBrowseStandardForm : DxRibbonForm
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBrowseStandardForm()
        {
            this.CreateBrowse();
        }
        protected override void OnFirstShownBefore()
        {
            base.OnFirstShownBefore();
            TargetRowCount = 100;
            FillBrowse();
        }
        #endregion
        #region Ribbon a StatusBar - obsah a rozcestník
        /// <summary>
        /// Připraví obsah Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.Text = "Grid: připraveno";

            IsVirtualMode = true;
            TargetRowCount = 0;
            _SetCoefficients(0.85m, 1.00m);

            this.DxRibbon.DebugName = "BrowseRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.DxRibbon.ApplicationButtonText = " SYSTEM ";
            this.DxRibbon.LogActive = true;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();

            DataRibbonPage page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);

            DataRibbonGroup group = new DataRibbonGroup() { GroupId = "params", GroupText = "BROWSE TEST" };
            string resourceClear = "svgimages/spreadsheet/removetablerows.svg";
            string resourceVirtual1 = "svgimages/richedit/insertpagebreak.svg";
            string resourceAddIcon = "svgimages/richedit/addparagraphtotableofcontents.svg";
            string resourceBestFit = "svgimages/richedit/tableautofitwindow.svg";
            string resourceCoefficient1 = "svgimages/richedit/columnsone.svg";
            string resourceCoefficient2 = "svgimages/richedit/columnstwo.svg";
            string resourceCoefficient3 = "svgimages/richedit/columnsthree.svg";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Clear", Text = "Smaž řádky", ToolTipText = "Do Gridu vloží tabulku bez řádků", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", Checked = true, ImageName = resourceClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Virtual", Text = "Virtuální režim", ToolTipText = "Do Gridu vloží jen několik řádků, další doplňuje podle listování", ItemType = RibbonItemType.CheckButton, Checked = IsVirtualMode, ImageName = resourceVirtual1, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add10", Text = "Vlož 10", ToolTipText = "Do Gridu vloží 10 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add50", Text = "Vlož 50", ToolTipText = "Do Gridu vloží 50 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large});
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add100", Text = "Vlož 100", ToolTipText = "Do Gridu vloží 100 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add500", Text = "Vlož 500", ToolTipText = "Do Gridu vloží 500 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add1k", Text = "Vlož 1000", ToolTipText = "Do Gridu vloží 1 000 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add10k", Text = "Vlož 10000", ToolTipText = "Do Gridu vloží 10 000 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add100k", Text = "Vlož 100000", ToolTipText = "Do Gridu vloží 100 000 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add500k", Text = "Vlož 500000", ToolTipText = "Do Gridu vloží 500 000 řádek", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CountGroup", ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.BestFit", Text = "Uprav šířky", ToolTipText = "V Gridu upraví šířky sloupců podle jejich obsahu", ItemType = RibbonItemType.Button, ImageName = resourceBestFit, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Refresh", Text = "Refresh", ToolTipText = "Občas něco změní - odebere řádek, vymění hodnoty, přidá řádky", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large});
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.SetCoeffMin", Text = "Koeficienty MIN", ToolTipText = "Nastaví koeficienty přednačítání 0.25 : 0.75", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CoefficientGroup", ImageName = resourceCoefficient1, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.SetCoeffMid", Text = "Koeficienty MID", ToolTipText = "Nastaví koeficienty přednačítání 0.85 : 1.00", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CoefficientGroup", Checked = true, ImageName = resourceCoefficient2, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.SetCoeffMax", Text = "Koeficienty MAX", ToolTipText = "Nastaví koeficienty přednačítání 1.25 : 2.00", ItemType = RibbonItemType.CheckButton, RadioButtonGroupName = "CoefficientGroup", ImageName = resourceCoefficient3, RibbonStyle = RibbonItemStyles.Large });

            page.Groups.Add(group);

            pages.Add(page);
            this.DxRibbon.AddPages(pages, true);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.Clear":
                    TargetRowCount = 0;
                    FillBrowse();
                    break;
                case "Dx.Test.Virtual":
                    this.IsVirtualMode = e.Item.Checked ?? false;
                    break;
                case "Dx.Test.Add10":
                    TargetRowCount = 10;
                    FillBrowse();
                    break;
                case "Dx.Test.Add50":
                    TargetRowCount = 50;
                    FillBrowse();
                    break;
                case "Dx.Test.Add100":
                    TargetRowCount = 100;
                    FillBrowse();
                    break;
                case "Dx.Test.Add500":
                    TargetRowCount = 500;
                    FillBrowse();
                    break;
                case "Dx.Test.Add1k":
                    TargetRowCount = 1000;
                    FillBrowse();
                    break;
                case "Dx.Test.Add10k":
                    TargetRowCount = 10000;
                    FillBrowse();
                    break;
                case "Dx.Test.Add100k":
                    TargetRowCount = 100000;
                    FillBrowse(Randomizer.WordBookType.TaborSvatych);
                    break;
                case "Dx.Test.Add500k":
                    TargetRowCount = 500000;
                    FillBrowse(Randomizer.WordBookType.CampOfSaints);
                    break;
                case "Dx.Test.BestFit":
                    BestFitBrowse();
                    break;
                case "Dx.Test.Refresh":
                    GridRefresh();
                    break;
                case "Dx.Test.SetCoeffMin":
                    _SetCoefficients(0.25m, 0.75m);
                    break;
                case "Dx.Test.SetCoeffMid":
                    _SetCoefficients(0.85m, 1.00m);
                    break;
                case "Dx.Test.SetCoeffMax":
                    _SetCoefficients(1.25m, 2.00m);
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
        public string StatusText
        { 
            get 
            {
                return __StatusText;
            } 
            set 
            {
                __StatusText = value;
                if (_StatusItemTitle != null) 
                    _StatusItemTitle.Caption = value; 
            }
        }
        private string __StatusText;
        #endregion
        #region Browse
        private DevExpress.XtraGrid.GridSplitContainer _GridContainer;
        private DevExpress.XtraGrid.GridControl _Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView _View;
        private System.Data.DataTable _DataSource;
        /// <summary>
        /// Vytvoří objekt Browse a dá mu základní vzhled.
        /// Vloží tabulku s daty s daným počtem řádků, default = 0 (tzn. budou tam jen sloupce!).
        /// </summary>
        protected void CreateBrowse(int rowCount = 0)
        {
            var timeStart = DxComponent.LogTimeCurrent;

            _GridContainer = new DevExpress.XtraGrid.GridSplitContainer();
            _GridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _GridContainer.Initialize();
            _GridContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            var timeInit = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            this.DxMainPanel.Controls.Add(_GridContainer);
            var grid = _GridContainer.Grid;    // new DevExpress.XtraGrid.GridControl();
            _Grid = grid;
            // _Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            var timeAdd = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Specify a data source:
            _DataSource = _CreateGridDataTable();
            _Grid.DataSource = _DataSource;
            // string dataLog = FillData(rowCount, Random.WordBookType.TriMuziNaToulkach);
            string dataLog = AddDataRows(0);

            timeStart = DxComponent.LogTimeCurrent;
            var view = grid.AvailableViews["GridView"].CreateView(grid) as DevExpress.XtraGrid.Views.Grid.GridView;
            _View = view;

            view.VertScrollTipFieldName = "nazev";
            view.ScrollStyle = DevExpress.XtraGrid.Views.Grid.ScrollStyleFlags.LiveHorzScroll | DevExpress.XtraGrid.Views.Grid.ScrollStyleFlags.LiveVertScroll;
            view.VertScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Always;
            view.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFullFocus;

            view.OptionsFind.AlwaysVisible = true;
            view.OptionsView.ShowAutoFilterRow = true;
            view.OptionsView.ShowGroupPanelColumnsAsSingleRow = true;             // to je dobrý!
            //   view.OptionsView.ShowPreview = true;    preview je přidaný řádek pod každý řádek s daty

            view.OptionsDetail.DetailMode = DevExpress.XtraGrid.Views.Grid.DetailMode.Embedded;
            view.OptionsBehavior.Editable = false;
            view.OptionsBehavior.SmartVertScrollBar = true;
            view.OptionsScrollAnnotations.ShowCustomAnnotations = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsScrollAnnotations.ShowFocusedRow = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsScrollAnnotations.ShowSelectedRows = DevExpress.Utils.DefaultBoolean.True;
            view.CustomScrollAnnotation += View_CustomScrollAnnotation;

            // RowFilter
            view.SubstituteFilter += View_SubstituteFilter;

            // Zkoušky
            view.OptionsCustomization.UseAdvancedCustomizationForm = DevExpress.Utils.DefaultBoolean.True;

            // Ošetřit v době plnění daty:
            view.TopRowChanged += View_TopRowChanged;
            view.RowCountChanged += View_RowCountChanged;

            // ScrollBar:
            view.CalcRowHeight += View_CalcRowHeight;
            view.CustomDrawScroll += View_CustomDrawScroll;

            grid.MainView = view;
            var timeCreateView = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Resize columns according to their values:
            timeStart = DxComponent.LogTimeCurrent;
            view.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // EditStyle for "status":
            PrepareEditStyleForStatus(view);

            // CustomDraw jako CodeTable:
            view.CustomDrawCell += View_CustomDrawCell;

            StatusText = $"Tvorba GridSplitContainer: {timeInit} sec;     Přidání na Form: {timeAdd} sec;     {dataLog}Generování View: {timeCreateView} sec;     BestFitColumns: {timeFitColumns} sec";
        }
        private void View_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (e.Column.FieldName == "status")
            {
                StatusCodeTable.DrawStatusCellCodeTable(sender, e);
            }
        }
        #region Řádkový filtr, konverze do MS SQL, Custom convertor pro editační styly
        private void View_SubstituteFilter(object sender, DevExpress.Data.SubstituteFilterEventArgs e)
        {
            var filter = e.Filter;
            var dxExpression = "DX: " + filter?.ToString();
            var msExpression = "SQL: " + TestDevExpress.AsolDX.News.DxToExpressionConvertor.ConvertToString(filter, AsolDX.News.DxExpressionLanguageType.MsSqlDatabase, this._RowFilterHandler);
            this.StatusText = dxExpression + "  |◘◘|◘◘|  " + msExpression;
        }
        /// <summary>
        /// Zdejší metoda je volána vždy, když konvertor filtru DevExpress to MsSql konvertuje jednotlivou operaci ve filtru.
        /// Metoda dostává v argumentu typ operace a data operace (operandy).<br/>
        /// Na základě typu operace a operandů může najít sloupec, a pokud sloupec bude implementovat editační styl (Code = Value), pak metoda může zadanou podmínku vyřešit jinak anebo s jinými daty.<br/>
        /// Pokud například na vstupu je sloupec "StavDokladu", a podmínka je == "Zaúčtováno", a my víme, že sloupec v šabloně s ColumnId = "StavDokladu" zobrazuje editační styl,
        /// pak změníme podmínku tak, aby v podmínce nebyl výraz editačního stylu:
        /// <code>(case dokl.status when '1' then 'Pořízeno' when '2' then 'Zaúčtováno' when '3' then 'Stornováno' else 'Jiný' end) = 'Zaúčtováno'</code>
        /// ale jednodušší podmínka na základě datové hodnoty:
        /// <code>dokl.status = '2'</code>
        /// <para/>
        /// <b><u>Praktická realizace:</u></b> 
        /// <list type="number">
        /// <item>Detekovat typ operace <see cref="TestDevExpress.AsolDX.News.DxExpressionCustomArgs.Operation"/></item>
        /// <item>Podle typu operace určit, zda ji můžeme řešit pro sloupec s editačním stylem</item>
        /// <item>Podle typu operace určit, ve kterém operandu očekáváme sloupec</item>
        /// <item>V poli operandů <see cref="TestDevExpress.AsolDX.News.DxExpressionCustomArgs.Operands"/> najít sloupec a najít jeho data v naší evidenci (my víme, co v kterém sloupci zobrazujeme)</item>
        /// <item>Pokud sloupec obsahuje editační styl, pak vyřešit operaci na úrovni datových hodnot CodeValue a nikoli DisplayValue</item>
        /// <item>Přeformulovat operaci</item>
        /// <item>Buď vytvořit zcela novou podmínku a uložit ji do <see cref="TestDevExpress.AsolDX.News.DxExpressionCustomArgs.CustomResult"/></item>
        /// <item>Anebo jen nahradit zdroj dat (sloupec s DisplayValue za CoeValue) a nahradit hodnoty v dalších operandech (<see cref="TestDevExpress.AsolDX.News.DxExpressionCustomArgs.Operands"/>) podobným způsobem</item>
        /// </list>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _RowFilterHandler(object sender, TestDevExpress.AsolDX.News.DxExpressionCustomArgs args)
        {
            if (_RowFilterIsForCodeValue(args, out var columnInfo))
                _RowFilterHandlerColumnEditStyle(sender, args, columnInfo);
        }
        private bool _RowFilterIsForCodeValue(TestDevExpress.AsolDX.News.DxExpressionCustomArgs args, out IColumnInfo columnInfo)
        {
            columnInfo = null;
            switch (args.Operation)
            {   // Tyto operace budeme řešit, jiné neumíme:
                case AsolDX.News.DxExpressionOperationType.Binary_Equal:
                case AsolDX.News.DxExpressionOperationType.Binary_NotEqual:
                case AsolDX.News.DxExpressionOperationType.In:
                    // Umíme je řešit tehdy, když první operand obsahuje sloupec, a tento sloupec má editační styl; a další operandy jsou ValueString:
                    if (isFirstOperandColumnEditStyle(out  columnInfo) && isNextOperandsValueString())
                        return true;
                    break;
            }
            // Daná operace řádkového filtru nebude řešena pomocí editačního stylu:
            return false;

            // Vrátí true, pokud operand [0] je SLoupec, který známe a který má Editační styl
            bool isFirstOperandColumnEditStyle(out IColumnInfo colInfo)
            {
                colInfo = null;
                // Operand [0] musí existovat, musí to být Sloupec, musíme ten sloupec najít, a musí mít editační styl:
                return (args.Operands != null && args.Operands.Count >= 1 && args.Operands[0].IsPropertyName && _TryFindColumn(args.Operands[0].PropertyName, out colInfo) && colInfo.HasEditStyle);
            }
            // Vrátí true, pokud všechny operandy počínaje indexem 1 jsou stringová hodnota (=odpovídá DisplayValue)
            // Vrátí false, pokud pole operandů nemá 2 (a více) operandů, anebo některý není ValueString
            bool isNextOperandsValueString()
            {
                int count = args.Operands?.Count ?? 0;
                if (count > 1)
                {
                    for (int i = 1; i < count; i++)
                    {   // Počínaje indexem [1], protože na [0] je Sloupec !
                        // Pokud Value není String, pak false = nelze konvertovat např. proměnnou na CodeValue:
                        if (!args.Operands[i].IsValueString) return false;
                    }
                    return true;
                }
                // Není dostatek operandů:
                return false;
            }
        }
        private void _RowFilterHandlerColumnEditStyle(object sender, TestDevExpress.AsolDX.News.DxExpressionCustomArgs args, IColumnInfo columnInfo)
        {
            switch (args.Operation)
            {   // Tyto operace budeme řešit, jiné neumíme:
                case AsolDX.News.DxExpressionOperationType.Binary_Equal:
                case AsolDX.News.DxExpressionOperationType.Binary_NotEqual:
                case AsolDX.News.DxExpressionOperationType.In:
                    // Pro tyto operace neměníme formulaci výrazu, jen vyměníme sloupec a hodnoty:
                    //  Namísto:    columnDisplay = "Zaúčtováno"           Namísto:    columnDisplay IN ("Zaúčtováno", "Stornováno")
                    //  Vložíme:    columnCode    = 2                      Vložíme:    columnCode    IN (2,            3)
                    foreach (var operand in args.Operands)
                    {
                        if (operand.IsPropertyName)
                            operand.PropertyName = columnInfo.ColumnSourceValue;
                        else if (operand.IsValue)
                            operand.Value = searchCodeValue(operand.Value);
                    }
                    break;
            }


            // Zadanou hodnotu (pokud je String) vyhledá v položkách editačního stylu aktuálního sloupce (columnInfo) a vrátí její odpovídajcí CodeValue. Pokud nenajde, vracívstupní hodnotu.
            object searchCodeValue(object displayValue)
            {
                if (displayValue is string displayText && columnInfo.EditStyleValues.TryFindFirst(out var editStyleItem, kvp => String.Equals(kvp.Value, displayText, StringComparison.Ordinal)))
                    return editStyleItem.Key;
                return displayValue;
            }
        }
        private bool _TryFindColumn(string columnId, out IColumnInfo columnInfo)
        {
            // Toto je sample metoda, reálně se musí napojit na reálné sloupce šablony !!!
            if (String.Equals(columnId, "period", StringComparison.OrdinalIgnoreCase))
            {
                if (__PeriodColumn is null)
                {
                    var periodColumn = new IColumnInfo()
                    {
                        ColumnId = "Period",
                        ColumnSourceDisplay = "case tab01.recper when 202501 then '2025-01' when 202502 then '2025-02' when 202501 then '2025-03' else '' end",
                        ColumnSourceValue = "tab01.recper",
                        HasEditStyle = true
                    };

                    var values = new List<KeyValuePair<object, string>>();
                    for (int yr = 2020; yr <= 2030; yr++)
                        for (int mo = 1; mo <= 12; mo++)
                            values.Add(new KeyValuePair<object, string>(100 * yr + mo, $"{yr}-{mo:D2}"));
                    periodColumn.EditStyleValues = values.ToArray();

                    __PeriodColumn = periodColumn;
                }
                columnInfo = __PeriodColumn;
                return true;
            }
            columnInfo = null;
            return false;
        }
        private IColumnInfo __PeriodColumn;
        internal class IColumnInfo      // měl by to být interface
        {
            /// <summary>
            /// Alias sloupce
            /// </summary>
            public string ColumnId;
            /// <summary>
            /// Zdroj dat ve sloupci: DisplayText, typicky výraz:
            /// <code>(case dokl.status when '1' then 'Pořízeno' when '2' then 'Zaúčtováno' when '3' then 'Stornováno' else 'Jiný' end) = 'Zaúčtováno'</code>
            /// </summary>
            public string ColumnSourceDisplay;
            /// <summary>
            /// Zdroj dat ve sloupci: CodeValue, typicky sloupec tabulky:
            /// <code>dokl.status</code>
            /// </summary>
            public string ColumnSourceValue;
            /// <summary>
            /// Obsahuje true, pokud tento sloupec zobrazuje editační styl
            /// </summary>
            public bool HasEditStyle;
            /// <summary>
            /// Položky editačního stylu: Key = CodeValue; Value = DisplayText
            /// </summary>
            public KeyValuePair<object, string>[] EditStyleValues;
        }
        #endregion
        #region Buňka s ImageComboBox jako CodeTable
        private void PrepareEditStyleForStatus(DevExpress.XtraGrid.Views.Grid.GridView view)
        {
            // Testovací definice editačního stylu:
            string[] displayValues = new string[]
            {
                "Akorát", "Beze všeho", "Co byste ještě chtěli", "Děkujeme", "Extra přídavek", "Fakturovat", "Grupování", "Hotovo"
            };
            string[] iconNames = new string[]
            {
    "images/scales/bluewhitered_16x16.png",
    "images/scales/geenyellow_16x16.png",
    "images/scales/greenwhite_16x16.png",
    "images/scales/greenwhitered_16x16.png",
    "images/scales/greenyellowred_16x16.png",
    "images/scales/redwhite_16x16.png",
    "images/scales/redwhiteblue_16x16.png",
    "images/scales/redwhitegreen_16x16.png",
    "images/scales/redyellowgreen_16x16.png",
    "images/scales/whitegreen_16x16.png",
    "images/scales/whitered_16x16.png",
    "images/scales/yellowgreen_16x16.png"
            };
            Color[] backColors = new Color[]
            {
                Color.FromArgb(255, 210, 255), Color.FromArgb(255, 210, 255), Color.FromArgb(255, 255, 210), Color.FromArgb(255, 255, 210),
                Color.FromArgb(210, 255, 255), Color.FromArgb(210, 255, 255), Color.FromArgb(210, 255, 210), Color.FromArgb(210, 255, 210)
            };
            Color backColor2 = Color.White;
            Color textColor = Color.Black;

            // Standardní instance ComboCodeTable:
            StatusCodeTable = new ComboCodeTable
            (
                new ComboCodeTable.Item("A", displayValues[0], iconNames[0], backColors[0], backColor2, textColor),
                new ComboCodeTable.Item("B", displayValues[1], iconNames[1], backColors[1], backColor2, textColor),
                new ComboCodeTable.Item("C", displayValues[2], iconNames[2], backColors[2], backColor2, textColor),
                new ComboCodeTable.Item("D", displayValues[3], iconNames[3], backColors[3], backColor2, textColor),
                new ComboCodeTable.Item("E", displayValues[4], iconNames[4], backColors[4], backColor2, textColor),
                new ComboCodeTable.Item("F", displayValues[5], iconNames[5], backColors[5], backColor2, textColor),
                new ComboCodeTable.Item("G", displayValues[6], iconNames[6], backColors[6], backColor2, textColor),
                new ComboCodeTable.Item("H", displayValues[7], iconNames[7], backColors[7], backColor2, textColor)
            );

            // Styl buňky s hodnotou, která není null ale není součástí editačního stylu:
            StatusCodeTable.NotFoundItemStyle = new ComboCodeTable.Item(null, null, null, Color.FromArgb(255, 160, 192), textStyle: FontStyle.Italic);

            DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox repoCombo = StatusCodeTable.CreateRepositoryCombo();
            _Grid.RepositoryItems.Add(repoCombo);
            var colStatus = view.Columns["status"];
            colStatus.ColumnEdit = repoCombo;
            colStatus.Tag = StatusCodeTable;
        }
        /// <summary>
        /// Úložiště editačního stylu
        /// </summary>
        private ComboCodeTable StatusCodeTable;
        #endregion
        #region Scrollbar a Auto LoadNext
        /// <summary>
        /// Cílový počet řádků, null = bez omezení
        /// </summary>
        protected int TargetRowCount
        {
            get { return __TargetRowCount; }
            set
            {
                if (value != __TargetRowCount)
                {
                    __TargetRowCount = value;
                    _RefreshFormTitle();
                }
            }
        }
        private int __TargetRowCount = 0;
        /// <summary>
        /// Je aktivní virtuální režim = do Gridu načítám jen omezený počet řádků najednou
        /// </summary>
        protected bool IsVirtualMode
        {
            get { return __IsVirtualMode; }
            set
            {
                if (value != __IsVirtualMode)
                {
                    __IsVirtualMode = value;
                    _RefreshFormTitle();
                }
            }
        }
        private bool __IsVirtualMode = false;

        /// <summary>
        /// Aktuální počet řádků
        /// </summary>
        protected int CurrentRowCount 
        {
            get { return _DataSource?.Rows.Count ?? 0; }
        }
        /// <summary>
        /// Jsou načteny všechny řádky?
        /// </summary>
        protected bool HasAllRows
        {
            get { return (CurrentRowCount >= __TargetRowCount); }
        }
        /// <summary>
        /// Čekám na donačtení dalších řádků => nebudu žádat o nové...
        /// </summary>
        protected bool WaitForNextRows
        {
            get { return __WaitForNextRows; }
            set
            {
                if (value != __WaitForNextRows)
                {
                    __WaitForNextRows = value;
                }
                _RefreshFormTitle();
            }
        }
        private bool __WaitForNextRows = false;
        /// <summary>
        /// Aktualizuje titulek okna podle počtu řádků
        /// </summary>
        private void _RefreshFormTitle()
        {
            if (this.IsHandleCreated && !this.Disposing && !this.IsDisposed)
            {
                string text = $"Grid: načteno {CurrentRowCount} z {TargetRowCount} požadovaných " + (HasAllRows ? "= načteno vše." : "...") + (WaitForNextRows ? " [čekáme na další]" : "") + $"   Koeficienty: {ReserveRatio}:{RequestRatio}";
                this.Text = text;
            }
        }
        private void View_CalcRowHeight(object sender, DevExpress.XtraGrid.Views.Grid.RowHeightEventArgs e)
        {
            _CurrentRowHeight = e.RowHeight;
        }
        /// <summary>
        /// Posledně známá výška jednoho řádku. Určuje se dříve, než se kreslí ScrollBar.
        /// </summary>
        private int _CurrentRowHeight = 22;
        /// <summary>
        /// Kolik řádků mohu aktuálně zobrazit
        /// </summary>
        private int _CurrentMaxVisibleRows
        {
            get
            {
                int visibleHeight = _View.ViewRect.Height - 8;
                int rowHeight = (_CurrentRowHeight > 12 ? _CurrentRowHeight : 22);
                return (visibleHeight / rowHeight) + 2;
            }
        }
        /// <summary>
        /// Proces kreslení ScrollBaru využijeme k zajištění donačítání řádků OnDemand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void View_CustomDrawScroll(object sender, DevExpress.XtraEditors.ScrollBarCustomDrawEventArgs e)
        {
            if (e.Info.ScrollBar.ScrollBarType == DevExpress.XtraEditors.ScrollBarType.Vertical && !HasAllRows && !WaitForNextRows)
            {   // Pro svislý ScrollBar, pokud nemáme načtené všechny řádky?
                decimal reserveRatio = ReserveRatio;                 // Kolik "obrazovek" dolů pod gridem chci mít přednačteno
                decimal requestRatio = RequestRatio;                 // Když už budu donačítat další řádky, o kolik obrazovek si mám požádat? (pokud to volající povoluje)
                var firstVisible = e.Info.ScrollBar.Value;
                var countVisible1 = e.Info.ScrollBar.LargeChange;
                var countVisible2 = _CurrentMaxVisibleRows;
                var countVisible = (countVisible1 > countVisible2) ? countVisible1 : countVisible2;          // Počet zobrazitelných řádků
                var totalRange = new DecimalRange(e.Info.ScrollBar.Minimum, e.Info.ScrollBar.Maximum + 1);   // Rozsah všech řádků gridu od prvního do posledního (včetně: Maximum obsahuje index posledního řádku, nikoliv Next)
                var visibleRange = new DecimalRange(firstVisible, firstVisible + countVisible);              // Rozsah právě zobrazených řádků ve viditelném prostoru
                var endTrigger = totalRange.End - reserveRatio * visibleRange.Size;                          // Jakmile už uvidím tento řádek (endTrigger), budu chtít další data

                if (totalRange.Size < visibleRange.Size || visibleRange.End >= endTrigger)                   // Jakmile mám celkem méně řádků, než mohu zobrazit, anebo pokud aktuálně viditelný poslední řádek dosáhne hranice pro donačtení, požádáme o další řádky
                {
                    int loadCount = (int)(requestRatio * countVisible);                                      // Požádám o tolik řádků = relativně k viditelné ploše
                    int minTotalCount = (int)((decimal)countVisible * (1m + reserveRatio));                  // Nejméně tolik, abych jedním načtením viděl i potřebnou dolní rezervu
                    int missingCount = minTotalCount - e.Info.ScrollBar.Maximum + 5;
                    if (missingCount > 0 && loadCount < missingCount) loadCount = missingCount;
                    WaitForNextRows = true;
                    this.AddDataRowsAsync(loadCount);
                }
            }
        }
        /// <summary>
        /// Nastaví koeficienty
        /// </summary>
        /// <param name="reserveRatio"></param>
        /// <param name="requestRatio"></param>
        private void _SetCoefficients(decimal reserveRatio, decimal requestRatio)
        {
            __ReserveRatio = reserveRatio;
            __RequestRatio = requestRatio;
            _RefreshFormTitle();
        }
        /// <summary>
        /// Kolik "obrazovek" dolů pod gridem chci mít přednačteno
        /// </summary>
        protected decimal ReserveRatio
        {
            get { return __ReserveRatio; }
            set
            {
                if (value != __ReserveRatio)
                {
                    __ReserveRatio = value;
                    _RefreshFormTitle();
                }
            }
        }
        private decimal __ReserveRatio = 1m;
        /// <summary>
        /// Když už budu donačítat další řádky, o kolik obrazovek si mám požádat? (pokud to volající povoluje)
        /// </summary>
        protected decimal RequestRatio
        {
            get { return __RequestRatio; }
            set
            {
                if (value != __RequestRatio)
                {
                    __RequestRatio = value;
                    _RefreshFormTitle();
                }
            }
        }
        private decimal __RequestRatio = 1m;
        private void View_CustomScrollAnnotation(object sender, DevExpress.XtraGrid.Views.Grid.GridCustomScrollAnnotationsEventArgs e)
        {
            e.Annotations = new List<DevExpress.XtraGrid.Views.Grid.GridScrollAnnotationInfo>();
            e.Annotations.Add(new DevExpress.XtraGrid.Views.Grid.GridScrollAnnotationInfo() { Index = 32, Color = System.Drawing.Color.DarkBlue, RowHandle = 32 });
            e.Annotations.Add(new DevExpress.XtraGrid.Views.Grid.GridScrollAnnotationInfo() { Index = 480, Color = System.Drawing.Color.Violet, RowHandle = 480 });
        }
        private void View_TopRowChanged(object sender, EventArgs e)
        {
        }
        private void View_RowCountChanged(object sender, EventArgs e)
        {
        }
        #endregion
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        /// <param name="wordBookType"></param>
        private void FillBrowse(Randomizer.WordBookType? wordBookType = null)
        {
            if (wordBookType.HasValue) __WordBookType = wordBookType.Value;

            int targetCount = this.TargetRowCount;
            if (targetCount <= 0)
            {
                if (CurrentRowCount > 0)
                    _DataSource.Rows.Clear();
                _NextRowId = 0;
            }
            else
            {
                int currentCount = this.CurrentRowCount;
                int remainingCount = targetCount - currentCount;
                bool isVirtualMode = this.IsVirtualMode;
                if (isVirtualMode && remainingCount > 50)
                    remainingCount = 5;
                if (remainingCount > 0)
                {
                    string dataLog = AddDataRows(remainingCount);
                    StatusText = dataLog;
                }
            }

            _RefreshFormTitle();
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private string AddDataRowsAsync(int rowCount)
        {
            ThreadManager.AddAction(() => AddDataRowsBgr(rowCount));
            return "Background...";
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private void AddDataRowsBgr(int rowCount)
        {
            int wait = Randomizer.Rand.Next(250, 600);
            System.Threading.Thread.Sleep(wait);                     // Něco jako uděláme...  - doba pro krátké čekání na data ze serveru
            this.RunInGui(() => AddDataRows(rowCount));              // Naplnění dáme do GUI
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="requestRowCount"></param>
        /// <returns></returns>
        private string AddDataRows(int requestRowCount)
        {
            if (requestRowCount <= 0)
            {
                WaitForNextRows = false;
                return "";
            }

            int currentRowCount = this.CurrentRowCount;
            int targetRowCount = this.TargetRowCount;
            if (targetRowCount <= 0 || currentRowCount >= targetRowCount)
            {
                WaitForNextRows = false;
                return "";
            }
            int remainingCount = targetRowCount - currentRowCount;
            if (requestRowCount > remainingCount) requestRowCount = remainingCount;

            var timeStart = DxComponent.LogTimeCurrent;
            var rows = _CreateDataRows(requestRowCount);
            var timeCreateData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // nevolat event View_RowCountChanged o změně počtu řádků:
            _Grid.BeginInit();
            _Grid.SuspendLayout();
            timeStart = DxComponent.LogTimeCurrent;
            foreach (var row in rows)
                _DataSource.Rows.Add(row);
            _Grid.ResumeLayout();
            _Grid.EndInit();

            WaitForNextRows = false;
            _RefreshFormTitle();

            var timeSetData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);
            string logText = $"Generování řádků [{requestRowCount}]: {timeCreateData} sec;     Vložení DataTable do Gridu: {timeSetData} sec;     ";
            return logText;
        }
        /// <summary>
        /// Vytvoří a vrátí data pro hlavní tabulku, bez řádků
        /// </summary>
        /// <returns></returns>
        private System.Data.DataTable _CreateGridDataTable()
        {
            System.Data.DataTable table = new System.Data.DataTable();

            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "id", Caption = "ID", DataType = typeof(int) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "refer", Caption = "Reference", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "nazev", Caption = "Název", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "category", Caption = "Kategorie", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "status_code", Caption = "Status", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "status", Caption = "Stav", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "period", Caption = "Období", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_inp", Caption = "Datum vstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_out", Caption = "Datum výstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "quantity", Caption = "Počet kusů", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_unit", Caption = "Cena jednotková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_total", Caption = "Cena celková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "note", Caption = "Poznámka", DataType = typeof(string) });

            return table;
        }
        /// <summary>
        /// Vytvoří a vrátí řádky pro hlavní tabulku
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private List<object[]> _CreateDataRows(int rowCount)
        {
            List<object[]> rows = new List<object[]>();

            var currWords = Randomizer.ActiveWordBook;
            Randomizer.ActiveWordBook = __WordBookType;

            int? targetRowCount = this.TargetRowCount;
            int currCount = _DataSource.Rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                int id = currCount + i + 1;
                if (targetRowCount.HasValue && id > targetRowCount.Value)
                {
                    break;
                }
                rows.Add(_CreateNewDataRow());
            }

            Randomizer.ActiveWordBook = currWords;

            return rows;
        }
        private object[] _CreateNewDataRow()
        {
            int rowId = ++_NextRowId;
            return _CreateDataRow(rowId);
        }
        private object[] _CreateDataRow(int rowId)
        {
            string refer = "DL:" + Randomizer.Rand.Next(100000, 1000000).ToString();
            string nazev = Randomizer.GetSentence(1, 3, false);
            string category = Randomizer.GetItem(Categories);

            string status = Randomizer.GetItem(Statuses);
            if (Randomizer.IsTrue(5)) status = null;                     // Testujeme i hodnotu NULL
            else if (Randomizer.IsTrue(4)) status = "X";                 // Testujeme i hodnotu mimo CodeTable

            DateTime dateInp = DateFirst.AddDays(Randomizer.Rand.Next(0, 730));
            DateTime dateOut = dateInp.AddDays(Randomizer.Rand.Next(7, 90));
            string period = dateInp.Year.ToString() + "-" + dateInp.Month.ToString("00");
            decimal qty = (decimal)(Randomizer.Rand.Next(10, 1000)) / 10m;
            decimal price1 = (decimal)(Randomizer.Rand.Next(10, 10000)) / 10m;
            decimal priceT = qty * price1;
            string note = Randomizer.GetSentence(5, 9, true);

            object[] row = new object[] { rowId, refer, nazev, category, status, status, period, dateInp, dateOut, qty, price1, priceT, note };
            return row;
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
        /// Refresh dat
        /// </summary>
        private void GridRefresh()
        {
            var source = _DataSource;
            Dictionary<int, object[]> changes = _CreateDataChanges(source.Rows.Count);

            int cnt = this._View.DataRowCount;
            var firstVisibleIndex = this._View.TopRowIndex;
            int focusedRowIndex = this._View.GetFocusedDataSourceRowIndex();
            int focusedRowVisibleOffset = focusedRowIndex - firstVisibleIndex;


            var firstVisibleId = (firstVisibleIndex >= 0 && firstVisibleIndex < cnt) ? source.Rows[firstVisibleIndex][0] : null;
            var focusedRowId = (focusedRowIndex >= 0 && focusedRowIndex < cnt) ? source.Rows[focusedRowIndex][0] : null;


            // Handle se odvolává na total index, kdežto VisibleIndex pracuje jen nad filtrovanými řádky.
            // VisibleIndex ale nijak nepracuje s reálně viditelnými řádky v CurrentVisibleArea.
            int visInd = this._View.GetVisibleIndex(this._View.FocusedRowHandle);

            var dh = this._Grid.FocusedView.DetailHeight;          // 350 = nejde o plochu dat

            var focusRow = _View.GetFocusedDataRow();

            if (this._View.FocusedRowHandle >= 0)
            {
                int vi = _View.GetVisibleIndex(this._View.FocusedRowHandle);
            }

            _ApplyDataChanges(source, changes, ref focusedRowIndex);


            firstVisibleIndex = focusedRowIndex - focusedRowVisibleOffset;
            this._View.TopRowIndex = firstVisibleIndex;
            int newRowHandle = this._View.GetRowHandle(focusedRowIndex);
            this._View.FocusedRowHandle = newRowHandle;

        }
        /// <summary>
        /// Vytvoří a vrátí Dictionary obsahující změny dat pro cílovou tabulku se stávajícím daným počtem vět
        /// </summary>
        /// <param name="currCount"></param>
        /// <returns></returns>
        private Dictionary<int, object[]> _CreateDataChanges(int currCount)
        {
            Dictionary<int, object[]> changes = new Dictionary<int, object[]>();

            int deleteCount = 0;
            int modifyCount = 0;
            int appendCount = 0;

            // Některé řádky smažeme:
            if (currCount >= 20)
            {
                deleteCount = (currCount <= 60 ? currCount / 10 : currCount / 15);       // currCount je dozajista 20 a více. Pokud je do 60, pak deleteCount je 2 až 6; pro větší je 4 a více (1/15 = 6.66%)
                deleteCount = Randomizer.Rand.Next(1, deleteCount);                          // deleteCount je 1 až 10% počtu pro malé seznamy, nebo 1 až 6.66% pro velké...
                for (int i = 0; i < deleteCount; i++)
                {
                    int index = Randomizer.Rand.Next(0, currCount);                          // Index řádku k vymazání
                    if (!changes.ContainsKey(index))                                     // Když index ještě není použit, přidáme jej; Value NULL = odebrat
                        changes.Add(index, null);
                    else
                        i--;                                                             // Pokud náhodný index už v seznamu máme, dáme si další pokus...
                }
            }

            // Některé řádky modifikujeme:
            if (currCount >= 20)
            {
                modifyCount = (currCount <= 60 ? currCount / 10 : currCount / 15);       // currCount je dozajista 20 a více. Pokud je do 60, pak modifyCount je 2 až 6; pro větší je 4 a více (1/15 = 6.66%)
                modifyCount = Randomizer.Rand.Next(1, modifyCount);                          // modifyCount je 1 až 10% počtu pro malé seznamy, nebo 1 až 6.66% pro velké...
                for (int i = 0; i < modifyCount; i++)
                {
                    int index = Randomizer.Rand.Next(0, currCount);                          // Index řádku k modifikování
                    if (!changes.ContainsKey(index))                                     // Když index ještě není použit, přidáme jej; Value dostane ID = (index + 1)
                        changes.Add(index, _CreateDataRow(index + 1));
                    else
                        i--;                                                             // Pokud náhodný index už v seznamu máme, dáme si další pokus...
                }
            }

            // Několik řádků přidáme:   přidáme přesně tolik řádků, kolik jsme jich smazali!
            // appendCount = Random.Rand.Next(10, 20 + (currCount / 20));
            appendCount = deleteCount;
            int rowId = currCount;
            for (int i = 0; i < appendCount; i++)
            {
                changes.Add(rowId++, _CreateNewDataRow());
            }

            return changes;
        }
        /// <summary>
        /// Do dané tabulky aplikuje dodané změny
        /// </summary>
        /// <param name="source"></param>
        /// <param name="changes"></param>
        /// <param name="focusedRowIndex">Index řádku s focusem v rámci tabulky.
        /// Pokud tento řádek v rámci aplikace změn smažu, měl bych do této proměnné vložit nejbližší vyšší řádek. 
        /// Pokud smažu řádky nižší, měl bych zmenšit tento index, aby ukazoval na stále tentýž řádek.
        /// Na výstupu by měla být hodnota 0 až (RowCount-1).</param>
        private void _ApplyDataChanges(System.Data.DataTable source, Dictionary<int, object[]> changes, ref int focusedRowIndex)
        {
            int currCount = source.Rows.Count;

            // Modifikovat řádky (provést před smazáním, aby platily indexy): ty, které mají index menší než Count, a které mají data:
            var modifyRows = changes.Where(c => c.Key < currCount && c.Value != null).ToList();
            modifyRows.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var modifyRow in modifyRows)
                source.Rows[modifyRow.Key].ItemArray = modifyRow.Value;

            // Smazat řádky (setřídit podle indexu sestupně): ty, které mají index menší než Count, a které nemají data:
            var deleteRows = changes.Where(c => c.Value is null).ToList();
            deleteRows.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (var deleteRow in deleteRows)
            {
                int deleteIndex = deleteRow.Key;
                if (deleteIndex == focusedRowIndex) focusedRowIndex++;
                else if (deleteIndex < focusedRowIndex) focusedRowIndex++;
                source.Rows.RemoveAt(deleteIndex);
            }

            // Přidat nové řádky (v pořadí jejich indexu): ty, které mají index menší než výchozí Count, a které mají data:
            var appendRows = changes.Where(c => c.Key >= currCount && c.Value != null).ToList();
            appendRows.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var appendRow in appendRows)
                source.Rows.Add(appendRow.Value);

            int rowCount = source.Rows.Count;
            if (focusedRowIndex >= rowCount) focusedRowIndex = rowCount - 1;
            if (focusedRowIndex < 0) focusedRowIndex = (rowCount == 0 ? -1 : 0);
        }
        private Randomizer.WordBookType __WordBookType = Randomizer.WordBookType.TriMuziNaToulkach;
        private string[] Categories
        {
            get
            {
                if (_Categories is null)
                    _Categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
                return _Categories;
            }
        }
        private string[] _Categories = null;
        private string[] Statuses
        {
            get
            {
                if (_Statuses is null)
                    _Statuses = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
                return _Statuses;
            }
        }
        private string[] _Statuses = null;

        private DateTime DateFirst
        {
            get
            {
                if (!_DateFirst.HasValue)
                    _DateFirst = new DateTime(DateTime.Now.Year, 1, 1);
                return _DateFirst.Value;
            }
        }
        private DateTime? _DateFirst = null;

        private int _NextRowId = 0;
        #endregion
    }




    /// <summary>
    /// Formulář obsahující Ribbon a Browse
    /// </summary>
    public class DxBrowseStandardFormOld : DxRibbonForm
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBrowseStandardFormOld()
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

            page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "BROWSE TEST" };
            page.Groups.Add(group);
            string resourceClear = "svgimages/spreadsheet/removetablerows.svg";
            string resourceAddIcon = "svgimages/richedit/addparagraphtotableofcontents.svg";
            string resourceBestFit = "svgimages/richedit/tableautofitwindow.svg";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Clear", Text = "Smaž řádky", ToolTipText = "Do Gridu vloží tabulku bez řádků", ItemType = RibbonItemType.Button, ImageName = resourceClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add1k", Text = "Vlož 100", ToolTipText = "Do Gridu vloží 1 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add10k", Text = "Vlož 10000", ToolTipText = "Do Gridu vloží 10 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add100k", Text = "Vlož 100000", ToolTipText = "Do Gridu vloží 100 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add500k", Text = "Vlož 500000", ToolTipText = "Do Gridu vloží 500 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.BestFit", Text = "Uprav šířky", ToolTipText = "V Gridu upraví šířky sloupců podle jejich obsahu", ItemType = RibbonItemType.Button, ImageName = resourceBestFit, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.AlignGroupSummaryInGroupRow", Text = "Group summary in group row", ToolTipText = "Typ zobrazení sumy v groupě", ItemType = RibbonItemType.CheckBoxStandard,RibbonStyle = RibbonItemStyles.Large });

            pages.Add(page);
            this.DxRibbon.AddPages(pages, true);

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
                    FillBrowse(100, Randomizer.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add10k":
                    FillBrowse(10000, Randomizer.WordBookType.TriMuziNaToulkach);
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
                case "Dx.Test.AlignGroupSummaryInGroupRow":
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
        /// <summary>
        /// Vytvoří objekt Browse a dá mu základní vzhled.
        /// Vloží tabulku s daty s daným počtem řádků, default = 0 (tzn. budou tam jen sloupce!).
        /// </summary>
        protected void CreateBrowse(int rowCount = 0)
        {
            var timeStart = DxComponent.LogTimeCurrent;

            _GridContainer = new DevExpress.XtraGrid.GridSplitContainer();
            _GridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _GridContainer.Initialize();
            _GridContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            _GridContainer.SynchronizeFocusedRow = DevExpress.Utils.DefaultBoolean.True;
            //((DevExpress.XtraGrid.Views.Grid.GridView)_GridContainer.SplitChildGrid.MainView).OptionsFind.AlwaysVisible = false; //do handleru po vytvoření splitu -> najit handler


            var timeInit = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            this.DxMainPanel.Controls.Add(_GridContainer);
            var grid = _GridContainer.Grid;    // new DevExpress.XtraGrid.GridControl();
            _Grid = grid;
            // _Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            var timeAdd = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Specify a data source:
            string dataLog = FillData(rowCount, Randomizer.WordBookType.TriMuziNaToulkach);

            timeStart = DxComponent.LogTimeCurrent;
            var view = grid.AvailableViews["GridView"].CreateView(grid) as DevExpress.XtraGrid.Views.Grid.GridView;
            _View = view;
            view.OptionsFind.AlwaysVisible = true;
            view.OptionsDetail.DetailMode = DevExpress.XtraGrid.Views.Grid.DetailMode.Embedded;
            view.OptionsView.ShowAutoFilterRow = true;
            view.FilterPopupExcelCustomizeTemplate += OnFilterPopupExcelCustomizeTemplate;

            grid.MainView = view;
            var timeCreateView = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            //---Groupování TEST
            view.OptionsMenu.ShowGroupSummaryEditorItem = true;
            view.OptionsMenu.ShowSummaryItemMode = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsMenu.EnableFooterMenu = true;
            view.OptionsMenu.EnableGroupRowMenu = true;
            view.OptionsView.GroupFooterShowMode = DevExpress.XtraGrid.Views.Grid.GroupFooterShowMode.VisibleAlways;


            //view.OptionsBehavior.AlignGroupSummaryInGroupRow = DefaultBoolean.True; Schovává groupfooter (sumy jsou v grupovacím řádku)
            view.OptionsBehavior.AlignGroupSummaryInGroupRow = DevExpress.Utils.DefaultBoolean.False;

            //----

            // Resize columns according to their values:
            timeStart = DxComponent.LogTimeCurrent;
            view.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            StatusText = $"Tvorba GridSplitContainer: {timeInit} sec;     Přidání na Form: {timeAdd} sec;     {dataLog}Generování View: {timeCreateView} sec;     BestFitColumns: {timeFitColumns} sec";
        }

        void OnFilterPopupExcelCustomizeTemplate(object sender, DevExpress.XtraGrid.Views.Grid.FilterPopupExcelCustomizeTemplateEventArgs e)
        {
            e.Template.BackColor = System.Drawing.Color.FromArgb(0, 0, 120);
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
                int id = i + 1;
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
                table.Rows.Add(id, refer, nazev, category, period, dateInp, dateOut, qty, price1, priceT, note);
            }

            Randomizer.ActiveWordBook = currWords;

            return table;
        }
        #endregion
    }
}
