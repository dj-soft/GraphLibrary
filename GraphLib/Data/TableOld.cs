using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Djs.Common.Components;
using System.Drawing;

namespace Djs.Common.Data.Old
{
    // This file contain Data items (Non-Visual) for Graphical Grid. This classes is used in application for data collections and for link data to visual Grid

    #region DTable
    /// <summary>
    /// DTable : container for one table (set of Columns + set of Rows)
    /// </summary>
    public class DTable
    {
        public DTable()
        {
            this._InitTable();
        }
        private void _InitTable()
        {
            this._Columns = new EList<DColumn>();
            this._ColumnsAttachEvents();
            this._Rows = new EList<DRow>();
            this._RowsAttachEvents();
        }

        #region Columns and Rows Add/Remove public methods
        public DColumn AddColumn(string name)
        {
            DColumn column = null;
            if (!String.IsNullOrEmpty(name))
            {
                column = new DColumn(name);
                this._Columns.Add(column);
            }
            return column;
        }
        public DColumn AddColumn(string name, string text)
        {
            DColumn column = null;
            if (!String.IsNullOrEmpty(name))
            {
                column = new DColumn(name, text);
                this._Columns.Add(column);
            }
            return column;
        }
        public void AddColumns(params string[] columnNames)
        {
            if (columnNames != null)
            {
                foreach (string columnName in columnNames)
                {
                    if (!String.IsNullOrEmpty(columnName))
                        this._Columns.Add(new DColumn(columnName));
                }
            }
        }
        public void AddColumn(DColumn column)
        {
            if (column != null)
                this._Columns.Add(column);
        }
        public void AddColumns(params DColumn[] columns)
        {
            if (columns != null)
                this._Columns.AddRange(columns.Where(c => c != null));
        }

        /// <summary>
        /// Returns ColumnId value for specified Column, after this Column is added into this Table.
        /// </summary>
        /// <param name="dColumn"></param>
        /// <returns></returns>
        internal int GetColumnIdFor(DColumn dColumn)
        {
            int index = this._Columns.FindIndex(c => Object.ReferenceEquals(c, dColumn));
            return index;
        }

        public DRow AddRow()
        {
            DRow row = new DRow();
            this._Rows.Add(row);
            return row;
        }
        public DRow AddRow(params object[] items)
        {
            DRow row = null;
            if (items != null)
            {
                row = new DRow(items);
                this._Rows.Add(row);
            }
            return row;
        }
        public void AddRow(DRow row)
        {
            if (row != null)
                this._Rows.Add(row);
        }
        public void AddRows(params DRow[] rows)
        {
            if (rows != null)
                this._Rows.AddRange(rows.Where(r => r != null));
        }
        #endregion
        #region Bridge from private eventhandlers (Columns.ItemAddAfter => private void _Columns_ItemAddAfter(), ...) to public events (ColumnAddAfter, ...)
        private void _ColumnsAttachEvents()
        {
            this._Columns.ItemAddAfter += this._Columns_ItemAddAfter;
            this._Columns.ItemRemoveAfter += this._Columns_ItemRemoveAfter;
        }
        private void _RowsAttachEvents()
        {
            this._Rows.ItemAddAfter += this._Rows_ItemAddAfter;
            this._Rows.ItemRemoveAfter += this._Rows_ItemRemoveAfter;
        }

        void _Columns_ItemAddAfter(object sender, EList<DColumn>.EListAfterEventArgs args)
        {
            this.ColumnAdded(args);
            this.OnColumnAddAfter(args);
            if (this.ColumnAddAfter != null)
                this.ColumnAddAfter(this, args);
        }
        protected virtual void OnColumnAddAfter(EList<DColumn>.EListAfterEventArgs args) { }
        public event EList<DColumn>.EListEventAfterHandler ColumnAddAfter;

        void _Columns_ItemRemoveAfter(object sender, EList<DColumn>.EListAfterEventArgs args)
        {
            this.ColumnRemoved(args);
            this.OnColumnRemoveAfter(args);
            if (this.ColumnRemoveAfter != null)
                this.ColumnRemoveAfter(this, args);
        }
        protected virtual void OnColumnRemoveAfter(EList<DColumn>.EListAfterEventArgs args) { }
        public event EList<DColumn>.EListEventAfterHandler ColumnRemoveAfter;

