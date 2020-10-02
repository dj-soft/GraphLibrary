using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class GTabContainer : blok, obsahující záhlaví jednotlivých "stránek" a k nim příslušné jednotlivé stránky
    /// <summary>
    /// GTabContainer : Ucelený blok, obsahující záhlaví jednotlivých "stránek" a k nim příslušné jednotlivé stránky
    /// </summary>
    public class GTabContainer : InteractiveContainer
    {
        #region Konstrukce, proměnné
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GTabContainer(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTabContainer()
        {
            this._TabHeaderMode = ShowTabHeaderMode.Default;
            this._IsCollapsed = false;

            this._TabHeader = new GTabHeader(this) { Position = RectangleSide.Top };
            this._TabItemCollapse = this._TabHeader.AddCollapseHeader();
            this._TabItemCollapse.Is.GetVisible = this._GetCollapseIsVisible;
            this._TabItemCollapse.Is.SetVisible = this._SetCollapseIsVisible;
            this._TabHeader.ActivePageChanged += _TabHeader_ActiveItemChanged;
            this._TabOrderData = 1;

            this._Controls = new List<IInteractiveItem>();
        }
        /// <summary>
        /// eventhandler po změně _TabHeader.ActiveItemChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabHeader_ActiveItemChanged(object sender, GPropertyChangeArgs<GTabPage> e)
        {
            GTabPage oldPage = this._TabItemLastActive;
            GTabPage newPage = e.NewValue;
            bool isCollapseOld = this._IsCollapsed;
            bool isCollapseNew = (newPage != null && newPage.IsCollapse);
            bool canRepaint = (newPage != null && newPage.DataControl != null);
            if (!isCollapseOld && !isCollapseNew)
            {   // Jde o reálnou změnu aktivní datové stránky, přičemž dosud nebyl a ani nově nebude stav IsCollapsed:
                if (canRepaint)
                    newPage.DataControl.Repaint();
                e.CorrectValue = this.CallActivePageChanged(oldPage, newPage, e.EventSource);
                this._TabItemLastActive = newPage;
                this.InvalidateChilds();
            }
            else if (!isCollapseOld && isCollapseNew)
            {   // Provádíme Collapse:
                this.IsCollapsedInternal = true;           // Set property řeší layout, volání eventů, atd.
                // Nevoláme CallActivePageChanged(), protože nejde o změnu datové záložky.
                this.InvalidateChilds();
            }
            else if (isCollapseOld && !isCollapseNew)
            {   // Provádíme Expand - to může být na původní záložku, anebo na jinou:
                this.IsCollapsedInternal = false;          // Set property řeší layout, volání eventů, atd.
                // Metodu CallActivePageChanged() voláme jen tejhdy, když nová záložka se liší od dosavadní datové záložky:
                if (oldPage != null && !Object.ReferenceEquals(oldPage, newPage))
                    e.CorrectValue = this.CallActivePageChanged(oldPage, newPage, e.EventSource);
                this._TabItemLastActive = newPage;
                this.InvalidateChilds();
            }
        }
        /// <summary>
        /// Sada záložek
        /// </summary>
        private GTabHeader _TabHeader;
        /// <summary>
        /// Záložka reprezentující "Collapse" položku
        /// </summary>
        private GTabPage _TabItemCollapse;
        /// <summary>
        /// true pokud má být zobrazen TabItem pro Collapse
        /// </summary>
        private bool _TabItemCollapseIsAvailable { get { return this._TabHeaderMode.HasFlag(ShowTabHeaderMode.CollapseItem); } }
        /// <summary>
        /// Pomocí této metody si prvek <see cref="GTabPage"/> reprezentující záložku Collapse (<see cref="_TabItemCollapse"/>) zjišťuje svoji viditelnost.
        /// Získává ji z <see cref="_TabItemCollapseIsAvailable"/>.
        /// </summary>
        /// <param name="isVisible"></param>
        /// <returns></returns>
        private bool _GetCollapseIsVisible(bool isVisible) { return this._TabItemCollapseIsAvailable; }
        /// <summary>
        /// Prvek <see cref="GTabPage"/> reprezentující záložku Collapse (<see cref="_TabItemCollapse"/>) zde nastavuje svoji viditelnost.
        /// Reálně nedělá nic.
        /// </summary>
        /// <param name="isVisible"></param>
        private void _SetCollapseIsVisible(bool isVisible) { }
        /// <summary>
        /// Záložka (TabItem), která byla naposledy aktivní před provedením Collapse.
        /// Pokud bude stav Collapse zrušen prostým nastavením IsCollapsed = false, pak algoritmus provede aktivaci této záložky.
        /// </summary>
        private GTabPage _TabItemLastActive;
        /// <summary>
        /// Hodnota TabOrder pro příští vkládaný TabItem pro data
        /// </summary>
        private int _TabOrderData;
        /// <summary>
        /// Výška prostoru záhlaví
        /// </summary>
        private int _HeaderHeight = Skin.TabHeader.HeaderHeight;
        /// <summary>
        /// Výška celého containeru v situaci, kdy není Collapsed.
        /// Po přepnuté do Collapsed je reálný rozměr dán jen záhlavím (_HeaderHeight), a po následném přepnutí do Non Collapsed režimu se rozměr vrací na tuto hodnotu.
        /// </summary>
        private int _TabContainerHeight = 0;
        private ShowTabHeaderMode _TabHeaderMode;
        /// <summary>
        /// Aktuální stav Collapsed: true = je vidět jen Záhlaví (pokud není skryt), ale nejsou vidět Data, false = jsou vidět Data
        /// </summary>
        private bool _IsCollapsed;
        private List<IInteractiveItem> _Controls;
        #endregion
        #region Přidání a odebrání položek
        /// <summary>
        /// Přidá (a vrátí) novou záložku pro daný prvek
        /// </summary>
        /// <returns></returns>
        public GTabPage AddTabItem(IInteractiveItem item, Localizable.TextLoc text, Localizable.TextLoc toolTip = null, Image image = null)
        {
            if (item == null) return null;
            item.Parent = this;
            bool setItemAsActive = (this._TabHeader.PageCount <= 1);
            GTabPage tabItem = this._TabHeader.AddHeader(null, text, image, linkItem: item);
            tabItem.TabOrder = this._TabOrderData++;
            if (toolTip != null)
                tabItem.ToolTipText = toolTip;
            if (setItemAsActive)
                this._TabHeader.ActivePage = tabItem;
            this.InvalidateLayout();
            this.InvalidateChilds();
            return tabItem;
        }
        /// <summary>
        /// Vymaže všechny svoje GTabPage.
        /// </summary>
        public override void ClearItems()
        {
            base.ClearItems();
            this._TabHeader.ClearItems();
            this.InvalidateLayout();
            this.InvalidateChilds();
        }
        /// <summary>
        /// Skrýt původní metodu
        /// </summary>
        /// <param name="item"></param>
        protected new void AddItem(IInteractiveItem item) { }
        #endregion
        #region Layout
        /// <summary>
        /// Připraví souřadnice vnitřních prvků
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            this.InvalidateLayout();
        }
        /// <summary>
        /// Zneplatní data layoutu
        /// </summary>
        protected void InvalidateLayout()
        {
            this._IsHeaderVisible = null;
            this._HeaderBounds = null;
            this._IsDataVisible = null;
            this._DataBounds = null;
            this._TabControlChilds = null;
        }
        /// <summary>
        /// Zajistí platnost dat layoutu
        /// </summary>
        protected void CheckLayout()
        {
            if (this._IsHeaderVisible.HasValue && this._HeaderBounds.HasValue && this._IsDataVisible.HasValue && this._DataBounds.HasValue) return;
            this.PrepareLayout();
        }
        /// <summary>
        /// Metoda upraví vlastní layout podle parametrů.
        /// Tzn. Umístí TabHeader, a v případě Collapse nastaví this.Bounds podle potřeby.
        /// </summary>
        protected void PrepareLayout()
        {
            bool isHeaderVisible = this.IsCurrentVisibleTabHeader;

            Rectangle oldBounds = this.Bounds;
            Size oldSize = this.ClientSize;
            Size newSize = oldBounds.Size;
            Rectangle headerBounds = Rectangle.Empty;
            bool isDataVisible = !this.IsCollapsed;
            Rectangle dataBounds = Rectangle.Empty;
            int headerSize = (isHeaderVisible ? this.HeaderHeight : 0);
            int dataSize = 0;
            int controlSize = 0;
            switch (this._TabHeader.Position)
            {
                case RectangleSide.Top:
                    dataSize = (isDataVisible ? (oldSize.Height - headerSize) : 0);      // Výška prostoru pro datový container = daný prostor mínus záhlaví, nebo 0 pro Collapsed
                    controlSize = headerSize + dataSize;                                 // Výška celého GTabContaineru reaguje na viditelnost záhlaví i Collapsed datového panelu
                    headerBounds = new Rectangle(0, 0, oldSize.Width, headerSize);       // Prostor záhlaví
                    dataBounds = new Rectangle(0, headerSize, oldSize.Width, dataSize);  // Prostor datového panelu
                    newSize = new Size(oldSize.Width, controlSize);                      // Prostor celého controlu
                    break;
                case RectangleSide.Right:
                    dataSize = (isDataVisible ? (oldSize.Width - headerSize) : 0);
                    controlSize = headerSize + dataSize;
                    headerBounds = new Rectangle(oldSize.Width - headerSize, 0, headerSize, oldSize.Height);
                    dataBounds = new Rectangle(0, 0, dataSize, oldSize.Height);
                    newSize = new Size(controlSize, oldSize.Height);
                    break;
                case RectangleSide.Bottom:
                    dataSize = (isDataVisible ? (oldSize.Height - headerSize) : 0);
                    controlSize = headerSize + dataSize;
                    headerBounds = new Rectangle(0, oldSize.Height - headerSize, oldSize.Width, headerSize);
                    dataBounds = new Rectangle(0, 0, oldSize.Width, dataSize);
                    newSize = new Size(oldSize.Width, controlSize);
                    break;
                case RectangleSide.Left:
                    dataSize = (isDataVisible ? (oldSize.Width - headerSize) : 0);
                    controlSize = headerSize + dataSize;
                    headerBounds = new Rectangle(0, 0, headerSize, oldSize.Height);
                    dataBounds = new Rectangle(headerSize, 0, dataSize, oldSize.Height);
                    newSize = new Size(controlSize, oldSize.Height);
                    break;
            }

            Rectangle newBounds = new Rectangle(oldBounds.Location, newSize);
            if (this.Bounds != newBounds)
                this.Bounds = newBounds;         // Tady dojde k SetBoundsPrepareInnerItems() a následně InvalidateLayout(),
            // .. jenže ty invalidované hodnoty hned zase správně validujeme:
            this._IsHeaderVisible = isHeaderVisible;
            this._HeaderBounds = headerBounds;
            this._IsDataVisible = isDataVisible;
            this._DataBounds = dataBounds;

            // Zapamatujeme si výšku celého containeru za situace, kdy není Collapsed:
            if (isDataVisible)
                this._TabContainerHeight = controlSize;
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně má být vidět záhlaví Tabů.
        /// Reaguje na režim <see cref="TabHeaderMode"/>, na zadanou výšku záhlaví <see cref="HeaderHeight"/>, 
        /// a na počet datových záložek <see cref="TabCount"/>.
        /// </summary>
        protected bool IsCurrentVisibleTabHeader
        {
            get
            {
                ShowTabHeaderMode tabHeaderMode = this.TabHeaderMode;
                if (this.HeaderHeight <= 0 || tabHeaderMode.HasFlag(ShowTabHeaderMode.NoTabHeader)) return false;      // Je nastaveno "Nezobrazovat"
                int tabCount = this.TabCount;
                if (tabCount == 0) return false;                                         // Pro počet záhlaví 0 se Header nezobrazuje nikdy
                if (tabHeaderMode.HasFlag(ShowTabHeaderMode.Always)) return true;        // V režimu Always se Header zobrazuje vždy
                if (tabCount == 1 && tabHeaderMode == ShowTabHeaderMode.Default) return false;     // Režim je Default (bez požadavku na CollapseItem) a počet = 1 => nezobrazovat záhlaví
                return true;
            }
        }
        /// <summary>
        /// Je viditelný prostor TabHeader?
        /// </summary>
        protected bool IsHeaderVisible { get { this.CheckLayout(); return this._IsHeaderVisible.Value; } }
        /// <summary>
        /// Souřadnice prostoru TabHeader v rámci this.Bounds
        /// </summary>
        protected Rectangle HeaderBounds { get { this.CheckLayout(); return this._HeaderBounds.Value; } }
        /// <summary>
        /// Je viditelný prostor Data?
        /// </summary>
        protected bool IsDataVisible { get { this.CheckLayout(); return this._IsDataVisible.Value; } }
        /// <summary>
        /// Souřadnice prostoru Data v rámci this.Bounds
        /// </summary>
        protected Rectangle DataBounds { get { this.CheckLayout(); return this._DataBounds.Value; } }
        /// <summary>
        /// Viditelnost Header
        /// </summary>
        private bool? _IsHeaderVisible;
        /// <summary>
        /// Souřadnice prostoru Header
        /// </summary>
        private Rectangle? _HeaderBounds;
        /// <summary>
        /// Viditelnost dat
        /// </summary>
        private bool? _IsDataVisible;
        /// <summary>
        /// Souřadnice prostoru Data
        /// </summary>
        private Rectangle? _DataBounds;
        #endregion
        #region Public property a metody
        /// <summary>
        /// Pole všech položek, které reprezentují datové záložky - v jejich nativním pořadí (=tak jak se přidávaly).
        /// </summary>
        /// <remarks>V těchto záložkách není uvedena záložka "Collapse".</remarks>
        public GTabPage[] TabItems { get { return this._TabHeader.Pages.Where(t => (!t.IsCollapse)).ToArray(); } }
        /// <summary>
        /// Počet TAB prvků, které reprezentují datové záložky
        /// </summary>
        /// <remarks>V těchto záložkách není uvedena záložka "Collapse".</remarks>
        public int TabCount { get { return this.TabItems.Length; } }
        /// <summary>
        /// Režim zobrazování záhlaví
        /// </summary>
        public ShowTabHeaderMode TabHeaderMode
        {
            get { return this._TabHeaderMode; }
            set
            {
                ShowTabHeaderMode oldValue = this._TabHeaderMode;
                ShowTabHeaderMode newValue = value;
                if (newValue == oldValue) return;
                this._TabHeaderMode = newValue;
                this.InvalidateChilds();
                this.InvalidateLayout();
            }
        }
        /// <summary>
        /// Režim zobrazování záhlaví
        /// </summary>
        public RectangleSide TabHeaderPosition
        {
            get { return this._TabHeader.Position; }
            set
            {
                RectangleSide oldValue = this._TabHeader.Position;
                RectangleSide newValue = value;
                if (newValue == oldValue) return;
                this._TabHeader.Position = newValue;
                this.InvalidateLayout();
            }
        }
        /// <summary>
        /// Výška záhlaví. Výška je to v režimu vodorovném, kdežto při svislém režimu je <see cref="HeaderHeight"/> použito pro šířku záhlaví.
        /// Lze vložit hodnotu 0 (a menší): pak je záhlaví skryto. Platný rozsah pro viditelné záhlaví je 18 - 120 pixelů.
        /// </summary>
        public int HeaderHeight
        {
            get { return this._HeaderHeight; }
            set
            {
                int oldValue = this._HeaderHeight;
                int newValue = (value <= 0 ? 0 : (value < 18 ? 18 : (value > 120 ? 120 : value)));
                if (newValue == oldValue) return;
                this._HeaderHeight = newValue;
                this.InvalidateLayout();
            }
        }
        /// <summary>
        /// Control, který je aktuálně zobrazen.
        /// Pokud je stav <see cref="IsCollapsed"/>, pak <see cref="ActiveControl"/> je null.
        /// </summary>
        public IInteractiveItem ActiveControl
        {
            get
            {
                GTabPage activePage = this.ActivePage;
                return (activePage != null ? activePage.DataControl : null);
            }
            set
            {
                IInteractiveItem oldValue = this.ActiveControl;
                IInteractiveItem newValue = value;
                if (newValue != null && !Object.ReferenceEquals(newValue, oldValue))
                {   // Pouze pokud je nový objekt zadán, a je odlišný od aktuálního:
                    GTabPage tabItem = (newValue != null ? this._TabHeader.Pages.FirstOrDefault(t => Object.ReferenceEquals(t, newValue)) : null);
                    if (tabItem != null)
                    {
                        this.ActivePage = tabItem;         // Vyvolá událost TabHeader.ActiveItemChanged => this._TabHeader_ActiveItemChanged;
                        this.CallActiveControlChanged(oldValue, newValue, EventSourceType.ApplicationCode);
                    }
                }
            }
        }
        /// <summary>
        /// Aktivní záhlaví
        /// </summary>
        public GTabPage ActivePage
        {
            get { return this._TabHeader.ActivePage; }
            set { this._TabHeader.ActivePage = value; /* Vyvolá event TabHeader_ActiveItemChanged => this._TabHeader_ActiveItemChanged  */ }
        }
        /// <summary>
        /// Obsahuje true, pokud this je ve stavu Collapsed, tzn. je zobrazen jen pás se záhlavím (<see cref="GTabHeader"/>),
        /// ale není zobrazen navázaný Control.
        /// </summary>
        public bool IsCollapsed
        {
            get { return this._IsCollapsed; }
            set
            {
                bool oldValue = this._IsCollapsed;
                bool newValue = value;
                if (newValue == oldValue) return;

                // Setování této property se provádí zvenku (aplikačním kódem), a jejím úkolem je aktivovat záložku Collapse nebo LastActive:
                GTabPage tabItem = ((newValue) ? this._TabItemCollapse : this._TabItemLastActive);
                if (!newValue && tabItem == null)
                    // Pokud je požadováno IsCollapsed = false (tzn. zobrazit data), ale v _TabItemLastActive je null, pak budeme aktivovat první záložku s daty:
                    tabItem = this._TabHeader.Pages.FirstOrDefault(t => !t.IsCollapse);

                // Toto setování zajistí aktivaci odpovídající záložky tak, jako by na ni klikl uživatel, 
                // poté this._TabHeader vyvolá event this._TabHeader.ActiveItemChanged, a tedy jeho zdejší handler this._TabHeader_ActiveItemChanged,
                // poté zdejší metodu this._TabHeaderItemChanged(), která vloží správnou hodnotu do this.IsCollapsedInternal:
                this._TabHeader.ActivePage = tabItem;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this je ve stavu Collapsed, tzn. je zobrazen jen pás se záhlavím (<see cref="GTabHeader"/>),
        /// ale není zobrazen navázaný Control.
        /// Setování této property neřeší přepínání TabItem, ale vyvolá PrepareLayout() a event IsCollapsedChanged.
        /// </summary>
        protected bool IsCollapsedInternal
        {
            get { return this._IsCollapsed; }
            set
            {
                bool oldValue = this._IsCollapsed;
                bool newValue = value;
                if (newValue != oldValue)
                {
                    this._IsCollapsed = value;
                    this.InvalidateChilds();
                    this.InvalidateLayout();
                    this.PrepareLayout();        // Chceme mít platné hodnoty souřadnic ještě před tím, než zavoláme event IsCollapsedChanged, protože v něm můžeme reagovat na již platné hodnoty
                    this.CallIsCollapsedChanged(oldValue, newValue, EventSourceType.ApplicationCode);
                }
            }
        }
        #endregion
        #region Draw, Childs
        /// <summary>
        /// Child prvky
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckChilds(); return this._TabControlChilds; } }
        /// <summary>
        /// Invaliduje Child prvky
        /// </summary>
        protected void InvalidateChilds()
        {
            this._TabControlChilds = null;
        }
        /// <summary>
        /// Zajistí platnost pole Child prvků
        /// </summary>
        protected void CheckChilds()
        {
            if (this._TabControlChilds != null) return;

            List<IInteractiveItem> items = new List<IInteractiveItem>();

            if (this.IsHeaderVisible)
            {
                Rectangle headerBounds = this.HeaderBounds;
                if (this._TabHeader.Bounds != headerBounds)
                    this._TabHeader.Bounds = headerBounds;
                items.Add(this._TabHeader);
            }

            if (this.IsDataVisible)
            {
                IInteractiveItem dataItem = (this.ActivePage != null ? this.ActivePage.DataControl : null);
                if (dataItem != null)
                {
                    Rectangle dataBounds = this.DataBounds;
                    if (dataItem.Bounds != dataBounds)
                        dataItem.Bounds = dataBounds;
                    items.Add(dataItem);
                }
            }

            this._TabControlChilds = items.ToArray();
        }
        private IInteractiveItem[] _TabControlChilds;
        #endregion
        #region Eventy
        /// <summary>
        /// Zavolá metody <see cref="OnActivePageChanged"/> a eventhandler <see cref="ActivePageChanged"/>.
        /// Tato metoda se má volat pouze při reálné změně stránky, nikoli při změně <see cref="IsCollapsed"/>.
        /// </summary>
        protected GTabPage CallActivePageChanged(GTabPage oldValue, GTabPage newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<GTabPage> args = new GPropertyChangeArgs<GTabPage>(oldValue, newValue, eventSource);
            this.OnActivePageChanged(args);
            if (!this.IsSuppressedEvent && this.ActivePageChanged != null)
                this.ActivePageChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Metoda prováděná při změně hodnoty <see cref="IsCollapsed"/>
        /// </summary>
        protected virtual void OnActivePageChanged(GPropertyChangeArgs<GTabPage> args) { }
        /// <summary>
        /// Event provedený po změně hodnoty <see cref="ActivePage"/>
        /// </summary>
        public event GPropertyChangedHandler<GTabPage> ActivePageChanged;

        /// <summary>
        /// Zavolá metody <see cref="OnActiveControlChanged"/> a eventhandler <see cref="ActiveControlChanged"/>.
        /// </summary>
        protected IInteractiveItem CallActiveControlChanged(IInteractiveItem oldValue, IInteractiveItem newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<IInteractiveItem> args = new GPropertyChangeArgs<IInteractiveItem>(oldValue, newValue, eventSource);
            this.OnActiveControlChanged(args);
            if (!this.IsSuppressedEvent && this.ActiveControlChanged != null)
                this.ActiveControlChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Metoda prováděná při změně hodnoty <see cref="IsCollapsed"/>
        /// </summary>
        protected virtual void OnActiveControlChanged(GPropertyChangeArgs<IInteractiveItem> args) { }
        /// <summary>
        /// Event provedený po změně hodnoty <see cref="IsCollapsed"/>
        /// </summary>
        public event GPropertyChangedHandler<IInteractiveItem> ActiveControlChanged;

        /// <summary>
        /// Zavolá metody <see cref="OnIsCollapsedChanged"/> a eventhandler <see cref="IsCollapsedChanged"/>.
        /// </summary>
        protected bool CallIsCollapsedChanged(bool oldValue, bool newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<bool> args = new GPropertyChangeArgs<bool>(oldValue, newValue, eventSource);
            this.OnIsCollapsedChanged(args);
            if (!this.IsSuppressedEvent && this.IsCollapsedChanged != null)
                this.IsCollapsedChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Metoda prováděná při změně hodnoty <see cref="IsCollapsed"/>
        /// </summary>
        protected virtual void OnIsCollapsedChanged(GPropertyChangeArgs<bool> args) { }
        /// <summary>
        /// Event provedený po změně hodnoty <see cref="IsCollapsed"/>
        /// </summary>
        public event GPropertyChangedHandler<bool> IsCollapsedChanged;
        #endregion
    }
    #endregion
    #region class GTabHeader : záhlaví bloku stránek; interface ITabHeaderInternal : přístup do interních prvků
    /// <summary>
    /// GTabHeader : záhlaví bloku stránek
    /// </summary>
    public class GTabHeader : InteractiveContainer, ITabHeaderInternal
    {
        #region Konstruktor a public data celého headeru
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GTabHeader(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTabHeader()
        {
            this._PageList = new List<GTabPage>();
            this._Position = RectangleSide.Top;
            this._HeaderSizeRange = new Int32Range(50, 600);
            this.__ActiveIndex = -1;
        }
        /// <summary>
        /// Připraví souřadnice vnitřních prvků
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            this.InvalidateChildItems();
        }
        /// <summary>
        /// Pozice, kde se záhlaví nachází vzhledem k datové oblasti, pro kterou má přepínat její obsah
        /// </summary>
        public RectangleSide Position { get { return this._Position; } set { this._Position = value; this.InvalidateChildItems(); } }
        private RectangleSide _Position;
        /// <summary>
        /// Rozsah velikosti (délky) jednotlivého záhlaví, počet pixelů.
        /// Výchozí hodnota je { 50 ÷ 600 }.
        /// </summary>
        public Int32Range HeaderSizeRange { get { return this._HeaderSizeRange; } set { this._HeaderSizeRange = value; this.InvalidateChildItems(); } }
        private Int32Range _HeaderSizeRange;
        /// <summary>
        /// Počet položek celkem přítomných v poli <see cref="Pages"/>
        /// </summary>
        public int PageCount { get { return this._PageList.Count; } }
        /// <summary>
        /// Pole všech položek, v jejich nativním pořadí (=tak jak se přidávaly)
        /// </summary>
        public GTabPage[] Pages { get { return this._PageList.ToArray(); } }
        /// <summary>
        /// Data aktuálně aktivního záhlaví. Může být null.
        /// Setování vyvolá eventy ActiveItemChanged
        /// </summary>
        public GTabPage ActivePage
        {
            get { return this._ActivePage; }
            set
            {
                GTabPage oldValue = this._ActivePage;
                GTabPage newValue = value;
                if (newValue == null || Object.ReferenceEquals(newValue, oldValue)) return;        // Není zadáno, nebo Není změna
                if (!this._PageList.Any(i => Object.ReferenceEquals(i, newValue))) return;         // není to žádná z našich stránek

                this._ActivePage = newValue;
                this._ShowDataItems(newValue);
                this.CallActivePageChanged(oldValue, newValue, EventSourceType.InteractiveChanged);
                this.Repaint();
            }
        }
        private GTabPage _ActivePage;
        /// <summary>
        /// Index aktuálního záhlaví. Vrací __ActiveHeaderIndex (bez kontrol). 
        /// Setování provede kontrolu hodnoty a po změně vyvolá ActiveItemChanged, a nastavení viditelnosti u <see cref="GTabPage.DataControl"/>.
        /// </summary>
        private int _ActiveIndex
        {
            get { return this.__ActiveIndex; }
            set
            {
                int count = this.PageCount;
                int oldIndex = this.__ActiveIndex;
                GTabPage oldItem = this.ActivePage;

                int newIndex = value;
                if (newIndex >= 0 && newIndex < count && newIndex != oldIndex)
                {
                    this.__ActiveIndex = newIndex;
                    this._ShowDataItems(this._PageList[newIndex]);
                    GTabPage newItem = this.ActivePage;
                    this.CallActivePageChanged(oldItem, newItem, EventSourceType.InteractiveChanged);
                }
            }
        }
        private int __ActiveIndex;
        /// <summary>
        /// Metoda zajistí nastavení IsVisible pro všechny <see cref="GTabPage.DataControl"/> v <see cref="_PageList"/>.
        /// Pouze záložka <see cref="ActivePage"/> bude mít LinkItem.IsVisible = true, ostatní budou mít false.
        /// </summary>
        private void _ShowDataItems()
        {
            this._ShowDataItems(this.ActivePage);
        }
        /// <summary>
        /// Metoda zajistí nastavení IsVisible pro všechny <see cref="GTabPage.DataControl"/> v <see cref="_PageList"/>.
        /// Pouze záložka odpovídající zadanému záhlaví bude mít LinkItem.IsVisible = true, ostatní budou mít false.
        /// </summary>
        /// <param name="activeItem"></param>
        private void _ShowDataItems(GTabPage activeItem)
        {
            foreach (GTabPage tabItem in this._PageList)
            {
                IInteractiveItem linkItem = tabItem.DataControl;
                if (linkItem != null)
                {
                    bool isVisible = (activeItem != null && Object.ReferenceEquals(tabItem, activeItem));
                    if (linkItem.Is.Visible != isVisible)
                    {
                        linkItem.Is.Visible = isVisible;
                        if (isVisible)
                            linkItem.Repaint();
                        else
                            linkItem.Parent.Repaint();
                    }
                }
            }
        }
        /// <summary>
        /// Font obecně použitý pro všechny položky.
        /// Pokud je null (což je default), pak se použije <see cref="FontInfo.CaptionBoldBig"/>.
        /// </summary>
        public FontInfo Font { get { return this._Font; } set { this._Font = value; this.InvalidateChildItems(); } } private FontInfo _Font;
        /// <summary>
        /// Aktuální font, nikdy není null.
        /// </summary>
        protected FontInfo CurrentFont { get { return (this._Font != null ? this._Font : FontInfo.CaptionBoldBig); } }
        #endregion
        #region Add header, Remove header, Get header
        /// <summary>
        /// Přidá další záložku
        /// </summary>
        /// <param name="text"></param>
        /// <param name="image"></param>
        /// <param name="tabOrder"></param>
        /// <param name="linkItem"></param>
        /// <returns></returns>
        public GTabPage AddHeader(Localizable.TextLoc text, Image image = null, int tabOrder = 0, IInteractiveItem linkItem = null)
        {
            return this._AddHeader(false, null, text, image, tabOrder, linkItem);
        }
        /// <summary>
        /// Přidá další záložku
        /// </summary>
        /// <param name="key"></param>
        /// <param name="text"></param>
        /// <param name="image"></param>
        /// <param name="tabOrder"></param>
        /// <param name="linkItem"></param>
        /// <returns></returns>
        public GTabPage AddHeader(string key, Localizable.TextLoc text, Image image = null, int tabOrder = 0, IInteractiveItem linkItem = null)
        {
            return this._AddHeader(false, key, text, image, tabOrder, linkItem);
        }
        /// <summary>
        /// Přidá záložku pro button Collapse
        /// </summary>
        /// <returns></returns>
        public GTabPage AddCollapseHeader()
        {
            // TabItemKeyCollapse, "", Components.IconStandard.GoTop, tabOrder: 99999
            return this._AddHeader(true, null, "", Components.IconStandard.GoTop, 99999, null);
        }
        /// <summary>
        /// Přidá další záložku
        /// </summary>
        /// <param name="isCollapse"></param>
        /// <param name="key"></param>
        /// <param name="text"></param>
        /// <param name="image"></param>
        /// <param name="tabOrder"></param>
        /// <param name="linkItem"></param>
        /// <returns></returns>
        private GTabPage _AddHeader(bool isCollapse, string key, Localizable.TextLoc text, Image image, int tabOrder, IInteractiveItem linkItem)
        {
            GTabPage tabPage = new GTabPage(this, isCollapse, key, text, image, linkItem, tabOrder);
            this._PageList.Add(tabPage);
            tabPage.TabPagePaintBackGround += _TabHeaderItemPaintBackGround;
            if (this.ActivePage == null)
                this.ActivePage = tabPage;
            this._ShowDataItems();
            this.InvalidateChildItems();
            return tabPage;
        }
        /// <summary>
        /// Vymaže všechny svoje GTabPage.
        /// </summary>
        public override void ClearItems()
        {
            base.ClearItems();
            this._PageList.Clear();
            this.InvalidateChildItems();
        }
        #endregion
        #region Eventy
        /// <summary>
        /// Eventhandler pro událost na konkrétním záhlaví (TabItem), kde se provádí uživatelské vykreslení pozadí
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabHeaderItemPaintBackGround(object sender, GUserDrawArgs e)
        {
            this.OnTabPagePaintBackGround(sender, e);
            if (this.TabPagePaintBackGround != null)
                this.TabPagePaintBackGround(sender, e);
        }
        /// <summary>
        /// Háček pro uživatelské kreslení pozadí záhlaví
        /// </summary>
        /// <param name="sender">Objekt kde k události došlo = <see cref="GTabPage"/></param>
        /// <param name="e">Data pro kreslení</param>
        protected virtual void OnTabPagePaintBackGround(object sender, GUserDrawArgs e) { }
        /// <summary>
        /// Event pro uživatelské kreslení pozadí záhlaví.
        /// Jako parametr sender je předán objekt konkrétního záhlaví <see cref="GTabPage"/>.
        /// </summary>
        public event GUserDrawHandler TabPagePaintBackGround;
        /// <summary>
        /// Vyvolá háček OnActiveItemChanged a event ActiveItemChanged
        /// </summary>
        protected GTabPage CallActivePageChanged(GTabPage oldValue, GTabPage newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<GTabPage> args = new GPropertyChangeArgs<GTabPage>(oldValue, newValue, eventSource);
            this.OnActivePageChanged(args);
            if (!this.IsSuppressedEvent && this.ActivePageChanged != null)
                this.ActivePageChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Provede se po změně aktivní záložky (<see cref="ActivePage"/>)
        /// </summary>
        protected virtual void OnActivePageChanged(GPropertyChangeArgs<GTabPage> args) { }
        /// <summary>
        /// Event volaný po změně aktivní záložky (<see cref="ActivePage"/>)
        /// </summary>
        public event GPropertyChangedHandler<GTabPage> ActivePageChanged;
        #endregion
        #region Uspořádání jednotlivých záhlaví - výpočty jejich Bounds podle orientace a jejich textu, fontu a zdejších souřadnic
        /// <summary>
        /// Invaliduje pole záložek.
        /// Volá se po změně pořadí stránek, po změně viditelnost, po změně textu / ikony, po posunu obsahu (scroll).
        /// Důsledkem je, že následující získání 
        /// </summary>
        protected void InvalidateChildItems() { this._SortedItems = null; this._HeaderContentLength = null; }
        /// <summary>
        /// Vrátí platné pole Child položek
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        protected GTabPage[] GetChilds(Graphics graphics)
        {
            this.CheckContent(graphics);
            return this._SortedItems.ToArray();
        }
        /// <summary>
        /// Zajistí, že obsah (_SortedList, ) je korektně připraven.
        /// </summary>
        protected void CheckContent(Graphics graphics)
        {
            if (this._SortedItems != null && this._HeaderContentLength.HasValue) return;
            this.PrepareContent(graphics);
        }
        /// <summary>
        /// Připraví vše pro korektní vykreslení headeru.
        /// </summary>
        /// <param name="graphics"></param>
        protected void PrepareContent(Graphics graphics)
        {
            // Pole setříděných a viditelných záhlaví:
            List<GTabPage> sortedList = new List<GTabPage>(this._PageList.Where(i => i.Is.Visible));
            int count = sortedList.Count;
            if (count > 1) sortedList.Sort(InteractiveObject.CompareByTabOrderAsc);

            bool hasGraphics = (graphics != null);
            if (!hasGraphics)
                graphics = Graphics.FromImage(new Bitmap(256, 128));

            // Počáteční pixel = mínus offset, ale nesmí být kladný:
            int begin = -this._HeaderOffsetPixel;
            if (begin > 0) begin = 0;

            // Určit velikost záhlaví a jejich souřadnice:
            foreach (GTabPage item in sortedList)
                item.PrepareBounds(graphics, ref begin);

            if (!hasGraphics)
                graphics.Dispose();

            this._HeaderContentLength = begin;
            this._SortedItems = sortedList.ToArray();

            // Vytvořit pořadí záhlaví pro zobrazování tak, aby aktuální záhlaví bylo navrchu (tj. poslední v seznamu):
            List<GTabPage> childList = new List<GTabPage>();
            GTabPage activeItem = this.ActivePage;

            // Nejprve přidám prvky vlevo od aktivního prvku, počínaje prvním, s tím že aktivní prvek v tomto chodu již nepřidáme:
            int activeIndex = -1;
            for (int i = 0; i < count; i++)
            {
                GTabPage item = sortedList[i];
                if (activeItem != null && Object.ReferenceEquals(item, activeItem))
                {
                    activeIndex = i;
                    break;
                }
                childList.Add(item);
            }

            // Poté přidám prvky ležící napravo od aktivního prvku, počínaje posledním, a konče právě tím aktivním 
            //  = tak se dostane na poslední místo = tj. na vrcholek v seznamu childList:
            if (activeIndex >= 0)
            {
                for (int i = count - 1; i >= activeIndex; i--)
                {
                    GTabPage item = sortedList[i];
                    childList.Add(item);
                }
            }

            this._ChildItems = childList.ToArray();
        }
        /// <summary>
        /// Souřadnice pixelu (vzhledem k this.Bounds), na které začíná první záhlaví.
        /// Výchozí je 0.
        /// Kladná hodnota říká, že reálně jsou vidět záhlaví až od daného pixelu.
        /// Záporná hodnota zde nemá co dělat.
        /// </summary>
        protected int HeaderOffsetPixel { get { return this._HeaderOffsetPixel; } set { this._HeaderOffsetPixel = value; this.InvalidateChildItems(); } } private int _HeaderOffsetPixel;
        /// <summary>
        /// true pokud this header je orientován vodorovně (jeho <see cref="Position"/> je <see cref="RectangleSide.Top"/> nebo <see cref="RectangleSide.Bottom"/>)
        /// </summary>
        protected bool HeaderIsHorizontal { get { return (this.Position == RectangleSide.Top || this.Position == RectangleSide.Bottom); } }
        /// <summary>
        /// true pokud this header je orientován svisle (jeho <see cref="Position"/> je <see cref="RectangleSide.Left"/> nebo <see cref="RectangleSide.Right"/>)
        /// </summary>
        protected bool HeaderIsVertical { get { return (this.Position == RectangleSide.Left || this.Position == RectangleSide.Right); } }
        /// <summary>
        /// "Výška" záhlaví v pixelech: pro vodorovný header jde o Bounds.Height, pro svislý header jde o Bounds.Width.
        /// Obecně jde o tu menší souřadnici.
        /// </summary>
        internal int HeaderHeight { get { return (this.HeaderIsHorizontal ? this.Bounds.Height : (this.HeaderIsVertical ? this.Bounds.Width : 0)); } }
        /// <summary>
        /// "Délka" prostoru pro záhlaví v pixelech: pro vodorovný header jde o Bounds.Width, pro svislý header jde o Bounds.Height.
        /// Jedná se o délku prostoru, do něhož se vkládají jednotlivé záhlaví.
        /// </summary>
        protected int HeaderLength { get { return (this.HeaderIsHorizontal ? this.Bounds.Width : (this.HeaderIsVertical ? this.Bounds.Height : 0)); } }
        /// <summary>
        /// "Délka" obsahu záhlaví v pixelech: jde o součet délek jednotlivých Itemů. Je zjištěn výpočtem, může se lišit od <see cref="HeaderLength"/>, i když je měřen ve shodném směru.
        /// Součet délky všech záhlaví.
        /// </summary>
        protected int HeaderContentLength { get { this.CheckContent(null); return this._HeaderContentLength.Value; } } private int? _HeaderContentLength;
        /// <summary>
        /// Pole obsahující všechny prvky TabItem v pořadí, jak byly přidávány
        /// </summary>
        private List<GTabPage> _PageList;
        /// <summary>
        /// Pole obsahující viditelné prvky TabItem v tom pořadí, v jakém jdou za sebou od začátku do konce (podle jejich <see cref="IInteractiveItem.TabOrder"/>)
        /// </summary>
        private GTabPage[] _SortedItems;
        /// <summary>
        /// Pole obsahující viditelné prvky TabItem v tom pořadí, jak mají být vykresleny (poslední v poli je aktivní záložka = na vrchu)
        /// </summary>
        private GTabPage[] _ChildItems;
        #endregion
        #region Draw, Interactivity
        /// <summary>
        /// Vykreslí TabHeader
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Color spaceColor = this.CurrentBackColor;                  // tam je default: Skin.TabHeader.SpaceColor;
            if (spaceColor.A > 0)
                e.Graphics.FillRectangle(Skin.Brush(spaceColor), absoluteBounds);

            this.DrawHeaderLine(e, absoluteBounds);
        }
        /// <summary>
        /// Vykreslí linku "pod" všemi položkami záhlaví, odděluje záhlaví a data.
        /// Aktivní záhlaví pak tuto linku zruší svým pozadím.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        protected void DrawHeaderLine(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            Rectangle line = Rectangle.Empty;
            switch (this.Position)
            {
                case RectangleSide.Top:
                    line = new Rectangle(absoluteBounds.X, absoluteBounds.Bottom - 2, absoluteBounds.Width, 2);
                    break;
                case RectangleSide.Right:
                    line = new Rectangle(absoluteBounds.X, absoluteBounds.Y, 2, absoluteBounds.Height);
                    break;
                case RectangleSide.Bottom:
                    line = new Rectangle(absoluteBounds.X, absoluteBounds.Y, absoluteBounds.Width, 2);
                    break;
                case RectangleSide.Left:
                    line = new Rectangle(absoluteBounds.Right - 2, absoluteBounds.Y, 2, absoluteBounds.Height);
                    break;
            }
            if (!line.HasPixels()) return;

            GTabPage activeItem = this.ActivePage;
            Color color = ((activeItem != null && activeItem.BackColor.HasValue) ? activeItem.BackColor.Value : Skin.TabHeader.BackColorActive);

            e.Graphics.FillRectangle(Skin.Brush(color), line);
        }
        /// <summary>
        /// Protože: this prvek může mít transparentní svoje pozadí, a navíc podporuje proměnný obsah popředí, musíme zajistit přemalování Parenta před přemalováním this.
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.OnBackColorAlpha; } }
        /// <summary>
        /// Aby fungovalo přemalování parenta v režimu <see cref="RepaintParentMode.OnBackColorAlpha"/>, musíme vracet korektní barvu BackColor.
        /// Výchozí je <see cref="Skin.TabHeader"/>.SpaceColor
        /// </summary>
        protected override Color BackColorDefault { get { return Skin.TabHeader.SpaceColor; } }
        /// <summary>
        /// Child prvky
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetChilds(null); } }
        #endregion
        #region Implementace ITabHeaderInternal
        void ITabHeaderInternal.InvalidateChildItems() { this.InvalidateChildItems(); }
        RectangleSide ITabHeaderInternal.Position { get { return this.Position; } }
        bool ITabHeaderInternal.HeaderIsVertical { get { return this.HeaderIsVertical; } }
        bool ITabHeaderInternal.HeaderIsHorizontal { get { return this.HeaderIsHorizontal; } }
        FontInfo ITabHeaderInternal.CurrentFont { get { return this.CurrentFont.Clone; } }
        #endregion
    }
    /// <summary>
    /// Interface pro přístup k interním prvkům třídy <see cref="GTabHeader"/>
    /// </summary>
    public interface ITabHeaderInternal
    {
        /// <summary>
        /// Invaliduje Child prvky, po změně
        /// </summary>
        void InvalidateChildItems();
        /// <summary>
        /// Vrací pozici, kde má být Header
        /// </summary>
        RectangleSide Position { get; }
        /// <summary>
        /// true = záhlaví je svislé
        /// </summary>
        bool HeaderIsVertical { get; }
        /// <summary>
        /// true = záhlaví je vodorovné
        /// </summary>
        bool HeaderIsHorizontal { get; }
        /// <summary>
        /// Aktuální font
        /// </summary>
        FontInfo CurrentFont { get; }
    }
    #endregion
    #region class GTabPage : jedna položka reprezentující jedno záhlaví stránky
    /// <summary>
    /// GTabPage : Třída reprezentující jedno záhlaví v objektu <see cref="TabHeader"/>.
    /// </summary>
    public class GTabPage : InteractiveContainer, ITabHeaderItemPaintData
    {
        #region Konstruktor a public data jednoho záhlaví
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="tabHeader"></param>
        /// <param name="isCollapse"></param>
        /// <param name="key"></param>
        /// <param name="text"></param>
        /// <param name="image"></param>
        /// <param name="dataControl"></param>
        /// <param name="tabOrder"></param>
        internal GTabPage(GTabHeader tabHeader, bool isCollapse, string key, Localizable.TextLoc text, Image image, IInteractiveItem dataControl, int tabOrder)
        {
            this.Parent = tabHeader;
            this.TabHeader = tabHeader;
            this._IsCollapse = isCollapse;
            this._Key = key;
            this._Text = text;
            this._Image = image;
            this.TabOrder = tabOrder;
            this._DataControl = dataControl;
            this.TabHeaderInvalidate();
            this.Is.SetVisible = this._SetVisible;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "TabPage: " + (this.IsCollapse ? "[Collapse]" : (this.Text != null ? this.Text.Text : "{Null}")) + "; " + base.ToString();
        }
        /// <summary>
        /// Zajistí invalidaci obsahu v <see cref="TabHeader"/>
        /// </summary>
        protected void TabHeaderInvalidate() { ((ITabHeaderInternal)this.TabHeader).InvalidateChildItems(); }
        /// <summary>
        /// Reference na vlastníka této záložky
        /// </summary>
        public GTabHeader TabHeader { get; private set; }
        /// <summary>
        /// Příznak, že toto záhlaví reprezentuje tlačítko Collapse.
        /// </summary>
        public bool IsCollapse { get { return this._IsCollapse; } }
        private bool _IsCollapse;
        /// <summary>
        /// Klíč headeru, zadaný při jeho vytváření.
        /// Jeho obsah a unikátnost je čistě na uživateli.
        /// </summary>
        public string Key { get { return this._Key; } }
        private string _Key;
        /// <summary>
        /// Text v záhlaví
        /// </summary>
        public Localizable.TextLoc Text { get { return this._Text; } set { this._Text = value; this.TabHeaderInvalidate(); } }
        private Localizable.TextLoc _Text;
        /// <summary>
        /// Text v tooltipu
        /// </summary>
        public Localizable.TextLoc ToolTipText { get { return this._ToolTipText; } set { this._ToolTipText = value; this.TabHeaderInvalidate(); } }
        private Localizable.TextLoc _ToolTipText;
        /// <summary>
        /// Ikonka
        /// </summary>
        public Image Image { get { return this._Image; } set { this._Image = value; this.TabHeaderInvalidate(); } }
        private Image _Image;
        /// <summary>
        /// Viditelnost buttonu Close (pro zavření záložky).
        /// Výchozí = false.
        /// </summary>
        public bool CloseButtonVisible { get { return this._CloseButtonVisible; } set { this._CloseButtonVisible = value; this.TabHeaderInvalidate(); } }
        private bool _CloseButtonVisible;
        /// <summary>
        /// Barva tohoto záhlaví. Null = defaultní dle schematu.
        /// Tuto barvu bude mít záhlaví v aktivním stavu.
        /// Pokud bude záhlaví neaktivní, pak bude mít tuto barvu morfovanou z 25% do standardní barvy pozadí neaktivního záhlaví.
        /// </summary>
        public new Color? BackColor { get { return this._BackColorTab; } set { this._BackColorTab = value; } }
        private Color? _BackColorTab;
        /// <summary>
        /// Grafický prvek, který je zobrazován tímto záhlavím
        /// </summary>
        public IInteractiveItem DataControl { get { return this._DataControl; } set { this._DataControl = value; } }
        private IInteractiveItem _DataControl;
        /// <summary>
        /// Viditelnost záhlaví.
        /// Set metoda, použitá v <see cref="InteractiveProperties"/> po setování hodnoty Visible.
        /// </summary>
        private void _SetVisible(bool value)
        {
            this.TabHeaderInvalidate();
        }
        /// <summary>
        /// Obsahuje true, pokud this záhlaví je aktivní
        /// </summary>
        public bool IsActive { get { return (this.TabHeader != null && Object.ReferenceEquals(this, this.TabHeader.ActivePage)); } }
        #endregion
        #region Souřadnice záhlaví a jeho vnitřních položek, jeho výpočty, komparátor podle TabOrder
        /// <summary>
        /// Metoda připraví souřadnice tohoto prvku (<see cref="GTabPage"/>) a jeho vnitřní souřadnice (ikona, text, close button).
        /// Vychází z pixelu begin, kde má zíhlaví začínat, z orientace headeru a jeho fontu, a z vnitřních dat záhlaví.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="begin"></param>
        internal void PrepareBounds(Graphics graphics, ref int begin)
        {
            int height = this.TabHeader.HeaderHeight;
            bool headerIsVertical = ((ITabHeaderInternal)this.TabHeader).HeaderIsVertical;
            bool headerIsReverseOrder = (this.TabHeader.Position == RectangleSide.Left);
            Int32Range contentHeight = Int32Range.CreateFromCenterSize(height / 2, height - 6);         // Prostor pro vnitřní obsah ve směru výšky headeru
            Int32Range textLengthRange = GetLengthRangeText(contentHeight);

            int contentPosition = LengthBorder;    // Začátek následujícího prvku ve směru délky headeru
            if (!headerIsReverseOrder)
            {
                PrepareBoundsImage(graphics, headerIsVertical, ref contentPosition, contentHeight);     // Ikonka
                PrepareBoundsText(graphics, headerIsVertical, ref contentPosition, contentHeight, textLengthRange);// Text
                PrepareBoundsClose(graphics, headerIsVertical, ref contentPosition, contentHeight);     // Close button
            }
            else
            {
                PrepareBoundsClose(graphics, headerIsVertical, ref contentPosition, contentHeight);     // Close button
                PrepareBoundsText(graphics, headerIsVertical, ref contentPosition, contentHeight, textLengthRange);// Text
                PrepareBoundsImage(graphics, headerIsVertical, ref contentPosition, contentHeight);     // Ikonka
            }
            contentPosition += (LengthBorder - LengthSpace);

            PrepareBoundsTotal(graphics, headerIsVertical, ref begin, contentPosition, height);         // Celkové rozměry
        }
        /// <summary>
        /// Metoda vypočte a vrátí rozsah délky (v pixelech) pro pole Text.
        /// Vychází přitom z povoleného rozsahu <see cref="GTabHeader.HeaderSizeRange"/>, a z velikosti zdejší ikony a close buttonu.
        /// Pokud není povolený rozsah <see cref="GTabHeader.HeaderSizeRange"/> zadán (je null) nebo pokud by výsledná velikost textu Max byla menší než 30 px, vrací se null.
        /// </summary>
        /// <param name="contentHeight"></param>
        /// <returns></returns>
        private Int32Range GetLengthRangeText(Int32Range contentHeight)
        {
            Int32Range sizeRange = this.TabHeader.HeaderSizeRange;         // Povolená délka celého záhlaví (tj. včetně okrajů, mezer a ikon)
            if (sizeRange == null) return null;

            int imageLen = this.GetLengthImage(contentHeight);
            int closeLen = (this.CloseButtonVisible ? LengthClose : 0);
            int fixedLen = LengthBorder +
                (imageLen == 0 ? 0 : imageLen + LengthSpace) +
                (closeLen == 0 ? 0 : closeLen + LengthSpace) +
                ((imageLen == 0 && closeLen == 0) ? LengthBorder : (LengthBorder - LengthSpace));

            int textMin = sizeRange.Begin - fixedLen;
            int textMax = sizeRange.End - fixedLen;
            return (textMax >= 30 ? new Int32Range(textMin, textMax) : null);
        }
        /// <summary>
        /// Vrátí délku ikony Image: pokud je null, pak 0; jinak vrátí její Width pokud je menší než contentHeight.Size; anebo vrátí contentHeight.Size (jako nejvyšší možnou velikost obrázku)
        /// </summary>
        /// <param name="contentHeight"></param>
        /// <returns></returns>
        private int GetLengthImage(Int32Range contentHeight)
        {
            int length = 0;
            if (this.Image != null)
            {
                length = this.Image.Size.Width;
                if (length > contentHeight.Size) length = contentHeight.Size;
            }
            return length;
        }
        private void PrepareBoundsImage(Graphics graphics, bool headerIsVertical, ref int contentPosition, Int32Range contentHeight)
        {
            this.ImageBounds = Rectangle.Empty;
            if (this.Image != null)
            {
                int imageWidth = this.GetLengthImage(contentHeight);
                Int32Range l = Int32Range.CreateFromBeginSize(contentPosition, imageWidth);        // Prostor ve směru délky headeru (Horizontal: fyzická osa X, Width; Vertical: fyzická osa Y, Height)
                Int32Range h = Int32Range.CreateFromCenterSize(contentHeight.Center, imageWidth);  // Prostor ve směru výšky headeru (Horizontal: fyzická osa Y, Height; Vertical: fyzická osa X, Width)
                this.ImageBounds = (headerIsVertical ? Int32Range.GetRectangle(h, l) : Int32Range.GetRectangle(l, h));
                contentPosition = l.End + LengthSpace;
            }
        }
        private void PrepareBoundsText(Graphics graphics, bool headerIsVertical, ref int contentPosition, Int32Range contentHeight, Int32Range textLengthRange)
        {
            this.TextBounds = Rectangle.Empty;
            if (!String.IsNullOrEmpty(this.Text))
            {
                FontInfo fontInfo = this.GetCurrentFont(true, false);
                string text = this.Text;
                Size textSize = GPainter.MeasureString(graphics, text, fontInfo);
                int width = textSize.Width + 6;
                if (textLengthRange != null)
                    width = textLengthRange.Align(width);
                else if (width > 220)
                    width = 220;
                Int32Range l = Int32Range.CreateFromBeginSize(contentPosition, width);
                Int32Range h = contentHeight;
                this.TextBounds = (headerIsVertical ? Int32Range.GetRectangle(h, l) : Int32Range.GetRectangle(l, h));
                contentPosition = l.End + LengthSpace;
            }
        }
        private void PrepareBoundsClose(Graphics graphics, bool headerIsVertical, ref int contentPosition, Int32Range contentHeight)
        {
            this.CloseButtonBounds = Rectangle.Empty;
            if (this.CloseButtonVisible)
            {
                Int32Range l = Int32Range.CreateFromBeginSize(contentPosition, LengthClose);
                Int32Range h = Int32Range.CreateFromCenterSize(contentHeight.Center, LengthClose);
                this.CloseButtonBounds = (headerIsVertical ? Int32Range.GetRectangle(h, l) : Int32Range.GetRectangle(l, h));
                contentPosition = l.End + LengthSpace;
            }
        }
        private void PrepareBoundsTotal(Graphics graphics, bool headerIsVertical, ref int begin, int contentPosition, int height)
        {
            // Souřadnice tohoto prvku jsou dány orientací TabHeaderu:
            Int32Range l = Int32Range.CreateFromBeginSize(begin, contentPosition);         // Pozice ve směru délky = od počátku, s danou velikostí
            Int32Range h = Int32Range.CreateFromBeginSize(0, height);                      // Pozice ve směru výšky = relativně k Headeru
            this.BoundsSilent = (headerIsVertical ? Int32Range.GetRectangle(h, l) : Int32Range.GetRectangle(l, h));   // Fyzické souřadnice this prvku

            // Začátek příštího prvku = konec this prvku + jeho velikost ve směru jeho délky:
            begin = l.End;
        }
        private const int LengthBorder = 6;
        private const int LengthSpace = 6;
        private const int LengthClose = 24;
        /// <summary>
        /// Pozice, kde se záhlaví nachází vzhledem k datové oblasti, pro kterou má přepínat její obsah
        /// </summary>
        protected RectangleSide Position { get { return this.TabHeader.Position; } }
        /// <summary>
        /// true pokud this header je aktivní v rámci TabHeader
        /// </summary>
        protected bool IsActiveHeader { get { return Object.ReferenceEquals(this, this.TabHeader.ActivePage); } }
        /// <summary>
        /// true pokud this header je MouseActive
        /// </summary>
        protected bool IsHotHeader { get { return this.InteractiveState.IsMouseActive(); } }
        /// <summary>
        /// Vrací Font uvedený v this.TabHeader.Font,
        /// a který odpovídá aktuálnímu stavu (<see cref="IsActiveHeader"/>, <see cref="IsHotHeader"/>).
        /// </summary>
        /// <returns></returns>
        protected FontInfo GetCurrentFont()
        {
            return this.GetCurrentFont(this.IsActiveHeader, this.IsHotHeader);
        }
        /// <summary>
        /// Vrací Font uvedený v this.TabHeader.Font,
        /// a jehož Bold = isActive;
        /// a jehož Underline = (not isActive and isHot)
        /// </summary>
        /// <param name="isActive"></param>
        /// <param name="isHot"></param>
        /// <returns></returns>
        protected FontInfo GetCurrentFont(bool isActive, bool isHot)
        {
            FontInfo fontInfo = ((ITabHeaderInternal)this.TabHeader).CurrentFont;
            fontInfo.Bold = isActive;
            fontInfo.Underline = !isActive && isHot;
            return fontInfo;
        }
        /// <summary>
        /// Souřadnice, do kterých se vykresluje Image.
        /// Souřadnice jsou relativní k this.Bounds
        /// </summary>
        protected Rectangle ImageBounds { get; set; }
        /// <summary>
        /// Souřadnice, do kterých se vykresluje Text.
        /// Souřadnice jsou relativní k this.Bounds
        /// </summary>
        protected Rectangle TextBounds { get; set; }
        /// <summary>
        /// Souřadnice, do kterých se vykresluje CloseButton.
        /// Souřadnice jsou relativní k this.Bounds
        /// </summary>
        protected Rectangle CloseButtonBounds { get; set; }
        #endregion
        #region Draw, Interactivity, ITabHeaderItemPaintData
        /// <summary>
        /// Zajistí standardní vykreslení záhlaví
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)  
        {
            this._DrawLayer = e.DrawLayer;
            GPainter.DrawTabHeaderItem(e.Graphics, absoluteBounds, this);
        }
        /// <summary>
        /// Zajistí vyvolání háčku <see cref="OnTabItemPaintBackGround(object, GUserDrawArgs)"/> a eventu <see cref="TabPagePaintBackGround"/>.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        protected void CallUserDataDraw(Graphics graphics, Rectangle bounds)
        {
            GUserDrawArgs e = new GUserDrawArgs(graphics, this._DrawLayer, bounds);
            this.OnTabItemPaintBackGround(this, e);
            if (this.TabPagePaintBackGround != null)
                this.TabPagePaintBackGround(this, e);
        }
        private GInteractiveDrawLayer _DrawLayer;
        /// <summary>
        /// Háček pro uživatelské kreslení pozadí záhlaví
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnTabItemPaintBackGround(object sender, GUserDrawArgs e) { }
        /// <summary>
        /// Event pro uživatelské kreslení pozadí záhlaví
        /// </summary>
        public event GUserDrawHandler TabPagePaintBackGround;
        /// <summary>
        /// Po kliknutí levou myší na záhlaví: provede aktivaci záhlaví
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);
            this.TabHeader.ActivePage = this;
        }
        /// <summary>
        /// Připraví Tooltip pro toto záhlaví
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            Localizable.TextLoc toolTip = this.ToolTipText;
            if (toolTip != null)
            {
                e.ToolTipData.TitleText = this.Text;
                e.ToolTipData.InfoText = toolTip.Text;
            }
        }
        #region Implementace ITabHeaderItemPaintData : pro vykreslení záhlaví pomocí Painteru
        RectangleSide ITabHeaderItemPaintData.Position { get { return this.Position; } }
        bool ITabHeaderItemPaintData.IsEnabled { get { return this.Is.Enabled; } }
        Color? ITabHeaderItemPaintData.BackColor { get { return this.BackColor; } }
        bool ITabHeaderItemPaintData.IsActive { get { return this.IsActiveHeader; } }
        FontInfo ITabHeaderItemPaintData.Font { get { return this.GetCurrentFont(); } }
        GInteractiveState ITabHeaderItemPaintData.InteractiveState { get { return this.InteractiveState; } }
        Image ITabHeaderItemPaintData.Image { get { return this.Image; } }
        Rectangle ITabHeaderItemPaintData.ImageBounds { get { return this.ImageBounds; } }
        string ITabHeaderItemPaintData.Text { get { return this.Text; } }
        Rectangle ITabHeaderItemPaintData.TextBounds { get { return this.TextBounds; } }
        bool ITabHeaderItemPaintData.CloseButtonVisible { get { return this.CloseButtonVisible; } }
        Rectangle ITabHeaderItemPaintData.CloseButtonBounds { get { return this.CloseButtonBounds; } }
        void ITabHeaderItemPaintData.UserDataDraw(Graphics graphics, Rectangle bounds) { this.CallUserDataDraw(graphics, bounds); }
        #endregion
        #endregion
    }
    #endregion
    #region enum ShowTabHeaderMode : Režim zobrazování záložek TabHeader při zobrazování pole prvků
    /// <summary>
    /// Režim zobrazování záložek <see cref="GTabHeader"/> při zobrazování pole prvků: zda bude záhlaví viditelné,
    /// zda bude v záhlaví obsažen prvek Collapse, zda bude záhlaví viditelné i pro jediný prvek.
    /// </summary>
    [Flags]
    public enum ShowTabHeaderMode
    {
        /// <summary>
        /// Zobrazovat <see cref="GTabHeader"/> jen tehdy, když pole prvků obsahuje dvě nebo více položek (pak má výběr zobrazené položky logický význam)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Zobrazovat TabHeader vždy, tedy i když pole prvků obsahuje jen jeden prvek (pak sice výběr zobrazené položky nemá logický význam, 
        /// ale <see cref="GTabHeader"/> pak hraje roli titulku = zobrazuje totiž Image a Text)
        /// </summary>
        Always = 1,
        /// <summary>
        /// Nikdy nezobrazovat <see cref="GTabHeader"/>, ani když je v poli přítomno více položek (přepínání položek se řeší kódem)
        /// </summary>
        NoTabHeader = 2,
        /// <summary>
        /// Přidat jedno záhlaví <see cref="GTabPage"/> pro funkci Collapse, která po kliknutí zmenší prostor <see cref="GTabContainer"/> jen na lištu záhlaví <see cref="GTabHeader"/>.
        /// </summary>
        CollapseItem = 0x10
    }
    #endregion
}
