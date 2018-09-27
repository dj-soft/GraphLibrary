using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Parsing = Asol.Tools.WorkScheduler.Data.Parsing;

namespace Asol.Tools.WorkScheduler.Forms
{
    /// <summary>
    /// EditorForm : formulář pro testy editoru
    /// </summary>
    public partial class EditorForm : Form
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public EditorForm()
        {
            InitializeComponent();
            InitEditor();
        }
        private void InitEditor()
        {
            this._Editor.ParserSetting = Parsing.DefaultSettings.CSharp;
        }
    }
}