        void _Rows_ItemAddAfter(object sender, EList<DRow>.EListAfterEventArgs args)
        {
            this.RowAdded(args);
            this.OnRowAddAfter(args);
            if (this.RowAddAfter != null)
                this.RowAddAfter(this, args);
        }
        protected virtual void OnRowAddAfter(EList<DRow>.EListAfterEventArgs args) { }
        public event EList<DRow>.EListEventAfterHandler RowAddAfter;

        void _Rows_ItemRemoveAfter(object sender, EList<DRow>.EListAfterEventArgs args)
        {
            this.RowRemoved(args);
            this.OnRowRemoveAfter(args);
            if (this.RowRemoveAfter != null)
                this.RowRemoveAfter(this, args);
        }
        protected virtual void OnRowRemoveAfter(EList<DRow>.EListAfterEventArgs args) { }
        public event EList<DRow>.EListEventAfterHandler RowRemoveAfter;
        #endregion
        #region Columns and Rows Add/Remove internal actions, not events
        protected void ColumnAdded(EList<DColumn>.EListAfterEventArgs args)
        {
            args.Item.AttachToTable(this);         // Visual Column link to Table
            foreach (DRow row in this._Rows)
                row.ItemsPrepare();              // Items in Row must have equal number of Columns as Table
        }
        protected void ColumnRemoved(EList<DColumn>.EListAfterEventArgs args)
        {
            args.Item.DetachFromTable();
        }
        protected void RowAdded(EList<DRow>.EListAfterEventArgs args)
        {
            args.Item.AtachToTable(this);
        }
        protected void RowRemoved(EList<DRow>.EListAfterEventArgs args)
        {
            args.Item.DetachFromTable();
        }
        #endregion
        #region Sorting rows
        public bool SortRows(List<DTableSortRowsItem> rowList, DColumn sortByColumn, DTableSortRowType sortType)
        {
            if (sortByColumn == null) return false;
            if (!sortByColumn.SortingEnabled) return false;

            int index = sortByColumn.ColumnId;
            bool hasComparator = sortByColumn.HasValueComparator;
            if (hasComparator)
                rowList.ForEachItem(r => { r.Value = r.Row[index]; });
            else
                rowList.ForEachItem(r => { r.ValueComparable = r.Row[index] as IComparable; });

            switch (sortType)
            {
                case DTableSortRowType.Ascending:
                    if (hasComparator)
                        rowList.Sort((a, b) => sortByColumn.ValueComparator(a.Value, b.Value));
                    else
                        rowList.Sort((a, b) => SortRowsCompare(a.ValueComparable, b.ValueComparable));
                    return true;

                case DTableSortRowType.Descending:
                    if (hasComparator)
                        rowList.Sort((a, b) => sortByColumn.ValueComparator(b.Value, a.Value));
                    else
                        rowList.Sort((a, b) => SortRowsCompare(b.ValueComparable, a.ValueComparable));
                    return true;

            }
            return false;
        }

        private static int SortRowsCompare(IComparable valueA, IComparable valueB)
        {
            if (valueA == null && valueB == null) return 0;
            if (valueA == null) return -1;
            if (valueB == null) return 1;
            return valueA.CompareTo(valueB);
        }
        #endregion

        private void _GridLink(GGrid grid)
        {
            if (this._Grid != null)
            {   // Unlink this data object from old visual this._Grid events:
            }

            this._Grid = grid;

            if (this._Grid != null)
            {   // Link this data object to new visual this._Grid events:
            }
        }
        /// <summary>
        /// Name of table, for search it in TableSet
        /// </summary>
        public string TableName { get { return this._TableName; } set { this._TableName = value; } }
        private string _TableName;
        /// <summary>
        /// Visual grid, can be null
        /// </summary>
        public GGrid Grid { get { return this._Grid; } internal set { this._GridLink(value); } }
        private GGrid _Grid;
        /// <summary>
        /// Data collection of columns
        /// </summary>
        public EList<DColumn> Columns { get { return this._Columns; } }
        private EList<DColumn> _Columns;
        /// <summary>
        /// Number of columns in this Table
        /// </summary>
        public int ColumnsCount { get { return this._Columns.Count; } }

