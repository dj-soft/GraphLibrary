using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;
using Djs.Common.Data.New;

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
        #region Inicializace
        internal GTable(GGrid grid, Table table)
        {
            this._Grid = grid;
            this._Table = table;
            this.Init();
        }
        private void Init()
        {
            this.InitRowsPositions();
            this.InitHeaderSplitter();
            this.InitTableSplitter();
        }
        void IDisposable.Dispose()
        {
            ((IGridMember)this).DetachFromGrid();
            this._Table = null;
        }
        #endregion
        #region IGridMember - Linkování na grid
        /// <summary>
        /// Reference na grid, kam tato tabulka patří.
        /// </summary>
        public GGrid Grid { get { return this._Grid; } private set { this._Grid = value; } }
        private GGrid _Grid;
        /// <summary>
        /// true pokud máme referenci na grid
        /// </summary>
        public bool HasGrid { get { return (this._Grid != null); } }
        /// <summary>
        /// Napojí this tabulku do daného gridu.
        /// Je voláno z gridu, v eventu ItemAdd kolekce Tables.
        /// </summary>
        /// <param name="dTable"></param>
        void IGridMember.AttachToGrid(GGrid grid, int id)
        {
            this._TableId = id;
            if (this.DataTable.TableOrder < 0)
                this.DataTable.TableOrder = id;
            this._Grid = grid;
        }
        /// <summary>
        /// Odpojí this tabulku z gridu.
        /// Je voláno z gridu, v eventu ItemRemove kolekce Tables.
        /// </summary>
        void IGridMember.DetachFromGrid()
        {
            this._TableId = -1;
            this.DataTable.TableOrder = -1;
            this._Grid = null;
        }
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
            this.Invalidate(InvalidateItem.TableBounds);
        }
        /// <summary>
        /// Metoda zajistí, že souřadnice vnitřních objektů budou platné a budou odpovídat aktuální velikosti Tabulky a poloze splitterů a rozsahu dat.
        /// Jde o hodnoty pro prvky tabulky: ColumnHeader, HeaderSplitter, RowArea, Scrollbar.
        /// </summary>
        protected void _InnerBoundsCheck()
        {
            this._TableLayoutValid = false;
            Size clientSize = this.ClientSize;

            if (clientSize.Width <= 0 || clientSize.Height <= 0) return;

            this._TableLayoutValid = true;                                           // Normálně to patří až na konec metody. Ale některé komponenty mohou používat již částečně napočtené hodnoty, a pak bychom se zacyklili

            // Bude viditelný scrollbar řádků? (to je tehdy, když výška zobrazitelných řádků je větší než výška prostoru pro řádky):
            //  Objekt RowsPositions tady provede dotaz na velikost dat (metoda _RowsPositionGetDataSize()) a velikost viditelného prostoru (metoda _RowsPositionGetVisualSize()).
            //  Velikost dat je dána pozicí End posledního řádku z this.RowListAll,
            //  velikost viditelného prostoru pro tabulky je dána výškou tabulky mínus výška hlavičky (ClientSize.Height - RowsPositions.VisualFirstPixel).
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
            int y5 = this.Bounds.Bottom;                                            // y5: this.Bottom v souřadném systému mého parenta, pro TableSplitterBounds

            this._TableHeaderBounds = new Rectangle(x0, y0, x1 - x0, y1 - y0);
            this._ColumnHeaderBounds = new Rectangle(x1, y0, x3 - x1, y1 - y0);
            this._RowHeaderBounds = new Rectangle(x0, y1, x1 - x0, y3 - y1);
            this._RowAreaBounds = new Rectangle(x1, y1, x2r - x1, y3 - y1);
            this._RowsScrollBarBounds = new Rectangle(x2t, y1, x3 - x2t, y3 - y1);
            this._HeaderSplitterBounds = new Rectangle(x0, y1, x3 - x0, 0);
            this._TableSplitterBounds = new Rectangle(x0, y5, x3 - x0, 0);

            this._VisibleColumns = null;
            this._VisibleRows = null;
            this._RowsScrollBarValid = false;
            this._HeaderSplitterValid = false;
            this._TableSplitterValid = false;
            this._ChildArrayValid = false;
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
        protected Rectangle TableSplitterBounds { get { this._InnerBoundsCheck(); return this._TableSplitterBounds; } } private Rectangle _TableSplitterBounds;
        /// <summary>
        /// true pokud je layout vnitřních prostor tabulky korektně spočítán
        /// </summary>
        protected bool TableLayoutValid { get { return this._TableLayoutValid; } } private bool _TableLayoutValid;
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
            Point absoluteOrigin = this.GetAbsoluteOriginPoint();
            Rectangle relativeBounds = this.GetRelativeBoundsForArea(areaType);
            return relativeBounds.Add(absoluteOrigin);
        }
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
        #region Sloupce tabulky
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
            GPosition columnsPositions = this.Grid.ColumnsPositions;
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
        #region Pozicování řádků = svislé : pozicioner pro řádky, svislý scrollbar vpravo
        /// <summary>
        /// Inicializace objektů pro pozicování tabulek: TablesPositions, TablesScrollBar
        /// </summary>
        private void InitRowsPositions()
        {
            this._RowsPositions = new GPosition(DefaultColumnHeaderHeight, 50, this._RowsPositionGetVisualSize, this._RowsPositionGetDataSize, this._GetVisualFirstPixel, this._SetVisualFirstPixel);

            this._RowsScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this._RowsScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(RowsScrollBar_ValueChange);
            this._RowsScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(RowsScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídící prvek pro Pozice řádků
        /// </summary>
        protected GPosition RowsPositions { get { return this._RowsPositions; } }
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
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        private void _RowsScrollBarCheck()
        {
            if (this._RowsScrollBarValid) return;

            if (this.RowsScrollBarVisible)
                this._RowsScrollBar.LoadFrom(this.RowsPositions, this.RowsScrollBarBounds, true);

            this._RowsScrollBarValid = true;
        }
        /// <summary>
        /// Datový pozicioner pro řádky
        /// </summary>
        private GPosition _RowsPositions;
        /// <summary>
        /// Scrollbar pro řádky
        /// </summary>
        private GScrollBar _RowsScrollBar;
        /// <summary>
        /// true po naplnění RowsScrollBar platnými daty, false po invalidaci
        /// </summary>
        private bool _RowsScrollBarValid;
        #endregion
        #region Řádky tabulky - zde jsou uložena dvě oddělená pole řádků: a) všechny aktuálně dostupné datové řádky - pro práci s kolekcí řádků, b) pouze viditelné grafické řádky - pro kreslení
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
                heightValid = false;
            }
            if (!heightValid)
            {
                SequenceLayout.SequenceLayoutCalculate(rows);                                      // Napočítat jejich ISequenceLayout.Begin a .End
                this._RowListHeightValid = true;
                this._VisibleRows = null;
            }
            if (this._Rows == null)
                this._Rows = rows;
        }
        /// <summary>
        /// Ověří a zajistí připravenost pole VisibleRows.
        /// Viditelné řádky mají korektně nastaveny aktuální souřadnice do row.RowHeader.VisualRange, neviditelné mají RowHeader.VisualRange == null.
        /// </summary>
        private void _VisibleRowsCheck()
        {
            if (this._VisibleRows != null) return;

            List<Row> visibleRows = new List<Row>();
            GPosition rowsPositions = this.RowsPositions;
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
        #region Řádky tabulky - posuny, aktivní řádek atd
        /// <summary>
        /// Posune oblast řádků tabulky podle dané akce
        /// </summary>
        /// <param name="action"></param>
        internal void ProcessRowAction(InteractivePositionAction action)
        { }
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

            if ((items & (InvalidateItem.TableBounds)) != 0)
            {   // Po změně rozměrů tabulky invalidujeme všechny viditelné prvky:
                this._TableLayoutValid = false;
                this._VisibleColumns = null;
                this._VisibleRows = null;
                this._RowsScrollBarValid = false;
                this._HeaderSplitterValid = false;
                this._ChildArrayValid = false;
            }

            if ((items & (InvalidateItem.TableHeight)) != 0)
            {   // Po změně výšky tabulky:
                this._TableLayoutValid = false;
                this._VisibleColumns = null;
                this._VisibleRows = null;
                this._ChildArrayValid = false;
            }

            if ((items & (InvalidateItem.RowHeader)) != 0)
            {   // Po změně šířky RowHeader pozice Rovnitřního uspořádání (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů (RowHeader, ColumnHeader):
                this._TableLayoutValid = false;
                this._VisibleColumns = null;
                this._ChildArrayValid = false;
                callGrid = true;
            }

            if ((items & (InvalidateItem.ColumnHeader)) != 0)
            {   // Po změně vnitřního uspořádání (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů (RowHeader, ColumnHeader):
                this._TableLayoutValid = false;
                this._VisibleColumns = null;
                this._VisibleRows = null;
                this._RowsScrollBarValid = false;
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
                this._RowsScrollBarValid = false;
                this._ChildArrayValid = false;
            }

            if ((items & (InvalidateItem.RowHeight)) != 0)
            {   // Po změně výšky řádku: zrušíme příznak platnosti výšky v řádcích, a zrušíme pole viditelných řádků, vygeneruje se znovu:
                this._RowListHeightValid = false;
                this._VisibleRows = null;
                this._RowsScrollBarValid = false;
                this._ChildArrayValid = false;
            }
            if ((items & (InvalidateItem.RowScroll)) != 0)
            {   // Po scrollu řádků: zrušíme pole viditelných řádků, vygeneruje se znovu:
                this._VisibleRows = null;
                this._ChildArrayValid = false;
            }

            // Předáme to šéfovi, pokud ho máme, a pokud to pro něj může být zajímavé, a pokud událost není určena jen pro naše (OnlyForTable) potřeby:
            if (this.HasGrid && callGrid && !items.HasFlag(InvalidateItem.OnlyForTable))
                this.Grid.Invalidate(items);
        }
        #endregion
        #region Interaktivita objektů tabulky do tabulky a dále
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví sloupce.
        /// </summary>
        /// <param name="column"></param>
        public void ColumnHeaderClick(Column column)
        {
            if (column != null)
            {
                column.Table.ColumnHeaderClick(column);
                if (column.AllowColumnSortByClick && column.Table.AllowColumnSortByClick)
                {
                    column.SortChange();
                    this.Repaint();
                }
            }
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví řádku.
        /// </summary>
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
        /// Eventhandler události _TableSplitter.LocationChanged (došlo nebo stále dochází ke změně pozice splitteru od tabulkou)
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
            if (this._HeaderSplitterValid) return;
            Rectangle bounds = this.HeaderSplitterBounds;
            this._HeaderSplitter.LoadFrom(bounds.Y, bounds, true);
            this._HeaderSplitterValid = true;
        }
        /// <summary>
        /// Platnost dat v HeaderSplitter
        /// </summary>
        private bool _HeaderSplitterValid;
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
            this.DataTable.Height = value;                           // Tady dojde ke kompletnímu vyhodnocení pravidel pro výšku Table (Minimum, Default, Range)
            e.CorrectValue = this.DataTable.Height;                  // Pokud požadovaná hodnota (value) nebyla akceptovatelná, pak correctValue je hodnota přípustná
            if (e.IsChangeValue)
            {
                this.Grid.RecalculateGrid();
                this.Grid.Repaint();
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
            if (this._TableSplitterValid) return;
            Rectangle bounds = this.TableSplitterBounds;
            this._TableSplitter.LoadFrom(bounds.Y, bounds, true);
            this._TableSplitterValid = true;
        }
        /// <summary>
        /// Platnost dat v TableSplitter
        /// </summary>
        private bool _TableSplitterValid;
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
        /// Pokud je neplatné, zavolá this.GridItemsReload()
        /// </summary>
        private void _ChildArrayCheck()
        {
            if (this._ChildArrayValid) return;
            this.ChildList.Clear();
            this._ChildItemsAddColumnHeaders();                      // Záhlaví sloupců (TableHeader + ColumnHeaders)
            this._ChildItemsAddColumnSplitters();                    // Oddělovače sloupců, které to mají povoleno
            this._ChildItemsAddRowsContent();                        // Řádky: záhlaví plus buňky, ale ne oddělovače
            this._ChildItemsAddHeaderSplitter();                     // Oddělovač pod hlavičkami sloupců (řídí výšku záhlaví)
            this._ChildItemsAddRowsSplitters();                      // Řádky: oddělovače řádků, pokud je povoleno
            this._ChildItemsAddRowsScrollBar();                      // Scrollbar řádků, pokud je viditelný
            this._ChildArrayValid = true;
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechna záhlaví sloupců (tedy TableHeader + VisibleColumns.ColumnHeader).
        /// Nepřidává splitetry: ani mezi sloupci, ani pod Headers.
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
                columnHeader.Bounds = new Rectangle(visualRange.Begin, headerBounds.Y, visualRange.Size, headerBounds.Height);
                this.ChildList.Add(columnHeader);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá splittery za sloupci. Přidává i splitter za posledním sloupcem.
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
                this.ChildItemsAddRowContent(row);
        }
        /// <summary>
        /// Do pole this.ChildList přidá obsah jednoho daného řádku.
        /// </summary>
        /// <param name="row"></param>
        protected void ChildItemsAddRowContent(Row row)
        {
            Rectangle rowHeaderBounds = this.RowHeaderBounds;
            Rectangle rowAreaBounds = this.RowAreaBounds;

            GRowHeader rowHeader = row.RowHeader;
            Int32Range rowVisualRange = rowHeader.VisualRange;
            rowHeader.Bounds = new Rectangle(rowHeaderBounds.X, rowVisualRange.Begin, rowHeaderBounds.Width, rowVisualRange.Size);
            this.ChildList.Add(rowHeader);

            foreach (Cell cell in row.Cells)
            {
                cell.g
            }
        }
        protected void _ChildItemsAddHeaderSplitter()
        { }
        protected void _ChildItemsAddRowsSplitters()
        {
            if (!this.datat
            foreach (Row row in this.VisibleRows)
                this.ChildItemsAddRowSplitters(row);

        }
        protected void ChildItemsAddRowSplitters(Row row)
        {
            
        }
        protected void _ChildItemsAddRowsScrollBar()
        {
            if (this.RowsScrollBarVisible)
            {
                this.RowsScrollBar.Bounds = this.RowsScrollBarBounds;
                this.ChildList.Add(this.RowsScrollBar);
            }
        }

        #endregion
        #region Reakce na GUI eventy
        /// <summary>
        /// Is called after ColumnHeader is clicked
        /// </summary>
        /// <param name="columnId"></param>
        internal void ColumnHeaderClicked(int columnId)
        {
            Column column;
            if (this.DataTable.TryGetColumn(columnId, out column))
            {
                if (column.SortChange())
                    this.Invalidate(InvalidateItem.RowOrder);
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
        /// Send informations about changes on one TimeAxis control for specified column (in GGrid object) to all GTables in this GGrid.
        /// All GTables in one GGrid have synchronized TimeAxis value in same column.
        /// </summary>
        /// <param name="columnId"></param>
        internal void RefreshTimeAxis(int columnId)
        {
            this._ColumnSet.RefreshTimeAxis(columnId);
        }
        /// <summary>
        /// Return an ITimeConvertor (from TimeAxis) for specified columnId.
        /// Is called from Drawing an TimeGraph, for accessing TimeConvertor for specified column, for conversion Time values to Pixel values.
        /// Can return null for non-existing columnId or for column, where TimeAxis is not used.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        internal ITimeConvertor GetTimeConvertor(int columnId)
        {
            return this._ColumnSet.GetTimeConvertor(columnId);
        }
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {

        }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
            // GTable kreslí pouze svoje vlastní pozadí (a to by si ještě měla rozmyslet, kolik ho bude, než začne malovat úplně celou plochu :-) ):
            if (this.DataTable == null || this.DataTable.ColumnsCount == 0)
                base.Draw(e);

            // Všechno ostatní (záhlaví sloupců, řádky, scrollbary, splittery) si malují Childs samy.
        }
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
    }
    #region Třídy GHeader, GColumnHeader, GRowHeader
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
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Grid (grafický), do kterého patří zdejší tabulka
        /// </summary>
        protected GGrid OwnerGGrid { get { return (this.OwnerTable != null ? this.OwnerTable.GTable.Grid : null); } }
        /// <summary>
        /// Tabulka (grafická), do které patří toto záhlaví
        /// </summary>
        protected GTable OwnerGTable { get { return (this.OwnerTable != null ? this.OwnerTable.GTable : null); } }
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
        #region Interaktivita, kreslení
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerTable.TableHeaderClick();
        }
        protected override bool CanDrag { get { return false; } }
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            // Clip() mi zajistí, že při pixelovém posunu záhlaví (sloupce, řádky) bude záhlaví vykresleno jen do příslušné části vymezeného prostoru pro danou oblast.
            // Grafická organizace GTable není členěna nijak výrazně strukturovaně => GTable obsahuje jako Child jednotlivé prvky (GHeader, GColumn),
            //  které mají svoje souřadnice relativní k GTable, ale mají se zobrazovat "oříznuté" do patřičných oblastí v GTable.
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                // Clip() ale provedeme jen pro Standard vrstvu; protože v ostatních vrstvách se provádí Dragging, a ten má být neomezený:
                e.GraphicsClipWith(this.OwnerGTable.GetAbsoluteBoundsForArea(this.HeaderType));

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
                e.GraphicsClipWith(this.OwnerGTable.GetAbsoluteBoundsForArea(this.HeaderType));

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
            int location = this._ColumnSplitter.Value;
            int width = location - left;
            this.OwnerGGrid.ColumnRowHeaderResizeTo(ref width);
            e.CorrectValue = left + width;
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
        }
        private Column _OwnerColumn;
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
            int location = this._ColumnSplitter.Value;
            int width = location - left;
            this.OwnerGGrid.ColumnResizeTo(this.OwnerColumn, ref width);
            e.CorrectValue = left + width;
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _ColumnSplitter;
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
            this.OwnerGTable.ColumnHeaderClick(this.OwnerColumn);
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
            : base(row.Table)
        {
            this._OwnerRow = row;
            this._RowSplitterInit();
        }
        private Row _OwnerRow;
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
        public bool ColumnSplitterVisible { get { return (this.OwnerTable.AllowRowResize); } }
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
            int value = this._RowSplitter.Value;
            int height = value - top;
            this.OwnerGTable.RowResizeTo(this.OwnerRow, ref height);
            e.CorrectValue = top + height;
        }
        /// <summary>
        /// RowSplitter
        /// </summary>
        protected GSplitter _RowSplitter;
        #endregion
        #region Interaktivita
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
            this.DrawSelectedRow(e, boundsAbsolute, opacity);
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
        public GCell(Row row, Column column)
        {
            this._Row = row;
            this._Column = column;
        }
        private Row _Row;
        private Column _Column;
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
    /// Set of all columns in one GTable
    /// </summary>
    public class GColumnSet : InteractiveContainer, IEnumerable<GColumn>
    {
        #region Constructor, public properties
        public GColumnSet(GTable table, DTable dataTable)
        {
            this._Columns = new Dictionary<int, GColumn>();
            this._HeaderColumn = new GColumn(this, null, -1);
            this._Table = table;
            this._PrepareColumnSetSplitter();
            this.ColumnIDNext = 0;
            this.ReloadColumns(dataTable);
        }
        /// <summary>
        /// Owner GGrid
        /// </summary>
        internal GGrid GGrid { get { return this._Table.GGrid; } }
        /// <summary>
        /// Owner table of this GColumnSet
        /// </summary>
        public GTable GTable { get { return this._Table; } } private GTable _Table;
        /// <summary>
        /// DataTable
        /// </summary>
        protected DTable DataTable { get { return (this.GTable != null ? this.GTable.DataTable : null); } }
        #endregion
        #region BoundsChange
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            this.SetPositions(newBounds, ref actions, eventSource);
        }
        /// <summary>
        /// Set all positions into this.Columns and ColumnSetSplitter, by current this.Bounds.
        /// </summary>
        internal void RefreshPositions()
        {
            Rectangle bounds = this.Bounds;
            ProcessAction actions = ProcessAction.PrepareInnerItems;
            EventSourceType eventSource = EventSourceType.BoundsChange | EventSourceType.ApplicationCode;
            this.SetPositions(bounds, ref actions, eventSource);
        }
        protected void SetPositions(Rectangle bounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            GridPositions gridPositions = this.GridPositions;
            gridPositions.ReloadColumns(this._Columns.Values.Select(g => g.DataColumn), false);

            this._HeaderColumn.SetPosition(bounds, actions, eventSource);
            foreach (var column in this._Columns.Values)
                column.SetPosition(bounds, actions, eventSource);

            int maxVisiblePixel = gridPositions.ColumnsMaxVisiblePixel;
            if (maxVisiblePixel > bounds.Right) maxVisiblePixel = bounds.Right;
            this.ColumnSetSplitter.BoundsNonActive = new Int32NRange(0, maxVisiblePixel - 1);
            this.ColumnSetSplitter.ValueSilent = bounds.Bottom;

            int dataColumnsLeft = this._HeaderColumn.Bounds.Right;
            this._BoundsDataColumns = new Rectangle(dataColumnsLeft, bounds.Y, maxVisiblePixel - 1 - dataColumnsLeft, bounds.Height);
        }
        /// <summary>
        /// Positions of all visual items (Columns and Tables) = this.Table.Grid.Positions
        /// </summary>
        protected GridPositions GridPositions { get { return this.GTable.GGrid.Positions; } }
        /// <summary>
        /// Position of this.Table (=vertical positions for Table and splitters)
        /// </summary>
        protected GridPositionItem TablePosition { get { return this.GTable.TablePosition; } }
        /// <summary>
        /// Bounds for DataColumns area = except RowHeaderColumn, and with Right coordinate = Right from last visible column
        /// </summary>
        public Rectangle BoundsDataColumns { get { return this._BoundsDataColumns; } } Rectangle _BoundsDataColumns;
        #endregion
        #region TimeAxis
        /// <summary>
        /// Refill TimeAxis value from column (columnId) into this ColumnSet
        /// </summary>
        /// <param name="columnId"></param>
        internal void RefreshTimeAxis(int columnId)
        {
            GColumn column;
            if (this._Columns.TryGetValue(columnId, out column))
                column.RefreshTimeAxis();
        }
        /// <summary>
        /// Return an ITimeConvertor (from TimeAxis) for specified columnId.
        /// Can return null for non-existing columnId or for column, where TimeAxis is not used.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        internal ITimeConvertor GetTimeConvertor(int columnId)
        {
            GColumn column;
            if (this._Columns.TryGetValue(columnId, out column))
                return column.GetTimeConvertor();
            return null;
        }
        #endregion
        #region ColumnCollection
        /// <summary>
        /// Count of columns in this set
        /// </summary>
        public int Count { get { return this._Columns.Count; } }
        /// <summary>
        /// Get GColumn at index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GColumn this[int index] { get { return this._Columns[index]; } }
        /// <summary>
        /// ID for new inserted column
        /// </summary>
        protected int ColumnIDNext;
        /// <summary>
        /// Add new DataColumn to this Graphic column
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="index"></param>
        internal void AddColumn(DColumn dataColumn, int? index)
        {
            int columnId = this.ColumnIDNext++;
            GColumn column = new GColumn(this, dataColumn, columnId);
            this._Columns.Add(columnId, column);
        }
        /// <summary>
        /// Remove one column from this Graphic column
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="index"></param>
        internal void RemoveColumn(DColumn dataColumn, int? index)
        {
            if (index.HasValue && this._Columns.ContainsKey(index.Value))
                this._Columns.Remove(index.Value);
        }
        /// <summary>
        /// Reload all columns from this.Table.DataTable.
        /// Does not trigger any event.
        /// </summary>
        internal void ReloadColumns()
        {
            this.ReloadColumns(this.DataTable);
        }
        /// <summary>
        /// Reload all columns from specified dataTable.
        /// Does not trigger any event.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void ReloadColumns(DTable dataTable)
        {
            this._Columns.Clear();
            this.ColumnIDNext = 0;
            if (dataTable != null)
            {
                foreach (DColumn dataColumn in dataTable.Columns)
                {
                    int columnId = this.ColumnIDNext++;
                    GColumn column = new GColumn(this, dataColumn, columnId);
                    this._Columns.Add(columnId, column);
                }
            }
        }
        IEnumerator<GColumn> IEnumerable<GColumn>.GetEnumerator() { return this._Columns.Values.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this._Columns.Values.GetEnumerator(); }
        protected GColumn _HeaderColumn;
        protected Dictionary<int, GColumn> _Columns;
        #endregion
        #region ColumnSetSplitter
        /// <summary>
        /// ColumnSetSplitter = GSplitter at bottom end of this ColumnSet.
        /// Is not part of this.Childs, but is one of GTable items (on same level as this GColumnSet).
        /// Its LinkedItemPrev object is this GColumnSet, but LinkedItemNext must be set in GTable object (LinkedItemNext is GRowSet).
        /// </summary>
        internal GSplitter ColumnSetSplitter { get { return this._ColumnSetSplitter; } }
        /// <summary>
        /// Prepare new _TableSplitter.
        /// </summary>
        protected void _PrepareColumnSetSplitter()
        {
            this._ColumnSetSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 1, SplitterActiveOverlap = 3, LinkedItemPrevMinSize = 20, LinkedItemNextMinSize = 30, IsResizeToLinkItems = true };
            this._ColumnSetSplitter.ValueSilent = this.Bounds.Bottom;
            this._ColumnSetSplitter.ValueChanging += new GPropertyChanged<int>(_ColumnSetSplitter_LocationChange);
            this._ColumnSetSplitter.ValueChanged += new GPropertyChanged<int>(_ColumnSetSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler for _ColumnSetSplitter.LocationChanged event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ColumnSetSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            GridPositionItem tablePosition = this.TablePosition;
            int location = this._ColumnSetSplitter.Value;
            tablePosition.Split = location;
            this.GTable.Invalidate(InvalidateItem.TableHeight);
            this.GTable.Repaint();
            // this.RepaintAllItems = true;
        }
        /// <summary>
        /// TableSplitter
        /// </summary>
        protected GSplitter _ColumnSetSplitter;
        #endregion
        #region Draw (GColumnSet draw background behind Columns header)
        protected override void Draw(GInteractiveDrawArgs e)
        {
            base.Draw(e);
        }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
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

            // Data columns:
            this.ChildList.AddRange(this._Columns.Values);

            // Header column, on top:
            this.ChildList.Add(this._HeaderColumn);

            // Columns splitter - after all Columns:
            this.ChildList.AddRange(this._Columns.Values.Select(c => c.ColumnSplitter));

            this.ChildList.Add(this._HeaderColumn.ColumnSplitter);

            this._ChildArrayValid = true;
        }
        private bool _ChildArrayValid;
        #endregion
    }
    /// <summary>
    /// GColumn : Container for one ColumnHeader objects, for one Table. Contain items: Header, TimeAxis, Splitter.
    /// </summary>
    public class xxxGColumn : InteractiveContainer
    {
        #region Constructor, public properties
        public GColumn(GColumnSet columnSet, DColumn dataColumn, int columnID)
        {
            this._ColumnSet = columnSet;
            this._DataColumn = dataColumn;
            this._ColumnID = columnID;
            this._PrepareColumnHeader();
            this._PrepareTimeAxis();
            this._PrepareColumnSplitter();
        }
        /// <summary>
        /// Positions of all visual items (Columns and Tables)
        /// </summary>
        internal GridPositions GridPositions { get { return this.GGrid.Positions; } }
        /// <summary>
        /// PositionItem for this column
        /// </summary>
        internal GridPositionColumnItem ColumnPosition { get { return this.GridPositions.GetColumn(this.ColumnID); } }
        /// <summary>
        /// Owner GGrid
        /// </summary>
        internal GGrid GGrid { get { return this.GTable.GGrid; } }
        /// <summary>
        /// Owner GTable
        /// </summary>
        internal GTable GTable { get { return this._ColumnSet.GTable; } }
        /// <summary>
        /// Owner ColumnSet
        /// </summary>
        internal GColumnSet ColumnSet { get { return this._ColumnSet; } } private GColumnSet _ColumnSet;
        /// <summary>
        /// Data for this column
        /// </summary>
        internal DColumn DataColumn { get { return this._DataColumn; } } private DColumn _DataColumn;
        /// <summary>
        /// true when GColumn has own DataColumn
        /// </summary>
        internal bool HasDataColumn { get { return (this._DataColumn != null); } }
        /// <summary>
        /// ID of this Graphic column
        /// </summary>
        internal int ColumnID { get { return this._ColumnID; } } private int _ColumnID;
        /// <summary>
        /// true when this column is RowHeaderColumn (then will be DataColumn == null !)
        /// </summary>
        internal bool IsRowHeaderColumn { get { return (this._ColumnID == -1); } }

        /// <summary>
        /// true when this column is "Sorting" (Asc or Desc), by this.SortType
        /// </summary>
        internal bool IsSortColumn { get { return (this._SortType == DTableSortRowType.Ascending || this._SortType == DTableSortRowType.Descending); } }
        /// <summary>
        /// Current sorting type rows by this column
        /// </summary>
        internal DTableSortRowType SortType { get { return this._SortType; } set { this._SortType = value; } } private DTableSortRowType _SortType = DTableSortRowType.None;
        #endregion
        #region BoundsChange
        /// <summary>
        /// Calculate Bounds for this column (header, splitter), from ColumnSet bounds and this ColumnPosition
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        internal void SetPosition(Rectangle bounds, ProcessAction actions, EventSourceType eventSource)
        {
            GridPositionColumnItem columnPosition = this.ColumnPosition;

            Rectangle columnBounds = new Rectangle(columnPosition.BeginVisual, bounds.Y, columnPosition.SizeVisible, bounds.Height);
            this.SetBounds(columnBounds, actions, eventSource);

            using (this._ColumnSplitter.SuppressEvents())
            {
                this._ColumnSplitter.Value = columnPosition.EndVisual;
                this._ColumnSplitter.BoundsNonActive = new Int32NRange(bounds.Y, bounds.Height);
            }
        }
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            Rectangle bounds = new Rectangle(0, 0, newBounds.Width, newBounds.Height);
            this._ColumnHeader.SetBounds(bounds, actions, eventSource);

            if (this.UseTimeAxis)
            {
                Rectangle timeAxisBounds = bounds.Enlarge(-2, -4, -1, -2);
                ProcessAction timeAxisActions = ProcessAction.RecalcInnerData | ProcessAction.PrepareInnerItems | ProcessAction.RecalcValue;
                this._TimeAxis.SetBounds(timeAxisBounds, timeAxisActions, eventSource);

                if (this.GTable.TablePosition.Order == 0)
                    this.ColumnPosition.TimeRange = this._TimeAxis.Value;
            }
        }
        #endregion
        #region Reorder columns support
        /// <summary>
        /// Draw an insert mark at BEGIN of this column.
        /// Any other column will be inserted before this column.
        /// The meaning of the value: 0 expresses no mark, 100 expresses a most distinctive mark.
        /// </summary>
        internal int DrawInsertMarkAtBegin { get; set; }
        /// <summary>
        /// Draw an insert mark at END of this column.
        /// Any other column will be inserted before this column.
        /// The meaning of the value: 0 expresses no mark, 100 expresses a most distinctive mark.
        /// </summary>
        internal int DrawInsertMarkAtEnd { get; set; }
        /// <summary>
        /// Reset values DrawInsertMarkAtBegin and DrawInsertMarkAtEnd, set RepaintToLayers to Standard, when were changes on Marks.
        /// </summary>
        internal void DrawInsertMarkReset()
        {
            bool reDraw = false;
            if (this.DrawInsertMarkAtBegin != 0)
            {
                this.DrawInsertMarkAtBegin = 0;
                reDraw = true;
            }
            if (this.DrawInsertMarkAtEnd != 0)
            {
                this.DrawInsertMarkAtEnd = 0;
                reDraw = true;
            }
            if (reDraw)
                this.RepaintToLayers = GInteractiveDrawLayer.Standard;
        }
        #endregion
        #region ColumnHeader
        /// <summary>
        /// Graphic for Header of this column
        /// </summary>
        public GColumnHeader ColumnHeader { get { return this._ColumnHeader; } }
        /// <summary>
        /// Prepare new _TableSplitter.
        /// </summary>
        protected void _PrepareColumnHeader()
        {
            this._ColumnHeader = new GColumnHeader(this);
        }
        /// <summary>
        /// ColumnHeader
        /// </summary>
        private GColumnHeader _ColumnHeader;
        #endregion
        #region TimeAxis
        /// <summary>
        /// TimeAxis for this column
        /// </summary>
        public GTimeAxis TimeAxis { get { return this._TimeAxis; } }
        /// <summary>
        /// Use TimeAxis on Grid table
        /// </summary>
        public bool UseTimeAxis { get { return this.DataColumn != null && this.DataColumn.UseTimeAxis; } }
        /// <summary>
        /// Refill TimeAxis value from column (columnId) into this Column
        /// </summary>
        /// <param name="columnId"></param>
        internal void RefreshTimeAxis()
        {
            if (this.UseTimeAxis)
            {
                GridPositionColumnItem columnPosition = this.ColumnPosition;
                this._TimeAxis.ValueSilent = columnPosition.TimeRange;
            }
        }
        /// <summary>
        /// Return an ITimeConvertor (from TimeAxis) for this column
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        internal ITimeConvertor GetTimeConvertor()
        {
            if (this.UseTimeAxis)
            {
                return this._TimeAxis;
            }
            return null;
        }
        /// <summary>
        /// Prepare new _TimeAxis.
        /// </summary>
        protected void _PrepareTimeAxis()
        {
            this._TimeAxis = new GTimeAxis() { Orientation = AxisOrientation.Top };
            this._TimeAxis.ValueChanging += new GPropertyChanged<TimeRange>(_TimeAxis_ValueChange);
            this._TimeAxis.ValueChanged += new GPropertyChanged<TimeRange>(_TimeAxis_ValueChange);
        }
        /// <summary>
        /// Eventhandler for _TimeAxis.ValueChange events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _TimeAxis_ValueChange(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            this.ColumnPosition.TimeRange = this._TimeAxis.Value;
            this.GGrid.RefreshTimeAxis(this.ColumnID);
            this.GGrid.Repaint();
            // this.RepaintAllItems = true;
        }
        /// <summary>
        /// TimeAxis
        /// </summary>
        protected GTimeAxis _TimeAxis;
        #endregion
        #region ColumnSplitter
        /// <summary>
        /// Splitter after this column, control Width for this column (and same column in other tables in this GGrid)
        /// </summary>
        public GSplitter ColumnSplitter { get { return this._ColumnSplitter; } }
        /// <summary>
        /// Prepare new _TableSplitter.
        /// </summary>
        protected void _PrepareColumnSplitter()
        {
            this._ColumnSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Vertical, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._ColumnSplitter.ValueSilent = this.Bounds.Bottom;
            this._ColumnSplitter.ValueChanging += new GPropertyChanged<int>(_ColumnSplitter_LocationChange);
            this._ColumnSplitter.ValueChanged += new GPropertyChanged<int>(_ColumnSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler for _ColumnSplitter.LocationChange event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ColumnSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            GridPositionItem columnPosition = this.ColumnPosition;
            int location = this._ColumnSplitter.Value;
            columnPosition.EndVisual = location;
            this.GGrid.RecalculateGrid();
            this.GGrid.Repaint();
            // this.RepaintAllItems = true;
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _ColumnSplitter;
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
            this.ChildList.Add(this.ColumnHeader);
            if (this.UseTimeAxis)
                this.ChildList.Add(this.TimeAxis);
            this._ChildArrayValid = true;
        }
        private bool _ChildArrayValid;
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
        }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
        }
        /// <summary>
        /// internal access to this.RepaintToLayers value
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        #endregion
    }
    /// <summary>
    /// GColumnHeader : class for visual representing one ColumnHeader.
    /// </summary>
    public class xColumnHeader : InteractiveDragObject, IInteractiveItem
    {
        #region Constructor, public properties
        public GColumnHeader(GColumn column)
        {
            this._Column = column;
            this.DragDrawGhostMode = Components.DragDrawGhostMode.DragWithGhostAtOriginal;
            this.Style |= GInteractiveStyles.LongClick;
        }
        /// <summary>
        /// GColumn, into which is this Header showed
        /// </summary>
        internal GColumn GColumn { get { return this._Column; } } private GColumn _Column;
        /// <summary>
        /// Data for this column
        /// </summary>
        internal DColumn DataColumn { get { return this._Column.DataColumn; } }
        /// <summary>
        /// GColumnSet
        /// </summary>
        internal GColumnSet GColumnSet { get { return (this._Column != null ? this._Column.ColumnSet : null); } }
        /// <summary>
        /// GTable, into which is this Header showed
        /// </summary>
        internal GTable GTable { get { return this._Column.GTable; } }
        /// <summary>
        /// Owner GGrid
        /// </summary>
        internal GGrid GGrid { get { return this._Column.GGrid; } }
        /// <summary>
        /// true when this column is RowHeaderColumn
        /// </summary>
        internal bool IsRowHeaderColumn { get { return this.GColumn.IsRowHeaderColumn; } }
        #endregion
        #region Interactivity (Mouse Enter, Move, Leave, Click)
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            Djs.Common.Localizable.TextLoc toolTip = (this.GColumn.HasDataColumn ? this.GColumn.DataColumn.ToolTip : null);
            if (!String.IsNullOrEmpty(toolTip))
            {
                e.ToolTipData.InfoText = toolTip;
                e.ToolTipData.TitleText = "Column info";
                e.ToolTipData.ShapeType = TooltipShapeType.Rectangle;
                e.ToolTipData.Opacity = 240;
            }
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.GTable.ColumnHeaderClicked(this.GColumn);
        }
        #endregion
        #region Drag
        /// <summary>
        /// true when this item can be dragged in its current state.
        /// false for IsRowHeaderColumn.
        /// </summary>
        protected override bool CanDrag { get { return !this.IsRowHeaderColumn; } }
        /// <summary>
        /// Call during DragMove process for dragged (=active) object.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        protected override void DragThisOverBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            Rectangle bounds = this.Bounds;
            bounds.X = boundsTarget.X;
            int dragY = boundsTarget.Y ;
            int dragYMin = bounds.Y + 5;
            int dragYMax = bounds.Bottom - 5;
            bounds.Y = (dragY < dragYMin ? dragYMin : (dragY > dragYMax ? dragYMax : dragY));

            Rectangle area = this.GColumnSet.BoundsAbsolute;
            Rectangle boundsAbsolute = this.GetAbsoluteBounds(bounds);
            if (boundsAbsolute.Right > area.Right) boundsAbsolute.X = area.Right - boundsAbsolute.Width;
            if (boundsAbsolute.X < area.X) boundsAbsolute.X = area.X;
            bounds = this.GetRelativeBounds(boundsAbsolute);

            // Set BoundsDragTarget to new bounds:
            base.DragThisOverBounds(e, bounds);

            // Search other columns, for change order:
            GColumn prevColumn, nextColumn;
            int prevMark, nextMark;
            this._DragThisSearchHeaders(e, boundsAbsolute, out prevColumn, out prevMark, out nextColumn, out nextMark);
            this._DragThisMarkHeaders(prevColumn, prevMark, nextColumn, nextMark);
        }
        /// <summary>
        /// Is called on DragDrop process for dragged (=active) object.
        /// Base class InteractiveDragObject set specified (boundsTarget) into this.Bounds (with ProcessAction = DragValueActions and EventSourceType = (InteractiveChanged | BoundsChange).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        protected override void DragThisDropToBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            // do not need: base.DragThisDropToBounds(e, boundsTarget);
            if (this._DragToOrder.HasValue)
            {
                this.GColumn.ColumnPosition.Order = this._DragToOrder.Value;
                this.GGrid.RecalculateGrid();
                this.GGrid.Repaint();
            }
        }
        /// <summary>
        /// Call at end DragMove process (drop or cancel) for dragged (=active) object.
        /// </summary>
        /// <param name="e"></param>
        protected override void DragThisOverEnd(GDragActionArgs e)
        {
            base.DragThisOverEnd(e);

            this.GColumnSet.ForEachItem(c => c.DrawInsertMarkReset());
            this._DragToOrder = null;
        }
        /// <summary>
        /// Finds the previous and next columns between which the current column should be inserted.
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <param name="prevColumn"></param>
        /// <param name="nextColumn"></param>
        private void _DragThisSearchHeaders(GDragActionArgs e, Rectangle boundsAbsolute, out GColumn prevColumn, out int prevMark, out GColumn nextColumn, out int nextMark)
        {
            prevColumn = null;
            prevMark = 0;
            nextColumn = null;
            nextMark = 0;

            // Sorted list of visible columns, except RowHeader an except this column:
            List<GColumn> columns = this.GColumnSet
                .Where(c => (!c.IsRowHeaderColumn && c.ColumnID != this.GColumn.ColumnID && c.IsVisible))
                .ToList();
            int count = columns.Count;
            int last = count - 1;
            if (count == 0)
                return;
            if (count > 1)
                columns.Sort((a, b) => a.Bounds.X.CompareTo(b.Bounds.X));

            // Search for column, over which is mouse pointer (X coordinate) currently dragging:
            int mouseX = this.GColumnSet.GetRelativePoint(e.MouseCurrentAbsolutePoint).Value.X;    // Current position of mouse on X axis, relative to this.GColumnSet.Location
            int index = -1;
            bool setDragToOrder = true;
            if (mouseX < columns[0].Bounds.X)
            {   // Mouse is BEFORE first column:
                index = 0;
                nextColumn = columns[0];
                nextMark = 100;
            }
            else if (mouseX >= columns[last].Bounds.Right)
            {   // Mouse is AFTER last column:
                index = last;
                prevColumn = columns[last];
                prevMark = 100;
            }
            else
            {   // Mouse is anywhere in Columns area:
                index = columns.FindIndex(c => (mouseX >= c.Bounds.X && mouseX < c.Bounds.Right)); // Search for column over which is now mouse (can be -1 for current position on current column, which is not contain in (columns) !)
                if (index >= 0)
                {   // Mouse is now over an column, we need detect if target column (=this) will be dropped before it or after it:
                    Rectangle targetBounds = columns[index].Bounds;
                    int targetCenterX = targetBounds.Center().X;
                    if (mouseX < targetCenterX)
                    {   // We drag this column BEFORE target column:
                        nextColumn = columns[index];
                        nextMark = _DragThisGetMark(mouseX - targetBounds.X, targetBounds.Width);
                        prevColumn = (index > 0 ? columns[index - 1] : null);
                        prevMark = (index > 0 ? nextMark : 0);
                    }
                    else
                    {   // We drag this column AFTER target column:
                        prevColumn = columns[index];
                        prevMark = _DragThisGetMark(targetBounds.Right - mouseX, targetBounds.Width);
                        nextColumn = (index < last ? columns[index + 1] : null);
                        nextMark = (index < last ? prevMark : 0);
                    }
                    
                }
                else
                {   // Mouse is over exactly this column, which is now dragged
                    int prevIndex = columns.FindLastIndex(c => (c.Bounds.Right < mouseX));
                    prevColumn = (prevIndex >= 0 ? columns[prevIndex] : null);
                    prevMark = (prevIndex >= 0 ? 100 : 0);
                    int nextIndex = columns.FindIndex(c => (c.Bounds.X >= mouseX));
                    nextColumn = (nextIndex >= 0 ? columns[nextIndex] : null);
                    nextMark = (nextIndex >= 0 ? 100 : 0);
                    this._DragToOrder = null;
                    setDragToOrder = false;                          // No drag need, target is at same position as current position
                }
            }
            if (setDragToOrder)
            {
                if (prevColumn != null && nextColumn != null)
                    this._DragToOrder = (prevColumn.ColumnPosition.Order + nextColumn.ColumnPosition.Order) / 2;
                else if (prevColumn != null)
                    this._DragToOrder = prevColumn.ColumnPosition.Order + 1;
                else if (nextColumn != null)
                    this._DragToOrder = nextColumn.ColumnPosition.Order - 1;
            }
        }
        /// <summary>
        /// Return a Mark value (in range 15 to 100) for ratio distance to width. When distance == 0 return mark = 100, when distance == width/2 returns 15 and proportionally between this.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private int _DragThisGetMark(int distance, int width)
        {
            int half = width / 2;
            if (distance < 0) return 100;
            if (distance >= half) return 15;
            return (int)(Math.Round(15f + 85f * (float)(half - distance)/ (float)half, 0));
        }
        /// <summary>
        /// Mark specified columns as "Drag into after" and "Drag into before".
        /// All other columns mark as "no drag".
        /// Where is change, there will set DrawToLayer...
        /// </summary>
        /// <param name="prevColumn"></param>
        /// <param name="nextColumn"></param>
        private void _DragThisMarkHeaders(GColumn prevColumn, int prevMark, GColumn nextColumn, int nextMark)
        {
            int prevId = (prevColumn != null ? prevColumn.ColumnID : -1);
            int nextId = (nextColumn != null ? nextColumn.ColumnID : -1);
            foreach (GColumn column in this.GColumnSet)
            {
                if (column.IsRowHeaderColumn) continue;
                
                int markBegin = ((column.ColumnID == nextId) ? nextMark : 0);
                if (column.DrawInsertMarkAtBegin != markBegin)
                {
                    column.DrawInsertMarkAtBegin = markBegin;
                    column.RepaintThisToLayers = GInteractiveDrawLayer.Standard;
                }

                int markEnd = ((column.ColumnID == prevId) ? prevMark : 0);
                if (column.DrawInsertMarkAtEnd != markEnd)
                {
                    column.DrawInsertMarkAtEnd = markEnd;
                    column.RepaintThisToLayers = GInteractiveDrawLayer.Standard;
                }
            }
        }
        /// <summary>
        /// Returns adjacent column from specified column, search it in list columns, from index with specified shift from column index.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="columns"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        private static GColumn _DragThisSearchAdjacentHeader(GColumn column, List<GColumn> columns, int shiftBy)
        {
            int index = columns.FindIndex(c => c.ColumnID == column.ColumnID);
            if (index < 0) return null;
            index += shiftBy;
            if (index < 0 || index >= columns.Count) return null;
            return columns[index];
        }
        private Int32? _DragToOrder;
        #endregion
        #region Draw
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (!this.IsRowHeaderColumn)
                e.GraphicsClipWith(this.GColumnSet.GetAbsoluteBounds(this.GColumnSet.BoundsDataColumns), GInteractiveDrawLayer.Standard);

            int? opacity = (e.DrawLayer == GInteractiveDrawLayer.Standard ? (int?)null : (int?)128);
            this.DrawHeader(e, boundsAbsolute, opacity);
        }
        protected override void DrawAsGhost(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                e.Graphics.FillRectangle(Brushes.DarkGray, boundsAbsolute);

            this.DrawHeader(e, boundsAbsolute, 128);
        }
        protected void DrawHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            GPainter.DrawColumnHeader(e.Graphics, boundsAbsolute, Skin.Grid.HeaderBackColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, null, opacity);

            if (this.DataColumn != null)
            {
                FontInfo fontInfo = FontInfo.Caption;
                fontInfo.Bold = this.IsSortColumn;
                Color textColor = Skin.Grid.HeaderTextColor.SetOpacity(opacity);
                Rectangle textArea;
                GPainter.DrawString(e.Graphics, boundsAbsolute, this.DataColumn.Text, textColor, fontInfo, ContentAlignment.MiddleCenter, out textArea);
                Image sortImage = this.SortImage;
                if (sortImage != null)
                {
                    int x = textArea.X - sortImage.Width - 2;
                    int y = textArea.Center().Y - sortImage.Height / 2;
                    Rectangle sortBounds = new Rectangle(x, y, sortImage.Width, sortImage.Height);
                    e.Graphics.DrawImage(sortImage, sortBounds);
                }
            }

            if (this.GColumn.DrawInsertMarkAtBegin > 0)
            {
                int m = (this.GColumn.DrawInsertMarkAtBegin <= 100 ? this.GColumn.DrawInsertMarkAtBegin : 100);
                int w = boundsAbsolute.Width * m / 300;
                Rectangle boundsMark = new Rectangle(boundsAbsolute.X + 1, boundsAbsolute.Y, w, boundsAbsolute.Height);
                GPainter.DrawInsertMark(e.Graphics, boundsMark, Skin.Modifiers.MouseDragTracking, System.Drawing.ContentAlignment.MiddleLeft);
            }

            if (this.GColumn.DrawInsertMarkAtEnd > 0)
            {
                int m = (this.GColumn.DrawInsertMarkAtEnd <= 100 ? this.GColumn.DrawInsertMarkAtEnd : 100);
                int w = boundsAbsolute.Width * m / 300;
                Rectangle boundsMark = new Rectangle(boundsAbsolute.Right - w - 1, boundsAbsolute.Y, w, boundsAbsolute.Height);
                GPainter.DrawInsertMark(e.Graphics, boundsMark, Skin.Modifiers.MouseDragTracking, System.Drawing.ContentAlignment.MiddleRight);
            }
        }
        /// <summary>
        /// true when this column is "Sorting" (Asc or Desc), by this.SortType
        /// </summary>
        protected bool IsSortColumn { get { return this.GColumn.IsSortColumn; } }
        /// <summary>
        /// Image by current SortType
        /// </summary>
        protected Image SortImage
        {
            get
            {
                switch (this.GColumn.SortType)
                {
                    case DTableSortRowType.Ascending: return IconStandard.SortAsc;
                    case DTableSortRowType.Descending: return IconStandard.SortDesc;
                }
                return null;
            }
        }
        /// <summary>
        /// Repaint item to this layers after current operation. Layers are combinable. Layer None is permissible (no repaint).
        /// Setting a value to GColumnHeader.RepaintToLayers set this value to this.GColumn.RepaintColumnToLayers.
        /// </summary>
        protected override GInteractiveDrawLayer RepaintToLayers { get { return base.RepaintToLayers; } set { base.RepaintToLayers = value; this.GColumn.RepaintThisToLayers = value; } }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        #endregion
    }
    /// <summary>
    /// Set of all rows in one GTable
    /// </summary>
    public class GRowSet : InteractiveContainer, IEnumerable<GRow>
    {
        #region Constructor, public properties
        public GRowSet(GTable table, DTable dataTable)
        {
            this._Rows = new Dictionary<int, GRow>();
            this._Table = table;
            this._SizeDefault = 21;
            this.InitScrollBar();
            this.ReloadRows(dataTable);
        }
        /// <summary>
        /// Table position
        /// </summary>
        public GridPositionItem TablePosition { get { return this._Table.TablePosition; } }
        /// <summary>
        /// Grid position (=Columns)
        /// </summary>
        public GridPositions GridPositions { get { return this.GGrid.Positions; } }
        /// <summary>
        /// Owner table of this GRowSet
        /// </summary>
        public GTable GTable { get { return this._Table; } } private GTable _Table;
        /// <summary>
        /// Owner Grid of this table.RowSet
        /// </summary>
        public GGrid GGrid { get { return this._Table.GGrid; } }
        /// <summary>
        /// DataTable
        /// </summary>
        protected DTable DataTable { get { return (this.GTable != null ? this.GTable.DataTable : null); } }
        #endregion
        #region BoundsChange
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            this.SetPositions(newBounds, ref actions, eventSource);
        }
        /// <summary>
        /// Set all positions into this.Columns and ColumnSetSplitter, by current this.Bounds.
        /// </summary>
        internal void RefreshPositions()
        {
            Rectangle bounds = this.Bounds;
            ProcessAction actions = ProcessAction.PrepareInnerItems;
            EventSourceType eventSource = EventSourceType.BoundsChange | EventSourceType.ApplicationCode;
            this.SetPositions(bounds, ref actions, eventSource);
        }
        protected void SetPositions(Rectangle bounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            Size clientSize = this.BoundsClient.Size;
            int scrollWidth = GScrollBar.DefaultSystemBarWidth;

            this.RowAreaBounds = new Rectangle(0, 0, clientSize.Width - scrollWidth, clientSize.Height);
            this.ScrollBarBounds = new Rectangle(clientSize.Width - scrollWidth, 0, scrollWidth, clientSize.Height);

            if (this.RowAreaBounds.Height != this.RowAreaLastBounds.Height)
                // When was change in Height, then will invalidate VisualList (visible rows):
                this.InvalidateVisualList();
            else if (this.RowAreaBounds.Width != this.RowAreaLastBounds.Width)
                // When was change in v, then will invalidate Bounds (=width of row):
                this.InvalidateBounds();
        }
        protected Rectangle RowAreaBounds;
        protected Rectangle ScrollBarBounds;
        #endregion
        #region RowCollection
        /// <summary>
        /// Count of columns in this set
        /// </summary>
        public int Count { get { return this._Rows.Count; } }
        public void Clear()
        {
            this._Rows.Values.ForEachItem(i => i.Dispose());
            this._Rows.Clear();
            this.NextOrder = 0;
            this.InvalidateOrder();
        }
        /// <summary>
        /// Get GRow for RowId
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GRow this[int rowId] { get { return this._Rows[rowId]; } }
        /// <summary>
        /// Add new DataRow to this Graphic RowSet
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="index"></param>
        internal void AddRow(DRow dataRow, int? index)
        {
            this.AddRow(dataRow);
        }
        /// <summary>
        /// Remove one DataRow from this Graphic RowSet
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="index"></param>
        internal void RemoveRow(DRow dataRow, int? index)
        {
            if (this._Rows.ContainsKey(dataRow.RowId))
                this._Rows.Remove(dataRow.RowId);
        }
        /// <summary>
        /// Reload all rows from this.Table.DataTable.
        /// Does not trigger any event.
        /// </summary>
        internal void ReloadRows()
        {
            this.ReloadRows(this.DataTable);
        }
        /// <summary>
        /// Reload all rows from specified dataTable.
        /// Does not trigger any event.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void ReloadRows(DTable dataTable)
        {
            this.ReloadRows(this.DataTable.Rows);
        }
        /// <summary>
        /// Reload all rows from specified rows.
        /// Does not trigger any event.
        /// </summary>
        /// <param name="dataRows"></param>
        protected void ReloadRows(IEnumerable<DRow> dataRows)
        {
            this.Clear();                                  // Call InvalidateOrder()
            if (dataRows != null)
                dataRows.ForEachItem(dRow => this.AddRow(dRow));
        }
        protected void AddRow(DRow dataRow)
        {
            GRow gRow = new GRow(this, dataRow);
            gRow.Order = this.NextOrder;                   // Call InvalidateOrder()
            this.NextOrder += 10;
            GRow oldRow;
            if (this._Rows.TryGetValue(dataRow.RowId, out oldRow))
            {
                oldRow.Dispose();
                this._Rows[dataRow.RowId] = gRow;
            }
            else
                this._Rows.Add(dataRow.RowId, gRow);
        }
        IEnumerator<GRow> IEnumerable<GRow>.GetEnumerator() { return this._Rows.Values.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this._Rows.Values.GetEnumerator(); }
        protected Dictionary<int, GRow> _Rows;
        #endregion
        #region ScrollBar
        /// <summary>
        /// Initialise scroll bar
        /// </summary>
        protected void InitScrollBar()
        {
            this.ScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this.ScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(ScrollBar_ValueChange);
            this.ScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(ScrollBar_ValueChange);
        }

        void ScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.ScrollBar.Value.Begin.Value;
            if (offset == this.VisualOffset) return;
            this.VisualOffset = offset;
            this.InvalidatePositions();
            this.CheckValid();
            this.RepaintToLayers = GInteractiveDrawLayer.Standard; 
        }
        /// <summary>
        /// Vertical Scrollbar for Tables (no its Rows!) shift
        /// </summary>
        protected GScrollBar ScrollBar;
        #endregion
        #region Childs items
        /// <summary>
        /// An array of sub-items in this item.
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckValid(); return this.ChildList; } }
       
        /// <summary>
        /// Reload all current items for this Grid into .
        /// Add this items: this._Tables, 
        /// </summary>
        protected void ChildArrayReload()
        {
            this.ChildList.Clear();

            // Currently Visible Rows:
            this.ChildList.AddRange(this.ItemsVisual);

            // Splitter after each rows:
            this.ChildList.AddRange(this.ItemsVisual.Select(r => r.RowSplitter));
            
            // ScrollBar:
            this.ChildList.Add(this.ScrollBar);
        }
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.WheelUp:
                    this.ProcessPositionAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.ProcessPositionAction(InteractivePositionAction.WheelDown);
                    break;
            }
        }
        internal bool ProcessPositionAction(InteractivePositionAction action)
        {
            bool isProcessed = false;
            switch (action)
            {
                case InteractivePositionAction.WheelDown:
                    isProcessed = this._ProcessPositionActionScroll(4 * this.RowSizeDefault);
                    break;
                case InteractivePositionAction.WheelUp:
                    isProcessed = this._ProcessPositionActionScroll(-4 * this.RowSizeDefault);
                    break;
            }
            return isProcessed;
        }
        private bool _ProcessPositionActionScroll(int scrollPixel)
        {
            int offset = this.VisualOffset + scrollPixel;
            if (offset < 0) offset = 0;
            int height = this.Bounds.Height;
            int bottomVoid = (3 * height / 20);
            if (bottomVoid < 15) bottomVoid = 15;
            int offsetMax = this.TotalSizeVisible - (height - bottomVoid);
            if (offset > offsetMax) offset = offsetMax;

            this.VisualOffset = offset;
            this.RepaintToLayers = GInteractiveDrawLayer.Standard;

            return true;
        }
        /// <summary>
        /// Active row (=with focus)
        /// </summary>
        internal GRow ActiveRow 
        {
            get { return this._ActiveRow; }
            set
            {
                this._ActiveRow = value;
                this.RepaintToLayers = GInteractiveDrawLayer.Standard;
            }
        } private GRow _ActiveRow;
        #endregion
        #region Draw
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            e.Graphics.FillRectangle(Skin.Brush(Skin.Grid.TableBackColor), boundsAbsolute);
        }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        #endregion
        #region Invalidate, CheckValid, IsValid, Positions
        /// <summary>
        /// Default Height for rows in pixel 
        /// </summary>
        public int RowSizeDefault { get { return this._SizeDefault; } set { this._SizeDefault = (value < 10 ? 10 : value); } } private int _SizeDefault;
        /// <summary>
        /// RowSizeRange : range for Rows.Size value.
        /// </summary>
        public Int32NRange RowSizeRange { get { return this._SizeRange; } set { this._SizeRange = value; this.InvalidatePositions(); } } private Int32NRange _SizeRange;
        /// <summary>
        /// Offset for Visual values.
        /// Contain positive value of first visible data pixel.
        /// When contains 0, then first item has visual begin at 0 (or ItemHeaderSize).
        /// </summary>
        public int VisualOffset { get { return this._VisualOffset; } set { this._VisualOffset = (value < 0 ? 0 : value); this.InvalidatePositions(); } } private int _VisualOffset;
        /// <summary>
        /// Sum of all Items.SizeVisible, except Header item.
        /// </summary>
        public int TotalSizeVisible { get { this.CheckValid(); return this._TotalSizeVisible; } } private int _TotalSizeVisible;
        /// <summary>
        /// Last visible pixel of last column
        /// </summary>
        public int MaxVisiblePixel { get { this.CheckValid(); return this._MaxVisiblePixel; } } private int _MaxVisiblePixel;
        /// <summary>
        /// Invalidate all visual values, enforce recalculate before its use.
        /// </summary>
        internal void InvalidateOrder()
        {
            this.IsValidOrder = false;
            this.IsValidPositions = false;
            this.IsValidVisualList = false;
            this.IsValidBounds = false;
            this.IsValidChilds = false;
        }
        /// <summary>
        /// Invalidate all visual values, enforce recalculate before its use.
        /// </summary>
        internal void InvalidatePositions()
        {
            this.IsValidPositions = false;
            this.IsValidVisualList = false;
            this.IsValidBounds = false;
            this.IsValidChilds = false;
        }
        /// <summary>
        /// Invalidate all visual values, enforce recalculate before its use.
        /// </summary>
        internal void InvalidateVisualList()
        {
            this.IsValidVisualList = false;
            this.IsValidBounds = false;
            this.IsValidChilds = false;
        }
        /// <summary>
        /// Invalidate all visual values, enforce recalculate before its use.
        /// </summary>
        internal void InvalidateBounds()
        {
            this.IsValidBounds = false;
            this.IsValidChilds = false;
        }
        /// <summary>
        /// Invalidate all visual values, enforce recalculate before its use.
        /// </summary>
        internal void InvalidateChilds()
        {
           this.IsValidChilds = false;
        }
        /// <summary>
        /// When is not valid, call recalculate visual values.
        /// </summary>
        internal void CheckValid()
        {
            if (this.IsValid) return;
            
            this.CheckValidOrder();
            this.CheckValidPositions();
            this.CheckValidVisualList();
            this.CheckValidBounds();
            this.CheckValidChilds();
        }
        protected void CheckValidOrder()
        {
            if (this.IsValidOrder) return;

            this.ItemsSort = this._Rows.Values.ToList();
            if (this.ItemsSort.Count > 1)
                this.ItemsSort.Sort((a, b) => GRow.CompareByOrder(a, b));

            this.IsValidPositions = false;
            this.IsValidVisualList = false;
            this.IsValidBounds = false;
            this.IsValidChilds = false;

            this.IsValidOrder = true;
        }
        protected void CheckValidPositions()
        {
            if (this.IsValidPositions) return;

            int beginLogical = 0;
            int beginVisual = 0;
            int order = 0;
            beginLogical = 0;
            beginVisual -= this._VisualOffset;
            foreach (GRow gRow in this.ItemsSort)
                gRow.SetPositions(ref order, ref beginLogical, ref beginVisual);
            this._TotalSizeVisible = beginLogical;
            this._MaxVisiblePixel = beginVisual - 1;
            this.NextOrder = order;

            this.IsValidVisualList = false;
            this.IsValidBounds = false;
            this.IsValidChilds = false;

            this.IsValidPositions = true;
        }
        protected void CheckValidVisualList()
        {
            if (this.IsValidVisualList) return;

            int begin = this.RowAreaBounds.Y;
            int end = this.RowAreaBounds.Bottom;
            this.ItemsVisual = this.ItemsSort.FindAll(g => g.IsVisibleIn(begin, end));
            
            this.IsValidBounds = false;
            this.IsValidChilds = false;

            this.IsValidVisualList = true;
        }
        protected void CheckValidBounds()
        {
            if (this.IsValidBounds) return;

            int left = this.RowAreaBounds.X;
            int width = this.RowAreaBounds.Width;
            int rowHeaderWidth = this.GridPositions.RowHeaderWidth;
            this.ItemsVisual.ForEach(g => g.SetRowBounds(left, width, rowHeaderWidth));

            this.ScrollBarRecalculate();

            this.IsValidChilds = false;

            this.IsValidBounds = true;
        }
        protected void ScrollBarRecalculate()
        {
            Rectangle bounds = this.ScrollBarBounds;
            int total = this._TotalSizeVisible + 10;
            int offset = this._VisualOffset;
            int visual = ScrollBarBounds.Height;
            using (this.ScrollBar.SuppressEvents())
            {
                this.ScrollBar.Bounds = bounds;
                this.ScrollBar.ValueTotal = new SizeRange(0, total);
                this.ScrollBar.Value = new SizeRange(offset, offset + visual);
                this.ScrollBar.IsEnabled = (total > visual);
            }
        }
        protected void CheckValidChilds()
        {
            if (this.IsValidChilds) return;

            this.ChildArrayReload();

            this.IsValidChilds = true;
        }
        /// <summary>
        /// Flag for validity of visual values
        /// </summary>
        protected bool IsValid { get { return this.IsValidOrder && this.IsValidPositions && this.IsValidVisualList && this.IsValidBounds && this.IsValidChilds; } }
        protected bool IsValidOrder;
        protected bool IsValidPositions;
        protected bool IsValidVisualList;
        protected bool IsValidBounds;
        protected bool IsValidChilds;
        protected List<GRow> ItemsSort;
        protected List<GRow> ItemsVisual;
        protected Rectangle RowAreaLastBounds;
        protected int NextOrder { get; private set; }
        #endregion
    }
    /// <summary>
    /// One row in GTable
    /// </summary>
    public class oldGRow : InteractiveContainer
    {
        #region Constructor, public properties
        public GRow(GRowSet rowSet, DRow dataRow)
        {
            this.Style = this.Style | GInteractiveStyles.KeyboardInput;
            this._RowSet = rowSet;
            this._DataRow = dataRow;
            this._IsVisible = true;
            if (dataRow is IVisualRow)
            {
                IVisualRow visualRow = dataRow as IVisualRow;
                this.SizeN = visualRow.RowHeight;
            }
            this._RowHeaderInit();
            this._RowSplitterInit();
            this._CellsInit();
        }
        internal void Dispose()
        {
            this._RowSet = null;
            this._DataRow = null;
        }
        /// <summary>
        /// Owner RowSet
        /// </summary>
        internal GRowSet GRowSet { get { return this._RowSet; } } private GRowSet _RowSet;
        /// <summary>
        /// Data for this column
        /// </summary>
        internal DRow DataRow { get { return this._DataRow; } } private DRow _DataRow;
        /// <summary>
        /// Owner table of this GRowSet
        /// </summary>
        public GTable GTable { get { return this._RowSet.GTable; } }
        /// <summary>
        /// Owner Grid of this GTable.GRowSet.GRow
        /// </summary>
        public GGrid GGrid { get { return this.GTable.GGrid; } }
        /// <summary>
        /// Positions of all visual items (Columns and Tables)
        /// </summary>
        internal GridPositions GridPositions { get { return this.GGrid.Positions; } }
        /// <summary>
        /// ID of DataRow (=this.DataRow.RowId)
        /// </summary>
        internal int RowID { get { return this._DataRow.RowId; } }
        #endregion
        #region Bounds
        /// <summary>
        /// Bounds (relative) of this GRow.
        /// Setting a value has not effect.
        /// </summary>
        public override Rectangle Bounds { get { this.CheckRowValid(); return base.Bounds; } set { /* no effect */ } }
        /// <summary>
        /// Bounds (relative) of this GRow.
        /// Setting a value will set base.Bounds value.
        /// </summary>
        protected Rectangle BoundsRow { get { return this.Bounds; } set { base.Bounds = value; } }
        #endregion
        #region Row-Positions (as sequence of ordered positions BeginLogical-EndLogical and BeginVisual-EndVisual)
        /// <summary>
        /// ID of item, a constant value
        /// </summary>
        public int ItemId { get { return this.RowID; } set { } }
        /// <summary>
        /// True for Visible item, false for Invisible
        /// </summary>
        public override bool IsVisible { get { return this._IsVisible; } set { this.InvalidateVisualList(); this._IsVisible = value; } } private bool _IsVisible;
        /// <summary>
        /// Order of item, can be changed
        /// </summary>
        public int Order { get { this.CheckRowValid(); return this._Order; } set { this.InvalidateOrder(); this._Order = (value < 0 ? 0 : value); } } private int _Order;
        /// <summary>
        /// Order of item, can be changed.
        /// This property does not perform CheckValid() nor Invalidate() !!! 
        /// When use this property, you must call CheckRowValid() before reading, or call InvalidateOrder() after setting value!!!
        /// </summary>
        internal int OrderValue { get { return this._Order; } set { this._Order = (value < 0 ? 0 : value); } }
        /// <summary>
        /// Size: Width for Column item, or Height for Table item. Contain positive value even for Invisible item (you can use property SizeVisible, which contain 0 for Invisible item).
        /// </summary>
        public int Size { get { this.CheckRowValid(); return this._SizeValid; } set { this.InvalidatePositions(); this._SizeN = (value < 0 ? 0 : value); } } private int _SizeValid;
        /// <summary>
        /// SizeN: Width for Column item, or Height for Table item. Can contain a null value.
        /// </summary>
        public int? SizeN { get { return this._SizeN; } set { this.InvalidatePositions(); this._SizeN = value; } } private int? _SizeN;
        /// <summary>
        /// SizeRange : range for Size value.
        /// </summary>
        public Int32NRange SizeRange { get { return this.GRowSet.RowSizeRange; } }
        /// <summary>
        /// SizeVisible: Width for Column item, or Height for Table item. Contain zero for Invisible item.
        /// </summary>
        public int SizeVisible { get { return (this.IsVisible ? this.Size : 0); } }
        /// <summary>
        /// Begin in logical values. First item has Begin = 0, second item has Begin = Prev.EndLogical, and so on.
        /// </summary>
        public int BeginLogical { get { this.CheckRowValid(); return this._BeginLogical; } } private int _BeginLogical;
        /// <summary>
        /// End in logical values. First item has End = SizeVisible, second item has End = BeginLogical + SizeVisible, and so on.
        /// </summary>
        public int EndLogical { get { this.CheckRowValid(); return this._EndLogical; } } private int _EndLogical;
        /// <summary>
        /// Begin in visual values. First item has Begin = VisualOffset, second item has Begin = Prev.EndVisual, and so on.
        /// </summary>
        public int BeginVisual { get { this.CheckRowValid(); return this._BeginVisual; } } private int _BeginVisual;
        /// <summary>
        /// End in logical values. First item has End = SizeVisible, second item has End = BeginLogical + SizeVisible, and so on.
        /// </summary>
        public int EndVisual { get { this.CheckRowValid(); return this._EndVisual; } set { this._EndVisualSet(value); } } private int _EndVisual;
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
        #region Invalidate, CheckValid, SetPositions methods
        /// <summary>
        /// Invalidate Order
        /// </summary>
        protected void InvalidateOrder() { if (this.GRowSet != null) this.GRowSet.InvalidateOrder(); }
        /// <summary>
        /// Invalidate Positions
        /// </summary>
        protected void InvalidatePositions() { if (this.GRowSet != null) this.GRowSet.InvalidatePositions(); }
        /// <summary>
        /// Invalidate VisualList
        /// </summary>
        protected void InvalidateVisualList() { if (this.GRowSet != null) this.GRowSet.InvalidateVisualList(); }
        /// <summary>
        /// Invalidate Bounds
        /// </summary>
        protected void InvalidateBounds() { if (this.GRowSet != null) this.GRowSet.InvalidateBounds(); }
        /// <summary>
        /// Invalidate Childs
        /// </summary>
        protected void InvalidateChilds() { if (this.GRowSet != null) this.GRowSet.InvalidateChilds(); }
        /// <summary>
        /// Ensure valid for this.Type items
        /// </summary>
        protected void CheckRowValid() { if (this.GRowSet != null) this.GRowSet.CheckValid(); }
        /// <summary>
        /// Update this Order, Begin and End Logical and Visual by parameters.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="beginLogical"></param>
        /// <param name="beginVisual"></param>
        internal void SetPositions(ref int order, ref int beginLogical, ref int beginVisual)
        {
            this._Order = order;
            order += 10;

            // Validation of Size:
            int? sizeN = this._SizeN;
            if (this.SizeRange != null)
                sizeN = this.SizeRange.Align(sizeN);
            int size = (sizeN.HasValue ? sizeN.Value : SizeDefault);
            this._SizeValid = size;

            // Size visible:
            int sizeVisible = (this._IsVisible ? size : 0);

            // Position Logical:
            this._BeginLogical = beginLogical;
            beginLogical += sizeVisible;
            this._EndLogical = beginLogical;

            // Position Visual:
            this._BeginVisual = beginVisual;
            beginVisual += sizeVisible;
            this._EndVisual = beginVisual;
        }
        internal bool IsVisibleIn(int begin, int end)
        {
            return (this._BeginVisual < end && this._EndVisual > begin);
        }
        internal void SetRowBounds(int left, int width, int rowHeaderWidth)
        {
            // Bounds:
            int height = (this._IsVisible ? this._SizeValid : 0);
            int right = left + width;
            if (right > this.GridPositions.ColumnsMaxVisiblePixel)
            {
                right = this.GridPositions.ColumnsMaxVisiblePixel;
                width = right - left;
            }
            this.BoundsRow = new Rectangle(left, this._BeginVisual, width, height);

            // Row splitter:
            this._RowSplitterSetBounds(rowHeaderWidth);
        }
        /// <summary>
        /// Default size for this item
        /// </summary>
        protected int SizeDefault { get { return this.GRowSet.RowSizeDefault; } }
        #endregion
        /// <summary>
        /// Compare two items by IGridPositionItem.SortValue1 ASC, IGridPositionItem.SortValue2 ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByOrder(GRow a, GRow b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            int cmp = a._Order.CompareTo(b._Order);
            if (cmp == 0)
                cmp = a.RowID.CompareTo(b.RowID);
            return cmp;
        }
        #endregion
        #region Childs items
        /// <summary>
        /// An array of sub-items in this item.
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckChildValid(); return this.ChildList; } }
        /// <summary>
        /// Reload all current items for this Grid into .
        /// Add this items: this._Tables, 
        /// </summary>
        protected void _CheckChildValidItems()
        {
            if (this._ChildArrayValid) return;

            this.ChildList.Clear();

            this.ChildList.AddRange(this._Cells);

            this.ChildList.Add(this._RowHeader);
            // this.ChildList.Add(this._RowSplitter);
            
            this._ChildArrayValid = true;
        }
        private bool _ChildArrayValid;
        #endregion
        #region RowHeader
        private void _RowHeaderInit()
        {
            this._RowHeader = new GRowHeader(this);

        }
        private GRowHeader _RowHeader;
        #endregion
        #region RowSplitter
        /// <summary>
        /// Splitter after this row, control Height for this row
        /// </summary>
        public GSplitter RowSplitter { get { return this._RowSplitter; } }
        /// <summary>
        /// Prepare new _RowSplitter.
        /// </summary>
        protected void _RowSplitterInit()
        {
            this._RowSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._RowSplitter.ValueSilent = this.Bounds.Bottom;
            this._RowSplitter.ValueChanging += new GPropertyChanged<int>(_RowSplitter_LocationChange);
            this._RowSplitter.ValueChanged += new GPropertyChanged<int>(_RowSplitter_LocationChange);
        }
        /// <summary>
        /// Set values to this _RowSplitter
        /// </summary>
        /// <param name="rowHeaderWidth"></param>
        private void _RowSplitterSetBounds(int rowHeaderWidth)
        {
            using (this._RowSplitter.SuppressEvents())
            {
                this._RowSplitter.BoundsNonActive = new Int32NRange(1, rowHeaderWidth + 3);
                this._RowSplitter.Value = this._EndVisual;
            }
        }
        /// <summary>
        /// Eventhandler for _ColumnSplitter.LocationChange event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int location = this._RowSplitter.Value;
            this.EndVisual = location;
            this.InvalidatePositions();
            this.RepaintToLayers = GInteractiveDrawLayer.Standard;
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _RowSplitter;
        #endregion
        #region Cells
        private void _CellsInit()
        {
            this._Cells = null;
        }
        private GCell[] _Cells;
        #endregion
        #region Positions (RowHeader and Cells.Bounds by ColumnPositions and this.Height): Invalidate(), CheckValid() and RefreshPositions
        private void CheckChildValid()
        {
            this._CheckChildValidPositions();
            this._CheckChildValidItems();
        }
        internal void InvalidateChildPositions()
        {
            this._PositionKeyChecked = null;
        }
        private void _CheckChildValidPositions()
        {
            Rectangle boundsClient = this.BoundsClient;

            // Equality of key (Visibe Height, Cells count and 
            string positionKey = this._PositionKeyCurrent;
            if (String.Equals(this._PositionKeyChecked, positionKey)) return;

            // Set currently valid Bounds into _RowHeader and to Cells:
            GridPositions gp = this.GridPositions;
            this._RowHeader.Bounds = new Rectangle(0, 0, gp.RowHeaderWidth, boundsClient.Height);

            GCell[] cellsOld = this._Cells;
            GCell[] cellsNew = this._Cells;
            int countOld = (cellsOld == null ? -1 : cellsOld.Length);
            int countNew = this._DataRow.ItemsCount;
            if (countOld != countNew)
                cellsNew = new GCell[countNew];

            for (int id = 0; id < countNew; id++)
            {
                GCell cell = (id < countOld ? cellsOld[id] : null);
                if (cell == null)
                {
                    cell = new GCell(this, id);
                }

                GridPositionColumnItem position = gp.GetColumn(id);
                cell.Bounds = new Rectangle(position.BeginVisual, 0, position.SizeVisible, boundsClient.Height);

                cellsNew[id] = cell;
            }
            this._Cells = cellsNew;

            this._ChildArrayValid = false;
            this._PositionKeyChecked = positionKey;
        }
        /// <summary>
        /// Visual identity of previous version of GRow, when was recalculated all child Bounds.
        /// </summary>
        private string _PositionKeyChecked;
        /// <summary>
        /// Visual identity of current GRow.
        /// Descripts : BoundsClient, _Cells.Length and DataRow.ItemsCount, and ColumnsIdentityKey of GridPosition.
        /// When any from this values is changed, then _PositionKeyCurrent is changed.
        /// Use as "hash" for this.Childs[].Bounds validity.
        /// </summary>
        private string _PositionKeyCurrent
        {
            get
            {
                Rectangle boundsClient = this.BoundsClient;
                string positionKey = 
                    boundsClient.Width.ToString() + "," + boundsClient.Height.ToString() + ";" + 
                    (this._Cells == null ? -1 : this._Cells.Length).ToString() + ";" + this.DataRow.ItemsCount.ToString() + ";" + 
                    this.GridPositions.ColumnsIdentityKey;
                return positionKey;
            }
        }
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardPreviewKeyDown:
                    // Sem chodí i klávesy Kurzor, Tab
                    this.KeyboardPreviewKeyDown(e);
                    break;
                case GInteractiveChangeState.KeyboardKeyPress:
                    break;
                case GInteractiveChangeState.KeyboardKeyDown:
                    // Sem chodí PageUp, PageDown a písmena
                    break;

                case GInteractiveChangeState.WheelUp:
                    this.GRowSet.ProcessPositionAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.GRowSet.ProcessPositionAction(InteractivePositionAction.WheelDown);
                    break;

            }
        }
        private void KeyboardPreviewKeyDown(GInteractiveChangeStateArgs e)
        {
            InteractivePositionAction action = e.KeyboardPreviewArgs.GetInteractiveAction();
            if (action == InteractivePositionAction.None) return;              // ignore key, when is not Action

            bool isProcessed = this.GRowSet.ProcessPositionAction(action);

            e.KeyboardPreviewArgs.IsInputKey = isProcessed;
            /*
            var code = e.KeyboardPreviewArgs.KeyCode;
            var data = e.KeyboardPreviewArgs.KeyData;
            int x = e.KeyboardPreviewArgs.KeyValue;
            
            e.ToolTipData.TitleText = "KeyboardPreviewKeyDown";
            e.ToolTipData.InfoText = "KeyCode: " + code.ToString() + "; KeyData: " + data.ToString() + "; Action = " + action.ToString();
            */
        }
        protected override void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPress(e);

        }
        /// <summary>
        /// After Click on RowHeader object
        /// </summary>
        internal void RowHeaderClick()
        {
            this._RowIsSelected = !this._RowIsSelected;
            this.RepaintThisToLayers = GInteractiveDrawLayer.Standard;
        }
        #endregion
        #region Draw, support for draw for GCell
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Rectangle bounds = boundsAbsolute; //.Enlarge(-1, -1, -1, -1);

            this.DrawBackground(e, bounds);

            Rectangle boundsClient = this.GetAbsoluteClientArea(); // .BoundsClient;
            boundsClient.Width = boundsClient.Width - 2;
            this.DrawBorders(e, boundsClient, true, false);
        }
        /// <summary>
        /// Draw background (=fill background) for this row, in specified absolute bounds
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        internal void DrawBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Color backColor = this.BackColorCurent;
            this.Host.FillRectangle(e.Graphics, boundsAbsolute, this.BackColorCurent);
            // e.Graphics.FillRectangle(Brushes.White, boundsAbsolute);
            // GPainter.DrawAreaBase(e.Graphics, boundsAbsolute, backColor, System.Windows.Forms.Orientation.Horizontal, null, null);
        }
        internal void DrawBorders(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawHorizontal, bool drawVertical)
        {
            if (drawVertical)
            {
                e.Graphics.DrawLine(Pens.DarkGray, boundsAbsolute.Right - 1, boundsAbsolute.Y, boundsAbsolute.Right - 1, boundsAbsolute.Bottom - 1);   // Right line
            }
            if (drawHorizontal)
            {
                Pen pen = (this.HasFocus ? Pens.LightGoldenrodYellow : Pens.DarkBlue);
                e.Graphics.DrawLine(pen, boundsAbsolute.X, boundsAbsolute.Bottom - 1, boundsAbsolute.Right - 1, boundsAbsolute.Bottom - 1);  // Bottom line
                if (this.HasFocus)
                    e.Graphics.DrawLine(pen, boundsAbsolute.X, boundsAbsolute.Y + 1, boundsAbsolute.Right - 1, boundsAbsolute.Y + 1);        // Top line
            }
        }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        /// <summary>
        /// Row has focus
        /// </summary>
        internal bool RowHasFocus { get { return this.HasFocus; } }
        internal bool RowIsSelected { get { return this._RowIsSelected; } } private bool _RowIsSelected;
        /// <summary>
        /// BackColor for this Row, by its State (Focus, Selected, etc)
        /// </summary>
        internal Color ForeColorCurent
        {
            get
            {
                if (this.RowHasFocus) return Skin.Grid.FocusRowTextColor;
                if (this.RowIsSelected) return Skin.Grid.SelectedRowTextColor;
                return Skin.Grid.RowTextColor;
            }
        }
        /// <summary>
        /// BackColor for this Row, by its State (Focus, Selected, etc)
        /// </summary>
        internal Color BackColorCurent
        {
            get
            {
                if (this.RowHasFocus) return Skin.Grid.FocusRowBackColor;
                if (this.RowIsSelected) return Skin.Grid.SelectedRowBackColor;
                return Skin.Grid.RowBackColor;
            }
        }
        #endregion
    }
    public class xRowHeader : InteractiveDragObject, IInteractiveItem
    {
        #region Constructor and owner properties
        public GRowHeader(GRow gRow)
        {
            this._GRow = gRow;
        }
        /// <summary>
        /// Owner Row
        /// </summary>
        internal GRow GRow { get { return this._GRow; } } private GRow _GRow;
        /// <summary>
        /// Owner RowSet
        /// </summary>
        internal GRowSet GRowSet { get { return this.GRow.GRowSet; } }
        /// <summary>
        /// Data for this column
        /// </summary>
        internal DRow DataRow { get { return this.GRow.DataRow; } }
        /// <summary>
        /// Owner table of this GRowSet
        /// </summary>
        public GTable GTable { get { return this.GRowSet.GTable; } }
        /// <summary>
        /// Owner Grid of this GTable.GRowSet.GRow
        /// </summary>
        public GGrid GGrid { get { return this.GTable.GGrid; } }
        /// <summary>
        /// Positions of all visual items (Columns and Tables)
        /// </summary>
        internal GridPositions GridPositions { get { return this.GGrid.Positions; } }
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.WheelUp:
                    this.GRowSet.ProcessPositionAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.GRowSet.ProcessPositionAction(InteractivePositionAction.WheelDown);
                    break;
                case GInteractiveChangeState.LeftClick:
                    this.GRow.RowHeaderClick();
                    break;
            }
        }
        #endregion
        #region Drag
        protected override bool CanDrag { get { return false; /* ToDo ... */ } }
        #endregion
        #region Draw
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            int? opacity = (e.DrawLayer == GInteractiveDrawLayer.Standard ? (int?)null : (int?)128);
            this.DrawHeader(e, boundsAbsolute, opacity);
        }
        protected override void DrawAsGhost(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                e.Graphics.FillRectangle(Brushes.DarkGray, boundsAbsolute);

            this.DrawHeader(e, boundsAbsolute, 128);
        }
        protected void DrawHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            GPainter.DrawColumnHeader(e.Graphics, boundsAbsolute, ColorPalette.ButtonBackEnableColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, null, opacity);

            if (this.GRow.RowIsSelected)
                this.DrawSelected(e, boundsAbsolute);
        }
        protected void DrawSelected(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Rectangle bounds = boundsAbsolute.Enlarge(-1, -1, -1, -1);
            Image image = IconStandard.RowSelected;
            bounds = image.Size.AlignTo(bounds, ContentAlignment.MiddleCenter, true);
            e.Graphics.DrawImage(image, bounds);
        }
        /// <summary>
        /// Repaint item to this layers after current operation. Layers are combinable. Layer None is permissible (no repaint).
        /// Setting a value to GColumnHeader.RepaintToLayers set this value to this.GRow.RepaintColumnToLayers.
        /// </summary>
        protected override GInteractiveDrawLayer RepaintToLayers { get { return base.RepaintToLayers; } set { base.RepaintToLayers = value; this.GRow.RepaintThisToLayers = (value & GInteractiveDrawLayer.Standard); } }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        #endregion
    }
    public class xCell : InteractiveContainer
    {
        #region Constructor and owner properties
        public GCell(GRow gRow, int columnId)
        {
            this._GRow = gRow;
            this._ColumnId = columnId;
        }
        /// <summary>
        /// Owner Row
        /// </summary>
        internal GRow GRow { get { return this._GRow; } } private GRow _GRow;
        /// <summary>
        /// ColumnId
        /// </summary>
        internal int ColumnId { get { return this._ColumnId; } } private int _ColumnId;
        /// <summary>
        /// Owner RowSet
        /// </summary>
        internal GRowSet GRowSet { get { return this.GRow.GRowSet; } }
        /// <summary>
        /// Data for this column
        /// </summary>
        internal DRow DataRow { get { return this.GRow.DataRow; } }
        /// <summary>
        /// Owner table of this GRowSet
        /// </summary>
        public GTable GTable { get { return this.GRowSet.GTable; } }
        /// <summary>
        /// Owner Grid of this GTable.GRowSet.GRow
        /// </summary>
        public GGrid GGrid { get { return this.GTable.GGrid; } }
        /// <summary>
        /// Positions of all visual items (Columns and Tables)
        /// </summary>
        internal GridPositions GridPositions { get { return this.GGrid.Positions; } }
        #endregion
        #region Interactivity
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.WheelUp:
                    this.GRowSet.ProcessPositionAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.GRowSet.ProcessPositionAction(InteractivePositionAction.WheelDown);
                    break;
            }
        }
        #endregion
        #region Draw
        protected override void Draw(GInteractiveDrawArgs e)
        {
            Rectangle boundsAbsolute = this.BoundsAbsolute;

            // Background:
            this.GRow.DrawBackground(e, boundsAbsolute);

            // Content:
            this.DrawContent(e, boundsAbsolute);

            // Borders:
            this.GRow.DrawBorders(e, boundsAbsolute, true, true);
        }
        /// <summary>
        /// Draw content of this Cell (as string or as Graph)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        private void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            object value = this.DataRow.Items[this.ColumnId];
            if (value is ITimeInteractiveGraph)
                this.DrawContentInteractiveTimeGraph(e, boundsAbsolute, value as ITimeInteractiveGraph);
            else if (value is ITimeGraph)
                this.DrawContentTimeGraph(e, boundsAbsolute, value as ITimeGraph);
            else if (value is Image)
                this.DrawContentImage(e, boundsAbsolute, value as Image);
            else
                this.DrawContentText(e, boundsAbsolute, value);
        }
        private void DrawContentInteractiveTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, ITimeInteractiveGraph graph)
        {
            if (graph.Convertor == null)
                graph.Convertor = this.GTable.GetTimeConvertor(this.ColumnId);
            if (graph.Parent == null)
                graph.Parent = this;
            if (graph.Bounds != this.BoundsClient)
                graph.Bounds = this.BoundsClient;

            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Draw value as GTimeGraph
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="graph"></param>
        private void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, ITimeGraph graph)
        {
            if (graph.Convertor == null)
                graph.Convertor = this.GTable.GetTimeConvertor(this.ColumnId);
           
            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Draw value as Image
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="image"></param>
        private void DrawContentImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Image image)
        {
            if (image == null) return;
            Size size = image.Size;
            Rectangle imageBounds = size.AlignTo(boundsAbsolute, ContentAlignment.MiddleCenter, true, true);
            if (imageBounds.Width > 4 && imageBounds.Height > 4)
                e.Graphics.DrawImage(image, imageBounds);
        }
        /// <summary>
        /// Draw value as text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="value"></param>
        private void DrawContentText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, object value)
        {
            ContentAlignment alignment = ContentAlignment.MiddleLeft;
            string text = GetText(value, null, ref alignment);
            GPainter.DrawString(e.Graphics, boundsAbsolute, text, Color.Black, FontInfo.Default, alignment);
        }
        /// <summary>
        /// Evaluate specified object to string, detect alignment by type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="formatString"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        private static string GetText(object value, string formatString, ref ContentAlignment alignment)
        {
            if (value == null) return "";
            if (value is DateTime)
            {
                DateTime valueDT = (DateTime)value;
                alignment = ContentAlignment.MiddleCenter;
                return valueDT.ToString();
            }
            if (value is Int32)
            {
                Int32 valueInt32 = (Int32)value;
                alignment = ContentAlignment.MiddleRight;
                return valueInt32.ToString();
            }
            if (value is Decimal)
            {
                Decimal valueDecimal = (Decimal)value;
                alignment = ContentAlignment.MiddleRight;
                return valueDecimal.ToString();
            }
            alignment = ContentAlignment.MiddleLeft;
            return value.ToString();
        }
        /// <summary>
        /// internal access to this.RepaintToLayers value.
        /// Set this value to Standard layer after interactive (!) change of any of visual properties.
        /// </summary>
        internal GInteractiveDrawLayer RepaintThisToLayers { get { return this.RepaintToLayers; } set { this.RepaintToLayers = value; } }
        #endregion
    }



}
