using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Scheduler
{



    /*
    
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
    */


    class OldMainControl
    {
        /*
        #region Datové zdroje
        /// <summary>
        /// Provede inicializaci datových zdrojů
        /// </summary>
        private void _InitData()
        {
            this._PrepareData();
            this._LoadData();
        }
        /// <summary>
        /// Načte soupis dostupných datových zdrojů (=pluginy).
        /// Z datových zdrojů získá popisky dat <see cref="DataDescriptor"/>.
        /// Pro každý <see cref="DataDescriptor"/> založí jednu záložku v <see cref="_TabContainer"/>.
        /// </summary>
        private void _PrepareData()
        {
            List<DataSourcePanel> dataList = new List<DataSourcePanel>();
            using (Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "GetDataSources", "GUIThread"))
            {
                var plugins = Application.App.GetPlugins(typeof(IDataSource));
                foreach (object plugin in plugins)
                {
                    IDataSource source = plugin as IDataSource;
                    if (source != null)
                    {
                        IEnumerable<DataDescriptor> dataDescriptors = this._GetDataPanel(source);
                        foreach (DataDescriptor dataDescriptor in dataDescriptors)
                        {
                            SchedulerPanel panel = new SchedulerPanel(source, dataDescriptor);
                            GTabPage page = this._TabContainerAdd(panel);
                            DataSourcePanel data = new DataSourcePanel(source, dataDescriptor, panel, page);
                        }
                    }
                }
            }
            this._Data = dataList.ToArray();
        }
        /// <summary>
        /// Pro daný datový zdroj vytvoří grafický panel (SchedulerPanel), pro panel vytvoří záložku v <see cref="_MainTabHeader"/>, 
        /// panel vloží do this.Items a panel poté vrátí.
        /// </summary>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        private IEnumerable<DataDescriptor> _GetDataPanel(IDataSource dataSource)
        {
            List<DataDescriptor> dataDescriptors = new List<DataDescriptor>();
            try
            {
                DataSourceGetTablesRequest request = new DataSourceGetTablesRequest(null);
                DataSourceGetTablesResponse response = dataSource.ProcessRequest(request) as DataSourceGetTablesResponse;
                if (response != null && response.DataDescriptorList != null)
                    dataDescriptors.AddRange(response.DataDescriptorList);

                {
                    foreach (DataDescriptor dataDescriptor in response.DataDescriptorList)
                    {
                        SchedulerPanel panel = new SchedulerPanel(dataSource, dataDescriptor);
                        GTabPage page = this._TabContainerAdd(panel);
                    }
                }
            }
            catch (Exception exc)
            {
                Type dataSourceType = dataSource.GetType();
                string dataSourceName = dataSourceType.Namespace + "." + dataSourceType.Name;
                Application.App.Trace.Exception(exc, "Error " + exc.Message + " in datasource " + dataSourceName + " on processing request: GetTables.");
                dataDescriptors.Clear();
            }
            return dataDescriptors.ToArray();
        }
        /// <summary>
        /// Metoda zajistí nastartování procesu načítání dat z datového zdroje (ze všech zdrojů) do jeho panelu, to vše na pozadí.
        /// </summary>
        private void _LoadData()
        {
            foreach (DataSourcePanel data in this._Data)
                this._LoadDataOne(data);
        }
        /// <summary>
        /// Nastartuje načítání dat pro jeden datový zdroj a jeden panel
        /// </summary>
        /// <param name="data"></param>
        private void _LoadDataOne(DataSourcePanel data)
        {
            if (data.DataDescriptor.NeedLoadData)
            {
                DataSourceGetDataRequest request = new DataSourceGetDataRequest(null, data.DataPanel);
                Application.App.ProcessRequestOnbackground<DataSourceGetDataRequest, DataSourceResponse>(data.DataSource.ProcessRequest, request, this._LoadDataOneResponse);
            }
        }
        /// <summary>
        /// Metoda je volána v threadu na pozadí, po dokončení zpracování požadavku <see cref="DataSourceGetDataRequest"/> v rámci datového zdroje.
        /// Tato metoda má za úkol zajistit dokončení zpracování dat.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        private void _LoadDataOneResponse(DataSourceGetDataRequest request, DataSourceResponse response)
        {

        }
        
        /*
        private void _ProcessResponseData(DataSourceGetDataRequest request, DataSourceResponse response)
        {
            if (this.InvokeRequired)
            {
                Application.App.TraceInfo(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "ProcessResponseData", "WorkerThread", "InvokeGUI");
                this.BeginInvoke(new Action<DataSourceGetDataRequest, DataSourceResponse>(this._ProcessResponseData), request, response);
            }
            else
            {
                using (Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "ProcessResponseData", "GUIThread"))
                {
                    


                }
            }
        }
        */


        /*
        private DataSourcePanel[] _Data;
        /// <summary>
        /// Třída, která spojuje datový zdroj <see cref="IDataSource"/>, descriptor konkrétních dat <see cref="Scheduler.DataDescriptor"/>, 
        /// jeho GUI <see cref="SchedulerPanel"/> a záložku, pod kterou je zobrazován <see cref="GTabPage"/>.
        /// </summary>
        protected class DataSourcePanel
        {
            public DataSourcePanel(IDataSource source, DataDescriptor dataDescriptor, SchedulerPanel panel, GTabPage page)
            {
                this.DataSource = source;
                this.DataDescriptor = dataDescriptor;
                this.DataPanel = panel;
                this.Page = page;
            }
            /// <summary>
            /// Datový zdroj, který zde zobrazuje data.
            /// Jeden datový zdroj může zobrazovat více dat, každá sada dat má svůj <see cref="Scheduler.DataDescriptor"/>, 
            /// svůj <see cref="SchedulerPanel"/> a svoji záložku <see cref="GTabPage"/>.
            /// </summary>
            public IDataSource DataSource { get; private set; }
            /// <summary>
            /// Popisek dat. Tento popisovač vytvořil datový zdroj <see cref="DataSource"/>, tato data jsou zde obsažena a zobrazena.
            /// </summary>
            public DataDescriptor DataDescriptor { get; private set; }
            /// <summary>
            /// GUI panel, který fyzicky zobrazuje data
            /// </summary>
            public SchedulerPanel DataPanel { get; private set; }
            /// <summary>
            /// Záložka, pod kterou jsou tato data zobrazena.
            /// </summary>
            public GTabPage Page { get; private set; }
        }
        */
    }
}
