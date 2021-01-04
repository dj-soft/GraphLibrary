using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Localizable;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class ToolBar : Toolbar kreslený jako panel
    /// <summary>
    /// <see cref="ToolBar"/> : Kompletní nabídka menu
    /// </summary>
    public class ToolBar : InteractiveContainer
    {
        #region Vytvoření Toolbaru bez načítání obsahu, public prvky
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public ToolBar(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ToolBar()
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
            this._ToolbarGroupList = new List<ToolBarGroup>();
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
            this._Splitter = new Splitter() { Orientation = System.Windows.Forms.Orientation.Horizontal };
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
        /// <summary>
        /// Přepočítá souřadnice
        /// </summary>
        /// <param name="withSplitterValue"></param>
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
        private Splitter _Splitter;
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
            this._ToolbarGroupList.Clear();

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
        /// Metoda do this ToolBaru přidá dodanou grupu funkcí
        /// </summary>
        /// <param name="group"></param>
        public void AddGroup(FunctionGlobalGroup group)
        {
            this._AddGToolbarGroup(ToolBarGroup.CreateFrom(this, group));
        }
        /// <summary>
        /// Metoda do this ToolBaru přidá dodanou grupu funkcí
        /// </summary>
        /// <param name="groups"></param>
        public void AddGroups(IEnumerable<FunctionGlobalGroup> groups)
        {
            this._AddGuiGroups(groups);
        }
        /// <summary>
        /// Souhrn všech grup v toolbaru (datové objekty)
        /// </summary>
        public FunctionGlobalGroup[] FunctionGroups { get { return this._ToolbarGroupList.Select(g => g.DataGroup).ToArray(); } }
        /// <summary>
        /// Souhrn všech prvků ve všech grupách toolbaru (datové objekty)
        /// </summary>
        public FunctionGlobalItem[] FunctionItems { get { return this._ToolbarGroupList.SelectMany(g => g.DataGroup.Items).ToArray(); } }
        /// <summary>
        /// Souhrn všech grafických grup v toolbaru (vizuální objekty)
        /// </summary>
        internal ToolBarGroup[] GFunctionGroups { get { return this._ToolbarGroupList.ToArray(); } }
        /// <summary>
        /// Souhrn všech grafických prvků v toolbaru (vizuální objekty) = tlačítka, labely, images...
        /// </summary>
        internal ToolBarItem[] GFunctionItems { get { return this._ToolbarGroupList.SelectMany(g => g.FunctionGItems).ToArray(); } }
        /// <summary>
        /// Vymaže všechny prvky Toolbaru
        /// </summary>
        public void ClearToolBar()
        {
            this.ClearItems();
        }
        /// <summary>
        /// Create GUI items (for Groups) and add it into this Items
        /// </summary>
        /// <param name="groups"></param>
        private void _AddGuiGroups(IEnumerable<FunctionGlobalGroup> groups)
        {
            foreach (FunctionGlobalGroup group in groups)
                this._AddGToolbarGroup(ToolBarGroup.CreateFrom(this, group));
        }
        /// <summary>
        /// Add one GToolbarGroup into Childs and into _ToolbarItemList.
        /// </summary>
        /// <param name="group"></param>
        private void _AddGToolbarGroup(ToolBarGroup group)
        {
            this.AddItem(group);
            this._ToolbarGroupList.Add(group);
        }
        private List<ToolBarGroup> _ToolbarGroupList;
        #endregion
        #region Layout of toolbar - Check, Invalidate, Prepare
        /// <summary>
        /// Zajistí platnost layoutu Toolbaru v aktuální situaci
        /// </summary>
        /// <param name="graphics"></param>
        protected void CheckLayout(Graphics graphics)
        {
            bool isValidSize = (this._LayoutSize == this.TBarSetting.ToolbarSize);
            if (isValidSize && (graphics == null && this._LayoutValidDefault)) return;
            if (isValidSize && (graphics != null && this._LayoutValidGraphics)) return;
            this.PrepareLayout(graphics);
        }
        /// <summary>
        /// Zneplatní layout Toolbaru
        /// </summary>
        protected void InvalidateLayout()
        {
            this._LayoutValidDefault = false;
            this._LayoutValidGraphics = false;
        }
        /// <summary>
        /// Nově připraví layout Toolbaru
        /// </summary>
        /// <param name="graphics"></param>
        protected void PrepareLayout(Graphics graphics)
        {
            if (graphics != null)
            {   // S reálnou grafikou
                this.PrepareLayoutRun(graphics);
                this._LayoutValidDefault = true;
                this._LayoutValidGraphics = true;
            }
            else
            {   // S náhradní grafikou (jde jen o měření velikosti textu v pixelech)
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
        /// <summary>
        /// Provede přípravu layoutu s dodanou grafikou
        /// </summary>
        /// <param name="graphics"></param>
        protected void PrepareLayoutRun(Graphics graphics)
        {
            this._Splitter.SplitterVisibleWidth = this.TBarSetting.SplitterHeight;
            int x = this.TBarSetting.ContentBounds.X;                // Prepare this.TBarSetting for current ToolbarSize, when current item is null or has not valid Size
            foreach (ToolBarGroup group in this._ToolbarGroupList)
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
            set { this.SetToolbarSize(value, ProcessAction.ChangeAll, EventSourceType.ValueChange | EventSourceType.ApplicationCode); ; }
        }
        /// <summary>
        /// Nastaví velikost toolbaru a zajistí návazné akce
        /// </summary>
        /// <param name="toolbarSize"></param>
        /// <param name="actions"></param>
        /// <param name="eventSource"></param>
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
        #region Status ToolBaru
        /// <summary>
        /// Aktuální živý stav ToolBaru, pro persistenci do příštího spuštění.
        /// Obsahuje názvy prvků Toolbaru a jejich persistované hodnoty.
        /// Lze číst i setovat. Setování vyvolá aplikační logiku daných prvků.
        /// </summary>
        public string CurrentStatus
        {
            get
            {
                return this._CurrentToolBarStatus.Snapshot;
            }
            set
            {
                if (value != null)
                {
                    try
                    {
                        this._SuppressCurrentStatusChanged = true;
                        string snapshot = value;
                        this._CurrentToolBarStatus.Snapshot = snapshot;
                        this._LastStatusSnapshot = snapshot;
                    }
                    finally
                    {
                        this._SuppressCurrentStatusChanged = false;
                    }
                }
            }
        }
        /// <summary>
        /// Aktuální nově vytvořená instance třídy <see cref="ToolBarStatus"/>, pro práci s persistencí hodnot prvků Toolbaru
        /// </summary>
        private ToolBarStatus _CurrentToolBarStatus
        {
            get
            {
                return ToolBarStatus.CreateFrom(this);
            }
        }
        /// <summary>
        /// Otisk posledně známého stavu, pro detekci změn.
        /// Otisk je získán z <see cref="ToolBarStatus.Snapshot"/>
        /// </summary>
        private string _LastStatusSnapshot;
        /// <summary>
        /// Pokud je nastaveno na true, pak se neprovede event <see cref="CurrentStatusChanged"/>
        /// </summary>
        private bool _SuppressCurrentStatusChanged;
        /// <summary>
        /// Metoda zjistí, zda v Toolbaru došlo ke změně stavu, a pokud ano tak vyvolá odpovídající událost.
        /// </summary>
        protected void TestCurrentStatusChanged()
        {
            if (this._SuppressCurrentStatusChanged) return;

            string oldSnapshot = this._LastStatusSnapshot;
            ToolBarStatus currentStatus = this._CurrentToolBarStatus;
            string newSnapshot = currentStatus.Snapshot;
            if (String.Equals(oldSnapshot, newSnapshot)) return;
            this._LastStatusSnapshot = newSnapshot;
            this.CallCurrentStatusChanged(newSnapshot);
        }
        /// <summary>
        /// Vyvolá háček OnCurrentStatusChanged a event CurrentStatusChanged
        /// </summary>
        protected void CallCurrentStatusChanged(string newSnapshot)
        {
            if (this._SuppressCurrentStatusChanged) return;

            GPropertyEventArgs<string> args = new GPropertyEventArgs<string>(newSnapshot, EventSourceType.InteractiveChanged);
            this.OnCurrentStatusChanged(args);
            if (this.CurrentStatusChanged != null)
                this.CurrentStatusChanged(this, args);
        }
        /// <summary>
        /// Je voláno při každé změně <see cref="CurrentStatus"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCurrentStatusChanged(GPropertyEventArgs<string> args) { }
        /// <summary>
        /// Událost volaná při každé změně <see cref="CurrentStatus"/>
        /// </summary>
        public event GPropertyEventHandler<string> CurrentStatusChanged;
        #endregion
        #region Public eventy
        /// <summary>
        /// Vyvolá metodu <see cref="OnToolbarSizeChanged(GPropertyChangeArgs{ComponentSize})"/> a event <see cref="ToolbarSizeChanged"/>, po změně hodnoty <see cref="ToolbarSize"/>
        /// </summary>
        protected void CallToolbarSizeChanged(ComponentSize oldValue, ComponentSize newValue, EventSourceType eventSource)
        {
            GPropertyChangeArgs<ComponentSize> args = new GPropertyChangeArgs<ComponentSize>(oldValue, newValue, eventSource);
            this.OnToolbarSizeChanged(args);
            if (!this.IsSuppressedEvent && this.ToolbarSizeChanged != null)
                this.ToolbarSizeChanged(this, args);
        }
        /// <summary>
        /// Háček volaný při změně hodnoty <see cref="ToolbarSize"/>
        /// </summary>
        protected virtual void OnToolbarSizeChanged(GPropertyChangeArgs<ComponentSize> args) { }
        /// <summary>
        /// Event volaný při změně hodnoty <see cref="ToolbarSize"/>
        /// </summary>
        public event GPropertyChangedHandler<ComponentSize> ToolbarSizeChanged;

        #endregion
        #region Layout of Toolbar - classes
        /// <summary>
        /// Layouts for whole Toolbar
        /// </summary>
        public class LayoutSettingTBarInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="toolbarSize"></param>
            public LayoutSettingTBarInfo(ComponentSize toolbarSize)
            {
                this.ToolbarSize = toolbarSize;
                this.ItemDict = new Dictionary<FunctionGlobalItemSize, LayoutSettingTItemInfo>();
                bool isNew = true;
                if (isNew)
                {
                    switch (toolbarSize)
                    {
                        case ComponentSize.Small:
                            this.HeightModule = 6;
                            this.PixelPerModule = 10;
                            this.ContentBounds = new Rectangle(2, 1, 1000, 60);
                            this.TitleBounds = new Rectangle(0, 64, 1000, 16);
                            this.ToolbarBounds = new Rectangle(0, 0, 1000, 82);
                            this.SplitterHeight = 2;
                            this.TitleZoom = 0.80f;
                            this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1, 10, 1,  8,  0, 0f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2, 20, 2, 16, 18, 0.50f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Half,  new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half,  3, 30, 3, 24, 20, 0.75f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4, 40, 3, 32, 22, 0.85f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 60, 4, 38, 22, 0.95f));
                            break;
                        case ComponentSize.Large:
                            this.HeightModule = 6;
                            this.PixelPerModule = 20;
                            this.ContentBounds = new Rectangle(1, 1, 1000, 120);
                            this.TitleBounds = new Rectangle(0, 124, 1000, 24);
                            this.ToolbarBounds = new Rectangle(0, 0, 1000, 150);
                            this.SplitterHeight = 2;
                            this.TitleZoom = 1.05f;
                            this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1,  20, 2, 16,  0, 0f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2,  40, 3, 32, 22, 0.95f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Half,  new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half,  3,  60, 4, 48, 24, 1.05f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4,  80, 4, 72, 26, 1.10f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 120, 6, 96, 26, 1.15f));
                            break;
                        case ComponentSize.Medium:
                        default:
                            this.HeightModule = 6;
                            this.PixelPerModule = 15;
                            this.ContentBounds = new Rectangle(1, 1, 1000, 90);
                            this.TitleBounds = new Rectangle(0, 94, 1000, 20);
                            this.ToolbarBounds = new Rectangle(0, 0, 1000, 116);
                            this.SplitterHeight = 2;
                            this.TitleZoom = 1.00f;
                            this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1, 15, 2, 12,  0, 0f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2, 30, 3, 24, 24, 0.85f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Half,  new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half,  3, 45, 4, 36, 24, 0.95f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4, 60, 6, 48, 26, 1.00f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 90, 6, 64, 26, 1.10f));
                            break;
                    }
                }
                else
                {   // Dřívější layout
                    switch (toolbarSize)
                    {
                        case ComponentSize.Small:
                            this.HeightModule = 6;
                            this.PixelPerModule = 10;
                            this.ContentBounds = new Rectangle(2, 1, 1000, 60);
                            this.TitleBounds = new Rectangle(0, 64, 1000, 16);
                            this.ToolbarBounds = new Rectangle(0, 0, 1000, 80);
                            this.SplitterHeight = 2;
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
                            this.SplitterHeight = 2;
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
                            this.SplitterHeight = 2;
                            this.TitleZoom = 1.00f;
                            this.ItemDict.Add(FunctionGlobalItemSize.Micro, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Micro, 1, 15, 1, 12, 0, 0f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Small, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Small, 2, 30, 1, 24, 24, 0.85f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Half, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Half, 3, 45, 1, 42, 24, 0.95f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Large, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Large, 4, 60, 2, 56, 26, 1.00f));
                            this.ItemDict.Add(FunctionGlobalItemSize.Whole, new LayoutSettingTItemInfo(this, FunctionGlobalItemSize.Whole, 6, 90, 2, 64, 26, 1.10f));
                            break;
                    }
                }
            }
            private Dictionary<FunctionGlobalItemSize, LayoutSettingTItemInfo> ItemDict;
            /// <summary>
            /// Velikost toolbaru
            /// </summary>
            public ComponentSize ToolbarSize { get; private set; }
            /// <summary>
            /// Výška toolbaru v počtu modulů
            /// </summary>
            public int HeightModule { get; private set; }
            /// <summary>
            /// Počet pixelů v jednom modulu
            /// </summary>
            public int PixelPerModule { get; private set; }
            /// <summary>
            /// Souřadnice toolbaru
            /// </summary>
            public Rectangle ToolbarBounds { get; private set; }
            /// <summary>
            /// Souřadnice prostoru Content, ale pouze na ose Y.
            /// Souřadnice na ose X jsou převzaty z grupy.
            /// </summary>
            public Rectangle ContentBounds { get; private set; }
            /// <summary>
            /// Souřadnice prostoru Title, ale pouze na ose Y.
            /// Souřadnice na ose X jsou převzaty z grupy.
            /// </summary>
            public Rectangle TitleBounds { get; private set; }
            /// <summary>
            /// Výška Splitteru
            /// </summary>
            public int SplitterHeight { get; private set; }
            /// <summary>
            /// Zoom pro titulky
            /// </summary>
            public float TitleZoom { get; private set; }
            /// <summary>
            /// Zoom pro font
            /// </summary>
            public FontInfo TitleFont { get { return FontInfo.CaptionSmall.GetZoom(this.TitleZoom); } }
            /// <summary>
            /// Vlastnosti layoutu pro Size = Micro
            /// </summary>
            public LayoutSettingTItemInfo ItemMicroInfo { get { return this.ItemDict[FunctionGlobalItemSize.Micro]; } }
            /// <summary>
            /// Vlastnosti layoutu pro Size = Small
            /// </summary>
            public LayoutSettingTItemInfo ItemSmallInfo { get { return this.ItemDict[FunctionGlobalItemSize.Small]; } }
            /// <summary>
            /// Vlastnosti layoutu pro Size = Half
            /// </summary>
            public LayoutSettingTItemInfo ItemHalfInfo { get { return this.ItemDict[FunctionGlobalItemSize.Half]; } }
            /// <summary>
            /// Vlastnosti layoutu pro Size = Large
            /// </summary>
            public LayoutSettingTItemInfo ItemLargeInfo { get { return this.ItemDict[FunctionGlobalItemSize.Large]; } }
            /// <summary>
            /// Vlastnosti layoutu pro Size = Whole
            /// </summary>
            public LayoutSettingTItemInfo ItemWholeInfo { get { return this.ItemDict[FunctionGlobalItemSize.Whole]; } }

            /// <summary>
            /// Returns LayoutSettingTItemInfo for item of specified Size, on current toolbar
            /// </summary>
            /// <param name="itemSize"></param>
            /// <returns></returns>
            public LayoutSettingTItemInfo GetInfoForSize(FunctionGlobalItemSize itemSize)
            {
                LayoutSettingTItemInfo info;
                if (this.ItemDict.TryGetValue(itemSize, out info)) return info;
                return this.ItemDict[FunctionGlobalItemSize.Small];
            }
            /// <summary>
            /// Returns Size for Image of specified itemSize
            /// </summary>
            /// <param name="itemSize"></param>
            /// <returns></returns>
            public Size GetIconSize(FunctionGlobalItemSize itemSize)
            {
                LayoutSettingTItemInfo info = this.GetInfoForSize(itemSize);
                return info.ImageSize;
            }
            /// <summary>
            /// Vrací <see cref="FontInfo"/> pro danou velikost prvku a daný font (=upraví velikost)
            /// </summary>
            /// <param name="itemSize"></param>
            /// <param name="fontInfo"></param>
            /// <returns></returns>
            public FontInfo GetFontInfo(FunctionGlobalItemSize itemSize, FontInfo fontInfo)
            {
                FontInfo fontCurr = (fontInfo != null ? fontInfo : FontInfo.Menu).Clone;
                float zoom = this.GetInfoForSize(itemSize).TextZoom;
                fontCurr.ApplyZoom(zoom);
                return fontCurr;
            }
            /// <summary>
            /// Vrací počet modulů pro daný počet pixelů (=zarovnaný nahoru)
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
            /// Vrací velikost v pixelech na základě velikosti v modulech
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
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="size"></param>
            /// <param name="moduleCount"></param>
            /// <param name="modulePixel"></param>
            /// <param name="offsetPixel"></param>
            /// <param name="imagePixel"></param>
            /// <param name="textHeight"></param>
            /// <param name="textZoom"></param>
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
            /// Počet modulů v této velikosti (1 - 6)
            /// </summary>
            public int ModuleCount { get { return _ModuleCount; } }
            /// <summary>
            /// Počet pixelů na jeden modul v této velikosti (Micro má 10 px)
            /// </summary>
            public int ModulePixel { get { return _ModulePixel; } }
            /// <summary>
            /// Mezera mezi vnitřními bloky v prvky, v pixelech = typicky odsazení textu od Image, atd.
            /// </summary>
            public int OffsetPixel { get { return _OffsetPixel; } }
            /// <summary>
            /// Velikost Image v pixelech v této velikosti, tak aby okolo UÍmage byl přiměřený okraj 
            /// a aby Image měla rozumnou velikost v pixelech (například 16 pixelů je rozumější než 17 pixelů).
            /// </summary>
            public Size ImageSize { get { return new Size(_ImagePixel, _ImagePixel); } }
            /// <summary>
            /// Maximální výška textu v pixelech
            /// </summary>
            public int TextHeight { get { return _TextHeight; } }
            /// <summary>
            /// Zoom pro text v této velikosti
            /// </summary>
            public float TextZoom { get { return _TextZoom; } }
        }
        #endregion
        #region InteractiveContainer : Interactivity, Draw
        /// <summary>
        /// Vizuální potomci
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { this.CheckLayout(null); return this.GetAllItems(); } }
        /// <summary>
        /// Vrátí všechny potomky
        /// </summary>
        /// <returns></returns>
        protected List<IInteractiveItem> GetAllItems()
        {
            List<IInteractiveItem> items = new List<IInteractiveItem>(base.Childs);
            items.Add(_Splitter);
            return items;
        }
        /// <summary>
        /// Provede řízený refresh layoutu a controlu, podle požadavku
        /// </summary>
        /// <param name="refreshMode">Co je třeba refreshovat</param>
        public void Refresh(ToolBarRefreshMode refreshMode)
        {
            bool needRefresh = false;
            if (refreshMode.HasFlag(ToolBarRefreshMode.RefreshLayout))
            {
                this.InvalidateLayout();
                needRefresh = true;
            }

            if (needRefresh || refreshMode.HasFlag(ToolBarRefreshMode.RefreshControl))
            {
                base.Refresh();
            }
        }
        /// <summary>
        /// Vykreslí Toolbar
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.CheckLayout(e.Graphics);
            this._SplitterCheckValue();
            this.DrawToolbarBackground(e.Graphics, absoluteBounds, null);
        }
        /// <summary>
        /// Draw background for toolbar. Can be called from GToolbarGroup.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="boundsDraw"></param>
        internal void DrawToolbarBackground(Graphics graphics, Rectangle absoluteBounds, Rectangle? boundsDraw)
        {
            Rectangle toolbarBounds = absoluteBounds;
            if (boundsDraw.HasValue)
            {
                toolbarBounds.X = boundsDraw.Value.X;
                toolbarBounds.Width = boundsDraw.Value.Width;
            }
            GPainter.DrawAreaBase(graphics, toolbarBounds, Skin.ToolBar.BackColor, System.Windows.Forms.Orientation.Horizontal, this.InteractiveState /* GInteractiveState.Enabled : MouseOver */, null, null);
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek po změně <see cref="FunctionItem.IsChecked"/>, úkolem je vyvolat event <see cref="ToolBar.ItemCheckedChange"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="activeItem"></param>
        internal void OnItemCheckedChange(FunctionGlobalGroup dataGroup, FunctionItem activeItem)
        {
            if (this.ItemCheckedChange != null)
                this.ItemCheckedChange(this, new FunctionItemEventArgs(activeItem));
            this.TestCurrentStatusChanged();
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek po kliknutí na něj, úkolem je vyvolat event <see cref="ToolBar.ItemClicked"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="activeItem"></param>
        internal void OnItemClicked(FunctionGlobalGroup dataGroup, FunctionItem activeItem)
        {
            if (this.ItemClicked != null)
                this.ItemClicked(this, new FunctionItemEventArgs(activeItem));
            this.TestCurrentStatusChanged();
        }
        /// <summary>
        /// Tuto metodu volá interaktivní prvek před načtením SubItems do itemu, úkolem je vyvolat event <see cref="ToolBar.ItemSubItemsEnumerateBefore"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="activeItem"></param>
        internal void OnItemSubItemsEnumerateBefore(FunctionGlobalGroup dataGroup, FunctionItem activeItem)
        {
            if (this.ItemSubItemsEnumerateBefore != null)
                this.ItemSubItemsEnumerateBefore(this, new FunctionItemEventArgs(activeItem));
        }
        /// <summary>
        /// Událost vyvolaná po změně <see cref="FunctionItem.IsChecked"/>
        /// </summary>
        public event FunctionItemEventHandler ItemCheckedChange;
        /// <summary>
        /// Událost vyvolaná po kliknutí na určitý prvek ToolBaru
        /// </summary>
        public event FunctionItemEventHandler ItemClicked;
        /// <summary>
        /// Událost vyvolaná před rozbalením prvku, který může mít SubItems. Aplikaci nyní může zjistit, zda jsou aktuální.
        /// </summary>
        public event FunctionItemEventHandler ItemSubItemsEnumerateBefore;
        #endregion
    }
    #endregion
    #region class ToolBarGroup : Jedna vizuální skupina prvků v toolbaru
    /// <summary>
    /// <see cref="ToolBarGroup"/> : Jedna vizuální skupina prvků v toolbaru
    /// </summary>
    internal class ToolBarGroup : InteractiveContainer
    {
        #region Constructor, basic properties
        internal static ToolBarGroup CreateFrom(ToolBar owner, FunctionGlobalGroup dataGroup)
        {
            ToolBarGroup group = new ToolBarGroup(owner, dataGroup);
            
            if (dataGroup.Items != null)
            {
                foreach (FunctionGlobalItem item in dataGroup.Items)
                    group._AddGToolbarItem(ToolBarItem.CreateFrom(group, item));
            }
            group._AddFinalSeparator();

            return group;
        }
        private ToolBarGroup(ToolBar toolbar, FunctionGlobalGroup dataGroup)
        {
            this._Toolbar = toolbar;
            this._DataGroup = dataGroup;
            this._ToolbarItemList = new List<ToolBarItem>();
        }
        /// <summary>
        /// When in this.Items are last item other than Separator, then add new Separator at end.
        /// </summary>
        private void _AddFinalSeparator()
        {
            if (this._ToolbarItemList.Count > 0 && this._ToolbarItemList[this._ToolbarItemList.Count - 1].ItemType != FunctionGlobalItemType.Separator)
                _AddGToolbarItem(ToolBarItem.CreateSeparator(this));
        }
        /// <summary>
        /// Add one GToolbarGroup into Childs and into _ToolbarItemList.
        /// </summary>
        /// <param name="item"></param>
        private void _AddGToolbarItem(ToolBarItem item)
        {
            this.AddItem(item);
            this._ToolbarItemList.Add(item);
        }
        private ToolBar _Toolbar;
        private FunctionGlobalGroup _DataGroup;
        /// <summary>
        /// All interactive items in this group
        /// </summary>
        private List<ToolBarItem> _ToolbarItemList;
        /// <summary>
        /// Provider of functions in this group = Service of type IFunctionGlobal.
        /// Může být null!
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
        /// Pole grafických položek
        /// </summary>
        internal ToolBarItem[] FunctionGItems { get { return this._ToolbarItemList.ToArray(); } }
        /// <summary>
        /// Toolbar in which is this group located.
        /// </summary>
        internal ToolBar Toolbar { get { return this._Toolbar; } }
        /// <summary>
        /// Setting for layout on current toolbar (by ToolbarSize)
        /// </summary>
        internal ToolBar.LayoutSettingTBarInfo TBarSetting { get { return this._Toolbar.TBarSetting; } }
        /// <summary>
        /// Return index of specified item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private int _SearchItemIndex(ToolBarItem item)
        {
            if (item == null) return -1;
            return this._ToolbarItemList.IndexOf(item);
        }
        #endregion
        #region Data properties
        /// <summary>
        /// Libovolný název tohoto controlu. Není povinné jej zadávat. Nemusí být jednoznačný. Nemá žádná pravidla co do obsahu.
        /// Je na aplikaci, jak jej naplní a jak jej bude vyhodnocovat.
        /// </summary>
        public override string Name { get { return this._DataGroup.Title.Text; } set { } }
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
        /// <param name="x"></param>
        internal void PrepareLayout(Graphics graphics, ref int x)
        {
            // 1. Only Visible items:
            ToolBarItem[] items = this._ToolbarItemList.Where(t => t.Is.Visible).ToArray();

            // 2. prepare ModuleSize and PixelSize:
            foreach (ToolBarItem item in items)
                item.PrepareLayout(graphics);

            // 3. Arrange items to layout:
            int tableX = this.TBarSetting.ContentBounds.X;
            List<ILayoutItem> layoutItemList = new List<ILayoutItem>(items);
            LayoutEngineArgs layoutArgs = this.PrepareLayoutArgs();
            while (true)
            {   // One call LayoutEngine.CreateLayout() create one "table" (visual group) from items:
                int count = LayoutEngine.CreateLayout(layoutItemList, layoutArgs);
                if (count <= 0) break;
                this.PrepareLayoutOne(layoutArgs, ref tableX);
                layoutArgs.PrepareNextProcess(false, false);
            }

            // 4. Group Bounds:
            var tBarSetting = this.TBarSetting;
            int groupX = x;
            int groupY = tBarSetting.ToolbarBounds.Y;
            int groupW = tableX;
            int groupH = tBarSetting.ToolbarBounds.Height - tBarSetting.SplitterHeight;
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
            ToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
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
                        ToolBarItem item = layoutItem as ToolBarItem;
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
                        ToolBarItem item = layoutItem as ToolBarItem;
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
        /// <summary>
        /// Vykreslí grupu
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.Toolbar.DrawToolbarBackground(e.Graphics, absoluteBounds, absoluteBounds);    // Nebudeme volat bázovou metodu, ta stejně kreslí jen pozadí.
            this.DrawGroupTitle(e, absoluteBounds);
        }
        internal void DrawGroupTitle(GInteractiveDrawArgs e, Rectangle absoluteBounds)
        {
            ToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
            Rectangle tb = tBarSetting.TitleBounds;

            Rectangle toolb = tBarSetting.ToolbarBounds;

            int dy = absoluteBounds.Y - this.Bounds.Y;
            Rectangle titleAbsoluteBounds = new Rectangle(absoluteBounds.X + 1, dy + tb.Y, absoluteBounds.Width - 3, tb.Height);

            if (this.IsMouseActive)
                GPainter.DrawAreaBase(e.Graphics, titleAbsoluteBounds, Skin.ToolBar.TitleBackColor, System.Windows.Forms.Orientation.Horizontal, GInteractiveState.Enabled, null, null);

            if (!String.IsNullOrEmpty(this.DataTitle))
                GPainter.DrawString(e.Graphics, this.DataTitle, tBarSetting.TitleFont, titleAbsoluteBounds, ContentAlignment.MiddleCenter, Skin.ToolBar.TextColor);
        }
        /// <summary>
        /// Metoda vrací všechny členy selectovací skupiny (<see cref="FunctionItem.CheckedGroupName"/>) daného jména.
        /// Pokud je na vstupu prázdné jméno skupiny, vrací se null.
        /// Pokud je jméno zadané, ale ve skupině by nebyl žádný prvek, vrací se prázdný seznam.
        /// Do výběru se dostávají i prvky, které mají <see cref="FunctionItem.IsCheckable"/> = false.
        /// Na předvybrané členy skupiny se aplikuje i filtr dle parametru itemFilter, pokud je zadán.
        /// </summary>
        /// <param name="selectionGroupName"></param>
        /// <param name="itemFilter"></param>
        /// <returns></returns>
        internal List<FunctionGlobalItem> GetOptionGroup(string selectionGroupName, Func<FunctionGlobalItem, bool> itemFilter)
        {
            if (String.IsNullOrEmpty(selectionGroupName)) return null;
            List<FunctionGlobalItem> result = new List<FunctionGlobalItem>();
            foreach (var tItem in this._ToolbarItemList)
            {
                FunctionGlobalItem item = tItem.DataItem;
                if (String.IsNullOrEmpty(item.CheckedGroupName) || !String.Equals(item.CheckedGroupName, selectionGroupName, StringComparison.InvariantCulture)) continue;
                if ((itemFilter != null) && !itemFilter(item)) continue;
                result.Add(item);
            }
            return result;
        }
        #endregion
    }
    #endregion
    #region class ToolBarItem a konkrétní potomci pro konkrétní typy
    /// <summary>
    /// Konkrétní položka Toolbaru: Separator
    /// </summary>
    internal class ToolBarSeparator : ToolBarItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        internal ToolBarSeparator(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base(toolbarGroup, dataItem) { }
        internal override void PrepareLayout(Graphics graphics)
        {
            this.PixelSizeMin = new Size(5, this.TBarSetting.ContentBounds.Height);
            this.ModuleSize = new Size(0, this.TBarSetting.HeightModule);
        }
        protected override void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            e.Graphics.DrawLine(Skin.Pen(Skin.ToolBar.SeparatorDarkColor), absoluteBounds.X + 3, absoluteBounds.Y, absoluteBounds.X + 3, absoluteBounds.Bottom);
            e.Graphics.DrawLine(Skin.Pen(Skin.ToolBar.SeparatorLightColor), absoluteBounds.X + 4, absoluteBounds.Y, absoluteBounds.X + 4, absoluteBounds.Bottom);
        }
    }
    /// <summary>
    /// Konkrétní položka Toolbaru: Label
    /// </summary>
    internal class ToolBarLabel : ToolBarItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        internal ToolBarLabel(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base(toolbarGroup, dataItem) { }
        internal override void PrepareLayout(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics);
        }
        protected override void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.DrawItemImage(e, absoluteBounds);
            this.DrawItemText(e, absoluteBounds);
            /*
            Image image = this._DataItem.Image;
            Image imageHot = null;
            if (image != null)
            {
                Rectangle boundsImageAbsolute = this.BoundsImage.ShiftBy(absoluteBounds.Location);
                if (this.InteractiveState.IsMouseActive())
                    imageHot = this._DataItem.ImageHot;
                e.Graphics.DrawImage(imageHot ?? image, boundsImageAbsolute);
            }

            string text = this.ItemText;
            if (!String.IsNullOrEmpty(text))
            {
                Rectangle boundsTextAbsolute = this.BoundsText.ShiftBy(absoluteBounds.Location);
                FontInfo fontInfo = this.CurrentItemFont;
                GPainter.DrawString(e.Graphics, boundsTextAbsolute, text, Skin.ToolBar.TextColor, fontInfo, ContentAlignment.MiddleCenter);
            }
            */
        }
    }
    /// <summary>
    /// Konkrétní položka Toolbaru: Image
    /// </summary>
    internal class ToolBarImage : ToolBarItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        internal ToolBarImage(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base(toolbarGroup, dataItem) { }
        internal override void PrepareLayout(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics);
        }
        protected override void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.DrawItemImage(e, absoluteBounds);
            this.DrawItemText(e, absoluteBounds);
            /*
            if (this._DataItem.Image != null)
            {
                Rectangle boundsImageAbsolute = this.BoundsImage.ShiftBy(absoluteBounds.Location);
                e.Graphics.DrawImage(this._DataItem.Image, boundsImageAbsolute);
            }

            string text = this.ItemText;
            if (!String.IsNullOrEmpty(text))
            {
                Rectangle boundsTextAbsolute = this.BoundsText.ShiftBy(absoluteBounds.Location);
                FontInfo fontInfo = this.CurrentItemFont;
                GPainter.DrawString(e.Graphics, boundsTextAbsolute, text, Skin.ToolBar.TextColor, fontInfo, ContentAlignment.MiddleCenter);
            }
            */
        }
    }
    /// <summary>
    /// Konkrétní položka Toolbaru: Button
    /// </summary>
    internal class ToolBarButton : ToolBarItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        internal ToolBarButton(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base(toolbarGroup, dataItem)
        {
            this.Is.GetChecked = this._GetChecked;
            this.Is.SetChecked = this._SetChecked;
        }
        /// <summary>
        /// Vrací reálnou hodnotu IsChecked, z navázaného prvku <see cref="FunctionItem.IsChecked"/>.
        /// </summary>
        private bool _GetChecked(bool value)
        {
            return (this._DataItem.IsCheckable && this._DataItem.IsChecked);
        }
        /// <summary>
        /// Reálně uloží danou hodnotu do navázaného prvku <see cref="FunctionItem.IsChecked"/>.
        /// </summary>
        /// <param name="value"></param>
        private void _SetChecked(bool value)
        {
            if (this._DataItem.IsCheckable) this._DataItem.IsChecked = value;
        }
        internal override void PrepareLayout(Graphics graphics)
        {
            this.PrepareBoundsCommon(graphics);
        }
        protected override void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.DrawItemBackground(e, absoluteBounds, this.Is.Checked);
            this.DrawItemImage(e, absoluteBounds);
            this.DrawItemText(e, absoluteBounds);
            this.DrawItemSelection(e, absoluteBounds, this.Is.Checked);
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.ExecLeftClick();
        }
        protected void ExecLeftClick()
        {
            if (this._DataItem.IsCheckable)
                this.IsCheckedChange();
            this.CallItemClick();
        }
        /// <summary>
        /// Metoda zajistí změnu stavu <see cref="InteractiveProperties.Checked"/> pro tento prvek, 
        /// a pokud tento prvek je členem nějaké selectovací grupy, pak najde i ostatní členy grupy a vyřeší to i pro ně.
        /// Metoda je volána před voláním <see cref="ToolBarItem.CallItemClick()"/>, 
        /// protože event Click má být volán už se správnou hodnotou <see cref="FunctionItem.IsChecked"/>.
        /// </summary>
        protected void IsCheckedChange()
        {
            this.IsCheckedChange(!this.Is.Checked);
        }
        /// <summary>
        /// Metoda zajistí nastavení stavu <see cref="InteractiveProperties.Checked"/> pro tento prvek, 
        /// a pokud tento prvek je členem nějaké selectovací grupy, pak najde i ostatní členy grupy a vyřeší to i pro ně.
        /// Metoda je volána před voláním <see cref="ToolBarItem.CallItemClick()"/>, 
        /// protože event Click má být volán už se správnou hodnotou <see cref="FunctionItem.IsChecked"/>.
        /// </summary>
        protected void IsCheckedChange(bool newIsChecked)
        {
            List<FunctionGlobalItem> itemGroup = this._ToolbarGroup.GetOptionGroup(this.CheckedGroupName, i => (i.ItemType == FunctionGlobalItemType.Button));
            if (itemGroup == null)
            {   // null = není zadána grupa => v tom případě this button je CheckBox:
                this.Is.Checked = newIsChecked;
                this.CallItemCheckedChange(this.DataItem);
            }
            else
            {   // máme grupu => v tom případě this button je OptionButton:
                FunctionGlobalItem currentItem = this.DataItem;
                // Projdeme všechny prvky grupy, pro prvky kromě this nastavíme IsChecked = false, 
                // na konci pro this prvek nastavíme IsChecked = true, přiměřeně voláme event ItemSelectedChange:
                foreach (FunctionGlobalItem item in itemGroup)
                {
                    if (!Object.ReferenceEquals(item, currentItem))
                        // Toto je jiný prvek, než this => ten má mít IsChecked = false:
                        this.IsCheckedSetValue(item, false);
                }
                // Náš prvek má mít IsChecked = true:
                this.IsCheckedSetValue(currentItem, true);
            }
        }
        /// <summary>
        /// Zajistí, že daný prvek bude mít hodnotu <see cref="FunctionItem.IsChecked"/> = daná hodnota.
        /// Pokud prvek tuto hodnotu nemá, bude do něj vložena a vyvolá se patřičný event.
        /// Pokud hodnotu již má, nic nemění a nedělá.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        protected void IsCheckedSetValue(FunctionGlobalItem item, bool value)
        {
            if (item.IsChecked != value)
            {
                item.IsChecked = value;
                this.CallItemCheckedChange(item);
            }
        }
        /// <summary>
        /// Hodnota, která je pro tento prvek persistována. Pokud je zde null, pak persistence není.
        /// Potomek může tuto property přepsat, a konvertovat string na svoji hodnotu, pak bude persistována do příštího spuštění aplikace.
        /// </summary>
        public override string PersistValue
        {
            get
            {
                if (!this._DataItem.IsCheckable) return null;

                bool isChecked = this.Is.Checked;
                return (isChecked ? "Checked" : "UnChecked");
            }
            set
            {
                if (!this._DataItem.IsCheckable) return;
                if (value == null) return;
                bool isChecked = (value == "Checked");
                this.PersistedValueApply(this.Is.Checked, isChecked);
            }
        }
        /// <summary>
        /// Metoda řídí načtení persistované IsChecked do this objektu.
        /// </summary>
        /// <param name="oldChecked"></param>
        /// <param name="newChecked"></param>
        protected void PersistedValueApply(bool oldChecked, bool newChecked)
        {
            //  Z hlediska praktického použití jsou dva scénáře:
            // 1. this button je prostý CheckBox (tj. není v žádné skupině, která by tvořila RadioButton):
            //    - Pak vyvoláme patřičnou akci CallItemClick() i tehdy, když nejde o změnu hodnoty, protože může být navázaná akce, která musí být provedena vždy (typicky nastavení IsVisible pro controly)
            // 2. this button je RadioButton (je členem skupiny):
            //    - Pak vyvoláme akci CallItemClick() pouze pokud newChecked je true:
            bool isCheckBox = String.IsNullOrEmpty(this.CheckedGroupName);
            bool isChanged = (newChecked != oldChecked);
            if (isCheckBox || newChecked)
            {   // This prvek je CheckBox, anebo (pro RadioButton) pokud nová hodnota je true:
                this.IsCheckedChange(newChecked);
                this.CallItemClick();
            }
        }
        /// <summary>
        /// Provedde stejnou akci, jako když uživatel klikne na prvek
        /// </summary>
        public override void DoClick()
        {
            this.ExecLeftClick();
            this.Repaint();
        }
        /// <summary>
        /// Obsahuje název skupiny prvků, které se vzájemně chovají jako OptionGroup.
        /// To znamená, že právě jeden z prvků skupiny může být IsChecked = být označen jako aktivní.
        /// <para/>
        /// Chování:
        /// <para/>
        /// a) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako CheckBox: změna jeho hodnoty IsChecked neovlivní žádný jiný prvek.
        /// Kliknutí na takový prvek mění hodnotu IsChecked z false na true a naopak = lze jej shodit na false.
        /// <para/>
        /// b) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako RadioButton: kliknutí na neoznačený button jej označí a současně odznačí ostatní buttony v grupě.
        /// Opakované kliknutí na označený button jej neodznačí.
        /// Prvky jedné grupy <see cref="CheckedGroupName"/> se musí nacházet v jedné grafické skupině "GroupName" (platí pro Toolbar).
        /// Pokud by byly umístěny v jiné grupě, nebudou považovány za jednu skupinu, ale více oddělených skupin.
        /// Naproti tomu jedna grafická grupa "GroupName" může obsahovat více skupin <see cref="CheckedGroupName"/>.
        /// <para/>
        /// Je rozumné dávat prvky jedné <see cref="CheckedGroupName"/> blízko k sobě, ale technicky nutné to není.
        /// </summary>
        public string CheckedGroupName { get { return this._DataItem?.CheckedGroupName; } }

    }
    /// <summary>
    /// Konkrétní položka Toolbaru: ComboBox
    /// </summary>
    internal class ToolBarComboBox : ToolBarItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        internal ToolBarComboBox(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base(toolbarGroup, dataItem) { }
        internal override void PrepareLayout(Graphics graphics)
        {
            EList<FunctionItem> subItems = this.ItemSubItems;
            this.PrepareBoundsCommon(graphics,
                g => this.GetImageSizeFromSubItems(true, subItems),
                g => this.GetTextSizeFromSubItems(g, true, subItems, 7));

        }
        protected override void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.DrawItemBackground(e, absoluteBounds, isSelected: false, roundCorner: 1, forceBorder: true);
            FunctionItem activeItem = this._DataItem.Value as FunctionItem;
            if (activeItem == null) activeItem = this._DataItem;
            this.DrawItemImage(e, absoluteBounds, activeItem);
            this.DrawItemText(e, absoluteBounds, activeItem);
            this.DrawItemIcon(e, absoluteBounds);
        }
        protected override string CurrentToolTip
        {
            get
            {
                FunctionItem activeItem = this._DataItem.Value as FunctionItem;
                return (activeItem != null ? activeItem.ToolTipText : this.ItemToolTip);
            }
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
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
            this.CallItemClick(item);
            this.Parent.Repaint();
        }
    }
    /// <summary>
    /// Konkrétní položka Toolbaru: TrackBar
    /// </summary>
    internal class ToolBarTrackBar : ToolBarItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        internal ToolBarTrackBar(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base(toolbarGroup, dataItem)
        {
            this._TrackBar = new TrackBar(this);
            this.ChildList.Add(this._TrackBar);
        }
        /// <summary>
        /// Připraví layout pro tento trackbar
        /// </summary>
        /// <param name="graphics"></param>
        internal override void PrepareLayout(Graphics graphics)
        {
            // this.PrepareBoundsCommon(graphics);


            ToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
            ToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;

            int h = itemSetting.ModulePixel;
            int s = itemSetting.OffsetPixel;
            int c = (h / 2);

            int w = 100;
            int modulesWidth = tBarSetting.GetModuleCount(w + 4);
            int modulesHeight = this.TBarSetting.HeightModule;
            this.ModuleSize = new Size(modulesWidth, modulesHeight);
            this.PixelSizeMin = tBarSetting.GetPixelSize(this.ModuleSize);

        }
        /// <summary>
        /// Vykreslí tento trackbar
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this.DrawItemBackground(e, absoluteBounds);
            this.DrawItemImage(e, absoluteBounds);
            this.DrawItemText(e, absoluteBounds);
        }
        private TrackBar _TrackBar;
        protected Rectangle ImageBeforeBounds { get; set; }
        protected Rectangle TrackBarBounds { get; set; }
        protected Rectangle ImageAfterBounds { get; set; }
    }
    
    /// <summary>
    /// Bázová abstraktní třída pro konkrétní položky Toolbaru
    /// </summary>
    internal abstract class ToolBarItem : InteractiveContainer, ILayoutItem
    {
        #region Konstruktor, základní properties
        /// <summary>
        /// Konstruktor abstraktní třídy je protected, používají ho potomci
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        protected ToolBarItem(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem) : base()
        {
            this._ToolbarGroup = toolbarGroup;
            this._DataItem = dataItem;
            this.Is.GetVisible = this._GetVisible;
            this.Is.SetVisible = this._SetVisible;
            this.Is.GetEnabled = this._GetEnabled;
            this.Is.SetEnabled = this._SetEnabled;
            this.Is.MouseDoubleClick = false;        // Toolbar není zvědav na doubleclicky!
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this._DataItem != null)
                return this.ItemSize.ToString() + " " + this.ItemType.ToString() + ": " + this.ItemText;
            return this.ItemSize.ToString() + " " + this.ItemType.ToString();
        }
        /// <summary>
        /// Factory creator
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        internal static ToolBarItem CreateFrom(ToolBarGroup toolbarGroup, FunctionGlobalItem dataItem)
        {
            switch (dataItem.ItemType)
            {
                case FunctionGlobalItemType.None: return null;
                case FunctionGlobalItemType.Separator: return new ToolBarSeparator(toolbarGroup, dataItem);
                case FunctionGlobalItemType.Label: return new ToolBarLabel(toolbarGroup, dataItem);
                case FunctionGlobalItemType.Image: return new ToolBarImage(toolbarGroup, dataItem);
                case FunctionGlobalItemType.Button: return new ToolBarButton(toolbarGroup, dataItem);
                case FunctionGlobalItemType.ComboBox: return new ToolBarComboBox(toolbarGroup, dataItem);
                case FunctionGlobalItemType.TrackBar: return new ToolBarTrackBar(toolbarGroup, dataItem);
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí separátor = oddělovač
        /// </summary>
        /// <param name="toolbarGroup"></param>
        /// <returns></returns>
        internal static ToolBarItem CreateSeparator(ToolBarGroup toolbarGroup)
        {
            FunctionGlobalItem dataItem = new FunctionGlobalItem(toolbarGroup.Provider)
            {
                ItemType = FunctionGlobalItemType.Separator,
                Size = FunctionGlobalItemSize.Whole
            };
            return new ToolBarSeparator(toolbarGroup, dataItem);
        }
        /// <summary>
        /// Grupa, do níž patří tento prvek
        /// </summary>
        protected ToolBarGroup _ToolbarGroup;
        /// <summary>
        /// Datový základ tohoto prvku
        /// </summary>
        protected FunctionGlobalItem _DataItem;
        /// <summary>
        /// Poskytovatel funkcí v této grupě = služba typu IFunctionGlobal
        /// </summary>
        public IFunctionProvider Provider { get { return this._DataItem.Provider; } }
        /// <summary>
        /// Typ konkrétního vlastníka této grupy = služba typu IFunctionGlobal
        /// </summary>
        public Type ProviderType { get { return this._DataItem.ProviderType; } }
        /// <summary>
        /// Toolbar, do kterého náleží tato grupa
        /// </summary>
        internal ToolBar Toolbar { get { return this._ToolbarGroup.Toolbar; } }
        /// <summary>
        /// Setting pro celý Toolbar
        /// </summary>
        internal ToolBar.LayoutSettingTBarInfo TBarSetting { get { return this.Toolbar.TBarSetting; } }
        /// <summary>
        /// Setting pro tento prvek
        /// </summary>
        internal ToolBar.LayoutSettingTItemInfo TItemSetting { get { return this.Toolbar.TBarSetting.GetInfoForSize(this.ItemSize); } }
        #endregion
        #region Data from DataItem
        /// <summary>
        /// Jméno tohoto prvku, prostor pro aplikační identifikátor položky
        /// </summary>
        public override string Name { get { return this._DataItem.Name; } set { } }
        /// <summary>
        /// Vlastní datový prvek, nad kterým je postaven tento prvek Toolbaru
        /// </summary>
        public FunctionGlobalItem DataItem { get { return this._DataItem; } }
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
        /// <summary>
        /// Ikonka pro stav MouseActive
        /// </summary>
        protected Image ItemImageHot { get { return this._DataItem.ImageHot; } }
        public FontInfo CurrentItemFont { get { return this.TBarSetting.GetFontInfo(this.ItemSize, this._DataItem.Font); } }
        /// <summary>
        /// Text pro tento prvek.
        /// Pokud velikost prvku je Micro, pak Text je prázdný.
        /// </summary>
        public string ItemText { get { return (this._DataItem.Size == FunctionGlobalItemSize.Micro ? "" : this._DataItem.TextText); } }
        /// <summary>
        /// ToolTip pro tento prvek.
        /// </summary>
        public string ItemToolTip { get { return this._DataItem.ToolTipText; } }
        /// <summary>
        /// Je prvek viditelný?
        /// </summary>
        private bool _GetVisible(bool value) { return this._DataItem.IsVisible; }
        /// <summary>
        /// Je prvek viditelný!
        /// </summary>
        private void _SetVisible(bool value) { this._DataItem.IsVisible = value; }
        /// <summary>
        /// Je prvek dostupný?
        /// </summary>
        private bool _GetEnabled(bool value) { return this._DataItem.IsEnabled; }
        /// <summary>
        /// Je prvek dostupný!
        /// </summary>
        private void _SetEnabled(bool value) { this._DataItem.IsEnabled = value; }
        /// <summary>
        /// Pole podpoložek prvku (ComboBox, SplitButton, atd)
        /// </summary>
        public EList<FunctionItem> ItemSubItems { get { this.CallSubItemsEnumerateBefore(); this._DataItem.OnSubItemsEnumerateBefore(); return this._DataItem.SubItems; } }
        /// <summary>
        /// Obsahuje true, pokud prvek má mít šipku dolů pro rozbalení (ComboBox vždy, Button jen tehdy, když má nějaké <see cref="ItemSubItems"/>)
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
        #region Support pro serializaci hodnoty prvku (IsChecked nebo Value, atd)
        /// <summary>
        /// Příznak, zda tento prvek bude persistovat svoji hodnotu <see cref="PersistValue"/>.
        /// Při příštím startu aplikace bude tato hodnota načtena z konfigurace a vložena do prvku.
        /// Defaultní hodnota je false.
        /// </summary>
        public virtual bool PersistEnabled { get { return this._DataItem.PersistEnabled; } }
        /// <summary>
        /// Hodnota, která je pro tento prvek persistována. Pokud je zde null, pak persistence není.
        /// Potomek může tuto property přepsat, a konvertovat string na svoji hodnotu, pak bude persistována do příštího spuštění aplikace.
        /// </summary>
        public virtual string PersistValue { get { return null; } set { } }
        #endregion
        #region Příprava rozvržení obsahu v tomto prvku, pro korektní rozvržení ToolbarGroup. Explicitní implementace ILayoutItem
        /// <summary>
        /// Potomek v této metodě určí minimální velikost pro tento prvek.
        /// Nepočítá konkrétní Bounds, protože to počítá engine Layout.
        /// </summary>
        /// <param name="graphics"></param>
        internal abstract void PrepareLayout(Graphics graphics);
        /// <summary>
        /// Připraví data pro layout aktuálního prvku, společná metoda.
        /// Jejím úkolem je změřit svůj obsah (typicky velikost textu), reagovat na <see cref="ItemSize"/>, 
        /// a připravit si na základě výsledků souřadnice virtuální i fyzické.
        /// Bázová metoda pracuje pro typický Button, a plní properties: 
        /// <see cref="BoundsImage"/>, <see cref="BoundsText"/>, <see cref="BoundsIcon"/>, 
        /// <see cref="ModuleSize"/>, <see cref="PixelSizeMin"/>.
        /// <para/>
        /// Tyto hodnoty následně využívají metody pro kreslení:
        /// <see cref="DrawItemBackground(GInteractiveDrawArgs, Rectangle, bool, int, bool)"/>,
        /// <see cref="DrawItemImage(GInteractiveDrawArgs, Rectangle, FunctionItem)"/>,
        /// <see cref="DrawItemText(GInteractiveDrawArgs, Rectangle, FunctionItem)"/>,
        /// <see cref="DrawItemIcon(GInteractiveDrawArgs, Rectangle)"/>.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="getImageSize"></param>
        /// <param name="getTextSize"></param>
        protected void PrepareBoundsCommon(Graphics graphics, Func<Graphics, Size> getImageSize = null, Func<Graphics, Size> getTextSize = null)
        {
            ToolBar.LayoutSettingTBarInfo tBarSetting = this.TBarSetting;
            ToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;

            Size sizeImage = (getImageSize != null ? getImageSize(graphics) : this.GetImageSize(this.ItemImage));      // Velikost pro prvek: Velká ikona = obrázek tlačítka
            Size sizeText = (getTextSize != null ? getTextSize(graphics) : this.GetTextSize(graphics, this.ItemText)); // Velikost pro prvek: Text tlačítka
            Size sizeIcon = this.GetIconSize(this.HasDownArrow);               // Ikona reprezentující rozbalovací tlačítko u combo boxu
            if (sizeText.Height > itemSetting.TextHeight) sizeText.Height = itemSetting.TextHeight;

            if (this.ItemSize == FunctionGlobalItemSize.Whole)
            {   // Celá výška toolbaru: nahoře je Image, pod ním je Text, obě jsou zarovnány na svislý střed:
                int textIconWidth = sizeText.Width + sizeIcon.Width;               // Šířka textové části = text + šipka dolů
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
            {   // Menší velikosti: Image je vlevo, vedle něj vpravo text, zarovnání je na vodorovnou osu linku:
                int h = itemSetting.ModulePixel;
                int s = itemSetting.OffsetPixel;
                int c = (h / 2);
                int x = 0;

                if (sizeImage.Width > 0 && sizeImage.Height > 0)
                {
                    int y = c - (sizeImage.Height / 2);
                    x = y;
                    this.BoundsImage = new Rectangle(x, y, sizeImage.Width, sizeImage.Height);
                    x = this.BoundsImage.Right + s;
                }
                else
                {
                    this.BoundsImage = Rectangle.Empty;
                    x = s;
                }

                if (sizeText.Width > 0 && sizeText.Height > 0)
                {
                    int y = c - (sizeText.Height / 2);
                    this.BoundsText = new Rectangle(x, y, sizeText.Width, sizeText.Height);
                    x = this.BoundsText.Right + s;
                }
                else
                {
                    this.BoundsText = Rectangle.Empty;
                }

                if (sizeIcon.Width > 0)
                {
                    this.BoundsIcon = new Rectangle(x, 2, sizeIcon.Width, h - 4);
                    x = this.BoundsIcon.Right + 3;
                }
                else
                {
                    this.BoundsIcon = Rectangle.Empty;
                }

                int modulesWidth = tBarSetting.GetModuleCount(x);
                int modulesHeight = itemSetting.ModuleCount;
                this.ModuleSize = new Size(modulesWidth, modulesHeight);
                this.PixelSizeMin = tBarSetting.GetPixelSize(this.ModuleSize);
            }
        }
        /// <summary>
        /// Fyzické souřadnice pro kreslení Image, relativně k this.Bounds
        /// </summary>
        protected Rectangle BoundsImage { get; set; }
        /// <summary>
        /// Fyzické souřadnice pro kreslení Text, relativně k this.Bounds
        /// </summary>
        protected Rectangle BoundsText { get; set; }
        /// <summary>
        /// Fyzické souřadnice pro kreslení Icon (šipka dolů), relativně k this.Bounds
        /// </summary>
        protected Rectangle BoundsIcon { get; set; }
        /// <summary>
        /// Minimální velikost pro tento prvek, v pixelech, podle Image, Text, Size, Font
        /// </summary>
        public Size PixelSizeMin { get; protected set; }
        /// <summary>
        /// Velikost tohoto prvku v "modulech", kde 1 modul je velikost elementu Micro bez textu.
        /// POkud prvek má nějaký text, pak je jeho velikost v modulech zarovnaná nahoru.
        /// </summary>
        public Size ModuleSize { get; protected set; }
        /// <summary>
        /// Souřadnice tohoto prvku v "modulech", výsledek procesu LayoutEngine
        /// </summary>
        public Rectangle? ModuleBounds { get; protected set; }
        /// <summary>
        /// Return size for image, for this Size and current Toolbar (LayoutInfo, Zoom)
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        protected Size GetImageSize(Image image)
        {
            if (image == null) return new Size(0, 0);
            ToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;
            return itemSetting.ImageSize;
        }
        /// <summary>
        /// Return size for image, for this Size and current Toolbar (LayoutInfo, Zoom)
        /// </summary>
        /// <param name="withThisImage"></param>
        /// <param name="subItems"></param>
        /// <returns></returns>
        protected Size GetImageSizeFromSubItems(bool withThisImage, EList<FunctionItem> subItems)
        {
            ToolBar.LayoutSettingTItemInfo itemSetting = this.TItemSetting;
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
        /// <param name="subItems"></param>
        /// <param name="maxModuleWidth"></param>
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
        #region Společné podpůrné metody pro potomky
        /// <summary>
        /// Metoda vykreslí pozadí aktuálního prvku, reaguje na stavy: Enabled, Selected, MouseActive, MouseDown.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="isSelected"></param>
        /// <param name="roundCorner"></param>
        /// <param name="forceBorder"></param>
        protected void DrawItemBackground(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool isSelected = false, int roundCorner = 2, bool forceBorder = false)
        {
            bool isEnabled = this.Is.Enabled;
            bool drawBackground = isEnabled && (this.IsMouseActive || isSelected);
            bool drawBorders = forceBorder || (isEnabled && this.IsMouseDown);

            if (drawBackground || drawBorders)
            {
                Color backColor = (!isSelected ? Skin.ToolBar.ItemBackColor : Skin.ToolBar.ItemSelectedBackColor);
                GPainter.DrawButtonBase(e.Graphics, boundsAbsolute, new DrawButtonArgs() { BackColor = backColor, InteractiveState = this.InteractiveState, RoundCorner = roundCorner, DrawBackground = drawBackground, DrawBorders = drawBorders, BorderColor = Skin.ToolBar.ItemBorderColor });
            }
        }
        /// <summary>
        /// Metoda vykreslí Image = hlavní obrázek prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void DrawItemImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemImage(e, boundsAbsolute, this._DataItem);
        }
        /// <summary>
        /// Metoda vykreslí Image = hlavní obrázek prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="activeItem"></param>
        protected void DrawItemImage(GInteractiveDrawArgs e, Rectangle boundsAbsolute, FunctionItem activeItem)
        {
            if (this.BoundsImage.Width <= 0) return;

            Image image = activeItem.Image;
            Image imageHot = null;
            if (image != null)
            {
                Rectangle boundsImageAbsolute = this.GetAbsoluteBoundsForPart(boundsAbsolute, this.BoundsImage); // this.BoundsImage.ShiftBy(boundsAbsolute.Location);
                if (this.InteractiveState.IsMouseActive())
                    imageHot = this._DataItem.ImageHot;
                GPainter.DrawImage(e.Graphics, boundsImageAbsolute, (imageHot ?? image), activeItem.IsEnabled, ContentAlignment.MiddleCenter);
            }
        }
        /// <summary>
        /// Metoda vykreslí Text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void DrawItemText(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            this.DrawItemText(e, boundsAbsolute, this._DataItem);
        }
        /// <summary>
        /// Metoda vykreslí Text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="activeItem"></param>
        protected void DrawItemText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, FunctionItem activeItem)
        {
            if (this.BoundsText.Width <= 0) return;

            string text = (activeItem != null ? this.ItemSize == FunctionGlobalItemSize.Micro ? "" : activeItem.TextText : null);
            if (!String.IsNullOrEmpty(text))
            {
                Rectangle boundsTextAbsolute = this.GetAbsoluteBoundsForPart(boundsAbsolute, this.BoundsText); // this.BoundsText.ShiftBy(boundsAbsolute.Location);
                FontInfo fontInfo = this.CurrentItemFont;
                Color textColor = Skin.ModifyForeColorByState(Skin.ToolBar.TextColor, this.InteractiveState);
                GPainter.DrawString(e.Graphics, text, fontInfo, boundsTextAbsolute, ContentAlignment.MiddleCenter, textColor);
            }
        }
        /// <summary>
        /// Metoda vykreslí Ikonu vpravo od textu, která znázorňuje DropDown 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected void DrawItemIcon(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (this.BoundsIcon.Width <= 0) return;

            Rectangle boundsIconAbsolute = this.GetAbsoluteBoundsForPart(boundsAbsolute, this.BoundsIcon); // this.BoundsIcon.ShiftBy(boundsAbsolute.Location);
            int w = boundsIconAbsolute.Width;
            if (w <= 0) return;
            int x = boundsIconAbsolute.X;
            int y  = boundsIconAbsolute.Y;
            int h = boundsIconAbsolute.Height;
            int b = boundsIconAbsolute.Bottom;
            e.Graphics.DrawLine(Skin.Pen(Skin.ToolBar.SeparatorDarkColor), x, y + 2, x, b - 3);
            e.Graphics.DrawLine(Skin.Pen(Skin.ToolBar.SeparatorLightColor), x + 1, y + 2, x + 1, b - 3);
        }
        /// <summary>
        /// Metoda vykreslí ohraničení okolo prvku v případě, že je isSelected.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="isSelected"></param>
        protected void DrawItemSelection(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool isSelected)
        {
            if (!isSelected) return;
            Rectangle boundsSelection = boundsAbsolute.Enlarge(-1);
            e.Graphics.DrawRectangle(Skin.Pen(Skin.ToolBar.ItemSelectedLineColor), boundsSelection);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice prostoru (boundsPart), který je zadán jako relativní, a to vzhledem k teoretické velikosti <see cref="PixelSizeMin"/>.
        /// Jsou zadány absolutní souřadnice prostoru prvku (boundsAbsolute), jehož velikost (Size) může být větší než velikost <see cref="PixelSizeMin"/>.
        /// To je dáno generátorem layoutu.
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <param name="boundsPart"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        protected Rectangle GetAbsoluteBoundsForPart(Rectangle boundsAbsolute, Rectangle boundsPart, ContentAlignment alignment = ContentAlignment.MiddleCenter)
        {
            Size originalSize = this.PixelSizeMin;         // Do této teoretické velikosti byly zarovnány souřadnice jednotlivých součástí prvku, tedy i (boundsPart)
            Rectangle originalBounds = originalSize.AlignTo(boundsAbsolute, alignment);  // Aktuální absolutní souřadnice původního zamýšleného prostoru PixelSizeMin
            return boundsPart.ShiftBy(originalBounds.Location);
        }
        protected virtual void CallSubItemsEnumerateBefore()
        {
            this.CallSubItemsEnumerateBefore(this._DataItem);
        }
        protected virtual void CallSubItemsEnumerateBefore(FunctionItem activeItem)
        {
            this._ToolbarGroup.DataGroup.OnSubItemsEnumerateBefore(activeItem);
            this._ToolbarGroup.Toolbar.OnItemSubItemsEnumerateBefore(this._ToolbarGroup.DataGroup, activeItem);
        }
        /// <summary>
        /// Kliknutí na this prvek
        /// </summary>
        protected virtual void CallItemClick()
        {
            this.CallItemClick(this._DataItem);
        }
        /// <summary>
        /// Kliknutí na daný prvek (což může být this, nebo jedna z položek v ComboBoxu = tedy moje Sub-Item)
        /// </summary>
        /// <param name="activeItem"></param>
        protected virtual void CallItemClick(FunctionItem activeItem)
        {
            this._ToolbarGroup.DataGroup.OnItemClicked(activeItem);
            if (activeItem.Parent != null)
                activeItem.Parent.OnSubItemsClick(activeItem);
            activeItem.OnClick();
            this._ToolbarGroup.Toolbar.OnItemClicked(this._ToolbarGroup.DataGroup, activeItem);
        }
        /// <summary>
        /// Změna stavu IsChecked
        /// </summary>
        /// <param name="activeItem"></param>
        protected virtual void CallItemCheckedChange(FunctionItem activeItem)
        {
            this._ToolbarGroup.DataGroup.OnItemCheckedChange(activeItem);
            if (activeItem.Parent != null)
                activeItem.Parent.OnSubItemsCheckedChange(activeItem);
            activeItem.OnCheckedChange();
            this._ToolbarGroup.Toolbar.OnItemCheckedChange(this._ToolbarGroup.DataGroup, activeItem);
        }
        #endregion
        #region Podpora pro aplikaci
        /// <summary>
        /// Provedde stejnou akci, jako když uživatel klikne na prvek
        /// </summary>
        public virtual void DoClick() { }
        #endregion
        #region Draw, Interaktivita
        /// <summary>
        /// Vykreslí jeden prvek
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            if (!this.Is.Visible) return;
            this.DrawItem(e, absoluteBounds, absoluteVisibleBounds, drawMode);
        }
        /// <summary>
        /// V této metodě potomek zajistí vykreslení sebe sama.
        /// Může k tomu využívat bázové metody pro kreslení, a může je kombinovat s vlastními metodami.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected abstract void DrawItem(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode);
        /// <summary>
        /// Připraví data pro zobrazení ToolTipu. Metodu volá předek.
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            string toolTip = this.CurrentToolTip;
            if (String.IsNullOrEmpty(toolTip)) return;
            e.ToolTipData.InfoText = toolTip;

            string toolTipTitle = this._ToolbarGroup.DataToolTipTitle;
            e.ToolTipData.TitleText = (!String.IsNullOrEmpty(toolTipTitle) ? toolTipTitle : this._ToolbarGroup.DataTitle);
        }
        /// <summary>
        /// Potomek může přepsat a zobrazovat tak svůj tooltip.
        /// Bázová třída vrací <see cref="ItemToolTip"/>.
        /// </summary>
        protected virtual string CurrentToolTip { get { return this.ItemToolTip; } }
        /// <summary>
        /// Interaktivita prvku
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            bool isEnabled = this.Is.Enabled;
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
        #endregion
    }
    #endregion
    #region class ToolBarStatus : stav celého ToolBaru = velikost, prvky Checked, hodnoty prvků, atd - pro persistenci do příštího spuštění
    /// <summary>
    /// ToolBarStatus : stav celého ToolBaru = velikost, prvky Checked, hodnoty prvků, atd - pro persistenci do příštího spuštění
    /// </summary>
    public class ToolBarStatus
    {
        #region Konstruktor, proměnné, persistované property
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        private ToolBarStatus() { }
        /// <summary>
        /// Velikost ToolBaru
        /// </summary>
        protected ComponentSize? ToolbarSize { get; set; }
        /// <summary>
        /// Index obsahující klíče prvků toolbaru a jejich persist hodnotu
        /// </summary>
        private Dictionary<string, ToolBarItem> KeyValueDict { get; set; }
        #endregion
        #region Snapshot = stringový výraz pro persistenci stavu Toolbaru
        /// <summary>
        /// Stringový otisk aktuálního stavu, pro detekci změny
        /// </summary>
        [PersistingEnabled(false)]
        public string Snapshot
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in this.KeyValueDict)
                {
                    string value = item.Value.PersistValue;
                    if (value != null)
                        sb.Append((sb.Length > 0 ? ";" : "") + item.Key + ":" + value);
                }
                return sb.ToString();
            }
            set
            {
                if (value == null) return;
                var table = value.ToTable(";", ":", true, true);
                foreach (var row in table)
                {
                    if (row.Length < 2) continue;
                    string key = row[0];
                    ToolBarItem item;
                    if (!String.IsNullOrEmpty(key) && this.KeyValueDict.TryGetValue(key, out item))
                        item.PersistValue = row[1];
                }
            }
        }
        #endregion
        #region Tvorba i Aplikace stavu toolbaru z/do dodaných prvků ToolBaru
        /// <summary>
        /// Metoda sestaví a vrátí new instanci <see cref="ToolBarStatus"/>, která obsahuje aktuální hodnoty prvků z dodaného ToolBaru.
        /// </summary>
        /// <param name="toolBar"></param>
        /// <returns></returns>
        public static ToolBarStatus CreateFrom(ToolBar toolBar)
        {
            if (toolBar == null) return null;

            ToolBarStatus status = new ToolBarStatus();

            status.ToolbarSize = toolBar.ToolbarSize;
            status.KeyValueDict = CreateItemDict(toolBar);

            return status;
        }
        /// <summary>
        /// Dodaný stav <see cref="ToolBarStatus"/> aplikuje do dodaného objektu <see cref="ToolBar"/>.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="toolBar"></param>
        public static void ApplyTo(ToolBarStatus status, ToolBar toolBar)
        {
            if (status == null || toolBar == null) return;

            if (status.ToolbarSize.HasValue) toolBar.ToolbarSize = status.ToolbarSize.Value;


            FunctionGlobalItem[] functionItems = toolBar.FunctionItems;

        }
        /// <summary>
        /// Metoda vrátí Dictionary obsahující prvky toolbaru podle jejich klíče; klíčem je <see cref="FunctionGlobalGroup.Title"/>.<see cref="FunctionItem.Name"/>.
        /// Prvek, jehož <see cref="FunctionItem.Name"/> je Empty, se do výstupu nedostane.
        /// Pokud stejný klíč bude mít více prvků, do výstupu se dostane jen první.
        /// </summary>
        /// <param name="toolBar"></param>
        /// <returns></returns>
        internal static Dictionary<string, ToolBarItem> CreateItemDict(ToolBar toolBar)
        {
            Dictionary<string, ToolBarItem> result = new Dictionary<string, ToolBarItem>();
            if (toolBar == null) return result;
            foreach (ToolBarGroup gGroup in toolBar.GFunctionGroups)
            {
                string groupName = (gGroup.Name ?? "_") + ".";
                foreach (ToolBarItem gItem in gGroup.FunctionGItems)
                {
                    if (!gItem.PersistEnabled) continue;
                    string itemName = gItem.Name;
                    if (String.IsNullOrEmpty(itemName)) continue;
                    string key = groupName + itemName;
                    result.AddOnce(key, gItem);
                }
            }
            return result;
        }
        #endregion
    }
    #endregion
    #region enum atd
    /// <summary>
    /// Režim provedení refresh a Invalidate pro ToolBar, poté kdy byla provedena metoda
    /// </summary>
    [Flags]
    public enum ToolBarRefreshMode
    {
        /// <summary>
        /// Netřeba provádět nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Je třeba provést přepočet celého Layoutu.
        /// Používá se po změně hodnot, které mají vliv na layout: Size, Width, Hint, Text
        /// </summary>
        RefreshLayout = 0x0001,
        /// <summary>
        /// Je třeba provést překreslení controlu (základní Refresh())
        /// </summary>
        RefreshControl = 0x0002,
        /// <summary>
        /// Refresh Layoutu i Controlu
        /// </summary>
        All = RefreshLayout | RefreshControl
    }
    #endregion
}
