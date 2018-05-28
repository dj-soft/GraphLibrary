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
    /// Hlavní control Dílenské tabule: obsahuje <see cref="GToolbar"/> + <see cref="SchedulerPanel"/>.
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
            this._MainToolbar = new GToolbar() { Bounds = new Rectangle(0, 0, 1024, 64) };
            this._MainToolbar.ToolbarSizeChanged += _MainToolbar_ToolbarSizeChanged;
            this.AddItem(this._MainToolbar);

            this._SchedulerPanel = new SchedulerPanel() { Bounds = new Rectangle(0, this._MainToolbar.Bounds.Bottom, 1024, 640) };
            this._SchedulerPanel.BackColor = Color.LightCyan;
            this.AddItem(this._SchedulerPanel);
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
            this._SchedulerPanel.Bounds = new Rectangle(0, y, size.Width, size.Height - y);
            this.Refresh();
        }

        private GToolbar _MainToolbar;
        private SchedulerPanel _SchedulerPanel;
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
            this._LoadFromDataSources();
        }
        /// <summary>
        /// Načte soupis dostupných datových zdrojů
        /// </summary>
        private void _GetDataSources()
        {
            using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "GetDataSources", "GUIThread"))
            {
                this._DataSourceList = new List<Services.IDataSource>();
                var plugins = Application.App.GetPlugins(typeof(Services.IDataSource));
                foreach (object plugin in plugins)
                {
                    Services.IDataSource source = plugin as Services.IDataSource;
                    if (source != null)
                        this._DataSourceList.Add(source);
                }
            }
        }
        private void _LoadFromDataSources()
        {
            using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "MainControl", "LoadFromDataSources", "GUIThread"))
            {
                Services.IDataSource source = this._DataSourceList.FirstOrDefault();
                if (source != null)
                {
                    Services.DataSourceGetDataRequest request = new Services.DataSourceGetDataRequest(null);
                    Application.App.ProcessRequestOnbackground<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(source.ProcessRequest, request, this._ProcessResponseData);
                }
            }
        }
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
        private List<Services.IDataSource> _DataSourceList;
        #endregion
    }
}
