using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Panel Dílenské tabule: obsahuje všechny prvky pro zobrazení dat (gridy), ale neobsahuje Toolbar.
    /// </summary>
    public class SchedulerPanel : InteractiveContainer, IInteractiveItem
    {
        #region Konstruktor, inicializace, privátní proměnné
        public SchedulerPanel()
        {
            this._InitComponents();
        }
        private void _InitComponents()
        {
            this._TaskGrid = new GGrid();
            this._TaskSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 300, BoundsNonActive = new Int32NRange(0, 200) };
            this._WorkGrid = new GGrid();
            this._SourceSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 600, BoundsNonActive = new Int32NRange(0, 200) };
            this._SourceGrid = new GGrid();
            this._InfoSplitter = new GSplitter() { SplitterVisibleWidth = SplitterSize, SplitterActiveOverlap = 2, Orientation = Orientation.Horizontal, Value = 300, BoundsNonActive = new Int32NRange(0, 600) };
            this._InfoGrid = new GGrid();

            this.AddItem(this._TaskGrid);
            this.AddItem(this._TaskSplitter);
            this.AddItem(this._WorkGrid);
            this.AddItem(this._SourceSplitter);
            this.AddItem(this._SourceGrid);
            this.AddItem(this._InfoSplitter);
            this.AddItem(this._InfoGrid);

            this._TaskSplitter.ValueChanging += LayoutChanging;
            this._TaskSplitter.ValueChanged += LayoutChanging;
            this._SourceSplitter.ValueChanging += LayoutChanging;
            this._SourceSplitter.ValueChanged += LayoutChanging;
            this._InfoSplitter.ValueChanging += LayoutChanging;
            this._InfoSplitter.ValueChanged += LayoutChanging;
        }
        /// <summary>
        /// Po změně velikosti controlu
        /// </summary>
        /// <param name="oldBounds"></param>
        /// <param name="newBounds"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            this.CalculateLayout();
        }
        /// <summary>
        /// Po změně pozice kteréhokoliv splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutChanging(object sender, GPropertyChangeArgs<int> e)
        {
            this.CalculateLayout();
        }
        private void CalculateLayout()
        {
            if (this.IsSuppressedEvent) return;

            using (this.SuppressEvents())
            {
                Size size = this.ClientSize;

                int sp = SplitterSize / 2;                 // Prostor "před splitterem" = odsazení souřadnice Prev.End před souřadnicí Splitter.Value
                int sn = SplitterSize - sp;                // Prostor "za splitterem" = odsazení souřadnice Next.Begin za souřadnicí Splitter.Value

                int width = size.Width;
                int maxw = width / 3;
                int x0 = 0;
                int x3 = width;

                bool isTaskVisible = (this._IsTaskVisible && width >= MinControlWidthForSideGrids);
                int x1 = CalculateLayoutOne(isTaskVisible, x0, this._TaskSplitter.Value, x0 + MinGridWidth, x0 + maxw);

                bool isWorkVisible = true;

                bool isSourceVisible = (this._IsSourceVisible && width >= MinControlWidthForSideGrids);
                int x2 = CalculateLayoutOne(isSourceVisible, x3, this._SourceSplitter.Value, x3 - maxw, x3 - MinGridWidth);

                int height = size.Height;
                int maxh = height / 2;
                int y0 = 0;
                int y3 = size.Height;

                bool isInfoVisible = (this._IsInfoVisible && height >= MinControlHeightForSideGrids);
                int y2 = CalculateLayoutOne(isInfoVisible, y3, this._InfoSplitter.Value, y3 - maxh, y3 - MinGridHeight);

                bool isChangeChildItems = (this._TaskGrid.IsVisible != isTaskVisible) ||
                                          (this._TaskSplitter.IsVisible != isTaskVisible) ||
                                          (this._WorkGrid.IsVisible != isWorkVisible) ||
                                          (this._SourceSplitter.IsVisible != isSourceVisible) ||
                                          (this._SourceGrid.IsVisible != isSourceVisible) ||
                                          (this._InfoSplitter.IsVisible != isInfoVisible) ||
                                          (this._InfoGrid.IsVisible != isInfoVisible);

                this._TaskGrid.IsVisible = isTaskVisible;
                this._TaskSplitter.IsVisible = isTaskVisible;
                this._WorkGrid.IsVisible = isWorkVisible;
                this._SourceSplitter.IsVisible = isSourceVisible;
                this._SourceGrid.IsVisible = isSourceVisible;
                this._InfoSplitter.IsVisible = isInfoVisible;
                this._InfoGrid.IsVisible = isInfoVisible;

                if (isTaskVisible)
                {
                    int r = x1 - sp;
                    int b = y2 - (isInfoVisible ? sp : 0);
                    this._TaskGrid.Bounds = new Rectangle(x0, y0, r - x0, b - y0);
                    this._TaskSplitter.LoadFrom(this._TaskGrid.Bounds, RectangleSide.Right, true);
                }
                if (isWorkVisible)
                {
                    int l = x1 + (isTaskVisible ? sn : 0);
                    int r = x2 - (isSourceVisible ? sp : 0);
                    int t = y0;
                    int b = y2 - (isInfoVisible ? sp : 0);
                    this._WorkGrid.Bounds = new Rectangle(l, t, r - l, b - t);
                }
                if (isSourceVisible)
                {
                    int l = x2 + sn;
                    int b = y2 - (isInfoVisible ? sp : 0);
                    this._SourceGrid.Bounds = new Rectangle(l, y0, x3 - l, b - y0);
                    this._SourceSplitter.LoadFrom(this._SourceGrid.Bounds, RectangleSide.Left, true);
                }
                if (isInfoVisible)
                {
                    int t = y2 + sn;
                    this._InfoGrid.Bounds = new Rectangle(x0, t, x3 - x0, y3 - t);
                    this._InfoSplitter.LoadFrom(this._InfoGrid.Bounds, RectangleSide.Top, true);
                }

                if (isChangeChildItems)
                    this._IsChildValid = false;
            }
        }
        private static int CalculateLayoutOne(bool isVisible, int valueInvisible, int valueVisible, int valueMin, int valueMax)
        {
            if (!isVisible) return valueInvisible;
            if (valueVisible < valueMin) return valueMin;
            if (valueVisible > valueMax) return valueMax;
            return valueVisible;
        }
        private const int MinControlWidthForSideGrids = 800;
        private const int MinControlHeightForSideGrids = 600;
        private const int MinGridWidth = 95;
        private const int MinGridHeight = 75;
        private const int SplitterSize = 4;
        private GGrid _TaskGrid;
        private GSplitter _TaskSplitter;
        private bool _IsTaskVisible;
        private GGrid _WorkGrid;
        private GSplitter _SourceSplitter;
        private GGrid _SourceGrid;
        private bool _IsSourceVisible;
        private GSplitter _InfoSplitter;
        private GGrid _InfoGrid;
        private bool _IsInfoVisible;
        #endregion
        #region Child items
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._ChildList; } }
        private void _ChildListValidate()
        {
            if (this._ChildList != null && this._IsChildValid) return;
        }
        private List<IInteractiveItem> _ChildList;
        private bool _IsChildValid;
        #endregion
        #region Public data
        /// <summary>
        /// true pokud je viditelná tabulka úkolů k zapracování
        /// </summary>
        public bool IsTaskVisible { get { return this._IsTaskVisible; } set { this._IsTaskVisible = value; this.CalculateLayout();  } }
        /// <summary>
        /// true pokud je viditelná tabulka zdrojů k zaplánování
        /// </summary>
        public bool IsSourceVisible { get { return this._IsSourceVisible; } set { this._IsSourceVisible = value; this.CalculateLayout(); } }
        /// <summary>
        /// true pokud je viditelná tabulka informací o zaplánování
        /// </summary>
        public bool IsInfoVisible { get { return this._IsInfoVisible; } set { this._IsInfoVisible = value; this.CalculateLayout(); } }


        #endregion
    }
}
