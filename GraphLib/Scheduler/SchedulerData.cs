using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Services;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// <summary>
    /// MainData : hlavní řídící prvek dat zobrazovaných v controlu <see cref="MainControl"/>.
    /// </summary>
    public class MainData
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
                    string key, data;
                    if (buffer.ReadNextData(out key, out data))
                        this._LoadDataOne(key, data);
                }
            }
            catch (Exception exc)
            {   // Zatím nijak explicitně neřešíme:
                throw;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        private void _LoadDataOne(string key, string data)
        {
            string tableName, tableType;
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
        /// Načte informace, popisující deklaraci dat.
        /// Na vstupu je komprimovaný text, obsahující serializovanou DataTable, která popisuje deklaraci dat.
        /// </summary>
        /// <param name="data"></param>
        private void _LoadDataDeclaration(string data)
        {
            string text = WorkSchedulerSupport.Decompress(data);
            DataTable table = WorkSchedulerSupport.TableDeserialize(text);
            WorkSchedulerSupport.CheckTable(table, WorkSchedulerSupport.DATA_DECLARATION_STRUCTURE);

            List<DataDeclaration> declarationList = new List<DataDeclaration>();
            foreach (DataRow row in table.Rows)
            {
                DataDeclaration declaration = DataDeclaration.CreateFrom(row);
                if (declaration != null)
                    declarationList.Add(declaration);
            }

            this.Declarations = declarationList.ToArray();
        }
        /// <summary>
        /// Deklarace dat = popis funkcí a tabulek
        /// </summary>
        protected DataDeclaration[] Declarations { get; private set; }
        #endregion
        #region Data tabulek

        private void _LoadDataGraphTable(string data, string tableName, string tableType)
        {
            DataGraphTable dataGraphTable;
            if (!this.GraphTableDict.TryGetValue(tableName, out dataGraphTable))
            {
                dataGraphTable = new DataGraphTable(this, tableName);
                this.GraphTableDict.Add(tableName, dataGraphTable);
            }
            dataGraphTable.AddTable(data, tableType);
        }
        /// <summary>
        /// Klíč z requestu typu "Table.workplace_table.Row.0" rozdělí na části, 
        /// z nichž název tabulky (zde "workplace_table") uloží do out tableName,
        /// a druh dat v tabulce (zde "Row") uloží do out tableType.
        /// Vrací true¨, pokud vstupující klíč obsahuje vyhovující data, nebo vrací false, pokud na vstupu je něco nerozpoznatelného.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tableName"></param>
        /// <param name="tableType"></param>
        /// <returns></returns>
        protected static bool IsKeyRequestTable(string key, out string tableName, out string tableType)
        {
            tableName = null;
            tableType = null;
            if (String.IsNullOrEmpty(key)) return false;
            string[] parts = key.Split('.');
            if (parts.Length < 3) return false;
            if (parts[0] != "Table") return false;
            tableName = parts[1];
            tableType = parts[2];
            return (!String.IsNullOrEmpty(tableName) && (tableType == "Row" || tableType == "Graph" || tableType == "Rel" || tableType == "Item"));
        }
        /// <summary>
        /// Dictionary obsahující data jednotlivých tabulek
        /// </summary>
        protected Dictionary<string, DataGraphTable> GraphTableDict { get; private set; }
        #endregion
        #endregion
        #region Tvorba GUI

        public System.Windows.Forms.Control CreateGui()
        {
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox() { Bounds = new System.Drawing.Rectangle(25, 8, 450, 150), Multiline = true, Text = "Data scheduleru" };
            System.Windows.Forms.Panel panel = new System.Windows.Forms.Panel();
            panel.Controls.Add(textBox);
            return panel;
        }


        #endregion
    }
    #endregion
    #region class DataDeclaration : deklarace dat, předaná z volajícího do pluginu, definuje rozsah dat a funkcí
    /// <summary>
    /// DataDeclaration : deklarace dat, předaná z volajícího do pluginu, definuje rozsah dat a funkcí
    /// </summary>
    public class DataDeclaration
    {
        #region Tvorba instance
        public static DataDeclaration CreateFrom(DataRow row)
        {
            if (row == null) return null;

            DataDeclaration data = new DataDeclaration();
            data.DataId = row.GetValue<int>("data_id");
            data.Target = GetDataTargetType(row.GetValue<string>("target"));
            data.Content = GetDataContentType(row.GetValue<string>("content"));
            data.Name = row.GetValue<string>("name");
            data.Title = row.GetValue<string>("title");
            data.ToolTip = row.GetValue<string>("tooltip");
            data.Image = row.GetValue<string>("image");
            data.Data = row.GetValue<string>("data");

            return data;
        }
        private DataDeclaration() { }
        public override string ToString()
        {
            return "Target: " + this.Target + "; Content: " + this.Content + "; Title: " + this.Title;
        }
        protected static DataTargetType GetDataTargetType(string text)
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
        protected static DataContentType GetDataContentType(string text)
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
    #endregion
    #region class DataGraphTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// <summary>
    /// DataGraphTable : obsah dat jedné logické tabulky: shrnuje v sobě fyzické řádky, položky grafů, vztahy položek grafů a popisky položek grafů
    /// </summary>
    public class DataGraphTable
    {
        #region Konstrukce, vkládání dat
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="tableName"></param>
        public DataGraphTable(MainData owner, string tableName)
        {
            this.Owner = owner;
            this.TableName = tableName;
        }
        protected MainData Owner { get; private set; }
        public string TableName { get; private set; }
        public void AddTable(string data, string tableType)
        {
            string text = WorkSchedulerSupport.Decompress(data);
            DataTable dataTable = WorkSchedulerSupport.TableDeserialize(text);
            switch (tableType)
            {
                case "Row":
                    this.AddTableRow(dataTable);
                    break;
                case "Graph":
                    this.AddTableGraph(dataTable);
                    break;
                case "Rel":
                    this.AddTableRel(dataTable);
                    break;
                case "Item":
                    this.AddTableItem(dataTable);
                    break;
            }
        }
        protected void AddTableRow(DataTable dataTable)
        {
            Table table = Table.CreateFrom(dataTable);
        }
        protected void AddTableGraph(DataTable dataTable)
        { }
        protected void AddTableRel(DataTable dataTable)
        { }
        protected void AddTableItem(DataTable dataTable)
        { }
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
