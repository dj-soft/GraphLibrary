using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;

namespace Djs.Common.ComponentsOld.Grid
{
    #region GridPositions, GridPositionSet, GridPositionItem : position of all columns, tables, rows in GGrid
    /// <summary>
    /// GridPositions : position of all columns and tables in GGrid
    /// </summary>
    public class GridPositions : IDataPersistent
    {
        #region Constructor + PersistentValue + Owner
        public GridPositions()
        {
            this._Columns = new GridPositionSet<GridPositionColumnItem>(125, true);
            this._Tables = new GridPositionSet<GridPositionItem>(350, false);
        }
        public override string ToString()
        {
            return "Columns: " + this._Columns.ToString() + "; " +
                   "Tables: " + this._Tables.ToString() + ".";
        }
        /// <summary>
        /// Clear all info for Columns and Tables.
        /// Leave only info for RowHeaderColumn.
        /// </summary>
        public void Clear()
        {
            this.ClearColumns();
            this.ClearTables();
        }
        /// <summary>
        /// Clear all info for Columns.
        /// Leave only info for RowHeaderColumn.
        /// </summary>
        public void ClearColumns()
        {
            this._Columns.Clear();
        }
        /// <summary>
        /// Clear all info for Tables.
        /// </summary>
        public void ClearTables()
        {
            this._Tables.Clear();
        }
        public string PersistentValue
        {
            get
            {
                DataPersistValue dpv = new DataPersistValue("GridPositions");

                // Columns:
                DataPersistValue col = new DataPersistValue("Columns");
                col.Add(this._Columns.PersistentValue);
                dpv.Add(col);

                // Tables:
                DataPersistValue tbl = new DataPersistValue("Tables");
                tbl.Add(this._Tables.PersistentValue);
                dpv.Add(tbl);

                return dpv.PersistentValue;
            }
            set
            {
                this.Clear();
                DataPersistValue dpv = DataPersistValue.FromPersist(value);
                if (dpv.IsValid("GridPositions", 2))
                {
                    DataPersistValue col = dpv.GetDataPersist(0);
                    if (col.IsValid("Columns", 1))
                        this._Columns.PersistentValue = col.GetString(0);

                    DataPersistValue tbl = dpv.GetDataPersist(1);
                    if (tbl.IsValid("Tables", 1))
                        this._Tables.PersistentValue = tbl.GetString(0);
                }
            }
        }
        #endregion
        #region Columns
        /// <summary>
        /// Columns set
        /// </summary>
        protected GridPositionSet<GridPositionColumnItem> _Columns;
        /// <summary>
        /// Number of data tables
        /// </summary>
        public int ColumnsCount { get { return this._Columns.Count; } }
        /// <summary>
        /// Return data for column with specified ColumnId. For index -1 returns RowHeaderColumn. For other negative indexes return null.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        public GridPositionColumnItem GetColumn(int columnId) { return this._Columns.GetItem(columnId); }
        /// <summary>
        /// Return data for column with specified ColumnId. For index -1 returns RowHeaderColumn. For other negative indexes return null.
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="isNewItem"></param>
        /// <returns></returns>
        public GridPositionColumnItem GetColumn(int columnId, out bool isNewItem) { return this._Columns.GetItem(columnId, out isNewItem); }
        /// <summary>
        /// Remove Column for specified ColumnID
        /// </summary>
        /// <param name="columnId"></param>
        public void RemoveColumn(int columnId) { this._Columns.RemoveItem(columnId); }
        /// <summary>
        /// Visual Offset for data column in pixel.
        /// When ColumnsVisualOffset = 0, then Grid show first pixel of first data column immediatelly after RowHeaderColumn Right coordinate.
        /// When ColumnsVisualOffset is (for example) 100, then first 100 pixels of data columns is scrolled to left from data area (=is hidden under RowHeaderColumn).
        /// When Column[0] has Width = 80 and Column[1] has Width 50, then Column[0] (80 pixel) is whole invisible (is scrolled out to left), and Column[1] is partially visible, from its pixel 20 to 50.
        /// ColumnsVisualOffset can not be a negative value.
        /// </summary>
        public int ColumnsVisualOffset { get { return this._Columns.VisualOffset; } set { this._Columns.VisualOffset = value; } }
        /// <summary>
        /// Positions for RowHeader Column
        /// </summary>
        internal GridPositionColumnItem RowHeaderColumn { get { return this._Columns.ItemHeader; } }
        /// <summary>
        /// Width of RowHeader Column
        /// </summary>
        public int RowHeaderWidth { get { return this._Columns.ItemHeaderSize; } set { this._Columns.ItemHeaderSize = value; } }
        /// <summary>
        /// Sum() of the Columns.SizeVisible (Width of all visible DataColumns, except RowHeader Column)
        /// </summary>
        public int ColumnsTotalSizeVisible { get { return this._Columns.TotalSizeVisible; } }
        /// <summary>
        /// Last visible pixel on columns, in ColumnSet coordinates
        /// </summary>
        public int ColumnsMaxVisiblePixel { get { return this._Columns.MaxVisiblePixel; } }
        /// <summary>
        /// ColumnsIdentityKey : sum of ColumnsVisualOffset and {ID and positions} from all Columns (include RowHeaderColumn). 
        /// Is recalculated after each change (in method CheckValid() after Invalidate()).
        /// User class can store this ID as "hash" after recalculate its private values, and check this "hash" in its equivalent "CheckValid()" proecss.
        /// </summary>
        public string ColumnsIdentityKey { get { return this._Columns.IdentityKey; } }

