using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Components.Grid;
using Asol.Tools.WorkScheduler.Application;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Data
{
    // Tento soubor obsahuje datové prvky pro tabulku s daty. 
    // To jest: Tabulka, Sloupec, Řádek, Buňka. 
    // A dále interface pro vizualizaci tabulky, které datová tabulka implementuje, a podporu pro třídění.
    // Datové prvky mají sadu datových public property, ale současně slouží jako nativní podklad pro grafické objekty (GGrid, GTable).
    // Pro jejich požadavky jsou do datových prvků explicitně implementovány rozličné interface, 
    //  takže grafické prvky si objekty přetypují a pracují přes interface, a tyto sady rozšiřujících prvků jsou uživateli skryté při běžné datové práci (=bez interface).

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

    #region Table
    /// <summary>
    /// Table : jedna tabulka s daty (sada Column + Row)
    /// </summary>
    public partial class Table : IGTableMember, IVisualMember, IContentValidity, ITableInternal
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
            this._TableId = Application.App.GetNextId(this.GetType());
            this._PrimaryKeyInit();
            this._TreeChildInit();
            this._ColumnsInit();
            this._RowsInit();
            this._LayoutInit();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "Table";
            if (!String.IsNullOrEmpty(this.TableName)) text += " Name: " + this.TableName;
            text += "; Columns: " + this.ColumnsCount.ToString();
            text += "; Rows: " + this.RowsCount.ToString();
            return text;
        }
        /// <summary>
        /// Jednoduché textové vyjádření obsahu this instance
        /// </summary>
        public string Text
        {
            get
            {
                string text = "Table " + (!String.IsNullOrEmpty(this.TableName) ? this.TableName.Trim() : "Id" + this.TableId.ToString());
                return text;
            }
        }
        /// <summary>
        /// Název tabulky, podle něj lze hledat. jde o klíčové slovo, nikoli popisek - ten je v <see cref="Title"/>.
        /// </summary>
        public string TableName { get { return this._TableName; } set { this._TableName = value; } }
        private string _TableName;
        /// <summary>
        /// Titulek tabulky, bude použit tehdy, když tabulka bude umístěna ve vhodném kontejneru.
        /// </summary>
        public Localizable.TextLoc Title { get { return this._Title; } set { this._Title = value; } }
        private Localizable.TextLoc _Title;
        /// <summary>
        /// Ikonka tabulky, bude použita tehdy, když tabulka bude umístěna ve vhodném kontejneru.
        /// </summary>
        public Image Image { get { return this._Image; } set { this._Image = value; } }
        private Image _Image;
        /// <summary>
        /// Jednoznačné ID tabulky
        /// </summary>
        public int TableId { get { return this._TableId; } }
        private int _TableId;
        /// <summary>
        /// Libovolná uživatelská data
        /// </summary>
        public object UserData { get { return this._UserData; } set { this._UserData = value; } }
        private object _UserData;
        /// <summary>
        /// Číslo třídy, z níž pochází data šablony
        /// </summary>
        public int? ClassId { get { return this._ClassId; } } private int? _ClassId;
        /// <summary>
        /// Číslo šablony
        /// </summary>
        public int? TemplateId { get { return this._TemplateId; } } private int? _TemplateId;
        #endregion
        #region GTable - Linkování datové tabulky do grafické tabulky
        /// <summary>
        /// Reference na vizuální tabulku (GTable), může být null
        /// </summary>
        public GTable GTable { get { return this._GTable; } }
        /// <summary>
        /// IGTableMember: Vizuální tabulka s možností setování
        /// </summary>
        GTable IGTableMember.GTable { get { return this._GTable; } set { this._GTable = value; } }
        /// <summary>
        /// Vizuální tabulka (GTable)
        /// </summary>
        private GTable _GTable;
        /// <summary>
        /// true pokud má referenci na vizuální tabulku (GTable)
        /// </summary>
        public bool HasGTable { get { return (this._GTable != null); } }
        /// <summary>
        /// Napojí se na danou vizuální tabulku (GTable)
        /// </summary>
        /// <param name="gTable"></param>
        protected void _GTableLink(GTable gTable)
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

            ITableMember itm = column as ITableMember;
            if (itm != null)
            {
                itm.Table = this;
                itm.Id = id;
            }
            if (column.ColumnOrder < 0) column.ColumnOrder = id;

            if (!this._ColumnIdDict.ContainsKey(id))
                this._ColumnIdDict.Add(id, column);
            string name = column.ColumnName;
            if (!String.IsNullOrEmpty(name) && !this._ColumnNameDict.ContainsKey(name))
                this._ColumnNameDict.Add(name, column);

            if (!this._ClassId.HasValue && column.RecordClassNumber.HasValue) this._ClassId = column.RecordClassNumber;
            if (!this._TemplateId.HasValue && column.TemplateId.HasValue) this._TemplateId = column.TemplateId;
            if (this._PrimaryKeyColumn == null && column.AllowPrimaryKey) this._PrimaryKeyColumn = column;
            column.ColumnSize.Parent = this.ColumnSize;
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

            ITableMember itm = column as ITableMember;
            if (itm != null)
            {
                itm.Table = null;
                itm.Id = -1;
            }

            if (this._ColumnIdDict.ContainsKey(id))
                this._ColumnIdDict.Remove(id);
            string name = column.ColumnName;
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
        /// Počet všech řádků v tabulce (viditelné + neviditelné; Root + Childs)
        /// </summary>
        public int RowsCount { get { return this._Rows.Count; } }
        /// <summary>
        /// Obsahuje true, pokud v této tabulce existuje nějaký řádek, který potřebuje časovou osu ke svému vlastnímu zobrazení 
        /// (tj. má nastaveno <see cref="Row.UseBackgroundTimeAxis"/> == true).
        /// </summary>
        internal bool UseBackgroundTimeAxis { get { return this.Rows.Any(r => r.UseBackgroundTimeAxis); } }
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

            ITableMember itm = row as ITableMember;
            if (itm != null)
            {
                itm.Table = this;
                itm.Id = id;
            }

            if (row.TagItems != null)
                this._InvalidateTagItems();

            this._PrimaryKeyAddRow(row);
            this.TreeChildAdded(row);

            this.RowsValid = false;
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
            Row row = args.Item;

            this.TreeChildRemoved(row);
            this._PrimaryKeyRemoveRow(row);

            ITableMember itm = row as ITableMember;
            if (itm != null)
            {
                itm.Table = null;
                itm.Id = -1;
            }
            this._InvalidateTagItems();
            this.RowsValid = false;
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
        #region Viditelné řádky, třídění a filtrování řádků
        /// <summary>
        /// Tato property vrací soupis všech zobrazitelných (=aktuálně viditelných) řádků, setříděný podle prvního ze sloupců, který má nastaven režim třídění jiný než None.
        /// Tato property vždy kompletně vyhodnotí data a vrátí nový objekt, nepoužívá se žádná úroveň cachování. 
        /// Pozor na výkon, užívejme tuto property střídmě.
        /// </summary>
        public Row[] RowsSorted
        {
            get
            {
                // Základní viditelnost řádku daná kódem (Row.Visible) a řádkovým filtrem (RowFiltersExists):
                bool rowFiltersExists = this.RowFiltersExists;
                List<Row> list = new List<Row>();
                foreach (Row row in this.Rows)
                {
                    bool isVisible = false;
                    if (!row.Hidden && row.TreeNode.IsRoot && (rowFiltersExists ? this.FilterRow(row) : true))
                    {
                        isVisible = true;
                        list.Add(row);
                    }
                    // Visible = true dostanou jen řádky, které jsou zobrazeny v úrovni Root. 
                    //   Child řádky dostanou false, na true přejdou až později v metodě CreateTreeViewList().
                    row.Visible = isVisible;     // Viditelnost pro všechny řádky je zde, přičemž pouze Root úroveň zde může dostat Visible = true.
                }

                // Třídění Root úrovně podle sloupce:
                if (list.Count > 1)
                {
                    Column sortColumn = this.Columns.FirstOrDefault(c => (c.SortCurrent == ItemSortType.Ascending || c.SortCurrent == ItemSortType.Descending));
                    if (sortColumn != null)
                        // Bude to tříděné...
                        _RowsSort(list, sortColumn);
                }

                // TreeView:
                if (this.IsTreeViewTable)
                    list = this.CreateTreeViewList(list);

                return list.ToArray();
            }
        }
        /// <summary>
        /// Proměnná, která pomáhá detekovat platnost pole řádků.
        /// Po různých změnách je nastavována na false, aplikace (<see cref="GTable"/>) může nastavit na true při převzetí řádků.
        /// </summary>
        public bool RowsValid { get; set; }
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
            bool valueIsComparable = !columnHasComparator;
            list.ForEach(r => (r as IComparableItem).PrepareValue(sortColumn.ColumnId, valueIsComparable));

            switch (sortColumn.SortCurrent)
            {
                case ItemSortType.Ascending:
                    if (columnHasComparator)
                        list.Sort((a, b) => sortColumn.ValueComparator((a as IComparableItem).Value, (b as IComparableItem).Value));
                    else
                        list.Sort((a, b) => SortRowsCompare((a as IComparableItem).ValueComparable, (b as IComparableItem).ValueComparable));
                    break;

                case ItemSortType.Descending:
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
        /// Do tabulky přidá další filtr
        /// </summary>
        /// <param name="filterName">Název filtru</param>
        /// <param name="filterFunc">Funkce filtru</param>
        public void AddFilter(string filterName, Func<Row, bool> filterFunc)
        {
            if (String.IsNullOrEmpty(filterName))
                throw new GraphLibCodeException("Do tabulky nelze přidat filtr, jehož název není zadán.");
            if (filterFunc == null)
                throw new GraphLibCodeException("Do tabulky nelze přidat filtr, jehož funkce by byla null.");

            TableFilter tableFilter = new TableFilter(filterName);
            tableFilter.Filter = filterFunc;
            this.AddFilter(tableFilter);
        }
        /// <summary>
        /// Do tabulky přidá další filtr
        /// </summary>
        /// <param name="tableFilter"></param>
        public void AddFilter(TableFilter tableFilter)
        {
            if (tableFilter == null)
                throw new GraphLibCodeException("Do tabulky nelze přidat filtr, který je null.");
            if (String.IsNullOrEmpty(tableFilter.Name))
                throw new GraphLibCodeException("Do tabulky nelze přidat filtr, jehož název není zadán.");

            if (this.FilterDict.ContainsKey(tableFilter.Name))
                this.FilterDict[tableFilter.Name] = tableFilter;
            else
                this.FilterDict.Add(tableFilter.Name, tableFilter);
            this.InvalidateRows();
        }
        /// <summary>
        /// Odebere daný filtr
        /// </summary>
        /// <param name="filter"></param>
        public bool RemoveFilter(TableFilter filter)
        {
            return (filter != null ? this.RemoveFilter(filter.Name) : false);
        }
        /// <summary>
        /// Odebere filtr daného jména
        /// </summary>
        /// <param name="name"></param>
        public bool RemoveFilter(string name)
        {
            if (!String.IsNullOrEmpty(name) && this.FilterDict.ContainsKey(name))
            {
                this.FilterDict.Remove(name);
                this.InvalidateRows();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Zahodí všechny filtry
        /// </summary>
        public void ResetFilters()
        {
            this.FilterDict.Clear();
            this.InvalidateRows();
        }
        /// <summary>
        /// Invaliduje počet řádků v grafické tabulce
        /// </summary>
        internal void InvalidateRows()
        {
            this.RowsValid = false;
            if (this.HasGTable)
                this.GTable.Invalidate(InvalidateItem.RowsCount);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný řádek má být zobrazen v kolekci <see cref="RowsSorted"/>, na základě řádkových filtrů.
        /// Tuto metodu nemá význam volat, když <see cref="RowFiltersExists"/> je false.
        /// Tato metoda ale sama <see cref="RowFiltersExists"/> netestuje, kvůli rychlosti (předpokládá se, že tuto hodnotu otestovala funkce volající).
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected bool FilterRow(Row row)
        {
            foreach (TableFilter tableFilter in this.FilterDict.Values)
            {
                if (!tableFilter.Filter(row)) return false;
            }
            return true;
        }
        /// <summary>
        /// Obsahuje true, pokud existují nějaké řádkové filtry, a volání metody <see cref="FilterRow(Row)"/> má význam.
        /// Pokud neexistují řádkové filtr, berou se řádky za viditelné.
        /// </summary>
        public bool RowFiltersExists { get { return (this._FilterDict != null && this._FilterDict.Count > 0); } }
        /// <summary>
        /// Index filtrů
        /// </summary>
        protected Dictionary<string, TableFilter> FilterDict { get { if (this._FilterDict == null) this._FilterDict = new Dictionary<string, TableFilter>(); return this._FilterDict; } }
        private Dictionary<string, TableFilter> _FilterDict;
        #endregion
        #region TreeView, Statické a Dynamické Child řádky
        /// <summary>
        /// Inicializace dat pro TreeChilds
        /// </summary>
        private void _TreeChildInit()
        {
            this.TreeDynamicChildDict = new Dictionary<int, Row>();
        }
        /// <summary>
        /// Po přidání řádku, po jeho přidání do indexu.
        /// Řeší věci okolo Child řádků.
        /// </summary>
        /// <param name="row"></param>
        protected void TreeChildAdded(Row row)
        {
            TreeNode treeNode = row.TreeNode;
            if (treeNode.IsRoot) return;
            if (treeNode.IsStaticChild)
            {   // Statický Child: má svého fixního Parenta:
                Row parentRow;
                if (this.TryGetRow(row.ParentRecordGId, out parentRow))
                    parentRow.TreeNode.AddStaticChild(row);
            }
            else if (treeNode.IsDynamicChild)
            {   // Dynamický Child: může být Childem pod kýmkoliv:
                this.TreeDynamicChildDict.AddRefresh(row.RowId, row);
            }
        }
        /// <summary>
        /// Po odebrání řádku, před jeho odebráním z indexu
        /// Řeší věci okolo Child řádků.
        /// </summary>
        /// <param name="row"></param>
        protected void TreeChildRemoved(Row row)
        {
            TreeNode treeNode = row.TreeNode;
            if (treeNode.IsRoot) return;
            if (treeNode.IsStaticChild)
            {   // Statický Child: má svého fixního Parenta:
                Row parentRow;
                if (this.TryGetRow(row.ParentRecordGId, out parentRow))
                    parentRow.TreeNode.RemoveStaticChild(row);
            }
            else if (treeNode.IsDynamicChild)
            {   // Dynamický Child: může být Childem pod kýmkoliv:
                this.TreeDynamicChildDict.RemoveIfExists(row.RowId);
            }
        }
        /// <summary>
        /// Soupis řádků v this tabulce, které jsou Dynamické Childs.
        /// Jsou to řádky this tabulky, které nejsou Root, a mají jako svůj Parent = Empty GID.
        /// Systém je může používat jako zdroj dynamických childs podle svých pravidel.
        /// Toto pole není nikdy null.
        /// </summary>
        public IEnumerable<Row> DynamicChilds { get { return this.TreeDynamicChildDict.Values; } }
        /// <summary>
        /// Index řádků v this tabulce, které jsou Dynamické Childs.
        /// Jsou to řádky this tabulky, které nejsou Root, a mají jako svůj Parent = Empty GID.
        /// Systém je může používat jako zdroj dynamických childs podle svých pravidel.
        /// </summary>
        protected Dictionary<int, Row> TreeDynamicChildDict { get; set; }
        /// <summary>
        /// Obsahuje true, pokud v této tabulce existuje nějaký řádek, který je Child k nějakému Parentovi.
        /// V takovém případě se Table bude vykreslovat jako TreeView.
        /// </summary>
        public bool IsTreeViewTable { get { return this.Rows.Any(r => r.TreeNode.IsChild); } }
        /// <summary>
        /// Pole řádků, které jsou Root v TreeView
        /// </summary>
        public Row[] TreeNodeRootRows { get { return this.Rows.Where(r => r.TreeNode.IsRoot).ToArray(); } }
        /// <summary>
        /// Provede scanování všech řádků a jejich Childs kolekcí
        /// </summary>
        /// <param name="scanAction">Akce volaná pro každý řádek, druhý parametr je level; 0 = root</param>
        /// <param name="testScanChilds">Volitelná funkce, která rozhoduje, zda se mají scanovat Childs prvky</param>
        public void TreeNodeScan(Action<Row, int> scanAction, Func<Row, bool> testScanChilds = null)
        {
            TreeNode.Scan(this.TreeNodeRootRows, 0, scanAction, testScanChilds);
        }
        /// <summary>
        /// Metoda vrátí lineární seznam řádků, vzniklý z řádků úrovně Root plus všechny Child řádky z nodů, které jsou Expanded
        /// </summary>
        /// <param name="rootRowList"></param>
        /// <returns></returns>
        protected List<Row> CreateTreeViewList(List<Row> rootRowList)
        {
            List<Row> treeList = new List<Row>();
            foreach (Row rootRow in rootRowList)
            {
                treeList.Add(rootRow);
                rootRow.TreeNode.StoreChildsTo(treeList);
            }
            return treeList;
        }
        #endregion
        #region TagItems = štítky náležející k řádku, podpora pro filtrování
        /// <summary>
        /// Obsahuje true, pokud this tabulka má alespoň jednu položku v <see cref="TagItems"/>.
        /// </summary>
        public bool HasTagItems { get { this._CheckTagItems(); return (this._TagItems.Length > 0); } }
        /// <summary>
        /// Soupis štítků ze všech řádků.
        /// Hodnota Key = text Tagu; hodnota Value = počet výskytů v řádcích. Počet může mít vliv na velikost štítku.
        /// </summary>
        public TagItem[] TagItems { get { this._CheckTagItems(); return this._TagItems; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterTagItems"></param>
        public void TagItemsSetFilter(TagItem[] filterTagItems)
        {
            if (filterTagItems == null || filterTagItems.Length == 0)
                this.RemoveFilter(TagItemFilterName);
            else
            {
                this.TagItemFilterItems = filterTagItems;
                this.AddFilter(TagItemFilterName, this.TagItemFilter);
            }
        }
        /// <summary>
        /// Metoda provede filtrování daného řádku pomocí TagFilter
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected bool TagItemFilter(Row row)
        {
            return ((ITagItemOwner)row).FilterByTagValues(this.TagItemFilterItems);
        }
        /// <summary>
        /// Položky filtru TagItems
        /// </summary>
        protected TagItem[] TagItemFilterItems { get; private set; }
        /// <summary>
        /// Název filtru podle TagItems
        /// </summary>
        protected const string TagItemFilterName = "TagItemFilterName";
        /// <summary>
        /// Výška jednoho řádku filtru s položkami <see cref="TagItems"/>.
        /// Nezadáno = default (24 pixelů)
        /// </summary>
        public int? TagItemsRowHeight { get; set; }
        /// <summary>
        /// Zajistí platnost dat v poli <see cref="_TagItems"/>
        /// </summary>
        private void _CheckTagItems()
        {
            if (this._TagItems != null) return;

            Dictionary<string, TagItem> tagDict = new Dictionary<string, TagItem>();
            foreach (Row row in this.Rows)
                ((ITagItemOwner)row).PrepareSummaryDict(tagDict);

            List<KeyValuePair<string, TagItem>> tagList = tagDict.ToList();
            if (tagList.Count > 1)
                tagList.Sort((a, b) => String.Compare(a.Key, b.Key));

            this._TagItems = tagList.Select(i => i.Value).ToArray();
        }
        /// <summary>
        /// Invaliduje pole štítků
        /// </summary>
        void ITableInternal.InvalidateTagItems()
        {
            this._InvalidateTagItems();
        }
        /// <summary>
        /// Invaliduje pole štítků
        /// </summary>
        private void _InvalidateTagItems()
        {
            this._TagItems = null;
            this.CallTagItemsChanged();
        }
        /// <summary>
        /// Vyvolá akce po změně TagItems
        /// </summary>
        protected void CallTagItemsChanged()
        {
            this.OnTagItemsChanged();
            if (this.TagItemsChanged != null)
                this.TagItemsChanged(this, new EventArgs());
        }
        /// <summary>
        /// Háček po změně TagItems
        /// </summary>
        protected virtual void OnTagItemsChanged() { }
        /// <summary>
        /// Event po změně TagItems
        /// </summary>
        public event EventHandler TagItemsChanged;
        /// <summary>
        /// Soupis všech štítků ze všech řádků.
        /// </summary>
        private TagItem[] _TagItems;
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
            get { return this.TableSize.Size.Value; }
            set
            {
                int oldValue = this.Height;
                this.TableSize.Size = value;
                int newValue = this.Height;
                if ((newValue != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableHeight);
            }
        }
        /// <summary>
        /// true pro viditelnou tabulku (default), false pro skrytou
        /// </summary>
        public bool IsVisible
        {
            get { return this.TableSize.Visible; }
            set
            {
                bool oldValue = this.IsVisible;
                this.TableSize.Visible = value;
                bool newValue = this.IsVisible;
                if ((newValue != oldValue) && this.HasGTable)
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
            get { return this.HeaderColumnSize.Size.Value; }
            set
            {
                int oldValue = this.ColumnHeaderHeight;
                this.HeaderColumnSize.Size = value;
                int newValue = this.ColumnHeaderHeight;
                if ((newValue != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.ColumnHeader);
            }
        }
        /// <summary>
        /// Šířka sloupce obsahujícího RowHeader
        /// </summary>
        public Int32 RowHeaderWidth
        {
            get { return this.HeaderRowSize.Size.Value; }
            set
            {
                int oldValue = this.RowHeaderWidth;
                this.HeaderRowSize.Size = value;
                int newValue = this.RowHeaderWidth;
                if ((newValue != oldValue) && this.HasGTable)
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
            get { return this._TableOrder; }
            set
            {
                int oldValue = this._TableOrder;
                this._TableOrder = value;
                int newValue = this._TableOrder;
                if ((newValue != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.ColumnHeader);
            }
        } private int _TableOrder = -1;
        /// <summary>
        /// true pokud je povoleno interaktivně změnit výšku tabulky (myší). Default = true;
        /// </summary>
        public bool AllowTableResize
        {
            get { return this.TableSize.ResizeEnabled.Value; }
            set
            {
                bool oldValue = this.AllowTableResize;
                this.TableSize.ResizeEnabled = value;
                bool newValue = this.AllowTableResize;
                if ((newValue != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.GridItems);
            }
        }
        /// <summary>
        /// true pokud je povoleno interaktivně změnit šířku sloupce, který obsahuje záhlaví řádku (myší). Default = true;
        /// </summary>
        public bool AllowRowHeaderWidthResize
        {
            get { return this.HeaderRowSize.ResizeEnabled.Value; }
            set
            {
                bool oldValue = this.AllowRowHeaderWidthResize;
                this.HeaderRowSize.ResizeEnabled = value;
                bool newValue = this.AllowRowHeaderWidthResize;
                if ((newValue != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        }
        /// <summary>
        /// true pokud je povoleno interaktivně přemisťovat sloupce (přetahovat je myší). Default = true;
        /// </summary>
        public bool AllowColumnReorder
        {
            get { return this.HeaderColumnSize.ReOrderEnabled.Value; }
            set
            {
                bool oldValue = this.AllowColumnReorder;
                this.HeaderColumnSize.ReOrderEnabled = value;
                bool newValue = this.AllowColumnReorder;
                if ((newValue != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        }
        /// <summary>
        /// true pokud je povoleno interaktivně změnit výšku záhlaví sloupců (myší). Default = true;
        /// </summary>
        public bool AllowColumnHeaderResize
        {
            get { return this._AllowColumnHeaderResize; }
            set
            {
                bool oldValue = this._AllowColumnHeaderResize;
                this._AllowColumnHeaderResize = value;
                if ((this._AllowColumnHeaderResize != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        } private bool _AllowColumnHeaderResize = true;
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
        /// true pokud je povoleno vybírat jednotlivé buňky tabulky, false pokud celý řádek. Default = false;
        /// </summary>
        public bool AllowSelectSingleCell { get { return this._AllowSelectSingleCell; } set { this._AllowSelectSingleCell = value; } } private bool _AllowSelectSingleCell = false;
        /// <summary>
        /// Image použitý pro zobrazení Selected řádku v prostoru RowHeader. Default = IconStandard.RowSelected;
        /// </summary>
        public Image SelectedRowImage { get { return this._SelectedRowImage; } set { this._SelectedRowImage = value; } } private Image _SelectedRowImage = IconStandard.RowSelected;
        /// <summary>
        /// Defaultní parametry pro grafy na pozadí této tabulky, anebo defaultní parametry pro grafy ve sloupci.
        /// Tato property nikdy není null (ve výchozím stavu má hodnotu <see cref="Components.Graph.TimeGraphProperties.Default"/>).
        /// </summary>
        public Components.Graph.TimeGraphProperties GraphParameters
        {
            get
            {
                if (this._GraphParameters == null)
                    this._GraphParameters = Components.Graph.TimeGraphProperties.Default;
                return this._GraphParameters;
            }
            set
            {
                this._GraphParameters = value;
            }
        }
        private Components.Graph.TimeGraphProperties _GraphParameters;
        /// <summary>
        /// Přepočítávat vždy souřadnice VŠECH řádků, i když nejsou ve viditelné oblasti?
        /// Nastavení na true je nutné tehdy, kdyý tabulka obsahuje grafy, a grafy mohou zobrazovat Linky. 
        /// Linky totiž mohou vést i na prvky, které jsou mimo viditelnou oblast, a pak potřebujeme znát jejich správné souřadnice.
        /// </summary>
        public bool CalculateBoundsForAllRows
        {
            get { return this._CalculateBoundsForAllRows; }
            set
            {
                bool oldValue = this._CalculateBoundsForAllRows;
                this._CalculateBoundsForAllRows = value;
                if ((this._CalculateBoundsForAllRows != oldValue) && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.TableItems);
            }
        }
        private bool _CalculateBoundsForAllRows = false;
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
        public GTableHeader TableHeader
        {
            get
            {
                if (this._TableHeader == null)
                    this._TableHeader = new GTableHeader(this);
                return this._TableHeader;
            }
            set { this._TableHeader = value; }
        }
        private GTableHeader _TableHeader;
        #endregion
        #region Layouty (výšky, šířky, rozmezí) : kořenové hodnoty uložené na tabulce
        /// <summary>
        /// Koordinátor výšky tabulky
        /// </summary>
        internal ItemSizeInt TableSize { get { if (this._TableSize == null) this._TableSize = new ItemSizeInt(60, 300, 4000); return this._TableSize; } }
        private ItemSizeInt _TableSize;
        /// <summary>
        /// Koordinátor výšky prostoru ColumnHeader
        /// </summary>
        internal ItemSizeInt HeaderColumnSize { get { if (this._HeaderColumnSize == null) this._HeaderColumnSize = new ItemSizeInt(20, 45, 160); return this._HeaderColumnSize; } }
        private ItemSizeInt _HeaderColumnSize;
        /// <summary>
        /// Koordinátor výšky prostoru ColumnHeader
        /// </summary>
        internal ItemSizeInt HeaderRowSize { get { if (this._HeaderRowSize == null) this._HeaderRowSize = new ItemSizeInt(20, 35, 120); return this._HeaderRowSize; } }
        private ItemSizeInt _HeaderRowSize;
        /// <summary>
        /// Koordinátor výšky řádku
        /// </summary>
        internal ItemSizeInt RowSize { get { if (this._RowSize == null) this._RowSize = new ItemSizeInt(8, 24, 512); return this._RowSize; } }
        private ItemSizeInt _RowSize;
        /// <summary>
        /// Koordinátor šířky sloupce
        /// </summary>
        internal ItemSizeInt ColumnSize { get { if (this._ColumnSize == null) this._ColumnSize = new ItemSizeInt(20, 100, 4000); return this._ColumnSize; } }
        private ItemSizeInt _ColumnSize;
        /// <summary>
        /// Inicializace prvků <see cref="ItemSizeInt"/>, definujících layout prvků v tabulce
        /// </summary>
        private void _LayoutInit()
        {
            this._TableSize = new ItemSizeInt(60, 300, 4000);
            this._HeaderColumnSize = new ItemSizeInt(20, 45, 160);
            this._HeaderRowSize = new ItemSizeInt(20, 35, 120);
            this._RowSize = new ItemSizeInt(8, 24, 512);
            this._ColumnSize = new ItemSizeInt(20, 100, 4000);
        }
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
        /// Všechny vizuální vlastnosti dat v této tabulce.
        /// Tato property je autoinicializační = nikdy není null.
        /// </summary>
        public VisualStyle VisualStyle
        {
            get
            {
                if (this._VisualStyle == null)
                    this._VisualStyle = new VisualStyle();
                return this._VisualStyle;
            }
            set { this._VisualStyle = value; }
        }
        private VisualStyle _VisualStyle = null;
        VisualStyle IVisualMember.Style
        {
            get
            {
                return VisualStyle.CreateFrom(this.VisualStyle);
            }
        }
        #endregion
        #region ITableValidity
        bool IContentValidity.DataIsValid { get { return _RowDataIsValid; } set { _RowDataIsValid = value; } } private bool _RowDataIsValid;
        bool IContentValidity.RowLayoutIsValid { get { return _RowLayoutIsValid; } set { _RowLayoutIsValid = value; } } private bool _RowLayoutIsValid;
        bool IContentValidity.ColumnLayoutIsValid { get { return _ColumnLayoutIsValid; } set { _ColumnLayoutIsValid = value; } } private bool _ColumnLayoutIsValid;
        #endregion
        #region Eventy vyvolávané z grafické vrstvy
        /// <summary>
        /// Obsluha události Změna Hot řádku (pod myší)
        /// </summary>
        /// <param name="oldActiveRow"></param>
        /// <param name="newHotRow"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallHotRowChanged(Row oldActiveRow, Row newHotRow, EventSourceType eventSource, bool callEvents)
        {
            GPropertyChangeArgs<Row> args = new GPropertyChangeArgs<Row>(oldActiveRow, newHotRow, eventSource);
            this.OnHotRowChanged(args);
            if (callEvents && this.HotRowChanged != null)
                this.HotRowChanged(this, args);
        }
        /// <summary>
        /// Háček OnHotRowChanged
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnHotRowChanged(GPropertyChangeArgs<Row> args) { }
        /// <summary>
        /// Událost, která se vyvolá pokud uživatel posune myš (jen pohyb, bez drag/drop, bez click) na nový řádek
        /// </summary>
        public event GPropertyChangedHandler<Row> HotRowChanged;

        /// <summary>
        /// Obsluha události Změna Hot buňky (pod myší)
        /// </summary>
        /// <param name="oldHotCell"></param>
        /// <param name="newHotCell"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallHotCellChanged(Cell oldHotCell, Cell newHotCell, EventSourceType eventSource, bool callEvents)
        {
            GPropertyChangeArgs<Cell> args = new GPropertyChangeArgs<Cell>(oldHotCell, newHotCell, eventSource);
            this.OnHotCellChanged(args);
            if (callEvents && this.HotCellChanged != null)
                this.HotCellChanged(this, args);
        }
        /// <summary>
        /// Háček OnHotCellChanged
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnHotCellChanged(GPropertyChangeArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá pokud uživatel posune myš (jen pohyb, bez drag/drop, bez click) na novou buňku (v témže řádku, v jiném řádku)
        /// </summary>
        public event GPropertyChangedHandler<Cell> HotCellChanged;

        /// <summary>
        /// Obsluha události Změna aktivního řádku
        /// </summary>
        /// <param name="oldActiveRow"></param>
        /// <param name="newActiveRow"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallActiveRowChanged(Row oldActiveRow, Row newActiveRow, EventSourceType eventSource, bool callEvents)
        {
            this.ActiveRow = newActiveRow;
            GPropertyChangeArgs<Row> args = new GPropertyChangeArgs<Row>(oldActiveRow, newActiveRow, eventSource);
            this.OnActiveRowChanged(args);
            if (callEvents && this.ActiveRowChanged != null)
                this.ActiveRowChanged(this, args);
        }
        /// <summary>
        /// Háček OnActiveRowChanged Změna aktivního řádku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActiveRowChanged(GPropertyChangeArgs<Row> args) { }
        /// <summary>
        /// Event Změna aktivního řádku
        /// </summary>
        public event GPropertyChangedHandler<Row> ActiveRowChanged;

        /// <summary>
        /// Obsluha události Změna aktivní buňky
        /// </summary>
        /// <param name="oldActiveCell"></param>
        /// <param name="newActiveCell"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallActiveCellChanged(Cell oldActiveCell, Cell newActiveCell, EventSourceType eventSource, bool callEvents)
        {
            this.ActiveCell = newActiveCell;
            GPropertyChangeArgs<Cell> args = new GPropertyChangeArgs<Cell>(oldActiveCell, newActiveCell, eventSource);
            this.OnActiveCellChanged(args);
            if (callEvents && this.ActiveCellChanged != null)
                this.ActiveCellChanged(this, args);
        }
        /// <summary>
        /// Háček Změna aktivní buňky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActiveCellChanged(GPropertyChangeArgs<Cell> args) { }
        /// <summary>
        /// Event Změna aktivní buňky
        /// </summary>
        public event GPropertyChangedHandler<Cell> ActiveCellChanged;

        /// <summary>
        /// Změna hodnoty <see cref="Row.IsChecked"/>
        /// </summary>
        /// <param name="row"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallCheckedRowChanged(Row row, bool oldValue, bool newValue, EventSourceType eventSource, bool callEvents)
        {
            GObjectPropertyChangeArgs<Row, bool> args = new GObjectPropertyChangeArgs<Row, bool>(row, oldValue, newValue, eventSource);
            this.OnCheckedRowChanged(args);
            if (callEvents && this.CheckedRowChanged != null)
                this.CheckedRowChanged(this, args);
        }
        /// <summary>
        /// Háček Změna aktivní buňky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCheckedRowChanged(GObjectPropertyChangeArgs<Row, bool> args) { }
        /// <summary>
        /// Event Změna aktivní buňky
        /// </summary>
        public event GObjectPropertyChangedHandler<Row, bool> CheckedRowChanged;

        /// <summary>
        /// Obsluha události MouseEnter na buňce tabulky
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallCellMouseEnter(Cell cell, GInteractiveChangeStateArgs e, bool callEvents)
        {
            GPropertyEventArgs<Cell> args = new GPropertyEventArgs<Cell>(cell, EventSourceType.InteractiveChanged, e);
            this.OnCellMouseEnter(args);
            if (callEvents && this.CellMouseEnter != null)
                this.CellMouseEnter(this, args);
        }
        /// <summary>
        /// Háček OnCellMouseEnter
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCellMouseEnter(GPropertyEventArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá při vstupu myši nad danou buňku, bez Drag, bez Click
        /// </summary>
        public event GPropertyEventHandler<Cell> CellMouseEnter;

        /// <summary>
        /// Obsluha události MouseLeave z buňky tabulky
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallCellMouseLeave(Cell cell, GInteractiveChangeStateArgs e, bool callEvents)
        {
            GPropertyEventArgs<Cell> args = new GPropertyEventArgs<Cell>(cell, EventSourceType.InteractiveChanged, e);
            this.OnCellMouseLeave(args);
            if (callEvents && this.CellMouseLeave != null)
                this.CellMouseLeave(this, args);
        }
        /// <summary>
        /// Háček OnCellMouseLeave
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCellMouseLeave(GPropertyEventArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá při opuštění myši z dané buňku, bez Drag, bez Click
        /// </summary>
        public event GPropertyEventHandler<Cell> CellMouseLeave;

        /// <summary>
        /// Obsluha události Click na buňku tabulky
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallActiveCellClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents)
        {
            GPropertyEventArgs<Cell> args = new GPropertyEventArgs<Cell>(cell, EventSourceType.InteractiveChanged, e);
            this.OnActiveCellClick(args);
            if (callEvents && this.ActiveCellClick != null)
                this.ActiveCellClick(this, args);
        }
        /// <summary>
        /// Háček OnActiveCellClick
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActiveCellClick(GPropertyEventArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá Click na dané buňce tabulky
        /// </summary>
        public event GPropertyEventHandler<Cell> ActiveCellClick;

        /// <summary>
        /// Obsluha události DoubleClick na buňku tabulky
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallActiveCellDoubleClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents)
        {
            GPropertyEventArgs<Cell> args = new GPropertyEventArgs<Cell>(cell, EventSourceType.InteractiveChanged, e);
            this.OnActiveCellDoubleClick(args);
            if (callEvents && this.ActiveCellDoubleClick != null)
                this.ActiveCellDoubleClick(this, args);
            this.DataCellDoubleClick(cell, args, callEvents);
        }
        /// <summary>
        /// Háček OnActiveCellDoubleClick
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActiveCellDoubleClick(GPropertyEventArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá DoubleClick na dané buňce tabulky
        /// </summary>
        public event GPropertyEventHandler<Cell> ActiveCellDoubleClick;

        /// <summary>
        /// Obsluha události LongClick na buňku tabulky
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallActiveCellLongClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents)
        {
            GPropertyEventArgs<Cell> args = new GPropertyEventArgs<Cell>(cell, EventSourceType.InteractiveChanged, e);
            this.OnActiveCellLongClick(args);
            if (callEvents && this.ActiveCellLongClick != null)
                this.ActiveCellLongClick(this, args);
        }
        /// <summary>
        /// Háček OnActiveCellLongClick
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActiveCellLongClick(GPropertyEventArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá LongClick na dané buňce tabulky
        /// </summary>
        public event GPropertyEventHandler<Cell> ActiveCellLongClick;

        /// <summary>
        /// Obsluha události RightClick na buňku tabulky
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void ITableInternal.CallActiveCellRightClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents)
        {
            GPropertyEventArgs<Cell> args = new GPropertyEventArgs<Cell>(cell, EventSourceType.InteractiveChanged, e);
            this.OnActiveCellRightClick(args);
            if (callEvents && this.ActiveCellRightClick != null)
                this.ActiveCellRightClick(this, args);
        }
        /// <summary>
        /// Háček OnActiveCellRightClick
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActiveCellRightClick(GPropertyEventArgs<Cell> args) { }
        /// <summary>
        /// Událost, která se vyvolá RightClick na dané buňce tabulky
        /// </summary>
        public event GPropertyEventHandler<Cell> ActiveCellRightClick;
        #endregion
        #region Eventy volané z tabulky na základě logických dat
        /// <summary>
        /// Metoda je volána po doubleclicku na buňku.
        /// Metoda má zajistit otevření záznamu nebo jinou akci (vyvolat odpovídající event <see cref="CallOpenRecordForm"/>),
        /// a to na základě detekce obsahu buňky, obsahu sloupce atd.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="args"></param>
        /// <param name="callEvents"></param>
        protected virtual void DataCellDoubleClick(Cell cell, GPropertyEventArgs<Cell> args, bool callEvents)
        {
            if (cell == null) return;
            GId recordId = null;

            // Otestujeme, jestli nejde o sloupec se vztahem => pokusíme se dohledat navázaný záznam z buňky:
            if (cell.IsRelation && args.HasInteractiveArgs && (args.InteractiveArgs.ModifierKeys == System.Windows.Forms.Keys.Control))
                recordId = cell.RelatedRecordGId;

            // Anebo zkusíme získat navázaný záznam z řádku:
            if (recordId == null)
                recordId = cell.Row.RecordGId;

            if (recordId != null)
                this.CallOpenRecordForm(recordId);
        }
        /// <summary>
        /// Metoda zajistí vyvolání akcí, které povedou k otevření formuláře záznamu specifikovaného v parametru.
        /// Vyvolá háček <see cref="OnOpenRecordForm(GPropertyEventArgs{GId})"/> a event <see cref="OpenRecordForm"/>.
        /// </summary>
        /// <param name="recordId"></param>
        protected void CallOpenRecordForm(GId recordId)
        {
            GPropertyEventArgs<GId> args = new GPropertyEventArgs<GId>(recordId, EventSourceType.InteractiveChanged);
            this.OnOpenRecordForm(args);
            if (this.OpenRecordForm != null)
                this.OpenRecordForm(this, args);
        }
        /// <summary>
        /// Háček volaný při otevírání záznamu z <see cref="Table"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnOpenRecordForm(GPropertyEventArgs<GId> args) { }
        /// <summary>
        /// Event volaný při otevírání záznamu z <see cref="Table"/>.
        /// </summary>
        public event EventHandler<GPropertyEventArgs<GId>> OpenRecordForm;
        #endregion
        #region Datové služby tabulky, aktivní řádek, aktivní buňka, označené řádky
        /// <summary>
        /// Vrátí true, pokud daný řádek v daném sloupci obsahuje hodnotu různou od null.
        /// Pokud daný řádek nebo sloupec je null nebo neexistuje, pak vrací false.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool HasValue(Row row, Column column)
        {
            if (row == null || column == null) return false;
            return row.HasValue(column);
        }
        /// <summary>
        /// Vrátí identifikátor záznamu v daném řádku.
        /// Jen tak mezi námi, pokud volající už má referenci na řádek <see cref="Row"/>, tak může rovnou číst <see cref="Row.RecordGId"/>.
        /// Zdejší metoda dělá to samé, jen před tím testuje dodaný řádek, zda není null.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public GId GetRecordGId(Row row)
        {
            if (row == null) return null;
            return row.RecordGId;
        }
        /// <summary>
        /// Vrátí identifikátor záznamu, který je navázán a zobrazen v dané buňce.
        /// Jen tak mezi námi, pokud volající už má referenci na buňku <see cref="Cell"/>, tak může rovnou číst <see cref="Cell.RelatedRecordGId"/>.
        /// Zdejší metoda dělá to samé, jen před tím testuje dodanou buňku, zda není null.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public GId GetRelatedRecordGId(Cell cell)
        {
            if (cell == null) return null;
            return cell.RelatedRecordGId;
        }
        /// <summary>
        /// Vrátí sloupec (Column), který obsahuje klíč (a další data) o vztaženém záznamu, který je zobrazován v daném sloupci.
        /// Daný sloupec (parametr column) může obsahovat <see cref="ColumnContentType.RelationRecordData"/> (tj. viditelné hodnoty ze vztaženého záznamu),
        /// anebo <see cref="ColumnContentType.RelationRecordId"/> (tj. číslo záznamu).
        /// Metoda vrátí sloupec, který má obsah <see cref="ColumnContentType.RelationRecordId"/>, a obsahuje číslo třídy v <see cref="Column.RecordClassNumber"/>.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public Column GetRelationKeyColumn(Column column)
        {
            if (column == null) return null;
            if (column.ColumnContent == ColumnContentType.RelationRecordId && column.RecordClassNumber.HasValue) return column;    // daný sloupec obsahuje číslo záznamu
            if (column.ColumnContent != ColumnContentType.RelationRecordData) return null;

            string relationColumnName = column.RelatedRecordColumnName;        // Název sloupce, který obsahuje číslo vztaženého záznamu
            if (String.IsNullOrEmpty(relationColumnName)) return null;
            Column relationColumn;
            if (!this.TryGetColumn(relationColumnName, out relationColumn) || relationColumn == null) return null;
            if (relationColumn.ColumnContent == ColumnContentType.RelationRecordId && relationColumn.RecordClassNumber.HasValue) return relationColumn;    // Vztahový sloupec obsahuje číslo záznamu
            return null;
        }
        /// <summary>
        /// Aktivní buňka tabulky - v režimu, kdy se tabulka aktivuje po buňkách, viz: <see cref="AllowSelectSingleCell"/>.
        /// Zatím nelze setovat.
        /// </summary>
        public Cell ActiveCell { get; private set; }
        /// <summary>
        /// Aktivní řádek tabulky.
        /// Zatím nelze setovat.
        /// </summary>
        public Row ActiveRow { get; private set; }
        /// <summary>
        /// Souhrn označených řádků tabulky.
        /// Zatím nelze setovat.
        /// </summary>
        public Row[] CheckedRows { get { return this.Rows.Where(r => r.IsChecked).ToArray(); } }
        #endregion
        #region Primární index = podle RecordGId řádků a podle RowId, jeho init, ReIndex, automatická údržba, hledání
        /// <summary>
        /// Provést regenerování indexů
        /// </summary>
        public void ReIndex()
        {
            this._PrimaryKeyInit();
            this.Rows.ForEach(row => this._PrimaryKeyAddRow(row));
        }
        /// <summary>
        /// Inicializace primárních indexů
        /// </summary>
        private void _PrimaryKeyInit()
        {
            this._PrimaryKeyId = new Dictionary<int, Row>();
            this._PrimaryKeyGId = new Dictionary<GId, Dictionary<int, Row>>();
        }
        /// <summary>
        /// Přidat řádek do primárních indexů
        /// </summary>
        /// <param name="row"></param>
        private void _PrimaryKeyAddRow(Row row)
        {
            if (row == null) return;
            this._PrimaryKeyId.AddRefresh(row.RowId, row);
            this._PrimaryKeyGId.AddRefresh(row.RecordGId, row.RowId, row);
        }
        /// <summary>
        /// Odebrat řádek z primárních indexů
        /// </summary>
        /// <param name="row"></param>
        private void _PrimaryKeyRemoveRow(Row row)
        {
            if (row == null) return;
            this._PrimaryKeyId.RemoveIfExists(row.RowId);
            this._PrimaryKeyGId.RemoveIfExists(row.RecordGId, row.RowId);
        }
        /// <summary>
        /// Primární klíč jednoduchý;
        /// Key = <see cref="Row.RowId"/>; Value = Row
        /// </summary>
        private Dictionary<int, Row> _PrimaryKeyId;
        /// <summary>
        /// Primární klíč složený;
        /// Key1 = <see cref="Row.RecordGId"/>; Key2 = <see cref="Row.RowId"/>; Value = Row
        /// </summary>
        private Dictionary<GId, Dictionary<int, Row>> _PrimaryKeyGId;
        /// <summary>
        /// Sloupec, podle něhož se vytváří primární index v situaci, kdy <see cref="Row.RecordGId"/> není zadán explicitně
        /// </summary>
        protected Column PrimaryKeyColumn { get { return this._PrimaryKeyColumn; } }
        /// <summary>ITableInternal member</summary>
        Column ITableInternal.PrimaryKeyColumn { get { return this._PrimaryKeyColumn; } }
        private Column _PrimaryKeyColumn;
        /// <summary>
        /// Metoda se pokusí najít řádek podle daného klíče, vrací true pokud je nalezen.
        /// Pokud dojde k chybě (nezadaný ID, neexistující PrimaryKey, více záznamů pro daný klíč), pak vrací false (=řádek nenalezen).
        /// Toto chování lze změnit parametrem checkErrors: false = default = chyby nehlásit, vrátit false; true = chyby hlásit.
        /// </summary>
        /// <param name="gId">Identifikátor řádku</param>
        /// <param name="row">Out nalezený řádek</param>
        /// <param name="checkErrors">false: Jakékoli chyby ignoruj, prostě vrať false (defaultní hodnota). true = chyby hlásit normálně jako chyby (GraphLibCodeException).</param>
        /// <returns></returns>
        public bool TryGetRow(GId gId, out Row row, bool checkErrors = false)
        {
            return this._TryGetRow(gId, out row, checkErrors);
        }
        /// <summary>
        /// Metoda se pokusí vrátit řádek pro daný GId. Pokud záznam neexistuje, vrací false a jako out Row dává null.
        /// Metoda vyhodí chybu, pokud daný klíč GId je null, nebo pokud pro GId existuje více řádků.
        /// </summary>
        /// <param name="gId">Identifikátor řádku</param>
        /// <param name="row">Out nalezený řádek</param>
        /// <param name="checkErrors">false: Jakékoli chyby ignoruj, prostě vrať false. true = chyby hlásit normálně jako chyby (GraphLibCodeException).</param>
        /// <returns></returns>
        private bool _TryGetRow(GId gId, out Row row, bool checkErrors)
        {
            row = null;
            if (gId == null)
            {
                if (!checkErrors) return false;
                throw new GraphLibCodeException("Nelze vyhledávat v tabulce podle GId, pokud je null.");
            }

            Row[] rows = this._GetRows(gId);
            if (rows == null || rows.Length == 0) return false;
            if (rows.Length > 1)
            {
                if (!checkErrors) return false;
                throw new GraphLibCodeException("Tabulka <" + this.TableName + "> obsahuje pro primární klíč <" + gId + "> víc než jeden záznam. Nelze v ní použít metodu TryGetRowOnPrimaryKey(), ale TryGetRowsOnPrimaryKey().");
            }
            row = rows[0];
            return true;
        }
        /// <summary>
        /// Metoda se pokusí vrátit všechny řádky pro daný GId. Pokud záznam neexistuje, vrací false a jako out Rows dává null.
        /// Metoda vyhodí chybu, pokud GId je null.
        /// </summary>
        /// <param name="gId"></param>
        /// <param name="rows"></param>
        /// <param name="checkErrors">false: Jakékoli chyby ignoruj, prostě vrať false. true = chyby hlásit normálně jako chyby (GraphLibCodeException).</param>
        /// <returns></returns>
        public bool TryGetRows(GId gId, out Row[] rows, bool checkErrors = false)
        {
            rows = null;
            if (gId == null)
            {
                if (!checkErrors) return false;
                throw new GraphLibCodeException("Nelze vyhledávat v tabulce podle GId, pokud je null.");
            }

            rows = this._GetRows(gId);
            if (rows == null || rows.Length == 0) return false;
            return true;
        }
        /// <summary>
        /// Metoda vrátí pole řádků odpovídající danému klíči.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        private Row[] _GetRows(GId gId)
        {
            Dictionary<int, Row> dictionary;
            if (!this._PrimaryKeyGId.TryGetValue(gId, out dictionary)) return null;
            if (dictionary == null || dictionary.Count == 0) return null;
            return dictionary.Values.ToArray();
        }
        #endregion
        #region Statické služby
        /// <summary>
        /// Vrací typ obsahu pro danou hodnotu.
        /// </summary>
        public static TableValueType GetValueType(object value)
        {
            if (value == null) return TableValueType.Null;
            if (value is IDrawItem) return TableValueType.IDrawItem;
            if (value is Components.Graph.ITimeInteractiveGraph) return TableValueType.ITimeInteractiveGraph;
            if (value is Components.Graph.ITimeGraph) return TableValueType.ITimeGraph;
            if (value is Image) return TableValueType.Image;
            if (value is GuiIdText) return TableValueType.TextRelation;
            return TableValueType.Text;
        }
        #endregion
        #region Full vizualizace
        /// <summary>
        /// CSV formát obsahující prvních 16384 řádků tabulky (anebo tolik, kolik se stihne vygenerovat za 100 milisekund)
        /// </summary>
        public string TextCsv
        {
            get
            {
                System.Text.StringBuilder sb = new StringBuilder();
                string tab = "\t";

                bool addNames = false;
                sb.Append(tab);
                foreach (Column column in this.Columns)
                {
                    sb.Append(column.Title + tab);
                    if (!addNames && (!String.Equals(column.Title, column.ColumnName)))
                        addNames = true;
                }
                sb.AppendLine();

                if (addNames)
                {
                    sb.Append(tab);
                    foreach (Column column in this.Columns)
                        sb.Append(column.ColumnName + tab);
                    sb.AppendLine();
                }

                DateTime end = DateTime.Now.AddMilliseconds(100);
                int maxRows = 16384;
                int rowCount = this.RowsCount;
                bool rowShort = (rowCount > maxRows);
                if (rowShort) rowCount = maxRows;
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    Row row = this.Rows[rowIndex];
                    sb.Append(rowIndex.ToString() + tab);
                    foreach (Column column in this.Columns)
                        sb.Append(row[column].Value + tab);
                    sb.AppendLine();
                    if (DateTime.Now > end)
                    {
                        rowShort = true;
                        break;
                    }
                }
                if (rowShort)
                    sb.AppendLine("...");

                return sb.ToString();
            }
        }
        #endregion
    }
    #endregion
    #region Column
    /// <summary>
    /// Column : informace o jednom sloupci tabulky
    /// </summary>
    public partial class Column : ITableMember, /* ISequenceLayout, */ IVisualMember, IIdKey
    {
        #region Konstruktor, základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Column()
        {
            this._ColumnId = -1;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        public Column(string name)
            : this()
        {
            this._ColumnName = name;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="title"></param>
        /// <param name="toolTip"></param>
        /// <param name="formatString"></param>
        /// <param name="width"></param>
        /// <param name="columnContent"></param>
        /// <param name="autoWidth"></param>
        /// <param name="sortingEnabled"></param>
        /// <param name="widthMininum"></param>
        /// <param name="widthMaximum"></param>
        /// <param name="isVisible"></param>
        /// <param name="allowColumnResize"></param>
        /// <param name="recordClassNumber"></param>
        public Column(string name, Localizable.TextLoc title = null, Localizable.TextLoc toolTip = null, string formatString = null, int? width = null,
            ColumnContentType columnContent = ColumnContentType.UserData, bool autoWidth = false, bool sortingEnabled = true, int? widthMininum = null, int? widthMaximum = null,
            bool isVisible = true, bool allowColumnResize = true, int? recordClassNumber = null)
            : this()
        {
            this._ColumnName = name;
            // Data.ColumnProperties columnProperties = this.ColumnProperties;
            this.Title = title;
            this.ToolTip = toolTip;
            this.FormatString = formatString;
            this.ColumnContent = columnContent;
            this.AllowColumnSortByClick = sortingEnabled;
            this.AllowColumnResize = allowColumnResize;
            this.Width = width;
            this.IsVisible = isVisible;
            this.AutoWidth = autoWidth;
            this.WidthMininum = widthMininum;
            this.WidthMaximum = widthMaximum;
            this.RecordClassNumber = recordClassNumber;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Column [" + this.ColumnId.ToString() + "]: "
                + this.ColumnName + ": " + this.Title
                + " in " + (this.HasTable ? this.Table.ToString() : "NULL");
        }
        #endregion
        #region Vlastnosti sloupce
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
        public string ColumnName { get { return this._ColumnName; } set { this._ColumnName = value; } } private string _ColumnName;
        /// <summary>
        /// Titulkový text, lokalizovaný
        /// </summary>
        public string Title { get { return this._Title; } set { this._Title = value; } } private string _Title;
        /// <summary>
        /// Text pro ToolTip pro hlavičku tohoto sloupce, lokalizovaný
        /// </summary>
        public string ToolTip { get { return this._ToolTip; } set { this._ToolTip = value; } } private string _ToolTip;
        /// <summary>
        /// Pořadí tohoto sloupce při zobrazování.
        /// Výchozí je -1, takový sloupec je při přidání do tabulky zařazen na její konec.
        /// Pokud je vytvořen sloupec s ColumnOrder nula a kladným, pak při přidání do tabulky se jeho ColumnOrder nezmění.
        /// Jednotlivé sloupce nemusí mít hodnoty ColumnOrder v nepřerušovaném pořadí.
        /// Po napojení sloupce do tabulky je do ColumnOrder vepsána hodnota = ColumnID, takže nový sloupec se zařadí vždy na konec.
        /// </summary>
        public int ColumnOrder { get { return this._ColumnOrder; } set { this._ColumnOrder = value; } } private int _ColumnOrder = -1;
        /// <summary>
        /// Jaká data obsahuje tento sloupec
        /// </summary>
        public ColumnContentType ColumnContent { get { return this._ColumnContent; } set { this._ColumnContent = value; } } private ColumnContentType _ColumnContent;
        /// <summary>
        /// Defaultní hodnota pro nové řádky
        /// </summary>
        public object DefaultValue { get { return this._DefaultValue; } set { this._DefaultValue = value; } } private object _DefaultValue;
        /// <summary>
        /// Formátovací string pro data zobrazovaná v tomto sloupci.
        /// </summary>
        public string FormatString { get { return this._FormatString; } set { this._FormatString = value; } } private string _FormatString;
        /// <summary>
        /// Zarovnání obsahu
        /// </summary>
        public ContentAlignment? Alignment { get { return this._Alignment; } set { this._Alignment = value; } } private ContentAlignment? _Alignment;
        /// <summary>
        /// Pouze pro čtení
        /// </summary>
        public bool ReadOnly { get { return this._ReadOnly; } set { this._ReadOnly = value; } } private bool _ReadOnly;
        /// <summary>
        /// true pokud je povoleno třídit řádky kliknutím na záhlaví tohoto sloupce. Default = true;
        /// </summary>
        public bool AllowColumnSortByClick { get { return this._AllowColumnSortByClick; } set { this._AllowColumnSortByClick = value; } } private bool _AllowColumnSortByClick = true;
        /// <summary>
        /// true pokud je měnit šířku tohoto sloupce pomocí myši. Default = true;
        /// </summary>
        public bool AllowColumnResize { get { return this._AllowColumnResize; } set { this._AllowColumnResize = value; } } private bool _AllowColumnResize = true;
        /// <summary>
        /// Obsahuje true, pokud se pro sloupec má zobrazit časová osa v záhlaví.
        /// To je jen tehdy, když sloupec obsahuje časový graf (<see cref="ColumnContent"/> == <see cref="ColumnContentType.TimeGraphSynchronized"/> nebo <see cref="ColumnContentType.TimeGraphStandalone"/>).
        /// </summary>
        public bool UseTimeAxis { get { return (this.ColumnContent == ColumnContentType.TimeGraphSynchronized || this.ColumnContent == ColumnContentType.TimeGraphStandalone); } }
        /// <summary>
        /// Obsahuje true, pokud se pro sloupec má zobrazit časová osa v záhlaví, a tato časová osa se má synchronizovat do dalších Gridů a objektů.
        /// To je jen tehdy, když sloupec obsahuje časový graf (<see cref="ColumnContent"/> == <see cref="ColumnContentType.TimeGraphSynchronized"/>).
        /// </summary>
        public bool UseTimeAxisSynchronized { get { return (this.ColumnContent == ColumnContentType.TimeGraphSynchronized); } }
        /// <summary>
        /// Zadaná šířka sloupce.
        /// Hodnotu může vložit aplikační kód, hodnota se projeví v GUI.
        /// Uživatel může interaktivně měnit velikost objektu, změna se projeví v této hodnotě.
        /// Veškerá další nastavení jsou v property WidthLayout.
        /// </summary>
        public Int32? Width { get { return this.ColumnSize.Size; } set { this.ColumnSize.Size = value; } }
        /// <summary>
        /// Nejmenší povolená šířka
        /// </summary>
        public Int32? WidthMininum { get { return this.ColumnSize.SizeMinimum; } set { this.ColumnSize.SizeMinimum = value; } }
        /// <summary>
        /// Největší povolená šířka
        /// </summary>
        public Int32? WidthMaximum { get { return this.ColumnSize.SizeMaximum; } set { this.ColumnSize.SizeMaximum = value; } }
        /// <summary>
        /// true pokud tento prvek má být použit jako "guma" při změně šířky tabulky tak, aby kolekce sloupců vyplnila celý prostor.
        /// Na true se nastavuje typicky u "hlavního" sloupce grafové tabulky.
        /// Je vhodné přitom nastavit minimální šířku sloupce (WidthLayout.SizeMinimum) tak, aby při zmenšení prostoru z daného sloupce něco zbylo.
        /// </summary>
        public bool AutoWidth { get { return this.ColumnSize.AutoSize.Value; } set { this.ColumnSize.AutoSize = value; } }
        /// <summary>
        /// true pro viditelný sloupec (default), false for skrytý
        /// </summary>
        public bool IsVisible { get { return (this.CanBeVisible && this.ColumnSize.Visible); } set { this.ColumnSize.Visible = (value && this.CanBeVisible); } }
        /// <summary>
        /// true, pokud this sloupec smí být někdy zobrazen uživateli.
        /// To mohou být pouze sloupce, jejichž obsah <see cref="ColumnContent"/> 
        /// je <see cref="ColumnContentType.UserData"/> nebo <see cref="ColumnContentType.RelationRecordData"/> nebo <see cref="ColumnContentType.TimeGraphSynchronized"/> nebo <see cref="ColumnContentType.TimeGraphStandalone"/>.
        /// </summary>
        public bool CanBeVisible { get { ColumnContentType cc = this.ColumnContent; return (cc == ColumnContentType.UserData || cc == ColumnContentType.RelationRecordData || cc == ColumnContentType.TimeGraphSynchronized || cc == ColumnContentType.TimeGraphStandalone); } }
        /// <summary>
        /// Komparátor pro dvě hodnoty v tomto sloupci, pro třídění podle tohoto sloupce
        /// </summary>
        public Func<object, object, int> ValueComparator { get { return this._ValueComparator; } set { this._ValueComparator = value; } } private Func<object, object, int> _ValueComparator;
        /// <summary>
        /// Číslo třídy tohoto záznamu.
        /// U sloupce [0] jde o číslo třídy záznamů v tabulce, 
        /// u jiných sloupců jde o číslo třídy záznamů ve vztahu, které jsou zobrazeny v některém ze sloupců.
        /// </summary>
        public int? RecordClassNumber { get { return this._RecordClassNumber; } private set { this._RecordClassNumber = value; } } private int? _RecordClassNumber;
        /// <summary>
        /// Číslo šablony, pokud jsou data načítáná z Greenu. Green vkládá číslo šablony do prvního sloupce, zdejší Table si číslo přenese do <see cref="Table.TemplateId"/>.
        /// </summary>
        public int? TemplateId { get { return this._TemplateId; } private set { this._TemplateId = value; } } private int? _TemplateId;
        /// <summary>
        /// true pokud tento sloupec zobrazuje vztažený záznam, a lze jej tedy rozkliknout (pomocí Ctrl + DoubleClick).
        /// Vztahový sloupec má typ obsahu <see cref="ColumnContent"/> buď <see cref="ColumnContentType.RelationRecordId"/> nebo <see cref="ColumnContentType.RelationRecordData"/>.
        /// </summary>
        public bool IsRelation { get { ColumnContentType cc = this.ColumnContent; return (cc == ColumnContentType.RecordGId || cc == ColumnContentType.MasterId || cc == ColumnContentType.RelationRecordId || cc == ColumnContentType.RelationRecordData); } }
        /// <summary>
        /// Název sloupce (ColumnName), v němž je uloženo číslo vztaženého záznamu, pokud this sloupec je vztahový (<see cref="IsRelation"/> je true).
        /// Při otevírání vztaženého záznamu (pomocí Ctrl + DoubleClick) je nalezen tento sloupec, přečteno jeho číslo a získaný záznam je otevřen.
        /// </summary>
        public string RelatedRecordColumnName { get { return this._RelatedRecordColumnName; } set { this._RelatedRecordColumnName = value; } } private string _RelatedRecordColumnName;
        /// <summary>
        /// Obsahuje true, pokud tento sloupec může být použit jako PrimaryKey.
        /// Může to být sloupec, jehož <see cref="ColumnContent"/> je <see cref="ColumnContentType.RecordGId"/>,
        /// anebo sloupec typu <see cref="ColumnContentType.MasterId"/> nebo <see cref="ColumnContentType.EntryId"/>, 
        /// pokud jeho číslo třídy <see cref="RecordClassNumber"/> má hodnotu.
        /// To jest: jeho datový typ je Int32, a obsah je RecordId. 
        /// </summary>
        internal bool AllowPrimaryKey
        {
            get
            {
                if (this.ColumnContent == ColumnContentType.RecordGId) return true;
                if (this.RecordClassNumber.HasValue && (this.ColumnContent == ColumnContentType.MasterId || this.ColumnContent == ColumnContentType.EntryId)) return true;
                return false;
            }
        }
        #endregion
        #region Linkování na tabulku
        /// <summary>
        /// Reference na tabulku, kam sloupec patří.
        /// </summary>
        public Table Table { get { return this._Table; } } private Table _Table;
        /// <summary>
        /// true pokud máme referenci na datovou tabulku
        /// </summary>
        public bool HasTable { get { return (this._Table != null); } }
        /// <summary>
        /// true pokud má referenci na vizuální tabulku (GTable)
        /// </summary>
        public bool HasGTable { get { return (this._Table != null && this._Table.HasGTable); } }
        /// <summary>
        /// Reference na vizuální tabulku (GTable), může být null
        /// </summary>
        public GTable GTable { get { return (this.HasGTable ? this._Table.GTable : null); } }
        /// <summary>
        /// ITableMember.Table : Reference na tabulku, která je vlastníkem this objektu
        /// </summary>
        Table ITableMember.Table { get { return this._Table; } set { this._Table = value; this.ColumnSize.Parent = this._Table?.ColumnSize; } }
        /// <summary>
        /// ITableMember.Id : Přidělené ID
        /// </summary>
        int ITableMember.Id { get { return this._ColumnId; } set { this._ColumnId = value; this._SetColumnOrder(); } }
        /// <summary>
        /// Nastaví this._ColumnOrder na hodnotu odpovídající this._ColumnId.
        /// </summary>
        private void _SetColumnOrder()
        {
            int id = this._ColumnId;
            if (id < 0)
            {   // this sloupec byl z tabulky odebrán:
                this.ColumnOrder = id;
            }
            else
            {   // this sloupec byl do tabulky přidán:
                if (this.ColumnOrder < 0)
                    this.ColumnOrder = id;
                Table table = this._Table;
                if (this.IsSortingColumn && table != null && table.Columns.Any(c => c.ColumnId != id && c.IsSortingColumn))
                    // Pokud do tabulky přidávám další (tj. this) sloupec, který už má v sobě nastavené třídění, 
                    //  a přitom v tabulce existuje nějaký jiný sloupec, který již je třídícím sloupcem, pak pro this sloupec třídění zruším:
                    this._SortCurrent = ItemSortType.None;
            }
        }
        #endregion
        #region GUI vlastnosti sloupce
        /// <summary>
        /// Defaultní parametry pro grafy v tomto sloupci.
        /// Tato property může být null.
        /// </summary>
        public Components.Graph.TimeGraphProperties GraphParameters
        {
            get { return this._GraphParameters; }
            set { this._GraphParameters = value; }
        }
        private Components.Graph.TimeGraphProperties _GraphParameters;
        /// <summary>
        /// Záhlaví tohoto sloupce, grafický prvek, auitoinicializační
        /// </summary>
        public GColumnHeader ColumnHeader
        {
            get
            {
                if (this._ColumnHeader == null)
                    this._ColumnHeader = new GColumnHeader(this);
                return this._ColumnHeader;
            }
            set { this._ColumnHeader = value; }
        }
        private GColumnHeader _ColumnHeader;
        /// <summary>
        /// Vizuální pořadí tohoto sloupce, 0 má první vizuálně dostupný sloupec (tzn. po přemístění jiného sloupce na první pozici bude ten nový mít Order = 0)
        /// </summary>
        public int VisualOrder { get { return this.ColumnSize.Order; } }
        #endregion
        #region Třídění podle sloupce
        /// <summary>
        /// Režim třídění v tomto sloupci.
        /// Změna hodnoty vyvolá invalidaci tabulky typu RowOrder.
        /// </summary>
        public ItemSortType SortCurrent
        {
            get { return this._SortCurrent; }
            set
            {
                ItemSortType oldValue = this._SortCurrent;
                if (value != oldValue)
                {
                    if (this.HasTable)
                    {   // Pokud this sloupec je součástí datové tabulky (on nemusí být), 
                        //  a pokud aktuální třídění je jiné než None,
                        //  pak zajistím, že pouze this sloupec bude mít nastavené třídění, a ostatní budou mít None:
                        if (value != ItemSortType.None)
                            this.Table.Columns.ForEachItem(c => c._SortCurrent = ItemSortType.None);
                        this._SortCurrent = value;
                        // Pokud this sloupec je součástí vizuální tabulky (on nemusí být), 
                        //  pak provedu invalidaci RowOrder:
                        if (this.HasGTable)
                            this.GTable.Invalidate(InvalidateItem.RowOrder);
                    }
                    else
                    {   // Zcela samostatně existující sloupec: nastaví si dané třídění a víc neřeší:
                        this._SortCurrent = value;
                    }
                }
            }
        } private ItemSortType _SortCurrent = ItemSortType.None;
        /// <summary>
        /// true pokud this sloupec je třídící (tzn. má SortCurrent : Ascending nebo Descending)
        /// </summary>
        protected bool IsSortingColumn { get { return (this._SortCurrent == ItemSortType.Ascending || this._SortCurrent == ItemSortType.Descending); } }
        /// <summary>
        /// Změní třídění tohoto sloupce, volá se po kliknutí na jeho záhlaví.
        /// Pokud je třídění povoleno, změní SortCurrent v pořadí None - Asc - Desc - None; a vrátí true.
        /// Pokud třídění není povoleno, vrátí false.
        /// Změna třídění se vepíše do this.SortCurrent, což vyvolá invalidaci tabulky typu RowOrder.
        /// </summary>
        /// <returns></returns>
        public bool SortChange()
        {
            if (!this.AllowColumnSortByClick) return false;
            switch (this.SortCurrent)
            {
                case ItemSortType.None:
                    this.SortCurrent = ItemSortType.Ascending;
                    break;
                case ItemSortType.Ascending:
                    this.SortCurrent = ItemSortType.Descending;
                    break;
                case ItemSortType.Descending:
                    this.SortCurrent = ItemSortType.None;
                    break;
            }
            return true;
        }
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
        #endregion
        #region Implementace interface ISequenceLayout (Layout šířky sloupce), IVisualMember (vizuální vlastnosti), IIdKey (dvojitý klíč)
        /// <summary>
        /// Koordinátor šířky sloupce.
        /// Hodnota může být čtena i jako Int32 (implicitní konverze) = přímo výška řádku.
        /// Property je autoinicializační.
        /// Po vložení this sloupce do tabulky je vhodné vložit referenci na <see cref="Table.ColumnSize"/> 
        /// do <see cref="ItemSize{T}.Parent"/>, aby fungovalo přebírání Parent default hodnot.
        /// </summary>
        internal ItemSizeInt ColumnSize { get { if (this._ColumnSize == null) this._ColumnSize = new ItemSizeInt(this.Table?.ColumnSize); return this._ColumnSize; } }
        private ItemSizeInt _ColumnSize;
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
        int IIdKey.Id { get { return this.ColumnId; } }
        string IIdKey.Key { get { return this.ColumnName; } }
        #endregion
    }
    #endregion
    #region Row
    /// <summary>
    /// Row : informace o jednom řádku tabulky
    /// </summary>
    public partial class Row : ITableMember, ITagItemOwner, IVisualMember, IVisualParent, IContentValidity, IComparableItem
    {
        #region Konstruktor, základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Row()
        {
            this._RowId = -1;
            this._CellDict = new Dictionary<int, Cell>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="values"></param>
        public Row(params object[] values)
            : this()
        {
            this._CellInit(values);
        }
        /// <summary>
        /// Konstruktor, který do nové instance překopíruje data ze zdrojového řádku
        /// </summary>
        /// <param name="original">Zdrojový řádek</param>
        /// <param name="cloneArgs">Data pro klonování</param>
        public Row(Row original, TableRowCloneArgs cloneArgs = null)
             : this()
        {
            if (original != null)
                CopyData(original, this, cloneArgs);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "Row [" + this.RowId.ToString() + "] "
                        + " in " + (this.HasTable ? this.Table.Text : "NULL")
                        + "; Content: " + this.Text;
            return text;
        }
        /// <summary>
        /// Jednoduché textové vyjádření obsahu this instance
        /// </summary>
        public string Text
        {
            get
            {
                string text = "|";
                foreach (Cell cell in this.Cells)
                    text += " " + cell.Text + " |";
                return text;
            }
        }
        /// <summary>
        /// Jednoznačné ID tohoto řádku. Read only.
        /// Je přiděleno při přidání do tabulky, pak má hodnotu 0 nebo kladnou.
        /// Hodnota se nemění ani přemístěním na jinou pozici, ani odebráním některého řádku s menším ID.
        /// Po odebrání z tabulky je hodnota -1.
        /// </summary>
        public int RowId { get { return this._RowId; } } private int _RowId = -1;
        #endregion
        #region Klonování řádku
        /// <summary>
        /// Přenese obsah z řádku Source to Target.
        /// </summary>
        /// <param name="source">Zdrojový řádek</param>
        /// <param name="target">Cílový řádek</param>
        /// <param name="cloneArgs">Data pro klonování</param>
        public static void CopyData(Row source, Row target, TableRowCloneArgs cloneArgs = null)
        {
            target._CellDict.Clear();
            foreach (var kvp in source._CellDict)
            {
                int columnId = kvp.Key;
                object value = CloneValue(kvp.Value.Value, cloneArgs);
                target._GetCell(columnId).Value = value;
            }

            target.BackgroundValue = CloneValue(source.BackgroundValue, cloneArgs);
            target.RecordGId = source.RecordGId;
            target.ParentRecordGId = source.ParentRecordGId;
            target.SelectedRowImage = source.SelectedRowImage;
            target.BackColor = source.BackColor;
            target.UserData = source.UserData;

            target._TagItemDict = null;
            if (cloneArgs != null && cloneArgs.CloneRowTagItems && source._TagItemDict != null)
                target._TagItemDict = source._TagItemDict.GetDictionary(kvp => kvp.Key, kvp => new TagItem(kvp.Value), true);
        }
        /// <summary>
        /// Metoda vrací klon z dodané hodnoty
        /// </summary>
        /// <param name="source">Zdrojový objekt</param>
        /// <param name="cloneArgs">Data pro klonování</param>
        /// <returns></returns>
        private static object CloneValue(object source, TableRowCloneArgs cloneArgs = null)
        {
            if (source == null) return null;
            if (source is Components.Graph.ITimeInteractiveGraph) return (source as Components.Graph.ITimeInteractiveGraph).GetGraphClone(cloneArgs);
            if (source is ICloneable) return (source as ICloneable).Clone();
            return source;
        }
        #endregion
        #region Linkování na tabulku a na sloupce
        /// <summary>
        /// Reference na tabulku, kam řádek patří.
        /// </summary>
        public Table Table { get { return this._Table; } } private Table _Table;
        /// <summary>
        /// Reference na tabulku, kam řádek patří, typovaná na interní interface <see cref="ITableInternal"/>.
        /// </summary>
        protected ITableInternal ITable { get { return this._Table as ITableInternal; } }
        /// <summary>
        /// Kolekce sloupců z tabulky, může být null pokud řádek není napojen do tabulky
        /// </summary>
        public EList<Column> Columns { get { return (this.HasTable ? this.Table.Columns : null); } }
        /// <summary>
        /// true pokud máme referenci na tabulku
        /// </summary>
        public bool HasTable { get { return (this._Table != null); } }
        /// <summary>
        /// ITableMember.Table : Reference na tabulku, která je vlastníkem this objektu
        /// </summary>
        Table ITableMember.Table { get { return this._Table; } set { this._Table = value; this.RowSize.Parent = value?.RowSize; } }
        /// <summary>
        /// true pokud tento řádek má svou tabulku, a tabulka má svoji grafickou vrstvu
        /// </summary>
        public bool HasGTable { get { return (this._Table != null && this._Table.HasGTable); ; } }
        /// <summary>
        /// Reference na grafickou vrstvu tabulky
        /// </summary>
        public GTable GTable { get { return (this.HasGTable ? this._Table.GTable : null); } }
        /// <summary>
        /// ITableMember.Id : Přidělené ID
        /// </summary>
        int ITableMember.Id { get { return this._RowId; } set { this._RowId = value; } }
        #endregion
        #region Cells = jednotlivé buňky v řádku
        /// <summary>
        /// Obsahuje (vrátí) instanci Cell, pro dané ID sloupce. 
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
        /// Obsahuje (vrátí) instanci Cell, pro dané columnName sloupce. 
        /// Trvá déle než hledání podle columnId.
        /// Může vrátit null, pokud neexistuje buňka se vztahem na sloupec daného jména.
        /// </summary>
        /// <param name="columnName">Název sloupce</param>
        /// <returns></returns>
        public Cell this[string columnName]
        {
            get { return this._GetCell(columnName); }
        }
        /// <summary>
        /// Obsahuje (vrátí) instanci Cell, pro daný object column. 
        /// Trvá déle než hledání podle columnId.
        /// Může vrátit null, pokud neexistuje buňka se vztahem na daný sloupec.
        /// </summary>
        /// <param name="column">Sloupec</param>
        /// <returns></returns>
        public Cell this[Column column]
        {
            get { return this._GetCell(column); }
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
        /// Lze tak v tabulce mít i "skryté" sloupce.
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
        /// Vrátí buňku pro dané columnName.
        /// Trvá déle než hledání podle Id.
        /// Může vrátit null, pokud neexistuje buňka se vztahem na sloupec daného jména.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private Cell _GetCell(string columnName)
        {
            return this._CellDict.Values.FirstOrDefault(c => c.Column != null && String.Equals(c.Column.ColumnName, columnName, StringComparison.InvariantCultureIgnoreCase));
        }
        /// <summary>
        /// Vrátí buňku pro daný column.
        /// Může vrátit null, pokud neexistuje buňka se vztahem na daný sloupec, a daný sloupec a this řádek nepatří do společné tabulky.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private Cell _GetCell(Column column)
        {
            if (this.HasTable && column.HasTable && Object.ReferenceEquals(this.Table, column.Table))
                return this._GetCell(column.ColumnId);
            return null;
        }
        /// <summary>
        /// Soupis buněk
        /// </summary>
        private Dictionary<int, Cell> _CellDict;
        #endregion
        #region TagItems = štítky náležející k řádku
        /// <summary>
        /// TagItems = štítky náležející k řádku.
        /// Štítky - pokud budou u některých řádků v tabulce zadány, budou sumarizovány ze všech řádků tabulky, a budou vypsány pod záhlavím tabulky.
        /// Ty pak slouží jako rychlý filtr řádků.
        /// </summary>
        public IEnumerable<TagItem> TagItems { get { return (this._TagItemDict != null ? this._TagItemDict.Values : null); } set { this._SetTagItems(value); } }
        /// <summary>
        /// Prvek přidá svoje Tagy do společné Dictionary
        /// </summary>
        /// <param name="tagDict"></param>
        void ITagItemOwner.PrepareSummaryDict(Dictionary<string, TagItem> tagDict)
        {
            if (this._TagItemDict == null) return;
            _AddTagItemsToDict(this._TagItemDict.Values, tagDict);
        }
        /// <summary>
        /// Prvek vrátí true, pokud jeho Tagy vyhovují zadaným (uživatelem zvoleným) Tagům.
        /// </summary>
        /// <param name="tagFilter"></param>
        bool ITagItemOwner.FilterByTagValues(TagItem[] tagFilter)
        {
            if (tagFilter == null || tagFilter.Length == 0) return true;
            if (this._TagItemDict == null) return false;
            return tagFilter.Any(tag => this._TagItemDict.ContainsKey(tag.Text));
        }
        /// <summary>
        /// Z dodaných položek vygeneruje data do <see cref="_TagItemDict"/>
        /// </summary>
        /// <param name="tagItems"></param>
        private void _SetTagItems(IEnumerable<TagItem> tagItems)
        {
            Dictionary<string, TagItem> tagDict = new Dictionary<string, TagItem>();
            _AddTagItemsToDict(tagItems, tagDict);
            this._TagItemDict = tagDict;
            if (this.HasTable)
                ((ITableInternal)this.Table).InvalidateTagItems();
        }
        /// <summary>
        /// Projde prvky z kolekce tagItems, a přidá je do Dictionary tagDict (přidá klíč a navýší hodnotu za každý jeden přidaný klíč).
        /// </summary>
        /// <param name="tagItems"></param>
        /// <param name="tagDict"></param>
        private static void _AddTagItemsToDict(IEnumerable<TagItem> tagItems, Dictionary<string, TagItem> tagDict)
        {
            if (tagItems == null || tagDict == null) return;
            foreach (TagItem tagItem in tagItems)
            {
                string key = tagItem.Text;
                if (String.IsNullOrEmpty(key)) continue;
                TagItem value;
                if (!tagDict.TryGetValue(key, out value))
                    tagDict.Add(key, tagItem);
            }
        }
        /// <summary>
        /// Úložiště Tagů tohoto řádku
        /// </summary>
        private Dictionary<string, TagItem> _TagItemDict;
        #endregion
        #region BackgroundValue
        /// <summary>
        /// Objekt, který bude vykreslen na pozadí celého řádku.
        /// Řešené datové typy jsou: IDrawItem, ITimeInteractiveGraph, ITimeGraph, Image.
        /// Vykreslování objektu pozadí provádí instance <see cref="Control"/> třídy <see cref="GRow"/>.
        /// </summary>
        public object BackgroundValue { get; set; }
        /// <summary>
        /// Typ objektu <see cref="BackgroundValue"/>, určuje režim kreslení jejího obsahu.
        /// Určuje se dynamicky podle obsahu <see cref="BackgroundValue"/>, tady pokud je zde určeno "Image", pak <see cref="BackgroundValue"/> určitě není Null.
        /// Vykreslování objektu pozadí provádí instance <see cref="Control"/> třídy <see cref="GRow"/>.
        /// </summary>
        public TableValueType BackgroundValueType { get { return Data.Table.GetValueType(this.BackgroundValue); } }
        /// <summary>
        /// Obsahuje true, pokud tento řádek používá časovou osu k vykreslení svého pozadí (=má na pozadí graf související s časovou osou)
        /// </summary>
        public bool UseBackgroundTimeAxis { get { TableValueType bgt = this.BackgroundValueType; return (bgt == TableValueType.ITimeInteractiveGraph || bgt == TableValueType.ITimeGraph); } }
        #endregion
        #region GUI vlastnosti
        /// <summary>
        /// true pokud this řádek je označen k další práci. Default = false.
        /// Setování nové hodnoty vyvolá událost v tabulce.
        /// </summary>
        public bool IsChecked
        {
            get { return this._IsChecked; }
            set
            {
                bool oldValue = this._IsChecked;
                bool newValue = value;
                if (oldValue == newValue) return;
                this._IsChecked = value;
                this.ITable.CallCheckedRowChanged(this, oldValue, newValue, EventSourceType.ApplicationCode, true);
            }
        }
        private bool _IsChecked = false;
        /// <summary>
        /// true pokud this řádek je aktivní (=vybraný kurzorem)
        /// </summary>
        public bool IsActive { get { return (this.HasGTable ? this.GTable.IsRowActive(this) : false); } }
        /// <summary>
        /// true pokud this řádek je nyní pod myší (=myš se pohybuje nad ním)
        /// </summary>
        public bool IsMouseHot { get { return (this.HasGTable ? this.GTable.IsRowHot(this) : false); } }
        /// <summary>
        /// Změní hodnotu <see cref="IsChecked"/> v tomto řádku
        /// </summary>
        public void CheckedChange()
        {
            this.IsChecked = !this.IsChecked;
        }
        /// <summary>
        /// Image použitý pro zobrazení Selected řádku v prostoru RowHeader. Default = null, v tom případě se použije image this.Table.SelectedRowImage.
        /// </summary>
        public Image SelectedRowImage { get { return this._SelectedRowImage ?? this.Table.SelectedRowImage; } set { this._SelectedRowImage = value; } } private Image _SelectedRowImage = null;
        /// <summary>
        /// Grafická instance reprezentující prostor řádku, grafický prvek, auitoinicializační
        /// </summary>
        public GRow Control
        {
            get
            {
                if (this._Control == null)
                    this._Control = new GRow(this);
                return this._Control;
            }
            set { this._Control = value; }
        }
        private GRow _Control;
        /// <summary>
        /// Záhlaví tohoto řádku, grafický prvek, auitoinicializační
        /// </summary>
        public GRowHeader RowHeader
        {
            get
            {
                if (this._RowHeader == null)
                    this._RowHeader = new GRowHeader(this);
                return this._RowHeader;
            }
            set { this._RowHeader = value; }
        }
        private GRowHeader _RowHeader;
        /// <summary>
        /// Řídící prvek TreeNode, autoinicializační
        /// </summary>
        public TreeNode TreeNode
        {
            get
            {
                if (this._TreeNode == null)
                    this._TreeNode = new TreeNode(this);
                return this._TreeNode;
            }
        }
        private TreeNode _TreeNode;
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
        #endregion
        #region Výška řádku, kompletní layout okolo výšky řádku, implementace ISequenceLayout a IVisualParent
        /// <summary>
        /// Koordinátor výšky řádku.
        /// Hodnota může být čtena i jako Int32 (implciitní konverze) = přímo výška řádku.
        /// </summary>
        internal ItemSizeInt RowSize { get { if (this._RowSize == null) this._RowSize = new ItemSizeInt(this.Table?.RowSize); return this._RowSize; } }
        private ItemSizeInt _RowSize;
        /// <summary>
        /// Zadaná výška řádku.
        /// Hodnotu může vložit aplikační kód, hodnota se projeví v GUI.
        /// Uživatel může interaktivně měnit velikost objektu, změna se projeví v této hodnotě.
        /// Veškerá další nastavení jsou v property HeightLayout.
        /// </summary>
        public Int32? Height { get { return this.RowSize.Size.Value; } set { this.RowSize.Size = value; } }
        /// <summary>
        /// true pro aktuálně viditelný řádek (default), false for skrytý (řádkové filtry, Tree nody).
        /// Tato hodnota odráží reálnou aktuální viditelnost v Gridu.
        /// </summary>
        public bool Visible { get { return this.RowSize.Visible; } set { this.RowSize.Visible = value; } }
        /// <summary>
        /// true pro řádek, který nemá být zobrazován z aplikačního důvodu, false (default) = řádek je dostupný
        /// </summary>
        public bool Hidden { get; set; }
        int IVisualParent.ClientWidth { get { return (this.HasGTable ? this.GTable.Bounds.Width : 0); } set { } }
        int IVisualParent.ClientHeight
        {
            // Výška řádku je + 1 pixel vyšší proti výšce klientského prostoru (grid line):
            get { return (this.RowSize.Size.Value - 1); }
            set
            {
                int height = value + 1;
                int oldValue = this.RowSize.Size.Value;
                this.RowSize.Size = height;
                int newValue = this.RowSize.Size.Value;
                if (newValue != oldValue && this.HasGTable)
                    this.GTable.Invalidate(InvalidateItem.RowHeight);
            }
        }
        #endregion
        #region Datové služby řádku
        /// <summary>
        /// Vrátí true, pokud tento řádek v daném sloupci obsahuje hodnotu různou od null.
        /// Pokud daný sloupec je null nebo neexistuje, pak vrací false.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool HasValue(Column column)
        {
            if (column == null) return false;
            Cell cell = this[column];
            return (cell != null ? cell.HasValue : false);
        }
        /// <summary>
        /// Obsahuje identifikátor záznamu, který se nachází v tomto řádku.
        /// Ten může být zadán explicitně do této property, anebo jej řádek vygeneruje ze svých dat a z údajů o sloupci <see cref="Table.PrimaryKeyColumn"/>.
        /// </summary>
        public GId RecordGId
        {
            get
            {
                if (this._RecordGId == null)
                    this._RecordGId = this._GetRecordGId();
                return this._RecordGId;
            }
            protected set
            {
                this._RecordGId = value;
            }
        }
        /// <summary>
        /// Určí ID záznamu <see cref="GId"/> z this řádku.
        /// Tato metoda ignoruje hodnotu v <see cref="_RecordGId"/>, volá se právě proto, že <see cref="_RecordGId"/> je null.
        /// Tato metoda získá <see cref="ITableInternal.PrimaryKeyColumn"/>, najde data v odpovídající buňce tohoto řádku, 
        /// vyhodnotí je a vrátí <see cref="GId"/> nebo null.
        /// </summary>
        /// <returns></returns>
        private GId _GetRecordGId()
        {
            // Bez tabulky a bez PrimaryKeyColumn to nepůjde:
            if (!this.HasTable) return null;
            Column primaryColumn = this.ITable.PrimaryKeyColumn;
            if (primaryColumn == null) return null;

            // Ve sloupci [primaryColumn] může být umístěn GId nebo GuiId:
            object value = this[primaryColumn].Value;
            if (value is GId) return value as GId;
            if (value is GuiId) return (GId)(value as GuiId);

            // Ve sloupci [primaryColumn] může být umístěn Int32, reprezentující číslo záznamu; pak ale potřebujeme i číslo třídy = z dat sloupce [primaryColumn]:
            if (!(value is Int32)) return null;
            Int32 recordId = (Int32)value;

            // Tabulka sama by měla znát číslo své třídy:
            Int32 classId = 0;
            if (this.Table.ClassId.HasValue)
                classId = this.Table.ClassId.Value;
            else
            {
                if (!primaryColumn.RecordClassNumber.HasValue) return null;
                classId = primaryColumn.RecordClassNumber.Value;
            }

            // Číslo záznamu může být Master nebo Entry:
            switch (primaryColumn.ColumnContent)
            {
                case ColumnContentType.MasterId: return new GId(classId, recordId);
                case ColumnContentType.EntryId: return new GId(classId, 0, recordId);
            }
            return null;
        }
        /// <summary>
        /// Úložiště pro hodnotu klíče tohoto řádku
        /// </summary>
        private GId _RecordGId;
        /// <summary>
        /// Obsahuje identifikátor Parent záznamu, pod kterým je this řádek Childem.
        /// </summary>
        public GId ParentRecordGId
        {
            get { return this.TreeNode.ParentRecordGId; }
            set { this.TreeNode.ParentRecordGId = value; }
        }
        /// <summary>
        /// Barva pozadí tohoto řádku, null = výchozí
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Libovolná aplikační data.
        /// Toto je prostor, který může využít aplikace k uložení svých dat nad rámec dat třídy.
        /// </summary>
        public object UserData { get; set; }
        #endregion
        #region IContentValidity
        bool IContentValidity.DataIsValid { get { return _RowDataIsValid; } set { _RowDataIsValid = value; } } private bool _RowDataIsValid;
        bool IContentValidity.RowLayoutIsValid { get { return _RowLayoutIsValid; } set { _RowLayoutIsValid = value; } } private bool _RowLayoutIsValid;
        bool IContentValidity.ColumnLayoutIsValid { get { return _ColumnLayoutIsValid; } set { _ColumnLayoutIsValid = value; } } private bool _ColumnLayoutIsValid;
        #endregion
        #region IComparableItem
        /// <summary>
        /// Metoda je volána jedenkrát pro jeden řádek, před tříděním seznamu řádků podle daného sloupce (columnId).
        /// Pokud sloupec má implementován svůj komparátor (Column.ValueComparator není null), pak na vstupu je valueIsComparable = false,
        ///  a tato metoda má připravit hodnotu z dané buňky (Cell[columnId]) do zdejší proměnné _IComparableItemValue
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="valueIsComparable"></param>
        void IComparableItem.PrepareValue(int columnId, bool valueIsComparable)
        {
            var cell = this._GetCell(columnId);
            if (valueIsComparable)
                // Daný sloupec NEMÁ svůj komparátor, v tom případě hodnota buňky musí být IComparable a vloží se do IComparableItem.ValueComparable:
                this._IComparableItemValueComparable = cell.Value as IComparable;
            else
                // Daný sloupec MÁ svůj komparátor, v tom případě hodnota buňky je libovolného typu, vloží se do IComparableItem.Value, zpracovávat ji bude komparátor sloupce:
                this._IComparableItemValue = cell.Value;
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
    public partial class Cell : IVisualMember
    {
        #region Konstruktor, reference na parenty této buňky
        internal Cell(Row dRow, int columnId)
        {
            this._ColumnId = columnId;
            this._Row = dRow;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string row = (this._Row != null ? this._Row.RowId.ToString() : "??");
            string col = (this._ColumnId >= 0 ? this._ColumnId.ToString() : "??");
            return "Cell[" + row + "," + col + "] "
                + " in " + (this.HasTable ? this.Table.Text : "NULL")
                + " Content: " + this.Text;
        }
        /// <summary>
        /// Jednoduché textové vyjádření obsahu this instance
        /// </summary>
        public string Text
        {
            get
            {
                TableValueType valueType = this.ValueType;
                if (valueType == TableValueType.Text || valueType == TableValueType.TextRelation)
                    return this.Value.ToString();
                return valueType.ToString();
            }
        }
        /// <summary>
        /// true pokud mám svůj řádek
        /// </summary>
        protected bool HasRow { get { return (this._Row != null); } }
        /// <summary>
        /// Řádek, do něhož tato buňka patří
        /// </summary>
        public Row Row { get { return _Row; } } private Row _Row;
        /// <summary>
        /// true pokud mám svojí tabulku
        /// </summary>
        protected bool HasTable { get { return (this.HasRow ? this._Row.HasTable : false); } }
        /// <summary>
        /// Tabulka, do které tato buňka patří
        /// Může být null, pokud buňka dosud není v řádku nebo řádek není v tabulce.
        /// </summary>
        public Table Table { get { return (this._Row != null ? this._Row.Table : null); } }
        /// <summary>
        /// true pokud mám svůj sloupec
        /// </summary>
        protected bool HasColumn { get { return (this.HasTable ? this.Table.ContainsColumn(this._ColumnId) : false); } }
        /// <summary>
        /// Sloupec, do něhož tato buňka patří.
        /// Může být null, pokud buňka dosud není v řádku nebo řádek není v tabulce.
        /// </summary>
        public Column Column { get { Column column = null; if (this.HasTable && this.Table.TryGetColumn(this._ColumnId, out column)) return column; return null; } }
        /// <summary>
        /// true pokud tato buňka má svou tabulku, a tabulka má svoji grafickou vrstvu
        /// </summary>
        public bool HasGTable { get { return (this.HasTable && this.Table.HasGTable); ; } }
        /// <summary>
        /// Reference na grafickou vrstvu tabulky
        /// </summary>
        public GTable GTable { get { return (this.HasGTable ? this.Table.GTable : null); } }
        /// <summary>
        /// ColumnID sloupce, do kterého tato buňka patří.
        /// Tato hodnota je platná bez ohledu na to, zda buňka (resp. její řádek) již je nebo není obsažena v tabulce.
        /// </summary>
        public int ColumnId { get { return _ColumnId; } } private int _ColumnId;
        #endregion
        #region Value
        /// <summary>
        /// Hodnota v této buňce. 
        /// Vložení hodnoty provede invalidaci dat řádku a tabulky, protože může dojít ke změně výšky a k požadavku na nové vykreslení obsahu.
        /// </summary>
        public object Value
        {
            get { return _Value; }
            set
            {
                _Value = value;
                this.InvalidateRowData();
                if (value != null && value is IInteractiveParent)
                {
                    IInteractiveParent iParent = (value as IInteractiveParent);
                    iParent.Parent = this.Control;
                }
            }
        }
        private object _Value;
        /// <summary>
        /// Explicitně daná výška v tomto řádku. Je daná obsahem.
        /// Výška se uplatní při určení výšky řádku.
        /// </summary>
        public Int32? Height { get { return _Height; } set { _Height = value; this.InvalidateRowLayout(); } } private Int32? _Height;
        /// <summary>
        /// Text pro ToolTip pro tuto buňku, lokalizovaný
        /// </summary>
        public Localizable.TextLoc ToolTip { get { return _ToolTip; } set { _ToolTip = value; } } private Localizable.TextLoc _ToolTip;
        /// <summary>
        /// Pokud this.ValueType bude Image, pak je možno použít tento obrázek (Cell.Value) jako ToolTip.Image.
        /// </summary>
        public bool UseImageAsToolTip { get { return _UseImageAsToolTip; } set { _UseImageAsToolTip = value; } } private bool _UseImageAsToolTip;
        /// <summary>
        /// Velký obrázek do tooltipu k buňce.
        /// Obrázek lze vložit do buňky, která je libovolného typu vyjma buňky typu Image (tam se neuplatní).
        /// </summary>
        public Image ToolTipImage { get { return _ToolTipImage; } set { _ToolTipImage = value; } }
        private Image _ToolTipImage;
        /// <summary>
        /// Typ obsahu této buňky, určuje režim kreslení jejího obsahu.
        /// Určuje se dynamicky podle typu this.Value, tady pokud je zde určeno "Image", pak Value určitě není Null.
        /// </summary>
        public TableValueType ValueType { get { return Data.Table.GetValueType(this.Value); } }
        /// <summary>
        /// Invaliduje řádek
        /// </summary>
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
        /// <summary>
        /// Invaliduje data řádku
        /// </summary>
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
        #region GUI vlastnosti
        /// <summary>
        /// true pokud this buňka je aktivní (=vybraná kurzorem)
        /// </summary>
        public bool IsActive { get { return (this.HasGTable ? this.GTable.IsCellActive(this) : false); } }
        /// <summary>
        /// true pokud this buňka je nyní pod myší (=myš se pohybuje nad ní)
        /// </summary>
        public bool IsMouseHot { get { return (this.HasGTable ? this.GTable.IsCellHot(this) : false); } }
        /// <summary>
        /// true pokud this buňka obsahuje Vztah (buď přímo zdejší <see cref="ValueType"/> je <see cref="TableValueType.TextRelation"/>, anebo sloupec má <see cref="Data.Column.IsRelation"/> = true).
        /// </summary>
        public bool IsRelation
        {
            get
            {
                if (this.ValueType == TableValueType.TextRelation) return true;
                if (this.HasColumn) return this.Column.IsRelation;
                return false;
            }
        }
        /// <summary>
        /// Grafická instance reprezentující tuto buňku, grafický prvek, auitoinicializační
        /// </summary>
        public GCell Control
        {
            get
            {
                if (this._Control == null)
                    this._Control = new GCell(this);
                return this._Control;
            }
            set { this._Control = value; }
        }
        private GCell _Control;
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
                return VisualStyle.CreateFrom(
                    this.VisualStyle,
                    (this.HasRow ? this.Row.VisualStyle : null),
                    (this.HasColumn ? this.Column.VisualStyle : null),
                    (this.HasTable ? this.Table.VisualStyle : null));
            }
        }
        #endregion
        #region Datové služby buňky
        /// <summary>
        /// Vrátí true, pokud tato buňka obsahuje hodnotu různou od null.
        /// Vrací false, pokud this buňka obsahuje <see cref="Value"/> == null.
        /// </summary>
        public bool HasValue
        {
            get { return (this.Value == null); }
        }
        /// <summary>
        /// Vrátí hodnotu z této buňky typovanou na daný typ (T).
        /// Pokud buňka obsahuje null (<see cref="HasValue"/> == false), vrací default(T).
        /// Pokud obsah buňky není převoditelný na (T), vyhodí chybu (aplikace čte nesprávným postupem).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            if (this.Value == null) return default(T);
            try
            {
                T result = (T)DataExtensions.GetValue<T>(this.Value);
                return result;
            }
            catch (Exception exc)
            {
                throw new GraphLibDataException("Hodnotu ze sloupce " + this.Column.ColumnName + " nelze převést na typ " + typeof(T).Name + ", hodnota je typu " + this.Value.GetType().Name + ", pokus o převod skončil chybou " + exc.Message + ".");
            }
        }
        /// <summary>
        /// Přečte hodnotu z této buňky typovanou na daný typ (T).
        /// Pokud buňka obsahuje null (<see cref="HasValue"/> == false), nebo její obsah není převoditelný na (T), přečte default(T).
        /// Metoda vrací true, pokud obsah buňky je null (to bereme jako OK) anebo pokud obsah buňky je převoditelný na (T) (to je OK).
        /// Metoda vrací false, pokud buňka není null, ale její typ není převoditelný na (T) == to je chyba!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(out T value)
        {
            try
            {
                value = (T)DataExtensions.GetValue<T>(this.Value);
                return true;
            }
            catch (Exception)
            {
                value = default(T);
            }
            return false;
        }
        /// <summary>
        /// Obsahuje identifikátor záznamu, který se nachází v této buňce.
        /// To funguje pouze tehdy, když buňka patří do sloupce, který je korektně označen a naplněn jako vztahový.
        /// Více v <see cref="Column.RelatedRecordColumnName"/> a v metodě <see cref="Table.GetRelationKeyColumn(Column)"/>.
        /// </summary>
        public GId RelatedRecordGId
        {
            get
            {
                if (this.ValueType == TableValueType.TextRelation)
                {
                    GuiIdText id = this.Value as GuiIdText;
                    if (id != null && id.GuiId != null) return (GId)(id.GuiId);                    // Buňka sama nese ID vztaženého záznamu
                }

                if (this.Row == null || this.Column == null || this.Table == null) return null;    // Buňka dosud není v řádku, nebo ve sloupci nebo v tabulce
                Column keyColumn = this.Table.GetRelationKeyColumn(this.Column);                   // Sloupec, který obsahuje číslo záznamu ve vztahu
                if (keyColumn == null) return null;

                int recordNumber;
                if (!this.Row[keyColumn].TryGetValue<int>(out recordNumber)) return null;          // Sloupec s číslem záznamu neobsahuje číslo?
                return new GId(keyColumn.RecordClassNumber.Value, recordNumber);
            }
        }
        #endregion
    }
    #endregion
    #region Importy z GuiDataTable a z Import z System.Data.DataTable
    public partial class Table
    {
        /// <summary>
        /// Metoda vytvoří novou tabulku <see cref="Table"/> na základě dat z tabulky <see cref="GuiDataTable"/>.
        /// </summary>
        /// <param name="guiTable"></param>
        /// <returns></returns>
        public static Table CreateFrom(GuiDataTable guiTable)
        {
            if (guiTable == null) return null;
            Table table = new Table(guiTable.TableName);
            table.Columns.AddRange(Column.CreateFrom(guiTable.Columns));      // Přidávat prvky musím včetně logiky AddItemAfter, kvůli navazujícím algoritmům (indexy, owner)
            table.Rows.AddRange(Row.CreateFrom(guiTable.Rows));               // Vytvoří řádky
            return table;
        }
        /// <summary>
        /// Metoda vytvoří novou tabulku <see cref="Table"/> na základě dat z tabulky <see cref="System.Data.DataTable"/>.
        /// </summary>
        /// <param name="dataTable">Data tabulky (sloupce, jejich properties, řádky)</param>
        /// <param name="tagItems">Data štítků <see cref="TagItem"/> ke všem řádkům</param>
        /// <returns></returns>
        public static Table CreateFrom(System.Data.DataTable dataTable, IEnumerable<KeyValuePair<GId, TagItem>> tagItems = null)
        {
            if (dataTable == null) return null;
            Table table = new Table(dataTable.TableName);
            table.Columns.AddRange(Column.CreateFrom(dataTable.Columns));      // Přidávat prvky musím včetně logiky AddItemAfter, kvůli navazujícím algoritmům (indexy, owner)
            table.Rows.AddRange(Row.CreateFrom(dataTable.Rows, table.ClassId, tagItems));     // Vytvoří řádky, a současně do nich vloží TagItems
            return table;
        }
    }
    public partial class Column
    {
        /// <summary>
        /// Metoda vytvoří soupis sloupců <see cref="Column"/> na základě dat o sloupcích z tabulky <see cref="GuiDataColumn"/>.
        /// </summary>
        /// <param name="guiColumns">Kolekce sloupců, vstup</param>
        /// <returns></returns>
        public static IEnumerable<Column> CreateFrom(IEnumerable<GuiDataColumn> guiColumns)
        {
            if (guiColumns == null) return null;

            // 1. Načíst data o sloupcích:
            List<Column> columnList = new List<Column>();
            foreach (GuiDataColumn dataColumn in guiColumns)
            {
                Column column = Column.CreateFrom(dataColumn);
                if (column != null)
                    columnList.Add(column);
            }

            // 2. Najít křížově uložené údaje o vztazích:
            foreach (Column column in columnList.Where(c => c.ColumnContent == ColumnContentType.RelationRecordData))
                SetRelationClassNumberToKeyColumn(column, columnList);

            return columnList;
        }
        /// <summary>
        /// Metoda vytvoří jeden sloupec <see cref="Column"/> na základě dat o sloupci z tabulky <see cref="GuiDataColumn"/>.
        /// </summary>
        /// <param name="guiColumn">Konkrétní sloupec, vstup</param>
        /// <returns></returns>
        public static Column CreateFrom(GuiDataColumn guiColumn)
        {
            if (guiColumn == null) return null;
            Column column = new Column(guiColumn.Name);

            column.Title = guiColumn.Title;
            column.ToolTip = guiColumn.ToolTip;
            column.DefaultValue = guiColumn.ColumnDefaultValue;
            column.Alignment = guiColumn.Alignment;
            column.ReadOnly = guiColumn.ColumnReadOnly;
            column.AllowColumnSortByClick = guiColumn.AllowSort;                // Povoleno třídění kliknutím
            column.ColumnContent = GetColumnContent(guiColumn);                 // Obsah sloupce
            column.FormatString = GetFormatString(guiColumn.Format);            // Formátovací string z Norisu, musí se převést na .NET
            column.IsVisible = guiColumn.IsVisible;                             // Je viditelný
            column.Width = GetWidth(guiColumn.Width);                           // Na vstupu je šířka Noris, v této metodě to lze upravit
            column.RecordClassNumber = GetClassNumber(guiColumn);
            column.RelatedRecordColumnName = guiColumn.RelationRecordIdColumnName;

            return column;
        }
        /// <summary>
        /// Metoda vytvoří soupis sloupců <see cref="Column"/> na základě dat o sloupcích z tabulky <see cref="System.Data.DataColumnCollection"/>.
        /// </summary>
        /// <param name="dataColumns">Kolekce sloupců, vstup</param>
        /// <returns></returns>
        public static IEnumerable<Column> CreateFrom(System.Data.DataColumnCollection dataColumns)
        {
            if (dataColumns == null) return null;

            // 1. Načíst data o sloupcích:
            List<Column> columnList = new List<Column>();
            foreach (System.Data.DataColumn dataColumn in dataColumns)
            {
                Column column = Column.CreateFrom(dataColumn);
                if (column != null)
                    columnList.Add(column);
            }

            // 2. Najít křížově uložené údaje o vztazích:
            foreach (Column column in columnList.Where(c => c.ColumnContent == ColumnContentType.RelationRecordData))
                SetRelationClassNumberToKeyColumn(column, columnList);

            return columnList;
        }
        /// <summary>
        /// Metoda vytvoří jeden sloupec <see cref="Column"/> na základě dat o sloupci z tabulky <see cref="System.Data.DataColumn"/>.
        /// </summary>
        /// <param name="dataColumn">Konkrétní sloupec, vstup</param>
        /// <returns></returns>
        public static Column CreateFrom(System.Data.DataColumn dataColumn)
        {
            if (dataColumn == null) return null;
            Column column = new Column(dataColumn.ColumnName);

            column.Title = dataColumn.Caption;
            column.DefaultValue = dataColumn.DefaultValue;
            column.ReadOnly = dataColumn.ReadOnly;

            DataColumnExtendedInfo extendedInfo = DataColumnExtendedInfo.CreateForColumn(dataColumn);
            column.AllowColumnSortByClick = extendedInfo.AllowSort;                   // Povoleno třídění kliknutím
            column.ColumnContent = GetColumnContent(extendedInfo);                    // Obsah sloupce
            column.FormatString = GetFormatString(extendedInfo.Format);               // Formátovací string z Norisu, musí se převést na .NET
            column.IsVisible = extendedInfo.IsVisible;                                // Je viditelný
            if (!String.IsNullOrEmpty(extendedInfo.Label)) column.Title = extendedInfo.Label;      // Jen pokud je vyplněno
            column.Width = GetWidth(extendedInfo.Width);                              // Na vstupu je šířka Noris, v této metodě to lze upravit
            column.RecordClassNumber = GetClassNumber(extendedInfo);
            column.RelatedRecordColumnName = extendedInfo.RelationRecordColumnName;

            return column;
        }
        /// <summary>
        /// Pro daný sloupec, který obsahuje data vztaženého záznamu (jeho <see cref="Column.ColumnContent"/> == <see cref="ColumnContentType.RelationRecordData"/>)
        /// (pokud obsahuje číslo třídy záznamu) najde sloupec, který obsahuje číslo záznamu, a v případě potřeby do něj vloží číslo třídy.
        /// </summary>
        /// <param name="relationDataColumn"></param>
        /// <param name="columnList"></param>
        private static void SetRelationClassNumberToKeyColumn(Column relationDataColumn, List<Column> columnList)
        {
            if (relationDataColumn == null || !relationDataColumn.IsRelation || !relationDataColumn.RecordClassNumber.HasValue) return;

            Column relationKeyColumn = GetRelationKeyColumn(relationDataColumn, columnList);
            if (relationKeyColumn == null || !relationKeyColumn.IsRelation || relationKeyColumn.RecordClassNumber.HasValue) return;

            relationKeyColumn.RecordClassNumber = relationDataColumn.RecordClassNumber;
        }
        /// <summary>
        /// Metoda najde a vrátí sloupec, který nese ČÍSLO ZÁZNAMU k danému sloupci, který nese VIZUÁLNÍ DATA záznamu ve vztahu.
        /// </summary>
        /// <param name="relationDataColumn"></param>
        /// <param name="columnList"></param>
        /// <returns></returns>
        private static Column GetRelationKeyColumn(Column relationDataColumn, List<Column> columnList)
        {
            if (relationDataColumn == null) return null;
            if (relationDataColumn.ColumnContent == ColumnContentType.RelationRecordId && relationDataColumn.RecordClassNumber.HasValue) return relationDataColumn;    // daný sloupec obsahuje číslo záznamu
            if (relationDataColumn.ColumnContent != ColumnContentType.RelationRecordData) return null;

            string relationColumnName = relationDataColumn.RelatedRecordColumnName;        // Název sloupce, který obsahuje číslo vztaženého záznamu
            if (String.IsNullOrEmpty(relationColumnName)) return null;
            Column relationKeyColumn = columnList.FirstOrDefault(c => String.Equals(c.ColumnName, relationColumnName, StringComparison.InvariantCultureIgnoreCase));
            return relationKeyColumn;
        }
        /// <summary>
        /// Vrátí typ obsahu pro daný sloupec, podle jeho <see cref="DataColumnExtendedInfo.BrowseColumnType"/> a dalších hodnot
        /// </summary>
        /// <param name="extendedInfo"></param>
        /// <returns></returns>
        protected static ColumnContentType GetColumnContent(DataColumnExtendedInfo extendedInfo)
        {
            return GetColumnContent(extendedInfo.Index, extendedInfo.BrowseColumnType, extendedInfo.RelationClassNumber);
        }
        /// <summary>
        /// Vrátí typ obsahu pro daný sloupec, podle jeho <see cref="DataColumnExtendedInfo.BrowseColumnType"/> a dalších hodnot
        /// </summary>
        /// <param name="guiColumn"></param>
        /// <returns></returns>
        protected static ColumnContentType GetColumnContent(GuiDataColumn guiColumn)
        {
            return GetColumnContent(guiColumn.Index, guiColumn.BrowseColumnType, guiColumn.RelationClassId);
        }
        /// <summary>
        /// Vrátí typ obsahu pro daný sloupec, podle jeho <see cref="DataColumnExtendedInfo.BrowseColumnType"/> a dalších hodnot
        /// </summary>
        /// <param name="index"></param>
        /// <param name="columnType"></param>
        /// <param name="relationClassNumber"></param>
        /// <returns></returns>
        protected static ColumnContentType GetColumnContent(int index, BrowseColumnType columnType, int? relationClassNumber)
        {
            if (index == 0) return ColumnContentType.MasterId;
            switch (columnType)
            {
                case BrowseColumnType.RecordId:
                    return ColumnContentType.RecordGId;
                case BrowseColumnType.SubjectNumber:
                    return ColumnContentType.MasterId;
                case BrowseColumnType.ObjectNumber:
                    return ColumnContentType.EntryId;
                case BrowseColumnType.DataColumn:
                    bool isRelation = (relationClassNumber.HasValue && relationClassNumber.Value > 0);
                    return (isRelation ? ColumnContentType.RelationRecordData : ColumnContentType.UserData);
                case BrowseColumnType.RelationHelpfulColumn:
                    return ColumnContentType.RelationRecordId;
                case BrowseColumnType.TotalCountHelpfulColumn:
                    return ColumnContentType.HiddenData;
            }
            return ColumnContentType.None;
        }
        /// <summary>
        /// Vrací formátovací string pro sloupec
        /// </summary>
        /// <param name="greenFormat"></param>
        /// <returns></returns>
        protected static string GetFormatString(string greenFormat)
        {
            return null;
        }
        /// <summary>
        /// Vrací šířku v pixelech pro daný sloupec
        /// </summary>
        /// <param name="greenWidth"></param>
        /// <returns></returns>
        protected static int? GetWidth(int greenWidth)
        {
            return 1 * greenWidth;
        }
        /// <summary>
        /// Vrací číslo třídy pro daný sloupec, pokud je zadaná
        /// </summary>
        /// <param name="extendedInfo"></param>
        /// <returns></returns>
        protected static int? GetClassNumber(DataColumnExtendedInfo extendedInfo)
        {
            if (extendedInfo.Index == 0) return extendedInfo.ClassNumber;
            if (extendedInfo.RelationClassNumber.HasValue) return extendedInfo.RelationClassNumber;
            return null;
        }
        /// <summary>
        /// Vrací číslo třídy pro daný sloupec, pokud je zadaná
        /// </summary>
        /// <param name="guiColumn"></param>
        /// <returns></returns>
        protected static int? GetClassNumber(GuiDataColumn guiColumn)
        {
            BrowseColumnType colType = guiColumn.BrowseColumnType;
            if ((colType == BrowseColumnType.RecordId || colType == BrowseColumnType.SubjectNumber || colType == BrowseColumnType.ObjectNumber) && guiColumn.TableClassId.HasValue) return guiColumn.TableClassId;
            if ((colType == BrowseColumnType.RelationHelpfulColumn) && guiColumn.RelationClassId.HasValue) return guiColumn.RelationClassId;
            return null;
        }
    }
    public partial class Row
    {
        /// <summary>
        /// Metoda vytvoří soupis řádků <see cref="Row"/> na základě dat o řádcích třídy <see cref="GuiDataRow"/>.
        /// </summary>
        /// <param name="guiRows">Kolekce řádků, vstup</param>
        /// <returns></returns>
        public static IEnumerable<Row> CreateFrom(IEnumerable<GuiDataRow> guiRows)
        {
            if (guiRows == null) return null;
            List<Row> rowList = new List<Row>();
            foreach (GuiDataRow dataRow in guiRows)
            {
                Row row = Row.CreateFrom(dataRow);
                if (row != null)
                    rowList.Add(row);
            }
            return rowList;
        }
        /// <summary>
        /// Metoda vytvoří jeden řádek <see cref="Row"/> na základě dat o řádku z tabulky <see cref="GuiDataRow"/>.
        /// </summary>
        /// <param name="guiRow">Konkrétní řádek, vstup</param>
        /// <returns></returns>
        public static Row CreateFrom(GuiDataRow guiRow)
        {
            if (guiRow == null) return null;
            Row row = new Row(guiRow.Cells.ToArray());
            row.RecordGId = guiRow.RowGuiId;
            row.ParentRecordGId = guiRow.ParentRowGuiId;
            row.BackColor = guiRow.BackColor;
            row.TagItems = TagItem.CreateFrom(guiRow.TagItems);
            row.UserData = guiRow;
            return row;
        }
        /// <summary>
        /// Metoda vytvoří soupis řádků <see cref="Row"/> na základě dat o řádcích z tabulky <see cref="System.Data.DataRowCollection"/>.
        /// </summary>
        /// <param name="dataRows">Kolekce řádků, vstup</param>
        /// <param name="tagItems">Data štítků <see cref="TagItem"/> ke všem řádkům</param>
        /// <param name="rowClassId">Číslo třídy tabulky (pochází z <see cref="Data.Table.ClassId"/>)</param>
        /// <returns></returns>
        public static IEnumerable<Row> CreateFrom(System.Data.DataRowCollection dataRows, int? rowClassId = null, IEnumerable<KeyValuePair<GId, TagItem>> tagItems = null)
        {
            if (dataRows == null) return null;
            bool addTags = (tagItems != null);
            DictionaryList<GId, TagItem> tagDict = (addTags ? new DictionaryList<GId, TagItem>(tagItems) : null);
            List<Row> rowList = new List<Row>();
            foreach (System.Data.DataRow dataRow in dataRows)
            {
                Row row = Row.CreateFrom(dataRow);
                if (row != null)
                {
                    if (addTags)
                        row.TagItems = GetTagItemsForRow(row, rowClassId, tagDict);
                    rowList.Add(row);
                }
            }
            return rowList;
        }
        /// <summary>
        /// Metoda vytvoří jeden řádek <see cref="Row"/> na základě dat o řádku z tabulky <see cref="System.Data.DataRow"/>.
        /// </summary>
        /// <param name="dataRow">Konkrétní řádek, vstup</param>
        /// <returns></returns>
        public static Row CreateFrom(System.Data.DataRow dataRow)
        {
            if (dataRow == null) return null;
            Row row = new Row(dataRow.ItemArray);
            return row;
        }
        /// <summary>
        /// Metoda najde a vrátí tagy pro daný řádek a třídu, z dodaného indexu
        /// </summary>
        /// <param name="row"></param>
        /// <param name="rowClassId"></param>
        /// <param name="tagDict"></param>
        /// <returns></returns>
        protected static TagItem[] GetTagItemsForRow(Row row, int? rowClassId, DictionaryList<GId, TagItem> tagDict)
        {
            int recordId;
            if (!row[0].TryGetValue<int>(out recordId)) return null;
            GId recordGId = new GId(rowClassId.HasValue ? rowClassId.Value : 0, recordId);
            TagItem[] tagItems;
            tagDict.TryGetValue(recordGId, out tagItems);
            return tagItems;
        }
    }
    #endregion
    #region TreeNode : Třída, které v sobě řeší všechny vlastnosti TreeNode pro jeden řádek Row
    /// <summary>
    /// TreeNode : Třída, které v sobě řeší všechny vlastnosti TreeNode pro jeden řádek <see cref="Row"/>
    /// </summary>
    public class TreeNode
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public TreeNode(Row owner)
        {
            this._Owner = owner;
        }
        private Row _Owner;
        #endregion
        #region Vztahy na Ownera a na Table
        /// <summary>
        /// Řádek, pro který je vytvořen this node
        /// </summary>
        protected Row Owner { get { return this._Owner; } }
        /// <summary>
        /// Tabulka
        /// </summary>
        protected Table Table { get { return this._Owner?.Table; } }
        /// <summary>
        /// Vyvolá invalidaci řádků tabulky
        /// </summary>
        protected void TableRowsInvalidate()
        {
            Table table = this.Table;
            if (table != null)
                table.InvalidateRows();
        }
        #endregion
        #region Public properties statické
        /// <summary>
        /// Obsahuje identifikátor Parent záznamu, pod kterým je this řádek Childem.
        /// </summary>
        public GId ParentRecordGId
        {
            get { return this._ParentRecordGId; }
            set { this._ParentRecordGId = value; }
        }
        private GId _ParentRecordGId;
        /// <summary>
        /// true pokud this řádek je Root, tj. nemá žádného Parenta.
        /// </summary>
        public bool IsRoot { get { return (this._ParentRecordGId == null); } }
        /// <summary>
        /// true pokud this řádek je Child řádkem, ať už statickým, nebo dynamickým. Dynamický Child řádek má nastaveno <see cref="IsDynamicChild"/> = true.
        /// </summary>
        public bool IsChild { get { return (this._ParentRecordGId != null); } }
        /// <summary>
        /// true pokud this řádek je Statickým Child řádkem = může se objevit jako Child řádek jen pod svým vlastním Parent řádkem.
        /// </summary>
        public bool IsStaticChild { get { return (this._ParentRecordGId != null && !this._ParentRecordGId.IsEmpty); } }
        /// <summary>
        /// true pokud this řádek je Dynamickým Child řádkem = může se objevit jako Child řádek pod kterýmkoli Parent řádkem, 
        /// což je dáno chování tabulky.
        /// </summary>
        public bool IsDynamicChild { get { return (this._ParentRecordGId != null && this._ParentRecordGId.IsEmpty); } }
        #endregion
        #region Child prvky tohoto nodu + vlastnosti nodu Level, Order
        /// <summary>
        /// Obsahuje Childs řádky tohoto Parent řádku. Může být null nebo může obsahovat 0 prvků.
        /// Setování vyvolá invalidaci řádků tabulky.
        /// </summary>
        public IEnumerable<Row> Childs { get { return this.ChildList; } }
        /// <summary>
        /// Položky Childs, vždy platné, nikdy null
        /// </summary>
        protected List<Row> ChildList
        {
            get
            {
                if (this._ChildList == null)
                {
                    this._ChildList = new List<Row>();
                    if (this._StaticChildDict != null && this._StaticChildDict.Count > 0)
                        this._ChildList.AddRange(this._StaticChildDict.Values);
                    if (this._DynamicChildList != null && this._DynamicChildList.Count > 0)
                        this._ChildList.AddRange(this._DynamicChildList);
                }
                return this._ChildList;
            }
        }
        
        /// <summary>
        /// Obsahuje true, pokud this řádek obsahuje nějaké <see cref="Childs"/> řádky
        /// </summary>
        public bool HasChilds
        {
            get
            {
                return ((this._StaticChildDict != null && this._StaticChildDict.Count > 0) ||
                       (this._DynamicChildList != null && this._DynamicChildList.Count > 0));
            }
        }
        /// <summary>
        /// Level Tree nodu.
        /// Hodnota 0 = Root řádek, hodnota 1 = Child řádek pod Root řádkem, atd.
        /// </summary>
        internal int Level { get; private set; }
        /// <summary>
        /// Pořadí nodu v sekvenci. 
        /// Má význam při vykreslování spojovacích linek.
        /// </summary>
        internal TreeNodeOrderType Order { get; private set; }
        /// <summary>
        /// Metoda přidá daný řádek jako Static Child
        /// </summary>
        /// <param name="row"></param>
        internal void AddStaticChild(Row row)
        {
            if (this._StaticChildDict == null)
                this._StaticChildDict = new Dictionary<int, Row>();
            this._StaticChildDict.AddRefresh(row.RowId, row);
            this._ChildsInvalidate();
        }
        /// <summary>
        /// Metoda odebere daný řádek ze Static Child
        /// </summary>
        /// <param name="row"></param>
        internal void RemoveStaticChild(Row row)
        {
            if (this._StaticChildDict != null && this._StaticChildDict.ContainsKey(row.RowId))
            {
                this._StaticChildDict.Remove(row.RowId);
                this._ChildsInvalidate();
            }
        }
        /// <summary>
        /// Metoda přidá daný řádek jako Dynamic Child
        /// </summary>
        /// <param name="row"></param>
        internal void AddDynamicChild(Row row)
        {
            if (this._DynamicChildList == null)
                this._DynamicChildList = new List<Row>();
            if (this._DynamicChildList.Count == 0 || !this._DynamicChildList.Any(r => Object.ReferenceEquals(r, row)))
            {
                this._DynamicChildList.Add(row);
                this._ChildsInvalidate();
            }
        }
        /// <summary>
        /// Metoda odebere daný řádek z Dynamic Child
        /// </summary>
        /// <param name="row"></param>
        internal void RemoveDynamicChild(Row row)
        {
            if (this._DynamicChildList != null)
            {
                if (this._DynamicChildList.RemoveAll(r => Object.ReferenceEquals(r, row)) > 0)
                    this._ChildsInvalidate();
            }
        }
        /// <summary>
        /// Child řádky tohoto řádku, dynamicky určované. Lze setovat.
        /// </summary>
        internal IEnumerable<Row> DynamicChilds
        {
            get { return this._DynamicChildList; }
            set
            {
                this._DynamicChildList = (value != null ? new List<Row>(value) : null);
                this._ChildsInvalidate();
            }
        }
        /// <summary>
        /// Invaliduje svoje Childs řádky, volá se po jakékoli změně.
        /// </summary>
        private void _ChildsInvalidate()
        {
            this._ChildList = null;
            this.TableRowsInvalidate();
        }
        /// <summary>
        /// List obsahující všechny aktuální Child řádky; null když je invalidován.
        /// </summary>
        private List<Row> _ChildList;
        /// <summary>
        /// List obsahující dynamické Child řádky; null když není žádný.
        /// Dynamické Childs mohou pocházet z různých tabulek, nelze je indexovat, a jediným identifikátorem je ObjectReference.
        /// </summary>
        private List<Row> _DynamicChildList;
        /// <summary>
        /// Dictionary obsahující statické Child řádky; null když není žádný.
        /// Statické Childs mohou pocházet pouze ze stejné tabulky jako Parent, proto lze použít Dictionary podle RowId (=v rámci jedné tabulky je unikátní).
        /// </summary>
        private Dictionary<int, Row> _StaticChildDict;
        #endregion
        #region Expand, Collapse
        /// <summary>
        /// Child nody tohoto řádku jsou otevřené?
        /// </summary>
        public bool IsExpanded
        {
            get { return this._IsExpanded; }
            set
            {
                bool oldValue = this._IsExpanded;
                bool newValue = value;
                if (newValue && !oldValue)
                    this.Expand();
                else if (!newValue && oldValue)
                    this.Collapse();
            }
        }
        /// <summary>
        /// Zajistí, že zdejší nody <see cref="Childs"/> budou viditelné.
        /// Po vyvolání této metody nebudou ale ChildNody Expandend, metoda na nich nastaví <see cref="IsExpanded"/> = false.
        /// </summary>
        public void Expand()
        {
            this._Expand(true);
        }
        /// <summary>
        /// Metoda zavře this node, a zavře i všechny jeho Child nody.
        /// </summary>
        public void Collapse()
        {
            this._Collapse(true);
        }
        /// <summary>
        /// Otevře this node, volitelně volá invalidaci
        /// </summary>
        /// <param name="invalidateRows"></param>
        private void _Expand(bool invalidateRows)
        {
            if (!this.HasChilds) return;

            bool oldValue = this._IsExpanded;
            this._CollapseChilds();
            this._CollapseOtherParents();
            this._IsExpanded = this.HasChilds;

            if (invalidateRows && (this._IsExpanded != oldValue))
                this.TableRowsInvalidate();
        }
        /// <summary>
        /// Zavře this node, volitelně volá invalidaci
        /// </summary>
        /// <param name="invalidateRows"></param>
        private void _Collapse(bool invalidateRows)
        {
            bool oldValue = this._IsExpanded;
            this._CollapseChilds();
            this._IsExpanded = false;
            if (invalidateRows && (this._IsExpanded != oldValue))
                this.TableRowsInvalidate();
        }
        /// <summary>
        /// Provede <see cref="_Collapse"/> pro všechny svoje <see cref="Childs"/>
        /// </summary>
        private void _CollapseChilds()
        {
            if (this.HasChilds)
            {
                foreach (Row child in this.Childs)
                    child.TreeNode._Collapse(false);
            }
        }
        /// <summary>
        /// Metoda zajistí, že všichni Parenti, kteří obsahují jako Child některý řádek z mých Childs, budou Collapsed.
        /// </summary>
        private void _CollapseOtherParents()
        {
            if (this.Table == null) return;

            var childDict = this.Childs.GetDictionary(row => row.RowId, true);
            this.Table.TreeNodeScan(
                (row, level) =>
                {   // Akce pro každý řádek: 
                    // Pokud daný řádek je otevřený, a některý z jeho Child řádků je obsažen v Dictionary childDict:
                    if (row.TreeNode.IsExpanded && row.TreeNode.Childs != null && row.TreeNode.Childs.Any(r => childDict.ContainsKey(r.RowId)))
                        // Pak takový řádek zavřeme:
                        row.TreeNode._Collapse(false);
                },
                // Projít Childs daného řádku?
                //  Ano, pokud řádek je otevřený:
                row => row.TreeNode.IsExpanded
                );
        }
        /// <summary>
        /// Příznak otevření Child nodů
        /// </summary>
        private bool _IsExpanded;
        #endregion
        #region Table + TreeView : vkládání Childs řádků do lineární sekvence; scanování Childs řádků
        /// <summary>
        /// Do daného seznamu řádků přidá svoje Child řádky
        /// </summary>
        /// <param name="allRowList"></param>
        public void StoreChildsTo(List<Row> allRowList)
        {
            this.StoreChildsTo(allRowList, 0);
        }
        /// <summary>
        /// Do daného seznamu řádků přidá svoje Child řádky
        /// </summary>
        /// <param name="allRowList"></param>
        /// <param name="level"></param>
        protected void StoreChildsTo(List<Row> allRowList, int level)
        {
            if (!this.HasChilds) return;
            if (!this.IsExpanded) return;
            level++;
            var childs = this.ChildList;
            int count = childs.Count;
            for (int i = 0; i < count; i++)
            {
                Row child = childs[i];
                child.TreeNode.Level = level;
                child.TreeNode.Order = (i == 0 ? (count > 1 ? TreeNodeOrderType.First : TreeNodeOrderType.Single) : (i < (count - 1) ? TreeNodeOrderType.Inner : TreeNodeOrderType.Last));
                allRowList.Add(child);
                child.Visible = true;            // Viditelnost pro Child řádky nastavuji výhradně zde.

                // Rekurzivně:
                child.TreeNode.StoreChildsTo(allRowList, level);
            }
        }
        /// <summary>
        /// Scanner nodů tohoto řádku + rekurzivně
        /// </summary>
        /// <param name="scanAction"></param>
        /// <param name="testScanChilds"></param>
        public void Scan(Action<Row, int> scanAction, Func<Row, bool> testScanChilds = null)
        {
            Scan(new Row[] { this._Owner }, 0, scanAction, testScanChilds);
        }
        /// <summary>
        /// Scanner dané kolekce + rekurzivně
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="level"></param>
        /// <param name="scanAction"></param>
        /// <param name="testScanChilds"></param>
        public static void Scan(IEnumerable<Row> rows, int level, Action<Row, int> scanAction, Func<Row, bool> testScanChilds = null)
        {
            if (rows == null) return;
            bool isTest = (testScanChilds != null);
            foreach (Row row in rows)
            {
                scanAction(row, level);
                if (row.TreeNode.HasChilds)
                {
                    bool scanChilds = (!isTest || testScanChilds(row));
                    if (scanChilds)
                        Scan(row.TreeNode.Childs, level + 1, scanAction, testScanChilds);
                }
            }
        }
        #region class NodeNext : třída pro tvorbu stromu řádků
        /// <summary>
        /// NodeNext : třída pro tvorbu stromu řádků
        /// </summary>
        protected class NodeNext<T>
        {
            #region Konstruktory
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="row"></param>
            public NodeNext(T row)
            {
                this.Parent = null;
                this.Item = row;
                this.NextList = new List<NodeNext<T>>();
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="row"></param>
            public NodeNext(NodeNext<T> parent, T row)
            {
                this.Parent = parent;
                this.Item = row;
                this.NextList = new List<NodeNext<T>>();
            }
            #endregion
            #region Public data
            /// <summary>
            /// Předchůdce
            /// </summary>
            public NodeNext<T> Parent { get; private set; }
            /// <summary>
            /// true = jsem první v řadě
            /// </summary>
            public bool IsFirst { get { return (this.Parent == null); } }
            /// <summary>
            /// Řádek
            /// </summary>
            public T Item { get; private set; }
            /// <summary>
            /// Seznam sousedních nodů
            /// </summary>
            public List<NodeNext<T>> NextList { get; private set; }
            /// <summary>
            /// Počet sousedních nodů
            /// </summary>
            public int NextCount { get { return this.NextList.Count; } }
            /// <summary>
            /// true = máme něco v NextCount
            /// </summary>
            public bool HasNext { get { return (this.NextCount > 0); } }
            /// <summary>
            /// Vloží Next řádky
            /// </summary>
            /// <param name="nextRows"></param>
            /// <param name="filter"></param>
            public void AddNextRows(IEnumerable<T> nextRows, Func<T, bool> filter)
            {
                if (nextRows == null) return;
                foreach (T row in nextRows)
                {
                    if (filter == null || filter(row))
                        this.NextList.Add(new NodeNext<T>(this, row));
                }
            }
            #endregion
            #region Kvazirekurze
            /// <summary>
            /// Metoda do daného nodu přidá prvky z kolekce, kterou získá danou metodou, a jejích prvky profiltruje je další metodou.
            /// Provádí to rekurzivně = pro přidané prvky se spustí identický algoritmus.
            /// </summary>
            /// <param name="node">Výchozí node, do něj se budou přidávat prvky</param>
            /// <param name="getCollection">Funkce, která získá kolekci next nodů</param>
            /// <param name="filter">Funkce, která každý získaný node proěří před tím, než se přidá do parent nodu. Null = přidají se všechny.</param>
            public static void AddNextRecursive(NodeNext<T> node, Func<T, IEnumerable<T>> getCollection, Func<T, bool> filter = null)
            {
                bool hasFilter = (filter != null);

                Queue<NodeNext<T>> queue = new Queue<NodeNext<T>>();
                queue.Enqueue(node);
                while (queue.Count > 0)
                {
                    NodeNext<T> workNode = queue.Dequeue();
                    IEnumerable<T> items = getCollection(workNode.Item);
                    if (items == null) continue;

                    foreach (T item in items)
                    {
                        if (!hasFilter || filter(item))
                        {
                            NodeNext<T> nextNode = new NodeNext<T>(workNode, item);
                            workNode.NextList.Add(nextNode);
                            queue.Enqueue(nextNode);
                        }
                    }
                }
            }
            #endregion
        }
        #endregion
        #endregion
    }
    /// <summary>
    /// Druh pořadí TreeNode v kolekci (první, vnitřní, poslední, jediný)
    /// </summary>
    internal enum TreeNodeOrderType { None, First, Inner, Last, Single }
    #endregion
    #region TagItem : Data pro jeden vizuální tag
    /// <summary>
    /// TagItem : Data pro jeden vizuální tag.
    /// Prvek má implicitní konverzi s datovým typem String; konvertuje se property <see cref="TagItem.Text"/>.
    /// </summary>
    public class TagItem : IOwnerProperty<GTagFilter>
    {
        #region Konstrukce, vztah na Ownera
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TagItem()
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TagItem(string text)
        {
            this._Text = text;
        }
        /// <summary>
        /// Konstruktor pro vytvoření klonu.
        /// Neklonuje se Owner.
        /// </summary>
        /// <param name="source"></param>
        public TagItem(TagItem source)
        {
            if (source != null)
            {
                this._Text = source._Text;
                this._BackColor = source._BackColor;
                this._CheckedBackColor = source._CheckedBackColor;
                this._BorderColor = source._BorderColor;
                this._TextColor = source._TextColor;
                this._Size = source._Size;
                this._Visible = source._Visible;
                this._Checked = source._Checked;
                this.UserData = source.UserData;
            }
        }
        /// <summary>
        /// Konstruktor pro vytvoření <see cref="TagItem"/> z instance <see cref="GuiTagItem"/>
        /// </summary>
        public TagItem(GuiTagItem guiTagItem)
        {
            if (guiTagItem != null)
            {
                this.Text = guiTagItem.TagText;
                this.BackColor = guiTagItem.BackColor;
                this.CheckedBackColor = guiTagItem.BackColorChecked;
                this.BorderColor = null;
                this.TextColor = null;
                this.Size = null;
                this.Visible = true;
                this.Checked = false;
                this.UserData = guiTagItem.UserData;
            }
        }
        /// <summary>
        /// Vrátí pole <see cref="TagItem"/> z dodané kolekce <see cref="GuiTagItem"/>
        /// </summary>
        /// <param name="guiTagItems"></param>
        /// <returns></returns>
        public static TagItem[] CreateFrom(IEnumerable<GuiTagItem> guiTagItems)
        {
            if (guiTagItems == null) return null;
            List<TagItem> tagItems = new List<TagItem>();
            foreach (GuiTagItem guiTagItem in guiTagItems)
            {
                if (guiTagItem != null)
                    tagItems.Add(new TagItem(guiTagItem));
            }
            return tagItems.ToArray();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Vlastník = <see cref="GTagFilter"/>
        /// </summary>
        GTagFilter IOwnerProperty<GTagFilter>.Owner { get { return this._Owner; } set { this._Owner = value; } }
        private GTagFilter _Owner;
        /// <summary>
        /// Zavolá Ownera, jeho metodu <see cref="GTagFilter._TagItemsChanged()"/>, 
        /// tím mu sdělí, že je třeba znovu přepočítat všechny prvky.
        /// </summary>
        private void _CallOwnerChange()
        {
            if (this._Owner != null)
                ((ITagFilter)this._Owner).TagItemsChanged();
        }
        /// <summary>
        /// Zavolá Ownera, jeho metodu <see cref="GTagFilter._TagItemsRepaint()"/>,
        /// tím mu sdělí, že je třeba pouze překreslit control, beze změny přepočtů.
        /// </summary>
        private void _CallOwnerRepaint()
        {
            if (this._Owner != null)
                ((ITagFilter)this._Owner).TagItemsRepaint();
        }
        #endregion
        #region Public data
        /// <summary>
        /// Zobrazovaný text
        /// </summary>
        public string Text { get { return this._Text; } set { this._Text = value; this._CallOwnerChange(); } }
        private string _Text;
        /// <summary>
        /// Explicitně definovaná barva pozadí
        /// </summary>
        public Color? BackColor { get { return this._BackColor; } set { this._BackColor = value; this._CallOwnerRepaint(); } }
        private Color? _BackColor;
        /// <summary>
        /// Explicitně definovaná barva pozadí ve stavu <see cref="Checked"/> = true
        /// </summary>
        public Color? CheckedBackColor { get { return this._CheckedBackColor; } set { this._CheckedBackColor = value; this._CallOwnerRepaint(); } }
        private Color? _CheckedBackColor;
        /// <summary>
        /// Explicitně definovaná barva rámečku
        /// </summary>
        public Color? BorderColor { get { return this._BorderColor; } set { this._BorderColor = value; this._CallOwnerRepaint(); } }
        private Color? _BorderColor;
        /// <summary>
        /// Explicitně definovaná barva textu
        /// </summary>
        public Color? TextColor { get { return this._TextColor; } set { this._TextColor = value; this._CallOwnerRepaint(); } }
        private Color? _TextColor;
        /// <summary>
        /// Relativní velikost proti ostatním prvkům
        /// </summary>
        public float? Size { get { return this._Size; } set { this._Size = value; this._CallOwnerChange(); } }
        private float? _Size;
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool Visible { get { return this._Visible; } set { this._Visible = value; this._CallOwnerChange(); } }
        private bool _Visible = true;
        /// <summary>
        /// Prvek je vybrán?
        /// </summary>
        public bool Checked { get { return this._Checked; } set { this._Checked = value; this._CallOwnerRepaint(); } }
        /// <summary>
        /// Prvek je vybrán?
        /// Jde o Silent hodnotu: její setování nezpůsobí překreslení vizuálního controlu.
        /// V podstatě tuto hodnotu má nastavovat pouze vizuální control sám - jako důsledek interakce uživatele.
        /// </summary>
        internal bool CheckedSilent { get { return this._Checked; } set { this._Checked = value; } }
        private bool _Checked;
        /// <summary>
        /// Libovolná uživatelská data
        /// </summary>
        public object UserData { get; set; }
        #endregion
        #region Implicitní konverze z/na String
        /// <summary>
        /// Implicitní konverze z <see cref="String"/> na <see cref="TagItem"/>.
        /// Pokud je na vstupu <see cref="String"/> = null, pak na výstupu je <see cref="TagItem"/> == null.
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator TagItem(String text) { return (text != null ? new TagItem(text) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="TagItem"/> na <see cref="String"/>.
        /// Pokud je na vstupu <see cref="TagItem"/> = null, pak na výstupu je <see cref="String"/> == null.
        /// </summary>
        /// <param name="tagItem"></param>
        public static implicit operator String(TagItem tagItem) { return (tagItem != null ? tagItem.Text : null); }
        #endregion
    }
    #endregion
    #region TableFilter : Předek pro třídy implementující filtr tabulky
    /// <summary>
    /// TableFilter : Předek pro třídy implementující filtr tabulky
    /// </summary>
    public class TableFilter
    {
        /// <summary>
        /// Konsturktor
        /// </summary>
        /// <param name="name"></param>
        public TableFilter(string name)
        {
            this.Name = name;
        }
        /// <summary>
        /// Jméno filtru
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Tato funkce aplikuje filtr <see cref="Filter"/>, 
        /// vrací true pro řádek který má být zobrazen.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        protected virtual bool ApplyFilter(Row row)
        {
            if (Filter == null) return false;
            return Filter(row);
        }
        /// <summary>
        /// Funkce, která se používá v metodě <see cref="ApplyFilter(Row)"/>
        /// </summary>
        public Func<Row, bool> Filter;
        /// <summary>
        /// Možné úložiště pro datový podklad filtru
        /// </summary>
        public object UserData;
    }
    #endregion
    #region TableRowCloneArgs : data pro podporu klonování obsahu řádku
    /// <summary>
    /// TableRowCloneArgs : data pro podporu klonování obsahu řádku
    /// </summary>
    public class TableRowCloneArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableRowCloneArgs()
        { }
        /// <summary>
        /// true = klonovat prvky TagItems; false = ne
        /// </summary>
        public bool CloneRowTagItems { get; set; }
        /// <summary>
        /// true = klonovat v rámci grafu jeho grafické prvky; false = ne
        /// </summary>
        public bool CloneGraphItems { get; set; }
        /// <summary>
        /// Filtr pro klonování prvů grafu
        /// </summary>
        public Func<Components.Graph.ITimeGraphItem, bool> CloneGraphsFilter { get; set; }

    }
    #endregion
    #region Interfaces
    /// <summary>
    /// Předpis pro tabulku, aby mohla dostávat události napřímo
    /// </summary>
    public interface ITableInternal
    {
        /// <summary>
        /// Změna hot řádku
        /// </summary>
        /// <param name="oldHotRow"></param>
        /// <param name="newHotRow"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void CallHotRowChanged(Row oldHotRow, Row newHotRow, EventSourceType eventSource, bool callEvents);
        /// <summary>
        /// Změna hot buňky
        /// </summary>
        /// <param name="oldHotCell"></param>
        /// <param name="newHotCell"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void CallHotCellChanged(Cell oldHotCell, Cell newHotCell, EventSourceType eventSource, bool callEvents);
        /// <summary>
        /// Změna aktivního řádku
        /// </summary>
        /// <param name="oldActiveRow"></param>
        /// <param name="newActiveRow"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void CallActiveRowChanged(Row oldActiveRow, Row newActiveRow, EventSourceType eventSource, bool callEvents);
        /// <summary>
        /// Změna aktivní buňky
        /// </summary>
        /// <param name="oldActiveCell"></param>
        /// <param name="newActiveCell"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void CallActiveCellChanged(Cell oldActiveCell, Cell newActiveCell, EventSourceType eventSource, bool callEvents);
        /// <summary>
        /// Změna hodnoty <see cref="Row.IsChecked"/>
        /// </summary>
        /// <param name="row"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="eventSource"></param>
        /// <param name="callEvents"></param>
        void CallCheckedRowChanged(Row row, bool oldValue, bool newValue, EventSourceType eventSource, bool callEvents);
        /// <summary>
        /// Událost MouseEnter
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void CallCellMouseEnter(Cell cell, GInteractiveChangeStateArgs e, bool callEvents);
        /// <summary>
        /// Událost MouseLeave
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void CallCellMouseLeave(Cell cell, GInteractiveChangeStateArgs e, bool callEvents);
        /// <summary>
        /// Událost CellClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void CallActiveCellClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents);
        /// <summary>
        /// Událost CellDoubleClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void CallActiveCellDoubleClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents);
        /// <summary>
        /// Událost CellLongClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void CallActiveCellLongClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents);
        /// <summary>
        /// Událost CellRightClick
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="e"></param>
        /// <param name="callEvents"></param>
        void CallActiveCellRightClick(Cell cell, GInteractiveChangeStateArgs e, bool callEvents);

        /// <summary>
        /// Invaliduje pole štítků
        /// </summary>
        void InvalidateTagItems();
        /// <summary>
        /// Sloupec, podle něhož se vytváří primární index v situaci, kdy <see cref="Row.RecordGId"/> není zadán explicitně
        /// </summary>
        Column PrimaryKeyColumn { get; }

    }
    /// <summary>
    /// Předpis pro prvek, který je zdrojem Tagů = "visaček" do zjednodušeného řádkového filtru
    /// </summary>
    public interface ITagItemOwner
    {
        /// <summary>
        /// Prvek přidá svoje Tagy do společné Dictionary
        /// </summary>
        /// <param name="tagDict"></param>
        void PrepareSummaryDict(Dictionary<string, TagItem> tagDict);
        /// <summary>
        /// Prvek vrátí true, pokud jeho Tagy vyhovují zadaným (uživatelem zvoleným) Tagům.
        /// </summary>
        /// <param name="tagFilter"></param>
        bool FilterByTagValues(TagItem[] tagFilter);
    }
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
        /// Reference na tabulku, která je vlastníkem this objektu
        /// </summary>
        Table Table { get; set; }
        /// <summary>
        /// Přidělené ID
        /// </summary>
        int Id { get; set; }
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
    /// Definice objektu, který bude filtrovat řádky
    /// </summary>
    public interface ITableFilter
    {
        /// <summary>
        /// Jméno filtru
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Filtrování řádku. Vstupem je řádek, výstupem je true = řádek je viditelný / false = neviditelný
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        bool Filter(Row row);
    }
    #endregion
    #region ItemSize : řízení jednorozměrné velikosti datového prvku, bez jeho pozicování ve vizuální sekvenci
    /// <summary>
    /// <see cref="ItemSizeInt"/> : řízení jednorozměrné velikosti datového prvku, bez jeho pozicování ve vizuální sekvenci.
    /// Instance této třídy má být umístěna v datovém prvku, nikoli v prvku vizuálním,
    /// protože nese logické údaje (šířku nebo výšku) o jednom prvku - ale bez závislosti 
    /// na jeho aktuálním pořadí v sekvenci a jeho fyzické pozici mezi ostatními.
    /// </summary>
    public class ItemSizeInt : ItemSize<Int32>
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ItemSizeInt()
            : base()
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public ItemSizeInt(ItemSizeInt parent)
            : base(parent)
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ItemSizeInt(Int32? sizeMinimum, Int32? sizeDefault, Int32? sizeMaximum)
            : base(sizeMinimum, sizeDefault, sizeMaximum)
        { }
        #endregion
        #region Order
        /// <summary>
        /// Vizuální pořadí; -1 = neviditelné (velikost není kladná)
        /// </summary>
        public int Order { get; set; }
        #endregion
        #region Overrides
        /// <summary>
        /// Metoda vrací danou velikost zarovnanou do platných mezí (dané parametry).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="sizeMinimum"></param>
        /// <param name="sizeMaximum"></param>
        /// <returns></returns>
        protected override int AlignSize(int size, int sizeMinimum, int sizeMaximum)
        {
            return (size < sizeMinimum ? sizeMinimum : (size > sizeMaximum ? sizeMaximum : size));
        }
        /// <summary>
        /// Vrací minimální hodnotu, použije se když ani Parent nemá hodnotu definovanou
        /// </summary>
        protected override int SizeMinimumDefault { get { return 5; } }
        /// <summary>
        /// Vrací maximální hodnotu, použije se když ani Parent nemá hodnotu definovanou
        /// </summary>
        protected override int SizeMaximumDefault { get { return 4000; } }
        /// <summary>
        /// Vrací implicitní hodnotu, použije se když ani Parent nemá hodnotu definovanou
        /// </summary>
        protected override int SizeDefaultDefault { get { return 100; } }
        /// <summary>
        /// Výchozí hodnoty
        /// </summary>
        protected override void Clear()
        {
            base.Clear();
            this.Order = -1;
        }
        #endregion
        #region Implicitní konverze z/na int, porovnání
        /// <summary>
        /// Implicitní konverze z <see cref="Int32"/> na <see cref="ItemSizeInt"/>.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ItemSizeInt(Int32 value) { return new ItemSizeInt(value); }
        /// <summary>
        /// Implicitní konverze z <see cref="ItemSizeInt"/> na <see cref="Int32"/>.
        /// Pokud je na vstupu <see cref="ItemSizeInt"/> = null, pak na výstupu je 0.
        /// </summary>
        /// <param name="itemSize"></param>
        public static implicit operator Int32(ItemSizeInt itemSize) { return (itemSize != null ? itemSize.Size.Value : 0); }
        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Size.Value;
        }
        /// <summary>
        /// Equals() - pro použití GID v Hashtabulkách
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is GId)) return false;
            return (ItemSizeInt._IsEqual(this, (ItemSizeInt)obj));
        }
        /// <summary>
        /// Porovnání dvou instancí této struktury, zda obsahují shodná data
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool _IsEqual(ItemSizeInt a, ItemSizeInt b)
        {
            Int32 av = (Int32)a;
            Int32 bv = (Int32)b;
            return (av == bv);
        }
        #endregion
    }
    /// <summary>
    /// <see cref="ItemSize{T}"/> : řízení jednorozměrné velikosti prvku, bez jeho pozicování v sekvenci.
    /// Instance této třídy má být umístěna v datovém prvku, nikoli v prvku vizuálním,
    /// protože nese logické údaje (šířku nebo výšku) o jednom prvku - ale bez závislosti 
    /// na jeho aktuálním pořadí v sekvenci a jeho fyzické pozici mezi ostatními.
    /// </summary>
    public abstract class ItemSize<T> where T : struct
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ItemSize()
        {
            this.Clear();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public ItemSize(ItemSize<T> parent)
            : this()
        {
            this.Parent = parent;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ItemSize(T? sizeMinimum, T? sizeDefault, T? sizeMaximum)
            : this()
        {
            this.SizeMinimum = sizeMinimum;
            this.SizeDefault = sizeDefault;
            this.SizeMaximum = sizeMaximum;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            T size = this.Size.Value;
            T sizeMinimum = this.SizeMinimum.Value;
            T sizeMaximum = this.SizeMaximum.Value;
            return "Size<" + typeof(T).Name + ">: " + size.ToString() + "; Range: {" + sizeMinimum.ToString() + " ÷ " + sizeMaximum.ToString() + "}";
        }
        #endregion
        #region Data
        /// <summary>
        /// Nositel hodnot, které zde nejsou explicitně zadány. 
        /// Může být null.
        /// </summary>
        public ItemSize<T> Parent { get; set; }
        /// <summary>
        /// true pokud je zadán parent
        /// </summary>
        protected bool HasParent { get { return (this.Parent != null); } }
        /// <summary>
        /// Aktuální velikost.
        /// Při čtení: hodnota nikdy není null, obsahuje velikost podle všech pravidel.
        /// Při zápisu: lze zapsat hodnotu null, pak se bude číst defaultní hodnota (zdejší nebo z parenta).
        /// </summary>
        public T? Size
        {
            get
            {
                T size = (this._Size.HasValue ? this._Size.Value : (this.HasParent ? this.Parent.Size.Value : this.SizeDefault.Value));
                return this.AlignSize(size);
            }
            set
            {
                this._Size = value;
            }
        }
        private T? _Size;
        /// <summary>
        /// Metoda vrací platnou velikost tohoto prvku.
        /// Tuto metodu volá vizuální prvek (<see cref="GTable"/>, <see cref="GRow"/> atd), který může / nemusí evidovat velikost tohoto prvku u sebe.
        /// Předává jako parametr tuto "vizuální" velikost, která může být null (tzn. uživatel dosud nedefinoval velikost prvku).
        /// Tato metoda vrací reálnou platnou velikost, vycházející z velikosti zadané (parametr), nebo ze zdejší (<see cref="Size"/>),
        /// vždy zarovnané do platného rozmezí <see cref="SizeMinimum"/> ÷ <see cref="SizeMaximum"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public T GetSize(T? size = null)
        {
            if (size.HasValue)
                return this.AlignSize(size.Value);
            return this.Size.Value;
        }
        /// <summary>
        /// Metoda vrací danou velikost zarovnanou do platných mezí <see cref="SizeMinimum"/> ÷ <see cref="SizeMaximum"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public T AlignSize(T size)
        {
            T sizeMinimum = this.SizeMinimum.Value;
            T sizeMaximum = this.SizeMaximum.Value;
            return this.AlignSize(size, sizeMinimum, sizeMaximum);
        }
        /// <summary>
        /// Minimální velikost prvku.
        /// Při čtení: vždy má hodnotu.
        /// Při zápisu: lze vložit null, tím se jako hodnota této property bude brát odpovídající hodnota z parenta.
        /// </summary>
        public T? SizeMinimum
        {
            get
            {
                if (this._SizeMinimum.HasValue) return this._SizeMinimum.Value;
                if (this.HasParent) return Parent.SizeMinimum;
                return this.SizeMinimumDefault;
            }
            set
            {
                this._SizeMinimum = value;
            }
        }
        private T? _SizeMinimum;
        /// <summary>
        /// Maximální velikost prvku.
        /// Při čtení: vždy má hodnotu.
        /// Při zápisu: lze vložit null, tím se jako hodnota této property bude brát odpovídající hodnota z parenta.
        /// </summary>
        public T? SizeMaximum
        {
            get
            {
                if (this._SizeMaximum.HasValue) return this._SizeMaximum.Value;
                if (this.HasParent) return Parent.SizeMaximum;
                return this.SizeMaximumDefault;
            }
            set
            {
                this._SizeMaximum = value;
            }
        }
        private T? _SizeMaximum;
        /// <summary>
        /// Implicitní velikost prvku.
        /// Při čtení: vždy má hodnotu.
        /// Při zápisu: lze vložit null, tím se jako hodnota této property bude brát odpovídající hodnota z parenta.
        /// </summary>
        public T? SizeDefault
        {
            get
            {
                if (this._SizeDefault.HasValue) return this._SizeDefault.Value;
                if (this.HasParent) return Parent.SizeDefault;
                return SizeDefaultDefault;
            }
            set
            {
                this._SizeDefault = value;
            }
        }
        private T? _SizeDefault;
        /// <summary>
        /// true pokud prvek je viditelný (dáno kódem, nikoli fitry atd). Default = true
        /// </summary>
        public bool Visible { get { return this._Visible; } set { this._Visible = value; } }
        private bool _Visible = true;
        /// <summary>
        /// true pokud tento prvek má být použit jako "guma" při změně rozměru hostitelského prvku tak, aby kolekce prvků obsadila celý prostor.
        /// Na true se nastavuje typicky u "hlavního" sloupce grafové tabulky.
        /// Při čtení: vždy má hodnotu.
        /// Při zápisu: lze vložit null, tím se jako hodnota této property bude brát odpovídající hodnota z parenta.
        /// </summary>
        public bool? AutoSize
        {
            get
            {
                if (this._AutoSize.HasValue) return this._AutoSize.Value;
                if (this.HasParent) return Parent.AutoSize;
                return AutoSizeDefault;
            }
            set
            {
                this._AutoSize = value;
            }
        }
        private bool? _AutoSize;
        /// <summary>
        /// true pokud uživatel může změnit velikost tohoto prvku. Default = true.
        /// Při čtení: vždy má hodnotu.
        /// Při zápisu: lze vložit null, tím se jako hodnota této property bude brát odpovídající hodnota z parenta.
        /// </summary>
        public bool? ResizeEnabled
        {
            get
            {
                if (this._ResizeEnabled.HasValue) return this._ResizeEnabled.Value;
                if (this.HasParent) return Parent.ResizeEnabled;
                return ResizeEnabledDefault;
            }
            set
            {
                this._ResizeEnabled = value;
            }
        }
        private bool? _ResizeEnabled;
        /// <summary>
        /// true pokud uživatel může přemístit tento prvek někam jinam. Default = true.
        /// Při čtení: vždy má hodnotu.
        /// Při zápisu: lze vložit null, tím se jako hodnota této property bude brát odpovídající hodnota z parenta.
        /// </summary>
        public bool? ReOrderEnabled
        {
            get
            {
                if (this._ReOrderEnabled.HasValue) return this._ReOrderEnabled.Value;
                if (this.HasParent) return Parent.ReOrderEnabled;
                return ReOrderEnabledDefault;
            }
            set
            {
                this._ReOrderEnabled = value;
            }
        }
        private bool? _ReOrderEnabled;
        #endregion
        #region K přepsání na potomkovi
        /// <summary>
        /// Metoda vrací danou velikost zarovnanou do platných mezí (dané parametry).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="sizeMinimum"></param>
        /// <param name="sizeMaximum"></param>
        /// <returns></returns>
        protected abstract T AlignSize(T size , T sizeMinimum, T sizeMaximum);
        /// <summary>
        /// Vrací minimální hodnotu, použije se když ani Parent nemá hodnotu definovanou
        /// </summary>
        protected abstract T SizeMinimumDefault { get; }
        /// <summary>
        /// Vrací maximální hodnotu, použije se když ani Parent nemá hodnotu definovanou
        /// </summary>
        protected abstract T SizeMaximumDefault { get; }
        /// <summary>
        /// Vrací implicitní hodnotu, použije se když ani Parent nemá hodnotu definovanou
        /// </summary>
        protected abstract T SizeDefaultDefault { get; }
        /// <summary>
        /// Výchozí hodnota pro <see cref="AutoSize"/>
        /// </summary>
        protected virtual bool AutoSizeDefault { get { return false; } }
        /// <summary>
        /// Výchozí hodnota pro <see cref="ResizeEnabled"/>
        /// </summary>
        protected virtual bool ResizeEnabledDefault { get { return true; } }
        /// <summary>
        /// Výchozí hodnota pro <see cref="ReOrderEnabled"/>
        /// </summary>
        protected virtual bool ReOrderEnabledDefault { get { return true; } }
        /// <summary>
        /// Je voláno na začátku každého konstruktoru!
        /// </summary>
        protected virtual void Clear() { }
        #endregion
    }
    #endregion
    #region Enums
    /// <summary>
    /// Jaký druh údaje je obsažen ve sloupci
    /// </summary>
    public enum ColumnContentType
    {
        /// <summary>
        /// Žádná data
        /// </summary>
        None,
        /// <summary>
        /// Běžná uživatelská data. 
        /// Tento sloupec může být viditelný.
        /// </summary>
        UserData,
        /// <summary>
        /// Zobrazovaná data záznamu, který je navázán ve vztahu. 
        /// Tento sloupec může být viditelný.
        /// Tento sloupec je vykreslován podtržený.
        /// </summary>
        RelationRecordData,
        /// <summary>
        /// Časový graf s časovou synchronizací do všech okolních Gridů.
        /// Tento sloupec může být viditelný.
        /// </summary>
        TimeGraphSynchronized,
        /// <summary>
        /// Časový graf bez časové synchronizace do všech okolních Gridů.
        /// Tento časový graf se synchronizuje pouze do sousedních tabulek v rámci jednoho Gridu.
        /// Tento sloupec může být viditelný.
        /// </summary>
        TimeGraphStandalone,
        /// <summary>
        /// Identifikátor záznamu, GId
        /// </summary>
        RecordGId,
        /// <summary>
        /// Číslo záznamu celého řádku.
        /// Tento sloupec se nikdy nezobrazuje.
        /// </summary>
        MasterId,
        /// <summary>
        /// Číslo položky v záznamu celého řádku.
        /// Tento sloupec se nikdy nezobrazuje.
        /// </summary>
        EntryId,
        /// <summary>
        /// Číslo vztaženého záznamu (takového, jehož typ == <see cref="RelationRecordData"/>).
        /// Tento sloupec se nikdy nezobrazuje.
        /// </summary>
        RelationRecordId,
        /// <summary>
        /// Jiná skrytá data.
        /// Tento sloupec se nikdy nezobrazuje.
        /// Typicky ID číslo řádku ze systému Green.
        /// </summary>
        HiddenData
    }
    /// <summary>
    /// Typ třídění
    /// </summary>
    public enum ItemSortType
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
    /// <summary>
    /// Typ hodnoty v tabulce (v buňce)
    /// </summary>
    public enum TableValueType
    {
        /// <summary>
        /// Není
        /// </summary>
        None,
        /// <summary>
        /// Null
        /// </summary>
        Null,
        /// <summary>
        /// Prvek, který se sám vykresluje
        /// </summary>
        IDrawItem,
        /// <summary>
        /// Interaktivní graf
        /// </summary>
        ITimeInteractiveGraph,
        /// <summary>
        /// Časový graf neinteraktivní
        /// </summary>
        ITimeGraph,
        /// <summary>
        /// Obrázek
        /// </summary>
        Image,
        /// <summary>
        /// Text
        /// </summary>
        Text,
        /// <summary>
        /// Textová hodnota, obsahující navíc ID vztaženého záznamu.
        /// Value je typu <see cref="GuiIdText"/>.
        /// </summary>
        TextRelation
    }
    #endregion
}
