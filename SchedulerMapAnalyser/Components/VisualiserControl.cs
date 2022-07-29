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
        public VisualiserControl()
        {
            ControlInit();
            DataInit();
            _MouseInit();
            SelectionInit();
        }
        protected void ControlInit()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.UserMouse, true);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
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
            if (e.Button == MouseButtons.None)
                ActivateItemForPoint(e.Location, MouseButtons.None);
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            ActivateItemForPoint(e.Location, e.Button);
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (!VirtualSpace.IsMouseDrag)
                SelectItem(CurrentItemMouseDown);
            ActivateItemForPoint(e.Location, MouseButtons.None);
        }
        private void _MouseLeave(object sender, EventArgs e)
        {
            this.SetCurrentItemAtMouse(null, null);
        }
        private void _MouseWheel(object sender, MouseEventArgs e)
        {
        }


        /// <summary>
        /// Najde a aktivuje prvek pod myší
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="button"></param>
        private void ActivateItemForPoint(Point currentPoint, MouseButtons button)
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
        protected void SelectItem(IVisualItem selectedItem)
        {
            bool extendedSelect = (Control.ModifierKeys == Keys.Control);
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
            items.AddRange(_VisualItems.Values.Where(i => i.IsVisibleInOwner));
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



        public void ActivateMapItem(int? itemId)
        {
            if (_MapSegment is null) return;
            _VisualItems.Clear();

            _ActiveItemId = itemId;
            if (!(_MapSegment.IsLoaded || _MapSegment.IsLoading))
            {
                Task.Factory.StartNew(_LoadMapSegment);
            }
            else
            {
                _ActivateMapItem();
            }

        }
        private int? _ActiveItemId;
        private void _LoadMapSegment()
        {
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
            var itemId = _ActiveItemId;

            if (_MapSegment != null && _MapSegment.ItemsCount > 0)
            {
                float x = 60f;
                float y = 35f;
                foreach (var mapItem in _MapSegment.Items)
                {
                    if (_VisualItems.Count > 5000) break;

                    var itemSize = MapItemPainter.GetVirtualSize(mapItem);
                    var visualItem = new VisualItem(this, mapItem, new PointF(x, y), itemSize);
                    _VisualItems.Add(mapItem.ItemId, visualItem);
                    y += 80f;
                    if (y > 1100)
                    {
                        x += 85f;
                        y = 35f;
                    }
                }
                RefreshContent();
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            foreach (var item in VisibleItems)
                item.OnPaint(e);
        }
        #region Typy objektů, barvy a Tvary
        /// <summary>
        /// Vrátí typ objektu pro daný objekt. Typ objektu deklaruje barvy a tvary.
        /// </summary>
        /// <param name="mapItem"></param>
        /// <returns></returns>
        internal VisualObjectType GetVisualObjectType(MapItem mapItem)
        {
            var type = mapItem.Type;
            switch (type)
            {
                case "IncrementByRealSupplierOrder": return VisualObjectType.IncrementByRealSupplierOrder;
                case "IncrementByPlanSupplierOrder": return VisualObjectType.IncrementByPlanSupplierOrder;
                case "IncrementByProposalReceipt": return VisualObjectType.IncrementByProposalReceipt;
                case "DecrementByProposalRequisition": return VisualObjectType.DecrementByProposalRequisition;
                case "IncrementByPlanStockTransfer": return VisualObjectType.IncrementByPlanStockTransfer;
                case "DecrementByRealComponent": return VisualObjectType.DecrementByRealComponent;
                case "DecrementByPlanComponent": return VisualObjectType.DecrementByPlanComponent;
                case "IncrementByRealByProductSuitable": return VisualObjectType.IncrementByRealByProductSuitable;
                case "IncrementByPlanByProductSuitable": return VisualObjectType.IncrementByPlanByProductSuitable;
                case "IncrementByRealByProductDissonant": return VisualObjectType.IncrementByRealByProductDissonant;
                case "IncrementByPlanByProductDissonant": return VisualObjectType.IncrementByPlanByProductDissonant;
                case "IncrementByRealProductOrder": return VisualObjectType.IncrementByRealProductOrder;
                case "IncrementByPlanProductOrder": return VisualObjectType.IncrementByPlanProductOrder;
                case "DecrementByRealEnquiry": return VisualObjectType.DecrementByRealEnquiry;
                case "DecrementByPlanEnquiry": return VisualObjectType.DecrementByPlanEnquiry;
            }

            if (type.StartsWith("OpVP: ")) return VisualObjectType.OperationPlan;
            if (type.StartsWith("OpSTPV: ")) return VisualObjectType.OperationReal;

            return VisualObjectType.None;
        }
        /// <summary>
        /// Vrátí barvu pozdí pro prvek daného typu.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        internal Color GetVisualObjectBackColor(VisualObjectType visualType)
        {
            switch (visualType)
            {
                case VisualObjectType.IncrementByRealSupplierOrder: return Color.FromArgb(255, 94, 255, 94);
                case VisualObjectType.IncrementByPlanSupplierOrder: return Color.FromArgb(255, 193, 255, 193);
                case VisualObjectType.IncrementByProposalReceipt: return Color.FromArgb(255, 191, 255, 223);
                case VisualObjectType.DecrementByProposalRequisition: return Color.FromArgb(255, 255, 255, 211);
                case VisualObjectType.IncrementByPlanStockTransfer: return Color.FromArgb(255, 224, 255, 193);
                case VisualObjectType.DecrementByRealComponent: return Color.FromArgb(255, 214, 94, 255);
                case VisualObjectType.DecrementByPlanComponent: return Color.FromArgb(255, 232, 163, 255);
                case VisualObjectType.IncrementByRealByProductSuitable: return Color.FromArgb(255, 255, 137, 255);
                case VisualObjectType.IncrementByPlanByProductSuitable: return Color.FromArgb(255, 240, 196, 255);
                case VisualObjectType.IncrementByRealByProductDissonant: return Color.FromArgb(255, 204, 153, 255);
                case VisualObjectType.IncrementByPlanByProductDissonant: return Color.FromArgb(255, 255, 196, 255);
                case VisualObjectType.OperationPlan: return Color.FromArgb(255, 119, 119, 255);
                case VisualObjectType.OperationReal: return Color.FromArgb(255, 168, 168, 255);
                case VisualObjectType.IncrementByRealProductOrder: return Color.FromArgb(255, 68, 255, 255);
                case VisualObjectType.IncrementByPlanProductOrder: return Color.FromArgb(255, 158, 255, 255);
                case VisualObjectType.DecrementByRealEnquiry: return Color.FromArgb(255, 255, 81, 81);
                case VisualObjectType.DecrementByPlanEnquiry: return Color.FromArgb(255, 255, 173, 173);
            }
            return Color.FromArgb(255, 216, 216, 216);
        }
        /// <summary>
        /// Metoda vrátí definici tvaru pro prvek daného typu.
        /// </summary>
        /// <param name="visualType"></param>
        /// <returns></returns>
        internal VisualShape GetVisualObjectShape(VisualObjectType visualType)
        {
            switch (visualType)
            {
                case VisualObjectType.IncrementByRealSupplierOrder: 
                case VisualObjectType.IncrementByPlanSupplierOrder: 
                case VisualObjectType.IncrementByProposalReceipt: return ShapeRight;
                case VisualObjectType.DecrementByProposalRequisition: return ShapeDown; 
                case VisualObjectType.IncrementByPlanStockTransfer: return ShapeUp;
                case VisualObjectType.DecrementByRealComponent: 
                case VisualObjectType.DecrementByPlanComponent: return ShapeDownHalf;
                case VisualObjectType.IncrementByRealByProductSuitable: 
                case VisualObjectType.IncrementByPlanByProductSuitable: return ShapeUpHalf;
                case VisualObjectType.IncrementByRealByProductDissonant: 
                case VisualObjectType.IncrementByPlanByProductDissonant: return ShapeUpHalf;
                case VisualObjectType.OperationPlan: 
                case VisualObjectType.OperationReal: return ShapeTask;
                case VisualObjectType.IncrementByRealProductOrder: 
                case VisualObjectType.IncrementByPlanProductOrder: return ShapeUpWide;
                case VisualObjectType.DecrementByRealEnquiry: 
                case VisualObjectType.DecrementByPlanEnquiry: return ShapeLeft;
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
    internal enum VisualObjectType
    {
        None,
        IncrementByRealSupplierOrder,
        IncrementByPlanSupplierOrder,
        IncrementByProposalReceipt,
        DecrementByProposalRequisition,
        IncrementByPlanStockTransfer,
        DecrementByRealComponent,
        DecrementByPlanComponent,
        OperationReal,
        OperationPlan,
        IncrementByRealByProductSuitable,
        IncrementByPlanByProductSuitable,
        IncrementByRealByProductDissonant,
        IncrementByPlanByProductDissonant,
        IncrementByRealProductOrder,
        IncrementByPlanProductOrder,
        DecrementByRealEnquiry,
        DecrementByPlanEnquiry
    }
    internal class MapItemPainter
    {
        /// <summary>
        /// Vrátí virtuální velikost pro daný prvek
        /// </summary>
        /// <param name="mapItem"></param>
        /// <returns></returns>
        internal static SizeF GetVirtualSize(MapItem mapItem)
        {
            return new SizeF(80f, 60f);
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

            var backgroundColor = visualItem.BackColor;
            set.BackgroundBrush = new SolidBrush(backgroundColor);
            set.BackgroundPath = visualShape.CreateGraphicsPath(bounds, 3f);

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
        public Brush OutlineBrush { get; set; }
        /// <summary>
        /// Oblast pro vykreslení stínu pod prvkem / Selection / MouseOver.
        /// Kreslí se jako první.
        /// </summary>
        public GraphicsPath OutlinePath { get; set; }
        /// <summary>
        /// true pokud máme Outline
        /// </summary>
        public bool HasOutline { get { return (OutlineBrush != null && OutlinePath != null); } }
        /// <summary>
        /// Barva pro vykreslení pozadí prvku.
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
        public bool HasBackground { get { return (BackgroundBrush != null && BackgroundPath != null); } }
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
        public bool HasBorder { get { return (BorderBrush != null && BorderPath != null); } }
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

            this.OutlineBrush = null;
            this.OutlinePath = null;
            this.BackgroundBrush = null;
            this.BackgroundPath = null;
            this.BorderBrush = null;
            this.BorderPath = null;
        }
    }
    #endregion
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

        public GraphicsPath CreateGraphicsPath(RectangleF bounds, float margin)
        {
            if (_PointsX is null || _PointsX.Length < 6)
                throw new InvalidOperationException("VisualShape.CreateGraphicsPath() error: PointsX is invalid.");

            float module = bounds.Height / 30f;            // velikost jednotky v PointsX: při výšce 60px je jednotka 2px, hodnota 3 odpovídá 6px, hodnota 6 = 12px, což v grafickém editoru vypadá přiměřeně.
            float l = bounds.X + margin;
            float r = bounds.Right - margin;
            float t = bounds.Y + margin;
            float c = bounds.Y + bounds.Height / 2f;
            float b = bounds.Bottom - margin;
            if ((r - l) <= 12f || (b - t) <= 6f) return null;

            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(new PointF[]
            {
                new PointF(l + module * (float)_PointsX[0], t),
                new PointF(l + module * (float)_PointsX[1], c),
                new PointF(l + module * (float)_PointsX[2], b),
                new PointF(r - module * (float)_PointsX[3], b),
                new PointF(r - module * (float)_PointsX[4], c),
                new PointF(r - module * (float)_PointsX[5], t),
                new PointF(l + module * (float)_PointsX[0], t),
            });
            path.CloseFigure();
            return path;
        }
    }

    #region class VisualItem : Prvek mapy umístěný ve virtuálním prostoru
    internal class VisualItem : VirtualItemBase, IVisualItem
    {
        public VisualItem(VisualiserControl visualiser, MapItem mapItem, PointF center, SizeF size)
            : base(visualiser)
        {
            Visualiser = visualiser;
            MapItem = mapItem;
            VirtualBounds.SetCenterSize(center, size);
        }
        public override void Dispose()
        {
            base.Dispose();
            Visualiser = null;
            MapItem = null;
        }
        protected VisualiserControl Visualiser { get; private set; }
        protected MapItem MapItem { get; private set; }
        /// <summary>
        /// Typ tvaru
        /// </summary>
        public VisualObjectType VisualType
        {
            get
            {
                if (!_VisualType.HasValue)
                    _VisualType = Visualiser.GetVisualObjectType(this.MapItem);
                return _VisualType.Value;
            }
        }
        private VisualObjectType? _VisualType;
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get { return Visualiser.GetVisualObjectBackColor(this.VisualType); } }
        /// <summary>
        /// Definice vizuálního tvaru
        /// </summary>
        public VisualShape VisualShape { get { return Visualiser.GetVisualObjectShape(this.VisualType); } }

        public void OnPaint(PaintEventArgs e)
        {
            using (var pathSet = MapItemPainter.CreateGraphicsPaths(this))
            {
                // Barvy a tvary pozadí a okrajů:
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                if (pathSet.HasOutline) e.Graphics.FillPath(pathSet.OutlineBrush, pathSet.OutlinePath);
                if (pathSet.HasBackground) e.Graphics.FillPath(pathSet.BackgroundBrush, pathSet.BackgroundPath);
                if (pathSet.HasBorder) e.Graphics.FillPath(pathSet.BorderBrush, pathSet.BorderPath);
            }
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
        void OnPaint(PaintEventArgs e);
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
