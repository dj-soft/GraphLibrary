using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;
using Djs.Common.Data.New;
using Djs.Common.Components.Grid;

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
            this.InitTablesData();
            this.InitTablesPositions();
            this.InitColumnsPositions();
        }
        #endregion
        #region Rozmístění vnitřních prvků gridu - souřadnice pro prostor tabulek a scrollbarů
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
            this.RecalcTableScrollBar(ref actions, eventSource);
            this.RecalcColumnScrollBar(ref actions, eventSource);
        }
        /// <summary>
        /// Přepočítá pozice ClientAreas = prostor tabulek a ScrollBarů.
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        private void RecalcClientAreas(ref ProcessAction actions, EventSourceType eventSource)
        {
            Size clientSize = this.ClientSize;
            if (this.ClientSizeLast == clientSize) return;

            this.GridLayoutValid = false;
            if (clientSize.Width <= 0 || clientSize.Height <= 0) return;

            // Určíme, zda bude zobrazen scrollbar vpravo (to je tehdy, když výška tabulek je větší než výška prostoru pro tabulky = (ClientSize.Height - ColumnsScrollBar.Bounds.Height)):
            //  Objekt TablesPositions tady provede dotaz na velikost dat (metoda this._TablesPositionGetDataSize()) a velikost viditelného prostoru (metoda this._TablesPositionGetVisualSize()).
            //  Velikost dat je dána tabulkami, resp. pozici End poslední tabulky v sekvenci this.TablesSequence, 
            //  velikost viditelného prostoru pro tabulky je dána (this.ClientSize.Height - this.ColumnsScrollBar.Bounds.Height), takže se vždy počítá s prostorem pro zobrazením vodorovného scrollbaru:
            this.TablesScrollBarVisible = this.TablesPositions.IsScrollBarActive;

            // Určíme, zda bude zobrazen scrollbar dole (to je tehdy, když šířka datových sloupců tabulek je větší než prostor od RowHeaderColumn do pravého Scrollbaru, pokud je zobrazen):
            //  Objekt ColumnsPositions si data zjistí sám, poocí zdejších metod this._ColumnPositionGetVisualSize() a this._ColumnPositionGetDataSize()
            this.ColumnsScrollBarVisible = this.ColumnsPositions.IsScrollBarActive;

            // Určíme souřadnice jednotlivých elementů:
            int x = 0;
            int y = 0;
            int w = clientSize.Width;
            int h = clientSize.Height;

            int rhw = this.ColumnsPositions.VisualFirstPixel;
            int sbw = GScrollBar.DefaultSystemBarWidth;
            int sbh = GScrollBar.DefaultSystemBarHeight;

            if (this.TablesScrollBarVisible)
            {
                this.TablesScrollBarBounds = new Rectangle(w - sbw, y, sbw, h - sbh);
                w -= sbw;
            }

            if (this.ColumnsScrollBarVisible)
            {
                this.ColumnsScrollBarBounds = new Rectangle(rhw, h - sbh, w - rhw, sbh);
                h -= sbh;
            }

            this.GridTablesBounds = new Rectangle(x, y, w, h);

            // Zapamatovat clientSize, abychom příští aktivitu prováděli jen po změně:
            this.ClientSizeLast = clientSize;
            this.GridLayoutValid = true;
        }
        /// <summary>
        /// Hodnota ClientSize, pro kterou byly naposledy přepočteny pozice vnitřních objektů (GridTablesBounds, ColumnsScrollBarBounds, TablesScrollBarBounds).
        /// Další přepočet se provede jen po změně.
        /// </summary>
        protected Size ClientSizeLast;
        /// <summary>
        /// true pokud se má zobrazovat svislý scrollbar (pro tabulky, vpravo)
        /// </summary>
        private bool TablesScrollBarVisible;
        /// <summary>
        /// Prostor pro svislý scrollbar (pro tabulky, vpravo)
        /// </summary>
        protected Rectangle TablesScrollBarBounds;
        /// <summary>
        /// true pokud se má zobrazovat vodorovný scrollbar (pro sloupce, dole)
        /// </summary>
        protected bool ColumnsScrollBarVisible;
        /// <summary>
        /// Prostor pro vodorovný scrollbar (pro sloupce, dole)
        /// </summary>
        protected Rectangle ColumnsScrollBarBounds;
        /// <summary>
        /// Prostor pro tabulky (hlavní prostor), neobsahuje pozice scrollbarů X a Y
        /// </summary>
        protected Rectangle GridTablesBounds;
        /// <summary>
        /// true pokud je layout vnitřních prostor gridu korektně spočítán
        /// </summary>
        protected bool GridLayoutValid;
        #endregion
        #region Pozicování svislé - tabulky a vpravo svislý scrollbar
        /// <summary>
        /// Inicializace objektů pro pozicování tabulek: TablesPositions, TablesScrollBar
        /// </summary>
        private void InitTablesPositions()
        {
            this.TablesPositions = new GPosition(0, this._TablesPositionGetVisualSize, this._TablesPositionGetDataSize);

            this.TablesScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this.TablesScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(TablesScrollBar_ValueChange);
            this.TablesScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(TablesScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídíci prvek pro Pozice tabulek
        /// </summary>
        protected GPosition TablesPositions;
        /// <summary>
        /// Vrací výšku prostoru pro tabulky (=prostor this.ClientSize.Height mínus dole prostor pro vodorovný scrollbar sloupců)
        /// </summary>
        /// <returns></returns>
        private int _TablesPositionGetVisualSize()
        {
            return this.ClientSize.Height - GScrollBar.DefaultSystemBarHeight;
        }
        /// <summary>
        /// Vrací výšku všech zobrazitelných tabulek (z this.TableSequence, z jejího posledního prvku, z jeho property End)
        /// </summary>
        /// <returns></returns>
        private int _TablesPositionGetDataSize()
        {
            ISequenceLayout[] list = this.TablesSequence;
            int count = list.Length;
            return (count > 0 ? list[count - 1].End : 0);
        }
        /// <summary>
        /// Soupis všech grafických objektů tabulek, setříděný podle TableOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou TableSequenceReset(), ta se má volat po těchto akcích:
        /// Změna pořadí tabulek, Změna počtu tabulek.
        /// Nemusí se volat při posunech svislého scrollbaru ani při resize gridu, ani při změně výšky tabulek!
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky).
        /// </summary>
        protected ISequenceLayout[] TablesSequence
        {
            get
            {
                if (this._TablesSequence == null)
                {
                    List<GTable> tables = new List<GTable>();
                    if (this._Tables != null && this._Tables.Count > 0)
                        tables.AddRange(this._Tables);
                    if (tables.Count > 1)
                        tables.Sort(GTable.CompareOrder);

                    ISequenceLayout[] array = tables.Cast<ISequenceLayout>().ToArray();
                    SequenceLayout.SequenceLayoutCalculate(array);
                    this._TablesSequence = array;
                }
                return this._TablesSequence;
            }
        }
        /// <summary>
        /// Cache kolekce TableSequence
        /// </summary>
        private ISequenceLayout[] _TablesSequence;
        /// <summary>
        /// Resetuje kolekci ColumnsSequence (=donutí ji znovu se načíst).
        /// Má se volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// </summary>
        protected void TableSequenceReset() { this._TablesSequence = null; }
        /// <summary>
        /// Eventhandler pro událost změny pozice svislého scrollbaru = posun pole tabulek nahoru/dolů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void TablesScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.TablesScrollBar.Value.Begin.Value;
            if (offset == this.TablesPositions.DataFirstPixel) return;
            this.TablesPositions.DataFirstPixel = offset;
            this.RecalcTables();
            this.RepaintToLayers = GInteractiveDrawLayer.Standard;
        }
        /// <summary>
        /// Zajistí vložení všech patřičných hodnot do scrollbaru tabulek.
        /// Tato akce nevyvolá žádný event.
        /// Aktualizují se hodnoty ColumnsScrollBar: Bounds, ValueTotal, Value, IsEnabled
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcTableScrollBar(ref ProcessAction actions, EventSourceType eventSource)
        {
            if (this.TablesScrollBarVisible)
                this.TablesScrollBar.LoadFrom(this.TablesPositions, this.TablesScrollBarBounds, true);
        }
        /// <summary>
        /// Svislý Scrollbar pro posouvání pole tabulek nahoru/dolů (nikoli jejich řádků, na to má každá tabulka svůj vlastní Scrollbar).
        /// </summary>
        protected GScrollBar TablesScrollBar;
        #endregion
        #region Pozicování vodorovné - sloupce tabulek a dolní vodorovný scrollbar
        private void InitColumnsPositions()
        {
            this.ColumnsPositions = new GPosition(GGrid.DefaultRowHeaderWidth, 28, this._ColumnPositionGetVisualSize, this._ColumnPositionGetDataSize);

            this.ColumnsScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            this.ColumnsScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
            this.ColumnsScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídící prvek pro Pozice sloupců
        /// </summary>
        protected GPosition ColumnsPositions;
        /// <summary>
        /// Vrací šířku vizuálního prostoru pro datové sloupce (= ClientSize.Width - ColumnsPositions.VisualFirstPixel - [šířka TablesScrollBar, pokud je viditelný])
        /// </summary>
        /// <returns></returns>
        private int _ColumnPositionGetVisualSize()
        {
            return this.ClientSize.Width - this.ColumnsPositions.VisualFirstPixel - (this.TablesScrollBarVisible ? GScrollBar.DefaultSystemBarWidth : 0);
        }
        /// <summary>
        /// Vrací šířku všech zobrazitelných datových sloupců, vyjma sloupec RowHeader (to není datový sloupec).
        /// </summary>
        /// <returns></returns>
        private int _ColumnPositionGetDataSize()
        {
            ISequenceLayout[] array = this.ColumnsSequence;
            int count = array.Length;
            return (count > 0 ? array[count - 1].End : 0);
        }
        /// <summary>
        /// Soupis sloupců master tabulky, vždy setříděný v pořadí podle ColumnOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou ColumnsSequenceReset(), ta se má volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky nebo Master tabulka nemá žádné sloupce).
        /// </summary>
        protected ISequenceLayout[] ColumnsSequence
        {
            get
            {
                if (this._ColumnsSequence == null)
                {
                    List<Column> columns = new List<Column>();
                    if (this._Tables != null && this._Tables.Count > 0)
                        columns.AddRange(this._Tables[0].DataTable.Columns.Where(c => c.Visible));
                    if (columns.Count > 1)
                        columns.Sort(Column.CompareOrder);

                    ISequenceLayout[] array = columns.Cast<ISequenceLayout>().ToArray();
                    SequenceLayout.SequenceLayoutCalculate(array);
                    this._ColumnsSequence = array;
                }
                return this._ColumnsSequence;
            }
        }
        /// <summary>
        /// Cache kolekce ColumnsSequence
        /// </summary>
        private ISequenceLayout[] _ColumnsSequence;
        /// <summary>
        /// Resetuje kolekci ColumnsSequence (=donutí ji znovu se načíst).
        /// Má se volat po těchto akcích:
        /// Změna pořadí sloupců, Změna počtu sloupců.
        /// Nemusí se volat při posunech vodorovného scrollbaru ani při resize gridu, ani při změně šířky sloupců!
        /// </summary>
        protected void ColumnsSequenceReset() { this._ColumnsSequence = null; this._MainTimeAxisReset(); }
        /// <summary>
        /// Eventhandler volaný při/po změně hodnoty na vodorovném scrollbaru = posuny sloupců
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ColumnsScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.ColumnsScrollBar.Value.Begin.Value;
            if (offset == this.ColumnsPositions.DataFirstPixel) return;
            this.ColumnsPositions.DataFirstPixel = offset;
            this.RecalcTables();
            this.RepaintAllItems = true;
        }
        /// <summary>
        /// Zajistí vložení všech patřičných hodnot do scrollbaru sloupců.
        /// Tato akce nevyvolá žádný event.
        /// Aktualizují se hodnoty ColumnsScrollBar: Bounds, ValueTotal, Value, IsEnabled
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcColumnScrollBar(ref ProcessAction actions, EventSourceType eventSource)
        {
            if (this.ColumnsScrollBarVisible)
                this.ColumnsScrollBar.LoadFrom(this.ColumnsPositions, this.ColumnsScrollBarBounds, true);
        }
        /// <summary>
        /// Dolní vodorovný Scrollbar, pro posuny sloupců doleva/doprava
        /// </summary>
        protected GScrollBar ColumnsScrollBar;
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
        /// Inicializuje pole tabulek (this.Tables), určené pro práci s daty. Nic dalšího neřeší (žádné pozicování).
        /// </summary>
        protected void InitTablesData()
        {
            this._Tables = new EList<GTable>();
            this._Tables.ItemAddAfter += _TableAddAfter;
            this._Tables.ItemRemoveAfter += _TableRemoveAfter;
            this._TableID = 0;
        }
        /// <summary>
        /// Přepočítá souřadnice pro umístění jednotlivých tabulek (=jejich Bound), na základě souřadnic prostoru tabulek GridTablesBounds
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
        /// Přepočítá souřadnice pro umístění jednotlivých tabulek (=jejich Bound), na základě souřadnic prostoru tabulek GridTablesBounds
        /// </summary>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected void RecalcTables(ref ProcessAction actions, EventSourceType eventSource)
        {
            Rectangle tablesBounds = this.GridTablesBounds;
            foreach (ISequenceLayout isl in this.TablesSequence)
            {   // Procházím tabulky jako ISequenceLayout, určím jejich souřadnice podle ISequenceLayout.Begin a Size (=svislá pozice) + pozice prostoru tabulek (X a Width):
                int y = this.TablesPositions.GetVisualPosition(isl.Begin);
                Rectangle bound = new Rectangle(tablesBounds.X, y, tablesBounds.Width, isl.Size);
                GTable gTable = isl as GTable;
                if (gTable != null)
                    gTable.SetBounds(bound, actions, eventSource);
            }
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
            this.ChildsReset();
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
            this.ChildsReset();
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
        #region Pole grafických prvků Childs - obsahuje všechny tabulky, jejich vzájemné oddělovače (Splitter), a scrollbary (sloupce vždy, tabulky podle potřeby)
        /// <summary>
        /// Pole grafických prvků v tomto gridu: tabulky, splittery (mezi tabulkami), scrollbary (svislý, vodorovný)
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.ChildArrayCheck(); return this.ChildList; } }
        /// <summary>
        /// Invaliduje pole Child.
        /// Je nutno zavolat, po každé změně počtu tabulek.
        /// </summary>
        protected void ChildsReset()
        {
            this._ChildsIsValid = false;
        }
        /// <summary>
        /// Check this.GridItems: when is null, then call this.GridItemsReload()
        /// </summary>
        protected void ChildArrayCheck()
        {
            if (!this._ChildsIsValid)
                this.ChildArrayReload();
        }
        /// <summary>
        /// Reload all current items for this Grid into .
        /// Add this items: this._Tables, 
        /// </summary>
        protected void ChildArrayReload()
        {
            this.ChildList.Clear();
            if (!this.GridLayoutValid) return;

            Rectangle bounds = this.GridTablesBounds;

            // Nejprve přidáme tabulky, a to jen ty které jsou viditelné:
            // Současně si nastřádáme jejich splittery:
            List<IInteractiveItem> splitterList = new List<IInteractiveItem>();
            foreach (ISequenceLayout isl in this.TablesSequence)
            {   // Procházím tabulky ze seznamu ISequenceLayout, a pokud tabulka je viditelná, pak ji zpracuji:
                GTable table = isl as GTable;
                bool isVisible = (Rectangle.Intersect(bounds, table.Bounds).Height > 0);
                if (isVisible)
                {
                    this.ChildList.Add(isl as IInteractiveItem);
                    splitterList.Add((isl as GTable).TableSplitter);
                }
            }
            // Pokud máme nastřádán více než 1 table-splitter, pak přidáme splitery krom posledního:
            if (splitterList.Count > 1)
                this.ChildList.AddRange(splitterList.GetRange(0, splitterList.Count - 1));

            // Pokud jsou viditelné scrollbary, přidáme je:
            if (this.ColumnsScrollBarVisible)
                this.ChildList.Add(this.ColumnsScrollBar);
            if (this.TablesScrollBarVisible)
                this.ChildList.Add(this.TablesScrollBar);

            this._ChildsIsValid = true;
        }
        private bool _ChildsIsValid;
        #endregion
        #region Hlavní časová osa, podpora pro časové osy v synchronizovaných sloupcích
        internal ITimeConvertor MainTimeAxis
        {
            get
            {
                if (!this._MainTimeAxisValid)
                {
                    Column timeAxisColumn = this.ColumnsSequence.Cast<Column>().FirstOrDefault(c => c.UseTimeAxis);
                    if (timeAxisColumn != null)
                    {
                        this._MainTimeAxis = timeAxisColumn.TimeAxis;
                        this._MainTimeAxisValid = true;
                    }
                }
                return this._MainTimeAxis;
            }
        }
        /// <summary>
        /// true pokud existuje hlavní časová osa = objekt v this.MainTimeAxis.
        /// Hlavní časová osa je časová osa z prvního sloupce, který zobrazuje časovou osu.
        /// </summary>
        internal bool MainTimeAxisExists { get; private set; }
        private void _MainTimeAxisReset()
        {
            this._MainTimeAxis = null;
            this._MainTimeAxisValid = false;
        }
        /// <summary>
        /// Úložiště pro hlavní časovou osu
        /// </summary>
        private ITimeConvertor _MainTimeAxis;
        /// <summary>
        /// true pokud hlavní časová osa byla vyhledána (může být i nenalezena == null, ale i to je platný výsledek, hlavní časová osa není povinná)
        /// </summary>
        private bool _MainTimeAxisValid;
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
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            // GGrid sám o sobě není interaktivní. Interaktivní jsou jeho Childs.
        }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
            // GGrid sám o sobě se nevykresluje, leda v situaci kdy nemá žádnou tabulku:
            if (this.TablesCount == 0)
                base.Draw(e);
        }
        #endregion
        #region Defaultní hodnoty
        /// <summary>
        /// Výchozí šířka sloupce RowHeader
        /// </summary>
        public static int DefaultRowHeaderWidth { get { return 32; } }
        #endregion
    }
    #region class GPosition : Třída, která řeší zobrazení obsahu prvku typicky v Gridu
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
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
        {
            this.VisualFirstPixel = 0;
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
            this._DataSizeAddSpace = 26;
        }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(int firstPixel, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
        {
            this.VisualFirstPixel = firstPixel;
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
            this._DataSizeAddSpace = 26;
        }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="dataSizeAddSpace">Přídavek k hodnotě DataSize (=celková velikost dat) v pixelech, který bude předáván do ScrollBaru.</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(int firstPixel, int dataSizeAddSpace, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
        {
            this.VisualFirstPixel = firstPixel;
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
            this._DataSizeAddSpace = dataSizeAddSpace;
        }
        /// <summary>
        /// Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data.
        /// Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.
        /// </summary>
        private Func<int> _GetVisualSizeMethod;
        /// <summary>
        /// Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.
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
        /// Číslo datového pixelu, který je při současném zobrazení End = první za viditelným prostorem (= DataFirstPixel + DataVisibleSize)
        /// </summary>
        protected int DataEndPixel { get { return (this.DataFirstPixel + this.VisualSize); } }
        /// <summary>
        /// Rozmezí datových pixelů, které spadají do viditelné oblasti.
        /// Vrácená hodnota má Begin i End naplněné (HasValue).
        /// Begin = DataFirstPixel, End = DataFirstPixel + VisualSize.
        /// </summary>
        public Int32Range DataRange { get { return new Int32Range(this.DataFirstPixel, this.DataEndPixel); } }
        /// <summary>
        /// true pokud má být zobrazen ScrollBar (respektive má být Enabled) : pokud DataSize je větší než VisualSize
        /// </summary>
        public bool IsScrollBarActive { get { return (this.DataSize > this.VisualSize); } }
        /// <summary>
        /// Přídavek k hodnotě DataSize (=celková velikost dat) v pixelech, který bude předáván do ScrollBaru.
        /// Důvod tohoto přídavku: při čistých výpočtech (bez přídavku) bude datový obsah scrollován zcela přesně do vizuálního prostoru, 
        /// a když Scrollbar dojede na konec prostoru (dolů/doprava), pak obsah bude zobrazen zcela přesně (dole/vpravo bude poslední pixel obsahu).
        /// To je sice matematicky správně, ale vizuálně (ergonomicky) není zřejmé, že "dál už nic není".
        /// Proto se běžně scrollbar chová tak, že při odscrollování na konec se "za koncem dat" zobrazí několik pixelů prázdného prostoru.
        /// Defaultně se zobrazuje 45 pixelů.
        /// Hodnota 0 pak zobrazuje "matematicky správně".
        /// Záporná hodnota v DataSizeOverhead zařídí, že daný bude zobrazen počet pixelů dat nahoře/vlevo:
        /// např. -40 zajistí, že při posunu scrollbaru na konec dráhy se zobrazí nahoře 40 pixelů dat, a celý zbytek bude prázdný.
        /// </summary>
        /// </summary>
        public int DataSizeAddSpace { get { return this._DataSizeAddSpace; } set { this._DataSizeAddSpace = value; } } private int _DataSizeAddSpace;
        #endregion
        /// <summary>
        /// Vrátí vizuální pozici (odpovídající aktuálnímu controlu) pro danou logickou (datovou) pozici.
        /// Vrací tedy danou pozici (dataPosition + VisualFirstPixel - DataFirstPixel).
        /// </summary>
        /// <param name="dataPosition"></param>
        /// <returns></returns>
        public int GetVisualPosition(int dataPosition)
        {
            return (dataPosition + this.VisualFirstPixel - this.DataFirstPixel);
        }
        #region IScrollBarData
        int IScrollBarData.DataBegin { get { return this.DataFirstPixel; } }
        int IScrollBarData.DataSize { get { return this.DataSize; } }
        int IScrollBarData.VisualSize { get { return this.VisualSize; } }
        int IScrollBarData.DataSizeAddSpace { get { return this.DataSizeAddSpace; } }
        #endregion
    }
    #endregion
    #region interface IGridMember : Člen gridu GGrid, u kterého je možno provést Attach a Detach
    /// <summary>
    /// Člen gridu GGrid, u kterého je možno provést Attach a Detach
    /// </summary>
    public interface IGridMember
    {
        /// <summary>
        /// Napojí this objekt do dodaného Gridu a uloží do sebe dané ID
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="id"></param>
        void AttachToGrid(GGrid grid, int id);
        /// <summary>
        /// Odpojí this objekt od navázaného Gridu, resetuje svoje ID
        /// </summary>
        void DetachFromGrid();
    }
    #endregion
}
