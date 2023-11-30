// Supervisor: David Janáček, od 01.02.2021
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
    public class DataFormRow : IChildOfParent<DataFormRows>  // ChildItems<DataFormRow>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormRow()
        {
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
    #region DataFormCell : jeden interaktivní prvek
    /// <summary>
    /// Reprezentuje jednu buňku = jeden fyzický prvek odpovídající prvku layoutu na jednom konkrétním řádku
    /// </summary>
    public class DataFormCell : IInteractiveItem
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
            this.__IsVisible = true;
            this.__IsInteractive = true;
            this.__DesignBounds = layoutItem.DesignBounds.Add(row.RowDesignBounds.Location);
            this.__InteractiveState = DxInteractiveState.Enabled;
        }
        private DataFormRow __Row;
        private DataFormLayoutItem __LayoutItem;
        private WinDraw.Rectangle __DesignBounds;
        private bool __IsVisible;
        private bool __IsInteractive;
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
        private DxRepositoryManager RepositoryManager { get { return __Row.DataForm?.RepositoryManager; } }
        #endregion

        /// <summary>
        /// Vrátí true, pokud this prvek je aktivní na dané Control nebo Designové souřadnici (objekt si sám vybere, kterou souřadnici bude vyhodnocovat).
        /// </summary>
        /// <param name="controlPoint">Souřadnice fyzická na controlu</param>
        /// <param name="designPoint">Souřadnice designová</param>
        /// <returns></returns>
        public bool _IsActiveOnPoint(WinDraw.Point controlPoint, WinDraw.Point designPoint)
        {
            return this.__IsInteractive && this.__DesignBounds.Contains(designPoint);
        }

        private bool _Paint(PaintDataEventArgs pdea)
        {
            var controlBounds = pdea.InteractivePanel.GetControlBounds(this.__DesignBounds);
            if (!pdea.ClientArea.IntersectsWith(controlBounds)) return false;            // Pokud vykreslovaný control a naše souřadnice nemají nic společného, pak nebudeme kreslit...

            WinDraw.Color color = WinDraw.Color.LightYellow;
            switch (this.__InteractiveState)
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
            return true;
        }

        private void _SetInteractiveState(DxInteractiveState interactiveState)
        {
            var oldState = __InteractiveState;
            if (interactiveState == oldState) return;

            __InteractiveState = interactiveState;

            if (oldState == DxInteractiveState.MouseLeftDown)
            { }
        }

        #region IInteractiveItem
        bool IInteractiveItem.IsVisible { get { return __IsVisible; } }
        bool IInteractiveItem.IsInteractive { get { return __IsInteractive; } }
        WinDraw.Rectangle IInteractiveItem.DesignBounds { get { return __DesignBounds; } }
        DxInteractiveState IInteractiveItem.InteractiveState { get { return __InteractiveState; } set { _SetInteractiveState(value); } }
        bool IInteractiveItem.Paint(PaintDataEventArgs pdea) { return this._Paint(pdea); }
        bool IInteractiveItem.IsActiveOnPoint(WinDraw.Point controlPoint, WinDraw.Point designPoint) { return this._IsActiveOnPoint(controlPoint, designPoint); }
        DxInteractivePanel IChildOfParent<DxInteractivePanel>.Parent { get { return null; } set { } }
        #endregion
    }
    #endregion
    #region DataFormLayoutSet + DataFormLayoutItem : Definice layoutu jednoho řádku (celá sada + jeden prvek)
    /// <summary>
    /// Sada definující layxout DataFormu = jednotlivé prvky
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
    public class DataFormLayoutItem : IChildOfParent<DataFormLayoutSet>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormLayoutItem()
        {
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
        /// ID tohoto sloupce. K němu se dohledají další data a případné modifikace stylu v konkrétním řádku.
        /// </summary>
        public string ColumnId { get; set; }
        /// <summary>
        /// Druh vstupního prvku
        /// </summary>
        public DxRepositoryEditorType ColumnType { get; set; }
        /// <summary>
        /// Konstantní text (pro Label, Button, atd)
        /// </summary>
        public string LabelText { get; set; }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible { get; set; }
        /// <summary>
        /// Souřadnice definované pomocí vzdáleností / rozměrů, a tedy libovolně ukotvené.
        /// Viz metoda <see cref="RectangleExt.GetBounds(WinDraw.Rectangle)"/>
        /// </summary>
        public RectangleExt DesignBoundsExt { get; set; }
        /// <summary>
        /// Aktuální přidělená souřadnice v metodě <see cref="PrepareDesignSize(WinDraw.Rectangle, ref int, ref int, ref int, ref int)"/>
        /// </summary>
        public WinDraw.Rectangle DesignBounds { get; private set; }
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
            DesignBounds = designBounds;
            if (right < designBounds.Right) right = designBounds.Right;
            if (bottom < designBounds.Bottom) bottom = designBounds.Bottom;
        }
    }
    #endregion
}
