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
    /// <summary>
    /// TabHeader : záhlaví bloku stránek
    /// </summary>
    public class TabHeader : InteractiveContainer
    {
        #region Konstruktor a public data celého headeru
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public TabHeader(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TabHeader()
        {
            this._ItemList = new List<TabItem>();
            this._Position = RectangleSide.Top;
            this._HeaderSizeRange = new Int32Range(50, 600);
            this.__ActiveHeaderIndex = -1;
        }
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
        /// Počet položek celkem přítomných v poli <see cref="TabItems"/>
        /// </summary>
        public int HeaderItemCount { get { return this._ItemList.Count; } }
        /// <summary>
        /// Pole všech položek, v jejich nativním pořadí (=tak jak se přidávaly)
        /// </summary>
        public TabItem[] TabItems { get { return this._ItemList.ToArray(); } }
        /// <summary>
        /// Index aktuálně aktivního záhlaví. Pokud je -1, pak není korektně zadán.
        /// Index se odkazuje na pole <see cref="TabItems"/>, které obsahuje všechny záhlaví, nejen ty které jsou viditelné;
        /// a je v nativním pořadí (jak byly prvky přidávány, ne jak jsou setříděny).
        /// Setování indexu lze provést pouze na hodnotu, která odkazuje na existující a viditelný prvek.
        /// Nelze tedy vložit ActiveHeaderIndex = 0 pokud prvek TabItems[0] má IsVisible = false (takové setování indexu bude ignorováno).
        /// Setování vyvolá eventy ActiveIndexChanged a ActiveItemChanged
        /// </summary>
        public int ActiveHeaderIndex
        {
            get
            {
                int index = this._ActiveHeaderIndex;
                int count = this.HeaderItemCount;
                return (index >= 0 && index < count ? index : -1);
            }
            protected set
            {
                int index = value;
                int count = this.HeaderItemCount;
                if (index >= 0 && index < count && this._ItemList[index].IsVisible)
                    this._ActiveHeaderIndex = index;
            }
        }
        /// <summary>
        /// Data aktuálně aktivního záhlaví. Může být null.
        /// Setování vyvolá eventy ActiveIndexChanged a ActiveItemChanged
        /// </summary>
        public TabItem ActiveHeaderItem
        {
            get
            {
                int count = this.HeaderItemCount;
                int index = this.ActiveHeaderIndex;
                return ((index >= 0 && index < count) ? this._ItemList[index] : null);
            }
            set
            {
                TabItem item = value;
                int index = (item != null ? this._ItemList.FindIndex(i => i.IsVisible && Object.ReferenceEquals(i, item)) : -1);
                if (index >= 0)
                {
                    this._ActiveHeaderIndex = index;
                    this.Repaint();
                }
            }
        }
        /// <summary>
        /// Index aktuálního záhlaví. Vrací __ActiveHeaderIndex (bez kontrol). 
        /// Setování provede kontrolu hodnoty a po změně vyvolá ActiveItemChanged, a nastavení viditelnosti u <see cref="TabItem.Control"/>.
        /// </summary>
        private int _ActiveHeaderIndex
        {
            get { return this.__ActiveHeaderIndex; }
            set
            {
                int count = this.HeaderItemCount;
                int oldIndex = this.__ActiveHeaderIndex;
                TabItem oldItem = this.ActiveHeaderItem;

                int newIndex = value;
                if (newIndex >= 0 && newIndex < count && newIndex != oldIndex)
                {
                    this.__ActiveHeaderIndex = newIndex;
                    this._ShowLinkedItems(this._ItemList[newIndex]);
                    TabItem newItem = this.ActiveHeaderItem;
                    this.CallActiveIndexChanged(oldIndex, newIndex, EventSourceType.InteractiveChanged);
                    this.CallActiveItemChanged(oldItem, newItem, EventSourceType.InteractiveChanged);
                }
            }
        }
        private int __ActiveHeaderIndex;
        /// <summary>
        /// Metoda zajistí nastavení IsVisible pro všechny <see cref="TabItem.Control"/> v <see cref="_ItemList"/>.
        /// Pouze záložka <see cref="ActiveHeaderItem"/> bude mít LinkItem.IsVisible = true, ostatní budou mít false.
        /// </summary>
        private void _ShowLinkedItems()
        {
            this._ShowLinkedItems(this.ActiveHeaderItem);
        }
        /// <summary>
        /// Metoda zajistí nastavení IsVisible pro všechny <see cref="TabItem.Control"/> v <see cref="_ItemList"/>.
        /// Pouze záložka odpovídající zadanému záhlaví bude mít LinkItem.IsVisible = true, ostatní budou mít false.
        /// </summary>
        /// <param name="activeItem"></param>
        private void _ShowLinkedItems(TabItem activeItem)
        {
            foreach (TabItem tabItem in this._ItemList)
            {
                IInteractiveItem linkItem = tabItem.Control;
                if (linkItem != null)
                {
                    bool isVisible = (activeItem != null && Object.ReferenceEquals(tabItem, activeItem));
                    if (linkItem.IsVisible != isVisible)
                    {
                        linkItem.IsVisible = isVisible;
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
        /// Pokud je null (což je default), pak se použije <see cref="FontInfo.Caption"/>.
        /// </summary>
        public FontInfo Font { get { return this._Font; } set { this._Font = value; this.InvalidateChildItems(); } } private FontInfo _Font;
        /// <summary>
        /// Aktuální font, nikdy není null.
        /// </summary>
        protected FontInfo CurrentFont { get { return (this._Font != null ? this._Font : FontInfo.CaptionBoldBig); } }
        #endregion
        #region Add header, Remove header, Get header

        public TabItem AddHeader(Localizable.TextLoc text, Image image = null, int tabOrder = 0, IInteractiveItem linkItem = null)
        {
            return this._AddHeader(null, text, image, tabOrder, linkItem);
        }
        public TabItem AddHeader(string key, Localizable.TextLoc text, Image image = null, int tabOrder = 0, IInteractiveItem linkItem = null)
        {
            return this._AddHeader(key, text, image, tabOrder, linkItem);
        }
        private TabItem _AddHeader(string key, Localizable.TextLoc text, Image image, int tabOrder, IInteractiveItem linkItem)
        {
            TabItem tabHeaderItem = new TabItem(this, key, text, image, linkItem, tabOrder);
            this._ItemList.Add(tabHeaderItem);
            tabHeaderItem.TabItemPaintBackGround += _TabHeaderItemPaintBackGround;
            if (this._ActiveHeaderIndex < 0)
                this._ActiveHeaderIndex = 0;
            this._ShowLinkedItems();
            this.InvalidateChildItems();
            return tabHeaderItem;
        }
        #endregion
        #region Eventy
        /// <summary>
        /// Eventhandler pro událost na konkrétním záhlaví (TabItem), kde se provádí uživatelské vykreslení pozadí
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabHeaderItemPaintBackGround(object sender, PaintEventArgs e)
        {
            this.OnTabItemPaintBackGround(sender, e);
            if (this.TabItemPaintBackGround != null)
                this.TabItemPaintBackGround(sender, e);
        }
        /// <summary>
        /// Háček pro uživatelské kreslení pozadí záhlaví
        /// </summary>
        /// <param name="sender">Objekt kde k události došlo = <see cref="TabHeader.TabItem"/></param>
        /// <param name="e">Data pro kreslení</param>
        protected virtual void OnTabItemPaintBackGround(object sender, PaintEventArgs e) { }
        /// <summary>
        /// Event pro uživatelské kreslení pozadí záhlaví.
        /// Jako parametr sender je předán objekt konkrétního záhlaví <see cref="TabHeader.TabItem"/>.
        /// </summary>
        public event PaintEventHandler TabItemPaintBackGround;

        /// <summary>
        /// Vyvolá háček OnActiveIndexChanged a event ActiveIndexChanged
        /// </summary>
        protected int CallActiveIndexChanged(int oldValue, int newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<int> args = new GPropertyChangeArgs<int>(eventSource, oldValue, newValue);
            this.OnActiveIndexChanged(args);
            if (!this.IsSuppressedEvent && this.ActiveIndexChanged != null)
                this.ActiveIndexChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Provede se po změně indexu aktivní záložky (<see cref="ActiveHeaderItem"/>)
        /// </summary>
        protected virtual void OnActiveIndexChanged(GPropertyChangeArgs<int> args) { }
        /// <summary>
        /// Event volaný po změně indexu aktivní záložky (<see cref="ActiveHeaderItem"/>)
        /// </summary>
        public event GPropertyChanged<int> ActiveIndexChanged;

        /// <summary>
        /// Vyvolá háček OnActiveItemChanged a event ActiveItemChanged
        /// </summary>
        protected TabItem CallActiveItemChanged(TabItem oldValue, TabItem newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TabItem> args = new GPropertyChangeArgs<TabItem>(eventSource, oldValue, newValue);
            this.OnActiveItemChanged(args);
            if (!this.IsSuppressedEvent && this.ActiveItemChanged != null)
                this.ActiveItemChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Provede se po změně aktivní záložky (<see cref="ActiveHeaderItem"/>)
        /// </summary>
        protected virtual void OnActiveItemChanged(GPropertyChangeArgs<TabItem> args) { }
        /// <summary>
        /// Event volaný po změně aktivní záložky (<see cref="ActiveHeaderItem"/>)
        /// </summary>
        public event GPropertyChanged<TabItem> ActiveItemChanged;
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
        protected TabItem[] GetChilds(Graphics graphics)
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
            List<TabItem> sortedList = new List<TabItem>(this._ItemList.Where(i => i.IsVisible));
            int count = sortedList.Count;
            if (count > 1) sortedList.Sort(TabItem.CompareByOrder);

            bool hasGraphics = (graphics != null);
            if (!hasGraphics)
                graphics = Graphics.FromImage(new Bitmap(256, 128));

            // Počáteční pixel = mínus offset, ale nesmí být kladný:
            int begin = -this._HeaderOffsetPixel;
            if (begin > 0) begin = 0;

            // Určit velikost záhlaví a jejich souřadnice:
            foreach (TabItem item in sortedList)
                item.PrepareBounds(graphics, ref begin);

            if (!hasGraphics)
                graphics.Dispose();

            this._HeaderContentLength = begin;
            this._SortedItems = sortedList.ToArray();

            // Vytvořit pořadí záhlaví pro zobrazování tak, aby aktuální záhlaví bylo navrchu (tj. poslední v seznamu):
            List<TabItem> childList = new List<TabItem>();
            TabItem activeItem = this.ActiveHeaderItem;

            // Nejprve přidám prvky vlevo od aktivního prvku, počínaje prvním, s tím že aktivní prvek v tomto chodu již nepřidáme:
            int activeIndex = -1;
            for (int i = 0; i < count; i++)
            {
                TabItem item = sortedList[i];
                if (activeItem != null && Object.ReferenceEquals(item, activeItem))
                {
                    activeIndex = i;
                    break;
                }
                childList.Add(item);
            }

            // Poté přidám prvky vpravo od aktivního prvku, počínaje posledním, a konče právě tím aktivním = tak se dostane na poslední místo v childList:
            if (activeIndex >= 0)
            {
                for (int i = count - 1; i >= activeIndex; i--)
                {
                    TabItem item = sortedList[i];
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
        protected int HeaderHeight { get { return (this.HeaderIsHorizontal ? this.Bounds.Height : (this.HeaderIsVertical ? this.Bounds.Width : 0)); } }
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
        private List<TabItem> _ItemList;
        /// <summary>
        /// Pole obsahující viditelné prvky TabItem v tom pořadí, v jakém jdou za sebou od začátku do konce (podle jejich <see cref="TabItem.TabOrder"/>)
        /// </summary>
        private TabItem[] _SortedItems;
        /// <summary>
        /// Pole obsahující viditelné prvky TabItem v tom pořadí, jak mají být vykresleny (poslední v poli je aktivní záložka = na vrchu)
        /// </summary>
        private TabItem[] _ChildItems;
        #endregion
        #region Draw, Interactivity
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Color spaceColor = this.BackColor;                  // tam je default: Skin.TabHeader.SpaceColor;
            if (spaceColor.A > 0)
                e.Graphics.FillRectangle(Skin.Brush(spaceColor), boundsAbsolute);

            this.DrawHeaderLine(e, boundsAbsolute);
        }
        /// <summary>
        /// Vykreslí linku "pod" všemi položkami záhlaví, odděluje záhlaví a data.
        /// Aktivní záhlaví pak tuto linku zruší svým pozadím.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void DrawHeaderLine(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Rectangle line = Rectangle.Empty;
            switch (this.Position)
            {
                case RectangleSide.Top:
                    line = new Rectangle(boundsAbsolute.X, boundsAbsolute.Bottom - 2, boundsAbsolute.Width, 2);
                    break;
                case RectangleSide.Right:
                    line = new Rectangle(boundsAbsolute.X, boundsAbsolute.Y, 2, boundsAbsolute.Height);
                    break;
                case RectangleSide.Bottom:
                    line = new Rectangle(boundsAbsolute.X, boundsAbsolute.Y, boundsAbsolute.Width, 2);
                    break;
                case RectangleSide.Left:
                    line = new Rectangle(boundsAbsolute.Right - 2, boundsAbsolute.Y, 2, boundsAbsolute.Height);
                    break;
            }
            if (!line.HasPixels()) return;

            TabItem activeItem = this.ActiveHeaderItem;
            Color color = ((activeItem != null && activeItem.BackColor.HasValue) ? activeItem.BackColor.Value : Skin.TabHeader.BackColorActive);

            e.Graphics.FillRectangle(Skin.Brush(color), line);
        }
        /// <summary>
        /// Protože: this prvek může mít transparentní svoje pozadí, a navíc podporuje proměnný obsah popředí, musíme zajistit přemalování Parenta před přemalováním this.
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.OnBackColorAlpha; } }
        /// <summary>
        /// Aby fungovalo přemalování parenta v režimu <see cref="RepaintParentMode.OnBackColorAlpha"/>, musíme vracet korektní barvu BackColor.
        /// Výchozí je <see cref="Skin.TabHeader.SpaceColor"/>.
        /// </summary>
        public override Color BackColor { get { return (this._BackColor.HasValue ? this._BackColor.Value : Skin.TabHeader.SpaceColor); } set { this._BackColor = value; base.BackColor = value; } }
        private new Color? _BackColor;
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetChilds(null); } }
        #endregion
        #region class TabItem : jedna položka reprezentující záhlaví stránky
        /// <summary>
        /// Třída reprezentující jedno záhlaví v objektu <see cref="TabHeader"/>.
        /// </summary>
        public class TabItem : InteractiveContainer, ITabHeaderItemPaintData
        {
            #region Konstruktor a public data jednoho záhlaví
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="tabHeader"></param>
            /// <param name="text"></param>
            /// <param name="image"></param>
            /// <param name="linkItem"></param>
            /// <param name="tabOrder"></param>
            internal TabItem(TabHeader tabHeader, string key, Localizable.TextLoc text, Image image, IInteractiveItem linkItem, int tabOrder)
            {
                this.Parent = tabHeader;
                this.TabHeader = tabHeader;
                this._Key = key;
                this._Text = text;
                this._Image = image;
                this._TabOrder = tabOrder;
                this._LinkItem = linkItem;
                this.TabHeaderInvalidate();
            }
            /// <summary>
            /// Zajistí invalidaci obsahu v <see cref="TabHeader"/>
            /// </summary>
            protected void TabHeaderInvalidate() { this.TabHeader.InvalidateChildItems(); }
            /// <summary>
            /// Reference na vlastníka této záložky
            /// </summary>
            protected TabHeader TabHeader { get; private set; }
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
            /// Pořadí. Implicitní pořadí = 0. 
            /// Pokud bude mít více prvků shodné pořadí, budou setříděny podle pořadí přidání (<see cref="InteractiveObject.Id"/>)
            /// </summary>
            public int TabOrder { get { return this._TabOrder; } set { this._TabOrder = value; this.TabHeaderInvalidate(); } }
            private int _TabOrder;
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
            public IInteractiveItem Control { get { return this._LinkItem; } set { this._LinkItem = value; } }
            private IInteractiveItem _LinkItem;
            /// <summary>
            /// Viditelnost záhlaví
            /// </summary>
            public override bool IsVisible { get { return base.IsVisible; } set { base.IsVisible = value; this.TabHeaderInvalidate(); } }
            /// <summary>
            /// Obsahuje true, pokud this záhlaví je aktivní
            /// </summary>
            public bool IsActive { get { return (this.TabHeader != null && Object.ReferenceEquals(this, this.TabHeader.ActiveHeaderItem)); } }
            /// <summary>
            /// Jakákoli uživatelská data
            /// </summary>
            public object UserData { get { return this._UserData; } set { this._UserData = value; } }
            private object _UserData;
            #endregion
            #region Souřadnice záhlaví a jeho vnitřních položek, jeho výpočty, komparátor podle TabOrder
            /// <summary>
            /// Metoda připraví souřadnice tohoto prvku (<see cref="TabItem"/>) a jeho vnitřní souřadnice (ikona, text, close button).
            /// Vychází z pixelu begin, kde má zíhlaví začínat, z orientace headeru a jeho fontu, a z vnitřních dat záhlaví.
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="begin"></param>
            internal void PrepareBounds(Graphics graphics, ref int begin)
            {
                int height = this.TabHeader.HeaderHeight;
                bool headerIsVertical = this.TabHeader.HeaderIsVertical;
                bool headerIsReverseOrder = (this.TabHeader.Position == RectangleSide.Left);
                Int32Range contentHeight = Int32Range.CreateFromCenterSize(height / 2, height - 6);     // Prostor pro vnitřní obsah ve směru výšky headeru
                Int32Range textLengthRange = GetLengthRangeText(contentHeight);

                int contentPosition = LengthBorder;    // Začátek následujícího prvku ve směru délky headeru
                if (!headerIsReverseOrder)
                {
                    PrepareBoundsImage(graphics, headerIsVertical, ref contentPosition, contentHeight);                // Ikonka
                    PrepareBoundsText(graphics, headerIsVertical, ref contentPosition, contentHeight, textLengthRange);// Text
                    PrepareBoundsClose(graphics, headerIsVertical, ref contentPosition, contentHeight);                // Close button
                }
                else
                {
                    PrepareBoundsClose(graphics, headerIsVertical, ref contentPosition, contentHeight);                // Close button
                    PrepareBoundsText(graphics, headerIsVertical, ref contentPosition, contentHeight, textLengthRange);// Text
                    PrepareBoundsImage(graphics, headerIsVertical, ref contentPosition, contentHeight);                // Ikonka
                }
                contentPosition += (LengthBorder - LengthSpace);

                PrepareBoundsTotal(graphics, headerIsVertical, ref begin, contentPosition, height);     // Celkové rozměry
            }
            /// <summary>
            /// Metoda vypočte a vrátí rozsah délky (v pixelech) pro pole Text.
            /// Vychází přitom z povoleného rozsahu <see cref="TabHeader.HeaderSizeRange"/>, a z velikosti zdejší ikony a close buttonu.
            /// Pokud není povolený rozsah <see cref="TabHeader.HeaderSizeRange"/> zadán (je null) nebo pokud by výsledná velikost textu Max byla menší než 30 px, vrací se null.
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
            private void PrepareBoundsImage(Graphics graphics, bool headerIsVertical,  ref int contentPosition, Int32Range contentHeight)
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
            protected bool IsActiveHeader { get { return Object.ReferenceEquals(this, this.TabHeader.ActiveHeaderItem); } }
            /// <summary>
            /// true pokud this header je MouseActive
            /// </summary>
            protected bool IsHotHeader { get { return this.CurrentState.IsMouseActive(); } }
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
                FontInfo fontInfo = this.TabHeader.CurrentFont.Clone;
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
            /// <summary>
            /// Vrátí výsledek porovnání TabOrder ASC, Id ASC
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            internal static int CompareByOrder(TabItem a, TabItem b)
            {
                int cmp = a.TabOrder.CompareTo(b.TabOrder);
                if (cmp == 0)
                    cmp = a.Id.CompareTo(b.Id);
                return cmp;
            }
            #endregion
            #region Draw, Interactivity, ITabHeaderItemPaintData
            /// <summary>
            /// Zajistí standardní vykreslení záhlaví
            /// </summary>
            /// <param name="e"></param>
            /// <param name="boundsAbsolute"></param>
            protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
            {
                GPainter.DrawTabHeaderItem(e.Graphics, boundsAbsolute, this);
            }
            /// <summary>
            /// Zajistí vyvolání háčku <see cref="OnTabHeaderPaintBackGround(Graphics, Rectangle)"/> a eventu <see cref="TabItemPaintBackGround"/>.
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="bounds"></param>
            protected void CallUserDataDraw(Graphics graphics, Rectangle bounds)
            {
                PaintEventArgs e = new PaintEventArgs(graphics, bounds);
                this.OnTabItemPaintBackGround(this, e);
                if (this.TabItemPaintBackGround != null)
                    this.TabItemPaintBackGround(this, e);
            }
            /// <summary>
            /// Háček pro uživatelské kreslení pozadí záhlaví
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="bounds"></param>
            protected virtual void OnTabItemPaintBackGround(object sender, PaintEventArgs e) { }
            /// <summary>
            /// Event pro uživatelské kreslení pozadí záhlaví
            /// </summary>
            public event PaintEventHandler TabItemPaintBackGround;

            /// <summary>
            /// Po kliknutí levou myší na záhlaví: provede aktivaci záhlaví
            /// </summary>
            /// <param name="e"></param>
            protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
            {
                base.AfterStateChangedLeftClick(e);
                this.TabHeader.ActiveHeaderItem = this;
            }
            /// <summary>
            /// Připraví Tooltip pro toto záhlaví
            /// </summary>
            /// <param name="e"></param>
            protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
            {
                Localizable.TextLoc toolTip = this.ToolTipText;
                if (toolTip != null)
                {
                    e.ToolTipData.TitleText = this.Text;
                    e.ToolTipData.InfoText = toolTip.Text;
                }
            }
            #region Implementace ITabHeaderItemPaintData
            RectangleSide ITabHeaderItemPaintData.Position { get { return this.Position; } }
            bool ITabHeaderItemPaintData.IsEnabled { get { return this.IsEnabled; } }
            Color? ITabHeaderItemPaintData.BackColor { get { return this.BackColor; } }
            bool ITabHeaderItemPaintData.IsActive { get { return this.IsActiveHeader; } }
            FontInfo ITabHeaderItemPaintData.Font { get { return this.GetCurrentFont(); } }
            GInteractiveState ITabHeaderItemPaintData.InteractiveState { get { return this.CurrentState; } }
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
    }
    /// <summary>
    /// Ucelený blok, obsahující záhlaví jednotlivých "stránek" a k nim příslušné jednotlivé stránky
    /// </summary>
    public class TabContainer : InteractiveContainer
    {
        #region Konstrukce, proměnné
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public TabContainer(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TabContainer()
        {
            this._TabHeaderMode = ShowTabHeaderMode.Default;
            this._IsCollapsed = false;

            this._TabHeader = new TabHeader(this) { Position = RectangleSide.Top };
            this._TabItemCollapse = this._TabHeader.AddHeader(TabItemKeyCollapse, "", Components.IconStandard.GoTop, tabOrder: 99999);
            this._TabItemCollapse.IsVisible = this._TabItemCollapseIsAvailable;
            this._TabHeader.ActiveItemChanged += _TabHeader_ActiveItemChanged;

            this._Controls = new List<IInteractiveItem>();
        }
        /// <summary>
        /// eventhandler po změně _TabHeader.ActiveItemChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabHeader_ActiveItemChanged(object sender, GPropertyChangeArgs<TabHeader.TabItem> e)
        {
            // Nejprve řeším vnitřní záležitosti, bez eventů:
            this._TabHeaderItemChanged(e);
            // Poté volám eventhandlery:
            e.CorrectValue = this.CallActivePageChanged(e.OldValue, e.NewValue, e.EventSource);
        }
        /// <summary>
        /// Provede se po změně aktivního záhlaví, řeší pouze změnu stavu IsCollapsed.
        /// </summary>
        /// <param name="e"></param>
        private void _TabHeaderItemChanged(GPropertyChangeArgs<TabHeader.TabItem> e)
        {
            bool isCollapseNew = (e.NewValue != null && e.NewValue.Key == TabItemKeyCollapse);
            if (!IsCollapsed)
                this._TabItemLastActive = e.NewValue;

            // Řeším změnu Collapsed:
            bool isCollapseOld = this._IsCollapsed;
            if (isCollapseOld == isCollapseNew) return;
            this.IsCollapsed = isCollapseNew;              // Set property řeší volání eventů, .
        }
        /// <summary>
        /// Sada záložek
        /// </summary>
        private TabHeader _TabHeader;
        /// <summary>
        /// Záložka reprezentující "Collapse" položku
        /// </summary>
        private TabHeader.TabItem _TabItemCollapse;
        /// <summary>
        /// true pokud má být zobrazen TabItem pro Collapse
        /// </summary>
        private bool _TabItemCollapseIsAvailable { get { return this._TabHeaderMode.HasFlag(ShowTabHeaderMode.CollapseItem); } }
        /// <summary>
        /// Záložka (TabItem), která byla naposledy aktivní před provedením Collapse.
        /// Pokud bude stav Collapse zrušen prostým nastavením IsCollapsed = false, pak algoritmus provede aktivaci této záložky.
        /// </summary>
        private TabHeader.TabItem _TabItemLastActive;
        private List<IInteractiveItem> _Controls;
        private ShowTabHeaderMode _TabHeaderMode;
        private bool _IsCollapsed;
        #endregion
        #region Přidání a odebrání položek
        /// <summary>
        /// Přidá (a vrátí) novou záložku pro daný prvek
        /// </summary>
        /// <returns></returns>
        public TabHeader.TabItem AddTabItem(IInteractiveItem item, Localizable.TextLoc text, Localizable.TextLoc toolTip = null, Image image = null)
        {
            if (item == null) return null;
            item.Parent = this;
            bool isActive = (this._TabHeader.HeaderItemCount <= 1);
            TabHeader.TabItem tabItem = this._TabHeader.AddHeader(TabItemKeyControl, text, image, linkItem: item);
            if (toolTip != null)
                tabItem.ToolTipText = toolTip;
            if (isActive)
                this._TabHeader.ActiveHeaderItem = tabItem;
            return tabItem;
        }
        /// <summary>
        /// Key název prvku TabItem, který obsahuje uživatelský Control
        /// </summary>
        private const string TabItemKeyControl = "ControlItem";
        /// <summary>
        /// Key název prvku TabItem, který provádí Collapse
        /// </summary>
        private const string TabItemKeyCollapse = "CollapseItem";
        #endregion
        #region Layout
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);

        }
        /// <summary>
        /// Metoda upraví vlastní layout podle parametrů.
        /// Tzn. Umístí TabHeader, a v případě Collapse nastaví this.Bounds podle potřeby.
        /// </summary>
        protected void PrepareLayout()
        { }
        #endregion
        #region Public property a metody
        /// <summary>
        /// Pole všech položek, v jejich nativním pořadí (=tak jak se přidávaly).
        /// </summary>
        public TabHeader.TabItem[] TabItems { get { return this._TabHeader.TabItems.Where(t => (t.Key != TabItemKeyCollapse)).ToArray(); } }

        public ShowTabHeaderMode TabHeaderMode
        {
            get { return this._TabHeaderMode; }
            set
            {
                ShowTabHeaderMode oldValue = this._TabHeaderMode;
                ShowTabHeaderMode newValue = value;
                if (newValue == oldValue) return;
                this.PrepareLayout();
                this._TabHeaderMode = newValue;
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
                TabHeader.TabItem activePage = this.ActivePage;
                return (activePage != null ? activePage.Control : null);
            }
            set
            {
                IInteractiveItem oldValue = this.ActiveControl;
                IInteractiveItem newValue = value;
                if (newValue != null && !Object.ReferenceEquals(newValue, oldValue))
                {   // Pouze pokud je nový objekt zadán, a je odlišný od aktuálního:
                    TabHeader.TabItem tabItem = (newValue != null ? this._TabHeader.TabItems.FirstOrDefault(t => Object.ReferenceEquals(t, newValue)) : null);
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
        public TabHeader.TabItem ActivePage
        {
            get { return this._TabHeader.ActiveHeaderItem; }
            set { this._TabHeader.ActiveHeaderItem = value; /* Vyvolá event TabHeader_ActiveItemChanged => this._TabHeader_ActiveItemChanged  */ }
        }
        /// <summary>
        /// Obsahuje true, pokud this je ve stavu Collapsed, tzn. je zobrazen jen pás se záhlavím (<see cref="TabHeader"/>),
        /// ale není zobrazen navázaný Control.
        /// </summary>
        public bool IsCollapsed
        {
            get { return this._IsCollapsed; }
            set
            {
                bool oldValue = this._IsCollapsed;
                bool newValue = value;
                if (newValue != oldValue)
                {   // Setování této property se provádí zvenku, a jejím úkolem je aktivovat záložku Collapse nebo LastActive:
                    if (newValue)
                    {   // true = stav je Collapsed => měl bych aktivovat TabItem pro Collapse:
                        TabHeader.TabItem tabItem = this._TabItemCollapse;
                    }
                    else
                    {   // false = stav je Expanded => měl bych aktivovat TabItem posledně aktivní:

                    }


                    this._IsCollapsed = value;
                    this.CallIsCollapsedChanged(oldValue, newValue, EventSourceType.ApplicationCode);
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this je ve stavu Collapsed, tzn. je zobrazen jen pás se záhlavím (<see cref="TabHeader"/>),
        /// ale není zobrazen navázaný Control.
        /// Setování této property neřeší přepínání TabItem, ale vyvolá event IsCollapsedChanged.
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
                    this.PrepareLayout();
                    this.CallIsCollapsedChanged(oldValue, newValue, EventSourceType.ApplicationCode);
                }
            }
        }
        #endregion
        #region Eventy

        /// <summary>
        /// Zavolá metody <see cref="OnActivePageChanged"/> a eventhandler <see cref="ActivePageChanged"/>.
        /// </summary>
        protected TabHeader.TabItem CallActivePageChanged(TabHeader.TabItem oldValue, TabHeader.TabItem newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<TabHeader.TabItem> args = new GPropertyChangeArgs<TabHeader.TabItem>(eventSource, oldValue, newValue);
            this.OnActivePageChanged(args);
            if (!this.IsSuppressedEvent && this.ActivePageChanged != null)
                this.ActivePageChanged(this, args);
            return args.ResultValue;
        }
        /// <summary>
        /// Metoda prováděná při změně hodnoty <see cref="IsCollapsed"/>
        /// </summary>
        protected virtual void OnActivePageChanged(GPropertyChangeArgs<TabHeader.TabItem> args) { }
        /// <summary>
        /// Event provedený po změně hodnoty <see cref="ActivePage"/>
        /// </summary>
        public event GPropertyChanged<TabHeader.TabItem> ActivePageChanged;

        /// <summary>
        /// Zavolá metody <see cref="OnActiveControlChanged"/> a eventhandler <see cref="ActiveControlChanged"/>.
        /// </summary>
        protected IInteractiveItem CallActiveControlChanged(IInteractiveItem oldValue, IInteractiveItem newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<IInteractiveItem> args = new GPropertyChangeArgs<IInteractiveItem>(eventSource, oldValue, newValue);
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
        public event GPropertyChanged<IInteractiveItem> ActiveControlChanged;

        /// <summary>
        /// Zavolá metody <see cref="OnIsCollapsedChanged"/> a eventhandler <see cref="IsCollapsedChanged"/>.
        /// </summary>
        protected bool CallIsCollapsedChanged(bool oldValue, bool newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<bool> args = new GPropertyChangeArgs<bool>(eventSource, oldValue, newValue);
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
        public event GPropertyChanged<bool> IsCollapsedChanged;
        #endregion
    }
    #region enum ShowTabHeaderMode : Režim zobrazování záložek TabHeader při zobrazování pole prvků
    /// <summary>
    /// Režim zobrazování záložek <see cref="TabHeader"/> při zobrazování pole prvků
    /// </summary>
    [Flags]
    public enum ShowTabHeaderMode
    {
        /// <summary>
        /// Zobrazovat <see cref="TabHeader"/> jen tehdy, když pole prvků obsahuje dvě nebo více položek (pak má výběr zobrazené položky logický význam)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Zobrazovat TabHeader vždy, tedy i když pole prvků obsahuje jen jeden prvek (pak výběr zobrazené položky nemá logický význam, 
        /// ale <see cref="TabHeader"/> pak hraje roli titulku = zobrazuje totiž Image a Text)
        /// </summary>
        Always = 1,
        /// <summary>
        /// Nikdy nezobrazovat <see cref="TabHeader"/>, ani když je v poli přítomno více položek (přepínání položek se řeší kódem)
        /// </summary>
        NoTabHeader = 2,
        /// <summary>
        /// Přidat <see cref="TabHeader.TabItem"/> pro funkci Collapse, která zmenší prostor <see cref="TabContainer"/> jen na lištu záhlaví <see cref="TabHeader"/>.
        /// </summary>
        CollapseItem = 0x10
    }
    #endregion
}