        /// <summary>
        /// Data collection of rows
        /// </summary>
        public EList<DRow> Rows { get { return this._Rows; } }
        private EList<DRow> _Rows;
        /// <summary>
        /// Number of rows in this Table
        /// </summary>
        public int RowsCount { get { return this._Rows.Count; } }
    }
    #endregion
    #region DColumn
    /// <summary>
    /// DColumn : one column informations
    /// </summary>
    public class DColumn
    {
        public DColumn()
        {
            this._ColumnId = -1;
            this._IsVisible = true;
            this._SortingEnabled = true;
        }
        public DColumn(string name)
            : this()
        {
            this.Name = name;
        }
        public DColumn(string name, Djs.Common.Localizable.TextLoc text)
            : this()
        {
            this.Name = name;
            this._Text = text;
        }
        public override string ToString()
        {
            return this.Name + ": " + this.Text;
        }

        /// <summary>
        /// ColumnId = index of column in Table.
        /// -1 = column without a table.
        /// </summary>
        public int ColumnId { get { return this._ColumnId; } }
        private int _ColumnId = -1;
        /// <summary>
        /// Name, for example name of DB column
        /// </summary>
        public string Name { get { return this._Name; } set { this._Name = value; } }
        private string _Name;
        /// <summary>
        /// Caption = user readable text
        /// </summary>
        public Djs.Common.Localizable.TextLoc Text { get { return (this._Text != null ? this._Text : (Djs.Common.Localizable.TextLoc)this._Name); } set { this._Text = value; } }
        private Djs.Common.Localizable.TextLoc _Text;
        /// <summary>
        /// ToolTip = user readable information text
        /// </summary>
        public Djs.Common.Localizable.TextLoc ToolTip { get { return this._ToolTip; } set { this._ToolTip = value; } }
        private Djs.Common.Localizable.TextLoc _ToolTip;
        /// <summary>
        /// DataType = type of data in this column. null = generic data.
        /// </summary>
        public Type DataType { get { return this._DataType; } set { this._DataType = value; } }
        private Type _DataType;
        /// <summary>
        /// Format string for data in this column
        /// </summary>
        public string FormatString { get { return this._FormatString; } set { this._FormatString = value; } }
        private string _FormatString;
        /// <summary>
        /// Use TimeAxis on Grid table
        /// </summary>
        public bool UseTimeAxis { get { return this._UseTimeAxis; } set { this._UseTimeAxis = value; } }
        private bool _UseTimeAxis;
        /// <summary>
        /// true for visible columns (default), false for hidden
        /// </summary>
        public bool IsVisible { get { return this._IsVisible; } set { this._IsVisible = value; } }
        private bool _IsVisible;
        /// <summary>
        /// Initial Width of this column, in pixels
        /// Is used only from first Table in TableSet (MasterTable).
        /// </summary>
        public int? Width { get { return this._Width; } set { this._Width = value; } }
        private int? _Width;
        /// <summary>
        /// Range for Width of this column, in pixels.
        /// Is used only from first Table in TableSet (MasterTable).
        /// </summary>
        public Int32NRange WidthRange { get { return this._WidthRange; } set { this._WidthRange = value; } }
        private Int32NRange _WidthRange;
        /// <summary>
        /// Reference to Table, where this Column is included
        /// </summary>
        public DTable Table { get; private set; }
        /// <summary>
        /// true when this Row has reference to its Table
        /// </summary>
        public bool HasTable { get { return (this.Table != null); } }

