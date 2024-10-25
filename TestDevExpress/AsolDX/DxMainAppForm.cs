using DevExpress.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Předek hlavního okna aplikace.<br/>
    /// Obsahuje Ribbon a Status bar (od svého předka) a obsahuje Dock panel, Tab View a Document manager.
    /// </summary>
    public class DxMainAppForm : DxRibbonBaseForm
    {
        #region Konstruktor a statická reference na Main okno
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxMainAppForm() : base() 
        {
            this.WindowState = FormWindowState.Maximized;
            this.FirstShownAfter += _FirstShownAfter;
            __MainAppForm = new WeakTarget<DxMainAppForm>(this);
        }
        /// <summary>
        /// Po prvním zobrazení formuláře
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FirstShownAfter(object sender, EventArgs e)
        {
            this.OnFirstShownApplyDockPanelsSize();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            __MainAppForm = null;
            DisposeImageMapOnTabbedView(__TabbedView);
            base.Dispose(disposing);
        }
        /// <summary>
        /// Reference na okno aplikace
        /// </summary>
        public static DxMainAppForm MainAppForm
        {
            get
            {
                var wt = __MainAppForm;
                if (wt != null && wt.IsAlive && wt.Target != null)
                {
                    DxMainAppForm mainAppForm = wt.Target;
                    if (!mainAppForm.IsDisposed) return mainAppForm;
                }
                return null;
            }
        }
        private static WeakTarget<DxMainAppForm> __MainAppForm;
        #endregion
        #region Ribbon
        /// <summary>
        /// Příprava obsahu Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            pages.Add(this.CreateRibbonHomePage(FormRibbonDesignGroupPart.All));
            this.DxRibbon.AddPages(pages, true);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            switch (e.Item.ItemId)
            {
                case "": break;
            }
        }
        #endregion
        #region Hlavní obsah okna aplikace: DocumentManager, TabbedView, DockManager
        public DevExpress.XtraBars.Docking2010.DocumentManager DocumentManager { get { return __DocumentManager; } }
        public DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView TabbedView { get { return __TabbedView; } }
        public DevExpress.XtraBars.Docking.DockManager DockManager { get { return __DockManager; } }
        public DevExpress.XtraSplashScreen.SplashScreenManager SplashManager { get { return __SplashManager; } }
        /// <summary>
        /// Defaultní velikost Resize zóny na komponentě Dockpanel
        /// </summary>
        public static int DefaultResizeZoneThickness { get { return 10; } }
        /// <summary>
        ///  Provede tvorbu hlavního obsahue okna, podle jeho typu, a jeho přidání do okna včetně zadokování.
        /// Provádí se před vytvořením Ribbonu a Status baru, aby obsah byl správně umístěn na Z ose.
        /// </summary>
        protected override void DxMainContentCreate()
        {
            __DockManager = new DevExpress.XtraBars.Docking.DockManager();
            ((System.ComponentModel.ISupportInitialize)(__DockManager)).BeginInit();

            __DocumentManager = new DevExpress.XtraBars.Docking2010.DocumentManager();
            ((System.ComponentModel.ISupportInitialize)(__DocumentManager)).BeginInit();

            __TabbedView = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView();
            ((System.ComponentModel.ISupportInitialize)(__TabbedView)).BeginInit();

            __SplashManager = new DevExpress.XtraSplashScreen.SplashScreenManager(); // (this, typeof(global::Noris.Clients.WinForms.Forms.WaitFormLoadingDesktop), false, false, true);
        }
        /// <summary>
        /// Provede přípravu obsahu hlavního obshau okna. Obsah je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected override void DxMainContentPrepare()
        {
            SetupDocumentManager();
            SetupTabbedView();
            SetupDockManager();
            InitializeFinalControls();
            InitializeDockPanelsContent();
            DxMainContentPreparedAfter();
        }
        /// <summary>
        /// Po dokončení tvorby Dockmanageru, DocumentManageru, TabbedView a DockPanelů
        /// </summary>
        protected virtual void DxMainContentPreparedAfter() { }
        /// <summary>
        /// Nastavení komponenty DocumentManager
        /// </summary>
        protected virtual void SetupDocumentManager()
        {
            var docManager = __DocumentManager;
            docManager.MdiParent = this;
            docManager.View = __TabbedView;
            docManager.ShowThumbnailsInTaskBar = DevExpress.Utils.DefaultBoolean.False;
            docManager.SnapMode = DevExpress.Utils.Controls.SnapMode.All;
            docManager.RibbonAndBarsMergeStyle = DevExpress.XtraBars.Docking2010.Views.RibbonAndBarsMergeStyle.WhenNotFloating;

            docManager.DocumentActivate += _DocumentManagerDocumentActivate;
            docManager.ViewChanged += _DocumentManagerViewChanged;
        }
        /// <summary>
        /// Nastavení komponenty TabbedView
        /// </summary>
        protected virtual void SetupTabbedView()
        {
            var tabView = __TabbedView;
            tabView.Style = DevExpress.XtraBars.Docking2010.Views.DockingViewStyle.Classic;
            tabView.EnableFreeLayoutMode = DevExpress.Utils.DefaultBoolean.True;                   // Umožní dát dokování do skupin různě rozmístěných (vodorovně + svisle v sobě)
            tabView.CustomResizeZoneThickness = DefaultResizeZoneThickness;                        // Viditelný splitter mezi dokovanými skupinami
            tabView.EnableStickySplitters = DevExpress.Utils.DefaultBoolean.True;                  // Splitter mezi dokovanými skupinami se bude přichytávat k okolním splitterům
            tabView.ShowDockGuidesOnPressingShift = DevExpress.Utils.DefaultBoolean.False;         // Při snaze o zadokování Floating formu se zobrazí nápovědná ikona false=bez Shiftu / true = jen po stisknutí Shiftu

            InitializeImageMapOnTabbedView(__TabbedView);
           

            tabView.Controller.Manager.MaxThumbnailCount = 7;
            tabView.Controller.Manager.ShowThumbnailsInTaskBar = DevExpress.Utils.DefaultBoolean.True;


            tabView.DocumentProperties.AllowAnimation = true;
            tabView.DocumentProperties.AllowPin = true;
            tabView.DocumentProperties.AllowTabReordering = true;                                  // Musí být true, jinak nejde myší utrhnout tabované okno ani přemístit do jiné grupy.
            tabView.DocumentProperties.MaxTabWidth = 0;
            tabView.DocumentProperties.ShowPinButton = true;
            tabView.DocumentProperties.ShowInDocumentSelector = true;
            tabView.DocumentProperties.UseFormIconAsDocumentImage = true;
            if (tabView.DocumentProperties is DevExpress.XtraBars.Docking2010.Views.Tabbed.DocumentProperties docProp)
            {   // docProp jde nad rámec interface IDocumentProperties:
            }

            tabView.DocumentGroupProperties.HeaderButtonsShowMode = DevExpress.XtraTab.TabButtonShowMode.WhenNeeded;
            tabView.DocumentGroupProperties.PinPageButtonShowMode = DevExpress.XtraTab.PinPageButtonShowMode.InActiveTabPageHeaderAndOnMouseHover;
            tabView.DocumentGroupProperties.ShowDocumentSelectorButton = true;
            tabView.DocumentGroupProperties.DestroyOnRemovingChildren = true;  // Po zavření posledního okna v grupě se zruší i grupa
            tabView.DocumentGroupProperties.CloseTabOnMiddleClick = DevExpress.XtraTabbedMdi.CloseTabOnMiddleClick.OnMouseUp;
            tabView.DocumentGroupProperties.HeaderAutoFill = DevExpress.Utils.DefaultBoolean.False;

            if (tabView.DocumentGroupProperties is DevExpress.XtraBars.Docking2010.Views.Tabbed.DocumentGroupProperties groupProp)
            {   // groupProp jde nad rámec interface IDocumentGroupProperties:
            }

            tabView.ShowDocumentSelectorMenuOnCtrlAltDownArrow = DevExpress.Utils.DefaultBoolean.True;
            tabView.DocumentSelectorProperties.ShowPreview = true;
            tabView.UseDocumentSelector = DevExpress.Utils.DefaultBoolean.True;

            tabView.DocumentGroups.CollectionChanged += _TabbedViewGroupsCollectionChanged;
            tabView.Layout += _TabbedViewLayout;
            tabView.Paint += _TabbedViewPaint;
            tabView.EndSizing += _TabbedViewEndSizing;
            tabView.Floating += _TabbedViewFloating;
            tabView.BeginDocking += _TabbedViewBeginDocking;
            tabView.EndDocking += _TabbedViewEndDocking;
            tabView.DocumentAdded += _TabbedViewDocumentAdded;
            tabView.BeginFloating += _TabbedViewBeginFloating;
            tabView.BeginSizing += _TabbedViewBeginSizing;
            tabView.ControlShown += _TabbedViewControlShown;
            tabView.DocumentActivated += _TabbedViewDocumentActivated;
            tabView.DocumentClosed += _TabbedViewDocumentClosed;
            tabView.DocumentDeactivated += _TabbedViewDocumentDeactivated;
            tabView.DocumentClosing += _TabbedViewDocumentClosing;
            tabView.DocumentRemoved += _TabbedViewDocumentRemoved;
            tabView.EndFloating += _TabbedViewEndFloating;
            tabView.EmptyDocumentsHostWindow += _TabbedViewEmptyDocumentsHostWindow;
            tabView.GotFocus += _TabbedViewGotFocus;
            tabView.LostFocus += _TabbedViewLostFocus;
            tabView.NextDocument += _TabbedViewNextDocument;

        }
        /// <summary>
        /// Inicializace komponenty DockManager
        /// </summary>
        protected virtual void SetupDockManager()
        {
            var dockMgr = __DockManager;
            dockMgr.DockingOptions.CustomResizeZoneThickness = DefaultResizeZoneThickness;
            dockMgr.DockingOptions.HidePanelsImmediately = DevExpress.XtraBars.Docking.Helpers.HidePanelsImmediatelyMode.Always;   // Vyjetí pravého panelu až na kliknutí; vyjetí na kliknutí + vypnutí animací
            dockMgr.DockingOptions.AllowDockToCenter = DevExpress.Utils.DefaultBoolean.False;
            dockMgr.DockingOptions.FloatOnDblClick = false;
            dockMgr.DockingOptions.ShowAutoHideButton = true;
            dockMgr.DockingOptions.ShowCloseButton = false;
            dockMgr.DockingOptions.ShowMaximizeButton = false;
            dockMgr.DockingOptions.ShowMinimizeButton = false;
            dockMgr.DockingOptions.SnapMode = DevExpress.Utils.Controls.SnapMode.OwnerForm;

            dockMgr.AutoHiddenPanelShowMode = DevExpress.XtraBars.Docking.AutoHiddenPanelShowMode.MouseClick;
            dockMgr.DockingOptions.AutoHidePanelVerticalTextOrientation = DevExpress.XtraBars.Docking.VerticalTextOrientation.BottomToTop;
            dockMgr.DockingOptions.TabbedPanelVerticalTextOrientation = DevExpress.XtraBars.Docking.VerticalTextOrientation.BottomToTop;

            dockMgr.RegisterDockPanel += _DockManagerRegisterDockPanel;
            dockMgr.ActiveChildChanged += _DockManagerActiveChildChanged;
            dockMgr.ActivePanelChanged += _DockManagerActivePanelChanged;
            dockMgr.ClosedPanel += _DockManagerClosedPanel;
            dockMgr.Collapsing += _DockManagerCollapsing;
            dockMgr.Collapsed += _DockManagerCollapsed;
            dockMgr.ClosingPanel += _DockManagerClosingPanel;
            dockMgr.Docking += _DockManagerDocking;
            dockMgr.EndDocking += _DockManagerEndDocking;
            dockMgr.Expanding += _DockManagerExpanding;
            dockMgr.Expanded += _DockManagerExpanded;
            dockMgr.StartDocking += _DockManagerStartDocking;
            dockMgr.StartSizing += _DockManagerStartSizing;
            dockMgr.EndSizing += _DockManagerEndSizing;
            dockMgr.TabbedChanged += _DockManagerTabbedChanged;
            dockMgr.TabsPositionChanged += _DockManagerTabsPositionChanged;
            dockMgr.TabsScrollChanged += _DockManagerTabsScrollChanged;
            dockMgr.VisibilityChanged += _DockManagerVisibilityChanged;
        }
        /// <summary>
        /// Vytvoří obsah Dock panelů
        /// </summary>
        protected virtual void InitializeDockPanelsContent() { }
        /// <summary>
        /// Závěrečná fáze inicializace formuláře: 
        /// správné poskládání komponent do sebe navzájem a do Formuláře do jeho Controls, ve správném pořadí.
        /// Ukončení inicializační fáze, EndEnit a ResumeLayout.
        /// <para/>
        /// Na pořadí ZÁLEŽÍ!
        /// </summary>
        private void InitializeFinalControls()
        {
            // Na pořadí ZÁLEŽÍ!

            // DockManager do okna:
            this.__DockManager.Form = this;
            this.__DockManager.TopZIndexControls.AddRange(new string[]
            {
                "DevExpress.XtraBars.BarDockControl", "DevExpress.XtraBars.StandaloneBarDockControl", "System.Windows.Forms.StatusBar", "System.Windows.Forms.MenuStrip", "System.Windows.Forms.StatusStrip",
                "DevExpress.XtraBars.Ribbon.RibbonStatusBar", "Noris.Clients.Win.Components.AsolDX.DxRibbonStatusBar",
                "DevExpress.XtraBars.Ribbon.RibbonControl",   "Noris.Clients.Win.Components.AsolDX.DxRibbonControl",
                "DevExpress.XtraBars.Navigation.OfficeNavigationBar", "DevExpress.XtraBars.Navigation.TileNavPane", "DevExpress.XtraBars.TabFormControl",
                "DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl", "DevExpress.XtraBars.ToolbarForm.ToolbarFormControl"
            });

            this.__DocumentManager.MdiParent = this;
            this.__DocumentManager.MenuManager = this.DxRibbon;
            this.__DocumentManager.View = this.__TabbedView;
            this.__DocumentManager.ViewCollection.AddRange(new DevExpress.XtraBars.Docking2010.Views.BaseView[] { this.__TabbedView });
        }
        /// <summary>
        /// Je voláno na konci konstruktoru třídy <see cref="DxRibbonBaseForm"/>.
        /// Typicky je zde ukončen cyklus BeginInit jednoltivých komponent.
        /// <para/>
        /// Je povinné volat base metodu, typicky na konci metody override.
        /// </summary>
        protected override void EndInitDxRibbonForm()
        {
            ((System.ComponentModel.ISupportInitialize)(__TabbedView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(__DocumentManager)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(__DockManager)).EndInit();

            base.EndInitDxRibbonForm();
        }
        private DevExpress.XtraBars.Docking2010.DocumentManager __DocumentManager;
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView __TabbedView;
        private DevExpress.XtraBars.Docking.DockManager __DockManager;
        private DevExpress.XtraSplashScreen.SplashScreenManager __SplashManager;

        private void _DocumentManagerDocumentActivate(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DocumentManager.DocumentActivate({e.Document?.Control?.Text})");
            // ActivateRibbonForControl(e.Document?.Control);
        }
        private void _DocumentManagerViewChanged(object sender, DevExpress.XtraBars.Docking2010.ViewEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DocumentManager.ViewChanged()");
        }
        private void _DocumentManagerBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DocumentManager.BeginFloating({e.Document.Control?.Text})");
        }

        private void _TabbedViewDocumentAdded(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentAdded({e.Document.Control?.Text})");
        }
        private void _TabbedViewDocumentClosing(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentClosing({e.Document.Control?.Text})");
        }
        private void _TabbedViewDocumentRemoved(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentRemoved({e.Document.Control?.Text})");
        }
        private void _TabbedViewEndFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.EndFloating({e.Document.Control?.Text})");
        }
        private void _TabbedViewNextDocument(object sender, DevExpress.XtraBars.Docking2010.Views.NextDocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.NextDocument({e.Document.Control?.Text})");
        }
        private void _TabbedViewLostFocus(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.LostFocus()");
        }
        private void _TabbedViewGotFocus(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.GotFocus()");
        }
        private void _TabbedViewEmptyDocumentsHostWindow(object sender, DevExpress.XtraBars.Docking2010.EmptyDocumentsHostWindowEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentsHostWindow({e.Reason})");
        }
        private void _TabbedViewDocumentDeactivated(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentDeactivated({e.Document.Control?.Text})");
            DeactivateRibbonForControl(e.Document?.Control);
        }
        private void _TabbedViewDocumentClosed(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentClosed({e.Document.Control?.Text})");
        }
        private void _TabbedViewDocumentActivated(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.DocumentActivated({e.Document.Control?.Text})");
            ActivateRibbonForControl(e.Document?.Control);
        }
        private void _TabbedViewControlShown(object sender, DevExpress.XtraBars.Docking2010.Views.DeferredControlLoadEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.ControlShown({e.Document.Control?.Text})");
        }
        private void _TabbedViewBeginSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutBeginSizingEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.BeginSizing()");
        }
        private void _TabbedViewBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.BeginFloating({e.Document.Control?.Text})");
            BeginFloatingRibbonForControl(e.Document.Control);
        }
        private void _TabbedViewGroupsCollectionChanged(DevExpress.XtraBars.Docking2010.Base.CollectionChangedEventArgs<DevExpress.XtraBars.Docking2010.Views.Tabbed.DocumentGroup> e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.GroupsCollectionChanged()");
        }
        private void _TabbedViewLayout(object sender, EventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.Layout()");
        }
        private void _TabbedViewPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // DxComponent.LogAddLine($"TabbedView.GroupsCollectionChanged({e.Document.Control?.Text})");
        }
        private void _TabbedViewEndSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutEndSizingEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.EndSizing()");
        }
        private void _TabbedViewFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.Floating({e.Document.Control?.Text})");
        }
        private void _TabbedViewBeginDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.BeginDocking({e.Document.Control?.Text})");
        }
        private void _TabbedViewEndDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"TabbedView.EndDocking({e.Document.Control?.Text})");
        }

        private void _DockManagerRegisterDockPanel(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.RegisterDockPanel({e.Panel?.Text})");
        }
        private void _DockManagerEndSizing(object sender, DevExpress.XtraBars.Docking.EndSizingEventArgs e) 
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.EndSizing({e.Panel?.Text})");
        }
        private void _DockManagerClosingPanel(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e) 
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.ClosingPanel({e.Panel?.Text})");
        }
        private void _DockManagerVisibilityChanged(object sender, DevExpress.XtraBars.Docking.VisibilityChangedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.VisibilityChanged({e.Panel?.Text})");
        }
        private void _DockManagerTabsScrollChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.TabsScrollChanged({e.Panel?.Text})");
        }
        private void _DockManagerTabsPositionChanged(object sender, DevExpress.XtraBars.Docking.TabsPositionChangedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.TabsPositionChanged({e.Panel?.Text})");
        }
        private void _DockManagerTabbedChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.TabbedChanged({e.Panel?.Text})");
        }
        private void _DockManagerStartSizing(object sender, DevExpress.XtraBars.Docking.StartSizingEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.StartSizing({e.Panel?.Text})");
        }
        private void _DockManagerStartDocking(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.StartDocking({e.Panel?.Text})");
        }
        private void _DockManagerExpanded(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.Expanded({e.Panel?.Text})");
        }
        private void _DockManagerExpanding(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.Expanding({e.Panel?.Text})");
        }
        private void _DockManagerEndDocking(object sender, DevExpress.XtraBars.Docking.EndDockingEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.EndDocking({e.Panel?.Text})");
        }
        private void _DockManagerDocking(object sender, DevExpress.XtraBars.Docking.DockingEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.Docking({e.Panel?.Text})");
        }
        private void _DockManagerCollapsed(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.Collapsed({e.Panel?.Text})");
        }
        private void _DockManagerCollapsing(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.Collapsing({e.Panel?.Text})");
        }
        private void _DockManagerClosedPanel(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.ClosedPanel({e.Panel?.Text})");
        }
        private void _DockManagerActivePanelChanged(object sender, DevExpress.XtraBars.Docking.ActivePanelChangedEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.ActivePanelChanged({e.Panel?.Text})");
        }
        private void _DockManagerActiveChildChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine(LogActivityKind.DocumentManager, $"DockManager.ActiveChildChanged({e.Panel?.Text})");
        }

        private void ActivateRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm && ribbonForm.MdiParent != null)
            {
                this.DxRibbon.MergeChildRibbon(ribbonForm.Ribbon);
                DxComponent.LogAddLine(LogActivityKind.Ribbon, $"Ribbon.Desktop.MergeChildRibbon({ribbonForm?.Text})");
                ribbonForm.Ribbon.Visible = true;
                DxComponent.LogAddLine(LogActivityKind.Ribbon, $"Ribbon.TabbedForm[{ribbonForm?.Text}].Visible = true");
            }
        }
        private void BeginFloatingRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm)
            {
                this.DxRibbon.UnMergeRibbon();
                DxComponent.LogAddLine(LogActivityKind.Ribbon, $"Ribbon.Desktop.UnMergeRibbon({ribbonForm?.Text})");
                ribbonForm.Ribbon.Visible = true;
                DxComponent.LogAddLine(LogActivityKind.Ribbon, $"Ribbon.FloatingForm[{ribbonForm?.Text}].Visible = true");
            }
        }
        private void DeactivateRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm && ribbonForm.MdiParent != null)
            {
                this.DxRibbon.UnMergeRibbon();
                DxComponent.LogAddLine(LogActivityKind.Ribbon, $"Ribbon.Desktop.UnMergeRibbon({ribbonForm?.Text})");
                ribbonForm.Ribbon.Visible = false;
                DxComponent.LogAddLine(LogActivityKind.Ribbon, $"Ribbon.TabbedForm[{ribbonForm?.Text}].Visible = false");
            }
        }
        #endregion
        #region TabView BackImage a MouseClick
        /// <summary>
        /// Inicializuje věci pro kreslení obrázku na pozadí TabView
        /// </summary>
        /// <param name="tabbedView"></param>
        protected virtual void InitializeImageMapOnTabbedView(DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView)
        {
        }
        /// <summary>
        /// Disposuje věci pro kreslení obrázku na pozadí TabView
        /// </summary>
        protected virtual void DisposeImageMapOnTabbedView(DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView)
        {
        }
        #endregion
        #region DockManager - služby
        /// <summary>
        /// Po prvním zobrazení formuláře aplikuje velikosti DockPanelů
        /// </summary>
        protected virtual void OnFirstShownApplyDockPanelsSize()
        {
            this._Panels.ForEachExec(p => p.ApplySizeAndVisibility());
        }
        /// <summary>
        /// Přidá nový dokovaný panel
        /// </summary>
        /// <param name="control"></param>
        /// <param name="panelTitle"></param>
        /// <param name="dockStyle"></param>
        /// <param name="panelSize">Velikost panelu; použije se ta hodnota, která je vhodná s ohledem na <paramref name="dockStyle"/></param>
        /// <param name="visibility"></param>
        /// <param name="imageName">Jméno ikony</param>
        public void AddControlToDockPanels(Control control, string panelTitle, DevExpress.XtraBars.Docking.DockingStyle dockStyle, Size? panelSize = null, DevExpress.XtraBars.Docking.DockVisibility visibility = DevExpress.XtraBars.Docking.DockVisibility.AutoHide, string imageName = null)
        {
            var dockPanelLayout = new DockPanelLayoutInfo()
            {
                UserControl = control,
                PanelTitle = panelTitle,
                DockStyle = dockStyle,
                PanelSize = panelSize,
                Visibility = visibility,
                ImageName = imageName
            };
            AddControlToDockPanels(dockPanelLayout);
        }
        /// <summary>
        /// Přidá nový dokovaný panel
        /// </summary>
        /// <param name="dockPanelLayout"></param>
        public void AddControlToDockPanels(DockPanelLayoutInfo dockPanelLayout)
        {
            DevExpress.XtraBars.Docking.DockPanel panel;

            var dockStyle = dockPanelLayout.DockStyle ?? DevExpress.XtraBars.Docking.DockingStyle.Right;
            var rootPanel = this.__DockManager.RootPanels.FirstOrDefault(p => p.Dock == dockStyle);
            if (rootPanel is null)
            {
                rootPanel = this.__DockManager.AddPanel(dockStyle);
                panel = rootPanel;
            }
            else
            {
                panel = rootPanel.AddPanel();
            }

            panel.Text = dockPanelLayout.PanelTitle;

            dockPanelLayout.UserControl.Dock = DockStyle.Fill;
            panel.Controls.Add(dockPanelLayout.UserControl);

            DxComponent.ApplyImage(panel.ImageOptions, dockPanelLayout.ImageName);
            panel.ImageOptions.SvgImageSize = new System.Drawing.Size(20, 20);

            this.__DockManager.DockingOptions.ShowCaptionImage = true;

            dockPanelLayout.DockStyle = dockStyle;                   // Reálná hodnota namísto Null
            dockPanelLayout.Visibility = panel.Visibility;           // Reálná hodnota namísto Null

            dockPanelLayout.DockPanel = panel;
            dockPanelLayout.HostForm = this;
            dockPanelLayout.ApplySizeAndVisibility();
            _Panels.Add(dockPanelLayout);

            panel.DockChanged += dockPanelLayout.Panel_DockChanged;
            panel.SizeChanged += dockPanelLayout.Panel_SizeChanged;
            panel.VisibilityChanged += dockPanelLayout.Panel_VisibilityChanged;

            dockPanelLayout.CheckChanged();
        }
        /// <summary>
        /// Metoda vrátí panel, v němž je nyní umístěn daný Control.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.Docking.DockPanel GetDockPanelForControl(Control control)
        {
            return _Panels.FirstOrDefault(p => Object.ReferenceEquals(p.UserControl, control))?.DockPanel;
        }
        /// <summary>
        /// Metoda vrátí panel, v němž je nyní umístěn control který vyhoví dodané podmínce.
        /// Volající sám může testovat control (testovat jeho typ, jeho Name, jeho Tag...).
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.Docking.DockPanel GetDockPanelForControl(Func<Control, bool> predicate)
        {
            return _Panels.FirstOrDefault(p => predicate(p.UserControl))?.DockPanel;
        }
        /// <summary>
        /// Seznam zadokovaných controlů a panelů, na nichž jsou dokovány
        /// </summary>
        private List<DockPanelLayoutInfo> _Panels
        {
            get
            {
                if (__Panels is null)
                    __Panels = new List<DockPanelLayoutInfo>();
                return __Panels;
            }
        }
        private List<DockPanelLayoutInfo> __Panels;
        #endregion
        #region Otevírání Child okna v okně aplikace
        /// <summary>
        /// Otevři okno v prostředí aplikace
        /// </summary>
        /// <param name="form"></param>
        /// <param name="showFloating"></param>
        /// <param name="toolTip"></param>
        public static void ShowChildForm(Form form, bool showFloating, string toolTip)
        {
            var mainAppForm = MainAppForm;
            if (mainAppForm != null)
            {
                // form.MdiParent = mainAppForm;
                DevExpress.XtraBars.Docking2010.Views.Tabbed.Document document;
                if (!showFloating)
                    document = mainAppForm.__TabbedView.AddDocument(form) as DevExpress.XtraBars.Docking2010.Views.Tabbed.Document;
                else
                    document = mainAppForm.__TabbedView.AddFloatDocument(form) as DevExpress.XtraBars.Docking2010.Views.Tabbed.Document;

                if (!String.IsNullOrEmpty(toolTip))
                    document.Tooltip = toolTip;

                form.Show();
            }
            else
            {
                form.Show();
            }
        }
        #endregion
    }
    #region DockPanelLayoutInfo : informace o umístění a velikosti dokovaného panelu
    /// <summary>
    /// Sada informací o dokovaném controlu
    /// </summary>
    public class DockPanelLayoutInfo
    {
        #region Data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DockPanelLayoutInfo() { }
        /// <summary>
        /// Text obsahující Název panelu pro uživatele
        /// </summary>
        public string PanelTitle { get; set; }
        /// <summary>
        /// Jméno obrázku
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Styl dokování (kde je umístěn)
        /// </summary>
        public DevExpress.XtraBars.Docking.DockingStyle? DockStyle { get; set; }
        /// <summary>
        /// Pořadí panelu v rámci dokované strany
        /// </summary>
        public int DockOrder { get; set; }
        /// <summary>
        /// Požadovaná / aktuální velikost panelu
        /// </summary>
        public Size? PanelSize { get; set; }
        /// <summary>
        /// Main formulář, který řídí dokování
        /// </summary>
        public DxMainAppForm HostForm { get; set; }
        /// <summary>
        /// Dokovací panel, kde je <see cref="UserControl"/> umístěn
        /// </summary>
        public DevExpress.XtraBars.Docking.DockPanel DockPanel { get; set; }
        /// <summary>
        /// Fyzický zobrazený Control
        /// </summary>
        public Control UserControl { get; set; }
        /// <summary>
        /// Požadovaná / aktuální viditelnost panelu
        /// </summary>
        public DevExpress.XtraBars.Docking.DockVisibility? Visibility { get; set; }
        #endregion
        #region Eventhandlery
        /// <summary>
        /// Ověří zda nedošlo ke změně hodnot (Dock a Size) a případně vyvolá událost 
        /// </summary>
        public void CheckChanged() { _CheckLayoutChanged(); }
        /// <summary>
        /// Po změně <see cref="DockPanel"/>.Dock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Panel_DockChanged(object sender, EventArgs e) { _CheckLayoutChanged(); }
        /// <summary>
        /// Po změně <see cref="DockPanel"/>.Size
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Panel_SizeChanged(object sender, EventArgs e) { _CheckLayoutChanged(); }
        /// <summary>
        /// Po změně <see cref="DockPanel"/>.Visibility
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Panel_VisibilityChanged(object sender, DevExpress.XtraBars.Docking.VisibilityChangedEventArgs e) { _CheckLayoutChanged(); }
        /// <summary>
        /// Porovná fyzický stav panelu a data, aktualizuje data a volá <see cref="LayoutChanged"/>
        /// </summary>
        private void _CheckLayoutChanged()
        {
            if (__SuppressSizeAndVisibilityChange) return;

            bool wasShown = this.HostForm?.WasShown ?? false;
            if (!wasShown)
            {   // Okno dosud nebylo zobrazeno => nejde o změnu od uživatele, ale od WinFormu (typicky EndInit => PerformLayout):
                // ApplySizeAndVisibility();
            }
            else
            {   // Po zobrazení okna budeme reálně hlídat hodnoty z panelu a ukládat je do this properties, a volat eventhandler LayoutChanged:
                var oldDock = this.DockStyle;
                var newDock = this.DockPanel.Dock;
                var oldSize = this.PanelSize;
                var newSize = this.DockPanel.Size;
                var oldVisibility = this.Visibility;
                var newVisibility = this.DockPanel.Visibility;

                bool isChanged = false;

                if (!oldDock.HasValue || (oldDock.HasValue && oldDock.Value != newDock)) { this.DockStyle = newDock; isChanged = true; }
                if (!oldSize.HasValue || (oldSize.HasValue && !isEqualSize(oldSize.Value, newSize, newDock))) { this.PanelSize = newSize; isChanged = true; }
                if (!oldVisibility.HasValue || (oldVisibility.HasValue && oldVisibility.Value != newVisibility)) { this.Visibility = newVisibility; isChanged = true; }

                if (isChanged) LayoutChanged?.Invoke(this, EventArgs.Empty);
            }

            bool isEqualSize(Size s1, Size s2, DevExpress.XtraBars.Docking.DockingStyle style)
            {
                switch (style)
                {
                    case DevExpress.XtraBars.Docking.DockingStyle.Top:
                    case DevExpress.XtraBars.Docking.DockingStyle.Bottom:
                        return s1.Height == s2.Height;
                    case DevExpress.XtraBars.Docking.DockingStyle.Left:
                    case DevExpress.XtraBars.Docking.DockingStyle.Right:
                        return s1.Width == s2.Width;
                    case DevExpress.XtraBars.Docking.DockingStyle.Float:
                        return s1.Width == s2.Width && s1.Height == s2.Height;
                    case DevExpress.XtraBars.Docking.DockingStyle.Fill:
                        return true;
                }
                return true;
            }
        }
        /// <summary>
        /// Do panelu <see cref="DockPanel"/> aplikuje velikost <see cref="PanelSize"/> podle stylu dokování <see cref="DockStyle"/>.
        /// </summary>
        public void ApplySizeAndVisibility()
        {
            if (__SuppressSizeAndVisibilityChange) return;
            bool formWasShown = this.HostForm?.WasShown ?? false;
            if (!formWasShown) return;

            __SuppressSizeAndVisibilityChange = true;
            try
            {
                var dockStyle = this.DockStyle ?? DevExpress.XtraBars.Docking.DockingStyle.Right;
                var panelSize = this.PanelSize;
                var panel = this.DockPanel;
                var container = this.HostForm.DockManager.Panels;
                switch (dockStyle)
                {
                    case DevExpress.XtraBars.Docking.DockingStyle.Top:
                    case DevExpress.XtraBars.Docking.DockingStyle.Bottom:
                        int h = panelSize?.Height ?? 150;
                        if (panel.Height != h) panel.Height = h;
                        break;
                    case DevExpress.XtraBars.Docking.DockingStyle.Left:
                    case DevExpress.XtraBars.Docking.DockingStyle.Right:
                        int w = panelSize?.Width ?? 270;
                        if (panel.Width != w) panel.Width = w;
                        break;
                    case DevExpress.XtraBars.Docking.DockingStyle.Float:
                        Size s = panelSize ?? new Size(480, 200);
                        if (panel.Size != s) panel.Size = s;
                        break;
                }

                var v = this?.Visibility ?? DevExpress.XtraBars.Docking.DockVisibility.AutoHide; ;
                if (panel.Visibility != v) panel.Visibility = v;
            }
            finally
            {
                __SuppressSizeAndVisibilityChange = false;
            }
        }
        /// <summary>
        /// Příznak, že provádíme změnu Size a Visibility v <see cref="DockPanel"/>, a máme ignorovat eventy, které to způsobuje
        /// </summary>
        private bool __SuppressSizeAndVisibilityChange;
        /// <summary>
        /// Událost volaná po změně dokování nebo velikosti nebo viditelnosti
        /// </summary>
        public event EventHandler LayoutChanged;
        #endregion
        #region Serializace
        /// <summary>
        /// Z dodaného stringu vytvoří a naplní new instanci, vrací true. Pokud vstupní formát není platný, vrací false.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="layoutInfo"></param>
        /// <returns></returns>
        public static bool TryLoadFrom(string definition, out DockPanelLayoutInfo layoutInfo)
        {
            if (_TryParseDefinition(definition, out DevExpress.XtraBars.Docking.DockingStyle? dockStyle, out Size? size, out DevExpress.XtraBars.Docking.DockVisibility? visibility))
            {
                layoutInfo = new DockPanelLayoutInfo()
                {
                    DockStyle = dockStyle,
                    PanelSize = size,
                    Visibility = visibility
                };
                return true;
            }
            layoutInfo = null;
            return false;
        }
        /// <summary>
        /// Stringová definice. Lze setovat, setování ??? změní stav ???
        /// </summary>
        public string Definition
        {
            get { return _CreateDefinition(this.DockStyle, this.PanelSize, this.Visibility); }
            set 
            {
                if (_TryParseDefinition(value, out DevExpress.XtraBars.Docking.DockingStyle? dockStyle, out Size? size, out DevExpress.XtraBars.Docking.DockVisibility? visibility))
                {
                    this.DockStyle = dockStyle;
                    this.PanelSize = size;
                    this.Visibility = visibility;
                }
            }
        }
        /// <summary>
        /// Serializuje dodané hodnoty do stringu
        /// </summary>
        /// <param name="dockStyle"></param>
        /// <param name="size"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        private static string _CreateDefinition(DevExpress.XtraBars.Docking.DockingStyle? dockStyle, Size? size, DevExpress.XtraBars.Docking.DockVisibility? visibility)
        {
            string d = dockStyle?.ToString() ?? "#";
            string s = size.HasValue ? Convertor.SizeToString(size.Value, '×') : "#";
            string v = visibility?.ToString() ?? "#";
            return $"{d};{s};{v}";
        }
        /// <summary>
        /// Deserializuje definici ze stringu do hodnot
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="dockStyle"></param>
        /// <param name="size"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        private static bool _TryParseDefinition(string definition, out DevExpress.XtraBars.Docking.DockingStyle? dockStyle, out Size? size, out DevExpress.XtraBars.Docking.DockVisibility? visibility)
        {
            if (!String.IsNullOrEmpty(definition))
            {
                var parts = definition.Split(';');
                if (parts.Length >= 3)
                {
                    if ((Enum.TryParse(parts[0], false, out DevExpress.XtraBars.Docking.DockingStyle d)) &&
                        (Enum.TryParse(parts[2], false, out DevExpress.XtraBars.Docking.DockVisibility v)))
                    {
                        var c = Convertor.StringToSize(parts[1], '×');
                        if (c is Size s && (s.Width > 0 || s.Height > 0))
                        {
                            dockStyle = d;
                            size = s;
                            visibility = v;
                            return true;
                        }
                    }
                }
            }
            dockStyle = null;
            size = null;
            visibility = null;
            return false;
        }
        #endregion
    }
    #endregion
}
