using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Services;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// <summary>
    /// MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// </summary>
    public class MainData : IMainDataInternal
    {
        #region Konstrukce a proměnné
        public MainData(Scheduler.IAppHost host)
        {
            this._AppHost = host;
        }
        private Scheduler.IAppHost _AppHost;
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
        /// Metoda zajistí provedení finalizace dat po jejich kompletním načtení.
        /// </summary>
        protected void _LoadDataFinalise()
        {
            this._LoadDataDeclarationFinalise();
            this._LoadDataGraphTableFinalise();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        private void _LoadDataOne(string key, string data)
        {
            string tableName;
            DataTableType tableType;
            if (key == WorkSchedulerSupport.KEY_REQUEST_DATA_DECLARATION)
            {   // Deklarace dat:
                this._LoadDataDeclaration(data);
            }
            else if (IsKeyRequestTable(key, out tableName, out tableType))
            {   // Tabulka s daty:
                this._LoadDataGraphTable(data, tableName, tableType);
            }
        }
        #region Deklarace dat
        /// <summary>
        /// Metoda zkusí vrátit deklaraci dat pro danou tabulku
        /// </summary>
        /// <param name="tableName">Název tabulky</param>
        /// <param name="dataDeclaration"></param>
        /// <returns></returns>
        protected bool TryGetDataDeclarationForTable(string tableName, out DataDeclaration dataDeclaration)
        {
            string key = DataDeclaration.GetKey(DataContentType.Table, tableName);
            return this.TryGetDataDeclaration(key, out dataDeclaration);
        }
        /// <summary>
        /// Metoda zkusí vrátit deklaraci dat s daným klíčem
        /// </summary>
        /// <param name="key">Klíč položky deklarace</param>
        /// <param name="dataDeclaration"></param>
        /// <returns></returns>
        protected bool TryGetDataDeclaration(string key, out DataDeclaration dataDeclaration)
        {
            dataDeclaration = null;
            if (String.IsNullOrEmpty(key)) return false;
            return this.DeclarationDict.TryGetValue(key, out dataDeclaration);
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

            Dictionary<string, DataDeclaration> declarationDict = new Dictionary<string, DataDeclaration>();
            foreach (DataRow row in dataTable.Rows)
            {
                DataDeclaration declaration = DataDeclaration.CreateFrom(this, row);
                if (declaration != null)
                {
                    string key = declaration.Key;
                    if (!declarationDict.ContainsKey(key))
                        declarationDict.Add(key, declaration);
                }
            }
            this.DeclarationDict = declarationDict;
        }
        /// <summary>
        /// Finalizuje informace, popisující deklaraci dat.
        /// V této době jsou načteny všechny datové tabulky (ale ty ještě neprošly finalizací).
        /// </summary>
        private void _LoadDataDeclarationFinalise()
        { }
        /// <summary>
        /// Deklarace dat = popis funkcí a tabulek
        /// </summary>
        protected Dictionary<string, DataDeclaration> DeclarationDict { get; private set; }
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
                DataDeclaration dataDeclaration;
                this.TryGetDataDeclarationForTable(tableName, out dataDeclaration);
                dataGraphTable = new DataGraphTable(this, tableName, dataDeclaration);
                this.GraphTableDict.Add(tableName, dataGraphTable);
            }
            dataGraphTable.AddTable(data, tableType);
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
                case "None": return DataGraphPositionType.None;
                case "LastColumn":
                case "InLastColumn": return DataGraphPositionType.InLastColumn;
                case "Background":
                case "Proportional":
                case "OnBackgroundProportional": return DataGraphPositionType.OnBackgroundProportional;
                case "Logarithmic":
                case "OnBackgroundLogarithmic": return DataGraphPositionType.OnBackgroundLogarithmic;
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
        #endregion
        #region Tvorba GUI

        public System.Windows.Forms.Control CreateGui()
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
        #region Implementace IMainDataInternal
        #endregion
    }
    public interface IMainDataInternal
    { }
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
        /// <summary>
        /// Vrátí stringový klíč pro dané hodnoty.
        /// </summary>
        /// <param name="content">Druh prvku</param>
        /// <param name="name">Název prvku</param>
        /// <returns></returns>
        public static string GetKey(DataContentType content, string name)
        {
            return content.ToString() + ":" + (name == null ? "" : name.Replace(":", "."));
        }
        #endregion
        #region Public data
        /// <summary>
        /// Obsahuje jednoznačný klíč této položky. Nesmějí existovat dvě položky v jedné deklaraci, které by měly shodný Key.
        /// </summary>
        public string Key { get { return GetKey(this.Content, this.Name); } }
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
    public class DataGraphTable : IDataGraphTableInternal
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
            this._TableItemList = new List<Table>();
            this._IdDictInit();
        }
        /// <summary>
        /// Vlastník = instance třídy <see cref="Scheduler.MainData"/>
        /// </summary>
        internal MainData MainData { get; private set; }
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

        private void _TableRow_OpenRecordForm(object sender, GPropertyEventArgs<GId> e)
        {
            System.Windows.Forms.MessageBox.Show("Rád bych otevřel záznam " + e.Value.ToString());
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
            this._TableItemList.Add(table);
        }
        /// <summary>
        /// Finalizuje dosud načtená data. Další data se již načítat nebudou.
        /// </summary>
        public void LoadFinalise()
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
        /// a do každého řádku této tabulky vloží (do tohoto sloupce) nový <see cref="GTimeGraph"/>.
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

            this.TableRow.Columns.Add(graphColumn);
            this.TableRowGraphColumn = graphColumn;

            foreach (Row row in this.TableRow.Rows)
                row[graphColumn].Value = new GTimeGraph();
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

            foreach (Row row in this.TableRow.Rows)
                row.BackgroundValue = new GTimeGraph();
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
        /// Dictionary pro vyhledání prvku grafu podle jeho GId. Primární úložiště položek grafů.
        /// </summary>
        protected Dictionary<GId, DataGraphItem> _GraphItemDict;
    
        /// <summary>
        /// Tabulky s inforacemi = popisky pro poožky grafů.
        /// </summary>
        public List<Table> TableItemList { get { return this._TableItemList; } }
        protected List<Table> _TableItemList;
        #endregion
        #region Správa ID, GId a objektů grafů
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
            int id;
            if (this._GIdIdDict.TryGetValue(gId, out id)) return id;

            id = this._IdNext++;
            this._GIdIdDict.Add(gId, id);
            this._IdGIdDict.Add(id, gId);
            return id;
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
            if (!this._IdGIdDict.TryGetValue(id, out gId)) return null;
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
        /// <summary>
        /// Inicializace objektů pro převody <see cref="GId"/> na Int32 atd
        /// </summary>
        private void _IdDictInit()
        {
            this._GIdIdDict = new Dictionary<GId, int>();
            this._IdGIdDict = new Dictionary<int, GId>();
            this._GraphItemDict = new Dictionary<GId, DataGraphItem>();
            this._IdNext = 1;
        }
        /// <summary>
        /// Dictionary pro převod <see cref="GId"/> na Int32
        /// </summary>
        private Dictionary<GId, int> _GIdIdDict;
        /// <summary>
        /// Dictionary pro převod Int32 na <see cref="GId"/>
        /// </summary>
        private Dictionary<int, GId> _IdGIdDict;
        /// <summary>
        /// ID pro následující nový prvek.
        /// Výchozí je 1, protože ID s hodnotou 0 značí nepřidělené ID.
        /// </summary>
        private int _IdNext;
        #region Explicitní implementace IDataGraphTableInternal
        int IDataGraphTableInternal.GetId(GId gId) { return this.GetId(gId); }
        GId IDataGraphTableInternal.GetGId(int id) { return this.GetGId(id); }
        DataGraphItem IDataGraphTableInternal.GetGraphItem(int id) { return this.GetGraphItem(id); }
        DataGraphItem IDataGraphTableInternal.GetGraphItem(GId gId) { return this.GetGraphItem(gId); }
        #endregion
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
            this.GraphTable = graphTable;
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
        /// Vlastník = tabulka
        /// </summary>
        protected DataGraphTable GraphTable { get; private set; }
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
    #region class DataGraphProperties : vlastnosti tabulky, popis chování atd
    /// <summary>
    /// DataGraphProperties : vlastnosti tabulky, popis chování atd
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
                    case "GraphPosition":
                        this.GraphPosition = MainData.GetGraphPosition(item.Value);
                        break;
                    case "LineHeight":
                        this.GraphLineHeight = MainData.GetInt32N(item.Value);
                        break;
                    case "MinHeight":
                        this.RowLineHeightMax = MainData.GetInt32N(item.Value);
                        break;
                    case "MaxHeight":
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
        #region Pubic data
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
    #region interface IAppHost : požadavky na objekt, který hraje roli hostitele Scheduleru.
    /// <summary>
    /// interface IAppHost : požadavky na objekt, který hraje roli hostitele Scheduleru.
    /// </summary>
    public interface IAppHost
    { }
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
        None,
        Main,
        ToolBar,
        Task,
        Schedule,
        Source,
        Info
    }
    /// <summary>
    /// Typ obsahu v deklaraci dat
    /// </summary>
    public enum DataContentType
    {
        None,
        Button,
        Table,
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




    /// <summary>
    /// Třída, která obsahuje veškerá data Scheduleru.
    /// Současně představuje jak zdroj globálních funkcí do ToolBaru (IFunctionGlobal), tak i zdroj dat pro grafy v GUI (IDataSource).
    /// </summary>
    public class SchedulerData : IFunctionGlobal, IDataSource
    {
        #region Konstruktor a privátní data
        public SchedulerData()
        {
            // Konstruktor vrátí new objekt, ale pouze pro použití jako Plugin.
            // Pro plnohodnotné použití a permanentní životnost je určen singleton Data.
            // Ten po vytvoření instance třídy SchedulerData navíc volá metodu Initialise(), která fyzicky naplní instanci potřebnými daty.
        }
        /// <summary>
        /// Singleton, jediná instance obsahující reálná data datového zdroje
        /// </summary>
        public static SchedulerData Data
        {
            get
            {
                if (__Data == null)
                {
                    lock (__Lock)
                    {
                        if (__Data == null)
                            __Data = CreateData();
                    }
                }
                return __Data;
            }
        }
        private static SchedulerData CreateData()
        {
            SchedulerData data = new SchedulerData();
            data.Initialise();
            return data;
        }
        private static SchedulerData __Data;
        private static object __Lock = new object();
        #endregion
        #region Inicializace dat objektu, který se používá jako datová základna
        /// <summary>
        /// Inicializace dat Scheduleru
        /// </summary>
        protected void Initialise()
        {
        }
        #endregion
        #region implementace IFunctionGlobal a IDataSource
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        /// <summary>
        /// Vytvoří a vrátí sadu globálních funkcí pro this plugin
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        FunctionGlobalPrepareResponse IFunctionGlobal.PrepareGui(FunctionGlobalPrepareGuiRequest request)
        {
            return Data.PrepareGui(request);
        }
        /// <summary>
        /// Metoda může prověřit funkce, vytvořené ostatními pluginy.
        /// Kterýkoli plugin tak může nastavit FunctionGlobalGroup.IsVisible = false, 
        /// nebo FunctionGlobalGroup.Items[].IsVisible nebo IsEnabled = false, a zajistit tak skrytí jakékoli funkce z jiné služby.
        /// </summary>
        /// <param name="request"></param>
        void IFunctionGlobal.CheckGui(FunctionGlobalCheckGuiRequest request)
        {
            Data.CheckGui(request);
        }
        /// <summary>
        /// Datový zdroj dostane jistý požadavek, a ten zpracuje a vrací data.
        /// Formát požadavku a vrácené odpovědi je závislý na konkrétní situaci.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        DataSourceResponse IDataSource.ProcessRequest(DataSourceRequest request)
        {
            return Data.ProcessRequest(request);
        }
        #endregion
        #region Tvorba prvků GUI (ToolBar), obsluha událostí vyvolaných v Toolbaru (eventhandlery)
        /// <summary>
        /// Připraví GUI.
        /// Běží v rámci Singletonu.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected FunctionGlobalPrepareResponse PrepareGui(FunctionGlobalPrepareGuiRequest request)
        {
            this._ToolBar = request.ToolBar;

            List<FunctionGlobalGroup> groups = new List<FunctionGlobalGroup>();
            this._PrepareGuiEdit(groups);
            this._PrepareGuiTime(groups);
            this._PrepareGuiShow(groups);

            FunctionGlobalPrepareResponse response = new FunctionGlobalPrepareResponse();
            response.Items = groups.ToArray();
            return response;
        }
        /// <summary>
        /// Zkontroluje všechny vytvořené prvky GUI, tzn. i z ostatníh modulů
        /// </summary>
        /// <param name="request"></param>
        protected void CheckGui(FunctionGlobalCheckGuiRequest request)
        { }
        #region Skupina Edit
        private void _PrepareGuiEdit(List<FunctionGlobalGroup> groups)
        {
            FunctionGlobalGroup group = new FunctionGlobalGroup(this);
            group.Title = "ÚPRAVY";
            group.Order = "A1";
            group.ToolTipTitle = "Úpravy zadaných dat";

            this.ButtonUndo = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditUndo, Text = "Zpět", IsEnabled = false, LayoutHint = LayoutHint.NextItemSkipToNextRow };
            this.ButtonUndo.Click += ButtonUndo_Click;
            this.ButtonRedo = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditRedo, Text = "Vpřed", IsEnabled = true };
            this.ButtonRedo.Click += ButtonRedo_Click;
            this.ButtonReload = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.Refresh, Text = "Přenačíst", ToolTip = "Zruší všechny provedené změny a znovu načte data z databáze", IsEnabled = true };
            this.ButtonReload.Click += ButtonReload_Click;
            this.ButtonSave = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.DocumentSave, Text = "Uložit", ToolTip = "Uloží všechny provedené změny do databáze", IsEnabled = false };
            this.ButtonSave.Click += ButtonSave_Click;

            group.Items.Add(this.ButtonUndo);
            group.Items.Add(this.ButtonRedo);
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });
            group.Items.Add(this.ButtonReload);
            group.Items.Add(this.ButtonSave);

            groups.Add(group);
        }
      
        /// <summary>
        /// Reference na objekt GToolBar, který reprezentuje hlavní toolbar aplikace.
        /// </summary>
        private GToolBar _ToolBar;
        private void ButtonUndo_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonRedo_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonReload_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonSave_Click(object sender, FunctionItemEventArgs args) { }

        protected FunctionGlobalItem ButtonUndo;
        protected FunctionGlobalItem ButtonRedo;
        protected FunctionGlobalItem ButtonReload;
        protected FunctionGlobalItem ButtonSave;
        #endregion
        #region Skupina : Řízení časové osy
        private void _PrepareGuiTime(List<FunctionGlobalGroup> groups)
        {
            FunctionGlobalGroup group = new FunctionGlobalGroup(this);
            group.Title = "ČASOVÁ OSA";
            group.Order = "D1";
            group.ToolTipTitle = "Řízení pohybu na časové ose";

            this.ButtonTimeWeek5 = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.EditUndo, Text = "Týden 5 (Po-Pá)", LayoutHint = LayoutHint.NextItemOnSameRow };
            this.ButtonTimeWeek5.Click += ButtonTimeWeek5_Click;
            this.ButtonTimeWeek7 = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.EditRedo, Text = "Týden 7 (Po-Ne)", LayoutHint = LayoutHint.NextItemSkipToNextRow };
            this.ButtonTimeWeek7.Click += ButtonTimeWeek7_Click;
            this.ButtonTimePrev = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.GoLeft, Text = "", ToolTip = "Zobrazí předchozí týden", ModuleWidth = 3, LayoutHint = LayoutHint.NextItemOnSameRow };
            this.ButtonTimePrev.Click += ButtonTimePrev_Click;
            this.ButtonTimeCurr = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.DocumentSave, Text = "Jdi na dnešek", ToolTip = "Zobrazí aktuální týden", LayoutHint = LayoutHint.NextItemOnSameRow };
            this.ButtonTimeCurr.Click += ButtonTimeCurr_Click;
            this.ButtonTimeNext = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.GoRight, Text = "", ToolTip = "Zobrazí následující týden", ModuleWidth = 3, LayoutHint = LayoutHint.NextItemSkipToNextTable };
            this.ButtonTimeNext.Click += ButtonTimeNext_Click;

            group.Items.Add(this.ButtonTimeWeek5);
            group.Items.Add(this.ButtonTimeWeek7);
            group.Items.Add(this.ButtonTimePrev);
            group.Items.Add(this.ButtonTimeCurr);
            group.Items.Add(this.ButtonTimeNext);
            // group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });

            groups.Add(group);
        }

        private void ButtonTimeWeek5_Click(object sender, FunctionItemEventArgs args)
        {
            
        }
        private void ButtonTimeWeek7_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonTimePrev_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonTimeCurr_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonTimeNext_Click(object sender, FunctionItemEventArgs args) { }
        protected FunctionGlobalItem ButtonTimeWeek5;
        protected FunctionGlobalItem ButtonTimeWeek7;
        protected FunctionGlobalItem ButtonTimePrev;
        protected FunctionGlobalItem ButtonTimeCurr;
        protected FunctionGlobalItem ButtonTimeNext;
        #endregion
        #region Skupina Show
        private void _PrepareGuiShow(List<FunctionGlobalGroup> groups)
        { }

        #endregion
        #endregion
        #region Zpracování požadavku z GUI vrstvy
        protected DataSourceResponse ProcessRequest(DataSourceRequest request)
        {
            if (request == null) return null;
            if (request is DataSourceGetTablesRequest) return this.ProcessRequestGetTables(request as DataSourceGetTablesRequest);
           // if (request is DataSourceGetDataRequest) return this.ProcessRequestGetData(request as DataSourceGetDataRequest);

            return null;
        }
        protected DataSourceResponse ProcessRequestGetTables(DataSourceGetTablesRequest request)
        {
            DataSourceGetTablesResponse response = new DataSourceGetTablesResponse(request);

            DataDescriptor dataDescriptor = new DataDescriptor();
            dataDescriptor.DataId = 123456;
            dataDescriptor.Title = "Dílna LISY";
            dataDescriptor.ToolTip = "Nabízí možnosti uspořádat práci na této dílně";

            Table productOrders = new Table() { Title = "Výrobní příkazy" };
            Table planItems = new Table() { Title = "Plán výroby" };
            dataDescriptor.TaskTables = new Table[] { productOrders, planItems };

            Table workPlaceSchedule = new Table();
            Table workPlaceSource = new Table();
            dataDescriptor.ScheduleTables = new Table[] { workPlaceSchedule, workPlaceSource };



            response.DataDescriptorList.Add(dataDescriptor);


            return response;
        }
        #endregion
    }
}
