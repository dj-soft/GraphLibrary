using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    public partial class SelectMainForm : Form
    {
        public SelectMainForm()
        {
            InitializeComponent();
            this.FillListBox();
        }
        private void FillListBox()
        {
            this._ListTypes.Items.Clear();
            this._ListTypes.Columns.Clear();
            this._ListTypes.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            this._ListTypes.View = View.Details;
            this._ListTypes.FullRowSelect = true;
            this._ListTypes.MultiSelect = false;
            this._ListTypes.Columns.Add(new ColumnHeader() { Name = "UserName", Text = "Formulář", Width = 280 });
            this._ListTypes.Columns.Add(new ColumnHeader() { Name = "TypeName", Text = "Type", Width = 160 });

            var mainFormTypes = Program.GetAvailableForms();

            int index = 0;
            foreach (var mainFormType in mainFormTypes)
            {
                var imfa = Program.GetMainFormAttribute(mainFormType);
                string userName = imfa.FormName;
                string typeName = mainFormType.Name;
                ListViewItem item = new ListViewItem(new string[] { userName, typeName }) { Tag = mainFormType };
                this._ListTypes.Items.Add(item);
                if (index == 0) item.Selected = true;
                index++;
            }

            this._ListTypes.MouseDoubleClick += _ListTypes_MouseDoubleClick;
            this._ListTypes.KeyDown += _ListTypes_KeyDown;

        }
        private void _ListTypes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this._RunSelectedForm();
        }
        private void _ListTypes_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this._RunSelectedForm();
        }
        private void _ButtonRun_Click(object sender, EventArgs e)
        {
            this._RunSelectedForm();
        }
        private void _RunSelectedForm()
        {
            if (this._ListTypes.SelectedItems.Count == 0)
            {
                Application.App.ShowError("Vyberte nejprve nějaký formulář ke spuštění.");
                return;
            }
            var item = this._ListTypes.SelectedItems[0];
            Type selectedType = item.Tag as Type;
            this.Hide();
            Application.App.RunForm(selectedType);
            this.Close();
        }
    }
}
