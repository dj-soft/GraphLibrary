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
            MouseInit();
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
        { }
        protected void CheckData()
        { }

        private Dictionary<int, VisualItem> _VisualItems;
        #endregion
        #region Myš
        protected void MouseInit()
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
            else
                VirtualSpace.MouseDrag(e.Location, e.Button);
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            ActivateItemForPoint(e.Location, e.Button);
            CurrentItemMouseDown = CurrentItemAtMouse;
            VirtualSpace.MouseDown(e.Location, e.Button);
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (VirtualSpace.IsMouseDrag)
                VirtualSpace.MouseUp(e.Location);
            else
                SelectItem(CurrentItemMouseDown);
            ActivateItemForPoint(e.Location, MouseButtons.None);
        }
        private void _MouseLeave(object sender, EventArgs e)
        {
            this.CurrentItemAtMouse = null;
        }
        private void _MouseWheel(object sender, MouseEventArgs e)
        {
            VirtualSpace.MouseWheel(e.Location, e.Delta);
        }


        /// <summary>
        /// Najde a aktivuje prvek pod myší
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="button"></param>
        private void ActivateItemForPoint(Point currentPoint, MouseButtons button)
        {
            var activeItems = _SearchForItemsOnPoint(currentPoint);
            var activeItem = SearchForActiveItem(activeItems, CurrentItemAtMouse);
            if (activeItem != null && button != MouseButtons.None)
                activeItem.MouseState = (button == MouseButtons.Left ? ItemMouseState.LeftDown : (button == MouseButtons.Right ? ItemMouseState.RightDown : ItemMouseState.OnMouse));
            this.CurrentItemAtMouse = activeItem;
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
        public IVisualItem CurrentItemAtMouse
        {
            get { return _CurrentItemAtMouse; }
            set
            {
                var oldItem = _CurrentItemAtMouse;
                var newItem = value;
                if (Object.ReferenceEquals(oldItem, newItem)) return;

                if (oldItem != null) oldItem.MouseState = ItemMouseState.None;
                _CurrentItemAtMouse = newItem;
                if (newItem != null) newItem.MouseState = ItemMouseState.OnMouse;

                this.Invalidate();
            }
        }
        /// <summary>
        /// Aktuálně aktivní prvek s myší, proměnná
        /// </summary>
        private IVisualItem _CurrentItemAtMouse;
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
        private IVisualItem CurrentItemMouseDown;
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
                    var itemSize = MapItemPainter.GetVirtualSize(mapItem);
                    var visualItem = new VisualItem(this, mapItem, new PointF(x, y), itemSize);
                    _VisualItems.Add(mapItem.ItemId, visualItem);
                    y += 80f;
                    if (y > 600)
                    {
                        x += 90f;
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

        internal static GraphicsPathSet CreateGraphicsPaths(IVisualItem visualItem, MapItem mapItem)
        {
            var bounds = visualItem.CurrentBounds;
            GraphicsPathSet set = new GraphicsPathSet(bounds);

            var outlineColor = visualItem.CurrentOutlineColor;
            if (outlineColor.HasValue)
            {
                set.OutlineBrush = new SolidBrush(outlineColor.Value);
                set.OutlinePath = new GraphicsPath();
                set.OutlinePath.AddRectangle(bounds);
            }

            var backgroundColor = Color.FromArgb(200, 200, 218);
            set.BackgroundBrush = new SolidBrush(backgroundColor);
            set.BackgroundPath = new GraphicsPath();
            Rectangle backgroundBounds = new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 6);
            set.BackgroundPath.AddRectangle(backgroundBounds);

            return set;
        }
    }
    internal class GraphicsPathSet : IDisposable
    {
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


        public void OnPaint(PaintEventArgs e)
        {
            using (var pathSet = MapItemPainter.CreateGraphicsPaths(this, this.MapItem))
            {
                // Barvy a tvary pozadí a okrajů:
                if (pathSet.OutlineBrush != null) e.Graphics.FillPath(pathSet.OutlineBrush, pathSet.OutlinePath);
                if (pathSet.BackgroundBrush != null) e.Graphics.FillPath(pathSet.BackgroundBrush, pathSet.BackgroundPath);
                if (pathSet.BorderBrush != null) e.Graphics.FillPath(pathSet.BorderBrush, pathSet.BorderPath);



            }
        }
        bool IVisualItem.IsActiveOnCurrentPoint(Point currentPoint)
        {
            var currentBounds = this.CurrentVisibleBounds;
            return (currentBounds.HasValue && currentBounds.Value.Contains(currentPoint));
        }
    }
    #endregion
    public interface IVisualItem
    {
        /// <summary>
        /// Vrstva prvku: čím vyšší hodnota, tím vyšší vrstva = prvek bude kreslen "nad" prvky s nižší vrstvou, a stejně tak bude i aktivní
        /// </summary>
        int Layer { get; }
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
        /// Je prvek aktivní na dané fyzické souřadnici?
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <returns></returns>
        bool IsActiveOnCurrentPoint(Point currentPoint);
        /// <summary>
        /// Je prvek aktuálně viditelný v rámci svého Owner controlu ?
        /// </summary>
        bool IsVisibleInOwner { get; }
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
}
