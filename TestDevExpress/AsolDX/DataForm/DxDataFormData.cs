// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm.Data
{
    #region DataFormRows + DataFormRow : Kolekce a Data řádků
    /// <summary>
    /// Pole řádků
    /// </summary>
    public class DataFormRows : IList<DataFormRow>
	{
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormRows(DxDataFormPanel dataForm)
        {
            __DataForm = dataForm;
            _InitRows();
        }
        /// <summary>
        /// Panel dataformu
        /// </summary>
        internal DxDataFormPanel DataForm { get { return __DataForm; } } private DxDataFormPanel __DataForm;
        #endregion
        #region Jednotlivé řádky s daty
        /// <summary>
        /// Inicializace řádků
        /// </summary>
        private void _InitRows()
        {
            __Rows = new ChildItems<DataFormRows, DataFormRow>(this);
            __Rows.CollectionChanged += _RowsChanged;
        }
        /// <summary>
        /// Po změně řádků
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowsChanged(object sender, EventArgs e)
        {
            _RunCollectionChanged();
        }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        public event EventHandler CollectionChanged;
        /// <summary>
        /// Metoda volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollectionChanged(EventArgs e) { }
        /// <summary>
        /// Zavolá metody <see cref="OnCollectionChanged(EventArgs)"/> a event <see cref="CollectionChanged"/>.
        /// </summary>
        private void _RunCollectionChanged()
        {
            InvalidateContentDesignSize();
            var args = EventArgs.Empty;
            OnCollectionChanged(args);
            CollectionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Pole řádků
        /// </summary>
        private ChildItems<DataFormRows, DataFormRow> __Rows;
        #endregion
        #region ContentDesignSize
        /// <summary>
        /// Designová velikost celého pole řádků (ésumární prostor všech viditelných řádků).
        /// </summary>
        public WinDraw.Size ContentDesignSize { get { CheckContentDesignSize(); return __ContentDesignSize.Value; } }
        /// <summary>
        /// Designová velikost jednoho řádku daná definicí layoutu.
        /// Po setování hodnoty je možno číst <see cref="ContentDesignSize"/>
        /// </summary>
        public WinDraw.Size? OneRowDesignSize
        {
            get { return __OneRowDesignSize; }
            set 
            {
                var oldSize = __OneRowDesignSize;
                __OneRowDesignSize = value; 
                var newSize = __OneRowDesignSize;
                if (oldSize != newSize)
                    InvalidateContentDesignSize();
            }
        }
        /// <summary>
        /// Do všech viditelných řádků vepíše jejich designové souřadnice jako kontinuální pozici o dané velikosti <paramref name="oneRowDesignSize"/>
        /// </summary>
        /// <param name="oneRowDesignSize"></param>
        public void PrepareRowDesignBounds(WinDraw.Size oneRowDesignSize)
        {
            __OneRowDesignSize = oneRowDesignSize;
            __ContentDesignSize = CalculateContentDesignSize();
        }
        /// <summary>
        /// Pokud je potřeba, přepočte pozice jednotlivých řádků
        /// </summary>
        /// <param name="force"></param>
        protected void CheckContentDesignSize(bool force = false)
        {
            if (!__ContentDesignSize.HasValue)
                __ContentDesignSize = CalculateContentDesignSize();
        }
        /// <summary>
        /// Určí umístění všec viditelných jednotlivých řádků podle aktuální velikosti designu jednoho řádku <see cref="OneRowDesignSize"/>
        /// </summary>
        /// <returns></returns>
        protected WinDraw.Size CalculateContentDesignSize()
        {
            int left = 0;
            int top = 0;
            int right = 0;
            int bottom = 0;
            var oneRowDesignSize = OneRowDesignSize;
            foreach (var row in __Rows)
                row.PrepareRowDesignBounds(oneRowDesignSize, ref left, ref top, ref right, ref bottom);
            return new WinDraw.Size(right, bottom);
        }
        /// <summary>
        /// Po změně počtu řádků nebo velikosti designu se invaliduje celková velikost prostoru řádků <see cref="ContentDesignSize"/>
        /// </summary>
        protected virtual void InvalidateContentDesignSize()
        {
            __ContentDesignSize = null;
        }
        /// <summary>
        /// Velikost designu jednoho řádku
        /// </summary>
        private WinDraw.Size? __OneRowDesignSize;
        /// <summary>
        /// Velikost designu celého seznamu
        /// </summary>
        private WinDraw.Size? __ContentDesignSize;
        #endregion
        #region Podmnožina řádků podle viditelných pixelů
        /// <summary>
        /// Metoda vytvoří a vrátí seznam řádků, které se alespoň částečně nacházejí v zadaném rozmezí.
        /// Pokud rozmezí bude null, vrátí se všechny řádky.
        /// </summary>
        /// <param name="designPixels"></param>
        /// <returns></returns>
        internal DataFormRow[] GetRowsInDesignPixels(ref Int32Range designPixels)
        {
            // Zkratka 1:
            if (this.__Rows.Count == 0)
            {
                designPixels = new Int32Range(0, 0);
                return new DataFormRow[0];
            }
            CheckContentDesignSize();

            // Zkratka 2:
            if (designPixels is null)
            {
                DxComponent.LogAddLine($"DataFormRows.GetRowsInDesignPixels(): AllRows");
                designPixels = new Int32Range(0, ContentDesignSize.Height);
                return __Rows.ToArray();
            }

            // Najdeme konkrétní řádky vyhovující svojí pozicí zadanému rozmezí:
            var result = new List<DataFormRow>();
            bool canModifyRange = designPixels.IsVariable;
            int firstPixel = designPixels.Begin;
            int lastPixel = designPixels.End;
            foreach (var row in __Rows)
            {
                var bounds = row.RowDesignBounds;
                if (bounds.Bottom < firstPixel) continue;              // Tento řádek je před požadovaným rozsahem (=končí dříve) => přeskočíme, a budeme hledat další
                if (bounds.Top > lastPixel) break;                     // Tento řádek je až za požadovaným rozsahem (=začíná po něm) => rovnou končíme a další nehledáme (a to i pro Invisible: i jakýkoli navazující row s Visible = true bude mít (Top > lastPixel) !)
                if (row.IsVisible)
                {
                    result.Add(row);
                    if (canModifyRange)
                    {   // Můžeme rozšířit design interval podle reálně přidaných řádků:
                        if (bounds.Top < designPixels.Begin) designPixels.Begin = bounds.Top;
                        if (bounds.Bottom > designPixels.End) designPixels.End = bounds.Bottom;
                    }
                }
            }
            DxComponent.LogAddLine($"DataFormRows.GetRowsInDesignPixels(): DesignPixels: {designPixels}; Rows.Count: {result.Count}");

            return result.ToArray();
        }
        #endregion
        #region AddRange a Store
        /// <summary>
        /// AddRange
        /// </summary>
        /// <param name="rows"></param>
        public void AddRange(IEnumerable<DataFormRow> rows)
        {
            __Rows.AddRange(rows);
        }
        /// <summary>
        /// Store
        /// </summary>
        /// <param name="rows"></param>
        public void Store(IEnumerable<DataFormRow> rows)
        {
            __Rows.Clear();
            __Rows.AddRange(rows);
        }
        #endregion
        #region IList
        /// <summary>
        /// Count
        /// </summary>
        public int Count => ((ICollection<DataFormRow>)__Rows).Count;
        /// <summary>
        /// IsReadOnly
        /// </summary>
        public bool IsReadOnly => ((ICollection<DataFormRow>)__Rows).IsReadOnly;
        /// <summary>
        /// this
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DataFormRow this[int index] { get => ((IList<DataFormRow>)__Rows)[index]; set => ((IList<DataFormRow>)__Rows)[index] = value; }
        /// <summary>
        /// IndexOf
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(DataFormRow item)
        {
            return ((IList<DataFormRow>)__Rows).IndexOf(item);
        }
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, DataFormRow item)
        {
            ((IList<DataFormRow>)__Rows).Insert(index, item);
        }
        /// <summary>
        /// RemoveAt
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            ((IList<DataFormRow>)__Rows).RemoveAt(index);
        }
        /// <summary>
        /// Add
        /// </summary>
        /// <param name="item"></param>
        public void Add(DataFormRow item)
        {
            ((ICollection<DataFormRow>)__Rows).Add(item);
        }
        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            ((ICollection<DataFormRow>)__Rows).Clear();
        }
        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(DataFormRow item)
        {
            return ((ICollection<DataFormRow>)__Rows).Contains(item);
        }
        /// <summary>
        /// CopyTo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(DataFormRow[] array, int arrayIndex)
        {
            ((ICollection<DataFormRow>)__Rows).CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(DataFormRow item)
        {
            return ((ICollection<DataFormRow>)__Rows).Remove(item);
        }
        /// <summary>
        /// GetEnumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<DataFormRow> GetEnumerator()
        {
            return ((IEnumerable<DataFormRow>)__Rows).GetEnumerator();
        }
        /// <summary>
        /// IEnumerable.GetEnumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)__Rows).GetEnumerator();
        }
        #endregion
    }
    /// <summary>
    /// Jeden každý řádek
    /// </summary>
    public class DataFormRow : IChildOfParent<DataFormRows>
    {
        #region Konstruktor a základní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormRow()
        {
            __Content = new DataContent();
            IsVisible = true;
        }
        /// <summary>
        /// Parent
        /// </summary>
        DataFormRows IChildOfParent<DataFormRows>.Parent { get { return __Parent; } set { __Parent = value; } } private DataFormRows __Parent;
        /// <summary>
        /// Parent = kolekce řádků
        /// </summary>
        protected DataFormRows Parent { get { return __Parent; } }
        /// <summary>
        /// Panel dataformu
        /// </summary>
        internal DxDataFormPanel DataForm { get { return __Parent?.DataForm; } }
        #endregion

        /// <summary>
        /// ID řádku, nemění se po dobu jeho života. Lze setovat jen jedenkrát. Při pokusu o další setování jiné hodnoty dojde k chybě.
        /// </summary>
        public int RowId
        {
            get { return __RowId ?? 0; }
            set
            {
                if (!__RowId.HasValue) __RowId = value;              // První přiřazení
                else if (__RowId.HasValue && __RowId.Value != value)
                    throw new InvalidOperationException($"Cannot set 'rowId'={value} to row with assigned 'RowId'={__RowId}.");
            }
        }
        private int? __RowId;
        /// <summary>
        /// Obsah řádků: obsahuje sloupce i jejich datové a popisné hodnoty.
        /// Klíčem musí být název sloupce + dvojtečka + název vlastnosti. Názvy vlastností jsou v konstantách <see cref="DxDataFormDef"/>
        /// </summary>
        public DataContent Content { get { return __Content; } } private DataContent __Content;
        /// <summary>
        /// Řádek je viditelný?
        /// </summary>
        public bool IsVisible { get; set; }

        #region RowDesignBounds : designové souřadnice řádku v kolekci všech řádků - určení, property
        /// <summary>
        /// Do tohoto řádku, pokud je viditelný, vepíše jeho designové souřadnice jako kontinuální pozici o dané velikosti <paramref name="oneRowDesignSize"/>
        /// </summary>
        /// <param name="oneRowDesignSize"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public void PrepareRowDesignBounds(WinDraw.Size? oneRowDesignSize, ref int left, ref int top, ref int right, ref int bottom)
        {
            int width = oneRowDesignSize?.Width ?? 100;
            int height = oneRowDesignSize?.Height ?? 20;
            var designBounds = new WinDraw.Rectangle(left, top, width, height);
            RowDesignBounds = designBounds;

            if (this.IsVisible)
            {   // Pouze viditelný řádek ovlivňuje souřadnice pro další řádek (top) a celkovou velikost prostoru řádků (right, bottom):
                top = designBounds.Bottom;
                if (right < designBounds.Right) right = designBounds.Right;
                if (bottom < designBounds.Bottom) bottom = designBounds.Bottom;
            }
        }
        /// <summary>
        /// Souřadnice tohoto řádku v designových pixelech
        /// </summary>
        public WinDraw.Rectangle RowDesignBounds { get; private set; }
        #endregion
        #region Tvorba interaktivních prvků z this řádku (layout + data řádku = IInteractiveItem)
        /// <summary>
        /// Metoda vytvoří interaktivní prvky za jednotlivé definice layoutu za tento řádek, a přidá je do předaného pole.
        /// </summary>
        /// <param name="items"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void PrepareValidInteractiveItems(List<IInteractiveItem> items)
        {
            var dataFormContent = DataForm.DataFormContent;
            var rowDesignPoint = this.RowDesignBounds.Location;
            var dataLayout = DataForm.DataFormLayout;
            items.AddRange(dataLayout.Items.Select(l => new DataFormCell(this, l)));
        }
        #endregion
    }
    #endregion
    #region DataFormLayoutSet + DataFormLayoutItem : Definice layoutu jednoho řádku (celá sada + jeden prvek). Layout je hierarchický.
    /// <summary>
    /// Sada definující layout DataFormu = jednotlivé prvky
    /// </summary>
    public class DataFormLayoutSet : IList<DataFormLayoutItem>
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormLayoutSet(DxDataFormPanel dataForm)
        {
            __DataForm = dataForm;
            _InitItems();
        }
        /// <summary>
        /// Panel dataformu
        /// </summary>
        internal DxDataFormPanel DataForm { get { return __DataForm; } } private DxDataFormPanel __DataForm;
        #endregion
        #region Jednotlivé prvky definice
        /// <summary>
        /// Pole prvků definice
        /// </summary>
        public IList<DataFormLayoutItem> Items { get { return __Items; } }
        /// <summary>
        /// Inicializace řádků
        /// </summary>
        private void _InitItems()
        {
            __Items = new ChildItems<DataFormLayoutSet, DataFormLayoutItem>(this);
            __Items.CollectionChanged += _ItemsChanged;
        }
        /// <summary>
        /// Po změně řádků
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ItemsChanged(object sender, EventArgs e)
        {
            _RunCollectionChanged();
        }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        public event EventHandler CollectionChanged;
        /// <summary>
        /// Metoda volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollectionChanged(EventArgs e) { }
        /// <summary>
        /// Zavolá metody <see cref="OnCollectionChanged(EventArgs)"/> a event <see cref="CollectionChanged"/>.
        /// </summary>
        private void _RunCollectionChanged()
        {
            this.InvalidateDesignSize();
            var args = EventArgs.Empty;
            OnCollectionChanged(args);
            CollectionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Pole prvků definice
        /// </summary>
        private ChildItems<DataFormLayoutSet, DataFormLayoutItem> __Items;
        #endregion
        #region DesignSize : celková designová velikost prvků v této definici
        /// <summary>
        /// Designová velikost panelu hostitele = prostor viditelný v rámci fyzického controlu, přepočtený na designové pixely
        /// </summary>
        public WinDraw.Size? HostDesignSize 
        {
            get { return __HostDesignSize; } 
            set 
            {
                var oldValue = __HostDesignSize;
                __HostDesignSize = value;
                var newValue = __HostDesignSize;
                if (oldValue != newValue && IsDesignSizeDependOnHostSize) { InvalidateDesignSize(); }
            }
        }
        /// <summary>
        /// Obsahuje true pokud zdejší <see cref="DesignSize"/> se může změnit poté, kdy se změní velikost <see cref="HostDesignSize"/>.
        /// Tedy když implementujeme dynamické přeskupování obsahu podle dostupného prostoru.
        /// </summary>
        public bool IsDesignSizeDependOnHostSize { get { return false; } }
        /// <summary>
        /// Okraje kolem definice jednoho řádku
        /// </summary>
        public WinForm.Padding Padding { get { return __Padding; } set { __Padding = value; InvalidateDesignSize(); } }
        /// <summary>
        /// Sumární velikost definice pro jeden řádek. Validovaná hodnota.
        /// </summary>
        public WinDraw.Size DesignSize { get { return _GetDesignSize(); } }
        /// <summary>
        /// Invaliduje velikost prostoru, vynutí tím budoucí přepočet <see cref="DesignSize"/>.
        /// </summary>
        protected void InvalidateDesignSize()
        {
            __DesignSize = null;
        }
        /// <summary>
        /// [Vypočte a] vrátí velikost definice. Přidává vlastní Padding.
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        private WinDraw.Size _GetDesignSize(bool force = false)
        {
            if (force || !__DesignSize.HasValue)
                __DesignSize = _CalculateDesignSize();
            return __DesignSize.Value;
        }
        /// <summary>
        /// Vrátí hodnotu: Sumární velikost definice pro jeden řádek.
        /// </summary>
        private WinDraw.Size _CalculateDesignSize()
        {
            if (!this.HostDesignSize.HasValue) return WinDraw.Size.Empty;

            var hostDesignSize = this.HostDesignSize.Value;
            WinDraw.Rectangle parentBounds = new WinDraw.Rectangle(0, 0, hostDesignSize.Width, hostDesignSize.Height);

            int left = 0;
            int top = 0;
            int right = 0;
            int bottom = 0;
            foreach (var item in __Items)
                item.PrepareDesignSize(parentBounds, ref left, ref top, ref right, ref bottom);

            var itemSize = new WinDraw.Size(right, bottom);
            return itemSize.Add(this.Padding);
        }
        /// <summary>
        /// Designová velikost panelu hostitele
        /// </summary>
        private WinDraw.Size? __HostDesignSize;
        /// <summary>
        /// Okraje uvnitř jednoho řádku okolo layoutu
        /// </summary>
        private WinForm.Padding __Padding;
        /// <summary>
        /// Sumární velikost definice pro jeden řádek. Úložiště.
        /// </summary>
        private WinDraw.Size? __DesignSize;
        #endregion
        #region AddRange a Store
        /// <summary>
        /// AddRange
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<DataFormLayoutItem> items)
        {
            __Items.AddRange(items);
        }
        /// <summary>
        /// Store
        /// </summary>
        /// <param name="items"></param>
        public void Store(IEnumerable<DataFormLayoutItem> items)
        {
            __Items.Clear();
            __Items.AddRange(items);
        }
        #endregion
        #region IList
        /// <summary>
        /// Count
        /// </summary>
        public int Count => ((ICollection<DataFormLayoutItem>)__Items).Count;
        /// <summary>
        /// IsReadOnly
        /// </summary>
        public bool IsReadOnly => ((ICollection<DataFormLayoutItem>)__Items).IsReadOnly;
        /// <summary>
        /// this
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DataFormLayoutItem this[int index] { get => ((IList<DataFormLayoutItem>)__Items)[index]; set => ((IList<DataFormLayoutItem>)__Items)[index] = value; }
        /// <summary>
        /// IndexOf
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(DataFormLayoutItem item)
        {
            return ((IList<DataFormLayoutItem>)__Items).IndexOf(item);
        }
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, DataFormLayoutItem item)
        {
            ((IList<DataFormLayoutItem>)__Items).Insert(index, item);
        }
        /// <summary>
        /// RemoveAt
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            ((IList<DataFormLayoutItem>)__Items).RemoveAt(index);
        }
        /// <summary>
        /// Add
        /// </summary>
        /// <param name="item"></param>
        public void Add(DataFormLayoutItem item)
        {
            ((ICollection<DataFormLayoutItem>)__Items).Add(item);
        }
        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            ((ICollection<DataFormLayoutItem>)__Items).Clear();
        }
        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(DataFormLayoutItem item)
        {
            return ((ICollection<DataFormLayoutItem>)__Items).Contains(item);
        }
        /// <summary>
        /// CopyTo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(DataFormLayoutItem[] array, int arrayIndex)
        {
            ((ICollection<DataFormLayoutItem>)__Items).CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(DataFormLayoutItem item)
        {
            return ((ICollection<DataFormLayoutItem>)__Items).Remove(item);
        }
        /// <summary>
        /// GetEnumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<DataFormLayoutItem> GetEnumerator()
        {
            return ((IEnumerable<DataFormLayoutItem>)__Items).GetEnumerator();
        }
        /// <summary>
        /// IEnumerable.GetEnumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)__Items).GetEnumerator();
        }
        #endregion
    }
    /// <summary>
    /// Definice jednoho prvku v layoutu. 
    /// Může to být container i jednotlivý prvek.
    /// Jde o ekvivalent "Column"
    /// </summary>
    public class DataFormLayoutItem : IDataFormLayoutDesignItem, IChildOfParent<DataFormLayoutSet>
    {
        #region Konstruktor a fixní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormLayoutItem()
        {
            __Content = new DataContent();
            ColumnType = DxRepositoryEditorType.TextBox;
            
            IsVisible = true;
        }
        /// <summary>
        /// Parent
        /// </summary>
        DataFormLayoutSet IChildOfParent<DataFormLayoutSet>.Parent { get { return __Parent; } set { __Parent = value; } } private DataFormLayoutSet __Parent;
        /// <summary>
        /// Panel dataformu
        /// </summary>
        internal DxDataFormPanel DataForm { get { return __Parent?.DataForm; } }
        /// <summary>
        /// Jméno tohoto sloupce. K němu se dohledají další data a případné modifikace stylu v konkrétním řádku.
        /// Interně se pracuje s <see cref="ColumnId"/> (int), konverze řeší <see cref="DataForm"/>.
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// Druh vstupního prvku
        /// </summary>
        public DxRepositoryEditorType ColumnType { get; set; }
        /// <summary>
        /// Souřadnice definované pomocí vzdáleností / rozměrů, a tedy libovolně ukotvené.
        /// Viz metoda <see cref="RectangleExt.GetBounds(WinDraw.Rectangle)"/>
        /// </summary>
        public RectangleExt DesignBoundsExt { get; set; }
        /// <summary>
        /// Obsah řádků: obsahuje sloupce i jejich datové a popisné hodnoty.
        /// Klíčem je název sloupce.
        /// </summary>
        public DataContent Content { get { return __Content; } } private DataContent __Content;
        #endregion
        #region Předpřipravené hodnoty obecně dostupné, získávané z Content
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string Label
        {
            get { return ((TryGetContent<string>(DxDataFormDef.Label, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.Label, value); }
        }
        /// <summary>
        /// ToolTipText
        /// </summary>
        public string ToolTipText
        {
            get { return ((TryGetContent<string>(DxDataFormDef.ToolTipText, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.ToolTipText, value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string IconName
        {
            get { return ((TryGetContent<string>(DxDataFormDef.IconName, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.IconName, value); }
        }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible
        {
            get { return ((TryGetContent<bool?>(DxDataFormDef.IsVisible, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormDef.IsVisible, (bool?)value); }
        }
        /// <summary>
        /// Prvek je interaktvní?
        /// </summary>
        public bool IsInteractive
        {
            get { return ((TryGetContent<bool?>(DxDataFormDef.IsInteractive, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormDef.IsInteractive, (bool?)value); }
        }
        #endregion
        #region Metody pro získání dat o prvku (hodnota, vzhled, editační maska, font, barva, editační styl, ikona, buttony, atd...)
        /// <summary>
        /// Zkusí najít hodnotu daného jména.
        /// Jména hodnot jsou v konstantách v <see cref="DxDataFormDef"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool TryGetContent<T>(string name, out T content)
        {
            if (Content.TryGetContent(name, out content)) return true;
            content = default;
            return false;
        }
        /// <summary>
        /// Uloží hodnotu daného jména.
        /// Jména hodnot jsou v konstantách v <see cref="DxDataFormDef"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public void SetContent<T>(string name, T content)
        {
            Content[name] = content;
        }
        #endregion
        #region Výpočty DesignSize, tvorba konkrétních buněk DataFormCell
        /// <summary>
        /// Určí aktuální designovou souřadnici v prostoru daného parenta
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        internal void PrepareDesignSize(WinDraw.Rectangle parentBounds, ref int left, ref int top, ref int right, ref int bottom)
        {
            var designBounds = this.DesignBoundsExt.GetBounds(parentBounds);
            __CurrentDesignBounds = designBounds;
            if (right < designBounds.Right) right = designBounds.Right;
            if (bottom < designBounds.Bottom) bottom = designBounds.Bottom;
        }
        /// <summary>
        /// Aktuální přidělená souřadnice v metodě <see cref="PrepareDesignSize(WinDraw.Rectangle, ref int, ref int, ref int, ref int)"/>
        /// </summary>
        private WinDraw.Rectangle __CurrentDesignBounds;
        #endregion
        #region IDataFormLayoutDesignItem
        WinDraw.Rectangle IDataFormLayoutDesignItem.CurrentDesignBounds { get { return __CurrentDesignBounds; } }
        #endregion
    }
    /// <summary>
    /// Interface umožňující přístup na designová pracovní data prvku layoutu
    /// </summary>
    public interface IDataFormLayoutDesignItem
    {
        /// <summary>
        /// Aktuální designové souřadnice
        /// </summary>
        WinDraw.Rectangle CurrentDesignBounds { get; }
    }
    #endregion
    #region DataFormCell : jeden interaktivní prvek = Řádek × LayoutItem  (nikoliv Řádek × Sloupec)
    /// <summary>
    /// Reprezentuje jednu buňku = jeden fyzický prvek odpovídající konkrétnímu prvku layoutu na jednom konkrétním řádku
    /// </summary>
    public class DataFormCell : IInteractiveItem, IPaintItemData
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="row"></param>
        /// <param name="layoutItem"></param>
        public DataFormCell(DataFormRow row, DataFormLayoutItem layoutItem)
        {
            this.__Row = row;
            this.__LayoutItem = layoutItem;
            this.__DesignBounds = _ILayoutItem.CurrentDesignBounds.Add(row.RowDesignBounds.Location);
            this.__ItemState = DxItemState.Enabled;
            this.__InteractiveState = DxInteractiveState.None;
        }
        private DataFormRow __Row;
        private DataFormLayoutItem __LayoutItem;
        private WinDraw.Rectangle __DesignBounds;
        private WinDraw.Rectangle __ControlBounds;
        private WinForm.Control __NativeControl;
        private DxItemState __ItemState;
        private DxInteractiveState __InteractiveState;
        #endregion
        #region Vztah na další instance - pomocí Row (DataForm, Repozitory) a LayoutSet
        /// <summary>
        /// DataForm
        /// </summary>
        private DxDataFormPanel _DataForm { get { return __Row.DataForm; } }
        /// <summary>
        /// Repozitory, obsahující fyzické controly pro zobrazení a editaci dat
        /// </summary>
        private DxRepositoryManager _RepositoryManager { get { return __Row.DataForm?.RepositoryManager; } }
        /// <summary>
        /// Prvek layoutu <see cref="__LayoutItem"/> přetypovaný na <see cref="IDataFormLayoutDesignItem"/> pro přístup na interní data
        /// </summary>
        private IDataFormLayoutDesignItem _ILayoutItem { get { return __LayoutItem; } }
        #endregion
        #region Metody pro detekci aktivity, kreslení a změnu interaktivního stavu
        /// <summary>
        /// Vrátí true, pokud this prvek je aktivní na dané Control nebo Designové souřadnici (objekt si sám vybere, kterou souřadnici bude vyhodnocovat).
        /// </summary>
        /// <param name="controlPoint">Souřadnice fyzická na controlu</param>
        /// <param name="designPoint">Souřadnice designová</param>
        /// <returns></returns>
        public bool _IsActiveOnPoint(WinDraw.Point controlPoint, WinDraw.Point designPoint)
        {
            return this.IsInteractive && this.__DesignBounds.Contains(designPoint);
        }
        /// <summary>
        /// Provede vykreslení obrazu objektu, anebo umístění fyzického controlu.
        /// Pokud objekt není vykreslen (tedy pokud se nenachází ve viditelném prostoru panelu), pak se zde vrací false.
        /// </summary>
        /// <param name="pdea"></param>
        private bool _Paint(PaintDataEventArgs pdea)
        {
            if (!IsVisible) return false;

            __ControlBounds = pdea.InteractivePanel.GetControlBounds(this.__DesignBounds);       // Umístění prvku v koordinátech nativního controlu (z Designové souřadnice, přes Zoom a posuny ScrollBarů do prostoru v Panelu)
            bool isDisplayed = pdea.ClientArea.IntersectsWith(__ControlBounds);                  // true pokud se prvek nachází ve viditelné oblasti vizuálního panelu

            if (_DataForm.TestPainting) return _PaintTest(pdea, isDisplayed);
            
            _RepositoryManager.PaintItem(this, pdea, __ControlBounds, isDisplayed);
            return isDisplayed;
        }
        /// <summary>
        /// Interaktivní stav prvku. Setování stavu může mít vliv na druh zobrazení (zda bude vykreslen obraz / nebo fyzický Control).
        /// </summary>
        private DxInteractiveState _InteractiveState
        {
            get
            {
                var interactiveState = (((DxInteractiveState)this.__ItemState) & DxInteractiveState.MaskItemState) | (this.__InteractiveState & DxInteractiveState.MaskInteractive);
                return interactiveState;
            }
            set
            {
                var oldState = __InteractiveState;
                var newState = (((DxInteractiveState)this.__ItemState) & DxInteractiveState.MaskItemState) | (value & DxInteractiveState.MaskInteractive);
                this.__InteractiveState = newState;
                if (oldState != newState)
                {
                    _RunInteractiveStateChanged();
                    _RepositoryManager.ChangeItemInteractiveState(this, __ControlBounds);
                }
            }
        }
        /// <summary>
        /// Vykreslí prvek v testovacím režimu
        /// </summary>
        /// <param name="pdea"></param>
        /// <param name="isDisplayed"></param>
        /// <returns></returns>
        private bool _PaintTest(PaintDataEventArgs pdea, bool isDisplayed)
        {
            var state = this.__InteractiveState;
            bool isActive = (state == DxInteractiveState.HasMouse || state == DxInteractiveState.MouseLeftDown);
            WinDraw.Rectangle controlBounds = __ControlBounds;
            if (isActive)
            {
                DxComponent.LogAddLine($"DataFormCell.Paint(): IsActive: {isActive}; IsDisplayed: {isDisplayed}; DesignBounds: {__DesignBounds}");
                if (!isDisplayed)
                    DxComponent.LogAddLine($"DataFormCell.Paint(): Outside: ControlBounds: {controlBounds}; ClientArea: {pdea.ClientArea};");
            }
            if (!isDisplayed) return false;            // Pokud vykreslovaný control a naše souřadnice nemají nic společného, pak nebudeme kreslit...

            WinDraw.Color color = WinDraw.Color.LightYellow;
            switch (state)
            {
                case DxInteractiveState.HasMouse:
                    color = WinDraw.Color.LightGreen;
                    break;
                case DxInteractiveState.MouseLeftDown:
                case DxInteractiveState.MouseRightDown:
                    color = WinDraw.Color.LightSkyBlue;
                    break;
            }
            pdea.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color), controlBounds);
            pdea.Graphics.DrawRectangle(DxComponent.PaintGetPen(WinDraw.Color.Black), controlBounds);
            string text = __LayoutItem.Label;
            if (!String.IsNullOrEmpty(text))
                pdea.Graphics.DrawString(text, DxComponent.GetSystemFont(DxComponent.SystemFontType.DefaultFont), DxComponent.PaintGetSolidBrush(WinDraw.Color.Black), controlBounds);

            return true;
        }
        /// <summary>
        /// Po změně interaktivního stavu - dáme vědět někam? Do dataformu, do datové vrstvy? ...
        /// </summary>
        private void _RunInteractiveStateChanged()
        {
        }
        #endregion
        #region Předpřipravené hodnoty obecně dostupné, získávané z Content
        /// <summary>
        /// Jméno tohoto sloupce. K němu se dohledají další data a případné modifikace stylu v konkrétním řádku.
        /// Interně se pracuje s <see cref="ColumnId"/> (int), konverze řeší <see cref="DataForm"/>.
        /// </summary>
        public string ColumnName { get { return this.__LayoutItem.ColumnName; } }
        /// <summary>
        /// Hodnota prvku
        /// </summary>
        public object Value
        {
            get { return ((TryGetContent<object>(DxDataFormDef.Value, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.Value, (bool?)value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string Label
        {
            get { return ((TryGetContent<string>(DxDataFormDef.Label, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.Label, value); }
        }
        /// <summary>
        /// ToolTipText
        /// </summary>
        public string ToolTipText
        {
            get { return ((TryGetContent<string>(DxDataFormDef.ToolTipText, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.ToolTipText, value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string IconName
        {
            get { return ((TryGetContent<string>(DxDataFormDef.IconName, out var content)) ? content : null); }
            set { SetContent(DxDataFormDef.IconName, value); }
        }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible
        {
            get { return ((TryGetContent<bool?>(DxDataFormDef.IsVisible, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormDef.IsVisible, (bool?)value); }
        }
        /// <summary>
        /// Prvek je interaktvní?
        /// </summary>
        public bool IsInteractive
        {
            get { return ((TryGetContent<bool?>(DxDataFormDef.IsInteractive, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormDef.IsInteractive, (bool?)value); }
        }
        #endregion
        #region Metody pro získání dat o prvku (hodnota, vzhled, editační maska, font, barva, editační styl, ikona, buttony, atd...)
        /// <summary>
        /// Zkusí najít hodnotu daného jména.
        /// Dané jméno nesmí obsahovat jméno sloupce.
        /// Hodnota se prioritně hledá v řádku (=specifická pro konkrétní řádek), a pokud tam není, pak se hledá v layoutu (defaultní hodnota).
        /// Jména hodnot jsou v konstantách v <see cref="DxDataFormDef"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Jméno vlastnosti. Nesmí obsahovat jméno sloupce, to bude přidáno.</param>
        /// <param name="content">Out hodnota</param>
        /// <returns></returns>
        public bool TryGetContent<T>(string name, out T content)
        {
            if (__Row.Content.TryGetContent(_GetFullName(name), out content)) return true;
            if (__LayoutItem.Content.TryGetContent(name, out content)) return true;
            if (_DataForm.Content.TryGetContent(name, out content)) return true;
            content = default;
            return false;
        }
        /// <summary>
        /// Zkusí najít hodnotu daného jména.
        /// Dané jméno nesmí obsahovat jméno sloupce.
        /// Hodnota se prioritně hledá v řádku (=specifická pro konkrétní řádek), a pokud tam není, pak se hledá v layoutu (defaultní hodnota).
        /// Jména hodnot jsou v konstantách v <see cref="DxDataFormDef"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Jméno vlastnosti. Nesmí obsahovat jméno sloupce, to bude přidáno.</param>
        /// <param name="content">Ukládaná hodnota</param>
        /// <returns></returns>
        public void SetContent<T>(string name, T content)
        {
            __Row.Content[_GetFullName(name)] = content;
        }
        private string _GetFullName(string propertyName)
        {
            return $"{this.ColumnName}{DxDataFormDef.ColumnDelimiter}{propertyName}";
        }
        #endregion
        #region IPaintItemData
        DxRepositoryEditorType IPaintItemData.EditorType { get { return this.__LayoutItem.ColumnType; } }
        DxInteractiveState IPaintItemData.InteractiveState { get { return this._InteractiveState; } }
        WinForm.Control IPaintItemData.NativeControl { get { return this.__NativeControl; } set { this.__NativeControl = value; } }
        bool IPaintItemData.TryGetContent<T>(string name, out T content) { return TryGetContent<T>(name, out content); }
        ulong? IPaintItemData.ImageId { get { return __ImageId; } set { __ImageId = value; } } private ulong? __ImageId;
        byte[] IPaintItemData.ImageData { get { return __ImageData; } set { __ImageData = value; } } private byte[] __ImageData;
        #endregion
        #region IInteractiveItem
        bool IInteractiveItem.IsVisible { get { return IsVisible; } }
        bool IInteractiveItem.IsInteractive { get { return IsInteractive; } }
        WinDraw.Rectangle IInteractiveItem.DesignBounds { get { return __DesignBounds; } }
        DxItemState IInteractiveItem.ItemState { get { return __ItemState; } }
        DxInteractiveState IInteractiveItem.InteractiveState { get { return _InteractiveState; } set { _InteractiveState = value; } }
        bool IInteractiveItem.Paint(PaintDataEventArgs pdea) { return this._Paint(pdea); }
        bool IInteractiveItem.IsActiveOnPoint(WinDraw.Point controlPoint, WinDraw.Point designPoint) { return this._IsActiveOnPoint(controlPoint, designPoint); }
        DxInteractivePanel IChildOfParent<DxInteractivePanel>.Parent { get { return null; } set { } }
        #endregion
    }
    #endregion
    #region DataContent : úložiště sady dat
    /// <summary>
    /// <see cref="DataContent"/> : Úložiště dat.<br/>
    /// Ukládá data libovolného datového typu. Klíčem je string.
    /// První částí klíče bývá jméno sloupce, za ním dvojtečka a pak jméno konkrétní vlastnosti. Jména vlastností jsou v konstantách ...
    /// </summary>
    public class DataContent
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataContent()
        {
            __Content = new Dictionary<string, object>();
        }
        private Dictionary<string, object> __Content;
        /// <summary>
        /// Přístup na konkrétní hodnotu.
        /// Setovat lze snadno hodnotu i tehdy, pokud dosud neexistuje.
        /// Setování null hodnotu vymaže.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object this[string name]
        {
            get 
            {
                if (name is null) return null;
                if (__Content.TryGetValue(name, out var value))  return value;           // Moje vlastní data
                return null;
            }
            set
            {
                if (name is null) return;

                bool exists = __Content.ContainsKey(name);
                if (value is null)
                {
                    if (exists)
                        __Content.Remove(name);
                }
                else
                {
                    if (exists)
                        __Content[name] = value;
                    else
                        __Content.Add(name, value);
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud this instance obsahuje dané jméno. Pokud jméno je null, pak jej neobshauje.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsName(string name)
        {
            return (name != null && this.__Content.ContainsKey(name));
        }
        /// <summary>
        /// Vrátí true, pokud this instance obsahuje dané jméno a uložená hodnota je daného typu. Pak na výstupu je hodnota již v požadovaném typu.
        /// Pokud jméno je null, nebo jméno není nalezeno, anebo je nalezena hodnota jiná než typu T, pak výstupem je false.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool TryGetContent<T>(string name, out T content)
        {
            if (name != null && this.__Content.TryGetValue(name, out var value) && value is T result)
            {
                content = result;
                return true;
            }
            content = default;
            return false;
        }
    }
    #endregion
    #region DxDataFormDef : definice pro DataForm, jména vlastností pro úložiště dat
    public class DxDataFormDef
    {
        public const string ColumnDelimiter = ":";
        public const string Value = "V";
        public const string IsVisible = "Vis";
        public const string IsInteractive = "Int";
        public const string Label = "Lbl";
        public const string ToolTipText = "Ttx";
        public const string IconName = "Icn";
        public const string FontStyle = "Fst";
        public const string BackColor = "BgC";
        public const string TextColor = "TxC";
        public const string TextBoxButtons = "TxBxBt";
        public const string BorderStyle = "BrS";
    }

    #endregion
}
