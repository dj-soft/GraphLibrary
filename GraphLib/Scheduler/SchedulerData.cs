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
using Noris.LCS.Manufacturing.WorkScheduler;

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
        public MainData(IAppHost host)
        {
            this._AppHost = host;
        }
        /// <summary>
        /// Obsahuje, true pokud máme vztah na datového hostitele
        /// </summary>
        private bool _HasHost { get { return (this._AppHost != null); } }
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
        private GuiData _GuiData;
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
        /// Reference na hlavní GUI control, který je vytvořen v metodě <see cref="CreateControl"/>
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
        /// ContextFunctionItem : adapter mezi <see cref="DataDeclaration"/> pro typ obsahu = <see cref="DataContentType.Button"/>, a položku kontextového menu <see cref="FunctionGlobalItem"/>.
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
            public override Image Image { get { return null /* this._Declaration.Image */; } }
            /// <summary>
            /// Velikost prvku na toolbaru, vzhledem k výšce toolbaru
            /// </summary>
            public override FunctionGlobalItemSize Size { get { return this._GuiToolBarItem.Size; } set { this._GuiToolBarItem.Size = value; } }
            /// <summary>
            /// Explicitně požadovaná šířka prvku v počtu modulů
            /// </summary>
            public override int? ModuleWidth { get { return this._GuiToolBarItem.ModuleWidth; } set { this._GuiToolBarItem.ModuleWidth = value; } }
            /// <summary>
            /// Nápověda ke zpracování layoutu této položky
            /// </summary>
            public override LayoutHint LayoutHint { get { return this._GuiToolBarItem.LayoutHint; } set { this._GuiToolBarItem.LayoutHint = value; } }
            /// <summary>
            /// Název grupy, kde se tento prvek objeví. Nezadaná grupa = implicitní s názvem "FUNKCE".
            /// </summary>
            public string GroupName { get { return this._GuiToolBarItem.GroupName; } set { this._GuiToolBarItem.GroupName = value; } }
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
        /// Do připraveného controlu vloží objekt obsahující 
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
        /// ContextFunctionItem : adapter mezi <see cref="DataDeclaration"/> pro typ obsahu = <see cref="DataContentType.Function"/>, a položku kontextového menu <see cref="FunctionItem"/>.
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



        #region Načítání a analýza dodaných dat, verze 1: string


        #region Data tabulek
        /// <summary>
        /// Načte a zpracuje vstupní data jedné tabulky
        /// </summary>
        /// <param name="data">Obsah dat ve formě komprimovaného stringu serializované <see cref="DataTable"/></param>
        /// <param name="dataId">DataId tabulky</param>
        /// <param name="tableName">Název tabulky</param>
        /// <param name="tableType">Typ dat, načtený z klíče (obsahuje string: Row, Graph, Rel, Item)</param>
        private void _LoadDataGraphTable(string data, int? dataId, string tableName, DataTableType tableType)
        {
            MainDataTable dataGraphTable = this.GetGraphTable(dataId, tableName);
            if (dataGraphTable == null)
            {   // Nová tabulka => založit nový kontenjer DataGraphTable:
                DataDeclaration dataDeclaration = this.SearchDataDeclarationForTable(tableName);
                dataGraphTable = new MainDataTable(this, tableName, dataDeclaration);
                this.GraphTableList.Add(dataGraphTable);
            }
            try
            {   // Do existujícího kontenjeru DataGraphTable vložit nová data daného typu:
                dataGraphTable.AddTable(data, tableType);
            }
            catch (Exception)
            { }
        }
        /// <summary>
        /// Najde a vrátí tabulku pro danou verzi dat a daný název.
        /// Může vrátit null.
        /// </summary>
        /// <param name="dataId"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected MainDataTable GetGraphTable(int? dataId, string tableName)
        {
            MainDataTable dataGraphTable = null;
            if (this.GraphTableList.Count == 0) return dataGraphTable;
            dataGraphTable = this.GraphTableList.FirstOrDefault(t => t.EqualsId(dataId, tableName));
            return dataGraphTable;
        }
        /// <summary>
        /// Finalizuje informace, popisující jednotlivé tabulky s daty.
        /// V této době jsou již načteny všechny datové tabulky, a deklarace dat prošla finalizací.
        /// </summary>
        private void _LoadDataGraphTableFinalise()
        {
            foreach (MainDataTable dataGraphTable in this.GraphTableList)
            {
                dataGraphTable.LoadFinalise();
            }
        }
        /// <summary>
        /// Klíč z requestu typu "Table.135103.workplace_table.Row.0" rozdělí na části, 
        /// z nichž název tabulky (zde "workplace_table") uloží do out tableName,
        /// a druh dat v tabulce (zde "Row") uloží do out tableType.
        /// Vrací true, pokud vstupující klíč obsahuje vyhovující data, nebo vrací false, pokud na vstupu je něco nerozpoznatelného.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dataId"></param>
        /// <param name="tableName"></param>
        /// <param name="tableType"></param>
        /// <returns></returns>
        protected static bool IsKeyRequestTable(string key, out int? dataId, out string tableName, out DataTableType tableType)
        {
            dataId = null;
            tableName = null;
            tableType = DataTableType.None;
            if (String.IsNullOrEmpty(key)) return false;
            string[] parts = key.Split('.');           // Nový formát  : [0]=Table   [1]=DataId     [2]=TableName    [3]=TableType    [4]=Part   ...
            int count = parts.Length;                  // Starý formát : [0]=Table   [1]=TableName  [2]=TableType    [4]=Part   ...
            if (count < 3) return false;
            if (parts[0] != "Table") return false;


            if (count >= 5 && parts[1].ContainsOnlyNumeric() && TryGetDataTableType(parts[3], out tableType))
            {   // Nový formát s kladným číslem verze dat v poli [1] a s platným typem dat v poli [3]:
                dataId = MainData.GetInt32N(parts[1]);
                tableName = parts[2];
            }
            else if (count >= 4 && TryGetDataTableType(parts[2], out tableType))
            {   // Starý formát s platným typem dat v poli [2]:
                tableName = parts[1];
            }

            return (!String.IsNullOrEmpty(tableName) && tableType != DataTableType.None);
        }
        /// <summary>
        /// Seznam obsahující data jednotlivých tabulek
        /// </summary>
        protected List<MainDataTable> GraphTableList { get; private set; }
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
        /// Metoda určí Typ údajů, které obsahuje určitá tabulka, na základě stringu, který je uveden v klíči těchto dat.
        /// Vrací true = je zadán správný text, false = nesprávný text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool TryGetDataTableType(string text, out DataTableType value)
        {
            value = GetDataTableType(text);
            return (value != DataTableType.None);
        }
        /// <summary>
        /// Metoda vrátí Typ údajů, které obsahuje určitá tabulka, na základě stringu, který je uveden v klíči těchto dat.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static DataTableType GetDataTableType(string text)
        {
            if (String.IsNullOrEmpty(text)) return DataTableType.None;
            switch (text)
            {
                case "Row": return DataTableType.Row;
                case "Graph": return DataTableType.Graph;
                case "Rel": return DataTableType.Rel;
                case "Item": return DataTableType.Item;
            }
            return DataTableType.None;
        }
        /// <summary>
        /// Převede string z pole "target" na typovou hodnotu
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static DataTargetType GetDataTargetType(string text)
        {
            if (String.IsNullOrEmpty(text)) return DataTargetType.None;
            switch (text)
            {
                case WorkSchedulerSupport.GUI_TARGET_MAIN: return DataTargetType.Main;
                case WorkSchedulerSupport.GUI_TARGET_TOOLBAR: return DataTargetType.ToolBar;
                case WorkSchedulerSupport.GUI_TARGET_TASK: return DataTargetType.Task;
                case WorkSchedulerSupport.GUI_TARGET_SCHEDULE: return DataTargetType.Schedule;
                case WorkSchedulerSupport.GUI_TARGET_SOURCE: return DataTargetType.Source;
                case WorkSchedulerSupport.GUI_TARGET_INFO: return DataTargetType.Info;
            }
            return DataTargetType.None;
        }
        /// <summary>
        /// Převede string z pole "content" na typovou hodnotu
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static DataContentType GetDataContentType(string text)
        {
            if (String.IsNullOrEmpty(text)) return DataContentType.None;
            switch (text)
            {
                case WorkSchedulerSupport.GUI_CONTENT_PANEL: return DataContentType.Panel;
                case WorkSchedulerSupport.GUI_CONTENT_BUTTON: return DataContentType.Button;
                case WorkSchedulerSupport.GUI_CONTENT_TABLE: return DataContentType.Table;
                case WorkSchedulerSupport.GUI_CONTENT_FUNCTION: return DataContentType.Function;
            }
            return DataContentType.None;
        }
        /// <summary>
        /// Metoda vrátí Pozici grafu v tabulce, na základě stringu, který je předán jako parametr.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static DataGraphPositionType GetGraphPosition(string text)
        {
            if (String.IsNullOrEmpty(text)) return DataGraphPositionType.None;
            switch (text)
            {
                case WorkSchedulerSupport.DATA_TABLE_POSITION_NONE: return DataGraphPositionType.None;

                case "LastColumn":
                case WorkSchedulerSupport.DATA_TABLE_POSITION_IN_LAST_COLUMN: return DataGraphPositionType.InLastColumn;

                case "Background":
                case "Proportional":
                case WorkSchedulerSupport.DATA_TABLE_POSITION_BACKGROUND_PROPORTIONAL: return DataGraphPositionType.OnBackgroundProportional;

                case "Logarithmic":
                case WorkSchedulerSupport.DATA_TABLE_POSITION_BACKGROUND_LOGARITHMIC: return DataGraphPositionType.OnBackgroundLogarithmic;
            }
            return DataGraphPositionType.None;
        }
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
        /// anebo může být HEX hodnota zadaná ve formě "0x8080C0" nebo "0&226688".
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
        /// Metoda vrátí <see cref="GraphItemBehaviorMode"/> pro zadaný text.
        /// Protože enum <see cref="GraphItemBehaviorMode"/> může obsahovat součty hodnot, tak konverze akceptuje znaky "|" a "+" mezi jednotlivými názvy hodnot.
        /// Vstup tedy může obsahovat: "ResizeTime + ResizeHeight + MoveToAnotherTime + MoveToAnotherRow" ("+" slouží jako oddělovač hodnot, mezery jsou odebrány).
        /// Může vracet <see cref="GraphItemBehaviorMode.None"/>, když vstup neobsahuje nic rozumného.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static GraphItemBehaviorMode GetBehaviorMode(string text)
        {
            GraphItemBehaviorMode behaviorMode = GraphItemBehaviorMode.None;
            if (String.IsNullOrEmpty(text)) return behaviorMode;
            string[] names = text.Split("+|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string name in names)
            {
                GraphItemBehaviorMode value;
                if (Enum.TryParse(name.Trim(), true, out value))
                    behaviorMode |= value;
            }
            return behaviorMode;
        }
        /// <summary>
        /// Metoda vrátí styl výplně pozadí pro zadaný text.
        /// Může vrátit null = Solid barva.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static System.Drawing.Drawing2D.HatchStyle? GetHatchStyle(string text)
        {
            System.Drawing.Drawing2D.HatchStyle? hatchStyle = null;
            if (String.IsNullOrEmpty(text)) return hatchStyle;
            System.Drawing.Drawing2D.HatchStyle value;
            if (Enum.TryParse(text, true, out value)) return value;
            return null;
        }
        /// <summary>
        /// Vrátí velikost buttonu <see cref="FunctionGlobalItemSize"/>
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static FunctionGlobalItemSize GetFunctionItemSize(string text)
        {
            FunctionGlobalItemSize defaultSize = FunctionGlobalItemSize.Half;
            if (String.IsNullOrEmpty(text)) return defaultSize;
            string key = text.Trim().ToLower();
            switch (key)
            {
                case "micro": return FunctionGlobalItemSize.Micro;
                case "standard":
                case "normal":
                case "small": return FunctionGlobalItemSize.Small;
                case "half": return FunctionGlobalItemSize.Half;
                case "large": return FunctionGlobalItemSize.Large;
                case "big":
                case "whole": return FunctionGlobalItemSize.Whole;
            }

            Int32? value = MainData.GetInt32N(text, 1, 6);
            if (!value.HasValue) return defaultSize;
            switch (value.Value)
            {
                case 1: return FunctionGlobalItemSize.Micro;
                case 2: return FunctionGlobalItemSize.Small;
                case 3: return FunctionGlobalItemSize.Half;
                case 4: return FunctionGlobalItemSize.Large;
                case 5:
                case 6: return FunctionGlobalItemSize.Whole;
            }
            return defaultSize;
        }
        /// <summary>
        /// Vrátí daný text převedený do enumu LayoutHint.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static LayoutHint GetToolBarLayoutHint(string text)
        {
            LayoutHint result = LayoutHint.Default;
            if (String.IsNullOrEmpty(text)) return result;

            // Textové hodnoty v této proměnné mají přesně odpovídat hodnotám enumu, proto zde není switch { }, ale Enum.TryParse() :
            string[] items = text.Split(new char[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                LayoutHint hint;
                if (Enum.TryParse(item.Trim(), true, out hint))
                    result |= hint;
            }

            return result;
        }
        /// <summary>
        /// Daný řetězec rozdělí na jednotlivé prvky v místě daného oddělovače, a z prvků sestaví Dictionary, kde klíčem i hodnotou je string.
        /// Duplicitní výskyty stejného textu nezpůsobí chybu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter"></param>
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
        /// <returns></returns>
        ToolStripDropDownMenu IMainDataInternal.CreateContextMenu(DataGraphItem graphItem, ItemActionArgs args) { return this.CreateContextMenu(graphItem, args); }
        IEnumerable<DataDeclaration> IMainDataInternal.DataDeclarations { get { return this.Declarations; } }
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
        /// <summary>
        /// Souhrn všech deklarací dat
        /// </summary>
        IEnumerable<DataDeclaration> DataDeclarations { get; }
    }
    #endregion
    #region class DataDeclaration : deklarace dat, předaná z volajícího do pluginu, definuje rozsah dat a funkcí
    /// <summary>
    /// DataDeclaration : deklarace dat, předaná z volajícího do pluginu, definuje rozsah dat a funkcí
    /// </summary>
    public class DataDeclaration
    {
        #region Tvorba instance
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="DataDeclaration"/> z datového řádku tabulky, 
        /// jejíž struktura odpovídá <see cref="WorkSchedulerSupport.KEY_REQUEST_DATA_DECLARATION"/>.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static DataDeclaration CreateFrom(MainData mainData, DataRow row)
        {
            if (row == null) return null;

            DataDeclaration data = new DataDeclaration(mainData);
            data.DataId = row.GetValue<int>("data_id");
            data.Target = Scheduler.MainData.GetDataTargetType(row.GetValue<string>("target"));
            data.Content = Scheduler.MainData.GetDataContentType(row.GetValue<string>("content"));
            data.Name = row.GetValue<string>("name");
            data.Title = row.GetValue<string>("title");
            data.ToolTip = row.GetValue<string>("tooltip");
            data.Image = row.GetValue<string>("image");
            data.Data = row.GetValue<string>("data");

            return data;
        }
        /// <summary>
        /// privátní konstruktor. Instanci lze založit pomocí metody <see cref="CreateFrom(MainData, DataRow)"/>.
        /// </summary>
        private DataDeclaration(MainData mainData)
        {
            this.MainData = mainData;
        }
        /// <summary>
        /// Vlastník = datová základna, instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        protected MainData MainData { get; set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Target: " + this.Target + "; Content: " + this.Content + "; Title: " + this.Title;
        }
        #endregion
        #region Public data
        /// <summary>
        /// ID skupiny dat. Jedna skupina dat se vkládá do jednoho controlu <see cref="SchedulerPanel"/>, 
        /// může jich být více, pak hlavní control <see cref="MainControl"/> obsahuje více panelů.
        /// </summary>
        public int DataId { get; private set; }
        /// <summary>
        /// Cílový prostor v panelu <see cref="SchedulerPanel"/> pro tuto položku deklarace
        /// </summary>
        public DataTargetType Target { get; private set; }
        /// <summary>
        /// Typ obsahu v této položce deklarace
        /// </summary>
        public DataContentType Content { get; private set; }
        /// <summary>
        /// Name = strojový identifikátor, nezobrazuje se - ale používá se při komunikaci
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Název položky = stále viditelný text pro tuto položku deklarace
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// Nápovědný text do ToolTipu k této položce deklarace
        /// </summary>
        public string ToolTip { get; private set; }
        /// <summary>
        /// Název nebo obsah ikony
        /// </summary>
        public string Image { get; private set; }
        /// <summary>
        /// Rozšiřující data, podle typu obsahu <see cref="Content"/>
        /// </summary>
        public string Data { get; private set; }
        #endregion
        #region Vyhledání nadřízených deklarací
        /// <summary>
        /// Najde a vrátí deklaraci dat typu <see cref="DataDeclaration.Target"/> == <see cref="DataTargetType.Main"/> 
        /// pro shodnou verzi dat (<see cref="DataDeclaration.DataId"/>) pro jakou je platný this prvek.
        /// Pokud takovou deklaraci nenajde, obsahue (vrací) null.
        /// </summary>
        public DataDeclaration MainDataDeclaration
        {
            get
            {
                if (!this._MainDataDeclarationValid)
                {
                    this._MainDataDeclaration = ((this.Target == DataTargetType.Main) ? this :
                        ((IMainDataInternal)this.MainData).DataDeclarations.FirstOrDefault(d => (d.Target == DataTargetType.Main && d.DataId == this.DataId)));
                    this._MainDataDeclarationValid = true;
                }
                return this._MainDataDeclaration;
            }
        }
        /// <summary>
        /// Platnost dat cache <see cref="_MainDataDeclaration"/>
        /// </summary>
        private bool _MainDataDeclarationValid;
        /// <summary>
        /// Cache dat pro property <see cref="MainDataDeclaration"/>
        /// </summary>
        private DataDeclaration _MainDataDeclaration;
        #endregion
    }
    #endregion

    // Toto se všechno musí zrušit:
    #region enumy : DataTableType, DataTargetType, DataContentType
    /// <summary>
    /// Typ údajů, které obsahuje určitá tabulka
    /// </summary>
    public enum DataTableType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Vizuální řádky
        /// </summary>
        Row,
        /// <summary>
        /// Položky grafu
        /// </summary>
        Graph,
        /// <summary>
        /// Vztahy mezi položkami grafu
        /// </summary>
        Rel,
        /// <summary>
        /// Informační texty k položkám grafu
        /// </summary>
        Item
    }
    /// <summary>
    /// Cílový prvek položky v deklaraci dat
    /// </summary>
    public enum DataTargetType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Funkce v toolbaru
        /// </summary>
        ToolBar,
        /// <summary>
        /// Main = záhlaví panelu jedné verze dat
        /// </summary>
        Main,
        /// <summary>
        /// Tabulky v panelu vlevo
        /// </summary>
        Task,
        /// <summary>
        /// Tabulky v hlavním panelu
        /// </summary>
        Schedule,
        /// <summary>
        /// Tabulky v panelu vpravo
        /// </summary>
        Source,
        /// <summary>
        /// Tabulky v panelu dole
        /// </summary>
        Info
    }
    /// <summary>
    /// Typ obsahu v deklaraci dat
    /// </summary>
    public enum DataContentType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Panel = záhlaví celé verze dat
        /// </summary>
        Panel,
        /// <summary>
        /// Button = položka ToolBaru
        /// </summary>
        Button,
        /// <summary>
        /// Table = tabulka
        /// </summary>
        Table,
        /// <summary>
        /// Function = kontextová funkce
        /// </summary>
        Function
    }
    /// <summary>
    /// Pozice grafu v tabulce
    /// </summary>
    public enum DataGraphPositionType
    {
        /// <summary>
        /// V dané tabulce není graf (výchozí stav)
        /// </summary>
        None,
        /// <summary>
        /// Graf zobrazit v posledním sloupci (sloupec bude do tabulky přidán)
        /// </summary>
        InLastColumn,
        /// <summary>
        /// Graf zobrazit jako poklad, měřítko časové osy = proporcionální
        /// </summary>
        OnBackgroundProportional,
        /// <summary>
        /// Graf zobrazit jako poklad, měřítko časové osy = logaritmické
        /// </summary>
        OnBackgroundLogarithmic
    }
    #endregion
}
