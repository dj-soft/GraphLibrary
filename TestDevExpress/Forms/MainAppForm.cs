using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Diagram.Core.Shapes;
using DevExpress.Charts.Native;
using Noris.Clients.Win.Components.AsolDX;
using Noris.Clients.Win.Components;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Hlavní okno testovací aplikace - fyzická třída.
    /// Načítá dostupné formuláře, které chtějí být členem Ribbonu v hlavním okně (podle implementace <see cref="RunTargetInfo"/>
    /// </summary>
    public class MainAppForm : Noris.Clients.Win.Components.AsolDX.DxMainAppForm
    {
        #region Konstruktor, Ribbon, Statusbar
        /// <summary>
        /// Příprava Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            // Noris.Clients.Win.Components.AsolDX.Colors.ColorConverting.Test();

            TestDevExpress.Forms.MainAppForm.CurrentInstance = this;

            if (!this.PositionIsFromConfig)
            {
                this.WindowState = FormWindowState.Maximized;
            }

            this.Text = $"Test DevExpress [{DxComponent.FrameworkName}]";

            var ribbonContent = new DataRibbonContent();
            ribbonContent.Pages.AddRange(_CreateRibbonPages());
            ribbonContent.StatusBarItems.AddRange(_CreateStatusItems());

            this.DxRibbon.RibbonContent = ribbonContent;
            this.DxRibbon.AllowCustomization = true;

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;

            this.__RefreshTimerGuid = WatchTimer.CallMeEvery(_RunRefreshGdi, 500);
        }
        public static MainAppForm CurrentInstance { get; private set; }
        public string StatusBarText
        {
            get { return _StatusMainInfoItem.Text; }
            set { _StatusMainInfoItem.Text = value; _StatusMainInfoItem.Refresh(); }
        }

        private List<DataRibbonPage> _CreateRibbonPages()
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();

            var ribbonButtons = FormRibbonDesignGroupPart.SkinButton | FormRibbonDesignGroupPart.PaletteButton | FormRibbonDesignGroupPart.UhdSupport | FormRibbonDesignGroupPart.ImageGallery |
                                FormRibbonDesignGroupPart.LogActivity | FormRibbonDesignGroupPart.NotCaptureWindows | FormRibbonDesignGroupPart.ZoomPresetMenuTest;
            DataRibbonPage homePage = this.CreateRibbonHomePage(ribbonButtons);
            homePage.PageOrder = 10;
            pages.Add(homePage);

            var start = DateTime.Now;
            var runFormInfos = RunTargetInfo.GetRunTargets();                  // Debug mode: 1354, 1283, 1224 milisecs;     Run mode: 219, 241, 238 milisecs
            __FormLoadTime = DateTime.Now - start;

            RunTargetInfo.CreateRibbonPages(runFormInfos, pages, homePage);

            return pages;
        }
        private List<DataRibbonItem> _CreateStatusItems()
        {
            var assemblyInfo = DxComponent.GetAssemblyInfo(this.GetType());
            string versionToolTipTitle = $"{assemblyInfo.AssemblyTitle}";
            string versionToolTipText = $"Version: {assemblyInfo.AssemblyFileVersion}\r\nFile: {assemblyInfo.AssemblyFullFileName}\r\nTime: {assemblyInfo.FileModifyTime}";
            string statText = $"Vyhledání aktivních formulářů s metodou RunFormInfo.GetFormsWithProperty(): čas = {__FormLoadTime.TotalMilliseconds:N0} ms";
            _StatusVersionItem = new DataRibbonItem() { ItemId = "StatusVersion", ItemType = RibbonItemType.Static, Text = $"Ver. {assemblyInfo.AssemblyFileVersion}", ImageName = "svgimages/icon%20builder/actions_info.svg", ToolTipTitle = versionToolTipTitle, ToolTipText = versionToolTipText };
            _StatusMainInfoItem = new DataRibbonItem() { ItemId = "StatusMainInfo", ItemType = RibbonItemType.StaticSpring, Text = statText, ImageName = "", ImageFromCaptionMode = ImageFromCaptionType.Disabled, ItemIsFirstInGroup = true };
            _StatusWin32InfoItem = new DataRibbonItem() { ItemId = "StatusWin32Info", ItemType = RibbonItemType.Static, Text = "GDI", ImageName = "", ImageFromCaptionMode = ImageFromCaptionType.Disabled, ItemIsFirstInGroup = true };
            _StatusZoomLabelItem = new DataRibbonItem() { ItemId = "StatusZoomLabel", ItemType = RibbonItemType.Static, Text = "Měřítko", ImageName = "", ImageFromCaptionMode = ImageFromCaptionType.Disabled, Alignment = BarItemAlignment.Right, ItemIsFirstInGroup = true };
            _StatusZoomMenuItem = new DataRibbonItem() { ItemId = "StatusZoomMenu", ItemType = RibbonItemType.ZoomPresetMenu, Text = "100%", ImageName = "", ImageFromCaptionMode = ImageFromCaptionType.Disabled, Alignment = BarItemAlignment.Right, Tag = "50,70,85,100,125,150,200" };
            var statusItems = new List<DataRibbonItem>();
            statusItems.Add(_StatusVersionItem);
            statusItems.Add(_StatusMainInfoItem);
            statusItems.Add(_StatusWin32InfoItem);
            statusItems.Add(_StatusZoomLabelItem);
            statusItems.Add(_StatusZoomMenuItem);
            return statusItems;
        }
        private DataRibbonItem _StatusVersionItem;
        private DataRibbonItem _StatusMainInfoItem;
        private DataRibbonItem _StatusWin32InfoItem;
        private DataRibbonItem _StatusZoomLabelItem;
        private DataRibbonItem _StatusZoomMenuItem;
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            switch (e.Item.ItemId)
            {
                case DxRibbonControl.DesignRibbonItemLogActivityId:
                    // Aktivita logu: spolupracuje s postranním panelem
                    this.LogActivityDockPanelVisible = e.Item.Checked ?? false;
                    break;
            }
        }
        /// <summary>
        /// Příprava StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            // StatusBar je připraven společně s Ribbonem do this.DxRibbon.RibbonContent
        }
        /// <summary>
        /// Text v prvním poli StatusBaru
        /// </summary>
        public string StatusVersionText { get { return __StatusVersionLabel?.Caption; } set { if (__StatusVersionLabel != null) __StatusVersionLabel.Caption = value; } }
        /// <summary>
        /// Text v druhém poli StatusBaru
        /// </summary>
        public string StatusInfoText { get { return __StatusInfoLabel?.Caption; } set { if (__StatusInfoLabel != null) __StatusInfoLabel.Caption = value; } }
        /// <summary>
        /// Jméno konfigurace v subsystému AsolDX.
        /// Pokud bude zde vráceno neprázdné jméno, pak načtení a uložení konfigurace okna zajistí sama třída, která implementuje <see cref="IFormStatusWorking"/>.
        /// Pokud nebude vráceno jméno, budou používány metody <see cref="DxRibbonBaseForm.PositionLoadFromConfig(string)"/> a <see cref="DxRibbonBaseForm.PositionSaveToConfig(string, string)"/>.
        /// </summary>
        protected override string PositionConfigName { get { return "MainAppForm"; } }
        private void _RunRefreshGdi()
        {
            if (_StatusWin32InfoItem != null)
            {
                if (!DxComponent.LogActive)
                    DxComponent.LogActive = true;

                var time = DxComponent.LogTimeCurrent;
                string currentGdiState = _GetCurrentGdiState();
                var mics1 = DxComponent.LogGetTimeElapsed(time);
                if (!String.Equals(currentGdiState, _StatusWin32InfoItem.Text))
                {
                    time = DxComponent.LogTimeCurrent;
                    _StatusWin32InfoItem.Text = currentGdiState;
                    _StatusWin32InfoItem.Refresh();
                    var mics2 = DxComponent.LogGetTimeElapsed(time);
                    _StatusWin32InfoItem.ToolTipText = $"Získání GDI informací: {mics1} microsec;\r\nRefresh statusbaru: {mics2} microsec;";     // Promítne se až příště.
                }
            }
        }
        private string _GetCurrentGdiState()
        {
            var info =  DxComponent.WinProcessInfo.GetCurent();
            return $"GDI: {info.GDIHandleCount}";
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            WatchTimer.Remove(this.__RefreshTimerGuid);
            this.__RefreshTimerGuid = null;
        }
        private DevExpress.XtraBars.BarItem __StatusVersionLabel;
        private DevExpress.XtraBars.BarItem __StatusInfoLabel;
        private Guid? __RefreshTimerGuid;
        private TimeSpan __FormLoadTime;
        #endregion
        #region DockManager a TabHeaderPainter - služby
        /// <summary>
        /// Po dokončení tvorby Dockmanageru, DocumentManageru, TabbedView a DockPanelů
        /// </summary>
        protected override void DxMainContentPreparedAfter() 
        {
            _TabHeaderImagePainterPrepare();
        }
        /// <summary>
        /// Inicializace painteru TabHeader
        /// </summary>
        private void _TabHeaderImagePainterPrepare()
        {
            __TabHeaderImagePainter = new DxTabHeaderImagePainter();
            __TabHeaderImagePainter.TabbedView = this.TabbedView;
            __TabHeaderImagePainter.AllIconsDirectPaint = true;
            __TabHeaderImagePainter.SecondImagePosition = DxTabHeaderImagePainter.ImagePositionType.AfterTextArea;
            __TabHeaderImagePainter.MainImageSizeType = ResourceImageSizeType.Medium;
            __TabHeaderImagePainter.SecondImageSizeType = ResourceImageSizeType.Medium;
        }
        /// <summary>
        /// Pomocník pro kreslení ikon (standardní + přidaná) v záhlaví TabHeader.
        /// </summary>
        private DxTabHeaderImagePainter __TabHeaderImagePainter;
        /// <summary>
        /// Inicializace DockPanelu LogActivity
        /// </summary>
        protected override void InitializeDockPanelsContent()
        {
            LogActivityDockPanel = new TestDevExpress.Components.AppLogPanel();
            var logVisibility = (DxComponent.LogActive ? DevExpress.XtraBars.Docking.DockVisibility.Visible : DevExpress.XtraBars.Docking.DockVisibility.AutoHide);

            DockPanelLayoutInfo dockPanelLayout = new DockPanelLayoutInfo();
            dockPanelLayout.DockStyle = DevExpress.XtraBars.Docking.DockingStyle.Right;
            dockPanelLayout.PanelSize = new Size(360, 220);
            dockPanelLayout.Definition = DxComponent.Settings.GetRawValue("Components", "AppLogPanelPosition");        // Pokud v Settings nebude hodnota, najde se null a vloží se do dockPanelLayout, tam vložení hodnoty null je ignorováno.
            dockPanelLayout.UserControl = LogActivityDockPanel;
            dockPanelLayout.PanelTitle = "Log aplikace";
            dockPanelLayout.Visibility = logVisibility;
            dockPanelLayout.ImageName = "svgimages/xaf/action_aboutinfo.svg";

            this.AddControlToDockPanels(dockPanelLayout);

            dockPanelLayout.Visibility = logVisibility;

            dockPanelLayout.LayoutChanged += _LogActivityPanel_LayoutChanged;


            //var logControl2 = new TestDevExpress.Components.AppLogPanel();
            //this.AddControlToDockPanels(logControl2, "Doplňkový log", DevExpress.XtraBars.Docking.DockingStyle.Right, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);

            //var logControl3 = new TestDevExpress.Components.AppLogPanel();
            //this.AddControlToDockPanels(logControl3, "přídavný log", DevExpress.XtraBars.Docking.DockingStyle.Right, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);

            //var logControl4 = new TestDevExpress.Components.AppLogPanel();
            //this.AddControlToDockPanels(logControl4, "Jinopohledový log", DevExpress.XtraBars.Docking.DockingStyle.Left, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);
        }
        /// <summary>
        /// Po změně layoutu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LogActivityPanel_LayoutChanged(object sender, EventArgs e)
        {
            if (this.WasShown && sender is DockPanelLayoutInfo dockPanelLayout)
            {
                DxComponent.Settings.SetRawValue("Components", "AppLogPanelPosition", dockPanelLayout.Definition);
            }
        }
        /// <summary>
        /// Viditelnost panelu <see cref="LogActivityDockPanel"/>
        /// </summary>
        protected bool LogActivityDockPanelVisible
        {
            get { return (LogActivityDockPanelVisibility == DevExpress.XtraBars.Docking.DockVisibility.Visible ? true : false); }
            set { LogActivityDockPanelVisibility = (value ? DevExpress.XtraBars.Docking.DockVisibility.Visible : DevExpress.XtraBars.Docking.DockVisibility.AutoHide); }
        }
        /// <summary>
        /// Viditelnost panelu <see cref="LogActivityDockPanel"/>
        /// </summary>
        protected DevExpress.XtraBars.Docking.DockVisibility LogActivityDockPanelVisibility
        {
            get 
            {
                if (LogActivityDockPanel?.Parent?.Parent is DevExpress.XtraBars.Docking.DockPanel dockPanel)
                    return dockPanel.Visibility;
                return DevExpress.XtraBars.Docking.DockVisibility.Hidden;
            }
            set 
            {
                if (LogActivityDockPanel?.Parent?.Parent is DevExpress.XtraBars.Docking.DockPanel dockPanel)
                    dockPanel.Visibility = value;
            }
        }
        /// <summary>
        /// Panel obsahující LogActivity
        /// </summary>
        protected DxPanelControl LogActivityDockPanel;
        #endregion
        #region TabView BackImage a MouseClick
        /// <summary>
        /// Inicializuje věci pro kreslení obrázku na pozadí TabView
        /// </summary>
        /// <param name="tabbedView"></param>
        protected override void InitializeImageMapOnTabbedView(DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView)
        {
            __TabViewBackImageMap = new DxImageAreaMap();
            _PrepareImageMap(__TabViewBackImageMap);
            __TabViewBackImageMap.Click += __TabViewBackImageMap_Click;
            tabbedView.CustomDrawBackground += _TabViewCustomDrawBackground;
        }
        /// <summary>
        /// Disposuje věci pro kreslení obrázku na pozadí TabView
        /// </summary>
        protected override void DisposeImageMapOnTabbedView(DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView)
        {
            tabbedView.CustomDrawBackground -= _TabViewCustomDrawBackground;
            __TabViewBackImageMap.OwnerControl = null;
            __TabViewBackImageMap.Dispose();
            __TabViewBackImageMap = null;
        }
        /// <summary>
        /// Naplní ImageMap daty dle definice
        /// </summary>
        /// <param name="imageMap"></param>
        private void _PrepareImageMap(DxImageAreaMap imageMap)
        {
            string imageFile = @"c:\DavidPrac\VsProjects\TestDevExpress\TestDevExpress\ImagesTest\Image01.png"; // @"c:\DavidPrac\VsProjects\TestDevExpress\TestDevExpress\ImagesTest\Svg\homer-simpson.svg";
            imageFile = @"Image00a.png"; // @"c:\DavidPrac\VsProjects\TestDevExpress\TestDevExpress\ImagesTest\Svg\homer-simpson.svg";
            // imageFile = @"c:\DavidPrac\VsProjects\TestDevExpress\TestDevExpress\ImagesTest\Svg\homer-simpson.svg";

            if (!System.IO.File.Exists(imageFile)) return;

            imageMap.ContentImage = System.IO.File.ReadAllBytes(imageFile);
            imageMap.Zoom = 0.95f;
            imageMap.RelativePosition = new PointF(0.98f, 0.98f);
            imageMap.BmpZoomMaxRatio = 2.0f;
            imageMap.InitialDelay = TimeSpan.FromMilliseconds(500d);
            imageMap.ResizeDelay = TimeSpan.FromMilliseconds(400d);

            imageMap.ClearActiveArea();
            imageMap.AddActiveArea(new RectangleF(0.05f, 0.05f, 0.80f, 0.20f), @"https://www.helios.eu", DxCursorType.Cross);
            imageMap.AddActiveArea(new RectangleF(0.50f, 0.35f, 0.40f, 0.30f), @"https://www.seznam.cz", DxCursorType.Hand);
            imageMap.AddActiveArea(new RectangleF(0.05f, 0.60f, 0.40f, 0.30f), @"https://www.idnes.cz", DxCursorType.Hand);
            imageMap.AddActiveArea(new RectangleF(0.00f, 0.80f, 1.00f, 0.25f), @"c:\Windows\notepad.exe", DxCursorType.Help);
        }
        /// <summary>
        /// Po kliknutí na ImageMap
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __TabViewBackImageMap_Click(object sender, DxImageAreaMap.AreaClickArgs e)
        {
            if (e.UserData is string runCmd && !String.IsNullOrEmpty(runCmd))
                System.Diagnostics.Process.Start(runCmd);
        }
        /// <summary>
        /// V události CustomDrawBackground vykreslíme obrázek na pozadí.
        /// To mimo jiné zajistí napojení Controlu na pozadí do klikací mapy, a do klikací mapy i vloží aktuální souřadnice obrázku, tím se zajistí správné rozmístění klikacích ploch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabViewCustomDrawBackground(object sender, DevExpress.XtraBars.Docking2010.CustomDrawBackgroundEventArgs e)
        {
            var imageMap = __TabViewBackImageMap;
            if (!imageMap.HasImage) return;
            if (!imageMap.WasStoredControl)
                imageMap.OwnerControl = this.Controls.OfType<MdiClient>().FirstOrDefault();

            if (!this.TabbedView.IsEmpty) return;

            var clientBounds = e.Bounds;
            var innerBounds = Rectangle.FromLTRB(clientBounds.Left + 12, clientBounds.Top + 48, clientBounds.Right - 12, clientBounds.Bottom - 12);
            imageMap.PaintImageMap(e.GraphicsCache, innerBounds);
        }
        /// <summary>
        /// Instance klikacího obrázku na pozadí TabView
        /// </summary>
        private DxImageAreaMap __TabViewBackImageMap;
        #endregion
    }
    #region class RunTargetInfo : řešení požadavku "Označ určitý kód (třída / metoda) pro jeho snadné automatické spuštění z Ribbonu
    /// <summary>
    /// <see cref="RunTargetInfo"/> : řešení požadavku "Označ určitý kód (třída / metoda) pro jeho snadné automatické spuštění z Ribbonu
    /// <para/>
    /// <b>1. Formulář</b>: Pokud určitý formulář chce být spouštěn z tlačítka v ribbonu okna <see cref="MainAppForm"/>, pak nechť si do záhlaví třídy přidá CustomAtribut <see cref="RunFormInfoAttribute"/> a naplní jej.<br/>
    /// Okno aplikace <see cref="MainAppForm"/> si při spuštění najde tyto formuláře, a sestaví je a do Ribbonu zobrazí jejich tlačítka.
    /// <para/>
    /// <b>2. Testovací algoritmus</b>: Pokud určitá třída obsahuje testovací metodu, která chce být spouštěna z Ribbonu okna <see cref="MainAppForm"/> a jejím úkolem je např. něco otestovat, 
    /// pak nechť si do záhlaví třídy přidá CustomAtribut <see cref="RunFormInfoAttribute"/> a naplní jej.<br/>
    /// Okno aplikace <see cref="MainAppForm"/> si při spuštění najde tyto formuláře, a sestaví je a do Ribbonu zobrazí jejich tlačítka.
    /// </summary>
    public class RunTargetInfo
    {
        #region Public definiční data
        /// <summary>
        /// Název stránky, kde se bude button nacházet.
        /// null = výchozí: "ZÁKLADNÍ"
        /// </summary>
        public string PageText { get; set; }
        /// <summary>
        /// Pořadí stránky.
        /// Když více prvků bude mít shodný text <see cref="PageText"/> a rozdélné pořadí <see cref="PageOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="PageOrder"/> v rámci stránky.
        /// </summary>
        public int PageOrder { get; set; }
        /// <summary>
        /// Název grupy, kde se bude button nacházet.
        /// null = výchozí = "FUNKCE"
        /// </summary>
        public string GroupText { get; set; }
        /// <summary>
        /// Pořadí grupy ve stránce.
        /// Když více prvků bude mít shodný text <see cref="GroupText"/> a rozdélné pořadí <see cref="GroupOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="GroupOrder"/> v rámci grupy.
        /// </summary>
        public int GroupOrder { get; set; }
        /// <summary>
        /// Název buttonu = zobrazený text.
        /// Prázdný text je přípustný, pokud je dána ikona <see cref="ButtonImage"/>.
        /// </summary>
        public string ButtonText { get; set; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        public int ButtonOrder { get; set; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        public string ButtonImage { get; set; }
        /// <summary>
        /// ToolTip k buttonu
        /// </summary>
        public string ButtonToolTip { get; set; }
        /// <summary>
        /// Spouštět jako Modální okno
        /// </summary>
        public bool RunAsModal { get; set; }
        /// <summary>
        /// Spouštět jako Floating okno
        /// </summary>
        public bool RunAsFloating { get; set; }
        /// <summary>
        /// ToolTip k otevřenému oknu nad záložkou
        /// </summary>
        public string TabViewToolTip { get; set; }
        /// <summary>
        /// true pro použitelný prvek
        /// </summary>
        internal bool IsValid { get { return (!String.IsNullOrEmpty(ButtonText) || !String.IsNullOrEmpty(ButtonImage)); } }
        /// <summary>
        /// Druh spouštění cíle
        /// </summary>
        internal TargetRunType RunType { get; set; }
        /// <summary>
        /// Cílový Type
        /// </summary>
        internal Type RunTargetType { get; set; }
        /// <summary>
        /// Cílová metoda
        /// </summary>
        internal System.Reflection.MethodInfo RunTargetMethod { get; set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{GroupText}: {ButtonText}";
        }
        /// <summary>
        /// Způsob spuštění cíle
        /// </summary>
        public enum TargetRunType
        {
            /// <summary>
            /// Není co spustit
            /// </summary>
            None,
            /// <summary>
            /// Formulář: vytvořit new instanci a zobrazit ji
            /// </summary>
            Form,
            /// <summary>
            /// Metoda: spustit ji (statická)
            /// </summary>
            StaticMethod
        }
        #endregion
        #region Static vyhledání typů formulářů, které obsahují platné RunFormInfoAttribute
        /// <summary>
        /// Metoda vrací pole obsahující typy formulářů a instance <see cref="RunTargetInfo"/>, pocházející z atributu třídy <see cref="RunFormInfoAttribute"/>
        /// </summary>
        /// <returns></returns>
        public static RunTargetInfo[] GetRunTargets()
        {
            var myAssembly = typeof(RunTargetInfo).Assembly;

            var runFormInfos = new List<RunTargetInfo>();
            DxComponent.GetTypes(myAssembly, t => ContainsRunFormInfoAttribute(t, runFormInfos));

            return runFormInfos.ToArray();
        }
        /// <summary>
        /// Metoda, která zjistí, zda daný <see cref="Type"/> je/není formulář spustitelný z hlavního okna aplikace.
        /// Side effectem metody je střádání spustitelných formulářů společně s informacemi <see cref="RunTargetInfo"/>, které formuláře deklarují.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="runFormInfos"></param>
        /// <returns></returns>
        private static bool ContainsRunFormInfoAttribute(Type type, List<RunTargetInfo> runFormInfos)
        {
            if (type is null) return false;
            if (type.IsSubclassOf(typeof(System.Windows.Forms.Form)))
            {
                var attrs = type.GetCustomAttributes(typeof(RunFormInfoAttribute), true);
                if (attrs.Length > 0)
                {
                    RunTargetInfo runFormInfo = RunTargetInfo.CreateForFormAttribute(type, attrs[0] as RunFormInfoAttribute);
                    if (runFormInfo != null && runFormInfo.IsValid)
                        runFormInfos.Add(runFormInfo);
                }
            }

            return false;
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí instanci <see cref="RunTargetInfo"/> z dodaného atributu <see cref="RunFormInfoAttribute"/>
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="runFormInfoAttribute"></param>
        /// <returns></returns>
        private static RunTargetInfo CreateForFormAttribute(Type targetType, RunFormInfoAttribute runFormInfoAttribute)
        {
            if (runFormInfoAttribute is null) return null;
            RunTargetInfo runFormInfo = new RunTargetInfo();
            runFormInfo.PageText = runFormInfoAttribute.PageText;
            runFormInfo.PageOrder = runFormInfoAttribute.PageOrder;
            runFormInfo.GroupText = runFormInfoAttribute.GroupText;
            runFormInfo.GroupOrder = runFormInfoAttribute.GroupOrder;
            runFormInfo.ButtonText = runFormInfoAttribute.ButtonText;
            runFormInfo.ButtonOrder = runFormInfoAttribute.ButtonOrder;
            runFormInfo.ButtonImage = runFormInfoAttribute.ButtonImage;
            runFormInfo.ButtonToolTip = runFormInfoAttribute.ButtonToolTip;
            runFormInfo.RunAsFloating = runFormInfoAttribute.RunAsFloating;
            runFormInfo.RunAsModal = runFormInfoAttribute.RunAsModal;
            runFormInfo.TabViewToolTip = runFormInfoAttribute.TabViewToolTip;
            runFormInfo.RunType = TargetRunType.Form;
            runFormInfo.RunTargetType = targetType;
            return runFormInfo;
        }
        #endregion
        #region Tvorba deklarace Ribbonu
        /// <summary>
        /// Vygeneruje korektní definici prvků do Ribbonu za dané formuláře
        /// </summary>
        /// <param name="runFormInfos"></param>
        /// <param name="pages"></param>
        /// <param name="basicPage"></param>
        public static void CreateRibbonPages(RunTargetInfo[] runFormInfos,  List<DataRibbonPage> pages, DataRibbonPage basicPage)
        {
            if (runFormInfos is null || runFormInfos.Length == 0) return;

            // var rfiPages = runFormInfos.GroupBy(i => i.Item2.PageText ?? "").ToArray();

            // Grupy za stránky podle PageText, tříděné podle PageOrder, v rámci jedné stránky podle nejvyšší hodnoty PageOrder:
            var rPages = runFormInfos.CreateSortedGroups(i => (i.PageText ?? ""), i => (i.PageOrder), (a, b) => (a > b ? a : b));
            int pageOrder = basicPage.PageOrder;
            foreach (var rPage in rPages)
            {   // V daném pořadí, které vychází z PageOrder:
                // Stránka Ribbonu: defaultní nebo nová explicitně pojmenovaná:
                DataRibbonPage dxPage = null;
                if (rPage.Item1 == "")
                    dxPage = basicPage;
                else
                {
                    dxPage = new DataRibbonPage() { PageText = rPage.Item1, PageOrder = ++pageOrder };
                    pages.Add(dxPage);
                }

                // RibbonGrupy = z aktuální stránky rPage vezmu prvky (rPage.Item2), vytvořím skupiny podle GroupText a setřídím podle GroupOrder.Max() :
                var rGroups = rPage.Item2.CreateSortedGroups(i => (i.GroupText ?? ""), i => (i.GroupOrder), (a, b) => (a > b ? a : b));
                int groupOrder = 0;
                foreach (var rGroup in rGroups)
                {   // Jedna grupa za druhou, v pořadí GroupOrder:
                    string groupText = (!String.IsNullOrEmpty(rGroup.Item1) ? rGroup.Item1 : "FUNKCE");
                    DataRibbonGroup dxGroup = new DataRibbonGroup() { GroupText = groupText, GroupOrder = ++groupOrder };
                    dxPage.Groups.Add(dxGroup);

                    // Jednotlivá tlačítka do grupy:
                    var rButtons = rGroup.Item2.ToList();
                    rButtons.Sort((a, b) => a.ButtonOrder.CompareTo(b.ButtonOrder));
                    int buttonOrder = 0;
                    foreach (var rButton in rButtons)
                    {
                        var dxItem = new DataRibbonItem() { Text = rButton.ButtonText, ToolTipText = rButton.ButtonToolTip, ImageName = rButton.ButtonImage, ItemOrder = ++buttonOrder, Tag = rButton };
                        dxItem.ClickAction = RunFormAction;
                        dxGroup.Items.Add(dxItem);
                    }
                }
            }
        }
        /// <summary>
        /// Akce volaná z buttonu v Ribbonu, jejím úkolem je otevřít dané okno, jehož definice je uložena v Tagu dodaného prvku
        /// </summary>
        /// <param name="item"></param>
        private static void RunFormAction(IMenuItem item)
        {
            if (item.Tag is not RunTargetInfo runTargetInfo) return;

            runTargetInfo.Run();
        }
        /// <summary>
        /// Instanční metoda v <see cref="RunTargetInfo"/>, má za úkol otevřít patřičným způsobem svůj formulář
        /// </summary>
        private void Run()
        {
            try
            {
                var form = System.Activator.CreateInstance(this.RunTargetType) as System.Windows.Forms.Form;
                var configIsMdiChild = ((form is IFormStatusWorking iForm) ? iForm.ConfigIsMdiChild : (bool?)null);
                bool runAsFloating = (configIsMdiChild.HasValue ? !configIsMdiChild.Value : this.RunAsFloating);
                DxMainAppForm.ShowChildForm(form, runAsFloating, this.TabViewToolTip);
            }
            catch (Exception exc)
            {
                DxComponent.ShowMessageException(exc);
            }
        }
        #endregion
    }
    #region atribut RunFormInfoAttribute
    /// <summary>
    /// Dekorátor třídy, která chce mít svoji ikonu v Ribbonu v Main okně
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RunFormInfoAttribute : Attribute
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="pageOrder"></param>
        /// <param name="groupText"></param>
        /// <param name="groupOrder"></param>
        /// <param name="buttonText"></param>
        /// <param name="buttonOrder"></param>
        /// <param name="buttonImage"></param>
        /// <param name="buttonToolTip"></param>
        /// <param name="runAsFloating"></param>
        /// <param name="tabViewToolTip"></param>
        public RunFormInfoAttribute(string pageText = null, int pageOrder = 0, string groupText = null, int groupOrder = 0,
            string buttonText = null, int buttonOrder = 0, string buttonImage = null,
            string buttonToolTip = null, bool runAsFloating = false, string tabViewToolTip = null)
        {
            this.PageText = pageText;
            this.PageOrder = pageOrder;
            this.GroupText = groupText;
            this.GroupOrder = groupOrder;
            this.ButtonText = buttonText;
            this.ButtonOrder = buttonOrder;
            this.ButtonImage = buttonImage;
            this.ButtonToolTip = buttonToolTip;
            this.RunAsFloating = runAsFloating;
            this.TabViewToolTip = tabViewToolTip;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{GroupText}: {ButtonText}";
        }
        /// <summary>
        /// Název stránky, kde se bude button nacházet.
        /// null = výchozí: "ZÁKLADNÍ"
        /// </summary>
        public string PageText { get; private set; }
        /// <summary>
        /// Pořadí stránky.
        /// Když více prvků bude mít shodný text <see cref="PageText"/> a rozdélné pořadí <see cref="PageOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="PageOrder"/> v rámci stránky.
        /// </summary>
        public int PageOrder { get; private set; }
        /// <summary>
        /// Název grupy, kde se bude button nacházet.
        /// null = výchozí = "FUNKCE"
        /// </summary>
        public string GroupText { get; private set; }
        /// <summary>
        /// Pořadí grupy ve stránce.
        /// Když více prvků bude mít shodný text <see cref="GroupText"/> a rozdélné pořadí <see cref="GroupOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="GroupOrder"/> v rámci grupy.
        /// </summary>
        public int GroupOrder { get; private set; }
        /// <summary>
        /// Název buttonu = zobrazený text.
        /// Prázdný text je přípustný, pokud je dána ikona <see cref="ButtonImage"/>.
        /// </summary>
        public string ButtonText { get; private set; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        public int ButtonOrder { get; private set; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        public string ButtonImage { get; private set; }
        /// <summary>
        /// ToolTip k buttonu
        /// </summary>
        public string ButtonToolTip { get; private set; }
        /// <summary>
        /// Spouštět jako Modální okno
        /// </summary>
        public bool RunAsModal { get; private set; }
        /// <summary>
        /// Spouštět jako Floating okno
        /// </summary>
        public bool RunAsFloating { get; private set; }
        /// <summary>
        /// ToolTip k otevřenému oknu nad záložkou
        /// </summary>
        public string TabViewToolTip { get; private set; }
    }
    #endregion
    #endregion
}
