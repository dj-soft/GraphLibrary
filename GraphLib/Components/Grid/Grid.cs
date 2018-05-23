using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;
using Djs.Common.Components.Grid;

namespace Djs.Common.Components
{
    // Filosofický základ pro obsluhu různých událostí: Grid je línej jako veš! 
    // Ten je tak línej, že když se dojde ke změně něčeho (třeba výšky některé tabulky), tak ta změna (v property Table.Height) zavolá "nahoru" že došlo k dané změně,
    //  to volání se dostane do GTable jako invalidace výšky tabulky, to vyvolá obdobné volání do Gridu, a Grid si jen líně poznamená: "Rozložení tabulek na výšku už neplatí".
    //  Současně s tím si poznamená: "Neplatí ani moje ChildItem prvky (protože některá další tabulka může/nemusí být vidět, protože se odsunula dolů).
    // Podobně se chová i GTable: poznamená si: moje vnitřní souřadnice ani moje ChildItem prvky nejsou platné.
    // Teprve až bude někdo chtít pracovat s něčím v Gridu nebo v jeho GTable (typicky: zjištění interaktivity prvků, vykreslení tabulky), tak si požádá o ChildItems,
    //  tam se zjistí že jsou neplatné, a Grid nebo GTable začne shánět platné údaje. 
    // Při tom zjistí, že je jich většina neplatných, a začne je přepočítávat z aktuálních reálných hodnot (fyzické rozměry, počet a velikost tabulek, pozice řádků, atd).

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
                items |= InvalidateItem.ColumnScroll | InvalidateItem.GridInnerBounds;

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

            this._TableInnerLayoutValid = true;                      // Normálně to patří až na konec metody. Ale některé komponenty mohou používat již částečně napočtené hodnoty, a pak bychom se zacyklili

            // Umožníme tabulkám aplikovat jejich hodnotu AutoSize při dané výšce viditelné oblasti:
            int ht = clientSize.Height - GScrollBar.DefaultSystemBarHeight;
            this._TablesPositionCalculateAutoSize(ht);

            // Určíme, zda bude zobrazen scrollbar vpravo (to je tehdy, když výška tabulek je větší než výška prostoru pro tabulky = (ClientSize.Height - ColumnsScrollBar.Bounds.Height)):
            //  Objekt TablesPositions tady provede dotaz na velikost dat (metoda this._TablesPositionGetDataSize()) a velikost viditelného prostoru (metoda this._TablesPositionGetVisualSize()).
            //  Velikost dat je dána tabulkami, resp. pozici End poslední tabulky v sekvenci this.TablesSequence, 
            //  velikost viditelného prostoru pro tabulky je dána (this.ClientSize.Height - this.ColumnsScrollBar.Bounds.Height), takže se vždy počítá s prostorem pro zobrazením vodorovného scrollbaru:
            this._TablesScrollBarVisible = this.TablesPositions.IsScrollBarActive;

            // Určíme souřadnice jednotlivých elementů:
            int x0 = 0;                                              // x0: úplně vlevo
            int x1 = this.ColumnsPositions.VisualFirstPixel;         // x1: zde začíná ColumnsScrollBar (hned za koncem RowHeaderColumn)
            int x3 = clientSize.Width;                               // x3: úplně vpravo
            int x2t = x3 - GScrollBar.DefaultSystemBarWidth;         // x2t: zde začíná TablesScrollBar (vpravo, hned za koncem ColumnsScrollBar), tedy pokud by byl zobrazen
            int x2r = (this._TablesScrollBarVisible ? x2t : x3);     // x2r: zde reálně končí oblast prostoru pro tabulky a končí zde i ColumnsScrollBar, se zohledněním aktuální viditelnosti TablesScrollBaru
            int y0 = 0;                                              // y0: úplně nahoře
            int y1 = y0;                                             // y1: zde začíná prostor pro tabulky i TablesScrollBar 
            int y3 = clientSize.Height;                              // y3: úplně dole
            int y2 = y3 - GScrollBar.DefaultSystemBarHeight;         // y2: zde začíná ColumnsScrollBar (dole, hned za koncem prostoru pro tabulky)

