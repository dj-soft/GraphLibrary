using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.Forms
{
    public partial class EditorForm : Form
    {
        public EditorForm()
        {
            InitializeComponent();
            InitEditor();
        }
        private void InitEditor()
        {
            this._Editor.ParserSetting = TextParser.ParserDefaultSetting.CSharp;
        }
    }
}
