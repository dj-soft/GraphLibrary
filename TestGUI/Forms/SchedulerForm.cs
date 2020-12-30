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
    [IsMainForm("Testy okna Scheduleru [Manufacturing]", MainFormMode.Default, 10)]
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

                GUI.GuiData guiData = null;
                using (App.Trace.Scope("SchedulerDataSource", "CreateGuiData", ""))
                    guiData = this.DataSource.CreateGuiData();

                using (App.Trace.Scope("SchedulerDataSource", "SerializeGuiData", ""))
                    this.GuiData = SchedulerDataSource.SerialDeserialData(guiData);

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
            string traceFile = App.Trace.File;
            using (App.Trace.Scope("SchedulerForm", "OnClosed", ""))
                base.OnClosed(e);
            ((IDisposable)this.DataSource).Dispose();
            if (!String.IsNullOrEmpty(traceFile))
                App.TryRun(() => WinClipboard.SetText(traceFile));
        }
        protected SchedulerDataSource DataSource;
        protected GUI.GuiData GuiData;
        protected WS.MainData MainData;
    }
}
