using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using WinForms = System.Windows.Forms;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Components.Graphs;
using Asol.Tools.WorkScheduler.Components.Grids;
using Noris.LCS.Base.WorkScheduler;
using R = Noris.LCS.Base.WorkScheduler.Resources;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// </summary>
    public class MainData : IMainDataInternal, IFunctionProvider
    {
        #region Konstrukce a privátní proměnné
        /// <summary>
        /// Protože implementuji IFunctionProvider, stává se ze mě IPlugin.
        /// A jádro systému si vytváří slovník všech pluginů - takže si vygeneruje 
        /// pro každý typ pluginu jednu instanci, aby z ní mohl číst její pluginové vlastnosti.
        /// Instance generuje pomocí <see cref="System.Activator"/> a vyžaduje k tomu bezparametrický konstruktor.
        /// Zde jej implementujeme, ale nic v něm neděláme.
        /// </summary>
        public MainData() { }
        /// <summary>
        /// Konstruktor pro konkrétního hostitele
        /// </summary>
        /// <param name="host"></param>
        public MainData(IAppHost host) : this(host, null, true) { }
        /// <summary>
        /// Konstruktor pro konkrétního hostitele a číslo Session
        /// </summary>
        /// <param name="host"></param>
        /// <param name="sessionId"></param>
        public MainData(IAppHost host, int? sessionId) : this(host, sessionId, true) { }
        /// <summary>
        /// Konstruktor pro konkrétního hostitele a číslo Session
        /// </summary>
        /// <param name="host"></param>
        /// <param name="sessionId"></param>
        /// <param name="useToolBar">Vygenerovat vlastní toolbar? V Nephrite může být false, pak si Connector musí převzít definici toolbaru/ribbonu z dat, a taky po stisku tlačítka musí volat metody pro provedení zvolené akce, typicky <see cref="RunToolBarItemClick(GuiToolbarItem)"/> a pod.</param>
        public MainData(IAppHost host, int? sessionId, bool useToolBar)
        {
            this._DataState = DataStateType.Initialising;
            this._AppHost = host;
            this._HasHost = (host != null);
            this._HasStyles = (host != null && host.HasStyles);
            this._SessionId = sessionId;
            this._UseToolBar = useToolBar;
            this.Init();

            this._DataState = DataStateType.PrepareConfig;
            using (App.Trace.Scope(TracePriority.Priority2_Lowest, "SchedulerConfig", ".ctor(null)", ""))
                this._Config = new SchedulerConfig(null);

            this._DataState = DataStateType.Initialised;
        }
        /// <summary>
        /// Datový hostitel
        /// </summary>
        private IAppHost _AppHost;
        /// <summary>
        /// Obsahuje true, pokud máme vztah na datového hostitele
        /// </summary>
        private bool _HasHost;
        /// <summary>
        /// Obsahuje true, pokud máme vztah na datového hostitele a ten podporuje styly (pak má smysl provádět Colorize)
        /// </summary>
        private bool _HasStyles;
        /// <summary>
        /// ID číslo this session pluginu. Identifikace pro volající systém (ten může mít otevřených více oken pluginů)
        /// </summary>
        private int? _SessionId;
        /// <summary>
        /// Máme generovat vizuální ToolBar?
        /// Pokud je false, pak funkci Toolbaru přebírá hostitel = generuje Ribbon.
        /// </summary>
        private bool _UseToolBar;
        /// <summary>
        /// Pořadové číslo requestu
        /// </summary>
        private int _RequestId;
        private GuiData _GuiData;
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Init()
        {
            this._DataState = DataStateType.Initialising;
            this.ClosingState = MainFormClosingState.None;
        }
        #endregion
        #region Public metody a properties: LoadData(), CreateControl(), RefreshData() - voláno zvenku (z pluginu)
        /// <summary>
        /// Načte data ze strukturovaného objektu <see cref="GuiData"/>
        /// </summary>
        /// <param name="guiData"></param>
        public void LoadData(GuiData guiData)
        {
            if (guiData == null)
                throw new GraphLibCodeException("Pro tvorbu Scheduler.MainData byl dodán objekt GuiData = null.");

            this._DataState = DataStateType.LoadingData;

            guiData.FillParents();
            this._GuiData = guiData;
            this._LoadGuiToolbar(guiData.ToolbarItems);
            this._LoadGuiPanels(guiData.Pages);
            this._LoadGuiContext(guiData.ContextMenuItems);

            this._DataState = DataStateType.DataLoaded;
        }
        /// <summary>
        /// Vytvoří a vrátí new WinForm control, obsahující kompletní strukturu pro zobrazení dodaných dat.
        /// Control rovnou vloží do dodaného Formu.
        /// Nastaví vlastnosti dodaného Formu podle dat v <see cref="GuiData.Properties"/>.
        /// </summary>
        /// <returns></returns>
        public System.Windows.Forms.Control CreateControlToForm(WinForms.Form mainForm)
        {
            using (App.Trace.Scope(TracePriority.Priority3_BellowNormal, "MainData", "ApplyPropertiesToForm", ""))
                this._ApplyPropertiesToForm(mainForm);     // Nastavíme vlastnosti formu podle GuiProperties

            this._CreateMainControl();

            mainForm.Controls.Add(this._MainControl);      // Control MainControl vložíme do formu
            this._MainControl.Dock = WinForms.DockStyle.Fill;       // Control MainControl roztáhneme na maximum

            this._DataState = DataStateType.Prepared;
            return this._MainControl;                      // hotovo!
        }
        /// <summary>
        /// Vytvoří a vrátí new WinForm control, obsahující kompletní strukturu pro zobrazení dodaných dat
        /// </summary>
        /// <returns></returns>
        public System.Windows.Forms.Control CreateControl()
        {
            this._CreateMainControl();

            this._DataState = DataStateType.Prepared;
            return this._MainControl;
        }
        /// <summary>
        /// Tato metoda je volána z vnějšího prostředí, v případě kdy je třeba akceptovat změny provedené vně controlu a promítnout je do GUI vrstvy
        /// </summary>
        /// <param name="guiResponse"></param>
        public void RefreshData(GuiResponse guiResponse)
        {
            try
            {
                this.ProcessGuiResponse(guiResponse);
            }
            catch (Exception exc)
            {
                App.Trace.Exception(exc, "RefreshData(GuiResponse) error");
            }
        }
        /// <summary>
        /// Hlavní objekt s daty <see cref="GuiData"/>
        /// </summary>
        public GuiData GuiData { get { return this._GuiData; } }
        /// <summary>
        /// Stav objektu z hlediska jeho inicializace a ukončování
        /// </summary>
        public DataStateType DataState { get { return this._DataState; } } private DataStateType _DataState = DataStateType.None;
        /// <summary>
        /// Metoda vrátí jméno ikony používané v systému klienta (pro Ribbon), anebo vytvoří nativní Image pro ikonu do out <paramref name="nativeImage"/>.
        /// <para/>
        /// Pokud v dodaném objektu je uveden přímo Image (v <see cref="GuiImage.Image"/>) anebo je dodán jeho obsah jako byte[] (v <see cref="GuiImage.ImageContent"/>),
        /// pak je <see cref="Image"/> vložen do out <paramref name="nativeImage"/> a vrácen je null.<br/>
        /// Pokud v <see cref="GuiImage.ImageFile"/> je předán název interní ikony WorkScheduleru (typicky <see cref="Noris.LCS.Base.WorkScheduler.Resources.Images.Actions.AddressBookNew2Png"/>),
        /// pak je z interního Resource vytvořen <see cref="Image"/> a ten je vložen do out <paramref name="nativeImage"/> a vrácen je null.<br/>
        /// Jinak je v out <paramref name="nativeImage"/> null, a je vrácen název ikony <see cref="GuiImage.ImageFile"/>, a je na volajícím, aby daný název našel ve svých zdrojích.
        /// </summary>
        /// <param name="guiImage"></param>
        /// <param name="nativeImage"></param>
        /// <returns></returns>
        public string GetClientImageName(GuiImage guiImage, out Image nativeImage)
        {
            nativeImage = App.ResourcesApp.GetImage(guiImage);
            return (nativeImage is null ? guiImage?.ImageFile : null);
        }
        #endregion
        #region Vytváření controlu, jeho vložení do Formu
        /// <summary>
        /// Do předaného formu vloží data z nastavení v <see cref="GuiProperties"/>
        /// </summary>
        /// <param name="mainForm"></param>
        private void _ApplyPropertiesToForm(WinForms.Form mainForm)
        {
            GuiProperties guiProperties = this._GuiData.Properties;
            mainForm.FormBorderStyle = _ConvertBorderStyle(guiProperties.PluginFormBorder);
            mainForm.WindowState = _ConvertWindowState(guiProperties.PluginFormIsMaximized);
            mainForm.Text = guiProperties.PluginFormTitle;
            mainForm.FormClosing += _MainFormClosing;
            mainForm.FormClosed += _MainFormClosed;
        }
        /// <summary>
        /// Vytvoří instanci hlavního interaktivního controlu <see cref="MainControl"/> a uloží ji do <see cref="_MainControl"/>.
        /// Do controlu vygeneruje všechny potřebné jeho controly a zaregistruje potřebné eventhandlery.
        /// </summary>
        private void _CreateMainControl()
        {
            this._DataState = DataStateType.PrepareMainControl;
            using (App.Trace.Scope(TracePriority.Priority3_BellowNormal, "MainControl", ".ctor", ""))
                this._MainControl = new MainControl(this); // Vytvoříme new control MainControl

            this._DataState = DataStateType.FillGuiControls;
            using (App.Trace.Scope(TracePriority.Priority3_BellowNormal, "MainData", "FillMainControlFromGui", ""))
                this._FillMainControlFromGui();            // Do controlu MainControl vygenerujeme všechny jeho controly

            this._DataState = DataStateType.RegisterEventHandlers;
            this._AddMainControlEventHandlers();           // A zaregistrujeme si eventhandlery.

            this._DataState = DataStateType.MainControlDone;
        }
        /// <summary>
        /// Vrátí WinForm styl borderu podle Plugin stylu.
        /// Důvod? Nechceme vždycky referencovat WinFormy kvůli <see cref="System.Windows.Forms.FormBorderStyle"/>, stačí nám naklonovaný enum <see cref="PluginFormBorderStyle"/>.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        private static WinForms.FormBorderStyle _ConvertBorderStyle(PluginFormBorderStyle style)
        {
            switch (style)
            {
                case PluginFormBorderStyle.None: return WinForms.FormBorderStyle.None;
                case PluginFormBorderStyle.FixedSingle: return WinForms.FormBorderStyle.FixedSingle;
                case PluginFormBorderStyle.Fixed3D: return WinForms.FormBorderStyle.Fixed3D;
                case PluginFormBorderStyle.FixedDialog: return WinForms.FormBorderStyle.FixedDialog;
                case PluginFormBorderStyle.Sizable: return WinForms.FormBorderStyle.Sizable;
                case PluginFormBorderStyle.FixedToolWindow: return WinForms.FormBorderStyle.FixedToolWindow;
                case PluginFormBorderStyle.SizableToolWindow: return WinForms.FormBorderStyle.SizableToolWindow;
            }
            return WinForms.FormBorderStyle.Sizable;
        }
        /// <summary>
        /// Vrátí WinForm stav okna podle nastavení Pluginu.
        /// Důvod? Nechceme vždycky referencovat WinFormy kvůli <see cref="System.Windows.Forms.FormWindowState"/>, stačí nám boolean IsMaximized.
        /// </summary>
        /// <param name="isMaximized"></param>
        /// <returns></returns>
        private static WinForms.FormWindowState _ConvertWindowState(bool isMaximized)
        {
            return (isMaximized ? WinForms.FormWindowState.Maximized : WinForms.FormWindowState.Normal);
        }
        /// <summary>
        /// Z dat dodaných v prvcích GuiItem vytvoří vizuální controly a vloží je do Main WinForm controlu
        /// </summary>
        private void _FillMainControlFromGui()
        {
            App.TryRun(this._FillMainControlToolbar);
            App.TryRun(this._FillMainControlPagesFromGui);
            App.TryRun(this._FillMainControlFromConfig);
        }
        /// <summary>
        /// Do hlavního controlu <see cref="_MainControl"/> zaregistrujeme naše eventhandlery.
        /// Volá se po úplném vytvoření a naplnění controlu podle dat z <see cref="GuiData"/>.
        /// </summary>
        private void _AddMainControlEventHandlers()
        {
            this._InteractiveMousePaintInit();
        }
        /// <summary>
        /// Reference na hlavní GUI control, který je vytvořen v metodě <see cref="CreateControlToForm(WinForms.Form)"/>
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
            request.CurrentState = this._CreateGuiCurrentState();

            int requestId = ++this._RequestId;
            bool hasCallBack = (callBackAction != null);
            bool hasBlockGui = blockGuiTime.HasValue;
            using (var scope = App.Trace.Scope("MainData", "CallAppHostFunction", "Start",
                "SessionId: " + this._SessionId.ToString(),
                "RequestId: " + requestId.ToString(),
                "Command: " + request.Command, 
                "BlockGui: " + (hasBlockGui ? blockGuiTime.Value.ToString() : "No"),
                "CallBack: " + (hasCallBack ? "Yes" : "No"),
                "Data: " + request.ToString()))
            {
                AppHostResponseArgs responseArgs = null;
                AppHostRequestArgs requestArgs = new AppHostRequestArgs(this._SessionId, requestId, request, hasBlockGui, userData, callBackAction);
                try
                {
                    if (this._VerifyAppHost(request))
                    {   // Máme-li hostitele, předáme mu požadavek:

                        // Pokud je požadavek na blokování aktuálního threadu po danou dobu, pak požádám control o nastavení bloku na GUI:
                        if (hasBlockGui)
                            this._MainControl.BlockGUI(blockGuiTime.Value, blockGuiMessage);

                        // Zajistím, že CallBack akce přijde vždy k vyřízení do zdejší metody, namísto do metody aplikační:
                        requestArgs.CallBackAction = this._CallAppHostAsyncFunctionResponse;
                        
                        // Zavoláme AppHost; předem nevíme, zda AppHost je Synchronní nebo Asynchronní:
                        responseArgs = this._AppHost.CallAppHostFunction(requestArgs);
                        // Pokud AppHost je Synchronní, tak nám rovnou vrátil Response, a vyřídíme to ihned:
                        if (responseArgs != null)
                            this._CallAppHostAsyncFunctionResponse(responseArgs);
                    }
                    else if (hasCallBack)
                    {   // Nemáme hostitele. Pokud volající očekává vyvolání callBackAction, musíme mu ho dát:
                        this._CallBackActionErrorNoHost(requestArgs);
                    }
                }
                catch (Exception exc)
                {
                    scope.AddItem("Exception: " + exc.Message);
                    App.Trace.Exception(exc, request.ToString());
                    string message = "Při zpracování požadavku " + request.Command + " došlo k chybě: " + exc.Message;
                    this.ShowDialog(message, null, GuiDialogButtons.Ok);
                }
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
            AppHostRequestArgs requestArgs = responseArgs.Request;
            bool hasBlockGui = requestArgs.BlockGui;
            using (var scope = App.Trace.Scope("MainData", "CallAppHostFunctionResponse", "Start",
                    "SessionId: " + this._SessionId.ToString(),
                    "RequestId: " + requestArgs.RequestId.ToString(),
                    "Command: " + requestArgs.Request.Command,
                    "BlockGui: " + (hasBlockGui ? "Yes" : "No"),
                    "CallBack: " + ((requestArgs.OriginalCallBackAction != null) ? "Yes" : "No")))
            {
                // Nejprve odblokuji GUI:
                if (requestArgs.BlockGui)
                    this._MainControl.ReleaseGUI();

                // Pak zavolám zpracování odpovědi, v něm může dojít k novému zablokování GUI (proto GUI odblokovávám před tím a ne až potom):
                if (requestArgs.OriginalCallBackAction != null)
                    requestArgs.OriginalCallBackAction(responseArgs);
            }
        }
        #endregion
        #region Klávesnice
        /// <summary>
        /// Provede akci po stisku klávesy
        /// </summary>
        /// <param name="action"></param>
        /// <param name="table"></param>
        /// <param name="objectFullName"></param>
        /// <param name="objectId"></param>
        private void _RunKeyAction(GuiKeyAction action, MainDataTable table, string objectFullName, GuiId objectId)
        {
            if (action == null) return;
            this._RunKeyActionCallAppHost(action, objectFullName, objectId);
        }
        /// <summary>
        /// Zavolá AppHost pro danou klávesu
        /// </summary>
        /// <param name="action"></param>
        /// <param name="objectFullName"></param>
        /// <param name="objectId"></param>
        private void _RunKeyActionCallAppHost(GuiKeyAction action, string objectFullName, GuiId objectId)
        {
            if (action.GuiActions.HasValue && action.GuiActions.Value.HasAnyFlag(GuiActionType.SuppressCallAppHost)) return;

            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_KeyPress;
            request.KeyPress = new GuiRequestKeyPress()
            {
                KeyData = action.KeyData,
                ObjectFullName = objectFullName,
                ObjectGuiId = objectId
            };
            this._CallAppHostFunction(request, this._RunKeyActionApplicationResponse, action.BlockGuiTime, action.BlockGuiMessage);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost RunKeyAction
        /// </summary>
        /// <param name="response"></param>
        private void _RunKeyActionApplicationResponse(AppHostResponseArgs response)
        {
            this.ProcessGuiResponse(response.GuiResponse);
        }
        #endregion
        #region Toolbar
        /// <summary>
        /// Vyvolá hostitel, který převzal roli Toolbaru (pomocí Ribbonu) v situaci, kdy uživatel kliknul na konkrétní prvek Ribbonu, typu Systémová akce
        /// </summary>
        /// <param name="systemAction"></param>
        public void RunToolBarItemClick(ToolbarSystemItem systemAction) { _ToolBarItemRunSystemAction(systemAction); }
        /// <summary>
        /// Vyvolá hostitel, který převzal roli Toolbaru (pomocí Ribbonu) v situaci, kdy uživatel kliknul na konkrétní prvek Ribbonu, typu Aplikační button
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        public void RunToolBarItemClick(GuiToolbarItem guiToolbarItem) { _ToolBarItemRunApplicationAction(guiToolbarItem, null); }
        /// <summary>
        /// Vyvolá hostitel, který převzal roli Toolbaru (pomocí Ribbonu) v situaci, kdy uživatel změnil stav Checked na konkrétním prvku Ribbonu, typu Systémová akce
        /// </summary>
        /// <param name="systemAction"></param>
        /// <param name="isChecked"></param>
        public void RunToolBarSelectedChange(ToolbarSystemItem systemAction, bool isChecked) { _ToolBarItemSelectedChangeSystem(systemAction, isChecked); }
        /// <summary>
        /// Vyvolá hostitel, který převzal roli Toolbaru (pomocí Ribbonu) v situaci, kdy uživatel změnil stav Checked na konkrétním prvku Ribbonu, typu Aplikační button
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        public void RunToolBarSelectedChange(GuiToolbarItem guiToolbarItem) { _ToolBarItemSelectedChangeApplication(guiToolbarItem); }
        /// <summary>
        /// Vyvolá hostitel, který převzal roli Toolbaru (pomocí Ribbonu) v situaci, kdy chce uložit nastavení configu GUI
        /// </summary>
        public void RunToolBarSaveToConfig() { this._SaveMainControlToConfig(); }
        /// <summary>
        /// Načte položky do Toolbaru z dodaných dat Gui
        /// </summary>
        /// <param name="guiToolbarPanel"></param>
        private void _LoadGuiToolbar(GuiToolbarPanel guiToolbarPanel)
        {
            this._GuiToolBarPanel = guiToolbarPanel;
        }
        /// <summary>
        /// Data o ToolBaru z Gui
        /// </summary>
        private GuiToolbarPanel _GuiToolBarPanel;
        /// <summary>
        /// Všechny položky v Toolbaru / Ribbonu.
        /// Pochází buď z <see cref="GuiToolbarPanel.RibbonPages"/>, pokud není null a je povoleno v <see cref="GuiToolbarPanel.UseSystemRibbon"/>;
        /// anebo z <see cref="GuiToolbarPanel.Items"/>.
        /// Anebo je null.
        /// </summary>
        private GuiToolbarItem[] _GuiToolBarItems
        {
            get
            {
                var panel = _GuiToolBarPanel;
                if (panel is null) return null;
                bool? useSystemRibbon = panel.UseSystemRibbon;
                if ((!useSystemRibbon.HasValue || (useSystemRibbon.HasValue && useSystemRibbon.Value)) && panel.RibbonPages != null)
                    return panel.RibbonPages
                        .Where(p => p.Groups != null).SelectMany(p => p.Groups)
                        .Where(g => g.Items != null).SelectMany(g => g.Items)
                        .ToArray();
                if (panel.Items != null)
                    return panel.Items.ToArray();
                return null;
            }
        }
        /// <summary>
        /// Grupa v ToolBaru, obsahující systémové položky pro časovou osu
        /// </summary>
        private FunctionGlobalGroup _ToolbarSystemTimeAxisGroup;
        /// <summary>
        /// Grupa v ToolBaru, obsahující systémové položky pro časovou osu
        /// </summary>
        private FunctionGlobalGroup _ToolbarSystemSettingsGroup;
        /// <summary>
        /// Z dat v <see cref="_GuiToolBarPanel"/> naplní toolbar
        /// </summary>
        private void _FillMainControlToolbar()
        {
            using (App.Trace.Scope(TracePriority.Priority3_BellowNormal, "MainData", "FillMainControlToolbar", ""))
            {
                this._MainControl.ToolBarItemClicked -= _ToolBarItemClicked;
                this._MainControl.ToolBarItemSelectedChange -= _ToolBarItemSelectedChange;
                this._MainControl.ToolBarStatusChanged -= _ToolBarStatusChanged;

                this._ToolBarGuiItems = new List<ToolBarItem>();
                this._MainControl.ClearToolBar();
                if (this._UseToolBar)
                {
                    this._MainControl.ToolBarVisible = this._GuiToolBarPanel.ToolbarVisible;
                    this._FillMainControlToolbarFromSystem();
                    this._FillMainControlToolbarFromGui();
                    this._MainControl.ToolBarItemClicked += _ToolBarItemClicked;
                    this._MainControl.ToolBarItemSelectedChange += _ToolBarItemSelectedChange;
                    this._MainControl.ToolBarStatusChanged += _ToolBarStatusChanged;
                }
                else
                {   // Tohle typicky znamená, že funkci Toolbaru převzal externí Ribbon:
                    this._MainControl.ToolBarVisible = false;
                    // Převzít prvky, které mají akci MousePaint:
                    var guiToolBarItems = this._GuiToolBarItems;
                    if (guiToolBarItems != null)
                    {
                        foreach (var guiToolBarItem in guiToolBarItems)
                            this._MousePaintAddToolBarItem(guiToolBarItem, null);
                    }
                }
            }
        }
        /// <summary>
        /// Do toolbaru vloží systémové funkce
        /// </summary>
        private void _FillMainControlToolbarFromSystem()
        {
            if (!this._GuiToolBarPanel.ToolbarVisible) return;
            // Systémové položky Toolbaru jsou položky třídy FunctionGlobalItem, nemají v sobě instanci GuiToolbarItem.

            this._ToolbarSystemTimeAxisGroup = new FunctionGlobalGroup() { Title = "ČASOVÁ OSA", ToolTipTitle = "Posuny časové osy, změna měřítka", Order = "A1" };
            this._ToolbarSystemSettingsGroup = new FunctionGlobalGroup() { Title = "SYSTEM", ToolTipTitle = "Systémové funkce", Order = "A2" };

            this._TimeAxisToolBarInit();
            this._SystemSettingsToolBarInit();

            if (this._ToolbarSystemTimeAxisGroup.Items.Count > 0)
                this._MainControl.AddToolBarGroup(this._ToolbarSystemTimeAxisGroup);
            if (this._ToolbarSystemSettingsGroup.Items.Count > 0)
                this._MainControl.AddToolBarGroup(this._ToolbarSystemSettingsGroup);
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
        /// <param name="isPersistEnabled"></param>
        /// <param name="selectionGroupName"></param>
        /// <param name="systemItemId"></param>
        /// <returns></returns>
        private FunctionGlobalItem _CreateToolbarItem(string name, string image, string text, string toolTip,
            FunctionGlobalItemSize size = FunctionGlobalItemSize.Half, string imageHot = null, FunctionGlobalItemType itemType = FunctionGlobalItemType.Button,
            LayoutHint layoutHint = LayoutHint.Default, int? moduleWidth = null,
            bool isSelectable = false, bool isSelected = false, bool isPersistEnabled = false, string selectionGroupName = null, ToolbarSystemItem? systemItemId = null)
        {
            FunctionGlobalItem functionItem = new FunctionGlobalItem(this);
            functionItem.Name = name;
            functionItem.Image = Application.App.ResourcesApp.GetImage(image);
            functionItem.Text = text;
            functionItem.ToolTip = toolTip;
            functionItem.Size = size;
            functionItem.ImageHot = Application.App.ResourcesApp.GetImage(imageHot);
            functionItem.ItemType = itemType;
            functionItem.LayoutHint = layoutHint;
            functionItem.ModuleWidth = moduleWidth;
            functionItem.IsCheckable = isSelectable;
            functionItem.IsChecked = isSelected;
            functionItem.PersistEnabled = isPersistEnabled;
            functionItem.CheckedGroupName = selectionGroupName;
            functionItem.UserData = systemItemId;

            // Přidat do Dictionary:
            if (systemItemId.HasValue)
            {
                if (this._ToolBarSysButtonDict == null)
                    this._ToolBarSysButtonDict = new Dictionary<ToolbarSystemItem, FunctionGlobalItem>();
                this._ToolBarSysButtonDict.AddOnce(systemItemId.Value, functionItem);
            }

            return functionItem;
        }
        /// <summary>
        /// Metoda vrátí první existující <see cref="FunctionGlobalItem"/> pro daný soupis klíčů <see cref="ToolbarSystemItem"/>. Může vrátit null.
        /// </summary>
        /// <param name="systemItemIds"></param>
        /// <returns></returns>
        private FunctionGlobalItem _SearchFirstToolBarItem(params ToolbarSystemItem[] systemItemIds)
        {
            FunctionGlobalItem result = null;
            if (this._ToolBarSysButtonDict != null && systemItemIds != null)
            {
                foreach (ToolbarSystemItem systemItemId in systemItemIds)
                {
                    if (this._ToolBarSysButtonDict.TryGetValue(systemItemId, out result)) break;
                }
            }
            return result;
        }
        /// <summary>
        /// Dictionary obsahující systémové prvky Toolbaru
        /// </summary>
        private Dictionary<ToolbarSystemItem, FunctionGlobalItem> _ToolBarSysButtonDict;
        /// <summary>
        /// Do toolbaru vloží aplikační funkce
        /// </summary>
        private void _FillMainControlToolbarFromGui()
        {
            if (!this._GuiToolBarPanel.ToolbarVisible) return;
            if (this._GuiToolBarPanel.Items == null) return;
            // Aplikační položky Toolbaru jsou položky třídy ToolBarItem, mají v sobě instanci GuiToolbarItem:

            // Nejprve sestavíme jednotlivé grupy pro prvky, podle názvu grup kam chtějí tyto prvky jít:
            Dictionary<string, FunctionGlobalGroup> toolBarGroups = new Dictionary<string, FunctionGlobalGroup>();
            string defaultGroupName = "FUNKCE";
            foreach (GuiToolbarItem guiToolBarItem in this._GuiToolBarPanel.Items)
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
                    this._MousePaintAddToolBarItem(guiToolBarItem, item);
                    this._AddControl(guiToolBarItem.FullName, item);
                }
            }

            // Výsledky (jednotlivé grupy, kde každá obsahuje sadu prvků = buttonů) vložím do předaného pole:
            if (toolBarGroups.Count > 0)
                this._MainControl.AddToolBarGroups(toolBarGroups.Values);
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
                _ToolBarItemRunApplicationAction(guiToolbarItem, args.Item);
            else if (args.Item?.UserData is ToolbarSystemItem action)           // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě = systémová akce
                _ToolBarItemRunSystemAction(action);
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
                _ToolBarItemSelectedChangeApplication(guiToolbarItem);
            else if (args.Item?.UserData is ToolbarSystemItem action)           // V UserData je uložena hodnota ToolbarSystemItem, odpovídající konkrétní funkcionalitě = systémová akce
                _ToolBarItemSelectedChangeSystem(action, args.Item.IsChecked);
        }
        /// <summary>
        /// Obsluha události StatusChanged na ToolBaru (něco bylo změněno, něco co by mělo být uchováno v konfiguraci)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ToolBarStatusChanged(object sender, GPropertyEventArgs<string> e)
        {
            this._SaveMainControlToConfig(e.Value);
        }
        /// <summary>
        /// Obsluha události ItemSelectedChange na Aplikační položce ToolBaru.
        /// Tuto akci zatím do aplikační funkce NEPŘEDÁVÁME. Neřešíme tedy ani její Response.
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        private void _ToolBarItemSelectedChangeApplication(GuiToolbarItem guiToolbarItem)
        {
            /* Tuto akci zatím do aplikační funkce NEPŘEDÁVÁME. Neřešíme tedy ani její Response. */

            // Můžeme řešit její vliv na MousePaint:
            this._MousePaintToolBarSelectedChange(guiToolbarItem);
        }
        /// <summary>
        /// Obsluha události ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="guiToolbarItem">Datová (GUI) položka ToolBaru</param>
        /// <param name="toolBarItem">Aktuální prvek z GUI, převezme se jeho hodnota <see cref="FunctionItem.IsChecked"/>. Může být null, pak platí hodnota z dodaného <see cref="GuiToolbarItem.IsChecked"/>.</param>
        private void _ToolBarItemRunApplicationAction(GuiToolbarItem guiToolbarItem, FunctionItem toolBarItem)
        {
            bool callAppHost = this._ToolBarItemRunApplicationActionInternal(guiToolbarItem);
            if (callAppHost)
                this._ToolBarItemRunApplicationActionAppHost(guiToolbarItem, toolBarItem);
        }
        /// <summary>
        /// Metoda detekuje GUI akce, které jsou předepsané v tlačítku Toolbaru <see cref="GuiToolbarItem.GuiActions"/>, a provede je.
        /// Vrací true, pokud má být volána servisní funkci v AppHost (tj. když toto volání NENÍ potlačeno příznakem <see cref="GuiActionType.SuppressCallAppHost"/>).
        /// </summary>
        /// <param name="guiToolbarItem">Datová (GUI) položka ToolBaru</param>
        /// <returns></returns>
        private bool _ToolBarItemRunApplicationActionInternal(GuiToolbarItem guiToolbarItem)
        {
            if (guiToolbarItem == null ||!guiToolbarItem.GuiActions.HasValue) return true;    // Žádná akce = žádné potlačení, vracím true
            GuiActionType guiActions = guiToolbarItem.GuiActions.Value;
            this.RunGuiInteractions(guiToolbarItem, guiActions);
            bool suppressCallAppHost = guiActions.HasFlag(GuiActionType.SuppressCallAppHost);
            return !suppressCallAppHost;
        }
        /// <summary>
        /// Metoda zavolá servisní funkci v AppHost, s commandem <see cref="GuiRequest.COMMAND_ToolbarClick"/>, a předá tam button, na nějž bylo kliknuto
        /// </summary>
        /// <param name="guiToolbarItem">Datová (GUI) položka ToolBaru</param>
        /// <param name="toolBarItem">Aktuální prvek z GUI, převezme se jeho hodnota <see cref="FunctionItem.IsChecked"/>. Může být null, pak platí hodnota z dodaného <see cref="GuiToolbarItem.IsChecked"/>.</param>
        private void _ToolBarItemRunApplicationActionAppHost(GuiToolbarItem guiToolbarItem, FunctionItem toolBarItem = null)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_ToolbarClick;
            if (toolBarItem != null) guiToolbarItem.IsChecked = toolBarItem.IsChecked;        // Přenesu příznak "Označen" (je to nutné?)
            request.ToolbarItem = guiToolbarItem;
            this._CallAppHostFunction(request, this._ToolBarItemClickApplicationResponse, guiToolbarItem.BlockGuiTime, guiToolbarItem.BlockGuiMessage);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="response"></param>
        private void _ToolBarItemClickApplicationResponse(AppHostResponseArgs response)
        {
            this.ProcessGuiResponse(response.GuiResponse);
        }
        /// <summary>
        /// Obsluha události ItemSelectedChange na Systémové položce ToolBaru
        /// </summary>
        /// <param name="action">Systémová akce</param>
        /// <param name="isSelected">Hodnota IsSelected (=IsChecked)</param>
        private void _ToolBarItemSelectedChangeSystem(ToolbarSystemItem action, bool isSelected)
        {
            this._TimeAxisToolBarSelected(action, isSelected);
        }
        /// <summary>
        /// Provede danou systémovou akci
        /// </summary>
        /// <param name="action"></param>
        private void _ToolBarItemRunSystemAction(ToolbarSystemItem action)
        {
            this._TimeAxisToolBarAction(action);
            this._SystemSettingsToolBarAction(action);
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
            ToolBarRefreshMode refreshMode = ToolBarRefreshMode.None;
            foreach (GuiToolbarItem guiToolbarItem in toolbarItems)
            {
                ToolBarItem toolBarItem = _ToolBarGuiItems.FirstOrDefault(t => String.Equals(t.Name, guiToolbarItem.Name, StringComparison.InvariantCulture));
                if (toolBarItem == null) continue;
                ToolBarRefreshMode itemMode = toolBarItem.RefreshFrom(guiToolbarItem);
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)itemMode);
            }
            this._MainControl.RefreshToolBar(refreshMode);
        }
        /// <summary>
        /// Soupis aplikačních položek Toolbaru (tj. položky, vytvářené z dat <see cref="GuiToolbarItem"/> v metodě <see cref="_FillMainControlToolbarFromGui()"/>, 
        /// při prvotním načítání a tvorbě celého controlu.
        /// </summary>
        private List<ToolBarItem> _ToolBarGuiItems;
        /// <summary>
        /// ToolBarItem : adapter mezi <see cref="GuiToolbarItem"/>, a položkou menu <see cref="FunctionGlobalItem"/>.
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
                this.ItemType = guiToolBarItem.ItemType ?? FunctionGlobalItemType.Button;
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
            public ToolBarRefreshMode RefreshFrom(GuiToolbarItem guiToolbarItem)
            {
                ToolBarRefreshMode refreshMode = ToolBarRefreshMode.None;
                GuiToolbarItem refrData = guiToolbarItem;
                if (refrData == null) return refreshMode;
                GuiToolbarItem currData = this._GuiToolBarItem;
                if (currData == null) return refreshMode;

                if (_Changed(currData.Title, refrData.Title, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Title = refrData.Title;
                if (_Changed(currData.ToolTip, refrData.ToolTip, ToolBarRefreshMode.None, ref refreshMode))
                    currData.ToolTip = refrData.ToolTip;
                if (_Changed(currData.Image, refrData.Image, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Image = refrData.Image;
                if (_Changed(currData.ImageHot, refrData.ImageHot, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.ImageHot = refrData.ImageHot;
                if (_Changed(currData.Visible, refrData.Visible, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Visible = refrData.Visible;
                if (_Changed(currData.Enable, refrData.Enable, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.Enable = refrData.Enable;
                if (_Changed(currData.IsCheckable, refrData.IsCheckable, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.IsCheckable = refrData.IsCheckable;
                if (_Changed(currData.IsChecked, refrData.IsChecked, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.IsChecked = refrData.IsChecked;
                if (_Changed(currData.CheckedGroupName, refrData.CheckedGroupName, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.CheckedGroupName = refrData.CheckedGroupName;
                if (_Changed(currData.ImageChecked, refrData.ImageChecked, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.ImageChecked = refrData.ImageChecked;
                if (_Changed(currData.Size, refrData.Size, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.Size = refrData.Size;
                if (_Changed(currData.ModuleWidth, refrData.ModuleWidth, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.ModuleWidth = refrData.ModuleWidth;
                if (_Changed(currData.LayoutHint, refrData.LayoutHint, ToolBarRefreshMode.RefreshLayout, ref refreshMode))
                    currData.LayoutHint = refrData.LayoutHint;
                if (_Changed(currData.StoreValueToConfig, refrData.StoreValueToConfig, ToolBarRefreshMode.None, ref refreshMode))
                    currData.StoreValueToConfig = refrData.StoreValueToConfig;
                if (_Changed(currData.BlockGuiTime, refrData.BlockGuiTime, ToolBarRefreshMode.None, ref refreshMode))
                    currData.BlockGuiTime = refrData.BlockGuiTime;
                if (_Changed(currData.BlockGuiMessage, refrData.BlockGuiMessage, ToolBarRefreshMode.None, ref refreshMode))
                    currData.BlockGuiMessage = refrData.BlockGuiMessage;
                if (_Changed(currData.GuiActions, refrData.GuiActions, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.GuiActions = refrData.GuiActions;
                if (_Changed(currData.RunInteractionNames, refrData.RunInteractionNames, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.RunInteractionNames = refrData.RunInteractionNames;
                if (_Changed(currData.RunInteractionSource, refrData.RunInteractionSource, ToolBarRefreshMode.RefreshControl, ref refreshMode))
                    currData.RunInteractionSource = refrData.RunInteractionSource;

                return refreshMode;
            }
            private static bool _Changed(int? oldValue, int? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(string oldValue, string newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (newValue == null) return false;                  // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (String.Equals(oldValue, newValue, StringComparison.InvariantCulture)) return false;
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(bool? oldValue, bool? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                if (oldValue == newValue) return false;
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(FunctionGlobalItemSize? oldValue, FunctionGlobalItemSize? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                if (oldValue == newValue) return false;
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(LayoutHint? oldValue, LayoutHint? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(GuiActionType? oldValue, GuiActionType? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(SourceActionType? oldValue, SourceActionType? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(TimeSpan? oldValue, TimeSpan? newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (!newValue.HasValue) return false;                // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                if (oldValue.HasValue && oldValue.Value == newValue.Value) return false;         // Pokud oldValue má hodnotu, a hodnota je beze změny, pak nejde o změnu.
                // Jde o změnu (buď z null na not null, anebo změnu hodnoty):
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                return true;
            }
            private static bool _Changed(GuiImage oldValue, GuiImage newValue, ToolBarRefreshMode result, ref ToolBarRefreshMode refreshMode)
            {
                if (newValue == null) return false;                  // Pokud newValue není zadáno, pak nejde o změnu (jen nebyl nový údaj vepsán).
                bool on = (oldValue == null);
                bool nn = (newValue == null);
                if (on && nn)
                    // Oba jsou null => to není změna:
                    return false;
                if (on)
                {   // Pouze oldValue je null => to je změna (protože newValue není null):
                    refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
                    return true;
                }
                // Oba nejsou null, podíváme se dovnitř:
                if (oldValue.ImageFile != null || newValue.ImageFile != null)
                {   // Některý Image je definován názvem souboru => porovnáme stringy:
                    return _Changed(oldValue.ImageFile, newValue.ImageFile, result, ref refreshMode);
                }
                // Dál do hloubky nejdeme, vrátíme true = jde o změnu:
                refreshMode = (ToolBarRefreshMode)((int)refreshMode | (int)result);
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
            public override Image Image { get { return (this._HasItem ? App.ResourcesApp.GetImage(this._GuiToolBarItem.Image) : null); } }
            /// <summary>
            /// Obrázek pro stav MouseActive
            /// </summary>
            public override Image ImageHot { get { return (this._HasItem ? App.ResourcesApp.GetImage(this._GuiToolBarItem.ImageHot) : null); } }
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
            /// Obsahuje true, pokud tento prvek je aktivní (jeho zaškrtávátko je zaškrtnuté)
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
            /// <summary>
            /// Příznak, zda tento prvek bude persistovat svoji hodnotu do uživatelské konfigurace.
            /// Při příštím startu aplikace bude tato hodnota načtena z konfigurace a vložena do prvku.
            /// Defaultní hodnota je false.
            /// </summary>
            public override bool PersistEnabled { get { return (this._HasItem ? (this._GuiToolBarItem.StoreValueToConfig.HasValue ? this._GuiToolBarItem.StoreValueToConfig.Value : true) : base.IsChecked); } set { if (this._HasItem) this._GuiToolBarItem.StoreValueToConfig = value; else base.PersistEnabled = value; } }
            #endregion
        }
        #endregion
        #region Config
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        public SchedulerConfig Config { get { return this._Config; } }
        /// <summary>
        /// Načte z configu hodnoty odpovídající uživatelskému stavu GUI a promítne je do něj.
        /// Tedy, jak si uživatel naposledy uspořádal GUI (a jak bylo uloženo v metodě <see cref="_SaveMainControlToConfig(string)"/>), 
        /// tak se GUI nyní po spuštění uspořádá.
        /// Tato metoda neřeší vnitřní rozložení jednotlivých panelů s daty, to si řeší panely samy ve své režii, viz metoda: <see cref="SchedulerPanel.ConnectConfigLayout(GuiPage)"/>
        /// </summary>
        private void _FillMainControlFromConfig()
        {
            using (App.Trace.Scope(TracePriority.Priority3_BellowNormal, "MainData", "FillMainControlFromConfig", ""))
            {
                SchedulerConfig config = this.Config;
                if (config == null) return;

                SchedulerConfigUserPair toolBarStatusPair = config.UserConfigSearch<SchedulerConfigUserPair>(p => p.Key == _CONFIG_TOOLBAR_STATUS).FirstOrDefault();
                if (toolBarStatusPair != null && toolBarStatusPair.Value != null && toolBarStatusPair.Value is string)
                    this._MainControl.ToolBarCurrentStatus = toolBarStatusPair.Value as string;     // Tady může dojít ke změně aktuální časové osy

                this._PrepareDefaultTimeZoom();  // Tady může dojít ke změně aktuální časové osy

                this._ApplyInitialTimeRange();
            }
        }
        /// <summary>
        /// Do časové osy vloží výchozí časový interval - podle dodaných dat GUI anebo podle poslední známé konfigurace
        /// </summary>
        private void _ApplyInitialTimeRange()
        {
            this._MainControl.SynchronizedTime.Value = this._GetInitialTimeRange();
        }
        /// <summary>
        /// Aktuální hodnota času na časové ose, uložená v konfiguraci.
        /// Může obsahovat null.
        /// </summary>
        private GuiTimeRange ConfigTimeAxisValue
        {
            get
            {
                SchedulerConfig config = this.Config;
                if (config == null) return null;

                SchedulerConfigUserPair timeRangePair = config.UserConfigSearch<SchedulerConfigUserPair>(p => p.Key == _CONFIG_TIMEAXIS_VALUE).FirstOrDefault();
                if (timeRangePair == null || timeRangePair.Value == null) return null;
                return timeRangePair.Value as GuiTimeRange;
            }
            set
            {
                SchedulerConfig config = this.Config;
                if (config == null) return;

                if (this.DataState == DataStateType.Prepared)
                {
                    SchedulerConfigUserPair timeRangePair = new SchedulerConfigUserPair(_CONFIG_TIMEAXIS_VALUE, value);
                    config.UserConfigStore<SchedulerConfigUserPair>(timeRangePair, p => p.Key == _CONFIG_TIMEAXIS_VALUE);
                    config.Save(TimeSpan.FromSeconds(10d));
                }
            }
        }
        /// <summary>
        /// Metoda uloží do Configu údaje o uživatelském stavu GUI, volá se po každé jeho změně.
        /// Metoda posbírá nejrůznější data z Main controlu a vepíše je do Configu. 
        /// Tato data budou při příštím spuštění Pluginu načtena z Configu a promítnuta do GUI v metodě <see cref="_FillMainControlFromConfig()"/>.
        /// </summary>
        private void _SaveMainControlToConfig()
        {
            string currentStatus = this._MainControl.ToolBarCurrentStatus;
            this._SaveMainControlToConfig(currentStatus);
        }
        /// <summary>
        /// Metoda uloží do Configu údaje o uživatelském stavu GUI, volá se po každé jeho změně.
        /// Metoda posbírá nejrůznější data z Main controlu a vepíše je do Configu. 
        /// Tato data budou při příštím spuštění Pluginu načtena z Configu a promítnuta do GUI v metodě <see cref="_FillMainControlFromConfig()"/>.
        /// </summary>
        private void _SaveMainControlToConfig(string currentStatus)
        {
            SchedulerConfig config = this.Config;
            if (config == null) return;

            SchedulerConfigUserPair toolBarStatusPair = new SchedulerConfigUserPair(_CONFIG_TOOLBAR_STATUS, currentStatus);
            config.UserConfigStore<SchedulerConfigUserPair>(toolBarStatusPair, p => p.Key == toolBarStatusPair.Key);
            
            config.Save();
        }
        private const string _CONFIG_MAIN_CONTROL = "ConfigMainControl";
        private const string _CONFIG_TOOLBAR_STATUS = "ConfigToolBarStatus";
        private const string _CONFIG_TIMEAXIS_VALUE = "ConfigTimeAxisValue";
        /// <summary>
        /// Konfigurace uživatelská
        /// </summary>
        private SchedulerConfig _Config;
        #endregion
        #region Interakce (GuiActions) - provádění interakcí GUI, podle deklarace v toolbaru nebo kdekoliv jinde
        /// <summary>
        /// Metoda detekuje interakce požadované v dodaném prvku toolbaru, a provede je.
        /// </summary>
        /// <param name="guiToolbarItem">Datový prvek Toolbaru</param>
        /// <param name="guiActions">Akce</param>
        internal void RunGuiInteractions(GuiToolbarItem guiToolbarItem, GuiActionType guiActions)
        {
            bool callRefresh = false;

            if (guiActions.HasFlag(GuiActionType.ResetAllRowFilters))
                this._RunGuiInteractionsResetAllRowFilters(ref callRefresh);

            if (guiActions.HasFlag(GuiActionType.ResetTargetInteractiveFilters))
                this._RunGuiInteractionsResetTargetInteractiveFilters(ref callRefresh);

            if (guiActions.HasFlag(GuiActionType.RunInteractions))
                this._RunGuiInteractionsByName(SourceActionType.ToolbarClicked | (guiToolbarItem.RunInteractionSource.HasValue ? guiToolbarItem.RunInteractionSource.Value : SourceActionType.None), guiToolbarItem.RunInteractionNames, ref callRefresh);

            if (guiActions.HasFlag(GuiActionType.SetVisibleForControl))
                this._RunGuiInteractionsSetVisibleForControl(guiToolbarItem, ref callRefresh);

            if (callRefresh)
                this._MainControl.Refresh();
        }
        /// <summary>
        /// Metoda detekuje interakce podle filtru akcí, a provede je.
        /// </summary>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="targetActions"></param>
        internal void RunGuiInteractions(SourceActionType currentSourceAction, TargetActionType? targetActions)
        {
            bool callRefresh = false;

            this._RunGuiInteractionsByActions(currentSourceAction, targetActions, ref callRefresh);

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
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="interactionNames">Názvy interakcí včetně názvu tabulky, sada párů ve formátu: "GridName1:InteractionNameA;GridName2:InteractionNameB;..."</param>
        /// <param name="callRefresh">Nastavit na true v případě potřeby Refreshe</param>
        private void _RunGuiInteractionsByName(SourceActionType currentSourceAction, string interactionNames, ref bool callRefresh)
        {
            var runInteractions = this._GetTableInteractions(currentSourceAction, interactionNames);
            if (runInteractions == null) return;

            foreach (var runInteraction in runInteractions)
                // MainDataTable . RunInteractionThisSource( GridInteractionRunInfo[] ):
                runInteraction.Item1.RunInteractionThisSource(runInteraction.Item2.ToArray(), ref callRefresh);
        }
        /// <summary>
        /// Provede znovu aktuálně platné interakce všech tabulek, vyhovující danému filtru akcí SourceActionType a TargetActionType
        /// </summary>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="targetActions">Hodnoty TargetAction: když daná interakce bude mít alespoň jeden Flag, bude zařazena do výstupu</param>
        /// <param name="callRefresh">Nastavit na true v případě potřeby Refreshe</param>
        private void _RunGuiInteractionsByActions(SourceActionType currentSourceAction, TargetActionType? targetActions, ref bool callRefresh)
        {
            var interactions = this._GetTableInteractions(currentSourceAction, targetActions);
            if (interactions == null) return;

            foreach (var interaction in interactions)
                // MainDataTable . RunInteractionThisSource( GridInteractionRunInfo[] ):
                interaction.Item1.RunInteractionThisSource(interaction.Item2.ToArray(), ref callRefresh);
        }
        /// <summary>
        /// Metoda vrací soupis tabulek a jejich interakcí, které se mají provést při kliknutí na tlačítko, které má zadané interakce v <see cref="GuiToolbarItem.RunInteractionNames"/>.
        /// Na vstupu je formát: "GridName1:InteractionNameA;GridName2:InteractionNameB;..."
        /// Výstupem této metody je pole obsahující páry: Tabulka + všechny její Interakce.
        /// </summary>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="interactionNames">Názvy interakcí včetně názvu tabulky, sada párů ve formátu: "GridName1:InteractionNameA;GridName2:InteractionNameB;..."</param>
        /// <returns></returns>
        private Tuple<MainDataTable, List<GridInteractionRunInfo>>[] _GetTableInteractions(SourceActionType currentSourceAction, string interactionNames)
        {
            if (String.IsNullOrEmpty(interactionNames)) return null;

            Dictionary<string, Tuple<MainDataTable, List<GridInteractionRunInfo>>> result = new Dictionary<string, Tuple<MainDataTable, List<GridInteractionRunInfo>>>();

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

                Tuple<MainDataTable, List<GridInteractionRunInfo>> tableList;
                if (!result.TryGetValue(tableName, out tableList))
                {
                    tableList = new Tuple<MainDataTable, List<GridInteractionRunInfo>>(dataTable, new List<GridInteractionRunInfo>());
                    result.Add(tableName, tableList);
                }

                string[] runParameters = null;
                int runLength = items.Length - 2;
                if (runLength > 0)
                {
                    runParameters = new string[runLength];
                    Array.Copy(items, 2, runParameters , 0, runLength);
                }

                GridInteractionRunInfo runInteraction = new GridInteractionRunInfo(interaction, currentSourceAction, runParameters);
                tableList.Item2.Add(runInteraction);
            }

            return result.Values.ToArray();
        }
        /// <summary>
        /// Metoda vrací soupis tabulek a jejich interakcí, které vyhovují danému zadání.
        /// Výstupem této metody je pole obsahující páry: Tabulka + všechny její Interakce.
        /// </summary>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="targetActions"></param>
        /// <returns></returns>
        private Tuple<MainDataTable, List<GridInteractionRunInfo>>[] _GetTableInteractions(SourceActionType currentSourceAction, TargetActionType? targetActions)
        {
            List<Tuple<MainDataTable, List<GridInteractionRunInfo>>> tableInterList = new List<Tuple<MainDataTable, List<GridInteractionRunInfo>>>();

            foreach (MainDataTable dataTable in this._DataTables)
            {
                List<GuiGridInteraction> interactions = _GetFilteredInteractions(dataTable.Interactions, currentSourceAction, targetActions);
                if (interactions != null && interactions.Count > 0)
                {
                    var runInteractions = interactions.Select(i => new GridInteractionRunInfo(i, currentSourceAction, null)).ToList();
                    tableInterList.Add(new Tuple<MainDataTable, List<GridInteractionRunInfo>>(dataTable, runInteractions));
                }
            }

            return tableInterList.ToArray();
        }
        /// <summary>
        /// Metoda filtruje danou sadu interakcí.
        /// </summary>
        /// <param name="interactions">Sada interakcí</param>
        /// <param name="currentSourceAction">Aktuální akce, která spustila interakce</param>
        /// <param name="targetActions">Hodnoty TargetAction: když daná interakce bude mít alespoň jeden Flag, bude zařazena do výstupu</param>
        /// <returns></returns>
        protected List<GuiGridInteraction> _GetFilteredInteractions(IEnumerable<GuiGridInteraction> interactions, SourceActionType currentSourceAction, TargetActionType? targetActions)
        {
            if (interactions == null) return null;
            bool filterTarget = (targetActions.HasValue);  // Filtr na TargetAction se provede jen pokud je na vstupu zadán
            return interactions
                .Where(i =>
                    {
                        if ((i.SourceAction & currentSourceAction) == 0) return false;
                        if (filterTarget && ((i.TargetAction & targetActions.Value) == 0)) return false;
                        return true;
                    }
                      )
                .ToList();
        }
        /// <summary>
        /// Zajistí akci GUI: <see cref="GuiActionType.SetVisibleForControl"/> pro daný prvek Toolbaru
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        /// <param name="callRefresh"></param>
        protected void _RunGuiInteractionsSetVisibleForControl(GuiToolbarItem guiToolbarItem, ref bool callRefresh)
        {
            var targetNames = GetTargetNames(guiToolbarItem);
            foreach (var target in targetNames)
                this._SetVisibleForControl(target.Item1, target.Item2, ref callRefresh);
        }
        /// <summary>
        /// Metoda z dodaného prvku Toolbaru <paramref name="guiToolbarItem"/> načte jeho 
        /// <see cref="GuiToolbarItem.ActionTargetNames"/> a rozdělí jej na jednotlivá jména (oddělená čárkou nebo středníkem).
        /// Zjistí stav <see cref="GuiToolbarItem.IsChecked"/>.
        /// Sestaví a vrátí pole párů: jméno + aktivita.
        /// Pokud před jednotlivým jménem je vykřičník, obrátí stav aktivity pro dané jméno cíle.
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        /// <returns></returns>
        protected List<Tuple<string, bool>> GetTargetNames(GuiToolbarItem guiToolbarItem)
        {
            if (guiToolbarItem == null) return null;
            string actionTargetNames = guiToolbarItem.ActionTargetNames;
            if (String.IsNullOrEmpty(actionTargetNames)) return null;
            bool isActive = (guiToolbarItem.IsCheckable.HasValue && guiToolbarItem.IsChecked.HasValue ? guiToolbarItem.IsChecked.Value : true);
            return this.GetTargetNames(actionTargetNames, isActive);
        }
        /// <summary>
        /// Metoda z dodaného textu <paramref name="actionTargetNames"/> (= <see cref="GuiToolbarItem.ActionTargetNames"/>) a dodané hodnoty aktivity <paramref name="isActive"/> 
        /// načte a rozdělí text na jednotlivá jména (oddělená čárkou nebo středníkem).
        /// Sestaví a vrátí pole párů: jméno + aktivita.
        /// Pokud před jednotlivým jménem je vykřičník, obrátí stav aktivity pro dané jméno cíle.
        /// </summary>
        /// <param name="actionTargetNames"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        protected List<Tuple<string, bool>> GetTargetNames(string actionTargetNames, bool isActive)
        {
            if (String.IsNullOrEmpty(actionTargetNames)) return null;
            List<Tuple<string, bool>> targetNames = new List<Tuple<string, bool>>();
            string[] names = actionTargetNames.Split(',', ';');
            foreach (string name in names)
            {
                if (String.IsNullOrEmpty(name)) continue;
                string targetName = name.Trim();
                bool targetActive = isActive;
                if (targetName.StartsWith("!"))
                {
                    targetName = (targetName.Length > 1 ? targetName.Substring(1).Trim() : "");
                    if (String.IsNullOrEmpty(targetName)) continue;
                    targetActive = !targetActive;
                }
                targetNames.Add(new Tuple<string, bool>(targetName, targetActive));
            }
            return targetNames;
        }
        /// <summary>
        /// Nastaví IsVisible pro control daného jména
        /// </summary>
        /// <param name="controlName">FullName controlu</param>
        /// <param name="isVisible">Má být zobrazen daný control?</param>
        /// <param name="callRefresh">Nastavit na true, pokud má proběhnout Refresh controlu (tj. po změně)</param>
        protected void _SetVisibleForControl(string controlName, bool isVisible, ref bool callRefresh)
        {
            if (String.IsNullOrEmpty(controlName)) return;

            object control = this._SearchForControl(controlName);
            if (control == null) return;

            if (control is MainDataPanel)
            {
                MainDataPanel mainDataPanel = control as MainDataPanel;
                mainDataPanel.GTabPage.TabHeader.ActivePage = mainDataPanel.GTabPage;
                callRefresh = true;
            }
            else if (control is TabContainer)
            {
                TabContainer tabContainer = control as TabContainer;
                tabContainer.IsCollapsed = !isVisible;
                callRefresh = true;
            }
            else if (control is MainDataTable)
            {
                MainDataTable mainDataTable = control as MainDataTable;
                mainDataTable.TableRow.Visible = isVisible;
                callRefresh = true;
            }
            else if (control is ToolBarItem)
            {
                ToolBarItem toolBarItem = control as ToolBarItem;
                toolBarItem.IsVisible = isVisible;
                callRefresh = true;
            }
            else if (control is TimeGraphLinkArray)
            {
                TimeGraphLinkArray graphLinkArray = control as TimeGraphLinkArray;
                graphLinkArray.CurrentLinksMode = (isVisible ? TimeGraphLinkMode.All : graphLinkArray.DefaultLinksMode);
                callRefresh = true;
            }
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
            ToolbarSystemItem items = (ToolbarSystemItem)(this._GuiToolBarPanel.ToolbarShowSystemItems & ToolbarSystemItem.TimeAxisAll);
            if (items == ToolbarSystemItem.None) return;

            // Oddělovač, pokud v grupě už jsou položky:
            this._ToolbarSystemTimeAxisGroup.AddSeparator(this);

            // Zoom:
            // ToolbarSystemItem timeAxisZoom = this.Config.TimeAxisZoom;
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomOneDay))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_Day, R.Images.Asol.Calendarday32x32Png, null, "Jeden den", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, /* isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomOneDay), */ isPersistEnabled: true, selectionGroupName: "TimeZoom", systemItemId: ToolbarSystemItem.TimeAxisZoomOneDay));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWorkWeek))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WorkWeek, R.Images.Asol.Calendarworkingweek32x32Png, null, "Pracovní týden Po-Pá", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, /* isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomWorkWeek), */ isPersistEnabled: true, selectionGroupName: "TimeZoom", systemItemId: ToolbarSystemItem.TimeAxisZoomWorkWeek));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWholeWeek))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WholeWeek, R.Images.Asol.Calendarweek32x32Png, null, "Celý týden Po-Ne", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, /* isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomWholeWeek), */ isPersistEnabled: true, selectionGroupName: "TimeZoom", systemItemId: ToolbarSystemItem.TimeAxisZoomWholeWeek));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomMonth))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_Month, R.Images.Asol.Calendarmonth32x32Png, null, "Měsíc 30 dní", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, /* isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomMonth), */ isPersistEnabled: true, selectionGroupName: "TimeZoom", systemItemId: ToolbarSystemItem.TimeAxisZoomMonth));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomHalfYear))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_HalfYear, R.Images.Actions.ViewProcessAllTreePng, null, "Půl rok", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, /* isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomMonth), */ isPersistEnabled: true, selectionGroupName: "TimeZoom", systemItemId: ToolbarSystemItem.TimeAxisZoomHalfYear));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisZoomWholeYear))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_WholeYear, R.Images.Actions.CodeBlockPng, null, "Celý rok", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, isSelectable: true, /* isSelected: (timeAxisZoom == ToolbarSystemItem.TimeAxisZoomMonth), */ isPersistEnabled: true, selectionGroupName: "TimeZoom", systemItemId: ToolbarSystemItem.TimeAxisZoomWholeYear));

            // Go:
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoPrev))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoPrev, R.Images.Asol.Calendarprevious32x32Png, null, "Zpět = doleva = do minulosti", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.ThisItemSkipToNextRow | LayoutHint.NextItemOnSameRow, moduleWidth: 1, systemItemId: ToolbarSystemItem.TimeAxisGoPrev));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoHome))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoHome, R.Images.Asol.Calendartoday32x32Png, null, "Aktuální čas", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemOnSameRow, moduleWidth: 2, systemItemId: ToolbarSystemItem.TimeAxisGoHome));
            if (items.HasFlag(ToolbarSystemItem.TimeAxisGoNext))
                this._ToolbarSystemTimeAxisGroup.Items.Add(_CreateToolbarItem(_Tlb_TimeAxis_GoNext, R.Images.Asol.Calendarnext32x32Png, null, "Vpřed = doprava = do budoucnosti", size: FunctionGlobalItemSize.Half, layoutHint: LayoutHint.NextItemSkipToNextTable, moduleWidth: 1, systemItemId: ToolbarSystemItem.TimeAxisGoNext));
        }
        /// <summary>
        /// Metoda se volá po akci SelectedChange na systémové položce ToolBaru. 
        /// Metoda zjistí, zda akce se týká časové osy, a pokud ano pak ji vyřeší.
        /// Pokud se akce nijak netýká časové osy, pak nic neprovádí (není tedy problém ji zavolat).
        /// </summary>
        /// <param name="action">Systémová akce</param>
        /// <param name="isSelected">Hodnota IsSelected (=IsChecked)</param>
        private void _TimeAxisToolBarSelected(ToolbarSystemItem action, bool isSelected)
        {
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
                this.CurrentTimeZoom = zoom;                        // Dříve šel do Configu:  this.Config.TimeAxisZoom = zoom;

            // Změna času:
            TimeRange currentTime = this._MainControl.SynchronizedTime.Value;
            TimeRange actionTime = _TimeAxisGetNewTime(currentTime, null, action, this.CurrentTimeZoom);     // Action a CurrentTimeZoom se mohou lišit !!!  action může být TimeAxisGoHome, a přitom _CurrentTimeZoom si udržuje aktuální hodnotu, např. TimeAxisZoomOneDay
            if (actionTime != null)
                this._MainControl.SynchronizedTime.Value = actionTime;
        }
        /// <summary>
        /// Aktuální časové měřítko odvozené od Toolbaru.
        /// Obsahuje pouze hodnoty z rozmezí <see cref="ToolbarSystemItem.TimeAxisZoomAll"/>.
        /// </summary>
        private ToolbarSystemItem CurrentTimeZoom
        {
            get { return this.__CurrentTimeZoom; }
            set { this.__CurrentTimeZoom = (value & ToolbarSystemItem.TimeAxisZoomAll); }
        }
        /// <summary>
        /// Aktuální časové měřítko odvozené od Toolbaru
        /// </summary>
        private ToolbarSystemItem __CurrentTimeZoom = ToolbarSystemItem.None;
        /// <summary>
        /// Metoda zajistí přípravu defaultního časového měřítka, pokud nebylo z Configu načteno do ToolBaru.
        /// Volá se až po načtení obsahu Configu do controlu.
        /// </summary>
        private void _PrepareDefaultTimeZoom()
        {
            // Pokud už máme určené nějaké časové měřítko (CurrentTimeZoom není None), pak nemusíme nic víc řešit:
            if (this.CurrentTimeZoom != ToolbarSystemItem.None) return;

            // V Configu nemáme uložené časové měřítko (anebo mu neodpovídá žádný aktuální button):
            //  zkusím najít první existující button časové osy pro některé časové měřítko:
            FunctionGlobalItem timeItem = this._SearchFirstToolBarItem(ToolbarSystemItem.TimeAxisZoomWorkWeek, ToolbarSystemItem.TimeAxisZoomWholeWeek, ToolbarSystemItem.TimeAxisZoomOneDay, ToolbarSystemItem.TimeAxisZoomMonth);
            if (timeItem == null) return;

            // V toolbaru najdu jeho grafický prvek (ten je "živý", na rozdíl od datového prvku):
            var gItem = this._MainControl.ToolBarGFunctionItems.FirstOrDefault(i => Object.ReferenceEquals(i.DataItem, timeItem));
            if (gItem == null) return;

            // Musím nasimulovat jeho zaškrtnutí uživatelem = včetně řízení IsChecked v OptionGroup, a vyvolání eventů (Selected, Click):
            gItem.DoClick();
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
                case ToolbarSystemItem.TimeAxisZoomHalfYear: return _TimeAxisGetNewTimeZoomHalfYear(date);
                case ToolbarSystemItem.TimeAxisZoomWholeYear: return _TimeAxisGetNewTimeZoomWholeYear(date);
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
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomHalfYear"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomHalfYear(DateTime value)
        {
            DateTime begin = value.Date.FirstDayOf(DateTimePart.HalfYear);
            DateTime end = begin.AddMonths(6);
            TimeRange time = new TimeRange(begin, end);
            return time.ZoomToRatio(time.Center.Value, _TimeAxisEnlargeRatio);
        }
        /// <summary>
        /// Vrátí nový časový interval pro časovou osu pro současný interval a požadavek <see cref="ToolbarSystemItem.TimeAxisZoomWholeYear"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TimeRange _TimeAxisGetNewTimeZoomWholeYear(DateTime value)
        {
            DateTime begin = value.Date.FirstDayOf(DateTimePart.Year);
            DateTime end = begin.AddMonths(12);
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
        private const string _Tlb_TimeAxis_HalfYear = "TimeAxisHalfYear";
        private const string _Tlb_TimeAxis_WholeYear = "TimeAxisWholeYear";
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
            SourceActionType currentSourceAction = SourceActionType.TimeAxisChanged;      // Zdroj akce = změna času na ose
            TargetActionType targetActions = TargetActionType.SearchSourceVisibleTime;    // Cílové akce, které jsou podmíněné zobrazeným časovým intervalem
            this.RunGuiInteractions(currentSourceAction, targetActions);
            this._TimeAxisChangeRunService(e.NewValue);
            this.ConfigTimeAxisValue = e.NewValue;
        }
        #endregion
        #region Časová osa - reakce na změny, volání servisní funkce s požadavkem GuiRequest.COMMAND_TimeChange
        /// <summary>
        /// Metoda je volána po každé změně na časové ose, zajistí detekci a volání servisní funkce s požadavkem <see cref="GuiRequest.COMMAND_TimeChange"/> v případě potřeby.
        /// </summary>
        /// <param name="timeRange"></param>
        private void _TimeAxisChangeRunService(TimeRange timeRange)
        {
            switch (this.GuiData.Properties.TimeChangeSend)
            {
                case TimeChangeSendMode.None:
                    break;
                case TimeChangeSendMode.OnNewTime:
                    this._TimeAxisChangeRunServiceOnNewTime(timeRange);
                    break;
                case TimeChangeSendMode.Allways:
                    this._TimeAxisChangeRunServiceAllways(timeRange);
                    break;
            }
        }
        /// <summary>
        /// Řeší změnu zobrazeného času v režimu <see cref="TimeChangeSendMode.OnNewTime"/>
        /// </summary>
        /// <param name="timeRange"></param>
        private void _TimeAxisChangeRunServiceOnNewTime(TimeRange timeRange)
        {
            if (timeRange == null || !timeRange.IsFilled || !timeRange.IsReal) return;

            // Pokud aktuální čas na ose celý již "známe", je to OK:
            if (this._TimeAxisKnownArray.Contains(timeRange)) return;

            // Aktuální čas zvětšíme, abychom měli na stranách rezervy:
            double? ratio = this.GuiData.Properties.TimeChangeSendEnlargement;
            TimeRange timeRangeEnlarge = ((ratio.HasValue && ratio.Value > 0f) ? timeRange.ZoomToRatio(timeRange.Center.Value, 1d + ratio.Value) : timeRange);

            // Zvětšený čas si přidáme do pole "známého času" abychom ho už brali jako "známý":
            this._TimeAxisKnownArray.Merge(timeRangeEnlarge);

            // Zavoláme servis:
            this._TimeAxisChangeCallAppHost(timeRange, timeRangeEnlarge);
        }
        /// <summary>
        /// Řeší změnu zobrazeného času v režimu <see cref="TimeChangeSendMode.Allways"/>
        /// </summary>
        /// <param name="timeRange"></param>
        private void _TimeAxisChangeRunServiceAllways(TimeRange timeRange)
        {
            // Aktuální čas zvětšíme, abychom měli na stranách rezervy:
            double? ratio = this.GuiData.Properties.TimeChangeSendEnlargement;
            TimeRange timeRangeEnlarge = ((ratio.HasValue && ratio.Value > 0f) ? timeRange.ZoomToRatio(timeRange.Center.Value, 1d + ratio.Value) : timeRange);

            // Zavoláme servis:
            this._TimeAxisChangeCallAppHost(timeRange, timeRangeEnlarge);
        }
        /// <summary>
        /// Vyvolá AppHost a předá command <see cref="GuiRequest.COMMAND_TimeChange"/> a odpovídající data
        /// </summary>
        /// <param name="timeRange"></param>
        /// <param name="timeRangeEnlarge"></param>
        private void _TimeAxisChangeCallAppHost(TimeRange timeRange, TimeRange timeRangeEnlarge)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_TimeChange;
            request.TimeAxisChange = new GuiRequestTimeAxisChange()
            {
                TimeRangeVisible = timeRange,
                TimeRangeEnlarged = timeRangeEnlarge,
                TimeRangeKnown = this._TimeAxisKnownArray.Items.Select(t => (GuiTimeRange)t).ToArray()
            };


            // Vyvoláme akci ihned nebo až po nějakém čase?
            _TimeAxisChangeRequest = request;
            int delay = this._TimeChangeSendDelay;
            if (delay <= 0)
                // Odeslat ihned:
                _TimeAxisChangeSendCommand();
            else
                // Odeslat se zpožděním, a pokud mezitím přijde další request, pak restartovat daný budík (_TimeAxisChangeTimerGuid):
                _TimeAxisChangeTimerGuid = WatchTimer.CallMeAfter(_TimeAxisChangeSendCommand, delay, true, _TimeAxisChangeTimerGuid);
        }
        /// <summary>
        /// Zde je uložený posledně platný požadavek pro vyvolání akce na server typu <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// Na server jej odešle metoda <see cref="_TimeAxisChangeSendCommand"/>.
        /// </summary>
        private GuiRequest _TimeAxisChangeRequest;
        /// <summary>
        /// Guid obsahující klíč budíku v <see cref="WatchTimer"/>, který řeší Delay při odesílání akce <see cref="_TimeAxisChangeSendCommand"/>.
        /// </summary>
        private Guid? _TimeAxisChangeTimerGuid;
        /// <summary>
        /// Zde je uložený posledně platný požadavek pro vyvolání akce na server typu <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// Na server jej odešle metoda 
        /// </summary>
        private void _TimeAxisChangeSendCommand()
        {   // Běžím v GUI threadu, tím jsem spolehlivě jediný v této metodě!
            // I metoda _TimeAxisChangeCallAppHost() běží výhradně v GUI threadu, proto se nepohádám (ThreadSync) ani o proměnnou _TimeAxisChangeTimerGuid:
            _TimeAxisChangeTimerGuid = null;                         // Tímto zajistím, že případný příští požadavek si nastartuje svého vlastního budíka.

            var request = _TimeAxisChangeRequest;
            _TimeAxisChangeRequest = null;                           // Tímto zajistím převzetí argumentu z proměnné a její nulování a tedy pouze 1x odeslání na server:
            if (request != null)
                this._CallAppHostFunction(request, _TimeAxisChangeApplicationResponse);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost TimeChange
        /// </summary>
        /// <param name="response"></param>
        private void _TimeAxisChangeApplicationResponse(AppHostResponseArgs response)
        {
            this.ProcessGuiResponse(response.GuiResponse);
        }

        /// <summary>
        /// Pole časových intervalů na časové ose, pro které jsme už zadali request <see cref="GuiRequest.COMMAND_TimeChange"/>, 
        /// a považujeme je tedy za "obsahující data".
        /// <para/>
        /// Pole obsahuje jednotlivé prvky, které mají kladný čas, mezi jednotlivými následujícími prvky je kladná mezera.
        /// Pokud by byl vkládán prvek s časem nezadaným nebo nulovým nebo záporným, nevloží se.
        /// Prvky se navzájem nepřekrývají ani nedotýkají (takové jsou při vkládání sloučeny).
        /// Prvky jsou setříděny podle času vzestupně.
        /// </summary>
        private TimeRangeArray _TimeAxisKnownArray;
        /// <summary>
        /// Režim odesílání změn časové osy.
        /// Načten z <see cref="GuiProperties.TimeChangeSend"/> zde při inicializaci v metodě <see cref="_FillMainControlPagesFromGui()"/>
        /// </summary>
        private TimeChangeSendMode _TimeChangeSend;
        /// <summary>
        /// Zpoždění v milisekundách, po kterém dojde k odeslání commandu TimeChange po poslední změně časové osy.
        /// Pokud mezi dvěma změnami na časové ose dojde v kratším intervalu, pak se ta předchozí neodesílá, odešle se až ta poslední.<br/>
        /// Default = 0: odesílá se ihned (stejně jako pro záporné hodnoty).
        /// </summary>
        private int _TimeChangeSendDelay;
        /// <summary>
        /// Zvětšení časového intervalu aktuální časové osy použité do requestu <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// Načten z <see cref="GuiProperties.TimeChangeSendEnlargement"/> zde při inicializaci v metodě <see cref="_FillMainControlPagesFromGui()"/>
        /// </summary>
        private double? _TimeChangeSendEnlargement;
        #endregion
        #region Systémové nastavení - tvorba položky v Toolbaru a celá obsluha
        /// <summary>
        /// Inicializace položek ToolBaru (grupa <see cref="_ToolbarSystemSettingsGroup"/>) pro hlavní okno konfigurace
        /// </summary>
        private void _SystemSettingsToolBarInit()
        {
            // Pokud v definici (ToolbarShowSystemItems) je uvedena hodnota (MoveItemAll | GuiEditAll), 
            //  pak NEBUDOU zobrazovány jednotlivé buttony, ale místo nich se zobrazí velký button CONFIG:
            ToolbarSystemItem currentItems = this._GuiToolBarPanel.ToolbarShowSystemItems;
            ToolbarSystemItem items = (ToolbarSystemItem)(currentItems & ToolbarSystemItem.SystemSettingsAll);
            if (items == ToolbarSystemItem.None) return;

            // Oddělovač, pokud v grupě už jsou položky:
            this._ToolbarSystemSettingsGroup.AddSeparator(this);

            if (items.HasFlag(ToolbarSystemItem.ShowSystemConfig))
                this._ToolbarSystemSettingsGroup.Items.Add(_CreateToolbarItem(_Tlb_SystemSettingsConfig, R.Images.Actions.RunBuildConfigurePng, "Nastavení", "Otevře okno kompletního nastavení aplikace", size: FunctionGlobalItemSize.Whole, systemItemId: ToolbarSystemItem.ShowSystemConfig));

            this._ToolbarSystemSettingsGroup.AddSeparator(this, true);
        }
        /// <summary>
        /// Po kliknutí na systémovou ikonu Toolbaru, řeší akce typu Config
        /// </summary>
        /// <param name="action"></param>
        private void _SystemSettingsToolBarAction(ToolbarSystemItem action)
        {
            action = (ToolbarSystemItem)(action & ToolbarSystemItem.SystemSettingsAll);
            if (action == ToolbarSystemItem.None) return;

            switch (action)
            {
                case ToolbarSystemItem.ShowSystemConfig:
                    this._MainControl.RunInGUI(() => SchedulerConfigForm.ShowDialog(this._MainControl.MainForm, this.Config));
                    break;
            }
        }
        private const string _Tlb_SystemSettingsConfig = "SystemSettingsConfig";
        #endregion
        #region Přichytávání prvku grafu při jeho posouvání procesem Drag and Drop - vlastní provádění příchytu prvků
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
            TimeGraph targetGraph = moveInfo.TargetGraph;
            if (targetGraph == null) return null;

            DateTime timePoint = (nearSide == RangeSide.Begin ? moveInfo.TimeRangeFinal.Begin.Value : moveInfo.TimeRangeFinal.End.Value);
            int xc = (nearSide == RangeSide.Begin ? moveInfo.BoundsFinalAbsolute.X : moveInfo.BoundsFinalAbsolute.Right - 1);
            DateTime? begin = moveInfo.GetTimeForPosition(xc - distance);
            DateTime? end = moveInfo.GetTimeForPosition(xc + distance);
            TimeRange timeWindow = new TimeRange(begin, end);

            TimeGraphGroup nearGroup;
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
        private DateTime? _AdjustSearchTimeSelector(TimeGraphGroup group, GraphItemChangeInfo moveInfo, RangeSide nearSide, SearchNearItemMode searchMode)
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
        /// Režim hledání vhodné grupy a jejího času v rámci metody <see cref="_AdjustSearchTimeSelector(TimeGraphGroup, GraphItemChangeInfo, RangeSide, SearchNearItemMode)"/>
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
            Int32Range boundsX = Int32Range.CreateFromRectangle(moveInfo.SourceBounds, WinForms.Orientation.Horizontal);
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
            using (App.Trace.Scope(TracePriority.Priority3_BellowNormal, "MainData", "FillMainControlPagesFromGui", ""))
            {
                this._TimeAxisKnownArray = new TimeRangeArray();
                this._TimeChangeSend = this.GuiData.Properties.TimeChangeSend;
                this._TimeChangeSendDelay = this.GuiData.Properties.TimeChangeSendDelay;
                this._TimeChangeSendEnlargement = this.GuiData.Properties.TimeChangeSendEnlargement;
                this._TimeAxisKnownArray.Merge(this.GuiData.Properties.TimeChangeInitialValue);
                this._MainControl.ClearPages();
                this._MainControl.SynchronizedTime.Value = this._GetInitialTimeRange();
                this._MainControl.SynchronizedTime.ValueLimit = this.GuiData.Properties.TotalTimeRange;
                this._MainControl.InitialDialog = this.GuiData.InitialDialog;
                this._LoadPages();
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
        /// Metoda získá a vrátí výchozí rozsah časové osy.
        /// Hodnotu bere z <see cref="GuiProperties.InitialTimeRange"/>,
        /// a pokud je <see cref="GuiProperties.UseInitialTimeRangeFromLastRun"/> = true pak se pokusí najít hodnotu v konfiguraci.
        /// </summary>
        /// <returns></returns>
        private TimeRange _GetInitialTimeRange()
        {
            GuiTimeRange timeAxisValue = this.GuiData.Properties.InitialTimeRange;
            if (this.GuiData.Properties.UseInitialTimeRangeFromLastRun)
            {
                GuiTimeRange configTimeAxisValue = this.ConfigTimeAxisValue;
                if (configTimeAxisValue != null && configTimeAxisValue.Begin.Year > 2000 && configTimeAxisValue.End > configTimeAxisValue.Begin)
                    timeAxisValue = configTimeAxisValue;
            }
            return timeAxisValue;
        }
        /// <summary>
        /// Načte data všech datových stránek <see cref="GuiPage"/>: vytvoří pro ně vizuální panely, načte jejich data, tabulky, grafy, atd.
        /// </summary>
        private void _LoadPages()
        {
            foreach (GuiPage guiPage in this._GuiPages.Pages)
            {
                App.TryRun(() =>
                {
                    var panel = this._MainControl.AddPage(guiPage);
                    this._AddControl(guiPage.FullName, panel);
                    this._AddControl(guiPage.LeftPanel?.FullName, panel.LeftPanelTabs);
                    this._AddControl(guiPage.RightPanel?.FullName, panel.RightPanelTabs);
                    this._AddControl(guiPage.BottomPanel?.FullName, panel.BottomPanelTabs);
                });
            }
        }
        /// <summary>
        /// Soupis všech stránek s daty, zobrazenými v GUI. Typický bývá jedna.
        /// Stránky (instance <see cref="MainDataPanel"/>) obsahují referenci na vstupní data <see cref="MainDataPanel.GuiPage"/>,
        /// refeenci na vizuální control Scheduleru <see cref="MainDataPanel.SchedulerPanel"/>
        /// a refeenci na záložku stránky <see cref="MainDataPanel.GTabPage"/>.
        /// </summary>
        internal MainDataPanel[] DataPanels { get { return this._MainControl.DataPanels; } }
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
                foreach (MainDataPanel panel in this._MainControl.DataPanels)
                {
                    foreach (MainDataTable table in panel.SchedulerPanel.DataTables)
                    {
                        table.GraphLinkArray.LinkDataSource = table;
                        tableList.Add(table);
                        this._AddControl(table.TableName, table);
                        this._AddControl(table.TableName + GuiData.NAME_DELIMITER + GuiData.TABLELINK_NAME, table.GraphLinkArray);
                    }
                }
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
                    dataTable.PrepareAfterLoad();
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
        /// Tabulka s daty, která je nyní aktivní.
        /// Obsahuje tabulku, která má nebo měla poslední focus.
        /// Pokud focus odejde na jiný objekt, než tabulka, pak zde zůstává odkaz na poslední tabulku.
        /// Null je zde jen po spuštění, ale je to přípustný stav.
        /// </summary>
        internal MainDataTable ActiveDataTable { get; private set; }
        /// <summary>
        /// Souhrn všech datových tabulek ze všech panelů.
        /// </summary>
        private MainDataTable[] _DataTables;
        #endregion
        #region Soupis úplně všech objektů GUI
        /// <summary>
        /// Metoda přidá do interního lineárního soupisu další control, který bude možno vyhledat podle jeho názvu <paramref name="fullName"/>.
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="control"></param>
        private void _AddControl(string fullName, object control)
        {
            if (this._ControlDict == null) this._ControlDict = new Dictionary<string, object>();
            if (String.IsNullOrEmpty(fullName)) return;
            if (control == null) return;
            if (!this._ControlDict.ContainsKey(fullName))
                this._ControlDict.Add(fullName, control);
            else
                this._ControlDict[fullName] = control;
        }
        /// <summary>
        /// Metoda najde a vrátí control pro daný název
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private object _SearchForControl(string fullName)
        {
            if (String.IsNullOrEmpty(fullName)) return null;
            if (this._ControlDict == null) return null;
            object control;
            if (!this._ControlDict.TryGetValue(fullName, out control)) return null;
            return control;
        }
        /// <summary>
        /// Dictionary obsahující všechny prvky GUI
        /// </summary>
        private Dictionary<string, object> _ControlDict;
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
        /// z tohoto souhrnu je vytvořeno kontextové menu pro konkrétní situaci, v metodě <see cref="CreateContextMenu(GuiContextMenuRunArgs, WinForms.ToolStripItemClickedEventHandler)"/>.
        /// </summary>
        protected ContextFunctionItem[] _ContextFunctions;
        /// <summary>
        /// Metoda vytvoří a vrátí kontextové menu pro konkrétní prvek grafu, zadaný jeho identifikátorem.
        /// Vytvořené kontextové menu má ve svém objektu Menu.Tag uložena data <see cref="GuiContextMenuRunArgs"/>, která jsou předaná do této metody jako argument.
        /// takže menu jako celek má informace o prvku a času kliknutí.
        /// Každá jednotlivá položka menu má v MenuItem.Tag uloženu definici kontextového menu, tedy instanci třídy <see cref="ContextFunctionItem"/>,
        /// takže po kliknutí na položku menu jsou k dispozici definiční date dané funkce.
        /// <para/>
        /// Tato metoda vrací menu, které ma nastavený eventhandler <see cref="WinForms.ToolStrip.ItemClicked"/> do zdejší metody, která předává požadavek na obsluhu funkce
        /// do datového zdroje.
        /// Pokud aplikační kód potřebuje řešit <see cref="WinForms.ToolStrip.ItemClicked"/> sám, pak předá druhý parametr 
        /// </summary>
        /// <param name="menuRunArgs">Data popisující prvek a čas kliknutí</param>
        /// <param name="itemClickedHandler">Externí handler pro obsluhu kliknutí na položku menu. Default = null, použije se interní. Pokud bude zadán, pak se interní handler nepoužije.</param>
        /// <returns></returns>
        protected WinForms.ToolStripDropDownMenu CreateContextMenu(GuiContextMenuRunArgs menuRunArgs, WinForms.ToolStripItemClickedEventHandler itemClickedHandler)
        {
            if (this._ContextFunctions == null || this._ContextFunctions.Length == 0) return null;              // Nejsou data => není menu.
            ContextFunctionItem[] items = this._ContextFunctions.Where(cfi => cfi.IsValidFor(menuRunArgs.ContextItemId)).ToArray();  // Vybereme jen ty funkce, které jsou vhodné pro daný prvek
            if (items.Length == 0) return null;         // Nic se nehodí => nic se nezobrazí

            // Celkové menu:
            WinForms.ToolStripDropDownMenu menu = FunctionItem.CreateDropDownMenuFrom(items, m => this.CreateContextMenuSetParams(m, menuRunArgs));

            // Kdo bude řešit kliknutí na položku: externí nebo interní handler?
            if (itemClickedHandler != null)
                menu.ItemClicked += itemClickedHandler;
            else
                menu.ItemClicked += this.ContextMenuItemClicked;

            return menu;
        }
        /// <summary>
        /// Do předaného menu vloží parametry a titulkový řádek
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuRunArgs"></param>
        protected void CreateContextMenuSetParams(WinForms.ToolStripDropDownMenu menu, GuiContextMenuRunArgs menuRunArgs)
        {
            // 1. Data:
            menu.Tag = menuRunArgs;

            // 2. Parametry menu:
            GuiContextMenuSet cmSet = this._GuiContextMenuSet;
            if (cmSet.BackColor.HasValue)
                menu.BackColor = cmSet.BackColor.Value;
            if (cmSet.DropShadowEnabled.HasValue)
                menu.DropShadowEnabled = cmSet.DropShadowEnabled.Value;
            if (cmSet.Opacity.HasValue)
                menu.Opacity = cmSet.Opacity.Value;
            if (cmSet.ImageScalingSize.HasValue)
                menu.ImageScalingSize = cmSet.ImageScalingSize.Value;

            // 3. Titulek menu:
            if (!String.IsNullOrEmpty(cmSet.Title))
            {
                WinForms.ToolStripLabel title = new WinForms.ToolStripLabel(cmSet.Title);
                title.ToolTipText = cmSet.ToolTip;
                if (cmSet.ImageScalingSize.HasValue)
                    title.Size = new Size(100, cmSet.ImageScalingSize.Value.Height + 4);
                title.Font = new Font(title.Font, System.Drawing.FontStyle.Bold);
                title.TextAlign = ContentAlignment.MiddleCenter;
                menu.Items.Add(title);

                menu.Items.Add(new WinForms.ToolStripSeparator());
            }
        }
        /// <summary>
        /// Obsluha kliknutí na položku kontextového menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuItemClicked(object sender, WinForms.ToolStripItemClickedEventArgs e)
        {
            WinForms.ToolStripDropDownMenu menu = sender as WinForms.ToolStripDropDownMenu;
            if (menu == null) return;
            menu.Hide();

            GuiContextMenuRunArgs menuRunArgs = menu.Tag as GuiContextMenuRunArgs;       // data o kliknutí (prvek = (tabulka, řádek, prvek), čas)
            ContextFunctionItem funcItem = e.ClickedItem.Tag as ContextFunctionItem;     // data konkrétní funkce, na kterou se kliklo
            if (menuRunArgs != null && funcItem != null && funcItem.GuiContextMenuItem != null)
            {
                menuRunArgs.ContextMenuItem = funcItem.GuiContextMenuItem;
                this._ContextMenuItemClickApplication(menuRunArgs);
            }
        }
        /// <summary>
        /// Obsluha události ItemClick na Aplikační položce kontextového menu
        /// </summary>
        /// <param name="menuRunArgs">Kompletní argumen</param>
        private void _ContextMenuItemClickApplication(GuiContextMenuRunArgs menuRunArgs)
        {
            GuiRequest request = new GuiRequest();
            request.Command = GuiRequest.COMMAND_ContextMenuClick;
            request.ActiveGraphItem = menuRunArgs.ContextItemId;
            request.ContextMenu = menuRunArgs;
            this._CallAppHostFunction(request, this._ContextMenuItemClickApplicationResponse);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost ItemClick na Aplikační položce ToolBaru
        /// </summary>
        /// <param name="response"></param>
        private void _ContextMenuItemClickApplicationResponse(AppHostResponseArgs response)
        {
            this.ProcessGuiResponse(response.GuiResponse);
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
            /// Barva pozadí prvku
            /// </summary>
            public override Color? BackColor { get { return this._GuiContextMenuItem.BackColor; } }
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
            public override Image Image { get { return Application.App.ResourcesApp.GetImage(this._GuiContextMenuItem.Image); } }
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
            #region Konstrukce, instanční proměnné
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            /// <param name="fullNameRegex"></param>
            /// <param name="area"></param>
            /// <param name="classDict"></param>
            private ContextFunctionValidInfo(System.Text.RegularExpressions.Regex fullNameRegex, AreaType area, Dictionary<int, ClassValidityRange> classDict)
            {
                this.FullNameRegex = fullNameRegex;
                this.Area = area;
                this.ClassDict = classDict;
            }
            /// <summary>
            /// Zadaná cesta k <see cref="GuiGrid"/>. Může obsahovat wildcards.
            /// </summary>
            protected System.Text.RegularExpressions.Regex FullNameRegex { get; private set; }
            /// <summary>
            /// Prostor, kde může být tato funkce použita
            /// </summary>
            protected AreaType Area { get; private set; }
            /// <summary>
            /// Povolené třídy. Pokud není žádná, neřeší se.
            /// </summary>
            protected Dictionary<int, ClassValidityRange> ClassDict { get; private set; }
            /// <summary>
            /// Typ oblasti, kde může být použita Kontextová funkce
            /// </summary>
            [Flags]
            protected enum AreaType
            {
                /// <summary>
                /// Nikde
                /// </summary>
                None = 0,
                /// <summary>
                /// Prvek grafu
                /// </summary>
                Item = 0x01,
                /// <summary>
                /// Graf samotný
                /// </summary>
                Graph = 0x02,
                /// <summary>
                /// Řádek tabulky
                /// </summary>
                Row = 0x04
            }
            #endregion
            #region Parsování textu
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
                string[] items = valid.Split(';');                        // Oddělíme jednotlivé prvky ContextFunctionValidInfo
                foreach (string item in items)
                {
                    if (String.IsNullOrEmpty(item)) continue;
                    System.Text.RegularExpressions.Regex fullNameRegex = null;
                    AreaType? area = null;
                    Dictionary<int, ClassValidityRange> classDict = null;
                    string[] parts = item.Split(':');                     // FullName:Classes, anebo FullName:Area:Classes
                    int partCount = parts.Length;
                    if (partCount == 1)
                    {   // Položka s jedním prvkem je autodetect:
                        classDict = _ParseClasses(parts[0], true);        // Parsování bude striktní = pokud najdu položku, která není číslo, pak se vrátí null
                        if (classDict == null)
                            fullNameRegex = _ParseRegex(parts[0]);        // Pokud to nejsou striktně jen číslice, může to být Regex
                    }
                    else if (partCount == 2)
                    {   // Položka s dvěma prvky může obsahovat FullName a Area, nebo FullName a Classes:
                        fullNameRegex = _ParseRegex(parts[0]);
                        area = _ParseArea(parts[1]);                      // Zkusíme parsovat typ prostoru
                        if (!area.HasValue)
                            classDict = _ParseClasses(parts[1], false);   // Parsování nebude striktní = beru vše, co se najde
                    }
                    else if (partCount == 3)
                    {
                        fullNameRegex = _ParseRegex(parts[0]);
                        area = _ParseArea(parts[1]);                      // Zkusíme parsovat typ prostoru
                        classDict = _ParseClasses(parts[2], false);       // Parsování nebude striktní = beru vše, co se najde
                    }
                    if (fullNameRegex != null || classDict != null)
                    {
                        if (!area.HasValue) area = AreaType.Item;
                        ContextFunctionValidInfo info = new ContextFunctionValidInfo(fullNameRegex, area.Value, classDict);
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
            /// Z dodaného textu se pokusí najít a vrátit typ prostoru. Pokud tam nebude nic rozpoznatelného, vrátí null.
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static AreaType? _ParseArea(string text)
            {
                if (String.IsNullOrEmpty(text)) return null;
                AreaType? result = null;
                string[] items = text.Split(" ,;-|+*".ToCharArray());
                foreach (string item in items)
                {
                    AreaType? value = _ParseAreaOne(item);
                    if (value.HasValue)
                        result = (result.HasValue ? (result.Value | value.Value) : value.Value);
                }
                return result;
            }
            /// <summary>
            /// Vrátí hodnotu <see cref="AreaType"/> z dodaného textu. Pokud ji nerozezná, vrátí null.
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            private static AreaType? _ParseAreaOne(string item)
            {
                if (String.IsNullOrEmpty(item)) return null;
                string text = item.Trim();
                if (String.Equals(text, GuiContextMenuItem.AREA_ITEM, StringComparison.InvariantCultureIgnoreCase)) return AreaType.Item;
                if (String.Equals(text, GuiContextMenuItem.AREA_GRAF, StringComparison.InvariantCultureIgnoreCase)) return AreaType.Graph;
                if (String.Equals(text, GuiContextMenuItem.AREA_ROW, StringComparison.InvariantCultureIgnoreCase)) return AreaType.Row;
                return null;
            }
            /// <summary>
            /// Z dodaného textu, který obsahuje čísla tříd oddělená čárkami, vrátí jejich Dictionary.
            /// </summary>
            /// <param name="text"></param>
            /// <param name="strictParse">Požadavek (true), aby se výsledná Dictionary vrátila pouze tehdy, když vstupní text je korektní = obsahuje jen čísla a oddělovače. Pokud by obsahoval jiné znaky, vrací se null.</param>
            /// <returns></returns>
            private static Dictionary<int, ClassValidityRange> _ParseClasses(string text, bool strictParse)
            {
                if (String.IsNullOrEmpty(text)) return null;
                Dictionary<int, ClassValidityRange> result = new Dictionary<int, ClassValidityRange>();
                string[] items = text.Split(',');
                foreach (string item in items)
                {
                    string classText = item.Trim();
                    ClassValidityRange validityRange = _ParseClassValidityRange(ref classText);
                    int value;
                    if (classText.Length > 0 && Int32.TryParse(classText, out value))
                    {   // Je to číslo, OK:
                        if (value > 0 && !result.ContainsKey(value))
                            result.Add(value, validityRange);
                    }
                    else if (strictParse)
                    {   // Není to číslo, a pokud máme být striktní, pak ihned vrátíme null:
                        return null;
                    }
                }
                return (result.Count == 0 ? null : result);
            }
            /// <summary>
            /// Řeší prefix <see cref="ClassValidityRange"/> v dodaném textu
            /// </summary>
            /// <param name="classText"></param>
            /// <returns></returns>
            private static ClassValidityRange _ParseClassValidityRange(ref string classText)
            {
                ClassValidityRange validityRange = ClassValidityRange.None;
                if (classText != null && classText.Length >= 2)
                {
                    string prefix = classText.Substring(0, 1);
                    switch (prefix)
                    {
                        case GuiContextMenuItem.CLASS_ALL:
                            validityRange = ClassValidityRange.WholeClass;
                            classText = classText.Substring(1).Trim();
                            break;
                        case GuiContextMenuItem.CLASS_MASTER:
                            validityRange = ClassValidityRange.Master;
                            classText = classText.Substring(1).Trim();
                            break;
                        case GuiContextMenuItem.CLASS_ENTRIES:
                            validityRange = ClassValidityRange.Entries;
                            classText = classText.Substring(1).Trim();
                            break;
                        default:
                            validityRange = ClassValidityRange.WholeClass;
                            break;
                    }
                }
                return validityRange;
            }
            /// <summary>
            /// Platnost z pohledu třídy
            /// </summary>
            protected enum ClassValidityRange
            {
                /// <summary>
                /// Nic
                /// </summary>
                None,
                /// <summary>
                /// Master
                /// </summary>
                Master,
                /// <summary>
                /// Entries
                /// </summary>
                Entries,
                /// <summary>
                /// Celá třída
                /// </summary>
                WholeClass
            }
            #endregion
            #region Testy validity
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
                if (!this._IsValidRegex(gridItemId.TableName)) return false;                                      // Pokud NEODPOVÍDÁ název Gridu, pak prvek nevyhovuje
                AreaType area = _GetAreaType(gridItemId);                                                         // Určí typ prvku ITEM / GRAPH
                if (!this._IsValidArea(area)) return false;                                                       // Testujeme typ prvku

                // Item: Testujeme číslo třídy Item, Group, Data
                if (this.Area.HasFlag(AreaType.Item) && this._IsValidClass(gridItemId.ItemId, gridItemId.GroupId, gridItemId.DataId)) return true;

                // Row: Testujeme číslo třídy Row
                if (this.Area.HasAnyFlag(AreaType.Graph | AreaType.Row) && this._IsValidClass(gridItemId.RowId)) return true;

                return false;
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
                {   // Jakýkoli GuiId s povolenou třídou a odpovídající Master/Entries zajistí vrácení true:
                    ClassValidityRange validityRange;
                    if (guiId != null && this.ClassDict.TryGetValue(guiId.ClassId, out validityRange))
                    {
                        bool isEntry = guiId.EntryId.HasValue;                 // true = je zadán položkový GuiId
                        if ((validityRange == ClassValidityRange.WholeClass) || 
                            (validityRange == ClassValidityRange.Master && !isEntry) ||
                            (validityRange == ClassValidityRange.Entries && isEntry))
                            return true;
                    }
                }
                // Žádný zadaný GuiId nemá povolenou třídu, vrátíme false:
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud daný typ prostoru vyhovuje zdejší podmínce
            /// </summary>
            /// <param name="area"></param>
            /// <returns></returns>
            private bool _IsValidArea(AreaType area)
            {
                return ((this.Area & area) != 0);
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
            /// <summary>
            /// Odhadne typ prostoru podle dat o prvku
            /// </summary>
            /// <param name="gridItemId"></param>
            /// <returns></returns>
            private static AreaType _GetAreaType(GuiGridItemId gridItemId)
            {
                if (gridItemId == null) return AreaType.Row;
                if (gridItemId.ItemId != null) return AreaType.Item;
                if (gridItemId.RowId != null) return AreaType.Graph;
                return AreaType.Row;
            }
            #endregion
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
            this._CallAppHostFunction(request, _CallHostRunOpenRecordsFormResponse);
        }
        /// <summary>
        /// Zpracování odpovědi z aplikační funkce, na událost OpenRecords
        /// </summary>
        /// <param name="response"></param>
        private void _CallHostRunOpenRecordsFormResponse(AppHostResponseArgs response)
        {
            this.ProcessGuiResponse(response.GuiResponse);
        }
        #endregion
        #region Interaktivní přidávání prvků pomocí myši (MousePaint) => předání do konkrétní MainDataTable
        /// <summary>
        /// Zaeviduje si prvek toolbaru, pokud má vztah k aktivitě MousePaint (=kreslení myší).
        /// </summary>
        /// <param name="guiToolbarItem">Definice, nesmí být null</param>
        /// <param name="toolItem">Fyzický prvek, smí být null</param>
        private void _MousePaintAddToolBarItem(GuiToolbarItem guiToolbarItem, ToolBarItem toolItem)
        {
            GuiActionType mousePaintActions = _MousePaintToolBarGetAction(guiToolbarItem);
            if (mousePaintActions == GuiActionType.None) return;

            // Daný prvek toolbaru má vliv na akci MousePaint, zaevidujeme si jej:
            if (this._MousePaintToolBarItems == null) this._MousePaintToolBarItems = new List<Tuple<GuiToolbarItem, GuiActionType, ToolBarItem>>();
            this._MousePaintToolBarItems.Add(new Tuple<GuiToolbarItem, GuiActionType, ToolBarItem>(guiToolbarItem, mousePaintActions, toolItem));

            // Do prvku nastavíme jeho požadované chování, pokud to aplikační kód nezvládl (tj. přepíšeme některé definice):
            guiToolbarItem.IsCheckable = true;                       // Prvek musí pracovat v režimu CheckBoxu
            guiToolbarItem.IsChecked = false;                        // Výchozí stav je NonChecked
            guiToolbarItem.StoreValueToConfig = false;               // Hodnota se nepersistuje
        }
        /// <summary>
        /// Na daném prvku Toolbaru došlo ke změně stavu IsChecked. Pokud má prvek vliv na aktivitu MousePaint, pak aplikace reaguje zde.
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        private void _MousePaintToolBarSelectedChange(GuiToolbarItem guiToolbarItem)
        {
            if (this._MousePaintToolBarItems == null) return;        // Žádný prvek ToolBaru není evidován jako EnableMousePaint*
            GuiActionType mousePaintAction = _MousePaintToolBarGetAction(guiToolbarItem);
            bool isActive = guiToolbarItem?.IsChecked ?? false;
            _MousePaintSetActiveTool(mousePaintAction, isActive, false);
        }
        /// <summary>
        /// Obsahuje true, pokud je aktivní režim MousePaint:LinkLine
        /// Tuto property není možno nastavit z aplikačního kódu na true, pouze na false.
        /// </summary>
        public bool MousePaintLinkLineActive
        {
            get { return this._MousePaintLinkLineActive; }
            set { _MousePaintSetActiveTool(GuiActionType.EnableMousePaintLinkLine, value, true); }
        }
        /// <summary>
        /// Obsahuje true, pokud je aktivní režim MousePaint:Rectangle
        /// Tuto property není možno nastavit z aplikačního kódu na true, pouze na false.
        /// </summary>
        public bool MousePaintRectangleActive
        {
            get { return this._MousePaintRectangleActive; }
            set { _MousePaintSetActiveTool(GuiActionType.EnableMousePaintRectangle, value, true); }
        }
        /// <summary>
        /// Nastaví aktivní režim kreslení MousePaint.
        /// </summary>
        /// <param name="mousePaintAction"></param>
        /// <param name="isActive"></param>
        /// <param name="refreshToolBar"></param>
        private void _MousePaintSetActiveTool(GuiActionType mousePaintAction, bool isActive, bool refreshToolBar)
        {
            // Do každého z prvků, které mají vliv na MousePaint, nastavím jeho IsChecked = (pokud má být aktivován, pak true pro prvek odpovídající dané akci, jinak false:
            this._MousePaintToolBarItems.ForEachItem(t => t.Item1.IsChecked = (isActive ? (_MousePaintToolBarGetAction(t.Item2) == mousePaintAction) : false));

            // Nyní nastavíme příznaky kreslení podle prvku toolbaru (jeho akce a jeho stav):
            this._MousePaintLinkLineActive = (isActive && mousePaintAction.HasFlag(GuiActionType.EnableMousePaintLinkLine));
            this._MousePaintRectangleActive = (isActive && !this._MousePaintLinkLineActive && mousePaintAction.HasFlag(GuiActionType.EnableMousePaintRectangle));
            this._MainControl.MousePaintEnabled = (this._MousePaintLinkLineActive || this._MousePaintRectangleActive);

            if (refreshToolBar) this._MainControl.RefreshToolBar(ToolBarRefreshMode.RefreshControl);
        }
        /// <summary>
        /// Je aktivní MousePaint typu Kreslení linky?
        /// </summary>
        private bool _MousePaintLinkLineActive;
        /// <summary>
        /// Je aktivní MousePaint typu Kreslení Rectangle?
        /// </summary>
        private bool _MousePaintRectangleActive;
        /// <summary>
        /// Metoda vrátí akce typu EnableMousePaint* z daného prvku Toolbaru
        /// </summary>
        /// <param name="guiToolbarItem"></param>
        /// <returns></returns>
        private GuiActionType _MousePaintToolBarGetAction(GuiToolbarItem guiToolbarItem)
        {
            GuiActionType? action = guiToolbarItem?.GuiActions;
            if (!action.HasValue) return GuiActionType.None;
            return _MousePaintToolBarGetAction(action);
        }
        /// <summary>
        /// Metoda vrátí akce typu EnableMousePaint* z daného prvku Toolbaru
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private GuiActionType _MousePaintToolBarGetAction(GuiActionType? action)
        {
            if (!action.HasValue) return GuiActionType.None;
            return (action.Value & (GuiActionType.EnableMousePaintLinkLine | GuiActionType.EnableMousePaintRectangle));
        }
        /// <summary>
        /// Prvky ToolBaru, které mají nějakou akci EnableMousePaint*
        /// Item1 = definice GUI;
        /// Item2 = fyzický prvek GUI (může být null, když Toolbar je řešen externě v Ribbonu);
        /// Item3 = akce
        /// </summary>
        private List<Tuple<GuiToolbarItem, GuiActionType, ToolBarItem>> _MousePaintToolBarItems;
        /// <summary>
        /// Inicializace subsystému interaktivního kreslení.
        /// Volá se po úplném vytvoření a naplnění controlu podle dat z <see cref="GuiData"/>.
        /// </summary>
        private void _InteractiveMousePaintInit()
        {
            this._MainControl.MousePaintProcessStart += _InteractiveMousePaintProcessStart;
            this._MainControl.MousePaintProcessTarget += _InteractiveMousePaintProcessTarget;
            this._MainControl.MousePaintProcessCommit += _InteractiveMousePaintProcessCommit;
            this._MainControl.MousePaintEnabled = false;
        }
        /// <summary>
        /// Aplikace zde zjistí, zda v daném bodě controlu je možno zahájit operaci MousePaint = uživatelsky nakreslit nějaký tvar, a následně jej převzít k dalšímu zpracování.
        /// Myš se nyní nachází na bodě, kde by uživatel mohl zmáčknout myš (nebo ji už dokonce právě nyní zmáčkl, 
        /// to když <see cref="GInteractiveMousePaintArgs.InteractiveChange"/> == <see cref="GInteractiveChangeState.LeftDown"/> nebo RightDown).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _InteractiveMousePaintProcessStart(object sender, GInteractiveMousePaintArgs e)
        {
            MainDataTable dataTable;
            if (!InteractiveMousePaintTryGetTable(e.CurrentItem, out dataTable)) return; // Veškeré aktivity řídí ta tabulka, na které se nachází StartItem (v metodě Start je to CurrentItem), tedy kde proces MousePaint začal...
            dataTable.InteractiveMousePaintProcessStart(e);
        }
        /// <summary>
        /// Aplikace zde zjistí, zda v daném bodě controlu je možno dokončit kreslení, tedy zda daný bod a prvek na něm je vhodným cílem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _InteractiveMousePaintProcessTarget(object sender, GInteractiveMousePaintArgs e)
        {
            MainDataTable dataTable;
            if (!InteractiveMousePaintTryGetTable(e.StartItem, out dataTable)) return;   // Veškeré aktivity řídí ta tabulka, na které se nachází StartItem, tedy kde proces MousePaint začal, nikoli ta tabulka, kde se právě nachází myš!!!
            dataTable.InteractiveMousePaintProcessTarget(e);
        }
        /// <summary>
        /// Aplikace zde zajistí zpracování vykresleného tvaru MousePaint = tedy převezme si souřadnice a prvky, a vytvoří z nich nějaká data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _InteractiveMousePaintProcessCommit(object sender, GInteractiveMousePaintArgs e)
        {
            MainDataTable dataTable;
            if (!InteractiveMousePaintTryGetTable(e.StartItem, out dataTable)) return;   // Veškeré aktivity řídí ta tabulka, na které se nachází StartItem, tedy kde proces MousePaint začal, nikoli ta tabulka, kde se právě nachází myš!!!
            dataTable.InteractiveMousePaintProcessCommit(e);
        }
        /// <summary>
        /// Metoda pro daný interaktivní prvek (prvek grafu, graf, řádek/sloupec tabulky, tabulka) najde jeho odpovídající <see cref="MainDataTable"/>.
        /// Vrací true, když je datová tabulka nalezena.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        internal static bool InteractiveMousePaintTryGetTable(IInteractiveItem item, out MainDataTable dataTable)
        {
            dataTable = null;
            if (item == null) return false;
            GTable table = InteractiveObject.SearchForParent(item, typeof(GTable)) as GTable;
            if (table == null) return false;
            dataTable = table.DataTable.UserData as MainDataTable;
            return (dataTable != null);
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
            currentState.ActivePage = this._MainControl.ActiveGuiPage?.FullName;
            currentState.TimeAxisValue = this.SynchronizedTime;
            currentState.CheckedRows = this._CreateGuiCurrentCheckedRows();
            currentState.SelectedGraphItems = this._CreateGuiCurrentSelectedGraphItems();
            return currentState;
        }
        /// <summary>
        /// Metoda najde a vrátí pole identifikátorů řádků ze všech tabulek, které jsou aktuálně označené.
        /// </summary>
        /// <returns></returns>
        private GuiGridRowId[] _CreateGuiCurrentCheckedRows()
        {
            List<GuiGridRowId> selectedRowList = new List<GuiGridRowId>();
            foreach (var mainTable in this.DataTables)
                selectedRowList.AddRange(mainTable.CheckedRowsId);
            return selectedRowList.ToArray();
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
        protected GuiDialogButtons ProcessGuiResponse(GuiResponse guiResponse)
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
            Dictionary<uint, TimeGraph> repaintGraphDict = new Dictionary<uint, TimeGraph>();
            this.Colorize(guiResponse);
            this._ProcessResponseCommon(guiResponse.Common, mainTableDict);
            this._ProcessResponseToolbarItems(guiResponse.ToolbarItems);
            this._ProcessResponseTime(guiResponse.TimeAxisValue);
            this._ProcessResponseRefreshRows(guiResponse.RefreshRows, mainTableDict, repaintGraphDict);
            this._ProcessResponseRefreshGraphs(guiResponse.RefreshGraphs, mainTableDict, repaintGraphDict);
            this._ProcessResponseRefreshGraphItems(guiResponse.RefreshGraphItems, mainTableDict, repaintGraphDict);
            this._ProcessResponseUpdateLinks(guiResponse.ChangeLinks, mainTableDict, repaintGraphDict);
            this._ProcessResponseExpandRows(guiResponse.ExpandRows, mainTableDict, repaintGraphDict);
            this._ProcessResponseRefreshProperties(guiResponse.RefreshProperties, mainTableDict, repaintGraphDict);
            this._ProcessResponseRepaintGraphs(repaintGraphDict.Values);
            this._ProcessResponseRecalcGrids(repaintGraphDict, mainTableDict);
            this._ProcessResponseRepaintParentChilds(repaintGraphDict.Values);
            this._ProcessResponseRepaintControl(repaintGraphDict.Values);
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
                    TimeGraphLinkArray graphLink = table.GraphLinkArray;
                    if (graphLink != null)
                        graphLink.Clear();
                });
            }

            if (common.ClearSelected)
            {   // Odselectovat všechny selectované prvky:
                this._MainControl.Selector.ClearSelected();
            }

            if (common.ClearLinks && !common.ClearSelected)
            {   // Najít prvky, které jsou Selected, a obnovit jejich Linky:
                foreach (var item in this._MainControl.Selector.SelectedItems)
                {
                    TimeGraphItem graphItem = item as TimeGraphItem;
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
        private void _ProcessResponseToolbarItems(IEnumerable<GuiToolbarItem> toolbarItems)
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
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.RefreshRows"/>
        /// </summary>
        /// <param name="refreshRows"></param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="repaintGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseRefreshRows(IEnumerable<GuiRefreshRow> refreshRows, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, TimeGraph> repaintGraphDict)
        {
            if (refreshRows == null) return;
            foreach (GuiRefreshRow refreshRow in refreshRows)
            {
                if (refreshRow == null || refreshRow.GridRowId == null || refreshRow.GridRowId.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(refreshRow.GridRowId.TableName, out mainDataTable))
                    mainDataTable.RefreshRow(refreshRow, repaintGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.RefreshGraphs"/>
        /// </summary>
        /// <param name="refreshGraphs"></param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="repaintGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseRefreshGraphs(IEnumerable<GuiRefreshGraph> refreshGraphs, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, TimeGraph> repaintGraphDict)
        {
            if (refreshGraphs == null) return;
            foreach (GuiRefreshGraph refreshGraph in refreshGraphs)
            {
                if (refreshGraph == null || refreshGraph.GridRowId == null || refreshGraph.GridRowId.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(refreshGraph.GridRowId.TableName, out mainDataTable))
                    mainDataTable.RefreshGraph(refreshGraph, repaintGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.RefreshGraphItems"/>
        /// </summary>
        /// <param name="refreshGraphItems"></param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="repaintGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseRefreshGraphItems(IEnumerable<GuiRefreshGraphItem> refreshGraphItems, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, TimeGraph> repaintGraphDict)
        {
            if (refreshGraphItems == null) return;
            foreach (GuiRefreshGraphItem refreshGraphItem in refreshGraphItems)
            {
                if (refreshGraphItem == null || refreshGraphItem.GridItemId == null || refreshGraphItem.GridItemId.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(refreshGraphItem.GridItemId.TableName, out mainDataTable))
                    mainDataTable.RefreshGraphItem(refreshGraphItem, repaintGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.ChangeLinks"/>
        /// </summary>
        /// <param name="changeLinks">Změněné vztahy</param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="repaintGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseUpdateLinks(IEnumerable<GuiGraphLink> changeLinks, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, TimeGraph> repaintGraphDict)
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
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.ExpandRows"/>
        /// </summary>
        /// <param name="expandRows">Řádky, které mají být Expanded</param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="repaintGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseExpandRows(IEnumerable<GuiGridRowId> expandRows, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, TimeGraph> repaintGraphDict)
        {
            if (expandRows == null) return;
            foreach (GuiGridRowId expandRow in expandRows)
            {
                if (expandRow == null || expandRow.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(expandRow.TableName, out mainDataTable))
                    mainDataTable.ExpandRow(expandRow, repaintGraphDict);
            }
        }
        /// <summary>
        /// Zpracuje odpověď z aplikace, část: <see cref="GuiResponse.RefreshProperties"/>
        /// </summary>
        /// <param name="refreshProperties">Řádky, které mají být Expanded</param>
        /// <param name="mainTableDict">Index tabulek podle jejich jména</param>
        /// <param name="repaintGraphDict">Index grafů, kterých se týkají změny, a na nichž na závěr provedeme Refresh</param>
        private void _ProcessResponseRefreshProperties(IEnumerable<GuiRefreshProperty> refreshProperties, Dictionary<string, MainDataTable> mainTableDict, Dictionary<uint, TimeGraph> repaintGraphDict)
        {
            if (refreshProperties == null) return;
            foreach (GuiRefreshProperty refreshProperty in refreshProperties)
            {
                if (refreshProperty == null || refreshProperty.GridItemId == null || refreshProperty.GridItemId.TableName == null) continue;
                MainDataTable mainDataTable;
                if (mainTableDict.TryGetValue(refreshProperty.GridItemId.TableName, out mainDataTable))
                    mainDataTable.RefreshProperty(refreshProperty, repaintGraphDict);
            }
        }
        /// <summary>
        /// Metoda provede Refresh na všech grafech, které se nacházejí v parametru refreshGraphDict.
        /// </summary>
        /// <param name="repaintGraphs"></param>
        private void _ProcessResponseRepaintGraphs(IEnumerable<TimeGraph> repaintGraphs)
        {
            TimeGraph.InvalidateItems invalidateParts = 
                TimeGraph.InvalidateItems.CoordinateX | TimeGraph.InvalidateItems.AllGroups |
                TimeGraph.InvalidateItems.CoordinateYVirtual | TimeGraph.InvalidateItems.CoordinateYReal;
            repaintGraphs.ForEachItem(g => g.Invalidate(invalidateParts));   // Nedávám Refresh, to by došlo k opakovanému Refresh() Hosta (Controlu)!
        }
        /// <summary>
        /// Metoda zajistí přepočet souřadnic v gridech pro dané grafy
        /// </summary>
        /// <param name="repaintGraphDict"></param>
        /// <param name="mainTableDict"></param>
        private void _ProcessResponseRecalcGrids(Dictionary<uint, TimeGraph> repaintGraphDict, Dictionary<string, MainDataTable> mainTableDict)
        {
            if (repaintGraphDict == null || repaintGraphDict.Count == 0) return;

            InvalidateItem invalidate = InvalidateItem.RowHeight;
            foreach (var table in this.DataTables)
            {
                table.GGrid.Invalidate(invalidate);
            }
        }
        /// <summary>
        /// Metoda zajistí nové vyhodnocení vztahů Parent - Child v těch tabulkách, kde došlo ke změně v grafech
        /// </summary>
        /// <param name="repaintGraphs"></param>
        private void _ProcessResponseRepaintParentChilds(IEnumerable<TimeGraph> repaintGraphs)
        {
            this.DataTables.ForEachItem(t => t.PrepareDynamicChilds(true));

            /*   Následující kód je slabý, protože provede PrepareDynamicChilds jen pro tabulky, kde došlo ke změně.
                 Ale změna v tabulce "A" se může promítat do Child řádků tabulky "B", takže raději provedu PrepareDynamicChilds pro všechny tabulky:
                
            // Z objektů grafů, kde došlo ke změně, s pomocí GTimeGraph.UserData, což je reference na MainDataTable, získám distinct seznam tabulek:
            MainDataTable[] tables = refreshGraphs
                .Where(g => g.UserData != null && g.UserData is MainDataTable)
                .Select(g => g.UserData as MainDataTable)
                .GetDictionary(t => t.TableName, true)
                .Values
                .ToArray();

            // Pro každou z tabulek zavolám metodu pro vyhodnocení jejich Parent - Child:
            tables.ForEachItem(t => t.PrepareDynamicChilds());
            */
        }
        /// <summary>
        /// Provede fyzický Refresh celého Controlu
        /// </summary>
        /// <param name="repaintGraphs"></param>
        private void _ProcessResponseRepaintControl(IEnumerable<TimeGraph> repaintGraphs)
        {
            this._MainControl.Refresh(true);
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
        #region Colorize
        /// <summary>
        /// Metoda zajistí aplikaci stylu na daný prvek, ať už je to cokoliv
        /// </summary>
        /// <param name="data"></param>
        protected void Colorize(object data)
        {
            if (!_HasStyles) return;
            if (data is GuiGrid guiGrid) { this.Colorize(guiGrid); return; }
            if (data is GuiGridProperties gridProperties) { this.Colorize(gridProperties); return; }
            if (data is GuiDataTable dataTable) { this.Colorize(dataTable); return; }
            if (data is IEnumerable<GuiDataRow> dataRows) { this.Colorize(dataRows); return; }
            if (data is GuiDataRow dataRow) { this.Colorize(dataRow); return; }
            if (data is IEnumerable<GuiGraphLink> graphLinks) { this.Colorize(graphLinks); return; }
            if (data is GuiGraphLink graphLink) { this.Colorize(graphLink); return; }
            if (data is GuiGraph graph) { this.Colorize(graph); return; }
            if (data is GuiGraphProperties graphProperties) { this.Colorize(graphProperties); return; }
            if (data is IEnumerable<GuiGraphItem> graphItems) { this.Colorize(graphItems); return; }
            if (data is GuiGraphItem graphItem) { this.Colorize(graphItem); return; }
            if (data is GuiGraphSkin graphSkin) { this.Colorize(graphSkin); return; }
            if (data is IEnumerable<GuiTagItem> tagItems) { this.Colorize(tagItems); return; }
            if (data is GuiTagItem tagItem) { this.Colorize(tagItem); return; }
            if (data is GuiResponse guiResponse) { this.Colorize(guiResponse); return; }
            if (data is IEnumerable<GuiRefreshRow> refreshRows) { this.Colorize(refreshRows); return; }
            if (data is GuiRefreshRow refreshRow) { this.Colorize(refreshRow); return; }
            if (data is IEnumerable<GuiRefreshGraph> refreshGraphs) { this.Colorize(refreshGraphs); return; }
            if (data is GuiRefreshGraph refreshGraph) { this.Colorize(refreshGraph); return; }
            if (data is IEnumerable<GuiRefreshGraphItem> refreshGraphItems) { this.Colorize(refreshGraphItems); return; }
            if (data is GuiRefreshGraphItem refreshGraphItem) { this.Colorize(refreshGraphItem); return; }
        }
        /// <summary>
        /// Vrátí požadovanou barvu pro dané jméno stylu a danou barvu.
        /// Aplikuje hodnotu <paramref name="transparency"/>.
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="stylePart"></param>
        /// <param name="color"></param>
        /// <param name="transparency">Pokud bude null, pak prvek barva pozadí bude "plná" = neprůhledná, stejně jako při hodnotě 0.
        /// Hodnota má rozsah 0 = neprůhledná až 100 (=100%) = úplně průhledná, pak je vidět vše, co je pod prvkem, jako by barva nebyla zadaná.</param>
        /// <returns></returns>
        protected Color? GetStylePartColor(string styleName, StylePartType stylePart, Color? color, int? transparency)
        {
            Color? resultColor = GetStylePartColor(styleName, stylePart, color);
            if (resultColor.HasValue && transparency.HasValue)
            {   // Aplikujeme průhlednost: převedeme hodnotu transparency z { 0 ÷ 100 } na hodnotu Alpha { 255 ÷ 0 }:
                int alpha = ConvertTransparencyToAlpha(transparency.Value);
                resultColor = Color.FromArgb(alpha, resultColor.Value.R, resultColor.Value.G, resultColor.Value.B);
            }
            return resultColor;
        }
        /// <summary>
        /// Převede hodnotu transparency z { 0 ÷ 100 } na hodnotu Alpha { 255 ÷ 0 }:
        /// </summary>
        /// <param name="transparency"></param>
        /// <returns></returns>
        protected int ConvertTransparencyToAlpha(int transparency)
        {
            if (transparency <= 0) return 255;             // Průhlednost =   0  =>  Alpha (tj. krytí) = 255 = plná barva
            if (transparency >= 100) return 0;             // Průhlednost = 100  =>  Alpha (tj. krytí) =   0 = průhledná barva
            decimal ratio = (100m - (decimal)transparency) / 100m;
            return (int)Math.Round((255m * ratio), 0);
        }
        /// <summary>
        /// Vrátí požadovanou barvu pro dané jméno stylu a danou barvu
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="stylePart"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color? GetStylePartColor(string styleName, StylePartType stylePart, Color? color)
        {
            if (String.IsNullOrEmpty(styleName) || !_HasStyles) return color;
            if (this._AppHost.TryGetStylePartValue(styleName, stylePart, out var value))
            {
                if (value is Color) return (Color)value;
                if (value is Color?) return (Color?)value;
            }
            return color;
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGrid"/>
        /// </summary>
        /// <param name="guiGrid"></param>
        protected void Colorize(GuiGrid guiGrid)
        {
            if (guiGrid == null || !_HasStyles) return;

            this.Colorize(guiGrid.GraphProperties);
            this.Colorize(guiGrid.GridProperties);
            this.Colorize(guiGrid.RowTable?.Rows);
            this.Colorize(guiGrid.RowTable?.TagItems);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGridProperties"/>
        /// </summary>
        /// <param name="gridProperties"></param>
        protected void Colorize(GuiGridProperties gridProperties)
        {
            if (gridProperties == null || !_HasStyles) return;
            gridProperties.TagFilterBackColor = this.GetStylePartColor(gridProperties.TagFilterBackColorName, StylePartType.TextAreaBackColor, gridProperties.TagFilterBackColor);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiDataTable"/>
        /// </summary>
        /// <param name="dataTable"></param>
        protected void Colorize(GuiDataTable dataTable)
        {
            if (dataTable == null || !_HasStyles) return;
            dataTable.TreeViewLinkColor = this.GetStylePartColor(dataTable.TreeViewLinkColorName, StylePartType.TextAreaBackColor, dataTable.TreeViewLinkColor);
            Colorize(dataTable.Rows);
            Colorize(dataTable.GraphLinks);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiDataRow"/>
        /// </summary>
        /// <param name="dataRows"></param>
        protected void Colorize(IEnumerable<GuiDataRow> dataRows)
        {
            if (dataRows == null || !_HasStyles) return;
            dataRows.ForEachItem(row => Colorize(row));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiDataRow"/>
        /// </summary>
        /// <param name="dataRow"></param>
        protected void Colorize(GuiDataRow dataRow)
        {
            if (dataRow == null || !_HasStyles) return;
            Colorize(dataRow.Graph);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiGraphLink"/>
        /// </summary>
        /// <param name="graphLinks"></param>
        protected void Colorize(IEnumerable<GuiGraphLink> graphLinks)
        {
            if (graphLinks == null || !_HasStyles) return;
            graphLinks.ForEachItem(graphLink => Colorize(graphLink));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGraphLink"/>
        /// </summary>
        /// <param name="graphLink"></param>
        protected void Colorize(GuiGraphLink graphLink)
        {
            if (graphLink == null || !_HasStyles) return;
            graphLink.LinkColorStandard = this.GetStylePartColor(graphLink.LinkColorStandardName, StylePartType.TextAreaBackColor, graphLink.LinkColorStandard);
            graphLink.LinkColorWarning = this.GetStylePartColor(graphLink.LinkColorWarningName, StylePartType.TextAreaBackColor, graphLink.LinkColorWarning);
            graphLink.LinkColorError = this.GetStylePartColor(graphLink.LinkColorErrorName, StylePartType.TextAreaBackColor, graphLink.LinkColorError);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGraph"/>
        /// </summary>
        /// <param name="graph"></param>
        protected void Colorize(GuiGraph graph)
        {
            if (graph == null || !_HasStyles) return;
            Colorize(graph.GraphProperties);
            graph.BackgroundColor = this.GetStylePartColor(graph.BackgroundColorName, StylePartType.TextAreaBackColor, graph.BackgroundColor);
            graph.BeginShadowColor = this.GetStylePartColor(graph.BeginShadowColorName, StylePartType.TextAreaBackColor, graph.BeginShadowColor);
            graph.EndShadowColor = this.GetStylePartColor(graph.EndShadowColorName, StylePartType.TextAreaBackColor, graph.EndShadowColor);
            Colorize(graph.GraphItems);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGraphProperties"/>
        /// </summary>
        /// <param name="graphProperties"></param>
        protected void Colorize(GuiGraphProperties graphProperties)
        {
            if (graphProperties == null || !_HasStyles) return;
            graphProperties.TimeAxisBackColor = this.GetStylePartColor(graphProperties.TimeAxisBackColorName, StylePartType.TextAreaBackColor, graphProperties.TimeAxisBackColor);
            graphProperties.LinkColorStandard = this.GetStylePartColor(graphProperties.LinkColorStandardName, StylePartType.TextAreaBackColor, graphProperties.LinkColorStandard);
            graphProperties.LinkColorWarning = this.GetStylePartColor(graphProperties.LinkColorWarningName, StylePartType.TextAreaBackColor, graphProperties.LinkColorWarning);
            graphProperties.LinkColorError = this.GetStylePartColor(graphProperties.LinkColorErrorName, StylePartType.TextAreaBackColor, graphProperties.LinkColorError);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiGraphItem"/>
        /// </summary>
        /// <param name="graphItems"></param>
        protected void Colorize(IEnumerable<GuiGraphItem> graphItems)
        {
            if (graphItems == null || !_HasStyles) return;
            graphItems.ForEachItem(item => Colorize(item));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGraphItem"/>
        /// </summary>
        /// <param name="graphItem"></param>
        protected void Colorize(GuiGraphItem graphItem)
        {
            if (graphItem == null || !_HasStyles) return;
            Colorize(graphItem.SkinDefault);
            graphItem.SkinDict?.Values.ForEachItem(skin => Colorize(skin));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiGraphSkin"/>
        /// </summary>
        /// <param name="graphSkin"></param>
        protected void Colorize(GuiGraphSkin graphSkin)
        {
            if (graphSkin == null || !_HasStyles) return;
            graphSkin.TextColor = this.GetStylePartColor(graphSkin.TextColorName, StylePartType.TextAreaForeColor, graphSkin.TextColor);
            graphSkin.BackColor = this.GetStylePartColor(graphSkin.BackColorName, StylePartType.TextAreaBackColor, graphSkin.BackColor, graphSkin.BackColorTransparency);
            graphSkin.HatchColor = this.GetStylePartColor(graphSkin.HatchColorName, StylePartType.TextAreaBackColor, graphSkin.HatchColor, graphSkin.HatchColorTransparency);
            graphSkin.LineColor = this.GetStylePartColor(graphSkin.LineColorName, StylePartType.TextAreaForeColor, graphSkin.LineColor);
            graphSkin.RatioBeginBackColor = this.GetStylePartColor(graphSkin.RatioBeginBackColorName, StylePartType.TextAreaBackColor, graphSkin.RatioBeginBackColor, graphSkin.RatioBeginBackColorTransparency);
            graphSkin.RatioEndBackColor = this.GetStylePartColor(graphSkin.RatioEndBackColorName, StylePartType.TextAreaBackColor, graphSkin.RatioEndBackColor, graphSkin.RatioEndBackColorTransparency);
            graphSkin.RatioLineColor = this.GetStylePartColor(graphSkin.RatioLineColorName, StylePartType.TextAreaForeColor, graphSkin.RatioLineColor);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiTagItem"/>
        /// </summary>
        /// <param name="tagItems"></param>
        protected void Colorize(IEnumerable<GuiTagItem> tagItems)
        {
            if (tagItems == null || !_HasStyles) return;
            tagItems.ForEachItem(tag => Colorize(tag));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiTagItem"/>
        /// </summary>
        /// <param name="tagItem"></param>
        protected void Colorize(GuiTagItem tagItem)
        {
            if (tagItem == null || !_HasStyles) return;
            tagItem.BackColor = this.GetStylePartColor(tagItem.BackColorName, StylePartType.TextAreaBackColor, tagItem.BackColor);
            tagItem.BackColorChecked = this.GetStylePartColor(tagItem.BackColorCheckedName, StylePartType.TextAreaBackColor, tagItem.BackColorChecked);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiResponse"/>
        /// </summary>
        /// <param name="guiResponse"></param>
        protected void Colorize(GuiResponse guiResponse)
        {
            if (guiResponse == null || !_HasStyles) return;
            this.Colorize(guiResponse.RefreshRows);
            this.Colorize(guiResponse.RefreshGraphs);
            this.Colorize(guiResponse.RefreshGraphItems);
            this.Colorize(guiResponse.ChangeLinks);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiRefreshRow"/>
        /// </summary>
        /// <param name="refreshRows"></param>
        protected void Colorize(IEnumerable<GuiRefreshRow> refreshRows)
        {
            if (refreshRows == null || !_HasStyles) return;
            refreshRows.ForEachItem(refreshRow => Colorize(refreshRow));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiRefreshRow"/>
        /// </summary>
        /// <param name="refreshRow"></param>
        protected void Colorize(GuiRefreshRow refreshRow)
        {
            if (refreshRow == null || !_HasStyles) return;
            Colorize(refreshRow.RowData);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiRefreshGraph"/>
        /// </summary>
        /// <param name="refreshGraphs"></param>
        protected void Colorize(IEnumerable<GuiRefreshGraph> refreshGraphs)
        {
            if (refreshGraphs == null || !_HasStyles) return;
            refreshGraphs.ForEachItem(refreshGraph => Colorize(refreshGraph));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiRefreshGraph"/>
        /// </summary>
        /// <param name="refreshGraph"></param>
        protected void Colorize(GuiRefreshGraph refreshGraph)
        {
            if (refreshGraph == null || !_HasStyles) return;
            Colorize(refreshGraph.GraphData);
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro prvky v kolekci <see cref="GuiRefreshGraphItem"/>
        /// </summary>
        /// <param name="refreshGraphItems"></param>
        protected void Colorize(IEnumerable<GuiRefreshGraphItem> refreshGraphItems)
        {
            if (refreshGraphItems == null || !_HasStyles) return;
            refreshGraphItems.ForEachItem(refreshGraphItem => Colorize(refreshGraphItem));
        }
        /// <summary>
        /// Naplní barvy dle kalíšků pro <see cref="GuiRefreshGraphItem"/>
        /// </summary>
        /// <param name="refreshGraphItem"></param>
        protected void Colorize(GuiRefreshGraphItem refreshGraphItem)
        {
            if (refreshGraphItem == null || !_HasStyles) return;
            Colorize(refreshGraphItem.ItemData);
        }
        #endregion
        #region Zavírání hlavního okna
        /// <summary>
        /// Event před zavřením okna: pošleme informaci hostiteli a počkáme si na odpověď, jinak se okno zavře.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainFormClosing(object sender, WinForms.FormClosingEventArgs e)
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
        private bool _MainFormClosingTest(WinForms.FormClosingEventArgs e)
        {
            // Některé důvody zavření okna jsou nadřazené, tam zavření okna nebudeme bránit a ani nebudeme provádět uzavírací algoritmus:
            switch (e.CloseReason)
            {
                case WinForms.CloseReason.TaskManagerClosing:
                case WinForms.CloseReason.WindowsShutDown:
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
            GuiDialogButtons dialogResult = this.ProcessGuiResponse(responseArgs.GuiResponse);

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
                GuiDialogButtons response = this.ProcessGuiResponse(responseArgs.GuiResponse);
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
        private void _MainFormClosed(object sender, WinForms.FormClosedEventArgs e)
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
        /// Provede akci po stisku klávesy
        /// </summary>
        /// <param name="action"></param>
        /// <param name="table"></param>
        /// <param name="objectFullName"></param>
        /// <param name="objectId"></param>
        void IMainDataInternal.RunKeyAction(GuiKeyAction action, MainDataTable table, string objectFullName, GuiId objectId) { this._RunKeyAction(action, table, objectFullName, objectId); }
        /// <summary>
        /// Metoda zavolá hostitele a předá mu požadavek.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        /// <param name="blockGuiTime"></param>
        void IMainDataInternal.CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction, TimeSpan? blockGuiTime) { this._CallAppHostFunction(request, callBackAction, blockGuiTime); }
        /// <summary>
        /// Metoda zajistí aplikaci stylu na daný prvek, ať už je to cokoliv
        /// </summary>
        /// <param name="data"></param>
        void IMainDataInternal.Colorize(object data) { this.Colorize(data); }
        /// <summary>
        /// Vrátí požadovanou barvu pro dané jméno stylu a danou barvu
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="stylePart"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        Color? IMainDataInternal.GetStylePartColor(string styleName, StylePartType stylePart, Color? color) { return this.GetStylePartColor(styleName, stylePart, color); }
        /// <summary>
        /// Zpracuje odpověď z aplikační vrstvy
        /// </summary>
        /// <param name="response"></param>
        void IMainDataInternal.ProcessAppHostResponse(GuiResponse response) { this.ProcessGuiResponse(response); }
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="contextMenu">Data popisující prvek a čas kliknutí</param>
        /// <param name="itemClickedHandler">Externí handler pro obsluhu kliknutí na položku menu. Default = null, použije se interní. Pokud bude zadán, pak se interní handler nepoužije.</param>
        /// <returns></returns>
        WinForms.ToolStripDropDownMenu IMainDataInternal.CreateContextMenu(GuiContextMenuRunArgs contextMenu, WinForms.ToolStripItemClickedEventHandler itemClickedHandler) { return this.CreateContextMenu(contextMenu, itemClickedHandler); }
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
        #endregion
    }
    #region interface IMainDataInternal
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
        /// Provede akci po stisku klávesy
        /// </summary>
        /// <param name="action"></param>
        /// <param name="table"></param>
        /// <param name="objectFullName"></param>
        /// <param name="objectId"></param>
        void RunKeyAction(GuiKeyAction action, MainDataTable table, string objectFullName, GuiId objectId);
        /// <summary>
        /// Metoda zavolá hostitele a předá mu požadavek.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callBackAction"></param>
        /// <param name="blockGuiTime"></param>
        void CallAppHostFunction(GuiRequest request, Action<AppHostResponseArgs> callBackAction, TimeSpan? blockGuiTime);
        /// <summary>
        /// Metoda zajistí aplikaci stylu na daný prvek, ať už je to cokoliv
        /// </summary>
        /// <param name="data"></param>
        void Colorize(object data);
        /// <summary>
        /// Vrátí požadovanou barvu pro dané jméno stylu a danou barvu
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="stylePart"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        Color? GetStylePartColor(string styleName, StylePartType stylePart, Color? color);
        /// <summary>
        /// Zpracuje odpověď z aplikační vrstvy
        /// </summary>
        /// <param name="response"></param>
        void ProcessAppHostResponse(GuiResponse response);
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="contextMenu">Data popisující prvek a čas kliknutí</param>
        /// <param name="itemClickedHandler">Externí handler pro obsluhu kliknutí na položku menu. Default = null, použije se interní. Pokud bude zadán, pak se interní handler nepoužije.</param>
        /// <returns></returns>
        WinForms.ToolStripDropDownMenu CreateContextMenu(GuiContextMenuRunArgs contextMenu, WinForms.ToolStripItemClickedEventHandler itemClickedHandler);
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
    }
    #endregion
    #region class MainDataPanel - třída obsahující data o jednom panelu.
    /// <summary>
    /// MainDataPanel - třída obsahující data o jednom panelu.
    /// Obsahuje referenci na vstupní data <see cref="GuiPage"/>, referenci na záložku <see cref="GTabPage"/> 
    /// i referenci na vlastní vizuální control <see cref="SchedulerPanel"/>.
    /// </summary>
    public class MainDataPanel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="guiPage"></param>
        /// <param name="tabPageIndex"></param>
        /// <param name="gTabPage"></param>
        /// <param name="schedulerPanel"></param>
        public MainDataPanel(GuiPage guiPage, int tabPageIndex, Components.TabPage gTabPage, SchedulerPanel schedulerPanel)
        {
            this.GuiPage = guiPage;
            this.TabPageIndex = tabPageIndex;
            this.GTabPage = gTabPage;
            this.SchedulerPanel = schedulerPanel;
        }
        /// <summary>
        /// Vstupní data pro tento panel
        /// </summary>
        public GuiPage GuiPage { get; private set; }
        /// <summary>
        /// Index záložky
        /// </summary>
        public int TabPageIndex { get; private set; }
        /// <summary>
        /// Objekt záložky obsahující panel
        /// </summary>
        public TabPage GTabPage { get; private set; }
        /// <summary>
        /// Data panelu
        /// </summary>
        public SchedulerPanel SchedulerPanel { get; private set; }
        /// <summary>
        /// Levý panel záložek
        /// </summary>
        public TabContainer LeftPanelTabs { get { return this.SchedulerPanel.LeftPanelTabs; } }
        /// <summary>
        /// Hlavní Grid
        /// </summary>
        public GGrid MainPanelGrid { get { return this.SchedulerPanel.MainPanelGrid; } }
        /// <summary>
        /// Pravý panel záložek
        /// </summary>
        public TabContainer RightPanelTabs { get { return this.SchedulerPanel.RightPanelTabs; } }
        /// <summary>
        /// Dolní panel záložek
        /// </summary>
        public TabContainer BottomPanelTabs { get { return this.SchedulerPanel.BottomPanelTabs; } }

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
        public TimeGraph SourceGraph { get; set; }
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
        public TimeGraph TargetGraph { get; set; }
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
    /// <summary>
    /// Jednotlivé stavy života MainControlu a MainData
    /// </summary>
    public enum DataStateType : int
    {
        /// <summary>
        /// Na začátku konstruktoru
        /// </summary>
        None = 0,
        /// <summary>
        /// Průběh konstruktoru
        /// </summary>
        Initialising,
        /// <summary>
        /// Příprava Configu
        /// </summary>
        PrepareConfig,
        /// <summary>
        /// Inicializováno
        /// </summary>
        Initialised,
        /// <summary>
        /// Zahájeno načítání dat
        /// </summary>
        LoadingData,
        /// <summary>
        /// Data načtena
        /// </summary>
        DataLoaded,
        /// <summary>
        /// Zahájení přípravy Controlu
        /// </summary>
        PrepareMainControl,
        /// <summary>
        /// Plnění vizuálních Controlů z dat v GUI
        /// </summary>
        FillGuiControls,
        /// <summary>
        /// Registrování event handlerů
        /// </summary>
        RegisterEventHandlers,
        /// <summary>
        /// Hlavní Control připraven
        /// </summary>
        MainControlDone,
        /// <summary>
        /// Data kompletně připravena
        /// </summary>
        Prepared
    }
    #endregion
}
