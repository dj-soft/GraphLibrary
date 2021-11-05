// Supervisor: David Janáček, od 23.01.2020
// Part of Helios Green, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using WinForm = System.Windows.Forms;

using NCC = Noris.Clients.Controllers;
using DXN = DevExpress.XtraBars.Navigation;
using Noris.Clients.Win.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress
{
    #region class TabHeaderStrip : Adapter na lištu se záložkami. Vytváří adapter na fyzické komponenty Infragistic / DevExpress, podle požadavku.
    /// <summary>
    /// Adapter na lištu se záložkami.
    /// Vytváří se statickou metodou <see cref="Create(TabHeaderStrip.HeaderType?, IEnumerable{NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo})"/>.
    /// Vlastní control je pak k dispozici v property 
    /// </summary>
    internal class TabHeaderStrip : IInfragisticsDevExpressSkinableSupport, IDisposable
    {
        #region Vytváření instance
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="TabHeaderStrip"/> typu <see cref="HeaderType.DevExpressTop"/>
        /// </summary>
        /// <returns></returns>
        internal static TabHeaderStrip Create(HeaderType? headerType = null, IEnumerable<NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo> labeledTabs = null)
        {
            if (!headerType.HasValue) headerType = _DefaultHeaderType;
            return _CreateByType(headerType.Value, labeledTabs);
        }
        /// <summary>
        /// Defaultní typ headeru podle klávesnice: Shift = Infragistic, ScrollLock = DevExpressLeft, jinak DevExpressTop
        /// </summary>
        private static HeaderType _DefaultHeaderType
        {
            get
            {
                if (!System.Diagnostics.Debugger.IsAttached) return HeaderType.DevExpressTop;

                if (WinForm.Control.ModifierKeys == WinForm.Keys.Shift) return HeaderType.DevExpressLeft;
                if (WinForm.Control.ModifierKeys == WinForm.Keys.Control) return HeaderType.Infragistic; 
                return HeaderType.DevExpressTop;
            }
        }
        private TabHeaderStrip()
        {
            _HeaderItems = new Dictionary<string, TabHeaderItem>();
            _IsDisposed = false;
        }
        /// <summary>
        /// Fyzicky vytvoří instanci
        /// </summary>
        /// <param name="headerType"></param>
        /// <param name="labeledTabs"></param>
        /// <returns></returns>
        private static TabHeaderStrip _CreateByType(HeaderType headerType, IEnumerable<NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo> labeledTabs = null)
        {
            TabHeaderStrip headerStrip = new TabHeaderStrip();
            switch (headerType)
            {
                case HeaderType.DevExpressTop:
                    headerStrip._Component = new TabHeaderStripDXTop(headerStrip);
                    break;
                case HeaderType.DevExpressLeft:
                    headerStrip._Component = new TabHeaderStripDXLeft(headerStrip);
                    break;
                case HeaderType.Infragistic:
#warning VYŘAZENO -    TabHeaderStripIG(headerStrip)
                    //  headerStrip._Component = new TabHeaderStripIG(headerStrip);
                    break;
                default:
                    throw new System.ArgumentException("TabHeaderStrip.Create(headerType) is null", "headerType");
            }
            headerStrip._Initialize();
            if (!(labeledTabs is null)) headerStrip.AddItems(TabHeaderItem.CreateItems(labeledTabs));
            headerStrip._RegisterEvents();
            return headerStrip;
        }
        /// <summary>
        /// Instance komponenty
        /// </summary>
        private ITabHeaderStrip _Component;
        /// <summary>
        /// Eventhandlery jsou aktivní
        /// </summary>
        private bool _EventsActive;
        /// <summary>
        /// Inicializace this instance a instance komponenty
        /// </summary>
        private void _Initialize()
        {
            _Component.Initialise();
            _Component.ImageTextMode = ImageTextMode.ImageAndText;
        }
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na eventy komponenty
        /// </summary>
        private void _RegisterEvents()
        {
            _Component.SelectedTabChanging += _Component_SelectedKeyChanging;
            _Component.SelectedTabChanged += _Component_SelectedKeyChanged;
            _Component.HeaderSizeChanged += _Component_HeaderSizeChanged;
            _EventsActive = true;
        }
        /// <summary>
        /// Eventhandler při změně záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Component_SelectedKeyChanging(object sender, ValueChangingArgs<string> e)
        {
            this.SelectedTabChanging?.Invoke(this, e);
        }
        /// <summary>
        /// Eventhandler po změně záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Component_SelectedKeyChanged(object sender, ValueChangedArgs<string> e)
        {
            this.SelectedTabChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Eventhandler po změně velikosti záhlaví
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Component_HeaderSizeChanged(object sender, ValueChangedArgs<Size> e)
        {
            this.HeaderSizeChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Typ headeru, defaultní je <see cref="DevExpressTop"/>
        /// </summary>
        internal enum HeaderType
        {
            /// <summary>
            /// DevExpress, nahoře
            /// </summary>
            DevExpressTop,
            /// <summary>
            /// DevExpress, vlevo
            /// </summary>
            DevExpressLeft,
            /// <summary>
            /// Infragistic
            /// </summary>
            Infragistic
        }
        /// <summary>
        /// Po změně Skinu dáme vědět komponentě
        /// </summary>
        /// <param name="arg"></param>
        void IInfragisticsDevExpressSkinableSupport.DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg)
        {
            _Component.DevexpressSkinChanged(arg);
        }
        /// <summary>
        /// Dispose instance provede Dispose patřičných komponent.
        /// Dispose probíhá v režimu <see cref="SilentScope(bool)"/>.
        /// </summary>
        public void Dispose()
        {
            using (SilentScope())
            {
                if (_Component != null)
                {
                    _Component.Dispose();
                    _Component = null;
                }
                if (_HeaderItems != null)
                {
                    foreach (var headerItem in _HeaderItems.Values)
                        ((IDisposable)headerItem).Dispose();
                    _HeaderItems.Clear();
                    _HeaderItems = null;
                }
                _IsDisposed = true;
            }
        }
        /// <summary>
        /// Obsahuje true poté, co proběhl <see cref="Dispose()"/>
        /// </summary>
        public bool IsDisposed { get { return _IsDisposed; } }
        private bool _IsDisposed;
        #endregion
        #region Práce se záložkami (=dovnitř)
        /// <summary>
        /// Sada záložek
        /// </summary>
        public TabHeaderItem[] Items { get { return _HeaderItems.Values.ToArray(); } }
        /// <summary>
        /// Přidá záložky
        /// </summary>
        /// <param name="labeledTabs"></param>
        public void AddItems(IEnumerable<NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo> labeledTabs) { AddItems(TabHeaderItem.CreateItems(labeledTabs)); }
        /// <summary>
        /// Vytvoří vizuální taby z dodaného seznamu
        /// </summary>
        /// <param name="headerItems"></param>
        public void AddItems(IEnumerable<TabHeaderItem> headerItems)
        {
            if (headerItems is null)
                throw new System.ArgumentNullException("headerItems", "TabHeaderStrip.AddHeaderItems(headerItems) is null");
            foreach (var headerItem in headerItems)
                this.AddItem(headerItem);
        }
        /// <summary>
        /// Vytvoří jeden vizuální tab z dodaného prvku
        /// </summary>
        /// <param name="labeledTab"></param>
        public void AddItem(NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo labeledTab)
        {
            if (labeledTab == null)
                throw new System.ArgumentNullException("labeledTab", "TabHeaderStrip.AddHeaderItem(labeledTab) is null");
            AddItem(TabHeaderItem.CreateItem(labeledTab));
        }
        /// <summary>
        /// Vytvoří jeden vizuální tab z dodaného prvku
        /// </summary>
        /// <param name="headerItem"></param>
        public void AddItem(TabHeaderItem headerItem)
        {
            if (headerItem == null)
                throw new System.ArgumentNullException("headerItem", "TabHeaderStrip.AddHeaderItem(headerItem) is null");
            if (headerItem.Key == null)
                throw new System.ArgumentNullException("TabHeaderItem.Key", "TabHeaderStrip.AddHeaderItem(headerItem) Key is null");
            if (this._HeaderItems.ContainsKey(headerItem.Key))
                throw new System.ArgumentException($"TabHeaderStrip.AddHeaderItem(headerItem) Key {headerItem.Key} is duplicite", "TabHeaderItem.Key");

            this._Component.AddHeader(headerItem);
            ((IMember<TabHeaderStrip>)headerItem).Owner = this;
            this._HeaderItems.Add(headerItem.Key, headerItem);
        }
        /// <summary>
        /// Najde a odebere položku
        /// </summary>
        /// <param name="key"></param>
        public void RemoveItem(string key)
        {
            _Component.RemoveHeader(key);

            if (key != null && _HeaderItems.TryGetValue(key, out var headerItem))
            {
                ((IDisposable)headerItem).Dispose();
                _HeaderItems.Remove(key);
            }
        }
        /// <summary>
        /// Odebere všechny záložky
        /// </summary>
        internal void Clear()
        {
            _Component.ClearHeaders();

            foreach (var headerItem in _HeaderItems.Values)
                ((IDisposable)headerItem).Dispose();
            _HeaderItems.Clear();
        }
        /// <summary>
        /// Dictionary obsahující data záložek
        /// </summary>
        protected Dictionary<string, TabHeaderItem> _HeaderItems;
        #endregion
        #region Práce s instancí (=zvenku)
        /// <summary>
        /// Fyzický control, který se bude vkládat do okna, a který reprezentuje sadu záložek
        /// </summary>
        public WinForm.Control Control { get { return _Component.Control; } }
        /// <summary>
        /// Zarovnání obsahu = tabů (doleva/doprava, nahoru/dolů
        /// </summary>
        public ContentAlignment ContentAlignment { get { return _Component.ContentAlignment; } set { _Component.ContentAlignment = value; } }
        /// <summary>
        /// Režim zobrazení ikony a textu pro tuto položku, default = <see cref="ImageTextMode.ImageAndText"/>
        /// </summary>
        public ImageTextMode ImageTextMode { get { return _Component.ImageTextMode; } set { _Component.ImageTextMode = value; } }
        /// <summary>
        /// Doporučovaný styl dokování
        /// </summary>
        public WinForm.DockStyle OptimalDockStyle { get { return _Component.OptimalDockStyle; } }
        /// <summary>
        /// Doporučená velikost, počet pixelů od dokované hrany (pro Top/Bottom je zde Height, pro Left/Right je zde Width)
        /// </summary>
        public int OptimalSize { get { return _Component.OptimalSize; } }
        /// <summary>
        /// Aktuálně vybraná záložka, její data.
        /// </summary>
        public TabHeaderItem ActiveTab
        {
            get
            {   // Najdu aktuální klíč v komponentě a vrátím odpovídající prvek (tím ověřím, že prvek máme):
                string key = _SelectedTabKey;
                return ((key != null && _HeaderItems.TryGetValue(key, out TabHeaderItem item)) ? item : null);
            }
            set { ActiveKey = value?.Key; }
        }
        /// <summary>
        /// Aktuálně vybraná záložka, její klíč.
        /// </summary>
        public string ActiveKey
        {
            get { TabHeaderItem activeTab = ActiveTab; return activeTab?.Key; }
            set
            {   // Setovat mohu jen klíč položky, kterou mám v seznamu:
                TabHeaderItem item = this[value];
                _SelectedTabKey = item?.Key;
            }
        }
        /// <summary>
        /// Aktuálně vybraná záložka, její index.
        /// </summary>
        public int ActiveIndex
        {
            get { var activeTab = ActiveTab; return (activeTab?.Index ?? -1); }
            set
            {   // Setovat mohu jen index položky, kterou mám v seznamu:
                TabHeaderItem item = this[value];
                _SelectedTabKey = item?.Key;
            }
        }
        /// <summary>
        /// Tato property zajišťuje GUI bezpečný přístup k <see cref="_Component"/> : <see cref="ITabHeaderStrip.SelectedTabKey"/> (provádí invokování GUI).
        /// </summary>
        private string _SelectedTabKey
        {
            get { return this.Control.GetGuiValue(() => _Component.SelectedTabKey); }
            set { this.Control.SetGuiValue(key => _Component.SelectedTabKey = key, value); }
        }
        /// <summary>
        /// Počet položek
        /// </summary>
        public int ItemCount { get { return this._HeaderItems.Count; } }
        /// <summary>
        /// Vrátí true, pokud this instance obsauhe záložku daného jména
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return (key != null && this._HeaderItems.ContainsKey(key));
        }
        /// <summary>
        /// Metoda vrátí scope pro tichý úsek kódu.
        /// Typicky na začátku úseku (při jeho vytváření) je komponenta uvedena do stavu BeginInit a SuspendLayout a jsou vypnuté eventy,
        /// a při Dispose tohoto scope je vše vráceno do výchozího stavu.
        /// Používá se v kódu, kde chceme manipulovat s objektem (odebírat/přidávat záložky) a nechceme, aby to bylo blikalo = vidět akce "po kouskách".
        /// </summary>
        /// <param name="activeEvents">Požadavek, aby eventy byly aktivní</param>
        /// <returns></returns>
        public IDisposable SilentScope(bool activeEvents = false) { return this._Component.SilentScope(activeEvents); }
        /// <summary>
        /// Vrátí prvek na daném indexu. Indexy jsou dané postupem přidávání položek.
        /// Pro index mimo rozsah obsahuje null.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TabHeaderItem this[int index] { get { return (index >= 0 && index < ItemCount ? this.Items[index] : null); } }
        /// <summary>
        /// Vrátí prvek dle daného klíče.
        /// Pro prázdný a pro neznámý klíč vrací null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TabHeaderItem this[string key] { get { if (key != null && this._HeaderItems.TryGetValue(key, out TabHeaderItem item)) return item; return null; } }
        /// <summary>
        /// Eventhandlery jsou aktivní? Default = true
        /// </summary>
        public bool EventsActive { get { return _EventsActive; } set { _EventsActive = value; } }
        /// <summary>
        /// Event při změně vybraného Tabu
        /// </summary>
        public event EventHandler<ValueChangingArgs<string>> SelectedTabChanging;
        /// <summary>
        /// Event po změně vybraného Tabu
        /// </summary>
        public event EventHandler<ValueChangedArgs<string>> SelectedTabChanged;
        /// <summary>
        /// Event po změně velikosti záhlaví
        /// </summary>
        public event EventHandler<ValueChangedArgs<Size>> HeaderSizeChanged;
        #endregion
        #region Static vlastnosti
        /// <summary>
        /// Styl fontu pro text záhlaví, stav Normal.
        /// Uplatní se při tvorbě nové instance <see cref="TabHeaderStrip"/>.
        /// Default = <see cref="FontStyle.Regular"/>.
        /// </summary>
        public static FontStyle NormalFontStyle { get { return _NormalFontStyle; } set { _NormalFontStyle = value; } }
        /// <summary>
        /// Styl fontu pro text záhlaví, stav Hovered (=najela na něj myš, ale ještě jej nezmáčkla).
        /// Uplatní se při tvorbě nové instance <see cref="TabHeaderStrip"/>.
        /// Default = <see cref="FontStyle.Underline"/>.
        /// </summary>
        public static FontStyle HoveredFontStyle { get { return _HoveredFontStyle; } set { _HoveredFontStyle = value; } }
        /// <summary>
        /// Styl fontu pro text záhlaví, stav Pressed. 
        /// Uplatní se při tvorbě nové instance <see cref="TabHeaderStrip"/>.
        /// Default = <see cref="FontStyle.Bold"/> | <see cref="FontStyle.Italic"/>.
        /// </summary>
        public static FontStyle PressedFontStyle { get { return _PressedFontStyle; } set { _PressedFontStyle = value; } }
        private static FontStyle _NormalFontStyle = FontStyle.Regular;
        private static FontStyle _HoveredFontStyle = FontStyle.Underline;
        private static FontStyle _PressedFontStyle = FontStyle.Bold;
        #endregion
    }
    #endregion
    #region class TabHeaderItem : Obecné rozhraní na záložky
    /// <summary>
    /// Obecné rozhraní na záložky
    /// </summary>
    internal class TabHeaderItem : IMember<TabHeaderStrip>, IDisposable
    {
        #region Data
        /// <summary>
        /// Klíč záložky.
        /// Pozor: připouštíme hodnotu Empty = "", ale nepřipouštíme NULL.
        /// </summary>
        public string Key { get { return _Key; } }
        private readonly string _Key;
        /// <summary>
        /// Index tohoto prvku v poli záložek.
        /// Dokud instance <see cref="TabHeaderItem"/> není přidána do kolekce <see cref="TabHeaderStrip"/>, pak <see cref="Index"/> = -1.
        /// </summary>
        public int Index
        {
            get
            {
                int index = -1;
                if (_Owner != null)
                {
                    string key = _Key;
                    var items = _Owner.Items.ToList();
                    index = items.FindIndex(i => String.Equals(i.Key, key, StringComparison.Ordinal));
                }
                return index;
            }
        }
        /// <summary>
        /// Text záložky v pruhu záložek
        /// </summary>
        public string Label { get { return _Label; } set { _NotifyChange(ref _Label, value, nameof(Label)); } }
        private string _Label;
        /// <summary>
        /// Titulek okna
        /// </summary>
        public string PageTitle { get { return _PageTitle; } set { _NotifyChange(ref _PageTitle, value, nameof(PageTitle)); } }
        private string _PageTitle;
        /// <summary>
        /// ToolTip
        /// </summary>
        public string ToolTip { get { return _ToolTip; } set { _NotifyChange(ref _ToolTip, value, nameof(ToolTip)); } }
        private string _ToolTip;
        /// <summary>
        /// Ikona záložky - jméno
        /// </summary>
        public string ImageName { get { return _ImageName; } set { _NotifyChange(ref _ImageName, value, nameof(ImageName)); } }
        private string _ImageName;
        /// <summary>
        /// Ikona záložky - obrázek
        /// </summary>
        public Image Image { get { return _Image; } set { _NotifyChange(ref _Image, value, nameof(Image)); } }
        private Image _Image;
        /// <summary>
        /// Je viditelná?
        /// Default = true;
        /// </summary>
        public bool Visible { get { return _Visible; } set { _NotifyChange(ref _Visible, value, nameof(Visible)); } }
        private bool _Visible;
        /// <summary>
        /// Je viditelný křížek na zavření?
        /// Default = false;
        /// </summary>
        public bool ShowCloseButton { get { return _ShowCloseButton; } set { _NotifyChange(ref _ShowCloseButton, value, nameof(ShowCloseButton)); } }
        private bool _ShowCloseButton;
        /// <summary>
        /// Režim zobrazení ikony a textu pro tuto položku, default = přebírá se z <see cref="TabHeaderStrip.ImageTextMode"/>
        /// </summary>
        public ImageTextMode ImageTextMode { get { return _ImageTextMode; } set { _NotifyChange(ref _ImageTextMode, value, nameof(ImageTextMode)); } }
        private ImageTextMode _ImageTextMode;
        TabHeaderStrip IMember<TabHeaderStrip>.Owner { get { return _Owner; } set { __Owner = value; } }
        private TabHeaderStrip _Owner { get { return __Owner?.Target; } set { __Owner = value; } }
        private WeakTarget<TabHeaderStrip> __Owner;
        /// <summary>
        /// Event volaný po každém setování hodnoty do některé property v this instanci.
        /// Tento event nedetekuje reálnou změnu hodnoty, je vyvolán po každém setování nové hodnoty.
        /// </summary>
        public event EventHandler<PropertyChangedArgs<object>> AfterPropertySet;
        #endregion
        #region Tvorba
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="key"></param>
        public TabHeaderItem(string key) { this._Key = key; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.Label; }
        /// <summary>
        /// Vytvoří a vrátí prvek z dodaných dat
        /// </summary>
        /// <param name="key"></param>
        /// <param name="label"></param>
        /// <param name="pageTitle"></param>
        /// <param name="toolTip"></param>
        /// <param name="iconName"></param>
        /// <param name="image"></param>
        /// <param name="visible"></param>
        /// <param name="showCloseButton"></param>
        /// <returns></returns>
        public static TabHeaderItem CreateItem(string key, string label, string pageTitle = null, string toolTip = null, string iconName = null, Image image = null, bool visible = true, bool showCloseButton = false)
        {
            return new TabHeaderItem(key)
            {
                _Label = label,
                _PageTitle = pageTitle,
                _ToolTip = toolTip,
                _ImageName = iconName,
                _Image = image,
                _Visible = visible,
                _ShowCloseButton = showCloseButton
            };
        }
        /// <summary>
        /// Vytvoří a vrátí prvek z dodaných dat
        /// </summary>
        /// <param name="labeledTab"></param>
        /// <returns></returns>
        public static TabHeaderItem CreateItem(NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo labeledTab)
        {
            return CreateItem(labeledTab.Name, labeledTab.Label);        // Tady by se předávaly další hodnoty z LabeledTabInfo, pokud by tam byly...
        }
        /// <summary>
        /// Vytvoří a vrátí pole prvků z dodaných dat
        /// </summary>
        /// <param name="labeledTabs"></param>
        /// <returns></returns>
        public static TabHeaderItem[] CreateItems(IEnumerable<NCC.DataFormDataSourceFacade.TabsFacade.LabeledTabInfo> labeledTabs)
        {
            List<TabHeaderItem> items = new List<TabHeaderItem>();
            if (labeledTabs != null)
            {
                foreach (var labeledTab in labeledTabs)
                {
                    if (labeledTab != null)
                        items.Add(CreateItem(labeledTab));
                }
            }
            return items.ToArray();
        }
        /// <summary>
        /// Změní hodnotu v ref proměnné, a vyvolá eventhandler <see cref="AfterPropertySet"/>.
        /// Tato metoda neřeší rozdíl hodnot.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        private void _NotifyChange<T>(ref T variable, T value, string propertyName)
        {
            T oldValue = variable;
            variable = value;
            if (AfterPropertySet != null)
            {
                PropertyChangedArgs<object> args = new PropertyChangedArgs<object>(oldValue, value, propertyName);
                AfterPropertySet(this, args);
            }
        }
        /// <summary>
        /// Dispose - zahodí eventhandlery <see cref="AfterPropertySet"/>
        /// </summary>
        void IDisposable.Dispose()
        {
            AfterPropertySet = null;
            _Owner = null;
        }
        #endregion
    }
    /// <summary>
    /// Režim zobrazení ikony a textu
    /// </summary>
    internal enum ImageTextMode
    {
        /// <summary>
        /// Výchozí, dle parenta
        /// </summary>
        Default = 0,
        /// <summary>
        /// Jen ikona
        /// </summary>
        Image = 1,
        /// <summary>
        /// Jen text
        /// </summary>
        Text = 2,
        /// <summary>
        /// Obrázek
        /// </summary>
        ImageAndText = 3,
        /// <summary>
        /// Obrázek nebo text (pokud je obrázek, nebude text)
        /// </summary>
        ImageOrText = 4
    }
    #endregion
    #region class TabHeaderStripIG : implementace controlu pro Infragistics.Win.UltraWinTabControl
    /*
    /// <summary>
    /// Implementace Infragistics.Win.UltraWinTabControl
    /// </summary>
    internal class TabHeaderStripIG : IGT.UltraTabStripControl, ITabHeaderStrip
    {
        #region Konstruktor, proměnné, něco ze života Infragistic
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TabHeaderStripIG(TabHeaderStrip owner)
        {
            this.__Owner = owner;
            this._CurrentTabKey = null;
            this.UseHotTracking = Infragistics.Win.DefaultableBoolean.True;
            this.TabLayoutStyle = Infragistics.Win.UltraWinTabs.TabLayoutStyle.SingleRowAutoSize;
            this.TextOrientation = Infragistics.Win.UltraWinTabs.TextOrientation.Horizontal;
            this.SharedControlsPage = new Infragistics.Win.UltraWinTabControl.UltraTabSharedControlsPage
            {
                BorderStyle = System.Windows.Forms.BorderStyle.None
            };
            this.MaxVisibleTabRows = 1;
            this.Dock = System.Windows.Forms.DockStyle.Top;
        }
        private TabHeaderStrip _Owner { get { return __Owner.Target; } }
        private WeakTarget<TabHeaderStrip> __Owner;
        private Size _HeaderSize;
        /// <summary>
        /// Řešení chyby, kdy horní záložky Dataformu se chybně zarovnávaly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            if (this.TabPageSize.Width == 0)
                this.TabPageSize = new Size(this.Width, 0);
            else
                this.TabPageSize = new Size(this.TabPageSize.Width, 0);
            base.OnSizeChanged(e);
        }
        /// <summary>
        /// Detekuje změnu velikosti záhlaví, při změně vyvolá <see cref="OnHeaderSizeChanged(Size, Size)"/> a aktuální velikost si uloží
        /// </summary>
        private void _HeadersSizeDetectChange()
        {
            Size oldSize = _HeaderSize;
            Size newSize = this.TabPageSize;
            _HeaderSize = newSize;
            if (newSize.Height != oldSize.Height)          // Tento control je zadokován nahoru, proto si hlídá Height
                OnHeaderSizeChanged(oldSize, newSize);
        }
        /// <summary>
        /// Při změně záložky
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSelectedTabChanging(IGT.SelectedTabChangingEventArgs e)
        {
            base.OnSelectedTabChanging(e);
            if (_CurrentTabKey != null && _EventsActive)
            {
                if (_SelectedTabChanging != null)
                {
                    var args = new ValueChangingArgs<string>(this._CurrentTabKey, e.Tab.Key);
                    _SelectedTabChanging(this, args);
                    e.Cancel = args.Cancel;
                }
            }
        }
        /// <summary>
        /// Po změně záložky
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSelectedTabChanged(IGT.SelectedTabChangedEventArgs e)
        {
            base.OnSelectedTabChanged(e);
            if (_CurrentTabKey != null && _EventsActive)
            {
                if (_SelectedTabChanged != null)
                {
                    var args = new ValueChangedArgs<string>(this._CurrentTabKey, e.Tab.Key);
                    _SelectedTabChanged(this, args);
                }
            }
            this._CurrentTabKey = e.Tab.Key;
        }
        /// <summary>
        /// Po změně velikosti záhlaví
        /// </summary>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        protected virtual void OnHeaderSizeChanged(Size oldSize, Size newSize)
        {
            if (!this.IsDisposed && _EventsActive)
            {
                if (_HeaderSizeChanged != null)
                {
                    var args = new ValueChangedArgs<Size>(oldSize, newSize);
                    _HeaderSizeChanged(this, args);
                }
            }
        }
        /// <summary>
        /// Zarovnání obsahu = tabů (doleva/doprava, nahoru/dolů
        /// </summary>
        private ContentAlignment _ContentAlignment
        {
            get { return __ContentAlignment; }
            set
            {
                this.TabOrientation =
                    (value == ContentAlignment.TopLeft ? Infragistics.Win.UltraWinTabs.TabOrientation.TopLeft :
                    (value == ContentAlignment.BottomLeft ? Infragistics.Win.UltraWinTabs.TabOrientation.BottomLeft :
                    (value == ContentAlignment.TopRight ? Infragistics.Win.UltraWinTabs.TabOrientation.TopRight :
                    (value == ContentAlignment.BottomRight ? Infragistics.Win.UltraWinTabs.TabOrientation.BottomRight :
                    Infragistics.Win.UltraWinTabs.TabOrientation.TopLeft))));
                __ContentAlignment = value;
            }
        }
        /// <summary>
        /// Zarovnání tabů- hodnota
        /// </summary>
        private ContentAlignment __ContentAlignment;
        /// <summary>
        /// Režim zobrazení ikony a textu pro tuto položku, default = <see cref="ImageTextMode.ImageAndText"/>
        /// </summary>
        private ImageTextMode _ImageTextMode
        {
            get { return ImageTextMode.Text; }
            set { }
        }
        /// <summary>
        /// Hodnota null : Potlačí volání eventhandlerů v procesu iniciace, tj. při prvotní aktivaci stránky
        /// </summary>
        private string _CurrentTabKey;
        /// <summary>
        /// Hodnota true : Povolí volání eventhandlerů
        /// </summary>
        private bool _EventsActive;
        private TabStripControlSkinableSupport _SkinSupport;
        private event EventHandler<ValueChangingArgs<string>> _SelectedTabChanging;
        private event EventHandler<ValueChangedArgs<string>> _SelectedTabChanged;
        private event EventHandler<ValueChangedArgs<Size>> _HeaderSizeChanged;
        #endregion
        #region Pracovní metody
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _Initialise()
        {
            this._SkinSupport = new TabStripControlSkinableSupport(this);
            _SkinSupport.Initialize();
            _EventsActive = true;
        }
        /// <summary>
        /// Přidá fyzickou záložku do komponenty
        /// </summary>
        /// <param name="headerItem"></param>
        private void _AddHeader(TabHeaderItem headerItem)
        {
            IGT.UltraTab ultraTab = new IGT.UltraTab() { Key = headerItem.Key };
            _SetTabData(headerItem, ultraTab);
            headerItem.AfterPropertySet += _HeaderItem_AfterPropertySet;
            this.Tabs.Add(ultraTab);
        }
        /// <summary>
        /// Eventhandler události, kdy určitý datový <see cref="TabHeaderItem"/> změnil některou svoji hodnotu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HeaderItem_AfterPropertySet(object sender, PropertyChangedArgs<object> e)
        {
            TabHeaderItem headerItem = sender as TabHeaderItem;
            if (_TryFindTab(headerItem.Key, out IGT.UltraTab ultraTab))
                _SetTabData(headerItem, ultraTab);
        }
        /// <summary>
        /// Z dodaného datového objektu přepíše data do vizuálního objektu
        /// </summary>
        /// <param name="headerItem"></param>
        /// <param name="ultraTab"></param>
        private void _SetTabData(TabHeaderItem headerItem, IGT.UltraTab ultraTab)
        {
            ultraTab.Text = headerItem.Label;
            ultraTab.ToolTipText = headerItem.ToolTip;
            ultraTab.Visible = headerItem.Visible;
        }
        /// <summary>
        /// Odebere záložku daného klíče
        /// </summary>
        /// <param name="key"></param>
        private void _RemoveHeader(string key)
        {
            if (_TryFindTabIndex(key, out var index))
                this.Tabs.RemoveAt(index);
            if (this.Tabs.Count == 0)
                this._CurrentTabKey = null;
        }
        /// <summary>
        /// Smaže všechny záložky
        /// </summary>
        private void _ClearHeaders()
        {
            this._CurrentTabKey = null;
            this.Tabs.Clear();
        }
        /// <summary>
        /// Zkusí najít data pro záhlaví na daném bodu
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="headerItem"></param>
        /// <returns></returns>
        private bool _TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem)
        {
            headerItem = null;
            if (_TryFindUltraTab(relativePoint, out var ultraTab))
                headerItem = this._Owner[ultraTab.Key];
            return (headerItem != null);
        }
        /// <summary>
        /// Zkusí najít objekt záhlaví na daném bodu. Použije reflexi.
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="ultraTab"></param>
        /// <returns></returns>
        private bool _TryFindUltraTab(Point relativePoint, out IGT.UltraTab ultraTab)
        {
            ultraTab = null;
            // Musíme na to jít nakoukáním pod pokličku Infragistic:
            string tagName = "tabItemTag";       // Pod těmito názvy má Infragistic schované Fieldy s potřebnými údaji o velikosti záložek
            string sizeName = "DisplaySize";
            System.Reflection.FieldInfo tagField = typeof(IGT.UltraTab).GetField(tagName, System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tagField is null) return false;

            int left = 0;
            foreach (Infragistics.Win.UltraWinTabControl.UltraTab tab in this.Tabs)
            {
                object tagData = tagField.GetValue(tab);
                object sizeData = tagData
                                .GetType()
                                .GetField(sizeName, System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .GetValue(tagData);
                Size size = (Size)sizeData;
                Rectangle bounds = new Rectangle(left, 0, size.Width, size.Height);
                if (bounds.Contains(relativePoint))
                {
                    ultraTab = tab;
                    return true;
                }
                left += size.Width;
            }
            return false;
        }
        /// <summary>
        /// Po změně skinu DevExpress bychom měli obarvičkovat tuto lištu záhlaví
        /// </summary>
        /// <param name="arg"></param>
        private void _DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg)
        { }
        /// <summary>
        /// Metoda vrátí scope pro tichý úsek kódu.
        /// </summary>
        /// <returns></returns>
        private IDisposable _SilentScope(bool activeEvents)
        {
            return new UsingScope(
            d =>
            {   // Na počátku scope:
                d.UserData = this._EventsActive;
                ((System.ComponentModel.ISupportInitialize)this).BeginInit();
                this.SuspendLayout();
                this._EventsActive = activeEvents;
            },
            d =>
            {   // Na konci scope, při Dispose našeho Scope:
                if (!this.IsDisposed)
                {   // Pouze pokud objekt (komponenta) sám není Disposed:
                    ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
                    this.ResumeLayout(false);
                }
                this._EventsActive = (bool)d.UserData;
            });
        }
        /// <summary>
        /// Dispose komponenty
        /// </summary>
        private void _Dispose()
        {
            foreach (IGT.UltraTab tab in this.Tabs)
                tab.Dispose();
            this.DataSource = null;
            base.Dispose();
        }
        /// <summary>
        /// Zkusí najít fyzický objekt záhlaví pro daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tabPage"></param>
        /// <returns></returns>
        private bool _TryFindTab(string key, out IGT.UltraTab tabPage)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Tabs.Count; i++)
                {
                    IGT.UltraTab item = this.Tabs[i];
                    if (String.Equals(item.Key, key, StringComparison.Ordinal))
                    {
                        tabPage = item;
                        return true;
                    }
                }
            }
            tabPage = null;
            return false;
        }
        /// <summary>
        /// Zkusí najít fyzický objekt záhlaví pro daný klíč, určí jeho index
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool _TryFindTabIndex(string key, out int index)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Tabs.Count; i++)
                {
                    IGT.UltraTab item = this.Tabs[i];
                    if (String.Equals(item.Key, key, StringComparison.Ordinal))
                    {
                        index = i;
                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        /// <summary>
        /// Aktuálně vybraný Tab, jeho Key
        /// </summary>
        private string _SelectedTabKey
        {
            get { return this.SelectedTab?.Key; }
            set
            {
                IGT.UltraTab ultraTab = (this.Tabs.Exists(value) ? this.Tabs[value] : (this.Tabs.Count > 0 ? this.Tabs[0] : null));
                this.SelectedTab = ultraTab;
                if (ultraTab != null)
                {   // Infragistic potřebují říct dvakrát:
                    ultraTab.Selected = true;
                    ultraTab.Selected = true;
                }
            }
        }
        /// <summary>
        /// Tento control by měl být dokován nahoře...
        /// </summary>
        private WinForm.DockStyle _OptimalDockStyle { get { return WinForm.DockStyle.Top; } }
        /// <summary>
        /// Optimální velikost tohoto controlu v jeho dokovacím směru
        /// </summary>
        private int _OptimalSize { get { int h = TabPageSize.Height; return (h > 10 ? h : 10); } }
        #endregion
        #region ITabHeaderStrip implementace
        void ITabHeaderStrip.Initialise() { _Initialise(); }
        void ITabHeaderStrip.AddHeader(TabHeaderItem headerItem) { this._AddHeader(headerItem); }
        void ITabHeaderStrip.RemoveHeader(string key) { this._RemoveHeader(key); }
        void ITabHeaderStrip.ClearHeaders() { this._ClearHeaders(); }
        bool ITabHeaderStrip.TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem) { return _TryFindTabHeader(relativePoint, out headerItem); }
        void ITabHeaderStrip.DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg) { this._DevexpressSkinChanged(arg); }
        IDisposable ITabHeaderStrip.SilentScope(bool activeEvents) { return _SilentScope(activeEvents); }
        string ITabHeaderStrip.SelectedTabKey { get { return _SelectedTabKey; } set { _SelectedTabKey = value; } }
        bool ITabHeaderStrip.EventsActive { get { return _EventsActive; } set { _EventsActive = value; } }
        WinForm.Control ITabHeaderStrip.Control { get { return this; } }
        ContentAlignment ITabHeaderStrip.ContentAlignment { get { return _ContentAlignment; } set { _ContentAlignment = value; } }
        ImageTextMode ITabHeaderStrip.ImageTextMode { get { return _ImageTextMode; } set { _ImageTextMode = value; } }
        WinForm.DockStyle ITabHeaderStrip.OptimalDockStyle { get { return this._OptimalDockStyle; } }
        int ITabHeaderStrip.OptimalSize { get { return this._OptimalSize; } }
        event EventHandler<ValueChangingArgs<string>> ITabHeaderStrip.SelectedTabChanging { add { _SelectedTabChanging += value; } remove { _SelectedTabChanging += value; } }
        event EventHandler<ValueChangedArgs<string>> ITabHeaderStrip.SelectedTabChanged { add { _SelectedTabChanged += value; } remove { _SelectedTabChanged += value; } }
        event EventHandler<ValueChangedArgs<Size>> ITabHeaderStrip.HeaderSizeChanged { add { _HeaderSizeChanged += value; } remove { _HeaderSizeChanged += value; } }
        void IDisposable.Dispose() { this._Dispose(); }
        #endregion
    }
    */
    #endregion
    #region class TabHeaderStripDXTop : implementace controlu pro DevExpress.XtraBars.Navigation.TabPane
    /// <summary>
    /// Implementace DevExpress.XtraBars.Navigation.TabPane
    /// </summary>
    internal class TabHeaderStripDXTop : DXN.TabPane, ITabHeaderStrip, IDisposableContainer
    {
        #region Konstruktor, proměnné, něco ze života DevExpress
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TabHeaderStripDXTop(TabHeaderStrip owner)
        {
            this.__Owner = owner;
            this._CurrentTabKey = null;
            this.Name = "TabHeaderStripDXTop";
            this.ClientSizeChanged += _ClientSizeChanged;
            _SetGuiProperties(this);
            this._ContentAlignment = ContentAlignment.TopLeft;
        }
        /// <summary>
        /// Po změně prostoru prověří velikost záhlaví
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this._HeadersSizeDetectChange();
        }
        /// <summary>
        /// Tady si DevExpress počítá velikosti svých elementů
        /// </summary>
        /// <param name="e"></param>
        protected override void CalcViewInfo(WinForm.PaintEventArgs e)
        {
            base.CalcViewInfo(e);
            this._HeadersSizeDetectChange();
        }
        /// <summary>
        /// Detekuje změnu velikosti záhlaví, při změně vyvolá <see cref="OnHeaderSizeChanged(Size, Size)"/> a aktuální velikost si uloží
        /// </summary>
        private void _HeadersSizeDetectChange()
        {
            Size oldSize = _HeaderSize;
            Size newSize = ButtonBounds.Size;
            _HeaderSize = newSize;
            if (newSize.Height != oldSize.Height)          // Tento control je zadokován nahoru, proto si hlídá Height
                OnHeaderSizeChanged(oldSize, newSize);
        }
        /// <summary>
        /// Tento control by měl být dokován nahoře...
        /// </summary>
        private WinForm.DockStyle _OptimalDockStyle { get { return WinForm.DockStyle.Top; } }
        /// <summary>
        /// Optimální velikost tohoto controlu v jeho dokovacím směru
        /// </summary>
        private int _OptimalSize { get { int h = ButtonBounds.Height; return (h > 10 ? h : 10) + 5; } }
        /// <summary>
        /// Aktuální souřadnice buttonů
        /// </summary>
        protected Rectangle ButtonBounds { get { return this.ViewInfo.ButtonsBounds; } }
        private TabHeaderStrip _Owner { get { return __Owner.Target; } }
        private WeakTarget<TabHeaderStrip> __Owner;
        private Size _HeaderSize;
        /// <summary>
        /// Při změně záložky
        /// </summary>
        /// <param name="oldPage"></param>
        /// <param name="newPage"></param>
        protected override void OnSelectedPageChanging(DXN.INavigationPageBase oldPage, DXN.INavigationPageBase newPage)
        {
            base.OnSelectedPageChanging(oldPage, newPage);
            if (_CurrentTabKey != null && _SelectedPageCancel is null && _EventsActive && newPage != null && !this.IsDisposed)
            {
                if (_SelectedTabChanging != null)
                {
                    DXN.TabNavigationPage newTabPage = newPage as DXN.TabNavigationPage;
                    string newKey = newTabPage?.Name;
                    var args = new ValueChangingArgs<string>(this._CurrentTabKey, newKey);
                    _SelectedTabChanging(this, args);
                    if (args.Cancel)
                        _SelectedPageCancel = this.SelectedPage;
                }
            }
        }
        /// <summary>
        /// Po změně záložky
        /// </summary>
        /// <param name="oldPage"></param>
        /// <param name="newPage"></param>
        protected override void OnSelectedPageChanged(DXN.INavigationPageBase oldPage, DXN.INavigationPageBase newPage)
        {
            base.OnSelectedPageChanged(oldPage, newPage);

            if (DoSelectingPageCancel()) return;           // Oďub: komponenta DevExpress nedovoluje provést Cancel v metodě OnSelectedPageChanging!

            string newKey = null;
            if (newPage != null && !this.IsDisposed)
            {
                DXN.TabNavigationPage newTabPage = newPage as DXN.TabNavigationPage;
                newKey = newTabPage?.Name;
                if (_CurrentTabKey != null && _EventsActive)
                {
                    if (_SelectedTabChanged != null)
                    {
                        var args = new ValueChangedArgs<string>(this._CurrentTabKey, newKey);
                        _SelectedTabChanged(this, args);
                    }
                }
            }
            this._CurrentTabKey = newKey;
        }
        /// <summary>
        /// Komponenta DevExpress nedovoluje provést Cancel v metodě OnSelectedPageChanging! Zde je náhradní řešení:
        /// V metodě <see cref="OnSelectedPageChanging(DXN.INavigationPageBase, DXN.INavigationPageBase)"/> 
        /// je nastaveno <see cref="_SelectedPageCancel"/> = <see cref="SelectedPage"/>; tím budeme vědět kam se máme vrátit.
        /// V metodě <see cref="OnSelectedPageChanged(DXN.INavigationPageBase, DXN.INavigationPageBase)"/> se nejprve volá tato metoda, 
        /// a pokud vrátí true pak další logika (její eventy) nepokračuje.
        /// V této metodě se zjistí, že se máme vrátit na původní SelectedPage, a vyřešíme to. 
        /// </summary>
        /// <returns></returns>
        protected bool DoSelectingPageCancel()
        {
            if (_SelectedPageCancel is null) return false;

            // Tato metoda je volána z OnSelectedPageChanged() dvakrát při jednom Cancelování změny záložky:
            //  1. když uživatel změnil záložku na novou;
            //     eventhandler v OnSelectedPageChanging() nastavil Cancel = true;
            //     v OnSelectedPageChanging() jsme nastavili _SelectedPageCancel = this.SelectedPage;
            //     komponenta DevExpress předala řízení do OnSelectedPageChanged(), a odtamtud sem = poprvé;
            //     a my tady zjistíme, že SelectedPage NENÍ SHODNÁ s _SelectedPageCancel;
            //       -> pak my nastavíme this.SelectedPage = _SelectedPageCancel; a rozběhne se nová změna záložky:
            //  2. když zdejší metoda vrátila this.SelectedPage = _SelectedPageCancel;
            //     ale ponechali jsme _SelectedPageCancel beze změny (není null);
            //     pak metoda OnSelectedPageChanging() vynechala volání eventhandlerů;
            //     komponenta DevExpress předala řízení do OnSelectedPageChanged(), a odtamtud sem = podruhé;
            //     a my tady pak zjistíme, že SelectedPage už JE SHODNÁ s _SelectedPageCancel;
            //       -> pak pouze vynulujeme _SelectedPageCancel;
            //  V obou případech vracíme true, protože NEJDE o změnu záložky:
            if (this.SelectedPage is null || !Object.Equals(this.SelectedPage, _SelectedPageCancel))
                this.SelectedPage = _SelectedPageCancel;
            else
                _SelectedPageCancel = null;

            return true;
        }
        /// <summary>
        /// Na tuto stránku se má vrátit SelectedPage poté, kdy v OnSelectedPageChanging() byl nastaven Cancel.
        /// Pokud je zde null, pak se nikam nevracíme.
        /// </summary>
        private DXN.TabNavigationPage _SelectedPageCancel;
        /// <summary>
        /// Po změně velikosti záhlaví
        /// </summary>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        protected virtual void OnHeaderSizeChanged(Size oldSize, Size newSize)
        {
            if (!this.IsDisposed && _EventsActive)
            {
                if (_HeaderSizeChanged != null)
                {
                    var args = new ValueChangedArgs<Size>(oldSize, newSize);
                    _HeaderSizeChanged(this, args);
                }
            }
        }
        /// <summary>
        /// Zarovnání obsahu = tabů (doleva/doprava, nahoru/dolů
        /// </summary>
        private ContentAlignment _ContentAlignment
        {
            get { return __ContentAlignment; }
            set
            {
                this.TabAlignment =
                    (value == ContentAlignment.TopLeft ? DevExpress.XtraEditors.Alignment.Near :
                    (value == ContentAlignment.BottomLeft ? DevExpress.XtraEditors.Alignment.Near :
                    (value == ContentAlignment.TopRight ? DevExpress.XtraEditors.Alignment.Far :
                    (value == ContentAlignment.BottomRight ? DevExpress.XtraEditors.Alignment.Far :
                     DevExpress.XtraEditors.Alignment.Near))));
                __ContentAlignment = value;
            }
        }
        /// <summary>
        /// Zarovnání tabů - hodnota
        /// </summary>
        private ContentAlignment __ContentAlignment;
        /// <summary>
        /// Režim zobrazení ikony a textu pro tuto položku, default = <see cref="ImageTextMode.ImageAndText"/>
        /// </summary>
        private ImageTextMode _ImageTextMode
        {
            get { return _GetImageTextMode(this.PageProperties.ShowMode); }
            set { this.PageProperties.ShowMode = _GetItemShowMode(value); }
        }
        /// <summary>
        /// Hodnota null : Potlačí volání eventhandlerů v procesu iniciace, tj. při prvotní aktivaci stránky
        /// </summary>
        private string _CurrentTabKey;
        /// <summary>
        /// Hodnota true : Povolí volání eventhandlerů
        /// </summary>
        private bool _EventsActive;
        private event EventHandler<ValueChangingArgs<string>> _SelectedTabChanging;
        private event EventHandler<ValueChangedArgs<string>> _SelectedTabChanged;
        private event EventHandler<ValueChangedArgs<Size>> _HeaderSizeChanged;
        #endregion
        #region Nastavení vzhledu
        /// <summary>
        /// Nastaví GUI vlastnosti celého pruhu záhlaví
        /// </summary>
        /// <param name="tabPane"></param>
        private static void _SetGuiProperties(DXN.TabPane tabPane)
        {
            tabPane.TabAlignment = DevExpress.XtraEditors.Alignment.Near;           // Near = doleva, Far = doprava, Center = uprostřed
            tabPane.PageProperties.ShowMode = DXN.ItemShowMode.ImageAndText;
            tabPane.AllowCollapse = DevExpress.Utils.DefaultBoolean.False;          // Nedovolí uživateli skrýt headery

            tabPane.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            tabPane.LookAndFeel.UseWindowsXPTheme = true;
            tabPane.OverlayResizeZoneThickness = 20;
            tabPane.ItemOrientation = WinForm.Orientation.Horizontal;               // Vertical = kreslí řadu záhlaví vodorovně, ale obsah jednotlivého buttonu svisle :-(

            // Animované změny zatím nemá význam aplikovat, protože ty se provádějí na controlu řízeném TabHeaderem,
            //  ale klient Greenu pracuje jinak = ten si podle aktivní záložky jen vyměňuje Layout na jednom a stále tomtéž controlu v DataFormu:
            tabPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.False;
            //this.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;
            //this.TransitionAnimationProperties.FrameInterval = 5 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
            //this.TransitionAnimationProperties.FrameCount = 50;                  // Celkový čas = interval * count
            //this.TransitionType = DevExpress.Utils.Animation.Transitions.Fade;

            // Požadavky designu na vzhled buttonů:
            tabPane.AppearanceButton.Normal.FontStyleDelta = TabHeaderStrip.NormalFontStyle;
            tabPane.AppearanceButton.Normal.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            tabPane.AppearanceButton.Pressed.FontStyleDelta = TabHeaderStrip.PressedFontStyle;
            tabPane.AppearanceButton.Pressed.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            tabPane.AppearanceButton.Hovered.FontStyleDelta = TabHeaderStrip.HoveredFontStyle;
            tabPane.AppearanceButton.Hovered.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            tabPane.Dock = WinForm.DockStyle.Top;
        }
        /// <summary>
        /// Nastaví GUI vlastnosti jednoho buttonu záhlaví
        /// </summary>
        /// <param name="navPage"></param>
        private static void _SetGuiProperties(DXN.TabNavigationPage navPage)
        { }
        /// <summary>
        /// Konvertuje hodnotu <see cref="ImageTextMode"/> na <see cref="DXN.ItemShowMode"/>
        /// </summary>
        /// <param name="imageTextMode"></param>
        /// <returns></returns>
        private static DXN.ItemShowMode _GetItemShowMode(ImageTextMode imageTextMode)
        {
            switch (imageTextMode)
            {
                case ImageTextMode.Default: return DXN.ItemShowMode.Default;
                case ImageTextMode.Image: return DXN.ItemShowMode.Image;
                case ImageTextMode.Text: return DXN.ItemShowMode.Text;
                case ImageTextMode.ImageAndText: return DXN.ItemShowMode.ImageAndText;
                case ImageTextMode.ImageOrText: return DXN.ItemShowMode.ImageOrText;
            }
            return DXN.ItemShowMode.ImageOrText; ;
        }
        /// <summary>
        /// Konvertuje hodnotu <see cref="DXN.ItemShowMode"/> na <see cref="ImageTextMode"/>
        /// </summary>
        /// <param name="itemShowMode"></param>
        /// <returns></returns>
        private static ImageTextMode _GetImageTextMode(DXN.ItemShowMode itemShowMode)
        {
            switch (itemShowMode)
            {
                case DXN.ItemShowMode.Default: return ImageTextMode.Default;
                case DXN.ItemShowMode.Image: return ImageTextMode.Image;
                case DXN.ItemShowMode.Text: return ImageTextMode.Text;
                case DXN.ItemShowMode.ImageAndText: return ImageTextMode.ImageAndText;
                case DXN.ItemShowMode.ImageOrText: return ImageTextMode.ImageOrText;
            }
            return ImageTextMode.ImageOrText; ;
        }
        #endregion
        #region Pracovní metody
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _Initialise()
        {
            _EventsActive = true;
        }
        /// <summary>
        /// Přidá fyzickou záložku do komponenty
        /// </summary>
        /// <param name="headerItem"></param>
        private void _AddHeader(TabHeaderItem headerItem)
        {
            DXN.TabNavigationPage navPage = this.CreateNewPage() as DXN.TabNavigationPage;
            _SetGuiProperties(navPage);
            _SetTabData(headerItem, navPage);              // Data do GUI objektu se musí vepsat až poté, co byl vložen do this.Pages !!!
            headerItem.AfterPropertySet += _HeaderItem_AfterPropertySet;
            this.Pages.Add(navPage);
        }
        /// <summary>
        /// Eventhandler události, kdy určitý datový <see cref="TabHeaderItem"/> změnil některou svoji hodnotu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HeaderItem_AfterPropertySet(object sender, PropertyChangedArgs<object> e)
        {
            TabHeaderItem headerItem = sender as TabHeaderItem;
            if (_TryFindTab(headerItem.Key, out DXN.TabNavigationPage navPage))
                _SetTabData(headerItem, navPage);
        }
        /// <summary>
        /// Z dodaného datového objektu přepíše data do vizuálního objektu
        /// </summary>
        /// <param name="headerItem"></param>
        /// <param name="navPage"></param>
        private void _SetTabData(TabHeaderItem headerItem, DXN.TabNavigationPage navPage)
        {
            if (headerItem == null || navPage == null) return;

            navPage.Name = headerItem.Key;
            navPage.Text = headerItem.Label;
            navPage.Caption = _FormatCaption(headerItem.PageTitle ?? headerItem.Label);
            navPage.PageText = _FormatCaption(headerItem.Label ?? headerItem.PageTitle);
            navPage.ToolTip = headerItem.ToolTip ?? headerItem.PageTitle ?? headerItem.Label;
            navPage.Visible = headerItem.Visible;
            navPage.Properties.ShowMode = _GetItemShowMode(headerItem.ImageTextMode);

            Image image = headerItem.Image;
            if (image is null)
            {
                string imageName = headerItem.ImageName;
                if (!String.IsNullOrEmpty(imageName))
                    image = DxComponent.GetImage(imageName, ResourceImageSizeType.Medium);
            }
            navPage.ImageOptions.Image = image;
        }
        /// <summary>
        /// Upraví daný text do DevExpress controlu (přidá dvě mezery před i za text)
        /// </summary>
        /// <param name="caption"></param>
        /// <returns></returns>
        private static string _FormatCaption(string caption)
        {
            return caption;
            // return "  " + caption + "  ";
        }
        /// <summary>
        /// Odebere záložku daného klíče
        /// </summary>
        /// <param name="key"></param>
        private void _RemoveHeader(string key)
        {
            if (_TryFindTab(key, out var navPage))
                this.Pages.Remove(navPage);
            if (this.Pages.Count == 0)
                this._CurrentTabKey = null;
        }
        /// <summary>
        /// Smaže všechny záložky
        /// </summary>
        private void _ClearHeaders()
        {
            this._CurrentTabKey = null;
            this.Pages.Clear();
        }
        /// <summary>
        /// Zkusí najít data pro záhlaví na daném bodu
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="headerItem"></param>
        /// <returns></returns>
        private bool _TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem)
        {
            headerItem = null;
            if (_TryFindNavPage(relativePoint, out var navPage))
                headerItem = this._Owner[navPage.Name];
            return (headerItem != null);
        }
        /// <summary>
        /// Zkusí najít objekt záhlaví na daném bodu.
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="navPage"></param>
        /// <returns></returns>
        private bool _TryFindNavPage(Point relativePoint, out DXN.TabNavigationPage navPage)
        {
            navPage = null;
            var hit = this.CalcHitInfo(relativePoint);
            if (hit != null && hit is DXN.TabNavigationPage page)
                navPage = page;
            return (navPage != null);
        }
        /// <summary>
        /// Po změně skinu DevExpress bychom měli obarvičkovat tuto lištu záhlaví
        /// </summary>
        /// <param name="arg"></param>
        private void _DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg)
        { }
        /// <summary>
        /// Metoda vrátí scope pro tichý úsek kódu.
        /// </summary>
        /// <returns></returns>
        private IDisposable _SilentScope(bool activeEvents)
        {
            return new ActionScope(
            d =>
            {   // Na počátku scope:
                d.UserData = this._EventsActive;
                //  POZOR: pokud se provede BeginInit(), tak TabHeader bude chybně vykreslen !!!
                //  proto je tento řádek odkomentován:     ((System.ComponentModel.ISupportInitialize)this).BeginInit();
                this.SuspendLayout();
                this._EventsActive = activeEvents;
            },
            d =>
            {   // Na konci scope, při Dispose našeho Scope:
                if (!this.IsDisposed)
                {   // Pouze pokud objekt (komponenta) sám není Disposed:
                    //  Na počátku byl potlačen BeginInit(), tak neprovádím ani EndInit:    ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
                    this.ResumeLayout(true);
                }
                this._EventsActive = (bool)d.UserData;
            });
        }
        /// <summary>
        /// Zkusí najít fyzický objekt záhlaví pro daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <param name="navPage"></param>
        /// <returns></returns>
        private bool _TryFindTab(string key, out DXN.TabNavigationPage navPage)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Pages.Count; i++)
                {
                    var item = this.Pages[i];
                    if ((item is DXN.TabNavigationPage page) && String.Equals(page.Name, key, StringComparison.Ordinal))
                    {
                        navPage = page;
                        return true;
                    }
                }
            }
            navPage = null;
            return false;
        }
        /// <summary>
        /// Zkusí najít fyzický objekt záhlaví pro daný klíč, určí jeho index
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool _TryFindTabIndex(string key, out int index)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Pages.Count; i++)
                {
                    var item = this.Pages[i];
                    if ((item is DXN.TabNavigationPage page) && String.Equals(page.Name, key, StringComparison.Ordinal))
                    {
                        index = i;
                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        /// <summary>
        /// Aktuálně vybraný Tab, jeho Key
        /// </summary>
        private string _SelectedTabKey
        {
            get { return this.SelectedPage?.Name; }
            set
            {
                _TryFindTab(value, out DXN.TabNavigationPage navPage);
                this.SelectedPage = navPage;
                if (navPage != null)
                {
                    navPage.Select();
                }
            }
        }
        /// <summary>
        /// Dispose mých vnořených controlů
        /// </summary>
        private void _DisposeControls()
        {
            this.Pages.Clear();
            this.Controls.Clear();
        }
        /// <summary>
        /// Dispose komponenty
        /// </summary>
        private void _Dispose()
        {
            if (!_IsDisposed)
                this.Dispose();
            _IsDisposed = true;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            __Owner = null;
            _IsDisposed = true;
        }
        /// <summary>
        /// true poté, kdy jsme prošli metodou Dispose
        /// </summary>
        private bool _IsDisposed;
        /// <summary>
        /// Gets a value indicating whether the control has been disposed of.
        /// </summary>
        public new bool IsDisposed { get { return (base.IsDisposed || this._IsDisposed); } }
        #endregion
        #region ITabHeaderStrip implementace
        void ITabHeaderStrip.Initialise() { _Initialise(); }
        void ITabHeaderStrip.AddHeader(TabHeaderItem headerItem) { this._AddHeader(headerItem); }
        void ITabHeaderStrip.RemoveHeader(string key) { this._RemoveHeader(key); }
        void ITabHeaderStrip.ClearHeaders() { this._ClearHeaders(); }
        bool ITabHeaderStrip.TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem) { return _TryFindTabHeader(relativePoint, out headerItem); }
        void ITabHeaderStrip.DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg) { this._DevexpressSkinChanged(arg); }
        IDisposable ITabHeaderStrip.SilentScope(bool activeEvents) { return _SilentScope(activeEvents); }
        string ITabHeaderStrip.SelectedTabKey { get { return _SelectedTabKey; } set { _SelectedTabKey = value; } }
        bool ITabHeaderStrip.EventsActive { get { return _EventsActive; } set { _EventsActive = value; } }
        WinForm.Control ITabHeaderStrip.Control { get { return this; } }
        ContentAlignment ITabHeaderStrip.ContentAlignment { get { return _ContentAlignment; } set { _ContentAlignment = value; } }
        ImageTextMode ITabHeaderStrip.ImageTextMode { get { return _ImageTextMode; } set { _ImageTextMode = value; } }
        WinForm.DockStyle ITabHeaderStrip.OptimalDockStyle { get { return this._OptimalDockStyle; } }
        int ITabHeaderStrip.OptimalSize { get { return this._OptimalSize; } }
        event EventHandler<ValueChangingArgs<string>> ITabHeaderStrip.SelectedTabChanging { add { _SelectedTabChanging += value; } remove { _SelectedTabChanging += value; } }
        event EventHandler<ValueChangedArgs<string>> ITabHeaderStrip.SelectedTabChanged { add { _SelectedTabChanged += value; } remove { _SelectedTabChanged += value; } }
        event EventHandler<ValueChangedArgs<Size>> ITabHeaderStrip.HeaderSizeChanged { add { _HeaderSizeChanged += value; } remove { _HeaderSizeChanged += value; } }
        void IDisposableContainer.DisposeControls() { this._DisposeControls(); }
        void IDisposable.Dispose() { this._Dispose(); }
        #endregion
    }
    #endregion
    #region class TabHeaderStripDXLeft : implementace controlu pro DevExpress.XtraBars.Navigation.NavigationPane
    /// <summary>
    /// Implementace DevExpress.XtraBars.Navigation.NavigationPane
    /// </summary>
    internal class TabHeaderStripDXLeft : DXN.NavigationPane, ITabHeaderStrip, IDisposableContainer
    {
        #region Konstruktor, proměnné, něco ze života DevExpress
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TabHeaderStripDXLeft(TabHeaderStrip owner)
        {
            this.__Owner = owner;
            this._CurrentTabKey = null;
            this.Name = "TabHeaderStripDXLeft";
            this.ClientSizeChanged += _ClientSizeChanged;
            _SetGuiProperties(this);
        }
        /// <summary>
        /// Po změně prostoru prověří velikost záhlaví
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this._HeadersSizeDetectChange();
        }
        /// <summary>
        /// Tady si DevExpress počítá velikosti svých elementů
        /// </summary>
        /// <param name="e"></param>
        protected override void CalcViewInfo(WinForm.PaintEventArgs e)
        {
            base.CalcViewInfo(e);
            this._HeadersSizeDetectChange();
        }
        /// <summary>
        /// Detekuje změnu velikosti záhlaví, při změně vyvolá <see cref="OnHeaderSizeChanged(Size, Size)"/> a aktuální velikost si uloží
        /// </summary>
        private void _HeadersSizeDetectChange()
        {
            Size oldSize = _HeaderSize;
            Size newSize = ViewInfo.ButtonsBounds.Size;
            _HeaderSize = newSize;
            if (newSize.Width != oldSize.Width)            // Tento control je zadokován doleva, proto si hlídá Width
                OnHeaderSizeChanged(oldSize, newSize);
        }
        private TabHeaderStrip _Owner { get { return __Owner.Target; } }
        private WeakTarget<TabHeaderStrip> __Owner;
        private Size _HeaderSize;
        /// <summary>
        /// Při změně záložky
        /// </summary>
        /// <param name="oldPage"></param>
        /// <param name="newPage"></param>
        protected override void OnSelectedPageChanging(DXN.INavigationPageBase oldPage, DXN.INavigationPageBase newPage)
        {
            base.OnSelectedPageChanging(oldPage, newPage);
            if (_CurrentTabKey != null && _EventsActive && newPage != null && !this.IsDisposed)
            {
                if (_SelectedTabChanging != null)
                {
                    var args = new ValueChangingArgs<string>(this._CurrentTabKey, newPage.Caption);
                    _SelectedTabChanging(this, args);
                }
            }
        }
        /// <summary>
        /// Po změně záložky
        /// </summary>
        /// <param name="oldPage"></param>
        /// <param name="newPage"></param>
        protected override void OnSelectedPageChanged(DXN.INavigationPageBase oldPage, DXN.INavigationPageBase newPage)
        {
            base.OnSelectedPageChanged(oldPage, newPage);
            string newKey = null;
            if (newPage != null && !this.IsDisposed)
            {
                DXN.NavigationPage newTabPage = newPage as DXN.NavigationPage;
                newKey = newTabPage?.Name;
                if (_CurrentTabKey != null && _EventsActive)
                {
                    if (_SelectedTabChanged != null)
                    {
                        var args = new ValueChangedArgs<string>(this._CurrentTabKey, newKey);
                        _SelectedTabChanged(this, args);
                    }
                }
            }
            this._CurrentTabKey = newKey;
        }
        /// <summary>
        /// Po změně velikosti záhlaví
        /// </summary>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        protected virtual void OnHeaderSizeChanged(Size oldSize, Size newSize)
        {
            if (!this.IsDisposed && _EventsActive)
            {
                if (_HeaderSizeChanged != null)
                {
                    var args = new ValueChangedArgs<Size>(oldSize, newSize);
                    _HeaderSizeChanged(this, args);
                }
            }
        }
        /// <summary>
        /// Zarovnání obsahu = tabů (doleva/doprava, nahoru/dolů
        /// </summary>
        private ContentAlignment _ContentAlignment
        {
            get { return __ContentAlignment; }
            set
            {
                __ContentAlignment = value;
            }
        }
        /// <summary>
        /// Zarovnání tabů- hodnota
        /// </summary>
        private ContentAlignment __ContentAlignment;
        /// <summary>
        /// Režim zobrazení ikony a textu pro tuto položku, default = <see cref="ImageTextMode.ImageAndText"/>
        /// </summary>
        private ImageTextMode _ImageTextMode
        {
            get { return _GetImageTextMode(this.PageProperties.ShowMode); }
            set { this.PageProperties.ShowMode = _GetItemShowMode(value); }
        }
        /// <summary>
        /// Hodnota null : Potlačí volání eventhandlerů v procesu iniciace, tj. při prvotní aktivaci stránky
        /// </summary>
        private string _CurrentTabKey;
        /// <summary>
        /// Hodnota true : Povolí volání eventhandlerů
        /// </summary>
        private bool _EventsActive;
        private event EventHandler<ValueChangingArgs<string>> _SelectedTabChanging;
        private event EventHandler<ValueChangedArgs<string>> _SelectedTabChanged;
        private event EventHandler<ValueChangedArgs<Size>> _HeaderSizeChanged;
        #endregion
        #region Nastavení vzhledu
        /// <summary>
        /// Nastaví GUI vlastnosti celého pruhu záhlaví
        /// </summary>
        /// <param name="navPane"></param>
        private static void _SetGuiProperties(DXN.NavigationPane navPane)
        {
            navPane.PageProperties.ShowMode = DXN.ItemShowMode.ImageAndText;
            navPane.PageProperties.ShowCollapseButton = false;
            navPane.PageProperties.ShowExpandButton = false;
            navPane.PageProperties.AllowBorderColorBlending = true;
            navPane.ShowToolTips = DevExpress.Utils.DefaultBoolean.True;
            navPane.State = DXN.NavigationPaneState.Expanded;

            navPane.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            navPane.LookAndFeel.UseWindowsXPTheme = true;
            navPane.OverlayResizeZoneThickness = 20;
            navPane.ItemOrientation = WinForm.Orientation.Horizontal;               // Vertical = kreslí řadu záhlaví vodorovně, ale obsah jednotlivého buttonu svisle :-(

            // Animované změny zatím nemá význam aplikovat, protože ty se provádějí na controlu řízeném TabHeaderem,
            //  ale klient Greenu pracuje jinak = ten si podle aktivní záložky jen vyměňuje Layout na jednom a stále tomtéž controlu v DataFormu:
            navPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.False;
            //navPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;
            //navPane.TransitionAnimationProperties.FrameInterval = 5 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
            //navPane.TransitionAnimationProperties.FrameCount = 50;                  // Celkový čas = interval * count
            //navPane.TransitionType = DevExpress.Utils.Animation.Transitions.Fade;

            navPane.Dock = System.Windows.Forms.DockStyle.Left;
        }
        /// <summary>
        /// Nastaví GUI vlastnosti jednoho buttonu záhlaví
        /// </summary>
        /// <param name="navPage"></param>
        private static void _SetGuiProperties(DXN.NavigationPage navPage)
        { }
        /// <summary>
        /// Konvertuje hodnotu <see cref="ImageTextMode"/> na <see cref="DXN.ItemShowMode"/>
        /// </summary>
        /// <param name="imageTextMode"></param>
        /// <returns></returns>
        private static DXN.ItemShowMode _GetItemShowMode(ImageTextMode imageTextMode)
        {
            switch (imageTextMode)
            {
                case ImageTextMode.Default: return DXN.ItemShowMode.Default;
                case ImageTextMode.Image: return DXN.ItemShowMode.Image;
                case ImageTextMode.Text: return DXN.ItemShowMode.Text;
                case ImageTextMode.ImageAndText: return DXN.ItemShowMode.ImageAndText;
                case ImageTextMode.ImageOrText: return DXN.ItemShowMode.ImageOrText;
            }
            return DXN.ItemShowMode.ImageOrText; ;
        }
        /// <summary>
        /// Konvertuje hodnotu <see cref="DXN.ItemShowMode"/> na <see cref="ImageTextMode"/>
        /// </summary>
        /// <param name="itemShowMode"></param>
        /// <returns></returns>
        private static ImageTextMode _GetImageTextMode(DXN.ItemShowMode itemShowMode)
        {
            switch (itemShowMode)
            {
                case DXN.ItemShowMode.Default: return ImageTextMode.Default;
                case DXN.ItemShowMode.Image: return ImageTextMode.Image;
                case DXN.ItemShowMode.Text: return ImageTextMode.Text;
                case DXN.ItemShowMode.ImageAndText: return ImageTextMode.ImageAndText;
                case DXN.ItemShowMode.ImageOrText: return ImageTextMode.ImageOrText;
            }
            return ImageTextMode.ImageOrText; ;
        }
        #endregion
        #region Pracovní metody
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _Initialise()
        {
            _EventsActive = true;
        }
        /// <summary>
        /// Přidá fyzickou záložku do komponenty
        /// </summary>
        /// <param name="headerItem"></param>
        private void _AddHeader(TabHeaderItem headerItem)
        {
            DXN.NavigationPage navPage = this.CreateNewPage() as DXN.NavigationPage;
            _SetTabData(headerItem, navPage);              // Data do GUI objektu se musí vepsat až poté, co byl vložen do this.Pages !!!
            _SetGuiProperties(navPage);
            headerItem.AfterPropertySet += _HeaderItem_AfterPropertySet;
            this.Pages.Add(navPage);
        }
        /// <summary>
        /// Eventhandler události, kdy určitý datový <see cref="TabHeaderItem"/> změnil některou svoji hodnotu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HeaderItem_AfterPropertySet(object sender, PropertyChangedArgs<object> e)
        {
            TabHeaderItem headerItem = sender as TabHeaderItem;
            if (_TryFindTab(headerItem.Key, out DXN.NavigationPage navPage))
                _SetTabData(headerItem, navPage);
        }
        /// <summary>
        /// Z dodaného datového objektu přepíše data do vizuálního objektu
        /// </summary>
        /// <param name="headerItem"></param>
        /// <param name="navPage"></param>
        private void _SetTabData(TabHeaderItem headerItem, DXN.NavigationPage navPage)
        {
            if (headerItem == null || navPage == null) return;

            navPage.Name = headerItem.Key;
            navPage.Text = headerItem.Label;
            navPage.Caption = headerItem.PageTitle ?? headerItem.Label;
            navPage.PageText = headerItem.Label ?? headerItem.PageTitle;
            navPage.ToolTip = headerItem.ToolTip ?? headerItem.PageTitle ?? headerItem.Label;
            navPage.Visible = headerItem.Visible;
            navPage.Properties.ShowMode = _GetItemShowMode(headerItem.ImageTextMode);

            Image image = headerItem.Image;
            if (image is null)
            {
                string imageName = headerItem.ImageName;
                if (!String.IsNullOrEmpty(imageName))
                    image = DxComponent.GetImage(imageName, ResourceImageSizeType.Medium);
            }
            navPage.ImageOptions.Image = image;
        }
        /// <summary>
        /// Odebere záložku daného klíče
        /// </summary>
        /// <param name="key"></param>
        private void _RemoveHeader(string key)
        {
            if (_TryFindTab(key, out var navPage))
                this.Pages.Remove(navPage);
            if (this.Pages.Count == 0)
                this._CurrentTabKey = null;
        }
        /// <summary>
        /// Smaže všechny záložky
        /// </summary>
        private void _ClearHeaders()
        {
            this._CurrentTabKey = null;
            this.Pages.Clear();
        }
        /// <summary>
        /// Zkusí najít data pro záhlaví na daném bodu
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="headerItem"></param>
        /// <returns></returns>
        private bool _TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem)
        {
            headerItem = null;
            if (_TryFindNavPage(relativePoint, out var navPage))
                headerItem = this._Owner[navPage.Name];
            return (headerItem != null);
        }
        /// <summary>
        /// Zkusí najít objekt záhlaví na daném bodu.
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="navPage"></param>
        /// <returns></returns>
        private bool _TryFindNavPage(Point relativePoint, out DXN.NavigationPage navPage)
        {
            navPage = null;
            var hit = this.CalcHitInfo(relativePoint);
            if (hit != null && hit is DXN.NavigationPage page)
                navPage = page;
            //for (int i = 0; i < this.Pages.Count; i++)
            //{
            //    if (this.Pages[i].Bounds.Contains(relativePoint))
            //    {
            //        navPage = this.Pages[i] as DXN.NavigationPage;
            //        break;
            //    }
            //}
            return (navPage != null);
        }
        /// <summary>
        /// Po změně skinu DevExpress bychom měli obarvičkovat tuto lištu záhlaví
        /// </summary>
        /// <param name="arg"></param>
        private void _DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg)
        { }
        /// <summary>
        /// Metoda vrátí scope pro tichý úsek kódu.
        /// </summary>
        /// <returns></returns>
        private IDisposable _SilentScope(bool activeEvents)
        {
            return new ActionScope(
            d =>
            {   // Na počátku scope:
                d.UserData = this._EventsActive;
                //  POZOR: pokud se provede BeginInit(), tak TabHeader bude chybně vykreslen !!!
                //  proto je tento řádek odkomentován:     ((System.ComponentModel.ISupportInitialize)this).BeginInit();
                this.SuspendLayout();
                this._EventsActive = activeEvents;
            },
            d =>
            {   // Na konci scope, při Dispose našeho Scope:
                if (!this.IsDisposed)
                {   // Pouze pokud objekt (komponenta) sám není Disposed:
                    //  Na počátku byl potlačen BeginInit(), tak neprovádím ani EndInit:    ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
                    this.ResumeLayout(true);
                }
                this._EventsActive = (bool)d.UserData;
            });
        }
        /// <summary>
        /// Zkusí najít fyzický objekt záhlaví pro daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <param name="navPage"></param>
        /// <returns></returns>
        private bool _TryFindTab(string key, out DXN.NavigationPage navPage)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Pages.Count; i++)
                {
                    var item = this.Pages[i];
                    if ((item is DXN.NavigationPage page) && String.Equals(page.Name, key, StringComparison.Ordinal))
                    {
                        navPage = page;
                        return true;
                    }
                }
            }
            navPage = null;
            return false;
        }
        /// <summary>
        /// Zkusí najít fyzický objekt záhlaví pro daný klíč, určí jeho index
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool _TryFindTabIndex(string key, out int index)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Pages.Count; i++)
                {
                    var item = this.Pages[i];
                    if ((item is DXN.NavigationPage page) && String.Equals(page.Name, key, StringComparison.Ordinal))
                    {
                        index = i;
                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        /// <summary>
        /// Aktuálně vybraný Tab, jeho Key
        /// </summary>
        private string _SelectedTabKey
        {
            get
            {
                DXN.NavigationPage navPage = this.SelectedPage as DXN.NavigationPage;
                return navPage?.Name;
            }
            set
            {
                _TryFindTab(value, out DXN.NavigationPage navPage);
                this.SelectedPage = navPage;
                if (navPage != null)
                {
                    navPage.Select();
                }
            }
        }
        /// <summary>
        /// Tento control by měl být dokován vlevo...
        /// </summary>
        private WinForm.DockStyle _OptimalDockStyle { get { return WinForm.DockStyle.Left; } }
        /// <summary>
        /// Optimální velikost tohoto controlu v jeho dokovacím směru
        /// </summary>
        private int _OptimalSize { get { int h = ButtonBounds.Width; return (h > 40 ? h : 40) + 10; } }
        /// <summary>
        /// Aktuální souřadnice buttonů
        /// </summary>
        protected Rectangle ButtonBounds { get { return this.ViewInfo.ButtonsBounds; } }
        /// <summary>
        /// Dispose mých vnořených controlů
        /// </summary>
        private void _DisposeControls()
        {
            this.Pages.Clear();
            this.Controls.Clear();
        }
        /// <summary>
        /// Dispose komponenty
        /// </summary>
        private void _Dispose()
        {
            if (!_IsDisposed)
                this.Dispose();
            _IsDisposed = true;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            __Owner = null;
            _IsDisposed = true;
        }
        /// <summary>
        /// true poté, kdy jsme prošli metodou Dispose
        /// </summary>
        private bool _IsDisposed;
        /// <summary>
        /// Gets a value indicating whether the control has been disposed of.
        /// </summary>
        public new bool IsDisposed { get { return (base.IsDisposed || this._IsDisposed); } }
        #endregion
        #region ITabHeaderStrip implementace
        void ITabHeaderStrip.Initialise() { _Initialise(); }
        void ITabHeaderStrip.AddHeader(TabHeaderItem headerItem) { this._AddHeader(headerItem); }
        void ITabHeaderStrip.RemoveHeader(string key) { this._RemoveHeader(key); }
        void ITabHeaderStrip.ClearHeaders() { this._ClearHeaders(); }
        bool ITabHeaderStrip.TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem) { return _TryFindTabHeader(relativePoint, out headerItem); }
        void ITabHeaderStrip.DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg) { this._DevexpressSkinChanged(arg); }
        IDisposable ITabHeaderStrip.SilentScope(bool activeEvents) { return _SilentScope(activeEvents); }
        string ITabHeaderStrip.SelectedTabKey { get { return _SelectedTabKey; } set { _SelectedTabKey = value; } }
        bool ITabHeaderStrip.EventsActive { get { return _EventsActive; } set { _EventsActive = value; } }
        WinForm.Control ITabHeaderStrip.Control { get { return this; } }
        ContentAlignment ITabHeaderStrip.ContentAlignment { get { return _ContentAlignment; } set { _ContentAlignment = value; } }
        ImageTextMode ITabHeaderStrip.ImageTextMode { get { return _ImageTextMode; } set { _ImageTextMode = value; } }
        WinForm.DockStyle ITabHeaderStrip.OptimalDockStyle { get { return this._OptimalDockStyle; } }
        int ITabHeaderStrip.OptimalSize { get { return this._OptimalSize; } }
        event EventHandler<ValueChangingArgs<string>> ITabHeaderStrip.SelectedTabChanging { add { _SelectedTabChanging += value; } remove { _SelectedTabChanging += value; } }
        event EventHandler<ValueChangedArgs<string>> ITabHeaderStrip.SelectedTabChanged { add { _SelectedTabChanged += value; } remove { _SelectedTabChanged += value; } }
        event EventHandler<ValueChangedArgs<Size>> ITabHeaderStrip.HeaderSizeChanged { add { _HeaderSizeChanged += value; } remove { _HeaderSizeChanged += value; } }
        void IDisposableContainer.DisposeControls() { this._DisposeControls(); }
        void IDisposable.Dispose() { this._Dispose(); }
        #endregion
    }
    #endregion
    #region interface ITabHeaderStrip : předpis rozhraní pro různé implementace lišty záložek
    /// <summary>
    /// ITabHeaderStrip : předpis rozhraní pro různé implementace lišty záložek
    /// </summary>
    internal interface ITabHeaderStrip : IDisposable
    {
        /// <summary>
        /// Voláno po kontruktoru, provede interní inicializaci
        /// </summary>
        void Initialise();
        /// <summary>
        /// Přidá fyzickou záložku do komponenty
        /// </summary>
        /// <param name="headerItem"></param>
        void AddHeader(TabHeaderItem headerItem);
        /// <summary>
        /// Odebere záložku daného klíče
        /// </summary>
        /// <param name="key"></param>
        void RemoveHeader(string key);
        /// <summary>
        /// Smaže všechny záložky
        /// </summary>
        void ClearHeaders();
        /// <summary>
        /// Komponenta podle dodané souřadnice najde odpovídající záhlaví a vrátí jej
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="headerItem"></param>
        /// <returns></returns>
        bool TryFindTabHeader(Point relativePoint, out TabHeaderItem headerItem);
        /// <summary>
        /// Po změně skinu DevExpress, pro reakci komponenty Infragistic
        /// </summary>
        /// <param name="arg"></param>
        void DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg);
        /// <summary>
        /// Vrátí IDisposable objekt ohraničující oblast kódu, v němž je objekt potichu. 
        /// Typicky se na začátku provede BeginInit() a SuspendLayout() a potlačí se eventy, a na konci se provede ResumeLayout() a obnovení eventů.
        /// </summary>
        /// <returns></returns>
        IDisposable SilentScope(bool activeEvents);
        /// <summary>
        /// Aktuálně vybraná záložka na komponentě, její stringový klíč
        /// </summary>
        string SelectedTabKey { get; set; }
        /// <summary>
        /// Eventhandlery jsou aktivní? Default = true
        /// </summary>
        bool EventsActive { get; set; }
        /// <summary>
        /// Zde je přítomen fyzický control lišty záložek, používaný v GUI
        /// </summary>
        WinForm.Control Control { get; }
        /// <summary>
        /// Zarovnání obsahu = tabů (doleva/doprava, nahoru/dolů
        /// </summary>
        ContentAlignment ContentAlignment { get; set; }
        /// <summary>
        /// Režim zobrazení ikony a textu pro tuto položku, default = <see cref="ImageTextMode.ImageAndText"/>
        /// </summary>
        ImageTextMode ImageTextMode { get; set; }
        /// <summary>
        /// Doporučovaný styl dokování
        /// </summary>
        WinForm.DockStyle OptimalDockStyle { get; }
        /// <summary>
        /// Doporučená velikost, počet pixelů od dokované hrany (pro Top/Bottom je zde Height, pro Left/Right je zde Width)
        /// </summary>
        int OptimalSize { get; }
        /// <summary>
        /// Event při změně vybraného Tabu
        /// </summary>
        event EventHandler<ValueChangingArgs<string>> SelectedTabChanging;
        /// <summary>
        /// Event po změně vybraného Tabu
        /// </summary>
        event EventHandler<ValueChangedArgs<string>> SelectedTabChanged;
        /// <summary>
        /// Event při změně velikosti záhlaví.
        /// Je vyvolán při prvním vykreslení a při každé změně Skinu.
        /// Aplikace může provést jiné ozmístění svých navazujících controlů.
        /// </summary>
        event EventHandler<ValueChangedArgs<Size>> HeaderSizeChanged;
    }
    #endregion
    #region EventArgs : ValueChangedArgs; ValueChangingArgs; PropertyChangedArgs
    /// <summary>
    /// Argument pro handlery obsluhující provedenou změnu hodnoty (z <see cref="ValueOld"/> na <see cref="ValueNew"/>)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueChangedArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="valueOld"></param>
        /// <param name="valueNew"></param>
        public ValueChangedArgs(T valueOld, T valueNew)
        {
            this.ValueOld = valueOld;
            this.ValueNew = valueNew;
        }
        /// <summary>
        /// Dřívější hodnota
        /// </summary>
        public T ValueOld { get; private set; }
        /// <summary>
        /// Aktuální hodnota
        /// </summary>
        public T ValueNew { get; private set; }
    }
    /// <summary>
    /// Argument pro handlery obsluhující probíhající změnu hodnoty (z <see cref="ValueChangedArgs{T}.ValueOld"/> na <see cref="ValueChangedArgs{T}.ValueNew"/>).
    /// Obsahuje property <see cref="Cancel"/>, která dovolí probíhající změnu stornovat.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueChangingArgs<T> : ValueChangedArgs<T>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="valueOld"></param>
        /// <param name="valueNew"></param>
        public ValueChangingArgs(T valueOld, T valueNew)
            : base(valueOld, valueNew)
        {
            this.Cancel = false;
        }
        /// <summary>
        /// Možnost stornování probíhající změny
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// Argument pro handlery obsluhující probíhající změnu hodnoty určité property (z <see cref="ValueChangedArgs{T}.ValueOld"/> na <see cref="ValueChangedArgs{T}.ValueNew"/>).
    /// Obsahuje jméno property v <see cref="PropertyName"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyChangedArgs<T> : ValueChangedArgs<T>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="valueOld"></param>
        /// <param name="valueNew"></param>
        /// <param name="propertyName"></param>
        public PropertyChangedArgs(T valueOld, T valueNew, string propertyName)
             : base(valueOld, valueNew)
        {
            this.PropertyName = propertyName;
        }
        /// <summary>
        /// Dřívější hodnota
        /// </summary>
        public string PropertyName { get; private set; }
    }

    #endregion
    #region Interface IMember : předpis pro podřízený prvek, který dovoluje svému Ownerovi se do něj vepsat jako Vlastní prvku
    /// <summary>
    /// Toto rozhraní implementují třídy podřízených členů, aby umožnily svému vlastníkovi se do nich vepsat
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IMember<T>
    {
        /// <summary>
        /// Vlastník prvku
        /// </summary>
        T Owner { get; set; }
    }
    #endregion
}
