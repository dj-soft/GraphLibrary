using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;

namespace Djs.Common.Components.Grid
{
    // Filosofický základ pro obsluhu různých událostí: Tabulka gridu je líná jako veš! 
    // Ta je tak líná, že když se dojde ke změně něčeho (třeba výšky některé tabulky), tak ta změna (v property Table.Height) zavolá "nahoru" že došlo k dané změně,
    //  to volání se dostane do GTable jako invalidace výšky tabulky, to vyvolá obdobné volání do Gridu, a Grid si jen líně poznamená: "Rozložení tabulek na výšku už neplatí".
    //  Současně s tím si poznamená: "Neplatí ani moje ChildItem prvky (protože některá další tabulka může/nemusí být vidět, protože se odsunula dolů).
    // Podobně se chová i GTable: poznamená si: moje vnitřní souřadnice ani moje ChildItem prvky nejsou platné.
    // Teprve až bude někdo chtít pracovat s něčím v Gridu nebo v jeho GTable (typicky: zjištění interaktivity prvků, vykreslení tabulky), tak si požádá o ChildItems,
    //  tam se zjistí že jsou neplatné, a Grid nebo GTable začne shánět platné údaje. 
    // Při tom zjistí, že je jich většina neplatných, a začne je přepočítávat z aktuálních reálných hodnot (fyzické rozměry, počet a velikost tabulek, pozice řádků, atd).

