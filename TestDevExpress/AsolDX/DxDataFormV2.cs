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
    public class DxDataFormV2 : DxScrollableContent, IDxDataFormV2
    {
        #region Konstruktor

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

            Refresh(RefreshParts.RecalculateContentTotalSize | RefreshParts.ReloadVisibleItems);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.InvalidateImageCache();
            this._Items.Clear();
        }
        /// <summary>
        /// Inicializuje panel <see cref="_ContentPanel"/>
        /// </summary>
        private void InitializeContentPanel()
        {
            this._ContentPanel = new DxPanelBufferedGraphic() { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            this._ContentPanel.LogActive = true;
            this._ContentPanel.Layers = new DxBufferedLayer[] { DxBufferedLayer.MainLayer };       // Tady můžu přidat další vrstvy, když budu chtít kreslit 'pod' anebo 'nad' hlavní prvky
            this._ContentPanel.PaintLayer += _ContentPanel_PaintLayer;                             // A tady bych pak musel reagovat na kreslení přidaných vrstev...
            this.ContentControl = this._ContentPanel;
        }
        private DxPanelBufferedGraphic _ContentPanel;
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
                Refresh(RefreshParts.RecalculateContentTotalSize | RefreshParts.ReloadVisibleItems | RefreshParts.InvalidateControl); 
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
            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis" };
            _ContentPanel.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 80, 20), Text = "Pokus" };
            _ContentPanel.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(210, 50, 100, 20), Text = "Předvolba" };
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

            Refresh(RefreshParts.RecalculateContentTotalSize | RefreshParts.ReloadVisibleItems | RefreshParts.InvalidateCache);
        }
        #endregion
        #region Interaktivita
        private void InitializeInteractivity()
        {
            _CurrentFocusedItem = null;
            _CurrentOnMouseItem = null;
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
        /// Myš klikla v Content panelu = nejspíš bychom měli zařídit přípravi prvku a předání focusu ondoň
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void PrepareItemForCurrentPoint()
        {
            Point absoluteLocation = Control.MousePosition;
            Point relativeLocation = _ContentPanel.PointToClient(absoluteLocation);
            PrepareItemForPoint(relativeLocation);
        }

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
        }
        private void MouseItemEnter(DxDataFormItemV2 item)
        {
            if (item.VisibleBounds.HasValue)
            {
                var newControl = GetControl(item.ItemType, DxDataFormControlMode.HotMouse);
                string text = item.Text;
                newControl.SetBounds(item.VisibleBounds.Value);
                newControl.Text = text;
                newControl.Enabled = true;
                newControl.Visible = true;
                if (newControl is BaseControl baseControl)
                {
                    _DxSuperToolTip.LoadValues(item);
                    if (_DxSuperToolTip.IsValid)
                        baseControl.SuperTip = _DxSuperToolTip;
                }

                _CurrentOnMouseControl = newControl;
                _CurrentOnMouseItem = item;
            }
        }
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
            _CurrentOnMouseControl = null;
            _CurrentOnMouseItem = null;
        }
        private DxDataFormItemV2 _CurrentFocusedItem;

        /// <summary>
        /// Vizuální control nacházející se nyní pod myší
        /// </summary>
        private System.Windows.Forms.Control _CurrentOnMouseControl;
        /// <summary>
        /// Prvek nacházející se nyní pod myší
        /// </summary>
        private DxDataFormItemV2 _CurrentOnMouseItem;
        #endregion
        #region Refresh
        /// <summary>
        /// Provede refresh daných částí
        /// </summary>
        /// <param name="refreshParts"></param>
        public void Refresh(RefreshParts refreshParts)
        {
            bool isRecalc = refreshParts.HasFlag(RefreshParts.RecalculateContentTotalSize);
            bool isVisibl = refreshParts.HasFlag(RefreshParts.ReloadVisibleItems);
            if (isRecalc && isVisibl)                                                    // Pokud jsou oba požadavky společně,
                this.RecalculateContentAndVisibleItems();                                //  pak provedu specifickou metodu, která enumeruje prvky jen jedenkrát
            else if (isRecalc)
                this.RecalculateContentTotalSize();                                      // Anebo jednoúčelová metoda
            else if (isVisibl)
                this.PrepareVisibleItems();                                              // Anebo jiná jednoúčelová metoda

            if (refreshParts.HasFlag(RefreshParts.InvalidateCache))
                this.InvalidateImageCache();

            if (refreshParts.HasFlag(RefreshParts.InvalidateControl))
                this._ContentPanel.InvalidateLayers(DxBufferedLayer.MainLayer);
        }
        /// <summary>
        /// Po změně DPI je třeba provést kompletní refresh (souřadnice, cache, atd)
        /// </summary>
        protected override void OnDpiChanged()
        {
            base.OnDpiChanged();
            Refresh(RefreshParts.RecalculateContentTotalSize | RefreshParts.ReloadVisibleItems | RefreshParts.InvalidateCache | RefreshParts.InvalidateControl);
        }
        /// <summary>
        /// Je vyvoláno po změně DPI, po změně Zoomu a po změně skinu. Volá se po přepočtu layoutu.
        /// Může vést k invalidaci interních dat v <see cref="DxScrollableContent.ContentControl"/>.
        /// </summary>
        protected override void OnInvalidateContentAfter()
        {
            base.OnInvalidateContentAfter();
            Refresh(RefreshParts.RecalculateContentTotalSize | RefreshParts.ReloadVisibleItems | RefreshParts.InvalidateCache | RefreshParts.InvalidateControl);
        }
        /// <summary>
        /// Je voláno pokud dojde ke změně hodnoty <see cref="DxScrollableContent.ContentVirtualBounds"/>, před eventem <see cref="DxScrollableContent.ContentVirtualBoundsChanged"/>
        /// </summary>
        protected override void OnContentVirtualBoundsChanged()
        {
            base.OnContentVirtualBoundsChanged();
            Refresh(RefreshParts.ReloadVisibleItems | RefreshParts.InvalidateControl);
        }
        /// <summary>
        /// Optimalizovaná metoda, která v jedné enumeraci vyhodnotí sumu souřadnic i viditelné prvky
        /// </summary>
        private void RecalculateContentAndVisibleItems()
        {
#warning TODO pro optimální běh doprogramovat metodu, která v jedné enumeraci vyhodnotí sumu souřadnic i viditelné prvky !!!
            RecalculateContentTotalSize();
            PrepareVisibleItems();
        }
        /// <summary>
        /// Určí seznam aktuálně viditelných prvků
        /// </summary>
        private void PrepareVisibleItems()
        {
            // Z prvků, které jsou viditelné nyní, odstraním vizuální souřadnici:
            //  některé prvky budou sice viditelné i nadále, ale budou překresleny a přitom dostanou platnou souřadnici,
            //  a jiné prvky viditelné nebudou = těm je třeba zrušit viditelnou souřadnici (ale je zbytečné ji rušit všem prvkům v poli _Items):
            if (this._VisibleItems != null)
                this._VisibleItems.ForEachExec(i => i.VisibleBounds = null);

            // Najdu a uložím soupis aktuálně viditelných prvků:
            this._VisibleItems = GetVisibleItems(this.ContentVirtualBounds);

            // Po změně viditelných prvků je třeba provést MouseLeave = prvek pod myší už není ten, co býval:
            this.MouseItemLeave();

            // A zajistit, že po vykreslení prvků bude aktivován prvek, který se nachází pod myší:
            // Až po vykreslení proto, že proces vykreslení určí aktuální viditelné souřadnice prvků!
            this._AfterPaintSearchActiveItem = true;
        }
        /// <summary>
        /// Vrátí List prvků z pole <see cref="Items"/>, které jsou alespoň zčásti viditelné v aktuálním prostoru
        /// </summary>
        /// <returns></returns>
        private List<DxDataFormItemV2> GetVisibleItems(Rectangle bounds)
        {
            List<DxDataFormItemV2> visibleItems = _Items.Where(i => i.IsVisible && bounds.Contains(i.CurrentBounds, true)).ToList();
            return visibleItems;
        }
        /// <summary>
        /// Podle aktuálního seznamu prvků a jejich velikostí určí sumární velikost obsahu.
        /// Za poslední prvek přidává okraj definovaný v 
        /// </summary>
        private void RecalculateContentTotalSize()
        {
            Rectangle summaryBounds = ItemsSummaryBounds ?? Rectangle.Empty;
            var padding = this.ContentPadding;
            int w = padding.Left + summaryBounds.Right + padding.Right;
            int h = padding.Top + summaryBounds.Bottom + padding.Bottom;
            ContentTotalSize = new Size(w, h);
        }
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
            /// Přepočítat celkovou velikost obsahu
            /// </summary>
            RecalculateContentTotalSize = 0x0001,
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
            InvalidateControl = 0x0100
        }
        #endregion
        #region Vykreslování a Bitmap cache
        #region Vykreslení celého Contentu
        private void InitializePaint()
        {
            _AfterPaintSearchActiveItem = false;
            _PaintingItems = false;
            _PaintLoop = 0L;
        }
        public void TestPerformance(int count, bool forceRefresh)
        {
            _PaintingPerformaceTestCount = count;
            _PaintingPerformaceForceRefresh = forceRefresh;
            this.Refresh(RefreshParts.InvalidateControl);
            Application.DoEvents();
        }

        private void _ContentPanel_PaintLayer(object sender, DxBufferedGraphicPaintArgs args)
        {
            switch (args.LayerId)
            {
                case DxBufferedLayer.MainLayer:
                    PaintContentMainLayer(args);
                    break;
            }
        }
        private void PaintContentMainLayer(DxBufferedGraphicPaintArgs e)
        { 
            bool afterPaintSearchActiveItem = _AfterPaintSearchActiveItem;
            _AfterPaintSearchActiveItem = false;
            _PaintLoop++;
            if (!_PaintingPerformaceForceRefresh && _PaintingPerformaceTestCount <= 1)
                OnPaintContentStandard(e);
            else
                OnPaintContentPerformaceTest(e);

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
        #region Fyzické controly - tvorba, správa, vykreslení bitmapy skrze control
        private Control GetControl(DataFormItemType itemType, DxDataFormControlMode mode)
        {
            if (_DataFormControls == null) _DataFormControls = new Dictionary<DataFormItemType, Dictionary<DxDataFormControlMode, Control>>();
            var dataFormControls = _DataFormControls;
            Dictionary<DxDataFormControlMode, Control> modeControls;
            if (!dataFormControls.TryGetValue(itemType, out modeControls))
            {
                modeControls = new Dictionary<DxDataFormControlMode, Control>();
                dataFormControls.Add(itemType, modeControls);
            }
            Control control;
            if (!modeControls.TryGetValue(mode, out control))
            {
                control = (itemType == DataFormItemType.Label ? (Control)new DxLabelControl() :
                          (itemType == DataFormItemType.TextBox ? (Control)new DxTextEdit() :
                          (itemType == DataFormItemType.CheckBox ? (Control)new DxCheckEdit() :
                          (itemType == DataFormItemType.Button ? (Control)new DxSimpleButton() : (Control)null))));

                if (control != null)
                {

                    Control parent = (mode == DxDataFormControlMode.Focused ? (Control)_ContentPanel :
                                     (mode == DxDataFormControlMode.HotMouse ? (Control)_ContentPanel :
                                     (mode == DxDataFormControlMode.Inactive ? (Control)this : (Control)null)));

                    if (parent != null)
                    {
                        control.Location = new Point(5, 5);
                        control.Visible = false;
                        parent.Controls.Add(control);
                    }
                }
                modeControls.Add(mode, control);
            }
            return control;
        }
        private Image CreateBitmapForItem(DxDataFormItemV2 item)
        {
            /*   Časomíra:
                           Získat control   Vložit Bounds    Vložit Text   Selection   DrawToBitmap   PaintImage      Čas mikrosekund
           0. Nic:                                                                                                   :        6  (režie smyčky)
           1. PaintImage                                                                                  ANO        :       15
           2. Bez Bounds        ANO                                                                                  :       25
           3. Bez Text          ANO             ANO                                                                  :      320
           4. Bez Selection     ANO             ANO             ANO                                                  :      470
           5. Bez obrázku       ANO             ANO             ANO           ANO                                    :      470
           6. Bez kreslení      ANO             ANO             ANO           ANO                         ANO        :      500
           7. Komplet           ANO             ANO             ANO           ANO         ANO             ANO        :      990

           */
            var source = GetControl(item.ItemType, DxDataFormControlMode.Inactive);
            if (source == null) return null;

            var bounds = item.CurrentBounds;

            Cursor cursor = null;

            // source.SetBounds(bounds);                  // Nastavím správné umístění, to kvůli obrázkům na pozadí panelu (různé skiny!), aby obrázky odpovídaly aktuální pozici...
            Rectangle sourceBounds = new Rectangle(4, 4, bounds.Width, bounds.Height);
            source.SetBounds(sourceBounds);
            source.Text = item.Text;

            var size = source.Size;
            if (size != bounds.Size)
            {
                item.CurrentSize = size;
                bounds = item.CurrentBounds;
            }
            int w = size.Width;
            int h = size.Height;
            Bitmap image = new Bitmap(w, h);
            if (source is DxTextEdit textEdit)
            {
                textEdit.DeselectAll();
                textEdit.SelectionStart = 0;
                cursor = Cursors.IBeam;
            }
            source.DrawToBitmap(image, new Rectangle(0, 0, w, h));
            //if (storeValues)
            //{
            //    DefaultCursor = cursor ?? Cursors.Default;
            //    ActiveBounds = bounds.Sub(source.Margin);
            //}
            return image;
        }

        private Dictionary<DataFormItemType, Dictionary<DxDataFormControlMode, Control>> _DataFormControls;
        #endregion
        #region Bitmap cache
        /// <summary>
        /// Najde a vrátí <see cref="Image"/> pro obsah daného prvku.
        /// Obrázek hledá nejprve v cache, a pokud tam není pak jej vygeneruje a do cache uloží.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Image CreateImage(DxDataFormItemV2 item)
        {
            if (ImageCache == null) ImageCache = new Dictionary<string, ImageCacheItem>();

            string key = item.ContentKey;
            if (key == null) return null;

            ImageCacheItem imageInfo = null;
            if (ImageCache.TryGetValue(key, out imageInfo))
            {
                imageInfo.AddHit();
            }
            else
            {
                using (Image image = CreateBitmapForItem(item))
                {
                    lock (ImageCache)
                    {
                        if (ImageCache.TryGetValue(key, out imageInfo))
                        {
                            imageInfo.AddHit();
                        }
                        else
                        {   // Do cache přidám i image == null, tím ušetřím opakované vytváření / testování obrázku.
                            // Pro přidávání aplikuji lock(), i když tedy tahle činnost má probíhat jen v jednom threadu = GUI:
                            CleanImageCache();
                            imageInfo = new ImageCacheItem(image);
                            ImageCache.Add(key, imageInfo);
                        }
                    }
                }
            }
            return imageInfo.CreateImage();
        }
        /// <summary>
        /// Před přidáním nového prvku do cache provede úklid zastaralých prvků v cache, podle potřeby.
        /// Volá se za stavu, kdy cache <see cref="ImageCache"/> je locknutá.
        /// </summary>
        private void CleanImageCache()
        { }
        /// <summary>
        /// Zruší veškerý obsah z cache uložených Image <see cref="ImageCache"/>, kde jsou uloženy obrázky pro jednotlivé ne-aktivní controly...
        /// Je nutno volat po změně skinu nebo Zoomu.
        /// </summary>
        private void InvalidateImageCache()
        {
            ImageCache = null;
        }
        /// <summary>
        /// Zruší informaci v cache uložených Image pro jeden prvek
        /// </summary>
        /// <param name="item"></param>
        private void InvalidateImageCache(DxDataFormItemV2 item)
        {
            if (ImageCache == null) return;

            string key = item.ContentKey;
            if (key == null) return;

            lock (ImageCache)
            {
                if (ImageCache.ContainsKey(key))
                    ImageCache.Remove(key);
            }
        }
        private Dictionary<string, ImageCacheItem> ImageCache;
        private class ImageCacheItem
        {
            public ImageCacheItem(Image image)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    _ImageContent = ms.ToArray();
                }
                this.HitCount = 1L;
            }
            private byte[] _ImageContent;
            public Image CreateImage()
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(_ImageContent))
                    return Image.FromStream(ms);
            }
            public int Length { get { return _ImageContent.Length; } } 
            public long HitCount { get; private set; }
            /// <summary>
            /// Přidá jednu trefu v použití prvku (nápočet statistiky prvku)
            /// </summary>
            public void AddHit() { HitCount++; }
        }
        #endregion
        #endregion
    }
    public interface IDxDataFormV2
    {
        int DeviceDpi { get; }
    }
    public enum DxDataFormControlMode
    {
        None,
        Inactive,
        HotMouse,
        Focused
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
        }

        private IDxDataFormV2 _Owner;
        private DataFormItemType _ItemType;
        private string _Text;

        public DataFormItemType ItemType { get { return _ItemType; } }
        public bool IsVisible { get; set; }

        /// <summary>
        /// Klíč obsahu. 
        /// Pokud dva různé prvky budou mít shodný klíč obsahu, pak oba prvky vypadají shodně.
        /// Klíč pokrývá: Typ objektu, velikost, obsah, vzhled, barvy, písmo.
        /// Slouží k tomu, aby v paměti bylo možno uložit sadu Image pro jednotlivé prvky, ale nikoli zbytečně opakované shodné Image.
        /// </summary>
        public string ContentKey
        {
            get
            {
                var size = CurrentBounds.Size;
                string text = Text ?? "";
                string key = $"{size.Width}.{size.Height}.{__CurrentDpi};{ItemType}::{text}";
                return key;
            }
        }

        #region Souřadnice designové, aktuální, viditelné
        /// <summary>
        /// Souřadnice designové, v logických koordinátech (kde bod {0,0} je absolutní počátek, bez posunu ScrollBarem).
        /// Typicky se vztahují k 96 DPI.
        /// </summary>
        public Rectangle DesignBounds 
        { 
            get { return __DesignBounds; } 
            set 
            {
                var currentDpi = _Owner.DeviceDpi;
                __DesignBounds = value;
                __CurrentBounds = value;
                __CurrentDpi = currentDpi;
            }
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
        /// Hodnota DPI, ke které jsou přepočteny souřadnice <see cref="CurrentBounds"/> a <see cref="VisibleBounds"/>.
        /// </summary>
        private int __CurrentDpi;
        /// <summary>
        /// Zajistí, že souřadnice <see cref="__CurrentBounds"/> budou platné k souřadnicím designovým a k hodnotám DPI designovým a aktuálním
        /// </summary>
        private void CheckDesignBounds()
        {
            var ownerDpi = _Owner.DeviceDpi;
            var currentDpi = __CurrentDpi;
            if (__CurrentBounds.HasValue && currentDpi == ownerDpi) return;
            __CurrentBounds = __DesignBounds.ConvertToDpi(ownerDpi);
            __CurrentDpi = ownerDpi;
        }
        /// <summary>
        /// Fyzické pixelové souřadnice tohoto prvku na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Může být null, pak prvek není zobrazen.
        /// Tuto hodnotu ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleBounds { get; set; }
        /// <summary>
        /// Vrátí true, pokud this prvek má nastaveny viditelné souřadnice v <see cref="VisibleBounds"/> 
        /// a pokud daný bod se nachází ve viditelné oblasti
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsVisibleOnPoint(Point point)
        {
            return (VisibleBounds.HasValue && VisibleBounds.Value.Contains(point));
        }
        #endregion


        public Cursor DefaultCursor { get; set; }


        /*   Časomíra:
                        Získat control   Vložit Bounds    Vložit Text   Selection   DrawToBitmap   PaintImage      Čas mikrosekund
        0. Nic:                                                                                                   :        6  (režie smyčky)
        1. PaintImage                                                                                  ANO        :       15
        2. Bez Bounds        ANO                                                                                  :       25
        3. Bez Text          ANO             ANO                                                                  :      320
        4. Bez Selection     ANO             ANO             ANO                                                  :      470
        5. Bez obrázku       ANO             ANO             ANO           ANO                                    :      470
        6. Bez kreslení      ANO             ANO             ANO           ANO                         ANO        :      500
        7. Komplet           ANO             ANO             ANO           ANO         ANO             ANO        :      990

        */
            

      
        private Bitmap CreateImageNative(Rectangle bounds, bool storeValues)
        {
            int w = bounds.Width;
            int h = bounds.Height;
            Bitmap image = new Bitmap(w, h);

            //DevExpress.Utils.Drawing.BorderPainter.DrawTextOnGlass
            //    DevExpress.Utils.Drawing.SimpleBorderPainter.
            //DevExpress.XtraEditors.RangeControlBorderPainter.


            int r = w - 1;
            int b = h - 1;
            image.SetPixel(0, 0, Color.DarkBlue);
            image.SetPixel(r, 0, Color.DarkViolet);
            image.SetPixel(0, b, Color.DarkGreen);
            image.SetPixel(r, b, Color.DarkMagenta);

            return image;
        }


    }
}
