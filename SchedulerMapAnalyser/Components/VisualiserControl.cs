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
    public class VisualiserControl : VirtualControl
    {
        #region Konstruktor a Dispose
        public VisualiserControl()
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
            _VisualItems = new Dictionary<int, VisualItem>();
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

        private Dictionary<int, VisualItem> _VisualItems;
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
                var itemSize = MapItemPainter.GetVirtualSize(mapItem, raster.Size);
                var visualItem = new VisualItem(this, mapItem, new PointF(x, y), itemSize);
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
                visualItems = mapSegment.FirstItems;
                if (visualItems.Length > 100)
                    visualItems = visualItems.Take(100).ToArray();
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

            // Kreslení prvků:
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
            var pen = _StandardPen;
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
        #region Typy objektů, barvy a Tvary
        /// <summary>
        /// Vrátí barvu pozadí pro prvek daného typu.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        internal Color GetVisualObjectBackColor(MapItemType visualType)
        {
            switch (visualType)
            {
                case MapItemType.IncrementByRealSupplierOrder: return Color.FromArgb(255, 94, 255, 94);
                case MapItemType.IncrementByPlanSupplierOrder: return Color.FromArgb(255, 193, 255, 193);
                case MapItemType.IncrementByProposalReceipt: return Color.FromArgb(255, 191, 255, 223);
                case MapItemType.DecrementByProposalRequisition: return Color.FromArgb(255, 255, 255, 211);
                case MapItemType.IncrementByPlanStockTransfer: return Color.FromArgb(255, 224, 255, 193);
                case MapItemType.DecrementByRealComponent: return Color.FromArgb(255, 214, 94, 255);
                case MapItemType.DecrementByPlanComponent: return Color.FromArgb(255, 232, 163, 255);
                case MapItemType.IncrementByRealByProductSuitable: return Color.FromArgb(255, 255, 137, 255);
                case MapItemType.IncrementByPlanByProductSuitable: return Color.FromArgb(255, 240, 196, 255);
                case MapItemType.IncrementByRealByProductDissonant: return Color.FromArgb(255, 204, 153, 255);
                case MapItemType.IncrementByPlanByProductDissonant: return Color.FromArgb(255, 255, 196, 255);
                case MapItemType.OperationPlan: return Color.FromArgb(255, 119, 119, 255);
                case MapItemType.OperationReal: return Color.FromArgb(255, 168, 168, 255);
                case MapItemType.IncrementByRealProductOrder: return Color.FromArgb(255, 68, 255, 255);
                case MapItemType.IncrementByPlanProductOrder: return Color.FromArgb(255, 158, 255, 255);
                case MapItemType.DecrementByRealEnquiry: return Color.FromArgb(255, 255, 81, 81);
                case MapItemType.DecrementByPlanEnquiry: return Color.FromArgb(255, 255, 173, 173);
            }
            return Color.FromArgb(255, 216, 216, 216);
        }
        /// <summary>
        /// Vrátí barvu textu pro prvek daného typu.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        internal Color GetVisualObjectTextColor(MapItemType visualType)
        {
            switch (visualType)
            {
                case MapItemType.IncrementByRealSupplierOrder:
                case MapItemType.DecrementByRealComponent:
                case MapItemType.IncrementByRealByProductSuitable:
                case MapItemType.IncrementByRealByProductDissonant:
                case MapItemType.OperationReal:
                case MapItemType.IncrementByRealProductOrder:
                case MapItemType.DecrementByRealEnquiry: return Color.FromArgb(255, 0, 0, 0);

                case MapItemType.IncrementByProposalReceipt:
                case MapItemType.DecrementByProposalRequisition:
                case MapItemType.IncrementByPlanStockTransfer: return Color.FromArgb(255, 64, 96, 96);

                case MapItemType.IncrementByPlanSupplierOrder:
                case MapItemType.DecrementByPlanComponent:
                case MapItemType.IncrementByPlanByProductSuitable:
                case MapItemType.IncrementByPlanByProductDissonant:
                case MapItemType.OperationPlan:
                case MapItemType.IncrementByPlanProductOrder:
                case MapItemType.DecrementByPlanEnquiry: return Color.FromArgb(255, 64, 64, 64);
            }
            return Color.FromArgb(255, 32, 0, 64);
        }
        /// <summary>
        /// Vrať vhodný text do prvku
        /// </summary>
        /// <param name="visualItem"></param>
        /// <returns></returns>
        internal string GetVisualObjectText(VisualItem visualItem)
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
        /// <summary>
        /// Metoda vrátí definici tvaru pro prvek daného typu.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        internal VisualShape GetVisualObjectShape(MapItemType visualType)
        {
            switch (visualType)
            {
                case MapItemType.IncrementByRealSupplierOrder: 
                case MapItemType.IncrementByPlanSupplierOrder: 
                case MapItemType.IncrementByProposalReceipt: return ShapeRight;
                case MapItemType.DecrementByProposalRequisition: return ShapeDown; 
                case MapItemType.IncrementByPlanStockTransfer: return ShapeUp;
                case MapItemType.DecrementByRealComponent: 
                case MapItemType.DecrementByPlanComponent: return ShapeDownHalf;
                case MapItemType.IncrementByRealByProductSuitable: 
                case MapItemType.IncrementByPlanByProductSuitable: return ShapeUpHalf;
                case MapItemType.IncrementByRealByProductDissonant: 
                case MapItemType.IncrementByPlanByProductDissonant: return ShapeUpHalf;
                case MapItemType.OperationPlan: 
                case MapItemType.OperationReal: return ShapeTask;
                case MapItemType.IncrementByRealProductOrder: 
                case MapItemType.IncrementByPlanProductOrder: return ShapeUpWide;
                case MapItemType.DecrementByRealEnquiry: 
                case MapItemType.DecrementByPlanEnquiry: return ShapeLeft;
            }
            return ShapeBasic;
        }
        private VisualShape ShapeBasic { get { return GetCreate(ref _ShapeBasic, () => new int[] { 0, 0, 0, 0, 0, 0 }); } } private VisualShape _ShapeBasic;
        private VisualShape ShapeRight { get { return GetCreate(ref _ShapeRight, () => new int[] { 0, 0, 0, 6, 0, 6 }); } } private VisualShape _ShapeRight;
        private VisualShape ShapeLeft { get { return GetCreate(ref _ShapeLeft, () => new int[] { 6, 0, 6, 0, 0, 0 }); } } private VisualShape _ShapeLeft;
        private VisualShape ShapeDown { get { return GetCreate(ref _ShapeDown, () => new int[] { 0, 3, 6, 6, 3, 0 }); } } private VisualShape _ShapeDown;
        private VisualShape ShapeUp { get { return GetCreate(ref _ShapeUp, () => new int[] { 6, 3, 0, 0, 3, 6 }); } } private VisualShape _ShapeUp;
        private VisualShape ShapeDownHalf { get { return GetCreate(ref _ShapeDownHalf, () => new int[] { 0, 0, 3, 3, 0, 0 }); } } private VisualShape _ShapeDownHalf;
        private VisualShape ShapeUpHalf { get { return GetCreate(ref _ShapeUpHalf, () => new int[] { 3, 0, 0, 0, 0, 3 }); } } private VisualShape _ShapeUpHalf;
        private VisualShape ShapeTask { get { return GetCreate(ref _ShapeTask, () => new int[] { 0, 6, 0, 6, 0, 6 }); } } private VisualShape _ShapeTask;
        private VisualShape ShapeUpWide { get { return GetCreate(ref _ShapeUpWide, () => new int[] { 6, 0, 0, 0, 0, 6 }); } } private VisualShape _ShapeUpWide;
        private VisualShape ShapeDownWide { get { return GetCreate(ref _ShapeDownWide, () => new int[] { 0, 0, 6, 6, 0, 0 }); } } private VisualShape _ShapeDownWide;

        private VisualShape GetCreate(ref VisualShape shape, Func<int[]> creator)
        {
            if (shape is null)
                shape = new VisualShape(creator());
            return shape;
        }
        private VisualShape GetCreate(ref VisualShape shape, Func<VisualShape> creator)
        {
            if (shape is null)
                shape = creator();
            return shape;
        }
        #endregion
    }
    internal class MapItemPainter
    {
        /// <summary>
        /// Vrátí virtuální velikost pro daný prvek
        /// </summary>
        /// <param name="mapItem"></param>
        /// <returns></returns>
        internal static SizeF GetVirtualSize(MapItem mapItem, SizeF rasterSize)
        {
            return rasterSize;
        }

        internal static GraphicsPathSet CreateGraphicsPaths(VisualItem visualItem)
        {
            var bounds = visualItem.CurrentBounds;
            GraphicsPathSet set = new GraphicsPathSet(bounds);

            var visualShape = visualItem.VisualShape;

            var outlineColor = visualItem.CurrentOutlineColor;
            if (outlineColor.HasValue)
            {
                set.OutlineBrush = new SolidBrush(outlineColor.Value);
                set.OutlinePath = visualShape.CreateGraphicsPath(bounds, 0f);
            }

            float backgroundMargin = VirtualControl.GetOutlineMargin(visualItem.Zoom);
            var backgroundColor = visualItem.BackColor;
            set.BackgroundBrush = new SolidBrush(backgroundColor);
            set.BackgroundPath = visualShape.CreateGraphicsPath(bounds, backgroundMargin);

            float borderMargin = VirtualControl.GetBorderMargin(visualItem.Zoom);
            var borderColor = Color.Black;
            set.BorderBrush = new SolidBrush(borderColor);
            set.BorderPath = visualShape.CreateGraphicsPath(bounds, backgroundMargin, borderMargin);

            set.TextEmSize = VirtualControl.GetFontSizeEm(visualItem.Zoom);
            set.TextColor = visualItem.TextColor;
            set.TextBounds = visualShape.CreateTextBounds(bounds, 2f * borderMargin);

            return set;
        }
    }
    #region class GraphicsPathSet : Sada grafických nástrojů pro kreslení podkresu - pozadí - okraje prvku
    /// <summary>
    /// Sada grafických nástrojů pro kreslení podkresu - pozadí - okraje prvku.
    /// Neřeší kreslení obsahu (texty, šipky).
    /// </summary>
    internal class GraphicsPathSet : IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="bounds"></param>
        public GraphicsPathSet(Rectangle bounds)
        {
            this.Bounds = bounds;
        }
        /// <summary>
        /// Vnější souřadnice oblasti. Vhodné pro Clip grafiky.
        /// </summary>
        public Rectangle Bounds { get; private set; }
        /// <summary>
        /// Barva pro vykreslení stínu pod prvkem / Selection / MouseOver.
        /// Kreslí se jako první.
        /// </summary>
        public Color? OutlineColor { get; set; }
        /// <summary>
        /// Štětec pro vykreslení stínu pod prvkem / Selection / MouseOver.
        /// Kreslí se jako první.
        /// </summary>
        public Brush OutlineBrush { get; set; }
        /// <summary>
        /// Oblast pro vykreslení stínu pod prvkem / Selection / MouseOver.
        /// Kreslí se jako první.
        /// </summary>
        public GraphicsPath OutlinePath { get; set; }
        /// <summary>
        /// true pokud máme Outline
        /// </summary>
        public bool HasOutline { get { return ((OutlineColor.HasValue || OutlineBrush != null) && OutlinePath != null); } }

        /// <summary>
        /// Barva pro vykreslení pozadí prvku.
        /// Kreslí se jako druhá.
        /// </summary>
        public Color? BackgroundColor { get; set; }
        /// <summary>
        /// Štětec pro vykreslení pozadí prvku.
        /// Kreslí se jako druhá.
        /// </summary>
        public Brush BackgroundBrush { get; set; }
        /// <summary>
        /// Oblast pro vykreslení pozadí prvku.
        /// Kreslí se jako druhá.
        /// </summary>
        public GraphicsPath BackgroundPath { get; set; }
        /// <summary>
        /// true pokud máme Background
        /// </summary>
        public bool HasBackground { get { return ((BackgroundColor.HasValue || BackgroundBrush != null) && BackgroundPath != null); } }

        /// <summary>
        /// Barva pro vykreslení okrajů vnějších a vnitřních předělů, barva okraje.
        /// Kreslí se jako třetí.
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Barva pro vykreslení okrajů vnějších a vnitřních předělů, barva okraje.
        /// Kreslí se jako třetí.
        /// </summary>
        public Brush BorderBrush { get; set; }
        /// <summary>
        /// Oblast pro vykreslení okrajů vnějších a vnitřních předělů, barva okraje.
        /// Kreslí se jako třetí.
        /// </summary>
        public GraphicsPath BorderPath { get; set; }
        /// <summary>
        /// true pokud máme Border
        /// </summary>
        public bool HasBorder { get { return ((BorderColor.HasValue || BorderBrush != null) && BorderPath != null); } }

        /// <summary>
        /// Velikost textu
        /// </summary>
        public float TextEmSize { get; set; }
        /// <summary>
        /// Barva pro vykreslení textu.
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Souřadnice textu
        /// </summary>
        public RectangleF TextBounds { get; set; }

        /// <summary>
        /// Dispose setu
        /// </summary>
        public void Dispose()
        {
            this.OutlineBrush?.Dispose();
            this.OutlinePath?.Dispose();
            this.BackgroundBrush?.Dispose();
            this.BackgroundPath?.Dispose();
            this.BorderBrush?.Dispose();
            this.BorderPath?.Dispose();

            this.OutlineColor = null;
            this.OutlineBrush = null;
            this.OutlinePath = null;
            this.BackgroundColor = null;
            this.BackgroundBrush = null;
            this.BackgroundPath = null;
            this.BorderColor = null;
            this.BorderBrush = null;
            this.BorderPath = null;
        }
    }
    #endregion
    #region class VisualShape : Obálka definující vizuální tvar
    /// <summary>
    /// Obálka definující vizuální tvar
    /// </summary>
    internal class VisualShape
    {
        /// <summary>
        /// Konstruktor
        /// <para/>
        /// Metoda dostává šest hodnot, které vyjadřují relativní posun šesti bodů ve směru osy X, ve směru od okraje prostoru prvku směrem dovnitř. Hodnoty by tedy měly být nula nebo kladné.<br/>
        /// <u>Postupně se jedná o body:</u><br/>
        /// [0] = vlevo nahoře; [1] = vlevo uprostřed; [2] = vlevo dole;<br/>
        /// [3] = vpravo dole; [4] = vpravo uprostřed; [5] = vpravo nahoře;<br/>
        /// <u>Hodnota pro jednotlivý bod:</u><br/>
        /// Nula = bod se nachází na hraně přiděleného prostoru<br/>
        /// Kladná hodnota = bod se blíží směrem ke středu na ose X v prostoru prvku<br/>
        /// Doporučené hodnoty: 0, 2, 3, 6
        /// <para/>
        /// Například pole bodů { 0, 0, 0, 0, 0, 0 } vygeneruje běžný obdélník v celé ploše;<br/>
        /// Například pole bodů { 3, 0, 3, 3, 0, 3 } vygeneruje obdobu klasického šestiúhelníku;<br/>
        /// Například pole bodů { 6, 0, 0, 0, 0, 6 } vygeneruje obdobu domečku;<br/>
        /// Například pole bodů { 0, 6, 0, 6, 0, 6 } vygeneruje obdobu segment BreadCrumbu (pásová šipka ukazující doprava), vhodné pro Operaci výroby;<br/>
        /// </summary>
        /// <param name="pointsX"></param>
        public VisualShape(int[] pointsX)
        {
            _PointsX = pointsX;
        }
        private int[] _PointsX;
        /// <summary>
        /// Vygeneruje fyzickou grafickou komponentu pro svůj tvar
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="margin"></param>
        /// <param name="border"></param>
        /// <returns></returns>
        public GraphicsPath CreateGraphicsPath(RectangleF bounds, float margin, float? border = null)
        {
            if (_PointsX is null || _PointsX.Length < 6)
                throw new InvalidOperationException("VisualShape.CreateGraphicsPath() error: PointsX is invalid.");

            if (bounds.Width < 12f || bounds.Height < 12f) return null;

            GraphicsPath path = new GraphicsPath();

            if (border.HasValue && border.Value > 0f)
            {
                AddPath(bounds, margin, path, false, false);
                AddPath(bounds, margin + border.Value, path, true, true);
            }
            else
            {
                AddPath(bounds, margin, path, false, true);
            }

            return path;
        }
        private void AddPath(RectangleF bounds, float margin, GraphicsPath path, bool reverse, bool closeFigure)
        {
            // Rozměry vnitřního obdélníku, zmenšeného o margin:
            float h = bounds.Height;
            float h2 = h / 2f;
            float module = h / 30f;              // velikost jednotky v PointsX: při výšce 60px je jednotka 2px, hodnota 3 odpovídá 6px, hodnota 6 = 12px, což v grafickém editoru vypadá přiměřeně.
            float l = bounds.X + margin;
            float r = bounds.Right - margin;
            float t = bounds.Y + margin;
            float c = bounds.Y + h2;
            float b = bounds.Bottom - margin;

            // Souřadnice X všech šesti bodů, platné ale na souřadnici bez margins (=nahoře a dole):
            float x0 = module * (float)_PointsX[0];
            float x1 = module * (float)_PointsX[1];
            float x2 = module * (float)_PointsX[2];
            float x3 = module * (float)_PointsX[3];
            float x4 = module * (float)_PointsX[4];
            float x5 = module * (float)_PointsX[5];

            // Korekce souřadnic 0, 2, 3, 5 (pro body na horní a dolní lince) pro případ, kdy je kladný margin, a bod je tedy mírně posunut dolů (pod horní linku) nebo nahoru (nad dolní linku)
            // tak, aby souřadnice X byla přiměřeně odsunutá, když sousední střední bod (1, 4) není svisle (její x1 nebo x4 je jiné než x0 ...)  [ono to chce obrázek]
            if (margin != 0f)
            {
                if (_PointsX[0] != _PointsX[1]) x0 = _ShiftX(x0, x1, h2, margin);
                if (_PointsX[2] != _PointsX[1]) x2 = _ShiftX(x2, x1, h2, margin);
                if (_PointsX[3] != _PointsX[4]) x3 = _ShiftX(x3, x4, h2, margin);
                if (_PointsX[5] != _PointsX[4]) x5 = _ShiftX(x5, x4, h2, margin);
            }

            if(!reverse)
                path.AddPolygon(new PointF[]
                {
                    new PointF(l + x0, t),
                    new PointF(l + x1, c),
                    new PointF(l + x2, b),
                    new PointF(r - x3, b),
                    new PointF(r - x4, c),
                    new PointF(r - x5, t),
                    new PointF(l + x0, t),
                });
            else
                path.AddPolygon(new PointF[]
                {
                    new PointF(l + x0, t),
                    new PointF(r - x5, t),
                    new PointF(r - x4, c),
                    new PointF(r - x3, b),
                    new PointF(l + x2, b),
                    new PointF(l + x1, c),
                    new PointF(l + x0, t),
                });

            if (closeFigure) path.CloseFigure();
        }
        /// <summary>
        /// Zajistí posunutí souřadnice X na šikmé lince z bodu X1 do X2 při dané výšce Y pro posun na ose Y o daný margin
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private static float _ShiftX(float x1, float x2, float y, float margin)
        {
            if (x1 == x2 || y <= 1f) return x1;
            float dx = x1 - x2;
            return x1 - margin * (dx / y);
        }
        /// <summary>
        /// Určí souřadnice oblasti, kam lze psát text
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public RectangleF CreateTextBounds(RectangleF bounds, float margin)
        {
            // Rozměry vnitřního obdélníku, zmenšeného o margin:
            float h = bounds.Height;
            float h2 = h / 2f;
            float module = h / 30f;              // velikost jednotky v PointsX: při výšce 60px je jednotka 2px, hodnota 3 odpovídá 6px, hodnota 6 = 12px, což v grafickém editoru vypadá přiměřeně.
            float l = bounds.X + margin;
            float r = bounds.Right - margin;
            float t = bounds.Y + margin;
            float c = bounds.Y + h2;
            float b = bounds.Bottom - margin;

            // Souřadnice X všech šesti bodů, platné ale na souřadnici bez margins (=nahoře a dole):
            float x0 = module * (float)_PointsX[0];
            float x1 = module * (float)_PointsX[1];
            float x2 = module * (float)_PointsX[2];
            float x3 = module * (float)_PointsX[3];
            float x4 = module * (float)_PointsX[4];
            float x5 = module * (float)_PointsX[5];

            // Největší souřadnice vlevo a vpravo:
            float maxL = _Max(x0, x1, x2);
            float maxR = _Max(x3, x4, x5);

            return RectangleF.FromLTRB(l + maxL, t, r - maxR, b);
        }
        private static float _Max(params float[] values)
        {
            if (values.Length == 0) return 0f;
            return values.Max();
        }

    }
    #endregion
    #region class VisualItem : Prvek mapy umístěný ve virtuálním prostoru
    /// <summary>
    /// Prvek mapy umístěný ve virtuálním prostoru
    /// </summary>
    internal class VisualItem : VirtualItemBase, IVisualItem
    {
        public VisualItem(VisualiserControl visualiser, MapItem mapItem, PointF center, SizeF size)
            : base(visualiser)
        {
            Visualiser = visualiser;
            MapItem = mapItem;
            MapItem.VisualItem = this;
            VirtualBounds.SetCenterSize(center, size);

            BackColor = visualiser.GetVisualObjectBackColor(this.ItemType);
            TextColor = visualiser.GetVisualObjectTextColor(this.ItemType);
            Text = visualiser.GetVisualObjectText(this);
            VisualShape = visualiser.GetVisualObjectShape(this.ItemType);
        }
        public override void Dispose()
        {
            base.Dispose();
            Visualiser = null;
            MapItem.VisualItem = null;
            MapItem = null;
        }
        protected VisualiserControl Visualiser { get; private set; }
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
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get; private set; }
        /// <summary>
        /// Barva písma
        /// </summary>
        public Color TextColor { get; private set; }
        /// <summary>
        /// Text zobrazený v prvku
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Definice vizuálního tvaru
        /// </summary>
        public VisualShape VisualShape { get; private set; }

        /// <summary>
        /// Adresa buňky ve formě Point (X, Y). 
        /// Adresu setuje koordinátor <see cref="Cells"/>, který udržuje vizuální mapu objektů. Adresu může kdykoliv změnit.
        /// Přepočet do virtuální souřadnice provádí metoda <see cref="VisualiserControl.GetVirtualCenter(Point)"/>, kterou toto setování vyvolá. 
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
        public void OnPaintLinks(PaintEventArgs e, Dictionary<long, MapLink> paintedLinks)
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
        public void OnPaintItem(PaintEventArgs e)
        {
            using (var pathSet = MapItemPainter.CreateGraphicsPaths(this))
            {
                e.Graphics.SetClip(pathSet.Bounds);

                // Barvy a tvary pozadí a okrajů:
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                OnPaintPath(e, pathSet.HasOutline, pathSet.OutlineBrush, pathSet.OutlineColor, pathSet.OutlinePath);
                OnPaintPath(e, pathSet.HasBackground, pathSet.BackgroundBrush, pathSet.BackgroundColor, pathSet.BackgroundPath);
                OnPaintPath(e, pathSet.HasBorder, pathSet.BorderBrush, pathSet.BorderColor, pathSet.BorderPath);

                OnPaintText(e, pathSet.TextEmSize, pathSet.TextColor, pathSet.TextBounds);

                e.Graphics.ResetClip();
            }
        }
        /// <summary>
        /// Vykreslí danou Path daným štětcem / barvou
        /// </summary>
        /// <param name="e"></param>
        /// <param name="hasData"></param>
        /// <param name="brush"></param>
        /// <param name="color"></param>
        /// <param name="path"></param>
        private void OnPaintPath(PaintEventArgs e, bool hasData, Brush brush, Color? color, GraphicsPath path)
        {
            if (hasData)
            {
                if (brush != null)
                    e.Graphics.FillPath(brush, path);
                else if (color.HasValue)
                    e.Graphics.FillPath(Visualiser.GetStandardBrush(color.Value), path);
            }
        }
        private void OnPaintText(PaintEventArgs e, float fontEmSize, Color? color, RectangleF textBouds)
        {
            if (!color.HasValue) return;

            string text = this.Text;
            if (String.IsNullOrEmpty(text)) return;

            var font = Visualiser.GetFont(fontEmSize, FontStyle.Regular);
            var brush = Visualiser.GetStandardBrush(color.Value);

            var textSize = e.Graphics.MeasureString(text, font, (int)textBouds.Width);
            var fontBounds = AlignSizeToBounds(textBouds, textSize, ContentAlignment.MiddleLeft, true);
            e.Graphics.DrawString(text, font, brush, fontBounds);
        }
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
    #region interface IVisualItem : Obecný interface pro grafický prvek, enum ItemMouseState
    /// <summary>
    /// Obecný interface pro grafický prvek
    /// </summary>
    public interface IVisualItem
    {
        /// <summary>
        /// Vrstva prvku: čím vyšší hodnota, tím vyšší vrstva = prvek bude kreslen "nad" prvky s nižší vrstvou, a stejně tak bude i aktivní
        /// </summary>
        int Layer { get; }
        /// <summary>
        /// Aktuální [Logaritmický] Zoom (= pro nativní přepočet velikosti Fyzická = Zoom * Virtuální)
        /// </summary>
        float Zoom { get; }
        /// <summary>
        /// Fyzické souřadnice celého prvku v aktuálním controlu.
        /// </summary>
        Rectangle CurrentBounds { get; }
        /// <summary>
        /// Fyzické souřadnice viditelné části prvku v aktuálním controlu, nebo null když prvek není viditelný.
        /// Tedy tyto souřadnice jsou oříznuty do viditelné oblasti
        /// </summary>
        Rectangle? CurrentVisibleBounds { get; }
        /// <summary>
        /// Je prvek aktuálně viditelný v rámci svého Owner controlu ?
        /// </summary>
        bool IsVisibleInOwner { get; }
        /// <summary>
        /// Sem je nastaveno true/false v okamžiku vyhodnocené viditelnosti prvku.
        /// </summary>
        bool CurrentlyIsVisible { get; set; }
        /// <summary>
        /// Je prvek aktivní na dané fyzické souřadnici?
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        bool IsActiveOnCurrentPoint(Point currentPoint);
        /// <summary>
        /// Stav prvku z hlediska myši
        /// </summary>
        ItemMouseState MouseState { get; set; }
        /// <summary>
        /// Prvek je Selectován
        /// </summary>
        bool Selected { get; set; }
        /// <summary>
        /// Aktuální barva orámování, vychází z hodnot <see cref="Selected"/> a <see cref="MouseState"/>
        /// </summary>
        Color? CurrentOutlineColor { get; }
        /// <summary>
        /// Vykresli se do grafiky
        /// </summary>
        /// <param name="e"></param>
        void OnPaintItem(PaintEventArgs e);
        /// <summary>
        /// Vykreslí svoje linky, pokud dosud nejsou v <paramref name="paintedLinks"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="paintedLinks"></param>
        void OnPaintLinks(PaintEventArgs e, Dictionary<long, MapLink> paintedLinks);
    }
    /// <summary>
    /// Stav prvku z hlediska myši
    /// </summary>
    public enum ItemMouseState
    {
        None,
        OnMouse,
        LeftDown,
        RightDown
    }
    #endregion
}
