using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Předek hlavního okna aplikace. <br/>
    /// Obsahuje Ribbon a Status bar (od svého předka) a obsahuje Dock panel, Tab View a Document manager.
    /// </summary>
    public class DxMainAppForm : DxRibbonBaseForm
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxMainAppForm() : base() 
        { }
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
        #region Hlavní obsah okna aplikace
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
            _DockManager = new DevExpress.XtraBars.Docking.DockManager();
            ((System.ComponentModel.ISupportInitialize)(_DockManager)).BeginInit();

            _DocumentManager = new DevExpress.XtraBars.Docking2010.DocumentManager();
            ((System.ComponentModel.ISupportInitialize)(_DocumentManager)).BeginInit();

            _TabbedView = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView();
            ((System.ComponentModel.ISupportInitialize)(_TabbedView)).BeginInit();

            _SplashManager = new DevExpress.XtraSplashScreen.SplashScreenManager(); // (this, typeof(global::Noris.Clients.WinForms.Forms.WaitFormLoadingDesktop), false, false, true);
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
        }
        /// <summary>
        /// Nastavení komponenty DocumentManager
        /// </summary>
        protected virtual void SetupDocumentManager()
        {
            var docManager = _DocumentManager;
            docManager.MdiParent = this;
            docManager.View = _TabbedView;
            docManager.ShowThumbnailsInTaskBar = DevExpress.Utils.DefaultBoolean.False;
            docManager.SnapMode = DevExpress.Utils.Controls.SnapMode.All;
            docManager.RibbonAndBarsMergeStyle = DevExpress.XtraBars.Docking2010.Views.RibbonAndBarsMergeStyle.WhenNotFloating;
            docManager.DocumentActivate += _DocumentManagerDocumentActivate;
            docManager.ViewChanged += _DocumentManagerViewChanged;
            docManager.View.DocumentAdded += _DocumentManagerDocumentAdded;
            docManager.View.DocumentClosing += _DocumentManagerDocumentClosing;
            docManager.View.DocumentRemoved += _DocumentManagerDocumentRemoved;
            docManager.View.BeginFloating += _DocumentManagerBeginFloating;
            docManager.View.EndFloating += _DocumentManagerEndFloating;
        }
        /// <summary>
        /// Nastavení komponenty TabbedView
        /// </summary>
        protected virtual void SetupTabbedView()
        {
            var tabView = _TabbedView;
            tabView.Style = DevExpress.XtraBars.Docking2010.Views.DockingViewStyle.Classic;
            tabView.EnableFreeLayoutMode = DevExpress.Utils.DefaultBoolean.True;                   // Umožní dát dokování do skupin různě rozmístěných (vodorovně + svisle v sobě)
            tabView.CustomResizeZoneThickness = DefaultResizeZoneThickness;                        // Viditelný splitter mezi dokovanými skupinami
            tabView.EnableStickySplitters = DevExpress.Utils.DefaultBoolean.True;                  // Splitter mezi dokovanými skupinami se bude přichytávat k okolním splitterům
            tabView.ShowDockGuidesOnPressingShift = DevExpress.Utils.DefaultBoolean.False;         // Při snaze o zadokování Floating formu se zobrazí nápovědná ikona false=bez Shiftu / true = jen po stisknutí Shiftu

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
            tabView.EndSizing += _TabbedViewEndSizing; ;
            tabView.Floating += _TabbedViewFloating;
            tabView.BeginDocking += _TabbedViewBeginDocking;
            tabView.EndDocking += _TabbedViewEndDocking;
        }
        /// <summary>
        /// Inicializace komponenty DockManager
        /// </summary>
        protected virtual void SetupDockManager()
        {
            var dockMgr = _DockManager;
            dockMgr.DockingOptions.CustomResizeZoneThickness = DefaultResizeZoneThickness;
            dockMgr.DockingOptions.HidePanelsImmediately = DevExpress.XtraBars.Docking.Helpers.HidePanelsImmediatelyMode.Always;   // Vyjetí pravého panelu až na kliknutí; vyjetí na kliknutí + vypnutí animací
            dockMgr.AutoHiddenPanelShowMode = DevExpress.XtraBars.Docking.AutoHiddenPanelShowMode.MouseClick;
            dockMgr.DockingOptions.AutoHidePanelVerticalTextOrientation = DevExpress.XtraBars.Docking.VerticalTextOrientation.BottomToTop;
            dockMgr.DockingOptions.TabbedPanelVerticalTextOrientation = DevExpress.XtraBars.Docking.VerticalTextOrientation.BottomToTop;

            dockMgr.ClosingPanel += _DockManagerClosingPanel;
            dockMgr.EndSizing += _DockManagerEndSizing;
        }
        /// <summary>
        /// Je voláno na konci konstruktoru třídy <see cref="DxRibbonBaseForm"/>.
        /// Typicky je zde ukončen cyklus BeginInit jednoltivých komponent.
        /// <para/>
        /// Je povinné volat base metodu, typicky na konci metody override.
        /// </summary>
        protected override void EndInitDxRibbonForm()
        {
            ((System.ComponentModel.ISupportInitialize)(_TabbedView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_DocumentManager)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(_DockManager)).EndInit();

            base.EndInitDxRibbonForm();
        }
        private DevExpress.XtraBars.Docking.DockManager _DockManager;
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView _TabbedView;
        private DevExpress.XtraBars.Docking2010.DocumentManager _DocumentManager;
        private DevExpress.XtraSplashScreen.SplashScreenManager _SplashManager;

        private void _DocumentManagerDocumentActivate(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e) { }
        private void _DocumentManagerViewChanged(object sender, DevExpress.XtraBars.Docking2010.ViewEventArgs e) { }
        private void _DocumentManagerDocumentAdded(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e) { }
        private void _DocumentManagerDocumentClosing(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e) { }
        private void _DocumentManagerDocumentRemoved(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e) { }
        private void _DocumentManagerBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e) { }
        private void _DocumentManagerEndFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e) { }

        private void _TabbedViewGroupsCollectionChanged(DevExpress.XtraBars.Docking2010.Base.CollectionChangedEventArgs<DevExpress.XtraBars.Docking2010.Views.Tabbed.DocumentGroup> e) { }
        private void _TabbedViewLayout(object sender, EventArgs e) { }
        private void _TabbedViewPaint(object sender, System.Windows.Forms.PaintEventArgs e) { }
        private void _TabbedViewEndSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutEndSizingEventArgs e) { }
        private void _TabbedViewFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e) { }
        private void _TabbedViewBeginDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e) { }
        private void _TabbedViewEndDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e) { }

        private void _DockManagerEndSizing(object sender, DevExpress.XtraBars.Docking.EndSizingEventArgs e) { }
        private void _DockManagerClosingPanel(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e) { }

        #endregion
    }
}
