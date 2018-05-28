using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.gInteractiveControl1.BackColor = Color.DarkBlue.Morph(Color.Black, 0.75f);
            // this.gInteractiveControl1.Refresh();
        }
    }
}
