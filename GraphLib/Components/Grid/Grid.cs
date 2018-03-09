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
            this.GridLayoutValid = false;

            Size clientSize = this.ClientSize;
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
            int x0 = 0;                                                        // x0: úplně vlevo
            int x1 = this.ColumnsPositions.VisualFirstPixel;                   // x1: zde začíná ColumnsScrollBar (hned za koncem RowHeaderColumn)
            int x3 = clientSize.Width;                                         // x3: úplně vpravo
            int x2t = x3 - GScrollBar.DefaultSystemBarWidth;                   // x2t: zde začíná TablesScrollBar (vpravo, hned za koncem ColumnsScrollBar), tedy pokud by byl zobrazen
            int x2r = (this.TablesScrollBarVisible ? x2t : x3);                // x2r: zde reálně končí oblast prostoru pro tabulky a končí zde i ColumnsScrollBar, se zohledněním aktuální viditelnosti TablesScrollBaru
            int y0 = 0;                                                        // y0: úplně nahoře
            int y1 = y0;                                                       // y1: zde začíná prostor pro tabulky i TablesScrollBar 
            int y3 = clientSize.Height;                                        // y3: úplně dole
            int y2 = y3 - GScrollBar.DefaultSystemBarHeight;                   // y2: zde začíná ColumnsScrollBar (dole, hned za koncem prostoru pro tabulky)

            this.GridTablesBounds = new Rectangle(x0, y0, x2r - x0, y2 - y0);
            this.ColumnRowHeaderVisualRange = new Int32Range(x0, x1);
            this.ColumnsDataVisualRange = new Int32Range(x1, x2r);
            if (this.TablesScrollBarVisible)
                this.TablesScrollBarBounds = new Rectangle(x2t, y1, x3 - x2t, y2 - y1);
            this.ColumnsScrollBarBounds = new Rectangle(x1, y2, x2r - x1, y3 - y2);
            this.GridVoidBounds1 = new Rectangle(x0, y2, x1 - x0, y3 - y2);
            this.GridVoidBounds2 = new Rectangle(x2r, y2, x3 - x2r, y3 - y2);

            this.GridLayoutValid = true;
        }
        /// <summary>
        /// Prostor pro tabulky (hlavní prostor), neobsahuje pozice scrollbarů X a Y
        /// </summary>
        protected Rectangle GridTablesBounds { get; set; }
        /// <summary>
        /// Vizuální rozmezí prostoru pro sloupec RowHeader v ose X = začíná na pozici 0 a končí tam, kde začíná prostor pro datové sloupce
        /// </summary>
        public Int32Range ColumnRowHeaderVisualRange { get; protected set; }
        /// <summary>
        /// Vizuální rozmezí prostoru pro datové sloupce v ose X = začíná za RowHeader columnem, a končí na začátku svislého scrollbaru tabulek
        /// </summary>
        public Int32Range ColumnsDataVisualRange { get; protected set; }
        /// <summary>
        /// true pokud se má zobrazovat svislý scrollbar (pro tabulky, vpravo)
        /// </summary>
        protected bool TablesScrollBarVisible { get; set; }
        /// <summary>
        /// Prostor pro svislý scrollbar (pro tabulky, vpravo)
        /// </summary>
        protected Rectangle TablesScrollBarBounds { get; set; }
        /// <summary>
        /// true pokud se má zobrazovat vodorovný scrollbar (pro sloupce, dole)
        /// </summary>
        protected bool ColumnsScrollBarVisible { get; set; }
        /// <summary>
        /// Prostor pro vodorovný scrollbar (pro sloupce, dole)
        /// </summary>
        protected Rectangle ColumnsScrollBarBounds { get; set; }
        /// <summary>
        /// Prázdný prostor 1 = pod sloupcem RowHeaderColumn, kam nezasahuje ColumnsScrollBar - ten je jen pod prostorem datových sloupců.
        /// Tento prostor není interaktvní, ale měl by být vyplněn barvou pozadí.
        /// </summary>
        protected Rectangle GridVoidBounds1 { get; set; }
        /// <summary>
        /// Prázdný prostor 2 = pod prostorem TablesScrollBar, kam nezasahuje ColumnsScrollBar - ten je jen pod prostorem datových sloupců.
        /// Tento prostor není interaktvní, ale měl by být vyplněn barvou pozadí.
        /// </summary>
        protected Rectangle GridVoidBounds2 { get; set; }
        /// <summary>
        /// true pokud je layout vnitřních prostor gridu korektně spočítán
        /// </summary>
        protected bool GridLayoutValid { get; set; }
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
        protected GPosition TablesPositions { get; set; }
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
        private ISequenceLayout[] _TablesSequence { get; set; }
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
        protected GScrollBar TablesScrollBar { get; set; }
        #endregion
        #region Pozicování vodorovné - sloupce tabulek a dolní vodorovný scrollbar
        /// <summary>
        /// Obsahuje všechny viditelné sloupce ze všech viditelných tabulek celého gridu.
        /// Pole je v tom pořadí, v jakém se sloupce mají zobrazovat.
        /// Sloupce vycházejí primárně z první tabulky gridu. Toto pole (this.Columns) reprezentuje synchronizované pole sloupců ze všech tabulek Gridu.
        /// Změna šířky sloupce nebo jeho přemístění na jinou pozici v kterékoli tabulce v Gridu se prostřednictvím Gridu promítá do všech tabulek Gridu.
        /// Jednotlivé tabulky mají svoji kolekci Columns, ale jejich šířka a pořadí jsou řízeny centrálně z této kolekce.
        /// Rovněž i TimeAxis je synchronizována díky této kolekci.
        /// </summary>
        public GridColumn[] Columns { get { this._ColumnsCheck(); return this._Columns; } }
        /// <summary>
        /// Zajistí, že pole sloupců budou obsahovat platné hodnoty
        /// </summary>
        private void _ColumnsCheck()
        {
            bool needContent = (this._ColumnDict == null || this._Columns == null);
            bool needRecalc = !this._ColumnsLayoutValid;
            if (needContent || needRecalc)
            {
                Dictionary<int, GridColumn> columnDict = this._ColumnDict;
                if (needContent)
                {    // Je nutné vygenerovat obsah těchto polí:
                     // Vytvořím index, kde klíčem je ColumnId, a hodnotou je instance GridColumn.
                     // každá jedna instance GridColumn bude obsahovat souhrn všech viditelných sloupců shodného ColumnId, ze všech viditelných tabulek.
                    columnDict = new Dictionary<int, GridColumn>();
                    foreach (GTable table in this._Tables)
                    {
                        if (table.DataTable == null || !table.DataTable.IsVisible) continue;
                        foreach (Column column in table.DataTable.Columns.Where(c => c.IsVisible))
                        {
                            int columnId = column.ColumnId;
                            GridColumn gridColumn;
                            if (columnDict.TryGetValue(columnId, out gridColumn))
                                gridColumn.AddColumn(column);
                            else
                                columnDict.Add(columnId, new GridColumn(column));
                        }
                    }
                }

                // Nyní vytvořím lineární soupis GridColumn, a setřídím jej podle pořadí dle sloupce Master (z první tabulky):
                List<GridColumn> columnList = columnDict.Values.ToList();
                columnList.Sort(GridColumn.CompareOrder);

                // Dále zajistím, že hodnoty ColumnOrder ve sloupcích budou číslovány po 2

                // Zajistím provedení nápočtu pozic:
                SequenceLayout.SequenceLayoutCalculate(columnList);

                // Na každý pád je nutno uložit vypočtená data:
                this._ColumnDict = columnDict;
                this._Columns = columnList.ToArray();
                this._ColumnsLayoutValid = true;
            }
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit data o sloupci Gridu pro dané ID.
        /// Sloupec Gridu (instance třídy GridColumn) reprezentuje jeden svislý sloupec stejného ColumnId, přes všechny tabulky.
        /// Sloupec obsahuje MasterColumn = sloupec tohoto ColumnId z první tabulky, ve které se vyskytl. Ten pak hraje roli Mastera.
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="gridColumn"></param>
        /// <returns></returns>
        public bool TryGetGridColumn(int columnId, out GridColumn gridColumn)
        {
            this._ColumnsCheck();
            return this._ColumnDict.TryGetValue(columnId, out gridColumn);
        }
        /// <summary>
        /// Index sloupců podle ColumnId
        /// </summary>
        private Dictionary<int, GridColumn> _ColumnDict;
        /// <summary>
        /// Seznam sloupců podle jejich pořadí
        /// </summary>
        private GridColumn[] _Columns;
        /// <summary>
        /// true pokud obsah pole _Columns má správné souřadnice, false pokud ne
        /// </summary>
        private bool _ColumnsLayoutValid = false;
        /// <summary>
        /// Řídící prvek pro Pozice sloupců
        /// </summary>
        public GPosition ColumnsPositions { get; protected set; }
        /// <summary>
        /// Metoda vrátí vizuální souřadnice daného sloupce v aktuálním prostoru Gridu, podle 
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public Int32Range GetColumnVisualPosition(ISequenceLayout column)
        {
            if (!SequenceLayout.IsItemVisible(column as ISequenceLayout, this.ColumnsPositions.DataVisibleRange)) return null;
            return this.ColumnsPositions.GetVisualPosition(column);
        }
        /// <summary>
        /// Metoda zajistí změnu šířky sloupce RowHeader, a návazné změny v interních strukturách plus překreslení
        /// </summary>
        /// <param name="width"></param>
        /// <returns></returns>
        public bool ColumnRowHeaderResizeTo(ref int width)
        {
            Table masterTable = (this._Tables.Count > 0 ? this._Tables[0].DataTable : null);
            if (masterTable == null) return false;

            int widthOld = masterTable.RowHeaderWidth;
            masterTable.RowHeaderWidth = width;
            int widthNew = masterTable.RowHeaderWidth;

            bool isChanged = (widthNew != widthOld);
            if (isChanged)
            {
                width = widthNew;
                // Zajistit invalidaci a překresení:
                this.ColumnsLayoutReset();
                this.Repaint();
            }
            return isChanged;
        }
        /// <summary>
        /// Metoda zajistí přesun sloupce na jiné místo: tak, aby daný sloupec (column) byl zobrazen na daném pořadí.
        /// Provede to tak, že změní hodnoty ColumnOrder vhodných sloupců, a zajistí přepočty polí sloupců a zajistí i překreslení.
        /// </summary>
        /// <param name="column">Sloupec, který chceme přesunout. Důležité je jen jeho ColumnId</param>
        /// <param name="targetOrder">Cíl přesunu = ColumnOrder jiného sloupce, na jehož místo chci dát daný column. Tento jiný sloupec bude až za aktuálním sloupcem. Cíl přesunu může být i vyšší, než je nejvyšší ID, pak aktuální sloupec bude zařazen za poslední prvek.</param>
        public bool ColumnMoveTo(Column column, int targetOrder)
        {
            if (column == null) return false;
            return this.ColumnMoveTo(column.ColumnId, targetOrder);
        }
        /// <summary>
        /// Metoda zajistí přesun sloupce na jiné místo: tak, aby daný sloupec (column) byl zobrazen na daném pořadí.
        /// Provede to tak, že změní hodnoty ColumnOrder vhodných sloupců, a zajistí přepočty polí sloupců a zajistí i překreslení.
        /// </summary>
        /// <param name="columnId">Id sloupce, který chceme přesunout.</param>
        /// <param name="targetOrder">Cíl přesunu = ColumnOrder jiného sloupce, na jehož místo chci dát daný column. Tento jiný sloupec bude až za aktuálním sloupcem. Cíl přesunu může být i vyšší, než je nejvyšší ID, pak aktuální sloupec bude zařazen za poslední prvek.</param>
        public bool ColumnMoveTo(int columnId, int targetOrder)
        {
            GridColumn sourceColumn;
            if (!this.TryGetGridColumn(columnId, out sourceColumn)) return false;        // Daný sloupec neznáme
            int sourceOrder = sourceColumn.ColumnOrder;
            if (sourceOrder == targetOrder) return false;                                // Není co dělat

            GridColumn[] columns = this.Columns;
            int length = columns.Length;
            bool isSourceAdded = false;

            // Do pole reorderedColumns přidám prvky fyzicky v požadovaném pořadí, a ve vhodnou chvíli do něj vložím požadovaný sloupec sourceColumn:
            List<GridColumn> reorderedColumns = new List<GridColumn>();
            for (int i = 0; i < length; i++)
            {
                GridColumn current = columns[i];
                if (current.ColumnId == sourceColumn.ColumnId) continue;                 // Přemisťovaný sloupec = v tuto chvíli jej přeskočím, dostane se do pole jinak!
                int currentOrder = current.ColumnOrder;
                // Nyní řeším sloupec (current), jehož pořadí je menší než požadované cílové = přidám ho do seznamu reorderedColumns:
                if (currentOrder < targetOrder) { reorderedColumns.Add(current); }
                // Nynější sloupec (current) má být za přemisťovaným sloupcem, a ten jsme ještě do seznamu reorderedColumns nezařadili - zařadíme tam sourceColumn, a za ním current:
                else if (!isSourceAdded) { reorderedColumns.Add(sourceColumn); isSourceAdded = true; reorderedColumns.Add(current); }
                // Sloupec current má být za sloupcem sourceColumn, a ten (sourceColumn) už byl do seznamu reorderedColumns přidán dříve:
                else { reorderedColumns.Add(current); }
            }
            // Pokud jsem až dosdu nepřidal sourceColumn, přidám jej na konec:
            if (!isSourceAdded) { reorderedColumns.Add(sourceColumn); isSourceAdded = true; }

            // Dosud jsme nezměnili žádné ColumnOrder, ale máme korektně seřazenou kolekci = takže do ní vepíšeme novou hodnotu ColumnOrder:
            int columnOrder = 0;
            foreach (GridColumn gc in reorderedColumns)
                gc.ColumnOrder = columnOrder++;

            // Zajistit invalidaci a překresení:
            this.ColumnsSequenceReset();
            this.Repaint();

            return true;
        }
        /// <summary>
        /// Metoda zajistí změnu šířky daného sloupce, a návazné změny v interních strukturách plus překreslení
        /// </summary>
        /// <param name="column"></param>
        /// <param name="width">Požadovaná šířka, může se změnit</param>
        /// <returns></returns>
        public bool ColumnResizeTo(Column column, ref int width)
        {
            if (column == null) return false;
            return this.ColumnResizeTo(column.ColumnId, ref width);
        }
        /// <summary>
        /// Metoda zajistí změnu šířky daného sloupce, a návazné změny v interních strukturách plus překreslení
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public bool ColumnResizeTo(int columnId, ref int width)
        {
            GridColumn sourceColumn;
            if (!this.TryGetGridColumn(columnId, out sourceColumn)) return false;        // Daný sloupec neznáme

            int widthOld = sourceColumn.ColumnWidth;
            sourceColumn.ColumnWidth = width;
            int widthNew = sourceColumn.ColumnWidth;

            bool isChanged = (widthNew != widthOld);
            if (isChanged)
            {
                width = widthNew;
                // Zajistit invalidaci a překresení:
                this.ColumnsLayoutReset();
                this.Repaint();
            }
            return isChanged;
        }
        /// <summary>
        /// Inicializace pozic sloupců
        /// </summary>
        private void InitColumnsPositions()
        {
            this.ColumnsPositions = new GPosition(GGrid.DefaultRowHeaderWidth, 28, this._ColumnPositionGetVisualSize, this._ColumnPositionGetDataSize);

            this.ColumnsScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            this.ColumnsScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
            this.ColumnsScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
        }
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
            ISequenceLayout[] array = this.Columns;
            int count = array.Length;
            return (count > 0 ? array[count - 1].End : 0);
        }
        /// <summary>
        /// Resetuje kolekci Columns (=donutí ji znovu se načíst).
        /// Má se volat po těchto akcích: Změna počtu sloupců; Změna pořadí sloupců.
        /// Nemá se volat po změně: Změna šířky sloupce (při této změně se nemění počet a pořadí sloupců).
        /// Nemusí se volat po změnách: Posun scrollbaru sloupců, změna rozměrů gridu (při této změně se nemění ani datové souřadnice sloupců).
        /// </summary>
        protected void ColumnsSequenceReset() { this._Columns = null; this._MainTimeAxisReset(); }
        /// <summary>
        /// Resetuje platnost datových souřadnic sloupců (jejich hodnoty v ISequenceLayout).
        /// Vynutí si přepočet datových hodnot Begin a End.
        /// Je nedostatečné po změnách: Změna počtu sloupců; Změna pořadí sloupců.
        /// Má se volat pouze po akci: Změna šířky sloupce.
        /// Nemusí se volat po změnách: Posun scrollbaru sloupců, změna rozměrů gridu (při této změně se nemění ani datové souřadnice sloupců).
        /// </summary>
        protected void ColumnsLayoutReset() { this._ColumnsLayoutValid = false; }
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
        protected GScrollBar ColumnsScrollBar { get; set; }
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
                    GridColumn timeAxisColumn = this.Columns.FirstOrDefault(c => c.UseTimeAxis);
                    if (timeAxisColumn != null)
                    {
                        this._MainTimeAxis = timeAxisColumn.MasterColumn.TimeAxis;
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
    #region class GridColumn : Třída popisující jeden sloupec Gridu
    /// <summary>
    /// GridColumn : Třída popisující jeden sloupec Gridu.
    /// Jeden sloupec Gridu reprezentuje svislý sloupec přes všechny tabulky jednoho Gridu, a obsahuje v sobě synchronizované sloupce stejného ColumnId ze všech tabulek Gridu.
    /// </summary>
    public class GridColumn : ISequenceLayout
    {
        #region Konstrukce, proměnné
        /// <summary>
        /// Vytvoří novou instanci pro daný Master column
        /// </summary>
        /// <param name="column"></param>
        public GridColumn(Column column)
        {
            this._ColumnList = new List<Column>();
            this.AddColumn(column);
        }
        /// <summary>
        /// Soupis datových sloupců stejného ColumnId ze všech tabulek jednoho Gridu = "svislé pole" obsahující všechny synchronizované sloupce = pod sebou
        /// </summary>
        private List<Column> _ColumnList;
        /// <summary>
        /// Master column, nikdy není null
        /// </summary>
        private Column _MasterColumn;
        /// <summary>
        /// Aktuální pořadí
        /// </summary>
        private int _SortOrder;
        #endregion
        #region Public rozhraní: Master, properties, AddColumn(), CompareOrder()
        /// <summary>
        /// Hlavní sloupec = první sloupec nalezený s tímto ID
        /// </summary>
        public Column MasterColumn { get { return this._MasterColumn; } }
        /// <summary>
        /// ColumnId, synchronizační klíč všech sloupců v této tabulce
        /// </summary>
        public int ColumnId { get { return this._MasterColumn.ColumnId; } }
        /// <summary>
        /// Pořadí tohoto sloupce při zobrazování. 
        /// Načítá se z MasterColumn.ColumnOrder.
        /// Hodnotu lze i vložit, pak se vkládá do všech sloupců!
        /// </summary>
        public int ColumnOrder
        {
            get { return this._MasterColumn.ColumnOrder; }
            set
            {
                foreach (Column column in this._ColumnList)
                    column.ColumnOrder = value;
            }
        }
        /// <summary>
        /// Šířka tohoto sloupce při zobrazování. 
        /// Načítá se z MasterColumn.Size.
        /// Hodnotu lze i vložit, pak se vkládá do všech sloupců!
        /// </summary>
        public int ColumnWidth
        {
            get { return ((ISequenceLayout)this).Size; }
            set { ((ISequenceLayout)this).Size = value; }
        }
        /// <summary>
        /// true pokud se pro sloupec má zobrazit časová osa v záhlaví
        /// </summary>
        public bool UseTimeAxis { get { return this._MasterColumn.UseTimeAxis; } }
        /// <summary>
        /// Přidá další sloupec do this GridColumnu
        /// </summary>
        /// <param name="column"></param>
        public void AddColumn(Column column)
        {
            if (column == null) return;
            if (this._ColumnList.Count == 0)
                this._MasterColumn = column;
            this._ColumnList.Add(column);
        }
        /// <summary>
        /// Komparátor ColumnOrder ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrder(GridColumn a, GridColumn b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.ColumnOrder.CompareTo(b.ColumnOrder);
        }
        #endregion
        #region ISequenceLayout - adapter: get čte data z Master sloupce, set ukládá data do všech sloupců
        int ISequenceLayout.Begin
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn).Begin;
            }
            set
            {
                foreach (Column column in this._ColumnList)
                    ((ISequenceLayout)column).Begin = value;
            }
        }
        int ISequenceLayout.Size
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn).Size;
            }
            set
            {
                foreach (Column column in this._ColumnList)
                    ((ISequenceLayout)column).Size = value;
            }
        }
        int ISequenceLayout.End
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn).End;
            }
        }
        #endregion
    }
    #endregion
    #region class GPosition : Třída, která řídí zobrazení většího obsahu dat (typicky sada řádků) v omezeném prostoru Controlu (typicky Grid, Tabulka)
    /// <summary>
    /// Třída, která řídí zobrazení většího obsahu dat (typicky sada řádků) v omezeném prostoru Controlu (typicky Grid, Tabulka),
    /// kde část prostoru je vyhrazena pro záhlaví, další část pro data a další část pro zápatí.
    /// Prostor pro data je typicky spojen se Scrollbarem, do kterého se promítá poměr viditelné části ku celkovému množství dat, a aktuální pozice dat.
    /// Tato třída eviduje: velikost záhlaví (=vizuální začátek dat), velikost prostoru pro data, počáteční logickou pozici dat (od kterého pixelu jsou data viditelná),
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
            : this(0, DefaultDataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, null, null)
        { }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(int firstPixel, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
            : this(firstPixel, DefaultDataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, null, null)
        { }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(int firstPixel, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod, Func<int> getVisualFirstPixel, Action<int> setVisualFirstPixel)
            : this(firstPixel, DefaultDataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, getVisualFirstPixel, setVisualFirstPixel)
        { }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="dataSizeAddSpace">Přídavek k hodnotě DataSize (=celková velikost dat) v pixelech, který bude předáván do ScrollBaru.</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(int firstPixel, int dataSizeAddSpace, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
            : this(firstPixel, dataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, null, null)
        {
        }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GPosition(int firstPixel, int dataSizeAddSpace, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod, Func<int> getVisualFirstPixel, Action<int> setVisualFirstPixel)
        {
            this.VisualFirstPixel = firstPixel;
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
            this._GetVisualFirstPixel = getVisualFirstPixel;
            this._SetVisualFirstPixel = setVisualFirstPixel;
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
        /// Číslo vizuálního pixelu, na kterém začíná zobrazení dat. Pixely před tímto jsou obsazeny něčím jiným (typicky Header).
        /// Tato hodnota je buď uložená v this instanci, anebo může být napojená na externí funkcionalitu 
        /// (k tomu je třeba vytvořit instanci této třídy pomocí konstruktoru s parametry getVisualFirstPixel, setVisualFirstPixel).
        /// </summary>
        public int VisualFirstPixel
        {
            get
            {
                if (this._GetVisualFirstPixel != null)
                    return this._GetVisualFirstPixel();
                return this._VisualFirstPixelValue;
            }
            set
            {
                if (this._SetVisualFirstPixel != null)
                    this._SetVisualFirstPixel(value);
                else
                    this._VisualFirstPixelValue = value;
            }
        }
        private Func<int> _GetVisualFirstPixel;
        private Action<int> _SetVisualFirstPixel;
        private int _VisualFirstPixelValue;
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
        /// Begin = DataFirstPixel, End = DataFirstPixel + VisualSize.
        /// </summary>
        public Int32Range DataVisibleRange { get { return new Int32Range(this.DataFirstPixel, this.DataEndPixel); } }
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
        /// <summary>
        /// Defaultní přídavek k hodnotě DataSize
        /// </summary>
        public static int DefaultDataSizeAddSpace { get { return 26; } }
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
        /// <summary>
        /// Vrátí vizuální pozici (odpovídající aktuálnímu controlu) pro daný prvek.
        /// Vrací tedy danou pozici, kde Begin = (dataPosition + VisualFirstPixel - DataFirstPixel).
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Int32Range GetVisualPosition(ISequenceLayout item)
        {
            if (item == null) return null;
            int begin = this.GetVisualPosition(item.Begin);
            int end = begin + item.Size;
            return new Int32Range(begin, end);
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
