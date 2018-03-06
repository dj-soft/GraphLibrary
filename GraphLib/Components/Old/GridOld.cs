using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;
using Djs.Common.Components;
using Djs.Common.ComponentsOld.Grid;

// This file contain Visual items for Graphical Grid (all classes are InteractiveObject, is used in GGrid class for visualisation of Grid data
namespace Djs.Common.ComponentsOld
{
    /// <summary>
    /// GGrid : Visual container for show one or more GTable, with synchronized ColumnsWidths
    /// </summary>
    public class GGrid : InteractiveContainer, IInteractiveItem
    {
        public GGrid()
        {
            this._InitialiseGrid();
        }
        private void _InitialiseGrid()
        {
            this.InitColors();
            this.InitProperties();
            this.InitPositions();
            this.InitTables();
            this.InitColumns();
            this.InitScrollBars();
        }
        protected void InitColors()
        {

        }
        #region BoundsChange
        /// <summary>
        /// Is called after Bounds change, from SetBound() method, only when action PrepareInnerItems is specified.
        /// Recalculate SubItems bounds after change this.Bounds.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {   // After change of Bounds, when action ProcessAction.PrepareInnerItems is requested:
            this.RecalcGrid(ref actions, eventSource);
        }
        /// <summary>
        /// Recalculate position of all columns, tables and scrollbar.
        /// </summary>
        internal void RecalcGrid()
        {
            ProcessAction actions = ProcessAction.None;
            EventSourceType eventSource = EventSourceType.ApplicationCode;
            this.RecalcGrid(ref actions, eventSource);
        }
        /// <summary>
        /// Recalculate position of all columns, tables and scrollbar.
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal void RecalcGrid(ref ProcessAction actions, EventSourceType eventSource)
        {
            this.RecalcClientAreas(ref actions, eventSource);
            this.RecalcTables(ref actions, eventSource);
            this.RecalcScrollBars(ref actions, eventSource);
        }
        /// <summary>
        /// Recalculate position of inner areas = for Tables and for Scrollbar
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        private void RecalcClientAreas(ref ProcessAction actions, EventSourceType eventSource)
        {
            Size clientSize = this.BoundsClient.Size;
            int x = 0;
            int y = 0;
            int hscY = clientSize.Height - 0 - GScrollBar.DefaultSystemBarHeight + 1;
            int tblH = hscY - 2;

            bool vscV = (tblH < this.Positions.TablesTotalSizeVisible);
            this._GridVerticalScrollBarVisible = vscV;
            this._GridVerticalScrollBarBounds = new Rectangle(x, y, (vscV ? GScrollBar.DefaultSystemBarWidth : 0), tblH);
            this.ScrollBarY.IsVisible = vscV;
            x += (vscV ? this._GridVerticalScrollBarBounds.Width : 0);

            int tblW = clientSize.Width - 1 - x;
            this._GridTablesBounds = new Rectangle(x, y, tblW, tblH);
            y += tblH;

            bool hscV = true;
            this._GridHorizontalScrollBarVisible = hscV;
            this._GridHorizontalScrollBarBounds = new Rectangle(x, y, tblW, (hscV ? GScrollBar.DefaultSystemBarHeight : 0));
            this.ScrollBarX.IsVisible = hscV;
        }
        private bool _GridVerticalScrollBarVisible;
        private Rectangle _GridVerticalScrollBarBounds;
        private bool _GridHorizontalScrollBarVisible;
        private Rectangle _GridHorizontalScrollBarBounds;
        private Rectangle _GridTablesBounds;
        #endregion
        #region Public events and virtual methods

        #endregion
        #region Public properties
        protected void InitProperties()
        {
            this.ScrollBarVerticalWidth = GScrollBar.DefaultSystemBarWidth;
            this.ScrollBarHorizontalHeight = GScrollBar.DefaultSystemBarHeight;
        }
        public int ScrollBarVerticalWidth { get { return this._ScrollBarVerticalWidth; } set { this._ScrollBarVerticalWidth = (value < 0 ? 0 : value); } }
        private int _ScrollBarVerticalWidth;
        public int ScrollBarHorizontalHeight { get { return this._ScrollBarHorizontalHeight; } set { this._ScrollBarHorizontalHeight = (value < 0 ? 0 : value); } }
        private int _ScrollBarHorizontalHeight;

