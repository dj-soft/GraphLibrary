using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components.Grid
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

            this._TableInnerLayoutValid = true;                      // Normálně to patří až na konec metody. Ale některé komponenty mohou používat již částečně napočtené hodnoty, a pak bychom se zacyklili

            // Bude viditelný scrollbar řádků? (to je tehdy, když výška zobrazitelných řádků je větší než výška prostoru pro řádky):
            //  Objekt RowsPositions tady provede dotaz na velikost dat (metoda _RowsPositionGetDataSize()) a velikost viditelného prostoru (metoda _RowsPositionGetVisualSize()).
            //  Velikost dat je dána pozicí End posledního řádku z this.RowListAll,
            //  velikost viditelného prostoru pro tabulky je dána výškou tabulky mínus výška hlavičky (this.ClientSize.Height - RowsPositions.VisualFirstPixel), hodnota this.ClientSize je v této době platná.
            this._RowsScrollBarVisible = this.RowsPositions.IsScrollBarActive;

            // Určíme souřadnice jednotlivých elementů:
            int x0 = 0;                                              // x0: úplně vlevo
            int x1 = (this.HasGrid ? this.Grid.ColumnsPositions.VisualFirstPixel : 0);   // x1: tady začíná prostor pro datové sloupce
            int x3 = clientSize.Width;                               // x3: úplně vpravo
            int x2t = x3 - GScrollBar.DefaultSystemBarWidth;         // x2t: zde začíná RowsScrollBar (vpravo, hned za koncem prostoru pro řádky), tedy pokud by byl zobrazen
            int x2r = (this._RowsScrollBarVisible ? x2t : x3);       // x2r: zde reálně končí oblast prostoru pro řádky, se zohledněním aktuální viditelnosti RowsScrollBaru
            int y0 = 0;                                              // y0: úplně nahoře
            int y1 = this.RowsPositions.VisualFirstPixel;            // y1: zde začíná prostor pro řádky, hned pod prostorem ColumnHeader (hodnota se fyzicky načte z this.DataTable.ColumnHeaderHeight)
            int y3 = clientSize.Height;                              // y3: úplně dole

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
                case TableAreaType.Table: return new Rectangle(new Point(0, 0), this.Bounds.Size);
                case TableAreaType.TableHeader: return this.TableHeaderBounds;
                case TableAreaType.ColumnHeader: return this.ColumnHeaderBounds;
                case TableAreaType.RowHeader: return this.RowHeaderBounds;
                case TableAreaType.RowArea: return this.RowAreaBounds;
                case TableAreaType.Cell: return this.RowAreaBounds;
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
            Rectangle areaRelativeBounds = this.GetRelativeBoundsForArea(areaType);
            Rectangle areaAbsoluteBounds = areaRelativeBounds.Add(tableAbsoluteBounds.Location);
            return areaAbsoluteBounds;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice požadovaného prostoru.
        /// Metoda může oříznout daný prostor do souřadnic, které GGrid vymezuje pro Tables (podle hodnoty clipToTableBounds).
        /// Souřadnice slouží k provedení Graphics.Clip() před vykreslením obsahu.
        /// </summary>
        /// <param name="areaType"></param>
        /// <param name="clipToTableBounds"></param>
        /// <returns></returns>
        public Rectangle GetAbsoluteBoundsForArea(TableAreaType areaType, bool clipToTableBounds)
        {
            Rectangle areaAbsoluteBounds = this.GetAbsoluteBoundsForArea(areaType);
            if (clipToTableBounds && this.HasGrid)
            {
                Rectangle tablesAbsoluteBounds = this.Grid.GetAbsoluteBoundsForArea(TableAreaType.AllTables);
                areaAbsoluteBounds = Rectangle.Intersect(areaAbsoluteBounds, tablesAbsoluteBounds);
            }
            return areaAbsoluteBounds;
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
            this._ColumnListWidthValid = true;                                                     // ISequenceLayout jsou platné
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

            Column[] columns = this.Columns;
            if (!this._ColumnListWidthValid)
            {
                SequenceLayout.SequenceLayoutCalculate(columns);                                   // Napočítat jejich ISequenceLayout.Begin a .End
                this._ColumnListWidthValid = true;                                                 // ISequenceLayout jsou platné
            }

            List<Column> visibleColumns = new List<Column>();
            GridPosition columnsPositions = this.Grid.ColumnsPositions;
            Int32Range dataVisibleRange = columnsPositions.DataVisibleRange;                       // Rozmezí datových pixelů, které jsou viditelné
            foreach (Column column in columns)
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
        /// true = hodnoty ISequenceLayout.Begin a End v sloupcích jsou platné
        /// </summary>
        private bool _ColumnListWidthValid;
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
                rows = this.DataTable.RowsSorted;                    // Získat viditelné řádky, setříděné podle zvoleného třídícího sloupce
                this._Rows = rows;
                heightValid = false;
            }
            if (!heightValid)
            {
                SequenceLayout.SequenceLayoutCalculate(rows);        // Napočítat jejich ISequenceLayout.Begin a .End
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
            this._RowsScrollBar.ValueChanging += new GPropertyChangedHandler<SizeRange>(RowsScrollBar_ValueChange);
            this._RowsScrollBar.ValueChanged += new GPropertyChangedHandler<SizeRange>(RowsScrollBar_ValueChange);
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
        /// true pokud je povoleno vybírat jednotlivé buňky tabulky, false pokud celý řádek.
        /// </summary>
        protected bool AllowSelectSingleCell { get { return this.DataTable.AllowSelectSingleCell; } }
        #endregion
        #endregion
        #region Nastavení aktivního řádku, aktivní tabulky; Scroll řádku/tabulky do viditlné oblasti; Repaint řádku, Repaint sloupce
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
        /// Zajistí vyvolání metody Repaint pro RowHeader i pro všechny Cell.Control v daném řádku.
        /// Je vhodné volat po změně aktivního řádku (pro starý i nový aktivní řádek), a stejně i po změně Hot řádku (=ten pod myší).
        /// </summary>
        /// <param name="row"></param>
        protected void RepaintRow(Row row)
        {
            if (row == null) return;
            ((IInteractiveParent)row.RowHeader).Repaint();
            foreach (Cell cell in row.Cells)
                ((IInteractiveParent)cell.Control).Repaint();
        }
        /// <summary>
        /// Zajistí vyvolání metody Repaint pro ColumnHeader i pro všechny Cell.Control ve viditelných řádcích v daném sloupci.
        /// Je vhodné volat po změně na časové ose tohoto sloupce.
        /// </summary>
        /// <param name="row"></param>
        protected void RepaintColumn(Column column)
        {
            if (column == null) return;
            ((IInteractiveParent)column.ColumnHeader).Repaint();
            int columnId = column.ColumnId;
            foreach (Row row in this.VisibleRows)
            {
                Cell cell = row[columnId];
                ((IInteractiveParent)cell.Control).Repaint();
            }
        }
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
            {   // Po změně šířky RowHeader pozice (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů) (RowHeader, ColumnHeader):
                this._TableInnerLayoutValid = false;
                this._VisibleColumns = null;
                this._ChildArrayValid = false;
                callGrid = true;
            }

            if ((items & (InvalidateItem.ColumnHeader)) != 0)
            {   // Po změně vnitřního uspořádání (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů) (RowHeader, ColumnHeader):
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
                this._ColumnListWidthValid = false;
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
            this._HeaderSplitter.ValueChanging += new GPropertyChangedHandler<int>(_HeaderSplitter_LocationChange);
            this._HeaderSplitter.ValueChanged += new GPropertyChangedHandler<int>(_HeaderSplitter_LocationChange);
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
            this._TableSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = TableSplitterSize, SplitterActiveOverlap = 4, LinkedItemPrevMinSize = 50, LinkedItemNextMinSize = 50, IsResizeToLinkItems = true };
            this._TableSplitter.ValueSilent = this.Bounds.Bottom;
            this._TableSplitter.ValueChanging += new GPropertyChangedHandler<int>(_TableSplitter_LocationChange);
            this._TableSplitter.ValueChanged += new GPropertyChangedHandler<int>(_TableSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler události _TableSplitter.LocationChanged (došlo nebo stále dochází ke změně pozice splitteru od tabulkou)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            // Vypočteme výšku tabulky:
            int heightOld = this.DataTable.Height;
            int value = this._TableSplitter.Value - this.Bounds.Top;
            this.DataTable.Height = value;                 // Tady dojde ke kompletnímu vyhodnocení vnitřních pravidel pro výšku Table (Minimum, Default, Range)
            int heightNew = this.DataTable.Height;
            e.CorrectValue = heightNew;                    // Pokud požadovaná hodnota (value) nebyla akceptovatelná, pak correctValue je hodnota přípustná
            if (e.IsChangeValue)
            {
                this.Grid.TableHeightChanged(this, heightOld, heightNew);
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
        /// <summary>
        /// Vizuální velikost splitteru pod tabulkou = jeho výška v pixelech
        /// </summary>
        public static int TableSplitterSize { get { return 3; } }
        #endregion
        #region Child items : pole všech prvků v tabulce (jednotlivé headery, jednotlivé buňky, jednotlivé spittery, a scrollbar řádků)
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
            Rectangle rowHeaderBounds = this.RowHeaderBounds;        // Celý prostor pro záhlaví řádků = přes všechny řádky
            Rectangle rowAreaBounds = this.RowAreaBounds;            // Celý prostor pro data (vpravo od RowHeader, dolů pod ColumnHeader)
            GRowHeader rowHeader = row.RowHeader;                    // Záhlaví aktuálního řádku
            Int32Range rowVisualRange = rowHeader.VisualRange;       // Pozice aktuálního řádku na ose Y (rozmezí pixelů)

            // Něco k pořadí vkládání prvků do Items: dospodu dáme to, co by mělo být "vespodu" = obsah buněk. Nad ně dáme Headers a na ně Splitters:

            GRow gRow = row.Control;
            gRow.Bounds = new Rectangle(rowAreaBounds.X, rowVisualRange.Begin, rowAreaBounds.Width, rowVisualRange.Size);
            this.ChildList.Add(gRow);

            foreach (Column column in this.VisibleColumns)
            {
                Int32Range columnVisualRange = column.ColumnHeader.VisualRange;               // Pozice aktuálního sloupce na ose X (rozmezí pixelů)  
                GCell gCell = row[column.ColumnId].Control;                                   // Data buňky, měli bychom doplnit jejího parenta = this
                if (((IInteractiveParent)gCell).Parent == null)
                    ((IInteractiveParent)gCell).Parent = this;
                gCell.Bounds = Int32Range.GetRectangle(columnVisualRange, rowVisualRange);    // Souřadnice buňky (v koordinátech GTable, která je Parentem)
                this.ChildList.Add(gCell);
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
        bool ISequenceLayout.AutoSize { get { return this._SequenceLayout.AutoSize; } }
        private ISequenceLayout _SequenceLayout { get { return (ISequenceLayout)this.DataTable; } }
        #endregion
        #region TimeAxis
        /// <summary>
        /// Je voláno z eventhandleru TimeAxis.ValueChange, při/po změně hodnoty Value na některé TimeAxis na sloupci columnId v this.Columns.
        /// Metoda zajistí synchronizaci okolních tabulek (kromě zdejší, ta je v tomto pohledu Master).
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v TÉTO tabulce.
        /// Metoda vyvolá Grid.OnChangeTimeAxis(), tím dojde k synchronizaci Value z this tabulky a aktuálního sloupce do okolních tabulek stejného Gridu.
        /// Metoda dále zajistí překreslení všech Cell pro daný sloupec ve všech viditelných řádcích této tabulky.
        /// </summary>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            Column column = this.Columns.FirstOrDefault(c => c.ColumnId == columnId);
            this.OnChangeTimeAxis(column, e);
        }
        /// <summary>
        /// Je voláno z eventhandleru TimeAxis.ValueChange, při/po změně hodnoty Value na některé TimeAxis na sloupci columnId v this.Columns.
        /// Metoda zajistí synchronizaci okolních tabulek (kromě zdejší, ta je v tomto pohledu Master).
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v TÉTO tabulce.
        /// Metoda vyvolá Grid.OnChangeTimeAxis(), tím dojde k synchronizaci Value z this tabulky a aktuálního sloupce do okolních tabulek stejného Gridu.
        /// Metoda dále zajistí překreslení všech Cell pro daný sloupec ve všech viditelných řádcích této tabulky.
        /// </summary>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(Column column, GPropertyChangeArgs<TimeRange> e)
        {
            if (column == null) return;
            this.RepaintColumn(column);
            if (this.HasGrid)
                this.Grid.OnChangeTimeAxis(this.TableId, column.ColumnId, e);
        }
        /// <summary>
        /// Je voláno z GGrid, po změně hodnoty Value na některé TimeAxis na sloupci columnId (v this.Columns), ale na jiné tabulce než je this tabulka.
        /// Tato tabulka je tedy Slave, a má si změnit svoji hodnotu bez toho, aby vyvolala další event o změně hodnoty.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v JINÉ tabulce.
        /// Metoda zajistí aktualizaci hodnoty v TimeAxis této tabulky v daném sloupci, a překreslení ColumnHeader (=tedy TimeAxis) 
        /// i překreslení všech Cell pro daný sloupec ve všech viditelných řádcích.
        /// </summary>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void RefreshTimeAxis(int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            Column column = this.Columns.FirstOrDefault(c => c.ColumnId == columnId);
            this.RefreshTimeAxis(column, e);
        }
        /// <summary>
        /// Je voláno z GGrid, po změně hodnoty Value na některé TimeAxis na sloupci columnId (v this.Columns), ale na jiné tabulce než je this tabulka.
        /// Tato tabulka je tedy Slave, a má si změnit svoji hodnotu bez toho, aby vyvolala další event o změně hodnoty.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v JINÉ tabulce.
        /// Metoda zajistí aktualizaci hodnoty v TimeAxis této tabulky v daném sloupci, a překreslení ColumnHeader (=tedy TimeAxis) 
        /// i překreslení všech Cell pro daný sloupec ve všech viditelných řádcích.
        /// </summary>
        /// <param name="column">Data sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void RefreshTimeAxis(Column column, GPropertyChangeArgs<TimeRange> e)
        {
            if (column == null || !column.UseTimeAxis) return;
            column.ColumnHeader.RefreshTimeAxis(e);
            this.RepaintColumn(column);
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
            this.GraphicClip(e);
            base.Draw(e);

            // Všechno ostatní (záhlaví sloupců, řádky, scrollbary, splittery) si malují Childs samy.
        }
        protected bool GraphicClip(GInteractiveDrawArgs e)
        {
            // Ořezáváme jen při kreslení do vrstvy Standard, jinak ne:
            if (e.DrawLayer != GInteractiveDrawLayer.Standard) return true;

            // Prostor pro oblast tabulky, se zohledněním souřadnic určených pro prostor tabulek v rámci Gridu:
            Rectangle boundsAbsolute = this.BoundsAbsolute;
            Rectangle areaAbsoluteBounds = this.GetAbsoluteBoundsForArea(TableAreaType.Table, true);

            // Prostor pro aktuální prvek = intersect se souřadnicemi prvku:
            Rectangle controlBounds = Rectangle.Intersect(areaAbsoluteBounds, boundsAbsolute);
            if (!controlBounds.HasPixels()) return false;

            // Prostor po oříznutí s aktuálním Clipem v grafice:
            //  Aktuální Clip v grafice obsahuje prostor, daný pro tento prvek v rámci jeho parentů:
            Rectangle clipBounds = Rectangle.Intersect(controlBounds, e.AbsoluteVisibleClip);
            e.GraphicsClipWith(clipBounds, true);

            // Pokud aktuální Clip je viditelný, pak jeho hodnota určuje souřadnice, kde je prvek interaktivní:
            bool isVisible = !e.IsVisibleClipEmpty;
            this.AbsoluteInteractiveBounds = (isVisible ? (Rectangle?)e.AbsoluteVisibleClip : (Rectangle?)null);

            return isVisible;
        }
        #endregion
        #region Draw : podpora pro kreslení obsahu řádků (pozadí, gridlines, hodnota)
        internal void DrawValue(GInteractiveDrawArgs e, Rectangle boundsAbsolute, object value, TableValueType valueType, Row row, Cell cell)
        {
            switch (valueType)
            {
                case TableValueType.Null:
                    this.DrawNull(e, boundsAbsolute, row, cell);
                    break;
                case TableValueType.IDrawItem:
                    this.DrawIDrawItem(e, boundsAbsolute, row, cell, value as IDrawItem);
                    break;
                case TableValueType.ITimeInteractiveGraph:
                    this.DrawContentInteractiveTimeGraph(e, boundsAbsolute, row, cell, value as ITimeInteractiveGraph);
                    break;
                case TableValueType.ITimeGraph:
                    this.DrawContentTimeGraph(e, boundsAbsolute, row, cell, value as ITimeGraph);
                    break;
                case TableValueType.Image:
                    this.DrawContentImage(e, boundsAbsolute, row, cell, value as Image);
                    break;
                case TableValueType.Text:
                    this.DrawContentText(e, boundsAbsolute, row, cell, value);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí prázdnou buňku / řádek (jen pozadí)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        private void DrawNull(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell)
        {
            this.DrawRowBackground(e, boundsAbsolute, row, cell);
        }
        /// <summary>
        /// Vykreslí obsah this buňky pomocí její vlastní metody IDrawItem.Draw()
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawItem"></param>
        private void DrawIDrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, IDrawItem drawItem)
        {
            try { drawItem.Draw(e, boundsAbsolute); }
            catch { }
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako interaktivní časový graf
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="graph"></param>
        private void DrawContentInteractiveTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, ITimeInteractiveGraph graph)
        {
            this.DrawRowBackground(e, boundsAbsolute, row, cell);

            if (graph.TimeConvertor == null)
                graph.TimeConvertor = this.GetTimeConvertor(cell);
            if (graph.Parent == null)
                graph.Parent = this.GetInteractiveParent(row, cell);
            Rectangle boundsClient = this.GetBoundsClient(row, cell);
            if (graph.Bounds != boundsClient)
                graph.Bounds = boundsClient;

            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako časový graf
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="graph"></param>
        private void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, ITimeGraph graph)
        {
            this.DrawRowBackground(e, boundsAbsolute, row, cell);

            if (graph.TimeConvertor == null)
                graph.TimeConvertor = this.GetTimeConvertor(cell);

            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako Image
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="image"></param>
        private void DrawContentImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, Image image)
        {
            this.DrawRowBackground(e, boundsAbsolute, row, cell);

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
        private void DrawContentText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, object value)
        {
            if (row == null || cell == null) return;

            if (!(row.BackgroundValueType == TableValueType.ITimeInteractiveGraph && cell != null))
                this.DrawRowBackground(e, boundsAbsolute, row, cell);

            // Obsah řádku:
            string formatString = cell.Column.FormatString;
            ContentAlignment textAlignment;
            string text = GetText(value, formatString, out textAlignment);

            VisualStyle style = ((IVisualMember)cell).Style;
            ContentAlignment alignment = style.ContentAlignment ?? textAlignment;
            FontInfo font = style.Font ?? FontInfo.Default;
            Color textColor = this.GetTextColor(row, cell);

            Rectangle boundsContent = boundsAbsolute.Enlarge(-1);
            GPainter.DrawString(e.Graphics, boundsContent, text, textColor, font, alignment);
        }
        /// <summary>
        /// Převede danou hodnotu (obsah buňky) na string s využitím formátovacího řetězce, a podle konkrétního datového typu určí výchozí zarovnání.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="formatString"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        internal static string GetText(object value, string formatString, out ContentAlignment alignment)
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
        /// <summary>
        /// Metoda vrátí konvertor dat (časovou osu) pro konverze souřadnic v prvcích typu TimeGraph
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private ITimeConvertor GetTimeConvertor(Cell cell)
        {
            if (cell != null && cell.Column != null && cell.Column.ColumnHeader != null && cell.Column.ColumnHeader.TimeConvertor != null)
                return cell.Column.ColumnHeader.TimeConvertor;
            if (this.Grid.MainTimeAxis != null)
                return this.Grid.MainTimeAxis;
            return null;
        }
        /// <summary>
        /// Metoda vrátí objekt IInteractiveParent z dodaného řádku a buňky (buňka má přednost, pokud je zadaná)
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private IInteractiveParent GetInteractiveParent(Row row, Cell cell)
        {
            if (cell != null) return (IInteractiveParent)cell.Control;
            if (row != null) return (IInteractiveParent)row.Control;
            return null;
        }
        /// <summary>
        /// Metoda vrátí klientské souřadnice z buňky nebo řádku.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private Rectangle GetBoundsClient(Row row, Cell cell)
        {
            if (cell != null) return cell.Control.Bounds.Sub(cell.Control.ClientBorder);
            if (row != null) return row.Control.Bounds.Sub(row.Control.ClientBorder);
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vykreslí pozadí (background) pro danou buňku jednoho řádku.
        /// Metoda nekreslí GridLines.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="cell"></param>
        private void DrawRowBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell)
        {
            float? effect3d = null;
            Color backColor = this.GetBackColor(row, cell, ref effect3d);
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
            GPainter.DrawBorder(e.Graphics, boundsAbsolute, RectangleSide.Right | RectangleSide.Bottom, null, color, null);
            if (cell.Row.IsActive)
            {
                Rectangle boundsActive = boundsAbsolute.Enlarge(0, 0, 0, -1);
                Color colorTop = color.Morph(Skin.Modifiers.Effect3DDark, Skin.Modifiers.Effect3DRatio);
                Color colorBottom = color.Morph(Skin.Modifiers.Effect3DLight, Skin.Modifiers.Effect3DRatio);
                GPainter.DrawBorder(e.Graphics, boundsActive, RectangleSide.Top | RectangleSide.Bottom, null, colorTop, null, colorBottom, null);
            }
            // this.Host.DrawBorder(e.Graphics, boundsAbsolute, color, linesType, true);
        }
        /// <summary>
        /// Vrátí barvu pozadí pro danou definici a vizuální styl.
        /// Akceptuje: aktivní řádek, buňku, focus, selected.
        /// Dále vyhodnotí VisualStyle buňky nebo řádku.
        /// </summary>
        /// <param name="row">Řádek</param>
        /// <param name="cell">Buňka</param>
        /// <param name="effect3D">Určení 3D stylu</param>
        /// <returns></returns>
        public Color GetBackColor(Row row, Cell cell, ref float? effect3D)
        {
            if (row == null && cell == null) return Skin.Grid.RowBackColor;

            // Vizuální styl z buňky - z řádku - z tabulky:
            VisualStyle style = (cell != null ? ((IVisualMember)cell).Style : (row != null ? ((IVisualMember)row).Style : ((IVisualMember)this.DataTable).Style));

            // Základní barva pozadí prvku vychází z barvy standardní, nebo Selected, podle stavu row.IsSelected; primárně z dodaného vizuálního stylu, sekundárně z palety:
            Color baseColor = ((style == null) ?
                (row.IsSelected ? Skin.Grid.SelectedRowBackColor : Skin.Grid.RowBackColor) :
                (row.IsSelected ? (style.SelectedBackColor ?? Skin.Grid.SelectedRowBackColor) : (style.BackColor ?? Skin.Grid.RowBackColor)));

            // Základní barva je poté morfována do barvy Active v poměru, který vyjadřuje aktivitu řádku, buňky, a focus tabulky, a stav HotMouse:
            float ratio = this.GetMorphRatio(row, cell, ref effect3D);

            // Pokud prvek není aktivní (aktivní řádek ani aktivní buňka), pak má základní barvu - bez morphování:
            if (ratio == 0f) return baseColor;

            // Pokud je aktuální prvek v nějakém aktivním stavu (má kladné ratio pro morfing barvy):
            Color activeColor = ((style == null) ?
                Skin.Grid.ActiveCellBackColor : 
                style.ActiveBackColor ?? Skin.Grid.ActiveCellBackColor);

            return baseColor.Morph(activeColor, ratio);
        }
        /// <summary>
        /// Vrátí barvu pro vykreslení textu dané buňky a řádku, pro jejich vizuální styl.
        /// Akceptuje: aktivní řádek, buňku, focus, selected.
        /// Dále vyhodnotí VisualStyle řádku.
        /// </summary>
        /// <param name="row">Řádek</param>
        /// <param name="cell">Buňka</param>
        /// <returns></returns>
        public Color GetTextColor(Row row, Cell cell)
        {
            if (row == null && cell == null) return Skin.Grid.RowTextColor;

            // Vizuální styl z buňky - z řádku - z tabulky:
            VisualStyle style = (cell != null ? ((IVisualMember)cell).Style : (row != null ? ((IVisualMember)row).Style : ((IVisualMember)this.DataTable).Style));

            // Základní barva prvku je podle jeho stavu isSelected, primárně ze stylu prvku, při nezadání barvy pak z odpovídající položky Skinu pro Grid:
            Color baseColor = ((style == null) ?
                (row.IsSelected ? Skin.Grid.SelectedRowTextColor : Skin.Grid.RowTextColor) :
                (row.IsSelected ? (style.SelectedTextColor ?? Skin.Grid.SelectedRowTextColor) : (style.TextColor ?? Skin.Grid.RowTextColor)));

            // Základní barva je poté morfována do barvy Active v poměru, který vyjadřuje aktivitu řádku, buňky, a focus tabulky, a stav HotMouse:
            float ratio = this.GetMorphRatio(row, cell);

            // Pokud prvek není aktivní (aktivní řádek ani aktivní buňka), pak má základní barvu - bez morphování:
            if (ratio == 0f) return baseColor;

            // Pokud je aktuální prvek v nějakém aktivním stavu (má kladné ratio pro morfing barvy):
            Color activeColor = ((style == null) ?
                Skin.Grid.ActiveCellTextColor :
                (style.ActiveTextColor ?? Skin.Grid.ActiveCellTextColor));

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
                target.CallActiveRowChanged(oldActiveRow, newActiveRow, eventSource, !this.IsSuppressedEvent);
        }
        protected void CallHotRowChanged(Row oldHotRow, Row newHotRow, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallHotRowChanged(oldHotRow, newHotRow, eventSource, !this.IsSuppressedEvent);
        }
        protected void CallHotCellChanged(Cell oldHotCell, Cell newHotCell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallHotCellChanged(oldHotCell, newHotCell, eventSource, !this.IsSuppressedEvent);
        }
        protected void CallActiveCellChanged(Cell oldActiveCell, Cell newActiveCell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallActiveCellChanged(oldActiveCell, oldActiveCell, eventSource, !this.IsSuppressedEvent);
        }
        protected void CallCellMouseEnter(Cell cell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallCellMouseEnter(cell, eventSource, !this.IsSuppressedEvent);
        }
        protected void CallCellMouseLeave(Cell cell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallCellMouseLeave(cell, eventSource, !this.IsSuppressedEvent);
        }
        protected void CallActiveCellClick(Cell cell, EventSourceType eventSource)
        {
            ITableEventTarget target = (this.DataTable as ITableEventTarget);
            if (target != null)
                target.CallActiveCellClick(cell, eventSource, !this.IsSuppressedEvent);
        }
        #endregion
    }
}
