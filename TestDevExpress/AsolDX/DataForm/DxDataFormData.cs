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
        /// Hlavní instance Dataformu
        /// </summary>
        internal DxDataFormPanel DataForm { get { return __DataForm; } } private DxDataFormPanel __DataForm;
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        public DxDataFormContentPanel DataFormContent { get { return __DataForm?.DataFormContent; } }
        /// <summary>
        /// Zajistí vykreslení obsahu Dataformu
        /// </summary>
        public void DataFormDraw() { DataFormContent?.Draw(); }
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
                DxComponent.LogAddLine(LogActivityKind.DataFormRepository, $"DataFormRows.GetRowsInDesignPixels(): AllRows");
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
            DxComponent.LogAddLine(LogActivityKind.VirtualChanges, $"DataFormRows.GetRowsInDesignPixels(): DesignPixels: {designPixels}; Rows.Count: {result.Count}");

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
        #region Konstruktor a základní proměnné, Parent
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
        /// Klíčem je název sloupce a druh vlastnosti <see cref="DxDataFormProperty"/>
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
        /// Hlavní instance Dataformu
        /// </summary>
        public DxDataFormPanel DataForm { get { return __DataForm; } } private DxDataFormPanel __DataForm;
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        public DxDataFormContentPanel DataFormContent { get { return __DataForm?.DataFormContent; } }
        /// <summary>
        /// Zajistí vykreslení obsahu Dataformu
        /// </summary>
        public void DataFormDraw() { DataFormContent?.Draw(); }
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
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ColumnName}  [{ColumnType}]";
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
            get { return ((TryGetContent<string>(DxDataFormProperty.Label, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.Label, value); }
        }
        /// <summary>
        /// ToolTipText
        /// </summary>
        public string ToolTipText
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.ToolTipText, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.ToolTipText, value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string IconName
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.IconName, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.IconName, value); }
        }
        /// <summary>
        /// Typ kurzoru, který bude aktivován po najetí myší na aktivní prvek
        /// </summary>
        public CursorTypes? CursorTypeMouseOn
        {
            get { return ((TryGetContent<CursorTypes?>(DxDataFormProperty.CursorTypeMouseOn, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.CursorTypeMouseOn, value); }
        }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible
        {
            get { return ((TryGetContent<bool?>(DxDataFormProperty.IsVisible, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormProperty.IsVisible, (bool?)value); }
        }
        /// <summary>
        /// Prvek je interaktvní?
        /// </summary>
        public bool IsInteractive
        {
            get { return ((TryGetContent<bool?>(DxDataFormProperty.IsInteractive, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormProperty.IsInteractive, (bool?)value); }
        }
        #endregion
        #region Metody pro získání dat o prvku (hodnota, vzhled, editační maska, font, barva, editační styl, ikona, buttony, atd...)
        /// <summary>
        /// Zkusí najít hodnotu požadované vlastnosti.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool TryGetContent<T>(DxDataFormProperty property, out T content)
        {
            if (Content.TryGetContent(property, out content)) return true;
            content = default;
            return false;
        }
        /// <summary>
        /// Uloží hodnotu požadované vlastnosti.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public void SetContent<T>(DxDataFormProperty property, T content)
        {
            Content[property] = content;
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
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ColumnName: {ColumnName}; State: {__InteractiveState}; Label: {Label}; Value: {Value}";
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
        /// DataFormContent = fyzický vykreslovací Control, na kterém jsou zobrazovány zdejší data
        /// </summary>
        private DxDataFormContentPanel _DataFormContent { get { return _DataForm.DataFormContent; } }
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
                    _RepositoryManager.ChangeItemInteractiveState(this, _DataFormContent, __ControlBounds);
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
                DxComponent.LogAddLine(LogActivityKind.Paint, $"DataFormCell.Paint(): IsActive: {isActive}; IsDisplayed: {isDisplayed}; DesignBounds: {__DesignBounds}");
                if (!isDisplayed)
                    DxComponent.LogAddLine(LogActivityKind.Paint, $"DataFormCell.Paint(): Outside: ControlBounds: {controlBounds}; ClientArea: {pdea.ClientArea};");
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
        /// </summary>
        public string ColumnName { get { return this.__LayoutItem.ColumnName; } }
        /// <summary>
        /// Hodnota prvku
        /// </summary>
        public object Value
        {
            get { return ((TryGetContent<object>(DxDataFormProperty.Value, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.Value, (bool?)value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string Label
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.Label, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.Label, value); }
        }
        /// <summary>
        /// ToolTipText
        /// </summary>
        public string ToolTipText
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.ToolTipText, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.ToolTipText, value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        public string IconName
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.IconName, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.IconName, value); }
        }
        /// <summary>
        /// Typ kurzoru, který bude aktivován po najetí myší na aktivní prvek
        /// </summary>
        public CursorTypes? CursorTypeMouseOn
        {
            get { return ((TryGetContent<CursorTypes?>(DxDataFormProperty.CursorTypeMouseOn, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.CursorTypeMouseOn, value); }
        }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible
        {
            get { return ((TryGetContent<bool?>(DxDataFormProperty.IsVisible, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormProperty.IsVisible, (bool?)value); }
        }
        /// <summary>
        /// Prvek je interaktvní?
        /// </summary>
        public bool IsInteractive
        {
            get { return ((TryGetContent<bool?>(DxDataFormProperty.IsInteractive, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormProperty.IsInteractive, (bool?)value); }
        }
        #endregion
        #region Metody pro získání a uložení dat o prvku (hodnota, vzhled, editační maska, font, barva, editační styl, ikona, buttony, atd...) a odeslání akce do DataFormu
        /// <summary>
        /// Zkusí najít hodnotu požadované vlastnosti.
        /// Hodnota se prioritně hledá v řádku (=specifická pro konkrétní řádek), a pokud tam není, pak se hledá v layoutu (defaultní hodnota).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content">Out hodnota</param>
        /// <returns></returns>
        public bool TryGetContent<T>(DxDataFormProperty property, out T content)
        {
            if (__Row.Content.TryGetContent(__LayoutItem.ColumnName, property, out content)) return true;
            if (__LayoutItem.Content.TryGetContent(property, out content)) return true;
            if (_DataForm.Content.TryGetContent(__LayoutItem.ColumnName, property, out content)) return true;
            if (_DataForm.Content.TryGetContent(property, out content)) return true;
            content = default;
            return false;
        }
        /// <summary>
        /// Zkusí najít hodnotu požadované vlastnosti.
        /// Hodnota se prioritně hledá v řádku (=specifická pro konkrétní řádek), a pokud tam není, pak se hledá v layoutu (defaultní hodnota).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content">Ukládaná hodnota</param>
        /// <returns></returns>
        public void SetContent<T>(DxDataFormProperty property, T content)
        {
            __Row.Content[__LayoutItem.ColumnName, property] = content;
            _InvalidateCache();
        }
        /// <summary>
        /// Vyvolá danou akci
        /// </summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        public void RunAction(DxDataFormAction action, object data)
        {
            this._DataForm?.OnInteractiveAction(this.__Row, this.ColumnName, action, data);
        }
        /// <summary>
        /// Invaliduje svoje grafická data (uložený statický obrázek) - volá se po změnách hodnoty
        /// </summary>
        private void _InvalidateCache()
        {
            __ImageId = null; 
            __ImageData = null;
        }
        #endregion
        #region IPaintItemData
        DxRepositoryEditorType IPaintItemData.EditorType { get { return this.__LayoutItem.ColumnType; } }
        DxInteractiveState IPaintItemData.InteractiveState { get { return this._InteractiveState; } set { this._InteractiveState = value; } }
        WinForm.Control IPaintItemData.NativeControl { get { return this.__NativeControl; } set { this.__NativeControl = value; } }
        DataFormLayoutItem IPaintItemData.LayoutItem { get { return __LayoutItem; } }
        bool IPaintItemData.TryGetContent<T>(DxDataFormProperty property, out T content) { return TryGetContent<T>(property, out content); }
        void IPaintItemData.SetContent<T>(DxDataFormProperty property, T value) { SetContent<T>(property, value); }
        void IPaintItemData.RunAction(DxDataFormAction action, object data) { RunAction(action, data); }
        void IPaintItemData.InvalidateCache() { _InvalidateCache(); }
        ulong? IPaintItemData.ImageId { get { return __ImageId; } set { __ImageId = value; } } private ulong? __ImageId;
        byte[] IPaintItemData.ImageData { get { return __ImageData; } set { __ImageData = value; } } private byte[] __ImageData;
        #endregion
        #region IInteractiveItem
        bool IInteractiveItem.IsVisible { get { return IsVisible; } }
        bool IInteractiveItem.IsInteractive { get { return IsInteractive; } }
        WinDraw.Rectangle IInteractiveItem.DesignBounds { get { return __DesignBounds; } }
        DxItemState IInteractiveItem.ItemState { get { return __ItemState; } }
        DxInteractiveState IInteractiveItem.InteractiveState { get { return _InteractiveState; } set { _InteractiveState = value; } }
        CursorTypes? IInteractiveItem.CursorTypeMouseOn { get { return CursorTypeMouseOn; } }
        bool IInteractiveItem.Paint(PaintDataEventArgs pdea) { return this._Paint(pdea); }
        bool IInteractiveItem.IsActiveOnPoint(WinDraw.Point controlPoint, WinDraw.Point designPoint) { return this._IsActiveOnPoint(controlPoint, designPoint); }
        DxInteractivePanel IChildOfParent<DxInteractivePanel>.Parent { get { return null; } set { } }
        #endregion
    }
    #endregion
    #region DataContent : úložiště sady dat
    /// <summary>
    /// <see cref="DataContent"/> : Úložiště dat.<br/>
    /// Ukládá data libovolného datového typu. Klíčem je jméno sloupce (volitelně) a typ vlastnosti <see cref="DxDataFormProperty"/>.
    /// Pokud vlastníkem tohoto objektu je konkrétní sloupec (<see cref="DataFormLayoutItem"/>), pak se v klíči nepoužívá jméno sloupce.
    /// Pokud vlastníkem tohoto objektu je řádek, pak se jméno sloupce zadává.
    /// </summary>
    public class DataContent
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataContent()
        {
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Columns: {(__Columns is null ? "None" : __Columns.Count.ToString())}; Data: {(__Content is null ? "None" : __Content.Count.ToString())}";
        }
        #endregion
        #region Columns: konverzní tabulka ColumnName => ID, protože data jednotlivých vlastností se uchovávají s klíčem ID a nikoli String
        /*    Původně jsem chtěl mít tuto tabulku { ColumnName => ID } jen jednu na celý DataForm. Dávalo by to velice smysl.
           Ale nepřišel jsem na to, jak koordinovat tvorbu řádků a layoutu tak, aby v době tvorby (když se do objektů DataFormRow a DataFormLayoutItem vkládají data)
           byl k dispozici i cílový objekt DataForm, který by obsahoval centrální tabulku { ColumnName => ID }.
           Musel bych vynutit tvorbu těchto objektů prostřednictvím DataFormu (něco jako DataForm.CreateRow) a to mi přijde krkolomný.
              Další variantou by bylo: vytvářet nejprve data separátně (s lokální tabulkou { ColumnName => ID }),
           a tuto lokální tabulku pak v okamžiku napojení na DataForm mergovat do centrální a současně změnit ID datových záznamů... ale i to mi přijde krkolomný.
              Budu tedy mít v každé instanci (DataForm, DataFormRow a DataFormLayoutItem) lokální ColumnId a zvenku se bude používat string ColumnName.
              Navíc instance DataFormLayoutItem pro získání dat nepoužívá jméno sloupce takže ai nevznikne tabulka { ColumnName => ID }
        */

        /// <summary>
        /// Vrátí ID sloupce daného jména.
        /// Zadáním <paramref name="forWrite"/> = false říkáme, že hledáme data pro čtení 
        /// - a pokud daný sloupec dosud nemáme v evidenci, pak jej ani nebudeme ukládat (protože mu sice přidělíme ID, ale 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="forWrite">Hledáme klíč pro zápis.<br/>
        /// true: pro zápis = Pokud je zadáno jméno sloupce, který dosud neexistuje, pak jej musíme zaevidovat a přidělit ID sloupce, abychom do něj mohli zapsat hodnotu.<br/>
        /// false: pro čtení = Pokud zadané jméno sloupce neexistuje, pak jej nemusíme zakládat, protože jeho hodnota stejně nebude nalezena.</param>
        /// <returns></returns>
        private int _GetColumnId(string columnName, bool forWrite)
        {
            if (String.IsNullOrEmpty(columnName)) return 0;          // Bez jména sloupce vrátím 0, což je validní číslo "bez sloupce" (první reálný dostane 1).

            if (__Columns is null)
            {
                if (!forWrite) return -1;                  // Požadavek je "číst hodnotu konkrétního sloupce" a my dosud nemáme žádné sloupce => nemůžeme mít ani data => vrátím -1 a číst se nebude.

                // Chceme zapisovat data do konkrétního sloupce (a dosud nemáme Dictionary  { ColumnName => ID }:
                __Columns = new Dictionary<string, int>();
                __LastColumnId = 0;
            }

            if (!this.ExactColumnNames) columnName = columnName.Trim().ToLower();
            if (!__Columns.TryGetValue(columnName, out int id))
            {
                if (!forWrite) return -1;                  // Požadavek je "číst hodnotu konkrétního sloupce" a my tento sloupec dosud nemáme => nemůžeme mít ani data => vrátím -1 a číst se nebude.

                // Chceme zapisovat data do konkrétního sloupce, a ten sloupec tu dosud není => přidělíme mu nové ID a uložíme si jej:
                id = ++__LastColumnId;
                __Columns.Add(columnName, id);
            }
            return id;
        }
        /// <summary>
        /// Používat exaktní jména sloupců? <br/>
        /// true = včetně mezer a CaseSensitive;<br/>
        /// false = Trim() a CaseInsensitive.
        /// <para/>
        /// Nelze změnit za běhu, když objekt už obsahuje data sloupců.
        /// </summary>
        public bool ExactColumnNames
        {
            get { return __ExactColumnNames; }
            set
            {
                if (__Columns != null && __Columns.Count > 0)
                    throw new InvalidOperationException("DataContent.ExactColumnNames cannot be changed if any columns already exist.");
                __ExactColumnNames = value;
            }
        }
        private int __LastColumnId;
        private Dictionary<string, int> __Columns;
        private bool __ExactColumnNames;
        #endregion
        #region Data
        /// <summary>
        /// Přístup na konkrétní hodnotu.
        /// Setovat lze snadno hodnotu i tehdy, pokud dosud neexistuje.
        /// Setování null hodnotu vymaže.
        /// </summary>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <returns></returns>
        public object this[DxDataFormProperty property]
        {
            get { return this[null, property]; }
            set { this[null, property] = value; }
        }
        /// <summary>
        /// Přístup na konkrétní hodnotu.
        /// Setovat lze snadno hodnotu i tehdy, pokud dosud neexistuje.
        /// Setování null hodnotu vymaže.
        /// </summary>
        /// <param name="columnName">Jméno sloupce</param>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <returns></returns>
        public object this[string columnName, DxDataFormProperty property]
        {
            get
            {
                int key = _GetKey(columnName, property, false);
                if (key > 0)
                {
                    var content = _Content;
                    if (content.TryGetValue(key, out var value)) return value;
                }
                return null;
            }
            set
            {
                int key = _GetKey(columnName, property, true);
                if (key > 0)
                {
                    var content = _Content;
                    bool exists = content.ContainsKey(key);
                    if (value is null)
                    {
                        if (exists)
                            content.Remove(key);
                    }
                    else
                    {
                        if (exists)
                            content[key] = value;
                        else
                            content.Add(key, value);
                    }
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud this instance obsahuje dané jméno. Pokud jméno je null, pak jej neobshauje.
        /// </summary>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <returns></returns>
        public bool ContainsName(DxDataFormProperty property)
        {
            return ContainsName(null, property);
        }
        /// <summary>
        /// Vrátí true, pokud this instance obsahuje dané jméno. Pokud jméno je null, pak jej neobshauje.
        /// </summary>
        /// <param name="columnName">Jméno sloupce</param>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <returns></returns>
        public bool ContainsName(string columnName, DxDataFormProperty property)
        {
            int key = _GetKey(columnName, property, false);
            if (key == 0) return false;

            var content = _Content;
            return (content.ContainsKey(key));
        }
        /// <summary>
        /// Vrátí true, pokud this instance obsahuje dané jméno a uložená hodnota je daného typu. Pak na výstupu je hodnota již v požadovaném typu.
        /// Pokud jméno je null, nebo jméno není nalezeno, anebo je nalezena hodnota jiná než typu T, pak výstupem je false.
        /// </summary>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool TryGetContent<T>(DxDataFormProperty property, out T content)
        {
            return TryGetContent<T>(null, property, out content);
        }
        /// <summary>
        /// Vrátí true, pokud this instance obsahuje dané jméno a uložená hodnota je daného typu. Pak na výstupu je hodnota již v požadovaném typu.
        /// Pokud jméno je null, nebo jméno není nalezeno, anebo je nalezena hodnota jiná než typu T, pak výstupem je false.
        /// </summary>
        /// <param name="columnName">Jméno sloupce</param>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool TryGetContent<T>(string columnName, DxDataFormProperty property, out T data)
        {
            int key = _GetKey(columnName, property, false);
            if (key > 0)
            {
                var content = _Content;
                if (content.TryGetValue(key, out var value) && value is T result)
                {
                    data = result;
                    return true;
                }
            }
            data = default;
            return false;
        }
        /// <summary>
        /// Vrátí unikátní Int klíč pro data daného sloupce a danou vlastnost
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="forWrite">Hledáme klíč pro zápis.<br/>
        /// true: pro zápis = Pokud je zadáno jméno sloupce, který dosud neexistuje, pak jej musíme zaevidovat a přidělit ID sloupce, abychom do něj mohli zapsat hodnotu.<br/>
        /// false: pro čtení = Pokud zadané jméno sloupce neexistuje, pak jej nemusíme zakládat, protože jeho hodnota stejně nebude nalezena.</param>
        /// <returns></returns>
        private int _GetKey(string columnName, DxDataFormProperty property, bool forWrite)
        {
            if (property == DxDataFormProperty.None) return 0;

            int columnId = _GetColumnId(columnName, forWrite);
            // Pokud vrácené columnId je záporné, pak jde o zadané jméno sloupce, který dosud nebyl použit a my jsme v režimu (forWrite == false) tedy čteme hodnotu.
            // Neexistující sloupec nikdy nemůže mít uloženou hodnotu, proto pro záporné ID vracím Key = 0 = neexistující data:
            // (Sloupec null vrátí hodnotu 0, což je hodnota bez sloupce, a ta se číst i ukládat může)
            if (columnId < 0) return 0;

            // Výstupní hodnota Int32 má 32 bitů, z nich použijeme dolních 31 (horní je znaménko Negative).
            // Pokud přidělím horních 15 bitů pro číslo sloupce, pak může existovat až 32767 sloupců, to je dost. 
            // A stejně tak 32767 vlastností umístíme do dolních 15 bitů. Tolik jich nikdy nebude.
            return ((columnId & 0x7FFF) << 16) | (((int)property) & 0x7FFF);
            // ==> Případné úpravy promítni i do zdejší metody _CreateData() !!!
        }
        /// <summary>
        /// Úložiště dat, autoinicializační property
        /// </summary>
        private Dictionary<int, object> _Content { get { return __Content ??= new Dictionary<int, object>(); } }
        /// <summary>
        /// Úložiště dat
        /// </summary>
        private Dictionary<int, object> __Content;
        #endregion
        #region Debugovací pole obsahující jména sloupců, vlastností a hodnoty - v provozu se nepoužívá, ale bez něj se špatně hledají data
        /// <summary>
        /// Data všech uložených sloupců a vlastností
        /// </summary>
        public ContentItem[] Data { get { return _CreateData(); } }
        /// <summary>
        /// Vytvoří a vrátí Data všech uložených sloupců a vlastností
        /// </summary>
        /// <returns></returns>
        private ContentItem[] _CreateData()
        {
            // Sloupce:
            var revColumns = (__Columns is null) ? new Dictionary<int, string>() : __Columns.CreateDictionary(kvp => kvp.Value, kvp => kvp.Key);

            // Data:
            List<ContentItem> data = new List<ContentItem>();
            var content = __Content;
            if (content != null)
            {
                foreach (var kvp in content)
                {
                    getColumnProperty(kvp.Key, out string columnName, out DxDataFormProperty property);
                    data.Add(new ContentItem(columnName, property, kvp.Value));
                }
            }
            return data.ToArray();

            // Reverzní cesta z int klíče na string ColumnName a DxDataFormProperty:
            void getColumnProperty(int key, out string colName, out DxDataFormProperty prop)
            {
                int colId = ((key >> 16) & 0x7FFF);
                int propId = (key & 0x7FFF);
                colName = ((colId > 0 && revColumns.TryGetValue(colId, out var name)) ? name : null);
                prop = (DxDataFormProperty)propId;
            }
        }
        /// <summary>
        /// Data jednoho prvku
        /// </summary>
        public class ContentItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="property"></param>
            /// <param name="value"></param>
            public ContentItem(string columnName, DxDataFormProperty property, object value)
            {
                ColumnName = columnName;
                Property = property;
                Value = value;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"{(ColumnName is null ? "[common]" : ColumnName)}:{Property} = {Value}";
            }
            /// <summary>
            /// Jméno sloupce
            /// </summary>
            public string ColumnName { get; private set; }
            /// <summary>
            /// Typ vlastnosti
            /// </summary>
            public DxDataFormProperty Property { get; private set; }
            /// <summary>
            /// Data
            /// </summary>
            public object Value { get; private set; }
        }
        #endregion
    }
    #endregion
    #region DxDataFormProperty : definice jednotlivých vlastností pro DataForm
    /// <summary>
    /// Souhrn veškerých vlastností (property) ze všech editorových typů.
    /// Konkrétní editorový typ používá jen podmnožinu vlastností.
    /// Všechny vlastnosti se ukládají do instance třídy <see cref="DataContent"/>
    /// Každá konkrétní vlastnost má svůj datový typ, 
    /// </summary>
    public enum DxDataFormProperty
    {
        /// <summary>
        /// Není property. Její hodnota se neukládá a tedy nikdy neexistuje.
        /// </summary>
        None = 0,
        /// <summary>
        /// Je viditelný?  Používá se v deklaraci layoutu
        /// </summary>
        IsVisible,
        /// <summary>
        /// Je ReadOnly?  Používá se v deklaraci layoutu
        /// </summary>
        IsReadOnly,
        /// <summary>
        /// Je Enabled?  Používá se v deklaraci layoutu
        /// </summary>
        IsEnabled,
        /// <summary>
        /// Je interaktivní?  Používá se v deklaraci layoutu
        /// </summary>
        IsInteractive,
        /// <summary>
        /// Statický text (pro Label, Title, ...)
        /// </summary>
        Label,
        /// <summary>
        /// Hodnota
        /// </summary>
        Value,
        ToolTipText,
        IconName,
        FontStyle,
        FontSizeRatio,
        BackColor,
        TextColor,
        /// <summary>
        /// Definice přidaných tlačítek u TextBoxu, instance třídy <see cref="TextBoxButtonProperties"/>
        /// </summary>
        TextBoxButtons,
        /// <summary>
        /// Definice prvků ComboBoxu, instance třídy <see cref="ImageComboBoxProperties"/>
        /// </summary>
        ComboBoxItems,
        /// <summary>
        /// Běžné orámované prvky jako TextBox, Combo atd: typ <see cref="DataForm.BorderStyle"/>, default = <see cref="BorderStyle.HotFlat"/>
        /// </summary>
        BorderStyle,
        /// <summary>
        /// Běžné neorámované prvky jako CheckBox, Toggle, RadioButton atd: typ <see cref="DataForm.BorderStyle"/>, default = <see cref="BorderStyle.NoBorder"/>
        /// </summary>
        CheckBoxBorderStyle,
        ButtonPaintStyle,
        CursorTypeMouseOn,
        LabelAlignment,
        /// <summary>
        /// Velikost Popup okna (=nabídka pod ComboBoxem)
        /// </summary>
        ComboPopupFormSize,
        /// <summary>
        /// ToggleSwitch: float poměr šířky přepínače k výšce objektu; default = 2.5f, vhodná hodnota 1.8 - 4
        /// </summary>
        ToggleSwitchRatio,
        /// <summary>
        /// CheckBox + ToggleSwitch: string text pro hodnotu False
        /// </summary>
        CheckBoxLabelFalse,
        /// <summary>
        /// CheckBox + ToggleSwitch: string text pro hodnotu true
        /// </summary>
        CheckBoxLabelTrue
    }
    /// <summary>
    /// Souhrn veškerých akcí, které může kterýkoli editorový typ vyvolat.
    /// Konkrétní editorový typ používá jen podmnožinu akcí.
    /// </summary>
    public enum DxDataFormAction
    {
        /// <summary>
        /// Není akce
        /// </summary>
        None,
        /// <summary>
        /// Vstup focusu do prvku
        /// </summary>
        GotFocus,
        /// <summary>
        /// Odchod focusu z prvku
        /// </summary>
        LostFocus,
        /// <summary>
        /// Kliknutí na button (včetně subbuttonů)
        /// </summary>
        ButtonClick,
        /// <summary>
        /// Stisknutí buttonu (???)
        /// </summary>
        ButtonPressed,
        /// <summary>
        /// Pravá myš (vede na kontextové menu), v přidaných datech je předána instance <see cref="DxMouseActionInfo"/>
        /// </summary>
        RightClick,
        /// <summary>
        /// Došlo ke změně hodnoty
        /// </summary>
        ValueChanged
    }
    /// <summary>
    /// Informace o stavu myši a kláves Ctrl-Alt-Shift v době akce
    /// </summary>
    public class DxMouseActionInfo
    {
        /// <summary>
        /// Pozice myši v koordinátech Screens
        /// </summary>
        public WinDraw.Point MouseAbsoluteLocation { get; private set; }
        /// <summary>
        /// Pozice myši v koordinátech Control
        /// </summary>
        public WinDraw.Point MouseControlLocation { get; private set; }
        /// <summary>
        /// Tlačítka myši
        /// </summary>
        public WinForm.MouseButtons MouseButtons { get; private set; }
        /// <summary>
        /// Modifikátorové klávesy
        /// </summary>
        public WinForm.Keys ModifierKeys { get; private set; }
    }
    #endregion
    #region Podpůrné třídy a enumy pro definici layoutu - specifikace detailů
    #region TextBoxButtonProperties : definice sady buttonů
    /// <summary>
    /// Třída definující buttony přidané k nějakému Controlu
    /// </summary>
    public class TextBoxButtonProperties
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TextBoxButtonProperties()
        {
            __Buttons = new List<Button>();
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        /// <summary>
        /// Konstruktor pro jednoduchou definici
        /// </summary>
        /// <param name="simpleDefinition"></param>
        public TextBoxButtonProperties(string simpleDefinition)
            : this()
        {
            if (!String.IsNullOrEmpty(simpleDefinition))
            {
                var buttonNames = simpleDefinition.Split(',', ';');
                foreach (var buttonName in buttonNames)
                {
                    if (!String.IsNullOrEmpty(buttonName))
                    {
                        if (Enum.TryParse(buttonName, out DevExpress.XtraEditors.Controls.ButtonPredefines kind))
                            this.__Buttons.Add(new Button(kind));
                        else
                            this.__Buttons.Add(new Button(buttonName));
                    }
                }
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.__Buttons.ToOneString(";");
        }
        /// <summary>
        /// Styl okrajů buttonu, default = <see cref="DevExpress.XtraEditors.Controls.BorderStyles.NoBorder"/>
        /// </summary>
        public DevExpress.XtraEditors.Controls.BorderStyles BorderStyle { get; set; }
        /// <summary>
        /// Pole buttonů
        /// </summary>
        public List<Button> Buttons { get { return __Buttons; } } private List<Button> __Buttons;
        /// <summary>
        /// Stringový klíč, jednoznačně určuje sadu buttonů. Používá se při tvorbě klíče do <see cref="DxRepositoryEditor"/>.
        /// </summary>
        public string Key
        {
            get
            {
                var bs = BorderStyle;
                return (bs == DevExpress.XtraEditors.Controls.BorderStyles.NoBorder ? "" : bs.ToString() + ":") + Buttons.ToOneString(":", b => b.Key);
            }
        }
        /// <summary>
        /// Vytvoří a vrátí pole obsahující buttony DevExpress, použitelné do nativního controlu,
        /// např. do <see cref="DevExpress.XtraEditors.ButtonEdit.Properties"/>.Buttons
        /// </summary>
        /// <returns></returns>
        public DevExpress.XtraEditors.Controls.EditorButton[] CreateDxButtons()
        {
            return this.Buttons.Select(b => b.CreateDxButton()).ToArray();
        }
        /// <summary>
        /// Třída definující jeden button
        /// </summary>
        public class Button
        {
            /// <summary>
            /// Konstruktor prázdný
            /// </summary>
            public Button()
            {
                Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.OK;
                ImageName = null;
                Alignment = BarItemAlignment.Right;
                Text = "";
                IsDefaultButton = false;
                ToolTipText = null;
                KeyShortcut = null;
            }
            /// <summary>
            /// Konstruktor pro jednoduše definovaný button
            /// </summary>
            /// <param name="kind"></param>
            public Button(DevExpress.XtraEditors.Controls.ButtonPredefines kind)
                : this()
            {
                Kind = kind;
            }
            /// <summary>
            /// Konstruktor pro jednoduše definovaný button
            /// </summary>
            /// <param name="imageName"></param>
            public Button(string imageName)
                : this()
            {
                Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                ImageName = imageName;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.CurrentActionName;
            }
            /// <summary>
            /// Předdefinovaný druh obrázku
            /// </summary>
            public DevExpress.XtraEditors.Controls.ButtonPredefines Kind { get; set; }
            /// <summary>
            /// Jméno obrázku
            /// </summary>
            public string ImageName { get; set; }
            /// <summary>
            /// Umístění vlevo nebo vpravo, vpravo je default
            /// </summary>
            public BarItemAlignment Alignment { get; set; }
            /// <summary>
            /// Text (cože?)
            /// </summary>
            public string Text { get; set; }
            /// <summary>
            /// Jde o default button?
            /// </summary>
            public bool IsDefaultButton { get; set; }
            /// <summary>
            /// Text tooltipu
            /// </summary>
            public string ToolTipText { get; set; }
            /// <summary>
            /// Klávesová zkratka
            /// </summary>
            public DevExpress.Utils.KeyShortcut KeyShortcut { get; set; }
            /// <summary>
            /// Jméno akce = je předáno do datové vrstvy po kliknutí na Button.
            /// Pokud není uvedeno, předává se <see cref="Kind"/> nebo <see cref="ImageName"/>.
            /// </summary>
            public string ActionName { get; set; }
            /// <summary>
            /// Jméno akce reálně použití pro Button (priorita: <see cref="ActionName"/> - <see cref="ImageName"/> - <see cref="Kind"/>)
            /// </summary>
            public string CurrentActionName 
            { 
                get
                {
                    if (!String.IsNullOrEmpty(this.ActionName)) return this.ActionName;
                    if (!String.IsNullOrEmpty(this.ImageName)) return this.ImageName;
                    return this.Kind.ToString();
                }
            }
            /// <summary>
            /// Stringový klíč, jednoznačně určuje sadu buttonů. Používá se při tvorbě klíče do <see cref="DxRepositoryEditor"/>.
            /// </summary>
            public string Key { get { return $"{CurrentActionName}{(Alignment == BarItemAlignment.Left ? ".L" : "")}"; } }
            /// <summary>
            /// Metoda vytvoří a vrátí new instanci buttonu ze své definice
            /// </summary>
            /// <returns></returns>
            public DevExpress.XtraEditors.Controls.EditorButton CreateDxButton()
            {
                var dxButton = new DevExpress.XtraEditors.Controls.EditorButton();

                dxButton.IsLeft = (this.Alignment == BarItemAlignment.Left);
                dxButton.Caption = this.Text;
                dxButton.IsDefaultButton = this.IsDefaultButton;
                if (String.IsNullOrEmpty(this.ImageName))
                    // Predefined kind:
                    dxButton.Kind = this.Kind;
                else
                {   // User defined:
                    dxButton.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                    DxComponent.ApplyImage(dxButton.ImageOptions, this.ImageName, sizeType: ResourceImageSizeType.Small);
                }
                dxButton.ToolTip = this.ToolTipText;
                dxButton.Shortcut = this.KeyShortcut;
                dxButton.Tag = this.CurrentActionName;

                return dxButton;
            }
        }
    }
    #endregion
    #region ImageComboBoxProperties : definice sady položek pro ComboBox
    /// <summary>
    /// Třída definující sadu položek pro ComboBox
    /// </summary>
    public class ImageComboBoxProperties
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ImageComboBoxProperties()
        {
            __Items = new List<Item>();
            AutoComplete = true;
            AllowDropDownWhenReadOnly = true;
            ImmediatePopup = true;
            PopupSizeable = true;
        }
        /// <summary>
        /// Konstruktor pro jednoduchou definici.
        /// Jednotlivé itemy jsou odděleny znakem ;
        /// Uvnitř itemu jsou uvedeny Value,DisplayText,ImageName   oddělené znakem ,
        /// </summary>
        /// <param name="simpleDefinition"></param>
        public ImageComboBoxProperties(string simpleDefinition)
            : this()
        {
            if (!String.IsNullOrEmpty(simpleDefinition))
            {
                var items = simpleDefinition.Split(';');
                foreach (var item in items)
                {
                    if (!String.IsNullOrEmpty(item))
                    {
                        var parts = item.Split(',');
                        if (parts.Length >= 2 && !String.IsNullOrEmpty(parts[1]))
                            this.__Items.Add(new Item(parts[0], parts[1], (parts.Length >= 3 ? parts[2] : null)));
                    }
                }
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.__Items.ToOneString(";");
        }
        /// <summary>
        /// Povolit vlastnost AutoComplete
        /// </summary>
        public bool AutoComplete { get; set; }
        /// <summary>
        /// Bude možno zobrazit DropDown, když Combo je ReadOnly?
        /// </summary>
        public bool AllowDropDownWhenReadOnly { get; set; }
        /// <summary>
        /// Zobrazit Popup hned po začátku psaní textu
        /// </summary>
        public bool ImmediatePopup { get; set; }
        /// <summary>
        /// Popup je resizovatelné
        /// </summary>
        public bool PopupSizeable { get; set; }
        /// <summary>
        /// Pole prvků
        /// </summary>
        public List<Item> Items { get { return __Items; } } private List<Item> __Items;
        /// <summary>
        /// Vytvoří a vrátí pole obsahující prvky <see cref="DevExpress.XtraEditors.Controls.ComboBoxItem"/> (typu DevExpress), použitelné do nativního controlu,
        /// např. do <see cref="DevExpress.XtraEditors.ComboBoxEdit.Properties"/>.Items
        /// </summary>
        /// <returns></returns>
        public DevExpress.XtraEditors.Controls.ComboBoxItem[] CreateDxComboItems()
        {
            return this.Items.Select(i => i.CreateDxComboItem()).ToArray();
        }
        /// <summary>
        /// Vytvoří a vrátí pole obsahující prvky <see cref="DevExpress.XtraEditors.Controls.ImageComboBoxItem"/> (typu DevExpress), použitelné do nativního controlu,
        /// např. do <see cref="DevExpress.XtraEditors.ImageComboBoxEdit.Properties"/>.Items
        /// </summary>
        /// <param name="imageList">Out ImageList, na který se vztahují indexy obrázků</param>
        /// <param name="imageSize">Požadovaná velikost obrázků</param>
        /// <returns></returns>
        public DevExpress.XtraEditors.Controls.ImageComboBoxItem[] CreateDxImageComboItems(out object imageList, ResourceImageSizeType imageSize = ResourceImageSizeType.Small)
        {
            imageList = DxComponent.GetPreferredImageList(imageSize);
            return this.Items.Select(i => i.CreateDxImageComboItem(imageSize)).ToArray();
        }
        /// <summary>
        /// Metoda najde a vrátí index prvku, který má zadanou hodnotu (CodeValue).
        /// Pokud na vstupu je null, anebo hodnota kterou nemá žádná zdejší položka, pak vrátí -1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetIndexOfValue(object value)
        {
            return ((value != null && this.Items.TryFindFirstIndex(i => Object.Equals(i.Value, value), out int foundIndex)) ? foundIndex : -1);
        }
        /// <summary>
        /// Třída definující jeden prvek ComboBoxu
        /// </summary>
        public class Item
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public Item()
            { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            public Item(object value, string displayText, string imageName = null)
            {
                this.Value = value;
                this.DisplayText = displayText;
                this.ImageName = imageName;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.DisplayText;
            }
            /// <summary>
            /// CodeValue
            /// </summary>
            public object Value { get; set; }
            /// <summary>
            /// DisplayValue = Text
            /// </summary>
            public string DisplayText { get; set; }
            /// <summary>
            /// Jméno obrázku
            /// </summary>
            public string ImageName { get; set; }
            /// <summary>
            /// Metoda vytvoří a vrátí new instanci <see cref="DevExpress.XtraEditors.Controls.ComboBoxItem"/> ze své definice
            /// </summary>
            /// <returns></returns>
            public DevExpress.XtraEditors.Controls.ComboBoxItem CreateDxComboItem()
            {
                var dxItem = new DevExpress.XtraEditors.Controls.ComboBoxItem();
                dxItem.Value = this;
                return dxItem;
            }
            /// <summary>
            /// Metoda vytvoří a vrátí new instanci <see cref="DevExpress.XtraEditors.Controls.ImageComboBoxItem"/> ze své definice
            /// </summary>
            /// <returns></returns>
            public DevExpress.XtraEditors.Controls.ImageComboBoxItem CreateDxImageComboItem(ResourceImageSizeType imageSize = ResourceImageSizeType.Small)
            {
                var dxItem = new DevExpress.XtraEditors.Controls.ImageComboBoxItem();
                dxItem.Value = this.Value;
                dxItem.Description = this.DisplayText;
                dxItem.ImageIndex = DxComponent.GetPreferredImageIndex(this.ImageName, imageSize);
                return dxItem;
            }
        }
    }
    #endregion
    #endregion
}
