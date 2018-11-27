using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    public partial class TestSnapForm : Form
    {
        public TestSnapForm()
        {
            InitializeComponent();
            this.InitTree();
        }
        protected void InitTree()
        {
            this.ConfigTree.Nodes.Clear();

            this.NodeFontMain = FontInfo.CaptionBoldBig.CreateNewFont();
            this.NodeFontStandard = FontInfo.CaptionBold.CreateNewFont();

            this.ConfigTree.Font = this.NodeFontMain;
            this.ConfigTree.BorderStyle = BorderStyle.None;
            this.ConfigTree.CheckBoxes = false;
            this.ConfigTree.DrawMode = TreeViewDrawMode.Normal;
            this.ConfigTree.FullRowSelect = true;
            this.ConfigTree.HideSelection = false;
            this.ConfigTree.HotTracking = false;
            this.ConfigTree.Indent = 16;
            this.ConfigTree.LabelEdit = false;
            this.ConfigTree.Scrollable = true;
            this.ConfigTree.ShowLines = true;
            this.ConfigTree.ShowNodeToolTips = true;
            this.ConfigTree.ShowPlusMinus = true;
            this.ConfigTree.ShowRootLines = true;

            TreeNode node = new TreeNode("Přemístění prvků myší");
            node.NodeFont = this.NodeFontMain;
            this.ConfigTree.Nodes.Add(node);

            TreeNode nodeMoveNone = new TreeNode("Pohyb bez klávesnice");
            nodeMoveNone.NodeFont = this.NodeFontStandard;
            node.Nodes.Add(nodeMoveNone);

            TreeNode nodeMoveShift = new TreeNode("Pohyb s klávesou SHIFT");
            nodeMoveShift.NodeFont = this.NodeFontStandard;
            node.Nodes.Add(nodeMoveShift);
            node.Expand();


        }
        protected Font NodeFontMain;
        protected Font NodeFontStandard;
    }
}
