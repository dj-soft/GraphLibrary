using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.DirectX.Common;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Filtering.Internal;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Drawing;
using DevExpress.XtraGrid.Localization;
using DevExpress.XtraGrid.Registrator;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Base.ViewInfo;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraTab;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Umožnuje split mezi dvěma <see cref="DxGridControl"/>
    /// </summary>
    public class DxGridSplitContainer : DevExpress.XtraGrid.GridSplitContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridSplitContainer()
        {
            InitProperties();
        }
        /// <inheritdoc/>
        protected override GridControl CreateGridControl()
        {
            return new DxGridControl();
        }

        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void InitProperties()
        {
            SynchronizeFocusedRow = DevExpress.Utils.DefaultBoolean.True;
        }
        /// <summary>
        /// Zobrazí horizontální split
        /// </summary>
        public void ShowHorizontalSplit()
        {

            if (!IsSplitViewVisible || !Horizontal)
            {
                if (!Horizontal) Horizontal = true;
                ShowSplitView();
            }
        }
        /// <summary>
        /// Zobrazí vertikálí split
        /// </summary>
        public void ShowVerticalSplit()
        {

            if (!IsSplitViewVisible || Horizontal)
            {
                if (Horizontal) Horizontal = false;
                ShowSplitView();
            }
        }
    }

    /// <summary>
    /// Hlavní grid, který může mít více možností zobrazení, např. GridView, v naší podobě <see cref="DxGridView"/>
    /// </summary>
    public class DxGridControl : GridControl, IDxToolTipDynamicClient
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridControl()
        {
            RegisterEventsHandlers();
            _ToolTipInit();
        }

        /// <summary>
        /// příznak při změně DataSource
        /// </summary>
        public bool DataSourceChanging { get; set; }
        /// <summary>
        /// Interní ukládání vlastností během změnu Datasource (filterRow, Scroll, ActiveRow...)
        /// </summary>
        public bool StorePropertiesDuringDatasourceChange { get; set; }

        /// <summary>
        /// Vytvoří default náš poděděný GridView <see cref="DxGridView"/>
        /// </summary>
        /// <returns></returns>
        protected override BaseView CreateDefaultView()
        {
            return CreateView("DxGridView");
        }

        protected override void RegisterAvailableViewsCore(InfoCollection collection)
        {
            base.RegisterAvailableViewsCore(collection);
            collection.Add(new DxGridViewInfoRegistrator());
        }

        private void RegisterEventsHandlers()
        {
            this.EditorKeyDown += _OnEditorKeyDown;
            this.Paint += _OnPaintEx;
        }

        /// <summary>
        /// Daatový zdroj
        /// </summary>
        public override object DataSource
        {
            get
            {
                return base.DataSource;
            }
            set
            {
                //void setDataSourceAction()
                //{
                this.BeginUpdate();
                this.SuspendLayout();

                if (this.DefaultView is DxGridView view)
                {
                    try
                    {
                        if (value == null)
                        {
                            view.GridViewColumns = null;
                            view.GridViewCells = null;
                            view.SetSummaryRowValues(null);
                        }

                        this.DataSourceChanging = true;
                        base.DataSource = value;
                        //view.SetScrollPosition();
                        if (view.WaitForNextRows) view.RestorePropertiesBeforeLoadMoreRows();
                    }
                    finally
                    {
                        this.DataSourceChanging = false;
                        this.ResumeLayout();
                        this.EndUpdate();
                        view.WaitForNextRows = false;
                    }
                }
                //}
                //this.RunInGui(setDataSourceAction);
            }
        }

        private void _OnPaintEx(object sender, PaintEventArgs e)
        {
            //Malování ohraničení aktivního (focusovaného) řádku
            GridControl grid = sender as GridControl;
            GridView view = grid.FocusedView as GridView;
            if (view == null) return;
            GridViewInfo viewInfo = view.GetViewInfo() as GridViewInfo;
            GridRowInfo rowInfo = viewInfo.GetGridRowInfo(view.FocusedRowHandle);
            //if (rowInfo == null || !view.IsFocusedView)
            //return;

            //int x = rowInfo.DataBounds.X /*- rowInfo.IndentRect.X*/;
            //int y = rowInfo.DataBounds.Y + 1;
            //int width = rowInfo.DataBounds.Width - 1 /*+ rowInfo.IndentRect.Width*/;
            //int height = rowInfo.DataBounds.Height - 2;
            if (rowInfo != null)
            {
                int x = rowInfo.DataBounds.X;
                int y = rowInfo.DataBounds.Y;
                int width = rowInfo.DataBounds.Width - 2;
                int height = rowInfo.DataBounds.Height - 1;

                Color borderColor = ColorTranslator.FromHtml("#383838");
                if (DxComponent.IsDarkTheme) borderColor = Color.White;
                var pen = DxComponent.PaintGetPen(borderColor);
                pen.DashStyle = view.IsFocusedView ? System.Drawing.Drawing2D.DashStyle.Solid : System.Drawing.Drawing2D.DashStyle.Dot;
                e.Graphics.DrawRectangle(pen, x, y, width, height);

                if (pen.DashStyle != System.Drawing.Drawing2D.DashStyle.Solid) pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;    //Vrátím zpět na solid, protože pen je cache, tak at to neměním ostatním
            }
        }

        private void _OnEditorKeyDown(object sender, KeyEventArgs e)
        {
            //TODO
            //oblsuha kláves v řádkovém filtru
            DxGridView view = (sender as DxGridControl).FocusedView as DxGridView;
            if (view == null) return;
            if (view.FocusedRowHandle != DxGridControl.AutoFilterRowHandle) return;
            if (view.FocusedColumn.FieldName != "B") return;
            // if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar)) return;
            e.Handled = true;
        }

        #region ToolTipy pro columns, HasMouse a IsFocused
        /// <summary>
        /// Inicializace ToolTipu, voláno z konstruktoru
        /// </summary>
        private void _ToolTipInit()
        {
            this.ToolTipAllowHtmlText = null;
            this.DxToolTipController = DxComponent.CreateNewToolTipController(ToolTipAnchor.Cursor);
            this.DxToolTipController.AddClient(this);      // Protože this třída implementuje IDxToolTipDynamicClient, bude volána metoda IDxToolTipDynamicClient.PrepareSuperTipForPoint()
        }


        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy? Null = default
        /// </summary>
        public bool? ToolTipAllowHtmlText { get; set; }
        /// <summary>
        /// Controller ToolTipu
        /// </summary>
        public DxToolTipController DxToolTipController
        {
            get { return __DxToolTipController; }
            private set { __DxToolTipController = value; this.ToolTipController = value; }
        }
        private DxToolTipController __DxToolTipController;
        /// <summary>
        /// Zde control určí, jaký ToolTip má být pro danou pozici myši zobrazen
        /// </summary>
        /// <param name="args"></param>
        void IDxToolTipDynamicClient.PrepareSuperTipForPoint(Noris.Clients.Win.Components.AsolDX.DxToolTipDynamicPrepareArgs args)
        {
            if (this.FocusedView is DxGridView view)
            {
                DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hitInfo = view.CalcHitInfo(args.MouseLocation);
                IToolTipItem objectWithTooltip = null;  //RMC 0077291 22.01.2025 Nový přehled - popis skupin; přidána možnost tooltipu nad skupinou
                object clientData = null;
                if (hitInfo.Column != null && (hitInfo.InColumnPanel || hitInfo.InGroupColumn))
                {
                    //tooltip nad hlavičkou sloupce
                    objectWithTooltip = view.GetIGridViewColumn(hitInfo.Column?.FieldName);
                    clientData = hitInfo.Column;
                }
                else if (hitInfo.InDataRow)
                {
                    //tooltip nad skupinovým řádkem
                    int groupId = view.GetGroupRow(hitInfo.RowHandle);
                    objectWithTooltip = view.GetGroupInfo(groupId);
                    clientData = hitInfo.RowInfo;
                }

                if (objectWithTooltip != null)
                {
                    // Pokud myš nyní ukazuje na ten samý Node, pro který už máme ToolTip vytvořen, pak nebudeme ToolTip připravovat:
                    bool isSameAsLast = (args.DxSuperTip != null && Object.ReferenceEquals(args.DxSuperTip.ClientData, clientData));
                    if (!isSameAsLast)
                    {   // Připravíme data pro ToolTip:
                        var dxSuperTip = DxComponent.CreateDxSuperTip(objectWithTooltip);        // Vytvořím new data ToolTipu
                        if (dxSuperTip != null)
                        {
                            if (ToolTipAllowHtmlText.HasValue) dxSuperTip.ToolTipAllowHtmlText = ToolTipAllowHtmlText;
                            dxSuperTip.ClientData = clientData;                           // Přibalím si do nich náš object abych příště detekoval, zda jsme/nejsme na tom samém
                        }
                        args.DxSuperTip = dxSuperTip;
                        args.ToolTipChange = DxToolTipChangeType.NewToolTip;                 // Zajistím rozsvícení okna ToolTipu
                    }
                    else
                    {
                        args.ToolTipChange = DxToolTipChangeType.SameAsLastToolTip;          // Není třeba nic dělat, nechme svítit stávající ToolTip
                    }
                }
                else
                {   // Myš je mimo nody:
                    args.ToolTipChange = DxToolTipChangeType.NoToolTip;                      // Pokud ToolTip svítí, zhasneme jej
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Registrátor našeho <see cref="DxGridView"/>
    /// </summary>
    public class DxGridViewInfoRegistrator : GridInfoRegistrator
    {
        /// <inheritdoc/>
        public override string ViewName { get { return "DxGridView"; } }
        /// <inheritdoc/>
        public override BaseView CreateView(GridControl grid) { return new DxGridView(grid as GridControl); }
        /// <inheritdoc/>
        public override BaseViewInfo CreateViewInfo(BaseView view)
        {
            return new DxGridViewInfo(view as DxGridView);
        }
    }
    /// <summary>
    /// DxGridViewInfo
    /// </summary>
    public class DxGridViewInfo : DevExpress.XtraGrid.Views.Grid.ViewInfo.GridViewInfo
    {
        /// <summary>
        /// Konsutruktor
        /// </summary>
        /// <param name="view"></param>
        public DxGridViewInfo(DxGridView view)
            : base(view)
        {

        }
        private DxGridView DxGridView { get { return base.View as DxGridView; } }
        /// <inheritdoc/>
        protected override DevExpress.XtraGrid.Views.Grid.ViewInfo.GroupPanelRow CalcGroupPanelRowDrawInfo(Rectangle bounds, bool showCaption, bool lineStyle)
        {
            return base.CalcGroupPanelRowDrawInfo(bounds, showCaption, lineStyle);
        }
        /// <inheritdoc/>
        protected override void CalcColumnInfo(DevExpress.XtraGrid.Drawing.GridColumnInfoArgs ci, ref int lastLeft, bool lastColumn)
        {
            //RMC 0077267 21.01.2025 Nový přehled - řádek seskupení;
            //Zde se to volá před prvním zobrazením hlavičky sloupce.
            //Zvětším šířku hlavičky v group panelu
            //posunu nadpis o velikost ikony group state
            //novou pozici (Rect) nadpisu si uložím pro daný sloupce. Dále je použiji pro vykreslení v CustomDrowColumnHeader
            //přepočítám souřadnice pro inner elementy (ikona řazení, ikona excel filteru..)
            base.CalcColumnInfo(ci, ref lastLeft, lastColumn);
            if (ci.Type == GridColumnInfoType.Column && ci.Column.GroupIndex != -1 && ci.HeaderPosition == DevExpress.Utils.Drawing.HeaderPositionKind.Special)
            {
                Rectangle rect = ci.Bounds;
                rect.Width += DxGridView.GroupStateIconBackgroundAreaWidth;
                ci.Bounds = rect;

                ci.CaptionRect = new Rectangle(ci.CaptionRect.X + DxGridView.GroupStateIconBackgroundAreaWidth, ci.CaptionRect.Y, ci.CaptionRect.Width, ci.CaptionRect.Height);
                DxGridView.GroupColumnHeaderInfos[ci.Column] = new GroupColumnHeaderInfo { CaptionRect = ci.CaptionRect };
                GridColumnInfoArgs args = new GridColumnInfoArgs(ci.Column);
                ci.InnerElements.CalcBounds(args, ci.Cache, rect, rect);
            }
        }
        /// <summary>
        /// Slouží pro uložení iformací o column header ve chvíli kdy počítáme rozměry pro vykreslení
        /// </summary>
        public class GroupColumnHeaderInfo
        {
            /// <summary>
            /// Rectangle nadpisu
            /// </summary>
            public Rectangle CaptionRect { get; set; }
        }
    }

    /// <summary>
    /// DxDataController. Aktuálně řeší schování nativních group řádek z gridView 
    /// </summary>
    public class DxDataController : CurrencyDataController
    {
        /// <inheritdoc/>
        protected override void BuildVisibleIndexes()
        {
            //Tohe vyhazuje všechny groupovací řádky, generované komponentou.
            base.BuildVisibleIndexes();
            if (GroupedColumnCount == 0) return;
            int[] indexes = new int[VisibleIndexes.Count];
            VisibleIndexes.CopyTo(indexes, 0);
            VisibleIndexes.Clear();
            foreach (int rowHandle in indexes)
            {
                if (IsGroupRowHandle(rowHandle))
                {
                    //ExpandRow(rowHandle);   //Auto expand  the empty group row; //RMC 0076915 19.11.2024 Optimalizace group pro velký poč. záznam; Nahrazeno pomocí: OptionsBehavior.AutoExpandAllGroups = true
                    continue;
                }
                VisibleIndexes.Add(rowHandle);
            }
        }
    }
    /// <summary>
    /// Datový pohled typu <see cref="GridView"/>
    /// </summary>
    public class DxGridView : DevExpress.XtraGrid.Views.Grid.GridView, IListenerStyleChanged
    {
        #region constatns
        const int maxColumnWidthNoTrimm = 25;
        const int GROUPSTATEICONHEIGHT = 10;
        const int GRPUPSTATEICONWIDTH = 10;
        const int GROUPSTATEICONBORDER = 3;
        #endregion
        #region Properties and members
        /// <inheritdoc/>
        protected override string ViewName { get { return "DxGridView"; } }
        /// <inheritdoc/>
        protected override BaseGridController CreateDataController() { return new DxDataController(); }

        /// <summary>
        /// DxGridControll parent
        /// </summary>
        public DxGridControl DxGridControl { get { return this.GridControl as DxGridControl; } }
        /// <summary>
        /// Povolení interního mechanismu řádkového filtru v GridView
        /// </summary>
        public bool RowFilterCoreEnabled { get; set; }
        /// <summary>
        /// Povolení interního mechanismu řazení
        /// </summary>
        public bool ColumnSortCoreEnabled { get; set; }
        /// <summary>
        /// Poslední řádek na který bylo kliknuto MouseDown.
        /// </summary>
        public GridHitInfo LastMouseDownGridHitInfo { get; private set; }
        /// <summary>
        /// Poslední stisknutá klávesa
        /// </summary>
        public Keys LastKeyCodeDown { get; private set; }
        /// <summary>
        /// Aktitvní řádek (RowIndex)
        /// </summary>
        public int ActualActiveRowIndex { get; private set; }
        /// <summary>
        /// Poslední aktitvní řádek (RowIndex), před <see cref="ActualActiveRowIndex"/>
        /// </summary>
        public int LastActiveRowIndex { get; private set; }

        /// <summary>CZ: Je aktivni radkový filtr</summary>
        public bool IsFilterRowActive { get { return this.IsFilterRow(this.FocusedRowHandle); } }

        private bool _checkBoxRowSelect;
        /// <summary>
        /// Povoluje selectování řádků pomocí chceckBoxu
        /// </summary>
        public bool CheckBoxRowSelect { get => _checkBoxRowSelect; set { _checkBoxRowSelect = value; _SetGridMultiSelectAndMode(); } }

        private bool _multiSelect;
        /// <summary>
        /// Povoluje multiselect řádků
        /// </summary>
        public bool MultiSelect { get => _multiSelect; set { _multiSelect = value; _SetGridMultiSelectAndMode(); } }

        private bool _showGroupPanel;
        /// <summary>
        /// Nastavení viditelnosti groupovacího řádku
        /// </summary>
        public bool ShowGroupPanel
        {
            get => _showGroupPanel;
            set
            {
                if (DisableSortChange)
                {
                    _showGroupPanel = false;
                }
                else
                {
                    _showGroupPanel = value;
                }
                _SetShowGroupPanel();
            }
        }

        private bool _showFilterRow;
        /// <summary>
        /// Nastavení viditelnosti filtrovacího řádku
        /// </summary>
        public bool ShowFilterRow
        {
            get => _showFilterRow;
            set
            {
                _showFilterRow = value;
                _SetShowFilterRow();
            }
        }


        private bool _alignGroupSummaryInGroupRow;
        /// <summary>
        /// Určuje typ zobrazování sumy v group řádcích
        /// </summary>
        public bool AlignGroupSummaryInGroupRow { get => _alignGroupSummaryInGroupRow; set { _alignGroupSummaryInGroupRow = value; _SetAlignGroupSummaryInGroupRow(); } }

        private string _lastFocusedColumnInRowFilter = String.Empty;

        /// <summary>
        /// slouží pro uchování informace, na který sloupec se má nastavit focus v případě že chci nastavit focus do řádkového filtru.
        /// </summary>
        internal string LastFocusedColumnInRowFilter { get => _lastFocusedColumnInRowFilter; set => _lastFocusedColumnInRowFilter = value; }
        private bool _RowFilterVisible { get { return this.OptionsView.ShowAutoFilterRow; } /*set;*/ }


        /// <summary>
        /// Slouží pro uložení informací o sloupcích, kterými byla provedena inicializace View
        /// </summary>
        internal List<IGridViewColumn> GridViewColumns { get; set; }

        /// <summary>
        /// slouží pro uložení dodatečných informací o buňce pomocí <see cref="IGridViewCell"/>. Klíčem je kombinace rowIndex a a fieldName 
        /// </summary>
        internal Dictionary<Tuple<int, string>, IGridViewCell> GridViewCells { get; set; }

        /// <summary>
        /// Zakazuje změnu řazení na celém přehledu a skrývá group panel.
        /// </summary>
        public bool DisableSortChange { get; set; }

        #endregion

        #region konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridView() : this(null) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="grid"></param>
        public DxGridView(DevExpress.XtraGrid.GridControl grid) : base(grid)
        {
            _InitGridView();
            _RegisterEventsHandlers();
            DxComponent.RegisterListener(this);
        }
        #endregion

        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void _InitGridView()
        {
            _SetGridMultiSelectAndMode();

            OptionsSelection.EnableAppearanceFocusedCell = false;   //zakáz selectu cell, ale pouze barva, ohraničení zůstává
            OptionsSelection.EnableAppearanceHotTrackedRow = DefaultBoolean.True;   //Hottrack podbarvení
            OptionsSelection.ShowCheckBoxSelectorInPrintExport = DefaultBoolean.False;

            OptionsBehavior.Editable = false;   //readonly, necheceme editaci
            FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;

            //this.OptionsView.HeaderFilterButtonShowMode = DevExpress.XtraEditors.Controls.FilterButtonShowMode.SmartTag;

            // OptionsMenu.ShowAutoFilterRowItem = false;  //nechceme povolit uživateli přepínat zda bude/nebude vidět filtrovací řádek

            OptionsView.EnableAppearanceEvenRow = true;

            OptionsScrollAnnotations.ShowFocusedRow = DevExpress.Utils.DefaultBoolean.True;
            OptionsScrollAnnotations.ShowSelectedRows = DevExpress.Utils.DefaultBoolean.True;

            VertScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Always;  //zdůvodů aby se volalo vykreslování scrollbaru, na které se chyta načítání dalších dat.

            OptionsView.ShowFilterPanelMode = ShowFilterPanelMode.Default;  //panel se zobrazí pokud je filtr
            OptionsView.RowAutoHeight = true;   //globalní povolení automatické výšky řádky.

            OptionsView.ShowHorizontalLines = DefaultBoolean.False; //Kreslíme je samy v _OnCustomDrawCell. Je to kvůli Merge buněk
            OptionsView.ShowVerticalLines = DefaultBoolean.True;

            //Clipboard
            OptionsClipboard.AllowExcelFormat = DefaultBoolean.True;
            OptionsClipboard.ClipboardMode = DevExpress.Export.ClipboardMode.Formatted;

            //---Groupování TEST
            OptionsMenu.ShowGroupSummaryEditorItem = false;
            OptionsMenu.ShowSummaryItemMode = DefaultBoolean.True;
            OptionsMenu.EnableFooterMenu = false;
            OptionsMenu.EnableGroupRowMenu = false;
            OptionsView.ShowGroupedColumns = true;  //nechá groupovaný sloupec viditelný (na místě kde byl před groupováním)
            OptionsView.GroupFooterShowMode = GroupFooterShowMode.Hidden;    //Sumy group jsou zobrazeny vždy
            OptionsView.ShowGroupPanelColumnsAsSingleRow = true;    //zobrazení group tlačítek v jednom řádku
            OptionsBehavior.AllowFixedGroups = DefaultBoolean.False;
            Appearance.GroupFooter.FontStyleDelta = FontStyle.Bold;
            Appearance.FooterPanel.FontStyleDelta = FontStyle.Bold;

            OptionsBehavior.AutoExpandAllGroups = true; //RMC 0076915 19.11.2024 Optimalizace group pro velký poč. záznam; groupy chceme mít všechny rozbalené při první inicializaci. Jejich stav rozbaleno/sbaleno, řešíme o něco později samy.
            LevelIndent = 0;    //vypíná odsazení při zapnutém groupování.


            //this.OptionsBehavior.AlignGroupSummaryInGroupRow = DefaultBoolean.True;
            _SetAlignGroupSummaryInGroupRow();

            //----

        }

        #region Register/unregister events
        private void _RegisterEventsHandlers()
        {
            //Filtering
            this.FilterPopupExcelCustomizeTemplate += _OnFilterPopupExcelCustomizeTemplate;
            this.ColumnFilterChanged += _OnColumnFilterChanged;
            this.CustomRowFilter += _OnCustomRowFilter;
            this.FilterPopupExcelData += _OnFilterPopupExcelData;
            this.FilterPopupExcelQueryFilterCriteria += _OnFilterPopupExcelQueryFilterCriteria;
            this.FilterPopupExcelParseFilterCriteria += _OnFilterPopupExcelParseFilterCriteria;

            this.DoubleClick += _OnDoubleClick;
            this.Click += _OnClick;

            this.SelectionChanged += _OnSelectionChanged;
            this.FocusedRowChanged += _OnFocusedRowChanged;
            this.FocusedColumnChanged += _OnFocusedColumnChanged;

            // sorting events
            this.StartSorting += _OnStartSorting;   //zatím nepoužívám
            this.CustomColumnSort += _OnCustomColumnSort;
            this.EndSorting += _OnEndSorting;   //zatím nepoužívám; registruji přímo v ListGrid

            //Grouping 
            this.EndGrouping += _OnEndGrouping;

            this.KeyDown += _OnKeyDown;
            this.KeyPress += _OnKeyPress;

            this.CustomRowCellEdit += _OnCustomRowCellEdit;

            // ScrollBar:
            this.TopRowChanged += _OnTopRowChanged;
            this.CalcRowHeight += View_CalcRowHeight;
            this.CustomDrawScroll += View_CustomDrawScroll;

            this.Layout += _OnLayout;

            this.PopupMenuShowing += _OnPopupMenuShowing;
            this.CustomDrawCell += _OnCustomDrawCell;

            this.CustomSummaryCalculate += _OnCustomSummaryCalculate;
            this.CustomDrawFooterCell += _OnCustomDrawFooterCell;
            this.CustomDrawGroupRowCell += _OnCustomDrawGroupRowCell;
            this.CustomDrawRowIndicator += _OnCustomDrawRowIndicator;
            this.CustomDrawColumnHeader += _OnCustomDrawColumnHeader;

            this.MouseUp += _OnMouseUp;
            this.MouseDown += _OnMouseDown;
            this.ColumnWidthChanged += _OnColumnWidthChanged;

            this.ValidatingEditor += _OnValidatingEditor;
            this.InvalidValueException += _OnInvalidValueException;

            this.DragObjectStart += _OnDragObjectStart;
            this.DragObjectOver += _OnDragObjectOver;
            this.DragObjectDrop += _OnDragObjectDrop;
        }
        private void _UnRegisterEventsHandlers()
        {
            //Filtering
            this.FilterPopupExcelCustomizeTemplate -= _OnFilterPopupExcelCustomizeTemplate;
            this.ColumnFilterChanged -= _OnColumnFilterChanged;
            this.CustomRowFilter -= _OnCustomRowFilter;
            this.FilterPopupExcelData += _OnFilterPopupExcelData;
            this.FilterPopupExcelQueryFilterCriteria += _OnFilterPopupExcelQueryFilterCriteria;
            this.FilterPopupExcelParseFilterCriteria += _OnFilterPopupExcelParseFilterCriteria;

            this.DoubleClick -= _OnDoubleClick;
            this.Click -= _OnClick;

            this.SelectionChanged -= _OnSelectionChanged;
            this.FocusedRowChanged -= _OnFocusedRowChanged;
            this.FocusedColumnChanged -= _OnFocusedColumnChanged;

            // sorting events
            this.StartSorting -= _OnStartSorting;   //zatím nepoužívám
            this.CustomColumnSort -= _OnCustomColumnSort;
            this.EndSorting -= _OnEndSorting;   //zatím nepoužívám; registruji přímo v ListGrid

            //Grouping 
            this.EndGrouping -= _OnEndGrouping;

            this.KeyDown -= _OnKeyDown;
            this.KeyPress -= _OnKeyPress;

            this.CustomRowCellEdit -= _OnCustomRowCellEdit;

            // ScrollBar:
            this.TopRowChanged -= _OnTopRowChanged;
            this.CalcRowHeight -= View_CalcRowHeight;
            this.CustomDrawScroll -= View_CustomDrawScroll;

            this.Layout -= _OnLayout;

            this.PopupMenuShowing -= _OnPopupMenuShowing;
            this.CustomDrawCell -= _OnCustomDrawCell;

            this.CustomSummaryCalculate -= _OnCustomSummaryCalculate;
            this.CustomDrawFooterCell -= _OnCustomDrawFooterCell;
            this.CustomDrawGroupRowCell -= _OnCustomDrawGroupRowCell;
            this.CustomDrawRowIndicator -= _OnCustomDrawRowIndicator;
            this.CustomDrawColumnHeader -= _OnCustomDrawColumnHeader;

            this.MouseUp -= _OnMouseUp;
            this.MouseDown -= _OnMouseDown;
            this.ColumnWidthChanged -= _OnColumnWidthChanged;

            this.ValidatingEditor -= _OnValidatingEditor;
            this.InvalidValueException -= _OnInvalidValueException;

            this.DragObjectStart -= _OnDragObjectStart;
            this.DragObjectOver -= _OnDragObjectOver;
            this.DragObjectDrop -= _OnDragObjectDrop;
        }
        #endregion

        #region skin/vzhled

        /// <summary>
        /// Je voláno vždy po změně skinu
        /// </summary>
        void IListenerStyleChanged.StyleChanged()
        {
            __groupButtonColapsed = __groupButtonExpandedBelow = __groupButtonExpandedAbove = null; //reset images=>Refresh, protože se načtou znovu pro aktuální skin
            _RefreshColumnHeaderImages();
            GroupInfos.ForEach(groupInfo => groupInfo.RefreshStyleInfo());  //refresh StyleInfo (kalíšku) pro GroupInfo
        }
        private void _RefreshColumnHeaderImages()
        {
            foreach (GridColumn gcItem in this.Columns)
            {
                var gridViewColumn = GetIGridViewColumn(gcItem);
                if (gridViewColumn != null) _ApplyImageToColumnHeader(gcItem, gridViewColumn);
            }
        }
        #endregion
        private void _OnColumnWidthChanged(object sender, ColumnEventArgs e)
        {
            _SetColumnTextTrimming(e.Column);
        }

        private static void _SetColumnTextTrimming(GridColumn column)
        {
            if (column.Width < maxColumnWidthNoTrimm)
            {
                column.AppearanceCell.TextOptions.Trimming = Trimming.None;
            }
            else
            {
                column.AppearanceCell.TextOptions.Trimming = Trimming.Default;
            }
        }

        private void _OnMouseDown(object sender, MouseEventArgs e)
        {
            DxGridView view = sender as DxGridView;
            GridHitInfo hitInfo = view.CalcHitInfo(e.Location);
            DXMouseEventArgs dxE = DXMouseEventArgs.GetMouseArgs(e);

            //Disable select groupRow
            //if (hitInfo.Column?.FieldName == "DX$CheckboxSelectorColumn" && hitInfo.RowInfo != null && _IsGroupRow(hitInfo.RowInfo.RowHandle))
            //{
            //    DXMouseEventArgs ea = DXMouseEventArgs.GetMouseArgs(e);
            //    ea.Handled = true;
            //    return;
            //}

            if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Control) //LButton and ctrl only
            {
                //Nemám označen žádný řádek a left click + ctrl (výběr jednotlivých řádků) na jíný řádek než byl aktuální. 
                //Chci aby se označil i původní (poslední) aktivní řádek. Stejně se to chová v TotalCommanderu.
                if (LastMouseDownGridHitInfo != null)
                {
                    int lastRow = LastMouseDownGridHitInfo.RowHandle;
                    if (view.IsMultiSelect
                        && view.SelectedRowsCount == 0
                        && hitInfo.InDataRow
                        && lastRow != hitInfo.RowHandle
                        && view.IsDataRow(lastRow) && !view.IsRowSelected(lastRow))
                    {
                        view.SelectRow(lastRow);
                    }
                }
            }
            LastMouseDownGridHitInfo = hitInfo;
        }

        private void _OnMouseUp(object sender, MouseEventArgs e)
        {
            DxGridView view = sender as DxGridView;
            GridHitInfo hitInfo = view.CalcHitInfo(e.Location);
            DXMouseEventArgs dxE = DXMouseEventArgs.GetMouseArgs(e);

            //nastavení clipboardu při alt left click.
            if (e.Button == MouseButtons.Left && Control.ModifierKeys == Keys.Alt) //LButton and ALT only
            {
                if (hitInfo.InRow && hitInfo.InRowCell)
                {
                    string valueString = string.Empty;
                    var value = view.GetRowCellDisplayText(hitInfo.RowHandle, hitInfo.Column);
                    if (value != null)
                    {
                        valueString = value.ToString();
                    }
                    if (String.IsNullOrEmpty(valueString))
                        System.Windows.Forms.Clipboard.Clear();
                    else
                        System.Windows.Forms.Clipboard.SetText(valueString, System.Windows.Forms.TextDataFormat.UnicodeText);
                    Globals.NotifyToast(GridControl, null, DxComponent.Localize(MsgCode.CopyBrowseCell, hitInfo.Column.Caption), System.Windows.Forms.ToolTipIcon.Info);
                    return;
                }
            }

            //Změna logiky označení/rušení označení všech řádků pomocí checkBoxů v hlavičce
            //Pokud mám označený nějaký řádek (ne všechny), tak prvním kliknutím na checkBox chci zrušit označení všech řádků.
            if (hitInfo.InColumn && !hitInfo.InRow)
            {
                if (hitInfo.Column.FieldName == GridView.CheckBoxSelectorColumnName || hitInfo.Column.FieldName == view.OptionsSelection.CheckBoxSelectorField)
                {
                    //view.BeginDataUpdate();
                    dxE.Handled = true; // Suppress default behavior
                    if (SelectedAllRows || !SelectedAllRows && SelectedRowsCount > 0)
                    {
                        SelectedAllRows = false;
                    }
                    else
                    {
                        SelectedAllRows = true;
                    }
                    //view.EndDataUpdate();
                }
            }
            if (hitInfo.HitTest == GridHitTest.GroupPanelColumn && hitInfo.Column != null && IsGroupIconRect(e.Location, hitInfo.Column))
            {
                GridColumn column = hitInfo.Column;
                //XtraMessageBox.Show(string.Format("Custom Button in {0}", column.FieldName));
                if (IsGroupExpanded(column.GroupIndex))
                {
                    _SetAllGroupsAndChildsColapsed(column.GroupIndex);
                }
                else
                {
                    _SetAllGroupsAndParentsExpanded(column.GroupIndex);
                }
                DXMouseEventArgs.GetMouseArgs(e).Handled = true;
            }
        }

        private string ConvertAndFormatValue(object value, Type targetType, string formatString)
        {
            if (value == DBNull.Value || value == null)
            {
                return null; // Ošetření null nebo DBNull hodnot
            }
            try
            {
                if (targetType == typeof(decimal))
                {
                    decimal decimalValue = Convert.ToDecimal(value);
                    return decimalValue.ToString(formatString);
                }
                else if (targetType == typeof(int) || targetType == typeof(short) || targetType == typeof(Int16))
                {
                    int intValue = Convert.ToInt32(value);
                    return intValue.ToString(formatString);
                }
                else if (targetType == typeof(long) || targetType == typeof(Int64))
                {
                    long longValue = Convert.ToInt64(value);
                    return longValue.ToString(formatString);
                }
            }
            catch { }
            return null; // Pokud dojde k chybě nebo typ není podporován
        }

        /// <summary>
        /// volá se pro typy Custom v summačním řádku...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnCustomSummaryCalculate(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            GridSummaryItem item = e.Item as GridSummaryItem;
            var column = GetIGridViewColumn(item.FieldName);
            if (e.IsTotalSummary && column.IsNumberColumnType)
            {
                switch (e.SummaryProcess)
                {
                    case CustomSummaryProcess.Start:
                        e.TotalValue = 0;
                        break;
                    case CustomSummaryProcess.Calculate:
                        if (e.FieldValue != null)
                        {
                            bool shouldSum = !_IsGroupRow(e.RowHandle);
                            if (shouldSum)
                            {
                                if (column.IsNumberIntegerColumnType && int.TryParse(e.FieldValue.ToString(), out int intValue))
                                {
                                    int.TryParse(e.TotalValue.ToString(), out int totalIntValue);
                                    e.TotalValue = totalIntValue + intValue;
                                }
                                else
                                {
                                    if (decimal.TryParse(e.FieldValue.ToString(), out decimal value))
                                    {
                                        decimal.TryParse(e.TotalValue.ToString(), out decimal totalValue);
                                        e.TotalValue = totalValue + value;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _UnRegisterEventsHandlers();
            }
            base.Dispose(disposing);
        }

        private void _SetShowGroupPanel()
        {
            if (OptionsView.ShowGroupPanel != _showGroupPanel)
            {
                //změna viditelnosti groupovacího řádku
                OptionsView.ShowGroupPanel = _showGroupPanel;
                OnDxShowGroupPanelChanged();
            }
        }

        private void _SetShowFilterRow()
        {
            if (OptionsView.ShowAutoFilterRow != _showFilterRow)
            {
                //změna viditelnosti filtrovacího řádku
                OptionsView.ShowAutoFilterRow = _showFilterRow;
                OnDxShowFilterRowChanged();
            }
        }

        #region Select rows and Current row

        private int FocusedRowIndex
        {
            get { return GetDataSourceRowIndex(FocusedRowHandle); }
            set { FocusedRowHandle = GetRowHandle(value); }
        }
        /// <summary>
        /// Aktuální řádek s fokusem
        /// </summary>
        public int CurrentRowNumber
        {
            get => FocusedRowIndex;
            set
            {
                if (IsValidRowHandle(value))
                {
                    if (FocusedRowIndex == value)
                    {
                        //uměle vyvolání změny i když se nic nezměnilo. Server to potřebuje vědět jako potvrzení, že jsme nastavili a zároveň se na tu změnu chytají nějaké háčky...
                        OnFocusedRowChanged(FocusedRowIndex, FocusedRowIndex);
                    }
                    else
                    {
                        //OnFocusedRowChanged vyvolá komponenta, protože se opravdu změní řádek
                        FocusedRowIndex = value;
                    }
                }
                else
                {
                    OnFocusedRowChanged(FocusedRowIndex, FocusedRowIndex);
                }
            }
        }

        private bool _selectedAllRows;
        /// <summary>
        /// Selectování všech řádků
        /// </summary>
        public bool SelectedAllRows { get => _selectedAllRows; set { _SetSelectedAllRows(value); } }
        private void _SetSelectedAllRows(bool value)
        {
            _selectedAllRows = value;
            if (_selectedAllRows)
            {
                SelectAll();
                if (!AllRowsLoaded)
                {
                    RaiseSummaryRowForAllRows();
                }
            }
            else
            {
                ClearSelection();
                //nulujeme summy, protože již nemáme všechny řádky selected
                _summaryRowData = null;
            }
        }

        /// <summary>
        /// Zde jsou uchovány selectovány řádky v pořadí jak byly selektovány. Pozor, funguje to jen pro režim MultiSelect.
        /// </summary>
        private List<int> _SelectedRowsCache = new List<int>();
        /// <summary>
        /// Selektované řádky v zachovaném pořadí jak byly selektovány. Oproti tomu GetSelectedRows() vrací seřazené podle indexu. />
        /// </summary>
        public List<int> SelectedRows
        {
            get { return this.MultiSelect ? _SelectedRowsCache : this.GetSelectedRows().ToList(); } //_SelectedRowsCache se plni správně jen v MultiSelect režimu.
            set
            {
                if (value?.Count > 0)
                {
                    BeginSelection();
                    try
                    {
                        ClearSelectionCore();
                        foreach (int rowIndex in value)
                        {
                            this.SelectRow(rowIndex);
                        }
                    }
                    finally
                    {
                        EndSelection();
                    }
                }
                else
                {
                    ClearSelection();
                }
            }
        }
        /// <inheritdoc/>
        public override void SelectRow(int rowHandle)
        {

            int rowIndex = this.GetDataSourceRowIndex(rowHandle);
            if (!_SelectedRowsCache.Contains(rowIndex))
                _SelectedRowsCache.Add(rowIndex);
            base.SelectRow(rowHandle);
        }
        /// <inheritdoc/>
        public override void UnselectRow(int rowHandle)
        {
            int rowIndex = this.GetDataSourceRowIndex(rowHandle);
            _SelectedRowsCache.Remove(rowIndex);
            base.UnselectRow(rowHandle);
        }
        /// <inheritdoc/>
        protected override void ClearSelectionCore()
        {
            _SelectedRowsCache.Clear();
            base.ClearSelectionCore();
        }
        /// <inheritdoc/>
        public override void SelectRange(int startRowHandle, int endRowHandle)
        {
            if (startRowHandle > endRowHandle)
            {
                for (int i = startRowHandle - 1; i < endRowHandle; i--)
                {
                    _SelectedRowsCache.Remove(i);
                    int posn = SelectedRows.IndexOf(i + 1) + 1;
                    _SelectedRowsCache.Insert(posn, i);
                }
            }
            base.SelectRange(startRowHandle, endRowHandle);
        }
        /// <inheritdoc/>
        public override void SelectAll()
        {
            BeginSelection();
            try
            {
                bool _oldSilent = _silentMode;
                if (RowCount > 0) _silentMode = true; //Nechci posílát event o změně selctovaných řádků, pokud mám co selectovat, tak se pošle se změnou.
                ClearSelectionCore();
                _silentMode = _oldSilent;
                for (int i = 0; i < RowCount; i++)
                {
                    SelectRow(GetVisibleRowHandle(i));
                }
            }
            finally
            {
                _selectedAllRows = true;
                EndSelection();
            }
        }

        #endregion

        #region Drawing, customDraw
        private void _OnCustomDrawCell(object sender, RowCellCustomDrawEventArgs e)
        {
            bool drawEmptyMergedCell = false;
            bool nextCellValueIsDiferent = true;
            Color mergedCellBackground = this.PaintAppearance.Row.GetBackColor(e.Cache);
            Color borderColor = this.PaintAppearance.HorzLine.GetBackColor(e.Cache);
            GridCellInfo cellInfo = (e.Cell as GridCellInfo);
            var iColumn = GetIGridViewColumn(e.Column);

            //Datový řádek (buňka)
            //Console.WriteLine($"CustomDrawCell rowHandle: {e.RowHandle} column: {e.Column}");
            if (this.IsDataRow(e.RowHandle) && e.Column.VisibleIndex >= 0 && e.Column.FieldName != GROUPBUTTON)
            {
                if (iColumn != null && iColumn.AllowMerge && !_IsGroupRow(e.RowHandle))
                {
                    //Merge column
                    int previousRowHandle = e.RowHandle - 1;
                    if (this.IsDataRow(previousRowHandle))
                    {
                        object previousCellValue = this.GetRowCellValue(previousRowHandle, e.Column);
                        //kontrola zda je aktuální hodnota buňky stejná s předchozí viditelnou
                        if (previousCellValue != null && previousCellValue.Equals(e.CellValue) && this.IsRowVisible(previousRowHandle) == RowVisibleState.Visible)
                        {
                            if (!this.IsRowSelected(e.RowHandle))
                            {
                                //změna pozadí pro merge buňky, překreslím to...
                                Rectangle rect = e.Bounds;
                                rect.Inflate(1, 1);
                                e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(mergedCellBackground), rect);
                            }
                            drawEmptyMergedCell = true;
                        }
                        else
                        {
                            if (!this.IsRowSelected(e.RowHandle))
                            {
                                e.Appearance.BackColor = mergedCellBackground;
                            }
                            drawEmptyMergedCell = false;
                        }
                    }
                    //zjištění zda další buňka je jiná
                    int nextRowHandle = e.RowHandle + 1;
                    if (this.IsDataRow(nextRowHandle) && !_IsGroupRow(nextRowHandle))
                    {
                        object nextCellValue = this.GetRowCellValue(nextRowHandle, e.Column);
                        if (nextCellValue != null && nextCellValue.Equals(e.CellValue))
                        {
                            nextCellValueIsDiferent = false;
                        }
                    }
                }

                //barvičky/kalíšky
                int dataSourceRowIndex = this.GetDataSourceRowIndex(e.RowHandle);
                StyleInfo styleInfo = null;

                if (iColumn != null && iColumn.IsHiddenValue)
                {
                    //Hidden value 
                    styleInfo = DxComponent.GetStyleInfo("HiddenValue", null);
                }
                else if (GridViewCells != null && GridViewCells.TryGetValue(GridViewCellData.CreateId(dataSourceRowIndex, e.Column.FieldName), out var cell))
                {
                    //kalíšek z definice ze serveru
                    styleInfo = cell.StyleInfo;
                }
                if (styleInfo != null)
                {
                    if (this.IsRowSelected(e.RowHandle))
                    {
                        //selectovaný řádek: provedeme merge barev pro pozadí. Barva pro označovaní řádků + barva kalíšku
                        if (styleInfo.AttributeBgColor != null) e.Appearance.BackColor = this.PaintAppearance.SelectedRow.BackColor.Morph(styleInfo.AttributeBgColor.Value, 0.5f); ;
                    }
                    else
                    {
                        if (styleInfo.AttributeBgColor != null) e.Appearance.BackColor = styleInfo.AttributeBgColor.Value;
                    }
                    if (styleInfo.AttributeColor != null) e.Appearance.ForeColor = styleInfo.AttributeColor.Value;
                    if (styleInfo.AttributeFontStyle != null) e.Appearance.FontStyleDelta = styleInfo.AttributeFontStyle.Value;
                }

                if (drawEmptyMergedCell)
                {
                    //nevolám defaultDraw => prázdná buňka
                }
                else
                {
                    e.DefaultDraw();
                }
                if (nextCellValueIsDiferent)
                {
                    //pokud následující buňkaje má jinou hodnotu, tak kresli spodni linku
                    //zde se to bude kreslit i pro sloupce bez merge. Máme globálně vypnuté horizontální linky.
                    DrawCellBottomBorder(e.Graphics, cellInfo.Bounds, borderColor);
                }
                e.Handled = true;   //Již se nic nezpracuje v base
            }
            if (e.Column.FieldName == GROUPBUTTON)
            {
                int groupId = _GetGroupIdRow(e.RowHandle);
                if (groupId >= 0)
                {
                    //vykreslíme tlačítko
                    GroupRowInfo gInfo = GroupRowInfos.First(y => y.GroupId == groupId);
                    //tlačítko nevykreslujeme pro polsední úroveň pro mode ShowOnlyGroups
                    if (GroupMode != GroupModes.ShowOnlyGroups || GroupMode == GroupModes.ShowOnlyGroups && !_IsTheLowestGroup(gInfo.Group))
                    {
                        if (!gInfo.Expanded)
                        {
                            e.Graphics.DrawImage(_GroupButtonColapsed, _GetPointForCenterImage(e.Bounds, _GroupButtonColapsed.Width, _GroupButtonColapsed.Height));
                        }
                        else { e.Graphics.DrawImage(_GroupButtonExpanded, _GetPointForCenterImage(e.Bounds, _GroupButtonExpanded.Width, _GroupButtonExpanded.Height)); }
                    }
                }

                DrawCellBottomBorder(e.Graphics, cellInfo.Bounds, borderColor);
                e.Handled = true;   //Již se nic nezpracuje v base
            }

            //filtrovací řádek
            if (this.IsFilterRow(e.RowHandle))
            { //zapisuji do DisplayText hodnotu před konverzi filtru, tedy restore původní hodnoty zadané uživatelem; případ kdy stojím mimo input...
                string displayText;
                _convertedColumnFilterCache.TryGetValue(e.Column.FilterInfo.FilterString, out displayText);
                if (!string.IsNullOrEmpty(displayText))
                {
                    e.DisplayText = displayText;
                }
            }

        }
        /// <summary>
        /// kreslí spodní linku ohraničení buňky.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="borderColor"></param>
        private void DrawCellBottomBorder(Graphics graphics, Rectangle bounds, Color borderColor)
        {
            Pen pen = DxComponent.PaintGetPen(borderColor);

            int x1 = bounds.X;
            int x2 = bounds.X + bounds.Width;
            int y = bounds.Bottom - 1;

            graphics.DrawLine(pen, x1, y, x2, y);
        }
        private void _OnCustomDrawRowIndicator(object sender, RowIndicatorCustomDrawEventArgs e)
        {//https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.Views.Grid.GridView.CustomDrawRowIndicator
            this.CustomDrawRowIndicator += (s, ee) =>
            {
                if (!ee.Info.IsRowIndicator)
                {
                    return;
                }
                if (!this.IsFilterRow(ee.RowHandle)) return;
                //RowIndicator pro RowFilter, změna ikonky na mazání filtru
                ee.Handled = false;  //false -> nechci uplně zazdít vykreslování, jen něco změnit (obrázek)
                if (HasRowFilterExpression)
                {
                    //je vyplněn nějaký filtr -> je co mazat
                    ee.Info.ImageIndex = 4;  //křížek
                }
            };
        }
        private Point _GetPointForCenterImage(Rectangle bounds, int imageWidth, int imageHeight)
        {
            Point locationPoint = bounds.Location;
            locationPoint.X += bounds.Width / 2 - imageWidth / 2;
            locationPoint.Y += bounds.Height / 2 - imageHeight / 2;
            return locationPoint;
        }
        private void _OnCustomDrawFooterCell(object sender, FooterCellCustomDrawEventArgs e)
        {
            GridSummaryItem summary = e.Info.SummaryItem;
            if (_summaryRowData != null)
            {
                IGridViewColumn gv = GetIGridViewColumn(e.Column);
                Type type = gv?.ColumnType;
                string formattedValue = null;   //RMC 13.01.2025; refaktor získání hodnoty ze summaryRowData a ošetření konverze.

                if (type != null && _summaryRowData.Table.Columns.Contains(e.Column.FieldName))
                {
                    object rawValue = _summaryRowData[e.Column.FieldName];
                    formattedValue = ConvertAndFormatValue(rawValue, type, gv?.FormatString);
                }
                if (formattedValue != null)
                {
                    e.Info.DisplayText = formattedValue;
                }
                else
                {
                    e.Info.DisplayText = "..."; // znějakého důvodu se nepodařilo dostat data. zobrazím ...
                }
            }
            e.Info.Icon = null; //vypnutí ikonky v summačním řádku.
        }
        private void _OnCustomDrawColumnHeader(object sender, ColumnHeaderCustomDrawEventArgs e)
        {
            if (e.Column?.GroupIndex >= 0 && e.Info.HeaderPosition == DevExpress.Utils.Drawing.HeaderPositionKind.Special)
            {
                if (GroupColumnHeaderInfos.TryGetValue(e.Column, out DxGridViewInfo.GroupColumnHeaderInfo info))
                {
                    e.Info.CaptionRect = info.CaptionRect;  //nastavení souřadnic pro nadpis. Bere souřadnice, které se vypočítaly při prvním výpočtu velikosti
                }

                e.Painter.DrawObject(e.Info);   //vykreslední pocí paintru

                //dokreslení ikonky stavu groupy
                Rectangle iconBackGroundArea = GetGroupStateIconBackgroundArea(e.Info.CaptionRect);
                Rectangle iconArea = GetGroupStateIconArea(e.Info.CaptionRect);

                GroupInfo gInfo = GroupInfos.FirstOrDefault(i => i.Group == e.Column?.GroupIndex);
                Color? groupColor = gInfo?.StyleInfo?.AttributeBgColor;

                if (groupColor != null) e.Graphics.FillRectangle(new SolidBrush(groupColor.Value), iconBackGroundArea);
                if (gInfo != null && gInfo.Expanded)
                {
                    e.Graphics.DrawImage(_GroupButtonExpanded, iconArea);
                }
                else
                {
                    e.Graphics.DrawImage(_GroupButtonColapsed, iconArea);
                }

                e.Handled = true;
            }

        }

        #endregion
        private void _SetGridMultiSelectAndMode()
        {
            OptionsSelection.MultiSelect = MultiSelect;
            if (CheckBoxRowSelect)
            {
                OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;     // mód označování v režimu multiselect=true (RowSelect,CellSelect,CheckBoxRowSelect)
                OptionsSelection.CheckBoxSelectorColumnWidth = 30;
            }
            else
            {
                OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.RowSelect;     // mód označování v režimu multiselect=true (RowSelect,CellSelect,CheckBoxRowSelect)
            }
        }
        private void _SetAlignGroupSummaryInGroupRow()
        {
            this.OptionsBehavior.AlignGroupSummaryInGroupRow = DxComponent.ConvertBool(AlignGroupSummaryInGroupRow);
        }
        private void _OnPopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            if (e.HitInfo.InDataRow && e.MenuType != DevExpress.XtraGrid.Views.Grid.GridMenuType.AutoFilter)    //obsluha zatím jen DataRow; skin WXI při autofilter má nastaveno inDataRow (asi bug)...
            {
                e.Allow = false;
                var hitInfo = new DxGridHitInfo(e.HitInfo, Cursor.Position);
                this.RaiseShowContextMenu(hitInfo, "");
            }
            else if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.AutoFilter)
            {
                //Přidání položel v menu operátora řádkového filtru. (Je zadáno, Není zadáno)
                IGridViewColumn colum = GetIGridViewColumn(e.HitInfo.Column);
                if (colum != null && !colum.IsEditStyleColumnType)  //nechceme pro editační styly, protože neumíme vložit jiný text než je editační styl
                {
                    DevExpress.Utils.Menu.DXMenuCheckItem item = new DevExpress.Utils.Menu.DXMenuCheckItem();
                    e.Menu.ItemClick -= Menu_ItemClick;
                    e.Menu.ItemClick += Menu_ItemClick;
                    e.Menu.Items.Add(_CreateConditionMenuItem(FilterUIElementLocalizerStringId.CustomUIFilterIsBlankName, "IsNullOrEmpty"));
                    e.Menu.Items.Add(_CreateConditionMenuItem(FilterUIElementLocalizerStringId.CustomUIFilterIsNotBlankName, "IsNotNullOrEmpty"));
                }
            }
            else if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Column && e.HitInfo.InGroupColumn)
            {
                //kontextové menu v group panelu. Sbalit vše a rozbalit vše.
                e.Menu.ItemClick -= Menu_ItemClick;
                e.Menu.ItemClick += Menu_ItemClick;

                int? group = GetGroupByColumn(e.HitInfo.Column);
                if (group != null)
                {
                    DXMenuItem expandAll = e.Menu.Items.FirstOrDefault(x => x.Tag.Equals(DevExpress.XtraGrid.Localization.GridStringId.MenuGroupPanelFullExpand));
                    if (expandAll != null)
                    {
                        //do tagu přidám číslo skupiny, abych mohl v Menu_ItemClick číslo skupiny parsovat a použít. 
                        expandAll.Tag = GridLocalizer.Active.GetLocalizedString(DevExpress.XtraGrid.Localization.GridStringId.MenuGroupPanelFullExpand) + "_" + group.Value;
                    }
                    DXMenuItem collapseAll = e.Menu.Items.FirstOrDefault(x => x.Tag.Equals(DevExpress.XtraGrid.Localization.GridStringId.MenuGroupPanelFullCollapse));
                    if (collapseAll != null)
                    {
                        collapseAll.Tag = GridLocalizer.Active.GetLocalizedString(DevExpress.XtraGrid.Localization.GridStringId.MenuGroupPanelFullCollapse) + "_" + group.Value;
                    }
                }
            }
            else if (e.HitInfo.InColumn && e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Column && e.HitInfo.Column?.FieldName == GROUPBUTTON)
            {
                //RMC 14.1.2025 0077153 Nový přehled -ad - hoc seskupení
                //Hromadné operace nad skupinou
                e.Allow = false;
                var hitInfo = new DxGridHitInfo(e.HitInfo, Cursor.Position);
                this.RaiseShowContextMenu(hitInfo, "ColumnHeader");
            }
        }

        /// <summary>
        ///Vytvoření položel pro menu operátora řádkového filtru. (Je zadáno, Není zadáno)
        /// </summary>
        /// <param name="localizerStringId"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        private DXMenuCheckItem _CreateConditionMenuItem(FilterUIElementLocalizerStringId localizerStringId, string condition)
        {
            //https://supportcenter.devexpress.com/ticket/details/t601149/how-to-access-auto-filter-row-icons
            Image image = DevExpress.XtraEditors.FilterControl.GetClauseImageByType(DevExpress.LookAndFeel.UserLookAndFeel.Default, condition);
            string caption = FilterUIElementLocalizer.GetString(localizerStringId);

            DXMenuCheckItem item = new DXMenuCheckItem();
            item.ImageOptions.Image = image;
            item.Caption = caption;
            item.Tag = localizerStringId;
            return item;
        }

        /// <summary>
        /// Obsluha clicku na námi přidanou položku v menu operátora řádkového filtru. (Je zadáno, Není zadáno)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_ItemClick(object sender, DevExpress.Utils.Menu.DXMenuItemEventArgs e)
        {
            var column = this.FocusedColumn;
            if (e.Item.Tag.Equals(FilterUIElementLocalizerStringId.CustomUIFilterIsBlankName))
            {
                var bOpertar = new BinaryOperator(column.FieldName, WildCardNull, BinaryOperatorType.Equal);
                column.FilterInfo = new ColumnFilterInfo(bOpertar);
            }
            else if (e.Item.Tag.Equals(FilterUIElementLocalizerStringId.CustomUIFilterIsNotBlankName))
            {
                var bOpertar = new BinaryOperator(column.FieldName, WildCardNull, BinaryOperatorType.NotEqual);
                column.FilterInfo = new ColumnFilterInfo(bOpertar);
            }
            else if (e.Item.Tag.ToString().StartsWith(GridLocalizer.Active.GetLocalizedString(DevExpress.XtraGrid.Localization.GridStringId.MenuGroupPanelFullExpand)))
            {
                string[] tags = e.Item.Tag.ToString().Split('_');
                if (tags.Length == 2 && int.TryParse(tags[1], out int group))
                {
                    _SetAllGroupsAndParentsExpanded(group);
                }
            }
            else if (e.Item.Tag.ToString().StartsWith(GridLocalizer.Active.GetLocalizedString(DevExpress.XtraGrid.Localization.GridStringId.MenuGroupPanelFullCollapse)))
            {
                string[] tags = e.Item.Tag.ToString().Split('_');
                if (tags.Length == 2 && int.TryParse(tags[1], out int group))
                {
                    _SetAllGroupsAndChildsColapsed(group);
                }
            }
            else
            {
                this.CloseEditor();
            }
        }

        private void _OnLayout(object sender, EventArgs e)
        {
            //nexistuje samostaná eventa pro změnu viditelnosti groupovací řádky, proto ošetřuji zde.
            if (OptionsView.ShowGroupPanel != _showGroupPanel)
            {
                _showGroupPanel = !_showGroupPanel;
                //změna viditelnosti groupovacího řádku
                OnDxShowGroupPanelChanged();
            }
            //nexistuje samostaná eventa pro změnu viditelnosti groupovací řádky, proto ošetřuji zde.
            if (OptionsView.ShowAutoFilterRow != _showFilterRow)
            {
                _showFilterRow = !_showFilterRow;
                //změna viditelnosti filtrovacího řádku
                OnDxShowFilterRowChanged();
            }
        }

        private void _OnCustomRowCellEdit(object sender, CustomRowCellEditEventArgs e)
        {
            if (GridViewColumns == null) return; //STR2023.03.14 - chybějící test na null při změně datového zdroje - aplikace jiné šablony s jinými sloupci

            //tohle dá stejný Edit do řádkového filtru jako je v cell
            //if (e.RowHandle == DevExpress.XtraGrid.GridControl.AutoFilterRowHandle)
            //    e.RepositoryItem = e.Column.ColumnEdit;

            if (e.RowHandle == DevExpress.XtraGrid.GridControl.AutoFilterRowHandle)
            {
                //Tady se snažím vytvořit nový pro řádkový filtr, protože pokud byl ReposotoryItem typu RepositoryItemImageComboBox a byla zobrazena jen ikonka bez textu, tak se ve filtru nezobrazoval text s ikonkou...
                var gvColumn = GridViewColumns.FirstOrDefault(x => x.FieldName == e.Column.FieldName);
                if (gvColumn != null)
                {
                    var repositoryItem = _CreateRepositoryItem(gvColumn, true);
                    if (repositoryItem != null)
                    {
                        //POZOR, neukládat do RepositoryItems nadbytečně! velký memmory leaks
                        //if (this.GridControl.RepositoryItems.Contains(repositoryItem))
                        //{
                        //    this.GridControl.RepositoryItems.Add(repositoryItem);    //musím ho opravdu přidávat sem, nestačí ho podstrčit e?
                        //}
                        e.RepositoryItem = repositoryItem;
                    }
                }
            }
        }

        private void _OnKeyPress(object sender, KeyPressEventArgs e)
        {
            List<char> specialChars = new List<char>(new char[] { '<', '!', '=', '>', '%', '.', ',', '+', '-' });

            if (char.IsLetterOrDigit(e.KeyChar) || specialChars.Contains(e.KeyChar))
            {//první znak v řádkovém filtru -> zkusím skočit do řádkového filtru
                if (TrySetFocusToFilterRow(LastFocusedColumnInRowFilter))
                {
                    e.Handled = true;
                    ShowEditorByKeyPress(e);    //activuje editor pro editaci (nastaví kurzor a vloží první písmeno)
                }
            }
        }

        private void _OnKeyDown(object sender, KeyEventArgs e)
        {
            //TODO zde bude lokální obsluhu.
            //budu asi dál posílat i když oblsoužím? ale nastavím e.Handled = true;
            if (sender is DxGridView view)
            {
                if (view.FocusedRowHandle == 0 && e.KeyData == Keys.Up)
                {//skok do řádkového filtru
                    e.Handled = TrySetFocusToFilterRow(LastFocusedColumnInRowFilter);
                }

                if (IsFilterRowActive && e.KeyData == Keys.Enter) _ApplyColumnsFilter();
            }
            LastKeyCodeDown = e.KeyData;

            OnKeyDown(this, e);
        }

        private void gridControl1_ProcessGridKey(object sender, KeyEventArgs e)
        {
            ColumnView view = (sender as GridControl).FocusedView as ColumnView;
            if (view == null) return;
            if (e.KeyCode == Keys.Delete && e.Control && view.Editable && view.SelectedRowsCount > 0)
            {
                if (view.ActiveEditor != null) return; //Prevent record deletion when an in-place editor is invoked
                e.Handled = true;
                if (DevExpress.XtraEditors.XtraMessageBox.Show("Record deletion", "Delete?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    view.DeleteSelectedRows();
            }
        }

        private void _OnClick(object sender, EventArgs e)
        {
            DevExpress.Utils.DXMouseEventArgs ea = e as DevExpress.Utils.DXMouseEventArgs;
            if (ea.Button != MouseButtons.Left) return; //Obsluha poze Levého tlačítka. Na pravý nechci nic dělat!

            DxGridView view = sender as DxGridView;
            GridHitInfo info = view.CalcHitInfo(ea.Location);

            if (info.InRow && !info.InRowCell && IsFilterRow(info.RowHandle) && HasRowFilterExpression)
            {
                //Křížek v řádkovém filtru ->smazat řádkový filtr
                this.ActiveFilterString = "";
                ClearColumnsFilter();
            }

            if (info.Column?.FieldName == GROUPBUTTON)
            {
                int group = GetGroupRow(info.RowHandle);
                int groupId = _GetGroupIdRow(info.RowHandle);
                int parentGroupId = _GetParentGroupIdRow(info.RowHandle);
                GroupRowInfo gInfo = GroupRowInfos.FirstOrDefault(x => x.GroupId == groupId);

                if (gInfo != null)
                {
                    gInfo.Expanded = !gInfo.Expanded;
                    if (!gInfo.Expanded) _SetAllChildGroupsColapsed(gInfo.GroupId);
                    if (!gInfo.DataRowsLoaded
                        && _CanLoadGroupRows(gInfo.Group))
                    {
                        //načti data pro groupu
                        WaitForNextRows = true;
                        int? groupPrimaryKey = _GetGroupPrimaryKey(info.RowHandle);
                        OnDxLoadGroupRows(groupId, gInfo.Group, false, groupPrimaryKey);
                        gInfo.DataRowsLoaded = true;
                    }
                }
                view.RefreshData();
            }
        }

        private void _OnDoubleClick(object sender, EventArgs e)
        {
            DevExpress.Utils.DXMouseEventArgs ea = e as DevExpress.Utils.DXMouseEventArgs;
            if (ea.Button != MouseButtons.Left) return; //Obsluha poze Levého tlačítka. Na pravý nechci nic dělat!

            GridView view = sender as GridView;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo info = view.CalcHitInfo(ea.Location);
            //DxGridHitInfo info = (DxGridHitInfo)view.CalcHitInfo(ea.Location);

            bool modifierKeyCtrl = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control;
            bool modifierKeyAlt = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt;
            bool modifierKeyShift = System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift;
            if (info.InRow || info.InRowCell)
            {
                if (CheckBoxRowSelect)
                {
                    // if (view.IsRowSelected(info.RowHandle) && modifierKeyCtrl)  //když je použit chceckBox, tak nechci selectovat řádek při ctrl+doubleClick - zpět zruším select řádku. Není to pěkné ale je to zatím jediná možnost
                    // {
                    //     view.UnselectRow(info.RowHandle);
                    // }
                }
                else
                {
                    if (!view.IsRowSelected(info.RowHandle))  //v případě chceckBoxu nedělat. Pokud bude použit check box, tak budeme využívat aktivní řádek místo selectovaného.
                    {
                        //nastvává v kombinaci ctrl+doubleClick
                        //Chci mít vždy selectovaný řádek nad kterým provádím doubleClick -> prvně se vyvolá _OnSelectionChanged -> server má nastaveny selectovavaný řádek
                        view.SelectRow(view.FocusedRowHandle);
                    }
                }
                OnDoubleClick(info.Column?.FieldName ?? null, GetDataSourceRowIndex(info.RowHandle), modifierKeyCtrl, modifierKeyAlt, modifierKeyShift);  //Zavoláme public event
            }
        }
        private void _OnFocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            bool isPrevFilterRow = this.IsFilterRow(e.PrevFocusedRowHandle);

            LastActiveRowIndex = this.IsFilterRow(e.PrevFocusedRowHandle) ? e.PrevFocusedRowHandle : GetDataSourceRowIndex(e.PrevFocusedRowHandle);
            ActualActiveRowIndex = this.IsFilterRow(e.FocusedRowHandle) ? e.FocusedRowHandle : GetDataSourceRowIndex(e.FocusedRowHandle);

            if (isPrevFilterRow) _ApplyColumnsFilter();
            OnFocusedRowChanged(LastActiveRowIndex, ActualActiveRowIndex);
        }
        private void _OnFocusedColumnChanged(object sender, FocusedColumnChangedEventArgs e)
        {
            if (IsFilterRowActive && FocusedColumn != null) LastFocusedColumnInRowFilter = FocusedColumn?.FieldName;
        }

        /// <summary>
        /// Změna selectovaných řádků/sloupců. Zde již mám naplněn this.GetSelectedRows(). Vhodnější než FocusedRowChanged. 
        /// https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.Views.Base.ColumnView.SelectionChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnSelectionChanged(object sender, DevExpress.Data.SelectionChangedEventArgs e)
        {
            OnSelectionChanged(this.SelectedRows);
        }
        private void _OnTopRowChanged(object sender, EventArgs e)
        {
            //Console.WriteLine(GetLastVisibleRowHandle() + " " + IsLastRowVisible());
            //Console.WriteLine("TopRowIndex" + TopRowIndex);
            //CheckNeedMoreRows();
            //StorePropertiesBeforeLoadMoreRows();

            //Console.WriteLine("TopRowIndex: " + TopRowIndex);
            //invaliduji horní 4 řádky, kvuli vykreslování "Jen odlišné" merge buňek. Kdy se snažím aby byla hodnota jen v horním řádku.
            this.InvalidateRow(TopRowIndex + 3);
            this.InvalidateRow(TopRowIndex + 2);
            this.InvalidateRow(TopRowIndex + 1);
            this.InvalidateRow(TopRowIndex);
        }

        private void _OnInvalidValueException(object sender, InvalidValueExceptionEventArgs e)
        {
            e.ExceptionMode = ExceptionMode.NoAction;   //Zobrazení jen tooltip při najetí error ikony v řádkovém filtru při nevalidní hodnotě. 
        }

        private void _OnValidatingEditor(object sender, BaseContainerValidateEditorEventArgs e)
        {
            ColumnView view = sender as ColumnView;
            GridColumn column = (e as EditFormValidateEditorEventArgs)?.Column ?? view.FocusedColumn;
            var iColumn = GetIGridViewColumn(column);
            if (view.FocusedRowHandle == DevExpress.XtraGrid.GridControl.AutoFilterRowHandle)
            {//validace řádkové filtru
                if (iColumn != null && (iColumn.IsDateTimeColumnType || iColumn.IsNumberColumnType))
                {//string or number
                    //Validate hodnoty pro filtr, ještě před tím, než ho budu parsovat a zadávat do filtru

                    string strVal = e.Value.ToString();
                    if (string.IsNullOrEmpty(strVal) || IsWildCardNull(strVal))
                    {
                        //Je prázdné nebo je zadáno <NULL>
                        view.ClearColumnErrors();
                        return;
                    }

                    //Odstranit případný operátor ze začátku
                    var condition = _GetConditionAndTrimStringExpression(ref strVal, iColumn);

                    StringBuilder sbErrorMsg = new StringBuilder();
                    //číslo
                    if (iColumn.IsNumberColumnType)
                    {
                        if (iColumn.IsNumberIntegerColumnType)
                        {//celočíselné
                            if (_TryParseDotsInterval(strVal, out long firstNumberLong, out long secondNumberLong))
                            {//interval ..

                                if (secondNumberLong < firstNumberLong)
                                {
                                    sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtNumberFirstNumberIsHigherThanSecond));
                                    e.Valid = false;
                                }
                            }
                            else if (!long.TryParse(strVal, out _))
                            {
                                sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtNumberValueNotValid));
                                e.Valid = false;
                            }
                        }
                        else
                        {
                            if (_TryParseDotsInterval(strVal, out decimal firstNumber, out decimal secondNumber))
                            {//interval ..
                                if (secondNumber < firstNumber)
                                {
                                    sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtNumberFirstNumberIsHigherThanSecond));
                                    e.Valid = false;
                                }
                            }
                            else if (!decimal.TryParse(strVal, out decimal _))
                            {
                                sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtNumberValueNotValid));
                                e.Valid = false;
                            }
                        }
                    }
                    //datum
                    else if (iColumn.IsDateTimeColumnType)
                    {
                        if (_TryParseDotsInterval(strVal, out DateTime startDate, out DateTime endDatetime))
                        {//interval ..
                            if (startDate.Year < _minYearForFilter)
                            {
                                sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtCalendarYearOutOfRange));
                                e.Valid = false;
                            }
                            if (endDatetime.Year < _minYearForFilter)
                            {
                                sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtCalendarYearOutOfRange));
                                e.Valid = false;
                            }
                            if (endDatetime < startDate)
                            {
                                sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtCalendarStartDateIsHigherThanEndDate));
                                e.Valid = false;
                            }
                        }
                        else if (_TryParseDateTime(strVal, out DateTime resultDateTime))
                        {//datum v jakékoliv podobě
                            if (resultDateTime.Year < _minYearForFilter)
                            {
                                sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtCalendarYearOutOfRange));
                                e.Valid = false;
                            }
                        }
                        else
                        {
                            //nevalidní, neprošlo ani jední parserem
                            sbErrorMsg.AppendLine(DxComponent.Localize(MsgCode.TxtCalendarDateNotValid));
                            e.Valid = false;
                        }
                    }
                    if (!e.Valid && !string.IsNullOrEmpty(sbErrorMsg.ToString())) e.ErrorText = sbErrorMsg.ToString();
                }

                if (!e.Valid) { view.SetColumnError(column, e.ErrorText); }
                else
                {
                    view.ClearColumnErrors();
                }
            }
        }

        internal IGridViewColumn GetIGridViewColumn(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) return null;
            return GridViewColumns?.FirstOrDefault(x => x.FieldName == columnName);
        }

        private IGridViewColumn GetIGridViewColumn(GridColumn gridColumn)
        {
            if (gridColumn == null) return null;
            return GetIGridViewColumn(gridColumn.FieldName);
        }

        #region Filtering a vše okolo

        /// <summary>
        /// Minimální rok který lze zadat pro filtrování
        /// </summary>
        private const int _minYearForFilter = 1753;
        /// <summary>
        /// zástupný znak pro null
        /// </summary>
        internal const string WildCardNull = "<NULL>";
        /// <summary>
        /// Porovníní řetězce s <see cref="WildCardNull"/>. Je to IgnoreCase.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool IsWildCardNull(string value) { if (value == null) return false; else return value.Equals(WildCardNull, StringComparison.InvariantCultureIgnoreCase); }
        /// <summary>
        /// Výraz filtrovacího řádku
        /// </summary>
        public string RowFilterExpression { get { return _GetConverterdRowFilterExpression(this.ActiveFilterCriteria); } set { _SetConverterdRowFilterExpression(value); } }
        /// <summary>
        /// Příznak zda je filtrovací řádek neaktivní
        /// </summary>
        public bool RowFilterIsInactive { get { return !this.ActiveFilterEnabled; } set { this.ActiveFilterEnabled = !value; } }
        /// <summary>
        /// Je nějaký filtrovací výraz, jedno zda aktivní/neaktivní?
        /// </summary>
        public bool HasRowFilterExpression => (!string.IsNullOrEmpty(ActiveFilterString));

        /// <summary>
        /// Uchovává původní výraz zadaný uživatelem (value) a výsledný criteria výraz (key) pro řádkový filtr pro jeden sloupec. 
        /// </summary>
        private Dictionary<string, string> _convertedColumnFilterCache = new Dictionary<string, string>();
        /// <summary>
        /// Uchovává původní výraz zadaný uživatelem (value) a výsledný criteria výraz (key) pro celý řádkový filtr. 
        /// </summary>
        private Dictionary<string, string> _convertedRowFilterCache = new Dictionary<string, string>();

        /// <summary>
        /// Voláno před aplikací filtru do GridView, možnost sestavení vlastní podmínky pro filtr
        /// </summary>
        /// <param name="column"></param>
        /// <param name="condition"></param>
        /// <param name="_value"></param>
        /// <param name="strVal"></param>
        /// <returns></returns>
        protected override DevExpress.Data.Filtering.CriteriaOperator CreateAutoFilterCriterion(DevExpress.XtraGrid.Columns.GridColumn column, DevExpress.XtraGrid.Columns.AutoFilterCondition condition, object _value, string strVal)
        {
            //1. změna základního operátoru. Pro všechny sloupce stejné. Pokud níže vyhodnotí, že je condition potřeba nastavit jinak, tak se tak uděje.
            var iColumn = GetIGridViewColumn(column);
            AutoFilterCondition conditionFromString = _GetConditionAndTrimStringExpression(ref strVal, iColumn);
            if (conditionFromString != AutoFilterCondition.Default)
            {
                condition = conditionFromString;
                _value = strVal;
            }

            //2. různé koverze. Intervaly, částečně zadané datumy...
            CriteriaOperator resultConvertedCriteriaOperator = null;
            bool isBetweenOperatator = false;

            if (iColumn.IsStringColumnType)
            {//string
                if (_TryParseDotsInterval(strVal, out string firstPart, out string secondPart))
                {//interval ..
                    resultConvertedCriteriaOperator = new BetweenOperator(column.FieldName, firstPart, secondPart);
                    isBetweenOperatator = true;
                }
            }
            else if (iColumn.IsNumberColumnType)
            {//čísla
                if (iColumn.IsNumberIntegerColumnType && _TryParseDotsInterval(strVal, out long firstPartLong, out long secondPartLong))
                {//interval celočíselný ..
                    resultConvertedCriteriaOperator = new BetweenOperator(column.FieldName, firstPartLong, secondPartLong);
                    isBetweenOperatator = true;
                }
                else if (_TryParseDotsInterval(strVal, out decimal firstPartDecimal, out decimal secondPartDecimal))
                {//interval deciaml..
                    resultConvertedCriteriaOperator = new BetweenOperator(column.FieldName, firstPartDecimal, secondPartDecimal);
                    isBetweenOperatator = true;
                }
            }
            else if (iColumn.IsDateTimeColumnType)
            {//datum
                if (_TryParseDotsInterval(strVal, out DateTime startDate, out DateTime endDatetime))
                {//interval ..
                    resultConvertedCriteriaOperator = new BetweenOperator(column.FieldName, startDate, endDatetime);
                    isBetweenOperatator = true;
                }
                else if (_TryParseDateTime(strVal, out DateTime dateTime, out int? day, out int? month, out int year, out int? hour, out int? minute, out int? second))
                {//jakýkoliv datum i nekompletní
                    DateTime minDateTime = dateTime;    //parse automaticky vrací minima
                    DateTime maxDateTime = _GetMaxDateTime(year, month, day, hour, minute, second);

                    //Pokud je operator = (rovná se), nebo nerovná se a je zadán datum bez kompletního času (nemá sekundy), tak se musí převést na BetweenOperator, protože chceme celý den (všechny časy)
                    if ((condition == AutoFilterCondition.Equals || condition == AutoFilterCondition.DoesNotEqual) && second == null)
                    {//převod na interval
                        resultConvertedCriteriaOperator = new BetweenOperator(column.FieldName, minDateTime, maxDateTime);
                        isBetweenOperatator = true;
                    }
                    else
                    {
                        //>,=>,<,<=
                        DateTime dt = minDateTime;
                        if (condition == AutoFilterCondition.Greater || condition == AutoFilterCondition.LessOrEqual)
                        {
                            // >, <=
                            dt = maxDateTime;
                        }
                        else if (condition == AutoFilterCondition.Less || condition == AutoFilterCondition.GreaterOrEqual)
                        {
                            //<, >=
                            dt = minDateTime;
                        }

                        _TryConvertAutoFilterConditionToBinaryOperatorType(condition, out BinaryOperatorType bTypeCondition);
                        resultConvertedCriteriaOperator = new BinaryOperator(column.FieldName, dt, bTypeCondition);
                    }
                }
            }

            //společné ošetření <NULL>
            if (object.ReferenceEquals(resultConvertedCriteriaOperator, null)
                 && IsWildCardNull(strVal))
            {
                if (condition == AutoFilterCondition.Equals || condition == AutoFilterCondition.DoesNotEqual)
                {
                    _TryConvertAutoFilterConditionToBinaryOperatorType(condition, out BinaryOperatorType bTypeCondition);
                    resultConvertedCriteriaOperator = new BinaryOperator(column.FieldName, WildCardNull, bTypeCondition);
                }
                else
                {
                    //když není při zadaném <NULL> operátor rovná nebo nerovná se, tak mažu. Je to případ, kdy přepínám operátor z vyplněného =<NULL> na něco jiného.
                    //Dle dohody s KOU je tato varianta průchozí a počítá s tím, že uživatel bude podmínku pro <NULL> spíše klikat z menu, než jej zadávat na klávesnici
                    return null;
                }
            }

            if (!object.ReferenceEquals(resultConvertedCriteriaOperator, null))
            {
                //koncová obsluha intervalu obecně
                if (isBetweenOperatator)
                {//interval
                    //když není rovná nebo nerovná se, tak nastav rovná se (výchozí operátor pro interval)
                    if (!(condition == AutoFilterCondition.Equals || condition == AutoFilterCondition.DoesNotEqual))
                    {
                        condition = AutoFilterCondition.Equals;
                    }
                    //zařízení negace
                    resultConvertedCriteriaOperator = condition == AutoFilterCondition.Equals ? (CriteriaOperator)resultConvertedCriteriaOperator : (CriteriaOperator)resultConvertedCriteriaOperator.Not();
                }

                //Provedl jsem nějakou konverzi, uložím si ji, abych mohl udělat kovnverzi zpět. a konverzi vrátím jako výsledek.
                _StoreConvertedColumnFiter(strVal, resultConvertedCriteriaOperator.ToString());
                //pokud je změna operatoru, tak ho nastavuji přímo do column, protože condition se nikde neaplikuje (nejde do base.)
                if (column.OptionsFilter.AutoFilterCondition != condition) column.OptionsFilter.AutoFilterCondition = condition;

                return resultConvertedCriteriaOperator;
            }

            //bez konverze, maximálně základní změna operátoru
            return base.CreateAutoFilterCriterion(column, condition, _value, strVal);
        }
        /// <summary>
        /// Vyhodnotí, zda výraz obsahuje znak (znaky), které vyjadřují změnu podmínku. Pokud najde tak tak vrací podmínku a ze string výrazu ji odstraní (pokud je na začátku). Pokud nenanajde, tak vrací <see cref="AutoFilterCondition.Default"/>
        /// Některé vyhodnotcuje se i typ sloupce. Například % (obsahuje), je validní jen pro string sloupce.
        /// </summary>
        /// <param name="stringExpression"></param>
        /// <param name="gridViewColumn"></param>
        /// <returns></returns>
        private AutoFilterCondition _GetConditionAndTrimStringExpression(ref string stringExpression, IGridViewColumn gridViewColumn)
        {
            //dva znaky
            if (stringExpression.StartsWith("!=") || stringExpression.StartsWith("<>")) { stringExpression = stringExpression.Substring(2); return AutoFilterCondition.DoesNotEqual; }
            if (stringExpression.StartsWith(">=")) { stringExpression = stringExpression.Substring(2); return AutoFilterCondition.GreaterOrEqual; }
            if (stringExpression.StartsWith("<=")) { stringExpression = stringExpression.Substring(2); return AutoFilterCondition.LessOrEqual; }
            if (stringExpression.StartsWith("!%") && gridViewColumn.IsStringColumnType) { stringExpression = stringExpression.Substring(2); return AutoFilterCondition.DoesNotContain; }
            if (stringExpression.StartsWith("!*") && gridViewColumn.IsStringColumnType) { stringExpression = stringExpression.Substring(2); return AutoFilterCondition.NotLike; }    //stejné jako "nezačíná"

            //Jeden znak
            if (stringExpression.StartsWith("=")) { stringExpression = stringExpression.Substring(1); return AutoFilterCondition.Equals; }
            if (stringExpression.StartsWith(">")) { stringExpression = stringExpression.Substring(1); return AutoFilterCondition.Greater; }
            if (stringExpression.StartsWith("<") && !IsWildCardNull(stringExpression)) { stringExpression = stringExpression.Substring(1); return AutoFilterCondition.Less; }
            if (stringExpression.StartsWith("%") && gridViewColumn.IsStringColumnType) { stringExpression = stringExpression.Substring(1); return AutoFilterCondition.Contains; }
            if (stringExpression.StartsWith("*") && gridViewColumn.IsStringColumnType) { stringExpression = stringExpression.Substring(1); return AutoFilterCondition.BeginsWith; }
            if (stringExpression.StartsWith("!") && gridViewColumn.IsStringColumnType) { stringExpression = stringExpression.Substring(1); return AutoFilterCondition.NotLike; } //stejné jako "nezačíná"

            return AutoFilterCondition.Default;
        }

        /// <summary>
        /// Parsuje interval datumů. Např: 2020..2025,8.2020..2023 5.2.2022..2035. Na obou stranách můžou býr všechny varianty kompletního/nekompletního datumu.
        /// endDateTime vrací vždy maximální pro daná zadaný formát. např: pro "2023" => 31.12.2023 23:59:59. Jediná povinná část datumu je rok, ostatná hodnoty pokud nejsou zadány, tak se vezmou maximální.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>        
        private static bool _TryParseDotsInterval(string input, out DateTime startDateTime, out DateTime endDateTime)
        {
            //interval
            startDateTime = DateTime.MinValue;
            endDateTime = DateTime.MinValue;

            if (!_TryParseDotsInterval(input, out string firstPart, out string secondPart)) { return false; }

            //firstPart.
            bool startDateTimeParsed = _TryParseDateTimeAndGetMinValue(firstPart, out startDateTime);

            //secondPart           
            bool endDateTimeParsed = _TryParseDateTimeAndGetMaxValue(secondPart, out endDateTime);
            return startDateTimeParsed && endDateTimeParsed;
        }
        /// <summary>
        /// Parsuje interval čísel. Např.: "-200..500", "30,5..99,9" 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="firstNumber"></param>
        /// <param name="secondNumber"></param>
        /// <returns></returns>
        private static bool _TryParseDotsInterval(string input, out decimal firstNumber, out decimal secondNumber)
        {
            //interval
            firstNumber = decimal.MinValue;
            secondNumber = decimal.MinValue;
            bool firstNumberParsed = false;
            bool secondNumberParsed = false;

            if (!_TryParseDotsInterval(input, out string firstPart, out string secondPart)) { return false; }

            //firstPart.
            if (decimal.TryParse(firstPart, out firstNumber))
            {
                firstNumberParsed = true;
            }
            //secondPart
            if (decimal.TryParse(secondPart, out secondNumber))
            {
                secondNumberParsed = true;
            }

            return firstNumberParsed && secondNumberParsed;
        }
        /// <summary>
        /// Parsuje interval celočíselných čísel. Např.: "-200..500"
        /// </summary>
        /// <param name="input"></param>
        /// <param name="firstNumber"></param>
        /// <param name="secondNumber"></param>
        /// <returns></returns>
        private static bool _TryParseDotsInterval(string input, out long firstNumber, out long secondNumber)
        {
            //interval
            firstNumber = long.MinValue;
            secondNumber = long.MinValue;
            bool firstNumberParsed = false;
            bool secondNumberParsed = false;

            if (!_TryParseDotsInterval(input, out string firstPart, out string secondPart)) { return false; }

            //firstPart.
            if (long.TryParse(firstPart, out firstNumber))
            {
                firstNumberParsed = true;
            }

            //secondPart
            if (long.TryParse(secondPart, out secondNumber))
            {
                secondNumberParsed = true;
            }

            return firstNumberParsed && secondNumberParsed;
        }

        /// <summary>
        /// Parse (split) obecného stringu na dvě string části oddělené dvěma tečkami. "První část..Druhá část"
        /// </summary>
        /// <param name="input"></param>
        /// <param name="firstPart"></param>
        /// <param name="secondPart"></param>
        /// <returns></returns>
        private static bool _TryParseDotsInterval(string input, out string firstPart, out string secondPart)
        {
            firstPart = "";
            secondPart = "";

            string pattern = @"^(.*?)\.\.(.*?)$"; // Regex výraz, "První část..Druhá část";
            Match match = Regex.Match(input, pattern);
            if (!match.Success) { return false; }

            firstPart = match.Groups[1].Value;
            secondPart = match.Groups[2].Value;

            return true;
        }

        private static bool _TryParseDateTimeAndGetMinValue(string value, out DateTime dateTime)
        {
            //tohle z principu vrací minimální hodnoty.
            bool parsed = _TryParseDateTime(value, out dateTime, out int? day, out int? month, out int year, out int? hour, out int? minute, out int? second);

            return parsed;
        }
        private static bool _TryParseDateTimeAndGetMaxValue(string value, out DateTime dateTime)
        {
            bool parsed = _TryParseDateTime(value, out dateTime, out int? day, out int? month, out int year, out int? hour, out int? minute, out int? second);
            if (parsed)
            {
                dateTime = _GetMaxDateTime(year, month, day, hour, minute, second);
            }
            return parsed;
        }

        /// <summary>
        /// Parsuje všechny podporované formáty datum
        /// </summary>
        /// <param name="value"></param>
        /// <param name="resultDateTime"></param>
        /// <returns></returns>
        private static bool _TryParseDateTime(string value, out DateTime resultDateTime)
        {
            return _TryParseDateTime(value, out resultDateTime, out _, out _, out _, out _, out _, out _);
        }

        /// <summary>
        /// Parsuje všechny podporované formáty datumu a vrací zároveň jednotlivé části, které ukazují na to, zda byli v řetězci obsaženy či nikoliv. Slouží pro rozpoznání zda byl zadán celý datum nebo jen jeho části.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="resultDateTime"></param>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static bool _TryParseDateTime(string value, out DateTime resultDateTime, out int? day, out int? month, out int year, out int? hour, out int? minute, out int? second)
        {
            resultDateTime = DateTime.MinValue;
            bool parsed = false;
            day = month = hour = minute = second = null;
            year = DateTime.MinValue.Year;

            // Formát datumu a času
            const string format_dmyhmsE = "d.M.yyyy H:m:s";
            const string format_dmyhmsU = "M/d/yyyy H:m:s";
            const string format_dmyhmE = "d.M.yyyy H:m";
            const string format_dmyhmU = "M/d/yyyy H:m";
            const string format_dmyhE = "d.M.yyyy H";
            const string format_dmyhU = "M/d/yyyy H";
            const string format_dmyE = "d.M.yyyy";
            const string format_dmyU = "M/d/yyyy";
            const string format_myE = "M.yyyy";
            const string format_myU = "M/yyyy";
            string format_y = "yyyy";

            if (DateTime.TryParseExact(value, format_dmyhmsE, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime)
                || DateTime.TryParseExact(value, format_dmyhmsU, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime))
            {
                parsed = true;
                day = resultDateTime.Day;
                month = resultDateTime.Month;
                year = resultDateTime.Year;
                hour = resultDateTime.Hour;
                minute = resultDateTime.Minute;
                second = resultDateTime.Second;
            }
            else if (DateTime.TryParseExact(value, format_dmyhmE, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime)
                 || DateTime.TryParseExact(value, format_dmyhmU, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime))
            {
                parsed = true;
                day = resultDateTime.Day;
                month = resultDateTime.Month;
                year = resultDateTime.Year;
                hour = resultDateTime.Hour;
                minute = resultDateTime.Minute;
            }
            else if (DateTime.TryParseExact(value, format_dmyhE, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime)
                 || DateTime.TryParseExact(value, format_dmyhU, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime))
            {
                parsed = true;
                day = resultDateTime.Day;
                month = resultDateTime.Month;
                year = resultDateTime.Year;
                hour = resultDateTime.Hour;
            }
            else if (DateTime.TryParseExact(value, format_dmyE, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime)
                 || DateTime.TryParseExact(value, format_dmyU, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime))
            {
                parsed = true;
                day = resultDateTime.Day;
                month = resultDateTime.Month;
                year = resultDateTime.Year;
            }
            else if (DateTime.TryParseExact(value, format_myE, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime)
                 || DateTime.TryParseExact(value, format_myU, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime))
            {
                parsed = true;
                month = resultDateTime.Month;
                year = resultDateTime.Year;
            }
            else if (DateTime.TryParseExact(value, format_y, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDateTime))
            {
                parsed = true;
                year = resultDateTime.Year;
            }
            return parsed;
        }

        /// <summary>
        /// Vrací max datum. Slouží pro vkládání do filtru.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static DateTime _GetMaxDateTime(int year, int? month, int? day, int? hour, int? minute, int? second)
        {
            if (month == null)
            {
                //nemám měsíce
                month = 12;
            }
            if (day == null)
            {
                //nemám dny
                day = DateTime.DaysInMonth(year, month.Value);
            }
            if (hour == null)
            {
                //nemám hodiny
                hour = 23;
                minute = 59;
                second = 59;
            }
            if (hour == null)
            {
                //nemám hodiny
                hour = 23;
                minute = 59;
                second = 59;
            }
            else if (hour != null && minute == null)
            {
                //nemám minuty
                minute = 59;
                second = 59;
            }
            else if (hour != null && minute != null && second == null)
            {
                //nemám sekundy
                second = 59;
            }
            return new DateTime(year, month.Value, day.Value, hour.Value, minute.Value, second.Value);
        }

        /// <summary>
        /// Slouží pro převod z AutoFilterCondition do BinaryOperatorType. Lze používat jen pro základní operátory rovná se, menší, větší... 
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="bOperator"></param>
        /// <returns></returns>
        private bool _TryConvertAutoFilterConditionToBinaryOperatorType(AutoFilterCondition condition, out BinaryOperatorType bOperator)
        {
            switch (condition)
            {
                case AutoFilterCondition.Equals:
                    bOperator = BinaryOperatorType.Equal;
                    return true;
                case AutoFilterCondition.DoesNotEqual:
                    bOperator = BinaryOperatorType.NotEqual;
                    return true;
                case AutoFilterCondition.Greater:
                    bOperator = BinaryOperatorType.Greater;
                    return true;
                case AutoFilterCondition.GreaterOrEqual:
                    bOperator = BinaryOperatorType.GreaterOrEqual;
                    return true;
                case AutoFilterCondition.Less:
                    bOperator = BinaryOperatorType.Less;
                    return true;
                case AutoFilterCondition.LessOrEqual:
                    bOperator = BinaryOperatorType.LessOrEqual;
                    return true;
                //case AutoFilterCondition.Default:
                //case AutoFilterCondition.Like:
                //case AutoFilterCondition.Contains:
                //case AutoFilterCondition.BeginsWith:
                //case AutoFilterCondition.EndsWith:
                //case AutoFilterCondition.DoesNotContain:
                //case AutoFilterCondition.NotLike:
                //    bOperator = BinaryOperatorType.Equal;
                //    return false;
                default:
                    bOperator = BinaryOperatorType.Equal;
                    return false;
            }
        }
        private void _StoreConvertedColumnFiter(string origin, string converted)
        {
            if (_convertedColumnFilterCache.ContainsKey(converted)) { _convertedColumnFilterCache.Remove(converted); }
            _convertedColumnFilterCache.Add(converted, origin);
        }

        private void _StoreConvertedRowFiter(string origin, string converted)
        {
            if (_convertedRowFilterCache.ContainsKey(converted)) { _convertedRowFilterCache.Remove(converted); }
            _convertedRowFilterCache.Add(converted, origin);
        }

        private string _GetConverterdRowFilterExpression(CriteriaOperator activeFilterCriteria)
        {
            if (object.ReferenceEquals(activeFilterCriteria, null)) return string.Empty;
            CriteriaOperator criteriaForPath = CriteriaOperator.Clone(activeFilterCriteria);
            CriteriaOperator resultCriteriaOperator = CriteriaPatcherConvertOperatorWithWildcard.Patch(criteriaForPath, false);

            //Využití ukládání konverze nyní je vypnuto
            //Nefungovalo potom  případ kdy přišel ze server nastaven filterString, který byl potřeba zkonverzovat (navazování vztahu a zadání "%neco")
            //uložím si konverzi (komponent->server)
            //_StoreConvertedRowFiter(activeFilterCriteria.ToString(), resultCriteriaOperator.ToString());

            return resultCriteriaOperator.ToString();
        }
        /// <summary>
        /// Nastaví ActiveFilterString. Pokusí se získat původní hodnotu z <see cref="_convertedRowFilterCache"/>. Pokud se to nepovede, tak se pokusí provést  konverze pomocí <see cref="CriteriaPatcherConvertOperatorWithWildcard"/>
        /// </summary>
        /// <param name="filterString"></param>
        private void _SetConverterdRowFilterExpression(string filterString)
        {
            //Využití ukládání konverze nyní je vypnuto
            //Nefungovalo potom  případ kdy přišel ze server nastaven filterString, který byl potřeba zkonverzovat (navazování vztahu a zadání "%neco")

            //string originRowFilter;
            //if (_convertedRowFilterCache.TryGetValue(filterString, out originRowFilter))
            //{
            //    this.ActiveFilterString = originRowFilter;
            //}
            //else
            //{
            CriteriaOperator criteriaOperator = CriteriaOperator.TryParse(filterString);
            criteriaOperator = CriteriaPatcherConvertOperatorWithWildcard.Patch(criteriaOperator, true);

            this.ActiveFilterString = criteriaOperator?.ToString() ?? filterString;
            //}
        }

        /// <inheritdoc/>
        public override void ClearColumnsFilter()
        {
            base.ClearColumnsFilter();
        }

        /// <summary>
        /// vlastní provedení filtru v GridView, přepsáním možno odstavit filtrovací mechanismus GridView
        /// </summary>
        /// <param name="updateMRU"></param>
        /// <param name="ignoreActiveFilter"></param>
        /// <param name="forceFindFilter"></param>
        protected override void ApplyColumnsFilterCore(bool updateMRU, bool ignoreActiveFilter, bool forceFindFilter = false)
        {
            if (EnableApplyColumnsFilter || _fireApplyColumnsFilter)
            {
                base.ApplyColumnsFilterCore(false, ignoreActiveFilter, forceFindFilter);    //tohle vyvoválá další události ohledně filtrování => _OnColumnFilterChanged => odeslání na server
                //RMC 0071608 09.07.2024 BROWSE2e - začlenění 2; Oďub, když se ruší filtr a přehled je prázdný (zadaný filtr, který neodpovídá žádným řádkům), tak se nevyvolá _OnColumnFilterChanged
                if (string.IsNullOrEmpty(this.ActiveFilterString) && this.RowCount == 0) { this.OnRowFilterChanged(); }
            }
        }

        /// <summary>
        /// Chci aplikovat řádkový filtr vždy kromě případu, že se pohybuji po řádkovém filtru buď myškou (změna buňky, nebo výběr oprátoru) a nebo se pohybuji pomocí Tab
        /// </summary>
        private bool EnableApplyColumnsFilter
        {
            get
            {
                bool isLastClickOnFilterRow = LastMouseDownGridHitInfo != null && this.IsFilterRow(LastMouseDownGridHitInfo.RowHandle) && LastMouseDownGridHitInfo.Column != null;
                bool isLastKeyTab = LastKeyCodeDown == Keys.Tab;
                return !(IsFilterRowActive && (isLastClickOnFilterRow || isLastKeyTab));
            }
        }
        private bool _fireApplyColumnsFilter = false;
        /// <summary>
        /// Chci vynutit aplikování řádkového filtru
        /// </summary>
        private void _ApplyColumnsFilter()
        {
            _fireApplyColumnsFilter = true;
            base.ApplyColumnsFilterCore();
            _fireApplyColumnsFilter = false;
        }
        protected override void OnActiveFilterChanged(object sender, EventArgs e)
        {
            base.OnActiveFilterChanged(sender, e);
        }

        /// <summary>
        /// Voláno po změně podmínek ve filtrovacím řádku
        /// Occurs when a column's filter condition changes. This event also raises when the Find Panel finishes its search.
        /// https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.Views.Base.ColumnView.ColumnFilterChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnColumnFilterChanged(object sender, EventArgs e)
        {
            this.OnRowFilterChanged();  //Vyvoláme naši Event o změně filtru
        }

        /// <summary>
        /// https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.Views.Base.ColumnView.CustomRowFilter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnCustomRowFilter(object sender, RowFilterEventArgs e)
        {
            if (!RowFilterCoreEnabled)
            {
                //tohle vypíní interní filtrování pomocí nastavení visible true každého řádku.
                // Make the current row visible.
                e.Visible = true;

                //mechanismus který schová datové řádky pro sbalené groupy
                ColumnView view = sender as ColumnView;
                int? parentGroupId = int.TryParse(view.GetListSourceRowCellValue(e.ListSourceRow, PARENTGROUPID).ToString(), out int result) ? result : (int?)null;
                var gInfo = GroupRowInfos.FirstOrDefault(i => i.GroupId == parentGroupId);
                if (gInfo != null)
                {
                    List<GroupRowInfo> groupRowInfoWithParents = new List<GroupRowInfo>();
                    GetGroupRowInfoWithParents(gInfo, groupRowInfoWithParents);
                    if (groupRowInfoWithParents.Any(x => x.Expanded == false))
                    {
                        e.Visible = false;
                    }
                }
                // Prevent default processing, so the row will be visible 
                // regardless of the view's filter.
                e.Handled = true;
            }
        }

        /// <summary>
        /// Prohledávání group struktury odspodu nahoru
        /// </summary>
        /// <param name="parentGroupRowInfo"></param>
        /// <param name="result"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private void GetGroupRowInfoWithParents(GroupRowInfo parentGroupRowInfo, List<GroupRowInfo> result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (parentGroupRowInfo is null)
            {
                throw new ArgumentNullException(nameof(parentGroupRowInfo));
            }

            if (!result.Contains(parentGroupRowInfo)) result.Add(parentGroupRowInfo);
            foreach (var groupRowInfoItem in GroupRowInfos)
            {
                if (parentGroupRowInfo.ParentGroupId != null && groupRowInfoItem.GroupId == parentGroupRowInfo.ParentGroupId.Value)
                {
                    result.Add(groupRowInfoItem);
                    GetGroupRowInfoWithParents(groupRowInfoItem, result);
                }
            }
        }

        #region Filternig - PopUpExcel
        private void _OnFilterPopupExcelCustomizeTemplate(object sender, DevExpress.XtraGrid.Views.Grid.FilterPopupExcelCustomizeTemplateEventArgs e)
        {
            //pokus ovlivnit vlastnoti zobrazovaného excel filtru
            return;
            e.Template.BackColor = System.Drawing.Color.FromArgb(0, 0, 120);
            //BeginInvoke(new Action(() => { (e.Template.Parent.Parent as XtraTabPage).PageEnabled = false; }));
            (e.Template.Parent.Parent as XtraTabPage).PageVisible = false;
        }

        private void _OnFilterPopupExcelData(object sender, FilterPopupExcelDataEventArgs e)
        {
            var column = GetIGridViewColumn(e.Column);
            if (column == null) { return; }
            if (column.IsEditStyleColumnType)
            {
                //Zde vytváří filtry na základě CodeTable pro ES, tak aby nic nechybělo ve výčtu. Pokud by to tu nebylo, tak by komponenta ve výběru ponechala jen ty prvky, které obsahuje datasource gridu.

                e.ClearData();// Remove the default data values from the filter menu.

                var resourceType = _GetResourceType(column);
                foreach (var code in column.CodeTable)
                {
                    var excelFilterDataItem = e.AddData(code.DisplayText, code.DisplayText.ToString());
                    if (column.EditStyleViewMode == EditStyleViewMode.Icon || column.EditStyleViewMode == EditStyleViewMode.IconText)
                    {
                        excelFilterDataItem.ImageIndex = _GetImageIndex(code.ImageName, ResourceImageSizeType.Small, resourceType, -1);
                    }
                }
            }
        }

        private void _OnFilterPopupExcelQueryFilterCriteria(object sender, FilterPopupExcelQueryFilterCriteriaEventArgs e)
        {
            //throw new NotImplementedException();
        }
        private void _OnFilterPopupExcelParseFilterCriteria(object sender, FilterPopupExcelParseFilterCriteriaEventArgs e)
        {
            //throw new NotImplementedException();
        }

        #endregion


        /// <summary>
        /// Filtrovací řádek se změnil <see cref="DxRowFilterChanged"/>
        /// </summary>
        public event DxGridRowFilterChangeHandler DxRowFilterChanged;
        /// <summary>
        /// Vyvolá event <see cref="DxRowFilterChanged"/>
        /// </summary>
        protected virtual void OnRowFilterChanged()
        {
            if (_silentMode) return;
            if (DxRowFilterChanged != null) DxRowFilterChanged(this, new DxGridRowFilterChangeEventArgs(this.RowFilterExpression, this.RowFilterIsInactive));
        }

        /// <summary>
        /// Předpis pro eventhandlery
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void DxGridRowFilterChangeHandler(object sender, DxGridRowFilterChangeEventArgs args);

        /// <summary>
        /// Argument pro eventhandlery
        /// </summary>
        public class DxGridRowFilterChangeEventArgs : EventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public DxGridRowFilterChangeEventArgs(string expression, bool isInactive)
            {
                this.Expression = expression;
                this.IsInactive = isInactive;
            }

            /// <summary>
            /// Filter v podobě Criterial Language
            /// </summary>
            public string Expression { get; private set; }
            /// <summary>
            /// Příznak zda je filtr aktivní či nikoliv
            /// </summary>
            public bool IsInactive { get; private set; }
        }


        #endregion

        #region sorting

        /// <summary>
        /// CustomColumnSort, volá se pro každý řádek. Využito pro vypnutí interního sort mechanismu
        /// https://supportcenter.devexpress.com/ticket/details/q239984/disabling-internal-gridview-sorting-algorithm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnCustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            if (!ColumnSortCoreEnabled)
            {
                //zajistí, že se nebude měnit řazení interním mechanismem
                e.Result = e.ListSourceRowIndex2 - e.ListSourceRowIndex1;
                e.Result = (e.Result > 0) ? 1 : ((e.Result < 0) ? -1 : 0);
                if (e.SortOrder == DevExpress.Data.ColumnSortOrder.Ascending)
                    e.Result = -e.Result;
                e.Handled = true;
            }
        }

        private void _OnStartSorting(object sender, EventArgs e)
        {
        }
        private void _OnEndSorting(object sender, EventArgs e)
        {
            GridView view = sender as GridView;
            foreach (DevExpress.XtraGrid.Columns.GridColumn gridColumn in view.Columns)
                _lastColumSortOrder[gridColumn.FieldName] = gridColumn.SortOrder;
        }
        /// <inheritdoc/>
        protected override void OnBeforeSorting()
        {
            base.OnBeforeSorting();
        }
        /// <inheritdoc/>
        protected override void OnAfterSorting()
        {
            base.OnAfterSorting();
        }
        /// <inheritdoc/>
        protected override void UpdateSorting()
        {
            base.UpdateSorting();
        }
        /// <inheritdoc/>
        protected override void RaiseStartSorting()
        {
            base.RaiseStartSorting();
        }
        /// <inheritdoc/>
        protected override void RaiseEndSorting()
        {
            base.RaiseEndSorting();
        }
        /// <inheritdoc/>
        protected override void RaiseCustomColumnSort(CustomColumnSortEventArgs e)
        {
            base.RaiseCustomColumnSort(e);
        }
        /// <inheritdoc/>
        protected override bool IsColumnAllowSort(DevExpress.XtraGrid.Columns.GridColumn column)
        {
            // přepsáním přijdeme o možnost custom sortingu, důležité je dovnitř poslat e.Handled=true a e.Result=1
            return base.IsColumnAllowSort(column);
        }
        /// <inheritdoc/>
        protected override void DoMouseSortColumn(DevExpress.XtraGrid.Columns.GridColumn column, System.Windows.Forms.Keys key)
        {
            // přepsáním přijdeme o symbol řazení v caption sloupce a informaci (column.Sortindex), jak je seřazeno
            base.DoMouseSortColumn(column, key);
        }

        #endregion

        #region Scroolling

        private int _lastScrollPosiotion = -1;
        /// <summary>
        /// Určuje pozici scrollBaru
        /// </summary>
        private int _lastTopRowindex = -1;
        private int _lastFocusedRowHandle = -1;
        private List<int> _lastSelectedRows = new List<int> { };

        private bool IsLastRowVisible()
        {
            bool result = false;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridViewInfo viewInfo = GetViewInfo() as DevExpress.XtraGrid.Views.Grid.ViewInfo.GridViewInfo;
            //return viewInfo.RowsInfo.Last().RowHandle;
            int? lastRowLandle = viewInfo.RowsInfo.LastOrDefault()?.RowHandle;

            result = lastRowLandle != null ? this.IsRowVisible(lastRowLandle.Value) == RowVisibleState.Visible : true;

            Console.WriteLine("IsLastRowVisible: " + result);
            return result;
        }

        private int GetLastVisibleRowHandle()
        {
            int first = this.TopRowIndex;
            int last = this.TopRowIndex;
            while (this.IsRowVisible(this.GetVisibleRowHandle(last)) != DevExpress.XtraGrid.Views.Grid.RowVisibleState.Hidden)
                last++;

            return last;
        }

        private void CheckNeedMoreRows()
        {
            if (IsLastRowVisible())
            {
                _OnLoadMoreRows();
            }
        }

        private void _OnLoadMoreRows()
        {
            if (CurrentRowCount == 0) return;
            StorePropertiesBeforeLoadMoreRows();
            this.OnDxLoadMoreRows();  //Vyvoláme naši Event
        }

        //internal void SetScrollPosition()
        //{
        //    Console.WriteLine("SetScrollPosition: " + _lastTopRowindex);

        //}

        internal void StorePropertiesBeforeLoadMoreRows()
        {
            _StoreScrollPosition();
            if (this.DxGridControl?.StorePropertiesDuringDatasourceChange == false) return;
            _StoreSelectedRows();
        }
        internal void RestorePropertiesBeforeLoadMoreRows()
        {
            _RestoreScrollPosition();
            if (this.DxGridControl?.StorePropertiesDuringDatasourceChange == false) return;
            _RestoreSelectedRows();
        }
        private void _StoreScrollPosition()
        {
            // _lastScrollPosiotion = this.ScrollInfo.VScrollPosition;
            _lastFocusedRowHandle = this.FocusedRowHandle;
            _lastTopRowindex = this.TopRowIndex;
        }
        private void _RestoreScrollPosition()
        {
            if (_lastTopRowindex > 0)
            {
                this.FocusedRowHandle = _lastFocusedRowHandle;
                this.TopRowIndex = _lastTopRowindex;
                //vymazat uložené hodnoty
                _lastFocusedRowHandle = -1;
                _lastTopRowindex = -1;
            }
        }
        private void _StoreSelectedRows()
        {
            _lastSelectedRows = this.SelectedRows;
        }
        private void _RestoreSelectedRows()
        {
            this.SelectedRows = _lastSelectedRows;
            _lastSelectedRows.Clear();
        }


        //----od daj
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
                }
            }
        }
        private int __TargetRowCount = 5000;
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
                }
            }
        }
        private bool __IsVirtualMode = false;

        /// <summary>
        /// Aktuální počet řádků
        /// </summary>
        protected int CurrentRowCount
        {
            //get { return this.DataSource?.Rows.Count ?? 0; }
            get { return ((System.Data.DataView)this.DataSource)?.Table?.Rows.Count ?? 0; }
        }
        /// <summary>
        /// Jsou načteny všechny řádky?
        /// </summary>
        protected bool HasAllRows
        {
            get { return (this.DataSource == null || CurrentRowCount >= __TargetRowCount); }
        }
        /// <summary>
        /// Čekám na donačtení dalších řádků => nebudu žádat o nové...
        /// </summary>
        internal bool WaitForNextRows
        {
            get { return __WaitForNextRows; }
            set
            {
                if (value != __WaitForNextRows)
                {
                    __WaitForNextRows = value;
                }
            }
        }
        private bool __WaitForNextRows = false;

        private bool _AllRowsLoaded;
        /// <summary>
        /// Příznak, že jsou načteny všechny řádky. Ovlivňuje možnost načítání dalších dat.
        /// </summary>
        public bool AllRowsLoaded { get { return _AllRowsLoaded; } set { _AllRowsLoaded = value; } }

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
                int visibleHeight = this.ViewRect.Height - 8;
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
            if (e.Info.ScrollBar.ScrollBarType == DevExpress.XtraEditors.ScrollBarType.Vertical && !AllRowsLoaded && !WaitForNextRows)
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
                    //     this.AddDataRowsAsync(loadCount);
                    Console.WriteLine($"LoadMoreRow: totalRange.Size: {totalRange.Size} visibleRange.Size: {visibleRange.Size}");
                    Task.Run(() => this._OnLoadMoreRows());
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
                }
            }
        }
        private decimal __ReserveRatio = 0.1m;
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
                }
            }
        }
        private decimal __RequestRatio = 1m;

        #endregion

        #region SummaryRow

        private DataRow _summaryRowData = null;
        private bool _showSummaryRow;
        /// <summary>
        /// Zobrazí/schová sumační řádek
        /// </summary>
        public bool ShowSummaryRow { get => _showSummaryRow; set { _showSummaryRow = value; _SetShowSummaryRow(); } }

        private void _SetShowSummaryRow()
        {
            if (OptionsView.ShowFooter != _showSummaryRow)
            {
                OptionsView.ShowFooter = _showSummaryRow;
            }
        }
        /// <summary>
        /// Nastavení hodnot summačního řádku
        /// </summary>
        /// <param name="summaryRowData"></param>
        public void SetSummaryRowValues(DataRow summaryRowData)
        {
            _summaryRowData = summaryRowData;
            this.InvalidateFooter();
        }
        /// <summary>
        /// Vyvolá metodu <see cref="OnSummaryRowForAllRows(EventArgs)"/> a event <see cref="DxSummaryRowForAllRows"/>
        /// </summary>
        private void RaiseSummaryRowForAllRows()
        {
            //if (_SilentMode) return;

            EventArgs args = new EventArgs();
            OnSummaryRowForAllRows(args);
            DxSummaryRowForAllRows?.Invoke(this, args);
        }
        /// <summary>
        /// Grid potřebuje informace o summách 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSummaryRowForAllRows(EventArgs args) { }
        /// <summary>
        /// Grid potřebuje informace o summách
        /// </summary>
        public event DxGridSummaryRowForAllRowsHandler DxSummaryRowForAllRows;

        #endregion

        #region GroupSummmary

        /// <summary>
        /// Nastaví sbalení/rozbalení skupiny
        /// </summary>
        /// <param name="groupLevel"></param>
        /// <param name="expanded"></param>
        /// <param name="recursive"></param>
        public void SetGroupExpanded(int groupLevel, bool expanded, bool recursive)
        {
            this.BeginUpdate();
            //this.SuspendLayout();
            try
            {
                var groupRowsByLevel = GetGroupRowsByLevel(this, groupLevel);
                if (recursive)
                {
                    int groupLevelRecursive = groupLevel;
                    while (groupRowsByLevel?.Count > 0)
                    {
                        foreach (var rowGroupHandle in groupRowsByLevel)
                        {
                            SetRowExpanded(rowGroupHandle, expanded);
                        }
                        groupLevelRecursive++;
                        groupRowsByLevel = GetGroupRowsByLevel(this, groupLevelRecursive);
                    }
                }
                else
                {
                    foreach (var rowGroupHandle in groupRowsByLevel)
                    {
                        SetRowExpanded(rowGroupHandle, expanded);
                    }
                }
            }
            finally
            {
                this.EndUpdate();
            }

        }
        private void _OnCustomDrawGroupRowCell(object sender, RowGroupRowCellEventArgs e)
        {
            //Kreslení spodní linky (border)
            Color borderColor = this.PaintAppearance.HorzLine.GetBackColor(e.Cache);
            DrawCellBottomBorder(e.Graphics, e.Bounds, borderColor);
        }

        /// <summary>
        /// Vrátí rowHandles pro group řádků pro daný level.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="level"></param>
        /// <returns>RowHandles</returns>
        private List<int> GetGroupRowsByLevel(GridView view, int level)
        {
            List<int> list = new List<int>();
            int rHandle = -1;
            while (view.IsValidRowHandle(rHandle))
            {
                if (view.GetRowLevel(rHandle) == level)
                {
                    list.Add(rHandle);
                }
                rHandle--;
            }
            return list;
        }
        #endregion

        #region Grouping        

        internal const string GROUP = "_group_";
        internal const string GROUPID = "_group_id_";
        internal const string PARENTGROUPID = "_parent_group_id_";
        private const string GROUPBUTTON = "_group_button_";
        internal const string PK_COLUMN_NAME = "H_PK_H";

        internal List<GroupRowInfo> GroupRowInfos = new List<GroupRowInfo>();
        internal List<GroupInfo> GroupInfos = new List<GroupInfo>();    //slouží pro řízení stavu celé skupiny, sbalit vše a rozbalit vše.
        //Slouží pro speciální případ, kdy ruším groupy a při zrušení poslední se neudrží informace o řazení view ani se nepošle změna o řazení.
        private Dictionary<string, DevExpress.Data.ColumnSortOrder> _lastColumSortOrder = new Dictionary<string, DevExpress.Data.ColumnSortOrder>();

        private Image __groupButtonColapsed;
        private Image __groupButtonExpandedBelow;
        private Image __groupButtonExpandedAbove;

        private Image _GroupButtonColapsed
        {
            get
            {
                if (__groupButtonColapsed == null)
                {
                    __groupButtonColapsed = _GetGroupButtonImage(false, false);
                }
                return __groupButtonColapsed;
            }
        }

        /// <summary>
        /// Ikonka rozbalené skupiny, která zohledňeje GroupMode
        /// </summary>
        private Image _GroupButtonExpanded
        {
            get
            {
                if (GroupMode == GroupModes.ShowGroupedSumRowAbove || GroupMode == GroupModes.ShowGroupedExpandedSumRowAbove)
                {
                    return _GroupButtonExpandedAbove;
                }
                else
                {
                    return _GroupButtonExpandedBelow;
                }
            }
        }
        private Image _GroupButtonExpandedBelow
        {
            get
            {
                if (__groupButtonExpandedBelow == null)
                {
                    __groupButtonExpandedBelow = _GetGroupButtonImage(true, false);
                }
                return __groupButtonExpandedBelow;
            }
        }
        private Image _GroupButtonExpandedAbove
        {
            get
            {
                if (__groupButtonExpandedAbove == null)
                {
                    __groupButtonExpandedAbove = _GetGroupButtonImage(true, true);
                }
                return __groupButtonExpandedAbove;
            }
        }
        private GroupModes _groupMode;
        internal GroupModes GroupMode
        {
            get => _groupMode;
            set
            {
                _groupMode = value;
                _SetGroupButtonVisibilty();
            }
        }
        internal enum GroupModes
        {
            None,
            Default,
            ShowOnlyGroups,
            ShowGrouped,
            ShowGroupedSumRowAbove,
            ShowGroupedExpandedSumRowAbove
        }

        /// <summary>
        /// získání obrázku pro group tlačítko
        /// </summary>
        /// <param name="groupExpanded"></param>
        /// <param name="groupSummaryAbove"></param>
        /// <returns></returns>
        private Image _GetGroupButtonImage(bool groupExpanded, bool groupSummaryAbove)
        {
            ImageCollection images = null;
            Image currentImage = null;

            SkinElement element = SkinManager.GetSkinElement(SkinProductId.Grid, UserLookAndFeel.Default, "PlusMinus");
            if (element.HasImage)
                images = element.Image.GetImages();
            else if (element.Glyph != null)
                images = element.Glyph.GetImages();

            if (images != null)
            {
                int imageIndex = (groupExpanded ? 1 : 0);
                //int imageIndexDelta = (info.RowState & GridRowCellState.Selected) != 0 ? 2 : 0;
                //if (images.Images.Count == 4)
                //    imageIndex += imageIndexDelta;
                currentImage = (Image)images.Images[imageIndex].Clone();
                if (!groupSummaryAbove && groupExpanded)
                {
                    currentImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
            }
            return currentImage;
        }

        private bool _LoadAllDataWhenGroupIsCollapsed;
        /// <summary>
        /// Příznak zda se mají načíst všechna data při snaze sbalit groupu a nejsou načteny všechny řádky <see cref="AllRowsLoaded"/>. 
        /// Smysl to dává jen pro režim Default, ten jediný má možnost nemít všechny datové řádky a pracuje s nimi.
        /// </summary>
        public bool LoadAllDataWhenGroupIsCollapsed { get { return _LoadAllDataWhenGroupIsCollapsed; } set { _LoadAllDataWhenGroupIsCollapsed = value; } }
        private void _OnEndGrouping(object sender, EventArgs e)
        {
            GridView view = sender as GridView;
            if (SortInfo.Count == 0 && _lastColumSortOrder.Count > 0)
            {
                //situace kdy po ukončení goupování nemám nic zagroupovaného. V tuto chvíli si view nepamatuje žádné nastavení sortignu ani nepošle změnu. Setřídím tedy podle posledního znameho nastavení.
                BeginSort();
                try
                {
                    foreach (DevExpress.XtraGrid.Columns.GridColumn gridColumn in view.Columns)
                        if (_lastColumSortOrder.Keys.Contains(gridColumn.FieldName))
                            gridColumn.SortOrder = _lastColumSortOrder[gridColumn.FieldName];
                }
                finally
                {
                    EndSort();
                }
            }
        }

        /// <summary>
        /// Je řáde groupa?
        /// </summary>
        /// <param name="rowHandle"></param>
        /// <returns></returns>
        private bool _IsGroupRow(int rowHandle)
        {
            return GetGroupRow(rowHandle) >= 0;
        }
        internal int GetGroupRow(int rowHandle)
        {
            int groupId = -1;
            DataRowView dtRow = this.GetRow(rowHandle) as DataRowView;
            if (dtRow != null)
            {
                int? gId = dtRow[GROUP] as int?;
                if (gId != null) { groupId = gId.Value; }
            }
            return groupId;
        }
        private int _GetGroupIdRow(int rowHandle)
        {
            int groupId = -1;
            DataRowView dtRow = this.GetRow(rowHandle) as DataRowView;
            if (dtRow != null)
            {
                int? gId = dtRow[GROUPID] as int?;
                if (gId != null) { groupId = gId.Value; }
            }
            return groupId;
        }
        internal int _GetParentGroupIdRow(int rowHandle)
        {
            int groupId = -1;
            DataRowView dtRow = this.GetRow(rowHandle) as DataRowView;
            if (dtRow != null)
            {
                int? gId = dtRow[PARENTGROUPID] as int?;
                if (gId != null) { groupId = gId.Value; }
            }
            return groupId;
        }
        private int _GetGroupPrimaryKey(int rowHandle)
        {
            int groupId = -1;
            DataRowView dtRow = this.GetRow(rowHandle) as DataRowView;
            if (dtRow != null)
            {
                int? gId = dtRow[PK_COLUMN_NAME] as int?;
                if (gId != null) { groupId = gId.Value; }
            }
            return groupId;
        }
        private void _SetAllChildGroupsColapsed(int parentGroupId)
        {
            foreach (GroupRowInfo item in GroupRowInfos)
            {
                if (item.ParentGroupId == parentGroupId)
                {
                    item.Expanded = false;
                    _SetAllChildGroupsColapsed(item.GroupId);
                }
            }
        }
        /// <summary>
        /// Sbalí všechny skupiny dané úrovně a jejich potomky (nižší úrovně)
        /// </summary>
        /// <param name="group"></param>
        private void _SetAllGroupsAndChildsColapsed(int group)
        {
            bool anyGroupRowOfGroup = GroupRowInfos.Any(x => x.Group == group);  //příznak zda existuje nějaký group řádek pro danou groupu
            bool loadAllRows = false;
            if (!anyGroupRowOfGroup)
            {
                if (_LoadAllDataWhenGroupIsCollapsed)
                {
                    //požádej server o všechna data a pokračuj. Tady můžu klidně pokračovat, protože nastavení které provedu níže, se aplikuje po donačtení dat.
                    loadAllRows = true;
                }
                else
                {
                    //nepokračuj a hoď placku.
                    DxComponent.ShowMessageInfo(DxComponent.Localize(MsgCode.ClientBrowse_Grouping_DataNotLoaded));
                    return;
                }
            }

            foreach (GroupRowInfo item in GroupRowInfos)
            {
                if (item.Group == group)
                {
                    item.Expanded = false;
                    _SetAllChildGroupsColapsed(item.GroupId);
                }
            }

            foreach (var item in GroupInfos.Where(item => item.Group >= group))
            {
                item.Expanded = false;
            }

            if (loadAllRows) OnDxLoadAllRows();
            this.RefreshData();
        }
        private void _SetAllParentGroupsExpanded(int? parentGroupId)
        {
            if (parentGroupId == null) return;

            foreach (GroupRowInfo item in GroupRowInfos)
            {
                if (item.GroupId == parentGroupId)
                {
                    item.Expanded = true;
                    _SetAllParentGroupsExpanded(item.ParentGroupId);
                }
            }
        }

        /// <summary>
        /// Rozbalí všechny groupy dané úrovně a jejich rodiče (vyšší úroveň). Pokud je to poslední a je potřeba, tak požádá o všechny řádky.
        /// </summary>
        /// <param name="group"></param>
        private void _SetAllGroupsAndParentsExpanded(int group)
        {
            foreach (var item in GroupInfos.Where(item => item.Group <= group))
            {
                item.Expanded = true;
            }

            foreach (GroupRowInfo item in GroupRowInfos)
            {
                if (item.Group == group)
                {
                    item.Expanded = true;
                    _SetAllParentGroupsExpanded(item.ParentGroupId);
                }
            }
            if (_CanLoadGroupRows(group))
            {
                WaitForNextRows = true;
                OnDxLoadGroupRows(null, group, true, null);   //požádát o všechny řádky pokud to dává smysl.
            }
            this.RefreshData();
        }
        private void _SetGroupButtonVisibilty()
        {
            var groupButtonColumn = this.Columns.FirstOrDefault(x => x.FieldName == GROUPBUTTON);
            if (groupButtonColumn != null)
            {
                if (groupButtonColumn.Visible != _GetGroupButtonVisibility())
                {
                    //zapamatuji si poslední sloupec s focusem v řádkovém filtru, protože při změně viditelnosti sloupce groupy, se focus změní
                    var lastFocused = _lastFocusedColumnInRowFilter;
                    groupButtonColumn.Visible = _GetGroupButtonVisibility();
                    if (!string.IsNullOrEmpty(lastFocused)) TrySetFocusToColumnInFilterRow(lastFocused);
                }
            }
        }
        /// <summary>
        /// Vyhodnocuje podmínky viditělnosti pro group button
        /// </summary>
        /// <returns></returns>
        private bool _GetGroupButtonVisibility()
        {
            return GroupMode != GroupModes.None && !DisableSortChange;
        }
        /// <summary>
        /// Vrací datový řádek podle pro rowindex
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        internal DataRow GetGroupDataRowByRowIndex(int rowIndex)
        {
            int rowHandle = GetRowHandle(rowIndex);
            return this.GetDataRow(rowHandle);
        }
        private bool _IsTheLowestGroup(int group)
        {
            return group >= GroupCount - 1;
        }
        /// <summary>
        /// Vrací číslo groupy sloupce. Vrací null pokud sloupec není grouped.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private int? GetGroupByColumn(string columnName)
        {
            return GetGroupByColumn(this.Columns.FirstOrDefault(x => x.FieldName == columnName));
        }
        /// <summary>
        /// Vrací číslo groupy sloupce. Vrací null pokud sloupec není grouped.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private int? GetGroupByColumn(GridColumn column)
        {
            return column?.GroupIndex >= 0 ? column.GroupIndex : (int?)null;
        }
        /// <summary>
        /// Vrací true pokud má význam žadat o datové řádky pro groupu.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private bool _CanLoadGroupRows(int group)
        {
            return _IsTheLowestGroup(group) //poslední úroveň
                          && (GroupMode == GroupModes.ShowGrouped || GroupMode == GroupModes.ShowGroupedSumRowAbove);
        }
        private bool IsGroupExpanded(int group)
        {
            return GroupInfos.Any(x => x.Group == group && x.Expanded == true);
        }

        #region group state icon
        /// <summary>
        /// Kolekce <see cref="DxGridViewInfo.GroupColumnHeaderInfo"/>. Jako klič je <see cref="GridColumn"/>.
        /// </summary>
        public static Dictionary<GridColumn, DxGridViewInfo.GroupColumnHeaderInfo> GroupColumnHeaderInfos = new Dictionary<GridColumn, DxGridViewInfo.GroupColumnHeaderInfo>();
        private Rectangle GetGroupStateIconBackgroundArea(Rectangle captionRect)
        {
            Point iconPoint = GetGroupStateIconBackgroundPoint(captionRect);
            Rectangle iconArea = new Rectangle(iconPoint.X - GROUPSTATEICONBORDER, iconPoint.Y - GROUPSTATEICONBORDER, GroupStateIconBackgroundAreaWidth, GROUPSTATEICONHEIGHT + 2 * GROUPSTATEICONBORDER);
            return iconArea;
        }
        private Rectangle GetGroupStateIconArea(Rectangle captionRect)
        {
            Point iconPoint = GetGroupStateIconPoint(captionRect);
            Rectangle iconArea = new Rectangle(iconPoint.X - GROUPSTATEICONBORDER, iconPoint.Y - GROUPSTATEICONBORDER, GRPUPSTATEICONWIDTH, GROUPSTATEICONHEIGHT);
            return iconArea;
        }
        private Point GetGroupStateIconBackgroundPoint(Rectangle captionRect)
        {
            int iconX = captionRect.X - GRPUPSTATEICONWIDTH - GROUPSTATEICONBORDER * 2;
            int iconY = captionRect.Y + captionRect.Height / 2 - GROUPSTATEICONHEIGHT / 2;
            return new Point(iconX, iconY);
        }
        private Point GetGroupStateIconPoint(Rectangle captionRect)
        {
            int iconX = captionRect.X - GRPUPSTATEICONWIDTH - GROUPSTATEICONBORDER;
            int iconY = captionRect.Y + captionRect.Height / 2 - GROUPSTATEICONHEIGHT / 2 + GROUPSTATEICONBORDER;
            return new Point(iconX, iconY);
        }
        /// <summary>
        /// Vrací true, pokud point je v olbasti pro GroupIcon
        /// </summary>
        /// <param name="point"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool IsGroupIconRect(Point point, GridColumn column)
        {
            if (column is null) return false;

            // Získání oblasti hlavičky skupiny
            GroupColumnHeaderInfos.TryGetValue(column, out DxGridViewInfo.GroupColumnHeaderInfo groupColumnHeaderInfo);
            if (groupColumnHeaderInfo is null) return false;
            Rectangle captionRect = groupColumnHeaderInfo.CaptionRect;
            Rectangle iconBounds = GetGroupStateIconBackgroundArea(captionRect);
            return iconBounds.Contains(point);
        }

        /// <summary>
        /// celková šířka ikonky stavu skupiny (ikona včetně pozadí)
        /// </summary>
        public int GroupStateIconBackgroundAreaWidth { get { return GRPUPSTATEICONWIDTH + 2 * GROUPSTATEICONBORDER; } }
        internal GroupInfo GetGroupInfo(int groupIndex)
        {
            return GroupInfos.FirstOrDefault(x => x.Group == groupIndex);
        }
        #endregion

        #endregion

        #region drag&drop
        bool _canMoveColumn = true;
        private void _OnDragObjectStart(object sender, DragObjectStartEventArgs e)
        {
            //Řešení pro zakázání "stáhnutí" groupovaného sloupečku mimo group panel, pokud je zakázáno řazení. V případě, že je zakázáno řazení, tak nejde vytvořit skupinu, ale šlo ji zrušit přetažením mimo group panel.
            //Využívám k tomu proměnou _canMoveColumn  kam si zaznamenám, zda jsem začal dragAndDrop sloupce v Group panelu a přitom mám zakázáné řazení tohoto sloupce. V _OnDragObjectDrop tuto proměnou resetuji.
            var dragObject = e.DragObject;
            //if (dragObject is not GridColumn) //pro c# 9
            if (!(dragObject is GridColumn))
            {
                return;
            }
            var column = dragObject as GridColumn;
            if (column.GroupIndex < 0)
            {
                return;
            }

            GridView view = sender as GridView;
            bool isInGroupPanel = GetHitInfo(view).InGroupPanel;

            if (isInGroupPanel && !this.IsColumnAllowSort(column))
            {
                _canMoveColumn = false;
            }
        }

        private void _OnDragObjectOver(object sender, DragObjectOverEventArgs e)
        {
            //Řešení pro zakázání "stáhnutí" groupovaného sloupečku mimo group panel, pokud je zakázáno řazení. V případě, že je zakázáno řazení, tak nejde vytvořit skupinu, ale šlo ji zrušit přetažením.
            if (!_canMoveColumn)
            {
                e.DropInfo.Valid = false;
            }

        }
        private void _OnDragObjectDrop(object sender, DragObjectDropEventArgs e)
        {
            //konec dragAndDrop -> reset zda můžu pustit sloupec někde...
            _canMoveColumn = true;
        }

        private GridHitInfo GetHitInfo(GridView view)
        {
            if (view == null)
            {
                return null;
            }
            var mousePosition = Control.MousePosition;
            var hitInfo = view.CalcHitInfo(view.GridControl.PointToClient(mousePosition));
            return hitInfo;
        }
        #endregion
        /// <summary>
        /// Nastavuje ikony v záhlaví sloupců
        /// </summary>
        /// <param name="columnNameImageName">Seznam sloupců a jijch ikon. U sloupců, které nebudou obsaženy v seznamu, se připadná ikona "vypne"</param>
        private void SetColumnHeaderImage(Dictionary<string, string> columnNameImageName)
        {
            string imageName = string.Empty;
            foreach (DevExpress.XtraGrid.Columns.GridColumn column in Columns)
            {
                columnNameImageName.TryGetValue(column.FieldName, out imageName);
                DxComponent.ApplyImage(column.ImageOptions, imageName, null, ResourceImageSizeType.Small);
            }
        }

        public void SetLayout()
        {
            _InitColumns();
        }

        #region Init columns

        /// <summary>
        /// Vytvoření koleckce sloupců v gridView
        /// </summary>
        private void _InitColumns()
        {
            GroupColumnHeaderInfos.Clear(); //reset uložených informací, budeme mít nové columns
            this.BeginUpdate();
            _SilentModeOn();    //Posílaly se změny řádkového filtru -> silentModeOn
            this.Columns.Clear();
            _SilentModeOff();

            this.Columns.Add(_CreateGridColumnGroupButton());
            foreach (var vc in GridViewColumns)
            {
                int index = this.Columns.Add(_CreateGridColumn(vc));
                _InitColumnAfterAdd(index, vc);
                _InitColumnSummaryRow(index, vc);
            }
            this.EndUpdate();
        }
        /// <summary>
        /// Tvorba GridColumn na základě našich dat z <see cref="IGridViewColumn"/>
        /// </summary>
        /// <param name="gridViewColumn"></param>
        /// <returns></returns>
        private DevExpress.XtraGrid.Columns.GridColumn _CreateGridColumn(IGridViewColumn gridViewColumn)
        {
            var gc = new DevExpress.XtraGrid.Columns.GridColumn()
            {
                FieldName = gridViewColumn.FieldName,
                Caption = gridViewColumn.Caption,
                UnboundDataType = gridViewColumn.ColumnType,
                VisibleIndex = gridViewColumn.VisibleIndex,
                Visible = gridViewColumn.Visible,
                ToolTip = ""
            };
            if (string.IsNullOrEmpty(gridViewColumn.Caption)) gc.OptionsColumn.ShowCaption = false; //pokud nenastavím Caption, tak nechci niz zobrazovat (název sloupce)

            gc.OptionsColumn.AllowSort = DxComponent.ConvertBool(gridViewColumn.AllowSort);
            //gc.OptionsColumn.AllowGroup = DxComponent.ConvertBool(gridViewColumn.AllowSort);
            //povolení filtrování
            gc.OptionsFilter.AllowFilter = gridViewColumn.AllowFilter;
            gc.OptionsFilter.AllowAutoFilter = gridViewColumn.AllowFilter;
            gc.OptionsFilter.AutoFilterCondition = gridViewColumn.AutoFilterCondition;

            gc.OptionsFilter.ImmediateUpdatePopupExcelFilter = DefaultBoolean.False;    //Vyklikávám hodnoty a update se provede až po stiknutí OK v popUp okně. 
            gc.OptionsFilter.ImmediateUpdateAutoFilter = false; //TODO předat z IGridViewColumn a případně zajistit nějaký timer? Prodlevu?
            gc.OptionsFilter.ImmediateUpdatePopupExcelFilter = DefaultBoolean.False;    //Vyklikávám hodnoty a update se provede až po stiknutí OK v popUp okně. 
            OptionsFilter.UseNewCustomFilterDialog = false;


            //šířka sloupce
            if (gridViewColumn.Width > 0) gc.Width = DxComponent.ZoomToGui(gridViewColumn.Width);
            if (gridViewColumn.IsImageColumnType) gc.OptionsColumn.FixedWidth = true;   //důležité aby se obrázek vykreslil podle šířky sloupce
            gc.OptionsColumn.AllowSize = gridViewColumn.AllowSize;
            //změna pořadí sloupců
            gc.OptionsColumn.AllowMove = gridViewColumn.AllowMove;

            //Merge stejných hodnot (distinct)
            gc.OptionsColumn.AllowMerge = DxComponent.ConvertBool(gridViewColumn.AllowMerge);

            gc.OptionsColumn.ShowInCustomizationForm = !gridViewColumn.IsSystemColumn;  //systémové sloupce nechceme nabízet ve výběru sloupců

            //ikonka
            _ApplyImageToColumnHeader(gc, gridViewColumn);

            //řazení
            gc.SortMode = ColumnSortMode.Custom;    //Custom sorting ->vyvolá se eventa CustomColumnSort kde mohu "vypnout" řazení

            if (gridViewColumn.IsNumberColumnType || gridViewColumn.IsDateTimeColumnType) //DateTine zatím nechámn na DisplayText, protože jinak nejde zadat jenom rok, ale celé datum...
            {
                gc.FilterMode = ColumnFilterMode.Value;
            }
            else
            {
                gc.FilterMode = ColumnFilterMode.DisplayText; //Tohle umožní dát do filtrovacího řádku jakýkoliv text a validovat / formátovat si ho budu sám v OnCustomRowFilter. J
            }

            var repositoryItem = _CreateRepositoryItem(gridViewColumn, false);
            if (repositoryItem != null)
            {
                this.GridControl.RepositoryItems.Add(repositoryItem);
                gc.ColumnEdit = repositoryItem;
            }

            //nastaveni displayFormat
            _SetFormatInfo(gc.DisplayFormat, gridViewColumn);
            //Zkracování nadpisu bez teček na konci pokud se nevejdou do šířky sloupce, stejně jako stary přehled. Default se přidávali 3 tečky (EllipsisCharacter).
            gc.AppearanceHeader.TextOptions.Trimming = Trimming.Character;
            _SetColumnTextTrimming(gc);

            return gc;
        }

        private void _ApplyImageToColumnHeader(GridColumn gridColumn, IGridViewColumn gridViewColumn)
        {
            gridColumn.ImageOptions.Alignment = gridColumn.OptionsColumn.ShowCaption == false ? StringAlignment.Center : gridViewColumn.IconAligment;   //pokud nechci nadpis, tak ikonky doprostřed.
            DxComponent.ApplyImage(gridColumn.ImageOptions, gridViewColumn.IconName, null, ResourceImageSizeType.Small);
        }

        private DevExpress.XtraGrid.Columns.GridColumn _CreateGridColumnGroupButton()
        {
            var gc = new DevExpress.XtraGrid.Columns.GridColumn()
            {
                FieldName = GROUPBUTTON,
                //Caption = "G",
                UnboundDataType = typeof(bool),
                VisibleIndex = 0,
                Visible = _GetGroupButtonVisibility(),
                ToolTip = "G",
                Width = 28
            };
            gc.OptionsColumn.AllowSort = DefaultBoolean.False;
            gc.OptionsFilter.AllowFilter = false;
            gc.OptionsFilter.AllowAutoFilter = false;
            gc.OptionsColumn.AllowSize = false;
            gc.OptionsColumn.AllowMove = false;
            gc.OptionsColumn.ShowInCustomizationForm = false;
            gc.ImageOptions.Alignment = StringAlignment.Center;
            DxComponent.ApplyImage(gc.ImageOptions, "images/snap/groupby_16x16.png", null, ResourceImageSizeType.Small);

            RepositoryItemImageEdit repositoryImageEdit = new RepositoryItemImageEdit();
            gc.ColumnEdit = repositoryImageEdit;
            this.GridControl.RepositoryItems.Add(repositoryImageEdit);

            return gc;
        }

        private void _SetFormatInfo(FormatInfo formatInfo, IGridViewColumn gridViewColumn)
        {
            //format; takto to je i v pivotGrid.
            if (gridViewColumn.ColumnType == typeof(System.String))
            {
                formatInfo.FormatType = DevExpress.Utils.FormatType.Custom;
                formatInfo.FormatString = "{0}"; // string nejde sčítat
            }
            else if (gridViewColumn.ColumnType == typeof(System.DateTime))
            {
                formatInfo.FormatType = DevExpress.Utils.FormatType.DateTime;
                formatInfo.FormatString = gridViewColumn.FormatString; //"MMM/d/yyyy hh:mm tt";
            }
            else if (gridViewColumn.IsNumberColumnType) //RMC 19.11.2024; změněno z Int na isNumber. Může přijít procento jako int a poté je potřeba použít formát.
            {
                formatInfo.FormatType = DevExpress.Utils.FormatType.Numeric;
                formatInfo.FormatString = gridViewColumn.FormatString; //"n"; //https://documentation.devexpress.com/#WindowsForms/CustomDocument2141
            }
        }
        /// <summary>
        /// Některé vlastnosti GridColumn nejdou nastavit v rámci <see cref="_CreateGridColumn(IGridViewColumn)"/>. Je třeba je nastavit až po přidání do kolekce.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="gridViewColumn"></param>
        private void _InitColumnAfterAdd(int columnIndex, IGridViewColumn gridViewColumn)
        {
            var gc = this.Columns[columnIndex];
            //Header settings
            gc.AppearanceHeader.TextOptions.HAlignment = gridViewColumn.HeaderAlignment;
            gc.AppearanceHeader.FontStyleDelta = gridViewColumn.HeaderFontStyle;
            gc.AppearanceCell.TextOptions.HAlignment = gridViewColumn.CellAlignment;
            gc.AppearanceCell.FontStyleDelta = gridViewColumn.CellFontStyle;
        }

        private void _InitColumnSummaryRow(int columnIndex, IGridViewColumn gridViewColumn)
        {
            var gc = this.Columns[columnIndex];
            if (gridViewColumn.IsNumberColumnType)
            {
                var sumItem = new GridColumnSummaryItem()
                {
                    SummaryType = DevExpress.Data.SummaryItemType.Custom,   //sumy si řeším v _OnCustomSummaryCalculate (nesčítám groupy)
                    Mode = DevExpress.Data.SummaryMode.Selection,
                    DisplayFormat = "{0:" + gridViewColumn.FormatString + "}"
                };
                gc.Summary.Add(sumItem);
            }
        }

        #endregion

        #region RepositoryItem, eitační styl, image, multiline...
        private RepositoryItem _CreateRepositoryItem(IGridViewColumn gridViewColumn, bool useForRowFilter = false)
        {
            if (gridViewColumn.IsEditStyleColumnType)   //Editační styl
                return _CreateRepositoryItemForEditStyle(gridViewColumn, useForRowFilter);
            else if (gridViewColumn.IsImageColumnType) //Obrázky
                return _CreateRepositoryItemForImage(useForRowFilter);
            else if (gridViewColumn.IsStringColumnType || gridViewColumn.IsNumberColumnType) //string a numeric
                return _CreateRepositoryItemForStringAndNumber(gridViewColumn, useForRowFilter);
            else if (gridViewColumn.IsDateTimeColumnType) //DateTime
                return _CreateRepositoryItemForDatetime(gridViewColumn, useForRowFilter);
            else
                return null;
        }

        private static RepositoryItem _CreateRepositoryItemForDatetime(IGridViewColumn gridViewColumn, bool useForRowFilter)
        {
            if (!useForRowFilter) return null;
            //pro řádkový filtr vytvářím TextEdit, aby šlo zadávát custom hodnoty, které poté konvertuji v CreateAutoFilterCriterion
            RepositoryItemTextEdit repItemTextEditForDateTime = new RepositoryItemTextEdit();
            repItemTextEditForDateTime.Enter += RepItemTextEditForDateTime_Enter;
            //repItemMemoEditForDateTime.Validating += RepItemMemoEditForDateTime_Validating;   //tady nešel propsat ErrorText do tooltipu, proto řeším v _OnValidatingEditor
            //pohlrával jsem si i CustomDisplayText eventou...

            return repItemTextEditForDateTime;
        }

        private static void RepItemTextEditForDateTime_Enter(object sender, EventArgs e)
        {
            //tady nahrazuji zpět do datumového editu v řádkovém filtru hodnotu, která byla zadaná uživatem, před konverzí, vrazím ho tam když vstoupím do inputu.. proto ten Enter...
            DevExpress.XtraEditors.TextEdit txtEdit = sender as DevExpress.XtraEditors.TextEdit;
            var view = (txtEdit.Parent as GridControl).MainView as DxGridView;
            var column = view.FocusedColumn;
            string filterCriteria = column.FilterInfo?.FilterString ?? string.Empty;
            string originFiler;
            view._convertedColumnFilterCache.TryGetValue(filterCriteria, out originFiler);
            if (!string.IsNullOrEmpty(originFiler))
            {
                txtEdit.EditValue = originFiler;
            }
        }

        private static RepositoryItem _CreateRepositoryItemForStringAndNumber(IGridViewColumn gridViewColumn, bool useForRowFilter)
        {
            if (useForRowFilter)
            {//řádkový filtr
                RepositoryItemTextEdit repItemTextEdit = new RepositoryItemTextEdit();
                repItemTextEdit.Enter += RepItemTextEdit_Enter;
                return repItemTextEdit;
            }
            else if (gridViewColumn.MultiLineCell)
            {
                RepositoryItemMemoEdit repItemMemoEdit = new RepositoryItemMemoEdit()
                {
                    LinesCount = 0 //0 = automatický počet
                };
                return repItemMemoEdit;
            }
            return null;
        }

        private static void RepItemTextEdit_Enter(object sender, EventArgs e)
        {
            //tady nahrazuji zpět do  editu v řádkovém filtru hodnotu, která byla zadaná uživatem, před konverzí, vrazím ho tam když vstoupím do inputu.. proto ten Enter...
            //jedná o zadávání "něco1..něco2", tedy intervalu.
            DevExpress.XtraEditors.TextEdit txtEdit = sender as DevExpress.XtraEditors.TextEdit;
            var view = (txtEdit.Parent as GridControl).MainView as DxGridView;
            var column = view.FocusedColumn;
            string filterCriteria = column.FilterInfo?.FilterString ?? string.Empty;
            string originFiler;
            view._convertedColumnFilterCache.TryGetValue(filterCriteria, out originFiler);
            if (!string.IsNullOrEmpty(originFiler))
            {
                txtEdit.EditValue = originFiler;
            }
        }

        private static RepositoryItem _CreateRepositoryItemForImage(bool useForRowFilter)
        {
            if (useForRowFilter) return null;
            RepositoryItemPictureEdit repItemPictureEdit = new RepositoryItemPictureEdit
            {
                NullText = " ", //Aby se nezobrazovalo DataEmpty
                SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze,
                CustomHeight = 0
            };
            return repItemPictureEdit;
        }
        private RepositoryItem _CreateRepositoryItemForEditStyle(IGridViewColumn gridViewColumn, bool useForRowFilter)
        {
            if (gridViewColumn.EditStyleViewMode == EditStyleViewMode.Icon || gridViewColumn.EditStyleViewMode == EditStyleViewMode.IconText)
            {
                //editační styl s ikonkomi
                ResourceContentType resourceType = _GetResourceType(gridViewColumn);
                var smallImageList = _GetImageList(resourceType, ResourceImageSizeType.Small);

                RepositoryItemImageComboBox repItemImageCombo = new RepositoryItemImageComboBox();          // vytvoříme objekt typu RepositoryItemImageComboBox
                repItemImageCombo.AutoHeight = false;

                repItemImageCombo.SmallImages = smallImageList;
                repItemImageCombo.GlyphAlignment = gridViewColumn.EditStyleViewMode == EditStyleViewMode.IconText ? HorzAlignment.Near : HorzAlignment.Center;

                repItemImageCombo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
                repItemImageCombo.ReadOnly = false;

                foreach (var code in gridViewColumn.CodeTable)
                {
                    //vytváření items
                    string displayText = gridViewColumn.EditStyleViewMode == EditStyleViewMode.IconText || useForRowFilter ? code.DisplayText : "";
                    //string value = useForRowFilter ? code.DisplayText : code.ImageName;   //v cell je místo value posílán imageName! Tady to s DAJ pokusíme změnit.
                    string value = code.DisplayText;
                    int index = _GetImageIndex(code.ImageName, ResourceImageSizeType.Small, resourceType, -1);
                    var imageComboBoxItem = new DevExpress.XtraEditors.Controls.ImageComboBoxItem(displayText, value, index);
                    repItemImageCombo.Items.Add(imageComboBoxItem);
                }

                return repItemImageCombo;
            }
            else
            {
                //Text
                //editační styl bez ikonek
                RepositoryItemComboBox repItemCombo = new RepositoryItemComboBox();          // vytvoříme objekt typu RepositoryItemComboBox
                int i = 0;
                foreach (var code in gridViewColumn.CodeTable)
                {
                    DevExpress.XtraEditors.Controls.ComboBoxItem cbItem = new DevExpress.XtraEditors.Controls.ComboBoxItem(code.DisplayText);
                    repItemCombo.Items.Add(cbItem);
                    i++;
                }

                return repItemCombo;
            }
        }

        /// <summary>
        /// Vrátí <see cref="ResourceContentType"/> podle prvního prvku v CodeTable
        /// </summary>
        /// <param name="gridViewColumn"></param>
        /// <returns></returns>
        private static ResourceContentType _GetResourceType(IGridViewColumn gridViewColumn)
        {
            ResourceContentType resourceType = ResourceContentType.None;
            //resourceType určíme podle prvního item s image
            var firstImageCodeTableItem = gridViewColumn.CodeTable.FirstOrDefault(x => !string.IsNullOrEmpty(x.ImageName));
            if (firstImageCodeTableItem.ImageName != null)
            {
                DxComponent.TryGetResourceContentType(firstImageCodeTableItem.ImageName, null, out resourceType);
            }

            return resourceType;
        }

        private static object _GetImageList(ResourceContentType resourceContentType, ResourceImageSizeType resourceImageSizeType)
        {
            switch (resourceContentType)
            {
                case ResourceContentType.Bitmap:
                    return DxComponent.GetBitmapImageList(resourceImageSizeType);
                case ResourceContentType.Vector:
                    return DxComponent.GetVectorImageList(resourceImageSizeType);
                default:
                    return null;
            }
        }

        private int _GetImageIndex(string imageName, ResourceImageSizeType resourceImageSizeType, ResourceContentType resourceContentType, int defaultValue)
        {
            int index = -1;
            //if (!String.IsNullOrEmpty(imageName) && _PrepareImageListFor(imageName))
            if (!String.IsNullOrEmpty(imageName))
            {
                switch (resourceContentType)
                {
                    case ResourceContentType.Bitmap:
                        index = DxComponent.GetBitmapImageIndex(imageName, resourceImageSizeType);
                        break;
                    case ResourceContentType.Vector:
                        index = DxComponent.GetVectorImageIndex(imageName, resourceImageSizeType);
                        break;
                }
            }
            if (index < 0 && defaultValue >= 0) index = defaultValue;
            return index;
        }

        #endregion

        /// <summary>
        /// Nastavení focusu do řádkového filtru. Pokud mám ho mám k dispozici a ještě tam nejsem.
        /// </summary>
        /// <param name="columnToFocus"></param>
        /// <returns></returns>
        internal bool TrySetFocusToFilterRow(string columnToFocus)
        {
            //TADY to budu potřebovat volat i z controleroru, proto internal

            if (!_RowFilterVisible || IsFilterRowActive) return false;
            //stojím mimo řádkový filtr a mám ho k dispozici

            this.FocusedRowHandle = DevExpress.XtraGrid.GridControl.AutoFilterRowHandle;
            bool focusFirstColumn = true;
            if (!string.IsNullOrEmpty(columnToFocus))
            {
                var column = this.Columns.Where(x => x.FieldName == columnToFocus).FirstOrDefault();
                if (column != null)
                {
                    this.FocusedColumn = column;
                    focusFirstColumn = false;
                }
            }

            //najdu první column který můžu filtrovat a nastavím mu focus
            if (focusFirstColumn)
            {
                foreach (var column in Columns.OrderBy(x => x.VisibleIndex))
                {
                    if (column.OptionsFilter.AllowAutoFilter)
                    {
                        this.FocusedColumn = column;
                        break;
                    }
                }
            }

            LastFocusedColumnInRowFilter = FocusedColumn?.FieldName;

            this.ShowEditor();  //activuje cell pro editaci (curzor)
            return true;
        }

        /// <summary>
        /// Slouží pro případ, kdy se snažím o znovu nastavení původně focusovaného sloupce během změny viditelnosti group sloupce.
        /// </summary>
        /// <param name="columnToFocus"></param>
        /// <returns></returns>
        private bool TrySetFocusToColumnInFilterRow(string columnToFocus)
        {
            if (!(IsFilterRowActive && FocusedColumn != null)) return false;

            //stojím již v řádkovém filtru
            if (!string.IsNullOrEmpty(columnToFocus))
            {
                var column = this.Columns.Where(x => x.FieldName == columnToFocus).FirstOrDefault();
                if (column != null)
                {
                    this.FocusedColumn = column;
                }
            }
            LastFocusedColumnInRowFilter = FocusedColumn?.FieldName;
            return true;
        }

        #region Public eventy a jejich volání

        /// <summary>
        /// Tichý režim = bez eventů.
        /// </summary>
        private bool _silentMode;
        private int _silentCount = 0;

        private void _SilentModeOn()
        {
            _silentCount++;
            _silentMode = true;
        }
        private void _SilentModeOff()
        {
            _silentCount--;
            if (_silentCount <= 0)
            {
                _silentMode = false;
                _silentCount = 0;
            }
        }

        /// <summary>
        /// Kliklo se do gridu <see cref="DxDoubleClick"/>
        /// </summary>
        public event DxGridDoubleClickHandler DxDoubleClick;
        protected virtual void OnDoubleClick(string columnName, int rowId, bool modifierKeyCtrl = false, bool modifierKeyAlt = false, bool modifierKeyShift = false)
        {
            if (DxDoubleClick != null) DxDoubleClick(this, new DxGridDoubleClickEventArgs(columnName, rowId, modifierKeyCtrl, modifierKeyAlt, modifierKeyShift));
        }

        public event DxGridFocusedRowChangedHandler DxFocusedRowChanged;
        protected virtual void OnFocusedRowChanged(int prevFocusedRowHandle, int focusedRowHandle)
        {
            if (DxFocusedRowChanged != null) DxFocusedRowChanged(this, new DxGridFocusedRowChangedEventArgs(prevFocusedRowHandle, focusedRowHandle));
        }

        public event DxGridSelectionChangedHandler DxSelectionChanged;
        protected virtual void OnSelectionChanged(List<int> selectedRows)
        {
            if (_silentMode) return;

            if (selectedRows.Count() != this.RowCount) _selectedAllRows = false; //schození přiznaku o vybraných všech řádcích
            if (DxSelectionChanged != null) DxSelectionChanged(this, new DxGridSelectionChangedChangedEventArgs(selectedRows, SelectedAllRows));
        }

        public event DxGridKeyDownHandler DxKeyDown;
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (DxKeyDown != null) DxKeyDown(this, e);
        }

        /// <summary>
        /// Grid chce další data <see cref="DxLoadMoreRows"/>
        /// </summary>
        public event DxGridLoadMoreRowsHandler DxLoadMoreRows;
        protected virtual void OnDxLoadMoreRows()
        {
            if (DxLoadMoreRows != null) DxLoadMoreRows(this, new DxGridLoadMoreRowsEventArgs());
        }

        /// <summary>
        /// Provedena změna viditelnosti groupovacího řádku
        /// </summary>
        public event DxGridShowGroupPanelChangedHandler DxShowGroupPanelChanged;
        protected virtual void OnDxShowGroupPanelChanged()
        {
            if (DxShowGroupPanelChanged != null) DxShowGroupPanelChanged(this, new EventArgs());
        }

        /// <summary>
        /// Provedena změna viditelnosti filtrovacího řádku
        /// </summary>
        public event DxGridShowGroupPanelChangedHandler DxShowFilterRowChanged;
        protected virtual void OnDxShowFilterRowChanged()
        {
            if (DxShowFilterRowChanged != null) DxShowFilterRowChanged(this, new EventArgs());
        }

        /// <summary>
        /// Vyvolá metodu <see cref="OnShowContextMenu(DxGridContextMenuEventArgs)"/> a event <see cref="DxShowContextMenu"/>
        /// </summary>
        /// <param name="hitInfo"></param>
        private void RaiseShowContextMenu(DxGridHitInfo hitInfo, string menuArea)
        {
            //if (_SilentMode) return;

            DxGridContextMenuEventArgs args = new DxGridContextMenuEventArgs(hitInfo, menuArea);
            OnShowContextMenu(args);
            DxShowContextMenu?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel chce zobrazit kontextové menu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnShowContextMenu(DxGridContextMenuEventArgs args) { }
        /// <summary>
        /// Uživatel chce zobrazit kontextové menu
        /// </summary>
        public new event DxGridContextMenuHandler DxShowContextMenu;

        /// <summary>
        /// Grid chce datové řádky pro skupinu <see cref="DxLoadMoreRows"/>
        /// </summary>
        public event DxLoadGroupRowsHandler DxLoadGroupRows;
        protected virtual void OnDxLoadGroupRows(int? groupId, int group, bool allRowsOfGroup, int? groupPrimaryKey)
        {
            if (DxLoadGroupRows != null) DxLoadGroupRows(this, new DxLoadGroupRowEventArgs(groupId, group, allRowsOfGroup, groupPrimaryKey));
        }

        /// <summary>
        /// Grid chce všechna data
        /// </summary>
        public event DxLoadAllRowsHandler DxLoadAllRows;
        protected virtual void OnDxLoadAllRows()
        {
            if (DxLoadAllRows != null)
            {
                WaitForNextRows = true;
                DxLoadAllRows(this, new EventArgs());
            }
        }

        #endregion
    }

    /// <summary>
    /// Třída pro definici vlastnosti sloupců.    
    /// </summary>
    public class GridViewColumnData : IGridViewColumn
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GridViewColumnData() : base()
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="caption"></param>
        /// <param name="columnType"></param>
        /// <param name="visibleIndex"></param>
        /// <param name="visible"></param>
        /// <param name="allowSort"></param>
        /// <param name="allowFilter"></param>
        /// <param name="width"></param>
        /// <param name="allowSize"></param>
        /// <param name="allowMove"></param>
        public GridViewColumnData(string fieldName, string caption, Type columnType, int visibleIndex,
            bool visible = true, bool allowSort = true, bool allowFilter = true, int width = 30, bool allowSize = true, bool allowMove = true)
        {
            FieldName = fieldName;
            Caption = caption;
            ColumnType = columnType;
            VisibleIndex = visibleIndex;
            Visible = visible;
            AllowSort = allowSort;
            AllowFilter = allowFilter;
            CodeTable = new List<(string DisplayText, string Value, string ImageName)>();
            Width = width;
            AllowSize = allowSize;
            AllowMove = allowMove;
            //todo přidat ostaní vlastnosti až jich bude více aby to to bylo slušně srovnané... zatím se nastavují mimo ctor.
        }

        /// <summary>
        /// Název sloupce
        /// </summary>
        public virtual string FieldName { get; private set; }
        /// <summary>
        /// Nadpis sloupce
        /// </summary>
        public virtual string Caption { get; set; }
        /// <summary>
        /// Datový typ sloupce
        /// </summary>
        public virtual Type ColumnType { get; private set; }
        /// <summary>
        /// Viditelnost sloupce
        /// </summary>
        public virtual bool Visible { get; private set; }
        /// <summary>
        /// Index zobrazení sloupce
        /// </summary>
        public virtual int VisibleIndex { get; private set; }
        /// <summary>
        /// Povoluje třídění sloupce
        /// </summary>
        public virtual bool AllowSort { get; set; }
        /// <summary>
        /// Povoluje řádkový filtr a tlačítko zobrazující možnosti filter boxu
        /// </summary>
        public virtual bool AllowFilter { get; set; }
        /// <summary>
        /// Název ikonky pro hlavičku
        /// </summary>
        public virtual string IconName { get; set; }
        /// <summary>
        /// Zarovnání ikonky v hlavičce
        /// </summary>
        public virtual System.Drawing.StringAlignment IconAligment { get; set; }
        /// <summary>
        /// Zarovnání textu v hlavičce
        /// </summary>
        public virtual HorzAlignment HeaderAlignment { get; set; }
        /// <summary>
        /// FontStyle pro header
        /// </summary>
        public virtual System.Drawing.FontStyle HeaderFontStyle { get; set; }
        /// <summary>
        /// Zarovnání textu v column
        /// </summary>
        public virtual HorzAlignment CellAlignment { get; set; }
        /// <summary>
        /// FontStyle pro column
        /// </summary>
        public virtual System.Drawing.FontStyle CellFontStyle { get; set; }
        /// <summary>
        /// Šírka sloupce v px
        /// </summary>
        public virtual int Width { get; set; }
        /// <inheritdoc/>
        public virtual bool AllowSize { get; set; }
        /// <inheritdoc/>
        public virtual bool AllowMove { get; set; }
        /// <summary>
        /// Systemový sloupec, nezobrazuje se v výběru sloupců
        /// </summary>
        public virtual bool IsSystemColumn { get; set; }
        /// <inheritdoc/>
        public virtual bool IsHiddenValue { get; set; }

        /// <summary>
        /// Display text, value, image name
        /// </summary>
        public virtual List<(string DisplayText, string Value, string ImageName)> CodeTable { get; set; }
        /// <summary>
        /// Styl zobrazení editačního stylu. 
        /// </summary>
        public virtual EditStyleViewMode EditStyleViewMode { get; set; }
        /// <summary>
        /// Display format string
        /// </summary>
        public virtual string FormatString { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public virtual string ToolTipIcon { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Text ToolTipu smí obsahovat HTML? Hodnota null = AutoDetect
        /// </summary>
        public virtual bool? ToolTipAllowHtml { get; set; }
        /// <summary>
        /// Více řádková buňka (poznámky ...)
        /// </summary>
        public virtual bool MultiLineCell { get; set; }
        /// <summary>
        /// Sloučení stejných hodnot (distinct)
        /// </summary>
        public virtual bool AllowMerge { get; set; }
        /// <summary>
        /// Sloupec obsahuje obrázek
        /// Předpokládámé, že jakékoliv pole bajtů v datovém zdroji představuje obrázek; přebráno ze starého browse.
        /// </summary>
        public bool IsImageColumnType => this.ColumnType == typeof(byte[]);
        /// <summary>
        /// Sloupec typu string
        /// </summary>
        public bool IsStringColumnType => this.ColumnType == typeof(string);
        /// <inheritdoc/>
        public bool IsDateTimeColumnType => this.ColumnType == typeof(DateTime);
        /// <summary>
        /// Sloupec s editačním stylem
        /// </summary>
        public bool IsEditStyleColumnType => this.CodeTable?.Count > 0;
        /// <inheritdoc/>
        public bool IsNumberColumnType => TypeHelper.IsNumeric(this.ColumnType);
        /// <inheritdoc/>
        public bool IsNumberIntegerColumnType => TypeHelper.IsNumericInteger(this.ColumnType);
        /// <inheritdoc/>
        public virtual DevExpress.XtraGrid.Columns.AutoFilterCondition AutoFilterCondition { get; set; }

    }
    /// <summary>
    /// Interface pro definici sloupců
    /// </summary>
    public interface IGridViewColumn : IToolTipItem
    {
        /// <summary>
        /// Název sloupce
        /// </summary>
        string FieldName { get; }
        /// <summary>
        /// Nadpis sloupce
        /// </summary>
        string Caption { get; }
        /// <summary>
        /// Datový typ sloupce
        /// </summary>
        Type ColumnType { get; }
        /// <summary>
        /// Viditelnost sloupce
        /// </summary>
        bool Visible { get; }
        /// <summary>
        /// Index zobrazení sloupce
        /// </summary>
        int VisibleIndex { get; }
        /// <summary>
        /// Povoluje třídění sloupce
        /// </summary>
        bool AllowSort { get; }
        /// <summary>
        /// Povoluje řádkový filtr a tlačítko zobrazující možnosti filter boxu
        /// </summary>
        bool AllowFilter { get; }
        /// <summary>
        /// Název ikonky pro hlavičku
        /// </summary>
        string IconName { get; }
        /// <summary>
        /// Zarovnání ikonky v hlavičce
        /// </summary>
        System.Drawing.StringAlignment IconAligment { get; }
        /// <summary>
        /// Zarovnání textu v hlavičce
        /// </summary>
        HorzAlignment HeaderAlignment { get; }
        /// <summary>
        /// FontStyle pro header
        /// </summary>
        System.Drawing.FontStyle HeaderFontStyle { get; }
        /// <summary>
        /// Zarovnání textu v column
        /// </summary>
        HorzAlignment CellAlignment { get; }
        /// <summary>
        /// FontStyle pro column
        /// </summary>
        System.Drawing.FontStyle CellFontStyle { get; }
        /// <summary>
        /// Šírka sloupce v px
        /// </summary>
        int Width { get; }
        /// <summary>
        /// Povolení změny šířky sloupce
        /// </summary>
        bool AllowSize { get; }
        /// <summary>
        /// Povolení přesouvat sloupec (pořadí)
        /// </summary>
        bool AllowMove { get; }
        /// <summary>
        /// Systemový sloupec, nezobrazuje se v výběru sloupců
        /// </summary>
        bool IsSystemColumn { get; }
        /// <summary>
        /// Sloupec obsahuje skryté atributy (slouží pro grafické odlišení - kalíšek)
        /// </summary>
        bool IsHiddenValue { get; }
        /// <summary>
        /// Display text, value, image name
        /// </summary>
        List<(string DisplayText, string Value, string ImageName)> CodeTable { get; }
        /// <summary>
        /// Styl zobrazení editačního stylu. 
        /// </summary>
        EditStyleViewMode EditStyleViewMode { get; }
        /// <summary>
        /// Display format string
        /// </summary>
        string FormatString { get; }
        /// <summary>
        /// Více řádková buňka (poznámky ...)
        /// </summary>
        bool MultiLineCell { get; }
        /// <summary>
        /// Sloučení stejných hodnot (distinct)
        /// </summary>
        bool AllowMerge { get; }
        /// <summary>
        /// Sloupec typu image
        /// </summary>
        bool IsImageColumnType { get; }
        /// <summary>
        /// Sloupec typu string
        /// </summary>
        bool IsStringColumnType { get; }
        /// <summary>
        /// Sloupce typu číslo
        /// </summary>
        bool IsNumberColumnType { get; }
        /// <summary>
        /// Sloupce typu celočíselné číslo
        /// </summary>
        bool IsNumberIntegerColumnType { get; }
        /// <summary>
        /// Sloupec typu DateTime
        /// </summary>
        bool IsDateTimeColumnType { get; }
        /// <summary>
        /// Sloupec s editačním stylem
        /// </summary>
        bool IsEditStyleColumnType { get; }
        ///// <summary>
        ///// Sloupec typu ikona
        ///// </summary>
        //bool IsCodeTableForServerResourceBin { get; }
        /// <summary>
        /// Výchozí typ podmínky v řádkovém filtru
        /// </summary>
        DevExpress.XtraGrid.Columns.AutoFilterCondition AutoFilterCondition { get; }
    }

    /// <summary>
    /// Třída pro dodatečnou deficici vlastností buňky, převázně se jedná o grafické vlastnosti (styl, image)
    /// </summary>
    public class GridViewCellData : IGridViewCell
    {
        private Tuple<int, string> _id = null;
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GridViewCellData() : base()
        {
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="viewRowIndex"></param>
        /// <param name="markImage"></param>
        /// <param name="styleName"></param>
        /// <param name="exactAttrColor"></param>
        public GridViewCellData(int viewRowIndex, string fieldName, string styleName, string markImage, Color? exactAttrColor)
        {
            FieldName = fieldName;
            ViewRowIndex = viewRowIndex;
            Id = CreateId(viewRowIndex, fieldName);
            StyleName = styleName;
            MarkImage = markImage;
            ExactAttrColor = exactAttrColor;
        }
        /// <inheritdoc/>
        public virtual string FieldName { get; private set; }
        /// <inheritdoc/>
        public virtual int ViewRowIndex { get; private set; }
        /// <inheritdoc/>
        public virtual Tuple<int, string> Id
        {
            get
            {
                if (_id == null) _id = CreateId(ViewRowIndex, FieldName);
                return _id;
            }
            private set { _id = value; }
        }
        /// <inheritdoc/>
        public virtual string StyleName { get; private set; }
        /// <inheritdoc/>
        public virtual string MarkImage { get; private set; }
        /// <inheritdoc/>
        public virtual Color? ExactAttrColor { get; private set; }
        /// <inheritdoc/>
        public virtual string ToolTipIcon { get; set; }
        /// <inheritdoc/>
        public virtual string ToolTipTitle { get; set; }
        /// <inheritdoc/>
        public virtual string ToolTipText { get; set; }
        /// <inheritdoc/>
        public virtual bool? ToolTipAllowHtml { get; set; }
        /// <summary>
        /// Vytvoří unikátní ID ma základě indexu řádku a názvu sloupce
        /// </summary>
        /// <param name="viewRowIndex"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static Tuple<int, string> CreateId(int viewRowIndex, string fieldName)
        {
            return new Tuple<int, string>(viewRowIndex, fieldName);
        }
        /// <inheritdoc/>
        public virtual StyleInfo StyleInfo { get { return DxComponent.GetStyleInfo(StyleName, ExactAttrColor); } }
    }

    /// <summary>
    /// Interface pro dodatečnou deficici vlastností buňky, převázně se jedná o grafické vlastnosti (styl, image)
    /// </summary>
    public interface IGridViewCell : IToolTipItem
    {
        /// <summary>
        /// Název sloupce
        /// </summary>
        string FieldName { get; }
        /// <summary>
        /// Číslo řádky
        /// </summary>
        int ViewRowIndex { get; }
        /// <summary>
        /// Id, dvojice  <see cref="ViewRowIndex"/> a <see cref="FieldName"/>
        /// </summary>
        Tuple<int, string> Id { get; }
        /// <summary>
        /// Název stylu (kalíšku)
        /// </summary>
        string StyleName { get; }
        /// <summary>
        /// Konkrétní název obrázku, použije se místo Stylu
        /// </summary>
        string MarkImage { get; }
        /// <summary>
        /// Konrétní barva atributu. Má přednost před <see cref="StyleName"/>, ve kterém je pro tento případ přenesena hodnota barvy.
        /// </summary>
        System.Drawing.Color? ExactAttrColor { get; }
        /// <summary>
        /// StyleInfo (kalíšek)
        /// </summary>
        StyleInfo StyleInfo { get; }
    }


    #region Deklarace delegátů a tříd pro eventhandlery, další enumy


    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridDoubleClickHandler(object sender, DxGridDoubleClickEventArgs args);

    public class DxGridDoubleClickEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridDoubleClickEventArgs(string columnName, int rowId, bool modifierKeyCtrl = false, bool modifierKeyAlt = false, bool modifierKeyShift = false)
        {
            ColumnName = columnName;
            RowId = rowId;
            ModifierKeyCtrl = modifierKeyCtrl;
            ModifierKeyAlt = modifierKeyAlt;
            ModifierKeyShift = modifierKeyShift;
        }

        /// <summary>
        /// Columnname (FieldName)
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// Id řádku
        /// </summary>
        public int RowId { get; set; }
        public bool ModifierKeyCtrl { get; private set; }
        public bool ModifierKeyAlt { get; private set; }
        public bool ModifierKeyShift { get; private set; }
    }


    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridFocusedRowChangedHandler(object sender, DxGridFocusedRowChangedEventArgs args);

    public class DxGridFocusedRowChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridFocusedRowChangedEventArgs(int prevFocusedRowHandle, int focusedRowHandle)
        {
            this.FocusedRowHandle = focusedRowHandle;
            this.PrevFocusedRowHandle = prevFocusedRowHandle;
        }

        /// <summary>
        /// Aktuálně fokusovaný řádek
        /// </summary>
        public int FocusedRowHandle { get; set; }

        /// <summary>
        /// Předchozí fokusovaný řádek
        /// </summary>
        public int PrevFocusedRowHandle { get; set; }
    }

    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridSelectionChangedHandler(object sender, DxGridSelectionChangedChangedEventArgs args);

    public class DxGridSelectionChangedChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridSelectionChangedChangedEventArgs(List<int> selectedRows, bool selectedAllRows)
        {
            this.SelectedRows = selectedRows;
            this.SelectedAllRows = selectedAllRows;
        }

        /// <summary>
        /// Aktuálně vybrané řádky
        /// </summary>
        public List<int> SelectedRows { get; set; }
        /// <summary>
        /// Selectované všechny řádky
        /// </summary>
        public bool SelectedAllRows { get; set; }

    }

    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridLoadMoreRowsHandler(object sender, DxGridLoadMoreRowsEventArgs args);

    public class DxGridLoadMoreRowsEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridLoadMoreRowsEventArgs()
        {
        }
    }

    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridShowGroupPanelChangedHandler(object sender, EventArgs args);


    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridKeyDownHandler(object sender, KeyEventArgs args);

    public delegate void DxGridContextMenuHandler(object sender, DxGridContextMenuEventArgs args);

    public delegate void DxGridSummaryRowForAllRowsHandler(object sender, EventArgs args);

    public delegate void DxLoadAllRowsHandler(object sender, EventArgs args);

    /// <summary>
    /// Argumenty pro zobrazení konetextového menu
    /// </summary>
    public class DxGridContextMenuEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridContextMenuEventArgs(DxGridHitInfo dxGridHitInfo, string menuArea)
        {
            this.DxGridHitInfo = dxGridHitInfo;
            this.MenuArea = menuArea;
        }
        public DxGridHitInfo DxGridHitInfo { get; private set; }
        /// <summary>
        /// Oblast menu
        /// </summary>
        public string MenuArea { get; private set; }

    }
    /// <summary>
    /// Informace o místu kde bylo kliknuto
    /// </summary>
    public class DxGridHitInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gridHitInfo"></param>
        /// <param name="mousePoint"></param>
        public DxGridHitInfo(GridHitInfo gridHitInfo, Point mousePoint)
        {
            this.GridHit = gridHitInfo;
            MousePoint = mousePoint;
        }
        /// <summary>
        /// Info DevExpress
        /// </summary>
        public GridHitInfo GridHit { get; private set; }
        /// <summary>
        /// Pozice kurzoru kde bylo kliknuto
        /// </summary>
        public Point MousePoint { get; private set; }
        /// <summary>
        /// Číslo řádky
        /// </summary>
        public int RowHandle { get { return GridHit.RowHandle; } }
        /// <summary>
        /// Název sloupce
        /// </summary>
        public string ColumnName { get { return GridHit.Column?.FieldName; } }
    }


    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxLoadGroupRowsHandler(object sender, DxLoadGroupRowEventArgs args);

    /// <summary>
    /// EventArgs pro donačítání datových řádků pro skupinu
    /// </summary>
    public class DxLoadGroupRowEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLoadGroupRowEventArgs(int? groupId, int group, bool allRowsOfGroup, int? groupPrimaryKey)
        {
            GroupId = groupId;
            Group = group;
            AllRowsOfGroup = allRowsOfGroup;
            GroupPrimaryKey = groupPrimaryKey;
        }

        /// <summary>
        /// GroupId. V tuhle chvíli nadbytečné, protože pracujeme s <see cref="GroupPrimaryKey"/>
        /// </summary>
        public int? GroupId { get; }

        /// <summary>
        /// Číslo skupiny
        /// </summary>
        public int Group { get; }

        /// <summary>
        /// Načtení všech řádků pro dané číslo skupiny <see cref="Group"/>.
        /// </summary>
        public bool AllRowsOfGroup { get; set; }

        /// <summary>
        /// Identifikátor skupiny, primární klíč. Snad bychom to mohli časem změnit na jeden unikátní identifikátor řádky.
        /// </summary>
        public int? GroupPrimaryKey { get; }
    }

    #region enums
    /// <summary>
    /// Způsob zobrazení editačního stylu
    /// </summary>
    public enum EditStyleViewMode
    {
        /// <summary>
        ///Edit style view as only text. This is Default value. Compatible with old HELIOS Green versions.
        /// </summary>
        Text = 0,
        /// <summary>
        ///Edit style view as icon.
        /// </summary>
        Icon = 1,
        /// <summary>
        ///Edit style view as icon and text.
        /// </summary>
        IconText = 2
    }

    /// <summary>
    /// Akce která proběhla v DxGridView
    /// </summary>
    public enum DxGridViewActionType
    {
        /// <summary>None</summary>
        None,
        /// <summary>RowFocusedChanged</summary>
        RowFocusedChanged,
        /// <summary>SelectedRowsChanged</summary>
        SelectedRowsChanged,
        /// <summary>NodeDoubleClick</summary>
        RowDoubleClick,
        /// <summary>KeyDown</summary>
        KeyDown,
        /// <summary>ShowContextMenu</summary>
        ShowContextMenu
    }
    #endregion

    #endregion

    #region Help třídy
    internal static class TypeHelper
    {
        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
    {
        typeof(int),  typeof(double),  typeof(decimal),
        typeof(long), typeof(short),   typeof(sbyte),
        typeof(byte), typeof(ulong),   typeof(ushort),
        typeof(uint), typeof(float)
    };
        private static readonly HashSet<Type> NumericIntegerTypes = new HashSet<Type>
    {
        typeof(int),
        typeof(long), typeof(short),   typeof(sbyte),
        typeof(byte), typeof(ulong),   typeof(ushort),
        typeof(uint)
    };

        /// <summary>
        /// Return true for numeric Type
        /// </summary>
        /// <param name="myType"></param>
        /// <returns></returns>
        internal static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        /// <summary>
        /// Return true for intergral numeric Type
        /// </summary>
        /// <param name="myType"></param>
        /// <returns></returns>
        internal static bool IsNumericInteger(this Type myType)
        {
            return NumericIntegerTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }
    }
    /// <summary>
    /// Třída která se stará o informace a stav group řádky
    /// </summary>
    internal class GroupRowInfo
    {
        public int Group { get; set; }
        public int GroupId { get; set; }
        public int? ParentGroupId { get; set; }
        public bool Expanded { get; set; }
        public bool DataRowsLoaded { get; set; }
    }

    /// <summary>
    /// Info za celou groupu
    /// </summary>
    internal class GroupInfo : IToolTipItem
    {
        public GroupInfo(int group, bool expanded, string groupDescription, string styleName)
        {
            Group = group;
            Expanded = expanded;
            GroupDescription = groupDescription;
            StyleInfo = DxComponent.GetStyleInfo(styleName, null);
        }
        public int Group { get; private set; }
        public bool Expanded { get; set; }
        //public bool DataRowsLoaded { get; set; }
        public string GroupDescription { get; private set; }
        /// <summary>
        /// StyleInfo (kalíšek)
        /// </summary>
        public StyleInfo StyleInfo { get; private set; }

        public string ToolTipIcon => string.Empty;

        public string ToolTipTitle => GroupDescription;

        public string ToolTipText => string.Empty;

        public bool? ToolTipAllowHtml => null;

        public void RefreshStyleInfo()
        {
            StyleInfo = DxComponent.GetStyleInfo(StyleInfo.Name, null);
        }
    }
    #endregion


    /// <summary>
    /// Slouží pro konverzi (patch) <see cref="CriteriaOperator"/>. Aktuálně se zde řeší problematika zástupných znaků '%' pro fuknce Contains a StartsWith
    /// https://supportcenter.devexpress.com/ticket/details/t320172/how-to-traverse-through-and-modify-the-criteriaoperator-instances
    /// Visit metod je více, mrkni do dokumentace. Zde jsou použity jen ty co potřebujeme
    /// </summary>
    internal class CriteriaPatcherConvertOperatorWithWildcard : ClientCriteriaLazyPatcherBase.AggregatesCommonProcessingBase
    {
        /// <summary>
        /// Určuje "směr" konverze: True - Server to component, False - component to server
        /// </summary>
        private readonly bool _patchToComponent;
        public CriteriaPatcherConvertOperatorWithWildcard(bool patchToComponent)
        {
            _patchToComponent = patchToComponent;
        }
        /// <summary>
        /// Spustí konverzi (patch) CriteriaOperatoru
        /// </summary>
        /// <param name="source"></param>
        /// <param name="patchToComponent"></param>
        /// <returns></returns>
        public static CriteriaOperator Patch(CriteriaOperator source, bool patchToComponent)
        {
            return new CriteriaPatcherConvertOperatorWithWildcard(patchToComponent).Process(source);
        }

        private static bool IsNull(CriteriaOperator theOperator)
        {
            return object.ReferenceEquals(theOperator, null);
        }

        public override CriteriaOperator Visit(BinaryOperator theOperator)
        {
            if (!_patchToComponent)
            {//to server
                if (theOperator.LeftOperand is OperandProperty operandProperty && theOperator.RightOperand is OperandValue operandValue && DxGridView.IsWildCardNull(operandValue.Value?.ToString()))
                {
                    if (theOperator.OperatorType == BinaryOperatorType.Equal)
                    {
                        //konverze = <NULL> na funkci IsNullOrEmpty
                        return _CreateCriteriaOperatorIsNull(operandProperty.PropertyName);
                    }
                    else if (theOperator.OperatorType == BinaryOperatorType.NotEqual)
                    {
                        //konverze != <NULL> na funkci Not IsNullOrEmpty
                        return _CreateCriteriaOperatorIsNotNull(operandProperty.PropertyName);
                    }
                }
            }

            return base.Visit(theOperator);
        }

        public override CriteriaOperator Visit(FunctionOperator theOperator)
        {
            if (_patchToComponent)
            {//to component
                if (theOperator.OperatorType == FunctionOperatorType.Custom)
                {
                    if (_TryGetOperandsForValuePropertyNameValue(theOperator, out string value1, out string propertyName, out string value2))
                    {
                        if (value1.ToLower() == "like")
                        {//Převod like na původní funkce
                            //začíná
                            if (!value2.StartsWith("%") && value2.Contains("%"))
                            {
                                theOperator = (FunctionOperator)_CreateCriteriaOperatorStartsWith(propertyName, _RemovePercentCharacter(value2, false, true));
                            }
                            //obsahuje (neobsahuje)
                            if (value2.StartsWith("%") && value2.LastIndexOf('%') > 0)
                            {
                                theOperator = (FunctionOperator)_CreateCriteriaOperatorContains(propertyName, _RemovePercentCharacter(value2, true, true));
                            }
                        }
                    }
                }
                else if (theOperator.OperatorType == FunctionOperatorType.IsNullOrEmpty)
                {
                    //konverze funkce IsNullOrEmpty na = <NULL>. (negaci Not neřeším, tak se tam přidá sama sem do Visit nechodí s Not, je na to jiný Visit)
                    return CriteriaOperator.Parse($"{theOperator.Operands[0]} == '{DxGridView.WildCardNull}'");
                }
            }
            else
            {//to server
                if (theOperator.OperatorType == FunctionOperatorType.StartsWith)
                {//začíná
                    if (_TryGetOperandsForPropertyNameAndValue(theOperator, out string propertyName, out string value))
                    {
                        if (value.Contains('%'))
                        {
                            theOperator = (FunctionOperator)_CreateCriteriaOperatorLike(propertyName, _AddPercentCharacter(value, false, true));
                        }
                    }
                }
                else if (theOperator.OperatorType == FunctionOperatorType.Contains)
                {//obsahuje (neobsahuje)
                    if (_TryGetOperandsForPropertyNameAndValue(theOperator, out string propertyName, out string value))
                    {
                        if (value.Contains('%'))
                        {
                            theOperator = (FunctionOperator)_CreateCriteriaOperatorLike(propertyName, _AddPercentCharacter(value, true, true));
                        }
                    }
                }
            }
            return theOperator;
        }
        /// <summary>
        /// Pro pole Operands obsahující kombinaci: PropertyName, Value. Pokud je value Null, tak se vrací false.
        /// </summary>
        /// <param name="theOperator"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool _TryGetOperandsForPropertyNameAndValue(FunctionOperator theOperator, out string propertyName, out string value)
        {
            propertyName = null;
            value = null;

            bool parsed = false;
            if (theOperator.Operands.Count() == 2)
            {
                if (theOperator.Operands[0] is OperandProperty operandProperty)
                {
                    propertyName = operandProperty.PropertyName;
                    parsed = true;
                }
                else { parsed = false; }
                if (theOperator.Operands[1] is OperandValue operandValue && operandValue.Value != null)
                {
                    value = operandValue.Value.ToString();
                    parsed = parsed && true;
                }
                else { parsed = false; }
            }
            return parsed;
        }
        /// <summary>
        /// Pro pole Operands obsahující kombinaci: Value, PropertyName, Value
        /// Například pro Like funkce ("Like", PropertyName, "value")
        /// Pokud je value Null, tak se vrací false.
        /// </summary>
        /// <param name="theOperator"></param>
        /// <param name="propertyName"></param>
        /// <param name="value2"></param>
        /// <param name="value1"></param>
        /// <returns></returns>
        private bool _TryGetOperandsForValuePropertyNameValue(FunctionOperator theOperator, out string value1, out string propertyName, out string value2)
        {
            propertyName = null;
            value1 = null;
            value2 = null;

            bool parsed = false;
            if (theOperator.Operands.Count() == 3)
            {
                if (theOperator.Operands[0] is OperandValue operandValue1 && operandValue1.Value != null)
                {
                    value1 = operandValue1.Value.ToString();
                    parsed = true;
                }
                else { parsed = false; }
                if (theOperator.Operands[1] is OperandProperty operandProperty)
                {
                    propertyName = operandProperty.PropertyName;
                    parsed = parsed && true;
                }
                else { parsed = false; }
                if (theOperator.Operands[2] is OperandValue operandValue2 && operandValue2.Value != null)
                {
                    value2 = operandValue2.Value.ToString();
                    parsed = parsed && true;
                }
                else { parsed = false; }
            }
            return parsed;
        }

        private CriteriaOperator _CreateCriteriaOperatorLike(string propertyName, string value)
        {
            return CriteriaOperator.Parse($"[{propertyName}] Like ?", value);
        }
        private CriteriaOperator _CreateCriteriaOperatorStartsWith(string propertyName, string value)
        {
            return CriteriaOperator.Parse($"StartsWith([{propertyName}], ?)", value);
        }
        private CriteriaOperator _CreateCriteriaOperatorContains(string propertyName, string value)
        {
            return CriteriaOperator.Parse($"Contains([{propertyName}], ?)", value);
        }
        private CriteriaOperator _CreateCriteriaOperatorIsNull(string propertyName)
        {
            return CriteriaOperator.Parse($"IsNullOrEmpty([{propertyName}])");
        }
        private CriteriaOperator _CreateCriteriaOperatorIsNotNull(string propertyName)
        {
            return CriteriaOperator.Parse($"Not IsNullOrEmpty([{propertyName}])");
        }
        /// <summary>
        /// Přidá znak '%' na začátek a na konec řetězce, pokud tam již není. 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="atBegin"></param>
        /// <param name="atEnd"></param>
        /// <returns></returns>
        private string _AddPercentCharacter(string value, bool atBegin, bool atEnd)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            if (atBegin && !value.StartsWith("%")) { value = $"%{value}"; }
            if (atEnd && !value.EndsWith("%")) { value = $"{value}%"; }
            return value;
        }
        /// <summary>
        /// Odebere znak '%' ze začátku a konce řetězce (podle nastavení parametrů)"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="atBegin"></param>
        /// <param name="atEnd"></param>
        /// <returns></returns>
        private string _RemovePercentCharacter(string value, bool atBegin, bool atEnd)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value; // Prázdný nebo null vstup, nemáme co odstraňovat.
            }

            if (atBegin && value.StartsWith("%"))
            {
                value = value.Substring(1);
            }

            if (atEnd && value.EndsWith("%"))
            {
                value = value.Substring(0, value.Length - 1);
            }

            return value;
        }
    }
}