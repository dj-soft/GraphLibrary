using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Data.Filtering;
using DevExpress.Utils;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Registrator;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;

namespace Noris.Clients.Win.Components.AsolDX
{

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

    public class DxGridControl : GridControl
    {
        public DxGridControl()
        {
            RegisterEventsHandlers();
        }

        /// <summary>
        /// příznak při změně DataSource
        /// </summary>
        public bool DataSourceChanging { get; set; }

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

    }

    public class DxGridViewInfoRegistrator : GridInfoRegistrator
    {
        public override string ViewName { get { return "DxGridView"; } }

        public override BaseView CreateView(GridControl grid) { return new DxGridView(grid as GridControl); }
    }

    public class DxGridView : DevExpress.XtraGrid.Views.Grid.GridView
    {

        /// <summary>
        /// Povolení interního mechanismu řádkového filtru v GridView
        /// </summary>
        public bool RowFilterCoreEnabled { get; set; }
        public int LastActiveRow { get; set; }
        public int ActualActiveRow { get; set; }

        /// <summary>CZ: Je aktivni nejaka bunka v radkovem filtru</summary>
        public bool IsFilterRowCellActive { get { return this.IsFilterRow(this.ActualActiveRow); } }

        private bool _checkBoxRowSelect;
        public bool CheckBoxRowSelect { get => _checkBoxRowSelect; set { _checkBoxRowSelect = value; SetGridMultiSelectMode(); } }

        private string _LastFilterRowCell { get; set; } = String.Empty;

        private bool _RowFilterVisible { get { return this.OptionsView.ShowAutoFilterRow; } /*set;*/ }

        /// <summary>
        /// Slouží pro uložení informací o sloupcích, kterými byla provedena inicializace View
        /// </summary>
        private IEnumerable<IGridViewColumn> _GridViewColumns { get; set; }


        public DxGridView() : this(null) { }

        public DxGridView(DevExpress.XtraGrid.GridControl grid) : base(grid)
        {
            InitProperties();
            RegisterEventsHandlers();
        }

        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void InitProperties()
        {
            OptionsSelection.MultiSelect = true;                                  // chceme multi select rows
            SetGridMultiSelectMode();
            //if (CheckBoxRowSelect)
            //{
            //    OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;     // mód označování v režimu multiselect=true (RowSelect,CellSelect,CheckBoxRowSelect)
            //}
            //else
            //{
            //    OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.RowSelect;     // mód označování v režimu multiselect=true (RowSelect,CellSelect,CheckBoxRowSelect)
            //}


            OptionsSelection.EnableAppearanceFocusedCell = false;   //zakáz selectu cell, ale pouze barva, ohraničení zůstává

            OptionsBehavior.Editable = false;   //readonly, necheceme editaci
            FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;

            //this.OptionsView.HeaderFilterButtonShowMode = DevExpress.XtraEditors.Controls.FilterButtonShowMode.SmartTag;

            OptionsMenu.ShowAutoFilterRowItem = false;  //nechceme povolit uživateli přepínat zda bude/nebude vidět filtrovací řádek

            OptionsView.EnableAppearanceEvenRow = true;



            RowFilterCoreEnabled = true;

        }

        private void SetGridMultiSelectMode()
        {
            if (CheckBoxRowSelect)
            {
                OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;     // mód označování v režimu multiselect=true (RowSelect,CellSelect,CheckBoxRowSelect)
            }
            else
            {
                OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.RowSelect;     // mód označování v režimu multiselect=true (RowSelect,CellSelect,CheckBoxRowSelect)
            }
        }

        private void RegisterEventsHandlers()
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
        }

