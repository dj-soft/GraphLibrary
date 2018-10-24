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
            InitializeComponent();
            InitializeScheduler();
        }
        protected void InitializeScheduler()
        {
            try
            {
                App.AppCompanyName = "Asseco Solutions";
                App.AppProductName = "WorkScheduler";
                App.AppProductTitle = "Dílenské plánování";

                this.DataSource = new SchedulerDataSource();
                this.GuiData = this.DataSource.CreateGuiData();

                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority3_BellowNormal, "PluginForm", "InitializeWorkScheduler", ""))
                {
                    this.MainData = new WS.MainData(this.DataSource as IAppHost);
                    this.MainData.LoadData(this.GuiData);
                    this.MainData.CreateControlToForm(this);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Při spouštění WorkScheduleru došlo k chybě:" + Environment.NewLine + exc.Message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
        protected SchedulerDataSource DataSource;
        protected GUI.GuiData GuiData;
        protected WS.MainData MainData;
    }
}
