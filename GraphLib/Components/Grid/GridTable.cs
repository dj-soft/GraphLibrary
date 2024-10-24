﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using WinForm = System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.Components.Grids
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
    public class GTable : InteractiveContainer, IInteractiveItem, IGTable, IGridMember, ISequenceLayout, IDisposable
    {
        #region Inicializace, reference na GGrid, IGridMember
        internal GTable(GGrid grid, Table table)
        {
            this.Parent = grid;
            this._ValidityLock = new object();
            this._Grid = grid;
            this._DataTable = table;
            IGTableMember igtm = table as IGTableMember;
            if (igtm != null) igtm.GTable = this;
            this.Init();
        }
        private void Init()
        {
            this.InitInteractive();
            this.InitTableSequence();
            this.InitRowsPositions();
            this.InitHeaderSplitter();
            this.InitTagFilter();
            this.InitTableSplitter();
        }
        /// <summary>
        /// Inicializace interaktivity
        /// </summary>
        protected void InitInteractive()
        {
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseProperties | InteractiveProperties.Bit.KeyboardInput);
            // Přesměruji údaj Is.GetVisible tohoto grafického objektu na odpovídající hodnotu datového řádku:
            this.Is.GetVisible = this._GetVisible;
            this.Is.SetVisible = this._SetVisible;
        }
        void IDisposable.Dispose()
        {
            IGTableMember igtm = this._DataTable as IGTableMember;
            if (igtm != null) igtm.GTable = null;

            this._Grid = null;
            this._TableId = -1;
            this._SetTableOrder();
            this._DataTable = null;
        }
        /// <summary>
        /// Metoda vrací viditelnost tabulky, hodnotu načítá z datového objektu z <see cref="Table.Visible"/>
        /// </summary>
        /// <param name="isVisible"></param>
        /// <returns></returns>
        private bool _GetVisible(bool isVisible) { return this._DataTable.Visible; }
        /// <summary>
        /// Metoda nastavuje viditelnost řádku, hodnotu vepisuje do datového objektu do <see cref="Table.Visible"/>
        /// </summary>
        /// <param name="isVisible"></param>
        private void _SetVisible(bool isVisible) { this._DataTable.Visible = isVisible; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
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
            Table table = this._DataTable;
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

            this.InvalidateData(items);
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
            int x2t = x3 - ScrollBar.DefaultSystemBarWidth;         // x2t: zde začíná RowsScrollBar (vpravo, hned za koncem prostoru pro řádky), tedy pokud by byl zobrazen
            int x2r = (this._RowsScrollBarVisible ? x2t : x3);       // x2r: zde reálně končí oblast prostoru pro řádky, se zohledněním aktuální viditelnosti RowsScrollBaru
            int y0 = 0;                                              // y0: úplně nahoře
            int y1 = this.ColumnHeaderHeight;                        // y1: pod ColumnHeader, zde začíná TagFilter (pokud existuje)
            int y2 = y1 + this.TagFilterHeight;                      // y2: zde začíná prostor pro řádky, hned pod prostorem TagFilter (hodnota se fyzicky načte z this.ColumnHeaderHeight)
            int y3 = clientSize.Height;                              // y3: úplně dole

            this._TableHeaderBounds = new Rectangle(x0, y0, x1 - x0, y1 - y0);
            this._ColumnHeadersBounds = new Rectangle(x1, y0, x3 - x1, y1 - y0);
            this._TagHeaderLBounds = new Rectangle(x0, y1, x1 - x0, y2 - y1);
            this._TagFilterBounds = new Rectangle(x1, y1, x2r - x1, y2 - y1);
            this._TagHeaderRBounds = new Rectangle(x2r, y1, x3 - x2r, y2 - y1);
            this._RowBounds = new Rectangle(x0, y2, x2r - x0, y3 - y2);
            this._RowHeadersBounds = new Rectangle(x0, y2, x1 - x0, y3 - y2);
            this._RowDataBounds = new Rectangle(x1, y2, x2r - x1, y3 - y2);
            this._RowsScrollBarBounds = new Rectangle(x2t, y2, x3 - x2t, y3 - y2);
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
        protected Rectangle ColumnHeadersBounds { get { this._InnerBoundsCheck(); return this._ColumnHeadersBounds; } } private Rectangle _ColumnHeadersBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru záhlaví PŘED filtrem TagFilter (vizuálně je vlevo, nad záhlavím řádků, i se podobně kreslí)
        /// </summary>
        protected Rectangle TagHeaderLBounds { get { this._InnerBoundsCheck(); return this._TagHeaderLBounds; } } private Rectangle _TagHeaderLBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru filtru TagFilter
        /// </summary>
        protected Rectangle TagFilterBounds { get { this._InnerBoundsCheck(); return this._TagFilterBounds; } } private Rectangle _TagFilterBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru záhlaví ZA filtrem TagFilter (vizuálně je vpravo, nad záhlavím řádků, i se podobně kreslí)
        /// </summary>
        protected Rectangle TagHeaderRBounds { get { this._InnerBoundsCheck(); return this._TagHeaderRBounds; } } private Rectangle _TagHeaderRBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru řádků (RowHeader + RowData)
        /// </summary>
        protected Rectangle RowBounds { get { this._InnerBoundsCheck(); return this._RowBounds; } } private Rectangle _RowBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru záhlaví řádků (RowHeader)
        /// </summary>
        protected Rectangle RowHeadersBounds { get { this._InnerBoundsCheck(); return this._RowHeadersBounds; } } private Rectangle _RowHeadersBounds;
        /// <summary>
        /// Vizuální souřadnice prostoru řádků (RowArea) = vlastní obsah dat, nikoli záhlaví
        /// </summary>
        protected Rectangle RowAreaBounds { get { this._InnerBoundsCheck(); return this._RowDataBounds; } } private Rectangle _RowDataBounds;
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
                case TableAreaType.ColumnHeaders: return this.ColumnHeadersBounds;
                case TableAreaType.TagFilterHeaderLeft: return this.TagHeaderLBounds;
                case TableAreaType.TagFilter: return this.TagFilterBounds;
                case TableAreaType.TagFilterHeaderRight: return this.TagHeaderRBounds;
                case TableAreaType.Row: return this.RowBounds;
                case TableAreaType.RowHeaders: return this.RowHeadersBounds;
                case TableAreaType.RowData: return this.RowAreaBounds;
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
        public Table DataTable { get { return this._DataTable; } }
        private Table _DataTable;
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
        /// Výška oblasti ColumnHeader. 
        /// Při nasetování hodnoty dojde k její kontrole a případně úpravě tak, aby uložená hodnota odpovídala pravidlům.
        /// To znamená, že po vložení hodnoty X může být okamžitě čtena hodnota ColumnHeaderHeight jiná, než byla vložena.
        /// </summary>
        public Int32 ColumnHeaderHeight
        {
            get { return (this._DataTable != null ? this._DataTable.ColumnHeaderHeight : 0); }
            set
            {
                if (this._DataTable != null)
                    this._DataTable.ColumnHeaderHeight = value;
            }
        }
        /// <summary>
        /// Ověří a zajistí připravenost pole Columns.
        /// Toto pole obsahuje správné souřadnice (ISequenceLayout), proto po změně šířky sloupce nebo po změně Order je třeba toto pole invalidovat.
        /// </summary>
        private void _ColumnsCheck()
        {
            if (this._Columns != null) return;
            List<Column> columnsList = this.DataTable.Columns.Where(c => c.IsVisible).ToList();    // Vybrat viditelné sloupce
            columnsList.Sort(Column.CompareOrder);                                                 // Setřídit podle pořadí
            SequenceLayout.SequenceLayoutCalculate(columnsList.Select(c => c.ColumnHeader));       // Napočítat jejich ISequenceLayout.Begin a .End
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
                SequenceLayout.SequenceLayoutCalculate(columns.Select(c => c.ColumnHeader));       // Napočítat jejich ISequenceLayout.Begin a .End
                this._ColumnListWidthValid = true;                                                 // ISequenceLayout jsou platné
            }

            List<Column> visibleColumns = new List<Column>();
            GridPosition columnsPositions = this.Grid.ColumnsPositions;
            Int32Range dataVisibleRange = columnsPositions.DataVisibleRange;                       // Rozmezí datových pixelů, které jsou viditelné
            foreach (Column column in columns)
            {
                GColumnHeader header = column.ColumnHeader;
                ISequenceLayout isl = header as ISequenceLayout;
                bool isColumnVisible = SequenceLayout.IsItemVisible(isl, dataVisibleRange);        // Tento sloupec je vidět?
                header.VisualRange = (isColumnVisible ? columnsPositions.GetVisualPosition(isl) : null);
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
        #region Sloupce tabulky - uživatelem řízená viditelnost sloupců, kontextové menu
        /// <summary>
        /// Provede se poté, kdy uživatel klikne pravou (=kontextovou) myší na záhlaví sloupce.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="column"></param>
        protected void ColumnContextMenu(GInteractiveChangeStateArgs e, Column column)
        {
            WinForm.ToolStripDropDownMenu menu = this.ColumnContextMenuCreate(column);
            menu.AutoClose = false;
            menu.ItemClicked += ColumnContextMenuClicked;
            menu.MouseLeave += ColumnContextMenuMouseLeave;
            Point point = (e.MouseAbsolutePoint.HasValue ? e.MouseAbsolutePoint.Value : WinForm.Control.MousePosition).Add(-20, -5);
            menu.Show(this.Host, point, WinForm.ToolStripDropDownDirection.BelowRight);
        }
        /// <summary>
        /// Vygeneruje Popup menu s nabídkou sloupců pro řízení viditelnosti
        /// </summary>
        /// <param name="currentColumn"></param>
        /// <returns></returns>
        private WinForm.ToolStripDropDownMenu ColumnContextMenuCreate(Column currentColumn)
        {
            WinForm.ToolStripDropDownMenu menu = Painter.CreateDropDownMenu(showImageMargin: false, showCheckMargin: true, title: "Zobrazit sloupce");

            menu.Items.Add(Painter.CreateDropDownItem("Všechny sloupce", tag: "A"));
            menu.Items.Add(Painter.CreateDropDownSeparator());

            foreach (GridColumn gridColumn in this.Grid.AllColumns)
            {
                if (!gridColumn.MasterColumn.CanBeVisible) continue;           // To jsou sloupce typu Primární klíč nebo Číslo vztaženého záznamu atd
                if (gridColumn.UseTimeAxis) continue;                          // Časovou osu nedovolíme skrýt
                Column masterColumn = gridColumn.MasterColumn;
                bool isCurrent = (currentColumn != null && currentColumn.ColumnId == masterColumn.ColumnId);
                System.Drawing.FontStyle? fontStyle = (isCurrent ? (System.Drawing.FontStyle?)System.Drawing.FontStyle.Bold : (System.Drawing.FontStyle?)null);
                WinForm.ToolStripMenuItem item = Painter.CreateDropDownItem(masterColumn.Title, toolTip: masterColumn.ToolTip, isCheckable: true, isChecked: gridColumn.IsVisible, fontStyle: fontStyle, tag: gridColumn);
                item.CheckedChanged += ColumnContextMenuItemCheckedChanged;
                menu.Items.Add(item);
            }
            menu.Items.Add(Painter.CreateDropDownSeparator());
            menu.Items.Add(Painter.CreateDropDownItem("Zavřít", tag: "C"));

            return menu;
        }
        /// <summary>
        /// Obsluha kliknutí na položku menu s nabídkou sloupců pro řízení viditelnosti
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnContextMenuClicked(object sender, WinForm.ToolStripItemClickedEventArgs e)
        {
            WinForm.ToolStripDropDownMenu menu = sender as WinForm.ToolStripDropDownMenu;
            WinForm.ToolStripMenuItem item = e.ClickedItem as WinForm.ToolStripMenuItem;
            object tag = item?.Tag;
            string code = (tag is string ? (string)tag : "");
            switch (code)
            {
                case "A":         // A = All = Zobrazit všechny sloupce
                    this.ColumnContextMenuShowAll(menu);
                    break;
                case "C":         // C = Close = Zavřít menu
                    if (menu != null)
                        menu.Close();
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí zobrazení všech sloupců, a to prostřednictvím kontextového menu
        /// </summary>
        /// <param name="menu"></param>
        private void ColumnContextMenuShowAll(WinForm.ToolStripDropDownMenu menu)
        {
            foreach (var i in menu.Items)
            {   // Tady jsou prvky menu: Label (=titulek), Separator, a různě použité ToolStripMenuItem...
                if (i is WinForm.ToolStripMenuItem)
                {
                    WinForm.ToolStripMenuItem item = i as WinForm.ToolStripMenuItem;
                    object tag = item?.Tag;
                    if (tag is GridColumn && !item.Checked)
                    {
                        item.Checked = true;         // Vyvolá se eventhandler ColumnContextMenuItemCheckedChanged() a ten zajistí potřebné...
                    }
                }
            }
        }
        /// <summary>
        /// Obsluha změny <see cref="WinForm.ToolStripMenuItem.Checked"/> na položce menu s nabídkou sloupců pro řízení viditelnosti
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnContextMenuItemCheckedChanged(object sender, EventArgs e)
        {
            WinForm.ToolStripMenuItem item = sender as WinForm.ToolStripMenuItem;
            object tag = item?.Tag;
            if (tag is GridColumn)
            {
                GridColumn gridColumn = tag as GridColumn;
                this.Grid.ColumnSetVisible(gridColumn.ColumnId, item.Checked);
                this.Grid.Refresh();
            }
        }
        /// <summary>
        /// Jakmile myš opustí menu, tak menu zhasne
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnContextMenuMouseLeave(object sender, EventArgs e)
        {
            WinForm.ToolStripDropDownMenu menu = sender as WinForm.ToolStripDropDownMenu;
            if (menu != null)
                menu.Close();
        }
        #endregion
        #region Řádky tabulky - dvě oddělená pole řádků: a) všechny aktuálně dostupné řádky - pro práci s kolekcí řádků, b) pouze viditelné řádky - pro kreslení
        /// <summary>
        /// Pole všech řádků této tabulky, které mohou být zobrazeny, v tom pořadí, v jakém jsou zobrazovány.
        /// </summary>
        public Row[] Rows { get { return this._GetRows().ToArray(); } }
        /// <summary>
        /// Ověří a zajistí připravenost pole Rows, pole vrátí.
        /// </summary>
        private List<Row> _GetRows()
        {
            List<Row> rows = this._Rows;
            bool heightValid = this._RowListHeightValid;
            if (rows == null || !heightValid)
            {
                lock (this._ValidityLock)
                {
                    rows = this._Rows;
                    if (rows == null)
                    {
                        rows = this.DataTable.RowsSorted.ToList();           // Získat zobrazitelné řádky, setříděné podle zvoleného třídícího sloupce
                        this._Rows = rows;
                        this._IsTreeView = this.DataTable.IsTreeViewTable;   // true = z běžné tabulky se stane TreeView
                        heightValid = false;
                    }
                    if (!heightValid)
                    {
                        SequenceLayout.SequenceLayoutCalculate(rows.Select(r => r.Control)); // Napočítat jejich ISequenceLayout.Begin a ISequenceLayout.End
                        this._RowListHeightValid = true;
                        this._VisibleRows = null;
                    }
                }
            }
            return rows;
        }
        /// <summary>
        /// Pole viditelných řádků této tabulky, které jsou nyní zčásti nebo plně viditelné, v tom pořadí, v jakém jsou zobrazovány.
        /// </summary>
        public Row[] VisibleRows { get { return this._GetVisibleRows().ToArray(); } }
        /// <summary>
        /// Ověří a zajistí připravenost pole VisibleRows, připravené pole vrátí.
        /// Viditelné řádky mají korektně nastaveny aktuální souřadnice do row.RowHeader.VisualRange, neviditelné mají RowHeader.VisualRange == null.
        /// </summary>
        private List<Row> _GetVisibleRows()
        {
            List<Row> visibleRows = this._VisibleRows;
            if (visibleRows == null)
            {
                lock (this._ValidityLock)
                {
                    var rows = this._GetRows();
                    visibleRows = new List<Row>();

                    // Připravím data, které budeme potřebovat pro každý řádek:
                    Rectangle rowAreaBounds = this.RowAreaBounds;
                    Rectangle rowDataBounds = new Rectangle(new Point(0, 0), rowAreaBounds.Size);  // Celý prostor pro data (vpravo od RowHeader, dolů pod ColumnHeader)
                    bool calcBoundsAll = this.DataTable.CalculateBoundsForAllRows;

                    GridPosition rowsPositions = this.RowsPositions;
                    Int32Range dataVisibleRange = rowsPositions.DataVisibleRange;                  // Rozmezí datových pixelů, které jsou viditelné
                    foreach (Row row in rows)
                    {
                        GRow gRow = row.Control;
                        gRow.VisualRange = null;
                        ISequenceLayout isl = gRow as ISequenceLayout;
                        bool isRowVisible = SequenceLayout.IsItemVisible(isl, dataVisibleRange);   // Tento řádek je vidět?
                        if (isRowVisible || calcBoundsAll)
                        {
                            gRow.VisualRange = rowsPositions.GetVisualPosition(isl);
                            this.PrepareRowDataBounds(row, false, rowAreaBounds, rowDataBounds);   // Připravit Bounds pro Row i jeho Cell, ale nedávat do ChilList
                            if (isRowVisible)
                                visibleRows.Add(row);
                        }
                    }
                    this._VisibleRows = visibleRows;
                }
            }
            return visibleRows;
        }
        /// <summary>
        /// Ověří a zajistí platnost hodnoty <see cref="IsTreeView"/>, hodnotu vrátí.
        /// </summary>
        /// <returns></returns>
        private bool _GetIsTreeView()
        {
            this._GetRows();
            return this._IsTreeView;
        }
        /// <summary>
        /// Metoda zajistí změnu výšky daného řádku, a návazné změny v interních strukturách plus překreslení
        /// </summary>
        /// <param name="row"></param>
        /// <param name="height">Požadovaná šířka, může se změnit</param>
        /// <returns></returns>
        public bool RowResizeTo(Row row, ref int height)
        {
            ISequenceLayout isl = row.Control as ISequenceLayout;

            int heightOld = isl.Size;
            isl.Size = height;
            int heightNew = isl.Size;

            bool isChanged = (heightNew != heightOld);
            if (isChanged)
            {
                height = heightNew;
                this.InvalidateData(InvalidateItem.RowHeight | InvalidateItem.Paint);
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
        private List<Row> _Rows;
        /// <summary>
        /// Soupis aktuálně zobrazovaných řádků, vizuální objekty
        /// </summary>
        private List<Row> _VisibleRows;
        /// <summary>
        /// Uložená hodnota <see cref="IsTreeView"/>
        /// </summary>
        private bool _IsTreeView;
        /// <summary>
        /// Zámek pro validaci/invalidaci
        /// </summary>
        private object _ValidityLock;
        #endregion
        #region Pozicování řádků svislé - pozicioner pro řádky, svislý scrollbar vpravo
        /// <summary>
        /// Inicializace objektů pro pozicování tabulek: TablesPositions, TablesScrollBar
        /// </summary>
        private void InitRowsPositions()
        {
            this._RowArea = new GRowArea(this, TableAreaType.RowData);

            this._RowsPositions = new GridPosition(DefaultColumnHeaderHeight, 50, this._RowsPositionGetVisualSize, this._RowsPositionGetDataSize, this._GetVisualFirstPixelRowArea, this._SetVisualFirstPixel);

            this._RowsScrollBar = new ScrollBar(this) { Orientation = System.Windows.Forms.Orientation.Vertical };
            this._RowsScrollBar.ValueChanging += new GPropertyChangedHandler<DecimalNRange>(RowsScrollBar_ValueChange);
            this._RowsScrollBar.ValueChanged += new GPropertyChangedHandler<DecimalNRange>(RowsScrollBar_ValueChange);
        }
        /// <summary>
        /// Vizuální prvek, reprezentující prostor pro data řádků
        /// </summary>
        protected GRowArea RowArea { get { return this._RowArea; } }
        /// <summary>
        /// Řídící prvek pro Pozice řádků
        /// </summary>
        protected GridPosition RowsPositions { get { return this._RowsPositions; } }
        /// <summary>
        /// Vrací výšku prostoru pro řádky (=this.RowAreaBounds.Height)
        /// </summary>
        /// <returns></returns>
        private int _RowsPositionGetVisualSize()
        {
            return this.RowAreaBounds.Height;
        }
        /// <summary>
        /// Vrací výšku všech zobrazitelných datových řádků (samosebou, vyjma řádek ColumnHeader - to není datový řádek).
        /// </summary>
        /// <returns></returns>
        private int _RowsPositionGetDataSize()
        {
            Row[] rows = this.Rows;
            int count = rows.Length;
            return (count > 0 ? ((ISequenceLayout)(rows[count - 1]).Control).End : 0);
        }
        /// <summary>
        /// Vrací hodnotu prvního vizuálního pixelu, kde jsou zobrazována data v rímci RowArea.
        /// Jde o hodnotu 0.
        /// </summary>
        /// <returns></returns>
        private int _GetVisualFirstPixelRowArea()
        {
            return 0;
        }
        /// <summary>
        /// Vrací hodnotu prvního vizuálního pixelu, kde se začíná zobrazovat RowArea.
        /// Jde o hodnotu <see cref="ColumnHeaderHeight"/> + <see cref="TagFilterHeight"/>.
        /// </summary>
        /// <returns></returns>
        private int _GetVisualFirstPixelRowHeader()
        {
            return this.RowAreaBounds.Top;
            //  int columnHeaderHeight = this.ColumnHeaderHeight;
            //  int tagFilterHeight = this.TagFilterHeight;
            //  return columnHeaderHeight + tagFilterHeight;
        }
        /// <summary>
        /// Zapíše danou hodnotu jako pozici prvního vizuálního pixelu, kde jsou zobrazována data.
        /// Daná hodnota se vepisuje do this.DataTable.ColumnHeaderHeight = výška oblasti ColumnHeader.
        /// Po vepsání hodnoty může dojít k úpravě vložené hodnoty podle pravidel.
        /// </summary>
        /// <param name="visualFirstPixel"></param>
        private void _SetVisualFirstPixel(int visualFirstPixel)
        {
            int tagFilterHeight = this.TagFilterHeight;
            this.ColumnHeaderHeight = visualFirstPixel - tagFilterHeight;
        }
        /// <summary>
        /// Eventhandler pro událost změny pozice svislého scrollbaru = posun pole tabulek nahoru/dolů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RowsScrollBar_ValueChange(object sender, GPropertyChangeArgs<DecimalNRange> e)
        {
            int offset = (int)this.RowsScrollBar.Value.Begin.Value;
            if (offset == this.RowsPositions.DataFirstPixel) return;
            this.RowsPositions.DataFirstPixel = offset;
            this.InvalidateData(InvalidateItem.RowScroll);
        }
        /// <summary>
        /// RowsScrollBar : svislý posuvník vpravo od řádků
        /// </summary>
        protected ScrollBar RowsScrollBar { get { this._RowsScrollBarCheck(); return this._RowsScrollBar; } }
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
        /// Vizuální prostor pro zobrazení řádků, jejich vizuální Parent a container
        /// </summary>
        private GRowArea _RowArea;
        /// <summary>
        /// Datový pozicioner pro řádky
        /// </summary>
        private GridPosition _RowsPositions;
        /// <summary>
        /// Scrollbar pro řádky
        /// </summary>
        private ScrollBar _RowsScrollBar;
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
            int rowCount = this.Rows.Length;
            int? activeRowIndex = this.ActiveRowIndex;
            switch (action)
            {
                case InteractivePositionAction.FirstRow:
                    this.ActiveRowIndex = 0;
                    isProcessed = true;
                    break;
                case InteractivePositionAction.LastRow:
                    this.ActiveRowIndex = rowCount - 1;
                    isProcessed = true;
                    break;
                case InteractivePositionAction.RowDown:
                    this.ActiveRowIndex = ((activeRowIndex.HasValue && activeRowIndex.Value < (rowCount - 1)) ? activeRowIndex.Value + 1 : (rowCount - 1));
                    isProcessed = true;
                    break;
                case InteractivePositionAction.RowUp:
                    this.ActiveRowIndex = ((activeRowIndex.HasValue && activeRowIndex.Value > 0) ? activeRowIndex.Value - 1 : 0);
                    isProcessed = true;
                    break;
                case InteractivePositionAction.PageUp:

                case InteractivePositionAction.PageDown:
                    isProcessed = true;
                    break;

                case InteractivePositionAction.WheelUp:
                    this.ScrollRowsByRatio(-0.20m);
                    isProcessed = true;
                    break;
                case InteractivePositionAction.WheelDown:
                    this.ScrollRowsByRatio(0.20m);
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
        public bool IsRowHot(Row row)
        {
            return (row != null && this._HotRow != null && Object.ReferenceEquals(row, this._HotRow));
        }
        /// <summary>
        /// Vrátí true, pokud daná buňka je Hot buňkou (pod myší) v této tabulce
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsCellHot(Cell cell)
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
            RepaintItems((oldHotRow != null ? oldHotRow.Control : null),
                         (oldHotRow != null ? oldHotRow.RowHeader : null),
                         (newHotRow != null ? newHotRow.Control : null),
                         (newHotRow != null ? newHotRow.RowHeader : null));

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
            RepaintItems((oldHotCell != null ? oldHotCell.Control : null), (newHotCell != null ? newHotCell.Control : null));
            this.CallHotCellChanged(oldHotCell, newHotCell, eventSource);
        }
        #endregion
        #region Active Row, Cell = řádek a buňka aktivní (po kliknutí, s kurzorem)
        /// <summary>
        /// Aktuální aktivní řádek = ten, který by měl focus, když by focus (this.HasFocus) měla aktuální tabulka.
        /// Lze setovat hodnotu, daný řádek BUDE nascrollován do viditelné oblasti (na rozdíl od property <see cref="ActiveRowSilent"/>).
        /// </summary>
        public Row ActiveRow
        {
            get { return this._ActiveRow; }
            set { this.SetActiveRow(value, EventSourceType.ApplicationCode, true); }
        }
        /// <summary>
        /// Aktuální aktivní řádek = ten, který by měl focus, když by focus (this.HasFocus) měla aktuální tabulka.
        /// Lze setovat hodnotu, daný řádek NEBUDE nascrollován do viditelné oblasti (na rozdíl od property <see cref="ActiveRow"/>).
        /// </summary>
        public Row ActiveRowSilent
        {
            get { return this._ActiveRow; }
            set { this.SetActiveRow(value, EventSourceType.ApplicationCode, false); }
        }
        /// <summary>
        /// Index aktivního řádku <see cref="ActiveRow"/> = ten, který by měl focus, když by focus (this.HasFocus) měla aktuální tabulka.
        /// Lze setovat hodnotu: pokud hodnota odpovídá nějakému existujícímu řádku, bude tento řádek aktivován. 
        /// Rovněž BUDE nascrollován do viditelné oblasti (na rozdíl od property <see cref="ActiveRowIndexSilent"/>).
        /// </summary>
        public int? ActiveRowIndex
        {
            get
            {
                Row row = this.ActiveRow;
                if (row == null) return null;
                int index = this._Rows.FindIndex(r => Object.ReferenceEquals(r, row));
                return (index >= 0 ? (int?)index : (int?)null);
            }
            set
            {
                Row row = null;
                int? idx = value;
                if (idx.HasValue && idx.Value >= 0 && idx.Value < this._Rows.Count)
                    row = this._Rows[idx.Value];
                this.ActiveRow = row;
            }
        }
        /// <summary>
        /// Index aktivního řádku <see cref="ActiveRow"/> = ten, který by měl focus, když by focus (this.HasFocus) měla aktuální tabulka.
        /// Lze setovat hodnotu: pokud hodnota odpovídá nějakému existujícímu řádku, bude tento řádek aktivován. 
        /// Rovněž NEBUDE nascrollován do viditelné oblasti (na rozdíl od property <see cref="ActiveRowIndex"/>).
        /// </summary>
        public int? ActiveRowIndexSilent
        {
            get
            {
                Row row = this.ActiveRow;
                if (row == null) return null;
                int index = this._Rows.FindIndex(r => Object.ReferenceEquals(r, row));
                return (index >= 0 ? (int?)index : (int?)null);
            }
            set
            {
                Row row = null;
                int? idx = value;
                if (idx.HasValue && idx.Value >= 0 && idx.Value < this._Rows.Count)
                    row = this._Rows[idx.Value];
                this.ActiveRowSilent = row;
            }
        }
        /// <summary>
        /// Aktivní řádek, reference na objekt Row
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
        public bool IsRowActive(Row row)
        {
            return (row != null && this._ActiveRow != null && Object.ReferenceEquals(row, this._ActiveRow));
        }
        /// <summary>
        /// Vrátí true, pokud daná buňka je aktivní buňkou v této tabulce
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsCellActive(Cell cell)
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
        #region Nastavení aktivního řádku, aktivní tabulky; Scroll řádku/tabulky do viditelné oblasti; Repaint řádku, Repaint sloupce
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
            RepaintItems((oldActiveRow != null ? oldActiveRow.Control : null),
                         (oldActiveRow != null ? oldActiveRow.RowHeader : null),
                         (newActiveRow != null ? newActiveRow.Control : null),
                         (newActiveRow != null ? newActiveRow.RowHeader : null));

            this._ActiveRow = newActiveRow;
            this.CallActiveRowChanged(oldActiveRow, newActiveRow, eventSource);

            if (scrollToVisible)
                this.ScrollRowToVisibleArea(newActiveRow);
        }
        /// <summary>
        /// Nastaví danou buňku jako aktivní, případně vyvolá event ActiveRowChanged, nastaví daný řádek tak aby byl vidět
        /// </summary>
        /// <param name="newActiveCell"></param>
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
                this.ScrollRowsReload();
        }
        /// <summary>
        /// Posune obsah řádků nahoru nebo dolů o poměrou část aktuálně viditelné stránky.
        /// Hodnota ratio = +1.0 posune obsah "dolů" o celou stránku = na prvním pixelu nahoře bude po této změně ten pixel, který byl před změnou umístěn dole pod posledním viditelným pixelem.
        /// Hodnota ratio = -0.333 posune obsah "nahoru" o třetinu stránky.
        /// </summary>
        /// <param name="ratio"></param>
        protected void ScrollRowsByRatio(decimal ratio)
        {
            bool isChange = this.RowsPositions.ScrollDataByRatio(ratio);
            if (isChange)
                this.ScrollRowsReload();
        }
        /// <summary>
        /// Přenačte řádky po Scrollu
        /// </summary>
        protected void ScrollRowsReload()
        {
            this.InvalidateData(InvalidateItem.RowScroll);
            // Nastaví _RowsScrollBarDataValid = false;
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
                this.InvalidateData(InvalidateItem.RowScroll);
        }
        /// <summary>
        /// Zajistí vyvolání metody Repaint pro ColumnHeader i pro všechny Cell.Control ve viditelných řádcích v daném sloupci.
        /// Je vhodné volat po změně na časové ose tohoto sloupce.
        /// </summary>
        /// <param name="column"></param>
        protected void RepaintColumn(Column column)
        {
            if (column == null) return;
            ((IInteractiveParent)column.ColumnHeader).Repaint();
            int columnId = column.ColumnId;
            foreach (Row row in this.VisibleRows)
            {
                Cell cell = row[columnId];
                // Požádáme o překreslení buňky. Překreslení buňky zajistí i překreslení grafu. 
                // Graf poté při překreslování sám rozezná rozdíl mezi stavem časové osy aktuálním, 
                //  a tím stavem časové osy, pro který jsou vypočteny souřadnice prvků, a provede si přepočet:
                ((IInteractiveParent)cell.Control).Repaint();
            }
        }
        #endregion
        #region Invalidace, resety, refreshe
        /// <summary>
        /// Zajistí invalidaci všech dat tabulky.
        /// Neřeší invalidaci nadřízeného Gridu.
        /// </summary>
        public void InvalidateData()
        {
            this.InvalidateData(InvalidateItem.Table);
        }
        /// <summary>
        /// Zajistí invalidaci položek po určité akci, která právě skončila.
        /// Volající v podstatě specifikuje, co změnil, a pošle tuto žádost s přiměřeným parametrem.
        /// Tabulka sama nejlíp ví, kam se daný údaj promítá, a co bude potřebovat přepočítat.
        /// </summary>
        /// <param name="items"></param>
        public void InvalidateData(InvalidateItem items)
        {
            // Pokud bude nastaven tento bit OnlyForGrid, znamená to, že tuto invalidaci Grid do podřízených tabulek rozeslal omylem, nebudeme na ni reagovat.
            if (items.HasFlag(InvalidateItem.OnlyForGrid)) return;

            bool callGrid = false;
            lock (this._ValidityLock)
            {

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
                    items |= InvalidateItem.Paint;
                }

                if ((items & (InvalidateItem.TableTagFilter)) != 0)
                {   // Po změně v TagFilteru v tabulce invalidujeme i rozložení tabulky:
                    this._TagFilterExists = null;
                    this._TagFilterHeight = null;
                    this._TableInnerLayoutValid = false;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                }

                if ((items & (InvalidateItem.TableHeight)) != 0)
                {   // Po změně výšky tabulky invalidujeme vnitřní prvky, ale ne viditelné řádky:
                    this._TableInnerLayoutValid = false;
                    this._VisibleColumns = null;
                    this._VisibleRows = null;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                    callGrid = true;
                }

                if ((items & (InvalidateItem.RowHeader)) != 0)
                {   // Po změně šířky RowHeader pozice (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů) (RowHeader, ColumnHeader):
                    this._TableInnerLayoutValid = false;
                    this._VisibleColumns = null;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                    callGrid = true;
                }

                if ((items & (InvalidateItem.ColumnHeader)) != 0)
                {   // Po změně vnitřního uspořádání (vlivem změny rozměrů, nebo vlivem posunu vnitřních splitterů) (ColumnHeader):
                    this._TableInnerLayoutValid = false;
                    this._VisibleColumns = null;
                    this._VisibleRows = null;
                    this._RowsScrollBarDataValid = false;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                    // Změna RowHeader by mohla zajímat i ostatní tabulky:
                    if ((items & (InvalidateItem.RowHeader)) != 0)
                        callGrid = true;
                }

                if ((items & (InvalidateItem.ColumnsCount | InvalidateItem.ColumnOrder)) != 0)
                {   // Po změně počtu nebo pořadí sloupců: zrušíme pole sloupců, vygeneruje se znovu:
                    this._Columns = null;
                    this._VisibleColumns = null;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                    callGrid = true;
                }

                if ((items & (InvalidateItem.ColumnWidth | InvalidateItem.ColumnScroll)) != 0)
                {   // Po změně šířky sloupce nebo scrollu sloupců: zrušíme pole viditelných sloupců, vygeneruje se znovu:
                    this._ColumnListWidthValid = false;
                    this._VisibleColumns = null;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                    callGrid = true;
                }

                if ((items & (InvalidateItem.RowsCount | InvalidateItem.RowOrder)) != 0)
                {   // Po změně počtu nebo pořadí řádků: zrušíme pole řádků, vygeneruje se znovu:
                    this._Rows = null;
                    this._RowListHeightValid = false;
                    this._VisibleRows = null;
                    this._RowsScrollBarDataValid = false;
                    this._ChildArrayValid = false;
                    this._TableInnerLayoutValid = false;                 // Přepočítáme vnitřní prvky, protože se může měnit viditelnost Scrollbaru od řádků...
                    items |= InvalidateItem.Paint;
                }

                if ((items & (InvalidateItem.RowHeight)) != 0)
                {   // Po změně výšky řádku: zrušíme příznak platnosti výšky v řádcích, a zrušíme pole viditelných řádků, vygeneruje se znovu:
                    this._RowListHeightValid = false;
                    this._VisibleRows = null;
                    this._RowsScrollBarDataValid = false;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                }
                if ((items & (InvalidateItem.RowScroll)) != 0)
                {   // Po scrollu řádků: zrušíme pole viditelných řádků, vygeneruje se znovu:
                    this._VisibleRows = null;
                    this._RowsScrollBarDataValid = false;
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                }
                if ((items & (InvalidateItem.TableItems)) != 0)
                {   // Po změně obsahu tabulky: zrušíme platnost pro pole ChildArray, vygeneruje se znovu:
                    this._ChildArrayValid = false;
                    items |= InvalidateItem.Paint;
                }
                if ((items & (InvalidateItem.Paint)) != 0)
                {   // Požadavek na kreslení tabulky:
                    this.Repaint();
                }
            }

            // Předáme to šéfovi, pokud ho máme, a pokud to pro něj může být zajímavé, a pokud událost není určena jen pro naše (OnlyForTable) potřeby:
            if (this.HasGrid && callGrid && !items.HasFlag(InvalidateItem.OnlyForTable))
                this.Grid.Invalidate(items | InvalidateItem.OnlyForGrid);
        }
        #endregion
        #region TableHeader a jeho funkčnost (DeSelect All, TreeViewNode Expand / Collapse All)
        /// <summary>
        /// Funkce, kterou aktuálně má objekt TableHeader
        /// </summary>
        protected TableHeaderFunctionType TableHeaderFunction
        {
            get
            {
                if (this.DataTable.HasCheckedRows) return TableHeaderFunctionType.DeselectAll;
                if (!this.IsTreeView) return TableHeaderFunctionType.None;
                if (this.Rows.Any(r => r.TreeNode.IsRoot && r.TreeNode.IsExpanded)) return TableHeaderFunctionType.CollapseAll;
                return TableHeaderFunctionType.ExpandAll;
            }
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví tabulky.
        /// </summary>
        /// <param name="e"></param>
        protected void TableHeaderClick(GInteractiveChangeStateArgs e)
        {
            bool repaint = false;
            switch (this.TableHeaderFunction)
            {
                case TableHeaderFunctionType.DeselectAll:
                    foreach (Row row in this.Rows.Where(r => r.IsChecked))
                    {
                        row.IsChecked = false;
                        repaint = true;
                    }
                    break;
                case TableHeaderFunctionType.ExpandAll:
                    TreeNode.ExpandAll(this.Rows.Select(r => r.TreeNode).Where(n => n.IsRoot));
                    break;
                case TableHeaderFunctionType.CollapseAll:
                    TreeNode.CollapseAll(this.Rows.Select(r => r.TreeNode).Where(n => n.IsRoot));
                    repaint = true;
                    break;
            }

            if (repaint)
                this.Repaint();
        }
        #endregion
        #region HeaderSplitter : splitter umístěný pod hlavičkou sloupců, je součástí GTable.Items
        /// <summary>
        /// Inicializuje objekt _HeaderSplitter.
        /// </summary>
        protected void InitHeaderSplitter()
        {
            this._HeaderSplitter = new Splitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4, LinkedItemPrevMinSize = 50, LinkedItemNextMinSize = 50, IsResizeToLinkItems = true };
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
            this.ColumnHeaderHeight = value;               // Tady dojde ke kompletnímu vyhodnocení pravidel pro výšku ColumnHeader (Minimum, Default, Range)
            e.CorrectValue = this.ColumnHeaderHeight;      // Pokud požadovaná hodnota (value) nebyla akceptovatelná, pak correctValue je hodnota přípustná
            if (e.IsChangeValue)
            {
                this.InvalidateData(InvalidateItem.RowScroll);
            }
        }
        /// <summary>
        /// TableSplitter = Splitter mezi ColumnHeader a RowArea
        /// Tento Splitter je součástí this.Childs, protože by neměl odcházet mimo this.GTable (na rozdíl od TableSplitter).
        /// </summary>
        protected Splitter HeaderSplitter { get { this._HeaderSplitterCheck(); return this._HeaderSplitter; } }
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
        private Splitter _HeaderSplitter;
        #endregion
        #region Obecný filtr na tabulce
        /// <summary>
        /// Metoda zajistí zrušení všech filtrů na aktuální tabulce
        /// </summary>
        public void ResetAllRowFilters(ref bool callRefresh)
        {
            // Reset TagFilter:
            if (this.TagFilterVisible)
                this.TagFilterReset(ref callRefresh);

            // Reset programového filtru:
            if (this.DataTable.RowFiltersExists)
                callRefresh = true;
            this.DataTable.ResetFilters();
        }
        /// <summary>
        /// Metoda aplikuje daný Aplikační filtr na řádky tabulky
        /// </summary>
        /// <param name="tableFilter"></param>
        public void ApplyRowFilter(TableFilter tableFilter)
        {
            this.DataTable.AddFilter(tableFilter);
        }
        /// <summary>
        /// Metoda aplikuje daný Aplikační filtr na řádky tabulky
        /// </summary>
        /// <param name="filterName">Název filtru</param>
        /// <param name="filterFunc">Funkce filtru</param>
        public void ApplyRowFilter(string filterName, Func<Row, bool> filterFunc)
        {
            this.DataTable.AddFilter(filterName, filterFunc);
        }
        /// <summary>
        /// Odewbere filtr daného jména
        /// </summary>
        /// <param name="filterName"></param>
        /// <returns></returns>
        public bool RemoveFilter(string filterName)
        {
            return this.DataTable.RemoveFilter(filterName);
        }
        #endregion
        #region TagFilter : filtr řádků na základě TagItems, je součástí GTable.Items
        /// <summary>
        /// Příznak existence dat pro filtr TagFilter.
        /// Obsahuje true, pokud napojená datová tabulka obsahuje položky pro filtr TagFilter.
        /// Tuto hodnotu nelze nastavit dle přání, k tomu se používá property <see cref="TagFilterEnabled"/>.
        /// </summary>
        public bool TagFilterExists { get { this._TagFilterExistsCheck(); return this._TagFilterExists.Value; } }
        /// <summary>
        /// Uživatelská hodnota, reprezentující přání aplikace, aby byl zobrazen filtr TagFilter.
        /// Aplikace může filtr skrýt nastavením <see cref="TagFilterEnabled"/> = false (pak filtr nebude zobrazen).
        /// Pokud aplikace nastaví <see cref="TagFilterEnabled"/> = true, pak bude filtr zobrazen jen tehdy, pokud filtr reálně existuje (<see cref="TagFilterExists"/>).
        /// Reálná viditelnost filtru je pak tedy součinem: <see cref="TagFilterVisible"/> = (<see cref="TagFilterEnabled"/> and <see cref="TagFilterExists"/>);
        /// Výchozí hodnota <see cref="TagFilterEnabled"/> je true.
        /// </summary>
        public bool TagFilterEnabled { get { return this._TagFilterEnabled; } set { this._TagFilterEnabled = value; this.InvalidateData(InvalidateItem.TableTagFilter); } }
        /// <summary>
        /// Barva pozadí filtru TagFilter
        /// </summary>
        public Color? TagFilterBackColor { get { return this._TagFilter.BackColor; } set { this._TagFilter.BackColor = value; this.InvalidateData(InvalidateItem.Paint); } }
        /// <summary>
        /// Filtr řádků TagFilter:
        /// Výška jednoho prvku.
        /// </summary>
        public int TagFilterItemHeight { get { return this._TagFilter.ItemHeight; } set { this._TagFilter.ItemHeight = value; this.InvalidateData(InvalidateItem.TableTagFilter); } }
        /// <summary>
        /// Filtr řádků TagFilter:
        /// Nejvyšší počet zobrazitelných prvků.
        /// Zatím bez efektu.
        /// </summary>
        public int TagFilterItemMaxCount { get { return 0; } set { } }
        /// <summary>
        /// Obsahuje true, pokud je reálně zobrazen filtr TagFilter.
        /// <see cref="TagFilterVisible"/> = (<see cref="TagFilterEnabled"/> and <see cref="TagFilterExists"/>);
        /// </summary>
        public bool TagFilterVisible { get { return (this.TagFilterEnabled && this.TagFilterExists); } }
        /// <summary>
        /// Vlastnost <see cref="TagFilter.RoundItemPercent"/> pro filtr TagFilter:
        /// Procento kulatých krajů jednotlivých prvků.
        /// 0 = hranaté prvky; 100 = 100% = čisté půlkruhy. Hodnoty mimo rozsah jsou zarovnané do rozsahu 0 až 100 (včetně).
        /// </summary>
        public int TagFilterRoundItemPercent { get { return this._TagFilter.RoundItemPercent; } set { this._TagFilter.RoundItemPercent = value; } }
        /// <summary>
        /// Metoda zruší filtr TagFilter
        /// </summary>
        public void TagFilterReset(ref bool callRefresh)
        {
            if (this._TagFilter == null) return;
            this._TagFilter.TagFilterReset(ref callRefresh);
        }
        /// <summary>
        /// Inicializace systému TagFilter
        /// </summary>
        private void InitTagFilter()
        {
            this._TagHeaderL = new GTagLine(this._DataTable, TableAreaType.TagFilterHeaderLeft);
            this._TagFilter = new TagFilter() { ExpandHeightOnMouse = true, RoundItemPercent = 0 };
            this._TagHeaderR = new GTagLine(this._DataTable, TableAreaType.TagFilterHeaderRight);
            this._TagFilterEnabled = true;
            this._TagFilter.FilterChanged += _TagFilter_FilterChanged;
            if (this._DataTable != null)
                this._DataTable.TagItemsChanged += this._TagItemsChanged;
        }
        /// <summary>
        /// Obsluha události, kdy uživatel změnil výběr ve filtru TagFilter.
        /// Je třeba provést aplikaci filtru a překreslení tabulky.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TagFilter_FilterChanged(object sender, EventArgs e)
        {
            if (this._DataTable != null)
                this._DataTable.TagItemsSetFilter(this._TagFilter.FilteredItems);
        }
        /// <summary>
        /// Eventhandler události, kdy navázaná <see cref="DataTable"/> provede změnu <see cref="Table.TagItemsChanged"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TagItemsChanged(object sender, EventArgs args)
        {
            this.InvalidateData(InvalidateItem.TableTagFilter);
        }
        /// <summary>
        /// Výška oblasti TagFilter.
        /// Pokud aktuální tabulka nemá žádné TagItems, pak <see cref="TagFilterHeight"/> = 0.
        /// Pokud nějaké TagItems má, pak <see cref="TagFilterHeight"/> odpovídá výšce <see cref="TagFilter.OptimalHeightOneRow"/>.
        /// </summary>
        protected int TagFilterHeight { get { this._TagFilterHeightCheck(); return this._TagFilterHeight.Value; } }
        /// <summary>
        /// Zajistí, že proměnná <see cref="_TagFilterHeight"/> bude obsahovat platnou hodnotu: 
        /// buď 0px (pokud <see cref="TagFilterVisible"/> = false),
        /// anebo hodnotu z <see cref="TagFilter.OptimalHeightOneRow"/>, 
        /// vypočtenou pro výšku jednoho prvku <see cref="TagFilter.ItemHeight"/> načtenou z <see cref="Table.TagItemsRowHeight"/>.
        /// </summary>
        private void _TagFilterHeightCheck()
        {
            if (this._TagFilterHeight.HasValue) return;

            if (this.TagFilterVisible)
            {
                this._TagFilter.ItemHeight = this._DataTable.TagItemsRowHeight ?? Skin.TagFilter.ItemHeight;
                this._TagFilterHeight = this._TagFilter.OptimalHeightOneRow;
            }
            else
            {
                this._TagFilterHeight = 0;
            }
        }
        /// <summary>
        /// Privátní úložiště pro výšku objektu TagFilter.
        /// Na null je nastaven při invalidaci <see cref="InvalidateItem.TableTagFilter"/>.
        /// Na platnou hodnotu je nastaven v <see cref="_TagFilterHeightCheck()"/>.
        /// </summary>
        private int? _TagFilterHeight;
        private bool _TagFilterEnabled;
        /// <summary>
        /// Prověří/nastaví platnost hodnoty <see cref="_TagFilterExists"/>.
        /// </summary>
        private void _TagFilterExistsCheck()
        {
            if (this._TagFilterExists.HasValue) return;
            bool tagFilterVisible = false;
            if (this._DataTable != null)
            {
                tagFilterVisible = this._DataTable.HasTagItems;
                if (tagFilterVisible)
                    this._TagFilter.TagItems = this._DataTable.TagItems;
            }
            this._TagFilterExists = tagFilterVisible;
        }
        /// <summary>
        /// Privátní příznak viditelnosti objektu TagFilter.
        /// Na null je nastaven při invalidaci <see cref="InvalidateItem.TableTagFilter"/>.
        /// Na platnou hodnotu je nastaven v <see cref="_TagFilterExistsCheck()"/>.
        /// </summary>
        private bool? _TagFilterExists;
        /// <summary>
        /// Instance prvku <see cref="GRowHeader"/>. Vždy má správné souřadnice.
        /// </summary>
        protected GTagLine TagHeaderL { get { this._TagHeaderL.Bounds = this.TagHeaderLBounds; return this._TagHeaderL; } }
        /// <summary>
        /// Instance prvku <see cref="TagFilter"/>. Vždy má správné souřadnice.
        /// </summary>
        protected TagFilter TagFilter { get { if (this._TagFilter.CurrentHeightState == TagFilterHeightState.OneRow) this._TagFilter.Bounds = this.TagFilterBounds; return this._TagFilter; } }
        /// <summary>
        /// Instance prvku <see cref="GRowHeader"/>. Vždy má správné souřadnice.
        /// </summary>
        protected GTagLine TagHeaderR { get { this._TagHeaderR.Bounds = this.TagHeaderRBounds; return this._TagHeaderR; } }
        /// <summary>
        /// Instance prvku <see cref="GRowHeader"/>, který je zobrazován před objektem <see cref="TagFilter"/>
        /// </summary>
        private GTagLine _TagHeaderL;
        /// <summary>
        /// Instance prvku GTagFilter = filtr řádků na základě TagItems
        /// </summary>
        private TagFilter _TagFilter;
        /// <summary>
        /// Instance prvku <see cref="GRowHeader"/>, který je zobrazován před objektem <see cref="TagFilter"/>
        /// </summary>
        private GTagLine _TagHeaderR;
        #endregion
        #region Linky grafu : koordinační objekt GTimeGraphLinkArray
        /// <summary>
        /// Reference na koordinační objekt pro kreslení linek všech grafů v této tabulce, třída: <see cref="Graphs.TimeGraphLinkItem"/>.
        /// Tento prvek slouží jednotlivým grafům.
        /// </summary>
        public Graphs.TimeGraphLinkArray GraphLinkArray
        {
            get
            {
                if (this._GraphLinkArray == null)
                {   // Dosud nemáme referenci na GTimeGraphLinkArray, vytvoříme ji a zajistíme, že bude součástí našich Childs prvků:
                    this._GraphLinkArray = new Graphs.TimeGraphLinkArray(this);
                    this.GraphLinkArrayIsOnTable = true;
                    this.InvalidateData(InvalidateItem.TableItems);
                }
                return this._GraphLinkArray;
            }
        }
        /// <summary>
        /// true pokud máme vytvořenou svoji zdejší instanci <see cref="GraphLinkArray"/> = pro tuto tabulku.
        /// Pak bychom ji měli vkládat do našich Childs.
        /// false = instance neexistuje, anebo to není naše instance, nebudeme ji dávat do Childs.
        /// </summary>
        protected bool GraphLinkArrayIsOnTable { get; private set; }
        /// <summary>
        /// Instance prvku <see cref="Graphs.TimeGraphLinkArray"/>, ať už je naše nebo cizí
        /// </summary>
        private Graphs.TimeGraphLinkArray _GraphLinkArray;
        #endregion
        #region TableSplitter :  splitter umístěný dole pod tabulkou, je součástí Parenta
        /// <summary>
        /// Inicializuje objekt _TableSplitter.
        /// </summary>
        protected void InitTableSplitter()
        {
            this._TableSplitter = new Splitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = TableSplitterSize, SplitterActiveOverlap = 4, LinkedItemPrevMinSize = 50, LinkedItemNextMinSize = 50, IsResizeToLinkItems = true };
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
            // Tato metoda obhospodařuje události ValueChanging i ValueChanged; ale při Interaktivní změně hodnoty vynecháváme konec interaktivního pohybu:
            //   Důvod? Reagovali jsme průběžně (na InteractiveChanging), ale v události InteractiveChanged je hodnota (e.NewValue - e.OldValue) vztažena k výchozí pozici Splitteru (na začátku pohybu)
            //     a na to tady nejsme připraveni...
            if (e.EventSource.HasFlag(EventSourceType.InteractiveChanged)) return;
            int offset = e.OldValue - this.Bounds.Bottom;  // O kolik pixelů je posunutá hodnota Splitteru (původní Value) proti Bottom tabulky = půl šířky splitteru?

            int heightOld = this.DataTable.Height;
            Application.Log.AddInfo(this.GetType(), "TableSplitter.LocationChange", "Grid.SequenceHeight(Before) = " + this.Grid.TablesVisibleSequenceHeight, "heightOld = " + heightOld.ToString());

            int delta = e.NewValue - e.OldValue;           // Rozdíl v hodnotě
            int height = heightOld + delta;
            this.DataTable.Height = height;                // Tady dojde ke kompletnímu vyhodnocení vnitřních pravidel pro výšku Table (Minimum, Default, Range)
            int heightNew = this.DataTable.Height;

            if (heightNew != heightOld && this.Grid != null)
            {   // Tabulka sama novou výšku akceptovala; musíme ji protlačit do Gridu:
                this.Grid.TableHeightChanged(this, heightOld, heightNew);      // Tady proběhne invalidace výšky
                var tablesCurrent = this.Grid.TablesVisibleCurrent;            // Tady proběhne validace souřadnic Y pro viditelné tabulky
                int valueNew = this.Bounds.Bottom + offset;                    // Tady by měl býti Splitter (=jeho Value) po všech validacích výšek GTable uvnitř GGridu
                e.CorrectValue = valueNew;
            }

            Application.Log.AddInfo(this.GetType(), "TableSplitter.LocationChange", "Grid.SequenceHeight(After) = " + this.Grid.TablesVisibleSequenceHeight, "heightNew = " + heightNew.ToString());


            /*
            // Vypočteme výšku tabulky - pozice splitteru (_TableSplitter) je na dolním okraji tabulky, a řídí výšku tabulky = relativně k její pozici Top:
            int tableTop = this.Bounds.Top;
            int heightOld = this.DataTable.Height;
            Application.Log.AddInfo(this.GetType(), "TableSplitter.LocationChange", "Grid.SequenceHeight(Before) = " + this.Grid.TablesVisibleSequenceHeight, "heightOld = " + heightOld.ToString());

            int height = this._TableSplitter.Value - tableTop;
            this.DataTable.Height = height;                // Tady dojde ke kompletnímu vyhodnocení vnitřních pravidel pro výšku Table (Minimum, Default, Range)
            int heightNew = this.DataTable.Height;
            e.CorrectValue = tableTop + heightNew;         // Pokud požadovaná hodnota (value) nebyla akceptovatelná, pak correctValue je hodnota přípustná
            if (e.IsChangeValue)
            {
                this.Grid.TableHeightChanged(this, heightOld, heightNew);
            }
            Application.Log.AddInfo(this.GetType(), "TableSplitter.LocationChange", "Grid.SequenceHeight(After) = " + this.Grid.TablesVisibleSequenceHeight, "heightNew = " + heightNew.ToString());
            */
        }
        /// <summary>
        /// TableSplitter = Splitter dole pod tabulkou.
        /// Tento Splitter není součástí this.Childs (protože pak by byl omezen do this.Bounds), je součástí Childs nadřízeného prvku (GGrid), protože pak se může pohybovat v jeho prostoru.
        /// </summary>
        internal Splitter TableSplitter { get { this._TableSplitterCheck(); return this._TableSplitter; } }
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
        private Splitter _TableSplitter;
        /// <summary>
        /// Vizuální velikost splitteru pod tabulkou = jeho výška v pixelech
        /// </summary>
        public static int TableSplitterSize { get { return 3; } }
        #endregion
        #region Child items : pole všech prvků v tabulce (jednotlivé headery, jednotlivé řádky, jednotlivé spittery, a scrollbar řádků)
        /// <summary>
        /// Pole sub-itemů v tabulce.
        /// Tabulka obsahuje: hlavičku tabulky, hlavičky viditelných sloupců, hlavičky viditelných řádků, buňky viditelných řádků a sloupců, 
        /// splitery hlaviček sloupců a hlaviček řádků (pokud je povoleno jejich resize), a scrollbar řádků (pokud je viditelný).
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this._ChildArrayCheck(); return this._ChildList; } }
        private List<IInteractiveItem> _ChildList;
        /// <summary>
        /// Validita prvků v poli ChildItems
        /// </summary>
        private bool _ChildArrayValid;
        /// <summary>
        /// Zajistí platnost pole sub-itemů.
        /// </summary>
        private void _ChildArrayCheck()
        {
            if (this._ChildList != null && this._ChildArrayValid) return;
            this._ChildList = new List<IInteractiveItem>();
            // Něco k pořadí vkládání prvků do Items: dospodu dáme to, co by mělo být "vespodu" = obsah buněk. Nad ně dáme Headers a na ně Splitters:
            this._ChildItemsAddRowsContent();                        // Řádky: buňky plus záhlaví, ale ne oddělovače
            this._ChildItemsAddTagFilter();                          // Objekty pro TagFilter (záhlaví + Filtr) : až po řádkách, protože se může kreslit "přes ně"
            this._ChildItemsAddColumnHeaders();                      // Záhlaví sloupců (TableHeader + ColumnHeaders)
            this._ChildItemsAddColumnSplitters();                    // Oddělovače sloupců, které to mají povoleno
            this._ChildItemsAddHeaderSplitter();                     // Oddělovač pod hlavičkami sloupců (řídí výšku záhlaví)
            this._ChildItemsAddRowsSplitters();                      // Řádky: oddělovače řádků, pokud je povoleno
            this._ChildItemsAddRowsScrollBar();                      // Scrollbar řádků, pokud je viditelný
            this._ChildItemsAddGraphLinkArray();                     // Koordinátor linků grafů, pokud je viditelný
            this._ChildArrayValid = true;
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechna záhlaví sloupců (tedy TableHeader + VisibleColumns.ColumnHeader).
        /// Nepřidává splittery: ani mezi sloupci, ani pod Headers.
        /// </summary>
        protected void _ChildItemsAddColumnHeaders()
        {
            GComponent.PrepareChildOne(this.DataTable.TableHeader, this, this.TableHeaderBounds, this._ChildList);

            Int32Range headerYBounds = this.ColumnHeadersBounds.GetVisualRange(System.Windows.Forms.Orientation.Vertical);
            foreach (Column column in this.VisibleColumns)
            {
                GColumnHeader columnHeader = column.ColumnHeader;
                GComponent.PrepareChildOne(columnHeader, this, columnHeader.VisualRange, headerYBounds, this._ChildList);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá objekty pro TagFilter: záhlaví a objekt TagFilter.
        /// Přidává je tehdy, když TagFilter je viditelný.
        /// </summary>
        protected void _ChildItemsAddTagFilter()
        {
            if (this.TagFilterVisible)
            {
                this._ChildList.Add(this.TagHeaderL);
                this._ChildList.Add(this.TagFilter);
                this._ChildList.Add(this.TagHeaderR);
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
                this._ChildList.Add(tableHeader.ColumnSplitter);

            foreach (Column column in this.VisibleColumns)
            {
                GColumnHeader columnHeader = column.ColumnHeader;
                if (columnHeader.ColumnSplitterVisible)
                    this._ChildList.Add(columnHeader.ColumnSplitter);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá obsah za všechny viditelné řádky (VisibleRows).
        /// Obsah = RowHeader + Cells.
        /// Nepřidává vodorovný Splitter pod RowHeader.
        /// </summary>
        protected void _ChildItemsAddRowsContent()
        {
            this.RowArea.ChildsClear();
            GComponent.PrepareChildOne(this.RowArea, this, this.RowAreaBounds, this._ChildList);   // Do Tabulky vložíme kontejner pro všechny řádky

            // Připravím data, které budeme potřebovat pro každý řádek:
            Rectangle rowAreaBounds = this.RowAreaBounds;
            Rectangle rowDataBounds = new Rectangle(new Point(0, 0), rowAreaBounds.Size);          // Celý prostor pro data (vpravo od RowHeader, dolů pod ColumnHeader)
            Int32Range rowHeaderXRange = this.RowHeadersBounds.GetVisualRange(System.Windows.Forms.Orientation.Horizontal);  // Pozice RowHeader na ose X  (záhlaví řádků)

            foreach (Row row in this.VisibleRows)
                this._ChildItemsAddRowContent(row, rowHeaderXRange, rowAreaBounds, rowDataBounds);
        }
        /// <summary>
        /// Do pole this.ChildList přidá prvky odpovídající jednomu daného řádku, obsahem jsou jednotlivé GRowHeaders.
        /// Splittery řádků se přidávají až nakonec, ale tady se počítají jejich souřadnice.
        /// Jednotlivé prvky řádku (GCells) jsou jako Childs umístěny v GRow.
        /// Jednotlivé řádky (GRow) jsou jako Childs umístěny v this.RowArea, nikoli v GTable.
        /// Nicméně tato metoda <see cref="_ChildItemsAddRowContent(Row, Int32Range, Rectangle, Rectangle)"/> vyvolá i přípravu těchto Childs v GRow, společně s informací o viditelných sloupcích a jejich souřadnicích.
        /// + za každý viditelný sloupec (VisibleColumns) pak obsah vizuální buňky (row[column.ColumnId].Control).
        /// </summary>
        /// <param name="row"></param>
        /// <param name="rowHeaderXRange"></param>
        /// <param name="rowAreaBounds"></param>
        /// <param name="rowDataBounds"></param>
        protected void _ChildItemsAddRowContent(Row row, Int32Range rowHeaderXRange, Rectangle rowAreaBounds, Rectangle rowDataBounds)
        {
            // Data řádku (to, co obsahuje data = hned za RowHeader, tedy prostor pro viditelné buňky):
            this.PrepareRowDataBounds(row, true, rowAreaBounds, rowDataBounds);

            // Záhlaví aktuálního řádku přidám až po buňkách, bude "navrchu" z hlediska kreslení i interaktivity:
            this.PrepareRowHeaderBounds(row, true, rowHeaderXRange);
        }
        /// <summary>
        /// Připraví korektní souřadnice pro obsah daného řádku i pro jeho vnitřní buňky Cells.
        /// Volitelně jej přidá do kolekce this.RowArea.ChildList.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="addAsChild"></param>
        /// <param name="rowAreaBounds"></param>
        /// <param name="rowDataBounds"></param>
        protected void PrepareRowDataBounds(Row row, bool addAsChild, Rectangle rowAreaBounds, Rectangle rowDataBounds)
        {
            Rectangle rowOneBounds;                                                                               // out: Prostor aktuálního řádku
            row.Control.PrepareChilds(rowAreaBounds.X, rowDataBounds, this.VisibleColumns, out rowOneBounds);     // Řádek si vyřeší svoje buňky, a nakonec i určí svoje souřadnice
            GComponent.PrepareChildOne(row.Control, this.RowArea, rowOneBounds, (addAsChild ? this.RowArea.ChildList : null)); // Do řádku GRow vložíme referenci Parent = this.RowArea, a do this.RowArea.ChildList vložíme GRow (kontejner pro data řádku)
        }
        /// <summary>
        /// Připraví korektní souřadnice pro Header a Splitter daného řádku.
        /// Volitelně jej přidá do kolekce this._ChildList.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="addAsChild"></param>
        /// <param name="rowHeaderXRange"></param>
        protected void PrepareRowHeaderBounds(Row row, bool addAsChild, Int32Range rowHeaderXRange)
        {
            Int32Range rowHeaderYRange = row.Control.VisualRange + this._GetVisualFirstPixelRowHeader();
            GComponent.PrepareChildOne(row.RowHeader, this, rowHeaderXRange, rowHeaderYRange, (addAsChild ? this._ChildList : null));
            row.RowHeader.PrepareSplitterBounds();
        }
        /// <summary>
        /// Do pole this.ChildList přidá HeaderSplitter, pokud tabulka povoluje změnu výšky záhlaví (DataTable.AllowColumnHeaderResize)
        /// </summary>
        protected void _ChildItemsAddHeaderSplitter()
        {
            if (this.DataTable.AllowColumnHeaderResize)
                this._ChildList.Add(this.HeaderSplitter);
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
                this._ChildList.Add(rowHeader.RowSplitter);
        }
        /// <summary>
        /// Do pole this.ChildList přidá RowsScrollBar, pokud je viditelný (RowsScrollBarVisible)
        /// </summary>
        protected void _ChildItemsAddRowsScrollBar()
        {
            if (this.RowsScrollBarVisible)
            {
                this.RowsScrollBar.Bounds = this.RowsScrollBarBounds;
                this._ChildList.Add(this.RowsScrollBar);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá _GraphLinkArray, pokud je viditelný (HasGraphLinkArray)
        /// </summary>
        protected void _ChildItemsAddGraphLinkArray()
        {
            if (this.GraphLinkArrayIsOnTable)
            {
                this._ChildList.Add(this._GraphLinkArray);
            }
        }
        #endregion
        #region SequenceLayout - výška a pozice této tabulky [v pixelech]
        /// <summary>
        /// Inicializuje řízení pozice této <see cref="GTable"/> v návaznosti na výšku <see cref="Table"/>.
        /// Metoda je volána poté, kdy reference na tabulku <see cref="DataTable"/> je již naplněna.
        /// </summary>
        protected void InitTableSequence()
        {
            this._SequenceLayout = new SequenceLayout(this.DataTable.TableSize);
        }
        int ISequenceLayout.Order { get { return this._ISequenceLayout.Order; } set { this._ISequenceLayout.Order = value; } }
        int ISequenceLayout.Begin { get { return this._ISequenceLayout.Begin; } set { this._ISequenceLayout.Begin = value; } }
        int ISequenceLayout.Size { get { return this._ISequenceLayout.Size; } set { this._SequenceLayout.Size = value; } }
        int ISequenceLayout.End { get { return this._ISequenceLayout.End; } }
        bool ISequenceLayout.AutoSize { get { return this._SequenceLayout.AutoSize; } }
        private ISequenceLayout _ISequenceLayout { get { return (ISequenceLayout)this._SequenceLayout; } }
        private SequenceLayout _SequenceLayout;
        #endregion
        #region TreeView - řízení a kreslení
        /// <summary>
        /// true pokud this tabulka může zobrazovat stromovou strukturu prvků
        /// </summary>
        internal bool IsTreeView { get { return this._GetIsTreeView(); } }
        /// <summary>
        /// Metoda vykreslí všechny prvky související s TreeView.
        /// Do argumentu vloží souřadnici prostoru, který má být interaktivní - zde je vykreslena ikona pro Expand/Collapse nodu daného řádku.
        /// Pokud řádek nemá tuto ikonu, vrací null.
        /// Nastavuje out parametr offsetX, který posouvá vykreslovaný textový obsah doprava.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="drawArgs"></param>
        /// <returns></returns>
        internal void DrawTreeView(GInteractiveDrawArgs e, TreeViewDrawArgs drawArgs)
        {
            if (drawArgs == null || !drawArgs.IsValid) return;

            drawArgs.CellValueBounds = drawArgs.BoundsAbsolute;
            drawArgs.LastLineBounds = null;
            drawArgs.IconActiveBounds = null;
            drawArgs.IconImageBounds = null;
            if (!this.DataTable.IsTreeViewTable) return;

            this.DrawTreeViewLines(e, drawArgs);
            this.DrawTreeViewIcon(e, drawArgs);
        }
        private void DrawTreeViewLines(GInteractiveDrawArgs e, TreeViewDrawArgs drawArgs)
        {
            drawArgs.TreeNodeLines = drawArgs.OwnerCell.Row.TreeNode.GetTreeLines();

            Rectangle boundsCell = drawArgs.CellValueBounds;
            int x = boundsCell.X + 1;
            int n = x;
            int y = boundsCell.Y;
            int h = boundsCell.Height;
            int w = drawArgs.TreeViewNodeOffset;
            Rectangle? lastLineBounds = null;
            foreach (TreeNodeLineType line in drawArgs.TreeNodeLines)
            {
                x = n;
                Rectangle bounds = new Rectangle(x, y, w, h);
                this.DrawTreeViewLine(e, drawArgs, line, bounds);
                n = bounds.Right;
                lastLineBounds = bounds;
            }
            drawArgs.LastLineBounds = lastLineBounds;
            drawArgs.CellValueBounds = new Rectangle(x, boundsCell.Y, boundsCell.Right - x, boundsCell.Height);
        }
        private void DrawTreeViewLine(GInteractiveDrawArgs e, TreeViewDrawArgs drawArgs, TreeNodeLineType line, Rectangle bounds)
        {
            if (line == TreeNodeLineType.None) return;

            Pen pen = DrawTreeViewGetPen(drawArgs);

            int x1 = bounds.X + 9;
            int x2 = x1 + 1;
            int x9 = bounds.Right;
            int y0 = bounds.Y;
            int y1 = bounds.Bottom - 12;
            int y2 = bounds.Bottom - 4;
            int y9 = bounds.Bottom;

            switch (line)
            {
                case TreeNodeLineType.First:
                    e.Graphics.DrawLine(pen, x1, y2, x1, y9);
                    break;
                case TreeNodeLineType.Line:
                    e.Graphics.DrawLine(pen, x1, y0, x1, y9);
                    break;
                case TreeNodeLineType.LineBranch:
                    e.Graphics.DrawLine(pen, x1, y0, x1, y9);
                    e.Graphics.DrawLine(pen, x2, y1, x9, y1);
                    break;
                case TreeNodeLineType.LineLast:
                    e.Graphics.DrawLine(pen, x1, y0, x1, y1);
                    e.Graphics.DrawLine(pen, x2, y1, x9, y1);
                    break;
            }
        }
        private Pen DrawTreeViewGetPen(TreeViewDrawArgs drawArgs)
        {
            Color color = (drawArgs.TreeViewLinkColor.HasValue ? drawArgs.TreeViewLinkColor.Value : Skin.Grid.TreeViewLineColor);
            float width = (drawArgs.TreeViewLinkMode == TreeViewLinkMode.Line2px ? 2f : 1f);
            System.Drawing.Drawing2D.DashStyle dashStyle = (drawArgs.TreeViewLinkMode == TreeViewLinkMode.Dot ? System.Drawing.Drawing2D.DashStyle.Dot : System.Drawing.Drawing2D.DashStyle.Solid);
            Pen pen = Skin.Pen(color, width, dashStyle: dashStyle);
            return pen;
        }
        /// <summary>
        /// Metoda vykreslí ikonu TreeNode, reaguje na stav Expanded, Mouse, MouseHot a MouseDown.
        /// Metoda vrací absolutní souřadnice ikony, kvůli následnému řešení interaktivity ikony.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="drawArgs"></param>
        /// <returns></returns>
        private void DrawTreeViewIcon(GInteractiveDrawArgs e, TreeViewDrawArgs drawArgs)
        {
            Row row = drawArgs.OwnerCell.Row;
            bool nodeHasChilds = row.TreeNode.HasChilds;
            bool isExpanded = (nodeHasChilds && row.TreeNode.IsExpanded);
            if (!this.HasMouse && !isExpanded) return;

            if (nodeHasChilds)
            {
                Image image = DrawTreeViewGetIcon(isExpanded, drawArgs.IconIsHot, drawArgs.IconIsDown);
                if (image != null)
                {
                    bool rowHasMouse = (row.Control.HasMouse);
                    float opacityRatio = DrawTreeViewGetOpacity(isExpanded, rowHasMouse, drawArgs.IconIsHot, drawArgs.IconIsDown);
                    if (opacityRatio > 0f)
                    {
                        Rectangle cellBounds = drawArgs.CellValueBounds;
                        Rectangle outerBounds = new Rectangle(cellBounds.X, cellBounds.Bottom - 2 - 20, 20, 26);
                        Rectangle imageBounds = new Rectangle(cellBounds.X + 2, outerBounds.Y + 2, 16, 16);
                        Painter.DrawImage(e.Graphics, imageBounds, image, opacityRatio);
                        drawArgs.IconActiveBounds = outerBounds;
                        drawArgs.IconImageBounds = imageBounds;
                    }
                }
            }
        }
        /// <summary>
        /// Vrátí Image pro vykreslení ikony nodu TreeView
        /// </summary>
        /// <param name="nodeIsExpanded"></param>
        /// <param name="iconIsHot"></param>
        /// <param name="iconIsDown"></param>
        /// <returns></returns>
        protected Image DrawTreeViewGetIcon(bool nodeIsExpanded, bool iconIsHot, bool iconIsDown)
        {
            string iconName = null;
            if (!nodeIsExpanded)
            {   // Zavřený uzel:
                if (iconIsDown)
                    // stisknuto   =>  zelená ikona doprava, plná:
                    iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowRight3Png;
                else if (iconIsHot)
                    // s myší      =>  modrá ikona doprava, plná:
                    iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowRightPng;
                else
                    // bez myši    =>  modrá ikona doprava, prázdná:
                    iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowRight2Png;
            }
            else
            {   // Otevřený uzel:
                if (iconIsDown)
                    // stisknuto   =>  zelená ikona dolů, plná:
                    iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowDown3Png;
                else if (iconIsHot)
                    // s myší      =>  modrá ikona dolů, plná:
                    iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowDownPng;
                else
                    // bez myši    =>  modrá ikona dolů, taky plná (nikoli prázdná):
                    // iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowDown2Png;
                    iconName = Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.ArrowDownPng;
            }

            return Application.App.ResourcesApp.GetImage(iconName);
        }
        /// <summary>
        /// Vrátí sytost barvy pro ikonu a daný stav
        /// </summary>
        /// <param name="nodeIsExpanded"></param>
        /// <param name="rowHasMouse"></param>
        /// <param name="iconIsHot"></param>
        /// <param name="iconIsDown"></param>
        /// <returns></returns>
        protected float DrawTreeViewGetOpacity(bool nodeIsExpanded, bool rowHasMouse, bool iconIsHot, bool iconIsDown)
        {
            // (!rowHasMouse ? 0.40f : (!iconIsHot ? 0.80f : 1.00f));
            if (!nodeIsExpanded)
            {   // Zavřený uzel:
                if (iconIsDown) return 1.00f;         // stisknuto        =>  plná barva
                else if (iconIsHot) return 0.80f;     // s myší na ikoně  =>  80%
                else if (rowHasMouse) return 0.70f;   // s myší na řádku  =>  70%;
                return 0.40f;                         // bez myši         =>  40%;
            }
            else
            {   // Otevřený uzel:
                if (iconIsDown) return 1.00f;         // stisknuto        =>  plná barva
                else if (iconIsHot) return 0.90f;     // s myší na ikoně  =>  90%
                else if (rowHasMouse) return 0.80f;   // s myší na řádku  =>  80%;
                return 0.70f;                         // bez myši         =>  70%;
            }
        }
        #endregion
        #region TimeAxis
        /// <summary>
        /// Je voláno z eventhandleru TimeAxis.ValueChange, při/po změně hodnoty Value na některé TimeAxis na sloupci columnId v this.Columns.
        /// Metoda zajistí synchronizaci okolních tabulek (kromě zdejší, ta je v tomto pohledu Master).
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v TÉTO tabulce.
        /// Metoda vyvolá <see cref="GGrid.OnChangeTimeAxis(int?, int, GPropertyChangeArgs{TimeRange})"/>, tím dojde k synchronizaci Value z this tabulky a aktuálního sloupce do okolních tabulek stejného Gridu.
        /// Metoda dále zajistí překreslení všech Cell pro daný sloupec ve všech viditelných řádcích této tabulky.
        /// </summary>
        /// <param name="column">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(Column column, GPropertyChangeArgs<TimeRange> e)
        {
            if (column == null) return;
            this.CallTimeAxisValueChanged(column, e);
            this.RepaintColumn(column);
            if (this.HasGrid && column.UseTimeAxis)
            {   // Já (GTable) zavolám můj GGrid, aby zavolal GridColumn.OnChangeTimeAxis(int?, GPropertyChangeArgs<TimeRange>) pro daný sloupec:
                // Grid dále zajistí aktualizaci synchronizované časové osy (nahoru), a následně i obsluhy jejího eventu o změně (dolů).
                this.Grid.OnChangeTimeAxis(this.TableId, column.ColumnId, e);
            }
        }
        /// <summary>
        /// Je voláno z GGrid, po změně hodnoty synchronizovaného času někde jinde.
        /// Tato tabulka má nastaveno <see cref="UseBackgroundTimeAxis"/> == true, takže by si měla aktualizovat svůj čas podle synchronního, a zajistit překreslení.
        /// </summary>
        /// <param name="e">Zdroj, který změnil hodnotu</param>
        /// <param name="sender">Data o změně</param>
        internal void RefreshBackgroundTimeAxis(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            this.Repaint();
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
            this.CallTimeAxisValueChanged(column, e);
            this.RepaintColumn(column);
        }
        /// <summary>
        /// Obsahuje true, pokud v této tabulce existuje nějaký řádek, který potřebuje časovou osu ke svému vlastnímu zobrazení 
        /// (tj. má nastaveno <see cref="Row.UseBackgroundTimeAxis"/> == true).
        /// </summary>
        internal bool UseBackgroundTimeAxis
        {
            get { return this.DataTable.UseBackgroundTimeAxis; }
        }
        /// <summary>
        /// Metoda vyvolá event TimeAxisValueChanged za daný sloupec a hodnotu.
        /// Event je volán po změně provedené v this tabulce (Master), i v tabulce jiné (kdy se m byla změna promítnuta jako do Slave tabulky)
        /// </summary>
        /// <param name="column"></param>
        /// <param name="e"></param>
        protected void CallTimeAxisValueChanged(Column column, GPropertyChangeArgs<TimeRange> e)
        {
            this.OnTimeAxisValueChanged(column, e);
            if (this.TimeAxisValueChanged != null)
                this.TimeAxisValueChanged(column, e);
        }
        /// <summary>
        /// Háček volaný po změně času na časové ose v daném sloupci
        /// </summary>
        /// <param name="column"></param>
        /// <param name="e"></param>
        protected virtual void OnTimeAxisValueChanged(Column column, GPropertyChangeArgs<TimeRange> e) { }
        /// <summary>
        /// Event volaný po změně času na časové ose v daném sloupci
        /// </summary>
        public event GPropertyChangedHandler<TimeRange> TimeAxisValueChanged;
        #endregion
        #region Interaktivita vlastní GTable
        /// <summary>
        /// Řeší interaktivitu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {   // Když už tady musí být override AfterStateChanged() (to kdyby to někoho napadlo), tak MUSÍ volat base metodu!
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardFocusEnter:
                case GInteractiveChangeState.KeyboardFocusLeave:
                    break;
                case GInteractiveChangeState.WheelUp:
                case GInteractiveChangeState.WheelDown:
                    InteractivePositionAction action = (e.ChangeState == GInteractiveChangeState.WheelUp ? InteractivePositionAction.WheelUp : InteractivePositionAction.WheelDown);
                    e.ActionIsSolved = this.ProcessRowAction(action);
                    break;
                case GInteractiveChangeState.KeyboardKeyPreview:           // Sem chodí i klávesy Kurzor, Tab
                    this.KeyboardPreviewKeyDown(e);        // Pokud se ani Cell, a ani Row nepřihlásí ke zpracování Keyboard událostí, musí to provést Table.
                    break;
                case GInteractiveChangeState.KeyboardKeyUp:
                    this.KeyboardKeyUp(e);
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
            {
                bool isProcessed = this.ProcessRowAction(action);
                e.KeyboardPreviewArgs.IsInputKey = isProcessed;
                e.ActionIsSolved = isProcessed;
            }
        }
        /// <summary>
        /// Obsluha standardní klávesy
        /// </summary>
        /// <param name="e"></param>
        private void KeyboardKeyUp(GInteractiveChangeStateArgs e)
        {
            this.CallKeyboardKeyUp(this.ActiveRow, this.ActiveCell, e);
        }
        /// <summary>
        /// Jakmile myš opouští tabulku, pak resetuje informaci o HotRow a HotCell:
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            if (this._HotCell != null)
                ((IGTable)this).CellMouseLeave(e, null);
            if (this._HotRow != null)
                this.SetHotRow(null, EventSourceType.InteractiveChanged);
        }
        /// <summary>
        /// Po opuštění Focusu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
        }
        #endregion
        #region Drag and Move řádků tabulky (voláno z GRowHeader a GCell)
        /// <summary>
        /// Řídí proces Drag and Move pro přemístění řádku, všechny fáze (viz argument e, <see cref="GDragActionArgs.DragAction"/>)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="currentRow"></param>
        /// <param name="targetRelativeBounds"></param>
        protected void RowDragMoveAction(GDragActionArgs e, Row currentRow, Rectangle? targetRelativeBounds)
        {
            switch (e.DragAction)
            {
                case DragActionType.DragThisStart:
                    this.RowDragActiveRows = this.RowDragGetRows(e, currentRow);
                    this.RowDragTableText = this.GetTableText(this.RowDragActiveRows);
                    break;
                case DragActionType.DragThisMove:
                    this.RowDragMousePointAbsolute = e.MouseCurrentAbsolutePoint;
                    this.RowDragMouseState = this.RowDragGetCurrentState(e);
                    break;
                case DragActionType.DragThisDrop:
                    this.RowDragCallDropEvent(e, this.RowDragMouseState);
                    this.RowDragMoveClear(e);
                    break;
                case DragActionType.DragThisEnd:
                    this.RowDragMoveClear(e);
                    break;
            }
        }
        /// <summary>
        /// Vynuluje data používaní v proces Drag and Move pro řádky.
        /// </summary>
        protected void RowDragMoveClear(GDragActionArgs e)
        {
            e.DragActiveItem = null;
            this.RowDragActiveRows = null;
            this.RowDragTableText = null;
            this.RowDragMousePointAbsolute = null;
            this.RowDragMouseState = null;
            this.RowDragCurrentTarget = null;
        }
        /// <summary>
        /// Metoda najde a vrátí pole řádků, jichž se týká proces Drag and Move řádků, podle konfigurace.
        /// Může vrátit pole s počtem 0 řádků.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="currentRow"></param>
        /// <returns></returns>
        protected Row[] RowDragGetRows(GDragActionArgs e, Row currentRow)
        {
            TableRowDragMoveSourceMode mode = this.DataTable.RowDragMoveSourceMode;
            Row[] rows = null;
            switch (mode)
            {
                case TableRowDragMoveSourceMode.None:
                    break;
                case TableRowDragMoveSourceMode.OnlyActiveRow:
                    if (currentRow != null)
                        rows = new Row[] { currentRow };
                    break;
                case TableRowDragMoveSourceMode.ActivePlusSelectedRows:
                    rows = this.Rows.Where(r => r.IsChecked || (currentRow != null && r.RowId == currentRow.RowId)).ToArray();
                    break;
                case TableRowDragMoveSourceMode.SelectedThenActiveRow:
                    rows = this.Rows.Where(r => r.IsChecked).ToArray();
                    if (rows.Length == 0)
                        rows = new Row[] { currentRow };
                    break;
                case TableRowDragMoveSourceMode.OnlySelectedRows:
                    rows = this.Rows.Where(r => r.IsChecked).ToArray();
                    break;
            }

            // Vyvoláme případnou aplikační logiku (event navázaný v DataTable), který řeší filtrování řádků vybraných pro RowDrag:
            TableRowDragMoveArgs args = new TableRowDragMoveArgs(e, rows);
            this.CallTableRowDragStart(args);
            rows = (args.DragRows != null ? args.DragRows.ToArray() : null);     // Převezmeme buď naše výchozí pole, anebo pole modifikované aplikací

            return rows;
        }
        /// <summary>
        /// Metoda sestaví a vrátí new instanci <see cref="TableTextRow"/>, která bude obsahovat data z this tabulky pro dané řádky.
        /// Pokud je na vstupu prázdné pole, vrací null.
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected TableText GetTableText(Row[] rows)
        {
            if (rows == null || rows.Length == 0) return null;

            TableText tableText = new TableText();

            Column[] columns = this.Columns.Where(c => !c.UseTimeAxis).ToArray();        // Sloupce zdrojové tabulky vyjma sloupec s grafem
            TableTextRow titleRow = new TableTextRow();
            titleRow.Font = FontInfo.DefaultBold;
            foreach (var column in columns)
            {
                TableTextCell textCell = new TableTextCell(column.Title, ContentAlignment.MiddleCenter, column.ColumnHeader.Bounds.Width);
                titleRow.Cells.Add(textCell);
            }
            tableText.Rows.Add(titleRow);

            foreach (Row row in rows)
                GetTableTextAddRow(tableText, columns, row);

            return tableText;
        }
        /// <summary>
        /// Do dané tabulky textů <see cref="TableText"/> přidá další řádek, obsahující data za dané sloupce z daného datového řádku.
        /// </summary>
        /// <param name="tableText"></param>
        /// <param name="columns"></param>
        /// <param name="row"></param>
        protected static void GetTableTextAddRow(TableText tableText, Column[] columns, Row row)
        {
            TableTextRow textRow = new TableTextRow();
            foreach (var column in columns)
            {
                Cell dataCell = row[column];
                TableTextCell textCell = new TableTextCell(dataCell.Text, column.Alignment, null);
                textRow.Cells.Add(textCell);
            }
            tableText.Rows.Add(textRow);
        }
        /// <summary>
        /// Metoda určí, zda je Drag and Move aktuálních řádků this tabulky do určitého místa povolené.
        /// Využívá k tomu event a aplikační logiku.
        /// Pokud pole <see cref="RowDragActiveRows"/> je prázdné, nic nedělá a vrací null.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected TableRowDragMoveArgs RowDragGetCurrentState(GDragActionArgs e)
        {
            if (this.RowDragActiveRows == null || this.RowDragActiveRows.Length == 0) return null;

            // Vyvoláme případnou aplikační logiku (event navázaný v DataTable), který řeší povolení pro Drop na daném cíli:
            TableRowDragMoveArgs args = new TableRowDragMoveArgs(e, this.RowDragActiveRows);
            this.CallTableRowDragMove(args);

            // Zajistíme aktivaci Target prvku, podle hodnoty args.TargetEnabled:
            if (args.TargetEnabled)
                e.DragActiveItem = args.ActiveItem;

            return args;
        }
        /// <summary>
        /// Metoda vyvolá událost <see cref="CallTableRowDragDrop(TableRowDragMoveArgs)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="args"></param>
        protected void RowDragCallDropEvent(GDragActionArgs e, TableRowDragMoveArgs args)
        {
            if (args != null)
                this.CallTableRowDragDrop(args);
        }
        /// <summary>
        /// Provádí vykreslení přesouvaného řádku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void RowDragMoveDraw(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            TableText tableText = this.RowDragTableText;
            if (tableText == null) return;
            if (tableText.NeedMeasure) tableText.TextMeasure(e.Graphics, false);

            Point point = (this.RowDragMousePointAbsolute.HasValue ? this.RowDragMousePointAbsolute.Value : System.Windows.Forms.Control.MousePosition).Add(-15, 8);
            Size size = tableText.CurrentSize.Value.Min(500, 350);
            Rectangle bounds = new Rectangle(point, size);

            bool targetEnabled = (this.RowDragMouseState != null ? this.RowDragMouseState.TargetEnabled : false);

            Color backColor = (targetEnabled ? Color.FromArgb(230, 180, 240, 180) : Color.FromArgb(110, 160, 160, 160));
            Color titleColor = (targetEnabled ? Color.FromArgb(230, 180, 200, 250) : Color.FromArgb(110, 160, 160, 160));
            Color borderColor = (targetEnabled ? Color.FromArgb(230, 130, 130, 20) : Color.FromArgb(170, 60, 60, 60));

            tableText.Rows[0].BackColor = titleColor;
            tableText.Rows[0].BackEffect3D = (targetEnabled ? 0.25f : 0.10f);
            tableText.BorderColor = borderColor;

            Painter.DrawTableText(e.Graphics, bounds, tableText, backColor);
        }
        /// <summary>
        /// Řádky, jichž se týká aktuální Drag and Move řádků
        /// </summary>
        protected Row[] RowDragActiveRows { get; private set; }
        /// <summary>
        /// Texty používané při Drag and Move, obsahují data ze zdrojové tabulky
        /// </summary>
        protected TableText RowDragTableText { get; private set; }
        /// <summary>
        /// Data o procesu RowDragMove po poslední změně pozice Target
        /// </summary>
        protected TableRowDragMoveArgs RowDragMouseState { get; private set; }
        /// <summary>
        /// Aktuální objekt Target, pokud je Enabled
        /// </summary>
        protected IInteractiveItem RowDragCurrentTarget { get; private set; }
        /// <summary>
        /// Souřadnice myši absolutní při jejím pohybu Drag and Move
        /// </summary>
        protected Point? RowDragMousePointAbsolute { get; private set; }
        #endregion
        #region Interaktivita z jednotlivých objektů tabulky do grafické tabulky, a dále
        /// <summary>
        /// Funkce, kterou aktuálně má objekt TableHeader
        /// </summary>
        TableHeaderFunctionType IGTable.TableHeaderFunction { get { return this.TableHeaderFunction; } }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví tabulky.
        /// </summary>
        /// <param name="e"></param>
        void IGTable.TableHeaderClick(GInteractiveChangeStateArgs e)
        {
            this.TableHeaderClick(e);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví sloupce.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="column"></param>
        void IGTable.ColumnHeaderClick(GInteractiveChangeStateArgs e, Column column)
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
        /// Provede se poté, kdy uživatel klikne pravou (=kontextovou) myší na záhlaví sloupce.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="column"></param>
        void IGTable.ColumnHeaderContextMenu(GInteractiveChangeStateArgs e, Column column)
        {
            this.ColumnContextMenu(e, column);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví řádku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="row">řádek</param>
        void IGTable.RowHeaderClick(GInteractiveChangeStateArgs e, Row row)
        {
            if (row != null && row.Table.AllowRowCheckedByClick)
            {
                row.Table.RowHeaderClick(row);
                row.CheckedChange();
                this.Repaint();
            }
        }
        /// <summary>
        /// Provede se poté, kdy uživatel vstoupí s myší nad určitou buňkou.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void IGTable.CellMouseEnter(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetHotCell(cell, EventSourceType.InteractiveChanged);
            this.CallCellMouseEnter(cell, e);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel vystoupí myší z určité buňky jinam.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void IGTable.CellMouseLeave(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetHotCell(null, EventSourceType.InteractiveChanged);
            this.CallCellMouseLeave(cell, e);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void IGTable.CellClick(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetActiveCell(cell, EventSourceType.InteractiveChanged, true);
            this.CallActiveCellClick(cell, e);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel povede DoubleClick na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void IGTable.CellDoubleClick(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetActiveCell(cell, EventSourceType.InteractiveChanged, true);
            this.CallActiveCellDoubleClick(cell, e);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel povede LongClick na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void IGTable.CellLongClick(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetActiveCell(cell, EventSourceType.InteractiveChanged, true);
            this.CallActiveCellLongClick(cell, e);
        }
        /// <summary>
        /// Provede se poté, kdy uživatel povede RightClick na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void IGTable.CellRightClick(GInteractiveChangeStateArgs e, Cell cell)
        {
            this.SetActiveCell(cell, EventSourceType.InteractiveChanged, true);
            this.CallActiveCellRightClick(cell, e);
        }
        /// <summary>
        /// Řídí proces Drag and Move pro přemístění řádku, všechny fáze (viz argument e, <see cref="GDragActionArgs.DragAction"/>)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="currentRow"></param>
        /// <param name="targetRelativeBounds"></param>
        void IGTable.RowDragMoveAction(GDragActionArgs e, Row currentRow, Rectangle? targetRelativeBounds)
        {
            this.RowDragMoveAction(e, currentRow, targetRelativeBounds);
        }
        /// <summary>
        /// Provádí vykreslení přesouvaného řádku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        void IGTable.RowDragMoveDraw(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.RowDragMoveDraw(e, boundsAbsolute);
        }
        #endregion
        #region Draw : kreslení vlastní tabulky
        /// <summary>
        /// Vykreslí tabulku
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            this.GraphicClip(e, absoluteBounds, absoluteVisibleBounds);
            base.Draw(e, absoluteBounds, absoluteVisibleBounds);
            // Všechno ostatní (záhlaví sloupců, řádky, scrollbary, splittery) si malují Childs samy.
        }
        /// <summary>
        /// Ořízne grafiku na cílový prostor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <returns></returns>
        protected bool GraphicClip(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            // Ořezáváme jen při kreslení do vrstvy Standard, jinak ne:
            if (e.DrawLayer != GInteractiveDrawLayer.Standard) return true;

            // Prostor pro oblast tabulky, se zohledněním souřadnic určených pro prostor tabulek v rámci Gridu:
            Rectangle areaAbsoluteBounds = this.GetAbsoluteBoundsForArea(TableAreaType.Table, true);

            // Prostor pro aktuální prvek = intersect se souřadnicemi prvku:
            Rectangle controlBounds = Rectangle.Intersect(areaAbsoluteBounds, absoluteBounds);
            if (!controlBounds.HasPixels()) return false;

            // Prostor po oříznutí s aktuálním Clipem v grafice:
            //  Aktuální Clip v grafice obsahuje prostor, daný pro tento prvek v rámci jeho parentů:
            e.GraphicsClipWith(controlBounds, true);

            // Pokud aktuální Clip je viditelný, pak jeho hodnota určuje souřadnice, kde je prvek interaktivní:
            bool isVisible = e.HasVisibleGraphics;

            return isVisible;
        }
        #endregion
        #region Draw : podpora pro kreslení obsahu řádků (styl, pozadí, gridlines, hodnota)
        /// <summary>
        /// Metoda zajistí vykreslení pasivního obsahu dané buňky nebo řádku daného typu.
        /// Aktivní obsah (v současné době <see cref="Graphs.ITimeInteractiveGraph"/>) se vykresluje automaticky jako Child prvek své buňky / řádku.
        /// Zdejší metoda pro něj pouze vykreslí pozadí řádku pod grafem.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <param name="row"></param>
        /// <param name="cell">Buňka tabulky, anebo null pokud se kreslí pozadí pod řádkem</param>
        internal void DrawValue(GInteractiveDrawArgs e, Rectangle boundsAbsolute, object value, TableValueType valueType, Row row, Cell cell)
        {
            this.GVisualStyle.PrepareFor(row, cell);
            this.DrawBackground(e, boundsAbsolute, row, cell);
            switch (valueType)
            {
                case TableValueType.Null:
                    this.DrawNull(e, boundsAbsolute, row, cell);
                    break;
                case TableValueType.IDrawItem:
                    this.DrawIDrawItem(e, boundsAbsolute, row, cell, value as IDrawItem);
                    break;
                case TableValueType.ITimeInteractiveGraph:
                    this.DrawContentInteractiveTimeGraph(e, boundsAbsolute, row, cell, value as Components.Graphs.ITimeInteractiveGraph);
                    break;
                case TableValueType.ITimeGraph:
                    this.DrawContentTimeGraph(e, boundsAbsolute, row, cell, value as Components.Graphs.ITimeGraph);
                    break;
                case TableValueType.Image:
                    this.DrawContentImage(e, boundsAbsolute, row, cell, value as Image);
                    break;
                case TableValueType.TextRelation:
                case TableValueType.Text:
                    this.DrawContentText(e, boundsAbsolute, row, cell, value, valueType);
                    break;
            }
        }
        /// <summary>
        /// Metoda vyplní pozadí dané plochy barvou pozadí řádku.
        /// Tato metoda nekreslí žádný obsah, pouze barvu.
        /// Tato metoda proběhne pouze tehdy, když parametr "row" není null, a parametr "cell" je null, tedy při kreslení celého řádku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        private void DrawBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell)
        {
            if (row != null && cell == null)
                this.DrawRowBackColor(e, boundsAbsolute, row, cell);
        }
        /// <summary>
        /// Metoda vykreslí pozadí (background) pro danou buňku jednoho řádku.
        /// Metoda nekreslí GridLines.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        private void DrawRowBackColor(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell)
        {
            float? effect3d = null;
            Color backColor = this._GetBackColor(row, cell, ref effect3d);
            Painter.DrawEffect3D(e.Graphics, boundsAbsolute, backColor, System.Windows.Forms.Orientation.Horizontal, effect3d);
        }
        /// <summary>
        /// Vykreslí prázdnou buňku / řádek (jen pozadí)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        private void DrawNull(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell)
        { }
        /// <summary>
        /// Vykreslí obsah this buňky jako text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        private void DrawContentText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, object value, TableValueType valueType)
        {
            if (row == null || cell == null) return;

            // Nejprve podtržení, pokud sloupec zobrazuje vztah:
            if (cell.IsRelation)
                this.DrawContentRelation(e, boundsAbsolute, row, cell, value);

            // Obsah a zarovnání řádku:
            string formatString = cell.Column.FormatString;
            ContentAlignment? columnAlignment = cell.Column.Alignment;
            ContentAlignment defaultAlignment;
            string text = GetText(value, valueType, formatString, out defaultAlignment);
            if (cell.Column.Alignment.HasValue) defaultAlignment = cell.Column.Alignment.Value;
            ContentAlignment alignment = (columnAlignment ?? defaultAlignment);

            // Font, barva textu:
            FontInfo font = this.GVisualStyle.CurrentFontInfo;
            Color textColor = this._GetTextColor(row, cell);

            Rectangle boundsContent = boundsAbsolute.Enlarge(-1);
            Painter.DrawString(e.Graphics, text, font, boundsContent, alignment, textColor);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako Image
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="image"></param>
        private void DrawContentImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, Image image)
        {
            if (image == null) return;
            Size size = image.Size;
            Rectangle imageBounds = size.AlignTo(boundsAbsolute, ContentAlignment.MiddleCenter, true, true);
            if (imageBounds.Width > 4 && imageBounds.Height > 4)
                e.Graphics.DrawImage(image, imageBounds);
        }
        /// <summary>
        /// Vykreslí obsah this buňky pomocí její vlastní metody IDrawItem.Draw()
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="drawItem"></param>
        private void DrawIDrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, IDrawItem drawItem)
        {
            try { drawItem.Draw(e, boundsAbsolute); }
            catch { }
        }
        /// <summary>
        /// Metoda vykreslí podtržení buňky, jako naznačení že sloupec zobrazuje vztah
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="value"></param>
        private void DrawContentRelation(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, object value)
        {
            Painter.DrawRelationLine(e.Graphics, boundsAbsolute, forGrid: true);
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako interaktivní časový graf
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="graph"></param>
        private void DrawContentInteractiveTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, Components.Graphs.ITimeInteractiveGraph graph)
        {
            this.DrawRowBackColor(e, boundsAbsolute, row, cell);     // Co s pozadím pod grafem?

            if (graph.TimeAxisConvertor == null)
                graph.TimeAxisConvertor = this.GetTimeAxisConvertor(cell);
            if (graph.Parent == null)
                graph.Parent = this.GetInteractiveParent(row, cell);

            // Graf se nevykresluje jako "obrázek" v rámci buňky/řádku, protože graf (reps. jeho Items) se vykresluje sám protože je Child prvkem své buňky nebo řádku...
        }
        /// <summary>
        /// Vykreslí obsah this buňky jako časový graf
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <param name="graph"></param>
        private void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Row row, Cell cell, Components.Graphs.ITimeGraph graph)
        {
            this.DrawRowBackColor(e, boundsAbsolute, row, cell);     // Co s pozadím pod grafem?

            if (graph.TimeConvertor == null)
                graph.TimeConvertor = this.GetTimeAxisConvertor(cell);

            graph.DrawContentTimeGraph(e, boundsAbsolute);
        }
        /// <summary>
        /// Převede danou hodnotu (obsah buňky) na string s využitím formátovacího řetězce, a podle konkrétního datového typu určí výchozí zarovnání.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <param name="formatString"></param>
        /// <param name="defaultAlignment"></param>
        /// <returns></returns>
        internal static string GetText(object value, TableValueType valueType, string formatString, out ContentAlignment defaultAlignment)
        {
            defaultAlignment = ContentAlignment.MiddleLeft;
            if (value == null) return "";

            if (valueType == TableValueType.TextRelation) return (value as Noris.LCS.Base.WorkScheduler.GuiIdText).Text;

            bool hasFormatString = (!String.IsNullOrEmpty(formatString));

            if (value is DateTime)
            {
                DateTime valueDT = (DateTime)value;
                defaultAlignment = ContentAlignment.MiddleCenter;
                if (hasFormatString) return valueDT.ToString(formatString);
                return valueDT.ToString();
            }

            if (value is Int32)
            {
                Int32 valueInt32 = (Int32)value;
                defaultAlignment = ContentAlignment.MiddleRight;
                if (hasFormatString) return valueInt32.ToString(formatString);
                return valueInt32.ToString();
            }

            if (value is Decimal)
            {
                Decimal valueDecimal = (Decimal)value;
                defaultAlignment = ContentAlignment.MiddleRight;
                if (hasFormatString) return valueDecimal.ToString(formatString);
                return valueDecimal.ToString();
            }
            defaultAlignment = ContentAlignment.MiddleLeft;

            return value.ToString();
        }
        /// <summary>
        /// Metoda vrátí konvertor dat (časovou osu) pro konverze souřadnic v prvcích typu TimeGraph
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        internal ITimeAxisConvertor GetTimeAxisConvertor(Cell cell)
        {
            if (cell != null && cell.Column != null && cell.Column.ColumnHeader != null && cell.Column.ColumnHeader.TimeConvertor != null)
                return cell.Column.ColumnHeader.TimeConvertor;
            if (this.HasGrid)
                return this.Grid.SynchronizedTimeConvertor;
            return null;
        }
        /// <summary>
        /// Metoda vrátí objekt IInteractiveParent z dodaného řádku a buňky (buňka má přednost, pokud je zadaná)
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        internal IInteractiveParent GetInteractiveParent(Row row, Cell cell)
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
        /// Metoda vykreslí linky ohraničující danou buňku jednoho řádku.
        /// Vykresluje se v podstatě jen dolní linka (jako Horizontal) a linka vpravo (Vertical).
        /// Horní a levá linka se nekreslí, protože u prvního řádku / sloupce postačí Header, a u dalších řádků / sloupců je vykreslená linka z předešlého řádku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell"></param>
        /// <param name="boundsAbsolute"></param>
        internal void DrawRowGridLines(GInteractiveDrawArgs e, Cell cell, Rectangle boundsAbsolute)
        {
            RectangleSide side = GetSidesFromLines(this.GVisualStyle.GridLines);
            Color horizontalColor = this.GVisualStyle.HorizontalLineColor;
            Color verticalColor = this.GVisualStyle.VerticalLineColor;
            Painter.DrawBorder(e.Graphics, boundsAbsolute, side, null, null, verticalColor, horizontalColor, null);
            if (cell.Row.IsActive)
            {
                Rectangle boundsActive = boundsAbsolute.Enlarge(0, 0, 0, -1);
                Color topColor = Skin.Modifiers.GetColor3DBorderDark(horizontalColor);
                Color bottomColor = Skin.Modifiers.GetColor3DBorderLight(horizontalColor);
                Painter.DrawBorder(e.Graphics, boundsActive, RectangleSide.Top | RectangleSide.Bottom, null, topColor, null, bottomColor, null);
            }
        }
        /// <summary>
        /// Vrátí strany z typu borderu
        /// </summary>
        /// <param name="gridLines"></param>
        /// <returns></returns>
        protected static RectangleSide GetSidesFromLines(GuiBorderSideType gridLines)
        {
            RectangleSide side = RectangleSide.None;

            if (gridLines.HasFlag(GuiBorderSideType.Left)) side |= RectangleSide.Left;
            if (gridLines.HasFlag(GuiBorderSideType.Top)) side |= RectangleSide.Top;
            if (gridLines.HasFlag(GuiBorderSideType.Right)) side |= RectangleSide.Right;
            if (gridLines.HasFlag(GuiBorderSideType.Bottom)) side |= RectangleSide.Bottom;

            return side;
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

            this.GVisualStyle.PrepareFor(row, cell);
            return this._GetBackColor(row, cell, ref effect3D);
        }
        /// <summary>
        /// Vrátí aktuální barvu pozadí.
        /// Před voláním této metody je třeba připravit vizuální styl v <see cref="GVisualStyle"/>, voláním <see cref="GTableVisualStyle.PrepareFor(Row, Cell)"/>.
        /// </summary>
        /// <param name="row">Řádek</param>
        /// <param name="cell">Buňka</param>
        /// <returns></returns>
        private Color _GetBackColor(Row row, Cell cell)
        {
            float? effect3d = null;
            return this._GetBackColor(row, cell, ref effect3d);
        }
        /// <summary>
        /// Vrátí aktuální barvu pozadí.
        /// Před voláním této metody je třeba připravit vizuální styl v <see cref="GVisualStyle"/>, voláním <see cref="GTableVisualStyle.PrepareFor(Row, Cell)"/>.
        /// </summary>
        /// <param name="row">Řádek</param>
        /// <param name="cell">Buňka</param>
        /// <param name="effect3D"></param>
        /// <returns></returns>
        private Color _GetBackColor(Row row, Cell cell, ref float? effect3D)
        {
            // Základní barva pozadí, daná stavem řádku Active, Selected, Root:
            Color backColor = this.GVisualStyle.CurrentBackColor;

            // Základní barva je poté morfována do barvy Active v poměru, který vyjadřuje aktivitu řádku, buňky, a focus tabulky, a stav HotMouse:
            float ratio = this.GetMorphRatio(row, cell, ref effect3D);

            // Pokud prvek není aktivní (aktivní řádek ani aktivní buňka), pak má základní barvu - bez morphování:
            if (ratio == 0f) return backColor;

            // Pokud je aktuální prvek v nějakém aktivním stavu (má kladné ratio pro morfing barvy):
            return backColor.Morph(this.GVisualStyle.ActiveBackColor, ratio);
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

            this.GVisualStyle.PrepareFor(row, cell);
            return this._GetTextColor(row, cell);
        }
        /// <summary>
        /// Vrátí aktuální barvu textu.
        /// Před voláním této metody je třeba připravit vizuální styl v <see cref="GVisualStyle"/>, voláním <see cref="GTableVisualStyle.PrepareFor(Row, Cell)"/>.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private Color _GetTextColor(Row row, Cell cell)
        {
            // Základní barva pozadí, daná stavem řádku Active, Selected, Root:
            Color textColor = this.GVisualStyle.CurrentTextColor;

            // Základní barva je poté morfována do barvy Active v poměru, který vyjadřuje aktivitu řádku, buňky, a focus tabulky, a stav HotMouse:
            float ratio = this.GetMorphRatio(row, cell);

            // Pokud prvek není aktivní (aktivní řádek ani aktivní buňka), pak má základní barvu - bez morphování:
            if (ratio == 0f) return textColor;

            // Pokud je aktuální prvek v nějakém aktivním stavu (má kladné ratio pro morfing barvy):
            return textColor.Morph(this.GVisualStyle.ActiveTextColor, ratio);
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
        /// <summary>Konstanta pro Morphing</summary>
        protected const float MORPH_RATIO_ACTIVE_CELL = 0.80f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float MORPH_RATIO_ACTIVE_ROW = 0.65f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float MORPH_RATIO_MOUSEHOT_CELL = 0.25f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float MORPH_RATIO_MOUSEHOT_ROW = 0.10f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float MORPH_RATIO_ACTIVE_NOFOCUS = 0.25f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float MORPH_RATIO_MOUSEHOT_NOFOCUS = 0.05f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float EFFECT_3D_ACTIVE_ROW = 0.45f;
        /// <summary>Konstanta pro Morphing</summary>
        protected const float EFFECT_3D_MOUSEHOT_ROW = 0.30f;
        /// <summary>
        /// Adapter na vizuální styl kreslení konkrétní buňky/řádku/sloupce
        /// </summary>
        internal GTableVisualStyle GVisualStyle
        {
            get
            {
                if (this._GVisualStyle == null)
                    this._GVisualStyle = new GTableVisualStyle(this);
                return this._GVisualStyle;
            }
        }
        private GTableVisualStyle _GVisualStyle;
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
        /// <summary>
        /// Vyvolá událost CallActiveRowChanged
        /// </summary>
        /// <param name="oldActiveRow"></param>
        /// <param name="newActiveRow"></param>
        /// <param name="eventSource"></param>
        protected void CallActiveRowChanged(Row oldActiveRow, Row newActiveRow, EventSourceType eventSource)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallActiveRowChanged(oldActiveRow, newActiveRow, eventSource, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallKeyboardKeyUp
        /// </summary>
        /// <param name="activeRow"></param>
        /// <param name="activeCell"></param>
        /// <param name="e"></param>
        protected void CallKeyboardKeyUp(Row activeRow, Cell activeCell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallKeyboardKeyUp(activeRow, activeCell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallHotRowChanged
        /// </summary>
        /// <param name="oldHotRow"></param>
        /// <param name="newHotRow"></param>
        /// <param name="eventSource"></param>
        protected void CallHotRowChanged(Row oldHotRow, Row newHotRow, EventSourceType eventSource)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallHotRowChanged(oldHotRow, newHotRow, eventSource, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallHotCellChanged
        /// </summary>
        /// <param name="oldHotCell"></param>
        /// <param name="newHotCell"></param>
        /// <param name="eventSource"></param>
        protected void CallHotCellChanged(Cell oldHotCell, Cell newHotCell, EventSourceType eventSource)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallHotCellChanged(oldHotCell, newHotCell, eventSource, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallActiveCellChanged
        /// </summary>
        /// <param name="oldActiveCell"></param>
        /// <param name="newActiveCell"></param>
        /// <param name="eventSource"></param>
        protected void CallActiveCellChanged(Cell oldActiveCell, Cell newActiveCell, EventSourceType eventSource)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallActiveCellChanged(oldActiveCell, oldActiveCell, eventSource, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallCellMouseEnter
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        protected void CallCellMouseEnter(Cell cell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallCellMouseEnter(cell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallCellMouseLeave
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        protected void CallCellMouseLeave(Cell cell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallCellMouseLeave(cell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallActiveCellClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        protected void CallActiveCellClick(Cell cell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallActiveCellClick(cell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallActiveCellDoubleClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        protected void CallActiveCellDoubleClick(Cell cell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallActiveCellDoubleClick(cell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallActiveCellLongClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        protected void CallActiveCellLongClick(Cell cell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallActiveCellLongClick(cell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost CallActiveCellRightClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        protected void CallActiveCellRightClick(Cell cell, GInteractiveChangeStateArgs e)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallActiveCellRightClick(cell, e, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost RowDragStart = na začátku přemísťování řádků tabulky pomocí myši
        /// </summary>
        /// <param name="args"></param>
        protected void CallTableRowDragStart(TableRowDragMoveArgs args)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallTableRowDragStart(args, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost RowDragMove = v průběhu přemísťování řádků tabulky pomocí myši
        /// </summary>
        /// <param name="args"></param>
        protected void CallTableRowDragMove(TableRowDragMoveArgs args)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallTableRowDragMove(args, !this.IsSuppressedEvent);
        }
        /// <summary>
        /// Vyvolá událost RowDragDrop = při ukončení přemísťování řádků tabulky pomocí myši
        /// </summary>
        /// <param name="args"></param>
        protected void CallTableRowDragDrop(TableRowDragMoveArgs args)
        {
            ITableInternal target = (this.DataTable as ITableInternal);
            if (target != null)
                target.CallTableRowDragDrop(args, !this.IsSuppressedEvent);
        }
        #endregion
    }
    #region class GTableVisualStyle : adapter na styl kreslení pro konkrétní buňku/řádek v rámci tabulky
    /// <summary>
    /// GTableVisualStyle : adapter na styl kreslení pro konkrétní buňku/řádek v rámci tabulky
    /// </summary>
    internal class GTableVisualStyle
    {
        #region Konstruktor a věci trvalého charakteru (tabulka a implicitní styl)
        /// <summary>
        /// Konstruktor pro tabulku
        /// </summary>
        /// <param name="gTable"></param>
        public GTableVisualStyle(GTable gTable)
        {
            this.GTable = gTable;
            this.ImplicitStyle = Data.Table.ImplicitStyle;
        }
        /// <summary>
        /// Tabulka, v jejímž rámci se kreslí
        /// </summary>
        protected GTable GTable { get; private set; }
        /// <summary>
        /// Datová tabulka, zdroj dat a stylů
        /// </summary>
        protected Table DataTable { get { return this.GTable.DataTable; } }
        /// <summary>
        /// Defaultní hodnoty, když jiné nebudou definovány.
        /// V této property jsou vyplněny všechny hodnoty, žádná není null.
        /// Použijí se tehdy, když konkrétnější styly nejsou zadány.
        /// </summary>
        protected GuiVisualStyle ImplicitStyle { get; private set; }
        #endregion
        #region Styly připravené ad-hoc pro kreslení jedné buňky
        /// <summary>
        /// Metoda si připraví hodnoty pro následné kreslení v rámci daného řádku a buňky.
        /// Buňka může být null, řádek by neměl být null.
        /// Metoda do sebe vloží styly odpovídající definici.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        internal void PrepareFor(Row row, Cell cell)
        {
            Column column = cell?.Column;

            this.TableDefaultStyle = this.DataTable.DefaultVisualStyle;
            this.TableDefaultChildStyle = this.DataTable.DefaultChildVisualStyle;
            this.RowStyle = (row != null ? this.GetVisualStyle(row.Style, row.StyleName) : null);
            this.RowIsActive = (row != null ? row.IsActive : false);
            this.RowIsChild = (row != null ? row.TreeNode.IsChild : false);
            this.RowIsChecked = (row != null ? row.IsChecked : false);
            this.ColumnStyle = (column != null ? this.GetVisualStyle(column.Style, column.StyleName) : null);
            this.CellStyle = (cell != null ? this.GetVisualStyle(cell.Style, cell.StyleName) : null);
        }
        /// <summary>
        /// Vrátí dodaný explicitní styl, anebo styl odpovídající danému jménu
        /// </summary>
        /// <param name="style"></param>
        /// <param name="styleName"></param>
        /// <returns></returns>
        protected GuiVisualStyle GetVisualStyle(GuiVisualStyle style, string styleName)
        {
            if (style != null) return style;
            if (!String.IsNullOrEmpty(styleName)) return this.DataTable.GetVisualStyle(styleName);
            return null;
        }
        /// <summary>
        /// Tabulka: Vizuální styl základní
        /// <see cref="Table.DefaultVisualStyle"/>
        /// </summary>
        protected GuiVisualStyle TableDefaultStyle { get; private set; }
        /// <summary>
        /// Tabulka: Vizuální styl základní pro Child řádky
        /// <see cref="Table.DefaultChildVisualStyle"/>
        /// </summary>
        protected GuiVisualStyle TableDefaultChildStyle { get; private set; }
        /// <summary>
        /// Řádek: styl definovaný na účovni řádku
        /// </summary>
        protected GuiVisualStyle RowStyle { get; private set; }
        /// <summary>
        /// true pokud řádek je Aktivním řádkem, false pro ostatná řádky
        /// </summary>
        protected bool RowIsActive { get; private set; }
        /// <summary>
        /// true pokud řádek je Child řádkem, false pro Root řádek
        /// </summary>
        protected bool RowIsChild { get; private set; }
        /// <summary>
        /// true pokud řádek je IsChecked, false pro nezaškrtnutý řádek
        /// </summary>
        protected bool RowIsChecked { get; private set; }
        /// <summary>
        /// Sloupec: styl definovaný na účovni sloupce
        /// </summary>
        protected GuiVisualStyle ColumnStyle { get; private set; }
        /// <summary>
        /// Buňka: styl definovaný na účovni buňky
        /// </summary>
        protected GuiVisualStyle CellStyle { get; private set; }
        #endregion
        #region Hodnoty konkrétních vizuálních vlastností, získané z vhodného stylu
        /// <summary>
        /// Typ fontu.
        /// Konkrétní typ použitý na počítači je dán přiřazením fontu v rámci Windows na počítači, kde aplikace běží.
        /// </summary>
        public GuiFontSetType FontType { get { return GetFirst(this.CellStyle?.FontType, this.RowStyle?.FontType, this.ColumnStyle?.FontType, (this.RowIsChild ? this.TableDefaultChildStyle?.FontType : null), this.TableDefaultStyle?.FontType, this.ImplicitStyle.FontType); } }
        /// <summary>
        /// Relativní velikost fontu v procentech. Null = 100 = 100%
        /// </summary>
        public int FontRelativeSize { get { return GetFirst(this.CellStyle?.FontRelativeSize, this.RowStyle?.FontRelativeSize, this.ColumnStyle?.FontRelativeSize, (this.RowIsChild ? this.TableDefaultChildStyle?.FontRelativeSize : null), this.TableDefaultStyle?.FontRelativeSize, this.ImplicitStyle.FontRelativeSize); } }
        /// <summary>
        /// Font je Bold?
        /// </summary>
        public bool FontBold { get { return GetFirst(this.CellStyle?.FontBold, this.RowStyle?.FontBold, this.ColumnStyle?.FontBold, (this.RowIsChild ? this.TableDefaultChildStyle?.FontBold : null), this.TableDefaultStyle?.FontBold, this.ImplicitStyle.FontBold); } }
        /// <summary>
        /// Font je Italic?
        /// </summary>
        public bool FontItalic { get { return GetFirst(this.CellStyle?.FontItalic, this.RowStyle?.FontItalic, this.ColumnStyle?.FontItalic, (this.RowIsChild ? this.TableDefaultChildStyle?.FontItalic : null), this.TableDefaultStyle?.FontItalic, this.ImplicitStyle.FontItalic); } }
        /// <summary>
        /// Font je Underlined?
        /// </summary>
        public bool FontUnderline { get { return GetFirst(this.CellStyle?.FontUnderline, this.RowStyle?.FontUnderline, this.ColumnStyle?.FontUnderline, (this.RowIsChild ? this.TableDefaultChildStyle?.FontUnderline : null), this.TableDefaultStyle?.FontUnderline, this.ImplicitStyle.FontUnderline); } }

        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud není Selected, a není to aktivní položka (řádek tabulky), prostě běžný prvek (řádek)
        /// </summary>
        public Color BackColor
        {
            get
            {
                /*
                Color? c1 = this.CellStyle?.BackColor;
                Color? c2 = this.RowStyle?.BackColor;
                Color? c3 = this.ColumnStyle?.BackColor;
                Color? c4 = (this.RowIsChild ? this.TableDefaultChildStyle?.BackColor : null);
                Color? c5 = this.TableDefaultStyle?.BackColor;
                Color? c6 = this.ImplicitStyle.BackColor;
                Color c0 = GetFirst(c1, c2, c3, c4, c5, c6);
                return c0;
                */
                return GetFirst(this.CellStyle?.BackColor, this.RowStyle?.BackColor, this.ColumnStyle?.BackColor, (this.RowIsChild ? this.TableDefaultChildStyle?.BackColor : null), this.TableDefaultStyle?.BackColor, this.ImplicitStyle.BackColor);
            }
        }
        /// <summary>
        /// Barva textu v prvku (řádek, buňka) pokud není Selected, a není to aktivní položka (řádek tabulky), prostě běžný prvek (řádek)
        /// </summary>
        public Color TextColor { get { return GetFirst(this.CellStyle?.TextColor, this.RowStyle?.TextColor, this.ColumnStyle?.TextColor, (this.RowIsChild ? this.TableDefaultChildStyle?.TextColor : null), this.TableDefaultStyle?.TextColor, this.ImplicitStyle.TextColor); } }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud je Selected, a není to aktivní položka (řádek tabulky)
        /// </summary>
        public Color SelectedBackColor { get { return GetFirst(this.CellStyle?.SelectedBackColor, this.RowStyle?.SelectedBackColor, this.ColumnStyle?.SelectedBackColor, (this.RowIsChild ? this.TableDefaultChildStyle?.SelectedBackColor : null), this.TableDefaultStyle?.SelectedBackColor, this.ImplicitStyle.SelectedBackColor); } }
        /// <summary>
        /// Barva textu v prvku (řádek, buňka) pokud je Selected, a není to aktivní položka (řádek tabulky)
        /// </summary>
        public Color SelectedTextColor { get { return GetFirst(this.CellStyle?.SelectedTextColor, this.RowStyle?.SelectedTextColor, this.ColumnStyle?.SelectedTextColor, (this.RowIsChild ? this.TableDefaultChildStyle?.SelectedTextColor : null), this.TableDefaultStyle?.SelectedTextColor, this.ImplicitStyle.SelectedTextColor); } }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud je tento prvek aktivní (řádek je vybraný) a v jeho controlu je focus.
        /// Po odchodu focusu z tohoto prvku je barva prvku změněna na 50% směrem k barvě BackColor nebo SelectedBackColor.
        /// </summary>
        public Color ActiveBackColor { get { return GetFirst(this.CellStyle?.ActiveBackColor, this.RowStyle?.ActiveBackColor, this.ColumnStyle?.ActiveBackColor, (this.RowIsChild ? this.TableDefaultChildStyle?.ActiveBackColor : null), this.TableDefaultStyle?.ActiveBackColor, this.ImplicitStyle.ActiveBackColor); } }
        /// <summary>
        /// Barva písma v prvku (řádek, buňka) pokud je tento prvek aktivní (řádek je vybraný) a v jeho controlu je focus.
        /// Po odchodu focusu z tohoto prvku je barva prvku změněna na 50% směrem k barvě TextColor nebo SelectedTextColor.
        /// </summary>
        public Color ActiveTextColor { get { return GetFirst(this.CellStyle?.ActiveTextColor, this.RowStyle?.ActiveTextColor, this.ColumnStyle?.ActiveTextColor, (this.RowIsChild ? this.TableDefaultChildStyle?.ActiveTextColor : null), this.TableDefaultStyle?.ActiveTextColor, this.ImplicitStyle.ActiveTextColor); } }
        /// <summary>
        /// Obsahuje aktuální barvu pozadí.
        /// Barva reaguje na hodnoty aktuálního řádku: <see cref="Row.IsActive"/>, <see cref="Row.IsChecked"/>, <see cref="Row.TreeNode"/>.IsRoot
        /// </summary>
        public Color CurrentBackColor
        {
            get
            {
                if (this.RowIsActive) return this.ActiveBackColor;
                if (this.RowIsChecked) return this.SelectedBackColor;
                return this.BackColor;
            }
        }
        /// <summary>
        /// Obsahuje aktuální barvu textu.
        /// Barva reaguje na hodnoty aktuálního řádku: <see cref="Row.IsActive"/>, <see cref="Row.IsChecked"/>, <see cref="Row.TreeNode"/>.IsRoot
        /// </summary>
        public Color CurrentTextColor
        {
            get
            {
                if (this.RowIsActive) return this.ActiveTextColor;
                if (this.RowIsChecked) return this.SelectedTextColor;
                return this.TextColor;
            }
        }
        /// <summary>
        /// Strany kreslené okolo buňky
        /// </summary>
        public GuiBorderSideType GridLines { get { return GetFirst(this.CellStyle?.GridLines, this.RowStyle?.GridLines, this.ColumnStyle?.GridLines, (this.RowIsChild ? this.TableDefaultChildStyle?.GridLines : null), this.TableDefaultStyle?.GridLines, this.ImplicitStyle.GridLines); } }
        /// <summary>
        /// Barva vodorovných linek
        /// </summary>
        public Color HorizontalLineColor { get { return GetFirst(this.CellStyle?.HorizontalLineColor, this.RowStyle?.HorizontalLineColor, this.ColumnStyle?.HorizontalLineColor, (this.RowIsChild ? this.TableDefaultChildStyle?.HorizontalLineColor : null), this.TableDefaultStyle?.HorizontalLineColor, this.ImplicitStyle.HorizontalLineColor); } }
        /// <summary>
        /// Barva svislých linek
        /// </summary>
        public Color VerticalLineColor { get { return GetFirst(this.CellStyle?.VerticalLineColor, this.RowStyle?.VerticalLineColor, this.ColumnStyle?.VerticalLineColor, (this.RowIsChild ? this.TableDefaultChildStyle?.VerticalLineColor : null), this.TableDefaultStyle?.VerticalLineColor, this.ImplicitStyle.VerticalLineColor); } }

        /// <summary>
        /// Aktuální font
        /// </summary>
        public FontInfo CurrentFontInfo
        {
            get
            {
                return new FontInfo()
                {
                    FontType = (FontSetType)((int)this.FontType),
                    SizeRatio = (float)this.FontRelativeSize / 100f,
                    Bold = this.FontBold,
                    Italic = this.FontItalic,
                    Underline = this.FontUnderline
                };
            }
        }
        /// <summary>
        /// Vrátí první not null hodnotu z dodaných parametrů.
        /// Pokud jsou všechny null, vrací se default(T).
        /// </summary>
        /// <typeparam name="T">Typ, který se má vracet (obsah nullable parametru)</typeparam>
        /// <param name="values">Seznam dodaných hodnot</param>
        /// <returns></returns>
        protected static T GetFirst<T>(params T?[] values) where T : struct
        {
            foreach (T? value in values)
            {
                if (value.HasValue) return value.Value;
            }
            return default(T);
        }
        #endregion
    }
    #endregion
    #region class TreeViewDrawArgs : Argumenty pro kreslení TreeView struktury
    /// <summary>
    /// TreeViewDrawArgs : Argumenty pro kreslení TreeView struktury
    /// </summary>
    public class TreeViewDrawArgs
    {
        /// <summary>
        /// Buňka, kde se bude TreeView vykreslovat
        /// </summary>
        public Cell OwnerCell { get; set; }
        /// <summary>
        /// Absolutní souřadnice buňky
        /// </summary>
        public Rectangle BoundsAbsolute { get; set; }

        /// <summary>
        /// Obsahuje offset pro posun nodů.
        /// </summary>
        public int TreeViewNodeOffset { get; set; }
        /// <summary>
        /// Styl kreslení linky mezi Root nodem a jeho Child nody.
        /// </summary>
        public TreeViewLinkMode TreeViewLinkMode { get; set; }
        /// <summary>
        /// Barva linky mezi Root nodem a jeho Child nody. Může obsahovat Alpha kanál. Může být null.
        /// </summary>
        public Color? TreeViewLinkColor { get; set; }

        /// <summary>
        /// Ikona je Hot = je na ní najetá myš
        /// </summary>
        public bool IconIsHot { get; set; }
        /// <summary>
        /// Ikona je stisknutá
        /// </summary>
        public bool IconIsDown { get; set; }

        /// <summary>
        /// Výsledné pole vykreslených TreeLine
        /// </summary>
        internal TreeNodeLineType[] TreeNodeLines { get; set; }

        /// <summary>
        /// Výsledná absolutní souřadnice prostoru, kam může být vepsán obsah buňky (tj. až za TreeLines a ikonu)
        /// </summary>
        public Rectangle CellValueBounds { get; set; }
        /// <summary>
        /// Výsledná absolutní souřadnice ikony, kde by měla být vykreslena
        /// </summary>
        public Rectangle? IconTargetBounds { get; set; }
        /// <summary>
        /// Výsledná absolutní souřadnice ikony, kde je interaktivní
        /// </summary>
        public Rectangle? IconActiveBounds { get; set; }
        /// <summary>
        /// Výsledná absolutní souřadnice ikony, kde je vykreslena
        /// </summary>
        public Rectangle? IconImageBounds { get; set; }
        /// <summary>
        /// Souřadnice prostoru TreeLine nejvíce vpravo
        /// </summary>
        public Rectangle? LastLineBounds { get; set; }

        /// <summary>
        /// Obsahuje true, pokud this objekt obsahuje data, podle kterých je možno kreslit
        /// </summary>
        public bool IsValid { get { return (this.OwnerCell != null && this.BoundsAbsolute.HasPixels()); } }
    }
    #endregion
    #region Interface IGTable, který dává přístup k interním metodám GTable
    /// <summary>
    /// Interface pro <see cref="GTable"/>, aby interní metody nebyly veřejně viditelné
    /// </summary>
    public interface IGTable
    {
        /// <summary>
        /// Funkce, kterou aktuálně má objekt TableHeader
        /// </summary>
        TableHeaderFunctionType TableHeaderFunction { get; }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví tabulky.
        /// </summary>
        /// <param name="e"></param>
        void TableHeaderClick(GInteractiveChangeStateArgs e);
        /// <summary>
        /// Provede se poté, kdy uživatel klikne levou (=standardní) myší na záhlaví sloupce.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="column"></param>
        void ColumnHeaderClick(GInteractiveChangeStateArgs e, Column column);
        /// <summary>
        /// Provede se poté, kdy uživatel klikne pravou (=kontextovou) myší na záhlaví sloupce.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="column"></param>
        void ColumnHeaderContextMenu(GInteractiveChangeStateArgs e, Column column);
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví řádku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="row">řádek</param>
        void RowHeaderClick(GInteractiveChangeStateArgs e, Row row);
        /// <summary>
        /// Provede se poté, kdy uživatel vstoupí s myší nad určitou buňkou.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void CellMouseEnter(GInteractiveChangeStateArgs e, Cell cell);
        /// <summary>
        /// Provede se poté, kdy uživatel vystoupí myší z určité buňky jinam.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void CellMouseLeave(GInteractiveChangeStateArgs e, Cell cell);
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void CellClick(GInteractiveChangeStateArgs e, Cell cell);
        /// <summary>
        /// Provede se poté, kdy uživatel povede DoubleClick na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void CellDoubleClick(GInteractiveChangeStateArgs e, Cell cell);
        /// <summary>
        /// Provede se poté, kdy uživatel povede LongClick na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void CellLongClick(GInteractiveChangeStateArgs e, Cell cell);
        /// <summary>
        /// Provede se poté, kdy uživatel povede RightClick na datovou buňku.
        /// Pokud řádek buňky není aktivní, měl by být aktivován.
        /// Pokud buňka není aktivní, a tabulka podporuje výběr buněk, měla by být aktivována.
        /// Po změně aktivní buňky se vyžádá překreslení tabulky.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cell">řádek</param>
        void CellRightClick(GInteractiveChangeStateArgs e, Cell cell);
        /// <summary>
        /// Řídí proces Drag and Move pro přemístění řádku, všechny fáze (viz argument e, <see cref="GDragActionArgs.DragAction"/>)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="currentRow"></param>
        /// <param name="targetRelativeBounds"></param>
        void RowDragMoveAction(GDragActionArgs e, Row currentRow, Rectangle? targetRelativeBounds);
        /// <summary>
        /// Provádí vykreslení přesouvaného řádku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        void RowDragMoveDraw(GInteractiveDrawArgs e, Rectangle boundsAbsolute);
    }
    #endregion
}
