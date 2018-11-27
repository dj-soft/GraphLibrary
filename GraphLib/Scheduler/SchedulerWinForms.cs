using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region SchedulerConfigForm : Okno s konfigurací
    /// <summary>
    /// SchedulerConfigForm : Okno s konfigurací
    /// </summary>
    internal class SchedulerConfigForm : WinFormButtons
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace formu, virtuální metoda volaná z konstruktoru.
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// <para/>
        /// Třída <see cref="WinFormButtons"/> inicializuje prvky <see cref="WinFormButtons.ButtonsPanel"/> a <see cref="WinFormButtons.DataPanel"/>, a pole buttonů.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            this.InitComponents();
            this.InitData();
        }
        /// <summary>
        /// Vygeneruje komponenty, včetně konkrétního obsahu stromu konfigurací
        /// </summary>
        protected void InitComponents()
        {
            this.ConfigTree = new System.Windows.Forms.TreeView()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                CheckBoxes = false,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                FullRowSelect = true,
                HideSelection = false,
                HotTracking = false,
                Indent = 16,
                LabelEdit = false,
                Scrollable = true,
                ShowLines = true,
                ShowNodeToolTips = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
            };
            this.ConfigTree.DrawNode += ConfigTree_DrawNode;
            this.ConfigTree.AfterSelect += ConfigTree_AfterSelect;
            this.ConfigContainer = new SplitContainer() { FixedPanel = FixedPanel.Panel1, SplitterDistance = 270, Dock = DockStyle.Fill };
            this.NodeFontInfoMain = FontInfo.CaptionBoldBig;
            this.NodeFontMain = this.NodeFontInfoMain.CreateNewFont();
            this.NodeFontInfoStandard = FontInfo.CaptionBold;
            this.NodeFontStandard = this.NodeFontInfoStandard.CreateNewFont();

            ((System.ComponentModel.ISupportInitialize)(this.ConfigContainer)).BeginInit();
            this.ConfigContainer.Panel1.SuspendLayout();
            this.ConfigContainer.Panel2.SuspendLayout();
            this.ConfigContainer.SuspendLayout();
            this.SuspendLayout();

            this.ConfigContainer.Panel1.Controls.Add(this.ConfigTree);

            this.DataPanel.Controls.Add(this.ConfigContainer);

            this.Text = "Nastavení";
            this.Buttons = Noris.LCS.Base.WorkScheduler.GuiDialogButtons.OkCancel;
            this.ButtonsAlignment = ContentAlignment.MiddleRight;
            this.CloseOnClick = false;

            this.ConfigContainer.Panel1.ResumeLayout(false);
            this.ConfigContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ConfigContainer)).EndInit();
            this.ConfigContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        protected override void OnButtonClick(GPropertyEventArgs<GuiDialogButtons> args)
        {
            base.OnButtonClick(args);
            switch (args.Value)
            {
                case GuiDialogButtons.Ok:
                case GuiDialogButtons.Save:
                    this.SaveConfig();
                    this.Close();
                    break;
                case GuiDialogButtons.Cancel:
                    this.Close();
                    break;
            }
        }
        /// <summary>
        /// Vykreslí label jednoho node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigTree_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            TreeNode node = e.Node;

            Rectangle nodeBounds = e.Bounds;
            Size treeSize = this.ConfigTree.ClientSize;

            Rectangle itemBounds = new Rectangle(nodeBounds.X, nodeBounds.Y, treeSize.Width - nodeBounds.X, nodeBounds.Height);
            Brush backBrush = Skin.Brush(this.ConfigTree.BackColor);
            e.Graphics.FillRectangle(backBrush, itemBounds);

            Color nodeColor = (e.Node.IsSelected ? Color.LightBlue : this.ConfigTree.BackColor);
            Rectangle rowBounds = new Rectangle(nodeBounds.X, nodeBounds.Y + 1, treeSize.Width - nodeBounds.X, nodeBounds.Height - 2);
            GPainter.DrawAreaBase(e.Graphics, rowBounds, nodeColor, Orientation.Horizontal, GInteractiveState.Enabled);

            Rectangle textBounds = new Rectangle(nodeBounds.X + 3, nodeBounds.Y + 1, treeSize.Width - nodeBounds.X - 6, nodeBounds.Height - 2);
            FontInfo fontInfo = this.NodeFontInfoStandard;
            GPainter.DrawString(e.Graphics, textBounds, e.Node.Text, Skin.Brush(this.ConfigTree.ForeColor), fontInfo, ContentAlignment.MiddleLeft);

            e.DrawDefault = false;
        }

        /// <summary>
        /// Při prvním zobrazení formu
        /// </summary>
        protected override void OnFirstShown()
        {
            this.ConfigContainer.SplitterDistance = 270;
            this.ConfigTree.FullRowSelect = true;
        }
        /// <summary>
        /// Hlavní container
        /// </summary>
        protected SplitContainer ConfigContainer;
        /// <summary>
        /// Strom obsahující položky konfigurace
        /// </summary>
        protected TreeView ConfigTree;
        /// <summary>
        /// Font pro hlavní úroveň nodů
        /// </summary>
        protected Font NodeFontMain;
        /// <summary>
        /// Font pro nižší úrovně nodů
        /// </summary>
        protected Font NodeFontStandard;
        /// <summary>
        /// FontInfo pro hlavní úroveň nodů
        /// </summary>
        protected FontInfo NodeFontInfoMain;
        /// <summary>
        /// FontInfo pro nižší úrovně nodů
        /// </summary>
        protected FontInfo NodeFontInfoStandard;
        #endregion
        #region Vazby na instanci SchedulerConfig
        /// <summary>
        /// Instance konfigurace, z níž se zobrazují a editují data
        /// </summary>
        protected SchedulerConfig Config
        {
            get { return this._Config; }
            set { this._Config = value; this._ReadData(); }
        }
        private SchedulerConfig _Config;
        /// <summary>
        /// Inicializace interních dat
        /// </summary>
        private void InitData()
        {
            this._TreeNodeDict = new Dictionary<string, TreeNode>();
        }
        /// <summary>
        /// Do jednotlivých controlů načte data z Configu
        /// </summary>
        private void _ReadData()
        {
            this.ConfigTree.Nodes.Clear();
            this._TreeNodeDict.Clear();
            this.ConfigContainer.Panel2.Controls.Clear();
            this._EditorItems = null;
            this._CurrentEditorItem = null;

            SchedulerConfig config = this._Config;
            if (config == null) return;

            this._EditorItems = config.CreateEditorItems();
            foreach (SchedulerEditorItem editorItem in this._EditorItems)
                this._CreateNodeData(editorItem);

            this.ConfigTree.ItemHeight = 140 * this.NodeFontMain.Height / 100;
        }
        /// <summary>
        /// Pro daný objekt editoru vytvoří node a zajistí další akce
        /// </summary>
        /// <param name="editorItem"></param>
        private void _CreateNodeData(SchedulerEditorItem editorItem)
        {
            TreeNode node = _SearchTreeNode(editorItem.NodeText);
            node.Tag = editorItem;

            editorItem.VisualControl.Read();

            Panel panel = editorItem.VisualControl.Panel;
            panel.Visible = false;
            panel.Dock = DockStyle.Fill;
            this.ConfigContainer.Panel2.Controls.Add(panel);
        }
        /// <summary>
        /// Najde nebo vytvoří node s daným názvem
        /// </summary>
        /// <param name="nodeText"></param>
        /// <returns></returns>
        private TreeNode _SearchTreeNode(string nodeText)
        {
            if (String.IsNullOrEmpty(nodeText)) return null;
            TreeNode node;
            if (!this._TreeNodeDict.TryGetValue(nodeText, out node))
            {
                string text = null;
                TreeNodeCollection parentCollection = null;
                Font font = null;
                string parentText = nodeText.SplitOn(SchedulerConfig.EditTitle_Separator, true);
                if (parentText.Length == 0)
                {   // Zadaný vstupní text nodeText neobsahuje separátor = jde o Root prvek:
                    parentCollection = this.ConfigTree.Nodes;
                    text = nodeText;
                    font = this.NodeFontMain;
                }
                else
                {   // Máme text před separátorem = jde o parent našeho hledaného node, necháme si ho rekurzivně najít:
                    TreeNode parentNode = this._SearchTreeNode(parentText);
                    parentCollection = parentNode.Nodes;
                    text = nodeText.SplitOn(SchedulerConfig.EditTitle_Separator, true, true);
                    font = this.NodeFontStandard;
                }
                node = new TreeNode(text);
                node.NodeFont = font;
                parentCollection.Add(node);
                this._TreeNodeDict.Add(nodeText, node);
            }
            return node;
        }
        /// <summary>
        /// Zajistí aktivaci editoru pro daný node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            SchedulerEditorItem editorItem = node.Tag as SchedulerEditorItem;

            if (editorItem == null && node.Nodes.Count > 0)
                editorItem = node.Nodes[0].Tag as SchedulerEditorItem;

            if (this._CurrentEditorItem != null && (editorItem == null || !Object.ReferenceEquals(this._CurrentEditorItem, editorItem)))
            {
                this._CurrentEditorItem.VisualControl.Panel.Visible = false;
                this._CurrentEditorItem = null;
            }

            if (editorItem != null)
            {
                this._CurrentEditorItem = editorItem;
                if (!this._CurrentEditorItem.VisualControl.Panel.Visible)
                    this._CurrentEditorItem.VisualControl.Panel.Visible = true;
            }
        }
        /// <summary>
        /// Aktuální prvek editace
        /// </summary>
        private SchedulerEditorItem _CurrentEditorItem;
        /// <summary>
        /// Položky editoru
        /// </summary>
        private SchedulerEditorItem[] _EditorItems;
        /// <summary>
        /// Dictionary obsahující TreeNode podle jejich názvu
        /// </summary>
        private Dictionary<string, TreeNode> _TreeNodeDict;
        /// <summary>
        /// Uloží data
        /// </summary>
        protected void SaveConfig()
        {
            if (this._EditorItems == null) return;
            using (this.Config.CreateEditingScope())
            {
                foreach (SchedulerEditorItem editorItem in this._EditorItems)
                {
                    editorItem.VisualControl.Save();
                }
            }
        }
        #endregion
        #region Statické spouštění okna konfigurace
        /// <summary>
        /// Otevře okno konfigurace
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="config"></param>
        public static void ShowDialog(Form owner, SchedulerConfig config)
        {
            using (SchedulerConfigForm form = new SchedulerConfigForm())
            {
                form.SetBounds(owner, 0.70f, 0.70f);
                form.Config = config;
                form.ShowDialog(owner);
            }
        }
        #endregion
    }
    #endregion
}
