using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestDevExpress.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro vstup textu
    /// </summary>
    public partial class InputForm : DevExpress.XtraEditors.XtraForm
    {
        public InputForm()
        {
            InitializeComponent();
            InitForm();
        }
        private void InitForm()
        {
            this.AcceptButton = this._OkBtn;
            this._OkBtn.Click += _OkBtn_Click;
            this.CancelButton = this._CancelBtn;
            this._CancelBtn.Click += _CancelBtn_Click;
            this._TextBox.Enter += _TextBox_Enter;
            this._TextBox.Validating += _TextBox_Validating;
            this.StartPosition = FormStartPosition.CenterParent;
            this.DialogResult = DialogResult.Cancel;
        }
        private void _OkBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private void _CancelBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void _TextBox_Enter(object sender, EventArgs e)
        {
            _ValueOld = Value;
        }
        private string _ValueOld;
        private void _TextBox_Validating(object sender, CancelEventArgs e)
        {
            if (ValueChange != null)
            {
                TEventValueChangeArgs<string> args = new TEventValueChangeArgs<string>(EventSource.User, _ValueOld, Value);
                ValueChange(this, args);
                e.Cancel = args.Cancel;
            }
        }

        public string Title { get { return this.Text; } set { this.Text = value; } }
        public string Label { get { return this._Label.Text; } set { this._Label.Text = value; } }
        public string Value { get { return this._TextBox.Text; } set { _ValueOld = value; this._TextBox.Text = value; } }
        public event EventHandler<TEventValueChangeArgs<string>> ValueChange;

        public static string InputDialogShow(IWin32Window owner = null, string title = null, string label = null, string value = null, Action<InputForm> formInitializer = null, EventHandler<TEventValueChangeArgs<string>> valueValidator = null)
        {
            string result = null;
            using (InputForm inputForm = new InputForm())
            {
                if (title != null) inputForm.Title = title;
                if (label != null) inputForm.Label = label;
                if (value != null) inputForm.Value = value;
                formInitializer?.Invoke(inputForm);
                inputForm.ValueChange += valueValidator;
                var resultDialog = inputForm.ShowDialog(owner);
                if (resultDialog == DialogResult.OK)
                    result = inputForm.Value ?? "";
            }
            return result;
        }
    }
}
