using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Components.Graph;
using Noris.LCS.Base.WorkScheduler;
using R = Noris.LCS.Base.WorkScheduler.Resources;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// <summary>
    /// MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// </summary>
    public class MainData : IMainDataInternal, IFunctionProvider
    {
        #region Konstrukce a privátní proměnné
        /// <summary>
        /// Konstruktor pro konkrétního hostitele
        /// </summary>
        /// <param name="host"></param>
        public MainData(IAppHost host) : this(host, null) { }
        /// <summary>
        /// Konstruktor pro konkrétního hostitele a číslo Session
        /// </summary>
        /// <param name="host"></param>
        /// <param name="sessionId"></param>
        public MainData(IAppHost host, int? sessionId)
        {
            this._AppHost = host;
            this._SessionId = sessionId;
        }
        /// <summary>
        /// Obsahuje, true pokud máme vztah na datového hostitele
        /// </summary>
        private bool _HasHost { get { return (this._AppHost != null); } }
        private int? _SessionId;
        /// <summary>
        /// Datový hostitel
        /// </summary>
        private IAppHost _AppHost;
        private GuiData _GuiData;
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        #endregion
        #region Public metody a properties
        /// <summary>
        /// Načte data ze strukturovaného objektu <see cref="GuiData"/>
        /// </summary>
        /// <param name="guiData"></param>
        public void LoadData(GuiData guiData)
        {
            this._GuiData = guiData;
            this._LoadGuiToolbar(guiData.ToolbarItems);
            this._LoadGuiPanels(guiData.Pages);
            this._LoadGuiContext(guiData.ContextMenuItems);
        }
        /// <summary>
        /// Vytvoří a vrátí new WinForm control, obsahující kompletní strukturu pro zobrazení dodaných dat.
        /// Control rovnou vloží do dodaného Formu.
        /// Nastaví vlastnosti dodaného Formu podle dat v <see cref="GuiData.Properties"/>.
        /// </summary>
        /// <returns></returns>
        public System.Windows.Forms.Control CreateControlToForm(Form mainForm)
        {
            this._ApplyPropertiesToForm(mainForm);         // Nastavíme vlastnosti formu podle GuiProperties
            this._MainControl = new MainControl(this);     // Vytvoříme new control MainControl
            this._FillMainControlFromGui();                // Do controlu MainControl vygenerujeme všechny jeho controly
            mainForm.Controls.Add(this._MainControl);      // Control MainControl vložíme do formu
            this._MainControl.Dock = DockStyle.Fill;       // Control MainControl roztáhneme na maximum
            return this._MainControl;                      // hotovo!
        }
        /// <summary>
        /// Vytvoří a vrátí new WinForm control, obsahující kompletní strukturu pro zobrazení dodaných dat
        /// </summary>
        /// <returns></returns>
        public System.Windows.Forms.Control CreateControl()
        {
            this._MainControl = new MainControl(this);
            this._FillMainControlFromGui();
            return this._MainControl;
        }
        /// <summary>
        /// Hlavní objekt s daty <see cref="GuiData"/>
        /// </summary>
        public GuiData GuiData { get { return this._GuiData; } }
        #endregion
        #region Vytváření controlu, jeho vložení do Formu
        /// <summary>
        /// Do předaného formu vloží data z nastavení v <see cref="GuiProperties"/>
        /// </summary>
        /// <param name="mainForm"></param>
        private void _ApplyPropertiesToForm(Form mainForm)
        {
            GuiProperties guiProperties = this._GuiData.Properties;
            mainForm.FormBorderStyle = _ConvertBorderStyle(guiProperties.PluginFormBorder);
            mainForm.WindowState = _ConvertWindowState(guiProperties.PluginFormIsMaximized);
            mainForm.Text = guiProperties.PluginFormTitle;
            mainForm.FormClosing += _MainFormClosing;
            mainForm.FormClosed += _MainFormClosed;
        }
        /// <summary>
        /// Vrátí WinForm styl borderu podle Plugin stylu.
        /// Důvod? Nechceme vždycky referencovat WinFormy kvůli <see cref="System.Windows.Forms.FormBorderStyle"/>, stačí nám naklonovaný enum <see cref="PluginFormBorderStyle"/>.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        private static FormBorderStyle _ConvertBorderStyle(PluginFormBorderStyle style)
        {
            switch (style)
            {
                case PluginFormBorderStyle.None: return FormBorderStyle.None;
                case PluginFormBorderStyle.FixedSingle: return FormBorderStyle.FixedSingle;
                case PluginFormBorderStyle.Fixed3D: return FormBorderStyle.Fixed3D;
                case PluginFormBorderStyle.FixedDialog: return FormBorderStyle.FixedDialog;
                case PluginFormBorderStyle.Sizable: return FormBorderStyle.Sizable;
                case PluginFormBorderStyle.FixedToolWindow: return FormBorderStyle.FixedToolWindow;
                case PluginFormBorderStyle.SizableToolWindow: return FormBorderStyle.SizableToolWindow;
            }
            return FormBorderStyle.Sizable;
        }
        /// <summary>
        /// Vrátí WinForm stav okna podle nastavení Pluginu.
        /// Důvod? Nechceme vždycky referencovat WinFormy kvůli <see cref="System.Windows.Forms.FormWindowState"/>, stačí nám boolean IsMaximized.
        /// </summary>
        /// <param name="isMaximized"></param>
        /// <returns></returns>
        private static FormWindowState _ConvertWindowState(bool isMaximized)
        {
            return (isMaximized ? FormWindowState.Maximized : FormWindowState.Normal);
        }
        /// <summary>
        /// Z dat dodaných v prvcích GuiItem vytvoří vizuální controly a vloží je do Main WinForm controlu
        /// </summary>
        private void _FillMainControlFromGui()
        {
            this._FillMainControlToolbar();
            this._FillMainControlPagesFromGui();
        }
        /// <summary>
        /// Reference na hlavní GUI control, který je vytvořen v metodě <see cref="CreateControlToForm(Form)"/>
        /// </summary>
        protected MainControl _MainControl;
        #endregion
        #region Zavírání hlavního okna
        private void _MainFormClosing(object sender, FormClosingEventArgs e)
        {
            
        }
        /// <summary>
        /// Event při zavření okna: pošleme informaci hostiteli.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainFormClosed(object sender, FormClosedEventArgs e)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_CloseWindow;
            this._CallAppHostFunction(request, null);
        }
        #endregion
        #region Vyvolání akcí z pluginu do hostitele IAppHost
        /// <summary>
        /// Metoda zavolá hostitele <see cref="_AppHost"/>, jeho metodu <see cref="IAppHost.CallAppHostFunction(AppHostRequestArgs)"/>,
        /// předá jí aktuální <see cref="_SessionId"/> a požadavek, a zajistí zavolání metody (callBackAction) po doběhnutí funkce.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        private void _CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction)
        {
            if (!this._CheckAppHost(request)) return;
            AppHostRequestArgs args = new AppHostRequestArgs(this._SessionId, request, null, null);
            this._AppHost.CallAppHostFunction(args);
        }
        /// <summary>
        /// Prověří správnost zadání requestu a existenci AppHost.
        /// Pokud je vše OK, vrací true.
        /// Pokud není, dá hlášku a vrací false.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool _CheckAppHost(GuiRequest request)
        {
            if (this._HasHost) return true;
            string message = "Je vyžadováno provedení funkce IAppHost:\r\n" + request.ToString() + ",\r\nale není zadán datový hostitel.";
            System.Windows.Forms.MessageBox.Show(message);
            return false;
        }
        #endregion
        #region Toolbar
        /// <summary>
        /// Načte položky do Toolbaru z dodaných dat Gui
        /// </summary>
        /// <param name="guiToolbarPanel"></param>
        private void _LoadGuiToolbar(GuiToolbarPanel guiToolbarPanel)
        {
            this._GuiToolbarPanel = guiToolbarPanel;
        }
        /// <summary>
        /// Data o ToolBaru z Gui
        /// </summary>
        private GuiToolbarPanel _GuiToolbarPanel;
        /// <summary>
        /// Z dat v <see cref="_GuiToolbarPanel"/> naplní toolbar
        /// </summary>
        private void _FillMainControlToolbar()
        {
            this._MainControl.ClearToolBar();
            this._MainControl.ToolBarVisible = this._GuiToolbarPanel.ToolbarVisible;
            this._FillMainControlToolbarFromSystem();
            this._FillMainControlToolbarFromGui();
            this._MainControl.ToolBarItemClicked += _ToolBarItemClicked;
            this._MainControl.ToolBarItemSelectedChange += _ToolBarItemSelectedChange;
        }
        /// <summary>
        /// Do toolbaru vloží systémové funkce
        /// </summary>
        private void _FillMainControlToolbarFromSystem()
        {
            if (!this._GuiToolbarPanel.ToolbarVisible) return;
            // Systémové položky Toolbaru jsou položky třídy FunctionGlobalItem, nemají v sobě instanci GuiToolbarItem.

            this._TimeAxisToolBarInit();
        }
        /// <summary>
        /// Metoda vytvoří a vrátí prvek FunctionGlobalItem pro dané zadání
        /// </summary>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <param name="text"></param>
        /// <param name="toolTip"></param>
        /// <param name="size"></param>
        /// <param name="imageHot"></param>
        /// <param name="itemType"></param>
        /// <param name="layoutHint"></param>
        /// <param name="moduleWidth"></param>
        /// <param name="isSelectable"></param>
        /// <param name="isSelected"></param>
        /// <param name="selectionGroupName"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private FunctionGlobalItem _CreateToolbarItem(string name, string image, string text, string toolTip,
            FunctionGlobalItemSize size = FunctionGlobalItemSize.Half, string imageHot = null, FunctionGlobalItemType itemType = FunctionGlobalItemType.Button,
            LayoutHint layoutHint = LayoutHint.Default, int? moduleWidth = null,
            bool isSelectable = false, bool isSelected = false, string selectionGroupName = null, object userData = null)
        {
            FunctionGlobalItem functionItem = new FunctionGlobalItem(this);
            functionItem.Name = name;
            functionItem.Image = Application.App.Resources.GetImage(image);
            functionItem.Text = text;
            functionItem.ToolTip = toolTip;
            functionItem.Size = size;
            functionItem.ImageHot = Application.App.Resources.GetImage(imageHot);
            functionItem.ItemType = itemType;
            functionItem.LayoutHint = layoutHint;
            functionItem.ModuleWidth = moduleWidth;
            functionItem.IsCheckable = isSelectable;
            functionItem.IsChecked = isSelected;
            functionItem.CheckedGroupName = selectionGroupName;
            functionItem.UserData = userData;
            return functionItem;
        }
        /// <summary>
        /// Do toolbaru vloží aplikační funkce
        /// </summary>
        private void _FillMainControlToolbarFromGui()
        {
            if (!this._GuiToolbarPanel.ToolbarVisible) return;
            if (this._GuiToolbarPanel.Items == null) return;
            // Aplikační položky Toolbaru jsou položky třídy ToolBarItem, mají v sobě instanci GuiToolbarItem:

            // Nejprve sestavíme jednotlivé grupy pro prvky, podle názvu grup kam chtějí tyto prvky jít:
            Dictionary<string, FunctionGlobalGroup> toolBarGroups = new Dictionary<string, FunctionGlobalGroup>();
            string defaultGroupName = "FUNKCE";
            foreach (GuiToolbarItem guiToolBarItem in this._GuiToolbarPanel.Items)
            {
                if (guiToolBarItem == null) continue;

                string groupName = guiToolBarItem.GroupName;
                if (String.IsNullOrEmpty(groupName)) groupName = defaultGroupName;
                FunctionGlobalGroup group;
                if (!toolBarGroups.TryGetValue(groupName, out group))
                {
                    group = new FunctionGlobalGroup() { Title = groupName };
                    toolBarGroups.Add(groupName, group);
                }
                ToolBarItem item = ToolBarItem.Create(this, guiToolBarItem);
                if (item != null)
                    group.Items.Add(item);
            }

            // Výsledky (jednotlivé grupy, kde každá obsahuje sadu prvků = buttonů) vložím do předaného pole:
            if (toolBarGroups.Count > 0)
                this._MainControl.AddToolBarGroups(toolBarGroups.Values);
        }
        /// <summary>
        /// Obsluha události ItemSelectedChange na ToolBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBarItemSelectedChange(object sender, FunctionItemEventArgs args)
        {
            GuiToolbarItem guiToolbarItem = _GetGuiToolBarItem(args);
            if (guiToolbarItem != null)
                this._ToolBarItemSelectedChangeApplication(guiToolbarItem);
            else
                this._ToolBarItemSelectedChangeSystem(args.Item);
        }
        /// <summary>
        /// Obsluha události ItemClick na ToolBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ToolBarItemClicked(object sender, FunctionItemEventArgs args)
        {
            GuiToolbarItem guiToolbarItem = _GetGuiToolBarItem(args);
            if (guiToolbarItem != null)
                this._ToolBarItemClickApplication(guiToolbarItem);
            else
                this._ToolBarItemClickSystem(args.Item);
        }
        /// <summary>
        /// Obsluha události ItemSelectedChange na Aplikační položce ToolBaru.
        /// Tuto akci zatím do aplikační funkce NEPŘEDÁVÁME. Neřešíme tedy ani její Response.
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        private void _ToolBarItemSelectedChangeApplication(GuiToolbarItem guiToolbarItem)
        {
            /* Tuto akci zatím do aplikační funkce NEPŘEDÁVÁME. Neřešíme tedy ani její Response. */
        }
        /// <summary>
        /// Obsluha události ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        private void _ToolBarItemClickApplication(GuiToolbarItem guiToolbarItem)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_ToolbarClick;
            request.ToolbarItem = guiToolbarItem;
            request.CurrentState = this._CreateGuiCurrentState();
            this._CallAppHostFunction(request, this._ToolBarItemClickApplicationResponse);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="response"></param>
        private void _ToolBarItemClickApplicationResponse(AppHostResponseArgs response)
        {
        }
        /// <summary>
        /// Obsluha události ItemSelectedChange na Systémové položce ToolBaru
        /// </summary>
        /// <param name="item"></param>
        private void _ToolBarItemSelectedChangeSystem(FunctionItem item)
        {
            this._TimeAxisToolBarSelected(item);
        }
        /// <summary>
        /// Obsluha události ItemClick na Systémové položce ToolBaru
        /// </summary>
        /// <param name="item"></param>
        private void _ToolBarItemClickSystem(FunctionItem item)
        {
            this._TimeAxisToolBarClick(item);
        }
        /// <summary>
        /// Metoda vrátí instanci <see cref="GuiToolbarItem"/> pro položku toolbaru z dodaného argumentu.
        /// Pokud argument obsahuje položku, která nepochází z <see cref="GuiToolbarItem"/>, 
        /// pak jde o systémovou položku toolbaru, tato metoda vrací null a požadovaná akce se řeší interně.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static GuiToolbarItem _GetGuiToolBarItem(FunctionItemEventArgs args)
        {
            ToolBarItem toolBarItem = args.Item as ToolBarItem;
            if (toolBarItem == null) return null;
            return toolBarItem.GuiToolbarItem;
        }
        /// <summary>
        /// ContextFunctionItem : adapter mezi <see cref="GuiToolbarItem"/>, a položkou kontextového menu <see cref="FunctionGlobalItem"/>.
        /// </summary>
        protected class ToolBarItem : FunctionGlobalItem
        {
            #region Konstrukce, načtení dat
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="guiToolBarItem"></param>
            protected ToolBarItem(IFunctionProvider provider, GuiToolbarItem guiToolBarItem) : base(provider)
            {
                this._GuiToolBarItem = guiToolBarItem;
                this.Size = FunctionGlobalItemSize.Half;
                this.ItemType = FunctionGlobalItemType.Button;
            }
            /// <summary>
            /// Obsahuje true v situaci, kdy existuje <see cref="_GuiToolBarItem"/>
            /// </summary>
            private bool _HasItem { get { return (this._GuiToolBarItem != null); } }
            /// <summary>
            /// Z této položky je funkce načtena
            /// </summary>
            private GuiToolbarItem _GuiToolBarItem;
            /// <summary>
            /// Vytvoří a vrátí new instanci <see cref="ToolBarItem"/> pro funkci definovanou v <see cref="GuiToolbarItem"/>.
            /// Může vrátit null pro neplatné zadání.
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="guiToolBarItem"></param>
            /// <returns></returns>
            public static ToolBarItem Create(IFunctionProvider provider, GuiToolbarItem guiToolBarItem)
            {
                if (provider == null || guiToolBarItem == null) return null;

                ToolBarItem toolBarItem = new ToolBarItem(provider, guiToolBarItem);
                return toolBarItem;
            }
            #endregion
            #region Public property FunctionGlobalItem, načítané z GuiToolbarItem, a explicitně přidané
            /// <summary>
            /// GUI Prvek toolbaru, z něhož je tato položka vytvořena. Jde o data dodaná z aplikace.
            /// </summary>
            public GuiToolbarItem GuiToolbarItem { get { return this._GuiToolBarItem; } }
            /// <summary>
            /// Text do funkce
            /// </summary>
            public override string TextText { get { return this._GuiToolBarItem.Title; } }
            /// <summary>
            /// ToolTip k textu
            /// </summary>
            public override string ToolTipText { get { return this._GuiToolBarItem.ToolTip; } }
            /// <summary>
            /// Obrázek
            /// </summary>
            public override Image Image { get { return (this._HasItem ? App.Resources.GetImage(this._GuiToolBarItem.Image) : null); } }
            /// <summary>
            /// Obrázek pro stav MouseActive
            /// </summary>
            public override Image ImageHot { get { return (this._HasItem ? App.Resources.GetImage(this._GuiToolBarItem.ImageHot) : null); } }
            /// <summary>
            /// Velikost prvku na toolbaru, vzhledem k výšce toolbaru
            /// </summary>
            public override FunctionGlobalItemSize Size { get { return (this._HasItem ? this._GuiToolBarItem.Size : base.Size); } set { if (this._HasItem) this._GuiToolBarItem.Size = value; else base.Size = value; } }
            /// <summary>
            /// Explicitně požadovaná šířka prvku v počtu modulů
            /// </summary>
            public override int? ModuleWidth { get { return (this._HasItem ? this._GuiToolBarItem.ModuleWidth : base.ModuleWidth); } set { if (this._HasItem) this._GuiToolBarItem.ModuleWidth = value; else base.ModuleWidth = value; } }
            /// <summary>
            /// Nápověda ke zpracování layoutu této položky
            /// </summary>
            public override LayoutHint LayoutHint { get { return (this._HasItem ? this._GuiToolBarItem.LayoutHint : base.LayoutHint); } set { if (this._HasItem) this._GuiToolBarItem.LayoutHint = value; else base.LayoutHint = value; } }
            /// <summary>
            /// Název grupy, kde se tento prvek objeví. Nezadaná grupa = implicitní s názvem "FUNKCE".
            /// </summary>
            public string GroupName { get { return (this._HasItem ? this._GuiToolBarItem.GroupName : ""); } set { if (this._HasItem) this._GuiToolBarItem.GroupName = value; } }
            #endregion
        }
        #endregion
        #region Datové panely
        /// <summary>
        /// Načte položky do panelů z dodaných dat Gui
        /// </summary>
        /// <param name="guiPages"></param>
        private void _LoadGuiPanels(GuiPages guiPages)
        {
            this._GuiPages = guiPages;
        }
        /// <summary>
        /// Data o stránkách dat z Gui
        /// </summary>
        private GuiPages _GuiPages;
        /// <summary>
        /// Do připraveného vizuálního controlu <see cref="_MainControl"/> vloží objekty za jednotlivé stránky s daty z <see cref="_GuiPages"/>.
        /// </summary>
        private void _FillMainControlPagesFromGui()
        {
            this._MainControl.ClearPages();
            this._MainControl.SynchronizedTime.Value = this.GuiData.Properties.InitialTimeRange;
            this._MainControl.SynchronizedTime.ValueLimit = this.GuiData.Properties.TotalTimeRange;
            foreach (GuiPage guiPage in this._GuiPages.Pages)
                this._MainControl.AddPage(guiPage);
        }
        #endregion
        #region Kontextové menu
        /// <summary>
        /// Načte položky do Kontextových menu z dodaných dat Gui
        /// </summary>
        /// <param name="guiContextMenuSet"></param>
        private void _LoadGuiContext(GuiContextMenuSet guiContextMenuSet)
        {
            this._GuiContextMenuSet = guiContextMenuSet;

            List<ContextFunctionItem> functions = new List<ContextFunctionItem>();
            foreach (GuiContextMenuItem guiContextMenuItem in guiContextMenuSet.Items)
            {
                ContextFunctionItem item = ContextFunctionItem.Create(this, guiContextMenuItem);
                if (item != null)
                    functions.Add(item);
            }
            this._ContextFunctions = functions.ToArray();
        }
        /// <summary>
        /// Data o kontextových funkcích z Gui
        /// </summary>
        private GuiContextMenuSet _GuiContextMenuSet;
        /// <summary>
        /// Souhrn všech definovaných funkcí pro všechna kontextová menu v systému.
        /// Souhrn je načten z <see cref="GuiContextMenuSet"/> v metodě <see cref="_LoadGuiContext(GuiContextMenuSet)"/>, 
        /// z tohoto souhrnu je vytvořeno kontextové menu pro konkrétní situaci, v metodě <see cref="CreateContextMenu(GuiGridItemId)"/>.
        /// </summary>
        protected ContextFunctionItem[] _ContextFunctions;
        /// <summary>
        /// Metoda vytvoří a vrátí kontextové menu pro konkrétní prvek grafu, zadaný jeho identifikátorem
        /// </summary>
        /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
        /// <returns></returns>
        protected ToolStripDropDownMenu CreateContextMenu(GuiGridItemId gridItemId)
        {
            if (this._ContextFunctions == null || this._ContextFunctions.Length == 0) return null;              // Nejsou data => není menu.
            ContextFunctionItem[] items = this._ContextFunctions.Where(cfi => cfi.IsValidFor(gridItemId)).ToArray();  // Vybereme jen ty funkce, které jsou vhodné pro daný prvek
            if (items.Length == 0) return null;         // Nic se nehodí => nic se nezobrazí

            // Celkové menu:
            ToolStripDropDownMenu menu = FunctionItem.CreateDropDownMenuFrom(items, m =>
            {   // Tuto akci vyvolá metoda CreateDropDownMenuFrom() po vytvoření menu, ale před přidáním položek:
                m.Tag = gridItemId;
                m.Items.Add(new ToolStripLabel("NABÍDKA FUNKCÍ"));
                m.Items.Add(new ToolStripSeparator());
            });

            menu.ItemClicked += this.ContextMenuItemClicked;
            return menu;
        }
        /// <summary>
        /// Obsluha kliknutí na položku kontextového menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripDropDownMenu menu = sender as ToolStripDropDownMenu;
            if (menu == null) return;
            menu.Hide();

            // Vyjmeme data:  a) identifikátor prvku grafu (tabulka, řádek, prvek),   b) data konkrétní vybrané funkce:
            GuiGridItemId gridItemId = menu.Tag as GuiGridItemId;
            ContextFunctionItem funcArgs = e.ClickedItem.Tag as ContextFunctionItem;
            GuiContextMenuItem guiContextMenuItem = (funcArgs != null ? funcArgs.GuiContextMenuItem : null);
            if (guiContextMenuItem != null)
                // Kontextové menu obsahuje výhradně aplikační funkce (GuiContextMenuItem):
                this._ContextMenuItemClickApplication(guiContextMenuItem, gridItemId);
        }
        /// <summary>
        /// Obsluha události ItemClick na Aplikační položce kontextového menu
        /// </summary>
        /// <param name="guiContextMenuItem"></param>
        /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
        private void _ContextMenuItemClickApplication(GuiContextMenuItem guiContextMenuItem, GuiGridItemId gridItemId)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_ContextMenuClick;
            request.ContextMenuItem = guiContextMenuItem;
            request.ActiveGraphItem = gridItemId;
            request.CurrentState = this._CreateGuiCurrentState();
            this._CallAppHostFunction(request, this._ContextMenuItemClickApplicationResponse);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="response"></param>
        private void _ContextMenuItemClickApplicationResponse(AppHostResponseArgs response)
        {
        }
        /// <summary>
        /// ContextFunctionItem : adapter mezi <see cref="GuiContextMenuItem"/>, a položku kontextového menu <see cref="FunctionItem"/>.
        /// </summary>
        protected class ContextFunctionItem : FunctionItem
        {
            #region Konstrukce, načtení dat
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="guiContextMenuItem"></param>
            protected ContextFunctionItem(IFunctionProvider provider, GuiContextMenuItem guiContextMenuItem) : base(provider)
            {
                this._GuiContextMenuItem = guiContextMenuItem;
                this._VisibleFor = ContextFunctionValidInfo.ParseValidInfo(guiContextMenuItem.VisibleFor);
                this._EnableFor = ContextFunctionValidInfo.ParseValidInfo(guiContextMenuItem.EnableFor);
            }
            /// <summary>
            /// Z této deklarace je funkce načtena
            /// </summary>
            private GuiContextMenuItem _GuiContextMenuItem;
            /// <summary>
            /// Vytvoří a vrátí new instanci <see cref="ContextFunctionItem"/> pro data definovaná v <see cref="GuiContextMenuItem"/>.
            /// Může vrátit null pro neplatné zadání.
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="guiContextMenuItem"></param>
            /// <returns></returns>
            public static ContextFunctionItem Create(IFunctionProvider provider, GuiContextMenuItem guiContextMenuItem)
            {
                if (provider == null || guiContextMenuItem == null) return null;

                ContextFunctionItem funcItem = new ContextFunctionItem(provider, guiContextMenuItem);
                return funcItem;
            }
            #endregion
            #region Public override property třídy FunctionItem, načítané z GuiContextMenuItem
            /// <summary>
            /// Z této deklarace je funkce načtena
            /// </summary>
            public GuiContextMenuItem GuiContextMenuItem { get { return this._GuiContextMenuItem; } }
            /// <summary>
            /// Text do funkce
            /// </summary>
            public override string TextText { get { return this._GuiContextMenuItem.Title; } }
            /// <summary>
            /// ToolTip k textu
            /// </summary>
            public override string ToolTipText { get { return this._GuiContextMenuItem.ToolTip; } }
            /// <summary>
            /// Obrázek pro položku menu
            /// </summary>
            public override Image Image { get { return Application.App.Resources.GetImage(this._GuiContextMenuItem.Image); } }
            #endregion
            #region Určení dostupnosti položky pro konkrétní situaci
            /// <summary>
            /// Vrátí true, pokud tato položka kontextového menu se má použít pro prvek, jehož identifikátor <see cref="GuiGridItemId"/> je předán v parametru.
            /// Pokud tato metoda vrátí true, bude tato položka zařazena do menu.
            /// Položka si v této metodě může sama určit, zda bude v menu zobrazena jako Enabled nebo Disabled:
            /// Pokud chce být zobrazena, ale jako Disabled, pak si v této metodě nastaví <see cref="FunctionItem.IsEnabled"/> = false, a pak vrátí true.
            /// Následně bude vyvolána metoda <see cref="FunctionItem.CreateWinFormItem()"/>, která vygeneruje WinForm položku s odpovídající hodnotou <see cref="System.Windows.Forms.ToolStripMenuItem.Enabled"/>.
            /// Obdobně se do položky menu přebírají hodnoty <see cref="System.Windows.Forms.ToolStripMenuItem.CheckOnClick"/> (z <see cref="FunctionItem.IsCheckable"/>);
            /// a <see cref="System.Windows.Forms.ToolStripMenuItem.Checked"/> (z <see cref="FunctionItem.IsChecked"/>).
            /// <para/>
            /// Metoda je volána po každém RightClick, když se má zobrazit kontextové menu, a je volána pro každou definovanou položku kontextového menu.
            /// Její vyhodnocení by nemělo trvat déle než 1ms.
            /// </summary>
            /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
            /// <returns></returns>
            public bool IsValidFor(GuiGridItemId gridItemId)
            {
                bool isValid = ContextFunctionValidInfo.IsValidFor(gridItemId, this._VisibleFor);
                if (isValid)
                    this.IsEnabled = ContextFunctionValidInfo.IsValidFor(gridItemId, this._EnableFor);
                return isValid;
            }
            /// <summary>
            /// Pole regulárních výrazů, které definují prvky, pro které se má tato funkce zobrazovat.
            /// </summary>
            private ContextFunctionValidInfo[] _VisibleFor;
            /// <summary>
            /// Pole regulárních výrazů, které definují prvky, pro které má být tato funkce Enabled.
            /// </summary>
            private ContextFunctionValidInfo[] _EnableFor;
            #endregion
        }
        /// <summary>
        /// Třída, která řeší vhodnost konkrétní kontextové funkce <see cref="ContextFunctionItem"/> pro konkrétní prvek grafu <see cref="GuiGridItemId"/>.
        /// Podklady získává z definice <see cref="GuiContextMenuItem.VisibleFor"/> a <see cref="GuiContextMenuItem.EnableFor"/>.
        /// </summary>
        protected class ContextFunctionValidInfo
        {
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            /// <param name="fullNameRegex"></param>
            /// <param name="classDict"></param>
            private ContextFunctionValidInfo(System.Text.RegularExpressions.Regex fullNameRegex, Dictionary<int, object> classDict)
            {
                this.FullNameRegex = fullNameRegex;
                this.ClassDict = classDict;
            }
            /// <summary>
            /// Zadaná cesta k <see cref="GuiGrid"/>. Může obsahovat wildcards.
            /// </summary>
            protected System.Text.RegularExpressions.Regex FullNameRegex { get; private set; }
            /// <summary>
            /// Povolené třídy. Pokud není žádná, neřeší se.
            /// </summary>
            protected Dictionary<int, object> ClassDict { get; private set; }
            /// <summary>
            /// Metoda vrátí parsované pole prvků <see cref="ContextFunctionValidInfo"/>, získané z daného stringu.
            /// Může vrátit null.
            /// </summary>
            /// <param name="valid"></param>
            /// <returns></returns>
            public static ContextFunctionValidInfo[] ParseValidInfo(string valid)
            {
                if (String.IsNullOrEmpty(valid)) return null;             // Bez zadání => bez omezení => null

                List<ContextFunctionValidInfo> result = new List<ContextFunctionValidInfo>();
                string[] items = valid.Split(';');                        // Jednotlivé prvky ContextFunctionValidInfo
                foreach (string item in items)
                {
                    if (String.IsNullOrEmpty(item)) continue;
                    System.Text.RegularExpressions.Regex fullNameRegex = null;
                    Dictionary<int, object> classDict = null;
                    string[] parts = item.Split(':');                     // FullName:Classes
                    int partCount = parts.Length;
                    if (partCount == 1)
                    {   // Položka s jedním prvkem je autodetect:
                        classDict = _ParseClasses(parts[0], true);        // Parsování bude striktní = pokud najdu položku, která není číslo, pak se vrátí null
                        if (classDict == null)
                            fullNameRegex = _ParseRegex(parts[0]);        // Pokud to nejsou striktně jen číslice, může to být Regex
                    }
                    else if (partCount == 2)
                    {   // Položka s dvěma prvky je jasná:
                        fullNameRegex = _ParseRegex(parts[0]);
                        classDict = _ParseClasses(parts[1], false);       // Parsování nebude striktní = beru vše, co se najde
                    }
                    if (fullNameRegex != null || classDict != null)
                    {
                        ContextFunctionValidInfo info = new ContextFunctionValidInfo(fullNameRegex, classDict);
                        result.Add(info);
                    }
                }

                return result.ToArray();
            }
            /// <summary>
            /// Z dodaného textu, který obsahuje lidsky zadaný Regex, vrátí formální Regex
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static System.Text.RegularExpressions.Regex _ParseRegex(string text)
            {
                if (String.IsNullOrEmpty(text)) return null;
                return RegexSupport.CreateWildcardsRegex(text);
            }
            /// <summary>
            /// Z dodaného textu, který obsahuje čísla tříd oddělená čárkami, vrátí jejich Dictionary.
            /// </summary>
            /// <param name="text"></param>
            /// <param name="strictParse">Požadavek (true), aby se výsledná Dictionary vrátila pouze tehdy, když vstupní text je korektní = obsahuje jen čísla a oddělovače. Pokud by obsahoval jiné znaky, vrací se null.</param>
            /// <returns></returns>
            private static Dictionary<int, object> _ParseClasses(string text, bool strictParse)
            {
                if (String.IsNullOrEmpty(text)) return null;
                Dictionary<int, object> result = new Dictionary<int, object>();
                string[] items = text.Split(',');
                foreach (string item in items)
                {
                    string number = item.Trim();
                    int value;
                    if (number.Length > 0 && Int32.TryParse(number, out value))
                    {   // Je to číslo, OK:
                        if (value > 0 && !result.ContainsKey(value))
                            result.Add(value, null);
                    }
                    else if (strictParse)
                    {   // Není to číslo, a pokud máme být striktní, pak ihned vrátíme null:
                        return null;
                    }
                }
                return (result.Count == 0 ? null : result);
            }
            /// <summary>
            /// Metoda zjistí, zda pro daný prvek máme nějaké platné záznamy <see cref="ContextFunctionValidInfo"/>.
            /// Pokud není určeno, vrací se true.
            /// </summary>
            /// <param name="gridItemId"></param>
            /// <param name="infos"></param>
            /// <returns></returns>
            public static bool IsValidFor(GuiGridItemId gridItemId, ContextFunctionValidInfo[] infos)
            {
                if (gridItemId == null ||infos == null || infos.Length == 0) return true;      // Bez zadání => null => bez omezení => true

                foreach (ContextFunctionValidInfo info in infos)
                {
                    if (info.IsValidFor(gridItemId)) return true;
                }

                return false;
            }
            /// <summary>
            /// Vrátí true, pokud this instance je platná pro daný prvek
            /// </summary>
            /// <param name="gridItemId"></param>
            /// <returns></returns>
            public bool IsValidFor(GuiGridItemId gridItemId)
            {
                if (gridItemId == null || this.ClassDict == null || this.FullNameRegex == null) return true;      // Bez zadání => null => bez omezení => true
                if (!this._IsValidClass(gridItemId.ItemId, gridItemId.GroupId)) return false;
                if (!this._IsValidRegex(gridItemId.TableName)) return false;
                return true;
            }
            /// <summary>
            /// Vrátí true, pokud nejsme omezeni třídami (<see cref="ClassDict"/> je null), anebo pokud některá ze zadaných tříd je povolená.
            /// Vrátí false, pokud máme definované povolené třídy, ale ani jedna ze tříd zadaných na vstupu povolená není.
            /// </summary>
            /// <param name="guiIds"></param>
            /// <returns></returns>
            private bool _IsValidClass(params GuiId[] guiIds)
            {
                if (this.ClassDict == null) return true;
                foreach (GuiId guiId in guiIds)
                {   // Jakýkoli GuiId s povolenou třídou zajistí vrácení true:
                    if (this.ClassDict.ContainsKey(guiId.ClassId)) return true;
                }
                // Žádný zadaný GuiId nemá povolenou třídu, vrátíme false:
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud nejsme omezeni jménem tabulky (<see cref="FullNameRegex"/> je null), anebo pokud zadaná tabulka vyhovuje.
            /// Vrátí false, pokud máme zadané jméno tabulky, ale aktuálně zadaná nevyhovuje.
            /// </summary>
            /// <param name="fullName"></param>
            /// <returns></returns>
            private bool _IsValidRegex(string fullName)
            {
                if (this.FullNameRegex == null) return true;
                return this.FullNameRegex.IsMatch(fullName);
            }
        }
        #endregion
        #region Časová osa - tvorba menu v ToolBaru, a obsluha akcí tohoto menu
        /// <summary>
        /// Inicializace položek ToolBaru pro časovou osu
        /// </summary>
        private void _TimeAxisToolBarInit()
        {
            ToolbarSystemItem items = (ToolbarSystemItem)(this._GuiToolbarPanel.ToolbarShowSystemItems & ToolbarSystemItem.TimeAxisAll);
            if (items == ToolbarSystemItem.None) return;

            this._ToolbarTimeGroup = new FunctionGlobalGroup() { Title = "ČASOVÁ OSA", ToolTipTitle = "Posuny časové osy, změna měřítka", Order = "A1" };
            this._TimeAxisCurrentZoom = ToolbarSystemItem.TimeAxisZoomWholeWeek;
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomOneDay))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_Day, R.Images.Asol.ViewCalendarDay2Png, null, "Jeden den", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomOneDay));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWorkWeek))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WorkWeek, R.Images.Asol.ViewCalendarWorkweek2Png, null, "Pracovní týden Po-Pá", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomWorkWeek));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWorkWeek))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WholeWeek, R.Images.Asol.ViewCalendarWeek2Png, null, "Celý týden Po-Ne", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, isSelected: true, selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomWholeWeek));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomMonth))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_Month, R.Images.Asol.ViewCalendarMonth2Png, null, "Měsíc 30 dní", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemSkipToNextRow, isSelectable: true, selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomMonth));

            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoPrev))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoPrev, R.Images.Asol.GoPreviousViewPng, null, "Zpět = doleva = do minulosti", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, moduleWidth: 1, userData: ToolbarSystemItem.TimeAxisGoPrev));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoHome))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoHome, R.Images.Asol.GoHome4Png, null, "Aktuální čas", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, moduleWidth: 2, userData: ToolbarSystemItem.TimeAxisGoHome));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoNext))
                this._ToolbarTimeGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoNext, R.Images.Asol.GoNextViewPng, null, "Vpřed = doprava = do budoucnosti", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, moduleWidth: 1, userData: ToolbarSystemItem.TimeAxisGoNext));

            this._MainControl.AddToolBarGroup(this._ToolbarTimeGroup);
        }
        /// <summary>
        /// Metoda se volá po akci SelectedChange na systémové položce ToolBaru. 
        /// Metoda zjistí, zda akce se týká časové osy, a pokud ano pak ji vyřeší.
        /// Pokud se akce nijak netýká časové osy, pak nic neprovádí (není tedy problém ji zavolat).
        /// </summary>
        /// <param name="item"></param>
        private void _TimeAxisToolBarSelected(FunctionItem item)
        {
            /*
            if (!item.IsSelected) return;        // Sem chodí obě události: jak pro objekt, jehož IsSelected je nyní false, tak i pro objekt s IsSelected true.
            if (!(item.UserData is ToolbarSystemItem)) return;               // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě.
            this._TimeAxisToolBarAction((ToolbarSystemItem)item.UserData);
            */
        }
        /// <summary>
        /// Metoda se volá po akci Click na systémové položce ToolBaru. 
        /// Metoda zjistí, zda akce se týká časové osy, a pokud ano pak ji vyřeší.
        /// Pokud se akce nijak netýká časové osy, pak nic neprovádí (není tedy problém ji zavolat).
        /// </summary>
        /// <param name="item"></param>
        private void _TimeAxisToolBarClick(FunctionItem item)
        {
            if (!(item.UserData is ToolbarSystemItem)) return;               // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě.
            this._TimeAxisToolBarAction((ToolbarSystemItem)item.UserData);
        }
        /// <summary>
        /// Metoda provede požadovanou akci s časovou osou.
        /// Pokud se akce netýká časové osy, nic nedělá.
        /// </summary>
        /// <param name="action"></param>
        private void _TimeAxisToolBarAction(ToolbarSystemItem action)
        {
            action = (ToolbarSystemItem)(action & ToolbarSystemItem.TimeAxisAll);
            if (action == ToolbarSystemItem.None) return;

            // Pokud požadovaná akce je nějaký Zoom, pak nastavím tento Zoom jako "aktuální":
            ToolbarSystemItem zoom = (action & ToolbarSystemItem.TimeAxisZoomAll);
            if (zoom != ToolbarSystemItem.None)
                this._TimeAxisCurrentZoom = zoom;

            // Změna času:
            TimeRange currentTime = this._MainControl.SynchronizedTime.Value;
            TimeRange actionTime = _TimeAxisGetNewTime(currentTime, null, action, this._TimeAxisCurrentZoom);
            if (actionTime != null)
                this._MainControl.SynchronizedTime.Value = actionTime;
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a daný požadavek
        /// </summary>
        /// <param name="currentTime">TimeRange pro akce GoPrev a GoNext</param>
        /// <param name="currentDate">Pivot datum pro akce Zoom</param>
        /// <param name="action"></param>
        /// <param name="zoom">Aktuální zoom pro výpočet nového času v režimu posunu (akce TimeAxisGo*) </param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTime(TimeRange currentTime, DateTime? currentDate, ToolbarSystemItem action, ToolbarSystemItem zoom)
        {
            DateTime date = (currentDate.HasValue ? currentDate.Value : (currentTime != null ? currentTime.Center.Value : DateTime.Now));
            switch (action)
            {
                case ToolbarSystemItem.TimeAxisZoomOneDay: return _TimeAxisGetNewTimeZoomOneDay(date);
                case ToolbarSystemItem.TimeAxisZoomWorkWeek: return _TimeAxisGetNewTimeZoomWorkWeek(date);
                case ToolbarSystemItem.TimeAxisZoomWholeWeek: return _TimeAxisGetNewTimeZoomWholeWeek(date);
                case ToolbarSystemItem.TimeAxisZoomMonth: return _TimeAxisGetNewTimeZoomMonth(date);
                case ToolbarSystemItem.TimeAxisGoPrev: return _TimeAxisGetNewTimeGoPrev(currentTime, zoom);
                case ToolbarSystemItem.TimeAxisGoHome: return _TimeAxisGetNewTimeGoHome(currentTime, zoom);
                case ToolbarSystemItem.TimeAxisGoNext: return _TimeAxisGetNewTimeGoNext(currentTime, zoom);
            }
            return null;
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomOneDay"/>
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomOneDay(DateTime center)
        {
            DateTime begin = center.Date;
            DateTime end = begin.AddDays(1d);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomWorkWeek"/>
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomWorkWeek(DateTime center)
        {
            DateTime begin = center.Date.FirstDayOf(DateTimePart.Week);
            DateTime end = begin.AddDays(5d);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomWholeWeek"/>
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomWholeWeek(DateTime center)
        {
            DateTime begin = center.Date.FirstDayOf(DateTimePart.Week);
            DateTime end = begin.AddDays(7d);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomMonth"/>
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomMonth(DateTime center)
        {
            DateTime begin = center.Date.FirstDayOf(DateTimePart.Month);
            DateTime end = begin.AddMonths(1);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisGoPrev"/>
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="zoom">Aktuální zoom pro výpočet nového času v režimu posunu (akce TimeAxisGo*) </param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeGoPrev(TimeRange currentTime, ToolbarSystemItem zoom)
        {
            DateTime date = currentTime.Begin.Value;
            return _TimeAxisGetNewTime(null, date, zoom, ToolbarSystemItem.None);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisGoHome"/>
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="zoom">Aktuální zoom pro výpočet nového času v režimu posunu (akce TimeAxisGo*) </param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeGoHome(TimeRange currentTime, ToolbarSystemItem zoom)
        {
            DateTime date = DateTime.Now;
            return _TimeAxisGetNewTime(null, date, zoom, ToolbarSystemItem.None);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisGoNext"/>
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="zoom">Aktuální zoom pro výpočet nového času v režimu posunu (akce TimeAxisGo*) </param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeGoNext(TimeRange currentTime, ToolbarSystemItem zoom)
        {
            DateTime date = currentTime.End.Value;
            // Specialitky: pokud je zoom = WorkWeek, pak nemůžu jít GoNext tak, abych jako Date bral prostý čas End stávajícího intervalu.
            //  Proč? Protože pátek. Konec pracovního týdne je pátek (respektive currentTime.End je sobota brzy ráno),
            //          a z tohoto datumu se jako nový počátek odvodí zase to naše pondělí :-).
            //        A ani termín Begin.AddDays(7) není OK, protože Begin má typicky hodnotu Neděle pozdě večer (to vše kvůli _TimeAxisEnlargeRatio).
            if (zoom == ToolbarSystemItem.TimeAxisZoomWorkWeek)
                date = currentTime.Center.Value.AddDays(7);
            return _TimeAxisGetNewTime(null, date, zoom, ToolbarSystemItem.None);
        }
        /// <summary>
        /// Poměr zvětšení časového intervalu nad rámec matematicky přesného výpočtu
        /// </summary>
        private static decimal _TimeAxisEnlargeRatio { get { return 1.04m; } }
        /// <summary>
        /// Grupa v ToolBaru s položkami pro časovou osu
        /// </summary>
        private FunctionGlobalGroup _ToolbarTimeGroup;
        /// <summary>
        /// Aktuální Zoom aplikovaný na Main časovou osu.
        /// Obsahuje pouze hodnoty z rozsahu <see cref="ToolbarSystemItem.TimeAxisZoomAll"/>.
        /// </summary>
        private ToolbarSystemItem _TimeAxisCurrentZoom;
        private const string _Tlb_TimeAxis_Day = "TimeAxisOneDay";
        private const string _Tlb_TimeAxis_WorkWeek = "TimeAxisWorkWeek";
        private const string _Tlb_TimeAxis_WholeWeek = "TimeAxisWholeWeek";
        private const string _Tlb_TimeAxis_Month = "TimeAxisMonth";
        private const string _Tlb_TimeAxis_GoPrev = "TimeAxisGoPrev";
        private const string _Tlb_TimeAxis_GoHome = "TimeAxisGoHome";
        private const string _Tlb_TimeAxis_GoNext = "TimeAxisGoNext";
        #endregion
        #region Otevření formulářů záznamů
        /// <summary>
        /// Metoda vyvolá akci RunOpenRecordsForm do AppHost
        /// </summary>
        /// <param name="recordGId"></param>
        private void _CallHostRunOpenRecordsForm(GId recordGId)
        {
            if (recordGId == null || recordGId.RecordId == 0) return;
            GuiId[] guiIds = new GuiId[] { new GuiId(recordGId.ClassId, recordGId.RecordId) };
            this._CallHostRunOpenRecordsForm(guiIds);
        }
        /// <summary>
        /// Metoda vyvolá akci OpenRecords do AppHost
        /// </summary>
        /// <param name="guiIds"></param>
        private void _CallHostRunOpenRecordsForm(IEnumerable<GuiId> guiIds)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_OpenRecords;
            request.RecordsToOpen = guiIds.ToArray();
            this._CallAppHostFunction(request, null);
        }
        #endregion
        #region Sestavení instance GuiRequestCurrentState, obsahující stav celého Scheduleru
        /// <summary>
        /// Metoda vytvoří new instanci <see cref="GuiRequestCurrentState"/>, a naplní do ní údaje o aktuálním stavu celého okna.
        /// Data se předávají do servisní funkce v aplikační vrstvě.
        /// </summary>
        /// <returns></returns>
        private GuiRequestCurrentState _CreateGuiCurrentState()
        {
            GuiRequestCurrentState currentState = new GuiRequestCurrentState();
            currentState.TimeAxisValue = this._MainControl.SynchronizedTime.Value;

            List<GuiGridItemId> gridItemList = new List<GuiGridItemId>();
            foreach (IInteractiveItem item in this._MainControl.Selector.SelectedItems)
            {
                GuiGridItemId[] gridItems = this._GetGuiGridItems(item);
                if (gridItems != null)
                    gridItemList.AddRange(gridItems);
            }
            currentState.SelectedGraphItems = gridItemList.ToArray();

            return currentState;
        }
        /// <summary>
        /// Metoda pro daný prvek <see cref="IInteractiveItem"/> zjistí, zda se jedná o prvek grafu. Pokud ne, pak vrací null.
        /// Pokud ano, pak určí, zda jde o grupu nebo o jednotlivý prvek. Pro grupu najde její jednotlivé prvky.
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private GuiGridItemId[] _GetGuiGridItems(IInteractiveItem item)
        {
            GTimeGraphItem graphItem = item as GTimeGraphItem;
            if (graphItem == null) return null;
            List<ITimeGraphItem> dataItems = new List<ITimeGraphItem>();
            switch (graphItem.Position)
            {
                case GGraphControlPosition.Group:
                    dataItems.AddRange(graphItem.DataItems);
                    break;
                case GGraphControlPosition.Item:


            else if ()
                dataItems.AddRange(graphItem.DataItems);
        }
        #endregion
        #region Implementace IMainDataInternal
        /// <summary>
        /// Metoda zavolá hostitele a předá mu požadavek.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        void IMainDataInternal.CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction) { this._CallAppHostFunction(request, callBackAction); }
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
        /// <returns></returns>
        ToolStripDropDownMenu IMainDataInternal.CreateContextMenu(GuiGridItemId gridItemId) { return this.CreateContextMenu(gridItemId); }
        #endregion
    }
    /// <summary>
    /// Interface pro zpřístupnění vnitřních metod třídy <see cref="MainData"/>
    /// </summary>
    public interface IMainDataInternal
    {
        /// <summary>
        /// Metoda zavolá hostitele a předá mu požadavek.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        void CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction);
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
        /// <returns></returns>
        ToolStripDropDownMenu CreateContextMenu(GuiGridItemId gridItemId);
    }
    #endregion
}
