using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Components;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Services;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Hlavní control Dílenské tabule: obsahuje <see cref="GToolBar"/> + <see cref="SchedulerPanel"/>.
    /// </summary>
    public class MainControl : GInteractiveControl
    {
        #region Konstruktor, inicializace, privátní proměnné grafiky
        public MainControl()
        {
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this._ToolBarInit();
            this._TabDataInit();
            this._InitData();
            this.CalculateLayout();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.CalculateLayout();
        }
        protected void CalculateLayout()
        {
            Size size = this.ClientSize;
            int th = this._ToolBar.Bounds.Height;
            int y = 0;
            this._ToolBar.Bounds = new Rectangle(y, 0, size.Width, th);
            y = this._ToolBar.Bounds.Bottom + 1;
            this._TabData.Bounds = new Rectangle(0, y, size.Width, size.Height - y);
            this.Refresh();
        }
        /// <summary>
        /// Instance všech Scheduler panelů
        /// </summary>
        private SchedulerPanel[] _SchedulerPanels;
        /// <summary>
        /// Index aktuálního Scheduler panelu
        /// </summary>
        private int _CurrentSchedulerPanelIndex;
        /// <summary>
        /// true pokud se má zobrazovat záhlaví panelů, tzn. je více než jeden Scheduler panel (máme více než jeden DataSource)
        /// </summary>
        protected bool SelectPanelExists { get { return (this._SchedulerPanels != null && this._SchedulerPanels.Length > 1); } }
        /// <summary>
        /// true pokud aktuálně máme vybraný panel v <see cref="SchedulerPanelCurrent"/>
        /// </summary>
        protected bool SchedulerPanelExists { get { return (this._SchedulerPanels != null && this._CurrentSchedulerPanelIndex >= 0 && this._CurrentSchedulerPanelIndex < this._SchedulerPanels.Length); } }
        /// <summary>
        /// Aktuálně zobrazovaný Scheduler panel
        /// </summary>
        protected SchedulerPanel SchedulerPanelCurrent { get { return (this.SchedulerPanelExists ? this._SchedulerPanels[this._CurrentSchedulerPanelIndex] : null); } }
        #endregion
        #region ToolBar
        private void _ToolBarInit()
        {
            this._ToolBar = new GToolBar() { Bounds = new Rectangle(0, 0, 1024, 64) };
            this._ToolBar.ToolbarSizeChanged += _ToolBarSizeChanged;
            this.AddItem(this._ToolBar);

            this._ToolBarLoad();
        }
        private void _ToolBarSizeChanged(object sender, GPropertyChangeArgs<ComponentSize> e)
        {
            this.CalculateLayout();
        }
        /// <summary>
        /// Naplní funkce do Toolbaru
        /// </summary>
        private void _ToolBarLoad()
        {
            this._ToolBar.FillFunctionGlobals();
        }
        /// <summary>
        /// Instance toolbaru
        /// </summary>
        private GToolBar _ToolBar;
        #endregion
        #region TabHeader nad panely (pokud je více než jeden datový zdroj)
        private void _TabDataInit()
        {
            this._TabData = new GTabContainer(this) { TabHeaderPosition = RectangleSide.Top, TabHeaderMode = ShowTabHeaderMode.Default };
            this._TabData.ActivePageChanged += _TabDataActivePageChanged;
            this.AddItem(this._TabData);
        }
        /// <summary>
        /// Po změně záložky, která reprezentuje komplexní GUI datového zdroje
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabDataActivePageChanged(object sender, GPropertyChangeArgs<GTabPage> e)
        {
            
        }
        /// <summary>
        /// Přidá novou záložku do containeru <see cref="_TabData"/>
        /// </summary>
        /// <param name="panel"></param>
        private GTabPage _TabDataAdd(SchedulerPanel panel)
        {
            this._TabData.AddItem(panel);
            return this._TabData.AddTabItem(panel, panel.Title, toolTip: panel.ToolTip, image: panel.Icon);
        }
        /// <summary>
        /// Záložky s daty jednotlivých datových zdrojů
        /// </summary>
        private GTabContainer _TabData;
        #endregion
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
        /// Pro každý <see cref="DataDescriptor"/> založí jednu záložku v <see cref="_TabData"/>.
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
                            GTabPage page = this._TabDataAdd(panel);
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
                        GTabPage page = this._TabDataAdd(panel);
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
        #endregion
    }
}