        /// <summary>
        /// Reload data from Data columns from Data table into this visual columns.
        /// Load data from columns only for new inserted colums (as for (force) parameter = false)
        /// </summary>
        /// <param name="dataTable"></param>
        internal void ReloadColumns(DTable dataTable)
        {
            this.ReloadColumns(dataTable, false);
        }
        /// <summary>
        /// Reload data from Data columns from Data table into this visual columns.
        /// When (force) is false, then reload only new inserted columns, when true then reload even current columns.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="force">true = overwrite values into existing columns, false = preserve values in existing columns, write valuse only to newly inserted columns.</param>
        internal void ReloadColumns(DTable dataTable, bool force)
        {
            this.ReloadColumns(dataTable.Columns, force);
        }
        /// <summary>
        /// Reload data from Data columns from Data table into this visual columns.
        /// When (force) is false, then reload only new inserted columns, when true then reload even current columns.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="force">true = overwrite values into existing columns, false = preserve values in existing columns, write valuse only to newly inserted columns.</param>
        internal void ReloadColumns(IEnumerable<DColumn> columns, bool force)
        {
            if (columns == null) return;
            int index = 0;
            foreach (DColumn dataColumn in columns)
                this.ReloadColumn(index++, dataColumn, force);
        }
        /// <summary>
        /// Reload data from DColumn into this position
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dataColumn"></param>
        /// <param name="force"></param>
        internal void ReloadColumn(int index, DColumn dataColumn, bool force)
        {
            bool isNewItem;
            GridPositionItem pColumn = this.GetColumn(index, out isNewItem);
            if (force || isNewItem)
            {
                if (dataColumn != null)
                {
                    pColumn.SizeRange = dataColumn.WidthRange;
                    pColumn.SizeN = dataColumn.Width;
                }
                pColumn.IsVisible = true;
            }
        }
        #endregion
        #region Tables
        /// <summary>
        /// Tables set
        /// </summary>
        protected GridPositionSet<GridPositionItem> _Tables;
        /// <summary>
        /// Number of data tables
        /// </summary>
        public int TableCount { get { return this._Tables.Count; } }
        /// <summary>
        /// Visual Offset for tables in pixel.
        /// When TableVisualOffset = 0, then Grid show first pixel of first data table immediatelly on to of grid.
        /// When TableVisualOffset is (for example) 500, then first 500 pixels of data columns is scrolled up out from visible area.
        /// When Table[0] has Height = 400 and Table[1] has Height 250, then Table[0] (400 pixel) is whole invisible (is scrolled out to top), and Table[1] is partially visible, from its pixel 100 to 250.
        /// TableVisualOffset can not be a negative value.
        /// </summary>
        public int TableVisualOffset { get { return this._Tables.VisualOffset; } set { this._Tables.VisualOffset = value; } }
        /// <summary>
        /// Sum() of the Tables.SizeVisible (Height of all visible Tables)
        /// </summary>
        public int TablesTotalSizeVisible { get { return this._Tables.TotalSizeVisible; } }
        /// <summary>
        /// Last visible pixel on tables, in Grid coordinates
        /// </summary>
        public int TablesMaxVisiblePixel { get { return this._Tables.MaxVisiblePixel; } }
        /// <summary>
        /// TablesIdentityKey : sum of TableVisualOffset and {ID and positions} from all Tables.
        /// Is recalculated after each change (in method CheckValid() after Invalidate()).
        /// User class can store this ID as "hash" after recalculate its private values, and check this "hash" in its equivalent "CheckValid()" proecss.
        /// </summary>
        public string TablesIdentityKey { get { return this._Tables.IdentityKey; } }

