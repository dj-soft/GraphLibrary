using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using System.Drawing;
using TestDevExpress.Components;
using Noris.Clients.Win.Components;
using DevExpress.XtraRichEdit.Layout;

namespace TestDevExpress.Forms
{
    [RunFormInfo(groupText: "Testovací okna", buttonText: "TreeList", buttonOrder: 60, buttonImage: "svgimages/dashboards/inserttreeview.svg", buttonToolTip: "Otevře okno TreeList s parametry", tabViewToolTip: "Okno zobrazující nový TreeList")]
    internal class DxTreeListForm : DxRibbonForm
    {
        #region Inicializace
        public DxTreeListForm()
        {
            // Páry barev mají: Item1 = Písmo, Item2 = Pozadí:
            var pairs = new Tuple<string, string>[] { Constants.ColorPairGreen, Constants.ColorPairRed, Constants.ColorPairYellow, Constants.ColorPairOrange, Constants.ColorPairBlue, Constants.ColorPairTurquoise, Constants.ColorPairPurple, Constants.ColorPairBrown, Constants.ColorPairBlack };
            var pair = pairs[Counter % (pairs.Length)];
            string znak = ((char)(65 + (Counter % 25))).ToString();

            Counter++;

            this.ImageName = "svgimages/dashboards/inserttreeview.svg";
            // Starý Nephrite:  this.ImageNameAdd = $"@text|{znak}|{pair.Item1}|tahoma|B|4|{pair.Item1}|{pair.Item2}";
            // Nový Nephrite:
            var iconData = new SvgImageTextIcon()
            {
                Text = znak,
                TextBold = true,
                TextFont = SvgImageTextIcon.TextFontType.Tahoma,
                TextColorName = pair.Item1,
                BackColorName = pair.Item2,
                BorderColorBW = false,
                Padding = 3,
                BorderWidth = 1,
                Rounding = 8
            };
            this.ImageNameAdd = iconData.SvgImageName;

            __CurrentId = ++__InstanceCounter;
            _RefreshTitle();
        }
        private void _RefreshTitle()
        {
            this.Text = $"TreeList   [{__CurrentId}]";
        }
        private static int Counter = 12;
        private int __CurrentId;
        private static int __InstanceCounter;
        #endregion
        #region Ribbon - obsah a rozcestník
        protected override void DxRibbonPrepare()
        {
            var ribbonContent = new DataRibbonContent();
            var homePage = DxRibbonControl.CreateStandardHomePage();
            var treePrepareGroup = new DataRibbonGroup() { GroupText = "Vytvoření TreeListu" };
            treePrepareGroup.Items.Add(new DataRibbonItem() { ItemId = "TreePrepareSet50", ImageName = "svgimages/icon%20builder/actions_addcircled.svg", Text = "Vytvoř TreeList 50", RibbonStyle = RibbonItemStyles.Large });
            treePrepareGroup.Items.Add(new DataRibbonItem() { ItemId = "TreePrepareSet500", ImageName = "svgimages/icon%20builder/actions_addcircled.svg", Text = "Vytvoř TreeList 500", RibbonStyle = RibbonItemStyles.Large });
            treePrepareGroup.Items.Add(new DataRibbonItem() { ItemId = "TreePrepareSet5000", ImageName = "svgimages/icon%20builder/actions_addcircled.svg", Text = "Vytvoř TreeList 5000", RibbonStyle = RibbonItemStyles.Large });
            homePage.Groups.Add(treePrepareGroup);
            ribbonContent.Pages.Add(homePage);

            this.DxRibbon.RibbonContent = ribbonContent;
            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            var itemId = e.Item.ItemId;
            switch (itemId)
            {
                case "TreePrepareSet50": _PrepareTreeList(20, 1); break;
                case "TreePrepareSet500": _PrepareTreeList(40, 2); break;
                case "TreePrepareSet5000": _PrepareTreeList(80, 3); break;
            }
        }
        #endregion
        #region Hlavní controly - tvorba a převolání Initů
        protected override void DxMainContentPrepare()
        {
            base.DxMainContentPrepare();

            __MainSplitContainer = new DxSplitContainerControl() { Dock = DockStyle.Fill, SplitterPosition = 450, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterOrientation = Orientation.Horizontal, ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True, Name = "MainSplitContainer" };
            this.DxMainPanel.Controls.Add(__MainSplitContainer);

            __DxTreeList = new DxTreeList() { Dock = DockStyle.Fill, Name = "DxTreeList" };
            _TreeListInit();
            __MainSplitContainer.Panel1.Controls.Add(__DxTreeList);

            __ParamSplitContainer = new DxSplitContainerControl() { Dock = DockStyle.Fill, SplitterPosition = 300, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterOrientation = Orientation.Vertical, ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True, Name = "ParamSplitContainer" };
            __MainSplitContainer.Panel2.Controls.Add(__ParamSplitContainer);

            __ParamsPanel = new DxPanelControl() { Dock = DockStyle.Fill, Name = "ParamsPanel" };
            _ParamsInit();
            __ParamSplitContainer.Panel1.Controls.Add(__ParamsPanel);

            __LogPanel = new DxPanelControl() { Dock = DockStyle.Fill, Name = "LogPanel" };
            _LogInit();
            __ParamSplitContainer.Panel2.Controls.Add(__LogPanel);

            _SettingLoad();
        }
        private static Keys[] _CreateHotKeys()
        {
            Keys[] keys = new Keys[]
            {
                Keys.Delete,
                Keys.Control | Keys.N,
                Keys.Control | Keys.Delete,
                Keys.Enter,
                Keys.Control | Keys.Enter,
                Keys.Control | Keys.Shift | Keys.Enter,
                Keys.Control | Keys.Home,
                Keys.Control | Keys.End,
                Keys.F1,
                Keys.F2,
                Keys.Control | Keys.Space
            };
            return keys;
        }
        private Noris.Clients.Win.Components.AsolDX.DxSplitContainerControl __MainSplitContainer;
        private Noris.Clients.Win.Components.AsolDX.DxTreeList __DxTreeList;
        private Noris.Clients.Win.Components.AsolDX.DxSplitContainerControl __ParamSplitContainer;
        private Noris.Clients.Win.Components.AsolDX.DxPanelControl __ParamsPanel;
        private Noris.Clients.Win.Components.AsolDX.DxPanelControl __LogPanel;
        #endregion
        #region TreeList setting a events
        private void _TreeListInit()
        {
            __DxTreeList.CheckBoxMode = TreeListCheckBoxMode.SpecifyByNode;
            __DxTreeList.ImageMode = TreeListImageMode.ImageStatic;
            __DxTreeList.LazyLoadNodeText = "Copak to tu asi bude?";
            __DxTreeList.LazyLoadNodeImageName = "hourglass_16";
            __DxTreeList.LazyLoadFocusNode = TreeListLazyLoadFocusNodeType.ParentNode;
            __DxTreeList.FilterBoxVisible = true;
            __DxTreeList.EditorShowMode = DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;
            __DxTreeList.IncrementalSearchMode = TreeListIncrementalSearchMode.InAllNodes;
            __DxTreeList.FilterBoxOperators = DxFilterBox.CreateDefaultOperatorItems(FilterBoxOperatorItems.DefaultText);
            __DxTreeList.FilterBoxChangedSources = DxFilterBoxChangeEventSource.Default;
            __DxTreeList.MultiSelectEnabled = true;
            __DxTreeList.MainClickMode = NodeMainClickMode.AcceptNodeSetting;

            __DxTreeList.NodeImageSize = ResourceImageSizeType.Large;        // Zkus různé...
            __DxTreeList.NodeImageSize = ResourceImageSizeType.Medium;
            __DxTreeList.NodeImageSize = ResourceImageSizeType.Small;

            __DxTreeList.NodeAllowHtmlText = true;

            __DxTreeList.HotKeys = _CreateHotKeys();

            __DxTreeList.FilterBoxChanged += _TreeList_FilterBoxChanged;
            __DxTreeList.FilterBoxKeyEnter += _TreeList_FilterBoxKeyEnter;
            __DxTreeList.NodeKeyDown += _TreeList_NodeKeyDown;
            __DxTreeList.NodeFocusedChanged += _TreeList_AnyAction;
            __DxTreeList.SelectedNodesChanged += _TreeList_SelectedNodesChanged;
            __DxTreeList.ShowContextMenu += _TreeList_ShowContextMenu;
            __DxTreeList.NodeIconClick += _TreeList_IconClick;
            __DxTreeList.NodeDoubleClick += _TreeList_DoubleClick;
            __DxTreeList.NodeExpanded += _TreeList_AnyAction;
            __DxTreeList.NodeCollapsed += _TreeList_AnyAction;
            __DxTreeList.ActivatedEditor += _TreeList_AnyAction;
            __DxTreeList.EditorDoubleClick += _TreeList_DoubleClick;
            __DxTreeList.NodeEdited += _TreeList_NodeEdited;
            __DxTreeList.NodeCheckedChange += _TreeList_AnyAction;
            __DxTreeList.NodesDelete += _TreeList_NodesDelete;
            __DxTreeList.LazyLoadChilds += _TreeList_LazyLoadChilds;
            __DxTreeList.ToolTipChanged += _TreeList_ToolTipChanged;
            __DxTreeList.MouseLeave += _TreeList_MouseLeave;
        }
        private void _TreeList_AnyAction(object sender, DxTreeListNodesArgs args)
        {
            _AddToLog(args.Action.ToString(), args);
        }
        private void _TreeList_AnyAction(object sender, DxTreeListNodeArgs args)
        {
            _AddToLog(args.Action.ToString(), args, (args.Action == TreeListActionType.NodeEdited || args.Action == TreeListActionType.EditorDoubleClick || args.Action == TreeListActionType.NodeCheckedChange));
        }
        private void _TreeList_FilterBoxChanged(object sender, DxFilterBoxChangeArgs args)
        {
            var filter = this.__DxTreeList.FilterBoxValue;
            _AddToLog($"RowFilter: Change: {args.EventSource}; Operator: {args.FilterValue.FilterOperator?.ItemId}, Text: \"{args.FilterValue.FilterText}\"");
        }
        private void _TreeList_FilterBoxKeyEnter(object sender, EventArgs e)
        {
            _AddToLog($"RowFilter: 'Enter' pressed");
        }
        private void _TreeList_NodeKeyDown(object sender, DxTreeListNodeKeyArgs args)
        {
            _AddToLog($"KeyUp: Node: {args.Node?.Text}; KeyCode: '{args.KeyArgs.KeyCode}'; KeyData: '{args.KeyArgs.KeyData}'; Modifiers: {args.KeyArgs.Modifiers}");
        }
        private void _TreeList_SelectedNodesChanged(object sender, DxTreeListNodeArgs args)
        {
            int count = 0;
            string selectedNodes = "";
            __DxTreeList.SelectedNodes.ForEachExec(n => { count++; selectedNodes += "; '" + n.ToString() + "'"; });
            if (selectedNodes.Length > 0) selectedNodes = selectedNodes.Substring(2);
            _AddToLog($"SelectedNodesChanged: Selected {count} Nodes: {selectedNodes}");
        }
        private void _TreeList_ShowContextMenu(object sender, DxTreeListNodeContextMenuArgs args)
        {
            _AddToLog($"ShowContextMenu: Node: {args.Node} Part: {args.HitInfo.PartType}");
            if (args.Node != null)
                _ShowDXPopupMenu(Control.MousePosition);
        }
        private void _TreeList_IconClick(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
        }
        private void _TreeList_DoubleClick(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeDoubleClickBgr(args));
        }
        private void _TreeList_NodeEdited(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeEditedBgr(args));
        }
        private void _TreeList_NodesDelete(object sender, DxTreeListNodesArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeDeleteBgr(args));
        }
        private void _TreeList_LazyLoadChilds(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _LoadChildNodesFromServerBgr(args));
        }
        private void _TreeList_ToolTipChanged(object sender, DxToolTipArgs args)
        {
            if (__LogToolTipChangeValue)
            {
                string line = "ToolTip: " + args.EventName;
                bool skipGUI = (line.Contains("IsFASTMotion"));             // ToolTip obsahující IsFASTMotion nebudu dávat do GUI Textu - to jsou rychlé eventy:
                _AddToLog(line, skipGUI);
            }
        }
        private void _TreeList_MouseLeave(object sender, EventArgs e)
        {
            if (_TreeListPending)
                _AddToLog("TreeList.MouseLeave");
        }
        #endregion
        #region Kontextové menu
        private void _ShowDXPopupMenu(Point mousePosition)
        {

        }
        #endregion
        #region TreeList a BackgroundRun
        private void _TreeNodeDoubleClickBgr(DxTreeListNodeArgs args)
        {
            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            if (args.Node.NodeType == NodeItemType.OnDoubleClickLoadNext)
            {
                __DxTreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    __DxTreeList.RemoveNode(node.ItemId);            // Odeberu OnDoubleClickLoadNext node, to kvůli pořadí: nový OnDoubleClickLoadNext přidám (možná) nakonec

                    var newNodes = _CreateNodes(node, false, true);
                    __DxTreeList.AddNodes(newNodes);

                    // Aktivuji první přidaný node:
                    if (newNodes.Length > 0)
                        __DxTreeList.SetFocusToNode(newNodes[0]);
                }
               ), args.Node);
            }
        }
        private void _TreeNodeEditedBgr(DxTreeListNodeArgs args)
        {
            var nodeInfo = args.Node;
            string nodeId = nodeInfo.ItemId;
            string parentNodeId = nodeInfo.ParentNodeFullId;
            string oldValue = nodeInfo.Text;
            string newValue = (args.EditedValue is string text ? text : "");
            _AddToLog($"Změna textu pro node '{nodeId}': '{oldValue}' => '{newValue}'");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            var newNodePosition = __NewNodePosition;
            bool isBlankNode = (oldValue == "" && (newNodePosition == NewNodePositionType.First || newNodePosition == NewNodePositionType.Last));
            if (String.IsNullOrEmpty(newValue))
            {   // Delete node:
                if (nodeInfo.CanDelete)
                    __DxTreeList.RemoveNode(nodeId);
            }
            else if (nodeInfo.NodeType == NodeItemType.BlankAtFirstPosition) // isBlankNode && newPosition == NewNodePositionType.First)
            {   // Insert new node, a NewPosition je First = je první (jako Green):
                __DxTreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    node.Text = "";                                 // Z prvního node odeberu jeho text, aby zase vypadal jako nový node
                    node.Refresh();

                    // Přidám nový node pro konkrétní text = jakoby nově zadaný záznam:
                    DataTreeListNode newNode = _CreateNode(node.ParentNodeFullId, NodeItemType.DefaultText);
                    if (newNode != null)
                    {
                        newNode.Text = newValue;
                        __DxTreeList.AddNode(newNode, 1);
                    }
                }
                ), nodeInfo);
            }
            else if (isBlankNode && newNodePosition == NewNodePositionType.Last)
            {   // Insert new node, a NewPosition je Last = na konci:
                __DxTreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    __DxTreeList.RemoveNode(node.ItemId);              // Odeberu blank node, to kvůli pořadí: nový blank přidám nakonec

                    // Přidám nový node pro konkrétní text = jakoby záznam:
                    DataTreeListNode newNode = _CreateNode(node.ParentNodeFullId, NodeItemType.DefaultText);
                    if (newNode != null)
                    {
                        newNode.Text = newValue;
                        __DxTreeList.AddNode(newNode);
                    }

                    // Přidám Blank node, ten bude opět na konci Childs:
                    DataTreeListNode blankNode = _CreateNode(node.ParentNodeFullId, NodeItemType.BlankAtLastPosition);
                    if (blankNode != null)
                    {
                        __DxTreeList.AddNode(blankNode);
                    }

                    // Aktivuji editovaný node:
                    if (newNode != null)
                    {
                        __DxTreeList.SetFocusToNode(newNode);
                    }
                }
                ), nodeInfo);
            }
            else
            {   // Edited node:
                if (args.Node is DataTreeListNode node)
                {
                    node.Text = newValue + " [OK]";
                    node.Refresh();
                }
            }
        }
        private void _TreeNodeDeleteBgr(DxTreeListNodesArgs args)
        {
            var removeNodeKeys = args.Nodes.Select(n => n.ItemId).ToArray();

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            __DxTreeList.RemoveNodes(removeNodeKeys);
        }
        private void _LoadChildNodesFromServerBgr(DxTreeListNodeArgs args)
        {
            var parentNode = args.Node;
            var parentNodeId = parentNode.ItemId;
            _AddToLog($"Načítám data pro node '{parentNodeId}'...");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            // Upravíme hodnoty v otevřeném nodu:
            string text = args.Node.Text;
            if (text.EndsWith(" ..."))
            {
                if (args.Node is DataTreeListNode node)
                {
                    node.Text = text.Substring(0, text.Length - 4);
                    node.MainClickAction = NodeMainClickActionType.ExpandCollapse;
                    node.Refresh();
                }
            }

            // Vytvoříme ChildNodes a zobrazíme je:
            bool empty = (Randomizer.Rand.Next(10) > 7);
            var nodes = _CreateNodes(parentNode);                          // A pak vyrobíme Child nody
            _AddToLog($"Načtena data: {nodes.Length} prvků.");
            __DxTreeList.AddLazyLoadNodes(parentNodeId, nodes);            //  a pošleme je do TreeView.
        }
        #endregion
        #region Plnění dat do TreeListu
        private void _PrepareTreeList(int sampleCountBase, int sampleLevelsCount)
        {
            _LogClear();

            __TotalNodesCount = 0;
            __SampleCountBase = sampleCountBase;
            __SampleLevelsCount = sampleLevelsCount;
            var nodes = _CreateNodes(null);
            __DxTreeList.AddNodes(nodes, true, PreservePropertiesMode.None);

            string text = this.GetControlStructure();
        }
        private DataTreeListNode[] _CreateNodes(ITreeListNode parentNode, bool canAddEditable = true, bool canAddShowNext = true)
        {
            List<DataTreeListNode> nodes = new List<DataTreeListNode>();
            _AddNodesToList(parentNode, canAddEditable, canAddShowNext, nodes);
            return nodes.ToArray();
        }
        /// <summary>
        /// Vrací počet prvků reálně přidaných
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="canAddEditable"></param>
        /// <param name="canAddShowNext"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private int _AddNodesToList(ITreeListNode parentNode, bool canAddEditable, bool canAddShowNext, List<DataTreeListNode> nodes)
        {
            int result = 0;
            int currentLevel = getNodeLevel(parentNode);
            int count = getCount(currentLevel);
            for (int i = 0; i < count; i++)
            {
                var child = _CreateNode(parentNode?.ItemId, NodeItemType.DefaultText);
                if (child is null) break;
                child.ParentItem = parentNode;
                nodes.Add(child);
                result++;
                if (canAddChilds(currentLevel))
                {
                    var childCount = _AddNodesToList(child, canAddEditable, canAddShowNext, nodes);
                    if (childCount > 0 && Randomizer.IsTrue(25))
                        child.Expanded = true;
                }
            }
            return result;

            // Určí level pro daný node. Pokud je null, pak výstup je 0.
            int getNodeLevel(IMenuItem node)
            {
                int level = 0;
                while (node != null)
                {
                    level++;
                    node = node.ParentItem;
                }
                return level;
            }
            // Vrátí počet prvků do daného levelu
            int getCount(int level)
            {
                if (level > __SampleLevelsCount) return 0;

                int baseCount = __SampleCountBase;
                if (level > 0)
                    baseCount = baseCount / (level + 1);

                return Randomizer.GetValueInRange(baseCount * 60 / 100, baseCount * 175 / 100);
            }
            bool canAddChilds(int level)
            {
                if (level >= __SampleLevelsCount) return false;
                int probability = (level == 0 ? 50 : (level == 1 ? 25 : (level == 2 ? 10 : 0)));
                return Randomizer.IsTrue(probability);
            }
        }
        /// <summary>
        /// Vytvoří a vrátí jeden Node
        /// </summary>
        /// <param name="parentKey"></param>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        private DataTreeListNode _CreateNode(string parentKey, NodeItemType nodeType)
        {
            if (__TotalNodesCount > 25000) return null;

            string childKey = "C." + (++_InternalNodeId).ToString();
            string text = "";
            DataTreeListNode childNode = null;
            switch (nodeType)
            {
                case NodeItemType.BlankAtFirstPosition:
                case NodeItemType.BlankAtLastPosition:
                    text = "";
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, canEdit: true, canDelete: false);          // Node pro přidání nového prvku (Blank) nelze odstranit
                    childNode.AddVoidCheckSpace = true;
                    childNode.ToolTipText = "Zadejte referenci nového prvku";
                    childNode.ImageDynamicDefault = "list_add_3_16";
                    __TotalNodesCount++;
                    break;
                case NodeItemType.OnDoubleClickLoadNext:
                    text = "Načíst další záznamy";
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, canEdit: false, canDelete: false);        // Node pro zobrazení dalších nodů nelze editovat ani odstranit
                    childNode.FontStyleDelta = FontStyle.Italic;
                    childNode.AddVoidCheckSpace = true;
                    childNode.ToolTipText = "Umožní načíst další sadu záznamů...";
                    childNode.ImageDynamicDefault = "move_task_down_16";
                    __TotalNodesCount++;
                    break;
                case NodeItemType.DefaultText:
                    text = Randomizer.GetSentence(2, 5);
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, canEdit: true, canDelete: true);
                    childNode.CanCheck = true;
                    childNode.Checked = (Randomizer.Rand.Next(20) > 16);
                    _FillNode(childNode);
                    __TotalNodesCount++;
                    break;
            }
            return childNode;
        }
        private void _FillNode(DataTreeListNode node)
        {
            if (Randomizer.IsTrue(10))
                node.ImageDynamicDefault = "object_locked_2_16";

            node.ImageName = _GetImageName();
            node.ToolTipTitle = null;
            node.ToolTipText = Randomizer.GetSentence(10, 50);
        }
        /// <summary>
        /// Vrátí náhodný obrázek z aktuální sady <see cref="__ImageSet"/>.
        /// </summary>
        /// <returns></returns>
        private string _GetImageName()
        {
            return _GetImageName(__ImageSet);
        }
        /// <summary>
        /// Vrátí náhodný obrázek z dané sady <paramref name="imageSet"/>.
        /// </summary>
        /// <returns></returns>
        private string _GetImageName(NodeImageSetType imageSet)
        {
            var images = _GetImages(imageSet);
            if (images != null && images.Length > 0)
                return Randomizer.GetItem(images);
            return null;
        }
        /// <summary>
        /// ID posledně přiděleného nodu
        /// </summary>
        private int _InternalNodeId;
        /// <summary>
        /// Celkový počet vygenerovaných nodes
        /// </summary>
        private int __TotalNodesCount;
        /// <summary>
        /// Základní typický počet nodů v Root úrovni; pro subnody je poloviční.
        /// </summary>
        private int __SampleCountBase;
        /// <summary>
        /// Maximální počet úrovní
        /// </summary>
        private int __SampleLevelsCount;
        /// <summary>
        /// Set aktuálních obrázků, podle <see cref="__ImageSet"/>. Autoinicializační.
        /// </summary>
        private string[] _ImagesCurrent { get { return _GetImages(__ImageSet); } }
        /// <summary>
        /// Vrátí set požadovaných obrázků. Autoinicializační.
        /// </summary>
        /// <param name="imageSet"></param>
        /// <returns></returns>
        private string[] _GetImages(NodeImageSetType imageSet)
        {
            switch (imageSet)
            {
                case NodeImageSetType.Documents:
                    if (__ImagesDocuments is null)
                        __ImagesDocuments = new string[]
{
    "svgimages/reports/alignmentbottomcenter.svg",
    "svgimages/reports/alignmentbottomleft.svg",
    "svgimages/reports/alignmentbottomright.svg",
    "svgimages/reports/alignmentcentercenter.svg",
    "svgimages/reports/alignmentcenterleft.svg",
    "svgimages/reports/alignmentcenterright.svg",
    "svgimages/reports/alignmenttopcenter.svg",
    "svgimages/reports/alignmenttopleft.svg",
    "svgimages/reports/alignmenttopright.svg",
    "svgimages/richedit/alignbottomcenter.svg",
    "svgimages/richedit/alignbottomcenterrotated.svg",
    "svgimages/richedit/alignbottomleft.svg",
    "svgimages/richedit/alignbottomleftrotated.svg",
    "svgimages/richedit/alignbottomright.svg",
    "svgimages/richedit/alignbottomrightrotated.svg",
    "svgimages/richedit/alignfloatingobjectbottomcenter.svg",
    "svgimages/richedit/alignfloatingobjectbottomleft.svg",
    "svgimages/richedit/alignfloatingobjectbottomright.svg",
    "svgimages/richedit/alignfloatingobjectmiddlecenter.svg",
    "svgimages/richedit/alignfloatingobjectmiddleleft.svg",
    "svgimages/richedit/alignfloatingobjectmiddleright.svg",
    "svgimages/richedit/alignfloatingobjecttopcenter.svg",
    "svgimages/richedit/alignfloatingobjecttopleft.svg",
    "svgimages/richedit/alignfloatingobjecttopright.svg",
    "svgimages/richedit/alignmiddlecenter.svg",
    "svgimages/richedit/alignmiddlecenterrotated.svg",
    "svgimages/richedit/alignmiddleleft.svg",
    "svgimages/richedit/alignmiddleleftrotated.svg",
    "svgimages/richedit/alignmiddleright.svg",
    "svgimages/richedit/alignmiddlerightrotated.svg",
    "svgimages/richedit/alignright.svg",
    "svgimages/richedit/aligntopcenter.svg",
    "svgimages/richedit/aligntopcenterrotated.svg",
    "svgimages/richedit/aligntopleft.svg",
    "svgimages/richedit/aligntopleftrotated.svg",
    "svgimages/richedit/aligntopright.svg",
    "svgimages/richedit/aligntoprightrotated.svg",
    "svgimages/richedit/borderbottom.svg",
    "svgimages/richedit/borderinsidehorizontal.svg",
    "svgimages/richedit/borderinsidevertical.svg",
    "svgimages/richedit/borderleft.svg",
    "svgimages/richedit/bordernone.svg",
    "svgimages/richedit/borderright.svg",
    "svgimages/richedit/bordersall.svg",
    "svgimages/richedit/bordersandshading.svg",
    "svgimages/richedit/bordersbox.svg",
    "svgimages/richedit/borderscustom.svg",
    "svgimages/richedit/bordersgrid.svg",
    "svgimages/richedit/bordersinside.svg",
    "svgimages/richedit/bordersoutside.svg",
    "svgimages/richedit/bordertop.svg"
};
                    return __ImagesDocuments;
                case NodeImageSetType.Actions:
                    if (__ImagesActions is null)
                        __ImagesActions = new string[]
{
    "svgimages/icon%20builder/actions_add.svg",
    "svgimages/icon%20builder/actions_addcircled.svg",
    "svgimages/icon%20builder/actions_arrow1down.svg",
    "svgimages/icon%20builder/actions_arrow1left.svg",
    "svgimages/icon%20builder/actions_arrow1leftdown.svg",
    "svgimages/icon%20builder/actions_arrow1leftup.svg",
    "svgimages/icon%20builder/actions_arrow1right.svg",
    "svgimages/icon%20builder/actions_arrow1rightdown.svg",
    "svgimages/icon%20builder/actions_arrow1rightup.svg",
    "svgimages/icon%20builder/actions_arrow1up.svg",
    "svgimages/icon%20builder/actions_arrow2down.svg",
    "svgimages/icon%20builder/actions_arrow2left.svg",
    "svgimages/icon%20builder/actions_arrow2leftdown.svg",
    "svgimages/icon%20builder/actions_arrow2leftup.svg",
    "svgimages/icon%20builder/actions_arrow2right.svg",
    "svgimages/icon%20builder/actions_arrow2rightdown.svg",
    "svgimages/icon%20builder/actions_arrow2rightup.svg",
    "svgimages/icon%20builder/actions_arrow2up.svg",
    "svgimages/icon%20builder/actions_arrow3down.svg",
    "svgimages/icon%20builder/actions_arrow3left.svg",
    "svgimages/icon%20builder/actions_arrow3right.svg",
    "svgimages/icon%20builder/actions_arrow3up.svg",
    "svgimages/icon%20builder/actions_arrow4down.svg",
    "svgimages/icon%20builder/actions_arrow4left.svg",
    "svgimages/icon%20builder/actions_arrow4leftdown.svg",
    "svgimages/icon%20builder/actions_arrow4leftup.svg",
    "svgimages/icon%20builder/actions_arrow4right.svg",
    "svgimages/icon%20builder/actions_arrow4rightdown.svg",
    "svgimages/icon%20builder/actions_arrow4rightup.svg",
    "svgimages/icon%20builder/actions_arrow4up.svg",
    "svgimages/icon%20builder/actions_arrow5downleft.svg",
    "svgimages/icon%20builder/actions_arrow5downright.svg",
    "svgimages/icon%20builder/actions_arrow5leftdown.svg",
    "svgimages/icon%20builder/actions_arrow5leftup.svg",
    "svgimages/icon%20builder/actions_arrow5rightdown.svg",
    "svgimages/icon%20builder/actions_arrow5rightup.svg",
    "svgimages/icon%20builder/actions_arrow5upleft.svg",
    "svgimages/icon%20builder/actions_arrow5upright.svg"
};
                    return __ImagesActions;
                case NodeImageSetType.Formats:
                    if (__ImagesFormats is null)
                        __ImagesFormats = new string[]
{
    "svgimages/export/exporttocsv.svg",
    "svgimages/export/exporttodoc.svg",
    "svgimages/export/exporttodocx.svg",
    "svgimages/export/exporttoepub.svg",
    "svgimages/export/exporttohtml.svg",
    "svgimages/export/exporttoimg.svg",
    "svgimages/export/exporttomht.svg",
    "svgimages/export/exporttoodt.svg",
    "svgimages/export/exporttopdf.svg",
    "svgimages/export/exporttortf.svg",
    "svgimages/export/exporttotxt.svg",
    "svgimages/export/exporttoxls.svg",
    "svgimages/export/exporttoxlsx.svg",
    "svgimages/export/exporttoxml.svg",
    "svgimages/export/exporttoxps.svg"
};
                    return __ImagesFormats;
                case NodeImageSetType.Charts:
                    if (__ImagesCharts is null)
                        __ImagesCharts = new string[]
{
    "svgimages/chart/chart.svg",
    "svgimages/chart/charttype_area.svg",
    "svgimages/chart/charttype_area3d.svg",
    "svgimages/chart/charttype_area3dstacked.svg",
    "svgimages/chart/charttype_area3dstacked100.svg",
    "svgimages/chart/charttype_areastacked.svg",
    "svgimages/chart/charttype_areastacked100.svg",
    "svgimages/chart/charttype_areastepstacked.svg",
    "svgimages/chart/charttype_areastepstacked100.svg",
    "svgimages/chart/charttype_bar.svg",
    "svgimages/chart/charttype_bar3d.svg",
    "svgimages/chart/charttype_bar3dstacked.svg",
    "svgimages/chart/charttype_bar3dstacked100.svg",
    "svgimages/chart/charttype_barstacked.svg",
    "svgimages/chart/charttype_barstacked100.svg",
    "svgimages/chart/charttype_boxplot.svg",
    "svgimages/chart/charttype_bubble.svg",
    "svgimages/chart/charttype_bubble3d.svg",
    "svgimages/chart/charttype_candlestick.svg",
    "svgimages/chart/charttype_doughnut.svg",
    "svgimages/chart/charttype_doughnut3d.svg",
    "svgimages/chart/charttype_funnel.svg",
    "svgimages/chart/charttype_funnel3d.svg",
    "svgimages/chart/charttype_gantt.svg",
    "svgimages/chart/charttype_histogram.svg",
    "svgimages/chart/charttype_line.svg",
    "svgimages/chart/charttype_line3d.svg",
    "svgimages/chart/charttype_line3dstacked.svg",
    "svgimages/chart/charttype_line3dstacked100.svg",
    "svgimages/chart/charttype_linestacked.svg",
    "svgimages/chart/charttype_linestacked100.svg",
    "svgimages/chart/charttype_manhattanbar.svg",
    "svgimages/chart/charttype_nesteddoughnut.svg",
    "svgimages/chart/charttype_pareto.svg",
    "svgimages/chart/charttype_pie.svg",
    "svgimages/chart/charttype_pie3d.svg",
    "svgimages/chart/charttype_point.svg",
    "svgimages/chart/charttype_point3d.svg",
    "svgimages/chart/charttype_polararea.svg",
    "svgimages/chart/charttype_polarline.svg",
    "svgimages/chart/charttype_polarpoint.svg",
    "svgimages/chart/charttype_polarrangearea.svg",
    "svgimages/chart/charttype_radararea.svg",
    "svgimages/chart/charttype_radarline.svg",
    "svgimages/chart/charttype_radarpoint.svg",
    "svgimages/chart/charttype_radarrangearea.svg",
    "svgimages/chart/charttype_rangearea.svg",
    "svgimages/chart/charttype_rangearea3d.svg",
    "svgimages/chart/charttype_rangebar.svg",
    "svgimages/chart/charttype_scatterline.svg",
    "svgimages/chart/charttype_scatterpolarline.svg",
    "svgimages/chart/charttype_scatterradarline.svg",
    "svgimages/chart/charttype_sidebysidebar3dstacked.svg",
    "svgimages/chart/charttype_sidebysidebar3dstacked100.svg",
    "svgimages/chart/charttype_sidebysidebarstacked.svg",
    "svgimages/chart/charttype_sidebysidebarstacked100.svg",
    "svgimages/chart/charttype_sidebysidegantt.svg",
    "svgimages/chart/charttype_sidebysiderangebar.svg",
    "svgimages/chart/charttype_spline.svg",
    "svgimages/chart/charttype_spline3d.svg",
    "svgimages/chart/charttype_splinearea.svg",
    "svgimages/chart/charttype_splinearea3d.svg",
    "svgimages/chart/charttype_splinearea3dstacked.svg",
    "svgimages/chart/charttype_splinearea3dstacked100.svg",
    "svgimages/chart/charttype_splineareastacked.svg",
    "svgimages/chart/charttype_splineareastacked100.svg",
    "svgimages/chart/charttype_steparea.svg",
    "svgimages/chart/charttype_steparea3d.svg",
    "svgimages/chart/charttype_stepline.svg",
    "svgimages/chart/charttype_stepline3d.svg",
    "svgimages/chart/charttype_stock.svg",
    "svgimages/chart/charttype_sunburst.svg",
    "svgimages/chart/charttype_swiftplot.svg",
    "svgimages/chart/charttype_waterfall.svg",
    "svgimages/chart/sankey.svg",
    "svgimages/chart/treemap.svg"
};
                    return __ImagesCharts;
                case NodeImageSetType.Spreadsheet:
                    if (__ImagesSpreadsheet is null)
                        __ImagesSpreadsheet = new string[]
                    {
                            "svgimages/spreadsheet/createarea3dchart.svg",
    "svgimages/spreadsheet/createareachart.svg",
    "svgimages/spreadsheet/createbar3dchart.svg",
    "svgimages/spreadsheet/createbarchart.svg",
    "svgimages/spreadsheet/createbubble3dchart.svg",
    "svgimages/spreadsheet/createbubblechart.svg",
    "svgimages/spreadsheet/createconebar3dchart.svg",
    "svgimages/spreadsheet/createconefullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createconemanhattanbarchart.svg",
    "svgimages/spreadsheet/createconestackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylindermanhattanbarchart.svg",
    "svgimages/spreadsheet/createcylinderstackedbar3dchart.svg",
    "svgimages/spreadsheet/createdoughnutchart.svg",
    "svgimages/spreadsheet/createexplodeddoughnutchart.svg",
    "svgimages/spreadsheet/createexplodedpie3dchart.svg",
    "svgimages/spreadsheet/createexplodedpiechart.svg",
    "svgimages/spreadsheet/createfullstackedarea3dchart.svg",
    "svgimages/spreadsheet/createfullstackedareachart.svg",
    "svgimages/spreadsheet/createfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createfullstackedbarchart.svg",
    "svgimages/spreadsheet/createfullstackedlinechart.svg",
    "svgimages/spreadsheet/createline3dchart.svg",
    "svgimages/spreadsheet/createlinechart.svg",
    "svgimages/spreadsheet/createmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpie3dchart.svg",
    "svgimages/spreadsheet/createpiechart.svg",
    "svgimages/spreadsheet/createpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpyramidstackedbar3dchart.svg",
    "svgimages/spreadsheet/createradarlinechart.svg",
    "svgimages/spreadsheet/createrotatedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedbarchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedbarchart.svg",
    "svgimages/spreadsheet/createrotatedstackedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createstackedarea3dchart.svg",
    "svgimages/spreadsheet/createstackedareachart.svg",
    "svgimages/spreadsheet/createstackedbar3dchart.svg",
    "svgimages/spreadsheet/createstackedbarchart.svg",
    "svgimages/spreadsheet/createstackedlinechart.svg"
};
                    return __ImagesSpreadsheet;
            }
            return null;
        }
        private string[] __ImagesDocuments;
        private string[] __ImagesActions;
        private string[] __ImagesFormats;
        private string[] __ImagesCharts;
        private string[] __ImagesSpreadsheet;
        private NodeImageSetType __ImageSet = NodeImageSetType.Charts;
        private NewNodePositionType __NewNodePosition = NewNodePositionType.None;
        private enum NodeImageSetType
        {
            None,
            Documents,
            Actions,
            Formats,
            Charts,
            Spreadsheet
        }
        private enum NewNodePositionType { None, First, Last }
        #endregion
        #region Parametry v okně
        private void _ParamsInit()
        {
            __TreeListMultiSelectChk = DxComponent.CreateDxCheckEdit(25, 8, 200, __ParamsPanel, "MultiSelectEnabled", _TreeListMultiSelectChkChecked, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1,
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, null,
                "MultiSelectEnabled = výběr více nodů", "Zaškrtnuto: lze vybrat více nodů (Ctrl, Shift). Sledujme pak události.");

            __TreeListGuideLinesChk = DxComponent.CreateDxCheckEdit(25, 38, 200, __ParamsPanel, "Guide Lines Visible", _TreeListGuideLinesChkChecked, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1,
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, null,
                "Guide Lines Visible = vodicí linky jsou viditelné", "Zaškrtnuto: Strom obsahuje GuideLines mezi úrovněmi nodů.");
      
            __LogToolTipChangeChk = DxComponent.CreateDxCheckEdit(375, 8, 200, __ParamsPanel, "Log: ToolTipChange", _LogToolTipChangeChkChecked, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1,
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, null,
                "Logovat události ToolTipChange", "Zaškrtnuto: při pohybu myši se plní Log událsotí.");
       
            // DxComponent.Settings.GetRawValue()


            __LogClearBtn = DxComponent.CreateDxSimpleButton(375, 250, 160, 30, __ParamsPanel, "Smazat Log", _LogClearBtnClick);

        }
        private void _TreeListMultiSelectChkChecked(object sender, EventArgs e)
        {
            if (__DxTreeList != null)
            {
                bool value = __TreeListMultiSelectChk.Checked;
                __DxTreeList.MultiSelectEnabled = value;
                _AddToLog($"MultiSelectEnabled: {value}");
            }
        }
        private void _TreeListGuideLinesChkChecked(object sender, EventArgs e)
        {
            if (__DxTreeList != null)
            {
                var value = _ConvertBoolN(__TreeListGuideLinesChk.CheckState);
                __DxTreeList.TreeListNative.OptionsView.ShowTreeLines = _ConvertDefaultBoolean(value);
                _AddToLog($"ShowTreeLines: {value}");
            }
        }

        private bool? _ConvertBoolN(CheckState checkState)
        {
            switch (checkState)
            {
                case CheckState.Checked: return true;
                case CheckState.Unchecked: return false;
                case CheckState.Indeterminate: return null;
            }
            return null;
        }
        private CheckState _ConvertCheckState(bool? value)
        {
            if (value.HasValue) return (value.Value ? CheckState.Checked: CheckState.Unchecked);
            return CheckState.Indeterminate;
        }
        private DevExpress.Utils.DefaultBoolean _ConvertDefaultBoolean(bool? value)
        {
            if (value.HasValue) return (value.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False);
            return DevExpress.Utils.DefaultBoolean.Default;
        }
        private void _LogToolTipChangeChkChecked(object sender, EventArgs e)
        {
            if (__LogToolTipChangeChk != null)
            {
                __LogToolTipChangeValue = __LogToolTipChangeChk.Checked;
            }
        }
        private void _LogClearBtnClick(object sender, EventArgs e)
        {
            _LogClear();
        }
        private DxCheckEdit __TreeListMultiSelectChk;
        private DxCheckEdit __TreeListGuideLinesChk;
        private DxCheckEdit __LogToolTipChangeChk;
        private DxSimpleButton __LogClearBtn;
        private bool __LogToolTipChangeValue;
        #endregion
        #region Settings
        private void _SettingLoad()
        {

            __TreeListMultiSelectChk.Checked = SetingsMultiSelect;
            __TreeListGuideLinesChk.CheckState = _ConvertCheckState(SetingsShowTreeLines);
            __LogToolTipChangeChk.Checked = SetingsLogToolTipChanges;

            __SettingsLoaded = true;
        }
        private void _SettingSave()
        {
            if (!__SettingsLoaded) return;
        }

        internal bool SetingsMultiSelect { get { return __SetingsMultiSelect; } set { __SetingsMultiSelect = value; _SettingSave(); } } private bool __SetingsMultiSelect;
        internal bool? SetingsShowTreeLines { get { return __SetingsShowTreeLines; } set { __SetingsShowTreeLines = value; _SettingSave(); } } private bool? __SetingsShowTreeLines;
        internal bool SetingsLogToolTipChanges { get { return __SetingsLogToolTipChanges; } set { __SetingsLogToolTipChanges = value; _SettingSave(); } } private bool __SetingsLogToolTipChanges;

        private bool __SettingsLoaded;
        #endregion
        #region Log událostí
        private void _LogInit()
        {
            _TreeListMemoEdit = DxComponent.CreateDxMemoEdit(0, 0, 100, 100, __LogPanel, readOnly: true);
            _TreeListMemoEdit.Dock = DockStyle.Fill;
            _TreeListMemoEdit.MouseEnter += _TreeListMemoEdit_MouseEnter;
        }
        private void _LogClear()
        {
            _TreeListLogId = 0;
            _TreeListLog = "";
            _TreeListShowLogText();
        }

        private void _AddToLog(string actionName, DxTreeListNodeArgs args, bool showValue = false)
        {
            string value = (showValue ? ", Value: " + (args.EditedValue == null ? "NULL" : "'" + args.EditedValue.ToString() + "'") : "");
            _AddToLog($"{actionName}: Node: {args.Node}{value}");
        }
        private void _AddToLog(string actionName, DxTreeListNodesArgs args)
        {
            string nodes = args.Nodes.ToOneString("; ");
            _AddToLog($"{actionName}: Nodes: {nodes}");
        }
        private void _AddToLog(string line, bool skipGUI = false)
        {
            int id = ++_TreeListLogId;
            var now = DateTime.Now;
            bool isLong = (_TreeListLogTime.HasValue && ((TimeSpan)(now - _TreeListLogTime.Value)).TotalMilliseconds > 750d);
            string log = id.ToString() + ". " + line + Environment.NewLine + (isLong ? Environment.NewLine : "") + _TreeListLog;
            _TreeListLog = log;
            _TreeListLogTime = now;
            if (skipGUI) _TreeListPending = true;
            else _TreeListShowLogText();
        }
        private void _TreeListShowLogText()
        {
            if (_TreeListMemoEdit != null)
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action(_TreeListShowLogText));
                else
                {
                    _TreeListMemoEdit.Text = _TreeListLog;
                    _TreeListPending = false;
                }
            }
        }
        private void _TreeListMemoEdit_MouseEnter(object sender, EventArgs e)
        {
            if (_TreeListPending)
                _TreeListShowLogText();
        }

        int _TreeListLogId;
        string _TreeListLog;
        DateTime? _TreeListLogTime;
        bool _TreeListPending;
        private DxMemoEdit _TreeListMemoEdit;
        #endregion
    }
}
