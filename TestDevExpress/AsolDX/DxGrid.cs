using System;
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
using DevExpress.Data.Filtering;
using DevExpress.Utils;
using DevExpress.Utils.Filtering.Internal;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Registrator;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraTab;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Umožnuje split mezi dvěma <see cref="DxGridControl"/>
    /// </summary>
    public class DxGridSplitContainer : DevExpress.XtraGrid.GridSplitContainer
    {
        public DxGridSplitContainer()
        {
            InitProperties();
        }

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

        public void ShowHorizontalSplit()
        {

            if (!IsSplitViewVisible || !Horizontal)
            {
                if (!Horizontal) Horizontal = true;
                ShowSplitView();
            }
        }

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
            // this.Paint += _OnPaintEx;

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
                        //var tempFilterString = view.ActiveFilterString;    //TODO tohle tu je dočasně. musím vyřešit jak nastavovat zpět filtr ze serveru.
                        //bool temActiveFilterEnabled = view.ActiveFilterEnabled;
                        this.DataSourceChanging = true;
                        base.DataSource = value;
                        //if (view.ActiveFilterString != tempFilterString)
                        //{
                        //    view.ActiveFilterString = tempFilterString;
                        //    view.ActiveFilterEnabled = temActiveFilterEnabled;
                        //}
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
            return;
            //Pokus o malování plné čáry aktivního řádku. Zatím to má řadů problémů
            //1.každý skin by potřeboval mít definovanou výraznou barvu, pokud použiji barvu, která je použita pro označené řádky Highlight, tak to není tak výrazné jako default

            //Malování ohraničení akivního (focusovaného) řádku
            GridControl grid = sender as GridControl;
            DevExpress.XtraGrid.Views.Grid.GridView view = grid.FocusedView as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null) return;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridViewInfo viewInfo = view.GetViewInfo() as DevExpress.XtraGrid.Views.Grid.ViewInfo.GridViewInfo;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridRowInfo rowInfo = viewInfo.GetGridRowInfo(view.FocusedRowHandle);
            if (rowInfo == null)
                return;
            Rectangle r = Rectangle.Empty;
            r = rowInfo.DataBounds;
            if (r != Rectangle.Empty)
            {
                r.Height -= 2;
                r.Width -= 2;

                var highlight = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_Highlight);
                Pen pen = Pens.Green;
                if (highlight != null)
                {
                    pen = new Pen(highlight.Value);
                }
                e.Graphics.DrawRectangle(pen, r);
            }
            //grid.Invalidate();  //překleslení gridu. Když to tu nebylo a chtěl jsem rámeček z venku řádku, tak zůstávali "duchové" pře skoku na další řádek https://supportcenter.devexpress.com/ticket/details/t114511/draw-custom-row-border-for-selected-row
            //tady jsem ještě našel že existuje InvalidateRow()... to by šlo použít pro překreslední jen řádku ze kterého odcházím (okolní), aby tam nezůstávali čáry z rámečku...


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
            this.DxToolTipController.BeforeShow += DxToolTipController_BeforeShow;

        }

        private void DxToolTipController_BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            e.ToolTip = "";
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
                //tooltip pro column header
                if (hitInfo.InColumnPanel)
                {
                    var column = view.GridViewColumns?.FirstOrDefault(x => x.FieldName == hitInfo.Column?.FieldName);
                    if (column != null)
                    {
                        // Pokud myš nyní ukazuje na ten samý Node, pro který už máme ToolTip vytvořen, pak nebudeme ToolTip připravovat:
                        bool isSameAsLast = (args.DxSuperTip != null && Object.ReferenceEquals(args.DxSuperTip.ClientData, column));
                        if (!isSameAsLast)
                        {   // Připravíme data pro ToolTip:
                            var dxSuperTip = DxComponent.CreateDxSuperTip(column);        // Vytvořím new data ToolTipu
                            if (dxSuperTip != null)
                            {
                                if (ToolTipAllowHtmlText.HasValue) dxSuperTip.ToolTipAllowHtmlText = ToolTipAllowHtmlText;
                                dxSuperTip.ClientData = column;                           // Přibalím si do nich náš Node abych příště detekoval, zda jsme/nejsme na tom samém
                            }
                            args.DxSuperTip = dxSuperTip;
                            args.ToolTipChange = DxToolTipChangeType.NewToolTip;                 // Zajistím rozsvícení okna ToolTipu
                        }
                        else
                        {
                            args.ToolTipChange = DxToolTipChangeType.SameAsLastToolTip;          // Není třeba nic dělat, nechme svítit stávající ToolTip
                        }
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

    public class DxGridViewInfoRegistrator : GridInfoRegistrator
    {
        public override string ViewName { get { return "DxGridView"; } }

        public override BaseView CreateView(GridControl grid) { return new DxGridView(grid as GridControl); }
    }

    /// <summary>
    /// Datový pohled typu <see cref="GridView"/>
    /// </summary>
    public class DxGridView : DevExpress.XtraGrid.Views.Grid.GridView
    {
        #region Properties and members
        /// <inheritdoc/>
        protected override string ViewName { get { return "DxGridView"; } }
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

        public int LastActiveRow { get; set; }
        public int ActualActiveRow { get; set; }

        /// <summary>CZ: Je aktivni nejaka bunka v radkovem filtru</summary>
        public bool IsFilterRowCellActive { get { return this.IsFilterRow(this.ActualActiveRow); } }

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
        public bool ShowGroupPanel { get => _showGroupPanel; set { _showGroupPanel = value; _SetShowGroupPanel(); } }


        private bool _alignGroupSummaryInGroupRow;
        /// <summary>
        /// Určuje typ zobrazování sumy v group řádcích
        /// </summary>
        public bool AlignGroupSummaryInGroupRow { get => _alignGroupSummaryInGroupRow; set { _alignGroupSummaryInGroupRow = value; _SetAlignGroupSummaryInGroupRow(); } }

        private string _LastFilterRowCell { get; set; } = String.Empty;

        private bool _RowFilterVisible { get { return this.OptionsView.ShowAutoFilterRow; } /*set;*/ }


        /// <summary>
        /// Slouží pro uložení informací o sloupcích, kterými byla provedena inicializace View
        /// </summary>
        internal List<IGridViewColumn> GridViewColumns { get; set; }

        /// <summary>
        /// slouží pro uložení dodatečných informací o buňce pomocí <see cref="IGridViewCell"/>. Klíčem je kombinace rowIndex a a fieldName 
        /// </summary>
        internal Dictionary<Tuple<int, string>, IGridViewCell> GridViewCells { get; set; }

        #endregion

        #region konstruktory
        public DxGridView() : this(null) { }

        public DxGridView(DevExpress.XtraGrid.GridControl grid) : base(grid)
        {
            _InitGridView();
            _RegisterEventsHandlers();
        }
        #endregion

        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void _InitGridView()
        {

            _SetGridMultiSelectAndMode();

            OptionsSelection.EnableAppearanceFocusedCell = false;   //zakáz selectu cell, ale pouze barva, ohraničení zůstává

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

            //---Groupování TEST
            OptionsView.ShowGroupPanel = false;

            OptionsMenu.ShowGroupSummaryEditorItem = true;
            OptionsMenu.ShowSummaryItemMode = DefaultBoolean.True;
            OptionsMenu.EnableFooterMenu = true;
            OptionsMenu.EnableGroupRowMenu = true;
    

            //this.OptionsBehavior.AlignGroupSummaryInGroupRow = DefaultBoolean.True;
            _SetAlignGroupSummaryInGroupRow();

            //----

        }
        private void _RegisterEventsHandlers()
        {
            this.FilterPopupExcelCustomizeTemplate += _OnFilterPopupExcelCustomizeTemplate;
            this.ColumnFilterChanged += _OnColumnFilterChanged;
            this.CustomRowFilter += _OnCustomRowFilter;

            this.DoubleClick += _OnDoubleClick;

            this.SelectionChanged += _OnSelectionChanged;
            this.FocusedRowChanged += _OnFocusedRowChanged;
            this.FocusedColumnChanged += _OnFocusedColumnChanged;

            // sorting events
            this.StartSorting += _OnStartSorting;   //zatím nepoužívám
            this.CustomColumnSort += _OnCustomColumnSort;
            this.EndSorting += _OnEndSorting;   //zatím nepoužívám; registruji přímo v ListGrid

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

            this.MouseUp += _OnMouseUp;
        }

        private void _OnMouseUp(object sender, MouseEventArgs e)
        {
            //nastavení clipboardu při alt left click. TODO NotifyToast??
            if (e.Button == System.Windows.Forms.MouseButtons.Left && System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control) //LButton and ALT only
            {
                var doSetClipboardValue = false;
                string valueString = string.Empty;
                if (doSetClipboardValue)
                {
                    if (String.IsNullOrEmpty(valueString))
                        System.Windows.Forms.Clipboard.Clear();
                    else
                        System.Windows.Forms.Clipboard.SetText(valueString, System.Windows.Forms.TextDataFormat.UnicodeText);
                    //Globals.NotifyToast(this, null, afMessage.GetMessage("CopyBrowseCell", columnName), System.Windows.Forms.ToolTipIcon.Info);
                    return;
                }
            }
        }

        private void _OnCustomDrawFooterCell(object sender, FooterCellCustomDrawEventArgs e)
        {
            GridSummaryItem summary = e.Info.SummaryItem;
            if (_summaryRowData != null)
            {
                IGridViewColumn gv = GridViewColumns.FirstOrDefault(x => x.FieldName == e.Column.FieldName);
                Type type = gv?.ColumnType;
                if (type == typeof(decimal))
                {
                    decimal value = Convert.ToDecimal(_summaryRowData[e.Column.FieldName]);
                    string formatedValue = value.ToString(gv?.FormatString);
                    e.Info.DisplayText = formatedValue;
                }
                else if (type == typeof(int) || type == typeof(Int32) || type == typeof(Int16))
                {
                    int value = Convert.ToInt32(_summaryRowData[e.Column.FieldName]);
                    string formatedValue = value.ToString(gv?.FormatString);
                    e.Info.DisplayText = formatedValue;
                }
            }
        }

        /// <summary>
        /// volá se pro typy Custom v summačním řádku...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnCustomSummaryCalculate(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {

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
        private void _UnRegisterEventsHandlers()
        {
            this.FilterPopupExcelCustomizeTemplate -= _OnFilterPopupExcelCustomizeTemplate;
            this.ColumnFilterChanged -= _OnColumnFilterChanged;
            this.CustomRowFilter -= _OnCustomRowFilter;

            this.DoubleClick -= _OnDoubleClick;

            this.SelectionChanged -= _OnSelectionChanged;
            this.FocusedRowChanged -= _OnFocusedRowChanged;
            this.FocusedColumnChanged -= _OnFocusedColumnChanged;

            // sorting events
            this.StartSorting -= _OnStartSorting;   //zatím nepoužívám
            this.CustomColumnSort -= _OnCustomColumnSort;
            this.EndSorting -= _OnEndSorting;   //zatím nepoužívám; registruji přímo v ListGrid

            this.KeyDown -= _OnKeyDown;
            this.KeyPress -= _OnKeyPress;

            this.CustomRowCellEdit -= _OnCustomRowCellEdit;

            // ScrollBar:
            this.TopRowChanged -= _OnTopRowChanged;
            this.CalcRowHeight -= View_CalcRowHeight;
            this.CustomDrawScroll -= View_CustomDrawScroll;

            this.Layout -= _OnLayout;

            this.PopupMenuShowing -= _OnPopupMenuShowing;

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

        #region Select rows
        private bool _selectedAllRows;
        /// <summary>
        /// Selectování všech řádků
        /// </summary>
        public bool SelectedAllRows { get => _selectedAllRows; set { _SetSelectedAllRows(value); } }
        private void _SetSelectedAllRows(bool value)
        {
            if (_selectedAllRows != value)
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
                foreach (int rowIndex in value)
                {
                    this.SelectRow(rowIndex);
                }
            }
        }
        /// <inheritdoc/>
        public override void SelectRow(int rowHandle)
        {
            if (!_SelectedRowsCache.Contains(rowHandle))
                _SelectedRowsCache.Add(rowHandle);
            base.SelectRow(rowHandle);
        }
        /// <inheritdoc/>
        public override void UnselectRow(int rowHandle)
        {
            _SelectedRowsCache.Remove(rowHandle);
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

        private void _OnCustomDrawCell(object sender, RowCellCustomDrawEventArgs e)
        {
            // zkontrolujeme, jestli se jedná o buňku s daty
            if (e.RowHandle >= 0 && e.Column.VisibleIndex >= 0)
            {
                if (GridViewCells == null) return;
                if (!GridViewCells.TryGetValue(GridViewCellData.CreateId(e.RowHandle, e.Column.FieldName), out var cell)) return;//nemám data pro cell, tak return

                var style = DxComponent.GetStyleInfo(cell.StyleName, cell.ExactAttrColor);
                //barvičky
                if (style == null) return;
                if (!this.IsRowSelected(e.RowHandle))
                {
                    //barvy
                    if (style.AttributeBgColor != null) e.Appearance.BackColor = style.AttributeBgColor.Value;
                    if (style.AttributeColor != null) e.Appearance.ForeColor = style.AttributeColor.Value;
                }
                if (style.AttributeFontStyle != null) e.Appearance.FontStyleDelta = style.AttributeFontStyle.Value;
            }
        }

        private void _OnPopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            if (e.HitInfo.InDataRow)    //obsluha zatím jen DataRow
            {
                e.Allow = false;
                var hitInfo = new DxGridHitInfo(e.HitInfo, Cursor.Position);
                this.RaiseShowContextMenu(hitInfo);
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
                        this.GridControl.RepositoryItems.Add(repositoryItem);
                        e.RepositoryItem = repositoryItem;
                    }
                }
            }
        }

        private void _OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!IsFilterRowCellActive // nezachytil jsem klavesu a ani nejsem ve filtru
                && _RowFilterVisible
                ) // nemam aktivni zadnou ridici klavesu
            {//první znak v řádkovém filtru
                System.Text.StringBuilder sbString = new System.Text.StringBuilder();
                List<char> specialChars = new List<char>(new char[] { '<', '!', '=', '>', '%', '.', ',', '+', '-' });
                char sKeyChar = e.KeyChar;
                if (char.IsLetterOrDigit(sKeyChar) || specialChars.Contains(sKeyChar))
                {
                    e.Handled = true;
                    SetFocusToFilterRow(_LastFilterRowCell);
                    ShowEditorByKeyPress(e);    //activuje editor pro editaci (nastaví kurzor a vloží první písmeno)
                                                // e.Handled = true;
                }
            }
        }

        private void _OnKeyDown(object sender, KeyEventArgs e)
        {
            //TODO zde bude lokální obsluhu.
            //budu asi dál posílat i když oblsoužím? ale nastavím e.Handled = true;

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
                OnDoubleClick(info.Column?.FieldName ?? null, info.RowHandle, modifierKeyCtrl, modifierKeyAlt, modifierKeyShift);  //Zavoláme public event
            }
        }
        private void _OnFocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            LastActiveRow = e.PrevFocusedRowHandle;
            ActualActiveRow = e.FocusedRowHandle;
            OnFocusedRowChanged(e.PrevFocusedRowHandle, e.FocusedRowHandle);
        }
        private void _OnFocusedColumnChanged(object sender, FocusedColumnChangedEventArgs e)
        {
            if (IsFilterRowCellActive) _LastFilterRowCell = FocusedColumn?.FieldName;
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
        }



        #region Filtering a vše okolo
        /// <summary>
        /// Regulární výraz pro nalezení částí mezi "Like '" a "'"
        /// </summary>
        const string _likePattern = @"Like '.*?'";
        /// <summary>
        /// Výraz filtrovacího řádku
        /// </summary>
        public string RowFilterExpression { get { return _ConvertGetRowFilterExpression(this.ActiveFilterString); } set { this.ActiveFilterString = _ConvertSetRowFilterExpression(value); } }
        /// <summary>
        /// Příznak zda je filtrovací řádek neaktivní
        /// </summary>
        public bool RowFilterIsInactive { get { return !this.ActiveFilterEnabled; } set { this.ActiveFilterEnabled = !value; } }

        /// <summary>
        /// voláno před aplikací filtru do GridView, možnost sestavení vlastní podmínky pro filtr
        /// </summary>
        /// <param name="column"></param>
        /// <param name="condition"></param>
        /// <param name="_value"></param>
        /// <param name="strVal"></param>
        /// <returns></returns>
        protected override DevExpress.Data.Filtering.CriteriaOperator CreateAutoFilterCriterion(DevExpress.XtraGrid.Columns.GridColumn column, DevExpress.XtraGrid.Columns.AutoFilterCondition condition, object _value, string strVal)
        {
            //List<char> specialChars = new List<char>(new char[] { '<', '!', '=', '>', '%', '.', ',', '+', '-' });

            //Základní rozpoznání speciálních znaků pro změnu podmínky
            if (column.ColumnType == typeof(string))
            {
                if (strVal.Count(x => x == '%') > 1 //více jak jedno % ve výrazu
                    || strVal.Count(x => x == '%') == 1 && !strVal.StartsWith("%")) //jen jedno procento, ale ne na začátku
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.Like;
                }
                else if (strVal.StartsWith("%"))
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.Contains;
                    strVal = strVal.Substring(1);
                }
                else if (strVal.StartsWith("!"))
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.DoesNotContain;
                    strVal = strVal.Substring(1);
                }
            }
            else if (TypeHelper.IsNumeric(column.ColumnType))
            {
                if (strVal.StartsWith("<="))
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.LessOrEqual;
                    strVal = strVal.Substring(2);
                    _value = strVal;
                }
                else if (strVal.StartsWith(">="))
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.GreaterOrEqual;
                    strVal = strVal.Substring(2);
                    _value = strVal;
                }
                else if (strVal.StartsWith(">"))
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.Greater;
                    strVal = strVal.Substring(1);
                    _value = strVal;
                }
                else if (strVal.StartsWith("<"))
                {
                    condition = DevExpress.XtraGrid.Columns.AutoFilterCondition.Less;
                    strVal = strVal.Substring(1);
                    _value = strVal;
                }

            }

            //if (column.ColumnType == typeof(DateTime) && strVal.Length > 0)
            //{
            //    BinaryOperatorType type = BinaryOperatorType.Equal;
            //    string operand = string.Empty;
            //    if (strVal.Length > 1)
            //    {
            //        operand = strVal.Substring(0, 2);
            //        if (operand.Equals(">="))
            //            type = BinaryOperatorType.GreaterOrEqual;
            //        else if (operand.Equals("<="))
            //            type = BinaryOperatorType.LessOrEqual;
            //    }
            //    if (type == BinaryOperatorType.Equal)
            //    {
            //        operand = strVal.Substring(0, 1);
            //        if (operand.Equals(">"))
            //            type = BinaryOperatorType.Greater;
            //        else if (operand.Equals("<"))
            //            type = BinaryOperatorType.Less;
            //    }
            //    if (type != BinaryOperatorType.Equal)
            //    {

            //        string val = strVal.Replace(operand, string.Empty);
            //        try
            //        {
            //            DateTime dt = DateTime.ParseExact(val, "d", column.RealColumnEdit.EditFormat.Format);
            //            return new BinaryOperator(column.FieldName, dt, type);
            //        }
            //        catch
            //        {
            //            return null;
            //        }
            //    }
            //}

            return base.CreateAutoFilterCriterion(column, condition, _value, strVal);
        }

        private string _ConvertGetRowFilterExpression(string input)
        {
            //Přidává % na začátku a na konci výrazu Like

            Regex regex = new Regex(_likePattern);
            // Najdeme všechny shody s regulárním výrazem v řetězci
            MatchCollection matches = regex.Matches(input);

            // Nahradíme všechny shody s regulárním výrazem zpět do původního řetězce
            string result = regex.Replace(input, match =>
            {
                string foundExpression = match.Value;
                // Odstraníme prefix "Like '"
                foundExpression = foundExpression.Substring(6);
                // Odstraníme sufix "'"
                foundExpression = foundExpression.Substring(0, foundExpression.Length - 1);

                if (!foundExpression.StartsWith("%"))
                {
                    foundExpression = "%" + foundExpression;
                }
                if (!foundExpression.EndsWith("%"))
                {
                    foundExpression += "%";
                }
                // Vrátíme upravenou část zpět do původního řetězce
                return "Like '" + foundExpression + "'";
            });
            return result;
        }

        private string _ConvertSetRowFilterExpression(string input)
        {
            //Odebírá % na začátku a na konci výrazu Like

            Regex regex = new Regex(_likePattern);
            // Najdeme všechny shody s regulárním výrazem v řetězci
            MatchCollection matches = regex.Matches(input);

            // Nahradíme všechny shody s regulárním výrazem zpět do původního řetězce
            string result = regex.Replace(input, match =>
            {
                string foundExpression = match.Value;
                // Odstraníme prefix "Like '"
                foundExpression = foundExpression.Substring(6);
                // Odstraníme sufix "'"
                foundExpression = foundExpression.Substring(0, foundExpression.Length - 1);

                if (foundExpression.StartsWith("%"))
                {
                    foundExpression = foundExpression.Substring(1);
                }
                if (foundExpression.EndsWith("%"))
                {
                    foundExpression = foundExpression.Substring(0, foundExpression.Length - 1); ;
                }
                // Vrátíme upravenou část zpět do původního řetězce
                return "Like '" + foundExpression + "'";
            });
            return result;
        }

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
            var filterString = this.ActiveFilter.Expression;
            var dataSetWhere = DevExpress.Data.Filtering.CriteriaToWhereClauseHelper.GetDataSetWhere(this.ActiveFilter.Criteria);
            var sqlWhere = DevExpress.Data.Filtering.CriteriaToWhereClauseHelper.GetMsSqlWhere(this.ActiveFilter.Criteria);

            base.ApplyColumnsFilterCore(false, ignoreActiveFilter, forceFindFilter);

            return;
            /*
            if (!DontApplyFilterCore)
            {   // statický režim ala excel, necháme to projít implicitem a šmitec
                // base.ApplyColumnsFilterCore(updateMRU, ignoreActiveFilter, forceFindFilter);
                return;
            }


            // odsud dále kód pro s+td režim filtrování HEG (statický mód filtrování vypnut)

            // pokud ActiveFilter nic neobsahuje, pak byl filtr zrušen
            if (this.ActiveFilter.Count == 0 && this.FindFilterText == "")
            {
                //Retrieve data from sql without where expr ...
                return;
            }

            // filtr něco má, tak to rozpitváme ...
            foreach (ViewColumnFilterInfo vcfi in this.ActiveFilter)
            {
                string colName = vcfi.Column.FieldName;
                Type colType = vcfi.Column.ColumnType;
                var v = vcfi.Filter.Value;

                if (vcfi.Filter.FilterCriteria is FunctionOperator)
                {
                    var op1 = ((FunctionOperator)vcfi.Filter.FilterCriteria).Operands[0];
                    var opType = ((FunctionOperator)vcfi.Filter.FilterCriteria).OperatorType;
                    var op2 = ((FunctionOperator)vcfi.Filter.FilterCriteria).Operands[1];
                }
                else if (vcfi.Filter.FilterCriteria is BinaryOperator)
                {
                    var opLeft = ((BinaryOperator)vcfi.Filter.FilterCriteria).LeftOperand;
                    var opType = ((BinaryOperator)vcfi.Filter.FilterCriteria).OperatorType;
                    var opRight = ((BinaryOperator)vcfi.Filter.FilterCriteria).RightOperand;
                }
                else if (vcfi.Filter.FilterCriteria is UnaryOperator)
                {
                    var op1 = ((UnaryOperator)vcfi.Filter.FilterCriteria).Operand;
                    var opType = ((UnaryOperator)vcfi.Filter.FilterCriteria).OperatorType;
                }
                else if (vcfi.Filter.FilterCriteria is InOperator)
                {
                    var sqlExpr = ((InOperator)vcfi.Filter.FilterCriteria).ToString();  //??
                    var opLeft = ((InOperator)vcfi.Filter.FilterCriteria).LeftOperand;
                    var operands = ((InOperator)vcfi.Filter.FilterCriteria).Operands.ToString();
                }

            }

            // pokud něco obsahuje pole pro fulltextové hledání ..
            if (this.FindFilterText != "")
            {
                // tak musíme nějak zpracovat fulltextové hledání
            }

            // filtr jsme rozpitvali, tak si z toho se stavíme nějaké where a retrievneme data ...
            */

        }
        protected override void OnActiveFilterChanged(object sender, EventArgs e)
        {
            base.OnActiveFilterChanged(sender, e);
        }
        private void _OnFilterPopupExcelCustomizeTemplate(object sender, DevExpress.XtraGrid.Views.Grid.FilterPopupExcelCustomizeTemplateEventArgs e)
        {
            //pokus ovlivnit vlastnoti zobrazovaného excel filtru
            return;
            e.Template.BackColor = System.Drawing.Color.FromArgb(0, 0, 120);
            //BeginInvoke(new Action(() => { (e.Template.Parent.Parent as XtraTabPage).PageEnabled = false; }));
            (e.Template.Parent.Parent as XtraTabPage).PageVisible = false;
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
                // Make the current row visible.
                e.Visible = true;
                // Prevent default processing, so the row will be visible 
                // regardless of the view's filter.
                e.Handled = true;
            }
        }


        /// <summary>
        /// Filtrovací řádek se změnil <see cref="DxRowFilterChanged"/>
        /// </summary>
        public event DxGridRowFilterChangeHandler DxRowFilterChanged;
        /// <summary>
        /// Vyvolá event <see cref="DxRowFilterChanged"/>
        /// </summary>
        protected virtual void OnRowFilterChanged()
        {
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

        protected override void OnBeforeSorting()
        {
            base.OnBeforeSorting();
        }

        protected override void OnAfterSorting()
        {
            base.OnAfterSorting();
        }

        protected override void UpdateSorting()
        {
            base.UpdateSorting();
        }

        protected override void RaiseStartSorting()
        {
            base.RaiseStartSorting();
        }

        protected override void RaiseEndSorting()
        {
            base.RaiseEndSorting();
        }

        protected override void RaiseCustomColumnSort(CustomColumnSortEventArgs e)
        {
            base.RaiseCustomColumnSort(e);
        }

        protected override bool IsColumnAllowSort(DevExpress.XtraGrid.Columns.GridColumn column)
        {
            // přepsáním přijdeme o možnost custom sortingu, důležité je dovnitř poslat e.Handled=true a e.Result=1
            return base.IsColumnAllowSort(column);
        }

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
            //TODO tady to je potřeba ještě doladit, po přepnutí šablony, který mergovala na šablonu, která nemá merge, tak se nezobrazí chcechBoxy
            //povolím pro view merge stejných hodnot (distinct), nastavení je pak individuální v rámci column
            //zapínám jen v případě, že mám info od serveru, že mám pro nějaký sloupec požadavek na merge. Merge má obecně několik omezení: nefunguje multiselect, rozlišení lichý/sudý řádek... https://docs.devexpress.com/WindowsForms/643/controls-and-libraries/data-grid/views/grid-view/cells#cell-merging
            bool allowMerge = this.GridViewColumns.Count(x => x.AllowMerge) > 0;
            OptionsView.AllowCellMerge = allowMerge;
            //CheckBoxRowSelect = CheckBoxRowSelect && !allowMerge;    //ještě vypínám chechBoxRowSelect, protože stejně nefunguje multiselect v případě merge viz výše, a dělá to jen neplechu.
            if (allowMerge) { MultiSelect = false; }

            _InitColumns();
        }


        /// <summary>
        /// Vytvoření koleckce sloupců v gridView
        /// </summary>
        //public void InitColumns(IEnumerable<IGridViewColumn> gridViewColumns)
        private void _InitColumns()
        {
            this.BeginUpdate();
            //GridViewColumns = gridViewColumns;
            this.Columns.Clear();
            foreach (var vc in GridViewColumns)
            {
                int index = this.Columns.Add(CreateGridColumn(vc));
                _InitColumnAfterAdd(index, vc);
                _InitColumnSummaryRow(index, vc);
            }
            this.EndUpdate();
        }
        /// <summary>
        /// Některé vlastnosti GridColumn nejdou nastavit v rámci <see cref="CreateGridColumn(IGridViewColumn)"/>. Je třeba je nastavit až po přidání do kolekce.
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
                    SummaryType = DevExpress.Data.SummaryItemType.Sum,
                    Mode = DevExpress.Data.SummaryMode.Selection,
                    DisplayFormat = "{0:" + gridViewColumn.FormatString + "}"
                };
                gc.Summary.Add(sumItem);
            }
        }

        /// <summary>
        /// Tvorba GridColumn na základě našich dat z <see cref="IGridViewColumn"/>
        /// </summary>
        /// <param name="gridViewColumn"></param>
        /// <returns></returns>
        internal DevExpress.XtraGrid.Columns.GridColumn CreateGridColumn(IGridViewColumn gridViewColumn)
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
            //povolení filtrování
            gc.OptionsFilter.AllowFilter = gridViewColumn.AllowFilter;
            gc.OptionsFilter.AllowAutoFilter = gridViewColumn.AllowFilter;
            gc.OptionsFilter.AutoFilterCondition = gridViewColumn.AutoFilterCondition;

            //šířka sloupce
            if (gridViewColumn.Width > 0) gc.Width = gridViewColumn.Width;
            if (gridViewColumn.IsImageColumnType) gc.OptionsColumn.FixedWidth = true;   //důležité aby se obrázek vykreslil podle šířky sloupce
            gc.OptionsColumn.AllowSize = gridViewColumn.AllowSize;
            //změna pořadí sloupců
            gc.OptionsColumn.AllowMove = gridViewColumn.AllowMove;

            //Merge stejných hodnot (distinct)
            gc.OptionsColumn.AllowMerge = DxComponent.ConvertBool(gridViewColumn.AllowMerge);

            gc.OptionsColumn.ShowInCustomizationForm = !gridViewColumn.IsSystemColumn;  //systémové sloupce nechceme nabízet ve výběru sloupců

            //ikonka
            gc.ImageOptions.Alignment = gc.OptionsColumn.ShowCaption == false ? StringAlignment.Center : gridViewColumn.IconAligment;   //pokud nechci nadpis, tak ikonky doprostřed.
            DxComponent.ApplyImage(gc.ImageOptions, gridViewColumn.IconName, null, ResourceImageSizeType.Small);

            gc.OptionsFilter.ImmediateUpdateAutoFilter = false; //TODO předat z IGridViewColumn a případně zajistit nějaký timer? Prodlevu?
            OptionsFilter.UseNewCustomFilterDialog = false;

            gc.SortMode = ColumnSortMode.Custom;    //Custom sorting ->vyvolá se eventa CustomColumnSort kde mohu "vypnout" řazení

            gc.FilterMode = ColumnFilterMode.DisplayText; //Tohle umožní dát do filtrovacího řádku jakýkoliv text a validovat / formátovat si ho budu sám v OnCustomRowFilter. J

            var repositoryItem = _CreateRepositoryItem(gridViewColumn, false);
            if (repositoryItem != null)
            {
                this.GridControl.RepositoryItems.Add(repositoryItem);
                gc.ColumnEdit = repositoryItem;
            }

            //nastaveni displayFormat
            _SetFormatInfo(gc.DisplayFormat, gridViewColumn);
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
            else if (gridViewColumn.ColumnType == typeof(System.Decimal))
            {
                formatInfo.FormatType = DevExpress.Utils.FormatType.Numeric;
                formatInfo.FormatString = gridViewColumn.FormatString; //"n"; //https://documentation.devexpress.com/#WindowsForms/CustomDocument2141
            }
        }

        #region RepositoryItem, eitační styl, image, multiline...
        private RepositoryItem _CreateRepositoryItem(IGridViewColumn gridViewColumn, bool useForRowFilter = false)
        {
            if (gridViewColumn.IsEditStyleColumnType)   //Editační styl
                return _CreateRepositoryItemForEditStyle(gridViewColumn, useForRowFilter);
            else if (gridViewColumn.IsImageColumnType) //Obrázky
                return _CreateRepositoryItemForImage(useForRowFilter);
            else if (gridViewColumn.IsStringColumnType) //string
                return _CreateRepositoryItemForString(gridViewColumn, useForRowFilter);
            else
                return null;
        }

        private static RepositoryItem _CreateRepositoryItemForString(IGridViewColumn gridViewColumn, bool useForRowFilter)
        {
            if (!gridViewColumn.MultiLineCell || useForRowFilter) return null;
            RepositoryItemMemoEdit repItemMemoEdit = new RepositoryItemMemoEdit
            {
                LinesCount = 0 //0 = automatický počet
            };
            return repItemMemoEdit;
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
            //if (gridViewColumn.CodeTable.Count(x => !String.IsNullOrEmpty(x.ImageName)) > 0)
            if (gridViewColumn.EditStyleViewMode == EditStyleViewMode.Icon || gridViewColumn.EditStyleViewMode == EditStyleViewMode.IconText)
            {
                RepositoryItemImageComboBox repItemImageCombo = new RepositoryItemImageComboBox();          // vytvoříme objekt typu RepositoryItemImageComboBox
                ResourceContentType resourceType = ResourceContentType.None;
                repItemImageCombo.AutoHeight = false;

                //resourceType určíme podle prvního item s image
                var firstImageCodeTableItem = gridViewColumn.CodeTable.FirstOrDefault(x => !string.IsNullOrEmpty(x.ImageName));
                if (firstImageCodeTableItem.ImageName != null)
                {
                    if (DxComponent.TryGetApplicationResource(firstImageCodeTableItem.ImageName, out var resourceItem))
                    {
                        resourceType = resourceItem.ContentType;
                    }
                }
                repItemImageCombo.SmallImages = _GetImageList(resourceType, ResourceImageSizeType.Small);
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
                    var a = new DevExpress.XtraEditors.Controls.ComboBoxItem(code.DisplayText);

                    repItemCombo.Items.Add(a);
                    i++;
                }
                return repItemCombo;
            }
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

        internal void SetFocusToFilterRow(string lastFilterRowCell)
        {

            //TADY to budu potřebovat volat i z controleroru, proto internal

            if (!_RowFilterVisible) return;

            this.FocusedRowHandle = DevExpress.XtraGrid.GridControl.AutoFilterRowHandle;
            bool focusFirstColumn = true;
            if (!string.IsNullOrEmpty(lastFilterRowCell))
            {
                var column = this.Columns.Where(x => x.FieldName == lastFilterRowCell).FirstOrDefault();
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

            _LastFilterRowCell = FocusedColumn?.FieldName;

            this.ShowEditor();  //activuje cell pro editaci (curzor)

        }

        #region Handlers 


        #endregion

        #region Public eventy a jejich volání

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
            if (selectedRows.Count() != this.RowCount) SelectedAllRows = false; //schození přiznaku o vybraných všech řádcích
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
        /// Vyvolá metodu <see cref="OnShowContextMenu(DxGridContextMenuEventArgs)"/> a event <see cref="DxShowContextMenu"/>
        /// </summary>
        /// <param name="hitInfo"></param>
        private void RaiseShowContextMenu(DxGridHitInfo hitInfo)
        {
            //if (_SilentMode) return;

            DxGridContextMenuEventArgs args = new DxGridContextMenuEventArgs(hitInfo);
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
        /// <param name="fixedWidth"></param>
        /// <param name="allowMove"></param>
        public GridViewColumnData(string fieldName, string caption, Type columnType, int visibleIndex,
            bool visible = true, bool allowSort = true, bool allowFilter = true, int width = 30, bool fixedWidth = false, bool allowMove = true)
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
            AllowSize = fixedWidth;
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
        /// <summary>
        /// Sloupec s editačním stylem
        /// </summary>
        public bool IsEditStyleColumnType => this.CodeTable?.Count > 0;
        /// <inheritdoc/>
        public bool IsNumberColumnType => TypeHelper.IsNumeric(this.ColumnType);
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
        /// <param name="id"></param>
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

        public static Tuple<int, string> CreateId(int viewRowIndex, string fieldName)
        {
            return new Tuple<int, string>(viewRowIndex, fieldName);
        }
    }

    /// <summary>
    /// Interface pro dodatečnou deficici vlastností buňky, převázně se jedná o grafické vlastnosti (styl, image)
    /// </summary>
    public interface IGridViewCell
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

    /// <summary>
    /// Argumenty pro zobrazení konetextového menu
    /// </summary>
    public class DxGridContextMenuEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridContextMenuEventArgs(DxGridHitInfo dxGridHitInfo)
        {
            this.DxGridHitInfo = dxGridHitInfo;
        }
        public DxGridHitInfo DxGridHitInfo { get; private set; }

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

        /// <summary>
        /// Return true for numeric Type
        /// </summary>
        /// <param name="myType"></param>
        /// <returns></returns>
        internal static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }
    }
    #endregion
}
