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
            this.InitPositions();
            this.InitTables();
            this.InitColumns();
            this.InitScrollBars();
        }
        #endregion
        #region Rozmístění vnitřních prvků, jejich přepočty, layout Gridu
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
            this.RecalcGrid(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá pozice všech prvků Gridu (ClientAreas, Tables, ScrollBars).
        /// Jako action vezme None, jako eventSource = ApplicationCode.
        /// </summary>
        internal void RecalcGrid()
        {
            ProcessAction actions = ProcessAction.None;
            EventSourceType eventSource = EventSourceType.ApplicationCode;
            this.RecalcGrid(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá pozice všech prvků Gridu (ClientAreas, Tables, ScrollBars).
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
        /// Přepočítá pozice ClientAreas = prostor tabulek a ScrollBarů.
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
        protected void InitTables()
        {
            this._Tables = new EList<GTable>();
            this._Tables.ItemAddAfter += _TableAddAfter;
            this._Tables.ItemRemoveAfter += _TableRemoveAfter;
            this._TableID = 0;
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
            this.ColumnLayoutIsValid = false;
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
            this.ColumnLayoutIsValid = false;
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
        #region Rozložení prvků Gridu = pozice sloupců (sloupce v Gridu řídí Master tabulka, jsou synchronizované do všech tabulek) a pozice jednotlivých tabulek
        /// <summary>
        /// Inicializace všech řídících prvků pro pozicování obsahu
        /// </summary>
        private void InitPositions()
        {
            this._ColumnsPositions = new GPosition(this._ColumnPositionGetVisualSize, this._ColumnPositionGetDataSize);
            this._TablesPositions = new GPosition(this._TablePositionGetVisualSize, this._TablePositionGetDataSize);
        }
        /// <summary>
        /// Řídíci prvek pro Pozice sloupců
        /// </summary>
        protected GPosition ColumnsPositions { get { return this._ColumnsPositions; } } private GPosition _ColumnsPositions;
        /// <summary>
        /// Vrací šířku prostoru pro sloupce (=this.ClientSize.Width)
        /// </summary>
        /// <returns></returns>
        private int _ColumnPositionGetVisualSize()
        {
            return this.ClientSize.Width;
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
        /// Řídíci prvek pro Pozice sloupců
        /// </summary>
        protected GPosition TablesPositions { get { return this._TablesPositions; } } private GPosition _TablesPositions;
        /// <summary>
        /// Vrací výšku prostoru pro tabulky (=this.ClientSize.Height)
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

                    List<Data.New.ISequenceLayout> list = new List<ISequenceLayout>(columns.Cast<ISequenceLayout>());
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
        protected void ColumnsSequenceReset() { this._ColumnsSequence = null; }
        /// <summary>
        /// Cache kolekce ColumnsSequence
        /// </summary>
        private List<ISequenceLayout> _ColumnsSequence;







        /// <summary>
        /// Soupis jednotlivých tabulek, vždy setříděný v pořadí podle TableId, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou LayoutYTablesReset(), ta se má volat po těchto akcích:
        /// Změna výšky tabulky, Změna pořadí tabulek (???), Změna počtu tabulek.
        /// Nemusí se volat při posunech svislého scrollbaru, ani při resize gridu.
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky).
        /// </summary>
        protected List<Data.New.ISequenceLayout> LayoutYTables
        {
            get
            {
                if (this._LayoutYTables == null)
                {
                    List<Data.New.ISequenceLayout> list = new List<Data.New.ISequenceLayout>();
                    if (this._Tables != null)
                        list.AddRange(this._Tables.Select(t => t.DataTable).Cast<Data.New.ISequenceLayout>());
                    if (list.Count > 1)
                        list.Sort((a, b) => a.Order.CompareTo(b.Order));
                    Table.SequenceLayoutCalculate(list);
                    this._LayoutYTables = list;
                }
                return this._LayoutYTables;
            }
        }
        /// <summary>
        /// Resetuje kolekci LayoutYTables (=donutí ji znovu se načíst).
        /// Má se volat po těchto akcích:
        /// Změna výšky tabulky, Změna pořadí tabulek (???), Změna počtu tabulek.
        /// Nemusí se volat při posunech svislého scrollbaru, ani při resize gridu.
        /// </summary>
        protected void LayoutYTablesReset() { this._LayoutYTables = null; }
        /// <summary>
        /// Cache kolekce LayoutXMasterColumns
        /// </summary>
        private List<Data.New.ISequenceLayout> _LayoutYTables;
        /// <summary>
        /// Číslo prvního zobrazovaného (=logického, nikoli vizuálního) pixelu v ose Y v prostoru tabulek.
        /// Souvisí s posouváním celého bloku tabulek nahoru/dolu pomocí svislého scrollbaru.
        /// </summary>
        protected int LayoutYTableOffset { get { return this._LayoutYTableOffset; } set { this._LayoutYTableOffset = value; this._LayoutYIsValid = false; } }
        private int _LayoutYTableOffset;
        /// <summary>
        /// Celková výška všech zobrazitelných tabulek = hodnota ISequenceLayout.End z poslední tabulky ze seznamu LayoutYTables.
        /// Jde o logickou hodnotu, která odpovídá celkové velikosti svislého (vpravo) scrollbaru.
        /// </summary>
        protected int LayoutXAllTablesWidth { get { List<Data.New.ISequenceLayout> list = this.LayoutYTables; int cnt = list.Count; return (cnt > 0 ? list[cnt - 1].End : 0); } }

        private bool _LayoutYIsValid;

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




        #region Tables
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
        
        #endregion
        #region Columns and Positions
        /// <summary>
        /// Initialise positions
        /// </summary>
       
        /// <summary>
        /// Positions of all visual items (Columns and Tables)
        /// </summary>
        internal GridPositions xxxPositions
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
        protected void ReloadColumns(Table dataTable, bool force)
        {
            this.Positions.ReloadColumns(dataTable, force);
            this.ChildArrayInvalidate();
        }
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

        private void RefreshRows(Table dTable)
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
    }
    /// <summary>
    /// Třída, která řeší zobrazení obsahu prvku typicky v Gridu.
    /// Daná oblast má záhlaví určité velikosti, jehož umístění se pohybem Scrollbaru nemění,
    /// a za tímto záhlavím má obsah, spojený se Scrollbarem, kde tento obsah je promítán do disponibilního prostoru.
    /// Tato třída eviduje: pozici (velikost) záhlaví, velikost prostoru pro data, počáteční logickou pozici dat (od kterého pixelu jsou data viditelná),
    /// a provádí převody viditelných pixelů na pixely virtuální = datové.
    /// </summary>
    public class GPosition
    {
        internal GPosition(Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
        {
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
        }
        private Func<int> _GetVisualSizeMethod;
        private Func<int> _GetDataSizeMethod;
        /// <summary>
        /// Celková velikost viditelná (=prostor v controlu = ClientBounds.Width nebo Height)
        /// </summary>
        public int VisualTotalSize { get { return this._GetVisualSizeMethod(); } }
        /// <summary>
        /// Velikost záhlaví (výška u záhlaví nahoře, šířka u záhlaví vlevo)
        /// </summary>
        public int HeaderSize { get; set; }
        /// <summary>
        /// Celková velikost zobrazovaných dat (=např. součet výšky všech zobrazitelných řádků)
        /// </summary>
        public int DataTotalSize { get { return this._GetDataSizeMethod(); } }

    }
}