        #endregion
        #region Tables
        /// <summary>
        /// Initialise GTables subsystem
        /// </summary>
        protected void InitTables()
        {
            this._Tables = new EList<GTable>();
            this._Tables.ItemAddAfter += new EList<GTable>.EListEventAfterHandler(_TableAddAfter);
            this._Tables.ItemRemoveAfter += new EList<GTable>.EListEventAfterHandler(_TableRemoveAfter);
        }
        /// <summary>
        /// Recalculate values and bounds for all GTables.
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcTables(ref ProcessAction actions, EventSourceType eventSource)
        {
            Rectangle tablesBounds = this._GridTablesBounds;
            foreach (GTable gTable in this._Tables)
                gTable.RecalcTable(tablesBounds, ref actions, eventSource);
        }
        /// <summary>
        /// Refill TimeAxis value from column (columnId) into all tables
        /// </summary>
        /// <param name="columnId"></param>
        internal void RefreshTimeAxis(int columnId)
        {
            foreach (GTable gTable in this._Tables)
                gTable.RefreshTimeAxis(columnId);
        }
        /// <summary>
        /// Collection of Tables in this Grids
        /// </summary>
        public IEnumerable<DTable> DataTables { get { return this._Tables.Select(t => t.DataTable); } }
        /// <summary>
        /// true when exists at least one Table
        /// </summary>
        public bool HasTables { get { return (this._Tables.Count > 0); } }

        public GTable AddTable(DTable dTable)
        {
            return this.AddTable(dTable, null, null, null);
        }
        public GTable AddTable(DTable dTable, string text, Int32NRange sizeRange)
        {
            return this.AddTable(dTable, text, sizeRange, null);
        }
        public GTable AddTable(DTable dTable, string text, Int32NRange sizeRange, int? size)
        {
            if (dTable == null) return null;
            int tableId = this._TableID++;
            GTable gTable = new GTable(this, dTable, tableId, text, sizeRange, size);
            this._Tables.Add(gTable);                      // In handler (EList => this._TableAddAfter()) will be called this._AttachGTable(gTable), and linked this handlers to DTable events ColumnAddAfter, ColumnRemoveAfter, RowAddAfter, RowRemoveAfter
            this.Refresh();
            return gTable;
        }
        public void RemoveTable(int index)
        {
            this.RemoveTable(this.GetTable(index));
        }
        public void RemoveTable(string tableName)
        {
            this.RemoveTable(this.GetTable(tableName));
        }
        public void RemoveTable(DTable dTable)
        {
            if (dTable == null) return;
            int index = this._Tables.FindIndex(t => Object.ReferenceEquals(t, dTable));
            if (index < 0) return;
            GTable gTable = this._Tables[index];
            this._Tables.RemoveAt(index);        // In handler this._TableRemoveAfter() will be called this._DetachGTable(gTable)
            this.Refresh();
        }
        /// <summary>
        /// Returns a table on index (index), or return null.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DTable GetTable(int index)
        {
            GTable table = (index >= 0 && index < this._Tables.Count ? this._Tables[index] : null);
            return (table == null ? null : table.DataTable);
        }
        /// <summary>
        /// Returns a table with name (tableName), or return null.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DTable GetTable(string tableName)
        {
            GTable table = (!String.IsNullOrEmpty(tableName) ? this._Tables.FirstOrDefault(t => !String.IsNullOrEmpty(t.TableName) && String.Equals(t.TableName, tableName)) : null);
            return (table == null ? null : table.DataTable);
        }