    /// <summary>
    /// GTable : vizuální třída pro zobrazení obsahu jedné datové tabulky v jednom Gridu.
    /// Grid může zobrazovat data z více tabulek pomocí více instancí GTable, ale jedna GTable může zobrazit data jen z jedné datové tabulky.
    /// </summary>
    public class GTable : InteractiveContainer, IInteractiveItem, IGridMember, ISequenceLayout, IDisposable
    {
        #region Inicializace, reference na GGrid, IGridMember
        internal GTable(GGrid grid, Table table)
        {
            this._Grid = grid;
            this._Table = table;
            IGTableMember igtm = table as IGTableMember;
            if (igtm != null) igtm.GTable = this;
            this.Init();
        }
        private void Init()
        {
            this.InitInteractive();
            this.InitRowsPositions();
            this.InitHeaderSplitter();
            this.InitTableSplitter();
        }
        protected void InitInteractive()
        {
            this.Style = GInteractiveStyles.AllMouseInteractivity | GInteractiveStyles.KeyboardInput;
        }
        void IDisposable.Dispose()
        {
            IGTableMember igtm = this._Table as IGTableMember;
            if (igtm != null) igtm.GTable = null;

            this._Grid = null;
            this._TableId = -1;
            this._SetTableOrder();
            this._Table = null;
        }
        public override string ToString()
        {
            return "GTable for " + this.DataTable.ToString();
        }
        /// <summary>
        /// Reference na grid, kam tato tabulka patří.
        /// </summary>
        public GGrid Grid { get { return this._Grid; } }
        /// <summary>
        /// true pokud máme referenci na grid
        /// </summary>
        public bool HasGrid { get { return (this._Grid != null); } }
        /// <summary>
        /// IGridMember.GGrid = _Grid
        /// </summary>
        GGrid IGridMember.GGrid { get { return this._Grid; } set { this._Grid = value; } }
        /// <summary>
        /// IGridMember.Id = _TableId
        /// </summary>
        int IGridMember.Id { get { return this._TableId; } set { this._TableId = value; this._SetTableOrder(); } }
        /// <summary>
        /// Nastaví this.DataTable.TableOrder na hodnotu odpovídající this._TableId.
        /// </summary>
        private void _SetTableOrder()
        {
            Table table = this._Table;
            if (table != null)
            {
                if (this._TableId < 0)
                    table.TableOrder = -1;
                else if (this._TableId >= 0 && table.TableOrder < 0)
                    table.TableOrder = this._TableId;
            }
        }
        /// <summary>
        /// Reference na GGrid
        /// </summary>
        private GGrid _Grid;
        #endregion
        #region Rozmístění vnitřních prvků tabulky - souřadnice pro prostor záhlaví, řádků a scrollbaru
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
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            InvalidateByBoundsChanges(newBounds);
        }
        /// <summary>
        /// Metoda invaliduje patřičné údaje na základě aktuální změny Bounds.
        /// Pokud se nemění vnitřní prostor tabulky (ClientSize), pak není nutno přepočítávat všechna data.
        /// K tomu dochází při standardním přesouvání sady tabulek nahoru/dolů: mění se hodnota Top, ale nemění se Width ani Height tabulky, 
        /// pak je zbytečné invalidovat vnitřní data a znovu je napočítávat, protože vnitřní objekty (Childs) jsou stále tytéž, na stále stejných souřadnicích (jejich souřadnice jsou relativní k GridTable).
        /// </summary>
        /// <param name="newBounds"></param>
        protected void InvalidateByBoundsChanges(Rectangle newBounds)
        {
            InvalidateItem items = InvalidateItem.None;

            // Změna umístění tabulky invaliduje TablePosition, dochází ke změně pozice TableSplitter:
            Rectangle? lastBounds = this._LastBounds;
            bool isSamePosition = (lastBounds.HasValue && lastBounds.Value.Location == newBounds.Location);
            if (!isSamePosition)
                items |= InvalidateItem.TablePosition;

            // Vnitřní prostor - to je výška a šířka:
            Size? lastClientSize = this._LastClientSize;
            Size currentClientSize = this.ClientSize;
            bool isSameHeight = (lastClientSize.HasValue && lastClientSize.Value.Height == currentClientSize.Height);
            if (!isSameHeight)
                items |= InvalidateItem.TableHeight;
            bool isSameWidth = (lastClientSize.HasValue && lastClientSize.Value.Width == currentClientSize.Width);
            if (!isSameWidth)
                items |= InvalidateItem.TableSize;

            this.Invalidate(items);
        }
        /// <summary>
        /// Metoda zajistí, že souřadnice vnitřních objektů budou platné a budou odpovídat aktuální velikosti Tabulky a poloze splitterů a rozsahu dat.
        /// Jde o hodnoty pro prvky tabulky: ColumnHeader, HeaderSplitter, RowArea, Scrollbar. Neřeší se OuterBounds = TableSplitterBounds.
        /// </summary>
        protected void _InnerBoundsCheck()
        {
            if (this._TableInnerLayoutValid) return;
            Size clientSize = this.ClientSize;

            if (clientSize.Width <= 0 || clientSize.Height <= 0) return;

            this._TableInnerLayoutValid = true;                                     // Normálně to patří až na konec metody. Ale některé komponenty mohou používat již částečně napočtené hodnoty, a pak bychom se zacyklili

            // Bude viditelný scrollbar řádků? (to je tehdy, když výška zobrazitelných řádků je větší než výška prostoru pro řádky):
            //  Objekt RowsPositions tady provede dotaz na velikost dat (metoda _RowsPositionGetDataSize()) a velikost viditelného prostoru (metoda _RowsPositionGetVisualSize()).
            //  Velikost dat je dána pozicí End posledního řádku z this.RowListAll,
            //  velikost viditelného prostoru pro tabulky je dána výškou tabulky mínus výška hlavičky (this.ClientSize.Height - RowsPositions.VisualFirstPixel), hodnota this.ClientSize je v této době platná.
            this._RowsScrollBarVisible = this.RowsPositions.IsScrollBarActive;

            // Určíme souřadnice jednotlivých elementů:
            int x0 = 0;                                                             // x0: úplně vlevo
            int x1 = (this.HasGrid ? this.Grid.ColumnsDataVisualRange.Begin : 0);   // x1: tady začíná prostor pro datové sloupce
            int x3 = clientSize.Width;                                              // x3: úplně vpravo
            int x2t = x3 - GScrollBar.DefaultSystemBarWidth;                        // x2t: zde začíná RowsScrollBar (vpravo, hned za koncem prostoru pro řádky), tedy pokud by byl zobrazen
            int x2r = (this._RowsScrollBarVisible ? x2t : x3);                      // x2r: zde reálně končí oblast prostoru pro řádky, se zohledněním aktuální viditelnosti RowsScrollBaru
            int y0 = 0;                                                             // y0: úplně nahoře
            int y1 = this.RowsPositions.VisualFirstPixel;                           // y1: zde začíná prostor pro řádky, hned pod prostorem ColumnHeader (hodnota se fyzicky načte z this.DataTable.ColumnHeaderHeight)
            int y3 = clientSize.Height;                                             // y3: úplně dole

            this._TableHeaderBounds = new Rectangle(x0, y0, x1 - x0, y1 - y0);
            this._ColumnHeaderBounds = new Rectangle(x1, y0, x3 - x1, y1 - y0);
            this._RowHeaderBounds = new Rectangle(x0, y1, x1 - x0, y3 - y1);
            this._RowAreaBounds = new Rectangle(x1, y1, x2r - x1, y3 - y1);
            this._RowsScrollBarBounds = new Rectangle(x2t, y1, x3 - x2t, y3 - y1);
            this._HeaderSplitterBounds = new Rectangle(x0, y1, x3 - x0, 0);

            // Invalidace závislých prvků:
            this._VisibleColumns = null;
            this._VisibleRows = null;
            this._RowsScrollBarDataValid = false;
            this._HeaderSplitterDataValid = false;
            this._ChildArrayValid = false;

            this._LastClientSize = clientSize;
        }
        /// <summary>
        /// Vypočte korektní souřadnice pro TableSplitter
        /// </summary>
        protected void _OuterBoundsCheck()
        {
            if (this._TableOuterLayoutValid) return;

            Rectangle bounds = this.Bounds;
            int x0 = bounds.X;                                                      // x0: úplně vlevo
            int w = bounds.Width;
            int y5 = this.Bounds.Bottom;                                            // y5: this.Bottom v souřadném systému mého parenta, pro TableSplitterBounds

            this._TableSplitterBounds = new Rectangle(x0, y5, w, 0);

            // Invalidace závislých prvků:
            this._TableSplitterDataValid = false;

            this._TableOuterLayoutValid = true;
            this._LastBounds = bounds;
        }
        /// <summary>
        /// Vizuální souřadnice prostoru TableHeader (TableHeader vlevo nahoře)
        /// </summary>
        protected Rectangle TableHeaderBounds { get { this._InnerBoundsCheck(); return this._TableHeaderBounds; } } private Rectangle _TableHeaderBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru záhlaví sloupců (datové ColumnHeaders)
        /// </summary>
        protected Rectangle ColumnHeaderBounds { get { this._InnerBoundsCheck(); return this._ColumnHeaderBounds; } } private Rectangle _ColumnHeaderBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru záhlaví řádků (RowHeader)
        /// </summary>
        protected Rectangle RowHeaderBounds { get { this._InnerBoundsCheck(); return this._RowHeaderBounds; } } private Rectangle _RowHeaderBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru řádků (RowArea) = vlastní obsah dat, nikoli záhlaví
        /// </summary>
        protected Rectangle RowAreaBounds { get { this._InnerBoundsCheck(); return this._RowAreaBounds; } } private Rectangle _RowAreaBounds;
        /// <summary>
        /// Viditelnost scrollbaru řádků
        /// </summary>
        protected bool RowsScrollBarVisible { get { this._InnerBoundsCheck(); return this._RowsScrollBarVisible; } } private bool _RowsScrollBarVisible;
        /// <summary>
        /// Vizuální souřadnice svislého scrollbaru pro řádky (vpravo od prostoru řádků), nemusí být zobrazován (podle RowsScrollBarVisible)
        /// </summary>
        protected Rectangle RowsScrollBarBounds { get { this._InnerBoundsCheck(); return this._RowsScrollBarBounds; } } private Rectangle _RowsScrollBarBounds;
        /// <summary>
        /// Vizuální souřadnice pro splitter pod ColumnHeaderem, tento splitter je vizuální součástí this.Childs,
        /// souřadnice jsou tedy relativní k this.
        /// Souřadnice X a Width se vkládají do ValueSilent, souřadnice Y do ValueSilent. Výška = 0.
        /// </summary>
        protected Rectangle HeaderSplitterBounds { get { this._InnerBoundsCheck(); return this._HeaderSplitterBounds; } } private Rectangle _HeaderSplitterBounds;
        /// <summary>
        /// Vizuální souřadnice pro splitter pod this tabulkou, tento splitter je vizuální součástí mého parenta = GGrid.Childs,
        /// souřadnice jsou tedy relativní k jeho koordinátům.
        /// Souřadnice X a Width se vkládají do ValueSilent, souřadnice Y do ValueSilent. Výška = 0.
        /// </summary>
        protected Rectangle TableSplitterBounds { get { this._OuterBoundsCheck(); return this._TableSplitterBounds; } } private Rectangle _TableSplitterBounds;
        /// <summary>
        /// Metoda vrátí relativní souřadnice požadovaného prostoru.
        /// Relativní = relativně k this.Bounds.Location, který představuje bod {0;0}
        /// </summary>
        /// <param name="areaType"></param>
        /// <returns></returns>
        public Rectangle GetRelativeBoundsForArea(TableAreaType areaType)
        {
            switch (areaType)
            {
                case TableAreaType.TableHeader: return this.TableHeaderBounds;
                case TableAreaType.ColumnHeader: return this.ColumnHeaderBounds;
                case TableAreaType.RowHeader: return this.RowHeaderBounds;
                case TableAreaType.Data: return this.RowAreaBounds;
                case TableAreaType.VerticalScrollBar: return this.RowsScrollBarBounds;
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice požadovaného prostoru.
        /// Souřadnice slouží k provedení Graphics.Clip() před vykreslením obsahu.
        /// </summary>
        /// <param name="areaType"></param>
        /// <returns></returns>
        public Rectangle GetAbsoluteBoundsForArea(TableAreaType areaType)
        {
            Rectangle tableAbsoluteBounds = this.BoundsAbsolute;
            Rectangle relativeBounds = this.GetRelativeBoundsForArea(areaType);
            return relativeBounds.Add(tableAbsoluteBounds.Location);
        }
        /// <summary>
        /// Platnosti souřadnic vnitřních objektů (_TableHeaderBounds, _ColumnHeaderBounds, _RowHeaderBounds, _RowAreaBounds, _RowsScrollBarVisible, _RowsScrollBarBounds, _HeaderSplitterBounds)
        /// </summary>
        private bool _TableInnerLayoutValid;
        /// <summary>
        /// Vnitřní rozměry tabulky, pro které byly naposledy validovány Inner souřadnice
        /// </summary>
        private Size? _LastClientSize;
        /// <summary>
        /// Platnosti souřadnic vnějších objektů (_TableSplitterBounds)
        /// </summary>
        private bool _TableOuterLayoutValid;
        /// <summary>
        /// Souřadnice tabulky, pro které byly naposledy validovány Outer souřadnice
        /// </summary>
        private Rectangle? _LastBounds;
        #endregion
        #region Public data o tabulce
        /// <summary>
        /// Datová tabulka
        /// </summary>
        public Table DataTable { get { return this._Table; } }
        private Table _Table;
        /// <summary>
        /// Jednoznačné ID této tabulky v rámci Gridu. Read only.
        /// Je přiděleno při přidání do gridu, pak má hodnotu 0 nebo kladnou.
        /// Hodnota se nemění ani přemístěním na jinou pozici, ani odebráním některé tabulky s menším ID.
        /// Po odebrání z gridu je hodnota -1.
        /// </summary>
        public int TableId { get { return this._TableId; } }
        private int _TableId;
        /// <summary>
        /// Název tabulky, podle něj lze hledat. jde o klíčové slovo, nikoli popisek (Caption)
        /// </summary>
        public string TableName { get { return this.DataTable.TableName; } }
        /// <summary>
        /// Pořadí této tabulky v Gridu při zobrazování.
        /// Výchozí je -1, pak bude tabulka zařazena na konec soupisu tabulek v jednom gridu.
        /// Datová vrstva může vložit jinou hodnotu (nula a kladnou), a tím explicitně určit pozici tabulky v gridu.
        /// Jednotlivé tabulky nemusí mít hodnoty TableOrder v nepřerušovaném pořadí.
        /// Po napojení tabulky do gridu je do TableOrder vepsána pořadová hodnota, pokud aktuální hodnota je záporná (což je default).
        /// Po odpojení tabuky z Gridu je vepsána hodnota -1.
        /// </summary>
        public int TableOrder { get { return this.DataTable.TableOrder; } }
        /// <summary>
        /// Úložiště pro souřadnice na ose Y v koordinátech Parent controlu (=Grid), kde má být tato tabulka zobrazena.
        /// Jde o hodnoty ISequenceLayout.Begin a .End převedené z datových koordinátů do vizuálních, reprezentují tedy this.Bounds.Y a this.Bounds.Bottom.
        /// Nicméně setování této property nemá provádět změnu this.Bounds, zde je hodnota jen uložena v procesu výpočtů svislého layoutu v Gridu, a odsud je vyzvednuta v potřebnou chvíli zase v Gridu.
        /// </summary>
        public Int32Range VisualRange { get; set; }
        /// <summary>
        /// Komparátor podle hodnoty TableOrder ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrder(GTable a, GTable b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.TableOrder.CompareTo(b.TableOrder);
        }
        #endregion
        #region Sloupce tabulky - dvě oddělená pole sloupců: a) všechny aktuálně dostupné sloupce - pro práci s kolekcí sloupců, b) pouze viditelné sloupce - pro kreslení
        /// <summary>
        /// Pole všech sloupců této tabulky, které mohou být zobrazeny, v tom pořadí, v jakém jsou zobrazovány.
        /// </summary>
        public Column[] Columns { get { this._ColumnsCheck(); return this._Columns; } }
        /// <summary>
        /// Pole viditelných sloupců této tabulky, které jsou nyní zčásti nebo plně viditelné, v tom pořadí, v jakém jsou zobrazovány.
        /// </summary>
        public Column[] VisibleColumns { get { this._VisibleColumnsCheck(); return this._VisibleColumns; } }
        /// <summary>
        /// Ověří a zajistí připravenost pole Columns.
        /// Toto pole obsahuje správné souřadnice (ISequenceLayout), proto po změně šířky sloupce nebo po změně Order je třeba toto pole invalidovat.
        /// </summary>
        private void _ColumnsCheck()
        {
            if (this._Columns != null) return;
            List<Column> columnsList = this.DataTable.Columns.Where(c => c.IsVisible).ToList();    // Vybrat viditelné sloupce
            columnsList.Sort(Column.CompareOrder);                                                 // Setřídit podle pořadí
            SequenceLayout.SequenceLayoutCalculate(columnsList);                                   // Napočítat jejich ISequenceLayout.Begin a .End
            this._Columns = columnsList.ToArray();                                                 // Uložit
            this._VisibleColumns = null;                                                           // Invalidovat viditelné sloupce
        }
        /// <summary>
        /// Ověří a zajistí připravenost pole VisibleColumns.
        /// Viditelné sloupce mají korektně nastaveny aktuální souřadnice do column.ColumnHeader.VisualRange, neviditelné mají VisualRange = null.
        /// </summary>
        private void _VisibleColumnsCheck()
        {
            if (this._VisibleColumns != null) return;

            List<Column> visibleColumns = new List<Column>();
            GridPosition columnsPositions = this.Grid.ColumnsPositions;
            Int32Range dataVisibleRange = columnsPositions.DataVisibleRange;                       // Rozmezí datových pixelů, které jsou viditelné
            foreach (Column column in this.Columns)
            {
                ISequenceLayout isl = column as ISequenceLayout;
                bool isColumnVisible = SequenceLayout.IsItemVisible(isl, dataVisibleRange);        // Tento sloupec je vidět?
                column.ColumnHeader.VisualRange = (isColumnVisible ? columnsPositions.GetVisualPosition(isl) : null);
                if (isColumnVisible)
                    visibleColumns.Add(column);
            }
            this._VisibleColumns = visibleColumns.ToArray();
            this._ChildArrayValid = false;
        }
        /// <summary>
        /// Pole všech sloupců této tabulky, v pořadí dle jejich ColumnOrder.
        /// Viditelné sloupce mají nastavenu hodnotu column.ColumnHeader.VisualRange na vizuální pixely, neviditelné sloupce mají VisualRange = null.
        /// </summary>
        private Column[] _Columns;
        /// <summary>
        /// Pole těch sloupců této tabulky, které jsou alespoň částečně viditelné.
        /// Viditelné sloupce mají nastavenu hodnotu column.ColumnHeader.VisualRange na vizuální pixely, neviditelné sloupce mají VisualRange = null.
        /// </summary>
        private Column[] _VisibleColumns;
        #endregion
        #region Řádky tabulky - dvě oddělená pole řádků: a) všechny aktuálně dostupné řádky - pro práci s kolekcí řádků, b) pouze viditelné řádky - pro kreslení
        /// <summary>
        /// Pole všech řádků této tabulky, které mohou být zobrazeny, v tom pořadí, v jakém jsou zobrazovány.
        /// </summary>
        public Row[] Rows { get { this._RowsCheck(); return this._Rows; } }
        /// <summary>
        /// Pole viditelných řádků této tabulky, které jsou nyní zčásti nebo plně viditelné, v tom pořadí, v jakém jsou zobrazovány.
        /// </summary>
        public Row[] VisibleRows { get { this._VisibleRowsCheck(); return this._VisibleRows; } }
        /// <summary>
        /// Ověří a zajistí připravenost pole Rows
        /// </summary>
        private void _RowsCheck()
        {
            Row[] rows = this._Rows;
            bool heightValid = this._RowListHeightValid;
            if (rows == null)
            {
                rows = this.DataTable.RowsSorted;                                                  // Získat viditelné řádky, setříděné
                this._Rows = rows;
                heightValid = false;
            }
            if (!heightValid)
            {
                SequenceLayout.SequenceLayoutCalculate(rows);                                      // Napočítat jejich ISequenceLayout.Begin a .End
                this._RowListHeightValid = true;
                this._VisibleRows = null;
            }
        }
        /// <summary>
        /// Ověří a zajistí připravenost pole VisibleRows.
        /// Viditelné řádky mají korektně nastaveny aktuální souřadnice do row.RowHeader.VisualRange, neviditelné mají RowHeader.VisualRange == null.
        /// </summary>
        private void _VisibleRowsCheck()
        {
            if (this._VisibleRows != null) return;

            List<Row> visibleRows = new List<Row>();
            GridPosition rowsPositions = this.RowsPositions;
            Int32Range dataVisibleRange = rowsPositions.DataVisibleRange;                          // Rozmezí datových pixelů, které jsou viditelné
            foreach (Row row in this.Rows)
            {
                ISequenceLayout isl = row as ISequenceLayout;
                bool isRowVisible = SequenceLayout.IsItemVisible(isl, dataVisibleRange);           // Tento řádek je vidět?
                row.RowHeader.VisualRange = (isRowVisible ? rowsPositions.GetVisualPosition(isl) : null);
                if (isRowVisible)
                    visibleRows.Add(row);
            }
            this._VisibleRows = visibleRows.ToArray();
        }
        /// <summary>
        /// Metoda zajistí změnu výšky daného řádku, a návazné změny v interních strukturách plus překreslení
        /// </summary>
        /// <param name="row"></param>
        /// <param name="height">Požadovaná šířka, může se změnit</param>
        /// <returns></returns>
        public bool RowResizeTo(Row row, ref int height)
        {
            ISequenceLayout isl = row as ISequenceLayout;

            int heightOld = isl.Size;
            isl.Size = height;
            int heightNew = isl.Size;

            bool isChanged = (heightNew != heightOld);
            if (isChanged)
            {
                height = heightNew;
                this.Invalidate(InvalidateItem.RowHeight | InvalidateItem.Paint);
            }
            return isChanged;
        }
        /// <summary>
        /// true = hodnoty ISequenceLayout.Begin a End v řádcích jsou platné
        /// </summary>
        private bool _RowListHeightValid;
        /// <summary>
        /// Soupis všech aktuálně dostupných řádků, setříděný a vyfiltrovaný.
        /// </summary>
        private Row[] _Rows;
        /// <summary>
        /// Soupis aktuálně zobrazovaných řádků, vizuální objekty
        /// </summary>
        private Row[] _VisibleRows;
        #endregion
        #region Pozicování řádků svislé - pozicioner pro řádky, svislý scrollbar vpravo
        /// <summary>
        /// Inicializace objektů pro pozicování tabulek: TablesPositions, TablesScrollBar
        /// </summary>
        private void InitRowsPositions()
        {
            this._RowsPositions = new GridPosition(DefaultColumnHeaderHeight, 50, this._RowsPositionGetVisualSize, this._RowsPositionGetDataSize, this._GetVisualFirstPixel, this._SetVisualFirstPixel);

            this._RowsScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this._RowsScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(RowsScrollBar_ValueChange);
            this._RowsScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(RowsScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídící prvek pro Pozice řádků
        /// </summary>
        protected GridPosition RowsPositions { get { return this._RowsPositions; } }
        /// <summary>
        /// Vrací výšku prostoru pro řádky (=this.ClientSize.Height - RowsPositions.VisualFirstPixel (=výška this.DataTable.ColumnHeaderHeight))
        /// </summary>
        /// <returns></returns>
        private int _RowsPositionGetVisualSize()
        {
            return this.ClientSize.Height - this.RowsPositions.VisualFirstPixel;
        }
        /// <summary>
        /// Vrací výšku všech zobrazitelných datových řádků (samosebou, vyjma řádek ColumnHeader - to není datový řádek).
        /// </summary>
        /// <returns></returns>
        private int _RowsPositionGetDataSize()
        {
            Row[] rows = this.Rows;
            int count = rows.Length;
            return (count > 0 ? ((ISequenceLayout)rows[count - 1]).End : 0);
        }
        /// <summary>
        /// Vrací hodnotu prvního vizuálního pixelu, kde jsou zobrazována data.
        /// Jde o hodnotu this.DataTable.ColumnHeaderHeight = výška oblasti ColumnHeader
        /// </summary>
        /// <returns></returns>
        private int _GetVisualFirstPixel()
        {
            return this.DataTable.ColumnHeaderHeight;
        }
        /// <summary>
        /// Zapíše danou hodnotu jako pozici prvního vizuálního pixelu, kde jsou zobrazována data.
        /// Daná hodnota se vepisuje do this.DataTable.ColumnHeaderHeight = výška oblasti ColumnHeader.
        /// Po vepsání hodnoty může dojít k úpravě vložené hodnoty podle pravidel.
        /// </summary>
        /// <param name="value"></param>
        private void _SetVisualFirstPixel(int value)
        {
            this.DataTable.ColumnHeaderHeight = value;
        }
        /// <summary>
        /// Eventhandler pro událost změny pozice svislého scrollbaru = posun pole tabulek nahoru/dolů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RowsScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.RowsScrollBar.Value.Begin.Value;
            if (offset == this.RowsPositions.DataFirstPixel) return;
            this.RowsPositions.DataFirstPixel = offset;
            this.Invalidate(InvalidateItem.RowScroll);
        }
        /// <summary>
        /// RowsScrollBar : svislý posuvník vpravo od řádků
        /// </summary>
        protected GScrollBar RowsScrollBar { get { this._RowsScrollBarCheck(); return this._RowsScrollBar; } }
        /// <summary>
        /// Ověří a zajistí připravenost dat v objektu RowsScrollBar.
        /// Pokud je nastavena jeho neplatnost, pak provede načtení dat z pozicioneru.
        /// Tato akce nevyvolá žádný event.
        /// Aktualizují se hodnoty RowsScrollBar: Bounds, ValueTotal, Value, IsEnabled
        /// </summary>
        private void _RowsScrollBarCheck()
        {
            if (this._RowsScrollBarDataValid) return;

            if (this.RowsScrollBarVisible)
                this._RowsScrollBar.LoadFrom(this.RowsPositions, this.RowsScrollBarBounds, true);

            this._RowsScrollBarDataValid = true;
        }
        /// <summary>
        /// Datový pozicioner pro řádky
        /// </summary>
        private GridPosition _RowsPositions;
        /// <summary>
        /// Scrollbar pro řádky
        /// </summary>
        private GScrollBar _RowsScrollBar;
        /// <summary>
        /// true po naplnění RowsScrollBar platnými daty, false po invalidaci
        /// </summary>
        private bool _RowsScrollBarDataValid;
        #endregion
        #region Řádky tabulky - posuny, aktivní řádek, aktivní buňka, atd
        /// <summary>
        /// Posune oblast řádků tabulky podle dané akce.
        /// Vrací true = požadovaná akce byla provedena (tzn. akce byla vhodná pro seznam řádků) / false = akce se nás netýkala.
        /// </summary>
        /// <param name="action"></param>
        internal bool ProcessRowAction(InteractivePositionAction action)
        {
            bool isProcessed = false;
            switch (action)
            {
                case InteractivePositionAction.FirstRow:

                    isProcessed = true;
                    break;

            }

            return isProcessed;
        }
        #region Hot Row, Cell = řádek a buňka pod myší
        /// <summary>
        /// Aktuální Hot řádek = ten, nad kterým se nachází myš.
        /// </summary>
        public Row HotRow
        {
            get { return this._HotRow; }
            set { this.SetHotRow(value, EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Aktivní řádek
        /// </summary>
        private Row _HotRow;
        /// <summary>
        /// Aktivní buňka, obsahuje referenci na buňku pouze tehdy, pokud tabulka povoluje vybírat buňky (AllowSelectSingleCell). Jinak obsahuje null.
        /// </summary>
        public Cell HotCell
        {
            get { return (this.AllowSelectSingleCell ? this._HotCell : null); }
            set { this.SetHotCell(value, EventSourceType.ApplicationCode); }
        }
        /// <summary>
        /// Aktivní buňka, pouze pokud tabulka povoluje vybírat buňky
        /// </summary>
        private Cell _HotCell;
        /// <summary>
        /// Vrátí true, pokud daný řádek je Hot řádkem (pod myší) v této tabulce
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool IsHotRow(Row row)
        {
            return (row != null && this._HotRow != null && Object.ReferenceEquals(row, this._HotRow));
        }
        /// <summary>
        /// Vrátí true, pokud daná buňka je Hot buňkou (pod myší) v této tabulce
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsHotCell(Cell cell)
        {
            if (!this.AllowSelectSingleCell) return false;
            return (cell != null && this._ActiveCell != null && Object.ReferenceEquals(cell, this._ActiveCell));
        }
        /// <summary>
        /// Nastaví daný řádek jako Hot = ten pod myší, když myš jezdí nad tabulkou, vyvolá event HotRowChanged.
        /// Zajistí překreslení řádku.
        /// </summary>
        /// <param name="newHotRow"></param>
        /// <param name="eventSource"></param>
        protected void SetHotRow(Row newHotRow, EventSourceType eventSource)
        {
            // Nelze jako aktivní řádek vložit řádek z cizí tabulky:
            if (newHotRow != null && !Object.ReferenceEquals(newHotRow.Table, this.DataTable)) return;

            Row oldHotRow = this._HotRow;

            // Změna z null na null není změnou:
            if (newHotRow == null && oldHotRow == null) return;

            // Pokud jeden údaj je null a druhý není, je to změna:
            bool isChange = ((newHotRow == null) != (oldHotRow == null));
            if ((newHotRow != null) && (oldHotRow != null))
                isChange = !Object.ReferenceEquals(newHotRow, oldHotRow);

            if (!isChange) return;

            // Je tu změna:

            // Zajistíme překreslení starého i nového řádku (kvůli barevnosti):
            this.RepaintRow(this._HotRow);
            this.RepaintRow(newHotRow);

            this._HotRow = newHotRow;
            this.CallHotRowChanged(oldHotRow, newHotRow, eventSource);
        }
        /// <summary>
        /// Nastaví danou buňku jako Hot = ta pod myší, když myš jezdí nad tabulkou, vyvolá event HotCellChanged.
        /// </summary>
        /// <param name="newHotCell"></param>
        /// <param name="eventSource"></param>
        protected void SetHotCell(Cell newHotCell, EventSourceType eventSource)
        {
            // Nelze jako aktivní buňku vložit buňku z cizí tabulky:
            if (newHotCell != null && !Object.ReferenceEquals(newHotCell.Table, this.DataTable)) return;

            Cell oldHotCell = this._HotCell;

            // Změna z null na null není změnou:
            if (newHotCell == null && oldHotCell == null) return;

            // Pokud jeden údaj je null a druhý není, je to změna:
            bool isChange = ((newHotCell == null) != (oldHotCell == null));
            if ((newHotCell != null) && (oldHotCell != null))
                isChange = !Object.ReferenceEquals(newHotCell, oldHotCell);

            // Aktivovat řádek nově zadané buňky:
            Row newActiveRow = (newHotCell != null ? newHotCell.Row : null);
            this.SetHotRow(newActiveRow, eventSource);

            // Pokud tabulka nepovoluje práci s jednotlivými buňkami, pak můžeme skončit:
            if (!this.AllowSelectSingleCell) return;

            // Je tu změna:
            this._HotCell = newHotCell;
            this.CallHotCellChanged(oldHotCell, newHotCell, eventSource);
        }
        #endregion
        #region Active Row, Cell = řádek a buňka aktivní (po kliknutí, s kurzorem)
        /// <summary>
        /// Aktuální aktivní řádek = ten, který by měl focus, když by focus (this.HasFocus) měla aktuální tabulka.
        /// </summary>
        public Row ActiveRow
        {
            get { return this._ActiveRow; }
            set { this.SetActiveRow(value, EventSourceType.ApplicationCode, true); }
        }
        /// <summary>
        /// Aktivní řádek
        /// </summary>
        private Row _ActiveRow;
        /// <summary>
        /// Aktivní buňka, obsahuje referenci na buňku pouze tehdy, pokud tabulka povoluje vybírat buňky (AllowSelectSingleCell). Jinak obsahuje null.
        /// </summary>
        public Cell ActiveCell
        {
            get { return (this.AllowSelectSingleCell ? this._ActiveCell : null); }
            set { this.SetActiveCell(value, EventSourceType.ApplicationCode, true); }
        }
        /// <summary>
        /// Aktivní buňka, pouze pokud tabulka povoluje vybírat buňky
        /// </summary>
        private Cell _ActiveCell;
        /// <summary>
        /// Vrátí true, pokud daný řádek je aktivním řádkem v této tabulce
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool IsActiveRow(Row row)
        {
            return (row != null && this._ActiveRow != null && Object.ReferenceEquals(row, this._ActiveRow));
        }
        /// <summary>
        /// Vrátí true, pokud daná buňka je aktivní buňkou v této tabulce
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsActiveCell(Cell cell)
        {
            if (!this.AllowSelectSingleCell) return false;
            return (cell != null && this._ActiveCell != null && Object.ReferenceEquals(cell, this._ActiveCell));
        }
        /// <summary>
        /// Nastaví daný řádek jako aktivní, vyvolá event ActiveRowChanged, nastaví daný řádek tak aby byl vidět.
        /// Zajistí překreslení řádku.
        /// </summary>
        /// <param name="newActiveRow"></param>
        /// <param name="eventSource"></param>
        /// <param name="scrollToVisible"></param>
        protected void SetActiveRow(Row newActiveRow, EventSourceType eventSource, bool scrollToVisible)
        {
            // Nelze jako aktivní řádek vložit řádek z cizí tabulky:
            if (newActiveRow != null && !Object.ReferenceEquals(newActiveRow.Table, this.DataTable)) return;

            Row oldActiveRow = this._ActiveRow;

            // Změna z null na null není změnou:
            if (newActiveRow == null && oldActiveRow == null) return;

            // Pokud jeden údaj je null a druhý není, je to změna:
            bool isChange = ((newActiveRow == null) != (oldActiveRow == null));
            if ((newActiveRow != null) && (oldActiveRow != null))
                isChange = !Object.ReferenceEquals(newActiveRow, oldActiveRow);

            if (!isChange) return;

            // Je tu změna:

            // Zajistíme překreslení starého i nového řádku (kvůli barevnosti):
            this.RepaintRow(this._ActiveRow);
            this.RepaintRow(newActiveRow);

            this._ActiveRow = newActiveRow;
            this.CallActiveRowChanged(oldActiveRow, newActiveRow, eventSource);

            if (scrollToVisible)
                this.ScrollRowToVisibleArea(newActiveRow);
        }
        /// <summary>
        /// Nastaví danou buňku jako aktivní, případně vyvolá event ActiveRowChanged, nastaví daný řádek tak aby byl vidět
        /// </summary>
        /// <param name="newActiveRow"></param>
        /// <param name="eventSource"></param>
        /// <param name="scrollToVisible"></param>
        protected void SetActiveCell(Cell newActiveCell, EventSourceType eventSource, bool scrollToVisible)
        {
            // Nelze jako aktivní buňku vložit buňku z cizí tabulky:
            if (newActiveCell != null && !Object.ReferenceEquals(newActiveCell.Table, this.DataTable)) return;

            Cell oldActiveCell = this._ActiveCell;

            // Změna z null na null není změnou:
            if (newActiveCell == null && oldActiveCell == null) return;

            // Pokud jeden údaj je null a druhý není, je to změna:
            bool isChange = ((newActiveCell == null) != (oldActiveCell == null));
            if ((newActiveCell != null) && (oldActiveCell != null))
                isChange = !Object.ReferenceEquals(newActiveCell, oldActiveCell);

            // Aktivovat řádek nově zadané buňky:
            Row newActiveRow = (newActiveCell != null ? newActiveCell.Row : null);
            this.SetActiveRow(newActiveRow, eventSource, scrollToVisible);

            // Pokud tabulka nepovoluje práci s jednotlivými buňkami, pak můžeme skončit:
            if (!this.AllowSelectSingleCell) return;

            // Je tu změna:
            this._ActiveCell = newActiveCell;
            this.CallActiveCellChanged(oldActiveCell, newActiveCell, eventSource);

            if (scrollToVisible)
                this.ScrollColumnToVisibleArea(newActiveCell.Column);
        }
        /// <summary>
        /// Zajistí vyvolání metody Repaint pro RowHeader i pro všechny Cell.Control v daném řádku.
        /// Je vhodné volat po změně aktivního řádku (pro starý i nový aktivní řádek), a stejně i po změně Hot řádku (=ten pod myší).
        /// </summary>
        /// <param name="row"></param>
        protected void RepaintRow(Row row)
        {
            if (row == null) return;
            row.RowHeader.Repaint();
            foreach (Cell cell in row.Cells)
                cell.Control.Repaint();
        }
        /// <summary>
        /// Nastaví daný řádek tak, aby byl viditelný = tj. zcela, ne jen zčásti (pokud to jen trochu jde).
        /// </summary>
        /// <param name="row"></param>
        protected void ScrollRowToVisibleArea(Row row)
        {
            ISequenceLayout isl = row as ISequenceLayout;
            bool isChange = this.RowsPositions.ScrollDataToVisible(isl);
            if (isChange)
                this.Invalidate(InvalidateItem.RowScroll);
        }
        /// <summary>
        /// Nastaví daný sloupec tak, aby byl viditelný = tj. zcela, ne jen zčásti (pokud to jen trochu jde).
        /// </summary>
        /// <param name="column"></param>
        protected void ScrollColumnToVisibleArea(Column column)
        {
            ISequenceLayout isl = column as ISequenceLayout;
            bool isChange = this.Grid.ColumnsPositions.ScrollDataToVisible(isl);
            if (isChange)
                this.Invalidate(InvalidateItem.RowScroll);
        }
        /// <summary>
        /// true pokud je povoleno vybírat jednotlivé buňky tabulky, false pokud celý řádek.
        /// </summary>
        protected bool AllowSelectSingleCell { get { return this.DataTable.AllowSelectSingleCell; } }
        #endregion
        #endregion
        #region Invalidace, resety, refreshe
        /// <summary>
        /// Zajistí invalidaci položek po určité akci, která právě skončila.
        /// Volající v podstatě specifikuje, co změnil, a pošle tuto žádost s přiměřeným parametrem.
        /// Tabulka sama nejlíp ví, kam se daný údaj promítá, a co bude potřebovat přepočítat.
        /// </summary>
        /// <param name="items"></param>
        public void Invalidate(InvalidateItem items)
        {
            // Pokud bude nastaven tento bit OnlyForGrid, znamená to, že tuto invalidaci Grid do podřízených tabulek rozeslal omylem, nebudeme na ni reagovat.
            if (items.HasFlag(InvalidateItem.OnlyForGrid)) return;

            bool callGrid = false;

            if ((items & (InvalidateItem.TablePosition)) != 0)
            {   // Po změně umístění tabulky (nejde o změnu vnitřní velikosti) invalidujeme pouze tabulkový splitter:
                this._TableOuterLayoutValid = false;
            }

            if ((items & (InvalidateItem.TableSize)) != 0)
            {   // Po změně vnitřních rozměrů tabulky invalidujeme všechny viditelné prvky i tabulkový splitter:
                this._TableInnerLayoutValid = false;
                this._TableOuterLayoutValid = false;
                this._VisibleColumns = null;
                this._VisibleRows = null;
                this._RowsScrollBarDataValid = false;
                this._HeaderSplitterDataValid = false;
                this._ChildArrayValid = false;
            }

            if ((items & (InvalidateItem.TableHeight)) != 0)
            {   // Po změně výšky tabulky invelidujeme vnitřní prvky, ale ne viditelné řádky:
                this._TableInnerLayoutValid = false;
                this._VisibleColumns = null;
                this._VisibleRows = null;
                this._ChildArrayValid = false;
            }

            if ((items & (InvalidateItem.RowHeader)) != 0)
            {   // Po změně šířky RowHeader pozice Rovnitřního uspořádání (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů (RowHeader, ColumnHeader):
                this._TableInnerLayoutValid = false;
                this._VisibleColumns = null;
                this._ChildArrayValid = false;
                callGrid = true;
            }

            if ((items & (InvalidateItem.ColumnHeader)) != 0)
            {   // Po změně vnitřního uspořádání (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů (RowHeader, ColumnHeader):
                this._TableInnerLayoutValid = false;
                this._VisibleColumns = null;
                this._VisibleRows = null;
                this._RowsScrollBarDataValid = false;
                this._ChildArrayValid = false;
                // Změna RowHeader by mohla zajímat i ostatní tabulky:
                if ((items & (InvalidateItem.RowHeader)) != 0)
                    callGrid = true;
            }

            if ((items & (InvalidateItem.ColumnsCount | InvalidateItem.ColumnOrder)) != 0)
            {   // Po změně počtu nebo pořadí sloupců: zrušíme pole sloupců, vygeneruje se znovu:
                this._Columns = null;
                this._VisibleColumns = null;
                this._ChildArrayValid = false;
                callGrid = true;
            }

            if ((items & (InvalidateItem.ColumnWidth | InvalidateItem.ColumnScroll)) != 0)
            {   // Po změně šířky sloupce nebo scrollu sloupců: zrušíme pole viditelných sloupců, vygeneruje se znovu:
                this._VisibleColumns = null;
                this._ChildArrayValid = false;
                callGrid = true;
            }

            if ((items & (InvalidateItem.RowsCount | InvalidateItem.RowOrder)) != 0)
            {   // Po změně počtu nebo pořadí řádků: zrušíme pole řádků, vygeneruje se znovu:
                this._Rows = null;
                this._RowListHeightValid = false;
                this._VisibleRows = null;
                this._RowsScrollBarDataValid = false;
                this._ChildArrayValid = false;
            }

            if ((items & (InvalidateItem.RowHeight)) != 0)
            {   // Po změně výšky řádku: zrušíme příznak platnosti výšky v řádcích, a zrušíme pole viditelných řádků, vygeneruje se znovu:
                this._RowListHeightValid = false;
                this._VisibleRows = null;
                this._RowsScrollBarDataValid = false;
                this._ChildArrayValid = false;
            }
            if ((items & (InvalidateItem.RowScroll)) != 0)
            {   // Po scrollu řádků: zrušíme pole viditelných řádků, vygeneruje se znovu:
                this._VisibleRows = null;
                this._ChildArrayValid = false;
            }
            if ((items & (InvalidateItem.Paint)) != 0)
            {   // Požadavek na kreslení tabulky:
                this.Repaint();
            }

            // Předáme to šéfovi, pokud ho máme, a pokud to pro něj může být zajímavé, a pokud událost není určena jen pro naše (OnlyForTable) potřeby:
            if (this.HasGrid && callGrid && !items.HasFlag(InvalidateItem.OnlyForTable))
                this.Grid.Invalidate(items);
        }
        #endregion
        #region HeaderSplitter : splitter umístěný pod hlavičkou sloupců, je součástí GTable.Items
        /// <summary>
        /// Inicializuje objekt _HeaderSplitter.
        /// </summary>
        protected void InitHeaderSplitter()
        {
            this._HeaderSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4, LinkedItemPrevMinSize = 50, LinkedItemNextMinSize = 50, IsResizeToLinkItems = true };
            this._HeaderSplitter.ValueSilent = this.Bounds.Bottom;
            this._HeaderSplitter.ValueChanging += new GPropertyChanged<int>(_HeaderSplitter_LocationChange);
            this._HeaderSplitter.ValueChanged += new GPropertyChanged<int>(_HeaderSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler události _TableSplitter.LocationChanged (došlo nebo stále dochází ke změně pozice splitteru pod tabulkou)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HeaderSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int value = this._HeaderSplitter.Value;
            this.DataTable.ColumnHeaderHeight = value;               // Tady dojde ke kompletnímu vyhodnocení pravidel pro výšku ColumnHeader (Minimum, Default, Range)
            e.CorrectValue = this.DataTable.ColumnHeaderHeight;      // Pokud požadovaná hodnota (value) nebyla akceptovatelná, pak correctValue je hodnota přípustná
            if (e.IsChangeValue)
            {
                this.Invalidate(InvalidateItem.RowScroll);
            }
        }
        /// <summary>
        /// TableSplitter = Splitter mezi ColumnHeader a RowArea
        /// Tento Splitter je součástí this.Childs, protože by neměl odcházet mimo this.GTable (na rozdíl od TableSplitter).
        /// </summary>
        protected GSplitter HeaderSplitter { get { this._HeaderSplitterCheck(); return this._HeaderSplitter; } }
        /// <summary>
        /// Zajistí platnost dat v HeaderSplitter
        /// </summary>
        private void _HeaderSplitterCheck()
        {
            if (this._HeaderSplitterDataValid) return;
            Rectangle bounds = this.HeaderSplitterBounds;
            this._HeaderSplitter.LoadFrom(bounds, RectangleSide.Bottom, true);
            this._HeaderSplitterDataValid = true;
        }
        /// <summary>
        /// Platnost dat v HeaderSplitter
        /// </summary>
        private bool _HeaderSplitterDataValid;
        /// <summary>
        /// HeaderSplitter
        /// </summary>
        private GSplitter _HeaderSplitter;
        #endregion
        #region TableSplitter :  splitter umístěný dole pod tabulkou, je součástí Parenta
        /// <summary>
        /// Inicializuje objekt _TableSplitter.
        /// </summary>
        protected void InitTableSplitter()
        {
            this._TableSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4, LinkedItemPrevMinSize = 50, LinkedItemNextMinSize = 50, IsResizeToLinkItems = true };
            this._TableSplitter.ValueSilent = this.Bounds.Bottom;
            this._TableSplitter.ValueChanging += new GPropertyChanged<int>(_TableSplitter_LocationChange);
            this._TableSplitter.ValueChanged += new GPropertyChanged<int>(_TableSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler události _TableSplitter.LocationChanged (došlo nebo stále dochází ke změně pozice splitteru od tabulkou)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            // Vypočteme výšku tabulky:
            int value = this._TableSplitter.Value - this.Bounds.Top;
            this.DataTable.Height = value;                 // Tady dojde ke kompletnímu vyhodnocení pravidel pro výšku Table (Minimum, Default, Range)
            e.CorrectValue = this.DataTable.Height;        // Pokud požadovaná hodnota (value) nebyla akceptovatelná, pak correctValue je hodnota přípustná
            if (e.IsChangeValue)
            {
                this.Grid.Invalidate(InvalidateItem.TableHeight);
            }
        }
        /// <summary>
        /// TableSplitter = Splitter dole pod tabulkou.
        /// Tento Splitter není součástí this.Childs (protože pak by byl omezen do this.Bounds), je součástí Childs nadřízeného prvku (GGrid), protože pak se může pohybovat v jeho prostoru.
        /// </summary>
        internal GSplitter TableSplitter { get { this._TableSplitterCheck(); return this._TableSplitter; } }
        /// <summary>
        /// Zajistí platnost dat v TableSplitter
        /// </summary>
        private void _TableSplitterCheck()
        {
            if (this._TableSplitterDataValid) return;
            Rectangle bounds = this.TableSplitterBounds;
            this._TableSplitter.LoadFrom(bounds, RectangleSide.Bottom, true);
            this._TableSplitterDataValid = true;
        }
        /// <summary>
        /// Platnost dat v TableSplitter
        /// </summary>
        private bool _TableSplitterDataValid;
        /// <summary>
        /// TableSplitter
        /// </summary>
        private GSplitter _TableSplitter;
        #endregion
        #region Childs items : pole všech prvků v tabulce (jednotlivé headery, jednotlivé buňky, jednotlivé spittery, a scrollbar řádků)
        /// <summary>
        /// Pole sub-itemů v tabulce.
        /// Tabulka obsahuje: hlavičku tabulky, hlavičky viditelných sloupců, hlavičky viditelných řádků, buňky viditelných řádků a sloupců, 
        /// splitery hlaviček sloupců a hlaviček řádků (pokud je povoleno jejich resize), a scrollbar řádků (pokud je viditelný).
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this._ChildArrayCheck(); return this.ChildList; } }
        /// <summary>
        /// Validita prvků v poli ChildItems
        /// </summary>
        private bool _ChildArrayValid;
        /// <summary>
        /// Zajistí platnost pole sub-itemů.
        /// </summary>
        private void _ChildArrayCheck()
        {
            if (this._ChildArrayValid) return;
            this.ChildList.Clear();
            // Něco k pořadí vkládání prvků do Items: dospodu dáme to, co by mělo být "vespodu" = obsah buněk. Nad ně dáme Headers a na ně Splitters:
            this._ChildItemsAddRowsContent();                        // Řádky: buňky plus záhlaví, ale ne oddělovače
            this._ChildItemsAddColumnHeaders();                      // Záhlaví sloupců (TableHeader + ColumnHeaders)
            this._ChildItemsAddColumnSplitters();                    // Oddělovače sloupců, které to mají povoleno
            this._ChildItemsAddHeaderSplitter();                     // Oddělovač pod hlavičkami sloupců (řídí výšku záhlaví)
            this._ChildItemsAddRowsSplitters();                      // Řádky: oddělovače řádků, pokud je povoleno
            this._ChildItemsAddRowsScrollBar();                      // Scrollbar řádků, pokud je viditelný
            this._ChildArrayValid = true;
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechna záhlaví sloupců (tedy TableHeader + VisibleColumns.ColumnHeader).
        /// Nepřidává splittery: ani mezi sloupci, ani pod Headers.
        /// </summary>
        protected void _ChildItemsAddColumnHeaders()
        {
            GTableHeader tableHeader = this.DataTable.TableHeader;
            tableHeader.Bounds = this.TableHeaderBounds;
            this.ChildList.Add(tableHeader);

            Rectangle headerBounds = this.ColumnHeaderBounds;
            foreach (Column column in this.VisibleColumns)
            {
                GColumnHeader columnHeader = column.ColumnHeader;
                Int32Range visualRange = columnHeader.VisualRange;
                columnHeader.Bounds = Int32Range.GetRectangle(visualRange, headerBounds);
                this.ChildList.Add(columnHeader);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá splittery, které se nacházejí za sloupci. Přidává i splitter za posledním sloupcem.
        /// Přidává je tehdy, když header má svůj splitter viditelný (ColumnSplitterVisible), tzn. pokud daný sloupec je možno resizovat.
        /// </summary>
        protected void _ChildItemsAddColumnSplitters()
        {
            GTableHeader tableHeader = this.DataTable.TableHeader;
            if (tableHeader.ColumnSplitterVisible)
                this.ChildList.Add(tableHeader.ColumnSplitter);

            foreach (Column column in this.VisibleColumns)
            {
                GColumnHeader columnHeader = column.ColumnHeader;
                if (columnHeader.ColumnSplitterVisible)
                    this.ChildList.Add(columnHeader.ColumnSplitter);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá obsah za všechny viditelné řádky (VisibleRows).
        /// Obsah = RowHeader + Cells.
        /// Nepřidává vodorovný Splitter pod RowHeader.
        /// </summary>
        protected void _ChildItemsAddRowsContent()
        {
            foreach (Row row in this.VisibleRows)
                this._ChildItemsAddRowContent(row);
        }
        /// <summary>
        /// Do pole this.ChildList přidá obsah jednoho daného řádku, obsah je: RowHeader + za každý viditelný sloupec (VisibleColumns) pak obsah vizuální buňky (row[column.ColumnId].Control).
        /// </summary>
        /// <param name="row"></param>
        protected void _ChildItemsAddRowContent(Row row)
        {
            Rectangle rowHeaderBounds = this.RowHeaderBounds;
            Rectangle rowAreaBounds = this.RowAreaBounds;
            GRowHeader rowHeader = row.RowHeader;
            Int32Range rowVisualRange = rowHeader.VisualRange;

            // Něco k pořadí vkládání prvků do Items: dospodu dáme to, co by mělo být "vespodu" = obsah buněk. Nad ně dáme Headers a na ně Splitters:
            foreach (Column column in this.VisibleColumns)
            {
                Int32Range columnVisualRange = column.ColumnHeader.VisualRange;
                GCell cell = row[column.ColumnId].Control;
                cell.Bounds = Int32Range.GetRectangle(columnVisualRange, rowVisualRange);
                this.ChildList.Add(cell);
            }

            rowHeader.Bounds = Int32Range.GetRectangle(rowHeaderBounds, rowVisualRange);
            this.ChildList.Add(rowHeader);
        }
        /// <summary>
        /// Do pole this.ChildList přidá HeaderSplitter, pokud tabulka povoluje změnu výšky záhlaví (DataTable.AllowColumnHeaderResize)
        /// </summary>
        protected void _ChildItemsAddHeaderSplitter()
        {
            if (this.DataTable.AllowColumnHeaderResize)
                this.ChildList.Add(this.HeaderSplitter);
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechny RowSplittery
        /// </summary>
        protected void _ChildItemsAddRowsSplitters()
        {
            foreach (Row row in this.VisibleRows)
                this._ChildItemsAddRowSplitters(row);
        }
        /// <summary>
        /// Do pole this.ChildList přidá RowSplitter pro daný řádek
        /// </summary>
        protected void _ChildItemsAddRowSplitters(Row row)
        {
            GRowHeader rowHeader = row.RowHeader;
            if (rowHeader.RowSplitterVisible)
                this.ChildList.Add(rowHeader.RowSplitter);
        }
        /// <summary>
        /// Do pole this.ChildList přidá RowsScrollBar, pokud je viditelný (RowsScrollBarVisible)
        /// </summary>
        protected void _ChildItemsAddRowsScrollBar()
        {
            if (this.RowsScrollBarVisible)
            {
                this.RowsScrollBar.Bounds = this.RowsScrollBarBounds;
                this.ChildList.Add(this.RowsScrollBar);
            }
        }
        #endregion
        #region ISequenceLayout - adapter na DataTable jako implementační objekt
        int ISequenceLayout.Begin { get { return this._SequenceLayout.Begin; } set { this._SequenceLayout.Begin = value; } }
        int ISequenceLayout.Size { get { return this._SequenceLayout.Size; } set { this._SequenceLayout.Size = value; } }
        int ISequenceLayout.End { get { return this._SequenceLayout.End; } }
        private ISequenceLayout _SequenceLayout { get { return (ISequenceLayout)this.DataTable; } }
        #endregion
        #region TimeAxis
        /// <summary>
        /// Je voláno z eventhandleru TimeAxis.ValueChange, při/po změně hodnoty Value na některé TimeAxis na sloupci columnId v this.Columns.
        /// Metoda zajistí synchronizaci okolních tabulek (kromě zdejší, ta je v tomto pohledu Master).
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v TÉTO tabulce.
        /// </summary>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            if (this.HasGrid)
                this.Grid.OnChangeTimeAxis(this.TableId, columnId, e);
        }
        /// <summary>
        /// Není voláno z GGrid (ale bývalo), po změně hodnoty Value na některé TimeAxis na sloupci columnId (v this.Columns), ale na jiné tabulce než je this tabulka.
        /// Tato tabulka je tedy Slave, a má si změnit svoji hodnotu bez toho, aby vyvolala další event o změně hodnoty.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v JINÉ tabulce.
        /// </summary>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void RefreshTimeAxis(int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            Column column = this.Columns.FirstOrDefault(c => c.ColumnId == columnId);
            if (column == null || !column.UseTimeAxis) return;
            column.ColumnHeader.RefreshTimeAxis(e);
        }
        /// <summary>
        /// Vrátí ITimeConvertor pro daný columnId.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        internal ITimeConvertor GetTimeConvertor(int columnId)
        {
            Column column = this.Columns.FirstOrDefault(c => c.ColumnId == columnId);
            if (column == null || !column.UseTimeAxis) return null;
            return column.ColumnHeader.TimeConvertor;
        }
        #endregion
        #region Interaktivita vlastní GTable
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {   // Když už tady musí být override AfterStateChanged() (to kdyby to někoho napadlo), tak MUSÍ volat base metodu!
            base.AfterStateChanged(e);
        }
        /// <summary>
        /// Jakmile myš opouští tabulku, pak resetuje informaci o HotRow a HotCell:
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            if (this._HotCell != null)
                this.CellMouseLeave(e, null);
            if (this._HotRow != null)
                this.SetHotRow(null, EventSourceType.InteractiveChanged);
        }
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
        }
        #endregion
        #region Interaktivita z jednotlivých objektů tabulky do grafické tabulky, a dále
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví tabulky.
        /// </summary>
        /// <param name="e"></param>
        public void TableHeaderClick(GInteractiveChangeStateArgs e)
        {
            // Tady by se asi mohl resetovat filtr, nebo nabídnout reset Rows[].IsSelected, atd...
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví sloupce.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="column"></param>
        public void ColumnHeaderClick(GInteractiveChangeStateArgs e, Column column)
        {   // Třídění podle sloupce, pokud ten to dovoluje:
            if (column != null)
            {
                column.Table.ColumnHeaderClick(column);
                if (column.AllowColumnSortByClick && column.Table.AllowColumnSortByClick)
                {
                    if (column.SortChange())
                        this.Repaint();
                }
            }
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví řádku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="row">řádek</param>
        public void RowHeaderClick(GInteractiveChangeStateArgs e, Row row)
        {
            if (row != null && row.Table.AllowRowSelectByClick)
            {
                row.Table.RowHeaderClick(row);
                if (row.Table.AllowRowSelectByClick)
                {
                    row.SelectedChange();
                    this.Repaint();
                }
            }
        }
        /// <summary>
        /// Provede se poté, kdy uživatel vstoupí s myší nad určitou buňkou.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        public void CellMouseEnter(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetHotCell(cell, EventSourceType.InteractiveChanged);
            this.CallCellMouseEnter(cell, EventSourceType.InteractiveChanged);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel vystoupí myší z určité buňky jinam.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        public void CellMouseLeave(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetHotCell(null, EventSourceType.InteractiveChanged);
            this.CallCellMouseLeave(cell, EventSourceType.InteractiveChanged);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        public void CellClick(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetActiveCell(cell, EventSourceType.InteractiveChanged, true);
            this.CallActiveCellClick(cell, EventSourceType.InteractiveChanged);
        }
        #endregion
        #region Draw : kreslení vlastní tabulky
        protected override void Draw(GInteractiveDrawArgs e)
        {
            // GTable kreslí pouze svoje vlastní pozadí (a to by si ještě měla rozmyslet, kolik ho bude, než začne malovat úplně celou plochu :-) ):
            if (this.DataTable == null || this.DataTable.ColumnsCount == 0)
                base.Draw(e);

            // Všechno ostatní (záhlaví sloupců, řádky, scrollbary, splittery) si malují Childs samy.
        }
        #endregion
        #region Podpora pro kreslení obsahu řádků (pozadí, gridlines)
        /// <summary>
        /// Metoda vykreslí pozadí (background) pro danou buňku jednoho řádku.
        /// Metoda nkreslí GridLines.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell"></param>
        /// <param name="boundsAbsolute"></param>
        internal void DrawRowBackground(GInteractiveDrawArgs e, Cell cell, Rectangle boundsAbsolute)
        {
            float? effect3d = null;
            Color backColor = this.GetBackColorForCell(cell, ref effect3d);
            GPainter.DrawEffect3D(e.Graphics, boundsAbsolute, backColor, System.Windows.Forms.Orientation.Horizontal, effect3d);
            // this.Host.FillRectangle(e.Graphics, boundsAbsolute, backColor);
        }
        /// <summary>
        /// Metoda vykreslí linky ohraničující danou buňku jednoho řádku.
        /// Vykresluje se v podstatě jen dolní linka (jako Horizontal) a linka vpravo (Vertical).
        /// Horní a levá linka se nekreslí, protože u prvního řádku / sloupce postačí Header, a u dalších řádků / sloupců je vykreslená linka z předešlého řádku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell"></param>
        /// <param name="boundsAbsolute"></param>
        internal void DrawRowGridLines(GInteractiveDrawArgs e, Cell cell, Rectangle boundsAbsolute)
        {
            VisualStyle style = ((IVisualMember)this.DataTable).Style;
            Color color = style.BorderColor ?? Skin.Grid.BorderLineColor;
            BorderLinesType linesType = style.BorderLines ?? Skin.Grid.BorderLineType;

            this.Host.DrawBorder(e.Graphics, boundsAbsolute, color, linesType, true);
        }
        /// <summary>
        /// Vrací barvu pro vykreslení pozadí daného řádku.
        /// Akceptuje: aktivní řádek, focus, selected.
        /// Dále vyhodnotí VisualStyle řádku.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Color GetBackColorForRow(Row row, ref float? effect3D)
        {
            if (row == null) return Skin.Grid.RowBackColor;
            return GetBackColor(((IVisualMember)row).Style, row, null, ref effect3D);
        }
        /// <summary>
        /// Vrací barvu pro vykreslení pozadí dané buňky.
        /// Akceptuje: aktivní řádek, buňku, focus, selected.
        /// Dále vyhodnotí VisualStyle řádku.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Color GetBackColorForCell(Cell cell, ref float? effect3D)
        {
            if (cell == null) return Skin.Grid.RowBackColor;
            return GetBackColor(((IVisualMember)cell).Style, cell.Row, cell, ref effect3D);
        }
        /// <summary>
        /// Vrátí barvu pozadí pro danou definici a vizuální styl
        /// </summary>
        /// <param name="style">Vizuální styl, obsahuje mj. barvy</param>
        /// <param name="row">Řádek</param>
        /// <param name="cell">Buňka</param>
        /// <param name="effect3D">Určení 3D stylu</param>
        /// <returns></returns>
        public Color GetBackColor(VisualStyle style, Row row, Cell cell, ref float? effect3D)
        {
            // Základní barva pozadí prvku vychází z barvy standardní, nebo Selected, podle stavu row.IsSelected; primárně z dodaného vizuálního stylu, sekundárně z palety:
            Color baseColor = (row.IsSelected ? (style.SelectedBackColor ?? Skin.Grid.SelectedRowBackColor) : (style.BackColor ?? Skin.Grid.RowBackColor));

            // Základní barva je poté morfována do barvy Active v poměru, který vyjadřuje aktivitu řádku, buňky, a focus tabulky, a stav HotMouse:
            float ratio = this.GetMorphRatio(row, cell, ref effect3D);

            // Pokud prvek není aktivní (aktivní řádek ani aktivní buňka), pak má základní barvu - bez morphování:
            if (ratio == 0f) return baseColor;

            // Pokud je aktuální prvek v nějakém aktivním stavu (má kladné ratio pro morfing barvy):
            Color activeColor = style.ActiveBackColor ?? Skin.Grid.ActiveCellBackColor;
            return baseColor.Morph(activeColor, ratio);
        }
        /// <summary>
        /// Vrací barvu pro vykreslení textu daného řádku.
        /// Akceptuje: aktivní řádek, focus, selected.
        /// Dále vyhodnotí VisualStyle řádku.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Color GetTextColorForRow(Row row)
        {
            if (row == null) return Skin.Grid.RowTextColor;
            return GetTextColor(((IVisualMember)row).Style, row, null);
        }
        /// <summary>
        /// Vrací barvu pro vykreslení textu dané buňky.
        /// Akceptuje: aktivní řádek, buňku, focus, selected.
        /// Dále vyhodnotí VisualStyle řádku.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Color GetTextColorForCell(Cell cell)
        {
            if (cell == null) return Skin.Grid.RowTextColor;
            return GetTextColor(((IVisualMember)cell).Style, cell.Row, cell);
        }
        /// <summary>
        /// Vrátí barvu pozadí pro danou definici a vizuální styl
        /// </summary>
        /// <param name="style">Vizuální styl, obsahuje mj. barvy</param>
        /// <param name="row">Řádek</param>
        /// <param name="cell">Buňka</param>
        /// <returns></returns>
        public Color GetTextColor(VisualStyle style, Row row, Cell cell)
        {
            // Základní barva prvku je podle jeho stavu isSelected, primárně ze stylu prvku, při nezadání barvy pak z odpovídající položky Skinu pro Grid:
            Color baseColor = (row.IsSelected ? (style.SelectedTextColor ?? Skin.Grid.SelectedRowTextColor) : (style.TextColor ?? Skin.Grid.RowTextColor));

            // Základní barva je poté morfována do barvy Active v poměru, který vyjadřuje aktivitu řádku, buňky, a focus tabulky, a stav HotMouse:
            float ratio = this.GetMorphRatio(row, cell);

            // Pokud prvek není aktivní (aktivní řádek ani aktivní buňka), pak má základní barvu - bez morphování:
            if (ratio == 0f) return baseColor;

            // Pokud je aktuální prvek v nějakém aktivním stavu (má kladné ratio pro morfing barvy):
            Color activeColor = style.ActiveTextColor ?? Skin.Grid.ActiveCellTextColor;
            return baseColor.Morph(activeColor, ratio);
        }
        /// <summary>
        /// Vrátí ratio pro morphing základní barvy (BackColor, TextColor) pro daný řádek a buňku, v závislosti na jejich stavu Hot, Active a Focus.
        /// Akceptuje i hodnotu AllowSelectSingleCell. Pokud je true, pak se více preferuje zvýraznění buňky, pokud je false pak je barva stejná pro celý řádek.
        /// Pokud je AllowSelectSingleCell = true, a předaná buňka je null, pak se vrací ratio pro barvu oslabenou pro neaktivní buňky aktivního řádku.
        /// Vstupy: řádek musí být zadán, buňka může být null. Pokud řádek není zadán, vrací se 0.00f.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        protected float GetMorphRatio(Row row, Cell cell)
        {
            float? effect3D = null;
            return this.GetMorphRatio(row, cell, ref effect3D);
        }
        /// <summary>
        /// Vrátí ratio pro morphing základní barvy (BackColor, TextColor) pro daný řádek a buňku, v závislosti na jejich stavu Hot, Active a Focus.
        /// Akceptuje i hodnotu AllowSelectSingleCell. Pokud je true, pak se více preferuje zvýraznění buňky, pokud je false pak je barva stejná pro celý řádek.
        /// Pokud je AllowSelectSingleCell = true, a předaná buňka je null, pak se vrací ratio pro barvu oslabenou pro neaktivní buňky aktivního řádku.
        /// Vstupy: řádek musí být zadán, buňka může být null. Pokud řádek není zadán, vrací se 0.00f.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="effect3D"></param>
        /// <returns></returns>
        protected float GetMorphRatio(Row row, Cell cell, ref float? effect3D)
        {
            float ratio = 0;
            if (row == null) return ratio;

            // Pokud řádek není Hot ani Active, pak vracíme ratio = 0, barva bude normální bez nějaké aktivity:
            bool rowIsMouseHot = row.IsMouseHot;
            bool rowIsActive = row.IsActive;
            if (!rowIsMouseHot && !rowIsActive) return ratio;

            // Řádek je buď aktivní, nebo alespoň MouseHot, bude mít ratio > 0 a bude mít vhodný 3D efekt:
            //  aktivní řádek (s kurzorem) má 3D efekt mírně dospodu, kdežto MouseHot řádek má 3D efekt jen lehce nahoru:
            effect3D = (rowIsActive ? EFFECT_3D_ACTIVE_ROW : EFFECT_3D_MOUSEHOT_ROW);
            
            // Barva řádku vychází z mnoha faktorů, z nichž HasFocus je první k řešení:
            if (this.HasFocus)
            {   // Tabulka s Focusem má větší reakce na aktivitu:
                // Pokud tabulka umožňuje vybírat jednotlivé buňky, ale buňka do této metody není předána (nebo není v tom stavu, v jakém je řádek), pak barva jde na 75%:
                if (this.AllowSelectSingleCell)
                {   // Tabulka umožňuje práci s jednotlivými buňkami:
                    if (cell != null)
                    {   // Je zadaná konkrétní buňka => tak zjistíme, zda buňka sama je MouseHot nebo Active, nebo zda je Active nebo Hot její řádek:
                        bool cellIsMouseHot = row.IsMouseHot;
                        bool cellIsActive = row.IsActive;
                        ratio = (cellIsActive ? MORPH_RATIO_ACTIVE_CELL : 
                                (rowIsActive ? MORPH_RATIO_ACTIVE_ROW : 
                                (cellIsMouseHot ? MORPH_RATIO_MOUSEHOT_CELL : 
                                (rowIsMouseHot ? MORPH_RATIO_MOUSEHOT_ROW : 0f))));
                    }
                    else
                    {   // Buňka není předaná, ale tabulka podporuje práci s buňkami => v tom případě generujeme barvu pro neaktivní buňky řádku, jejichž barva je 75% plné barvy:
                        ratio = (rowIsActive ? MORPH_RATIO_ACTIVE_ROW :
                                (rowIsMouseHot ? MORPH_RATIO_MOUSEHOT_CELL : 0f));
                    }
                }
                else
                {   // Tabulka nepodporuje práci s jednotlivými buňkami => generujeme barvu pro plnobarevný řádek (jakoby všechny buňky řádku byly aktivní nebo hotmouse):
                    ratio = (rowIsActive ? MORPH_RATIO_ACTIVE_CELL : MORPH_RATIO_MOUSEHOT_CELL);
                }
            }
            else
            {   // Pokud tabulka nemá focus, pak aktivní řádek má 25% plné barvy, řádek MouseHot má 5% a neřešíme aktivní buňky:
                ratio = (rowIsActive ? MORPH_RATIO_ACTIVE_NOFOCUS : MORPH_RATIO_MOUSEHOT_NOFOCUS);
            }

            return ratio;
        }
        protected const float MORPH_RATIO_ACTIVE_CELL = 1.00f;
        protected const float MORPH_RATIO_ACTIVE_ROW = 0.75f;
        protected const float MORPH_RATIO_MOUSEHOT_CELL = 0.25f;
        protected const float MORPH_RATIO_MOUSEHOT_ROW = 0.10f;
        protected const float MORPH_RATIO_ACTIVE_NOFOCUS = 0.25f;
        protected const float MORPH_RATIO_MOUSEHOT_NOFOCUS = 0.05f;
        protected const float EFFECT_3D_ACTIVE_ROW = -0.45f;
        protected const float EFFECT_3D_MOUSEHOT_ROW = 0.30f;
        #endregion
        #region Defaultní hodnoty
        /// <summary>
        /// Výchozí výška oblasti ColumnHeader
        /// </summary>
        public int DefaultColumnHeaderHeight { get { return 35; } }
        /// <summary>
        /// Výchozí šířka sloupce RowHeader
        /// </summary>
        public int DefaultRowHeaderWidth { get { return 25; } }
        #endregion
        #region Převolávač událostí z GTable do DataTable
        protected void CallActiveRowChanged(Row oldActiveRow, Row newActiveRow, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallActiveRowChanged(oldActiveRow, newActiveRow, eventSource, !this.IsSupressedEvent);
        }
        protected void CallHotRowChanged(Row oldHotRow, Row newHotRow, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallHotRowChanged(oldHotRow, newHotRow, eventSource, !this.IsSupressedEvent);
        }
        protected void CallHotCellChanged(Cell oldHotCell, Cell newHotCell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallHotCellChanged(oldHotCell, newHotCell, eventSource, !this.IsSupressedEvent);
        }
        protected void CallActiveCellChanged(Cell oldActiveCell, Cell newActiveCell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallActiveCellChanged(oldActiveCell, oldActiveCell, eventSource, !this.IsSupressedEvent);
        }
        protected void CallCellMouseEnter(Cell cell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallCellMouseEnter(cell, eventSource, !this.IsSupressedEvent);
        }
        protected void CallCellMouseLeave(Cell cell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallCellMouseLeave(cell, eventSource, !this.IsSupressedEvent);
        }
        protected void CallActiveCellClick(Cell cell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallActiveCellClick(cell, eventSource, !this.IsSupressedEvent);
        }
        #endregion
    }
    #region Třídy GHeader : GTableHeader, GColumnHeader, GRowHeader
    /// <summary>
    /// GHeader : vizuální třída pro zobrazování záhlaví tabulky, a předek pro třídy zobrazující záhlaví sloupce a řádku.
    /// </summary>
    public abstract class GHeader : InteractiveDragObject, IInteractiveItem
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor
        /// </summary>
        protected GHeader()
        { }
        /// <summary>
        /// Souřadnice headeru.
        /// Vložením nové hodnoty do souřadnic dojde i k správnému umístění Splitteru, který je součástí tohoto headeru, a případně i časové osy (pokud je součástí ColumnHeaderu).
        /// </summary>
        public override Rectangle Bounds { get { return base.Bounds; } set { base.Bounds = value; } }        // tahle property je tu jen kvůli XML komentáři, který je odlišný od base třídy :-)
        protected override void OnBoundsChanged(GPropertyChangeArgs<Rectangle> args)
        {
            base.OnBoundsChanged(args);
            this.SetChildBounds(args.NewValue);
        }
        /// <summary>
        /// Potomek by měl nastavit souřadnice svých Childs objektů, a/nebo svého splitteru, na základě nových souřadnic this headeru.
        /// </summary>
        /// <param name="newBounds"></param>
        protected virtual void SetChildBounds(Rectangle newBounds) { }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Grid (grafický), do kterého patří zdejší tabulka
        /// </summary>
        protected GGrid OwnerGGrid { get { return ((this.OwnerTable != null && this.OwnerTable.HasGTable) ? this.OwnerTable.GTable.Grid : null); } }
        /// <summary>
        /// Tabulka (grafická), do které patří toto záhlaví
        /// </summary>
        protected GTable OwnerGTable { get { return ((this.OwnerTable != null && this.OwnerTable.HasGTable) ? this.OwnerTable.GTable : null); } }
        /// <summary>
        /// Datová tabulka, do které this záhlaví patří.
        /// Je k dispozici pro všechny tři typy záhlaví (Table, Column, Row).
        /// </summary>
        protected abstract Table OwnerTable { get; }
        /// <summary>
        /// Typ záhlaví. Potomek musí přepsat na správnou hodnotu.
        /// </summary>
        protected abstract TableAreaType HeaderType { get; }
        #endregion
        #region Podpora kreslení
        protected override bool CanDrag { get { return false; } }
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this.OwnerTable.TableName == "směny")
            { }

            // Clip() mi zajistí, že při pixelovém posunu záhlaví (sloupce, řádky) bude záhlaví vykresleno jen do příslušné části vymezeného prostoru pro danou oblast.
            // Grafická organizace GTable není členěna nijak výrazně strukturovaně => GTable obsahuje jako Child jednotlivé prvky (GHeader, GColumn),
            //  které mají svoje souřadnice relativní k GTable, ale mají se zobrazovat "oříznuté" do patřičných oblastí v GTable.
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
            {
                // Clip() ale provedeme jen pro Standard vrstvu; protože v ostatních vrstvách se provádí Dragging, a ten má být neomezený:
                Rectangle areaAbsoluteBounds = this.OwnerGTable.GetAbsoluteBoundsForArea(this.HeaderType);
                e.GraphicsClipWith(areaAbsoluteBounds, true);
            }

            int? opacity = (e.DrawLayer == GInteractiveDrawLayer.Standard ? (int?)null : (int?)128);
            this.DrawHeader(e, boundsAbsolute, opacity);
        }
        protected override void DrawAsGhost(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            // Clip() mi zajistí, že při pixelovém posunu záhlaví (sloupce, řádky) bude záhlaví vykresleno jen do příslušné části vymezeného prostoru pro danou oblast.
            // Grafická organizace GTable není členěna nijak výrazně strukturovaně => GTable obsahuje jako Child jednotlivé prvky (GHeader, GColumn),
            //  které mají svoje souřadnice relativní k GTable, ale mají se zobrazovat "oříznuté" do patřičných oblastí v GTable.
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                // Clip() ale provedeme jen pro Standard vrstvu; protože v ostatních vrstvách se provádí Dragging, a ten má být neomezený:
                e.GraphicsClipWith(this.OwnerGTable.GetAbsoluteBoundsForArea(this.HeaderType), true);

            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                e.Graphics.FillRectangle(Brushes.DarkGray, boundsAbsolute);
            this.DrawHeader(e, boundsAbsolute, 128);
        }
        /// <summary>
        /// Vykreslí podklad prostoru pro záhlaví.
        /// Bázová třída GHeader vykreslí pouze pozadí, pomocí metody GPainter.DrawColumnHeader()
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected virtual void DrawHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            GPainter.DrawColumnHeader(e.Graphics, boundsAbsolute, ColorPalette.ButtonBackEnableColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, null, opacity);
        }
        #endregion
        #region Drag - podpora pro přesunutí this headeru na jinou pozici
        /// <summary>
        /// Procento zvýraznění začátku tohoto sloupce v procesu přetahování jiného sloupce.
        /// Pokud je větší než 0, zvýrazní se část počátku, protože se před tento sloupec přetáhne nějaký jiný.
        /// </summary>
        protected int DrawInsertMarkAtBegin { get; set; }
        /// <summary>
        /// Procento zvýraznění konce tohoto sloupce v procesu přetahování jiného sloupce.
        /// Pokud je větší než 0, zvýrazní se část konce, protože se za tento sloupec přetáhne nějaký jiný.
        /// </summary>
        protected int DrawInsertMarkAtEnd { get; set; }
        #endregion
    }
    public class GTableHeader : GHeader
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor pro záhlaví, s odkazem na tabulku
        /// </summary>
        /// <param name="table"></param>
        public GTableHeader(Table table)
        {
            this._OwnerTable = table;
            this._ColumnSplitterInit();
        }
        private Table _OwnerTable;
        protected override void SetChildBounds(Rectangle newBounds)
        {
            this.SetSplitterBounds(newBounds);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "TableHeader in " + this._OwnerTable.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka, do které patří toto záhlaví
        /// </summary>
        protected override Table OwnerTable { get { return this._OwnerTable; } }
        /// <summary>
        /// Typ záhlaví.
        /// </summary>
        protected override TableAreaType HeaderType { get { return TableAreaType.TableHeader; } }
        #endregion
        #region Interaktivita
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerGTable.TableHeaderClick(e);
        }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Souřadnice na ose X, v pixelech, v koordinátech GTable, kde je tento sloupec právě zobrazen.
        /// </summary>
        public Int32Range VisualRangeX { get; set; }
        /// <summary>
        /// Souřadnice na ose Y, v pixelech, v koordinátech GTable, kde je tento sloupec právě zobrazen.
        /// </summary>
        public Int32Range VisualRangeY { get; set; }
        #endregion
        #region ColumnSplitter
        /// <summary>
        /// Svislý Splitter za tímto sloupcem (který představuje táhlaví řádků), řídí šířku tohoto sloupce (a tím všech sloupců shodného ColumnId v celém Gridu)
        /// </summary>
        public GSplitter ColumnSplitter { get { return this._ColumnSplitter; } }
        /// <summary>
        /// true pokud má být zobrazen splitter za tímto sloupcem, závisí na (OwnerTable.AllowRowHeaderWidthResize)
        /// </summary>
        public bool ColumnSplitterVisible { get { return (this.OwnerTable.AllowRowHeaderWidthResize); } }
        /// <summary>
        /// Připraví ColumnSplitter.
        /// Splitter je připraven vždy, i když se aktuálně nepoužívá.
        /// To proto, že uživatel (tj. aplikační kód) může změnit názor, a pak bude pozdě provádět inicializaci.
        /// </summary>
        protected void _ColumnSplitterInit()
        {
            this._ColumnSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Vertical, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._ColumnSplitter.ValueSilent = this.Bounds.Right;
            this._ColumnSplitter.ValueChanging += new GPropertyChanged<int>(_ColumnSplitter_LocationChange);
            this._ColumnSplitter.ValueChanged += new GPropertyChanged<int>(_ColumnSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler pro událost _ColumnSplitter.ValueChanging a ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ColumnSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int left = this.Bounds.Left;
            int location = this.ColumnSplitter.Value;
            int width = location - left;
            this.OwnerGGrid.ColumnRowHeaderResizeTo(ref width);
            e.CorrectValue = left + width;
        }
        /// <summary>
        /// Nastaví souřadnice splitteru, po změně souřadnic this headeru.
        /// Splitter má být vždy umístěn na pravém okraji this záhlaví.
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetSplitterBounds(Rectangle newBounds)
        {
            this.ColumnSplitter.LoadFrom(newBounds, RectangleSide.Right, true);
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _ColumnSplitter;
        #endregion
    }
    /// <summary>
    /// GColumnHeader : vizuální třída pro zobrazování záhlaví sloupce
    /// </summary>
    public class GColumnHeader : GHeader
    {
        #region Konstruktor, data
        public GColumnHeader(Column column)
            : base()
        {
            this._OwnerColumn = column;
            this._ColumnSplitterInit();
            this._TimeAxisInit();
        }
        private Column _OwnerColumn;
        protected override void SetChildBounds(Rectangle newBounds)
        {
            this.SetSplitterBounds(newBounds);
            this.SetTimeAxisBounds(newBounds);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ColumnHeader in " + this._OwnerColumn.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Sloupec, do kterého patří toto záhlaví
        /// </summary>
        protected virtual Column OwnerColumn { get { return this._OwnerColumn; } }
        /// <summary>
        /// Tabulka (datová), do které patří toto záhlaví
        /// </summary>
        protected override Table OwnerTable { get { return this._OwnerColumn.Table; } }
        /// <summary>
        /// Typ záhlaví.
        /// </summary>
        protected override TableAreaType HeaderType { get { return TableAreaType.ColumnHeader; } }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Souřadnice na ose X, v pixelech, v koordinátech GTable, kde je tento sloupec právě zobrazen.
        /// Může být null pro sloupce mimo zobrazovaný prostor.
        /// </summary>
        public Int32Range VisualRange { get; set; }
        #endregion
        #region ColumnSplitter
        /// <summary>
        /// Svislý Splitter za tímto sloupcem, řídí šířku tohoto sloupce (a tím všech sloupců shodného ColumnId v celém Gridu)
        /// </summary>
        public GSplitter ColumnSplitter { get { return this._ColumnSplitter; } }
        /// <summary>
        /// true pokud má být zobrazen splitter za tímto sloupcem, závisí na (OwnerTable.AllowColumnResize && OwnerColumn.AllowColumnResize)
        /// </summary>
        public bool ColumnSplitterVisible { get { return (this.OwnerTable.AllowColumnResize && this.OwnerColumn.AllowColumnResize); } }
        /// <summary>
        /// Připraví ColumnSplitter.
        /// Splitter je připraven vždy, i když se aktuálně nepoužívá.
        /// To proto, že uživatel (tj. aplikační kód) může změnit názor, a pak bude pozdě provádět inicializaci.
        /// </summary>
        protected void _ColumnSplitterInit()
        {
            this._ColumnSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Vertical, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._ColumnSplitter.ValueSilent = this.Bounds.Right;
            this._ColumnSplitter.ValueChanging += new GPropertyChanged<int>(_ColumnSplitter_LocationChange);
            this._ColumnSplitter.ValueChanged += new GPropertyChanged<int>(_ColumnSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler pro událost _ColumnSplitter.ValueChanging a ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ColumnSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int left = this.Bounds.Left;
            int location = this.ColumnSplitter.Value;
            int width = location - left;
            this.OwnerGGrid.ColumnResizeTo(this.OwnerColumn, ref width);
            e.CorrectValue = left + width;
        }
        /// <summary>
        /// Nastaví souřadnice splitteru, po změně souřadnic this headeru.
        /// Splitter má být vždy umístěn na pravém okraji this záhlaví.
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetSplitterBounds(Rectangle newBounds)
        {
            this.ColumnSplitter.LoadFrom(newBounds, RectangleSide.Right, true);
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _ColumnSplitter;
        #endregion
        #region TimeAxis
        /// <summary>
        /// true pokud se pro sloupec má zobrazit časová osa v záhlaví
        /// </summary>
        public bool UseTimeAxis { get { return this.OwnerColumn.UseTimeAxis; } }
        /// <summary>
        /// Objekt, který provádí konverze časových údajů a pixelů, jde o vizuální časovou osu.
        /// Může být null, pokud this.UseTimeAxis je false.
        /// </summary>
        public ITimeConvertor TimeConvertor { get { return (this.UseTimeAxis ? this.TimeAxis : null); } }
        /// <summary>
        /// Objekt, který provádí konverze časových údajů a pixelů, jde o vizuální časovou osu.
        /// Může být null, pokud this.UseTimeAxis je false.
        /// </summary>
        public GTimeAxis TimeAxis
        {
            get
            {
                if (!this.OwnerColumn.UseTimeAxis) return null;
                this._TimeAxisCheck();
                return this._TimeAxis;
            }
        }
        /// <summary>
        /// Inicializace časové osy - nic neprovede, protože TimeAxis se vytváří On-Demand podle nastavení OwnerColumnu, v TimeAxis.get()
        /// </summary>
        protected void _TimeAxisInit() { /* TimeAxis je On-Demand, netřeba řešit inicializaci */ }
        private void _TimeAxisCheck()
        {
            if (this._TimeAxis != null) return;

            this._TimeAxis = new GTimeAxis();
            this._TimeAxis.ValueChanging += _TimeAxis_ValueChange;
            this._TimeAxis.ValueChanged += _TimeAxis_ValueChange;
        }
        /// <summary>
        /// Eventhandler události při/po změně ValueChanging nebo ValueChanged.
        /// Handler vyvolá metodu OnChangeTimeAxis() na OwnerGTable, 
        /// která zajistí synchronizaci této změny do ostatních tabulek (změna projde do metody this.RefreshTimeAxis, ale v jiných instancích této třídy).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TimeAxis_ValueChange(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            this.OwnerGTable.OnChangeTimeAxis(this.OwnerColumn.ColumnId, e);
        }
        /// <summary>
        /// Je voláno z GGrid, po změně hodnoty Value na některé TimeAxis na sloupci columnId (v this.Columns), ale na jiné tabulce než je this tabulka.
        /// Tato tabulka je tedy Slave, a má si změnit svoji hodnotu bez toho, aby vyvolala další event o změně hodnoty.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v JINÉ tabulce.
        /// </summary>
        /// <param name="e"></param>
        internal void RefreshTimeAxis(GPropertyChangeArgs<TimeRange> e)
        {
            if (!this.UseTimeAxis) return;
            this.TimeAxis.ValueSilent = e.NewValue;
        }
        /// <summary>
        /// Umístí časovou osu do odpovídajícího prostoru v this objektu (nastaví TimeAxis.Bounds).
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetTimeAxisBounds(Rectangle newBounds)
        {
            if (this.UseTimeAxis)
            {   // Časovou osu kreslíme v ose X přesně do this.Bounds, v ose Y necháme nahoře 5 pixelů (pro Drag sloupce), dole necháme 1 pixel (pro strýčka Příhodu):
                Rectangle bounds = this.Bounds;
                this.TimeAxis.Bounds = new Rectangle(0, 5, bounds.Width, bounds.Height - 6);
            }
        }
        /// <summary>
        /// Časová osa, fyzické úložiště
        /// </summary>
        private GTimeAxis _TimeAxis;
        #endregion
        #region Interaktivita
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            Djs.Common.Localizable.TextLoc toolTip = this.OwnerColumn.ToolTip;
            if (toolTip != null && !String.IsNullOrEmpty(toolTip.Text))
            {
                e.ToolTipData.InfoText = toolTip.Text;
                e.ToolTipData.TitleText = "Column info";
                e.ToolTipData.ShapeType = TooltipShapeType.Rectangle;
                e.ToolTipData.Opacity = 240;
            }
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerGTable.ColumnHeaderClick(e, this.OwnerColumn);
        }
        #endregion
        #region Drag - Proces přesouvání sloupce
        /// <summary>
        /// Můžeme tento sloupec přemístit jinam? Závisí na OwnerTable.AllowColumnReorder
        /// </summary>
        protected override bool CanDrag { get { return this.OwnerTable.AllowColumnReorder; } }
        /// <summary>
        /// Volá se v procesu přesouvání. Zarovná souřadnice do povoleného rozmezí a najde sloupce, kam by se měl přesun provést.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected override void DragThisOverBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            // base třída je ochotná přesunout this objekt do libovolného místa (to je ostatně její velké pozitivum).
            // Ale ColumnHeader má mít prostor pro posun omezen jen na vhodná místa mezi ostatními sloupci:
            Rectangle allowedBounds = this.OwnerGTable.GetRelativeBoundsForArea(TableAreaType.ColumnHeader);    // Souřadnice prostoru ColumnHeader, relativně k Table
            allowedBounds.Y = allowedBounds.Y + 5;                   // Prostor ColumnHeader omezím: dolů o 5px,
            allowedBounds.Height = 2 * allowedBounds.Height - 5;     //  a dolní okraj tak, aby byl o něco menší než 2x výšky.
            Rectangle modifiedBounds = targetRelativeBounds.FitInto(allowedBounds, false);         // Souřadnice "Drag" musí být uvnitř vymezeného prostoru

            // V této chvíli si base třída zapracuje "upravené" souřadnice (bounds) do this objektu,
            //  takže this záhlaví se bude vykreslovat "jako duch" v tomto omezeném prostoru:
            base.DragThisOverBounds(e, modifiedBounds);

            // Vyhledáme okolní sloupce, mezi které bychom rádi vložili this sloupec:
            Column prevColumn, nextColumn;
            int prevMark, nextMark;
            this._DragThisSearchHeaders(e, modifiedBounds, out prevColumn, out prevMark, out nextColumn, out nextMark);
            this._DragThisMarkHeaders(prevColumn, prevMark, nextColumn, nextMark);
        }
        /// <summary>
        /// Je vyvoláno po skončení přetahování (=při uvolnění myši nad cílovým prostorem). Je voláno na objektu, který je přetahován, nikoli na objektu kam bylo přetaženo.
        /// Bázová třída (InteractiveDragObject) vložila dané souřadnice do this.Bounds (přičemž ProcessAction = DragValueActions; a EventSourceType = (InteractiveChanged | BoundsChange).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        protected override void DragThisDropToBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            if (this.DragThisToColumnOrder.HasValue)
                this.OwnerGGrid.ColumnMoveTo(this.OwnerColumn, this.DragThisToColumnOrder.Value);
        }
        /// <summary>
        /// Je voláno po skončení přetahování, ať už skončilo OK (=Drop) nebo Escape (=Cancel).
        /// Účelem je provést úklid po skončení přetahování.
        /// </summary>
        /// <param name="e"></param>
        protected override void DragThisOverEnd(GDragActionArgs e)
        {
            base.DragThisOverEnd(e);
            this.OwnerGTable.Columns.ForEachItem(c => c.ColumnHeader.ResetInsertMark());
            this.DragThisToColumnOrder = null;
        }
        /// <summary>
        /// Nuluje proměnné, které byly použity při přetahování nějakého jiného ColumnHeader přes this ColumnHeader.
        /// Zhasíná se tím prosvícení Drag-Target označení.
        /// </summary>
        protected void ResetInsertMark()
        { }
        /// <summary>
        /// Najde sloupce ležící před a za místem, kam bychom rádi vložili this sloupec v procesu přetahování.
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <param name="prevColumn"></param>
        /// <param name="nextColumn"></param>
        private void _DragThisSearchHeaders(GDragActionArgs e, Rectangle boundsAbsolute, out Column prevColumn, out int prevMark, out Column nextColumn, out int nextMark)
        {
            prevColumn = null;
            prevMark = 0;
            nextColumn = null;
            nextMark = 0;

            // Získám soupis sloupců, které jsou viditelné, vyjma this sloupec (podle ColumnId),
            //  tyto sloupce mají korektně vyplněny souřadnice 
            List<Column> columns = this.OwnerGTable.VisibleColumns
                .Where(c => (c.ColumnId != this.OwnerColumn.ColumnId))
                .ToList();
            int count = columns.Count;
            if (count == 0) return;

            // Určím souřadnici myši ve směru X, relativně k tabulce (protože relativně k tabulce jsou určeny souřadnice sloupců):
            int mouseX = this.OwnerGTable.GetRelativePoint(e.MouseCurrentAbsolutePoint).Value.X;

            // Najdu sloupec, nad kterým se aktuálně pohybuje myš v ose X:
            int index = -1;
            int lastIndex = count - 1;
            bool setDragToOrder = true;
            if (mouseX < columns[0].ColumnHeader.Bounds.X)
            {   // Myš je PŘED PRVNÍM ze sloupců:
                index = 0;
                nextColumn = columns[0];
                nextMark = 100;
            }
            else if (mouseX >= columns[lastIndex].ColumnHeader.Bounds.Right)
            {   // Myš je ZA POSLEDNÍM ze sloupců:
                index = lastIndex;
                prevColumn = columns[lastIndex];
                prevMark = 100;
            }
            else
            {   // Bude to složitější: myš je někde uvnitř, nad nějakým sloupcem:
                // Zkusím najít sloupec, nad kterým se nachází myš (na souřadnici X):
                index = columns.FindIndex(c => (mouseX >= c.ColumnHeader.Bounds.X && mouseX < c.ColumnHeader.Bounds.Right));
                // Může být, že sloupec nenajdu, protože v poli "columns" není obsažen prvek this, a nad ním může stále být myš umístěna!
                if (index >= 0)
                {   // Myš je nad nějakým sloupcem [index], zjistíme zda náš sloupec (this) budeme dávet před něj nebo za něj:
                    Rectangle targetBounds = columns[index].ColumnHeader.Bounds;
                    int targetCenterX = targetBounds.Center().X;
                    if (mouseX < targetCenterX)
                    {   // Myš je v levé polovině sloupce => přetáhneme nás PŘED ten sloupec:
                        nextColumn = columns[index];
                        nextMark = _DragThisGetMark(mouseX - targetBounds.X, targetBounds.Width);
                        prevColumn = (index > 0 ? columns[index - 1] : null);
                        prevMark = (index > 0 ? nextMark : 0);
                    }
                    else
                    {   // Myš je v pravé polovině sloupce => přetáhneme nás ZA ten sloupec:
                        prevColumn = columns[index];
                        prevMark = _DragThisGetMark(targetBounds.Right - mouseX, targetBounds.Width);
                        nextColumn = (index < lastIndex ? columns[index + 1] : null);
                        nextMark = (index < lastIndex ? prevMark : 0);
                    }
                }
                else
                {   // Myš je stále nad naším sloupcem, najdeme sloupce před a za námi:
                    int prevIndex = columns.FindLastIndex(c => (c.ColumnHeader.Bounds.Right < mouseX));
                    prevColumn = (prevIndex >= 0 ? columns[prevIndex] : null);
                    prevMark = (prevIndex >= 0 ? 100 : 0);
                    int nextIndex = columns.FindIndex(c => (c.ColumnHeader.Bounds.X >= mouseX));
                    nextColumn = (nextIndex >= 0 ? columns[nextIndex] : null);
                    nextMark = (nextIndex >= 0 ? 100 : 0);
                    this.DragThisToColumnOrder = null;
                    // V tomto případě nebudeme nastavovat _DragThisToColumnOrder:
                    setDragToOrder = false;
                }
            }

            if (setDragToOrder)
            {   // Nastavíme _DragThisToColumnOrder na hodnotu toho sloupce, před kterým chceme být umístěni:
                if (nextColumn != null)
                    this.DragThisToColumnOrder = nextColumn.ColumnOrder;
                else
                    // Pokud máme být umístěni za poslední sloupec, dáme hodnotu posledního sloupce + 1:
                    this.DragThisToColumnOrder = columns[lastIndex].ColumnOrder + 1;
            }
        }
        /// <summary>
        /// Vrací procentuální hodnotu (15 - 100), která reprezentuje vizuální přesnost zacílení při přesouvání sloupce myší.
        /// 15 = slabé, myš je někde uprostřed; 100 = přesné, myš je přesně na hraně cílového prvku.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private int _DragThisGetMark(int distance, int width)
        {
            int half = width / 2;
            if (distance < 0) return 100;
            if (distance >= half) return 15;
            return (int)(Math.Round(15f + 85f * (float)(half - distance) / (float)half, 0));
        }
        /// <summary>
        /// Mark specified columns as "Drag into after" and "Drag into before".
        /// All other columns mark as "no drag".
        /// Where is change, there will set DrawToLayer...
        /// </summary>
        /// <param name="prevColumn"></param>
        /// <param name="nextColumn"></param>
        private void _DragThisMarkHeaders(Column prevColumn, int prevMark, Column nextColumn, int nextMark)
        {
            int prevId = (prevColumn != null ? prevColumn.ColumnId : -1);
            int nextId = (nextColumn != null ? nextColumn.ColumnId : -1);
            foreach (Column column in this.OwnerTable.Columns)
            {
                var header = column.ColumnHeader;
                int markBegin = ((column.ColumnId == nextId) ? nextMark : 0);
                if (header.DrawInsertMarkAtBegin != markBegin)
                {
                    header.DrawInsertMarkAtBegin = markBegin;
                    header.Repaint();
                }

                int markEnd = ((column.ColumnId == prevId) ? prevMark : 0);
                if (header.DrawInsertMarkAtEnd != markEnd)
                {
                    header.DrawInsertMarkAtEnd = markEnd;
                    header.Repaint();
                }
            }
        }
        /// <summary>
        /// Cílové pořadí pro this sloupec v procesu přetahování tohoto sloupce na jiné místo.
        /// </summary>
        protected Int32? DragThisToColumnOrder { get; set; }

        #endregion
        #region Draw - kreslení záhlaví sloupce : ikona, text, značky při procesu Drag
        protected override void DrawHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            base.DrawHeader(e, boundsAbsolute, opacity);
            this.DrawInsertMarks(e, boundsAbsolute, opacity);
            this.DrawColumnHeader(e, boundsAbsolute, opacity);
        }
        /// <summary>
        /// Do this záhlaví vykreslí ikonu třídění a titulkový text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawColumnHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            Column column = this.OwnerColumn;
            string text = column.Title;
            Rectangle textArea = Rectangle.Empty;
            if (!String.IsNullOrEmpty(text) && !column.UseTimeAxis)
            {   // Sloupec má zadaný titulek, a nepoužívá časovou osu (pak nebudeme kreslit titulek, bude tam jen osa):
                FontInfo fontInfo = FontInfo.Caption;
                fontInfo.Bold = (column.SortCurrent == TableSortRowType.Ascending || column.SortCurrent == TableSortRowType.Descending);
                Color textColor = Skin.Grid.HeaderTextColor.SetOpacity(opacity);
                GPainter.DrawString(e.Graphics, boundsAbsolute, text, textColor, fontInfo, ContentAlignment.MiddleCenter, out textArea);

                // Obrázek odpovídající aktuálnímu třídění sloupce:
                Image sortImage = this.SortCurrentImage;
                if (sortImage != null)
                {
                    int x = textArea.X - sortImage.Width - 2;
                    int y = textArea.Center().Y - sortImage.Height / 2;
                    Rectangle sortBounds = new Rectangle(x, y, sortImage.Width, sortImage.Height);
                    e.Graphics.DrawImage(sortImage, sortBounds);
                }
            }
        }
        /// <summary>
        /// Do this záhlaví vykreslí značky, označující cíl při procesu Drag. Kreslí značky Begin i End, podle jejich hodnoty.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawInsertMarks(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            int mark;

            mark = this.DrawInsertMarkAtBegin;
            if (mark > 0)
            {
                int m = (mark <= 100 ? mark : 100);
                int w = boundsAbsolute.Width * m / 300;
                Rectangle boundsMark = new Rectangle(boundsAbsolute.X + 1, boundsAbsolute.Y, w, boundsAbsolute.Height);
                GPainter.DrawInsertMark(e.Graphics, boundsMark, Skin.Modifiers.MouseDragTracking, System.Drawing.ContentAlignment.MiddleLeft);
            }

            mark = this.DrawInsertMarkAtEnd;
            if (mark > 0)
            {
                int m = (mark <= 100 ? mark : 100);
                int w = boundsAbsolute.Width * m / 300;
                Rectangle boundsMark = new Rectangle(boundsAbsolute.Right - w - 1, boundsAbsolute.Y, w, boundsAbsolute.Height);
                GPainter.DrawInsertMark(e.Graphics, boundsMark, Skin.Modifiers.MouseDragTracking, System.Drawing.ContentAlignment.MiddleRight);
            }
        }
        /// <summary>
        /// Image odvozený podle this.OwnerColumn.SortCurrent
        /// </summary>
        protected Image SortCurrentImage
        {
            get
            {
                switch (this.OwnerColumn.SortCurrent)
                {
                    case TableSortRowType.Ascending: return Skin.Grid.SortAscendingImage;
                    case TableSortRowType.Descending: return Skin.Grid.SortDescendingImage;
                }
                return null;
            }
        }
        #endregion
    }
    /// <summary>
    /// GRowHeader : vizuální třída pro zobrazování záhlaví řádku
    /// </summary>
    public class GRowHeader : GHeader
    {
        #region Konstruktor, data
        public GRowHeader(Row row)
            : base()
        {
            this._OwnerRow = row;
            this._RowSplitterInit();
        }
        private Row _OwnerRow;
        protected override void SetChildBounds(Rectangle newBounds)
        {
            this.SetSplitterBounds(newBounds);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "RowHeader in " + this._OwnerRow.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka (datová), do které patří toto záhlaví
        /// </summary>
        protected override Table OwnerTable { get { return this._OwnerRow.Table; } }
        /// <summary>
        /// Řádek, do kterého patří toto záhlaví
        /// </summary>
        protected virtual Row OwnerRow { get { return this._OwnerRow; } }
        /// <summary>
        /// Typ záhlaví.
        /// </summary>
        protected override TableAreaType HeaderType { get { return TableAreaType.RowHeader; } }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Souřadnice na ose Y, v pixelech, v koordinátech GTable, kde je tento řádek právě zobrazen.
        /// Může být null pro řádky mimo zobrazovaný prostor.
        /// </summary>
        public Int32Range VisualRange { get; set; }
        #endregion
        #region RowSplitter
        /// <summary>
        /// Vodorovný Splitter pod tímto řádkem, řídí výšku tohoto řádku
        /// </summary>
        public GSplitter RowSplitter { get { return this._RowSplitter; } }
        /// <summary>
        /// true pokud má být zobrazen splitter za tímto řádkem, závisí na (OwnerTable.AllowRowResize)
        /// </summary>
        public bool RowSplitterVisible { get { return (this.OwnerTable.AllowRowResize); } }
        /// <summary>
        /// Připraví ColumnSplitter.
        /// Splitter je připraven vždy, i když se aktuálně nepoužívá.
        /// To proto, že uživatel (tj. aplikační kód) může změnit názor, a pak bude pozdě provádět inicializaci.
        /// </summary>
        protected void _RowSplitterInit()
        {
            this._RowSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._RowSplitter.ValueSilent = this.Bounds.Right;
            this._RowSplitter.ValueChanging += new GPropertyChanged<int>(_RowSplitter_LocationChange);
            this._RowSplitter.ValueChanged += new GPropertyChanged<int>(_RowSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler pro událost _RowSplitter.ValueChanging a ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int top = this.Bounds.Top;
            int value = this.RowSplitter.Value;
            int height = value - top;
            this.OwnerGTable.RowResizeTo(this.OwnerRow, ref height);
            e.CorrectValue = top + height;
        }
        /// <summary>
        /// Nastaví souřadnice zdejšího splitteru, po změně souřadnic this headeru.
        /// Splitter má být vždy umístěn na dolním okraji this záhlaví.
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetSplitterBounds(Rectangle newBounds)
        {
            this.RowSplitter.LoadFrom(newBounds, RectangleSide.Bottom, true);
        }
        /// <summary>
        /// RowSplitter
        /// </summary>
        protected GSplitter _RowSplitter;
        #endregion
        #region Interaktivita
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.WheelUp:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelDown);
                    break;
            }
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerGTable.RowHeaderClick(e, this.OwnerRow);
        }
        protected override bool CanDrag { get { return this.OwnerTable.AllowColumnReorder; } }
        #endregion
        #region Draw - kreslení záhlaví řádku
        protected override void DrawHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            base.DrawHeader(e, boundsAbsolute, opacity);
            this.DrawMouseHot(e, boundsAbsolute, opacity);
            this.DrawSelectedRow(e, boundsAbsolute, opacity);
        }
        /// <summary>
        /// Do this záhlaví podbarvení v situaci, kdy tento řádek je MouseHot
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawMouseHot(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            if (!this.OwnerRow.IsMouseHot) return;

            Rectangle bounds = new Rectangle(boundsAbsolute.Right - 6, boundsAbsolute.Y + 1, 6, boundsAbsolute.Height - 2);
            GPainter.DrawInsertMark(e.Graphics, bounds, Skin.Control.GlowColor, ContentAlignment.MiddleRight, false, 192);
        }
        /// <summary>
        /// Do this záhlaví vykreslí ikonu pro RowHeaderImage (typicky pro SelectedRow).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawSelectedRow(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            Image image = this.RowHeaderImage;
            if (image == null) return;

            Rectangle bounds = boundsAbsolute.Enlarge(-1, -1, -1, -1);
            bounds = image.Size.AlignTo(bounds, ContentAlignment.MiddleCenter, true);
            e.Graphics.DrawImage(image, bounds);
        }
        /// <summary>
        /// Image vhodný do záhlaví this řádku
        /// </summary>
        protected Image RowHeaderImage
        {
            get
            {
                Row row = this.OwnerRow;
                if (row.IsSelected) return Skin.Grid.RowSelectedImage;
                // Případné další ikonky mohou být zde...
                return null;
            }
        }
        #endregion
    }
    #endregion
    #region Třída GCell : vizuální třída pro zobrazení obsahu sloupce
    /// <summary>
    /// GCell : vizuální třída pro zobrazení obsahu sloupce
    /// </summary>
    public class GCell : InteractiveContainer
    {
        #region Konstrukce
        public GCell(Cell cell)
        {
            this._Cell = cell;
        }
        private Cell _Cell;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Cell in " + this._Cell.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Grid (grafický), do kterého patří tato vizuální buňka
        /// </summary>
        protected GGrid OwnerGGrid { get { return (this.OwnerTable != null ? this.OwnerTable.GTable.Grid : null); } }
        /// <summary>
        /// Tabulka (grafická), do které patří tato vizuální buňka
        /// </summary>
        protected GTable OwnerGTable { get { return (this.OwnerTable != null ? this.OwnerTable.GTable : null); } }
        /// <summary>
        /// Tabulka (datová), do které patří tato vizuální buňka
        /// </summary>
        protected Table OwnerTable { get { return this._Cell.Table; } }
        /// <summary>
        /// Řádek, do kterého patří tato vizuální buňka
        /// </summary>
        protected Row OwnerRow { get { return this._Cell.Row; } }
        /// <summary>
        /// Záhlaví řádku kam patří tato buňka, grafický prvek
        /// </summary>
        protected GRowHeader RowHeader { get { return this._Cell.Row.RowHeader; } }
        /// <summary>
        /// Sloupec, do kterého patří tato vizuální buňka
        /// </summary>
        protected Column OwnerColumn { get { return this._Cell.Column; } }
        /// <summary>
        /// Záhlaví sloupce, kam patří tato buňka, grafický prvek
        /// </summary>
        protected GColumnHeader ColumnHeader { get { return this._Cell.Column.ColumnHeader; } }
        /// <summary>
        /// Datová buňka, do které patří tato vizuální buňka
        /// </summary>
        protected Cell OwnerCell { get { return this._Cell; } }
        /// <summary>
        /// Typ prvku = Data
        /// </summary>
        protected TableAreaType AreaType { get { return TableAreaType.Data; } }
        #endregion
        #region Interaktivita
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardPreviewKeyDown:           // Sem chodí i klávesy Kurzor, Tab
                    this.KeyboardPreviewKeyDown(e);
                    break;
                case GInteractiveChangeState.KeyboardKeyPress:                 // Sem nechodí "kurzorové" klávesy, zatím nás event nezajímá. Mohl by aktivovat řádkový filtr...
                    break;
                case GInteractiveChangeState.KeyboardKeyDown:                  // Sem chodí PageUp, PageDown a písmena
                    break;
                case GInteractiveChangeState.WheelUp:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelDown);
                    break;
            }
        }
        /// <summary>
        /// Reaguje na klávesy typu kurzor, posune seznam řádků nahoru / dolů
        /// </summary>
        /// <param name="e"></param>
        private void KeyboardPreviewKeyDown(GInteractiveChangeStateArgs e)
        {
            InteractivePositionAction action = e.KeyboardPreviewArgs.GetInteractiveAction();
            if (action != InteractivePositionAction.None)
                e.KeyboardPreviewArgs.IsInputKey = this.OwnerGTable.ProcessRowAction(action);

            /*
            var code = e.KeyboardPreviewArgs.KeyCode;
            var data = e.KeyboardPreviewArgs.KeyData;
            int x = e.KeyboardPreviewArgs.KeyValue;
            
            e.ToolTipData.TitleText = "KeyboardPreviewKeyDown";
            e.ToolTipData.InfoText = "KeyCode: " + code.ToString() + "; KeyData: " + data.ToString() + "; Action = " + action.ToString();
            */
        }
        /// <summary>
        /// Myš vstoupila nad tuto buňku
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseEnter(e);
            this.OwnerGTable.CellMouseEnter(e, this.OwnerCell);
        }
        /// <summary>
        /// Myš odešla z této buňky
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            this.OwnerGTable.CellMouseLeave(e, this.OwnerCell);
        }
        /// <summary>
        /// Uživatel klikl do této buňky
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);
            this.OwnerGTable.CellClick(e, this.OwnerCell);
        }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
            Rectangle boundsAbsolute = this.BoundsAbsolute;

            // Clip() mi zajistí, že při pixelovém posunu buňky bude buňka vykreslena jen do příslušné části vymezeného prostoru pro danou oblast.
            // Grafická organizace GTable není členěna nijak výrazně strukturovaně => GTable obsahuje jako Child jednotlivé prvky (GTableHeader, GColumnHeader, GowHeader, GCell),
            //  které mají svoje souřadnice relativní k GTable, ale mají se zobrazovat "oříznuté" jen do patřičných oblastí v GTable.
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                // Clip() ale provedeme jen pro Standard vrstvu; protože v ostatních vrstvách se provádí Dragging, a ten má být neomezený:
                e.GraphicsClipWith(this.OwnerGTable.GetAbsoluteBoundsForArea(this.AreaType), true);

            // Background + obsah:
            this.DrawContent(e, boundsAbsolute);

            // GridLines:
            this.OwnerGTable.DrawRowGridLines(e, this.OwnerCell, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí obsah této buňky podle jejího druhu, jako text nebo jako graf nebo jako obrázek.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        private void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            object value = this.OwnerCell.Value;
            if (value == null)
                this.DrawNull(e, boundsAbsolute);
            else if (value is IDrawItem)
                this.DrawIDrawItem(e, boundsAbsolute, value as IDrawItem);
            else if (value is ITimeInteractiveGraph)
                this.DrawContentInteractiveTimeGraph(e, boundsAbsolute, value as ITimeInteractiveGraph);
            else if (value is ITimeGraph)
                this.DrawContentTimeGraph(e, boundsAbsolute, value as ITimeGraph);
            else if (value is Image)
                this.DrawContentImage(e, boundsAbsolute, value as Image);
            else
                this.DrawContentText(e, boundsAbsolute, value);
        }
        /// <summary>
        /// Vykreslí prázdnou buňku (jen pozadí)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        private void DrawNull(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            // Pozadí řádku:
            this.OwnerGTable.DrawRowBackground(e, this.OwnerCell, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí obsah this buňky pomocí její vlastní metody IDrawItem.Draw()
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawItem"></param>
        private void DrawIDrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, IDrawItem drawItem)
        {
            try
            {
                drawItem.Draw(e, boundsAbsolute);
            }
            catch { }
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako interaktivní časový graf
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="graph"></param>
        private void DrawContentInteractiveTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, ITimeInteractiveGraph graph)
        {
            if (graph.Convertor == null)
                graph.Convertor = this.ColumnHeader.TimeConvertor;
            if (graph.Parent == null)
                graph.Parent = this;
            if (graph.Bounds != this.BoundsClient)
                graph.Bounds = this.BoundsClient;

            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako časový graf
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="graph"></param>
        private void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, ITimeGraph graph)
        {
            if (graph.Convertor == null)
                graph.Convertor = this.ColumnHeader.TimeConvertor;

            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako Image
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="image"></param>
        private void DrawContentImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Image image)
        {
            // Pozadí řádku:
            this.OwnerGTable.DrawRowBackground(e, this.OwnerCell, boundsAbsolute);

            if (image == null) return;
            Size size = image.Size;
            Rectangle imageBounds = size.AlignTo(boundsAbsolute, ContentAlignment.MiddleCenter, true, true);
            if (imageBounds.Width > 4 && imageBounds.Height > 4)
                e.Graphics.DrawImage(image, imageBounds);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="value"></param>
        private void DrawContentText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, object value)
        {
            // Pozadí řádku:
            this.OwnerGTable.DrawRowBackground(e, this.OwnerCell, boundsAbsolute);

            // Obsah řádku:
            string formatString = this.OwnerColumn.FormatString;
            ContentAlignment textAlignment;
            string text = GetText(value, formatString, out textAlignment);

            VisualStyle style = ((IVisualMember)this.OwnerCell).Style;
            ContentAlignment alignment = style.ContentAlignment ?? textAlignment;
            FontInfo font = style.Font ?? FontInfo.Default;
            Color textColor = this.OwnerGTable.GetTextColorForCell(this.OwnerCell);

            GPainter.DrawString(e.Graphics, boundsAbsolute, text, textColor, font, alignment);
        }
        /// <summary>
        /// Převede danou hodnotu (obsah buňky) na string s využitím formátovacího řetězce, a podle konkrétního datového typu určí výchozí zarovnání.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="formatString"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        private static string GetText(object value, string formatString, out ContentAlignment alignment)
        {
            alignment = ContentAlignment.MiddleLeft;
            if (value == null) return "";

            bool hasFormatString = (!String.IsNullOrEmpty(formatString));

            if (value is DateTime)
            {
                DateTime valueDT = (DateTime)value;
                alignment = ContentAlignment.MiddleCenter;
                if (hasFormatString) return valueDT.ToString(formatString);
                return valueDT.ToString();
            }

            if (value is Int32)
            {
                Int32 valueInt32 = (Int32)value;
                alignment = ContentAlignment.MiddleRight;
                if (hasFormatString) return valueInt32.ToString(formatString);
                return valueInt32.ToString();
            }

            if (value is Decimal)
            {
                Decimal valueDecimal = (Decimal)value;
                alignment = ContentAlignment.MiddleRight;
                if (hasFormatString) return valueDecimal.ToString(formatString);
                return valueDecimal.ToString();
            }
            alignment = ContentAlignment.MiddleLeft;

            return value.ToString();
        }
        #endregion
    }
    #endregion
    #region enum TableAreaType
    /// <summary>
    /// Typ prostoru v tabulce
    /// </summary>
    public enum TableAreaType
    {
        None,
        /// <summary>
        /// Záhlaví tabulky (pak jde o header vlevo nahoře, v křížení sloupce RowHeader a řádku ColumnHeader)
        /// </summary>
        TableHeader,
        /// <summary>
        /// Záhlaví sloupce
        /// </summary>
        ColumnHeader,
        /// <summary>
        /// Záhlaví řádku
        /// </summary>
        RowHeader,
        /// <summary>
        /// Data tabulky
        /// </summary>
        Data,
        /// <summary>
        /// Svislý scrollbar vpravo
        /// </summary>
        VerticalScrollBar,
        /// <summary>
        /// Vodorovný scrollbar dole
        /// </summary>
        HorizontalScrollBar
    }
    #endregion
    /// <summary>
    /// Člen grafické tabulky GTable, do kterého je možno vložit i odebrat referenci na danou GTable
    /// </summary>
    public interface IGTableMember
    {
        /// <summary>
        /// Reference na GTable, umístěná v datovém prvku
        /// </summary>
        GTable GTable { get; set; }
    }

}
