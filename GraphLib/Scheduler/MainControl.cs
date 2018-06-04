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
            this._InitToolBar();
            this._TabHeaderInit();
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
            int th = this._MainToolbar.Bounds.Height;
            int y = 0;
            this._MainToolbar.Bounds = new Rectangle(y, 0, size.Width, th);
            y = this._MainToolbar.Bounds.Bottom + 1;
            // this._SchedulerPanel.Bounds = new Rectangle(0, y, size.Width, size.Height - y);
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
        private void _InitToolBar()
        {
            this._MainToolbar = new GToolBar() { Bounds = new Rectangle(0, 0, 1024, 64) };
            this._MainToolbar.ToolbarSizeChanged += _MainToolbar_ToolbarSizeChanged;
            this.AddItem(this._MainToolbar);

            this._LoadGlobalFunctions();

            // this._SchedulerPanel = new SchedulerPanel() { Bounds = new Rectangle(0, this._MainToolbar.Bounds.Bottom, 1024, 640) };
            // this._SchedulerPanel.BackColor = Color.LightCyan;
            // this.AddItem(this._SchedulerPanel);
        }
        private void _MainToolbar_ToolbarSizeChanged(object sender, GPropertyChangeArgs<ComponentSize> e)
        {
            this.CalculateLayout();
        }
        /// <summary>
        /// Naplní funkce do Toolbaru
        /// </summary>
        private void _LoadGlobalFunctions()
        {
            this._MainToolbar.FillFunctionGlobals();
        }
        /// <summary>
        /// Instance toolbaru
        /// </summary>
        private GToolBar _MainToolbar;
        #endregion
        #region TabHeader nad panely (pokud je více než jeden datový zdroj)
        private void _TabHeaderInit()
        {
            this._MainTabHeader = new GTabHeader() { Position = RectangleSide.Top };
            this._MainTabHeader.ActivePageChanged += _TabHeaderActiveItemChanged;
            this.AddItem(this._MainTabHeader);
        }
        /// <summary>
        /// Po změně aktivní záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabHeaderActiveItemChanged(object sender, GPropertyChangeArgs<GTabPage> e)
        {
            // Záložka automaticky zajišťuje přepínání IsVisible na navázaných objektech (třída MainPanel)
        }
        /// <summary>
        /// Zajistí přidání nové záložky do _MainTabHeader pro předaný panel
        /// </summary>
        /// <param name="panel"></param>
        private void _TabHeaderAddItem(SchedulerPanel panel)
        {
            if (panel == null) return;
            var tabItem = this._MainTabHeader.AddHeader(panel.Title, panel.Icon, linkItem: panel);

        }
        /// <summary>
        /// TabHeader, který umožní přepínat mezi hlavními panely různých datových zdrojů
        /// </summary>
        private GTabHeader _MainTabHeader;

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
        /// Načte soupis dostupných datových zdrojů (=pluginy)
        /// </summary>
        private void _PrepareData()
        {
            List<DataSourcePanel> dataList = new List<DataSourcePanel>();
            using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "GetDataSources", "GUIThread"))
            {
                var plugins = Application.App.GetPlugins(typeof(IDataSource));
                foreach (object plugin in plugins)
                {
                    IDataSource source = plugin as IDataSource;
                    if (source != null)
                    {
                        SchedulerPanel panel = this._GetDataPanel(source);
                        if (panel != null)
                        {
                            DataSourcePanel data = new DataSourcePanel(source, panel);
                            dataList.Add(data);
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
        private SchedulerPanel _GetDataPanel(IDataSource dataSource)
        {
            SchedulerPanel panel = null;
            try
            {
                DataSourceGetTablesRequest request = new DataSourceGetTablesRequest(null);
                DataSourceGetTablesResponse response = dataSource.ProcessRequest(request) as DataSourceGetTablesResponse;
                if (response != null)
                {
                    panel = new SchedulerPanel(dataSource, response);
                    this.AddItem(panel);
                    this._TabHeaderAddItem(panel);
                }
            }
            catch (Exception exc)
            {
                Type dataSourceType = dataSource.GetType();
                string dataSourceName = dataSourceType.Namespace + "." + dataSourceType.Name;
                Application.App.TraceException(exc, $"Error {exc.Message} in datasource {dataSourceName} on processing request: GetTables.");
                panel = null;
            }
            return panel;
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
            DataSourceGetDataRequest request = new DataSourceGetDataRequest(null, data.DataPanel);
            Application.App.ProcessRequestOnbackground<DataSourceGetDataRequest, DataSourceResponse>(data.DataSource.ProcessRequest, request, this._LoadDataOneResponse);
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
                using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "ProcessResponseData", "GUIThread"))
                {
                    


                }
            }
        }
        */
        private DataSourcePanel[] _Data;
        protected class DataSourcePanel
        {
            public DataSourcePanel(IDataSource source, SchedulerPanel panel)
            {
                this.DataSource = source;
                this.DataPanel = panel;
            }
            public IDataSource DataSource { get; private set; }
            public SchedulerPanel DataPanel { get; private set; }
        }
        #endregion
    }
}
