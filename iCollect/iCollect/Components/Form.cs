using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using XBars = DevExpress.XtraBars;
using XRibbon = DevExpress.XtraBars.Ribbon;
using XEditors = DevExpress.XtraEditors;

using DjSoft.App.iCollect.Data;
using DComponents = DjSoft.App.iCollect.Components;
using DjSoft.App.iCollect.Application;

namespace DjSoft.App.iCollect.Components
{
    #region DjTabbedRibbonForm : Formulář s Ribbonem a StatusBarem, a s TabDocument organizérem - pro Main okno celé aplikace
    /// <summary>
    /// Formulář s Ribbonem a StatusBarem, a s TabDocument organizérem - pro Main okno celé aplikace
    /// </summary>
    public class DjTabbedRibbonForm : DjRibbonForm
    {
        /// <summary>
        /// Provede tvorbu hlavního obsahue okna, podle jeho typu, a jeho přidání do okna včetně zadokování.
        /// Provádí se před vytvořením Ribbonu a Status baru, aby obsah byl správně umístěn na Z ose.
        /// </summary>
        protected override void OnMainAreaInitialize()
        {
            __DockManager = new DevExpress.XtraBars.Docking.DockManager();
            ((System.ComponentModel.ISupportInitialize)(__DockManager)).BeginInit();

            __DocumentManager = new DevExpress.XtraBars.Docking2010.DocumentManager();
            ((System.ComponentModel.ISupportInitialize)(__DocumentManager)).BeginInit();

            __TabbedView = new DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView();
            ((System.ComponentModel.ISupportInitialize)(__TabbedView)).BeginInit();

            __SplashManager = new DevExpress.XtraSplashScreen.SplashScreenManager(); // (this, typeof(global::Noris.Clients.WinForms.Forms.WaitFormLoadingDesktop), false, false, true);

            DxMainContentPrepare();
        }

        public DevExpress.XtraBars.Docking2010.DocumentManager DocumentManager { get { return __DocumentManager; } }
        public DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView TabbedView { get { return __TabbedView; } }
        public DevExpress.XtraBars.Docking.DockManager DockManager { get { return __DockManager; } }
        public DevExpress.XtraSplashScreen.SplashScreenManager SplashManager { get { return __SplashManager; } }
        /// <summary>
        /// Defaultní velikost Resize zóny na komponentě Dockpanel
        /// </summary>
        public static int DefaultResizeZoneThickness { get { return 10; } }
        /// <summary>
        /// Provede přípravu obsahu hlavního obshau okna. Obsah je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected virtual void DxMainContentPrepare()
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
            this.__DocumentManager.MenuManager = this.DjRibbon;
            this.__DocumentManager.View = this.__TabbedView;
            this.__DocumentManager.ViewCollection.AddRange(new DevExpress.XtraBars.Docking2010.Views.BaseView[] { this.__TabbedView });
        }
        /// <summary>
        /// Zde končí konstruktor, měly by se zde ukončit bloky SuspendLayout = ResumeLayout
        /// </summary>
        protected override void OnEndInitialize() 
        {
            ((System.ComponentModel.ISupportInitialize)(__TabbedView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(__DocumentManager)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(__DockManager)).EndInit();

            base.OnEndInitialize();
        }
        private DevExpress.XtraBars.Docking2010.DocumentManager __DocumentManager;
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView __TabbedView;
        private DevExpress.XtraBars.Docking.DockManager __DockManager;
        private DevExpress.XtraSplashScreen.SplashScreenManager __SplashManager;

        private void _DocumentManagerDocumentActivate(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }
        private void _DocumentManagerViewChanged(object sender, DevExpress.XtraBars.Docking2010.ViewEventArgs e)
        {
        }
        private void _DocumentManagerBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
        }

