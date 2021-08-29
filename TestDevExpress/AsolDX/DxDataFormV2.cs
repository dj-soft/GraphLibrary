using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

using DevExpress.XtraEditors;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// DataForm
    /// </summary>
    public class DxDataFormV2 : DxScrollableContent, IDxDataFormV2
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormV2()
        {
            this.DoubleBuffered = true;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;

            InitializeContentPanel();
            InitializeInteractivity();
            InitializeItems();
            InitializePaint();

            InitializeSampleControls();
            // InitializeSampleItems();

            Refresh(RefreshParts.AfterItemsChangedSilent);
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeControls();
            this.InvalidateImageCache();
            this._Items.Clear();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Inicializuje panel <see cref="_ContentPanel"/>
        /// </summary>
        private void InitializeContentPanel()
        {
            this._ContentPanel = new DxPanelBufferedGraphic() { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            this._ContentPanel.LogActive = true;
            this._ContentPanel.Layers = new DxBufferedLayer[] { DxBufferedLayer.AppBackground, DxBufferedLayer.MainLayer };       // Tady můžu přidat další vrstvy, když budu chtít kreslit 'pod' anebo 'nad' hlavní prvky
            this._ContentPanel.PaintLayer += _ContentPanel_PaintLayer;                             // A tady bych pak musel reagovat na kreslení přidaných vrstev...
            this.ContentControl = this._ContentPanel;
        }
        private DxPanelBufferedGraphic _ContentPanel;
        /// <summary>
        /// Souhrn vrstev použitých v this controlu, používá se při invalidaci všech vrstev
        /// </summary>
        private static DxBufferedLayer UsedLayers { get { return DxBufferedLayer.AppBackground | DxBufferedLayer.MainLayer; } }
        /// <summary>
        /// Inicializuje pole prvků
        /// </summary>
        private void InitializeItems()
        {
            _Items = new List<DxDataFormItemV2>();
            _VisibleItems = new List<DxDataFormItemV2>();
            _ContentPadding = new Padding(0);
            _DxSuperToolTip = new DxSuperToolTip() { AcceptTitleOnlyAsValid = false };
        }
        private List<DxDataFormItemV2> _Items;
        private List<DxDataFormItemV2> _VisibleItems;
        private Padding _ContentPadding;
        private DxSuperToolTip _DxSuperToolTip;

        /// <summary>
        /// Okraje kolem vlastních prvků
        /// </summary>
        public Padding ContentPadding 
        { 
            get { return _ContentPadding; } 
            set 
            { 
                _ContentPadding = value; 
                Refresh(RefreshParts.AfterItemsChanged); 
            }
        }
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int ItemsCount { get { return _Items.Count; } }
        /// <summary>
        /// Počet aktuálně viditelných prvků
        /// </summary>
        public int VisibleItemsCount { get { return _VisibleItems.Count; } }
        /// <summary>
        /// Pole prvků.
        /// Přídávání a odebírání řeší metody Add a Remove.
        /// </summary>
        public DxDataFormItemV2[] Items { get { return _Items.ToArray(); } }
        /// <summary>
        /// Souhrnná souřadnice všech prvků v <see cref="Items"/>
        /// </summary>
        public Rectangle? ItemsSummaryBounds { get { return DrawingExtensions.SummaryRectangle(_Items.Select(i => (Rectangle?)i.CurrentBounds)); } }

        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        int IDxDataFormV2.DeviceDpi { get { return this.CurrentDpi; } }
        #endregion
        #region Testovací prvky

        private void InitializeSampleControls()
        {
            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis", TabIndex = 1 };
            _ContentPanel.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 80, 20), Text = "Pokus", TabIndex = 2, TabStop = false };
            _ContentPanel.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(210, 50, 100, 20), Text = "Předvolba", TabIndex = 3, TabStop = false };
            _ContentPanel.Controls.Add(_CheckBox);
        }
        public DxTextEdit TextBox { get { return _TextBox; } }
        private DxLabelControl _Label;
        private DxTextEdit _TextBox;
        private DxCheckEdit _CheckBox;

        public void CreateSampleItems(string[] texts, string[] tooltips, int sampleId, int rowCount)
        {
            _Items.Clear();
            Random random = new Random();
            int textsCount = texts.Length;
            int tooltipsCount = tooltips.Length;

            string text, tooltip;
            int[] widths = null;
            int addY = 0;
            switch (sampleId)
            {
                case 1:
                    widths = new int[] { 140, 260, 40, 300, 120 };
                    addY = 28;
                    break;
                case 2:
                    widths = new int[] { 80, 150, 80, 60, 100, 120, 160, 40, 120, 180, 80, 40, 60, 250 };
                    addY = 22;
                    break;
            }
            int count = rowCount;
            int y = 80;
            int maxX = 0;
            int q;
            for (int r = 0; r < count; r++)
            {
                int x = 20;
                text = $"Řádek {(r + 1)}";
                _Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, text) { DesignBounds = new Rectangle(x, y + 2, 70, 18) });
                x += 80;
                foreach (int width in widths)
                {
                    bool blank = (random.Next(100) == 68);
                    text = (!blank ? texts[random.Next(textsCount)] : "");
                    tooltip = (!blank ? tooltips[random.Next(tooltipsCount)] : "");

                    q = random.Next(100);
                    DataFormItemType itemType = (q < 5 ? DataFormItemType.None :
                                                (q < 10 ? DataFormItemType.CheckBox :
                                                (q < 15 ? DataFormItemType.Button :
                                                DataFormItemType.TextBox)));
                    if (itemType != DataFormItemType.None)
                        _Items.Add(new DxDataFormItemV2(this, itemType, text) { DesignBounds = new Rectangle(x, y, width, 20), ToolTipText = tooltip });
                    x += width + 3;
                }
                maxX = x;
                y += addY;
            }

            Refresh(RefreshParts.Default);
        }
        #endregion
        #region Interaktivita
        private void InitializeInteractivity()
        {
            _CurrentFocusedItem = null;
            InitializeInteractivityKeyboard();
            InitializeInteractivityMouse();
        }
        #region Keyboard a Focus
        private void InitializeInteractivityKeyboard()
        {
            this._CurrentFocusedItem = null;

            Control parent = this._ContentPanel;        // finálně bude parentem this, pak buttony nebudou vidět
            _FocusInButton = DxComponent.CreateDxSimpleButton(142, 5, 140, 25, parent, " Focus in...", tabStop: true);
            _FocusInButton.TabIndex = 0;
            _FocusOutButton = DxComponent.CreateDxSimpleButton(352, 5, 140, 25, parent, "... focus out.", tabStop: true);
            _FocusOutButton.TabIndex = 29;
        }
        private DxSimpleButton _FocusInButton;
        private DxSimpleButton _FocusOutButton;
        private DxDataFormItemV2 _CurrentFocusedItem;
        #endregion
        #region Myš - Move, Down
        private void InitializeInteractivityMouse()
        {
            this._CurrentOnMouseItem = null;
            this._CurrentOnMouseControlSet = null;
            this._CurrentOnMouseControl = null;
            this._ContentPanel.MouseMove += _ContentPanel_MouseMove;
            this._ContentPanel.MouseDown += _ContentPanel_MouseDown;
        }
        /// <summary>
        /// Myš se pohybuje po Content panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
                PrepareItemForPoint(e.Location);
        }
        /// <summary>
        /// Myš klikla v Content panelu = nejspíš bychom měli zařídit přípravu prvku a předání focusu ondoň
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // toto je nonsens, protože když pod myší existuje prvek, pak MouseDown přejde ondoň nativně, a nikoli z _ContentPanel_MouseDown.
            // Sem se dostanu jen tehdy, když myš klikne na panelu _ContentPanel v místě, kde není žádný prvek.
        }
        /// <summary>
        /// Vyhledá prvek nacházející se pod aktuální souřadnicí myši a zajistí pro prvky <see cref="MouseItemLeave()"/> a <see cref="MouseItemEnter(DxDataFormItemV2)"/>.
        /// </summary>
        private void PrepareItemForCurrentPoint()
        {
            Point absoluteLocation = Control.MousePosition;
            Point relativeLocation = _ContentPanel.PointToClient(absoluteLocation);
            PrepareItemForPoint(relativeLocation);
        }
        /// <summary>
        /// Vyhledá prvek nacházející se pod danou souřadnicí myši a zajistí pro prvky <see cref="MouseItemLeave()"/> a <see cref="MouseItemEnter(DxDataFormItemV2)"/>.
        /// </summary>
        /// <param name="location">Souřadnice myši relativně k controlu <see cref="_ContentPanel"/> = reálný parent prvků</param>
        private void PrepareItemForPoint(Point location)
        {
            if (_VisibleItems == null) return;

            DxDataFormItemV2 oldItem = _CurrentOnMouseItem;
            bool oldExists = (oldItem != null);
            bool newExists = _VisibleItems.TryGetLast(i => i.IsVisibleOnPoint(location), out var newItem);

            bool isMouseLeave = (oldExists && (!newExists || (newExists && !Object.ReferenceEquals(oldItem, newItem))));
            if (isMouseLeave)
                MouseItemLeave();

            bool isMouseEnter = (newExists && (!oldExists || (oldExists && !Object.ReferenceEquals(oldItem, newItem))));
            if (isMouseEnter)
                MouseItemEnter(newItem);

            if (isMouseLeave || isMouseEnter)
                this._ContentPanel.InvalidateLayers(DxBufferedLayer.AppBackground);
        }
        /// <summary>
        /// Je voláno při příchodu myši na daný prvek.
        /// </summary>
        /// <param name="item"></param>
        private void MouseItemEnter(DxDataFormItemV2 item)
        {
            if (item.VisibleBounds.HasValue)
            {
                _CurrentOnMouseItem = item;
                _CurrentOnMouseControlSet = GetControlSet(item);
                _CurrentOnMouseControl = _CurrentOnMouseControlSet.GetControlForMouse(item);
                if (!_ContentPanel.IsPaintLayersInProgress)
                {   // V době, kdy probíhá proces Paint, NEBUDU provádět Scroll:
                    bool isScrolled = this.ScrollToBounds(item.CurrentBounds, null, true);
                    if (isScrolled) Refresh(RefreshParts.AfterScroll);
                }
            }
        }
        /// <summary>
        /// Je voláno při opuštění myši z aktuálního prvku.
        /// </summary>
        private void MouseItemLeave()
        {
            var oldControl = _CurrentOnMouseControl;
            if (oldControl != null)
            {
                oldControl.Visible = false;
                oldControl.Location = new Point(0, -20 - oldControl.Height);
                oldControl.Enabled = false;
                if (oldControl is BaseControl baseControl)
                    baseControl.SuperTip = null;
            }
            _CurrentOnMouseItem = null;
            _CurrentOnMouseControlSet = null;
            _CurrentOnMouseControl = null;
        }
        /// <summary>
        /// Datový prvek, nacházející se nyní pod myší
        /// </summary>
        private DxDataFormItemV2 _CurrentOnMouseItem;
        /// <summary>
        /// Datový set popisující control, nacházející se nyní pod myší
        /// </summary>
        private ControlSetInfo _CurrentOnMouseControlSet;
        /// <summary>
        /// Vizuální control, nacházející se nyní pod myší
        /// </summary>
        private System.Windows.Forms.Control _CurrentOnMouseControl;
        #endregion
        #endregion
        #region Refresh
        /// <summary>
        /// Provede refresh prvku
        /// </summary>
        public override void Refresh()
        {
            this.RunInGui(() => _RefreshInGui(RefreshParts.Default | RefreshParts.RefreshControl, UsedLayers));
        }
        /// <summary>
        /// Provede refresh daných částí
        /// </summary>
        /// <param name="refreshParts"></param>
        public void Refresh(RefreshParts refreshParts)
        {
            this.RunInGui(() => _RefreshInGui(refreshParts, UsedLayers));
        }
        /// <summary>
        /// Provede refresh daných částí a vrstev
        /// </summary>
        /// <param name="refreshParts"></param>
        /// <param name="layers"></param>
        public void Refresh(RefreshParts refreshParts, DxBufferedLayer layers)
        {
            this.RunInGui(() => _RefreshInGui(refreshParts, layers));
        }
        /// <summary>
        /// Refresh prováděný v GUI threadu
        /// </summary>
        /// <param name="refreshParts"></param>
        /// <param name="layers"></param>
        private void _RefreshInGui(RefreshParts refreshParts, DxBufferedLayer layers)
        {
            // Protože jsme v GUI threadu, nemusím řešit zamykání hodnot - nikdy nebudou dvě vlákna přistupovat k jednomu objektu současně!
            // Spíše jde o to, že některá část procesu Refresh způsobí požadavek na refresh jiné části, což ale může být část před i za aktuální.
            
            // Zapamatuji si úkoly ke zpracování:
            _RefreshPartCurrentBounds |= refreshParts.HasFlag(RefreshParts.InvalidateCurrentBounds);
            _RefreshPartContentTotalSize |= refreshParts.HasFlag(RefreshParts.RecalculateContentTotalSize);
            _RefreshPartVisibleItems |= refreshParts.HasFlag(RefreshParts.ReloadVisibleItems);
            _RefreshPartCache |= refreshParts.HasFlag(RefreshParts.InvalidateCache);
            _RefreshPartInvalidateControl |= refreshParts.HasFlag(RefreshParts.InvalidateControl);
            _RefreshPartRefreshControl |= refreshParts.HasFlag(RefreshParts.RefreshControl);
            _RefreshLayers |= layers;

            // Pokud právě nyní probíhá Refresh, nebudu jej provádět rekurzivně, ale nechám dřívější iteraci doběhnout a zpracovat nově požadované úkoly:
            if (_RefreshInProgress) return;

            _RefreshInProgress = true;
            try
            {
                while (true)
                {
                    // Autodetekce dalších požadavků:
                    _RefreshPartsAutoDetect();

                    // Pokud nebude co dělat, skončíme:
                    bool doAny = _RefreshPartCurrentBounds || _RefreshPartContentTotalSize || _RefreshPartVisibleItems ||
                                 _RefreshPartCache || _RefreshPartInvalidateControl || _RefreshPartRefreshControl;
                    if (!doAny) return;

                    // Provedeme požadované akce; každá akce nejprve shodí svůj příznak (a teoreticky může nahodit jiný příznak):
                    if (_RefreshPartCurrentBounds) _DoRefreshPartCurrentBounds();
                    if (_RefreshPartContentTotalSize) _DoRefreshPartContentTotalSize();
                    if (_RefreshPartVisibleItems) _DoRefreshPartVisibleItems();
                    if (_RefreshPartCache) _DoRefreshPartCache();
                    if (_RefreshPartInvalidateControl) _DoRefreshPartInvalidateControl();
                    if (_RefreshPartRefreshControl) _DoRefreshPartRefreshControl();
                }
            }
            catch (Exception exc)
            {
                DxComponent.LogAddException(exc);
            }
            finally
            {
                _RefreshInProgress = false;
            }
        }
        /// <summary>
        /// Refresh právě probíhá
        /// </summary>
        public bool IsRefreshInProgress { get { return _RefreshInProgress; } }
        /// <summary>
        /// Refresh právě probíhá
        /// </summary>
        private bool _RefreshInProgress;
        /// <summary>
        /// Zajistit přepočet CurrentBounds v prvcích (=provést InvalidateBounds) = provádí se po změně Zoomu a/nebo DPI
        /// </summary>
        private bool _RefreshPartCurrentBounds;
        /// <summary>
        /// Přepočítat celkovou velikost obsahu
        /// </summary>
        private bool _RefreshPartContentTotalSize;
        /// <summary>
        /// Určit aktuálně viditelné prvky
        /// </summary>
        private bool _RefreshPartVisibleItems;
        /// <summary>
        /// Resetovat cache předvykreslených controlů
        /// </summary>
        private bool _RefreshPartCache;
        /// <summary>
        /// Znovuvykreslit grafiku
        /// </summary>
        private bool _RefreshPartInvalidateControl;
        /// <summary>
        /// Explicitně vyvolat i metodu <see cref="Control.Refresh()"/>
        /// </summary>
        private bool _RefreshPartRefreshControl;
        /// <summary>
        /// Invalidovat tyto vrstvy v rámci _RefreshPartInvalidateControl
        /// </summary>
        private DxBufferedLayer _RefreshLayers;
        /// <summary>
        /// Detekuje automatické požadavky na Refresh
        /// </summary>
        private void _RefreshPartsAutoDetect()
        {
            if (!_RefreshPartCurrentBounds)
            {
                decimal currentZoom = DxComponent.Zoom;
                int currentDpi = this.CurrentDpi;
                if (_LastCalcZoom != currentZoom || _LastCalcDeviceDpi != currentDpi)
                    _RefreshPartCurrentBounds = true;
            }
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateCurrentBounds"/>
        /// </summary>
        private void _DoRefreshPartCurrentBounds()
        {
            _RefreshPartCurrentBounds = false;

            _Items.ForEachExec(i => i.InvalidateBounds());

            _LastCalcZoom = DxComponent.Zoom;
            _LastCalcDeviceDpi = this.CurrentDpi;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.RecalculateContentTotalSize"/>
        /// </summary>
        private void _DoRefreshPartContentTotalSize()
        {
            _RefreshPartContentTotalSize = false;

            Rectangle summaryContentBounds = this.ItemsSummaryBounds ?? Rectangle.Empty;
            var padding = DxComponent.ZoomToGuiInt(this.ContentPadding, this.CurrentDpi);
            int w = padding.Left + summaryContentBounds.Right + padding.Right;
            int h = padding.Top + summaryContentBounds.Bottom + padding.Bottom;
            ContentTotalSize = new Size(w, h);
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.ReloadVisibleItems"/>
        /// </summary>
        private void _DoRefreshPartVisibleItems()
        {
            _RefreshPartVisibleItems = false;

            // Uložím soupis aktuálně viditelných prvků:
            Rectangle virtualBounds = this.ContentVirtualBounds;
            this._VisibleItems = this._Items.Where(i => i.IsVisibleInVirtualBounds(virtualBounds)).ToList();

            // Po změně viditelných prvků je třeba provést MouseLeave = prvek pod myší už není ten, co býval:
            this.MouseItemLeave();

            // A zajistit, že po vykreslení prvků bude aktivován prvek, který se nachází pod myší:
            // Až po vykreslení proto, že proces vykreslení určí aktuální viditelné souřadnice prvků!
            this._AfterPaintSearchActiveItem = true;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateCache"/>
        /// </summary>
        private void _DoRefreshPartCache()
        {
            _RefreshPartCache = false;

            this.InvalidateImageCache();
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateControl"/>
        /// </summary>
        private void _DoRefreshPartInvalidateControl()
        {
            _RefreshPartInvalidateControl = false;

            var layers = this._RefreshLayers;
            if (layers != DxBufferedLayer.None)
                this._ContentPanel.InvalidateLayers(layers);
            this._RefreshLayers = DxBufferedLayer.None;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.RefreshControl"/>
        /// </summary>
        private void _DoRefreshPartRefreshControl()
        {
            _RefreshPartRefreshControl = false;

            base.Refresh();
        }


        // odstranit:

        /// <summary>
        /// Zajistí provedení refreshů pro jednotlivé položky, vše v jednom cyklu přes všechny položky
        /// </summary>
        /// <param name="refreshParts"></param>
        private void RefreshItems(RefreshParts refreshParts)
        {   // Tento algoritmus NENÍ REÁLNĚ POUŽITELNÝ.
            // Proč? Protože nelze současně určovat SUMÁRNÍ VELIKOST všech prvků (ContentTotalSize), a přitom určovat VIDITELNOST prvků.
            // Proč? Protože SUMÁRNÍ VELIKOST ovlivňuje ContentVirtualBounds a přitom její hodnota je známa AŽ PO ZPACOVÁNÍ VŠECH PRVKŮ
            //       a přitom tedy NELZE průběžně určovat VIDITELNOST PRVKŮ v prostoru ContentVirtualBounds,
            //       protože dokud nebudou zmapovaány všechny prvky do ContentTotalSize => ContentVirtualBounds,
            //       tak není možno pouívat ContentVirtualBounds pro vyhodnocení viditelnosti prvku.
            bool doInvalidBounds = refreshParts.HasFlag(RefreshParts.InvalidateCurrentBounds);
            decimal currentZoom = DxComponent.Zoom;
            int currentDpi = this.CurrentDpi;
            if (_LastCalcZoom != currentZoom || _LastCalcDeviceDpi != currentDpi)
                doInvalidBounds = true;
            bool doRecalcTotalSize = refreshParts.HasFlag(RefreshParts.RecalculateContentTotalSize);
            bool doVisibleItems = refreshParts.HasFlag(RefreshParts.ReloadVisibleItems);

            // Pokud není třeba dělat nic s položkami, skončíme hned:
            if (!doInvalidBounds && !doRecalcTotalSize && !doVisibleItems) return;

            int count = _Items.Count;
            Rectangle virtualBounds = this.ContentVirtualBounds;
            List<DxDataFormItemV2> visibleItems = new List<DxDataFormItemV2>();
            int l = 0, t = 0, r = 0, b = 0;
            for (int i = 0; i < count; i++)
            {   // Všechny refreshe, týkající se prvků provedu v jednom cyklu:
                var item = _Items[i];

                // Nejprve invalidace souřadnic; protože jejich následné čtení zajistí jejich přepočet:
                if (doInvalidBounds) item.InvalidateBounds();

                if (doRecalcTotalSize || doVisibleItems)
                {
                    Rectangle currentBounds = item.CurrentBounds;

                    // TotalContentSize:
                    if (doRecalcTotalSize)
                    {
                        if (i == 0)
                        {
                            l = currentBounds.Left;
                            t = currentBounds.Top;
                            r = currentBounds.Right;
                            b = currentBounds.Bottom;
                        }
                        else
                        {
                            if (currentBounds.Left < l) l = currentBounds.Left;
                            if (currentBounds.Top < t) t = currentBounds.Top;
                            if (currentBounds.Right > r) r = currentBounds.Right;
                            if (currentBounds.Bottom > b) b = currentBounds.Bottom;
                        }
                    }

                    // VisibleItems:
                    if (doVisibleItems)
                    {
                        bool isVisible = (item.IsVisible && virtualBounds.Contains(currentBounds, true));
                        if (isVisible)
                            visibleItems.Add(item);
                        if (item.VisibleBounds.HasValue)
                            item.VisibleBounds = null;               // Souřadnici VisibleBounds nulujeme tehdy, když tam je nenulová - tj. i pro Visible prvky. (Protože jejich Visible souřadnici teprve určíme v procesu Paint, tady ji počítat nebudu)
                    }
                }
            }

            // Závěrečné výpočty

            // TotalContentSize:
            if (doRecalcTotalSize)
            {
                Rectangle summaryBounds = Rectangle.FromLTRB(l, t, r, b);
                var padding = DxComponent.ZoomToGuiInt(this.ContentPadding, currentDpi);
                int w = padding.Left + summaryBounds.Right + padding.Right;
                int h = padding.Top + summaryBounds.Bottom + padding.Bottom;
                ContentTotalSize = new Size(w, h);
            }

            // VisibleItems:
            if (doVisibleItems)
            {
                // Uložím soupis aktuálně viditelných prvků:
                this._VisibleItems = visibleItems;

                // Po změně viditelných prvků je třeba provést MouseLeave = prvek pod myší už není ten, co býval:
                this.MouseItemLeave();

                // A zajistit, že po vykreslení prvků bude aktivován prvek, který se nachází pod myší:
                // Až po vykreslení proto, že proces vykreslení určí aktuální viditelné souřadnice prvků!
                this._AfterPaintSearchActiveItem = true;
            }
            _LastCalcZoom = currentZoom;
            _LastCalcDeviceDpi = currentDpi;
        }



        /// <summary>
        /// Po změně DPI je třeba provést kompletní refresh (souřadnice, cache, atd)
        /// </summary>
        protected override void OnDpiChanged()
        {
            base.OnDpiChanged();
            Refresh(RefreshParts.All);
        }
        /// <summary>
        /// Je vyvoláno po změně DPI, po změně Zoomu a po změně skinu. Volá se po přepočtu layoutu.
        /// Může vést k invalidaci interních dat v <see cref="DxScrollableContent.ContentControl"/>.
        /// </summary>
        protected override void OnInvalidateContentAfter()
        {
            base.OnInvalidateContentAfter();
            Refresh(RefreshParts.All);
        }
        /// <summary>
        /// Je voláno pokud dojde ke změně hodnoty <see cref="DxScrollableContent.ContentVirtualBounds"/>, před eventem <see cref="DxScrollableContent.ContentVirtualBoundsChanged"/>
        /// </summary>
        protected override void OnContentVirtualBoundsChanged()
        {
            base.OnContentVirtualBoundsChanged();
            Refresh(RefreshParts.AfterScroll);
        }
        /// <summary>
        /// Systémový Zoom, po který byly posledně přepočteny CurrentBounds
        /// </summary>
        private decimal _LastCalcZoom;
        /// <summary>
        /// DPI this controlu, po který byly posledně přepočteny CurrentBounds
        /// </summary>
        private int _LastCalcDeviceDpi;
        /// <summary>
        /// Položky pro refresh
        /// </summary>
        [Flags]
        public enum RefreshParts
        {
            /// <summary>
            /// Nic
            /// </summary>
            None = 0,
            /// <summary>
            /// Zajistit přepočet CurrentBounds v prvcích (=provést InvalidateBounds) = provádí se po změně Zoomu a/nebo DPI
            /// </summary>
            InvalidateCurrentBounds = 0x0001,
            /// <summary>
            /// Přepočítat celkovou velikost obsahu
            /// </summary>
            RecalculateContentTotalSize = 0x0002,
            /// <summary>
            /// Určit aktuálně viditelné prvky
            /// </summary>
            ReloadVisibleItems = 0x0004,
            /// <summary>
            /// Resetovat cache předvykreslených controlů
            /// </summary>
            InvalidateCache = 0x0010,
            /// <summary>
            /// Znovuvykreslit grafiku
            /// </summary>
            InvalidateControl = 0x0100,
            /// <summary>
            /// Explicitně vyvolat i metodu <see cref="Control.Refresh()"/>
            /// </summary>
            RefreshControl = 0x0200,

            /// <summary>
            /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/>).
            /// Tato hodnota je Silent = neobsahuje <see cref="InvalidateControl"/>.
            /// </summary>
            AfterItemsChangedSilent = RecalculateContentTotalSize | ReloadVisibleItems,
            /// <summary>
            /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>).
            /// Tato hodnota není Silent = obsahuje i invalidaci <see cref="InvalidateControl"/> = překreslení controlu.
            /// <para/>
            /// Toto je standardní refresh.
            /// </summary>
            AfterItemsChanged = RecalculateContentTotalSize | ReloadVisibleItems | InvalidateControl,
            /// <summary>
            /// Po scrollování (<see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>)
            /// </summary>
            AfterScroll = ReloadVisibleItems | InvalidateControl,
            /// <summary>
            /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>).
            /// <para/>
            /// Toto je standardní refresh.
            /// </summary>
            Default = AfterItemsChanged,
            /// <summary>
            /// Všechny akce, včetně invalidace cache (brutální refresh)
            /// </summary>
            All = RecalculateContentTotalSize | ReloadVisibleItems | InvalidateCache | InvalidateControl
        }
        #endregion
        #region Vykreslování a Bitmap cache
        #region Vykreslení celého Contentu
        /// <summary>
        /// Inicializace kreslení
        /// </summary>
        private void InitializePaint()
        {
            _AfterPaintSearchActiveItem = false;
            _PaintingItems = false;
            _PaintLoop = 0L;
            _NextCleanPaintLoop = _CACHECLEAN_OLD_LOOPS + 1;         // První pokus o úklid proběhne po tomto počtu PaintLoop, protože i kdyby bylo potřeba uklidit staré položky, tak stejně nemůže zahodit starší položky - žádné by nevyhovovaly...
        }
        public void TestPerformance(int count, bool forceRefresh)
        {
            _PaintingPerformaceTestCount = count;
            _PaintingPerformaceForceRefresh = forceRefresh;
            Refresh(RefreshParts.InvalidateControl);
            Application.DoEvents();
        }
        /// <summary>
        /// ContentPanel chce vykreslit některou vrstvu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ContentPanel_PaintLayer(object sender, DxBufferedGraphicPaintArgs args)
        {
            switch (args.LayerId)
            {
                case DxBufferedLayer.AppBackground:
                    PaintContentAppBackground(args);
                    break;
                case DxBufferedLayer.MainLayer:
                    PaintContentMainLayer(args);
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí vykreslení aplikačního pozadí (okraj aktivních prvků)
        /// </summary>
        /// <param name="e"></param>
        private void PaintContentAppBackground(DxBufferedGraphicPaintArgs e)
        {
            bool isPainted = false;
            var mouseControl = _CurrentOnMouseControl;
            if (mouseControl != null)
                PaintBorder(e, mouseControl.Bounds, Color.DarkViolet, ref isPainted);

            //  Specifikum bufferované grafiky:
            // - pokud do konkrétní vrstvy jednou něco vepíšu, zůstane to tam (až do nějakého většího refreshe).
            // - pokud v procesu PaintLayer do předaného argumentu do e.Graphics nic nevepíšu, znamená to "beze změny".
            // - pokud tedy nyní nemám žádný control k vykreslení, ale posledně jsem něco vykreslil, měl bych grafiku smazat:
            // - k tomu používám e.LayerUserData
            bool oldPainted = (e.LayerUserData is bool && (bool)e.LayerUserData);
            if (oldPainted && !isPainted)
                e.UseBlankGraphics();
            e.LayerUserData = isPainted;
        }
        private void PaintBorder(DxBufferedGraphicPaintArgs e, Rectangle bounds, Color color, ref bool isPainted)
        {
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color, 32), bounds.Enlarge(3));
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color, 96), bounds.Enlarge(1));
            isPainted = true;
        }
        private void PaintContentMainLayer(DxBufferedGraphicPaintArgs e)
        { 
            bool afterPaintSearchActiveItem = _AfterPaintSearchActiveItem;
            _AfterPaintSearchActiveItem = false;
            _PaintLoop++;
            int cacheCount = ImageCacheCount;
            if (!_PaintingPerformaceForceRefresh && _PaintingPerformaceTestCount <= 1)
                OnPaintContentStandard(e);
            else
                OnPaintContentPerformaceTest(e);

            if (ImageCacheCount > cacheCount || _NextCleanLiable)
                CleanImageCache();

            if (afterPaintSearchActiveItem)
                PrepareItemForCurrentPoint();
        }
        private void OnPaintContentStandard(DxBufferedGraphicPaintArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;
            try
            {
                _PaintingItems = true;
                _VisibleItems.ForEachExec(i => PaintItem(i, e));
            }
            finally
            {
                _PaintingItems = false;
            }
            DxComponent.LogAddLineTime($"DxDataFormV2 Paint Standard() Items: {_VisibleItems.Count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        private void OnPaintContentPerformaceTest(DxBufferedGraphicPaintArgs e)
        {
            bool forceRefresh = _PaintingPerformaceForceRefresh;
            int count = (_PaintingPerformaceTestCount > 1 ? _PaintingPerformaceTestCount : 1);
            var size = this.ContentVisualSize;
            var startTime = DxComponent.LogTimeCurrent;
            try
            {
                _PaintingItems = true;
                int x = 0;
                int y = 0;
                Rectangle? sumBounds = ItemsSummaryBounds;
                int maxX = size.Width - (sumBounds?.Right ?? 0) - 12;
                int maxY = size.Height - (sumBounds?.Bottom ?? 0) - 12;
                while (count > 0)
                {
                    if (forceRefresh) ImageCache = null;

                    Point? offset = null;                  // První smyčka má offset == null, bude tedy generovat VisibleBounds
                    _VisibleItems.ForEachExec(i => PaintItem(i, e, offset));
                    y += 7;
                    if (y >= maxY)
                    {
                        y = 0;
                        x += 36;
                        if (x >= maxX)
                            x = 0;
                    }
                    count--;
                    offset = new Point(x, y);              // Další smyčky budou kreslit posunuté obrázky, ale nebudou ukládat VisibleBounds do prvků
                }
            }
            finally
            {
                _PaintingItems = false;
                _PaintingPerformaceTestCount = 1;
                _PaintingPerformaceForceRefresh = false;
            }
            DxComponent.LogAddLineTime($"DxDataFormV2 Paint PerformanceTest() Items: {_VisibleItems.Count}; Loops: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Provede vykreslení jednoho daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="e"></param>
        /// <param name="offset"></param>
        private void PaintItem(DxDataFormItemV2 item, DxBufferedGraphicPaintArgs e, Point? offset = null)
        {
            var bounds = item.CurrentBounds;
            using (var image = CreateImage(item))
            {
                if (image != null)
                {
                    var visibleOrigin = this.ContentVirtualLocation;
                    Point location = bounds.Location.Sub(visibleOrigin);
                    if (offset.HasValue)
                        location = location.Add(offset.Value);                      // když má offset hodnotu, pak kreslím "posunutý" obraz (jen pro testy), ale nejde o VisibleBounds
                    else
                        item.VisibleBounds = new Rectangle(location, bounds.Size);  // Hodnota offset = null: kreslím "platný obraz", takže si uložím vizuální souřadnici
                    e.Graphics.DrawImage(image, location);
                }
            }
        }
        private bool _AfterPaintSearchActiveItem;
        private long _PaintLoop;
        private bool _PaintingItems = false;
        private int _PaintingPerformaceTestCount;
        private bool _PaintingPerformaceForceRefresh;

        #endregion
        #region Bitmap cache
        /// <summary>
        /// Najde a vrátí <see cref="Image"/> pro obsah daného prvku.
        /// Obrázek hledá nejprve v cache, a pokud tam není pak jej vygeneruje a do cache uloží.
        /// <para/>
        /// POZOR: výstupem této metody je vždy new instance Image, a volající ji musí použít v using { } patternu, jinak zlikviduje paměť.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Image CreateImage(DxDataFormItemV2 item)
        {
            if (ImageCache == null) ImageCache = new Dictionary<string, ImageCacheItem>();

            ControlSetInfo controlSet = GetControlSet(item);
            string key = controlSet.GetKeyToCache(item);
            if (key == null) return null;

            ImageCacheItem imageInfo = null;
            if (ImageCache.TryGetValue(key, out imageInfo))
            {   // Pokud mám Image již uloženu v Cache, je tam uložena jako byte[], a tady z ní vygenerujeme new Image a vrátíme, uživatel ji Disposuje sám:
                imageInfo.AddHit(_PaintLoop);
                return imageInfo.CreateImage();
            }
            else
            {   // Image v cache nemáme, takže nyní vytvoříme skutečný Image s pomocí controlu, obsah Image uložíme jako byte[] do cache, a uživateli vrátíme ten živý obraz:
                // Tímto postupem šetřím čas, protože Image použiju jak pro uložení do Cache, tak pro vykreslení do grafiky v controlu:
                Image image = CreateBitmapForItem(item);
                lock (ImageCache)
                {
                    if (ImageCache.TryGetValue(key, out imageInfo))
                    {
                        imageInfo.AddHit(_PaintLoop);
                    }
                    else
                    {   // Do cache přidám i image == null, tím ušetřím opakované vytváření / testování obrázku.
                        // Pro přidávání aplikuji lock(), i když tedy tahle činnost má probíhat jen v jednom threadu = GUI:
                        imageInfo = new ImageCacheItem(image, _PaintLoop);
                        ImageCache.Add(key, imageInfo);
                    }
                }
                return image;
            }

            
        }
        /// <summary>
        /// Před přidáním nového prvku do cache provede úklid zastaralých prvků v cache, podle potřeby.
        /// <para/>
        /// Časová náročnost: kontroly se provednou v řádu 0.2 milisekundy, reálný úklid (kontroly + odstranění starých a nepoužívaných položek cache) trvá cca 0.8 milisekundy.
        /// Obecně se pracuje s počtem prvků řádově pod 10000, což není problém.
        /// </summary>
        private void CleanImageCache()
        {
            if (ImageCacheCount == 0) return;

            var startTime = DxComponent.LogTimeCurrent;

            long paintLoop = _PaintLoop;
            if (!_NextCleanLiable && paintLoop < _NextCleanPaintLoop)   // Pokud není úklid povinný, a velikost jsme kontrolovali nedávno, nebudu to ještě řešit.
                return;

            long currentCacheSize = ImageCacheSize;
            if (currentCacheSize < _CACHESIZE_MIN)                   // Pokud mám v cache málo dat (pod 4MB), nebudeme uklízet.
            {
                _NextCleanLiable = false;
                _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS;
                if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není nutný. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Budu pracovat jen s těmi prvky, které nebyly dlouho použity:
            long lastLoop = paintLoop - _CACHECLEAN_OLD_LOOPS;
            var items = ImageCache.Where(kvp => kvp.Value.LastPaintLoop <= lastLoop).ToList();
            if (items.Count == 0)                                    // Pokud všechny prvky pravidelně používám, nebudu je zahazovat.
            {
                _NextCleanLiable = false;
                _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
                if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není možný, všechny položky se používají. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Z nich zahodím ty, které byly použity méně než je průměr:
            string[] keys1 = null;
            decimal? averageUse = items.Average(kvp => (decimal)kvp.Value.HitCount);
            if (averageUse.HasValue)
                keys1 = items.Where(kvp => (decimal)kvp.Value.HitCount < averageUse.Value).Select(kvp => kvp.Key).ToArray();

            CleanImageCache(keys1);

            long cleanedCacheSize = ImageCacheSize;
            if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; Odstraněno {keys1?.Length ?? 0} položek; Po provedení úklidu: {cleanedCacheSize}B. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);

            // Co a jak příště:
            if (cleanedCacheSize < _CACHESIZE_MIN)                   // Pokud byl tento úklid úspěšný z hlediska minimální paměti, pak příští úklid bude až za daný počet cyklů
            {
                _NextCleanLiable = false;
                _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS;
            }
            else                                                     // Tento úklid byl potřebný (z hlediska času nebo velikosti paměti), ale nedostali jsme se pod _CACHESIZE_MIN:
            {
                _NextCleanLiable = true;                             // Příště budeme volat úklid povinně!
                if (cleanedCacheSize < currentCacheSize)             // Sice jsme neuklidili pod minimum, ale něco jsme uklidili: příští kontrolu zaplánujeme o něco dříve:
                    _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
            }
        }
        /// <summary>
        /// Z cache vyhodí záznamy pro dané klíče. 
        /// Tato metoda si v případě potřeby (tj. když jsou zadané nějaké klíče) zamkne cache na dobu úklidu.
        /// </summary>
        /// <param name="keys"></param>
        private void CleanImageCache(string[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            lock (ImageCache)
            {
                foreach (string key in keys)
                {
                    if (ImageCache.ContainsKey(key))
                        ImageCache.Remove(key);
                }
            }
        }
        /// <summary>
        /// Po kterém vykreslení <see cref="_PaintLoop"/> budeme dělat další úklid
        /// </summary>
        private long _NextCleanPaintLoop;
        /// <summary>
        /// Po příštím vykreslení zavolat úklid cache i když nedojde k navýšení počtu prvků v cache, protože poslední úklid byl potřebný ale ne zcela úspěšný
        /// </summary>
        private bool _NextCleanLiable;
        /// <summary>
        /// Jaká velikost cache nám nepřekáží? Pokud bude cache menší, nebude probíhat její čištění.
        /// </summary>
        private const long _CACHESIZE_MIN = 1572864L;            // Pro provoz nechme 6MB:  6291456L;      Pro testování úklidu je vhodné mít 1.5MB = 1572864L
        /// <summary>
        /// Po kolika vykresleních controlu budeme ochotni provést další úklid cache?
        /// </summary>
        private const long _CACHECLEAN_AFTER_LOOPS = 6L;
        /// <summary>
        /// Po tolika vykresleních provedeme kontrolu velikosti cache když poslední kontrola neuklidila pod _CACHESIZE_MIN
        /// </summary>
        private const long _CACHECLEAN_AFTER_LOOPS_SMALL = 4L;
        /// <summary>
        /// Jak staré prvky z cache můžeme vyhodit? Počet vykreslovacích cyklů, kdy byl prvek použit.
        /// Pokud prvek nebyl posledních (NNN) cyklů potřeba, můžeme uvažovat o jeho zahození.
        /// </summary>
        private const long _CACHECLEAN_OLD_LOOPS = 12;
        /// <summary>
        /// Zruší veškerý obsah z cache uložených Image <see cref="ImageCache"/>, kde jsou uloženy obrázky pro jednotlivé ne-aktivní controly...
        /// Je nutno volat po změně skinu nebo Zoomu.
        /// </summary>
        private void InvalidateImageCache()
        {
            ImageCache = null;
        }
        /// <summary>
        /// Počet prvků v cache
        /// </summary>
        private int ImageCacheCount { get { return ImageCache?.Count ?? 0; } }
        /// <summary>
        /// Sumární velikost dat v cache
        /// </summary>
        private long ImageCacheSize { get { return (ImageCache != null ? ImageCache.Values.Select(i => i.Length).Sum() : 0L); } }
        /// <summary>
        /// Cache obrázků controlů
        /// </summary>
        private Dictionary<string, ImageCacheItem> ImageCache;
        private class ImageCacheItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="image"></param>
            /// <param name="paintLoop"></param>
            public ImageCacheItem(Image image, long paintLoop)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);    // PNG: čas v testu 20-24ms, spotřeba paměti 0.5MB.    BMP: čas 18-20ms, pamět 5MB.    TIFF: čas 50ms, paměť 1.5MB
                    _ImageContent = ms.ToArray();
                }
                this.HitCount = 1L;
                this.LastPaintLoop = paintLoop;
            }
            private byte[] _ImageContent;
            /// <summary>
            /// Metoda vrací new Image vytvořený z this položky cache
            /// </summary>
            /// <returns></returns>
            public Image CreateImage()
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(_ImageContent))
                    return Image.FromStream(ms);
            }
            /// <summary>
            /// Počet byte uložených jako Image v této položce cache
            /// </summary>
            public long Length { get { return _ImageContent.Length; } } 
            /// <summary>
            /// Počet použití této položky cache
            /// </summary>
            public long HitCount { get; private set; }
            /// <summary>
            /// Poslední číslo smyčky, kdy byl prvek použit
            /// </summary>
            public long LastPaintLoop { get; private set; }
            /// <summary>
            /// Přidá jednu trefu v použití prvku (nápočet statistiky prvku)
            /// </summary>
            public void AddHit(long paintLoop)
            { 
                HitCount++;
                if (LastPaintLoop < paintLoop)
                    LastPaintLoop = paintLoop;
            }
        }
        #endregion
        #endregion
        #region Fyzické controly - tvorba, správa, vykreslení bitmapy skrze control
        /// <summary>
        /// Uvolní z paměti veškerá data fyzických controlů
        /// </summary>
        private void DisposeControls()
        {
            if (_DataFormControls == null) return;
            foreach (ControlSetInfo controlSet in _DataFormControls.Values)
                controlSet.Dispose();
            _DataFormControls.Clear();
        }
        private ControlSetInfo GetControlSet(DxDataFormItemV2 item)
        {
            if (_DataFormControls == null) _DataFormControls = new Dictionary<DataFormItemType, ControlSetInfo>();
            var dataFormControls = _DataFormControls;

            ControlSetInfo controlSet;
            DataFormItemType itemType = item.ItemType;
            if (!dataFormControls.TryGetValue(itemType, out controlSet))
            {
                controlSet = new ControlSetInfo(this, itemType);
                dataFormControls.Add(itemType, controlSet);
            }
            return controlSet;

            //Control control;
            //if (!controlSet.TryGetValue(mode, out control))
            //{
            //    control = (itemType == DataFormItemType.Label ? (Control)new DxLabelControl() :
            //              (itemType == DataFormItemType.TextBox ? (Control)new DxTextEdit() :
            //              (itemType == DataFormItemType.CheckBox ? (Control)new DxCheckEdit() :
            //              (itemType == DataFormItemType.Button ? (Control)new DxSimpleButton() : (Control)null))));

            //    if (control != null)
            //    {

            //        Control parent = (mode == DxDataFormControlMode.Focused ? (Control)_ContentPanel :
            //                         (mode == DxDataFormControlMode.HotMouse ? (Control)_ContentPanel :
            //                         (mode == DxDataFormControlMode.Inactive ? (Control)this : (Control)null)));

            //        if (parent != null)
            //        {
            //            control.Location = new Point(5, 5);
            //            control.Visible = false;
            //            parent.Controls.Add(control);
            //        }
            //    }
            //    controlSet.Add(mode, control);
            //}
            //return control;
        }
        private Image CreateBitmapForItem(DxDataFormItemV2 item)
        {
            /*   Časomíra:

               1. Vykreslení bitmapy z paměti do Graphics                    10 mikrosekund
               2. Nastavení souřadnic (Bounds) do controlu                  300 mikrosekund
               3. Vložení textu (Text) do controlu                          150 mikrosekund
               4. Zrušení Selection v TextBoxu                                5 mikrosekund
               5. Vykreslení controlu do bitmapy                            480 mikrosekund

           */

            ControlSetInfo controlSet = GetControlSet(item);
            Control drawControl = controlSet.GetControlForDraw(item);

            int w = drawControl.Width;
            int h = drawControl.Height;
            Bitmap image = new Bitmap(w, h);
            drawControl.DrawToBitmap(image, new Rectangle(0, 0, w, h));

            return image;
        }

        private Dictionary<DataFormItemType, ControlSetInfo> _DataFormControls;
        /// <summary>
        /// Instance třídy, která obhospodařuje jeden typ (<see cref="DataFormItemType"/>) vizuálního controlu, a má až tři instance (Draw, Mouse, Focus)
        /// </summary>
        private class ControlSetInfo : IDisposable
        {
            #region Konstruktor
            /// <summary>
            /// Vytvoří <see cref="ControlSetInfo"/> pro daný typ controlu
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="itemType"></param>
            public ControlSetInfo(IDxDataFormV2 owner, DataFormItemType itemType)
            {
                _Owner = owner;
                _ItemType = itemType;
                switch (itemType)
                {
                    case DataFormItemType.Label:
                        _CreateControlFunction = _LabelCreate;
                        _GetKeyFunction = _LabelGetKey;
                        _FillControlAction = _LabelFill;
                        _ReadControlAction = _LabelRead;
                        break;
                    case DataFormItemType.TextBox:
                        _CreateControlFunction = _TextBoxCreate;
                        _GetKeyFunction = _TextBoxGetKey;
                        _FillControlAction = _TextBoxFill;
                        _ReadControlAction = _TextBoxRead;
                        break;
                    case DataFormItemType.CheckBox:
                        _CreateControlFunction = _CheckBoxCreate;
                        _GetKeyFunction = _CheckBoxGetKey;
                        _FillControlAction = _CheckBoxFill;
                        _ReadControlAction = _CheckBoxRead;
                        break;
                    case DataFormItemType.Button:
                        _CreateControlFunction = _ButtonCreate;
                        _GetKeyFunction = _ButtonGetKey;
                        _FillControlAction = _ButtonFill;
                        _ReadControlAction = _ButtonRead;
                        break;
                    default:
                        throw new ArgumentException($"Není možno vytvořit 'ControlSetInfo' pro typ prvku '{itemType}'.");
                }
                _Disposed = false;
            }
            /// <summary>
            /// Dispose prvků
            /// </summary>
            public void Dispose()
            {
                DisposeControl(ref _ControlDraw, ControlUseMode.Draw);
                DisposeControl(ref _ControlMouse, ControlUseMode.Mouse);
                DisposeControl(ref _ControlFocus, ControlUseMode.Focus);

                _CreateControlFunction = null;
                _FillControlAction = null;
                _ReadControlAction = null;

                _Disposed = true;
            }
            /// <summary>
            /// Pokud je objekt disposován, vyhodí chybu.
            /// </summary>
            private void CheckNonDisposed()
            {
                if (_Disposed) throw new InvalidOperationException($"Nelze pracovat s objektem 'ControlSetInfo', protože je zrušen.");
            }
            private IDxDataFormV2 _Owner;
            private DataFormItemType _ItemType;
            private Func<Control> _CreateControlFunction;
            private Func<DxDataFormItemV2, string> _GetKeyFunction;
            private Action<DxDataFormItemV2, Control, ControlUseMode> _FillControlAction;
            private Action<DxDataFormItemV2, Control> _ReadControlAction;
            private bool _Disposed;
            #endregion
            #region Label
            private Control _LabelCreate() { return new DxLabelControl(); }
            private string _LabelGetKey(DxDataFormItemV2 item) 
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _LabelFill(DxDataFormItemV2 item, Control control, ControlUseMode mode)
            {
                if (!(control is DxLabelControl label)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxLabelControl).Name}.");
                CommonFill(item, label, mode);
            }
            private void _LabelRead(DxDataFormItemV2 item, Control control)
            { }
            #endregion
            #region TextBox
            private Control _TextBoxCreate() { return new DxTextEdit(); }
            private string _TextBoxGetKey(DxDataFormItemV2 item)
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _TextBoxFill(DxDataFormItemV2 item, Control control, ControlUseMode mode)
            {
                if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
                CommonFill(item, textEdit, mode);
                textEdit.DeselectAll();
                textEdit.SelectionStart = 0;
            }
            private void _TextBoxRead(DxDataFormItemV2 item, Control control)
            { }
            #endregion
            // EditBox
            // SpinnerBox
            #region CheckBox
            private Control _CheckBoxCreate() { return new DxCheckEdit(); }
            private string _CheckBoxGetKey(DxDataFormItemV2 item)
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _CheckBoxFill(DxDataFormItemV2 item, Control control, ControlUseMode mode)
            {
                if (!(control is DxCheckEdit checkEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxCheckEdit).Name}.");
                CommonFill(item, checkEdit, mode);
            }
            private void _CheckBoxRead(DxDataFormItemV2 item, Control control)
            { }
            #endregion
            // BreadCrumb
            // ComboBoxList
            // ComboBoxEdit
            // ListView
            // TreeView
            // RadioButtonBox
            #region Button
            private Control _ButtonCreate() { return new DxSimpleButton(); }
            private string _ButtonGetKey(DxDataFormItemV2 item)
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _ButtonFill(DxDataFormItemV2 item, Control control, ControlUseMode mode)
            {
                if (!(control is DxSimpleButton button)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxSimpleButton).Name}.");
                CommonFill(item, button, mode);
            }
            private void _ButtonRead(DxDataFormItemV2 item, Control control)
            { }
            #endregion
            // CheckButton
            // DropDownButton
            // Image
            #region Společné metody pro všechny typy prvků
            /// <summary>
            /// Naplní obecně platné hodnoty do daného controlu
            /// </summary>
            /// <param name="item"></param>
            /// <param name="control"></param>
            /// <param name="mode"></param>
            private void CommonFill(DxDataFormItemV2 item, BaseControl control, ControlUseMode mode)
            {
                Rectangle bounds = item.CurrentBounds;
                if (mode == ControlUseMode.Draw)
                {
                    bounds.Location = new Point(4, 4);
                }
                else if (item.VisibleBounds.HasValue)
                {
                    bounds.Location = item.VisibleBounds.Value.Location;
                }

                control.Text = item.Text;
                control.Enabled = item.Enabled;
                control.SetBounds(bounds);
                control.Visible = true;
                control.SuperTip = GetSuperTip(item, mode);
            }
            /// <summary>
            /// Vrátí instanci <see cref="DxSuperToolTip"/> připravenou pro daný prvek a daný režim. Může vrátit null.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="mode"></param>
            /// <returns></returns>
            private DxSuperToolTip GetSuperTip(DxDataFormItemV2 item, ControlUseMode mode)
            {
                if (mode != ControlUseMode.Mouse) return null;
                var superTip = _Owner.DxSuperToolTip;
                superTip.LoadValues(item);
                if (!superTip.IsValid) return null;
                return superTip;
            }
            /// <summary>
            /// Vrátí standardní klíč daného prvku do ImageCache
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            private string GetStandardKeyForItem(DxDataFormItemV2 item)
            {
                var size = item.CurrentBounds.Size;
                string text = item.Text ?? "";
                string type = ((int)item.ItemType).ToString();
                string key = $"{size.Width}.{size.Height};{type}::{text}";
                return key;
            }
            #endregion
            #region Získání a naplnění controlu z datového Itemu, a reverzní zpětné načtení hodnot z controlu do datového Itemu
            internal Control GetControlForDraw(DxDataFormItemV2 item)
            {
                CheckNonDisposed();
                if (_ControlDraw == null)
                    _ControlDraw = _CreateControl(ControlUseMode.Draw);
                _FillControl(item, _ControlDraw, ControlUseMode.Draw);
                return _ControlDraw;
            }
            internal Control GetControlForMouse(DxDataFormItemV2 item)
            {
                CheckNonDisposed();
                if (_ControlMouse == null)
                    _ControlMouse = _CreateControl(ControlUseMode.Mouse);
                _FillControl(item, _ControlMouse, ControlUseMode.Mouse);
                return _ControlMouse;
            }
            internal Control GetControlForFocus(DxDataFormItemV2 item)
            {
                CheckNonDisposed();
                if (_ControlFocus == null)
                    _ControlFocus = _CreateControl(ControlUseMode.Focus);
                _FillControl(item, _ControlFocus, ControlUseMode.Focus);
                return _ControlFocus;
            }
            /// <summary>
            /// Metoda vrátí stringový klíč do ImageCache pro konkrétní prvek.
            /// Vrácený klíč zohledňuje všechny potřebné a specifické hodnoty z konkrétního prvku.
            /// Je tedy jisté, že dva různé objekty, které vrátí stejný klíč, budou mít stejný vzhled.
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            internal string GetKeyToCache(DxDataFormItemV2 item)
            {
                CheckNonDisposed();
                string key = _GetKeyFunction?.Invoke(item);
                return key;
            }
            /// <summary>
            /// Vytvoří new instanci zdejšího controlu, umístí ji do neviditelné souřadnice, přidá do Ownera a vrátí.
            /// </summary>
            /// <returns></returns>
            private Control _CreateControl(ControlUseMode mode)
            {
                var control = _CreateControlFunction();
                Size size = control.Size;
                Point location = new Point(10, -10 - size.Height);
                Rectangle bounds = new Rectangle(location, size);
                control.Visible = false;
                control.SetBounds(bounds);
                bool addToBackground = (mode == ControlUseMode.Draw);
                _Owner.AddControl(control, addToBackground);
                return control;
            }
            private void _FillControl(DxDataFormItemV2 item, Control control, ControlUseMode mode)
            {
                _FillControlAction(item, control, mode);
                control.TabIndex = 10;

                //// source.SetBounds(bounds);                  // Nastavím správné umístění, to kvůli obrázkům na pozadí panelu (různé skiny!), aby obrázky odpovídaly aktuální pozici...
                //Rectangle sourceBounds = new Rectangle(4, 4, bounds.Width, bounds.Height);
                //source.SetBounds(sourceBounds);
                //source.Text = item.Text;

                //var size = source.Size;
                //if (size != bounds.Size)
                //{
                //    item.CurrentSize = size;
                //    bounds = item.CurrentBounds;
                //}

            }
            /// <summary>
            /// Odebere daný control z Ownera, disposuje jej a nulluje ref proměnnou.
            /// </summary>
            /// <param name="control"></param>
            /// <param name="mode"></param>
            private void DisposeControl(ref Control control, ControlUseMode mode)
            {
                if (control == null) return;
                bool removeFromBackground = (mode == ControlUseMode.Draw);
                _Owner.RemoveControl(control, removeFromBackground);
                control.Dispose();
                control = null;
            }
            private Control _ControlDraw;
            private Control _ControlMouse;
            private Control _ControlFocus;
            #endregion
        }
        private enum ControlUseMode { None, Draw, Mouse, Focus }
        /// <summary>
        /// Sdílený objekt ToolTipu do všech controlů
        /// </summary>
        DxSuperToolTip IDxDataFormV2.DxSuperToolTip { get { return this._DxSuperToolTip; } }
        /// <summary>
        /// Daný control přidá do panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="addToBackground"></param>
        void IDxDataFormV2.AddControl(Control control, bool addToBackground)
        {
            if (control == null) return;
            if (addToBackground)
                this.Controls.Add(control);
            else
                this._ContentPanel.Controls.Add(control);
        }
        /// <summary>
        /// Daný control odebere z panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="addToBackground"></param>
        void IDxDataFormV2.RemoveControl(Control control, bool addToBackground)
        {
            if (control == null) return;
            if (addToBackground)
            {
                if (this.Controls.Contains(control))
                    this.Controls.Remove(control);
            }
            else
            {
                if (this._ContentPanel.Controls.Contains(control))
                    this._ContentPanel.Controls.Remove(control);
            }
        }
        #endregion
    }
    /// <summary>
    /// Rozhraní na interní věci DataForm panelu
    /// </summary>
    public interface IDxDataFormV2
    {
        /// <summary>
        /// DPI panelu
        /// </summary>
        int DeviceDpi { get; }
        /// <summary>
        /// Sdílený objekt ToolTipu do všech controlů
        /// </summary>
        DxSuperToolTip DxSuperToolTip { get; }
        /// <summary>
        /// Daný control přidá do panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="addToBackground"></param>
        void AddControl(Control control, bool addToBackground);
        /// <summary>
        /// Daný control odebere z panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="addToBackground"></param>
        void RemoveControl(Control control, bool addToBackground);
    }
   
    /// <summary>
    /// Třída reprezentující jeden každý vizuální prvek v <see cref="DxDataFormV2"/>.
    /// </summary>
    public class DxDataFormItemV2 : DataTextItem
    {
        public DxDataFormItemV2(IDxDataFormV2 owner, DataFormItemType itemType, string text)
            : base()
        {
            _Owner = owner;
            _ItemType = itemType;
            Text = text;
            IsVisible = true;
            HotTrackingEnabled = (itemType != DataFormItemType.Label);
        }

        private IDxDataFormV2 _Owner;
        private DataFormItemType _ItemType;
        private string _Text;

        public DataFormItemType ItemType { get { return _ItemType; } }
        public bool IsVisible { get; set; }

        #region Vzhled
        /// <summary>
        /// Prvek bude podsvícen při pohybu myší nad ním
        /// </summary>
        public bool HotTrackingEnabled { get; set; }

        #endregion

        #region Souřadnice designové, aktuální, viditelné
        /// <summary>
        /// Souřadnice designové, v logických koordinátech (kde bod {0,0} je absolutní počátek, bez posunu ScrollBarem).
        /// Typicky se vztahují k 96 DPI.
        /// Setování hodnoty provede invalidaci fyzických souřadnic <see cref="InvalidateBounds()"/>, tedy hodnot <see cref="CurrentBounds"/> a <see cref="VisibleBounds"/>.
        /// </summary>
        public Rectangle DesignBounds 
        { 
            get { return __DesignBounds; } 
            set 
            {
                __DesignBounds = value;
                InvalidateBounds();
            }
        }
        /// <summary>
        /// Invaliduje souřadnice <see cref="CurrentBounds"/> a <see cref="VisibleBounds"/>.
        /// </summary>
        public void InvalidateBounds()
        {
            __CurrentBounds = null;
            __VisibleBounds = null;
        }
        /// <summary>
        /// Souřadnice designové, v logických koordinátech (kde bod {0,0} je absolutní počátek, bez posunu ScrollBarem).
        /// </summary>
        private Rectangle __DesignBounds;
        /// <summary>
        /// Aktuální logické koordináty - přepočtené z <see cref="DesignBounds"/> na aktuálně platné DPI.
        /// Tato souřadnice není posunuta ScrollBarem. 
        /// Posunutá vizuální souřadnice je v <see cref="VisibleBounds"/>.
        /// </summary>
        public Rectangle CurrentBounds { get { this.CheckDesignBounds(); return __CurrentBounds.Value; } }
        /// <summary>
        /// Aktuální velikost prvku. Lze setovat (nezmění se umístění = <see cref="CurrentBounds"/>.Location).
        /// <para/>
        /// Setujme opatrně a jen v případě nutné potřeby, typicky tehdy, když konkrétní vizuální control nechce akceptovat předepsanou velikost (např. výška textboxu v jiném než očekávaném fontu).
        /// Vložená hodnota zde zůstane (a bude obsažena i v <see cref="CurrentBounds"/>) do doby, než se změní Zoom nebo Skin aplikace.
        /// </summary>
        public Size CurrentSize 
        { 
            get { this.CheckDesignBounds(); return __CurrentBounds.Value.Size; }
            set
            {
                this.CheckDesignBounds();
                __CurrentBounds = new Rectangle(__CurrentBounds.Value.Location, value);
            }
        }
        /// <summary>
        /// Úložiště pro <see cref="CurrentBounds"/>, po přepočtech DPI
        /// </summary>
        private Rectangle? __CurrentBounds;
        /// <summary>
        /// Zajistí, že souřadnice <see cref="__CurrentBounds"/> budou platné k souřadnicím designovým a k hodnotám aktuálním DPI
        /// </summary>
        private void CheckDesignBounds()
        {
            if (!__CurrentBounds.HasValue)
                __CurrentBounds = DxComponent.ZoomToGuiInt(__DesignBounds, _Owner.DeviceDpi);
        }
        /// <summary>
        /// Fyzické pixelové souřadnice tohoto prvku na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Jde o vizuální souřadnice v koordinátech controlu, odpovídají např. pohybu myši.
        /// Může být null, pak prvek není zobrazen. Null je i po invalidaci <see cref="InvalidateBounds()"/>.
        /// Tuto hodnotu ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleBounds { get { return __VisibleBounds; } set { __VisibleBounds = value; } }
        private Rectangle? __VisibleBounds;
        /// <summary>
        /// Vrátí true, pokud this prvek se nachází v rámci dané virtuální souřadnice.
        /// Tedy pokud souřadnice <see cref="CurrentBounds"/> se alespoň zčásti nacházejí uvnitř souřadného prostoru dle parametru <paramref name="virtualBounds"/>.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public bool IsVisibleInVirtualBounds(Rectangle virtualBounds)
        {
            return (IsVisible && virtualBounds.Contains(CurrentBounds, true));
        }
        /// <summary>
        /// Vrátí true, pokud this prvek má nastaveny viditelné souřadnice v <see cref="VisibleBounds"/> 
        /// a pokud daný bod (souřadný systém shodný s <see cref="VisibleBounds"/>) se nachází v prostoru this prvku
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsVisibleOnPoint(Point point)
        {
            return (IsVisible && VisibleBounds.HasValue && VisibleBounds.Value.Contains(point));
        }
        #endregion
    }
}
