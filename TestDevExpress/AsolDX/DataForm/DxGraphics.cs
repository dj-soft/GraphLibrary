// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using DevExpress.Utils.Drawing;
using DevExpress.Utils.Extensions;
using DevExpress.XtraCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxVirtualPanel : řeší hostování a zobrazení datového panelu ve svém prostoru plus řízení Scrollbarů a Virtual prostoru
    /// <summary>
    /// <see cref="DxVirtualPanel"/> : řeší hostování a zobrazení datového panelu ve svém prostoru plus automatické zobrazení Scrollbarů a posouvání datového obsahu.
    /// </summary>
    public class DxVirtualPanel : DxGraphicsPanel, IDisposable
    {
        #region Info ... Typy souřadnic a přepočty
        /*

        Úkolem virtuálního panelu DxVirtualPanel je zobrazovat určitý obsah v "uživatelském" panelu ContentPanel a umožnit virtuální posouvání jeho obsahu pomocí Scrollbarů.
        Obsah tohoto panelu je vykreslovaný graficky (proto panel je typu DxBufferedGraphicPanel) a konkrétní prvek je vykreslen na určitou pozici v panelu.
        Konkrétní prvek je deklarován na určitou Design souřadnici DesignBounds, která je v designových pixelech a je relativně k počátku 0/0 celkového prostoru.
        Pokud souhrn velikostí jednotlivých prvků je větší než disponibilní reálný prostor, pak se zobrazí Scrollbar[y] a dovolí se posouvání obsahu uvnitř controlu.

        Další funkcí DxVirtualPanel je poskytování Zoomu a tedy možnost zvětšení/zmenšení obsahu panelu.
        Zoom se interně skládá ze dvou hodnot: Zoom systému (daný DxComponent.Zoom) krát Zoom panelu (DxVirtualPanel.Zoom), jehož výsledkem je DxVirtualPanel.CurrentZoom.
        Tento CurrentZoom násobí pozice a rozměry dané Designem a určuje velikost písma standardních prvků.

        Definice a přepočty souřadnic jsou následovné:
        - DesignBounds     ... souřadnice definovaná designem, vztahuje se k "normalizovaným" pixelům (při Zoomu 1.00). Např. výška textboxu je 20 dpx (DesignPixel)
        - VirtualBounds    ... souřadnice přepočtená aktuálním Zoomem (CurrentZoom), v koordinátech celé definice, tedy k počátku 0/0, nezahrnuje tedy posun daný Scrollbary
        - ControlBounds    ... souřadnice fyzická na aktuálním ContentPanel, odpovídá např. souřadnici myši, zahrnuje posun Scrollbarů.

        Základní souřadnice je DesignBounds, ze které je vypočtena sumární velikost všech prvků do DesignSize.
        Hodnota DesignSize je přepočtena do VirtualSize pomocí CurrentZoom a odtud pak slouží jako výchozí hodnota pro ScrollBary.
        Fyzická souřadnice ControlBounds je tedy vypočtena jako: (DesignBounds * CurrentZoom) [=VirtualBounds] - ScrollBar (kladná hodnota ScrollBaru se odečte od VirtualBounds)

        Prvek Root a prvek Child:
        Root prvek:  je umístěn přímo v DxInteractivePanel. Nejčastěji to je nějaký druh Containeru = obsahuje svoje Child prvky.
                      Jeho Design souřadnice se vztahují k základnímu dokumentu
        Child prvek: je umístěn ve svém Parent Containeru, v jeho ChildBounds. 
                      Parent Container má svoje vlastní souřadnice, ve kterých kreslí svoji režii, a uvnitř svého prostoru poskytuje další prostor pro svoje Childs.

        Postup přepočtů:
        Design => Control :    Control = (Design * Zoom) - Scrollbar         // Scrollbar obsahuje kladnou hodnotu, určuje který virtuální pixel je zobrazen na fyzické pozici 0;
        Control => Design :    Design  = (Control + Scrollbar) / Zoom        //  Zoom obsahuje 1.00 = 100%, obsahuje 1.50 při zvětšení na 150%, atd
        
        Přepočty Rectangle:    Vždy se určí souřadnice bodu TopLeft a souřadnice bodu BottomRight a z nich se vytvoří výsledný Rectangle.
                               Nikdy se nepočítá samostatně Size, která by se připočetla k TopLeft! 
                               Důvod? Tímto postupem budu mít správně určenou souřadnici Right,Bottom pro různé prvky, 
                               které tímto postupem budou správně zarovnány doprava doprava/dolů. I při zaokrouhlování double => int.
                               Opačný postup by sice vedl k přesné šířce, ale tu v rámci 1px nikdo neocení...

        */
        #endregion
        #region Konstruktor a Dispose a handlery vnitřních událostí
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxVirtualPanel()
            : base()
        {
            this._InitVirtualDimensions();
        }
        /// <summary>
        /// Proběhne při změně velikosti
        /// </summary>
        protected override void OnCurrentClientSizeChanged()
        {
            this._AcceptControlSize();
        }
        /// <summary>
        /// Před vykreslením provedeme kontrolu validity <see cref="_CheckValidityDesignSize"/>
        /// </summary>
        protected override void OnBeforePaint()
        {
            base.OnBeforePaint();
            _CheckValidityDesignSize();
        }
        #endregion
        #region ContentPanel: Vlastní grafický control umístěný mezi Scrollbary, v něm se zobrazuje grafický obsah
        /// <summary>
        /// Vlastní grafický control umístěný mezi Scrollbary, v něm se zobrazuje grafický obsah
        /// </summary>
        public DxBufferedGraphicPanel ContentPanel
        {
            get { return __ContentPanel; }
            set
            {
                var oldPanel = __ContentPanel;
                if (oldPanel != null)
                {
                    _DetachPanel(oldPanel);
                    this.Controls.Remove(oldPanel);
                    oldPanel = null;
                    __ContentPanel = null;
                }

                var newPanel = value;
                if (newPanel != null)
                {
                    _AttachPanel(newPanel);
                    this.Controls.Add(newPanel);
                    __ContentPanel = newPanel;
                    __ContentPanelBounds = null;
                    _SetContentBounds();
                }
            }
        }
        /// <summary>
        /// Content panel přetypovaný na <see cref="DxInteractivePanel"/>. Do něj pak lze posílat informace o změnách virtuální pozice.
        /// </summary>
        private DxInteractivePanel InteractiveContentPanel { get { return __ContentPanel as DxInteractivePanel; } }
        private void _AttachPanel(DxBufferedGraphicPanel contentPanel)
        {
            if (contentPanel != null)
            {
                contentPanel.VirtualPanel = this;
            }
        }
        private void _DetachPanel(DxBufferedGraphicPanel contentPanel)
        {
            if (contentPanel != null)
            {
                contentPanel.VirtualPanel = null;
            }
        }
        private DxBufferedGraphicPanel __ContentPanel;
        /// <summary>
        /// Nastaví fyzické souřadnice panelu <see cref="ContentPanel"/> = kde je zobrazen.
        /// </summary>
        /// <param name="contentBounds"></param>
        private void _SetContentPanelBounds(Rectangle contentBounds)
        {
            if (!__ContentPanelBounds.HasValue || (__ContentPanelBounds.HasValue && __ContentPanelBounds.Value != contentBounds))
            {
                __ContentPanelBounds = contentBounds;
                var contentPanel = this.ContentPanel;
                if (contentPanel != null)
                {
                    contentPanel.Bounds = contentBounds;
                    contentPanel.Draw();
                }
            }
        }
        /// <summary>
        /// Fyzické souřadnice panelu <see cref="ContentPanel"/> = kde je zobrazen. Nesouvisí s jeho virtuální velikostí (=jeho obsah).
        /// </summary>
        private Rectangle? __ContentPanelBounds;
        /// <summary>
        /// Určí souřadnice pro <see cref="ContentPanel"/> podle aktuální velikosti celého controlu a podle přítomnosti Scrollbarů,
        /// a <see cref="ContentPanel"/> do těchto souřadnic umístí.
        /// </summary>
        private void _SetContentBounds()
        {
            var clientBounds = this.CurrentClientBounds;

            int clientRight = clientBounds.Right;
            int clientBottom = clientBounds.Bottom;
            int scrollWidth = 0;
            int scrollHeight = 0;
            if (_IsInVirtualMode)
            {
                if (__DimensionY.UseScrollbar && __DimensionY.ScrollBounds.HasValue) clientRight = __DimensionY.ScrollBounds.Value.Left;
                if (__DimensionX.UseScrollbar && __DimensionX.ScrollBounds.HasValue) clientBottom = __DimensionX.ScrollBounds.Value.Top;
                scrollWidth = __DimensionY.ScrollbarSize;
                scrollHeight = __DimensionX.ScrollbarSize;
            }
            // Umístění ContentPanelu:
            var contentBounds = Rectangle.FromLTRB(clientBounds.Left, clientBounds.Top, clientRight, clientBottom);
            _SetContentPanelBounds(contentBounds);

            // Celý viditelný prostor DesignSize:
            double rZoom = 1d / this.CurrentZoom;
            Size virtualSize = new Size(clientBounds.Width - scrollWidth, clientBounds.Height - scrollHeight);
            Size designSize = virtualSize.ZoomByRatio(rZoom);
            ContentPanelDesignSize = designSize;

            // Kontrola změn VisibleDesignBounds:
            _CheckVisibleDesignBoundsChanged(true);
        }
        /// <summary>
        /// Designový prostor v panelu <see cref="ContentPanel"/> když budou zobrazeny oba Scrollbary.
        /// Tedy jde o vnitřní prostor zdejšího <see cref="DxVirtualPanel"/>, zmenšený o prostor Scrollbarů (i když by aktuálně nebyly zobrazeny) 
        /// a přepočtený pomocí <see cref="CurrentZoom"/> na designové pixely.
        /// Slouží k automatickému určování layoutu podle disponibilního prostoru.
        /// </summary>
        public Size ContentPanelDesignSize
        { 
            get { return __ContentPanelDesignSize ?? this.ClientSize; }
            private set
            {
                var oldValue = __ContentPanelDesignSize;
                __ContentPanelDesignSize = value;
                var newValue = __ContentPanelDesignSize;
                if (oldValue != newValue) { OnContentPanelDesignSizeChanged(); }
            }
        }
        /// <summary>
        /// Po změně hodnoty <see cref="DxVirtualPanel.ContentPanelDesignSize"/>
        /// </summary>
        protected virtual void OnContentPanelDesignSizeChanged() { }
        /// <summary>
        /// Designový prostor v panelu <see cref="ContentPanel"/> když budou zobrazeny oba Scrollbary. Úložiště hodnoty.
        /// </summary>
        private Size? __ContentPanelDesignSize;
        #endregion
        #region ContentDesignSize a ContentVirtualSize: velikost zobrazovaného obsahu
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech.
        /// Pokud je použit <see cref="ContentPanel"/>, který definuje svoji virtuální velikost <see cref="DxBufferedGraphicPanel.ContentDesignSize"/> (=není null), pak je zde čtena jeho hodnota, a setování hodnoty je ignorováno.
        /// Výchozí je null = control zobrazuje to, co je vidět, a nikdy nepoužívá Scrollbary.
        /// Lze setovat hodnotu = celková velikost zobrazených dat, pak se aktivuje virtuální režim se zobrazením výřezu a s možností Scrollbarů.
        /// </summary>
        public virtual Size? ContentDesignSize
        {
            get
            {
                Size? designSize = this.ContentPanel?.ContentDesignSize;
                if (!designSize.HasValue)
                    designSize = __ContentDesignSize;
                return designSize;
            }
            set
            {
                __ContentDesignSize = value;
                __ContentVirtualSize = null;
                _RefreshInnerLayout();
            }
        }
        /// <summary>
        /// Velikost vnitřního obsahu explicitně zadaná v virtuálních pixelech (násobené Zoom) = platí jen když není přítomen <see cref="ContentPanel"/> anebo ten nemá svoji VirtualSize.
        /// </summary>
        protected virtual Size? ContentVirtualSize
        {
            get
            {
                var designSize = ContentDesignSize;
                if (!designSize.HasValue) return null;               // Pokud nejsem ve virtuálním režimu (ten má definovanou velikost obsahu), pak nemám ani potřebu přepočítávat ...

                if (!__ContentVirtualSize.HasValue)
                    __ContentVirtualSize = designSize.Value.ZoomByRatio(this.CurrentZoom);

                return __ContentVirtualSize.Value;
            }
        }
        /// <summary>
        /// Metoda zajistí, že velikost <see cref="ContentDesignSize"/> bude platná (bude odpovídat souhrnu velikosti prvků).
        /// Metoda je volána před každým Draw tohoto objektu.
        /// </summary>
        protected virtual void ContentDesignSizeCheckValidity(bool force = false)
        {

        }
        /// <summary>
        /// Invaliduje celkovou velikost <see cref="ContentDesignSize"/> a navazující <see cref="ContentVirtualSize"/>.
        /// Provádí se po změně řádků nebo definice designu.
        /// Volitelně provede i <see cref="RefreshInnerLayout"/>
        /// </summary>
        /// <param name="refreshLayout">Vyvolat poté metodu <see cref="RefreshInnerLayout"/></param>
        protected virtual void DesignSizeInvalidate(bool refreshLayout)
        {
            __ContentDesignSize = null;
            __ContentVirtualSize = null;

            if (refreshLayout)
                _RefreshInnerLayout();
        }
        /// <summary>
        /// Kontrola platnosti souřadnic <see cref="ContentVirtualSize"/> a případný přepočet layoutu <see cref="_RefreshInnerLayout()"/>.
        /// Volá se před vykreslením.
        /// </summary>
        private void _CheckValidityDesignSize()
        {
            var validatedVirtualSize = __ValidatedVirtualSize;
            var currentVirtualSize = this.ContentVirtualSize;        // Tady proběhne validace hodnot i ve třídě potomka
            if (currentVirtualSize != validatedVirtualSize) _RefreshInnerLayout();
        }
        /// <summary>
        /// Metoda je volána v situaci, kdy se určuje vnitřní layout a ScrollBary, a budeme potřebovat platnou hodnotu <see cref="ContentVirtualSize"/>.
        /// Vnější aplikace může reagovat On-Demand a tuto hodnotu validovat (např. pomocí eventhandleru )
        /// </summary>
        private void _ValidateDesignSize()
        {
            OnValidateDesignSize();
            ValidateDesignSize?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se na začátku procesu určení vnitřního layoutu podle hodnoty <see cref="ContentDesignSize"/>. 
        /// Toto je vhodná situace, kdy volající se může ujistit, že tuto velikost nastavil na platnou hodnotu.
        /// </summary>
        protected virtual void OnValidateDesignSize() { }
        /// <summary>
        /// Volá se na začátku procesu určení vnitřního layoutu podle hodnoty <see cref="ContentDesignSize"/>.
        /// Toto je vhodná situace, kdy volající se může ujistit, že tuto velikost nastavil na platnou hodnotu.
        /// </summary>
        public event EventHandler ValidateDesignSize;
        /// <summary>
        /// Velikost vnitřního obsahu explicitně zadaná v designových pixelech = platí jen když není přítomen <see cref="ContentPanel"/> anebo ten nemá svoji VirtualSize.
        /// Zadat lze kdykoliv, ale ne vždy bude zadaná hodnota použita.
        /// </summary>
        private Size? __ContentDesignSize;
        /// <summary>
        /// Velikost vnitřního obsahu explicitně zadaná v virtuálních pixelech (násobené Zoom) = platí jen když není přítomen <see cref="ContentPanel"/> anebo ten nemá svoji VirtualSize.
        /// </summary>
        private Size? __ContentVirtualSize;
        /// <summary>
        /// Hodnota <see cref="ContentVirtualSize"/>, která byla použita při určení layoutu v metodě <see cref="_RefreshInnerLayout"/>.
        /// Před dalším kreslením se porovná s aktuální, a případně se znovu určí Layout, viz metoda <see cref="_CheckValidityDesignSize()"/>.
        /// </summary>
        private Size? __ValidatedVirtualSize;
        /// <summary>
        /// Obsahuje true, pokud objekt obsahuje platnou hodnotu <see cref="ContentVirtualSize"/> a mohl by tedy být ve virtuálním modu.
        /// </summary>
        private bool _HasContentSize { get { var virtualSize = this.ContentVirtualSize; return (virtualSize.HasValue && virtualSize.Value.Width > 0 && virtualSize.Value.Height > 0); } }
        #endregion
        #region VisibleDesignBegin a VisibleDesignBounds : aktuální pozice počátku a zobrazované souřadnice v rámci DesignSize
        /// <summary>
        /// Souřadnice počátku viditelného prostoru v designových pixelech. Lze setovat.
        /// Pokud vnější aplikace má určitý prvek na designovém pixelu např. 150,260; a nastaví <see cref="VisibleDesignBegin"/> = 0,260; 
        /// pak tento virtuální panel zajistí, že daný prvek bude zobrazen na horním okraji viditelného prostoru, a osa X bude na počátku (vlevo).
        /// Virtuální panel interně řeší i aktuální Zoom (proto zdejší property je typu "Design" = pracuje s designovými pixely.
        /// </summary>
        public Point VisibleDesignBegin
        {
            get { return _VisibleDesignBegin; }
            set
            {
                if (!_VirtualInitialized || !_HasContentSize) return;

                // Konverze Design => Virtual:    Virtual = Design * Zoom
                Point virtualBegin = value.ZoomByRatio(this.CurrentZoom);
                __DimensionX.VirtualBegin = virtualBegin.X;
                __DimensionY.VirtualBegin = virtualBegin.Y;
                _VirtualBeginInvalidate();
                _RefreshInnerLayout();
            }
        }
        /// <summary>
        /// Souřadnice počátku viditelného prostoru v designových pixelech.
        /// Validní a cachovaná hodnota.
        /// </summary>
        private Point _VisibleDesignBegin
        {
            get
            {
                if (!_VirtualInitialized || !_HasContentSize) return Point.Empty;

                if (!__VisibleDesignBegin.HasValue)
                {
                    // Konverze Virtual => Design:    Design = Virtual / Zoom
                    var rZoom = 1d / this.CurrentZoom;
                    Point virtualBegin = new Point(__DimensionX.VirtualBegin, __DimensionY.VirtualBegin);
                    Point designBegin = virtualBegin.ZoomByRatio(rZoom);
                    __VisibleDesignBegin = designBegin;
                    DxComponent.LogAddLine(LogActivityKind.VirtualChanges, $"VirtualPanel: VisibleDesignBegin validated to {designBegin}");
                }
                return __VisibleDesignBegin.Value;
            }
        }
        /// <summary>
        /// Souřadnice počátku viditelného prostoru v designových pixelech. Úložiště cachované hodnoty.
        /// </summary>
        private Point? __VisibleDesignBegin;
        /// <summary>
        /// Zajistí znovunapočtení hodnoty Počátek virtuálního prostoru, určený ze Scrollbarů
        /// </summary>
        private void _VirtualBeginInvalidate()
        {
            __VisibleDesignBegin = null;
            DxComponent.LogAddLine(LogActivityKind.VirtualChanges, $"VirtualPanel: VisibleDesignBegin invalidated");
        }
        /// <summary>
        /// Oblast designového prostoru, která je aktuálně zobrazena. Je přepočtena ze souřadnic Controlu na souřadnice Designové.
        /// </summary>
        public Rectangle VisibleDesignBounds { get { _CheckVisibleDesignBoundsChanged(false); return __VisibleDesignBounds.Value; } } private Rectangle? __VisibleDesignBounds;
        /// <summary>
        /// Metoda prověří, jaká je aktuální hodnota <see cref="VisibleDesignBounds"/>, a pokud je jiná naž posledně, pak vyvolá událost o změně.
        /// Metoda se volá po pohybech na Scrollbarech a po změnách velikosti controlu
        /// </summary>
        /// <param name="force">Povinně vypočítat? true po změnách, false v VisibleDesignBounds.get</param>
        private void _CheckVisibleDesignBoundsChanged(bool force)
        {
            if (force || !__VisibleDesignBounds.HasValue)
            {
                Size controlSize = __ContentPanelBounds?.Size ?? this.ClientSize;
                Rectangle controlBounds = new Rectangle(0, 0, controlSize.Width, controlSize.Height);
                Rectangle designBounds = GetDesignBounds(controlBounds);

                if (!(__VisibleDesignBounds.HasValue && __VisibleDesignBounds.Value == designBounds))
                {
                    __VisibleDesignBounds = designBounds;
                    DxComponent.LogAddLine(LogActivityKind.VirtualChanges, $"VirtualPanel: VisibleDesignBounds validated to {designBounds}");

                    InteractiveContentPanel?.VirtualCoordinatesChanged();
                    OnVisibleDesignBoundsChanged();
                    VisibleDesignBoundsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Metoda, která proběhne po změně hodnoty v <see cref="VisibleDesignBounds"/>
        /// </summary>
        protected virtual void OnVisibleDesignBoundsChanged() { }
        /// <summary>
        /// Událost, která proběhne po změně hodnoty v <see cref="VisibleDesignBounds"/>
        /// </summary>
        public event EventHandler VisibleDesignBoundsChanged;
        #endregion
        #region CurrentZoom = Zoom a DPI: hlídání změny a její obsluha
        /// <summary>
        /// Aktuálně platný Zoom = součin systémového <see cref="DxComponent.Zoom"/> * lokálního <see cref="DataFormZoom"/> Zoomu * koeficient DeviceDPI
        /// </summary>
        public double CurrentZoom { get { return (double)DxComponent.Zoom * this.DataFormZoom * this._DeviceDpiZoom; } }
        /// <summary>
        /// Zoom daný tímto konkrétním DataFormem. Lze měnit kolečkem myši. Rozsah hodnot 0.25 až 4.00
        /// </summary>
        public double DataFormZoom
        {
            get
            {
                return __DataFormZoom;
            }
            set
            {
                var oldZoom = __DataFormZoom;
                __DataFormZoom = Math.Round((value < 0.25f ? 0.25f : (value > 4.0f ? 4.0f : value)),3);
                var newZoom = __DataFormZoom;
                if (newZoom != oldZoom)
                    _RunInvalidateZoom();
            }
        }
        /// <summary>
        /// Obsahuje Zoom daný <see cref="Control.DeviceDpi"/> vůči designovému DPI = 96. 
        /// Tedy pokud operační systém nastavil 150%, což se promítlo do <see cref="Control.DeviceDpi"/> = 144, pak zde je 1.5d
        /// </summary>
        private double _DeviceDpiZoom { get { return ((double)this.DeviceDpi / _DesignDpi); } }
        /// <summary>
        /// Designové DPI = 96
        /// </summary>
        private const double _DesignDpi = 96d;
        /// <summary>
        /// Po změně Zoomu v systému
        /// </summary>
        protected override void OnZoomChanged()
        {
            base.OnZoomChanged();
            this._RunInvalidateZoom();
        }
        /// <summary>
        /// Po změně DPI v systému
        /// </summary>
        protected override void OnCurrentDpiChanged()
        {
            base.OnCurrentDpiChanged();
            this._RunInvalidateZoom();
        }
        /// <summary>
        /// Invaliduje data po změně Zoomu systémového i lokálního. Volá event. Refreshuje vnitřní layout.
        /// </summary>
        protected virtual void _RunInvalidateZoom()
        {
            __ContentVirtualSize = null;
            OnInvalidatedZoom();
            InvalidatedZoom?.Invoke(this, EventArgs.Empty);
            this.ContentPanel?.OnCurrentZoomChanged();
            _RefreshInnerLayout();
        }
        /// <summary>
        /// Proběhne po změně Zoomu za běhu aplikace. Může dojít k invalidaci cachovaných souřadnic prvků.
        /// </summary>
        protected virtual void OnInvalidatedZoom() { }
        /// <summary>
        /// Proběhne po změně Zoomu za běhu aplikace. Může dojít k invalidaci cachovaných souřadnic prvků.
        /// </summary>
        public event EventHandler InvalidatedZoom;
        /// <summary>
        /// Zoom daný tímto konkrétním DataFormem.
        /// </summary>
        private double __DataFormZoom;
        #endregion
        #region Přepočty souřadnic Design <=> Control
        /// <summary>
        /// Přepočte souřadnici bodu z designového pixelu (kde se bod nachází v designovém návrhu) do souřadnice v pixelových koordinátech na this controlu (aplikuje Zoom a posun daný Scrollbary)
        /// </summary>
        /// <param name="designPoint">Souřadnice bodu v design pixelech</param>
        /// <returns></returns>
        public Point GetControlPoint(Point designPoint)
        {
            if (!_IsInVirtualMode) return designPoint;

            // Design => Control :    Control = (Design * Zoom) - Scrollbar 
            double zoom = this.CurrentZoom;
            Point virtualPoint = designPoint.ZoomByRatio(zoom);
            Point controlPoint = virtualPoint.Sub(_VisibleDesignBegin);
            return controlPoint;
        }
        /// <summary>
        /// Přepočte souřadnici bodu v pixelových koordinátech na this controlu do souřadnice v designovém prostoru (aplikuje posun daný Scrollbary a Zoom)
        /// </summary>
        /// <param name="controlPoint">Souřadnice bodu fyzickém controlu</param>
        /// <returns></returns>
        public Point GetDesignPoint(Point controlPoint)
        {
            if (!_IsInVirtualMode) return controlPoint;

            // Control => Design :    Design  = (Control + Scrollbar) / Zoom
            double rZoom = 1d / this.CurrentZoom;
            Point virtualPoint = controlPoint.Add(_VisibleDesignBegin);
            Point designPoint = virtualPoint.ZoomByRatio(rZoom);
            return designPoint;
        }
        /// <summary>
        /// Přepočte souřadnici prostoru z designového pixelu (kde se bod nachází v designovém návrhu) do souřadnice v pixelových koordinátech na this controlu (aplikuje Zoom a posun daný Scrollbary)
        /// </summary>
        /// <param name="designBounds">Souřadnice prostoru v design pixelech</param>
        /// <returns></returns>
        public Rectangle GetControlBounds(Rectangle designBounds)
        {
            if (!_IsInVirtualMode) return designBounds;

            // Design => Control :    Control = (Design * Zoom) - Scrollbar 
            double zoom = this.CurrentZoom;
            var virtualBegin = _VisibleDesignBegin;
            Point virtualPointLT = designBounds.GetPoint(RectangleSide.TopLeft).Value.ZoomByRatio(zoom);
            Point virtualPointBR = designBounds.GetPoint(RectangleSide.BottomRight).Value.ZoomByRatio(zoom);

            Point controlPointLT = virtualPointLT.Sub(virtualBegin);
            Point controlPointBR = virtualPointBR.Sub(virtualBegin);

            return Rectangle.FromLTRB(controlPointLT.X, controlPointLT.Y, controlPointBR.X, controlPointBR.Y);
        }
        /// <summary>
        /// Přepočte souřadnici prostoru v pixelových koordinátech na this controlu do souřadnice v designovém prostoru (aplikuje posun daný Scrollbary a Zoom)
        /// </summary>
        /// <param name="controlBounds">Souřadnice prostoru fyzickém controlu</param>
        /// <returns></returns>
        public Rectangle GetDesignBounds(Rectangle controlBounds)
        {
            if (!_IsInVirtualMode) return controlBounds;

            // Control => Design :    Design  = (Control + Scrollbar) / Zoom
            var virtualBegin = _VisibleDesignBegin;
            double rZoom = 1d / this.CurrentZoom;
            Point virtualPointLT = controlBounds.GetPoint(RectangleSide.TopLeft).Value.Add(virtualBegin);
            Point virtualPointBR = controlBounds.GetPoint(RectangleSide.BottomRight).Value.Add(virtualBegin);

            Point designPointLT = virtualPointLT.ZoomByRatio(rZoom);
            Point designPointBR = virtualPointBR.ZoomByRatio(rZoom);

            return Rectangle.FromLTRB(designPointLT.X, designPointLT.Y, designPointBR.X, designPointBR.Y);
        }
        #endregion
        #region Scrollbary a VirtualDimensions, včetně výpočtu _RefreshInnerLayout() a eventhandlerů
        /// <summary>
        /// Metoda přepočítá velikost controlu a celkovou velikost dat, a určí potřebost Scrollbarů, scrollbary umístí na správná místa a zobrazí je.
        /// </summary>
        public virtual void RefreshInnerLayout()
        {
            _RefreshInnerLayout();
        }
        /// <summary>
        /// Povoluje se práce se Scrollbary, pokud bude zadána velikost obsahu <see cref="ContentVirtualSize"/>. Default = true.
        /// Pokud je false, a přitom je zadána veliksat obsahu <see cref="ContentVirtualSize"/>, pak jsme ve virtuálním režimu, 
        /// ale uživatel nevidí posuny prostoru (Scrollbary) a nemůže je ani interaktivně měnit. Posuny se pak řídí nastavením <see cref="VisibleDesignBegin"/>.
        /// </summary>
        public bool ScrollbarsEnabled { get { return __ScrollbarsEnabled; } set { __ScrollbarsEnabled = value; _RefreshInnerLayout(); } } private bool __ScrollbarsEnabled;
        /// <summary>
        /// Velikost zdejšího Scrollbaru, pokud bude zobrazen. Zde je tedy kladné číslo, i když scrollbar aktuálně není zobrazen.
        /// Pro dimenzi Y (svislá, řeší Y a Height) je zde Šířka svislého Scrollbaru.
        /// </summary>
        public int VerticalScrollBarWidth { get { return __DimensionY.ScrollbarSize; } }
        /// <summary>
        /// Velikost zdejšího Scrollbaru, pokud bude zobrazen. Zde je tedy kladné číslo, i když scrollbar aktuálně není zobrazen.
        /// Pro dimenzi X (vodorovná, řeší X a Width) je zde Výška vodorovného Scrollbaru.
        /// </summary>
        public int HorizontalScrollBarHeight { get { return __DimensionX.ScrollbarSize; } }
        /// <summary>
        /// Inicializuje data pro Virtuální souřadnice
        /// </summary>
        private void _InitVirtualDimensions()
        {
            this.MouseWheel += _MouseWheel;

            __ScrollbarsEnabled = true;

            __DimensionX = new VirtualDimension(this, Axis.X);
            __DimensionX.VirtualBeginChanged += _ScrollbarsValueChanged;

            __ScrollBarX = new DxScrollBarX() { Visible = false };
            __ScrollBarX.ValueChanged += __DimensionX.ScrollBarValueChanged;
            this.Controls.Add(__ScrollBarX);

            __DimensionY = new VirtualDimension(this, Axis.Y);
            __DimensionY.VirtualBeginChanged += _ScrollbarsValueChanged;

            __ScrollBarY = new DxScrollBarY() { Visible = false };
            __ScrollBarY.ValueChanged += __DimensionY.ScrollBarValueChanged;
            this.Controls.Add(__ScrollBarY);

            __VirtualInitialized = true;
            __DataFormZoom = 1f;

            this._RefreshInnerLayout();
        }
        /// <summary>
        /// Obsahuje true po dokončení inicializace virtuálního prostoru
        /// </summary>
        private bool _VirtualInitialized { get { return __VirtualInitialized; } } private bool __VirtualInitialized;
        /// <summary>
        /// Obsahuje true, pokud objekt reprezentuje virtuální prostor = má nastavenou velikost obsahu <see cref="ContentVirtualSize"/> (kladné rozměry)
        /// a má povoleno používat Scrollbary <see cref="ScrollbarsEnabled"/>.
        /// </summary>
        private bool _IsInVirtualMode { get { return (_HasContentSize && __ScrollbarsEnabled); } }
        /// <summary>
        /// Nativní událost MouseWheel na controlu - přeneseme ji do Scrollbaru Y, pokud to lze
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _MouseWheel(object sender, MouseEventArgs e)
        {
            if (_IsInVirtualMode && __DimensionY.UseScrollbar)
                __ScrollBarY.DoMouseWheel(e);
        }
        /// <summary>
        /// Akceptuje aktuální fyzickou velikost Controlu a zařídí podle ní platnost Scrollbarů
        /// </summary>
        private void _AcceptControlSize()
        {
            _RefreshInnerLayout();
        }
        /// <summary>
        /// Zajistí přepočty potřebnosti a hodnoty a souřadnice ScrollBarů podle aktuální velikosti a aktuálního rozměru a zajistí zobrazení / skrytí ScrollBarů.
        /// Určí souřadnice prostoru pro <see cref="ContentPanel"/> a tento panel do daných souřadnic umístí. Vyvolá jeho překreslení.
        /// </summary>
        private void _RefreshInnerLayout()
        {
            if (!_VirtualInitialized) return;
            if (__RefreshInnerLayoutRunning) return;                 // Jeden _RefreshInnerLayout() už běží...

            try
            {
                __RefreshInnerLayoutRunning = true;
                _ValidateDesignSize();                               // Může vyvolat setování VirtualSize, tedy _SetVirtualSize(), což volá nás _RefreshInnerLayout(); ale druhá instance metody se neprovádí protože __RefreshInnerLayoutRunning je true
                _DetectScrollbars();
                _ShowScrollBars();
                _SetContentBounds();
                _CheckVisibleDesignBoundsChanged(true);
                __ValidatedVirtualSize = this.ContentVirtualSize;
            }
            finally
            {
                __RefreshInnerLayoutRunning = false;
            }
        }
        /// <summary>
        /// Detekuje potřebu zobrazení Scrollbarů. Volá se jak po změně <see cref="ContentVirtualSize"/>, tak po Resize controlu.
        /// </summary>
        private void _DetectScrollbars()
        {
            __DimensionX.UseScrollbar = false;
            __DimensionY.UseScrollbar = false;                                 // Z této hodnoty bude vycházet __DimensionX pro určené své hodnoty .NeedScrollbar

            if (_IsInVirtualMode)
            {
                __DimensionX.UseScrollbar = __DimensionX.NeedScrollbar;        // Osa X (Width) si určí jen svoji potřebu Scrollbaru, bez přítomnosti Scrollbaru Y
                __DimensionY.UseScrollbar = __DimensionY.NeedScrollbar;        // Osa Y (Height) si určí jen svoji potřebu Scrollbaru, nyní už se zohledněním Scrollbaru X (dle __DimensionX.UseScrollbar)
                if (__DimensionY.UseScrollbar && !__DimensionX.UseScrollbar)   // Pokud osa Y má Scrollbar a osa X jej dosud nemá, pak se zohledněním existence Scrollbaru Y (dle __DimensionY.UseScrollbar, zmenšení prostoru) si jej nyní může taky chtít použít...
                    __DimensionX.UseScrollbar = __DimensionX.NeedScrollbar;    // Osa X (Width) si určí potřebu Scrollbaru X, se zohledněním přítomnosti Scrollbaru Y
                __DimensionX.ModifyValueForClientSize();
                __DimensionY.ModifyValueForClientSize();
            }
        }
        /// <summary>
        /// Zajistí zobrazení Scrollbarů podle stavu a podle potřeby
        /// </summary>
        private void _ShowScrollBars()
        {
            if (_IsInVirtualMode)
            {
                __DimensionX.ShowScrollBar();
                __DimensionY.ShowScrollBar();
            }
            else
            {
                __ScrollBarX.Visible = false;
                __ScrollBarY.Visible = false;
            }
        }
        /// <summary>
        /// Po změně virtuálního počátku provedu invalidaci VirtualBegin, kontrolu změn VisibleDesignBounds a vykreslení ContentPanel.Draw()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ScrollbarsValueChanged(object sender, EventArgs e)
        {
            _VirtualBeginInvalidate();
            _CheckVisibleDesignBoundsChanged(true);
            this.InteractiveContentPanel?.VirtualCoordinatesChanged();
            this.ContentPanel?.Draw();
        }
        /// <summary>
        /// Fyzický Scrollbar vodorovný pro posun na ose X
        /// </summary>
        private DxScrollBarX __ScrollBarX;
        /// <summary>
        /// Fyzický Scrollbar svislý pro posun na ose Y
        /// </summary>
        private DxScrollBarY __ScrollBarY;
        /// <summary>
        /// Virtuální souřadnice ve směru osy X (Width)
        /// </summary>
        private VirtualDimension __DimensionX;
        /// <summary>
        /// Virtuální souřadnice ve směru osy Y (Height)
        /// </summary>
        private VirtualDimension __DimensionY;
        /// <summary>
        /// Aktuálně provádíme metodu <see cref="_RefreshInnerLayout"/>? Pak ji neumožníme provést rekurzivně.
        /// </summary>
        private bool __RefreshInnerLayoutRunning;
        #endregion
        #region subclass VirtualDimension a enum Axis
        /// <summary>
        /// Třída pro řešení virtuální / nativní souřadnice v jedné ose (Velikost obsahu / reálný prostor) + Scrollbar pro tuto osu
        /// </summary>
        private class VirtualDimension
        {
            #region Konstruktor a privátní fieldy a základní metody
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="axis"></param>
            public VirtualDimension(DxVirtualPanel owner, Axis axis)
            {
                __Owner = owner;
                __Axis = axis;
                ReloadScrollbarSize();
            }
            /// <summary>
            /// Vlastník
            /// </summary>
            private DxVirtualPanel __Owner;
            /// <summary>
            /// Směr osy
            /// </summary>
            private Axis __Axis;
            /// <summary>
            /// Velikost zdejšího Scrollbaru, pokud bude zobrazen.
            /// Pro dimenzi X (vodorovná, řeší X a Width) je zde Výška vodorovného Scrollbaru.
            /// Pro dimenzi Y (svislá, řeší Y a Height) je zde Šířka svislého Scrollbaru.
            /// </summary>
            private int __ScrollbarSize;
            /// <summary>
            /// Hodnota SmallChange na ScrollBaru
            /// </summary>
            private int __ScrollbarSmallChange;
            /// <summary>
            /// Vrátí hodnotu pro osu X nebo Y podle <see cref="__Axis"/>.
            /// Hodnotu čte pomocí dodané funkce.
            /// <para/>
            /// Myslím že je to optimálnější, než když bych očekával dva parametry obsahující prostá hotová data - protože při volání zdejší metody bych je nejprve musel oba vyhodnotit (náročnost), a pak bych jeden zahodil.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="funcValueX"></param>
            /// <param name="funcValueY"></param>
            /// <returns></returns>
            private T _GetValue<T>(Func<T> funcValueX, Func<T> funcValueY)
            {
                switch (__Axis)
                {
                    case Axis.X: return funcValueX();
                    case Axis.Y: return funcValueY();
                }
                return default(T);
            }
            /// <summary>
            /// Prostor za koncem dat, který zobrazujeme nad rámec VirtualSize
            /// </summary>
            private const int _EndSpace = 15;
            #endregion
            #region Používání a velikost Scrollbaru
            /// <summary>
            /// V tomto směru by měl být zobrazen Scrollbar? Detekuje se z rozměrů (virtuální a nativní) a z přítomnosti a velikosti Scrollbaru v opačném směru
            /// </summary>
            public bool NeedScrollbar
            {
                get
                {
                    int virtualBegin = 0;
                    _ValidateVirtualBegin(ref virtualBegin, out bool needScrollbar, out bool beginChanged);
                    return needScrollbar;
                }
            }
            /// <summary>
            /// V tomto směru bude zobrazen Scrollbar? 
            /// Setuje <see cref="__Owner"/> jako výsledek výpočtů!
            /// Owner k tomu používá hodnotu <see cref="NeedScrollbar"/> z této dimenze, ale musí zohlednit křížovou potřebu Scrollbaru.
            /// Slovně: pokud osa X těsně nepotřebuje Scrollbar, ale osa Y jej potřebuje, tak zmenší vizuální prostor na ose X a poté i osa X potřebuje svůj Scrollbar - proto, že dostupný prostor na ose X zmenšil Scrollbar Y.
            /// </summary>
            public bool UseScrollbar { get; set; }
            /// <summary>
            /// Znovu načte velikost Scrollbar - je vhodné volat po změně DPI atd...
            /// </summary>
            public void ReloadScrollbarSize()
            {
                __ScrollbarSize = _GetValue(() => SystemInformation.HorizontalScrollBarHeight, () => SystemInformation.VerticalScrollBarWidth);
                __ScrollbarSmallChange = (int)(((float)SystemInformation.MouseWheelScrollLines) * DxComponent.GetSystemFont(DxComponent.SystemFontType.DefaultFont).GetHeight());
            }
            /// <summary>
            /// Velikost zdejšího Scrollbaru, pokud bude zobrazen. Zde je tedy kladné číslo, i když scrollbar aktuálně není zobrazen.
            /// Pro dimenzi X (vodorovná, řeší X a Width) je zde Výška vodorovného Scrollbaru.
            /// Pro dimenzi Y (svislá, řeší Y a Height) je zde Šířka svislého Scrollbaru.
            /// </summary>
            public int ScrollbarSize { get { return __ScrollbarSize; } }
            /// <summary>
            /// Metoda upraví aktuální počátek této virtuální dimenze podle velikosti klientského prostoru.
            /// Účelem je řešit přizpůsobení hodnoty ScrollBaru změně velikosti klientského prostoru.
            /// <para/>
            /// Příklad: zobrazujeme data velikosti 500 a máme prostor 400 = zobrazujeme Scrollbar (<see cref="NeedScrollbar"/> je true, návazně na to <see cref="UseScrollbar"/> = true).
            /// Posuneme scrollbar tak, že vidíme prostor od pixelu 70 do 470 (hodnota <see cref="VirtualBegin"/> = 70).
            /// Následně zvětšíme prostor klienta ze 400 na 450: virtuální container vyhodnotí změnu velikosti: <see cref="NeedScrollbar"/> je nadále true (protože VirtualSize = 500 a ClientSize = 450) návazně na to <see cref="UseScrollbar"/> = true.
            /// Ale když necháme <see cref="VirtualBegin"/> = 70, tak budeme zobrazovat prostor 70 + 450 = 520, tedy zbytečně. 
            /// <para/>
            /// Měli bychom (a to řeší zdejší metoda) posunout <see cref="VirtualBegin"/> tak, aby se zobrazil prostor na konci do 500, a ne 520 = upravíme <see cref="VirtualBegin"/> na 50.
            /// <para/>
            /// Obdobně při zvětšení ClientSize např. na 550 (to už <see cref="NeedScrollbar"/> i <see cref="UseScrollbar"/> je false) máme změnit <see cref="VirtualBegin"/> na 0.
            /// </summary>
            public void ModifyValueForClientSize()
            {
                int virtualBegin = __VirtualBegin;
                _ValidateVirtualBegin(ref virtualBegin, out bool needScrollbar, out bool beginChanged);
                if (beginChanged)
                    _SetVirtualBeginValidated(virtualBegin);
            }
            /// <summary>
            /// Obsahuje true, pokud objekt reprezentuje virtuální prostor = má nastavenou velikost obsahu <see cref="__ContentDesignSize"/> (kladné rozměry).
            /// V tom případě se v procesu Resize v metodě <see cref="_AcceptControlSize"/> 
            /// </summary>
            private bool _IsInVirtualMode { get { return __Owner._IsInVirtualMode; } }
            /// <summary>
            /// Velikost datového obsahu = virtuální velikost
            /// </summary>
            private int? _VirtualSize { get { return _GetValue(() => __Owner.ContentVirtualSize?.Width, () => __Owner.ContentVirtualSize?.Height); } }
            /// <summary>
            /// Velikost viditelného prostoru, celková (tj. fyzický Control = obsah + případný scrollbar)
            /// </summary>
            private int _ClientSize { get { return _GetValue(() => __Owner.CurrentClientBounds.Width, () => __Owner.CurrentClientBounds.Height); } }
            /// <summary>
            /// Velikost viditelného datového prostoru, zmenšená o druhý (párový) Scrollbar = zde jsou zobrazena data
            /// </summary>
            private int _ClientDataSize { get { return _GetValue(() => __Owner.CurrentClientBounds.Width - __Owner.__DimensionY._CurrentScrollbarSize, () => __Owner.CurrentClientBounds.Height - __Owner.__DimensionX._CurrentScrollbarSize); } }
            /// <summary>
            /// Aktuální velikost zdejšího Scrollbar, se zohledněním <see cref="UseScrollbar"/> (pokud se nepoužívá, je zde 0)
            /// </summary>
            private int _CurrentScrollbarSize { get { return (UseScrollbar ? __ScrollbarSize : 0); } }
            /// <summary>
            /// Aktuální velikost Scrollbar z opačné osy, se zohledněním jejího <see cref="UseScrollbar"/> (pokud se nepoužívá, je zde 0)
            /// </summary>
            private int _OtherScrollbarSize { get { return _GetValue(() => __Owner.__DimensionY._CurrentScrollbarSize, () => __Owner.__DimensionX._CurrentScrollbarSize); } }
            #endregion
            #region Aktivace a Eventhandler Scrollbaru, jeho VirtualBegin = počátek virtuálního prostoru
            /// <summary>
            /// Zobrazí Scrollbar na správném místě a se správnými hodnotami
            /// </summary>
            public void ShowScrollBar()
            {
                var scrollBar = _ScrollBar;
                if (scrollBar is null) return;
                if (!UseScrollbar)
                {   // Nezobrazovat:
                    ScrollBounds = null;
                    if (scrollBar.Visible) scrollBar.Visible = false;
                    return;
                }

                try
                {
                    __SuppressScrollBarValueChange = true;

                    // Fyzická souřadnice:
                    var bounds = _GetScrollbarBounds();
                    ScrollBounds = bounds;
                    scrollBar.Bounds = bounds;

                    // Hodnoty:
                    _GetScrollbarValues(out int minimum, out int maximum, out int value, out int largeChange, out int smallChange);
                    scrollBar.Minimum = minimum;
                    scrollBar.Maximum = maximum;           // Reprezentuje celý virtuální prostor = ContentSize
                    scrollBar.Value = value;               // Na této hodnotě je první zobrazený pixel z virtuální oblasti; Value má rozsah od Minimum do (Maximum - LargeChange + 1)
                    scrollBar.LargeChange = largeChange;   // Odpovídá viditelné oblasti = ClientSize, typicky je cca 90% = provede posun o "celou stránku" = při kliknutí na prostor nad / pod Thumbem
                    scrollBar.SmallChange = smallChange;   // Odpovídá scrollování myší nebo kliknutí na horní/dolní button, typicky 5% viditelné oblasti
                }
                finally
                {
                    __SuppressScrollBarValueChange = false;
                }

                if (!scrollBar.Visible) scrollBar.Visible = true;
            }
            /// <summary>
            /// Aktuální fyzické souřadnice zdejšího scrollbaru, nebo null pokud není zobrazen
            /// </summary>
            public Rectangle? ScrollBounds { get; private set; }
            /// <summary>
            /// Handler události, kdy patřičný ScrollBar zaregistroval pohyb / změnu hodnoty
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void ScrollBarValueChanged(object sender, EventArgs e)
            {
                if (__SuppressScrollBarValueChange) return;
                if (!_IsInVirtualMode) return;

                _SetVirtualBegin(_ScrollBar.Value);
            }
            /// <summary>
            /// Control Scrollbar pro tuto osu
            /// </summary>
            private IDxScrollBar _ScrollBar { get { return _GetValue<IDxScrollBar>(() => __Owner.__ScrollBarX, () => __Owner.__ScrollBarY); } }
            /// <summary>
            /// Vrátí fyzické souřadnice pro control Scrollbar
            /// </summary>
            /// <returns></returns>
            private Rectangle _GetScrollbarBounds()
            {
                Rectangle clientBounds = this.__Owner.CurrentClientBounds;
                int x, y, w, h;
                switch (this.__Axis)
                {
                    case Axis.X:
                        w = clientBounds.Width - _OtherScrollbarSize;
                        h = __ScrollbarSize;
                        x = clientBounds.Left;
                        y = clientBounds.Height - h;
                        return new Rectangle(x, y, w, h);
                    case Axis.Y:
                        h = clientBounds.Height - _OtherScrollbarSize;
                        w = __ScrollbarSize;
                        y = clientBounds.Top;
                        x = clientBounds.Width - w;
                        return new Rectangle(x, y, w, h);
                }
                return Rectangle.Empty;
            }
            /// <summary>
            /// Určí a vrátí hodnoty pro fyzický Scrollbar
            /// </summary>
            /// <param name="minimum"></param>
            /// <param name="maximum"></param>
            /// <param name="value"></param>
            /// <param name="largeChange"></param>
            /// <param name="smallChange"></param>
            private void _GetScrollbarValues(out int minimum, out int maximum, out int value, out int largeChange, out int smallChange)
            {
                var dataSize = _ClientDataSize;

                minimum = 0;
                maximum = _VirtualSize ?? dataSize;
                value = VirtualBegin;
                largeChange = dataSize;
                smallChange = __ScrollbarSmallChange;
            }
            /// <summary>
            /// Aktuálně je potlačený event <see cref="ScrollBarValueChanged"/> = změny provádíme z kódu a nechceme se v nich zacyklit
            /// </summary>
            private bool __SuppressScrollBarValueChange;
            /// <summary>
            /// Hodnota Virtuálního počátku.
            /// Vyjadřuje souřadnici Virtuálního prostoru, která je zobrazena na viditelném pixelu 0 = první viditelný pixel virtuálního prostoru.
            /// <para/>
            /// Pokud je například <see cref="VirtualBegin"/> = 50; pak to značí, že ScrollBar mírně odroloval od začátku, a prvních 50 pixelů virtuálního (tj. celkového) obsahu je skryto vlevo/nahoře.
            /// </summary>
            public int VirtualBegin
            {
                get { return _GetVirtualBegin(); }
                set { _SetVirtualBegin(value); }
            }
            /// <summary>
            /// Událost volaná po jakékoli změně počátku virtuálního prostoru
            /// </summary>
            public event EventHandler VirtualBeginChanged;
            /// <summary>
            /// Metoda volaná po jakékoli změně počátku virtuálního prostoru
            /// </summary>
            /// <param name="args"></param>
            protected virtual void OnVirtualBeginChanged(EventArgs args) { }
            /// <summary>
            /// Vyvolá metodu <see cref="OnVirtualBeginChanged(EventArgs)"/> a event <see cref="VirtualBeginChanged"/>
            /// </summary>
            private void _RunVirtualBeginChanged()
            {
                EventArgs args = EventArgs.Empty;
                OnVirtualBeginChanged(args);
                VirtualBeginChanged?.Invoke(this, args);
            }
            /// <summary>
            /// Vrátí validní hodnotu počátku virtuálního prostoru
            /// </summary>
            /// <returns></returns>
            private int _GetVirtualBegin()
            {
                if (!UseScrollbar) return 0;

                int virtualBegin = __VirtualBegin;
                _ValidateVirtualBegin(ref virtualBegin, out bool needScrollbar, out bool beginChanged);
                if (beginChanged)
                {
                    __VirtualBegin = virtualBegin;
                    _RunVirtualBeginChanged();
                }
                return virtualBegin;
            }
            /// <summary>
            /// Uloží danou hodnotu po validaci jako hodnotu počátku virtuálního prostoru
            /// </summary>
            /// <param name="virtualBegin"></param>
            private void _SetVirtualBegin(int virtualBegin)
            {
                if (!_IsInVirtualMode) return;
                if (!UseScrollbar) return;

                _ValidateVirtualBegin(ref virtualBegin);
                _SetVirtualBeginValidated(virtualBegin);
            }
            /// <summary>
            /// Uloží dodanou hodnotu bez dalších kontrol do <see cref="__VirtualBegin"/> a vyvolá patřičné eventy. Hodnotu nekontroluje
            /// </summary>
            /// <param name="virtualBegin"></param>
            private void _SetVirtualBeginValidated(int virtualBegin)
            {
                int oldBegin = __VirtualBegin;
                if (virtualBegin != oldBegin)
                {
                    __VirtualBegin = virtualBegin;
                    DxComponent.LogAddLine(LogActivityKind.Scroll, $"VirtualDimension {__Axis}: VirtualBeginChanged from {oldBegin} to {VirtualBegin}");
                    _RunVirtualBeginChanged();
                }
            }
            /// <summary>
            /// Provede validaci hodnoty <paramref name="virtualBegin"/> s ohledem na aktuální velikost panelu a velikost dat.
            /// </summary>
            /// <param name="virtualBegin"></param>
            private void _ValidateVirtualBegin(ref int virtualBegin)
            {
                _ValidateVirtualBegin(ref virtualBegin, out var _, out var _);
            }
            /// <summary>
            /// Provede validaci hodnoty <paramref name="virtualBegin"/> s ohledem na aktuální velikost panelu a velikost dat.
            /// Tato metoda určí vhodnou hodnotu pro <see cref="VirtualBegin"/>, ale nikam ji nezapisuje.
            /// Určí i potřebnost Scrollbaru = <see cref="NeedScrollbar"/>.
            /// </summary>
            /// <param name="virtualBegin"></param>
            /// <param name="needScrollbar"></param>
            /// <param name="beginChanged"></param>
            private void _ValidateVirtualBegin(ref int virtualBegin, out bool needScrollbar, out bool beginChanged)
            {
                beginChanged = false;
                int? virtualSize = _VirtualSize;                               // Celková velikost dat (DesignSize přepočtené Zoomem na VirtualSize) 
                if (virtualSize.HasValue && virtualSize.Value > 0)
                {   // Mám VirtualSize:
                    int clientDataSize = this._ClientDataSize;                 // Velikost controlu zmenšená o ten druhý ScrollBar
                    int virtualSpace = virtualSize.Value + _EndSpace;          // Velikost dat zvětšená o rezervu
                    needScrollbar = (virtualSpace > clientDataSize);           // Scrollbar potřebuji, když data jsou větší než aktuální prostor
                    if (needScrollbar)
                    {   // Potřebuji Scrollbar = řeším hodnotu 'virtualBegin':
                        int validBegin = virtualBegin;
                        int virtualEnd = virtualBegin + clientDataSize;        // Toto by byl poslední zobrazený pixel na konci prostoru
                        if (virtualEnd > virtualSpace)                         // Pokud teoretický poslední zobrazený pixel je větší než poslední potřebný pixel:
                            validBegin = virtualSpace - clientDataSize;        //  tak posunu 'validBegin' tak, aby počínaje od něj + clientDataSize vyšel poslední zobrazený pixel == poslední potřebný pixel.
                        if (validBegin < 0)                                    // Hodnota 'validBegin' nesmí být záporná.
                            validBegin = 0;

                        // Došlo ke změně 'virtualBegin'?
                        beginChanged = (validBegin != virtualBegin);
                        if (beginChanged)
                            virtualBegin = validBegin;
                    }
                }
                else
                {   // Nemám zadanou VirtualSize => neřeším ScrollBar:
                    needScrollbar = false;
                }

                if (!needScrollbar)
                {   // Pokud nepotřebuji Scrollbar, pak 'virtualBegin' musí být 0:
                    beginChanged = (virtualBegin != 0);
                    virtualBegin = 0;
                }
            }
            /// <summary>
            /// Souřadnice počátku = odpovídá prvnímu viditelnému pixelu
            /// </summary>
            private int __VirtualBegin;
            #endregion
        }
        /// <summary>
        /// Orientace osy a Scrollbaru
        /// </summary>
        private enum Axis
        {
            /// <summary>
            /// X: vodorovný scrollbar, je dole
            /// </summary>
            X,
            /// <summary>
            /// Y: svislý scrollbar, je vpravo
            /// </summary>
            Y
        }
        #endregion
    }
    #endregion
    #region DxBufferedGraphicPanel : implementuje doublebuffer pro grafiku.
    /// <summary>
    /// <see cref="DxBufferedGraphicPanel"/> : implementuje doublebuffer pro grafiku.
    /// <para/>
    /// Potomci této třídy nemusí zajišťovat bufferování grafiky.
    /// Potomci této třídy implementují vykreslování svého obsahu tím, že přepíšou metodu OnPaintToBuffer(), a v této metodě zajisté své vykreslení.
    /// Pro spuštění překreslení svého obsahu volají Draw() (namísto Invalidate()).
    /// </summary>
    public class DxBufferedGraphicPanel : DxGraphicsPanel, IDisposable
    {
        #region Konstruktor a Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBufferedGraphicPanel()
        {
            this._InitGraphics();
        }
        void IDisposable.Dispose()
        {
            if (MainBuffGraphics != null)
            {
                MainBuffGraphics.Dispose();
                MainBuffGraphics = null;
            }
            if (MainBuffGraphContent != null)
            {
                MainBuffGraphContent.Dispose();
                MainBuffGraphContent = null;
            }
            if (BackupBuffGraphics != null)
            {
                BackupBuffGraphics.Dispose();
                BackupBuffGraphics = null;
            }
            if (BackupBuffGraphContent != null)
            {
                BackupBuffGraphContent.Dispose();
                BackupBuffGraphContent = null;
            }
        }
        #endregion
        #region Public property a eventy
        /// <summary>
        /// Virtuální panel, který provádí přepočty designových hodnot na hodnoty vizuální (Zoom a posuny pomocí Scrollbarů)
        /// </summary>
        public DxVirtualPanel VirtualPanel { get; set; }
        /// <summary>
        /// Pokud chceme využít bufferovaného vykreslování této třídy bez toho, abychom ji dědili (použijeme nativní třídu),
        /// pak je nutno vykreslování umístit do tohoto eventu.
        /// Pracuje se zde zcela stejně, jako v eventu Paint(), ale vizuální rozdíl je zcela zásadní:
        /// Zatímco Paint() kreslí přímo do controlu, naživo, a pokaždé znovu,
        /// pak tato metoda PaintToBuffer() kreslí do bufferu do paměti, a control si přebírá výsledek najednou, optimalizovaně.
        /// </summary>
        [Browsable(true)]
        [Category("Paint")]
        [Description("Zde musí být implementováno uživatelské vykreslování obsahu objektu do grafického bufferu. Pracuje se zde zcela stejně, jako v eventu Paint(), ale fyzicky se metoda volá jen v nutných případech.")]
        public event PaintEventHandler PaintToBuffer;
        /// <summary>
        /// Barva pozadí prvku. Je využita pokud není nastaveno (BackgroundIsTransparent == true)
        /// </summary>
        [Category("Appearance")]
        [Description("Barva pozadí prvku. Je využita pokud není nastaveno (BackgroundIsTransparent == true)")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; Draw(); }
        }
        /// <summary>
        /// Vykreslované okraje controlu (strany borderu).
        /// Umožní řešit navazování různých controlů do jednoho borderu.
        /// </summary>
        [Description("Vykreslované okraje controlu (strany borderu). Umožní řešit navazování různých controlů do jednoho borderu.")]
        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(Border3DSide.All)]
        public virtual Border3DSide BorderSides
        {
            get { return _BorderSides; }
            set { _BorderSides = value; Draw(); }
        }
        private Border3DSide _BorderSides = Border3DSide.All;
        /// <summary>
        /// Prostor pro kreslení uvnitř tohoto prvku, s vynecháním aktuálního Borderu
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle ClientArea { get { return this._GetClientBounds(); } }
        /// <summary>
        /// Tato property může obsahovat potřebnou velikost prostoru v designových pixelech, kterou budeme zobrazovat ve virtuálním panelu <see cref="DxVirtualPanel"/>.
        /// Bázová třída <see cref="DxBufferedGraphicPaintArgs"/> vrací null, tato třída neřeší virtuální obsah.
        /// Potomek může implementovat, pak přímo odsud bude <see cref="DxVirtualPanel"/> číst a akceptovat tuto hodnotu.
        /// </summary>
        public virtual Size? ContentDesignSize { get { return null; } }
        /// <summary>
        /// Metodu volá <see cref="DxVirtualPanel"/> v situaci, kdy na něm dojde k změně Zoomu v <see cref="DxVirtualPanel.CurrentZoom"/>.
        /// Pak může this panel zajistit přepočet nebo invalidaci souřadnic svých prvků.
        /// </summary>
        public virtual void OnCurrentZoomChanged() { }
        #endregion
        #region Řízení kreslení - public vrstva: vyvolávací metoda + virtual výkonná metoda
        /// <summary>
        /// Potlačení kreslení při provádění rozsáhlejších změn. Po ukončení je třeba nastavit na false !
        /// Default = false = kreslení není potlačeno.
        /// Při provádění rozsáhlejších změn je vhodné nastavit na true, a po dokončení změn vrátit na false => tím se automaticky vyvolá Draw.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool SuppressDrawing
        {
            get
            {
                return this._SuppressDrawing;
            }
            set
            {
                bool setDraw = (!value && this._SuppressDrawing);        // true při změně z true na false 
                this._SuppressDrawing = value;
                if (setDraw)
                    this.Draw();
                this._SuppressLevel = 0;
            }
        }
        private bool _SuppressDrawing = false;
        private int _SuppressLevel = 0;
        /// <summary>
        /// Umožní řídit potlačení vykreslování v hierarchii metod.
        /// Zajistí, že vykreslování bude potlačeno přinejmenším do párového vyvolání metody SuppressDrawingPop().
        /// Chování je obdobou chování Stacku: první Push zablokuje kreslení, následné Push and Pop to nezmění, poslední Pop to povolí.
        /// Podmínka: Push a Pop musí být v páru, jinak kreslení zamrzne.
        /// Řešení: je možno kdykoliv vložit SuppressDrawing = false a vykreslování ožije (nepárový zásobník se vynuluje).
        /// </summary>
        public virtual void SuppressDrawingPush()
        {
            if (this._SuppressLevel <= 0)        // První volání skutečně zablokuje Drawing:
                this.SuppressDrawing = true;
            this._SuppressLevel++;               // Každé volání zvýší level
        }
        /// <summary>
        /// Umožní řídit potlačení vykreslování v hierarchii metod.
        /// Zajistí, že vykreslování bude potlačeno přinejmenším do párového vyvolání metody SuppressDrawingPop().
        /// Chování je obdobou chování Stacku: první Push zablokuje kreslení, následné Push and Pop to nezmění, poslední Pop to povolí.
        /// Podmínka: Push a Pop musí být v páru, jinak kreslení zamrzne.
        /// Řešení: je možno kdykoliv vložit SuppressDrawing = false a vykreslování ožije (nepárový zásobník se vynuluje).
        /// </summary>
        public virtual void SuppressDrawingPop()
        {
            this._SuppressLevel--;               // Každé volání sníží level
            if (this._SuppressLevel <= 0)        // A až se dostaneme na 0, tak obnovíme Drawing:
                this.SuppressDrawing = false;
        }
        /// <summary>
        /// Po změně prostoru uvnitř
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.Draw();
        }
        /// <summary>
        /// Metoda, která zajišťuje kreslení.
        /// Potomkové mohou využít, ale musí volat base(sender, e);
        /// base metoda zajišťuje e.Graphics.Clear(this.BackColor);
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
        }
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual void Draw()
        {
            this._Draw(Rectangle.Empty);
        }
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        /// <param name="drawRectangle">
        /// Informace pro kreslící program o rozsahu překreslování.
        /// Nemusí nutně jít o grafický prostor, toto je pouze informace předáváná z parametru metody Draw() do handleru PaintToBuffer().
        /// V servisní třídě se nikdy nepoužije ve významu grafického prostoru.
        /// </param>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual void Draw(Rectangle drawRectangle)
        {
            this._Draw(drawRectangle);
        }
        #endregion
        #region Řízení práce s BufferedGraphic (obecně přenosný mechanismus i do jiných tříd) a Virtuální souřadnice + Scrollbary
        /// <summary>
        /// Inicializace grafických objektů
        /// </summary>
        private void _InitGraphics()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserMouse | ControlStyles.UserPaint, true);

            this._MainGraphBufferInit();
            this._BackupGraphBufferInit();
        }
        #region Privátní řídící mechanismus - Main buffer, Backup buffer
        #region Main grafika
        /// <summary>
        /// Obsah bufferované grafiky, pro rychlejší překreslování a udržení obrazu v paměti i mimo plochu Controlu
        /// </summary>
        protected BufferedGraphicsContext MainBuffGraphContent;
        /// <summary>
        /// Řídící objekt bufferované grafiky
        /// </summary>
        protected BufferedGraphics MainBuffGraphics;
        /// <summary>
        /// Tato metoda do objektu this nastaví parametry pro doublebuffer grafiky.
        /// Tuto metodu voláme z konstruktoru objektu.
        /// </summary>
        private void _MainGraphBufferInit()
        {
            // Retrieves the BufferedGraphicsContext for the current application domain.
            MainBuffGraphContent = BufferedGraphicsManager.Current;

            // Sets the maximum size for the primary graphics buffer
            // of the buffered graphics context for the application
            // domain.  Any allocation requests for a buffer larger 
            // than this will create a temporary buffered graphics 
            // context to host the graphics buffer.
            MainBuffGraphContent.MaximumBuffer = _MaximumBufferSize;

            // Allocates a graphics buffer the size of this form
            // using the pixel format of the Graphics created by 
            // the Form.CreateGraphics() method, which returns a 
            // Graphics object that matches the pixel format of the form.
            MainBuffGraphics = MainBuffGraphContent.Allocate(this.CreateGraphics(), _CurrentGraphicsRectangle);

            this.Resize += new EventHandler(_ControlResize);
            this.Paint += new PaintEventHandler(_ControlPaint);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.OptimizedDoubleBuffer, true);

            // Draw the first frame to the buffer.
            // _Draw(Rectangle.Empty);       Nemůžeme, protože jsme v konstruktoru, 
            // a Draw() nám vyvolá virtuální metodu OnPaintToBuffer(), tedy akci na třídě potomka, 
            // čímž porušíme zásadu Nevolat z konstruktoru virtuální metody.
            // Důsledek = třída potomka může mít v konstuktoru zajištěnou inicializaci proměnných pro Draw(), 
            // ale ten se vyvolá z konstruktoru předka ještě před potřebnou inicializací.

        }
        /// <summary>
        /// Vrací MaximumBufferSize pro BufferedGraphicsContext (=this.Size + 1)
        /// </summary>
        private Size _MaximumBufferSize
        {
            get
            {
                int w = (this.Width < 1 ? 1 : this.Width);
                int h = (this.Height < 1 ? 1 : this.Height);
                return new Size(w + 1, h + 1);
            }
        }
        /// <summary>
        /// Vrací Rectangle pro alokaci prostoru metodou BufferedGraphics.Allocate()
        /// </summary>
        private Rectangle _CurrentGraphicsRectangle
        {
            get
            {
                int w = (this.Width < 1 ? 1 : this.Width);
                int h = (this.Height < 1 ? 1 : this.Height);
                return new Rectangle(0, 0, w, h);
            }
        }
        #endregion
        #region Záložní grafika
        /// <summary>
        /// Záloha dat grafiky.
        /// Umožní uložit obraz grafiky do této zálohy (metodou BackupGraphicStore()) 
        /// anebo obsah zálohy načíst do pracovní grafiky (metodou BackupGraphicLoad()).
        /// Připravenost grafiky před použitím metody BackupGraphicLoad() lze testovat čtením property BackupGraphicIsReady.
        /// </summary>
        private BufferedGraphicsContext BackupBuffGraphContent;
        /// <summary>
        /// Řídící objekt zálohy grafiky.
        /// Umožní uložit obraz grafiky do této zálohy (metodou BackupGraphicStore()) 
        /// anebo obsah zálohy načíst do pracovní grafiky (metodou BackupGraphicLoad()).
        /// Připravenost grafiky před použitím metody BackupGraphicLoad() lze testovat čtením property BackupGraphicIsReady.
        /// </summary>
        private BufferedGraphics BackupBuffGraphics;
        /// <summary>
        /// Dimenze záložní grafiky. Musí odpovídat aktuálním dimenzím, jinak grafiku nelze použít.
        /// </summary>
        private Size BackupBuffSize;
        /// <summary>
        /// Iniciace dat záložní grafiky
        /// </summary>
        private void _BackupGraphBufferInit()
        {
            this.BackupBuffGraphContent = BufferedGraphicsManager.Current;
            this.BackupBuffSize = Size.Empty;
        }
        /// <summary>
        /// Uloží současný stav z hlavního grafického bufferu (do něhož se kreslí v metodě OnPaintToBuffer přes e.Graphics)
        /// do záložního grafického bufferu.
        /// Účel: současnou podobu grafiky si zazálohujeme jako "podklad", protože její vytvoření nás stálo mnoho úsilí.
        /// Následně je možno tento "podklad" okamžitě natáhnout ze zálohy (metodou BackupGraphicLoad()), a "počmárat" ji něčím rychlým a dočasným,
        /// pak vykreslit, a příště ji znovu vytáhnout ze zálohy a počmárat ji něčím jiným, s tím že náročný podklad se nemusí znovu vykreslovat.
        /// </summary>
        protected void BackupGraphicStore()
        {
            // 1. Je nutno alokovat prostor pro záložní grafiku (rozdíl Size) ?
            Size currentSize = _MaximumBufferSize;
            if (this.BackupBuffSize != currentSize)
            {
                BackupBuffGraphContent.MaximumBuffer = currentSize; ;
                if (BackupBuffGraphics != null)
                {
                    BackupBuffGraphics.Dispose();
                    BackupBuffGraphics = null;
                }
                BackupBuffGraphics = BackupBuffGraphContent.Allocate(this.CreateGraphics(), _CurrentGraphicsRectangle);
                this.BackupBuffSize = currentSize;
            }

            // 2. Zazálohovat stav hlavní grafiky:
            MainBuffGraphics.Render(BackupBuffGraphics.Graphics);
        }
        /// <summary>
        /// Načte zálohu grafiky ze záložního grafického bufferu do hlavního (v němž se kreslí v metodě OnPaintToBuffer přes e.Graphics).
        /// Pozor: před použitím je třeba ověřit, zda lze data načíst, ověřením že (BackupGraphicIsReady == true).
        /// Použití: po některém dřívějším plném renderování grafiky lze výsledek zazálohovat (metodou BackupGraphicStore()).
        /// Následně, když je třeba nad touto grafikou vykreslit např. letícího motýla, je vhodné tuto zálohu načíst touto metodou (BackupGraphicLoad(e.Graphics))
        /// - tím se vrátíme do stavu po plném vyrenderování, a pak stačí nakreslit motýla, a je to hned.
        /// </summary>
        /// <param name="target">Cílová grafika, kam se má záloha přenést. Typicky v metodě OnPaintToBuffer() je to parametr e.Graphics.</param>
        protected void BackupGraphicLoad(Graphics target)
        {
            if (BackupGraphicIsReady)
                BackupBuffGraphics.Render(target);
        }
        /// <summary>
        /// Informace o tom, že (true) záložní grafika obsahuje použitelná data, a že je tedy přípustné použít metodu BackupGraphicLoad().
        /// Pokud obsahuje false, pak záložní grafická data nejsou použitelná, a metoda BackupGraphicLoad() vyvolá chybu.
        /// </summary>
        protected bool BackupGraphicIsReady
        {
            get
            {
                return (MainBuffGraphics != null && this.BackupBuffSize == _MaximumBufferSize);
            }
        }
        #endregion
        #region Eventy, řízení metod na potomkovi
        /// <summary>
        /// Fyzický Paint.
        /// Probíhá kdykoliv, když potřebuje okno překreslit.
        /// Aplikační logiku k tomu nepotřebuje, obrázek pro vykreslení má připravený v bufferu. Jen jej přesune na obrazovku.
        /// Aplikační logika kreslí v případě Resize (viz event Dbl_Resize) a v případě, kdy ona sama chce (když si vyvolá metodu Draw()).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlPaint(object sender, PaintEventArgs e)
        {
            // Okno chce vykreslit svoji grafiku - okamžitě je vylijeme do okna z našeho bufferu:
            MainBuffGraphics.Render(e.Graphics);
        }
        /// <summary>
        /// Handler události OnResize: zajistí přípravu nového bufferu, vyvolání kreslení do bufferu, a zobrazení dat z bufferu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlResize(object sender, EventArgs e)
        {
            OnClientSizeBefore();

            // Re-create the graphics buffer for a new window size.
            MainBuffGraphContent.MaximumBuffer = _MaximumBufferSize;
            if (MainBuffGraphics != null)
            {
                MainBuffGraphics.Dispose();
                MainBuffGraphics = null;
            }
            MainBuffGraphics = MainBuffGraphContent.Allocate(this.CreateGraphics(), _CurrentGraphicsRectangle);

            OnClientSizeAfter();
        }
        /// <summary>
        /// Proběhne po změně velikosti, potomek může reagovat na aktuální velikost
        /// </summary>
        protected virtual void OnClientSizeBefore() { }
        /// <summary>
        /// Tuto metodu mají přepisovat potomkové, kteří chtějí reagovat na změnu velikosti.
        /// Až si připraví objekty, mají zavolat base.ResizeAfter(), kde se zajistí vyvolání Draw() => event PaintToBuffer().
        /// </summary>
        protected virtual void OnClientSizeAfter()
        {
            this._Draw(Rectangle.Empty);
        }
        /// <summary>
        /// Po změně Visible.
        /// Při změně na true zajišťuje CheckToolTipInitialized() a Draw()
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                this.Draw();
            }
        }
        /// <summary>
        /// Interní spouštěč metody pro kreslení dat
        /// </summary>
        /// <param name="drawRectangle">
        /// Informace pro kreslící program o rozsahu překreslování.
        /// Nemusí nutně jít o grafický prostor, toto je pouze informace předáváná z parametru metody Draw() do handleru PaintToBuffer().
        /// V servisní třídě se nikdy nepoužije ve významu grafického prostoru.
        /// </param>
        private void _Draw(Rectangle drawRectangle)
        {
            if (_SuppressDrawing) return;              // Potlačené kreslení.
            if (!EnabledDrawing) return;               // Nevhodná situace pro kreslení
            if (this.CurrentlyDrawing) return;         // Už kreslím, nemohu kreslit podruhé
            lock (this.CurrentlyDrawingLock)           // Zamknu si a znovu otestuji hodnotu this.CurrentlyDrawing:
            {
                if (!this.CurrentlyDrawing)
                {
                    this.CurrentlyDrawing = true;
                    if (this.Width > 0 && this.Height > 0 && this.Visible)
                    {
                        PaintEventArgs e = new PaintEventArgs(this.MainBuffGraphics.Graphics, drawRectangle);
                        this.OnPaintToBuffer(this, e);
                        if (this.PaintToBuffer != null) this.PaintToBuffer(this, e);          // Event
                    }
                    if (this.InvokeRequired)
                        this.BeginInvoke(new Action(this.Refresh));
                    else
                        this.Refresh();
                    this.CurrentlyDrawing = false;
                }
            }
        }
        /// <summary>
        /// true když je důvod abych se vykresloval
        /// </summary>
        protected bool EnabledDrawing
        {
            get
            {
                if (IsInDesignMode) return true;       // V design modu se vykreslovat budu
                if (FormExists) return true;           // Když mám form, tak se vykreslovat budu
                return false;                          // Jinak se do vykreslování pouštět nemusíme.
            }
        }
        /// <summary>
        /// Příznak, že právě nyní probíhá kreslení.
        /// Pokud probíhá, pak další požadavky na vykreslení (Invalidate(), Refresh(), Draw()) jsou ignorovány.
        /// </summary>
        protected bool CurrentlyDrawing { get; private set; }
        /// <summary>
        /// Zámek pro nerušené kreslení
        /// </summary>
        private object CurrentlyDrawingLock = new object();
        /// <summary>
        /// true, pokud již existuje Form
        /// </summary>
        protected bool FormExists
        {
            get
            {
                if (_FormExists) return true;
                Form form = this.FindForm();
                if (form == null) return false;
                _FormExists = true;
                return true;
            }
        }
        private bool _FormExists = false;
        /// <summary>
        /// true, pokud jsem já nebo můj parent v design modu. Pak sice nemám žádný Form, ale přesto bych se měl vykreslovat.
        /// </summary>
        internal bool IsInDesignMode { get { return true; } }
        #endregion
        #endregion
        #endregion
        #region Podpora kreslení - konverze barev, kreslení Borderu, Stringu, atd
        #region ColorShift
        /// <summary>
        /// Posune danou barvu o daný posun. Odstín ponechává, posouvá světlost.
        /// </summary>
        /// <param name="color">Vstupní barva</param>
        /// <param name="shift">Posun, zadaný v číslu (+- 255)</param>
        /// <returns>Upravená barva</returns>
        public static Color ColorShift(Color color, int shift)
        {
            int r = _ColorShiftOne(color.R, shift);
            int g = _ColorShiftOne(color.G, shift);
            int b = _ColorShiftOne(color.B, shift);
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Posune danou barvu o daný posun, v každé složce může být jiný.
        /// </summary>
        /// <param name="color">Vstupní barva</param>
        /// <param name="shiftR">Posun složky R, zadaný v číslu (+- 255)</param>
        /// <param name="shiftG">Posun složky G, zadaný v číslu (+- 255)</param>
        /// <param name="shiftB">Posun složky B, zadaný v číslu (+- 255)</param>
        /// <returns>Upravená barva</returns>
        public static Color ColorShift(Color color, int shiftR, int shiftG, int shiftB)
        {
            int r = _ColorShiftOne(color.R, shiftR);
            int g = _ColorShiftOne(color.G, shiftG);
            int b = _ColorShiftOne(color.B, shiftB);
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Posune jednu barevnou složku o daný posun.
        /// </summary>
        /// <param name="colourComponent"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        private static int _ColorShiftOne(byte colourComponent, int shift)
        {
            int newColor = colourComponent + shift;
            return ((newColor < 0) ? 0 : ((newColor > 255) ? 255 : newColor));
        }
        #endregion
        #region Border
        /// <summary>
        /// Vykreslí rámeček okolo celého this controlu
        /// </summary>
        public void DrawBorder(Graphics graphics)
        {
            DrawBorder(graphics, new Point(0, 0), this.Size, this.BorderStyle, this.BorderSides);
        }
        /// <summary>
        /// Metoda vrátí aktuální prostor pro kreslení po odečtení Borderu od Size
        /// </summary>
        /// <returns></returns>
        private Rectangle _GetClientBounds()
        {
            Padding borderPadding = _GetBorderPadding();
            Rectangle clientBounds = new Rectangle(
                borderPadding.Left,
                borderPadding.Top,
                this.Width - borderPadding.Left - borderPadding.Right,
                this.Height - borderPadding.Top - borderPadding.Bottom);
            return clientBounds;
        }
        /// <summary>
        /// Metoda vrátí šířku Borderu na jednotlivých okrajích prvku
        /// </summary>
        /// <returns></returns>
        private Padding _GetBorderPadding()
        {
            int borderWidth = this.BorderWidth;

            Border3DSide sides = this.BorderSides;
            Padding padd = new Padding();
            padd.Top = (((sides & Border3DSide.Top) == Border3DSide.Top) ? borderWidth : 0);
            padd.Left = (((sides & Border3DSide.Left) == Border3DSide.Left) ? borderWidth : 0);
            padd.Right = (((sides & Border3DSide.Right) == Border3DSide.Right) ? borderWidth : 0);
            padd.Bottom = (((sides & Border3DSide.Bottom) == Border3DSide.Bottom) ? borderWidth : 0);
            return padd;
        }
        /// <summary>
        /// Vykreslí rámeček do specifikovaného prostoru
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="rectangle">Prostor</param>
        public static void DrawBorder(Graphics graphics, Rectangle rectangle)
        {
            DrawBorder(graphics, rectangle.Location, rectangle.Size, DevExpress.XtraEditors.Controls.BorderStyles.Default, Border3DSide.All);
        }
        /// <summary>
        /// Vykreslí rámeček okolo celého předaného controlu
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="begin">Počátek</param>
        /// <param name="size">Velikost</param>
        public static void DrawBorder(Graphics graphics, Point begin, Size size)
        {
            DrawBorder(graphics, begin, size, DevExpress.XtraEditors.Controls.BorderStyles.Default, Border3DSide.All);
        }
        /// <summary>
        /// Vykreslí rámeček okolo celého předaného controlu, v daném stylu
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="begin">Počátek</param>
        /// <param name="size">Velikost</param>
        /// <param name="borderStyle">Styl borderu, default = BorderStyle.Fixed3D</param>
        public static void DrawBorder(Graphics graphics, Point begin, Size size, DevExpress.XtraEditors.Controls.BorderStyles borderStyle)
        {
            DrawBorder(graphics, begin, size, borderStyle, Border3DSide.All);
        }
        /// <summary>
        /// Vykreslí rámeček okolo celého předaného controlu, v daném stylu
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="begin">Počátek</param>
        /// <param name="size">Velikost</param>
        /// <param name="borderStyle">Styl borderu, default = BorderStyle.Fixed3D</param>
        /// <param name="borderSide">Kreslené okraje borderu, default = Border3DSide.All</param>
        public static void DrawBorder(Graphics graphics, Point begin, Size size, DevExpress.XtraEditors.Controls.BorderStyles borderStyle, Border3DSide borderSide)
        {
            if (borderStyle == DevExpress.XtraEditors.Controls.BorderStyles.NoBorder) return;
            Point target = new Point(begin.X + size.Width - 1, begin.Y + size.Height - 1);
            Color controlColor = SystemColors.ControlDark;
            using (Pen border = new Pen(controlColor, 1F))
            {
                switch (borderStyle)
                {
                    case DevExpress.XtraEditors.Controls.BorderStyles.Simple:
                    case DevExpress.XtraEditors.Controls.BorderStyles.Flat:
                    case DevExpress.XtraEditors.Controls.BorderStyles.HotFlat:
                    case DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat:
                        border.Color = Color.Black;
                        graphics.DrawRectangle(border, begin.X, begin.Y, size.Width - 1, size.Height - 1);
                        break;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Style3D:
                    case DevExpress.XtraEditors.Controls.BorderStyles.Office2003:
                    case DevExpress.XtraEditors.Controls.BorderStyles.Default:
                        border.Color = SystemColors.ControlDark;
                        graphics.DrawLine(border, begin.X, begin.Y, target.X - 1, begin.Y);                 // Vnější horní čára
                        graphics.DrawLine(border, begin.X, begin.Y, begin.X, target.Y - 1);                 // Vnější levá čára
                        border.Color = SystemColors.ControlDarkDark;
                        graphics.DrawLine(border, begin.X + 1, begin.Y + 1, target.X - 2, begin.Y + 1);     // Vnitřní horní čára
                        graphics.DrawLine(border, begin.X + 1, begin.Y + 1, begin.X + 1, target.Y - 2);     // Vnitřní levá čára
                        border.Color = SystemColors.ControlLightLight;
                        graphics.DrawLine(border, target.X, begin.Y, target.X, target.Y);                   // Vnější pravá čára
                        graphics.DrawLine(border, begin.X, target.Y, target.X, target.Y);                   // Vnější dolní čára
                        border.Color = SystemColors.Control;
                        graphics.DrawLine(border, target.X - 1, begin.Y + 1, target.X - 1, target.Y - 1);   // Vnitřní pravá čára
                        graphics.DrawLine(border, begin.X + 1, target.Y - 1, target.X - 1, target.Y - 1);   // Vnitřní dolní čára
                        break;
                }
            }
        }
        #endregion
        #region String
        /// <summary>
        /// Do daného prostoru vepíše text, se zarovnáním
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="brush"></param>
        /// <param name="textArea"></param>
        /// <param name="alignment"></param>
        /// <param name="stringFormat"></param>
        public static void DrawString(Graphics graphics, string text, Font font, Brush brush, Rectangle textArea, ContentAlignment alignment, StringFormatFlags stringFormat)
        {
            StringFormat format = new StringFormat(stringFormat);
            bool isVertical = ((stringFormat & StringFormatFlags.DirectionVertical) == StringFormatFlags.DirectionVertical);
            int textWidth = (isVertical ? textArea.Height : textArea.Width);
            SizeF textSize = graphics.MeasureString(text, font, textWidth, format);
            RectangleF alignArea = AlignSizeIntoArea(textArea, textSize, alignment);
            graphics.DrawString(text, font, brush, alignArea, format);
        }
        /// <summary>
        /// Zarovná určitý prostor do daného prostoru v daném zarovnání.
        /// </summary>
        /// <param name="outerArea">Vnější prostor</param>
        /// <param name="innerSize">Vnitřní rámec</param>
        /// <param name="alignment">Styl zarovnání</param>
        /// <returns>Vnitřní rámec, zarovnaný do vnějšího prostoru</returns>
        public static RectangleF AlignSizeIntoArea(RectangleF outerArea, SizeF innerSize, ContentAlignment alignment)
        {
            // Zarovnání spočívá v určení pointu, kde se začne s psaním:
            PointF origin = PointF.Empty;
            // Svisle:
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    origin.Y = outerArea.Y;
                    break;
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.MiddleRight:
                    origin.Y = (outerArea.Y + 0.5F * (outerArea.Height - innerSize.Height));
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    origin.Y = (outerArea.Y + (outerArea.Height - innerSize.Height));
                    break;
            }

            // Vodorovně:
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    origin.X = outerArea.X;
                    break;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    origin.X = (outerArea.X + 0.5F * (outerArea.Width - innerSize.Width));
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    origin.X = (outerArea.X + (outerArea.Width - innerSize.Width));
                    break;
            }

            return new RectangleF(origin, innerSize);
        }

        #endregion
        #region Find control with focused
        /// <summary>
        /// Najde a vrátí objekt Control.
        /// Inspirace: http://windowsclient.net/blogs/faqs/archive/2006/05/26/how-do-i-find-out-which-control-has-focus.aspx
        /// </summary>
        /// <returns></returns>
        public static Control FindControlWithFocus()
        {
            Control focusControl = null;
            IntPtr focusHandle = GetFocus();
            if (focusHandle != IntPtr.Zero)
                // returns null if handle is not to a .NET control
                focusControl = Control.FromHandle(focusHandle);
            return focusControl;
        }
        // Import GetFocus() from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
        internal static extern IntPtr GetFocus();
        #endregion
        #endregion
    }
    #endregion
    #region DxGraphicsPanel : DevExpress panel s podporou pro hlídání změny skinu a DPI a Size
    /// <summary>
    /// <see cref="DxGraphicsPanel"/> : DevExpress panel s podporou pro hlídání změny skinu a DPI a Size, 
    /// řízení <see cref="BorderStyle"/> a <see cref="AllowTransparency"/>.
    /// Je bázovou třídou jak pro 
    /// </summary>
    public class DxGraphicsPanel : DevExpress.XtraEditors.PanelControl, IListenerZoomChange, IListenerStyleChanged, IListenerApplicationIdle
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxGraphicsPanel()
        {
            this.__CurrentDpi = DxComponent.DesignDpi;
            this.__LastDpi = DxComponent.DesignDpi;           // ??? anebo   0 ?
            this.__LastClientSize = null;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
            this.AllowTransparency = false;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DxComponent.RegisterListener(this);
            this.SizeChanged += _ControlSizeChanged;
            this.ClientSizeChanged += _ControlClientSizeChanged;
        }
        /// <summary>
        /// OnPaint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            OnBeforePaint();
            base.OnPaint(e);
        }
        /// <summary>
        /// Proběhne před bázovou metodu OnPaint
        /// </summary>
        protected virtual void OnBeforePaint() { }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnregisterListener(this);
            DestroyContent();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Zruší veškerý svůj obsah v procesu Dispose. Volá base.DestroyContent() !!!
        /// </summary>
        protected virtual void DestroyContent()
        {
        }
        /// <summary>
        /// Gets or sets the panel's border style.
        /// </summary>
        public new DevExpress.XtraEditors.Controls.BorderStyles BorderStyle
        {
            get { return base.BorderStyle; }
            set
            {
                if (value != base.BorderStyle)
                {
                    base.BorderStyle = value;
                    this.__BorderStyle = value;
                    OnBorderStyleChanged();
                    BorderStyleChanged?.Invoke(this, EventArgs.Empty);
                    _DetectClientSizeChange();
                }
            }
        }
        /// <summary>
        /// Nikdy nesetovat přímo sem, odsud jen číst...
        /// </summary>
        private DevExpress.XtraEditors.Controls.BorderStyles __BorderStyle;
        /// <summary>
        /// Při změně <see cref="BorderStyle"/>
        /// </summary>
        protected virtual void OnBorderStyleChanged() { }
        /// <summary>
        /// Při změně <see cref="BorderStyle"/>
        /// </summary>
        public event EventHandler BorderStyleChanged;
        /// <summary>
        /// Počet pixelů aktuálního rámečku (na každé straně)
        /// </summary>
        public int BorderWidth
        {
            get
            {
                switch (this.__BorderStyle)
                {
                    case DevExpress.XtraEditors.Controls.BorderStyles.NoBorder: return 0;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Simple: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Flat: return 2;
                    case DevExpress.XtraEditors.Controls.BorderStyles.HotFlat: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Style3D: return 2;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Office2003: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Default: return 1;
                }
                return 0;
            }
        }
        /// <summary>
        /// Povolit průhlednost panelu?
        /// </summary>
        public bool AllowTransparency { get { return __AllowTransparency; } set { __AllowTransparency = value; } }
        private bool __AllowTransparency;
        /// <summary>
        /// Povolit průhlednost panelu?
        /// Hodnotu čte DevExpress při zpracování panelu. V DxPanelControl ji umožníme nastavit.
        /// </summary>
        protected override bool AllowTotalTransparency { get { return __AllowTransparency; /* namísto base.AllowTotalTransparency */ } }
        #endregion
        #region HasMouse a InteractiveState
        /// <summary>
        /// Panel má na sobě myš?
        /// Pozor, tato property signalizuje, že myš se nachází přímo na panelu na místě, kde není žádný Child control!
        /// Pokud na panelu je Child control a myš přejde na tento control, pak myš "odchází" z panelu a zde v <see cref="HasMouse"/> bude false!
        /// Lze ale testovat property <see cref="IsMouseOnPanel"/>.
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Obsahuje true, pokud myš se nachází nad klientským prostorem this panelemu, nebo nad některým z jeho Child prvků.
        /// Testuje polohu myši a pozici panelu.
        /// </summary>
        public bool IsMouseOnPanel
        {
            get
            {
                var mousePosition = this.PointToClient(Control.MousePosition);
                return this.ClientRectangle.Contains(mousePosition);
            }
        }
        /// <summary>
        /// Interaktivní stav tohoto prvku z hlediska Enabled, Mouse, Focus, Selected
        /// </summary>
        public virtual DxInteractiveState InteractiveState
        {
            get
            {
                if (!this.Enabled) return DxInteractiveState.Disabled;
                DxInteractiveState state = DxInteractiveState.Enabled;
                if (IsMouseOnPanel) state |= DxInteractiveState.HasMouse;
                if (this.Focused) state |= DxInteractiveState.HasFocus;
                return state;
            }
        }
        /// <summary>
        /// DevExpress vyjádření interaktivního stavu tohoto panelu, vychází z <see cref="InteractiveState"/>
        /// </summary>
        public virtual ObjectState InteractiveObjectState
        {
            get
            {
                var interactiveState = this.InteractiveState;
                if (interactiveState.HasFlag(DxInteractiveState.Disabled)) return ObjectState.Disabled;

                ObjectState state = ObjectState.Normal;
                if (interactiveState.HasFlag(DxInteractiveState.HasMouse)) state |= ObjectState.Hot;
                if (interactiveState.HasFlag(DxInteractiveState.HasFocus)) state |= ObjectState.Selected;
                return state;
            }
        }
        #endregion
        #region ClientSize
        /// <summary>
        /// Po změně velikosti ClientArea controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlClientSizeChanged(object sender, EventArgs e)
        {
            _DetectClientSizeChange();
        }
        /// <summary>
        /// Po změně velikosti celého controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlSizeChanged(object sender, EventArgs e)
        {
            _DetectClientSizeChange();
        }
        /// <summary>
        /// Detekuje změnu velikosti, po změně si ukládá aktuální hodnotu a volá eventy
        /// </summary>
        private void _DetectClientSizeChange()
        {
            _CurrentClientBoundsInvalidate();

            var currentSize = CurrentClientSize;
            var lastSize = __LastClientSize;
            if (lastSize.HasValue && lastSize.Value == currentSize) return;
            __LastClientSize = currentSize;

            this.OnCurrentClientSizeChanged();
            this.CurrentClientSizeChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se po změně velikosti <see cref="CurrentClientSize"/>
        /// </summary>
        protected virtual void OnCurrentClientSizeChanged() { }
        /// <summary>
        /// Volá se po změně velikosti <see cref="CurrentClientSize"/>
        /// </summary>
        public event EventHandler CurrentClientSizeChanged;
        /// <summary>
        /// Souřadnice vnitřního prostoru panelu.
        /// Pokud Panel má nějaký Border, který je vykreslován uvnitř <see cref="Control.ClientRectangle"/>, 
        /// pak <see cref="CurrentClientBounds"/> je o tento Border zmenšený.
        /// </summary>
        public Rectangle CurrentClientBounds
        {
            get
            {
                if (!__CurrentClientBounds.HasValue)
                {
                    var size = Size;
                    var clientSize = ClientSize;
                    var borderWidth = BorderWidth;
                    if (clientSize.Width == size.Width && borderWidth > 0)
                    {   // DevExpress s oblibou tvrdí, že ClientSize == Size, a přitom mají Border nenulové velikosti. Pak by se nám obsah kreslil přes Border.
                        int b2 = 2 * borderWidth;
                        __CurrentClientBounds = new Rectangle(borderWidth, borderWidth, size.Width - b2, size.Height - b2);
                    }
                    else
                    {
                        __CurrentClientBounds = new Rectangle(Point.Empty, clientSize);
                    }
                }
                return __CurrentClientBounds.Value;
            }
        }
        /// <summary>
        /// Invaliduje cache <see cref="CurrentClientBounds"/>; volá se po jakékoli změně, která ovlivní velikost vnitřního prostoru = tedy změny Border i Size
        /// </summary>
        private void _CurrentClientBoundsInvalidate()
        {
            __CurrentClientBounds = null;
        }
        /// <summary>
        /// cache velikosti <see cref="CurrentClientBounds"/>
        /// </summary>
        private Rectangle? __CurrentClientBounds;
        /// <summary>
        /// Aktuální velikost klientského prostoru po odečtení Borderu, je tedy uvnitř <see cref="CurrentClientBounds"/>
        /// </summary>
        protected Size CurrentClientSize { get { return CurrentClientBounds.Size; } }
        /// <summary>
        /// Posledně evidovaná velikost <see cref="CurrentClientSize"/>
        /// </summary>
        private Size? __LastClientSize;
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); DeviceDpiCheck(false); OnContentSizeChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); DeviceDpiCheck(false); OnContentSizeChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        /// <summary>
        /// Po změně Parenta prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DeviceDpiCheck(true);
        }
        /// <summary>
        /// Po změně DPI v parentu prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            DeviceDpiCheck(true);
        }
        /// <summary>
        /// Při invalidaci prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            DeviceDpiCheck(true);
            base.OnInvalidated(e);
        }
        /// <summary>
        /// Tento háček je vyvolán po jakékoli akci, která může vést k přepočtu vnitřních velikostí controlů.
        /// Je volán: po změně Zoomu, po změně Skinu, po změně DPI hostitelského okna.
        /// <para/>
        /// Potomek by v této metodě měl provést přepočty velikosti svých controlů, pokud závisejí na Zoomu a DPI (a možná Skinu) (rozdílnost DesignSize a CurrentSize).
        /// <para/>
        /// Metoda není volána po změně velikosti controlu samotného ani po změně ClientBounds, ta změna nezakládá důvod k přepočtu velikosti obsahu
        /// </summary>
        protected virtual void OnContentSizeChanged() { }
        #endregion
        #region DPI - podpora pro MultiMonitory s různým rozlišením / pro jiné DPI než designové
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        public int CurrentDpi { get { return this.__CurrentDpi; } }
        /// <summary>
        /// Aktuální hodnota DeviceDpi z formuláře / anebo z this controlu
        /// </summary>
        private int __CurrentDpi;
        /// <summary>
        /// Znovu načte hodnotu DeviceDpi z formuláře / anebo z this controlu
        /// </summary>
        /// <returns></returns>
        private int _ReloadCurrentDpi()
        {
            __CurrentDpi = this.FindForm()?.DeviceDpi ?? this.DeviceDpi;
            return __CurrentDpi;
        }
        /// <summary>
        /// Hodnota DeviceDpi, pro kterou byly naposledy přepočteny souřadnice prostoru
        /// </summary>
        private int __LastDpi;
        /// <summary>
        /// Obsahuje true, pokud se nyní platné DPI liší od DPI posledně použitého pro přepočet souřadnic
        /// </summary>
        private bool _DpiChanged { get { return (this.__CurrentDpi != this.__LastDpi); } }
        /// <summary>
        /// Ověří, zda nedošlo ke změně DeviceDpi, a pokud ano pak zajistí vyvolání metod <see cref="OnCurrentDpiChanged()"/> a eventu <see cref="CurrentDpiChanged"/>.
        /// Pokud this panel není umístěn na formuláři, neprovede nic, protože DPI nemůže být platné.
        /// </summary>
        /// <param name="callContentSizeChanged">Pokud došlo ke změně DPI, má být volán háček <see cref="OnContentSizeChanged()"/>? Někdy to není nutné, protože se bude volat po této metodě vždy (i bez změny DPI).</param>
        protected void DeviceDpiCheck(bool callContentSizeChanged)
        {
            if (this.FindForm() == null) return;
            var currentDpi = _ReloadCurrentDpi();
            if (_DpiChanged)
            {
                OnCurrentDpiChanged();
                if (callContentSizeChanged)
                    OnContentSizeChanged();
                CurrentDpiChanged?.Invoke(this, EventArgs.Empty);
                __LastDpi = currentDpi;
            }
            _DetectClientSizeChange();
        }
        /// <summary>
        /// Po jakékoli změně DPI
        /// </summary>
        protected virtual void OnCurrentDpiChanged() { }
        /// <summary>
        /// Po jakékoli změně DPI
        /// </summary>
        public event EventHandler CurrentDpiChanged;
        #endregion
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
    }
    #endregion
    #region DxScrollBarX a Y : potomci H- a V- Scrollbaru
    /// <summary>
    /// Vodorovný ScrollBar (Horizontal)
    /// </summary>
    public class DxScrollBarX : DevExpress.XtraEditors.HScrollBar, IDxScrollBar
    {
        /// <summary>
        /// Zajistí provedení akce MouseWheel
        /// </summary>
        /// <param name="e"></param>
        public void DoMouseWheel(MouseEventArgs e)
        {
            this.SimulateMouseWheel(e, Control.ModifierKeys == Keys.Control);
        }
    }
    /// <summary>
    /// Svislý ScrollBar (Vertical)
    /// </summary>
    public class DxScrollBarY : DevExpress.XtraEditors.VScrollBar, IDxScrollBar
    {
        /// <summary>
        /// Zajistí provedení akce MouseWheel
        /// </summary>
        /// <param name="e"></param>
        public void DoMouseWheel(MouseEventArgs e)
        {
            this.SimulateMouseWheel(e, Control.ModifierKeys == Keys.Control);
        }
    }
    /// <summary>
    /// Obecné rozhraní Scrollbaru
    /// </summary>
    public interface IDxScrollBar
    {
        /// <summary>
        /// Viditelnost Scrollbaru
        /// </summary>
        bool Visible { get; set; }
        /// <summary>
        /// Umístění Scrollbaru
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Minimální hodnota na Scrollbaru
        /// </summary>
        int Minimum { get; set; }
        /// <summary>
        /// Maximální hodnota na Scrollbaru
        /// </summary>
        int Maximum { get; set; }
        /// <summary>
        /// Velká změna na Scrollbaru
        /// </summary>
        int LargeChange { get; set; }
        /// <summary>
        /// Malá změna na Scrollbaru
        /// </summary>
        int SmallChange { get; set; }
        /// <summary>
        /// Aktuální hodnota na Scrollbaru
        /// </summary>
        int Value { get; set; }
    }
    #endregion
}
