using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Components;
using System.Drawing;
using System.Windows.Forms;

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
            this._InitComponents();
            this._InitDataSources();
            this._LoadGlobalFunctions();
            this.CalculateLayout();
        }
        private void _InitComponents()
        {
            this._MainToolbar = new GToolBar() { Bounds = new Rectangle(0, 0, 1024, 64) };
            this._MainToolbar.ToolbarSizeChanged += _MainToolbar_ToolbarSizeChanged;
            this.AddItem(this._MainToolbar);

            // this._SchedulerPanel = new SchedulerPanel() { Bounds = new Rectangle(0, this._MainToolbar.Bounds.Bottom, 1024, 640) };
            // this._SchedulerPanel.BackColor = Color.LightCyan;
            // this.AddItem(this._SchedulerPanel);
        }
        private void _MainToolbar_ToolbarSizeChanged(object sender, GPropertyChangeArgs<Services.ComponentSize> e)
        {
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
        /// Instance toolbaru
        /// </summary>
        private GToolBar _MainToolbar;
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
        #region Datové zdroje
        /// <summary>
        /// Naplní funkce do Toolbaru
        /// </summary>
        private void _LoadGlobalFunctions()
        {
            this._MainToolbar.FillFunctionGlobals();
        }
        /// <summary>
        /// Provede inicializaci datových zdrojů
        /// </summary>
        private void _InitDataSources()
        {
            this._GetDataSources();
            this._GetSchedulerPanels();
        }
        /// <summary>
        /// Načte soupis dostupných datových zdrojů (=pluginy)
        /// </summary>
        private void _GetDataSources()
        {
            List<Services.IDataSource> sourceList = new List<Services.IDataSource>();
            using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "GetDataSources", "GUIThread"))
            {
                var plugins = Application.App.GetPlugins(typeof(Services.IDataSource));
                foreach (object plugin in plugins)
                {
                    Services.IDataSource source = plugin as Services.IDataSource;
                    if (source != null)
                        sourceList.Add(source);
                }
            }
            this._DataSources = sourceList.ToArray();
        }
        /// <summary>
        /// Načte soupis tabulek z datového zdroje
        /// </summary>
        private void _GetSchedulerPanels()
        {
            List<SchedulerPanel> panelList = new List<SchedulerPanel>();
            using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "LoadFromDataSources", "GUIThread"))
            {
                foreach (Services.IDataSource dataSource in this._DataSources)
                {
                    DataSourceGetTablesRequest request = new DataSourceGetTablesRequest(null);
                    DataSourceGetTablesResponse response = dataSource.ProcessRequest(request) as DataSourceGetTablesResponse;
                    if (response != null)
                    {
                        SchedulerPanel panel = new SchedulerPanel(dataSource, response);
                        panelList.Add(panel);
                    }
                }
            }
            this._SchedulerPanels = panelList.ToArray();
        }
        /*
        private void _ProcessResponseData(Services.DataSourceGetDataRequest request, Services.DataSourceResponse response)
        {
            if (this.InvokeRequired)
            {
                Application.App.TraceInfo(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "ProcessResponseData", "WorkerThread", "InvokeGUI");
                this.BeginInvoke(new Action<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(this._ProcessResponseData), request, response);
            }
            else
            {
                using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "ProcessResponseData", "GUIThread"))
                {
                    


                }
            }
        }
        */
        private Services.IDataSource[] _DataSources;
        #endregion
    }
}
