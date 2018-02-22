using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Djs.Common.Components;
using System.Drawing;

namespace Djs.Common.Data.New
{
    // Tento soubor obsahuje datové prvky pro tabulku s daty. 
    // To jest: Tabulka, Sloupec, Řádek, Buňka. 
    // A dále interface pro vizualizaci tabulky, které datová tabulka implementuje, a podporu pro třídění.

    #region Table
    /// <summary>
    /// Table : jedna tabulka s dat (sada Column + Row)
    /// </summary>
    public class Table : IVisualMember, IContentValidity
    {
        #region Constructor, Initialisation
        /// <summary>
        /// Konstructor
        /// </summary>
        public Table()
        {
            this._TableInit();
        }
        /// <summary>
        /// Konstructor
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
        }
        /// <summary>
        /// Název tabulky, podle něj lze hledat. jde o klíčové slovo, nikoli popisek (Caption)
        /// </summary>
        public string TableName { get { return this._TableName; } set { this._TableName = value; } }
        private string _TableName;
        #endregion
        #region GTable - Link, Invalidate Graphic data
        /// <summary>
        /// Reference na vizuální tabulku (GTable), může být null
        /// </summary>
        public Components.Grid.GTable GTable { get { return this._GTable; } internal set { this._GTableLink(value); } }
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
        #region Columns - Add/Remove public, protected and private methods
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
        #region Rows - Add/Remove public, protected and private methods
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
        #region Sorting rows
        /// <summary>
        /// Setřídí dodaný seznam řádků podle dodaného sloupce a směru
        /// </summary>
        /// <param name="rowList"></param>
        /// <param name="sortByColumn"></param>
        /// <param name="sortType"></param>
        /// <returns></returns>
        public bool SortRows(List<TableSortRowsItem> rowList, Column sortByColumn, TableSortRowType sortType)
        {
            if (sortByColumn == null) return false;
            if (!sortByColumn.SortingEnabled) return false;

            int index = sortByColumn.ColumnId;
            bool hasComparator = (sortByColumn.ValueComparator != null);
            if (hasComparator)
                rowList.ForEachItem(r => { r.Value = r.Row[index]; });
            else
                rowList.ForEachItem(r => { r.ValueComparable = r.Row[index] as IComparable; });

            switch (sortType)
            {
                case TableSortRowType.Ascending:
                    if (hasComparator)
                        rowList.Sort((a, b) => sortByColumn.ValueComparator(a.Value, b.Value));
                    else
                        rowList.Sort((a, b) => SortRowsCompare(a.ValueComparable, b.ValueComparable));
                    return true;

                case TableSortRowType.Descending:
                    if (hasComparator)
                        rowList.Sort((a, b) => sortByColumn.ValueComparator(b.Value, a.Value));
                    else
                        rowList.Sort((a, b) => SortRowsCompare(b.ValueComparable, a.ValueComparable));
                    return true;

            }
            return false;
        }
        /// <summary>
        /// Komparátor
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
        #endregion
        #region GUI properties
        public int? ColumnHeight { get; set; }
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
        #region Konstructor, základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Column()
        {
            this._ColumnId = -1;
            this._Visible = true;
            this._SortingEnabled = true;
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
        /// <param name="dTable"></param>
        void ITableMember.AttachToTable(Table dTable, int id)
        {
            this._ColumnId = id;
            this._ColumnOrder = id;
            this.Table = dTable;
        }
        /// <summary>
        /// Odpojí this sloupec z dané tabulky.
        /// Je voláno z tabulky, v eventu ItemRemove kolekce Columns.
        /// </summary>
        void ITableMember.DetachFromTable()
        {
            this._ColumnId = -1;
            this.Table = null;
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
        /// Jednotlivé sloupce nemusí mít hodnoty ColumnOrder v nepřerušovaném pořadí.
        /// Po napojení sloupce do tabulky je do ColumnOrder vepsána hodnota = ColumnID, takže nový sloupec se zařadí vždy na konec.
        /// </summary>
        public int ColumnOrder { get { return this._ColumnOrder; } set { this._ColumnOrder = value; } } private int _ColumnOrder;
        /// <summary>
        /// Datový typ obsahu sloupce. Null = obecná data
        /// </summary>
        public Type DataType { get { return this._DataType; } set { this._DataType = value; } } private Type _DataType;
        /// <summary>
        /// Formátovací string pro data zobrazovaná v tomto sloupci.
        /// </summary>
        public string FormatString { get { return this._FormatString; } set { this._FormatString = value; } } private string _FormatString;
        /// <summary>
        /// true pokud se pro sloupec má zobrazit časová osa v záhlaví
        /// </summary>
        public bool UseTimeAxis { get { return this._UseTimeAxis; } set { this._UseTimeAxis = value; } } private bool _UseTimeAxis;
        /// <summary>
        /// true pro viditelný sloupec (default), false for skrytý
        /// </summary>
        public bool Visible { get { return this._Visible; } set { this._Visible = value; } } private bool _Visible;
        /// <summary>
        /// Výchozí šířka sloupce, v pixelech. 
        /// Má význam pouze v první tabulce použité v Gridu, další tabulky přebírají šířku z první tabulky.
        /// </summary>
        public int? Width { get { return this._Width; } set { this._Width = value; } } private int? _Width;
        /// <summary>
        /// Platné rozmezí šířky sloupce.
        /// Má význam pouze v první tabulce použité v Gridu, další tabulky přebírají hodnotu z první tabulky.
        /// </summary>
        public Int32Range WidthRange { get { return this._WidthRange; } set { this._WidthRange = value; } } private Int32Range _WidthRange;
        #endregion
        #region Sorting
        /// <summary>
        /// Komparátor pro dvě hodnoty v tomto sloupci, pro třídění
        /// </summary>
        public Func<object, object, int> ValueComparator;
        /// <summary>
        /// true pokud je povoleno třídit podle tohoto sloupce.
        /// false = nemožno.
        /// </summary>
        public bool SortingEnabled { get { return this._SortingEnabled; } set { this._SortingEnabled = value; } } private bool _SortingEnabled;
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
        Int32? IVisualMember.Height { get { return (this.HasTable ? this.Table.ColumnHeight : null); } }
        #endregion
    }
    #endregion
    #region Row
    /// <summary>
    /// Row : informace o jednom řádku tabulky
    /// </summary>
    public class Row : ITableMember, IVisualMember, ISequenceLayout, IContentValidity
    {
        #region Konstruktor, základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Row()
        {
            this._RowId = -1;
            this._CellDict = new Dictionary<int, Cell>();
            this._Visible = true;
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
        /// <param name="dTable"></param>
        void ITableMember.AttachToTable(Table dTable, int id)
        {
            this._RowId = id;
            this.Table = dTable;
        }
        /// <summary>
        /// Odpojí this řádek z dané tabulky.
        /// Je voláno z tabulky, v eventu ItemRemove kolekce Rows.
        /// </summary>
        void ITableMember.DetachFromTable()
        {
            this._RowId = -1;
            this.Table = null;
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
        /// true pro viditelný sloupec (default), false for skrytý
        /// </summary>
        public bool Visible { get { return this._Visible; } set { this._Visible = value; } } private bool _Visible;
        public Int32Range HeightRange { get; set; }
        public Int32? Height { get; set; }
        #region Práce s velikostí řádků (výškou): spolupráce explicitně zadané datové hodnoty, hodnoty interaktivní a rozmezí platných hodnot
        protected void SetLayoutHeight(int height)
        {
            Int32Range sizeRange = this.HeightRange;
        }
        protected int GetHeight(Int32? layoutSize)
        {

        }

        #endregion
        #endregion
        #region Visual style
        /// <summary>
        /// Všechny vizuální vlastnosti dat v tomto řádku (nikoli buňky ani tabulky).
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
        Int32? IVisualMember.Width { get { return null; } }
        Int32? IVisualMember.Height { get { return (this.HasTable ? this.Table.ColumnHeight : null); } }
        #endregion
        #region ISequenceLayout = pořadí, počáteční pixel, velikost, následující pixel
        /// <summary>
        /// Pozice, kde prvek začíná.
        /// Interface ISequenceLayout tuto hodnotu setuje v případě, kdy se layout těchto prvků změní (změna prvků nebo jejich velikosti).
        /// </summary>
        int ISequenceLayout.Begin { get { return _ISequenceLayoutBegin; } set { _ISequenceLayoutBegin = value; } } private int _ISequenceLayoutBegin;
        /// <summary>
        /// Velikost prvku v pixelech (šířka sloupce, výška řádku, výška tabulky). 
        /// Lze ji setovat, protože prvky lze pomocí splitterů zvětšovat / zmenšovat.
        /// Aplikační logika prvku musí zabránit vložení neplatné hodnoty (reálně se uloží hodnota platná).
        /// </summary>
        int ISequenceLayout.Size
        {
            get
            {
                return this.GetHeight(this._ISequenceLayoutSize);
            }
            set
            {
                this.SetLayoutHeight(value);
                this._ISequenceLayoutSize = this.Height.Value;
            }
        }
        private Int32? _ISequenceLayoutSize;
        /// <summary>
        /// Pozice, kde za tímto prvkem začíná následující prvek. 
        /// Velikost prvku = (End - Begin) = počet pixelů, na které se zobrazuje tento prvek.
        /// Interface ISequenceLayout tuto hodnotu nesetuje, pouze ji čte.
        /// </summary>
        int ISequenceLayout.End { get { return this._ISequenceLayoutBegin + (this.Visible ? this._ISequenceLayoutSize : 0); } }
        /// <summary>
        /// Pořadí tohoto prvku v sekvenci ostatních prvků.
        /// Nemusí to být kontinuální řada, může obsahovat díry.
        /// Kolekce se třídí prostým Sort(podle Order ASC).
        /// </summary>
        int ISequenceLayout.Order { get { return _ISequenceLayoutOrder; } set { _ISequenceLayoutOrder = value; } } private int _ISequenceLayoutOrder;
        #endregion
        #region IContentValidity
        bool IContentValidity.DataIsValid { get { return _RowDataIsValid; } set { _RowDataIsValid = value; } } private bool _RowDataIsValid;
        bool IContentValidity.RowLayoutIsValid { get { return _RowLayoutIsValid; } set { _RowLayoutIsValid = value; } } private bool _RowLayoutIsValid;
        bool IContentValidity.ColumnLayoutIsValid { get { return _ColumnLayoutIsValid; } set { _ColumnLayoutIsValid = value; } } private bool _ColumnLayoutIsValid;
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
        /// <summary>
        /// Pořadí tohoto prvku v sekvenci ostatních prvků.
        /// Nemusí to být kontinuální řada, může obsahovat díry.
        /// Kolekce se třídí prostým Sort(podle Order ASC).
        /// </summary>
        int Order { get; set; }
    }
    #endregion
    #region Podpora třídění
    /// <summary>
    /// Třída umožňující třídění řádků podle hodnoty ValueComparable
    /// </summary>
    public class TableSortRowsItem
    {
        public TableSortRowsItem(Row row)
        {
            this.Row = row;
        }
        public TableSortRowsItem(Row row, object userData)
        {
            this.Row = row;
            this.UserData = userData;
        }
        /// <summary>
        /// Data řádku
        /// </summary>
        public Row Row { get; private set; }
        /// <summary>
        /// Libovolná další data
        /// </summary>
        public object UserData { get; private set; }
        /// <summary>
        /// Hodnota
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Hodnota pro komparaci
        /// </summary>
        public IComparable ValueComparable { get; set; }
    }
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
}