        /// <summary>
        /// Comparartor of two values for this column, for sorting
        /// </summary>
        public Func<object, object, int> ValueComparator;
        /// <summary>
        /// true (default) when Sort by this column is enabled.
        /// false = disable sorting.
        /// </summary>
        public bool SortingEnabled { get { return this._SortingEnabled; } set { this._SortingEnabled = value; } }
        private bool _SortingEnabled;
        /// <summary>
        /// true when this column has ValueComparator
        /// </summary>
        internal bool HasValueComparator { get { return (this.ValueComparator != null); } }
        /// <summary>
        /// Attach this column into table.
        /// An column can be attached only to one table in one time.
        /// </summary>
        /// <param name="dTable"></param>
        internal void AttachToTable(DTable dTable)
        {
            this._ColumnId = dTable.GetColumnIdFor(this);
            this.Table = dTable;
        }
        /// <summary>
        /// Release this column from its table
        /// </summary>
        internal void DetachFromTable()
        {
            this._ColumnId = -1;
            this.Table = null;
        }
    }
    #endregion
    #region DRow
    /// <summary>
    /// DRow : data of one row
    /// </summary>
    public class DRow
    {
        public DRow()
        {
            this.RowId = Application.App.GetNextId(typeof(DRow));
            this.__ItemList = new List<object>();
        }
        public DRow(params object[] items) : this()
        {
            this.__ItemList = new List<object>(items);
        }
        /// <summary>
        /// Unique ID of this column, constant, read-only
        /// </summary>
        public int RowId { get; private set; }
        /// <summary>
        /// Number of items in this.Items array
        /// </summary>
        public int ItemsCount { get { return this.ItemList.Count; } }
        /// <summary>
        /// Array of items in this Row
        /// </summary>
        public object[] Items { get { return this.ItemList.ToArray(); } set { this.__ItemList = new List<object>(value); } }
        /// <summary>
        /// Value from / to item by its index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public object this[int index]
        {
            get { return (this._IsValidIndex(index) ? this.ItemList[index] : null); }
            set { if (this._IsValidIndex(index)) this.ItemList[index] = value; }
        }
        /// <summary>
        /// true when index is valid for current Items
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool _IsValidIndex(int index) { return (index >= 0 && index < this.ItemList.Count); }
        /// <summary>
        /// List of items in this Row
        /// </summary>
        protected virtual List<object> ItemList { get { if (this.__ItemList == null) this.__ItemList = new List<object>(); return this.__ItemList; } set { this.__ItemList = new List<object>(value); } }
        protected List<object> __ItemList;
        /// <summary>
        /// Refresh values in this.Items from source object (on descendants only)
        /// </summary>
        public virtual void Refresh() { }
        /// <summary>
        /// Reference to Table, where this Row is included
        /// </summary>
        public DTable Table { get; private set; }
        /// <summary>
        /// true when this Row has reference to its Table
        /// </summary>
        public bool HasTable { get { return (this.Table != null); } }
        /// <summary>
        /// Link this Row to its Table.
        /// Store DTable to this.Table and prepare items in this row by ColumnCount
        /// </summary>
        /// <param name="dTable"></param>
        internal void AtachToTable(DTable dTable)
        {
            this.Table = dTable;
            this.ItemsPrepare();
        }
        internal void DetachFromTable()
        {
            this.Table = null;
        }
        /// <summary>
        /// Prepare this.Items array, to ItemsCount equal to Table.ColumnsCount.
        /// Added new (null) items to end of array Items.
        /// </summary>
        internal virtual void ItemsPrepare()
        {
            if (this.HasTable)
            {
                while (this.ItemsCount < this.Table.ColumnsCount)
                    this.__ItemList.Add(null);
            }
        }
    }
    #endregion
    #region DTypeTable
    /// <summary>
    /// Type table, contains rows of strictly Type.
    /// </summary>
    /// <typeparam name="T">Type of rows. Properties of this type acts as Columns in Grid.</typeparam>
    public class DTypeTable<T> : DTable
    {
        /// <summary>
        /// Type table, contains rows of strictly Type.
        /// </summary>
        public DTypeTable()
            : base()
        {
            this.PrepareColumns();
        }
        protected void PrepareColumns()
        {
            List<DTypeColumn> columns = new List<DTypeColumn>();

            // Using Reflection of type <T> to create Columns for Table:
            Type type = typeof(T);
            int propertyIndex = 0;
            foreach (System.Reflection.PropertyInfo property in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy))
            {
                DTypeColumn dColumn = DTypeColumn.CreateFromProperty(property, ref propertyIndex);
                if (dColumn != null)
                    columns.Add(dColumn);
            }
            columns.Sort((a, b) => DTypeColumn.CompareByColumnIndex(a, b));
            this._TypeColumns = columns.ToArray();

