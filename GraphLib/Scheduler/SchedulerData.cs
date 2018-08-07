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

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// <summary>
    /// MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// </summary>
    public class MainData : IMainDataInternal, IFunctionProvider
    {
        #region Konstrukce a proměnné
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
        #region Načítání a analýza dodaných dat
        /// <summary>
        /// Načte data z datového balíku
        /// </summary>
        /// <param name="dataPack"></param>
        public void LoadData(string dataPack)
        {
            try
            {
                this.GraphTableDict = new Dictionary<string, DataGraphTable>();
                this.ImageDict = new Dictionary<string, Image>();
                using (var buffer = WorkSchedulerSupport.CreateDataBufferReader(dataPack))
                {
                    while (!buffer.ReaderIsEnd)
                    {
                        string key, data;
                        if (buffer.ReadNextData(out key, out data))
                            this._LoadDataOne(key, data);
                    }
                }
                this._LoadDataFinalise();
            }
            catch (Exception)
            {   // Zatím nijak explicitně neřešíme:
                throw;
            }
        }
        /// <summary>
        /// Zajistí načtení jednoho bloku dat z datového balíčku.
        /// Blok má svůj název (klíč = key) a obsah (data).
        /// Měl by existovat blok s názvem "DataDeclaration" (jen jeden, obsahuje deklaraci GUI) 
        /// a několik bloků s daty jednotlivých tabulek, název typicky "Table.workplace_table.Graph.1", jejich obsahem je DataTable.
        /// Mohou existovat i bloky dodávající obrázky (název "Image.name"), jejich obsahem je bitmapa (ikony funkcí, tollbaru, aplikační obrázky, atd).
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        private void _LoadDataOne(string key, string data)
        {
            string itemName;
            DataTableType tableType;
            if (key == WorkSchedulerSupport.KEY_REQUEST_DATA_DECLARATION)
            {   // Deklarace dat:
                this._LoadDataDeclaration(data);
            }
            else if (IsKeyRequestTable(key, out itemName, out tableType))
            {   // Tabulka s daty:
                this._LoadDataGraphTable(data, itemName, tableType);
            }
            else if (IsKeyRequestImage(key, out itemName))
            {   // Image:
                this._LoadDataImage(data, itemName);
            }
        }
        /// <summary>
        /// Metoda zajistí provedení finalizace dat po jejich kompletním načtení.
        /// </summary>
        protected void _LoadDataFinalise()
        {
            this._LoadDataDeclarationFinalise();
            this._LoadDataGraphTableFinalise();
        }
        #region Deklarace dat
        /// <summary>
        /// Metoda vrátí deklaraci dat pro tabulku daného názvu.
        /// Může vrátit null, pokud v deklaraci nebyla uvedena tabulka daného názvu.
        /// Název se hledá <see cref="StringComparison.InvariantCulture"/>, je tedy Case-Sensitive.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected DataDeclaration SearchDataDeclarationForTable(string tableName)
        {
            return this.Declarations.FirstOrDefault(d => d.Content == DataContentType.Table && String.Equals(d.Name, tableName, StringComparison.InvariantCulture));
        }
        /// <summary>
        /// Načte informace, popisující deklaraci dat.
        /// Na vstupu je komprimovaný text, obsahující serializovanou DataTable, která popisuje deklaraci dat.
        /// </summary>
        /// <param name="data"></param>
        private void _LoadDataDeclaration(string data)
        {
            string text = WorkSchedulerSupport.Decompress(data);
            DataTable dataTable = WorkSchedulerSupport.TableDeserialize(text);
            WorkSchedulerSupport.CheckTable(dataTable, WorkSchedulerSupport.DATA_DECLARATION_STRUCTURE);

            List<DataDeclaration> declarationList = new List<DataDeclaration>();
            foreach (DataRow row in dataTable.Rows)
            {
                DataDeclaration declaration = DataDeclaration.CreateFrom(this, row);
                if (declaration != null)
                    declarationList.Add(declaration);
            }
            this.Declarations = declarationList.ToArray();
        }
        /// <summary>
        /// Finalizuje informace, popisující deklaraci dat.
        /// V této době jsou načteny všechny datové tabulky (ale ty ještě neprošly finalizací).
        /// </summary>
        private void _LoadDataDeclarationFinalise()
        {
            this._LoadToolBarButtonItems();
            this._LoadContextMenuFunctionItems();
        }
        /// <summary>
        /// Deklarace dat = popis funkcí a tabulek
        /// </summary>
        protected DataDeclaration[] Declarations { get; private set; }
        #endregion
        #region Data tabulek
        /// <summary>
        /// Načte a zpracuje vstupní data jedné tabulky
        /// </summary>
        /// <param name="data">Obsah dat ve formě komprimovaného stringu serializované <see cref="DataTable"/></param>
        /// <param name="tableName">Název tabulky</param>
        /// <param name="tableType">Typ dat, načtený z klíče (obsahuje string: Row, Graph, Rel, Item)</param>
        private void _LoadDataGraphTable(string data, string tableName, DataTableType tableType)
        {
            DataGraphTable dataGraphTable;
            if (!this.GraphTableDict.TryGetValue(tableName, out dataGraphTable))
            {
                DataDeclaration dataDeclaration = this.SearchDataDeclarationForTable(tableName);
                dataGraphTable = new DataGraphTable(this, tableName, dataDeclaration);
                this.GraphTableDict.Add(tableName, dataGraphTable);
            }
            try
            {
                dataGraphTable.AddTable(data, tableType);
            }
            catch (Exception)
            { }
        }
        /// <summary>
        /// Finalizuje informace, popisující jednotlivé tabulky s daty.
        /// V této době jsou již načteny všechny datové tabulky, a deklarace dat prošla finalizací.
        /// </summary>
        private void _LoadDataGraphTableFinalise()
        {
            foreach (DataGraphTable dataGraphTable in this.GraphTableDict.Values)
            {
                dataGraphTable.LoadFinalise();
            }
        }
        /// <summary>
        /// Klíč z requestu typu "Table.workplace_table.Row.0" rozdělí na části, 
        /// z nichž název tabulky (zde "workplace_table") uloží do out tableName,
        /// a druh dat v tabulce (zde "Row") uloží do out tableType.
        /// Vrací true, pokud vstupující klíč obsahuje vyhovující data, nebo vrací false, pokud na vstupu je něco nerozpoznatelného.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tableName"></param>
        /// <param name="tableType"></param>
        /// <returns></returns>
        protected static bool IsKeyRequestTable(string key, out string tableName, out DataTableType tableType)
        {
            tableName = null;
            tableType = DataTableType.None;
            if (String.IsNullOrEmpty(key)) return false;
            string[] parts = key.Split('.');
            if (parts.Length < 3) return false;
            if (parts[0] != "Table") return false;
            tableName = parts[1];
            tableType = GetDataTableType(parts[2]);
            return (!String.IsNullOrEmpty(tableName) && tableType != DataTableType.None);
        }
        /// <summary>
        /// Dictionary obsahující data jednotlivých tabulek
        /// </summary>
        protected Dictionary<string, DataGraphTable> GraphTableDict { get; private set; }
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
        #endregion
        #region Konverze stringů a enumů
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

            if (name.StartsWith("0x") || name.StartsWith("0&"))
                color = _GetColorHex(name);
            else
                color = _GetColorName(name);

            if (!colorDict.ContainsKey(name))
                colorDict.Add(name, color);
            return color;
        }
        /// <summary>
        /// Vrátí barvu pro zadaný název barvy. Název může být string z enumu <see cref="KnownColor"/>, například "Violet";, ignoruje se velikost písmen.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Color? _GetColorName(string name)
        {
            KnownColor color;
            if (Enum.TryParse(name, true, out color)) return Color.FromKnownColor(color);
            return null;
        }
        /// <summary>
        /// Vrátí barvu pro zadaný hexadecimální řetězec.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Color? _GetColorHex(string name)
        {
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
            string hexValue = name.Substring(2).ToUpper();
            int value;
            if (!Int32.TryParse(hexValue, System.Globalization.NumberStyles.AllowHexSpecifier, nfi, out value)) return null;
            Color color = Color.FromArgb(value);
            if (color.A == 0)                              // Pokud v barvě NENÍ zadáno nic do složky Alpha, jde nejspíš o opomenutí!
                color = Color.FromArgb(255, color);        //   (implicitně se hodnota Alpha nezadává, a přitom se předpokládá že tan bude 255)  =>  Alpha = 255 = plná barva
            return color;
        }
        /// <summary>
        /// Cache pro rychlejší konverzi názvů barev na Color hodnoty.
        /// </summary>
        private static Dictionary<string, Color?> _ColorDict;
        /// <summary>
        /// Metoda vrátí <see cref="GraphItemEditMode"/> pro zadaný text.
        /// Protože enum <see cref="GraphItemEditMode"/> může obsahovat součty hodnot, tak konverze akceptuje znaky "|" a "+" mezi jednotlivými názvy hodnot.
        /// Vstup tedy může obsahovat: "ResizeTime + ResizeHeight + MoveToAnotherTime + MoveToAnotherRow" ("+" slouží jako oddělovač hodnot, mezery jsou odebrány).
        /// Může vracet <see cref="GraphItemEditMode.None"/>, když vstup neobsahuje nic rozumného.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static GraphItemEditMode GetEditMode(string text)
        {
            GraphItemEditMode editMode = GraphItemEditMode.None;
            if (String.IsNullOrEmpty(text)) return editMode;
            string[] names = text.Split("+|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string name in names)
            {
                GraphItemEditMode value;
                if (Enum.TryParse(name.Trim(), true, out value))
                    editMode |= value;
            }
            return editMode;
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
            FunctionGlobalItemSize defaultSize = FunctionGlobalItemSize.Small;
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
        #region Tvorba GUI
        public System.Windows.Forms.Control CreateGui()
        {
            this._MainControl = new MainControl(this);
            this._MainControl.ToolBarItemClicked += _MainControl_ToolBarItemClicked;
            this._FillMainDataToolBar();
            this._FillMainDataTables();
            return this._MainControl;
        }
        /// <summary>
        /// Naplní do <see cref="_MainControl"/> veškeré položky do ToolBaru
        /// </summary>
        protected void _FillMainDataToolBar()
        {
            List<FunctionGlobalGroup> groupList = this.CreateToolBarContent();
            this._MainControl.AddToolBarGroups(groupList);
        }
        /// <summary>
        /// Naplní do <see cref="_MainControl"/> veškeré tabulky
        /// </summary>
        protected void _FillMainDataTables()
        {
            foreach (var graphTable in this.GraphTableDict.Values)
                this._MainControl.AddGraphTable(graphTable);
        }
        /// <summary>
        /// Reference na hlavní GUI control
        /// </summary>
        protected MainControl _MainControl;
        #endregion
        #region Testovací GUI
        public System.Windows.Forms.Control OldCreateGui()
        {
            this._GControl = new GInteractiveControl();

            this._GGrid = new GGrid();
            this._GGrid.AddTable(this.GraphTableDict.Values.FirstOrDefault().TableRow);

            this._GControl.AddItem(this._GGrid);
            this._GGrid.Bounds = new Rectangle(3, 3, 600, 450);
            this._GControl.SizeChanged += new EventHandler(_GControlSizeChanged);
            return this._GControl;
            
        }
        private GInteractiveControl _GControl;
        private GGrid _GGrid;
        void _GControlSizeChanged(object sender, EventArgs e)
        {
            var form = this._GControl.FindForm();
            string state = "State: " + (form != null ? form.WindowState.ToString() : "NULL");

            Size size = this._GControl.ClientSize;
            Rectangle oldBounds = this._GGrid.Bounds;
            Rectangle newBounds = new Rectangle(3, 3, size.Width - 6, size.Height - 6);

            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "SizeChanged", "", "OldBounds: " + oldBounds, "NewBounds: " + newBounds, state))
            {
                this._GGrid.Bounds = new Rectangle(3, 3, size.Width - 6, size.Height - 6);
            }
        }
        #endregion
        #region ToolBar
        /// <summary>
        /// Z datové deklarace načte veškeré funkce pro veškere položky ToolBaru (provede jejich načtení a parsování dat).
        /// </summary>
        private void _LoadToolBarButtonItems()
        {
            List<ToolBarItem> toolBarItems = new List<ToolBarItem>();
            foreach (DataDeclaration declaration in this.Declarations.Where(d => d.Content == DataContentType.Button))
            {
                ToolBarItem toolBarItem = ToolBarItem.Create(this, declaration);
                if (toolBarItem != null)
                    toolBarItems.Add(toolBarItem);
            }
            this._ToolBarItems = toolBarItems.ToArray();
        }
        /// <summary>
        /// Metoda vrátí kompletní obsah ToolBaru. Tedy jak aplikační prvky, tak prvky z deklarace.
        /// </summary>
        /// <returns></returns>
        protected List<FunctionGlobalGroup> CreateToolBarContent()
        {
            List<FunctionGlobalGroup> groupList = new List<FunctionGlobalGroup>();
            this._AddToolBarContentApplication(groupList);
            this._AddToolBarContentDeclaration(groupList);
            return groupList;
        }
        /// <summary>
        /// Do předaného soupisu prvků do ToolBaru přidá fixní položky aplikace
        /// </summary>
        /// <param name="groupList"></param>
        protected void _AddToolBarContentApplication(List<FunctionGlobalGroup> groupList)
        {
            FunctionGlobalGroup group = new FunctionGlobalGroup(null);
            group.Title = "ÚPRAVY";
            group.Order = "A1";
            group.ToolTipTitle = "Úpravy zadaných dat";

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditUndo, Text = "Zpět", IsEnabled = false, LayoutHint = LayoutHint.NextItemSkipToNextRow, UserData = "EditUndo" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditRedo, Text = "Vpřed", IsEnabled = true, UserData = "EditRedo" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.Refresh, Text = "Přenačíst", ToolTip = "Zruší všechny provedené změny a znovu načte data z databáze", IsEnabled = true, UserData = "Refresh" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.DocumentSave, Text = "Uložit", ToolTip = "Uloží všechny provedené změny do databáze", IsEnabled = false, UserData = "DocumentSave" });

            groupList.Add(group);
        }
        /// <summary>
        /// Do předaného soupisu prvků do ToolBaru přidá položky menu, dodané v datech = v deklaraci (<see cref="Declarations"/>).
        /// </summary>
        /// <param name="groupList"></param>
        protected void _AddToolBarContentDeclaration(List<FunctionGlobalGroup> groupList)
        {
            foreach (var item in this.Declarations.Where(d => d.Content == DataContentType.Button))
            {
                throw new GraphLibCodeException("Tohle se musí dodělat: Scheduler.MainData._AddToolBarContentDeclaration()");
            }
        }
        /// <summary>
        /// Obsluha události Click na ToolBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MainControl_ToolBarItemClicked(object sender, FunctionItemEventArgs args)
        {
            
        }
        protected ToolBarItem[] _ToolBarItems;
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
            /// <param name="declaration"></param>
            protected ToolBarItem(IFunctionProvider provider, DataDeclaration declaration) : base(provider)
            {
                this._Declaration = declaration;
            }
            /// <summary>
            /// Z této deklarace je funkce načtena
            /// </summary>
            private DataDeclaration _Declaration;
            /// <summary>
            /// Vytvoří a vrátí new instanci <see cref="ToolBarItem"/> pro data z deklarace <see cref="DataDeclaration"/>.
            /// Může vrátit null pro neplatné zadání.
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="declaration"></param>
            /// <returns></returns>
            public static ToolBarItem Create(IFunctionProvider provider, DataDeclaration declaration)
            {
                if (provider == null || declaration == null) return null;
                if (declaration.Content != DataContentType.Function) return null;

                ToolBarItem toolBarItem = new ToolBarItem(provider, declaration);
                toolBarItem._LoadData(declaration.Data);
                return toolBarItem;
            }
            /// <summary>
            /// Načte data (=dodaný string, pocházející z <see cref="DataDeclaration.Data"/>).
            /// Jednotlivé prvky uloží do <see cref="_TablesDict"/> a <see cref="_ClassesDict"/>.
            /// </summary>
            /// <param name="data"></param>
            private void _LoadData(string data)
            {
                var items = data.ToKeyValues(";", ":", true, true);
                foreach (var item in items)
                {
                    switch (item.Key)
                    {
                        case WorkSchedulerSupport.DATA_BUTTON_HEIGHT:
                            this.Size = MainData.GetFunctionItemSize(item.Value);
                            break;
                        case WorkSchedulerSupport.DATA_BUTTON_WIDTH:
                            this.ModuleWidth = MainData.GetInt32N(item.Value, 1, 24);
                            break;
                        case WorkSchedulerSupport.DATA_BUTTON_LAYOUT:
                            this.LayoutHint = MainData.GetToolBarLayoutHint(item.Value);
                            break;
                    }
                }
            }
            #endregion
            #region Public property FunctionGlobalItem, načítané z DataDeclaration
            /// <summary>
            /// Text do funkce
            /// </summary>
            public override string TextText { get { return this._Declaration.Title; } }
            /// <summary>
            /// ToolTip k textu
            /// </summary>
            public override string ToolTipText { get { return this._Declaration.ToolTip; } }
            /// <summary>
            /// Obrázek
            /// </summary>
            public override Image Image { get { return null /* this._Declaration.Image */; } }
            #endregion
        }
        #endregion
        #region Kontextové menu
        protected ToolStripDropDownMenu CreateContextMenu(DataGraphItem graphItem, ItemActionArgs args)
        {
            IEnumerable<FunctionItem> menuItems = this._GetContextMenuItems(graphItem, args);
            System.Windows.Forms.ToolStripDropDownMenu toolStripMenu = FunctionItem.CreateDropDownMenuFrom(menuItems);
            return toolStripMenu;
        }
        /// <summary>
        /// Z datové deklarace načte veškeré funkce pro veškerá kontextová menu (provede jejich načtení a parsování dat).
        /// Následně jakékoli použití kontextového menu načítá jejich patřičnou podmnožinu.
        /// </summary>
        private void _LoadContextMenuFunctionItems()
        {
            List<ContextFunctionItem> functions = new List<ContextFunctionItem>();
            foreach (DataDeclaration declaration in this.Declarations.Where(d => d.Content == DataContentType.Function))
            {
                ContextFunctionItem funcItem = ContextFunctionItem.Create(this, declaration);
                if (funcItem != null)
                    functions.Add(funcItem);
            }
            this._ContextFunctions = functions.ToArray();
        }
        private IEnumerable<FunctionItem> _GetContextMenuItems(DataGraphItem graphItem, ItemActionArgs args)
        {
            // graphItem.GraphTable;
            List<FunctionItem> menuItems = new List<FunctionItem>();

                DataDeclaration dataDeclaration;
            // this.TryGetDataDeclaration()
            var fd = this.Declarations.FirstOrDefault();

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
        /// <summary>
        /// Souhrn všech definovaných funkcí pro všechna kontextová menu v systému.
        /// Souhrn je načten z <see cref="Declarations"/> v metodě <see cref="_LoadContextMenuFunctionItems()"/>, 
        /// z tohoto souhrnu jsou následně vybírány funkce pro konkrétní situaci v metodě <see cref="_GetContextMenuItems(DataGraphItem, ItemActionArgs)"/>, 
        /// a z nich je pak vytvořeno fyzické kontextové menu v metodě <see cref="CreateContextMenu(DataGraphItem, ItemActionArgs)"/>.
        /// </summary>
        protected ContextFunctionItem[] _ContextFunctions;
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
            /// <param name="declaration"></param>
            protected ContextFunctionItem(IFunctionProvider provider, DataDeclaration declaration) : base(provider)
            {
                this._Declaration = declaration;
            }
            /// <summary>
            /// Z této deklarace je funkce načtena
            /// </summary>
            private DataDeclaration _Declaration;
            /// <summary>
            /// Vytvoří a vrátí new instanci <see cref="ContextFunctionItem"/> pro data z deklarace <see cref="DataDeclaration"/>.
            /// Může vrátit null pro neplatné zadání.
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="declaration"></param>
            /// <returns></returns>
            public static ContextFunctionItem Create(IFunctionProvider provider, DataDeclaration declaration)
            {
                if (provider == null || declaration == null) return null;
                if (declaration.Content != DataContentType.Function) return null;

                ContextFunctionItem funcItem = new ContextFunctionItem(provider, declaration);
                funcItem._LoadData(declaration.Data);
                return funcItem;
            }
            /// <summary>
            /// Načte data (=dodaný string, pocházející z <see cref="DataDeclaration.Data"/>).
            /// Jednotlivé prvky uloží do <see cref="_TablesDict"/> a <see cref="_ClassesDict"/>.
            /// </summary>
            /// <param name="data"></param>
            private void _LoadData(string data)
            {
                var items = data.ToKeyValues(";", ":", true, true);
                foreach (var item in items)
                {
                    switch (item.Key)
                    {
                        case WorkSchedulerSupport.DATA_FUNCTION_TABLE_NAMES:
                            this._TablesDict = MainData.GetItemsAsDictionary(item.Value, ",");
                            break;
                        case WorkSchedulerSupport.DATA_FUNCTION_CLASS_NUMBERS:
                            this._ClassesDict = MainData.GetItemsAsDictionary(item.Value, ",");
                            break;
                    }
                }
            }
            /// <summary>
            /// Pro tyto tabulky je funkce zobrazována.
            /// Může být null, pokud klíč "TableNames" není přítomen v <see cref="DataDeclaration.Data"/>.
            /// </summary>
            private Dictionary<string, string> _TablesDict;
            /// <summary>
            /// Pro tyto třídy je funkce zobrazována.
            /// Může být null, pokud klíč "TableNames" není přítomen v <see cref="DataDeclaration.Data"/>.
            /// </summary>
            private Dictionary<string, string> _ClassesDict;
            #endregion
            #region Public property FunctionItem, načítané z DataDeclaration
            /// <summary>
            /// Text do funkce
            /// </summary>
            public override string TextText { get { return this._Declaration.Title; } }
            /// <summary>
            /// ToolTip k textu
            /// </summary>
            public override string ToolTipText { get { return this._Declaration.ToolTip; } }
            /// <summary>
            /// Obrázek
            /// </summary>
            public override Image Image { get { return null /* this._Declaration.Image */; } }
            #endregion
            #region Určení dostupnosti položky pro konkrétní situaci
            public bool IsValidFor(string tableName, params int[] classNumbers)
            {
                return false;
            }
            #endregion
        }
        #endregion
        #region Implementace IMainDataInternal
        protected void RunOpenRecordForm(GId recordGId)
        {
            if (this._HasHost)
                this._AppHost.RunOpenRecordForm(recordGId);
            else
                System.Windows.Forms.MessageBox.Show("Rád bych otevřel záznam " + recordGId.ToString() + ",\r\n ale není zadán datový hostitel.");
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
    }
    #endregion
    #region class DataGraphTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// <summary>
    /// DataGraphTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// </summary>
    public class DataGraphTable : IDataGraphTableInternal, ITimeGraphDataSource
    {
        #region Konstrukce, postupné vkládání dat z tabulek, včetně finalizace
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mainData"></param>
        /// <param name="tableName"></param>
        public DataGraphTable(MainData mainData, string tableName, DataDeclaration dataDeclaration)
        {
            this.MainData = mainData;
            this.TableName = tableName;
            this.DataDeclaration = dataDeclaration;
            this.DataGraphProperties = DataGraphProperties.CreateFrom(this, this.DataDeclaration.Data);
            this._TableRow = null;
            this._TableInfoList = new List<Table>();
            this._GIdIndex = new Index<GId>(IndexScopeType.TKeyType);
            this._GraphItemDict = new Dictionary<GId, DataGraphItem>();
        }
        /// <summary>
        /// Vlastník = instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        internal MainData MainData { get; private set; }
        /// <summary>
        /// Vlastník přetypovaný na IMainDataInternal
        /// </summary>
        protected IMainDataInternal IMainData { get { return (this.MainData as IMainDataInternal); } }
        /// <summary>
        /// Název této tabulky
        /// </summary>
        public string TableName { get; private set; }
        /// <summary>
        /// Deklarace dat pro tuto tabulku.
        /// Pokud je null, je to způsobené nekonzistencí dat (je předán obsah tabulky, ale její jméno není uvedeno v deklaraci).
        /// </summary>
        public DataDeclaration DataDeclaration { get; private set; }
        /// <summary>
        /// Vlastnosti tabulky, načtené z DataDeclaration
        /// </summary>
        public DataGraphProperties DataGraphProperties { get; private set; }
        /// <summary>
        /// Přidá další data, dodaná ve formě serializované <see cref="DataTable"/> do this tabulky
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tableType"></param>
        public void AddTable(string data, DataTableType tableType)
        {
            string text = WorkSchedulerSupport.Decompress(data);
            DataTable dataTable = WorkSchedulerSupport.TableDeserialize(text);
            switch (tableType)
            {
                case DataTableType.Row:
                    this.AddTableRow(dataTable);
                    break;
                case DataTableType.Graph:
                    this.AddTableGraph(dataTable);
                    break;
                case DataTableType.Rel:
                    this.AddTableRel(dataTable);
                    break;
                case DataTableType.Item:
                    this.AddTableItem(dataTable);
                    break;
            }
        }
        /// <summary>
        /// Metoda vloží data řádků.
        /// Lze vložit pouze jednu tabulku; další pokus o vložení skončí chybou.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void AddTableRow(DataTable dataTable)
        {
            if (this.TableRow != null)
                throw new GraphLibDataException("Duplicitní zadání dat typu Row pro tabulku <" + this.TableName + ">.");
            this._TableRow = Table.CreateFrom(dataTable);
            this._TableRow.OpenRecordForm += _TableRow_OpenRecordForm;
            if (this.TableRow.AllowPrimaryKey)
                this.TableRow.HasPrimaryIndex = true;
        }
        /// <summary>
        /// Obsluha události, kdy tabulka sama (řádek nebo statický vztah) chce otevírat záznam
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TableRow_OpenRecordForm(object sender, GPropertyEventArgs<GId> e)
        {
            this.RunOpenRecordForm(e.Value);
        }
        /// <summary>
        /// Metoda přidá data grafických prvků.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void AddTableGraph(DataTable dataTable)
        {
            WorkSchedulerSupport.CheckTable(dataTable, WorkSchedulerSupport.DATA_TABLE_GRAPH_STRUCTURE);
            foreach (DataRow row in dataTable.Rows)
            {   // Grafické řádky se vytvářejí přímo z DataRow, nepotřebujeme kvůli nim konvertovat DataTable na Table:
                DataGraphItem dataGraphItem = DataGraphItem.CreateFrom(this, row);
                this.AddGraphItem(dataGraphItem);
            }
        }
        protected void AddTableRel(DataTable dataTable)
        {
            // Doplnit strukturu a načítání vztahů
        }
        /// <summary>
        /// Provede uložení dat typu Item = textové informace o položce grafu.
        /// Tabulka musí umožnit <see cref="Table.AllowPrimaryKey"/>.
        /// </summary>
        /// <param name="dataTable"></param>
        protected void AddTableItem(DataTable dataTable)
        {
            Table table = Table.CreateFrom(dataTable);
            if (!table.AllowPrimaryKey)
                throw new GraphLibDataException("Data typu Item pro tabulku <" + this.TableName + "> nepodporují PrimaryKey.");
            table.HasPrimaryIndex = true;
            this._TableInfoList.Add(table);
        }
        /// <summary>
        /// Finalizuje dosud načtená data. Další data se již načítat nebudou.
        /// </summary>
        internal void LoadFinalise()
        {
            if (this.DataDeclaration == null || this.TableRow == null)
                return;

            using (var scope = App.Trace.Scope(TracePriority.Priority3_BellowNormal, "DataGraphTable", "LoadFinalise", ""))
            {
                this.CreateGraphs();
                this.FillGraphItems();
            }
        }
        #endregion
        #region Tvorba a modifikace grafů
        /// <summary>
        /// Metoda vytvoří grafy do položek tabulky řádků
        /// </summary>
        protected void CreateGraphs()
        {
            if (!this.IsTableRowWithGraph) return;

            switch (this.DataGraphProperties.GraphPosition.Value)
            {
                case DataGraphPositionType.InLastColumn:
                    this.CreateGraphLastColumn();
                    break;
                case DataGraphPositionType.OnBackgroundProportional:
                case DataGraphPositionType.OnBackgroundLogarithmic:
                    this.CreateGraphBackground();
                    break;
            }
        }
        /// <summary>
        /// Připraví do tabulky <see cref="TableRow"/> nový sloupec pro graf, nastaví vlastnosti sloupce i grafu,
        /// a do každého řádku této tabulky vloží (do tohoto nového sloupce) nový <see cref="GTimeGraph"/>.
        /// </summary>
        protected void CreateGraphLastColumn()
        {
            Column graphColumn = new Column("__time__graph__");

            graphColumn.ColumnProperties.AllowColumnResize = true;
            graphColumn.ColumnProperties.AllowColumnSortByClick = false;
            graphColumn.ColumnProperties.AutoWidth = true;
            graphColumn.ColumnProperties.ColumnContent = ColumnContentType.TimeGraph;
            graphColumn.ColumnProperties.IsVisible = true;
            graphColumn.ColumnProperties.WidthMininum = 250;

            graphColumn.GraphParameters = new TimeGraphProperties();
            graphColumn.GraphParameters.TimeAxisMode = TimeGraphTimeAxisMode.Standard;
            graphColumn.GraphParameters.TimeAxisVisibleTickLevel = AxisTickType.StdTick;
            graphColumn.GraphParameters.InitialResizeMode = AxisResizeContentMode.ChangeScale;
            graphColumn.GraphParameters.InitialValue = this.CreateInitialTimeRange();
            graphColumn.GraphParameters.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;

            this.TableRow.Columns.Add(graphColumn);
            this.TableRowGraphColumn = graphColumn;

            this.AddTimeGraphToRows();
        }
        /// <summary>
        /// Připraví do tabulky <see cref="TableRow"/> data (nastavení) pro graf, který se zobrazuje na pozadí,
        /// a do každého řádku této tabulky vloží (do property <see cref="Table.BackgroundValue"/>) nový <see cref="GTimeGraph"/>.
        /// </summary>
        protected void CreateGraphBackground()
        {
            this.TableRow.GraphParameters = new TimeGraphProperties();
            this.TableRow.GraphParameters.TimeAxisMode = this.TimeAxisMode;
            this.TableRow.GraphParameters.TimeAxisVisibleTickLevel = AxisTickType.BigTick;

            this.AddTimeGraphToRows();
        }
        /// <summary>
        /// Metoda zajistí, že všechny řádky v tabulce <see cref="TableRow"/> budou mít korektně vytvořený graf,
        /// a to buď ve sloupci <see cref="TableRowGraphColumn"/>, anebo jako <see cref="Row.BackgroundValue"/>.
        /// Pokud již graf je vytvořen, nebude vytvářet nový.
        /// </summary>
        protected void AddTimeGraphToRows()
        {
            Column graphColumn = this.TableRowGraphColumn;
            foreach (Row row in this.TableRow.Rows)
                this.AddTimeGraphToRow(row, graphColumn);
        }
        /// <summary>
        /// Metoda zajistí, že daný řádek bude mít korektně vytvořený graf,
        /// a to buď ve sloupci <see cref="TableRowGraphColumn"/>, anebo jako <see cref="Row.BackgroundValue"/>.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="graphColumn"></param>
        protected void AddTimeGraphToRow(Row row, Column graphColumn)
        {
            GId rowGId = row.RecordGId;
            if (graphColumn != null)
            {
                Cell graphCell = row[graphColumn];
                if (graphCell.ValueType != TableValueType.ITimeInteractiveGraph)
                    graphCell.Value = this.CreateGTimeGraph(rowGId, true);
            }
            else
            {
                if (row.BackgroundValueType != TableValueType.ITimeInteractiveGraph)
                    row.BackgroundValue = this.CreateGTimeGraph(rowGId, false);
            }
        }
        /// <summary>
        /// Vrátí časový interval, který se má zobrazit jako výchozí v grafu.
        /// </summary>
        /// <returns></returns>
        protected TimeRange CreateInitialTimeRange()
        {
            DateTime now = DateTime.Now;
            int dow = (now.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)now.DayOfWeek) - 1);
            DateTime begin = new DateTime(now.Year, now.Month, now.Day).AddDays(-dow);
            DateTime end = begin.AddDays(7d);
            double add = 6d;
            return new TimeRange(begin.AddHours(-add), end.AddHours(add));
        }
        /// <summary>
        /// Vytvoří a vrátí new instanci grafu (třída <see cref="GTimeGraph"/>), kompletně připravenou k práci, ale bez položek (ty se dodávají později).
        /// Do instance grafu se vepíše její <see cref="GTimeGraph.GraphId"/> = index odpovídající danému GId řádku (parametr "rowGId").
        /// </summary>
        /// <param name="rowGId"></param>
        /// <param name="isFullInteractive"></param>
        /// <returns></returns>
        protected GTimeGraph CreateGTimeGraph(GId rowGId, bool isFullInteractive)
        {
            GTimeGraph graph = new GTimeGraph();
            graph.DataSource = this;
            graph.GraphId = this.GetId(rowGId);
            return graph;
        }
        /// <summary>
        /// Metoda zajistí vložení všech načtených položek grafů do odpovídajících grafů v tabulce TableRow.
        /// </summary>
        protected void FillGraphItems()
        {
            this.FillGraphItems(this.GraphItems);
        }
        /// <summary>
        /// Metoda zajistí vložení zadaných položek grafů do odpovídajících grafů v tabulce TableRow.
        /// </summary>
        protected void FillGraphItems(IEnumerable<DataGraphItem> graphItems)
        {
            foreach (var graphItem in graphItems)
                this.FillGraphItem(graphItem);
        }
        /// <summary>
        /// Metoda zajistí vložení dané položky graf do odpovídajícího grafu v tabulce TableRow.
        /// </summary>
        protected void FillGraphItem(DataGraphItem graphItem)
        {
            GTimeGraph timeGraph;
            if (!this.TryGetGraphForItem(graphItem, out timeGraph)) return;
            timeGraph.ItemList.Add(graphItem);
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit objekt <see cref="GTimeGraph"/> pro položku grafu dle parametru.
        /// Vyhledá řádek v tabulce <see cref="TableRow"/> podle <see cref="DataGraphItem.ParentGId"/>,
        /// a v řádku najde a vrátí graf podle režimu zobrazení grafu: buď z Value posledního columnu, nebo z <see cref="Row.BackgroundValue"/>
        /// </summary>
        /// <param name="graphItem"></param>
        /// <param name="timeGraph"></param>
        /// <returns></returns>
        protected bool TryGetGraphForItem(DataGraphItem graphItem, out GTimeGraph timeGraph)
        {
            timeGraph = null;
            Row row;
            if (graphItem.ParentGId == null || !this.TableRow.TryGetRowOnPrimaryKey(graphItem.ParentGId, out row)) return false;
            switch (this.GraphPosition)
            {
                case DataGraphPositionType.InLastColumn:
                    if (this.TableRowGraphColumn != null)
                        timeGraph = row[this.TableRowGraphColumn].Value as GTimeGraph;
                    break;
                case DataGraphPositionType.OnBackgroundLogarithmic:
                case DataGraphPositionType.OnBackgroundProportional:
                    timeGraph = row.BackgroundValue as GTimeGraph;
                    break;
            }
            return (timeGraph != null);
        }
        /// <summary>
        /// Obsahuje true, pokud this tabulka má zobrazit graf
        /// </summary>
        protected bool IsTableRowWithGraph
        {
            get
            {
                DataGraphPositionType graphPosition = this.GraphPosition;
                return (graphPosition == DataGraphPositionType.InLastColumn ||
                        graphPosition == DataGraphPositionType.OnBackgroundProportional ||
                        graphPosition == DataGraphPositionType.OnBackgroundLogarithmic);
            }
        }
        /// <summary>
        /// Režim časové osy v grafu, podle zadání v deklaraci
        /// </summary>
        protected TimeGraphTimeAxisMode TimeAxisMode
        {
            get
            {
                DataGraphPositionType graphPosition = this.GraphPosition;
                switch (graphPosition)
                {
                    case DataGraphPositionType.InLastColumn: return TimeGraphTimeAxisMode.Standard;
                    case DataGraphPositionType.OnBackgroundProportional: return TimeGraphTimeAxisMode.ProportionalScale;
                    case DataGraphPositionType.OnBackgroundLogarithmic: return TimeGraphTimeAxisMode.LogarithmicScale;
                }
                return TimeGraphTimeAxisMode.Default;
            }
        }
        /// <summary>
        /// Pozice grafu. Obsahuje None, pokud graf není definován.
        /// </summary>
        protected DataGraphPositionType GraphPosition
        {
            get
            {
                if (this.TableRow == null || this.DataDeclaration == null || this.DataGraphProperties == null) return DataGraphPositionType.None;
                DataGraphPositionType? gp = this.DataGraphProperties.GraphPosition;
                return (gp.HasValue ? gp.Value : DataGraphPositionType.None);
            }
        }
        /// <summary>
        /// Sloupec hlavní tabulky, který zobrazuje graf při umístění <see cref="DataGraphPositionType.InLastColumn"/>
        /// </summary>
        protected Column TableRowGraphColumn { get; private set; }
        #endregion
        #region Data - tabulka s řádky, prvky grafů, vztahů, položky s informacemi
        /// <summary>
        /// Tabulka s řádky.
        /// Tato tabulka je zobrazována.
        /// </summary>
        public Table TableRow { get { return this._TableRow; } }
        protected Table _TableRow;
        /// <summary>
        /// Data položek všech grafů (=ze všech řádků) tabulky <see cref="TableRow"/>
        /// </summary>
        public IEnumerable<DataGraphItem> GraphItems { get { return this._GraphItemDict.Values; } }
        /// <summary>
        /// Index pro obousměrnou konverzi Int32 - GId
        /// </summary>
        protected Index<GId> _GIdIndex;
        /// <summary>
        /// Dictionary pro vyhledání prvku grafu podle jeho GId. Primární úložiště položek grafů.
        /// </summary>
        protected Dictionary<GId, DataGraphItem> _GraphItemDict;
        #endregion
        #region Textové informace pro položky grafů - tabulka TableInfoList a její obsluha
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro daný prvek.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="graphItem"></param>
        /// <returns></returns>
        protected Row GetTableInfoRow(DataGraphItem graphItem)
        {
            if (graphItem == null) return null;
            return this.GetTableInfoRow(graphItem.ItemGId, graphItem.GroupGId, graphItem.DataGId, graphItem.ParentGId);
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro některý GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="gids"></param>
        /// <returns></returns>
        protected Row GetTableInfoRow(params GId[] gids)
        {
            foreach (GId gId in gids)
            {
                Row row = this.GetTableInfoRowForGId(gId);
                if (row != null) return row;
            }
            return null;
        }
        /// <summary>
        /// Metoda se pokusí najít první řádek z tabulky INFO, obsahující textové informace pro daný GID.
        /// Může vrátit NULL.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected Row GetTableInfoRowForGId(GId gId)
        {
            if (gId == null) return null;

            foreach (Table table in this._TableInfoList)
            {
                Row row;
                if (table.TryGetRowOnPrimaryKey(gId, out row))
                    return row;
            }
            return null;
        }
        /// <summary>
        /// Tabulky s informacemi = popisky pro položky grafů.
        /// </summary>
        public List<Table> TableInfoList { get { return this._TableInfoList; } }
        protected List<Table> _TableInfoList;
        #endregion
        #region Správa indexů GId a objektů grafů
        /// <summary>
        /// Metoda vrátí Int32 ID pro daný <see cref="GId"/>.
        /// Pro opakovaný požadavek na tentýž <see cref="GId"/> vrací shodnou hodnotu ID.
        /// Pro první požadavek na určitý <see cref="GId"/> vytvoří nový ID.
        /// Reverzní metoda je <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected int GetId(GId gId)
        {
            if (gId == null) return 0;
            return this._GIdIndex.GetIndex(gId);
        }
        /// <summary>
        /// Pro daný ID vrátí <see cref="GId"/>, ale pouze pokud byl přidělen v metodě <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected GId GetGId(int id)
        {
            if (id == 0) return null;
            GId gId;
            if (!this._GIdIndex.TryGetKey(id, out gId)) return null;
            return gId;
        }
        /// <summary>
        /// Metoda uloží danou položku grafu do interního úložiště <see cref="_GraphItemDict"/>.
        /// </summary>
        /// <param name="dataGraphItem"></param>
        /// <returns></returns>
        protected void AddGraphItem(DataGraphItem dataGraphItem)
        {
            if (dataGraphItem == null || dataGraphItem.ItemGId == null) return;
            GId gId = dataGraphItem.ItemGId;
            if (!this._GraphItemDict.ContainsKey(gId))
                this._GraphItemDict.Add(gId, dataGraphItem);
        }
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected DataGraphItem GetGraphItem(int id)
        {
            return this.GetGraphItem(this.GetGId(id));
        }
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho GId
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        protected DataGraphItem GetGraphItem(GId gId)
        {
            if (gId == null) return null;
            DataGraphItem dataGraphItem;
            if (!this._GraphItemDict.TryGetValue(gId, out dataGraphItem)) return null;
            return dataGraphItem;
        }
        #region Explicitní implementace IDataGraphTableInternal
        int IDataGraphTableInternal.GetId(GId gId) { return this.GetId(gId); }
        GId IDataGraphTableInternal.GetGId(int id) { return this.GetGId(id); }
        DataGraphItem IDataGraphTableInternal.GetGraphItem(int id) { return this.GetGraphItem(id); }
        DataGraphItem IDataGraphTableInternal.GetGraphItem(GId gId) { return this.GetGraphItem(gId); }
        #endregion
        #endregion
        #region Komunikace s hlavním zdrojem dat (MainData)
        /// <summary>
        /// Tato metoda zajistí otevření formuláře daného záznamu.
        /// Pouze převolá odpovídající metodu v <see cref="MainData"/>.
        /// </summary>
        /// <param name="recordGId"></param>
        protected void RunOpenRecordForm(GId recordGId)
        {
            if (this.MainData != null)
                this.IMainData.RunOpenRecordForm(recordGId);
        }
        #endregion
        #region Implementace ITimeGraphDataSource: Zdroj dat pro grafy
        /// <summary>
        /// Připraví tooltip pro položku grafu
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemPrepareToolTip(CreateToolTipArgs args)
        {
            DataGraphItem graphItem = this.GetActionGraphItem(args);
            if (graphItem == null) return;

            Row infoRow = this.GetTableInfoRow(graphItem);
            if (infoRow == null) return;

            GId recordGId = infoRow.RecordGId;
            args.ToolTipData.TitleText = (recordGId != null ? recordGId.ClassName : "INFORMACE O POLOŽCE");

            StringBuilder sb = new StringBuilder();
            foreach (Column column in infoRow.Table.Columns)
            {
                if (!column.ColumnProperties.IsVisible) continue;
                Cell cell = infoRow[column];
                if (cell.ValueType == TableValueType.Text)
                    sb.AppendLine(column.ColumnProperties.Title + "\t" + cell.Value);
            }
            string text = sb.ToString();
            args.ToolTipData.InfoText = text;
            args.ToolTipData.AnimationFadeInTime = TimeSpan.FromMilliseconds(100);
            args.ToolTipData.AnimationShowTime = TimeSpan.FromMilliseconds(100 * text.Length);     // 1 sekunda na přečtení 10 znaků
            args.ToolTipData.AnimationFadeOutTime = TimeSpan.FromMilliseconds(10 * text.Length);
            args.ToolTipData.InfoUseTabs = true;
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném grafu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForGraph(ItemActionArgs args)
        {
            return this.IMainData.CreateContextMenu(null, args);
        }
        /// <summary>
        /// Uživatel chce vidět kontextové menu na daném prvku
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected ToolStripDropDownMenu GetContextMenuForItem(ItemActionArgs args)
        {
            DataGraphItem graphItem = this.GetActionGraphItem(args);                               // Prvek, na nějž se kliklo
            return this.IMainData.CreateContextMenu(graphItem, args);
        }
        /// <summary>
        /// Uživatel dal doubleclick na grafický prvek
        /// </summary>
        /// <param name="args"></param>
        protected void GraphItemDoubleClick(ItemActionArgs args)
        {
            if (args.ModifierKeys == Keys.Control)
            {   // Akce typu Ctrl+DoubleClick na grafickém prvku si žádá otevření formuláře:
                DataGraphItem graphItem = this.GetActionGraphItem(args);
                if (graphItem != null)
                    this.RunOpenRecordForm(graphItem.DataGId);
            }
        }
        /// <summary>
        /// Metoda najde a vrátí grafický prvek zdejší třídy <see cref="DataGraphItem"/> pro daný interaktivní prvek.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected DataGraphItem GetActionGraphItem(ItemArgs args)
        {
            int itemId = (args.CurrentItem != null ? args.CurrentItem.ItemId : (args.GroupedItems.Length > 0 ? args.GroupedItems[0].ItemId : 0));
            if (itemId <= 0) return null;
            return this.GetGraphItem(itemId);
        }
        void ITimeGraphDataSource.CreateText(CreateTextArgs args) { }
        void ITimeGraphDataSource.CreateToolTip(CreateToolTipArgs args) { this.GraphItemPrepareToolTip(args); }
        void ITimeGraphDataSource.GraphRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForGraph(args); }
        void ITimeGraphDataSource.ItemRightClick(ItemActionArgs args) { args.ContextMenu = this.GetContextMenuForItem(args); }
        void ITimeGraphDataSource.ItemDoubleClick(ItemActionArgs args) { this.GraphItemDoubleClick(args); }
        void ITimeGraphDataSource.ItemLongClick(ItemActionArgs args) { }
        void ITimeGraphDataSource.ItemChange(ItemChangeArgs args) { }
        #endregion
    }
    /// <summary>
    /// Rozhraní pro přístup k interním metodám třídy DataGraphTable
    /// </summary>
    public interface IDataGraphTableInternal
    {
        /// <summary>
        /// Metoda vrátí Int32 ID pro daný <see cref="GId"/>.
        /// Pro opakovaný požadavek na tentýž <see cref="GId"/> vrací shodnou hodnotu ID.
        /// Pro první požadavek na určitý <see cref="GId"/> vytvoří nový ID.
        /// Reverzní metoda je <see cref="GetGId(int)"/>.
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        int GetId(GId gId);
        /// <summary>
        /// Pro daný ID vrátí <see cref="GId"/>, ale pouze pokud byl přidělen v metodě <see cref="GetId(GId)"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        GId GetGId(int id);
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        DataGraphItem GetGraphItem(int id);
        /// <summary>
        /// Najde a vrátí položku grafu podle jeho GId
        /// </summary>
        /// <param name="gId"></param>
        /// <returns></returns>
        DataGraphItem GetGraphItem(GId gId);
    }
    #endregion
    #region class DataGraphItem : jedna položka grafu, implementuje ITimeGraphItem, je vykreslována v tabulce
    /// <summary>
    /// DataGraphItem : jedna položka grafu, implementuje ITimeGraphItem, je vykreslována v tabulce
    /// </summary>
    public class DataGraphItem : ITimeGraphItem
    {
        #region Konstrukce, načítání dat, proměné
        /// <summary>
        /// Metoda vytvoří a vrátí instanci položky grafu z dodaného řádku s daty.
        /// </summary>
        /// <param name="graphTable"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static DataGraphItem CreateFrom(DataGraphTable graphTable, DataRow row)
        {
            if (row == null) return null;

            DataGraphItem item = new DataGraphItem(graphTable);
            // Struktura řádku: parent_rec_id int; parent_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; back_color string; join_back_color string; data string
            item._ParentGId = GetGId(row, "parent");
            item._ItemGId = GetGId(row, "item");
            item._GroupGId = GetGId(row, "group");
            item._DataGId = GetGId(row, "data");
            item._Layer = row.GetValue<Int32>("layer");
            item._Level = row.GetValue<Int32>("level");
            item._Order = 0;
            item._Height = row.GetValue<float>("height");
            item._Time = new TimeRange(row.GetValue<DateTime?>("time_begin"), row.GetValue<DateTime?>("time_end"));
            item._BackColor = MainData.GetColor(row.GetValue<string>("back_color"));
            item._BorderColor = null;
            item._BackStyle = null;
            item._LinkBackColor = MainData.GetColor(row.GetValue<string>("join_back_color"));
            item._LoadData(row.GetValue<string>("data"));

            // ID pro grafickou vrstvu:
            IDataGraphTableInternal iGraphTable = graphTable as IDataGraphTableInternal;
            item._ItemId = iGraphTable.GetId(item.ItemGId);
            item._GroupId = iGraphTable.GetId(item.GroupGId);

            return item;
        }
        /// <summary>
        /// Metoda rozebere string "data" na KeyValues a z nich naplní další nepovinné prvky.
        /// </summary>
        /// <param name="data"></param>
        protected void _LoadData(string data)
        {
            this._LoadDataDefault();
            if (String.IsNullOrEmpty(data)) return;

            // data mají formát: "key: value; key:value; key: value", 
            // například: "EditMode: ResizeTime + ResizeHeight + MoveToAnotherTime; BackStyle: Percent50; BorderColor: Black"
            var items = data.ToKeyValues(";", ":", true, true);
            foreach (var item in items)
            {
                string key = item.Key.ToLower();
                switch (key)
                {
                    case "editmode":
                    case "edit_mode":
                        this._EditMode = Scheduler.MainData.GetEditMode(item.Value);
                        break;
                    case "backstyle":
                    case "back_style":
                        this._BackStyle = Scheduler.MainData.GetHatchStyle(item.Value);
                        break;
                    case "bordercolor":
                    case "border_color":
                        this._BorderColor = Scheduler.MainData.GetColor(item.Value);
                        break;
                        // ...a další klíče a hodnoty mohou následovat:
                }
            }
        }
        /// <summary>
        /// Naplní defaultní hodnoty podle čísla třídy prvku
        /// </summary>
        protected void _LoadDataDefault()
        {
            switch (this._DataGId.ClassId)
            {
                case GreenClasses.PlanUnitCCl:
                    this._BackStyle = System.Drawing.Drawing2D.HatchStyle.Percent25;
                    this._EditMode = GraphItemEditMode.None;
                    break;
                case GreenClasses.PlanUnitCUnit:
                    this._BackStyle = null;
                    this._EditMode = GraphItemEditMode.DefaultWorkTime;
                    break;
            }
        }
        /// <summary>
        /// Metoda z daného řádku načte hodnoty pro číslo třídy a číslo záznamu, a z nich vrátí <see cref="GId"/>.
        /// Jako název (parametr name) dostává základ jména dvojice sloupců, které obsahují třídu a záznam.
        /// Například pro dvojici sloupců "parent_rec_id" a "parent_class_id" se jako name předává "parent".
        /// Pokud sloupce neexistují, dojde k chybě.
        /// Pokud obsahují null, vrací se null.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static GId GetGId(DataRow row, string name)
        {
            Int32? classId = row.GetValue<Int32?>(name + "_class_id");
            Int32? recordId = row.GetValue<Int32?>(name + "_rec_id");
            if (!(classId.HasValue && recordId.HasValue)) return null;
            return new GId(classId.Value, recordId.Value);
        }
        /// <summary>
        /// privátní konstruktor. Instanci lze založit pomocí metody <see cref="CreateFrom(DataGraphTable, DataRow)"/>.
        /// </summary>
        private DataGraphItem(DataGraphTable graphTable)
        {
            this._GraphTable = graphTable;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Item: " + this._ItemGId.ToString() + "; Time: " + this._Time.ToString() + "; Height: " + this._Height.ToString();
        }
        /// <summary>
        /// Vlastník = datová základna, instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        protected MainData MainData { get { return this.GraphTable.MainData; } }
        /// <summary>
        /// Vlastník prvku = celá tabulka
        /// </summary>
        private DataGraphTable _GraphTable;
        private ITimeInteractiveGraph _OwnerGraph;
        private GId _ParentGId;
        private GId _ItemGId;
        private GId _GroupGId;
        private GId _DataGId;
        private int _ItemId;
        private int _GroupId;
        private int _Layer;
        private int _Level;
        private int _Order;
        private float _Height;
        private GraphItemEditMode _EditMode;
        private TimeRange _Time;
        private Color? _BackColor;
        private System.Drawing.Drawing2D.HatchStyle? _BackStyle;
        private Color? _BorderColor;
        private Color? _LinkBackColor;
        private GTimeGraphControl _GControl;
        #endregion
        #region Aplikační data - identifikátory atd
        /// <summary>
        /// Vlastník prvku grafu = tabulka s komplexními daty
        /// </summary>
        public DataGraphTable GraphTable { get { return this._GraphTable; } }
        /// <summary>
        /// Veřejný identifikátor MAJITELE PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o Kapacitní plánovací jednotku.
        /// </summary>
        public GId ParentGId { get { return this._ParentGId; } }
        /// <summary>
        /// Veřejný identifikátor GRAFICKÉHO PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o záznam třídy Stav kapacit, nebo Pracovní jednotka.
        /// </summary>
        public GId ItemGId { get { return this._ItemGId; } }
        /// <summary>
        /// Veřejný identifikátor SKUPINY PRVKU (obsahuje číslo třídy a číslo záznamu).
        /// Může jít o záznam třídy Paralelní průchod.
        /// </summary>
        public GId GroupGId { get { return this._GroupGId; } }
        /// <summary>
        /// Veřejný identifikátor DATOVÉHO OBJEKTU: obsahuje číslo třídy a číslo záznamu.
        /// Může jít o Operaci výrobního příkazu.
        /// </summary>
        public GId DataGId { get { return this._DataGId; } }
        /// <summary>
        /// Číslo grafické vrstvy (Z-order).
        /// </summary>
        public int Layer { get { return this._Layer; } }
        /// <summary>
        /// Číslo grafické hladiny (Y-group).
        /// </summary>
        public int Level { get { return this._Level; } }
        /// <summary>
        /// Číslo pořadí (sub-Y-group)
        /// </summary>
        public int Order { get { return this._Order; } }
        /// <summary>
        /// Logická výška grafického prvku, 1=normální jednotková výška
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        public GraphItemEditMode EditMode { get { return this._EditMode; } }
        /// <summary>
        /// Časový interval tohoto prvku
        /// </summary>
        public TimeRange Time { get { return this._Time; } }
        /// <summary>
        /// Barva pozadí prvku
        /// </summary>
        public Color? BackColor { get { return this._BackColor; } }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// </summary>
        public System.Drawing.Drawing2D.HatchStyle? BackStyle { get { return this._BackStyle; } }
        /// <summary>
        /// Barva okrajů prvku
        /// </summary>
        public Color? BorderColor { get { return this._BorderColor; } }
        /// <summary>
        /// Barva spojovací linky prvků
        /// </summary>
        public Color? LinkBackColor { get { return this._LinkBackColor; } }

        #endregion
        #region Podpora pro kreslení a interaktivitu
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/> : <see cref="GTimeGraphControl.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        protected void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            this._GControl.DrawItem(e, boundsAbsolute, drawMode);
        }
        #endregion
        #region Explicitní implementace rozhraní ITimeGraphItem
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        int ITimeGraphItem.ItemId { get { return this._ItemId; } } 
        int ITimeGraphItem.GroupId { get { return this._GroupId; } } 
        int ITimeGraphItem.Layer { get { return this._Layer; } } 
        int ITimeGraphItem.Level { get { return this._Level; } } 
        int ITimeGraphItem.Order { get { return this._Order; } }
        float ITimeGraphItem.Height { get { return this._Height; } }
        GraphItemEditMode ITimeGraphItem.EditMode { get { return this._EditMode; } }
        TimeRange ITimeGraphItem.Time { get { return this._Time; } }
        Color? ITimeGraphItem.BackColor { get { return this._BackColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._BackStyle; } }
        Color? ITimeGraphItem.BorderColor { get { return this._BorderColor; } }
        Color? ITimeGraphItem.LinkBackColor { get { return this._LinkBackColor; } }
        GTimeGraphControl ITimeGraphItem.GControl { get { return this._GControl; } set { this._GControl = value; } }
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
    #endregion
    #region class DataGraphProperties : vlastnosti tabulky, popis chování atd - načteno z dodaných dat
    /// <summary>
    /// DataGraphProperties : vlastnosti tabulky, popis chování atd - načteno z dodaných dat
    /// </summary>
    public class DataGraphProperties
    {
        #region Konstrukce, načtení
        /// <summary>
        /// Vytvoří a vrátí instanci DataGraphProperties,vloží do ní dodaná data.
        /// </summary>
        /// <param name="dataGraphTable"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataGraphProperties CreateFrom(DataGraphTable dataGraphTable, string data)
        {
            DataGraphProperties dataGraphProperties = new DataGraphProperties(dataGraphTable);
            dataGraphProperties.LoadData(data);
            return dataGraphProperties;
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        /// <param name="dataGraphTable"></param>
        private DataGraphProperties(DataGraphTable dataGraphTable)
        {
            this.DataGraphTable = dataGraphTable;
        }
        /// <summary>
        /// Načte data do this objektu z datového stringu
        /// </summary>
        /// <param name="data">Obsahuje formát: "GraphPosition: LastColumn; LineHeight: 16; MaxHeight: 320"</param>
        protected void LoadData(string data)
        {
            if (data == null) return;
            var items = data.ToKeyValues(";", ":", true, true);
            foreach (var item in items)
            {
                switch (item.Key)
                {
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_POSITION:
                        this.GraphPosition = MainData.GetGraphPosition(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_LINE_HEIGHT:
                        this.GraphLineHeight = MainData.GetInt32N(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_MIN_HEIGHT:
                        this.RowLineHeightMin = MainData.GetInt32N(item.Value);
                        break;
                    case WorkSchedulerSupport.DATA_TABLE_GRAPH_MAX_HEIGHT:
                        this.RowLineHeightMax = MainData.GetInt32N(item.Value);
                        break;
                }
            }
        }
        /// <summary>
        /// Vlastník = tabulka
        /// </summary>
        protected DataGraphTable DataGraphTable { get; private set; }
        #endregion
        #region Public data
        /// <summary>
        /// Pozice grafu v tabulce
        /// </summary>
        public DataGraphPositionType? GraphPosition { get; private set; }
        /// <summary>
        /// Výška jednotky v grafu, v pixelech
        /// </summary>
        public int? GraphLineHeight { get; private set; }
        /// <summary>
        /// Výška řádku v tabulce minimální, v pixelech
        /// </summary>
        public int? RowLineHeightMin { get; private set; }
        /// <summary>
        /// Výška řádku v tabulce maximální, v pixelech
        /// </summary>
        public int? RowLineHeightMax { get; private set; }

        #endregion
    }
    #endregion
    #region enumy : DataTableType, DataTargetType, DataContentType
    /// <summary>
    /// Typ údajů, které obsahuje určitá tabulka
    /// </summary>
    public enum DataTableType
    {
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
