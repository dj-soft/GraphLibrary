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
using TestDevExpress.Components;

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
                var info = DxComponent.WinProcessInfo.GetCurent();
                string text = $"GDI: {info.GDIHandleCount}";
                string toolTip = info.Text4Full;
                var mics1 = DxComponent.LogGetTimeElapsed(time);

                if (!String.Equals(text, _StatusWin32InfoItem.Text))
                {
                    time = DxComponent.LogTimeCurrent;
                    _StatusWin32InfoItem.Text = text;
                    _StatusWin32InfoItem.Refresh(true);
                    var mics2 = DxComponent.LogGetTimeElapsed(time);
                    _StatusWin32InfoItem.ToolTipTitle = "Stav GDI objektů a paměti";
                    _StatusWin32InfoItem.ToolTipText = toolTip + $"\r\nZískání GDI informací: {mics1} microsec;\r\nRefresh statusbaru: {mics2} microsec;";     // Promítne se až příště.
                }
            }
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
}