        private void _OnCustomRowCellEdit(object sender, CustomRowCellEditEventArgs e)
        {
            //tohle dá stejný Edit do řádkového filtru jako je v cell
            //if (e.RowHandle == DevExpress.XtraGrid.GridControl.AutoFilterRowHandle)
            //    e.RepositoryItem = e.Column.ColumnEdit;

            if (e.RowHandle == DevExpress.XtraGrid.GridControl.AutoFilterRowHandle)
            {
                //Tady se snažím vytvořit nový pro řádkový filtr, protože pokud byl ReposotoryItem typu RepositoryItemImageComboBox a byla zobrazena jen ikonka bez textu, tak se ve filtru nezobrazoval text s ikonkou...
                var gvColumn = _GridViewColumns.FirstOrDefault(x => x.FieldName == e.Column.FieldName);
                if (gvColumn != null)
                {
                    var repositoryItem = CreateRepositoryItem(gvColumn, true);
                    if (repositoryItem != null)
                    {
                        this.GridControl.RepositoryItems.Add(repositoryItem);
                        e.RepositoryItem = repositoryItem;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnRegisterEventsHandlers();
            }
            base.Dispose(disposing);
        }

        private void UnRegisterEventsHandlers()
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
            //zajistí, že se nebude měnit řazení interním mechanismem
            e.Result = e.ListSourceRowIndex2 - e.ListSourceRowIndex1;
            e.Result = (e.Result > 0) ? 1 : ((e.Result < 0) ? -1 : 0);
            if (e.SortOrder == DevExpress.Data.ColumnSortOrder.Ascending)
                e.Result = -e.Result;
            e.Handled = true;
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
            GridView view = sender as GridView;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo info = view.CalcHitInfo(ea.Location);
            //DxGridHitInfo info = (DxGridHitInfo)view.CalcHitInfo(ea.Location);

            if (info.InRow || info.InRowCell)
            {
                string colCaption = info.Column == null ? "N/A" : info.Column.GetCaption();
                //MessageBox.Show(string.Format("DoubleClick on row: {0}, column: {1}.", info.RowHandle, colCaption));

                if (!CheckBoxRowSelect && !view.IsRowSelected(info.RowHandle))  //v případě chceckBoxu nedělat. Pokud bude použit check box, tak budeme využívat aktivní řádek místo selectovaného.
                {
                    //nastvává v kombinaci ctrl+doubleClick
                    //Chci mít vždy selectovaný řádek nad kterým provádím doubleClick -> prvně se vyvolá _OnSelectionChanged -> server má nastaveny selectovavaný řádek
                    view.SelectRow(view.FocusedRowHandle);
                }
                OnDoubleClick(info.Column?.FieldName ?? null, info.RowHandle);  //Zavoláme public event
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
            OnSelectionChanged(this.GetSelectedRows());
        }

        protected override void OnActiveFilterChanged(object sender, EventArgs e)
        {
            base.OnActiveFilterChanged(sender, e);
        }

        protected override string ViewName { get { return "DxGridView"; } }


        #region Filtering
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
                if (strVal.StartsWith("%"))
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
        /// <summary>
        /// Vytvoření koleckce sloupců v gridView
        /// </summary>
        /// <param name="gridViewColumns"></param>
        public void InitColumns(IEnumerable<IGridViewColumn> gridViewColumns)
        {
            _GridViewColumns = gridViewColumns;
            this.Columns.Clear();
            foreach (var vc in _GridViewColumns)
            {
                int index = this.Columns.Add(CreateGridColumn(vc));
                InitColumnAfterAdd(index, vc);
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
                FieldName = gridViewColumn.FieldName, Caption = gridViewColumn.Caption, UnboundDataType = gridViewColumn.ColumnType, VisibleIndex = gridViewColumn.VisibleIndex,
                Visible = gridViewColumn.Visible, ToolTip = gridViewColumn.ToolTip
            };
            gc.OptionsColumn.AllowSort = DxComponent.Convert(gridViewColumn.AllowSort);
            //povolení filtrování
            gc.OptionsFilter.AllowFilter = gridViewColumn.AllowFilter;
            gc.OptionsFilter.AllowAutoFilter = gridViewColumn.AllowFilter;

            //šířka sloupce
            if (gridViewColumn.Width > 0) gc.Width = gridViewColumn.Width;

            //ikonka
            gc.ImageOptions.Alignment = gridViewColumn.IconAligment;
            DxComponent.ApplyImage(gc.ImageOptions, gridViewColumn.IconName, null, ResourceImageSizeType.Small);

            gc.OptionsFilter.ImmediateUpdateAutoFilter = false; //TODO předat z IGridViewColumn a případně zajistit nějaký timer? Prodlevu?
            OptionsFilter.UseNewCustomFilterDialog = false;

            gc.SortMode = ColumnSortMode.Custom;    //Custom sorting ->vyvolá se eventa CustomColumnSort kde mohu "vypnout" řazení

            gc.FilterMode = ColumnFilterMode.DisplayText; //Tohle umožní dát do filtrovacího řádku jakýkoliv text a validovat / formátovat si ho budu sám v OnCustomRowFilter. J

            var repositoryItem = CreateRepositoryItem(gridViewColumn, false);
            if (repositoryItem != null)
            {
                this.GridControl.RepositoryItems.Add(repositoryItem);
                gc.ColumnEdit = repositoryItem;
            }

            //format; takto to je i v pivotGrid.
            if (gridViewColumn.ColumnType == typeof(System.String))
            {
                gc.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
                gc.DisplayFormat.FormatString = "{0}"; // string nejde sčítat
            }
            else if (gridViewColumn.ColumnType == typeof(System.DateTime))
            {
                gc.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                gc.DisplayFormat.FormatString = gridViewColumn.FormatString; //"MMM/d/yyyy hh:mm tt";
            }
            else if (gridViewColumn.ColumnType == typeof(System.Decimal))
            {
                gc.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gc.DisplayFormat.FormatString = gridViewColumn.FormatString; //"n"; //https://documentation.devexpress.com/#WindowsForms/CustomDocument2141
            }

            return gc;
        }

        private RepositoryItem CreateRepositoryItem(IGridViewColumn gridViewColumn, bool useForRowFilter = false)
        {
            if (gridViewColumn.CodeTable?.Count > 0)
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
            else
            {
                return null;
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




        /// <summary>
        /// Některé vlastnosti GridColumn nejdou nastavit v rámci <see cref="CreateGridColumn(IGridViewColumn)"/>. Je třeba je nastavit až po přidání do kolekce.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="gridViewColumn"></param>
        private void InitColumnAfterAdd(int columnIndex, IGridViewColumn gridViewColumn)
        {
            var gc = this.Columns[columnIndex];
            //Header settings
            gc.AppearanceHeader.TextOptions.HAlignment = gridViewColumn.HeaderAlignment;
            gc.AppearanceHeader.FontStyleDelta = gridViewColumn.HeaderFontStyle;
            gc.AppearanceCell.TextOptions.HAlignment = gridViewColumn.CellAlignment;
            gc.AppearanceCell.FontStyleDelta = gridViewColumn.CellFontStyle;
        }
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
            this.OnFilterRowChanged();  //Vyvoláme naši Event o změně filtru
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


        #endregion

        #region Public eventy a jejich volání

        /// <summary>
        /// Filtrovací řádek se změnil <see cref="FilterRowChanged"/>
        /// </summary>
        public event DxGridColumnFilterChangeHandler FilterRowChanged;
        /// <summary>
        /// Vyvolá event <see cref="FilterRowChanged"/>
        /// </summary>
        protected virtual void OnFilterRowChanged()
        {
            if (FilterRowChanged != null) FilterRowChanged(this, new DxGridFilterRowChangeEventArgs(this.ActiveFilterString));
        }


        /// <summary>
        /// Kliklo se do gridu <see cref="DxDoubleClick"/>
        /// </summary>
        public event DxGridDoubleClickHandler DxDoubleClick;
        protected virtual void OnDoubleClick(string columnName, int rowId)
        {
            if (DxDoubleClick != null) DxDoubleClick(this, new DxGridDoubleClickEventArgs(columnName, rowId));
        }

        public event DxGridFocusedRowChangedHandler DxFocusedRowChanged;
        protected virtual void OnFocusedRowChanged(int prevFocusedRowHandle, int focusedRowHandle)
        {
            if (DxFocusedRowChanged != null) DxFocusedRowChanged(this, new DxGridFocusedRowChangedEventArgs(prevFocusedRowHandle, focusedRowHandle));
        }

        public event DxGridSelectionChangedHandler DxSelectionChanged;
        protected virtual void OnSelectionChanged(int[] selectedRows)
        {
            if (DxSelectionChanged != null) DxSelectionChanged(this, new DxGridSelectionChangedChangedEventArgs(selectedRows));
        }

        public event DxGridKeyDownHandler DxKeyDown;
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (DxKeyDown != null) DxKeyDown(this, e);
        }


        #endregion
    }

    public class GridViewColumnData : IGridViewColumn
    {
        public GridViewColumnData()
        {
        }
        public GridViewColumnData(string fieldName, string caption, Type columnType, int visibleIndex,
            bool visible = true, bool allowSort = true, bool allowFilter = true)
        {
            FieldName = fieldName;
            Caption = caption;
            ColumnType = columnType;
            VisibleIndex = visibleIndex;
            Visible = visible;
            AllowSort = allowSort;
            AllowFilter = allowFilter;
            CodeTable = new List<(string DisplayText, string Value, string ImageName)>();
            //todo přidat ostaní vlastnosti až jich bude více aby to to bylo slušně srovnané... zatím se nastavují mimo ctor.
        }

        public virtual string FieldName { get; private set; }
        public virtual string Caption { get; private set; }
        public virtual Type ColumnType { get; private set; }
        public virtual bool Visible { get; private set; }
        public virtual int VisibleIndex { get; private set; }
        public virtual string ToolTip { get; set; }
        public virtual bool AllowSort { get; set; }
        public virtual bool AllowFilter { get; set; }
        public virtual string IconName { get; set; }
        public virtual System.Drawing.StringAlignment IconAligment { get; set; }
        public virtual HorzAlignment HeaderAlignment { get; set; }
        public virtual System.Drawing.FontStyle HeaderFontStyle { get; set; }
        public virtual HorzAlignment CellAlignment { get; set; }
        public virtual System.Drawing.FontStyle CellFontStyle { get; set; }
        public virtual int Width { get; set; }

        public virtual bool IsSystemColumn { get; set; }

        public List<(string DisplayText, string Value, string ImageName)> CodeTable { get; set; }
        public EditStyleViewMode EditStyleViewMode { get; set; }
        public string FormatString { get; set; }

    }
    public interface IGridViewColumn
    {
        string FieldName { get; }
        string Caption { get; }
        Type ColumnType { get; }
        bool Visible { get; }
        int VisibleIndex { get; }
        string ToolTip { get; }

        /// <summary>
        /// Povoluje třídění sloupce
        /// </summary>
        bool AllowSort { get; }
        /// <summary>
        /// Povoluje řádkový filtr a tlačítko zobrazující možnosti filter boxu
        /// </summary>
        bool AllowFilter { get; }
        /// <summary>
        /// název ikonky pro hlavičku
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
        /// systemový sloupec, nezobrazuje se v výběru sloupců
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

    }

    //public class DxGridHitInfo : DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo
    //{
    //    public int MyProperty { get; set; } = 99;
    //}

    #region Deklarace delegátů a tříd pro eventhandlery, další enumy

    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridColumnFilterChangeHandler(object sender, DxGridFilterRowChangeEventArgs args);

    /// <summary>
    /// Argument pro eventhandlery
    /// </summary>
    public class DxGridFilterRowChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGridFilterRowChangeEventArgs(string filterString)
        {
            this.FilterString = filterString;
        }

        /// <summary>
        /// Filter v podobě Criterial Language
        /// </summary>
        public string FilterString { get; private set; }

    }


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
        public DxGridDoubleClickEventArgs(string columnName, int rowId)
        {
            ColumnName = columnName;
            RowId = rowId;
        }

        /// <summary>
        /// Columnname (FieldName)
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// Id řádku
        /// </summary>
        public int RowId { get; set; }
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
        public DxGridSelectionChangedChangedEventArgs(int[] selectedRows)
        {
            this.SelectedRows = selectedRows;
        }

        /// <summary>
        /// Aktuálně vybrané řádky
        /// </summary>
        public int[] SelectedRows { get; set; }

    }


    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxGridKeyDownHandler(object sender, KeyEventArgs args);

    public enum EditStyleViewMode
    {
        //
        // Summary:
        //     Edit style view as only text. This is Default value. Compatible with old HELIOS
        //     Green versions.
        Text = 0,
        //
        // Summary:
        //     Edit style view as icon.
        Icon = 1,
        //
        // Summary:
        //     Edit style view as icon and text.
        IconText = 2
    }

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
