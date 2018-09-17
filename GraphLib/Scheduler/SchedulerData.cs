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

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// <summary>
    /// MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// </summary>
    public class MainData : IMainDataInternal, IFunctionProvider
    {
        #region Konstrukce a proměnné
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
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        #endregion
        #region Technika zpracování serializovaných prvků typu GuiItem
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
        /// Hlavní objekt s daty <see cref="GuiData"/>
        /// </summary>
        public GuiData GuiData { get { return this._GuiData; } }
        /// <summary>
        /// Hlavní objekt s daty <see cref="GuiData"/>
        /// </summary>
        private GuiData _GuiData;
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
        /// Do předaného formu vloží data z nastavení v <see cref="GuiProperties"/>
        /// </summary>
        /// <param name="mainForm"></param>
        private void _ApplyPropertiesToForm(Form mainForm)
        {
            GuiProperties guiProperties = this._GuiData.Properties;
            mainForm.FormBorderStyle = _ConvertBorderStyle(guiProperties.PluginFormBorder);
            mainForm.WindowState = _ConvertWindowState(guiProperties.PluginFormIsMaximized);
            mainForm.Text = guiProperties.PluginFormTitle;
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
        /// Z dat dodaných v prvcích GuiItem vytvoří vizuální controly a vloží je do Main WinForm controlu
        /// </summary>
        private void _FillMainControlFromGui()
        {
            this._FillMainControlToolbarFromGui();
            this._FillMainControlPagesFromGui();
        }
        /// <summary>
        /// Reference na hlavní GUI control, který je vytvořen v metodě <see cref="CreateControlToForm(Form)"/>
        /// </summary>
        protected MainControl _MainControl;
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
        private void _FillMainControlToolbarFromGui()
        {
            this._MainControl.ClearToolBar();
            this._MainControl.ToolBarVisible = this._GuiToolbarPanel.ToolbarVisible;
            this._FillMainControlToolbarSystemFromGui();
            this._FillMainControlToolbarDataFromGui();
            this._MainControl.ToolBarItemClicked += _MainControl_ToolBarItemClicked;
        }
        /// <summary>
        /// Do toolbaru vloží systémové funkce
        /// </summary>
        private void _FillMainControlToolbarSystemFromGui()
        {
            if (!this._GuiToolbarPanel.ToolbarShowSystemItems) return;

            FunctionGlobalGroup group = new FunctionGlobalGroup();
            group.Title = "ÚPRAVY";
            group.Order = "A1";
            group.ToolTipTitle = "Úpravy zadaných dat";

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditUndo, Text = "Zpět", IsEnabled = false, LayoutHint = LayoutHint.NextItemSkipToNextRow, UserData = "EditUndo" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditRedo, Text = "Vpřed", IsEnabled = true, UserData = "EditRedo" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.Refresh, Text = "Přenačíst", ToolTip = "Zruší všechny provedené změny a znovu načte data z databáze", IsEnabled = true, UserData = "Refresh" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.DocumentSave, Text = "Uložit", ToolTip = "Uloží všechny provedené změny do databáze", IsEnabled = false, UserData = "DocumentSave" });

            this._MainControl.AddToolBarGroup(group);
        }
        /// <summary>
        /// Do toolbaru vloží aplikační funkce
        /// </summary>
        private void _FillMainControlToolbarDataFromGui()
        {
            if (this._GuiToolbarPanel.Items == null) return;

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
        /// ContextFunctionItem : adapter mezi <see cref="GuiToolbarItem"/>, a položku kontextového menu <see cref="FunctionGlobalItem"/>.
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
        /// z tohoto souhrnu jsou následně vybírány funkce pro konkrétní situaci v metodě <see cref="_GetContextMenuItems(DataGraphItem, ItemActionArgs)"/>, 
        /// a z nich je pak vytvořeno fyzické kontextové menu v metodě <see cref="CreateContextMenu(DataGraphItem, ItemActionArgs)"/>.
        /// </summary>
        protected ContextFunctionItem[] _ContextFunctions;
        /// <summary>
        /// Metoda vytvoří a vrátí kontextové menu pro konkrétní prvek grafu
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu CreateContextMenu(DataGraphItem graphItem, ItemActionArgs args)
        {
            IEnumerable<FunctionItem> menuItems = this._GetContextMenuItems(graphItem, args);
            ToolStripDropDownMenu toolStripMenu = FunctionItem.CreateDropDownMenuFrom(menuItems);
            return toolStripMenu;
        }
        /// <summary>
        /// Metoda najde a vrátí soupis položek, popisujících jednotlivé funkce <see cref="FunctionItem"/>, 
        /// které mají být zobrazeny jako Kontextové menu pro daný prvek grafu.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private IEnumerable<FunctionItem> _GetContextMenuItems(DataGraphItem graphItem, ItemActionArgs args)
        {
            // graphItem.GraphTable;
            List<FunctionItem> menuItems = new List<FunctionItem>();


            ToolStripDropDownMenu menu = new ToolStripDropDownMenu();
            menu.Text = "nabídka funkcí";
            menu.DropShadowEnabled = true;
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Tag = args;

            ToolStripLabel menuTitle = new ToolStripLabel("NABÍDKA FUNKCÍ");
            // menuTitle.BackColor = Color.DarkBlue;
            menu.Items.Add(menuTitle);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem menuItem = new ToolStripMenuItem("Změnit čas události", IconStandard.BulletBlue16);
            menuItem.Tag = "Změna času";
            if (graphItem != null)
                menu.Items.Add(menuItem);

            if (graphItem == null)
                menu.Items.Add("Přidej stav kapacit");

            menu.Items.Add("Přidej další pracovní linku");

            if (graphItem != null)
                menu.Items.Add("Změnit čas směny");

            menu.ItemClicked += ContextMenuItemClicked;
            return menuItems;
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
            #region Public property FunctionItem, načítané z DataDeclaration
            /// <summary>
            /// Text do funkce
            /// </summary>
            public override string TextText { get { return this._GuiContextMenuItem.Title; } }
            /// <summary>
            /// ToolTip k textu
            /// </summary>
            public override string ToolTipText { get { return this._GuiContextMenuItem.ToolTip; } }
            /// <summary>
            /// Obrázek
            /// </summary>
            public override Image Image { get { return null /* this._GuiContextMenuItem.Image */ ; } }
            #endregion
            #region Určení dostupnosti položky pro konkrétní situaci
            /// <summary>
            /// Vrátí true, pokud tato položka kontextového menu se má použít pro prvek, jehož <see cref="GuiBase.FullName"/> je v parametru.
            /// </summary>
            /// <param name="fullName"></param>
            /// <returns></returns>
            public bool IsValidFor(string fullName)
            {
                return false;
            }
            #endregion
        }
        #endregion
        #endregion



        #region Data obrázků
        /// <summary>
        /// Vrátí obrázek daného jména. Může dojít k chybě <see cref="System.ArgumentNullException"/> nebo <see cref="System.Collections.Generic.KeyNotFoundException"/>.
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        protected Image GetImage(string imageName)
        {
            return this.ImageDict[imageName];
        }
        /// <summary>
        /// Zkusí najít obrázek daného jména. Nedojde k chybě.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        protected bool TryGetImage(string imageName, out Image image)
        {
            image = null;
            if (String.IsNullOrEmpty(imageName)) return false;
            return this.ImageDict.TryGetValue(imageName, out image);
        }
        /// <summary>
        /// Z dodaných dat (data) deserializuje Image a ten uloží pod danám názvem (imageName) do <see cref="ImageDict"/>.
        /// Chyby odchytí a ignoruje.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="imageName"></param>
        private void _LoadDataImage(string data, string imageName)
        {
            if (String.IsNullOrEmpty(data) || String.IsNullOrEmpty(imageName)) return;
            if (this.ImageDict.ContainsKey(imageName)) return;

            try
            {
                Image image = WorkSchedulerSupport.ImageDeserialize(data);
                this.ImageDict.Add(imageName, image);
            }
            catch (Exception)
            { }
        }
        /// <summary>
        /// Klíč z requestu typu "Image.imagename.cokoli dalšího" rozdělí na části, 
        /// z nichž název obrázku (zde "imagename") uloží do out imageName.
        /// Vrací true, pokud vstupující klíč obsahuje vyhovující data, nebo vrací false, pokud na vstupu je něco nerozpoznatelného.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="imageName"></param>
        /// <returns></returns>
        protected static bool IsKeyRequestImage(string key, out string imageName)
        {
            imageName = null;
            if (String.IsNullOrEmpty(key)) return false;
            string[] parts = key.Split('.');
            if (parts.Length < 2) return false;
            if (parts[0] != "Image") return false;
            imageName = parts[1];
            return (!String.IsNullOrEmpty(imageName));
        }
        /// <summary>
        /// Dictionary obsahující data jednotlivých obrázků
        /// </summary>
        protected Dictionary<string, Image> ImageDict { get; private set; }
        #endregion

        #region Konverze stringů a enumů
        /// <summary>
        /// Převede string obsahující číslo na Int32?.
        /// Pokud nebude rozpoznáno, vrací se null.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Int32? GetInt32N(string text)
        {
            Int32 number;
            if (String.IsNullOrEmpty(text)) return null;
            if (!Int32.TryParse(text, out number)) return null;
            return number;
        }
        /// <summary>
        /// Převede string obsahující číslo na Int32?.
        /// Pokud nebude rozpoznáno, vrací se null.
        /// Tato varianta provádí zarovnání do daných mezí.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="minValue">Dolní mez, včetně</param>
        /// <param name="maxValue">Horní mez, včetně</param>
        /// <returns></returns>
        public static Int32? GetInt32N(string text, Int32? minValue, Int32? maxValue)
        {
            Int32 number;
            if (String.IsNullOrEmpty(text)) return null;
            if (!Int32.TryParse(text, out number)) return null;
            if (maxValue.HasValue && number > maxValue.Value) return maxValue;
            if (minValue.HasValue && number < minValue.Value) return minValue;
            return number;
        }
        /// <summary>
        /// Převede string obsahující barvu na Color?.
        /// String může obsahovat název barvy = některou hodnotu z enumu <see cref="KnownColor"/>, například "Violet";, ignoruje se velikost písmen.
        /// anebo může být HEX hodnota zadaná ve formě "0x8080C0" nebo "0(and)226688".
        /// Pokud nebude rozpoznáno, vrací se null.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Color? GetColor(string text)
        {
            Color? color = null;
            if (String.IsNullOrEmpty(text)) return color;
            Dictionary<string, Color?> colorDict = _ColorDict;
            if (colorDict == null)
            {
                colorDict = new Dictionary<string, Color?>();
                _ColorDict = colorDict;
            }
            string name = text.Trim().ToLower();
            if (colorDict.TryGetValue(name, out color)) return color;

            WorkSchedulerSupport.TryColorDeserialize(name, out color);
            
            if (!colorDict.ContainsKey(name))
                colorDict.Add(name, color);
            return color;
        }
        /// <summary>
        /// Cache pro rychlejší konverzi názvů barev na Color hodnoty.
        /// </summary>
        private static Dictionary<string, Color?> _ColorDict;

        /// <summary>
        /// Daný řetězec rozdělí na jednotlivé prvky v místě daného oddělovače, a z prvků sestaví Dictionary, kde klíčem i hodnotou je string.
        /// Duplicitní výskyty stejného textu nezpůsobí chybu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiters"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetItemsAsDictionary(string text, params string[] delimiters)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] items = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                if (!String.IsNullOrEmpty(item) && !result.ContainsKey(item))
                    result.Add(item, item);
            }
            return result;
        }
        #endregion
        
        #region Eventy z GUI controlu
        /// <summary>
        /// Obsluha události Click na ToolBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MainControl_ToolBarItemClicked(object sender, FunctionItemEventArgs args)
        {
            
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
            ItemActionArgs itemArgs = menu.Tag as ItemActionArgs;

            string funcArgs = e.ClickedItem.Tag as string;

            RunContextFunctionArgs runArgs = new RunContextFunctionArgs()
            {
                GraphItemArgs = itemArgs,
                MenuItemText = funcArgs
            };
            if (this._HasHost)
            {
                this._AppHost.RunContextFunction(runArgs);
            }
            else
                System.Windows.Forms.MessageBox.Show("Rád bych provedl funkci " + runArgs.MenuItemText + ",\r\n ale není zadán datový hostitel.");
        }

        #endregion



        #region Implementace IMainDataInternal
        /// <summary>
        /// Zajistí otevření daného formuláře, prostřednictvím aktuálního hostitele <see cref="_AppHost"/>.
        /// </summary>
        /// <param name="recordGId"></param>
        protected void RunOpenRecordForm(GId recordGId)
        {
            if (this._HasHost)
                this._AppHost.RunOpenRecordForm(recordGId);
            else
                System.Windows.Forms.MessageBox.Show("Rád bych otevřel záznam " + recordGId.ToString() + ",\r\nale není zadán datový hostitel.");
        }
        /// <summary>
        /// Tato metoda zajistí otevření formuláře daného záznamu.
        /// Pouze převolá odpovídající metodu v <see cref="MainData"/>.
        /// </summary>
        /// <param name="recordGId"></param>
        void IMainDataInternal.RunOpenRecordForm(GId recordGId) { this.RunOpenRecordForm(recordGId); }
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ToolStripDropDownMenu IMainDataInternal.CreateContextMenu(DataGraphItem graphItem, ItemActionArgs args) { return this.CreateContextMenu(graphItem, args); }
        #endregion
    }
    /// <summary>
    /// Interface pro zpřístupnění vnitřních metod třídy <see cref="MainData"/>
    /// </summary>
    public interface IMainDataInternal
    {
        /// <summary>
        /// Metoda, která zajistí otevření formuláře daného záznamu.
        /// </summary>
        /// <param name="recordGId">Identifikátor záznamu</param>
        void RunOpenRecordForm(GId recordGId);
        /// <summary>
        /// Metoda pro daný prvek připraví a vrátí kontextové menu.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ToolStripDropDownMenu CreateContextMenu(DataGraphItem graphItem, ItemActionArgs args);
    }
    #endregion
}