        private void _AttachGTableEventHandlers(GTable gTable)
        {
            gTable.ColumnAddAfter += this._TableColumnAddAfter;
            gTable.ColumnRemoveAfter += this._TableColumnRemoveAfter;
            gTable.RowAddAfter += this._TableRowAddAfter;
            gTable.RowRemoveAfter += this._TableRowRemoveAfter;
        }
        private void _DetachGTableEventHandlers(GTable gTable)
        {
            gTable.ColumnAddAfter -= this._TableColumnAddAfter;
            gTable.ColumnRemoveAfter -= this._TableColumnRemoveAfter;
            gTable.RowAddAfter -= this._TableRowAddAfter;
            gTable.RowRemoveAfter -= this._TableRowRemoveAfter;
        }
        private void _TableAddAfter(object sender, EList<GTable>.EListAfterEventArgs args)
        {
            GTable gTable = args.Item;
            this._AttachGTableEventHandlers(gTable);       // Attach handler for GTable.Events => this.Handlers
            gTable.TablePosition = this.Positions.GetTable(gTable.TableId);
            gTable.TablePosition.SizeRange = gTable.SizeRange;
            gTable.TablePosition.SizeN = gTable.Size;
            this.ReloadColumns(gTable.DataTable, false);
        }
        private void _TableRemoveAfter(object sender, EList<GTable>.EListAfterEventArgs args)
        {
            GTable gTable = args.Item;
            this._DetachGTableEventHandlers(args.Item);       // Remove handler for GTable.Events => this.Handlers
            args.Item.TablePosition = null;
            this.Positions.RemoveTable(gTable.TableId);
            this.ChildArrayInvalidate();
        }
        private void _TableColumnAddAfter(object sender, EList<DColumn>.EListEventArgs args)
        {
            this.ReloadColumns(args.Item.Table, false);
        }
        private void _TableColumnRemoveAfter(object sender, EList<DColumn>.EListEventArgs args)
        {
            this.ReloadColumns(true);
        }
        private void _TableRowAddAfter(object sender, EList<DRow>.EListEventArgs args)
        {
            // One DataTable was added new DataRow, linked GTable added new GRow. Now GGrid will invalidate its ...:
        }
        private void _TableRowRemoveAfter(object sender, EList<DRow>.EListEventArgs args)
        {
            // One DataTable was removed a DataRow, linked GTable removed appropriate GRow. Now GGrid will invalidate its ...:
        }
        /// <summary>
        /// ID for next added table
        /// </summary>
        private int _TableID = 0;
        /// <summary>
        /// Returns a Table from index shifted from specified column.
        /// Returns Tables[IndexOf(gTable) + shift], or return null.
        /// </summary>
        /// <param name="gTable"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        internal GTable GetOtherTable(GTable gTable, int shift)
        {
            int index = this.GetTableIndex(gTable);
            int otherIndex = (index < 0 ? -1 : index + shift);
            return ((otherIndex >= 0 && otherIndex < this.TableCount) ? this._Tables[otherIndex] : null);
        }
        /// <summary>
        /// Returns a index of specified object in this._Tables list.
        /// </summary>
        /// <param name="gTable"></param>
        /// <returns></returns>
        internal int GetTableIndex(GTable gTable)
        {
            return (gTable != null ? this._Tables.FindIndex(tbl => Object.ReferenceEquals(tbl, gTable)) : -1);
        }
        /// <summary>
        /// Contain a number of tables in this Grid
        /// </summary>
        internal int TableCount { get { return this._Tables.Count; } }
        /// <summary>
        /// Collection of Tables
        /// </summary>
        internal IEnumerable<GTable> Tables { get { return this._Tables; } }
        private EList<GTable> _Tables;
        #endregion
        #region Columns and Positions
        /// <summary>
        /// Initialise positions
        /// </summary>
        private void InitPositions()
        {
            this._Positions = new GridPositions();
        }
        /// <summary>
        /// Positions of all visual items (Columns and Tables)
        /// </summary>
        internal GridPositions Positions
        {
            get
            {
                if (this._Positions == null)
                {
                    this._Positions = new GridPositions();
                }
                return this._Positions;
            }
            set
            {
                this._Positions = ((value != null) ? value : new GridPositions());
                ProcessAction actions = ProcessAction.All;
                this.RecalcGrid(ref actions, EventSourceType.ApplicationCode | EventSourceType.BoundsChange);
            }
        }
        private GridPositions _Positions;
        protected void InitColumns()
        {
            this.Positions.RowHeaderWidth = DefaultRowHeaderWidth;
        }
        /// <summary>
        /// Reload data for all columns from all tables into this.Positions.
        /// Create a new items into this.Positions.Columns array.
        /// Preserve current parameters, when possible.
        /// </summary>
        /// <param name="force">true = clear first all positions</param>
        protected void ReloadColumns(bool force)
        {
            if (force)
                this.Positions.ClearColumns();
            foreach (GTable table in this.Tables)
                this.Positions.ReloadColumns(table.DataTable, false);          // Load data only to new columns. This is: definition from Master Table is preferred than defitions from another Tables. But columns from another tables are added to Positions.Columns, when this tables has more columns than Master table.
            this.ChildArrayInvalidate();
        }
        /// <summary>
        /// Create a new items into this.Columns array.
        /// Preserve current values, when possible.
        /// </summary>
        /// <param name="dataTable">Source table</param>
        /// <param name="force">true = overwrite values into existing columns, false = preserve values in existing columns.</param>
        protected void ReloadColumns(DTable dataTable, bool force)
        {
            this.Positions.ReloadColumns(dataTable, force);
            this.ChildArrayInvalidate();
        }
        /// <summary>
        /// Default width for RowHeader column
        /// </summary>
        public static int DefaultRowHeaderWidth { get { return 20; } }
        #endregion
        #region ScrollBarX, ScrollBarY
        /// <summary>
        /// Initialise scroll bars
        /// </summary>
        protected void InitScrollBars()
        {
            this.ScrollBarX = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            this.ScrollBarY = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            ProcessAction actions = ProcessAction.None;
            this.RecalcScrollBars(ref actions, EventSourceType.ApplicationCode);
            this.ScrollBarX.ValueChanging += new GPropertyChanged<SizeRange>(ScrollBarX_ValueChange);
            this.ScrollBarX.ValueChanged += new GPropertyChanged<SizeRange>(ScrollBarX_ValueChange);
            this.ScrollBarY.ValueChanging += new GPropertyChanged<SizeRange>(ScrollBarY_ValueChange);
            this.ScrollBarY.ValueChanged += new GPropertyChanged<SizeRange>(ScrollBarY_ValueChange);
        }
        /// <summary>
        /// Recalculate values and bounds for Horizontal and Vertical Scrollbar.
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcScrollBars(ref ProcessAction actions, EventSourceType eventSource)
        {
            this.RecalcScrollBarX(ref actions, eventSource);
            this.RecalcScrollBarY(ref actions, eventSource);
        }
        /// <summary>
        /// Recalculate values and bounds for Horizontal Scrollbar ("Column" scrollbar).
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcScrollBarX(ref ProcessAction actions, EventSourceType eventSource)
        {
            if (this._GridHorizontalScrollBarVisible)
            {
                Rectangle bounds = this._GridHorizontalScrollBarBounds;
                int total = this.Positions.ColumnsTotalSizeVisible + 45;
                int offset = this.Positions.ColumnsVisualOffset;
                int visual = bounds.Width;
                using (this.ScrollBarX.SuppressEvents())
                {
                    this.ScrollBarX.Bounds = bounds;
                    this.ScrollBarX.ValueTotal = new SizeRange(0, total);
                    this.ScrollBarX.Value = new SizeRange(offset, offset + visual);
                    this.ScrollBarX.IsEnabled = (total > visual);
                }
            }
        }
        /// <summary>
        /// Recalculate values and bounds for Vertical Scrollbar ("Tables" scrollbar).
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcScrollBarY(ref ProcessAction actions, EventSourceType eventSource)
        {
            if (this._GridVerticalScrollBarVisible)
            {
                Rectangle bounds = this._GridVerticalScrollBarBounds;
                int total = this.Positions.TablesTotalSizeVisible + 5;
                int offset = this.Positions.TableVisualOffset;
                int visual = bounds.Height;
                using (this.ScrollBarY.SuppressEvents())
                {
                    this.ScrollBarY.Bounds = bounds;
                    this.ScrollBarY.ValueTotal = new SizeRange(0, total);
                    this.ScrollBarY.Value = new SizeRange(offset, offset + visual);
                    this.ScrollBarY.IsEnabled = (total > visual);
                }
            }
        }
        /// <summary>
        /// Eventhandler for Horizontal splitter value changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollBarX_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.ScrollBarX.Value.Begin.Value;
            if (offset == this.Positions.ColumnsVisualOffset) return;
            this.Positions.ColumnsVisualOffset = offset;
            this.RecalcGrid();
            this.RepaintAllItems = true;
        }
        /// <summary>
        /// Eventhandler for Vertical splitter value changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollBarY_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.ScrollBarY.Value.Begin.Value;
            if (offset == this.Positions.TableVisualOffset) return;
            this.Positions.TableVisualOffset = offset;
            this.RecalcGrid();
            this.RepaintToLayers = GInteractiveDrawLayer.Standard;
            //        this.RepaintAllItems = true;
        }
        /// <summary>
        /// Horizontal Scrollbar for Columns shift
        /// </summary>
        protected GScrollBar ScrollBarX;
        /// <summary>
        /// Vertical Scrollbar for Tables (no its Rows!) shift
        /// </summary>
        protected GScrollBar ScrollBarY;
        #endregion
        #region Childs items
        /// <summary>
        /// An array of sub-items in this item.
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.ChildArrayCheck(); return this.ChildList; } }
        /// <summary>
        /// Invalidate Child array, call after any change on Tables or Columns in this grid.
        /// </summary>
        protected void ChildArrayInvalidate()
        {
            this._ChildArrayValid = false;
        }
        /// <summary>
        /// Check this.GridItems: when is null, then call this.GridItemsReload()
        /// </summary>
        protected void ChildArrayCheck()
        {
            if (!this._ChildArrayValid)
                this.ChildArrayReload();
        }
        /// <summary>
        /// Reload all current items for this Grid into .
        /// Add this items: this._Tables, 
        /// </summary>
        protected void ChildArrayReload()
        {
            this.ChildList.Clear();

            // First (on bottom in hierarchy) is Tables:
            this.ChildList.AddRange(this._Tables);

            // Next (=in hierarchy: above Tables) is Table-Splitters, for all except last GTable:
            for (int t = 0; t < (this._Tables.Count - 1); t++)
                this.ChildList.Add(this._Tables[t].TableSplitter);

            // And top-most (in hierarchy) is HorizontalScrollBar (only when exists any Columns) and VerticalScrollBar:

            if (this.Positions.TableCount > 0)
                this.ChildList.Add(this.ScrollBarY);
            if (this.Positions.ColumnsCount > 0)
                this.ChildList.Add(this.ScrollBarX);

            this._ChildArrayValid = true;
        }
        private bool _ChildArrayValid;
        #endregion
        #region Refresh
        public override void Refresh()
        {
            this.Refresh(GridRefreshStyle.All, null);
        }
        public void Refresh(DTable table)
        {
            this.Refresh(GridRefreshStyle.All, table);
        }
        protected void Refresh(GridRefreshStyle refreshStyle, DTable dTable)
        {
            if ((refreshStyle & GridRefreshStyle.ReloadColumns) != 0)
                this.ReloadColumns(false);
            if ((refreshStyle & GridRefreshStyle.ReloadRows) != 0)
                this.RefreshRows(dTable);
            base.Refresh();
        }

        private void RefreshRows(DTable dTable)
        {

        }

        [Flags]
        protected enum GridRefreshStyle
        {
            None = 0,
            ReloadColumns = 0x0001,
            ReloadRows = 0x0010,

            All = 0xFFFF
        }
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            // GGrid (this is other areas, without any Child object) is inactive.
            // This is only in situation, when GGrid has no GTable (=no Columns, no Splitters, and HorizontalScrollbar is hidden)
        }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
            // Rectangle bounds = this.BoundsAbsolute;
            // e.Graphics.FillRectangle(Brushes.LightYellow, bounds);
            /*
            foreach (IInteractiveItem item in this.Childs)
                item.Draw(e);
            */
        }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        #endregion
        #region Architecture comments
        /*   Vizuální grid se skládá z:

     - Colums = Jedna kolekce sloupců, řídí pozice sloupců v gridu (počet, šířka), vizuálně se projevuje pouze přes splittery za sloupci jednotlivých tabulek
     - Svislé splittery za každým sloupcem, včetně sloupce RowHeader = řídí šířku sloupce, jsou přes celou výšku Gridu
     - Jedna kolekce tabulek, vizuálně umístěných pod sebou
      - Každá tabulka se skládá z:
       - Prostor ColumnHeader
          - RowColumnHeader
          - ColumnsHeader
       - Prostor RowsArea, skládá se z:
          - Řádků, kde každý řádek má:
            - RowHeader
            - RowArea = buňky dle pozic sloupců Gridu
          - Prostor bez řádků pod ním
          - Svislý ScrollBar vpravo, určuje vodorovný offset (Y) pro řádky
       - Vodorovný Splitter pod prostorem řádků, pouze u tabulek které NEJSOU poslední (=pod splitterem začíná další tabulka)
     - Vodorovný ScrollBar dole, určuje vodorovný offset (Y) pro sloupce, nachází se dole v gridu, začíná za sloupcem RowHeader, končí před svislými scrollbary
    
    */
        #endregion
        #region GGridColumnSet + GGridColumn                  comment out !!!
        /*
        /// <summary>
        /// GGridColumnSet : set of synchronized columns on Grid, for all Tables and its appropriate Column.
        /// </summary>
        internal class GGridColumnSet : List<GGridColumn>
        {
            public GGridColumnSet(GGrid grid)
            {
                this._Grid = grid;
            }
            /// <summary>
            /// Owner GGrid
            /// </summary>
            public GGrid Grid { get { return this._Grid; } } private GGrid _Grid;
        }
        /// <summary>
        /// GGridColumn : one synchronized column on Grid, for all Tables and its appropriate Column.
        /// GGridColumn itself is not drawed and is not interactive, his role is to control GTable.Columns Widths and its Splitters.
        /// </summary>
        internal class GGridColumn
        {
            public GGridColumn(GGrid grid, int index, DColumn dColumn, GGridColumn gColumn)
            {
                this._Grid = grid;
                this._ColumnIndex = index;
                this._Visible = true;
                this.Width = 125;
                this._FillFrom(dColumn, gColumn);
                this._CreateSplitter();
            }
            /// <summary>
            /// Owner GGrid
            /// </summary>
            public GGrid Grid { get { return this._Grid; } } private GGrid _Grid;
            /// <summary>
            /// Index of data column. 
            /// Contain -1 for RowHeader Column!
            /// </summary>
            public int ColumnIndex { get { return this._ColumnIndex; } } private int _ColumnIndex;
            /// <summary>
            /// true for RowHeader column (has ColumnIndex == -1, and has no data from table)
            /// </summary>
            public bool IsRowHeader { get { return (this._ColumnIndex < 0); } }
            /// <summary>
            /// Is column visible?
            /// </summary>
            public bool Visible { get { return this._Visible; } } private bool _Visible;
            /// <summary>
            /// Current Width of this column, in pixel. 
            /// Is not aligned to WidthRange. 
            /// Has positive value even for Visible = false.
            /// You can use CurrentWidth for accept (Visible == false) and align to range..
            /// </summary>
            public int Width { get; set; }
            /// <summary>
            /// Range for Width of this column, in pixels. Can be null.
            /// Is used only from first Table in TableSet (MasterTable).
            /// </summary>
            public Int32Range WidthRange { get; set; }
            /// <summary>
            /// Current real Logical Width of this column, in pixel.
            /// For IsRowHeader returns 0 (RowHeader does not have logical width!).
            /// For other columns return VisualWidth (=Accept this.Visible, Width and WidthRange). Does not return a negative value.
            /// </summary>
            public int LogicalWidth { get { return (this.IsRowHeader ? 0 : this.VisualWidth); } }
            /// <summary>
            /// Current real Visual Width of this column, in pixel. Accept this.Visible, Width and WidthRange. Does not return a negative value.
            /// </summary>
            public int VisualWidth { get { return this.GetCurrentWidth(); } }
            #region Coordinates of column (CurrentBounds, AbsoluteBounds)
            /// <summary>
            /// Logical bounds of column = coordinates on X axis (Left, Width, Right as: Begin, Size, End), with shift of columns on X-axis.
            /// </summary>
            public Int32Range LogicalBounds { get { return this._LogicalBounds; } set { this._LogicalBounds = value; } } private Int32Range _LogicalBounds;
            /// <summary>
            /// Visual bounds of column = coordinates on X axis (Left, Width, Right as: Begin, Size, End), in real coordinates on current Grid.
            /// Bounds of whole column, by its VisualWidth, from its begin to End, is not cropped to real area (see CurrentBounds)
            /// </summary>
            public Int32Range VisualBounds { get { return this._VisualBounds; } set { this._VisualBounds = value; } } private Int32Range _VisualBounds;
            /// <summary>
            /// Current Visual bounds of column = coordinates on X axis (Left, Width, Right as: Begin, Size, End), in real coordinates on current Grid, cropped to Visible Area.
            /// </summary>
            public Int32Range CurrentBounds { get { return this._CurrentBounds; } set { this._CurrentBounds = value; } } private Int32Range _CurrentBounds;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="logical"></param>
            /// <param name="visual"></param>
            /// <param name="current"></param>
            /// <param name="currentRange"></param>
            internal void StoreBounds(ref int logical, ref int visual, Int32Range currentRange, Int32Range currentHeight)
            {
                this._LogicalBounds = new Int32Range(logical, logical + this.LogicalWidth); logical += this.LogicalWidth;
                this._VisualBounds = new Int32Range(visual, visual + this.VisualWidth); visual += this.VisualWidth;
                this._CurrentBounds = (currentRange != null ? this._VisualBounds * currentRange : this._VisualBounds);

                // Position for Splitter:
                if (this.Splitter != null)
                {
                    int location = visual;
                    bool active = (currentRange == null || (currentRange != null && currentRange.Contains(visual)));
                    if (this.Splitter.Value != location && active)
                        this.Splitter.Value = location;
                    if (this.Splitter.IsVisible != active)
                        this.Splitter.IsVisible = active;
                    if (currentHeight != null && this.Splitter.BoundsNonActive != currentHeight)
                        this.Splitter.BoundsNonActive = currentHeight;
                }
            }
            /// <summary>
            /// Returns correct Width value, from Visible, Width and WidthRange. Does not return a negative value.
            /// </summary>
            /// <returns></returns>
            protected int GetCurrentWidth()
            {
                int width = 0;
                if (this.Visible)
                {
                    width = this.Width;
                    if (this.WidthRange != null && this.WidthRange.IsReal)
                        width = this.WidthRange.Align(width).Value;
                    if (width < 0) width = 0;
                }
                return width;
            }
            #endregion
            #region Fill properties from Data Column and old GGridColumn
            /// <summary>
            /// Fill this properties from DataColumn
            /// </summary>
            /// <param name="dColumn"></param>
            internal void FillFrom(DColumn dColumn)
            {
                int width = this.Width;
                if (dColumn != null)
                {
                    this.WidthRange = dColumn.WidthRange;
                    if (dColumn.Width.HasValue && this.WidthRange != null && !this.WidthRange.Contains(this.Width))
                        width = dColumn.Width.Value;
                }
                this.Width = width;
            }
            /// <summary>
            /// Fill this properties from DataColumn and from GridColumn
            /// </summary>
            /// <param name="dColumn"></param>
            /// <param name="gColumn"></param>
            private void _FillFrom(DColumn dColumn, GGridColumn gColumn)
            {
                int width = this.Width;
                if (dColumn != null)
                {
                    this.WidthRange = dColumn.WidthRange;
                    if (dColumn.Width.HasValue) width = dColumn.Width.Value;
                }
                if (gColumn != null)
                {
                    width = gColumn.Width;
                }
                this.Width = width;
            }
            #endregion
            #region Spliter (creation, handlers, events)
            private void _CreateSplitter()
            {
                this._Splitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Vertical, SplitterActiveOverlap = 2, SplitterVisibleWidth = 3 };
                this._Splitter.LocationChanged += new GPropertyChanged<int>(_Splitter_LocationChanged);
            }
            /// <summary>
            /// Handler for event LocationChanged on Column-Splitter.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _Splitter_LocationChanged(object sender, GPropertyChangeArgs<int> e)
            {
                this._Grid.ColumnSplitterLocationChanged(this, e);
            }
            /// <summary>
            /// Graphical Splitter, after this column (on right-edge)
            /// </summary>
            public GSplitter Splitter { get { return this._Splitter; } } private GSplitter _Splitter;
            #endregion
            #region PrevItem, NextItem, IsFirst, IsLast
            /// <summary>
            /// GGridColumn before this column
            /// </summary>
            public GGridColumn PrevItem { get { return this._Grid.GetOtherColumn(this, -1); } }
            /// <summary>
            /// true when this GGridColumn is first in collection (PrevItem is null)
            /// </summary>
            public bool IsFirst { get { return (this.PrevItem == null); } }
            /// <summary>
            /// GGridColumn after this column
            /// </summary>
            public GGridColumn NextItem { get { return this._Grid.GetOtherColumn(this, 1); } }
            /// <summary>
            /// true when this GGridColumn is last in collection (NextItem is null)
            /// </summary>
            public bool IsLast { get { return (this.NextItem == null); } }
            #endregion
        }
        */
        #endregion
    }

}
