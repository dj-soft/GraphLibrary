using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GUI = Noris.LCS.Base.WorkScheduler;
using WS = Asol.Tools.WorkScheduler.Scheduler;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    public partial class SchedulerForm : Form
    {
        public SchedulerForm()
        {
            InitializeApplication();

            using (App.Trace.Scope("SchedulerForm", "InitializeComponent", ""))
                InitializeComponent();

            using (App.Trace.Scope("SchedulerForm", "InitializeScheduler", ""))
                InitializeScheduler();
        }
        protected void InitializeApplication()
        {
            App.AppCompanyName = "Asseco Solutions";
            App.AppProductName = "WorkScheduler";
            App.AppProductTitle = "Dílenské plánování";
            App.Trace.Info("SchedulerForm", "InitializeApplication", "", "First row");
        }
        protected void InitializeScheduler()
        {
            try
            {
                using (App.Trace.Scope("SchedulerDataSource", ".ctor", ""))
                    this.DataSource = new SchedulerDataSource();

                using (App.Trace.Scope("SchedulerDataSource", "CreateGuiData", ""))
                    this.GuiData = this.DataSource.CreateGuiData();

                using (var scope = App.Trace.Scope(Application.TracePriority.Priority3_BellowNormal, "SchedulerForm", "Initialize.MainData", ""))
                {
                    using (App.Trace.Scope("MainData", ".ctor", ""))
                        this.MainData = new WS.MainData(this.DataSource as IAppHost);

                    using (App.Trace.Scope("MainData", "LoadData", ""))
                        this.MainData.LoadData(this.GuiData);

                    using (App.Trace.Scope("MainData", "CreateControlToForm", ""))
                        this.MainData.CreateControlToForm(this);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Při spouštění WorkScheduleru došlo k chybě:" + Environment.NewLine + exc.Message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
        protected override void OnShown(EventArgs e)
        {
            using (App.Trace.Scope("SchedulerForm", "OnShown", ""))
                base.OnShown(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            using (App.Trace.Scope("SchedulerForm", "OnClosed", ""))
                base.OnClosed(e);

            Clipboard.SetText(App.Trace.File);
        }
        protected SchedulerDataSource DataSource;
        protected GUI.GuiData GuiData;
        protected WS.MainData MainData;
    }
}
