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
                this.GraphTableList = new List<DataGraphTable>();
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
            int? dataId;
            string itemName;
            DataTableType tableType;
            if (key == WorkSchedulerSupport.KEY_REQUEST_DATA_DECLARATION)
            {   // Deklarace dat:
                this._LoadDataDeclaration(data);
            }
            else if (IsKeyRequestTable(key, out dataId, out itemName, out tableType))
            {   // Tabulka s daty:
                this._LoadDataGraphTable(data, dataId, itemName, tableType);
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
        /// <param name="dataId">DataId tabulky</param>
        /// <param name="tableName">Název tabulky</param>
        /// <param name="tableType">Typ dat, načtený z klíče (obsahuje string: Row, Graph, Rel, Item)</param>
        private void _LoadDataGraphTable(string data, int? dataId, string tableName, DataTableType tableType)
        {
            DataGraphTable dataGraphTable = this.GetGraphTable(dataId, tableName);
            if (dataGraphTable == null)
            {   // Nová tabulka = založit DataGraphTable:
                DataDeclaration dataDeclaration = this.SearchDataDeclarationForTable(tableName);
                dataGraphTable = new DataGraphTable(this, tableName, dataDeclaration);
                this.GraphTableList.Add(dataGraphTable);
            }
            try
            {   // Do existující tabulky DataGraphTable vložit nová datá daného typu:
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
        protected DataGraphTable GetGraphTable(int? dataId, string tableName)
        {
            DataGraphTable dataGraphTable = null;
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
            foreach (DataGraphTable dataGraphTable in this.GraphTableList)
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
        protected List<DataGraphTable> GraphTableList { get; private set; }
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
            foreach (var graphTable in this.GraphTableList)
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
            this._GGrid.AddTable(this.GraphTableList.FirstOrDefault().TableRow);

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
            FunctionGlobalGroup group = new FunctionGlobalGroup();
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
            // Nejprve sestavíme jednotlivé grupy pro prvky, podle názvu grup kam chtějí jít:
            Dictionary<string, FunctionGlobalGroup> toolBarGroups = new Dictionary<string, FunctionGlobalGroup>();
            string defaultGroupName = "FUNKCE";
            foreach (ToolBarItem toolBarItem in this._ToolBarItems)
            {
                string groupName = toolBarItem.GroupName;
                if (String.IsNullOrEmpty(groupName)) groupName = defaultGroupName;
                FunctionGlobalGroup group;
                if (!toolBarGroups.TryGetValue(groupName, out group))
                {
                    group = new FunctionGlobalGroup() { Title = groupName };
                    toolBarGroups.Add(groupName, group);
                }
                group.Items.Add(toolBarItem);
            }

            // Výsledky (jednotlivé grupy, kde každá obsahuje sadu prvků = buttonů) vložím do předaného pole:
            if (toolBarGroups.Count > 0)
                groupList.AddRange(toolBarGroups.Values);
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
                this.Size = FunctionGlobalItemSize.Half;
                this.ItemType = FunctionGlobalItemType.Button;
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
                if (declaration.Content != DataContentType.Button) return null;

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
                        case WorkSchedulerSupport.DATA_BUTTON_GROUPNAME:
                            this.GroupName = item.Value;
                            break;
                    }
                }
            }
            #endregion
            #region Public property FunctionGlobalItem, načítané z DataDeclaration, a explicitně přidané
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
            /// <summary>
            /// Název grupy, kde se tento prvek objeví. Nezadaná grupa = implicitní s názvem "FUNKCE".
            /// </summary>
            public string GroupName { get; set; }
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
