﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Components.Grids;

namespace Asol.Tools.WorkScheduler.Components
{
    /* Filosofický základ pro obsluhu různých událostí: Grid je línej jako veš! 
     *  Ten je tak línej, že když se dojde ke změně něčeho (třeba výšky některé tabulky), tak ta změna (v property Table.Height) zavolá "nahoru" že došlo k dané změně,
     *   to volání se dostane do GTable jako invalidace výšky tabulky, to vyvolá obdobné volání do Gridu, a Grid si jen líně poznamená: "Rozložení tabulek na výšku už neplatí".
     *   Současně s tím si poznamená: "Neplatí ani moje ChildItem prvky (protože některá další tabulka může/nemusí být vidět, protože se odsunula dolů).
     *  Podobně se chová i GTable: poznamená si: moje vnitřní souřadnice ani moje ChildItem prvky nejsou platné.
     *   Teprve až bude někdo chtít pracovat s něčím v Gridu nebo v jeho GTable (typicky: zjištění interaktivity prvků, vykreslení tabulky), tak si požádá o ChildItems,
     *   tam se zjistí že jsou neplatné, a Grid nebo GTable začne shánět platné údaje. 
     *   Při tom zjistí, že je jich většina neplatných, a začne je přepočítávat z aktuálních reálných hodnot (fyzické rozměry, počet a velikost tabulek, pozice řádků, atd).
     */

    /* Řešení Layoutu (7.12.2018): Velikost prvku a jeho pozice v celé sekvenci
     *  Datové prvky (Table, Row, Column) v sobě mají instanci třídy ItemSizeInt:
     *      - Tato třída je DATOVÁ, proto si řídí jen svoji VELIKOST, ale ne svoji POZICI.
     *      - Tato třída obsahuje aktuální velikost prvku Size (=Height nebo Width)
     *      - A dále určuje rozsah této hodnoty (SizeMinimum, SizeMaximum) a výchozí hodnotu (SizeDefault)
     *      - K tomu obshauje: Visible, Autosize, ResizeEnabled a ReOrderEnabled
     *      - Dále tato třída obsahuje referenci na Parent instanci (typicky uložená v tabulce),
     *        kde tato Parent instance obsahuje výchozí hodnoty pro všechny property, které na konkrétním řádku/sloupci nejsou naplněny (jsou null).
     *  Vizuální prvky (GTable, GRow, GColumn) v sobě mají instanci třídy SequenceLayout (implementuje ISequenceLayout):
     *      - Je to třída určená pro VIZUÁLNÍ PRÁCI, proto v sobě obsahuje pozici BEGIN a referenci na DATA, která obsahují VELIKOST
     *      - Tato třída řeší pozici konkrétního vizuálního prvku v sekvenci pole sousedních prvků (řádky pod sebou, sloupce vedle sebe)
     *      - Při určování souřadnic celoého pole se provádí metoda SequenceLayout.SequenceLayoutCalculate(),
     *        která pro řadu prvku nastavuje jejich Begin, odvozuje End (=Begin + Size + space), a ten vkládá do Begin následujcího prvku
     */

