// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors.ViewInfo;

using XS = Noris.WS.Parser.XmlSerializer;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxAutoScrollPanelControl
    /// <summary>
    /// DxAutoScrollPanelControl : Panel s podporou AutoScroll a s podporou události při změně VisibleBounds
    /// </summary>
    public class DxAutoScrollPanelControl : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxAutoScrollPanelControl()
        {
            this.AutoScroll = true;
            this.SetAutoScrollMargin(40, 6);
            this.Padding = new Padding(10);
            this.SetStyle(ControlStyles.UserPaint, true);
        }
        #region VisibleBounds
        /// <summary>
        /// Souřadnice Child prvků, které jsou nyní vidět (=logické koordináty).
        /// Pokud tedy mám Child prvek s Bounds = { 0, 0, 600, 2000 } 
        /// a this container má velikost { 500, 300 } a je odscrollovaný trochu dolů (o 100 pixelů),
        /// pak VisibleBounds obsahuje právě to "viditelné okno" v Child controlu = { 100, 0, 500, 300 }
        /// </summary>
        public Rectangle VisibleBounds { get { return __CurrentVisibleBounds; } }
        /// <summary>
        /// Je provedeno po změně <see cref="DxAutoScrollPanelControl.VisibleBounds"/>
        /// </summary>
        protected virtual void OnVisibleBoundsChanged() { }
        /// <summary>
        /// Událost je vyvolaná po každé změně <see cref="VisibleBounds"/>
        /// </summary>
        public event EventHandler VisibleBoundsChanged;
        /// <summary>
        /// Zkontroluje, zda aktuální viditelná oblast je shodná/jiná než dosavadní, a pokud je jiná pak ji upraví a vyvolá události.
        /// </summary>
        private void _CheckVisibleBoundsChange()
        {
            Rectangle last = __CurrentVisibleBounds;
            Rectangle current = _GetVisibleBounds();
            if (current == last) return;
            __CurrentVisibleBounds = current;
            OnVisibleBoundsChanged();
            VisibleBoundsChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vrátí aktuálně viditelnou oblast vypočtenou pro AutoScrollPosition a ClientSize
        /// </summary>
        /// <returns></returns>
        private Rectangle _GetVisibleBounds()
        {
            Point autoScrollPoint = this.AutoScrollPosition;
            Point origin = new Point(-autoScrollPoint.X, -autoScrollPoint.Y);
            Size size = this.ClientSize;
            return new Rectangle(origin, size);
        }
        private Rectangle __CurrentVisibleBounds;
        /// <summary>
        /// Volá se při kreslení pozadí.
        /// Potomci zde mohou detekovat nové <see cref="VisibleBounds"/> a podle nich zobrazit potřebné controly.
        /// V této metodě budou controly zobrazeny bez blikání = ještě dříve, než se Panel naroluje na novou souřadnici.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            this._CheckVisibleBoundsChange();
            base.OnPaintBackground(e);
        }
        //   TATO METODA SE VOLÁ AŽ PO OnPaintBackground() A NENÍ TEDY NUTNO JI ŘEŠIT:
        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    this._CheckVisibleBoundsChange();
        //    base.OnPaint(e);
        //}
        /// <summary>
        /// Tato metoda je jako jediná vyvolaná při posunu obsahu pomocí kolečka myší a některých dalších akcích (pohyb po controlech, resize), 
        /// ale není volaná při manipulaci se Scrollbary.
        /// </summary>
        protected override void SyncScrollbars()
        {
            base.SyncScrollbars();
            this._CheckVisibleBoundsChange();
        }
        /// <summary>
        /// Tato metoda je vyvolaná při manipulaci se Scrollbary.
        /// Při té se ale nevolá SyncScrollbars().
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnScroll(object sender, XtraScrollEventArgs e)
        {
            base.OnScroll(sender, e);
            this._CheckVisibleBoundsChange();
        }
        /// <summary>
        /// Volá se po změně velikosti tohoto controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this._CheckVisibleBoundsChange();
        }
        #endregion
    }
    #endregion
    #region DxScrollableContent
    /// <summary>
    /// Panel, který v sobě hostuje virtuální control <see cref="ContentControl"/>
    /// a dovoluje uživateli pomocí scrollbarů posouvat jeho virtuální obsah.
    /// <para/>
    /// Rozdíly od standardního <see cref="DxAutoScrollPanelControl"/> panelu:
    /// Tato třída (<see cref="DxScrollableContent"/>) dává prostor pro umístění uživatelského controlu do <see cref="ContentControl"/>, 
    /// kterému pak udržuje maximální možnou velikost v rámci své velikosti [mínus potřebné scrollbary].
    /// Eviduje se zde virtuální celkovou velikost uživatelského controlu v <see cref="ContentTotalSize"/> a pro tuto velikost zajišťuje ScrollBary, 
    /// a s jejich pomocí řídí virtuální zobrazený prostor v <see cref="ContentVirtualBounds"/> (plus event <see cref="ContentVirtualBoundsChanged"/>).
    /// </summary>
    public class DxScrollableContent : DxPanelControl
    {
        #region Konstrukce, proměnné a základní public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxScrollableContent()
        {
            _ContentControl = null;
            _SuppressEvent = false;
            _ContentTotalSize = Size.Empty;
            _ContentVirtualBounds = Rectangle.Empty;
            _ContentVisualPadding = Padding.Empty;
            _ContentVisualSize = this.ClientSize;
            _HScrollBarAllowed = true;
            _HScrollBarVisible = false;
            _VScrollBarAllowed = true;
            _VScrollBarVisible = false;

            _HScrollBar = new DxHScrollBar() { Visible = false, Minimum = 0, SmallChange = 80 };
            _HScrollBar.ValueChanged += _HScrollBar_ValueChanged;
            _HScrollBar.MouseWheel += _HScrollBar_MouseWheel;
            Controls.Add(_HScrollBar);

            _VScrollBar = new DxVScrollBar() { Visible = false, Minimum = 0, SmallChange = 40 };
            _VScrollBar.ValueChanged += _VScrollBar_ValueChanged;
            _VScrollBar.MouseWheel += _VScrollBar_MouseWheel;
            Controls.Add(_VScrollBar);

            ScrollToBoundsBasicPadding = new Padding(3);
            ScrollToBoundsScrollPadding = new Padding(24);
        }
        /// <summary>Control, který zobrazuje obsah</summary>
        private Control _ContentControl;
        /// <summary>Horizontal scrollbar</summary>
        private DxHScrollBar _HScrollBar;
        /// <summary>Horizontal scrollbar: je povolený? = v případě potřeby bude zobrazen</summary>
        private bool _HScrollBarAllowed;
        /// <summary>Horizontal scrollbar: je potřebný? = obsah je větší než současný prostor na ose X</summary>
        private bool _HScrollBarRequired;
        /// <summary>Horizontal scrollbar: je reálně viditelný? = je potřebný a je povolený</summary>
        private bool _HScrollBarVisible;
        /// <summary>Vertical scrollbar</summary>
        private DxVScrollBar _VScrollBar;
        /// <summary>Vertical scrollbar: je povolený? = v případě potřeby bude zobrazen</summary>
        private bool _VScrollBarAllowed;
        /// <summary>Vertical scrollbar: je potřebný? = obsah je větší než současný prostor na ose Y</summary>
        private bool _VScrollBarRequired;
        /// <summary>Vertical scrollbar: je reálně viditelný? = je potřebný a je povolený</summary>
        private bool _VScrollBarVisible;
        /// <summary>Velikost obsahu, který je scrollován = pokud bue větší než viditelný prostor, bude možno scrollovat</summary>
        private Size _ContentTotalSize;
        /// <summary>Aktuálně viditelný výřez obsahu</summary>
        private Rectangle _ContentVirtualBounds;
        /// <summary>Okraje kolem contentu = mezi vnitřním okrajem this panelu a vnějším okrajem panelu <see cref="ContentControl"/></summary>
        private Padding _ContentVisualPadding;
        /// <summary>Viditelná velikost obsahu</summary>
        private Size _ContentVisualSize;
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public override bool LogActive { get { return base.LogActive; } set { base.LogActive = value; if (_ContentControl != null && _ContentControl is DxPanelControl dxPanel) dxPanel.LogActive = value; } }
        #endregion
        #region Splittery
        public bool VSplitterEnabled { get { return _VSplitterEnabled; } set { _SetVSplitterEnabled(value); } }
        private bool _VSplitterEnabled;
        private DevExpress.XtraEditors.SplitterControl _VSplitter;
        private void _SetVSplitterEnabled(bool enabled)
        {
            if (enabled && _VSplitter == null)
            {
                _VSplitter = new DevExpress.XtraEditors.SplitterControl();
                _VSplitter.Bounds = new Rectangle(10, 5, 5, 500);
                _VSplitter.SplitPosition = 15;
                _VSplitter.ShowSplitGlyph = DefaultBoolean.True;
                // _VSplitter.Dock = DockStyle.None;               nemůže být!!!
                _VSplitter.Enabled = true;
                this.Controls.Add(_VSplitter);
            }
            _VSplitterEnabled = enabled;
            if (_VSplitter != null)
                _VSplitter.Visible = enabled;
        }
        #endregion
        #region Public vlastnosti - ContentControl, Size, ContentVirtualBounds...
        /// <summary>
        /// Aktuálně zobrazený obsah.
        /// Jeho fyzický rozměr bude vždy odpovídat aktuálně viditelnému prostoru.
        /// Je třeba zadat celkovou velikost obsahu do <see cref="ContentTotalSize"/>, na tuto velikost budou dimenzovány scrollbary a jejich posuny.
        /// Virtuálně zobrazené souřadnice controlu <see cref="ContentControl"/> jsou vždy uloženy v <see cref="ContentVirtualBounds"/>.
        /// Na změny virtuálních souřadnic (dané změnou fyzického prostoru anebo posunem scrollbarů) lze reagovat v handleru události <see cref="ContentVirtualBoundsChanged"/>.
        /// <para/>
        /// Setování instance do této property ji zařadí do this controlu jako Child , změna instance vyřadí dosavadní z this.Controls atd.
        /// <para/>
        /// Uživatel by nikdy neměl řídit pozici tohoto vnitřního objektu, ta je dána prostorem uvnitř this panelu <see cref="DxScrollableContent"/>.
        /// Při každé změně rozměru this panelu bude správně umístěn i tento <see cref="ContentControl"/>.
        /// </summary>
        public Control ContentControl
        {
            get { return _ContentControl; }
            set
            {
                Control contentControl = _ContentControl;
                if (contentControl != null)
                {
                    if (this.Controls.Contains(contentControl))
                        this.Controls.Remove(contentControl);
                    contentControl.MouseWheel -= ContentControl_MouseWheel;
                    if (contentControl is DxPanelControl dxPanel)
                        dxPanel.LogActive = false;
                    _ContentControl = null;
                }
                contentControl = value;
                if (contentControl != null)
                {
                    this.Controls.Add(contentControl);
                    _ContentControl = contentControl;
                    contentControl.MouseWheel += ContentControl_MouseWheel;
                    if (contentControl is DxPanelControl dxPanel)
                        dxPanel.LogActive = this.LogActive;
                    DoLayoutContent();
                }
            }
        }
        /// <summary>
        /// Okraje kolem contentu = mezi vnitřním okrajem this panelu a vnějším okrajem panelu <see cref="ContentControl"/>.
        /// Výchozí hodnota = {0,0,0,0}. Pak Content (obsah) obsazuje celý vnitřní prostor this panelu, vyjma potřebné scrollbary.
        /// Jde o designovou hodnotu, na vizuální pixely je přepočtena podle aktuálního Zoomu a DPI.
        /// <para/>
        /// Zadáním kladných hodnot dojde k vytvoření prostoru ("okraje") v daných oblastech okolo <see cref="ContentControl"/> 
        /// (<see cref="ContentControl"/> bude menší než dostupný vnitřní prostor). 
        /// Tyto okraje pak aplikace může využít k umístění fixních = nescrollovaných prvků (záhlaví sloupců, řádků; pravítka nahoře, dole; dolní součtový řádek, atd).
        /// <para/>
        /// Společně s panelem <see cref="ContentControl"/> budou odsunuty a upraveny i ScrollBary.
        /// Ty budou vždy na úplném okaji this panelu, ale budou jen ve velikosti odpovídající <see cref="ContentControl"/>.
        /// <para/>
        /// Záporné hodnoty v této souřadnici nejsou akceptovány.
        /// Příliš velké hodnoty nejsou doporučovány, mohou vést ke zmizení obsahu.
        /// </summary>
        public Padding ContentVisualPadding { get { return _ContentVisualPadding; } set { SetContentVisualPadding(value); } }
        /// <summary>
        /// Aktuální viditelná velikost obsahu
        /// </summary>
        public Size ContentVisualSize { get { return _ContentVisualSize; } }
        /// <summary>
        /// Celková (virtuální) velikost obsahu. Na tuto plochu jsou dimenzovány ScrollBary a tato plocha je posouvána.
        /// </summary>
        public Size ContentTotalSize { get { return _ContentTotalSize; } set { _ContentTotalSize = value; DoLayoutContent(); } }
        /// <summary>
        /// Aktuální viditelné souřadnice virtuálního obsahu.
        /// Tento prostor si lze jednoduše představit jako "malou osvětlenou část" z celkové "virtuální" plochy - dostupné v tomto panelu.
        /// Máme-li celkovou velikost prostoru <see cref="ContentTotalSize"/> = { W=500, H=20000 }, pak nelze najednou zobrazit oněch 20000 pixelů výšky.
        /// Aktuálně bude zobrazeno pouze malé množství z jeho výšky, zbytek bude dostupný pomocí svislého Scrollbaru.
        /// Pokud tedy <see cref="ContentVirtualBounds"/> bude { X=0, Y=400, W=500, H=400 }, pak zobrazujeme prostor odrolovaný o jednu obrazovku dolů (výška "prostoru kukátka" = 400px).
        /// <para/>
        /// Počáteční bod je dán ScrollBary, velikost je dána fyzickou velikostí this panelu mínus prostor ScrollBarů.
        /// <para/>
        /// Tuto hodnotu není možno změnit, je odvozena od fyzické velikosti celého controlu, zmenšené o případné ScrollBary, a pozice je dána hodnotou ScrollBarů.
        /// Setovat lze <see cref="ContentVirtualLocation"/>.
        /// </summary>
        public Rectangle ContentVirtualBounds
        {
            get { return _ContentVirtualBounds; }
            private set
            {   // Tady nebudeme řešit kontroly ani návaznosti na ScrollBary, to musel řešit volající. Tady jen hlídáme změnu a voláme event:
                var oldValue = _ContentVirtualBounds;
                var newValue = value;
                if (oldValue != newValue)
                {
                    _ContentVirtualBounds = newValue;
                    _RunContentVirtualBoundsChanged();
                }
            }
        }
        /// <summary>
        /// Souřadnice počátku viditelné části obsahu <see cref="ContentVirtualBounds"/>.
        /// Tuto hodnotu je možno setovat a tak programově řídit posuny obsahu.
        /// Setovaná hodnota bude zkonrolována, upravena a následně vložena do <see cref="ContentVirtualBounds"/>. 
        /// Po změně dojde k volání události <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        public Point ContentVirtualLocation { get { return _ContentVirtualBounds.Location; } set { SetVirtualLocation(value); } }
        /// <summary>
        /// Tuto událost vyvolá this panel <see cref="DxScrollableContent"/> 
        /// při každé změně velikosti nebo pozice virtuálního prostoru <see cref="ContentVirtualBounds"/>.
        /// </summary>
        public EventHandler ContentVirtualBoundsChanged;
        /// <summary>
        /// Obsahuje true, pokud je povolenou zobrazit a používat Horizontální (=vodorovný) ScrollBar.
        /// Výchozí je true. ScrollBar bude zobrazen, když bude potřeba.
        /// Pokud je false, ScrollBar nebude zobrazován, ale myší kolečko na prvku bude fungovat jako by byl vidět.
        /// <para/>
        /// Používá se tedy, když je vedle sebe více panelů se spřaženými souřadnicemi (typicky Bands v tabulkách), 
        /// kde máme vedle sebe dvě skupiny dat, v pravé je svislý ScollBar zobrazen, ale nechceme jej mít i ve skupině vlevo.
        /// </summary>
        public bool HScrollBarAllowed { get { return _HScrollBarAllowed; } set { _HScrollBarAllowed = value; DoLayoutContent(); } }
        /// <summary>
        /// Obsahuje true, pokud je Horizontální (=vodorovný) ScrollBar potřebný = obsah je větší než viditelná oblast.
        /// Pokud ale není povole (<see cref="HScrollBarAllowed"/> je false), pak ScrollBar nebude fyzicky zobrazen (<see cref="HScrollBarVisible"/> bude false).
        /// </summary>
        public bool HScrollBarRequired { get { return _HScrollBarRequired; } }
        /// <summary>
        /// Obsahuje true, pokud je viditelný Horizontální (=vodorovný) ScrollBar
        /// </summary>
        public bool HScrollBarVisible { get { return _HScrollBarVisible; } }
        /// <summary>
        /// Indikátory přítomné na horizontálním scrollbaru
        /// </summary>
        public ScrollBarIndicators HScrollBarIndicators { get { return _HScrollBar.Indicators; } }
        /// <summary>
        /// Hodnota na horizontálním ScrollBaru, bez korekcí, podle povolení pro horizontální ScrollBar:
        /// <para/>
        /// Pokud je viditelný (<see cref="_HScrollBarVisible"/> = true), 
        /// pak je zde reálná hodnota ze ScrollBaru <see cref="_HScrollBar"/>.
        /// <para/>
        /// Pokud není potřebný (<see cref="_HScrollBarRequired"/> = false), 
        /// pak je zde 0 (protože obsah je zobrazen celý = obsah není větší než disponibilní prostor).
        /// <para/>
        /// Pokud je potřebný (<see cref="_HScrollBarRequired"/> = true), ale z nějakého důvodu není viditelný
        /// (není povolen: <see cref="_HScrollBarAllowed"/> = false) anebo není možno jej zobrazit: <see cref="_HScrollBarVisible"/> = false), 
        /// pak je zde odpovídající hodnota z <see cref="_ContentVirtualBounds"/>.X.
        /// </summary>
        private int _HScrollBarCurrentValue
        {
            get
            {
                if (_HScrollBarVisible) return _HScrollBar.Value;              // Je fyzicky viditelný = převezmu hodnotu
                if (_HScrollBarRequired) return _ContentVirtualBounds.X;       // Není viditelný, ale je potřebný = převezmu souřadnici
                return 0;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud je povolenou zobrazit a používat Vertikální (=svislý) ScrollBar.
        /// Výchozí je true. ScrollBar bude zobrazen, když bude potřeba.
        /// Pokud je false, ScrollBar nebude zobrazován, ale myší kolečko na prvku bude fungovat jako by byl vidět.
        /// <para/>
        /// Používá se tedy, když je nad sebou více panelů se spřaženými souřadnicemi (typicky Bands v tabulkách), 
        /// kde máme nad sebou dvě skupiny dat, v dolní je vodorovný ScollBar zobrazen, ale nechceme jej mít i ve skupině nahoře.
        /// </summary>
        public bool VScrollBarAllowed { get { return _VScrollBarAllowed; } set { _VScrollBarAllowed = value; DoLayoutContent(); } }
        /// <summary>
        /// Obsahuje true, pokud je Vertikální (=svislý) ScrollBar potřebný = obsah je větší než viditelná oblast.
        /// Pokud ale není povole (<see cref="VScrollBarAllowed"/> je false), pak ScrollBar nebude fyzicky zobrazen (<see cref="VScrollBarVisible"/> bude false).
        /// </summary>
        public bool VScrollBarRequired { get { return _VScrollBarRequired; } }
        /// <summary>
        /// Obsahuje true, pokud je viditelný Vertikální (=svislý) ScrollBar
        /// </summary>
        public bool VScrollBarVisible { get { return _VScrollBarVisible; } }
        /// <summary>
        /// Indikátory přítomné na vertikálním scrollbaru
        /// </summary>
        public ScrollBarIndicators VScrollBarIndicators { get { return _VScrollBar.Indicators; } }
        /// <summary>
        /// Hodnota na vertikálním ScrollBaru, bez korekcí, podle povolení pro vertikální ScrollBar:
        /// <para/>
        /// Pokud je viditelný (<see cref="_VScrollBarVisible"/> = true), 
        /// pak je zde reálná hodnota ze ScrollBaru <see cref="_VScrollBar"/>.
        /// <para/>
        /// Pokud není potřebný (<see cref="_VScrollBarRequired"/> = false), 
        /// pak je zde 0 (protože obsah je zobrazen celý = obsah není větší než disponibilní prostor).
        /// <para/>
        /// Pokud je potřebný (<see cref="_VScrollBarRequired"/> = true), ale z nějakého důvodu není viditelný
        /// (není povolen: <see cref="_VScrollBarAllowed"/> = false) anebo není možno jej zobrazit: <see cref="_VScrollBarVisible"/> = false), 
        /// pak je zde odpovídající hodnota z <see cref="_ContentVirtualBounds"/>.Y.
        /// </summary>
        private int _VScrollBarCurrentValue
        {
            get
            {
                if (_VScrollBarVisible) return _VScrollBar.Value;              // Je fyzicky viditelný = převezmu hodnotu
                if (_VScrollBarRequired) return _ContentVirtualBounds.Y;       // Není viditelný, ale je potřebný = převezmu souřadnici
                return 0;
            }
        }
        #endregion
        #region Layout a řízení ScrollBarů
        /// <summary>
        /// Výška vodorovného ScrollBaru
        /// </summary>
        public int DefaultHorizontalScrollBarHeight { get { return _HScrollBar.GetDefaultHorizontalScrollBarHeight(); } }
        /// <summary>
        /// Šířka svislého ScrollBaru
        /// </summary>
        public int DefaultVerticalScrollBarWidth { get { return _VScrollBar.GetDefaultVerticalScrollBarWidth(); } }
        /// <summary>
        /// Uloží a akceptuje souřadnici <see cref="ContentVisualPadding"/>
        /// </summary>
        /// <param name="contentVisualPadding"></param>
        protected void SetContentVisualPadding(Padding contentVisualPadding)
        {
            int l = contentVisualPadding.Left.Align(0, 800);
            int t = contentVisualPadding.Top.Align(0, 600);
            int r = contentVisualPadding.Right.Align(0, 800);
            int b = contentVisualPadding.Bottom.Align(0, 600);

            _ContentVisualPadding = new Padding(l, t, r, b);
            DoLayoutContent();
        }
        /// <summary>
        /// Na základě aktuálních fyzických rozměrů a podle <see cref="ContentTotalSize"/> určí potřebnou viditelnost ScrollBarů,
        /// určí souřadnice prvků (Content i ScrollBary), určí vlastnosti pro ScrollBary a velikost prosotru pro vlastní obsah (<see cref="_ContentVisualSize"/>).
        /// Pokud dojde k jakékoli změně, vyvolá jedenkrát událost <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        protected void DoLayoutContent()
        {
            // Vizuální prostor:
            Rectangle innerBounds = InnerRectangle;
            Padding visualPadding = DxComponent.ZoomToGui(_ContentVisualPadding, this.CurrentDpi);           // přepočet Design => Current
            Rectangle contentBounds = Rectangle.FromLTRB(innerBounds.Left + visualPadding.Left, innerBounds.Top + visualPadding.Top, innerBounds.Right - visualPadding.Right, innerBounds.Bottom - visualPadding.Bottom);
            if (contentBounds.Width < 0) contentBounds.Width = 0;
            if (contentBounds.Height < 0) contentBounds.Height = 0;
            if (this.Parent == null)
            {   // Bez parenta toho moc neděláme, je to předčasné (typicky: nemáme správnou velikost, ani neznáme CurrentDPI).
                // Po změně parenta tahle metoda proběhne taky, a vyřešíme vše potřebné.
                _ContentVisualSize = contentBounds.Size;
                _ContentControl?.SetBounds(contentBounds);
                return;
            }

            // Velikost virtuálního obsahu:
            Size contentTotalSize = this.ContentTotalSize;

            // Horizontální (vodorovný) ScrollBar: bude potřebný (a viditelný), když šířka obsahu je větší než šířka klienta, a zmenší tak výšku klienta:
            bool hRequired = (contentBounds.Width > 0 && contentTotalSize.Width > contentBounds.Width);
            bool hVisible = (_HScrollBarAllowed && hRequired);
            int hScrollSize = (hVisible ? _VScrollBar.GetDefaultHorizontalScrollBarHeight() : 0);
            if (hVisible) contentBounds.Height -= hScrollSize;

            // Vertikální (svislý) ScrollBar: bude potřebný (a viditelný), když výška obsahu je větší než výška klienta, a zmenší tak šířku klienta:
            bool vRequired = (contentBounds.Height > 0 && contentTotalSize.Height > contentBounds.Height);
            bool vVisible = (_VScrollBarAllowed && vRequired);
            int vScrollSize = (vVisible ? _VScrollBar.GetDefaultVerticalScrollBarWidth() : 0);
            if (vVisible) contentBounds.Width -= vScrollSize;

            // Pokud dosud nebyl viditelný Vertikální (svislý) ScrollBar, ale je viditelný Horizontální (vodorovný) ScrollBar:
            //  pak Horizontální ScrollBar zmenšil výšku obsahu (clientHeight), a může se stát, že bude třeba zobrazit i Vertikální ScrollBar:
            if (!vVisible && hVisible && (contentTotalSize.Height > contentBounds.Height))
            {
                vVisible = true;
                vScrollSize = _VScrollBar.GetDefaultVerticalScrollBarWidth();
                contentBounds.Width -= vScrollSize;
            }

            // Pokud je přílš malá šířka a je viditelný Vertikální (svislý) ScrollBar: vrátit plnou šířku a zrušit scrollBar:
            if (contentBounds.Width < 10 && vVisible)
            {
                contentBounds.Width += vScrollSize;
                vVisible = false;
                vScrollSize = 0;
            }
            // Pokud je přílš malá výška a je viditelný Horizontální (vodorovný) ScrollBar: vrátit plnou výšku a zrušit scrollBar:
            if (contentBounds.Height < 10 && hVisible)
            {
                contentBounds.Height += hScrollSize;
                hVisible = false;
                hScrollSize = 0;
            }

            // bool reCalcVirtualBounds = (clientWidth != contentVirtualBounds.Width || clientHeight != contentVirtualBounds.Height);

            _ContentControl?.SetBounds(contentBounds);
            _ContentVisualSize = new Size(contentBounds.Width, contentBounds.Height);
            _HScrollBarRequired = hRequired;
            _HScrollBarVisible = hVisible;
            _VScrollBarRequired = vRequired;
            _VScrollBarVisible = vVisible;

            bool suppressEvent = _SuppressEvent;
            try
            {
                _SuppressEvent = true;

                if (vVisible)
                {
                    _VScrollBar.SetBounds(new Rectangle(innerBounds.Right - vScrollSize, contentBounds.Y, vScrollSize, contentBounds.Height));
                    _VScrollBar.Maximum = contentTotalSize.Height;
                    _VScrollBar.LargeChange = contentBounds.Height;
                }
                if (hVisible)
                {
                    _HScrollBar.SetBounds(new Rectangle(contentBounds.X, innerBounds.Bottom - hScrollSize, contentBounds.Width, hScrollSize));
                    _HScrollBar.Maximum = contentTotalSize.Width;
                    _HScrollBar.LargeChange = contentBounds.Width;
                }

                if (_VScrollBar.VisibleInternal != vVisible) _VScrollBar.Visible = vVisible;
                if (_HScrollBar.VisibleInternal != hVisible) _HScrollBar.Visible = hVisible;
            }
            finally
            {
                _SuppressEvent = suppressEvent;
            }

            // Tady se vezmou souřadnice X a Y ze ScrollBarů (z těch viditelných), vezme se i aktuální _ContentVisualSize,
            //  určí se a uloží reálné souřadnice ContentVirtualBounds a pokud dojde ke změně, vyvolá se patřičný event:
            ApplyScrollBarsToVirtualLocation();
        }
        /// <summary>
        /// OnParentChanged
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DoLayoutContent();
        }
        /// <summary>
        /// OnClientSizeChanged
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            DoLayoutContent();
        }
        /// <summary>
        /// OnBorderStyleChanged
        /// </summary>
        protected override void OnBorderStyleChanged()
        {
            base.OnBorderStyleChanged();
            DoLayoutContent();
        }
        /// <summary>
        /// OnZoomChanged
        /// </summary>
        protected override void OnZoomChanged()
        {
            base.OnZoomChanged();
            DoLayoutContent();
            OnInvalidateContentAfter();
        }
        /// <summary>
        /// OnStyleChanged
        /// </summary>
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            DoLayoutContent();
            OnInvalidateContentAfter();
        }
        /// <summary>
        /// OnDpiChangedAfterParent
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            DoLayoutContent();
            OnInvalidateContentAfter();
        }
        /// <summary>
        /// Je vyvoláno po změně DPI, po změně Zoomu a po změně skinu. Volá se po přepočtu layoutu.
        /// Může vést k invalidaci interních dat v <see cref="DxScrollableContent.ContentControl"/>.
        /// </summary>
        protected virtual void OnInvalidateContentAfter() { }
        #endregion
        #region Scroll to control / to virtual bounds
        /// <summary>
        /// Zajistí nascrollování obsahu tak, aby daný prostor byl viditelný.
        /// Akceptuje přídavek k rozměru daný v <see cref="ScrollToBoundsBasicPaddingCurrent"/>.
        /// </summary>
        /// <param name="controlVirtualBounds"></param>
        /// <param name="groupVirtualBounds"></param>
        /// <param name="skipEvent"></param>
        /// <returns></returns>
        public bool ScrollToBounds(Rectangle controlVirtualBounds, Rectangle? groupVirtualBounds = null, bool skipEvent = false)
        {
            Size totalSize = this.ContentTotalSize;
            Rectangle currentBounds = this.ContentVirtualBounds;
            Rectangle targetBounds = controlVirtualBounds.Add(ScrollToBoundsBasicPaddingCurrent);           // Malý přídavek, jen aby daný control nebyl zobrazen úplně na hraně
            if (currentBounds.Contains(targetBounds)) return false;                                         // Požadovaný prostor je zcela vidět

            // Budeme muset Scrollovat:
            Point oldVirtualOrigin = currentBounds.Location;
            bool suppressEvent = _SuppressEvent;
            try
            {
                _SuppressEvent = true;

                if (groupVirtualBounds.HasValue)
                {   // Když už scrollovat, tak se pokusíme narolovat na větší prostor:
                    targetBounds = groupVirtualBounds.Value.Add(ScrollToBoundsScrollPaddingCurrent);        // Například control plus jeho label nebo celá grupa...  Plus větší přídavek.
                    ScrollToBounds(currentBounds, targetBounds, totalSize);
                }

                targetBounds = controlVirtualBounds.Add(ScrollToBoundsScrollPaddingCurrent);                // Větší přídavek, když už scrollujeme, aby cílový prostor nebyl úplně na okraji
                ScrollToBounds(currentBounds, targetBounds, totalSize);
            }
            finally
            {
                _SuppressEvent = suppressEvent;
            }

            Point newVirtualOrigin = this.ContentVirtualBounds.Location;
            if (newVirtualOrigin == oldVirtualOrigin) return false;

            // Nyní víme, že došlo ke změně:
            if (!skipEvent)
                _RunContentVirtualBoundsChanged();

            return true;
        }
        /// <summary>
        /// Zajistí scrollování podle patřičných pravidel, pro požadované souřadnice, pro aktuální zobrazené souřadnice a celkovou velikost obsahu
        /// </summary>
        /// <param name="currentBounds">Aktuální zobrazený prostor</param>
        /// <param name="targetBounds">Požadovaný prostor, který má být zobrazen</param>
        /// <param name="totalSize">Velikost obsahu</param>
        protected void ScrollToBounds(Rectangle currentBounds, Rectangle targetBounds, Size totalSize)
        {
            ScrollToBounds(targetBounds.X, targetBounds.Right, currentBounds.X, currentBounds.Right, totalSize.Width, HScrollBarVisible, _HScrollBar);
            ScrollToBounds(targetBounds.Y, targetBounds.Bottom, currentBounds.Y, currentBounds.Bottom, totalSize.Height, VScrollBarVisible, _VScrollBar);
        }
        /// <summary>
        /// Zajistí scrollování podle patřičných pravidel v jednom směru (Vertikální nebo Horizontální)
        /// </summary>
        /// <param name="targetBegin"></param>
        /// <param name="targetEnd"></param>
        /// <param name="currentBegin"></param>
        /// <param name="currentEnd"></param>
        /// <param name="totalSize"></param>
        /// <param name="scrollBarVisible"></param>
        /// <param name="scrollBar"></param>
        protected void ScrollToBounds(int targetBegin, int targetEnd, int currentBegin, int currentEnd, int totalSize, bool scrollBarVisible, ScrollBarBase scrollBar)
        {
            if (!scrollBarVisible || scrollBar == null) return;
            int currentStart = currentBegin;
            int currentSize = currentEnd - currentBegin;

            if (targetEnd > currentEnd)
            {
                currentEnd = targetEnd;
                if (currentEnd > totalSize) currentEnd = totalSize;
                currentBegin = currentEnd - currentSize;
            }

            if (targetBegin < currentBegin)
            {
                currentBegin = targetBegin;
                if (currentBegin < 0) currentBegin = 0;
                currentEnd = currentBegin + currentSize;
            }

            if (currentBegin != currentStart)
            {
                scrollBar.Value = currentBegin;
            }
        }
        /// <summary>
        /// Okraje, přidávané k požadovaném prostoru controlu v metodě <see cref="ScrollToBounds(Rectangle, Rectangle?, bool)"/> před tím, než se ověří jeho aktuální viditelnost.
        /// Tyto okraje "zvětšují" control, tak aby se Scroll provedl i tehdy, když vlastní control sice je vidět, ale je těsně na okraji viditelného prostoru.
        /// <para/>
        /// Výchozí hodnota = 3 pixely.
        /// Jde o designové pixely = bez aplikování odlišného DPI a Zoomu, ty se aplikují interně.
        /// </summary>
        public Padding ScrollToBoundsBasicPadding { get; set; }
        /// <summary>
        /// Aktuální hodnota <see cref="ScrollToBoundsBasicPadding"/> (pro aktuální Zoom a DPI)
        /// </summary>
        protected Padding ScrollToBoundsBasicPaddingCurrent { get { return DxComponent.ZoomToGui(ScrollToBoundsBasicPadding, CurrentDpi); } }
        /// <summary>
        /// Okraje, přidávané ke scrollu prováděnému v metodě <see cref="ScrollToBounds(Rectangle, Rectangle?, bool)"/> v situaci, kdy je potřeba reálně posunout obsah.
        /// Tedy: pokud požadovaný obsah (s přidáním <see cref="ScrollToBoundsBasicPadding"/>) je celý viditelný, pak se scrollovat nebude ani když nebude dodržen zde uvedený okraj.
        /// Jakmile ale bude část (zvětšeného) obsahu neviditelná, pak se provede Scroll tak, aby okolo obsahu byl právě tento okraj.
        /// <para/>
        /// Výchozí hodnota = 24 pixelů.
        /// Jde o designové pixely = bez aplikování odlišného DPI a Zoomu, ty se aplikují interně.
        /// </summary>
        public Padding ScrollToBoundsScrollPadding { get; set; }
        /// <summary>
        /// Aktuální hodnota <see cref="ScrollToBoundsScrollPadding"/> (pro aktuální Zoom a DPI)
        /// </summary>
        protected Padding ScrollToBoundsScrollPaddingCurrent { get { return DxComponent.ZoomToGui(ScrollToBoundsScrollPadding, CurrentDpi); } }
        #endregion
        #region Výpočty virtuální souřadnice a reakce na interaktivní posuny
        /// <summary>
        /// Nastaví počáteční souřadnici virtuálního prostoru podle daného bodu, před tím provede veškeré kontroly, při změně reálné hodnoty vyvolá událost
        /// </summary>
        /// <param name="virtualLocation"></param>
        protected void SetVirtualLocation(Point virtualLocation)
        {
            Rectangle virtualBoundsOld = this.ContentVirtualBounds;

            Size contentVisualSize = _ContentVisualSize;
            int x = virtualLocation.X;
            int y = virtualLocation.Y;
            int vw = contentVisualSize.Width;
            int vh = contentVisualSize.Height;

            Size contentTotalSize = _ContentTotalSize;
            int tw = contentTotalSize.Width;
            int th = contentTotalSize.Height;
            if ((x + vw) > tw) x = tw - vw;                // Pokud by aktuální X bylo větší, takže by viditelná šířka přesahovala celkovou šířku, pak posunu X doleva...
            if ((y + vh) > th) y = th - vh;                //  stejně tak výška a Y
            if (x < 0) x = 0;
            if (y < 0) y = 0;

            // Scrollbary a případná změna jejich hodnoty - jen pokud jsou reálně viditelné:
            // (změna souřadnice Location nemění šířku - a to ani vizuální, ani celkovou, proto nemění Visible ScrollBarů ani jejich maximum a LargeChange).
            bool hv = _HScrollBarVisible;
            bool vv = _VScrollBarVisible;
            if (hv || vv)
            {
                int sx = _HScrollBarCurrentValue;
                int sy = _VScrollBarCurrentValue;
                bool changeX = (hv && x != sx);
                bool changeY = (vv && y != sy);
                if (changeX || changeY)
                {   // Došlo k tomu, že musíme změnit hodnotu na některém ScrollBaru, protože setovaná hodnota je jiná, než ukazují ScrollBary.
                    bool suppressEvent = _SuppressEvent;
                    try
                    {
                        // Tedy vyvoláme setování upravené souřadnice do ScrollBarů, to vyvolá zápis nové hodnoty do ContentVirtualBounds, a to klidně dvakrát (X i Y).
                        // Ale nechci volat dva eventy, jeden pro každý směr (s ohledem na náročnost navazujících přepočtů),
                        // takže potlačím volání eventu ContentVirtualBoundsChanged :
                        _SuppressEvent = true;
                        if (changeX) _HScrollBar.Value = x;
                        if (changeY) _VScrollBar.Value = y;
                    }
                    finally
                    {
                        _SuppressEvent = suppressEvent;
                    }
                }
            }

            // Nyní musím vložit novou hodnotu do ContentVirtualBounds - ale s vyvoláním eventhandleru ContentVirtualBoundsChanged,
            // protože ke změně reálně došlo, a aplikace musí dostat informaci o této změně:
            // Ono je totiž možné, že vložením korigované hodnoty do ScrollBarů (o pár řádků nahoře) se hodnota ContentVirtualBoundsChanged už změnila, ale byl potlačen eventhandler!
            Rectangle virtualBoundsNew = new Rectangle(x, y, vw, vh);
            this._ContentVirtualBounds = virtualBoundsOld;
            this.ContentVirtualBounds = virtualBoundsNew;                      // Tato sekvence spolehlivě zajistí, že pokud došlo ke změně hodnoty v rámci této metody, bude volán eventhandler, a že hodnota bude ve výsledku platná.
        }
        /// <summary>
        /// Pokud není potlačen event <see cref="_SuppressEvent"/>, pak vyvolá háček <see cref="OnContentVirtualBoundsChanged"/> a event <see cref="ContentVirtualBoundsChanged"/>
        /// </summary>
        private void _RunContentVirtualBoundsChanged()
        {
            if (!_SuppressEvent)
            {
                OnContentVirtualBoundsChanged();
                ContentVirtualBoundsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Je voláno pokud dojde ke změně hodnoty <see cref="DxScrollableContent.ContentVirtualBounds"/>, před eventem <see cref="DxScrollableContent.ContentVirtualBoundsChanged"/>
        /// </summary>
        protected virtual void OnContentVirtualBoundsChanged() { }
        /// <summary>
        /// Po změně hodnoty na ScrollBarech - přemístí <see cref="ContentVirtualLocation"/> (a vyvolá události, pokud nejsou potlačené)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HScrollBar_ValueChanged(object sender, EventArgs e)
        {
            if (_HScrollBarAllowed && _HScrollBarVisible)
                ApplyScrollBarsToVirtualLocation();
        }
        /// <summary>
        /// Po změně hodnoty na ScrollBarech - přemístí <see cref="ContentVirtualLocation"/> (a vyvolá události, pokud nejsou potlačené)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            if (_VScrollBarAllowed && _VScrollBarVisible)
                ApplyScrollBarsToVirtualLocation();
        }
        /// <summary>
        /// Na controlu <see cref="ContentControl"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentControl_MouseWheel(object sender, MouseEventArgs e)
        {
            Orientation? orientation = GetContentShiftOrientation();
            bool largeStep = ModifierKeys.HasFlag(Keys.Shift);
            DoContentShift(orientation, e.Delta, largeStep);
        }
        /// <summary>
        /// Na controlu <see cref="_VScrollBar"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VScrollBar_MouseWheel(object sender, MouseEventArgs e)
        {
            bool largeStep = ModifierKeys.HasFlag(Keys.Shift);
            DoContentShift(Orientation.Vertical, e.Delta, largeStep);
        }
        /// <summary>
        /// Na controlu <see cref="_HScrollBar"/> bylo otočeno myškou
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HScrollBar_MouseWheel(object sender, MouseEventArgs e)
        {
            bool largeStep = ModifierKeys.HasFlag(Keys.Shift);
            DoContentShift(Orientation.Horizontal, e.Delta, largeStep);
        }
        /// <summary>
        /// Vrátí vhodný scrollbar pro případ, kdy uživatel skroluje na clastním ContentPanelu (typická situace).
        /// Primárně vrací svislý, při klávese Control vrací vodorovný (pokud jsou přítomny oba).
        /// </summary>
        /// <returns></returns>
        private Orientation? GetContentShiftOrientation()
        {
            bool hasVScrollBar = _VScrollBarVisible;
            bool hasHScrollBar = _HScrollBarVisible;
            if (hasVScrollBar && hasHScrollBar)
            {
                if (ModifierKeys.HasFlag(Keys.Control)) return Orientation.Horizontal;
                return Orientation.Vertical;
            }
            if (hasVScrollBar) return Orientation.Vertical;
            if (hasHScrollBar) return Orientation.Horizontal;
            return null;
        }
        /// <summary>
        /// Má být proveden posun obsahu v dané orientaci, v daném směru a v kroku malém/velkém.
        /// Zajistí posunutí obsahu pomocí výpočtu nové souřadnice (které bude vložena do odpovídajícího ScrollBaru).
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="delta"></param>
        /// <param name="largeStep"></param>
        private void DoContentShift(Orientation? orientation, int delta, bool largeStep)
        {
            if (!orientation.HasValue) return;
            ScrollBarBase scrollBar = (orientation.Value == Orientation.Horizontal ? (ScrollBarBase)_HScrollBar : (ScrollBarBase)_VScrollBar);

            int direction = (delta < 0 ? 1 : (delta > 0 ? -1 : 0));                                          // Směr posunutí
            int coefficient = (largeStep ? (9 * scrollBar.LargeChange / 10) : (2 * scrollBar.SmallChange));  // largeStep posouvá o 90% LargeChange, smallStep posouvá 2 * SmallChange
            int distance = direction * coefficient;
            int value = scrollBar.Value;
            int maxValue = scrollBar.Maximum - scrollBar.LargeChange + 1;
            int newValue = value + distance;
            newValue = (newValue < 0 ? 0 : (newValue > maxValue ? maxValue : newValue));
            if (newValue == value) return;

            // Došlo ke změně hodnoty. Mám dvě možnosti, jak ji do controlu dostat:
            //  a) Vložím ji do zde používaného ScrollBaru:
            //        scrollBar.Value = newValue;
            //    Ale to vede k následujícímu: pokud zde určím hodnotu, kterou následně ScrollBar pošle skrze ScrollBars_ValueChanged(), do ApplyScrollBarsToVirtualLocation(),
            //    a pak do SetVirtualLocation(), kde následně dojde ke korekci hodnoty ScrollBaru = v části kódu: if (changeX || changeY) ... 
            //    Pak se hodnota daného ScrollBaru znovu změní a proběhne rekurze celé sekvence.
            //  b) Čistší řešení je určit cílovou souřadnici zde, poslat ji do metody SetVirtualLocation() přímo odsud,
            //    a tam se pak případně nastaví aktuálně platné hodnoty ScrollBarů.
            if (orientation.Value == Orientation.Horizontal)
                SetVirtualLocation(new Point(newValue, _VScrollBarCurrentValue));
            else
                SetVirtualLocation(new Point(_HScrollBarCurrentValue, newValue));
        }
        /// <summary>
        /// Hodnoty ze ScrollBarů (pokud jsou viditelné) aplikuje do <see cref="SetVirtualLocation(Point)"/>
        /// </summary>
        private void ApplyScrollBarsToVirtualLocation()
        {
            SetVirtualLocation(new Point(_HScrollBarCurrentValue, _VScrollBarCurrentValue));
        }
        /// <summary>
        /// Hodnota true potlačí vyvolání události <see cref="OnContentVirtualBoundsChanged"/> a eventu <see cref="ContentVirtualBoundsChanged"/>.
        /// </summary>
        private bool _SuppressEvent;
        #endregion
    }
    #endregion
    #region DxHScrollBar + DxVScrollBar + ScrollBarIndicators
    /// <summary>
    /// Horizontální ScrollBar (vodorovný = zleva doprava)
    /// </summary>
    public class DxHScrollBar : DevExpress.XtraEditors.HScrollBar
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region Indikátory
        /// <summary>
        /// Indikátory, označující část plochy scrollbaru
        /// </summary>
        public ScrollBarIndicators Indicators { get { if (_Indicators == null) _Indicators = new ScrollBarIndicators(this, Orientation.Horizontal); return _Indicators; } }
        private ScrollBarIndicators _Indicators;
        /// <summary>
        /// Obsahuje true, pokud máme reálně nějaké indikátory
        /// </summary>
        protected bool HasIndicators { get { return (_Indicators != null && _Indicators.HasIndicators); } }
        /// <summary>
        /// Kreslení ScrollBaru vyvolá i kreslení indikátorů
        /// </summary>
        /// <param name="args"></param>
        protected override void OnPaint(ScrollBarInfoArgs args)
        {
            base.OnPaint(args);
            _Indicators?.PaintIndicators(args);
        }
        #endregion
    }
    /// <summary>
    /// Vertikální ScrollBar (svislý = zeshora dolů)
    /// </summary>
    public class DxVScrollBar : DevExpress.XtraEditors.VScrollBar
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
        #region Indikátory
        /// <summary>
        /// Indikátory, označující část plochy scrollbaru
        /// </summary>
        public ScrollBarIndicators Indicators { get { if (_Indicators == null) _Indicators = new ScrollBarIndicators(this, Orientation.Vertical); return _Indicators; } }
        private ScrollBarIndicators _Indicators;
        /// <summary>
        /// Obsahuje true, pokud máme reálně nějaké indikátory
        /// </summary>
        protected bool HasIndicators { get { return (_Indicators != null && _Indicators.HasIndicators); } }
        /// <summary>
        /// Kreslení ScrollBaru vyvolá i kreslení indikátorů
        /// </summary>
        /// <param name="args"></param>
        protected override void OnPaint(ScrollBarInfoArgs args)
        {
            base.OnPaint(args);
            _Indicators?.PaintIndicators(args);
        }
        #endregion
    }
    /// <summary>
    /// Třída definující a vykreslující sadu indikátorů v prostoru ScrollBaru
    /// </summary>
    public class ScrollBarIndicators
    {
        #region Konstrukce a základní public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="orientation"></param>
        public ScrollBarIndicators(DevExpress.XtraEditors.ScrollTouchBase owner, Orientation orientation)
        {
            _Owner = owner;
            _Orientation = orientation;
            _Indicators = new List<Indicator>();
            _ColorAlphaArea = 200;
            _ColorAlphaThumb = 90;
            _Effect3DRatio = 0.25f;
        }
        private DevExpress.XtraEditors.ScrollTouchBase _Owner;
        private Orientation _Orientation;
        private List<Indicator> _Indicators;
        /// <summary>
        /// Obsahuje true, pokud v této sadě indikátorů je alespoň jeden platný indikátor, který se má vykreslit
        /// </summary>
        public bool HasIndicators { get { return _Indicators.Any(i => i.IsValid); } }
        /// <summary>
        /// Obsahuje počet všech indikátorů
        /// </summary>
        public int Count { get { return _Indicators.Count; } }
        /// <summary>
        /// Pole indikátorů
        /// </summary>
        public Indicator[] Indicators { get { return _Indicators.ToArray(); } }
        /// <summary>
        /// Přidat další indikátor.
        /// Po změnách indikátorů je nutno vyvolat <see cref="Refresh()"/>, jinak budou vykresleny "až tam uživatel najede myší".
        /// </summary>
        /// <param name="values"></param>
        /// <param name="alignment"></param>
        /// <param name="color"></param>
        public void AddIndicator(Int32Range values, ScrollBarIndicatorType alignment, Color color)
        {
            if ((values?.Size ?? 0) > 0)
                _Indicators.Add(new Indicator(values, alignment, color));
        }
        /// <summary>
        /// Smaže pole indikátorů.
        /// Po změnách indikátorů je nutno vyvolat <see cref="Refresh()"/>, jinak budou vykresleny "až tam uživatel najede myší".
        /// </summary>
        public void Clear()
        {
            _Indicators.Clear();
        }
        /// <summary>
        /// Odstraní indikátory vyhovující danému filtru.
        /// Po změnách indikátorů je nutno vyvolat <see cref="Refresh()"/>, jinak budou vykresleny "až tam uživatel najede myší".
        /// </summary>
        public void Remove(Predicate<Indicator> filter)
        {
            _Indicators.Remove(filter);
        }
        /// <summary>
        /// Metoda zajistí překreslení zadaných indikátorů.
        /// Je nutno volat po změně indikátorů (metody: <see cref="Clear()"/>, <see cref="AddIndicator(Int32Range, ScrollBarIndicatorType, Color)"/>, <see cref="Remove(Predicate{Indicator})"/>...
        /// </summary>
        public void Refresh()
        {
            _Owner.Refresh();
        }
        /// <summary>
        /// Průhlednost zadané barvy indikátoru při vykreslení mimo thumb (=přímo viditelná).
        /// Výchozí hodnota = 200, maximum = 255 (plná barva přes thumb, nehezké), minimum = 20, rozumné minimum = 140;
        /// </summary>
        public int ColorAlphaArea { get { return _ColorAlphaArea; } set { _ColorAlphaArea = value.Align(20, 255); } }
        private int _ColorAlphaArea;
        /// <summary>
        /// Průhlednost zadané barvy indikátoru při vykreslení do prostoru thumbu (=lehce překrytá thumbem).
        /// Výchozí hodnota = 90, maximum = 255 (plná barva přes thumb, nehezké), minimum = 20, rozumné minimum = 50;
        /// </summary>
        public int ColorAlphaThumb { get { return _ColorAlphaThumb; } set { _ColorAlphaThumb = value.Align(20, 255); } }
        private int _ColorAlphaThumb;
        /// <summary>
        /// Síla efektu 3D pro prvky, které mají vlastnost <see cref="ScrollBarIndicatorType.InnerGradientEffect"/> nebo <see cref="ScrollBarIndicatorType.OutsideGradientEffect"/>.
        /// Hodnota 0 = plochý prvek (to se ale nemusí nastavovat Gardient), hodnota 1 je maximum (příšerně kulatý prvek), defaultní = 0.25f.
        /// </summary>
        public float Effect3DRatio { get { return _Effect3DRatio; } set { _Effect3DRatio = value.Align(0f, 1f); } }
        private float _Effect3DRatio;
        #endregion
        #region Kreslení
        /// <summary>
        /// Vykreslí svoje indikátory
        /// </summary>
        /// <param name="args"></param>
        internal void PaintIndicators(ScrollBarInfoArgs args)
        {
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    _PaintIndicatorsHorizontal(args);
                    break;
                case Orientation.Vertical:
                    _PaintIndicatorsVertical(args);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí svoje indikátory - Horizontal
        /// </summary>
        /// <param name="args"></param>
        private void _PaintIndicatorsHorizontal(ScrollBarInfoArgs args)
        {
            // Rozsah viditelných pixelů i překrytí prvkem Thumb - v souřadnici Y:
            var areaBegin = args.DecButtonBounds.Right;
            var thumbBegin = args.ThumbButtonBounds.X;
            var thumbEnd = args.ThumbButtonBounds.Right;
            var areaEnd = args.IncButtonBounds.X;
            var areaBefore = new Int32Range(areaBegin, thumbBegin);
            var areaThumb = new Int32Range(thumbBegin, thumbEnd);
            var areaAfter = new Int32Range(thumbEnd, areaEnd);

            // Cache pro souřadnice a efekty, pro typy reálně použité v indikátorech:
            var sizeCache = new Dictionary<ScrollBarIndicatorType, Tuple<Int32Range, Gradient3DEffectType>>();

            // Přepočtová funkce z value (X) na visual (Y) hodnoty:
            var function = Algebra.GetLinearEquation(args.ViewInfo.Minimum, areaBegin, args.ViewInfo.Maximum, areaEnd);
            foreach (var indicator in _Indicators)
            {
                // Pixely na výšku:
                Int32Range sizeV = _GetVSize(indicator.Alignment, args.IncButtonBounds.Y, args.IncButtonBounds.Bottom, sizeCache, out var effect);
                int vBegin = sizeV.Begin;
                int vEnd = sizeV.End;

                Int32Range sizeL = _GetVisualRangeIndicator(indicator.Values, function, areaBegin, areaEnd); // Celý viditelný rozsah aktuálního intervalu na šířku

                // Před thumbem:
                Int32Range partBefore = Int32Range.Intersect(areaBefore, sizeL);
                if (partBefore != null && partBefore.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(partBefore.Begin, vBegin, partBefore.End, vEnd), effect);

                // Over thumb:
                Int32Range partThumb = Int32Range.Intersect(areaThumb, sizeL);
                if (partThumb != null && partThumb.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaThumb, Rectangle.FromLTRB(partThumb.Begin, vBegin, partThumb.End, vEnd), effect);

                // Pod thumbem:
                Int32Range partAfter = Int32Range.Intersect(areaAfter, sizeL);
                if (partAfter != null && partAfter.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(partAfter.Begin, vBegin, partAfter.End, vEnd), effect);
            }
        }
        /// <summary>
        /// Vykreslí svoje indikátory - Vertical
        /// </summary>
        /// <param name="args"></param>
        private void _PaintIndicatorsVertical(ScrollBarInfoArgs args)
        {
            // Rozsah viditelných pixelů i překrytí prvkem Thumb - v souřadnici Y:
            var areaBegin = args.DecButtonBounds.Bottom;
            var thumbBegin = args.ThumbButtonBounds.Y;
            var thumbEnd = args.ThumbButtonBounds.Bottom;
            var areaEnd = args.IncButtonBounds.Y;
            var areaBefore = new Int32Range(areaBegin, thumbBegin);
            var areaThumb = new Int32Range(thumbBegin, thumbEnd);
            var areaAfter = new Int32Range(thumbEnd, areaEnd);

            // Cache pro souřadnice a efekty, pro typy reálně použité v indikátorech:
            var sizeCache = new Dictionary<ScrollBarIndicatorType, Tuple<Int32Range, Gradient3DEffectType>>();

            // Přepočtová funkce z value (X) na visual (Y) hodnoty:
            var function = Algebra.GetLinearEquation(args.ViewInfo.Minimum, areaBegin, args.ViewInfo.Maximum, areaEnd);
            foreach (var indicator in _Indicators)
            {
                // Pixely na šířku:
                Int32Range sizeV = _GetVSize(indicator.Alignment, args.IncButtonBounds.X, args.IncButtonBounds.Right, sizeCache, out var effect);
                int vBegin = sizeV.Begin;
                int vEnd = sizeV.End;

                Int32Range sizeL = _GetVisualRangeIndicator(indicator.Values, function, areaBegin, areaEnd); // Celý viditelný rozsah aktuálního intervalu na výšku

                // Před thumbem:
                Int32Range partBefore = Int32Range.Intersect(areaBefore, sizeL);
                if (partBefore != null && partBefore.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(vBegin, partBefore.Begin, vEnd, partBefore.End), effect);

                // Over thumb:
                Int32Range partThumb = Int32Range.Intersect(areaThumb, sizeL);
                if (partThumb != null && partThumb.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaThumb, Rectangle.FromLTRB(vBegin, partThumb.Begin, vEnd, partThumb.End), effect);

                // Pod thumbem:
                Int32Range partAfter = Int32Range.Intersect(areaAfter, sizeL);
                if (partAfter != null && partAfter.Size > 0)
                    _PaintIndicatorOne(args, indicator, _ColorAlphaArea, Rectangle.FromLTRB(vBegin, partAfter.Begin, vEnd, partAfter.End), effect);
            }
        }
        /// <summary>
        /// Metoda vrátí vizuální rozsah (odkud kam v pixelech zobrazen) je pro daný datový rozsah (od jaké do jaké hodnoty se nachází),
        /// s pomocí dané lineární rovnice.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="function"></param>
        /// <param name="areaBegin"></param>
        /// <param name="areaEnd"></param>
        /// <returns></returns>
        private Int32Range _GetVisualRangeIndicator(Int32Range values, Algebra.LinearEquation function, int areaBegin, int areaEnd)
        {
            int visualBegin = (int)Math.Round(function.GetY(values.Begin), 0);
            int visualEnd = (int)Math.Round(function.GetY(values.End), 0);

            // Korekce - chceme indikátor vidět i tehdy, když jeho exaktní velikost je 0 (nebo 1) pixel, tedy chceme nejméně 2 pixely azarovnané do viditelného rozmezí:
            int visualSize = visualEnd - visualBegin;
            if (visualSize <= 0)
            {
                visualBegin = (visualBegin - 1).Align(areaBegin, areaEnd - 2);
                visualEnd = visualBegin + 2;
            }
            else if (visualSize == 1)
            {
                visualBegin = visualBegin.Align(areaBegin, areaEnd - 2);
                visualEnd = visualBegin + 2;
            }
            return new Int32Range(visualBegin, visualEnd);
        }
        /// <summary>
        /// Vrátí rozsah pozice indikátoru dle jeho šířky
        /// </summary>
        /// <param name="type"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="effect"></param>
        /// <param name="sizeCache"></param>
        /// <returns></returns>
        private Int32Range _GetVSize(ScrollBarIndicatorType type, int begin, int end,
            Dictionary<ScrollBarIndicatorType, Tuple<Int32Range, Gradient3DEffectType>> sizeCache,
            out Gradient3DEffectType effect)
        {
            Tuple<Int32Range, Gradient3DEffectType> info;
            if (!sizeCache.TryGetValue(type, out info))
            {
                int vb = begin + 2;
                int ve = end - 2;
                int vs = ve - vb;

                int start = vb;
                int size = vs;
                if (!type.HasFlag(ScrollBarIndicatorType.FullSize))
                {
                    if (type.HasFlag(ScrollBarIndicatorType.BigSize))
                        size = 20 * vs / 30;
                    else if (type.HasFlag(ScrollBarIndicatorType.HalfSize))
                        size = 15 * vs / 30;
                    else if (type.HasFlag(ScrollBarIndicatorType.ThirdSize))
                        size = 10 * vs / 30;

                    if (type.HasFlag(ScrollBarIndicatorType.Center))
                        start = vb + (vs - size) / 2;
                    else if (type.HasFlag(ScrollBarIndicatorType.Far))
                        start = ve - size;
                }
                Gradient3DEffectType ef = (type.HasFlag(ScrollBarIndicatorType.InnerGradientEffect) ? Gradient3DEffectType.Inset :
                                       (type.HasFlag(ScrollBarIndicatorType.OutsideGradientEffect) ? Gradient3DEffectType.Outward : Gradient3DEffectType.None));

                info = new Tuple<Int32Range, Gradient3DEffectType>(new Int32Range(start, start + size), ef);
                sizeCache.Add(type, info);
            }
            effect = info.Item2;
            return info.Item1;
        }
        /// <summary>
        /// Vykreslí jeden daný indikátor
        /// </summary>
        /// <param name="args"></param>
        /// <param name="indicator"></param>
        /// <param name="alpha"></param>
        /// <param name="bounds"></param>
        /// <param name="effect"></param>
        private void _PaintIndicatorOne(ScrollBarInfoArgs args, Indicator indicator, int alpha, Rectangle bounds, Gradient3DEffectType effect)
        {
            if (indicator.Color.A < 255) alpha = alpha * indicator.Color.A / 255;        // Sloučení Alpha kanálu z dodané barvy + explicitní Alpha definovaná indikátorem
            Color color = Color.FromArgb(alpha, indicator.Color);
            switch (effect)
            {
                case Gradient3DEffectType.Inset:
                case Gradient3DEffectType.Outward:
                    float effectRatio = (effect == Gradient3DEffectType.Inset ? -_Effect3DRatio : _Effect3DRatio);
                    using (var brush = DxComponent.PaintCreateBrushForGradient(bounds, color, _Orientation, effectRatio))
                        args.Graphics.FillRectangle(brush, bounds);
                    break;
                default:
                    args.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color), bounds);
                    break;
            }
        }
        #endregion
        #region class Indicator = Třída jednoho konkrétního indikátoru = značky
        /// <summary>
        /// Třída jednoho konkrétního indikátoru = značky
        /// </summary>
        public class Indicator
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="values"></param>
            /// <param name="alignment"></param>
            /// <param name="color"></param>
            public Indicator(Int32Range values, ScrollBarIndicatorType alignment, Color color)
            {
                Values = values;
                Alignment = alignment;
                Color = color;
            }
            /// <summary>
            /// Rozmezí indikátoru v datech
            /// </summary>
            public Int32Range Values { get; private set; }
            /// <summary>
            /// Umístění indikátoru na ScrollBaru
            /// </summary>
            public ScrollBarIndicatorType Alignment { get; private set; }
            /// <summary>
            /// Barva indikátoru, smí obsahovat Alpha kanál (průhlednost)
            /// </summary>
            public Color Color { get; private set; }
            /// <summary>
            /// Tento indikátor je platný? Tzn. jeho <see cref="Values"/> má kladnou délku
            /// </summary>
            public bool IsValid { get { return ((Values?.Size ?? 0) > 0); } }
        }
        #endregion
    }
    #region enum ScrollBarIndicatorType
    /// <summary>
    /// Typy indikátorů na ScrollBaru, používají se v <see cref="ScrollBarIndicators"/>
    /// </summary>
    [Flags]
    public enum ScrollBarIndicatorType
    {
        /// <summary>Žádný</summary>
        None = 0,

        /// <summary>U vnitřního okraje (vlevo / nahoře)</summary>
        Near = 0x0001,
        /// <summary>Uprostřed</summary>
        Center = 0x0002,
        /// <summary>U vzdálenějšího okraje (vpravo / dole)</summary>
        Far = 0x0004,
        /// <summary>Přes plnou velikost (pak není třeba určovat <see cref="Near"/> / <see cref="Center"/> / <see cref="Far"/>)</summary>
        FullSize = 0x0010,
        /// <summary>Poloviční velikost ScrollBaru</summary>
        HalfSize = 0x0020,
        /// <summary>Třetina ScrollBaru</summary>
        ThirdSize = 0x0040,
        /// <summary>Dvě třetiny ScrollBaru</summary>
        BigSize = 0x0080,
        /// <summary>Gradient "dovnitř" = "dolů"</summary>
        InnerGradientEffect = 0x0100,
        /// <summary>Gradient "vně" = "nahoru"</summary>
        OutsideGradientEffect = 0x0200,

        /// <summary>Poloviční velikost, vnitřní okraj</summary>
        HalfNear = HalfSize | Near,
        /// <summary>Poloviční velikost, uprostřed</summary>
        HalfCenter = HalfSize | Center,
        /// <summary>Poloviční velikost, vnější okraj</summary>
        HalfFar = HalfSize | Far,
        /// <summary>Třetinová velikost, vnitřní okraj</summary>
        ThirdNear = ThirdSize | Near,
        /// <summary>Třetinová velikost, uprostřed</summary>
        ThirdCenter = ThirdSize | Center,
        /// <summary>Třetinová velikost, vnější okraj</summary>
        ThirdFar = ThirdSize | Far,
        /// <summary>Dvoutřetinová velikost, vnitřní okraj</summary>
        BigNear = BigSize | Near,
        /// <summary>Dvoutřetinová velikost, uprostřed</summary>
        BigCenter = BigSize | Center,
        /// <summary>Dvoutřetinová velikost, vnější okraj</summary>
        BigFar = BigSize | Far,

        /// <summary>Defaultní = Dvoutřetinová velikost, uprostřed</summary>
        Default = BigCenter
    }
    #endregion
    #endregion
}
