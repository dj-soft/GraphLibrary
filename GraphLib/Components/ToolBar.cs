using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Localizable;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class Toolbar
    public class GToolBar : InteractiveContainer
    {
        #region Vytvoření Toolbaru bez načítání obsahu, public prvky
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GToolBar(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GToolBar()
        {
            this.InitToolbar();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void InitToolbar()
        {
            this._TitleFont = FontInfo.CaptionSmallBold;
            this._ItemFont = FontInfo.Menu;
            this._ToolbarItemList = new List<GToolbarGroup>();
            this._CreateSettings();
            this._CreateSplitter();
        }
        /// <summary>
        /// Font pro titulek na Toolbaru
        /// </summary>
        public FontInfo ToolbarTitleFont { get { return (this._TitleFont != null ? this._TitleFont.Clone : null); } set { this._TitleFont = (value != null ? value.Clone : null); } } private FontInfo _TitleFont;
        /// <summary>
        /// Default Font pro prvky na Toolbaru
        /// </summary>
        public FontInfo ToolbarDefaultItemFont { get { return (this._ItemFont != null ? this._ItemFont.Clone : null); } set { this._ItemFont = (value != null ? value.Clone : null); } } private FontInfo _ItemFont;
        /// <summary>
        /// Souřadnice Toolbaru v rámci jeho Parenta.
        /// </summary>
        public override Rectangle Bounds
        {
            get
            {
                return base.Bounds;
            }
            set
            {
                int x = value.X;
                int y = value.Y;
                int width = value.Width;
                int height = this.TBarSetting.ToolbarBounds.Height;
                Rectangle bounds = new Rectangle(x, y, width, height);
                base.Bounds = bounds;
                this._SplitterSetPosition(bounds, true);
            }
        }
        #endregion
        #region ToolbarSettings for ComponentSize
        /// <summary>
        /// Setting for layout on current toolbar (by ToolbarSize)
        /// </summary>
        internal LayoutSettingTBarInfo TBarSetting
        {
            get
            {
                ComponentSize size = this._ToolbarSize;
                LayoutSettingTBarInfo info;
                if (this._SettingDict.TryGetValue(size, out info)) return info;
                return this._SettingDict[ComponentSize.Medium];
            }
        }
        private void _CreateSettings()
        {
            this._SettingDict = new Dictionary<ComponentSize, LayoutSettingTBarInfo>();
            this._SettingDict.Add(ComponentSize.Small, new LayoutSettingTBarInfo(ComponentSize.Small));
            this._SettingDict.Add(ComponentSize.Medium, new LayoutSettingTBarInfo(ComponentSize.Medium));
            this._SettingDict.Add(ComponentSize.Large, new LayoutSettingTBarInfo(ComponentSize.Large));
        }
        private Dictionary<ComponentSize, LayoutSettingTBarInfo> _SettingDict;
        #endregion
        #region Splitter
        private void _CreateSplitter()
        {
            this._Splitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal };
            int heightMin = this._SettingDict[ComponentSize.Small].ToolbarBounds.Height;
            int heightMax = this._SettingDict[ComponentSize.Large].ToolbarBounds.Height;
            this._Splitter.ValueRange = new Int32NRange(heightMin, heightMax);
            this._Splitter.SplitterVisibleWidth = 2;
            this._Splitter.SplitterActiveOverlap = 2;
            this._Splitter.ValueChanging += new GPropertyChangedHandler<int>(_Splitter_ValueChanging);
            this._Splitter.ValueChanged += new GPropertyChangedHandler<int>(_Splitter_ValueChanged);
        }
        void _Splitter_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            int height = this._Splitter.Value;
            int hs = this._SettingDict[ComponentSize.Small].ToolbarBounds.Height;
            int hm = this._SettingDict[ComponentSize.Medium].ToolbarBounds.Height;
            int hl = this._SettingDict[ComponentSize.Large].ToolbarBounds.Height;
            int h1 = (hs + hm) / 2;
            int h2 = (hm + hl) / 2;

            this.ToolbarSize = (height < h1 ? ComponentSize.Small :
                               (height < h2 ? ComponentSize.Medium : ComponentSize.Large));
            this.RecalculateBounds(false);
            this.Repaint();
        }
        void _Splitter_ValueChanged(object sender, GPropertyChangeArgs<int> e)
        {
            this.RecalculateBounds(true);
            this.Repaint();
        }
        protected void RecalculateBounds(bool withSplitterValue)
        {
            Rectangle bounds = this.Bounds;
            bounds.Height = this.TBarSetting.ToolbarBounds.Height;
            base.Bounds = bounds;
            this._SplitterSetPosition(bounds, withSplitterValue);

            this.InvalidateLayout();
        }
        private void _SplitterCheckValue()
        {
            int splitterValue = this.Bounds.Bottom - 1;
            if (this._Splitter.ValueSilent != splitterValue)
                this._Splitter.ValueSilent = splitterValue;
        }
        private void _SplitterSetPosition(Rectangle bounds, bool withSplitterValue)
        {
            this._Splitter.BoundsNonActive = new Int32NRange(bounds.Left, bounds.Right);
            if (withSplitterValue)
                this._SplitterCheckValue();
        }
        private GSplitter _Splitter;
        #endregion
        #region Načtení obsahu Toolbaru z dostupných Services (IFunctionGlobal)
        /// <summary>
        /// Zajistí načtení obsahu Toolbaru z dostupných Services typu IFunctionGlobal
        /// </summary>
        public void FillFunctionGlobals()
        {
            this._FillFunctionGlobals(typeof(IFunctionGlobal));
        }
        /// <summary>
        /// Zajistí načtení obsahu Toolbaru z dostupných Services daného typu
        /// </summary>
        /// <param name="providerType"></param>
        public void FillFunctionGlobals(Type providerType)
        {
            this._FillFunctionGlobals(providerType);
        }
        private void _FillFunctionGlobals(Type providerType)
        {
            this._ToolbarItemList.Clear();

            IEnumerable<object> plugins = Application.App.GetPlugins(providerType);
            List<FunctionGlobalGroup> groupList = new List<FunctionGlobalGroup>();

            // Read GUI from services:
            foreach (object plugin in plugins)
            {
                IFunctionGlobal function = plugin as IFunctionGlobal;
                if (function != null)
                    this._ReadGuiFromService(function, groupList);
            }

            // Check all GUI with all services:
            foreach (object plugin in plugins)
            {
                IFunctionGlobal function = plugin as IFunctionGlobal;
                if (function != null)
                    this._CheckGuiWithService(function, groupList);
            }

            // Arrange groups (sort):
            groupList.Sort(FunctionGlobalGroup.SortByOrder);

            // Create GUI items from groups:
            this._AddGuiGroups(groupList);
        }
        /// <summary>
        /// Načte skupiny GUI z objektu function, implementujícího IFunctionGlobal
        /// </summary>
        /// <param name="function"></param>
        /// <param name="groupList"></param>
        private void _ReadGuiFromService(Services.IFunctionGlobal function, List<FunctionGlobalGroup> groupList)
        {
            try
            {
                Services.FunctionGlobalPrepareGuiRequest request = new Services.FunctionGlobalPrepareGuiRequest(this);
                Services.FunctionGlobalPrepareResponse response = function.PrepareGui(request);
                if (response != null && response.Items != null && response.Items.Length > 0)
                    groupList.AddRange(response.Items);
            }
            catch (Exception exc)
            {
                App.Trace.Exception(exc, "IFunctionGlobal.PrepareGui() error", "Type: " + function.GetType().NsName());
            }
        }
        /// <summary>
        /// Check all GUI with one service
        /// </summary>
        /// <param name="function"></param>
        /// <param name="groupList"></param>
        private void _CheckGuiWithService(IFunctionGlobal function, List<FunctionGlobalGroup> groupList)
        {
            try
            {
                FunctionGlobalCheckGuiRequest request = new FunctionGlobalCheckGuiRequest(groupList.ToArray());
                function.CheckGui(request);
            }
            catch (Exception exc)
            {
                App.Trace.Exception(exc, "IFunctionGlobal.CheckGui() error", "Type: " + function.GetType().NsName());
            }
        }
        /// <summary>
        /// Create GUI items (for Groups) and add it into this Items
        /// </summary>
        /// <param name="groupList"></param>
        private void _AddGuiGroups(List<FunctionGlobalGroup> groupList)
        {
            foreach (FunctionGlobalGroup group in groupList)
                this._AddGToolbarGroup(GToolbarGroup.CreateFrom(this, group));
        }
        /// <summary>
        /// Add one GToolbarGroup into Childs and into _ToolbarItemList.
        /// </summary>
        /// <param name="group"></param>
        private void _AddGToolbarGroup(GToolbarGroup group)
        {
            this.AddItem(group);
            this._ToolbarItemList.Add(group);
        }
        private List<GToolbarGroup> _ToolbarItemList;
        #endregion
        #region Layout of toolbar - Check, Invalidate, Prepare
        protected void CheckLayout(Graphics graphics)
        {
            bool isValidSize = (this._LayoutSize == this.TBarSetting.ToolbarSize);
            if (isValidSize && (graphics == null && this._LayoutValidDefault)) return;
            if (isValidSize && (graphics != null && this._LayoutValidGraphics)) return;
            this.PrepareLayout(graphics);
        }
        protected void InvalidateLayout()
        {
            this._LayoutValidDefault = false;
            this._LayoutValidGraphics = false;
        }
        protected void PrepareLayout(Graphics graphics)
        {
            if (graphics != null)
            {
                this.PrepareLayoutRun(graphics);
                this._LayoutValidDefault = true;
                this._LayoutValidGraphics = true;
            }
            else
            {
                using (Image img = new Bitmap(1024, 96))
                using (Graphics grp = Graphics.FromImage(img))
                {
                    this.PrepareLayoutRun(grp);
                }
                this._LayoutValidDefault = true;
            }
            this._LayoutSize = this.TBarSetting.ToolbarSize;
        }
        private ComponentSize _LayoutSize;
        private bool _LayoutValidDefault;
        private bool _LayoutValidGraphics;
        protected void PrepareLayoutRun(Graphics graphics)
        {
            int x = this.TBarSetting.ContentBounds.X;                // Prepare this.TBarSetting for current ToolbarSize, when current item is null or has not valid Size
            foreach (GToolbarGroup group in this._ToolbarItemList)
                group.PrepareLayout(graphics, ref x);
        }
        #endregion
        #region ToolbarSize
        /// <summary>
        /// Size of toolbar.
        /// Small: items has height 24/48 (Small/Large);
        /// Medium: items has height 32/64 (Small/Large) (default);
        /// Large: items has height 48/96 (Small/Large);
        /// </summary>
        public ComponentSize ToolbarSize 
        {
            get { return this._ToolbarSize; }
            set { this.SetToolbarSize(value, ProcessAction.All, EventSourceType.ValueChange | EventSourceType.ApplicationCode); ; }
        }
        protected virtual void SetToolbarSize(ComponentSize toolbarSize, ProcessAction actions, EventSourceType eventSource)
        {
            ComponentSize sizeNew = toolbarSize;
            ComponentSize sizeOld = this._ToolbarSize;
            if (sizeNew == sizeOld) return;

            this._ToolbarSize = sizeNew;
            
            if (IsAction(actions, ProcessAction.PrepareInnerItems))
                this.RecalculateBounds(true);

            if (IsAction(actions, ProcessAction.CallChangedEvents))
                this.CallToolbarSizeChanged(sizeOld, sizeNew, eventSource);
        }
        private ComponentSize _ToolbarSize = ComponentSize.Medium;
        #endregion
        #region Public events and virtual methods
        /// <summary>
        /// Call method OnValueChanging() and event ValueChanging
        /// </summary>
        protected void CallToolbarSizeChanged(ComponentSize oldValue, ComponentSize newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<ComponentSize> args = new GPropertyChangeArgs<ComponentSize>(oldValue, newValue, eventSource);
            this.OnToolbarSizeChanged(args);
            if (!this.IsSuppressedEvent && this.ToolbarSizeChanged != null)
                this.ToolbarSizeChanged(this, args);
        }
        /// <summary>
        /// Occured during interactive changing Value value
        /// </summary>
        protected virtual void OnToolbarSizeChanged(GPropertyChangeArgs<ComponentSize> args) { }
        /// <summary>
        /// Event on this.Value interactive changing
        /// </summary>
        public event GPropertyChangedHandler<ComponentSize> ToolbarSizeChanged;

        #endregion
        #region Layout of Toolbar - classes
        /// <summary>
        /// Layouts for whole Toolbar
        /// </summary>
        public class LayoutSettingTBarInfo
        {
            public LayoutSettingTBarInfo(ComponentSize toolbarSize)
            {
                this.ToolbarSize = toolbarSize;
                this.ItemDict = new Dictionary<FunctionGlobalItemSize, LayoutSettingTItemInfo>();
                switch (toolbarSize)
                {
                    case ComponentSize.Small:
                        this.HeightModule = 6;
                        this.PixelPerModule = 10;
                        this.ContentBounds = new Rectangle(2, 1, 1000, 60);
                        this.TitleBounds = new Rectangle(0, 64, 1000, 16);
                        this.ToolbarBounds = new Rectangle(0, 0, 1000, 80);
                        this.TitleZoom = 0.80f;
                        this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1, 10, 1, 8, 0, 0f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2, 20, 1, 18, 18, 0.50f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Half, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half, 3, 30, 1, 28, 20, 0.75f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4, 40, 1, 38, 22, 0.85f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 60, 1, 38, 22, 0.95f));
                        break;
                    case ComponentSize.Large:
                        this.HeightModule = 6;
                        this.PixelPerModule = 20;
                        this.ContentBounds = new Rectangle(1, 1, 1000, 120);
                        this.TitleBounds = new Rectangle(0, 124, 1000, 24);
                        this.ToolbarBounds = new Rectangle(0, 0, 1000, 148);
                        this.TitleZoom = 1.05f;
                        this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1, 20, 2, 16, 0, 0f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2, 40, 2, 36, 22, 0.95f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Half, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half, 3, 60, 2, 56, 24, 1.05f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4, 80, 3, 72, 26, 1.10f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 120, 3, 90, 26, 1.15f));
                        break;
                    case ComponentSize.Medium:
                    default:
                        this.HeightModule = 6;
                        this.PixelPerModule = 15;
                        this.ContentBounds = new Rectangle(1, 1, 1000, 90);
                        this.TitleBounds = new Rectangle(0, 94, 1000, 20);
                        this.ToolbarBounds = new Rectangle(0, 0, 1000, 114);
                        this.TitleZoom = 1.00f;
                        this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1, 15, 1, 12, 0, 0f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2, 30, 1, 24, 24, 0.85f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Half, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half, 3, 45, 1, 42, 24, 0.95f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4, 60, 2, 56, 26, 1.00f));
                        this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 90, 2, 64, 26, 1.10f));
                        break;
                }
            }
            private Dictionary<FunctionGlobalItemSize, LayoutSettingTItemInfo> ItemDict;
            public ComponentSize ToolbarSize { get; private set; }
            /// <summary>
            /// Height in modules = number of module units in toolbar
            /// </summary>
            public int HeightModule { get; private set; }
            /// <summary>
            /// Number of pixel per one module
            /// </summary>
            public int PixelPerModule { get; private set; }
            public Rectangle ToolbarBounds { get; private set; }
            public Rectangle ContentBounds { get; private set; }
            public Rectangle TitleBounds { get; private set; }
            public float TitleZoom { get; private set; }
            public FontInfo TitleFont { get { return FontInfo.CaptionSmall.GetZoom(this.TitleZoom); } }
            public LayoutSettingTItemInfo ItemMicroInfo { get { return this.ItemDict[FunctionGlobalItemSize.Micro]; } }
            public LayoutSettingTItemInfo ItemSmallInfo { get { return this.ItemDict[FunctionGlobalItemSize.Small]; } }
            public LayoutSettingTItemInfo ItemHalfInfo { get { return this.ItemDict[FunctionGlobalItemSize.Half]; } }
            public LayoutSettingTItemInfo ItemLargeInfo { get { return this.ItemDict[FunctionGlobalItemSize.Large]; } }
            public LayoutSettingTItemInfo ItemWholeInfo { get { return this.ItemDict[FunctionGlobalItemSize.Whole]; } }

            /// <summary>
            /// Returns LayoutSettingTItemInfo for item of specified Size, on current toolbar
            /// </summary>
            /// <param name="itemSize"></param>
            /// <returns></returns>
            public LayoutSettingTItemInfo GetInfoForSize(FunctionGlobalItemSize itemSize)
            {
                return this.ItemDict[itemSize];
            }
            /// <summary>
            /// Returns Size for Image of specified itemSize
            /// </summary>
            /// <param name="itemSize"></param>
            /// <returns></returns>
            public Size GetIconSize(FunctionGlobalItemSize itemSize)
            {
                LayoutSettingTItemInfo info = this.ItemDict[itemSize];
                return info.ImageSize;
            }
            /// <summary>
            /// Returns FontInfo for item in size
            /// </summary>
            /// <param name="itemSize"></param>
            /// <param name="fontInfo"></param>
            /// <returns></returns>
            public FontInfo GetFontInfo(FunctionGlobalItemSize itemSize, FontInfo fontInfo)
            {
                FontInfo fontCurr = (fontInfo != null ? fontInfo : FontInfo.Menu).Clone;
                float zoom = this.ItemDict[itemSize].TextZoom;
                fontCurr.ApplyZoom(zoom);
                return fontCurr;
            }
            /// <summary>
            /// Returns number of modules for specified pixel size
            /// </summary>
            /// <param name="pixels"></param>
            /// <returns></returns>
            public int GetModuleCount(int pixels)
            {
                if (pixels <= 0) return 0;
                decimal modules = Math.Ceiling((decimal)pixels / (decimal)this.PixelPerModule);
                return (int)modules;
            }
            /// <summary>
            /// Returns size in pixels from size in modules
            /// </summary>
            /// <param name="size"></param>
            /// <returns></returns>
            public Size GetPixelSize(Size size)
            {
                return new Size(PixelPerModule * size.Width, PixelPerModule * size.Height);
            }
        }
        /// <summary>
        /// Layouts for one Toolbar Item
        /// </summary>
        public class LayoutSettingTItemInfo
        {
            public LayoutSettingTItemInfo(LayoutSettingTBarInfo owner, FunctionGlobalItemSize size, int moduleCount, int modulePixel, int offsetPixel, int imagePixel, int textHeight, float textZoom)
            {
                this._Owner = owner;
                this._Size = size;
                this._ModuleCount = moduleCount;
                this._ModulePixel = modulePixel;
                this._OffsetPixel = offsetPixel;
                this._ImagePixel = imagePixel;
                this._TextHeight = textHeight;
                this._TextZoom = textZoom;
            }
            private LayoutSettingTBarInfo _Owner;
            private FunctionGlobalItemSize _Size;
            private int _ModuleCount;
            private int _ModulePixel;
            private int _OffsetPixel;
            private int _ImagePixel;
            private int _TextHeight;
            private float _TextZoom;
            /// <summary>
            /// Number of modules for this size (1 - 6)
            /// </summary>
            public int ModuleCount { get { return _ModuleCount; } }
            /// <summary>
            /// Size of one item of this size in pixels
            /// </summary>
            public int ModulePixel { get { return _ModulePixel; } }
            /// <summary>
            /// Size for Image in this item
            /// </summary>
            public Size ImageSize { get { return new Size(_ImagePixel, _ImagePixel); } }
            /// <summary>
            /// Height of text, maximal
            /// </summary>
            public int TextHeight { get { return _TextHeight; } }
            /// <summary>
            /// Zoom for text in this item
            /// </summary>
            public float TextZoom { get { return _TextZoom; } }
        }
        #endregion
        #region InteractiveContainer : Interactivity, Draw
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckLayout(null); return this.GetAllItems(); } }
        protected List<IInteractiveItem> GetAllItems()
        {
            List<IInteractiveItem> items = new List<IInteractiveItem>(base.Childs);
            items.Add(_Splitter);
            return items;
        }
        protected override void DrawStandard(GInteractiveDrawArgs e, System.Drawing.Rectangle boundsAbsolute)
        {
            this.CheckLayout(e.Graphics);
            this._SplitterCheckValue();

            // base.DrawStandard(e, boundsAbsolute);
            this.DrawToolbarBackground(e.Graphics, null);
        }
        /// <summary>
        /// Draw background for toolbar. Can be called from GToolbarGroup.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="boundsDraw"></param>
        internal void DrawToolbarBackground(Graphics graphics, Rectangle? boundsDraw)
        {
            Rectangle toolbarBounds = this.BoundsAbsolute;
            if (boundsDraw.HasValue)
            {
                toolbarBounds.X = boundsDraw.Value.X;
                toolbarBounds.Width = boundsDraw.Value.Width;
            }
            GPainter.DrawAreaBase(graphics, toolbarBounds, Skin.ToolBar.BackColor, GInteractiveState.MouseOver, System.Windows.Forms.Orientation.Horizontal, null, null);
        }
        #endregion
    }
    #endregion
    #region class ToolbarGroup
    /// <summary>
    /// One visual group on toolbar
    /// </summary>
    internal class GToolbarGroup : InteractiveContainer
    {
        #region Constructor, basic properties
        internal static GToolbarGroup CreateFrom(GToolBar owner, FunctionGlobalGroup dataGroup)
        {
            GToolbarGroup group = new GToolbarGroup(owner, dataGroup);

            if (dataGroup.Items != null)
            {
                foreach (FunctionGlobalItem item in dataGroup.Items)
                    group._AddGToolbarItem(GToolbarItem.CreateFrom(group, item));
            }
            group._AddFinalSeparator();

            return group;
        }
        private GToolbarGroup(GToolBar toolbar, FunctionGlobalGroup dataGroup)
        {
            this._Toolbar = toolbar;
            this._DataGroup = dataGroup;
            this._ToolbarItemList = new List<GToolbarItem>();
        }
        /// <summary>
        /// When in this.Items are last item other than Separator, then add new Separator at end.
        /// </summary>
        private void _AddFinalSeparator()
        {
            if (this._ToolbarItemList.Count > 0 && this._ToolbarItemList[this._ToolbarItemList.Count - 1].ItemType != FunctionGlobalItemType.Separator)
                _AddGToolbarItem(GToolbarItem.CreateSeparator(this));
        }
        /// <summary>
        /// Add one GToolbarGroup into Childs and into _ToolbarItemList.
        /// </summary>
        /// <param name="group"></param>
        private void _AddGToolbarItem(GToolbarItem item)
        {
            this.AddItem(item);
            this._ToolbarItemList.Add(item);
        }
        private GToolBar _Toolbar;
        private FunctionGlobalGroup _DataGroup;
        /// <summary>
        /// All interactive items in this group
        /// </summary>
        private List<GToolbarItem> _ToolbarItemList;
        /// <summary>
        /// Provider of functions in this group = Service of type IFunctionGlobal
        /// </summary>
        public IFunctionProvider Provider { get { return this._DataGroup.Provider; } }
        /// <summary>
        /// Type of provider of items in this group = Service of type IFunctionGlobal
        /// </summary>
        public Type ProviderType { get { return this._DataGroup.ProviderType; } }
        /// <summary>
        /// Data for this Group
        /// </summary>
        public FunctionGlobalGroup DataGroup { get { return this._DataGroup; } }
        /// <summary>
        /// Toolbar in which is this group located.
        /// </summary>
        internal GToolBar Toolbar { get { return this._Toolbar; } }
        /// <summary>
        /// Setting for layout on current toolbar (by ToolbarSize)
        /// </summary>
        internal GToolBar.LayoutSettingTBarInfo TBarSetting { get { return this._Toolbar.TBarSetting; } }
        /// <summary>
        /// Return index of specified item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private int _SearchItemIndex(GToolbarItem item)
        {
            if (item == null) return -1;
            return this._ToolbarItemList.IndexOf(item);
        }
        #endregion
        #region Data properties
        /// <summary>
        /// Title of this group
        /// </summary>
        public string DataTitle { get { return this._DataGroup.Title; } }
        /// <summary>
        /// ToolTip for this group (active on Title bar)
        /// </summary>
        public string DataToolTipTitle { get { return this._DataGroup.ToolTipTitle; } }
        /// <summary>
        /// Width of one group in this group, in "modules", where one module is equal to one "micro" icon.
        /// Default = 24, range is 3 - 64;
        /// </summary>
        public int DataLayoutWidth { get { int w = this._DataGroup.LayoutWidth; return (w < 3 ? 3 : (w > 64 ? 64 : w)); } }
        #endregion
        #region Prepare layout of this item in ToolbarGroup, prepare layout for inner Items
        /// <summary>
        /// Prepare Layout (=this.Bounds and all Items.Bounds) for this Group
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="toolbarBounds"></param>
        internal void PrepareLayout(Graphics graphics, ref int x)
        {
            // 1. prepare ModuleSize and PixelSize:
            foreach (GToolbarItem item in this._ToolbarItemList)
                item.PrepareLayout(graphics);

            // 2. Arrange items to layout:
            int tableX = this.TBarSetting.ContentBounds.X;
            List<ILayoutItem> layoutItemList = new List<ILayoutItem>(this._ToolbarItemList);
            LayoutEngineArgs layoutArgs = this.PrepareLayoutArgs();
            while (true)
            {   // One call LayoutEngine.CreateLayout() create one "table" (visual group) from items:
                int count = LayoutEngine.CreateLayout(layoutItemList, layoutArgs);
                if (count <= 0) break;
                this.PrepareLayoutOne(layoutArgs, ref tableX);
                layoutArgs.PrepareNextProcess(false, false);
            }

            // 3. Group Bounds:
            var tBarSetting = this.TBarSetting;
            int groupX = x;
            int groupY = tBarSetting.ToolbarBounds.Y;
            int groupW = tableX;
            int groupH = tBarSetting.ToolbarBounds.Height;
            this.Bounds = new Rectangle(groupX, groupY, groupW, groupH);

            x += tableX;
        }
        /// <summary>
        /// Prepare and return arguments for LayoutEngine
        /// </summary>
        /// <returns></returns>
        private LayoutEngineArgs PrepareLayoutArgs()
        {
            int height = this.TBarSetting.HeightModule;
            LayoutEngineArgs layoutArgs = new LayoutEngineArgs()
            {
                HeightTarget = height,
                ProcessStartIndex = 0,
                ProcessStartLocationX = 0,
                ProcessStartLocationY = 0,
                RemoveProcessedItems = false,
                WidthMaximal = this.DataLayoutWidth,
                WidthOptimal = this.DataLayoutWidth
            };
            return layoutArgs;
        }
        /// <summary>
        /// Process layout for one layout-table in this Group
        /// </summary>
        /// <param name="layoutArgs"></param>
        /// <param name="tableX"></param>
        private void PrepareLayoutOne(LayoutEngineArgs layoutArgs, ref int tableX)
        {
            if (layoutArgs.ResultProcessedItemCount <= 0) return;
            GToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
            int itemY = tBarSetting.ContentBounds.Y;
            int nextX = tableX;
            int modulePixel = tBarSetting.PixelPerModule;
            int moduleWidth = layoutArgs.ResultProcessedItemWidth;
            foreach (var layoutRow in layoutArgs.ResultRows)
            {   // Rows in one "layout table":
                int rowWidth = layoutRow.Size.Width;
                if (rowWidth > 0)
                {   // This row has items with positive sizes, this is standard row:
                    decimal ratio = (decimal)modulePixel * (decimal)moduleWidth / (decimal)rowWidth;
                    foreach (var layoutItem in layoutRow.Items)
                    {
                        GToolbarItem item = layoutItem as GToolbarItem;
                        if (item != null && item.ModuleBounds.HasValue)
                        {
                            Rectangle mb = item.ModuleBounds.Value;
                            int ix = (int)Math.Round((ratio * (decimal)mb.X), 0);
                            int ir = (int)Math.Round((ratio * (decimal)mb.Right), 0);
                            int iy = itemY + modulePixel * mb.Y;
                            int ih = modulePixel * mb.Height;
                            item.Bounds = new Rectangle(tableX + ix, iy, (ir - ix), ih);
                            int itemR = item.Bounds.Right;
                            if (nextX < itemR) nextX = itemR;
                        }
                    }
                }
                else
                {   // This row has items with zero sizes, this is only separator:
                    foreach (var layoutItem in layoutRow.Items)
                    {
                        GToolbarItem item = layoutItem as GToolbarItem;
                        if (item != null)
                        {
                            bool isLastSeparator = ((layoutArgs.ResultRows.Length == 1 && layoutRow.Items.Count == 1) && (this._SearchItemIndex(item) == (this._ToolbarItemList.Count - 1)));
                            bool hasModuleBounds = (item.ModuleBounds.HasValue);
                            if (isLastSeparator || !hasModuleBounds)
                            {   // Any separator or item without ModuleBounds: has height for whole toolbar (ContentBounds or ToolbarBounds for last separator):
                                Rectangle area = (isLastSeparator ? tBarSetting.ToolbarBounds : tBarSetting.ContentBounds);
                                int itemX = tableX;
                                int itemR = tableX + item.PixelSizeMin.Width;
                                int iy = area.Y;
                                int ih = area.Height;
                                item.Bounds = new Rectangle(itemX, iy, (itemR - itemX), ih);
                                if (nextX < itemR) nextX = itemR;
                            }
                            else
                            {   // Any items with zero width and detected position (?):
                                Rectangle mb = item.ModuleBounds.Value;
                                int itemX = tableX;
                                int itemR = tableX + item.PixelSizeMin.Width;
                                int iy = itemY + modulePixel * mb.Y;
                                int ih = modulePixel * mb.Height;
                                item.Bounds = new Rectangle(itemX, iy, (itemR - itemX), ih);
                                if (nextX < itemR) nextX = itemR;
                            }
                        }
                    }
                }
            }
            tableX = nextX;
        }
        /// <summary>
        /// Size of toolbar.
        /// Small: items has height 24/48 (Small/Large);
        /// Medium: items has height 32/64 (Small/Large) (default);
        /// Large: items has height 48/96 (Small/Large);
        /// </summary>
        internal ComponentSize ToolbarSize { get { return this._Toolbar.ToolbarSize; } }
        #endregion
        #region InteractiveContainer : Interactivity, Draw
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.Toolbar.DrawToolbarBackground(e.Graphics, boundsAbsolute);
            // Not: base.DrawStandard(e, boundsAbsolute);

            this.DrawGroupTitle(e, boundsAbsolute);
        }
        internal void DrawGroupTitle(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            GToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
            Rectangle tb = tBarSetting.TitleBounds;
            Rectangle titleBounds = new Rectangle(this.Bounds.X, tb.Y, this.Bounds.Width - 4, tb.Height);
            Rectangle titleAbsoluteBounds = this.GetAbsoluteBounds(titleBounds);

            if (this.IsMouseActive)
                GPainter.DrawAreaBase(e.Graphics, titleAbsoluteBounds, Skin.ToolBar.TitleBackColor, GInteractiveState.Enabled, System.Windows.Forms.Orientation.Horizontal, null, null);

            if (!String.IsNullOrEmpty(this.DataTitle))
                GPainter.DrawString(e.Graphics, titleAbsoluteBounds, this.DataTitle, Skin.ToolBar.TextColor, tBarSetting.TitleFont, ContentAlignment.MiddleCenter);
        }
        #endregion
    }
    #endregion
    #region class ToolbarItem
    internal class GToolbarItem : InteractiveContainer, ILayoutItem
    {
        #region Constructor, basic properties
        private GToolbarItem(GToolbarGroup toolbarGroup, FunctionGlobalItem dataItem)
        {
            this._ToolbarGroup = toolbarGroup;
            this._DataItem = dataItem;
        }
        public override string ToString()
        {
            if (this._DataItem != null)
                return this.ItemSize.ToString() + " " + this.ItemType.ToString() + ": " + this.ItemText;
            return this.ItemSize.ToString() + " " + this.ItemType.ToString();
        }
        internal static GToolbarItem CreateFrom(GToolbarGroup toolbarGroup, FunctionGlobalItem dataItem)
        {
            return new GToolbarItem(toolbarGroup, dataItem);
        }
        internal static GToolbarItem CreateSeparator(GToolbarGroup toolbarGroup)
        {
            FunctionGlobalItem dataItem = new FunctionGlobalItem(toolbarGroup.Provider)
            {
                ItemType = FunctionGlobalItemType.Separator,
                Size = FunctionGlobalItemSize.Whole
            };
            return new GToolbarItem(toolbarGroup, dataItem);
        }
        private GToolbarGroup _ToolbarGroup;
        private FunctionGlobalItem _DataItem;
        /// <summary>
        /// Provider of functions in this group = Service of type IFunctionGlobal
        /// </summary>
        public IFunctionProvider Provider { get { return this._DataItem.Provider; } }
        /// <summary>
        /// Type of owner of this group = Service of type IFunctionGlobal
        /// </summary>
        public Type ProviderType { get { return this._DataItem.ProviderType; } }
        /// <summary>
        /// Toolbar in which is this group located.
        /// </summary>
        internal GToolBar Toolbar { get { return this._ToolbarGroup.Toolbar; } }
        /// <summary>
        /// Setting for layout on current toolbar (by ToolbarSize)
        /// </summary>
        internal GToolBar.LayoutSettingTBarInfo TBarSetting { get { return this.Toolbar.TBarSetting; } }
        /// <summary>
        /// Setting for layout current Item by ItemSize
        /// </summary>
        internal GToolBar.LayoutSettingTItemInfo TItemSetting { get { return this.Toolbar.TBarSetting.GetInfoForSize(this.ItemSize); } }
        #endregion
        #region Data from DataItem
        /// <summary>
        /// Typ prvku GlobalItem
        /// </summary>
        public FunctionGlobalItemType ItemType { get { return this._DataItem.ItemType; } }
        /// <summary>
        /// Velikost prvku na toolbaru, vzhledem k jeho výšce
        /// </summary>
        public FunctionGlobalItemSize ItemSize { get { return this._DataItem.Size; } }
        /// <summary>
        /// Nápověda ke zpracování layoutu této položky
        /// </summary>
        public LayoutHint ItemLayoutHint { get { return this._DataItem.LayoutHint; } }
        /// <summary>
        /// Explicitně požadovaná šířka prvku v počtu modulů
        /// </summary>
        public int? ItemModuleWidth { get { return this._DataItem.ModuleWidth; } }
        /// <summary>
        /// Ikonka
        /// </summary>
        protected Image ItemImage { get { return this._DataItem.Image; } }
        // public FontInfo ItemFont { get { FontInfo itemFont = this._DataItem.Font; return (itemFont != null ? itemFont : FontInfo.Menu); } }
        public FontInfo CurrentItemFont { get { return this.TBarSetting.GetFontInfo(this.ItemSize, this._DataItem.Font); } }
        /// <summary>
        /// Text for item. When ItemSize is Micro, then Text is empty.
        /// </summary>
        public string ItemText { get { return (this._DataItem.Size == FunctionGlobalItemSize.Micro ? "" : this._DataItem.TextText); } }
        /// <summary>
        /// ToolTip for item.
        /// </summary>
        public string ItemToolTip { get { return this._DataItem.ToolTipText; } }
        /// <summary>
        /// Is Visible?
        /// </summary>
        public override bool IsVisible { get { return this._DataItem.IsVisible; } set { this._DataItem.IsVisible = value; base.IsVisible = value; } }
        /// <summary>
        /// Is Enabled?
        /// </summary>
        public override bool IsEnabled { get { return this._DataItem.IsEnabled; } set { this._DataItem.IsEnabled = value; base.IsEnabled = value; } }
        /// <summary>
        /// SubItem array (for ComboBox, SplitButton, and so on)
        /// </summary>
        public EList<FunctionItem> ItemSubItems { get { this.CallSubItemsEnumerateBefore(); this._DataItem.OnSubItemsEnumerateBefore(); return this._DataItem.SubItems; } }
        /// <summary>
        /// true when item has down arrow (ComboBox, SplitButton)
        /// </summary>
        public bool HasDownArrow
        {
            get
            {
                switch (this.ItemType)
                {
                    case FunctionGlobalItemType.Button:
                        return (this.ItemSubItems.Count > 0);
                    case FunctionGlobalItemType.ComboBox:
                        return true;
                }
                return false; 
            }
        }
        #endregion
        #region Prepare layout of this item in ToolbarGroup. ILayoutItem explicit members
        /// <summary>
        /// Prepare Layout for this item:
        /// Calculate inner Bounds (BoundsImage and BoundsText, they are relative to this.Bounds).
        /// Then calculate this.Size as "minimum" size for this item.
        /// Do not calculate this.Bounds, this is dependent on layout of neighborough items.
        /// This will be calculated in FinaliseLayout() method.
        /// </summary>
        /// <param name="graphics"></param>
        internal void PrepareLayout(Graphics graphics)
        {
            switch (this.ItemType)
            {
                case FunctionGlobalItemType.Separator:
                    this.PrepareBoundsSeparator(graphics);
                    break;
                case FunctionGlobalItemType.Label:
                    this.PrepareBoundsLabel(graphics);
                    break;
                case FunctionGlobalItemType.Button:
                    this.PrepareBoundsButton(graphics);
                    break;
                case FunctionGlobalItemType.ComboBox:
                    this.PrepareBoundsComboBox(graphics);
                    break;
                case FunctionGlobalItemType.Image:
                    this.PrepareBoundsImage(graphics);
                    break;
            }
        }
        /// <summary>
        /// Bounds for draw Image, relative to this.Bounds!!!
        /// </summary>
        protected Rectangle BoundsImage;
        /// <summary>
        /// Bounds for draw Text, relative to this.Bounds!!!
        /// </summary>
        protected Rectangle BoundsText;
        /// <summary>
        /// Bounds for draw Icon (down arrow), relative to this.Bounds!!!
        /// </summary>
        protected Rectangle BoundsIcon;
        /// <summary>
        /// Minimum Size in pixel of this item, by Image, Size, Text, Font.
        /// </summary>
        internal Size PixelSizeMin { get; private set; }
        /// <summary>
        /// Size of this item in "modules", where "1" = size of Micro icon without text.
        /// When item has any text, then its ModuleSize.Width is rounded to nearest higher Width.
        /// </summary>
        internal Size ModuleSize { get; private set; }
        /// <summary>
        /// Position of this item in "modules", from process in LayoutEngine algorithm
        /// </summary>
        internal Rectangle? ModuleBounds { get; private set; }
        /// <summary>
        /// Return size for image, for this Size and current Toolbar (LayoutInfo, Zoom)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        protected Size GetImageSize(Image image)
        {
            if (image == null) return new Size(0, 0);
            GToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;
            return itemSetting.ImageSize;
        }
        /// <summary>
        /// Return size for image, for this Size and current Toolbar (LayoutInfo, Zoom)
        /// </summary>
        /// <param name="withThisText"></param>
        /// <returns></returns>
        protected Size GetImageSizeFromSubItems(bool withThisImage, EList<FunctionItem> subItems)
        {
            GToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;
            if (withThisImage && this.ItemImage != null)
                return itemSetting.ImageSize;

            foreach (FunctionItem subItem in subItems)
            {
                if (subItem != null && subItem.Image != null)
                    return itemSetting.ImageSize;
            }

            return Size.Empty;
        }
        /// <summary>
        /// Return size for specified text, for this Font and current Toolbar (LayoutInfo, Zoom)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        protected Size GetTextSize(Graphics graphics, string text)
        {
            if (String.IsNullOrEmpty(text)) return new Size(0, 0);
            FontInfo fontInfo = this.CurrentItemFont;
            Size textSize = (graphics != null
                ? GPainter.MeasureString(graphics, text, fontInfo)
                : GPainter.MeasureString(text, fontInfo));
            return textSize;
        }
        /// <summary>
        /// Returns Max(Size) for all subitems, and this.ItemText
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="withThisText"></param>
        /// <returns></returns>
        protected Size GetTextSizeFromSubItems(Graphics graphics, bool withThisText, EList<FunctionItem> subItems, Int32? maxModuleWidth)
        {
            Size size = Size.Empty;

            if (withThisText)
                size = size.Max(this.GetTextSize(graphics, this.ItemText));

            foreach (FunctionItem subItem in subItems)
            {
                if (subItem != null)
                    size = size.Max(this.GetTextSize(graphics, subItem.TextText));
            }

            // Max text width = Nnn * module:
            if (maxModuleWidth.HasValue && maxModuleWidth.Value > 0)
            {
                int maxTextWidth = this.TItemSetting.ModulePixel * maxModuleWidth.Value;
                if (size.Width > maxTextWidth) size.Width = maxTextWidth;
            }

            return size;
        }
        /// <summary>
        /// Get size for Icon (DownArrow)
        /// </summary>
        /// <param name="withIcon"></param>
        /// <returns></returns>
        protected Size GetIconSize(bool withIcon)
        {
            return (withIcon ? new Size(10, 10) : Size.Empty);
        }
        /// <summary>
        /// Size of this item (input)
        /// </summary>
        Size ILayoutItem.ItemSize { get { return this.ModuleSize; } }
        /// <summary>
        /// Nápověda ke zpracování layoutu této položky
        /// </summary>
        LayoutHint ILayoutItem.Hint { get { return this.ItemLayoutHint; } }
        /// <summary>
        /// Explicitně požadovaná šířka prvku v počtu modulů
        /// </summary>
        int? ILayoutItem.ModuleWidth { get { return this.ItemModuleWidth; } }
        /// <summary>
        /// Position (=Location + this.ItemSize) of this item after Layout processed (result)
        /// </summary>
        Rectangle? ILayoutItem.ItemBounds { get { return this.ModuleBounds; } set { this.ModuleBounds = value; } }
        #endregion
        #region Společné metody pro přípravu layoutu
        /// <summary>
        /// Připraví data pro layout aktuálního prvku, společná metoda
        /// </summary>
        /// <param name="graphics"></param>
        protected void PrepareBoundsCommon(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics, null, null);
        }
        /// <summary>
        /// Připraví data pro layout aktuálního prvku, společná metoda
        /// </summary>
        /// <param name="graphics"></param>
        protected void PrepareBoundsCommon(Graphics graphics, Func<Graphics, Size> getImageSize, Func<Graphics, Size> getTextSize)
        {
            GToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
            GToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;

            Size sizeImage = (getImageSize != null ? getImageSize(graphics) : this.GetImageSize(this.ItemImage));      // Velikost pro prvek: Velká ikona = obrázek tlačítka
            Size sizeText = (getTextSize != null ? getTextSize(graphics) : this.GetTextSize(graphics, this.ItemText)); // Velikost pro prvek: Text tlačítka
            Size sizeIcon = this.GetIconSize(this.HasDownArrow);               // Ikona reprezentující rozbalovací tlačítko u combo boxu
            if (sizeText.Height > itemSetting.TextHeight) sizeText.Height = itemSetting.TextHeight;
            int textIconWidth = sizeText.Width + sizeIcon.Width;               // Šířka textové části = text + šipka dolů

            if (this.ItemSize == FunctionGlobalItemSize.Whole)
            {   // Celá výška toolbaru: nahoře je Image, pod ním je Text, obě jsou zarovnány na svislý střed:
                int w = (sizeImage.Width > textIconWidth ? sizeImage.Width : textIconWidth);
                int c = 2 + (w / 2);
                int y = 2;
                this.BoundsImage = new Rectangle(c - (sizeImage.Width / 2), y, sizeImage.Width, sizeImage.Height);
                if (sizeImage.Height > 0) y = y + sizeImage.Height + 2;
                this.BoundsText = new Rectangle(c - (textIconWidth / 2), y, sizeText.Width, sizeText.Height);
                this.BoundsIcon = new Rectangle(this.BoundsText.Right, this.BoundsText.Y, sizeIcon.Width, sizeText.Height);
                if (sizeText.Height > 0) y = y + sizeText.Height + 2;

                int modulesWidth = tBarSetting.GetModuleCount(w + 4);
                int modulesHeight = this.TBarSetting.HeightModule;
                this.ModuleSize = new Size(modulesWidth, modulesHeight);
                this.PixelSizeMin = tBarSetting.GetPixelSize(this.ModuleSize);
            }
            else
            {   // Small item: small image on left, before text, horizontally aligned to Y center:
                int h = itemSetting.ModulePixel;
                int c = (h / 2);
                int x = 2;
                this.BoundsImage = new Rectangle(x, c - (sizeImage.Height / 2), sizeImage.Width, sizeImage.Height);
                if (sizeImage.Width > 0) x = x + sizeImage.Width;
                if (sizeImage.Width > 0 && sizeText.Width > 0) x = x + 4;
                this.BoundsText = new Rectangle(x, c - (sizeText.Height / 2), sizeText.Width, sizeText.Height);
                if (sizeText.Width > 0) x = x + sizeText.Width + 3;
                this.BoundsIcon = new Rectangle(x, 2, sizeIcon.Width, h - 4);
                if (sizeIcon.Width > 0) x = x + sizeIcon.Width + 3;

                int modulesWidth = tBarSetting.GetModuleCount(x);
                int modulesHeight = itemSetting.ModuleCount;
                this.ModuleSize = new Size(modulesWidth, modulesHeight);
                this.PixelSizeMin = tBarSetting.GetPixelSize(this.ModuleSize);
            }
        }
        #endregion
        #region ItemType Separator: specific methods
        protected virtual void PrepareBoundsSeparator(Graphics graphics)
        {
            this.PixelSizeMin = new Size(5, this.TBarSetting.ContentBounds.Height);
            this.ModuleSize = new Size(0, this.TBarSetting.HeightModule);
        }
        protected virtual void DrawStandardSeparator(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            e.Graphics.DrawLine(Skin.Pen(Skin.ToolBar.SeparatorDarkColor), boundsAbsolute.X + 3, boundsAbsolute.Y, boundsAbsolute.X + 3, boundsAbsolute.Bottom);
            e.Graphics.DrawLine(Skin.Pen(Skin.ToolBar.SeparatorLightColor), boundsAbsolute.X + 4, boundsAbsolute.Y, boundsAbsolute.X + 4, boundsAbsolute.Bottom);
        }
        protected virtual string GetToolTipTextSeparator()
        {
            return this.ItemToolTip;
        }
        protected virtual void LeftClickSeparator(GInteractiveChangeStateArgs e)
        { }
        #endregion
        #region ItemType Label: specific methods
        protected virtual void PrepareBoundsLabel(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics);
        }
        protected virtual void DrawStandardLabel(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this._DataItem.Image != null)
            {
                Rectangle boundsImageAbsolute = this.BoundsImage.ShiftBy(boundsAbsolute.Location);
                e.Graphics.DrawImage(this._DataItem.Image, boundsImageAbsolute);
            }

            string text = this.ItemText;
            if (!String.IsNullOrEmpty(text))
            {
                Rectangle boundsTextAbsolute = this.BoundsText.ShiftBy(boundsAbsolute.Location);
                FontInfo fontInfo = this.CurrentItemFont;
                GPainter.DrawString(e.Graphics, boundsTextAbsolute, text, Skin.ToolBar.TextColor, fontInfo, ContentAlignment.MiddleCenter);
            }
        }
        protected virtual string GetToolTipTextLabel()
        {
            return this.ItemToolTip;
        }
        protected virtual void LeftClickLabel(GInteractiveChangeStateArgs e)
        {
            this.CallItemAction();
        }
        #endregion
        #region ItemType Button: specific methods
        protected virtual void PrepareBoundsButton(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics);
        }
        protected virtual void DrawStandardButton(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemBackground(e, boundsAbsolute);
            this.DrawItemImage(e, boundsAbsolute);
            this.DrawItemText(e, boundsAbsolute);
        }
        protected virtual string GetToolTipTextButton()
        {
            return this.ItemToolTip;
        }
        protected virtual void LeftClickButton(GInteractiveChangeStateArgs e)
        {
            this.CallItemAction();
        }
        #endregion
        #region ItemType ComboBox: specific methods
        protected virtual void PrepareBoundsComboBox(Graphics graphics)
        {
            EList<FunctionItem> subItems = this.ItemSubItems;
            this.PrepareBoundsCommon(graphics, 
                g => this.GetImageSizeFromSubItems(true, subItems), 
                g => this.GetTextSizeFromSubItems(g, true, subItems, 7));
        }
        protected virtual void DrawStandardComboBox(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemBackground(e, boundsAbsolute, 1, true);
            FunctionItem activeItem = this._DataItem.Value as FunctionItem;
            if (activeItem == null) activeItem = this._DataItem;
            this.DrawItemImage(e, boundsAbsolute, activeItem);
            this.DrawItemText(e, boundsAbsolute, activeItem);
            this.DrawItemIcon(e, boundsAbsolute);
        }
        protected virtual string GetToolTipTextComboBox()
        {
            FunctionItem activeItem = this._DataItem.Value as FunctionItem;
            return (activeItem != null ? activeItem.ToolTipText : this.ItemToolTip);
        }
        protected virtual void LeftClickComboBox(GInteractiveChangeStateArgs e)
        {
            var host = this.Host;
            if (host == null) return;

            Rectangle bounds = this.BoundsAbsolute;
            Point point = new Point(bounds.X, bounds.Bottom - 1);

            EList<FunctionItem> subItems = this.ItemSubItems;
            System.Windows.Forms.ToolStripDropDownMenu menu = FunctionItem.CreateDropDownMenuFrom(subItems);
            menu.MinimumSize = new Size(bounds.Width, 0); // 3 * this.TItemSetting.ModulePixel);
            menu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(_ComboMenu_ItemClicked);
            menu.Show(host, point, System.Windows.Forms.ToolStripDropDownDirection.BelowRight);
        }
        private void _ComboMenu_ItemClicked(object sender, System.Windows.Forms.ToolStripItemClickedEventArgs e)
        {
            FunctionItem item = e.ClickedItem.Tag as FunctionItem;
            if (e.ClickedItem is System.Windows.Forms.ToolStripSeparator || item == null) return;

            System.Windows.Forms.ToolStrip owner = e.ClickedItem.Owner;
            if (owner != null) owner.Hide();

            this._DataItem.Value = item;
            this.CallItemAction(item);
            this.Parent.Repaint();
        }
        #endregion
        #region ItemType Image: specific methods
        protected virtual void PrepareBoundsImage(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics);
        }
        protected virtual void DrawStandardImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this._DataItem.Image != null)
            {
                Rectangle boundsImageAbsolute = this.BoundsImage.ShiftBy(boundsAbsolute.Location);
                e.Graphics.DrawImage(this._DataItem.Image, boundsImageAbsolute);
            }

            string text = this.ItemText;
            if (!String.IsNullOrEmpty(text))
            {
                Rectangle boundsTextAbsolute = this.BoundsText.ShiftBy(boundsAbsolute.Location);
                FontInfo fontInfo = this.CurrentItemFont;
                GPainter.DrawString(e.Graphics, boundsTextAbsolute, text, Skin.ToolBar.TextColor, fontInfo, ContentAlignment.MiddleCenter);
            }
        }
        protected virtual string GetToolTipTextImage()
        {
            return this.ItemToolTip;
        }
        protected virtual void LeftClickImage(GInteractiveChangeStateArgs e)
        {
            this.CallItemAction();
        }
        #endregion
        #region Common Draw and Click methods
        protected void DrawItemBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemBackground(e, boundsAbsolute, 2, false);
        }
        protected void DrawItemBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int roundCorner, bool forceBorder)
        {
            bool isEnabled = this.IsEnabled;
            bool drawBackground = isEnabled && this.IsMouseActive;
            bool drawBorders = forceBorder || (isEnabled && this.IsMouseDown);

            if (drawBackground || drawBorders)
                GPainter.DrawButtonBase(e.Graphics, boundsAbsolute, Skin.ToolBar.ItemBackColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, roundCorner, null, null, drawBackground, drawBorders, Skin.ToolBar.ItemBorderColor);
        }
        protected void DrawItemImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemImage(e, boundsAbsolute, this._DataItem);
        }
        protected void DrawItemImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute, FunctionItem activeItem)
        {
            Image image = activeItem.Image;
            if (image != null)
            {
                Rectangle boundsImageAbsolute = this.BoundsImage.ShiftBy(boundsAbsolute.Location);
                GPainter.DrawImage(e.Graphics, boundsImageAbsolute, image, activeItem.IsEnabled, ContentAlignment.MiddleCenter);
            }
        }
        protected void DrawItemText(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemText(e, boundsAbsolute, this._DataItem);
        }
        protected void DrawItemText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, FunctionItem activeItem)
        {
            string text = (activeItem != null ? this.ItemSize == FunctionGlobalItemSize.Micro ? "" : activeItem.TextText : null);
            if (!String.IsNullOrEmpty(text))
            {
                Rectangle boundsTextAbsolute = this.BoundsText.ShiftBy(boundsAbsolute.Location);
                FontInfo fontInfo = this.CurrentItemFont;
                Color textColor = Skin.ModifyForeColorByState(Skin.ToolBar.TextColor, this.CurrentState);
                GPainter.DrawString(e.Graphics, boundsTextAbsolute, text, textColor, fontInfo, ContentAlignment.MiddleCenter);
            }
        }
        protected void DrawItemIcon(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Rectangle boundsIconAbsolute = this.BoundsIcon.ShiftBy(boundsAbsolute.Location);
            int w = boundsIconAbsolute.Width;
            if (w <= 0) return;
            int x = boundsIconAbsolute.X;
            int y  = boundsIconAbsolute.Y;
            int h = boundsIconAbsolute.Height;
            int b = boundsIconAbsolute.Bottom;
            e.Graphics.DrawLine(Skin.Pen(Color.DimGray), x, y + 2, x, b - 3);
            e.Graphics.DrawLine(Skin.Pen(Color.LightGray), x + 1, y + 2, x + 1, b - 3);
        }
        protected virtual void CallSubItemsEnumerateBefore()
        {
            this.CallSubItemsEnumerateBefore(this._DataItem);
        }
        protected virtual void CallSubItemsEnumerateBefore(FunctionItem activeItem)
        {
            this._ToolbarGroup.DataGroup.OnSubItemsEnumerateBefore(activeItem);
        }
        protected virtual void CallItemAction()
        {
            this.CallItemAction(this._DataItem);
        }
        protected virtual void CallItemAction(FunctionItem activeItem)
        {
            this._ToolbarGroup.DataGroup.OnItemAction(activeItem);
            if (activeItem.Parent != null)
                activeItem.Parent.OnSubItemsClick(this._DataItem);
            activeItem.OnClick();
        }
        #endregion
        #region InteractiveContainer : Interactivity, Draw
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (!this.IsVisible) return;
            switch (this.ItemType)
            {
                case FunctionGlobalItemType.Separator:
                    this.DrawStandardSeparator(e, boundsAbsolute);
                    break;
                case FunctionGlobalItemType.Label:
                    this.DrawStandardLabel(e, boundsAbsolute);
                    break;
                case FunctionGlobalItemType.Button:
                    this.DrawStandardButton(e, boundsAbsolute);
                    break;
                case FunctionGlobalItemType.ComboBox:
                    this.DrawStandardComboBox(e, boundsAbsolute);
                    break;
                case FunctionGlobalItemType.Image:
                    this.DrawStandardImage(e, boundsAbsolute);
                    break;
            }
        }
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            bool isEnabled = this.IsEnabled;
            base.AfterStateChanged(e);
            switch (e.ChangeState)
            {
                case GInteractiveChangeState.LeftDown:
                    if (isEnabled)
                        this.Parent.Repaint();
                    break;
                case GInteractiveChangeState.LeftUp:
                    if (isEnabled)
                        this.Parent.Repaint();
                    break;
                case GInteractiveChangeState.MouseEnter:
                    if (isEnabled)
                        this.Parent.Repaint();
                    break;
                case GInteractiveChangeState.MouseLeave:
                    if (isEnabled)
                        this.Parent.Repaint();
                    break;
            }
        }
        /// <summary>
        /// Připraví data pro zobrazení ToolTipu. Metodu volá předek.
        /// </summary>
        /// <param name="e"></param>
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            string toolTip = this.GetToolTipText();
            if (String.IsNullOrEmpty(toolTip)) return;
            e.ToolTipData.InfoText = toolTip;

            string toolTipTitle = this._ToolbarGroup.DataToolTipTitle;
            e.ToolTipData.TitleText = (!String.IsNullOrEmpty(toolTipTitle) ? toolTipTitle : this._ToolbarGroup.DataTitle);
        }
        protected virtual string GetToolTipText()
        {
            switch (this.ItemType)
            {
                case FunctionGlobalItemType.Separator: return this.GetToolTipTextSeparator();
                case FunctionGlobalItemType.Label: return this.GetToolTipTextLabel();
                case FunctionGlobalItemType.Button: return this.GetToolTipTextButton();
                case FunctionGlobalItemType.ComboBox: return this.GetToolTipTextComboBox();
                case FunctionGlobalItemType.Image: return this.GetToolTipTextImage();
            }
            return this.ItemToolTip;
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);

            if (!this.IsVisible || !this.IsEnabled) return;
            switch (this.ItemType)
            {
                case FunctionGlobalItemType.Separator:
                    this.LeftClickSeparator(e);
                    break;
                case FunctionGlobalItemType.Label:
                    this.LeftClickLabel(e);
                    break;
                case FunctionGlobalItemType.Button:
                    this.LeftClickButton(e);
                    break;
                case FunctionGlobalItemType.ComboBox:
                    this.LeftClickComboBox(e);
                    break;
                case FunctionGlobalItemType.Image:
                    this.LeftClickImage(e);
                    break;
            }
        }
        #endregion
    }
    #endregion
}
