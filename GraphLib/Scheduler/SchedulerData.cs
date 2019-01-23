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
using Asol.Tools.WorkScheduler.Components.Grid;
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
            this.Init();
            using (App.Trace.Scope(TracePriority.Priority2_Lowest,  "SchedulerConfig", ".ctor(null)", ""))
                this._Config = new SchedulerConfig(null);
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
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        private SchedulerConfig _Config;
        /// <summary>
        /// Protože implementuji IFunctionProvider, stává se ze mě IPlugin.
        /// A jádro systému si vytváří slovník všech pluginů - takže si vygeneruje 
        /// pro každý typ pluginu jednu instanci, aby z ní mohl číst její pluginové vlastnosti.
        /// Instance generuje pomocí <see cref="System.Activator"/> a vyžaduje k tomu bezparametrický konstruktor.
        /// Zde jej implementujeme, ale nic v něm neděláme.
        /// </summary>
        public MainData() { }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Init()
        {
            this.ClosingState = MainFormClosingState.None;
        }
        #endregion
        #region Public metody a properties
        /// <summary>
        /// Načte data ze strukturovaného objektu <see cref="GuiData"/>
        /// </summary>
        /// <param name="guiData"></param>
        public void LoadData(GuiData guiData)
        {
            if (guiData == null)
                throw new GraphLibCodeException("Pro tvorbu Scheduler.MainData byl dodán objekt GuiData = null.");
            guiData.FillParents();
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
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainData", "ApplyPropertiesToForm", ""))
                this._ApplyPropertiesToForm(mainForm);     // Nastavíme vlastnosti formu podle GuiProperties

            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", ".ctor", ""))
                this._MainControl = new MainControl(this); // Vytvoříme new control MainControl

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
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainControl", ".ctor", ""))
                this._MainControl = new MainControl(this);

            this._FillMainControlFromGui();
            return this._MainControl;
        }
        /// <summary>
        /// Hlavní objekt s daty <see cref="GuiData"/>
        /// </summary>
        public GuiData GuiData { get { return this._GuiData; } }
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        public SchedulerConfig Config { get { return this._Config; } }
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
            App.TryRun(this._FillMainControlToolbar);
            App.TryRun(this._FillMainControlPagesFromGui);
        }
        /// <summary>
        /// Reference na hlavní GUI control, který je vytvořen v metodě <see cref="CreateControlToForm(Form)"/>
        /// </summary>
        protected MainControl _MainControl;
        #endregion
        #region Vyvolání akcí z pluginu do hostitele IAppHost
        /// <summary>
        /// Metoda zavolá hostitele <see cref="_AppHost"/>, jeho metodu <see cref="IAppHost.CallAppHostFunction(AppHostRequestArgs)"/>,
        /// předá jí aktuální <see cref="_SessionId"/> a požadavek, a zajistí zavolání metody (callBackAction) po doběhnutí funkce.
        /// </summary>
        /// <param name="request">Požadavek</param>
        /// <param name="callBackAction">Odkaz na metodu, která dostane řízení po asynchronním doběhnutí akce v hostiteli</param>
        /// <param name="blockGuiTime">Volba, zda blokovat aktuální thread do doby, než doběhne response - po stanovenou dobu. Hodnota null = nečekat.</param>
        /// <param name="blockGuiMessage">Zpráva zobrazená uživateli po dobu blokování GUI.</param>
        /// <param name="userData">Libovolná další data, která chce dostat metoda (callBackAction). Tato data se nijak nezpracovávají v hostiteli.</param>
        private void _CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction, TimeSpan? blockGuiTime = null, string blockGuiMessage = null, object userData = null)
        {
            bool hasCallBack = (callBackAction != null);
            bool hasBlockGui = blockGuiTime.HasValue;
            AppHostResponseArgs responseArgs = null;
            AppHostRequestArgs requestArgs = new AppHostRequestArgs(this._SessionId, request, hasBlockGui, userData, callBackAction);
            try
            {
                if (this._VerifyAppHost(request))
                {   // Máme-li hostitele, předáme mu požadavek:
                    if (hasBlockGui)
                    {   // Pokud přišel požadavek na blokování aktuálního threadu po danou dobu, pak požádám control o nastavení bloku na GUI:
                        this._MainControl.BlockGUI(blockGuiTime.Value, blockGuiMessage);
                        // A zajistím, že CallBack akce přijde k vyřízení do zdejší metody, namísto do metody aplikační:
                        requestArgs.CallBackAction = this._CallAppHostAsyncFunctionResponse;
                    }
                    // Zavoláme AppHost; předem nevíme, zda AppHost je Synchronní nebo Asynchronní:
                    responseArgs = this._AppHost.CallAppHostFunction(requestArgs);
                    // Pokud AppHost je Synchronní, tak nám rovnou vrátil Response, a vyřídíme to ihned:
                    if (responseArgs != null)
                    {
                        if (hasCallBack)
                            callBackAction(responseArgs);
                        if (hasBlockGui)
                            this._MainControl.ReleaseGUI();
                    }
                }
                else if (hasCallBack)
                {   // Nemáme hostitele. Pokud volající očekává vyvolání callBackAction, musíme mu ho dát:
                    this._CallBackActionErrorNoHost(requestArgs);
                }
            }
            catch (Exception exc)
            {
                App.Trace.Exception(exc, request.ToString());
                string message = "Při zpracování požadavku " + request.Command + " došlo k chybě: " + exc.Message;
                this.ShowDialog(message, null, GuiDialogButtons.Ok);
            }
        }
        /// <summary>
        /// Prověří správnost zadání requestu a existenci AppHost.
        /// Pokud je vše OK, vrací true.
        /// Pokud není, dá hlášku a vrací false.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool _VerifyAppHost(GuiRequest request)
        {
            if (this._HasHost) return true;
            string message = "Je vyžadováno provedení funkce IAppHost:\r\n" + request.ToString() + ",\r\nale není zadán datový hostitel.";
            System.Windows.Forms.MessageBox.Show(message);
            return false;
        }
        /// <summary>
        /// Metoda zavolá metodu <see cref="AppHostRequestArgs.CallBackAction"/> a předá jí chybovou hlášku o tom, že neexistuje IAppHost.
        /// </summary>
        /// <param name="requestArgs"></param>
        private void _CallBackActionErrorNoHost(AppHostRequestArgs requestArgs)
        {
            AppHostResponseArgs responseArgs = new AppHostResponseArgs(requestArgs);
            responseArgs.Result = AppHostActionResult.NotResponse;
            responseArgs.UserMessage = "Není zadán datový hostitel.";
            responseArgs.FullMessage = "[IAppHost does not exists] " + responseArgs.UserMessage;
            requestArgs.CallBackAction(responseArgs);
        }
        /// <summary>
        /// Metoda, která je volána z AppHost po asynchronním doběhnutí požadavku, v situaci kdy je blokováno GUI.
        /// Tato metoda vyvolá aplikační CallBack, a poté uvolní GUI.
        /// </summary>
        /// <param name="responseArgs"></param>
        private void _CallAppHostAsyncFunctionResponse(AppHostResponseArgs responseArgs)
        {
            // Nejprve odblokuji GUI:
            if (responseArgs.Request.BlockGui)
                this._MainControl.ReleaseGUI();
            // Pak zavolám zpracování odpovědi, v něm může dojít k novému zablokování GUI (proto GUI odblokovávám před tím a ne až potom):
            if (responseArgs.Request.OriginalCallBackAction != null)
                responseArgs.Request.OriginalCallBackAction(responseArgs);
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
        /// Grupa v ToolBaru, obsahující systémové položky pro časovou osu
        /// </summary>
        private FunctionGlobalGroup _ToolbarSystemTimeAxisGroup;
        /// <summary>
        /// Grupa v ToolBaru, obsahující systémové položky pro časovou osu
        /// </summary>
        private FunctionGlobalGroup _ToolbarSystemGuiEditGroup;
        /// <summary>
        /// Z dat v <see cref="_GuiToolbarPanel"/> naplní toolbar
        /// </summary>
        private void _FillMainControlToolbar()
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainData", "FillMainControlToolbar", ""))
            {
                this._ToolBarGuiItems = new List<ToolBarItem>();
                this._MainControl.ClearToolBar();
                this._MainControl.ToolBarVisible = this._GuiToolbarPanel.ToolbarVisible;
                this._FillMainControlToolbarFromSystem();
                this._FillMainControlToolbarFromGui();
                this._MainControl.ToolBarItemClicked += _ToolBarItemClicked;
                this._MainControl.ToolBarItemSelectedChange += _ToolBarItemSelectedChange;
            }
        }
        /// <summary>
        /// Do toolbaru vloží systémové funkce
        /// </summary>
        private void _FillMainControlToolbarFromSystem()
        {
            if (!this._GuiToolbarPanel.ToolbarVisible) return;
            // Systémové položky Toolbaru jsou položky třídy FunctionGlobalItem, nemají v sobě instanci GuiToolbarItem.

            this._ToolbarSystemTimeAxisGroup = new FunctionGlobalGroup() { Title = "ČASOVÁ OSA", ToolTipTitle = "Posuny časové osy, změna měřítka", Order = "A1" };
            this._ToolbarSystemGuiEditGroup = new FunctionGlobalGroup() { Title = "EDITACE", ToolTipTitle = "Editace prvků grafu", Order = "A2" };

            this._TimeAxisToolBarInit();
            this._ConfigToolBarInit();
            this._MoveItemToolBarInit();
            this._GuiEditToolBarInit();

            if (this._ToolbarSystemTimeAxisGroup.Items.Count > 0)
                this._MainControl.AddToolBarGroup(this._ToolbarSystemTimeAxisGroup);
            if (this._ToolbarSystemGuiEditGroup.Items.Count > 0)
                this._MainControl.AddToolBarGroup(this._ToolbarSystemGuiEditGroup);
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

                FunctionGlobalGroup group = toolBarGroups.GetAdd(groupName, key => new FunctionGlobalGroup() { Title = groupName });

                ToolBarItem item = ToolBarItem.Create(this, guiToolBarItem);
                if (item != null)
                {
                    group.Items.Add(item);
                    this._ToolBarGuiItems.Add(item);
                }
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
                this._ToolBarItemClickApplication(args.Item, guiToolbarItem);
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
        /// <param name="toolBarItem">Vizuální položka ToolBaru</param>
        /// <param name="guiToolbarItem">Datová (GUI) položka ToolBaru</param>
        private void _ToolBarItemClickApplication(FunctionItem toolBarItem, GuiToolbarItem guiToolbarItem)
        {
            bool callAppHost = this._ToolBarItemClickApplicationRunGuiActions(toolBarItem, guiToolbarItem);
            if (callAppHost)
                this._ToolBarItemClickApplicationCallAppHost(toolBarItem, guiToolbarItem);
        }
        /// <summary>
        /// Metoda detekuje GUI akce, které jsou předepsané v tlačítku Toolbaru <see cref="GuiToolbarItem.GuiActions"/>, a provede je.
        /// Vrací true, pokud má být volána servisní funkci v AppHost (tj. když toto volání NENÍ potlačeno příznakem <see cref="GuiActionType.SuppressCallAppHost"/>).
        /// </summary>
        /// <param name="toolBarItem"></param>
        /// <param name="guiToolbarItem"></param>
        /// <returns></returns>
        private bool _ToolBarItemClickApplicationRunGuiActions(FunctionItem toolBarItem, GuiToolbarItem guiToolbarItem)
        {
            if (guiToolbarItem == null ||!guiToolbarItem.GuiActions.HasValue) return true;    // Žádná akce = žádné potlačení, vracím true
            GuiActionType guiActions = guiToolbarItem.GuiActions.Value;
            this.RunGuiInteractions(toolBarItem, guiToolbarItem, guiActions);
            bool suppressCallAppHost = guiActions.HasFlag(GuiActionType.SuppressCallAppHost);
            return !suppressCallAppHost;
        }
        /// <summary>
        /// Metoda zavolá servisní funkci v AppHost, s commandem <see cref="GuiRequest.COMMAND_ToolbarClick"/>, a předá tam button, na nějž bylo kliknuto
        /// </summary>
        /// <param name="toolBarItem"></param>
        /// <param name="guiToolbarItem"></param>
        private void _ToolBarItemClickApplicationCallAppHost(FunctionItem toolBarItem, GuiToolbarItem guiToolbarItem)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_ToolbarClick;
            guiToolbarItem.IsChecked = toolBarItem.IsChecked;        // Přenesu příznak "Označen" (je to nutné?)
            request.ToolbarItem = guiToolbarItem;
            request.CurrentState = this._CreateGuiCurrentState();
            this._CallAppHostFunction(request, this._ToolBarItemClickApplicationResponse, guiToolbarItem.BlockGuiTime, guiToolbarItem.BlockGuiMessage);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="response"></param>
        private void _ToolBarItemClickApplicationResponse(AppHostResponseArgs response)
        {
            this._ProcessResponse(response.GuiResponse);
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
            this._ConfigToolBarClick(item);
            this._MoveItemToolBarClick(item);
            this._GuiEditToolBarClick(item);
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
        /// Metoda najde a aktualizuje položky Toolbaru z dat v dodaném soupisu.
        /// </summary>
        /// <param name="toolbarItems"></param>
        private void _ToolBarRefreshFromResponse(IEnumerable<GuiToolbarItem> toolbarItems)
        {
            if (toolbarItems == null) return;
            GToolBarRefreshMode refreshMode = GToolBarRefreshMode.None;
            foreach (GuiToolbarItem guiToolbarItem in toolbarItems)
            {
                ToolBarItem toolBarItem = _ToolBarGuiItems.FirstOrDefault(t => String.Equals(t.Name, guiToolbarItem.Name, StringComparison.InvariantCulture));
                if (toolBarItem == null) continue;
                GToolBarRefreshMode itemMode = toolBarItem.RefreshFrom(guiToolbarItem);
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)itemMode);
            }
            this._MainControl.RefreshToolBar(refreshMode);
        }
        /// <summary>
        /// Soupis aplikačních položek Toolbaru (tj. položky, vytvářené z dat <see cref="GuiToolbarItem"/> v metodě <see cref="_FillMainControlToolbarFromGui()"/>, 
        /// při prvotním načítání a tvorbě celého controlu.
        /// </summary>
        private List<ToolBarItem> _ToolBarGuiItems;
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
            #region RefreshFrom
            /// <summary>
            /// Metoda do this prvku opíše nové hodnoty z dodaného objektu.
            /// Neukládá do sebe nově dodaný objekt, jen jeho hodnoty.
            /// Řeší hodnoty pro tyto properties:
            /// <see cref="GuiTextItem.Title"/>, <see cref="GuiTextItem.ToolTip"/>, <see cref="GuiTextItem.Image"/>, <see cref="GuiToolbarItem.ImageHot"/>, 
            /// <see cref="GuiToolbarItem.Visible"/>, <see cref="GuiToolbarItem.Enable"/>, <see cref="GuiToolbarItem.Size"/>, 
            /// <see cref="GuiToolbarItem.ModuleWidth"/>, <see cref="GuiToolbarItem.LayoutHint"/>.
            /// Vrací rozsah změn = potřebné oblasti pro refresh Toolbaru.
            /// </summary>
            /// <param name="guiToolbarItem"></param>
            /// <returns></returns>
            public GToolBarRefreshMode RefreshFrom(GuiToolbarItem guiToolbarItem)
            {
                GToolBarRefreshMode refreshMode = GToolBarRefreshMode.None;
                GuiToolbarItem refrData = guiToolbarItem;
                if (refrData == null) return refreshMode;
                GuiToolbarItem currData = this._GuiToolBarItem;
                if (currData == null) return refreshMode;

                if (_Changed(currData.Title, refrData.Title, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Title = refrData.Title;
                if (_Changed(currData.ToolTip, refrData.ToolTip, GToolBarRefreshMode.None, ref refreshMode))
                    currData.ToolTip = refrData.ToolTip;
                if (_Changed(currData.Image, refrData.Image, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Image = refrData.Image;
                if (_Changed(currData.ImageHot, refrData.ImageHot, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.ImageHot = refrData.ImageHot;
                if (_Changed(currData.Visible, refrData.Visible, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Visible = refrData.Visible;
                if (_Changed(currData.Enable, refrData.Enable, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.Enable = refrData.Enable;
                if (_Changed(currData.IsCheckable, refrData.IsCheckable, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.IsCheckable = refrData.IsCheckable;
                if (_Changed(currData.IsChecked, refrData.IsChecked, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.IsChecked = refrData.IsChecked;
                if (_Changed(currData.CheckedGroupName, refrData.CheckedGroupName, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.CheckedGroupName = refrData.CheckedGroupName;
                if (_Changed(currData.ImageChecked, refrData.ImageChecked, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.ImageChecked = refrData.ImageChecked;
                if (_Changed(currData.Size, refrData.Size, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Size = refrData.Size;
                if (_Changed(currData.ModuleWidth, refrData.ModuleWidth, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.ModuleWidth = refrData.ModuleWidth;
                if (_Changed(currData.LayoutHint, refrData.LayoutHint, GToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.LayoutHint = refrData.LayoutHint;
                if (_Changed(currData.BlockGuiTime, refrData.BlockGuiTime, GToolBarRefreshMode.None, ref refreshMode))
                    currData.BlockGuiTime = refrData.BlockGuiTime;
                if (_Changed(currData.BlockGuiMessage, refrData.BlockGuiMessage, GToolBarRefreshMode.None, ref refreshMode))
                    currData.BlockGuiMessage = refrData.BlockGuiMessage;
                if (_Changed(currData.GuiActions, refrData.GuiActions, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.GuiActions = refrData.GuiActions;
                if (_Changed(currData.RunInteractionNames, refrData.RunInteractionNames, GToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.RunInteractionNames = refrData.RunInteractionNames;

                return refreshMode;
            }
            private static bool _Changed(string oldValue, string newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (newValue == null) return false;                  // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (String.Equals(oldValue, newValue, StringComparison.InvariantCulture)) return false;
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(bool? oldValue, bool? newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                if (oldValue == newValue) return false;
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(FunctionGlobalItemSize? oldValue, FunctionGlobalItemSize? newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                if (oldValue == newValue) return false;
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(TimeSpan? oldValue, TimeSpan? newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(LayoutHint? oldValue, LayoutHint? newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(GuiActionType? oldValue, GuiActionType? newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(int? oldValue, int? newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(GuiImage oldValue, GuiImage newValue, GToolBarRefreshMode result, ref GToolBarRefreshMode refreshMode)
            {
                if (newValue == null) return false;                  // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                bool on = (oldValue == null);
                bool nn = (newValue == null);
                if (on && nn)
                    // Oba jsou null => to není změna:
                    return false;
                if (on)
                {   // Pouze oldValue je null => to je změna (protože newValue není null):
                    refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                    return true;
                }
                // Oba nejsou null, podíváme se dovnitř:
                if (oldValue.ImageFile != null || newValue.ImageFile != null)
                {   // Některý Image je definován názvem souboru => porovnáme stringy:
                    return _Changed(oldValue.ImageFile, newValue.ImageFile, result, ref refreshMode);
                }
                // Dál do hloubky nejdeme, vrátíme true = jde o změnu:
                refreshMode = (GToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            #endregion
            #region Public property FunctionGlobalItem, načítané z GuiToolbarItem, a explicitně přidané
            /// <summary>
            /// GUI Prvek toolbaru, z něhož je tato položka vytvořena. Jde o data dodaná z aplikace.
            /// </summary>
            public GuiToolbarItem GuiToolbarItem { get { return this._GuiToolBarItem; } }
            /// <summary>
            /// Jméno tohoto prvku, prostor pro aplikační identifikátor položky
            /// </summary>
            public override string Name { get { return (this._HasItem ? this._GuiToolBarItem.Name : base.Name); } set { if (this._HasItem) this._GuiToolBarItem.Name = value; else base.Name = value; } }
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
            /// Položka je viditelná?
            /// </summary>
            public override bool IsVisible { get { return (this._HasItem ? (this._GuiToolBarItem.Visible.HasValue ? this._GuiToolBarItem.Visible.Value : true) : base.IsVisible); } set { if (this._HasItem) this._GuiToolBarItem.Visible = value; else base.IsVisible = value; } }
            /// <summary>
            /// Položka je Enabled?
            /// </summary>
            public override bool IsEnabled { get { return (this._HasItem ? (this._GuiToolBarItem.Enable.HasValue ? this._GuiToolBarItem.Enable.Value : true) : base.IsEnabled); } set { if (this._HasItem) this._GuiToolBarItem.Enable = value; else base.IsEnabled = value; } }
            /// <summary>
            /// Velikost prvku na toolbaru, vzhledem k výšce toolbaru
            /// </summary>
            public override FunctionGlobalItemSize Size { get { return (this._HasItem ? (this._GuiToolBarItem.Size.HasValue ? this._GuiToolBarItem.Size.Value : FunctionGlobalItemSize.Half) : base.Size); } set { if (this._HasItem) this._GuiToolBarItem.Size = value; else base.Size = value; } }
            /// <summary>
            /// Explicitně požadovaná šířka prvku v počtu modulů
            /// </summary>
            public override int? ModuleWidth { get { return (this._HasItem ? this._GuiToolBarItem.ModuleWidth : base.ModuleWidth); } set { if (this._HasItem) this._GuiToolBarItem.ModuleWidth = value; else base.ModuleWidth = value; } }
            /// <summary>
            /// Nápověda ke zpracování layoutu této položky
            /// </summary>
            public override LayoutHint LayoutHint { get { return (this._HasItem ? (this._GuiToolBarItem.LayoutHint.HasValue ? this._GuiToolBarItem.LayoutHint.Value : LayoutHint.Default) : base.LayoutHint); } set { if (this._HasItem) this._GuiToolBarItem.LayoutHint = value; else base.LayoutHint = value; } }
            /// <summary>
            /// Název grupy, kde se tento prvek objeví. Nezadaná grupa = implicitní s názvem "FUNKCE".
            /// </summary>
            public string GroupName { get { return (this._HasItem ? this._GuiToolBarItem.GroupName : ""); } set { if (this._HasItem) this._GuiToolBarItem.GroupName = value; } }
            /// <summary>
            /// Obsahuje true pokud this prvek může být <see cref="IsChecked"/> = být označen jako "aktivní"
            /// </summary>
            public override bool IsCheckable { get { return (this._HasItem ? (this._GuiToolBarItem.IsCheckable.HasValue ? this._GuiToolBarItem.IsCheckable.Value : false) : base.IsCheckable); } set { if (this._HasItem) this._GuiToolBarItem.IsCheckable = value; else base.IsCheckable = value; } }
            /// <summary>
            /// Obsahuje true, pokud tento prvek je aktivní (má u sebe zaškrtávátko)
            /// </summary>
            public override bool IsChecked { get { return (this._HasItem ? (this._GuiToolBarItem.IsChecked.HasValue ? this._GuiToolBarItem.IsChecked.Value : false) : base.IsChecked); } set { if (this._HasItem) this._GuiToolBarItem.IsChecked = value; else base.IsChecked = value; } }
            /// <summary>
            /// Obsahuje název skupiny prvků, které se vzájemně chovají jako OptionGroup.
            /// To znamená, že právě jeden z prvků skupiny může být <see cref="IsChecked"/> = být označen jako aktivní.
            /// <para/>
            /// Chování:
            /// <para/>
            /// a) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako CheckBox: změna jeho hodnoty <see cref="IsChecked"/> neovlivní žádný jiný prvek.
            /// Kliknutí na takový prvek mění hodnotu <see cref="IsChecked"/> z false na true a naopak = lze jej shodit na false.
            /// <para/>
            /// b) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako RadioButton: kliknutí na neoznačený button jej označí a současně odznačí ostatní buttony v grupě.
            /// Opakované kliknutí na označený button jej neodznačí.
            /// Prvky jedné grupy <see cref="CheckedGroupName"/> se musí nacházet v jedné grafické skupině <see cref="GroupName"/> (platí pro Toolbar).
            /// Pokud by byly umístěny v jiné grupě, nebudou považovány za jednu skupinu, ale více oddělených skupin.
            /// Naproti tomu jedna grafická grupa <see cref="GroupName"/> může obsahovat více skupin <see cref="CheckedGroupName"/>.
            /// <para/>
            /// Je rozumné dávat prvky jedné <see cref="CheckedGroupName"/> blízko k sobě, ale technicky nutné to není.
            /// </summary>
            public override string CheckedGroupName { get { return (this._HasItem ? this._GuiToolBarItem.CheckedGroupName : base.CheckedGroupName); } set { if (this._HasItem) this._GuiToolBarItem.CheckedGroupName = value; else base.CheckedGroupName = value; } }
            #endregion
        }
        #endregion
        #region Interakce (GuiActions) - provádění interakcí GUI, podle deklarace v toolbaru nebo kdekoliv jinde
        /// <summary>
        /// Metoda detekuje interakce požadované v dodaném prvku toolbaru, a provede je.
        /// </summary>
        /// <param name="toolBarItem">Vizuální prvek Toolbaru</param>
        /// <param name="guiToolbarItem">Datový prvek Toolbaru</param>
        /// <param name="guiActions">Akce</param>
        internal void RunGuiInteractions(FunctionItem toolBarItem, GuiToolbarItem guiToolbarItem, GuiActionType guiActions)
        {
            bool callRefresh = false;

            if (guiActions.HasFlag(GuiActionType.ResetAllRowFilters))
                this._RunGuiInteractionsResetAllRowFilters(ref callRefresh);

            if (guiActions.HasFlag(GuiActionType.ResetTargetInteractiveFilters))
                this._RunGuiInteractionsResetTargetInteractiveFilters(ref callRefresh);

            if (guiActions.HasFlag(GuiActionType.RunInteractionRowActivated))
                this._RunGuiInteractionsByName(guiToolbarItem.RunInteractionNames, ref callRefresh);

            if (callRefresh)
                this._MainControl.Refresh();
        }
        /// <summary>
        /// Metoda detekuje interakce podle filtru akcí, a provede je.
        /// </summary>
        /// <param name="sourceActions"></param>
        /// <param name="targetActions"></param>
        internal void RunGuiInteractions(SourceActionType? sourceActions, TargetActionType? targetActions)
        {
            bool callRefresh = false;

            this._RunGuiInteractionsByActions(sourceActions, targetActions, ref callRefresh);

            if (callRefresh)
                this._MainControl.Refresh();
        }
        /// <summary>
        /// Zruší řádkové filtry všech tabulek
        /// </summary>
        /// <param name="callRefresh">Nastavit na true v případě potřeby Refreshe</param>
        private void _RunGuiInteractionsResetAllRowFilters(ref bool callRefresh)
        {
            foreach (MainDataTable table in this.DataTables)
                table.ResetAllRowFilters(ref callRefresh);
        }
        /// <summary>
        /// Metoda provede akci "Zrušit interaktivní filtry pro target tabulky"
        /// </summary>
        /// <param name="callRefresh">Nastavit na true v případě potřeby Refreshe</param>
        private void _RunGuiInteractionsResetTargetInteractiveFilters(ref bool callRefresh)
        {
            foreach (MainDataTable table in this.DataTables)
                table.ResetInteractionFilters(ref callRefresh);
        }
        /// <summary>
        /// Provede znovu aktuálně platné interakce všech tabulek
        /// </summary>
        /// <param name="interactionNames">Názvy interakcí včetně názvu tabulky, sada párů ve formátu: "GridName1:InteractionNameA;GridName2:InteractionNameB;..."</param>
        /// <param name="callRefresh">Nastavit na true v případě potřeby Refreshe</param>
        private void _RunGuiInteractionsByName(string interactionNames, ref bool callRefresh)
        {
            var interactions = this._GetTableInteractions(interactionNames);
            if (interactions == null) return;

            foreach (var interaction in interactions)
                // MainDataTable . RunInteractionThisSource( GuiGridInteraction[] ):
                interaction.Item1.RunInteractionThisSource(interaction.Item2.ToArray(), ref callRefresh);
        }
        /// <summary>
        /// Provede znovu aktuálně platné interakce všech tabulek, vyhovující danému filtru akcí SourceActionType a TargetActionType
        /// </summary>
        /// <param name="sourceActions">Hodnoty SourceAction: když daná interakce bude mít alespoň jeden Flag, bude zařazena do výstupu</param>
        /// <param name="targetActions">Hodnoty TargetAction: když daná interakce bude mít alespoň jeden Flag, bude zařazena do výstupu</param>
        /// <param name="callRefresh">Nastavit na true v případě potřeby Refreshe</param>
        private void _RunGuiInteractionsByActions(SourceActionType? sourceActions, TargetActionType? targetActions, ref bool callRefresh)
        {
            var interactions = this._GetTableInteractions(sourceActions, targetActions);
            if (interactions == null) return;

            foreach (var interaction in interactions)
                // MainDataTable . RunInteractionThisSource( GuiGridInteraction[] ):
                interaction.Item1.RunInteractionThisSource(interaction.Item2.ToArray(), ref callRefresh);
        }
        /// <summary>
        /// Metoda vrací soupis tabulek a jejich interakcí, které se mají provést při kliknutí na tlačítko, které má zadané interakce v <see cref="GuiToolbarItem.RunInteractionNames"/>.
        /// Na vstupu je formát: "GridName1:InteractionNameA;GridName2:InteractionNameB;..."
        /// Výstupem této metody je pole obsahující páry: Tabulka + všechny její Interakce.
        /// </summary>
        /// <param name="interactionNames">Názvy interakcí včetně názvu tabulky, sada párů ve formátu: "GridName1:InteractionNameA;GridName2:InteractionNameB;..."</param>
        /// <returns></returns>
        protected Tuple<MainDataTable, List<GuiGridInteraction>>[] _GetTableInteractions(string interactionNames)
        {
            if (String.IsNullOrEmpty(interactionNames)) return null;

            Dictionary<string, Tuple<MainDataTable, List<GuiGridInteraction>>> result = new Dictionary<string, Tuple<MainDataTable, List<GuiGridInteraction>>>();

            var tableDict = this._DataTables.GetDictionary(t => t.TableName, true);

            string[] pairs = interactionNames.Split(';', ',');  // Jména interakcí rozdělit na páry: "GridName1:InteractionNameA" ; "GridName2:InteractionNameB";...
            foreach (string pair in pairs)
            {   // Jeden každý pár, například "GridName1:InteractionNameA":
                if (String.IsNullOrEmpty(pair) || !(pair.Contains(":") || pair.Contains("."))) continue;
                string[] items = pair.Split(':', '.');
                if (items.Length < 2) continue;
                string tableName = items[0].Trim();
                string interName = items[1].Trim();
                if (String.IsNullOrEmpty(tableName) || String.IsNullOrEmpty(interName)) continue;

                MainDataTable dataTable;
                if (!tableDict.TryGetValue(tableName, out dataTable)) continue;

                GuiGridInteraction interaction = dataTable.Interactions.FirstOrDefault(i => i.Name == interName);
                if (interaction == null ) continue;

                Tuple<MainDataTable, List<GuiGridInteraction>> tableList;
                if (!result.TryGetValue(tableName, out tableList))
                {
                    tableList = new Tuple<MainDataTable, List<GuiGridInteraction>>(dataTable, new List<GuiGridInteraction>());
                    result.Add(tableName, tableList);
                }
                tableList.Item2.Add(interaction);
            }

            return result.Values.ToArray();
        }
        /// <summary>
        /// Metoda vrací soupis tabulek a jejich interakcí, které vyhovují danému zadání.
        /// Výstupem této metody je pole obsahující páry: Tabulka + všechny její Interakce.
        /// </summary>
        /// <returns></returns>
        protected Tuple<MainDataTable, List<GuiGridInteraction>>[] _GetTableInteractions(SourceActionType? sourceActions, TargetActionType? targetActions)
        {
            List<Tuple<MainDataTable, List<GuiGridInteraction>>> tableInterList = new List<Tuple<MainDataTable, List<GuiGridInteraction>>>();

            foreach (MainDataTable dataTable in this._DataTables)
            {
                List<GuiGridInteraction> interactions = _GetFilteredInteractions(dataTable.Interactions, sourceActions, targetActions);
                if (interactions != null && interactions.Count > 0)
                    tableInterList.Add(new Tuple<MainDataTable, List<GuiGridInteraction>>(dataTable, interactions));
            }

            return tableInterList.ToArray();
        }
        /// <summary>
        /// Metoda filtruje danou sadu interakcí.
        /// </summary>
        /// <param name="interactions">Sada interakcí</param>
        /// <param name="sourceActions">Hodnoty SourceAction: když daná interakce bude mít alespoň jeden Flag, bude zařazena do výstupu</param>
        /// <param name="targetActions">Hodnoty TargetAction: když daná interakce bude mít alespoň jeden Flag, bude zařazena do výstupu</param>
        /// <returns></returns>
        protected List<GuiGridInteraction> _GetFilteredInteractions(IEnumerable<GuiGridInteraction> interactions, SourceActionType? sourceActions, TargetActionType? targetActions)
        {
            if (interactions == null) return null;
            bool filterSource = (sourceActions.HasValue);
            bool filterTarget = (targetActions.HasValue);
            return interactions
                .Where(i =>
                        {   // return (filterSource AND filterTarget), přičemž zadání hodnoty NULL se chápe jako True:
                            if (filterSource && ((i.SourceAction & sourceActions.Value) == 0)) return false;
                            if (filterTarget && ((i.TargetAction & targetActions.Value) == 0)) return false;
                            return true;
                        }
                        )
                .ToList();
        }
        #endregion
        #region Časová osa - tvorba menu v ToolBaru, a obsluha akcí tohoto menu; reakce na změny synchronního času
        /// <summary>
        /// Aktuálně zobrazený čas na synchronní časové ose. Lze vložit hodnotu, pokud není null.
        /// Vložení hodnoty do této property vyvolá Refresh controlu!
        /// </summary>
        protected TimeRange SynchronizedTime
        {
            get { return this._MainControl.SynchronizedTime.Value; }
            set { if (value != null) { this._MainControl.SynchronizedTime.Value = value; this._MainControl.Refresh(); } }
        }
        /// <summary>
        /// Inicializace položek ToolBaru (grupa <see cref="_ToolbarSystemTimeAxisGroup"/>) pro časovou osu
        /// </summary>
        private void _TimeAxisToolBarInit()
        {
            ToolbarSystemItem items = (ToolbarSystemItem)(this._GuiToolbarPanel.ToolbarShowSystemItems & ToolbarSystemItem.TimeAxisAll);
            if (items == ToolbarSystemItem.None) return;

            // Oddělovač, pokud v grupě už jsou položky:
            this._ToolbarSystemTimeAxisGroup.AddSeparator(this);

            // Zoom:
            ToolbarSystemItem timeAxisZoom = this.Config.TimeAxisZoom;
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomOneDay))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_Day, R.Images.Asol.Calendarday32x32Png, null, "Jeden den", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomOneDay), selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomOneDay));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWorkWeek))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WorkWeek, R.Images.Asol.Calendarworkingweek32x32Png, null, "Pracovní týden Po-Pá", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomWorkWeek), selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomWorkWeek));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWorkWeek))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WholeWeek, R.Images.Asol.Calendarweek32x32Png, null, "Celý týden Po-Ne", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomWholeWeek), selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomWholeWeek));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomMonth))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_Month, R.Images.Asol.Calendarmonth32x32Png, null, "Měsíc 30 dní", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemSkipToNextRow, isSelectable: true, isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomMonth), selectionGroupName: "TimeZoom", userData: ToolbarSystemItem.TimeAxisZoomMonth));

            // Go:
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoPrev))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoPrev, R.Images.Asol.Calendarprevious32x32Png, null, "Zpět = doleva = do minulosti", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.ThisItemSkipToNextRow | LayoutHint.NextItemOnSameRow, moduleWidth: 1, userData: ToolbarSystemItem.TimeAxisGoPrev));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoHome))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoHome, R.Images.Asol.Calendartoday32x32Png, null, "Aktuální čas", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, moduleWidth: 2, userData: ToolbarSystemItem.TimeAxisGoHome));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoNext))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoNext, R.Images.Asol.Calendarnext32x32Png, null, "Vpřed = doprava = do budoucnosti", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemSkipToNextTable, moduleWidth: 1, userData: ToolbarSystemItem.TimeAxisGoNext));
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
            this._TimeAxisToolBarAction(item, (ToolbarSystemItem)item.UserData);
        }
        /// <summary>
        /// Metoda provede požadovanou akci s časovou osou.
        /// Pokud se akce netýká časové osy, nic nedělá.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="action"></param>
        private void _TimeAxisToolBarAction(FunctionItem item, ToolbarSystemItem action)
        {
            action = (ToolbarSystemItem)(action & ToolbarSystemItem.TimeAxisAll);
            if (action == ToolbarSystemItem.None) return;

            // Pokud požadovaná akce je nějaký Zoom, pak nastavím tento Zoom jako "aktuální":
            ToolbarSystemItem zoom = (action & ToolbarSystemItem.TimeAxisZoomAll);
            if (zoom != ToolbarSystemItem.None)
                this.Config.TimeAxisZoom = zoom;

            // Změna času:
            TimeRange currentTime = this._MainControl.SynchronizedTime.Value;
            TimeRange actionTime = _TimeAxisGetNewTime(currentTime, null, action, this.Config.TimeAxisZoom);     // Action a TimeAxisZoom se mohou lišit !!!  action může být TimeAxisGoHome, a přitom TimeAxisZoom si udržuje aktuální hodnotu, např. TimeAxisZoomOneDay
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
            DateTime date = _TimeAxisGetCurrentPoint(currentDate, currentTime, true);
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
        /// Vrátí optimální Begin pro novou časovou osu
        /// </summary>
        /// <param name="currentDate"></param>
        /// <param name="currentTime"></param>
        /// <param name="shrinkByRatio"></param>
        /// <returns></returns>
        private static DateTime _TimeAxisGetCurrentPoint(DateTime? currentDate, TimeRange currentTime, bool shrinkByRatio)
        {
            DateTime value = DateTime.Now;
            if (currentDate.HasValue)
            {
                value = currentDate.Value;
            }
            else if (currentTime != null && currentTime.HasBegin)
            {
                if (shrinkByRatio && _TimeAxisEnlargeRatio > 0m)
                {
                    TimeRange shrinkTime = currentTime.ZoomToRatio(currentTime.Center.Value, (1m / _TimeAxisEnlargeRatio));
                    value = shrinkTime.Begin.Value;
                }
                else
                {
                    value = currentTime.Begin.Value;
                }
                if (value.TimeOfDay.Ticks > 0)
                    value = value.Date.AddDays(1d);
            }
            return value;
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomOneDay"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomOneDay(DateTime value)
        {
            DateTime begin = value.Date;
            DateTime end = begin.AddDays(1d);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomWorkWeek"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomWorkWeek(DateTime value)
        {
            DateTime begin = value.Date.FirstDayOf(DateTimePart.Week);
            DateTime end = begin.AddDays(5d);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomWholeWeek"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomWholeWeek(DateTime value)
        {
            DateTime begin = value.Date.FirstDayOf(DateTimePart.Week);
            DateTime end = begin.AddDays(7d);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomMonth"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomMonth(DateTime value)
        {
            DateTime begin = value.Date.FirstDayOf(DateTimePart.Month);
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
        private const string _Tlb_TimeAxis_Day = "TimeAxisOneDay";
        private const string _Tlb_TimeAxis_WorkWeek = "TimeAxisWorkWeek";
        private const string _Tlb_TimeAxis_WholeWeek = "TimeAxisWholeWeek";
        private const string _Tlb_TimeAxis_Month = "TimeAxisMonth";
        private const string _Tlb_TimeAxis_GoPrev = "TimeAxisGoPrev";
        private const string _Tlb_TimeAxis_GoHome = "TimeAxisGoHome";
        private const string _Tlb_TimeAxis_GoNext = "TimeAxisGoNext";

        /// <summary>
        /// Eventhandler volaný po změně synchronního času
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SynchronizedTimeValueChanging(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            SourceActionType sourceActions = SourceActionType.TableRowActivatedOnly | SourceActionType.TableRowActivatedWithRowsChecked | SourceActionType.TableRowChecked;
            TargetActionType targetActions = TargetActionType.SearchSourceVisibleTime;    // Akce, které jsou podmíněné zobrazeným časovým intervalem
            this.RunGuiInteractions(sourceActions, targetActions);
        }
        #endregion
        #region Okno konfigurace - tvorba položky v Toolbaru a celá obsluha
        /// <summary>
        /// Inicializace položek ToolBaru (grupa <see cref="_ToolbarSystemGuiEditGroup"/>) pro hlavní okno konfigurace
        /// </summary>
        private void _ConfigToolBarInit()
        {
            // Pokud v definici (ToolbarShowSystemItems) je uvedena hodnota (MoveItemAll | GuiEditAll), 
            //  pak NEBUDOU zobrazovány jednotlivé buttony, ale místo nich se zobrazí velký button CONFIG:
            ToolbarSystemItem currentItems = this._GuiToolbarPanel.ToolbarShowSystemItems;
            if (!_SystemItemsIsConfig(currentItems)) return;

            // Oddělovač, pokud v grupě už jsou položky:
            this._ToolbarSystemGuiEditGroup.AddSeparator(this);
            
            this._ToolbarSystemGuiEditGroup.Items.Add(_CreateToolbarItem(_Tlb_SystemSchedulerConfig, R.Images.Actions.RunBuildConfigurePng, 
                "Nastavení", "Otevře okno kompletního nastavení aplikace", size: FunctionGlobalItemSize.Whole, userData: ToolbarSystemItem.MoveItemAll | ToolbarSystemItem.GuiEditAll));

            this._ToolbarSystemGuiEditGroup.AddSeparator(this, true);
        }
        /// <summary>
        /// Metoda se volá po akci Click na systémové položce ToolBaru. 
        /// Metoda zjistí, zda akce se týká konfigurace, a pokud ano pak ji vyřeší.
        /// Pokud se akce nijak netýká konfigurace, pak nic neprovádí (není tedy problém ji zavolat).
        /// </summary>
        /// <param name="item"></param>
        private void _ConfigToolBarClick(FunctionItem item)
        {
            if (!(item.UserData is ToolbarSystemItem)) return;               // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě.
            this._ConfigToolBarAction(item, (ToolbarSystemItem)item.UserData);
        }
        /// <summary>
        /// Po kliknutí na systémovou ikonu Toolbaru, řeší akce typu Config
        /// </summary>
        /// <param name="item"></param>
        /// <param name="action"></param>
        private void _ConfigToolBarAction(FunctionItem item, ToolbarSystemItem action)
        {
            // Jako "Povolovací hodnota" akce Config je brána hodnota (MoveItemAll | GuiEditAll),
            //  pokud tam není, pak se nejedná o Config:
            if (!_SystemItemsIsConfig(action)) return;

            this._MainControl.RunInGUI(() => SchedulerConfigForm.ShowDialog(this._MainControl.MainForm, this.Config));
        }
        private const string _Tlb_SystemSchedulerConfig = "SystemSchedulerConfig";
        /// <summary>
        /// Metoda vrátí true, pokud daná hodnota <see cref="ToolbarSystemItem"/> 
        /// obsahuje všechny hodnoty <see cref="ToolbarSystemItem.MoveItemAll"/> + <see cref="ToolbarSystemItem.GuiEditAll"/>,
        /// kterážto hodnota má význam "Okno konfigurace".
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private static bool _SystemItemsIsConfig(ToolbarSystemItem items)
        {
            ToolbarSystemItem configItem = (ToolbarSystemItem.MoveItemAll | ToolbarSystemItem.GuiEditAll);
            return (((ToolbarSystemItem)(items & configItem)) == configItem);
        }
        #endregion
        #region Přichytávání prvku grafu při jeho posouvání procesem Drag and Drop - ToolBar (definice i obsluha Clicku), i vlastní provádění příchytu prvků
        /// <summary>
        /// Inicializace položek ToolBaru (grupa <see cref="_ToolbarSystemGuiEditGroup"/>) pro řízení pohybu prvků
        /// </summary>
        private void _MoveItemToolBarInit()
        {
            // Pokud v definici (ToolbarShowSystemItems) je uvedena hodnota (MoveItemAll | GuiEditAll), 
            //  pak NEBUDOU zobrazovány jednotlivé buttony, ale místo nich se zobrazí velký button CONFIG:
            ToolbarSystemItem currentItems = this._GuiToolbarPanel.ToolbarShowSystemItems;
            if (_SystemItemsIsConfig(currentItems)) return;

            ToolbarSystemItem items = (ToolbarSystemItem)(currentItems & ToolbarSystemItem.MoveItemAll);
            if (App.IsDebugMode)
                items = ToolbarSystemItem.MoveItemAll;

            if (items == ToolbarSystemItem.None) return;

            // Oddělovač, pokud v grupě už jsou položky:
            this._ToolbarSystemGuiEditGroup.AddSeparator(this);

            if (items.HasFlag(ToolbarSystemItem.MoveItemSnapToNearItems))
                this._ToolbarSystemGuiEditGroup.Items.Add(_CreateToolbarItem(_Tlb_MoveItem_SnapNearTime, R.Images.Actions24.AlignHorizontalRightOutPng, null, "Při přesouvání prvku jej přichytávat k sousedním existujícím prvkům v řádku", size: FunctionGlobalItemSize.Small, layoutHint: LayoutHint.NextItemSkipToNextRow, moduleWidth: 1, isSelectable: true, isSelected: this.Config.MoveSnapMouse.OriginalTimeNearActive, userData: ToolbarSystemItem.MoveItemSnapToNearItems));
            if (items.HasFlag(ToolbarSystemItem.MoveItemSnapToOriginalTime))
                this._ToolbarSystemGuiEditGroup.Items.Add(_CreateToolbarItem(_Tlb_MoveItem_SnapOriginalTime, R.Images.Actions24.AlignHorizontalLeftPng, null, "Při přesouvání prvku jej přichytávat k původnímu času", size: FunctionGlobalItemSize.Small, layoutHint: LayoutHint.NextItemSkipToNextRow, moduleWidth: 1, isSelectable: true, isSelected: this.Config.MoveSnapMouse.OriginalTimeNearActive, userData: ToolbarSystemItem.MoveItemSnapToOriginalTime));
            if (items.HasFlag(ToolbarSystemItem.MoveItemSnapToRoundTimeGrid))
                this._ToolbarSystemGuiEditGroup.Items.Add(_CreateToolbarItem(_Tlb_MoveItem_SnapToRoundTimeGrid, R.Images.Actions24.CodeVariablePng, null, "Při přesouvání prvku jej přichytávat k zaokrouhleným časovým jednotkám", size: FunctionGlobalItemSize.Small, moduleWidth: 1, isSelectable: true, isSelected: this.Config.MoveSnapMouse.GridTickActive, userData: ToolbarSystemItem.MoveItemSnapToRoundTimeGrid));
        }
        /// <summary>
        /// Metoda se volá po akci Click na systémové položce ToolBaru. 
        /// Metoda zjistí, zda akce se týká přesunu prvků, a pokud ano pak ji vyřeší.
        /// Pokud se akce nijak netýká přesunu prvků, pak nic neprovádí (není tedy problém ji zavolat).
        /// </summary>
        /// <param name="item"></param>
        private void _MoveItemToolBarClick(FunctionItem item)
        {
            if (!(item.UserData is ToolbarSystemItem)) return;               // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě.
            this._MoveItemToolBarAction(item, (ToolbarSystemItem)item.UserData);
        }
        /// <summary>
        /// Po kliknutí na systémovou ikonu Toolbaru, řeší akce typu ItemMove
        /// </summary>
        /// <param name="item"></param>
        /// <param name="action"></param>
        private void _MoveItemToolBarAction(FunctionItem item, ToolbarSystemItem action)
        {
            // Jako "Povolovací hodnota" akce Config je brána hodnota (MoveItemAll | GuiEditAll),
            //  pokud tam je, pak se jedná o Config a ne o jednotlivou akci:
            if (_SystemItemsIsConfig(action)) return;

            action = (ToolbarSystemItem)(action & ToolbarSystemItem.MoveItemAll);
            if (action == ToolbarSystemItem.None) return;

            switch (action)
            {
                case ToolbarSystemItem.MoveItemSnapToNearItems:
                    this.Config.MoveSnapMouse.OriginalTimeNearActive = item.IsChecked;
                    this.Config.Save();
                    break;
                case ToolbarSystemItem.MoveItemSnapToOriginalTime:
                    this.Config.MoveSnapMouse.OriginalTimeNearActive = item.IsChecked;
                    break;
                case ToolbarSystemItem.MoveItemSnapToRoundTimeGrid:
                    this.Config.MoveSnapMouse.GridTickActive = item.IsChecked;
                    break;
            }
        }
        private const string _Tlb_MoveItem_SnapNearTime = "MoveItemSnapNearTime";
        private const string _Tlb_MoveItem_SnapOriginalTime = "MoveItemSnapOriginalTime";
        private const string _Tlb_MoveItem_SnapToRoundTimeGrid = "MoveItemSnapToRoundTimeGrid";
        /// <summary>
        /// Metoda má za úkol modifikovat data při procesu Drag and Drop pro grafický prvek.
        /// Jde o to, že při přetahování prvků můžeme chtít, aby se prvek "přichytával" 
        /// buď k původnímu času, nebo k okolním bližším prvkům, nebo k zaokrouhlenému času na ose.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="snapInfo">Informace o nastavení MoveSnap pro aktuální modifikační klávesy Ctrl/Shift</param>
        private void _AdjustGraphItemDragMove(GraphItemChangeInfo moveInfo, SchedulerConfig.MoveSnapInfo snapInfo)
        {
            RangeSide nearSide = RangeSide.None;
            DateTime? nearTime = null;

            // 1. Přichytávání přemísťovaného prvku k vhodné straně nejbližšího přiléhajícímu prvku v cílovém grafu:
            if (!nearTime.HasValue)
                nearTime = this._AdjustSearchNearItemTime(moveInfo, snapInfo, ref nearSide);

            // 2. Přichytávání k výchozímu času:
            if (!nearTime.HasValue)
                nearTime = this._AdjustSearchOriginalTime(moveInfo, snapInfo, ref nearSide);

            // 3. Přichytávání k zaokrouhlenému času (hrubší mřížka než 1 pixel):
            if (!nearTime.HasValue)
                nearTime = this._AdjustSearchRoundTime(moveInfo, snapInfo, ref nearSide);

            // 4. Nalezené hodnoty promítneme do prvku:
            this._AdjustSetResults(moveInfo, nearTime, nearSide);
        }
        /// <summary>
        /// Metoda se pokusí najít nejbližší sousední prvek v cílovém grafu, k jehož okraji bychom mohli zarovnat náš přesouvaný prvek.
        /// Určí i stranu aktuálního prvku, která se má přichytit.
        /// </summary>
        /// <param name="moveInfo">Informace o přesouvaném prvku</param>
        /// <param name="snapInfo">Informace o nastavení MoveSnap pro aktuální modifikační klávesy Ctrl/Shift</param>
        /// <param name="nearSide">ref hodnota, na které straně je prvek k němuž se přichytáváme</param>
        /// <returns></returns>
        private DateTime? _AdjustSearchNearItemTime(GraphItemChangeInfo moveInfo, SchedulerConfig.MoveSnapInfo snapInfo, ref RangeSide nearSide)
        {
            RangeSide moveSide = ((snapInfo.SequenceActive || snapInfo.InnerItemActive) ? this._AdjustDetectNearSide(moveInfo) : RangeSide.None);
            DateTime? nearTime = null;

            if (snapInfo.SequenceActive)
            {   // Hledat navazující operaci : hledáme End == Begin:
                int distance = snapInfo.SequenceDistance;

                // Podle pozice myši vzhledem k prvku určíme stranu prvku (Begin/End), která se má přichytávat:
                nearSide = moveSide;
                nearTime = this._AdjustSearchNearItemTimeSide(moveInfo, SearchNearItemMode.ContinuingSameLevel, distance, nearSide);
                if (nearTime.HasValue) return nearTime;

                // Pokud jsme nenašli prvek na bližší straně přesouvaného prvku, můžeme jej hledat i na protější straně?
                if (moveInfo.MoveMode == GraphItemMoveMode.Move)
                {
                    nearSide = this._AdjustGetOppositeSide(nearSide);
                    nearTime = this._AdjustSearchNearItemTimeSide(moveInfo, SearchNearItemMode.ContinuingSameLevel, distance, nearSide);
                    if (nearTime.HasValue) return nearTime;
                }
            }

            if (snapInfo.InnerItemActive)
            {   // Hledat podkladový prvek (=směna) : hledáme Begin == Begin:
                int distance = snapInfo.InnerItemDistance;

                // Podle pozice myši vzhledem k prvku určíme stranu prvku (Begin/End), která se má přichytávat:
                nearSide = moveSide;
                nearTime = this._AdjustSearchNearItemTimeSide(moveInfo, SearchNearItemMode.SupportingLowLevel, distance, nearSide);
                if (nearTime.HasValue) return nearTime;

                // Pokud jsme nenašli prvek na bližší straně přesouvaného prvku, můžeme jej hledat i na protější straně?
                if (moveInfo.MoveMode == GraphItemMoveMode.Move)
                {
                    nearSide = this._AdjustGetOppositeSide(nearSide);
                    nearTime = this._AdjustSearchNearItemTimeSide(moveInfo, SearchNearItemMode.SupportingLowLevel, distance, nearSide);
                    if (nearTime.HasValue) return nearTime;
                }
            }

            // Nenašli?
            nearSide = RangeSide.None;
            return null;
        }
        /// <summary>
        /// Metoda se pokusí najít nejbližší sousední prvek v cílovém grafu, na dané straně, k jehož okraji bychom mohli zarovnat náš přesouvaný prvek
        /// </summary>
        /// <param name="moveInfo">Informace o přesouvaném prvku</param>
        /// <param name="searchLevel"></param>
        /// <param name="distance"></param>
        /// <param name="nearSide">ref hodnota, na které straně je prvek k němuž se přichytáváme</param>
        /// <returns></returns>
        private DateTime? _AdjustSearchNearItemTimeSide(GraphItemChangeInfo moveInfo, SearchNearItemMode searchLevel, int distance, RangeSide nearSide)
        {
            GTimeGraph targetGraph = moveInfo.TargetGraph;
            if (targetGraph == null) return null;

            DateTime timePoint = (nearSide == RangeSide.Begin ? moveInfo.TimeRangeFinal.Begin.Value : moveInfo.TimeRangeFinal.End.Value);
            int xc = (nearSide == RangeSide.Begin ? moveInfo.BoundsFinalAbsolute.X : moveInfo.BoundsFinalAbsolute.Right - 1);
            DateTime? begin = moveInfo.GetTimeForPosition(xc - distance);
            DateTime? end = moveInfo.GetTimeForPosition(xc + distance);
            TimeRange timeWindow = new TimeRange(begin, end);

            GTimeGraphGroup nearGroup;
            DateTime? nearTime = null;

            switch (searchLevel)
            {
                case SearchNearItemMode.ContinuingSameLevel:
                    nearTime = targetGraph.SearchNearTime(group => _AdjustSearchTimeSelector(group, moveInfo, nearSide, SearchNearItemMode.ContinuingSameLevel), timePoint, out nearGroup, timeWindow);
                    break;
                case SearchNearItemMode.SupportingLowLevel:
                    nearTime = targetGraph.SearchNearTime(group => _AdjustSearchTimeSelector(group, moveInfo, nearSide, SearchNearItemMode.SupportingLowLevel), timePoint, out nearGroup, timeWindow);
                    break;
            }

            return nearTime;
        }
        /// <summary>
        /// Selector grupy a jejího času
        /// </summary>
        /// <param name="group">Prohlížená grupa (kterýkoli prvek grafu, do něhož chceme náš prvek moveInfo umístit)</param>
        /// <param name="moveInfo">Přesouvaný prvek a veškerá jeho data</param>
        /// <param name="nearSide">Strana našeho přesouvaného prvku</param>
        /// <param name="searchMode">Režim hledání</param>
        /// <returns></returns>
        private DateTime? _AdjustSearchTimeSelector(GTimeGraphGroup group, GraphItemChangeInfo moveInfo, RangeSide nearSide, SearchNearItemMode searchMode)
        {
            int groupId = moveInfo.DragGroupId;
            int itemId = moveInfo.DragItemId;
            int layer = moveInfo.DragLayer;

            // Pokud daná grupa (group) má shodné číslo grupy (nebo prvku) jako ta, která se přesouvá (moveInfo), pak grupu nebereme = vracíme null:
            if (groupId != 0 && group.GroupId == groupId) return null;
            if (groupId == 0 && group.Items[0].ItemId == itemId) return null;

            // Pokračujeme podle režimu:
            switch (searchMode)
            {
                case SearchNearItemMode.ContinuingSameLevel:
                    // Máme najít prvek pouze ve stejné vrstvě; jiné vrstvy nebrat:
                    if (group.Layer != layer) return null;
                    // Máme vybrat datum NAVAZUJÍCÍ na datum přesouvaného prvku; tedy pokud nearSide == RangeSide.Begin, pak hledáme End, a naopak:
                    return (nearSide == RangeSide.Begin ? group.Time.End : group.Time.Begin);

                case SearchNearItemMode.SupportingLowLevel:
                    // Máme najít pouze prvek v nižší vrstvě; jiné vrstvy nebrat:
                    if (group.Layer >= layer) return null;
                    // Máme vybrat datum NAVAZUJÍCÍ na datum přesouvaného prvku; tedy pokud nearSide == RangeSide.Begin, pak hledáme End, a naopak:
                    return (nearSide == RangeSide.Begin ? group.Time.Begin : group.Time.End);
            }
            return null;
        }
        /// <summary>
        /// Režim hledání vhodné grupy a jejího času v rámci metody <see cref="_AdjustSearchTimeSelector(GTimeGraphGroup, GraphItemChangeInfo, RangeSide, SearchNearItemMode)"/>
        /// </summary>
        private enum SearchNearItemMode
        {
            /// <summary>
            /// Neurčeno
            /// </summary>
            None = 0,
            /// <summary>
            /// Navazující prvek ve stejné vrstvě <see cref="ITimeGraphItem.Layer"/>, a s navazujícím časem: tak aby End prvku navazoval na Begin dalšího prvku
            /// </summary>
            ContinuingSameLevel,
            /// <summary>
            /// Podporující prvek v nižší vrstvě <see cref="ITimeGraphItem.Layer"/>, s časem odpovídajícím: tak aby Begin prvku souhlasil s Beginem dalšího prvku
            /// </summary>
            SupportingLowLevel
        }
        /// <summary>
        /// Vrátí stranu prvku, která je blíže k místu prvku, za který byl prvek uchopen myší.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        private RangeSide _AdjustDetectNearSide(GraphItemChangeInfo moveInfo)
        {
            if (moveInfo.MoveMode == GraphItemMoveMode.ResizeBegin) return RangeSide.Begin;
            if (moveInfo.MoveMode == GraphItemMoveMode.ResizeEnd) return RangeSide.End;
            if (!moveInfo.SourceMousePoint.HasValue) return RangeSide.Begin;
            Int32Range boundsX = Int32Range.CreateFromRectangle(moveInfo.SourceBounds, Orientation.Horizontal);
            int mouseX = moveInfo.SourceMousePoint.Value.X;
            decimal w = boundsX.Size;
            decimal z = this.Config.MoveItemDetectSideMinSize;
            decimal r = (decimal)this.Config.MoveItemDetectSideRatio;
            if (w <= z) return RangeSide.Begin;
            decimal t = z + (r * (w - z));
            decimal x = (mouseX - boundsX.Begin);
            return ((x <= t) ? RangeSide.Begin : RangeSide.End);
        }
        /// <summary>
        /// Vrátí opačnou stranu <see cref="RangeSide"/> ke straně zadané
        /// </summary>
        /// <param name="nearSide"></param>
        /// <returns></returns>
        private RangeSide _AdjustGetOppositeSide(RangeSide nearSide)
        {
            return (nearSide == RangeSide.End ? RangeSide.Begin : RangeSide.End);
        }
        /// <summary>
        /// Metoda zjistí, zda aktuální prvek je relativně blízko svému původnímu času, protože i k němu může být "přichycen".
        /// Při pohybu v původním grafu je tato přichytávací vzdálenost menší = je snadné přesunou prvek o malý časový kousek ve vlastním grafu.
        /// Při pohybu v jiném grafu je tato přichytávací vzdálenost větší = je snadné přemístit prvek na jiný řádek, a zachovat přitom původní čas.
        /// Pokud bude určeno, že přichytávání se provede, pak se vrátí datum Begin původního času, a nastaví se ref parametr nearSide na hodnotu <see cref="RangeSide.Begin"/>.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="snapInfo">Informace o nastavení MoveSnap pro aktuální modifikační klávesy Ctrl/Shift</param>
        /// <param name="nearSide"></param>
        /// <returns></returns>
        private DateTime? _AdjustSearchOriginalTime(GraphItemChangeInfo moveInfo, SchedulerConfig.MoveSnapInfo snapInfo, ref RangeSide nearSide)
        {
            DateTime? nearTime = null;

            bool isSameGraph = !moveInfo.IsChangeRow;
            // Aktivita, podle přesunu na jiný / stejný graf:
            bool isOriginalTimeActive = (isSameGraph ? snapInfo.OriginalTimeNearActive : snapInfo.OriginalTimeLongActive);
            if (!isOriginalTimeActive) return null;

            // Limitní vzdálenost od výchozí pozice, podle toho, zda jde o přesun na jiný / stejný graf:
            int originalTimeDistance = (isSameGraph ? snapInfo.OriginalTimeNearDistance : snapInfo.OriginalTimeLongDistance);

            Rectangle boundsSource = moveInfo.SourceBounds;                              // Výchozí souřadnice prvku
            Rectangle boundsTarget = moveInfo.BoundsFinalAbsolute;                       // Přesunutá souřadnice
            bool testBegin = (moveInfo.MoveMode != GraphItemMoveMode.ResizeEnd);         // Budeme testovat souřadnici X nebo Right? (Right = jen pro ResizeEnd)
            int sourceX = (testBegin ? boundsSource.X : boundsSource.Right);
            int targetX = (testBegin ? boundsTarget.X : boundsTarget.Right);
            int diffX = ((sourceX > targetX) ? (sourceX - targetX) : (targetX - sourceX));    // O kolik pixelů jsme přesunuli prvek oproti původnímu stavu?
            if (diffX <= originalTimeDistance)
            {   // Menší přesun => provedeme adjust = přichytíme prvek k výchozímu bodu:
                nearSide = this._AdjustDetectNearSide(moveInfo);
                nearTime = (nearSide == RangeSide.Begin ? moveInfo.SourceTime.Begin.Value : moveInfo.SourceTime.End.Value);
            }
            return nearTime;
        }
        /// <summary>
        /// Metoda určí zaokrouhlený čas prvku
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="snapInfo">Informace o nastavení MoveSnap pro aktuální modifikační klávesy Ctrl/Shift</param>
        /// <param name="nearSide"></param>
        /// <returns></returns>
        private DateTime? _AdjustSearchRoundTime(GraphItemChangeInfo moveInfo, SchedulerConfig.MoveSnapInfo snapInfo, ref RangeSide nearSide)
        {
            if (!snapInfo.GridTickActive) return null;

            // Podle pozice myši vzhledem k prvku určíme stranu prvku (Begin/End), která se má přichytávat:
            nearSide = this._AdjustDetectNearSide(moveInfo);
            DateTime timePoint = (nearSide == RangeSide.Begin ? moveInfo.TimeRangeFinal.Begin.Value : moveInfo.TimeRangeFinal.End.Value);
            DateTime? nearTime = moveInfo.GetRoundedTime(timePoint, AxisTickType.StdTick);

            // Měli bychom vyhodnotit, jestli upravený čas je v rozsahu (snapInfo.GridTickDistance) od původního času!

            return nearTime;
        }
        /// <summary>
        /// Metoda zpracuje výsledky (přichytávání času) do dat o modifikovaném prvku (Move/Resize).
        /// Prvek již má určené hodnoty <see cref="GraphItemChangeInfo.BoundsFinalAbsolute"/> i <see cref="GraphItemChangeInfo.TimeRangeFinal"/>, 
        /// ale bez přichytávání a bez zarovnání Y.
        /// </summary>
        /// <param name="moveInfo">Data o přesunu</param>
        /// <param name="nearTime">Upravené datum</param>
        /// <param name="nearSide">Upravená strana</param>
        private void _AdjustSetResults(GraphItemChangeInfo moveInfo, DateTime? nearTime, RangeSide nearSide)
        {
            switch (moveInfo.MoveMode)
            {
                case GraphItemMoveMode.Move:
                    this._AdjustSetResultsMove(moveInfo, nearTime, nearSide);
                    break;
                case GraphItemMoveMode.ResizeBegin:
                    this._AdjustSetResultsResizeBegin(moveInfo, nearTime, nearSide);
                    break;
                case GraphItemMoveMode.ResizeEnd:
                    this._AdjustSetResultsResizeEnd(moveInfo, nearTime, nearSide);
                    break;
            }
        }
        /// <summary>
        /// Metoda zpracuje výsledky (přichytávání času) do dat o přesouvaném prvku.
        /// Prvek již má určené hodnoty <see cref="GraphItemChangeInfo.BoundsFinalAbsolute"/> i <see cref="GraphItemChangeInfo.TimeRangeFinal"/>, 
        /// ale bez přichytávání a bez zarovnání Y.
        /// </summary>
        /// <param name="moveInfo">Data o přesunu</param>
        /// <param name="nearTime">Upravené datum</param>
        /// <param name="nearSide">Upravená strana</param>
        private void _AdjustSetResultsMove(GraphItemChangeInfo moveInfo, DateTime? nearTime, RangeSide nearSide)
        {
            bool hasNearSide = (nearSide == RangeSide.Begin || nearSide == RangeSide.End);

            Rectangle sourceBounds = moveInfo.SourceBounds;
            Rectangle targetBounds = moveInfo.BoundsFinalAbsolute;

            // Úprava X a Time:
            if (nearTime.HasValue && hasNearSide)
            {
                int? nearX = moveInfo.GetPositionForTime(nearTime.Value);
                TimeSpan sourceSpan = moveInfo.SourceTime.Size.Value;
                TimeRange targetTime = moveInfo.TimeRangeFinal;
                if (nearSide == RangeSide.Begin)
                {   // Čas a pozice vyjadřují Begin prvku:
                    targetBounds.X = nearX.Value;
                    targetTime = TimeRange.CreateFromBeginSize(nearTime.Value, sourceSpan);
                }
                else
                {   // Čas a pozice vyjadřují End prvku:
                    targetBounds.X = nearX.Value - sourceBounds.Width;
                    targetTime = TimeRange.CreateFromSizeEnd(sourceSpan, nearTime.Value);
                }
                moveInfo.TimeRangeFinal = targetTime;
            }

            // Úprava Y:
            GraphItemMoveAlignY alignY = this._AdjustSetResultsGetAlignY(moveInfo);
            switch (alignY)
            {
                case GraphItemMoveAlignY.OnOriginalItemPosition:
                    // Dle původní souřadnice Y:
                    targetBounds.Y = sourceBounds.Y;
                    break;
                case GraphItemMoveAlignY.OnGraphTopPosition:
                    // Na souřadnici Y + 1 cílového grafu:
                    if (moveInfo.TargetGraph != null)
                    {   // jen pokud známe Cílový graf (on totiž může být null !!)
                        int graphY = moveInfo.TargetGraph.BoundsAbsolute.Y;
                        targetBounds.Y = graphY + 1;
                    }
                    break;
                case GraphItemMoveAlignY.OnMousePosition:
                default:
                    // Dle pozice myši = souřadnice Y se nechává bez úprav.
                    break;
            }

            // Vložení nearSide:
            if (!hasNearSide)
                nearSide = this._AdjustDetectNearSide(moveInfo);
            moveInfo.AttachedSide = nearSide;
            
            moveInfo.BoundsFinalAbsolute = targetBounds;
        }
        /// <summary>
        /// Metoda provede adjustaci hodnoty Begin v procesu ResizeBegin
        /// </summary>
        /// <param name="moveInfo">Data o přesunu</param>
        /// <param name="nearTime">Upravené datum</param>
        /// <param name="nearSide">Upravená strana</param>
        private void _AdjustSetResultsResizeBegin(GraphItemChangeInfo moveInfo, DateTime? nearTime, RangeSide nearSide)
        {
            if (nearTime.HasValue)
            {
                // Čas: upravíme Begin, ponecháme End:
                moveInfo.TimeRangeFinal = new TimeRange(nearTime, moveInfo.SourceTime.End);

                // Souřadnice: upravíme X, ponecháme Right a osu Y:
                int? nearX = moveInfo.GetPositionForTime(nearTime.Value);
                Rectangle sourceBounds = moveInfo.SourceBounds;
                moveInfo.BoundsFinalAbsolute = new Rectangle(nearX.Value, sourceBounds.Y, sourceBounds.Right - nearX.Value, sourceBounds.Height);
                moveInfo.AttachedSide = RangeSide.Begin;
            }
        }
        /// <summary>
        /// Metoda provede adjustaci hodnoty End v procesu ResizeEnd
        /// </summary>
        /// <param name="moveInfo">Data o přesunu</param>
        /// <param name="nearTime">Upravené datum</param>
        /// <param name="nearSide">Upravená strana</param>
        private void _AdjustSetResultsResizeEnd(GraphItemChangeInfo moveInfo, DateTime? nearTime, RangeSide nearSide)
        {
            if (nearTime.HasValue)
            {
                // Čas: ponecháme Begin, upravíme End:
                moveInfo.TimeRangeFinal = new TimeRange(moveInfo.SourceTime.Begin, nearTime);

                // Souřadnice: ponecháme X, upravíme Right a osu Y:
                int? nearX = moveInfo.GetPositionForTime(nearTime.Value);
                Rectangle sourceBounds = moveInfo.SourceBounds;
                moveInfo.BoundsFinalAbsolute = new Rectangle(sourceBounds.X, sourceBounds.Y, nearX.Value - sourceBounds.X, sourceBounds.Height);
                moveInfo.AttachedSide = RangeSide.End;
            }
        }
        /// <summary>
        /// Metoda vrátí režim zarovnání na ose Y pro přesouvaný prvek a podle konfigurace
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        private GraphItemMoveAlignY _AdjustSetResultsGetAlignY(GraphItemChangeInfo moveInfo)
        {
            GraphItemMoveAlignY alignY = GraphItemMoveAlignY.OnMousePosition;
            GraphItemMoveAlignY configY;
            if (!moveInfo.IsChangeRow)
            {   // Beze změny grafu = na tom, odkud prvek pochází:
                configY = this._GuiData.Properties.GraphItemMoveSameGraph;
                if (configY == GraphItemMoveAlignY.OnMousePosition || configY == GraphItemMoveAlignY.OnOriginalItemPosition || configY == GraphItemMoveAlignY.OnGraphTopPosition)
                    alignY = configY;
            }
            else
            {   // Přesun prvku na jiný graf:
                configY = this._GuiData.Properties.GraphItemMoveOtherGraph;
                if (configY == GraphItemMoveAlignY.OnOriginalItemPosition)
                    alignY = GraphItemMoveAlignY.OnGraphTopPosition;
                else if (configY == GraphItemMoveAlignY.OnMousePosition || configY == GraphItemMoveAlignY.OnGraphTopPosition)
                    alignY = configY;
            }
            return alignY;
        }
        #endregion
        #region Další detaily chování GUI - ToolBar (definice i obsluha Clicku)
        /// <summary>
        /// Inicializace položek ToolBaru (grupa <see cref="_ToolbarSystemGuiEditGroup"/>) pro řízení pohybu prvků
        /// </summary>
        private void _GuiEditToolBarInit()
        {
            // Pokud v definici (ToolbarShowSystemItems) je uvedena hodnota (MoveItemAll | GuiEditAll), 
            //  pak NEBUDOU zobrazovány jednotlivé buttony, ale místo nich se zobrazí velký button CONFIG:
            ToolbarSystemItem currentItems = this._GuiToolbarPanel.ToolbarShowSystemItems;
            if (_SystemItemsIsConfig(currentItems)) return;

            ToolbarSystemItem items = (ToolbarSystemItem)(currentItems & ToolbarSystemItem.GuiEditAll);
            if (App.IsDebugMode)
                items = ToolbarSystemItem.GuiEditAll;

            if (items == ToolbarSystemItem.None) return;

            // Oddělovač, pokud v grupě už jsou položky:
            this._ToolbarSystemGuiEditGroup.AddSeparator(this);

            if (items.HasFlag(ToolbarSystemItem.GuiEditShowLinkWholeTask))
                this._ToolbarSystemGuiEditGroup.Items.Add(_CreateToolbarItem(_Tlb_GuiEdit_ShowLinkWholeTask, R.Images.Actions.OfficeChartLinePng, null, "Při najetí myší zobrazovat vztahy v rámci celého postupu, nejen nejbližší sousední položky", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemSkipToNextRow, moduleWidth: 1, isSelectable: true, isSelected: this.Config.GuiEditShowLinkMouseWholeTask, userData: ToolbarSystemItem.GuiEditShowLinkWholeTask));
            if (items.HasFlag(ToolbarSystemItem.GuiEditShowLinkAsSCurve))
                this._ToolbarSystemGuiEditGroup.Items.Add(_CreateToolbarItem(_Tlb_GuiEdit_ShowLinkWholeTask, R.Images.Actions.DrawBezierCurvesPng, null, "Vztahy zobrazovat jako křivky (výchozí: zobrazovat jako rovné čáry)", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemSkipToNextRow, moduleWidth: 1, isSelectable: true, isSelected: this.Config.GuiEditShowLinkAsSCurve, userData: ToolbarSystemItem.GuiEditShowLinkAsSCurve));
        }
        /// <summary>
        /// Metoda se volá po akci Click na systémové položce ToolBaru. 
        /// Metoda zjistí, zda akce se týká přesunu prvků, a pokud ano pak ji vyřeší.
        /// Pokud se akce nijak netýká přesunu prvků, pak nic neprovádí (není tedy problém ji zavolat).
        /// </summary>
        /// <param name="item"></param>
        private void _GuiEditToolBarClick(FunctionItem item)
        {
            if (!(item.UserData is ToolbarSystemItem)) return;               // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě.
            this._GuiEditToolBarAction(item, (ToolbarSystemItem)item.UserData);
        }
        /// <summary>
        /// Po kliknutí na systémovou ikonu Toolbaru, řeší akce typu ItemMove
        /// </summary>
        /// <param name="item"></param>
        /// <param name="action"></param>
        private void _GuiEditToolBarAction(FunctionItem item, ToolbarSystemItem action)
        {
            // Jako "Povolovací hodnota" akce Config je brána hodnota (MoveItemAll | GuiEditAll),
            //  pokud tam je, pak se jedná o Config a ne o jednotlivou akci:
            if (_SystemItemsIsConfig(action)) return;

            action = (ToolbarSystemItem)(action & ToolbarSystemItem.GuiEditAll);
            if (action == ToolbarSystemItem.None) return;

            switch (action)
            {
                case ToolbarSystemItem.GuiEditShowLinkWholeTask:
                    this.Config.GuiEditShowLinkMouseWholeTask = item.IsChecked;
                    break;
                case ToolbarSystemItem.GuiEditShowLinkAsSCurve:
                    this.Config.GuiEditShowLinkAsSCurve = item.IsChecked;
                    this.Config.Save();
                    break;
            }
        }
        private const string _Tlb_GuiEdit_ShowLinkWholeTask = "GuiEditShowLinkWholeTask";
        private const string _Tlb_GuiEdit_ShowLinkAsSCurve = "GuiEditShowLinkAsSCurve";
        #endregion
        #region Datové panely, jednotlivé datové tabulky
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
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainData", "FillMainControlPagesFromGui", ""))
            {
                this._MainControl.ClearPages();
                this._MainControl.SynchronizedTime.Value = this.GuiData.Properties.InitialTimeRange;
                this._MainControl.SynchronizedTime.ValueLimit = this.GuiData.Properties.TotalTimeRange;
                foreach (GuiPage guiPage in this._GuiPages.Pages)
                    App.TryRun(() => this._MainControl.AddPage(guiPage));
                this._FillDataTables();
                this._PrepareAllTablesAfterLoad();

                // Následující řádek musí být až po _FillDataTables():
                //  protože tam se registruje eventhandler změny času do jednotlivých Gridů (a proto se vykoná dříve),
                //  a teprve poté chceme registrovat eventhandler globální (_SynchronizedTimeValueChanging),
                //   který bude volán až poté, kdy jednotlivé Gridy mají svoji obsluhu změny času hotovou:
                this._MainControl.SynchronizedTime.ValueChanging += _SynchronizedTimeValueChanging;
            }
        }
        /// <summary>
        /// Souhrn všech datových tabulek ze všech panelů.
        /// Každá tabulka má své unikátní jméno (alespoň měla by mít), uložené v <see cref="MainDataTable.TableName"/>.
        /// </summary>
        protected MainDataTable[] DataTables { get { return this._DataTables; } }
        /// <summary>
        /// Metoda načte souhrn všech tabulek <see cref="MainDataTable"/> ze všech vytvořených panelů do <see cref="_DataTables"/>.
        /// </summary>
        private void _FillDataTables()
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainData", "FillDataTables", ""))
            {
                List<MainDataTable> tableList = new List<MainDataTable>();
                foreach (SchedulerPanelInfo panel in this._MainControl.SchedulerPanels)
                    tableList.AddRange(panel.SchedulerPanel.DataTables);
                this._DataTables = tableList.ToArray();
            }
        }
        /// <summary>
        /// Metoda zavolá akci <see cref="MainDataTable.PrepareAfterLoad()"/> pro všechny načtené tabulky ze soupisu <see cref="_DataTables"/>.
        /// </summary>
        private void _PrepareAllTablesAfterLoad()
        {
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "MainData", "PrepareAllTables", ""))
            {
                foreach (MainDataTable dataTable in this._DataTables)
                {
                    dataTable.PrepareAfterLoad();
                }
            }
        }
        /// <summary>
        /// Metoda má najít a vrátit komplexní tabulku MainDataTable podle jejího plného jména.
        /// Může vrátit null.
        /// Pokud by existovalo více tabulek shodného jména, vrací první z nich.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private MainDataTable _SearchTable(string fullName)
        {
            return (this._DataTables != null && !String.IsNullOrEmpty(fullName) ? this._DataTables.FirstOrDefault(t => String.Equals(t.TableName, fullName)) : null);
        }
        /// <summary>
        /// Souhrn všech datových tabulek ze všech panelů.
        /// </summary>
        private MainDataTable[] _DataTables;
        /// <summary>
        /// Tabulka s daty, která je nyní aktivní.
        /// Obsahuje tabulku, která má nebo měla poslední focus.
        /// Pokud focus odejde na jiný objekt, než tabulka, pak zde zůstává odkaz na poslední tabulku.
        /// Null je zde jen po spuštění, ale je to přípustný stav.
        /// </summary>
        internal MainDataTable ActiveDataTable { get; private set; }
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
            this._ProcessResponse(response.GuiResponse);
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
            /// true pokud je reálně přítomna položka <see cref="_GuiContextMenuItem"/>
            /// </summary>
            private bool _HasItem { get { return (this._GuiContextMenuItem != null); } }
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
            /// Jméno tohoto prvku, prostor pro aplikační identifikátor položky
            /// </summary>
            public override string Name { get { return (this._HasItem ? this._GuiContextMenuItem.Name : base.Name); } set { if (this._HasItem) this._GuiContextMenuItem.Name = value; else base.Name = value; } }
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
                if (gridItemId == null || infos == null || infos.Length == 0) return true;      // Bez zadání => null => bez omezení => true

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
            currentState.TimeAxisValue = this.SynchronizedTime;
            currentState.SelectedGraphItems = this._CreateGuiCurrentSelectedGraphItems();

            return currentState;
        }
        /// <summary>
        /// Metoda najde všechny aktuálně vybrané prvky grafů v GUI (s pomocí instance <see cref="Selector"/>),
        /// a vrátí pole jejich identifikátorů <see cref="GuiGridItemId"/>.
        /// </summary>
        /// <returns></returns>
        private GuiGridItemId[] _CreateGuiCurrentSelectedGraphItems()
        {
            Dictionary<GuiId, GuiGridItemId> gridItemDict = new Dictionary<GuiId, GuiGridItemId>();
            foreach (IInteractiveItem item in this._MainControl.Selector.SelectedItems)
            {
                GuiGridItemId[] gridItems = MainDataTable.GetGuiGridItems(item, true);   // Pro daný prvek IInteractiveItem získá pole identifikátorů GuiGridItemId
                gridItemDict.AddNewItems(gridItems, g => g.ItemId);            // Do gridItemDict přidá jen ty prvky z pole gridItems, které mají jiný ItemId, než prvky již existující.
            }
            return gridItemDict.Values.ToArray();
        }
        #endregion
        #region Zpracování odpovědi (data v GuiResponse) z aplikace poté, kdy doběhla aplikační funkce
        /// <summary>
        /// Metoda zpracuje odpovědi z aplikace.
        /// </summary>
        /// <param name="guiResponse"></param>
        private GuiDialogButtons _ProcessResponse(GuiResponse guiResponse)
        {
            if (guiResponse == null) return GuiDialogButtons.None;

            this._ProcessResponseData(guiResponse);
            GuiDialogButtons dialogResult = this._ProcessResponseDialog(guiResponse);
            return dialogResult;
        }
        #region Zpracování dat
        /// <summary>
        /// Metoda zpracuje odpovědi z aplikace, část týkající se dat
        /// </summary>
        /// <param name="guiResponse"></param>
        private void _ProcessResponseData(GuiResponse guiResponse)
        {
            Dictionary<string, MainDataTable> mainTableDict = this.DataTables.Where(t => t.TableName != null).GetDictionary(t => t.TableName, true);
            Dictionary<uint, GTimeGraph> refreshGraphDict = new Dictionary<uint, GTimeGraph>();
            this._ProcessResponseCommon(guiResponse.Common, mainTableDict);
            this._ProcessResponseToolbarItems(guiResponse.ToolbarItems);
            this._ProcessResponseTime(guiResponse.TimeAxisValue);
            this._ProcessResponseRemoveItems(guiResponse.RemoveItems, mainTableDict, refreshGraphDict);
            this._ProcessResponseUpdateGraphs(guiResponse.UpdateGraphs, mainTableDict, refreshGraphDict);
            this._ProcessResponseAddItems(guiResponse.AddItems, mainTableDict, refreshGraphDict);
            this._ProcessResponseUpdateLinks(guiResponse.ChangeLinks, mainTableDict, refreshGraphDict);
            this._ProcessResponseRefreshGraphs(refreshGraphDict.Values);
            this._ProcessResponseRefreshParentChilds(refreshGraphDict.Values);
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.Common"/>
        /// </summary>
        /// <param name="common">Common data z odpovědi</param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        private void _ProcessResponseCommon(GuiResponseCommon common, Dictionary<string, MainDataTable> mainTableDict)
        {
            if (common == null) return;
            if (common.ClearLinks)
            {   // Odebrat Linky ze všech tabulek:
                mainTableDict.Values.ForEachItem(table =>
                {
                    GTimeGraphLinkArray graphLink = table.GraphLinkArray;
                    if (graphLink != null)
                        graphLink.Clear();
                });
            }

            if (common.ClearSelected)
            {   // Odselectovat všechny selectované prvky:
                this._MainControl.Selector.ClearSelected();
            }

            if (common.ClearLinks && !common.ClearSelected)
            {   // Najít a obnovit Linky:
                foreach (var item in this._MainControl.Selector.SelectedItems)
                {
                    GTimeGraphItem graphItem = item as GTimeGraphItem;
                    if (graphItem != null)
                        graphItem.ActivateLink(false, true);
                }
            }

            if (common.TimeAxisValue != null)
            {   // Nastavit zobrazený čas:
                this.SynchronizedTime = common.TimeAxisValue;
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.ToolbarItems"/>
        /// </summary>
        /// <param name="toolbarItems"></param>
        private void _ProcessResponseToolbarItems(GuiToolbarItem[] toolbarItems)
        {
            if (toolbarItems == null) return;
            this._ToolBarRefreshFromResponse(toolbarItems);
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.TimeAxisValue"/>
        /// </summary>
        /// <param name="timeRange"></param>
        private void _ProcessResponseTime(GuiTimeRange timeRange)
        {
            if (timeRange == null) return;
            this._MainControl.SynchronizedTime.Value = timeRange;
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.RemoveItems"/>
        /// </summary>
        /// <param name="removeItems"></param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="refreshGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseRemoveItems(GuiGridItemId[] removeItems, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, GTimeGraph> refreshGraphDict)
        {
            if (removeItems == null) return;
            foreach (GuiGridItemId removeItem in removeItems)
            {
                if (removeItem == null || removeItem.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(removeItem.TableName, out mainDataTable))
                    mainDataTable.RemoveGraphItem(removeItem, refreshGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.UpdateGraphs"/>
        /// </summary>
        /// <param name="updateGraphs"></param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="refreshGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseUpdateGraphs(GuiResponseGraph[] updateGraphs, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, GTimeGraph> refreshGraphDict)
        {
            if (updateGraphs == null) return;
            foreach (GuiResponseGraph updateGraph in updateGraphs)
            {
                if (updateGraph == null || updateGraph.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(updateGraph.TableName, out mainDataTable))
                    mainDataTable.UpdateGraph(updateGraph, refreshGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.AddItems"/>
        /// </summary>
        /// <param name="addItems">Přidávané položky do grafů</param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="refreshGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseAddItems(GuiResponseGraphItem[] addItems, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, GTimeGraph> refreshGraphDict)
        {
            if (addItems == null) return;
            foreach (GuiResponseGraphItem addItem in addItems)
            {
                if (addItem == null || addItem.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(addItem.TableName, out mainDataTable))
                    mainDataTable.UpdateGraphItem(addItem, refreshGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.ChangeLinks"/>
        /// </summary>
        /// <param name="changeLinks">Změněné vztahy</param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="refreshGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseUpdateLinks(GuiResponseGraphLink[] changeLinks, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, GTimeGraph> refreshGraphDict)
        {
            if (changeLinks == null) return;
            var changeGroups = changeLinks.Where(l => l.TableName != null).GroupBy(l => l.TableName);
            foreach (var changeGroup in changeGroups)
            {   // Jdeme po skupinách, kde jedna skupina (changeGroup.Key) = jeden název tabulky GuiResponseGraphLink.TableName:
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(changeGroup.Key, out mainDataTable))
                    mainDataTable.UpdateGraphLinks(changeGroup);
            }
        }
        /// <summary>
        /// Metoda provede Refresh na všech grafech, které se nacházejí v parametru refreshGraphDict.
        /// </summary>
        /// <param name="refreshGraphs"></param>
        private void _ProcessResponseRefreshGraphs(IEnumerable<GTimeGraph> refreshGraphs)
        {
            refreshGraphs.ForEachItem(g => g.Refresh());
        }
        /// <summary>
        /// Metoda zajistí nové vyhodnocení vztahů Parent - Child v těch tabulkách, kde došlo ke změně v grafech
        /// </summary>
        /// <param name="refreshGraphs"></param>
        private void _ProcessResponseRefreshParentChilds(IEnumerable<GTimeGraph> refreshGraphs)
        {
            // Z objektů grafů, kde došlo ke změně, s pomocí GTimeGraph.UserData, což je reference na MainDataTable, získám distinct seznam tabulek:
            MainDataTable[] tables = refreshGraphs
                .Where(g => g.UserData != null && g.UserData is MainDataTable)
                .Select(g => g.UserData as MainDataTable)
                .GetDictionary(t => t.TableName, true)
                .Values
                .ToArray();

            // Pro každou z tabulek zavolám metodu pro vyhodnocení jejich Parent - Child:
            tables.ForEachItem(t => t.PrepareDynamicChilds());
        }
        #endregion
        #region Zpracování dialogu
        /// <summary>
        /// Metoda zpracuje dialog, zadaný v Response
        /// </summary>
        /// <param name="guiResponse"></param>
        /// <returns></returns>
        private GuiDialogButtons _ProcessResponseDialog(GuiResponse guiResponse)
        {
            GuiDialogButtons response = GuiDialogButtons.None;
            if (guiResponse != null && guiResponse.Dialog != null && !guiResponse.Dialog.IsEmpty)
                response = this.ShowDialog(guiResponse.Dialog);
            return response;
        }
        /// <summary>
        /// Metoda zpracuje požadovaný dialog, vrací výsledek
        /// </summary>
        /// <param name="dialog">Data pro dialog</param>
        /// <returns></returns>
        protected GuiDialogButtons ShowDialog(GuiDialog dialog)
        {
            return this._MainControl.ShowDialog(dialog);
        }
        /// <summary>
        /// Zobrazí daný dialog a vrátí odpověď.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="guiButtons"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public GuiDialogButtons ShowDialog(string message, string title = null, GuiDialogButtons guiButtons = GuiDialogButtons.None, GuiImage icon = null)
        {
            return this._MainControl.ShowDialog(message, title, guiButtons, icon);
        }
        #endregion
        #endregion
        #region Zavírání hlavního okna
        /// <summary>
        /// Event před zavřením okna: pošleme informaci hostiteli a počkáme si na odpověď, jinak se okno zavře.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (this._MainFormClosingTest(e))
                this._ClosingProcessStart();
        }
        /// <summary>
        /// Metoda zjistí, zda se hlavní okno má zavřít nebo ne, a zda se mají provést uzavírací algoritmy.
        /// Pokud se uzavírací algoritmy MAJÍ provádět, vrací se TRUE, pokud se provádět NEMAJÍ, vrací se FALSE.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _MainFormClosingTest(FormClosingEventArgs e)
        {
            // Některé důvody zavření okna jsou nadřazené, tam zavření okna nebudeme bránit a ani nebudeme provádět uzavírací algoritmus:
            switch (e.CloseReason)
            {
                case CloseReason.TaskManagerClosing:
                case CloseReason.WindowsShutDown:
                    // Nebráníme zavření okna, a vracíme false = neprovádí se uzavírací algoritmus:
                    return false;
            }

            // Podle stavu procesu zavírání zjistíme, zda se má okno zavřít nebo nezavírat, a zda se má spustit uzavírací algoritmus nebo ne:
            switch (this.ClosingState)
            {
                case MainFormClosingState.WaitCommandQueryCloseWindow:
                case MainFormClosingState.WaitCommandSaveBeforeCloseWindow:
                    // Aktuálně běží nějaké commandy související se zavíráním okna => v tuto dobu nelze okno zavřít, ani znovu rozběhnout další uzavírací proces:
                    if (!this.ClosingStart.HasValue || ((DateTime.Now - this.ClosingStart.Value) >= TimeSpan.FromSeconds(7d)))
                        // Pokud již čekáme déle než 7 sekund, dáme uživateli dialog; a okno zavřeme jen když uživatel souhlasí:
                        e.Cancel = (this.ShowDialog("Čekáme na doběhnutí funkce; přejete si i přesto zavřít okno?", null, GuiDialogButtons.YesNo) == GuiDialogButtons.No);
                    else
                        // Pokud již čekáme jenom malou chvíli, dialog uživateli nedáme:
                        e.Cancel = true;
                    return false;
                case MainFormClosingState.ClosingForm:
                    // Commandy související se zavíráním okna doběhly a stanovily, že okno se skutečně zavře => povolíme zavření okna bez dalších cavyků:
                    return false;
            }

            // Zabráníme zavření okna a spustíme uzavírací algoritmus:
            e.Cancel = true;
            return true;
        }
        /// <summary>
        /// Zahájí proces zavírání okna
        /// </summary>
        private void _ClosingProcessStart()
        {
            //  Není možné volat AppHost synchronně, proto musíme synchronní požadavek řešit jinak:
            // a) Zavoláme asynchronně AppHost s Commandem COMMAND_QueryCloseWindow, a zablokujeme UI na 5 sekund (timeout na získání jednoduché response z aplikačního serveru)
            // b) Na formuláři si poznačíme aktuální stav zavrání okna "čekáme odpověď COMMAND_QueryCloseWindow";
            // c) cancelujeme zavírání okna, ale máme blokované GUI; uživatel toho moc dalšího neudělá
            // d) přijde odpověď na COMMAND_QueryCloseWindow do metody _ClosingProcessResponseQuery; pokračování komentáře tam.
            this.ClosingState = MainFormClosingState.WaitCommandQueryCloseWindow;

            this.ClosingStart = DateTime.Now;

            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_QueryCloseWindow;
            this._CallAppHostFunction(request, this._ClosingProcessResponseQuery, TimeSpan.FromSeconds(5d));
        }
        /// <summary>
        /// Zpracování odpovědi z AppHost na request COMMAND_QueryCloseWindow
        /// </summary>
        /// <param name="responseArgs"></param>
        private void _ClosingProcessResponseQuery(AppHostResponseArgs responseArgs)
        {
            if (responseArgs == null)
            {
                this._ClosingProcessCloseNow();
                return;
            }

            // Zpracujeme odpověď z aplikace, mj. může být proveden uživatelský dialog a vrácena odpověď na něj:
            GuiDialogButtons dialogResult = this._ProcessResponse(responseArgs.GuiResponse);

            // Zpracujeme odpověď uživatele na dotaz, pokud byl:
            switch (dialogResult)
            {
                case GuiDialogButtons.Yes:           // Ano, uložit data
                case GuiDialogButtons.Ok:            // OK, uložit data
                    this._ClosingProcessSaveData(responseArgs.GuiResponse);
                    break;
                case GuiDialogButtons.Maybe:         // Response z AppHost nedorazila, nebo neobsahovala dialog => data se ukládat nebudou, ale skončíme
                case GuiDialogButtons.None:          // Response dorazila, ale nebyl v ní žádný dialog
                case GuiDialogButtons.No:            // Dialog byl, odpověď uživatele zní: Neuložit data a skončit:
                    this._ClosingProcessCloseNow();
                    break;
                case GuiDialogButtons.Cancel:        // Zrušit zavírání, ale data neukládat:
                    this.ClosingStart = null;
                    this.ClosingState = MainFormClosingState.None;
                    break;
            }
        }
        /// <summary>
        /// Zajistí reálné a okamžité zavření okna
        /// </summary>
        private void _ClosingProcessCloseNow()
        {
            this.ClosingState = MainFormClosingState.ClosingForm;    // Tato hodnota zajistí, že příští pokus o zavření okna proběhne hladce a bez dalších dotazů.
            this._MainControl.CloseForm();
        }
        /// <summary>
        /// Uložení dat při ukončení scheduleru: vyvolá se command <see cref="GuiRequest.COMMAND_SaveBeforeCloseWindow"/>
        /// </summary>
        /// <param name="guiResponse"></param>
        private void _ClosingProcessSaveData(GuiResponse guiResponse)
        {
            //  Máme uložit data, a poté máme zavřít okno, a to vše asynchronně:
            this.ClosingState = MainFormClosingState.WaitCommandSaveBeforeCloseWindow;

            GuiSaveData saveData = guiResponse.CloseSaveData;
            TimeSpan? blockGuiTime = saveData?.BlockGuiTime;
            string blockGuiMessage = saveData?.BlockGuiMessage;

            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_SaveBeforeCloseWindow;
            this._CallAppHostFunction(request, this._ClosingProcessResponseSave, blockGuiTime, blockGuiMessage);
        }
        /// <summary>
        /// Zpracování odpovědi z AppHost na request COMMAND_SaveBeforeCloseWindow
        /// </summary>
        /// <param name="responseArgs"></param>
        private void _ClosingProcessResponseSave(AppHostResponseArgs responseArgs)
        {
            this.ClosingStart = null;

            if (responseArgs == null)
            {
                this._ClosingProcessCloseNow();
                return;
            }

            // Data jsou uložena, zpracujeme response:
            // Pokud se nám vrátila chyba, pak zavírání okna zrušíme:
            if (responseArgs != null && responseArgs.Result == AppHostActionResult.Failure)
            {   // Po chybě dáme dialog:
                GuiDialogButtons response = this._ProcessResponse(responseArgs.GuiResponse);
                if (response == GuiDialogButtons.None || response == GuiDialogButtons.Yes || response == GuiDialogButtons.Ignore)
                    this._ClosingProcessCloseNow();
                else
                    this.ClosingState = MainFormClosingState.None;
            }
            else
            {   // Bez chyby => zavřeme okno:
                this._ClosingProcessCloseNow();
            }
        }
        /// <summary>
        /// Event při zavření okna: pošleme informaci hostiteli.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainFormClosed(object sender, FormClosedEventArgs e)
        {
            this.CloseSchedulerData();

            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_CloseWindow;
            this._CallAppHostFunction(request, null);
        }
        /// <summary>
        /// Aktuální stav procesu zavírání okna
        /// </summary>
        protected MainFormClosingState ClosingState { get; private set; }
        /// <summary>
        /// Datum a čas, kdy se zahájil proces zavírání okna
        /// </summary>
        protected DateTime? ClosingStart { get; private set; }
        /// <summary>
        /// Stavy procesu zavírání okna
        /// </summary>
        protected enum MainFormClosingState
        {
            /// <summary>
            /// Okno se nezavírá. Pokud přijde event MainFormClosing, pak se řeší, a to od začátku.
            /// </summary>
            None = 0,
            /// <summary>
            /// Byl proveden pokus o zavření okna, zpracovává se, očekáváme response na command QueryCloseWindow, GUI je blokováno
            /// </summary>
            WaitCommandQueryCloseWindow,
            /// <summary>
            /// Byl proveden pokus o zavření okna, zpracovává se, ukládají se data příkazem SaveBeforeCloseWindow, čekáme na jeho doběhnutí, GUI je blokováno
            /// </summary>
            WaitCommandSaveBeforeCloseWindow,
            /// <summary>
            /// Nyní reálně zavíráme okno
            /// </summary>
            ClosingForm
        }
        /// <summary>
        /// Reálné zavření okna, těsně před odesláním commandu <see cref="GuiRequest.COMMAND_CloseWindow"/>.
        /// Zde se například provádí uložení konfigurace.
        /// </summary>
        protected void CloseSchedulerData()
        {
            this.Config.Save();
        }
        #endregion
        #region Implementace IMainDataInternal
        /// <summary>
        /// Reference na main data celého systému
        /// </summary>
        GuiData IMainDataInternal.GuiData { get { return this.GuiData; } }
        /// <summary>
        /// Aktuální synchronizovaný časový interval
        /// </summary>
        TimeRange IMainDataInternal.SynchronizedTime { get { return this.SynchronizedTime; } set { this.SynchronizedTime = value; } }
        /// <summary>
        /// Metoda zavolá hostitele a předá mu požadavek.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        /// <param name="blockGuiTime"></param>
        void IMainDataInternal.CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction, TimeSpan? blockGuiTime) { this._CallAppHostFunction(request, callBackAction, blockGuiTime); }
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
        /// <returns></returns>
        ToolStripDropDownMenu IMainDataInternal.CreateContextMenu(GuiGridItemId gridItemId) { return this.CreateContextMenu(gridItemId); }
        /// <summary>
        /// Metoda vrátí aktuální stav celého GUI, pro použití v <see cref="GuiRequest.CurrentState"/>.
        /// </summary>
        /// <returns></returns>
        GuiRequestCurrentState IMainDataInternal.CreateGuiCurrentState() { return this._CreateGuiCurrentState(); }
        /// <summary>
        /// Metoda má za úkol modifikovat data při procesu Drag and Drop pro grafický prvek.
        /// Jde o to, že při přetahování prvků můžeme chtít, aby se prvek "přichytával" 
        /// buď k původnímu času, nebo k okolním bližším prvkům, nebo k zaokrouhlenému času na ose.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="snapInfo">Informace o nastavení MoveSnap pro aktuální modifikační klávesy Ctrl/Shift</param>
        void IMainDataInternal.AdjustGraphItemDragMove(GraphItemChangeInfo moveInfo, SchedulerConfig.MoveSnapInfo snapInfo) { this._AdjustGraphItemDragMove(moveInfo, snapInfo); }
        /// <summary>
        /// Metoda má najít a vrátit komplexní tabulku MainDataTable podle jejího plného jména.
        /// Může vrátit null.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        MainDataTable IMainDataInternal.SearchTable(string fullName) { return this._SearchTable(fullName); }
        /// <summary>
        /// Tabulka s daty, která je nyní aktivní.
        /// Obsahuje tabulku, která má nebo měla poslední focus.
        /// Pokud focus odejde na jiný objekt, než tabulka, pak zde zůstává odkaz na poslední tabulku.
        /// Null je zde jen po spuštění, ale je to přípustný stav.
        /// </summary>
        MainDataTable IMainDataInternal.ActiveDataTable { get { return this.ActiveDataTable; } set { this.ActiveDataTable = value; } }
        /// <summary>
        /// Všechny tabulky s daty
        /// </summary>
        IEnumerable<MainDataTable> IMainDataInternal.DataTables { get { return this._DataTables; } }
        /// <summary>
        /// Metoda zpracuje odpovědi z aplikace.
        /// </summary>
        /// <param name="guiResponse"></param>
        void IMainDataInternal.ProcessResponse(GuiResponse guiResponse) { this._ProcessResponse(guiResponse); }
        #endregion
    }
    /// <summary>
    /// Interface pro zpřístupnění vnitřních metod třídy <see cref="MainData"/>
    /// </summary>
    public interface IMainDataInternal
    {
        /// <summary>
        /// Reference na main data celého systému
        /// </summary>
        GuiData GuiData { get; }
        /// <summary>
        /// Aktuální synchronizovaný časový interval
        /// </summary>
        TimeRange SynchronizedTime { get; set; }
        /// <summary>
        /// Metoda zavolá hostitele a předá mu požadavek.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        /// <param name="blockGuiTime"></param>
        void CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction, TimeSpan? blockGuiTime);
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="gridItemId">Identifikátor gridu, řádku, a prvku grafu</param>
        /// <returns></returns>
        ToolStripDropDownMenu CreateContextMenu(GuiGridItemId gridItemId);
        /// <summary>
        /// Metoda vrátí aktuální stav celého GUI, pro použití v <see cref="GuiRequest.CurrentState"/>.
        /// </summary>
        /// <returns></returns>
        GuiRequestCurrentState CreateGuiCurrentState();
        /// <summary>
        /// Metoda má za úkol modifikovat data při procesu Drag and Drop pro grafický prvek.
        /// Jde o to, že při přetahování prvků můžeme chtít, aby se prvek "přichytával" 
        /// buď k původnímu času, nebo k okolním bližším prvkům, nebo k zaokrouhlenému času na ose.
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <param name="snapInfo">Informace o nastavení MoveSnap pro aktuální modifikační klávesy Ctrl/Shift</param>
        void AdjustGraphItemDragMove(GraphItemChangeInfo moveInfo, SchedulerConfig.MoveSnapInfo snapInfo);
        /// <summary>
        /// Metoda má najít a vrátit komplexní tabulku MainDataTable podle jejího plného jména.
        /// Může vrátit null.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        MainDataTable SearchTable(string fullName);
        /// <summary>
        /// Tabulka s daty, která je nyní aktivní.
        /// Obsahuje tabulku, která má nebo měla poslední focus.
        /// Pokud focus odejde na jiný objekt, než tabulka, pak zde zůstává odkaz na poslední tabulku.
        /// Null je zde jen po spuštění, ale je to přípustný stav.
        /// </summary>
        MainDataTable ActiveDataTable { get; set; }
        /// <summary>
        /// Všechny tabulky s daty
        /// </summary>
        IEnumerable<MainDataTable> DataTables { get; }
        /// <summary>
        /// Metoda zpracuje odpovědi z aplikace.
        /// </summary>
        /// <param name="guiResponse"></param>
        void ProcessResponse(GuiResponse guiResponse);
    }
    #endregion
    #region class GraphItemDragMoveInfo : Analyzovaná data na úrovni Scheduleru, pro akce při přemísťování prvku na úrovni GUI
    /// <summary>
    /// GraphItemDragMoveInfo : Analyzovaná data na úrovni Scheduleru, pro akce při přemísťování prvku na úrovni GUI
    /// </summary>
    public class GraphItemChangeInfo
    {
        /// <summary>
        /// Argumenty pro situaci Drag and Drop
        /// </summary>
        public ItemDragDropArgs DragDropArgs { get; set; }
        /// <summary>
        /// Argumenty pro situaci Resize
        /// </summary>
        public ItemResizeArgs ResizeArgs { get; set; }
        /// <summary>
        /// Režim změny prvku
        /// </summary>
        public GraphItemMoveMode MoveMode { get; set; }
        /// <summary>
        /// Prvek, který se přemisťuje
        /// </summary>
        public ITimeGraphItem DragItem { get; set; }
        /// <summary>
        /// ID prvku, který se přemisťuje
        /// </summary>
        public int DragItemId { get { return this.DragItem.ItemId; } }
        /// <summary>
        /// Id grupy, která se přemisťuje.
        /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
        /// </summary>
        public int DragGroupId { get { return this.DragItem.GroupId; } }
        /// <summary>
        /// Level grupy, která se přemisťuje.
        /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
        /// </summary>
        public int DragLevel { get { return this.DragItem.Level; } }
        /// <summary>
        /// Layer grupy, která se přemisťuje.
        /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
        /// </summary>
        public int DragLayer { get { return this.DragItem.Layer; } }
        /// <summary>
        /// GId grupy, která se přemisťuje.
        /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
        /// </summary>
        public GId DragGroupGId { get; set; }
        /// <summary>
        /// Jednotlivé prvky grupy, které jsou její součástí a mají se přemístit.
        /// Vždy se přemisťuje celá grupa, nikdy ne jednotlivý prvek.
        /// </summary>
        public DataGraphItem[] DragGroupItems { get; set; }
        /// <summary>
        /// Typ aktuální akce Drag and Drop
        /// </summary>
        public DragActionType DragAction { get; set; }
        /// <summary>
        /// Absolutní pozice myši v okamžiku vzniku akce.
        /// Jde o bod, který se nachází někde uvnitř <see cref="SourceBounds"/>, jinak by se prvek nezačal přemísťovat.
        /// Z umístění tohoto bodu lze určit, zda myš byla blíže k počátku, středu nebo konci prvku.
        /// </summary>
        public Point? SourceMousePoint { get; set; }
        /// <summary>
        /// Graf, v němž byl umístěn prvek na začátku.
        /// Může být tentýž, jako cílový (<see cref="TargetGraph"/>).
        /// </summary>
        public GTimeGraph SourceGraph { get; set; }
        /// <summary>
        /// Řádek, na němž byl umístěn prvek na začátku.
        /// Může být tentýž, jako cílový (<see cref="TargetRow"/>).
        /// </summary>
        public GId SourceRow { get; set; }
        /// <summary>
        /// Absolutní souřadnice prvku před přemístěním
        /// </summary>
        public Rectangle SourceBounds { get; set; }
        /// <summary>
        /// Původní čas prvku před přemístěním
        /// </summary>
        public TimeRange SourceTime { get; set; }
        /// <summary>
        /// Přichycená strana prvku při jeho přemísťování, bude tvořit "Pevný bod" při přeplánování.
        /// Toto je výstupní hodnota poté, kdy proběhl proces <see cref="IMainDataInternal.AdjustGraphItemDragMove(GraphItemChangeInfo, SchedulerConfig.MoveSnapInfo)"/>,
        /// tedy když je určeno přichycení některé strany.
        /// </summary>
        public RangeSide AttachedSide { get; set; }
        /// <summary>
        /// Graf, kam má být prvek přemístěn.
        /// Pozor, může být null - když přesun prvku se provádí na místo, kde žádný graf není!
        /// </summary>
        public GTimeGraph TargetGraph { get; set; }
        /// <summary>
        /// Cílový řádek, kam má být prvek přemístěn.
        /// Pozor, může být null - když přesun prvku se provádí na místo, kde žádný graf není!
        /// </summary>
        public GId TargetRow { get; set; }
        /// <summary>
        /// Absolutní souřadnice prvku po přemístění
        /// </summary>
        public Rectangle BoundsFinalAbsolute { get; set; }
        /// <summary>
        /// Cílový čas prvku po přemístění
        /// </summary>
        public TimeRange TimeRangeFinal { get; set; }
        /// <summary>
        /// Obsahuje true, pokud dochází ke změně řádku
        /// </summary>
        public bool IsChangeRow { get { return (this.SourceRow != null && this.TargetRow != null && this.SourceRow != this.TargetRow); } }
        /// <summary>
        /// Obsahuje true, pokud dochází ke změně času
        /// </summary>
        public bool IsChangeTime { get { return (this.SourceTime != null && this.TimeRangeFinal != null && this.SourceTime != this.TimeRangeFinal); } }
        /// <summary>
        /// Posun času: target = source + ShiftTime.Value; ale pokud <see cref="IsChangeTime"/> je false, pak zde je null.
        /// </summary>
        public TimeSpan? ShiftTime { get { return (this.IsChangeTime ? (TimeSpan?)(this.TimeRangeFinal.Begin.Value - this.SourceTime.Begin.Value) : (TimeSpan?)null); } }
        /// <summary>
        /// Funkce vrátí čas, odpovídající dané absolutní souřadnici X.
        /// Vstupem je absolutní souřadnice X, výstupem datum a čas na dané souřadnici.
        /// </summary>
        /// <returns></returns>
        public Func<int, DateTime?> GetTimeForPosition;
        /// <summary>
        /// Funkce vrátí absolutní souřadnici X, odpovídající danému čas.
        /// Vstupem je datum a čas, výstupem absolutní souřadnice X.
        /// </summary>
        public Func<DateTime, int?> GetPositionForTime;
        /// <summary>
        /// Funkce vrátí dané datum zaokrouhlené na vhodné jednotky na aktuální časové ose
        /// </summary>
        public Func<DateTime, AxisTickType, DateTime?> GetRoundedTime;
    }
    /// <summary>
    /// Režim pohybu prvku, ovlivňuje chování při adjustování
    /// </summary>
    public enum GraphItemMoveMode
    {
        /// <summary>
        /// Není pohyb
        /// </summary>
        None,
        /// <summary>
        /// Přesouvání celého prvku
        /// </summary>
        Move,
        /// <summary>
        /// Změna velikosti pomocí přesunu Begin
        /// </summary>
        ResizeBegin,
        /// <summary>
        /// Změna velikosti pomocí přesunu End
        /// </summary>
        ResizeEnd
    }
    #endregion
}
