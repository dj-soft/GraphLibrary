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

using Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using Noris.Clients.Win.Components.AsolDX.DxForm;
using System.Drawing;

namespace Noris.Clients.Win.Components.AsolDX.DataForm.Layout
{
    #region DataFormLayoutSet + DataFormLayoutItem : Definice layoutu jednoho řádku (celá sada + jeden prvek). Layout je hierarchický.
    /// <summary>
    /// Sada definující layout celého formuláře = všechny containery a jejich prvky, plus vlastnosti šablony z její hlavičky
    /// </summary>
    internal class LayoutForm : LayoutContainer
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        internal LayoutForm(DxDataForm dataForm, DfForm template)
            : base(dataForm)
        {
            __DataForm = dataForm;
            __Template = template;
            // _InitItems();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {this.Count}; ItemType: '{(typeof(LayoutControl).FullName)}'";
        }
        /// <summary>
        /// Datový základ DataFormu
        /// </summary>
        internal DxDataForm DataForm { get { return __DataForm; } } private DxDataForm __DataForm;
        /// <summary>
        /// Definice šablony
        /// </summary>
        internal DfForm Template { get { return __Template; } } private DfForm __Template;
        /// <summary>
        /// Vizuální control <see cref="DxDataFormPanel"/> = virtuální hostitel obsahující Scrollbary a <see cref="DxDataFormContentPanel"/>
        /// </summary>
        internal DxDataFormPanel DataFormPanel { get { return __DataForm?.DataFormPanel; } }
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        internal DxDataFormContentPanel DataFormContent { get { return __DataForm?.DataFormContent; } }
        /// <summary>
        /// Zajistí vykreslení obsahu Dataformu
        /// </summary>
        internal void DataFormDraw() { DataFormContent?.Draw(); }
        #endregion
    }
    /// <summary>
    /// Sada definující layout DataFormu = jednotlivé prvky
    /// </summary>
    internal class LayoutContainer : IList<LayoutItem>
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        internal LayoutContainer(DxDataForm dataForm)
        {
            __DataForm = dataForm;
            _InitItems();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {this.Count}; ItemType: '{(typeof(LayoutControl).FullName)}'";
        }
        /// <summary>
        /// Datový základ DataFormu
        /// </summary>
        internal DxDataForm DataForm { get { return __DataForm; } } private DxDataForm __DataForm;
        /// <summary>
        /// Vizuální control <see cref="DxDataFormPanel"/> = virtuální hostitel obsahující Scrollbary a <see cref="DxDataFormContentPanel"/>
        /// </summary>
        internal DxDataFormPanel DataFormPanel { get { return __DataForm?.DataFormPanel; } }
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        internal DxDataFormContentPanel DataFormContent { get { return __DataForm?.DataFormContent; } }
        /// <summary>
        /// Zajistí vykreslení obsahu Dataformu
        /// </summary>
        internal void DataFormDraw() { DataFormContent?.Draw(); }
        #endregion
        #region Jednotlivé prvky definice
        /// <summary>
        /// Pole prvků definice
        /// </summary>
        internal IList<LayoutItem> Items { get { return __Items; } }
        /// <summary>
        /// Inicializace řádků
        /// </summary>
        private void _InitItems()
        {
            __Items = new ChildItems<LayoutContainer, LayoutItem>(this);
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
        /// Zavolá metody <see cref="OnCollectionChanged(EventArgs)"/> a event <see cref="CollectionChanged"/>.
        /// </summary>
        private void _RunCollectionChanged()
        {
            this.InvalidateDesignSize();
            this.InvalidateTabOrders();
            var args = EventArgs.Empty;
            OnCollectionChanged(args);
            CollectionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Obecná událost volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        internal event EventHandler CollectionChanged;
        /// <summary>
        /// Metoda volaná poté, kdy se přidal nebo odebral prvek kolekce (Add/Remove).
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollectionChanged(EventArgs e) { }
        /// <summary>
        /// Pole prvků definice
        /// </summary>
        private ChildItems<LayoutContainer, LayoutItem> __Items;
        #endregion
        #region TabOrder: pořadí prvků
        /// <summary>
        /// Obsahuje soupis všech prků layoutu seřazený podle jejich pořadí TabOrder.
        /// </summary>
        public LayoutItem[] TabOrderItems;
        /// <summary>
        /// Invaliduje pole prvků uspořádaných dle jejich pořadí
        /// </summary>
        public void InvalidateTabOrders()
        {
            __TabOrderItems = null;
        }

        private LayoutItem[] __TabOrderItems;
        #endregion
        #region DesignSize : celková designová velikost prvků v této definici
        /// <summary>
        /// Designová velikost panelu hostitele = prostor viditelný v rámci fyzického controlu, přepočtený na designové pixely
        /// </summary>
        internal WinDraw.Size? HostDesignSize
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
        internal bool IsDesignSizeDependOnHostSize { get { return false; } }
        /// <summary>
        /// Okraje kolem definice jednoho řádku
        /// </summary>
        internal WinForm.Padding Padding { get { return __Padding; } set { __Padding = value; InvalidateDesignSize(); } }
        /// <summary>
        /// Sumární velikost definice pro jeden řádek. Validovaná hodnota.
        /// </summary>
        internal WinDraw.Size DesignSize { get { return _GetDesignSize(); } }
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
        internal void AddRange(IEnumerable<LayoutItem> items)
        {
            __Items.AddRange(items);
        }
        /// <summary>
        /// Store
        /// </summary>
        /// <param name="items"></param>
        internal void Store(IEnumerable<LayoutItem> items)
        {
            __Items.Clear();
            __Items.AddRange(items);
        }
        #endregion
        #region IList
        /// <summary>
        /// Count
        /// </summary>
        public int Count => ((ICollection<LayoutItem>)__Items).Count;
        /// <summary>
        /// IsReadOnly
        /// </summary>
        public bool IsReadOnly => ((ICollection<LayoutItem>)__Items).IsReadOnly;
        /// <summary>
        /// this
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public LayoutItem this[int index] { get => ((IList<LayoutItem>)__Items)[index]; set => ((IList<LayoutItem>)__Items)[index] = value; }
        /// <summary>
        /// IndexOf
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(LayoutItem item)
        {
            return ((IList<LayoutItem>)__Items).IndexOf(item);
        }
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, LayoutItem item)
        {
            ((IList<LayoutItem>)__Items).Insert(index, item);
        }
        /// <summary>
        /// RemoveAt
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            ((IList<LayoutItem>)__Items).RemoveAt(index);
        }
        /// <summary>
        /// Add
        /// </summary>
        /// <param name="item"></param>
        public void Add(LayoutItem item)
        {
            ((ICollection<LayoutItem>)__Items).Add(item);
        }
        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            ((ICollection<LayoutItem>)__Items).Clear();
        }
        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(LayoutItem item)
        {
            return ((ICollection<LayoutItem>)__Items).Contains(item);
        }
        /// <summary>
        /// CopyTo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(LayoutItem[] array, int arrayIndex)
        {
            ((ICollection<LayoutItem>)__Items).CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(LayoutItem item)
        {
            return ((ICollection<LayoutItem>)__Items).Remove(item);
        }
        /// <summary>
        /// GetEnumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<LayoutItem> GetEnumerator()
        {
            return ((IEnumerable<LayoutItem>)__Items).GetEnumerator();
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
    internal class LayoutControl : LayoutItem, IDataFormLayoutDesignItem, IChildOfParent<LayoutContainer>
    {
        #region Konstruktor a fixní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        internal LayoutControl()
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
        LayoutContainer IChildOfParent<LayoutContainer>.Parent { get { return __Parent; } set { __Parent = value; } } private LayoutContainer __Parent;
        /// <summary>
        /// Panel dataformu
        /// </summary>
        internal DxDataFormPanel DataForm { get { return __Parent?.DataFormPanel; } }
        /// <summary>
        /// Jméno tohoto sloupce. K němu se dohledají další data a případné modifikace stylu v konkrétním řádku.
        /// </summary>
        internal string ColumnName { get; set; }
        /// <summary>
        /// Druh vstupního prvku
        /// </summary>
        internal DxRepositoryEditorType ColumnType { get; set; }
        /// <summary>
        /// Souřadnice definované pomocí vzdáleností / rozměrů, a tedy libovolně ukotvené.
        /// Viz metoda <see cref="RectangleExt.GetBounds(WinDraw.Rectangle)"/>
        /// </summary>
        internal RectangleExt DesignBoundsExt { get; set; }
        /// <summary>
        /// Obsah řádků: obsahuje sloupce i jejich datové a popisné hodnoty.
        /// Klíčem je název sloupce.
        /// </summary>
        internal DataContent Content { get { return __Content; } } private DataContent __Content;
        #endregion
        #region Předpřipravené hodnoty obecně dostupné, získávané z Content
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        internal string Label
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.Label, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.Label, value); }
        }
        /// <summary>
        /// ToolTipText
        /// </summary>
        internal string ToolTipText
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.ToolTipText, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.ToolTipText, value); }
        }
        /// <summary>
        /// Label prvku = fixní text
        /// </summary>
        internal string IconName
        {
            get { return ((TryGetContent<string>(DxDataFormProperty.IconName, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.IconName, value); }
        }
        /// <summary>
        /// Typ kurzoru, který bude aktivován po najetí myší na aktivní prvek
        /// </summary>
        internal CursorTypes? CursorTypeMouseOn
        {
            get { return ((TryGetContent<CursorTypes?>(DxDataFormProperty.CursorTypeMouseOn, out var content)) ? content : null); }
            set { SetContent(DxDataFormProperty.CursorTypeMouseOn, value); }
        }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        internal bool IsVisible
        {
            get { return ((TryGetContent<bool?>(DxDataFormProperty.IsVisible, out var content) && content.HasValue) ? content.Value : true); }
            set { SetContent(DxDataFormProperty.IsVisible, (bool?)value); }
        }
        /// <summary>
        /// Prvek je interaktvní?
        /// </summary>
        internal bool IsInteractive
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
        internal bool TryGetContent<T>(DxDataFormProperty property, out T content)
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
        internal void SetContent<T>(DxDataFormProperty property, T content)
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
        internal override void PrepareDesignSize(WinDraw.Rectangle parentBounds, ref int left, ref int top, ref int right, ref int bottom)
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
    /// Jeden fyzicky umístěný a viditelný prvek v layoutu. Předek pro Container i Control.
    /// Obsahuje prvky, které dovolují sestavit layout celého DataFormu.
    /// Podporuje hierarchické skládání DataFormu (vnořované containery) i jejich následné lineární zpracování.
    /// </summary>
    internal class LayoutItem : IChildOfParent<LayoutContainer>, IDataFormLayoutDesignItem
    {
        /// <summary>
        /// Parent
        /// </summary>
        LayoutContainer IChildOfParent<LayoutContainer>.Parent { get { return __Parent; } set { __Parent = value; } } private LayoutContainer __Parent;
        /// <summary>
        /// Aktuální designové souřadnice
        /// </summary>
        Rectangle IDataFormLayoutDesignItem.CurrentDesignBounds { get { return AbsoluteBounds ?? Rectangle.Empty; } }
        /// <summary>
        /// Určí aktuální designovou souřadnici v prostoru daného parenta
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        internal virtual void PrepareDesignSize(WinDraw.Rectangle parentBounds, ref int left, ref int top, ref int right, ref int bottom)
        {
            //var designBounds = this.DesignBoundsExt.GetBounds(parentBounds);
            //__CurrentDesignBounds = designBounds;
            //if (right < designBounds.Right) right = designBounds.Right;
            //if (bottom < designBounds.Bottom) bottom = designBounds.Bottom;
        }
        /// <summary>
        /// Souřadnice prvku dané layoutem. Některé jednotlivé hodnoty nebo i celá souřadnice mohou být null.
        /// </summary>
        public DesignBounds LayoutBounds { get; set; }
        /// <summary>
        /// Souřadnice prvku relativní ke svému parentu, dopočítané, a víceméně neměnné, vždy validní
        /// </summary>
        public Rectangle RelativeBounds { get; set; }
        /// <summary>
        /// Absolutní souřadnice v rámci celého DataFormu. 
        /// Může být null pro prvky, které nemohou být vidět = jejich <see cref="IsDisplayed"/> je false.
        /// </summary>
        public Rectangle? AbsoluteBounds { get; set; }
        /// <summary>
        /// Prvek je zobrazován. 
        /// Hodnotu false mají prvky na neaktivní stránce nebo na Collapsed panelu.
        /// Hodnota true říká, že na prvek může být scrollováno - tedy prvek může být zobrazen uživateli.
        /// </summary>
        public bool IsDisplayed { get; set; }
        /// <summary>
        /// Definice Controlu / Containeru.
        /// Protože jeden <see cref="DfItem"/> může reprezentovat Input control s Labelem (<see cref="DfBaseLabeledInputControl"/> a potomci),
        /// ale zdejší <see cref="LayoutItem"/> reprezentuje vždy striktně jeden vizuální prvek, pak pro jeden definiční prvek <see cref="DfBaseLabeledInputControl"/>
        /// mohou být vytvořeny dva vizuální prvky <see cref="LayoutItem"/> a sdílet tedy jeden <see cref="DfItem"/> !!!
        /// </summary>
        public DfBase DfItem { get; set; }
    }
    /// <summary>
    /// Interface umožňující přístup na designová pracovní data prvku layoutu
    /// </summary>
    internal interface IDataFormLayoutDesignItem
    {
        /// <summary>
        /// Aktuální designové souřadnice
        /// </summary>
        Rectangle CurrentDesignBounds { get; }
    }
    #endregion
}