            // Columns to base-table:
            this.Columns.Clear();
            this.Columns.AddRange(columns);
        }
        internal DTypeColumn[] TypeColumns { get { return this._TypeColumns; } }
        DTypeColumn[] _TypeColumns;
        /// <summary>
        /// Add new row with specified data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public DTypeRow<T> AddRow(T data)
        {
            DTypeRow<T> row = null;
            if (data != null)
            {
                row = new DTypeRow<T>(data);
                this.AddRow(row);
            }
            return row;
        }
    }
    /// <summary>
    /// DTypeColumn : One column (DColumn) in Type Table. Can read value from System.Reflection.PropertyInfo.
    /// </summary>
    public class DTypeColumn : DColumn
    {
        /// <summary>
        /// Try create new DTypeColumn from PropertyInfo.
        /// Property must be readable (property.CanRead == true), and must have one custom-attribute of type ColumnInfoAttribute.
        /// In other case, returns a null.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="propertyIndex"></param>
        /// <returns></returns>
        internal static DTypeColumn CreateFromProperty(System.Reflection.PropertyInfo property, ref int propertyIndex)
        {
            if (!property.CanRead) return null;
            ColumnInfoAttribute attribute = property.GetCustomAttributes(true).FirstOrDefault(a => a is ColumnInfoAttribute) as ColumnInfoAttribute;
            if (attribute == null) return null;
            return new DTypeColumn(property, attribute, propertyIndex++);
        }
        private DTypeColumn(System.Reflection.PropertyInfo property, ColumnInfoAttribute attribute, int propertyIndex)
            : base(property.Name)
        {
            this._Property = property;
            this._PropertyIndex = propertyIndex;
            this._ColumnIndex = attribute.ColumnIndex;
            this.Text = attribute.Text;
            this.ToolTip = attribute.ToolTip;
            this.IsVisible = attribute.Visible;
            this.DataType = property.PropertyType;
            this.UseTimeAxis = attribute.UseTimeAxis;
            this.Width = attribute.Width;
            this.WidthRange = (attribute.WidthMin.HasValue || attribute.WidthMax.HasValue ? new Int32NRange(attribute.WidthMin, attribute.WidthMax) : null);
        }
        /// <summary>
        /// Reflected PropertyInfo of data
        /// </summary>
        public System.Reflection.PropertyInfo Property { get { return this._Property; } }
        private System.Reflection.PropertyInfo _Property;
        /// <summary>
        /// Index from property attribute ColumnInfoAttribute.Index
        /// </summary>
        public int? ColumnIndex { get { return this._ColumnIndex; } }
        private int? _ColumnIndex;
        /// <summary>
        /// Ordinal index from Type reflection
        /// </summary>
        public int PropertyIndex { get { return this._PropertyIndex; } }
        private int _PropertyIndex;
        /// <summary>
        /// Compare two columns by ColumnIndex ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int CompareByColumnIndex(DTypeColumn a, DTypeColumn b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            int? ai = a.ColumnIndex;
            int? bi = b.ColumnIndex;
            if (ai.HasValue && bi.HasValue) return ai.Value.CompareTo(bi.Value);
            if (ai.HasValue) return 1;           // Compare(-1, null): +1 (null is allways smaller)
            if (bi.HasValue) return -1;          // Compare(null, -1): -1 (null is allways smaller)
            // Booth ColumnIndex does not have Value.

            int cmp = a.PropertyIndex.CompareTo(b.PropertyIndex);
            if (cmp != 0) return cmp;

            return String.Compare(a.Name, b.Name);
        }
    }
    public class DTypeRow<T> : DRow, IVisualRow
    {
        public DTypeRow(T data)
            : base()
        {
            this._Data = data;
            if (data is IVisualRow)
                this._DataVisual = data as IVisualRow;
        }
        /// <summary>
        /// List of items in this Row
        /// </summary>
        protected override List<object> ItemList
        {
            get { this.CheckItems(); return this.__ItemList; }
            set { base.ItemList = value; }
        }
        /// <summary>
        /// Type data of this row
        /// </summary>
        public T Data { get { return this._Data; } }
        private T _Data;
        /// <summary>
        /// Refresh data in this.Items array.
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.__ItemList = null;                  // In fact, data does not read now, data will be reloaded just-in-time (in Items.get() method)
        }
        /// <summary>
        /// Prepare this._Items
        /// </summary>
        internal override void ItemsPrepare()
        {
            // base method enlarge this._Items list to columns count.
            // In this (DTypeRow) method will be _Items prepared in Items.get() => CheckItems() => ReloadItems()
            this.__ItemList = null;                  // In fact, data does not read now, data will be reloaded just-in-time (in Items.get() method)
        }
        /// <summary>
        /// If (this._Items == null), then reloads data from this.Data to this._Items
        /// </summary>
        protected void CheckItems()
        {
            if (this.__ItemList == null)
                this.ReloadItems();
        }
        /// <summary>
        /// Reload items from this.Data object (generic type of user data) into this._Items, using columns informations from DTypeTable.TypeColumns
        /// </summary>
        protected void ReloadItems()
        {
            DTable table = this.Table;
            object[] items = null;
            if (table != null)
            {
                items = new object[table.ColumnsCount];
                DTypeTable<T> typeTable = (table != null ? table as DTypeTable<T> : null);
                T data = this.Data;
                if (typeTable != null && data != null)
                {
                    DTypeColumn[] columns = typeTable.TypeColumns;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        DTypeColumn column = columns[i];
                        if (column.Property != null)
                            items[i] = column.Property.GetValue(data, null);
                    }
                }
            }
            else
            {
                items = new object[0];
            }
            this.__ItemList = new List<object>(items);
        }
        #region IVisualRow explicit interface
        int? IVisualRow.RowHeight { get { return (this._DataVisual != null ? this._DataVisual.RowHeight : null); } }
        Int32NRange IVisualRow.RowHeightRange { get { return (this._DataVisual != null ? this._DataVisual.RowHeightRange : null); } }
        Color? IVisualRow.RowBackColor { get { return (this._DataVisual != null ? this._DataVisual.RowBackColor : null); } }
        private IVisualRow _DataVisual;
        #endregion
    }
    #endregion
    #region ColumnInfoAttribute
    /// <summary>
    /// Desctiption for column on visual Grid
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ColumnInfoAttribute : Attribute
    {
        /// <summary>
        /// Info on Column in Table
        /// </summary>
        /// <param name="columnIndex">Index of column in Grid</param>
        /// <param name="text">Name of column in Grid</param>
        /// <param name="toolTip">Tooltip for column in Grid</param>
        /// <param name="useTimeAxis">Use TimeAxis on Graph</param>
        /// <param name="width">Current width in pixel, -1 = default</param>
        /// <param name="widthMin">Minimal width in pixel, -1 = unlimited</param>
        /// <param name="widthMax">Maximal width in pixel, -1 = unlimited</param>
        public ColumnInfoAttribute(int columnIndex = 0, string text = null, string toolTip = null, bool useTimeAxis = false, int width = -1, int widthMin = -1, int widthMax = -1, bool visible = true)
        {
            this._ColumnIndex = columnIndex;
            this._Text = text;
            this._ToolTip = toolTip;
            this._Visible = visible;
            this._UseTimeAxis = useTimeAxis;
            this._Width = (width >= 0 ? (int?)width : (int?)null);
            this._WidthMin = (widthMin >= 0 ? (int?)widthMin : (int?)null);
            this._WidthMax = (widthMax >= 0 ? (int?)widthMax : (int?)null);
        }
        public int ColumnIndex { get { return this._ColumnIndex; } }
        readonly int _ColumnIndex;
        public Djs.Common.Localizable.TextLoc Text { get { return this._Text; } }
        readonly Djs.Common.Localizable.TextLoc _Text;
        public Djs.Common.Localizable.TextLoc ToolTip { get { return this._ToolTip; } }
        readonly Djs.Common.Localizable.TextLoc _ToolTip;
        public bool Visible { get { return this._Visible; } }
        readonly bool _Visible;
        public bool UseTimeAxis { get { return this._UseTimeAxis; } }
        readonly bool _UseTimeAxis;
        public int? Width { get { return this._Width; } }
        readonly int? _Width;
        public int? WidthMin { get { return this._WidthMin; } }
        readonly int? _WidthMin;
        public int? WidthMax { get { return this._WidthMax; } }
        readonly int? _WidthMax;
    }
    #endregion
    #region Interfaces

    public interface IVisualTable
    {
        /// <summary>
        /// Height Range for Column header
        /// </summary>
        Int32NRange ColumnHeaderHeightRange { get; }
    }

    public interface IVisualRow
    {
        /// <summary>
        /// Height for this Row
        /// </summary>
        Int32? RowHeight { get; }
        /// <summary>
        /// Height Range for Row
        /// </summary>
        Int32NRange RowHeightRange { get; }
        /// <summary>
        /// Color for row
        /// </summary>
        Color? RowBackColor { get; }
    }
    /// <summary>
    /// Class for sorting DRow list by comparable value
    /// </summary>
    public class DTableSortRowsItem
    {
        public DTableSortRowsItem(DRow row)
        {
            this.Row = row;
        }
        public DTableSortRowsItem(DRow row, object userData)
        {
            this.Row = row;
            this.UserData = userData;
        }
        /// <summary>
        /// Data row
        /// </summary>
        public DRow Row { get; private set; }
        /// <summary>
        /// Any user data
        /// </summary>
        public object UserData { get; private set; }
        public object Value { get; set; }
        public IComparable ValueComparable { get; set; }
    }
    public enum DTableSortRowType
    {
        None = 0,
        Ascending = 1,
        Descending = 2
    }
    #endregion
}
