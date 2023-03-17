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
            __MainAppForm = new WeakTarget<DxMainAppForm>(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            __MainAppForm = null;
            _TabViewBackImageDispose(__TabbedView);
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
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ" };
            pages.Add(page);
            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: false) as DataRibbonGroup;
            page.Groups.Add(group);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "": break;
            }
        }
        #endregion
        #region Hlavní obsah okna aplikace: DocumentManager, TabbedView, DockManager
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
        }
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

            var cc = docManager.ClientControl;

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

            _TabViewBackImageInit(__TabbedView);
           

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

        private void ClientControl_MouseClick(object sender, MouseEventArgs e)
        {
            
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
            DxComponent.LogAddLine($"DocumentManager.DocumentActivate({e.Document?.Control?.Text})");
            // ActivateRibbonForControl(e.Document?.Control);
        }
        private void _DocumentManagerViewChanged(object sender, DevExpress.XtraBars.Docking2010.ViewEventArgs e)
        {
            DxComponent.LogAddLine($"DocumentManager.ViewChanged()");
        }
        private void _DocumentManagerBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine($"DocumentManager.BeginFloating({e.Document.Control?.Text})");
        }



        private void _TabbedViewDocumentAdded(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentAdded({e.Document.Control?.Text})");
        }
        private void _TabbedViewDocumentClosing(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentClosing({e.Document.Control?.Text})");
        }
        private void _TabbedViewDocumentRemoved(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentRemoved({e.Document.Control?.Text})");
        }
        private void _TabbedViewEndFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.EndFloating({e.Document.Control?.Text})");
        }
        private void _TabbedViewNextDocument(object sender, DevExpress.XtraBars.Docking2010.Views.NextDocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.NextDocument({e.Document.Control?.Text})");
        }
        private void _TabbedViewLostFocus(object sender, EventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.LostFocus()");
        }
        private void _TabbedViewGotFocus(object sender, EventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.GotFocus()");
        }
        private void _TabbedViewEmptyDocumentsHostWindow(object sender, DevExpress.XtraBars.Docking2010.EmptyDocumentsHostWindowEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentsHostWindow({e.Reason})");
        }
        private void _TabbedViewDocumentDeactivated(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentDeactivated({e.Document.Control?.Text})");
            DeactivateRibbonForControl(e.Document?.Control);
        }
        private void _TabbedViewDocumentClosed(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentClosed({e.Document.Control?.Text})");
        }
        private void _TabbedViewDocumentActivated(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.DocumentActivated({e.Document.Control?.Text})");
            ActivateRibbonForControl(e.Document?.Control);
        }
        private void _TabbedViewControlShown(object sender, DevExpress.XtraBars.Docking2010.Views.DeferredControlLoadEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.ControlShown({e.Document.Control?.Text})");
        }
        private void _TabbedViewBeginSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutBeginSizingEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.BeginSizing()");
        }
        private void _TabbedViewBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.BeginFloating({e.Document.Control?.Text})");
            BeginFloatingRibbonForControl(e.Document.Control);
        }
        private void _TabbedViewGroupsCollectionChanged(DevExpress.XtraBars.Docking2010.Base.CollectionChangedEventArgs<DevExpress.XtraBars.Docking2010.Views.Tabbed.DocumentGroup> e)
        {
            DxComponent.LogAddLine($"TabbedView.GroupsCollectionChanged()");
        }
        private void _TabbedViewLayout(object sender, EventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.Layout()");
        }
        private void _TabbedViewPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // DxComponent.LogAddLine($"TabbedView.GroupsCollectionChanged({e.Document.Control?.Text})");
        }
        private void _TabbedViewEndSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutEndSizingEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.EndSizing()");
        }
        private void _TabbedViewFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.Floating({e.Document.Control?.Text})");
        }
        private void _TabbedViewBeginDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.BeginDocking({e.Document.Control?.Text})");
        }
        private void _TabbedViewEndDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DxComponent.LogAddLine($"TabbedView.EndDocking({e.Document.Control?.Text})");
        }


        private void _DockManagerRegisterDockPanel(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.RegisterDockPanel({e.Panel?.Text})");
        }
        private void _DockManagerEndSizing(object sender, DevExpress.XtraBars.Docking.EndSizingEventArgs e) 
        {
            DxComponent.LogAddLine($"DockManager.EndSizing({e.Panel?.Text})");
        }
        private void _DockManagerClosingPanel(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e) 
        {
            DxComponent.LogAddLine($"DockManager.ClosingPanel({e.Panel?.Text})");
        }
        private void _DockManagerVisibilityChanged(object sender, DevExpress.XtraBars.Docking.VisibilityChangedEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.VisibilityChanged({e.Panel?.Text})");
        }
        private void _DockManagerTabsScrollChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.TabsScrollChanged({e.Panel?.Text})");
        }
        private void _DockManagerTabsPositionChanged(object sender, DevExpress.XtraBars.Docking.TabsPositionChangedEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.TabsPositionChanged({e.Panel?.Text})");
        }
        private void _DockManagerTabbedChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.TabbedChanged({e.Panel?.Text})");
        }
        private void _DockManagerStartSizing(object sender, DevExpress.XtraBars.Docking.StartSizingEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.StartSizing({e.Panel?.Text})");
        }
        private void _DockManagerStartDocking(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.StartDocking({e.Panel?.Text})");
        }
        private void _DockManagerExpanded(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.Expanded({e.Panel?.Text})");
        }
        private void _DockManagerExpanding(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.Expanding({e.Panel?.Text})");
        }
        private void _DockManagerEndDocking(object sender, DevExpress.XtraBars.Docking.EndDockingEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.EndDocking({e.Panel?.Text})");
        }
        private void _DockManagerDocking(object sender, DevExpress.XtraBars.Docking.DockingEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.Docking({e.Panel?.Text})");
        }
        private void _DockManagerCollapsed(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.Collapsed({e.Panel?.Text})");
        }
        private void _DockManagerCollapsing(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.Collapsing({e.Panel?.Text})");
        }
        private void _DockManagerClosedPanel(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.ClosedPanel({e.Panel?.Text})");
        }
        private void _DockManagerActivePanelChanged(object sender, DevExpress.XtraBars.Docking.ActivePanelChangedEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.ActivePanelChanged({e.Panel?.Text})");
        }
        private void _DockManagerActiveChildChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
            DxComponent.LogAddLine($"DockManager.ActiveChildChanged({e.Panel?.Text})");
        }


        private void ActivateRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm && ribbonForm.MdiParent != null)
            {
                this.DxRibbon.MergeChildRibbon(ribbonForm.Ribbon);
                DxComponent.LogAddLine($"Ribbon.Desktop.MergeChildRibbon({ribbonForm?.Text})");
                ribbonForm.Ribbon.Visible = true;
                DxComponent.LogAddLine($"Ribbon.TabbedForm[{ribbonForm?.Text}].Visible = true");
            }
        }
        private void BeginFloatingRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm)
            {
                this.DxRibbon.UnMergeRibbon();
                DxComponent.LogAddLine($"Ribbon.Desktop.UnMergeRibbon({ribbonForm?.Text})");
                ribbonForm.Ribbon.Visible = true;
                DxComponent.LogAddLine($"Ribbon.FloatingForm[{ribbonForm?.Text}].Visible = true");
            }
        }
        private void DeactivateRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm && ribbonForm.MdiParent != null)
            {
                this.DxRibbon.UnMergeRibbon();
                DxComponent.LogAddLine($"Ribbon.Desktop.UnMergeRibbon({ribbonForm?.Text})");
                ribbonForm.Ribbon.Visible = false;
                DxComponent.LogAddLine($"Ribbon.TabbedForm[{ribbonForm?.Text}].Visible = false");
            }
        }
        #endregion
        #region TabView BackImage a MouseClick
        /// <summary>
        /// Inicializuje věci pro kreslení obrázku na pozadí TabView
        /// </summary>
        /// <param name="tabbedView"></param>
        private void _TabViewBackImageInit(DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView)
        {
            __TabViewBackImageMap = new DxImageAreaMap();
            _PrepareImageMap(__TabViewBackImageMap);
            __TabViewBackImageMap.Click += __TabViewBackImageMap_Click;
            tabbedView.CustomDrawBackground += _TabViewCustomDrawBackground;
        }
        /// <summary>
        /// Naplní ImageMap daty dle definice
        /// </summary>
        /// <param name="imageMap"></param>
        private void _PrepareImageMap(DxImageAreaMap imageMap)
        {
            string imageFile = @"c:\DavidPrac\VsProjects\TestDevExpress\TestDevExpress\ImagesTest\Svg\homer-simpson.svg";
            imageMap.ContentImage = System.IO.File.ReadAllBytes(imageFile);
            imageMap.Zoom = 0.40f;
            imageMap.RelativePosition = new PointF(0.04f, 0.96f);

            imageMap.ClearActiveArea();
            imageMap.AddActiveArea(new RectangleF(0.05f, 0.05f, 0.80f, 0.20f), @"https://www.helios.eu", DxCursorType.Hand);
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
        /// Odpojí zdejší eventhandlery od instance <see cref="__TabViewBackImageMap"/> a poté ji disposuje
        /// </summary>
        private void _TabViewBackImageDispose(DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView)
        {
            tabbedView.CustomDrawBackground -= _TabViewCustomDrawBackground;
            __TabViewBackImageMap.OwnerControl = null;
            __TabViewBackImageMap.Dispose();
            __TabViewBackImageMap = null;
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
            
            if (!this.__TabbedView.IsEmpty) return;

            var clientBounds = e.Bounds;
            var innerBounds = Rectangle.FromLTRB(clientBounds.Left + 36, clientBounds.Top + 48, clientBounds.Right - 36, clientBounds.Bottom - 36);
            imageMap.PaintImageMap(e.GraphicsCache, innerBounds);
        }
        /// <summary>
        /// Instance klikacího obrázku na pozadí TabView
        /// </summary>
        private DxImageAreaMap __TabViewBackImageMap;
        #endregion
        #region DockManager - služby
        /// <summary>
        /// Přidá nový dokovaný panel
        /// </summary>
        /// <param name="control"></param>
        /// <param name="panelTitle"></param>
        /// <param name="dockStyle"></param>
        /// <param name="visibility"></param>
        public void AddControlToDockPanels(Control control, string panelTitle, DevExpress.XtraBars.Docking.DockingStyle dockStyle, DevExpress.XtraBars.Docking.DockVisibility visibility)
        {
            DevExpress.XtraBars.Docking.DockPanel panel;

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

            panel.Text = panelTitle;
            panel.Visibility = DevExpress.XtraBars.Docking.DockVisibility.AutoHide;
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control);

            string resource1 = "svgimages/xaf/action_aboutinfo.svg";
            DxComponent.ApplyImage(panel.ImageOptions, resource1);
            panel.ImageOptions.SvgImageSize = new System.Drawing.Size(20, 20);

            this.__DockManager.DockingOptions.ShowCaptionImage = true;

            _Panels.Add(new Tuple<Control, DevExpress.XtraBars.Docking.DockPanel>(control, panel));
        }
        /// <summary>
        /// Metoda vrátí panel, v němž je nyní umístěn control který vyhoví dodané podmínce.
        /// Volající sám může testovat control (testovat jeho typ, jeho Name, jeho Tag...).
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.Docking.DockPanel GetDockPanelForControl(Func<Control, bool> predicate)
        {
            return _Panels.FirstOrDefault(t => predicate(t.Item1))?.Item2;
        }
        /// <summary>
        /// Seznam zadokovaných controlů a panelů, na nichž jsou dokovány
        /// </summary>
        private List<Tuple<Control, DevExpress.XtraBars.Docking.DockPanel>> _Panels
        {
            get
            {
                if (__Panels is null)
                    __Panels = new List<Tuple<Control, DevExpress.XtraBars.Docking.DockPanel>>();
                return __Panels;
            }
        }
        private List<Tuple<Control, DevExpress.XtraBars.Docking.DockPanel>> __Panels;
        #endregion
        #region Otevírání Child okna v okně aplikace
        /// <summary>
        /// Otevři okno v prostředí aplikace
        /// </summary>
        /// <param name="form"></param>
        /// <param name="showFloating"></param>
        public static void ShowChildForm(Form form, bool showFloating)
        {
            var mainAppForm = MainAppForm;
            if (mainAppForm != null)
            {
                form.MdiParent = mainAppForm;
                mainAppForm.__TabbedView.AddDocument(form);
                // mainAppForm._TabbedView.AddDocument(form);
                form.Show();
            }
            else
            {
                form.Show();
            }
        }
        #endregion
    }
}
