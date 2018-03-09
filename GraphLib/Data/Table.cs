using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Djs.Common.Components;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Djs.Common.Data.New
{
    // Tento soubor obsahuje datové prvky pro tabulku s daty. 
    // To jest: Tabulka, Sloupec, Řádek, Buňka. 
    // A dále interface pro vizualizaci tabulky, které datová tabulka implementuje, a podporu pro třídění.
    // Datové prvky mají sadu datových public property, ale současně slouží jako nativní podklad pro grafické objekty (GGrid, GTable).
    // Pro jejich požadavky jsou do datových prvků explicitně implementovány rozličné interface, 
    //  takže grafické prvky si objekty přetypují a pracují přes interface, a tyto sady rozšiřujících prvků jsou uživateli skryté při běžné datové práci (=bez interface).

    #region Table
    /// <summary>
    /// Table : jedna tabulka s daty (sada Column + Row)
    /// </summary>
    public class Table : IVisualMember, ISequenceLayout, IContentValidity
    {
        #region Konstruktor, Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Table()
        {
            this._TableInit();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="tableName"></param>
        public Table(string tableName)
        {
            this._TableName = tableName;
            this._TableInit();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _TableInit()
        {
            this._ColumnsInit();
            this._RowsInit();
            this._LayoutInit();
        }
        /// <summary>
        /// Název tabulky, podle něj lze hledat. jde o klíčové slovo, nikoli popisek (Caption)
        /// </summary>
        public string TableName { get { return this._TableName; } set { this._TableName = value; } }
        private string _TableName;
        #endregion
        #region GTable - Linkování datové tabulky do grafické tabulky
        /// <summary>
        /// Reference na vizuální tabulku (GTable), může být null
        /// </summary>
        public Components.Grid.GTable GTable
        {
            get { return this._GTable; }
            internal set { this._GTableLink(value); }
        }
        private Components.Grid.GTable _GTable;
        /// <summary>
        /// true pokud má referenci na vizuální tabulku (GTable)
        /// </summary>
        public bool HasGTable { get { return (this._GTable != null); } }
        /// <summary>
        /// Napojí se na danou vizuální tabulku (GTable)
        /// </summary>
        /// <param name="gTable"></param>
        protected void _GTableLink(Components.Grid.GTable gTable)
        {
            if (this._GTable != null)
            {   // Odpojit starou
            }

            this._GTable = gTable;

            if (this._GTable != null)
            {   // Napojit novou
            }
        }
        #endregion
        #region Columns - soupis sloupců tabulky, přidávání, odebírání
        /// <summary>
        /// Inicializace dat pro sloupce
        /// </summary>
        private void _ColumnsInit()
        {
            this._Columns = new EList<Column>();
            this._Columns.ItemAddAfter += this._ColumnAddAfter;
            this._Columns.ItemRemoveAfter += this._ColumnRemoveAfter;
            this._ColumnsId = 0;
            this._ColumnIdDict = new Dictionary<int, Column>();
            this._ColumnNameDict = new Dictionary<string, Column>();
        }
        /// <summary>
        /// Kolekce sloupců, nový sloupec lze přidat i sem
        /// </summary>
        public EList<Column> Columns { get { return this._Columns; } }
        /// <summary>
        /// Seznam sloupců
        /// </summary>
        private EList<Column> _Columns;
        /// <summary>
        /// ID pro nové sloupce, výchozí = 0 = index pro první přidaný sloupec. Vždy se jen navyšuje. Po odebrání sloupce se jeho ID již nepoužije.
        /// </summary>
        private int _ColumnsId;
        /// <summary>
        /// Index sloupců dle ID
        /// </summary>
        private Dictionary<int, Column> _ColumnIdDict;
        /// <summary>
        /// Index sloupců dle Name
        /// </summary>
        private Dictionary<string, Column> _ColumnNameDict;
        /// <summary>
        /// Počet sloupců v tabulce
        /// </summary>
        public int ColumnsCount { get { return this._Columns.Count; } }
        /// <summary>
        /// Přidá nový sloupec daného názvu, vrátí jeho objekt
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Column AddColumn(string name)
        {
            Column column = null;
            if (!String.IsNullOrEmpty(name))
            {
                column = new Column(name);
                this._Columns.Add(column);
            }
            return column;
        }
        /// <summary>
        /// Přidá nový sloupec daného názvu a popisku, vrátí jeho objekt
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public Column AddColumn(string name, string text)
        {
            Column column = null;
            if (!String.IsNullOrEmpty(name))
            {
                column = new Column(name, text);
                this._Columns.Add(column);
            }
            return column;
        }
        /// <summary>
        /// Přidá nový sloupec
        /// </summary>
        /// <param name="column"></param>
        public void AddColumn(Column column)
        {
            if (column != null)
                this._Columns.Add(column);
        }
        /// <summary>
        /// Přidá nové sloupce
        /// </summary>
        /// <param name="columnNames"></param>
        public void AddColumns(params string[] columnNames)
        {
            if (columnNames != null)
            {
                foreach (string columnName in columnNames)
                {
                    if (!String.IsNullOrEmpty(columnName))
                        this._Columns.Add(new Column(columnName));
                }
            }
        }
        /// <summary>
        /// Přidá nové sloupce
        /// </summary>
        /// <param name="columns"></param>
        public void AddColumns(params Column[] columns)
        {
            if (columns != null)
                this._Columns.AddRange(columns.Where(c => c != null));
        }
        /// <summary>
        /// Handler eventu event Columns.ItemAddAfter, vyvolá se po přidání objektu do kolekce Columns
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ColumnAddAfter(object sender, EList<Column>.EListAfterEventArgs args)
        {
            this.ColumnAdded(args);
            this.OnColumnAddAfter(args);
            if (this.ColumnAddAfter != null)
                this.ColumnAddAfter(this, args);
        }
        /// <summary>
        /// Akce po přidání sloupce do tabulky: napojí sloupec na tabulku, přiřadí ID, uloží do indexů
        /// </summary>
        /// <param name="args"></param>
        protected void ColumnAdded(EList<Column>.EListAfterEventArgs args)
        {
            Column column = args.Item;
            int id = this._ColumnsId++;
            ((ITableMember)column).AttachToTable(this, id);
            if (!this._ColumnIdDict.ContainsKey(id))
                this._ColumnIdDict.Add(id, column);
            string name = column.Name;
            if (!String.IsNullOrEmpty(name) && !this._ColumnNameDict.ContainsKey(name))
                this._ColumnNameDict.Add(name, column);
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu přidání sloupce, sloupec je platný, event ColumnAddAfter ještě neproběhl. V DTable je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnColumnAddAfter(EList<Column>.EListAfterEventArgs args) { }
        /// <summary>
        /// Public event vyvolaný po přidání nového sloupce do tabulky. Sloupec je již v tabulce umístěn, sloupec je uveden v argumentu.
        /// </summary>
        public event EList<Column>.EListEventAfterHandler ColumnAddAfter;
        /// <summary>
        /// Handler eventu event Columns.ItemRemoveAfter, vyvolá se po odebrání objektu (sloupce) z kolekce Columns
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ColumnRemoveAfter(object sender, EList<Column>.EListAfterEventArgs args)
        {
            this.ColumnRemoved(args);
            this.OnColumnRemoveAfter(args);
            if (this.ColumnRemoveAfter != null)
                this.ColumnRemoveAfter(this, args);
        }
        /// <summary>
        /// Akce po odebrání sloupce z tabulky: odpojí sloupec z tabulky, a odebere jej z do indexů
        /// </summary>
        /// <param name="args"></param>
        protected void ColumnRemoved(EList<Column>.EListAfterEventArgs args)
        {
            Column column = args.Item;
            int id = column.ColumnId;
            ((ITableMember)column).DetachFromTable();
            if (this._ColumnIdDict.ContainsKey(id))
                this._ColumnIdDict.Remove(id);
            string name = column.Name;
            if (!String.IsNullOrEmpty(name) && this._ColumnNameDict.ContainsKey(name))
                this._ColumnNameDict.Remove(name);
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu odebrání sloupce, sloupec je platný, event ColumnRemoveAfter ještě neproběhl. V DTable je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnColumnRemoveAfter(EList<Column>.EListAfterEventArgs args) { }
        /// <summary>
        /// Public event vyvolaný po odebrání sloupce z tabulky. Sloupec již v tabulce není umístěn, sloupec je uveden v argumentu.
        /// </summary>
        public event EList<Column>.EListEventAfterHandler ColumnRemoveAfter;
        /// <summary>
        /// Vrátí true pokud tabulka obsahuje sloupec daného názvu
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsColumn(string name)
        {
            return (!String.IsNullOrEmpty(name) && this._ColumnNameDict.ContainsKey(name));
        }
        /// <summary>
        /// Vrátí true pokud tabulka obsahuje sloupec daného ID
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        public bool ContainsColumn(int columnId)
        {
            return (this._ColumnIdDict.ContainsKey(columnId));
        }
        /// <summary>
        /// Vrátí sloupec daného názvu, může dojít k chybě při jeho neexistenci
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Column GetColumn(string name)
        {
            return (!String.IsNullOrEmpty(name) ? this._ColumnNameDict[name] : null);
        }
        /// <summary>
        /// Vrátí sloupec daného ID, může dojít k chybě při jeho neexistenci
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        public Column GetColumn(int columnId)
        {
            return this._ColumnIdDict[columnId];
        }
        /// <summary>
        /// Zkusí najít sloupec daného názvu, vrací true = nalezen
        /// </summary>
        /// <param name="name"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool TryGetColumn(string name, out Column column)
        {
            column = null;
            return (!String.IsNullOrEmpty(name) && this._ColumnNameDict.TryGetValue(name, out column));
        }
        /// <summary>
        /// Zkusí najít sloupec daného ID, vrací true = nalezen
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool TryGetColumn(int columnId, out Column column)
        {
            column = null;
            return (this._ColumnIdDict.TryGetValue(columnId, out column));
        }
        #endregion
        #region Rows - soupis řádků tabulky, přidávání, odebírání
        /// <summary>
        /// Inicializace dat pro řádky
        /// </summary>
        private void _RowsInit()
        {
            this._Rows = new EList<Row>();
            this._Rows.ItemAddAfter += this._RowAddAfter;
            this._Rows.ItemRemoveAfter += this._RowRemoveAfter;
            this._RowId = 0;
        }
        /// <summary>
        /// Kolekce řádků, nový řádek lze přidat i sem
        /// </summary>
        public EList<Row> Rows { get { return this._Rows; } }
        /// <summary>
        /// Seznam řádků
        /// </summary>
        private EList<Row> _Rows;
        /// <summary>
        /// ID pro nové řádky, výchozí = 0 = index pro první přidaný řádek. Vždy se jen navyšuje. Po odebrání řádku se jeho ID již nepoužije.
        /// </summary>
        private int _RowId;
        /// <summary>
        /// Počet řádků v tabulce
        /// </summary>
        public int RowsCount { get { return this._Rows.Count; } }
        /// <summary>
        /// Přidá nový řádek, vrátí jeho objekt
        /// </summary>
        /// <returns></returns>
        public Row AddRow()
        {
            Row row = new Row();
            this._Rows.Add(row);
            return row;
        }
        /// <summary>
        /// Přidá nový řádek, vrátí jeho objekt.
        /// Do řádku rovnou vepíše dodané hodnoty.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public Row AddRow(params object[] items)
        {
            Row row = null;
            if (items != null)
            {
                row = new Row(items);
                this._Rows.Add(row);
            }
            return row;
        }
        /// <summary>
        /// Přidá daný řádek.
        /// </summary>
        /// <param name="row"></param>
        public void AddRow(Row row)
        {
            if (row != null)
                this._Rows.Add(row);
        }
        /// <summary>
        /// Přidá dané řádky do této tabulky.
        /// </summary>
        /// <param name="rows"></param>
        public void AddRows(params Row[] rows)
        {
            if (rows != null)
                this._Rows.AddRange(rows.Where(r => r != null));
        }
        /// <summary>
        /// Handler eventu event Rows.ItemAddAfter, vyvolá se po přidání objektu do kolekce Rows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _RowAddAfter(object sender, EList<Row>.EListAfterEventArgs args)
        {
            this.RowAdded(args);
            this.OnRowAddAfter(args);
            if (this.RowAddAfter != null)
                this.RowAddAfter(this, args);
        }
        /// <summary>
        /// Akce po přidání řádku do tabulky: napojí řádek na tabulku, přiřadí ID
        /// </summary>
        /// <param name="args"></param>
        protected void RowAdded(EList<Row>.EListAfterEventArgs args)
        {
            Row row = args.Item;
            int id = this._RowId++;
            ((ITableMember)row).AttachToTable(this, id);
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu přidání řádku, řádek je platný, event RowAddAfter ještě neproběhl. V DTable je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRowAddAfter(EList<Row>.EListAfterEventArgs args) { }
        /// <summary>
        /// Public event vyvolaný po přidání nového řádku do tabulky. Řádek je již v tabulce umístěn, řádek je uveden v argumentu.
        /// </summary>
        public event EList<Row>.EListEventAfterHandler RowAddAfter;
        /// <summary>
        /// Handler eventu event Rows.ItemRemoveAfter, vyvolá se po odebrání objektu (řádku) z kolekce Rows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _RowRemoveAfter(object sender, EList<Row>.EListAfterEventArgs args)
        {
            this.RowRemoved(args);
            this.OnRowRemoveAfter(args);
            if (this.RowRemoveAfter != null)
                this.RowRemoveAfter(this, args);
        }
        /// <summary>
        /// Akce po odebrání řádku z tabulky: odpojí řádek z tabulky
        /// </summary>
        /// <param name="args"></param>
        protected void RowRemoved(EList<Row>.EListAfterEventArgs args)
        {
            ((ITableMember)args.Item).DetachFromTable();
        }
        /// <summary>
        /// Protected virtual metoda volaná v procesu odebrání řádku, řádek je platný, event RowRemoveAfter ještě neproběhl. V DTable je tato metoda prázdná.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRowRemoveAfter(EList<Row>.EListAfterEventArgs args) { }
        /// <summary>
        /// Public event vyvolaný po odebrání řádku z tabulky. Řádek již v tabulce není umístěn, řádek je uveden v argumentu.
        /// </summary>
        public event EList<Row>.EListEventAfterHandler RowRemoveAfter;
        #endregion
        #region Třídění a filtrování řádků
        /// <summary>
        /// Tato property vrací soupis všech aktuálně viditelných řádků, setříděný podle pvního ze sloupců, který má nastaven režim třídění jiný než None.
        /// Tato property vždy kompletně vyhodnotí data a vrátí nový objekt, nepoužívá se žádná úroveň cachování. Pozor na výkon, užívejme tuto property střídmě.
        /// </summary>
        public Row[] RowsSorted
        {
            get
            {
                // Základní viditelnost řádku daná kódem (Row.Visible) a řádkovým filtrem (RowFilterApply):
                bool hasRowFilter = this.HasRowFilter;
                List<Row> list = this.Rows.Where(r => (r.Visible && (hasRowFilter ? this.RowFilterApply(r) : true))).ToList();

                // Třídění podle sloupce:
                if (list.Count > 1)
                {
                    Column sortColumn = this.Columns.FirstOrDefault(c => (c.SortCurrent == TableSortRowType.Ascending || c.SortCurrent == TableSortRowType.Descending));
                    if (sortColumn != null)
                        // Bude to tříděné...
                        _RowsSort(list, sortColumn);
                }

                return list.ToArray();
            }
        }
        /// <summary>
        /// Metoda zajistí setřídění seznamu řádků podle dodaného sloupce.
        /// Metoda sama detekuje směr třídění, sloupec, jeho komparátor, přípravu komparační hodnoty (IComparableItem.Value) a konečně i provedení odpovídajícího třídění seznamu.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="sortColumn"></param>
        private void _RowsSort(List<Row> list, Column sortColumn)
        {
            // Pokud třídící sloupec obsahuje komparátor (funkce sortColumn.ValueComparator je zadaná), 
            //   pak metoda PrepareValue() dostává parametr "valueIsComparable" = false, metoda PrepareValue() tedy naplní Value, 
            //   a komparátor (sortColumn.ValueComparator) dostane k porovnání hodnotu object IComparableItem.Value :
            bool columnHasComparator = (sortColumn.ValueComparator != null);
            list.ForEach(r => (r as IComparableItem).PrepareValue(sortColumn.ColumnId, !columnHasComparator));

            switch (sortColumn.SortCurrent)
            {
                case TableSortRowType.Ascending:
                    if (columnHasComparator)
                        list.Sort((a, b) => sortColumn.ValueComparator((a as IComparableItem).Value, (b as IComparableItem).Value));
                    else
                        list.Sort((a, b) => SortRowsCompare((a as IComparableItem).ValueComparable, (b as IComparableItem).ValueComparable));
                    break;

                case TableSortRowType.Descending:
                    if (columnHasComparator)
                        list.Sort((a, b) => sortColumn.ValueComparator((b as IComparableItem).Value, (a as IComparableItem).Value));
                    else
                        list.Sort((a, b) => SortRowsCompare((b as IComparableItem).ValueComparable, (a as IComparableItem).ValueComparable));
                    break;

            }
        }
        /// <summary>
        /// Komparátor dvou hodnot typu IComparable, z nichž kterákoli smí být null
        /// </summary>
        /// <param name="valueA"></param>
        /// <param name="valueB"></param>
        /// <returns></returns>
        private static int SortRowsCompare(IComparable valueA, IComparable valueB)
        {
            if (valueA == null && valueB == null) return 0;
            if (valueA == null) return -1;
            if (valueB == null) return 1;
            return valueA.CompareTo(valueB);
        }
        /// <summary>
        /// true pokud tabulka má řádkový filtr
        /// </summary>
        protected bool HasRowFilter { get { return false; } }
        /// <summary>
        /// aplikuje řádkový filtr na daný řádek, vrátí true = řádek má být viditelný / false = skrýt, nevyhovuje filtru
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected bool RowFilterApply(Row row) { return true; }
        #endregion

        #region GUI vlastnosti
        /// <summary>
        /// Zadaná výška tabulky. S touto výškou bude tabulka zobrazena uživateli. Uživatel může / nemůže výšku tabulky měnit (podle situace v gridu).
        /// Pokud uživatel interaktivně změní výšku tabulky, projeví se to zde.
        /// Pokud kód změní výšku tabulky, bude tato výška tabulky zarovnána do patřičných mezí.
        /// Pokud kód nastaví výšku tabulky = null, pak se pro zobrazení převezme defaultní výška dle Gridu.
        /// Výška tabulky bude vždy v rozmezí HeightRange.
        /// </summary>
        public Int32 Height
        {
            get
            { return this.TableHeightLayout.CurrentSize; }
            set
            {
                int oldValue = this.TableHeightLayout.CurrentSize;
                this.TableHeightLayout.Size = value;
                if ((this.TableHeightLayout.CurrentSize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableHeight);
            }
        }
        /// <summary>
        /// true pro viditelnou tabulku (default), false pro skrytou
        /// </summary>
        public bool IsVisible
        {
            get
            { return this.TableHeightLayout.Visible; }
            set
            {
                bool oldValue = this.TableHeightLayout.Visible;
                this.TableHeightLayout.Visible = value;
                if ((this.TableHeightLayout.Visible != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableHeight);
            }
        }
        /// <summary>
        /// Výška oblasti ColumnHeader. 
        /// Při nasetování hodnoty dojde k její kontrole a případně úpravě tak, aby uložená hodnota odpovídala pravidlům.
        /// To znamená, že po vložení hodnoty X může být okamžitě čtena hodnota ColumnHeaderHeight jiná, než byla vložena.
        /// </summary>
        public Int32 ColumnHeaderHeight
        {
            get
            { return this.ColumnHeaderHeightLayout.CurrentSize; }
            set
            {
                int oldValue = this.ColumnHeaderHeightLayout.CurrentSize;
                this.ColumnHeaderHeightLayout.Size = value;
                if ((this.ColumnHeaderHeightLayout.CurrentSize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.ColumnHeader);
            }
        }
        /// <summary>
        /// Šířka sloupce obsahujícího RowHeader
        /// </summary>
        public Int32 RowHeaderWidth
        {
            get { return this.RowHeaderWidthLayout.CurrentSize; }
            set
            {
                int oldValue = this.RowHeaderWidthLayout.CurrentSize;
                this.RowHeaderWidthLayout.Size = value;
                if ((this.RowHeaderWidthLayout.CurrentSize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.ColumnHeader);
            }
        }
        /// <summary>
        /// Pořadí této tabulky v Gridu při zobrazování.
        /// Výchozí je -1, pak bude tabulka zařazena na konec soupisu tabulek v jednom gridu.
        /// Datová vrstva může vložit jinou hodnotu (nula a kladnou), a tím explicitně určit pozici tabulky v gridu.
        /// Jednotlivé tabulky nemusí mít hodnoty TableOrder v nepřerušovaném pořadí.
        /// Po napojení tabulky do gridu je do TableOrder vepsána pořadová hodnota, pokud aktuální hodnota je záporná (což je default).
        /// Po odpojení tabuky z Gridu je vepsána hodnota -1.
        /// </summary>
        public int TableOrder
        {
            get
            { return this._TableOrder; }
            set
            {
                int oldValue = this._TableOrder;
                this._TableOrder = value;
                if ((this._TableOrder != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.ColumnHeader);
            }
        } private int _TableOrder = -1;
        /// <summary>
        /// true pokud je povoleno interaktivně změnit výšku tabulky (myší). Default = true;
        /// </summary>
        public bool AllowTableResize
        {
            get { return this._AllowTableResize; }
            set
            {
                bool oldValue = this._AllowTableResize;
                this._AllowTableResize = value;
                if ((this._AllowTableResize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.GridItems);
            }
        } private bool _AllowTableResize = true;
        /// <summary>
        /// true pokud je povoleno interaktivně změnit šířku sloupce, který obsahuje záhlaví řádku (myší). Default = true;
        /// </summary>
        public bool AllowRowHeaderWidthResize
        {
            get { return this._AllowRowHeaderWidthResize; }
            set
            {
                bool oldValue = this._AllowRowHeaderWidthResize;
                this._AllowRowHeaderWidthResize = value;
                if ((this._AllowRowHeaderWidthResize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        } private bool _AllowRowHeaderWidthResize = true;
        /// <summary>
        /// true pokud je povoleno interaktivně přemisťovat sloupce (přetahovat je myší). Default = true;
        /// </summary>
        public bool AllowColumnReorder
        {
            get { return this._AllowColumnReorder; }
            set
            {
                bool oldValue = this._AllowColumnReorder;
                this._AllowColumnReorder = value;
                if ((this._AllowColumnReorder != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        } private bool _AllowColumnReorder = true;
        /// <summary>
        /// true pokud je povoleno interaktivně změnit šířku sloupce (myší). Default = true;
        /// </summary>
        public bool AllowColumnResize
        {
            get { return this._AllowColumnResize; }
            set
            {
                bool oldValue = this._AllowColumnResize;
                this._AllowColumnResize = value;
                if ((this._AllowColumnResize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        } private bool _AllowColumnResize = true;
        /// <summary>
        /// true pokud je povoleno interaktivně přemisťovat řádky (přetahovat je myší). Default = false;
        /// </summary>
        public bool AllowRowReorder
        {
            get { return this._AllowRowReorder; }
            set
            {
                bool oldValue = this._AllowRowReorder;
                this._AllowRowReorder = value;
                if ((this._AllowRowReorder != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        } private bool _AllowRowReorder = false;
        /// <summary>
        /// true pokud je povoleno interaktivně změnit výšku řádku (myší). Default = true;
        /// </summary>
        public bool AllowRowResize
        {
            get { return this._AllowRowResize; }
            set
            {
                bool oldValue = this._AllowRowResize;
                this._AllowRowResize = value;
                if ((this._AllowRowResize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        } private bool _AllowRowResize = true;
        /// <summary>
        /// true pokud je povoleno třídit řádky kliknutím na záhlaví sloupce. Default = true;
        /// </summary>
        public bool AllowColumnSortByClick { get { return this._AllowColumnSortByClick; } set { this._AllowColumnSortByClick = value; } } private bool _AllowColumnSortByClick = true;
        /// <summary>
        /// true pokud je povoleno označovat řádky (Selected) kliknutím na záhlaví řádku. Default = true;
        /// </summary>
        public bool AllowRowSelectByClick { get { return this._AllowRowSelectByClick; } set { this._AllowRowSelectByClick = value; } } private bool _AllowRowSelectByClick = true;
        /// <summary>
        /// Image použitý pro zobrazení Selected řádku v prostoru RowHeader. Default = IconStandard.RowSelected;
        /// </summary>
        public Image SelectedRowImage { get { return this._SelectedRowImage; } set { this._SelectedRowImage = value; } } private Image _SelectedRowImage = IconStandard.RowSelected;
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví tabulky = buňka v křížení ColumnHeader * RowHeader.
        /// </summary>
        public void TableHeaderClick()
        { }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví sloupce.
        /// Účelem této metody není změna třídění, to zavolá Grid hned poté, pokud je to povoleno.
        /// </summary>
        /// <param name="column"></param>
        public void ColumnHeaderClick(Column column)
        { }
        /// <summary>
        /// Provede se poté, kdy uživatel klikne na záhlaví řádku.
        /// Účelem této metody není změna hodnoty Selected na řádku, to zavolá Grid hned poté, pokud je to povoleno.
        /// </summary>
        /// <param name="row">řádek</param>
        public void RowHeaderClick(Row row)
        { }
        /// <summary>
        /// Záhlaví této tabulky, grafický prvek, auitoinicializační
        /// </summary>
        public Components.Grid.GTableHeader TableHeader
        {
            get
            {
                if (this._TableHeader == null)
                    this._TableHeader = new Components.Grid.GTableHeader(this);
                return this._TableHeader;
            }
            set { this._TableHeader = value; }
        }
        private Components.Grid.GTableHeader _TableHeader;
        #endregion
        #region Layouty (výšky, šířky, rozmezí) : kořenové hodnoty uložené na tabulce
        /// <summary>
        /// Výška celé tabulky: default, rozmezí, aktuální hodnota
        /// </summary>
        public SequenceLayout TableHeightLayout { get { return this._TableHeightLayout; } } private SequenceLayout _TableHeightLayout;
        /// <summary>
        /// Výška prostoru ColumnHeader: default, rozmezí, aktuální hodnota
        /// </summary>
        public SequenceLayout ColumnHeaderHeightLayout { get { return this._ColumnHeaderHeightLayout; } } private SequenceLayout _ColumnHeaderHeightLayout;
        /// <summary>
        /// Šířka sloupce obsahujícího RowHeader: default, rozmezí, aktuální hodnota
        /// </summary>
        public SequenceLayout RowHeaderWidthLayout { get { return this._RowHeaderWidthLayout; } } private SequenceLayout _RowHeaderWidthLayout;
        /// <summary>
        /// Výchozí hodnota šířky sloupce a povolené rozmezí šířky: default, rozmezí, aktuální hodnota
        /// </summary>
        public SequenceLayout ColumnWidthLayout { get { return this._ColumnWidthLayout; } } private SequenceLayout _ColumnWidthLayout;
        /// <summary>
        /// Výchozí hodnota výšky řádku a povolené rozmezí výšky: default, rozmezí, aktuální hodnota
        /// </summary>
        public SequenceLayout RowHeightLayout { get { return this._RowHeightLayout; } } private SequenceLayout _RowHeightLayout;
        private void _LayoutInit()
        {
            this._TableHeightLayout = new SequenceLayout(60, 250);
            this._ColumnHeaderHeightLayout = new SequenceLayout(20, 45);
            this._RowHeaderWidthLayout = new SequenceLayout(20, 35);
            this._ColumnWidthLayout = new SequenceLayout(20, 160);
            this._RowHeightLayout = new SequenceLayout(8, 24);
        }
        private ISequenceLayout _SequenceLayout { get { return (ISequenceLayout)this._TableHeightLayout; } }
        int ISequenceLayout.Begin { get { return this._SequenceLayout.Begin; } set { this._SequenceLayout.Begin = value; } }
        int ISequenceLayout.Size { get { return this._SequenceLayout.Size; } set { this._SequenceLayout.Size = value; } }
        int ISequenceLayout.End { get { return this._SequenceLayout.End; } }
        /// <summary>
        /// Komparátor podle hodnoty TableOrder ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrder(Table a, Table b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.TableOrder.CompareTo(b.TableOrder);
        }
        #endregion
        #region Visual style
        /// <summary>
        /// Všechny vizuální vlastnosti dat v tomto sloupci (nikoli hlavičky).
        /// Default hodnota je null.
        /// </summary>
        public VisualStyle VisualStyle { get { return _VisualStyle; } set { _VisualStyle = value; } } private VisualStyle _VisualStyle = null;
        VisualStyle IVisualMember.Style
        {
            get
            {
                return VisualStyle.CreateFrom(this.VisualStyle);
            }
        }
        Int32? IVisualMember.Width { get { return null; } }
        Int32? IVisualMember.Height { get { return null; } }
        #endregion
        #region ITableValidity
        bool IContentValidity.DataIsValid { get { return _RowDataIsValid; } set { _RowDataIsValid = value; } } private bool _RowDataIsValid;
        bool IContentValidity.RowLayoutIsValid { get { return _RowLayoutIsValid; } set { _RowLayoutIsValid = value; } } private bool _RowLayoutIsValid;
        bool IContentValidity.ColumnLayoutIsValid { get { return _ColumnLayoutIsValid; } set { _ColumnLayoutIsValid = value; } } private bool _ColumnLayoutIsValid;
        #endregion
      
    }
    #endregion
    #region Column
    /// <summary>
    /// Column : informace o jednom sloupci tabulky
    /// </summary>
    public class Column : ITableMember, IVisualMember, ISequenceLayout
    {
        #region Konstruktor, základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Column()
        {
            this._ColumnId = -1;
            this._SizeLayout = new SequenceLayout(8, 160);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        public Column(string name)
            : this()
        {
            this._Name = name;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        public Column(string name, Djs.Common.Localizable.TextLoc text)
            : this()
        {
            this._Name = name;
            this._Text = text;
        }
        public override string ToString()
        {
            return this.Name + ": " + this.Title;
        }
        /// <summary>
        /// Jednoznačné ID tohoto sloupce. Read only.
        /// Je přiděleno při přidání do tabulky, pak má hodnotu 0 nebo kladnou.
        /// Hodnota se nemění ani přemístěním na jinou pozici, ani odebráním některého sloupce s menším ID.
        /// Po odebrání z tabulky je hodnota -1.
        /// </summary>
        public int ColumnId { get { return this._ColumnId; } } private int _ColumnId = -1;
        /// <summary>
        /// Klíčový název, typicky sloupec DB tabulky nebo jiné klíčové slovo. Nejde o titulek.
        /// </summary>
        public string Name { get { return this._Name; } set { this._Name = value; } } private string _Name;
        #endregion
        #region Linkování na tabulku
        /// <summary>
        /// Reference na tabulku, kam sloupec patří.
        /// </summary>
        public Table Table { get { return this._Table; } private set { this._Table = value; } } private Table _Table;
        /// <summary>
        /// true pokud máme referenci na tabulku
        /// </summary>
        public bool HasTable { get { return (this.Table != null); } }
        /// <summary>
        /// Napojí this sloupec do dané tabulky.
        /// Je voláno z tabulky, v eventu ItemAdd kolekce Columns.
        /// </summary>
        /// <param name="table"></param>
        void ITableMember.AttachToTable(Table table, int id)
        {
            this._ColumnId = id;
            if (this._ColumnOrder < 0)
                this._ColumnOrder = id;
            this.Table = table;
            this.WidthLayout.ParentLayout = table.ColumnWidthLayout;
        }
        /// <summary>
        /// Odpojí this sloupec z dané tabulky.
        /// Je voláno z tabulky, v eventu ItemRemove kolekce Columns.
        /// </summary>
        void ITableMember.DetachFromTable()
        {
            this._ColumnId = -1;
            this.Table = null;
            this.WidthLayout.ParentLayout = null;
        }
        #endregion
        #region GUI vlastnosti sloupce
        /// <summary>
        /// Titulkový text, lokalizovaný
        /// </summary>
        public Djs.Common.Localizable.TextLoc Title { get { return (this._Text != null ? this._Text : (Djs.Common.Localizable.TextLoc)this._Name); } set { this._Text = value; } } private Djs.Common.Localizable.TextLoc _Text;
        /// <summary>
        /// ToolTip pro hlavičku sloupce, lokalizovaný
        /// </summary>
        public Djs.Common.Localizable.TextLoc ToolTip { get { return this._ToolTip; } set { this._ToolTip = value; } } private Djs.Common.Localizable.TextLoc _ToolTip;
        /// <summary>
        /// Pořadí tohoto sloupce při zobrazování.
        /// Výchozí je -1, takový sloupec je při přidání do tabulky zařazen na její konec.
        /// Pokud je vytvořen sloupec s ColumnOrder nula a kladným, pak při přidání do tabulky se jeho ColumnOrder nezmění.
        /// Jednotlivé sloupce nemusí mít hodnoty ColumnOrder v nepřerušovaném pořadí.
        /// Po napojení sloupce do tabulky je do ColumnOrder vepsána hodnota = ColumnID, takže nový sloupec se zařadí vždy na konec.
        /// </summary>
        public int ColumnOrder { get { return this._ColumnOrder; } set { this._ColumnOrder = value; } } private int _ColumnOrder = -1;
        /// <summary>
        /// Datový typ obsahu sloupce. Null = obecná data
        /// </summary>
        public Type DataType { get { return this._DataType; } set { this._DataType = value; } } private Type _DataType;
        /// <summary>
        /// Formátovací string pro data zobrazovaná v tomto sloupci.
        /// </summary>
        public string FormatString { get { return this._FormatString; } set { this._FormatString = value; } } private string _FormatString;
        /// <summary>
        /// true pokud je povoleno třídit řádky kliknutím na záhlaví tohoto sloupce. Default = true;
        /// </summary>
        public bool AllowColumnSortByClick { get { return this._AllowColumnSortByClick; } set { this._AllowColumnSortByClick = value; } } private bool _AllowColumnSortByClick = true;
        /// <summary>
        /// true pokud je měnit šířku tohoto sloupce pomocí myši. Default = true;
        /// </summary>
        public bool AllowColumnResize { get { return this._AllowColumnResize; } set { this._AllowColumnResize = value; } } private bool _AllowColumnResize = true;
        /// <summary>
        /// true pokud se pro sloupec má zobrazit časová osa v záhlaví
        /// </summary>
        public bool UseTimeAxis { get { return this._UseTimeAxis; } set { this._UseTimeAxis = value; } } private bool _UseTimeAxis;
        /// <summary>
        /// Komparátor ColumnOrder ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrder(Column a, Column b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return a.ColumnOrder.CompareTo(b.ColumnOrder);
        }
        /// <summary>
        /// Záhlaví tohoto sloupce, grafický prvek, auitoinicializační
        /// </summary>
        public Components.Grid.GColumnHeader ColumnHeader
        {
            get
            {
                if (this._ColumnHeader == null)
                    this._ColumnHeader = new Components.Grid.GColumnHeader(this);
                return this._ColumnHeader;
            }
            set { this._ColumnHeader = value; }
        }
        private Components.Grid.GColumnHeader _ColumnHeader;
        #endregion
        #region Třídění podle sloupce
        /// <summary>
        /// Režim třídění v tomto sloupci
        /// </summary>
        public TableSortRowType SortCurrent { get { return this._SortCurrent; } set { this._SortCurrent = value; } } private TableSortRowType _SortCurrent = TableSortRowType.None;
        /// <summary>
        /// Změní třídění tohoto sloupce, volá se po kliknutí na jeho záhlaví.
        /// Pokud je třídění povoleno, změní SortCurrent v pořadí None - Asc - Desc - None; a vrátí true.
        /// Pokud třídění není povoleno, vrátí false.
        /// </summary>
        /// <returns></returns>
        public bool SortChange()
        {
            if (!this.SortingEnabled) return false;
            switch (this.SortCurrent)
            {
                case TableSortRowType.None:
                    this.SortCurrent = TableSortRowType.Ascending;
                    break;
                case TableSortRowType.Ascending:
                    this.SortCurrent = TableSortRowType.Descending;
                    break;
                case TableSortRowType.Descending:
                    this.SortCurrent = TableSortRowType.None;
                    break;
            }
            return true;
        }
        /// <summary>
        /// true pokud je povoleno třídit podle tohoto sloupce.
        /// false = nemožno.
        /// </summary>
        public bool SortingEnabled { get { return this._SortingEnabled; } set { this._SortingEnabled = value; } } private bool _SortingEnabled = true;
        /// <summary>
        /// Komparátor pro dvě hodnoty v tomto sloupci, pro třídění
        /// </summary>
        public Func<object, object, int> ValueComparator;
        #endregion
        #region Šířka sloupce, kompletní layout okolo šířky sloupce
        /// <summary>
        /// Zadaná šířka sloupce.
        /// Hodnotu může vložit aplikační kód, hodnota se projeví v GUI.
        /// Uživatel může interaktivně měnit velikost objektu, změna se projeví v této hodnotě.
        /// Veškerá další nastavení jsou v property WidthLayout.
        /// </summary>
        public Int32? Width { get { return this.WidthLayout.Size; } set { this.WidthLayout.Size = value; } }
        /// <summary>
        /// true pro viditelný sloupec (default), false for skrytý
        /// </summary>
        public bool IsVisible { get { return this.WidthLayout.Visible; } set { this.WidthLayout.Visible = value; } }
        /// <summary>
        /// Veškeré hodnoty související s výškou řádku (rozsah hodnot, povolení Resize)
        /// </summary>
        public SequenceLayout WidthLayout { get { return this._SizeLayout; } } private SequenceLayout _SizeLayout;
        private ISequenceLayout _SequenceLayout { get { return (ISequenceLayout)this._SizeLayout; } }
        int ISequenceLayout.Begin { get { return this._SequenceLayout.Begin; } set { this._SequenceLayout.Begin = value; } }
        int ISequenceLayout.Size { get { return this._SequenceLayout.Size; } set { this._SequenceLayout.Size = value; } }
        int ISequenceLayout.End { get { return this._SequenceLayout.End; } }
        #endregion
        #region Visual style
        /// <summary>
        /// Všechny vizuální vlastnosti dat v tomto sloupci (nikoli hlavičky).
        /// Default hodnota je null.
        /// </summary>
        public VisualStyle VisualStyle { get { return _VisualStyle; } set { _VisualStyle = value; } }
        private VisualStyle _VisualStyle;
        VisualStyle IVisualMember.Style
        {
            get
            {
                return VisualStyle.CreateFrom(this.VisualStyle, (this.Table != null ? this.Table.VisualStyle : null));
            }
        }
        Int32? IVisualMember.Width { get { return this.Width; } }
        Int32? IVisualMember.Height { get { return (this.HasTable ? this.Table.ColumnHeaderHeightLayout.Size : null); } }
        #endregion
    }
    #endregion
    #region Row
    /// <summary>
    /// Row : informace o jednom řádku tabulky
    /// </summary>
    public class Row : ITableMember, IVisualMember, ISequenceLayout, IContentValidity, IComparableItem
    {
        #region Konstruktor, základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Row()
        {
            this._RowId = -1;
            this._CellDict = new Dictionary<int, Cell>();
            this._HeightLayout = new SequenceLayout(6, 24);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="values"></param>
        public Row(params object[] values) : this()
        {
            this._CellInit(values);
        }
        /// <summary>
        /// Jednoznačné ID tohoto řádku. Read only.
        /// Je přiděleno při přidání do tabulky, pak má hodnotu 0 nebo kladnou.
        /// Hodnota se nemění ani přemístěním na jinou pozici, ani odebráním některého řádku s menším ID.
        /// Po odebrání z tabulky je hodnota -1.
        /// </summary>
        public int RowId { get { return this._RowId; } } private int _RowId = -1;
        #endregion
        #region Linkování na tabulku
        /// <summary>
        /// Reference na tabulku, kam řádek patří.
        /// </summary>
        public Table Table { get { return this._Table; } private set { this._Table = value; } } private Table _Table;
        /// <summary>
        /// Kolekce sloupců z tabulky, může být null pokud řádek není napojen do tabulky
        /// </summary>
        public EList<Column> Columns { get { return (this.HasTable ? this.Table.Columns : null); } }
        /// <summary>
        /// true pokud máme referenci na tabulku
        /// </summary>
        public bool HasTable { get { return (this.Table != null); } }
        /// <summary>
        ///Napojí this řádek do dané tabulky.
        /// Je voláno z tabulky, v eventu ItemAdd kolekce Rows.
        /// </summary>
        /// <param name="table"></param>
        void ITableMember.AttachToTable(Table table, int id)
        {
            this._RowId = id;
            this.Table = table;
            this.HeightLayout.ParentLayout = (table != null ? table.RowHeightLayout : null);
        }
        /// <summary>
        /// Odpojí this řádek z dané tabulky.
        /// Je voláno z tabulky, v eventu ItemRemove kolekce Rows.
        /// </summary>
        void ITableMember.DetachFromTable()
        {
            this._RowId = -1;
            this.Table = null;
            this.HeightLayout.ParentLayout = null;
        }
        #endregion
        #region Cells = jednotlivé buňky v řádku
        /// <summary>
        /// Obsahuje (vrátí) instanci DCell, pro dané ID sloupce. 
        /// Nikdy nevrací null. 
        /// Lze tedy založit a udržovat buňky i pro neexistující sloupce.
        /// </summary>
        /// <param name="columnId">ID sloupce</param>
        /// <returns></returns>
        public Cell this[int columnId]
        {
            get { return this._GetCell(columnId); }
        }
        /// <summary>
        /// Obsahuje všechny platné buňky řádku (tj. ty, které odpovídají sloupcům tabulky). 
        /// </summary>
        public Cell[] Cells
        {
            get
            {
                if (!this.HasTable) return this._CellDict.Values.ToArray();              // Row with no table linked
                List<Cell> list = new List<Cell>();
                foreach (Column column in this.Columns)                                 // Cells for existing Columns
                    list.Add(this._GetCell(column.ColumnId));
                return list.ToArray();
            }
        }
        /// <summary>
        /// Uloží dané hodnoty do všech buněk, v pořadí 0 - Length
        /// </summary>
        /// <param name="values"></param>
        private void _CellInit(object[] values)
        {
            if (this._CellDict.Count > 0)
                this._CellDict.Clear();
            for (int columnId = 0; columnId < values.Length; columnId++)
                this._GetCell(columnId).Value = values[columnId];
        }
        /// <summary>
        /// Vrátí buňku pro dané ColumnId.
        /// Vždy vrátí buňku pro dané ID, i kdyby takový sloupec neexistoval v tabulce a dané ID bylo mimo rozsah.
        /// </summary>
        /// <param name="columnId"></param>
        /// <returns></returns>
        private Cell _GetCell(int columnId)
        {
            Cell cell;
            if (!this._CellDict.TryGetValue(columnId, out cell))
            {
                cell = new Cell(this, columnId);
                this._CellDict.Add(columnId, cell);
            }
            return cell;
        }
        /// <summary>
        /// Soupis buněk
        /// </summary>
        private Dictionary<int, Cell> _CellDict;
        #endregion
        #region GUI vlastnosti
        /// <summary>
        /// true pokud this řádek je vybrán k další práci. Default = false
        /// </summary>
        public bool IsSelected { get { return this._IsSelected; } set { this._IsSelected = value; } } private bool _IsSelected = false;
        /// <summary>
        /// Změní hodnotu IsSelected v tomto řádku
        /// </summary>
        public void SelectedChange()
        {
            this.IsSelected = !this.IsSelected;
        }
        /// <summary>
        /// Image použitý pro zobrazení Selected řádku v prostoru RowHeader. Default = null, v tom případě se použije image this.Table.SelectedRowImage.
        /// </summary>
        public Image SelectedRowImage { get { return this._SelectedRowImage ?? this.Table.SelectedRowImage; } set { this._SelectedRowImage = value; } } private Image _SelectedRowImage = null;
        /// <summary>
        /// Záhlaví tohoto sloupce, grafický prvek, auitoinicializační
        /// </summary>
        public Components.Grid.GRowHeader RowHeader
        {
            get
            {
                if (this._RowHeader == null)
                    this._RowHeader = new Components.Grid.GRowHeader(this);
                return this._RowHeader;
            }
            set { this._RowHeader = value; }
        }
        private Components.Grid.GRowHeader _RowHeader;
        #endregion
        #region Visual style
        /// <summary>
        /// Všechny vizuální vlastnosti dat v tomto řádku (nikoli buňky ani tabulky).
        /// Default hodnota je null.
        /// </summary>
        public VisualStyle VisualStyle { get { return _VisualStyle; } set { _VisualStyle = value; } } private VisualStyle _VisualStyle;
        VisualStyle IVisualMember.Style
        {
            get
            {
                return VisualStyle.CreateFrom(this.VisualStyle, (this.Table != null ? this.Table.VisualStyle : null));
            }
        }
        Int32? IVisualMember.Width { get { return null; } }
        Int32? IVisualMember.Height { get { return this.Height; } }
        #endregion
        #region Výška řádku, kompletní layout okolo výšky řádku
        /// <summary>
        /// Zadaná výška řádku.
        /// Hodnotu může vložit aplikační kód, hodnota se projeví v GUI.
        /// Uživatel může interaktivně měnit velikost objektu, změna se projeví v této hodnotě.
        /// Veškerá další nastavení jsou v property HeightLayout.
        /// </summary>
        public Int32? Height { get { return this.HeightLayout.Size; } set { this.HeightLayout.Size = value; } }
        /// <summary>
        /// true pro viditelný sloupec (default), false for skrytý
        /// </summary>
        public bool Visible { get { return this.HeightLayout.Visible; } set { this.HeightLayout.Visible = value; } }
        /// <summary>
        /// Veškeré hodnoty související s výškou řádku (rozsah hodnot, povolení Resize)
        /// </summary>
        public SequenceLayout HeightLayout { get { return this._HeightLayout; } } private SequenceLayout _HeightLayout;
        private ISequenceLayout _SequenceLayout { get { return (ISequenceLayout)this._HeightLayout; } }
        int ISequenceLayout.Begin { get { return this._SequenceLayout.Begin; } set { this._SequenceLayout.Begin = value; } }
        int ISequenceLayout.Size { get { return this._SequenceLayout.Size; } set { this._SequenceLayout.Size = value; } }
        int ISequenceLayout.End { get { return this._SequenceLayout.End; } }
        #endregion
        #region IContentValidity
        bool IContentValidity.DataIsValid { get { return _RowDataIsValid; } set { _RowDataIsValid = value; } } private bool _RowDataIsValid;
        bool IContentValidity.RowLayoutIsValid { get { return _RowLayoutIsValid; } set { _RowLayoutIsValid = value; } } private bool _RowLayoutIsValid;
        bool IContentValidity.ColumnLayoutIsValid { get { return _ColumnLayoutIsValid; } set { _ColumnLayoutIsValid = value; } } private bool _ColumnLayoutIsValid;
        #endregion
        #region IComparableItem
        void IComparableItem.PrepareValue(int valueId, bool valueIsComparable)
        {
            var cell = this._GetCell(valueId);
            if (valueIsComparable)
                this._IComparableItemValue = cell.Value;
            else
                this._IComparableItemValueComparable = cell.Value as IComparable;
        }
        object IComparableItem.Value { get { return _IComparableItemValue; } } private object _IComparableItemValue;
        IComparable IComparableItem.ValueComparable { get { return _IComparableItemValueComparable; } } private IComparable _IComparableItemValueComparable;
        #endregion
    }
    #endregion
    #region Cell
    /// <summary>
    /// Cell : informace v jedné buňce tabulky (jeden sloupec v jednom řádku).
    /// Obsahuje data a formátovací informace.
    /// </summary>
    public class Cell : IVisualMember
    {
        #region Constructor, parents of this Cell
        internal Cell(Row dRow, int columnId)
        {
            this._ColumnId = columnId;
            this._Row = dRow;
        }
        /// <summary>
        /// Řádek, do něhož tato buňka patří
        /// </summary>
        public Row Row { get { return _Row; } private set { _Row = value; } } private Row _Row;
        /// <summary>
        /// Tabulka, do které tato buňka patří
        /// Může být null, pokud buňka dosud není v řádku nebo řádek není v tabulce.
        /// </summary>
        public Table Table { get { return (this._Row != null ? this._Row.Table : null); } }
        /// <summary>
        /// Sloupc, do něhož tato buňka patří.
        /// Může být null, pokud buňka dosud není v řádku nebo řádek není v tabulce.
        /// </summary>
        public Column Column { get { Column column = null; if (this._Row != null && this._Row.HasTable && this._Row.Table.TryGetColumn(this._ColumnId, out column)) return column; return null; } }
        /// <summary>
        /// ColumnID sloupce, do kterého tato buňka patří.
        /// Tato hodnota je platná bez ohledu na to, zda buňka (resp. její řádek) již je nebo není obsažena v tabulce.
        /// </summary>
        public int ColumnId { get { return _ColumnId; } } private int _ColumnId;
        #endregion
        #region Value
        /// <summary>
        /// Hodnota v této buňce. Setování hodnoty provede invalidaci dat řádku a tabulky.
        /// </summary>
        public object Value { get { return _Value; } set { _Value = value; this.InvalidateRowData(); } } private object _Value;
        /// <summary>
        /// Explicitně daná výška v tomto řádku. Je daná obsahem.
        /// Výška se uplatní při určení výšky řádku.
        /// </summary>
        public Int32? Height { get { return _Height; } set { _Height = value; this.InvalidateRowLayout(); } } private Int32? _Height;
        protected void InvalidateRowLayout()
        {
            // 1. Tímhle donutím můj Parent řádek, aby si přepočítal (až to bude potřeba) svoji výšku:
            IContentValidity rowValidity = this.Row as IContentValidity;
            if (rowValidity != null && rowValidity.RowLayoutIsValid)
                rowValidity.RowLayoutIsValid = false;

            // 2. Tímhle donutím tabulku, aby si přepočítala vizuální pozice řádků:
            IContentValidity tableValidity = this.Table as IContentValidity;
            if (tableValidity != null && tableValidity.RowLayoutIsValid)
                tableValidity.RowLayoutIsValid = false;
        }
        protected void InvalidateRowData()
        {
            // 1. Tímhle sdělím mému Parent řádku, že obsahuje nevalidní data:
            IContentValidity rowValidity = this.Row as IContentValidity;
            if (rowValidity != null && rowValidity.DataIsValid)
                rowValidity.DataIsValid = false;

            // 1. Tímhle sdělím tabulce, že obsahuje nevalidní data:
            IContentValidity tableValidity = this.Table as IContentValidity;
            if (tableValidity != null && tableValidity.DataIsValid)
                tableValidity.DataIsValid = false;
        }
        #endregion
        #region Visual style
        /// <summary>
        /// Všechny vizuální vlastnosti dat v této buňce.
        /// Default hodnota je null.
        /// </summary>
        public VisualStyle VisualStyle { get { return _VisualStyle; } set { _VisualStyle = value; } }
        private VisualStyle _VisualStyle;
        VisualStyle IVisualMember.Style
        {
            get
            {
                Column column = this.Column;
                return VisualStyle.CreateFrom(this.VisualStyle, this.Row.VisualStyle, (column != null ? column.VisualStyle : null), this.Table.VisualStyle);
            }
        }
        Int32? IVisualMember.Width { get { return null; } }
        Int32? IVisualMember.Height { get { return null; } }
        #endregion
    }
    #endregion
    #region Visual style for one item
    public class VisualStyle
    {
        /// <summary>
        /// Vytvoří a vrátí new instanci VisualStyle, v níž budou jednotlivé property naplněny hodnotami z dodaných instancí.
        /// Slouží k vyhodnocení řetězce od explicitních údajů (zadaných do konkrétního prvku) až po defaultní (zadané např. v konfiguraci).
        /// Dodané instance se vyhodnocují v pořadá od první do poslední, hodnoty null se přeskočí.
        /// Logika: hodnota do každé jednotlivé property výsledné instance se převezme z nejbližšího dodaného objektu, kde tato hodnota není null.
        /// </summary>
        /// <param name="styles"></param>
        /// <returns></returns>
        public static VisualStyle CreateFrom(params VisualStyle[] styles)
        {
            VisualStyle result = new VisualStyle();
            foreach (VisualStyle style in styles)
                result._AddFrom(style);
            return result;
        }
        /// <summary>
        /// Do this instance vloží potřebné hodnoty z dodané instance.
        /// Dodaná instance může být null, pak se nic neprovádí.
        /// Plní se jen takové property v this, které obsahují null.
        /// </summary>
        /// <param name="style"></param>
        private void _AddFrom(VisualStyle style)
        {
            if (style != null)
            {
                if (this.Font == null) this.Font = style.Font;
                if (!this.ContentAlignment.HasValue) this.ContentAlignment = style.ContentAlignment;
                if (!this.BackColor.HasValue) this.BackColor = style.BackColor;
                if (!this.TextColor.HasValue) this.TextColor = style.TextColor;
                if (!this.SelectedBackColor.HasValue) this.SelectedBackColor = style.SelectedBackColor;
                if (!this.SelectedTextColor.HasValue) this.SelectedTextColor = style.SelectedTextColor;
                if (!this.FocusBackColor.HasValue) this.FocusBackColor = style.FocusBackColor;
                if (!this.FocusTextColor.HasValue) this.FocusTextColor = style.FocusTextColor;
            }
        }
        public FontInfo Font { get; set; }
        public ContentAlignment? ContentAlignment { get; set; }
        public Color? BackColor { get; set; }
        public Color? TextColor { get; set; }
        public Color? SelectedBackColor { get; set; }
        public Color? SelectedTextColor { get; set; }
        public Color? FocusBackColor { get; set; }
        public Color? FocusTextColor { get; set; }

    }
    #endregion
    #region Interfaces
    /// <summary>
    /// Objekt, kterému je možno nastavit stav platnosti dat, sloupce a řádku
    /// </summary>
    public interface IContentValidity
    {
        /// <summary>
        /// true pokud jsou platná data, false po jejich změně před akceptováním dat
        /// </summary>
        bool DataIsValid { get; set; }
        /// <summary>
        /// true pokud je platný layout řádků, false po jejich změně před akceptováním dat
        /// </summary>
        bool RowLayoutIsValid { get; set; }
        /// <summary>
        /// true pokud je platný layout sloupců, false po jejich změně před akceptováním dat
        /// </summary>
        bool ColumnLayoutIsValid { get; set; }
    }
    /// <summary>
    /// Člen tabulky Table, u kterého je možno provést Attach a Detach
    /// </summary>
    public interface ITableMember
    {
        /// <summary>
        /// Napojí this objekt do dodané tabulky a uloží do sebe dané ID
        /// </summary>
        /// <param name="table"></param>
        /// <param name="id"></param>
        void AttachToTable(Table table, int id);
        /// <summary>
        /// Odpojí this objekt od navázané tabulky, resetuje svoje ID
        /// </summary>
        void DetachFromTable();
    }
    /// <summary>
    /// Prvek s vizuálním stylem, šířkou a výškou
    /// </summary>
    public interface IVisualMember
    {
        /// <summary>
        /// Visual style for this item. Can be combined with style of all parents, using method VisualStyle.CreateFrom(my style, all parent styles)
        /// </summary>
        VisualStyle Style { get; }
        /// <summary>
        /// Requested Width (for Column)
        /// </summary>
        int? Width { get; }
        /// <summary>
        /// Requested Height (for Row)
        /// </summary>
        int? Height { get; }
    }
    /// <summary>
    /// Prvky umožňující třídění
    /// </summary>
    public interface IComparableItem
    {
        /// <summary>
        /// Metoda připraví hodnotu pro budoucí porovnání.
        /// Hodnota má pocházet z uvedeného prvku (valueId, typicky ColumnId), 
        /// a má být naplněna do Value (pokud valueIsComparable je false) nebo do ValueComparable (pokud valueIsComparable je true).
        /// Po této přípravě bude následovat třídění seznamu položek právě na základě jedné z uvedených hodnot.
        /// </summary>
        /// <param name="valueId">ID hodnoty, typicky ColumnId pro datový řádek</param>
        /// <param name="valueIsComparable">true pokud máme naplnit ValueComparable, false pokud máme naplnit Value</param>
        void PrepareValue(int valueId, bool valueIsComparable);
        /// <summary>
        /// Obecná hodnota
        /// </summary>
        object Value { get; }
        /// <summary>
        /// Hodnota přímo tříditelná
        /// </summary>
        IComparable ValueComparable { get; }
    }
    /// <summary>
    /// Prvek podporující sekvenční layout (řádek, sloupec umístěný v kolekci podobných řádků).
    /// Má svůj Begin a End. Pokud Begin = End, pak prvek nebud zobrazován.
    /// </summary>
    public interface ISequenceLayout
    {
        /// <summary>
        /// Pozice, kde prvek začíná.
        /// Interface ISequenceLayout tuto hodnotu setuje v případě, kdy se layout těchto prvků změní (změna prvků nebo jejich velikosti).
        /// </summary>
        int Begin { get; set; }
        /// <summary>
        /// Velikost prvku v pixelech (šířka sloupce, výška řádku, výška tabulky). 
        /// Lze ji setovat, protože prvky lze pomocí splitterů zvětšovat / zmenšovat.
        /// Aplikační logika prvku musí zabránit vložení neplatné hodnoty (reálně se uloží hodnota platná).
        /// </summary>
        int Size { get; set; }
        /// <summary>
        /// Pozice, kde za tímto prvkem začíná následující prvek. 
        /// Velikost prvku = (End - Begin) = počet pixelů, na které se zobrazuje tento prvek.
        /// Interface ISequenceLayout tuto hodnotu nesetuje, pouze ji čte.
        /// </summary>
        int End { get; }
    }
    #endregion
    #region class SequenceLayout
    /// <summary>
    /// Třída, která v sobě uchovává velikost elementu v jednom směru (šířka sloupce, nebo výška tabulky, nebo výška řádku...), 
    /// současně dokáže tuto hodnotu akceptovat z datové i vizuální vrstvy, dokáže hlídat minimální hodnotu a zadaný rozsah hodnot, a obsahuje i defaultní hodnotu.
    /// Navíc podporuje interface ISequenceLayout, které se používá pro sekvenční řazené prvků za sebe.
    /// </summary>
    public class SequenceLayout : ISequenceLayout
    {
        #region Konstruktor a základní property
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sizeMinimum"></param>
        /// <param name="sizeDefault"></param>
        public SequenceLayout(int sizeMinimum, int sizeDefault)
        {
            this.SizeMinimum = sizeMinimum;
            this.SizeDefault = sizeDefault;
        }
        /// <summary>
        /// Aktuální velikost, vyhodnocená podle všech pravidel a velikosti předků, vždy Int32, tato hodnota není null.
        /// </summary>
        public int CurrentSize { get { return this.GetLayoutSize(); } }
        /// <summary>
        /// Velikost. Lze vložit hodnotu null, a hodnota null může být vrácena z property. Pak není velikost tohoto prvku dána explicitně.
        /// Pokud bylo vloženo null, pak příští get čte hodnotu z ParentLayout (i rekurzivně).
        /// </summary>
        public Int32? Size { get { return (this._Size.HasValue ? this._Size : (this.HasParent ? this.ParentLayout.Size : null)); } set { this.SetLayoutSize(value); } } private Int32? _Size;
        /// <summary>
        /// Minimální velikost prvku
        /// </summary>
        protected int SizeMinimum { get; set; }
        /// <summary>
        /// Implicitní velikost prvku
        /// </summary>
        protected int SizeDefault { get; set; }
        /// <summary>
        /// Rozmezí povolené velikosti prvku. Lze vložit null.
        /// Pokud bylo vloženo null, pak příští get čte hodnotu z ParentLayout (i rekurzivně).
        /// </summary>
        public Int32NRange SizeRange { get { return (this._SizeRange != null ? this._SizeRange : (this.HasParent ? this.ParentLayout.SizeRange : null)); } set { this._SizeRange = value; } } private Int32NRange _SizeRange;
        /// <summary>
        /// true pokud prvek je viditelný (dáno kódem, nikoli fitry atd). Default = true
        /// </summary>
        public bool Visible { get { return this._Visible; } set { this._Visible = value; } } private bool _Visible = true;
        /// <summary>
        /// true pokud uživatel může změnit velikost tohoto prvku. Default = true
        /// </summary>
        public bool ResizeEnabled { get { return this._ResizeEnabled; } set { this._ResizeEnabled = value; } } private bool _ResizeEnabled = true;
        /// <summary>
        /// Parent definice velikosti, z něj se čtou hodnoty které v this prvku nejsou určeny.
        /// Výchozí je null.
        /// </summary>
        public SequenceLayout ParentLayout { get; set; }
        /// <summary>
        /// true pokud má parenta
        /// </summary>
        protected bool HasParent { get { return (this.ParentLayout != null); } }
        /// <summary>
        /// Uloží do this._Size danou hodnotu zarovnanou do platných mezí.
        /// Pokud je na vstupu null, pak uloží null.
        /// </summary>
        /// <param name="size"></param>
        protected void SetLayoutSize(Int32? size)
        {
            if (size.HasValue)
                this._Size = this.AlignSizeToValidRange(size.Value);
            else
                this._Size = null;
        }
        /// <summary>
        /// Vrátí velikost prvku, která se reálně použije pro layout prvku.
        /// Vyhodnocuje Size, SizeRange, ParentLayout, SizeMinimum a SizeDefault.
        /// </summary>
        /// <returns></returns>
        protected int GetLayoutSize()
        {
            Int32? size = this.Size;                       // Automaticky vyhodnocuje ParentLayout
            if (!size.HasValue) size = this.SizeDefault;
            return this.AlignSizeToValidRange(size.Value);
        }
        /// <summary>
        /// Zarovná danou velikost do platných mezí a vráti ji
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected int AlignSizeToValidRange(int size)
        {
            if (size < this.SizeMinimum) size = this.SizeMinimum;
            Int32NRange sizeRange = this.SizeRange;         // Automaticky vyhodnocuje ParentLayout
            if (sizeRange != null)
            {
                Int32? sizeAligned = sizeRange.Align(size);
                if (sizeAligned.HasValue)
                    size = sizeAligned.Value;
            }
            return size;
        }
        #endregion
        #region ISequenceLayout podpora - nápočet hodnot ISequenceLayout.Begin do prvků pole, a do jednotlivého prvku
        /// <summary>
        /// Do všech položek ISequenceLayout dodané kolekce vepíše hodnotu Begin postupně od 0.
        /// K hodnotě position přičte item.Size (pouze pokud je hodnota větší než 0), tato upravená position se vrací v ref parametru, a slouží jako Begin pro další položky v kolekci.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="position"></param>
        public static void SequenceLayoutCalculate(IEnumerable<ISequenceLayout> items)
        {
            int position = 0;
            foreach (ISequenceLayout item in items)
                SequenceLayoutCalculate(item, ref position);
        }
        /// <summary>
        /// Do položky ISequenceLayout vepíše Begin = position.
        /// K hodnotě position přičte item.Size (pouze pokud je hodnota větší než 0), tato upravená position se vrací v ref parametru, a slouží jako Begin pro další položky v kolekci.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="position"></param>
        public static void SequenceLayoutCalculate(ISequenceLayout item, ref int position)
        {
            item.Begin = position;
            int size = item.Size;
            if (size > 0) position += size;
        }
        #endregion
        #region ISequenceLayout podpora - filtrování prvků typu ISequenceLayout podle viditelné oblasti
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataBegin">První viditelný datový pixel</param>
        /// <param name="dataEnd">První datový pixel za viditelnou oblastí</param>
        /// <returns></returns>
        public static bool IsItemVisible(ISequenceLayout item, int dataBegin, int dataEnd)
        {
            return _FilterVisibleItem(item, dataBegin, dataEnd);
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static bool IsItemVisible(ISequenceLayout item, Int32NRange dataRange)
        {
            return _FilterVisibleItem(item, dataRange);
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static bool IsItemVisible(ISequenceLayout item, Int32Range dataRange)
        {
            return _FilterVisibleItem(item, dataRange);
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T">Typ datových položek</typeparam>
        /// <param name="items">Kolekce datových položek</param>
        /// <param name="dataBegin">První viditelný datový pixel</param>
        /// <param name="dataEnd">První datový pixel za viditelnou oblastí</param>
        /// <returns></returns>
        public static IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items, int dataBegin, int dataEnd) where T : ISequenceLayout
        {
            return items.Where(i => _FilterVisibleItem(i, dataBegin, dataEnd));
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T">Typ datových položek</typeparam>
        /// <param name="items">Kolekce datových položek</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items, Int32NRange dataRange) where T : ISequenceLayout
        {
            return items.Where(i => _FilterVisibleItem(i, dataRange));
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T">Typ datových položek</typeparam>
        /// <param name="items">Kolekce datových položek</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items, Int32Range dataRange) where T : ISequenceLayout
        {
            return items.Where(i => _FilterVisibleItem(i, dataRange));
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="begin">První viditelný datový pixel</param>
        /// <param name="end">První datový pixel za viditelnou oblastí</param>
        /// <returns></returns>
        private static bool _FilterVisibleItem(ISequenceLayout item, int dataBegin, int dataEnd)
        {
            return (item.Size > 0 
                && item.Begin < dataEnd 
                && item.End > dataBegin);
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        private static bool _FilterVisibleItem(ISequenceLayout item, Int32NRange dataRange)
        {
            return (item.Size > 0 
                && (!dataRange.HasEnd || item.Begin < dataRange.End.Value) 
                && (!dataRange.HasBegin || item.End > dataRange.Begin.Value));
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        private static bool _FilterVisibleItem(ISequenceLayout item, Int32Range dataRange)
        {
            return (item.Size > 0
                && item.Begin < dataRange.End
                && item.End > dataRange.Begin);
        }
        #endregion
        #region implementace ISequenceLayout = pořadí, počáteční pixel, velikost, následující pixel. Podpůrné metody GetLayoutSize() a SetLayoutSize().
        /// <summary>
        /// Pozice, kde prvek začíná.
        /// Interface ISequenceLayout tuto hodnotu setuje v případě, kdy se layout těchto prvků změní (změna prvků nebo jejich velikosti).
        /// </summary>
        int ISequenceLayout.Begin { get { return _ISequenceLayoutBegin; } set { _ISequenceLayoutBegin = value; } } private int _ISequenceLayoutBegin;
        /// <summary>
        /// Velikost prvku v pixelech (šířka sloupce, výška řádku, výška tabulky). 
        /// Lze ji setovat, protože prvky lze pomocí splitterů zvětšovat / zmenšovat.
        /// Pokud ale prvek nemá povoleno Resize (ResizeEnabled je false), pak setování hodnoty je ignorováno.
        /// Aplikační logika prvku musí zabránit vložení neplatné hodnoty (reálně se uloží hodnota platná).
        /// </summary>
        int ISequenceLayout.Size { get { return this.GetLayoutSize(); } set { if (this.ResizeEnabled) this.SetLayoutSize(value); } }
        /// <summary>
        /// Pozice, kde za tímto prvkem začíná následující prvek. 
        /// Velikost prvku = (End - Begin) = počet pixelů, na které se zobrazuje tento prvek.
        /// Interface ISequenceLayout tuto hodnotu nesetuje, pouze ji čte.
        /// </summary>
        int ISequenceLayout.End { get { return this._ISequenceLayoutBegin + (this.Visible ? this.GetLayoutSize() : 0); } }
        #endregion
    }
    #endregion
    #region Enums
    /// <summary>
    /// Typ třídění
    /// </summary>
    public enum TableSortRowType
    {
        /// <summary>
        /// Netřídit
        /// </summary>
        None = 0,
        /// <summary>
        /// Vzestupně
        /// </summary>
        Ascending = 1,
        /// <summary>
        /// Sestupně
        /// </summary>
        Descending = 2
    }
    #endregion


    [TestClass]
    public class Test_Table
    {
        [TestMethod]
        public void Test()
        {
            Table tbl = new Table();
            tbl.AddRow(12, 34, 56, 78);
            tbl.AddColumns("a", "b", "c", "d");
            tbl.Rows[0].Height = 65;
            Row row = new Row(1, 2, 3, 4);
            tbl.AddRow(row);
        }
    }
}
