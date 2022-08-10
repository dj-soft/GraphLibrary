using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DjSoft.SchedulerMap.Analyser
{
    /// <summary>
    /// Vizualizer mapy
    /// </summary>
    public class MapVisualiserControl : VirtualControl
    {
        #region Konstruktor a Dispose
        public MapVisualiserControl()
        {
            ControlInit();
            DataInit();
            CoordInit();
            _MouseInit();
            SelectionInit();
        }
        protected void ControlInit()
        {
            this.SetStyle(ControlStyles.Selectable, true);
        }
        protected override void Dispose(bool disposing)
        {
            this.DisposePainters();
            base.Dispose(disposing);
        }
        #endregion
        #region Data
        protected void DataInit()
        {
            _VisualItems = new Dictionary<int, MapVisualItem>();
        }
        public MapSegment MapSegment
        {
            get { return _MapSegment; }
            set { _MapSegment = value; InvalidateData(); }
        }
        private MapSegment _MapSegment;
        protected void InvalidateData()
        {
            _VisualItems.Clear();
            RefreshContent();
        }
        protected void CheckData()
        { }

        private Dictionary<int, MapVisualItem> _VisualItems;
        #endregion
        #region Myš
        private void _MouseInit()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseWheel += _MouseWheel;
            this.MouseLeave += _MouseLeave;


        }
        private void _MouseEnter(object sender, EventArgs e)
        {
            ActivateItemForPoint(this.PointToClient(Control.MousePosition), MouseButtons.None);
        }
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            MouseButtons? button = (Control.ModifierKeys != Keys.Alt ? MouseButtons.None : (MouseButtons?)null);
            if (e.Button == MouseButtons.None)
                ActivateItemForPoint(e.Location, button);
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {   // Stisk myši akceptuji jen když k tomu není stisknutá klávesa
            _MouseDownKeys = Control.ModifierKeys;
            if (_MouseDownKeys == Keys.None || _MouseDownKeys == Keys.Control)
                ActivateItemForPoint(e.Location, e.Button);
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (!VirtualSpace.IsMouseDrag && (_MouseDownKeys == Keys.None || _MouseDownKeys == Keys.Control))
                SelectItem(CurrentItemMouseDown, _MouseDownKeys);
            ActivateItemForPoint(e.Location, MouseButtons.None);
            _MouseDownKeys = Keys.None;
        }
        private void _MouseLeave(object sender, EventArgs e)
        {
            this.SetCurrentItemAtMouse(null, null);
        }
        private void _MouseWheel(object sender, MouseEventArgs e)
        {
        }
        private Keys _MouseDownKeys;


        /// <summary>
        /// Najde a aktivuje prvek pod myší
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="button"></param>
        private void ActivateItemForPoint(Point currentPoint, MouseButtons? button)
        {
            var activeItems = _SearchForItemsOnPoint(currentPoint);
            var activeItem = SearchForActiveItem(activeItems, CurrentItemMouseOn);
            SetCurrentItemAtMouse(activeItem, button);
        }
        /// <summary>
        /// Najdi jeden vybraný aktivní prvek , s příchylností k dosavadnímu <paramref name="oldActiveItem"/>
        /// </summary>
        /// <param name="activeItems"></param>
        /// <param name="oldActiveItem"></param>
        /// <returns></returns>
        private IVisualItem SearchForActiveItem(List<IVisualItem> activeItems, IVisualItem oldActiveItem)
        {
            int count = activeItems.Count;
            if (count == 0) return null;                   // Nic není aktivní
            if (count == 1) return activeItems[0];         // Jen jeden prvek je aktivní
            if (oldActiveItem != null && activeItems.Any(i => Object.ReferenceEquals(i, oldActiveItem)))     // Pokud máme nějaký dosavadní aktivní prvek, a ten je i v nově aktivních prvcích:
                return oldActiveItem;                                                                        //  pak má přednost = prvek zůstane aktivní, dokud na něm bude myš.

            return activeItems[count - 1];                 // Vezmeme poslední aktivní prvek = na nejvyšší Z pozici
        }
        /// <summary>
        /// Aktuálně aktivní prvek s myší, proměnná
        /// </summary>
        public IVisualItem CurrentItemMouseOn { get { return _CurrentItemMouseOn; } }
        protected void SetCurrentItemAtMouse(IVisualItem newItem, MouseButtons? button)
        {
            SetCurrentItemAtMouse(newItem, GetItemMouseState(button));
        }
        /// <summary>
        /// Aktivuje daný prvek i jeho stav jako prvek <see cref="CurrentItemMouseOn"/> a případně <see cref="CurrentItemMouseDown"/>.
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="newState"></param>
        protected void SetCurrentItemAtMouse(IVisualItem newItem, ItemMouseState newState)
        {
            var oldItem = _CurrentItemMouseOn;
            if (Object.ReferenceEquals(oldItem, newItem) && (newItem is null || newItem.MouseState == newState)) return;   // Není změna prvku ani změna jeho stavu

            if (oldItem != null) oldItem.MouseState = ItemMouseState.None;
            _CurrentItemMouseOn = newItem;
            _CurrentItemMouseDown = ((newState == ItemMouseState.LeftDown || newState == ItemMouseState.RightDown) ? newItem : null);
            if (newItem != null) newItem.MouseState = newState;
        }
        /// <summary>
        /// Vrátí stav prvku <see cref="ItemMouseState"/> pro daný button myši <see cref="MouseButtons"/>. 
        /// Pokud <paramref name="button"/> je null, pak vrací <see cref="ItemMouseState.None"/>
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        protected static ItemMouseState GetItemMouseState(MouseButtons? button)
        {
            if (!button.HasValue) return ItemMouseState.None;
            if (button.Value == MouseButtons.Left) return ItemMouseState.LeftDown;
            if (button.Value == MouseButtons.Right) return ItemMouseState.RightDown;
            return ItemMouseState.OnMouse;
        }
        /// <summary>
        /// Aktuálně aktivní prvek s myší, proměnná
        /// </summary>
        private IVisualItem _CurrentItemMouseOn;
        #endregion
        #region Selectované prvky
        protected void SelectionInit()
        {
            this._CurrentSelectedItems = new List<IVisualItem>();
        }
        /// <summary>
        /// Zajistí označení daného prvku. Může to být i null = pak jde o odselectování všech.
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <param name="mouseDownKeys"></param>
        protected void SelectItem(IVisualItem selectedItem, Keys mouseDownKeys)
        {
            bool extendedSelect = (mouseDownKeys == Keys.Control);
            bool runRefresh = false;
            if (!extendedSelect)
            {   // Basic režim bez klávesy Ctrl
                if (_CurrentSelectedItems.Count > 0)
                {
                    this.UnSelectAll();
                    runRefresh = true;
                }
                if (selectedItem != null)
                {
                    _CurrentSelectedItems.Add(selectedItem);
                    selectedItem.Selected = true;
                    runRefresh = true;
                }
            }
            else
            {   // Extended režim s klávesou Ctrl
                if (selectedItem != null)
                {
                    if (selectedItem.Selected)
                    {
                        _CurrentSelectedItems.RemoveAll(i => Object.ReferenceEquals(i, selectedItem));
                        selectedItem.Selected = false;
                    }
                    else
                    {
                        _CurrentSelectedItems.Add(selectedItem);
                        selectedItem.Selected = true;
                    }
                    runRefresh = true;
                }
            }
            if (runRefresh)
                this.Invalidate();
        }
        /// <summary>
        /// Odselectuje všechny dosavadní selectované prvky
        /// </summary>
        protected void UnSelectAll()
        {
            foreach (var item in _CurrentSelectedItems)
                item.Selected = false;
            _CurrentSelectedItems.Clear();
        }
        /// <summary>
        /// Aktuální prvek, na kterém se stiskla myš
        /// </summary>
        public IVisualItem CurrentItemMouseDown { get { return _CurrentItemMouseDown; } }
        private IVisualItem _CurrentItemMouseDown;
        /// <summary>
        /// Prvky, které jsou označeny klikáním myši
        /// </summary>
        private List<IVisualItem> _CurrentSelectedItems;
        #endregion
        #region Viditelné prvky, vyhledání prvku
        /// <summary>
        /// Najdi viditelné prvky, které jsou aktivní na daném fyzickém bodě (=bod myši)
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        private List<IVisualItem> _SearchForItemsOnPoint(Point currentPoint)
        {
            List<IVisualItem> items = new List<IVisualItem>();
            items.AddRange(VisibleItems.Where(i => i.IsActiveOnCurrentPoint(currentPoint)));
            return items;
        }
        /// <summary>
        /// Aktuálně viditelné prvky v this controlu, OnDemand load.
        /// Pořadí = od nejnižších po nejvyšší vrstvy (<see cref="IVisualItem.Layer"/> ASC) = vhodné pro kreslení.
        /// </summary>
        public IVisualItem[] VisibleItems
        {
            get
            {
                if (_VisibleItems is null)
                    _VisibleItems = _GetVisibleItems();
                return _VisibleItems;
            }
        }
        /// <summary>
        /// Zajistí nové vykreslení obsahu
        /// </summary>
        public override void RefreshContent()
        {
            _VisibleItems = null;
            base.RefreshContent();
        }
        /// <summary>
        /// Aktuálně viditelné prvky v this controlu, proměnná
        /// </summary>
        private IVisualItem[] _VisibleItems;
        /// <summary>
        /// Najdi a vrať aktuálně viditelné prvky v this controlu, setřídit je podle <see cref="IVisualItem.Layer"/> ASC
        /// </summary>
        /// <returns></returns>
        private IVisualItem[] _GetVisibleItems()
        {
            List<IVisualItem> items = new List<IVisualItem>();
            foreach (var item in _VisualItems.Values)
            {
                item.CurrentlyIsVisible = item.IsVisibleInOwner;
                if (item.CurrentlyIsVisible)
                    items.Add(item);
            }
            items.Sort(VisualItemLayerComparer);
            return items.ToArray();
        }
        /// <summary>
        /// Komparátor podle <see cref="IVisualItem.Layer"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int VisualItemLayerComparer(IVisualItem a, IVisualItem b)
        {
            int al = a?.Layer ?? 0;
            int bl = b?.Layer ?? 0;
            return al.CompareTo(bl);
        }
        #endregion
        #region Koordinátor obsahu buněk
        /// <summary>
        /// Inicializace koordinátů
        /// </summary>
        protected void CoordInit()
        {
            _CellBounds = new RectangleF(5f, 5f, 120f, 60f);
            _Cells = new Cells();
        }
        /// <summary>
        /// Vrátí souřadnice virtuálního středu dané buňky
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public PointF GetVirtualCenter(Point cell)
        {
            var cellBounds = CellBounds;
            return new PointF((float)cell.X * cellBounds.Width, (float)cell.Y * cellBounds.Height);
        }
        /// <summary>
        /// Definice velikosti a rozestupů logických buněk, ve virtuálních hodnotách.
        /// Hodnota X a Y určuje rozestupy mezi buňkami, hodnota Width a Height určuje velikost buňky.
        /// </summary>
        public RectangleF CellBounds 
        {
            get { return _CellBounds; }
            set { _SetCellBounds(value); }
        }
        private void _SetCellBounds(RectangleF cellBounds)
        {
            float x = VirtualSpace.Align(cellBounds.X, 2f, 50f);
            float y = VirtualSpace.Align(cellBounds.Y, 2f, 50f);
            float w = VirtualSpace.Align(cellBounds.Width, 40f, 600f);
            float h = VirtualSpace.Align(cellBounds.Height, 40f, 600f);

            var oldBounds = _CellBounds;
            if (x == oldBounds.X && y == oldBounds.Y && w == oldBounds.Width && h == oldBounds.Height) return;

            _CellBounds = cellBounds;
            RecalculateVirtualBounds();
        }
        private RectangleF _CellBounds;
        /// <summary>
        /// Do všech existujících viditelných prvků znovu vepíše jejich VirtualBounds po změně souřadnic buňky <see cref="CellBounds"/>
        /// </summary>
        private void RecalculateVirtualBounds()
        { }
        private Cells _Cells;
        #endregion
        #region Načítání dat a úvodní rozsvícení prvků
        /// <summary>
        /// Metoda aktivuje Vizualizer, 
        /// a jakmile bude mít k dispozici načtená data <see cref="MapSegment"/>, pak do vizualizeru aktivuje požadované prvky, 
        /// které dodá selector <paramref name="initialItemsSelector"/>. Pokud není dodán Selector pak aktivuje prvních 100 z FirstItems.
        /// </summary>
        /// <param name="initialItemsSelector">Funkce, je volaná po načtení dat do segmentu</param>
        public void ActivateMapItem(Func<MapSegment, IEnumerable<MapItem>> initialItemsSelector = null)
        {
            if (_MapSegment is null) return;
            _VisualItemsClear();

            _InitialItemsSelector = initialItemsSelector;
            if (!(_MapSegment.IsLoaded || _MapSegment.IsLoading))
            {
                Task.Factory.StartNew(_LoadMapSegment);
            }
            else
            {
                _ActivateMapItem();
            }
        }
        /// <summary>
        /// Selector, který má najít první prvky k zobrazení.
        /// </summary>
        private Func<MapSegment, IEnumerable<MapItem>> _InitialItemsSelector;
        /// <summary>
        /// Vyprázdní obsah <see cref="_VisualItems"/>, včetně Dispose prvků
        /// </summary>
        private void _VisualItemsClear()
        {
            foreach (var i in _VisualItems.Values) i.Dispose();
            _VisualItems.Clear();
        }
        private void _LoadMapSegment()
        {   // Běží v pracovním threadu na pozadí!
            _MapSegment.LoadData();
            _ActivateMapItem();
        }
        private void _ActivateMapItem()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(_ActivateMapItemGui));
            else
                _ActivateMapItemGui();
        }
        private void _ActivateMapItemGui()
        {
            MapItem[] visualItems = _GetInitialItems();
            if (visualItems is null || visualItems.Length == 0) return;

            var raster = _CellBounds;

            float x = raster.Right;
            float y = raster.Bottom;
            foreach (var mapItem in visualItems)
            {
                if (_VisualItems.ContainsKey(mapItem.ItemId)) continue;
                var itemSize = raster.Size;
                var visualItem = new MapVisualItem(this, mapItem, new PointF(x, y), itemSize);
                _VisualItems.Add(mapItem.ItemId, visualItem);
                y += raster.Bottom;
                if (y > 1100f)
                {
                    x += raster.Right;
                    y = raster.Bottom;
                }
                if (_VisualItems.Count >= 5000) break;
            }
            RefreshContent();
        }
        /// <summary>
        /// Metoda získá seznam prvků, které mají být zobrazeny jako výchozí.
        /// Pro jejich získání použije funkci uloženou v <see cref="_InitialItemsSelector"/>, defaultně vezme FirstItems, nanejvýše 100 prvků.
        /// </summary>
        /// <returns></returns>
        private MapItem[] _GetInitialItems()
        {
            var mapSegment = this._MapSegment;
            if (mapSegment is null || mapSegment.ItemsCount == 0) return null;

            var initialItemsSelector = _InitialItemsSelector;
            MapItem[] visualItems = null;
            if (initialItemsSelector != null)
            {
                var selectedItems = initialItemsSelector(mapSegment);
                if (selectedItems != null)
                    visualItems = selectedItems.ToArray();
            }
            if (visualItems is null || visualItems.Length == 0)
            {
                visualItems = mapSegment.FirstMainItems;
                if (visualItems.Length < 250) visualItems = mapSegment.FirstItems;
                if (visualItems.Length < 250) visualItems = mapSegment.Items;
                if (visualItems.Length > 750) visualItems = visualItems.Take(750).ToArray();
            }
            _InitialItemsSelector = null;
            return visualItems;
        }
        #endregion
        #region Kreslení a podpora pro něj
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Kreslení vztahů těch prvků, které jsou vidět:
            Dictionary<long, MapLink> paintedLinks = new Dictionary<long, MapLink>();
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var item in VisibleItems)
                item.OnPaintLinks(e, paintedLinks);

            // Kreslení těch prvků, které jsou vidět:
            foreach (var item in VisibleItems)
                item.OnPaintItem(e);

            // Kreslení overlaye:

        }
        /// <summary>
        /// Podejte mi obyčejný štětec dané barvy!
        /// Štětec nezahazujme, bude se hodit někomu dalšímu.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public Brush GetStandardBrush(Color color)
        {
            var brush = StandardBrush;
            brush.Color = color;
            return brush;
        }
        /// <summary>
        /// Podejte mi řádně namočené, připravené a kvalitně píšící pero dané barvy!!!
        /// Pero nezahazujme, bude se hodit někomu dalšímu.
        /// </summary>
        /// <param name="color">Barva inkoustu</param>
        /// <param name="width">Šířka pera</param>
        /// <param name="dashStyle">Tečkování čáry</param>
        /// <param name="startCap">Styl začátku</param>
        /// <param name="endCap">Styl konce</param>
        /// <returns></returns>
        public Pen GetPen(Color color, float? width = null, DashStyle? dashStyle = null, LineCap? startCap = null, LineCap? endCap = null)
        {
            var pen = StandardPen;
            pen.Color = color;
            pen.Width = (width ?? 1f);
            pen.DashStyle = (dashStyle ?? DashStyle.Solid);
            pen.StartCap = startCap ?? LineCap.Round;
            pen.EndCap = endCap ?? LineCap.Round;
            return pen;
        }
        /// <summary>
        /// Podejte mi font dané kvality.
        /// Font nezahazujme, bude se hodit někomu dalšímu.
        /// </summary>
        /// <param name="fontSizeEm"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public Font GetFont(float fontSizeEm, FontStyle fontStyle)
        {
            var fontDict = this.StardardFontDict;
            string key = GetFontKey(fontSizeEm, fontStyle);
            if (!fontDict.TryGetValue(key, out var font))
            {
                font = new Font(SystemFonts.DefaultFont.FontFamily, fontSizeEm, fontStyle);
                fontDict.Add(key, font);
            }
            return font;
        }
        /// <summary>
        /// Vrátí klíč pro daný font
        /// </summary>
        /// <param name="fontSizeEm"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private string GetFontKey(float fontSizeEm, FontStyle fontStyle)
        {
            string key = fontSizeEm.ToString("##0.0") + ":" +
                (fontStyle.HasFlag(FontStyle.Bold) ? "B" : "-") +
                (fontStyle.HasFlag(FontStyle.Italic) ? "I" : "-") +
                (fontStyle.HasFlag(FontStyle.Underline) ? "I" : "-") +
                (fontStyle.HasFlag(FontStyle.Strikeout) ? "S" : "-");
            return key;
        }
        /// <summary>
        /// Obyčejný štětec s barvou pro výplně
        /// </summary>
        protected SolidBrush StandardBrush
        {
            get
            {
                if (_StandardBrush == null) _StandardBrush = new SolidBrush(Color.White);
                return _StandardBrush;
            }
        }
        private SolidBrush _StandardBrush;
        /// <summary>
        /// Obyčejné pero s násadkou pro kreslení čar
        /// </summary>
        protected Pen StandardPen
        {
            get
            {
                if (_StandardPen == null) _StandardPen = new Pen(Color.Black);
                return _StandardPen;
            }
        }
        private Pen _StandardPen;
        /// <summary>
        /// Dictionary obsahující písma
        /// </summary>
        protected Dictionary<string, Font> StardardFontDict
        {
            get
            {
                if (_StardardFontDict is null) _StardardFontDict = new Dictionary<string, Font>();
                return _StardardFontDict;
            }
        }
        private Dictionary<string, Font> _StardardFontDict;
        /// <summary>
        /// DIsposuje grafické prvky
        /// </summary>
        protected void DisposePainters()
        {
            if (_StandardBrush != null)
            {
                _StandardBrush.Dispose();
                _StandardBrush = null;
            }
            if (_StandardPen != null)
            {
                _StandardPen.Dispose();
                _StandardPen = null;
            }
            if (_StardardFontDict != null)
            {
                foreach (var font in _StardardFontDict.Values)
                    font.Dispose();
                _StardardFontDict.Clear();
                _StardardFontDict = null;
            }
        }
        #endregion
        #region Získání textu vhodného pro zobrazení v prvku
        /// <summary>
        /// Vrať vhodný text do prvku
        /// </summary>
        /// <param name="visualItem"></param>
        /// <returns></returns>
        internal string GetVisualObjectText(MapVisualItem visualItem)
        {
            switch (visualItem.ItemType)
            {
                case MapItemType.IncrementByRealSupplierOrder:
                case MapItemType.IncrementByPlanSupplierOrder: return GetTextAfter(visualItem.MapItem.Description1, "Unit=", ": ");
                case MapItemType.IncrementByProposalReceipt: 
                case MapItemType.DecrementByProposalRequisition: 
                case MapItemType.IncrementByPlanStockTransfer: 
                case MapItemType.DecrementByRealComponent: 
                case MapItemType.DecrementByPlanComponent: 
                case MapItemType.IncrementByRealByProductSuitable: 
                case MapItemType.IncrementByPlanByProductSuitable: 
                case MapItemType.IncrementByRealByProductDissonant: 
                case MapItemType.IncrementByPlanByProductDissonant: return GetTextAfter(visualItem.MapItem.Description1, "Unit=", ": ");
                case MapItemType.OperationPlan: 
                case MapItemType.OperationReal: return GetTextAfter(visualItem.MapItem.Description1, "Operation", "=");    // Operation=50: DC 3
                case MapItemType.IncrementByRealProductOrder: 
                case MapItemType.IncrementByPlanProductOrder: return GetTextAfter(visualItem.MapItem.Description1, "Unit=", ": ");
                case MapItemType.DecrementByRealEnquiry: 
                case MapItemType.DecrementByPlanEnquiry: return GetTextAfter(visualItem.MapItem.Description1, "Unit=", ": ");
            }
            return null;
        }
        /// <summary>
        /// Vrať text, který se nachází za delimiterem <paramref name="delimiter"/>, pokud vstupní text <paramref name="text"/> začíná daným začátkem <paramref name="startWith"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startWith"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        private static string GetTextAfter(string text, string startWith, string delimiter)
        {
            if (String.IsNullOrEmpty(text) || !text.StartsWith(startWith)) return null;
            int delimiterIndex = text.IndexOf(delimiter, startWith.Length);
            if (delimiterIndex < 0) return null;
            return text.Substring(delimiterIndex + delimiter.Length);
        }
        #region Vizuální tvary a barvy pro jednotlivé typy prvků, jejich cache, jejich tvorba
        /// <summary>
        /// Metoda vrátí definici tvaru pro prvek daného typu.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        internal VisualShape GetVisualObjectShape(MapItemType visualType)
        {
            VisualShape visualShape = null;
            if (_ShapeDict is null) _ShapeDict = new Dictionary<MapItemType, VisualShape>();
            if (!_ShapeDict.TryGetValue(visualType, out visualShape))
            {
                visualShape = CreateVisualObjectShape(visualType);
                _ShapeDict.Add(visualType, visualShape);
            }
            return visualShape;
        }
        /// <summary>
        /// Vytvoří new instanci vizuálního tvaru pro daný typ prvku <paramref name="visualType"/> a vrátí tento <see cref="VisualShape"/>.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        private VisualShape CreateVisualObjectShape(MapItemType visualType)
        {
            VisualShape visualShape = new VisualShape();

            // 1. Tvary:
            switch (visualType)
            {
                case MapItemType.IncrementByRealSupplierOrder: 
                case MapItemType.IncrementByPlanSupplierOrder: 
                case MapItemType.IncrementByProposalReceipt:
                    visualShape.Points = CreatePoints(5, 5, 5, 94, 183, 94, 194, 83, 194, 16, 183, 5);
                    visualShape.TextBounds = new RectangleF(20f, 16f, 160f, 68f);
                    break;
                case MapItemType.DecrementByProposalRequisition:
                case MapItemType.IncrementByPlanStockTransfer:
                case MapItemType.DecrementByRealComponent: 
                case MapItemType.DecrementByPlanComponent:
                case MapItemType.IncrementByRealByProductSuitable: 
                case MapItemType.IncrementByPlanByProductSuitable:
                case MapItemType.IncrementByRealByProductDissonant: 
                case MapItemType.IncrementByPlanByProductDissonant:
                    visualShape.Points = CreatePoints(16, 5, 5, 16, 5, 83, 16, 94, 183, 94, 194, 83, 194, 16, 183, 5);
                    visualShape.TextBounds = new RectangleF(20f, 16f, 160f, 68f);
                    break;
                case MapItemType.OperationPlan: 
                case MapItemType.OperationReal:
                    visualShape.Points = CreatePoints(5, 5, 5, 72, 27, 94, 194, 94, 194, 27, 172, 5);
                    visualShape.TextBounds = new RectangleF(27f, 16f, 146f, 68f);
                    break;
                case MapItemType.IncrementByRealProductOrder: 
                case MapItemType.IncrementByPlanProductOrder:
                    visualShape.Points = CreatePoints(5, 5, 5, 94, 194, 94, 194, 5);
                    visualShape.TextBounds = new RectangleF(20f, 16f, 160f, 68f);
                    break;
                case MapItemType.DecrementByRealEnquiry: 
                case MapItemType.DecrementByPlanEnquiry:
                    visualShape.Points = CreatePoints(16, 5, 5, 16, 5, 83, 16, 94, 194, 94, 194, 5);
                    visualShape.TextBounds = new RectangleF(20f, 16f, 160f, 68f);
                    break;
                default:
                    visualShape.Points = CreatePoints(5, 5, 5, 94, 194, 94, 194, 5);
                    visualShape.TextBounds = new RectangleF(20f, 16f, 160f, 68f);
                    break;
            }

            // 2. Barvy:
            switch (visualType)
            {
                case MapItemType.IncrementByRealSupplierOrder:
                    visualShape.BackColor = Color.FromArgb(255, 94, 255, 94);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextStyle = FontStyle.Bold;
                    break;
                case MapItemType.IncrementByPlanSupplierOrder:
                    visualShape.BackColor = Color.FromArgb(255, 193, 255, 193);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.IncrementByProposalReceipt:
                    visualShape.BackColor = Color.FromArgb(255, 191, 255, 223);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.DecrementByProposalRequisition:
                    visualShape.BackColor = Color.FromArgb(255, 255, 255, 211);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.IncrementByPlanStockTransfer:
                    visualShape.BackColor = Color.FromArgb(255, 224, 255, 193);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.DecrementByRealComponent:
                    visualShape.BackColor = Color.FromArgb(255, 214, 94, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    break;
                case MapItemType.DecrementByPlanComponent:
                    visualShape.BackColor = Color.FromArgb(255, 232, 163, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.IncrementByRealByProductSuitable:
                    visualShape.BackColor = Color.FromArgb(255, 255, 137, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextStyle = FontStyle.Italic;
                    break;
                case MapItemType.IncrementByPlanByProductSuitable:
                    visualShape.BackColor = Color.FromArgb(255, 240, 196, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextStyle = FontStyle.Italic;
                    break;
                case MapItemType.IncrementByRealByProductDissonant:
                    visualShape.BackColor = Color.FromArgb(255, 204, 153, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextStyle = FontStyle.Italic;
                    break;
                case MapItemType.IncrementByPlanByProductDissonant:
                    visualShape.BackColor = Color.FromArgb(255, 255, 196, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextStyle = FontStyle.Italic;
                    break;
                case MapItemType.OperationReal:
                    visualShape.BackColor = Color.FromArgb(255, 168, 168, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextStyle = FontStyle.Bold;
                    break;
                case MapItemType.OperationPlan:
                    visualShape.BackColor = Color.FromArgb(255, 119, 119, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.IncrementByRealProductOrder:
                    visualShape.BackColor = Color.FromArgb(255, 68, 255, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextStyle = FontStyle.Bold;
                    break;
                case MapItemType.IncrementByPlanProductOrder:
                    visualShape.BackColor = Color.FromArgb(255, 158, 255, 255);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
                case MapItemType.DecrementByRealEnquiry:
                    visualShape.BackColor = Color.FromArgb(255, 255, 81, 81);
                    visualShape.BorderColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextColor = Color.FromArgb(255, 0, 0, 0);
                    visualShape.TextStyle = FontStyle.Bold;
                    break;
                case MapItemType.DecrementByPlanEnquiry:
                    visualShape.BackColor = Color.FromArgb(255, 255, 173, 173);
                    visualShape.BorderColor = Color.FromArgb(255, 64, 96, 96);
                    visualShape.TextColor = Color.FromArgb(255, 64, 96, 96);
                    break;
            }
            return visualShape;
        }
        /// <summary>
        /// Ze zadaných souřadnic (v pořadí X0,Y0, X1,Y1, X2,Y2, ... Xn,Yn) vytvoří pole bodů a to vrátí.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static PointF[] CreatePoints(params int[] values)
        {
            List<PointF> points = new List<PointF>();
            int count = values.Length;
            for (int i = 0; i < count; i += 2)
                points.Add(new PointF(values[i], values[i + 1]));
            return points.ToArray();
        }
        /// <summary>
        /// Úložiště vizuálních tvarů
        /// </summary>
        private Dictionary<MapItemType, VisualShape> _ShapeDict;
        #endregion

        #endregion
    }
    #region class VisualShape : Obálka definující vizuální tvar
    /// <summary>
    /// Obálka definující vizuální tvar
    /// </summary>
    internal class VisualShape
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public VisualShape()
        {
            BackColor = Color.FromArgb(216, 216, 216);
            BorderColor = Color.FromArgb(80, 80, 80);
            TextColor = Color.FromArgb(32, 32, 32);
            TextStyle = FontStyle.Regular;
        }
        /// <summary>
        /// Souřadnice bodů okraje obrazce.
        /// <para/>
        /// Vyhrazená souřadnice pro obrazec je { 0f, 0f, 200f, 100f }. V tomto prostoru mají být definovány oblasti obrazce.
        /// Následně bude obrazec transformován do cílového souřadného systému a vykreslen.
        /// Uvnitř těchto okrajů se má vyskytovat prostor textu <see cref="TextBounds"/>.
        /// <para/>
        /// Souřadnice mají uvažovat s tím, že okraje prostoru budou vykreslovány linkou určité tloušťky a budou tedy přesahovat i ven a dovnitř od této souřadnice, 
        /// proto by tyto souřadnice měly ponechávat odstup 5 jednotek od okraje prostoru obrazce, tedy měly by se pohybovat v rozmezí { 5f, 5f, 190f, 90f }.
        /// </summary>
        public PointF[] Points { get; set; }
        /// <summary>
        /// Souřadnice vyhrazená pro text, uvnitř obrazce.
        /// <para/>
        /// Vyhrazená souřadnice pro obrazec je { 0f, 0f, 200f, 100f }. Prostor textu by měl být o 5 jednotek menší než vnitřní souřadnice okraje, kvůli tloušťce orámování.
        /// </summary>
        public RectangleF TextBounds { get; set; }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get; set; }
        /// <summary>
        /// Barva rámečku
        /// </summary>
        public Color BorderColor { get; set; }
        /// <summary>
        /// Barva písma
        /// </summary>
        public Color TextColor { get; set; }
        /// <summary>
        /// Styl písma
        /// </summary>
        public FontStyle TextStyle { get; set; }

        /// <summary>
        /// Vygeneruje fyzickou grafickou komponentu pro svůj tvar, převedenou do daného cílového prostoru.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="margin"></param>
        /// <param name="border"></param>
        /// <returns></returns>
        public GraphicsPath CreateBaseGraphicsPath(RectangleF bounds)
        {
            if (bounds.Width < 12f || bounds.Height < 12f) return null;

            int count = this.Points?.Length ?? 0;
            if (count == 0) return null;

            bool useFloat = (bounds.Width < 60f);

            List<PointF> points = new List<PointF>();
            PointF? first = null;
            for (int i = 0; i < count; i++)
            {
                PointF point = GetCurrentPoint(this.Points[i], bounds, useFloat);
                points.Add(point);
                if (!first.HasValue) first = point;
            }
            if (first.HasValue) points.Add(first.Value);

            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(points.ToArray());
            path.CloseFigure();

            return path;
        }
        /// <summary>
        /// Vrátí souřadnici textu <see cref="TextBounds"/> převedenou do daného cílového prostoru.
        /// </summary>
        /// <param name="targetBounds"></param>
        /// <returns></returns>
        public RectangleF GetCurrentTextBounds(RectangleF targetBounds)
        {
            return GetCurrentBounds(this.TextBounds, targetBounds);
        }

        private RectangleF GetCurrentBounds(RectangleF designBounds, RectangleF targetBounds, bool useFloat = false)
        {
            float zoomX = targetBounds.Width / 200f;
            float zoomY = targetBounds.Height / 100f;
            float x = targetBounds.X + zoomX * designBounds.X;
            float y = targetBounds.Y + zoomY * designBounds.Y;
            float w = zoomX * designBounds.Width;
            float h = zoomY * designBounds.Height;
            if (!useFloat)
            {
                float r = x + w;
                float b = y + h;
                x = (float)Math.Round(x, 0);
                y = (float)Math.Round(y, 0);
                w = (float)Math.Round(r, 0) - x;
                h = (float)Math.Round(b, 0) - y;
            }
            return new RectangleF(x, y, w, h);
        }
        private PointF GetCurrentPoint(PointF designPoint, RectangleF targetBounds, bool useFloat = false)
        {
            float zoomX = targetBounds.Width / 200f;
            float zoomY = targetBounds.Height / 100f;
            float x = targetBounds.X + zoomX * designPoint.X;
            float y = targetBounds.Y + zoomY * designPoint.Y;
            if (!useFloat)
            {
                x = (float)Math.Round(x, 0);
                y = (float)Math.Round(y, 0);
            }
            return new PointF(x, y);
        }
    }
    #endregion
    #region class VisualItem : Prvek mapy umístěný ve virtuálním prostoru
    /// <summary>
    /// Prvek mapy umístěný ve virtuálním prostoru
    /// </summary>
    internal class MapVisualItem : VirtualItemBase, IVisualItem
    {
        public MapVisualItem(MapVisualiserControl visualiser, MapItem mapItem, PointF center, SizeF size)
            : base(visualiser)
        {
            Visualiser = visualiser;
            MapItem = mapItem;
            MapItem.VisualItem = this;
            VirtualBounds.SetCenterSize(center, size);
            Text = visualiser.GetVisualObjectText(this);
        }
        public override void Dispose()
        {
            base.Dispose();
            Visualiser = null;
            MapItem.VisualItem = null;
            MapItem = null;
        }
        protected MapVisualiserControl Visualiser { get; private set; }
        /// <summary>
        /// Zobrazovaný prvek
        /// </summary>
        public MapItem MapItem { get; private set; }
        /// <summary>
        /// Typ prvku <see cref="Type"/> ondemand převedený na enum
        /// </summary>
        internal MapItemType ItemType { get { return this.MapItem.ItemType; } }
        /// <summary>
        /// Prev vztahy.
        /// Ve vztahu je jako <see cref="MapLink.NextItem"/> prvek this a ve vztahu <see cref="MapLink.PrevItem"/> je prvek vpravo.
        /// Toto pole může být null, pokud this prvek je první = má <see cref="IsFirst"/> = true.
        /// Pokud není null, pak typicky obsahuje nějaký prvek, tyto prvky vždy mají naplněné obě instance (<see cref="MapLink.PrevItem"/> i <see cref="MapLink.NextItem"/>).
        /// </summary>
        public MapLink[] PrevLinks { get { return this.MapItem.PrevLinks; } }
        /// <summary>
        /// Next vztahy.
        /// Ve vztahu je jako <see cref="MapLink.PrevItem"/> prvek this a ve vztahu <see cref="MapLink.NextItem"/> je prvek vpravo.
        /// Toto pole může být null, pokud this prvek je poslední = má <see cref="IsLast"/> = true.
        /// Pokud není null, pak typicky obsahuje nějaký prvek, tyto prvky vždy mají naplněné obě instance (<see cref="MapLink.PrevItem"/> i <see cref="MapLink.NextItem"/>).
        /// </summary>
        public MapLink[] NextLinks { get { return this.MapItem.NextLinks; } }

        /// <summary>
        /// Text zobrazený v prvku
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Definice vizuálního tvaru, je dána typem prvku <see cref="ItemType"/>.
        /// </summary>
        public VisualShape VisualShape { get { return Visualiser.GetVisualObjectShape(this.ItemType); } }
        /// <summary>
        /// Barva pozadí. Vychází z typu prvku. Nikdy není null (při čtení).
        /// Lze setovat explicitní, ale lze setovat i null (pak se vrátí default = podle typu prvku).
        /// </summary>
        public Color? BackColor 
        {
            get { return _BackColor ?? VisualShape.BackColor; }
            set { _BackColor = value; }
        }
        private Color? _BackColor;
        /// <summary>
        /// Barva písma. Vychází z typu prvku. Nikdy není null (při čtení).
        /// Lze setovat explicitní, ale lze setovat i null (pak se vrátí default = podle typu prvku).
        /// </summary>
        public Color? TextColor
        {
            get { return _TextColor ?? VisualShape.TextColor; }
            set { _TextColor = value; }
        }
        private Color? _TextColor;
        /// <summary>
        /// Styl písma. Vychází z typu prvku. Nikdy není null (při čtení).
        /// Lze setovat explicitní, ale lze setovat i null (pak se vrátí default = podle typu prvku).
        /// </summary>
        public FontStyle? TextStyle
        {
            get { return _TextStyle ?? VisualShape.TextStyle; }
            set { _TextStyle = value; }
        }
        private FontStyle? _TextStyle;

        /// <summary>
        /// Adresa buňky ve formě Point (X, Y). 
        /// Adresu setuje koordinátor <see cref="Cells"/>, který udržuje vizuální mapu objektů. Adresu může kdykoliv změnit.
        /// Přepočet do virtuální souřadnice provádí metoda <see cref="MapVisualiserControl.GetVirtualCenter(Point)"/>, kterou toto setování vyvolá. 
        /// Setování dále vyvolá invalidaci controlu pro jeho překreslení.
        /// </summary>
        public Point Coordinate
        {
            get { return _Coordinate; }
            set { _Coordinate = value; Center = Visualiser.GetVirtualCenter(value); this.InvalidateControl(); }
        }
        private Point _Coordinate;
        /// <summary>
        /// Virtuální střed objektu. Lze přemístit jinam.
        /// </summary>
        public PointF Center { get { return VirtualBounds.Center; } set { VirtualBounds.Center = value; } }

        /// <summary>
        /// Vykreslí svoje linky, pokud dosud nejsou uvedeny v <paramref name="paintedLinks"/> - tam se dávají při vykreslení.
        /// Nechceme jeden Link kreslit dvakrát v průběhu jednoho Paint controlu (jednak kvůli času a výkonu, druhak proto že dvojí kreslení s antialiasem dává "zvýrazněný" obraz.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="paintedLinks"></param>
        public override void OnPaintLinks(PaintEventArgs e, Dictionary<long, MapLink> paintedLinks)
        {
            OnPaintLinks(e, this.PrevLinks, paintedLinks);
            OnPaintLinks(e, this.NextLinks, paintedLinks);
        }
        private void OnPaintLinks(PaintEventArgs e, MapLink[] mapLinks, Dictionary<long, MapLink> paintedLinks)
        {
            if (mapLinks != null && mapLinks.Length > 0)
            {
                foreach (var mapLink in mapLinks)
                    OnPaintLink(e, mapLink, paintedLinks);
            }
        }
        private void OnPaintLink(PaintEventArgs e, MapLink mapLink, Dictionary<long, MapLink> paintedLinks)
        {
            if (mapLink is null || paintedLinks.ContainsKey(mapLink.LinkId)) return;
            paintedLinks.Add(mapLink.LinkId, mapLink);

            var prevVisualItem = mapLink.PrevItem.VisualItem;
            var nextVisualItem = mapLink.NextItem.VisualItem;
            if (prevVisualItem is null || nextVisualItem is null) return;

            var prevBounds = prevVisualItem.CurrentBounds;
            var nextBounds = nextVisualItem.CurrentBounds;
            Point prevPoint = new Point(prevBounds.Right, prevBounds.Y + prevBounds.Height / 2);
            Point nextPoint = new Point(nextBounds.Left, nextBounds.Y + nextBounds.Height / 2);
            e.Graphics.DrawLine(Pens.Red, prevPoint, nextPoint);
        }
        public override void OnPaintItem(PaintEventArgs e)
        {
            var currentBounds = this.CurrentBounds;
            e.Graphics.SetClip(currentBounds);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            PaintBasePath(e.Graphics, currentBounds);
            PaintItemText(e.Graphics, currentBounds);
            e.Graphics.ResetClip();
        }
        private void PaintBasePath(Graphics graphics, Rectangle currentBounds)
        {
            using (var graphicsPath = this.VisualShape.CreateBaseGraphicsPath(currentBounds))
            {
                // Podklad v jednoduché barvě:
                var brush1 = GetBackBrush();
                if (brush1 != null)
                    graphics.FillPath(brush1, graphicsPath);
                else
                {   // Anebo podklad s komplexní výplní (typicky LinearGradient):
                    using (var brush2 = CreateBackBrush())
                    {
                        if (brush2 != null)
                            graphics.FillPath(brush2, graphicsPath);
                    }
                }

                // Pero podkladové (může být širší):
                var pen1 = GetPen1();
                if (pen1 != null)
                    graphics.DrawPath(pen1, graphicsPath);

                // Pero vrchní (bude tenčí než pero 1):
                var pen2 = GetPen2();
                if (pen2 != null)
                    graphics.DrawPath(pen2, graphicsPath);
            }
        }
        private void PaintItemText(Graphics graphics, Rectangle currentBounds)
        {
            string text = this.Text;
            if (String.IsNullOrEmpty(text)) return;

            var textEmSize = VirtualControl.GetFontSizeEm(this.ZoomLinear);
            var font = Visualiser.GetFont(textEmSize, this.TextStyle.Value);

            var textColor = this.TextColor.Value;
            var brush = Visualiser.GetStandardBrush(textColor);

            var borderMargin = VirtualControl.GetBorderMargin(this.Zoom);
            var textBounds = this.VisualShape.GetCurrentTextBounds(currentBounds);
            var textSize = graphics.MeasureString(text, font, (int)textBounds.Width);
            var fontBounds = AlignSizeToBounds(textBounds, textSize, ContentAlignment.MiddleLeft, true);

            graphics.DrawString(text, font, brush, fontBounds);
        }
        /// <summary>
        /// Vrátí štětec pro vykreslení výplně pozadí prvku. 
        /// Tento nástroj nesmí volající disposovat, používá se opakovaně.
        /// </summary>
        /// <returns></returns>
        private Brush GetBackBrush()
        {
            return Visualiser.GetStandardBrush(this.BackColor.Value);
        }
        private Brush CreateBackBrush()
        {
            return null;
        }
        /// <summary>
        /// Vrátí pero pro vykreslení okraje prvku, podkladová vrstva. 
        /// Tento nástroj nesmí volající disposovat, používá se opakovaně.
        /// </summary>
        /// <returns></returns>
        private Pen GetPen1()
        {
            var borderColor = this.CurrentOutlineColor;
            if (!borderColor.HasValue) return null;
            var borderMargin = 2f * VirtualControl.GetBorderMargin(this.Zoom);
            return Visualiser.GetPen(borderColor.Value, borderMargin);
        }
        /// <summary>
        /// Vrátí pero pro vykreslení okraje prvku, horní vrstva. 
        /// Tento nástroj nesmí volající disposovat, používá se opakovaně.
        /// </summary>
        /// <returns></returns>
        private Pen GetPen2()
        {
            var borderColor = Color.Black;
            var borderMargin = 1f;
            return Visualiser.GetPen(borderColor, borderMargin);
        }


        ///// <summary>
        ///// Vykreslí danou Path daným štětcem / barvou
        ///// </summary>
        ///// <param name="e"></param>
        ///// <param name="hasData"></param>
        ///// <param name="brush"></param>
        ///// <param name="color"></param>
        ///// <param name="path"></param>
        //private void OnPaintPath(PaintEventArgs e, bool hasData, Brush brush, Color? color, GraphicsPath path)
        //{
        //    if (hasData)
        //    {
        //        if (brush != null)
        //            e.Graphics.FillPath(brush, path);
        //        else if (color.HasValue)
        //            e.Graphics.FillPath(Visualiser.GetStandardBrush(color.Value), path);
        //    }
        //}
        //private void OnPaintText(PaintEventArgs e, float fontEmSize, Color? color, RectangleF textBouds)
        //{
        //    if (!color.HasValue) return;

        //    string text = this.Text;
        //    if (String.IsNullOrEmpty(text)) return;

        //    var font = Visualiser.GetFont(fontEmSize, FontStyle.Regular);
        //    var brush = Visualiser.GetStandardBrush(color.Value);

        //    var textSize = e.Graphics.MeasureString(text, font, (int)textBouds.Width);
        //    var fontBounds = AlignSizeToBounds(textBouds, textSize, ContentAlignment.MiddleLeft, true);
        //    e.Graphics.DrawString(text, font, brush, fontBounds);
        //}

        /// <summary>
        /// Umístí danou cílovou velikost <paramref name="size"/> do daného prostoru <paramref name="bounds"/> tak, aby v něm byla cílová velikost umístěna v daném zarovnání <paramref name="alignment"/>.
        /// Pokud by daný prostor <paramref name="bounds"/> byl menší než potřebný, může / nemusí být cílová velikost <paramref name="size"/> zmenšena tak, aby se do prostoru vešla.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="size"></param>
        /// <param name="alignment"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        private static RectangleF AlignSizeToBounds(RectangleF bounds, SizeF size, ContentAlignment alignment, bool clip = false)
        {
            float w = size.Width;
            if (clip && w > bounds.Width) w = bounds.Width;
            float h = size.Height;
            if (clip && h > bounds.Height) h = bounds.Height;
            float x = bounds.X;
            float y = bounds.Y;
            float dx = bounds.Width - w;
            float dy = bounds.Height - h;

            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += dx / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x += dx;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += dy / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    y += dy / 2f;
                    x += dx / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    y += dy / 2f;
                    x += dx;
                    break;
                case ContentAlignment.BottomLeft:
                    y += dy;
                    break;
                case ContentAlignment.BottomCenter:
                    y += dy;
                    x += dx / 2f;
                    break;
                case ContentAlignment.BottomRight:
                    y += dy;
                    x += dx;
                    break;
            }
            return new RectangleF(x, y, w, h);
        }
        bool IVisualItem.IsActiveOnCurrentPoint(Point currentPoint)
        {
            var currentBounds = this.CurrentVisibleBounds;
            return (currentBounds.HasValue && currentBounds.Value.Contains(currentPoint));
        }
    }
    #endregion
}