            // Umožníme sloupcům aplikovat jejich hodnotu AutoSize při dané šířce viditelné oblasti:
            int wt = (x2r - x1);                                     // wt: šířka tabulky (včetně svislého scrollbaru pro řádky)
            int wc = wt - GScrollBar.DefaultSystemBarWidth;          // wc: šířka dat tabulky (viditelný prostor bez svislého scrollbaru pro řádky)
            this._ColumnsPositionCalculateAutoSize(wc);

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
            this._TablesVisible = null;
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
            IGridMember igm = args.Item as IGridMember;
            int id = this._TableID++;
            if (igm != null)
            {
                igm.GGrid = this;
                igm.Id = id;
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
            IGridMember igm = args.Item as IGridMember;
            if (igm != null)
            {
                igm.GGrid = null;
                igm.Id = -1;
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
        #region Sloupce Gridu
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

                // Zajistím provedení nápočtu pozic (ISequenceLayout.Begin, End):
                SequenceLayout.SequenceLayoutCalculate(columnList);

                // Na závěr je nutno uložit vypočtená data:
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
            // Pokud jsem až dosud nepřidal sourceColumn, přidám jej na konec:
            if (!isSourceAdded) { reorderedColumns.Add(sourceColumn); isSourceAdded = true; }

            // Dosud jsme nezměnili žádné ColumnOrder, ale máme korektně seřazenou kolekci = takže do ní vepíšeme novou hodnotu ColumnOrder:
            int columnOrder = 0;
            foreach (GridColumn gc in reorderedColumns)
                gc.ColumnOrder = columnOrder++;

            // Zajistit invalidaci a překresení:
            this.Invalidate(InvalidateItem.GridColumnsChange);

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
                this.Invalidate(InvalidateItem.GridColumnsScroll);
            }
            return isChanged;
        }
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

