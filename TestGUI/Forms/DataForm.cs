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
    public partial class DataForm : Form
    {
        public DataForm()
        {
            InitializeApplication();
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;

            using (App.Trace.Scope("DataForm", "InitializeComponent", ""))
                InitializeComponent();

            using (App.Trace.Scope("DataForm", "InitializeDataForm", ""))
                InitializeDataForm();
        }

        protected void InitializeApplication()
        {
            App.AppCompanyName = "Asseco Solutions";
            App.AppProductName = "WorkScheduler";
            App.AppProductTitle = "Dílenské plánování";
            App.Trace.Info("DataForm", "InitializeApplication", "", "First row");
        }

        protected void InitializeDataForm()
        {
        }

        protected string GetFormXml()
        {
            string xml = @"";


            return xml;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