    /// <summary>
    /// GGrid : Vizuální objekt, kontejner na jednu nebo více tabulek pod sebou. Tyto tabulky mají společný layout sloupců (šířka) i společný vodorovný (dolní) posuvník.
    /// </summary>
    public class GGrid : InteractiveContainer, IInteractiveItem
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GGrid(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
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
            InvalidateItem items = InvalidateItem.Paint;

            // Změna umístění gridu:
            Rectangle? lastBounds = this._LastBounds;
            bool isSamePosition = (lastBounds.HasValue && lastBounds.Value.Location == newBounds.Location);
            if (!isSamePosition)
                items |= InvalidateItem.Paint;

            // Vnitřní prostor - to je výška a šířka:
            Size? lastClientSize = this._LastClientSize;
            Size currentClientSize = this.ClientSize;
            bool isSameHeight = (lastClientSize.HasValue && lastClientSize.Value.Height == currentClientSize.Height);
            if (!isSameHeight)
                items |= InvalidateItem.GridTablesScroll | InvalidateItem.GridInnerBounds;
            bool isSameWidth = (lastClientSize.HasValue && lastClientSize.Value.Width == currentClientSize.Width);
            if (!isSameWidth)
                items |= InvalidateItem.GridColumnsScroll | InvalidateItem.GridInnerBounds;

            this.Invalidate(items);

            this._LastBounds = newBounds;
        }
        /// <summary>
        /// Metoda zajistí, že souřadnice vnitřních objektů budou platné a budou odpovídat aktuální velikosti Tabulky a poloze splitterů a rozsahu dat.
        /// Jde o souřadnice: _TablesBounds, _TablesScrollBarVisible, _TablesScrollBarBounds, _ColumnsScrollBarVisible, _ColumnsScrollBarBounds,
        /// _GridVoidBounds1, _GridVoidBounds2, _ColumnRowHeaderVisualRange, _ColumnsDataVisualRange.
        /// </summary>
        protected void _InnerBoundsCheck()
        {
            if (this._TableInnerLayoutValid) return;

            Size clientSize = this.ClientSize;
            if (clientSize.Width <= 0 || clientSize.Height <= 0) return;

            // Umožníme tabulkám aplikovat jejich hodnotu AutoSize při dané výšce viditelné oblasti:
            this._TablesPositionCalculateAutoSize();

            // Umožníme sloupcům aplikovat jejich hodnotu AutoSize při dané šířce viditelné oblasti:
            this._ColumnsPositionCalculateAutoSize();

            this._TableInnerLayoutValid = true;                      // Normálně to patří až na konec metody. Ale některé komponenty mohou používat již částečně napočtené hodnoty, a pak bychom se zacyklili

            // Určíme, zda bude zobrazen scrollbar vpravo (to je tehdy, když výška tabulek je větší než výška prostoru pro tabulky = (ClientSize.Height - ColumnsScrollBar.Bounds.Height)):
            //  Objekt TablesPositions tady provede dotaz na velikost dat (metoda this._TablesPositionGetDataSize()) a velikost viditelného prostoru (metoda this._TablesPositionGetVisualSize()).
            //  Velikost dat je dána tabulkami, resp. pozici End poslední tabulky v sekvenci this.TablesSequence, 
            //  velikost viditelného prostoru pro tabulky je dána (this.ClientSize.Height - this.ColumnsScrollBar.Bounds.Height), takže se vždy počítá s prostorem pro zobrazením vodorovného scrollbaru:
            this._TablesScrollBarVisible = this.TablesPositions.IsScrollBarActive;

            // Určíme souřadnice jednotlivých elementů:
            int x0 = 0;                                              // x0: úplně vlevo
            int x1 = this.ColumnsPositions.VisualFirstPixel;         // x1: zde začíná ColumnsScrollBar (hned za koncem RowHeaderColumn)
            int x3 = clientSize.Width;                               // x3: úplně vpravo
            int x2t = x3 - ScrollBar.DefaultSystemBarWidth;         // x2t: zde začíná TablesScrollBar (vpravo, hned za koncem ColumnsScrollBar), tedy pokud by byl zobrazen
            int x2r = (this._TablesScrollBarVisible ? x2t : x3);     // x2r: zde reálně končí oblast prostoru pro tabulky a končí zde i ColumnsScrollBar, se zohledněním aktuální viditelnosti TablesScrollBaru
            int y0 = 0;                                              // y0: úplně nahoře
            int y1 = y0;                                             // y1: zde začíná prostor pro tabulky i TablesScrollBar 
            int y3 = clientSize.Height;                              // y3: úplně dole
            int y2 = y3 - ScrollBar.DefaultSystemBarHeight;         // y2: zde začíná ColumnsScrollBar (dole, hned za koncem prostoru pro tabulky)
            int wt = x2r - x1;                                       // wt: šířka prostoru pro datové sloupce tabulek (za RowHeaderem, před TableScrollBarem)

            // Určíme, zda bude zobrazen scrollbar dole (to je tehdy, když šířka datových sloupců tabulek je větší než prostor od RowHeaderColumn do pravého Scrollbaru, pokud je zobrazen):
            //  Objekt ColumnsPositions si data zjistí sám, poocí zdejších metod this._ColumnPositionGetVisualSize() a this._ColumnPositionGetDataSize()
            //  Tzn. opírá se o hodnoty získané z: ClientSize.Width, ColumnsPositions.VisualFirstPixel, TablesScrollBarVisible (GScrollBar.DefaultSystemBarWidth)
            //       a ze sloupce: Columns[last].End
            this._ColumnsScrollBarVisible = this.ColumnsPositions.IsScrollBarActive;

            // Souřadnice jednotlivých oblastí:
            this._TablesBounds = new Rectangle(x0, y0, x2r - x0, y2 - y0);
            this._TablesScrollBarBounds = new Rectangle(x2t, y1, x3 - x2t, y2 - y1);
            this._ColumnsScrollBarBounds = new Rectangle(x1, y2, wt, y3 - y2);
            this._GridVoidBounds1 = new Rectangle(x0, y2, x1 - x0, y3 - y2);
            this._GridVoidBounds2 = new Rectangle(x2r, y2, x3 - x2r, y3 - y2);

            // Invalidace závislých prvků:
            this._TablesVisibleCurrent = null;
            this._TablesScrollBarDataValid = false;
            this._ColumnsScrollBarDataValid = false;
            this._ChildArrayValid = false;

            this._LastClientSize = clientSize;
        }
        /// <summary>
        /// Prostor pro tabulky (hlavní prostor), neobsahuje pozice scrollbarů X a Y
        /// </summary>
        protected Rectangle TablesBounds { get { this._InnerBoundsCheck(); return this._TablesBounds; } } private Rectangle _TablesBounds;
        /// <summary>
        /// true pokud se má zobrazovat svislý scrollbar (pro tabulky, vpravo)
        /// </summary>
        protected bool TablesScrollBarVisible { get { this._InnerBoundsCheck(); return this._TablesScrollBarVisible; } } private bool _TablesScrollBarVisible;
        /// <summary>
        /// Prostor pro svislý scrollbar (pro tabulky, vpravo)
        /// </summary>
        protected Rectangle TablesScrollBarBounds { get { this._InnerBoundsCheck(); return this._TablesScrollBarBounds; } } private Rectangle _TablesScrollBarBounds;
        /// <summary>
        /// true pokud se má zobrazovat vodorovný scrollbar (pro sloupce, dole)
        /// </summary>
        protected bool ColumnsScrollBarVisible { get { this._InnerBoundsCheck(); return this._ColumnsScrollBarVisible; } } private bool _ColumnsScrollBarVisible;
        /// <summary>
        /// Prostor pro vodorovný scrollbar (pro sloupce, dole)
        /// </summary>
        protected Rectangle ColumnsScrollBarBounds { get { this._InnerBoundsCheck(); return this._ColumnsScrollBarBounds; } } private Rectangle _ColumnsScrollBarBounds;
        /// <summary>
        /// Prázdný prostor 1 = pod sloupcem RowHeaderColumn, kam nezasahuje ColumnsScrollBar - ten je jen pod prostorem datových sloupců.
        /// Tento prostor není interaktvní, ale měl by být vyplněn barvou pozadí.
        /// </summary>
        protected Rectangle GridVoidBounds1 { get { this._InnerBoundsCheck(); return this._GridVoidBounds1; } } private Rectangle _GridVoidBounds1;
        /// <summary>
        /// Prázdný prostor 2 = pod prostorem TablesScrollBar, kam nezasahuje ColumnsScrollBar - ten je jen pod prostorem datových sloupců.
        /// Tento prostor není interaktvní, ale měl by být vyplněn barvou pozadí.
        /// </summary>
        protected Rectangle GridVoidBounds2 { get { this._InnerBoundsCheck(); return this._GridVoidBounds2; } } private Rectangle _GridVoidBounds2;
        /// <summary>
        /// Metoda vrátí relativní souřadnice požadovaného prostoru.
        /// Relativní = relativně k this.Bounds.Location, který představuje bod {0;0}.
        /// Povolené typy prostoru jsou: AllTables, HorizontalScrollBar, VerticalScrollBar.
        /// </summary>
        /// <param name="areaType"></param>
        /// <returns></returns>
        public Rectangle GetRelativeBoundsForArea(TableAreaType areaType)
        {
            switch (areaType)
            {
                case TableAreaType.AllTables: return this.TablesBounds;
                case TableAreaType.HorizontalScrollBar: return this.ColumnsScrollBarBounds;
                case TableAreaType.VerticalScrollBar: return this.TablesScrollBarBounds;
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice požadovaného prostoru.
        /// Souřadnice slouží k provedení Graphics.Clip() před vykreslením obsahu.
        /// Povolené typy prostoru jsou: AllTables, HorizontalScrollBar, VerticalScrollBar.
        /// </summary>
        /// <param name="areaType"></param>
        /// <returns></returns>
        public Rectangle GetAbsoluteBoundsForArea(TableAreaType areaType)
        {
            Rectangle gridAbsoluteBounds = this.BoundsAbsolute;
            Rectangle relativeBounds = this.GetRelativeBoundsForArea(areaType);
            return relativeBounds.Add(gridAbsoluteBounds.Location);
        }
        /// <summary>
        /// Platnosti souřadnic vnitřních objektů 
        /// (_TablesBounds, _TablesScrollBarVisible, _TablesScrollBarBounds, _ColumnsScrollBarVisible, _ColumnsScrollBarBounds,
        /// _GridVoidBounds1, _GridVoidBounds2, _ColumnRowHeaderVisualRange, _ColumnsDataVisualRange)
        /// </summary>
        private bool _TableInnerLayoutValid;
        /// <summary>
        /// Souřadnice Gridu, pro které byly naposledy validovány Outer souřadnice
        /// </summary>
        private Rectangle? _LastBounds;
        /// <summary>
        /// Vnitřní rozměry tabulky, pro které byly naposledy validovány Inner souřadnice
        /// </summary>
        private Size? _LastClientSize;
        #endregion
        #region Tabulky uložené v Gridu
        /// <summary>
        /// Soupis všech grafických tabulek v gridu
        /// </summary>
        internal IEnumerable<GTable> Tables { get { return this._Tables; } }
        /// <summary>
        /// Soupis všech datových tabulek v gridu
        /// </summary>
        public IEnumerable<Table> DataTables { get { return this._Tables.Select(t => t.DataTable); } }
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
        /// Počet tabulek v gridu
        /// </summary>
        public int TablesCount { get { return this._Tables.Count; } }
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
            GTable gTable = args.Item;
            IGridMember iGridMember = gTable as IGridMember;
            int id = this._TableID++;
            if (iGridMember != null)
            {
                iGridMember.GGrid = this;
                iGridMember.Id = id;
            }
            this.Invalidate(InvalidateItem.GridTablesChange | InvalidateItem.GridColumnsChange | InvalidateItem.GridItems);
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu přidání tabulky, tabulka je platná, event TableAddAfter ještě neproběhl. V GGrid je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTableAddAfter(EList<GTable>.EListAfterEventArgs args) { }
        /// <summary>
        /// Public event vyvolaný po přidání nové tabulky do gridu. Grid je již v tabulce umístěn, grid je uveden v argumentu.
        /// </summary>
        public event EList<GTable>.EListEventAfterHandler TableAddAfter;
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
            IGridMember iGridMember = args.Item as IGridMember;
            if (iGridMember != null)
            {
                iGridMember.GGrid = null;
                iGridMember.Id = -1;
            }
            this.Invalidate(InvalidateItem.GridTablesChange | InvalidateItem.GridColumnsChange | InvalidateItem.GridItems);
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu odebrání řádku, řádek je platný, event RowRemoveAfter ještě neproběhl. V Table je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTableRemoveAfter(EList<GTable>.EListAfterEventArgs args) { }
        /// <summary>
        /// Public event vyvolaný po odebrání řádku z tabulky. Řádek již v tabulce není umístěn, řádek je uveden v argumentu.
        /// </summary>
        public event EList<GTable>.EListEventAfterHandler TableRemoveAfter;
        /// <summary>
        /// ID pro příští vkládanou GTable
        /// </summary>
        private int _TableID = 0;
        /// <summary>
        /// Fyzické úložiště tabulek GTable
        /// </summary>
        private EList<GTable> _Tables;
        #endregion
        #region Sloupce Gridu a ColumnLayout (persistence nastavení sloupců)
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
        /// Obsahuje všechny sloupce ze všech tabulek celého gridu, včetně těch sloupců, co zrovna nejsou viditelné (ale jen z viditelných tabulek).
        /// </summary>
        public GridColumn[] AllColumns { get { this._ColumnsCheck(); return this._AllColumns; } }
        /// <summary>
        /// Rozložení sloupců tohoto gridu. Používá se pro uložení layoutu. 
        /// Get i Set jsou přímo napojeny na živé sloupce; obsah <see cref="ColumnLayout"/> se nikde izolovaně neukládá.
        /// </summary>
        public string ColumnLayout
        {
            get
            {
                return _GetColumnLayout();
            }
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                var oldLayout = _GetColumnLayout();
                if (String.Equals(oldLayout, value)) return;
                SetColumnLayout(value);
            }
        }
        /// <summary>
        /// Vrací aktuální layout sloupců
        /// </summary>
        /// <returns></returns>
        private string _GetColumnLayout()
        {
            GridColumn[] columns = this.AllColumns;
            if (columns == null || columns.Length == 0) return null;
            StringBuilder sb = new StringBuilder();
            foreach (var column in columns)
            {
                string name = column.MasterColumn.ColumnName;
                if (String.IsNullOrEmpty(name)) continue;
                if (sb.Length > 0) sb.Append(";");
                sb.Append($"{name}:{column.ColumnOrder}:{column.ColumnWidth}:{(column.IsVisible ? "1" : "0")}");
            }
            return sb.ToString();
        }
        /// <summary>
        /// Vloží dodaný layout sloupců
        /// </summary>
        /// <param name="layout"></param>
        internal void SetColumnLayout(string layout)
        {
            if (String.IsNullOrEmpty(layout)) return;

            GridColumn[] columns = this.AllColumns;
            if (columns == null || columns.Length == 0) return;

            Dictionary<string, GridColumn> colDict = columns
                .Where(c => !String.IsNullOrEmpty(c.MasterColumn.ColumnName))
                .GetDictionary(c => c.MasterColumn.ColumnName, true);

            var table = layout.ToTable(";", ":", true, true);
            foreach (var row in table)
            {
                if (row.Length < 3) continue;
                GridColumn column;
                string name = row[0];
                if (String.IsNullOrEmpty(name) || !colDict.TryGetValue(name, out column)) continue;
                column.ColumnOrder = _ToInt(row[1]);
                column.ColumnWidth = _ToInt(row[2]);
                if (row.Length >= 4 && (row[3] == "1" || row[3] == "0"))
                    column.IsVisible = (row[3] == "1");
            }
            this.Invalidate(InvalidateItem.GridColumnsScroll);
            this.RefreshColumns(false);
            this.Refresh();
        }
        /// <summary>
        /// Vrací Int32 hodnotu z daného stringu
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static int _ToInt(string text)
        {
            int value;
            if (Int32.TryParse(text, out value)) return value;
            return -1;
        }
        /// <summary>
        /// Zajistí, že pole sloupců budou obsahovat platné hodnoty
        /// </summary>
        private void _ColumnsCheck(bool force = false)
        {
            bool needContent = (this._ColumnDict == null || this._Columns == null || this._AllColumns == null);
            bool needRecalc = !this._ColumnsLayoutValid || !this._ColumnsSizeValid;
            if (force || needContent || needRecalc)
            {
                var columnDict = this._ColumnDict;
                var allColumns = this._AllColumns;
                var oldColumnDict = columnDict;
                if (force || needContent)
                {    // Je nutné vygenerovat obsah těchto polí:
                     // Vytvořím index, kde klíčem je ColumnId, a hodnotou je instance GridColumn.
                     // každá jedna instance GridColumn bude obsahovat souhrn všech viditelných sloupců shodného ColumnId, ze všech viditelných tabulek.
                    columnDict = new Dictionary<int, GridColumn>();
                    Dictionary<int, GridColumn> allColumnDict = new Dictionary<int, GridColumn>();        // Všechny sloupce z viditelných tabulek
                    foreach (GTable table in this._Tables)
                    {
                        if (table.DataTable == null || !table.DataTable.Visible) continue;                // Neviditelné tabulky nebudu vůbec řešit. Změna viditelnosti tabulky (Data.Table.Visible) vyvolá GGrid.RefreshColumns(), odkud se volá _ColumnsCheck(true).
                        foreach (Column column in table.DataTable.Columns)
                        {
                            int columnId = column.ColumnId;
                            
                            // a) Souhrn všech sloupců:
                            GridColumn gridColumn = allColumnDict.GetAdd(columnId, k => new GridColumn(this, columnId));
                            gridColumn.AddColumn(column);

                            // b) Souhrn jen viditelných sloupců:
                            if (column.IsVisible && !columnDict.ContainsKey(columnId))
                                columnDict.Add(columnId, gridColumn);
                            //{
                            //    gridColumn = columnDict.GetAdd(columnId, k => new GridColumn(this, columnId));
                            //    gridColumn.AddColumn(column);
                            //}
                        }
                    }
                    allColumns = allColumnDict.Values.ToArray();
                }

                // Nyní vytvořím lineární soupis GridColumn, a setřídím jej podle pořadí dle sloupce Master (z první tabulky):
                List<GridColumn> columnList = columnDict.Values.ToList();
                columnList.Sort(GridColumn.CompareOrder);

                if (force)
                    _ColumnsPositionSizeRefresh(columnList, oldColumnDict);
                if (force || !this._ColumnsSizeValid)
                    _ColumnsPositionCalculateAutoSize(columnList);

                // Zajistím provedení nápočtu pozic (ISequenceLayout.Begin, End):
                SequenceLayout.SequenceLayoutCalculate(columnList, 0);         // explicitně zadávám spacing = 0

                // Na závěr je nutno uložit vypočtená data:
                this._ColumnDict = columnDict;
                this._Columns = columnList.ToArray();
                this._AllColumns = allColumns;
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
            return TryGetGridColumn(columnId, false, out gridColumn);
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit data o sloupci Gridu pro dané ID.
        /// Sloupec Gridu (instance třídy GridColumn) reprezentuje jeden svislý sloupec stejného ColumnId, přes všechny tabulky.
        /// Sloupec obsahuje MasterColumn = sloupec tohoto ColumnId z první tabulky, ve které se vyskytl. Ten pak hraje roli Mastera.
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="skipValidations">true = přeskoč validace, pokud to jde</param>
        /// <param name="gridColumn"></param>
        /// <returns></returns>
        protected bool TryGetGridColumn(int columnId, bool skipValidations, out GridColumn gridColumn)
        {
            if (skipValidations)
            {   // Přeskočit validace: pokud máme sloupce načteny, a pro dané ID sloupec existuje, pak mi stačí jej vrátit bez dalších validací:
                if (this._ColumnDict != null && this._ColumnDict.TryGetValue(columnId, out gridColumn))
                    return true;
            }

            // Plné získání sloupce včetně validací:
            this._ColumnsCheck();
            return this._ColumnDict.TryGetValue(columnId, out gridColumn);
        }
        /// <summary>
        /// Index sloupců podle ColumnId
        /// </summary>
        private Dictionary<int, GridColumn> _ColumnDict;
        /// <summary>
        /// Seznam viditelných sloupců podle jejich pořadí
        /// </summary>
        private GridColumn[] _Columns;
        /// <summary>
        /// Seznam všech sloupců (tj. včetně neviditelných) podle jejich pořadí
        /// </summary>
        private GridColumn[] _AllColumns;
        /// <summary>
        /// true pokud obsah pole _Columns má správné hodnoty Size (=AutoSize na odpovídající sloupec s grafem), false pokud ne
        /// </summary>
        private bool _ColumnsSizeValid = false;
        /// <summary>
        /// true pokud obsah pole _Columns má správné souřadnice, false pokud ne
        /// </summary>
        private bool _ColumnsLayoutValid = false;
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
            this.ColumnsPositions.VisualFirstPixel = width;
            int widthNew = masterTable.RowHeaderWidth;

            bool isChanged = (widthNew != widthOld);
            if (isChanged)
            {
                width = widthNew;
                // Zajistit invalidaci a překreslení:
                this.Invalidate(InvalidateItem.RowHeader | InvalidateItem.GridColumnsScroll);
            }
            return isChanged;
        }
        /// <summary>
        /// Metoda zajistí přesun sloupce na jiné místo: tak, aby daný sloupec (column) byl zobrazen na daném pořadí (hodnota ColumnOrder).
        /// Pokud na dané pozici je jiný sloupec (jeho hodnota ColumnOrder == targetOrder), pak tento jiný sloupec bude umístěn za daný sloupec.
        /// Pokud targetOrder je větší než ColumnOrder posledního sloupce, pak daný sloupec (parametr column) bude umístěn na konec.
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
        /// <param name="newOrder">Cíl přesunu = ColumnOrder jiného sloupce, na jehož místo chci dát daný column. Tento jiný sloupec bude až za aktuálním sloupcem. Cíl přesunu může být i vyšší, než je nejvyšší ID, pak aktuální sloupec bude zařazen za poslední prvek.</param>
        public bool ColumnMoveTo(int columnId, int newOrder)
        {
            GridColumn sourceColumn;
            if (!this.TryGetGridColumn(columnId, out sourceColumn)) return false;        // Daný sloupec neznáme
            int oldOrder = sourceColumn.ColumnOrder;
            if (oldOrder == newOrder) return false;                                      // Není co dělat

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
                if (currentOrder < newOrder) { reorderedColumns.Add(current); }
                // Nynější sloupec (current) má být za přemisťovaným sloupcem, a ten jsme ještě do seznamu reorderedColumns nezařadili - zařadíme tam sourceColumn, a za ním current:
                else if (!isSourceAdded) { reorderedColumns.Add(sourceColumn); isSourceAdded = true; reorderedColumns.Add(current); }
                // Sloupec current má být za sloupcem sourceColumn, a ten (sourceColumn) už byl do seznamu reorderedColumns přidán dříve:
                else { reorderedColumns.Add(current); }
            }
            // Pokud jsem až dosud nepřidal sourceColumn, přidám jej na konec:
            if (!isSourceAdded) { reorderedColumns.Add(sourceColumn); isSourceAdded = true; }

            // Dosud jsme nezměnili žádné ColumnOrder, ale máme korektně seřazenou kolekci = takže do ní vepíšeme novou hodnotu ColumnOrder:
            int columnOrder = 0;
            foreach (GridColumn gc in reorderedColumns)
                gc.ColumnOrder = columnOrder++;

            // Zajistit invalidaci a překresení:
            this.Invalidate(InvalidateItem.GridColumnsChange);

            // Zavolám event:
            this.CallColumnOrderChanged(sourceColumn, oldOrder, newOrder, EventSourceType.InteractiveChanged);

            return true;
        }
        /// <summary>
        /// Metoda zajistí změnu šířky daného sloupce, a návazné změny v interních strukturách plus překreslení
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="e"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public bool ColumnResizeTo(int columnId, GPropertyChangeArgs<int> e, ref int width)
        {
            GridColumn sourceColumn;
            if (!this.TryGetGridColumn(columnId, true, out sourceColumn)) return false;        // ,true: přeskočíme kompletní validace, pokud to jde. Výsledek false = Daný sloupec neznáme!

            int widthOld = sourceColumn.ColumnWidth;
            if (!sourceColumn.ColumnWidthOriginal.HasValue) sourceColumn.ColumnWidthOriginal = widthOld;
            sourceColumn.ColumnWidth = width;
            int widthNew = sourceColumn.ColumnWidth;

            bool isChanged = (widthNew != widthOld);
            if (isChanged)
            {
                width = widthNew;
                // Zajistit invalidaci a překresení:
                this.Invalidate(InvalidateItem.GridColumnsWidth);
                _ColumnsPositionCalculateAutoSize(this._Columns);
            }

            // Finální změna (=nejde o změnu typu "Changing" = stále probíhající, ale změna už je finální):
            EventSourceType eventSource = (e != null ? e.EventSource : EventSourceType.ValueChange);
            bool isFinal = !eventSource.HasAnyFlag(EventSourceType.InteractiveChanging | EventSourceType.ValueChanging);
            if (isFinal)
            {
                int widthOriginal = (sourceColumn.ColumnWidthOriginal.HasValue ? sourceColumn.ColumnWidthOriginal.Value : widthOld);
                if (widthNew != widthOriginal)
                    this.CallColumnWidthChanged(sourceColumn, widthOriginal, widthNew, eventSource);
            }

            return isChanged;
        }
        /// <summary>
        /// Zajistí refresh synchronních šířek sloupců všech tabulek pod sebou v tomto Gridu, volá se například po změně viditelnosti tabulek za běhu systému.
        /// Nově zviditelněné tabulky se v této metodě zapojí do systému synchronizovaných sloupců (nastaví si šířku podle Master tabulky).
        /// </summary>
        /// <param name="synchronizeTime">Zajistit i synchronizaci času (je vhodné tehdy, když došlo k přidání nové tabulky do Gridu a je třeba, aby měla zobrazený správný čas)</param>
        public void RefreshColumns(bool synchronizeTime)
        {
            this._TablesVisibleCheck(true);
            this._TablesPositionCalculateAutoSize();
            this._ColumnsCheck(true);
            if (synchronizeTime)
                this.RefreshTimeRange();
            this.Invalidate(InvalidateItem.All);
        }
        /// <summary>
        /// Metoda zajistí nastavení hodnoty isVisible do daného sloupce plus vyvolání další logiky (event)
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="isVisible"></param>
        /// <returns></returns>
        public bool ColumnSetVisible(int columnId, bool isVisible)
        {
            GridColumn sourceColumn = this.AllColumns.FirstOrDefault(c => c.ColumnId == columnId);    // Sloupec hledám v AllColumns, protože může být neviditelný.
            if (sourceColumn == null) return false;        // Daný sloupec neznáme

            bool oldVisible = sourceColumn.IsVisible;
            sourceColumn.IsVisible = isVisible;
            bool newVisible = sourceColumn.IsVisible;

            bool isChanged = (newVisible != oldVisible);
            if (isChanged)
            {
                this.CallColumnVisibleChanged(sourceColumn, oldVisible, newVisible, EventSourceType.ValueChange);
                // Zajistit invalidaci a překresení:
                this.Invalidate(InvalidateItem.GridColumnsScroll);
            }

            return isChanged;
        }
        /// <summary>
        /// Metoda vyvolá háček <see cref="OnColumnWidthChanged"/> a event <see cref="ColumnWidthChanged"/>
        /// </summary>
        /// <param name="column"></param>
        /// <param name="oldWidth"></param>
        /// <param name="newWidth"></param>
        /// <param name="eventSource"></param>
        protected void CallColumnWidthChanged(GridColumn column, int oldWidth, int newWidth, EventSourceType eventSource)
        {
            GObjectPropertyChangeArgs<GridColumn, int> args = new GObjectPropertyChangeArgs<GridColumn, int>(column, oldWidth, newWidth, eventSource);
            this.OnColumnWidthChanged(args);
            if (!this.IsSuppressedEvent && this.ColumnWidthChanged != null)
                this.ColumnWidthChanged(this, args);
        }
        /// <summary>
        /// Háček volaný před událostí <see cref="ColumnWidthChanged"/> = změna šířky sloupce
        /// </summary>
        protected virtual void OnColumnWidthChanged(GObjectPropertyChangeArgs<GridColumn, int> args) { }
        /// <summary>
        /// Událost <see cref="ColumnWidthChanged"/> = změna šířky sloupce
        /// </summary>
        public event GObjectPropertyChangedHandler<GridColumn, int> ColumnWidthChanged;
        /// <summary>
        /// Metoda vyvolá háček <see cref="OnColumnWidthChanged"/> a event <see cref="ColumnWidthChanged"/>
        /// </summary>
        /// <param name="column"></param>
        /// <param name="oldOrder"></param>
        /// <param name="newOrder"></param>
        /// <param name="eventSource"></param>
        protected void CallColumnOrderChanged(GridColumn column, int oldOrder, int newOrder, EventSourceType eventSource)
        {
            GObjectPropertyChangeArgs<GridColumn, int> args = new GObjectPropertyChangeArgs<GridColumn, int>(column, oldOrder, newOrder, eventSource);
            this.OnColumnOrderChanged(args);
            if (!this.IsSuppressedEvent && this.ColumnOrderChanged != null)
                this.ColumnOrderChanged(this, args);
        }
        /// <summary>
        /// Háček volaný před událostí <see cref="ColumnOrderChanged"/> = změna pozice sloupce
        /// </summary>
        protected virtual void OnColumnOrderChanged(GObjectPropertyChangeArgs<GridColumn, int> args) { }
        /// <summary>
        /// Událost <see cref="ColumnOrderChanged"/> = změna šířky sloupce
        /// </summary>
        public event GObjectPropertyChangedHandler<GridColumn, int> ColumnOrderChanged;
        /// <summary>
        /// Metoda vyvolá háček <see cref="OnColumnVisibleChanged"/> a event <see cref="ColumnVisibleChanged"/>
        /// </summary>
        /// <param name="column"></param>
        /// <param name="oldVisible"></param>
        /// <param name="newVisible"></param>
        /// <param name="eventSource"></param>
        protected void CallColumnVisibleChanged(GridColumn column, bool oldVisible, bool newVisible, EventSourceType eventSource)
        {
            GObjectPropertyChangeArgs<GridColumn, bool> args = new GObjectPropertyChangeArgs<GridColumn, bool>(column, oldVisible, newVisible, eventSource);
            this.OnColumnVisibleChanged(args);
            if (!this.IsSuppressedEvent && this.ColumnVisibleChanged != null)
                this.ColumnVisibleChanged(this, args);
        }
        /// <summary>
        /// Háček volaný před událostí <see cref="ColumnVisibleChanged"/> = změna viditelnosti sloupce
        /// </summary>
        protected virtual void OnColumnVisibleChanged(GObjectPropertyChangeArgs<GridColumn, bool> args) { }
        /// <summary>
        /// Událost <see cref="ColumnVisibleChanged"/> = změna viditelnosti sloupce
        /// </summary>
        public event GObjectPropertyChangedHandler<GridColumn, bool> ColumnVisibleChanged;
        #endregion
        #region Pozicování svislé - tabulky a vpravo svislý scrollbar
        /// <summary>
        /// Určuje, která tabulka z tabulek obsažených v Gridu (<see cref="DataTables"/>) bude se bude chovat jako by měla AutoSize = true, když fyzicky žádná z nich to nebude mít nastavené.
        /// Default = First
        /// </summary>
        public ImplicitAutoSizeType TablesAutoSize { get { return this._TablesAutoSize; } set { this._TablesAutoSize = value; } } private ImplicitAutoSizeType _TablesAutoSize;
        /// <summary>
        /// Inicializace objektů pro pozicování tabulek: TablesPositions, TablesScrollBar
        /// </summary>
        private void InitTablesPositions()
        {
            this._TablesPositions = new GridPosition(0, this._TablesPositionGetVisualSize, this._TablesPositionGetDataSize);
            this._TablesAutoSize = ImplicitAutoSizeType.FirstItem;

            this._TablesScrollBar = new ScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this._TablesScrollBar.ValueChanging += new GPropertyChangedHandler<DecimalNRange>(TablesScrollBar_ValueChange);
            this._TablesScrollBar.ValueChanged += new GPropertyChangedHandler<DecimalNRange>(TablesScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídící prvek pro Pozice tabulek
        /// </summary>
        protected GridPosition TablesPositions { get { return this._TablesPositions; } }
        /// <summary>
        /// Vrací výšku prostoru pro tabulky (=prostor this.ClientSize.Height mínus dole prostor pro vodorovný scrollbar sloupců).
        /// Prostor pro dolní scrollbar odečítá vždy, bez ohledu na jeho viditelnost.
        /// </summary>
        /// <returns></returns>
        private int _TablesPositionGetVisualSize()
        {
            return this.ClientSize.Height - ScrollBar.DefaultSystemBarHeight;
        }
        /// <summary>
        /// Vrací výšku všech zobrazitelných tabulek (z this.TableSequence, z jejího posledního prvku, z jeho property End)
        /// </summary>
        /// <returns></returns>
        private int _TablesPositionGetDataSize()
        {
            return this.TablesVisibleDataSize;
        }
        /// <summary>
        /// Metoda je volána poté, kdy dojde ke změně výšky tabulky, a Grid by měl reagovat v případě potřeby změnou výšky okolních tabulek, aby byl zajištěn režim AutoSize.
        /// Tato metoda "není líná jako celý GGrid", protože rovnou dopočítá výšky ostatních tabulek v TablesVisible (=nečeká až na přepočet "až to bude nutné").
        /// Tento dopočet je vhodný k tomu, aby Splitter (který Resize provádí) mohl reagovat na krajní meze přesunu a mohl tedy omezovat pohyb spliteru v rámci Gridu.
        /// </summary>
        /// <param name="gTable"></param>
        /// <param name="heightOld">Původní výška tabulky před změnou</param>
        /// <param name="heightNew">Nový výška tabulky po změně</param>
        internal void TableHeightChanged(GTable gTable, int heightOld, int heightNew)
        {
            if (this.TablesHasAutoSize)
            {   // Pokud máme režim AutoSize u tabulek v Gridu, pak změna výšky tabulky 01 se projeví jako opačná změna u tabulky 02:
                // Protože poslední tabulka v řadě nemá Resize splitter (dolní jej nemívá), pak nearTable bude tabulka následující za gTable:
                var tablesVisible = this.TablesVisible;
                GTable nearTable = tablesVisible.GetNearestItem(gTable);
                if (nearTable != null)
                {
                    ISequenceLayout nearItem = nearTable as ISequenceLayout;
                    int oldSize = nearItem.Size;
                    int newSize = oldSize - (heightNew - heightOld);
                    string sequence = this.TablesVisibleSequenceHeight;
                    Application.Log.AddInfo(this.GetType(), "TableHeightChanged", "Grid.SequenceHeight(After) = " + sequence, "HasNearTable", "NearTable.Size(Before) = " + oldSize.ToString(), "NearTable.Size(After) = " + newSize.ToString());
                    // nearItem.Size = newSize;

                    _TablesPositionCalculateAutoSize(fixedTable: gTable, variableTable: nearTable);
                }
            }
            this.Invalidate(InvalidateItem.TableHeight);
        }
        /// <summary>
        /// Umožní tabulkám aplikovat jejich AutoSize (pokud některá tabulka tuto vlastnost má), pro danou výšku viditelné oblasti
        /// </summary>
        /// <param name="tablesHeight">Výška prostoru pro tabulky</param>
        /// <param name="implicitAutoSize"></param>
        /// <param name="fixedTable">Tabulka, jejíž velikost bychom neradi měnili (anebo až jako poslední možnost)</param>
        /// <param name="variableTable">Tabulka, jejíž velikost bychom rádi upravili v první řadě (nehledě na příznak AutoSize)</param>
        private void _TablesPositionCalculateAutoSize(int? tablesHeight = null, ImplicitAutoSizeType? implicitAutoSize = null, GTable fixedTable = null, GTable variableTable = null)
        {
            if (!this.TablesHasAutoSize || !this.IsReadyToDraw) return;

            int targetHeight = (tablesHeight ?? DefaultTablesHeight);
            ImplicitAutoSizeType autoSize = implicitAutoSize ?? this.TablesAutoSize;

            bool isChanged = SequenceLayout.AutoSizeLayoutCalculate(this.TablesVisible, targetHeight, GTable.TableSplitterSize, autoSize, fixedTable, variableTable);
            Application.Log.AddInfo(this.GetType(), "_TablesPositionCalculateAutoSize", "Grid.SequenceHeight(After) = " + this.TablesVisibleSequenceHeight, "totalHeight = " + targetHeight.ToString(), "isChanged = " + isChanged.ToString());
            if (isChanged)
                this.Invalidate(InvalidateItem.TableHeight);
        }
        /// <summary>
        /// true pokud existuje nějaká tabulka s vlastností <see cref="ISequenceLayout.AutoSize"/> = true,
        /// anebo pokud režim <see cref="TablesAutoSize"/> je <see cref="ImplicitAutoSizeType.FirstItem"/> nebo <see cref="ImplicitAutoSizeType.LastItem"/>.
        /// Pokud je true, pak poslední tabulka nemá mít svůj dolní splitter, protože jeho chování v AutoSize gridu je nevhodné.
        /// <para/>
        /// Při <see cref="TablesHasAutoSize"/> = true se Grid chová tak, že při Resize některé z tabulek (pomocí jejího Splitteru) najde nejbližší tabulku, která se smí resizovat, 
        /// a upraví její výšku tak, aby vytvořila prostor pro aktuálně resizovanou tabulku. V podstatě se vždy resizuje tabulka pod aktuální tabulkou.
        /// </summary>
        protected bool TablesHasAutoSize { get { return ((this.TablesAutoSize == ImplicitAutoSizeType.FirstItem || this.TablesAutoSize == ImplicitAutoSizeType.LastItem) || this.Tables.Any(t => ((ISequenceLayout)t).AutoSize)); } }
        /// <summary>
        /// Eventhandler pro událost změny pozice svislého scrollbaru = posun pole tabulek nahoru/dolů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TablesScrollBar_ValueChange(object sender, GPropertyChangeArgs<DecimalNRange> e)
        {
            int offset = (int)this.TablesScrollBar.Value.Begin.Value;
            if (offset == this.TablesPositions.DataFirstPixel) return;
            this.TablesPositions.DataFirstPixel = offset;
            this.Invalidate(InvalidateItem.GridTablesScroll);
        }
        /// <summary>
        /// Soupis všech potenciálně viditelných grafických objektů tabulek, setříděný podle TableOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Potenciálně viditelná = aktuálně nemusí být vidět, ale může se na ni nascrollovat. Neobsahuje tedy tabulky, jejichž IsVisible je false.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou Invalidate(), ta se má volat po těchto akcích:
        /// Změna pořadí tabulek, Změna počtu tabulek, Změna IsVisible tabulek.
        /// Nemusí se volat při posunech svislého scrollbaru ani při resize gridu, ani při změně výšky tabulek!
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky).
        /// </summary>
        protected GTable[] TablesVisible { get { this._TablesVisibleCheck(); return this._TablesVisible; } }
        /// <summary>
        /// Stringem vyjádřené souřadnice viditelných tabulek v ose Y (Top, Height, Bottom)
        /// </summary>
        internal string TablesVisibleSequenceHeight
        {
            get
            {
                var tablesVisible = this._TablesVisible;
                if (tablesVisible == null) return "NULL";
                string result = "";
                for (int t = 0; t < tablesVisible.Length; t++)
                {
                    var table = tablesVisible[t];
                    var bounds = table.Bounds;
                    result += $"[{t}]: T={bounds.Top},H={bounds.Height},B={bounds.Bottom}; ";
                }
                return result;
            }
        }
        /// <summary>
        /// Součet velikostí (Height) všech tabulek v gridu.
        /// Jde o počet datových pixelů kolekce <see cref="TablesVisible"/>, 
        /// je určeno při nápočtu <see cref="SequenceLayout.SequenceLayoutCalculate(IEnumerable{ISequenceLayout}, int)"/> pro pole tabulek <see cref="TablesVisible"/>.
        /// </summary>
        protected int TablesVisibleDataSize { get { this._TablesVisibleDataSizeCheck(); return this._TablesVisibleDataSize.Value; } }
        /// <summary>
        /// Soupis aktuálně viditelných grafických objektů tabulek (viditelný = právě je zobrazeno prostoru v Gridu, nejsou zde tedy tabulky odscrollované dole nebo nahoře),
        /// setříděný podle TableOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End (=datová oblast).
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Tabulky mají korektně napočtenou hodnotu VisualRange = vizuální pozice na ose Y v rámci Gridu.
        /// Invalidace seznamu se provádí metodou Invalidate(), ta se má volat po těchto akcích:
        /// Invalidace Scroll nebo Resize tabulek.
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky).
        /// </summary>
        internal GTable[] TablesVisibleCurrent { get { this._TablesVisibleCurrentCheck(); return this._TablesVisibleCurrent; } }
        /// <summary>
        /// Ověří a zajistí připravenost pole <see cref="TablesVisible"/> a hodnoty <see cref="_TablesVisibleDataSize"/>.
        /// </summary>
        private void _TablesVisibleCheck(bool force = false)
        {
            GTable[] tablesVisible = this._TablesVisible;
            if (force || tablesVisible == null)
            {   // Tabulky v TablesVisible mají být sice VŠECHNY, ale jen ty všechny, které se uživateli mohou zobrazit při vhodném scrollování gridu nahoru/dolů.
                //  Proto v nich NESMÍ být tabulky, které jsou označeny jako IsVisible = false:
                List<GTable> tableList = this._Tables.Where(t => t.Is.Visible).ToList();
                if (tableList.Count > 1)
                    tableList.Sort(GTable.CompareOrder);
                tablesVisible = tableList.ToArray();
                this._TablesVisible = tablesVisible;
                this._TablesVisibleDataSize = null;
            }

            if (!this._TablesVisibleDataSize.HasValue)
            {
                this._TablesVisibleDataSize = SequenceLayout.SequenceLayoutCalculate(tablesVisible, GTable.TableSplitterSize);
                Application.Log.AddInfo(this.GetType(), "_TablesVisibleCheck", "Grid.SequenceHeight(After) = " + this.TablesVisibleSequenceHeight);
                this._TablesVisibleCurrent = null;
            }
        }
        /// <summary>
        /// Ověří a zajistí připravenost hodnoty <see cref="TablesVisibleDataSize"/>
        /// </summary>
        private void _TablesVisibleDataSizeCheck()
        {
            if (this._TablesVisibleDataSize.HasValue) return;

            GTable[] tables = this._TablesVisible;
            if (tables == null)
            {   // Protože není platná ani kolekce tabulek, předáme to tam:
                this._TablesVisibleCheck();
            }
            else
            {   // Kolekce tabulek je OK, pouze přepočteme SequenceLayoutCalculate a tím určíme this._TablesAllDataSize:
                this._TablesVisibleDataSize = SequenceLayout.SequenceLayoutCalculate(tables, GTable.TableSplitterSize);
                Application.Log.AddInfo(this.GetType(), "_TablesVisibleDataSizeCheck", "Grid.SequenceHeight(After) = " + this.TablesVisibleSequenceHeight);
                this._TablesVisibleCurrent = null;
            }
        }
        /// <summary>
        /// Ověří a zajistí připravenost pole <see cref="TablesVisibleCurrent"/>
        /// </summary>
        private void _TablesVisibleCurrentCheck()
        {
            if (this._TablesVisibleCurrent != null) return;

            List<GTable> visibleTables = new List<GTable>();
            GridPosition tablesPositions = this.TablesPositions;
            Int32Range dataVisibleRange = tablesPositions.DataVisibleRange;                          // Rozmezí datových pixelů, které jsou viditelné
            foreach (GTable table in this.TablesVisible)
            {
                if (table.DataTable.Visible)
                {
                    ISequenceLayout isl = table as ISequenceLayout;
                    bool isTableVisible = SequenceLayout.IsItemVisible(isl, dataVisibleRange);           // Tato tabulka je vidět?
                    table.VisualRange = (isTableVisible ? tablesPositions.GetVisualPosition(isl) : null);
                    if (isTableVisible)
                        visibleTables.Add(table);
                }
            }
            this._TablesVisibleCurrent = visibleTables.ToArray();

            Application.Log.AddInfo(this.GetType(), "_TablesVisibleCurrentCheck", "Grid.SequenceHeight(After) = " + this.TablesVisibleSequenceHeight);
        }
        /// <summary>
        /// Cache kolekce <see cref="TablesVisible"/>
        /// </summary>
        private GTable[] _TablesVisible;
        /// <summary>
        /// Počet datových pixelů kolekce <see cref="TablesVisible"/>, 
        /// je určeno při nápočtu <see cref="SequenceLayout.SequenceLayoutCalculate(IEnumerable{ISequenceLayout}, int)"/> pro pole tabulek <see cref="TablesVisible"/>.
        /// Pokud je null, pak není platný tento nápočet a je nutno jej přepočítat znovu.
        /// </summary>
        private int? _TablesVisibleDataSize;
        /// <summary>
        /// Cache kolekce <see cref="TablesVisibleCurrent"/>
        /// </summary>
        private GTable[] _TablesVisibleCurrent;
        /// <summary>
        /// Svislý Scrollbar pro posouvání pole tabulek nahoru/dolů (nikoli jejich řádků, na to má každá tabulka svůj vlastní Scrollbar).
        /// </summary>
        protected ScrollBar TablesScrollBar { get { this._TablesScrollBarCheck(); return this._TablesScrollBar; } }
        /// <summary>
        /// Ověří a zajistí připravenost dat v objektu TablesScrollBar.
        /// Pokud je nastavena jeho neplatnost (_TablesScrollBarDataValid je false), pak provede načtení dat z pozicioneru.
        /// Tato akce nevyvolá žádný event.
        /// Aktualizují se hodnoty TablesScrollBar: Bounds, ValueTotal, Value, IsEnabled
        /// </summary>
        private void _TablesScrollBarCheck()
        {
            if (this._TablesScrollBarDataValid) return;

            if (this.TablesScrollBarVisible)
                this._TablesScrollBar.LoadFrom(this.TablesPositions, this.TablesScrollBarBounds, true);

            this._TablesScrollBarDataValid = true;
        }
        /// <summary>
        /// Pozicování tabulek
        /// </summary>
        private GridPosition _TablesPositions;
        /// <summary>
        /// Scrollbar pro tabulky
        /// </summary>
        private ScrollBar _TablesScrollBar;
        /// <summary>
        /// true po naplnění RowsScrollBar platnými daty, false po invalidaci
        /// </summary>
        private bool _TablesScrollBarDataValid;
        #endregion
        #region Pozicování vodorovné - sloupce tabulek a dolní vodorovný scrollbar
        /// <summary>
        /// Inicializace pozic sloupců
        /// </summary>
        private void InitColumnsPositions()
        {
            this._ColumnsPositions = new GridPosition(GGrid.DefaultRowHeaderWidth, 28, this._ColumnPositionGetVisualSize, this._ColumnPositionGetDataSize);

            this._ColumnsScrollBar = new ScrollBar() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            this._ColumnsScrollBar.ValueChanging += new GPropertyChangedHandler<DecimalNRange>(ColumnsScrollBar_ValueChange);
            this._ColumnsScrollBar.ValueChanged += new GPropertyChangedHandler<DecimalNRange>(ColumnsScrollBar_ValueChange);
        }
        /// <summary>
        /// Řídící prvek pro Pozice sloupců
        /// </summary>
        public GridPosition ColumnsPositions { get { return this._ColumnsPositions; } }
        /// <summary>
        /// Vrací šířku vizuálního prostoru pro datové sloupce (= ClientSize.Width - ColumnsPositions.VisualFirstPixel - [šířka TablesScrollBar, pokud je viditelný])
        /// </summary>
        /// <returns></returns>
        private int _ColumnPositionGetVisualSize()
        {
            return this.ClientSize.Width - this.ColumnsPositions.VisualFirstPixel - (this.TablesScrollBarVisible ? ScrollBar.DefaultSystemBarWidth : 0);
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
        /// Zajistí synchronizaci šířek všech sloupců
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="defaultWidths"></param>
        private void _ColumnsPositionSizeRefresh(IEnumerable<GridColumn> columns = null, Dictionary<int, GridColumn> defaultWidths = null)
        {
            if (columns == null) columns = this.Columns;
            bool hasDefaults = (defaultWidths != null && defaultWidths.Count > 0);
            foreach (var column in columns)
            {
                int width = ((hasDefaults && defaultWidths.TryGetValue(column.ColumnId, out var defaultColumn)) ? defaultColumn.ColumnWidth : column.ColumnWidth);       // column.ColumnWidth get: jde z MasterColumn
                column.ColumnWidth = width;                                                                                                                              // column.ColumnWidth set: jde do všech Columns
            }
        }
        /// <summary>
        /// Umožní sloupcům aplikovat jejich AutoSize (pokud některý sloupec tuto vlastnost má), pro danou šířku viditelné oblasti
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="columnsWidth"></param>
        /// <param name="fixedColumn"></param>
        /// <param name="variableColumn"></param>
        private void _ColumnsPositionCalculateAutoSize(IEnumerable<GridColumn> columns = null, int? columnsWidth = null, GridColumn fixedColumn = null, GridColumn variableColumn = null)
        {
            if (!this.IsReadyToDraw) return;

            if (this.ColumnsHasAutoSize)
            {
                if (columns == null) columns = this.Columns;
                int targetWidth = (columnsWidth ?? DefaultColumnsWidth);
                bool isChanged = SequenceLayout.AutoSizeLayoutCalculate(columns, targetWidth, 0, ImplicitAutoSizeType.None, fixedColumn, variableColumn);
                if (isChanged)
                    this._ChildArrayValid = false;
            }
            this._ColumnsSizeValid = true;
        }
        /// <summary>
        /// Eventhandler volaný při/po změně hodnoty na vodorovném scrollbaru = posuny sloupců
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnsScrollBar_ValueChange(object sender, GPropertyChangeArgs<DecimalNRange> e)
        {
            int offset = (int)this.ColumnsScrollBar.Value.Begin.Value;
            if (offset == this.ColumnsPositions.DataFirstPixel) return;
            this.ColumnsPositions.DataFirstPixel = offset;
            this.Invalidate(InvalidateItem.GridColumnsScroll);
        }
        /// <summary>
        /// Dolní vodorovný Scrollbar, pro posuny sloupců doleva/doprava
        /// </summary>
        protected ScrollBar ColumnsScrollBar { get { this._ColumnsScrollBarCheck(); return this._ColumnsScrollBar; } }
        /// <summary>
        /// Ověří a zajistí připravenost dat v objektu ColumnsScrollBar.
        /// Pokud je nastavena jeho neplatnost, pak provede načtení dat z pozicioneru.
        /// Tato akce nevyvolá žádný event.
        /// Aktualizují se hodnoty ColumnsScrollBar: Bounds, ValueTotal, Value, IsEnabled
        /// </summary>
        private void _ColumnsScrollBarCheck()
        {
            if (this._ColumnsScrollBarDataValid) return;

            if (this.ColumnsScrollBarVisible)
                this._ColumnsScrollBar.LoadFrom(this.ColumnsPositions, this.ColumnsScrollBarBounds, true);

            this._ColumnsScrollBarDataValid = true;
        }
        /// <summary>
        /// Defaultní šířka pro zobrazení prostoru sloupců (od prvního pixelu pro vlastní sloupce po prostor svislého scrollbaru)
        /// </summary>
        protected int DefaultColumnsWidth { get { return this.ClientSize.Width - ScrollBar.DefaultSystemBarWidth - this.ColumnsPositions.VisualFirstPixel; } }
        /// <summary>
        /// Obsahuje true, pokud existuje sloupec s vlastností AutoSize
        /// </summary>
        protected bool ColumnsHasAutoSize { get { var columns = _AllColumns; return (columns != null && columns.Any(c => ((ISequenceLayout)c).AutoSize)); } }
        /// <summary>
        /// Pozicování sloupců
        /// </summary>
        private GridPosition _ColumnsPositions;
        /// <summary>
        /// ScrollBar sloupců
        /// </summary>
        private ScrollBar _ColumnsScrollBar;
        /// <summary>
        /// true po naplnění ColumnsScrollBar platnými daty, false po invalidaci
        /// </summary>
        private bool _ColumnsScrollBarDataValid;
        #endregion
        #region Obecná práce s časovými osami, hlavní časová osa, podpora pro časové osy v synchronizovaných sloupcích
        /// <summary>
        /// Synchronizační element časové osy.
        /// Pozor, smí být null, pokud GGrid není zapojen do synchronního okruhu.
        /// Pak změna jeho časové osy se nepromítne nikam jinam.
        /// </summary>
        public ValueTimeRangeSynchronizer SynchronizedTime
        {
            get { return this._SynchronizedTime; }
            set
            {
                // Odpojit od dosavadního synchronizátoru:
                if (this.HasSynchronizedTime)
                    this._SynchronizedTime.ValueChanging -= this._SynchronizedTime_ValueChanging;

                // Uložit nový synchronizátor:
                this._SynchronizedTime = value;

                // Napojit se do nového synchronizátoru:
                if (this.HasSynchronizedTime)
                    this._SynchronizedTime.ValueChanging += this._SynchronizedTime_ValueChanging;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this Grid má napojení na časový synchronizátor SynchronizedTime
        /// </summary>
        public bool HasSynchronizedTime { get { return (this._SynchronizedTime != null); } }
        /// <summary>
        /// Vyvolá RefreshTimeAxis pro všechny GTable, vyjma tabulky s daným TableId (ta se považuje za zdroj události, a řeší si svůj event jinak), a předá jim ID sloupce pro refresh.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce.
        /// Fyzicky se zavolá <see cref="GridColumn.OnChangeTimeAxis(int?, GPropertyChangeArgs{TimeRange})"/> pro daný columnId.
        /// Tato metoda dále zajistí synchronizaci hodnoty do <see cref="SynchronizedTime"/>, pokud daný sloupec používá synchronní časovou osu.
        /// </summary>
        /// <param name="tableId">identifikace tabulky, kde došlo ke změně</param>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(int? tableId, int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            if (this._TimeAxisValueIsChanging) return;                                   // Zabráníme rekurzi
            try
            {
                this._TimeAxisValueIsChanging = true;
                GridColumn gridColumn;
                if (this.TryGetGridColumn(columnId, out gridColumn))
                {
                    gridColumn.OnChangeTimeAxis(tableId, e);                             // Novou hodnotu promítneme do sousedních tabulek v this Gridu (vyjma tabulky tableId, ta událost vyvolala, a novou hodnotu už má vyřešenou interně)
                    if (this.HasSynchronizedTime && gridColumn.UseTimeAxisSynchronized)  // Pokud se má řešit synchronicita časové osy:
                        this.SynchronizedTime.SetValue(e.NewValue, this, e.EventSource); //  Vložíme novou hodnotou do synchronizátoru, ten si ji uloží a pošle event všem, kdo o to stojí (tedy i nám do metody _SynchronizedTime_ValueChanging)
                }
            }
            finally
            {
                this._TimeAxisValueIsChanging = false;
            }
        }
        /// <summary>
        /// Zajistí Refresh časové osy v jednotlivých tabulkách po změně viditelnosti jedné z těchto tabulek.
        /// </summary>
        internal void RefreshTimeRange()
        {
            if (this._TimeAxisValueIsChanging) return;                                   // Zabráníme rekurzi
            if (this._ColumnDict is null || this._ColumnDict.Count == 0) return;
            var syncTimeRange = this.SynchronizedTime?.Value;
            if (syncTimeRange is null) return;
            var syncColumns = this._ColumnDict.Values.Where(c => c.UseTimeAxisSynchronized).ToArray();
            if (syncColumns.Length == 0) return;

            try
            {
                this._TimeAxisValueIsChanging = true;
                foreach (var syncColumn in syncColumns)
                    syncColumn.RefreshTimeRange(syncTimeRange);
            }
            finally
            {
                this._TimeAxisValueIsChanging = false;
            }
        }
        /// <summary>
        /// Eventhandler vyvolaný po změně hodnoty (času) v synchronizátoru <see cref="SynchronizedTime"/>.
        /// Kdokoliv mohl změnit jeho hodnotu, a synchronizátor volá tento event do všech odběratelů.
        /// Autor změny může je uveden v parametru sender (pokud je znám).
        /// </summary>
        /// <param name="sender">Autor, který změnu vyvolal. Pokud jsme změnu vyvolali my, pak (sender == this), viz metoda <see cref="OnChangeTimeAxis(int?, int, GPropertyChangeArgs{TimeRange})"/>.</param>
        /// <param name="e"></param>
        private void _SynchronizedTime_ValueChanging(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            if (sender != null && Object.ReferenceEquals(sender, this)) return;          // Tuto změnu jsme vyvolali my => už na ni nebudeme reagovat!
            if (this._TimeAxisValueIsChanging) return;                                   // Zabráníme rekurzi
            // Zvenku došlo ke změně synchronizované hodnoty časové osy, my v našem Gridu to promítneme do všech synchronizovaných sloupců, ale ne do synchronizačního objektu:
            try
            {
                this._TimeAxisValueIsChanging = true;
                this.SynchronizedTimeConvertor.Value = e.NewValue;                       // Obecný časový konvertor
                // 1. Sloupce, které mají synchronní časovou osu:
                foreach (GridColumn gridColumn in this.Columns.Where(c => c.UseTimeAxisSynchronized))
                    gridColumn.OnChangeTimeAxis(null, e);                                // Novou hodnotu promítneme do VŠECH tabulek v this Gridu, do sloupců které používají synchronní časovou osu
                // 2. Tabulky, které zobrazují časový graf na pozadí řádků:
                foreach (GTable table in this.Tables.Where(t => t.UseBackgroundTimeAxis))
                    table.RefreshBackgroundTimeAxis(sender, e);
            }
            finally
            {
                this._TimeAxisValueIsChanging = false;
            }
        }
        /// <summary>
        /// V době změn hodnoty časové osy je zde true, aby se eventy nevolaly opakovaně ve vnořeném režimu.
        /// </summary>
        private bool _TimeAxisValueIsChanging;
        /// <summary>
        /// Synchronizovaný konvertor časových údajů
        /// </summary>
        public ITimeAxisConvertor SynchronizedTimeConvertor
        {
            get
            {
                if (this._SynchronizedTimeConvertor == null)
                {   // TimeConvertor pro tento Grid dosud neexistuje, vytvořím jej tedy nový, a napojím ho na synchronizovaný čas:
                    this._SynchronizedTimeConvertor = new TimeAxisConvertor(this._SynchronizedTime);
                }
                return this._SynchronizedTimeConvertor;
            }
        }
        /// <summary>
        /// Instance konvertoru času na prostor
        /// </summary>
        private TimeAxisConvertor _SynchronizedTimeConvertor;
        /// <summary>
        /// Instance synchronní hodnoty času přes veškeré objekty
        /// </summary>
        private ValueTimeRangeSynchronizer _SynchronizedTime;
        #endregion
        #region Invalidace, resety, refreshe
        /// <summary>
        /// Zajistí invalidaci dat this prvku, a jeho vykreslení včetně překreslení Host controlu <see cref="InteractiveControl"/>.
        /// </summary>
        public override void Refresh()
        {
            this.Invalidate(InvalidateItem.GridBounds | InvalidateItem.GridTablesChange | InvalidateItem.GridColumnsChange);
            base.Refresh();
        }
        /// <summary>
        /// Zajistí invalidaci položek po určité akci, která právě skončila
        /// </summary>
        /// <param name="items"></param>
        public void Invalidate(InvalidateItem items)
        {
            // Pokud bude nastaven tento bit OnlyForTable, znamená to, že tuto invalidaci rozeslal Grid do podřízených tabulek, a některá podřízená tabulka ji poslala zase do Gridu.
            if (items.HasFlag(InvalidateItem.OnlyForTable)) return;

            Application.Log.AddInfo(this.GetType(), nameof(Invalidate), "Grid.SequenceHeight(After) = " + this.TablesVisibleSequenceHeight, "InvalidateItems = " + items.ToString());

            bool callTables = false;
            bool repaint = false;

            if ((items & (InvalidateItem.GridBounds | InvalidateItem.GridInnerBounds)) != 0)
            {
                this._TableInnerLayoutValid = false;
                this._ChildArrayValid = false;
                repaint = true;
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridTablesChange))
            {
                this._TableInnerLayoutValid = false;
                this._TablesVisible = null;
                this._ChildArrayValid = false;
                repaint = true;
            }
            if (items.HasFlag(InvalidateItem.TableHeight))
            {   // Sem přijdu, když se změní výška nebo viditelnost některé z mých tabulek:
                this._TableInnerLayoutValid = false;
                this._TablesVisibleDataSize = null;
                this._TablesVisible = null;
                this._TablesVisibleCurrent = null;
                this._ChildArrayValid = false;
                repaint = true;
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridTablesScroll))
            {
                this._TablesVisibleCurrent = null;
                this._ChildArrayValid = false;
            }
            if (items.HasFlag(InvalidateItem.GridColumnsWidth))
            {
                this._ColumnsLayoutValid = false;
                this._ColumnsSizeValid = false;
                items |= InvalidateItem.ColumnWidth;
                repaint = true;
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridColumnsChange))
            {
                this._Columns = null;
                this._ChildArrayValid = false;
                items |= InvalidateItem.ColumnsCount;
                repaint = true;
                callTables = true;
            }
            if ((items & (InvalidateItem.GridColumnsScroll | InvalidateItem.ColumnScroll)) != 0)
            {
                this._ColumnsLayoutValid = false;
                this._ChildArrayValid = false;
                items |= (InvalidateItem.ColumnScroll | InvalidateItem.ColumnWidth);
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridItems))
            {
                this._ChildArrayValid = false;
            }
            if (items.HasAnyFlag(InvalidateItem.AnyRow))
            {
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.Paint))
            {
                callTables = true;
                repaint = true;
            }

            if (!items.HasFlag(InvalidateItem.OnlyForGrid) && callTables)                // Invalidaci tabulek volám jen tehdy, když aktuální invalidace není "Jen pro grid", a podle významu se má týkat i tabulek...
            {
                InvalidateItem itemsTable = items | InvalidateItem.OnlyForTable;         // Nastavím bit, že navazující invalidace se má provést už jen v tabulkách, ale nemá se volat do Gridu!   Viz začátek zdejší metody.
                foreach (GTable table in this._Tables.Where(t => t.Is.Visible))
                    table.InvalidateData(itemsTable);
            }

            if (repaint)
                this.Repaint();
        }
        #endregion
        #region Pole grafických prvků Childs - obsahuje všechny tabulky, jejich vzájemné oddělovače (Splitter), a scrollbary (sloupce vždy, tabulky podle potřeby)
        /// <summary>
        /// Pole grafických prvků v tomto gridu: tabulky, splittery (mezi tabulkami), scrollbary (svislý, vodorovný)
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.ChildArrayCheck(); return this.ChildList; } }
        /// <summary>
        /// Zajistí platnost pole sub-itemů.
        /// </summary>
        protected void ChildArrayCheck()
        {
            if (this._ChildArrayValid) return;
            this.ChildList.Clear();
            this._ChildItemsAddTables();                             // Tabulky
            this._ChildItemsAddTableSplitters();                     // Splittery mezi tabulkami
            this._ChildItemsAddColumnScrollBar();                    // ScrollBar vodorovný, pro sloupce
            this._ChildItemsAddTablesScrollBar();                    // ScrollBar svislý, pro tabulky
            this._ChildArrayValid = true;
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechny viditelné tabulky.
        /// Nepřidává splittery, ale naplní jejich souřadnice.
        /// </summary>
        protected void _ChildItemsAddTables()
        {
            Rectangle tablesBounds = this.TablesBounds;
            GTable[] tables = this.TablesVisibleCurrent;
            int count = tables.Length;
            for (int i = 0; i < count; i++)
            {
                GTable table = tables[i];
                bool isLast = (i == (count - 1));
                Rectangle tableBounds = _CreateBoundsForTable(tablesBounds, table.VisualRange, isLast);
                table.Bounds = tableBounds;
                table.TableSplitter.LoadFrom(tableBounds, RectangleSide.Bottom, true);
                this.ChildList.Add(table);
            }
        }
        /// <summary>
        /// Metoda vrátí souřadnice pro tabulku
        /// </summary>
        /// <param name="tablesBounds"></param>
        /// <param name="tableVisualRange"></param>
        /// <param name="isLast"></param>
        /// <returns></returns>
        protected Rectangle _CreateBoundsForTable(Rectangle tablesBounds, Int32Range tableVisualRange, bool isLast)
        {
            Rectangle tableBounds = Int32Range.GetRectangle(tablesBounds, tableVisualRange);
            if (isLast && tableBounds.Bottom < tablesBounds.Bottom)
                tableBounds.Height = tablesBounds.Bottom - tableBounds.Y;
            return tableBounds;
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechny potřebné splittery.
        /// Souřadnice splitterů jsou již připraveny, to řešila metoda this._ChildItemsAddTables()
        /// </summary>
        protected void _ChildItemsAddTableSplitters()
        {
            Rectangle tablesBounds = this.TablesBounds;
            GTable [] tables = this.TablesVisibleCurrent;
            int count = tables.Length;
            // Pokud Grid má tabulky v režimu AutoSize, pak poslední tabulka gridu nebude mít svůj TableSplitter. Chování Gridu s ním by bylo nevhodné!
            if (this.TablesHasAutoSize)
                count--;
            for (int i = 0; i < count; i++)
            {
                GTable table = tables[i];
                this.ChildList.Add(table.TableSplitter);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá ColumnScrollBar, pokud je viditelný.
        /// </summary>
        protected void _ChildItemsAddColumnScrollBar()
        {
            if (this.ColumnsScrollBarVisible)
                this.ChildList.Add(this.ColumnsScrollBar);
        }
        /// <summary>
        /// Do pole this.ChildList přidá TablesScrollBar, pokud je viditelný.
        /// </summary>
        protected void _ChildItemsAddTablesScrollBar()
        {
            if (this.TablesScrollBarVisible)
                this.ChildList.Add(this.TablesScrollBar);
        }
        /// <summary>
        /// Platnost dat v poli ChildItems
        /// </summary>
        private bool _ChildArrayValid;
        #endregion
        #region Interaktivita vlastního Gridu
        /// <summary>
        /// Interaktivita Gridu jako celku je potlačena
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            // GGrid sám o sobě není interaktivní. Interaktivní jsou jeho Childs.
        }
        #endregion
        #region Draw
        /// <summary>
        /// Vykreslí this <see cref="GGrid"/>
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            // GGrid sám o sobě se nevykresluje, leda v situaci kdy nemá žádnou tabulku:
            if (this.TablesCount == 0)
                base.Draw(e, absoluteBounds, absoluteVisibleBounds);
            else
                this.DrawVoidAreas(e);
        }
        /// <summary>
        /// Metoda vykreslí barvu pozadí do míst, kam moji ChildItems nekreslí nic svého.
        /// </summary>
        /// <param name="e"></param>
        protected void DrawVoidAreas(GInteractiveDrawArgs e)
        {
            Rectangle bounds1 = this.GridVoidBounds1;
            if (bounds1.HasPixels())
            { /* GPainter.DrawRectangle(e.Graphics, this.GetAbsoluteBounds(bounds1), Color.Violet); */ }

            Rectangle bounds2 = this.GridVoidBounds2;
            if (bounds2.HasPixels())
            { /*    GPainter.DrawRectangle(e.Graphics, this.GetAbsoluteBounds(bounds2), Color.Violet); // this.BackColor); */ }
        }
        #endregion
        #region Defaultní hodnoty
        /// <summary>
        /// Výchozí šířka sloupce RowHeader
        /// </summary>
        public static int DefaultRowHeaderWidth { get { return 32; } }
        /// <summary>
        /// Výška prostoru pro tabulky určená jako (<see cref="InteractiveObject.ClientSize"/>.Height - <see cref="ScrollBar.DefaultSystemBarHeight"/>)
        /// </summary>
        protected int DefaultTablesHeight { get { return (this.ClientSize.Height - ScrollBar.DefaultSystemBarHeight); } }
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
        /// <param name="gGrid"></param>
        /// <param name="columnId"></param>
        public GridColumn(GGrid gGrid, int columnId)
        {
            this._Grid = gGrid;
            this._ColumnList = new List<Column>();
            this._ColumnId = columnId;
        }
        /// <summary>
        /// Vztah na Grid, v němž je tento sloupec doma
        /// </summary>
        private GGrid _Grid;
        /// <summary>
        /// Soupis datových sloupců stejného ColumnId ze všech tabulek jednoho Gridu = "svislé pole" obsahující všechny synchronizované sloupce = pod sebou
        /// </summary>
        private List<Column> _ColumnList;
        /// <summary>
        /// Master column, nikdy není null
        /// </summary>
        private Column _MasterColumn;
        #endregion
        #region Public rozhraní: Master, properties, AddColumn(), CompareOrder()
        /// <summary>
        /// Grid, do něhož patří this column
        /// </summary>
        public GGrid Grid { get { return this._Grid; } }
        /// <summary>
        /// Hlavní sloupec = první sloupec nalezený s tímto ID
        /// </summary>
        public Column MasterColumn { get { return this._MasterColumn; } }
        /// <summary>
        /// ColumnId, synchronizační klíč všech sloupců v této tabulce
        /// </summary>
        public int ColumnId { get { return this._ColumnId; } } private int _ColumnId;
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
        /// Šířka tohoto sloupce výchozí, platná na začátku interaktivního procesu Resize.
        /// Běžně je null.
        /// </summary>
        public int? ColumnWidthOriginal { get; set; }
        /// <summary>
        /// Šířka tohoto sloupce při zobrazování. 
        /// Načítá se z MasterColumn.Size.
        /// Hodnotu lze i vložit, pak se vkládá do všech sloupců!
        /// Rozdíl: hodnota <see cref="ISequenceLayout.Size"/> reaguje na <see cref="IsVisible"/>, tedy pro <see cref="IsVisible"/> = false obsahuje <see cref="ISequenceLayout.Size"/> = 0.
        /// Naproti tomu <see cref="ColumnWidth"/> vrací šířku sloupce jako zadanou hodnotu i pro <see cref="IsVisible"/> = false, tedy ne 0.
        /// </summary>
        public int ColumnWidth
        {
            get { return ((ISequenceLayout)this.MasterColumn.ColumnHeader).Size; }
            set
            {
                foreach (Column column in this._ColumnList)
                    ((ISequenceLayout)column.ColumnHeader).Size = value;
            }
        }
        /// <summary>
        /// true pro viditelný sloupec (default), false for skrytý
        /// </summary>
        public bool IsVisible
        {
            get { return this._MasterColumn.IsVisible; }
            set
            {
                foreach (Column column in this._ColumnList)
                    column.IsVisible = value;
            }
        }
        /// <summary>
        /// true pokud se pro sloupec má zobrazit časová osa v záhlaví
        /// </summary>
        public bool UseTimeAxis { get { return this._MasterColumn.UseTimeAxis; } }
        /// <summary>
        /// Obsahuje true, pokud se pro sloupec má zobrazit časová osa v záhlaví, a tato časová osa se má synchronizovat do dalších Gridů a objektů.
        /// To je jen tehdy, když sloupec obsahuje časový graf (<see cref="Column.ColumnContent"/> == <see cref="ColumnContentType.TimeGraphSynchronized"/>).
        /// </summary>
        public bool UseTimeAxisSynchronized { get { return this._MasterColumn.UseTimeAxisSynchronized; } }
        /// <summary>
        /// Objekt, který provádí konverze časových údajů a pixelů, jde o vizuální časovou osu
        /// </summary>
        public ITimeAxisConvertor TimeConvertor { get { return this._MasterColumn.ColumnHeader.TimeConvertor; } }
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
        #region Volání změn do konkrétních podřízených sloupců Data.Column
        /// <summary>
        /// Vyvolá RefreshTimeAxis pro všechny GTable, vyjma tabulky s daným TableId (ta se považuje za zdroj události, a řeší si svůj event jinak), a předá jim ID sloupce pro refresh.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce.
        /// </summary>
        /// <param name="tableId">identifikace tabulky, kde došlo ke změně</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(int? tableId, GPropertyChangeArgs<TimeRange> e)
        {
            foreach (Column column in this._ColumnList)
            {
                if (!tableId.HasValue || (tableId.HasValue && tableId.Value != column.Table.GTable.TableId))
                {
                    column.Table.GTable.RefreshTimeAxis(column, e);
                }
            }
        }
        /// <summary>
        /// Aktualizuje hodnotu časové osy do svých fyzických columnů
        /// </summary>
        /// <param name="timeRange"></param>
        internal void RefreshTimeRange(TimeRange timeRange)
        {
            if (!this.UseTimeAxis) return;

            // Do každého sloupce pošleme požadavek na refresh se specifickým argumentem, který obsahuje jako OldValue jeho dosavadní hodnotu:
            foreach (Column column in this._ColumnList)
            {
                TimeRange oldTimeRange = (column.UseTimeAxis ? column?.ColumnHeader?.TimeAxis.Value : null);
                GPropertyChangeArgs<TimeRange> e = new GPropertyChangeArgs<TimeRange>(oldTimeRange, timeRange, EventSourceType.ApplicationCode | EventSourceType.ValueChange);
                column.Table.GTable.RefreshTimeAxis(column, e);
            }
        }
        #endregion
        #region ISequenceLayout - adapter: get čte data z Master sloupce, set ukládá data do všech sloupců
        int ISequenceLayout.Order
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn.ColumnHeader).Order;
            }
            set
            {
                foreach (Column column in this._ColumnList)
                    ((ISequenceLayout)column.ColumnHeader).Order = value;
            }
        }
        int ISequenceLayout.Begin
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn.ColumnHeader).Begin;
            }
            set
            {
                foreach (Column column in this._ColumnList)
                    ((ISequenceLayout)column.ColumnHeader).Begin = value;
            }
        }
        int ISequenceLayout.Size
        {
            get { return (this.IsVisible ? this.ColumnWidth : 0); }
            set { this.ColumnWidth = value; }
        }
        int ISequenceLayout.End
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn.ColumnHeader).End;
            }
        }
        bool ISequenceLayout.AutoSize
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn.ColumnHeader).AutoSize;
            }
        }
        #endregion
    }
    #endregion
    #region class GridPosition : Třída, která řídí zobrazení většího obsahu dat (typicky sada tabulek) v omezeném prostoru Controlu (typicky Grid, Tabulka)
    /// <summary>
    /// GridPosition : Třída, která řídí zobrazení většího obsahu dat (typicky sada tabulek) v omezeném prostoru Controlu (typicky Grid, Tabulka),
    /// kde část prostoru je vyhrazena pro záhlaví, další část pro data a další část pro zápatí.
    /// Prostor pro data je typicky spojen se Scrollbarem, do kterého se promítá poměr viditelné části ku celkovému množství dat, a aktuální pozice dat.
    /// Tato třída eviduje: velikost záhlaví (=vizuální začátek dat), velikost prostoru pro data, počáteční logickou pozici dat (od kterého pixelu jsou data viditelná),
    /// a provádí převody viditelných pixelů na pixely virtuální = datové.
    /// </summary>
    public class GridPosition : IScrollBarData
    {
        #region Konstrukce
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GridPosition(Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
            : this(0, DefaultDataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, null, null)
        { }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GridPosition(int firstPixel, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
            : this(firstPixel, DefaultDataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, null, null)
        { }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        /// <param name="getVisualFirstPixel">Metoda, která vrátí první viditelný pixel (VisualFirstPixel) z aplikace</param>
        /// <param name="setVisualFirstPixel">Metoda, která nastaví daný pixel jako první viditelný pixel (VisualFirstPixel) do aplikace</param>
        internal GridPosition(int firstPixel, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod, Func<int> getVisualFirstPixel, Action<int> setVisualFirstPixel)
            : this(firstPixel, DefaultDataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, getVisualFirstPixel, setVisualFirstPixel)
        { }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="dataSizeAddSpace">Přídavek k hodnotě DataSize (=celková velikost dat) v pixelech, který bude předáván do ScrollBaru.</param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        internal GridPosition(int firstPixel, int dataSizeAddSpace, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod)
            : this(firstPixel, dataSizeAddSpace, getVisualSizeMethod, getDataSizeMethod, null, null)
        {
        }
        /// <summary>
        /// Vytvoří pozicioner pro obsah.
        /// </summary>
        /// <param name="firstPixel">Pozice prvního vizuálního pixelu, kde se začínají zobrazovat data</param>
        /// <param name="dataSizeAddSpace"></param>
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
        /// <param name="getVisualFirstPixel"></param>
        /// <param name="setVisualFirstPixel"></param>
        internal GridPosition(int firstPixel, int dataSizeAddSpace, Func<int> getVisualSizeMethod, Func<int> getDataSizeMethod, Func<int> getVisualFirstPixel, Action<int> setVisualFirstPixel)
        {
            this.VisualFirstPixel = firstPixel;
            this._GetVisualSizeMethod = getVisualSizeMethod;
            this._GetDataSizeMethod = getDataSizeMethod;
            this._GetVisualFirstPixel = getVisualFirstPixel;
            this._SetVisualFirstPixel = setVisualFirstPixel;
            this._DataSizeAddSpace = dataSizeAddSpace;
            this._DataVisibleReserve = DefaultDataVisibleReserve;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            int visualBegin = this.VisualFirstPixel;
            int visualSize = this.VisualSize;
            Int32Range visualRange = new Int32Range(visualBegin, visualBegin + visualSize);
            int dataBegin = this.DataFirstPixel;
            int dataSize = this.DataSize;
            Int32Range dataRange = new Int32Range(dataBegin, dataBegin + dataSize);
            return "Visual Range: " + visualRange.ToString() + "; Data Range: " + dataRange.ToString();
        }
        #endregion
        #region Funkční provázanost s podkladovou grafickou a datovou vrstvou
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
        /// Metoda, která vrátí první viditelný pixel (VisualFirstPixel) z aplikace
        /// </summary>
        private Func<int> _GetVisualFirstPixel;
        /// <summary>
        /// Metoda, která nastaví daný pixel jako první viditelný pixel (VisualFirstPixel) do aplikace
        /// </summary>
        private Action<int> _SetVisualFirstPixel;
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
        /// <summary>
        /// Lokálně uložená hodnota VisualFirstPixel, pokud tato hodnota není provázaná s aplikací pomocí metod _GetVisualFirstPixel a _SetVisualFirstPixel
        /// </summary>
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
        /// Rozmezí DATOVÝCH pixelů, které spadají do viditelné oblasti.
        /// Begin = DataFirstPixel, End = DataFirstPixel + VisualSize.
        /// </summary>
        public Int32Range DataVisibleRange { get { int dfp = this.DataFirstPixel; int size = this.VisualSize; return new Int32Range(dfp, dfp + size); } }
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
        /// Záporná hodnota v DataSizeAddSpace zařídí, že daný bude zobrazen počet pixelů dat nahoře/vlevo:
        /// např. -40 zajistí, že při posunu scrollbaru na konec dráhy se zobrazí nahoře 40 pixelů dat, a celý zbytek bude prázdný.
        /// </summary>
        public int DataSizeAddSpace { get { return this._DataSizeAddSpace; } set { this._DataSizeAddSpace = value; } } private int _DataSizeAddSpace;
        /// <summary>
        /// Obsahuje počet pixelů, které se "přidávají" k DataSize, tj. obsahuje hodnotu z <see cref="DataSizeAddSpace"/>.
        /// Pokud by ale <see cref="DataSizeAddSpace"/> obsahovalo záporné číslo (tj. obsahuje počet datových pixelů v horní části poslední obrazovky dat),
        /// pak tato property <see cref="DataSizeBottomPixels"/> obsahuje "aktuální počet dolních pixelů" vypočtený z <see cref="VisualSize"/> mínus záporná hodnota <see cref="DataSizeAddSpace"/>.
        /// </summary>
        protected int DataSizeBottomPixels
        {
            get
            {
                int space = this._DataSizeAddSpace;
                if (space < 0) space = this.VisualSize + space;
                if (space < 0) space = 0;
                return space;
            }
        }
        /// <summary>
        /// Velikost vizuální rezervy při scrolování dat do viditelné oblasti (metoda ScrollDataToVisible()).
        /// Jde o počet pixelů před / za aktivním datovým prvkem, které se zobrazují ve viditelném prostoru proto, 
        /// aby uživatel dostal podvědomou informaci, že aktivní prvek ještě není první / poslední v řadě, ale že před / za ním jsou ještě nějaké další prvky.
        /// Výchozí hodnota = 12.
        /// Změna hodnoty nevede k přepočtům, ale ovlivní následující výpočty v metodě ScrollDataToVisible().
        /// Vložení záporných hodnot je ignorováno.
        /// </summary>
        public int DataVisibleReserve { get { return this._DataVisibleReserve; } set { if (value >= 0) this._DataVisibleReserve = value; } } private int _DataVisibleReserve;
        /// <summary>
        /// Defaultní přídavek k hodnotě DataSize = 26
        /// </summary>
        public static int DefaultDataSizeAddSpace { get { return 26; } }
        /// <summary>
        /// Defaultní vizuální rezerva = 12
        /// </summary>
        public static int DefaultDataVisibleReserve { get { return 12; } }
        #endregion
        #region Přepočty datových a vizuálních souřadnic, scrollování dat do viditelné oblasti, obecné scrollování dat
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
        /// <summary>
        /// Nastaví svoji hodnotu DataFirstPixel tak, aby daný prvek byl ve viditelném prostoru. 
        /// Pokud je prvek nyní zcela viditelný, nemění nic.
        /// Pokud je prvek viditelný jen částečně, nebo vůbec, pak se pokusí nastavit jej tak, aby byl vidět zcela.
        /// Pokud to nejde (jeho velikost je větší než dostupná vizuální velikost), pak nastaví pozici pro jeho Begin, a jeho End bude za koncem viditelné oblasti.
        /// Metoda vrací true = došlo ke změně DataFirstPixel, false = nebylo nutno měnit.
        /// </summary>
        /// <param name="item">Prvek</param>
        /// <returns></returns>
        public bool ScrollDataToVisible(ISequenceLayout item)
        {
            if (item == null) return false;
            Int32Range dataRange = new Int32Range(item.Begin, item.End);
            return this._ScrollDataToVisible(dataRange, this.DataVisibleReserve);
        }
        /// <summary>
        /// Nastaví svoji hodnotu DataFirstPixel tak, aby daný prvek byl ve viditelném prostoru. 
        /// Pokud je prvek nyní zcela viditelný, nemění nic.
        /// Pokud je prvek viditelný jen částečně, nebo vůbec, pak se pokusí nastavit jej tak, aby byl vidět zcela.
        /// Pokud to nejde (jeho velikost je větší než dostupná vizuální velikost), pak nastaví pozici pro jeho Begin, a jeho End bude za koncem viditelné oblasti.
        /// Metoda vrací true = došlo ke změně DataFirstPixel, false = nebylo nutno měnit.
        /// </summary>
        /// <param name="dataRange">Datové souřadnice prvku</param>
        /// <returns></returns>
        public bool ScrollDataToVisible(Int32Range dataRange)
        {
            return this._ScrollDataToVisible(dataRange, this.DataVisibleReserve);
        }
        /// <summary>
        /// Nastaví svoji hodnotu DataFirstPixel tak, aby daný prvek byl ve viditelném prostoru.
        /// </summary>
        /// <param name="dataRange"></param>
        /// <param name="visibleReserve"></param>
        /// <returns></returns>
        private bool _ScrollDataToVisible(Int32Range dataRange, int visibleReserve)
        {
            if (dataRange.Size <= 0) return false;                   // Neviditelný nebo chybný prvek nebude vidět nikdy

            Int32Range visibleRange = new Int32Range(this.DataFirstPixel, this.DataEndPixel);
            if (visibleRange.Size <= 0) return false;                // Nepoužitelný prostor pro zobrazení

            // Ergonomie chování říká:
            //  1. Pokud daný prvek je nyní zcela viditelný, pak nic neřeším a skončím (beze změny), neřeším rezervu
            //  2. Pokud daný prvek je nyní viditelný jen zčásti, zajistím jeho zobrazení celé včetně zobrazení "rezervy", viz níže
            //  3. Totéž pokud je zcela neviditelný.
            //  4. Pokud se prvek nachází nad současným zobrazeným prostorem (má menší souřadnice), naroluji ho do horní pozice
            //  5. A obdobně prvek umístěný dole naroluji na spodní pozici
            //  6. Pokud je velikost prvek větší než viditelná oblast, naroluji jeho Begin na začátek + rezerva
            if (dataRange.IsAllWithin(visibleRange)) return false;   // Řádek je viditelný, není co řešit

            int dataFirstPixel = this.DataFirstPixel;
            int visibleSize = visibleRange.Size;

            // Práce s rezervou: při základních výpočtech budeme mířit do prostoru zmenšeného o rezervu na obou stranách,
            //  po dokončení výpočtu posuneme výsledek o rezervu:
            if (visibleReserve < 0) visibleReserve = 0;              // Rezerva musí být v rozmezí 0 ÷ (velikost / 4)
            if (visibleReserve > (visibleSize / 4)) visibleReserve = (visibleSize / 4);
            int dataFirstPixelR = dataFirstPixel + visibleReserve;
            int visibleSizeR = visibleSize - (2 * visibleReserve);   // Viditelný prostor zmenšený o rezervu na obou stranách

            // Zarovnání prvního zobrazeného pixelu podle datových souřadnic prvek:
            if (dataRange.Begin < dataFirstPixelR)
            {   // Pokud data začínají před viditelným prostorem (s rezervou), pak začátek viditelného prostoru (s rezervou) dáme na začátek dat:
                dataFirstPixelR = dataRange.Begin;
            }
            else if (dataRange.End > (dataFirstPixelR + visibleSizeR))
            {   // Pokud data končí za viditelným prostorem (s rezervou), pak viditelný prostor dáme tak, aby končil na konci dat:
                if (dataRange.Size <= visibleSizeR)
                    // Pokud velikost dat je menší než velikost prostoru (s rezervou), pak zarovnáme data ke konci prostoru:
                    dataFirstPixelR = dataRange.End - visibleSizeR;
                else
                    // Pokud ale velikost dat je větší než prostor, pak data zarovnáme k začátku prostoru, a konec bude "přesahovat za viditelný prostor":
                    dataFirstPixelR = dataRange.Begin;
            }

            // Rezerva: hodnota "dataFirstPixelR" je s rezervou, určíme správnou hodnotu bez rezervy:
            dataFirstPixel = dataFirstPixelR - visibleReserve;
            if (dataFirstPixel < 0) dataFirstPixel = 0;

            // Není změna?
            if (dataFirstPixel == this.DataFirstPixel) return false;

            this.DataFirstPixel = dataFirstPixel;
            return true;
        }
        /// <summary>
        /// Metoda posune obsah dat o daný poměr zobrazené části.
        /// Například pro svislý scrollbar : 
        /// Hodnota ratio = +1.0 posune obsah "dolů" o celou stránku = na prvním pixelu nahoře bude po této změně ten pixel, který byl před změnou umístěn dole pod posledním viditelným pixelem.
        /// Hodnota ratio = -0.333 posune obsah "nahoru" o třetinu stránky.
        /// Metoda vrací true, pokud došlo ke změně.
        /// </summary>
        /// <param name="ratio"></param>
        public bool ScrollDataByRatio(decimal ratio)
        {
            int dLast = this.DataSize + this.DataSizeBottomPixels;             // Hodnota posledního datového pixelu, pro který má být alokováno místo
            int vSize = this.VisualSize;
            int shift = (int)(Math.Round(((decimal)vSize) * ratio, 0));        // Posun v pixelech
            int first = this.DataFirstPixel + shift;                           // První viditelný datový pixel po posunu
            if ((first + vSize) > dLast)                                       // Pokud po posunu by poslední viditelný pixel byl větší, než dLast...
                first = dLast - vSize;                                         //  ... pak první viditelný pixel bude takový, aby i ten poslední vyhovoval.
            if (first < 0) first = 0;

            bool isChange = (first != this.DataFirstPixel);
            if (isChange)
                this.DataFirstPixel = first;
            return isChange;
        }
        #endregion
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
    /// Člen gridu GGrid, u kterého je možno vložit referenci na GGrid
    /// </summary>
    public interface IGridMember
    {
        /// <summary>
        /// Reference na garfický grid
        /// </summary>
        GGrid GGrid { get; set; }
        /// <summary>
        /// ID tohoto prvku v rámci nadřízeného systému
        /// </summary>
        int Id { get; set; }
    }
    #endregion
    #region enum InvalidateItem
    /// <summary>
    /// Identifikace prvků, které se mají invalidovat
    /// </summary>
    [Flags]
    public enum InvalidateItem : ulong
    {
        /// <summary>Nic</summary>
        None = 0,
        /// <summary>Rozměry Gridu</summary>
        GridBounds = 1,
        /// <summary>Změna vnitřních rozměrů Gridu</summary>
        GridInnerBounds = GridBounds << 1,
        /// <summary>Změna v počtu tabulek nebo jejich pořadí v Gridu (volat po akcích: přidat / odebrat sloupec, přemístit sloupec)</summary>
        GridTablesChange = GridInnerBounds << 1,
        /// <summary>Změna v pozici tabulek v Gridu (volat po akcích: resize tabulky, scroll tabulek)</summary>
        GridTablesScroll = GridTablesChange << 1,
        /// <summary>Změna v počtu sloupců nebo jejich pořadí v Gridu (volat po akcích: přidat / odebrat sloupec, přemístit sloupec)</summary>
        GridColumnsChange = GridTablesScroll << 1,
        /// <summary>Změna v pozici sloupců v Gridu (volat po akcích: resize sloupce, scroll sloupců)</summary>
        GridColumnsScroll = GridColumnsChange << 1,
        /// <summary>Změna v šířce sloupců v Gridu (volat po akcích: resize sloupce)</summary>
        GridColumnsWidth = GridColumnsScroll << 1,
        /// <summary>Změna v ChildItems u Gridu, např. po přidání/odebrání tabulky nebo viditelnosti některého splitteru mezi tabulkami (po změně Table.AllowResize*): nejde o přepočty souřadnic ani invalidaci polí, pouze o žádost o invalidaci Child prvků gridu</summary>
        GridItems = GridColumnsWidth << 1,
        /// <summary>Souřadnice Tabulky (pouze její umístění, ale ne velikost = bez vlivu na vnitřní prvky)</summary>
        TablePosition = GridItems << 1,
        /// <summary>Velikost vnitřního prostoru Tabulky (má vliv na vnitřní prvky)</summary>
        TableSize = TablePosition << 1,
        /// <summary>Změna ve viditelnosti některého splitteru mezi sloupci nebo mezi řádky (po změně Table.AllowResize*): nejde o přepočty souřadnic ani invalidaci polí, pouze o žádost o invalidaci Child prvků tabulky</summary>
        TableItems = TableSize << 1,
        /// <summary>Počet tabulek v Gridu</summary>
        TablesCount = TableItems << 1,
        /// <summary>Pořadí tabulek v Gridu</summary>
        TableOrder = TablesCount << 1,
        /// <summary>Výška některé tabulky v Gridu</summary>
        TableHeight = TableOrder << 1,
        /// <summary>Posun tabulek</summary>
        TableScroll = TableHeight << 1,
        /// <summary>Obsah filtru TagFilter, po změně filtru v tabulce</summary>
        TableTagFilter = TableScroll << 1,
        /// <summary>Počet sloupců</summary>
        ColumnsCount = TableTagFilter << 1,
        /// <summary>Pořadí sloupců</summary>
        ColumnOrder = ColumnsCount << 1,
        /// <summary>Šířka sloupců</summary>
        ColumnWidth = ColumnOrder << 1,
        /// <summary>Posun sloupců</summary>
        ColumnScroll = ColumnWidth << 1,
        /// <summary>Výška záhlaví sloupců (ColumnHeader.Height)</summary>
        ColumnHeader = ColumnScroll << 1,
        /// <summary>Počet řádků v tabulce</summary>
        RowsCount = ColumnHeader << 1,
        /// <summary>Pořadí řádků (po setřídění)</summary>
        RowOrder = RowsCount << 1,
        /// <summary>Výška řádku</summary>
        RowHeight = RowOrder << 1,
        /// <summary>Posun řádků nahoru/dolů</summary>
        RowScroll = RowHeight << 1,
        /// <summary>Šířka záhlaví řádku (RowHeader.Width)</summary>
        RowHeader = RowScroll << 1,

        /// <summary>Pokud je nastaven tento bit, pak aktuální akce je určena pouze k provádění v Gridu, a Grid už nemá volat invalidaci do tabulek GTable. A pokud by ji Grid zavolal, pak ji GTable bude ignorovat.</summary>
        OnlyForGrid = RowHeader << 1,
        /// <summary>Pokud je nastaven tento bit, pak aktuální akce je určena pouze k provádění v GTable, a GTable už nemá volat invalidaci do Gridu. A pokud by ji zavolala, pak ji Grid bude ignorovat.</summary>
        OnlyForTable = OnlyForGrid << 1,

        /// <summary>Změna v obsahu některého pole tabulky: nejde o přepočty souřadnic ani invalidaci polí, pouze o žádost o nové vykreslení Gridu</summary>
        Paint = OnlyForTable << 1,

        /// <summary>Změna typu Grid</summary>
        AnyGrid = GridBounds | GridItems,
        /// <summary>Změna typu Table</summary>
        AnyTable = TablePosition | TableSize | TableItems | TablesCount | TableOrder | TableHeight | TableScroll,
        /// <summary>Změna typu Column</summary>
        AnyColumn = ColumnsCount | ColumnOrder | ColumnWidth | ColumnScroll | ColumnHeader,
        /// <summary>Změna typu Row</summary>
        AnyRow = RowsCount | RowOrder | RowHeight | RowScroll | RowHeader,

        /// <summary>Invalidovat všechny prvky, ale neprovádět nové vykreslení (to se provede po dokončení aktuální aktivity automaticky)</summary>
        AnyInvalidate = AnyGrid | AnyTable | AnyColumn | AnyRow,

        /// <summary>Invalidovat všechny prvky jedné tabulky, a na závěr vyžádat nové vykreslení (změna byla provedena z jiné části kódu, která neprovádí automatické vykreslení)</summary>
        Table = TablePosition | TableSize | TableTagFilter | TableHeight | TableItems | TableOrder | 
                ColumnsCount | ColumnOrder | ColumnWidth | ColumnScroll | ColumnHeader |
                RowsCount | RowOrder | RowHeight | RowScroll | RowHeader |
                Paint,

        /// <summary>
        /// Vcelku všechno
        /// </summary>
        All = AnyTable | AnyRow | AnyGrid | Table
    }
    #endregion
}
