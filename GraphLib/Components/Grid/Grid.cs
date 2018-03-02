using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;
using Djs.Common.Data.New;
using Djs.Common.Components.Grid;

// This file contain Visual items for Graphical Grid (all classes are InteractiveObject, is used in GGrid class for visualisation of Grid data
namespace Djs.Common.Components
{
    /// <summary>
    /// GGrid : Vizuální objekt, kontejner na jednu nebo více tabulek pod sebou. Tyto tabulky mají společný layout sloupců (šířka) i společný vodorovný (dolní) posuvník.
    /// </summary>
    public class GGrid : InteractiveContainer, IInteractiveItem
    {
        #region Inicializace
        public GGrid()
        {
            this.Init();
        }
        private void Init()
        {
            this.InitProperties();
            this.InitTablesData();
            this.InitTablesPositions();
            this.InitColumnsPositions();
            this.InitScrollBars();
        }
        #endregion
        #region Rozmístění vnitřních prvků gridu = souřadnice pro prostor tabulek a scrollbarů
        /// <summary>
        /// Je voláno po změně Bounds, z metody SetBound(), pokud je vyžadována akce PrepareInnerItems.
        /// Přepočte umístění vnitřních prvků objektu, podle rozměrů this.BoundsClient.Size
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změně. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.RecalculateGrid(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá pozice všech prvků Gridu (ClientAreas, Tables, ScrollBars).
        /// Jako action vezme None, jako eventSource = ApplicationCode.
        /// </summary>
        internal void RecalculateGrid()
        {
            ProcessAction actions = ProcessAction.None;
            EventSourceType eventSource = EventSourceType.ApplicationCode;
            this.RecalculateGrid(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá pozice všech prvků Gridu (ClientAreas, Tables, ScrollBars).
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal void RecalculateGrid(ref ProcessAction actions, EventSourceType eventSource)
        {
            this.RecalcClientAreas(ref actions, eventSource);
            this.RecalcTables(ref actions, eventSource);
            this.RecalcScrollBars(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá pozice ClientAreas = prostor tabulek a ScrollBarů.
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        private void RecalcClientAreas(ref ProcessAction actions, EventSourceType eventSource)
        {
            Size clientSize = this.BoundsClient.Size;
            this.ColumnsPositions.
            int x = 0;
            int y = 0;
            int hscY = clientSize.Height - 0 - GScrollBar.DefaultSystemBarHeight + 1;
            int tblH = hscY - 2;

            bool vscV = (tblH < this.Positions.TablesTotalSizeVisible);
            this._GridVerticalScrollBarVisible = vscV;
            this._GridVerticalScrollBarBounds = new Rectangle(x, y, (vscV ? GScrollBar.DefaultSystemBarWidth : 0), tblH);
            this.TablesScrollBar.IsVisible = vscV;
            x += (vscV ? this._GridVerticalScrollBarBounds.Width : 0);

            int tblW = clientSize.Width - 1 - x;
            this._GridTablesBounds = new Rectangle(x, y, tblW, tblH);
            y += tblH;

            bool hscV = true;
            this._GridHorizontalScrollBarVisible = hscV;
            this._GridHorizontalScrollBarBounds = new Rectangle(x, y, tblW, (hscV ? GScrollBar.DefaultSystemBarHeight : 0));
            this.ColumnsScrollBar.IsVisible = hscV;

            this._GridLayoutValid = true;
        }
        private bool _GridVerticalScrollBarVisible;
        /// <summary>
        /// Prostor pro svislý scrollbar (vpravo)
        /// </summary>
        private Rectangle _GridVerticalScrollBarBounds;
        private bool _GridHorizontalScrollBarVisible;
        /// <summary>
        /// Prostor pro dolní scrollbar (horizontální)
        /// </summary>
        private Rectangle _GridHorizontalScrollBarBounds;
        /// <summary>
        /// Prostor pro tabulky (vlevo a nahoře), neobsahuje pozice scrollbarů X a Y
        /// </summary>
        private Rectangle _GridTablesBounds;
        private bool _GridLayoutValid;
        #endregion
        #region Pozicování vodorovné = sloupce tabulek a dolní vodorovný scrollbar
        private void InitColumnsPositions()
        {
            this._ColumnsPositions = new GPosition(26, this._ColumnPositionGetVisualSize, this._ColumnPositionGetDataSize);

            this.ColumnsScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            this.ColumnsScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
            this.ColumnsScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídící prvek pro Pozice sloupců
        /// </summary>
        protected GPosition ColumnsPositions { get { return this._ColumnsPositions; } }
        private GPosition _ColumnsPositions;
        /// <summary>
        /// Vrací šířku prostoru pro datové sloupce (= ClientSize.Width - ColumnsPositions.VisualFirstPixel)
        /// </summary>
        /// <returns></returns>
        private int _ColumnPositionGetVisualSize()
        {
            return this.ClientSize.Width - this._ColumnsPositions.VisualFirstPixel;
        }
        /// <summary>
        /// Vrací šířku všech zobrazitelných datových sloupců, vyjma sloupec RowHeader (to není datový sloupec).
        /// </summary>
        /// <returns></returns>
        private int _ColumnPositionGetDataSize()
        {
            List<ISequenceLayout> list = this.ColumnsSequence;
            int count = list.Count;
            return (count > 0 ? list[count - 1].End : 0);
        }
        /// <summary>
        /// Soupis sloupců master tabulky, vždy setříděný v pořadí podle ColumnOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou ColumnsSequenceReset(), ta se má volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky nebo Master tabulka nemá žádné sloupce).
        /// </summary>
        protected List<ISequenceLayout> ColumnsSequence
        {
            get
            {
                if (this._ColumnsSequence == null)
                {
                    List<Column> columns = new List<Column>();
                    if (this._Tables != null && this._Tables.Count > 0)
                        columns.AddRange(this._Tables[0].DataTable.Columns);
                    if (columns.Count > 1)
                        columns.Sort(Column.CompareOrder);

                    List<Data.New.ISequenceLayout> list = new List<ISequenceLayout>(columns.Cast<ISequenceLayout>());
                    Table.SequenceLayoutCalculate(list);
                    this._ColumnsSequence = list;
                }
                return this._ColumnsSequence;
            }
        }
        /// <summary>
        /// Resetuje kolekci ColumnsSequence (=donutí ji znovu se načíst).
        /// Má se volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// </summary>
        protected void ColumnsSequenceReset() { this._ColumnsSequence = null; }
        /// <summary>
        /// Cache kolekce ColumnsSequence
        /// </summary>
        private List<ISequenceLayout> _ColumnsSequence;
        /// <summary>
        /// Eventhandler volaný při/po změně hodnoty na vodorovném scrollbaru = posuny sloupců
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnsScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.ColumnsScrollBar.Value.Begin.Value;
            if (offset == this._ColumnsPositions.DataFirstPixel) return;
            this._ColumnsPositions.DataFirstPixel = offset;
            this.RecalcTables();
            this.RepaintAllItems = true;
        }
        /// <summary>
        /// Horizontal Scrollbar for Columns shift
        /// </summary>
        protected GScrollBar ColumnsScrollBar;
        #endregion
        #region Pozicování svislé = tabulky a vpravo svislý scrollbar
        private void InitTablesPositions()
        {
            this._TablesPositions = new GPosition(0, this._TablePositionGetVisualSize, this._TablePositionGetDataSize);

            this.TablesScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this.TablesScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(TablesScrollBar_ValueChange);
            this.TablesScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(TablesScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídíci prvek pro Pozice sloupců
        /// </summary>
        protected GPosition TablesPositions { get { return this._TablesPositions; } }
        private GPosition _TablesPositions;
        /// <summary>
        /// Vrací výšku prostoru pro tabulky (=celý prostor this.ClientSize.Height)
        /// </summary>
        /// <returns></returns>
        private int _TablePositionGetVisualSize()
        {
            return this.ClientSize.Height;
        }
        /// <summary>
        /// Vrací výšku všech zobrazitelných tabulek
        /// </summary>
        /// <returns></returns>
        private int _TablePositionGetDataSize()
        {
            List<ISequenceLayout> list = this.TableSequence;
            int count = list.Count;
            return (count > 0 ? list[count - 1].End : 0);
        }
        /// <summary>
        /// Soupis sloupců master tabulky, vždy setříděný v pořadí podle ColumnOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou ColumnsSequenceReset(), ta se má volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky nebo Master tabulka nemá žádné sloupce).
        /// </summary>
        protected List<ISequenceLayout> TableSequence
        {
            get
            {
                if (this._TableSequence == null)
                {
                    List<Table> tables = new List<Table>();
                    if (this._Tables != null && this._Tables.Count > 0)
                        tables.AddRange(this._Tables.Select(g => g.DataTable));
                    if (tables.Count > 1)
                        tables.Sort(Table.CompareOrder);

                    List<Data.New.ISequenceLayout> list = new List<ISequenceLayout>(tables.Cast<ISequenceLayout>());
                    Table.SequenceLayoutCalculate(list);
                    this._TableSequence = list;
                }
                return this._TableSequence;
            }
        }
        /// <summary>
        /// Resetuje kolekci ColumnsSequence (=donutí ji znovu se načíst).
        /// Má se volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// </summary>
        protected void TableSequenceReset() { this._TableSequence = null; }
        /// <summary>
        /// Cache kolekce ColumnsSequence
        /// </summary>
        private List<ISequenceLayout> _TableSequence;
        /// <summary>
        /// Eventhandler for Vertical splitter value changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TablesScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.TablesScrollBar.Value.Begin.Value;
            if (offset == this._TablesPositions.DataFirstPixel) return;
            this._TablesPositions.DataFirstPixel = offset;
            this.RecalcTables();
            this.RepaintToLayers = GInteractiveDrawLayer.Standard;
        }
        /// <summary>
        /// Vertical Scrollbar for Tables (no its Rows!) shift
        /// </summary>
        protected GScrollBar TablesScrollBar;
        #endregion
        #region Tabulky - soupis GTable
        #region Veřejné rozhraní
        /// <summary>
        /// Přidá danou datovou tabulku do tohoto gridu.
        /// Vrací grafický objekt právě přidané tabulky.
        /// </summary>
        /// <param name="dTable"></param>
        /// <returns></returns>
        public GTable AddTable(Table dTable)
        {
            return this._AddTable(dTable);
        }
        /// <summary>
        /// Danou datovou tabulku odebere z this gridu.
        /// </summary>
        /// <param name="dTable"></param>
        public void RemoveTable(Table dTable)
        {
            this._RemoveTable(dTable);
        }
        /// <summary>
        /// Soupis všech datových tabulek v gridu
        /// </summary>
        public IEnumerable<Table> DataTables { get { return this._Tables.Select(t => t.DataTable); } }
        /// <summary>
        /// Soupis všech grafických tabulek v gridu
        /// </summary>
        internal IEnumerable<GTable> Tables { get { return this._Tables; } }
        /// <summary>
        /// Public event vyvolaný po přidání nové tabulky do gridu. Grid je již v tabulce umístěn, grid je uveden v argumentu.
        /// </summary>
        public event EList<GTable>.EListEventAfterHandler TableAddAfter;
        /// <summary>
        /// Public event vyvolaný po odebrání řádku z tabulky. Řádek již v tabulce není umístěn, řádek je uveden v argumentu.
        /// </summary>
        public event EList<GTable>.EListEventAfterHandler TableRemoveAfter;
        /// <summary>
        /// Počet tabulek v gridu
        /// </summary>
        public int TablesCount { get { return this._Tables.Count; } }
        #endregion
        #region Privátní obsluha
        /// <summary>
        /// Inicializuje pole tabulek
        /// </summary>
        protected void InitTablesData()
        {
            this._Tables = new EList<GTable>();
            this._Tables.ItemAddAfter += _TableAddAfter;
            this._Tables.ItemRemoveAfter += _TableRemoveAfter;
            this._TableID = 0;
        }
        /// <summary>
        /// Přepočítá souřadnice pro umístění jednotlivých tabulek, na základě souřadnic prostoru tabulek _GridTablesBounds
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcTables()
        {
            ProcessAction actions = ProcessAction.None;
            EventSourceType eventSource = EventSourceType.ApplicationCode;
            this.RecalcTables(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá souřadnice pro umístění jednotlivých tabulek, na základě souřadnic prostoru tabulek _GridTablesBounds
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
        /// Přidá danou datovou tabulku do tohoto gridu
        /// </summary>
        /// <param name="dTable"></param>
        /// <returns></returns>
        protected GTable _AddTable(Table dTable)
        {
            if (dTable == null) return null;
            GTable gTable = new GTable(this, dTable);
            this._Tables.Add(gTable);                      // Instance EList zajistí vyvolání eventhandleru this._TableAddAfter(), tam se GTable napojí na this GGrid a dostane svoje ID.
            return gTable;
        }
        /// <summary>
        /// Odebere danou datovou tabulku z tohoto gridu
        /// </summary>
        /// <param name="dTable"></param>
        /// <returns></returns>
        protected void _RemoveTable(Table dTable)
        {
            if (dTable == null) return;
            this._Tables.RemoveAll(g => Object.ReferenceEquals(g.DataTable, dTable));
        }
        /// <summary>
        /// Handler události, kdy byla přidána další tabulka do tohoto gridu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TableAddAfter(object sender, EList<GTable>.EListAfterEventArgs args)
        {
            this.TableAdded(args);
            this.OnTableAddAfter(args);
            if (this.TableAddAfter != null)
                this.TableAddAfter(this, args);
        }
        /// <summary>
        /// Akce po přidání GTable do GGridu: napojí tabulku na Grid, přiřadí ID
        /// </summary>
        /// <param name="args"></param>
        protected void TableAdded(EList<GTable>.EListAfterEventArgs args)
        {
            GTable table = args.Item;
            int id = this._TableID++;
            ((IGridMember)table).AttachToGrid(this, id);
            this.ColumnsSequenceReset();
            this.TableSequenceReset();
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu přidání tabulky, tabulka je platná, event TableAddAfter ještě neproběhl. V GGrid je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTableAddAfter(EList<GTable>.EListAfterEventArgs args) { }
        /// <summary>
        /// Handler eventu event Tables.ItemRemoveAfter, vyvolá se po odebrání objektu (řádku) z kolekce Rows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TableRemoveAfter(object sender, EList<GTable>.EListAfterEventArgs args)
        {
            this.TableRemoved(args);
            this.OnTableRemoveAfter(args);
            if (this.TableRemoveAfter != null)
                this.TableRemoveAfter(this, args);
        }
        /// <summary>
        /// Akce po odebrání tabulky z gridu: odpojí tabulku od gridu
        /// </summary>
        /// <param name="args"></param>
        protected void TableRemoved(EList<GTable>.EListAfterEventArgs args)
        {
            ((IGridMember)args.Item).DetachFromGrid();
            this.ColumnsSequenceReset();
            this.TableSequenceReset();
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu odebrání řádku, řádek je platný, event RowRemoveAfter ještě neproběhl. V Table je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTableRemoveAfter(EList<GTable>.EListAfterEventArgs args) { }
        /// <summary>
        /// ID pro příští vkládanou GTable
        /// </summary>
        private int _TableID = 0;
        /// <summary>
        /// Fyzické úložiště tabulek GTable
        /// </summary>
        private EList<GTable> _Tables;
        #endregion
        #endregion
        #region ScrollBarX, ScrollBarY
        /// <summary>
        /// Inicializace Scrollbarů X a Y. Volat až po inicializaci Positions.
        /// </summary>
        protected void InitScrollBars()
        {
            ProcessAction actions = ProcessAction.None;
            this.RecalcScrollBars(ref actions, EventSourceType.ApplicationCode);
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
                using (this.ColumnsScrollBar.SuppressEvents())
                {
                    this.ColumnsScrollBar.Bounds = bounds;
                    this.ColumnsScrollBar.ValueTotal = new SizeRange(0, total);
                    this.ColumnsScrollBar.Value = new SizeRange(offset, offset + visual);
                    this.ColumnsScrollBar.IsEnabled = (total > visual);
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
                using (this.TablesScrollBar.SuppressEvents())
                {
                    this.TablesScrollBar.Bounds = bounds;
                    this.TablesScrollBar.ValueTotal = new SizeRange(0, total);
                    this.TablesScrollBar.Value = new SizeRange(offset, offset + visual);
                    this.TablesScrollBar.IsEnabled = (total > visual);
                }
            }
        }
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
                this.ChildList.Add(this.TablesScrollBar);
            if (this.Positions.ColumnsCount > 0)
                this.ChildList.Add(this.ColumnsScrollBar);

            this._ChildArrayValid = true;
        }
        private bool _ChildArrayValid;
        #endregion
        #region Podpora pro časové osy v synchronizovaných sloupcích
        /// <summary>
        /// Vyvolá RefreshTimeAxis pro všechny GTable, předá jim ID sloupce pro refresh.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce (je volána z handleru Column._TimeAxis_ValueChange)
        /// </summary>
        /// <param name="columnId"></param>
        internal void RefreshTimeAxis(int columnId)
        {
            foreach (GTable gTable in this._Tables)
                gTable.RefreshTimeAxis(columnId);
        }
        #endregion


        #region Public properties
        protected void InitProperties()
        {
            this.ScrollBarVerticalWidth = GScrollBar.DefaultSystemBarWidth;
            this.ScrollBarHorizontalHeight = GScrollBar.DefaultSystemBarHeight;
        }
        /// <summary>
        /// Šířka svislého Scrollbaru (ten voravo, co posouvá tabulky - když je potřeba).
        /// </summary>
        public int ScrollBarVerticalWidth { get { return this._ScrollBarVerticalWidth; } set { this._ScrollBarVerticalWidth = (value < 0 ? 0 : value); this._GridLayoutValid = false; } } private int _ScrollBarVerticalWidth;
        /// <summary>
        /// Výška vodorovného Scrollbaru (ten dole, co posouvá sloupce).
        /// </summary>
        public int ScrollBarHorizontalHeight { get { return this._ScrollBarHorizontalHeight; } set { this._ScrollBarHorizontalHeight = (value < 0 ? 0 : value); this._GridLayoutValid = false; } } private int _ScrollBarHorizontalHeight;
        #endregion



        
        #region Columns and Positions
     
     
        /// <summary>
        /// Default width for RowHeader column
        /// </summary>
        public static int DefaultRowHeaderWidth { get { return 20; } }
        #endregion
        
        #region Refresh
        public override void Refresh()
        {
            this.Refresh(GridRefreshStyle.All, null);
        }
        public void Refresh(Table table)
        {
            this.Refresh(GridRefreshStyle.All, table);
        }
        protected void Refresh(GridRefreshStyle refreshStyle, Table dTable)
        {
            if ((refreshStyle & GridRefreshStyle.ReloadColumns) != 0)
                this.ReloadColumns(false);
            if ((refreshStyle & GridRefreshStyle.ReloadRows) != 0)
                this.RefreshRows(dTable);
            base.Refresh();
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
    }
    /// <summary>
    /// Třída, která řeší zobrazení obsahu prvku typicky v Gridu.
    /// Daná oblast má záhlaví určité velikosti, jehož umístění se pohybem Scrollbaru nemění,
    /// a za tímto záhlavím má obsah, spojený se Scrollbarem, kde tento obsah je promítán do disponibilního prostoru.
    /// Tato třída eviduje: pozici (velikost) záhlaví, velikost prostoru pro data, počáteční logickou pozici dat (od kterého pixelu jsou data viditelná),
    /// a provádí převody viditelných pixelů na pixely virtuální = datové.
    /// </summary>
    public class GPosition : IScrollBarData
    {
        #region Základní data
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="getVisualSizeMethod"></param>
        /// <param name="getDataSizeMethod"></param>
        internal GPosition(int firstPixel, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
        {
            this.VisualFirstPixel = firstPixel;
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
            this._DataSizeAddSpace = 26;
        }
        /// <summary>
        /// Funkce, která vrátí velikost prostoru = počet pixelů, které jsou nyní viditelné (celkově = header + prostor pro data = ClientBounds.Width nebo Height)
        /// </summary>
        private Func<int> _GetVisualSizeMethod;
        /// <summary>
        /// Funkce, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou
        /// </summary>
        private Func<int> _GetDataSizeMethod;
        /// <summary>
        /// Číslo vizuálního pixelu, na kterém začíná zobrazení dat. Pixely před tímto jsou obsazeny něčím jiným (typicky Header)
        /// </summary>
        public int VisualFirstPixel { get; set; }
        /// <summary>
        /// Celková velikost viditelná (=prostor v controlu = ClientBounds.Width nebo Height, v němž je zobrazován obsah).
        /// jde o čistou velikost prostoru, v němž se zobrazují data.
        /// </summary>
        public int VisualSize { get { return this._GetVisualSizeMethod(); } }
        /// <summary>
        /// Číslo prvního datového pixelu, který je zobrazen na prvním vizuálním pixelu (VisualFirstPixel).
        /// Datové pixely před tímto pixelem nejsou vidět, protože jsou odscrollované nad nebo vlevo.
        /// </summary>
        public int DataFirstPixel { get; set; }
        /// <summary>
        /// Celková velikost zobrazovaných dat (=např. součet výšky všech zobrazitelných řádků)
        /// </summary>
        public int DataSize { get { return this._GetDataSizeMethod(); } }
        /// <summary>
        /// true pokud má být zobrazen ScrollBar (respektive má být Enabled) : pokud DataSize je větší než VisualSize
        /// </summary>
        public bool IsScrollBarActive { get { return (this.DataSize > this.VisualSize); } }
        /// <summary>
        /// Počet přidaných pixelů za datovou oblastí, které se zobrazí pod / za daty, když ScrollBar dojede na konec rozsahu.
        /// Je to prázdné místo, které uživateli signalizuje "že dál nic není".
        /// Default = 26 pixelů.
        /// </summary>
        public int DataSizeAddSpace { get { return this._DataSizeAddSpace; } set { this._DataSizeAddSpace = value; } } private int _DataSizeAddSpace;
        #endregion
        #region Filtrování datových prvků podle aktuální viditelné oblasti a podle umístění datových prvků (ISequenceLayout)
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool FilterVisibleItem(ISequenceLayout item)
        {
            int dataBegin = this.DataFirstPixel;
            int dataEnd = this.DataEndPixel;
            return this._FilterVisibleItem(item, dataBegin, dataEnd);
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items) where T : ISequenceLayout
        {
            int dataBegin = this.DataFirstPixel;
            int dataEnd = this.DataEndPixel;
            return items.Where(i => this._FilterVisibleItem(i, dataBegin, dataEnd));
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dataBegin"></param>
        /// <param name="dataEnd"></param>
        /// <returns></returns>
        private bool _FilterVisibleItem(ISequenceLayout item, int dataBegin, int dataEnd)
        {
            return (item.Size > 0 && item.Begin < dataEnd && item.End > dataBegin);
        }
        /// <summary>
        /// Číslo datového pixelu, který je při současném zobrazení End = první za viditelným prostorem (= DataFirstPixel + DataVisibleSize)
        /// </summary>
        protected int DataEndPixel { get { return (this.DataFirstPixel + this.VisualSize); } }
        #endregion
        #region IScrollBarData
        int IScrollBarData.DataBegin { get { return this.DataFirstPixel; } }
        int IScrollBarData.DataSize { get { return this.DataSize; } }
        int IScrollBarData.VisualSize { get { return this.VisualSize; } }
        int IScrollBarData.DataSizeAddSpace { get { return this.DataSizeAddSpace; } }
        #endregion
    }
}
