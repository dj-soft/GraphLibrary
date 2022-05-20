using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.XtraPivotGrid.Data;
using DevExpress.XtraTreeList;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro test TreeListu s během refreshe na pozadí
    /// </summary>
    public class TreeBgrForm : DxControlForm
    {
        #region Fyzická tvorba instancí
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TreeBgrForm() : base()
        {
            this.InitSplitContainer();
            this.InitTreeList();
            this.InitEvents();
            this.PrepareTreeNodes();
            this.ApplyTreeNodes(true, false);
        }
        /// <summary>
        /// Inicializace samotného formuláře, probíhá úplně první, ještě není vytvořen žádný control
        /// </summary>
        protected override void InitializeForm()
        {
            this.Text = "Test TreeListu s akcí refreshe nodů volanou z Background threadu";
            base.InitializeForm();
        }
        private void InitSplitContainer()
        {
            _SplitContainer = new DxSplitContainerControl()
            {
                SplitterOrientation = System.Windows.Forms.Orientation.Vertical,
                SplitterPosition = 300,
                FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1,
                IsSplitterFixed = false,
                Dock = System.Windows.Forms.DockStyle.Fill,
            };
            this.ControlPanel.Controls.Add(_SplitContainer);
        }
        private void InitTreeList()
        {
            _TreeList = new DxTreeList()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                ImageMode = TreeListImageMode.ImageStatic,
                NodeImageSize = ResourceImageSizeType.Small,
                RootNodeVisible = true
            };

            _TreeList.AnimationType = TreeListAnimationType.NeverAnimate;
            _TreeList.AllowOptionExpandAnimation = DevExpress.Utils.DefaultBoolean.False;
            _TreeList.FocusRectStyle = DrawFocusRectStyle.RowFullFocus;

            _SplitContainer.Panel1.Controls.Add(_TreeList);
        }
        private DxSplitContainerControl _SplitContainer;
        private DxTreeList _TreeList;
        #endregion
        #region Příprava dat nodů a jejich vložení do TreeLiszu
        private void PrepareTreeNodes()
        {
            PrepareImages();

            var treeNodes = new List<ITreeListNode>();
            AddTreeNodes(treeNodes, 0, null);

            var extensions = treeNodes.Select(t => System.IO.Path.GetExtension(t.ImageName)).Distinct().ToArray();
            if (extensions.Length > 1) throw new InvalidOperationException("Pole TreeNodes obsahuje více přípon než jednu.");
            string expectedExtension = (_TreeList.NodeImageType == ResourceContentType.Vector ? ".svg" : ".png");
            if (!String.Equals(expectedExtension, extensions[0])) throw new InvalidOperationException($"Pole TreeNodes images s jinou příponou '{expectedExtension}' než tou očekávanou '{extensions[0]}'.");

            _TreeNodes = treeNodes;
        }
        private void AddTreeNodes(List<ITreeListNode> treeNodes, int level, string parentNodeId)
        {
            if (level > 3) return;
            int nodesCount = Random.Rand.Next((12 - 3 * level), (35 - 10 * level));           // Počet nodů v této úrovni: Level 0 (=Root) má rozsah { 12 - 35 }; Level 3 (=poslední) má rozsah { 3 - 5 }
            int subNodesRatio = Random.Rand.Next((50 / (level + 5)), (100 / (level + 3)));    // Procento pravděpodobnosti, že Node bude mít svoje SubNodes (Level 0: 10-33; Level 1: 8-25 Level 3: 6-11)
            for (int i = 0; i < nodesCount; i++)
            {
                string nodeId = $"id{((treeNodes.Count + 1000000).ToString())}";
                ITreeListNode treeNode = CreateOneNode(level, nodeId, parentNodeId);
                treeNodes.Add(treeNode);

                if (Random.IsTrue(subNodesRatio))
                    AddTreeNodes(treeNodes, level + 1, treeNode.ItemId);
            }
        }
        private ITreeListNode CreateOneNode(int level, string nodeId, string parentNodeId)
        {
            string text = Random.GetSentence(2, 5);
            string title = text;
            string tool = Random.GetSentences(5, 9, 2, 6);
            string image = Random.GetItem(_Images);
            NodeItemType nodeType = NodeItemType.DefaultText;
            bool expanded = Random.IsTrue(20);

            DataTreeListNode node = new DataTreeListNode(nodeId, parentNodeId, text, nodeType, false, false, expanded, false, image, image, image, title, tool);
            node.Tag = text;

            return node;
        }
        /// <summary>
        /// Do TreeListu vloží nody připravené v poli _TreeNodes
        /// </summary>
        private void ApplyTreeNodes(bool clear = true, bool preservePosition = true)
        {
            try
            {
                _TreeList.RunInLock(new Action<bool, bool>(FillTreeNodes), clear, preservePosition);
            }
            catch (Exception exc)
            {
                DxComponent.ShowMessageException(exc, "Chyba při plnění TreeListu daty:", "Chyba při plnění TreeListu daty");
            }
        }
        private void FillTreeNodes(bool clear, bool preservePosition)
        {
            var nodes = _TreeNodes;
            PreservePropertiesMode preserveProperties = (preservePosition ? (PreservePropertiesMode.SelectedItems | PreservePropertiesMode.FirstVisibleItem | PreservePropertiesMode.FirstVisiblePixel) : PreservePropertiesMode.None);
            _TreeList.AddNodes(nodes, clear, preserveProperties);
        }
        /// <summary>
        /// Připraví pole 200 náhodných názvů obrázků jednoho typu (náhodná volba SVG nebo PNG) do <see cref="_Images"/>.
        /// </summary>
        private void PrepareImages()
        {
            bool isSvg = Random.IsTrue(40);
            var names = DxComponent.GetResourceNames(isSvg ? ".svg" : ".png", true, false);
            _Images = Random.GetItems(200, names);
            _TreeList.NodeImageType = (isSvg ? ResourceContentType.Vector : ResourceContentType.Bitmap);
        }
        private List<ITreeListNode> _TreeNodes;
        private string[] _Images;
        #endregion
        #region Události, dynamika
        /// <summary>
        /// Inicializace eventů
        /// </summary>
        private void InitEvents()
        {
            _TreeList.NodeExpanded += _TreeList_NodeExpanded;
            _TreeList.NodeCollapsed += _TreeList_NodeCollapsed;
        }
        /// <summary>
        /// Uživatel provedl Expand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TreeList_NodeExpanded(object sender, DxTreeListNodeArgs args)
        {
            _ChangeNodeAndRunRefresh(args.Node, " [+]");
        }
        /// <summary>
        /// Uživatel provedl Collapse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TreeList_NodeCollapsed(object sender, DxTreeListNodeArgs args)
        {
            _ChangeNodeAndRunRefresh(args.Node, " [-]");
        }
        /// <summary>
        /// Uprav node (změna Image a přidání suffixu), ale v threadu na pozadí a s malým časovým odstupem
        /// </summary>
        /// <param name="iNode"></param>
        /// <param name="suffix"></param>
        private void _ChangeNodeAndRunRefresh(ITreeListNode iNode, string suffix)
        {
            ThreadManager.AddAction(() => _ChangeNodeAndRunRefreshBgr(iNode, suffix));
        }
        /// <summary>
        /// Akce provede změnu textu a Image v zadaném nodu, ale provede to v threadu na pozadí a s malým zpožděním...
        /// </summary>
        /// <param name="iNode"></param>
        /// <param name="suffix"></param>
        private void _ChangeNodeAndRunRefreshBgr(ITreeListNode iNode, string suffix)
        {
            if (iNode is DataTreeListNode node)
            {
                string image = Random.GetItem(_Images);
                node.Text = (node.Tag as string) + suffix;
                node.ImageName = image;
                node.ImageDynamicDefault = image;
                node.ImageDynamicSelected = image;

                int delay = Random.Rand.Next(10, 150);
                System.Threading.Thread.Sleep(delay);
                
                ApplyTreeNodes(true, true);
            }
        }
        #endregion
    }
}