        private void _TabbedViewDocumentAdded(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }
        private void _TabbedViewDocumentClosing(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
        }
        private void _TabbedViewDocumentRemoved(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }
        private void _TabbedViewEndFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }
        private void _TabbedViewNextDocument(object sender, DevExpress.XtraBars.Docking2010.Views.NextDocumentEventArgs e)
        {
        }
        private void _TabbedViewLostFocus(object sender, EventArgs e)
        {
        }
        private void _TabbedViewGotFocus(object sender, EventArgs e)
        {
        }
        private void _TabbedViewEmptyDocumentsHostWindow(object sender, DevExpress.XtraBars.Docking2010.EmptyDocumentsHostWindowEventArgs e)
        {
        }
        private void _TabbedViewDocumentDeactivated(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            DeactivateRibbonForControl(e.Document?.Control);
        }
        private void _TabbedViewDocumentClosed(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }
        private void _TabbedViewDocumentActivated(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            ActivateRibbonForControl(e.Document?.Control);
        }
        private void _TabbedViewControlShown(object sender, DevExpress.XtraBars.Docking2010.Views.DeferredControlLoadEventArgs e)
        {
        }
        private void _TabbedViewBeginSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutBeginSizingEventArgs e)
        {
        }
        private void _TabbedViewBeginFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            BeginFloatingRibbonForControl(e.Document.Control);
        }
        private void _TabbedViewGroupsCollectionChanged(DevExpress.XtraBars.Docking2010.Base.CollectionChangedEventArgs<DevExpress.XtraBars.Docking2010.Views.Tabbed.DocumentGroup> e)
        {
        }
        private void _TabbedViewLayout(object sender, EventArgs e)
        {
        }
        private void _TabbedViewPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
        }
        private void _TabbedViewEndSizing(object sender, DevExpress.XtraBars.Docking2010.Views.LayoutEndSizingEventArgs e)
        {
        }
        private void _TabbedViewFloating(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }
        private void _TabbedViewBeginDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
        }
        private void _TabbedViewEndDocking(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
        }

        private void _DockManagerRegisterDockPanel(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerEndSizing(object sender, DevExpress.XtraBars.Docking.EndSizingEventArgs e)
        {
        }
        private void _DockManagerClosingPanel(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
        }
        private void _DockManagerVisibilityChanged(object sender, DevExpress.XtraBars.Docking.VisibilityChangedEventArgs e)
        {
        }
        private void _DockManagerTabsScrollChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerTabsPositionChanged(object sender, DevExpress.XtraBars.Docking.TabsPositionChangedEventArgs e)
        {
        }
        private void _DockManagerTabbedChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerStartSizing(object sender, DevExpress.XtraBars.Docking.StartSizingEventArgs e)
        {
        }
        private void _DockManagerStartDocking(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
        }
        private void _DockManagerExpanded(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerExpanding(object sender, DevExpress.XtraBars.Docking.DockPanelCancelEventArgs e)
        {
        }
        private void _DockManagerEndDocking(object sender, DevExpress.XtraBars.Docking.EndDockingEventArgs e)
        {
        }
        private void _DockManagerDocking(object sender, DevExpress.XtraBars.Docking.DockingEventArgs e)
        {
        }
        private void _DockManagerCollapsed(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerCollapsing(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerClosedPanel(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }
        private void _DockManagerActivePanelChanged(object sender, DevExpress.XtraBars.Docking.ActivePanelChangedEventArgs e)
        {
        }
        private void _DockManagerActiveChildChanged(object sender, DevExpress.XtraBars.Docking.DockPanelEventArgs e)
        {
        }

        private void ActivateRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm && ribbonForm.MdiParent != null)
            {
                this.DjRibbon.MergeRibbon(ribbonForm.Ribbon);
                ribbonForm.Ribbon.Visible = true;
            }
        }
        private void BeginFloatingRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm)
            {
                this.DjRibbon.UnMergeRibbon();
                ribbonForm.Ribbon.Visible = true;
            }
        }
        private void DeactivateRibbonForControl(Control control)
        {
            if (control is DevExpress.XtraBars.Ribbon.RibbonForm ribbonForm && ribbonForm.MdiParent != null)
            {
                this.DjRibbon.UnMergeRibbon();
                ribbonForm.Ribbon.Visible = false;
            }
        }
    }
    #endregion
    #region DjMainPanelRibbonForm : Formulář s Ribbonem a StatusBarem, a s jedním Main panelem - pro jednoduchá okna
    /// <summary>
    /// Formulář s Ribbonem a StatusBarem, a s jedním Main panelem - pro jednoduchá okna
    /// </summary>
    public class DjMainPanelRibbonForm : DjRibbonForm
    {
        #region MainPanel
        protected override void OnMainAreaInitialize()
        {
            __MainPanel = new XEditors.PanelControl() { Dock = DockStyle.Fill, BackColor = Color.LightBlue, BorderStyle = XEditors.Controls.BorderStyles.Office2003 };
            this.Controls.Add(__MainPanel);
        }
        public XEditors.PanelControl MainPanel { get { return __MainPanel; } }
        private XEditors.PanelControl __MainPanel;
        #endregion
    }
    #endregion
    #region DjRibbonForm : Abstraktní třída obsahující Ribbon a StatusBar, její potomci doplní Main content
    /// <summary>
    /// Abstraktní třída obsahující Ribbon a StatusBar, její potomci doplní Main content.
    /// </summary>
    public abstract class DjRibbonForm : XRibbon.RibbonForm, IFormStatusWorking
    {
        #region Inicializace
        public DjRibbonForm()
        {
            this.ComponentInitialize();
            this.FormStateInitialize();
            this.OnMainAreaInitialize();
            this.RibbonInitialize();
            this.StatusInitialize();
            this.OnRibbonPrepare();
            this.OnStatusPrepare();
            this.OnContentPrepare();
            this.OnEndInitialize();
        }
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void ComponentInitialize()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        }
        /// <summary>
        /// Zde si potomek vytvoří svůj MainControl (panel nebo TabView) a vloží jej do Controls.
        /// Volá se před vytvořením Ribbonu a StatusBaru, tak aby Main control byl umístěn v ZOrder vespodu a správně tak obsadil prostor spolu s Ribbonem a StatusBarem.
        /// Tato metoda nemá vytvářet obsah do hlavního prostoru (jednotlivé prvky), ale jen hlavní container (Panel, TabView).
        /// </summary>
        protected abstract void OnMainAreaInitialize();
        /// <summary>
        /// Zde potomek naplní hlavní prostor potřebnými prvky. Volá se po vytvoření všech základních controlů a po naplnění Ribbonu a StatusBaru.
        /// </summary>
        protected virtual void OnContentPrepare() { }
        /// <summary>
        /// Zde končí konstruktor, měly by se zde ukončit bloky SuspendLayout = ResumeLayout
        /// </summary>
        protected virtual void OnEndInitialize() { }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        #endregion
        #region FormStatus (stav okna a ukládání pozice), support
        /// <summary>
        /// Umístí toto okno do viditelných souřadnic monitorů.
        /// Pokud je parametr <paramref name="force"/> = false (default), 
        /// pak to provádí jen když pozice okna <see cref="Form.StartPosition"/> je <see cref="FormStartPosition.Manual"/>.
        /// Pokud parametr <paramref name="force"/> = true, provede to vždy.
        /// </summary>
        /// <param name="force"></param>
        public void MoveToVisibleScreen(bool force = false)
        {
            __FormState.MoveToVisibleScreen(force);
        }
        /// <summary>
        /// Pozice okna byla nastavena v jeho konstruktoru z dat načtených z konfigurace. Bounds ani WindowState ani StartPosition by neměly být měněny.
        /// </summary>
        public bool PositionIsFromConfig { get { return __FormState.PositionIsFromConfig; } }
        /// <summary>
        /// Inicializace dat pro detekci stavu
        /// </summary>
        protected virtual void FormStateInitialize()
        {
            __FormState = new FormStateInfo(this);                 // Konstruktor si zaregistruje svoje eventhandlery, uloží si WeakReferenci, a zajistí i uvolnění v eventu Disposed
        }
        private FormStateInfo __FormState;
        /// <summary>
        /// Stav aktivity okna. Při změně je volána událost <see cref="ActivityStateChanged"/>.
        /// </summary>
        public WindowActivityState ActivityState
        {
            get { return __FormState.ActivityState; }
            private set { __FormState.ActivityState = value; }      // Formulář smí měnit svůj stav, když ví jak
        }
        /// <summary>
        /// Provede akce jako po prvním Show
        /// </summary>
        protected void RunShow()
        {
            this.OnShown(EventArgs.Empty);
        }
        /// <summary>
        /// Metoda proběhne při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivityStateChanged(TEventValueChangedArgs<WindowActivityState> args) { }
        /// <summary>
        /// Událost volaná při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        public event EventHandler<TEventValueChangedArgs<WindowActivityState>> ActivityStateChanged;
        /// <summary>
        /// Jméno konfigurace v subsystému AsolDX.
        /// Pokud bude zde vráceno neprázdné jméno, pak načtení a uložení konfigurace okna zajistí sama třída, která implementuje <see cref="IFormStatusWorking"/>.
        /// Pokud nebude vráceno jméno, budou používány metody <see cref="DxStdForm.PositionLoadFromConfig(string)"/> a <see cref="DxStdForm.PositionSaveToConfig(string, string)"/>.
        /// </summary>
        protected virtual string PositionConfigName { get { return null; } }
        /// <summary>
        /// Pokusí se z konfigurace najít a načíst string popisující pozici okna.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Za toto jméno přidá suffix (začíná podtržítkem a obsahuje XML validní znaky) a vyhledá konfiguraci se suffixem.<br/>
        /// 3. Pokud nenajde konfiguraci se suffixem, vyhledá konfiguraci bez suffixu = obecná, posledně použití (viz <see cref="PositionSaveToConfig(string, string)"/>).<br/>
        /// 4. Nalezený string je ten, který byl uložen v metodě <see cref="PositionSaveToConfig(string, string)"/> a je roven parametru 'positionData'. Pokud položku v konfiguraci nenajde, vrátí null (nebo prázdný string).
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// <para/>
        /// Konverze formátů: Pokud v konfiguraci budou uložena stringová data ve starším formátu, než dokáže obsloužit zpracující třída <see cref="FormStateInfo"/>, pak konverzi do jejího formátu musí zajistit aplikační kód (protože on ví, jak zpracovat starý formát).<br/>
        /// <b><u>Postup:</u></b><br/>
        /// 1. Po načtení konfigurace se lze dotázat metodou <see cref="FormStateInfo.IsPositionDataValid(string)"/>, zda načtená data jsou validní.<br/>
        /// 2. Pokud nejsou validní, pak je volající aplikace zkusí analyzovat svým starším (legacy) postupem na prvočinitele;<br/>
        /// 3. A pokud je úspěšně rozpoznala, pak ze základních dat sestaví validní konfirurační string s pomocí metody <see cref="FormStateInfo.CreatePositionData(bool?, FormWindowState?, Rectangle?, Rectangle?)"/>.<br/>
        /// </summary>
        /// <param name="nameSuffix">Suffix ke jménu konfigurace, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory</param>
        /// <returns></returns>
        protected virtual string PositionLoadFromConfig(string nameSuffix) { return null; }
        /// <summary>
        /// Do konfigurace uloží dodaná data o pozici okna '<paramref name="positionData"/>'.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Jednak uloží data <paramref name="positionData"/> přímo do položky konfigurace pod svým vlastním jménem bez suffixu = data obecná pro libovolnou konfiguraci monitorů.<br/>
        /// 3. A dále uloží tato data do položky konfigurace, kde za svoje jméno přidá dodaný suffix <paramref name="nameSuffix"/> = tato hodnota se použije po restore na shodné konfiguraci monitorů.<br/>
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// </summary>
        /// <param name="positionData"></param>
        /// <param name="nameSuffix"></param>
        protected virtual void PositionSaveToConfig(string positionData, string nameSuffix) { }

        string IFormStatusWorking.PositionConfigName { get { return PositionConfigName; } }
        bool IFormStatusWorking.PositionIsFromConfig { get { return PositionIsFromConfig; } }
        bool? IFormStatusWorking.ConfigIsMdiChild { get { return __FormState.ConfigIsMdiChild; } }
        string IFormStatusWorking.PositionLoadFromConfig(string nameSuffix) { return PositionLoadFromConfig(nameSuffix); }
        void IFormStatusWorking.PositionSaveToConfig(string positionData, string nameSuffix) { PositionSaveToConfig(positionData, nameSuffix); }
        bool IFormStatusWorking.WasShown { get { return __WasShown; } }
        void IFormStatusWorking.RunShow() { this.RunShow(); }
        void IFormStatusWorking.RunActivityStateChanged(TEventValueChangedArgs<WindowActivityState> args)
        {
            OnActivityStateChanged(args);
            ActivityStateChanged?.Invoke(this, args);
        }
        #endregion
        #region Ribbon
        private void RibbonInitialize()
        {
            __DjRibbon = new DComponents.Ribbon.DjRibbonControl();
            this.Controls.Add(__DjRibbon);
        }
        /// <summary>
        /// Potomek zde může naplnit prvky do Ribbonu <see cref="DjRibbonForm.DjRibbon"/>
        /// </summary>
        protected virtual void OnRibbonPrepare() { }
        /// <summary>
        /// Ribbon
        /// </summary>
        public DComponents.Ribbon.DjRibbonControl DjRibbon { get { return __DjRibbon; } }
        private DComponents.Ribbon.DjRibbonControl __DjRibbon;
        #endregion
        #region Status
        /// <summary>
        /// Připraví StatusBar
        /// </summary>
        private void StatusInitialize()
        {
            __DjStatus = new Ribbon.DjStatusControl() { Ribbon = __DjRibbon };
             this.Controls.Add(__DjStatus);
        }
        /// <summary>
        /// Potomek zde může naplnit prvky do Statusbaru <see cref="DjRibbonForm.DjStatus"/>
        /// </summary>
        protected virtual void OnStatusPrepare() { }
        /// <summary>
        /// Status
        /// </summary>
        public DComponents.Ribbon.DjStatusControl DjStatus { get { return __DjStatus; } }
        private DComponents.Ribbon.DjStatusControl __DjStatus;
        #endregion
        #region Show
        /// <summary>
        /// Při zobrazení okna
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            if (!__WasShown)
            {
                this.ActivityState = WindowActivityState.FirstShow;            // Zajistí aplikování souřadnic okna z těch uložených v konfiguraci do živého okna
                this.OnFirstShownBefore();
                this.FirstShownBefore?.Invoke(this, EventArgs.Empty);
                base.OnShown(e);
                __WasShown = true;
                this.OnFirstShownAfter();
                this.FirstShownAfter?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                base.OnShown(e);
                this.OnNextShown();
                this.NextShown?.Invoke(this, EventArgs.Empty);
            }
            this.ActivityState = WindowActivityState.Visible;
        }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně před prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = false.
        /// </summary>
        protected virtual void OnFirstShownBefore() { }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně před prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = false.
        /// </summary>
        public event EventHandler FirstShownBefore;
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně po prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = true.
        /// </summary>
        protected virtual void OnFirstShownAfter() { }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně po prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = true.
        /// </summary>
        public event EventHandler FirstShownAfter;
        /// <summary>
        /// Je vyvoláno těsně po druhém a každém dalším zobrazením okna. Nikoli po prvním.
        /// </summary>
        protected virtual void OnNextShown() { }
        /// <summary>
        /// Je vyvoláno těsně po druhém a každém dalším zobrazením okna. Nikoli po prvním.
        /// </summary>
        public event EventHandler NextShown;
        /// <summary>
        /// Obsahuje true poté, kdy formulář byl zobrazen. 
        /// Obsahuje true již v metodě <see cref="OnFirstShownAfter"/> a v eventu <see cref="FirstShownAfter"/>.
        /// </summary>
        public bool WasShown { get { return __WasShown; } }
        /// <summary>
        /// Okno již bylo zobrazeno?
        /// </summary>
        private bool __WasShown = false;
        /// <summary>
        /// Obsahuje true v situaci, kdy toto okno již prošlo fází zobrazení (<see cref="WasShown"/> je true) a dosud nebylo disposováno.
        /// </summary>
        public bool IsLive { get { return __WasShown && !Disposing && !IsDisposed; } }
        /// <summary>
        /// Obsahuje true v situaci, kdy toto okno je ve viditelném stavu = je živé <see cref="IsLive"/> a není minimalizované.
        /// V tomto stavu má smysl provádět Layout okna.
        /// </summary>
        public bool IsDisplayed { get { return IsLive && WindowState != FormWindowState.Minimized; } }
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        public int CurrentDpi { get { return this.DeviceDpi; } }
        #endregion
    }
    #endregion
    #region FormStatusInfo : Informace o životním stavu formuláře (proces otevírání, zavírání atd), a o jeho pozici, rozměrech a maximalizaci
    /// <summary>
    /// Informace o životním stavu formuláře (proces otevírání, zavírání atd), a o jeho pozici, rozměrech a maximalizaci
    /// </summary>
    internal class FormStateInfo
    {
        #region Konstruktor, Owner, Dispose...
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FormStateInfo(Form owner)
        {
            if (owner is null) throw new ArgumentNullException($"FormStatusInfo(Form owner): 'owner' can not be null.");

            this._Owner = owner;
            owner.Disposed += _Owner_Disposed;
            this.__ActivityState = WindowActivityState.None;
            this._LinkStatusEvents(owner);
            this._LinkBoundsEvents(owner);
        }
        /// <summary>
        /// Formulář byl Disposován
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Owner_Disposed(object sender, EventArgs e)
        {
            var owner = _Owner;
            this._UnLinkStatusEvents(owner);
            this._UnLinkBoundsEvents(owner);
            _Owner = null;
            this.ActivityState = WindowActivityState.Disposed;
        }
        /// <summary>
        /// Vlastník = formulář, ukládá se WeakTarget
        /// </summary>
        private Form _Owner
        {
            get { return ((__Owner != null && __Owner.IsAlive) ? __Owner.Target : null); }
            set { __Owner = (value != null ? new WeakTarget<Form>(value) : null); }
        }
        /// <summary>
        /// Vlastník = formulář, pokud implementuje <see cref="IFormStatusWorking"/>. Jinak je null.
        /// </summary>
        private IFormStatusWorking _IOwner { get { return _Owner as IFormStatusWorking; } }
        /// <summary>
        /// WeakTarget na Vlastník = formulář
        /// </summary>
        private WeakTarget<Form> __Owner;
        /// <summary>
        /// Obsahuje hodnotu Visible z formuláře <see cref="_Owner"/>
        /// </summary>
        private bool _OwnerVisible { get { return (_Owner?.Visible ?? false); } }
        /// <summary>
        /// Obsahuje hodnotu WasShown z formuláře <see cref="_Owner"/> (as <see cref="IFormStatusWorking"/>)
        /// </summary>
        private bool _OwnerWasShown { get { return (_IOwner?.WasShown ?? false); } }
        #endregion
        #region Stav okna v jeho životním cyklu: ActivityState
        /// <summary>
        /// Stav aktivity okna. Při změně je volána metoda ownera: <see cref="IFormStatusWorking.RunActivityStateChanged(TEventValueChangedArgs{WindowActivityState})"/>.
        /// </summary>
        public WindowActivityState ActivityState { get { return __ActivityState; } set { _SetActivityState(value); } }
        private void _SetActivityState(WindowActivityState activityState)
        {
            // Setování může provádět i Owner form:
            var oldValue = __ActivityState;
            var newValue = activityState;

            // Stavový diagram: co mohu akceptovat z OldValue:
            bool isAcceptable = (oldValue == WindowActivityState.None && (newValue == WindowActivityState.Creating))
                             || (oldValue == WindowActivityState.Creating && (newValue == WindowActivityState.Initialized))
                             || (oldValue == WindowActivityState.Initialized && (newValue == WindowActivityState.FirstShow))
                             || (oldValue == WindowActivityState.FirstShow && (newValue == WindowActivityState.ShowBefore))
                             || (oldValue == WindowActivityState.ShowBefore && (newValue == WindowActivityState.ShowAfter))
                             || (oldValue == WindowActivityState.ShowAfter && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Visible || newValue == WindowActivityState.Invisible || newValue == WindowActivityState.Closing))
                             || (oldValue == WindowActivityState.Visible && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Visible || newValue == WindowActivityState.Invisible || newValue == WindowActivityState.Closing))
                             || (oldValue == WindowActivityState.Active && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Visible || newValue == WindowActivityState.Invisible || newValue == WindowActivityState.Closing))
                             || (oldValue == WindowActivityState.Inactive && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Visible || newValue == WindowActivityState.Invisible || newValue == WindowActivityState.Closing))
                             || (oldValue == WindowActivityState.Invisible && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Visible || newValue == WindowActivityState.Invisible || newValue == WindowActivityState.Closing))
                             || (oldValue == WindowActivityState.Closing && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Visible || newValue == WindowActivityState.Invisible || newValue == WindowActivityState.Closed))
                             || (oldValue == WindowActivityState.Closed && (newValue == WindowActivityState.Disposing || newValue == WindowActivityState.Disposed))
                             || (oldValue == WindowActivityState.Disposing && (newValue == WindowActivityState.Disposed))
                             || (oldValue == WindowActivityState.Disposed && (newValue == WindowActivityState.Disposed));

            bool isIgnored = (oldValue == WindowActivityState.Initialized && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive))
                             || (oldValue == WindowActivityState.ShowBefore && (newValue == WindowActivityState.Active || newValue == WindowActivityState.Inactive))
                             || (oldValue == WindowActivityState.Disposing && (newValue == WindowActivityState.Inactive || newValue == WindowActivityState.Invisible));

            if (isAcceptable)
            {
                // Těsně před prvním zobrazením okna (FirstShow) znovu aplikuji pozici okna:
                if (newValue == WindowActivityState.FirstShow)
                    this._Position_Apply(true);

                if (newValue != oldValue)
                {
                    __ActivityState = newValue;

                    // Zajistíme vyvolání eventu v Formu:
                    var iOwner = _IOwner;
                    if (iOwner != null)
                        iOwner.RunActivityStateChanged(new TEventValueChangedArgs<WindowActivityState>(EventSource.None, oldValue, newValue));
                }
            }
            else if (isIgnored)
            {   // Toto jsou známé přechody stavů, které ignorujeme úmyslně...
            }
            else
            {   // Toto jsou nezmapované přechody stavů... Breakpoint sem!
            }
        }
        /// <summary>Stav okna</summary>
        private WindowActivityState __ActivityState;
        /// <summary>
        /// Aktivuje svoje eventhandlery do daného fornuláře pro sledování <see cref="ActivityState"/>
        /// </summary>
        /// <param name="owner"></param>
        private void _LinkStatusEvents(Form owner)
        {
            if (owner != null)
            {
                owner.Activated += _ActivityStateDetect_Activated;
                owner.Deactivate += _ActivityStateDetect_Deactivate;
                owner.GotFocus += _ActivityStateDetect_GotFocus;
                owner.LostFocus += _ActivityStateDetect_LostFocus;
                owner.VisibleChanged += _ActivityStateDetect_VisibleChanged;
                owner.FormClosing += _ActivityStateDetect_FormClosing;
                owner.FormClosed += _ActivityStateDetect_FormClosed;
            }
        }
        /// <summary>
        /// Deaktivuje svoje eventhandlery z daného fornuláře pro sledování <see cref="ActivityState"/>
        /// </summary>
        /// <param name="owner"></param>
        private void _UnLinkStatusEvents(Form owner)
        {
            if (owner != null)
            {
                owner.Activated -= _ActivityStateDetect_Activated;
                owner.Deactivate -= _ActivityStateDetect_Deactivate;
                owner.GotFocus -= _ActivityStateDetect_GotFocus;
                owner.LostFocus -= _ActivityStateDetect_LostFocus;
                owner.VisibleChanged -= _ActivityStateDetect_VisibleChanged;
                owner.FormClosing -= _ActivityStateDetect_FormClosing;
                owner.FormClosed -= _ActivityStateDetect_FormClosed;
            }
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Activated(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Deactivate(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_GotFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_LostFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_VisibleChanged(object sender, EventArgs e)
        {
            bool formWasShown = this._OwnerWasShown;
            bool formIsVisible = this._OwnerVisible;
            if (!formWasShown && formIsVisible)
            {   // Okno dosud nebylo zobrazeno, a nyní je Visible => provedeme totéž jako v OnShow():
                this._IOwner.RunShow();
            }
            else
            {   // Okno již bylo zobrazeno => nastavíme odpovídající State:
                this.ActivityState = (formIsVisible ? WindowActivityState.Visible : WindowActivityState.Invisible);
            }
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closing;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closed;
        }
        #endregion
        #region Pozice a velikost okna a jeho WindowState
        /// <summary>
        /// Umístí toto okno do viditelných souřadnic monitorů.
        /// Pokud je parametr <paramref name="force"/> = false (default), 
        /// pak to provádí jen když pozice okna <see cref="Form.StartPosition"/> je <see cref="FormStartPosition.Manual"/>.
        /// Pokud parametr <paramref name="force"/> = true, provede to vždy.
        /// </summary>
        /// <param name="force"></param>
        public void MoveToVisibleScreen(bool force = false)
        {
            Form owner = _Owner;
            if (owner != null)
            {
                if (force || owner.StartPosition == FormStartPosition.Manual) return;
                owner.Bounds = owner.Bounds.FitIntoMonitors(true, false, true);
            }
        }
        /// <summary>
        /// Pozice okna byla nastavena v jeho konstruktoru z dat načtených z konfigurace. Bounds ani WindowState ani StartPosition by neměly být měněny.
        /// </summary>
        public bool PositionIsFromConfig { get { return __PositionIsFromConfig; } }
        /// <summary>
        /// Příznak z načtené konfigurace okna: true = okno bylo zavíráno ve stavu IsMdiChild (tedy jako Tabované) / false = bylo plovoucí
        /// </summary>
        public bool? ConfigIsMdiChild { get { return __ConfigIsMdiChild; } }
        /// <summary>
        /// Vrátí true, pokud dodaný string je validní pro použití jako data konfigurace
        /// </summary>
        /// <param name="configData"></param>
        /// <returns></returns>
        public static bool IsPositionDataValid(string configData)
        {
            return _TryParsePositionData(configData, out var _, out var _, out var _, out var _);
        }
        /// <summary>
        /// Z dodaných hodnot vrátí konfigurační string popisující stav a pozici okna
        /// </summary>
        /// <returns></returns>
        public static string CreatePositionData(bool? isMdiChild, FormWindowState? windowState, Rectangle? normalBounds, Rectangle? maximizedBounds)
        {
            return _CreatePositionData(isMdiChild, windowState, normalBounds, maximizedBounds);
        }
        /// <summary>
        /// Aktivuje svoje eventhandlery do daného fornuláře pro sledování jeho souřadnic a stavu Maximized.
        /// </summary>
        /// <param name="owner"></param>
        private void _LinkBoundsEvents(Form owner)
        {
            this._Position_Restore();
            if (owner != null)
            {
                owner.LocationChanged += _Position_LocationChanged;
                owner.SizeChanged += _Position_SizeChanged;
                owner.FormClosed += _Position_FormClosed;
            }
            this._Position_Apply(false);
        }
        /// <summary>
        /// Deaktivuje svoje eventhandlery do daného fornuláře pro sledování jeho souřadnic a stavu Maximized.
        /// </summary>
        /// <param name="owner"></param>
        private void _UnLinkBoundsEvents(Form owner)
        {
            if (owner != null)
            {
                owner.LocationChanged += _Position_LocationChanged;
                owner.SizeChanged += _Position_SizeChanged;
                owner.FormClosed += _Position_FormClosed;
            }
        }
        /// <summary>
        /// Po změně velikosti okna provede uložení aktuálních hodnot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Position_SizeChanged(object sender, EventArgs e)
        {
            _Position_FetchCurrent();
        }
        /// <summary>
        /// Po změně velikosti okna provede uložení aktuálních hodnot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Position_LocationChanged(object sender, EventArgs e)
        {
            _Position_FetchCurrent();
        }
        /// <summary>
        /// Při zavírání okna uložíme jeho pozici do konfigurace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Position_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._Position_Store();
        }
        /// <summary>
        /// Zachytí do svých proměnných informace o aktuální pozici a stavu Maximized o okně.
        /// Volá se z eventhandlerů o změnách.
        /// </summary>
        private void _Position_FetchCurrent()
        {
            if (__IgnorePositionChange) return;
            if (!this._OwnerWasShown) return;

            var form = _Owner;
            if (form is null) return;
            var currentState = form.WindowState;
            bool isMdiChild = form.IsMdiChild;
            __CurrentIsMdiChild = isMdiChild;
            if (!isMdiChild)
            {   // Informace ukládám jen o "plovoucím okně" = nikoliv když je Tabované.
                switch (currentState)
                {   // Pouze Normal a Maximized (pro Minimized neukládám nic !!!), a souřadnice odděleně:
                    // Na toto ukládání navazuje metoda _Position_Apply(bool isShow)
                    case FormWindowState.Normal:
                        __CurrentWindowState = FormWindowState.Normal;
                        __CurrentNormalBounds = form.Bounds;
                        break;
                    case FormWindowState.Maximized:
                        __CurrentWindowState = FormWindowState.Maximized;
                        __CurrentMaximizedBounds = form.Bounds;
                        break;
                }
            }
        }
        /// <summary>
        /// Aplikuje Config hodnoty o pozici okna, v rámci inicializace i 
        /// </summary>
        /// <param name="isShow"></param>
        private void _Position_Apply(bool isShow)
        {
            var owner = this._Owner;
            var iOwner = this._IOwner;
            if (owner != null)
            {
                try
                {
                    __IgnorePositionChange = true;

                    var windowState = __ConfigWindowState;
                    if (windowState.HasValue)
                    {
                        if (windowState.Value == FormWindowState.Maximized)
                        {   // Při zavření okna (když se ukládala konfigurace) bylo Maximized
                            if (owner.WindowState != FormWindowState.Maximized) owner.WindowState = FormWindowState.Maximized;
                            if (owner.StartPosition != FormStartPosition.Manual) owner.StartPosition = FormStartPosition.Manual;
                            this.__PositionIsFromConfig = true;
                        }
                        else
                        {   // Při zavření okna (když se ukládala konfigurace) bylo Normal (nebo Minimized, ale pak jej otevřu jako Normal)
                            var normalBounds = __ConfigNormalBounds;
                            if (normalBounds.HasValue)
                            {
                                var bounds = normalBounds.Value.FitIntoMonitors(true, false, true);
                                if (owner.WindowState != FormWindowState.Normal) owner.WindowState = FormWindowState.Normal;
                                if (owner.StartPosition != FormStartPosition.Manual) owner.StartPosition = FormStartPosition.Manual;
                                if (owner.Bounds != normalBounds) owner.Bounds = bounds;
                                this.__PositionIsFromConfig = true;
                            }
                        }
                        __IgnorePositionChange = false;
                        _Position_FetchCurrent();
                    }
                }
                finally
                {
                    __IgnorePositionChange = false;
                }
            }
        }
        /// <summary>
        /// Z konfigurace načte data o pozici tohoto okna a uloží je do hodnot __Config.
        /// Neprovádí jejich aplikování, to se provede v pravý čas.
        /// </summary>
        /// <param name="owner"></param>
        private void _Position_Restore()
        {
            var iOwner = this._IOwner;
            if (iOwner != null)
            {
                var nameSuffix = "_" + MainApp.CurrentMonitorsKey;
                var configName = iOwner.PositionConfigName;
                var positionData = (!String.IsNullOrEmpty(configName) ? _PositionLoadFromConfig(configName, nameSuffix) : iOwner.PositionLoadFromConfig(nameSuffix));
                __ConfigPositionData = positionData;
                _TryParsePositionData(positionData, out __ConfigIsMdiChild, out __ConfigWindowState, out __ConfigNormalBounds, out __ConfigMaximizedBounds);
            }
        }
        /// <summary>
        /// Metoda načte a vrátí data o pozici okna z defaultní konfigurace pro dané jméno
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="nameSuffix"></param>
        /// <returns></returns>
        private string _PositionLoadFromConfig(string configName, string nameSuffix)
        {
            bool hasPosition = MainApp.Settings.TryGetValue("FormPosition", configName + nameSuffix, out string positionData);
            if (!hasPosition)
                hasPosition = MainApp.Settings.TryGetValue("FormPosition", configName, out positionData);
            return (hasPosition ? positionData : null);
        }
        /// <summary>
        /// Hodnoty o pozici okna __Current uloží do konfigurace.
        /// </summary>
        private void _Position_Store()
        {
            if (!this._OwnerWasShown) return;

            var iOwner = this._IOwner;
            if (iOwner != null)
            {
                var positionData = _CreatePositionData(__CurrentIsMdiChild, __CurrentWindowState, __CurrentNormalBounds, __CurrentMaximizedBounds);
                if (!String.IsNullOrEmpty(positionData) && (String.IsNullOrEmpty(__ConfigPositionData) || !String.Equals(__ConfigPositionData, positionData)))
                {   // Data o pozici okna uložím do konfigurace jen tehdy, když se mi od posledně změnila = uživatel něco udělal s pozicí okna:
                    var nameSuffix = "_" + MainApp.CurrentMonitorsKey;
                    var configName = iOwner.PositionConfigName;
                    if (!String.IsNullOrEmpty(configName))
                        this._PositionSaveToConfig(positionData, configName, nameSuffix);
                    else
                        iOwner.PositionSaveToConfig(positionData, nameSuffix);
                }
            }
        }
        /// <summary>
        /// Metoda uloží dodaná data o pozici okna do defaultní konfigurace pro dané jméno
        /// </summary>
        /// <param name="positionData"></param>
        /// <param name="configName"></param>
        /// <param name="nameSuffix"></param>
        private void _PositionSaveToConfig(string positionData, string configName, string nameSuffix)
        {
            MainApp.Settings.SetValue("FormPosition", configName, positionData);
            MainApp.Settings.SetValue("FormPosition", configName + nameSuffix, positionData);
        }
        /// <summary>
        /// Z konfiguračního stringu najde a parsuje jednotlivé hodnoty
        /// </summary>
        /// <param name="configData"></param>
        /// <param name="isMdiChild"></param>
        /// <param name="windowState"></param>
        /// <param name="normalBounds"></param>
        /// <param name="maximizedBounds"></param>
        /// <returns></returns>
        private static bool _TryParsePositionData(string configData, out bool? isMdiChild, out FormWindowState? windowState, out Rectangle? normalBounds, out Rectangle? maximizedBounds)
        {
            if (!String.IsNullOrEmpty(configData))
            {
                var keyValues = configData.SplitToKeyValues(";", "=", true, true);
                if (keyValues != null &&
                    keyValues.TryGetFirst(q => String.Equals(q.Key, "MC", StringComparison.OrdinalIgnoreCase), out var mcv) &&
                    keyValues.TryGetFirst(q => String.Equals(q.Key, "WS", StringComparison.OrdinalIgnoreCase), out var wsv))
                {   // Pokud jsme našli všechny očekávané povinné hodnoty:
                    // Dohledáme i nepovinné hodnoty:
                    bool hasNb = keyValues.TryGetFirst(q => String.Equals(q.Key, "NB", StringComparison.OrdinalIgnoreCase), out var nbv);
                    bool hasXb = keyValues.TryGetFirst(q => String.Equals(q.Key, "XB", StringComparison.OrdinalIgnoreCase), out var xbv);

                    // Konverze ze string Value do cílového typu:
                    var mc = (bool)Convertor.StringToBoolean(mcv.Value);
                    var ws = Convertor.StringToEnum<FormWindowState>(wsv.Value);
                    var nb = hasNb ? (Rectangle)Convertor.StringToRectangle(nbv.Value, ',') : Rectangle.Empty;
                    var xb = hasXb ? (Rectangle)Convertor.StringToRectangle(xbv.Value, ',') : Rectangle.Empty;
                    if (ws == FormWindowState.Maximized || (hasNb && !nb.IsEmpty))
                    {
                        isMdiChild = mc;
                        windowState = ws;
                        normalBounds = (hasNb && !nb.IsEmpty) ? (Rectangle?)nb : (Rectangle?)null;
                        maximizedBounds = (hasXb && !xb.IsEmpty) ? (Rectangle?)xb : (Rectangle?)null;

                        return true;
                    }
                }
            }
            isMdiChild = null;
            windowState = null;
            normalBounds = null;
            maximizedBounds = null;
            return false;
        }
        /// <summary>
        /// Z dodaných jednotlivých hodnot vrátí konfigurační string popisující stav a pozici okna
        /// </summary>
        /// <returns></returns>
        private static string _CreatePositionData(bool? isMdiChild, FormWindowState? windowState, Rectangle? normalBounds, Rectangle? maximizedBounds)
        {
            string positionData = null;
            if (isMdiChild.HasValue && windowState.HasValue && (windowState.Value == FormWindowState.Maximized || normalBounds.HasValue))
            {   // Povinné jsou isMdiChild a windowState, a pokud windowState je Maximized tak víc nic, jinak je povinné ještě normalBounds:
                positionData = $"MC={Convertor.BooleanToString(isMdiChild.Value)};" +
                               $"WS={Convertor.EnumToString(windowState.Value)}";
                // Souřadnice jsou obecně Optional: když jsou dodané, tak je přidáme. Hodnota normalBounds musí být dodaná, pokud windowState není Maximized.
                if (normalBounds.HasValue)
                    positionData += $";NB={Convertor.RectangleToString(normalBounds.Value, ',')}";
                if (maximizedBounds.HasValue)
                    positionData += $";XB={Convertor.RectangleToString(maximizedBounds.Value, ',')}";
            }
            return positionData;
        }

        private bool __IgnorePositionChange;
        private bool __PositionIsFromConfig;

        private string __ConfigPositionData;
        private bool? __ConfigIsMdiChild;
        private FormWindowState? __ConfigWindowState;
        private Rectangle? __ConfigNormalBounds;
        private Rectangle? __ConfigMaximizedBounds;

        private bool? __CurrentIsMdiChild;
        private FormWindowState? __CurrentWindowState;
        private Rectangle? __CurrentNormalBounds;
        private Rectangle? __CurrentMaximizedBounds;
        #endregion
    }
    /// <summary>
    /// Interface popisující rozšířené pracovní vlastnosti Formulářů pro spolupráci formuláře a <see cref="FormStateInfo"/>
    /// </summary>
    internal interface IFormStatusWorking
    {
        /// <summary>
        /// Obsahuje true poté, kdy formulář byl zobrazen. 
        /// Obsahuje true již v metodě <c>OnFirstShownAfter</c> a v eventu <c>FirstShownAfter</c>.
        /// </summary>
        bool WasShown { get; }
        /// <summary>
        /// Provede akce jako po prvním Show
        /// </summary>
        void RunShow();
        /// <summary>
        /// Příznak z načtené konfigurace okna: true = okno bylo zavíráno ve stavu IsMdiChild (tedy jako Tabované) / false = bylo plovoucí
        /// </summary>
        bool? ConfigIsMdiChild { get; }
        /// <summary>
        /// Pozice okna byla nastavena v jeho konstruktoru z dat načtených z konfigurace. Bounds ani WindowState ani StartPosition by neměly být měněny.
        /// </summary>
        bool PositionIsFromConfig { get; }
        /// <summary>
        /// Vyvolá událost po změně stavu aktivity.
        /// </summary>
        /// <param name="args"></param>
        void RunActivityStateChanged(TEventValueChangedArgs<WindowActivityState> args);
        /// <summary>
        /// Jméno konfigurace v subsystému AsolDX.
        /// Pokud bude zde vráceno neprázdné jméno, pak načtení a uložení konfigurace okna zajistí sama třída, která implementuje <see cref="IFormStatusWorking"/>.
        /// Pokud nebude vráceno jméno, budou používány metody <see cref="PositionLoadFromConfig(string)"/> a <see cref="PositionSaveToConfig(string, string)"/>.
        /// </summary>
        string PositionConfigName { get; }
        /// <summary>
        /// Pokusí se z konfigurace najít a načíst string popisující pozici okna.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Za toto jméno přidá suffix (začíná podtržítkem a obsahuje XML validní znaky) a vyhledá konfiguraci se suffixem.<br/>
        /// 3. Pokud nenajde konfiguraci se suffixem, vyhledá konfiguraci bez suffixu = obecná, posledně použití (viz <see cref="PositionSaveToConfig(string, string)"/>).<br/>
        /// 4. Nalezený string je ten, který byl uložen v metodě <see cref="PositionSaveToConfig(string, string)"/> a je roven parametru 'positionData'. Pokud položku v konfiguraci nenajde, vrátí null (nebo prázdný string).
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// <para/>
        /// Konverze formátů: Pokud v konfiguraci budou uložena stringová data ve starším formátu, než dokáže obsloužit zpracující třída <see cref="FormStateInfo"/>, pak konverzi do jejího formátu musí zajistit aplikační kód (protože on ví, jak zpracovat starý formát).<br/>
        /// <b><u>Postup:</u></b><br/>
        /// 1. Po načtení konfigurace se lze dotázat metodou <see cref="FormStateInfo.IsPositionDataValid(string)"/>, zda načtená data jsou validní.<br/>
        /// 2. Pokud nejsou validní, pak je volající aplikace zkusí analyzovat svým starším (legacy) postupem na prvočinitele;<br/>
        /// 3. A pokud je úspěšně rozpoznala, pak ze základních dat sestaví validní konfirurační string s pomocí metody <see cref="FormStateInfo.CreatePositionData(bool?, FormWindowState?, Rectangle?, Rectangle?)"/>.<br/>
        /// </summary>
        /// <param name="nameSuffix">Suffix ke jménu konfigurace, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory</param>
        /// <returns></returns>
        string PositionLoadFromConfig(string nameSuffix);
        /// <summary>
        /// Do konfigurace uloží dodaná data o pozici okna '<paramref name="positionData"/>'.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Jednak uloží data <paramref name="positionData"/> přímo do položky konfigurace pod svým vlastním jménem bez suffixu = data obecná pro libovolnou konfiguraci monitorů.<br/>
        /// 3. A dále uloží tato data do položky konfigurace, kde za svoje jméno přidá dodaný suffix <paramref name="nameSuffix"/> = tato hodnota se použije po restore na shodné konfiguraci monitorů.<br/>
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// </summary>
        /// <param name="positionData"></param>
        /// <param name="nameSuffix"></param>
        void PositionSaveToConfig(string positionData, string nameSuffix);
    }

    /// <summary>
    /// Stav aktivity okna
    /// </summary>
    public enum WindowActivityState
    {
        /// <summary>
        /// Výchozí stav, v němž se okno nikdy nenachází
        /// </summary>
        None,
        /// <summary>
        /// Stav uvnitř konstruktoru, na konci konstruktoru přechází do <see cref="Initialized"/>.
        /// </summary>
        Creating,
        /// <summary>
        /// Stav na konci konstruktoru, následovat bude <see cref="FirstShow"/>.
        /// </summary>
        Initialized,
        /// <summary>
        /// Právě bylo zahájeno první zobrazení okna
        /// </summary>
        FirstShow,
        /// <summary>
        /// Právě bylo zahájeno zobrazení okna (jak první, tak následující), volá se před fyzickým Show
        /// </summary>
        ShowBefore,
        /// <summary>
        /// Právě bylo dokončeno zobrazení okna
        /// </summary>
        ShowAfter,
        /// <summary>
        /// Okno již bylo zobrazeno a je viditelné, nyní může být aktivováno, nebo skryto nebo zavřeno
        /// </summary>
        Visible,
        /// <summary>
        /// Okno je viditelné a aktivní (má Focus)
        /// </summary>
        Active,
        /// <summary>
        /// Okno je viditelné, ale nemá focus
        /// </summary>
        Inactive,
        /// <summary>
        /// Okno je skryto
        /// </summary>
        Invisible,
        /// <summary>
        /// Bylo zahájeno zavírání okna
        /// </summary>
        Closing,
        /// <summary>
        /// Okno je zavřeno, ale dosud neprošlo Dispose
        /// </summary>
        Closed,
        /// <summary>
        /// Začal proces Dispose
        /// </summary>
        Disposing,
        /// <summary>
        /// Ukončen proces Dispose
        /// </summary>
        Disposed
    }
    #endregion
    #region class TEventValueChangeArgs<T> : Třída argumentů obsahující dva prvky generického typu OldValue a NewValue
    /// <summary>
    /// Třída argumentů obsahující dva prvky generického typu <typeparamref name="T"/> s charakterem 
    /// Původní hodnota <see cref="OldValue"/> a Nová hodnota <see cref="NewValue"/>.
    /// Novou hodnotu <see cref="NewValue"/> lze upravit (setovat), a zdroj eventu ji možná převezme.
    /// Lze nastavit <see cref="Cancel"/> = true, a zdroj eventu možná akci zruší.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TEventValueChangeArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source">Zdroj události</param>
        /// <param name="oldValue">Původní hodnota</param>
        /// <param name="newValue">Nová hodnota</param>
        public TEventValueChangeArgs(EventSource source, T oldValue, T newValue) { Source = source; OldValue = oldValue; _NewValue = newValue; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Change from: {OldValue}, to: {NewValue}, source: {Source}";
        }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSource Source { get; private set; }
        /// <summary>
        /// Původní hodnota. Nelze změnit.
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Nová hodnota. 
        /// Hodnotu lze změnit, a zdroj eventu ji možná převezme.
        /// Vložením hodnoty dojde k nastavení <see cref="Changed"/> na true.
        /// </summary>
        public T NewValue { get { return _NewValue; } set { Changed = true; _NewValue = value; } }
        private T _NewValue;
        /// <summary>
        /// Zrušit událost? default = false, lze nastavit.
        /// </summary>
        public bool Cancel { get; set; } = false;
        /// <summary>
        /// Bude nastaveno na true poté, kdy aplikace vloží novou hodnotu do <see cref="NewValue"/>.
        /// A to bez ohledu na změnu hodnoty.
        /// </summary>
        public bool Changed { get; private set; } = false;
    }
    /// <summary>
    /// Třída argumentů obsahující dva prvky generického typu <typeparamref name="T"/> s charakterem 
    /// Původní hodnota <see cref="OldValue"/> a Nová hodnota <see cref="NewValue"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TEventValueChangedArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source">Zdroj události</param>
        /// <param name="oldValue">Původní hodnota</param>
        /// <param name="newValue">Nová hodnota</param>
        public TEventValueChangedArgs(EventSource source, T oldValue, T newValue) { Source = source; OldValue = oldValue; NewValue = newValue; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Change from: {OldValue}, to: {NewValue}, source: {Source}";
        }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSource Source { get; private set; }
        /// <summary>
        /// Původní hodnota. Nelze změnit.
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Nová hodnota. Nelze změnit.
        /// </summary>
        public T NewValue { get; private set; }
    }
    /// <summary>
    /// Zdroj eventu
    /// </summary>
    public enum EventSource
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Zásah kódu
        /// </summary>
        Code,
        /// <summary>
        /// Interaktivní akce uživatele
        /// </summary>
        User
    }
    #endregion
}