        /// <summary>
        /// Return data for table with specified TableID. For negative indexes return null.
        /// </summary>
        /// <param name="tableId"></param>
        /// <returns></returns>
        public GridPositionItem GetTable(int tableId) { return this._Tables.GetItem(tableId); }
        /// <summary>
        /// Return data for table with specified TableID. For negative indexes return null.
        /// </summary>
        /// <param name="tableId"></param>
        /// <returns></returns>
        public GridPositionItem GetTable(int tableId, out bool isNewItem) { return this._Tables.GetItem(tableId, out isNewItem); }
        /// <summary>
        /// Remove Table for specified TableID
        /// </summary>
        /// <param name="tableId"></param>
        public void RemoveTable(int tableId) { this._Tables.RemoveItem(tableId); }
        #endregion
    }
    /// <summary>
    /// Set of GridPositionItem
    /// </summary>
    public class GridPositionSet<T> : IDataPersistent, IGridPositionSet
        where T : IGridPositionItem, IDataPersistent, new()
    {
        #region Constructor, ToString, IDataPersistent
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="withHeader"></param>
        public GridPositionSet(int sizeDefault, bool withHeader)
        {
            this.WithHeader = withHeader;
            this._ItemDict = new Dictionary<int, T>();
            this._ItemHeader = new T() { Owner = this, ItemId = -1, Order = 0 };
        }
        public string PersistentValue
        {
            get
            {
                DataPersistValue dpv = new DataPersistValue("PositionSet");

                if (this.WithHeader)
                    dpv.Add(this.ItemHeader.PersistentValue);

                foreach (T column in this._ItemDict.Values)
                    dpv.Add(column);

                return dpv.PersistentValue;
            }
            set
            {
                this.Clear();
                this.WithHeader = false;
                DataPersistValue dpv = DataPersistValue.FromPersist(value);
                if (dpv.IsValid("PositionSet", 0))
                {
                    for (int i = 0; i < dpv.ValueCount; i++)
                    {
                        DataPersistValue ipv = dpv.GetDataPersist(i);
                        T item = new T() { Owner = this, DataPersistValue = ipv };
                        int id = item.ItemId;
                        if (id == -1)
                        {
                            this._ItemHeader = item;
                            this.WithHeader = true;
                        }
                        else if (id >= 0)
                        {
                            if (this._ItemDict.ContainsKey(id))
                                this._ItemDict[id] = item;
                            else
                                this._ItemDict.Add(id, item);
                        }
                        this.Invalidate();
                    }
                }
            }
        }
        public override string ToString()
        {
            return "Count: " + this.Count.ToString() + "; " +
                   "Valid: " + (this.IsValid ? "Yes" : "No") + "; " +
                   "Header: " + (this.WithHeader ? "Yes" : "No") + "; " +
                   "TotalSize: " + this._TotalSizeVisible.ToString();
        }
        protected Dictionary<int, T> _ItemDict;
        protected bool WithHeader;
        protected int NextOrder;
        #endregion
        #region Get, Clear, Count, Remove
        /// <summary>
        /// Count of all Items, except Header
        /// </summary>
        public int Count { get { return this._ItemDict.Count; } }
        /// <summary>
        /// Get/Add an item with ItemID.
        /// For negative ID returns null, only for ItemID == -1 and WithHeader == true returns ItemHeader.
        /// For new ID create a new item.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public T GetItem(int itemId)
        {
            bool isNew;
            return this.GetItem(itemId, out isNew);
        }
        /// <summary>
        /// Get/Add an item with ItemID.
        /// For negative ID returns null, only for ItemID == -1 and WithHeader == true returns ItemHeader.
        /// For new ID create a new item.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        public T GetItem(int itemId, out bool isNew)
        {
            isNew = false;
            if (itemId == -1 && this.WithHeader) return this.ItemHeader;
            if (itemId < 0) return default(T);

            T item;
            if (!this._ItemDict.TryGetValue(itemId, out item))
            {
                item = new T() { Owner = this, ItemId = itemId, Order = this.NextOrder };
                this.NextOrder += 10;
                this._ItemDict.Add(itemId, item);
                isNew = true;
                this.Invalidate();
            }
            return item;
        }
        /// <summary>
        /// Remove an item with ID.
        /// Negative ID does not remove.
        /// </summary>
        /// <param name="itemId"></param>
        public void RemoveItem(int itemId)
        {
            if (itemId < 0) return;

            if (this._ItemDict.ContainsKey(itemId))
            {
                this._ItemDict.Remove(itemId);
                this.Invalidate();
            }
        }
        /// <summary>
        /// Clear all items, except header.
        /// </summary>
        public void Clear()
        {
            this._ItemDict.Clear();
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Default size for this item
        /// </summary>
        public int SizeDefault { get { return this._SizeDefault; } }
        private int _SizeDefault;
        /// <summary>
        /// GridPositionItem for header item
        /// </summary>
        public T ItemHeader { get { return this._ItemHeader; } }
        private T _ItemHeader;
        /// <summary>
        /// Size of header item
        /// </summary>
        public int ItemHeaderSize
        {
            get { return (this.WithHeader ? this._ItemHeader.Size : 0); }
            set { if (this.WithHeader) this._ItemHeader.Size = value /* causing Invalidate() through Item.Size_set() */ ; }
        }
        /// <summary>
        /// Offset for Visual values.
        /// Contain positive value of first visible data pixel.
        /// When contains 0, then first item has visual begin at 0 (or ItemHeaderSize).
        /// </summary>
        public int VisualOffset { get { return this._VisualOffset; } set { this._VisualOffset = (value < 0 ? 0 : value); this.Invalidate(); } }
        private int _VisualOffset;
        /// <summary>
        /// Sum of all Items.SizeVisible, except Header item.
        /// </summary>
        public int TotalSizeVisible { get { this.CheckValid(); return this._TotalSizeVisible; } }
        private int _TotalSizeVisible;
        /// <summary>
        /// Last visible pixel of last column
        /// </summary>
        public int MaxVisiblePixel { get { this.CheckValid(); return this._MaxVisiblePixel; } }
        private int _MaxVisiblePixel;
        /// <summary>
        /// IdentityKey : sum of {ID and positions} from all items. Is recalculated after each change (in method CheckValid() after Invalidate()).
        /// User class can store this ID as "hash" after recalculate its private values, and check this "hash" in its equivalent "CheckValid()" proecss.
        /// </summary>
        public string IdentityKey { get { this.CheckValid(); return this._IdentityKey; } }
        private string _IdentityKey;
        #endregion
        #region IGridPositionSet : Invalidate, CheckValid, Recalculate, IsValid
        /// <summary>
        /// Invalidate all visual values, enforce recalculate before its use.
        /// </summary>
        void IGridPositionSet.Invalidate() { this._IsValid = false; }
        /// <summary>
        /// When is not valid, call recalculate visual values.
        /// </summary>
        void IGridPositionSet.CheckValid()
        {
            if (this._IsValid) return;

            this._IsValid = true;

            StringBuilder identityKey = new StringBuilder();
            identityKey.Append(this._VisualOffset.ToString() + ";");

            List<IGridPositionItem> itemList = this._ItemDict.Values.Select(i => (IGridPositionItem)i).ToList();
            if (itemList.Count > 1)
                itemList.Sort((a, b) => GridPositionItem.CompareByOrder(a, b));
            int beginLogical = 0;
            int beginVisual = 0;
            int order = 0;
            if (this.WithHeader)
                this.ItemHeader.SetPositions(ref order, ref beginLogical, ref beginVisual, identityKey);
            beginLogical = 0;
            beginVisual -= this._VisualOffset;
            foreach (IGridPositionItem item in itemList)
                item.SetPositions(ref order, ref beginLogical, ref beginVisual, identityKey);
            this._TotalSizeVisible = beginLogical;
            this._MaxVisiblePixel = beginVisual - 1;
            this._IdentityKey = identityKey.ToString();
            this.NextOrder = order;
        }
        /// <summary>
        /// Flag for validity of visual values
        /// </summary>
        protected bool IsValid { get { return this._IsValid; } }
        private bool _IsValid;
        protected void Invalidate() { ((IGridPositionSet)this).Invalidate(); }
        protected void CheckValid() { ((IGridPositionSet)this).CheckValid(); }
        #endregion
    }
    public class GridPositionColumnItem : GridPositionItem, IGridPositionItem
    {
        public TimeRange TimeRange { get { return this._TimeRange; } set { this._TimeRange = value; } }
        private TimeRange _TimeRange;
    }
    /// <summary>
    /// One item with GridPosition instance (one column or one table)
    /// </summary>
    public class GridPositionItem : IDataPersistent, IGridPositionItem
    {
        #region Constructors + PersistentValue
        public GridPositionItem()
        {
            this._IsVisible = true;
        }
        public override string ToString()
        {
            return "Id: " + this._ItemId.ToString() + "; " +
                   "BeginLogical: " + this._BeginLogical.ToString() + "; " +
                   "EndLogical: " + this._EndLogical.ToString() + "; " +
                   "BeginVisual: " + this._BeginVisual.ToString() + "; " +
                   "EndVisual: " + this._EndVisual.ToString() + ".";
        }
        public string PersistentValue
        {
            get { return this.PersistentValueCreate().PersistentValue; }
            set { this.PersistentValueFill(DataPersistValue.FromPersist(value)); }
        }
        public DataPersistValue DataPersistValue
        {
            get { return this.PersistentValueCreate(); }
            set { this.PersistentValueFill(value); }
        }
        private DataPersistValue PersistentValueCreate()
        {
            return new DataPersistValue("PositionItem",
                ItemId,
                Order,
                Size,
                Split,
                IsVisible);
        }
        private void PersistentValueFill(DataPersistValue dpv)
        {
            if (dpv.IsValid("PositionItem", 5))
            {
                this._ItemId = dpv.GetInt32(1);
                this._Order = dpv.GetInt32(2);
                this._SizeN = dpv.GetInt32N(3);
                this._SplitN = dpv.GetInt32N(4);
                this._IsVisible = dpv.GetBoolean(5);
                this.Invalidate();
            }
        }
        #endregion
        #region Properties
        /// <summary>
        /// True when this is ColumnHeader item (for Table type: there is no Header item).
        /// </summary>
        public bool IsHeader { get { return this._ItemId == -1; } }
        /// <summary>
        /// Owner = GridPosition instance
        /// </summary>
        public IGridPositionSet Owner { get { return this._Owner; } set { this._Owner = value; this.Invalidate(); } }
        private IGridPositionSet _Owner;
        /// <summary>
        /// ID of item, a constant value
        /// </summary>
        public int ItemId { get { return this._ItemId; } set { this.Invalidate(); this._ItemId = (value < 0 ? 0 : value); } }
        private int _ItemId;
        /// <summary>
        /// True for Visible item, false for Invisible
        /// </summary>
        public bool IsVisible { get { return this._IsVisible; } set { this.Invalidate(); this._IsVisible = value; } }
        private bool _IsVisible;
        /// <summary>
        /// Order of item, can be changed
        /// </summary>
        public int Order { get { this.CheckValid(); return this._Order; } set { this.Invalidate(); this._Order = (value < 0 ? 0 : value); } }
        private int _Order;
        /// <summary>
        /// Size: Width for Column item, or Height for Table item. Contain positive value even for Invisible item (you can use property SizeVisible, which contain 0 for Invisible item).
        /// </summary>
        public int Size { get { this.CheckValid(); return this._SizeValid; } set { this.Invalidate(); this._SizeN = (value < 0 ? 0 : value); } }
        private int _SizeValid;
        /// <summary>
        /// SizeN: Width for Column item, or Height for Table item. Can contain a null value.
        /// </summary>
        public int? SizeN { get { return this._SizeN; } set { this.Invalidate(); this._SizeN = value; } }
        private int? _SizeN;
        /// <summary>
        /// SizeRange : range for Size value.
        /// </summary>
        public Int32NRange SizeRange { get { return this._SizeRange; } set { this.Invalidate(); this._SizeRange = value; } }
        private Int32NRange _SizeRange;
        /// <summary>
        /// SizeVisible: Width for Column item, or Height for Table item. Contain zero for Invisible item.
        /// </summary>
        public int SizeVisible { get { return (this.IsVisible ? this.Size : 0); } }
        /// <summary>
        /// Split : Distance between Begin and splitter, which cut header and data part of this item. Contain positive value even for Invisible item (you can use property SplitVisible, which contain 0 for Invisible item).
        /// </summary>
        public int Split { get { return (this._SplitN.HasValue ? this._SplitN.Value : 40); } set { this.Invalidate(); this._SplitN = (value < 0 ? 0 : value); } }
        private int? _SplitN;
        /// <summary>
        /// SplitVisible: Distance between Begin and splitter, which cut header and data part of this item. Contain zero for Invisible item.
        /// </summary>
        public int SplitVisible { get { return (this.IsVisible ? this.Split : 0); } }
        /// <summary>
        /// SplitRange : range for Split value.
        /// </summary>
        public Int32NRange SplitRange { get { return this._SplitRange; } set { this.Invalidate(); this._SplitRange = value; } }
        private Int32NRange _SplitRange;
        /// <summary>
        /// Begin in logical values. First item has Begin = 0, second item has Begin = Prev.EndLogical, and so on.
        /// </summary>
        public int BeginLogical { get { this.CheckValid(); return this._BeginLogical; } }
        private int _BeginLogical;
        /// <summary>
        /// Splitter position in logical values. First item has SplitLogical = Split, second item has SplitLogical = BeginLogical + Split , and so on.
        /// </summary>
        public int SplitLogical { get { return this.BeginLogical + this.Split; } }
        /// <summary>
        /// End in logical values. First item has End = SizeVisible, second item has End = BeginLogical + SizeVisible, and so on.
        /// </summary>
        public int EndLogical { get { this.CheckValid(); return this._EndLogical; } }
        private int _EndLogical;
        /// <summary>
        /// Begin in visual values. First item has Begin = VisualOffset, second item has Begin = Prev.EndVisual, and so on.
        /// </summary>
        public int BeginVisual { get { this.CheckValid(); return this._BeginVisual; } }
        private int _BeginVisual;
        /// <summary>
        /// Splitter position in visual values. First item has SplitVisual = VisualOffset + Split, second item has SplitVisual = BeginVisual + Split, and so on.
        /// </summary>
        public int SplitVisual { get { return this.BeginVisual + this.Split; } }
        /// <summary>
        /// End in logical values. First item has End = SizeVisible, second item has End = BeginLogical + SizeVisible, and so on.
        /// </summary>
        public int EndVisual { get { this.CheckValid(); return this._EndVisual; } set { this._EndVisualSet(value); } }
        private int _EndVisual;
        private void _SplitSet(int split)
        { }
        /// <summary>
        /// Set this.Size to value, for which will be this.EndVisual == parameter endVisual
        /// </summary>
        /// <param name="endVisual"></param>
        private void _EndVisualSet(int endVisual)
        {
            int beginVisual = this.BeginVisual;
            int size = endVisual - beginVisual;
            if (size < 5) size = 5;
            if (size != this.EndVisual)
                this.Size = size;
        }
        #endregion
        #region Invalidate, CheckValid, SetPositions methods
        /// <summary>
        /// Invalidate Owner.Columns or Owner.Tables, by this.Type
        /// </summary>
        protected void Invalidate()
        {
            if (this._Owner != null)
                this._Owner.Invalidate();
        }
        /// <summary>
        /// Ensure valid for this.Type items
        /// </summary>
        protected void CheckValid()
        {
            if (this._Owner != null)
                this._Owner.CheckValid();
        }
        /// <summary>
        /// Update this Order, Begin and End Logical and Visual by parameters.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="beginLogical"></param>
        /// <param name="beginVisual"></param>
        void IGridPositionItem.SetPositions(ref int order, ref int beginLogical, ref int beginVisual, StringBuilder identityKey)
        {
            this._Order = order;
            order += 10;

            // Validation of Size:
            int? sizeN = this._SizeN;
            if (this._SizeRange != null)
                sizeN = this._SizeRange.Align(sizeN);
            int size = (sizeN.HasValue ? sizeN.Value : SizeDefault);
            this._SizeValid = size;

            // Validation of Split:
            int? splitN = this._SplitN;
            if (this._SplitRange != null)
                this._SplitN = this._SplitRange.Align(splitN);

            // Size visible:
            int sizeVisible = (this._IsVisible ? size : 0);

            // Position Logical:
            this._BeginLogical = beginLogical;
            if (!this.IsHeader)
                beginLogical += sizeVisible;
            this._EndLogical = beginLogical;

            // Position Visual:
            this._BeginVisual = beginVisual;
            beginVisual += sizeVisible;
            this._EndVisual = beginVisual;

            // IdentityKey:
            identityKey.Append(this._ItemId.ToString() + ":" + this._EndVisual.ToString() + ";");
        }
        /// <summary>
        /// Default size for this item
        /// </summary>
        protected int SizeDefault { get { return this.Owner.SizeDefault; } }
        /// <summary>
        /// Compare two items by IGridPositionItem.SortValue1 ASC, IGridPositionItem.SortValue2 ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByOrder(IGridPositionItem a, IGridPositionItem b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            int cmp = a.SortValue1.CompareTo(b.SortValue1);
            if (cmp == 0)
                cmp = a.SortValue2.CompareTo(b.SortValue2);
            return cmp;
        }
        int IGridPositionItem.SortValue1 { get { return this._Order; } }
        int IGridPositionItem.SortValue2 { get { return this._ItemId; } }
        #endregion
    }
    #region Interfaces

    public interface IGridPositionSet
    {
        int SizeDefault { get; }
        void Invalidate();
        void CheckValid();
    }
    public interface IGridPositionItem
    {
        IGridPositionSet Owner { get; set; }
        int ItemId { get; set; }
        int Order { get; set; }
        int Size { get; set; }
        DataPersistValue DataPersistValue { get; set; }
        int SortValue1 { get; }
        int SortValue2 { get; }
        void SetPositions(ref int order, ref int beginLogical, ref int beginVisual, StringBuilder identityKey);
    }
    #endregion
    #endregion
}
