using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Djs.Common.Data;

namespace Djs.Common.Components
{
    public class GTimeGraph : InteractiveContainer, ITimeInteractiveGraph
    {
        public GTimeGraph()
        {
            this._ItemList = new EList<ITimeGraphItem>();
            this._ItemList.ItemAddAfter += new EList<ITimeGraphItem>.EListEventAfterHandler(_ItemList_ItemAddAfter);
            this._ItemList.ItemRemoveAfter += new EList<ITimeGraphItem>.EListEventAfterHandler(_ItemList_ItemRemoveAfter);

            this.LineUnitHeight = 6;
            this.GraphHeightRange = new Int32Range(16, 125);
            
        }
        #region Items
        /// <summary>
        /// All Items in graph
        /// </summary>
        public EList<ITimeGraphItem> ItemList { get { return this._ItemList; } } private EList<ITimeGraphItem> _ItemList;
        private void _ItemList_ItemRemoveAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { this.InvalidateItemList(); }
        private void _ItemList_ItemAddAfter(object sender, EList<ITimeGraphItem>.EListAfterEventArgs args) { this.InvalidateItemList(); }
        protected void InvalidateItemList()
        {
            this._ItemValid = false;
        }
        private bool _ItemValid;
        #endregion
        #region CheckValid, recalculate validity
        protected void CheckValid()
        {
            using (var scope = Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "GTimeGraph", "CheckValid", ""))
            {
                this.CheckValidTimeAxis();
                this.CheckValidLogicalY();
                this.CheckValidVisibleList();
            }
        }
        #endregion
        #region TimeAxis : CheckValid, Identity
        protected void CheckValidTimeAxis()
        {
            string identity = this.TimeAxisIdentityCurrent;
            if (String.Equals(identity, this.TimeAxisIdentityPrevious)) return;
            this.TimeAxisReload();
            this.TimeAxisIdentityPrevious = identity;
        }
        /// <summary>
        /// Reload all relevant data from current TimeAxis
        /// </summary>
        protected void TimeAxisReload()
        {
            this.TimeAxisTicks = this._TimeConvertor.Ticks.Where(t => t.TickType == AxisTickType.BigLabel || t.TickType == AxisTickType.StdLabel || t.TickType == AxisTickType.BigTick).ToArray();
            this.TimeAxisBegin = this._TimeConvertor.GetPixel(this._TimeConvertor.VisibleTime.Begin);

            this.InvalidateVisibleList();
        }
        /// <summary>
        /// Contain array of Tick from current Axis (from this._TimeConvertor.Ticks), only Ticks of type: ( BigLabel || StdLabel || BigTick )
        /// </summary>
        protected VisualTick[] TimeAxisTicks;
        /// <summary>
        /// Relative position X in pixel where TimeAxis begins
        /// </summary>
        protected int TimeAxisBegin;
        /// <summary>
        /// Identity of TimeAxis on last calculate of VisibleList
        /// </summary>
        protected string TimeAxisIdentityPrevious;
        /// <summary>
        /// Identity of current TimeAxis
        /// </summary>
        protected string TimeAxisIdentityCurrent { get { return (this._TimeConvertor != null ? this._TimeConvertor.Identity : null); } }
        /// <summary>
        /// TimeConvertor
        /// </summary>
        private ITimeConvertor _TimeConvertor;
        #endregion
        #region LogY : Grouping items of type ITimeGraphItem to GTimeGraphGroup, Sorting array of ITimeGraphItem to Logical Y axis
        /// <summary>
        /// Invalidate LogY.
        /// Call after add/remove any GraphItem, or after change GraphItem properties: Time, Height
        /// </summary>
        protected void InvalidateLogicalY()
        {
            this.IsValidLogicalY = false;
        }
        /// <summary>
        /// Check validity LogY.
        /// </summary>
        protected void CheckValidLogicalY()
        {
            if (this.IsValidLogicalY) return;
            this.ItemsRecalculateLogicalY();
        }
        /// <summary>
        /// true when LogY is valid
        /// </summary>
        protected bool IsValidLogicalY;
        /// <summary>
        /// Recalculate LogY for all Items
        /// </summary>
        protected void ItemsRecalculateLogicalY()
        {
            using (var scope = Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "GTimeGraph", "ItemsRecalculateLogY", ""))
            {
                int layers = 0;
                int levels = 0;
                int groups = 0;
                int items = this.ItemList.Count;

                this.ItemGroupList = new List<List<GTimeGraphGroup>>();
                Interval<float> usedHeightLogY = new Interval<float>(0f, 0f, true);

                // In groups by visual Layer, ascending:
                List<IGrouping<int, ITimeGraphItem>> layerGroups = this.ItemList.GroupBy(i => i.Layer).ToList();
                if (layerGroups.Count > 1)
                    layerGroups.Sort((a, b) => a.Key.CompareTo(b.Key));
                layers = layerGroups.Count;

                foreach (IGrouping<int, ITimeGraphItem> layerGroup in layerGroups)
                {
                    // Each layer has its own array of usage. This array is common to all Levels:
                    PointArray<DateTime, IntervalArray<float>> layerUsing = new PointArray<DateTime, IntervalArray<float>>();

                    // Layer value for this group. One Layer is equivalent to graphical layer, items can be drawed one over other.
                    int layer = layerGroup.Key;

                    // In one groups: subgrouping by Level value, ascending:
                    List<IGrouping<int, ITimeGraphItem>> levelGroups = layerGroup.GroupBy(i => i.Level).ToList();
                    if (levelGroups.Count > 1)
                        levelGroups.Sort((a, b) => a.Key.CompareTo(b.Key));
                    levels += levelGroups.Count;

                    // Level is "visual group" of more items in one range of Y coordinates.
                    // Negative levels are drawed to bottom (as negative values on Y axis).
                    List<GTimeGraphGroup> layerGroupList = new List<GTimeGraphGroup>();

                    Interval<float> yUsed = new Interval<float>(0f, 0f, true);
                    foreach (IGrouping<int, ITimeGraphItem> levelGroup in levelGroups)
                    {
                        layerUsing.Clear();
                        this.ItemsRecalculateLogicalYOneLevel(layer, layerUsing, levelGroup.Key, levelGroup, layerGroupList, yUsed, ref groups);
                    }
                    usedHeightLogY.MergeWith(yUsed);

                    this.ItemGroupList.Add(layerGroupList);
                }

                this.CalculatorYPrepare(usedHeightLogY);

                this.InvalidateVisibleList();
                this.IsValidLogicalY = true;

                scope.AddItem("Layers Count: " + layers.ToString());
                scope.AddItem("Levels Count: " + levels.ToString());
                scope.AddItem("Groups Count: " + groups.ToString());
                scope.AddItem("Items Count: " + items.ToString());
            }
        }
        /// <summary>
        /// Recalculate one level in one layer
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layerUsing"></param>
        /// <param name="level"></param>
        /// <param name="levelItems"></param>
        /// <param name="yUsed"></param>
        protected void ItemsRecalculateLogicalYOneLevel(int layer, PointArray<DateTime, IntervalArray<float>> layerUsing, int level, IEnumerable<ITimeGraphItem> levelItems, List<GTimeGraphGroup> layerGroupList, Interval<float> yUsed, ref int groups)
        {
            bool isDownward = (level < 0 );
            float searchFrom = (isDownward ? yUsed.Begin : yUsed.End);
            float nextSearch = searchFrom;

            // by GroupId (one Group has more ITimeGraphItem, with same GroupId, for example: more working times for one job):
            List<GTimeGraphGroup> groupList = new List<GTimeGraphGroup>();
            IEnumerable<IGrouping<int, ITimeGraphItem>> groupArray = levelItems.GroupBy(i => i.GroupId);
            foreach (IGrouping<int, ITimeGraphItem> group in groupArray)
                groupList.Add(new GTimeGraphGroup(group));
            // Sort by Time.Begin ASC:
            if (groupList.Count > 1)
                groupList.Sort((a, b) => GTimeGraphGroup.ItemsRecalculateLogicalYCompare(a, b));
            groups += groupList.Count;

            // Now recalculate Y position for each item from GroupItem list, by its time and height and use pattern on time:
            foreach (GTimeGraphGroup group in groupList)
            {
                if (group.IsValidRealTime)
                {   // Group is Real:
                    // Array of IntervalArray for group.Time, insert explicit points for Begin and End (when this points does not exists), include last item (where Point (on X axis) == end):
                    DateTime begin = group.Time.Begin.Value;
                    DateTime end = group.Time.End.Value;
                    var intervalAllItems = layerUsing.Search(begin, end, true);

                    // Array of found IntervalArray, except last item:
                    var intervalWorkItems = intervalAllItems.GetRange(0, intervalAllItems.Count - 1);

                    // Summary of all intervals of usage this graph row within our time (except last row):
                    IntervalArray<float> summary = (intervalWorkItems.Count > 1 ? IntervalArray<float>.Summary(intervalWorkItems.Select(i => i.Value.Value)) : intervalWorkItems[0].Value.Value);

                    // Negative level search for negative size (downward):
                    float size = (isDownward ? -group.Height : group.Height);
                    Interval<float> useSpace = summary.SearchForSpace(searchFrom, size, (a, b) => (a + b));

                    // Store useSpace into all items in current group:
                    group.LogicalY = useSpace;

                    // Store usage of (useSpace) into all working points:
                    intervalWorkItems.ForEachItem(pni => pni.Value.Value.Add(useSpace));

                    // Keep the summary values for next Level:
                    if (isDownward && useSpace.Begin < nextSearch)
                        nextSearch = useSpace.Begin;
                    else if (!isDownward && useSpace.End > nextSearch)
                        nextSearch = useSpace.End;

                }
                else
                {   // NonReal item (Time or Height is zero or negative):
                    group.LogicalY = new Interval<float>(searchFrom, searchFrom);
                }
                layerGroupList.Add(group);
            }

            // Current Level has use this part of layerUsing:
            if (isDownward && nextSearch < yUsed.Begin)
                yUsed.Begin = nextSearch; // RoundLogicalY(nextSearch, isDownward);
            else if (!isDownward && nextSearch > yUsed.End)
                yUsed.End = RoundLogicalY(nextSearch, isDownward);
        }
        /// <summary>
        /// Round logical Y value after recalculate one Level
        /// </summary>
        /// <param name="y"></param>
        /// <param name="isDownward"></param>
        /// <returns></returns>
        protected static float RoundLogicalY(float y, bool isDownward)
        {
            float ya = (y < 0f ? -y : y);
            if ((ya % 1f) == 0f) return (isDownward ? -ya : ya);
            return (float)(isDownward ? -Math.Ceiling((double)ya) : Math.Ceiling((double)ya));
        }
        /// <summary>
        /// List of all items, structured as List of Layers, and List of Groups of items.
        /// </summary>
        protected List<List<GTimeGraphGroup>> ItemGroupList;
        #endregion
        #region VisibleList : recalculate items to visible area on X axis y TimeAxis, prepare pixel-coordinates for GraphGroups and GraphItems
        /// <summary>
        /// Invalidate VisibleList.
        /// Call after any change on LogY or TimeAxis change.
        /// </summary>
        protected void InvalidateVisibleList()
        {
            this.IsValidVisibleList = false;
        }
        /// <summary>
        /// Check validity VisibleList.
        /// </summary>
        protected void CheckValidVisibleList()
        {
            if (this.IsValidVisibleList) return;
            this.ItemsRecalculateVisibleList();
        }
        /// <summary>
        /// true when LogY is valid
        /// </summary>
        protected bool IsValidVisibleList;
        /// <summary>
        /// Recalculate VisibleList and its properties (pixel bounds)
        /// </summary>
        protected void ItemsRecalculateVisibleList()
        {
            using (var scope = Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "GTimeGraph", "ItemsRecalculateVisibleList", ""))
            {
                int layers = 0;
                int groups = 0;
                int items = 0;

                int luh = this.LineUnitHeight + 2;

                this.ItemGroupVisibleList = new List<List<GTimeGraphGroup>>();

                ITimeConvertor timeConvertor = this._TimeConvertor;
                if (timeConvertor == null) return;

                TimeRange visibleTime = timeConvertor.VisibleTime;

                foreach (List<GTimeGraphGroup> layerList in this.ItemGroupList)
                {   // Items for one Layer:
                    List<GTimeGraphGroup> visibleItems = new List<GTimeGraphGroup>();

                    foreach (GTimeGraphGroup group in layerList)
                    {
                        if (group.IsValidRealTime && visibleTime.HasIntersect(group.Time))
                        {   // Group time is fully / partially in visible part of TimeAxis:
                            groups++;

                            int yTop = this.CalculatorYGetPixel(group.LogicalY.End);
                            int yBot = this.CalculatorYGetPixel(group.LogicalY.Begin);
                            int h = (yTop - yBot);

                            int x1 = timeConvertor.GetPixel(group.Time.Begin);
                            int x2 = timeConvertor.GetPixel(group.Time.End);
                            group.VirtualBounds = new Rectangle(x1, yTop, (x2 - x1), h);

                            foreach (ITimeGraphItem item in group.Items)
                            {
                                items++;

                                x1 = timeConvertor.GetPixel(item.Time.Begin);
                                x2 = timeConvertor.GetPixel(item.Time.End);
                                item.VirtualBounds = new Rectangle(x1, yTop, (x2 - x1), h);
                            }

                            visibleItems.Add(group);
                        }
                    }

                    if (visibleItems.Count > 0)
                    {
                        this.ItemGroupVisibleList.Add(visibleItems);
                        layers++;
                    }
                }

                this.IsValidVisibleList = true;

                scope.AddItem("Visual Layers Count: " + layers.ToString());
                scope.AddItem("Visual Groups Count: " + groups.ToString());
                scope.AddItem("Visual Items Count: " + items.ToString());
            }
        }
        /// <summary>
        /// List of currently visible items, structured as List of Layers, and List of Groups of items.
        /// List1 = layers; List2 = Group items in one layer, all levels
        /// </summary>
        protected List<List<GTimeGraphGroup>> ItemGroupVisibleList;
        #endregion
        #region Calculator LogY to PixelY
        /// <summary>
        /// Prepare value to GraphPixelHeight, prepare variables to formula for calculator Y pixel
        /// </summary>
        /// <param name="usedHeightLogY"></param>
        protected void CalculatorYPrepare(Interval<float> usedHeightLogY)
        {
            this.UsedHeightLogY = usedHeightLogY;

            float logBegin = usedHeightLogY.Begin;
            if (logBegin > 0f) logBegin = 0f;
            float logSize = usedHeightLogY.End - logBegin;
            if (logSize < 1f) logSize = 1f;

            int pixelHeight = (int)(Math.Ceiling(logSize * this.LineUnitHeight));
            Int32Range range = this.GraphHeightRange;
            if (range != null && range.IsReal)
                pixelHeight = range.Align(pixelHeight).Value;
            this._GraphPixelHeight = pixelHeight;

            this._CalculatorY_Offset = logBegin;
            this._CalculatorY_Scale = (float)pixelHeight / logSize;
        }
        /// <summary>
        /// Return Y coordinate in pixels for logical Y values.
        /// Returned values is between 0 and (GraphPixelHeight - 1), 
        /// converted from logical axis (where negative values are at bottom and positive is at top) 
        /// to Windows graphic (where positive values have Y = 0).
        /// </summary>
        /// <param name="logY"></param>
        /// <returns></returns>
        protected int CalculatorYGetPixel(float logY)
        {
            int pixelY = (int)(Math.Round(this._CalculatorY_Scale * (logY - this._CalculatorY_Offset), 0));
            if (pixelY < 0) pixelY = 0;
            return pixelY;
        }
        /// <summary>
        /// Current height of graph in pixel
        /// </summary>
        public int GraphPixelHeight
        {
            get
            {
                this.CheckValidLogicalY();
                return this._GraphPixelHeight;
            }
        }
        private int _GraphPixelHeight;
        protected Interval<float> UsedHeightLogY;
        private float _CalculatorY_Offset;
        private float _CalculatorY_Scale;
        #endregion
        
        public int GraphDefaultHeight { get; set; }
        public int LineUnitHeight { get { return this._LineUnitHeight; } set { this._LineUnitHeight = (value < 5 ? 5 : (value > 500 ? 500 : value)); } } private int _LineUnitHeight = 32;
        public Int32Range GraphHeightRange { get { return this._GraphHeightRange; } set { this._GraphHeightRange = value; } } private Int32Range _GraphHeightRange;
        

        #region Draw
        protected virtual void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawContentPrepareArgs(e, boundsAbsolute);
            using (var scope = Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "GTimeGraph", "DrawContent", ""))
            {
                this.Bounds = new Rectangle(0, 0, boundsAbsolute.Width, boundsAbsolute.Height);
                this.CheckValid();
                e.GraphicsClipWith(boundsAbsolute, GInteractiveDrawLayer.Standard);
                this.DrawTicks();
                this.DrawItems();
            }
        }
        /// <summary>
        /// Prepare this.ItemDrawArgs for subsequent Draw operations (prepare, store new Graphics and boundsAbsolute, and current _TimeConvertor)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void DrawContentPrepareArgs(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this.ItemDrawArgs == null)
                this.ItemDrawArgs = new TimeGraphItemDrawArgs(this.Host);
            this.ItemDrawArgs.Prepare(e, boundsAbsolute, this._TimeConvertor);
        }
        /// <summary>
        /// Draw all ticks
        /// </summary>
        protected void DrawTicks()
        {
            using (var scope = Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "GTimeGraph", "PaintGrid", ""))
            {
                int x;
                int x0 = this.ItemDrawArgs.GraphBoundsAbsolute.X + this.TimeAxisBegin;
                int y1 = this.ItemDrawArgs.GraphBoundsAbsolute.Top;
                int y2 = this.ItemDrawArgs.GraphBoundsAbsolute.Bottom - 1;

                foreach (VisualTick tick in this.TimeAxisTicks)
                {
                    x = x0 + tick.RelativePixel;
                    switch (tick.TickType)
                    {
                        case AxisTickType.BigLabel:
                            this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 2f, System.Drawing.Drawing2D.DashStyle.Solid);
                            break;

                        case AxisTickType.StdLabel:
                            this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 1f, System.Drawing.Drawing2D.DashStyle.Solid);
                            break;

                        case AxisTickType.BigTick:
                            this.ItemDrawArgs.DrawLine(x, y1, x, y2, Color.Gray, 1f, System.Drawing.Drawing2D.DashStyle.Dot);
                            break;
                    }
                }
            }
        }
        protected void DrawItems()
        {
            using (var scope = Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "GTimeGraph", "PaintItems", ""))
            {
                int layers = 0;
                int groups = 0;
                int items = 0;

                foreach (List<GTimeGraphGroup> layerList in this.ItemGroupVisibleList)
                {
                    layers++;
                    foreach (GTimeGraphGroup group in layerList)
                    {
                        groups++;
                        group.Draw(this.ItemDrawArgs);
                        foreach (ITimeGraphItem item in group.Items)
                        {
                            items++;
                            item.Draw(this.ItemDrawArgs);
                        }
                    }
                }

                scope.AddItem("Layers drawed: " + layers.ToString());
                scope.AddItem("Groups drawed: " + groups.ToString());
                scope.AddItem("Item drawed: " + items.ToString());
            }
        }
        protected TimeGraphItemDrawArgs ItemDrawArgs;
        #endregion
        #region ITimeGraph members
        ITimeConvertor ITimeGraph.Convertor { get { return this._TimeConvertor; } set { this._TimeConvertor = value; } }
        int ITimeGraph.UnitHeight { get { return this._LineUnitHeight; } } 
        void ITimeGraph.DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute) { this.DrawContentTimeGraph(e, boundsAbsolute); }
        #endregion
    }
    #region class GTimeGraphGroup : Group of one or more ITimeGraphItem, with summary Time and maximal Height from items
    /// <summary>
    /// GTimeGraphGroup : Group of one or more ITimeGraphItem, with summary Time and maximal Height from items
    /// </summary>
    public class GTimeGraphGroup : ITimeGraphItem
    {
        #region Constructors - from IEnumerable<ITimeGraphItem> items
        public GTimeGraphGroup()
        {
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
            this._FirstItem = null;
        }
        public GTimeGraphGroup(IEnumerable<ITimeGraphItem> items)
            : this()
        {
            this._Items = items.ToArray();
            float height = 0f;
            DateTime? begin = null;
            DateTime? end = null;
            foreach (ITimeGraphItem item in this.Items)
            {
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height > height) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
            }
            this._Height = height;
            this._Time = new TimeRange(begin, end);
            this._IsValidRealTime = ((height > 0f) && (begin.HasValue && end.HasValue && end.Value > begin.Value));
        }
        public override string ToString()
        {
            return "Time: " + this.Time.ToString() +
                "; Height: " + this.Height.ToString() +
                "; UseSpace: " + (this.LogicalY == null ? "none" : this.LogicalY.ToString());
        }
        #endregion
        #region Private members
        private int _ItemId;
        private ITimeGraphItem _FirstItem;
        private ITimeGraphItem[] _Items;
        private float _Height;
        private TimeRange _Time;
        private bool _IsValidRealTime;
        private Interval<float> _LogicalY;
        private Rectangle _VirtualBounds;
        private Rectangle _Bounds;
        #endregion
        #region Public properties, Draw()
        /// <summary>
        /// All items in this Group. Always has at least one item.
        /// </summary>
        public ITimeGraphItem[] Items { get { return this._Items; } }
        /// <summary>
        /// Count of items in Items array
        /// </summary>
        public int ItemCount { get { return this._Items.Length; } }
        /// <summary>
        /// Logical height of this item. Only postive Height is seen as Real.
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// Summary time of all items.
        /// Only positive time is seen as real (End is higher than Begin).
        /// </summary>
        public TimeRange Time { get { return this._Time; } }
        /// <summary>
        /// true when this is real item: has positive Height and its Time.End is higher (not equal!) to Time.Begin
        /// </summary>
        internal bool IsValidRealTime { get { return this._IsValidRealTime; } }
        /// <summary>
        /// Allocated logical space on the Y axis (not pixels). Value of 1 is standard logical unit of height.
        /// </summary>
        public Interval<float> LogicalY
        {
            get { return this._LogicalY; }
            set
            {
                this._LogicalY = value.ValueClone;
                this.Items.ForEachItem(i => i.LogicalY = this._LogicalY);
            }
        }
        /// <summary>
        /// Virtual bounds in pixels, where X axis is same as Bounds, but Y axis is reverted (Virtual Y has 0 at bottom, in contrast to WinForm Y which has 0 at top)
        /// </summary>
        public Rectangle VirtualBounds { get { return this._VirtualBounds; } set { this._VirtualBounds = value; } }
        /// <summary>
        /// Relative bounds in pixels, in standard bounds coordinates as WinForm control
        /// </summary>
        public Rectangle Bounds { get { return this._Bounds; } set { this._Bounds = value; } }
        /// <summary>
        /// Draw this group
        /// </summary>
        /// <param name="drawArgs">All data and support for drawing</param>
        public void Draw(TimeGraphItemDrawArgs drawArgs)
        {
            if (!this.IsValidRealTime || this._FirstItem.Layer < 0 || this.ItemCount <= 1) return;
            drawArgs.FillRectangle(this.VirtualBounds, Color.FromArgb(160, Color.Gray), -1, -2, -1, -1);
        }
        /// <summary>
        /// Compare two instance by Order ASC, Time.Begin ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int ItemsRecalculateLogicalYCompare(GTimeGraphGroup a, GTimeGraphGroup b)
        {
            int cmp = a._FirstItem.Order.CompareTo(b._FirstItem.Order);
            if (cmp == 0)
                cmp = TimeRange.CompareByBeginAsc(a.Time, b.Time);
            return cmp;
        }
        #endregion
        #region explicit ITimeGraphItem members
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.Layer { get { return this._FirstItem.Layer; } }
        int ITimeGraphItem.Level { get { return this._FirstItem.Level; } }
        int ITimeGraphItem.Order { get { return this._FirstItem.Order; } }
        int ITimeGraphItem.GroupId { get { return this._FirstItem.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this.Time; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        Interval<float> ITimeGraphItem.LogicalY { get { return this.LogicalY; } set { this.LogicalY = value; } }
        Rectangle ITimeGraphItem.VirtualBounds { get { return this.VirtualBounds; } set { this.VirtualBounds = value; } }
        Rectangle ITimeGraphItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        void ITimeGraphItem.Draw(TimeGraphItemDrawArgs drawArgs) { this.Draw(drawArgs); }
        #endregion
    }
    #endregion
    public class GTimeGraphItem : ITimeGraphItem
    {
        #region Public members
        public GTimeGraphItem()
        {
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
        }
        /// <summary>
        /// ID of this item
        /// </summary>
        public Int32 ItemId { get { return this._ItemId; } } private Int32 _ItemId;
        /// <summary>
        /// Visual layer.
        /// Items are drawed from lowest layer to highest.
        /// Items on different layers can be drawed one over another, items on same layer is drawed on different Y coordinate.
        /// </summary>
        public Int32 Layer { get; set; }
        /// <summary>
        /// Visual level.
        /// Items are positioned to visual level from bottom (logical Y = 0) up.
        /// Items of level 1 began at topmost coordinate of items from level 0, and so on.
        /// </summary>
        public Int32 Level { get; set; }
        /// <summary>
        /// Order of item. Items in same Order are stored to graph on order their Time.Begin, item with higher Order are stored after store all items with lower Order.
        /// </summary>
        public Int32 Order { get; set; }
        /// <summary>
        /// Group of items for one logical unit (has items in more rows, or more items in one row).
        /// Items from one group in same row has same Y coordinate.
        /// Items from one group in another rows is "fixed" together.
        /// </summary>
        public Int32 GroupId { get; set; }
        /// <summary>
        /// Time of this item
        /// </summary>
        public virtual TimeRange Time { get; set; }
        /// <summary>
        /// Height of item, where value 1.0 = ITimeGraph.UnitHeight
        /// </summary>
        public float Height { get; set; }
        
        public Color? BackColor { get; set; }
        public Color? BorderColor { get; set; }
        public Color? TextColor { get; set; }
        public string[] Captions { get; set; }
        public string ToolTip { get; set; }
        #endregion
        #region Protected members - VirtualBounds, LogicalY, Draw()
        /// <summary>
        /// Virtual bounds in pixels, where X axis is same as Bounds, but Y axis is reverted (Virtual Y has 0 at bottom, in contrast to WinForm Y which has 0 at top)
        /// </summary>
        protected Rectangle VirtualBounds { get; set; }
        /// <summary>
        /// Relative bounds in pixels, in standard bounds coordinates as WinForm control
        /// </summary>
        protected Rectangle Bounds { get; set; }
        /// <summary>
        /// Logical coordinates on Y axis in Graph
        /// </summary>
        protected virtual Interval<float> LogicalY { get; set; }
        /// <summary>
        /// true if this item has positive Height and Time
        /// </summary>
        protected bool IsValidRealTime { get { return (this.Time != null && this.Time.IsFilled && this.Time.IsReal); } }
        /// <summary>
        /// Draw this item
        /// </summary>
        protected virtual void Draw(TimeGraphItemDrawArgs drawArgs)
        {
            if (!this.IsValidRealTime) return;
            int l = ((this.Layer >= 0) ? -1 : 0);
            drawArgs.FillRectangle(this.VirtualBounds, this.BackColor, 0, l, 0, 0);
            drawArgs.BorderRectangle(this.VirtualBounds, this.BorderColor, 0, l, 0, 0);
        }
        #endregion
        #region explicit ITimeGraphItem members
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.Layer { get { return this.Layer; } }
        int ITimeGraphItem.Level { get { return this.Level; } }
        int ITimeGraphItem.Order { get { return this.Order; } }
        int ITimeGraphItem.GroupId { get { return this.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this.Time; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        Interval<float> ITimeGraphItem.LogicalY { get { return this.LogicalY; } set { this.LogicalY = value; } }
        Rectangle ITimeGraphItem.VirtualBounds { get { return this.VirtualBounds; } set { this.VirtualBounds = value; } }
        Rectangle ITimeGraphItem.Bounds { get { return this.Bounds; } set { this.Bounds = value; } }
        void ITimeGraphItem.Draw(TimeGraphItemDrawArgs drawArgs) { this.Draw(drawArgs); }
        #endregion
    }
    public class GTestGraph : InteractiveContainer, ITimeGraph
    {

        public void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            double tx = (double)boundsAbsolute.Width;
            double ty = (double)boundsAbsolute.Height;
            int bx = boundsAbsolute.X;
            int ex = boundsAbsolute.Right - 1;
            int by = boundsAbsolute.Y;
            int ey = boundsAbsolute.Bottom - 1;

            Point prev = boundsAbsolute.Center();
            Random rand = Rand;
            int count = rand.Next(50, 350);
            double q1 = 2.25d;
            double q2 = 1.05d;
            
            double cx = q1 * (rand.NextDouble() - 0.5d);
            double cx1 = -10d;
            double cx2 = 10d;
            double cy = q1 * (rand.NextDouble() - 0.5d);
            double cy1 = -5d;
            double cy2 = 5d;
            using (GPainter.GraphicsUseSmooth(e.Graphics))
            {
                for (int i = 0; i < count; i++)
                {
                    double dx = q2 * (rand.NextDouble() - 0.5d);
                    double dy = q2 * (rand.NextDouble() - 0.5d);

                    cx += dx;
                    cy += dy;
                    cx = (cx < cx1 ? cx1 : (cx > cx2 ? cx2 : cx));
                    cy = (cy < cy1 ? cy1 : (cy > cy2 ? cy2 : cy));

                    int sx = (int)(prev.X + cx);
                    int sy = (int)(prev.Y + cy);
                    _Align(ref sx, bx, ex, ref cx);
                    _Align(ref sy, by, ey, ref cy);

                    Point next = new Point(sx, sy);
                    e.Graphics.DrawLine(Pens.Black, prev, next);
                    prev = next;
                }
            }
        }
        private static Random Rand = new Random();

        private void _Align(ref int sx, int bx, int ex, ref double cx)
        {
            if (sx < bx)
            {
                sx = bx;
                cx = -cx;
            }
            else if (sx > ex)
            {
                sx = ex;
                cx = -cx;
            }
        }

        #region ITimeGraph members
        ITimeConvertor ITimeGraph.Convertor { get { return this._Convertor; } set { this._Convertor = value; } } private ITimeConvertor _Convertor;
        int ITimeGraph.UnitHeight { get { return this._UnitHeight; } } private int _UnitHeight;
        void ITimeGraph.DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute) { this.DrawContentTimeGraph(e, boundsAbsolute); }
        #endregion
    }
    #region Interface ITimeGraph, ITimeGraphItem, ITimeConvertor; class TimeGraphItemDrawArgs for arguments
    public interface ITimeInteractiveGraph : ITimeGraph, IInteractiveItem
    { }
    public interface ITimeGraph
    {
        /// <summary>
        /// Host control store reference to ITimeConvertor into this property.
        /// </summary>
        ITimeConvertor Convertor { get; set; }
        /// <summary>
        /// Height (in pixels) for one unit of GTimeItem.Height
        /// </summary>
        int UnitHeight { get; }
        /// <summary>
        /// Draw content of graph
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        void DrawContentTimeGraph(GInteractiveDrawArgs e, Rectangle boundsAbsolute);

    }
    public interface ITimeGraphItem
    {
        /// <summary>
        /// Unique ID of item
        /// </summary>
        Int32 ItemId { get; }
        /// <summary>
        /// Layer: visual layer. Layers are drawed one over another, from lowest Layer to highest.
        /// Usually: negative layers are "background" = non-editable.
        /// </summary>
        Int32 Layer { get; }
        /// <summary>
        /// Level of item. Items in same Level are drawed in one band, item of higher level are drawed into new band on higher Y coordinate (lower Top value).
        /// </summary>
        Int32 Level { get; }
        /// <summary>
        /// Order of item. Items in same Order are stored to graph on order their Time.Begin, item with higher Order are stored after store all items with lower Order.
        /// </summary>
        Int32 Order { get; }
        /// <summary>
        /// Group of items. Items in same Group are drawed in same line (same Y coordinate) and are not mixed with items of other Group.
        /// </summary>
        Int32 GroupId { get; }
        /// <summary>
        /// Time of his item
        /// </summary>
        TimeRange Time { get; }
        /// <summary>
        /// Logical height of this item. Default = 1.Of.
        /// Height == 0 or negative is not allowed.
        /// </summary>
        float Height { get; }
        /// <summary>
        /// Logical coordinates on Y axis in Graph
        /// </summary>
        Interval<float> LogicalY { get; set; }
        /// <summary>
        /// Virtual bounds in pixels, where X axis is same as Bounds, but Y axis is reverted (Virtual Y has 0 at bottom, in contrast to WinForm Y which has 0 at top)
        /// </summary>
        Rectangle VirtualBounds { get; set; }
        /// <summary>
        /// Relative bounds in pixels, in standard bounds coordinates as WinForm control
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Draw this item
        /// </summary>
        /// <param name="drawArgs">All data and support for drawing</param>
        void Draw(TimeGraphItemDrawArgs drawArgs);
    }
    public interface ITimeConvertor
    {
        string Identity { get; }
        /// <summary>
        /// Currently visible TimeRange
        /// </summary>
        TimeRange VisibleTime { get; }
        /// <summary>
        /// All current visual ticks
        /// </summary>
        VisualTick[] Ticks { get; }
        int GetPixel(DateTime? time);
    }
    public class TimeGraphItemDrawArgs : IDisposable
    {
        #region Constructor, private variables
        public TimeGraphItemDrawArgs(GInteractiveControl host)
        {
            this._Host = host;
        }
        internal void Prepare(GInteractiveDrawArgs drawArgs, Rectangle graphBoundsAbsolute, ITimeConvertor timeConvertor)
        {
            this._DrawArgs = drawArgs;
            this._GraphBoundsAbsolute = graphBoundsAbsolute;
            this._TimeConvertor = timeConvertor;
        }
        private GInteractiveControl _Host;
        private GInteractiveDrawArgs _DrawArgs;
        private Rectangle _GraphBoundsAbsolute;
        private ITimeConvertor _TimeConvertor;
        void IDisposable.Dispose()
        {
            this._DrawSupportDispose();
        }
        #endregion
        #region Public properties
        /// <summary>
        /// An Graphics object to draw on
        /// </summary>
        public Graphics Graphics { get { return this._DrawArgs.Graphics; } }
        /// <summary>
        /// Layer, currently drawed.
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get { return this._DrawArgs.DrawLayer; } }
        /// <summary>
        /// Whole GInteractiveDrawArgs
        /// </summary>
        public GInteractiveDrawArgs InteractiveDrawArgs { get { return this._DrawArgs; } }
        /// <summary>
        /// Absolute bounds of Graph
        /// </summary>
        public Rectangle GraphBoundsAbsolute { get { return this._GraphBoundsAbsolute; } }
        /// <summary>
        /// Time convertor for X axis
        /// </summary>
        public ITimeConvertor TimeConvertor { get { return this._TimeConvertor; } }
        #endregion
        #region Draw support
        /// <summary>
        /// Common SolidBrush object
        /// </summary>
        public SolidBrush SolidBrush { get { return this._Host.SolidBrush; } }
        /// <summary>
        /// Common Pen object
        /// </summary>
        public Pen Pen { get { return this._Host.Pen; } }
        /// <summary>
        /// Default color for fill rectangle
        /// </summary>
        public Color DefaultBackColor { get { return this._Host.DefaultBackColor; } set { this._Host.DefaultBackColor = value; } }
        /// <summary>
        /// Default color for border rectangle
        /// </summary>
        public Color DefaultBorderColor { get { return this._Host.DefaultBorderColor; } set { this._Host.DefaultBorderColor = value; } }
        /// <summary>
        /// Fill rectangle (convert VirtualBounds to real bounds), with color (or DefaultBackColor).
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="backColor"></param>
        public void FillRectangle(Rectangle virtualBounds, Color? backColor)
        {
            this._FillRectangle(virtualBounds, backColor, false, 0, 0, 0, 0);
        }
        /// <summary>
        /// Fill rectangle (convert VirtualBounds to real bounds), with color (or DefaultBackColor).
        /// Real bounds are enlarged (Rectangle.Enlarge()) by specified values for each edge: positive value produce greater bounds, negative value smaller bounds.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="backColor"></param>
        /// <param name="enlargeL"></param>
        /// <param name="enlargeT"></param>
        /// <param name="enlargeR"></param>
        /// <param name="enlargeB"></param>
        public void FillRectangle(Rectangle virtualBounds, Color? backColor, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            this._FillRectangle(virtualBounds, backColor, true, enlargeL, enlargeT, enlargeR, enlargeB);
        }
        private void _FillRectangle(Rectangle virtualBounds, Color? backColor, bool enlarge, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            Rectangle bounds = this.GetBounds(virtualBounds);
            if (enlarge)
                bounds = bounds.Enlarge(enlargeL, enlargeT, enlargeR, enlargeB);
            if (this._IsBoundsVisible(bounds))
                this._Host.FillRectangle(this.Graphics, bounds, (backColor.HasValue ? backColor.Value : this.DefaultBackColor));
        }
        /// <summary>
        /// Draw Border around Virtual bounds, with specified color (or DefaultBorderColor).
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="borderColor"></param>
        public void BorderRectangle(Rectangle virtualBounds, Color? borderColor)
        {
            this._BorderRectangle(virtualBounds, borderColor, false, 0, 0, 0, 0);
        }
        /// <summary>
        /// Draw Border around Virtual bounds, with specified color (or DefaultBorderColor).
        /// Real bounds are enlarged (Rectangle.Enlarge()) by specified values for each edge: positive value produce greater bounds, negative value smaller bounds.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="borderColor"></param>
        /// <param name="enlargeL"></param>
        /// <param name="enlargeT"></param>
        /// <param name="enlargeR"></param>
        /// <param name="enlargeB"></param>
        public void BorderRectangle(Rectangle virtualBounds, Color? borderColor, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            this._BorderRectangle(virtualBounds, borderColor, true, enlargeL, enlargeT, enlargeR, enlargeB);
        }
        private void _BorderRectangle(Rectangle virtualBounds, Color? borderColor, bool enlarge, int enlargeL, int enlargeT, int enlargeR, int enlargeB)
        {
            Rectangle bounds = this.GetBounds(virtualBounds);
            bounds = bounds.Enlarge(enlargeL, enlargeT, enlargeR - 1, enlargeB - 1);     // Shring Width and Height by 1 pixel is standard for draw Border into (!) area.
            if (this._IsBoundsVisible(bounds))
            {
                this._ResetPen(borderColor);
                this.Graphics.DrawRectangle(this.Pen, bounds);
            }
        }
        /// <summary>
        /// Return absolute WinForm bounds for specified Virtual Bounds.
        /// Returned bounds can be outside of visible bounds of Graph.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public Rectangle GetBounds(Rectangle virtualBounds)
        {
            int graphB = this._GraphBoundsAbsolute.Bottom - 4;
            int graphX = this._GraphBoundsAbsolute.X;
            return new Rectangle(graphX + virtualBounds.X, graphB - virtualBounds.Y, virtualBounds.Width, virtualBounds.Height);
        }
        /// <summary>
        /// Return true when specified item Virtual bounds is (whole or partially) visible in current Graph (in GraphBoundsAbsolute).
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool IsVirtualBoundsVisible(Rectangle virtualBounds)
        {
            Rectangle bounds = this._GetBounds(virtualBounds);
            return this._IsBoundsVisible(bounds);
        }
        /// <summary>
        /// Return true when specified item Absolute bounds is (whole or partially) visible in current Graph (in GraphBoundsAbsolute).
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool IsBoundsVisible(Rectangle bounds)
        {
            return this._IsBoundsVisible(bounds);
        }
        /// <summary>
        /// Return absolute WinForm bounds for specified Virtual Bounds.
        /// Returned bounds can be outside of visible bounds of Graph.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        private Rectangle _GetBounds(Rectangle virtualBounds)
        {
            int graphB = this._GraphBoundsAbsolute.Bottom - 4;
            int graphX = this._GraphBoundsAbsolute.X;
            return new Rectangle(graphX + virtualBounds.X, graphB - virtualBounds.Y, virtualBounds.Width, virtualBounds.Height);
        }
        /// <summary>
        /// Return true when specified item absolute bounds is (whole or partially) visible in current Graph (in GraphBoundsAbsolute).
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private bool _IsBoundsVisible(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            Rectangle graphBounds = this.GraphBoundsAbsolute;
            return !(
                    bounds.Right <= graphBounds.Left ||
                    bounds.Bottom <= graphBounds.Top ||
                    bounds.Left >= graphBounds.Right ||
                    bounds.Top >= graphBounds.Bottom);
        }
        /// <summary>
        /// Draw one line using standard Pen, 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="dashStyle"></param>
        public void DrawLine(int x1, int y1, int x2, int y2, Color color, float width, System.Drawing.Drawing2D.DashStyle dashStyle)
        {
            Pen pen = this.Pen;
            pen.Width = width;
            pen.Color = color;
            pen.DashStyle = dashStyle;
            this.Graphics.DrawLine(pen, x1, y1, x2, y2);
        }
        private void _ResetPen(Color? color)
        {
            this.Pen.Color = (color.HasValue ? color.Value : this.DefaultBorderColor);
            if (this.Pen.Width != 1f) this.Pen.Width = 1f;
            if (this.Pen.DashStyle != System.Drawing.Drawing2D.DashStyle.Solid) this.Pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
        }
        private SolidBrush _SolidBrush;
        private Pen _Pen;
        private void _DrawSupportDispose()
        {
            if (this._SolidBrush != null)
                this._SolidBrush.Dispose();
            this._SolidBrush = null;
        }
        #endregion
    }
    #endregion
}