            this._TablesScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Vertical };
            this._TablesScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(TablesScrollBar_ValueChange);
            this._TablesScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(TablesScrollBar_ValueChange);
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
            return this.ClientSize.Height - GScrollBar.DefaultSystemBarHeight;
        }
        /// <summary>
        /// Vrací výšku všech zobrazitelných tabulek (z this.TableSequence, z jejího posledního prvku, z jeho property End)
        /// </summary>
        /// <returns></returns>
        private int _TablesPositionGetDataSize()
        {
            return this.TablesAllDataSize;
        }
        /// <summary>
        /// Metoda je volána poté, kdy dojde ke změně výšky tabulky, a Grid by měl reagovat v případě potřeby změnou výšky okolních tabulek, aby byl zajištěn režim AutoSize
        /// </summary>
        /// <param name="gTable"></param>
        internal void TableHeightChanged(GTable gTable)
        {


            this.Invalidate(InvalidateItem.TableHeight);
        }
        /// <summary>
        /// Umožní tabulkám aplikovat jejich AutoSize (pokud některá tabulka tuto vlastnost má), pro aktuální výšku viditelné oblasti
        /// </summary>
        private void _TablesPositionCalculateAutoSize()
        {
            int height = this.ClientSize.Height - GScrollBar.DefaultSystemBarHeight;
            this._TablesPositionCalculateAutoSize(height);
        }
        /// <summary>
        /// Umožní tabulkám aplikovat jejich AutoSize (pokud některá tabulka tuto vlastnost má), pro danou výšku viditelné oblasti
        /// </summary>
        /// <param name="height">Výška prostoru pro tabulky</param>
        private void _TablesPositionCalculateAutoSize(int height)
        {
            bool isChanged = SequenceLayout.AutoSizeLayoutCalculate(this.Tables, height, this.TablesAutoSize);
            if (isChanged)
                this._ChildArrayValid = false;
        }
        /// <summary>
        /// Eventhandler pro událost změny pozice svislého scrollbaru = posun pole tabulek nahoru/dolů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TablesScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.TablesScrollBar.Value.Begin.Value;
            if (offset == this.TablesPositions.DataFirstPixel) return;
            this.TablesPositions.DataFirstPixel = offset;
            this.Invalidate(InvalidateItem.GridTablesScroll);
        }
        /// <summary>
        /// Soupis všech grafických objektů tabulek, setříděný podle TableOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End.
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Invalidace seznamu se provádí metodou Invalidate(), ta se má volat po těchto akcích:
        /// Změna pořadí tabulek, Změna počtu tabulek.
        /// Nemusí se volat při posunech svislého scrollbaru ani při resize gridu, ani při změně výšky tabulek!
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky).
        /// </summary>
        protected GTable[] TablesAll { get { this._TablesAllCheck(); return this._TablesAll; } }
        protected int TablesAllDataSize { get { this._TablesAllDataSizeCheck(); return this._TablesAllDataSize.Value; } }
        /// <summary>
        /// Soupis viditelných grafických objektů tabulek, setříděný podle TableOrder, se správně napočtenou hodnotou ISequenceLayout.Begin a End (=datová oblast).
        /// Tento seznam se ukládá do místní cache, jeho generování se provádí jen jedenkrát po jeho invalidaci.
        /// Tabulky mají korektně napočtenou hodnotu VisualRange = vizuální pozice na ose Y v rámci Gridu.
        /// Invalidace seznamu se provádí metodou Invalidate(), ta se má volat po těchto akcích:
        /// Invalidace Scroll nebo Resize tabulek.
        /// Tato property nikdy nevrací null, ale může vrátit kolekci s počtem = 0 prvků (pokud neexistují tabulky).
        /// </summary>
        protected GTable[] TablesVisible { get { this._TablesVisibleCheck(); return this._TablesVisible; } }
        /// <summary>
        /// Ověří a zajistí připravenost pole TablesAll
        /// </summary>
        private void _TablesAllCheck()
        {
            GTable[] tables = this._TablesAll;
            if (tables == null)
            {
                List<GTable> tableList = this._Tables.ToList();
                if (tableList.Count > 1)
                    tableList.Sort(GTable.CompareOrder);
                tables = tableList.ToArray();
                this._TablesAll = tables;
                this._TablesAllDataSize = null;
            }
            if (!this._TablesAllDataSize.HasValue)
            {
                this._TablesAllDataSize = SequenceLayout.SequenceLayoutCalculate(tables, GTable.TableSplitterSize);
                this._TablesVisible = null;
            }
        }
        /// <summary>
        /// Ověří a zajistí připravenost hodnoty TablesAllDataSize
        /// </summary>
        private void _TablesAllDataSizeCheck()
        {
            if (this._TablesAllDataSize.HasValue) return;

            GTable[] tables = this._TablesAll;
            if (tables == null)
            {   // Protože není platná ani kolekce tabulek, předáme to tam:
                this._TablesAllCheck();
            }
            else
            {   // Kolekce tabulek je OK, pouze přepočteme SequenceLayoutCalculate a tím určíme this._TablesAllDataSize:
                this._TablesAllDataSize = SequenceLayout.SequenceLayoutCalculate(tables, GTable.TableSplitterSize);
                this._TablesVisible = null;
            }
        }
        /// <summary>
        /// Ověří a zajistí připravenost pole TablesVisible
        /// </summary>
        private void _TablesVisibleCheck()
        {
            if (this._TablesVisible != null) return;

            List<GTable> visibleTables = new List<GTable>();
            GridPosition tablesPositions = this.TablesPositions;
            Int32Range dataVisibleRange = tablesPositions.DataVisibleRange;                          // Rozmezí datových pixelů, které jsou viditelné
            foreach (GTable table in this.TablesAll)
            {
                ISequenceLayout isl = table as ISequenceLayout;
                bool isTableVisible = SequenceLayout.IsItemVisible(isl, dataVisibleRange);           // Tato tabulka je vidět?
                table.VisualRange = (isTableVisible ? tablesPositions.GetVisualPosition(isl) : null);
                if (isTableVisible)
                    visibleTables.Add(table);
            }
            this._TablesVisible = visibleTables.ToArray();
        }
        /// <summary>
        /// Cache kolekce TablesAll
        /// </summary>
        private GTable[] _TablesAll;
        /// <summary>
        /// Počet datových pixelů kolekce _TablesAll, je určeno při nápočtu SequenceLayoutCalculate().
        /// Pokud je null, pak není platný tento nápočet a je nutno jej přepočítat znovu.
        /// </summary>
        private int? _TablesAllDataSize;
        /// <summary>
        /// Cache kolekce TablesVisible
        /// </summary>
        private GTable[] _TablesVisible;
        /// <summary>
        /// Svislý Scrollbar pro posouvání pole tabulek nahoru/dolů (nikoli jejich řádků, na to má každá tabulka svůj vlastní Scrollbar).
        /// </summary>
        protected GScrollBar TablesScrollBar { get { this._TablesScrollBarCheck(); return this._TablesScrollBar; } }
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
        private GScrollBar _TablesScrollBar;
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

            this._ColumnsScrollBar = new GScrollBar() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            this._ColumnsScrollBar.ValueChanging += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
            this._ColumnsScrollBar.ValueChanged += new GPropertyChanged<SizeRange>(ColumnsScrollBar_ValueChange);
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
        /// Umožní sloupcům aplikovat jejich AutoSize (pokud některý sloupec tuto vlastnost má), pro aktuální šířku viditelné oblasti
        /// </summary>
        private void _ColumnsPositionCalculateAutoSize()
        {
            int width = this._ColumnsScrollBarBounds.Width;
            this._ColumnsPositionCalculateAutoSize(width);
        }
        /// <summary>
        /// Umožní sloupcům aplikovat jejich AutoSize (pokud některý sloupec tuto vlastnost má), pro danou šířku viditelné oblasti
        /// </summary>
        /// <param name="width"></param>
        private void _ColumnsPositionCalculateAutoSize(int width)
        {
            bool isChanged = SequenceLayout.AutoSizeLayoutCalculate(this.Columns, width);
            if (isChanged)
                this._ChildArrayValid = false;
        }
        /// <summary>
        /// Eventhandler volaný při/po změně hodnoty na vodorovném scrollbaru = posuny sloupců
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnsScrollBar_ValueChange(object sender, GPropertyChangeArgs<SizeRange> e)
        {
            int offset = (int)this.ColumnsScrollBar.Value.Begin.Value;
            if (offset == this.ColumnsPositions.DataFirstPixel) return;
            this.ColumnsPositions.DataFirstPixel = offset;
            this.Invalidate(InvalidateItem.GridColumnsScroll);
        }
        /// <summary>
        /// Dolní vodorovný Scrollbar, pro posuny sloupců doleva/doprava
        /// </summary>
        protected GScrollBar ColumnsScrollBar { get { this._ColumnsScrollBarCheck(); return this._ColumnsScrollBar; } }
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
        /// Pozicování sloupců
        /// </summary>
        private GridPosition _ColumnsPositions;
        /// <summary>
        /// ScrollBar sloupců
        /// </summary>
        private GScrollBar _ColumnsScrollBar;
        /// <summary>
        /// true po naplnění ColumnsScrollBar platnými daty, false po invalidaci
        /// </summary>
        private bool _ColumnsScrollBarDataValid;
        #endregion
        #region Obecná práce s časovými osami, hlavní časová osa, podpora pro časové osy v synchronizovaných sloupcích
        /// <summary>
        /// Vyvolá RefreshTimeAxis pro všechny GTable, předá jim ID sloupce pro refresh.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce.
        /// </summary>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            this.OnChangeTimeAxis(null, columnId, e);
        }
        /// <summary>
        /// Vyvolá RefreshTimeAxis pro všechny GTable, vyjma tabulky s daným TableId (ta se považuje za zdroj události, a řeší si svůj event jinak), a předá jim ID sloupce pro refresh.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce.
        /// </summary>
        /// <param name="tableId">identifikace tabulky, kde došlo ke změně</param>
        /// <param name="columnId">Identifikace sloupce</param>
        /// <param name="e">Data o změně</param>
        internal void OnChangeTimeAxis(int? tableId, int columnId, GPropertyChangeArgs<TimeRange> e)
        {
            GridColumn gridColumn;
            if (this.TryGetGridColumn(columnId, out gridColumn))
                gridColumn.OnChangeTimeAxis(tableId, e);
        }
        /// <summary>
        /// Vrací ITimeConvertor pro daný sloupec.
        /// Pokud daný sloupec nepoužívá časovou osu (nebo sloupec pro columnId neexistuje), vrací null.
        /// De facto jde o TimeAxis z první tabulky z daného sloupce.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        internal ITimeConvertor GetTimeConvertor(int columnId)
        {
            GridColumn gridColumn;
            if (!this.TryGetGridColumn(columnId, out gridColumn)) return null;
            if (!gridColumn.UseTimeAxis) return null;
            return gridColumn.TimeConvertor;
        }
        /// <summary>
        /// Obsahuje hlavní časovou osu celého Gridu.
        /// Je to ITimeConvertor z prvního sloupce, který používá časovou osu (má UseTimeAxis == true).
        /// </summary>
        internal ITimeConvertor MainTimeAxis
        {
            get
            {
                GridColumn timeAxisColumn = this.Columns.FirstOrDefault(c => c.UseTimeAxis);
                return (timeAxisColumn != null ? timeAxisColumn.TimeConvertor : null);
            }
        }
        #endregion
        #region Invalidace, resety, refreshe
        /// <summary>
        /// Zajistí invalidaci položek po určité akci, která právě skončila
        /// </summary>
        /// <param name="items"></param>
        public void Invalidate(InvalidateItem items)
        {
            // Pokud bude nastaven tento bit OnlyForTable, znamená to, že tuto invalidaci rozeslal Grid do podřízených tabulek, a některá podřízená tabulka ji poslala zase do Gridu.
            if (items.HasFlag(InvalidateItem.OnlyForTable)) return;

            bool callTables = false;

            if ((items & (InvalidateItem.GridBounds | InvalidateItem.GridInnerBounds)) != 0)
            {
                this._TableInnerLayoutValid = false;
                this._ChildArrayValid = false;
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridTablesChange))
            {
                this._TableInnerLayoutValid = false;
                this._TablesAll = null;
                this._ChildArrayValid = false;
            }
            if (items.HasFlag(InvalidateItem.TableHeight))
            {
                this._TableInnerLayoutValid = false;
                this._TablesAllDataSize = null;
                this._TablesVisible = null;
                this._ChildArrayValid = false;
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridTablesScroll))
            {
                this._TablesVisible = null;
                this._ChildArrayValid = false;
            }
            if (items.HasFlag(InvalidateItem.GridColumnsChange))
            {
                this._Columns = null;
                this._ChildArrayValid = false;
                items |= InvalidateItem.ColumnsCount;
                callTables = true;
            }
            if (items.HasFlag(InvalidateItem.GridColumnsScroll))
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
            if (items.HasFlag(InvalidateItem.Paint))
            {
                callTables = true;
            }

            if (!items.HasFlag(InvalidateItem.OnlyForGrid) && callTables)                // Invalidaci tabulek volám jen tehdy, když aktuální invalidace není "Jen pro grid", a podle významu se má týkat i tabulek...
            {
                InvalidateItem itemsTable = items | InvalidateItem.OnlyForTable;         // Nastavím bit, že navazující invalidace se má provést už jen v tabulkách, ale nemá se volat do Gridu!   Viz začátek zdejší metody.
                foreach (GTable table in this._Tables.Where(t => t.IsVisible))
                    table.Invalidate(itemsTable);
            }
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
            foreach (GTable table in this.TablesVisible)
            {
                Rectangle tableBounds = Int32Range.GetRectangle(tablesBounds, table.VisualRange);
                table.Bounds = tableBounds;
                table.TableSplitter.LoadFrom(tableBounds, RectangleSide.Bottom, true);
                this.ChildList.Add(table);
            }
        }
        /// <summary>
        /// Do pole this.ChildList přidá všechny potřebné splittery.
        /// Souřadnice splitterů jsou již připraveny, to řešila metoda this._ChildItemsAddTables()
        /// </summary>
        protected void _ChildItemsAddTableSplitters()
        {
            Rectangle tablesBounds = this.TablesBounds;
            GTable [] tables = this.TablesVisible;
            int count = tables.Length;
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
                this.Host.FillRectangle(e.Graphics, this.GetAbsoluteBounds(bounds1), Color.Violet); // this.BackColor);

            Rectangle bounds2 = this.GridVoidBounds2;
            if (bounds2.HasPixels())
                this.Host.FillRectangle(e.Graphics, this.GetAbsoluteBounds(bounds2), Color.Violet); // this.BackColor);
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
        /// Objekt, který provádí konverze časových údajů a pixelů, jde o vizuální časovou osu
        /// </summary>
        public ITimeConvertor TimeConvertor { get { return this._MasterColumn.ColumnHeader.TimeConvertor; } }
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
        #region Volání 
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
        bool ISequenceLayout.AutoSize
        {
            get
            {
                return ((ISequenceLayout)this.MasterColumn).AutoSize;
            }
        }
        #endregion
    }
    #endregion
    #region class GridPosition : Třída, která řídí zobrazení většího obsahu dat (typicky sada řádků) v omezeném prostoru Controlu (typicky Grid, Tabulka)
    /// <summary>
    /// GridPosition : Třída, která řídí zobrazení většího obsahu dat (typicky sada řádků) v omezeném prostoru Controlu (typicky Grid, Tabulka),
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
        /// <param name="getVisualSizeMethod">Metoda, která vrátí počet vizuálních pixelů, na nichž se zobrazují data. Jde o čistý prostor pro data, nezahrnuje žádný Header ani Footer nebo Scrollbar.</param>
        /// <param name="getDataSizeMethod">Metoda, která vrátí velikost dat = počet pixelů, které by obsadila data zobrazená najednou. Bez rezervy, bez přídavku.</param>
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
        /// </summary>
        public int DataSizeAddSpace { get { return this._DataSizeAddSpace; } set { this._DataSizeAddSpace = value; } } private int _DataSizeAddSpace;
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
        #region Přepočty datových a vizuálních souřadnic, scrollování dat do viditelné oblasti
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
        /// <summary>Změna v ChildItems u Gridu, např. po přidání/odebrání tabulky nebo viditelnosti některého splitteru mezi tabulkami (po změně Table.AllowResize*): nejde o přepočty souřadnic ani invalidaci polí, pouze o žádost o invalidaci Child prvků gridu</summary>
        GridItems = GridColumnsScroll << 1,
        /// <summary>Souřadnice Tabulky (pouze její umístění, ale ne velikost = bez vlivu na vnitřní prvky)</summary>
        TablePosition = GridItems << 1,
        /// <summary>Velikost vnitřního prostoru Tabulky (má vlivu na vnitřní prvky)</summary>
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
        /// <summary>Počet sloupců</summary>
        ColumnsCount = TableScroll << 1,
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
        AnyInvalidate = AnyGrid| AnyTable | AnyColumn | AnyRow,

        /// <summary>Invalidovat všechny prvky, a na závěr vyžádat nové vykreslení (změna byla provedena z jiné části kódu, která neprovádí automatické vykreslení)</summary>
        All = 0x0FFFFF,
    }
    #endregion
}
